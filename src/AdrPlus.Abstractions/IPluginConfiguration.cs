// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the AdrPlus project under MIT license
// ***************************************************************************************

namespace AdrPlus.Abstractions
{
    /// <summary>
    /// Typed, read-only access to the plugin's own <c>plugin.json</c> <c>settings</c> object.
    /// </summary>
    /// <remarks>
    /// Settings are plain, non-secret configuration (base URLs, space keys, etc.) — <c>plugin.json</c> may be
    /// committed to the repo, so credential values must never be stored here.
    /// </remarks>
    public interface IPluginConfiguration
    {
        /// <summary>
        /// Gets the value for <paramref name="key"/> converted to <typeparamref name="T"/>, or <c>default</c> if the key is absent.
        /// </summary>
        T? GetValue<T>(string key);
    }
}
