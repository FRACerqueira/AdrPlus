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
    private IValidateConfig _mockValidateConfig = null!;
    private IPluginManager _mockPluginManager = null!;
    private PluginsCommandHandler _handler = null!;

    public PluginsCommandHandlerTests()
    {
        _mockLogger = Substitute.For<ILogger<PluginsCommandHandler>>();
        _mockFileSystem = Substitute.For<IFileSystemService>();
        _mockConsole = Substitute.For<IConsoleWriter>();
        _mockAdrServices = Substitute.For<IAdrServices>();
        _mockValidateConfig = Substitute.For<IValidateConfig>();
        _mockPluginManager = Substitute.For<IPluginManager>();

        _handler = CreateHandler(new AdrPlusConfig());
    }

    private PluginsCommandHandler CreateHandler(AdrPlusConfig config) =>
        new(_mockLogger, _mockFileSystem, _mockConsole, _mockAdrServices, _mockValidateConfig, Options.Create(config), _mockPluginManager);

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
        _mockFileSystem.FileExists(Arg.Is<string>(s => s.EndsWith("pending.json"))).Returns(true);
        _mockFileSystem.ReadAllTextAsync(Arg.Is<string>(s => s.EndsWith("pending.json")), Arg.Any<CancellationToken>()).Returns(json);
    }

    /// <summary>
    /// Mocks a minimal, structurally-valid <c>adr-config.adrplus</c> read — required by <c>--list</c> (and the
    /// manage wizard) since <see cref="PluginsCommandHandler"/> now reads the repo config to know
    /// <c>ActivePlugins</c>/<c>DisablePlugins</c>. Uses a path-suffix matcher distinct from <see cref="SeedPending"/>'s
    /// so the two mocked <c>ReadAllTextAsync</c> setups never collide on the same call.
    /// </summary>
    private void ArrangeValidRepoConfig(IEnumerable<string>? activePlugins = null, bool disablePlugins = false)
    {
        var namesJson = string.Join(", ", (activePlugins ?? []).Select(n => $"\"{n}\""));
        var json = $$"""{"activeplugins": [{{namesJson}}], "disableplugins": {{(disablePlugins ? "true" : "false")}}}""";
        _mockValidateConfig.GetFileNameRepoConfig().Returns(".adrplus");
        _mockFileSystem.FileExists(Arg.Is<string>(s => s.EndsWith(".adrplus"))).Returns(true);
        _mockFileSystem.ReadAllTextAsync(Arg.Is<string>(s => s.EndsWith(".adrplus")), Arg.Any<CancellationToken>()).Returns(json);
        _mockValidateConfig.ValidateRepoStructure(json).Returns((true, Array.Empty<string>()));
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
        ArrangeValidRepoConfig();
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
        ArrangeValidRepoConfig();
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
        ArrangeValidRepoConfig();
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
        ArrangeValidRepoConfig();
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
        ArrangeValidRepoConfig();
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
        ArrangeValidRepoConfig();
        _mockPluginManager.LoadedPlugins.Returns(new List<LoadedPlugin>());
        _mockPluginManager.Rejections.Returns(new List<PluginRejection>());

        await _handler.ExecuteAsync(["--list", "--path", RepositoryPath], TestContext.Current.CancellationToken);

        await _mockPluginManager.DidNotReceive().DispatchAsync(
            Arg.Any<AdrEventType>(), Arg.Any<AdrPlus.Abstractions.Domain.AdrRecordSnapshot>(), Arg.Any<string>(),
            Arg.Any<Func<string>>(), Arg.Any<AdrPlus.Abstractions.Domain.RepoInfoSnapshot>(), Arg.Any<bool>(), Arg.Any<Func<LoadedPlugin, bool>>(), Arg.Any<CancellationToken>());
        await _mockPluginManager.DidNotReceive().RetryPendingAsync(Arg.Any<Func<string, (AdrPlus.Abstractions.Domain.AdrRecordSnapshot, string, string)?>>(), Arg.Any<AdrPlus.Abstractions.Domain.RepoInfoSnapshot>(), Arg.Any<Func<LoadedPlugin, bool>>(), Arg.Any<CancellationToken>());
        await _mockPluginManager.DidNotReceive().BackfillAsync(Arg.Any<IEnumerable<(AdrEventType, AdrPlus.Abstractions.Domain.AdrRecordSnapshot, string, Func<string>)>>(), Arg.Any<AdrPlus.Abstractions.Domain.RepoInfoSnapshot>(), Arg.Any<Func<LoadedPlugin, bool>>(), Arg.Any<CancellationToken>());
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
            Arg.Any<Func<string>>(), Arg.Any<AdrPlus.Abstractions.Domain.RepoInfoSnapshot>(), Arg.Any<bool>(), Arg.Any<Func<LoadedPlugin, bool>>(), Arg.Any<CancellationToken>());
        await _mockPluginManager.DidNotReceive().RetryPendingAsync(Arg.Any<Func<string, (AdrPlus.Abstractions.Domain.AdrRecordSnapshot, string, string)?>>(), Arg.Any<AdrPlus.Abstractions.Domain.RepoInfoSnapshot>(), Arg.Any<Func<LoadedPlugin, bool>>(), Arg.Any<CancellationToken>());
        await _mockPluginManager.DidNotReceive().BackfillAsync(Arg.Any<IEnumerable<(AdrEventType, AdrPlus.Abstractions.Domain.AdrRecordSnapshot, string, Func<string>)>>(), Arg.Any<AdrPlus.Abstractions.Domain.RepoInfoSnapshot>(), Arg.Any<Func<LoadedPlugin, bool>>(), Arg.Any<CancellationToken>());
    }

    private void SetupWizardParsedArgs()
    {
        _mockAdrServices.ParseArgs(Arg.Any<string[]>(), Arg.Any<Arguments[]>())
            .Returns(new Dictionary<Arguments, string> { { Arguments.WizardPlugins, string.Empty } });
    }

    private void SetupActivateArgs(string name, string targetRepo) =>
        _mockAdrServices.ParseArgs(Arg.Any<string[]>(), Arg.Any<Arguments[]>())
            .Returns(new Dictionary<Arguments, string> { [Arguments.PluginsActivate] = name, [Arguments.TargetRepo] = targetRepo });

    private void SetupDeactivateArgs(string name, string targetRepo) =>
        _mockAdrServices.ParseArgs(Arg.Any<string[]>(), Arg.Any<Arguments[]>())
            .Returns(new Dictionary<Arguments, string> { [Arguments.PluginsDeactivate] = name, [Arguments.TargetRepo] = targetRepo });

    [Fact]
    public async Task ExecuteAsync_WithWizardMode_ListSelected_ShowsTableInsteadOfPlainText()
    {
        SetupWizardParsedArgs();
        _mockFileSystem.GetDrives().Returns([SingleTestDrive]);
        _mockFileSystem.DirectoryExists(RepositoryPath).Returns(true);
        _mockConsole.PromptSelectFolderPath(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<string>(), _mockFileSystem, _mockValidateConfig, Arg.Any<CancellationToken>())
            .Returns((false, RepositoryPath));
        _mockConsole.PromptSelectPluginsMode(Arg.Any<CancellationToken>()).Returns((false, PluginsWizardMode.List));
        ArrangeValidRepoConfig();
        var plugin = CreateLoadedPlugin("SlackNotifier", "1.2.0", ["Approved"]);
        _mockPluginManager.LoadedPlugins.Returns(new List<LoadedPlugin> { plugin });
        _mockPluginManager.Rejections.Returns(new List<PluginRejection>());

        await _handler.ExecuteAsync(["--wizard"], TestContext.Current.CancellationToken);

        _mockConsole.Received(1).PromptShowPluginsListTable(
            Arg.Is<IReadOnlyList<(string Status, string Name, string Version, string Events, string Allowlist, int Pending)>>(rows => rows.Count == 1 && rows[0].Name == "SlackNotifier"),
            Arg.Any<CancellationToken>());
        _mockConsole.DidNotReceive().PromptWriteInfo(Arg.Is<string>(s => s.Contains("SlackNotifier")));
    }

    [Fact]
    public async Task ExecuteAsync_WithWizardMode_ValidateSelected_ShowsTableInsteadOfPlainText()
    {
        SetupWizardParsedArgs();
        _mockFileSystem.GetDrives().Returns([SingleTestDrive]);
        _mockFileSystem.DirectoryExists(RepositoryPath).Returns(true);
        _mockConsole.PromptSelectFolderPath(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<string>(), _mockFileSystem, _mockValidateConfig, Arg.Any<CancellationToken>())
            .Returns((false, RepositoryPath));
        _mockConsole.PromptSelectPluginsMode(Arg.Any<CancellationToken>()).Returns((false, PluginsWizardMode.Validate));
        var plugin = CreateLoadedPlugin("JiraSync", "2.0.0", ["Approved"]);
        _mockPluginManager.LoadedPlugins.Returns(new List<LoadedPlugin> { plugin });
        var rejection = new PluginRejection(FolderPath, PluginRejectionReason.NotInAllowlist, "not allowed");
        _mockPluginManager.Rejections.Returns(new List<PluginRejection> { rejection });

        await _handler.ExecuteAsync(["--wizard"], TestContext.Current.CancellationToken);

        _mockConsole.Received(1).PromptShowPluginsValidateTable(
            Arg.Is<IReadOnlyList<(string Status, string NameOrFolder, string Detail)>>(rows => rows.Count == 2),
            Arg.Any<CancellationToken>());
        _mockConsole.DidNotReceive().PromptWriteInfo(Arg.Is<string>(s => s.Contains("JiraSync")));
    }

    [Fact]
    public async Task ExecuteAsync_WithWizardMode_ListSelected_NoPluginsLoaded_FallsBackToTextNotTable()
    {
        SetupWizardParsedArgs();
        _mockFileSystem.GetDrives().Returns([SingleTestDrive]);
        _mockFileSystem.DirectoryExists(RepositoryPath).Returns(true);
        _mockConsole.PromptSelectFolderPath(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<string>(), _mockFileSystem, _mockValidateConfig, Arg.Any<CancellationToken>())
            .Returns((false, RepositoryPath));
        _mockConsole.PromptSelectPluginsMode(Arg.Any<CancellationToken>()).Returns((false, PluginsWizardMode.List));
        ArrangeValidRepoConfig();
        _mockPluginManager.LoadedPlugins.Returns(new List<LoadedPlugin>());
        _mockPluginManager.Rejections.Returns(new List<PluginRejection>());

        await _handler.ExecuteAsync(["--wizard"], TestContext.Current.CancellationToken);

        _mockConsole.DidNotReceive().PromptShowPluginsListTable(Arg.Any<IReadOnlyList<(string, string, string, string, string, int)>>(), Arg.Any<CancellationToken>());
        _mockConsole.Received(1).PromptWriteInfo(Arg.Is<string>(s => s.Contains("No plugins loaded")));
    }

    [Fact]
    public async Task ExecuteAsync_WithWizardMode_FolderAborted_ThrowsOperationCanceledException()
    {
        SetupWizardParsedArgs();
        _mockFileSystem.GetDrives().Returns([SingleTestDrive]);
        _mockConsole.PromptSelectFolderPath(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<string>(), _mockFileSystem, _mockValidateConfig, Arg.Any<CancellationToken>())
            .Returns((true, string.Empty));

        await _handler.Invoking(h => h.ExecuteAsync(["--wizard"], TestContext.Current.CancellationToken))
            .Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task ExecuteAsync_WithWizardMode_ModeAborted_ThrowsOperationCanceledException()
    {
        SetupWizardParsedArgs();
        _mockFileSystem.GetDrives().Returns([SingleTestDrive]);
        _mockConsole.PromptSelectFolderPath(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<string>(), _mockFileSystem, _mockValidateConfig, Arg.Any<CancellationToken>())
            .Returns((false, RepositoryPath));
        _mockConsole.PromptSelectPluginsMode(Arg.Any<CancellationToken>()).Returns((true, PluginsWizardMode.List));

        await _handler.Invoking(h => h.ExecuteAsync(["--wizard"], TestContext.Current.CancellationToken))
            .Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task ExecuteAsync_WithWizardMode_TableAborted_ThrowsOperationCanceledException()
    {
        SetupWizardParsedArgs();
        _mockFileSystem.GetDrives().Returns([SingleTestDrive]);
        _mockFileSystem.DirectoryExists(RepositoryPath).Returns(true);
        _mockConsole.PromptSelectFolderPath(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<string>(), _mockFileSystem, _mockValidateConfig, Arg.Any<CancellationToken>())
            .Returns((false, RepositoryPath));
        _mockConsole.PromptSelectPluginsMode(Arg.Any<CancellationToken>()).Returns((false, PluginsWizardMode.List));
        ArrangeValidRepoConfig();
        var plugin = CreateLoadedPlugin("SlackNotifier", "1.2.0", ["Approved"]);
        _mockPluginManager.LoadedPlugins.Returns(new List<LoadedPlugin> { plugin });
        _mockPluginManager.Rejections.Returns(new List<PluginRejection>());
        _mockConsole.PromptShowPluginsListTable(Arg.Any<IReadOnlyList<(string, string, string, string, string, int)>>(), Arg.Any<CancellationToken>())
            .Returns(true);

        await _handler.Invoking(h => h.ExecuteAsync(["--wizard"], TestContext.Current.CancellationToken))
            .Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task ExecuteAsync_List_ComputesStatusPerPlugin_ActiveInactiveAndMissing()
    {
        SetupParsedArgs(Arguments.PluginsList, Arguments.TargetRepo);
        _mockFileSystem.DirectoryExists(Arg.Any<string>()).Returns(true);
        ArrangeValidRepoConfig(["SlackNotifier", "GhostPlugin"]);
        var active = CreateLoadedPlugin("SlackNotifier", "1.0.0", ["Approved"]);
        var inactive = CreateLoadedPlugin("OtherPlugin", "1.0.0", ["Approved"]);
        _mockPluginManager.LoadedPlugins.Returns(new List<LoadedPlugin> { active, inactive });
        _mockPluginManager.Rejections.Returns(new List<PluginRejection>());
        SeedPending(0);

        await _handler.ExecuteAsync(["--list", "--path", RepositoryPath], TestContext.Current.CancellationToken);

        _mockConsole.Received(1).PromptWriteInfo(Arg.Is<string>(s => s.StartsWith("Active") && s.Contains("SlackNotifier")));
        _mockConsole.Received(1).PromptWriteInfo(Arg.Is<string>(s => s.StartsWith("Inactive") && s.Contains("OtherPlugin")));
        _mockConsole.Received(1).PromptWriteInfo(Arg.Is<string>(s => s.StartsWith("Missing") && s.Contains("GhostPlugin")));
    }

    [Fact]
    public async Task ExecuteAsync_List_WithDisablePlugins_MarksEveryRowDisabled()
    {
        SetupParsedArgs(Arguments.PluginsList, Arguments.TargetRepo);
        _mockFileSystem.DirectoryExists(Arg.Any<string>()).Returns(true);
        ArrangeValidRepoConfig(["SlackNotifier"], disablePlugins: true);
        var plugin = CreateLoadedPlugin("SlackNotifier", "1.0.0", ["Approved"]);
        _mockPluginManager.LoadedPlugins.Returns(new List<LoadedPlugin> { plugin });
        _mockPluginManager.Rejections.Returns(new List<PluginRejection>());
        SeedPending(0);

        await _handler.ExecuteAsync(["--list", "--path", RepositoryPath], TestContext.Current.CancellationToken);

        _mockConsole.Received(1).PromptWriteInfo(Arg.Is<string>(s => s.StartsWith("Disabled") && s.Contains("SlackNotifier")));
    }

    [Fact]
    public async Task ExecuteAsync_WithWizardMode_ManageSelected_WritesSelectedActivePlugins()
    {
        SetupWizardParsedArgs();
        _mockFileSystem.GetDrives().Returns([SingleTestDrive]);
        _mockFileSystem.DirectoryExists(RepositoryPath).Returns(true);
        _mockConsole.PromptSelectFolderPath(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<string>(), _mockFileSystem, _mockValidateConfig, Arg.Any<CancellationToken>())
            .Returns((false, RepositoryPath));
        _mockConsole.PromptSelectPluginsMode(Arg.Any<CancellationToken>()).Returns((false, PluginsWizardMode.Manage));
        ArrangeValidRepoConfig(["OldPlugin"]);
        var plugin = CreateLoadedPlugin("NewPlugin", "1.0.0", ["Approved"]);
        _mockPluginManager.LoadedPlugins.Returns(new List<LoadedPlugin> { plugin });
        _mockConsole.PromptSelectActivePlugins(Arg.Any<IReadOnlyList<string>>(), Arg.Any<IReadOnlySet<string>>(), Arg.Any<CancellationToken>())
            .Returns((false, new[] { "NewPlugin" }));

        string? written = null;
        _mockFileSystem.WriteAllTextAsync(Arg.Is<string>(s => s.EndsWith(".adrplus")), Arg.Do<string>(c => written = c), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        await _handler.ExecuteAsync(["--wizard"], TestContext.Current.CancellationToken);

        written.Should().NotBeNull();
        using var doc = JsonDocument.Parse(written!);
        doc.RootElement.GetProperty("activeplugins").EnumerateArray().Select(e => e.GetString()).Should().BeEquivalentTo(["NewPlugin"]);
        _mockConsole.Received(1).PromptWriteSuccess(Arg.Is<string>(s => s.Contains("NewPlugin")));
    }

    [Fact]
    public async Task ExecuteAsync_WithListAndActivate_ThrowsArgumentException()
    {
        _mockAdrServices.ParseArgs(Arg.Any<string[]>(), Arg.Any<Arguments[]>())
            .Returns(new Dictionary<Arguments, string> { [Arguments.PluginsList] = string.Empty, [Arguments.PluginsActivate] = "Plugin1", [Arguments.TargetRepo] = RepositoryPath });
        _mockFileSystem.DirectoryExists(Arg.Any<string>()).Returns(true);

        await _handler.Invoking(h => h.ExecuteAsync(["--list", "--activate", "Plugin1", "--path", RepositoryPath], TestContext.Current.CancellationToken))
            .Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task ExecuteAsync_Activate_AddsNameAndNeverLoadsPlugins()
    {
        SetupActivateArgs("NewPlugin", RepositoryPath);
        _mockFileSystem.DirectoryExists(Arg.Any<string>()).Returns(true);
        ArrangeValidRepoConfig(["ExistingPlugin"]);
        string? written = null;
        _mockFileSystem.WriteAllTextAsync(Arg.Is<string>(s => s.EndsWith(".adrplus")), Arg.Do<string>(c => written = c), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        await _handler.ExecuteAsync(["--activate", "NewPlugin", "--path", RepositoryPath], TestContext.Current.CancellationToken);

        written.Should().NotBeNull();
        using var doc = JsonDocument.Parse(written!);
        doc.RootElement.GetProperty("activeplugins").EnumerateArray().Select(e => e.GetString())
            .Should().BeEquivalentTo(["ExistingPlugin", "NewPlugin"]);
        _mockConsole.Received(1).PromptWriteSuccess(Arg.Is<string>(s => s.Contains("NewPlugin") && s.Contains("ExistingPlugin")));
        await _mockPluginManager.DidNotReceive().LoadPluginsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_Activate_AlreadyActive_IsIdempotent()
    {
        SetupActivateArgs("ExistingPlugin", RepositoryPath);
        _mockFileSystem.DirectoryExists(Arg.Any<string>()).Returns(true);
        ArrangeValidRepoConfig(["ExistingPlugin"]);
        string? written = null;
        _mockFileSystem.WriteAllTextAsync(Arg.Is<string>(s => s.EndsWith(".adrplus")), Arg.Do<string>(c => written = c), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        await _handler.ExecuteAsync(["--activate", "ExistingPlugin", "--path", RepositoryPath], TestContext.Current.CancellationToken);

        written.Should().NotBeNull();
        using var doc = JsonDocument.Parse(written!);
        doc.RootElement.GetProperty("activeplugins").EnumerateArray().Select(e => e.GetString())
            .Should().BeEquivalentTo(["ExistingPlugin"]);
    }

    [Fact]
    public async Task ExecuteAsync_Deactivate_RemovesNameAndNeverLoadsPlugins()
    {
        SetupDeactivateArgs("ExistingPlugin", RepositoryPath);
        _mockFileSystem.DirectoryExists(Arg.Any<string>()).Returns(true);
        ArrangeValidRepoConfig(["ExistingPlugin", "OtherPlugin"]);
        string? written = null;
        _mockFileSystem.WriteAllTextAsync(Arg.Is<string>(s => s.EndsWith(".adrplus")), Arg.Do<string>(c => written = c), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        await _handler.ExecuteAsync(["--deactivate", "ExistingPlugin", "--path", RepositoryPath], TestContext.Current.CancellationToken);

        written.Should().NotBeNull();
        using var doc = JsonDocument.Parse(written!);
        doc.RootElement.GetProperty("activeplugins").EnumerateArray().Select(e => e.GetString())
            .Should().BeEquivalentTo(["OtherPlugin"]);
        await _mockPluginManager.DidNotReceive().LoadPluginsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_Deactivate_NotPresent_IsNoOp()
    {
        SetupDeactivateArgs("GhostPlugin", RepositoryPath);
        _mockFileSystem.DirectoryExists(Arg.Any<string>()).Returns(true);
        ArrangeValidRepoConfig(["ExistingPlugin"]);
        string? written = null;
        _mockFileSystem.WriteAllTextAsync(Arg.Is<string>(s => s.EndsWith(".adrplus")), Arg.Do<string>(c => written = c), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        await _handler.ExecuteAsync(["--deactivate", "GhostPlugin", "--path", RepositoryPath], TestContext.Current.CancellationToken);

        written.Should().NotBeNull();
        using var doc = JsonDocument.Parse(written!);
        doc.RootElement.GetProperty("activeplugins").EnumerateArray().Select(e => e.GetString())
            .Should().BeEquivalentTo(["ExistingPlugin"]);
    }

    [Fact]
    public async Task ExecuteAsync_WithWizardMode_UninstallSelected_NoInstalledPlugins_ShowsInfoAndReturns()
    {
        SetupWizardParsedArgs();
        _mockFileSystem.GetDrives().Returns([SingleTestDrive]);
        _mockFileSystem.DirectoryExists(RepositoryPath).Returns(true);
        _mockConsole.PromptSelectFolderPath(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<string>(), _mockFileSystem, _mockValidateConfig, Arg.Any<CancellationToken>())
            .Returns((false, RepositoryPath));
        _mockConsole.PromptSelectPluginsMode(Arg.Any<CancellationToken>()).Returns((false, PluginsWizardMode.Uninstall));

        await _handler.ExecuteAsync(["--wizard"], TestContext.Current.CancellationToken);

        _mockConsole.Received(1).PromptWriteInfo(Arg.Is<string>(s => s.Contains("./plugins")));
        _mockConsole.DidNotReceive().PromptSelectPluginsToUninstall(Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>());
    }
}
