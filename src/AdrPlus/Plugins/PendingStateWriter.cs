// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the AdrPlus project under MIT license
// ***************************************************************************************

using AdrPlus.Infrastructure.FileSystem;
using System.Text.Json;

namespace AdrPlus.Plugins
{
    /// <summary>
    /// Writes <see cref="PendingEntry"/> records to a plugin's <c>state/pending.json</c> (spec §7). The file is a
    /// JSON array — a plugin can have pending entries for more than one ADR at once — so writing upserts by
    /// <c>adrKey+eventType</c> rather than overwriting the whole file.
    /// </summary>
    internal static class PendingStateWriter
    {
        private const string StateFolderName = "state";
        private const string PendingFileName = "pending.json";

        /// <summary>
        /// Adds or replaces the entry matching <paramref name="entry"/>'s <see cref="PendingEntry.AdrKey"/> and
        /// <see cref="PendingEntry.EventType"/> in <paramref name="pluginFolderPath"/>'s <c>state/pending.json</c>.
        /// </summary>
        public static async Task UpsertAsync(IFileSystemService fileSystem, string pluginFolderPath, PendingEntry entry, CancellationToken cancellationToken)
        {
            var stateFolder = Path.Combine(pluginFolderPath, StateFolderName);
            var pendingPath = Path.Combine(stateFolder, PendingFileName);

            List<PendingEntry> entries = [];
            if (fileSystem.FileExists(pendingPath))
            {
                var json = await fileSystem.ReadAllTextAsync(pendingPath, cancellationToken);
                entries = JsonSerializer.Deserialize<List<PendingEntry>>(json, PluginManifest.SerializerOptions) ?? [];
            }

            entries.RemoveAll(existing =>
                string.Equals(existing.AdrKey, entry.AdrKey, StringComparison.Ordinal) &&
                string.Equals(existing.EventType, entry.EventType, StringComparison.Ordinal));
            entries.Add(entry);

            fileSystem.CreateDirectory(stateFolder);
            var updatedJson = JsonSerializer.Serialize(entries, PluginManifest.SerializerOptions);
            await fileSystem.WriteAllTextAsync(pendingPath, updatedJson, cancellationToken);
        }
    }
}
