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
            Func<LoadedPlugin, bool>? isActive = null,
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
                (isActive is null || isActive(plugin)) &&
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

        /// <inheritdoc/>
        public async Task<SyncSummary> RetryPendingAsync(
            Func<string, (AdrRecordSnapshot Adr, string FilePath, string Content)?> resolveAdr,
            RepoInfoSnapshot repo,
            Func<LoadedPlugin, bool>? isActive = null,
            CancellationToken cancellationToken = default)
        {
            var summary = new SyncSummary();

            foreach (var plugin in _loadedPlugins)
            {
                if (isActive is not null && !isActive(plugin))
                {
                    continue;
                }

                var entries = await PendingStateStore.ReadAllAsync(_fileSystem, plugin.FolderPath, cancellationToken);
                if (entries.Count == 0)
                {
                    continue;
                }

                if (!await EnsureInitializedAsync(plugin, cancellationToken))
                {
                    // Config is broken, not the entries themselves — leave them untouched; the user fixes the
                    // plugin and reruns sync (D30).
                    continue;
                }

                var retryPolicy = plugin.Manifest.RetryPolicy ?? new PluginRetryPolicy();
                var remaining = new List<PendingEntry>();

                foreach (var entry in entries)
                {
                    if (!Enum.TryParse<AdrEventType>(entry.EventType, out var eventType))
                    {
                        WriteWarning($"{plugin.Manifest.Name}: pending entry for '{entry.AdrKey}' has an unrecognized eventType '{entry.EventType}', dropping");
                        summary.Dropped++;
                        continue;
                    }

                    var resolved = resolveAdr(entry.AdrKey);
                    if (resolved is not { } adr)
                    {
                        WriteWarning(string.Format(null, FormatMessages.PluginPendingAdrNotFound, plugin.Manifest.Name, entry.AdrKey));
                        summary.Dropped++;
                        continue;
                    }

                    var context = new AdrEventContext
                    {
                        EventType = eventType,
                        IsReplay = false,
                        Adr = adr.Adr,
                        AdrFilePath = adr.FilePath,
                        GetAdrRenderedContent = () => adr.Content,
                        Repo = repo,
                        CorrelationId = Guid.NewGuid().ToString()
                    };

                    bool shouldHandle;
                    try
                    {
                        shouldHandle = plugin.Instance.ShouldHandle(context);
                    }
                    catch (Exception ex)
                    {
                        LogMessages.LogPluginError(_logger, ex, $"{plugin.Manifest.Name}: ShouldHandle threw on retry, skipping for this entry");
                        remaining.Add(entry);
                        summary.StillPending++;
                        continue;
                    }

                    if (!shouldHandle)
                    {
                        summary.Skipped++;
                        continue;
                    }

                    var retried = await RetryEntryAsync(plugin, context, entry, retryPolicy, summary, cancellationToken);
                    if (retried is not null)
                    {
                        remaining.Add(retried);
                    }
                }

                await PendingStateStore.WriteAllAsync(_fileSystem, plugin.FolderPath, remaining, cancellationToken);
            }

            return summary;
        }

        /// <inheritdoc/>
        public async Task DisposeLoadedPluginsAsync(CancellationToken cancellationToken = default)
        {
            foreach (var plugin in _loadedPlugins)
            {
                try
                {
                    await plugin.Instance.DisposeAsync();
                }
                catch (Exception ex)
                {
                    LogMessages.LogPluginError(_logger, ex, $"{plugin.Manifest.Name}: DisposeAsync threw during shutdown");
                }
            }

            foreach (var plugin in _loadedPlugins)
            {
                try
                {
                    plugin.LoadContext?.Unload();
                }
                catch (Exception ex)
                {
                    LogMessages.LogPluginError(_logger, ex, $"{plugin.Manifest.Name}: AssemblyLoadContext.Unload threw during shutdown");
                }
            }

            _loadedPlugins.Clear();
            _rejections.Clear();
        }

        /// <inheritdoc/>
        public async Task<SyncSummary> BackfillAsync(
            IEnumerable<(AdrEventType EventType, AdrRecordSnapshot Adr, string FilePath, Func<string> GetContent)> settledAdrs,
            RepoInfoSnapshot repo,
            Func<LoadedPlugin, bool>? isActive = null,
            CancellationToken cancellationToken = default)
        {
            var total = new SyncSummary();

            if (_loadedPlugins.Count == 0)
            {
                return total;
            }

            var items = settledAdrs.ToList();

            // Sequential init phase: EnsureInitializedAsync mutates _initializedPlugins/_initFailedPlugins
            // (plain HashSets, not thread-safe). Running it here — before the parallel per-plugin sweep below —
            // means those sets are only ever read, never mutated, once concurrency starts.
            foreach (var plugin in _loadedPlugins)
            {
                if (isActive is not null && !isActive(plugin))
                {
                    continue;
                }
                await EnsureInitializedAsync(plugin, cancellationToken);
            }

            var readyPlugins = _loadedPlugins.Where(plugin =>
                (isActive is null || isActive(plugin)) &&
                !_initFailedPlugins.Contains(plugin.Manifest.Name!));
            var perPluginSummaries = await Task.WhenAll(readyPlugins.Select(plugin => BackfillPluginAsync(plugin, items, repo, cancellationToken)));

            foreach (var summary in perPluginSummaries)
            {
                total.Succeeded += summary.Succeeded;
                total.Skipped += summary.Skipped;
                total.PermanentlyFailed += summary.PermanentlyFailed;
                total.Exhausted += summary.Exhausted;
            }

            return total;
        }

        private async Task<SyncSummary> BackfillPluginAsync(
            LoadedPlugin plugin,
            IReadOnlyList<(AdrEventType EventType, AdrRecordSnapshot Adr, string FilePath, Func<string> GetContent)> items,
            RepoInfoSnapshot repo,
            CancellationToken cancellationToken)
        {
            var summary = new SyncSummary();
            var retryPolicy = plugin.Manifest.RetryPolicy ?? new PluginRetryPolicy();

            foreach (var item in items)
            {
                if (!(plugin.Manifest.SubscribedEvents?.Contains(item.EventType.ToString(), StringComparer.OrdinalIgnoreCase) ?? false))
                {
                    continue;
                }

                var context = new AdrEventContext
                {
                    EventType = item.EventType,
                    IsReplay = true,
                    Adr = item.Adr,
                    AdrFilePath = item.FilePath,
                    GetAdrRenderedContent = item.GetContent,
                    Repo = repo,
                    CorrelationId = Guid.NewGuid().ToString()
                };

                bool shouldHandle;
                try
                {
                    shouldHandle = plugin.Instance.ShouldHandle(context);
                }
                catch (Exception ex)
                {
                    LogMessages.LogPluginError(_logger, ex, $"{plugin.Manifest.Name}: ShouldHandle threw during backfill, skipping this ADR");
                    continue;
                }

                if (!shouldHandle)
                {
                    summary.Skipped++;
                    continue;
                }

                AttemptLoopOutcome outcome;
                try
                {
                    outcome = await RunAttemptLoopAsync(plugin, context, retryPolicy, startAttempts: 0, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    // Return whatever this plugin already accomplished rather than losing it — a backfill sweep
                    // can run for a long time (D18 serializes per plugin, full retryPolicy per item) and a
                    // cancelled item shouldn't erase every prior item's outcome.
                    return summary;
                }

                switch (outcome.Result)
                {
                    case AttemptLoopResult.Succeeded:
                        summary.Succeeded++;
                        break;
                    case AttemptLoopResult.Skipped:
                        summary.Skipped++;
                        break;
                    case AttemptLoopResult.PermanentlyFailed:
                        WritePermanentFailure(plugin.Manifest.Name!, outcome.LastError ?? string.Empty);
                        summary.PermanentlyFailed++;
                        break;
                    default: // Exhausted
                        WriteWarning(string.Format(null, FormatMessages.PluginBackfillExhausted, plugin.Manifest.Name, BuildAdrKey(item.Adr)));
                        summary.Exhausted++;
                        break;
                }
            }

            return summary;
        }

        /// <summary>
        /// Retries a single pending entry against <paramref name="retryPolicy"/>, guaranteeing at least one
        /// attempt this run even if <see cref="PendingEntry.Attempts"/> already reached
        /// <see cref="PluginRetryPolicy.MaxAttempts"/> in a previous <c>sync</c> run — otherwise the user's
        /// "keep pending across runs" policy silently becomes "never retried again".
        /// </summary>
        /// <returns>
        /// <see langword="null"/> when the entry should be removed from <c>pending.json</c> (succeeded, skipped,
        /// or permanently failed); the (possibly updated) entry when it should remain pending.
        /// </returns>
        private async Task<PendingEntry?> RetryEntryAsync(LoadedPlugin plugin, AdrEventContext context, PendingEntry entry, PluginRetryPolicy retryPolicy, SyncSummary summary, CancellationToken cancellationToken)
        {
            var outcome = await RunAttemptLoopAsync(plugin, context, retryPolicy, entry.Attempts, cancellationToken);

            switch (outcome.Result)
            {
                case AttemptLoopResult.Succeeded:
                    summary.Succeeded++;
                    return null;
                case AttemptLoopResult.Skipped:
                    summary.Skipped++;
                    return null;
                case AttemptLoopResult.PermanentlyFailed:
                    WritePermanentFailure(plugin.Manifest.Name!, outcome.LastError ?? string.Empty);
                    summary.PermanentlyFailed++;
                    return null;
                default: // Exhausted
                    entry.Attempts = outcome.AttemptsMade;
                    entry.LastError = outcome.LastError;
                    entry.Timestamp = DateTime.UtcNow;
                    summary.StillPending++;
                    return entry;
            }
        }

        /// <summary>
        /// Runs the attempt loop for one (plugin, event) pair against <paramref name="retryPolicy"/>, guaranteeing
        /// at least one attempt even if <paramref name="startAttempts"/> already reached
        /// <see cref="PluginRetryPolicy.MaxAttempts"/> — shared by Fase 5's pending re-drive (<c>startAttempts</c>
        /// = the entry's prior attempt count) and Fase 6's backfill sweep (<c>startAttempts</c> = <c>0</c>,
        /// always a fresh sweep). The delay before attempt N uses N's absolute, cumulative number — it grows
        /// even across separate <c>sync</c> runs for the same logical item — but the very first attempt made in
        /// this call never sleeps first (the caller's own state already reflects real elapsed time).
        /// </summary>
        private async Task<AttemptLoopOutcome> RunAttemptLoopAsync(LoadedPlugin plugin, AdrEventContext context, PluginRetryPolicy retryPolicy, int startAttempts, CancellationToken cancellationToken)
        {
            var attemptsThisRun = Math.Max(1, retryPolicy.MaxAttempts - startAttempts);
            string? lastError = null;
            var lastAttempt = startAttempts;

            for (var i = 1; i <= attemptsThisRun; i++)
            {
                var absoluteAttempt = startAttempts + i;
                lastAttempt = absoluteAttempt;

                if (absoluteAttempt > 1)
                {
                    var delayMs = ComputeDelay(retryPolicy, absoluteAttempt);
                    if (delayMs > 0)
                    {
                        await Task.Delay(delayMs, cancellationToken);
                    }
                }

                var outcome = await InvokeOnceAsync(plugin, context, plugin.Manifest.BackgroundTimeoutMs, cancellationToken);

                switch (outcome.Status)
                {
                    case PluginInvokeStatus.Success:
                        return new AttemptLoopOutcome(AttemptLoopResult.Succeeded, absoluteAttempt, null);
                    case PluginInvokeStatus.Skipped:
                        return new AttemptLoopOutcome(AttemptLoopResult.Skipped, absoluteAttempt, null);
                    default:
                        if (!outcome.IsRetryable)
                        {
                            return new AttemptLoopOutcome(AttemptLoopResult.PermanentlyFailed, absoluteAttempt, outcome.ErrorMessage);
                        }
                        lastError = outcome.ErrorMessage;
                        break;
                }
            }

            return new AttemptLoopOutcome(AttemptLoopResult.Exhausted, lastAttempt, lastError);
        }

        /// <summary>
        /// Computes the backoff delay (ms) for <paramref name="attempt"/> (1-based, absolute across every
        /// <c>sync</c> run an entry has ever survived) per spec §4.4. <c>Fixed</c> is a flat <see cref="PluginRetryPolicy.DelayMs"/>;
        /// <c>Exponential</c> doubles per attempt. The exponent and the result are both clamped — <paramref name="attempt"/>
        /// is cumulative and unbounded (the user's chosen "keep pending across runs" policy), so an unclamped
        /// <c>2^(attempt-1)</c> overflows and goes negative around attempt≈32, which would make <c>Task.Delay</c> throw.
        /// </summary>
        // internal (not private): lets ComputeDelayTests exercise the Fixed/Exponential/jitter/overflow-clamp
        // formula in isolation, without any Task.Delay actually elapsing.
        internal static int ComputeDelay(PluginRetryPolicy retryPolicy, int attempt)
        {
            const int MaxDelayMs = 300_000; // 5 minutes

            long delay = string.Equals(retryPolicy.Backoff, "Exponential", StringComparison.OrdinalIgnoreCase)
                ? retryPolicy.DelayMs * (1L << Math.Min(attempt - 1, 30))
                : retryPolicy.DelayMs;

            delay = Math.Min(delay, MaxDelayMs);

            if (retryPolicy.Jitter)
            {
                delay = Random.Shared.NextInt64(0, delay + 1);
            }

            return (int)delay;
        }

        private async Task DispatchToPluginAsync(LoadedPlugin plugin, AdrEventContext context, string correlationId, CancellationToken cancellationToken)
        {
            var name = plugin.Manifest.Name!;

            if (!await EnsureInitializedAsync(plugin, cancellationToken))
            {
                return;
            }

            var outcome = await InvokeOnceAsync(plugin, context, plugin.Manifest.ForegroundTimeoutMs, cancellationToken);

            switch (outcome.Status)
            {
                case PluginInvokeStatus.Success:
                case PluginInvokeStatus.Skipped:
                    if (_logger.IsEnabled(LogLevel.Information))
                    {
                        var message = string.Concat(name, ": ", outcome.Status.ToString());
                        LogMessages.LogPluginInfo(_logger, message);
                    }
                    return;
                default:
                    var attempts = outcome.WasTimeout ? 0 : 1;
                    await HandleFailureAsync(plugin, context, correlationId, outcome.ErrorMessage ?? string.Empty, outcome.IsRetryable, attempts, cancellationToken);
                    return;
            }
        }

        /// <summary>
        /// Lazily runs <see cref="IAdrPlugin.InitializeAsync"/> once per plugin per process (D30). Shared by
        /// the foreground dispatch path and the background retry engine so "already tried to initialize this
        /// run" state (<see cref="_initializedPlugins"/>/<see cref="_initFailedPlugins"/>) is consistent
        /// between both callers.
        /// </summary>
        /// <returns><see langword="true"/> when the plugin is ready to be invoked; <see langword="false"/> when
        /// initialization already failed (permanently, for this process) and the caller should skip it.</returns>
        private async Task<bool> EnsureInitializedAsync(LoadedPlugin plugin, CancellationToken cancellationToken)
        {
            var name = plugin.Manifest.Name!;

            if (_initializedPlugins.Contains(name))
            {
                return true;
            }
            if (_initFailedPlugins.Contains(name))
            {
                return false;
            }

            try
            {
                var pluginLogger = new HostPluginLogger(_logger);
                var pluginContext = new HostPluginContext(pluginLogger);
                var pluginConfig = new HostPluginConfiguration(plugin.Manifest.Settings);
                await plugin.Instance.InitializeAsync(pluginContext, pluginConfig, cancellationToken);
                _initializedPlugins.Add(name);
                return true;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _initFailedPlugins.Add(name);
                WritePermanentFailure(name, ex);
                return false;
            }
        }

        /// <summary>
        /// Invokes <see cref="IAdrPlugin.OnAdrEventAsync"/> exactly once, racing it against
        /// <paramref name="timeoutMs"/>. Shared by the foreground dispatch path (<paramref name="timeoutMs"/> =
        /// <see cref="PluginManifest.ForegroundTimeoutMs"/>) and the background retry engine
        /// (<paramref name="timeoutMs"/> = <see cref="PluginManifest.BackgroundTimeoutMs"/>) — the only difference between
        /// the two callers is which timeout they race against and how they interpret a non-success outcome.
        /// </summary>
        private async Task<PluginInvokeOutcome> InvokeOnceAsync(LoadedPlugin plugin, AdrEventContext context, int timeoutMs, CancellationToken cancellationToken)
        {
            var name = plugin.Manifest.Name!;

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
                return PluginInvokeOutcome.Failed(ex.Message, isRetryable: true);
            }

            // The delay uses CancellationToken.None deliberately: if the user cancels the command, the delay must
            // not race to "elapsed" and get misread as a plugin timeout — cancellation is checked explicitly below.
            var delayTask = Task.Delay(timeoutMs, CancellationToken.None);
            var completed = await Task.WhenAny(hookTask, delayTask);

            cancellationToken.ThrowIfCancellationRequested();

            if (completed == delayTask)
            {
                // The losing hook task keeps running; observe its eventual fault so it never surfaces as an
                // unobserved task exception later. Its result is never used.
                _ = hookTask.ContinueWith(
                    t => LogMessages.LogPluginError(_logger, t.Exception, $"{name}: hook faulted after timeout ({timeoutMs}ms) elapsed"),
                    CancellationToken.None,
                    TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously,
                    TaskScheduler.Default);

                return PluginInvokeOutcome.Timeout(timeoutMs);
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
                return PluginInvokeOutcome.Failed(ex.Message, isRetryable: true);
            }

            return result.Status switch
            {
                PluginResultStatus.Success => PluginInvokeOutcome.Success(),
                PluginResultStatus.Skipped => PluginInvokeOutcome.Skipped(),
                _ => PluginInvokeOutcome.Failed(result.Message ?? string.Empty, result.IsRetryable)
            };
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
            await PendingStateStore.UpsertAsync(_fileSystem, plugin.FolderPath, entry, cancellationToken);
            WriteWarning(string.Format(null, FormatMessages.PluginQueuedForRetry, name));
        }

        private static string BuildAdrKey(AdrRecordSnapshot adr) => AdrKeyFormatter.Format(adr.Number, adr.Version, adr.Revision);

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
