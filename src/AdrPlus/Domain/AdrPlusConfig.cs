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

        /// <summary>
        /// Optional allowlist restricting which plugins under ./plugins may be loaded, matched by name. Null means the allowlist is disabled (all plugins load); an empty list means no plugin loads.
        /// </summary>
        public List<PluginAllowlistEntry>? PluginAllowlist { get; set; }
    }

    /// <summary>
    /// An entry in the plugin allowlist. <see cref="Hash"/> is accepted for forward-compatibility but not yet enforced.
    /// </summary>
    internal sealed class PluginAllowlistEntry
    {
        /// <summary>
        /// The plugin name, matched case-insensitively against the plugin's manifest.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// An optional assembly hash. Not enforced in v1 — present only to avoid a future schema change.
        /// </summary>
        public string? Hash { get; set; }
    }
}
