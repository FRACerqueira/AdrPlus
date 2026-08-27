// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the AdrPlus project under MIT license
// ***************************************************************************************

using AdrPlus.Commands;
using AdrPlus.Commands.Init;
using AdrPlus.Core;
using AdrPlus.Domain;
using AdrPlus.Infrastructure.FileSystem;
using AdrPlus.Infrastructure.UI;
using AdrPlus.Plugins;
using Microsoft.Extensions.Logging;
using static AdrPlus.Tests.Helpers.TestPathData;

namespace AdrPlus.Tests.Commands.Init;

/// <summary>
/// Unit tests for InitCommandHandler class.
/// Tests demonstrate init command execution, repository initialization, and wizard flows using NSubstitute.
/// </summary>
public class InitCommandHandlerTests
{
    private readonly ILogger<InitCommandHandler> _mockLogger;
    private readonly IFileSystemService _mockFileSystem;
    private readonly IConsoleWriter _mockConsole;
    private readonly IValidateConfig _mockValidateConfig;
    private readonly IAdrServices _mockAdrServices;
    private readonly IPluginManager _mockPluginManager;
    private readonly AdrPlusConfig _config;
    private readonly InitCommandHandler _handler;

    public InitCommandHandlerTests()
    {
        _mockLogger = Substitute.For<ILogger<InitCommandHandler>>();
        _mockFileSystem = Substitute.For<IFileSystemService>();
        _mockConsole = Substitute.For<IConsoleWriter>();
        _mockValidateConfig = Substitute.For<IValidateConfig>();
        _mockAdrServices = Substitute.For<IAdrServices>();
        _mockPluginManager = Substitute.For<IPluginManager>();
        _mockPluginManager.LoadedPlugins.Returns(new List<LoadedPlugin>());

        _config = new AdrPlusConfig
        {
        };


        _handler = new InitCommandHandler(
            _mockLogger,
            _mockFileSystem,
            _mockValidateConfig,
            _mockConsole,
            _mockAdrServices,
            _mockPluginManager);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidParameters_CreatesInstance()
    {
        // Arrange & Act
        var handler = new InitCommandHandler(
            _mockLogger,
            _mockFileSystem,
            _mockValidateConfig,
            _mockConsole,
            _mockAdrServices,
            _mockPluginManager);

        // Assert
        handler.Should().NotBeNull();
    }

    #endregion

    #region ExecuteAsync - Help Tests

    [Fact]
    public async Task ExecuteAsync_WithHelpArgument_WritesHelpToConsole()
    {
        // Arrange
        var args = new[] { "--help" };
        var parsedArgs = new Dictionary<Arguments, string> { { Arguments.Help, string.Empty } };
        _mockAdrServices.ParseArgs(args, Arg.Any<Arguments[]>()).Returns(parsedArgs);
        _mockAdrServices.GetHelpText(Arg.Any<string>(), Arg.Any<Arguments[]>(), Arg.Any<string[]>())
            .Returns("Help text");

        // Act
        await _handler.ExecuteAsync(args, TestContext.Current.CancellationToken);

        // Assert
        _mockConsole.Received(1).PromptWriteHelp("Help text");
    }

    #endregion

    #region ExecuteAsync - Direct Path Tests

    [Fact]
    public async Task ExecuteAsync_WithValidPath_InitializesRepository()
    {
        // Arrange
        var args = new[] { "--path", InitRepositoryPath };
        var parsedArgs = new Dictionary<Arguments, string> { { Arguments.TargetRepo, InitRepositoryPath } };
        var repoPath = InitRepositoryAdrPath;
        var configPath = Path.Combine(repoPath, ".adrplus");
        var jsonConfig = """{"Prefix": "ADR", "LenSeq": 4, "FolderByScope": false}""";

        _mockAdrServices.ParseArgs(args, Arg.Any<Arguments[]>()).Returns(parsedArgs);
        _mockValidateConfig.HasTemplateRepoFile().Returns(true);
        _mockFileSystem.DirectoryExists(InitRepositoryPath).Returns(true);
        _mockFileSystem.DirectoryExists(repoPath).Returns(false);
        _mockFileSystem.FileExists(Arg.Any<string>()).Returns(false);
        _mockValidateConfig.GetFileNameRepoConfig().Returns(".adrplus");
        _mockValidateConfig.GetDefaultConfigRepoFilePath().Returns("template-path");
        _mockValidateConfig.GetConfigDefaultRepoContentAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(jsonConfig);
        _mockValidateConfig.ValidateRepoStructure(jsonConfig).Returns((true, []));
        _mockValidateConfig.GetMaxNumberVersionRevision(Arg.Any<string>(), Arg.Any<AdrPlusRepoConfig>()).Returns((0, 0, 0));
        _mockFileSystem.CreateDirectory(repoPath).Returns(repoPath);
        _mockFileSystem.GetFullNameFile(Arg.Any<string>()).Returns(configPath);

        var repoConfig = new AdrPlusRepoConfig("", "");
        _mockAdrServices.FromJson(jsonConfig, "").Returns(repoConfig);

        // Act
        await _handler.ExecuteAsync(args, TestContext.Current.CancellationToken);

        // Assert
        await _mockFileSystem.Received(1).WriteAllTextAsync(Arg.Any<string>(), jsonConfig, Arg.Any<CancellationToken>());
        _mockConsole.Received().PromptWriteSuccess(Arg.Any<string>());
    }

    [Fact]
    public async Task ExecuteAsync_WithEmptyPath_InitializesInCurrentDirectory()
    {
        // Arrange
        var currentDir = Directory.GetCurrentDirectory();
        var args = new[] { "--path", currentDir };
        var parsedArgs = new Dictionary<Arguments, string> { { Arguments.TargetRepo, currentDir } };
        var repoPath = Path.Combine(currentDir, "docs", "adr");
        var configPath = Path.Combine(repoPath, ".adrplus");
        var jsonConfig = """{"Prefix": "ADR", "LenSeq": 4, "FolderByScope": false}""";

        _mockAdrServices.ParseArgs(args, Arg.Any<Arguments[]>()).Returns(parsedArgs);
        _mockValidateConfig.HasTemplateRepoFile().Returns(true);
        _mockFileSystem.DirectoryExists(currentDir).Returns(true);
        _mockFileSystem.DirectoryExists(repoPath).Returns(false);
        _mockFileSystem.FileExists(Arg.Any<string>()).Returns(false);
        _mockValidateConfig.GetFileNameRepoConfig().Returns(".adrplus");
        _mockValidateConfig.GetDefaultConfigRepoFilePath().Returns("template-path");
        _mockValidateConfig.GetConfigDefaultRepoContentAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(jsonConfig);
        _mockValidateConfig.ValidateRepoStructure(jsonConfig).Returns((true, []));
        _mockValidateConfig.GetMaxNumberVersionRevision(Arg.Any<string>(), Arg.Any<AdrPlusRepoConfig>()).Returns((0, 0, 0));
        _mockFileSystem.CreateDirectory(repoPath).Returns(repoPath);
        _mockFileSystem.GetFullNameFile(Arg.Any<string>()).Returns(configPath);

        var repoConfig = new AdrPlusRepoConfig("", "");
        _mockAdrServices.FromJson(jsonConfig, "").Returns(repoConfig);

        // Act
        await _handler.ExecuteAsync(args, TestContext.Current.CancellationToken);

        // Assert
        await _mockFileSystem.Received(1).WriteAllTextAsync(Arg.Any<string>(), jsonConfig, Arg.Any<CancellationToken>());
        _mockConsole.Received().PromptWriteSuccess(Arg.Any<string>());
    }

    [Fact]
    public async Task ExecuteAsync_WhenDirectoryNotFound_ThrowsDirectoryNotFoundException()
    {
        // Arrange
        var args = new[] { "--path", NonexistentPath };
        var parsedArgs = new Dictionary<Arguments, string> { { Arguments.TargetRepo, NonexistentPath } };

        _mockAdrServices.ParseArgs(args, Arg.Any<Arguments[]>()).Returns(parsedArgs);
        _mockValidateConfig.HasTemplateRepoFile().Returns(true);
        _mockFileSystem.DirectoryExists(NonexistentPath).Returns(false);

        // Act & Assert
        await _handler.Invoking(h => h.ExecuteAsync(args, TestContext.Current.CancellationToken))
            .Should().ThrowAsync<DirectoryNotFoundException>();
    }

    [Fact]
    public async Task ExecuteAsync_WhenConfigFileAlreadyExists_ThrowsInvalidOperationException()
    {
        // Arrange
        var args = new[] { "--path", InitRepositoryPath };
        var parsedArgs = new Dictionary<Arguments, string> { { Arguments.TargetRepo, InitRepositoryPath } };
        var repoPath = InitRepositoryAdrPath;
        var configPath = Path.Combine(repoPath, ".adrplus");

        _mockAdrServices.ParseArgs(args, Arg.Any<Arguments[]>()).Returns(parsedArgs);
        _mockValidateConfig.HasTemplateRepoFile().Returns(true);
        _mockFileSystem.DirectoryExists(InitRepositoryPath).Returns(true);
        _mockValidateConfig.GetFileNameRepoConfig().Returns(".adrplus");
        _mockFileSystem.FileExists(Arg.Is<string>(s => s.Contains(".adrplus"))).Returns(true);

        // Act & Assert
        await _handler.Invoking(h => h.ExecuteAsync(args, TestContext.Current.CancellationToken))
            .Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task ExecuteAsync_WhenInvalidRepoConfig_ThrowsInvalidOperationException()
    {
        // Arrange
        var args = new[] { "--path", InitRepositoryPath };
        var parsedArgs = new Dictionary<Arguments, string> { { Arguments.TargetRepo, InitRepositoryPath } };
        var jsonConfig = """{"Invalid": "config"}""";
        var errors = new[] { "Missing Prefix field" };

        _mockAdrServices.ParseArgs(args, Arg.Any<Arguments[]>()).Returns(parsedArgs);
        _mockValidateConfig.HasTemplateRepoFile().Returns(true);
        _mockFileSystem.DirectoryExists(InitRepositoryPath).Returns(true);
        _mockFileSystem.FileExists(Arg.Any<string>()).Returns(false);
        _mockValidateConfig.GetFileNameRepoConfig().Returns(".adrplus");
        _mockValidateConfig.GetDefaultConfigRepoFilePath().Returns("template-path");
        _mockValidateConfig.GetConfigDefaultRepoContentAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(jsonConfig);
        _mockValidateConfig.ValidateRepoStructure(jsonConfig).Returns((false, errors));

        // Act & Assert
        await _handler.Invoking(h => h.ExecuteAsync(args, TestContext.Current.CancellationToken))
            .Should().ThrowAsync<InvalidOperationException>();
    }

    #endregion

    #region ExecuteAsync - Wizard Mode Tests

    [Fact]
    public async Task ExecuteAsync_WithWizardMode_ProcessesWizardFlow()
    {
        // Arrange
        var args = new[] { "--wizard" };
        var parsedArgs = new Dictionary<Arguments, string> { { Arguments.WizardInit, string.Empty } };
        var drives = new[] { SingleTestDrive };
        var selectedPath = ProjectRepositoryPath;
        var repoPath = ProjectRepositoryAdrPath;
        var configPath = Path.Combine(repoPath, ".adrplus");
        var jsonConfig = """{"Prefix": "ADR", "LenSeq": 4, "FolderByScope": false}""";

        _mockAdrServices.ParseArgs(args, Arg.Any<Arguments[]>()).Returns(parsedArgs);
        _mockValidateConfig.HasTemplateRepoFile().Returns(true);
        _mockFileSystem.GetDrives().Returns(drives);
        _mockConsole.PromptSelectFolderPath(Arg.Any<string>(), false, SingleTestDrive, _mockFileSystem, _mockValidateConfig, Arg.Any<CancellationToken>())
            .Returns((false, selectedPath));
        _mockFileSystem.DirectoryExists(selectedPath).Returns(true);
        _mockFileSystem.DirectoryExists(repoPath).Returns(false);
        _mockFileSystem.FileExists(Arg.Any<string>()).Returns(false);
        _mockValidateConfig.GetFileNameRepoConfig().Returns(".adrplus");
        _mockValidateConfig.GetDefaultConfigRepoFilePath().Returns("template-path");
        _mockValidateConfig.GetConfigDefaultRepoContentAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(jsonConfig);
        _mockValidateConfig.ValidateRepoStructure(jsonConfig).Returns((true, []));
        _mockValidateConfig.GetMaxNumberVersionRevision(Arg.Any<string>(), Arg.Any<AdrPlusRepoConfig>()).Returns((0, 0, 0));
        _mockFileSystem.CreateDirectory(Arg.Any<string>()).Returns(callInfo => callInfo.Arg<string>());
        _mockFileSystem.GetFullNameFile(Arg.Any<string>()).Returns(configPath);

        var repoConfig = new AdrPlusRepoConfig("", "");
        _mockAdrServices.FromJson(jsonConfig, "").Returns(repoConfig);

        // Act
        await _handler.ExecuteAsync(args, TestContext.Current.CancellationToken);

        // Assert
        _mockConsole.Received(1).PromptSelectFolderPath(Arg.Any<string>(), false, SingleTestDrive, _mockFileSystem, _mockValidateConfig, Arg.Any<CancellationToken>());
        await _mockFileSystem.Received(1).WriteAllTextAsync(Arg.Any<string>(), jsonConfig, Arg.Any<CancellationToken>());
        _mockConsole.Received().PromptWriteSuccess(Arg.Any<string>());
    }

    [Fact]
    public async Task ExecuteAsync_WithWizardModeMultipleDrives_PromptsDriveSelection()
    {
        // Arrange
        var args = new[] { "--wizard" };
        var parsedArgs = new Dictionary<Arguments, string> { { Arguments.WizardInit, string.Empty } };
        var drives = MultipleTestDrives;
        var selectedDrive = AlternativeDrivePath;
        var selectedPath = AlternativeDriveProjectPath;
        var repoPath = Path.Combine(selectedPath, "docs", "adr");
        var configPath = Path.Combine(repoPath, ".adrplus");
        var jsonConfig = """{"Prefix": "ADR", "LenSeq": 4, "FolderByScope": false}""";

        _mockAdrServices.ParseArgs(args, Arg.Any<Arguments[]>()).Returns(parsedArgs);
        _mockValidateConfig.HasTemplateRepoFile().Returns(true);
        _mockFileSystem.GetDrives().Returns(drives);
        _mockConsole.PromptSelectLogicalDrive(Arg.Any<string>(), _mockFileSystem, Arg.Any<CancellationToken>())
            .Returns((false, selectedDrive));
        _mockConsole.PromptSelectFolderPath(Arg.Any<string>(), false, selectedDrive, _mockFileSystem, _mockValidateConfig, Arg.Any<CancellationToken>())
            .Returns((false, selectedPath));
        _mockFileSystem.DirectoryExists(selectedPath).Returns(true);
        _mockFileSystem.DirectoryExists(repoPath).Returns(false);
        _mockFileSystem.FileExists(Arg.Any<string>()).Returns(false);
        _mockValidateConfig.GetFileNameRepoConfig().Returns(".adrplus");
        _mockValidateConfig.GetDefaultConfigRepoFilePath().Returns("template-path");
        _mockValidateConfig.GetConfigDefaultRepoContentAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(jsonConfig);
        _mockValidateConfig.ValidateRepoStructure(jsonConfig).Returns((true, []));
        _mockValidateConfig.GetMaxNumberVersionRevision(Arg.Any<string>(), Arg.Any<AdrPlusRepoConfig>()).Returns((0, 0, 0));
        _mockFileSystem.CreateDirectory(Arg.Any<string>()).Returns(callInfo => callInfo.Arg<string>());
        _mockFileSystem.GetFullNameFile(Arg.Any<string>()).Returns(configPath);

        var repoConfig = new AdrPlusRepoConfig("", "");
        _mockAdrServices.FromJson(jsonConfig, "").Returns(repoConfig);

        // Act
        await _handler.ExecuteAsync(args, TestContext.Current.CancellationToken);

        // Assert
        _mockConsole.Received(1).PromptSelectLogicalDrive(Arg.Any<string>(), _mockFileSystem, Arg.Any<CancellationToken>());
        _mockConsole.Received(1).PromptSelectFolderPath(Arg.Any<string>(), false, selectedDrive, _mockFileSystem, _mockValidateConfig, Arg.Any<CancellationToken>());
        await _mockFileSystem.Received(1).WriteAllTextAsync(Arg.Any<string>(), jsonConfig, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WithWizardModeDriveSelectionAborted_ThrowsOperationCanceledException()
    {
        // Arrange
        var args = new[] { "--wizard" };
        var parsedArgs = new Dictionary<Arguments, string> { { Arguments.WizardInit, string.Empty } };
        var drives = MultipleTestDrives;

        _mockAdrServices.ParseArgs(args, Arg.Any<Arguments[]>()).Returns(parsedArgs);
        _mockValidateConfig.HasTemplateRepoFile().Returns(true);
        _mockFileSystem.GetDrives().Returns(drives);
        _mockConsole.PromptSelectLogicalDrive(Arg.Any<string>(), _mockFileSystem, Arg.Any<CancellationToken>())
            .Returns((true, string.Empty));

        // Act & Assert
        await _handler.Invoking(h => h.ExecuteAsync(args, TestContext.Current.CancellationToken))
            .Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task ExecuteAsync_WithWizardModeFolderSelectionAborted_ThrowsOperationCanceledException()
    {
        // Arrange
        var args = new[] { "--wizard" };
        var parsedArgs = new Dictionary<Arguments, string> { { Arguments.WizardInit, string.Empty } };
        var drives = new[] { SingleTestDrive };

        _mockAdrServices.ParseArgs(args, Arg.Any<Arguments[]>()).Returns(parsedArgs);
        _mockValidateConfig.HasTemplateRepoFile().Returns(true);
        _mockFileSystem.GetDrives().Returns(drives);
        _mockConsole.PromptSelectFolderPath(Arg.Any<string>(), false, SingleTestDrive, _mockFileSystem, _mockValidateConfig, Arg.Any<CancellationToken>())
            .Returns((true, string.Empty));

        // Act & Assert
        await _handler.Invoking(h => h.ExecuteAsync(args, TestContext.Current.CancellationToken))
            .Should().ThrowAsync<OperationCanceledException>();
    }

    #endregion

    #region ADR Folder Creation Tests

    [Fact]
    public async Task ExecuteAsync_WhenAdrFolderDoesNotExist_CreatesAdrFolder()
    {
        // Arrange
        var args = new[] { "--path", InitRepositoryPath };
        var parsedArgs = new Dictionary<Arguments, string> { { Arguments.TargetRepo, InitRepositoryPath } };
        var jsonConfig = """{"Prefix": "ADR", "LenSeq": 4, "FolderAdr": "adr", "FolderByScope": false}""";

        _mockAdrServices.ParseArgs(args, Arg.Any<Arguments[]>()).Returns(parsedArgs);
        _mockValidateConfig.HasTemplateRepoFile().Returns(true);
        _mockFileSystem.DirectoryExists(Arg.Is<string>(s => s == InitRepositoryPath)).Returns(true);
        _mockFileSystem.DirectoryExists(Arg.Is<string>(s => s != InitRepositoryPath)).Returns(false);
        _mockFileSystem.FileExists(Arg.Any<string>()).Returns(false);
        _mockValidateConfig.GetFileNameRepoConfig().Returns(".adrplus");
        _mockValidateConfig.GetDefaultConfigRepoFilePath().Returns("template-path");
        _mockValidateConfig.GetConfigDefaultRepoContentAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(jsonConfig);
        _mockValidateConfig.ValidateRepoStructure(jsonConfig).Returns((true, []));
        _mockValidateConfig.GetMaxNumberVersionRevision(Arg.Any<string>(), Arg.Any<AdrPlusRepoConfig>()).Returns((0, 0, 0));
        _mockFileSystem.CreateDirectory(Arg.Any<string>()).Returns(callInfo => callInfo.Arg<string>());
        _mockFileSystem.GetFullNameFile(Arg.Any<string>()).Returns("configPath");

        var repoConfig = new AdrPlusRepoConfig("", "") { FolderAdr = "adr" };
        _mockAdrServices.FromJson(jsonConfig, "").Returns(repoConfig);

        // Act
        await _handler.ExecuteAsync(args, TestContext.Current.CancellationToken);

        // Assert
        _mockFileSystem.Received(1).CreateDirectory(Arg.Is<string>(s => s.EndsWith("adr", StringComparison.OrdinalIgnoreCase)));
    }

    [Fact]
    public async Task ExecuteAsync_WhenAdrFolderExists_DoesNotCreateAdrFolder()
    {
        // Arrange
        var args = new[] { "--path", InitRepositoryPath };
        var parsedArgs = new Dictionary<Arguments, string> { { Arguments.TargetRepo, InitRepositoryPath } };
        var repoPath = InitRepositoryAdrPath;
        var configPath = Path.Combine(repoPath, ".adrplus");
        var jsonConfig = """{"Prefix": "ADR", "LenSeq": 4, "FolderByScope": false}""";

        _mockAdrServices.ParseArgs(args, Arg.Any<Arguments[]>()).Returns(parsedArgs);
        _mockValidateConfig.HasTemplateRepoFile().Returns(true);
        _mockFileSystem.DirectoryExists(InitRepositoryPath).Returns(true);
        _mockFileSystem.DirectoryExists(repoPath).Returns(true);
        _mockFileSystem.FileExists(Arg.Any<string>()).Returns(false);
        _mockValidateConfig.GetFileNameRepoConfig().Returns(".adrplus");
        _mockValidateConfig.GetDefaultConfigRepoFilePath().Returns("template-path");
        _mockValidateConfig.GetConfigDefaultRepoContentAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(jsonConfig);
        _mockValidateConfig.ValidateRepoStructure(jsonConfig).Returns((true, []));
        _mockValidateConfig.GetMaxNumberVersionRevision(Arg.Any<string>(), Arg.Any<AdrPlusRepoConfig>()).Returns((0, 0, 0));
        _mockFileSystem.GetFullNameFile(Arg.Any<string>()).Returns(configPath);

        var repoConfig = new AdrPlusRepoConfig("", "");
        _mockAdrServices.FromJson(jsonConfig, "").Returns(repoConfig);

        // Act
        await _handler.ExecuteAsync(args, TestContext.Current.CancellationToken);

        // Assert
        _mockFileSystem.DidNotReceive().CreateDirectory(repoPath);
    }

    #endregion

    #region Cancellation Tests

    [Fact]
    public async Task ExecuteAsync_WhenCancelled_ThrowsOperationCanceledException()
    {
        // Arrange
        var args = new[] { "--path", "C:\\repo" };
        var parsedArgs = new Dictionary<Arguments, string> { { Arguments.TargetRepo, "C:\\repo" } };
        var cts = new CancellationTokenSource();
        cts.Cancel();

        _mockAdrServices.ParseArgs(args, Arg.Any<Arguments[]>()).Returns(parsedArgs);
        _mockValidateConfig.HasTemplateRepoFile().Returns(true);
        _mockFileSystem.DirectoryExists("C:\\repo").Returns(true);
        _mockFileSystem.FileExists(Arg.Any<string>()).Returns(false);
        _mockValidateConfig.GetFileNameRepoConfig().Returns(".adrplus");
        _mockValidateConfig.GetDefaultConfigRepoFilePath().Returns("template-path");
        _mockValidateConfig.GetConfigDefaultRepoContentAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns<string>(callInfo => throw new OperationCanceledException());

        // Act & Assert
        await _handler.Invoking(h => h.ExecuteAsync(args, cts.Token))
            .Should().ThrowAsync<OperationCanceledException>();
    }

    #endregion

    #region Exception Handling Tests

    [Fact]
    public async Task ExecuteAsync_WhenExceptionOccurs_LogsException()
    {
        // Arrange
        var args = new[] { "--path", "C:\\repo" };
        var parsedArgs = new Dictionary<Arguments, string> { { Arguments.TargetRepo, "C:\\repo" } };
        var exception = new InvalidOperationException("Test exception");

        _mockAdrServices.ParseArgs(args, Arg.Any<Arguments[]>()).Returns(parsedArgs);
        _mockFileSystem.When(x => x.DirectoryExists(Arg.Any<string>())).Do(x => throw exception);

        // Act & Assert
        await _handler.Invoking(h => h.ExecuteAsync(args, TestContext.Current.CancellationToken))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Test exception");
    }

    #endregion

    #region Success Output Tests

    [Fact]
    public async Task ExecuteAsync_OnSuccess_WritesAllCreatedPathsToConsole()
    {
        // Arrange
        var args = new[] { "--path", InitRepositoryPath };
        var parsedArgs = new Dictionary<Arguments, string> { { Arguments.TargetRepo, InitRepositoryPath } };
        var repoPath = InitRepositoryAdrPath;
        var configPath = Path.Combine(InitRepositoryPath, ".adrplus");
        var jsonConfig = """{"Prefix": "ADR", "LenSeq": 4}""";

        _mockAdrServices.ParseArgs(args, Arg.Any<Arguments[]>()).Returns(parsedArgs);
        _mockValidateConfig.HasTemplateRepoFile().Returns(true);
        _mockFileSystem.DirectoryExists(InitRepositoryPath).Returns(true);
        _mockFileSystem.DirectoryExists(repoPath).Returns(false);
        _mockFileSystem.FileExists(Arg.Any<string>()).Returns(false);
        _mockValidateConfig.GetFileNameRepoConfig().Returns(".adrplus");
        _mockValidateConfig.GetDefaultConfigRepoFilePath().Returns("template-path");
        _mockValidateConfig.GetConfigDefaultRepoContentAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(jsonConfig);
        _mockValidateConfig.ValidateRepoStructure(jsonConfig).Returns((true, []));
        _mockValidateConfig.GetMaxNumberVersionRevision(Arg.Any<string>(), Arg.Any<AdrPlusRepoConfig>()).Returns((0, 0, 0));
        _mockFileSystem.CreateDirectory(Arg.Any<string>()).Returns(callInfo => callInfo.Arg<string>());
        _mockFileSystem.GetFullNameFile(Arg.Any<string>()).Returns(configPath);

        var repoConfig = new AdrPlusRepoConfig("", "");
        _mockAdrServices.FromJson(jsonConfig, "").Returns(repoConfig);

        // Act
        await _handler.ExecuteAsync(args, TestContext.Current.CancellationToken);

        // Assert
        _mockConsole.Received().PromptWriteSuccess(configPath);
    }

    #endregion

    #region Coverage Enhancement Tests


    [Fact]
    public async Task ExecuteAsync_WithCustomFolderAdrName_CreatesCustomFolderSuccessfully()
    {
        // Arrange - Test with non-default FolderAdr value
        var args = new[] { "--path", InitRepositoryPath };
        var parsedArgs = new Dictionary<Arguments, string> { { Arguments.TargetRepo, InitRepositoryPath } };
        var repoPath = Path.Combine(InitRepositoryPath, "decisions");
        var configPath = Path.Combine(InitRepositoryPath, ".adrplus");
        var jsonConfig = """{"Prefix": "ADR", "LenSeq": 4, "FolderAdr": "decisions", "FolderByScope": false}""";

        _mockAdrServices.ParseArgs(args, Arg.Any<Arguments[]>()).Returns(parsedArgs);
        _mockValidateConfig.HasTemplateRepoFile().Returns(true);
        _mockFileSystem.DirectoryExists(InitRepositoryPath).Returns(true);
        _mockFileSystem.DirectoryExists(repoPath).Returns(false);
        _mockFileSystem.FileExists(Arg.Any<string>()).Returns(false);
        _mockValidateConfig.GetFileNameRepoConfig().Returns(".adrplus");
        _mockValidateConfig.GetDefaultConfigRepoFilePath().Returns("template-path");
        _mockValidateConfig.GetConfigDefaultRepoContentAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(jsonConfig);
        _mockValidateConfig.ValidateRepoStructure(jsonConfig).Returns((true, []));
        _mockValidateConfig.GetMaxNumberVersionRevision(Arg.Any<string>(), Arg.Any<AdrPlusRepoConfig>()).Returns((0, 0, 0));
        _mockFileSystem.CreateDirectory(Arg.Any<string>()).Returns(callInfo => callInfo.Arg<string>());
        _mockFileSystem.GetFullNameFile(Arg.Any<string>()).Returns(configPath);

        var repoConfig = new AdrPlusRepoConfig("", "") { FolderAdr = "decisions" };
        _mockAdrServices.FromJson(jsonConfig, "").Returns(repoConfig);

        // Act
        await _handler.ExecuteAsync(args, TestContext.Current.CancellationToken);

        // Assert - Should create custom folder name
        _mockFileSystem.Received(1).CreateDirectory(Arg.Is<string>(s => s.Contains("decisions")));
        await _mockFileSystem.Received(1).WriteAllTextAsync(Arg.Any<string>(), jsonConfig, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WithDynamicFolderAdrName_CreatesCorrectlyNamedFolder()
    {
        // Arrange - Test with different FolderAdr value (not nested, not empty)
        var args = new[] { "--path", InitRepositoryPath };
        var parsedArgs = new Dictionary<Arguments, string> { { Arguments.TargetRepo, InitRepositoryPath } };
        var folderAdrName = "specifications";
        var repoPath = Path.Combine(InitRepositoryPath, folderAdrName);
        var configPath = Path.Combine(InitRepositoryPath, ".adrplus");
        var jsonConfig = """{"Prefix": "ADR", "LenSeq": 4, "FolderAdr": "specifications", "FolderByScope": false}""";

        _mockAdrServices.ParseArgs(args, Arg.Any<Arguments[]>()).Returns(parsedArgs);
        _mockValidateConfig.HasTemplateRepoFile().Returns(true);
        _mockFileSystem.DirectoryExists(InitRepositoryPath).Returns(true);
        _mockFileSystem.DirectoryExists(repoPath).Returns(false);
        _mockFileSystem.FileExists(Arg.Any<string>()).Returns(false);
        _mockValidateConfig.GetFileNameRepoConfig().Returns(".adrplus");
        _mockValidateConfig.GetDefaultConfigRepoFilePath().Returns("template-path");
        _mockValidateConfig.GetConfigDefaultRepoContentAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(jsonConfig);
        _mockValidateConfig.ValidateRepoStructure(jsonConfig).Returns((true, []));
        _mockValidateConfig.GetMaxNumberVersionRevision(Arg.Any<string>(), Arg.Any<AdrPlusRepoConfig>()).Returns((0, 0, 0));
        _mockFileSystem.CreateDirectory(Arg.Any<string>()).Returns(callInfo => callInfo.Arg<string>());
        _mockFileSystem.GetFullNameFile(Arg.Any<string>()).Returns(configPath);

        var repoConfig = new AdrPlusRepoConfig("", "") { FolderAdr = folderAdrName };
        _mockAdrServices.FromJson(jsonConfig, "").Returns(repoConfig);

        // Act
        await _handler.ExecuteAsync(args, TestContext.Current.CancellationToken);

        // Assert - Should create folder with specified name
        _mockFileSystem.Received(1).CreateDirectory(Arg.Is<string>(s => s.Contains(folderAdrName)));
    }


    #endregion

    #region Wizard Cancellation and Retry Tests

    [Fact]
    public async Task ExecuteAsync_WithWizardDriveSelectionSuccessThenFolderCancellation_ThrowsOperationCancelledException()
    {
        // Arrange - Successful drive selection followed by folder selection cancellation
        var args = new[] { "--wizard" };
        var parsedArgs = new Dictionary<Arguments, string> { { Arguments.WizardInit, string.Empty } };
        var drives = MultipleTestDrives;
        var selectedDrive = drives[1]; // User selects second drive

        _mockAdrServices.ParseArgs(args, Arg.Any<Arguments[]>()).Returns(parsedArgs);
        _mockValidateConfig.HasTemplateRepoFile().Returns(true);
        _mockFileSystem.GetDrives().Returns(drives);
        // Drive selection succeeds
        _mockConsole.PromptSelectLogicalDrive(Arg.Any<string>(), _mockFileSystem, Arg.Any<CancellationToken>())
            .Returns((false, selectedDrive)); // IsAborted = false, returns selected drive
        // Folder selection is aborted
        _mockConsole.PromptSelectFolderPath(Arg.Any<string>(), false, selectedDrive, _mockFileSystem, _mockValidateConfig, Arg.Any<CancellationToken>())
            .Returns((true, string.Empty)); // IsAborted = true

        // Act & Assert
        await _handler.Invoking(h => h.ExecuteAsync(args, TestContext.Current.CancellationToken))
            .Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task ExecuteAsync_WithWizardSuccessfulDriveSelectionSingleDrive_SkipsDrivePrompt()
    {
        // Arrange - Single drive available, so drive selection is skipped
        var args = new[] { "--wizard" };
        var parsedArgs = new Dictionary<Arguments, string> { { Arguments.WizardInit, string.Empty } };
        var drives = new[] { SingleTestDrive };
        var repoPath = InitRepositoryAdrPath;
        var configPath = Path.Combine(InitRepositoryPath, ".adrplus");
        var jsonConfig = """{"Prefix": "ADR", "LenSeq": 4, "FolderAdr": "adr", "FolderByScope": false}""";

        _mockAdrServices.ParseArgs(args, Arg.Any<Arguments[]>()).Returns(parsedArgs);
        _mockValidateConfig.HasTemplateRepoFile().Returns(true);
        _mockFileSystem.GetDrives().Returns(drives);
        _mockFileSystem.DirectoryExists(InitRepositoryPath).Returns(true);
        _mockFileSystem.DirectoryExists(repoPath).Returns(false);
        _mockFileSystem.FileExists(Arg.Any<string>()).Returns(false);
        // Folder selection succeeds
        _mockConsole.PromptSelectFolderPath(Arg.Any<string>(), false, SingleTestDrive, _mockFileSystem, _mockValidateConfig, Arg.Any<CancellationToken>())
            .Returns((false, InitRepositoryPath)); // IsAborted = false, returns selected folder
        _mockValidateConfig.GetFileNameRepoConfig().Returns(".adrplus");
        _mockValidateConfig.GetDefaultConfigRepoFilePath().Returns("template-path");
        _mockValidateConfig.GetConfigDefaultRepoContentAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(jsonConfig);
        _mockValidateConfig.ValidateRepoStructure(jsonConfig).Returns((true, []));
        _mockValidateConfig.GetMaxNumberVersionRevision(Arg.Any<string>(), Arg.Any<AdrPlusRepoConfig>()).Returns((0, 0, 0));
        _mockFileSystem.CreateDirectory(Arg.Any<string>()).Returns(callInfo => callInfo.Arg<string>());
        _mockFileSystem.GetFullNameFile(Arg.Any<string>()).Returns(configPath);

        var repoConfig = new AdrPlusRepoConfig("", "") { FolderAdr = "adr" };
        _mockAdrServices.FromJson(jsonConfig, "").Returns(repoConfig);

        // Act
        await _handler.ExecuteAsync(args, TestContext.Current.CancellationToken);

        // Assert - Drive prompt should NOT be called
        _mockConsole.DidNotReceive().PromptSelectLogicalDrive(Arg.Any<string>(), Arg.Any<IFileSystemService>(), Arg.Any<CancellationToken>());
        _mockConsole.Received(1).PromptSelectFolderPath(Arg.Any<string>(), false, SingleTestDrive, _mockFileSystem, _mockValidateConfig, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WithWizardBothSelectionsSuccess_CreatesRepositoryWithWizardPath()
    {
        // Arrange - Both drive and folder selections succeed
        var args = new[] { "--wizard" };
        var parsedArgs = new Dictionary<Arguments, string> { { Arguments.WizardInit, string.Empty } };
        var drives = MultipleTestDrives;
        var selectedDrive = drives[1];
        var selectedFolder = Path.Combine(selectedDrive, "my-repo");
        var selectedFolderAdr = Path.Combine(selectedFolder, "adr");
        var configPath = Path.Combine(selectedFolder, ".adrplus");
        var jsonConfig = """{"Prefix": "ADR", "LenSeq": 4, "FolderAdr": "adr"}""";

        _mockAdrServices.ParseArgs(args, Arg.Any<Arguments[]>()).Returns(parsedArgs);
        _mockValidateConfig.HasTemplateRepoFile().Returns(true);
        _mockFileSystem.GetDrives().Returns(drives);
        _mockFileSystem.DirectoryExists(selectedFolder).Returns(true);
        _mockFileSystem.DirectoryExists(selectedFolderAdr).Returns(false);
        _mockFileSystem.FileExists(Arg.Any<string>()).Returns(false);
        // Drive selection succeeds
        _mockConsole.PromptSelectLogicalDrive(Arg.Any<string>(), _mockFileSystem, Arg.Any<CancellationToken>())
            .Returns((false, selectedDrive));
        // Folder selection succeeds
        _mockConsole.PromptSelectFolderPath(Arg.Any<string>(), false, selectedDrive, _mockFileSystem, _mockValidateConfig, Arg.Any<CancellationToken>())
            .Returns((false, selectedFolder));
        _mockValidateConfig.GetFileNameRepoConfig().Returns(".adrplus");
        _mockValidateConfig.GetDefaultConfigRepoFilePath().Returns("template-path");
        _mockValidateConfig.GetConfigDefaultRepoContentAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(jsonConfig);
        _mockValidateConfig.ValidateRepoStructure(jsonConfig).Returns((true, []));
        _mockValidateConfig.GetMaxNumberVersionRevision(Arg.Any<string>(), Arg.Any<AdrPlusRepoConfig>()).Returns((0, 0, 0));
        _mockFileSystem.CreateDirectory(Arg.Any<string>()).Returns(callInfo => callInfo.Arg<string>());
        _mockFileSystem.GetFullNameFile(Arg.Any<string>()).Returns(configPath);

        var repoConfig = new AdrPlusRepoConfig("", "")
        {
            FolderAdr = "adr"
        };
        _mockAdrServices.FromJson(jsonConfig, "").Returns(repoConfig);

        // Act
        await _handler.ExecuteAsync(args, TestContext.Current.CancellationToken);

        // Assert - Both prompts should be called, repo initialized at selected location
        _mockConsole.Received(1).PromptSelectLogicalDrive(Arg.Any<string>(), _mockFileSystem, Arg.Any<CancellationToken>());
        _mockConsole.Received(1).PromptSelectFolderPath(Arg.Any<string>(), false, selectedDrive, _mockFileSystem, _mockValidateConfig, Arg.Any<CancellationToken>());
        await _mockFileSystem.Received(1).WriteAllTextAsync(Arg.Any<string>(), jsonConfig, Arg.Any<CancellationToken>());
        _mockFileSystem.Received(1).CreateDirectory(selectedFolderAdr);
    }

    [Fact]
    public async Task ExecuteAsync_WithWizardCancellationDuringFolderSelectionMultipleDrives_ThrowsOperationCancelledException()
    {
        // Arrange - Drive selection succeeds but folder selection is cancelled after drive choice
        var args = new[] { "--wizard" };
        var parsedArgs = new Dictionary<Arguments, string> { { Arguments.WizardInit, string.Empty } };
        var drives = new[] { "C:\\", "D:\\", "E:\\" };
        var selectedDrive = drives[1]; // User selects D:

        _mockAdrServices.ParseArgs(args, Arg.Any<Arguments[]>()).Returns(parsedArgs);
        _mockValidateConfig.HasTemplateRepoFile().Returns(true);
        _mockFileSystem.GetDrives().Returns(drives);
        // Drive selection succeeds
        _mockConsole.PromptSelectLogicalDrive(Arg.Any<string>(), _mockFileSystem, Arg.Any<CancellationToken>())
            .Returns((false, selectedDrive)); // IsAborted = false
        // Folder selection is cancelled
        _mockConsole.PromptSelectFolderPath(Arg.Any<string>(), false, Arg.Any<string>(), _mockFileSystem, _mockValidateConfig, Arg.Any<CancellationToken>())
            .Returns((true, string.Empty)); // IsAborted = true

        // Act & Assert
        await _handler.Invoking(h => h.ExecuteAsync(args, TestContext.Current.CancellationToken))
            .Should().ThrowAsync<OperationCanceledException>();
    }

    #endregion
}


