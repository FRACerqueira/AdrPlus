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

namespace AdrPlus.Commands.Version
{
    /// <summary>
    /// Handles the <c>newver</c> command to create a new major version of an accepted or rejected ADR.
    /// The target ADR must be the latest version for its sequence number, must have been accepted or rejected,
    /// and versioning (<see cref="AdrPlusRepoConfig.LenVersion"/>) must be enabled in the repository configuration.
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of the <see cref="VersionCommandHandler"/> class.
    /// </remarks>
    /// <param name="logger">The logger for recording command execution and errors.</param>
    /// <param name="config">The application configuration settings (folder, language, open command, etc.).</param>
    /// <param name="fileSystem">The file system service for I/O operations.</param>
    /// <param name="validateconfig">The service for validating and loading JSON configuration files.</param>
    /// <param name="console">The console writer for displaying output and prompting user input.</param>
    /// <param name="adrServices">The ADR services for argument parsing and ADR file operations.</param>
    internal sealed partial class VersionCommandHandler(
        ILogger<VersionCommandHandler> logger,
        IOptions<AdrPlusConfig> config,
        IFileSystemService fileSystem,
        IValidateJsonConfig validateconfig,
        IConsoleWriter console,
        IAdrServices adrServices) : ICommandHandler
    {
        private readonly ILogger<VersionCommandHandler> _logger = logger;
        private readonly AdrPlusConfig _config = config.Value;
        private readonly IFileSystemService _filesystem = fileSystem;
        private readonly IConsoleWriter _console = console;
        private readonly IValidateJsonConfig _validateconfig = validateconfig;
        private readonly IAdrServices _adrServices = adrServices;
        private static readonly Arguments[] ValidCommandArgs =
            [Arguments.WizardVersion,
             Arguments.FileAdr,
             Arguments.DateRefAdr,
             Arguments.OpenAdr,
             Arguments.EmptyAdr,
             Arguments.Help];

        /// <summary>
        /// Determines whether an ADR is eligible for a new version.
        /// An ADR is eligible when its update status is <see cref="AdrStatus.Accepted"/> or <see cref="AdrStatus.Rejected"/>
        /// and its change status is <see cref="AdrStatus.Unknown"/>.
        /// </summary>
        /// <param name="info">The parsed ADR filename components containing header and status information.</param>
        /// <returns><see langword="true"/> when the ADR is eligible for versioning; otherwise <see langword="false"/>.</returns>
        private static bool SelectionCondition(AdrFileNameComponents info)
        {
            return (info.Header.IsValid &&
                (info.Header.StatusCreate == AdrStatus.Proposed || (info.Header.StatusCreate == AdrStatus.Unknown && info.Header.IsMigrated)) &&
                ((info.Header.StatusUpdate == AdrStatus.Accepted || info.Header.StatusUpdate == AdrStatus.Rejected) || (info.Header.StatusUpdate == AdrStatus.Unknown && info.Header.IsMigrated)) &&
                info.Header.StatusChange == AdrStatus.Unknown);
        }

        /// <summary>
        /// Builds a localized error message indicating that the ADR's current status does not allow a new version.
        /// </summary>
        /// <returns>A formatted error string listing the required statuses (Accepted/Rejected).</returns>
        private static string MessageNotValidStatusForUpdate()
        {
            return string.Format(null, FormatMessages.NotValidStatusForUpdate, $"{Helper.GetResourceStatus(AdrStatus.Accepted)}/{Helper.GetResourceStatus(AdrStatus.Rejected)}");
        }

        /// <summary>
        /// Builds a localized error message indicating that the ADR's current status does not allow a new version, 
        /// </summary>
        /// <param name="adrStatus">The current (invalid) status of the ADR.</param>
        /// <returns>A formatted error string naming the current status.</returns>
        private static string MessageNotValidStatusForUpdate(AdrStatus adrStatus)
        {
            return string.Format(null, FormatMessages.NotValidStatusForUpdate, $"{Helper.GetResourceStatus(adrStatus)}");
        }


        /// <summary>
        /// Executes the <c>newver</c> command asynchronously to create a new version of an ADR.
        /// When <c>--wizard</c> is specified the user is guided interactively;
        /// otherwise the file is taken from the <c>--file</c> argument.
        /// </summary>
        /// <param name="args">The raw command-line tokens (e.g. <c>--wizard</c>, <c>--file</c>, <c>--refdate</c>, <c>--open</c>, <c>--help</c>).</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="args"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">Thrown when required arguments are missing or invalid.</exception>
        /// <exception cref="FileNotFoundException">Thrown when the ADR or configuration file is not found.</exception>
        /// <exception cref="InvalidDataException">
        /// Thrown when versioning is not configured, the ADR status is not eligible,
        /// or the repository configuration is invalid.
        /// </exception>
        /// <exception cref="InvalidOperationException">Thrown when the selected ADR is not the latest version for its sequence number.</exception>
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
                        "version",
                        ValidCommandArgs,
                        [
                            "adrplus version --wizard --open",
                            "adrplus version --file \"path/to/File-ADR\" --refdate \"2026-01-01\"",
                        ]));
                    return;
                }

                if (!_validateconfig.HasTemplateRepoFile())
                {
                    throw new FileNotFoundException(Resources.AdrPlus.ErrMsgTemplateRepoFileNotFound);
                }

                var hasWizard = parsedArgs.ContainsKey(Arguments.WizardVersion);
                if (hasWizard)
                {
                    parsedArgs  = await VersionAdrWizard(parsedArgs.ContainsKey(Arguments.OpenAdr), cancellationToken);
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
                var configrootPath = _filesystem.GetFileRootRepositoryPath(fileadr)
                        ?? throw new InvalidDataException(string.Format(null, FormatMessages.ErrorCannotDetermineRootPath, fileadr));

                string jsonString = await _filesystem.ReadAllTextAsync(configrootPath, cancellationToken);
                var (IsValid, ErrorReport) = _validateconfig.ValidateRepoStructure(jsonString);
                if (!IsValid)
                {
                    LogAndWriteErrors(ErrorReport);
                    throw new InvalidDataException(Resources.AdrPlus.ErrorInConfigFile);
                }

                var repoconfig = JsonSerializer.Deserialize<AdrPlusRepoConfig>(jsonString, AppConstants.RepoSerializerOptions)!;

                var rootPath = _filesystem.GetFullNameDirectoryByFile(configrootPath);

                var infoadr = await _adrServices.ParseFileName(fileadr, repoconfig,_filesystem);
                if (!infoadr.IsValid)
                {
                    throw new InvalidDataException(infoadr.ErrorMessage);
                }
                if (!infoadr.Header.IsValid)
                {
                    throw new InvalidDataException(infoadr.Header.ErrorMessage);
                }

                var curpos = _console.GetCursorPosition();
                if (hasWizard)
                {
                    _console.WriteWait(Resources.AdrPlus.WaitReadFiles);
                }
                var infolastadr = (await _adrServices.GetLatestADRSequence(infoadr.Number, _filesystem, rootPath, repoconfig))!;
                if (hasWizard)
                {
                    _console.ClearWait(curpos);
                }
                if (!infolastadr.IsValid)
                {
                    throw new InvalidDataException(infolastadr.ErrorMessage);
                }
                if (!infolastadr.Header.IsValid)
                {
                    throw new InvalidDataException(infolastadr.Header.ErrorMessage);
                }
                if (infolastadr.UniqueTitle != infoadr.UniqueTitle || infoadr.Number != infolastadr.Number)
                {
                    throw new InvalidOperationException(string.Format(null, FormatMessages.ErrorSequenceAdrNotFound, infoadr.Number, infolastadr.FileName));
                }
                if (infolastadr.FileName != infoadr.FileName)
                {
                    if (repoconfig.LenRevision == 0)
                    {
                        if (infolastadr.Version > infoadr.Version)
                        {
                            if (infolastadr.Header.StatusUpdate != AdrStatus.Rejected)
                            {
                                throw new InvalidOperationException(Resources.AdrPlus.NotLatestVersion);
                            }
                        }
                        else
                        {
                            throw new InvalidOperationException(Resources.AdrPlus.NotLatestVersion);
                        }
                    }
                    else
                    {
                        if (infolastadr.Version > infoadr.Version)
                        {
                            if (infolastadr.Header.StatusUpdate != AdrStatus.Rejected)
                            {
                                throw new InvalidOperationException(Resources.AdrPlus.NotLatestVersion);
                            }
                        }
                        else if (infolastadr.Version == infoadr.Version && (infolastadr.Revision??0) > (infoadr.Revision??0))
                        {
                            if (infolastadr.Header.StatusUpdate != AdrStatus.Rejected)
                            {
                                throw new InvalidOperationException(Resources.AdrPlus.NotLatestVersion);
                            }
                        }
                        else
                        {
                            throw new InvalidOperationException(Resources.AdrPlus.NotLatestVersion);
                        }
                    }
                }
                if (!hasWizard)
                {
                    if (!SelectionCondition(infoadr))
                    {
                        throw new InvalidDataException(MessageNotValidStatusForUpdate(AdrStatus.Unknown));
                    }
                    var filescheckadrs = _adrServices.ReadAllAdrByNumber(infoadr.Number, _filesystem, rootPath, repoconfig).Result;
                    if (filescheckadrs.Any(adr => adr.Header.StatusChange == AdrStatus.Superseded))
                    {
                        throw new InvalidDataException(MessageNotValidStatusForUpdate(AdrStatus.Superseded));
                    }
                    if (filescheckadrs.Any(adr => adr.Header.StatusUpdate == AdrStatus.Unknown && !adr.Header.IsMigrated))
                    {
                        throw new InvalidDataException(MessageNotValidStatusForUpdate(AdrStatus.Proposed));
                    }
                }

                // Parse date reference
                var dateAdr = ParseDateReference(parsedArgs);

                var emptyard = false;
                if (parsedArgs.ContainsKey(Arguments.EmptyAdr))
                { 
                    emptyard = true;
                }

                // Create ADR record and file
                var adrRecord = new AdrRecord
                {
                    Number = infoadr.Number,
                    Title = infoadr.Header.Title,
                    Scope = infoadr.Header.Scope ?? string.Empty,
                    Domain = infoadr.Header.Domain ?? string.Empty,
                    StatusCreate = AdrStatus.Proposed,
                    CreateRef = dateAdr,
                    Version = infolastadr.Header.Version+1,
                    Revision = repoconfig.LenRevision == 0 ? null : 1,
                    Template = emptyard ? repoconfig.Template : infoadr.ContentAdr!,
                };
                var filePath = await CreateAdrFile(adrRecord, rootPath, repoconfig, cancellationToken);
                var msgok = $"{repoconfig.StatusNew} : {filePath}";
                LogAndWriteSuccess(msgok);

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
        /// Logs <paramref name="message"/> as an error and writes it to the console.
        /// </summary>
        /// <param name="message">The error message to log and display.</param>
        private void LogAndWriteError(string message)
        {
            LogMessages.LogError(_logger, message);
            _console.WriteError(message);
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
        /// Writes the ADR content (header + body) to a <c>.md</c> file under the configured repository folder,
        /// creating a scope sub-folder when <see cref="AdrPlusRepoConfig.FolderByScope"/> is enabled.
        /// </summary>
        /// <param name="adrRecord">The ADR record whose filename and content will be generated.</param>
        /// <param name="rootpath">The root directory of the repository.</param>
        /// <param name="auxconfig">The repository configuration defining folder structure and naming.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>The fully qualified path of the newly created ADR file.</returns>
        private async Task<string> CreateAdrFile(AdrRecord adrRecord, string rootpath, AdrPlusRepoConfig auxconfig, CancellationToken cancellationToken)
        {
            var filename = adrRecord.GetFileName(auxconfig);
            var folder = Path.GetFullPath(Path.Combine(rootpath, auxconfig.FolderAdr));

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
                LogAndWriteSuccess(msg);
            }
            else
            {
                var msg = string.Format(null, CompositeFormat.Parse(Resources.AdrPlus.NewAdrErrorFailedToOpen), result);
                LogAndWriteError(msg);
            }
        }

        /// <summary>
        /// Runs the interactive wizard for the <c>newver</c> command, guiding the user through selecting
        /// a drive, repository folder, eligible ADR, and reference date.
        /// The wizard loops until the user confirms the selection.
        /// </summary>
        /// <param name="isOpenAdr">When <see langword="true"/>, the <see cref="Arguments.OpenAdr"/> flag is pre-populated in the result.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A dictionary of parsed arguments ready for <see cref="ExecuteAsync"/>.</returns>
        /// <exception cref="OperationCanceledException">Thrown when the user cancels any wizard prompt.</exception>
        /// <exception cref="FileNotFoundException">Thrown when no eligible ADR files are found in the repository.</exception>
        /// <exception cref="InvalidDataException">Thrown when the repository configuration is structurally invalid.</exception>
        private async Task<Dictionary<Arguments, string>> VersionAdrWizard(bool isOpenAdr, CancellationToken cancellationToken)
        {
            var parsedArgs = new Dictionary<Arguments, string>();
            while (true)
            {
                parsedArgs.Clear();

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
                var folderPrompt = _console.PromptSelectFolderRepositoryPath(true, rootPath, _filesystem, _validateconfig,cancellationToken);
                if (folderPrompt.IsAborted)
                {
                    throw new OperationCanceledException(Resources.AdrPlus.CancelledByUser);
                }

                // Validate repo config
                var configPath = Path.GetFullPath(Path.Combine(folderPrompt.Content, _validateconfig.GetFileNameRepoConfig()));
                string jsonString = await _filesystem.ReadAllTextAsync(configPath, cancellationToken);
                var (IsValid, ErrorReport) = _validateconfig.ValidateRepoStructure(jsonString);

                if (!IsValid)
                {
                    LogAndWriteErrors(ErrorReport);
                    throw new InvalidDataException(string.Format(null, FormatMessages.ErrorInConfigFile, _filesystem.GetFullNameFile(configPath)));
                }

                var repoconfig = JsonSerializer.Deserialize<AdrPlusRepoConfig>(jsonString, AppConstants.RepoSerializerOptions)!;
                var curpos = _console.GetCursorPosition();
                _console.WriteWait(Resources.AdrPlus.WaitReadFiles);
                var filesadrs = await _adrServices.ReadAllAdr(_filesystem, folderPrompt.Content, repoconfig,false);
                _console.ClearWait(curpos);
                if (filesadrs.Length == 0)
                {
                    throw new FileNotFoundException(Resources.AdrPlus.NotFoundADR);
                }
                (bool, string?) validselect(AdrFileNameComponents info)
                {
                    if (!SelectionCondition(info))
                    {
                        return (false, MessageNotValidStatusForUpdate(AdrStatus.Unknown));
                    }
                    
                    var filescheckadrs = _adrServices.ReadAllAdrByNumber(info.Number, _filesystem, folderPrompt.Content, repoconfig).Result;
                    if (filescheckadrs.Any(adr => adr.Header.StatusChange == AdrStatus.Superseded))
                    {
                        return (false, MessageNotValidStatusForUpdate(AdrStatus.Superseded));
                    }
                    if (filescheckadrs.Any(adr => adr.Header.StatusUpdate == AdrStatus.Unknown && !adr.Header.IsMigrated))
                    {
                        throw new InvalidDataException(MessageNotValidStatusForUpdate(AdrStatus.Proposed));
                    }
                    return (true, null);
                }
                var filenewver = _console.PromptSelecAdrs(filesadrs,repoconfig, validselect, cancellationToken);
                if (filenewver.IsAborted)
                {
                    throw new OperationCanceledException(Resources.AdrPlus.CancelledByUser);
                }

                if (filenewver.info!.Header.StatusUpdate == AdrStatus.Rejected)
                {
                    var (IsAborted, ConfirmYes) = _console.PromptConfirm(Resources.AdrPlus.NewAdrPromptConfirmRejectCreation, cancellationToken);
                    if (!ConfirmYes)
                    {
                        throw new OperationCanceledException(Resources.AdrPlus.CancelledByUser);
                    }
                }
                parsedArgs[Arguments.FileAdr] = filenewver.info!.FileName;

                // Get date
                var dateRefPrompt = _console.PromptCalendar(Resources.AdrPlus.NewAdrPromptSelectDate, DateTime.UtcNow, _config, cancellationToken);
                if (dateRefPrompt.IsAborted)
                {
                    throw new OperationCanceledException(Resources.AdrPlus.CancelledByUser);
                }
                parsedArgs[Arguments.DateRefAdr] = $"{dateRefPrompt.Content.ToString("d", CultureInfo.GetCultureInfo(_config.Language))}";
                if (isOpenAdr)
                {
                    parsedArgs[Arguments.OpenAdr] = string.Empty;
                }

                var emptyadr = _console.PromptEmptyTemplate(cancellationToken);
                if (emptyadr.IsAborted)
                {
                    throw new OperationCanceledException(Resources.AdrPlus.CancelledByUser);
                }
                if (emptyadr.Content)
                {
                    parsedArgs[Arguments.EmptyAdr] = string.Empty;
                }

                // Display summary and confirm
                DisplayWizardSummary(folderPrompt.Content, Path.GetFileName(filenewver.info.FileName), dateRefPrompt.Content);
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
        /// Logs each error in <paramref name="errors"/> as a failure entry and writes it to the console.
        /// </summary>
        /// <param name="errors">An array of validation error messages to log and display.</param>
        private void LogAndWriteErrors(string[] errors)
        {
            foreach (var error in errors)
            {
                LogMessages.LogError(_logger, error);
                _console.WriteError(error);
            }
        }

        /// <summary>
        /// Displays the wizard summary showing selected options for ADR versioning.
        /// </summary>
        /// <param name="rootpath">The root repository path.</param>
        /// <param name="fileref">The reference file name.</param>
        /// <param name="defDateRef">The reference date for the operation.</param>
        private void DisplayWizardSummary(string rootpath ,string fileref, DateTime defDateRef)
        {
            _console.WriteSummary(Resources.AdrPlus.SelectRepo + ": " + rootpath);
            _console.WriteSummary(Resources.AdrPlus.File + ": " + fileref);
            _console.WriteSummary(Resources.AdrPlus.Date + ": " + defDateRef.ToString("d", CultureInfo.GetCultureInfo(_config.Language)));
            _console.WriteSummary("");
        }
    }
}
