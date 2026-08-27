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

namespace AdrPlus.Commands.NewAdr
{
    /// <summary>
    /// Handles the <c>new</c> command to create a new Architecture Decision Record (ADR).
    /// Validates uniqueness of title, resolves the next sequence number, and writes
    /// the new <c>.md</c> file following the configured naming convention.
    /// </summary>
    internal sealed class NewAdrCommandHandler(
        ILogger<NewAdrCommandHandler> logger,
        IOptions<AdrPlusConfig> config,
        IFileSystemService fileSystem,
        IValidateConfig validateconfig,
        IConsoleWriter prompt,
        INewAdrPrompts newAdrPrompts,
        IAdrServices adrServices,
        IPluginManager pluginManager) : ICommandHandler
    {
        private readonly ILogger<NewAdrCommandHandler> _logger = logger;
        private readonly AdrPlusConfig _config = config.Value;
        private readonly IFileSystemService _filesystem = fileSystem;
        private readonly IConsoleWriter _prompt = prompt;
        private readonly INewAdrPrompts _newAdrPrompts = newAdrPrompts;
        private readonly IValidateConfig _validateconfig = validateconfig;
        private readonly IAdrServices _adrServices = adrServices;
        private readonly IPluginManager _pluginManager = pluginManager;
        private static readonly Arguments[] ValidCommandArgs =
            [Arguments.WizardNew,
             Arguments.TargetRepo,
             Arguments.TitleAdr, 
             Arguments.DomainAdr, 
             Arguments.ScopeAdr,
             Arguments.DateRefAdr,
             Arguments.OpenFile, 
             Arguments.Help];

        /// <summary>
        /// Executes the <c>new</c> command asynchronously to create a new ADR.
        /// When <c>--wizard</c> is specified the user is guided interactively;
        /// otherwise all required values are taken from command-line arguments.
        /// </summary>
        /// <param name="args">The raw command-line tokens (e.g. <c>--wizard</c>, <c>--path</c>, <c>--title</c>, <c>--domain</c>, <c>--scope</c>, <c>--refdate</c>, <c>--open</c>, <c>--help</c>).</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="args"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">Thrown when required arguments are missing or invalid.</exception>
        /// <exception cref="FileNotFoundException">Thrown when the repository template or configuration file is not found.</exception>
        /// <exception cref="DirectoryNotFoundException">Thrown when the specified target directory does not exist.</exception>
        /// <exception cref="InvalidOperationException">Thrown when an ADR with the same unique title already exists.</exception>
        /// <exception cref="InvalidDataException">Thrown when the repository configuration is structurally invalid.</exception>
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
                        "new",
                        ValidCommandArgs,
                        [
                            "adrplus new --wizard --open",
                            "adrplus new --path \"path/to/repository\" --title \"Title of ADR\" --domain \"Domain\" --scope \"Scope\" --refdate \"2026-01-01\"",
                        ]));
                    return;
                }


                var hasWizard = parsedArgs.ContainsKey(Arguments.WizardNew);
                AdrFileNameComponents[]? wizardAdrFiles = null;
                if (hasWizard)
                {
                    var openafter = parsedArgs.ContainsKey(Arguments.OpenFile);
                    if (!openafter && _config.ComandOpenAdr.Length > 0)
                    {
                        openafter = true;
                    }
                    (parsedArgs, wizardAdrFiles) = await NewAdrWizard(openafter, cancellationToken);
                }

                parsedArgs.TryGetValue(Arguments.TargetRepo, out var targetPath);
                targetPath ??= string.Empty;

                if (!_filesystem.DirectoryExists(targetPath))
                {
                    throw new DirectoryNotFoundException(string.Format(null, FormatMessages.ErrDirectoryNotFound, targetPath));
                }

                var configPath = Path.GetFullPath(Path.Combine(targetPath, _validateconfig.GetFileNameRepoConfig()));
                if (!_filesystem.FileExists(configPath))
                {
                    throw new FileNotFoundException(Resources.AdrPlus.ErrorInitCommandNotExecuted);
                }

                string jsonString = await _filesystem.ReadAllTextAsync(configPath, cancellationToken);
                var (IsValid, ErrorReport) = _validateconfig.ValidateRepoStructure(jsonString);
                if (!IsValid)
                {
                    LogAndWriteErrors(ErrorReport);
                    throw new InvalidDataException(string.Format(null, FormatMessages.ErrInvalidRepositoryConfig, configPath));
                }
                var repoconfig = JsonSerializer.Deserialize<AdrPlusRepoConfig>(jsonString, AppConstants.RepoSerializerOptions)!;

                await _pluginManager.LoadPluginsAsync(cancellationToken);
                var (isActive, missingNames) = PluginActivationGate.Resolve(_pluginManager, repoconfig);

                var title = parsedArgs[Arguments.TitleAdr];
                var curpos = _prompt.PromptGetCursorPosition();
                if (hasWizard && wizardAdrFiles is null)
                {
                    _prompt.PromptWriteWait(Resources.AdrPlus.WaitReadFiles);
                }
                // Reuses the wizard's own read (for the confirmed folder) instead of reading the repository
                // again here - title-uniqueness and next-number both come from the same single read.
                var adrFiles = wizardAdrFiles ?? await _adrServices.ReadAllAdr(_filesystem, targetPath, repoconfig, includeContent: false);
                if (hasWizard && wizardAdrFiles is null)
                {
                    _prompt.PromptClearWaitText(curpos);
                }
                var existfile = _adrServices.GetFileByUniqueTitleFrom(title, adrFiles, repoconfig);
                if (!string.IsNullOrEmpty(existfile))
                {
                    throw new InvalidOperationException(string.Format(null, FormatMessages.ErrAdrUniqueTitleAlreadyExists, Path.GetFileName(existfile)));
                }

                var nextNumber = _adrServices.GetNextNumberFrom(adrFiles);

                var dateAdr = ParseDateReference(parsedArgs);

                var adrRecord = CreateAdrRecord(nextNumber, parsedArgs, dateAdr, repoconfig);
                var filename = adrRecord.GetFileName(repoconfig);
                var folder = Path.GetFullPath(Path.Combine(targetPath, repoconfig.FolderAdr));
                var filePath = _filesystem.GetFullNameFile(Path.Combine(folder, filename));
                if (_filesystem.FileExists(filePath))
                {
                    throw new InvalidOperationException(string.Format(null, FormatMessages.ErrFileAlreadyExists, Path.GetFileName(filePath)));
                }
                var content = $"{adrRecord.GetHeader(repoconfig)}{adrRecord.Template}";
                await _filesystem.WriteAllTextAsync(filePath, content, cancellationToken);

                PluginActivationGate.WarnMissingActivePlugins(_logger, _prompt, missingNames);
                LogMessages.LogCommandSuccessful(_logger, filePath);
                _prompt.PromptWriteSuccess($"{repoconfig.StatusNew} : {filePath}");

                await _pluginManager.DispatchAsync(AdrEventType.Created, adrRecord.ToSnapshot(), filePath, () => content, repoconfig.ToSnapshot(), Path.Combine(targetPath, "plugins-state"), isReplay: false, isActive: isActive, cancellationToken: cancellationToken);

                OpenAdrFileIfRequested(parsedArgs, filePath);
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

        private void LogAndWriteError(string message)
        {
            LogMessages.LogCommandFailure(_logger, message);
            _prompt.PromptWriteError(message);
        }

        /// <summary>
        /// Parses the date reference from <paramref name="parsedArgs"/> using the configured application culture.
        /// Returns <see cref="DateTime.UtcNow"/> when no date argument was provided.
        /// </summary>
        /// <param name="parsedArgs">The dictionary of parsed command-line arguments.</param>
        /// <returns>The parsed <see cref="DateTime"/>, or <see cref="DateTime.UtcNow"/> when the argument is absent.</returns>
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
                throw new FormatException(string.Format(null, FormatMessages.ErrInvalidDateFormat, _config.Language));
            }
            return dateAdr;
        }

        /// <summary>
        /// Builds an <see cref="AdrRecord"/> with <see cref="AdrStatus.Proposed"/> status from the
        /// parsed command arguments and the repository configuration defaults (version length, revision length, template).
        /// </summary>
        /// <param name="nextNumber">The sequence number to assign to the new ADR.</param>
        /// <param name="parsedArgs">The parsed command arguments supplying title, scope, domain, and date.</param>
        /// <param name="dateAdr">The creation date to record.</param>
        /// <param name="auxconfig">The repository configuration providing version/revision lengths and the default template.</param>
        /// <returns>A fully initialized <see cref="AdrRecord"/> ready to be written to disk.</returns>
        private static AdrRecord CreateAdrRecord(int nextNumber, Dictionary<Arguments, string> parsedArgs, DateTime dateAdr, AdrPlusRepoConfig auxconfig)
        {
            var title = parsedArgs[Arguments.TitleAdr];
            var scope = parsedArgs.TryGetValue(Arguments.ScopeAdr, out string? value) ? value : string.Empty;
            var domain = parsedArgs.TryGetValue(Arguments.DomainAdr, out string? valueDomain) ? valueDomain : string.Empty;

            return new AdrRecord
            {
                Number = nextNumber,
                Title = title,
                Scope = scope,
                Domain = domain,
                StatusCreate = AdrStatus.Proposed,
                CreateRef = dateAdr,
                Version = 1,
                Revision = auxconfig.LenRevision == 0 ? null : 1,
                Template = auxconfig.Template
            };
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
                LogAndWriteError(msg);
            }
        }

        /// <summary>
        /// Runs the interactive wizard for the <c>new</c> command, prompting the user to select a drive,
        /// repository folder, title, date, scope, and domain.
        /// The wizard loops until the user confirms the selection.
        /// </summary>
        /// <param name="isOpenAdr">When <see langword="true"/>, the <see cref="Arguments.OpenFile"/> flag is pre-populated in the result.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>
        /// The parsed arguments ready to be consumed by <see cref="ExecuteAsync"/>, alongside the
        /// <see cref="AdrFileNameComponents"/> array already read for the confirmed folder - reused by
        /// <see cref="ExecuteAsync"/> for the title-uniqueness/next-number checks instead of reading again.
        /// </returns>
        /// <exception cref="OperationCanceledException">Thrown when the user cancels any wizard prompt.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the repository configuration at the selected folder is invalid.</exception>
        private async Task<(Dictionary<Arguments, string> ParsedArgs, AdrFileNameComponents[] AdrFiles)> NewAdrWizard(bool isOpenAdr, CancellationToken cancellationToken)
        {
            var parsedArgs = new Dictionary<Arguments, string>();
            string defFolder;
            string defTitle = string.Empty;
            string defScope = string.Empty;
            string defDomain = string.Empty;
            DateTime defDateRef = DateTime.UtcNow;
            var oldDefFolder = string.Empty;
            string[] defArrScope = [];
            string[] defArrDomain = [];
            AdrFileNameComponents[] defArrAdrFiles = [];

            while (true)
            {
                parsedArgs.Clear();

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

                var folderPrompt = _prompt.PromptSelectFolderPath(Resources.AdrPlus.PromptSelectRepositoryPath, true, rootPath, _filesystem, _validateconfig,  cancellationToken);
                if (folderPrompt.IsAborted)
                {
                    throw new OperationCanceledException(Resources.AdrPlus.CancelledByUser);
                }

                var configPath = Path.Combine(folderPrompt.Content, _validateconfig.GetFileNameRepoConfig());
                string jsonString = await _filesystem.ReadAllTextAsync(configPath, cancellationToken);
                var (IsValid, _) = _validateconfig.ValidateRepoStructure(jsonString);

                if (!IsValid)
                {
                    throw new InvalidOperationException(string.Format(null, FormatMessages.ErrConfigFileInvalid, _filesystem.GetFullNameFile(configPath)));
                }

                var auxconfig = JsonSerializer.Deserialize<AdrPlusRepoConfig>(jsonString, AppConstants.RepoSerializerOptions)!;

                parsedArgs[Arguments.TargetRepo] = folderPrompt.Content;
                defFolder = folderPrompt.Content;

                var titlePrompt = _newAdrPrompts.PromptEditTitleAdr(defTitle, cancellationToken);
                if (titlePrompt.IsAborted)
                {
                    throw new OperationCanceledException(Resources.AdrPlus.CancelledByUser);
                }
                parsedArgs[Arguments.TitleAdr] = titlePrompt.Content.Trim();
                defTitle = titlePrompt.Content.Trim();

                var dateRefPrompt = _prompt.PromptCalendar(Resources.AdrPlus.NewAdrPromptSelectDate, defDateRef, _config, cancellationToken);
                if (dateRefPrompt.IsAborted)
                {
                    throw new OperationCanceledException(Resources.AdrPlus.CancelledByUser);
                }
                defDateRef = dateRefPrompt.Content;
                parsedArgs[Arguments.DateRefAdr] = $"{defDateRef.ToString("d", CultureInfo.GetCultureInfo(_config.Language))}";

                // Get scope and domain (free-text header fields, always optional; suggestions from
                // values already used in this repo are advisory only and never restrict the input).
                // Reads the repository once per folder selection - the array is reused below for
                // title-uniqueness/next-number too, instead of each lookup re-reading the same directory.
                if (oldDefFolder != defFolder)
                {
                    defArrAdrFiles = await _adrServices.ReadAllAdr(_filesystem, folderPrompt.Content, auxconfig, includeContent: false);

                    var (ScopesAborted, scopes, scopesException) = _newAdrPrompts.PromptGetArrayScopesAdr(defArrAdrFiles, cancellationToken);
                    if (ScopesAborted)
                    {
                        throw new OperationCanceledException(Resources.AdrPlus.CancelledByUser);
                    }
                    if (scopesException != null)
                    {
                        LogMessages.LogError(_logger, $"Failed to read registered scopes for suggestions: {scopesException.Message}");
                    }
                    var (DomainsAborted, domains, domainsException) = _newAdrPrompts.PromptGetArrayDomainsAdr(defArrAdrFiles, cancellationToken);
                    if (DomainsAborted)
                    {
                        throw new OperationCanceledException(Resources.AdrPlus.CancelledByUser);
                    }
                    if (domainsException != null)
                    {
                        LogMessages.LogError(_logger, $"Failed to read registered domains for suggestions: {domainsException.Message}");
                    }
                    oldDefFolder = defFolder;
                    defArrScope = scopes;
                    defArrDomain = domains;
                }

                var scopePrompt = _newAdrPrompts.PromptEditScopeAdr(defScope, defArrScope, cancellationToken);
                if (scopePrompt.IsAborted)
                {
                    throw new OperationCanceledException(Resources.AdrPlus.CancelledByUser);
                }
                parsedArgs[Arguments.ScopeAdr] = scopePrompt.Content.Trim();
                defScope = scopePrompt.Content.Trim();

                var domainPrompt = _newAdrPrompts.PromptEditDomainAdr(defDomain, defArrDomain, cancellationToken);
                if (domainPrompt.IsAborted)
                {
                    throw new OperationCanceledException(Resources.AdrPlus.CancelledByUser);
                }
                parsedArgs[Arguments.DomainAdr] = domainPrompt.Content.Trim();
                defDomain = domainPrompt.Content.Trim();

                if (isOpenAdr)
                {
                    parsedArgs[Arguments.OpenFile] = string.Empty;
                }

                var (_, Top) = _prompt.PromptCursorPosition();
                DisplayWizardSummary(parsedArgs, defDateRef);
                var resultCnf = _prompt.PromptConfirm(Resources.AdrPlus.NewAdrPromptConfirmCreation, cancellationToken);
                _prompt.PromptMovePosition(0, Top);
                if (resultCnf.IsAborted)
                {
                    throw new OperationCanceledException(Resources.AdrPlus.CancelledByUser);
                }

                if (resultCnf.ConfirmYes)
                {
                    return (parsedArgs, defArrAdrFiles);
                }
            }
        }

        private void DisplayWizardSummary(Dictionary<Arguments, string> parsedArgs, DateTime defDateRef)
        {
            _prompt.PromptWriteInfo($"{Resources.AdrPlus.SelectRepo} : {parsedArgs[Arguments.TargetRepo]}");
            _prompt.PromptWriteInfo($"{Resources.AdrPlus.Date} : {defDateRef.ToString("d", CultureInfo.GetCultureInfo(_config.Language))}");
            _prompt.PromptWriteInfo($"{Resources.AdrPlus.Title} : {parsedArgs[Arguments.TitleAdr]}");

            if (parsedArgs.TryGetValue(Arguments.ScopeAdr, out string? scope))
            {
                _prompt.PromptWriteInfo($"{Resources.AdrPlus.Scope} : {scope}");
            }

            if (parsedArgs.TryGetValue(Arguments.DomainAdr, out string? domain))
            {
                _prompt.PromptWriteInfo($"{Resources.AdrPlus.Domain} : {domain}");
            }
            _prompt.PromptWriteInfo("");
        }

    }
}
