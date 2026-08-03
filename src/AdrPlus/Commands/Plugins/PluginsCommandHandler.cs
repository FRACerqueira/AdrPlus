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
using System.Text.Json;

namespace AdrPlus.Commands.Plugins
{
    /// <summary>
    /// Handles the <c>plugins</c> command's diagnostic subcommands (spec §8, Fase 7): <c>list</c> reports every
    /// loaded plugin (name, version, subscribed events, allowlist status, pending-item count); <c>validate</c>
    /// re-runs the Fase 3 structural load validation and reports loaded vs. rejected plugins, without
    /// dispatching any event.
    /// </summary>
    /// <param name="logger">The logger for recording command execution and errors.</param>
    /// <param name="fileSystem">The file system service for I/O operations.</param>
    /// <param name="prompt">The console writer for displaying output.</param>
    /// <param name="adrServices">The ADR services for argument parsing and help text.</param>
    /// <param name="validateConfig">The service for validating repository configuration, used by the wizard's folder browser.</param>
    /// <param name="config">The application configuration, providing the optional plugin allowlist.</param>
    /// <param name="pluginManager">The plugin manager used to discover and validate plugins.</param>
    internal sealed class PluginsCommandHandler(
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

        private static readonly Arguments[] ValidCommandArgs =
            [Arguments.TargetRepo,
             Arguments.PluginsList,
             Arguments.PluginsValidate,
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
                if (hasList && hasValidate)
                {
                    throw new ArgumentException(string.Format(null, FormatMessages.PluginsModeAmbiguous));
                }
                if (!hasList && !hasValidate)
                {
                    throw new ArgumentException(string.Format(null, FormatMessages.PluginsModeRequired));
                }

                parsedArgs.TryGetValue(Arguments.TargetRepo, out var targetPath);
                targetPath ??= string.Empty;

                if (!_fileSystem.DirectoryExists(targetPath))
                {
                    throw new DirectoryNotFoundException(string.Format(null, FormatMessages.ErrDirectoryNotFound, targetPath));
                }

                await _pluginManager.LoadPluginsAsync(Path.Combine(targetPath, "plugins"), cancellationToken);

                if (hasList)
                {
                    var (repoconfig, _) = await ReadRepoConfigAsync(targetPath, cancellationToken);
                    await ReportListAsync(hasWizard, repoconfig, cancellationToken);
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
        private async Task ReportListAsync(bool useTable, AdrPlusRepoConfig repoconfig, CancellationToken cancellationToken)
        {
            var allowlistStatus = _config.PluginAllowlist is null
                ? string.Format(null, FormatMessages.PluginsNoAllowlistConfigured)
                : string.Format(null, FormatMessages.PluginsAllowlisted);
            var rows = await BuildListRowsAsync(repoconfig, allowlistStatus, cancellationToken);

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
        private async Task<List<(string Status, string Name, string Version, string Events, string Allowlist, int Pending)>> BuildListRowsAsync(AdrPlusRepoConfig repoconfig, string allowlistStatus, CancellationToken cancellationToken)
        {
            var rows = new List<(string Status, string Name, string Version, string Events, string Allowlist, int Pending)>();
            var activeNames = new HashSet<string>(repoconfig.ActivePlugins, StringComparer.OrdinalIgnoreCase);
            var loadedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var disabledLabel = string.Format(null, FormatMessages.PluginsStatusDisabled);

            foreach (var plugin in _pluginManager.LoadedPlugins)
            {
                loadedNames.Add(plugin.Manifest.Name!);
                var pending = await PendingStateStore.ReadAllAsync(_fileSystem, plugin.FolderPath, cancellationToken);
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
            await _pluginManager.LoadPluginsAsync(Path.Combine(targetPath, "plugins"), cancellationToken);

            var loadedNames = _pluginManager.LoadedPlugins.Select(plugin => plugin.Manifest.Name!).ToArray();
            var currentlyActive = new HashSet<string>(repoconfig.ActivePlugins, StringComparer.OrdinalIgnoreCase);

            var selection = _prompt.PromptSelectActivePlugins(loadedNames, currentlyActive, cancellationToken);
            if (selection.IsAborted)
            {
                throw new OperationCanceledException(Resources.AdrPlus.CancelledByUser);
            }

            await ActivePluginsWriter.WriteAsync(_fileSystem, configPath, selection.SelectedNames, cancellationToken);

            var message = string.Format(null, FormatMessages.PluginsActiveUpdated, selection.SelectedNames.Length == 0 ? "-" : string.Join(", ", selection.SelectedNames));
            LogMessages.LogCommandSuccessful(_logger, message);
            _prompt.PromptWriteSuccess(message);
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
        /// Interactive <c>adrplus plugins --wizard</c>: resolves the repository path and picks <c>list</c>,
        /// <c>validate</c>, or <c>manage</c> mode via prompts. For <c>list</c>/<c>validate</c>, returns the same
        /// <see cref="Dictionary{Arguments, String}"/> shape <c>ParseArgs</c> would have produced for the
        /// equivalent non-interactive flags — the rest of <see cref="ExecuteAsync"/> runs unchanged from there
        /// (only its final rendering step is aware of <c>--wizard</c>, to show a table instead of plain text).
        /// <c>manage</c> instead runs <see cref="RunManageActivePluginsAsync"/> to completion here and returns
        /// <see langword="null"/>, signalling <see cref="ExecuteAsync"/> that there's nothing left to do.
        /// </summary>
        /// <exception cref="OperationCanceledException">Thrown when the user cancels any prompt.</exception>
        private async Task<Dictionary<Arguments, string>?> PluginsWizard(CancellationToken cancellationToken)
        {
            string[] drives = _fileSystem.GetDrives();
            var rootPath = drives[0];
            if (drives.Length > 1)
            {
                var (IsAborted, Content) = _prompt.PromptSelectLogicalDrive(Resources.AdrPlus.NewAdrPromptSelectDrive, _fileSystem, cancellationToken);
                if (IsAborted)
                {
                    throw new OperationCanceledException(Resources.AdrPlus.CancelledByUser);
                }
                rootPath = Content;
            }

            var folderPrompt = _prompt.PromptSelectFolderPath(Resources.AdrPlus.PromptSelectRepositoryPath, true, rootPath, _fileSystem, _validateConfig, cancellationToken);
            if (folderPrompt.IsAborted)
            {
                throw new OperationCanceledException(Resources.AdrPlus.CancelledByUser);
            }

            var modePrompt = _prompt.PromptSelectPluginsMode(cancellationToken);
            if (modePrompt.IsAborted)
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
    }
}
