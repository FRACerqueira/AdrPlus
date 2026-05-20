// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the AdrPlus project under MIT license
// ***************************************************************************************

using AdrPlus.Core;
// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the AdrPlus project under MIT license
// ***************************************************************************************

using AdrPlus.Domain;
using AdrPlus.Infrastructure.FileSystem;
using AdrPlus.Infrastructure.Formatting;
using AdrPlus.Infrastructure.Logging;
using AdrPlus.Infrastructure.UI;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace AdrPlus.Commands.Migrate
{
    /// <summary>
    /// Handles the <c>migrate</c> command, which migrates existing ADR files in a repository to the new format 
    /// </summary>
    /// <param name="logger">The logger for recording command execution and errors.</param>
    /// <param name="config">The application configuration settings (folder, language, etc.).</param>
    /// <param name="fileSystem">The file system service for I/O operations.</param>
    /// <param name="validateConfig">The service for validating and loading JSON configuration files.</param>
    /// <param name="prompt">The console writer for displaying output and prompting user input.</param>
    /// <param name="adrServices">The ADR services for argument parsing and configuration deserialization.</param>
    internal sealed class MigrateCommandHandler(
        ILogger<MigrateCommandHandler> logger,
        IFileSystemService fileSystem,
        IValidateJsonConfig validateConfig,
        IPromptConsole prompt,
        IAdrServices adrServices) : ICommandHandler
    {
        private readonly ILogger<MigrateCommandHandler> _logger = logger;
        private readonly IFileSystemService _fileSystem = fileSystem;
        private readonly IValidateJsonConfig _validateConfig = validateConfig;
        private readonly IPromptConsole _prompt = prompt;
        private readonly IAdrServices _adrServices = adrServices;
        private static readonly Arguments[] ValidCommandArgs =
            [Arguments.WizardMigrate,
             Arguments.TargetRepo,
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
                        "migrate",
                        ValidCommandArgs,
                        [
                            "adrplus migrate --wizard",
                            "adrplus migrate --path \"path/to/repository/\"",
                        ]));
                    return;
                }

                var hasWizard = parsedArgs.ContainsKey(Arguments.WizardMigrate);
                AdrFileNameComponents[] foundfiles = [];
                if (hasWizard)
                {
                    var (ArgsWizard, Adrfiles) = await MigrateWizardAsync(cancellationToken);
                    parsedArgs = ArgsWizard;
                    foundfiles = Adrfiles;
                }

                parsedArgs.TryGetValue(Arguments.TargetRepo, out var targetPath);
                targetPath ??= string.Empty;

                if (!_fileSystem.DirectoryExists(targetPath))
                {
                    throw new DirectoryNotFoundException(string.Format(null, FormatMessages.ErrDirectoryNotFound, targetPath));
                }

                if (!_validateConfig.HasTemplateRepoFile())
                {
                    throw new FileNotFoundException(Resources.AdrPlus.ErrMsgTemplateRepoFileNotFound);
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
                var hasChanges = false;
                if (repoconfig.MigrationPattern.Length == 0)
                {
                    repoconfig.MigrationPattern = await _validateConfig.LoadPatternsConfigMigration(cancellationToken);
                    if (repoconfig.MigrationPattern.Length > 0)
                    {
                         hasChanges = true;
                    }
                }

                if (!hasWizard)
                {
                    foundfiles = await _adrServices.ReadAllAdr(_fileSystem, targetPath, repoconfig, true);
                    if (foundfiles.Length == 0)
                    {
                        throw new InvalidDataException(Resources.AdrPlus.NotFoundADR);
                    }
                    if (foundfiles.Any(x => x.IsValid && x.Header.IsValid && !x.Header.IsMigrated))
                    {
                        throw new InvalidDataException(Resources.AdrPlus.AlredyAdrPlusCreated);
                    }
                    if (!foundfiles.Any(x => x.IsValid))
                    {
                        throw new InvalidDataException(Resources.AdrPlus.NotFoundValidMigrateADR);
                    }
                }

                var result = await MigrateRepositoryAsync(foundfiles, repoconfig, cancellationToken);
                foreach (var item in result)
                {
                    LogMessages.LogCommandSuccessful(_logger, item);
                    _prompt.PromptWriteSuccess(item);
                }
                if (hasChanges)
                {
                    var jsonStringNew = JsonSerializer.Serialize(repoconfig, AppConstants.RepoSerializerOptions);
                    await _fileSystem.WriteAllTextAsync(configPath, jsonStringNew, cancellationToken);
                    LogMessages.LogCommandSuccessful(_logger, configPath);
                    _prompt.PromptWriteSuccess(configPath);
                }
            }
            catch (Exception ex)
            {
                LogMessages.LogCommandException(_logger, ex);
                throw;
            }
        }

        private async Task<IEnumerable<string>> MigrateRepositoryAsync(AdrFileNameComponents[] adrfiles, AdrPlusRepoConfig repoConfig, CancellationToken cancellationToken)
        {
            var result = new List<string>();    
            foreach (var file in adrfiles)
            {
                if (!file.IsValid)
                {
                    continue;
                }
                if (file.Header.StatusCreate == AdrStatus.Unknown && !file.Header.IsMigrated && !file.Header.IsValid)
                {
                    // Create ADR record and file
                    var content = await _fileSystem.ReadAllTextAsync(file.FileName, cancellationToken);
                    var adrRecord = new AdrRecord
                    {
                        Number = file.Number,
                        Title = file.Title.Trim(),
                        Scope = string.Empty,
                        Domain = string.Empty,
                        StatusCreate = AdrStatus.Unknown,
                        StatusChange = AdrStatus.Unknown,
                        StatusUpdate = AdrStatus.Unknown,
                        CreateRef = null,
                        ChangeRef = null,
                        UpdateRef = null,
                        Superseded = null,
                        Version = 0,
                        Revision = null,
                        Template = string.Empty
                    };
                    var newcontent = $"{adrRecord.GetHeader(repoConfig,null,true)}{content}";
                    await _fileSystem.WriteAllTextAsync(file.FileName, newcontent, cancellationToken);
                    result.Add($"{Resources.AdrPlus.Migrated} : {file.FileName}");
                }
            }
            return result;
        }

        /// <summary>
        /// Runs the interactive wizard for the <c>init</c> command, prompting the user to select a
        /// logical drive (when more than one is available) and then a target repository folder.
        /// </summary>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A dictionary of parsed arguments pre-populated with <see cref="Arguments.TargetRepo"/>.</returns>
        /// <exception cref="OperationCanceledException">Thrown when the user cancels any wizard prompt.</exception>

        private async Task<(Dictionary<Arguments, string> ArgsWizard, AdrFileNameComponents[] Adrfiles)> MigrateWizardAsync(CancellationToken cancellationToken)
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

                var folderPrompt = _prompt.PromptSelectFolderPath(Resources.AdrPlus.PromptSelectRepositoryPath, false, rootPath, _fileSystem, _validateConfig, cancellationToken);
                if (folderPrompt.IsAborted)
                {
                    throw new OperationCanceledException(Resources.AdrPlus.CancelledByUser);
                }

                parsedArgs[Arguments.TargetRepo] = folderPrompt.Content;

                var configPath = Path.GetFullPath(Path.Combine(folderPrompt.Content, _validateConfig.GetFileNameRepoConfig()));
                if (!_fileSystem.FileExists(configPath))
                {
                    throw new FileNotFoundException(string.Format(null, FormatMessages.ErrFileNotFound, configPath));
                }

                string jsonString = await _fileSystem.ReadAllTextAsync(configPath, cancellationToken);
                var (IsValid, ErrorReport) = _validateConfig.ValidateRepoStructure(jsonString);
                if (!IsValid)
                {
                    LogAndWriteErrors(ErrorReport);
                    throw new InvalidDataException(Resources.AdrPlus.ErrorInConfigFile);
                }

                var repoconfig = JsonSerializer.Deserialize<AdrPlusRepoConfig>(jsonString, AppConstants.RepoSerializerOptions)!;
                if (repoconfig.MigrationPattern.Length == 0)
                {
                    repoconfig.MigrationPattern = await _validateConfig.LoadPatternsConfigMigration(cancellationToken);
                }

                var foundfiles = await _adrServices.ReadAllAdr(_fileSystem, folderPrompt.Content, repoconfig, true);
                if (foundfiles.Length == 0)
                {
                    throw new InvalidDataException(Resources.AdrPlus.NotFoundADR);
                }
                if (foundfiles.Any(x => x.IsValid && x.Header.IsValid && !x.Header.IsMigrated))
                {
                    throw new InvalidDataException(Resources.AdrPlus.AlredyAdrPlusCreated);
                }
                if (!foundfiles.Any(x => x.IsValid))
                {
                    throw new InvalidDataException(Resources.AdrPlus.NotFoundValidMigrateADR);
                }

                var adrselectedPrompt = _prompt.PromptShowAdrsMigrations(foundfiles, repoconfig, cancellationToken);
                if (adrselectedPrompt.IsAborted)
                {
                    throw new OperationCanceledException(Resources.AdrPlus.CancelledByUser);
                }

                var resultCnf = _prompt.PromptConfirm(Resources.AdrPlus.PromptConfirmMigration, cancellationToken);
                if (resultCnf.IsAborted)
                {
                    throw new OperationCanceledException(Resources.AdrPlus.CancelledByUser);
                }
                if (resultCnf.ConfirmYes)
                {
                    return (parsedArgs, foundfiles);
                }
            }
        }

        /// <summary>
        /// Logs each error in <paramref name="errors"/> as a command failure and writes it to the console.
        /// </summary>
        /// <param name="errors">An array of validation error messages to log and display.</param>
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
