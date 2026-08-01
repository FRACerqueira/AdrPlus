// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the AdrPlus project under MIT license
// ***************************************************************************************

namespace AdrPlus.Plugins
{
    /// <summary>
    /// Result of <see cref="PluginManager.RunAttemptLoopAsync"/> — shared between the Fase 5 pending re-drive
    /// (<c>RetryEntryAsync</c>) and the Fase 6 backfill sweep (<c>BackfillPluginAsync</c>). Only the bookkeeping
    /// around the loop differs between the two callers, not the loop itself.
    /// </summary>
    internal enum AttemptLoopResult
    {
        Succeeded,
        Skipped,
        PermanentlyFailed,
        /// <summary>Every allowed attempt this run was made and none succeeded — the caller decides what to do
        /// with that (Fase 5: keep the pending entry; Fase 6: log only, never persist).</summary>
        Exhausted
    }

    /// <param name="Result">The final outcome of the attempt loop.</param>
    /// <param name="AttemptsMade">The absolute attempt number (1-based, cumulative across runs) reached.</param>
    /// <param name="LastError">The last failure's message, when <see cref="Result"/> is <see cref="AttemptLoopResult.PermanentlyFailed"/> or <see cref="AttemptLoopResult.Exhausted"/>.</param>
    internal readonly record struct AttemptLoopOutcome(AttemptLoopResult Result, int AttemptsMade, string? LastError);
}
