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
            FolderRepo = "docs/adr",
        };

        var options = Options.Create(_config);

        _handler = new InitCommandHandler(
            _mockLogger,
            options,
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
        var args = new[] { "--path", "C:\\repo" };
        var parsedArgs = new Dictionary<Arguments, string> { { Arguments.TargetRepo, "C:\\repo" } };
        var repoPath = "C:\\repo\\docs\\adr";
        var configPath = Path.Combine(repoPath, ".adrplus");
        var jsonConfig = """{"Prefix": "ADR", "LenSeq": 4, "FolderByScope": false}""";

        _mockAdrServices.ParseArgs(args, Arg.Any<Arguments[]>()).Returns(parsedArgs);
        _mockValidateConfig.HasTemplateRepoFile().Returns(true);
        _mockFileSystem.DirectoryExists("C:\\repo").Returns(true);
        _mockFileSystem.DirectoryExists(repoPath).Returns(false);
        _mockFileSystem.FileExists(Arg.Any<string>()).Returns(false);
        _mockValidateConfig.GetFileNameRepoConfig().Returns(".adrplus");
        _mockValidateConfig.GetConfigRepoFilePath().Returns("template-path");
        _mockFileSystem.ReadAllTextAsync("template-path", Arg.Any<CancellationToken>()).Returns(jsonConfig);
        _mockValidateConfig.ValidateRepoStructure(jsonConfig).Returns((true, []));
        _mockFileSystem.CreateDirectory(repoPath).Returns(repoPath);
        _mockFileSystem.GetFullNameFile(Arg.Any<string>()).Returns(configPath);

        var repoConfig = new AdrPlusRepoConfig { FolderByScope = false };
        _mockAdrServices.FromJson(jsonConfig, "", "").Returns(repoConfig);

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
        var args = new[] { "--path", "" };
        var parsedArgs = new Dictionary<Arguments, string> { { Arguments.TargetRepo, "" } };
        var repoPath = Path.GetFullPath("docs\\adr");
        var configPath = Path.Combine(repoPath, ".adrplus");
        var jsonConfig = """{"Prefix": "ADR", "LenSeq": 4, "FolderByScope": false}""";

        _mockAdrServices.ParseArgs(args, Arg.Any<Arguments[]>()).Returns(parsedArgs);
        _mockValidateConfig.HasTemplateRepoFile().Returns(true);
        _mockFileSystem.DirectoryExists(Arg.Any<string>()).Returns(true);
        _mockFileSystem.FileExists(Arg.Any<string>()).Returns(false);
        _mockValidateConfig.GetFileNameRepoConfig().Returns(".adrplus");
        _mockValidateConfig.GetConfigRepoFilePath().Returns("template-path");
        _mockFileSystem.ReadAllTextAsync("template-path", Arg.Any<CancellationToken>()).Returns(jsonConfig);
        _mockValidateConfig.ValidateRepoStructure(jsonConfig).Returns((true, []));
        _mockFileSystem.CreateDirectory(Arg.Any<string>()).Returns(repoPath);
        _mockFileSystem.GetFullNameFile(Arg.Any<string>()).Returns(configPath);

        var repoConfig = new AdrPlusRepoConfig { FolderByScope = false };
        _mockAdrServices.FromJson(jsonConfig, "", "").Returns(repoConfig);

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
        var args = new[] { "--path", "C:\\nonexistent" };
        var parsedArgs = new Dictionary<Arguments, string> { { Arguments.TargetRepo, "C:\\nonexistent" } };

        _mockAdrServices.ParseArgs(args, Arg.Any<Arguments[]>()).Returns(parsedArgs);
        _mockValidateConfig.HasTemplateRepoFile().Returns(true);
        _mockFileSystem.DirectoryExists("C:\\nonexistent").Returns(false);

        // Act & Assert
        await _handler.Invoking(h => h.ExecuteAsync(args, CancellationToken.None))
            .Should().ThrowAsync<DirectoryNotFoundException>();
    }

    [Fact]
    public async Task ExecuteAsync_WhenConfigFileAlreadyExists_ThrowsInvalidOperationException()
    {
        // Arrange
        var args = new[] { "--path", "C:\\repo" };
        var parsedArgs = new Dictionary<Arguments, string> { { Arguments.TargetRepo, "C:\\repo" } };
        var repoPath = "C:\\repo\\docs\\adr";
        var configPath = Path.Combine(repoPath, ".adrplus");

        _mockAdrServices.ParseArgs(args, Arg.Any<Arguments[]>()).Returns(parsedArgs);
        _mockValidateConfig.HasTemplateRepoFile().Returns(true);
        _mockFileSystem.DirectoryExists("C:\\repo").Returns(true);
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
        var args = new[] { "--path", "C:\\repo" };
        var parsedArgs = new Dictionary<Arguments, string> { { Arguments.TargetRepo, "C:\\repo" } };
        var jsonConfig = """{"Invalid": "config"}""";
        var errors = new[] { "Missing Prefix field" };

        _mockAdrServices.ParseArgs(args, Arg.Any<Arguments[]>()).Returns(parsedArgs);
        _mockValidateConfig.HasTemplateRepoFile().Returns(true);
        _mockFileSystem.DirectoryExists("C:\\repo").Returns(true);
        _mockFileSystem.FileExists(Arg.Any<string>()).Returns(false);
        _mockValidateConfig.GetFileNameRepoConfig().Returns(".adrplus");
        _mockValidateConfig.GetConfigRepoFilePath().Returns("template-path");
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
        var drives = new[] { "C:\\" };
        var selectedPath = "C:\\projects\\myrepo";
        var repoPath = "C:\\projects\\myrepo\\docs\\adr";
        var configPath = Path.Combine(repoPath, ".adrplus");
        var jsonConfig = """{"Prefix": "ADR", "LenSeq": 4, "FolderByScope": false}""";

        _mockAdrServices.ParseArgs(args, Arg.Any<Arguments[]>()).Returns(parsedArgs);
        _mockValidateConfig.HasTemplateRepoFile().Returns(true);
        _mockFileSystem.GetDrives().Returns(drives);
        _mockConsole.PromptSelectFolderRepositoryAdr(false, "C:\\", _mockFileSystem, _mockValidateConfig, _config, Arg.Any<CancellationToken>())
            .Returns((false, selectedPath));
        _mockFileSystem.DirectoryExists(selectedPath).Returns(true);
        _mockFileSystem.DirectoryExists(repoPath).Returns(false);
        _mockFileSystem.FileExists(Arg.Any<string>()).Returns(false);
        _mockValidateConfig.GetFileNameRepoConfig().Returns(".adrplus");
        _mockValidateConfig.GetConfigRepoFilePath().Returns("template-path");
        _mockFileSystem.ReadAllTextAsync("template-path", Arg.Any<CancellationToken>()).Returns(jsonConfig);
        _mockValidateConfig.ValidateRepoStructure(jsonConfig).Returns((true, []));
        _mockFileSystem.CreateDirectory(repoPath).Returns(repoPath);
        _mockFileSystem.GetFullNameFile(Arg.Any<string>()).Returns(configPath);

        var repoConfig = new AdrPlusRepoConfig { FolderByScope = false };
        _mockAdrServices.FromJson(jsonConfig, "", "").Returns(repoConfig);

        // Act
        await _handler.ExecuteAsync(args, CancellationToken.None);

        // Assert
        _mockConsole.Received(1).PromptSelectFolderRepositoryAdr(false, "C:\\", _mockFileSystem, _mockValidateConfig, _config, Arg.Any<CancellationToken>());
        await _mockFileSystem.Received(1).WriteAllTextAsync(Arg.Any<string>(), jsonConfig, Arg.Any<CancellationToken>());
        _mockConsole.Received().WriteSuccess(Arg.Any<string>());
    }

    [Fact]
    public async Task ExecuteAsync_WithWizardModeMultipleDrives_PromptsDriveSelection()
    {
        // Arrange
        var args = new[] { "--wizard" };
        var parsedArgs = new Dictionary<Arguments, string> { { Arguments.WizardInit, string.Empty } };
        var drives = new[] { "C:\\", "D:\\" };
        var selectedDrive = "D:\\";
        var selectedPath = "D:\\projects\\myrepo";
        var repoPath = "D:\\projects\\myrepo\\docs\\adr";
        var configPath = Path.Combine(repoPath, ".adrplus");
        var jsonConfig = """{"Prefix": "ADR", "LenSeq": 4, "FolderByScope": false}""";

        _mockAdrServices.ParseArgs(args, Arg.Any<Arguments[]>()).Returns(parsedArgs);
        _mockValidateConfig.HasTemplateRepoFile().Returns(true);
        _mockFileSystem.GetDrives().Returns(drives);
        _mockConsole.PromptSelectLogicalDrive(Arg.Any<string>(), _mockFileSystem, Arg.Any<CancellationToken>())
            .Returns((false, selectedDrive));
        _mockConsole.PromptSelectFolderRepositoryAdr(false, selectedDrive, _mockFileSystem, _mockValidateConfig, _config, Arg.Any<CancellationToken>())
            .Returns((false, selectedPath));
        _mockFileSystem.DirectoryExists(selectedPath).Returns(true);
        _mockFileSystem.DirectoryExists(repoPath).Returns(false);
        _mockFileSystem.FileExists(Arg.Any<string>()).Returns(false);
        _mockValidateConfig.GetFileNameRepoConfig().Returns(".adrplus");
        _mockValidateConfig.GetConfigRepoFilePath().Returns("template-path");
        _mockFileSystem.ReadAllTextAsync("template-path", Arg.Any<CancellationToken>()).Returns(jsonConfig);
        _mockValidateConfig.ValidateRepoStructure(jsonConfig).Returns((true, []));
        _mockFileSystem.CreateDirectory(repoPath).Returns(repoPath);
        _mockFileSystem.GetFullNameFile(Arg.Any<string>()).Returns(configPath);

        var repoConfig = new AdrPlusRepoConfig { FolderByScope = false };
        _mockAdrServices.FromJson(jsonConfig, "", "").Returns(repoConfig);

        // Act
        await _handler.ExecuteAsync(args, CancellationToken.None);

        // Assert
        _mockConsole.Received(1).PromptSelectLogicalDrive(Arg.Any<string>(), _mockFileSystem, Arg.Any<CancellationToken>());
        _mockConsole.Received(1).PromptSelectFolderRepositoryAdr(false, selectedDrive, _mockFileSystem, _mockValidateConfig, _config, Arg.Any<CancellationToken>());
        await _mockFileSystem.Received(1).WriteAllTextAsync(Arg.Any<string>(), jsonConfig, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WithWizardModeDriveSelectionAborted_ThrowsOperationCanceledException()
    {
        // Arrange
        var args = new[] { "--wizard" };
        var parsedArgs = new Dictionary<Arguments, string> { { Arguments.WizardInit, string.Empty } };
        var drives = new[] { "C:\\", "D:\\" };

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
        var drives = new[] { "C:\\" };

        _mockAdrServices.ParseArgs(args, Arg.Any<Arguments[]>()).Returns(parsedArgs);
        _mockValidateConfig.HasTemplateRepoFile().Returns(true);
        _mockFileSystem.GetDrives().Returns(drives);
        _mockConsole.PromptSelectFolderRepositoryAdr(false, "C:\\", _mockFileSystem, _mockValidateConfig, _config, Arg.Any<CancellationToken>())
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
        var args = new[] { "--path", "C:\\repo" };
        var parsedArgs = new Dictionary<Arguments, string> { { Arguments.TargetRepo, "C:\\repo" } };
        var repoPath = "C:\\repo\\docs\\adr";
        var configPath = Path.Combine(repoPath, ".adrplus");
        var jsonConfig = """{"Prefix": "ADR", "LenSeq": 4, "FolderByScope": true, "Scopes": "frontend;backend;database"}""";

        _mockAdrServices.ParseArgs(args, Arg.Any<Arguments[]>()).Returns(parsedArgs);
        _mockValidateConfig.HasTemplateRepoFile().Returns(true);
        _mockFileSystem.DirectoryExists("C:\\repo").Returns(true);
        _mockFileSystem.DirectoryExists(repoPath).Returns(false);
        _mockFileSystem.DirectoryExists(Arg.Is<string>(s => s.Contains("frontend") || s.Contains("backend") || s.Contains("database")))
            .Returns(false);
        _mockFileSystem.FileExists(Arg.Any<string>()).Returns(false);
        _mockValidateConfig.GetFileNameRepoConfig().Returns(".adrplus");
        _mockValidateConfig.GetConfigRepoFilePath().Returns("template-path");
        _mockFileSystem.ReadAllTextAsync("template-path", Arg.Any<CancellationToken>()).Returns(jsonConfig);
        _mockValidateConfig.ValidateRepoStructure(jsonConfig).Returns((true, []));
        _mockFileSystem.CreateDirectory(Arg.Any<string>()).Returns(callInfo => callInfo.Arg<string>());
        _mockFileSystem.GetFullNameFile(Arg.Any<string>()).Returns(configPath);

        var repoConfig = new AdrPlusRepoConfig
        {
            FolderByScope = true,
            Scopes = "frontend;backend;database"
        };
        _mockAdrServices.FromJson(jsonConfig, "", "").Returns(repoConfig);

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
        var args = new[] { "--path", "C:\\repo" };
        var parsedArgs = new Dictionary<Arguments, string> { { Arguments.TargetRepo, "C:\\repo" } };
        var repoPath = "C:\\repo\\docs\\adr";
        var configPath = Path.Combine(repoPath, ".adrplus");
        var jsonConfig = """{"Prefix": "ADR", "LenSeq": 4, "FolderByScope": true, "Scopes": "frontend;backend"}""";

        _mockAdrServices.ParseArgs(args, Arg.Any<Arguments[]>()).Returns(parsedArgs);
        _mockValidateConfig.HasTemplateRepoFile().Returns(true);
        _mockFileSystem.DirectoryExists("C:\\repo").Returns(true);
        _mockFileSystem.DirectoryExists(repoPath).Returns(false);
        _mockFileSystem.DirectoryExists(Arg.Is<string>(s => s.Contains("frontend"))).Returns(true);
        _mockFileSystem.DirectoryExists(Arg.Is<string>(s => s.Contains("backend"))).Returns(false);
        _mockFileSystem.FileExists(Arg.Any<string>()).Returns(false);
        _mockValidateConfig.GetFileNameRepoConfig().Returns(".adrplus");
        _mockValidateConfig.GetConfigRepoFilePath().Returns("template-path");
        _mockFileSystem.ReadAllTextAsync("template-path", Arg.Any<CancellationToken>()).Returns(jsonConfig);
        _mockValidateConfig.ValidateRepoStructure(jsonConfig).Returns((true, []));
        _mockFileSystem.CreateDirectory(Arg.Any<string>()).Returns(callInfo => callInfo.Arg<string>());
        _mockFileSystem.GetFullNameFile(Arg.Any<string>()).Returns(configPath);

        var repoConfig = new AdrPlusRepoConfig
        {
            FolderByScope = true,
            Scopes = "frontend;backend"
        };
        _mockAdrServices.FromJson(jsonConfig, "", "").Returns(repoConfig);

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
        var args = new[] { "--path", "C:\\repo" };
        var parsedArgs = new Dictionary<Arguments, string> { { Arguments.TargetRepo, "C:\\repo" } };
        var repoPath = "C:\\repo\\docs\\adr";
        var configPath = Path.Combine(repoPath, ".adrplus");
        var jsonConfig = """{"Prefix": "ADR", "LenSeq": 4, "FolderByScope": false, "Scopes": "frontend;backend"}""";

        _mockAdrServices.ParseArgs(args, Arg.Any<Arguments[]>()).Returns(parsedArgs);
        _mockValidateConfig.HasTemplateRepoFile().Returns(true);
        _mockFileSystem.DirectoryExists("C:\\repo").Returns(true);
        _mockFileSystem.DirectoryExists(repoPath).Returns(false);
        _mockFileSystem.FileExists(Arg.Any<string>()).Returns(false);
        _mockValidateConfig.GetFileNameRepoConfig().Returns(".adrplus");
        _mockValidateConfig.GetConfigRepoFilePath().Returns("template-path");
        _mockFileSystem.ReadAllTextAsync("template-path", Arg.Any<CancellationToken>()).Returns(jsonConfig);
        _mockValidateConfig.ValidateRepoStructure(jsonConfig).Returns((true, []));
        _mockFileSystem.CreateDirectory(repoPath).Returns(repoPath);
        _mockFileSystem.GetFullNameFile(Arg.Any<string>()).Returns(configPath);

        var repoConfig = new AdrPlusRepoConfig
        {
            FolderByScope = false,
            Scopes = "frontend;backend"
        };
        _mockAdrServices.FromJson(jsonConfig, "", "").Returns(repoConfig);

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
        var args = new[] { "--path", "C:\\repo" };
        var parsedArgs = new Dictionary<Arguments, string> { { Arguments.TargetRepo, "C:\\repo" } };
        var repoPath = "C:\\repo\\docs\\adr";
        var configPath = Path.Combine(repoPath, ".adrplus");
        var jsonConfig = """{"Prefix": "ADR", "LenSeq": 4, "FolderByScope": false}""";

        _mockAdrServices.ParseArgs(args, Arg.Any<Arguments[]>()).Returns(parsedArgs);
        _mockValidateConfig.HasTemplateRepoFile().Returns(true);
        _mockFileSystem.DirectoryExists("C:\\repo").Returns(true);
        _mockFileSystem.DirectoryExists(repoPath).Returns(false);
        _mockFileSystem.FileExists(Arg.Any<string>()).Returns(false);
        _mockValidateConfig.GetFileNameRepoConfig().Returns(".adrplus");
        _mockValidateConfig.GetConfigRepoFilePath().Returns("template-path");
        _mockFileSystem.ReadAllTextAsync("template-path", Arg.Any<CancellationToken>()).Returns(jsonConfig);
        _mockValidateConfig.ValidateRepoStructure(jsonConfig).Returns((true, []));
        _mockFileSystem.CreateDirectory(repoPath).Returns(repoPath);
        _mockFileSystem.GetFullNameFile(Arg.Any<string>()).Returns(configPath);

        var repoConfig = new AdrPlusRepoConfig { FolderByScope = false };
        _mockAdrServices.FromJson(jsonConfig, "", "").Returns(repoConfig);

        // Act
        await _handler.ExecuteAsync(args, CancellationToken.None);

        // Assert
        _mockFileSystem.Received(1).CreateDirectory(repoPath);
    }

    [Fact]
    public async Task ExecuteAsync_WhenAdrFolderExists_DoesNotCreateAdrFolder()
    {
        // Arrange
        var args = new[] { "--path", "C:\\repo" };
        var parsedArgs = new Dictionary<Arguments, string> { { Arguments.TargetRepo, "C:\\repo" } };
        var repoPath = "C:\\repo\\docs\\adr";
        var configPath = Path.Combine(repoPath, ".adrplus");
        var jsonConfig = """{"Prefix": "ADR", "LenSeq": 4, "FolderByScope": false}""";

        _mockAdrServices.ParseArgs(args, Arg.Any<Arguments[]>()).Returns(parsedArgs);
        _mockValidateConfig.HasTemplateRepoFile().Returns(true);
        _mockFileSystem.DirectoryExists("C:\\repo").Returns(true);
        _mockFileSystem.DirectoryExists(repoPath).Returns(true);
        _mockFileSystem.FileExists(Arg.Any<string>()).Returns(false);
        _mockValidateConfig.GetFileNameRepoConfig().Returns(".adrplus");
        _mockValidateConfig.GetConfigRepoFilePath().Returns("template-path");
        _mockFileSystem.ReadAllTextAsync("template-path", Arg.Any<CancellationToken>()).Returns(jsonConfig);
        _mockValidateConfig.ValidateRepoStructure(jsonConfig).Returns((true, []));
        _mockFileSystem.GetFullNameFile(Arg.Any<string>()).Returns(configPath);

        var repoConfig = new AdrPlusRepoConfig { FolderByScope = false };
        _mockAdrServices.FromJson(jsonConfig, "", "").Returns(repoConfig);

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
        _mockValidateConfig.GetConfigRepoFilePath().Returns("template-path");
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
        var args = new[] { "--path", "C:\\repo" };
        var parsedArgs = new Dictionary<Arguments, string> { { Arguments.TargetRepo, "C:\\repo" } };
        var repoPath = "C:\\repo\\docs\\adr";
        var configPath = "C:\\repo\\docs\\adr\\.adrplus";
        var jsonConfig = """{"Prefix": "ADR", "LenSeq": 4, "FolderByScope": true, "Scopes": "api"}""";

        _mockAdrServices.ParseArgs(args, Arg.Any<Arguments[]>()).Returns(parsedArgs);
        _mockValidateConfig.HasTemplateRepoFile().Returns(true);
        _mockFileSystem.DirectoryExists("C:\\repo").Returns(true);
        _mockFileSystem.DirectoryExists(repoPath).Returns(false);
        _mockFileSystem.DirectoryExists(Arg.Is<string>(s => s.Contains("api"))).Returns(false);
        _mockFileSystem.FileExists(Arg.Any<string>()).Returns(false);
        _mockValidateConfig.GetFileNameRepoConfig().Returns(".adrplus");
        _mockValidateConfig.GetConfigRepoFilePath().Returns("template-path");
        _mockFileSystem.ReadAllTextAsync("template-path", Arg.Any<CancellationToken>()).Returns(jsonConfig);
        _mockValidateConfig.ValidateRepoStructure(jsonConfig).Returns((true, []));
        _mockFileSystem.CreateDirectory(Arg.Any<string>()).Returns(callInfo => callInfo.Arg<string>());
        _mockFileSystem.GetFullNameFile(Arg.Any<string>()).Returns(configPath);

        var repoConfig = new AdrPlusRepoConfig
        {
            FolderByScope = true,
            Scopes = "api"
        };
        _mockAdrServices.FromJson(jsonConfig, "", "").Returns(repoConfig);

        // Act
        await _handler.ExecuteAsync(args, CancellationToken.None);

        // Assert
        _mockConsole.Received().WriteSuccess(configPath);
        _mockConsole.Received().WriteSuccess(Arg.Is<string>(s => s.Contains("api")));
    }

    #endregion
}
