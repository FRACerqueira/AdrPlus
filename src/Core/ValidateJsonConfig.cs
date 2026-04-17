// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the AdrPlus project under MIT license
// ***************************************************************************************

using AdrPlus.Domain;
using AdrPlus.Infrastructure.FileSystem;
using AdrPlus.Infrastructure.Formatting;
using Microsoft.Extensions.Configuration;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace AdrPlus.Core
{
    /// <summary>
    /// Validates the consistency and fields of the AdrPlus.json configuration file
    /// </summary>
    internal sealed class ValidateJsonConfig(IFileSystemService fileSystem, IConfiguration configuration) : IValidateJsonConfig
    {
        private readonly IFileSystemService _fileSystem = fileSystem;
        private readonly IConfiguration _configuration = configuration;

        // Private constants for file paths and templates
        private const string TemplateDirectoryName = "template";
        private const string AdrTemplateFileName = "adr-template.md";
        private const string AdrTemplatePtBrFileName = "adr-templateptbr.md";
        private const string AdrConfigFileName = "adr-config.adrplus";
        private const string ResourceNamespace = "AdrPlus.Resources";
        private const string PtCulturePrefix = "pt-";

        /// <summary>
        /// Validates the entire application configuration (DefaultSettings section) and returns a formatted error report.
        /// </summary>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A tuple: <c>IsValid = true</c> with an empty array when valid; otherwise <c>IsValid = false</c> with the list of error messages.</returns>
        public async Task<(bool IsValid, string[] ErrorReport)> ValidateAsync(CancellationToken cancellationToken)
        {
            var errors = await ValidateDefaultSettingsAsync(cancellationToken);

            if (errors.Count == 0)
            {
                return (true, []);
            }
            return (false, [.. errors]);
        }

        /// <summary>
        /// Validates the <c>DefaultSettings</c> section of the application configuration.
        /// Checks language code validity, template file existence, folder path format, and date format.
        /// </summary>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A list of validation error messages. Empty when all checks pass.</returns>
        private async Task<List<string>> ValidateDefaultSettingsAsync(CancellationToken cancellationToken = default)
        {
            var errors = new List<string>();
            var section = _configuration.GetSection(AppConstants.DefaultSettingsRoot);

            if (!section.Exists())
            {
                errors.Add(Resources.AdrPlus.ErrMsgDefaultSettingsMissing);
                return errors;
            }

            // Validate language (optional, can be empty)
            var language = section[AppConstants.FieldLanguage];
            if (!string.IsNullOrWhiteSpace(language) && !Helper.IsValidCultureName(language))
            {
                errors.Add(string.Format(null, FormatMessages.ErrMsgInvalidLanguageCodeFormat, language));
            }

            // Validate content (required for the generation of the ADR)
            var contentpath = Path.Combine(TemplateDirectoryName, AdrTemplateFileName);
            errors.AddRange(await ValidateContentFileAsync(language, contentpath, cancellationToken));

            // Validate folderRepo (optional, must be relative path if specified)
            var folderRepo = section[AppConstants.FieldFolderRepo];
            if (!string.IsNullOrWhiteSpace(folderRepo))
            {
                if (Path.IsPathRooted(folderRepo))
                {
                    errors.Add(string.Format(null, FormatMessages.ErrMsgFolderRepoMustBeRelativeFormat, folderRepo));
                }
            }

            return errors;
        }

        /// <summary>
        /// Checks whether the default repository configuration file (<c>adr-config.adrplus</c>) exists at the expected path.
        /// </summary>
        /// <returns><see langword="true"/> if the file exists; otherwise <see langword="false"/>.</returns>
        public bool HasTemplateRepoFile()
        {
            return _fileSystem.FileExists(GetConfigRepoFilePath());
        }

        /// <summary>
        /// Gets the full file-system path for the default repository configuration file.
        /// The file is expected in the <c>template</c> subdirectory of the application base directory.
        /// </summary>
        /// <returns>The absolute path to the <c>adr-config.adrplus</c> file.</returns>
        public string GetConfigRepoFilePath()
        {
            var baseDirectory = AppContext.BaseDirectory;
            return Path.GetFullPath(Path.Combine(baseDirectory, TemplateDirectoryName, GetFileNameRepoConfig()));
        }

        /// <summary>
        /// Gets the full file-system path for the application configuration file (<c>adrplus.json</c>).
        /// </summary>
        /// <returns>The absolute path to <c>adrplus.json</c> in the application base directory.</returns>
        public string GetConfigAppFilePath()
        {
            var baseDirectory = AppContext.BaseDirectory;
            return Path.GetFullPath(Path.Combine(baseDirectory, AppConstants.AppConfigfileName));
        }


        /// <summary>
        /// Retrieves the content of the repository configuration template file (<c>adr-config.adrplus</c>) asynchronously.
        /// </summary>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A task representing the asynchronous operation, containing the template content as a string.</returns>
        /// <exception cref="FileNotFoundException">Thrown when the template file does not exist at the expected path.</exception>
        public async Task<string> GetConfigRepoTemplateAsync(CancellationToken cancellationToken)
        {
            var baseDirectory = AppContext.BaseDirectory;
            var fullpath = Path.GetFullPath(Path.Combine(baseDirectory, TemplateDirectoryName, AdrConfigFileName));
            if (_fileSystem.FileExists(fullpath))
            {
                return await _fileSystem.ReadAllTextAsync(fullpath, cancellationToken);
            }
            throw new FileNotFoundException(string.Format(null, FormatMessages.ErrMsgConfigFileNotFoundFormat, fullpath));
        }

        /// <summary>
        /// Retrieves the content of the ADR Markdown template file (<c>adr-template.md</c>) asynchronously.
        /// </summary>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A task representing the asynchronous operation, containing the template content as a string.</returns>
        /// <exception cref="FileNotFoundException">Thrown when the template file does not exist at the expected path.</exception>
        public async Task<string> GetConfigAdrTemplateAsync(CancellationToken cancellationToken)
        {
            var fullpath = GetConfigAdrTemplatePath();
            if (_fileSystem.FileExists(fullpath))
            {
                return await _fileSystem.ReadAllTextAsync(fullpath, cancellationToken);
            }
            throw new FileNotFoundException(string.Format(null, FormatMessages.ErrMsgConfigFileNotFoundFormat, fullpath));
        }

        /// <summary>
        /// Gets the full file-system path for the ADR Markdown template file (<c>adr-template.md</c>).
        /// The file is expected in the <c>template</c> subdirectory of the application base directory.
        /// </summary>
        /// <returns>The absolute path to <c>adr-template.md</c>.</returns>
        public string GetConfigAdrTemplatePath()
        {
            var baseDirectory = AppContext.BaseDirectory;
            return Path.GetFullPath(Path.Combine(baseDirectory, TemplateDirectoryName, AdrTemplateFileName));
        }

        /// <summary>
        /// Returns the default file name for the repository configuration file.
        /// </summary>
        /// <returns>The constant file name <c>adr-config.adrplus</c>.</returns>
        public string GetFileNameRepoConfig()
        {
            return AdrConfigFileName;
        }


        /// <summary>
        /// Validates that the content path represents a valid file path and, when the file does not exist, initializes the template.
        /// Handles relative paths by resolving them against <see cref="AppContext.BaseDirectory"/>.
        /// </summary>
        /// <param name="culture">The culture value from the configuration (used to select the appropriate template language).</param>
        /// <param name="content">The content path value from the configuration to validate.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>An array of validation error messages. Empty when the path is valid and the file exists (or was successfully initialized).</returns>
        private async Task<string[]> ValidateContentFileAsync(string? culture, string content, CancellationToken cancellationToken)
        {
            List<string> errors = [];
            try
            {
                // Check if content looks like a file path (has extension or path separators)
                if (content.Contains(Path.DirectorySeparatorChar) ||
                    content.Contains(Path.AltDirectorySeparatorChar) ||
                    Path.HasExtension(content))
                {
                    string fullPath;

                    // Handle relative or absolute paths
                    if (Path.IsPathRooted(content))
                    {
                        fullPath = content;
                    }
                    else
                    {
                        // Get the application base directory
                        var baseDirectory = AppContext.BaseDirectory;
                        fullPath = Path.GetFullPath(Path.Combine(baseDirectory, content));
                    }

                    // Check if the file exists
                    if (!_fileSystem.FileExists(fullPath))
                    {
                        await InitializeTemplateAsync(culture, cancellationToken); // Ensure template is initialized
                    }
                }
            }
            catch (ArgumentException ex)
            {
                errors.Add(string.Format(null, FormatMessages.ErrMsgContentInvalidPathFormat, ex.Message));
            }
            catch (PathTooLongException)
            {
                errors.Add(string.Format(null, FormatMessages.ErrMsgContentPathTooLongFormat, content));
            }
            catch (NotSupportedException)
            {
                errors.Add(string.Format(null, FormatMessages.ErrMsgContentPathNotSupportedFormat, content));
            }
            return [.. errors];
        }

        /// <summary>
        /// Validates and normalizes the repository configuration JSON to ensure all required fields are present and consistent.
        /// Normalizes scopes, <c>skipdomain</c>, <c>folderByScope</c>, <c>lenversion</c>, and <c>lenrevision</c> based on <c>lenscope</c>.
        /// Removes duplicate scope entries (case-insensitive) and removes <c>skipdomain</c> entries that are not in the defined scopes.
        /// </summary>
        /// <param name="jsonContent">The JSON string to validate and normalize.</param>
        /// <returns>The normalized JSON content as a string.</returns>
        /// <exception cref="JsonException">Thrown when <paramref name="jsonContent"/> is not valid JSON.</exception>
        /// <exception cref="KeyNotFoundException">Thrown when a required field is missing from the JSON object.</exception>
        public string EnsureFieldsRepoStructure(string jsonContent)
        {
            var auxjsonObject = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonContent, AppConstants.RepoSerializerOptions)!;
            // Convert all keys to lowercase for case-insensitive access
            var jsonObject = auxjsonObject.ToDictionary(
                kvp => kvp.Key.ToLowerInvariant(),
                kvp => kvp.Value
            );

            var lenscope = int.Parse(jsonObject[AppConstants.FieldLenScope].ToString() ?? "0", CultureInfo.InvariantCulture);
            var folderByScope = bool.Parse(jsonObject[AppConstants.FieldFolderByScope].ToString() ?? "false");
            var scopes = jsonObject[AppConstants.FieldScopes].ToString() ?? string.Empty;
            var listscopes = scopes.Split(';', StringSplitOptions.RemoveEmptyEntries);
            // Ensure lenversion and lenrevision are within valid ranges
            var lenversion = Math.Clamp(int.Parse(jsonObject[AppConstants.FieldLenVersion].ToString() ?? "0", CultureInfo.InvariantCulture), 2, 3);
            var lenrevision = Math.Clamp(int.Parse(jsonObject[AppConstants.FieldLenRevision].ToString() ?? "0", CultureInfo.InvariantCulture), 0, 3);

            // Remove duplicate scopes (case-insensitive)
            scopes = string.Join(";", listscopes.Distinct(StringComparer.OrdinalIgnoreCase));

            var skipdomain = jsonObject[AppConstants.FieldSkipDomain].ToString() ?? string.Empty;
            // Remove duplicate skipdomain entries (case-insensitive)
            var skipdomainScopes = skipdomain.Split(';', StringSplitOptions.RemoveEmptyEntries)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
            skipdomain = string.Join(";", skipdomainScopes);

            if (lenscope == 0)
            {
                scopes = string.Empty;
                skipdomain = string.Empty;
                folderByScope = false;
            }
            else if (scopes.Length == 0)
            {
                scopes = Resources.AdrPlus.DefaultScope;
                listscopes = scopes.Split(';', StringSplitOptions.RemoveEmptyEntries);
                if (skipdomain.Length == 0)
                {
                    skipdomain = Resources.AdrPlus.Defaultskipdomain;
                    skipdomainScopes = skipdomain.Split(';', StringSplitOptions.RemoveEmptyEntries);
                }
            }
            if (lenscope > 0 && scopes.Length > 0)
            {
                var minlen = Math.Clamp(
                    scopes.Split(';', StringSplitOptions.RemoveEmptyEntries).Min(x => x.Length),
                    0, 5);
                lenscope = Math.Min(lenscope, minlen);
            }
            if (skipdomain.Length > 0 && listscopes.Length > 0)
            {
                var validScopes = new HashSet<string>(listscopes, StringComparer.OrdinalIgnoreCase);
                skipdomain = string.Join(';', skipdomainScopes.Where(validScopes.Contains));
            }
            jsonObject[AppConstants.FieldLenVersion] = lenversion;
            jsonObject[AppConstants.FieldLenRevision] = lenrevision;
            jsonObject[AppConstants.FieldLenScope] = lenscope;
            jsonObject[AppConstants.FieldFolderByScope] = folderByScope;
            jsonObject[AppConstants.FieldScopes] = scopes;
            jsonObject[AppConstants.FieldSkipDomain] = skipdomain;
            return JsonSerializer.Serialize(jsonObject, AppConstants.RepoSerializerOptions);
        }

        /// <summary>
        /// Ensures the ADR Markdown template file exists on disk. When it is missing, extracts the appropriate embedded resource
        /// (Portuguese for cultures starting with <c>pt-</c>, English otherwise) and writes it to the <c>template</c> directory.
        /// </summary>
        /// <param name="appculture">The application culture string (e.g. "pt-BR"). Null or whitespace defaults to the English template.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>The template content that was written, or an empty string if the file already existed.</returns>
        public async Task<string> InitializeTemplateAsync(string? appculture, CancellationToken cancellationToken)
        {
            var content = string.Empty;
            var resfiletemplate = AdrTemplateFileName;

            if (!string.IsNullOrWhiteSpace(appculture) && Helper.IsValidCultureName(appculture))
            {
                if (appculture.StartsWith(PtCulturePrefix, StringComparison.OrdinalIgnoreCase))
                {
                    resfiletemplate = AdrTemplatePtBrFileName;
                }
            }
            var baseDirectory = AppContext.BaseDirectory;
            var fullPathtemplate = Path.GetFullPath(Path.Combine(baseDirectory, TemplateDirectoryName));
            if (!_fileSystem.DirectoryExists(fullPathtemplate))
            {
                _fileSystem.CreateDirectory(fullPathtemplate);
            }

            var fullPathfiletemplate = Path.GetFullPath(Path.Combine(fullPathtemplate, AdrTemplateFileName));
            if (!_fileSystem.FileExists(fullPathfiletemplate))
            {
                var assembly = System.Reflection.Assembly.GetExecutingAssembly();
                using var stream = assembly.GetManifestResourceStream($"{ResourceNamespace}.{resfiletemplate}");
                if (stream != null)
                {
                    using var reader = new StreamReader(stream);
                    content = reader.ReadToEnd();
                    await _fileSystem.WriteAllTextAsync(fullPathfiletemplate, content, cancellationToken);
                }
            }
            return content!;
        }

        /// <summary>
        /// Validates that a JSON string has exactly the same field set and types as the <c>adr-config.adrplus</c> template.
        /// After structural validation, performs value-level validation on fields such as lengths, status strings, and separators.
        /// </summary>
        /// <param name="jsonContent">The JSON string to validate against the expected repository structure.</param>
        /// <returns>A tuple: <c>IsValid = true</c> with an empty array when valid; otherwise <c>IsValid = false</c> with the list of error messages.</returns>
        public (bool IsValid, string[] ErrorReport) ValidateRepoStructure(string jsonContent)
        {
            var errors = new List<string>();
            try
            {
                using var originalJsonDocument = JsonDocument.Parse(jsonContent, AppConstants.DocumentOptions);
                var originalRoot = originalJsonDocument.RootElement;

                var normalizedJson = NormalizeJsonKeysToLowerInvariant(jsonContent);
                // Parse the JSON content
                using var jsonDocument = JsonDocument.Parse(normalizedJson, AppConstants.DocumentOptions);

                var root = jsonDocument.RootElement;

                // Define all required fields with their expected types
                var requiredFields = new Dictionary<string, JsonValueKind>(StringComparer.OrdinalIgnoreCase)
                {
                    { AppConstants.FieldFolderRepo, JsonValueKind.String },
                    { AppConstants.FieldTemplate, JsonValueKind.String },
                    { AppConstants.FieldPrefix, JsonValueKind.String },
                    { AppConstants.FieldLenSeq, JsonValueKind.Number },
                    { AppConstants.FieldLenVersion , JsonValueKind.Number },
                    { AppConstants.FieldLenRevision, JsonValueKind.Number },
                    { AppConstants.FieldLenScope, JsonValueKind.Number },
                    { AppConstants.FieldScopes, JsonValueKind.String },
                    { AppConstants.FieldFolderByScope, JsonValueKind.True | JsonValueKind.False },
                    { AppConstants.FieldSkipDomain, JsonValueKind.String },
                    { AppConstants.FieldSeparator, JsonValueKind.String },
                    { AppConstants.FieldCaseTransform, JsonValueKind.String },
                    { AppConstants.FieldStatusNew, JsonValueKind.String },
                    { AppConstants.FieldStatusAccepted, JsonValueKind.String },
                    { AppConstants.FieldStatusRejected, JsonValueKind.String },
                    { AppConstants.FieldStatusSuperseded, JsonValueKind.String },
                    { AppConstants.FieldHeaderDisclaimer, JsonValueKind.String },
                    { AppConstants.FieldHeaderStatus, JsonValueKind.String },
                    { AppConstants.FieldHeaderVersion, JsonValueKind.String },
                    { AppConstants.FieldHeaderRevision, JsonValueKind.String }
                };

                // Check for missing required fields
                foreach (var field in requiredFields)
                {
                    if (!root.TryGetProperty(field.Key, out var property))
                    {
                        errors.Add(string.Format(null, FormatMessages.ValidationMissingRequiredFieldFormat, field.Key));
                        continue;
                    }

                    // Validate type - special handling for boolean
                    if (field.Key.Equals(AppConstants.FieldFolderByScope, StringComparison.OrdinalIgnoreCase))
                    {
                        if (property.ValueKind != JsonValueKind.True && property.ValueKind != JsonValueKind.False)
                        {
                            errors.Add(string.Format(null, FormatMessages.ValidationFieldMustBeBooleanFormat, field.Key, property.ValueKind));
                        }
                    }
                    else if (property.ValueKind != field.Value)
                    {
                        errors.Add(string.Format(null, FormatMessages.ValidationFieldWrongTypeFormat, field.Key, field.Value, property.ValueKind));
                    }
                }

                // Check for extra fields that shouldn't be there
                var actualFields = new List<string>();
                foreach (var property in originalRoot.EnumerateObject())
                {
                    actualFields.Add(property.Name);
                }

                var extraFields = actualFields.Except(requiredFields.Keys, StringComparer.OrdinalIgnoreCase).ToList();
                if (extraFields.Count > 0)
                {
                    errors.Add(string.Format(null, FormatMessages.ValidationUnexpectedFieldsFormat, string.Join(", ", extraFields)));
                }
                if (errors.Count == 0)
                {
                    // Validate specific field values
                    errors.AddRange(ValidateConfigRepoFieldValues(root));
                }
            }
            catch (JsonException ex)
            {
                errors.Add(string.Format(null, FormatMessages.ValidationInvalidJsonFormatMsg, ex.Message));
            }

            if (errors.Count > 0)
            {
                return (false, errors.ToArray());
            }
            return (true, Array.Empty<string>());
        }

        private static string NormalizeJsonKeysToLowerInvariant(string jsonContent)
        {
            var root = JsonNode.Parse(jsonContent) ?? throw new JsonException("Invalid JSON content.");
            var normalized = NormalizeNodeKeysToLowerInvariant(root);
            return normalized.ToJsonString();
        }

        private static JsonNode NormalizeNodeKeysToLowerInvariant(JsonNode node)
        {
            if (node is JsonObject obj)
            {
                var normalized = new JsonObject();
                foreach (var item in obj)
                {
                    normalized[item.Key.ToLowerInvariant()] = item.Value is null
                        ? null
                        : NormalizeNodeKeysToLowerInvariant(item.Value);
                }

                return normalized;
            }

            if (node is JsonArray array)
            {
                var normalizedArray = new JsonArray();
                foreach (var item in array)
                {
                    normalizedArray.Add(item is null ? null : NormalizeNodeKeysToLowerInvariant(item));
                }

                return normalizedArray;
            }

            return node.DeepClone();
        }

        /// <summary>
        /// Validates the values of repository configuration fields for correctness and internal consistency.
        /// Checks numeric minimums, scope/skipdomain coherence, separator, case-transform enum, and required string fields.
        /// </summary>
        /// <param name="root">The root <see cref="JsonElement"/> of the repository configuration JSON.</param>
        /// <returns>An array of validation error messages. Empty when all field values are valid.</returns>
        private static string[] ValidateConfigRepoFieldValues(JsonElement root)
        {
            List<string> errors = [];

            // Validate numeric fields 
            JsonElement property = root.GetProperty(AppConstants.FieldLenSeq);
            if (property.TryGetInt32(out var numvalue))
            {
                if (numvalue < 3)
                {
                    errors.Add(string.Format(null, FormatMessages.ValidationFieldMinimumValueFormat, AppConstants.FieldLenSeq, 3));
                }
            }

            property = root.GetProperty(AppConstants.FieldLenVersion);
            if (property.TryGetInt32(out numvalue))
            {
                if (numvalue < 2)
                {
                    errors.Add(string.Format(null, FormatMessages.ValidationFieldMinimumValueFormat, AppConstants.FieldLenVersion, 2));
                }
            }

            property = root.GetProperty(AppConstants.FieldLenRevision);
            if (property.TryGetInt32(out numvalue))
            {
                if (numvalue < 0)
                {
                    errors.Add(string.Format(null, FormatMessages.ValidationFieldMustBeNonNegativeFormat, AppConstants.FieldLenRevision));
                }
            }

            property = root.GetProperty(AppConstants.FieldScopes);
            var propertylen = root.GetProperty(AppConstants.FieldLenScope);
            if (propertylen.TryGetInt32(out var lenscope))
            {
                if (lenscope == 0 && (property.GetString() ?? string.Empty).Length > 0)
                {
                    errors.Add(string.Format(null, FormatMessages.ValidationScopesMustBeEmptyWhenLenScopeZeroFormat, AppConstants.FieldScopes, AppConstants.FieldLenScope));
                }
                if (lenscope > 0 && (property.GetString() ?? string.Empty).Length == 0)
                {
                    errors.Add(string.Format(null, FormatMessages.ValidationScopesMustNotBeEmptyWhenLenScopePositiveFormat, AppConstants.FieldScopes, AppConstants.FieldLenScope));
                }
                if (lenscope > 0 && (property.GetString() ?? string.Empty).Length > 0)
                {
                    var minlen = property.GetString()!.Split(';', StringSplitOptions.RemoveEmptyEntries).Select(x => x.Length).Min();
                    if (minlen < lenscope)
                    {
                        errors.Add(string.Format(null, FormatMessages.ValidationScopeMinLengthFormat, AppConstants.FieldScopes, AppConstants.FieldLenScope, lenscope));
                    }
                }
            }
            property = root.GetProperty(AppConstants.FieldSkipDomain);
            var propertyscope = root.GetProperty(AppConstants.FieldScopes);
            var skipdomainValue = property.GetString() ?? string.Empty;
            if (skipdomainValue.Length > 0 && (propertyscope.GetString() ?? string.Empty).Length > 0)
            {
                var skipdomainScopes = skipdomainValue.Split(';', StringSplitOptions.RemoveEmptyEntries);
                var definedScopes = propertyscope.GetString()!.Split(';', StringSplitOptions.RemoveEmptyEntries);
                var invalidskipdomains = skipdomainScopes.Except(definedScopes, StringComparer.OrdinalIgnoreCase);
                if (invalidskipdomains.Any())
                {
                    errors.Add(string.Format(null, FormatMessages.ValidationskipdomainInvalidScopesFormat, AppConstants.FieldSkipDomain, AppConstants.FieldScopes, string.Join(", ", invalidskipdomains)));
                }
            }

            property = root.GetProperty(AppConstants.FieldFolderByScope);
            var foldervalue = property.GetBoolean();
            if (foldervalue && (propertyscope.GetString() ?? string.Empty).Length == 0)
            {
                errors.Add(string.Format(null, FormatMessages.ValidationFolderByScopeRequiresScopesFormat, AppConstants.FieldFolderByScope, AppConstants.FieldScopes));
            }


            property = root.GetProperty(AppConstants.FieldSeparator);
            var validSeparators = new[] { "-", "~", "." };
            var valuestring = property.GetString() ?? string.Empty;
            if (!validSeparators.Contains(valuestring))
            {
                errors.Add(string.Format(null, FormatMessages.ValidationMustbeFollowing, AppConstants.FieldSeparator, string.Join(", ", validSeparators)));
            }

            property = root.GetProperty(AppConstants.FieldCaseTransform);
            var validCaseTransforms = Enum.GetNames<CaseFormat>();
            valuestring = property.GetString() ?? string.Empty;
            if (!validCaseTransforms.Contains(valuestring))
            {
                errors.Add(string.Format(null, FormatMessages.ValidationMustbeFollowing, AppConstants.FieldCaseTransform, string.Join(", ", validCaseTransforms)));
            }

            property = root.GetProperty(AppConstants.FieldStatusNew);
            valuestring = property.GetString() ?? string.Empty;
            if (valuestring.Length == 0)
            {
                errors.Add(string.Format(null, FormatMessages.ValidationFieldCannotBeEmptyFormat, AppConstants.FieldStatusNew));
            }

            property = root.GetProperty(AppConstants.FieldStatusAccepted);
            valuestring = property.GetString() ?? string.Empty;
            if (valuestring.Length == 0)
            {
                errors.Add(string.Format(null, FormatMessages.ValidationFieldCannotBeEmptyFormat, AppConstants.FieldStatusAccepted));
            }

            property = root.GetProperty(AppConstants.FieldStatusRejected);
            valuestring = property.GetString() ?? string.Empty;
            if (valuestring.Length == 0)
            {
                errors.Add(string.Format(null, FormatMessages.ValidationFieldCannotBeEmptyFormat, AppConstants.FieldStatusRejected));
            }

            property = root.GetProperty(AppConstants.FieldStatusSuperseded);
            valuestring = property.GetString() ?? string.Empty;
            if (valuestring.Length == 0)
            {
                errors.Add(string.Format(null, FormatMessages.ValidationFieldCannotBeEmptyFormat, AppConstants.FieldStatusSuperseded));
            }

            property = root.GetProperty(AppConstants.FieldHeaderDisclaimer);
            valuestring = property.GetString() ?? string.Empty;
            if (valuestring.Length == 0)
            {
                errors.Add(string.Format(null, FormatMessages.ValidationFieldCannotBeEmptyFormat, AppConstants.FieldHeaderDisclaimer));
            }

            property = root.GetProperty(AppConstants.FieldHeaderStatus);
            valuestring = property.GetString() ?? string.Empty;
            if (valuestring.Length == 0)
            {
                errors.Add(string.Format(null, FormatMessages.ValidationFieldCannotBeEmptyFormat, AppConstants.FieldHeaderStatus));
            }

            property = root.GetProperty(AppConstants.FieldHeaderVersion);
            valuestring = property.GetString() ?? string.Empty;
            if (valuestring.Length == 0)
            {
                errors.Add(string.Format(null, FormatMessages.ValidationFieldCannotBeEmptyFormat, AppConstants.FieldHeaderVersion));
            }

            property = root.GetProperty(AppConstants.FieldHeaderRevision);
            valuestring = property.GetString() ?? string.Empty;
            if (valuestring.Length == 0)
            {
                errors.Add(string.Format(null, FormatMessages.ValidationFieldCannotBeEmptyFormat, AppConstants.FieldHeaderRevision));
            }

            return [.. errors];
        }

        /// <summary>
        /// Returns the default repository configuration content. If the configuration file already exists on disk, its content is returned verbatim;
        /// otherwise a new JSON string is generated from the embedded template and the supplied <paramref name="config"/> defaults.
        /// </summary>
        /// <param name="config">The application configuration providing default folder and date format values.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A task representing the asynchronous operation, containing the repository configuration JSON string.</returns>
        public async Task<string> GetConfigDefaultRepoContentAsync(AdrPlusConfig config, CancellationToken cancellationToken = default)
        {
            var fullpath = GetConfigRepoFilePath();
            if (!_fileSystem.FileExists(fullpath))
            {
                var template = await GetConfigAdrTemplateAsync(cancellationToken);
                var aux =  JsonSerializer.Serialize(new AdrPlusRepoConfig(template, config.FolderRepo), AppConstants.RepoSerializerOptions);
                var normalized = NormalizeJsonKeysToLowerInvariant(aux);
                await _fileSystem.WriteAllTextAsync(fullpath, normalized, cancellationToken);
                return normalized;
            }
            return await _fileSystem.ReadAllTextAsync(fullpath, cancellationToken);
        }

        /// <summary>
        /// Validates that a JSON string matches the expected structure of <c>AdrPlus.json</c>,
        /// ensuring all required fields are present under <c>DefaultSettings</c> with the correct types.
        /// After structural validation, performs value-level validation on language, open-ADR command, folder, date format, and yes/no values.
        /// </summary>
        /// <param name="jsonContent">The JSON string to validate against the expected application structure.</param>
        /// <returns>A tuple: <c>IsValid = true</c> with an empty array when valid; otherwise <c>IsValid = false</c> with the list of error messages.</returns>
        public (bool IsValid, string[] ErrorReport) ValidateAppStructure(string jsonContent)
        {
            var errors = new List<string>();
            try
            {
                // Parse the JSON content
                var jsonDocument = JsonDocument.Parse(jsonContent, AppConstants.DocumentOptions);
                var root = jsonDocument.RootElement;

                // Define all required fields with their expected types
                var requiredFields = new Dictionary<string, JsonValueKind>(StringComparer.OrdinalIgnoreCase)
                {
                    { AppConstants.FieldLanguage, JsonValueKind.String },
                    { AppConstants.FieldFolderRepo, JsonValueKind.String },
                    { AppConstants.FieldOpenAdr, JsonValueKind.String },
                    { AppConstants.FieldYesValue, JsonValueKind.String },
                    { AppConstants.FieldNoValue, JsonValueKind.String },

                };

                // Check for missing required fields
                foreach (var field in requiredFields)
                {
                    if (!root.GetProperty(AppConstants.DefaultSettingsRoot).TryGetProperty(field.Key, out var property))
                    {
                        errors.Add(string.Format(null, FormatMessages.ValidationMissingRequiredFieldFormat, field.Key));
                        continue;
                    }

                    if (property.ValueKind != field.Value)
                    {
                        errors.Add(string.Format(null, FormatMessages.ValidationFieldWrongTypeFormat, field.Key, field.Value, property.ValueKind));
                    }
                }

                // Check for extra fields that shouldn't be there
                var actualFields = new List<string>();
                foreach (var property in root.GetProperty(AppConstants.DefaultSettingsRoot).EnumerateObject())
                {
                    actualFields.Add(property.Name);
                }

                var extraFields = actualFields.Except(requiredFields.Keys).ToList();
                if (extraFields.Count > 0)
                {
                    errors.Add(string.Format(null, FormatMessages.ValidationUnexpectedFieldsFormat, string.Join(", ", extraFields)));
                }
                if (errors.Count == 0)
                {
                    // Validate specific field values
                    errors.AddRange(ValidateConfigAppFieldValues(root));
                }
            }
            catch (JsonException ex)
            {
                errors.Add(string.Format(null, FormatMessages.ValidationInvalidJsonFormatMsg, ex.Message));
            }

            if (errors.Count > 0)
            {
                return (false, errors.ToArray());
            }
            return (true, Array.Empty<string>());
        }

        /// <summary>
        /// Validates the values of application configuration fields for correctness and consistency.
        /// Checks language code, open-ADR command format, folder path format, date format, and yes/no value lengths.
        /// </summary>
        /// <param name="root">The root <see cref="JsonElement"/> of the application configuration JSON.</param>
        /// <returns>An array of validation error messages. Empty when all field values are valid.</returns>
        private static string[] ValidateConfigAppFieldValues(JsonElement root)
        {
            List<string> errors = [];

            var property = root.GetProperty(AppConstants.DefaultSettingsRoot).GetProperty(AppConstants.FieldLanguage);
            var languageValue = property.GetString() ?? string.Empty;
            if (languageValue.Length > 0 &&  !Helper.IsValidCultureName(languageValue))
            {
                errors.Add(string.Format(null, FormatMessages.ValidationLanguageInvalidFormat, languageValue));
            }

            property = root.GetProperty(AppConstants.DefaultSettingsRoot).GetProperty(AppConstants.FieldOpenAdr);
            var openAdrValue = property.GetString() ?? string.Empty;
            if (openAdrValue.Length > 0 && !openAdrValue.Contains("{0}"))
            {
                errors.Add(string.Format(null, FormatMessages.ValidationMustbeFollowing, AppConstants.FieldOpenAdr, "{0}"));
            }

            property = root.GetProperty(AppConstants.DefaultSettingsRoot).GetProperty(AppConstants.FieldFolderRepo);
            var foldervalue = property.GetString() ?? string.Empty;
            if (foldervalue.Length == 0)
            {
                errors.Add(string.Format(null, FormatMessages.ValidationFieldCannotBeEmptyFormat, AppConstants.FieldFolderRepo));
            }
            else
            {
                try
                {
                    // Validate that the path doesn't contain invalid characters
                    _ = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, foldervalue));
                }
                catch (ArgumentException)
                {
                    errors.Add(string.Format(null, FormatMessages.ErrMsgContentInvalidPathFormat, foldervalue));
                }
                catch (NotSupportedException)
                {
                    errors.Add(string.Format(null, FormatMessages.ErrMsgContentPathNotSupportedFormat, foldervalue));
                }
            }

            property = root.GetProperty(AppConstants.DefaultSettingsRoot).GetProperty(AppConstants.FieldYesValue);
            var yesvalueValue = property.GetString() ?? string.Empty;
            if (yesvalueValue.Length > 1)
            {
                errors.Add(string.Format(null, FormatMessages.ValidationFieldMaxCharValue, AppConstants.FieldYesValue));
            }

            property = root.GetProperty(AppConstants.DefaultSettingsRoot).GetProperty(AppConstants.FieldNoValue);
            var novalueValue = property.GetString() ?? string.Empty;
            if (novalueValue.Length > 1)
            {
                errors.Add(string.Format(null, FormatMessages.ValidationFieldMaxCharValue, AppConstants.FieldNoValue));
            }
            return [.. errors];
        }
    }
}
