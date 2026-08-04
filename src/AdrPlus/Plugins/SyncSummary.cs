// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the AdrPlus project under MIT license
// ***************************************************************************************

namespace AdrPlus.Plugins
{
    /// <summary>
    /// Aggregate counts reported to the user by <c>SyncCommandHandler</c>, shared by both <c>adrplus sync</c>
    /// modes: <see cref="IPluginManager.RetryPendingAsync"/> (default mode, populates every field except
    /// <see cref="Exhausted"/>) and <see cref="IPluginManager.BackfillAsync"/> (<c>--backfill</c>, populates
    /// <see cref="Succeeded"/>/<see cref="Skipped"/>/<see cref="PermanentlyFailed"/>/<see cref="Exhausted"/> only
    /// — <see cref="StillPending"/> and <see cref="Dropped"/> never apply to a backfill sweep). The process exit
    /// code never carries plugin-level signal; this summary is informational only.
    /// </summary>
    internal sealed class SyncSummary
    {
        /// <summary>Items that succeeded.</summary>
        public int Succeeded { get; set; }

        /// <summary>Items removed/skipped because the plugin returned <c>Skipped</c> or its <c>ShouldHandle</c> now returns <see langword="false"/>.</summary>
        public int Skipped { get; set; }

        /// <summary>Default-mode only: entries that failed retryably and remain in <c>pending.json</c> for a future <c>sync</c> run.</summary>
        public int StillPending { get; set; }

        /// <summary>Items removed because the plugin reported a non-retryable failure.</summary>
        public int PermanentlyFailed { get; set; }

        /// <summary>Default-mode only: entries removed because their <c>adrKey</c> no longer resolves to an ADR file.</summary>
        public int Dropped { get; set; }

        /// <summary>
        /// Backfill-mode only: items whose retries exhausted <c>maxAttempts</c> during the sweep — logged only,
        /// never written to <c>pending.json</c> (a re-run of <c>--backfill</c> is the recovery path, not
        /// accumulating pending entries from one bad sweep).
        /// </summary>
        public int Exhausted { get; set; }
    }
}
