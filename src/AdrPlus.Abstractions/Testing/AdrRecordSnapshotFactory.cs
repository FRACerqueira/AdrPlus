// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the AdrPlus project under MIT license
// ***************************************************************************************

using AdrPlus.Abstractions.Domain;

namespace AdrPlus.Abstractions.Testing
{
    /// <summary>
    /// Builds a valid <see cref="AdrRecordSnapshot"/> for use in a plugin author's own unit tests, without
    /// requiring every <c>required</c> field to be filled in by hand.
    /// </summary>
    public static class AdrRecordSnapshotFactory
    {
        /// <summary>
        /// Creates an <see cref="AdrRecordSnapshot"/> with sensible defaults, overriding only the parameters
        /// a test cares about.
        /// </summary>
        /// <param name="number">The ADR's stable sequence number. Defaults to <c>1</c>.</param>
        /// <param name="version">The ADR's version number. Defaults to <c>1</c>.</param>
        /// <param name="revision">The ADR's revision number, if any. Defaults to <see langword="null"/>.</param>
        /// <param name="title">The ADR's title. Defaults to <c>"Sample decision"</c>.</param>
        /// <param name="domain">The ADR's domain. Defaults to <c>"General"</c>.</param>
        /// <param name="scope">The ADR's scope. Defaults to <c>"core"</c>.</param>
        /// <param name="statusCreate">The status when the ADR was created. Defaults to <see cref="AdrStatus.Proposed"/>.</param>
        /// <param name="statusUpdate">The status after an update operation. Defaults to <see cref="AdrStatus.Unknown"/>.</param>
        /// <param name="statusChange">The status after a change operation. Defaults to <see cref="AdrStatus.Unknown"/>.</param>
        /// <param name="createRef">The date reference when the ADR was created. Defaults to <see langword="null"/>.</param>
        /// <param name="updateRef">The date reference when the ADR was updated. Defaults to <see langword="null"/>.</param>
        /// <param name="changeRef">The date reference when the ADR status was changed. Defaults to <see langword="null"/>.</param>
        /// <param name="superseded">The sequence number of the ADR this one supersedes, if any. Defaults to <see langword="null"/>.</param>
        /// <returns>A fully populated, valid <see cref="AdrRecordSnapshot"/>.</returns>
        public static AdrRecordSnapshot Create(
            int number = 1,
            int version = 1,
            int? revision = null,
            string title = "Sample decision",
            string domain = "General",
            string scope = "core",
            AdrStatus statusCreate = AdrStatus.Proposed,
            AdrStatus statusUpdate = AdrStatus.Unknown,
            AdrStatus statusChange = AdrStatus.Unknown,
            DateTime? createRef = null,
            DateTime? updateRef = null,
            DateTime? changeRef = null,
            int? superseded = null) => new()
            {
                Number = number,
                Version = version,
                Revision = revision,
                Title = title,
                Domain = domain,
                Scope = scope,
                StatusCreate = statusCreate,
                StatusUpdate = statusUpdate,
                StatusChange = statusChange,
                CreateRef = createRef,
                UpdateRef = updateRef,
                ChangeRef = changeRef,
                Superseded = superseded
            };
    }
}
