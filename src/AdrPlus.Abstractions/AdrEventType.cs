// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the AdrPlus project under MIT license
// ***************************************************************************************

namespace AdrPlus.Abstractions
{
    /// <summary>
    /// Identifies the ADR lifecycle event that triggered a plugin dispatch.
    /// Plugins MUST treat unknown/future values as <see cref="PluginResultStatus.Skipped"/> rather than throwing.
    /// </summary>
    public enum AdrEventType
    {
        /// <summary>
        /// An ADR was created. Content is metadata-only scaffolding at this point.
        /// </summary>
        Created,

        /// <summary>
        /// A new version of an ADR was created. Content is metadata-only scaffolding at this point.
        /// </summary>
        Versioned,

        /// <summary>
        /// An ADR was revised. Content is metadata-only scaffolding at this point (may start from an empty draft).
        /// </summary>
        Revised,

        /// <summary>
        /// An ADR was marked as superseded by another ADR. Content is settled.
        /// </summary>
        Superseded,

        /// <summary>
        /// An ADR was approved. Content is settled.
        /// </summary>
        Approved,

        /// <summary>
        /// An ADR was rejected. Content is settled.
        /// </summary>
        Rejected,

        /// <summary>
        /// A previous status change on an ADR was undone. Content is settled.
        /// </summary>
        StatusUndone,

        /// <summary>
        /// An ADR was migrated. Content is settled.
        /// </summary>
        Migrated
    }
}
