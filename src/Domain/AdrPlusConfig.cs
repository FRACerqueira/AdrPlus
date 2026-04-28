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
        /// <summary>
        /// Language for showing messages, e.g. "en-us", "pt-br". Optional.
        /// </summary>
        public string Language { get; init; } = AppConstants.GetNeutralLanguage();

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
    }
}
