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
        /// Language for the tool's UI and ADR templates, e.g. "en-us", "pt-br", "de-de", "es-es", "fr-fr", "it-it", "ja-jp", "ko-kr", "nl-be", "ru-ru", "zh-cn". Optional.
        /// </summary>
        public string Language { get; set; } = AppConstants.GetNeutralLanguage;

        /// <summary>
        /// Gets the command used to open an ADR.
        /// </summary>
        public string ComandOpenAdr { get; set; } = string.Empty;

        /// <summary>
        /// Gets the behavior of the application when no arguments are provided.
        /// </summary>
        public BehaviorWithoutArg WithoutArgs { get; set; } = BehaviorWithoutArg.Help;
    }
}
