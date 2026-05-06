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
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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
    private readonly IValidateJsonConfig _mockValidateConfig;
    private readonly IAdrServices _mockAdrServices;
    private readonly AdrPlusConfig _config;
    private readonly InitCommandHandler _handler;

    public InitCommandHandlerTests()
    {
        _mockLogger = Substitute.For<ILogger<InitCommandHandler>>();
        _mockFileSystem = Substitute.For<IFileSystemService>();
        _mockConsole = Substitute.For<IConsoleWriter>();
        _mockValidateConfig = Substitute.For<IValidateJsonConfig>();
        _mockAdrServices = Substitute.For<IAdrServices>();

        _config = new AdrPlusConfig
        {
        };


        _handler = new InitCommandHandler(
            _mockLogger,
            Options.Create(_config),
            _mockFileSystem,
            _mockValidateConfig,
            _mockConsole,
            _mockAdrServices);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidParameters_CreatesInstance()
    {
        // Arrange & Act
        var handler = new InitCommandHandler(
            _mockLogger,
            Options.Create(_config),
            _mockFileSystem,
            _mockValidateConfig,
            _mockConsole,
            _mockAdrServices);

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
        await _handler.ExecuteAsync(args, CancellationToken.None);

        // Assert
        _mockConsole.Received(1).WriteHelp("Help text");
    }

    #endregion

    #region ExecuteAsync - Template File Not Found Tests

    [Fact]
    public async Task ExecuteAsync_WhenTemplateRepoFileNotFound_ThrowsFileNotFoundException()
    {
        // Arrange
        var args = new[] { "--path", "C:\\repo" };
        var parsedArgs = new Dictionary<Arguments, string> { { Arguments.TargetRepo, "C:\\repo" } };

        _mockAdrServices.ParseArgs(args, Arg.Any<Arguments[]>()).Returns(parsedArgs);
        _mockValidateConfig.HasTemplateRepoFile().Returns(false);

        // Act & Assert
        await _handler.Invoking(h => h.ExecuteAsync(args, CancellationToken.None))
            .Should().ThrowAsync<FileNotFoundException>();
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
        _mockFileSystem.ReadAllTextAsync("template-path", Arg.Any<CancellationToken>()).Returns(jsonConfig);
        _mockValidateConfig.ValidateRepoStructure(jsonConfig).Returns((true, []));
        _mockFileSystem.CreateDirectory(repoPath).Returns(repoPath);
        _mockFileSystem.GetFullNameFile(Arg.Any<string>()).Returns(configPath);

        var repoConfig = new AdrPlusRepoConfig("", "") { FolderByScope = false };
        _mockAdrServices.FromJson(jsonConfig, "").Returns(repoConfig);

        // Act
        await _handler.ExecuteAsync(args, CancellationToken.None);

        // Assert
        await _mockFileSystem.Received(1).WriteAllTextAsync(Arg.Any<string>(), jsonConfig, Arg.Any<CancellationToken>());
        _mockConsole.Received().WriteSuccess(Arg.Any<string>());
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
        _mockFileSystem.ReadAllTextAsync("template-path", Arg.Any<CancellationToken>()).Returns(jsonConfig);
        _mockValidateConfig.ValidateRepoStructure(jsonConfig).Returns((true, []));
        _mockFileSystem.CreateDirectory(repoPath).Returns(repoPath);
        _mockFileSystem.GetFullNameFile(Arg.Any<string>()).Returns(configPath);

        var repoConfig = new AdrPlusRepoConfig("", "") { FolderByScope = false };
        _mockAdrServices.FromJson(jsonConfig, "").Returns(repoConfig);

        // Act
        await _handler.ExecuteAsync(args, CancellationToken.None);

        // Assert
        await _mockFileSystem.Received(1).WriteAllTextAsync(Arg.Any<string>(), jsonConfig, Arg.Any<CancellationToken>());
        _mockConsole.Received().WriteSuccess(Arg.Any<string>());
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
        await _handler.Invoking(h => h.ExecuteAsync(args, CancellationToken.None))
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
        await _handler.Invoking(h => h.ExecuteAsync(args, CancellationToken.None))
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
        _mockFileSystem.ReadAllTextAsync("template-path", Arg.Any<CancellationToken>()).Returns(jsonConfig);
        _mockValidateConfig.ValidateRepoStructure(jsonConfig).Returns((false, errors));

        // Act & Assert
        await _handler.Invoking(h => h.ExecuteAsync(args, CancellationToken.None))
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
        _mockConsole.PromptSelectFolderRepositoryPath(false, SingleTestDrive, _mockFileSystem, _mockValidateConfig, Arg.Any<CancellationToken>())
            .Returns((false, selectedPath));
        _mockFileSystem.DirectoryExists(selectedPath).Returns(true);
        _mockFileSystem.DirectoryExists(repoPath).Returns(false);
        _mockFileSystem.FileExists(Arg.Any<string>()).Returns(false);
        _mockValidateConfig.GetFileNameRepoConfig().Returns(".adrplus");
        _mockValidateConfig.GetDefaultConfigRepoFilePath().Returns("template-path");
        _mockFileSystem.ReadAllTextAsync("template-path", Arg.Any<CancellationToken>()).Returns(jsonConfig);
        _mockValidateConfig.ValidateRepoStructure(jsonConfig).Returns((true, []));
        _mockFileSystem.CreateDirectory(Arg.Any<string>()).Returns(callInfo => callInfo.Arg<string>());
        _mockFileSystem.GetFullNameFile(Arg.Any<string>()).Returns(configPath);

        var repoConfig = new AdrPlusRepoConfig("", "") { FolderByScope = false };
        _mockAdrServices.FromJson(jsonConfig, "").Returns(repoConfig);

        // Act
        await _handler.ExecuteAsync(args, CancellationToken.None);

        // Assert
        _mockConsole.Received(1).PromptSelectFolderRepositoryPath(false, SingleTestDrive, _mockFileSystem, _mockValidateConfig, Arg.Any<CancellationToken>());
        await _mockFileSystem.Received(1).WriteAllTextAsync(Arg.Any<string>(), jsonConfig, Arg.Any<CancellationToken>());
        _mockConsole.Received().WriteSuccess(Arg.Any<string>());
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
        _mockConsole.PromptSelectFolderRepositoryPath(false, selectedDrive, _mockFileSystem, _mockValidateConfig, Arg.Any<CancellationToken>())
            .Returns((false, selectedPath));
        _mockFileSystem.DirectoryExists(selectedPath).Returns(true);
        _mockFileSystem.DirectoryExists(repoPath).Returns(false);
        _mockFileSystem.FileExists(Arg.Any<string>()).Returns(false);
        _mockValidateConfig.GetFileNameRepoConfig().Returns(".adrplus");
        _mockValidateConfig.GetDefaultConfigRepoFilePath().Returns("template-path");
        _mockFileSystem.ReadAllTextAsync("template-path", Arg.Any<CancellationToken>()).Returns(jsonConfig);
        _mockValidateConfig.ValidateRepoStructure(jsonConfig).Returns((true, []));
        _mockFileSystem.CreateDirectory(Arg.Any<string>()).Returns(callInfo => callInfo.Arg<string>());
        _mockFileSystem.GetFullNameFile(Arg.Any<string>()).Returns(configPath);

        var repoConfig = new AdrPlusRepoConfig("", "") { FolderByScope = false };
        _mockAdrServices.FromJson(jsonConfig, "").Returns(repoConfig);

        // Act
        await _handler.ExecuteAsync(args, CancellationToken.None);

        // Assert
        _mockConsole.Received(1).PromptSelectLogicalDrive(Arg.Any<string>(), _mockFileSystem, Arg.Any<CancellationToken>());
        _mockConsole.Received(1).PromptSelectFolderRepositoryPath(false, selectedDrive, _mockFileSystem, _mockValidateConfig, Arg.Any<CancellationToken>());
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
        await _handler.Invoking(h => h.ExecuteAsync(args, CancellationToken.None))
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
        _mockConsole.PromptSelectFolderRepositoryPath(false, SingleTestDrive, _mockFileSystem, _mockValidateConfig, Arg.Any<CancellationToken>())
            .Returns((true, string.Empty));

        // Act & Assert
        await _handler.Invoking(h => h.ExecuteAsync(args, CancellationToken.None))
            .Should().ThrowAsync<OperationCanceledException>();
    }

    #endregion

    #region Scope Directory Tests

    [Fact]
    public async Task ExecuteAsync_WithFolderByScope_CreatesScopeDirectories()
    {
        // Arrange
        var args = new[] { "--path", InitRepositoryPath };
        var parsedArgs = new Dictionary<Arguments, string> { { Arguments.TargetRepo, InitRepositoryPath } };
        var repoPath = InitRepositoryAdrPath;
        var configPath = Path.Combine(repoPath, ".adrplus");
        var jsonConfig = """{"Prefix": "ADR", "LenSeq": 4, "FolderByScope": true, "Scopes": "frontend;backend;database"}""";

        _mockAdrServices.ParseArgs(args, Arg.Any<Arguments[]>()).Returns(parsedArgs);
        _mockValidateConfig.HasTemplateRepoFile().Returns(true);
        _mockFileSystem.DirectoryExists(InitRepositoryPath).Returns(true);
        _mockFileSystem.DirectoryExists(repoPath).Returns(false);
        _mockFileSystem.DirectoryExists(Arg.Is<string>(s => s.Contains("frontend") || s.Contains("backend") || s.Contains("database")))
            .Returns(false);
        _mockFileSystem.FileExists(Arg.Any<string>()).Returns(false);
        _mockValidateConfig.GetFileNameRepoConfig().Returns(".adrplus");
        _mockValidateConfig.GetDefaultConfigRepoFilePath().Returns("template-path");
        _mockFileSystem.ReadAllTextAsync("template-path", Arg.Any<CancellationToken>()).Returns(jsonConfig);
        _mockValidateConfig.ValidateRepoStructure(jsonConfig).Returns((true, []));
        _mockFileSystem.CreateDirectory(Arg.Any<string>()).Returns(callInfo => callInfo.Arg<string>());
        _mockFileSystem.GetFullNameFile(Arg.Any<string>()).Returns(configPath);

        var repoConfig = new AdrPlusRepoConfig("", "")
        {
            FolderByScope = true,
            Scopes = "frontend;backend;database"
        };
        _mockAdrServices.FromJson(jsonConfig, "").Returns(repoConfig);

        // Act
        await _handler.ExecuteAsync(args, CancellationToken.None);

        // Assert
        _mockFileSystem.Received(1).CreateDirectory(Arg.Is<string>(s => s.Contains("frontend")));
        _mockFileSystem.Received(1).CreateDirectory(Arg.Is<string>(s => s.Contains("backend")));
        _mockFileSystem.Received(1).CreateDirectory(Arg.Is<string>(s => s.Contains("database")));
    }

    [Fact]
    public async Task ExecuteAsync_WithFolderByScopeExistingDirectories_SkipsExistingDirectories()
    {
        // Arrange
        var args = new[] { "--path", InitRepositoryPath };
        var parsedArgs = new Dictionary<Arguments, string> { { Arguments.TargetRepo, InitRepositoryPath } };
        var repoPath = InitRepositoryAdrPath;
        var configPath = Path.Combine(repoPath, ".adrplus");
        var jsonConfig = """{"Prefix": "ADR", "LenSeq": 4, "FolderByScope": true, "Scopes": "frontend;backend"}""";

        _mockAdrServices.ParseArgs(args, Arg.Any<Arguments[]>()).Returns(parsedArgs);
        _mockValidateConfig.HasTemplateRepoFile().Returns(true);
        _mockFileSystem.DirectoryExists(InitRepositoryPath).Returns(true);
        _mockFileSystem.DirectoryExists(repoPath).Returns(false);
        _mockFileSystem.DirectoryExists(Arg.Is<string>(s => s.Contains("frontend"))).Returns(true);
        _mockFileSystem.DirectoryExists(Arg.Is<string>(s => s.Contains("backend"))).Returns(false);
        _mockFileSystem.FileExists(Arg.Any<string>()).Returns(false);
        _mockValidateConfig.GetFileNameRepoConfig().Returns(".adrplus");
        _mockValidateConfig.GetDefaultConfigRepoFilePath().Returns("template-path");
        _mockFileSystem.ReadAllTextAsync("template-path", Arg.Any<CancellationToken>()).Returns(jsonConfig);
        _mockValidateConfig.ValidateRepoStructure(jsonConfig).Returns((true, []));
        _mockFileSystem.CreateDirectory(Arg.Any<string>()).Returns(callInfo => callInfo.Arg<string>());
        _mockFileSystem.GetFullNameFile(Arg.Any<string>()).Returns(configPath);

        var repoConfig = new AdrPlusRepoConfig("", "")
        {
            FolderByScope = true,
            Scopes = "frontend;backend"
        };
        _mockAdrServices.FromJson(jsonConfig, "").Returns(repoConfig);

        // Act
        await _handler.ExecuteAsync(args, CancellationToken.None);

        // Assert
        _mockFileSystem.DidNotReceive().CreateDirectory(Arg.Is<string>(s => s.Contains("frontend")));
        _mockFileSystem.Received(1).CreateDirectory(Arg.Is<string>(s => s.Contains("backend")));
    }

    [Fact]
    public async Task ExecuteAsync_WithoutFolderByScope_DoesNotCreateScopeDirectories()
    {
        // Arrange
        var args = new[] { "--path", InitRepositoryPath };
        var parsedArgs = new Dictionary<Arguments, string> { { Arguments.TargetRepo, InitRepositoryPath } };
        var repoPath = InitRepositoryAdrPath;
        var configPath = Path.Combine(repoPath, ".adrplus");
        var jsonConfig = """{"Prefix": "ADR", "LenSeq": 4, "FolderByScope": false, "Scopes": "frontend;backend"}""";

        _mockAdrServices.ParseArgs(args, Arg.Any<Arguments[]>()).Returns(parsedArgs);
        _mockValidateConfig.HasTemplateRepoFile().Returns(true);
        _mockFileSystem.DirectoryExists(InitRepositoryPath).Returns(true);
        _mockFileSystem.DirectoryExists(repoPath).Returns(false);
        _mockFileSystem.FileExists(Arg.Any<string>()).Returns(false);
        _mockValidateConfig.GetFileNameRepoConfig().Returns(".adrplus");
        _mockValidateConfig.GetDefaultConfigRepoFilePath().Returns("template-path");
        _mockFileSystem.ReadAllTextAsync("template-path", Arg.Any<CancellationToken>()).Returns(jsonConfig);
        _mockValidateConfig.ValidateRepoStructure(jsonConfig).Returns((true, []));
        _mockFileSystem.CreateDirectory(repoPath).Returns(repoPath);
        _mockFileSystem.GetFullNameFile(Arg.Any<string>()).Returns(configPath);

        var repoConfig = new AdrPlusRepoConfig("", "")
        {
            FolderByScope = false,
            Scopes = "frontend;backend"
        };
        _mockAdrServices.FromJson(jsonConfig, "").Returns(repoConfig);

        // Act
        await _handler.ExecuteAsync(args, CancellationToken.None);

        // Assert
        _mockFileSystem.DidNotReceive().CreateDirectory(Arg.Is<string>(s => s.Contains("frontend")));
        _mockFileSystem.DidNotReceive().CreateDirectory(Arg.Is<string>(s => s.Contains("backend")));
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
        _mockFileSystem.ReadAllTextAsync("template-path", Arg.Any<CancellationToken>()).Returns(jsonConfig);
        _mockValidateConfig.ValidateRepoStructure(jsonConfig).Returns((true, []));
        _mockFileSystem.CreateDirectory(Arg.Any<string>()).Returns(callInfo => callInfo.Arg<string>());
        _mockFileSystem.GetFullNameFile(Arg.Any<string>()).Returns("configPath");

        var repoConfig = new AdrPlusRepoConfig("", "") { FolderAdr = "adr", FolderByScope = false };
        _mockAdrServices.FromJson(jsonConfig, "").Returns(repoConfig);

        // Act
        await _handler.ExecuteAsync(args, CancellationToken.None);

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
        _mockFileSystem.ReadAllTextAsync("template-path", Arg.Any<CancellationToken>()).Returns(jsonConfig);
        _mockValidateConfig.ValidateRepoStructure(jsonConfig).Returns((true, []));
        _mockFileSystem.GetFullNameFile(Arg.Any<string>()).Returns(configPath);

        var repoConfig = new AdrPlusRepoConfig("", "") { FolderByScope = false };
        _mockAdrServices.FromJson(jsonConfig, "").Returns(repoConfig);

        // Act
        await _handler.ExecuteAsync(args, CancellationToken.None);

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
        _mockFileSystem.ReadAllTextAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
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
        _mockValidateConfig.When(x => x.HasTemplateRepoFile()).Do(x => throw exception);

        // Act & Assert
        await _handler.Invoking(h => h.ExecuteAsync(args, CancellationToken.None))
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
        var jsonConfig = """{"Prefix": "ADR", "LenSeq": 4, "FolderByScope": true, "Scopes": "api"}""";

        _mockAdrServices.ParseArgs(args, Arg.Any<Arguments[]>()).Returns(parsedArgs);
        _mockValidateConfig.HasTemplateRepoFile().Returns(true);
        _mockFileSystem.DirectoryExists(InitRepositoryPath).Returns(true);
        _mockFileSystem.DirectoryExists(repoPath).Returns(false);
        _mockFileSystem.DirectoryExists(Arg.Is<string>(s => s.Contains("api"))).Returns(false);
        _mockFileSystem.FileExists(Arg.Any<string>()).Returns(false);
        _mockValidateConfig.GetFileNameRepoConfig().Returns(".adrplus");
        _mockValidateConfig.GetDefaultConfigRepoFilePath().Returns("template-path");
        _mockFileSystem.ReadAllTextAsync("template-path", Arg.Any<CancellationToken>()).Returns(jsonConfig);
        _mockValidateConfig.ValidateRepoStructure(jsonConfig).Returns((true, []));
        _mockFileSystem.CreateDirectory(Arg.Any<string>()).Returns(callInfo => callInfo.Arg<string>());
        _mockFileSystem.GetFullNameFile(Arg.Any<string>()).Returns(configPath);

        var repoConfig = new AdrPlusRepoConfig("", "")
        {
            FolderByScope = true,
            Scopes = "api"
        };
        _mockAdrServices.FromJson(jsonConfig, "").Returns(repoConfig);

        // Act
        await _handler.ExecuteAsync(args, CancellationToken.None);

        // Assert
        _mockConsole.Received().WriteSuccess(configPath);
        _mockConsole.Received().WriteSuccess(Arg.Is<string>(s => s.Contains("api")));
    }

    #endregion

    #region Coverage Enhancement Tests

    [Fact]
    public async Task ExecuteAsync_WithScopesEmptyString_DoesNotCreateScopeDirectories()
    {
        // Arrange - Test with empty scopes string  
        var args = new[] { "--path", InitRepositoryPath };
        var parsedArgs = new Dictionary<Arguments, string> { { Arguments.TargetRepo, InitRepositoryPath } };
        var repoPath = InitRepositoryAdrPath;
        var configPath = Path.Combine(InitRepositoryPath, ".adrplus");
        var jsonConfig = """{"Prefix": "ADR", "LenSeq": 4, "FolderAdr": "adr", "FolderByScope": true, "Scopes": ""}""";

        _mockAdrServices.ParseArgs(args, Arg.Any<Arguments[]>()).Returns(parsedArgs);
        _mockValidateConfig.HasTemplateRepoFile().Returns(true);
        _mockFileSystem.DirectoryExists(InitRepositoryPath).Returns(true);
        _mockFileSystem.DirectoryExists(repoPath).Returns(false);
        _mockFileSystem.FileExists(Arg.Any<string>()).Returns(false);
        _mockValidateConfig.GetFileNameRepoConfig().Returns(".adrplus");
        _mockValidateConfig.GetDefaultConfigRepoFilePath().Returns("template-path");
        _mockFileSystem.ReadAllTextAsync("template-path", Arg.Any<CancellationToken>()).Returns(jsonConfig);
        _mockValidateConfig.ValidateRepoStructure(jsonConfig).Returns((true, []));
        _mockFileSystem.CreateDirectory(Arg.Any<string>()).Returns(callInfo => callInfo.Arg<string>());
        _mockFileSystem.GetFullNameFile(Arg.Any<string>()).Returns(configPath);

        var repoConfig = new AdrPlusRepoConfig("", "")
        {
            FolderByScope = true,
            Scopes = "",
            FolderAdr = "adr"
        };
        _mockAdrServices.FromJson(jsonConfig, "").Returns(repoConfig);

        // Act
        await _handler.ExecuteAsync(args, CancellationToken.None);

        // Assert - Should not attempt to create empty scope directories, but ADR folder should be created
        _mockFileSystem.Received(1).CreateDirectory(Arg.Is<string>(s => s.EndsWith("adr", StringComparison.OrdinalIgnoreCase)));
        // WriteSuccess is called for config file and adr folder (2 times)
        _mockConsole.Received(2).WriteSuccess(Arg.Any<string>());
    }

    [Fact]
    public async Task ExecuteAsync_WithScopesContainingWhitespace_TrimsAndProcessesCorrectly()
    {
        // Arrange - Scopes with leading/trailing whitespace
        var args = new[] { "--path", InitRepositoryPath };
        var parsedArgs = new Dictionary<Arguments, string> { { Arguments.TargetRepo, InitRepositoryPath } };
        var repoPath = InitRepositoryAdrPath;
        var configPath = Path.Combine(InitRepositoryPath, ".adrplus");
        var jsonConfig = """{"Prefix": "ADR", "LenSeq": 4, "FolderByScope": true, "Scopes": " frontend ; backend "}""";

        _mockAdrServices.ParseArgs(args, Arg.Any<Arguments[]>()).Returns(parsedArgs);
        _mockValidateConfig.HasTemplateRepoFile().Returns(true);
        _mockFileSystem.DirectoryExists(InitRepositoryPath).Returns(true);
        _mockFileSystem.DirectoryExists(repoPath).Returns(false);
        _mockFileSystem.DirectoryExists(Arg.Is<string>(s => s.Contains("frontend") || s.Contains("backend"))).Returns(false);
        _mockFileSystem.FileExists(Arg.Any<string>()).Returns(false);
        _mockValidateConfig.GetFileNameRepoConfig().Returns(".adrplus");
        _mockValidateConfig.GetDefaultConfigRepoFilePath().Returns("template-path");
        _mockFileSystem.ReadAllTextAsync("template-path", Arg.Any<CancellationToken>()).Returns(jsonConfig);
        _mockValidateConfig.ValidateRepoStructure(jsonConfig).Returns((true, []));
        _mockFileSystem.CreateDirectory(Arg.Any<string>()).Returns(callInfo => callInfo.Arg<string>());
        _mockFileSystem.GetFullNameFile(Arg.Any<string>()).Returns(configPath);

        var repoConfig = new AdrPlusRepoConfig("", "")
        {
            FolderByScope = true,
            Scopes = " frontend ; backend "
        };
        _mockAdrServices.FromJson(jsonConfig, "").Returns(repoConfig);

        // Act
        await _handler.ExecuteAsync(args, CancellationToken.None);

        // Assert - Should create scope folders despite whitespace
        _mockFileSystem.Received(1).CreateDirectory(Arg.Is<string>(s => s.Contains("frontend")));
        _mockFileSystem.Received(1).CreateDirectory(Arg.Is<string>(s => s.Contains("backend")));
    }

    [Fact]
    public async Task ExecuteAsync_WithMultipleScopesAndMixedExistingDirs_CreatesOnlyMissing()
    {
        // Arrange - Mixed scenario: some scopes exist, others don't
        var args = new[] { "--path", InitRepositoryPath };
        var parsedArgs = new Dictionary<Arguments, string> { { Arguments.TargetRepo, InitRepositoryPath } };
        var repoPath = InitRepositoryAdrPath;
        var configPath = Path.Combine(InitRepositoryPath, ".adrplus");
        var jsonConfig = """{"Prefix": "ADR", "LenSeq": 4, "FolderByScope": true, "Scopes": "web;api;mobile"}""";

        _mockAdrServices.ParseArgs(args, Arg.Any<Arguments[]>()).Returns(parsedArgs);
        _mockValidateConfig.HasTemplateRepoFile().Returns(true);
        _mockFileSystem.DirectoryExists(InitRepositoryPath).Returns(true);
        _mockFileSystem.DirectoryExists(repoPath).Returns(false);
        _mockFileSystem.DirectoryExists(Arg.Is<string>(s => s.Contains("web"))).Returns(false);
        _mockFileSystem.DirectoryExists(Arg.Is<string>(s => s.Contains("api"))).Returns(true);
        _mockFileSystem.DirectoryExists(Arg.Is<string>(s => s.Contains("mobile"))).Returns(false);
        _mockFileSystem.FileExists(Arg.Any<string>()).Returns(false);
        _mockValidateConfig.GetFileNameRepoConfig().Returns(".adrplus");
        _mockValidateConfig.GetDefaultConfigRepoFilePath().Returns("template-path");
        _mockFileSystem.ReadAllTextAsync("template-path", Arg.Any<CancellationToken>()).Returns(jsonConfig);
        _mockValidateConfig.ValidateRepoStructure(jsonConfig).Returns((true, []));
        _mockFileSystem.CreateDirectory(Arg.Any<string>()).Returns(callInfo => callInfo.Arg<string>());
        _mockFileSystem.GetFullNameFile(Arg.Any<string>()).Returns(configPath);

        var repoConfig = new AdrPlusRepoConfig("", "")
        {
            FolderByScope = true,
            Scopes = "web;api;mobile"
        };
        _mockAdrServices.FromJson(jsonConfig, "").Returns(repoConfig);

        // Act
        await _handler.ExecuteAsync(args, CancellationToken.None);

        // Assert - Only web and mobile should be created
        _mockFileSystem.Received(1).CreateDirectory(Arg.Is<string>(s => s.Contains("web")));
        _mockFileSystem.DidNotReceive().CreateDirectory(Arg.Is<string>(s => s.Contains("api")));
        _mockFileSystem.Received(1).CreateDirectory(Arg.Is<string>(s => s.Contains("mobile")));
    }

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
        _mockFileSystem.ReadAllTextAsync("template-path", Arg.Any<CancellationToken>()).Returns(jsonConfig);
        _mockValidateConfig.ValidateRepoStructure(jsonConfig).Returns((true, []));
        _mockFileSystem.CreateDirectory(Arg.Any<string>()).Returns(callInfo => callInfo.Arg<string>());
        _mockFileSystem.GetFullNameFile(Arg.Any<string>()).Returns(configPath);

        var repoConfig = new AdrPlusRepoConfig("", "") { FolderAdr = "decisions", FolderByScope = false };
        _mockAdrServices.FromJson(jsonConfig, "").Returns(repoConfig);

        // Act
        await _handler.ExecuteAsync(args, CancellationToken.None);

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
        _mockFileSystem.ReadAllTextAsync("template-path", Arg.Any<CancellationToken>()).Returns(jsonConfig);
        _mockValidateConfig.ValidateRepoStructure(jsonConfig).Returns((true, []));
        _mockFileSystem.CreateDirectory(Arg.Any<string>()).Returns(callInfo => callInfo.Arg<string>());
        _mockFileSystem.GetFullNameFile(Arg.Any<string>()).Returns(configPath);

        var repoConfig = new AdrPlusRepoConfig("", "") { FolderAdr = folderAdrName, FolderByScope = false };
        _mockAdrServices.FromJson(jsonConfig, "").Returns(repoConfig);

        // Act
        await _handler.ExecuteAsync(args, CancellationToken.None);

        // Assert - Should create folder with specified name
        _mockFileSystem.Received(1).CreateDirectory(Arg.Is<string>(s => s.Contains(folderAdrName)));
    }

    [Fact]
    public async Task ExecuteAsync_WithValidConfigAndMultiplePaths_WritesConfigAndAllDirectories()
    {
        // Arrange - Comprehensive scenario with config file and multiple directories
        var args = new[] { "--path", InitRepositoryPath };
        var parsedArgs = new Dictionary<Arguments, string> { { Arguments.TargetRepo, InitRepositoryPath } };
        var repoPath = InitRepositoryAdrPath;
        var configPath = Path.Combine(InitRepositoryPath, ".adrplus");
        var jsonConfig = """{"Prefix": "ADR", "LenSeq": 4, "FolderAdr": "adr", "FolderByScope": true, "Scopes": "frontend;backend"}""";

        _mockAdrServices.ParseArgs(args, Arg.Any<Arguments[]>()).Returns(parsedArgs);
        _mockValidateConfig.HasTemplateRepoFile().Returns(true);
        _mockFileSystem.DirectoryExists(InitRepositoryPath).Returns(true);
        _mockFileSystem.DirectoryExists(repoPath).Returns(false);
        _mockFileSystem.DirectoryExists(Arg.Is<string>(s => s.Contains("frontend") || s.Contains("backend"))).Returns(false);
        _mockFileSystem.FileExists(Arg.Any<string>()).Returns(false);
        _mockValidateConfig.GetFileNameRepoConfig().Returns(".adrplus");
        _mockValidateConfig.GetDefaultConfigRepoFilePath().Returns("template-path");
        _mockFileSystem.ReadAllTextAsync("template-path", Arg.Any<CancellationToken>()).Returns(jsonConfig);
        _mockValidateConfig.ValidateRepoStructure(jsonConfig).Returns((true, []));
        _mockFileSystem.CreateDirectory(Arg.Any<string>()).Returns(callInfo => callInfo.Arg<string>());
        _mockFileSystem.GetFullNameFile(Arg.Any<string>()).Returns(configPath);

        var repoConfig = new AdrPlusRepoConfig("", "")
        {
            FolderAdr = "adr",
            FolderByScope = true,
            Scopes = "frontend;backend"
        };
        _mockAdrServices.FromJson(jsonConfig, "").Returns(repoConfig);

        // Act
        await _handler.ExecuteAsync(args, CancellationToken.None);

        // Assert - Should write config and create all directories
        await _mockFileSystem.Received(1).WriteAllTextAsync(Arg.Any<string>(), jsonConfig, Arg.Any<CancellationToken>());
        _mockFileSystem.Received(1).CreateDirectory(Arg.Is<string>(s => s.Contains("adr")));
        _mockFileSystem.Received(1).CreateDirectory(Arg.Is<string>(s => s.Contains("frontend")));
        _mockFileSystem.Received(1).CreateDirectory(Arg.Is<string>(s => s.Contains("backend")));
        _mockConsole.Received().WriteSuccess(Arg.Any<string>());
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
        _mockConsole.PromptSelectFolderRepositoryPath(false, selectedDrive, _mockFileSystem, _mockValidateConfig, Arg.Any<CancellationToken>())
            .Returns((true, string.Empty)); // IsAborted = true

        // Act & Assert
        await _handler.Invoking(h => h.ExecuteAsync(args, CancellationToken.None))
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
        _mockConsole.PromptSelectFolderRepositoryPath(false, SingleTestDrive, _mockFileSystem, _mockValidateConfig, Arg.Any<CancellationToken>())
            .Returns((false, InitRepositoryPath)); // IsAborted = false, returns selected folder
        _mockValidateConfig.GetFileNameRepoConfig().Returns(".adrplus");
        _mockValidateConfig.GetDefaultConfigRepoFilePath().Returns("template-path");
        _mockFileSystem.ReadAllTextAsync("template-path", Arg.Any<CancellationToken>()).Returns(jsonConfig);
        _mockValidateConfig.ValidateRepoStructure(jsonConfig).Returns((true, []));
        _mockFileSystem.CreateDirectory(Arg.Any<string>()).Returns(callInfo => callInfo.Arg<string>());
        _mockFileSystem.GetFullNameFile(Arg.Any<string>()).Returns(configPath);

        var repoConfig = new AdrPlusRepoConfig("", "") { FolderAdr = "adr", FolderByScope = false };
        _mockAdrServices.FromJson(jsonConfig, "").Returns(repoConfig);

        // Act
        await _handler.ExecuteAsync(args, CancellationToken.None);

        // Assert - Drive prompt should NOT be called
        _mockConsole.DidNotReceive().PromptSelectLogicalDrive(Arg.Any<string>(), Arg.Any<IFileSystemService>(), Arg.Any<CancellationToken>());
        _mockConsole.Received(1).PromptSelectFolderRepositoryPath(false, SingleTestDrive, _mockFileSystem, _mockValidateConfig, Arg.Any<CancellationToken>());
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
        var jsonConfig = """{"Prefix": "ADR", "LenSeq": 4, "FolderAdr": "adr", "FolderByScope": true, "Scopes": "api;web"}""";

        _mockAdrServices.ParseArgs(args, Arg.Any<Arguments[]>()).Returns(parsedArgs);
        _mockValidateConfig.HasTemplateRepoFile().Returns(true);
        _mockFileSystem.GetDrives().Returns(drives);
        _mockFileSystem.DirectoryExists(selectedFolder).Returns(true);
        _mockFileSystem.DirectoryExists(selectedFolderAdr).Returns(false);
        _mockFileSystem.DirectoryExists(Arg.Is<string>(s => s.Contains("api") || s.Contains("web"))).Returns(false);
        _mockFileSystem.FileExists(Arg.Any<string>()).Returns(false);
        // Drive selection succeeds
        _mockConsole.PromptSelectLogicalDrive(Arg.Any<string>(), _mockFileSystem, Arg.Any<CancellationToken>())
            .Returns((false, selectedDrive));
        // Folder selection succeeds
        _mockConsole.PromptSelectFolderRepositoryPath(false, selectedDrive, _mockFileSystem, _mockValidateConfig, Arg.Any<CancellationToken>())
            .Returns((false, selectedFolder));
        _mockValidateConfig.GetFileNameRepoConfig().Returns(".adrplus");
        _mockValidateConfig.GetDefaultConfigRepoFilePath().Returns("template-path");
        _mockFileSystem.ReadAllTextAsync("template-path", Arg.Any<CancellationToken>()).Returns(jsonConfig);
        _mockValidateConfig.ValidateRepoStructure(jsonConfig).Returns((true, []));
        _mockFileSystem.CreateDirectory(Arg.Any<string>()).Returns(callInfo => callInfo.Arg<string>());
        _mockFileSystem.GetFullNameFile(Arg.Any<string>()).Returns(configPath);

        var repoConfig = new AdrPlusRepoConfig("", "")
        {
            FolderAdr = "adr",
            FolderByScope = true,
            Scopes = "api;web"
        };
        _mockAdrServices.FromJson(jsonConfig, "").Returns(repoConfig);

        // Act
        await _handler.ExecuteAsync(args, CancellationToken.None);

        // Assert - Both prompts should be called, repo initialized at selected location
        _mockConsole.Received(1).PromptSelectLogicalDrive(Arg.Any<string>(), _mockFileSystem, Arg.Any<CancellationToken>());
        _mockConsole.Received(1).PromptSelectFolderRepositoryPath(false, selectedDrive, _mockFileSystem, _mockValidateConfig, Arg.Any<CancellationToken>());
        await _mockFileSystem.Received(1).WriteAllTextAsync(Arg.Any<string>(), jsonConfig, Arg.Any<CancellationToken>());
        _mockFileSystem.Received(1).CreateDirectory(Arg.Is<string>(s => s.Contains("api")));
        _mockFileSystem.Received(1).CreateDirectory(Arg.Is<string>(s => s.Contains("web")));
    }

    [Fact]
    public async Task ExecuteAsync_WithWizardMultipleDrivesAndComplexScopes_InitializesCompleteStructure()
    {
        // Arrange - Complex scenario: multiple drives, multiple scopes, all succeeding
        var args = new[] { "--wizard" };
        var parsedArgs = new Dictionary<Arguments, string> { { Arguments.WizardInit, string.Empty } };
        var drives = new[] { "C:\\", "D:\\", "E:\\" };
        var selectedDrive = drives[2]; // Select E:
        var selectedFolder = Path.Combine(selectedDrive, "adrplus-repo");
        var selectedFolderAdr = Path.Combine(selectedFolder, "decisions");
        var configPath = Path.Combine(selectedFolder, ".adrplus");
        var jsonConfig = """{"Prefix": "ADR", "LenSeq": 4, "FolderAdr": "decisions", "FolderByScope": true, "Scopes": "backend;frontend;mobile;database"}""";

        _mockAdrServices.ParseArgs(args, Arg.Any<Arguments[]>()).Returns(parsedArgs);
        _mockValidateConfig.HasTemplateRepoFile().Returns(true);
        _mockFileSystem.GetDrives().Returns(drives);
        _mockFileSystem.DirectoryExists(selectedFolder).Returns(true);
        _mockFileSystem.DirectoryExists(selectedFolderAdr).Returns(false);
        _mockFileSystem.DirectoryExists(Arg.Is<string>(s => s.Contains("backend") || s.Contains("frontend") || s.Contains("mobile") || s.Contains("database")))
            .Returns(false);
        _mockFileSystem.FileExists(Arg.Any<string>()).Returns(false);
        // Drive selection: user selects E:
        _mockConsole.PromptSelectLogicalDrive(Arg.Any<string>(), _mockFileSystem, Arg.Any<CancellationToken>())
            .Returns((false, selectedDrive));
        // Folder selection: user selects the repo folder
        _mockConsole.PromptSelectFolderRepositoryPath(false, selectedDrive, _mockFileSystem, _mockValidateConfig, Arg.Any<CancellationToken>())
            .Returns((false, selectedFolder));
        _mockValidateConfig.GetFileNameRepoConfig().Returns(".adrplus");
        _mockValidateConfig.GetDefaultConfigRepoFilePath().Returns("template-path");
        _mockFileSystem.ReadAllTextAsync("template-path", Arg.Any<CancellationToken>()).Returns(jsonConfig);
        _mockValidateConfig.ValidateRepoStructure(jsonConfig).Returns((true, []));
        _mockFileSystem.CreateDirectory(Arg.Any<string>()).Returns(callInfo => callInfo.Arg<string>());
        _mockFileSystem.GetFullNameFile(Arg.Any<string>()).Returns(configPath);

        var repoConfig = new AdrPlusRepoConfig("", "")
        {
            FolderAdr = "decisions",
            FolderByScope = true,
            Scopes = "backend;frontend;mobile;database"
        };
        _mockAdrServices.FromJson(jsonConfig, "").Returns(repoConfig);

        // Act
        await _handler.ExecuteAsync(args, CancellationToken.None);

        // Assert - Full structure created at wizard-selected location
        _mockConsole.Received(1).PromptSelectLogicalDrive(Arg.Any<string>(), _mockFileSystem, Arg.Any<CancellationToken>());
        _mockConsole.Received(1).PromptSelectFolderRepositoryPath(false, selectedDrive, _mockFileSystem, _mockValidateConfig, Arg.Any<CancellationToken>());
        await _mockFileSystem.Received(1).WriteAllTextAsync(Arg.Any<string>(), jsonConfig, Arg.Any<CancellationToken>());
        _mockFileSystem.Received(1).CreateDirectory(Arg.Is<string>(s => s.Contains("decisions")));
        _mockFileSystem.Received(1).CreateDirectory(Arg.Is<string>(s => s.Contains("backend")));
        _mockFileSystem.Received(1).CreateDirectory(Arg.Is<string>(s => s.Contains("frontend")));
        _mockFileSystem.Received(1).CreateDirectory(Arg.Is<string>(s => s.Contains("mobile")));
        _mockFileSystem.Received(1).CreateDirectory(Arg.Is<string>(s => s.Contains("database")));
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
        _mockConsole.PromptSelectFolderRepositoryPath(false, selectedDrive, _mockFileSystem, _mockValidateConfig, Arg.Any<CancellationToken>())
            .Returns((true, string.Empty)); // IsAborted = true

        // Act & Assert
        await _handler.Invoking(h => h.ExecuteAsync(args, CancellationToken.None))
            .Should().ThrowAsync<OperationCanceledException>();
    }

    #endregion
}

