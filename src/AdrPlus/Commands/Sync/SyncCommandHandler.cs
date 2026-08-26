// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the AdrPlus project under MIT license
// ***************************************************************************************

using AdrPlus.Abstractions;
using AdrPlus.Abstractions.Domain;
using AdrPlus.Core;
using AdrPlus.Domain;
using AdrPlus.Extensions;
using AdrPlus.Infrastructure.FileSystem;
using AdrPlus.Infrastructure.Formatting;
using AdrPlus.Infrastructure.Logging;
using AdrPlus.Infrastructure.UI;
using AdrPlus.Plugins;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace AdrPlus.Commands.Sync
{
    /// <summary>
    /// Handles the <c>sync</c> command. Default mode re-attempts every plugin's pending lifecycle events queued
    /// in <c>./plugins-state/&lt;name&gt;/pending.json</c>. <c>--backfill</c> instead sweeps every ADR in the
    /// repo and re-emits the event matching its current settled status — scriptable/cron-friendly for the
    /// default mode, manual-only for <c>--backfill</c> (never self-limiting).
    /// </summary>
    /// <param name="logger">The logger for recording command execution and errors.</param>
    /// <param name="fileSystem">The file system service for I/O operations.</param>
    /// <param name="validateConfig">The service for validating and loading JSON configuration files.</param>
    /// <param name="prompt">The console writer for displaying output.</param>
    /// <param name="adrServices">The ADR services for argument parsing and resolving ADRs by their current file state.</param>
    /// <param name="pluginManager">The plugin manager used to load plugins and re-drive their pending entries.</param>
    internal sealed class SyncCommandHandler(
        ILogger<SyncCommandHandler> logger,
        IFileSystemService fileSystem,
        IValidateConfig validateConfig,
        IConsoleWriter prompt,
        IAdrServices adrServices,
        IPluginManager pluginManager) : ICommandHandler
    {
        private readonly ILogger<SyncCommandHandler> _logger = logger;
        private readonly IFileSystemService _fileSystem = fileSystem;
        private readonly IValidateConfig _validateConfig = validateConfig;
        private readonly IConsoleWriter _prompt = prompt;
        private readonly IAdrServices _adrServices = adrServices;
        private readonly IPluginManager _pluginManager = pluginManager;
        private static readonly Arguments[] ValidCommandArgs =
            [Arguments.TargetRepo,
             Arguments.Backfill,
             Arguments.WizardSync,
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
                        "sync",
                        ValidCommandArgs,
                        [
                            "adrplus sync --wizard",
                            "adrplus sync --path \"path/to/repository/\"",
                            "adrplus sync --path \"path/to/repository/\" --backfill",
                        ]));
                    return;
                }

                if (parsedArgs.ContainsKey(Arguments.WizardSync))
                {
                    var wizardResult = await SyncWizard(cancellationToken);
                    if (wizardResult is null)
                    {
                        return;
                    }
                    parsedArgs = wizardResult;
                }

                parsedArgs.TryGetValue(Arguments.TargetRepo, out var targetPath);
                targetPath ??= string.Empty;

                if (!_fileSystem.DirectoryExists(targetPath))
                {
                    throw new DirectoryNotFoundException(string.Format(null, FormatMessages.ErrDirectoryNotFound, targetPath));
                }

                var configPath = Path.GetFullPath(Path.Combine(targetPath, _validateConfig.GetFileNameRepoConfig()));
                if (!_fileSystem.FileExists(configPath))
                {
                    throw new FileNotFoundException(string.Format(null, FormatMessages.ErrFileNotFound, configPath));
                }

                string jsonString = await _fileSystem.ReadAllTextAsync(configPath, cancellationToken);
                var (IsValid, ErrorReport) = _validateConfig.ValidateRepoStructure(jsonString);
                if (!IsValid)
                {
                    LogAndWriteErrors(ErrorReport);
                    throw new InvalidDataException(string.Format(null, FormatMessages.ErrInvalidRepositoryConfig, configPath));
                }
                var repoconfig = JsonSerializer.Deserialize<AdrPlusRepoConfig>(jsonString, AppConstants.RepoSerializerOptions)!;

                await _pluginManager.LoadPluginsAsync(cancellationToken);
                var (isActive, missingNames) = PluginActivationGate.Resolve(_pluginManager, repoconfig);

                if (parsedArgs.ContainsKey(Arguments.Backfill))
                {
                    await ExecuteBackfillAsync(targetPath, repoconfig, isActive, missingNames, cancellationToken);
                    return;
                }

                await ExecuteDefaultSyncAsync(targetPath, repoconfig, isActive, missingNames, cancellationToken);
            }
            catch (Exception ex)
            {
                LogMessages.LogCommandException(_logger, ex);
                throw;
            }
        }

        private async Task ExecuteDefaultSyncAsync(string targetPath, AdrPlusRepoConfig repoconfig, Func<LoadedPlugin, bool> isActive, IReadOnlyList<string> missingNames, CancellationToken cancellationToken)
        {
            // Every valid ADR in the repo, keyed by the same adrKey format pending.json entries use — a
            // pending entry may reference an ADR that's since been deleted/renamed, which resolveAdr below
            // reports as "not found" rather than treating it as fatal.
            var adrFiles = await _adrServices.ReadAllAdr(_fileSystem, targetPath, repoconfig, includeNotMatched: false);
            var adrsByKey = new Dictionary<string, AdrFileNameComponents>(StringComparer.Ordinal);
            foreach (var file in adrFiles)
            {
                if (file.IsValid)
                {
                    adrsByKey[AdrKeyFormatter.Format(file.Number, file.Version, file.Revision)] = file;
                }
            }

            (AdrRecordSnapshot Adr, string FilePath, string Content)? ResolveAdr(string adrKey)
            {
                if (!adrsByKey.TryGetValue(adrKey, out var file))
                {
                    return null;
                }
                var record = Helper.CreateAdrRecord(file, repoconfig);
                return (record.ToSnapshot(), file.FileName, file.ContentAdr ?? string.Empty);
            }

            var summary = await _pluginManager.RetryPendingAsync(ResolveAdr, repoconfig.ToSnapshot(), Path.Combine(targetPath, "plugins-state"), isActive: isActive, cancellationToken: cancellationToken);

            var message = string.Format(null, FormatMessages.SyncSummaryReport, summary.Succeeded, summary.Skipped, summary.StillPending, summary.PermanentlyFailed, summary.Dropped);
            PluginActivationGate.WarnMissingActivePlugins(_logger, _prompt, missingNames);
            LogMessages.LogCommandSuccessful(_logger, message);
            _prompt.PromptWriteSuccess(message);
        }

        private async Task ExecuteBackfillAsync(string targetPath, AdrPlusRepoConfig repoconfig, Func<LoadedPlugin, bool> isActive, IReadOnlyList<string> missingNames, CancellationToken cancellationToken)
        {
            // Backfill is the expensive path (full retryPolicy per settled ADR) — skip reading the whole repo's
            // ADRs entirely when nothing is loaded to receive them (plugins are already loaded by ExecuteAsync).
            var summary = new SyncSummary();
            if (_pluginManager.LoadedPlugins.Count > 0)
            {
                var adrFiles = await _adrServices.ReadAllAdr(_fileSystem, targetPath, repoconfig, includeNotMatched: false);
                var settledItems = new List<(AdrEventType EventType, AdrRecordSnapshot Adr, string FilePath, Func<string> GetContent)>();

                foreach (var file in adrFiles)
                {
                    if (!file.IsValid || !(file.Header.IsValid || file.Header.IsMigrated))
                    {
                        continue;
                    }

                    var eventType = DetermineSettledEventType(file.Header);
                    if (eventType is null)
                    {
                        continue;
                    }

                    var record = Helper.CreateAdrRecord(file, repoconfig);
                    settledItems.Add((eventType.Value, record.ToSnapshot(), file.FileName, () => file.ContentAdr ?? string.Empty));
                }

                summary = await _pluginManager.BackfillAsync(settledItems, repoconfig.ToSnapshot(), isActive: isActive, cancellationToken: cancellationToken);
            }

            // BackfillAsync deliberately swallows a mid-sweep cancellation per-plugin and returns whatever
            // partial summary it already accumulated (so a long sweep doesn't lose everything to one Ctrl+C) —
            // but that means a caller who never re-checks the token would report the partial summary as if it
            // were a normal, complete run. Unlike approve/reject/etc. (where plugin dispatch is secondary to a
            // primary local file operation), the sweep itself *is* the entire point of `--backfill`, so silently
            // treating an interrupted sweep as success would misrepresent what actually happened.
            cancellationToken.ThrowIfCancellationRequested();

            var message = string.Format(null, FormatMessages.BackfillSummaryReport, summary.Succeeded, summary.Skipped, summary.PermanentlyFailed, summary.Exhausted);
            PluginActivationGate.WarnMissingActivePlugins(_logger, _prompt, missingNames);
            LogMessages.LogCommandSuccessful(_logger, message);
            _prompt.PromptWriteSuccess(message);
        }

        /// <summary>
        /// Determines the <see cref="AdrEventType"/> matching an ADR's current settled status:
        /// <see cref="AdrEventType.Superseded"/> &gt; <see cref="AdrEventType.Rejected"/> &gt;
        /// <see cref="AdrEventType.Approved"/> &gt; <see cref="AdrEventType.Migrated"/>, in that priority order —
        /// or <see langword="null"/> when the ADR is still <c>Proposed</c> (nothing settled to replay).
        /// </summary>
        private static AdrEventType? DetermineSettledEventType(AdrHeader header)
        {
            if (header.StatusChange == AdrPlus.Domain.AdrStatus.Superseded)
            {
                return AdrEventType.Superseded;
            }
            if (header.StatusUpdate == AdrPlus.Domain.AdrStatus.Rejected)
            {
                return AdrEventType.Rejected;
            }
            if (header.StatusUpdate == AdrPlus.Domain.AdrStatus.Accepted)
            {
                return AdrEventType.Approved;
            }
            if (header.IsMigrated)
            {
                return AdrEventType.Migrated;
            }
            return null;
        }

        /// <summary>
        /// Interactive <c>adrplus sync --wizard</c>: resolves the repository path and sync mode via prompts,
        /// then returns the same <see cref="Dictionary{Arguments, String}"/> shape <c>ParseArgs</c> would have
        /// produced for the equivalent non-interactive flags — the rest of <see cref="ExecuteAsync"/> runs
        /// unchanged from there. Mirrors the loop-until-confirmed pattern used by every other wizard in the app
        /// (e.g. <c>ApproveCommandHandler.ApproveAdrWizard</c>). Selecting <c>Back</c> at the mode prompt instead
        /// returns <see langword="null"/>, signalling <see cref="ExecuteAsync"/> that there's nothing left to do.
        /// </summary>
        /// <exception cref="OperationCanceledException">Thrown when the user cancels any prompt.</exception>
        private async Task<Dictionary<Arguments, string>?> SyncWizard(CancellationToken cancellationToken)
        {
            var parsedArgs = new Dictionary<Arguments, string>();

            while (true)
            {
                parsedArgs.Clear();

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
                parsedArgs[Arguments.TargetRepo] = folderPrompt.Content;

                var modePrompt = _prompt.PromptSelectSyncMode(cancellationToken);
                if (modePrompt.IsAborted)
                {
                    throw new OperationCanceledException(Resources.AdrPlus.CancelledByUser);
                }

                if (modePrompt.Mode == SyncWizardMode.Back)
                {
                    Helper.SkipWizardContinuePrompt = true;
                    return null;
                }

                if (modePrompt.Mode == SyncWizardMode.Backfill)
                {
                    // Extra safety gate that only the wizard can add: --backfill must never be automated, and
                    // automation is exactly what a TTY-only wizard can't do — this is the one place in the
                    // whole flow where the wizard is stricter than the direct flag path.
                    _prompt.PromptWriteInfo(Resources.AdrPlus.HelpUsageBackfill);
                    var confirmBackfill = _prompt.PromptConfirm(Resources.AdrPlus.WizardConfirmBackfill, cancellationToken);
                    if (confirmBackfill.IsAborted)
                    {
                        throw new OperationCanceledException(Resources.AdrPlus.CancelledByUser);
                    }
                    if (!confirmBackfill.ConfirmYes)
                    {
                        continue;
                    }
                    parsedArgs[Arguments.Backfill] = string.Empty;
                }

                return parsedArgs;
            }
        }

        private void LogAndWriteErrors(string[] errors)
        {
            foreach (var error in errors)
            {
                LogMessages.LogCommandFailure(_logger, error);
                _prompt.PromptWriteError(error);
            }
        }
    }
}
