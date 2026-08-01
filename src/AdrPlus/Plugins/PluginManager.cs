// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the AdrPlus project under MIT license
// ***************************************************************************************

using AdrPlus.Abstractions;
using AdrPlus.Abstractions.Domain;
using AdrPlus.Domain;
using AdrPlus.Infrastructure.FileSystem;
using AdrPlus.Infrastructure.Formatting;
using AdrPlus.Infrastructure.Logging;
using AdrPlus.Infrastructure.UI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AdrPlus.Plugins
{
    /// <summary>
    /// Default <see cref="IPluginManager"/> implementation. Discovers plugin subfolders via
    /// <see cref="IFileSystemService.GetDirectories"/>, validates each with a <see cref="PluginLoader"/>, and
    /// reads the plugin allowlist from <see cref="AdrPlusConfig.PluginAllowlist"/>. Never throws for a
    /// rejected plugin — fail-soft, consistent with D30.
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of the <see cref="PluginManager"/> class.
    /// </remarks>
    /// <param name="fileSystem">The file system service used to discover plugin subfolders and read manifests.</param>
    /// <param name="config">The application configuration, providing the optional plugin allowlist.</param>
    /// <param name="logger">The logger for recording plugin rejections and warnings.</param>
    /// <param name="prompt">The console writer for surfacing plugin rejections and warnings to the user.</param>
    internal sealed class PluginManager(
        IFileSystemService fileSystem,
        IOptions<AdrPlusConfig> config,
        ILogger<PluginManager> logger,
        IConsoleWriter prompt) : IPluginManager
    {
        private readonly IFileSystemService _fileSystem = fileSystem;
        private readonly AdrPlusConfig _config = config.Value;
        private readonly ILogger<PluginManager> _logger = logger;
        private readonly IConsoleWriter _prompt = prompt;
        // internal (not private): lets unit tests seed loaded plugins directly, bypassing the real
        // AssemblyLoadContext load that LoadPluginsAsync requires (deferred to Fase 11's fixture plugin).
        internal readonly List<LoadedPlugin> _loadedPlugins = [];
        private readonly List<PluginRejection> _rejections = [];

        // Per-plugin InitializeAsync state (D30). Deliberately NOT cleared by LoadPluginsAsync: "already tried to
        // initialize this run" must survive any reload, unlike the discovery-scoped _loadedPlugins/_rejections.
        private readonly HashSet<string> _initializedPlugins = new(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _initFailedPlugins = new(StringComparer.OrdinalIgnoreCase);

        /// <inheritdoc/>
        public IReadOnlyList<LoadedPlugin> LoadedPlugins => _loadedPlugins;

        /// <inheritdoc/>
        public IReadOnlyList<PluginRejection> Rejections => _rejections;

        /// <inheritdoc/>
        public async Task LoadPluginsAsync(string pluginsRootPath, CancellationToken cancellationToken = default)
        {
            _loadedPlugins.Clear();
            _rejections.Clear();

            if (!_fileSystem.DirectoryExists(pluginsRootPath))
            {
                return;
            }

            var loader = new PluginLoader(_fileSystem);

            // Stage 1: validate every candidate's manifest in isolation (schema, path-traversal guard, allowlist).
            // Duplicate names can only be known once every candidate has reached this point.
            var candidates = new List<(string FolderPath, PluginManifest Manifest)>();

            foreach (var folderPath in _fileSystem.GetDirectories(pluginsRootPath).OrderBy(path => path, StringComparer.Ordinal))
            {
                var outcome = await loader.ValidateManifestAsync(
                    folderPath,
                    _config.PluginAllowlist,
                    pluginName => WriteWarning(string.Format(null, FormatMessages.PluginAllowlistHashNotEnforced, pluginName)),
                    cancellationToken);

                if (outcome.Manifest is { } manifest)
                {
                    candidates.Add((folderPath, manifest));
                }
                else if (outcome.Rejection is { } rejection)
                {
                    _rejections.Add(rejection);
                    WriteWarning(rejection.Message);
                }
            }

            // Stage 2: group by name (D22, case-insensitive) — every candidate in a group with more than one
            // member is rejected as a duplicate; only a uniquely-named candidate proceeds to load its assembly.
            foreach (var group in candidates.GroupBy(candidate => candidate.Manifest.Name!, StringComparer.OrdinalIgnoreCase))
            {
                if (group.Count() > 1)
                {
                    foreach (var (folderPath, manifest) in group)
                    {
                        var rejection = PluginLoader.RejectDuplicateName(folderPath, manifest.Name!);
                        _rejections.Add(rejection);
                        WriteWarning(rejection.Message);
                    }
                    continue;
                }

                var (soleFolderPath, soleManifest) = group.Single();
                var outcome = PluginLoader.LoadAssembly(soleFolderPath, soleManifest);

                if (outcome.Loaded is { } loaded)
                {
                    _loadedPlugins.Add(loaded);
                }
                else if (outcome.Rejection is { } rejection)
                {
                    _rejections.Add(rejection);
                    WriteWarning(rejection.Message);
                }
            }
        }

        /// <inheritdoc/>
        public async Task DispatchAsync(
            AdrEventType eventType,
            AdrRecordSnapshot adr,
            string adrFilePath,
            Func<string> getAdrRenderedContent,
            RepoInfoSnapshot repo,
            bool isReplay,
            CancellationToken cancellationToken = default)
        {
            if (_loadedPlugins.Count == 0)
            {
                return;
            }

            var correlationId = Guid.NewGuid().ToString();
            var context = new AdrEventContext
            {
                EventType = eventType,
                IsReplay = isReplay,
                Adr = adr,
                AdrFilePath = adrFilePath,
                GetAdrRenderedContent = getAdrRenderedContent,
                Repo = repo,
                CorrelationId = correlationId
            };

            var candidates = _loadedPlugins.Where(plugin =>
                !_initFailedPlugins.Contains(plugin.Manifest.Name!) &&
                (plugin.Manifest.SubscribedEvents?.Contains(eventType.ToString(), StringComparer.OrdinalIgnoreCase) ?? false));

            var filtered = new List<LoadedPlugin>();
            foreach (var plugin in candidates)
            {
                bool shouldHandle;
                try
                {
                    shouldHandle = plugin.Instance.ShouldHandle(context);
                }
                catch (Exception ex)
                {
                    LogMessages.LogPluginError(_logger, ex, $"{plugin.Manifest.Name}: ShouldHandle threw, skipping for this event");
                    continue;
                }

                if (shouldHandle)
                {
                    filtered.Add(plugin);
                }
            }

            if (filtered.Count == 0)
            {
                return;
            }

            await Task.WhenAll(filtered.Select(plugin => DispatchToPluginAsync(plugin, context, correlationId, cancellationToken)));
        }

        private async Task DispatchToPluginAsync(LoadedPlugin plugin, AdrEventContext context, string correlationId, CancellationToken cancellationToken)
        {
            var name = plugin.Manifest.Name!;

            if (!_initializedPlugins.Contains(name))
            {
                try
                {
                    var pluginLogger = new HostPluginLogger(_logger);
                    var pluginContext = new HostPluginContext(pluginLogger);
                    var pluginConfig = new HostPluginConfiguration(plugin.Manifest.Settings);
                    await plugin.Instance.InitializeAsync(pluginContext, pluginConfig, cancellationToken);
                    _initializedPlugins.Add(name);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    _initFailedPlugins.Add(name);
                    WritePermanentFailure(name, ex);
                    return;
                }
            }

            Task<PluginResult> hookTask;
            try
            {
                hookTask = plugin.Instance.OnAdrEventAsync(context, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                await HandleFailureAsync(plugin, context, correlationId, ex.Message, isRetryable: true, attempts: 1, cancellationToken);
                return;
            }

            // The delay uses CancellationToken.None deliberately: if the user cancels the command, the delay must
            // not race to "elapsed" and get misread as a plugin timeout — cancellation is checked explicitly below.
            var delayTask = Task.Delay(plugin.Manifest.ForegroundTimeoutMs, CancellationToken.None);
            var completed = await Task.WhenAny(hookTask, delayTask);

            cancellationToken.ThrowIfCancellationRequested();

            if (completed == delayTask)
            {
                // The losing hook task keeps running; observe its eventual fault so it never surfaces as an
                // unobserved task exception later. Its result is never used.
                _ = hookTask.ContinueWith(
                    t => LogMessages.LogPluginError(_logger, t.Exception, $"{name}: hook faulted after the foreground timeout elapsed"),
                    CancellationToken.None,
                    TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously,
                    TaskScheduler.Default);

                await HandleFailureAsync(plugin, context, correlationId, $"foreground timeout ({plugin.Manifest.ForegroundTimeoutMs}ms) elapsed", isRetryable: true, attempts: 0, cancellationToken);
                return;
            }

            PluginResult result;
            try
            {
                result = await hookTask;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                await HandleFailureAsync(plugin, context, correlationId, ex.Message, isRetryable: true, attempts: 1, cancellationToken);
                return;
            }

            switch (result.Status)
            {
                case PluginResultStatus.Success:
                case PluginResultStatus.Skipped:
                    if (_logger.IsEnabled(LogLevel.Information))
                    {
                        var message = string.Concat(name, ": ", result.Status.ToString());
                        LogMessages.LogPluginInfo(_logger, message);
                    }
                    return;
                default:
                    await HandleFailureAsync(plugin, context, correlationId, result.Message ?? string.Empty, result.IsRetryable, attempts: 1, cancellationToken);
                    return;
            }
        }

        private async Task HandleFailureAsync(LoadedPlugin plugin, AdrEventContext context, string correlationId, string lastError, bool isRetryable, int attempts, CancellationToken cancellationToken)
        {
            var name = plugin.Manifest.Name!;

            if (!isRetryable)
            {
                WritePermanentFailure(name, lastError);
                return;
            }

            var entry = new PendingEntry
            {
                AdrKey = BuildAdrKey(context.Adr),
                EventType = context.EventType.ToString(),
                CorrelationId = correlationId,
                LastError = lastError,
                Attempts = attempts,
                Timestamp = DateTime.UtcNow
            };
            await PendingStateWriter.UpsertAsync(_fileSystem, plugin.FolderPath, entry, cancellationToken);
            WriteWarning(string.Format(null, FormatMessages.PluginQueuedForRetry, name));
        }

        private static string BuildAdrKey(AdrRecordSnapshot adr) => $"{adr.Number:D4}-v{adr.Version}-r{adr.Revision ?? 0}";

        private void WriteWarning(string message)
        {
            LogMessages.LogPluginWarning(_logger, message);
            _prompt.PromptWriteInfo(message);
        }

        private void WritePermanentFailure(string pluginName, Exception exception) => WritePermanentFailure(pluginName, exception.Message, exception);

        private void WritePermanentFailure(string pluginName, string reasonDetail, Exception? exception = null)
        {
            var message = string.Format(null, FormatMessages.PluginPermanentFailure, pluginName);
            LogMessages.LogPluginError(_logger, exception, $"{message} ({reasonDetail})");
            _prompt.PromptWriteError(message);
        }
    }
}
