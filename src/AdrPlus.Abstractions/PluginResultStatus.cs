// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the AdrPlus project under MIT license
// ***************************************************************************************

namespace AdrPlus.Abstractions
{
    /// <summary>
    /// Outcome of a plugin's reaction to an ADR lifecycle event.
    /// </summary>
    public enum PluginResultStatus
    {
        /// <summary>
        /// The plugin handled the event successfully.
        /// </summary>
        Success,

        /// <summary>
        /// The plugin deliberately chose not to act on this event.
        /// </summary>
        Skipped,

        /// <summary>
        /// The plugin attempted to handle the event and failed. See <see cref="PluginResult.IsRetryable"/>.
        /// </summary>
        Failed
    }
}
