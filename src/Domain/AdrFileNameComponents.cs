// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the AdrPlus project under MIT license
// ***************************************************************************************

namespace AdrPlus.Domain
{
    /// <summary>
    /// Represents the parsed components of an ADR filename.
    /// </summary>
    internal sealed class AdrFileNameComponents
    {
        /// <summary>
        /// Gets or sets the prefix of the ADR filename.
        /// </summary>
        public string Prefix { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the sequence number of the ADR.
        /// </summary>
        public int Number { get; set; }

        /// <summary>
        /// Gets or sets the title of the ADR.
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the version number of the ADR, if versioning is enabled.
        /// </summary>
        public int Version { get; set; }

        /// <summary>
        /// Gets or sets the revision number of the ADR, if revisions are enabled.
        /// </summary>
        public int? Revision { get; set; }

        /// <summary>
        /// Gets or sets the scope of the ADR.
        /// </summary>
        public string? Scope { get; set; }

        /// <summary>
        /// Gets or sets the domain of the ADR.
        /// </summary>
        public string? Domain { get; set; }

        /// <summary>
        /// Gets or sets the sequence number of the ADR that supersedes this one.
        /// </summary>
        public int? SupersededValue { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the filename was parsed successfully.
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// Gets or sets the error message if parsing failed.
        /// </summary>
        public string ErrorMessage { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the full filename.
        /// </summary>
        public string FileName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the parsed ADR header information.
        /// </summary>
        public AdrHeader Header { get; set; } = new AdrHeader();

        /// <summary>
        /// Gets or sets the content of the ADR file.
        /// </summary>
        public string?  ContentAdr { get; set; }

        /// <summary>
        /// Gets the unique title combining the title and domain.
        /// </summary>
        public string UniqueTitle => CreateUniqueTitle(Title, Domain);

        /// <summary>
        /// Creates a unique title by concatenating <paramref name="title"/> and <paramref name="domain"/>.
        /// Used to distinguish ADRs that share a title but differ by domain.
        /// </summary>
        /// <param name="title">The ADR title.</param>
        /// <param name="domain">The ADR domain (optional). When <see langword="null"/>, an empty string is appended.</param>
        /// <returns>A concatenated unique title string of the form <c>&lt;title&gt;&lt;domain&gt;</c>.</returns>
        public static string CreateUniqueTitle(string title, string? domain)
        {
            return $"{title}{domain ?? string.Empty}";
        }
    }
}
