// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the AdrPlus project under MIT license
// ***************************************************************************************

using AdrPlus.Domain;

namespace AdrPlus.Core
{
    /// <summary>
    /// Interface for validating the consistency and fields of the AdrPlus.json configuration file
    /// </summary>
    internal interface IValidateConfig
    {
        /// <summary>
        /// Removes the old version file and creates a new one with the updated version.
        /// </summary>
        /// <param name="currentVersion">The new version to set.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task RecreateVersionFileAsync(string currentVersion, CancellationToken cancellationToken = default);

        /// <summary>
        /// Removes the old version file and creates a new one with the current version.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task RecreateVersionFileAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the path to the version-history directory.
        /// </summary>
        /// <returns>The version-history directory path.</returns>
        string GetHistoryPath();

        /// <summary>
        /// Retrieves the maximum number, version, and revision values from the existing ADR files in the specified root path. 
        /// </summary>
        /// <param name="rootPath">The root path of the ADR repository.</param>
        /// <param name="repoconfig">The repository configuration.</param>
        /// <returns>A Task that represents the asynchronous operation, containing a tuple of (MaxNumber, MaxVersion, MaxRevision)</returns>
        Task<(int MaxNumber, int MaxVersion, int MaxRevision)> GetMaxNumberVersionRevision(string rootPath, AdrPlusRepoConfig repoconfig);

        /// <summary>
        /// Loads the migration pattern configuration from default template file. 
        /// </summary>
        /// <param name="cancellationToken">A cancellation token for the async operation.</param>
        /// <returns>A Task that represents the asynchronous operation, containing the migration pattern configuration as a string.</returns>
        Task<string> LoadPatternsConfigMigration(CancellationToken cancellationToken);

        /// <summary>
        /// Validates the entire configuration and returns a formatted error report
        /// </summary>
        /// <returns>A Task that represents the asynchronous operation, containing a tuple of (isValid, errorMessages)</returns>
        Task<(bool IsValid, string[] ErrorReport)> ValidateAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Validates the structure of the repository JSON content, ensuring required fields are present and correctly formatted, and returns a report of any validation errors. 
        /// </summary>
        /// <param name="jsonContent">
        /// The JSON string to validate against the expected repository structure.
        /// </param>
        /// <returns>A tuple containing a boolean indicating validity and an array of error messages</returns>
        (bool IsValid, string[] ErrorReport) ValidateRepoStructure(string jsonContent);

        /// <summary>
        /// Validates the structure of the application JSON content, ensuring required fields are present and correctly formatted, and returns a report of any validation errors. 
        /// </summary>
        /// <param name="jsonContent">
        /// The JSON string to validate against the expected application structure.
        /// </param>
        /// <returns>A tuple containing a boolean indicating validity and an array of error messages</returns>
        (bool IsValid, string[] ErrorReport) ValidateAppStructure(string jsonContent);

        /// <summary>
        /// Checks if the configuration file exists in the expected location 
        /// </summary>
        /// <returns>True if the configuration file exists, otherwise false</returns>
        bool HasTemplateRepoFile();

        /// <summary>
        /// Gets the full file path of the application configuration file 
        /// </summary>
        /// <returns>
        /// The full file path of the application configuration file 
        /// </returns>
        string GetConfigAppFilePath();

        /// <summary>
        /// Gets the full file path of the configuration file
        /// </summary>
        /// <returns>
        /// The full file path of the configuration file
        /// </returns>
        string GetDefaultConfigRepoFilePath();

        /// <summary>
        /// Gets the full file path of the opt-in first-install seed file (<c>firstinstaller.adrplus</c>).
        /// </summary>
        /// <returns>The full file path of the seed file.</returns>
        string GetFirstInstallerFilePath();

        /// <summary>
        /// Applies the first-install seed file when present, as an alternative to the interactive wizard for
        /// automation/CI/AI-agent scenarios: a team pre-provisions <c>firstinstaller.adrplus</c> with approved
        /// default repository settings, and this consumes it in place of prompting.
        /// </summary>
        /// <remarks>
        /// Only valid before the repository has ever been configured. If the seed file is present after
        /// configuration already exists, this throws instead of silently ignoring it, since that state means
        /// the seed file is being misused (leftover from provisioning, or dropped back in after the fact).
        /// On success, the seed file is renamed with the <see cref="AppConstants.FirstInstallerAppliedSuffix"/>
        /// suffix so it cannot be re-applied on a later run.
        /// </remarks>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns><see langword="true"/> if the seed file was found and applied; <see langword="false"/> if no seed file is present.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the seed file is present but the repository is already configured, or when its content fails repository structure validation.</exception>
        Task<bool> TryApplyFirstInstallerAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves the file name configuration value.
        /// </summary>
        /// <returns>
        /// A string containing the file name configuration.
        /// </returns>
        string GetFileNameRepoConfig();

        /// <summary>
        /// Retrieves the default repository configuration embeded content.
        /// </summary>
        /// <param name="pathadr">The path to the ADR folder, used to replace the placeholder in the template content.</param>
        /// <param name="cancellationToken">Cancellation token for the async operation</param>
        /// <returns>A Task that represents the asynchronous operation, containing a string with the default repository configuration.</returns>
        Task<string> GetConfigDefaultRepoContentAsync(string pathadr, CancellationToken cancellationToken = default);

        /// <summary>
        /// Validates and adjusts the structure of the specified repository JSON content to ensure required fields are present.
        /// </summary>
        /// <param name="jsonContent">The JSON string to validate and adjust.</param>
        /// <returns>A JSON string with the ensured fields structure.</returns>
        string EnsureFieldsRepoStructure(string jsonContent);

        /// <summary>
        /// Ensures the ADR Markdown template file exists on disk. When it is missing, extracts the appropriate embedded resource
        /// (Portuguese for cultures starting with <c>pt-</c>, English otherwise) and writes it to the <c>template</c> directory.
        /// </summary>
        /// <param name="appculture">The application culture string (e.g. "pt-BR"). Null or whitespace defaults to the English template.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        Task InitializeTemplateAsync(string? appculture, CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves a repository template , which can be used as a starting point for creating or validating the configuration. 
        /// </summary>
        /// <param name="cancellationToken">Cancellation token for the async operation</param>
        /// <returns>
        /// A Task that represents the asynchronous operation, containing a string with the configuration template.
        /// </returns>
        Task<string> GetConfigRepoTemplateAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves a template for ADR, which can be used as a starting point for creating or validating the configuration. 
        /// </summary>
        /// <param name="cancellationToken">Cancellation token for the async operation</param>
        /// <returns>
        /// A Task that represents the asynchronous operation, containing a string with the configuration template.
        /// </returns>
        Task<string> GetConfigAdrTemplateAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the full file path of the ADR template configuration file  
        /// </summary>
        /// <returns>The full file path of the ADR template configuration file.</returns>
        string GetConfigAdrTemplatePath();
    }
}
