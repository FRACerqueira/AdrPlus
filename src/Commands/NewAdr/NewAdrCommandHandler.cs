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

namespace AdrPlus.Commands.NewAdr
{
    /// <summary>
    /// Handles the <c>new</c> command to create a new Architecture Decision Record (ADR).
    /// Validates uniqueness of title+domain, resolves the next sequence number, and writes
    /// the new <c>.md</c> file following the configured naming convention.
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of the <see cref="NewAdrCommandHandler"/> class.
    /// </remarks>
    /// <param name="logger">The logger for recording command execution and errors.</param>
    /// <param name="config">The application configuration settings (folder, language, open command, etc.).</param>
    /// <param name="fileSystem">The file system service for I/O operations.</param>
    /// <param name="validateconfig">The service for validating and loading JSON configuration files.</param>
    /// <param name="console">The console writer for displaying output and prompting user input.</param>
    /// <param name="adrServices">The ADR services for argument parsing, ADR file operations, and command metadata.</param>
    internal sealed class NewAdrCommandHandler(
        ILogger<NewAdrCommandHandler> logger,
        IOptions<AdrPlusConfig> config,
        IFileSystemService fileSystem,
        IValidateJsonConfig validateconfig,
        IConsoleWriter console,
        IAdrServices adrServices) : ICommandHandler
    {
        private readonly ILogger<NewAdrCommandHandler> _logger = logger;
        private readonly AdrPlusConfig _config = config.Value;
        private readonly IFileSystemService _filesystem = fileSystem;
        private readonly IConsoleWriter _console = console;
        private readonly IValidateJsonConfig _validateconfig = validateconfig;
        private readonly IAdrServices _adrServices = adrServices;
        private static readonly Arguments[] ValidCommandArgs =
            [Arguments.WizardNew,
             Arguments.TargetRepo,
             Arguments.TitleAdr, 
             Arguments.DomainAdr, 
             Arguments.ScopeAdr,
             Arguments.DateRefAdr,
             Arguments.OpenAdr, 
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
        /// <exception cref="ArgumentException">Thrown when required arguments are missing, invalid, or scope/domain validation fails.</exception>
        /// <exception cref="FileNotFoundException">Thrown when the repository template or configuration file is not found.</exception>
        /// <exception cref="DirectoryNotFoundException">Thrown when the specified target directory does not exist.</exception>
        /// <exception cref="InvalidOperationException">Thrown when an ADR with the same unique title already exists.</exception>
        /// <exception cref="InvalidDataException">Thrown when the repository configuration is structurally invalid.</exception>
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
                        "new",
                        ValidCommandArgs,
                        [
                            "adrplus new --wizard --open",
                            "adrplus new --path \"path/to/repository\" --title \"Title of ADR\" --domain \"Domain\" --scope \"Scope\" --refdate \"2026-01-01\"",
                        ]));
                    return;
                }

                if (!_validateconfig.HasTemplateRepoFile())
                {
                    throw new FileNotFoundException(Resources.AdrPlus.ErrMsgTemplateRepoFileNotFound);
                }

                var hasWizard = parsedArgs.ContainsKey(Arguments.WizardNew);
                if (hasWizard)
                {
                    parsedArgs = await NewAdrWizard(parsedArgs.ContainsKey(Arguments.OpenAdr), cancellationToken);
                }

                var targetPath = Path.GetFullPath(parsedArgs[Arguments.TargetRepo]);

                if (!_filesystem.DirectoryExists(targetPath))
                {
                    throw new DirectoryNotFoundException(string.Format(null, FormatMessages.ExceptionDirectoryNotFound, targetPath));
                }

                var configPath = Path.GetFullPath(Path.Combine(targetPath, _config.FolderRepo, _validateconfig.GetFileNameRepoConfig()));
                if (!_filesystem.FileExists(configPath))
                {
                    throw new FileNotFoundException(Resources.AdrPlus.ErrorInitCommandNotExecuted);
                }

                string jsonString = await _filesystem.ReadAllTextAsync(configPath, cancellationToken);
                var (IsValid, ErrorReport) = _validateconfig.ValidateRepoStructure(jsonString);
                if (!IsValid)
                {
                    LogAndWriteErrors(ErrorReport);
                    throw new InvalidDataException(Resources.AdrPlus.ErrorInConfigFile);
                }
                var repoconfig = JsonSerializer.Deserialize<AdrPlusRepoConfig>(jsonString, AppConstants.RepoSerializerOptions)!;

                ValidateScopeAndDomain(repoconfig, parsedArgs);

                var title = parsedArgs[Arguments.TitleAdr];
                var domain = parsedArgs.TryGetValue(Arguments.DomainAdr, out string? valueDomain) ? valueDomain : string.Empty;
                var existfile = string.Empty;
                var curpos = _console.GetCursorPosition();
                if (hasWizard)
                {
                    _console.WriteWait(Resources.AdrPlus.WaitReadFiles);
                }
                existfile = await _adrServices.GetFileByUniqueTitle(title, domain, _filesystem, Path.Combine(targetPath, _config.FolderRepo), repoconfig);
                if (hasWizard)
                {
                    _console.ClearWait(curpos);
                }
                if (!string.IsNullOrEmpty(existfile))
                {
                    throw new InvalidOperationException(string.Format(null, FormatMessages.NewAdrErrorUniqueTitleAlreadyExists, Path.GetFileName(existfile)));
                }

                var folderRepo = Path.GetFullPath(Path.Combine(targetPath, _config.FolderRepo));
                curpos = _console.GetCursorPosition();
                if (hasWizard)
                {
                    _console.WriteWait(Resources.AdrPlus.WaitReadFiles);
                }
                var nextNumber = await _adrServices.GetNextNumber(_filesystem, folderRepo, repoconfig);
                if (hasWizard)
                {
                    _console.ClearWait(curpos);
                }

                // Parse date reference
                var dateAdr = ParseDateReference(parsedArgs);
    
                // Create ADR record and file
                var adrRecord = CreateAdrRecord(nextNumber, parsedArgs, dateAdr, repoconfig);
                var filePath = await CreateAdrFile(adrRecord, targetPath, repoconfig, cancellationToken);

                LogMessages.LogCommandSuccessful(_logger, filePath);
                _console.WriteSuccess($"{repoconfig.StatusNew} : {filePath}");

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
        /// Logs <paramref name="message"/> as a command failure and writes it to the console as an error.
        /// </summary>
        /// <param name="message">The error message to log and display.</param>
        private void LogAndWriteError(string message)
        {
            LogMessages.LogCommandFailure(_logger, message);
            _console.WriteError(message);
        }

        /// <summary>
        /// Validates the scope and domain arguments against the repository configuration.
        /// When <see cref="AdrPlusRepoConfig.LenScope"/> is zero, scope and domain are removed from
        /// <paramref name="parsedArgs"/> silently. Otherwise the scope must exist in the configured list
        /// and the domain must be provided unless the scope is in the skip-domain list.
        /// </summary>
        /// <param name="auxconfig">The repository configuration defining valid scopes and skip-domain rules.</param>
        /// <param name="parsedArgs">The parsed command arguments (modified in-place).</param>
        /// <exception cref="ArgumentException">
        /// Thrown when a required scope is missing, the scope is not in the configured list,
        /// or a required domain is missing for the given scope.
        /// </exception>
        private static void ValidateScopeAndDomain(AdrPlusRepoConfig auxconfig, Dictionary<Arguments, string> parsedArgs)
        {
            if (auxconfig.LenScope == 0)
            {
                parsedArgs.Remove(Arguments.ScopeAdr);
                parsedArgs.Remove(Arguments.DomainAdr);
                return;
            }

            if (!parsedArgs.TryGetValue(Arguments.ScopeAdr, out string? scopeArg))
            {
                throw new ArgumentException(string.Format(null, FormatMessages.ExceptionMissingRequiredArgument, "--scope", "-s"));
            }

            if (auxconfig.Scopes != null && !auxconfig.GetScopes().Any(x => x.Equals(scopeArg, StringComparison.OrdinalIgnoreCase)))
            {
                throw new ArgumentException(string.Format(null, FormatMessages.InvalidScopeError, scopeArg, auxconfig.Scopes));
            }

            string domainArg = parsedArgs.TryGetValue(Arguments.DomainAdr, out string? valueArg) ? valueArg : string.Empty;
            bool skipdomains = auxconfig.Getskipdomains().Any(x => x.Equals(scopeArg, StringComparison.OrdinalIgnoreCase));

            if (domainArg.Length == 0 && !skipdomains)
            {
                throw new ArgumentException(string.Format(null, FormatMessages.ExceptionMissingRequiredArgument, "--domain", "-d"));
            }

            if (domainArg.Length > 0 && skipdomains)
            {
                parsedArgs.Remove(Arguments.DomainAdr);
            }
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
                throw new FormatException(string.Format(null, FormatMessages.ErrorDateFormat, _config.Language));
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
            var folder = Path.Combine(targetPath, _config.FolderRepo);
            
            if (auxconfig.FolderByScope)
            {
                folder = Path.Combine(folder, adrRecord.Scope);
            }

            var filePath = _filesystem.GetFullNameFile(Path.Combine(folder, filename));
            var content = $"{adrRecord.GetHeader(auxconfig)}{adrRecord.Template}";
            await _filesystem.WriteAllTextAsync(filePath, content, cancellationToken);

            return filePath;
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
                LogAndWriteError(msg);
            }
        }

        /// <summary>
        /// Runs the interactive wizard for the <c>new</c> command, prompting the user to select a drive,
        /// repository folder, title, date, and (when configured) scope and domain.
        /// The wizard loops until the user confirms the selection.
        /// </summary>
        /// <param name="isOpenAdr">When <see langword="true"/>, the <see cref="Arguments.OpenAdr"/> flag is pre-populated in the result.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A dictionary of parsed arguments ready to be consumed by <see cref="ExecuteAsync"/>.</returns>
        /// <exception cref="OperationCanceledException">Thrown when the user cancels any wizard prompt.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the repository configuration at the selected folder is invalid.</exception>
        private async Task<Dictionary<Arguments, string>> NewAdrWizard(bool isOpenAdr, CancellationToken cancellationToken)
        {
            var parsedArgs = new Dictionary<Arguments, string>();
            var defDrive = string.Empty;
            var defFolder = string.Empty;
            var defTitle = string.Empty;
            var defScope = string.Empty;
            var defDomain = string.Empty;
            var defDateRef = DateTime.UtcNow;
            var oldDefFolder = string.Empty;
            string[] defArrDomain = [];

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
                    defDrive = rootPath;
                }

                // Select folder
                var folderPrompt = _console.PromptSelectFolderRepositoryPath(true, rootPath, _filesystem, _validateconfig, _config, cancellationToken);
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
                    throw new InvalidOperationException(string.Format(null, FormatMessages.ErrorInConfigFile, _filesystem.GetFullNameFile(configPath)));
                }

                var auxconfig = JsonSerializer.Deserialize<AdrPlusRepoConfig>(jsonString, AppConstants.RepoSerializerOptions)!;
                parsedArgs[Arguments.TargetRepo] = folderPrompt.Content;
                defFolder = folderPrompt.Content;

                // Get title
                var titlePrompt = _console.PromptEditTitleAdr(defTitle, cancellationToken);
                if (titlePrompt.IsAborted)
                {
                    throw new OperationCanceledException(Resources.AdrPlus.CancelledByUser);
                }
                parsedArgs[Arguments.TitleAdr] = titlePrompt.Content.Trim();
                defTitle = titlePrompt.Content.Trim();

                // Get date
                var dateRefPrompt = _console.PrompCalendar(Resources.AdrPlus.NewAdrPromptSelectDate, defDateRef, _config, cancellationToken);
                if (dateRefPrompt.IsAborted)
                {
                    throw new OperationCanceledException(Resources.AdrPlus.CancelledByUser);
                }
                defDateRef = dateRefPrompt.Content;
                parsedArgs[Arguments.DateRefAdr] = $"{defDateRef.ToString("d", CultureInfo.GetCultureInfo(_config.Language))}";

                // Get scope and domain if configured
                if (auxconfig.Scopes.Length > 0)
                {
                    var scopePrompt = _console.PromptEditScopeAdr(defScope, auxconfig, cancellationToken);
                    if (scopePrompt.IsAborted)
                    {
                        throw new OperationCanceledException(Resources.AdrPlus.CancelledByUser);
                    }
                    parsedArgs[Arguments.ScopeAdr] = scopePrompt.Content.Trim();
                    defScope = scopePrompt.Content.Trim();

                    if (!auxconfig.Getskipdomains().Any(x => x.Equals(scopePrompt.Content, StringComparison.OrdinalIgnoreCase)))
                    {
                        if (oldDefFolder != defFolder)
                        {
                            var (IsAborted, domains, _) = _console.PromptGetArrayDomainsAdr(_filesystem, folderPrompt.Content, _config, auxconfig, cancellationToken);
                            if (IsAborted)
                            {
                                throw new OperationCanceledException(Resources.AdrPlus.CancelledByUser);
                            }
                            oldDefFolder = defFolder;
                            defArrDomain = domains;
                        }

                        var domainPrompt = _console.PromptEditDomainAdr(defDomain, defArrDomain, cancellationToken);
                        if (domainPrompt.IsAborted)
                        {
                            throw new OperationCanceledException(Resources.AdrPlus.CancelledByUser);
                        }
                        parsedArgs[Arguments.DomainAdr] = domainPrompt.Content.Trim();
                        defDomain = domainPrompt.Content.Trim();
                    }
                }

                if (isOpenAdr)
                {
                    parsedArgs[Arguments.OpenAdr] = string.Empty;
                }

                // Display summary and confirm
                DisplayWizardSummary(parsedArgs, defDateRef);

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
        /// Displays a summary of the wizard selections (repository, date, title, and optional scope/domain)
        /// before the user confirms the ADR creation.
        /// </summary>
        /// <param name="parsedArgs">The parsed command arguments containing the selected values.</param>
        /// <param name="defDateRef">The reference date for the new ADR.</param>
        private void DisplayWizardSummary(Dictionary<Arguments, string> parsedArgs, DateTime defDateRef)
        {
            _console.WriteInfo($"{Resources.AdrPlus.RE} : {parsedArgs[Arguments.TargetRepo]}");
            _console.WriteInfo($"{Resources.AdrPlus.Date} : {defDateRef.ToString("d", CultureInfo.GetCultureInfo(_config.Language))}");
            _console.WriteInfo($"{Resources.AdrPlus.Title} : {parsedArgs[Arguments.TitleAdr]}");

            if (parsedArgs.TryGetValue(Arguments.ScopeAdr, out string? scope))
            {
                _console.WriteInfo($"{Resources.AdrPlus.Scope} : {scope}");
            }

            if (parsedArgs.TryGetValue(Arguments.DomainAdr, out string? domain))
            {
                _console.WriteInfo($"{Resources.AdrPlus.Domain} : {domain}");
            }
            _console.WriteInfo("");
        }

    }
}
