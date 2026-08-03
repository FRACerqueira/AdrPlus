// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the AdrPlus project under MIT license
// ***************************************************************************************

using AdrPlus.Domain;

namespace AdrPlus.Plugins
{
    /// <summary>
    /// Resolves, once per command, which of a repository's currently loaded plugins should actually receive
    /// dispatch this run — the per-repo <see cref="AdrPlusRepoConfig.ActivePlugins"/> baseline and
    /// <see cref="AdrPlusRepoConfig.DisablePlugins"/> kill switch.
    /// </summary>
    /// <remarks>
    /// A loaded plugin is one of: <c>Active</c> (loaded and listed in <see cref="AdrPlusRepoConfig.ActivePlugins"/>
    /// — dispatched to), <c>Inactive</c> (loaded but deliberately not listed, e.g. unchecked via
    /// <c>adrplus plugins --wizard</c> — silently skipped, no warning), or, when <see cref="AdrPlusRepoConfig.DisablePlugins"/>
    /// is set, every plugin is off regardless of the list. A name listed in <see cref="AdrPlusRepoConfig.ActivePlugins"/>
    /// with no matching loaded plugin is <c>Missing</c> — the one case that warns, since it's the only state that
    /// wasn't deliberately chosen.
    /// </remarks>
    internal static class PluginActivationGate
    {
        /// <summary>
        /// Computes the active-plugin filter for this call, alongside the display data for
        /// <c>IConsoleWriter.PromptShowActivePlugins</c>. Deliberately does not print anything itself — callers
        /// should call <c>Resolve</c> as early as the repository config is available (right after
        /// <c>IPluginManager.LoadPluginsAsync</c>), to compute <c>IsActive</c> for the dispatch-family calls, but
        /// defer the actual <c>PromptShowActivePlugins</c> call to right before their own result message. Printing
        /// any earlier can land on a cursor position a wizard flow has already repositioned (e.g. via
        /// <c>IConsoleWriter.PromptMovePosition</c> after a confirm step), making the output invisible even
        /// though it was technically written.
        /// </summary>
        /// <param name="pluginManager">The plugin manager whose <c>LoadedPlugins</c> reflects this run's discovery.</param>
        /// <param name="repoconfig">The repository configuration providing <see cref="AdrPlusRepoConfig.ActivePlugins"/>/<see cref="AdrPlusRepoConfig.DisablePlugins"/>.</param>
        /// <returns>
        /// <c>IsActive</c>: the predicate to pass as the dispatch-family methods' <c>isActive</c> parameter.
        /// <c>ActiveSummary</c>/<c>MissingNames</c>: pass directly into <c>IConsoleWriter.PromptShowActivePlugins</c>.
        /// </returns>
        public static (Func<LoadedPlugin, bool> IsActive, IReadOnlyList<string> ActiveSummary, IReadOnlyList<string> MissingNames) Resolve(
            IPluginManager pluginManager, AdrPlusRepoConfig repoconfig)
        {
            if (repoconfig.DisablePlugins)
            {
                return (_ => false, [], []);
            }

            var active = new HashSet<string>(repoconfig.ActivePlugins, StringComparer.OrdinalIgnoreCase);
            var loadedNames = pluginManager.LoadedPlugins.Select(p => p.Manifest.Name!).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var missing = active.Except(loadedNames).ToList();

            var activeSummary = pluginManager.LoadedPlugins
                .Where(plugin => active.Contains(plugin.Manifest.Name!))
                .Select(plugin => $"{plugin.Manifest.Name} v{plugin.Manifest.Version}")
                .ToList();

            return (plugin => active.Contains(plugin.Manifest.Name!), activeSummary, missing);
        }
    }
}
