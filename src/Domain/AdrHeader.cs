// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the AdrPlus project under MIT license
// ***************************************************************************************

namespace AdrPlus.Domain
{
    /// <summary>
    /// Represents the parsed header components of an ADR file.
    /// </summary>
    internal sealed record AdrHeader
    {
        /// <summary>
        /// Gets or sets the disclaimer text from the ADR header.
        /// </summary>
        public string Disclaimer { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the version number from the ADR header.
        /// </summary>
        public int Version { get; set; }

        /// <summary>
        /// Gets or sets the revision number from the ADR header.
        /// </summary>
        public int? Revision { get; set; }

        /// <summary>
        /// Gets or sets the scope from the ADR header.
        /// </summary>
        public string Scope { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the domain from the ADR header.
        /// </summary>
        public string Domain { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the creation status from the ADR header.
        /// </summary>
        public AdrStatus StatusCreate { get; set; } = AdrStatus.Unknown;

        /// <summary>
        /// Gets or sets the creation date from the ADR header.
        /// </summary>
        public DateTime? DateCreate { get; set; }

        /// <summary>
        /// Gets or sets the update status from the ADR header.
        /// </summary>
        public AdrStatus StatusUpdate { get; set; } = AdrStatus.Unknown;

        /// <summary>
        /// Gets or sets the update date from the ADR header.
        /// </summary>
        public DateTime? DateUpdate { get; set; }

        /// <summary>
        /// Gets or sets the change status from the ADR header.
        /// </summary>
        public AdrStatus StatusChange { get; set; } = AdrStatus.Unknown;

        /// <summary>
        /// Gets or sets the name of the file that this file supersedes.
        /// </summary>
        public string FileSuperSedes { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the change date from the ADR header.
        /// </summary>
        public DateTime? DateChange { get; set; }

        /// <summary>
        /// Gets or sets the title from the ADR header.
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a value indicating whether the header was parsed successfully.
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// Gets or sets the error message if header parsing failed.
        /// </summary>
        public string ErrorMessage { get; set; } = string.Empty;
    }
}