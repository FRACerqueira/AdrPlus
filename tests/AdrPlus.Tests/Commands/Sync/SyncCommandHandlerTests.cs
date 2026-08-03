// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the AdrPlus project under MIT license
// ***************************************************************************************

using AdrPlus.Abstractions;
using AdrPlus.Abstractions.Domain;
using AdrPlus.Commands;
using AdrPlus.Commands.Sync;
using AdrPlus.Core;
using AdrPlus.Domain;
using AdrPlus.Infrastructure.FileSystem;
using AdrPlus.Infrastructure.UI;
using AdrPlus.Plugins;
using Microsoft.Extensions.Logging;
using static AdrPlus.Tests.Helpers.TestPathData;

namespace AdrPlus.Tests.Commands.Sync;

/// <summary>
/// Unit tests for <see cref="SyncCommandHandler"/> — the <c>adrplus sync</c> default (no-flag) mode command,
/// mirroring <c>MigrateCommandHandlerTests</c>'s repo-wide (not single-file) argument pattern.
/// </summary>
public class SyncCommandHandlerTests
{
    private ILogger<SyncCommandHandler> _mockLogger = null!;
    private IFileSystemService _mockFileSystem = null!;
    private IConsoleWriter _mockConsole = null!;
    private IValidateConfig _mockValidateConfig = null!;
    private IAdrServices _mockAdrServices = null!;
    private IPluginManager _mockPluginManager = null!;
    private SyncCommandHandler _handler = null!;

    public SyncCommandHandlerTests()
    {
        _mockLogger = Substitute.For<ILogger<SyncCommandHandler>>();
        _mockFileSystem = Substitute.For<IFileSystemService>();
        _mockConsole = Substitute.For<IConsoleWriter>();
        _mockValidateConfig = Substitute.For<IValidateConfig>();
        _mockAdrServices = Substitute.For<IAdrServices>();
        _mockPluginManager = Substitute.For<IPluginManager>();

        _handler = new SyncCommandHandler(
            _mockLogger,
            _mockFileSystem,
            _mockValidateConfig,
            _mockConsole,
            _mockAdrServices,
            _mockPluginManager);
    }

    [Fact]
    public void Constructor_WithValidParameters_CreatesInstance()
    {
        var handler = new SyncCommandHandler(
            _mockLogger,
            _mockFileSystem,
            _mockValidateConfig,
            _mockConsole,
            _mockAdrServices,
            _mockPluginManager);

        handler.Should().NotBeNull();
    }

    [Fact]
    public async Task ExecuteAsync_WithHelpArgument_WritesHelpToConsole()
    {
        var args = new[] { "--help" };
        var parsedArgs = new Dictionary<Arguments, string> { { Arguments.Help, string.Empty } };
        _mockAdrServices.ParseArgs(args, Arg.Any<Arguments[]>()).Returns(parsedArgs);
        _mockAdrServices.GetHelpText(Arg.Any<string>(), Arg.Any<Arguments[]>(), Arg.Any<string[]>())
            .Returns("Help text");

        await _handler.ExecuteAsync(args, TestContext.Current.CancellationToken);

        _mockConsole.Received(1).PromptWriteHelp("Help text");
    }

    [Fact]
    public async Task ExecuteAsync_WithNullArgs_ThrowsArgumentNullException()
    {
        await _handler.Invoking(h => h.ExecuteAsync(null!, TestContext.Current.CancellationToken))
            .Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task ExecuteAsync_WithInvalidDirectory_ThrowsDirectoryNotFoundException()
    {
        var args = new[] { "--path", "nonexistent/path" };
        var parsedArgs = new Dictionary<Arguments, string> { { Arguments.TargetRepo, "nonexistent/path" } };
        _mockAdrServices.ParseArgs(args, Arg.Any<Arguments[]>()).Returns(parsedArgs);
        _mockFileSystem.DirectoryExists(Arg.Any<string>()).Returns(false);

        await _handler.Invoking(h => h.ExecuteAsync(args, TestContext.Current.CancellationToken))
            .Should().ThrowAsync<DirectoryNotFoundException>();
    }

    [Fact]
    public async Task ExecuteAsync_WithMissingConfigFile_ThrowsFileNotFoundException()
    {
        var args = new[] { "--path", RepositoryPath };
        var parsedArgs = new Dictionary<Arguments, string> { { Arguments.TargetRepo, RepositoryPath } };
        var configPath = Path.Combine(RepositoryPath, ".adrplus");

        _mockAdrServices.ParseArgs(args, Arg.Any<Arguments[]>()).Returns(parsedArgs);
        _mockFileSystem.DirectoryExists(RepositoryPath).Returns(true);
        _mockValidateConfig.GetFileNameRepoConfig().Returns(".adrplus");
        _mockFileSystem.FileExists(configPath).Returns(false);

        await _handler.Invoking(h => h.ExecuteAsync(args, TestContext.Current.CancellationToken))
            .Should().ThrowAsync<FileNotFoundException>();
    }

    [Fact]
    public async Task ExecuteAsync_WithInvalidConfigStructure_ThrowsInvalidDataException()
    {
        var args = new[] { "--path", RepositoryPath };
        var parsedArgs = new Dictionary<Arguments, string> { { Arguments.TargetRepo, RepositoryPath } };
        var jsonConfig = """{"Invalid": "config"}""";
        var errors = new[] { "Config error" };

        _mockAdrServices.ParseArgs(args, Arg.Any<Arguments[]>()).Returns(parsedArgs);
        _mockFileSystem.DirectoryExists(RepositoryPath).Returns(true);
        _mockValidateConfig.GetFileNameRepoConfig().Returns(".adrplus");
        _mockFileSystem.FileExists(Arg.Is<string>(s => s.EndsWith(".adrplus"))).Returns(true);
        _mockFileSystem.ReadAllTextAsync(Arg.Is<string>(s => s.EndsWith(".adrplus")), Arg.Any<CancellationToken>()).Returns(jsonConfig);
        _mockValidateConfig.ValidateRepoStructure(jsonConfig).Returns((false, errors));

        await _handler.Invoking(h => h.ExecuteAsync(args, TestContext.Current.CancellationToken))
            .Should().ThrowAsync<InvalidDataException>();
        _mockConsole.Received(1).PromptWriteError("Config error");
    }

    [Fact]
    public async Task ExecuteAsync_WithNoAdrsFound_IsNotFatalAndStillRetriesPending()
    {
        // pending.json can reference adrKeys from ADRs that no longer exist in the repo — that's the
        // resolveAdr callback's job to report as "not found", not a reason for the command itself to fail.
        var args = new[] { "--path", RepositoryPath };
        var parsedArgs = new Dictionary<Arguments, string> { { Arguments.TargetRepo, RepositoryPath } };
        var jsonConfig = """{"Prefix": "ADR", "LenSeq": 4, "LenVersion": 2}""";

        _mockAdrServices.ParseArgs(args, Arg.Any<Arguments[]>()).Returns(parsedArgs);
        _mockFileSystem.DirectoryExists(RepositoryPath).Returns(true);
        _mockValidateConfig.GetFileNameRepoConfig().Returns(".adrplus");
        _mockFileSystem.FileExists(Arg.Is<string>(s => s.EndsWith(".adrplus"))).Returns(true);
        _mockFileSystem.ReadAllTextAsync(Arg.Is<string>(s => s.EndsWith(".adrplus")), Arg.Any<CancellationToken>()).Returns(jsonConfig);
        _mockValidateConfig.ValidateRepoStructure(jsonConfig).Returns((true, []));
        _mockAdrServices.ReadAllAdr(_mockFileSystem, RepositoryPath, Arg.Any<AdrPlusRepoConfig>(), false).Returns([]);
        _mockPluginManager.RetryPendingAsync(Arg.Any<Func<string, (AdrRecordSnapshot, string, string)?>>(), Arg.Any<RepoInfoSnapshot>(), Arg.Any<Func<LoadedPlugin, bool>>(), Arg.Any<CancellationToken>())
            .Returns(new SyncSummary());

        await _handler.ExecuteAsync(args, TestContext.Current.CancellationToken);

        await _mockPluginManager.Received(1).LoadPluginsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _mockPluginManager.Received(1).RetryPendingAsync(Arg.Any<Func<string, (AdrRecordSnapshot, string, string)?>>(), Arg.Any<RepoInfoSnapshot>(), Arg.Any<Func<LoadedPlugin, bool>>(), Arg.Any<CancellationToken>());
        _mockConsole.Received(1).PromptWriteSuccess(Arg.Any<string>());
    }

    [Fact]
    public async Task ExecuteAsync_HappyPath_LoadsPluginsAndRetriesPending()
    {
        var args = new[] { "--path", RepositoryPath };
        var parsedArgs = new Dictionary<Arguments, string> { { Arguments.TargetRepo, RepositoryPath } };
        var jsonConfig = """{"Prefix": "ADR", "LenSeq": 4, "LenVersion": 2}""";

        _mockAdrServices.ParseArgs(args, Arg.Any<Arguments[]>()).Returns(parsedArgs);
        _mockFileSystem.DirectoryExists(RepositoryPath).Returns(true);
        _mockValidateConfig.GetFileNameRepoConfig().Returns(".adrplus");
        _mockFileSystem.FileExists(Arg.Is<string>(s => s.EndsWith(".adrplus"))).Returns(true);
        _mockFileSystem.ReadAllTextAsync(Arg.Is<string>(s => s.EndsWith(".adrplus")), Arg.Any<CancellationToken>()).Returns(jsonConfig);
        _mockValidateConfig.ValidateRepoStructure(jsonConfig).Returns((true, []));

        var testFile = new AdrFileNameComponents
        {
            FileName = Path.Combine(RepositoryPath, "adr-0001.md"),
            IsValid = true,
            Number = 1,
            Title = "Test ADR",
            ContentAdr = "## Context\n\nTest decision.",
            Header = new AdrHeader
            {
                IsValid = true,
                StatusCreate = AdrPlus.Domain.AdrStatus.Accepted,
                StatusUpdate = AdrPlus.Domain.AdrStatus.Unknown,
                StatusChange = AdrPlus.Domain.AdrStatus.Unknown,
                Version = 1
            }
        };
        _mockAdrServices.ReadAllAdr(_mockFileSystem, RepositoryPath, Arg.Any<AdrPlusRepoConfig>(), false).Returns([testFile]);
        _mockPluginManager.RetryPendingAsync(Arg.Any<Func<string, (AdrRecordSnapshot, string, string)?>>(), Arg.Any<RepoInfoSnapshot>(), Arg.Any<Func<LoadedPlugin, bool>>(), Arg.Any<CancellationToken>())
            .Returns(new SyncSummary { Succeeded = 2, StillPending = 1 });

        await _handler.ExecuteAsync(args, TestContext.Current.CancellationToken);

        await _mockPluginManager.Received(1).LoadPluginsAsync(Path.Combine(RepositoryPath, "plugins"), Arg.Any<CancellationToken>());
        await _mockPluginManager.Received(1).RetryPendingAsync(Arg.Any<Func<string, (AdrRecordSnapshot, string, string)?>>(), Arg.Any<RepoInfoSnapshot>(), Arg.Any<Func<LoadedPlugin, bool>>(), Arg.Any<CancellationToken>());
        _mockConsole.Received(1).PromptWriteSuccess(Arg.Any<string>());
    }

    private static LoadedPlugin CreateDummyLoadedPlugin() => new(
        Substitute.For<IAdrPlugin>(),
        new PluginManifest { Name = "p1", Version = "1.0.0", EntryAssembly = "x.dll", EntryType = "x", AbstractionsVersion = "1.0.0" },
        "/repo/plugins/p1");

    private void ArrangeValidRepoConfig(string[] args, string jsonConfig)
    {
        var parsedArgs = new Dictionary<Arguments, string> { { Arguments.TargetRepo, RepositoryPath } };
        if (args.Contains("--backfill"))
        {
            parsedArgs[Arguments.Backfill] = string.Empty;
        }
        _mockAdrServices.ParseArgs(args, Arg.Any<Arguments[]>()).Returns(parsedArgs);
        _mockFileSystem.DirectoryExists(RepositoryPath).Returns(true);
        _mockValidateConfig.GetFileNameRepoConfig().Returns(".adrplus");
        _mockFileSystem.FileExists(Arg.Is<string>(s => s.EndsWith(".adrplus"))).Returns(true);
        _mockFileSystem.ReadAllTextAsync(Arg.Is<string>(s => s.EndsWith(".adrplus")), Arg.Any<CancellationToken>()).Returns(jsonConfig);
        _mockValidateConfig.ValidateRepoStructure(jsonConfig).Returns((true, []));
    }

    [Fact]
    public async Task ExecuteAsync_WithBackfillAndNoPluginsLoaded_SkipsReadingAdrsEntirely()
    {
        var args = new[] { "--path", RepositoryPath, "--backfill" };
        ArrangeValidRepoConfig(args, """{"Prefix": "ADR", "LenSeq": 4, "LenVersion": 2}""");
        _mockPluginManager.LoadedPlugins.Returns(new List<LoadedPlugin>());

        await _handler.ExecuteAsync(args, TestContext.Current.CancellationToken);

        await _mockPluginManager.Received(1).LoadPluginsAsync(Path.Combine(RepositoryPath, "plugins"), Arg.Any<CancellationToken>());
        await _mockAdrServices.DidNotReceive().ReadAllAdr(Arg.Any<IFileSystemService>(), Arg.Any<string>(), Arg.Any<AdrPlusRepoConfig>(), Arg.Any<bool>());
        await _mockPluginManager.DidNotReceive().BackfillAsync(
            Arg.Any<IEnumerable<(AdrEventType, AdrRecordSnapshot, string, Func<string>)>>(), Arg.Any<RepoInfoSnapshot>(), Arg.Any<Func<LoadedPlugin, bool>>(), Arg.Any<CancellationToken>());
        _mockConsole.Received(1).PromptWriteSuccess(Arg.Any<string>());
    }

    [Fact]
    public async Task ExecuteAsync_WithBackfill_FiltersProposedAndMapsSettledStatusesCorrectly()
    {
        var args = new[] { "--path", RepositoryPath, "--backfill" };
        ArrangeValidRepoConfig(args, """{"Prefix": "ADR", "LenSeq": 4, "LenVersion": 2}""");
        _mockPluginManager.LoadedPlugins.Returns(new List<LoadedPlugin> { CreateDummyLoadedPlugin() });

        static AdrFileNameComponents MakeFile(int number, AdrHeader header) => new()
        {
            FileName = Path.Combine(RepositoryPath, $"adr-{number:D4}.md"),
            IsValid = true,
            Number = number,
            Title = "Test decision",
            ContentAdr = "## Context\n\nTest decision.",
            Header = header
        };

        var proposed = MakeFile(1, new AdrHeader { IsValid = true, StatusCreate = AdrPlus.Domain.AdrStatus.Proposed, Version = 1 });
        var approved = MakeFile(2, new AdrHeader { IsValid = true, StatusCreate = AdrPlus.Domain.AdrStatus.Proposed, StatusUpdate = AdrPlus.Domain.AdrStatus.Accepted, Version = 1 });
        var rejected = MakeFile(3, new AdrHeader { IsValid = true, StatusCreate = AdrPlus.Domain.AdrStatus.Proposed, StatusUpdate = AdrPlus.Domain.AdrStatus.Rejected, Version = 1 });
        var superseded = MakeFile(4, new AdrHeader { IsValid = true, StatusCreate = AdrPlus.Domain.AdrStatus.Proposed, StatusUpdate = AdrPlus.Domain.AdrStatus.Accepted, StatusChange = AdrPlus.Domain.AdrStatus.Superseded, Version = 1 });
        var migrated = MakeFile(5, new AdrHeader { IsValid = false, IsMigrated = true, StatusCreate = AdrPlus.Domain.AdrStatus.Unknown, Version = 1 });

        _mockAdrServices.ReadAllAdr(_mockFileSystem, RepositoryPath, Arg.Any<AdrPlusRepoConfig>(), false)
            .Returns([proposed, approved, rejected, superseded, migrated]);

        IEnumerable<(AdrEventType EventType, AdrRecordSnapshot Adr, string FilePath, Func<string> GetContent)>? captured = null;
        _mockPluginManager.BackfillAsync(
            Arg.Do<IEnumerable<(AdrEventType, AdrRecordSnapshot, string, Func<string>)>>(items => captured = items),
            Arg.Any<RepoInfoSnapshot>(),
            Arg.Any<Func<LoadedPlugin, bool>>(),
            Arg.Any<CancellationToken>())
            .Returns(new SyncSummary { Succeeded = 4 });

        await _handler.ExecuteAsync(args, TestContext.Current.CancellationToken);

        captured.Should().NotBeNull();
        var items = captured!.ToList();
        items.Should().HaveCount(4);
        items.Should().Contain(i => i.Adr.Number == 2 && i.EventType == AdrEventType.Approved);
        items.Should().Contain(i => i.Adr.Number == 3 && i.EventType == AdrEventType.Rejected);
        items.Should().Contain(i => i.Adr.Number == 4 && i.EventType == AdrEventType.Superseded);
        items.Should().Contain(i => i.Adr.Number == 5 && i.EventType == AdrEventType.Migrated);
        items.Should().NotContain(i => i.Adr.Number == 1);
        _mockConsole.Received(1).PromptWriteSuccess(Arg.Any<string>());
    }

    [Fact]
    public async Task ExecuteAsync_WithWizardMode_DefaultSelected_RunsDefaultSyncWithoutExtraConfirm()
    {
        var args = new[] { "--wizard" };
        var parsedArgs = new Dictionary<Arguments, string> { { Arguments.WizardSync, string.Empty } };
        var jsonConfig = """{"Prefix": "ADR", "LenSeq": 4, "LenVersion": 2}""";

        _mockAdrServices.ParseArgs(args, Arg.Any<Arguments[]>()).Returns(parsedArgs);
        _mockFileSystem.GetDrives().Returns([SingleTestDrive]);
        _mockFileSystem.DirectoryExists(RepositoryPath).Returns(true);
        _mockConsole.PromptSelectFolderPath(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<string>(), _mockFileSystem, _mockValidateConfig, Arg.Any<CancellationToken>())
            .Returns((false, RepositoryPath));
        _mockConsole.PromptSelectSyncMode(Arg.Any<CancellationToken>()).Returns((false, false));
        _mockValidateConfig.GetFileNameRepoConfig().Returns(".adrplus");
        _mockFileSystem.FileExists(Arg.Is<string>(s => s.EndsWith(".adrplus"))).Returns(true);
        _mockFileSystem.ReadAllTextAsync(Arg.Is<string>(s => s.EndsWith(".adrplus")), Arg.Any<CancellationToken>()).Returns(jsonConfig);
        _mockValidateConfig.ValidateRepoStructure(jsonConfig).Returns((true, []));
        _mockAdrServices.ReadAllAdr(_mockFileSystem, RepositoryPath, Arg.Any<AdrPlusRepoConfig>(), false).Returns([]);
        _mockPluginManager.RetryPendingAsync(Arg.Any<Func<string, (AdrRecordSnapshot, string, string)?>>(), Arg.Any<RepoInfoSnapshot>(), Arg.Any<Func<LoadedPlugin, bool>>(), Arg.Any<CancellationToken>())
            .Returns(new SyncSummary());

        await _handler.ExecuteAsync(args, TestContext.Current.CancellationToken);

        await _mockPluginManager.Received(1).RetryPendingAsync(Arg.Any<Func<string, (AdrRecordSnapshot, string, string)?>>(), Arg.Any<RepoInfoSnapshot>(), Arg.Any<Func<LoadedPlugin, bool>>(), Arg.Any<CancellationToken>());
        await _mockPluginManager.DidNotReceive().BackfillAsync(Arg.Any<IEnumerable<(AdrEventType, AdrRecordSnapshot, string, Func<string>)>>(), Arg.Any<RepoInfoSnapshot>(), Arg.Any<Func<LoadedPlugin, bool>>(), Arg.Any<CancellationToken>());
        _mockConsole.DidNotReceive().PromptConfirm(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WithWizardMode_BackfillConfirmed_RunsBackfill()
    {
        var args = new[] { "--wizard" };
        var parsedArgs = new Dictionary<Arguments, string> { { Arguments.WizardSync, string.Empty } };
        var jsonConfig = """{"Prefix": "ADR", "LenSeq": 4, "LenVersion": 2}""";

        _mockAdrServices.ParseArgs(args, Arg.Any<Arguments[]>()).Returns(parsedArgs);
        _mockFileSystem.GetDrives().Returns([SingleTestDrive]);
        _mockFileSystem.DirectoryExists(RepositoryPath).Returns(true);
        _mockConsole.PromptSelectFolderPath(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<string>(), _mockFileSystem, _mockValidateConfig, Arg.Any<CancellationToken>())
            .Returns((false, RepositoryPath));
        _mockConsole.PromptSelectSyncMode(Arg.Any<CancellationToken>()).Returns((false, true));
        _mockConsole.PromptConfirm(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((false, true));
        _mockValidateConfig.GetFileNameRepoConfig().Returns(".adrplus");
        _mockFileSystem.FileExists(Arg.Is<string>(s => s.EndsWith(".adrplus"))).Returns(true);
        _mockFileSystem.ReadAllTextAsync(Arg.Is<string>(s => s.EndsWith(".adrplus")), Arg.Any<CancellationToken>()).Returns(jsonConfig);
        _mockValidateConfig.ValidateRepoStructure(jsonConfig).Returns((true, []));
        _mockPluginManager.LoadedPlugins.Returns(new List<LoadedPlugin>());

        await _handler.ExecuteAsync(args, TestContext.Current.CancellationToken);

        _mockConsole.Received(1).PromptConfirm(Arg.Any<string>(), Arg.Any<CancellationToken>());
        // LoadedPlugins is empty, so ExecuteBackfillAsync short-circuits before reading ADRs (Fase 6) —
        // this alone proves the backfill branch (not the default RetryPendingAsync branch) ran.
        await _mockAdrServices.DidNotReceive().ReadAllAdr(Arg.Any<IFileSystemService>(), Arg.Any<string>(), Arg.Any<AdrPlusRepoConfig>(), Arg.Any<bool>());
        await _mockPluginManager.DidNotReceive().RetryPendingAsync(Arg.Any<Func<string, (AdrRecordSnapshot, string, string)?>>(), Arg.Any<RepoInfoSnapshot>(), Arg.Any<Func<LoadedPlugin, bool>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WithWizardMode_BackfillDeclined_ReturnsToModeSelectionAndRunsDefault()
    {
        var args = new[] { "--wizard" };
        var parsedArgs = new Dictionary<Arguments, string> { { Arguments.WizardSync, string.Empty } };
        var jsonConfig = """{"Prefix": "ADR", "LenSeq": 4, "LenVersion": 2}""";

        _mockAdrServices.ParseArgs(args, Arg.Any<Arguments[]>()).Returns(parsedArgs);
        _mockFileSystem.GetDrives().Returns([SingleTestDrive]);
        _mockFileSystem.DirectoryExists(RepositoryPath).Returns(true);
        _mockConsole.PromptSelectFolderPath(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<string>(), _mockFileSystem, _mockValidateConfig, Arg.Any<CancellationToken>())
            .Returns((false, RepositoryPath));
        _mockConsole.PromptSelectSyncMode(Arg.Any<CancellationToken>()).Returns((false, true), (false, false));
        _mockConsole.PromptConfirm(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((false, false));
        _mockValidateConfig.GetFileNameRepoConfig().Returns(".adrplus");
        _mockFileSystem.FileExists(Arg.Is<string>(s => s.EndsWith(".adrplus"))).Returns(true);
        _mockFileSystem.ReadAllTextAsync(Arg.Is<string>(s => s.EndsWith(".adrplus")), Arg.Any<CancellationToken>()).Returns(jsonConfig);
        _mockValidateConfig.ValidateRepoStructure(jsonConfig).Returns((true, []));
        _mockAdrServices.ReadAllAdr(_mockFileSystem, RepositoryPath, Arg.Any<AdrPlusRepoConfig>(), false).Returns([]);
        _mockPluginManager.RetryPendingAsync(Arg.Any<Func<string, (AdrRecordSnapshot, string, string)?>>(), Arg.Any<RepoInfoSnapshot>(), Arg.Any<Func<LoadedPlugin, bool>>(), Arg.Any<CancellationToken>())
            .Returns(new SyncSummary());

        await _handler.ExecuteAsync(args, TestContext.Current.CancellationToken);

        _mockConsole.Received(1).PromptConfirm(Arg.Any<string>(), Arg.Any<CancellationToken>());
        _mockConsole.Received(2).PromptSelectSyncMode(Arg.Any<CancellationToken>());
        await _mockPluginManager.Received(1).RetryPendingAsync(Arg.Any<Func<string, (AdrRecordSnapshot, string, string)?>>(), Arg.Any<RepoInfoSnapshot>(), Arg.Any<Func<LoadedPlugin, bool>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WithWizardMode_FolderAborted_ThrowsOperationCanceledException()
    {
        var args = new[] { "--wizard" };
        var parsedArgs = new Dictionary<Arguments, string> { { Arguments.WizardSync, string.Empty } };
        _mockAdrServices.ParseArgs(args, Arg.Any<Arguments[]>()).Returns(parsedArgs);
        _mockFileSystem.GetDrives().Returns([SingleTestDrive]);
        _mockConsole.PromptSelectFolderPath(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<string>(), _mockFileSystem, _mockValidateConfig, Arg.Any<CancellationToken>())
            .Returns((true, string.Empty));

        await _handler.Invoking(h => h.ExecuteAsync(args, TestContext.Current.CancellationToken))
            .Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task ExecuteAsync_WithWizardMode_ModeAborted_ThrowsOperationCanceledException()
    {
        var args = new[] { "--wizard" };
        var parsedArgs = new Dictionary<Arguments, string> { { Arguments.WizardSync, string.Empty } };
        _mockAdrServices.ParseArgs(args, Arg.Any<Arguments[]>()).Returns(parsedArgs);
        _mockFileSystem.GetDrives().Returns([SingleTestDrive]);
        _mockConsole.PromptSelectFolderPath(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<string>(), _mockFileSystem, _mockValidateConfig, Arg.Any<CancellationToken>())
            .Returns((false, RepositoryPath));
        _mockConsole.PromptSelectSyncMode(Arg.Any<CancellationToken>()).Returns((true, false));

        await _handler.Invoking(h => h.ExecuteAsync(args, TestContext.Current.CancellationToken))
            .Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task ExecuteAsync_WithWizardMode_BackfillConfirmAborted_ThrowsOperationCanceledException()
    {
        var args = new[] { "--wizard" };
        var parsedArgs = new Dictionary<Arguments, string> { { Arguments.WizardSync, string.Empty } };
        _mockAdrServices.ParseArgs(args, Arg.Any<Arguments[]>()).Returns(parsedArgs);
        _mockFileSystem.GetDrives().Returns([SingleTestDrive]);
        _mockConsole.PromptSelectFolderPath(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<string>(), _mockFileSystem, _mockValidateConfig, Arg.Any<CancellationToken>())
            .Returns((false, RepositoryPath));
        _mockConsole.PromptSelectSyncMode(Arg.Any<CancellationToken>()).Returns((false, true));
        _mockConsole.PromptConfirm(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((true, false));

        await _handler.Invoking(h => h.ExecuteAsync(args, TestContext.Current.CancellationToken))
            .Should().ThrowAsync<OperationCanceledException>();
    }
}
