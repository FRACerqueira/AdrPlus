// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the AdrPlus project under MIT license
// ***************************************************************************************

using AdrPlus.Domain;
using AdrPlus.Infrastructure.Formatting;
using AdrPlus.Infrastructure.Logging;
using AdrPlus.Infrastructure.UI;
using Microsoft.Extensions.Logging;

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
    /// wasn't deliberately chosen. <c>Active</c>/<c>Inactive</c> status for every loaded plugin is available in
    /// detail via <c>adrplus plugins --list</c>; this gate only surfaces the actionable <c>Missing</c> case inline
    /// on the dispatching commands themselves, to avoid repeating that detail on every single command run.
    /// </remarks>
    internal static class PluginActivationGate
    {
        /// <summary>
        /// Computes the active-plugin filter for this call, alongside the <c>Missing</c> names for
        /// <c>IConsoleWriter.PromptWarnMissingActivePlugins</c>. Deliberately does not print anything itself —
        /// callers should call <c>Resolve</c> as early as the repository config is available (right after
        /// <c>IPluginManager.LoadPluginsAsync</c>), to compute <c>IsActive</c> for the dispatch-family calls, but
        /// defer the actual <c>PromptWarnMissingActivePlugins</c> call to right before their own result message.
        /// Printing any earlier can land on a cursor position a wizard flow has already repositioned (e.g. via
        /// <c>IConsoleWriter.PromptClearRegionFromTop</c> after a confirm step), making the output invisible even
        /// though it was technically written.
        /// </summary>
        /// <param name="pluginManager">The plugin manager whose <c>LoadedPlugins</c> reflects this run's discovery.</param>
        /// <param name="repoconfig">The repository configuration providing <see cref="AdrPlusRepoConfig.ActivePlugins"/>/<see cref="AdrPlusRepoConfig.DisablePlugins"/>.</param>
        /// <returns>
        /// <c>IsActive</c>: the predicate to pass as the dispatch-family methods' <c>isActive</c> parameter.
        /// <c>MissingNames</c>: pass directly into <c>IConsoleWriter.PromptWarnMissingActivePlugins</c>.
        /// </returns>
        public static (Func<LoadedPlugin, bool> IsActive, IReadOnlyList<string> MissingNames) Resolve(
            IPluginManager pluginManager, AdrPlusRepoConfig repoconfig)
        {
            if (repoconfig.DisablePlugins)
            {
                return (_ => false, []);
            }

            var active = new HashSet<string>(repoconfig.ActivePlugins, StringComparer.OrdinalIgnoreCase);
            var loadedNames = pluginManager.LoadedPlugins.Select(p => p.Manifest.Name!).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var missing = active.Except(loadedNames).ToList();

            return (plugin => active.Contains(plugin.Manifest.Name!), missing);
        }

        /// <summary>
        /// Warns about <paramref name="missingPluginNames"/> (the <c>Missing</c> case from <see cref="Resolve"/>)
        /// on both the console and the log file. <see cref="IConsoleWriter.PromptWarnMissingActivePlugins"/> alone
        /// only ever reached the console - a non-interactive/cron <c>adrplus sync</c> run had no record of a
        /// configured-active plugin failing to load, since this is the only channel that surfaces it at all.
        /// Callers should call this at the exact point they previously called
        /// <c>IConsoleWriter.PromptWarnMissingActivePlugins</c> directly - see <see cref="Resolve"/>'s remarks on
        /// why the console call's timing matters.
        /// </summary>
        public static void WarnMissingActivePlugins(ILogger logger, IConsoleWriter prompt, IReadOnlyList<string> missingPluginNames)
        {
            if (missingPluginNames.Count > 0)
            {
                LogMessages.LogPluginWarning(logger, string.Format(null, FormatMessages.PluginsActiveMissing, string.Join(", ", missingPluginNames)));
            }

            prompt.PromptWarnMissingActivePlugins(missingPluginNames);
        }
    }
}
