// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the AdrPlus project under MIT license
// ***************************************************************************************

namespace AdrPlus.Abstractions
{
    /// <summary>
    /// Host-provided services given to a plugin at <see cref="IAdrPlugin.InitializeAsync"/>. Provides no secrets —
    /// credential resolution is entirely the plugin's own responsibility.
    /// </summary>
    public interface IPluginContext
    {
        /// <summary>
        /// Gets the logger the plugin should use, unified with the host's own file log.
        /// </summary>
        IPluginLogger Logger { get; }
    }
}
