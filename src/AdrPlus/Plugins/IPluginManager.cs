// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the AdrPlus project under MIT license
// ***************************************************************************************

using AdrPlus.Abstractions;
using AdrPlus.Abstractions.Domain;

namespace AdrPlus.Plugins
{
    /// <summary>
    /// Orchestrates discovery, structural load validation, and foreground dispatch of plugins under
    /// <c>./plugins/&lt;name&gt;/</c> (spec §4.2). Background re-drive and full backfill are out of scope — see
    /// Fase 5/6.
    /// </summary>
    internal interface IPluginManager
    {
        /// <summary>
        /// Plugins that passed structural load validation, in discovery order (folders sharing a duplicate name
        /// with another candidate are excluded — D22).
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
        /// Discovers and validates every immediate subfolder of <paramref name="pluginsRootPath"/>, populating
        /// <see cref="LoadedPlugins"/> and <see cref="Rejections"/>. A missing <paramref name="pluginsRootPath"/>
        /// is a no-op (empty repo without a <c>./plugins</c> folder is not an error).
        /// </summary>
        /// <param name="pluginsRootPath">The full path to the repository's <c>./plugins</c> folder.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        Task LoadPluginsAsync(string pluginsRootPath, CancellationToken cancellationToken = default);

        /// <summary>
        /// Dispatches a single lifecycle event to every loaded plugin whose manifest subscribes to
        /// <paramref name="eventType"/> and whose <see cref="IAdrPlugin.ShouldHandle"/> returns <see langword="true"/>
        /// (spec §4.2). A no-op if <see cref="LoadPluginsAsync"/> was never called or found nothing to load.
        /// </summary>
        /// <remarks>
        /// Does not take a plugins-root path — call <see cref="LoadPluginsAsync"/> once before the first dispatch
        /// of a run. Single-shot per plugin, no retry (D27): a <c>Failed</c>/timed-out outcome is queued to
        /// <c>state/pending.json</c> and this method still returns — it never waits out a plugin's own retry
        /// schedule (that's background re-drive, Fase 5).
        /// </remarks>
        /// <param name="eventType">The lifecycle event being dispatched.</param>
        /// <param name="adr">The snapshot of the ADR this event concerns.</param>
        /// <param name="adrFilePath">The absolute path of the ADR's <c>.md</c> file.</param>
        /// <param name="getAdrRenderedContent">Lazily renders the ADR's full Markdown content — only invoked if a plugin's filter accepts the event.</param>
        /// <param name="repo">The snapshot of the repository configuration relevant to plugins.</param>
        /// <param name="isReplay">Whether this dispatch is a replay (e.g. from <c>adrplus sync --backfill</c>, Fase 6) rather than a live event.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        Task DispatchAsync(
            AdrEventType eventType,
            AdrRecordSnapshot adr,
            string adrFilePath,
            Func<string> getAdrRenderedContent,
            RepoInfoSnapshot repo,
            bool isReplay,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Re-attempts every pending entry across every loaded plugin's <c>state/pending.json</c>, using each
        /// plugin's <see cref="PluginManifest.RetryPolicy"/> (spec §4.4) — <c>adrplus sync</c>'s default mode
        /// (Fase 5). Unlike <see cref="DispatchAsync"/>, this runs a real in-process retry loop (with backoff
        /// delays) per entry, bounded by <see cref="PluginManifest.TimeoutMs"/> per attempt rather than
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
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        Task<SyncSummary> RetryPendingAsync(
            Func<string, (AdrRecordSnapshot Adr, string FilePath, string Content)?> resolveAdr,
            RepoInfoSnapshot repo,
            CancellationToken cancellationToken = default);
    }
}
