// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the AdrPlus project under MIT license
// ***************************************************************************************

using AdrPlus.Abstractions;
using System.Text;

namespace AdrPlus.Plugins.AdrIndexer
{
    /// <summary>
    /// Reference/example <see cref="IAdrPlugin"/> (Phase 11): regenerates a repo-wide ADR index file
    /// (<c>settings.outputFileName</c>, default <c>indexadrs.md</c>) from a bundled template on every ADR event.
    /// <c>settings.outputFolder</c> (optional, relative to the ADR root; absent/empty keeps today's default of
    /// writing directly into the ADR root) lets the index land somewhere else, e.g. a docs site's input folder.
    /// </summary>
    /// <remarks>
    /// A host dispatch only carries one ADR (<see cref="AdrEventContext.Adr"/>), so this plugin re-scans the
    /// whole ADR folder itself and parses each file's own fixed-position metadata header (the HTML-comment-
    /// delimited table every AdrPlus template renders) instead of relying on any host-side listing API.
    /// </remarks>
    public sealed class AdrIndexerPlugin : AdrPluginBase
    {
        private const string DefaultOutputFileName = "indexadrs.md";
        private const string DefaultTemplateFile = "indexadrs-template.md";
        private const string ContentTag = "[content]";

        private string _outputFileName = DefaultOutputFileName;
        private string _templateFile = DefaultTemplateFile;
        private string _outputFolder = string.Empty;

        /// <inheritdoc />
        public override string Name => "AdrIndexer";

        /// <inheritdoc />
        public override string Version => "1.0.0";

        /// <inheritdoc />
        public override Task InitializeAsync(IPluginContext context, IPluginConfiguration config, CancellationToken ct)
        {
            _outputFileName = config.GetValue<string>("outputFileName") is { Length: > 0 } outputFileName ? outputFileName : DefaultOutputFileName;
            _templateFile = config.GetValue<string>("templateFile") is { Length: > 0 } templateFile ? templateFile : DefaultTemplateFile;
            _outputFolder = config.GetValue<string>("outputFolder") ?? string.Empty;
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        protected override async Task<PluginResult> HandleAsync(AdrEventContext context, CancellationToken ct)
        {
            var templatePath = Path.Combine(GetPluginFolder(), _templateFile);
            if (!File.Exists(templatePath))
            {
                return Fail($"Template file not found: {templatePath}", isRetryable: false);
            }

            try
            {
                var adrRoot = ResolveAdrRoot(context.AdrFilePath, context.Repo.FolderAdr);
                var rows = ScanAdrRows(adrRoot);
                var table = BuildTable(rows);
                var template = await File.ReadAllTextAsync(templatePath, ct).ConfigureAwait(false);
                var content = template.Replace(ContentTag, table, StringComparison.Ordinal);

                var outputDir = Path.GetFullPath(Path.Combine(adrRoot, _outputFolder));
                Directory.CreateDirectory(outputDir);
                var outputPath = Path.Combine(outputDir, _outputFileName);
                await File.WriteAllTextAsync(outputPath, content, ct).ConfigureAwait(false);

                return Success();
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                return Fail(ex.Message);
            }
        }

        /// <summary>
        /// Walks up from the current ADR's own folder to the ADR root (<c>Repo.FolderAdr</c>), so the scan
        /// covers every scope subfolder uniformly regardless of whether the repo groups ADRs by scope.
        /// </summary>
        private static string ResolveAdrRoot(string adrFilePath, string folderAdr)
        {
            var suffix = folderAdr.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar).TrimEnd(Path.DirectorySeparatorChar);
            var dir = new DirectoryInfo(Path.GetDirectoryName(adrFilePath)!);
            while (dir != null)
            {
                if (dir.FullName.TrimEnd(Path.DirectorySeparatorChar).EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                {
                    return dir.FullName;
                }
                dir = dir.Parent;
            }

            return Path.GetDirectoryName(adrFilePath)!;
        }

        private static string GetPluginFolder() =>
            Path.GetDirectoryName(typeof(AdrIndexerPlugin).Assembly.Location)!;

        private List<(string Link, string Title, string VersionLabel, string Status)> ScanAdrRows(string adrRoot)
        {
            var entries = new List<(string Id, string RelativePath, string Title, string VersionLabel, string Status)>();

            foreach (var file in Directory.EnumerateFiles(adrRoot, "*.md", SearchOption.AllDirectories))
            {
                if (string.Equals(Path.GetFileName(file), _outputFileName, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (!TryReadHeaderRows(file, out var rows) || string.IsNullOrWhiteSpace(RowValue(rows, 0)))
                {
                    continue;
                }

                var title = RowValue(rows, 0);
                var version = RowValue(rows, 1);
                var revision = RowValue(rows, 2);
                var created = RowValue(rows, 5);
                var changed = RowValue(rows, 6);
                var status = string.IsNullOrWhiteSpace(changed) ? created : changed;
                var versionLabel = string.IsNullOrWhiteSpace(revision) ? $"V{version}" : $"V{version} (R{revision})";
                var relativePath = Path.GetRelativePath(adrRoot, file).Replace('\\', '/');

                entries.Add((Path.GetFileNameWithoutExtension(file), relativePath, title, versionLabel, string.IsNullOrWhiteSpace(status) ? "Unknown" : status));
            }

            return entries
                .OrderBy(e => e.Id, StringComparer.Ordinal)
                .Select(e => ($"[{e.Id}]({e.RelativePath})", e.Title, e.VersionLabel, e.Status))
                .ToList();
        }

        private static string BuildTable(List<(string Link, string Title, string VersionLabel, string Status)> rows)
        {
            var sb = new StringBuilder();
            sb.AppendLine("| ADR | Title | Version | Status |");
            sb.AppendLine("| --- | --- | --- | --- |");
            foreach (var row in rows)
            {
                sb.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"| {row.Link} | {row.Title} | {row.VersionLabel} | {row.Status} |");
            }

            return sb.ToString().TrimEnd('\r', '\n');
        }

        /// <summary>
        /// Parses the fixed-position ADR metadata header every AdrPlus template renders: an HTML comment, a
        /// two-column table (label header row, separator row, then title/version/revision/scope/domain/created/
        /// changed/superseded in that fixed order), and a closing HTML comment. Row labels are configurable per
        /// repo (<c>adr-config.adrplus</c>); row order is not, so parsing is purely positional.
        /// </summary>
        private static bool TryReadHeaderRows(string filePath, out List<string> dataRows)
        {
            dataRows = [];
            var lines = File.ReadAllLines(filePath);
            if (lines.Length == 0 || !lines[0].TrimStart().StartsWith("<!--", StringComparison.Ordinal))
            {
                return false;
            }

            var pipeRows = new List<string>();
            var closed = false;
            for (var i = 1; i < lines.Length; i++)
            {
                var trimmed = lines[i].Trim();
                if (trimmed.StartsWith("<!--", StringComparison.Ordinal))
                {
                    closed = true;
                    break;
                }

                if (trimmed.StartsWith('|'))
                {
                    pipeRows.Add(trimmed);
                }
            }

            if (!closed || pipeRows.Count < 3)
            {
                return false;
            }

            dataRows = [.. pipeRows.Skip(2).Select(ExtractCellValue)];
            return true;
        }

        private static string ExtractCellValue(string row)
        {
            var parts = row.Split('|');
            return parts.Length >= 3 ? parts[2].Trim() : string.Empty;
        }

        private static string RowValue(List<string> rows, int index) => index < rows.Count ? rows[index] : string.Empty;
    }
}
