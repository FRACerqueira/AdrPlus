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
using System.Text.Json;

namespace AdrPlus.Commands.Reject
{
    /// <summary>
    /// Handles the <c>reject</c> command to mark a proposed ADR as rejected.
    /// When the ADR superseded a previous one, its supersede status is also undone.
    /// The target ADR must currently have <see cref="AdrStatus.Unknown"/> as its update status.
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of the <see cref="RejectCommandHandler"/> class.
    /// </remarks>
    /// <param name="logger">The logger for recording command execution and errors.</param>
    /// <param name="config">The application configuration settings (folder, language, etc.).</param>
    /// <param name="fileSystem">The file system service for I/O operations.</param>
    /// <param name="validateconfig">The service for validating and loading JSON configuration files.</param>
    /// <param name="console">The console writer for displaying output and prompting user input.</param>
    /// <param name="adrServices">The ADR services for argument parsing and ADR file operations.</param>
    internal sealed class RejectCommandHandler(
        ILogger<RejectCommandHandler> logger,
        IOptions<AdrPlusConfig> config,
        IFileSystemService fileSystem,
        IValidateJsonConfig validateconfig,
        IConsoleWriter console,
        IAdrServices adrServices) : ICommandHandler
    {
        private readonly ILogger<RejectCommandHandler> _logger = logger;
        private readonly AdrPlusConfig _config = config.Value;
        private readonly IFileSystemService _filesystem = fileSystem;
        private readonly IConsoleWriter _console = console;
        private readonly IValidateJsonConfig _validateconfig = validateconfig;
        private readonly IAdrServices _adrServices = adrServices;
        private static readonly Arguments[] ValidCommandArgs =
            [Arguments.WizardReject,
             Arguments.FileAdr,
             Arguments.DateRefAdr,
             Arguments.Help];

        /// <summary>
        /// Determines whether an ADR is eligible for rejection.
        /// An ADR is eligible when its update status is <see cref="AdrStatus.Unknown"/>
        /// (i.e. it has not yet been approved or rejected).
        /// </summary>
        /// <param name="info">The parsed ADR filename components containing header and status information.</param>
        /// <returns><see langword="true"/> when the ADR is eligible for rejection; otherwise <see langword="false"/>.</returns>
        private static bool SelectionCondition(AdrFileNameComponents info)
        {
            return (info.Header.StatusUpdate == AdrStatus.Unknown);
        }

        /// <summary>
        /// Builds a localized error message indicating that the ADR's current status does not allow rejection.
        /// </summary>
        /// <param name="repoconfig">The repository configuration providing the status-to-string mapping.</param>
        /// <param name="adrStatus">The current (invalid) status of the ADR.</param>
        /// <returns>A formatted error string naming the current status.</returns>
        private static string MessageNotValidStatusForUpdate(AdrPlusRepoConfig repoconfig, AdrStatus adrStatus)
        {
            return string.Format(null, FormatMessages.NotValidStatusForApproveAndReject, $"{repoconfig.StatusMapping[adrStatus]}");
        }

        /// <summary>
        /// Executes the <c>reject</c> command asynchronously to mark an ADR as rejected.
        /// When the ADR superseded another, the supersede status of that ADR is also undone.
        /// When <c>--wizard</c> is specified the user is guided interactively;
        /// otherwise the file is taken from the <c>--file</c> argument.
        /// </summary>
        /// <param name="args">The raw command-line tokens (e.g. <c>--wizard</c>, <c>--file</c>, <c>--refdate</c>, <c>--help</c>).</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="args"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">Thrown when required arguments are missing or invalid.</exception>
        /// <exception cref="FileNotFoundException">Thrown when the ADR or configuration file is not found.</exception>
        /// <exception cref="InvalidDataException">Thrown when the ADR status is not eligible for rejection, or the config is invalid.</exception>
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
                        "reject",
                        ValidCommandArgs,
                        [
                            "adrplus reject --wizard",
                            "adrplus reject --file \"path/to/File-ADR\" --refdate \"2026-01-01\"",
                        ]));
                    return;
                }

                if (!_validateconfig.HasTemplateRepoFile())
                {
                    throw new FileNotFoundException(Resources.AdrPlus.ErrMsgTemplateRepoFileNotFound);
                }

                var hasWizard = parsedArgs.ContainsKey(Arguments.WizardReject);
                if (hasWizard)
                {
                    parsedArgs = await RejectAdrWizard(cancellationToken);
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

                if (!rootPath.Contains(folderadrroot))
                {
                    throw new InvalidDataException(string.Format(null, FormatMessages.FileMustBeOverFolderFormat, _config.FolderRepo, _config.FolderRepo));
                }
                var posindex = rootPath.LastIndexOf(folderadrroot, StringComparison.OrdinalIgnoreCase);

                var targetPath = rootPath[..(posindex + folderadrroot.Length)];

                rootPath = rootPath[..posindex];

                // Validate that the repository configuration file exists
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
                        throw new InvalidDataException(MessageNotValidStatusForUpdate(repoconfig, infoadr.Header.StatusUpdate));
                    }
                }

                var dateAdr = ParseDateReference(parsedArgs);

                var (updok, upderror) = await _adrServices.StatusUpdateAdrAsync(infoadr.FileName, AdrStatus.Rejected, dateAdr, repoconfig, _filesystem, cancellationToken);
                if (!updok)
                {
                    throw new InvalidDataException(upderror);
                }
                LogAndWriteSuccess($"{repoconfig.StatusRej} : {infoadr.FileName}");
                if (infoadr.SupersededValue.HasValue)
                {
                    var infoundo = await _adrServices.GetLatestADRSequence(infoadr.SupersededValue.Value, _filesystem, rootPath, repoconfig) ?? throw new InvalidDataException(string.Format(null, FormatMessages.ErrorSequenceAdrNotFound, infoadr.SupersededValue.Value));
                    var (updundo, updundoerror) = await _adrServices.StatusChangeAdrAsync(infoundo.FileName, AdrStatus.Unknown, dateAdr, repoconfig, _filesystem, cancellationToken);
                    if (!updundo)
                    {
                        throw new InvalidDataException(updundoerror);
                    }
                    LogAndWriteSuccess($"{Resources.AdrPlus.Undone} {repoconfig.StatusMapping[AdrStatus.Superseded]} : {infoundo.FileName}");
                }
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
        /// Parses the date reference from <paramref name="parsedArgs"/> using the configured application culture.
        /// Returns <see cref="DateTime.UtcNow"/> when no date argument was provided.
        /// </summary>
        /// <param name="parsedArgs">The dictionary of parsed command-line arguments.</param>
        /// <returns>The parsed <see cref="DateTime"/>, or <see cref="DateTime.UtcNow"/> when absent.</returns>
        /// <exception cref="FormatException">Thrown when the provided date string cannot be parsed for the configured culture.</exception>
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
                LogMessages.LogErrorFormatDateForCulture(_logger, _config.Language);
                throw new FormatException(string.Format(null, FormatMessages.ErrorDateFormat, _config.Language));
            }
            return dateAdr;
        }

        /// <summary>
        /// Runs the interactive wizard for the <c>reject</c> command, guiding the user through selecting
        /// a drive, repository folder, eligible ADR, and reference date.
        /// The wizard loops until the user confirms the selection.
        /// </summary>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A dictionary of parsed arguments (file path and date) ready for <see cref="ExecuteAsync"/>.</returns>
        /// <exception cref="OperationCanceledException">Thrown when the user cancels any wizard prompt.</exception>
        /// <exception cref="FileNotFoundException">Thrown when no eligible ADR files are found in the repository.</exception>
        /// <exception cref="InvalidDataException">Thrown when the repository configuration is structurally invalid.</exception>
        private async Task<Dictionary<Arguments, string>> RejectAdrWizard(CancellationToken cancellationToken)
        {
            var parsedArgs = new Dictionary<Arguments, string>();

            while (true)
            {
                parsedArgs.Clear();

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

                var folderPrompt = _console.PromptSelectFolderRepositoryAdr(true, rootPath, _filesystem, _validateconfig, _config, cancellationToken);
                if (folderPrompt.IsAborted)
                {
                    throw new OperationCanceledException(Resources.AdrPlus.CancelledByUser);
                }

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
                        return (false, MessageNotValidStatusForUpdate(repoconfig, info.Header.StatusUpdate));
                    }
                    return (true, null);
                }
                var filenewver = _console.PromptSelecLatesAdrs(filesadrs, repoconfig, validselect, cancellationToken);
                if (filenewver.IsAborted)
                {
                    throw new OperationCanceledException(Resources.AdrPlus.CancelledByUser);
                }
                parsedArgs[Arguments.FileAdr] = filenewver.info!.FileName;

                var dateRefPrompt = _console.PrompCalendar(Resources.AdrPlus.NewAdrPromptSelectDate, DateTime.UtcNow, _config, cancellationToken);
                if (dateRefPrompt.IsAborted)
                {
                    throw new OperationCanceledException(Resources.AdrPlus.CancelledByUser);
                }
                var defDateRef = dateRefPrompt.Content;
                parsedArgs[Arguments.DateRefAdr] = $"{defDateRef.ToString("d", CultureInfo.GetCultureInfo(_config.Language))}";

                DisplayWizardSummary(folderPrompt.Content, Path.GetFileName(filenewver.info.FileName), defDateRef);
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
        /// Displays a formatted summary of the wizard selections before the user confirms the rejection.
        /// Shows the selected repository path, ADR filename, and reference date.
        /// </summary>
        /// <param name="rootpath">The root repository path selected by the user.</param>
        /// <param name="fileref">The filename of the ADR to be rejected.</param>
        /// <param name="defDateRef">The reference date for the rejection operation.</param>
        private void DisplayWizardSummary(string rootpath, string fileref, DateTime defDateRef)
        {
            _console.WriteSummary(Resources.AdrPlus.SelectedRepository + ": " + rootpath);
            _console.WriteSummary(Resources.AdrPlus.File + ": " + fileref);
            _console.WriteSummary(Resources.AdrPlus.Date + ": " + defDateRef.ToString("d", CultureInfo.GetCultureInfo(_config.Language)));
            _console.WriteSummary("");
        }
    }
}
