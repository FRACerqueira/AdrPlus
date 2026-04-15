// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the AdrPlus project under MIT license
// ***************************************************************************************

using AdrPlus.Core;

namespace AdrPlus.Domain
{
    /// <summary>
    /// Represents the configuration options for ADR Plus application.
    /// Maps to the root structure of AdrPlus.json configuration file.
    /// </summary>
    internal sealed record AdrPlusConfig
    {
        private string? _normalizedFolderCache;

        /// <summary>
        /// Language for showing messages, e.g. "en-us", "pt-br". Optional.
        /// </summary>
        public string Language { get; init; } = AppConstants.GetNeutralLanguage();

        /// <summary>
        /// Folder for the ADRs in the repository, e.g. "docs/adr". Optional, can be empty for the root folder.
        /// </summary>
        public string FolderRepo { get; init; } = string.Empty;

        /// <summary>
        /// Gets the command used to open an ADR.
        /// </summary>
        public string ComandOpenAdr { get; init; } = string.Empty;

        /// <summary>
        /// Gets or sets the character used for yes responses. Optional, can be empty for the default language (pt-br or en-us).
        /// </summary>
        public string YesValue { get; init; } = string.Empty;

        /// <summary>
        /// Gets or sets the character used for no responses. Optional, can be empty for the default language (pt-br or en-us).
        /// </summary>
        public string NoValue { get; init; } = string.Empty;

        /// <summary>
        /// Gets the normalized folder path with proper directory separators for the current operating system.
        /// Replaces both forward slashes and backslashes with the platform-specific directory separator.
        /// The result is cached after the first call for improved performance.
        /// </summary>
        /// <returns>The normalized folder path string.</returns>
        public string GetFolderNormalized()
        {
            return _normalizedFolderCache ??= FolderRepo
                .Replace('\\', Path.DirectorySeparatorChar)
                .Replace('/', Path.DirectorySeparatorChar);
        }

    }
}
