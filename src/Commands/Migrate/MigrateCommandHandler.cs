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
using System.Text.Json;

namespace AdrPlus.Commands.Migrate
{
    internal sealed class MigrateCommandHandler(
        ILogger<MigrateCommandHandler> logger,
        IOptions<AdrPlusConfig> config,
        IFileSystemService fileSystem,
        IValidateJsonConfig validateConfig,
        IConsoleWriter console,
        IAdrServices adrServices) : ICommandHandler
    {
        private readonly ILogger<MigrateCommandHandler> _logger = logger;
        private readonly IOptions<AdrPlusConfig> _config = config;
        private readonly IFileSystemService _fileSystem = fileSystem;
        private readonly IValidateJsonConfig _validateConfig = validateConfig;
        private readonly IConsoleWriter _console = console;
        private readonly IAdrServices _adrServices = adrServices;
        private static readonly Arguments[] ValidCommandArgs =
            [Arguments.WizardMigrate,
             Arguments.TargetRepo,
             Arguments.MatchAdr,
             Arguments.Help];

        public async Task ExecuteAsync(string[] args, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(args);
            var parsedArgs = _adrServices.ParseArgs(args, ValidCommandArgs);
            if (parsedArgs.ContainsKey(Arguments.Help))
            {
                _console.WriteHelp(_adrServices.GetHelpText(
                    "migrate",
                    ValidCommandArgs,
                    [
                        "adrplus migrate --wizard",
                        "adrplus migrate --path \"path/to/repository/\" --match 10",
                    ]));
                return;
            }

            if (!_validateConfig.HasTemplateRepoFile())
            {
                throw new FileNotFoundException(Resources.AdrPlus.ErrMsgTemplateRepoFileNotFound);
            }

            var hasWizard = parsedArgs.ContainsKey(Arguments.WizardRepo);
            if (hasWizard)
            {
                parsedArgs = MigrateWizard(cancellationToken);
            }

            if (!parsedArgs.TryGetValue(Arguments.MatchAdr, out var matchAdrValue))
            {
                throw new ArgumentException("ErrMsgMatchAdrRequired");
            }
            if (!int.TryParse(matchAdrValue, out int matchAdr))
            {
                throw new ArgumentException("ErrMsgInvalidMatchAdrValue");
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

            var foundfiles = await _adrServices.ReadAllAdr(_fileSystem, targetPath, repoconfig, true);

            if (foundfiles.Count(x => x.IsValid && !x.Header.IsValid) != matchAdr)
            { 
                throw new InvalidOperationException("ErrMsgMatchAdrCount");
            }

            throw new NotImplementedException("teste");
        }

        /// <summary>
        /// Runs the interactive wizard for the <c>init</c> command, prompting the user to select a
        /// logical drive (when more than one is available) and then a target repository folder.
        /// </summary>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A dictionary of parsed arguments pre-populated with <see cref="Arguments.TargetRepo"/>.</returns>
        /// <exception cref="OperationCanceledException">Thrown when the user cancels any wizard prompt.</exception>
        private Dictionary<Arguments, string> MigrateWizard(CancellationToken cancellationToken)
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

            var folderPrompt = _console.PromptSelectFolderRepositoryPath(false, rootPath, _fileSystem, _validateConfig, cancellationToken);
            if (folderPrompt.IsAborted)
            {
                throw new OperationCanceledException(Resources.AdrPlus.CancelledByUser);
            }

            parsedArgs[Arguments.TargetRepo] = folderPrompt.Content;
            return parsedArgs;
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
    }
}
