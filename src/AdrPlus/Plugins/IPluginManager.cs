// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the AdrPlus project under MIT license
// ***************************************************************************************

using AdrPlus.Abstractions;
using AdrPlus.Abstractions.Domain;

namespace AdrPlus.Plugins
{
    /// <summary>
    /// Orchestrates discovery, structural load validation, and foreground dispatch of plugins from two
    /// host-global roots — <see cref="BuiltinPluginsRoot"/> and <see cref="UserPluginsRoot"/>. Background
    /// re-drive and full backfill are separate concerns, handled by <see cref="RetryPendingAsync"/> and
    /// <see cref="BackfillAsync"/>.
    /// </summary>
    internal interface IPluginManager
    {
        /// <summary>
        /// The folder containing plugins bundled with the AdrPlus install itself (e.g. <c>plugins-builtin</c>
        /// next to the tool's own assembly), or empty to disable it. Never repository-scoped.
        /// </summary>
        string BuiltinPluginsRoot { get; }

        /// <summary>
        /// The stable, host-global folder holding plugins installed via <c>adrplus plugins --install</c>
        /// (e.g. <c>%UserProfile%/AdrPlus.Plugins</c>), or empty to disable it. Never repository-scoped.
        /// </summary>
        string UserPluginsRoot { get; }

        /// <summary>
        /// Plugins that passed structural load validation, in discovery order (folders sharing a duplicate name
        /// with another candidate are excluded — neither is loaded).
        /// </summary>
        IReadOnlyList<LoadedPlugin> LoadedPlugins { get; }

        /// <summary>
        /// Candidate plugin subfolders that failed structural load validation. Manifest-level rejections
        /// (invalid manifest, path traversal, not in allowlist) appear in discovery order first, followed by
        /// duplicate-name rejections grouped by name — duplicates can only be detected once every candidate's
        /// manifest has been read, so they cannot be interleaved with the first pass.
        /// </summary>
        IReadOnlyList<PluginRejection> Rejections { get; }

        /// <summary>
        /// Discovers and validates every immediate subfolder of <see cref="BuiltinPluginsRoot"/> and
        /// <see cref="UserPluginsRoot"/> (merged into one candidate set before validation/dedup), populating
        /// <see cref="LoadedPlugins"/> and <see cref="Rejections"/>. A missing or empty root is a no-op for
        /// that root — nothing installed on either is not an error.
        /// </summary>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        Task LoadPluginsAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Dispatches a single lifecycle event to every loaded plugin whose manifest subscribes to
        /// <paramref name="eventType"/> and whose <see cref="IAdrPlugin.ShouldHandle"/> returns <see langword="true"/>.
        /// A no-op if <see cref="LoadPluginsAsync"/> was never called or found nothing to load.
        /// </summary>
        /// <remarks>
        /// Does not take a plugins-root path — call <see cref="LoadPluginsAsync"/> once before the first dispatch
        /// of a run. Single-shot per plugin, no retry: a <c>Failed</c>/timed-out outcome is queued to
        /// <c>pending.json</c> and this method still returns — it never waits out a plugin's own retry
        /// schedule (that's background re-drive, via <see cref="RetryPendingAsync"/>).
        /// </remarks>
        /// <param name="eventType">The lifecycle event being dispatched.</param>
        /// <param name="adr">The snapshot of the ADR this event concerns.</param>
        /// <param name="adrFilePath">The absolute path of the ADR's <c>.md</c> file.</param>
        /// <param name="getAdrRenderedContent">Lazily renders the ADR's full Markdown content — only invoked if a plugin's filter accepts the event.</param>
        /// <param name="repo">The snapshot of the repository configuration relevant to plugins.</param>
        /// <param name="pendingStateRoot">
        /// The repository-scoped root under which each plugin's <c>pending.json</c> is read/written (e.g.
        /// <c>&lt;repo&gt;/plugins-state</c>). Required, and deliberately repository-scoped rather than derived
        /// from a loaded plugin's own (host-global, shared) folder — sharing pending state across repositories
        /// would let one repository's failed dispatch get re-driven against another repository's ADRs.
        /// </param>
        /// <param name="isReplay">Whether this dispatch is a replay (e.g. from <c>adrplus sync --backfill</c>) rather than a live event.</param>
        /// <param name="isActive">
        /// Optional filter over <see cref="LoadedPlugins"/> for this call only — a plugin for which this returns
        /// <see langword="false"/> is skipped entirely (no dispatch, no warning). <see langword="null"/> (the
        /// default) dispatches to every loaded plugin, unfiltered.
        /// </param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        Task DispatchAsync(
            AdrEventType eventType,
            AdrRecordSnapshot adr,
            string adrFilePath,
            Func<string> getAdrRenderedContent,
            RepoInfoSnapshot repo,
            string pendingStateRoot,
            bool isReplay,
            Func<LoadedPlugin, bool>? isActive = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Re-attempts every pending entry across every loaded plugin's <c>pending.json</c>, using each
        /// plugin's <see cref="PluginManifest.RetryPolicy"/> — <c>adrplus sync</c>'s default mode. Unlike
        /// <see cref="DispatchAsync"/>, this runs a real in-process retry loop (with backoff delays) per entry,
        /// bounded by <see cref="PluginManifest.BackgroundTimeoutMs"/> per attempt rather than
        /// <see cref="PluginManifest.ForegroundTimeoutMs"/>.
        /// </summary>
        /// <param name="resolveAdr">
        /// Resolves a pending entry's <c>adrKey</c> back to the ADR's current snapshot, file path, and rendered
        /// content, or <see langword="null"/> if the ADR no longer exists (deleted/renamed since the original
        /// failure) — in which case the entry is dropped. Kept as a callback so <see cref="IPluginManager"/> stays
        /// independent of <c>IAdrServices</c>/<c>AdrPlusRepoConfig</c>; the caller (<c>SyncCommandHandler</c>)
        /// owns ADR resolution.
        /// </param>
        /// <param name="repo">The snapshot of the repository configuration relevant to plugins.</param>
        /// <param name="pendingStateRoot">The repository-scoped root under which each plugin's <c>pending.json</c> is read/written — see <see cref="DispatchAsync"/>'s remarks.</param>
        /// <param name="isActive">
        /// Optional filter over <see cref="LoadedPlugins"/> for this call only — a plugin for which this returns
        /// <see langword="false"/> has its <c>pending.json</c> left completely untouched this run (not retried,
        /// not dropped), so re-activating it later picks up cleanly. <see langword="null"/> (the default)
        /// processes every loaded plugin's pending state, unfiltered.
        /// </param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        Task<SyncSummary> RetryPendingAsync(
            Func<string, (AdrRecordSnapshot Adr, string FilePath, string Content)?> resolveAdr,
            RepoInfoSnapshot repo,
            string pendingStateRoot,
            Func<LoadedPlugin, bool>? isActive = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Re-emits the settled-status event for every ADR in <paramref name="settledAdrs"/> to every loaded
        /// plugin whose manifest subscribes to it — <c>adrplus sync --backfill</c>. Unlike
        /// <see cref="RetryPendingAsync"/>, every item always starts at attempt 1 (a backfill sweep has no prior
        /// state to continue), and an item whose retries exhaust <see cref="PluginRetryPolicy.MaxAttempts"/> is
        /// only logged — never written to <c>pending.json</c> (re-running <c>--backfill</c> is itself the
        /// recovery path). Different plugins are swept in parallel; a single plugin processes its own list of
        /// ADRs sequentially (per-plugin concurrency is fixed at 1, not a manifest-configurable value). If
        /// cancelled mid-sweep, the partial <see cref="SyncSummary"/> accumulated so far is returned rather than lost.
        /// </summary>
        /// <param name="settledAdrs">
        /// Every ADR with a settled (non-<c>Proposed</c>) status, paired with the <see cref="AdrEventType"/> that
        /// status corresponds to. Built by the caller (<c>SyncCommandHandler</c>), which owns reading the repo's
        /// ADRs and determining each one's current status — kept out of <see cref="IPluginManager"/> for the same
        /// reason <see cref="RetryPendingAsync"/>'s resolver callback is.
        /// </param>
        /// <param name="repo">The snapshot of the repository configuration relevant to plugins.</param>
        /// <param name="isActive">
        /// Optional filter over <see cref="LoadedPlugins"/> for this call only — a plugin for which this returns
        /// <see langword="false"/> is skipped entirely for this sweep (not initialized, not counted). <see
        /// langword="null"/> (the default) sweeps every loaded plugin, unfiltered.
        /// </param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <remarks>No <c>pendingStateRoot</c> parameter (unlike <see cref="DispatchAsync"/>/<see cref="RetryPendingAsync"/>): backfill never reads or writes <c>pending.json</c> — an exhausted retry during backfill is logged only, so there is nothing here that touches per-repository state.</remarks>
        Task<SyncSummary> BackfillAsync(
            IEnumerable<(AdrEventType EventType, AdrRecordSnapshot Adr, string FilePath, Func<string> GetContent)> settledAdrs,
            RepoInfoSnapshot repo,
            Func<LoadedPlugin, bool>? isActive = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Disposes every plugin in <see cref="LoadedPlugins"/> — the CLI's graceful-shutdown hook, called once
        /// at process exit. Each plugin's <c>DisposeAsync</c> is invoked independently: one plugin throwing does
        /// not prevent the others from being disposed (fail-soft). Each plugin's isolated
        /// <see cref="System.Runtime.Loader.AssemblyLoadContext"/> is then unloaded on a best-effort basis — not
        /// guaranteed to actually free the assembly in a short-lived CLI process, and not required for correctness.
        /// Idempotent: clears <see cref="LoadedPlugins"/> and <see cref="Rejections"/> afterward, so a second call
        /// is a no-op rather than disposing twice.
        /// </summary>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        Task DisposeLoadedPluginsAsync(CancellationToken cancellationToken = default);
    }
}
