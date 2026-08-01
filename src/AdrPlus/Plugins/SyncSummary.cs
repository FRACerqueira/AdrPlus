// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the AdrPlus project under MIT license
// ***************************************************************************************

namespace AdrPlus.Plugins
{
    /// <summary>
    /// Aggregate counts across every loaded plugin's <c>pending.json</c> after one <see cref="IPluginManager.RetryPendingAsync"/>
    /// run — reported to the user by <c>SyncCommandHandler</c>. Per D28, the exit code never carries plugin-level
    /// signal; this summary is informational only.
    /// </summary>
    internal sealed class SyncSummary
    {
        /// <summary>Entries that succeeded and were removed from <c>pending.json</c>.</summary>
        public int Succeeded { get; set; }

        /// <summary>Entries removed because the plugin returned <c>Skipped</c> or its <c>ShouldHandle</c> now returns <see langword="false"/>.</summary>
        public int Skipped { get; set; }

        /// <summary>Entries that failed retryably and remain in <c>pending.json</c> for a future <c>sync</c> run.</summary>
        public int StillPending { get; set; }

        /// <summary>Entries removed because the plugin reported a non-retryable failure.</summary>
        public int PermanentlyFailed { get; set; }

        /// <summary>Entries removed because their <c>adrKey</c> no longer resolves to an ADR file.</summary>
        public int Dropped { get; set; }
    }
}
