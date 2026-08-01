// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the AdrPlus project under MIT license
// ***************************************************************************************

namespace AdrPlus.Plugins
{
    /// <summary>
    /// Orchestrates discovery and structural load validation of plugins under <c>./plugins/&lt;name&gt;/</c>
    /// (spec §4.2). Dispatch, <c>InitializeAsync</c>, retry and shutdown are out of scope — see Fase 4.
    /// </summary>
    internal interface IPluginManager
    {
        /// <summary>
        /// Plugins that passed structural load validation, in discovery order (folders sharing a duplicate name
        /// with another candidate are excluded — D22).
        /// </summary>
        IReadOnlyList<LoadedPlugin> LoadedPlugins { get; }

        /// <summary>
        /// Candidate plugin subfolders that failed structural load validation. Manifest-level rejections
        /// (invalid manifest, path traversal, not in allowlist) appear in discovery order first, followed by
        /// duplicate-name rejections grouped by name — duplicates can only be detected once every candidate's
        /// manifest has been read, so they cannot be interleaved with the first pass.
        /// </summary>
        IReadOnlyList<PluginRejection> Rejections { get; }

        /// <summary>
        /// Discovers and validates every immediate subfolder of <paramref name="pluginsRootPath"/>, populating
        /// <see cref="LoadedPlugins"/> and <see cref="Rejections"/>. A missing <paramref name="pluginsRootPath"/>
        /// is a no-op (empty repo without a <c>./plugins</c> folder is not an error).
        /// </summary>
        /// <param name="pluginsRootPath">The full path to the repository's <c>./plugins</c> folder.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        Task LoadPluginsAsync(string pluginsRootPath, CancellationToken cancellationToken = default);
    }
}
