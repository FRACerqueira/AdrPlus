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
        public string Language { get; init; } = AppConstants.GetNeutralLanguage;

        /// <summary>
        /// Gets the command used to open an ADR.
        /// </summary>
        public string ComandOpenAdr { get; init; } = string.Empty;
    }
}
