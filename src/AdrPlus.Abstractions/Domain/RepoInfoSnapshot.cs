// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the AdrPlus project under MIT license
// ***************************************************************************************

namespace AdrPlus.Abstractions.Domain
{
    /// <summary>
    /// Immutable public snapshot of the parts of the repository configuration relevant to plugins,
    /// exposed via <see cref="AdrEventContext.Repo"/>.
    /// </summary>
    /// <remarks>
    /// Deliberately excludes filename/header-formatting settings (prefix, separators, digit lengths, case
    /// transform, header label strings) — those exist to build the <c>.md</c> filename and header the host
    /// already writes, not information a plugin needs to react to an event.
    /// </remarks>
    public sealed record RepoInfoSnapshot
    {
        /// <summary>
        /// Gets the folder path where ADR files are stored, e.g., "docs/adr".
        /// </summary>
        public required string FolderAdr { get; init; }

        /// <summary>
        /// Gets the mapping between <see cref="AdrStatus"/> values and their configured, localized string representations.
        /// </summary>
        public required IReadOnlyDictionary<AdrStatus, string> StatusMapping { get; init; }
    }
}
