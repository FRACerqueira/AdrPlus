// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the AdrPlus project under MIT license
// ***************************************************************************************

using AdrPlus.Infrastructure.FileSystem;
using System.Text.Json;

namespace AdrPlus.Plugins
{
    /// <summary>
    /// Reads and writes a plugin's <c>pending.json</c>. The file is a JSON array — a
    /// plugin can have pending entries for more than one ADR at once. Callers pass a repo-scoped,
    /// per-plugin state folder (e.g. <c>&lt;repo&gt;/plugins-state/&lt;name&gt;</c>) — not the plugin's own
    /// (now host-global, shared-across-repos) folder, since pending state must never be shared between repos.
    /// </summary>
    internal static class PendingStateStore
    {
        private const string PendingFileName = "pending.json";

        /// <summary>
        /// Reads all pending entries for <paramref name="pluginStateFolderPath"/>, or an empty list when
        /// <c>pending.json</c> doesn't exist yet, or when it exists but isn't valid JSON (e.g. truncated by a
        /// process kill mid-write) — a corrupted file is reported via <paramref name="onWarning"/> and treated
        /// as "no recoverable pending entries" rather than propagating a parse exception to the caller. The file
        /// itself is left untouched; a caller that goes on to call <see cref="WriteAllAsync"/> for the same path
        /// naturally replaces it with valid content on its next successful write.
        /// </summary>
        public static async Task<List<PendingEntry>> ReadAllAsync(IFileSystemService fileSystem, string pluginStateFolderPath, CancellationToken cancellationToken, Action<string>? onWarning = null)
        {
            var pendingPath = Path.Combine(pluginStateFolderPath, PendingFileName);

            if (!fileSystem.FileExists(pendingPath))
            {
                return [];
            }

            var json = await fileSystem.ReadAllTextAsync(pendingPath, cancellationToken);
            try
            {
                return JsonSerializer.Deserialize<List<PendingEntry>>(json, PluginManifest.SerializerOptions) ?? [];
            }
            catch (JsonException ex)
            {
                onWarning?.Invoke($"'{pendingPath}' is not valid JSON ({ex.Message}) - treating as no pending entries for this run.");
                return [];
            }
        }

        /// <summary>
        /// Replaces the entire <c>pending.json</c> for <paramref name="pluginStateFolderPath"/> with
        /// <paramref name="entries"/>. Writes to a temporary file first and then renames it into place
        /// (<see cref="IFileSystemService.MoveFile"/>, itself an atomic <c>File.Move</c> on the same volume) so a
        /// process killed mid-write leaves the previous, still-valid <c>pending.json</c> in place instead of a
        /// truncated one.
        /// </summary>
        public static async Task WriteAllAsync(IFileSystemService fileSystem, string pluginStateFolderPath, List<PendingEntry> entries, CancellationToken cancellationToken)
        {
            var pendingPath = Path.Combine(pluginStateFolderPath, PendingFileName);
            var tempPath = pendingPath + ".tmp";

            fileSystem.CreateDirectory(pluginStateFolderPath);
            var json = JsonSerializer.Serialize(entries, PluginManifest.SerializerOptions);
            await fileSystem.WriteAllTextAsync(tempPath, json, cancellationToken);
            fileSystem.MoveFile(tempPath, pendingPath);
        }

        /// <summary>
        /// Adds or replaces the entry matching <paramref name="entry"/>'s <see cref="PendingEntry.AdrKey"/> and
        /// <see cref="PendingEntry.EventType"/> in <paramref name="pluginStateFolderPath"/>'s <c>pending.json</c>.
        /// </summary>
        public static async Task UpsertAsync(IFileSystemService fileSystem, string pluginStateFolderPath, PendingEntry entry, CancellationToken cancellationToken, Action<string>? onWarning = null)
        {
            var entries = await ReadAllAsync(fileSystem, pluginStateFolderPath, cancellationToken, onWarning);

            entries.RemoveAll(existing =>
                string.Equals(existing.AdrKey, entry.AdrKey, StringComparison.Ordinal) &&
                string.Equals(existing.EventType, entry.EventType, StringComparison.Ordinal));
            entries.Add(entry);

            await WriteAllAsync(fileSystem, pluginStateFolderPath, entries, cancellationToken);
        }
    }
}
