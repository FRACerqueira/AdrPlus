// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the AdrPlus project under MIT license
// ***************************************************************************************

using AdrPlus.Abstractions;

namespace AdrPlus.Abstractions.Domain
{
    /// <summary>
    /// Immutable public snapshot of an ADR record, exposed to plugins via <see cref="AdrEventContext.Adr"/>.
    /// </summary>
    /// <remarks>
    /// <see cref="Number"/> is the ADR's stable identity across its lifetime; it does not change on
    /// <see cref="AdrEventType.Revised"/>/<see cref="AdrEventType.Versioned"/>. A plugin wanting one external
    /// artifact that persists across revisions must key off <see cref="Number"/>, not a scoped adrKey.
    /// </remarks>
    public sealed record AdrRecordSnapshot
    {
        /// <summary>
        /// Gets the sequence number of the ADR. Stable across versions/revisions.
        /// </summary>
        public required int Number { get; init; }

        /// <summary>
        /// Gets the version number of the ADR.
        /// </summary>
        public required int Version { get; init; }

        /// <summary>
        /// Gets the revision number of the ADR, if any.
        /// </summary>
        public int? Revision { get; init; }

        /// <summary>
        /// Gets the title of the ADR.
        /// </summary>
        public required string Title { get; init; }

        /// <summary>
        /// Gets the domain of the ADR.
        /// </summary>
        public required string Domain { get; init; }

        /// <summary>
        /// Gets the scope of the ADR.
        /// </summary>
        public required string Scope { get; init; }

        /// <summary>
        /// Gets the status when the ADR was created.
        /// </summary>
        public required AdrStatus StatusCreate { get; init; }

        /// <summary>
        /// Gets the status after an update operation.
        /// </summary>
        public required AdrStatus StatusUpdate { get; init; }

        /// <summary>
        /// Gets the status after a change operation.
        /// </summary>
        public required AdrStatus StatusChange { get; init; }

        /// <summary>
        /// Gets the date reference when the ADR was created.
        /// </summary>
        public DateTime? CreateRef { get; init; }

        /// <summary>
        /// Gets the date reference when the ADR was updated.
        /// </summary>
        public DateTime? UpdateRef { get; init; }

        /// <summary>
        /// Gets the date reference when the ADR status was changed.
        /// </summary>
        public DateTime? ChangeRef { get; init; }

        /// <summary>
        /// Gets the sequence number of the ADR that this one supersedes, if any.
        /// </summary>
        public int? Superseded { get; init; }
    }
}
