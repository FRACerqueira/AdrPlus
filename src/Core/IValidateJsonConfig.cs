// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the AdrPlus project under MIT license
// ***************************************************************************************

namespace AdrPlus.Core
{
    /// <summary>
    /// Interface for validating the consistency and fields of the AdrPlus.json configuration file
    /// </summary>
    internal interface IValidateJsonConfig
    {
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
