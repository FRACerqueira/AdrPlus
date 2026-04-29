// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the AdrPlus project under MIT license
// ***************************************************************************************

using AdrPlus.Commands;
using AdrPlus.Commands.Upgrade;
using AdrPlus.Core;
using AdrPlus.Domain;
using AdrPlus.Infrastructure.FileSystem;
using AdrPlus.Infrastructure.UI;
using AdrPlus.Tests.Helpers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using static AdrPlus.Tests.Helpers.TestPathData;

namespace AdrPlus.Tests.Commands.Upgrade;

/// <summary>
/// Unit tests for RepoCommandHandler class.
/// Tests demonstrate repo command execution, configuration changes, wizard flows, and validation patterns using NSubstitute.
/// </summary>
public class UpgradeCommandHandlerTests
{
    private readonly ILogger<UpgradeCommandHandler> _mockLogger;
    private readonly IFileSystemService _mockFileSystem;
    private readonly IConsoleWriter _mockConsole;
    private readonly IValidateJsonConfig _mockValidateConfig;
    private readonly IAdrServices _mockAdrServices;
    private readonly AdrPlusConfig _config;
    private readonly UpgradeCommandHandler _handler;

    public UpgradeCommandHandlerTests()
    {
        _mockLogger = Substitute.For<ILogger<UpgradeCommandHandler>>();
        _mockFileSystem = Substitute.For<IFileSystemService>();
        _mockConsole = Substitute.For<IConsoleWriter>();
        _mockValidateConfig = Substitute.For<IValidateJsonConfig>();
        _mockAdrServices = Substitute.For<IAdrServices>();

        _config = new AdrPlusConfig
        {
            Language = "en-US"
        };

        _handler = new UpgradeCommandHandler(
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
        var handler = new UpgradeCommandHandler(
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

    #region ExecuteAsync - Template File Validation Tests

    [Fact]
    public async Task ExecuteAsync_WhenTemplateRepoFileNotFound_ThrowsFileNotFoundException()
    {
        // Arrange
        var args = new[] { "--template", "--path", RepositoryPath };
        var parsedArgs = new Dictionary<Arguments, string>
        {
            { Arguments.RepoTemplate, string.Empty },
            { Arguments.TargetRepoAdr, RepositoryPath }
        };

        _mockAdrServices.ParseArgs(args, Arg.Any<Arguments[]>()).Returns(parsedArgs);
        _mockValidateConfig.HasTemplateRepoFile().Returns(false);

        // Act & Assert
        await _handler.Invoking(h => h.ExecuteAsync(args, CancellationToken.None))
            .Should().ThrowAsync<FileNotFoundException>();
    }

    #endregion

    #region ExecuteAsync - Config File Tests

    [Fact]
    public async Task ExecuteAsync_WhenConfigFileNotFound_ThrowsFileNotFoundException()
    {
        // Arrange
        var args = new[] { "--version", "2", "--path", RepositoryPath };
        var parsedArgs = new Dictionary<Arguments, string>
        {
            { Arguments.RepoVersion, "2" },
            { Arguments.TargetRepoAdr, RepositoryPath }
        };

        _mockAdrServices.ParseArgs(args, Arg.Any<Arguments[]>()).Returns(parsedArgs);
        _mockValidateConfig.HasTemplateRepoFile().Returns(true);
        _mockValidateConfig.GetFileNameRepoConfig().Returns(".adrplus");
        _mockFileSystem.FileExists(Arg.Any<string>()).Returns(false);

        // Act & Assert
        await _handler.Invoking(h => h.ExecuteAsync(args, CancellationToken.None))
            .Should().ThrowAsync<FileNotFoundException>();
    }

    [Fact]
    public async Task ExecuteAsync_WhenInvalidRepoConfig_ThrowsInvalidDataException()
    {
        // Arrange
        var args = new[] { "--version", "2", "--path", RepositoryPath };
        var parsedArgs = new Dictionary<Arguments, string>
        {
            { Arguments.RepoVersion, "2" },
            { Arguments.TargetRepoAdr, RepositoryPath }
        };
        var configPath = ConfigFilePath;
        var jsonConfig = """{"Invalid": "config"}""";
        var errors = new[] { "Missing Prefix field" };

        SetupBasicMocks(parsedArgs, jsonConfig, configPath);
        _mockValidateConfig.ValidateRepoStructure(jsonConfig).Returns((false, errors));

        // Act & Assert
        await _handler.Invoking(h => h.ExecuteAsync(args, CancellationToken.None))
            .Should().ThrowAsync<InvalidDataException>();

        _mockConsole.Received(1).WriteError("Missing Prefix field");
    }

    #endregion

    #region ExecuteAsync - Template Configuration Tests

    [Fact]
    public async Task ExecuteAsync_WithTemplateChange_UpdatesConfigFile()
    {
        // Arrange
        var templatePath = PathHelper.GetAdrTemplatePath();
        var args = new[] { "--template", "--file", templatePath, "--path", RepositoryPath };
        var parsedArgs = new Dictionary<Arguments, string>
        {
            { Arguments.RepoTemplate, string.Empty },
            { Arguments.FileTemplate, templatePath },
            { Arguments.TargetRepoAdr, RepositoryPath }
        };
        var jsonConfig = GetValidRepoConfig();
        var configPath = ConfigFilePath;

        SetupBasicMocks(parsedArgs, jsonConfig, configPath);
        _mockFileSystem.FileExists(templatePath).Returns(true);

        // Act
        await _handler.ExecuteAsync(args, CancellationToken.None);

        // Assert
        await _mockFileSystem.Received(1).WriteAllTextAsync(
            configPath,
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
        _mockConsole.Received(1).WriteSuccess(Arg.Any<string>());
    }

    [Fact]
    public async Task ExecuteAsync_WithTemplateChange_WhenFileNotFound_ThrowsFileNotFoundException()
    {
        // Arrange
        var missingTemplate = PathHelper.GetAlternativeFolderFilePath("missing-template.md");
        var args = new[] { "--template", "--file", missingTemplate, "--path", RepositoryPath };
        var parsedArgs = new Dictionary<Arguments, string>
        {
            { Arguments.RepoTemplate, string.Empty },
            { Arguments.FileTemplate, missingTemplate },
            { Arguments.TargetRepoAdr, RepositoryPath }
        };
        var jsonConfig = GetValidRepoConfig();
        var configPath = ConfigFilePath;

        SetupBasicMocks(parsedArgs, jsonConfig, configPath);
        _mockFileSystem.FileExists(missingTemplate).Returns(false);

        // Act & Assert
        await _handler.Invoking(h => h.ExecuteAsync(args, CancellationToken.None))
            .Should().ThrowAsync<FileNotFoundException>();
    }

    [Fact]
    public async Task ExecuteAsync_WithTemplateChange_WhenInvalidExtension_ThrowsInvalidOperationException()
    {
        // Arrange
        var invalidTemplate = PathHelper.GetAlternativeFolderFilePath("template.txt");
        var args = new[] { "--template", "--file", invalidTemplate, "--path", RepositoryPath };
        var parsedArgs = new Dictionary<Arguments, string>
        {
            { Arguments.RepoTemplate, string.Empty },
            { Arguments.FileTemplate, invalidTemplate },
            { Arguments.TargetRepoAdr, RepositoryPath }
        };
        var jsonConfig = GetValidRepoConfig();
        var configPath = ConfigFilePath;

        SetupBasicMocks(parsedArgs, jsonConfig, configPath);
        _mockFileSystem.FileExists(invalidTemplate).Returns(true);

        // Act & Assert
        await _handler.Invoking(h => h.ExecuteAsync(args, CancellationToken.None))
            .Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task ExecuteAsync_WithTemplateFile_ButNoTemplateFlag_ThrowsArgumentException()
    {
        // Arrange
        var templatePath = PathHelper.GetAdrTemplatePath();
        var args = new[] { "--file", templatePath, "--path", RepositoryPath };
        var parsedArgs = new Dictionary<Arguments, string>
        {
            { Arguments.FileTemplate, templatePath },
            { Arguments.TargetRepoAdr, RepositoryPath }
        };
        var jsonConfig = GetValidRepoConfig();
        var configPath = ConfigFilePath;

        SetupBasicMocks(parsedArgs, jsonConfig, configPath);

        // Act & Assert
        await _handler.Invoking(h => h.ExecuteAsync(args, CancellationToken.None))
            .Should().ThrowAsync<ArgumentException>();
    }

    #endregion

    #region ExecuteAsync - Version Configuration Tests

    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    public async Task ExecuteAsync_WithValidVersion_UpdatesConfigFile(int version)
    {
        // Arrange
        var args = new[] { "--version", version.ToString(), "--path", RepositoryPath };
        var parsedArgs = new Dictionary<Arguments, string>
        {
            { Arguments.RepoVersion, version.ToString() },
            { Arguments.TargetRepoAdr, RepositoryPath }
        };
        var jsonConfig = GetValidRepoConfig();
        var configPath = ConfigFilePath;

        SetupBasicMocks(parsedArgs, jsonConfig, configPath);

        // Act
        await _handler.ExecuteAsync(args, CancellationToken.None);

        // Assert
        await _mockFileSystem.Received(1).WriteAllTextAsync(
            configPath,
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
        _mockConsole.Received(1).WriteSuccess(Arg.Any<string>());
    }

    [Theory]
    [InlineData(1)]
    [InlineData(4)]
    public async Task ExecuteAsync_WithInvalidVersion_ThrowsArgumentException(int version)
    {
        // Arrange
        var args = new[] { "--version", version.ToString(), "--path", RepositoryPath };
        var parsedArgs = new Dictionary<Arguments, string>
        {
            { Arguments.RepoVersion, version.ToString() },
            { Arguments.TargetRepoAdr, RepositoryPath }
        };
        var jsonConfig = GetValidRepoConfig();
        var configPath = ConfigFilePath;

        SetupBasicMocks(parsedArgs, jsonConfig, configPath);

        // Act & Assert
        await _handler.Invoking(h => h.ExecuteAsync(args, CancellationToken.None))
            .Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task ExecuteAsync_WithVersionNotProvided_ThrowsArgumentException()
    {
        // Arrange
        var args = new[] { "--version", "--path", RepositoryPath };
        var parsedArgs = new Dictionary<Arguments, string>
        {
            { Arguments.RepoVersion, string.Empty },
            { Arguments.TargetRepoAdr, RepositoryPath }
        };
        var jsonConfig = GetValidRepoConfig();
        var configPath = ConfigFilePath;

        SetupBasicMocks(parsedArgs, jsonConfig, configPath);

        // Act & Assert
        await _handler.Invoking(h => h.ExecuteAsync(args, CancellationToken.None))
            .Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task ExecuteAsync_WithVersionAlreadySet_ThrowsInvalidOperationException()
    {
        // Arrange
        var args = new[] { "--version", "1", "--path", RepositoryPath };
        var parsedArgs = new Dictionary<Arguments, string>
        {
            { Arguments.RepoVersion, "1" },
            { Arguments.TargetRepoAdr, RepositoryPath }
        };
        var jsonConfig = """{"Prefix": "ADR", "LenSeq": 4, "LenVersion": 1}""";
        var configPath = ConfigFilePath;

        SetupBasicMocks(parsedArgs, jsonConfig, configPath);

        // Act & Assert
        await _handler.Invoking(h => h.ExecuteAsync(args, CancellationToken.None))
            .Should().ThrowAsync<ArgumentException>();
    }

    #endregion

    #region ExecuteAsync - Revision Configuration Tests

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public async Task ExecuteAsync_WithValidRevision_UpdatesConfigFile(int revision)
    {
        // Arrange
        var args = new[] { "--revision", revision.ToString(), "--path", RepositoryPath };
        var parsedArgs = new Dictionary<Arguments, string>
        {
            { Arguments.RepoRevision, revision.ToString() },
            { Arguments.TargetRepoAdr, RepositoryPath }
        };
        var jsonConfig = GetValidRepoConfig();
        var configPath = ConfigFilePath;

        SetupBasicMocks(parsedArgs, jsonConfig, configPath);

        // Act
        await _handler.ExecuteAsync(args, CancellationToken.None);

        // Assert
        await _mockFileSystem.Received(1).WriteAllTextAsync(
            configPath,
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
        _mockConsole.Received(1).WriteSuccess(Arg.Any<string>());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(4)]
    public async Task ExecuteAsync_WithInvalidRevision_ThrowsArgumentException(int revision)
    {
        // Arrange
        var args = new[] { "--revision", revision.ToString(), "--path", RepositoryPath };
        var parsedArgs = new Dictionary<Arguments, string>
        {
            { Arguments.RepoRevision, revision.ToString() },
            { Arguments.TargetRepoAdr, RepositoryPath }
        };
        var jsonConfig = GetValidRepoConfig();
        var configPath = ConfigFilePath;

        SetupBasicMocks(parsedArgs, jsonConfig, configPath);

        // Act & Assert
        await _handler.Invoking(h => h.ExecuteAsync(args, CancellationToken.None))
            .Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task ExecuteAsync_WithRevisionAlreadySet_ThrowsInvalidOperationException()
    {
        // Arrange
        var args = new[] { "--revision", "1", "--path", RepositoryPath };
        var parsedArgs = new Dictionary<Arguments, string>
        {
            { Arguments.RepoRevision, "1" },
            { Arguments.TargetRepoAdr, RepositoryPath }
        };
        var jsonConfig = """{"Prefix": "ADR", "LenSeq": 4, "LenRevision": 1, "FolderAdr": "adr"}""";
        var configPath = ConfigFilePath;

        SetupBasicMocks(parsedArgs, jsonConfig, configPath);

        // Act & Assert
        await _handler.Invoking(h => h.ExecuteAsync(args, CancellationToken.None))
            .Should().ThrowAsync<InvalidOperationException>();
    }

    #endregion

    #region ExecuteAsync - Scope Configuration Tests

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(5)]
    public async Task ExecuteAsync_WithValidScope_UpdatesConfigFile(int scopeValue)
    {
        // Arrange
        var scopeItems = "Enterprise;Business;Application";
        var args = new[] { "--scope", scopeValue.ToString(), "--items", scopeItems, "--path", RepositoryPath };
        var parsedArgs = new Dictionary<Arguments, string>
        {
            { Arguments.RepoScope, scopeValue.ToString() },
            { Arguments.RepoScopeItems, scopeItems },
            { Arguments.TargetRepoAdr, RepositoryPath }
        };
        var jsonConfig = GetValidRepoConfig();
        var configPath = ConfigFilePath;

        SetupBasicMocks(parsedArgs, jsonConfig, configPath);

        // Act
        await _handler.ExecuteAsync(args, CancellationToken.None);

        // Assert
        await _mockFileSystem.Received(1).WriteAllTextAsync(
            configPath,
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
        _mockConsole.Received(1).WriteSuccess(Arg.Any<string>());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    public async Task ExecuteAsync_WithInvalidScope_ThrowsArgumentException(int scope)
    {
        // Arrange
        var args = new[] { "--scope", scope.ToString(), "--items", "Item1", "--path", RepositoryPath };
        var parsedArgs = new Dictionary<Arguments, string>
        {
            { Arguments.RepoScope, scope.ToString() },
            { Arguments.RepoScopeItems, "Item1" },
            { Arguments.TargetRepoAdr, RepositoryPath }
        };
        var jsonConfig = GetValidRepoConfig();
        var configPath = ConfigFilePath;

        SetupBasicMocks(parsedArgs, jsonConfig, configPath);

        // Act & Assert
        await _handler.Invoking(h => h.ExecuteAsync(args, CancellationToken.None))
            .Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task ExecuteAsync_WithScopeButNoItems_ThrowsArgumentException()
    {
        // Arrange
        var args = new[] { "--scope", "1", "--path", RepositoryPath };
        var parsedArgs = new Dictionary<Arguments, string>
        {
            { Arguments.RepoScope, "1" },
            { Arguments.TargetRepoAdr, RepositoryPath }
        };
        var jsonConfig = GetValidRepoConfig();
        var configPath = ConfigFilePath;

        SetupBasicMocks(parsedArgs, jsonConfig, configPath);

        // Act & Assert
        await _handler.Invoking(h => h.ExecuteAsync(args, CancellationToken.None))
            .Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task ExecuteAsync_WithDuplicateScopeItems_ThrowsArgumentException()
    {
        // Arrange
        var scopeItems = "Enterprise;Enterprise;Application";
        var args = new[] { "--scope", "2", "--items", scopeItems, "--path", RepositoryPath };
        var parsedArgs = new Dictionary<Arguments, string>
        {
            { Arguments.RepoScope, "2" },
            { Arguments.RepoScopeItems, scopeItems },
            { Arguments.TargetRepoAdr, RepositoryPath }
        };
        var jsonConfig = GetValidRepoConfig();
        var configPath = ConfigFilePath;

        SetupBasicMocks(parsedArgs, jsonConfig, configPath);

        // Act & Assert
        await _handler.Invoking(h => h.ExecuteAsync(args, CancellationToken.None))
            .Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task ExecuteAsync_WithScopeAlreadySet_ThrowsInvalidOperationException()
    {
        // Arrange
        var args = new[] { "--scope", "1", "--items", "Enterprise", "--path", RepositoryPath };
        var parsedArgs = new Dictionary<Arguments, string>
        {
            { Arguments.RepoScope, "1" },
            { Arguments.RepoScopeItems, "Enterprise" },
            { Arguments.TargetRepoAdr, RepositoryPath }
        };
        var jsonConfig = """{"Prefix": "ADR", "LenSeq": 4, "LenScope": 1, "Scopes": "Enterprise"}""";
        var configPath = ConfigFilePath;

        SetupBasicMocks(parsedArgs, jsonConfig, configPath);

        // Act & Assert
        await _handler.Invoking(h => h.ExecuteAsync(args, CancellationToken.None))
            .Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task ExecuteAsync_WithScopeAndFolderFlag_CreatesScopeDirectories()
    {
        // Arrange
        var scopeItems = "Enterprise;Business";
        var args = new[] { "--scope", "1", "--items", scopeItems, "--withfolders", "--path", RepositoryPath };
        var parsedArgs = new Dictionary<Arguments, string>
        {
            { Arguments.RepoScope, "1" },
            { Arguments.RepoScopeItems, scopeItems },
            { Arguments.RepoWithFolders, string.Empty },
            { Arguments.TargetRepoAdr, RepositoryPath }
        };
        var jsonConfig = GetValidRepoConfig();
        var configPath = ConfigFilePath;

        SetupBasicMocks(parsedArgs, jsonConfig, configPath);
        _mockFileSystem.DirectoryExists(Arg.Any<string>()).Returns(false);
        _mockFileSystem.CreateDirectory(Arg.Any<string>()).Returns(callInfo => callInfo.Arg<string>());

        // Act
        await _handler.ExecuteAsync(args, CancellationToken.None);

        // Assert
        _mockFileSystem.Received(2).CreateDirectory(Arg.Any<string>());
    }

    [Fact]
    public async Task ExecuteAsync_WithScopeValueGreaterThanMinLength_AdjustsScopeLength()
    {
        // Arrange
        var scopeItems = "App;Business;Enterprise";
        var args = new[] { "--scope", "5", "--items", scopeItems, "--path", RepositoryPath };
        var parsedArgs = new Dictionary<Arguments, string>
        {
            { Arguments.RepoScope, "5" },
            { Arguments.RepoScopeItems, scopeItems },
            { Arguments.TargetRepoAdr, RepositoryPath }
        };
        var jsonConfig = GetValidRepoConfig();
        var configPath = ConfigFilePath;

        SetupBasicMocks(parsedArgs, jsonConfig, configPath);

        // Act
        await _handler.ExecuteAsync(args, CancellationToken.None);

        // Assert - should adjust to 3 (min length of scope items)
        await _mockFileSystem.Received(1).WriteAllTextAsync(
            configPath,
            Arg.Is<string>(s => s.Contains("\"lenscope\": 3") || s.Contains("\"LenScope\":3")),
            Arg.Any<CancellationToken>());
    }

    #endregion

    #region ExecuteAsync - Directory Creation Tests

    [Fact]
    public async Task ExecuteAsync_WithScopeAndFolders_WhenDirectoryAlreadyExists_SkipsSilently()
    {
        // Arrange
        var scopeItems = "Enterprise;Business";
        var args = new[] { "--scope", "1", "--items", scopeItems, "--withfolders", "--path", RepositoryPath };
        var parsedArgs = new Dictionary<Arguments, string>
        {
            { Arguments.RepoScope, "1" },
            { Arguments.RepoScopeItems, scopeItems },
            { Arguments.RepoWithFolders, string.Empty },
            { Arguments.TargetRepoAdr, RepositoryPath }
        };
        var jsonConfig = GetValidRepoConfig();
        var configPath = ConfigFilePath;

        SetupBasicMocks(parsedArgs, jsonConfig, configPath);
        _mockFileSystem.DirectoryExists(Arg.Any<string>()).Returns(true);

        // Act
        await _handler.ExecuteAsync(args, CancellationToken.None);

        // Assert
        _mockFileSystem.Received(0).CreateDirectory(Arg.Any<string>());
    }

    [Fact]
    public async Task ExecuteAsync_WithoutFolderFlag_DoesNotCreateDirectories()
    {
        // Arrange
        var scopeItems = "Enterprise;Business";
        var args = new[] { "--scope", "1", "--items", scopeItems, "--path", RepositoryPath };
        var parsedArgs = new Dictionary<Arguments, string>
        {
            { Arguments.RepoScope, "1" },
            { Arguments.RepoScopeItems, scopeItems },
            { Arguments.TargetRepoAdr, RepositoryPath }
        };
        var jsonConfig = GetValidRepoConfig();
        var configPath = ConfigFilePath;

        SetupBasicMocks(parsedArgs, jsonConfig, configPath);

        // Act
        await _handler.ExecuteAsync(args, CancellationToken.None);

        // Assert
        _mockFileSystem.Received(0).CreateDirectory(Arg.Any<string>());
    }

    #endregion

    #region ExecuteAsync - Wizard Mode Tests

    [Fact]
    public async Task ExecuteAsync_WithWizardMode_CompletesSuccessfully()
    {
        // Arrange
        var args = new[] { "--wizard" };
        var parsedArgs = new Dictionary<Arguments, string> { { Arguments.WizardRepo, string.Empty } };
        var drives = TestDrives;

        _mockAdrServices.ParseArgs(args, Arg.Any<Arguments[]>()).Returns(parsedArgs);
        _mockValidateConfig.HasTemplateRepoFile().Returns(true);
        _mockFileSystem.GetDrives().Returns(drives);
        _mockConsole.PromptSelectLogicalDrive(Arg.Any<string>(), _mockFileSystem, Arg.Any<CancellationToken>())
            .Returns((false, SingleTestDrive));
        _mockConsole.PromptSelectFolderRepositoryAdr(SingleTestDrive, _mockFileSystem, _mockValidateConfig, Arg.Any<CancellationToken>())
            .Returns((false, RepositoryPath));
        _mockConsole.PromptSelectRepoActions(Arg.Any<CancellationToken>())
            .Returns((false, []));
        _mockConsole.PromptConfirm(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((false, true));

        SetupWizardMocks(GetValidRepoConfig());

        // Act
        await _handler.ExecuteAsync(args, CancellationToken.None);

        // Assert
        await _mockFileSystem.Received(1).WriteAllTextAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WithWizardModeDriveSelectionAborted_ThrowsOperationCanceledException()
    {
        // Arrange
        var args = new[] { "--wizard" };
        var parsedArgs = new Dictionary<Arguments, string> { { Arguments.WizardRepo, string.Empty } };
        var drives = TestDrives;

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
        var parsedArgs = new Dictionary<Arguments, string> { { Arguments.WizardRepo, string.Empty } };
        var drives = new[] { SingleTestDrive };

        _mockAdrServices.ParseArgs(args, Arg.Any<Arguments[]>()).Returns(parsedArgs);
        _mockValidateConfig.HasTemplateRepoFile().Returns(true);
        _mockFileSystem.GetDrives().Returns(drives);
        _mockConsole.PromptSelectFolderRepositoryAdr(SingleTestDrive, _mockFileSystem, _mockValidateConfig, Arg.Any<CancellationToken>())
            .Returns((true, string.Empty));

        // Act & Assert
        await _handler.Invoking(h => h.ExecuteAsync(args, CancellationToken.None))
            .Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task ExecuteAsync_WithWizardModeTemplateAborted_ThrowsOperationCanceledException()
    {
        // Arrange
        var args = new[] { "--wizard" };
        var parsedArgs = new Dictionary<Arguments, string> { { Arguments.WizardRepo, string.Empty } };
        var drives = new[] { SingleTestDrive };

        _mockAdrServices.ParseArgs(args, Arg.Any<Arguments[]>()).Returns(parsedArgs);
        _mockValidateConfig.HasTemplateRepoFile().Returns(true);
        _mockFileSystem.GetDrives().Returns(drives);
        _mockConsole.PromptSelectFolderRepositoryAdr(SingleTestDrive, _mockFileSystem, _mockValidateConfig, Arg.Any<CancellationToken>())
            .Returns((false, RepositoryPath));
        _mockConsole.PromptSelectRepoActions(Arg.Any<CancellationToken>())
            .Returns((false, [RepoActions.Template]));
        _mockConsole.PromptConfigTemplateAdrSelect(SingleTestDrive, Arg.Any<CancellationToken>())
            .Returns((true, string.Empty));

        // Act & Assert
        await _handler.Invoking(h => h.ExecuteAsync(args, CancellationToken.None))
            .Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task ExecuteAsync_WithWizardModeVersionSelected_ConfiguresVersion()
    {
        // Arrange
        var args = new[] { "--wizard" };
        var parsedArgs = new Dictionary<Arguments, string> { { Arguments.WizardRepo, string.Empty } };
        var drives = new[] { SingleTestDrive };

        _mockAdrServices.ParseArgs(args, Arg.Any<Arguments[]>()).Returns(parsedArgs);
        _mockValidateConfig.HasTemplateRepoFile().Returns(true);
        _mockFileSystem.GetDrives().Returns(drives);
        _mockConsole.PromptSelectFolderRepositoryAdr(SingleTestDrive, _mockFileSystem, _mockValidateConfig, Arg.Any<CancellationToken>())
            .Returns((false, RepositoryPath));
        _mockConsole.PromptSelectRepoActions(Arg.Any<CancellationToken>())
            .Returns((false, [RepoActions.Version]));
        _mockConsole.PromptEditFieldVersion(Arg.Any<FieldsJson>(), Arg.Any<CancellationToken>())
            .Returns((false, 2));
        _mockConsole.PromptConfirm(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((false, true));

        SetupWizardMocks(GetValidRepoConfig());

        // Act
        await _handler.ExecuteAsync(args, CancellationToken.None);

        // Assert
        await _mockFileSystem.Received(1).WriteAllTextAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WithWizardModeRevisionSelected_ConfiguresRevision()
    {
        // Arrange
        var args = new[] { "--wizard" };
        var parsedArgs = new Dictionary<Arguments, string> { { Arguments.WizardRepo, string.Empty } };
        var drives = new[] { SingleTestDrive };

        _mockAdrServices.ParseArgs(args, Arg.Any<Arguments[]>()).Returns(parsedArgs);
        _mockValidateConfig.HasTemplateRepoFile().Returns(true);
        _mockFileSystem.GetDrives().Returns(drives);
        _mockConsole.PromptSelectFolderRepositoryAdr(SingleTestDrive, _mockFileSystem, _mockValidateConfig, Arg.Any<CancellationToken>())
            .Returns((false, RepositoryPath));
        _mockConsole.PromptSelectRepoActions(Arg.Any<CancellationToken>())
            .Returns((false, [RepoActions.Revision]));
        _mockConsole.PromptEditFieldVersion(Arg.Any<FieldsJson>(), Arg.Any<CancellationToken>())
            .Returns((false, 1));
        _mockConsole.PromptConfirm(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((false, true));

        SetupWizardMocks(GetValidRepoConfig());

        // Act
        await _handler.ExecuteAsync(args, CancellationToken.None);

        // Assert
        await _mockFileSystem.Received(1).WriteAllTextAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WithWizardModeScopeSelected_ConfiguresScope()
    {
        // Arrange
        var args = new[] { "--wizard" };
        var parsedArgs = new Dictionary<Arguments, string> { { Arguments.WizardRepo, string.Empty } };
        var drives = new[] { SingleTestDrive };
        var scopeItems = "Enterprise;Business;Application";

        _mockAdrServices.ParseArgs(args, Arg.Any<Arguments[]>()).Returns(parsedArgs);
        _mockValidateConfig.HasTemplateRepoFile().Returns(true);
        _mockFileSystem.GetDrives().Returns(drives);
        _mockConsole.PromptSelectFolderRepositoryAdr(SingleTestDrive, _mockFileSystem, _mockValidateConfig, Arg.Any<CancellationToken>())
            .Returns((false, RepositoryPath));
        _mockConsole.PromptSelectRepoActions(Arg.Any<CancellationToken>())
            .Returns((false, [RepoActions.Scope]));
        _mockConsole.PromptEditFieldLenScope(Arg.Any<FieldsJson>(), Arg.Any<CancellationToken>())
            .Returns((false, 2));
        _mockConsole.PromptEditFieldScopes(Arg.Any<FieldsJson>(), Arg.Any<CancellationToken>())
            .Returns((false, scopeItems));
        _mockConsole.PromptEditFieldFolderByScope(Arg.Any<FieldsJson>(), Arg.Any<CancellationToken>())
            .Returns((false, true));
        _mockConsole.PromptConfirm(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((false, true));

        SetupWizardMocks(GetValidRepoConfig());
        _mockFileSystem.DirectoryExists(Arg.Any<string>()).Returns(false);
        _mockFileSystem.CreateDirectory(Arg.Any<string>()).Returns(callInfo => callInfo.Arg<string>());

        // Act
        await _handler.ExecuteAsync(args, CancellationToken.None);

        // Assert
        await _mockFileSystem.Received(1).WriteAllTextAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
        _mockFileSystem.Received(3).CreateDirectory(Arg.Any<string>());
    }

    [Fact]
    public async Task ExecuteAsync_WithWizardModeUserCancelsConfirmation_RestartsWizard()
    {
        // Arrange
        var args = new[] { "--wizard" };
        var parsedArgs = new Dictionary<Arguments, string> { { Arguments.WizardRepo, string.Empty } };
        var drives = new[] { SingleTestDrive };
        var confirmCount = 0;

        _mockAdrServices.ParseArgs(args, Arg.Any<Arguments[]>()).Returns(parsedArgs);
        _mockValidateConfig.HasTemplateRepoFile().Returns(true);
        _mockFileSystem.GetDrives().Returns(drives);
        _mockConsole.PromptSelectFolderRepositoryAdr(SingleTestDrive, _mockFileSystem, _mockValidateConfig, Arg.Any<CancellationToken>())
            .Returns((false, RepositoryPath));
        _mockConsole.PromptSelectRepoActions(Arg.Any<CancellationToken>())
            .Returns((false, []));
        _mockConsole.PromptConfirm(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                confirmCount++;
                return confirmCount == 1 ? (false, false) : (false, true);
            });

        SetupWizardMocks(GetValidRepoConfig());

        // Act
        await _handler.ExecuteAsync(args, CancellationToken.None);

        // Assert - should be called twice (once for restart, once for completion)
        _mockConsole.Received(2).PromptConfirm(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WithWizardModeUserSelectsActionsAborted_ThrowsOperationCanceledException()
    {
        // Arrange
        var args = new[] { "--wizard" };
        var parsedArgs = new Dictionary<Arguments, string> { { Arguments.WizardRepo, string.Empty } };

        _mockAdrServices.ParseArgs(args, Arg.Any<Arguments[]>()).Returns(parsedArgs);
        _mockValidateConfig.HasTemplateRepoFile().Returns(true);
        _mockConsole.PromptSelectRepoActions(Arg.Any<CancellationToken>())
            .Returns((true, []));

        // Act & Assert
        await _handler.Invoking(h => h.ExecuteAsync(args, CancellationToken.None))
            .Should().ThrowAsync<OperationCanceledException>();
    }

    #endregion

    #region Cancellation Tests

    [Fact]
    public async Task ExecuteAsync_WhenCancelled_WithCancelledToken_ThrowsOperationCanceledException()
    {
        // Arrange
        var args = new[] { "--version", "2", "--path", RepositoryPath };
        var parsedArgs = new Dictionary<Arguments, string>
        {
            { Arguments.RepoVersion, "2" },
            { Arguments.TargetRepoAdr, RepositoryPath }
        };
        var cts = new CancellationTokenSource();
        cts.Cancel();

        _mockAdrServices.ParseArgs(args, Arg.Any<Arguments[]>()).Returns(parsedArgs);
        _mockValidateConfig.HasTemplateRepoFile().Returns(true);
        _mockValidateConfig.GetFileNameRepoConfig().Returns(".adrplus");
        _mockFileSystem.FileExists(Arg.Any<string>()).Returns(true);
        _mockFileSystem.ReadAllTextAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<string>(new OperationCanceledException()));

        // Act & Assert
        await _handler.Invoking(h => h.ExecuteAsync(args, cts.Token))
            .Should().ThrowAsync<OperationCanceledException>();
    }

    #endregion

    #region Null Arguments Tests

    [Fact]
    public async Task ExecuteAsync_WithNullArgs_ThrowsArgumentNullException()
    {
        // Act & Assert
        await _handler.Invoking(h => h.ExecuteAsync(null!, CancellationToken.None))
            .Should().ThrowAsync<ArgumentNullException>();
    }

    #endregion

    #region Helper Methods

    private void SetupBasicMocks(Dictionary<Arguments, string> parsedArgs, string jsonConfig, string configPath)
    {
        _mockAdrServices.ParseArgs(Arg.Any<string[]>(), Arg.Any<Arguments[]>()).Returns(parsedArgs);
        _mockValidateConfig.HasTemplateRepoFile().Returns(true);
        _mockValidateConfig.GetFileNameRepoConfig().Returns(".adrplus");
        _mockFileSystem.FileExists(configPath).Returns(true);
        _mockFileSystem.ReadAllTextAsync(configPath, Arg.Any<CancellationToken>()).Returns(jsonConfig);
        _mockValidateConfig.ValidateRepoStructure(jsonConfig).Returns((true, []));
        _mockFileSystem.GetFullNameDirectoryByFile(Arg.Any<string>())
            .Returns(callInfo =>
            {
                var filePath = callInfo.Arg<string>();
                return Path.GetDirectoryName(filePath) ?? string.Empty;
            });
        _mockFileSystem.GetFileRootRepositoryPath(Arg.Any<string>())
            .Returns(callInfo =>
            {
                var filePath = callInfo.Arg<string>();
                var directory = Path.GetDirectoryName(filePath);
                return string.IsNullOrEmpty(directory) ? null : Path.Combine(directory, ".adrplus");
            });
    }

    private void SetupWizardMocks(string jsonConfig)
    {
        var configPath = ConfigFilePath;
        _mockValidateConfig.GetFileNameRepoConfig().Returns(".adrplus");
        _mockFileSystem.FileExists(configPath).Returns(true);
        _mockFileSystem.ReadAllTextAsync(configPath, Arg.Any<CancellationToken>()).Returns(jsonConfig);
        _mockValidateConfig.ValidateRepoStructure(jsonConfig).Returns((true, []));
    }

    private static string GetValidRepoConfig()
    {
        return """{"Prefix": "ADR", "LenSeq": 4, "LenVersion": 0, "LenRevision": 0, "FolderAdr": "adr", "LenScope": 0}""";
    }

    #endregion
}

