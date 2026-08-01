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
    /// <param name="config">The application configuration, providing the optional plugin allowlist.</param>
    /// <param name="pluginManager">The plugin manager used to discover and validate plugins.</param>
    internal sealed class PluginsCommandHandler(
        ILogger<PluginsCommandHandler> logger,
        IFileSystemService fileSystem,
        IConsoleWriter prompt,
        IAdrServices adrServices,
        IOptions<AdrPlusConfig> config,
        IPluginManager pluginManager) : ICommandHandler
    {
        private readonly ILogger<PluginsCommandHandler> _logger = logger;
        private readonly IFileSystemService _fileSystem = fileSystem;
        private readonly IConsoleWriter _prompt = prompt;
        private readonly IAdrServices _adrServices = adrServices;
        private readonly AdrPlusConfig _config = config.Value;
        private readonly IPluginManager _pluginManager = pluginManager;

        private static readonly Arguments[] ValidCommandArgs =
            [Arguments.TargetRepo,
             Arguments.PluginsList,
             Arguments.PluginsValidate,
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
                            "adrplus plugins --list --path \"path/to/repository/\"",
                            "adrplus plugins --validate --path \"path/to/repository/\"",
                        ]));
                    return;
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
                    await ReportListAsync(cancellationToken);
                }
                else
                {
                    ReportValidate();
                }
            }
            catch (Exception ex)
            {
                LogMessages.LogCommandException(_logger, ex);
                throw;
            }
        }

        private async Task ReportListAsync(CancellationToken cancellationToken)
        {
            var loaded = _pluginManager.LoadedPlugins;
            if (loaded.Count == 0)
            {
                _prompt.PromptWriteInfo(string.Format(null, FormatMessages.PluginsListEmpty));
            }

            var allowlistStatus = _config.PluginAllowlist is null
                ? string.Format(null, FormatMessages.PluginsNoAllowlistConfigured)
                : string.Format(null, FormatMessages.PluginsAllowlisted);

            foreach (var plugin in loaded)
            {
                var pending = await PendingStateStore.ReadAllAsync(_fileSystem, plugin.FolderPath, cancellationToken);
                var events = string.Join(", ", plugin.Manifest.SubscribedEvents ?? []);
                var message = string.Format(
                    null,
                    FormatMessages.PluginsListEntry,
                    plugin.Manifest.Name,
                    plugin.Manifest.Version,
                    events,
                    allowlistStatus,
                    pending.Count);
                _prompt.PromptWriteInfo(message);
            }

            var summary = string.Format(null, FormatMessages.PluginsListSummary, loaded.Count, _pluginManager.Rejections.Count);
            LogMessages.LogCommandSuccessful(_logger, summary);
            _prompt.PromptWriteSuccess(summary);
        }

        private void ReportValidate()
        {
            var loaded = _pluginManager.LoadedPlugins;
            var rejections = _pluginManager.Rejections;

            if (loaded.Count == 0 && rejections.Count == 0)
            {
                _prompt.PromptWriteInfo(string.Format(null, FormatMessages.PluginsValidateEmpty));
            }

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

            var summary = string.Format(null, FormatMessages.PluginsValidateSummary, loaded.Count, rejections.Count);
            LogMessages.LogCommandSuccessful(_logger, summary);
            _prompt.PromptWriteSuccess(summary);
        }
    }
}
