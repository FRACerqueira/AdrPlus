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

namespace AdrPlus.Commands.Init
{
    /// <summary>
    /// Handles the <c>init</c> command, which initializes the ADR repository structure by creating
    /// the configuration file and optional scope sub-folders at the specified target path.
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of the <see cref="InitCommandHandler"/> class.
    /// </remarks>
    /// <param name="logger">The logger for recording command execution and errors.</param>
    /// <param name="appconfig">The application configuration settings (folder, date format, etc.).</param>
    /// <param name="fileSystem">The file system service for I/O operations.</param>
    /// <param name="validateconfig">The service for validating and loading JSON configuration files.</param>
    /// <param name="console">The console writer for displaying output and prompting user input.</param>
    /// <param name="adrServices">The ADR services for argument parsing, command metadata, and config deserialization.</param>
    internal sealed class InitCommandHandler(
        ILogger<InitCommandHandler> logger,
        IOptions<AdrPlusConfig> appconfig,
        IFileSystemService fileSystem,
        IValidateJsonConfig validateconfig,
        IConsoleWriter console,
        IAdrServices adrServices) : ICommandHandler
    {
        private readonly ILogger<InitCommandHandler> _logger = logger;
        private readonly AdrPlusConfig _appconfig = appconfig.Value;
        private readonly IFileSystemService _fileSystem = fileSystem;
        private readonly IConsoleWriter _console = console;
        private readonly IValidateJsonConfig _validateconfig = validateconfig;
        private readonly IAdrServices _adrServices = adrServices;
        private static readonly Arguments[] ValidCommandArgs = [Arguments.WizardInit, Arguments.TargetRepo, Arguments.Help];

        /// <summary>
        /// Executes the <c>init</c> command asynchronously to initialize the ADR repository structure.
        /// When <c>--wizard</c> is specified the user is guided interactively to choose the target path;
        /// otherwise the path is taken directly from the <c>--path</c> argument.
        /// </summary>
        /// <param name="args">The raw command-line tokens (e.g. <c>--wizard</c>, <c>--path &lt;dir&gt;</c>, <c>--help</c>).</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="args"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">Thrown when required arguments are missing or invalid.</exception>
        /// <exception cref="FileNotFoundException">Thrown when the repository template file is not found.</exception>
        /// <exception cref="DirectoryNotFoundException">Thrown when the specified target directory does not exist.</exception>
        /// <exception cref="InvalidOperationException">Thrown when a configuration file already exists at the target path.</exception>
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
                        "init",
                        ValidCommandArgs,
                        [
                            "adrplus init --wizard",
                            "adrplus init --path \"path/to/repository\"",
                        ]));
                    return;
                }

                if (!_validateconfig.HasTemplateRepoFile())
                {
                    throw new FileNotFoundException(Resources.AdrPlus.ErrMsgTemplateRepoFileNotFound);
                }

                if (parsedArgs.ContainsKey(Arguments.WizardInit))
                {
                    parsedArgs =  InitWizard(cancellationToken);
                }

                parsedArgs.TryGetValue(Arguments.TargetRepo, out var targetPath);
                targetPath ??= string.Empty;

                LogMessages.LogInitializingRepository(_logger, targetPath);

                if (!_fileSystem.DirectoryExists(targetPath))
                {
                    throw new DirectoryNotFoundException(string.Format(null, FormatMessages.ExceptionDirectoryNotFound, targetPath));
                }

                var result = await InitializeRepositoryAsync(targetPath, cancellationToken);
                foreach (var item in result)
                {
                    LogMessages.LogCommandSuccessful(_logger, item);
                    _console.WriteSuccess(item);
                }
            }
            catch (Exception ex)
            {
                LogMessages.LogCommandException(_logger, ex);
                throw;
            }
        }

        /// <summary>
        /// Runs the interactive wizard for the <c>init</c> command, prompting the user to select a
        /// logical drive (when more than one is available) and then a target repository folder.
        /// </summary>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A dictionary of parsed arguments pre-populated with <see cref="Arguments.TargetRepo"/>.</returns>
        /// <exception cref="OperationCanceledException">Thrown when the user cancels any wizard prompt.</exception>
        private Dictionary<Arguments, string> InitWizard(CancellationToken cancellationToken)
        {
            var parsedArgs = new Dictionary<Arguments, string>();
            string[] drives = _fileSystem.GetDrives();
            var rootPath = drives[0];

            if (drives.Length > 1)
            {
                var (IsAborted, Content) = _console.PromptSelectLogicalDrive(Resources.AdrPlus.NewAdrPromptSelectDrive, _fileSystem, cancellationToken);
                if (IsAborted)
                {
                    throw new OperationCanceledException(Resources.AdrPlus.CancelledByUser);
                }
                rootPath = Content;
            }

            var folderPrompt = _console.PromptSelectFolderRepositoryPath(false, rootPath, _fileSystem, _validateconfig, _appconfig, cancellationToken);
            if (folderPrompt.IsAborted)
            {
                throw new OperationCanceledException(Resources.AdrPlus.CancelledByUser);
            }

            parsedArgs[Arguments.TargetRepo] = folderPrompt.Content;
            return parsedArgs;
        }

        /// <summary>
        /// Creates a sub-folder for each scope defined in <paramref name="config"/> when
        /// <see cref="AdrPlusRepoConfig.FolderByScope"/> is <see langword="true"/>.
        /// Folders that already exist are silently skipped.
        /// </summary>
        /// <param name="config">The repository configuration that defines scopes and the folder-by-scope flag.</param>
        /// <param name="repoPath">The root repository path under which scope folders are created.</param>
        /// <param name="result">The list to which the fully qualified paths of newly created directories are appended.</param>
        private void CreateScopeDirectories(AdrPlusRepoConfig config, string repoPath, List<string> result)
        {
            if (!config.FolderByScope)
            {
                return;
            }

            var scopes = config.Scopes.Split(';', StringSplitOptions.RemoveEmptyEntries);
            foreach (var scope in scopes)
            {
                var scopePath = Path.GetFullPath(Path.Combine(repoPath, scope));
                if (!_fileSystem.DirectoryExists(scopePath))
                {
                    var fullname = _fileSystem.CreateDirectory(scopePath);
                    result.Add(fullname);
                }
            }
        }

        /// <summary>
        /// Initializes the ADR repository structure at <paramref name="targetPath"/>:
        /// creates the ADR folder (when it does not exist), writes the configuration file,
        /// and creates optional scope sub-folders.
        /// </summary>
        /// <param name="targetPath">The root directory where the repository will be initialized.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>An array of fully qualified paths for all files and directories that were created.</returns>
        /// <exception cref="InvalidOperationException">Thrown when a configuration file already exists at the computed config path.</exception>
        private async Task<string[]> InitializeRepositoryAsync(string targetPath,CancellationToken cancellationToken)
        {
            var result = new List<string>();

            var repoPath = Path.GetFullPath(Path.Combine(targetPath, _appconfig.FolderRepo));
            if (!_fileSystem.DirectoryExists(repoPath))
            {
                repoPath = _fileSystem.CreateDirectory(repoPath);
            }
            var configPath = Path.GetFullPath(Path.Combine(repoPath, _validateconfig.GetFileNameRepoConfig()));
            if (_fileSystem.FileExists(configPath))
            {
                throw new InvalidOperationException(string.Format(null, FormatMessages.InitCmdConfigFileAlreadyExists, configPath));
            }

            await CreateNewConfigAsync(configPath, repoPath, result, cancellationToken);

            return [.. result];
        }

        /// <summary>
        /// Reads the default repository configuration template, validates its structure, writes it to
        /// <paramref name="configPath"/>, and creates scope sub-folders when required.
        /// </summary>
        /// <param name="configPath">The destination path for the new configuration file.</param>
        /// <param name="repoPath">The repository root path used for scope folder creation.</param>
        /// <param name="result">The list to which the created file path (and any scope folder paths) are appended.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <exception cref="InvalidOperationException">Thrown when the default configuration template fails structure validation.</exception>
        private async Task CreateNewConfigAsync(string configPath, string repoPath, List<string> result, CancellationToken cancellationToken)
        {
            var jsonrepoconfig = await _fileSystem.ReadAllTextAsync(_validateconfig.GetConfigRepoFilePath(), cancellationToken);
            var (isValid, errorMessage) =  _validateconfig.ValidateRepoStructure(jsonrepoconfig);

            if (!isValid)
            {
                LogMessages.LogInvalidRepoConfiguration(_logger, string.Join("; ", errorMessage));
                throw new InvalidOperationException(string.Format(null, FormatMessages.ErrMsgInvalidRepoConfig, _validateconfig.GetConfigRepoFilePath()));
            }

            await _fileSystem.WriteAllTextAsync(configPath, jsonrepoconfig, cancellationToken);
            result.Add(_fileSystem.GetFullNameFile(configPath));

            var config = _adrServices.FromJson(jsonrepoconfig, "", "")!;
            CreateScopeDirectories(config, repoPath, result);
        }
    }
}
