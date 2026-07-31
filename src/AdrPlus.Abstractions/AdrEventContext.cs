// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the AdrPlus project under MIT license
// ***************************************************************************************

using AdrPlus.Abstractions.Domain;

namespace AdrPlus.Abstractions
{
    /// <summary>
    /// Immutable event payload delivered to a plugin's <see cref="IAdrPlugin.OnAdrEventAsync"/>.
    /// </summary>
    public sealed record AdrEventContext
    {
        /// <summary>
        /// Gets the lifecycle event that triggered this dispatch.
        /// </summary>
        public required AdrEventType EventType { get; init; }

        /// <summary>
        /// Gets whether this dispatch is a replay (e.g. from <c>adrplus sync --backfill</c>) rather than a live, first-time event.
        /// </summary>
        public required bool IsReplay { get; init; }

        /// <summary>
        /// Gets the snapshot of the ADR this event concerns.
        /// </summary>
        public required AdrRecordSnapshot Adr { get; init; }

        /// <summary>
        /// Gets the absolute path of the ADR's <c>.md</c> file.
        /// </summary>
        public required string AdrFilePath { get; init; }

        /// <summary>
        /// Gets a delegate that renders the ADR's full Markdown content on demand.
        /// </summary>
        /// <remarks>
        /// Lazy by design: rendering only happens if a plugin's <c>subscribedEvents</c>/<see cref="IAdrPlugin.ShouldHandle"/>
        /// filter actually decides to handle the event, so un-subscribed events stay cheap to dispatch.
        /// </remarks>
        public required Func<string> GetAdrRenderedContent { get; init; }

        /// <summary>
        /// Gets the snapshot of the repository configuration relevant to plugins.
        /// </summary>
        public required RepoInfoSnapshot Repo { get; init; }

        /// <summary>
        /// Gets the correlation id for this dispatch, for cross-referencing plugin logs with the host's file log.
        /// </summary>
        public required string CorrelationId { get; init; }
    }
}
