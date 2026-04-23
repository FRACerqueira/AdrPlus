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

namespace AdrPlus.Commands.Upgrade
{
    internal sealed class UpgradeCommandHandler(
        ILogger<UpgradeCommandHandler> logger,
        IOptions<AdrPlusConfig> config,
        IFileSystemService fileSystem,
        IValidateJsonConfig validateconfig,
        IConsoleWriter console,
        IAdrServices adrServices) : ICommandHandler
    {
        private readonly ILogger<UpgradeCommandHandler> _logger = logger;
        private readonly AdrPlusConfig _config = config.Value;
        private readonly IFileSystemService _filesystem = fileSystem;
        private readonly IConsoleWriter _console = console;
        private readonly IValidateJsonConfig _validateconfig = validateconfig;
        private readonly IAdrServices _adrServices = adrServices;
        private static readonly Arguments[] ValidCommandArgs =
            [Arguments.WizardRepo,
             Arguments.RepoTemplate,
             Arguments.RepoVersion,
             Arguments.RepoRevision,
             Arguments.RepoScope,
             Arguments.RepoScopeItems,
             Arguments.RepoWithFolders,
             Arguments.FileTemplate,
             Arguments.TargetRepoAdr,
             Arguments.Help];

        public async Task ExecuteAsync(string[] args, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(args);
            var parsedArgs = _adrServices.ParseArgs(args, ValidCommandArgs);
            if (parsedArgs.ContainsKey(Arguments.Help))
            {
                _console.WriteHelp(_adrServices.GetHelpText(
                    "upgrade",
                    ValidCommandArgs,
                    [
                        "adrplus upgrade --wizard",
                        "adrplus upgrade --template --path \"path/to/repository/folder-ADR\" --file \"path/to/template\"",
                        "adrplus upgrade --version 2 --path \"path/to/repository/folder-ADR\"",
                        "adrplus upgrade --scope 1 --path \\\"path/to/repository/folder-ADR\" --items \"list;of;scope\" --createfolders",
                    ]));
                return;
            }

            if (!_validateconfig.HasTemplateRepoFile())
            {
                throw new FileNotFoundException(Resources.AdrPlus.ErrMsgTemplateRepoFileNotFound);
            }

            var hasWizard = parsedArgs.ContainsKey(Arguments.WizardRepo);
            if (hasWizard)
            {
                parsedArgs = await UpgradeWizard(_config,cancellationToken);
            }

            parsedArgs.TryGetValue(Arguments.TargetRepoAdr, out var targetPathRepoconfig);
            targetPathRepoconfig ??= string.Empty;

            var configPath = Path.GetFullPath(Path.Combine(targetPathRepoconfig, _validateconfig.GetFileNameRepoConfig()));
            if (!_filesystem.FileExists(configPath))
            {
                throw new FileNotFoundException(string.Format(null, FormatMessages.ExceptionFileNotFound, configPath));
            }

            string jsonString = await _filesystem.ReadAllTextAsync(configPath, cancellationToken);
            var (IsValid, ErrorReport) = _validateconfig.ValidateRepoStructure(jsonString);
            if (!IsValid)
            {
                LogAndWriteErrors(ErrorReport);
                throw new InvalidDataException(string.Format(null, FormatMessages.ErrorInConfigFile, configPath));
            }
            var repoconfig = JsonSerializer.Deserialize<AdrPlusRepoConfig>(jsonString, AppConstants.RepoSerializerOptions)!;

            var changetemplate = parsedArgs.ContainsKey(Arguments.RepoTemplate);
            var filetemplate = string.Empty;
            if (changetemplate)
            {
                if (parsedArgs.TryGetValue(Arguments.FileTemplate, out filetemplate))
                {
                    filetemplate = Path.GetFullPath(filetemplate);
                    if (!_filesystem.FileExists(filetemplate))
                    {
                        throw new FileNotFoundException(string.Format(null, FormatMessages.ExceptionFileNotFound, filetemplate));
                    }
                    if (string.IsNullOrEmpty(Path.GetExtension(filetemplate)) || !Path.GetExtension(filetemplate).Equals(".md", StringComparison.OrdinalIgnoreCase))
                    {
                        throw new InvalidOperationException(Resources.AdrPlus.FileTemplateMusbeMd);
                    }
                    filetemplate = await _filesystem.ReadAllTextAsync(filetemplate, cancellationToken);  
                }
                else
                {
                    filetemplate = await _validateconfig.GetConfigAdrTemplateAsync(cancellationToken);
                }
            }
            else
            {
                if (parsedArgs.ContainsKey(Arguments.FileTemplate))
                { 
                    throw new ArgumentException(Resources.AdrPlus.ErrMsgFileTemplateNotFound);
                }
            }

            var changeversion = parsedArgs.ContainsKey(Arguments.RepoVersion);
            var lenversionvalue = -1;
            if (changeversion)
            {
                if (parsedArgs.TryGetValue(Arguments.RepoVersion, out string? value))
                {
                    if (int.TryParse(value, out int result) && result >= 2 && result <= 3)
                    {
                        lenversionvalue = result;

                    }
                    else
                    {
                        throw new ArgumentException(Resources.AdrPlus.ErrMsgRepoVersionValueMustBeIntegerAndRangeVersion);
                    }
                    if (lenversionvalue < repoconfig.LenVersion)
                    {
                        throw new ArgumentException(Resources.AdrPlus.ErrMsgRepoVersionValueMustBeGreaterThanCurrent);
                    }
                    else if (lenversionvalue == repoconfig.LenVersion)
                    { 
                        changeversion = false;
                        lenversionvalue = -1;
                    }
                }
                else
                { 
                    throw new ArgumentException(Resources.AdrPlus.ErrMsgRepoVersionValueNotFound);
                }
            }

            var changerevision = parsedArgs.ContainsKey(Arguments.RepoRevision);
            var lenrevisionvalue = -1;
            if (changerevision)
            {
                if (repoconfig.LenRevision > 0)
                {
                    throw new InvalidOperationException(Resources.AdrPlus.ErrMsgRepoRevisionAlreadySet);
                }
                if (parsedArgs.TryGetValue(Arguments.RepoRevision, out string? value))
                {
                    if (int.TryParse(value, out int result) && result >= 1 && result <= 3)
                    {
                        lenrevisionvalue = result;
                    }
                    else
                    {
                        throw new ArgumentException(Resources.AdrPlus.ErrMsgRepoRevisionValueMustBeIntegerAndRangeRevision);
                    }
                }
                else
                {
                    throw new ArgumentException(Resources.AdrPlus.ErrMsgRepoRevisionValueNotFound);
                }
            }

            var changescope = parsedArgs.ContainsKey(Arguments.RepoScope);
            var lenscopevalue = -1;
            var scopeitems = Array.Empty<string>();
            var skipitems = Array.Empty<string>();

            var withfolders = parsedArgs.ContainsKey(Arguments.RepoWithFolders);
            if (changescope)
            {
                if (repoconfig.LenScope > 0)
                {
                    throw new InvalidOperationException(Resources.AdrPlus.ErrMsgRepoScopeAlreadySet);
                }
                if (parsedArgs.TryGetValue(Arguments.RepoScope, out string? value))
                {
                    if (int.TryParse(value, out int result) && result >= 1  && result <=5)
                    {
                        lenscopevalue = result;
                    }
                    else
                    {
                        throw new ArgumentException(Resources.AdrPlus.ErrMsgRepoScopeValueMustBeIntegerAndRangeScope);
                    }
                }
                else
                { 
                    throw new ArgumentException(Resources.AdrPlus.ErrMsgRepoScopeValueNotFound);
                }

                if (!parsedArgs.TryGetValue(Arguments.RepoScopeItems, out var reposcopeitems))
                {
                    throw new ArgumentException(Resources.AdrPlus.ErrMsgRepoScopeitemsValueNotFound);
                }

                scopeitems = [.. reposcopeitems
                    .Split(';', StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => s.Replace("*","").Trim())];
                skipitems = [.. reposcopeitems
                     .Split(';', StringSplitOptions.RemoveEmptyEntries)
                     .Where(s => s.EndsWith("*", StringComparison.OrdinalIgnoreCase))
                     .Select(s => s.Replace("*", "").Trim())]; 
                var uniqueScopes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var minlen = scopeitems.Min(s => s.Length);
                foreach (var scope in scopeitems)
                {
                    if (!uniqueScopes.Add(scope))
                    {
                        throw new ArgumentException(Resources.AdrPlus.ErrMsgScopesDuplicateEntries);
                    }
                }
                if (lenscopevalue > minlen)
                {
                    lenscopevalue = minlen;
                }
            }
            if (changetemplate)
            { 
                repoconfig.Template = filetemplate;
            }
            if (changeversion)
            {
                repoconfig.LenVersion = lenrevisionvalue;
            }
            if (changerevision)
            {
                repoconfig.LenRevision = lenrevisionvalue;
            }
            if (changescope)
            {
                repoconfig.LenScope = lenscopevalue;
                repoconfig.Scopes = string.Join(';', scopeitems);
                repoconfig.SkipDomain = string.Join(';', skipitems);
                repoconfig.FolderByScope = withfolders;
            }

            await _filesystem.WriteAllTextAsync(configPath, JsonSerializer.Serialize(repoconfig, AppConstants.RepoSerializerOptions), cancellationToken);
            var resultrepo = new List<string>() 
            { 
                configPath
            };
            CreateScopeDirectories(repoconfig, Path.GetDirectoryName(configPath)!, resultrepo);
            foreach (var item in resultrepo)
            {
                LogMessages.LogCommandSuccessful(_logger, item);
                _console.WriteSuccess(item);
            }

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
                if (!_filesystem.DirectoryExists(scopePath))
                {
                    var fullname = _filesystem.CreateDirectory(scopePath);
                    result.Add(fullname);
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
        private async Task<Dictionary<Arguments, string>> UpgradeWizard(AdrPlusConfig adrPlusConfig, CancellationToken cancellationToken)
        {
            var parsedArgs = new Dictionary<Arguments, string>();
            while (true)
            {
                parsedArgs.Clear();

                var options = _console.PromptSelectRepoActions(cancellationToken);
                if (options.IsAborted)
                {
                    throw new OperationCanceledException(Resources.AdrPlus.CancelledByUser);
                }

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

                var folderPrompt = _console.PromptSelectFolderRepositoryAdr(rootPath, _filesystem, _validateconfig, cancellationToken);
                if (folderPrompt.IsAborted)
                {
                    throw new OperationCanceledException(Resources.AdrPlus.CancelledByUser);
                }
                parsedArgs[Arguments.TargetRepoAdr] = folderPrompt.Content;

                if (options.Content.Contains(RepoActions.Template))
                {
                    var (IsAborted, FilePathAdrTemplate) = _console.PromptConfigTemplateAdrSelect(rootPath, cancellationToken);
                    if (IsAborted)
                    {
                        throw new OperationCanceledException(Resources.AdrPlus.CancelledByUser);
                    }
                    parsedArgs[Arguments.RepoTemplate] = string.Empty;
                    parsedArgs[Arguments.FileTemplate] = FilePathAdrTemplate;
                }
                if (options.Content.Contains(RepoActions.Version))
                { 
                    var (IsAborted, newversion) = _console.PromptEditFieldVersion(new FieldsJson { Name = AppConstants.FieldLenVersion }, cancellationToken);
                    if (IsAborted)
                    {
                        throw new OperationCanceledException(Resources.AdrPlus.CancelledByUser);
                    }
                    parsedArgs[Arguments.RepoVersion] = newversion.ToString(CultureInfo.CurrentCulture);
                }
                if (options.Content.Contains(RepoActions.Revision))
                {
                    var (IsAborted, newrevision) = _console.PromptEditFieldVersion(new FieldsJson { Name = AppConstants.FieldLenRevision }, cancellationToken);
                    if (IsAborted)
                    {
                        throw new OperationCanceledException(Resources.AdrPlus.CancelledByUser);
                    }
                    parsedArgs[Arguments.RepoRevision] = newrevision.ToString(CultureInfo.CurrentCulture);
                }
                if (options.Content.Contains(RepoActions.Scope))
                {
                    var (IsAborted, newrevision) = _console.PromptEditFieldLenScope(new FieldsJson { Name = AppConstants.FieldLenScope, Value = "1" }, cancellationToken);
                    if (IsAborted)
                    {
                        throw new OperationCanceledException(Resources.AdrPlus.CancelledByUser);
                    }
                    parsedArgs[Arguments.RepoScope] = newrevision.ToString(CultureInfo.CurrentCulture);

                    var newitems = _console.PromptEditFieldScopes(new FieldsJson { Name = AppConstants.FieldScopes }, cancellationToken);
                    if (newitems.IsAborted)
                    {
                        throw new OperationCanceledException(Resources.AdrPlus.CancelledByUser);
                    }
                    parsedArgs[Arguments.RepoScopeItems] = newitems.Content;

                    var withdolder = _console.PromptEditFieldFolderByScope(new FieldsJson { Name = AppConstants.FieldFolderByScope }, cancellationToken);
                    if (withdolder.IsAborted)
                    {
                        throw new OperationCanceledException(Resources.AdrPlus.CancelledByUser);
                    }
                    if (withdolder.Content)
                    {
                        parsedArgs[Arguments.RepoWithFolders] = string.Empty;
                    }
                }

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
    }

}
