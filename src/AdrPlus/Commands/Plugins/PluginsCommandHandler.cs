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
                    parsedArgs = await PluginsWizard(cancellationToken);
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
                    await ReportListAsync(hasWizard, cancellationToken);
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
        private async Task ReportListAsync(bool useTable, CancellationToken cancellationToken)
        {
            var loaded = _pluginManager.LoadedPlugins;

            if (loaded.Count == 0)
            {
                _prompt.PromptWriteInfo(string.Format(null, FormatMessages.PluginsListEmpty));
            }
            else
            {
                var allowlistStatus = _config.PluginAllowlist is null
                    ? string.Format(null, FormatMessages.PluginsNoAllowlistConfigured)
                    : string.Format(null, FormatMessages.PluginsAllowlisted);
                var rows = await BuildListRowsAsync(allowlistStatus, cancellationToken);

                if (useTable)
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
                        var message = string.Format(null, FormatMessages.PluginsListEntry, row.Name, row.Version, row.Events, row.Allowlist, row.Pending);
                        _prompt.PromptWriteInfo(message);
                    }
                }
            }

            var summary = string.Format(null, FormatMessages.PluginsListSummary, loaded.Count, _pluginManager.Rejections.Count);
            LogMessages.LogCommandSuccessful(_logger, summary);
            _prompt.PromptWriteSuccess(summary);
        }

        private async Task<List<(string Name, string Version, string Events, string Allowlist, int Pending)>> BuildListRowsAsync(string allowlistStatus, CancellationToken cancellationToken)
        {
            var rows = new List<(string Name, string Version, string Events, string Allowlist, int Pending)>();
            foreach (var plugin in _pluginManager.LoadedPlugins)
            {
                var pending = await PendingStateStore.ReadAllAsync(_fileSystem, plugin.FolderPath, cancellationToken);
                var events = string.Join(", ", plugin.Manifest.SubscribedEvents ?? []);
                rows.Add((plugin.Manifest.Name!, plugin.Manifest.Version!, events, allowlistStatus, pending.Count));
            }
            return rows;
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
        /// Interactive <c>adrplus plugins --wizard</c>: resolves the repository path and picks <c>list</c> or
        /// <c>validate</c> mode via prompts, then returns the same <see cref="Dictionary{Arguments, String}"/>
        /// shape <c>ParseArgs</c> would have produced for the equivalent non-interactive flags — the rest of
        /// <see cref="ExecuteAsync"/> runs unchanged from there (only its final rendering step is aware of
        /// <c>--wizard</c>, to show a table instead of plain text).
        /// </summary>
        /// <exception cref="OperationCanceledException">Thrown when the user cancels any prompt.</exception>
        private async Task<Dictionary<Arguments, string>> PluginsWizard(CancellationToken cancellationToken)
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

            var parsedArgs = new Dictionary<Arguments, string>
            {
                [Arguments.TargetRepo] = folderPrompt.Content
            };
            parsedArgs[modePrompt.UseValidate ? Arguments.PluginsValidate : Arguments.PluginsList] = string.Empty;
            return parsedArgs;
        }
    }
}
