// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the AdrPlus project under MIT license
// ***************************************************************************************

using System.Text.Json.Serialization;

namespace AdrPlus.Domain
{
    /// <summary>
    /// Represents the configuration for ADR Plus Repository.
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of the <see cref="AdrPlusRepoConfig"/> class with default values.
    /// </remarks>
    /// <param name="folderadr">The default folder for ADRs.</param>
    /// <param name="template">The default template content.</param>
    internal sealed class AdrPlusRepoConfig(string folderadr, string template)
    {

        /// <summary>
        /// Folder for the ADRs in the repository, e.g. "docs/adr".
        /// </summary>
        public string FolderAdr { get; set; } = folderadr;

        /// <summary>
        /// Gets the template string used for formatting or processing content ADR.
        /// </summary>
        public string Template { get; set; } = template;

        /// <summary>
        /// Optional prefix for the filename, e.g. "ADR-0001-My-ADR.md"
        /// </summary>
        public string? Prefix { get; set; } = Resources.AdrPlus.DefaultPrefix;

        /// <summary>
        /// Number of digits for the sequence number, e.g. 4 for "0001". Must be greater than zero.
        /// </summary>
        public int LenSeq { get; set; } = 4;

        /// <summary>
        /// Number of digits for the version number, e.g. 2 for "v01". Zero suggested for no versioning.
        /// </summary>
        public int LenVersion { get; set; } = 2;

        /// <summary>
        /// Number of digits for the revision number, e.g. 2 for "rev01". Zero suggested for no versioning.
        /// </summary>
        public int LenRevision { get; set; }

        /// <summary>
        /// Number of digits for the scope text, e.g. 1 for "E". Zero suggested for no versioning.
        /// </summary>
        public int LenScope { get; set; }

        /// <summary>
        /// Separator for different parts of the filename.
        /// </summary>
        public char Separator { get; set; } = '-';

        /// <summary>
        /// Case transformation for the Title filename, e.g. "CamelCase", "PascalCase", "SnakeCase", "KebabCase"
        /// </summary>
        public CaseFormat CaseTransform { get; set; } = CaseFormat.PascalCase;

        /// <summary>
        /// Status for new ADRs, e.g. "Propose". Required for creating a new ADR.
        /// </summary>  
        public string StatusNew { get; set; } = Resources.AdrPlus.StatusNew;

        /// <summary>
        /// Status for accepted ADRs, e.g. "Accepted". Required for changing status ADR. Valid only if status equal to "statusnew".
        /// </summary>
        public string StatusAcc { get; set; } = Resources.AdrPlus.StatusAcc;

        /// <summary>
        /// Status for rejected ADRs, e.g. "Rejected". Optional status. Valid only if status equal to "statusnew".
        /// </summary>
        public string StatusRej { get; set; } = Resources.AdrPlus.StatusRej;

        /// <summary>
        /// Status for superseded ADRs, e.g. "Superseded". Required for changing status ADR. Valid only if status equal to "statusacc".
        /// </summary>
        public string StatusSup { get; set; } = Resources.AdrPlus.StatusSup;

        /// <summary>
        /// Semicolon-separated list of scopes, e.g. "Enterprise;Domain;Project"
        /// </summary>
        public string Scopes { get; set; } = string.Empty;

        /// <summary>
        /// Whether to create subfolders for each scope, e.g. "true" or "false"
        /// </summary>
        public bool FolderByScope { get; set; }

        /// <summary>
        /// Scope for which to skip the domain section in the filename, e.g. "Enterprise". Must be one of the scopes defined in "scopes". Optional.
        /// </summary>
        public string SkipDomain { get; set; } = string.Empty;

        /// <summary>
        /// Template for the header of the ADR. Optional.
        /// </summary>
        public string HeaderDisclaimer { get; set; } = Resources.AdrPlus.DefaultHeaderDisclaimer;

        /// <summary>
        /// Template for the status section of the ADR. Optional. 
        /// </summary>
        public string HeaderStatus { get; set; } = Resources.AdrPlus.DefaultTextStatus;

        /// <summary>
        /// Template for the version section of the ADR. Optional. 
        /// </summary>
        public string HeaderVersion { get; set; } = Resources.AdrPlus.Version;

        /// <summary>
        /// Template for the revision section of the ADR. Optional.
        /// </summary>
        public string HeaderRevision { get; set; } = Resources.AdrPlus.Revision;

        /// <summary>
        /// Gets the mapping between ADR status enum values and their string representations.
        /// </summary>
        [JsonIgnore]
        public Dictionary<AdrStatus, string> StatusMapping => new()
        {
            {AdrStatus.Unknown, Resources.AdrPlus.Unknown},
            {AdrStatus.Proposed, StatusNew},
            {AdrStatus.Accepted, StatusAcc},
            {AdrStatus.Rejected, StatusRej},
            {AdrStatus.Superseded, StatusSup},
        };

        /// <summary>
        /// Gets the semicolon-separated <see cref="Scopes"/> value as a trimmed, non-empty string array.
        /// </summary>
        /// <returns>An array of scope name strings, or an empty array when <see cref="Scopes"/> is null or whitespace.</returns>
        public string[] GetScopes()
        {
            if (string.IsNullOrWhiteSpace(Scopes))
                return [];

            return Scopes.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        }

        /// <summary>
        /// Gets the semicolon-separated <see cref="SkipDomain"/> value as a trimmed, non-empty string array.
        /// These are scopes for which the domain segment is omitted from the filename.
        /// </summary>
        /// <returns>An array of scope name strings to skip domain for, or an empty array when <see cref="SkipDomain"/> is null or whitespace.</returns>
        public string[] Getskipdomains()
        {
            if (string.IsNullOrWhiteSpace(SkipDomain))
                return [];

            return SkipDomain.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        }
    }
}
