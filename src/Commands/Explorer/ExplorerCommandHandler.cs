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
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Globalization;
using System.Text;
using System.Text.Json;

namespace AdrPlus.Commands.Explorer
{
    /// <summary>
    /// Handles the <c>explorer</c> command, which allows users to interactively explore their ADR repository, 
    /// </summary>
    /// <param name="logger">The logger for recording command execution and errors.</param>
    /// <param name="config">The application configuration settings (folder, language, etc.).</param>
    /// <param name="fileSystem">The file system service for I/O operations.</param>
    /// <param name="validateConfig">The service for validating and loading JSON configuration files.</param>
    /// <param name="prompt">The console writer for displaying output and prompting user input.</param>
    /// <param name="adrServices">The ADR services for argument parsing and configuration deserialization.</param>
    internal sealed class ExplorerCommandHandler(
        ILogger<ExplorerCommandHandler> logger,
        IOptions<AdrPlusConfig> config,
        IFileSystemService fileSystem,
        IValidateJsonConfig validateConfig,
        IPromptConsole prompt,
        IAdrServices adrServices) : ICommandHandler
    {
        private readonly ILogger<ExplorerCommandHandler> _logger = logger;
        private readonly IOptions<AdrPlusConfig> _config = config;
        private readonly IFileSystemService _fileSystem = fileSystem;
        private readonly IValidateJsonConfig _validateConfig = validateConfig;
        private readonly IPromptConsole _prompt = prompt;
        private readonly IAdrServices _adrServices = adrServices;
        private static readonly Arguments[] ValidCommandArgs =
            [Arguments.WizardExplorer,
             Arguments.TargetRepo,
             Arguments.FileReport,
             Arguments.OpenFile,
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
                        "explorer",
                        ValidCommandArgs,
                        [
                            "adrplus explorer --wizard",
                            "adrplus explorer --path \"path/to/repository/\" --file \"path/to/report.md\" --open",
                        ]));
                    return;
                }

                if (!_validateConfig.HasTemplateRepoFile())
                {
                    throw new FileNotFoundException(Resources.AdrPlus.ErrMsgTemplateRepoFileNotFound);
                }
                string[] fields = Resources.AdrPlus.ListFieldReport.Split(',');

                var hasWizard = parsedArgs.ContainsKey(Arguments.WizardExplorer);
                if (hasWizard)
                {
                    var (wizardArgs, wizardFields) = await ExplorerWizardAsync(cancellationToken);
                    parsedArgs = wizardArgs;
                    fields = wizardFields;
                }

                parsedArgs.TryGetValue(Arguments.TargetRepo, out var targetPath);
                targetPath ??= string.Empty;

                if (!_fileSystem.DirectoryExists(targetPath))
                {
                    throw new DirectoryNotFoundException(string.Format(null, FormatMessages.ExceptionDirectoryNotFound, targetPath));
                }

                var configPath = Path.GetFullPath(Path.Combine(targetPath, _validateConfig.GetFileNameRepoConfig()));
                if (!_fileSystem.FileExists(configPath))
                {
                    throw new FileNotFoundException(string.Format(null, FormatMessages.ExceptionFileNotFound, configPath));
                }

                string jsonString = await _fileSystem.ReadAllTextAsync(configPath, cancellationToken);
                var (IsValid, ErrorReport) = _validateConfig.ValidateRepoStructure(jsonString);
                if (!IsValid)
                {
                    LogAndWriteErrors(ErrorReport);
                    throw new InvalidDataException(Resources.AdrPlus.ErrorInConfigFile);
                }

                var repoconfig = JsonSerializer.Deserialize<AdrPlusRepoConfig>(jsonString, AppConstants.RepoSerializerOptions)!;

                parsedArgs.TryGetValue(Arguments.FileReport, out var targetreport);
                targetreport ??= string.Empty;

                var foundfiles = await _adrServices.ReadAllAdr(_fileSystem, targetPath, repoconfig, true);

                if (foundfiles.Length == 0)
                {
                    throw new InvalidDataException(Resources.AdrPlus.NotFoundADR);
                }
                var file = string.Empty;
                if (hasWizard && targetreport.Length == 0)
                {   
                    var folderadr = Path.GetFullPath(Path.Combine(targetPath, repoconfig.FolderAdr));
                    file = ShowSelectExplorerAdr(foundfiles, fields, folderadr, repoconfig);
                }
                else
                {
                    file = await CreateFileAdrReport(foundfiles, fields, targetreport, targetPath, repoconfig, cancellationToken);
                    LogMessages.LogCommandSuccessful(_logger, file);
                    _prompt.PromptWriteSuccess(file);
                }

                // Open file if requested
                OpenFileIfRequested(parsedArgs, file);
            }
            catch (Exception ex)
            {
                LogMessages.LogCommandException(_logger, ex);
                throw;
            }
        }

        /// <summary>
        /// Opens the file in the configured external editor when the <c>--open</c> argument was provided
        /// and <see cref="AdrPlusConfig.ComandOpenAdr"/> is non-empty.
        /// </summary>
        /// <param name="parsedArgs">The parsed command arguments used to check for the open flag.</param>
        /// <param name="filePath">The fully qualified path of the ADR file to open.</param>
        private void OpenFileIfRequested(Dictionary<Arguments, string> parsedArgs, string filePath)
        {
            if (!parsedArgs.ContainsKey(Arguments.OpenFile))
            {
                return;
            }

            var commandFormat = CompositeFormat.Parse(_config.Value.ComandOpenAdr.Trim());
            var command = string.Format(null, commandFormat, filePath);
            var result = _adrServices.OpenFile(filePath, command);

            if (string.IsNullOrEmpty(result))
            {
                var msg = string.Format(null, CompositeFormat.Parse(Resources.AdrPlus.SuccessExternalCommand), command);
                LogMessages.LogCommandSuccessful(_logger, msg);
                _prompt.PromptWriteSuccess(msg);
            }
            else
            {
                var msg = string.Format(null, CompositeFormat.Parse(Resources.AdrPlus.ErrorExternalCommand), result);
                LogAndWriteErrors([msg]);
            }
        }

        private async Task<string> CreateFileAdrReport(AdrFileNameComponents[] foundfiles,string[] fields, string targetreport, string targetPath, AdrPlusRepoConfig repoconfig, CancellationToken cancellationToken)
        {
            if (targetreport.Length == 0)
            {
                throw new InvalidDataException(Resources.AdrPlus.ErrFileReportEmpty);
            }
            var folderadr = Path.GetFullPath(Path.Combine(targetPath, repoconfig.FolderAdr));
            var dirreport = Path.GetDirectoryName(targetreport)!;
            if (!_fileSystem.DirectoryExists(dirreport))
            {
                throw new DirectoryNotFoundException(string.Format(null, FormatMessages.ExceptionDirectoryNotFound, dirreport));
            }
            var report = new StringBuilder();
            report.AppendLine(null,$"# {Resources.AdrPlus.ReportTitle}");
            report.AppendLine(null,$"## {DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)} : {repoconfig.FolderAdr}");
            report.AppendLine("---");
            //1)File,
            //2)Current Status,
            //3)Folder,
            //4)Format,
            //5)Prefix,
            //6)Version,
            //7)Revision,
            //8)Status Created,
            //9)Status Updated,
            //10)Scope,
            //11)Domain
            report.Append(null,$"|{Resources.AdrPlus.File}");
            report.Append(null, $"|{Resources.AdrPlus.CurrentStatus}");
            if (fields.Any(x => x.StartsWith("3)", false, CultureInfo.InvariantCulture)))
            {
                report.Append(null,$"|{Resources.AdrPlus.Folder}");
            }
            if (fields.Any(x => x.StartsWith("4)", false, CultureInfo.InvariantCulture)))
            {
                report.Append(null,$"|{Resources.AdrPlus.Format}");
            }
            if (fields.Any(x => x.StartsWith("5)", false, CultureInfo.InvariantCulture)))
            {
                report.Append(null, $"|{Resources.AdrPlus.Prefix}");
            }
            if (fields.Any(x => x.StartsWith("6)", false, CultureInfo.InvariantCulture)))
            {
                report.Append(null, $"|{Resources.AdrPlus.Version}");
            }
            if (fields.Any(x => x.StartsWith("7)", false, CultureInfo.InvariantCulture)))
            {
                report.Append(null, $"|{Resources.AdrPlus.Revision}");
            }
            if (fields.Any(x => x.StartsWith("8)", false, CultureInfo.InvariantCulture)))
            {
                report.Append(null,$"|{Resources.AdrPlus.StatusCreated}");
            }
            if (fields.Any(x => x.StartsWith("9)", false, CultureInfo.InvariantCulture)))
            {
                report.Append(null,$"|{Resources.AdrPlus.StatusUpdated}");
            }
            if (fields.Any(x => x.StartsWith("10)", false, CultureInfo.InvariantCulture)))
            {
                report.Append(null,$"|{Resources.AdrPlus.Scope}");
            }
            if (fields.Any(x => x.StartsWith("11)", false, CultureInfo.InvariantCulture)))
            {
                report.Append(null,$"|{Resources.AdrPlus.Domain}");
            }
            report.AppendLine("|");

            //1)File,
            //2)Current Status,
            //3)Folder,
            //4)Format,
            //5)Prefix,
            //6)Version,
            //7)Revision,
            //8)Status Created,
            //9)Status Updated,
            //10)Scope,
            //11)Domain
            
            report.Append("|---");
            report.Append("|---");

            if (fields.Any(x => x.StartsWith("3)", false, CultureInfo.InvariantCulture)))
            {
                report.Append("|---");
            }
            if (fields.Any(x => x.StartsWith("4)", false, CultureInfo.InvariantCulture)))
            {
                report.Append("|---");
            }
            report.Append("|---");
            if (fields.Any(x => x.StartsWith("5)", false, CultureInfo.InvariantCulture)))
            {
                report.Append("|---");
            }
            if (fields.Any(x => x.StartsWith("6)", false, CultureInfo.InvariantCulture)))
            {
                report.Append("|---");
            }
            if (fields.Any(x => x.StartsWith("7)", false, CultureInfo.InvariantCulture)))
            {
                report.Append("|---");
            }
            if (fields.Any(x => x.StartsWith("8)", false, CultureInfo.InvariantCulture)))
            {
                report.Append("|---");
            }
            if (fields.Any(x => x.StartsWith("9)", false, CultureInfo.InvariantCulture)))
            {
                report.Append("|---");
            }
            if (fields.Any(x => x.StartsWith("10)", false, CultureInfo.InvariantCulture)))
            {
                report.Append("|---");
            }
            if (fields.Any(x => x.StartsWith("11)", false, CultureInfo.InvariantCulture)))
            {
                report.Append("|---");
            }
            report.AppendLine("|");


            foreach (var field in foundfiles)
            {
                //1)File,
                //2)Current Status,
                //3)Folder,
                //4)Format,
                //5)Prefix,
                //6)Version,
                //7)Revision,
                //8)Status Created,
                //9)Status Updated,
                //10)Scope,
                //11)Domain
                report.Append('|');
                report.Append(Path.GetFileName(field.FileName));
                report.Append('|');
                report.Append(Helper.FmtStatus(field, repoconfig));
                if (fields.Any(x => x.StartsWith("3)", false, CultureInfo.InvariantCulture)))
                {
                    report.Append('|');
                    report.Append(Helper.FmtFolder(field, folderadr));
                }
                if (fields.Any(x => x.StartsWith("4)", false, CultureInfo.InvariantCulture)))
                {
                    report.Append('|');
                    report.Append(Helper.FmtFormat(field));
                }
                if (fields.Any(x => x.StartsWith("5)", false, CultureInfo.InvariantCulture)))
                {
                    report.Append('|');
                    report.Append(field.Prefix);
                }
                if (fields.Any(x => x.StartsWith("6)", false, CultureInfo.InvariantCulture)))
                {
                    report.Append('|');
                    report.Append(field.Version);
                }
                if (fields.Any(x => x.StartsWith("7)", false, CultureInfo.InvariantCulture)))
                {
                    report.Append('|');
                    report.Append(field.Revision);
                }
                if (fields.Any(x => x.StartsWith("8)", false, CultureInfo.InvariantCulture)))
                {
                    report.Append('|');
                    report.Append(field.Header.DateCreate == null ? string.Empty : $"{field.Header.DateCreate.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}:{Helper.GetResourceStatus(field.Header.StatusCreate)}");
                }
                if (fields.Any(x => x.StartsWith("9)", false, CultureInfo.InvariantCulture)))
                {
                    report.Append('|');
                    report.Append(field.Header.DateUpdate == null ? string.Empty : $"{field.Header.DateUpdate.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}:{Helper.GetResourceStatus(field.Header.StatusUpdate)}");
                }
                if (fields.Any(x => x.StartsWith("10)", false, CultureInfo.InvariantCulture)))
                {
                    report.Append('|');
                    report.Append(field.Header.Scope);
                }
                if (fields.Any(x => x.StartsWith("11)", false, CultureInfo.InvariantCulture)))
                {
                    report.Append('|');
                    report.Append(field.Header.Domain);
                }
                report.AppendLine("|");
            }
            await _fileSystem.WriteAllTextAsync(targetreport, report.ToString(), cancellationToken);
            return targetreport;
        }

        private string ShowSelectExplorerAdr(AdrFileNameComponents[] foundfiles,string[] fields, string folderrepoadr, AdrPlusRepoConfig adrPlusRepoConfig)
        {
            (bool IsAborted, string FileSelectd) = _prompt.PromptTableExplorer(foundfiles,fields,folderrepoadr,adrPlusRepoConfig);
            if (IsAborted)
            {
                throw new OperationCanceledException(Resources.AdrPlus.CancelledByUser);
            }
            return FileSelectd;
        }

        private async Task<(Dictionary<Arguments, string> WizardArgs, string[] Fields)> ExplorerWizardAsync(CancellationToken cancellationToken)
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
                    throw new FileNotFoundException(string.Format(null, FormatMessages.ExceptionFileNotFound, configPath));
                }

                string jsonString = await _fileSystem.ReadAllTextAsync(configPath, cancellationToken);
                var (IsValid, ErrorReport) = _validateConfig.ValidateRepoStructure(jsonString);
                if (!IsValid)
                {
                    LogAndWriteErrors(ErrorReport);
                    throw new InvalidDataException(Resources.AdrPlus.ErrorInConfigFile);
                }
                var fieldsseleted = _prompt.PromptFieldsExplorer(cancellationToken);
                if (fieldsseleted.IsAborted)
                {
                    throw new OperationCanceledException(Resources.AdrPlus.CancelledByUser);
                }

                var explorerreport = _prompt.PromptOptionShowOrCreateReport(cancellationToken);
                if (explorerreport.IsAborted)
                {
                    throw new OperationCanceledException(Resources.AdrPlus.CancelledByUser);
                }
                if (explorerreport.IsCreatingReport)
                {
                    var (IsAborted, Filename) = _prompt.PromptInputFileReport(cancellationToken);
                    if (IsAborted)
                    {
                        throw new OperationCanceledException(Resources.AdrPlus.CancelledByUser);
                    }
                    var folderreport = _prompt.PromptSelectFolderPath($"{Resources.AdrPlus.PromptSelectFolderForReport}: ", false, rootPath, _fileSystem, _validateConfig, cancellationToken);
                    if (folderreport.IsAborted)
                    {
                        throw new OperationCanceledException(Resources.AdrPlus.CancelledByUser);
                    }

                    parsedArgs.Add(Arguments.FileReport, Path.GetFullPath(Path.Combine(folderreport.Content, $"{Filename}.md")));

                    if (_config.Value.ComandOpenAdr.Length > 0)
                    {
                        var openfile = _prompt.PromptConfirm($"{Resources.AdrPlus.PromptOpenReportAfterCreate}: ", cancellationToken);
                        if (openfile.IsAborted)
                        {
                            throw new OperationCanceledException(Resources.AdrPlus.CancelledByUser);
                        }
                        if (openfile.ConfirmYes)
                        {
                            parsedArgs.Add(Arguments.OpenFile, string.Empty);
                        }
                    }
                }
                else
                {
                    if (_config.Value.ComandOpenAdr.Length > 0)
                    {
                        var (IsAborted, ConfirmYes) = _prompt.PromptConfirm($"{Resources.AdrPlus.PromptOpenAdrAfterSelection}: ", cancellationToken);
                        if (IsAborted)
                        {
                            throw new OperationCanceledException(Resources.AdrPlus.CancelledByUser);
                        }
                        if (ConfirmYes)
                        {
                            parsedArgs.Add(Arguments.OpenFile, string.Empty);
                        }
                    }
                }

                // Display summary and confirm
                var (_, Top) = _prompt.PromptCursorPosition();
                DisplayWizardSummary(parsedArgs, fieldsseleted.FieldsExplorer);
                var resultCnf = _prompt.PromptConfirm(Resources.AdrPlus.PromptConfirmExplorer, cancellationToken);
                _prompt.PromptMovePosition(0, Top);
                if (resultCnf.IsAborted)
                {
                    throw new OperationCanceledException(Resources.AdrPlus.CancelledByUser);
                }

                if (resultCnf.ConfirmYes)
                {
                    return (parsedArgs, fieldsseleted.FieldsExplorer);
                }
            }
        }

        private void DisplayWizardSummary(Dictionary<Arguments, string> parsedArgs, string[] fields)
        {
            var targetreport =string.Empty;
            parsedArgs.TryGetValue(Arguments.FileReport, out targetreport);
            _prompt.PromptWriteInfo($"{Resources.AdrPlus.SelectRepo} : {parsedArgs[Arguments.TargetRepo]}");
            _prompt.PromptWriteInfo($"{Resources.AdrPlus.Fields} : {string.Join(", ", fields.Select(x => x[2..]))}");
            if ((targetreport??string.Empty).Length > 0)
            {
                _prompt.PromptWriteInfo($"{Resources.AdrPlus.FileReportSummary} : {parsedArgs[Arguments.FileReport]}");
            }
            var openfile = false;
            if (parsedArgs.TryGetValue(Arguments.OpenFile, out _))
            { 
                openfile = true;
            }

            if (openfile)
            {
                _prompt.PromptWriteInfo($"{Resources.AdrPlus.OpenFile}");
            }
            _prompt.PromptWriteInfo("");
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
