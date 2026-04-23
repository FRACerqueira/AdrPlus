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

namespace AdrPlus.Commands.Config
{
    /// <summary>
    /// Handles the <c>config</c> command, allowing users to create or edit the application
    /// (<c>AdrPlus.json</c>), repository (<c>adr-config.adrplus</c>), or ADR template (<c>adr-template.md</c>)
    /// configuration files through an interactive wizard or by supplying an external file.
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of the <see cref="ConfigCommandHandler"/> class.
    /// </remarks>
    /// <param name="logger">The logger for recording command execution and errors.</param>
    /// <param name="config">The application configuration settings (folder, language, etc.).</param>
    /// <param name="fileSystem">The file system service for I/O operations.</param>
    /// <param name="validateconfig">The service for validating and loading JSON configuration files.</param>
    /// <param name="console">The console writer for displaying output and prompting user input.</param>
    /// <param name="adrServices">The ADR services for argument parsing and configuration deserialization.</param>
    internal sealed class ConfigCommandHandler(
        ILogger<ConfigCommandHandler> logger,
        IFileSystemService fileSystem,
        IValidateJsonConfig validateconfig,
        IConsoleWriter console,
        IOptions<AdrPlusConfig> config,
        IAdrServices adrServices) : ICommandHandler
    {
        private readonly ILogger<ConfigCommandHandler> _logger = logger;
        private readonly IFileSystemService _fileSystem = fileSystem;
        private readonly IConsoleWriter _console = console;
        private readonly IValidateJsonConfig _validateConfig = validateconfig;
        private readonly IAdrServices _adrServices = adrServices;
        private readonly AdrPlusConfig _config = config.Value;


        private static readonly Arguments[] ValidCommandArgs = [Arguments.WizardConfigApplication, Arguments.WizardConfigRepository,Arguments.WizardConfigTemplate,Arguments.FileConfig, Arguments.Help];

        /// <summary>
        /// Executes the <c>config</c> command asynchronously to create or edit configuration files.
        /// Routes to application, repository, or template sub-commands based on the provided arguments.
        /// </summary>
        /// <param name="args">The raw command-line tokens (e.g. <c>--application</c>, <c>--repository</c>, <c>--template</c>, <c>-file</c>, <c>--help</c>).</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="args"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">Thrown when required arguments are missing or invalid.</exception>
        /// <exception cref="FileNotFoundException">Thrown when a specified configuration or template file is not found.</exception>
        /// <exception cref="InvalidDataException">Thrown when the configuration content fails structure validation.</exception>
        /// <exception cref="OperationCanceledException">Thrown when the user cancels a wizard prompt.</exception>
        public async Task ExecuteAsync(string[] args, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(args);
            try
            {
                var parsedArgs = _adrServices.ParseArgs(args, ValidCommandArgs);
                if (parsedArgs.ContainsKey(Arguments.Help))
                {
                    _console.WriteHelp(_adrServices.GetHelpText(
                        "config", 
                        ValidCommandArgs, 
                        [
                            "adrplus config --application",
                            "adrplus config --repository",
                            "adrplus config --template -file \"path/to/file-config\""
                        ]));
                    return;
                }

                if (parsedArgs.ContainsKey(Arguments.WizardConfigRepository) && _validateConfig.HasTemplateRepoFile())
                {
                    var (IsAborted, ConfirmYes) = _console.PromptConfirm(Resources.AdrPlus.ConfigPromptOverwriteConfig, cancellationToken);
                    if (IsAborted)
                    {
                        throw new OperationCanceledException(Resources.AdrPlus.CancelledByUser, cancellationToken);
                    }
                    if (!ConfirmYes)
                    {
                        return;
                    }
                }
                var fileConfigPath = string.Empty;
                if (parsedArgs.TryGetValue(Arguments.FileConfig, out string? value))
                {
                    fileConfigPath = Path.GetFullPath(value);
                }

                if (parsedArgs.ContainsKey(Arguments.WizardConfigRepository))
                {
                    await ProcessRepoConfigAsync(fileConfigPath,cancellationToken);
                }
                else if (parsedArgs.ContainsKey(Arguments.WizardConfigApplication))
                {
                    await ProcessAppConfigAsync(fileConfigPath, cancellationToken);
                }
                else if (parsedArgs.ContainsKey(Arguments.WizardConfigTemplate))
                {
                    await ProcessTemplateConfigAsync(fileConfigPath, cancellationToken);
                }
                else
                {
                    throw new NotImplementedException();
                }
            }
            catch (Exception ex)
            {
                LogMessages.LogCommandException(_logger, ex);
                throw;
            }
        }

        /// <summary>
        /// Processes the ADR template configuration:
        /// when <paramref name="fileConfigPath"/> is provided it must be an existing <c>.md</c> file whose
        /// content is used directly; otherwise the user is guided through a wizard to select a template file.
        /// The selected content is written to the configured template path.
        /// </summary>
        /// <param name="fileConfigPath">Optional path to an external <c>.md</c> template file. Empty string triggers the wizard.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <exception cref="InvalidOperationException">Thrown when <paramref name="fileConfigPath"/> does not have a <c>.md</c> extension.</exception>
        /// <exception cref="FileNotFoundException">Thrown when the specified template file does not exist.</exception>
        /// <exception cref="OperationCanceledException">Thrown when the user cancels the wizard.</exception>
        private async Task ProcessTemplateConfigAsync(string fileConfigPath, CancellationToken cancellationToken)
        {
            string? jsoncontent;
            if (!string.IsNullOrEmpty(fileConfigPath))
            {

                var filetemplate = Path.GetFullPath(fileConfigPath);
                if (string.IsNullOrEmpty(Path.GetExtension(filetemplate)) || !Path.GetExtension(filetemplate).Equals(".md", StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException(Resources.AdrPlus.FileTemplateMusbeMd);
                }
                if (!_fileSystem.FileExists(filetemplate))
                {
                    throw new FileNotFoundException(string.Format(null, FormatMessages.ExceptionInvalidFilename, filetemplate));
                }
                jsoncontent = await _fileSystem.ReadAllTextAsync(fileConfigPath, cancellationToken);
            }
            else
            {
                jsoncontent = await WizardTemplateConfig(cancellationToken);
            }
            var fielpath = _validateConfig.GetConfigAdrTemplatePath();
            await _fileSystem.WriteAllTextAsync(fielpath, jsoncontent, cancellationToken);
            LogMessages.LogCommandSuccessful(_logger, fielpath);
            _console.WriteSuccess(fielpath);
        }

        /// <summary>
        /// Runs the interactive wizard for selecting an ADR template file.
        /// Prompts the user to choose a logical drive (when multiple are available) and then
        /// navigate to a <c>.md</c> template file on the file system.
        /// </summary>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>The content of the selected template file as a string.</returns>
        /// <exception cref="OperationCanceledException">Thrown when the user cancels any wizard prompt.</exception>
        /// <exception cref="FileNotFoundException">Thrown when the selected template file does not exist.</exception>
        private async Task<string> WizardTemplateConfig(CancellationToken cancellationToken)
        {
            // Select drive
            string[] drives = _fileSystem.GetDrives();
            var rootPath = drives[0];
            if (drives.Length > 1)
            {
                var (IsAbortedDrive, driveContent) = _console.PromptSelectLogicalDrive(Resources.AdrPlus.NewAdrPromptSelectDrive, _fileSystem, cancellationToken);
                if (IsAbortedDrive)
                {
                    throw new OperationCanceledException(Resources.AdrPlus.CancelledByUser);
                }
                rootPath = driveContent;
            }

            // Select file template
            var (IsAborted, Content) = _console.PromptConfigTemplateAdrSelect(rootPath,cancellationToken);
            if (IsAborted)
            {
                throw new OperationCanceledException(Resources.AdrPlus.CancelledByUser, cancellationToken);
            }
            if (!_fileSystem.FileExists(Content))
            {
                throw new FileNotFoundException(string.Format(null, FormatMessages.ExceptionInvalidFilename, Content));
            }
            return await _fileSystem.ReadAllTextAsync(Content, cancellationToken)!;
        }

        /// <summary>
        /// Processes the application configuration (<c>AdrPlus.json</c>):
        /// when <paramref name="fileConfigPath"/> is provided its content replaces the current config;
        /// otherwise the current config is loaded and the user edits it interactively.
        /// The result is validated and written back to the application config file.
        /// </summary>
        /// <param name="fileConfigPath">Optional path to an external JSON file. Empty string triggers the wizard.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <exception cref="FileNotFoundException">Thrown when the application config file or the external file is not found.</exception>
        /// <exception cref="InvalidDataException">Thrown when the configuration fails structure validation.</exception>
        /// <exception cref="OperationCanceledException">Thrown when the user cancels the wizard.</exception>
        private async Task ProcessAppConfigAsync(string fileConfigPath, CancellationToken cancellationToken)
        {
            var filePath = _validateConfig.GetConfigAppFilePath();
            if (!_fileSystem.FileExists(filePath))
            {
                throw new FileNotFoundException(string.Format(null, FormatMessages.ExceptionInvalidFilename, filePath));
            }
            string? jsoncontent;
            if (!string.IsNullOrEmpty(fileConfigPath))
            {
                if (!_fileSystem.FileExists(fileConfigPath))
                {
                    throw new FileNotFoundException(string.Format(null, FormatMessages.ExceptionInvalidFilename, fileConfigPath));
                }
                jsoncontent = await _fileSystem.ReadAllTextAsync(fileConfigPath, cancellationToken);
            }
            else
            {
                jsoncontent = await _fileSystem.ReadAllTextAsync(filePath, cancellationToken);
            }
            var (IsValid, ErrorReport) = _validateConfig.ValidateAppStructure(jsoncontent);
            if (!IsValid)
            {
                LogAndWriteErrors(ErrorReport);
                throw new InvalidDataException(Resources.AdrPlus.InvalidAppStructure);
            }
            if (string.IsNullOrEmpty(fileConfigPath))
            {
                jsoncontent = WizardAppConfig(jsoncontent, cancellationToken);
            }
            await _fileSystem.WriteAllTextAsync(filePath, jsoncontent, cancellationToken);
            Helper.HasAppConfigChange = true;
            LogMessages.LogCommandSuccessful(_logger, filePath);
            _console.WriteSuccess(filePath);
        }

        /// <summary>
        /// Processes the repository configuration (<c>adr-config.adrplus</c>):
        /// when <paramref name="fileConfigPath"/> is provided its content replaces the current config;
        /// otherwise the default content is loaded and the user edits it interactively.
        /// The result is validated and written to the repository config file in the template directory.
        /// </summary>
        /// <param name="fileConfigPath">Optional path to an external JSON file. Empty string triggers the wizard.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <exception cref="FileNotFoundException">Thrown when the specified external config file is not found.</exception>
        /// <exception cref="InvalidDataException">Thrown when the configuration fails structure validation.</exception>
        /// <exception cref="OperationCanceledException">Thrown when the user cancels the wizard.</exception>
        private async Task ProcessRepoConfigAsync(string fileConfigPath,  CancellationToken cancellationToken)
        {
            string? jsoncontent;
            if (!string.IsNullOrEmpty(fileConfigPath))
            {
                if (!_fileSystem.FileExists(fileConfigPath))
                {
                    throw new FileNotFoundException(string.Format(null, FormatMessages.ExceptionInvalidFilename, fileConfigPath));
                }
                jsoncontent = await _fileSystem.ReadAllTextAsync(fileConfigPath, cancellationToken);
            }
            else
            {
                jsoncontent = await _validateConfig.GetConfigDefaultRepoContentAsync(_config, cancellationToken);
            }
            if (string.IsNullOrEmpty(fileConfigPath))
            {
                jsoncontent = WizardRepoConfig(jsoncontent, cancellationToken);
            }
            var (IsValid, ErrorReport) = _validateConfig.ValidateRepoStructure(jsoncontent);
            if (!IsValid)
            {
                LogAndWriteErrors(ErrorReport);
                throw new InvalidDataException(Resources.AdrPlus.InvalidRepoStructure);
            }

            var filePath = _validateConfig.GetConfigRepoFilePath();
            await _fileSystem.WriteAllTextAsync(filePath, jsoncontent, cancellationToken);
            LogMessages.LogCommandSuccessful(_logger, filePath);
            _console.WriteSuccess(filePath);
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
        /// Runs the interactive wizard for editing the application configuration JSON.
        /// Presents a field list, lets the user select and edit individual fields, and loops until the user ends the edit.
        /// </summary>
        /// <param name="content">The current application configuration JSON string.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>The modified application configuration JSON string.</returns>
        /// <exception cref="OperationCanceledException">Thrown when the user cancels any wizard prompt.</exception>
        private string WizardAppConfig(string content, CancellationToken cancellationToken)
        {
            var modifiedConfig = content;
            FieldsJson defaultselect = new();
            while (true)
            {
                var fields = BuildAppFieldsFromJson(modifiedConfig);
                var (IsAborted, Content) = _console.PromptConfigJsonAppSelect(defaultselect, fields, cancellationToken);

                if (IsAborted)
                {
                    throw new OperationCanceledException(Resources.AdrPlus.CancelledByUser, cancellationToken);
                }

                defaultselect = Content!;

                if (Content!.IsEndEdit)
                {
                    break;
                }

                var (aborted, field) = EditFieldApp(Content.Name, [.. fields], cancellationToken);
                if (aborted)
                {
                    throw new OperationCanceledException(Resources.AdrPlus.CancelledByUser, cancellationToken);
                }

                modifiedConfig = UpdateJsonFieldApp(modifiedConfig, field.Name, field.Value, field.Type);
            }

            return modifiedConfig;
        }

        /// <summary>
        /// Runs the interactive wizard for editing the repository configuration JSON.
        /// Normalizes the JSON via <see cref="IValidateJsonConfig.EnsureFieldsRepoStructure"/> before and after each edit,
        /// displays sample ADR filenames, and loops until the user ends the edit.
        /// </summary>
        /// <param name="content">The current repository configuration JSON string.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>The modified and normalized repository configuration JSON string.</returns>
        /// <exception cref="OperationCanceledException">Thrown when the user cancels any wizard prompt.</exception>
        private string WizardRepoConfig(string content, CancellationToken cancellationToken)
        {
            var modifiedConfig = _validateConfig.EnsureFieldsRepoStructure(content);
            FieldsJson defaultselect = new();
            while (true)
            {
                var fields = BuildRepoFieldsFromJson(modifiedConfig);
                DisplaySampleFiles(modifiedConfig);

                var (IsAborted, Content) = _console.PromptConfigJsonRepoSelect(defaultselect, fields, cancellationToken);

                if (IsAborted)
                {
                    throw new OperationCanceledException(Resources.AdrPlus.CancelledByUser, cancellationToken);
                }

                defaultselect = Content!;

                if (Content!.IsEndEdit)
                {
                    break;
                }

                var (aborted, field) = EditFieldRepo(Content.Name, [.. fields], cancellationToken);
                if (aborted)
                {
                    throw new OperationCanceledException(Resources.AdrPlus.CancelledByUser, cancellationToken);
                }

                modifiedConfig = UpdateJsonFieldRepo(modifiedConfig, field.Name, field.Value, field.Type);
                modifiedConfig = _validateConfig.EnsureFieldsRepoStructure(modifiedConfig);
            }

            return modifiedConfig;
        }

        /// <summary>
        /// Parses <paramref name="modifiedConfig"/> and builds a list of editable <see cref="FieldsJson"/> entries
        /// for all root-level properties. The <c>template</c> field is marked as non-editable.
        /// </summary>
        /// <param name="modifiedConfig">The current repository configuration JSON string.</param>
        /// <returns>A list of <see cref="FieldsJson"/> entries representing each configuration field.</returns>
        private static List<FieldsJson> BuildRepoFieldsFromJson(string modifiedConfig)
        {
            using var jsonDoc = JsonDocument.Parse(modifiedConfig, AppConstants.DocumentOptions);
            var root = jsonDoc.RootElement;
            var fields = new List<FieldsJson>();

            foreach (var property in root.EnumerateObject())
            {
                var enabled = true;
                if (property.Name.Equals(AppConstants.FieldTemplate, StringComparison.OrdinalIgnoreCase))
                {
                    enabled = false;
                }
                var value = GetJsonValueAsString(property.Value);
                fields.Add(new FieldsJson
                {
                    Name = property.Name,
                    Value = value,
                    Type = property.Value.ValueKind,
                    IsEnabled = enabled
                });
            }
            return fields;
        }

        /// <summary>
        /// Parses <paramref name="modifiedConfig"/> and builds a list of editable <see cref="FieldsJson"/> entries
        /// for all properties under the <c>DefaultSettings</c> section.
        /// </summary>
        /// <param name="modifiedConfig">The current application configuration JSON string.</param>
        /// <returns>A list of <see cref="FieldsJson"/> entries representing each configuration field.</returns>
        private static List<FieldsJson> BuildAppFieldsFromJson(string modifiedConfig)
        {
            var jsonDoc = JsonDocument.Parse(modifiedConfig, AppConstants.DocumentOptions);
            var root = jsonDoc.RootElement;
            var fields = new List<FieldsJson>();

            foreach (var property in root.GetProperty(AppConstants.DefaultSettingsRoot).EnumerateObject())
            {
                var value = GetJsonValueAsString(property.Value);
                fields.Add(new FieldsJson
                {
                    Name = property.Name,
                    Value = value,
                    Type = property.Value.ValueKind,
                    IsEnabled = true
                });
            }

            return fields;
        }

        /// <summary>
        /// Generates and writes sample ADR filenames to the console to help the user preview the effect of the current configuration.
        /// </summary>
        /// <param name="modifiedConfig">The current repository configuration JSON string.</param>
        private void DisplaySampleFiles(string modifiedConfig)
        {
            _console.WriteInfo(Resources.AdrPlus.ConfigInfoFileNameSample);
            foreach (var sample in GetSampleFiles(modifiedConfig))
            {
                _console.WriteInfo($"- {sample}");
            }
        }

        /// <summary>
        /// Prompts the user to edit the application configuration field identified by <paramref name="fieldName"/>.
        /// </summary>
        /// <param name="fieldName">The name of the field to edit.</param>
        /// <param name="fields">All available application configuration fields.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>
        /// A tuple: <c>IsAborted = true</c> when the user cancelled; otherwise <c>IsAborted = false</c>
        /// and <c>EditField</c> contains the updated field.
        /// </returns>
        private (bool IsAborted, FieldsJson EditField) EditFieldApp(string fieldName, FieldsJson[] fields, CancellationToken cancellationToken)
        {
            var selection = fields.First(f => f.Name.Equals(fieldName, StringComparison.OrdinalIgnoreCase));

            _console.WriteInfo(Resources.AdrPlus.ConfigInfoPropertyEditor);
            _console.WriteInfo(string.Format(null, FormatMessages.ConfigInfoSelectedField, AppConstants.GetTitleField(selection.Name)));

            var isaborted = ProcessFieldEdit(selection.Name, selection, fields, -1, cancellationToken);

            _console.WriteInfo(string.Format(null, FormatMessages.ConfigInfoCurrentValue, selection.Name, selection.Value));
            _console.WriteInfo(string.Empty);

            return (isaborted, selection);
        }

        /// <summary>
        /// Prompts the user to edit the repository configuration field identified by <paramref name="fieldName"/>.
        /// Respects the current <c>lenscope</c> value to enable scope-dependent fields.
        /// </summary>
        /// <param name="fieldName">The name of the field to edit.</param>
        /// <param name="fields">All available repository configuration fields.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>
        /// A tuple: <c>IsAborted = true</c> when the user cancelled; otherwise <c>IsAborted = false</c>
        /// and <c>EditField</c> contains the updated field.
        /// </returns>
        private (bool IsAborted, FieldsJson EditField) EditFieldRepo(string fieldName, FieldsJson[] fields, CancellationToken cancellationToken)
        {
            var selection = fields.First(f => f.Name.Equals(fieldName, StringComparison.OrdinalIgnoreCase));
            var fieldscope = fields.First(f => f.Name.Equals(AppConstants.FieldLenScope, StringComparison.OrdinalIgnoreCase));
            int lenscope = int.TryParse(fieldscope.Value, out var parsedValue) ? parsedValue : 0;

            _console.WriteInfo(Resources.AdrPlus.ConfigInfoPropertyEditor);
            _console.WriteInfo(string.Format(null, FormatMessages.ConfigInfoSelectedField, AppConstants.GetTitleField(selection.Name)));

            var isaborted = ProcessFieldEdit(selection.Name, selection, fields, lenscope, cancellationToken);

            _console.WriteInfo(string.Format(null, FormatMessages.ConfigInfoCurrentValue, selection.Name, selection.Value));
            _console.WriteInfo(string.Empty);

            return (isaborted, selection);
        }

        /// <summary>
        /// Dispatches to the appropriate prompt for the given <paramref name="fieldName"/> and updates
        /// <paramref name="selection"/> with the user-supplied value.
        /// </summary>
        /// <param name="fieldName">The configuration field key to edit.</param>
        /// <param name="selection">The <see cref="FieldsJson"/> entry to update in-place.</param>
        /// <param name="fields">All available configuration fields (used for cross-field validation prompts).</param>
        /// <param name="lenscope">The current value of <c>lenscope</c>; controls availability of scope-dependent fields.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns><see langword="true"/> when the user aborted the edit; otherwise <see langword="false"/>.</returns>
        private bool ProcessFieldEdit(string fieldName, FieldsJson selection, FieldsJson[] fields, int lenscope, CancellationToken cancellationToken)
        {
            return fieldName switch
            {
                AppConstants.FieldLanguage => HandleEditField(() => 
                    _console.PromptEditFieldLanguage(selection, cancellationToken), selection, v => v.Trim()),
                AppConstants.FieldYesValue => HandleEditField(() => 
                    _console.PromptEditFieldYesNoChar(selection, fields, cancellationToken), selection, v => v.Trim()),
                AppConstants.FieldNoValue => HandleEditField(() => 
                    _console.PromptEditFieldYesNoChar(selection, fields, cancellationToken), selection, v => v.Trim()),
                AppConstants.FieldFolderRepo => HandleEditField(() => 
                    _console.PromptEditFieldFolderRepo(selection, cancellationToken), selection, v => v.Trim()),
                AppConstants.FieldOpenAdr => HandleEditField(() => 
                    _console.PromptEditFielOpenAdr(selection, cancellationToken), selection, v => v.Trim()),
                AppConstants.FieldPrefix => HandleEditField(() => 
                    _console.PromptEditFieldPrefix(selection, cancellationToken), selection, v => v.Trim()),
                AppConstants.FieldLenSeq => HandleEditField(() => 
                    _console.PromptEditFieldLenSeq(selection, cancellationToken), selection, v => v.ToString(CultureInfo.CurrentCulture)!),
                AppConstants.FieldLenVersion => HandleEditField(() => 
                    _console.PromptEditFieldVersion(selection, cancellationToken), selection, v => v.ToString(CultureInfo.CurrentCulture)!),
                AppConstants.FieldLenRevision => HandleEditField(() =>
                    _console.PromptEditFieldRevision(selection, cancellationToken), selection, v => v.ToString(CultureInfo.CurrentCulture)!),
                AppConstants.FieldScopes when lenscope > 0 => HandleEditField(() => 
                    _console.PromptEditFieldScopes(selection, cancellationToken), selection, v => v.ToString(CultureInfo.CurrentCulture)!),
                AppConstants.FieldSkipDomain when lenscope > 0 => HandleEditField(() =>
                    _console.PromptEditFieldskipdomain(selection, fields, cancellationToken), selection, v => v.ToString()!),
                AppConstants.FieldLenScope => HandleEditField(() => 
                    _console.PromptEditFieldLenScope(selection, cancellationToken), selection, v => v.ToString(CultureInfo.CurrentCulture)!),
                AppConstants.FieldFolderByScope when lenscope > 0 => HandleEditField(() => 
                    _console.PromptEditFieldFolderByScope(selection, cancellationToken), selection, v => v.ToString(CultureInfo.CurrentCulture)!),
                AppConstants.FieldCaseTransform => HandleEditField(() => 
                    _console.PromptEditFieldCaseTransform(selection, cancellationToken), selection, v => v.ToString()!),
                AppConstants.FieldSeparator => HandleEditField(() => 
                    _console.PromptEditFieldSeparator(selection, cancellationToken), selection, v => v.ToString()!),
                AppConstants.FieldStatusNew  => HandleEditField(() => 
                    _console.PromptEditFieldStatus(selection, cancellationToken), selection, v => v.ToString()!.ToPascalCase()),
                AppConstants.FieldStatusAccepted => HandleEditField(() => 
                    _console.PromptEditFieldStatus(selection, cancellationToken), selection, v => v.ToString()!.ToPascalCase()),
                AppConstants.FieldStatusSuperseded => HandleEditField(() => 
                    _console.PromptEditFieldStatus(selection, cancellationToken), selection, v => v.ToString()!.ToPascalCase()),
                AppConstants.FieldStatusRejected => HandleEditField(() => 
                    _console.PromptEditFieldStatus(selection, cancellationToken), selection, v => v.ToString()!.ToPascalCase()),
                AppConstants.FieldHeaderDisclaimer => HandleEditField(() => 
                    _console.PromptEditFieldHeaderDisclaimer(selection, cancellationToken), selection, v => v.ToString()!.ToPascalCase()),
                AppConstants.FieldHeaderStatus => HandleEditField(() => 
                    _console.PromptEditFieldHeaderStatus(selection, cancellationToken), selection, v => v.ToString()!.ToPascalCase()),
                AppConstants.FieldHeaderVersion => HandleEditField(() => 
                    _console.PromptEditFieldHeaderVersion(selection, cancellationToken), selection, v => v.ToString()!.ToPascalCase()),
                AppConstants.FieldHeaderRevision => HandleEditField(() =>
                    _console.PromptEditFieldHeaderRevision(selection, cancellationToken), selection, v => v.ToString()!.ToPascalCase()),
                _ => false
            };
        }

        /// <summary>
        /// Invokes <paramref name="promptFunc"/> and, when the user did not abort, updates
        /// <paramref name="selection"/>.<see cref="FieldsJson.Value"/> using <paramref name="converter"/>.
        /// </summary>
        /// <typeparam name="T">The value type returned by the prompt.</typeparam>
        /// <param name="promptFunc">A function that presents the prompt and returns <c>(IsAborted, Content)</c>.</param>
        /// <param name="selection">The field to update when the user confirms.</param>
        /// <param name="converter">Converts the prompt result to the string value stored in <paramref name="selection"/>.</param>
        /// <returns><see langword="true"/> when the user aborted; otherwise <see langword="false"/>.</returns>
        private static bool HandleEditField<T>(Func<(bool IsAborted, T Content)> promptFunc, FieldsJson selection, Func<T, string> converter)
        {
            var (isAborted, content) = promptFunc();
            if (!isAborted)
            {
                selection.Value = converter(content);
            }
            return isAborted;
        }

        /// <summary>
        /// Generates representative sample ADR filenames from the current configuration
        /// to preview the effect of naming-convention settings.
        /// </summary>
        /// <param name="modifiedConfig">The current repository configuration JSON string.</param>
        /// <returns>An array of sample filename strings demonstrating the naming convention.</returns>
        private string[] GetSampleFiles(string modifiedConfig)
        {
            var repoconfig = _adrServices.FromJson(modifiedConfig, string.Empty, _config.FolderRepo);
            var skipdomains = repoconfig.Getskipdomains();
            var scopes = repoconfig.GetScopes();
            var eligibleScope = scopes.Where(x => !skipdomains.Any(s => s == x)).FirstOrDefault() ?? string.Empty;

            var models = new[]
            {
                CreateSampleModel(1, 1, 1, AdrStatus.Proposed, null, "X", skipdomains.FirstOrDefault() ?? string.Empty, string.Empty, repoconfig),
                CreateSampleModel(2, 1, 1, AdrStatus.Proposed, null, "Y", eligibleScope, "K", repoconfig),
                CreateSampleModel(2, 2, 1, AdrStatus.Proposed, null, "Y", eligibleScope, "K", repoconfig),
                CreateSampleModel(3, 1, 1, AdrStatus.Superseded, 2, "Y", eligibleScope, "K", repoconfig),
            };

            return [.. models.Select(m => m.GetFileName(repoconfig))];
        }

        /// <summary>
        /// Creates a minimal <see cref="AdrRecord"/> used solely for generating sample filenames.
        /// </summary>
        /// <param name="number">The ADR sequence number.</param>
        /// <param name="version">The version number.</param>
        /// <param name="revision">The revision number.</param>
        /// <param name="status">The ADR creation status.</param>
        /// <param name="superseded">The sequence number of the superseded ADR, or <see langword="null"/>.</param>
        /// <param name="titleSuffix">A short distinguishing suffix appended to the sample title.</param>
        /// <param name="scope">The scope string (applied as-is, without case transform).</param>
        /// <param name="domainSuffix">A short distinguishing suffix appended to the sample domain.</param>
        /// <param name="repoconfig">The repository configuration used for case transformation.</param>
        /// <returns>A new <see cref="AdrRecord"/> instance suitable for filename generation.</returns>
        private static AdrRecord CreateSampleModel(int number, int version, int revision, AdrStatus status, int? superseded,
            string titleSuffix, string scope, string domainSuffix, AdrPlusRepoConfig repoconfig)
        {
            return new AdrRecord
            {
                StatusCreate = status,
                Superseded = superseded,
                Number = number,
                Version = version,
                Revision = revision,
                Title = $"{Resources.AdrPlus.TitleSample} {titleSuffix}".ToCase(repoconfig.CaseTransform),
                Scope = scope,
                Domain = string.IsNullOrEmpty(domainSuffix) ? string.Empty : $"{Resources.AdrPlus.DomainSample} {domainSuffix}".ToCase(repoconfig.CaseTransform),
                CreateRef = DateTime.UtcNow,
                Template = string.Empty
            };
        }

        /// <summary>
        /// Converts a JSON element value to its string representation.
        /// </summary>
        /// <param name="element">The JSON element to convert.</param>
        /// <returns>The string representation of the element value.</returns>
        private static string GetJsonValueAsString(JsonElement element)
        {
            return element.ValueKind switch
            {
                JsonValueKind.String => element.GetString() ?? string.Empty,
                JsonValueKind.Number => element.GetInt32().ToString(CultureInfo.CurrentCulture),
                JsonValueKind.True => "true",
                JsonValueKind.False => "false",
                _ => element.ToString()
            };
        }

        /// <summary>
        /// Updates a specific field value in the application configuration JSON.
        /// </summary>
        /// <param name="jsonContent">The current JSON configuration content.</param>
        /// <param name="fieldName">The name of the field to update.</param>
        /// <param name="newValue">The new value for the field.</param>
        /// <param name="type">The JSON value kind of the field.</param>
        /// <returns>The updated JSON configuration content.</returns>
        private static string UpdateJsonFieldApp(string jsonContent, string fieldName, string newValue, JsonValueKind type)
        {
            var jsonDoc = JsonDocument.Parse(jsonContent, AppConstants.DocumentOptions);
            var root = jsonDoc.RootElement;

            using var stream = new MemoryStream();
            using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true }))
            {
                writer.WriteStartObject();
                writer.WritePropertyName(AppConstants.DefaultSettingsRoot);
                writer.WriteStartObject();
                foreach (var property in root.GetProperty(AppConstants.DefaultSettingsRoot).EnumerateObject())
                {
                    if (property.Name == fieldName)
                    {
                        WriteJsonProperty(writer, property.Name, newValue, type);
                    }
                    else
                    {
                        property.WriteTo(writer);
                    }
                }
                writer.WriteEndObject();
                writer.WriteEndObject();
            }
            return Encoding.UTF8.GetString(stream.ToArray());
        }

        /// <summary>
        /// Updates a specific field value in the repository configuration JSON.
        /// </summary>
        /// <param name="jsonContent">The current JSON configuration content.</param>
        /// <param name="fieldName">The name of the field to update.</param>
        /// <param name="newValue">The new value for the field.</param>
        /// <param name="type">The JSON value kind of the field.</param>
        /// <returns>The updated JSON configuration content.</returns>
        private static string UpdateJsonFieldRepo(string jsonContent, string fieldName, string newValue, JsonValueKind type)
        {
            using var jsonDoc = JsonDocument.Parse(jsonContent, AppConstants.DocumentOptions);

            var root = jsonDoc.RootElement;

            using var stream = new MemoryStream();
            using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true }))
            {
                writer.WriteStartObject();
                foreach (var property in root.EnumerateObject())
                {
                    if (property.Name.Equals(fieldName, StringComparison.OrdinalIgnoreCase))
                    {
                        WriteJsonProperty(writer, property.Name, newValue, type);
                    }
                    else
                    {
                        property.WriteTo(writer);
                    }
                }
                writer.WriteEndObject();
            }
            return Encoding.UTF8.GetString(stream.ToArray());
        }

        /// <summary>
        /// Writes a JSON property with the appropriate type to a Utf8JsonWriter.
        /// </summary>
        /// <param name="writer">The JSON writer.</param>
        /// <param name="propertyName">The name of the property.</param>
        /// <param name="value">The string representation of the value.</param>
        /// <param name="type">The JSON value kind to write.</param>
        /// <exception cref="ArgumentException">Thrown when the value cannot be parsed to the expected type.</exception>
        private static void WriteJsonProperty(Utf8JsonWriter writer, string propertyName, string value, JsonValueKind type)
        {
            writer.WritePropertyName(propertyName);

            switch (type)
            {
                case JsonValueKind.Number:
                    if (int.TryParse(value, out int intValue))
                    {
                        writer.WriteNumberValue(intValue);
                    }
                    else
                    {
                        throw new ArgumentException(string.Format(null, FormatMessages.ConfigErrorInvalidNumber, value));
                    }
                    break;

                case JsonValueKind.True:
                case JsonValueKind.False:
                    if (bool.TryParse(value, out bool boolValue))
                    {
                        writer.WriteBooleanValue(boolValue);
                    }
                    else
                    {
                        throw new ArgumentException(string.Format(null, FormatMessages.ConfigErrorInvalidBoolean, value));
                    }
                    break;
                default:
                    writer.WriteStringValue(value);
                    break;
            }
        }
    }
}
