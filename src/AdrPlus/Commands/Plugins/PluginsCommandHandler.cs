// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the AdrPlus project under MIT license
// ***************************************************************************************

using AdrPlus.Core;
using AdrPlus.Domain;
using AdrPlus.Infrastructure.FileSystem;
using AdrPlus.Infrastructure.Formatting;
using AdrPlus.Infrastructure.Logging;
using AdrPlus.Infrastructure.UI;
using AdrPlus.Plugins;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace AdrPlus.Commands.Plugins
{
    /// <summary>
    /// Handles the <c>plugins</c> command's diagnostic, activation-management, and distribution subcommands:
    /// <c>list</c> reports every loaded plugin (name, version, subscribed events, allowlist status,
    /// pending-item count); <c>validate</c> re-runs structural load validation and reports loaded vs.
    /// rejected plugins, without dispatching any event; <c>--activate</c>/<c>--deactivate</c> are the
    /// non-interactive counterpart to the wizard's manage mode, adding or removing a single plugin name from
    /// <c>ActivePlugins</c>; <c>--install</c>/<c>--uninstall</c> are host-global, zip-based operations against
    /// <see cref="IPluginManager.UserPluginsRoot"/> — no repository is in scope for either.
    /// </summary>
    /// <param name="logger">The logger for recording command execution and errors.</param>
    /// <param name="fileSystem">The file system service for I/O operations.</param>
    /// <param name="prompt">The console writer for displaying output.</param>
    /// <param name="adrServices">The ADR services for argument parsing and help text.</param>
    /// <param name="validateConfig">The service for validating repository configuration, used by the wizard's folder browser.</param>
    /// <param name="config">The application configuration, providing the optional plugin allowlist.</param>
    /// <param name="pluginManager">The plugin manager used to discover and validate plugins.</param>
    internal sealed partial class PluginsCommandHandler(
        ILogger<PluginsCommandHandler> logger,
        IFileSystemService fileSystem,
        IConsoleWriter prompt,
        IAdrServices adrServices,
        IValidateConfig validateConfig,
        IOptions<AdrPlusConfig> config,
        IPluginManager pluginManager) : ICommandHandler
    {
        private readonly ILogger<PluginsCommandHandler> _logger = logger;
        private readonly IFileSystemService _fileSystem = fileSystem;
        private readonly IConsoleWriter _prompt = prompt;
        private readonly IAdrServices _adrServices = adrServices;
        private readonly IValidateConfig _validateConfig = validateConfig;
        private readonly AdrPlusConfig _config = config.Value;
        private readonly IPluginManager _pluginManager = pluginManager;

        [GeneratedRegex(@"^(?<name>[A-Za-z0-9_.-]+)-(?<version>\d+\.\d+\.\d+)\.zip$")]
        private static partial Regex PluginZipFileNameRegex();

        private static readonly Arguments[] ValidCommandArgs =
            [Arguments.TargetRepo,
             Arguments.PluginsList,
             Arguments.PluginsValidate,
             Arguments.PluginsActivate,
             Arguments.PluginsDeactivate,
             Arguments.PluginsInstall,
             Arguments.PluginsUninstall,
             Arguments.PluginsForce,
             Arguments.WizardPlugins,
             Arguments.Help];

        public async Task ExecuteAsync(string[] args, CancellationToken cancellationToken = default)
        {
            try
            {
                ArgumentNullException.ThrowIfNull(args);
                var parsedArgs = _adrServices.ParseArgs(args, ValidCommandArgs);
                if (parsedArgs.ContainsKey(Arguments.Help))
                {
                    _prompt.PromptWriteHelp(_adrServices.GetHelpText(
                        "plugins",
                        ValidCommandArgs,
                        [
                            "adrplus plugins --wizard",
                            "adrplus plugins --list --path \"path/to/repository/\"",
                            "adrplus plugins --validate --path \"path/to/repository/\"",
                            "adrplus plugins --activate \"PluginName\" --path \"path/to/repository/\"",
                            "adrplus plugins --deactivate \"PluginName\" --path \"path/to/repository/\"",
                            "adrplus plugins --install \"./PluginName-1.0.0.zip\"",
                            "adrplus plugins --uninstall \"PluginName\"",
                        ]));
                    return;
                }

                var hasWizard = parsedArgs.ContainsKey(Arguments.WizardPlugins);
                if (hasWizard)
                {
                    var wizardResult = await PluginsWizard(cancellationToken);
                    if (wizardResult is null)
                    {
                        return;
                    }
                    parsedArgs = wizardResult;
                }

                var hasList = parsedArgs.ContainsKey(Arguments.PluginsList);
                var hasValidate = parsedArgs.ContainsKey(Arguments.PluginsValidate);
                var hasActivate = parsedArgs.ContainsKey(Arguments.PluginsActivate);
                var hasDeactivate = parsedArgs.ContainsKey(Arguments.PluginsDeactivate);
                var hasInstall = parsedArgs.ContainsKey(Arguments.PluginsInstall);
                var hasUninstall = parsedArgs.ContainsKey(Arguments.PluginsUninstall);
                var modeCount = new[] { hasList, hasValidate, hasActivate, hasDeactivate, hasInstall, hasUninstall }.Count(selected => selected);
                if (modeCount > 1)
                {
                    throw new ArgumentException(string.Format(null, FormatMessages.PluginsModeAmbiguous));
                }
                if (modeCount == 0)
                {
                    throw new ArgumentException(string.Format(null, FormatMessages.PluginsModeRequired));
                }

                // --install/--uninstall are host-global — no repository is in scope, so neither
                // reads Arguments.TargetRepo nor requires --path. Handled before the --path check below, which
                // only applies to the remaining, repo-scoped modes.
                if (hasInstall)
                {
                    await InstallPluginAsync(parsedArgs[Arguments.PluginsInstall], parsedArgs.ContainsKey(Arguments.PluginsForce), cancellationToken);
                    return;
                }
                if (hasUninstall)
                {
                    UninstallPlugin(parsedArgs[Arguments.PluginsUninstall]);
                    return;
                }

                parsedArgs.TryGetValue(Arguments.TargetRepo, out var targetPath);
                targetPath ??= string.Empty;

                if (!_fileSystem.DirectoryExists(targetPath))
                {
                    throw new DirectoryNotFoundException(string.Format(null, FormatMessages.ErrDirectoryNotFound, targetPath));
                }

                if (hasActivate)
                {
                    await SetActivePluginAsync(targetPath, parsedArgs[Arguments.PluginsActivate], activate: true, cancellationToken);
                    return;
                }
                if (hasDeactivate)
                {
                    await SetActivePluginAsync(targetPath, parsedArgs[Arguments.PluginsDeactivate], activate: false, cancellationToken);
                    return;
                }

                await _pluginManager.LoadPluginsAsync(cancellationToken);

                if (hasList)
                {
                    var (repoconfig, _) = await ReadRepoConfigAsync(targetPath, cancellationToken);
                    await ReportListAsync(hasWizard, targetPath, repoconfig, cancellationToken);
                }
                else
                {
                    ReportValidate(hasWizard, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                LogMessages.LogCommandException(_logger, ex);
                throw;
            }
        }

        /// <summary>
        /// Reports <see cref="IPluginManager.LoadedPlugins"/>. When <paramref name="useTable"/> is
        /// <see langword="true"/> (the <c>--wizard</c> path only), rows are shown in a read-only
        /// <see cref="IConsoleWriter.PromptShowPluginsListTable"/> instead of one line per plugin — the
        /// non-interactive <c>--list</c> flag always uses plain text and stays scriptable.
        /// </summary>
        private async Task ReportListAsync(bool useTable, string targetPath, AdrPlusRepoConfig repoconfig, CancellationToken cancellationToken)
        {
            var allowlistStatus = _config.PluginAllowlist is null
                ? string.Format(null, FormatMessages.PluginsNoAllowlistConfigured)
                : string.Format(null, FormatMessages.PluginsAllowlisted);
            var rows = await BuildListRowsAsync(targetPath, repoconfig, allowlistStatus, cancellationToken);

            if (rows.Count == 0)
            {
                _prompt.PromptWriteInfo(string.Format(null, FormatMessages.PluginsListEmpty));
            }
            else if (useTable)
            {
                if (_prompt.PromptShowPluginsListTable(rows, cancellationToken))
                {
                    throw new OperationCanceledException(Resources.AdrPlus.CancelledByUser);
                }
            }
            else
            {
                foreach (var row in rows)
                {
                    var message = string.Format(null, FormatMessages.PluginsListEntry, row.Status, row.Name, row.Version, row.Events, row.Allowlist, row.Pending);
                    _prompt.PromptWriteInfo(message);
                }
            }

            var summary = string.Format(null, FormatMessages.PluginsListSummary, _pluginManager.LoadedPlugins.Count, _pluginManager.Rejections.Count);
            LogMessages.LogCommandSuccessful(_logger, summary);
            _prompt.PromptWriteSuccess(summary);
        }

        /// <summary>
        /// Builds one row per loaded plugin (<c>Active</c>/<c>Inactive</c> depending on <see cref="AdrPlusRepoConfig.ActivePlugins"/>,
        /// or <c>Disabled</c> uniformly when <see cref="AdrPlusRepoConfig.DisablePlugins"/>), plus a synthetic row
        /// for each active-listed name that isn't currently loaded (<c>Missing</c>, or <c>Disabled</c> if the
        /// repo's plugins are off) — the only way the leading status column surfaces a plugin that vanished,
        /// since it has no <see cref="LoadedPlugin"/> row otherwise.
        /// </summary>
        private async Task<List<(string Status, string Name, string Version, string Events, string Allowlist, int Pending)>> BuildListRowsAsync(string targetPath, AdrPlusRepoConfig repoconfig, string allowlistStatus, CancellationToken cancellationToken)
        {
            var rows = new List<(string Status, string Name, string Version, string Events, string Allowlist, int Pending)>();
            var activeNames = new HashSet<string>(repoconfig.ActivePlugins, StringComparer.OrdinalIgnoreCase);
            var loadedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var disabledLabel = string.Format(null, FormatMessages.PluginsStatusDisabled);
            var pendingStateRoot = Path.Combine(targetPath, "plugins-state");

            foreach (var plugin in _pluginManager.LoadedPlugins)
            {
                loadedNames.Add(plugin.Manifest.Name!);
                var pending = await PendingStateStore.ReadAllAsync(_fileSystem, Path.Combine(pendingStateRoot, plugin.Manifest.Name!), cancellationToken);
                var events = string.Join(", ", plugin.Manifest.SubscribedEvents ?? []);
                var status = repoconfig.DisablePlugins
                    ? disabledLabel
                    : activeNames.Contains(plugin.Manifest.Name!)
                        ? string.Format(null, FormatMessages.PluginsStatusActive)
                        : string.Format(null, FormatMessages.PluginsStatusInactive);
                rows.Add((status, plugin.Manifest.Name!, plugin.Manifest.Version!, events, allowlistStatus, pending.Count));
            }

            foreach (var missingName in repoconfig.ActivePlugins.Where(name => !loadedNames.Contains(name)))
            {
                var status = repoconfig.DisablePlugins ? disabledLabel : string.Format(null, FormatMessages.PluginsStatusMissing);
                rows.Add((status, missingName, "-", "-", allowlistStatus, 0));
            }

            return rows;
        }

        /// <summary>
        /// Reads, validates, and deserializes <paramref name="targetPath"/>'s <c>adr-config.adrplus</c> — the same
        /// pattern every other command handler follows, now also required by <c>plugins --list</c> to determine
        /// each plugin's active/inactive status (previously this handler only checked the directory existed).
        /// </summary>
        private async Task<(AdrPlusRepoConfig Config, string ConfigPath)> ReadRepoConfigAsync(string targetPath, CancellationToken cancellationToken)
        {
            var configPath = Path.GetFullPath(Path.Combine(targetPath, _validateConfig.GetFileNameRepoConfig()));
            if (!_fileSystem.FileExists(configPath))
            {
                throw new FileNotFoundException(string.Format(null, FormatMessages.ErrFileNotFound, configPath));
            }

            var jsonString = await _fileSystem.ReadAllTextAsync(configPath, cancellationToken);
            var (isValid, errorReport) = _validateConfig.ValidateRepoStructure(jsonString);
            if (!isValid)
            {
                LogAndWriteErrors(errorReport);
                throw new InvalidDataException(string.Format(null, FormatMessages.ErrInvalidRepositoryConfig, configPath));
            }

            var repoconfig = JsonSerializer.Deserialize<AdrPlusRepoConfig>(jsonString, AppConstants.RepoSerializerOptions)!;
            return (repoconfig, configPath);
        }

        private void LogAndWriteErrors(string[] errors)
        {
            foreach (var error in errors)
            {
                LogMessages.LogCommandFailure(_logger, error);
                _prompt.PromptWriteError(error);
            }
        }

        /// <summary>
        /// The <c>adrplus plugins --wizard</c> manage mode: lets the user choose the repo's new <c>ActivePlugins</c>
        /// baseline via a <c>MultiSelect</c> over currently loaded plugins, pre-checked by the current baseline.
        /// A plugin that's currently <c>Missing</c> (listed but not loaded) and not re-selected here is naturally
        /// dropped from the list — that's how drift resolves itself, with no extra special-casing needed.
        /// </summary>
        private async Task RunManageActivePluginsAsync(string targetPath, CancellationToken cancellationToken)
        {
            var (repoconfig, configPath) = await ReadRepoConfigAsync(targetPath, cancellationToken);
            await _pluginManager.LoadPluginsAsync(cancellationToken);

            var loadedNames = _pluginManager.LoadedPlugins.Select(plugin => plugin.Manifest.Name!).ToArray();
            var currentlyActive = new HashSet<string>(repoconfig.ActivePlugins, StringComparer.OrdinalIgnoreCase);

            var selection = _prompt.PromptSelectActivePlugins(loadedNames, currentlyActive, cancellationToken);
            if (selection.IsAborted)
            {
                throw new OperationCanceledException(Resources.AdrPlus.CancelledByUser);
            }

            await WriteActivePluginsAndReportAsync(configPath, selection.SelectedNames, cancellationToken);
        }

        /// <summary>
        /// The non-interactive <c>--activate</c>/<c>--deactivate</c> counterpart to the wizard's manage mode:
        /// adds or removes a single <paramref name="name"/> from <c>ActivePlugins</c> instead of
        /// replacing the whole baseline. Idempotent by construction — <see cref="HashSet{T}"/> with
        /// <see cref="StringComparer.OrdinalIgnoreCase"/> makes activating an already-active name or
        /// deactivating an already-inactive one a no-op. Deliberately does not require <paramref name="name"/>
        /// to match a currently loaded plugin — a typo surfaces later as <c>Missing</c> via the existing
        /// active-plugin warning, so this stays free of plugin-loading here.
        /// </summary>
        private async Task SetActivePluginAsync(string targetPath, string name, bool activate, CancellationToken cancellationToken)
        {
            var (repoconfig, configPath) = await ReadRepoConfigAsync(targetPath, cancellationToken);
            var newActiveNames = new HashSet<string>(repoconfig.ActivePlugins, StringComparer.OrdinalIgnoreCase);
            if (activate)
            {
                newActiveNames.Add(name);
            }
            else
            {
                newActiveNames.Remove(name);
            }

            await WriteActivePluginsAndReportAsync(configPath, newActiveNames, cancellationToken);
        }

        /// <summary>
        /// Shared by the wizard's manage mode and the direct <c>--activate</c>/<c>--deactivate</c> flags:
        /// persists the new <c>ActivePlugins</c> baseline and reports the same success message either way.
        /// </summary>
        private async Task WriteActivePluginsAndReportAsync(string configPath, IEnumerable<string> newActiveNames, CancellationToken cancellationToken)
        {
            var names = newActiveNames.ToArray();
            await ActivePluginsWriter.WriteAsync(_fileSystem, configPath, names, cancellationToken);

            var message = string.Format(null, FormatMessages.PluginsActiveUpdated, names.Length == 0 ? "-" : string.Join(", ", names));
            LogMessages.LogCommandSuccessful(_logger, message);
            _prompt.PromptWriteSuccess(message);
        }

        /// <summary>
        /// Non-interactive plugin distribution: a host-global, zip-based flow — no
        /// repository is in scope. <paramref name="zipPath"/> must be named <c>&lt;name&gt;-&lt;version&gt;.zip</c>;
        /// the destination folder name comes from that file name, not from the manifest's <c>Name</c> (which may
        /// legitimately differ, as with the bundled <c>AdrIndexer</c>/<c>adr-indexer</c> pair). Extracts to a
        /// staging folder under <see cref="IPluginManager.UserPluginsRoot"/> first (same volume as the
        /// destination, so the final <see cref="Directory.Move"/> is a cheap rename, not a cross-volume copy) so
        /// a failed/mismatched zip never touches the real destination. Every entry is rejected if it contains
        /// <c>/</c>, <c>\</c>, <c>..</c>, or <c>:</c> — mirrors <c>PluginLoader</c>'s <c>entryAssembly</c>
        /// path-traversal guard, and as a side effect enforces the flat archive layout every plugin
        /// folder already uses. Never touches any repo's <c>activeplugins</c> — a downloaded zip must never
        /// start dispatching on its own; activating it is a separate step via <c>--activate</c>. With
        /// <paramref name="force"/>, an existing destination is deleted and replaced entirely, including its
        /// <c>plugin.json</c> — an accepted trade-off (no surgical merge of local customization), not a bug.
        /// One installed version per name per host — "exists" means "this name has any version installed,"
        /// since there is nowhere for a second version to coexist.
        /// </summary>
        private async Task InstallPluginAsync(string zipPath, bool force, CancellationToken cancellationToken)
        {
            if (!_fileSystem.FileExists(zipPath))
            {
                throw new FileNotFoundException(string.Format(null, FormatMessages.ErrFileNotFound, zipPath));
            }

            var zipFileName = Path.GetFileName(zipPath);
            var match = PluginZipFileNameRegex().Match(zipFileName);
            if (!match.Success)
            {
                throw new ArgumentException(string.Format(null, FormatMessages.ErrPluginZipNameInvalid, zipFileName));
            }
            var name = match.Groups["name"].Value;
            var version = match.Groups["version"].Value;

            var pluginsRoot = _pluginManager.UserPluginsRoot;
            var destDir = Path.Combine(pluginsRoot, name);
            if (Directory.Exists(destDir) && !force)
            {
                throw new InvalidOperationException(string.Format(null, FormatMessages.ErrPluginAlreadyInstalled, destDir));
            }

            Directory.CreateDirectory(pluginsRoot);
            var stagingDir = Path.Combine(pluginsRoot, $".install-staging-{Guid.NewGuid():N}");
            Directory.CreateDirectory(stagingDir);
            var movedIntoPlace = false;
            try
            {
                using (var archive = ZipFile.OpenRead(zipPath))
                {
                    foreach (var entry in archive.Entries)
                    {
                        if (entry.Name.Length == 0)
                        {
                            continue;
                        }
                        if (ContainsUnsafeZipEntryPath(entry.FullName))
                        {
                            throw new InvalidDataException(string.Format(null, FormatMessages.ErrPluginZipTraversal, entry.FullName));
                        }
                        entry.ExtractToFile(Path.Combine(stagingDir, entry.FullName), overwrite: true);
                    }
                }

                var manifestPath = Path.Combine(stagingDir, "plugin.json");
                if (!File.Exists(manifestPath))
                {
                    throw new InvalidDataException(string.Format(null, FormatMessages.ErrPluginZipMissingManifest));
                }
                var manifestJson = await File.ReadAllTextAsync(manifestPath, cancellationToken);
                var manifest = JsonSerializer.Deserialize<PluginManifest>(manifestJson, PluginManifest.SerializerOptions);
                if (manifest is null
                    || !string.Equals(manifest.Name, name, StringComparison.OrdinalIgnoreCase)
                    || !string.Equals(manifest.Version, version, StringComparison.Ordinal))
                {
                    throw new InvalidDataException(string.Format(null, FormatMessages.ErrPluginZipManifestMismatch,
                        manifest?.Name ?? "?", manifest?.Version ?? "?", name, version));
                }

                if (Directory.Exists(destDir))
                {
                    Directory.Delete(destDir, recursive: true);
                }
                Directory.Move(stagingDir, destDir);
                movedIntoPlace = true;
            }
            finally
            {
                if (!movedIntoPlace && Directory.Exists(stagingDir))
                {
                    Directory.Delete(stagingDir, recursive: true);
                }
            }

            var installedMessage = string.Format(null, FormatMessages.PluginInstalled, name, version, ComputeSha256(zipPath));
            LogMessages.LogCommandSuccessful(_logger, installedMessage);
            _prompt.PromptWriteSuccess(installedMessage);

            await _pluginManager.LoadPluginsAsync(cancellationToken);
            var loaded = _pluginManager.LoadedPlugins.FirstOrDefault(plugin => string.Equals(plugin.FolderPath, destDir, StringComparison.OrdinalIgnoreCase));
            if (loaded is not null)
            {
                _prompt.PromptWriteInfo(string.Format(null, FormatMessages.PluginsValidateEntryValid, loaded.Manifest.Name, loaded.Manifest.Version));
                return;
            }
            var rejection = _pluginManager.Rejections.FirstOrDefault(r => string.Equals(r.FolderPath, destDir, StringComparison.OrdinalIgnoreCase));
            if (rejection is not null)
            {
                _prompt.PromptWriteInfo(string.Format(null, FormatMessages.PluginsValidateEntryRejected, rejection.FolderPath, rejection.Reason, rejection.Message));
            }
        }

        /// <summary>
        /// Non-interactive plugin removal: deletes <see cref="IPluginManager.UserPluginsRoot"/>'s
        /// <c>&lt;name&gt;/</c> folder if present (a no-op, not an error, if it's already gone — uninstall is
        /// safe to re-run). <c>--uninstall</c> is host-global and never touches any repo's <c>activeplugins</c>
        /// — with no repository in scope, and no registry of which repos on the machine might still reference
        /// this name, there is nothing safe to edit here. Drift resolves itself as a <c>Missing</c> warning the
        /// next time an affected repo runs a dispatching command — this is intentional, not a gap.
        /// </summary>
        private void UninstallPlugin(string name)
        {
            var destDir = Path.Combine(_pluginManager.UserPluginsRoot, name);
            if (Directory.Exists(destDir))
            {
                Directory.Delete(destDir, recursive: true);
                var message = string.Format(null, FormatMessages.PluginUninstalled, name);
                LogMessages.LogCommandSuccessful(_logger, message);
                _prompt.PromptWriteSuccess(message);
            }
        }

        private static bool ContainsUnsafeZipEntryPath(string entryFullName) =>
            entryFullName.Contains('/') || entryFullName.Contains('\\') || entryFullName.Contains("..", StringComparison.Ordinal) || entryFullName.Contains(':');

        private static string ComputeSha256(string filePath)
        {
            using var stream = File.OpenRead(filePath);
            return Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
        }

        /// <summary>
        /// Reports <see cref="IPluginManager.LoadedPlugins"/> (as valid) and <see cref="IPluginManager.Rejections"/>
        /// (as rejected). When <paramref name="useTable"/> is <see langword="true"/> (the <c>--wizard</c> path
        /// only), rows are shown in a read-only <see cref="IConsoleWriter.PromptShowPluginsValidateTable"/> —
        /// the non-interactive <c>--validate</c> flag always uses plain text and stays scriptable.
        /// </summary>
        private void ReportValidate(bool useTable, CancellationToken cancellationToken)
        {
            var loaded = _pluginManager.LoadedPlugins;
            var rejections = _pluginManager.Rejections;

            if (loaded.Count == 0 && rejections.Count == 0)
            {
                _prompt.PromptWriteInfo(string.Format(null, FormatMessages.PluginsValidateEmpty));
            }
            else if (useTable)
            {
                var rows = BuildValidateTableRows(loaded, rejections);
                if (_prompt.PromptShowPluginsValidateTable(rows, cancellationToken))
                {
                    throw new OperationCanceledException(Resources.AdrPlus.CancelledByUser);
                }
            }
            else
            {
                foreach (var plugin in loaded)
                {
                    var message = string.Format(null, FormatMessages.PluginsValidateEntryValid, plugin.Manifest.Name, plugin.Manifest.Version);
                    _prompt.PromptWriteInfo(message);
                }

                foreach (var rejection in rejections)
                {
                    var message = string.Format(null, FormatMessages.PluginsValidateEntryRejected, rejection.FolderPath, rejection.Reason, rejection.Message);
                    _prompt.PromptWriteInfo(message);
                }
            }

            var summary = string.Format(null, FormatMessages.PluginsValidateSummary, loaded.Count, rejections.Count);
            LogMessages.LogCommandSuccessful(_logger, summary);
            _prompt.PromptWriteSuccess(summary);
        }

        private static List<(string Status, string NameOrFolder, string Detail)> BuildValidateTableRows(
            IReadOnlyList<LoadedPlugin> loaded, IReadOnlyList<PluginRejection> rejections)
        {
            var rows = new List<(string Status, string NameOrFolder, string Detail)>();
            foreach (var plugin in loaded)
            {
                rows.Add((
                    string.Format(null, FormatMessages.PluginsValidateStatusValid),
                    plugin.Manifest.Name!,
                    $"v{plugin.Manifest.Version}"));
            }
            foreach (var rejection in rejections)
            {
                rows.Add((
                    string.Format(null, FormatMessages.PluginsValidateStatusRejected),
                    rejection.FolderPath,
                    $"{rejection.Reason}: {rejection.Message}"));
            }
            return rows;
        }

        /// <summary>
        /// Interactive <c>adrplus plugins --wizard</c>: picks a mode first, then — only for the repo-scoped
        /// modes (<c>list</c>/<c>validate</c>/<c>manage</c>) — resolves a repository path via prompts. Install
        /// and uninstall are host-global and never prompt for a repository at all. For
        /// <c>list</c>/<c>validate</c>, returns the same <see cref="Dictionary{Arguments, String}"/> shape
        /// <c>ParseArgs</c> would have produced for the equivalent non-interactive flags — the rest of
        /// <see cref="ExecuteAsync"/> runs unchanged from there (only its final rendering step is aware of
        /// <c>--wizard</c>, to show a table instead of plain text). <c>manage</c>/<c>install</c>/<c>uninstall</c>/<c>back</c>
        /// instead run to completion here (or, for <c>back</c>, do nothing) and return <see langword="null"/>,
        /// signalling <see cref="ExecuteAsync"/> that there's nothing left to do.
        /// </summary>
        /// <exception cref="OperationCanceledException">Thrown when the user cancels any prompt.</exception>
        private async Task<Dictionary<Arguments, string>?> PluginsWizard(CancellationToken cancellationToken)
        {
            var modePrompt = _prompt.PromptSelectPluginsMode(cancellationToken);
            if (modePrompt.IsAborted)
            {
                throw new OperationCanceledException(Resources.AdrPlus.CancelledByUser);
            }

            if (modePrompt.Mode == PluginsWizardMode.Back)
            {
                Helper.SkipWizardContinuePrompt = true;
                return null;
            }

            if (modePrompt.Mode == PluginsWizardMode.Install)
            {
                var installRootPath = ResolveRootPath(cancellationToken);
                var zipPrompt = _prompt.PromptInputPluginZipPath(installRootPath, cancellationToken);
                if (zipPrompt.IsAborted)
                {
                    throw new OperationCanceledException(Resources.AdrPlus.CancelledByUser);
                }
                var forcePrompt = _prompt.PromptConfirm(Resources.AdrPlus.PromptPluginInstallForce, cancellationToken);
                if (forcePrompt.IsAborted)
                {
                    throw new OperationCanceledException(Resources.AdrPlus.CancelledByUser);
                }
                var installArgs = new Dictionary<Arguments, string>
                {
                    [Arguments.PluginsInstall] = zipPrompt.ZipPath
                };
                if (forcePrompt.ConfirmYes)
                {
                    installArgs[Arguments.PluginsForce] = string.Empty;
                }
                return installArgs;
            }

            if (modePrompt.Mode == PluginsWizardMode.Uninstall)
            {
                var installedNames = GetInstalledPluginFolderNames();
                if (installedNames.Length == 0)
                {
                    _prompt.PromptWriteInfo(string.Format(null, FormatMessages.PluginsNoInstalledPlugins));
                    return null;
                }
                var uninstallPrompt = _prompt.PromptSelectPluginsToUninstall(installedNames, cancellationToken);
                if (uninstallPrompt.IsAborted)
                {
                    throw new OperationCanceledException(Resources.AdrPlus.CancelledByUser);
                }
                foreach (var name in uninstallPrompt.SelectedNames)
                {
                    UninstallPlugin(name);
                }
                return null;
            }

            var rootPath = ResolveRootPath(cancellationToken);

            var folderPrompt = _prompt.PromptSelectFolderPath(Resources.AdrPlus.PromptSelectRepositoryPath, true, rootPath, _fileSystem, _validateConfig, cancellationToken);
            if (folderPrompt.IsAborted)
            {
                throw new OperationCanceledException(Resources.AdrPlus.CancelledByUser);
            }

            if (modePrompt.Mode == PluginsWizardMode.Manage)
            {
                await RunManageActivePluginsAsync(folderPrompt.Content, cancellationToken);
                return null;
            }

            var parsedArgs = new Dictionary<Arguments, string>
            {
                [Arguments.TargetRepo] = folderPrompt.Content
            };
            parsedArgs[modePrompt.Mode == PluginsWizardMode.Validate ? Arguments.PluginsValidate : Arguments.PluginsList] = string.Empty;
            return parsedArgs;
        }

        /// <summary>
        /// Resolves the starting path for the wizard's file/folder browsers: the sole drive when there's
        /// only one, otherwise prompts the user to pick one.
        /// </summary>
        /// <exception cref="OperationCanceledException">Thrown when the user cancels the drive prompt.</exception>
        private string ResolveRootPath(CancellationToken cancellationToken)
        {
            string[] drives = _fileSystem.GetDrives();
            if (drives.Length <= 1)
            {
                return drives[0];
            }
            var (isAborted, content) = _prompt.PromptSelectLogicalDrive(Resources.AdrPlus.NewAdrPromptSelectDrive, _fileSystem, cancellationToken);
            if (isAborted)
            {
                throw new OperationCanceledException(Resources.AdrPlus.CancelledByUser);
            }
            return content;
        }

        /// <summary>
        /// Every folder name currently under <see cref="IPluginManager.UserPluginsRoot"/> — used by the
        /// wizard's uninstall mode to offer a selection instead of requiring the name be typed. Deliberately
        /// scans the real folder tree (not <see cref="IPluginManager.LoadedPlugins"/>) so a rejected/broken
        /// plugin can still be found and removed. Staging folders left over from a crashed install (which
        /// should already self-clean, see <see cref="InstallPluginAsync"/>) are filtered out defensively.
        /// </summary>
        private string[] GetInstalledPluginFolderNames()
        {
            var pluginsRoot = _pluginManager.UserPluginsRoot;
            if (string.IsNullOrEmpty(pluginsRoot) || !Directory.Exists(pluginsRoot))
            {
                return [];
            }

            return Directory.GetDirectories(pluginsRoot)
                .Select(Path.GetFileName)
                .Where(name => !string.IsNullOrEmpty(name) && !name.StartsWith('.'))
                .Select(name => name!)
                .ToArray();
        }
    }
}
