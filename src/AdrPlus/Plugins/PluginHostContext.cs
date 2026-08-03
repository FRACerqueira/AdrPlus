// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the AdrPlus project under MIT license
// ***************************************************************************************

using AdrPlus.Abstractions;
using AdrPlus.Infrastructure.Logging;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace AdrPlus.Plugins
{
    /// <summary>
    /// Adapts the host's <see cref="ILogger"/> to <see cref="IPluginLogger"/>, unifying plugin log entries with
    /// the host's own file log.
    /// </summary>
    internal sealed class HostPluginLogger(ILogger logger) : IPluginLogger
    {
        private readonly ILogger _logger = logger;

        public void LogInformation(string message) => LogMessages.LogPluginInfo(_logger, message);

        public void LogWarning(string message) => LogMessages.LogPluginWarning(_logger, message);

        public void LogError(string message, Exception? exception = null) => LogMessages.LogPluginError(_logger, exception, message);
    }

    /// <summary>
    /// Host-provided services passed to a plugin's <see cref="IAdrPlugin.InitializeAsync"/>. Provides no
    /// secrets — credential resolution is entirely the plugin's own responsibility.
    /// </summary>
    internal sealed class HostPluginContext(IPluginLogger logger) : IPluginContext
    {
        public IPluginLogger Logger { get; } = logger;
    }

    /// <summary>
    /// Typed, read-only view over a plugin's own <c>plugin.json</c> <c>settings</c> object.
    /// </summary>
    internal sealed class HostPluginConfiguration(Dictionary<string, JsonElement>? settings) : IPluginConfiguration
    {
        private readonly Dictionary<string, JsonElement> _settings = settings ?? [];

        public T? GetValue<T>(string key) =>
            _settings.TryGetValue(key, out var element) ? element.Deserialize<T>() : default;
    }
}
