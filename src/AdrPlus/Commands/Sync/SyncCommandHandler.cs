// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the AdrPlus project under MIT license
// ***************************************************************************************

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
    /// Handles the <c>sync</c> command, which re-attempts every plugin's pending lifecycle events queued in
    /// <c>./plugins/&lt;name&gt;/state/pending.json</c> (spec §6/§7, Fase 5's default mode). Scriptable/cron-friendly
    /// — no wizard, no <c>--backfill</c> (full repo sweep, Fase 6).
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
                            "adrplus sync --path \"path/to/repository/\"",
                        ]));
                    return;
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

                await _pluginManager.LoadPluginsAsync(Path.Combine(targetPath, "plugins"), cancellationToken);
                var summary = await _pluginManager.RetryPendingAsync(ResolveAdr, repoconfig.ToSnapshot(), cancellationToken);

                var message = string.Format(null, FormatMessages.SyncSummaryReport, summary.Succeeded, summary.Skipped, summary.StillPending, summary.PermanentlyFailed, summary.Dropped);
                LogMessages.LogCommandSuccessful(_logger, message);
                _prompt.PromptWriteSuccess(message);
            }
            catch (Exception ex)
            {
                LogMessages.LogCommandException(_logger, ex);
                throw;
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
