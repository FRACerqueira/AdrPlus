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
using AbstractionsDomain = AdrPlus.Abstractions.Domain;

namespace AdrPlus.Tests.Plugins;

/// <summary>
/// Unit tests for <see cref="PluginManager.BackfillAsync"/> — the Fase 6 full-repo sweep (<c>adrplus sync
/// --backfill</c>): per-item retry loop (always starting at attempt 1, exhaustion logged not persisted),
/// sequential-per-plugin dispatch (D18), and safe concurrent init across plugins.
/// </summary>
public class PluginManagerBackfillTests
{
    private readonly IFileSystemService _fileSystem = Substitute.For<IFileSystemService>();
    private readonly ILogger<PluginManager> _logger = Substitute.For<ILogger<PluginManager>>();
    private readonly IConsoleWriter _console = Substitute.For<IConsoleWriter>();

    private static PluginManifest CreateManifest(string name, int maxAttempts = 2, int delayMs = 1, int timeoutMs = 5000) => new()
    {
        Name = name,
        Version = "1.0.0",
        EntryAssembly = "Plugin.dll",
        EntryType = "Plugin.Type",
        AbstractionsVersion = "1.0.0",
        SubscribedEvents = ["Approved"],
        TimeoutMs = timeoutMs,
        RetryPolicy = new PluginRetryPolicy { MaxAttempts = maxAttempts, DelayMs = delayMs, Jitter = false, Backoff = "Fixed" }
    };

    private static AbstractionsDomain.AdrRecordSnapshot CreateAdrSnapshot(int number = 1) => new()
    {
        Number = number,
        Version = 1,
        Revision = null,
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
        Scopes = [],
        StatusMapping = new Dictionary<AbstractionsDomain.AdrStatus, string>()
    };

    private static (AdrEventType EventType, AbstractionsDomain.AdrRecordSnapshot Adr, string FilePath, Func<string> GetContent) Item(int number) =>
        (AdrEventType.Approved, CreateAdrSnapshot(number), $"/repo/adr/{number:D4}.md", () => $"content-{number}");

    private PluginManager CreateManager() =>
        new(_fileSystem, Options.Create(new AdrPlusConfig()), _logger, _console);

    [Fact]
    public async Task BackfillAsync_WhenHookSucceeds_CountsSucceededAndMarksContextAsReplay()
    {
        var plugin = Substitute.For<IAdrPlugin>();
        plugin.ShouldHandle(Arg.Any<AdrEventContext>()).Returns(true);
        AdrEventContext? capturedContext = null;
        plugin.OnAdrEventAsync(Arg.Do<AdrEventContext>(c => capturedContext = c), Arg.Any<CancellationToken>())
            .Returns(new PluginResult { Status = PluginResultStatus.Success });
        var manager = CreateManager();
        manager._loadedPlugins.Add(new LoadedPlugin(plugin, CreateManifest("p1"), "/repo/plugins/p1"));

        var summary = await manager.BackfillAsync([Item(1)], CreateRepoSnapshot(), TestContext.Current.CancellationToken);

        summary.Succeeded.Should().Be(1);
        capturedContext.Should().NotBeNull();
        capturedContext!.IsReplay.Should().BeTrue();
    }

    [Fact]
    public async Task BackfillAsync_WhenRetriesExhaustMaxAttempts_CountsExhaustedWithoutWritingPending()
    {
        var plugin = Substitute.For<IAdrPlugin>();
        plugin.ShouldHandle(Arg.Any<AdrEventContext>()).Returns(true);
        plugin.OnAdrEventAsync(Arg.Any<AdrEventContext>(), Arg.Any<CancellationToken>())
            .Returns(new PluginResult { Status = PluginResultStatus.Failed, Message = "boom", IsRetryable = true });
        var manager = CreateManager();
        manager._loadedPlugins.Add(new LoadedPlugin(plugin, CreateManifest("p1", maxAttempts: 2), "/repo/plugins/p1"));

        var summary = await manager.BackfillAsync([Item(1)], CreateRepoSnapshot(), TestContext.Current.CancellationToken);

        summary.Exhausted.Should().Be(1);
        await plugin.Received(2).OnAdrEventAsync(Arg.Any<AdrEventContext>(), Arg.Any<CancellationToken>());
        await _fileSystem.DidNotReceive().WriteAllTextAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task BackfillAsync_WhenHookReturnsPermanentFailed_CountsPermanentlyFailed()
    {
        var plugin = Substitute.For<IAdrPlugin>();
        plugin.ShouldHandle(Arg.Any<AdrEventContext>()).Returns(true);
        plugin.OnAdrEventAsync(Arg.Any<AdrEventContext>(), Arg.Any<CancellationToken>())
            .Returns(new PluginResult { Status = PluginResultStatus.Failed, Message = "bad credentials", IsRetryable = false });
        var manager = CreateManager();
        manager._loadedPlugins.Add(new LoadedPlugin(plugin, CreateManifest("p1"), "/repo/plugins/p1"));

        var summary = await manager.BackfillAsync([Item(1)], CreateRepoSnapshot(), TestContext.Current.CancellationToken);

        summary.PermanentlyFailed.Should().Be(1);
        _console.Received(1).PromptWriteError(Arg.Is<string>(s => s.Contains("p1")));
    }

    [Fact]
    public async Task BackfillAsync_WhenShouldHandleFalse_CountsSkippedWithoutAttempt()
    {
        var plugin = Substitute.For<IAdrPlugin>();
        plugin.ShouldHandle(Arg.Any<AdrEventContext>()).Returns(false);
        var manager = CreateManager();
        manager._loadedPlugins.Add(new LoadedPlugin(plugin, CreateManifest("p1"), "/repo/plugins/p1"));

        var summary = await manager.BackfillAsync([Item(1)], CreateRepoSnapshot(), TestContext.Current.CancellationToken);

        summary.Skipped.Should().Be(1);
        await plugin.DidNotReceive().OnAdrEventAsync(Arg.Any<AdrEventContext>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task BackfillAsync_WhenPluginNotSubscribedToEvent_IsNeverInvoked()
    {
        var plugin = Substitute.For<IAdrPlugin>();
        var manager = CreateManager();
        var manifest = CreateManifest("p1");
        manifest.SubscribedEvents = ["Rejected"];
        manager._loadedPlugins.Add(new LoadedPlugin(plugin, manifest, "/repo/plugins/p1"));

        var summary = await manager.BackfillAsync([Item(1)], CreateRepoSnapshot(), TestContext.Current.CancellationToken);

        await plugin.DidNotReceive().OnAdrEventAsync(Arg.Any<AdrEventContext>(), Arg.Any<CancellationToken>());
        summary.Succeeded.Should().Be(0);
        summary.Skipped.Should().Be(0);
    }

    [Fact]
    public async Task BackfillAsync_WhenShouldHandleThrowsForOneItem_SkipsOnlyThatItemAndContinues()
    {
        var plugin = Substitute.For<IAdrPlugin>();
        plugin.ShouldHandle(Arg.Any<AdrEventContext>()).Returns(callInfo =>
        {
            var context = callInfo.Arg<AdrEventContext>();
            if (context.Adr.Number == 1)
            {
                throw new InvalidOperationException("boom");
            }
            return true;
        });
        plugin.OnAdrEventAsync(Arg.Any<AdrEventContext>(), Arg.Any<CancellationToken>())
            .Returns(new PluginResult { Status = PluginResultStatus.Success });
        var manager = CreateManager();
        manager._loadedPlugins.Add(new LoadedPlugin(plugin, CreateManifest("p1"), "/repo/plugins/p1"));

        var summary = await manager.BackfillAsync([Item(1), Item(2)], CreateRepoSnapshot(), TestContext.Current.CancellationToken);

        summary.Succeeded.Should().Be(1);
        await plugin.Received(1).OnAdrEventAsync(Arg.Any<AdrEventContext>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task BackfillAsync_WithMultiplePlugins_InitializesAllSafelyAndSweepsIndependently()
    {
        var plugins = Enumerable.Range(1, 5).Select(i =>
        {
            var plugin = Substitute.For<IAdrPlugin>();
            plugin.ShouldHandle(Arg.Any<AdrEventContext>()).Returns(true);
            plugin.OnAdrEventAsync(Arg.Any<AdrEventContext>(), Arg.Any<CancellationToken>())
                .Returns(new PluginResult { Status = PluginResultStatus.Success });
            return (Instance: plugin, Loaded: new LoadedPlugin(plugin, CreateManifest($"p{i}"), $"/repo/plugins/p{i}"));
        }).ToList();

        var manager = CreateManager();
        foreach (var (_, loaded) in plugins)
        {
            manager._loadedPlugins.Add(loaded);
        }

        var summary = await manager.BackfillAsync([Item(1)], CreateRepoSnapshot(), TestContext.Current.CancellationToken);

        summary.Succeeded.Should().Be(5);
        foreach (var (instance, _) in plugins)
        {
            await instance.Received(1).InitializeAsync(Arg.Any<IPluginContext>(), Arg.Any<IPluginConfiguration>(), Arg.Any<CancellationToken>());
        }
    }

    [Fact]
    public async Task BackfillAsync_WithinOnePlugin_ProcessesTwoAdrsSequentiallyNotConcurrently()
    {
        var plugin = Substitute.For<IAdrPlugin>();
        plugin.ShouldHandle(Arg.Any<AdrEventContext>()).Returns(true);
        var concurrentCalls = 0;
        var maxObservedConcurrency = 0;
        plugin.OnAdrEventAsync(Arg.Any<AdrEventContext>(), Arg.Any<CancellationToken>())
            .Returns(async _ =>
            {
                var current = Interlocked.Increment(ref concurrentCalls);
                maxObservedConcurrency = Math.Max(maxObservedConcurrency, current);
                await Task.Delay(20, TestContext.Current.CancellationToken);
                Interlocked.Decrement(ref concurrentCalls);
                return new PluginResult { Status = PluginResultStatus.Success };
            });
        var manager = CreateManager();
        manager._loadedPlugins.Add(new LoadedPlugin(plugin, CreateManifest("p1"), "/repo/plugins/p1"));

        var summary = await manager.BackfillAsync([Item(1), Item(2)], CreateRepoSnapshot(), TestContext.Current.CancellationToken);

        summary.Succeeded.Should().Be(2);
        maxObservedConcurrency.Should().Be(1);
    }

    [Fact]
    public async Task BackfillAsync_WhenCancelledDuringBackoff_ReturnsPartialSummaryWithoutThrowing()
    {
        var plugin = Substitute.For<IAdrPlugin>();
        plugin.ShouldHandle(Arg.Any<AdrEventContext>()).Returns(true);
        plugin.OnAdrEventAsync(Arg.Any<AdrEventContext>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var context = callInfo.Arg<AdrEventContext>();
                if (context.Adr.Number == 1)
                {
                    return Task.FromResult(new PluginResult { Status = PluginResultStatus.Success });
                }
                // Item 2 always fails retryably, forcing a real backoff delay before the 2nd attempt.
                return Task.FromResult(new PluginResult { Status = PluginResultStatus.Failed, Message = "boom", IsRetryable = true });
            });
        var manager = CreateManager();
        manager._loadedPlugins.Add(new LoadedPlugin(plugin, CreateManifest("p1", maxAttempts: 2, delayMs: 200), "/repo/plugins/p1"));

        using var cts = new CancellationTokenSource();
        cts.CancelAfter(50);

        var summary = await manager.BackfillAsync([Item(1), Item(2)], CreateRepoSnapshot(), cts.Token);

        summary.Succeeded.Should().Be(1);
        summary.Exhausted.Should().Be(0);
        summary.PermanentlyFailed.Should().Be(0);
    }
}
