// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the AdrPlus project under MIT license
// ***************************************************************************************

namespace AdrPlus.Plugins
{
    /// <summary>
    /// Normalized result of a single <see cref="PluginManager.InvokeOnceAsync"/> call — shared between the
    /// foreground dispatch path and the background retry engine.
    /// </summary>
    internal enum PluginInvokeStatus
    {
        Success,
        Skipped,
        Failed
    }

    /// <summary>
    /// Outcome of one attempt to invoke a plugin's <c>OnAdrEventAsync</c> hook.
    /// </summary>
    /// <param name="Status">Whether the hook succeeded, was skipped, or failed.</param>
    /// <param name="IsRetryable">Whether a <see cref="PluginInvokeStatus.Failed"/> outcome should be retried.</param>
    /// <param name="ErrorMessage">The failure detail, when <see cref="Status"/> is <see cref="PluginInvokeStatus.Failed"/>.</param>
    /// <param name="WasTimeout">
    /// Whether the failure was caused by the hook not completing within its timeout (as opposed to an explicit
    /// <c>Failed</c> result or a thrown exception) — only consumed by the foreground dispatch path, which
    /// records a pending entry's <c>attempts</c> as <c>0</c> for a timeout vs. <c>1</c> for a completed failure.
    /// </param>
    internal readonly record struct PluginInvokeOutcome(PluginInvokeStatus Status, bool IsRetryable, string? ErrorMessage, bool WasTimeout = false)
    {
        public static PluginInvokeOutcome Success() => new(PluginInvokeStatus.Success, false, null);

        public static PluginInvokeOutcome Skipped() => new(PluginInvokeStatus.Skipped, false, null);

        public static PluginInvokeOutcome Failed(string message, bool isRetryable) => new(PluginInvokeStatus.Failed, isRetryable, message);

        public static PluginInvokeOutcome Timeout(int timeoutMs) => new(PluginInvokeStatus.Failed, true, $"timeout ({timeoutMs}ms) elapsed", WasTimeout: true);
    }
}
