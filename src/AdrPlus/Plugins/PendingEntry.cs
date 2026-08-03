// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the AdrPlus project under MIT license
// ***************************************************************************************

namespace AdrPlus.Plugins
{
    /// <summary>
    /// A single pending re-drive entry in a plugin's <c>pending.json</c>. Written by the host after the
    /// single foreground attempt fails or times out; read and retried by <c>adrplus sync</c>.
    /// </summary>
    internal sealed class PendingEntry
    {
        /// <summary>
        /// Stable per-version-and-revision identity of the ADR this entry concerns, in the form
        /// <c>"{Number:D4}-v{Version}-r{Revision}"</c> (e.g. <c>"0007-v1-r0"</c>) — see <see cref="AdrKeyFormatter"/>,
        /// the single source of this format.
        /// </summary>
        public string AdrKey { get; set; } = string.Empty;

        /// <summary>
        /// The <see cref="AdrPlus.Abstractions.AdrEventType"/> name that was being dispatched.
        /// </summary>
        public string EventType { get; set; } = string.Empty;

        /// <summary>
        /// The correlation id shared with the host's file log and the plugin's own log entries for this dispatch.
        /// </summary>
        public string CorrelationId { get; set; } = string.Empty;

        /// <summary>
        /// The plugin's failure message, or a host-synthesized message on timeout.
        /// </summary>
        public string? LastError { get; set; }

        /// <summary>
        /// Attempts made so far: <c>1</c> if the plugin's hook actually ran and returned/threw a failure,
        /// <c>0</c> if the foreground attempt never completed (timed out).
        /// </summary>
        public int Attempts { get; set; }

        /// <summary>
        /// When this entry was written (UTC).
        /// </summary>
        public DateTime Timestamp { get; set; }
    }
}
