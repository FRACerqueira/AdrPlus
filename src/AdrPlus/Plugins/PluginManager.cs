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
using System.Collections.Concurrent;

namespace AdrPlus.Plugins
{
    /// <summary>
    /// Default <see cref="IPluginManager"/> implementation. Discovers plugin subfolders under
    /// <see cref="BuiltinPluginsRoot"/> and <see cref="UserPluginsRoot"/> (merged into one candidate set)
    /// via <see cref="IFileSystemService.GetDirectories"/>, validates each with a <see cref="PluginLoader"/>,
    /// and reads the plugin allowlist from <see cref="AdrPlusConfig.PluginAllowlist"/>.
    /// Never throws for a rejected plugin — fail-soft by design.
    /// </summary>
    /// <param name="fileSystem">The file system service used to discover plugin subfolders and read manifests.</param>
    /// <param name="config">The application configuration, providing the optional plugin allowlist.</param>
    /// <param name="logger">The logger for recording plugin rejections and warnings.</param>
    /// <param name="prompt">The console writer for surfacing plugin rejections and warnings to the user.</param>
    /// <param name="builtinPluginsRoot">
    /// The folder containing plugins bundled with the AdrPlus install itself (e.g. <c>plugins-builtin</c> next
    /// to the tool's own assembly), or empty to disable it. Left empty by default so tests constructing this
    /// class directly never touch the real file system for this root.
    /// </param>
    /// <param name="userPluginsRoot">
    /// The stable, host-global folder holding plugins installed via <c>adrplus plugins --install</c>, or empty
    /// to disable it. Left empty by default for the same reason as <paramref name="builtinPluginsRoot"/>.
    /// </param>
    internal sealed class PluginManager(
        IFileSystemService fileSystem,
        IOptions<AdrPlusConfig> config,
        ILogger<PluginManager> logger,
        IConsoleWriter prompt,
        string builtinPluginsRoot = "",
        string userPluginsRoot = "") : IPluginManager
    {
        private readonly IFileSystemService _fileSystem = fileSystem;
        private readonly AdrPlusConfig _config = config.Value;
        private readonly ILogger<PluginManager> _logger = logger;
        private readonly IConsoleWriter _prompt = prompt;
        // internal (not private): lets unit tests seed loaded plugins directly, bypassing the real
        // AssemblyLoadContext load that LoadPluginsAsync requires.
        internal readonly List<LoadedPlugin> _loadedPlugins = [];
        private readonly List<PluginRejection> _rejections = [];

        // Per-plugin-instance InitializeAsync state, keyed by reference (not by name): LoadPluginsAsync always
        // creates a brand-new IAdrPlugin instance per candidate, so a reload must not let a new instance inherit
        // an older instance's "already initialized"/"init failed" status just because it shares a manifest name -
        // the new instance has never actually run InitializeAsync. Cleared alongside _loadedPlugins whenever a
        // generation is disposed, so these never hold a reference to a disposed instance.
        private readonly HashSet<IAdrPlugin> _initializedPlugins = new();
        private readonly HashSet<IAdrPlugin> _initFailedPlugins = new();

        // Hook tasks abandoned by a foreground/background timeout, keyed by the plugin instance they belong to.
        // Lets a subsequent dispose/reload wait briefly for a still-running abandoned hook instead of blindly
        // racing DisposeAsync against it on the same instance. ConcurrentDictionary because DispatchAsync fans
        // out to multiple plugins concurrently via Task.WhenAll, each touching its own (but concurrently-written) key.
        private readonly ConcurrentDictionary<LoadedPlugin, Task> _outstandingHooks = new();

        /// <inheritdoc/>
        public string BuiltinPluginsRoot { get; } = builtinPluginsRoot;

        /// <inheritdoc/>
        public string UserPluginsRoot { get; } = userPluginsRoot;

        /// <inheritdoc/>
        public IReadOnlyList<LoadedPlugin> LoadedPlugins => _loadedPlugins;

        /// <inheritdoc/>
        public IReadOnlyList<PluginRejection> Rejections => _rejections;

        /// <inheritdoc/>
        public async Task LoadPluginsAsync(CancellationToken cancellationToken = default)
        {
            // Dispose/unload whatever generation is currently loaded before replacing it - reloading within the
            // same process (e.g. the interactive wizard looping after a config change) must not silently leak
            // the previous generation's instances and AssemblyLoadContexts.
            await DisposeCurrentGenerationAsync();
            _rejections.Clear();

            var loader = new PluginLoader(_fileSystem);

            // Stage 1: validate every candidate's manifest in isolation (schema, path-traversal guard, allowlist).
            // Duplicate names can only be known once every candidate has reached this point. Candidates are
            // gathered from both host-global roots before validation — either root missing/empty is a
            // no-op for that root, not an error.
            var candidates = new List<(string FolderPath, PluginManifest Manifest)>();

            var folderPaths = EnumerateRoot(BuiltinPluginsRoot).Concat(EnumerateRoot(UserPluginsRoot))
                .OrderBy(path => path, StringComparer.Ordinal);

            foreach (var folderPath in folderPaths)
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

            // Stage 2: group by name, case-insensitively — every candidate in a group with more than one
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
            string pendingStateRoot,
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
                !_initFailedPlugins.Contains(plugin.Instance) &&
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
                    var message = $"{plugin.Manifest.Name}: ShouldHandle threw, skipping for this event (correlationId={correlationId})";
                    LogMessages.LogPluginError(_logger, ex, message);
                    _prompt.PromptWriteInfo(message);
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

            // Sequential init phase: EnsureInitializedAsync mutates _initializedPlugins/_initFailedPlugins
            // (plain HashSets, not thread-safe) - must finish before the parallel dispatch below starts,
            // mirroring BackfillAsync's existing sequential-init-then-parallel-sweep split. Without this,
            // concurrent first-time EnsureInitializedAsync calls from Task.WhenAll below corrupt the shared
            // HashSets - silently dropping a subscribed plugin from dispatch, or throwing from inside Add.
            var ready = new List<LoadedPlugin>();
            foreach (var plugin in filtered)
            {
                if (await EnsureInitializedAsync(plugin, cancellationToken))
                {
                    ready.Add(plugin);
                }
            }

            if (ready.Count == 0)
            {
                return;
            }

            await Task.WhenAll(ready.Select(plugin => DispatchToPluginAsync(plugin, context, correlationId, pendingStateRoot, cancellationToken)));
        }

        /// <inheritdoc/>
        public async Task<SyncSummary> RetryPendingAsync(
            Func<string, (AdrRecordSnapshot Adr, string FilePath, string Content)?> resolveAdr,
            RepoInfoSnapshot repo,
            string pendingStateRoot,
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

                var pluginStateFolder = Path.Combine(pendingStateRoot, plugin.Manifest.Name!);

                List<PendingEntry> entries;
                try
                {
                    entries = await PendingStateStore.ReadAllAsync(_fileSystem, pluginStateFolder, cancellationToken, WriteWarning);
                }
                catch (OperationCanceledException) { throw; }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    WriteWarning($"{plugin.Manifest.Name}: could not read pending state ({ex.Message}); skipping this plugin's retry this run.");
                    continue;
                }
                if (entries.Count == 0)
                {
                    continue;
                }

                if (!await EnsureInitializedAsync(plugin, cancellationToken))
                {
                    // Config is broken, not the entries themselves — leave them untouched; the user fixes the
                    // plugin and reruns sync.
                    continue;
                }

                var retryPolicy = plugin.Manifest.RetryPolicy ?? new PluginRetryPolicy();
                var remaining = new List<PendingEntry>();
                var index = 0;

                try
                {
                    for (; index < entries.Count; index++)
                    {
                        var entry = entries[index];

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
                            // Reuses the pending entry's own correlationId (assigned when the original live
                            // dispatch first failed) rather than minting a new one, so retry log lines can
                            // actually be cross-referenced back to the dispatch that queued this entry.
                            CorrelationId = entry.CorrelationId
                        };

                        bool shouldHandle;
                        try
                        {
                            shouldHandle = plugin.Instance.ShouldHandle(context);
                        }
                        catch (Exception ex)
                        {
                            var message = $"{plugin.Manifest.Name}: ShouldHandle threw on retry, skipping for this entry (correlationId={context.CorrelationId})";
                            LogMessages.LogPluginError(_logger, ex, message);
                            _prompt.PromptWriteInfo(message);
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
                }
                catch (OperationCanceledException)
                {
                    // The entry at `index` was interrupted mid-retry (most likely during backoff) and everything
                    // after it was never reached — both are still pending exactly as before, so keep them rather
                    // than losing them: without this, entries already resolved earlier in this same loop would
                    // vanish from `remaining` along with everything unresolved, and get redundantly retried (or
                    // for a non-idempotent plugin, redundantly re-actioned) on the next `sync`.
                    remaining.AddRange(entries.Skip(index));
                    throw;
                }
                finally
                {
                    // CancellationToken.None deliberately: this recovery write must still happen even when the
                    // token that triggered the catch above is already cancelled.
                    try
                    {
                        await PendingStateStore.WriteAllAsync(_fileSystem, pluginStateFolder, remaining, CancellationToken.None);
                    }
                    catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                    {
                        WriteWarning($"{plugin.Manifest.Name}: could not persist pending state ({ex.Message}); progress made this run may be redone next time.");
                    }
                }
            }

            return summary;
        }

        /// <inheritdoc/>
        public async Task DisposeLoadedPluginsAsync(CancellationToken cancellationToken = default)
        {
            await DisposeCurrentGenerationAsync();
            _rejections.Clear();
        }

        /// <summary>
        /// Disposes and unloads every plugin in the currently-loaded generation, then clears
        /// <see cref="_loadedPlugins"/> and the per-instance init-tracking sets. Shared by
        /// <see cref="DisposeLoadedPluginsAsync"/> (final shutdown) and <see cref="LoadPluginsAsync"/> (start of
        /// every reload) — each plugin's own <see cref="DisposeAsync"/> is bounded by its
        /// <see cref="PluginManifest.ForegroundTimeoutMs"/> so a slow, non-throwing <c>DisposeAsync</c> cannot
        /// hang the caller indefinitely. That bound matters here specifically because <see cref="LoadPluginsAsync"/>
        /// runs at the start of every plugin-touching command, not only at process exit — an unbounded wait here
        /// would hang every such command, not just shutdown.
        /// </summary>
        private async Task DisposeCurrentGenerationAsync()
        {
            foreach (var plugin in _loadedPlugins)
            {
                // If a hook abandoned by an earlier timeout is still outstanding for this exact instance, give
                // it the same grace period to finish before disposing — narrows, though cannot fully eliminate,
                // the window where DisposeAsync runs concurrently with that hook on the same instance.
                if (_outstandingHooks.TryGetValue(plugin, out var outstandingHook))
                {
                    var graceTask = Task.Delay(plugin.Manifest.ForegroundTimeoutMs, CancellationToken.None);
                    if (await Task.WhenAny(outstandingHook, graceTask) == graceTask)
                    {
                        LogMessages.LogPluginError(_logger, null, $"{plugin.Manifest.Name}: disposing while a hook abandoned by an earlier timeout is still running; DisposeAsync may run concurrently with it");
                    }
                }

                try
                {
                    var disposeTask = plugin.Instance.DisposeAsync().AsTask();
                    var timeoutTask = Task.Delay(plugin.Manifest.ForegroundTimeoutMs, CancellationToken.None);
                    if (await Task.WhenAny(disposeTask, timeoutTask) == timeoutTask)
                    {
                        LogMessages.LogPluginError(_logger, null, $"{plugin.Manifest.Name}: DisposeAsync did not complete within {plugin.Manifest.ForegroundTimeoutMs}ms, abandoning it");
                        _ = disposeTask.ContinueWith(
                            t =>
                            {
                                if (t.IsFaulted)
                                {
                                    LogMessages.LogPluginError(_logger, t.Exception, $"{plugin.Manifest.Name}: abandoned DisposeAsync faulted");
                                }
                            },
                            CancellationToken.None,
                            TaskContinuationOptions.ExecuteSynchronously,
                            TaskScheduler.Default);
                    }
                }
                catch (Exception ex)
                {
                    LogMessages.LogPluginError(_logger, ex, $"{plugin.Manifest.Name}: DisposeAsync threw");
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
                    LogMessages.LogPluginError(_logger, ex, $"{plugin.Manifest.Name}: AssemblyLoadContext.Unload threw");
                }
            }

            _loadedPlugins.Clear();
            _initializedPlugins.Clear();
            _initFailedPlugins.Clear();
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
                !_initFailedPlugins.Contains(plugin.Instance));
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
                    // ShouldHandle throwing means this item was never even attempted - it must still be
                    // reflected in the summary. A bare `continue` here left every SyncSummary field at zero
                    // when this happened for every eligible ADR, so a fully-failed sweep looked identical to
                    // "nothing to do" (green PromptWriteSuccess banner, 0/0/0/0).
                    WritePermanentFailure(plugin.Manifest.Name!, $"ShouldHandle threw during backfill ({ex.Message}) (correlationId={context.CorrelationId})", ex);
                    summary.PermanentlyFailed++;
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
                    // can run for a long time (each plugin's ADRs are processed sequentially, with the full
                    // retryPolicy per item) and a cancelled item shouldn't erase every prior item's outcome.
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
        /// <see cref="PluginRetryPolicy.MaxAttempts"/> — shared by pending re-drive (<c>startAttempts</c>
        /// = the entry's prior attempt count) and backfill sweeps (<c>startAttempts</c> = <c>0</c>,
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
        /// <c>sync</c> run an entry has ever survived). <c>Fixed</c> is a flat <see cref="PluginRetryPolicy.DelayMs"/>;
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

        private async Task DispatchToPluginAsync(LoadedPlugin plugin, AdrEventContext context, string correlationId, string pendingStateRoot, CancellationToken cancellationToken)
        {
            var name = plugin.Manifest.Name!;

            // Initialization already happened in DispatchAsync's sequential phase before this method's caller
            // fanned out in parallel - EnsureInitializedAsync must never be called from here too.
            var outcome = await InvokeOnceAsync(plugin, context, plugin.Manifest.ForegroundTimeoutMs, cancellationToken);

            switch (outcome.Status)
            {
                case PluginInvokeStatus.Success:
                case PluginInvokeStatus.Skipped:
                    if (_logger.IsEnabled(LogLevel.Information))
                    {
                        var message = $"{name}: {outcome.Status} (correlationId={correlationId})";
                        LogMessages.LogPluginInfo(_logger, message);
                    }
                    return;
                default:
                    var attempts = outcome.WasTimeout ? 0 : 1;
                    await HandleFailureAsync(plugin, context, correlationId, outcome.ErrorMessage ?? string.Empty, outcome.IsRetryable, attempts, pendingStateRoot, cancellationToken);
                    return;
            }
        }

        /// <summary>
        /// Lazily runs <see cref="IAdrPlugin.InitializeAsync"/> once per plugin *instance*. Shared by
        /// the foreground dispatch path and the background retry engine so "already tried to initialize this
        /// instance" state (<see cref="_initializedPlugins"/>/<see cref="_initFailedPlugins"/>) is consistent
        /// between both callers. Keyed by <see cref="LoadedPlugin.Instance"/> reference, not by name — a reload
        /// (<see cref="LoadPluginsAsync"/>) always produces a genuinely new instance, which must be initialized
        /// again regardless of whether an earlier instance sharing the same manifest name already was.
        /// </summary>
        /// <returns><see langword="true"/> when the plugin is ready to be invoked; <see langword="false"/> when
        /// initialization already failed (permanently, for this instance) and the caller should skip it.</returns>
        private async Task<bool> EnsureInitializedAsync(LoadedPlugin plugin, CancellationToken cancellationToken)
        {
            var name = plugin.Manifest.Name!;

            if (_initializedPlugins.Contains(plugin.Instance))
            {
                return true;
            }
            if (_initFailedPlugins.Contains(plugin.Instance))
            {
                return false;
            }

            try
            {
                var pluginLogger = new HostPluginLogger(_logger);
                var pluginContext = new HostPluginContext(pluginLogger);
                var pluginConfig = new HostPluginConfiguration(plugin.Manifest.Settings);
                await plugin.Instance.InitializeAsync(pluginContext, pluginConfig, cancellationToken);
                _initializedPlugins.Add(plugin.Instance);
                return true;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _initFailedPlugins.Add(plugin.Instance);
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

            // Linked so the plugin's own token actually reflects the timeout, not just the ambient/user-cancel
            // token — previously the plugin was never told to stop when it timed out, only when the whole
            // process was cancelled, so a well-behaved plugin got no signal at all when abandoned.
            var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(timeoutMs);

            Task<PluginResult> hookTask;
            try
            {
                hookTask = plugin.Instance.OnAdrEventAsync(context, timeoutCts.Token);
            }
            catch (OperationCanceledException)
            {
                timeoutCts.Dispose();
                throw;
            }
            catch (Exception ex)
            {
                timeoutCts.Dispose();
                return PluginInvokeOutcome.Failed(ex.Message, isRetryable: true);
            }

            // Dispose the linked CTS only once the hook task actually finishes, however long that takes — an
            // abandoned (post-timeout) hook may still be running well after this method returns, and disposing
            // while it might still register a callback on its copy of the token risks ObjectDisposedException.
            _ = hookTask.ContinueWith(
                static (_, state) => ((CancellationTokenSource)state!).Dispose(),
                timeoutCts,
                CancellationToken.None,
                TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default);

            // The delay uses CancellationToken.None deliberately: if the user cancels the command, the delay must
            // not race to "elapsed" and get misread as a plugin timeout — cancellation is checked explicitly below.
            var delayTask = Task.Delay(timeoutMs, CancellationToken.None);
            var completed = await Task.WhenAny(hookTask, delayTask);

            cancellationToken.ThrowIfCancellationRequested();

            if (completed == delayTask)
            {
                // The losing hook task keeps running (now actually signalled to stop via timeoutCts, though a
                // plugin that ignores its token can still keep going regardless). Track it so a subsequent
                // dispose/reload can wait briefly for it instead of blindly racing DisposeAsync against it, and
                // observe its eventual fault so it never surfaces as an unobserved task exception later. Its
                // result is never used.
                _outstandingHooks[plugin] = hookTask;
                _ = hookTask.ContinueWith(
                    t =>
                    {
                        // Remove by key+value, not by key alone - if this same plugin instance times out
                        // again before this hook completes (e.g. MigrateCommandHandler's per-file dispatch
                        // loop hitting the same slow plugin repeatedly), a newer hook's tracking entry has
                        // already overwritten this one at the same key; removing by key alone would erase
                        // that newer, still-running entry too.
                        _outstandingHooks.TryRemove(new KeyValuePair<LoadedPlugin, Task>(plugin, hookTask));
                        if (t.IsFaulted)
                        {
                            LogMessages.LogPluginError(_logger, t.Exception, $"{name}: hook faulted after timeout ({timeoutMs}ms) elapsed (correlationId={context.CorrelationId})");
                        }
                    },
                    CancellationToken.None,
                    TaskContinuationOptions.ExecuteSynchronously,
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

        private async Task HandleFailureAsync(LoadedPlugin plugin, AdrEventContext context, string correlationId, string lastError, bool isRetryable, int attempts, string pendingStateRoot, CancellationToken cancellationToken)
        {
            var name = plugin.Manifest.Name!;

            if (!isRetryable)
            {
                WritePermanentFailure(name, $"{lastError} (correlationId={correlationId})");
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
            try
            {
                await PendingStateStore.UpsertAsync(_fileSystem, Path.Combine(pendingStateRoot, name), entry, cancellationToken, WriteWarning);
                WriteWarning(string.Format(null, FormatMessages.PluginQueuedForRetry, name) + $" (correlationId={correlationId})");
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // Critical fail-soft boundary: this runs on the foreground dispatch path used by every ADR
                // lifecycle command (approve/reject/new/etc.) — a failure persisting *that a plugin needs
                // retrying* must never itself propagate and turn a successful local ADR operation into a
                // command-level error/exit code 1.
                WriteWarning($"{name}: could not queue pending retry ({ex.Message}); this failure will not be automatically retried via 'adrplus sync'. (correlationId={correlationId})");
            }
        }

        /// <summary>
        /// Every immediate subfolder of <paramref name="root"/>, or empty when <paramref name="root"/> is blank
        /// or doesn't exist — either host-global root is optional.
        /// </summary>
        private string[] EnumerateRoot(string root) =>
            string.IsNullOrEmpty(root) || !_fileSystem.DirectoryExists(root)
                ? []
                : _fileSystem.GetDirectories(root);

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
