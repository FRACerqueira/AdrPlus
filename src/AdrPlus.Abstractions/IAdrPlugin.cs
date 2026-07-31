// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the AdrPlus project under MIT license
// ***************************************************************************************

namespace AdrPlus.Abstractions
{
    /// <summary>
    /// The single mandatory contract every AdrPlus plugin must implement.
    /// </summary>
    /// <remarks>
    /// One singleton instance is held per plugin, reused across events for the lifetime of the process —
    /// <see cref="OnAdrEventAsync"/> must be reentrant. <see cref="InitializeAsync"/> is called lazily: only the
    /// first time, in this process, that an event this plugin subscribes to is about to be dispatched.
    /// </remarks>
    public interface IAdrPlugin : IAsyncDisposable
    {
        /// <summary>
        /// Gets the plugin's name. Must match the <c>name</c> declared in its <c>plugin.json</c> manifest.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets the plugin's version. Must match the <c>version</c> declared in its <c>plugin.json</c> manifest.
        /// </summary>
        string Version { get; }

        /// <summary>
        /// Called once, lazily, before the first subscribed event is dispatched to this plugin in this process.
        /// If this throws, the host skips this plugin for the rest of the run and logs a permanent-failure warning —
        /// no event dispatched here is queued for retry.
        /// </summary>
        Task InitializeAsync(IPluginContext context, IPluginConfiguration config, CancellationToken ct);

        /// <summary>
        /// Cheap, synchronous, declarative filter allowing the host to skip invoking <see cref="OnAdrEventAsync"/> entirely.
        /// </summary>
        bool ShouldHandle(AdrEventContext context);

        /// <summary>
        /// Reacts to an ADR lifecycle event. Must never throw for control flow — return a <see cref="PluginResult"/>
        /// with <see cref="PluginResultStatus.Failed"/> instead. Must treat unknown/future <see cref="AdrEventType"/>
        /// values as <see cref="PluginResultStatus.Skipped"/>.
        /// </summary>
        Task<PluginResult> OnAdrEventAsync(AdrEventContext context, CancellationToken ct);
    }
}
