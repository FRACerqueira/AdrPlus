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

namespace AdrPlus.Commands.Supersede
{
    /// <summary>
    /// Handles the <c>supersede</c> command to create a new ADR that supersedes an existing accepted one.
    /// The superseded ADR is marked with <see cref="AdrStatus.Superseded"/> and the new ADR is created
    /// with <see cref="AdrStatus.Proposed"/> status and a back-reference to the superseded sequence number.
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of the <see cref="SupersedeCommandHandler"/> class.
    /// </remarks>
    /// <param name="logger">The logger for recording command execution and errors.</param>
    /// <param name="config">The application configuration settings (folder, language, open command, etc.).</param>
    /// <param name="fileSystem">The file system service for I/O operations.</param>
    /// <param name="validateconfig">The service for validating and loading JSON configuration files.</param>
    /// <param name="console">The console writer for displaying output and prompting user input.</param>
    /// <param name="adrServices">The ADR services for argument parsing and ADR file operations.</param>
    internal sealed class SupersedeCommandHandler(
        ILogger<SupersedeCommandHandler> logger,
        IOptions<AdrPlusConfig> config,
        IFileSystemService fileSystem,
        IValidateJsonConfig validateconfig,
        IConsoleWriter console,
        IAdrServices adrServices) : ICommandHandler
    {
        private readonly ILogger<SupersedeCommandHandler> _logger = logger;
        private readonly AdrPlusConfig _config = config.Value;
        private readonly IFileSystemService _filesystem = fileSystem;
        private readonly IConsoleWriter _console = console;
        private readonly IValidateJsonConfig _validateconfig = validateconfig;
        private readonly IAdrServices _adrServices = adrServices;
        private static readonly Arguments[] ValidCommandArgs =
            [Arguments.WizardSupersede,
             Arguments.FileAdr,
             Arguments.DateRefAdr,
             Arguments.OpenAdr,
             Arguments.Help];

        /// <summary>
        /// Determines whether an ADR is eligible to be superseded.
        /// An ADR is eligible when its update status is <see cref="AdrStatus.Accepted"/>
        /// and its change status is <see cref="AdrStatus.Unknown"/>.
        /// </summary>
        /// <param name="info">The parsed ADR filename components containing header and status information.</param>
        /// <returns><see langword="true"/> when the ADR is eligible to be superseded; otherwise <see langword="false"/>.</returns>
        private static bool SelectionCondition(AdrFileNameComponents info)
        {
            return (info.Header.StatusUpdate == AdrStatus.Accepted && info.Header.StatusChange == AdrStatus.Unknown);
        }

        /// <summary>
        /// Builds a localized error message indicating that the ADR's current status does not allow superseding.
        /// </summary>
        /// <param name="repoconfig">The repository configuration providing the status-to-string mapping.</param>
        /// <returns>A formatted error string naming the required status (<see cref="AdrStatus.Accepted"/>).</returns>
        private static string MessageNotValidStatusForUpdate(AdrPlusRepoConfig repoconfig)
        {
            return string.Format(null, FormatMessages.NotValidStatusForSupersede, $"{repoconfig.StatusMapping[AdrStatus.Accepted]}");
        }

        /// <summary>
        /// Executes the <c>supersede</c> command asynchronously to mark an ADR as superseded and
        /// create a new successor ADR. When <c>--wizard</c> is specified the user is guided interactively;
        /// otherwise the file is taken from the <c>--file</c> argument.
        /// </summary>
        /// <param name="args">The raw command-line tokens (e.g. <c>--wizard</c>, <c>--file</c>, <c>--refdate</c>, <c>--open</c>, <c>--help</c>).</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="args"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">Thrown when required arguments are missing or invalid.</exception>
        /// <exception cref="FileNotFoundException">Thrown when the ADR or configuration file is not found.</exception>
        /// <exception cref="InvalidDataException">Thrown when the ADR status is not eligible for superseding, or the config is invalid.</exception>
        /// <exception cref="FormatException">Thrown when the provided date string cannot be parsed.</exception>
        /// <exception cref="OperationCanceledException">Thrown when the user cancels the wizard.</exception>
        public async Task ExecuteAsync(string[] args, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(args);
            try
            {
                var parsedArgs = _adrServices.ParseArgs(args, ValidCommandArgs);
                if (parsedArgs.ContainsKey(Arguments.Help))
                {
                    _console.WriteHelp(_adrServices.GetHelpText(
                        "supersede",
                        ValidCommandArgs,
                        [
                            "adrplus supersede --wizard --open",
                            "adrplus supersede --file \"path/to/File-ADR\" --refdate \"2026-01-01\"",
                        ]));
                    return;
                }

                if (!_validateconfig.HasTemplateRepoFile())
                {
                    throw new FileNotFoundException(Resources.AdrPlus.ErrMsgTemplateRepoFileNotFound);
                }

                var hasWizard = parsedArgs.ContainsKey(Arguments.WizardSupersede);
                if (hasWizard)
                {
                    parsedArgs = await SupersedeAdrWizard(parsedArgs.ContainsKey(Arguments.OpenAdr), cancellationToken);
                }

                var fileadr = Path.GetFullPath(parsedArgs[Arguments.FileAdr]);
                if (!Path.HasExtension(fileadr))
                {
                    fileadr = Path.ChangeExtension(fileadr, ".md");
                }
                if (!_filesystem.FileExists(fileadr))
                {
                    throw new FileNotFoundException(string.Format(null, FormatMessages.ExceptionFileNotFound, fileadr));
                }
                var rootPath = Path.GetDirectoryName(fileadr) ?? string.Empty;
                var folderadrroot = _config.GetFolderNormalized();
                // Replace the throw statement to use the cached CompositeFormat
                if (!rootPath.Contains(folderadrroot))
                {
                    throw new InvalidDataException(string.Format(null, FormatMessages.FileMustBeOverFolderFormat, _config.FolderRepo, _config.FolderRepo));
                }
                var posindex = rootPath.LastIndexOf(folderadrroot, StringComparison.OrdinalIgnoreCase);

                var targetPath = rootPath[..(posindex + folderadrroot.Length)];

                rootPath = rootPath[..posindex];

                // Validate config file exists and load repo config
                var configPath = Path.GetFullPath(Path.Combine(targetPath, _validateconfig.GetFileNameRepoConfig()));
                if (!_filesystem.FileExists(configPath))
                {
                    throw new FileNotFoundException(string.Format(null, FormatMessages.ExceptionFileNotFound, configPath));
                }


                string jsonString = await _filesystem.ReadAllTextAsync(configPath, cancellationToken);
                var (IsValid, ErrorReport) = _validateconfig.ValidateRepoStructure(jsonString);
                if (!IsValid)
                {
                    LogAndWriteErrors(ErrorReport);
                    throw new InvalidDataException(Resources.AdrPlus.ErrorInConfigFile);
                }
                var repoconfig = JsonSerializer.Deserialize<AdrPlusRepoConfig>(jsonString, AppConstants.RepoSerializerOptions)!;

                var infoadr = await _adrServices.ParseFileName(fileadr, repoconfig, _filesystem);
                if (!infoadr.IsValid)
                {
                    throw new InvalidDataException(infoadr.ErrorMessage);
                }
                if (!infoadr.Header.IsValid)
                {
                    throw new InvalidDataException(infoadr.Header.ErrorMessage);
                }
                if (!hasWizard)
                {
                    if (!SelectionCondition(infoadr))
                    {
                        throw new InvalidDataException(MessageNotValidStatusForUpdate(repoconfig));
                    }
                }

                // Parse date reference
                var dateAdr = ParseDateReference(parsedArgs);

                var curpos = _console.GetCursorPosition();
                if (hasWizard)
                {
                    _console.WriteWait(Resources.AdrPlus.WaitReadFiles);
                }
                var nextNumber = await _adrServices.GetNextNumber(_filesystem, rootPath, repoconfig);
                if (hasWizard)
                {
                    _console.ClearWait(curpos);
                }

                // Create ADR record and file
                var adrRecord = new AdrRecord
                {
                    Number = nextNumber,
                    Title = infoadr.Header.Title,
                    Scope = infoadr.Header.Scope ?? string.Empty,
                    Domain = infoadr.Header.Domain ?? string.Empty,
                    StatusCreate = AdrStatus.Proposed,
                    CreateRef = dateAdr,
                    Version = repoconfig.LenVersion,
                    Revision = repoconfig.LenRevision == 0 ? null : 1,
                    Superseded = infoadr.Number,
                    Template = infoadr.ContentAdr!,
                };

                var (updok, upderror) = await _adrServices.StatusChangeSupersedeAdrAsync(infoadr.FileName, adrRecord.GetFileName(repoconfig), dateAdr, repoconfig, _filesystem, cancellationToken);
                if (!updok)
                {
                    throw new InvalidDataException(upderror);
                }
                LogAndWriteSuccess($"{repoconfig.StatusSup} : {infoadr.FileName}");

                var filePath = await CreateAdrFile(adrRecord, rootPath, repoconfig, cancellationToken);
                LogAndWriteSuccess($"{repoconfig.StatusNew} : {filePath}");

                // Open file if requested
                OpenAdrFileIfRequested(parsedArgs, filePath);
            }
            catch (Exception ex)
            {
                LogMessages.LogCommandException(_logger, ex);
                throw;
            }
        }

        /// <summary>
        /// Logs <paramref name="message"/> as an informational entry and writes it to the console as a success.
        /// </summary>
        /// <param name="message">The success message to log and display.</param>
        private void LogAndWriteSuccess(string message)
        {
            LogMessages.LogInfo(_logger, message);
            _console.WriteSuccess(message);
        }

        /// <summary>
        /// Parses the date reference from <paramref name="parsedArgs"/>.
        /// Returns <see cref="DateTime.UtcNow"/> when no date argument was provided.
        /// </summary>
        /// <param name="parsedArgs">The dictionary of parsed command-line arguments.</param>
        /// <returns>The parsed <see cref="DateTime"/>, or <see cref="DateTime.UtcNow"/> when absent.</returns>
        /// <exception cref="FormatException">Thrown when the provided date string cannot be parsed.</exception>
        private DateTime ParseDateReference(Dictionary<Arguments, string> parsedArgs)
        {
            var dateRef = parsedArgs.TryGetValue(Arguments.DateRefAdr, out string? valueDateRef) ? valueDateRef : string.Empty;

            if (dateRef.Length == 0)
            {
                return DateTime.UtcNow;
            }
            var culture = CultureInfo.GetCultureInfo(_config.Language);
            if (!DateTime.TryParse(dateRef, culture, DateTimeStyles.None, out var dateAdr))
            {
                throw new FormatException(string.Format(null, FormatMessages.ErrorDateFormat, _config.Language));
            }
            return dateAdr;
        }

        /// <summary>
        /// Writes the ADR content (header + template body) to a <c>.md</c> file under the configured
        /// repository folder, creating a scope sub-folder when <see cref="AdrPlusRepoConfig.FolderByScope"/> is enabled.
        /// </summary>
        /// <param name="adrRecord">The ADR record whose filename and content will be generated.</param>
        /// <param name="targetPath">The root directory of the repository (without the FolderRepo suffix).</param>
        /// <param name="auxconfig">The repository configuration defining folder structure and naming.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>The fully qualified path of the newly created ADR file.</returns>
        private async Task<string> CreateAdrFile(AdrRecord adrRecord, string targetPath, AdrPlusRepoConfig auxconfig, CancellationToken cancellationToken)
        {
            var filename = adrRecord.GetFileName(auxconfig);
            var folder = Path.GetFullPath(Path.Combine(targetPath, _config.FolderRepo));

            if (auxconfig.FolderByScope)
            {
                folder = Path.GetFullPath(Path.Combine(folder, adrRecord.Scope));
            }

            var filePath = _filesystem.GetFullNameFile(Path.Combine(folder, filename));
            var content = $"{adrRecord.GetHeader(auxconfig)}{adrRecord.Template}";
            await _filesystem.WriteAllTextAsync(filePath, content, cancellationToken);

            return filePath;
        }

        /// <summary>
        /// Opens the ADR file in the configured external editor when the <c>--open</c> argument was provided
        /// and <see cref="AdrPlusConfig.ComandOpenAdr"/> is non-empty.
        /// </summary>
        /// <param name="parsedArgs">The parsed command arguments used to check for the open flag.</param>
        /// <param name="filePath">The fully qualified path of the ADR file to open.</param>
        private void OpenAdrFileIfRequested(Dictionary<Arguments, string> parsedArgs, string filePath)
        {
            if (string.IsNullOrWhiteSpace(_config.ComandOpenAdr) || !parsedArgs.ContainsKey(Arguments.OpenAdr))
            {
                return;
            }

            var commandFormat = CompositeFormat.Parse(_config.ComandOpenAdr.Trim());
            var command = string.Format(null, commandFormat, filePath);
            var result = _adrServices.OpenFile(filePath, command);

            if (string.IsNullOrEmpty(result))
            {
                var msg = string.Format(null, CompositeFormat.Parse(Resources.AdrPlus.NewAdrSuccessExternalCommand), _config.ComandOpenAdr);
                LogMessages.LogCommandSuccessful(_logger, msg);
                _console.WriteSuccess(msg);
            }
            else
            {
                var msg = string.Format(null, CompositeFormat.Parse(Resources.AdrPlus.NewAdrErrorFailedToOpen), result);
                LogMessages.LogCommandFailure(_logger, msg);
                _console.WriteError(msg);
            }
        }

        /// <summary>
        /// Runs the interactive wizard for the <c>supersede</c> command, guiding the user through selecting
        /// a drive, repository folder, eligible ADR, and reference date.
        /// The wizard loops until the user confirms the selection.
        /// </summary>
        /// <param name="isOpenAdr">When <see langword="true"/>, the <see cref="Arguments.OpenAdr"/> flag is pre-populated in the result.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A dictionary of parsed arguments ready for <see cref="ExecuteAsync"/>.</returns>
        /// <exception cref="OperationCanceledException">Thrown when the user cancels any wizard prompt.</exception>
        /// <exception cref="FileNotFoundException">Thrown when no eligible ADR files are found in the repository.</exception>
        /// <exception cref="InvalidDataException">Thrown when the repository configuration is structurally invalid.</exception>
        private async Task<Dictionary<Arguments, string>> SupersedeAdrWizard(bool isOpenAdr, CancellationToken cancellationToken)
        {
            var parsedArgs = new Dictionary<Arguments, string>();

            while (true)
            {
                parsedArgs.Clear();
                if (isOpenAdr)
                {
                    parsedArgs[Arguments.OpenAdr] = string.Empty;
                }

                // Select drive
                string[] drives = _filesystem.GetDrives();
                var rootPath = drives[0];
                if (drives.Length > 1)
                {
                    var (IsAborted, Content) = _console.PromptSelectLogicalDrive(Resources.AdrPlus.NewAdrPromptSelectDrive, _filesystem, cancellationToken);
                    if (IsAborted)
                    {
                        throw new OperationCanceledException(Resources.AdrPlus.CancelledByUser);
                    }
                    rootPath = Content;
                }

                // Select folder
                var folderPrompt = _console.PromptSelectFolderRepositoryAdr(true, rootPath, _filesystem, _validateconfig, _config, cancellationToken);
                if (folderPrompt.IsAborted)
                {
                    throw new OperationCanceledException(Resources.AdrPlus.CancelledByUser);
                }

                // Validate repo config
                var configPath = Path.Combine(folderPrompt.Content, _config.FolderRepo, _validateconfig.GetFileNameRepoConfig());
                string jsonString = await _filesystem.ReadAllTextAsync(configPath, cancellationToken);
                var (IsValid, ErrorReport) = _validateconfig.ValidateRepoStructure(jsonString);

                if (!IsValid)
                {
                    LogAndWriteErrors(ErrorReport);
                    throw new InvalidDataException(string.Format(null, FormatMessages.ErrorInConfigFile, _filesystem.GetFullNameFile(configPath)));
                }

                var repoconfig = JsonSerializer.Deserialize<AdrPlusRepoConfig>(jsonString, AppConstants.RepoSerializerOptions)!;
                var targetPath = Path.GetFullPath(Path.Combine(folderPrompt.Content, _config.FolderRepo));

                var curpos = _console.GetCursorPosition();
                _console.WriteWait(Resources.AdrPlus.WaitReadFiles);
                var filesadrs = await _adrServices.ReadLatestAdrFiles(_filesystem, targetPath, repoconfig);
                _console.ClearWait(curpos);

                if (filesadrs.Length == 0)
                {
                    throw new FileNotFoundException(Resources.AdrPlus.NotFoundADR);
                }

                (bool, string?) validselect(AdrFileNameComponents info)
                {
                    if (!SelectionCondition(info))
                    {
                        return (false, MessageNotValidStatusForUpdate(repoconfig));
                    }
                    return (true, null);
                }
                var filenewsup = _console.PromptSelecLatesAdrs(filesadrs, repoconfig, validselect, cancellationToken);
                if (filenewsup.IsAborted)
                {
                    throw new OperationCanceledException(Resources.AdrPlus.CancelledByUser);
                }
                parsedArgs[Arguments.FileAdr] = filenewsup.info!.FileName;

                // Get date
                var dateRefPrompt = _console.PrompCalendar(Resources.AdrPlus.NewAdrPromptSelectDate, DateTime.UtcNow, _config, cancellationToken);
                if (dateRefPrompt.IsAborted)
                {
                    throw new OperationCanceledException(Resources.AdrPlus.CancelledByUser);
                }
                var defDateRef = dateRefPrompt.Content;
                parsedArgs[Arguments.DateRefAdr] = $"{defDateRef.ToString("d", CultureInfo.GetCultureInfo(_config.Language))}";

                // Display summary and confirm
                DisplayWizardSummary(folderPrompt.Content, Path.GetFileName(filenewsup.info.FileName), defDateRef);
                var resultCnf = _console.PromptConfirm(Resources.AdrPlus.NewAdrPromptConfirmCreation, cancellationToken);
                if (resultCnf.IsAborted)
                {
                    throw new OperationCanceledException(Resources.AdrPlus.CancelledByUser);
                }
                if (resultCnf.ConfirmYes)
                {
                    return parsedArgs;
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
                _console.WriteError(error);
            }
        }

        /// <summary>
        /// Displays a formatted summary of the wizard selections before the user confirms the supersede operation.
        /// Shows the selected repository path, ADR filename, and reference date.
        /// </summary>
        /// <param name="rootpath">The root repository path selected by the user.</param>
        /// <param name="fileref">The filename of the ADR that will be superseded.</param>
        /// <param name="defDateRef">The reference date for the supersede operation.</param>
        private void DisplayWizardSummary(string rootpath, string fileref, DateTime defDateRef)
        {
            _console.WriteSummary(Resources.AdrPlus.SelectedRepository + ": " + rootpath);
            _console.WriteSummary(Resources.AdrPlus.File + ": " + fileref);
            _console.WriteSummary(Resources.AdrPlus.Date + ": " + defDateRef.ToString("d", CultureInfo.GetCultureInfo(_config.Language)));
            _console.WriteSummary("");
        }
    }
}
