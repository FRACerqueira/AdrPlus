// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the AdrPlus project under MIT license
// ***************************************************************************************

using AdrPlus.Abstractions;
using AdrPlus.Core;
using AdrPlus.Domain;
using AdrPlus.Extensions;
using AdrPlus.Infrastructure.FileSystem;
using AdrPlus.Infrastructure.Formatting;
using AdrPlus.Infrastructure.Logging;
using AdrPlus.Infrastructure.UI;
using AdrPlus.Plugins;
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
    internal sealed class SupersedeCommandHandler(
        ILogger<SupersedeCommandHandler> logger,
        IOptions<AdrPlusConfig> config,
        IFileSystemService fileSystem,
        IValidateConfig validateconfig,
        IConsoleWriter prompt,
        INewAdrPrompts newAdrPrompts,
        IAdrServices adrServices,
        IPluginManager pluginManager) : ICommandHandler
    {
        private readonly ILogger<SupersedeCommandHandler> _logger = logger;
        private readonly AdrPlusConfig _config = config.Value;
        private readonly IFileSystemService _filesystem = fileSystem;
        private readonly IConsoleWriter _prompt = prompt;
        private readonly INewAdrPrompts _newAdrPrompts = newAdrPrompts;
        private readonly IValidateConfig _validateconfig = validateconfig;
        private readonly IAdrServices _adrServices = adrServices;
        private readonly IPluginManager _pluginManager = pluginManager;
        private static readonly Arguments[] ValidCommandArgs =
            [Arguments.WizardSupersede,
             Arguments.FileAdr,
             Arguments.DomainAdr,
             Arguments.ScopeAdr,
             Arguments.DateRefAdr,
             Arguments.OpenFile,
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
            return (info.Header.IsValid &&
                (info.Header.StatusCreate == AdrStatus.Proposed || (info.Header.StatusCreate == AdrStatus.Unknown && info.Header.IsMigrated)) &&
                (info.Header.StatusUpdate == AdrStatus.Accepted || (info.Header.StatusUpdate == AdrStatus.Unknown && info.Header.IsMigrated)) &&
                info.Header.StatusChange == AdrStatus.Unknown);
        }

        /// <summary>
        /// Builds a localized error message indicating that the ADR's current status does not allow superseding.
        /// </summary>
        /// <returns>A formatted error string naming the required status (<see cref="AdrStatus.Accepted"/>).</returns>
        private static string MessageNotValidStatusForUpdate()
        {
            return string.Format(null, FormatMessages.ErrInvalidStatusForSupersede, $"{Helper.GetResourceStatus(AdrStatus.Accepted)}");
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
            try
            {
                ArgumentNullException.ThrowIfNull(args);
                var parsedArgs = _adrServices.ParseArgs(args, ValidCommandArgs);
                if (parsedArgs.ContainsKey(Arguments.Help))
                {
                    _prompt.PromptWriteHelp(_adrServices.GetHelpText(
                        "supersede",
                        ValidCommandArgs,
                        [
                            "adrplus supersede --wizard --open",
                            "adrplus supersede --file \"path/to/File-ADR\" --refdate \"2026-01-01\"",
                            "adrplus supersede --file \"path/to/File-ADR\" --scope \"Backend\" --domain \"Payments\"",
                        ]));
                    return;
                }


                var hasWizard = parsedArgs.ContainsKey(Arguments.WizardSupersede);
                if (hasWizard)
                {
                    var openafter = parsedArgs.ContainsKey(Arguments.OpenFile);
                    if (!openafter && _config.ComandOpenAdr.Length > 0)
                    {
                        openafter = true;
                    }
                    parsedArgs = await SupersedeAdrWizard(openafter, cancellationToken);
                }

                var fileadr = Path.GetFullPath(parsedArgs[Arguments.FileAdr]);
                if (!Path.HasExtension(fileadr))
                {
                    fileadr = Path.ChangeExtension(fileadr, ".md");
                }
                if (!_filesystem.FileExists(fileadr))
                {
                    throw new FileNotFoundException(string.Format(null, FormatMessages.ErrFileNotFound, fileadr));
                }
                var configrootPath = _filesystem.GetFileRootRepositoryPath(fileadr)
                        ?? throw new InvalidDataException(string.Format(null, FormatMessages.ErrCannotDetermineRootPath, fileadr));

                var rootrepo = _filesystem.GetFullNameDirectoryByFile(configrootPath);

                string jsonString = await _filesystem.ReadAllTextAsync(configrootPath, cancellationToken);
                var (IsValid, ErrorReport) = _validateconfig.ValidateRepoStructure(jsonString);
                if (!IsValid)
                {
                    LogAndWriteErrors(ErrorReport);
                    throw new InvalidDataException(string.Format(null, FormatMessages.ErrInvalidRepositoryConfig, configrootPath));
                }

                var repoconfig = JsonSerializer.Deserialize<AdrPlusRepoConfig>(jsonString, AppConstants.RepoSerializerOptions)!;

                await _pluginManager.LoadPluginsAsync(cancellationToken);
                var (isActive, missingNames) = PluginActivationGate.Resolve(_pluginManager, repoconfig);

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
                        throw new InvalidDataException(MessageNotValidStatusForUpdate());
                    }
                }

                var dateAdr = ParseDateReference(parsedArgs);

                var curpos = _prompt.PromptGetCursorPosition();
                if (hasWizard)
                {
                    _prompt.PromptWriteWait(Resources.AdrPlus.WaitReadFiles);
                }
                var nextNumber = await _adrServices.GetNextNumber(_filesystem, rootrepo, repoconfig);
                if (hasWizard)
                {
                    _prompt.PromptClearWaitText(curpos);
                }

                var scope = parsedArgs.TryGetValue(Arguments.ScopeAdr, out string? scopeValue) ? scopeValue : (infoadr.Header.Scope ?? string.Empty);
                var domain = parsedArgs.TryGetValue(Arguments.DomainAdr, out string? domainValue) ? domainValue : (infoadr.Header.Domain ?? string.Empty);

                var adrRecord = new AdrRecord
                {
                    Number = nextNumber,
                    Title = infoadr.Title,
                    Scope = scope,
                    Domain = domain,
                    StatusCreate = AdrStatus.Proposed,
                    CreateRef = dateAdr,
                    Version = 1,
                    Revision = repoconfig.LenRevision == 0 ? null : 1,
                    Superseded = infoadr.Number,
                    Template = repoconfig.Template,
                };


                var filename = adrRecord.GetFileName(repoconfig);
                var folder = Path.GetFullPath(Path.Combine(rootrepo, repoconfig.FolderAdr));
                var filePath = _filesystem.GetFullNameFile(Path.Combine(folder, filename));
                if (_filesystem.FileExists(filePath))
                {
                    throw new InvalidOperationException(string.Format(null, FormatMessages.ErrFileAlreadyExists, Path.GetFileName(filePath)));
                }

                var numbersupersede = nextNumber.ToString($"D{repoconfig.LenSeq}", null);
                var (updok, upderror, oldrecord, oldcontent) = await _adrServices.StatusChangeSupersedeAdrAsync(infoadr.FileName, numbersupersede, dateAdr, repoconfig, _filesystem, cancellationToken);
                if (!updok || oldrecord is null || oldcontent is null)
                {
                    throw new InvalidDataException(upderror);
                }
                PluginActivationGate.WarnMissingActivePlugins(_logger, _prompt, missingNames);
                LogAndWriteSuccess($"{repoconfig.StatusSup} : {infoadr.FileName}");

                await _pluginManager.DispatchAsync(AdrEventType.Superseded, oldrecord.ToSnapshot(), infoadr.FileName, () => oldcontent, repoconfig.ToSnapshot(), Path.Combine(rootrepo, "plugins-state"), isReplay: false, isActive: isActive, cancellationToken: cancellationToken);

                var content = $"{adrRecord.GetHeader(repoconfig)}{adrRecord.Template}";
                await _filesystem.WriteAllTextAsync(filePath, content, cancellationToken);

                LogAndWriteSuccess($"{repoconfig.StatusNew} : {filePath}");

                OpenAdrFileIfRequested(parsedArgs, filePath);
            }
            catch (Exception ex)
            {
                LogMessages.LogCommandException(_logger, ex);
                throw;
            }
        }

        private void LogAndWriteSuccess(string message)
        {
            LogMessages.LogInfo(_logger, message);
            _prompt.PromptWriteSuccess(message);
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
                throw new FormatException(string.Format(null, FormatMessages.ErrInvalidDateFormat, _config.Language));
            }
            return dateAdr;
        }

        /// <summary>
        /// Opens the ADR file in the configured external editor when the <c>--open</c> argument was provided
        /// and <see cref="AdrPlusConfig.ComandOpenAdr"/> is non-empty.
        /// Logs and displays the result (success or error) to the console.
        /// </summary>
        /// <param name="parsedArgs">The parsed command arguments used to check for the open flag.</param>
        /// <param name="filePath">The fully qualified path of the ADR file to open.</param>
        private void OpenAdrFileIfRequested(Dictionary<Arguments, string> parsedArgs, string filePath)
        {
            if (!parsedArgs.ContainsKey(Arguments.OpenFile) || _config.ComandOpenAdr.Length == 0)
            {
                return;
            }

            var commandFormat = CompositeFormat.Parse(_config.ComandOpenAdr.Trim());
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

        /// <summary>
        /// Runs the interactive wizard for the <c>supersede</c> command, guiding the user through selecting
        /// a drive, repository folder, eligible ADR, and reference date.
        /// The wizard loops until the user confirms the selection.
        /// </summary>
        /// <param name="isOpenAdr">When <see langword="true"/>, the <see cref="Arguments.OpenFile"/> flag is pre-populated in the result.</param>
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
                    parsedArgs[Arguments.OpenFile] = string.Empty;
                }

                string[] drives = _filesystem.GetDrives();
                var rootPath = drives[0];
                if (drives.Length > 1)
                {
                    var (IsAborted, Content) = _prompt.PromptSelectLogicalDrive(Resources.AdrPlus.NewAdrPromptSelectDrive, _filesystem, cancellationToken);
                    if (IsAborted)
                    {
                        throw new OperationCanceledException(Resources.AdrPlus.CancelledByUser);
                    }
                    rootPath = Content;
                }

                var folderPrompt = _prompt.PromptSelectFolderPath(Resources.AdrPlus.PromptSelectRepositoryPath, true, rootPath, _filesystem, _validateconfig, cancellationToken);
                if (folderPrompt.IsAborted)
                {
                    throw new OperationCanceledException(Resources.AdrPlus.CancelledByUser);
                }

                var configPath = Path.Combine(folderPrompt.Content, _validateconfig.GetFileNameRepoConfig());
                string jsonString = await _filesystem.ReadAllTextAsync(configPath, cancellationToken);
                var (IsValid, ErrorReport) = _validateconfig.ValidateRepoStructure(jsonString);

                if (!IsValid)
                {
                    LogAndWriteErrors(ErrorReport);
                    throw new InvalidDataException(string.Format(null, FormatMessages.ErrConfigFileInvalid, _filesystem.GetFullNameFile(configPath)));
                }

                var repoconfig = JsonSerializer.Deserialize<AdrPlusRepoConfig>(jsonString, AppConstants.RepoSerializerOptions)!;

                var curpos = _prompt.PromptGetCursorPosition();
                _prompt.PromptWriteWait(Resources.AdrPlus.WaitReadFiles);
                var filesadrs = await _adrServices.ReadAllAdr(_filesystem, folderPrompt.Content, repoconfig,false);
                _prompt.PromptClearWaitText(curpos);

                if (filesadrs.Length == 0)
                {
                    throw new FileNotFoundException(Resources.AdrPlus.NotFoundADR);
                }

                static (bool, string?) validselect(AdrFileNameComponents info)
                {
                    if (!SelectionCondition(info))
                    {
                        return (false, MessageNotValidStatusForUpdate());
                    }
                    return (true, null);
                }
                var filenewsup = _prompt.PromptSelecAdrs(filesadrs, repoconfig, validselect, cancellationToken);
                if (filenewsup.IsAborted)
                {
                    throw new OperationCanceledException(Resources.AdrPlus.CancelledByUser);
                }
                parsedArgs[Arguments.FileAdr] = filenewsup.info!.FileName;

                var (ScopesAborted, scopes, scopesException) = _newAdrPrompts.PromptGetArrayScopesAdr(filesadrs, cancellationToken);
                if (ScopesAborted)
                {
                    throw new OperationCanceledException(Resources.AdrPlus.CancelledByUser);
                }
                if (scopesException != null)
                {
                    LogMessages.LogError(_logger, $"Failed to read registered scopes for suggestions: {scopesException.Message}");
                }
                var (DomainsAborted, domains, domainsException) = _newAdrPrompts.PromptGetArrayDomainsAdr(filesadrs, cancellationToken);
                if (DomainsAborted)
                {
                    throw new OperationCanceledException(Resources.AdrPlus.CancelledByUser);
                }
                if (domainsException != null)
                {
                    LogMessages.LogError(_logger, $"Failed to read registered domains for suggestions: {domainsException.Message}");
                }

                var scopePrompt = _newAdrPrompts.PromptEditScopeAdr(filenewsup.info.Header.Scope ?? string.Empty, scopes, cancellationToken);
                if (scopePrompt.IsAborted)
                {
                    throw new OperationCanceledException(Resources.AdrPlus.CancelledByUser);
                }
                parsedArgs[Arguments.ScopeAdr] = scopePrompt.Content.Trim();

                var domainPrompt = _newAdrPrompts.PromptEditDomainAdr(filenewsup.info.Header.Domain ?? string.Empty, domains, cancellationToken);
                if (domainPrompt.IsAborted)
                {
                    throw new OperationCanceledException(Resources.AdrPlus.CancelledByUser);
                }
                parsedArgs[Arguments.DomainAdr] = domainPrompt.Content.Trim();

                var dateRefPrompt = _prompt.PromptCalendar(Resources.AdrPlus.NewAdrPromptSelectDate, DateTime.UtcNow, _config, cancellationToken);
                if (dateRefPrompt.IsAborted)
                {
                    throw new OperationCanceledException(Resources.AdrPlus.CancelledByUser);
                }
                var defDateRef = dateRefPrompt.Content;
                parsedArgs[Arguments.DateRefAdr] = $"{defDateRef.ToString("d", CultureInfo.GetCultureInfo(_config.Language))}";

                var (_, Top) = _prompt.PromptCursorPosition();
                DisplayWizardSummary(folderPrompt.Content, Path.GetFileName(filenewsup.info.FileName), defDateRef, scopePrompt.Content.Trim(), domainPrompt.Content.Trim());
                var resultCnf = _prompt.PromptConfirm(Resources.AdrPlus.NewAdrPromptConfirmCreation, cancellationToken);
                _prompt.PromptMovePosition(0, Top);

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

        private void LogAndWriteErrors(string[] errors)
        {
            foreach (var error in errors)
            {
                LogMessages.LogCommandFailure(_logger, error);
                _prompt.PromptWriteError(error);
            }
        }

        private void DisplayWizardSummary(string rootpath, string fileref, DateTime defDateRef, string scope, string domain)
        {
            _prompt.PromptWriteSummary(Resources.AdrPlus.SelectRepo + ": " + rootpath);
            _prompt.PromptWriteSummary(Resources.AdrPlus.File + ": " + fileref);
            _prompt.PromptWriteSummary(Resources.AdrPlus.Date + ": " + defDateRef.ToString("d", CultureInfo.GetCultureInfo(_config.Language)));
            _prompt.PromptWriteSummary(Resources.AdrPlus.Scope + ": " + scope);
            _prompt.PromptWriteSummary(Resources.AdrPlus.Domain + ": " + domain);
            _prompt.PromptWriteSummary("");
        }
    }
}
