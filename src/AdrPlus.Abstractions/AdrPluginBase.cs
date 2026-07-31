// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the AdrPlus project under MIT license
// ***************************************************************************************

namespace AdrPlus.Abstractions
{
    /// <summary>
    /// Optional convenience base class implementing <see cref="IAdrPlugin"/>. Shields <see cref="HandleAsync"/>
    /// exceptions into a <see cref="PluginResultStatus.Failed"/> result and exposes <see cref="Success"/>/<see cref="Skip"/>/<see cref="Fail(string, bool)"/>
    /// helpers. Plugin authors may ignore this and implement <see cref="IAdrPlugin"/> directly instead.
    /// </summary>
    public abstract class AdrPluginBase : IAdrPlugin
    {
        /// <inheritdoc />
        public abstract string Name { get; }

        /// <inheritdoc />
        public abstract string Version { get; }

        /// <inheritdoc />
        public virtual Task InitializeAsync(IPluginContext context, IPluginConfiguration config, CancellationToken ct) => Task.CompletedTask;

        /// <inheritdoc />
        public virtual bool ShouldHandle(AdrEventContext context) => true;

        /// <summary>
        /// Reacts to an ADR lifecycle event that already passed <see cref="ShouldHandle"/>. Exceptions thrown here
        /// are caught by <see cref="OnAdrEventAsync"/> and turned into a retryable <see cref="Fail(string, bool)"/> result.
        /// </summary>
        protected abstract Task<PluginResult> HandleAsync(AdrEventContext context, CancellationToken ct);

        /// <inheritdoc />
        public async Task<PluginResult> OnAdrEventAsync(AdrEventContext context, CancellationToken ct)
        {
            if (!ShouldHandle(context))
            {
                return Skip();
            }

            try
            {
                return await HandleAsync(context, ct).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                return Fail(ex.Message);
            }
        }

        /// <inheritdoc />
        public virtual ValueTask DisposeAsync()
        {
            GC.SuppressFinalize(this);
            return ValueTask.CompletedTask;
        }

        /// <summary>
        /// Builds a <see cref="PluginResultStatus.Success"/> result.
        /// </summary>
        protected static PluginResult Success(string? externalKey = null) =>
            new() { Status = PluginResultStatus.Success, ExternalKey = externalKey };

        /// <summary>
        /// Builds a <see cref="PluginResultStatus.Skipped"/> result.
        /// </summary>
        protected static PluginResult Skip(string? message = null) =>
            new() { Status = PluginResultStatus.Skipped, Message = message };

        /// <summary>
        /// Builds a <see cref="PluginResultStatus.Failed"/> result. Retryable by default; pass
        /// <paramref name="isRetryable"/>: <c>false</c> for permanent/configuration failures (e.g. invalid credentials)
        /// that would fail identically on every retry.
        /// </summary>
        protected static PluginResult Fail(string message, bool isRetryable = true) =>
            new() { Status = PluginResultStatus.Failed, Message = message, IsRetryable = isRetryable };
    }
}
