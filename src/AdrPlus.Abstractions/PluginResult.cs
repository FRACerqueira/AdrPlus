// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the AdrPlus project under MIT license
// ***************************************************************************************

namespace AdrPlus.Abstractions
{
    /// <summary>
    /// The structured outcome a plugin returns from <see cref="IAdrPlugin.OnAdrEventAsync"/>.
    /// </summary>
    public sealed record PluginResult
    {
        /// <summary>
        /// Gets the outcome of the plugin's reaction to the event.
        /// </summary>
        public required PluginResultStatus Status { get; init; }

        /// <summary>
        /// Gets an optional human-readable message, surfaced in host warnings and file logs on failure.
        /// </summary>
        public string? Message { get; init; }

        /// <summary>
        /// Gets an optional external identifier (e.g. a Confluence page id) the plugin can use for idempotent upserts on retry/replay.
        /// </summary>
        public string? ExternalKey { get; init; }

        /// <summary>
        /// Gets whether a <see cref="PluginResultStatus.Failed"/> outcome is worth retrying.
        /// Set to <c>false</c> for permanent/configuration failures (e.g. invalid credentials) that would fail identically on every retry —
        /// the host will not queue those for background re-drive.
        /// </summary>
        public bool IsRetryable { get; init; } = true;
    }
}
