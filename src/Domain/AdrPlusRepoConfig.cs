// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the AdrPlus project under MIT license
// ***************************************************************************************

using System.Text.Json.Serialization;

namespace AdrPlus.Domain
{
    /// <summary>
    /// Represents the repository configuration for the ADR Plus system, including folder paths, naming conventions, and header templates.
    /// </summary>
    /// <remarks>
    /// Initialized with the folder path for ADRs and the default Markdown template. All other settings use sensible defaults
    /// that can be customized via property setters.
    /// </remarks>
    /// <param name="folderadr">The folder path where ADR files are stored, e.g., "docs/adr".</param>
    /// <param name="template">The Markdown template content used for generating new ADRs.</param>
    internal sealed class AdrPlusRepoConfig(string folderadr, string template)
    {

        /// <summary>
        /// Gets or sets the folder path where ADR files are stored, e.g., "docs/adr".
        /// </summary>
        public string FolderAdr { get; set; } = folderadr;

        /// <summary>
        /// Gets or sets the Markdown template content used for generating new ADRs.
        /// </summary>
        public string Template { get; set; } = template;

        /// <summary>
        /// Gets or sets the optional prefix for ADR filenames, e.g., "ADR" for "ADR-0001-My-ADR.md".
        /// </summary>
        public string? Prefix { get; set; } = Resources.AdrPlus.DefaultPrefix;

        /// <summary>
        /// Gets or sets the number of digits for the sequence number in the filename, e.g., 4 for "0001".
        /// Must be at least 3.
        /// </summary>
        public int LenSeq { get; set; } = 4;

        /// <summary>
        /// Gets or sets the number of digits for the version number in the filename, e.g., 2 for "v01".
        /// Set to 0 to omit version from the filename.
        /// </summary>
        public int LenVersion { get; set; } = 2;

        /// <summary>
        /// Gets or sets the number of digits for the revision number in the filename, e.g., 2 for "rev01".
        /// Set to 0 to omit revision from the filename.
        /// </summary>
        public int LenRevision { get; set; }

        /// <summary>
        /// Gets or sets the number of characters to use for the scope text in the filename, e.g., 1 for "E".
        /// Set to 0 to omit scope from the filename.
        /// </summary>
        public int LenScope { get; set; }

        /// <summary>
        /// Gets or sets the separator character used between different parts of the filename.
        /// Valid values are "-", "~", or ".".
        /// </summary>
        public char Separator { get; set; } = '-';

        /// <summary>
        /// Gets or sets the case transformation to apply to the title portion of the filename,
        /// e.g., <see cref="CaseFormat.CamelCase"/>, <see cref="CaseFormat.PascalCase"/>, <see cref="CaseFormat.SnakeCase"/>, or <see cref="CaseFormat.KebabCase"/>.
        /// </summary>
        public CaseFormat CaseTransform { get; set; } = CaseFormat.PascalCase;

        /// <summary>
        /// Gets or sets the status value for newly proposed ADRs, e.g., "Proposed".
        /// This is required when creating a new ADR.
        /// </summary>  
        public string StatusNew { get; set; } = Resources.AdrPlus.StatusNew;

        /// <summary>
        /// Gets or sets the status value for accepted ADRs, e.g., "Accepted".
        /// This status is valid only when the current ADR status is <see cref="StatusNew"/>.
        /// </summary>
        public string StatusAcc { get; set; } = Resources.AdrPlus.StatusAcc;

        /// <summary>
        /// Gets or sets the status value for rejected ADRs, e.g., "Rejected".
        /// This status is valid only when the current ADR status is <see cref="StatusNew"/>.
        /// </summary>
        public string StatusRej { get; set; } = Resources.AdrPlus.StatusRej;

        /// <summary>
        /// Gets or sets the status value for superseded ADRs, e.g., "Superseded".
        /// This status is valid only when the current ADR status is <see cref="StatusAcc"/>.
        /// </summary>
        public string StatusSup { get; set; } = Resources.AdrPlus.StatusSup;

        /// <summary>
        /// Gets or sets the semicolon-separated list of scopes for organizing ADRs, e.g., "Enterprise;Domain;Project".
        /// </summary>
        public string Scopes { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a value indicating whether to create subfolders for each scope, e.g., <c>true</c> or <c>false</c>.
        /// </summary>
        public bool FolderByScope { get; set; }

        /// <summary>
        /// Gets or sets the scope for which to skip the domain section in the filename, e.g., "Enterprise".
        /// Must be one of the scopes defined in <see cref="Scopes"/>. Optional when no domain skipping is needed.
        /// </summary>
        public string SkipDomain { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the disclaimer text that appears in the ADR header.
        /// </summary>
        public string HeaderDisclaimer { get; set; } = Resources.AdrPlus.DefaultHeaderDisclaimer;

        /// <summary>
        /// Gets or sets the template text for the file title displayed in the ADR header.
        /// </summary>
        public string HeaderTitleFile { get; set; } = Resources.AdrPlus.DefaultHeaderTitleFile;

        /// <summary>
        /// Gets or sets the template text for the version label displayed in the ADR header.
        /// </summary>
        public string HeaderVersion { get; set; } = Resources.AdrPlus.Version;

        /// <summary>
        /// Gets or sets the template text for the revision label displayed in the ADR header.
        /// </summary>
        public string HeaderRevision { get; set; } = Resources.AdrPlus.Revision;


        /// <summary>
        /// Gets or sets the template text for the scope label displayed in the ADR header.
        /// </summary>
        public string HeaderScope { get; set; } = Resources.AdrPlus.Scope;


        /// <summary>
        /// Gets or sets the template text for the domain label displayed in the ADR header.
        /// </summary>
        public string HeaderDomain { get; set; } = Resources.AdrPlus.Domain;

        /// <summary>
        /// Gets or sets the template text for the "Created" status label displayed in the ADR header.
        /// </summary>
        public string HeaderTitleStatusCreated { get; set; } = Resources.AdrPlus.Created;

        /// <summary>
        /// Gets or sets the template text for the "Changed" status label displayed in the ADR header.
        /// </summary>
        public string HeaderTitleStatusChanged { get; set; } = Resources.AdrPlus.Changed;


        /// <summary>
        /// Gets or sets the template text for the "Superseded" status label displayed in the ADR header.
        /// </summary>
        public string HeaderTitleStatusSuperseded { get; set; } = Resources.AdrPlus.StatusSup;

        /// <summary>
        /// Gets or sets the template text for the table header displaying field names in the ADR.
        /// </summary>
        public string HeaderTableFields { get; set; } = Resources.AdrPlus.Fields;

        /// <summary>
        /// Gets or sets the template text for the table header displaying field values in the ADR.
        /// </summary>
        public string HeaderTableValues { get; set; } = Resources.AdrPlus.Values;


        /// <summary>
        /// Gets or sets the template text for the "Migrated" label displayed in the ADR header.
        /// </summary>
        public string HeaderMigrated { get; set; } = Resources.AdrPlus.Migrated;

        /// <summary>
        /// Gets the mapping between ADR status enum values and their configured string representations.
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
        /// Gets the <see cref="Scopes"/> value as a trimmed, non-empty string array.
        /// </summary>
        /// <returns>An array of scope name strings; an empty array if <see cref="Scopes"/> is null or whitespace.</returns>
        public string[] GetScopes()
        {
            if (string.IsNullOrWhiteSpace(Scopes))
                return [];

            return Scopes.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        }

        /// <summary>
        /// Gets the <see cref="SkipDomain"/> value as a trimmed, non-empty string array.
        /// These are the scopes for which the domain segment is omitted from the filename.
        /// </summary>
        /// <returns>An array of scope name strings; an empty array if <see cref="SkipDomain"/> is null or whitespace.</returns>
        public string[] GetSkipDomains()
        {
            if (string.IsNullOrWhiteSpace(SkipDomain))
                return [];

            return SkipDomain.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        }
    }
}
