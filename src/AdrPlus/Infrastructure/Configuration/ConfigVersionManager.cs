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
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace AdrPlus.Infrastructure.Configuration
{
    /// <summary>
    /// Manages version tracking for template files and handles configuration migration when versions change.
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of the <see cref="ConfigVersionManager"/> class.
    /// </remarks>
    /// <param name="prompt">The prompt console for user interactions.</param>
    /// <param name="logger">The logger for recording operations.</param>
    /// <param name="fileSystem">The file system service for I/O operations.</param>
    /// <param name="configuration">The configuration service for accessing application settings.</param>
    internal sealed partial class ConfigVersionManager(
        IConsoleWriter prompt,
        ILogger<ConfigVersionManager> logger,
        IValidateConfig validateJsonConfig,
        IFileSystemService fileSystem,
        IConfiguration configuration) : IConfigurationMigrator
    {
        private readonly ILogger<ConfigVersionManager> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        private readonly IValidateConfig _validateJsonConfig = validateJsonConfig ?? throw new ArgumentNullException(nameof(validateJsonConfig));
        private readonly IFileSystemService _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
        private readonly IConfiguration _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        private readonly IConsoleWriter _prompt = prompt ?? throw new ArgumentNullException(nameof(prompt));
        


        /// <summary>
        /// Checks for version file in template directory and performs migration if needed.
        /// </summary>
        /// <param name="currentVersion">The current application version.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>True if migration was performed or version file is up-to-date; false if an error occurred.</returns>
        public async Task<bool> CheckAndMigrateConfigAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var currentVersion = _configuration[AppConstants.CfgNameVersionApp] ?? "0.0.0";

                // Search for existing version file
                var existingVersionFile = FindVersionFile();

                if (existingVersionFile == null)
                {
                    await _validateJsonConfig.RecreateVersionFileAsync(currentVersion, cancellationToken);
                    LogAndWriteSuccess(string.Format(null, FormatMessages.NotFoundRecreatedVersionMigration, currentVersion));
                    return false;
                }

                // Extract version from filename
                var storedVersion = ExtractVersionFromFilename(existingVersionFile);

                if (storedVersion == null)
                {
                    await _validateJsonConfig.RecreateVersionFileAsync(currentVersion, cancellationToken);
                    LogAndWriteSuccess(string.Format(null, FormatMessages.NotFoundRecreatedVersionMigration, currentVersion));
                    return false;
                }

                // Compare versions
                if (storedVersion != currentVersion)
                {
                    // Execute migration
                    var migrationSuccess = await MigrateAsync(storedVersion, cancellationToken);

                    if (!migrationSuccess)
                    {
                        throw new InvalidOperationException(string.Format(null, FormatMessages.ErrMigrationVersionFailed, storedVersion, currentVersion));
                    }
                    // Recreate version file with new version
                    await _validateJsonConfig.RecreateVersionFileAsync(currentVersion, cancellationToken);
                    LogAndWriteSuccess(string.Format(null, FormatMessages.MigrationVersionSuccess, storedVersion, currentVersion));
                    Thread.Sleep(3000);
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                LogMessages.LogCriticalError(_logger, ex);
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
            _prompt.PromptWriteSuccess(message);
        }

        /// <summary>
        /// Finds the existing version file in the history directory.
        /// </summary>
        /// <returns>The filename if found; null otherwise.</returns>
        private string? FindVersionFile()
        {
            var historyDirectoryPath = _validateJsonConfig.GetHistoryPath();
            var versionFiles = _fileSystem.GetFiles(historyDirectoryPath, $"{AppConstants.VersionFilePrefix}*.txt", SearchOption.TopDirectoryOnly);
            return versionFiles.FirstOrDefault();
        }


        /// <summary>
        /// Extracts the version number from the version filename.
        /// </summary>
        /// <param name="filename">The full path to the version file.</param>
        /// <returns>The version string if extraction is successful; null otherwise.</returns>
        private static string? ExtractVersionFromFilename(string filename)
        {
            var fileNameOnly = Path.GetFileName(filename);
            var match = RegexFileconfigVersion().Match(fileNameOnly);

            if (match.Success && match.Groups.Count > 1)
            {
                var version = match.Groups[1].Value.Trim().ToLowerInvariant().Replace(".txt", "");
                return string.IsNullOrEmpty(version) ? null : version;
            }

            return null;
        }

        private async Task<bool> MigrateAsync(string fromVersion, CancellationToken cancellationToken = default)
        {
            // Convert version strings to comparable numbers (e.g., "0.5.0" -> 500, "0.6.0" -> 600)
            long fromversionAsNumber = (((int)char.GetNumericValue(fromVersion[0])) * 10000) + ((int)char.GetNumericValue(fromVersion[2]) * 100) + (int)char.GetNumericValue(fromVersion[4]);

            if (fromversionAsNumber < 500)
            {
                return true; // skip migration for versions below 0.5.0 or above 0.6.0
            }


            var historyDirectoryPath = _validateJsonConfig.GetHistoryPath();
            var oldversionFilePath = Path.Combine(historyDirectoryPath, $"{AppConstants.VersionFilePrefix}{fromVersion}.txt");
            var oldjson = await _fileSystem.ReadAllTextAsync(oldversionFilePath, cancellationToken);
            var filesconfig = JsonSerializer.Deserialize<string[]>(oldjson, AppConstants.RepoSerializerOptions)!;
            var jsondocold = JsonDocument.Parse(filesconfig[0], AppConstants.DocumentOptions);

            // application config migration
            var newconfigapp = new AdrPlusConfig();
            var dictionary = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            foreach (var property in jsondocold.RootElement.GetProperty(AppConstants.DefaultSettingsRoot).EnumerateObject())
            {
                dictionary[property.Name] = property.Value;
            }
            Type apptype = typeof(AdrPlusConfig);
            foreach (var item in apptype.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (dictionary.TryGetValue(item.Name, out var value))
                {
                    if (item?.Name.ToLowerInvariant() == "withoutargs" || item?.Name.ToLowerInvariant() == "behaviorwithoutargs")
                    {
                        if (Enum.TryParse(typeof(BehaviorWithoutArg), value?.ToString() ?? string.Empty, true, out var enumvalue))
                        {
                            item?.SetValue(newconfigapp, enumvalue);
                        }
                    }
                    else
                    {
                        JsonElement? jvalue = value as JsonElement?;
                        if (jvalue != null)
                        {
                            switch (jvalue.Value.ValueKind)
                            {
                                case JsonValueKind.String:
                                    item?.SetValue(newconfigapp, jvalue.Value.GetString());
                                    break;
                                case JsonValueKind.Number:
                                    if (jvalue.Value.TryGetInt32(out var intValue))
                                    {
                                        item?.SetValue(newconfigapp, intValue);
                                    }
                                    break;
                                case JsonValueKind.True:
                                    item?.SetValue(newconfigapp, true);
                                    break;
                                case JsonValueKind.False:
                                    item?.SetValue(newconfigapp, false);
                                    break;
                                case JsonValueKind.Array:
                                    item?.SetValue(newconfigapp, JsonSerializer.Deserialize<List<PluginAllowlistEntry>>(jvalue.Value.GetRawText(), AppConstants.RepoSerializerOptions));
                                    break;
                            }
                        }
                    }
                }
            }
            var pluginAllowlistField = newconfigapp.PluginAllowlist != null
                ? $",\"{AppConstants.FieldPluginAllowlist}\": {JsonSerializer.Serialize(newconfigapp.PluginAllowlist, AppConstants.RepoSerializerOptions)}"
                : string.Empty;
            var jsoncontent = $"{{\"{AppConstants.DefaultSettingsRoot}\":{{\"{AppConstants.FieldLanguage}\": \"{newconfigapp.Language}\",\"{AppConstants.FieldOpenAdr}\": \"{newconfigapp.ComandOpenAdr}\",\"{AppConstants.FieldWithoutArgs}\": \"{newconfigapp.WithoutArgs}\"{pluginAllowlistField}}}}}";
            using (var jsonDoc = JsonDocument.Parse(jsoncontent))
            {
                jsoncontent = JsonSerializer.Serialize(jsonDoc, AppConstants.RepoSerializerOptions);
            }
            var filepath = _validateJsonConfig.GetConfigAppFilePath();
            await _fileSystem.WriteAllTextAsync(filepath, jsoncontent, cancellationToken);


            // repository config migration
            var defaulttemplate = await _validateJsonConfig.GetConfigAdrTemplateAsync(cancellationToken);
            var newconfigrepo = new AdrPlusRepoConfig(AppConstants.DefaultFolderAdr, defaulttemplate);
            jsondocold = JsonDocument.Parse(filesconfig[1], AppConstants.DocumentOptions);
            dictionary.Clear();
            foreach (var property in jsondocold.RootElement.EnumerateObject())
            {
                dictionary[property.Name] = property.Value;
            }
            Type repotype = typeof(AdrPlusRepoConfig);
            foreach (var item in repotype.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (dictionary.TryGetValue(item.Name, out var value))
                {
                    if (item?.Name.ToLowerInvariant() == "casetransform")
                    {
                        if (Enum.TryParse(typeof(CaseFormat), value?.ToString() ?? string.Empty, true, out var enumvalue))
                        {
                            item?.SetValue(newconfigrepo, enumvalue);
                        }
                    }
                    else if (item?.Name.ToLowerInvariant() == "separator")
                    {
                        if (value != null && value is JsonElement jvalue && jvalue.ValueKind == JsonValueKind.String)
                        {
                            var strValue = jvalue.GetString();
                            if (!string.IsNullOrEmpty(strValue) && strValue.Length == 1)
                            {
                                item?.SetValue(newconfigrepo, strValue[0]);
                            }
                        }
                    }
                    else
                    {
                        JsonElement? jvalue = value as JsonElement?;
                        if (jvalue != null)
                        {
                            switch (jvalue.Value.ValueKind)
                            {
                                case JsonValueKind.String:
                                    item?.SetValue(newconfigrepo, jvalue.Value.GetString());
                                    break;
                                case JsonValueKind.Number:
                                    if (jvalue.Value.TryGetInt32(out var intValue))
                                    {
                                        item?.SetValue(newconfigrepo, intValue);
                                    }
                                    break;
                                case JsonValueKind.True:
                                    item?.SetValue(newconfigrepo, true);
                                    break;
                                case JsonValueKind.False:
                                    item?.SetValue(newconfigrepo, false);
                                    break;
                            }
                        }
                    }
                }
            }
            jsoncontent = JsonSerializer.Serialize(newconfigrepo, AppConstants.RepoSerializerOptions);
            filepath = _validateJsonConfig.GetDefaultConfigRepoFilePath();
            await _fileSystem.WriteAllTextAsync(filepath, jsoncontent, cancellationToken);
            return true;
        }


        [GeneratedRegex(@"adrplus\.version\.(.+)$", RegexOptions.IgnoreCase)]
        private static partial Regex RegexFileconfigVersion();
    }
}
