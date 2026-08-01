// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the AdrPlus project under MIT license
// ***************************************************************************************

using AdrPlus.Infrastructure.FileSystem;
using System.Text.Json;

namespace AdrPlus.Plugins
{
    /// <summary>
    /// Reads and writes a plugin's <c>state/pending.json</c> (spec §7). The file is a JSON array — a
    /// plugin can have pending entries for more than one ADR at once.
    /// </summary>
    internal static class PendingStateStore
    {
        private const string StateFolderName = "state";
        private const string PendingFileName = "pending.json";

        /// <summary>
        /// Reads all pending entries for <paramref name="pluginFolderPath"/>, or an empty list when
        /// <c>state/pending.json</c> doesn't exist yet.
        /// </summary>
        public static async Task<List<PendingEntry>> ReadAllAsync(IFileSystemService fileSystem, string pluginFolderPath, CancellationToken cancellationToken)
        {
            var pendingPath = Path.Combine(pluginFolderPath, StateFolderName, PendingFileName);

            if (!fileSystem.FileExists(pendingPath))
            {
                return [];
            }

            var json = await fileSystem.ReadAllTextAsync(pendingPath, cancellationToken);
            return JsonSerializer.Deserialize<List<PendingEntry>>(json, PluginManifest.SerializerOptions) ?? [];
        }

        /// <summary>
        /// Replaces the entire <c>state/pending.json</c> for <paramref name="pluginFolderPath"/> with
        /// <paramref name="entries"/>.
        /// </summary>
        public static async Task WriteAllAsync(IFileSystemService fileSystem, string pluginFolderPath, List<PendingEntry> entries, CancellationToken cancellationToken)
        {
            var stateFolder = Path.Combine(pluginFolderPath, StateFolderName);
            var pendingPath = Path.Combine(stateFolder, PendingFileName);

            fileSystem.CreateDirectory(stateFolder);
            var json = JsonSerializer.Serialize(entries, PluginManifest.SerializerOptions);
            await fileSystem.WriteAllTextAsync(pendingPath, json, cancellationToken);
        }

        /// <summary>
        /// Adds or replaces the entry matching <paramref name="entry"/>'s <see cref="PendingEntry.AdrKey"/> and
        /// <see cref="PendingEntry.EventType"/> in <paramref name="pluginFolderPath"/>'s <c>state/pending.json</c>.
        /// </summary>
        public static async Task UpsertAsync(IFileSystemService fileSystem, string pluginFolderPath, PendingEntry entry, CancellationToken cancellationToken)
        {
            var entries = await ReadAllAsync(fileSystem, pluginFolderPath, cancellationToken);

            entries.RemoveAll(existing =>
                string.Equals(existing.AdrKey, entry.AdrKey, StringComparison.Ordinal) &&
                string.Equals(existing.EventType, entry.EventType, StringComparison.Ordinal));
            entries.Add(entry);

            await WriteAllAsync(fileSystem, pluginFolderPath, entries, cancellationToken);
        }
    }
}
