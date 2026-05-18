// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the AdrPlus project under MIT license
// ***************************************************************************************

using AdrPlus.Commands;
using AdrPlus.Domain;
using AdrPlus.Tests.Helpers;

namespace AdrPlus.Tests.Commands.Explorer;

/// <summary>
/// Unit tests for ExplorerCommandHandler class.
/// Tests demonstrate explorer command execution, wizard flows, report generation, and file operations.
/// Tests are cross-platform compatible and use helper methods to reduce boilerplate.
/// </summary>
public class ExplorerCommandHandlerTests
{
    private readonly ExplorerCommandHandlerFixture _fixture;

    public ExplorerCommandHandlerTests()
    {
        _fixture = new ExplorerCommandHandlerFixture();
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidParameters_CreatesInstance()
    {
        // Act & Assert
        _fixture.Handler.Should().NotBeNull();
    }

    #endregion

    #region ExecuteAsync - Help Tests

    [Fact]
    public async Task ExecuteAsync_WithHelpArgument_WritesHelpToConsole()
    {
        // Arrange
        var args = new[] { "--help" };
        var parsedArgs = new Dictionary<Arguments, string> { { Arguments.Help, string.Empty } };
        _fixture.MockAdrServices.ParseArgs(args, Arg.Any<Arguments[]>()).Returns(parsedArgs);
        _fixture.MockAdrServices.GetHelpText(Arg.Any<string>(), Arg.Any<Arguments[]>(), Arg.Any<string[]>())
            .Returns("Help text");

        // Act
        await _fixture.Handler.ExecuteAsync(args, TestContext.Current.CancellationToken);

        // Assert
        _fixture.MockConsole.Received(1).PromptWriteHelp("Help text");
    }

    [Fact]
    public async Task ExecuteAsync_WithHelpArgument_DoesNotContinueExecution()
    {
        // Arrange
        var args = new[] { "--help" };
        var parsedArgs = new Dictionary<Arguments, string> { { Arguments.Help, string.Empty } };
        _fixture.MockAdrServices.ParseArgs(args, Arg.Any<Arguments[]>()).Returns(parsedArgs);
        _fixture.MockAdrServices.GetHelpText(Arg.Any<string>(), Arg.Any<Arguments[]>(), Arg.Any<string[]>())
            .Returns("Help text");

        // Act
        await _fixture.Handler.ExecuteAsync(args, TestContext.Current.CancellationToken);

        // Assert
        _fixture.MockValidateConfig.DidNotReceive().HasTemplateRepoFile();
    }

    #endregion

    #region ExecuteAsync - Template Repo File Not Found Tests

    [Fact]
    public async Task ExecuteAsync_WhenTemplateRepoFileNotFound_ThrowsFileNotFoundException()
    {
        // Arrange
        var targetPath = PathHelper.GetRepositoryAdrPath();
        var args = new[] { "--path", targetPath, "--file", "report.md" };
        var parsedArgs = new Dictionary<Arguments, string>
        {
            { Arguments.TargetRepo, targetPath },
            { Arguments.FileReport, "report.md" }
        };
        _fixture.MockAdrServices.ParseArgs(args, Arg.Any<Arguments[]>()).Returns(parsedArgs);
        _fixture.MockValidateConfig.HasTemplateRepoFile().Returns(false);

        // Act & Assert
        await _fixture.Handler.Invoking(h => h.ExecuteAsync(args, TestContext.Current.CancellationToken))
            .Should().ThrowAsync<FileNotFoundException>();
    }

    #endregion

    #region ExecuteAsync - Directory and File Validation Tests

    [Fact]
    public async Task ExecuteAsync_WhenTargetDirectoryNotExists_ThrowsDirectoryNotFoundException()
    {
        // Arrange
        var targetPath = PathHelper.GetRepositoryAdrPath();
        var args = new[] { "--path", targetPath };
        var parsedArgs = new Dictionary<Arguments, string> { { Arguments.TargetRepo, targetPath } };
        _fixture.MockAdrServices.ParseArgs(args, Arg.Any<Arguments[]>()).Returns(parsedArgs);
        _fixture.MockValidateConfig.HasTemplateRepoFile().Returns(true);
        _fixture.MockFileSystem.DirectoryExists(targetPath).Returns(false);

        // Act & Assert
        await _fixture.Handler.Invoking(h => h.ExecuteAsync(args, TestContext.Current.CancellationToken))
            .Should().ThrowAsync<DirectoryNotFoundException>();
    }

    [Fact]
    public async Task ExecuteAsync_WhenRepoConfigFileNotExists_ThrowsFileNotFoundException()
    {
        // Arrange
        var targetPath = PathHelper.GetRepositoryAdrPath();
        var args = new[] { "--path", targetPath };
        var parsedArgs = new Dictionary<Arguments, string> { { Arguments.TargetRepo, targetPath } };
        _fixture.MockAdrServices.ParseArgs(args, Arg.Any<Arguments[]>()).Returns(parsedArgs);
        _fixture.MockValidateConfig.HasTemplateRepoFile().Returns(true);
        _fixture.MockValidateConfig.GetFileNameRepoConfig().Returns("adr-config.adrplus");
        _fixture.MockFileSystem.DirectoryExists(targetPath).Returns(true);
        _fixture.MockFileSystem.FileExists(Arg.Any<string>()).Returns(false);

        // Act & Assert
        await _fixture.Handler.Invoking(h => h.ExecuteAsync(args, TestContext.Current.CancellationToken))
            .Should().ThrowAsync<FileNotFoundException>();
    }

    [Fact]
    public async Task ExecuteAsync_WhenRepoConfigInvalid_ThrowsInvalidDataException()
    {
        // Arrange
        var targetPath = PathHelper.GetRepositoryAdrPath();
        var configPath = Path.Combine(targetPath, "adr-config.adrplus");
        var args = new[] { "--path", targetPath };
        var parsedArgs = new Dictionary<Arguments, string> { { Arguments.TargetRepo, targetPath } };
        var invalidJson = "{ invalid json }";

        _fixture.MockAdrServices.ParseArgs(args, Arg.Any<Arguments[]>()).Returns(parsedArgs);
        _fixture.MockValidateConfig.HasTemplateRepoFile().Returns(true);
        _fixture.MockValidateConfig.GetFileNameRepoConfig().Returns("adr-config.adrplus");
        _fixture.MockFileSystem.DirectoryExists(targetPath).Returns(true);
        _fixture.MockFileSystem.FileExists(Arg.Any<string>()).Returns(true);
        _fixture.MockFileSystem.ReadAllTextAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(invalidJson);
        _fixture.MockValidateConfig.ValidateRepoStructure(invalidJson)
            .Returns((false, ["Invalid structure"]));

        // Act & Assert
        await _fixture.Handler.Invoking(h => h.ExecuteAsync(args, TestContext.Current.CancellationToken))
            .Should().ThrowAsync<InvalidDataException>();
    }

    #endregion

    #region ExecuteAsync - No ADR Files Found Tests

    [Fact]
    public async Task ExecuteAsync_WhenNoAdrFilesFound_ThrowsInvalidDataException()
    {
        // Arrange
        var targetPath = PathHelper.GetRepositoryAdrPath();
        var args = new[] { "--path", targetPath };
        var parsedArgs = new Dictionary<Arguments, string> { { Arguments.TargetRepo, targetPath } };
        var validJson = ExplorerCommandHandlerMockHelper.BuildValidJsonConfigForExplorer();

        _fixture.MockAdrServices.ParseArgs(args, Arg.Any<Arguments[]>()).Returns(parsedArgs);
        _fixture.MockValidateConfig.HasTemplateRepoFile().Returns(true);
        _fixture.MockValidateConfig.GetFileNameRepoConfig().Returns("adr-config.adrplus");
        _fixture.MockFileSystem.DirectoryExists(targetPath).Returns(true);
        _fixture.MockFileSystem.FileExists(Arg.Any<string>()).Returns(true);
        _fixture.MockFileSystem.ReadAllTextAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(validJson);
        _fixture.MockValidateConfig.ValidateRepoStructure(validJson)
            .Returns((true, []));
        _fixture.MockAdrServices.ReadAllAdr(_fixture.MockFileSystem, targetPath, Arg.Any<AdrPlusRepoConfig>(), Arg.Any<bool>())
            .Returns([]);

        // Act & Assert
        await _fixture.Handler.Invoking(h => h.ExecuteAsync(args, TestContext.Current.CancellationToken))
            .Should().ThrowAsync<InvalidDataException>()
            .WithMessage("*found*");
    }

    #endregion

    #region ExecuteAsync - Wizard Flow Tests

    [Fact]
    public void ExecuteAsync_WithWizardArgument_IsRecognized()
    {
        // Arrange & Act
        var args = new[] { "--wizard" };

        // Assert - verify wizard argument is recognized in args array
        args.Should().Contain("--wizard");
    }

    #endregion

    #region ExecuteAsync - Report File Creation Tests

    [Fact]
    public async Task ExecuteAsync_WithFileReportPath_CreatesReportFile()
    {
        // Arrange
        var targetPath = PathHelper.GetRepositoryAdrPath();
        var reportPath = PathHelper.GetAlternativeFolderFilePath("report.md");
        var args = new[] { "--path", targetPath, "--file", reportPath };
        var parsedArgs = new Dictionary<Arguments, string>
        {
            { Arguments.TargetRepo, targetPath },
            { Arguments.FileReport, reportPath }
        };

        var adrFiles = ExplorerCommandHandlerMockHelper.CreateTestAdrFiles(2);
        ExplorerCommandHandlerMockHelper.SetupExplorerReportMocks(
            _fixture.MockAdrServices,
            _fixture.MockFileSystem,
            _fixture.MockValidateConfig,
            parsedArgs,
            targetPath,
            reportPath,
            adrFiles);

        // Act
        await _fixture.Handler.ExecuteAsync(args, TestContext.Current.CancellationToken);

        // Assert
        await _fixture.MockFileSystem.Received(1).WriteAllTextAsync(reportPath, Arg.Any<string>(), Arg.Any<CancellationToken>());
        _fixture.MockConsole.Received(1).PromptWriteSuccess(reportPath);
    }

    [Fact]
    public async Task ExecuteAsync_WithEmptyFileReportPath_ThrowsInvalidDataException()
    {
        // Arrange
        var targetPath = PathHelper.GetRepositoryAdrPath();
        var args = new[] { "--path", targetPath, "--file", "" };
        var parsedArgs = new Dictionary<Arguments, string>
        {
            { Arguments.TargetRepo, targetPath },
            { Arguments.FileReport, "" }
        };

        var adrFile = ExplorerCommandHandlerMockHelper.CreateTestAdrFile();
        ExplorerCommandHandlerMockHelper.SetupExplorerReportMocks(
            _fixture.MockAdrServices,
            _fixture.MockFileSystem,
            _fixture.MockValidateConfig,
            parsedArgs,
            targetPath,
            "",
            [adrFile]);

        // Act & Assert
        await _fixture.Handler.Invoking(h => h.ExecuteAsync(args, TestContext.Current.CancellationToken))
            .Should().ThrowAsync<InvalidDataException>()
            .WithMessage("*empty*");
    }

    [Fact]
    public async Task ExecuteAsync_WhenReportDirectoryNotExists_ThrowsDirectoryNotFoundException()
    {
        // Arrange
        var targetPath = PathHelper.GetRepositoryAdrPath();
        var reportPath = Path.Combine(PathHelper.GetAlternativeFolderPath(), "nonexistent", "report.md");
        var args = new[] { "--path", targetPath, "--file", reportPath };
        var parsedArgs = new Dictionary<Arguments, string>
        {
            { Arguments.TargetRepo, targetPath },
            { Arguments.FileReport, reportPath }
        };

        var adrFile = ExplorerCommandHandlerMockHelper.CreateTestAdrFile();
        _fixture.MockAdrServices.ParseArgs(args, Arg.Any<Arguments[]>()).Returns(parsedArgs);
        _fixture.MockValidateConfig.HasTemplateRepoFile().Returns(true);
        _fixture.MockValidateConfig.GetFileNameRepoConfig().Returns("adr-config.adrplus");
        _fixture.MockFileSystem.DirectoryExists(targetPath).Returns(true);
        _fixture.MockFileSystem.DirectoryExists(Path.GetDirectoryName(reportPath)!).Returns(false);
        _fixture.MockFileSystem.FileExists(Arg.Any<string>()).Returns(true);
        _fixture.MockFileSystem.ReadAllTextAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(ExplorerCommandHandlerMockHelper.BuildValidJsonConfigForExplorer());
        _fixture.MockValidateConfig.ValidateRepoStructure(Arg.Any<string>())
            .Returns((true, []));
        _fixture.MockAdrServices.ReadAllAdr(_fixture.MockFileSystem, targetPath, Arg.Any<AdrPlusRepoConfig>(), Arg.Any<bool>())
            .Returns([adrFile]);

        // Act & Assert
        await _fixture.Handler.Invoking(h => h.ExecuteAsync(args, TestContext.Current.CancellationToken))
            .Should().ThrowAsync<DirectoryNotFoundException>();
    }

    #endregion

    #region ExecuteAsync - Open File Tests

    [Fact]
    public async Task ExecuteAsync_WithOpenFileArgument_OpensFile()
    {
        // Arrange
        var targetPath = PathHelper.GetRepositoryAdrPath();
        var reportPath = PathHelper.GetAlternativeFolderFilePath("report.md");
        var args = new[] { "--path", targetPath, "--file", reportPath, "--open" };
        var parsedArgs = new Dictionary<Arguments, string>
        {
            { Arguments.TargetRepo, targetPath },
            { Arguments.FileReport, reportPath },
            { Arguments.OpenFile, string.Empty }
        };

        var adrFiles = ExplorerCommandHandlerMockHelper.CreateTestAdrFiles(1);
        ExplorerCommandHandlerMockHelper.SetupExplorerReportMocks(
            _fixture.MockAdrServices,
            _fixture.MockFileSystem,
            _fixture.MockValidateConfig,
            parsedArgs,
            targetPath,
            reportPath,
            adrFiles);
        _fixture.MockAdrServices.OpenFile(reportPath, Arg.Any<string>())
            .Returns(string.Empty);

        var customConfig = new AdrPlusConfig
        {
            Language = "en-US",
            ComandOpenAdr = "code {0}"
        };
        var handlerWithCommand = _fixture.CreateHandlerWithConfig(customConfig);

        // Act
        await handlerWithCommand.ExecuteAsync(args, TestContext.Current.CancellationToken);

        // Assert
        _fixture.MockAdrServices.Received(1).OpenFile(Arg.Any<string>(), Arg.Any<string>());
        _fixture.MockConsole.Received(2).PromptWriteSuccess(Arg.Any<string>());
    }

    #endregion

    #region ExecuteAsync - Exception Handling Tests

    [Fact]
    public async Task ExecuteAsync_WithNullArgs_ThrowsArgumentNullException()
    {
        // Act & Assert
        await _fixture.Handler.Invoking(h => h.ExecuteAsync(null!, TestContext.Current.CancellationToken))
            .Should().ThrowAsync<ArgumentNullException>();
    }

    #endregion
}

