// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the AdrPlus project under MIT license
// ***************************************************************************************

namespace AdrPlus.Abstractions.Domain
{
    /// <summary>
    /// Public mirror of the host's internal ADR status, exposed to plugins via <see cref="AdrRecordSnapshot"/> and <see cref="RepoInfoSnapshot"/>.
    /// </summary>
    public enum AdrStatus
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
