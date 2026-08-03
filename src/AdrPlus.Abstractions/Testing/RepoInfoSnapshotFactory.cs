// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the AdrPlus project under MIT license
// ***************************************************************************************

using AdrPlus.Abstractions.Domain;

namespace AdrPlus.Abstractions.Testing
{
    /// <summary>
    /// Builds a valid <see cref="RepoInfoSnapshot"/> for use in a plugin author's own unit tests, without
    /// requiring every <c>required</c> field to be filled in by hand.
    /// </summary>
    public static class RepoInfoSnapshotFactory
    {
        /// <summary>
        /// Creates a <see cref="RepoInfoSnapshot"/> with sensible defaults, overriding only the parameters
        /// a test cares about.
        /// </summary>
        /// <param name="folderAdr">The folder path where ADR files are stored. Defaults to <c>"docs/adr"</c>.</param>
        /// <param name="scopes">The configured scopes for organizing ADRs. Defaults to a single <c>"core"</c> scope.</param>
        /// <param name="statusMapping">
        /// The mapping between <see cref="AdrStatus"/> values and their configured string representations.
        /// Defaults to each <see cref="AdrStatus"/> value mapped to its own enum name.
        /// </param>
        /// <returns>A fully populated, valid <see cref="RepoInfoSnapshot"/>.</returns>
        public static RepoInfoSnapshot Create(
            string folderAdr = "docs/adr",
            IReadOnlyList<string>? scopes = null,
            IReadOnlyDictionary<AdrStatus, string>? statusMapping = null) => new()
            {
                FolderAdr = folderAdr,
                Scopes = scopes ?? ["core"],
                StatusMapping = statusMapping ?? DefaultStatusMapping
            };

        private static readonly IReadOnlyDictionary<AdrStatus, string> DefaultStatusMapping =
            Enum.GetValues<AdrStatus>().ToDictionary(status => status, status => status.ToString());
    }
}
