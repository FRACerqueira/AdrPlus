// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the AdrPlus project under MIT license
// ***************************************************************************************

using AdrPlus.Core;
using AdrPlus.Infrastructure.FileSystem;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace AdrPlus.Plugins
{
    /// <summary>
    /// Patches a repository's <c>adr-config.adrplus</c> <c>activeplugins</c> key in place — shared by
    /// <c>InitCommandHandler</c> (writes the initial baseline) and <c>PluginsCommandHandler</c>'s manage-mode
    /// wizard (rewrites it after a <c>MultiSelect</c> confirmation).
    /// </summary>
    /// <remarks>
    /// Patches via <see cref="JsonNode"/> rather than re-serializing a deserialized <c>AdrPlusRepoConfig</c>
    /// object — the object's <c>Template</c> property may not reflect the file's real content depending on how
    /// it was constructed, and round-tripping it back to JSON would silently overwrite the repo's real ADR
    /// template with whatever that object happens to hold.
    /// </remarks>
    internal static class ActivePluginsWriter
    {
        /// <summary>
        /// Reads <paramref name="configFilePath"/>, sets its <c>activeplugins</c> key to
        /// <paramref name="activePluginNames"/>, and writes the result back to the same file.
        /// </summary>
        public static async Task WriteAsync(IFileSystemService fileSystem, string configFilePath, IEnumerable<string> activePluginNames, CancellationToken cancellationToken)
        {
            var json = await fileSystem.ReadAllTextAsync(configFilePath, cancellationToken);
            var node = JsonNode.Parse(json)!.AsObject();
            node[AppConstants.FieldActivePlugins] = JsonSerializer.SerializeToNode(activePluginNames.ToArray());
            await fileSystem.WriteAllTextAsync(configFilePath, node.ToJsonString(AppConstants.RepoSerializerOptions), cancellationToken);
        }
    }
}
