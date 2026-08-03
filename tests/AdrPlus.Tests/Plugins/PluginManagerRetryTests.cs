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
/// Unit tests for <see cref="PluginManager.RetryPendingAsync"/> — the Fase 5 background re-drive engine
/// (<c>adrplus sync</c>'s default mode): per-entry retry loop, minimum-one-attempt-per-run guarantee,
/// <c>ShouldHandle</c> re-evaluation, dropped/skipped/permanently-failed bookkeeping.
/// </summary>
/// <remarks>
/// Same seeding approach as <see cref="PluginManagerDispatchTests"/>: plugins are added directly to
/// <see cref="PluginManager._loadedPlugins"/>. Every fixture manifest sets <c>Jitter = false</c> and a tiny
/// <c>DelayMs</c> so retry-loop tests stay deterministic and fast — never rely on "small" being good enough.
/// </remarks>
public class PluginManagerRetryTests
{
    private const string FolderPath = "/repo/plugins/test-plugin";
    private readonly IFileSystemService _fileSystem = Substitute.For<IFileSystemService>();
    private readonly ILogger<PluginManager> _logger = Substitute.For<ILogger<PluginManager>>();
    private readonly IConsoleWriter _console = Substitute.For<IConsoleWriter>();

    private static PluginManifest CreateManifest(string name, int maxAttempts = 3, int timeoutMs = 5000) => new()
    {
        Name = name,
        Version = "1.0.0",
        EntryAssembly = "Plugin.dll",
        EntryType = "Plugin.Type",
        AbstractionsVersion = "1.0.0",
        SubscribedEvents = ["Approved"],
        BackgroundTimeoutMs = timeoutMs,
        RetryPolicy = new PluginRetryPolicy { MaxAttempts = maxAttempts, DelayMs = 1, Jitter = false, Backoff = "Fixed" }
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

    private static LoadedPlugin CreateLoadedPlugin(IAdrPlugin instance, PluginManifest manifest) =>
        new(instance, manifest, FolderPath);

    private PluginManager CreateManager() =>
        new(_fileSystem, Options.Create(new AdrPlusConfig()), _logger, _console);

    private static Func<string, (AbstractionsDomain.AdrRecordSnapshot Adr, string FilePath, string Content)?> ResolverFor(string adrKey, int number = 1) =>
        key => key == adrKey ? (CreateAdrSnapshot(number), $"/repo/adr/{number:D4}.md", "content") : null;

    private void SeedPending(PendingEntry entry)
    {
        var json = JsonSerializer.Serialize(new List<PendingEntry> { entry }, PluginManifest.SerializerOptions);
        _fileSystem.FileExists(Arg.Any<string>()).Returns(true);
        _fileSystem.ReadAllTextAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(json);
    }

    [Fact]
    public async Task RetryPendingAsync_WhenHookSucceedsOnFirstAttempt_RemovesEntry()
    {
        var plugin = Substitute.For<IAdrPlugin>();
        plugin.ShouldHandle(Arg.Any<AdrEventContext>()).Returns(true);
        plugin.OnAdrEventAsync(Arg.Any<AdrEventContext>(), Arg.Any<CancellationToken>())
            .Returns(new PluginResult { Status = PluginResultStatus.Success });
        var manager = CreateManager();
        manager._loadedPlugins.Add(CreateLoadedPlugin(plugin, CreateManifest("p1")));
        SeedPending(new PendingEntry { AdrKey = "0001-v1-r0", EventType = "Approved", Attempts = 0 });

        List<PendingEntry>? written = null;
        _fileSystem.WriteAllTextAsync(Arg.Any<string>(), Arg.Do<string>(j => written = JsonSerializer.Deserialize<List<PendingEntry>>(j, PluginManifest.SerializerOptions)), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var summary = await manager.RetryPendingAsync(ResolverFor("0001-v1-r0"), CreateRepoSnapshot(), isActive: null, cancellationToken: TestContext.Current.CancellationToken);

        summary.Succeeded.Should().Be(1);
        written.Should().BeEmpty();
    }

    [Fact]
    public async Task RetryPendingAsync_WhenRetryableFailureExhaustsMaxAttempts_KeepsEntryUpdated()
    {
        var plugin = Substitute.For<IAdrPlugin>();
        plugin.ShouldHandle(Arg.Any<AdrEventContext>()).Returns(true);
        plugin.OnAdrEventAsync(Arg.Any<AdrEventContext>(), Arg.Any<CancellationToken>())
            .Returns(new PluginResult { Status = PluginResultStatus.Failed, Message = "boom", IsRetryable = true });
        var manager = CreateManager();
        manager._loadedPlugins.Add(CreateLoadedPlugin(plugin, CreateManifest("p1", maxAttempts: 3)));
        SeedPending(new PendingEntry { AdrKey = "0001-v1-r0", EventType = "Approved", Attempts = 0 });

        List<PendingEntry>? written = null;
        _fileSystem.WriteAllTextAsync(Arg.Any<string>(), Arg.Do<string>(j => written = JsonSerializer.Deserialize<List<PendingEntry>>(j, PluginManifest.SerializerOptions)), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var summary = await manager.RetryPendingAsync(ResolverFor("0001-v1-r0"), CreateRepoSnapshot(), isActive: null, cancellationToken: TestContext.Current.CancellationToken);

        summary.StillPending.Should().Be(1);
        await plugin.Received(3).OnAdrEventAsync(Arg.Any<AdrEventContext>(), Arg.Any<CancellationToken>());
        written.Should().ContainSingle(e => e.AdrKey == "0001-v1-r0" && e.Attempts == 3 && e.LastError == "boom");
    }

    [Fact]
    public async Task RetryPendingAsync_WhenEntryAlreadyAtMaxAttempts_StillMakesOneAttemptThisRun()
    {
        // Regression: the user's "keep pending across runs" policy must not silently become "never retried
        // again" just because a previous run already reached maxAttempts.
        var plugin = Substitute.For<IAdrPlugin>();
        plugin.ShouldHandle(Arg.Any<AdrEventContext>()).Returns(true);
        plugin.OnAdrEventAsync(Arg.Any<AdrEventContext>(), Arg.Any<CancellationToken>())
            .Returns(new PluginResult { Status = PluginResultStatus.Success });
        var manager = CreateManager();
        manager._loadedPlugins.Add(CreateLoadedPlugin(plugin, CreateManifest("p1", maxAttempts: 3)));
        SeedPending(new PendingEntry { AdrKey = "0001-v1-r0", EventType = "Approved", Attempts = 3 });

        var summary = await manager.RetryPendingAsync(ResolverFor("0001-v1-r0"), CreateRepoSnapshot(), isActive: null, cancellationToken: TestContext.Current.CancellationToken);

        await plugin.Received(1).OnAdrEventAsync(Arg.Any<AdrEventContext>(), Arg.Any<CancellationToken>());
        summary.Succeeded.Should().Be(1);
    }

    [Fact]
    public async Task RetryPendingAsync_WhenShouldHandleNowFalse_RemovesEntryAsSkippedWithoutAttempt()
    {
        var plugin = Substitute.For<IAdrPlugin>();
        plugin.ShouldHandle(Arg.Any<AdrEventContext>()).Returns(false);
        var manager = CreateManager();
        manager._loadedPlugins.Add(CreateLoadedPlugin(plugin, CreateManifest("p1")));
        SeedPending(new PendingEntry { AdrKey = "0001-v1-r0", EventType = "Approved", Attempts = 0 });

        List<PendingEntry>? written = null;
        _fileSystem.WriteAllTextAsync(Arg.Any<string>(), Arg.Do<string>(j => written = JsonSerializer.Deserialize<List<PendingEntry>>(j, PluginManifest.SerializerOptions)), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var summary = await manager.RetryPendingAsync(ResolverFor("0001-v1-r0"), CreateRepoSnapshot(), isActive: null, cancellationToken: TestContext.Current.CancellationToken);

        summary.Skipped.Should().Be(1);
        await plugin.DidNotReceive().OnAdrEventAsync(Arg.Any<AdrEventContext>(), Arg.Any<CancellationToken>());
        written.Should().BeEmpty();
    }

    [Fact]
    public async Task RetryPendingAsync_WhenHookReturnsSkipped_RemovesEntryAsSkipped()
    {
        var plugin = Substitute.For<IAdrPlugin>();
        plugin.ShouldHandle(Arg.Any<AdrEventContext>()).Returns(true);
        plugin.OnAdrEventAsync(Arg.Any<AdrEventContext>(), Arg.Any<CancellationToken>())
            .Returns(new PluginResult { Status = PluginResultStatus.Skipped });
        var manager = CreateManager();
        manager._loadedPlugins.Add(CreateLoadedPlugin(plugin, CreateManifest("p1")));
        SeedPending(new PendingEntry { AdrKey = "0001-v1-r0", EventType = "Approved", Attempts = 0 });

        var summary = await manager.RetryPendingAsync(ResolverFor("0001-v1-r0"), CreateRepoSnapshot(), isActive: null, cancellationToken: TestContext.Current.CancellationToken);

        summary.Skipped.Should().Be(1);
    }

    [Fact]
    public async Task RetryPendingAsync_WhenHookReturnsPermanentFailed_RemovesEntryAsPermanentlyFailed()
    {
        var plugin = Substitute.For<IAdrPlugin>();
        plugin.ShouldHandle(Arg.Any<AdrEventContext>()).Returns(true);
        plugin.OnAdrEventAsync(Arg.Any<AdrEventContext>(), Arg.Any<CancellationToken>())
            .Returns(new PluginResult { Status = PluginResultStatus.Failed, Message = "bad credentials", IsRetryable = false });
        var manager = CreateManager();
        manager._loadedPlugins.Add(CreateLoadedPlugin(plugin, CreateManifest("p1")));
        SeedPending(new PendingEntry { AdrKey = "0001-v1-r0", EventType = "Approved", Attempts = 0 });

        List<PendingEntry>? written = null;
        _fileSystem.WriteAllTextAsync(Arg.Any<string>(), Arg.Do<string>(j => written = JsonSerializer.Deserialize<List<PendingEntry>>(j, PluginManifest.SerializerOptions)), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var summary = await manager.RetryPendingAsync(ResolverFor("0001-v1-r0"), CreateRepoSnapshot(), isActive: null, cancellationToken: TestContext.Current.CancellationToken);

        summary.PermanentlyFailed.Should().Be(1);
        written.Should().BeEmpty();
        _console.Received(1).PromptWriteError(Arg.Is<string>(s => s.Contains("p1")));
    }

    [Fact]
    public async Task RetryPendingAsync_WhenAdrNoLongerResolves_DropsEntry()
    {
        var plugin = Substitute.For<IAdrPlugin>();
        var manager = CreateManager();
        manager._loadedPlugins.Add(CreateLoadedPlugin(plugin, CreateManifest("p1")));
        SeedPending(new PendingEntry { AdrKey = "0001-v1-r0", EventType = "Approved", Attempts = 0 });

        var summary = await manager.RetryPendingAsync(_ => null, CreateRepoSnapshot(), isActive: null, cancellationToken: TestContext.Current.CancellationToken);

        summary.Dropped.Should().Be(1);
        await plugin.DidNotReceive().OnAdrEventAsync(Arg.Any<AdrEventContext>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RetryPendingAsync_WhenInitializeAsyncThrows_LeavesEntriesUntouched()
    {
        var plugin = Substitute.For<IAdrPlugin>();
        plugin.InitializeAsync(Arg.Any<IPluginContext>(), Arg.Any<IPluginConfiguration>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("missing credentials"));
        var manager = CreateManager();
        manager._loadedPlugins.Add(CreateLoadedPlugin(plugin, CreateManifest("p1")));
        SeedPending(new PendingEntry { AdrKey = "0001-v1-r0", EventType = "Approved", Attempts = 1, LastError = "prior error" });

        var summary = await manager.RetryPendingAsync(ResolverFor("0001-v1-r0"), CreateRepoSnapshot(), isActive: null, cancellationToken: TestContext.Current.CancellationToken);

        summary.Succeeded.Should().Be(0);
        summary.StillPending.Should().Be(0);
        summary.PermanentlyFailed.Should().Be(0);
        summary.Dropped.Should().Be(0);
        await _fileSystem.DidNotReceive().WriteAllTextAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RetryPendingAsync_WithTwoDifferentPlugins_ProcessesEntriesIndependently()
    {
        var succeeding = Substitute.For<IAdrPlugin>();
        succeeding.ShouldHandle(Arg.Any<AdrEventContext>()).Returns(true);
        succeeding.OnAdrEventAsync(Arg.Any<AdrEventContext>(), Arg.Any<CancellationToken>())
            .Returns(new PluginResult { Status = PluginResultStatus.Success });

        var failing = Substitute.For<IAdrPlugin>();
        failing.ShouldHandle(Arg.Any<AdrEventContext>()).Returns(true);
        failing.OnAdrEventAsync(Arg.Any<AdrEventContext>(), Arg.Any<CancellationToken>())
            .Returns(new PluginResult { Status = PluginResultStatus.Failed, Message = "boom", IsRetryable = false });

        var manager = CreateManager();
        manager._loadedPlugins.Add(new LoadedPlugin(succeeding, CreateManifest("p1"), "/repo/plugins/p1"));
        manager._loadedPlugins.Add(new LoadedPlugin(failing, CreateManifest("p2"), "/repo/plugins/p2"));

        // Matched by substring, not exact path — Path.Combine's separator is platform-dependent.
        var p1Json = JsonSerializer.Serialize(new List<PendingEntry> { new() { AdrKey = "0001-v1-r0", EventType = "Approved" } }, PluginManifest.SerializerOptions);
        var p2Json = JsonSerializer.Serialize(new List<PendingEntry> { new() { AdrKey = "0002-v1-r0", EventType = "Approved" } }, PluginManifest.SerializerOptions);
        _fileSystem.FileExists(Arg.Any<string>()).Returns(true);
        _fileSystem.ReadAllTextAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(ci => ci.Arg<string>().Contains("p1") ? p1Json : p2Json);

        var summary = await manager.RetryPendingAsync(
            key => key switch
            {
                "0001-v1-r0" => (CreateAdrSnapshot(1), "/repo/adr/0001.md", "c1"),
                "0002-v1-r0" => (CreateAdrSnapshot(2), "/repo/adr/0002.md", "c2"),
                _ => null
            },
            CreateRepoSnapshot(),
            isActive: null,
            cancellationToken: TestContext.Current.CancellationToken);

        summary.Succeeded.Should().Be(1);
        summary.PermanentlyFailed.Should().Be(1);
    }
}
