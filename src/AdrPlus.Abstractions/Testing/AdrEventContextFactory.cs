// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the AdrPlus project under MIT license
// ***************************************************************************************

using AdrPlus.Abstractions.Domain;

namespace AdrPlus.Abstractions.Testing
{
    /// <summary>
    /// Builds a valid <see cref="AdrEventContext"/> for use in a plugin author's own unit tests, without
    /// requiring every <c>required</c> field — including the nested <see cref="AdrRecordSnapshot"/> and
    /// <see cref="RepoInfoSnapshot"/> — to be filled in by hand.
    /// </summary>
    /// <remarks>
    /// <see cref="AdrEventContext"/> is a <see langword="record"/>: once built, use a <c>with</c> expression
    /// for any further one-off overrides <see cref="Create"/>'s parameters don't cover.
    /// </remarks>
    public static class AdrEventContextFactory
    {
        /// <summary>
        /// Creates an <see cref="AdrEventContext"/> with sensible defaults, overriding only the parameters
        /// a test cares about.
        /// </summary>
        /// <param name="eventType">The lifecycle event that triggered this dispatch. Defaults to <see cref="AdrEventType.Approved"/>.</param>
        /// <param name="isReplay">Whether this dispatch is a replay rather than a live event. Defaults to <see langword="false"/>.</param>
        /// <param name="adr">
        /// The ADR snapshot this event concerns. Defaults to <see cref="AdrRecordSnapshotFactory.Create"/>'s
        /// own defaults.
        /// </param>
        /// <param name="adrFilePath">The absolute path of the ADR's <c>.md</c> file. Defaults to a sample path.</param>
        /// <param name="renderedContent">
        /// The ADR's rendered Markdown content, wrapped in a delegate to satisfy <see cref="AdrEventContext.GetAdrRenderedContent"/>.
        /// Defaults to a short sample document. Ignored if <paramref name="getAdrRenderedContent"/> is supplied.
        /// </param>
        /// <param name="getAdrRenderedContent">
        /// Overrides <paramref name="renderedContent"/> when a test needs the delegate itself to be lazy,
        /// throw, or be invoked a specific number of times.
        /// </param>
        /// <param name="repo">
        /// The repository configuration snapshot. Defaults to <see cref="RepoInfoSnapshotFactory.Create"/>'s
        /// own defaults.
        /// </param>
        /// <param name="correlationId">The dispatch correlation id. Defaults to a new GUID.</param>
        /// <returns>A fully populated, valid <see cref="AdrEventContext"/>.</returns>
        public static AdrEventContext Create(
            AdrEventType eventType = AdrEventType.Approved,
            bool isReplay = false,
            AdrRecordSnapshot? adr = null,
            string adrFilePath = "docs/adr/ADR0001V01-sample-decision.md",
            string renderedContent = "# Sample decision\n\nSample content.",
            Func<string>? getAdrRenderedContent = null,
            RepoInfoSnapshot? repo = null,
            string? correlationId = null) => new()
            {
                EventType = eventType,
                IsReplay = isReplay,
                Adr = adr ?? AdrRecordSnapshotFactory.Create(),
                AdrFilePath = adrFilePath,
                GetAdrRenderedContent = getAdrRenderedContent ?? (() => renderedContent),
                Repo = repo ?? RepoInfoSnapshotFactory.Create(),
                CorrelationId = correlationId ?? Guid.NewGuid().ToString()
            };
    }
}
