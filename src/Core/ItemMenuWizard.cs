// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the AdrPlus project under MIT license
// ***************************************************************************************

namespace AdrPlus.Core
{
    /// <summary>
    /// Represents the configuration for an item menu wizard, including its identifier, title, description, and enabled state when not configured.
    /// </summary>
    internal sealed record ItemMenuWizard
    {
        /// <summary>
        /// Gets the unique identifier for the item menu wizard, which is used to reference it in configuration and code.
        /// </summary>
        public string Id { get; init; } = string.Empty;
        /// <summary>
        /// Gets the title of the item menu wizard, which is displayed in the user interface.
        /// </summary>
        public string Title { get; init; } = string.Empty;
        /// <summary>
        /// Gets the description of the item menu wizard, providing additional information to the user.
        /// </summary>  
        public string Description { get; init; } = string.Empty;
        /// <summary>
        /// Gets a value indicating whether the item menu wizard is enabled when not configured.
        /// </summary>
        public bool EnabledWhenNotConfigured { get; init; }
    }
}
