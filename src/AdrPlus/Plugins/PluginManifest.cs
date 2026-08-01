// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the AdrPlus project under MIT license
// ***************************************************************************************

using System.Text.Json;
using System.Text.Json.Serialization;

namespace AdrPlus.Plugins
{
    /// <summary>
    /// Deserialization DTO for a plugin's <c>plugin.json</c> manifest (spec §4.3). Uses camelCase property
    /// naming, distinct from the host's own config files (<c>adrplus.json</c>/<c>adr-config.adrplus</c>),
    /// which use an all-lowercase naming policy.
    /// </summary>
    internal sealed class PluginManifest
    {
        public string? Name { get; set; }

        public string? Version { get; set; }

        public string? EntryAssembly { get; set; }

        public string? EntryType { get; set; }

        public string? AbstractionsVersion { get; set; }

        public List<string>? SubscribedEvents { get; set; }

        public int MaxConcurrency { get; set; } = 1;

        public int ForegroundTimeoutMs { get; set; } = 5000;

        public int TimeoutMs { get; set; } = 30000;

        public PluginRetryPolicy? RetryPolicy { get; set; }

        public Dictionary<string, JsonElement>? Settings { get; set; }

        /// <summary>
        /// JSON serializer options for <c>plugin.json</c>: camelCase naming, case-insensitive.
        /// </summary>
        public static JsonSerializerOptions SerializerOptions { get; } = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            AllowTrailingCommas = true,
            ReadCommentHandling = JsonCommentHandling.Skip
        };
    }

    /// <summary>
    /// Background re-drive retry policy declared in a plugin's manifest (D15). Scoped to <c>adrplus sync</c>
    /// only — the foreground dispatch path (Fase 4) is a single non-retried attempt bounded by
    /// <see cref="PluginManifest.ForegroundTimeoutMs"/>.
    /// </summary>
    internal sealed class PluginRetryPolicy
    {
        public int MaxAttempts { get; set; } = 3;

        public int DelayMs { get; set; } = 2000;

        public string Backoff { get; set; } = "Exponential";

        public bool Jitter { get; set; } = true;
    }
}
