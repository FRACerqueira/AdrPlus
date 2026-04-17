// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the AdrPlus project under MIT license
// ***************************************************************************************

namespace AdrPlus.Domain
{

    /// <summary>
    /// Represents the status of an ADR (Architecture Decision Record).
    /// </summary>
    internal enum AdrStatus
    {
        /// <summary>
        /// Indicates an unknown or unspecified value.
        /// </summary>
        Unknown,

        /// <summary>
        /// Draft open for proposed discussion.
        /// </summary>
        Proposed,

        /// <summary>
        /// Approved and ready for implementation.
        /// </summary>
        Accepted,

        /// <summary>
        /// Decision not adopted (record rationale).
        /// </summary>
        Rejected,

        /// <summary>
        /// A new decision has been made that invalidates the previous one; maintain link and history.
        /// </summary>
        Superseded
    }
}
