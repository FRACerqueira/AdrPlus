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
using AdrPlus.Plugins;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Text.Json;

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
    /// <param name="fileSystem">The file system service for I/O operations.</param>
    /// <param name="validateconfig">The service for validating and loading JSON configuration files.</param>
    /// <param name="prompt">The console writer for displaying output and prompting user input.</param>
    /// <param name="adrServices">The ADR services for argument parsing, command metadata, and config deserialization.</param>
    /// <param name="pluginManager">The plugin manager used to load whatever plugins end up under the new repo's <c>./plugins</c>, to record the <c>activeplugins</c> baseline.</param>
    /// <param name="builtinPluginsRoot">
    /// The folder containing plugins bundled with the adrplus package itself (e.g. <c>plugins-builtin</c> next
    /// to the tool's own assembly), or empty to disable installing any of them. Left empty by default so tests
    /// that construct this handler directly never touch the real file system for this step.
    /// </param>
    internal sealed class InitCommandHandler(
        ILogger<InitCommandHandler> logger,
        IFileSystemService fileSystem,
        IValidateConfig validateconfig,
        IConsoleWriter prompt,
        IAdrServices adrServices,
        IPluginManager pluginManager,
        string builtinPluginsRoot = "") : ICommandHandler
    {
        private readonly ILogger<InitCommandHandler> _logger = logger;
        private readonly IFileSystemService _fileSystem = fileSystem;
        private readonly IConsoleWriter _prompt = prompt;
        private readonly IValidateConfig _validateconfig = validateconfig;
        private readonly IAdrServices _adrServices = adrServices;
        private readonly IPluginManager _pluginManager = pluginManager;
        private readonly string _builtinPluginsRoot = builtinPluginsRoot;
        private static readonly Arguments[] ValidCommandArgs = [
            Arguments.WizardInit, 
            Arguments.TargetRepo,
            Arguments.FileConfig,
            Arguments.Help];

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
            try
            {
                ArgumentNullException.ThrowIfNull(args);
                var parsedArgs = _adrServices.ParseArgs(args, ValidCommandArgs);
                if (parsedArgs.ContainsKey(Arguments.Help))
                {
                    _prompt.PromptWriteHelp(_adrServices.GetHelpText(
                        "init",
                        ValidCommandArgs,
                        [
                            "adrplus init --wizard",
                            "adrplus init --path \"path/to/repository\"",
                            "adrplus init --path \"path/to/repository\" -file \"path/to/file-config\"",
                        ]));
                    return;
                }

                if (parsedArgs.ContainsKey(Arguments.WizardInit))
                {
                    parsedArgs =  InitWizard(cancellationToken);
                }

                parsedArgs.TryGetValue(Arguments.FileConfig, out var fileConfig);
                fileConfig ??= string.Empty;

                parsedArgs.TryGetValue(Arguments.TargetRepo, out var targetPath);
                targetPath ??= string.Empty;


                if (!_fileSystem.DirectoryExists(targetPath))
                {
                    throw new DirectoryNotFoundException(string.Format(null, FormatMessages.ErrDirectoryNotFound, targetPath));
                }

                LogMessages.LogInitializingRepository(_logger, targetPath);

                var result = await InitializeRepositoryAsync(targetPath, fileConfig, cancellationToken);
                foreach (var item in result)
                {
                    LogMessages.LogCommandSuccessful(_logger, item);
                    _prompt.PromptWriteSuccess(item);
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
                var (IsAborted, Content) = _prompt.PromptSelectLogicalDrive(Resources.AdrPlus.NewAdrPromptSelectDrive, _fileSystem, cancellationToken);
                if (IsAborted)
                {
                    throw new OperationCanceledException(Resources.AdrPlus.CancelledByUser);
                }
                rootPath = Content;
            }

            var folderPrompt = _prompt.PromptSelectFolderPath(Resources.AdrPlus.PromptSelectRepositoryPath, false, rootPath, _fileSystem, _validateconfig, cancellationToken);
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
        /// <param name="fileConfig">The path to the configuration file.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>An array of fully qualified paths for all files and directories that were created.</returns>
        /// <exception cref="InvalidOperationException">Thrown when a configuration file already exists at the computed config path.</exception>
        private async Task<string[]> InitializeRepositoryAsync(string targetPath, string fileConfig, CancellationToken cancellationToken)
        {
            var result = new List<string>();

            var configPath = Path.GetFullPath(Path.Combine(targetPath, _validateconfig.GetFileNameRepoConfig()));
            string jsonrepoconfig;
            string filecfg;
            if (_fileSystem.FileExists(configPath) && fileConfig.Length == 0)
            {
                filecfg = configPath;
                var (IsAborted, ConfirmYes) = _prompt.PromptConfirm(Resources.AdrPlus.InitCmdConfigUpdateFileAlreadyExists, cancellationToken);
                if (IsAborted)
                {
                    throw new OperationCanceledException(Resources.AdrPlus.CancelledByUser, cancellationToken);
                }
                if (!ConfirmYes)
                {
                    throw new InvalidOperationException(string.Format(null, FormatMessages.ErrConfigFileAlreadyExists, configPath));
                }
                filecfg = _validateconfig.GetDefaultConfigRepoFilePath();
                jsonrepoconfig = await _validateconfig.GetConfigDefaultRepoContentAsync(AppConstants.DefaultFolderAdr, cancellationToken);
            }
            else
            {
                if (fileConfig.Length > 0)
                {
                    filecfg = Path.GetFullPath(fileConfig);
                    if (!_fileSystem.FileExists(fileConfig))
                    {
                        throw new FileNotFoundException(string.Format(null, FormatMessages.ErrFileNotFound, fileConfig));
                    }
                    jsonrepoconfig = await _fileSystem.ReadAllTextAsync(filecfg, cancellationToken);
                }
                else
                {
                    filecfg = _validateconfig.GetDefaultConfigRepoFilePath();
                    jsonrepoconfig = await _fileSystem.ReadAllTextAsync(filecfg, cancellationToken);
                }
            }

            var (isValid, errorMessage) = _validateconfig.ValidateRepoStructure(jsonrepoconfig);
            if (!isValid)
            {
                LogMessages.LogInvalidRepoConfiguration(_logger, string.Join("; ", errorMessage));
                throw new InvalidOperationException(string.Format(null, FormatMessages.ErrInvalidRepositoryConfig, filecfg));
            }
            var newrepoconfig = JsonSerializer.Deserialize<AdrPlusRepoConfig>(jsonrepoconfig, AppConstants.RepoSerializerOptions)!;

            //ensure if new configutation is valid (check existent number,version and revision)
            (int maxnumber, int maxversion, int maxrevision) = await _validateconfig.GetMaxNumberVersionRevision(targetPath, newrepoconfig);
            if (maxnumber.ToString(CultureInfo.InvariantCulture).Length > newrepoconfig.LenSeq)
            {
                throw new InvalidOperationException(string.Format(null, FormatMessages.ErrNewLenSeqGreaterThanConfig, maxnumber, newrepoconfig.LenSeq));
            }
            if (maxversion.ToString(CultureInfo.InvariantCulture).Length > newrepoconfig.LenVersion)
            {
                throw new InvalidOperationException(string.Format(null, FormatMessages.ErrNewLenVersionGreaterThanConfig, maxversion, newrepoconfig.LenVersion));
            }
            if (maxrevision.ToString(CultureInfo.InvariantCulture).Length > newrepoconfig.LenRevision && newrepoconfig.LenRevision > 0)
            {
                throw new InvalidOperationException(string.Format(null, FormatMessages.ErrNewLenRevisionGreaterThanConfig, maxrevision, newrepoconfig.LenRevision));
            }

            await CreateNewConfigAsync(jsonrepoconfig, targetPath, result, cancellationToken);

            return [.. result];
        }

        /// <summary>
        /// Reads the default repository configuration template, validates its structure, writes it to
        /// <paramref name="rootrepoPath"/>, and creates scope sub-folders when required.
        /// </summary>
        /// <param name="jsonrepoconfig">The JSON string representing the repository configuration.</param>
        /// <param name="rootrepoPath">The destination root path repository for the new configuration file.</param>
        /// <param name="result">The list to which the created file path (and any scope folder paths) are appended.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <exception cref="InvalidOperationException">Thrown when the default configuration template fails structure validation.</exception>
        private async Task CreateNewConfigAsync(string jsonrepoconfig, string rootrepoPath,  List<string> result, CancellationToken cancellationToken)
        {
            var filepath = Path.GetFullPath(Path.Combine(rootrepoPath, _validateconfig.GetFileNameRepoConfig()));
            await _fileSystem.WriteAllTextAsync(filepath, jsonrepoconfig, cancellationToken);
            result.Add(filepath);

            var config = _adrServices.FromJson(jsonrepoconfig, "")!;

            var folderadr = Path.GetFullPath(Path.Combine(rootrepoPath, config.FolderAdr));
            if (!_fileSystem.DirectoryExists(folderadr))
            {
                _fileSystem.CreateDirectory(folderadr);
                result.Add(folderadr);
            }
            CreateScopeDirectories(config, folderadr, result);
            InstallBuiltinPlugins(rootrepoPath, result);
            await WriteActivePluginsBaselineAsync(rootrepoPath, filepath, cancellationToken);
        }

        /// <summary>
        /// Loads whatever plugins ended up under <paramref name="rootrepoPath"/>'s <c>./plugins</c> (including
        /// any just installed by <see cref="InstallBuiltinPlugins"/>) and records their names as the repo's
        /// <c>activeplugins</c> baseline — the set <see cref="Plugins.PluginActivationGate"/> treats as expected
        /// going forward. A no-op when nothing is loaded (fresh repos with no plugins keep <c>activeplugins: []</c>,
        /// their default from <see cref="AdrPlusRepoConfig"/>).
        /// </summary>
        /// <remarks>
        /// Patches the already-written config file's <c>activeplugins</c> key via <see cref="ActivePluginsWriter"/>
        /// rather than re-serializing a deserialized <see cref="AdrPlusRepoConfig"/> — the object built from
        /// <c>jsonrepoconfig</c> further up (<c>_adrServices.FromJson(jsonrepoconfig, "")</c>) passes an empty
        /// string as its template argument, and round-tripping that object back to JSON would silently blank the
        /// repo's real ADR template content.
        /// </remarks>
        /// <param name="rootrepoPath">The root directory of the repository being initialized.</param>
        /// <param name="configFilePath">The full path of the config file already written by <see cref="CreateNewConfigAsync"/>.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        private async Task WriteActivePluginsBaselineAsync(string rootrepoPath, string configFilePath, CancellationToken cancellationToken)
        {
            await _pluginManager.LoadPluginsAsync(Path.Combine(rootrepoPath, "plugins"), cancellationToken);
            if (_pluginManager.LoadedPlugins.Count == 0)
            {
                return;
            }

            var names = _pluginManager.LoadedPlugins.Select(plugin => plugin.Manifest.Name!).ToArray();
            await ActivePluginsWriter.WriteAsync(_fileSystem, configFilePath, names, cancellationToken);
        }

        /// <summary>
        /// Copies the AdrIndexer reference plugin bundled with the adrplus package itself into the new
        /// repository's <c>plugins/adr-indexer</c> folder, so every repo gets it out of the box.
        /// </summary>
        /// <remarks>
        /// Uses raw <see cref="System.IO"/> calls rather than <see cref="IFileSystemService"/>: like the plugin
        /// assembly load itself (see <c>PluginLoader.LoadAssembly</c>), copying a binary plugin DLL is outside
        /// that abstraction's text/config-file scope. Does nothing when <see cref="_builtinPluginsRoot"/> is
        /// empty (the default for handlers built directly in tests) or its <c>adr-indexer</c> subfolder is absent.
        /// Never overwrites a file that already exists at the destination — <c>init</c> can run again against an
        /// existing repo, and a plugin's own <c>settings</c> (e.g. <c>outputFileName</c>) may have been hand-edited.
        /// </remarks>
        /// <param name="rootrepoPath">The root directory of the repository being initialized.</param>
        /// <param name="result">The list to which the copied file paths are appended.</param>
        private void InstallBuiltinPlugins(string rootrepoPath, List<string> result)
        {
            if (string.IsNullOrEmpty(_builtinPluginsRoot))
            {
                return;
            }

            var sourceDir = Path.Combine(_builtinPluginsRoot, "adr-indexer");
            if (!Directory.Exists(sourceDir))
            {
                return;
            }

            var destDir = Path.Combine(rootrepoPath, "plugins", "adr-indexer");
            Directory.CreateDirectory(destDir);
            foreach (var sourceFile in Directory.EnumerateFiles(sourceDir))
            {
                var destFile = Path.Combine(destDir, Path.GetFileName(sourceFile));
                if (File.Exists(destFile))
                {
                    continue;
                }

                File.Copy(sourceFile, destFile, overwrite: false);
                result.Add(destFile);
            }
        }
    }
}
