// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the AdrPlus project under MIT license
// ***************************************************************************************

using AdrPlus.Abstractions;
using AdrPlus.Domain;
using AdrPlus.Infrastructure.FileSystem;
using AdrPlus.Infrastructure.UI;
using AdrPlus.Plugins;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute.ExceptionExtensions;
using System.Text.Json;
using AbstractionsDomain = AdrPlus.Abstractions.Domain;

namespace AdrPlus.Tests.Plugins;

/// <summary>
/// Unit tests for <see cref="PluginManager.DispatchAsync"/>: event filtering, lazy once-per-process
/// <c>InitializeAsync</c>, the per-plugin foreground timeout race, and outcome handling
/// (success/skip, retryable failure -> pending.json, permanent failure -> no pending.json).
/// </summary>
/// <remarks>
/// Plugins are seeded directly into <see cref="PluginManager._loadedPlugins"/> (internal, test-visible) rather
/// than through <see cref="PluginManager.LoadPluginsAsync"/>, which requires a real compiled assembly
/// (deferred to the reference fixture plugin) — dispatch logic itself doesn't depend on how a plugin got loaded.
/// </remarks>
public class PluginManagerDispatchTests
{
    private const string FolderPath = "/repo/plugins/test-plugin";
    private readonly IFileSystemService _fileSystem = Substitute.For<IFileSystemService>();
    private readonly ILogger<PluginManager> _logger = Substitute.For<ILogger<PluginManager>>();
    private readonly IConsoleWriter _console = Substitute.For<IConsoleWriter>();

    private static PluginManifest CreateManifest(string name, IEnumerable<string> subscribedEvents, int foregroundTimeoutMs = 5000) => new()
    {
        Name = name,
        Version = "1.0.0",
        EntryAssembly = "Plugin.dll",
        EntryType = "Plugin.Type",
        AbstractionsVersion = "1.0.0",
        SubscribedEvents = [.. subscribedEvents],
        ForegroundTimeoutMs = foregroundTimeoutMs
    };

    private static AbstractionsDomain.AdrRecordSnapshot CreateAdrSnapshot(int number = 1, int version = 1, int? revision = null) => new()
    {
        Number = number,
        Version = version,
        Revision = revision,
        Title = "Test decision",
        Domain = "Domain",
        Scope = "Scope",
        StatusCreate = AbstractionsDomain.AdrStatus.Accepted,
        StatusUpdate = AbstractionsDomain.AdrStatus.Accepted,
        StatusChange = AbstractionsDomain.AdrStatus.Unknown
    };

    private static AbstractionsDomain.RepoInfoSnapshot CreateRepoSnapshot() => new()
    {
        FolderAdr = "docs/adr",
        StatusMapping = new Dictionary<AbstractionsDomain.AdrStatus, string>()
    };

    private static LoadedPlugin CreateLoadedPlugin(IAdrPlugin instance, PluginManifest manifest) =>
        new(instance, manifest, FolderPath);

    private PluginManager CreateManager() =>
        new(_fileSystem, Options.Create(new AdrPlusConfig()), _logger, _console);

    [Fact]
    public async Task DispatchAsync_WithPluginNotSubscribedToEvent_DoesNotInvokeHook()
    {
        var plugin = Substitute.For<IAdrPlugin>();
        var manager = CreateManager();
        manager._loadedPlugins.Add(CreateLoadedPlugin(plugin, CreateManifest("p1", ["Approved"])));

        await manager.DispatchAsync(AdrEventType.Rejected, CreateAdrSnapshot(), "/repo/adr/0001.md", () => "content", CreateRepoSnapshot(), "/repo/plugins-state", isReplay: false, isActive: null, cancellationToken: TestContext.Current.CancellationToken);

        await plugin.DidNotReceive().OnAdrEventAsync(Arg.Any<AdrEventContext>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DispatchAsync_WithShouldHandleFalse_DoesNotInvokeHook()
    {
        var plugin = Substitute.For<IAdrPlugin>();
        plugin.ShouldHandle(Arg.Any<AdrEventContext>()).Returns(false);
        var manager = CreateManager();
        manager._loadedPlugins.Add(CreateLoadedPlugin(plugin, CreateManifest("p1", ["Approved"])));

        await manager.DispatchAsync(AdrEventType.Approved, CreateAdrSnapshot(), "/repo/adr/0001.md", () => "content", CreateRepoSnapshot(), "/repo/plugins-state", isReplay: false, isActive: null, cancellationToken: TestContext.Current.CancellationToken);

        await plugin.DidNotReceive().OnAdrEventAsync(Arg.Any<AdrEventContext>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DispatchAsync_CalledForTwoEvents_InitializesPluginOnlyOnce()
    {
        // Simulates MigrateCommandHandler's per-file dispatch loop: one process, many events.
        var plugin = Substitute.For<IAdrPlugin>();
        plugin.ShouldHandle(Arg.Any<AdrEventContext>()).Returns(true);
        plugin.OnAdrEventAsync(Arg.Any<AdrEventContext>(), Arg.Any<CancellationToken>())
            .Returns(new PluginResult { Status = PluginResultStatus.Success });
        var manager = CreateManager();
        manager._loadedPlugins.Add(CreateLoadedPlugin(plugin, CreateManifest("p1", ["Migrated"])));

        await manager.DispatchAsync(AdrEventType.Migrated, CreateAdrSnapshot(1), "/repo/adr/0001.md", () => "c1", CreateRepoSnapshot(), "/repo/plugins-state", isReplay: false, isActive: null, cancellationToken: TestContext.Current.CancellationToken);
        await manager.DispatchAsync(AdrEventType.Migrated, CreateAdrSnapshot(2), "/repo/adr/0002.md", () => "c2", CreateRepoSnapshot(), "/repo/plugins-state", isReplay: false, isActive: null, cancellationToken: TestContext.Current.CancellationToken);

        await plugin.Received(1).InitializeAsync(Arg.Any<IPluginContext>(), Arg.Any<IPluginConfiguration>(), Arg.Any<CancellationToken>());
        await plugin.Received(2).OnAdrEventAsync(Arg.Any<AdrEventContext>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DispatchAsync_WhenInitializeAsyncThrows_SkipsSubsequentEventsWithoutPending()
    {
        var plugin = Substitute.For<IAdrPlugin>();
        plugin.InitializeAsync(Arg.Any<IPluginContext>(), Arg.Any<IPluginConfiguration>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("missing credentials"));
        plugin.ShouldHandle(Arg.Any<AdrEventContext>()).Returns(true);
        var manager = CreateManager();
        manager._loadedPlugins.Add(CreateLoadedPlugin(plugin, CreateManifest("p1", ["Approved"])));

        await manager.DispatchAsync(AdrEventType.Approved, CreateAdrSnapshot(1), "/repo/adr/0001.md", () => "c1", CreateRepoSnapshot(), "/repo/plugins-state", isReplay: false, isActive: null, cancellationToken: TestContext.Current.CancellationToken);
        await manager.DispatchAsync(AdrEventType.Approved, CreateAdrSnapshot(2), "/repo/adr/0002.md", () => "c2", CreateRepoSnapshot(), "/repo/plugins-state", isReplay: false, isActive: null, cancellationToken: TestContext.Current.CancellationToken);

        await plugin.Received(1).InitializeAsync(Arg.Any<IPluginContext>(), Arg.Any<IPluginConfiguration>(), Arg.Any<CancellationToken>());
        await plugin.DidNotReceive().OnAdrEventAsync(Arg.Any<AdrEventContext>(), Arg.Any<CancellationToken>());
        await _fileSystem.DidNotReceive().WriteAllTextAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        _console.Received(1).PromptWriteError(Arg.Is<string>(s => s.Contains("p1")));
    }

    [Fact]
    public async Task DispatchAsync_WhenHookNeverReturns_TimesOutAndWritesPendingWithZeroAttempts()
    {
        var plugin = Substitute.For<IAdrPlugin>();
        plugin.ShouldHandle(Arg.Any<AdrEventContext>()).Returns(true);
        var neverCompletes = new TaskCompletionSource<PluginResult>();
        plugin.OnAdrEventAsync(Arg.Any<AdrEventContext>(), Arg.Any<CancellationToken>()).Returns(neverCompletes.Task);
        var manager = CreateManager();
        manager._loadedPlugins.Add(CreateLoadedPlugin(plugin, CreateManifest("p1", ["Approved"], foregroundTimeoutMs: 30)));

        string? writtenJson = null;
        _fileSystem.WriteAllTextAsync(Arg.Any<string>(), Arg.Do<string>(j => writtenJson = j), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        await manager.DispatchAsync(AdrEventType.Approved, CreateAdrSnapshot(), "/repo/adr/0001.md", () => "content", CreateRepoSnapshot(), "/repo/plugins-state", isReplay: false, isActive: null, cancellationToken: TestContext.Current.CancellationToken);

        writtenJson.Should().NotBeNull();
        var entries = JsonSerializer.Deserialize<List<PendingEntry>>(writtenJson!, PluginManifest.SerializerOptions);
        entries.Should().ContainSingle(e => e.Attempts == 0 && e.EventType == "Approved");
    }

    [Fact]
    public async Task DispatchAsync_WhenHookReturnsRetryableFailed_WritesPendingWithOneAttempt()
    {
        var plugin = Substitute.For<IAdrPlugin>();
        plugin.ShouldHandle(Arg.Any<AdrEventContext>()).Returns(true);
        plugin.OnAdrEventAsync(Arg.Any<AdrEventContext>(), Arg.Any<CancellationToken>())
            .Returns(new PluginResult { Status = PluginResultStatus.Failed, Message = "boom", IsRetryable = true });
        var manager = CreateManager();
        manager._loadedPlugins.Add(CreateLoadedPlugin(plugin, CreateManifest("p1", ["Approved"])));

        string? writtenJson = null;
        _fileSystem.WriteAllTextAsync(Arg.Any<string>(), Arg.Do<string>(j => writtenJson = j), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        await manager.DispatchAsync(AdrEventType.Approved, CreateAdrSnapshot(), "/repo/adr/0001.md", () => "content", CreateRepoSnapshot(), "/repo/plugins-state", isReplay: false, isActive: null, cancellationToken: TestContext.Current.CancellationToken);

        var entries = JsonSerializer.Deserialize<List<PendingEntry>>(writtenJson!, PluginManifest.SerializerOptions);
        entries.Should().ContainSingle(e => e.Attempts == 1 && e.LastError == "boom");
    }

    [Fact]
    public async Task DispatchAsync_WhenHookReturnsPermanentFailed_DoesNotWritePending()
    {
        var plugin = Substitute.For<IAdrPlugin>();
        plugin.ShouldHandle(Arg.Any<AdrEventContext>()).Returns(true);
        plugin.OnAdrEventAsync(Arg.Any<AdrEventContext>(), Arg.Any<CancellationToken>())
            .Returns(new PluginResult { Status = PluginResultStatus.Failed, Message = "bad credentials", IsRetryable = false });
        var manager = CreateManager();
        manager._loadedPlugins.Add(CreateLoadedPlugin(plugin, CreateManifest("p1", ["Approved"])));

        await manager.DispatchAsync(AdrEventType.Approved, CreateAdrSnapshot(), "/repo/adr/0001.md", () => "content", CreateRepoSnapshot(), "/repo/plugins-state", isReplay: false, isActive: null, cancellationToken: TestContext.Current.CancellationToken);

        await _fileSystem.DidNotReceive().WriteAllTextAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        _console.Received(1).PromptWriteError(Arg.Is<string>(s => s.Contains("p1")));
    }

    [Fact]
    public async Task DispatchAsync_WhenTwoDifferentAdrsFailForSamePlugin_PendingAccumulatesBoth()
    {
        var plugin = Substitute.For<IAdrPlugin>();
        plugin.ShouldHandle(Arg.Any<AdrEventContext>()).Returns(true);
        plugin.OnAdrEventAsync(Arg.Any<AdrEventContext>(), Arg.Any<CancellationToken>())
            .Returns(new PluginResult { Status = PluginResultStatus.Failed, Message = "boom", IsRetryable = true });
        var manager = CreateManager();
        manager._loadedPlugins.Add(CreateLoadedPlugin(plugin, CreateManifest("p1", ["Approved"])));

        string? storedJson = null;
        _fileSystem.FileExists(Arg.Any<string>()).Returns(_ => storedJson != null);
        _fileSystem.ReadAllTextAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(_ => storedJson!);
        _fileSystem.WriteAllTextAsync(Arg.Any<string>(), Arg.Do<string>(j => storedJson = j), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        await manager.DispatchAsync(AdrEventType.Approved, CreateAdrSnapshot(number: 1), "/repo/adr/0001.md", () => "c1", CreateRepoSnapshot(), "/repo/plugins-state", isReplay: false, isActive: null, cancellationToken: TestContext.Current.CancellationToken);
        await manager.DispatchAsync(AdrEventType.Approved, CreateAdrSnapshot(number: 2), "/repo/adr/0002.md", () => "c2", CreateRepoSnapshot(), "/repo/plugins-state", isReplay: false, isActive: null, cancellationToken: TestContext.Current.CancellationToken);

        var entries = JsonSerializer.Deserialize<List<PendingEntry>>(storedJson!, PluginManifest.SerializerOptions);
        entries.Should().HaveCount(2);
        entries!.Select(e => e.AdrKey).Should().Contain(["0001-v1-r0", "0002-v1-r0"]);
    }

    [Fact]
    public async Task DispatchAsync_TwoReposWithSamePluginName_WritePendingToIndependentStateFolders()
    {
        // Regression: plugin binaries are host-global and shared across repos, but pending.json must
        // stay per-repo. If pendingStateRoot were ever ignored (e.g. a future refactor derived the state path
        // from the shared plugin.FolderPath again), this test would start seeing writes collide on one path
        // instead of two.
        var plugin = Substitute.For<IAdrPlugin>();
        plugin.ShouldHandle(Arg.Any<AdrEventContext>()).Returns(true);
        plugin.OnAdrEventAsync(Arg.Any<AdrEventContext>(), Arg.Any<CancellationToken>())
            .Returns(new PluginResult { Status = PluginResultStatus.Failed, Message = "boom", IsRetryable = true });
        var manager = CreateManager();
        manager._loadedPlugins.Add(CreateLoadedPlugin(plugin, CreateManifest("p1", ["Approved"])));

        var writtenPaths = new List<string>();
        _fileSystem.WriteAllTextAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                writtenPaths.Add(callInfo.ArgAt<string>(0));
                return Task.CompletedTask;
            });

        await manager.DispatchAsync(AdrEventType.Approved, CreateAdrSnapshot(1), "/repoA/adr/0001.md", () => "c1", CreateRepoSnapshot(), "/repoA/plugins-state", isReplay: false, isActive: null, cancellationToken: TestContext.Current.CancellationToken);
        await manager.DispatchAsync(AdrEventType.Approved, CreateAdrSnapshot(1), "/repoB/adr/0001.md", () => "c1", CreateRepoSnapshot(), "/repoB/plugins-state", isReplay: false, isActive: null, cancellationToken: TestContext.Current.CancellationToken);

        writtenPaths.Should().HaveCount(2);
        writtenPaths.Should().OnlyHaveUniqueItems();
        writtenPaths.Should().Contain(p => p.Contains("repoA") && p.Contains("p1"));
        writtenPaths.Should().Contain(p => p.Contains("repoB") && p.Contains("p1"));
    }

    [Fact]
    public async Task DispatchAsync_WhenHookTimesOut_CancelsTheTokenPassedToThePlugin()
    {
        // Previously the plugin only ever received the ambient/user-cancel token, never one derived from the
        // manifest's own timeout — so a well-behaved plugin was never actually told to stop when abandoned.
        var plugin = Substitute.For<IAdrPlugin>();
        plugin.ShouldHandle(Arg.Any<AdrEventContext>()).Returns(true);
        // The manifest's ForegroundTimeoutMs and the linked CTS's CancelAfter(timeoutMs) are independent
        // timers of the same duration — there's no guarantee CancelAfter has already fired the instant
        // DispatchAsync returns, so wait on the token's own cancellation signal instead of racing timers.
        var cancelledSignal = new TaskCompletionSource();
        plugin.OnAdrEventAsync(Arg.Any<AdrEventContext>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                callInfo.Arg<CancellationToken>().Register(() => cancelledSignal.TrySetResult());
                return new TaskCompletionSource<PluginResult>().Task;
            });
        var manager = CreateManager();
        manager._loadedPlugins.Add(CreateLoadedPlugin(plugin, CreateManifest("p1", ["Approved"], foregroundTimeoutMs: 30)));

        await manager.DispatchAsync(AdrEventType.Approved, CreateAdrSnapshot(), "/repo/adr/0001.md", () => "content", CreateRepoSnapshot(), "/repo/plugins-state", isReplay: false, isActive: null, cancellationToken: TestContext.Current.CancellationToken);
        var completed = await Task.WhenAny(cancelledSignal.Task, Task.Delay(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken));

        completed.Should().BeSameAs(cancelledSignal.Task);
    }

    [Fact]
    public async Task DispatchAsync_WhenPersistingPendingStateFails_DoesNotPropagateException()
    {
        // Critical fail-soft boundary: this runs on the foreground path used by every ADR lifecycle command, so
        // a failure to persist "this plugin needs retrying" must never itself turn a successful local ADR
        // operation into a command-level error/exit code 1.
        var plugin = Substitute.For<IAdrPlugin>();
        plugin.ShouldHandle(Arg.Any<AdrEventContext>()).Returns(true);
        plugin.OnAdrEventAsync(Arg.Any<AdrEventContext>(), Arg.Any<CancellationToken>())
            .Returns(new PluginResult { Status = PluginResultStatus.Failed, Message = "boom", IsRetryable = true });
        var manager = CreateManager();
        manager._loadedPlugins.Add(CreateLoadedPlugin(plugin, CreateManifest("p1", ["Approved"])));
        _fileSystem.WriteAllTextAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new IOException("disk full"));

        var act = () => manager.DispatchAsync(AdrEventType.Approved, CreateAdrSnapshot(), "/repo/adr/0001.md", () => "content", CreateRepoSnapshot(), "/repo/plugins-state", isReplay: false, isActive: null, cancellationToken: TestContext.Current.CancellationToken);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task LoadPluginsAsync_CalledAgainAfterDispatch_DisposesPreviousInstanceAndReinitializesNewOne()
    {
        // Regression: a same-process reload (e.g. the interactive wizard looping after a config change)
        // previously reused per-name init bookkeeping, so a genuinely new instance skipped InitializeAsync
        // and the previous generation's instance was never disposed.
        var manifest = CreateManifest("p1", ["Approved"]);
        var firstInstance = Substitute.For<IAdrPlugin>();
        firstInstance.ShouldHandle(Arg.Any<AdrEventContext>()).Returns(true);
        firstInstance.OnAdrEventAsync(Arg.Any<AdrEventContext>(), Arg.Any<CancellationToken>())
            .Returns(new PluginResult { Status = PluginResultStatus.Success });
        var manager = CreateManager();
        manager._loadedPlugins.Add(CreateLoadedPlugin(firstInstance, manifest));

        await manager.DispatchAsync(AdrEventType.Approved, CreateAdrSnapshot(1), "/repo/adr/0001.md", () => "c1", CreateRepoSnapshot(), "/repo/plugins-state", isReplay: false, isActive: null, cancellationToken: TestContext.Current.CancellationToken);
        await firstInstance.Received(1).InitializeAsync(Arg.Any<IPluginContext>(), Arg.Any<IPluginConfiguration>(), Arg.Any<CancellationToken>());

        // No builtin/user plugin roots are configured on this fixture, so discovery finds nothing new — but the
        // previous generation must still be disposed and its init bookkeeping cleared.
        await manager.LoadPluginsAsync(TestContext.Current.CancellationToken);

        await firstInstance.Received(1).DisposeAsync();
        manager.LoadedPlugins.Should().BeEmpty();

        var secondInstance = Substitute.For<IAdrPlugin>();
        secondInstance.ShouldHandle(Arg.Any<AdrEventContext>()).Returns(true);
        secondInstance.OnAdrEventAsync(Arg.Any<AdrEventContext>(), Arg.Any<CancellationToken>())
            .Returns(new PluginResult { Status = PluginResultStatus.Success });
        manager._loadedPlugins.Add(CreateLoadedPlugin(secondInstance, manifest));

        await manager.DispatchAsync(AdrEventType.Approved, CreateAdrSnapshot(2), "/repo/adr/0002.md", () => "c2", CreateRepoSnapshot(), "/repo/plugins-state", isReplay: false, isActive: null, cancellationToken: TestContext.Current.CancellationToken);

        await secondInstance.Received(1).InitializeAsync(Arg.Any<IPluginContext>(), Arg.Any<IPluginConfiguration>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DispatchAsync_WithManyNeverInitializedPluginsOnSameEvent_InitializesEachExactlyOnceWithoutCorruption()
    {
        // Regression: EnsureInitializedAsync used to run inside DispatchAsync's parallel Task.WhenAll
        // fan-out, doing unsynchronized Contains/Add on the shared _initializedPlugins/_initFailedPlugins
        // HashSets from multiple threads at once - corrupting them (a disposable audit probe reproduced both
        // an uncaught InvalidOperationException from inside HashSet.Add, and a silently-dropped plugin never
        // invoked at all, across 3/3 target frameworks). Task.Yield widens the interleaving window.
        const int pluginCount = 50;
        var manager = CreateManager();
        var plugins = new List<IAdrPlugin>();
        for (var i = 0; i < pluginCount; i++)
        {
            var plugin = Substitute.For<IAdrPlugin>();
            plugin.ShouldHandle(Arg.Any<AdrEventContext>()).Returns(true);
            plugin.InitializeAsync(Arg.Any<IPluginContext>(), Arg.Any<IPluginConfiguration>(), Arg.Any<CancellationToken>())
                .Returns(async _ => { await Task.Yield(); });
            plugin.OnAdrEventAsync(Arg.Any<AdrEventContext>(), Arg.Any<CancellationToken>())
                .Returns(new PluginResult { Status = PluginResultStatus.Success });
            plugins.Add(plugin);
            manager._loadedPlugins.Add(CreateLoadedPlugin(plugin, CreateManifest($"p{i}", ["Approved"])));
        }

        await manager.DispatchAsync(AdrEventType.Approved, CreateAdrSnapshot(), "/repo/adr/0001.md", () => "content", CreateRepoSnapshot(), "/repo/plugins-state", isReplay: false, isActive: null, cancellationToken: TestContext.Current.CancellationToken);

        foreach (var plugin in plugins)
        {
            await plugin.Received(1).InitializeAsync(Arg.Any<IPluginContext>(), Arg.Any<IPluginConfiguration>(), Arg.Any<CancellationToken>());
            await plugin.Received(1).OnAdrEventAsync(Arg.Any<AdrEventContext>(), Arg.Any<CancellationToken>());
        }
    }

    [Fact]
    public async Task DispatchAsync_WhenShouldHandleThrows_WarnsOnConsoleNotJustLog()
    {
        // Regression: previously log-only, unlike every other plugin failure path in this class.
        var plugin = Substitute.For<IAdrPlugin>();
        plugin.ShouldHandle(Arg.Any<AdrEventContext>()).Returns(_ => throw new InvalidOperationException("boom"));
        var manager = CreateManager();
        manager._loadedPlugins.Add(CreateLoadedPlugin(plugin, CreateManifest("p1", ["Approved"])));

        await manager.DispatchAsync(AdrEventType.Approved, CreateAdrSnapshot(), "/repo/adr/0001.md", () => "content", CreateRepoSnapshot(), "/repo/plugins-state", isReplay: false, isActive: null, cancellationToken: TestContext.Current.CancellationToken);

        _console.Received(1).PromptWriteInfo(Arg.Is<string>(s => s.Contains("p1") && s.Contains("correlationId=")));
        await plugin.DidNotReceive().OnAdrEventAsync(Arg.Any<AdrEventContext>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DispatchAsync_WhenCancelledDuringHook_PropagatesWithoutWritingPending()
    {
        var plugin = Substitute.For<IAdrPlugin>();
        plugin.ShouldHandle(Arg.Any<AdrEventContext>()).Returns(true);
        var hookCompletion = new TaskCompletionSource<PluginResult>();
        plugin.OnAdrEventAsync(Arg.Any<AdrEventContext>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var ct = callInfo.Arg<CancellationToken>();
                ct.Register(() => hookCompletion.TrySetCanceled(ct));
                return hookCompletion.Task;
            });
        var manager = CreateManager();
        manager._loadedPlugins.Add(CreateLoadedPlugin(plugin, CreateManifest("p1", ["Approved"], foregroundTimeoutMs: 30_000)));

        using var cts = new CancellationTokenSource();
        var dispatchTask = manager.DispatchAsync(AdrEventType.Approved, CreateAdrSnapshot(), "/repo/adr/0001.md", () => "content", CreateRepoSnapshot(), "/repo/plugins-state", isReplay: false, isActive: null, cancellationToken: cts.Token);
        cts.Cancel();

        await FluentActions.Awaiting(() => dispatchTask).Should().ThrowAsync<OperationCanceledException>();
        await _fileSystem.DidNotReceive().WriteAllTextAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}
