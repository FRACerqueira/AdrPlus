// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the AdrPlus project under MIT license
// ***************************************************************************************

using AdrPlus.Abstractions;
using AdrPlus.Commands;
using AdrPlus.Commands.Plugins;
using AdrPlus.Core;
using AdrPlus.Domain;
using AdrPlus.Infrastructure.FileSystem;
using AdrPlus.Infrastructure.UI;
using AdrPlus.Plugins;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;
using static AdrPlus.Tests.Helpers.TestPathData;

namespace AdrPlus.Tests.Commands.Plugins;

/// <summary>
/// Unit tests for <see cref="PluginsCommandHandler"/> — the <c>adrplus plugins --list</c>/<c>--validate</c>
/// diagnostic subcommands (spec §8, Fase 7). <see cref="IPluginManager"/> is mocked directly (not the concrete
/// <c>PluginManager</c>): the deeper "does a rejection get classified correctly" behavior is already covered by
/// <c>PluginLoaderTests</c>/<c>PluginManagerTests</c> (Fase 3) — this suite only verifies the handler reports
/// whatever <see cref="IPluginManager.LoadedPlugins"/>/<see cref="IPluginManager.Rejections"/> say, and never
/// triggers dispatch/retry/backfill as a side effect.
/// </summary>
public class PluginsCommandHandlerTests
{
    private const string FolderPath = "/repo/plugins/test-plugin";

    private ILogger<PluginsCommandHandler> _mockLogger = null!;
    private IFileSystemService _mockFileSystem = null!;
    private IConsoleWriter _mockConsole = null!;
    private IAdrServices _mockAdrServices = null!;
    private IPluginManager _mockPluginManager = null!;
    private PluginsCommandHandler _handler = null!;

    public PluginsCommandHandlerTests()
    {
        _mockLogger = Substitute.For<ILogger<PluginsCommandHandler>>();
        _mockFileSystem = Substitute.For<IFileSystemService>();
        _mockConsole = Substitute.For<IConsoleWriter>();
        _mockAdrServices = Substitute.For<IAdrServices>();
        _mockPluginManager = Substitute.For<IPluginManager>();

        _handler = CreateHandler(new AdrPlusConfig());
    }

    private PluginsCommandHandler CreateHandler(AdrPlusConfig config) =>
        new(_mockLogger, _mockFileSystem, _mockConsole, _mockAdrServices, Options.Create(config), _mockPluginManager);

    private static PluginManifest CreateManifest(string name, string version, IEnumerable<string> subscribedEvents) => new()
    {
        Name = name,
        Version = version,
        EntryAssembly = "Plugin.dll",
        EntryType = "Plugin.Type",
        AbstractionsVersion = "1.0.0",
        SubscribedEvents = [.. subscribedEvents]
    };

    private static LoadedPlugin CreateLoadedPlugin(string name, string version, IEnumerable<string> subscribedEvents) =>
        new(Substitute.For<IAdrPlugin>(), CreateManifest(name, version, subscribedEvents), FolderPath);

    private void SeedPending(int count)
    {
        var entries = Enumerable.Range(1, count)
            .Select(i => new PendingEntry { AdrKey = $"{i:D4}-v1-r0", EventType = "Approved", CorrelationId = "c", Attempts = 1, Timestamp = DateTime.UtcNow })
            .ToList();
        var json = JsonSerializer.Serialize(entries, PluginManifest.SerializerOptions);
        _mockFileSystem.FileExists(Arg.Any<string>()).Returns(true);
        _mockFileSystem.ReadAllTextAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(json);
    }

    private void SetupParsedArgs(params Arguments[] present) => SetupParsedArgs(targetRepo: null, present);

    private void SetupParsedArgs(string? targetRepo, params Arguments[] present)
    {
        var parsed = present.ToDictionary(a => a, _ => string.Empty);
        if (targetRepo is not null)
        {
            parsed[Arguments.TargetRepo] = targetRepo;
        }
        _mockAdrServices.ParseArgs(Arg.Any<string[]>(), Arg.Any<Arguments[]>()).Returns(parsed);
    }

    [Fact]
    public void Constructor_WithValidParameters_CreatesInstance()
    {
        _handler.Should().NotBeNull();
    }

    [Fact]
    public async Task ExecuteAsync_WithHelpArgument_WritesHelpToConsole()
    {
        var args = new[] { "--help" };
        _mockAdrServices.ParseArgs(args, Arg.Any<Arguments[]>())
            .Returns(new Dictionary<Arguments, string> { { Arguments.Help, string.Empty } });
        _mockAdrServices.GetHelpText(Arg.Any<string>(), Arg.Any<Arguments[]>(), Arg.Any<string[]>())
            .Returns("Help text");

        await _handler.ExecuteAsync(args, TestContext.Current.CancellationToken);

        _mockConsole.Received(1).PromptWriteHelp("Help text");
        await _mockPluginManager.DidNotReceive().LoadPluginsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WithNullArgs_ThrowsArgumentNullException()
    {
        await _handler.Invoking(h => h.ExecuteAsync(null!, TestContext.Current.CancellationToken))
            .Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task ExecuteAsync_WithNeitherListNorValidate_ThrowsArgumentException()
    {
        SetupParsedArgs(Arguments.TargetRepo);
        _mockFileSystem.DirectoryExists(Arg.Any<string>()).Returns(true);

        await _handler.Invoking(h => h.ExecuteAsync(["--path", RepositoryPath], TestContext.Current.CancellationToken))
            .Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task ExecuteAsync_WithBothListAndValidate_ThrowsArgumentException()
    {
        SetupParsedArgs(Arguments.PluginsList, Arguments.PluginsValidate);
        _mockFileSystem.DirectoryExists(Arg.Any<string>()).Returns(true);

        await _handler.Invoking(h => h.ExecuteAsync(["--list", "--validate"], TestContext.Current.CancellationToken))
            .Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task ExecuteAsync_WithInvalidDirectory_ThrowsDirectoryNotFoundException()
    {
        SetupParsedArgs(Arguments.PluginsList, Arguments.TargetRepo);
        _mockFileSystem.DirectoryExists(Arg.Any<string>()).Returns(false);

        await _handler.Invoking(h => h.ExecuteAsync(["--list", "--path", "nonexistent"], TestContext.Current.CancellationToken))
            .Should().ThrowAsync<DirectoryNotFoundException>();
    }

    [Fact]
    public async Task ExecuteAsync_List_LoadsPluginsFromPluginsSubfolder()
    {
        SetupParsedArgs(RepositoryPath, Arguments.PluginsList);
        _mockFileSystem.DirectoryExists(Arg.Any<string>()).Returns(true);
        _mockPluginManager.LoadedPlugins.Returns(new List<LoadedPlugin>());
        _mockPluginManager.Rejections.Returns(new List<PluginRejection>());

        await _handler.ExecuteAsync(["--list", "--path", RepositoryPath], TestContext.Current.CancellationToken);

        await _mockPluginManager.Received(1).LoadPluginsAsync(Path.Combine(RepositoryPath, "plugins"), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_List_WithLoadedPlugin_WritesEntryWithAllFields()
    {
        SetupParsedArgs(Arguments.PluginsList, Arguments.TargetRepo);
        _mockFileSystem.DirectoryExists(Arg.Any<string>()).Returns(true);
        var plugin = CreateLoadedPlugin("SlackNotifier", "1.2.0", ["Approved", "Rejected"]);
        _mockPluginManager.LoadedPlugins.Returns(new List<LoadedPlugin> { plugin });
        _mockPluginManager.Rejections.Returns(new List<PluginRejection>());
        SeedPending(3);

        await _handler.ExecuteAsync(["--list", "--path", RepositoryPath], TestContext.Current.CancellationToken);

        _mockConsole.Received(1).PromptWriteInfo(Arg.Is<string>(s =>
            s.Contains("SlackNotifier") && s.Contains("1.2.0") && s.Contains("Approved") && s.Contains("Rejected") && s.Contains('3')));
    }

    [Fact]
    public async Task ExecuteAsync_List_WithNullAllowlist_ReportsNoAllowlistConfigured()
    {
        SetupParsedArgs(Arguments.PluginsList, Arguments.TargetRepo);
        _mockFileSystem.DirectoryExists(Arg.Any<string>()).Returns(true);
        var plugin = CreateLoadedPlugin("Plugin1", "1.0.0", ["Approved"]);
        _mockPluginManager.LoadedPlugins.Returns(new List<LoadedPlugin> { plugin });
        _mockPluginManager.Rejections.Returns(new List<PluginRejection>());
        SeedPending(0);
        _handler = CreateHandler(new AdrPlusConfig { PluginAllowlist = null });

        await _handler.ExecuteAsync(["--list", "--path", RepositoryPath], TestContext.Current.CancellationToken);

        _mockConsole.Received(1).PromptWriteInfo(Arg.Is<string>(s => s.Contains("No allowlist configured")));
    }

    [Fact]
    public async Task ExecuteAsync_List_WithAllowlistConfigured_ReportsAllowlisted()
    {
        SetupParsedArgs(Arguments.PluginsList, Arguments.TargetRepo);
        _mockFileSystem.DirectoryExists(Arg.Any<string>()).Returns(true);
        var plugin = CreateLoadedPlugin("Plugin1", "1.0.0", ["Approved"]);
        _mockPluginManager.LoadedPlugins.Returns(new List<LoadedPlugin> { plugin });
        _mockPluginManager.Rejections.Returns(new List<PluginRejection>());
        SeedPending(0);
        _handler = CreateHandler(new AdrPlusConfig { PluginAllowlist = [new PluginAllowlistEntry { Name = "Plugin1" }] });

        await _handler.ExecuteAsync(["--list", "--path", RepositoryPath], TestContext.Current.CancellationToken);

        _mockConsole.Received(1).PromptWriteInfo(Arg.Is<string>(s => s.Contains("Allowlisted")));
    }

    [Fact]
    public async Task ExecuteAsync_List_WithNoLoadedPlugins_WritesEmptyMessageAndSummary()
    {
        SetupParsedArgs(Arguments.PluginsList, Arguments.TargetRepo);
        _mockFileSystem.DirectoryExists(Arg.Any<string>()).Returns(true);
        _mockPluginManager.LoadedPlugins.Returns(new List<LoadedPlugin>());
        _mockPluginManager.Rejections.Returns(new List<PluginRejection>());

        await _handler.ExecuteAsync(["--list", "--path", RepositoryPath], TestContext.Current.CancellationToken);

        _mockConsole.Received(1).PromptWriteInfo(Arg.Is<string>(s => s.Contains("No plugins loaded")));
        _mockConsole.Received(1).PromptWriteSuccess(Arg.Is<string>(s => s.Contains('0')));
    }

    [Fact]
    public async Task ExecuteAsync_List_NeverDispatchesRetriesOrBackfills()
    {
        SetupParsedArgs(Arguments.PluginsList, Arguments.TargetRepo);
        _mockFileSystem.DirectoryExists(Arg.Any<string>()).Returns(true);
        _mockPluginManager.LoadedPlugins.Returns(new List<LoadedPlugin>());
        _mockPluginManager.Rejections.Returns(new List<PluginRejection>());

        await _handler.ExecuteAsync(["--list", "--path", RepositoryPath], TestContext.Current.CancellationToken);

        await _mockPluginManager.DidNotReceive().DispatchAsync(
            Arg.Any<AdrEventType>(), Arg.Any<AdrPlus.Abstractions.Domain.AdrRecordSnapshot>(), Arg.Any<string>(),
            Arg.Any<Func<string>>(), Arg.Any<AdrPlus.Abstractions.Domain.RepoInfoSnapshot>(), Arg.Any<bool>(), Arg.Any<CancellationToken>());
        await _mockPluginManager.DidNotReceive().RetryPendingAsync(Arg.Any<Func<string, (AdrPlus.Abstractions.Domain.AdrRecordSnapshot, string, string)?>>(), Arg.Any<AdrPlus.Abstractions.Domain.RepoInfoSnapshot>(), Arg.Any<CancellationToken>());
        await _mockPluginManager.DidNotReceive().BackfillAsync(Arg.Any<IEnumerable<(AdrEventType, AdrPlus.Abstractions.Domain.AdrRecordSnapshot, string, Func<string>)>>(), Arg.Any<AdrPlus.Abstractions.Domain.RepoInfoSnapshot>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_Validate_WithLoadedPlugin_WritesValidEntry()
    {
        SetupParsedArgs(Arguments.PluginsValidate, Arguments.TargetRepo);
        _mockFileSystem.DirectoryExists(Arg.Any<string>()).Returns(true);
        var plugin = CreateLoadedPlugin("JiraSync", "2.0.0", ["Approved"]);
        _mockPluginManager.LoadedPlugins.Returns(new List<LoadedPlugin> { plugin });
        _mockPluginManager.Rejections.Returns(new List<PluginRejection>());

        await _handler.ExecuteAsync(["--validate", "--path", RepositoryPath], TestContext.Current.CancellationToken);

        _mockConsole.Received(1).PromptWriteInfo(Arg.Is<string>(s => s.Contains("VALID") && s.Contains("JiraSync") && s.Contains("2.0.0")));
    }

    [Fact]
    public async Task ExecuteAsync_Validate_WithRejection_NotInAllowlist_WritesRejectedEntry() =>
        await AssertRejectionReported(PluginRejectionReason.NotInAllowlist);

    [Fact]
    public async Task ExecuteAsync_Validate_WithRejection_DuplicateName_WritesRejectedEntry() =>
        await AssertRejectionReported(PluginRejectionReason.DuplicateName);

    private async Task AssertRejectionReported(PluginRejectionReason reason)
    {
        SetupParsedArgs(Arguments.PluginsValidate, Arguments.TargetRepo);
        _mockFileSystem.DirectoryExists(Arg.Any<string>()).Returns(true);
        var rejection = new PluginRejection(FolderPath, reason, $"rejected because {reason}");
        _mockPluginManager.LoadedPlugins.Returns(new List<LoadedPlugin>());
        _mockPluginManager.Rejections.Returns(new List<PluginRejection> { rejection });

        await _handler.ExecuteAsync(["--validate", "--path", RepositoryPath], TestContext.Current.CancellationToken);

        _mockConsole.Received(1).PromptWriteInfo(Arg.Is<string>(s =>
            s.Contains("REJECTED") && s.Contains(FolderPath) && s.Contains(reason.ToString()) && s.Contains($"rejected because {reason}")));
    }

    [Fact]
    public async Task ExecuteAsync_Validate_WithNoPluginsAndNoRejections_WritesEmptyMessage()
    {
        SetupParsedArgs(Arguments.PluginsValidate, Arguments.TargetRepo);
        _mockFileSystem.DirectoryExists(Arg.Any<string>()).Returns(true);
        _mockPluginManager.LoadedPlugins.Returns(new List<LoadedPlugin>());
        _mockPluginManager.Rejections.Returns(new List<PluginRejection>());

        await _handler.ExecuteAsync(["--validate", "--path", RepositoryPath], TestContext.Current.CancellationToken);

        _mockConsole.Received(1).PromptWriteInfo(Arg.Is<string>(s => s.Contains("No plugin candidates found")));
    }

    [Fact]
    public async Task ExecuteAsync_Validate_NeverDispatchesRetriesOrBackfills()
    {
        SetupParsedArgs(Arguments.PluginsValidate, Arguments.TargetRepo);
        _mockFileSystem.DirectoryExists(Arg.Any<string>()).Returns(true);
        var plugin = CreateLoadedPlugin("Plugin1", "1.0.0", ["Approved"]);
        _mockPluginManager.LoadedPlugins.Returns(new List<LoadedPlugin> { plugin });
        _mockPluginManager.Rejections.Returns(new List<PluginRejection>());

        await _handler.ExecuteAsync(["--validate", "--path", RepositoryPath], TestContext.Current.CancellationToken);

        await _mockPluginManager.DidNotReceive().DispatchAsync(
            Arg.Any<AdrEventType>(), Arg.Any<AdrPlus.Abstractions.Domain.AdrRecordSnapshot>(), Arg.Any<string>(),
            Arg.Any<Func<string>>(), Arg.Any<AdrPlus.Abstractions.Domain.RepoInfoSnapshot>(), Arg.Any<bool>(), Arg.Any<CancellationToken>());
        await _mockPluginManager.DidNotReceive().RetryPendingAsync(Arg.Any<Func<string, (AdrPlus.Abstractions.Domain.AdrRecordSnapshot, string, string)?>>(), Arg.Any<AdrPlus.Abstractions.Domain.RepoInfoSnapshot>(), Arg.Any<CancellationToken>());
        await _mockPluginManager.DidNotReceive().BackfillAsync(Arg.Any<IEnumerable<(AdrEventType, AdrPlus.Abstractions.Domain.AdrRecordSnapshot, string, Func<string>)>>(), Arg.Any<AdrPlus.Abstractions.Domain.RepoInfoSnapshot>(), Arg.Any<CancellationToken>());
    }
}
