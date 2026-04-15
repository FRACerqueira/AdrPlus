// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the AdrPlus project under MIT license
// ***************************************************************************************

using AdrPlus.Core;
using System.Text;

namespace AdrPlus.Domain
{
    /// <summary>
    /// Represents an Architecture Decision Record (ADR).
    /// </summary>
    internal sealed record AdrRecord()
    {
        /// <summary>
        /// Gets or sets the initial status when the ADR was created.
        /// </summary>
        public AdrStatus StatusCreate { get; set; } = AdrStatus.Proposed;

        /// <summary>
        /// Gets or sets the status after an update operation.
        /// </summary>
        public AdrStatus StatusUpdate { get; set; } = AdrStatus.Unknown;

        /// <summary>
        /// Gets or sets the status after a change operation.
        /// </summary>
        public AdrStatus StatusChange { get; set; } = AdrStatus.Unknown;

        /// <summary>
        /// Gets or sets the date reference when the ADR was created.
        /// </summary>
        public DateTime? CreateRef { get; set; }

        /// <summary>
        /// Gets or sets the date reference when the ADR was updated.
        /// </summary>
        public DateTime? UpdateRef { get; set; }

        /// <summary>
        /// Gets or sets the date reference when the ADR status was changed.
        /// </summary>
        public DateTime? ChangeRef { get; set; }

        /// <summary>
        /// Gets or sets the sequence number of the ADR that this one supersedes.
        /// </summary>
        public int? Superseded { get; set; }

        /// <summary>
        /// Gets or sets the sequence number of the ADR.
        /// </summary>
        public int Number { get; set; }

        /// <summary>
        /// Gets or sets the version number of the ADR.
        /// </summary>
        public int Version { get; set; }

        /// <summary>
        /// Gets or sets the revision number of the ADR.
        /// </summary>
        public int? Revision { get; set; }

        /// <summary>
        /// Gets or sets the title of the ADR.
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the domain of the ADR.
        /// </summary>
        public string Domain { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the scope of the ADR.
        /// </summary>
        public string Scope { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the template content of the ADR.
        /// </summary>
        public string Template { get; set; } = string.Empty;

        /// <summary>
        /// Generates the ADR filename following the naming convention:
        /// <c>&lt;prefix&gt;&lt;number&gt;&lt;sep&gt;&lt;title&gt;&lt;sep&gt;V&lt;version&gt;[R&lt;revision&gt;][&lt;sep&gt;&lt;scope&gt;[&lt;sep&gt;&lt;domain&gt;]][&lt;sep&gt;SUP&lt;superseded&gt;].md</c>.
        /// </summary>
        /// <param name="config">The ADR Plus configuration containing naming conventions (prefix, separators, lengths, and case transform).</param>
        /// <returns>A formatted filename string following the ADR naming convention (e.g. <c>ADR-0001-MyTitle-V01R00-ENT-MyDomain-SUP0002.md</c>).</returns>
        public string GetFileName(AdrPlusRepoConfig config)
        {
            var baseFileName = $"{config.Prefix ?? string.Empty}{Number.ToString($"D{config.LenSeq}", null)}{config.Separator}{Title.ToCase(config.CaseTransform)}";
            var ver = $"{config.Separator}V{Version.ToString($"D{config.LenVersion}", null)}";
            var rev = config.LenRevision > 0 ? $"R{Revision!.Value.ToString($"D{config.LenRevision}", null)}" : string.Empty;
            var scopeSuffix = string.Empty;
            if (!string.IsNullOrWhiteSpace(Scope) && config.LenScope > 0)
            {
                scopeSuffix = $"{config.Separator}{Scope[..config.LenScope]}";
                if (Domain.Length > 0)
                {
                    scopeSuffix += $"{config.Separator}{Domain.ToCase(config.CaseTransform)}";
                }
            }
            var supersede = (Superseded ?? 0) > 0 ? $"{config.Separator}SUP{Superseded!.Value.ToString($"D{config.LenSeq}", null)}" : string.Empty;
            var fileName = $"{baseFileName}{ver}{rev}{scopeSuffix}{supersede}.md";
            return fileName;
        }

        /// <summary>
        /// Generates the ADR header section (first 10 lines) based on the configuration and record properties.
        /// The header includes disclaimer, version, revision, scope/domain, status lines, title, and a separator.
        /// </summary>
        /// <param name="config">The ADR Plus configuration containing header format, date format, and status string mappings.</param>
        /// <param name="supersedefile">When <see cref="StatusChange"/> is <see cref="AdrStatus.Superseded"/>, the filename of the superseding ADR to append after the status date. Defaults to <see langword="null"/>.</param>
        /// <returns>A formatted multi-line header string terminated with <c>"---"</c> and a newline.</returns>
        public string GetHeader(AdrPlusRepoConfig config, string? supersedefile = null)
        {
            var result = new StringBuilder();
            result.AppendLine(null, $"###### {config.HeaderDisclaimer}");
            result.AppendLine(null, $"##### {config.HeaderVersion}: {Version.ToString($"D{config.LenVersion}", null)}");
            if (Revision.HasValue)
            {
                result.AppendLine(null, $"##### {config.HeaderRevision}: {Revision.Value.ToString($"D{config.LenRevision}", null)}");
            }
            else
            {
                result.AppendLine(null, $"##### {config.HeaderRevision}: -");
            }
            if (Scope.Length > 0)
            {
                if (Domain.Length > 0)
                {
                    result.AppendLine(null, $"##### {Scope} : {Domain}");
                }
                else
                {
                    result.AppendLine(null, $"##### {Scope}");
                }
            }
            else
            {
                result.AppendLine(null, $"##### -");
            }
            result.AppendLine(null, $"##### {config.HeaderStatus}");

            var textstatus = StatusCreate switch
            {
                AdrStatus.Unknown => string.Empty,
                AdrStatus.Proposed => config.StatusNew,
                AdrStatus.Accepted => config.StatusAcc,
                AdrStatus.Rejected => config.StatusRej,
                AdrStatus.Superseded => config.StatusSup,
                _ => StatusCreate.ToString()
            };
            if (string.IsNullOrEmpty(textstatus))
            {
                result.AppendLine(null, $"- \\-");
            }
            else
            {
                if (CreateRef.HasValue)
                {
                    result.AppendLine(null, $"- {textstatus} ({CreateRef!.Value.ToString("yyyy-MM-dd", null)})");
                }
                else
                {
                    result.AppendLine(null, $"- {textstatus} ({DateTime.UtcNow.ToString("yyyy-MM-dd", null)})");
                }
            }
            textstatus = StatusUpdate switch
            {
                AdrStatus.Unknown => string.Empty,
                AdrStatus.Proposed => config.StatusNew,
                AdrStatus.Accepted => config.StatusAcc,
                AdrStatus.Rejected => config.StatusRej,
                AdrStatus.Superseded => config.StatusSup,
                _ => StatusUpdate.ToString()
            };
            if (string.IsNullOrEmpty(textstatus))
            {
                result.AppendLine(null, $"- \\-");
            }
            else
            {
                if (UpdateRef.HasValue)
                {
                    result.AppendLine(null, $"- {textstatus} ({UpdateRef!.Value.ToString("yyyy-MM-dd", null)})");
                }
                else
                {
                    result.AppendLine(null, $"- {textstatus} ({DateTime.UtcNow.ToString("yyyy-MM-dd", null)})");
                }
            }
            textstatus = StatusChange switch
            {
                AdrStatus.Unknown => string.Empty,
                AdrStatus.Proposed => config.StatusNew,
                AdrStatus.Accepted => config.StatusAcc,
                AdrStatus.Rejected => config.StatusRej,
                AdrStatus.Superseded => config.StatusSup,
                _ => StatusChange.ToString()
            };
            if (StatusChange != AdrStatus.Superseded)
            { 
                supersedefile = null;
            }
            if (string.IsNullOrEmpty(textstatus))
            {
                result.AppendLine(null, $"- \\-");
            }
            else
            {
                if (string.IsNullOrEmpty(supersedefile))
                {
                    if (ChangeRef.HasValue)
                    {
                        result.AppendLine(null, $"- {textstatus} ({ChangeRef!.Value.ToString("yyyy-MM-dd", null)})");
                    }
                    else
                    {
                        result.AppendLine(null, $"- {textstatus} ({DateTime.UtcNow.ToString("yyyy-MM-dd", null)})");
                    }
                }
                else
                {
                    if (ChangeRef.HasValue)
                    {
                        result.AppendLine(null, $"- {textstatus} ({ChangeRef!.Value.ToString("yyyy-MM-dd", null)}) : {supersedefile}");
                    }
                    else
                    {
                        result.AppendLine(null, $"- {textstatus} ({DateTime.UtcNow.ToString("yyyy-MM-dd", null)}) : {supersedefile}");
                    }
                }
            }
            result.AppendLine(null, $"# {Title}");
            result.AppendLine("---");
            return result.ToString();
        }
    }
}
