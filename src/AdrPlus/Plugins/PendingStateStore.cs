// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the AdrPlus project under MIT license
// ***************************************************************************************

using AdrPlus.Infrastructure.FileSystem;
using System.Text.Json;

namespace AdrPlus.Plugins
{
    /// <summary>
    /// Reads and writes a plugin's <c>pending.json</c> (spec §7, D36). The file is a JSON array — a
    /// plugin can have pending entries for more than one ADR at once. Callers pass a repo-scoped,
    /// per-plugin state folder (e.g. <c>&lt;repo&gt;/plugins-state/&lt;name&gt;</c>) — not the plugin's own
    /// (now host-global, shared-across-repos) folder, since pending state must never be shared between repos.
    /// </summary>
    internal static class PendingStateStore
    {
        private const string PendingFileName = "pending.json";

        /// <summary>
        /// Reads all pending entries for <paramref name="pluginStateFolderPath"/>, or an empty list when
        /// <c>pending.json</c> doesn't exist yet.
        /// </summary>
        public static async Task<List<PendingEntry>> ReadAllAsync(IFileSystemService fileSystem, string pluginStateFolderPath, CancellationToken cancellationToken)
        {
            var pendingPath = Path.Combine(pluginStateFolderPath, PendingFileName);

            if (!fileSystem.FileExists(pendingPath))
            {
                return [];
            }

            var json = await fileSystem.ReadAllTextAsync(pendingPath, cancellationToken);
            return JsonSerializer.Deserialize<List<PendingEntry>>(json, PluginManifest.SerializerOptions) ?? [];
        }

        /// <summary>
        /// Replaces the entire <c>pending.json</c> for <paramref name="pluginStateFolderPath"/> with
        /// <paramref name="entries"/>.
        /// </summary>
        public static async Task WriteAllAsync(IFileSystemService fileSystem, string pluginStateFolderPath, List<PendingEntry> entries, CancellationToken cancellationToken)
        {
            var pendingPath = Path.Combine(pluginStateFolderPath, PendingFileName);

            fileSystem.CreateDirectory(pluginStateFolderPath);
            var json = JsonSerializer.Serialize(entries, PluginManifest.SerializerOptions);
            await fileSystem.WriteAllTextAsync(pendingPath, json, cancellationToken);
        }

        /// <summary>
        /// Adds or replaces the entry matching <paramref name="entry"/>'s <see cref="PendingEntry.AdrKey"/> and
        /// <see cref="PendingEntry.EventType"/> in <paramref name="pluginStateFolderPath"/>'s <c>pending.json</c>.
        /// </summary>
        public static async Task UpsertAsync(IFileSystemService fileSystem, string pluginStateFolderPath, PendingEntry entry, CancellationToken cancellationToken)
        {
            var entries = await ReadAllAsync(fileSystem, pluginStateFolderPath, cancellationToken);

            entries.RemoveAll(existing =>
                string.Equals(existing.AdrKey, entry.AdrKey, StringComparison.Ordinal) &&
                string.Equals(existing.EventType, entry.EventType, StringComparison.Ordinal));
            entries.Add(entry);

            await WriteAllAsync(fileSystem, pluginStateFolderPath, entries, cancellationToken);
        }
    }
}
