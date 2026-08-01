// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the AdrPlus project under MIT license
// ***************************************************************************************

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
        _mockPluginManager.RetryPendingAsync(Arg.Any<Func<string, (AdrRecordSnapshot, string, string)?>>(), Arg.Any<RepoInfoSnapshot>(), Arg.Any<CancellationToken>())
            .Returns(new SyncSummary());

        await _handler.ExecuteAsync(args, TestContext.Current.CancellationToken);

        await _mockPluginManager.Received(1).LoadPluginsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _mockPluginManager.Received(1).RetryPendingAsync(Arg.Any<Func<string, (AdrRecordSnapshot, string, string)?>>(), Arg.Any<RepoInfoSnapshot>(), Arg.Any<CancellationToken>());
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
        _mockPluginManager.RetryPendingAsync(Arg.Any<Func<string, (AdrRecordSnapshot, string, string)?>>(), Arg.Any<RepoInfoSnapshot>(), Arg.Any<CancellationToken>())
            .Returns(new SyncSummary { Succeeded = 2, StillPending = 1 });

        await _handler.ExecuteAsync(args, TestContext.Current.CancellationToken);

        await _mockPluginManager.Received(1).LoadPluginsAsync(Path.Combine(RepositoryPath, "plugins"), Arg.Any<CancellationToken>());
        await _mockPluginManager.Received(1).RetryPendingAsync(Arg.Any<Func<string, (AdrRecordSnapshot, string, string)?>>(), Arg.Any<RepoInfoSnapshot>(), Arg.Any<CancellationToken>());
        _mockConsole.Received(1).PromptWriteSuccess(Arg.Any<string>());
    }
}
