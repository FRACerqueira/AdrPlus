// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the AdrPlus project under MIT license
// ***************************************************************************************

using AdrPlus.Commands;
using AdrPlus.Commands.UndoStatus;
using AdrPlus.Core;
using AdrPlus.Domain;
using AdrPlus.Infrastructure.FileSystem;
using AdrPlus.Infrastructure.UI;
using AdrPlus.Tests.Helpers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using static AdrPlus.Tests.Helpers.TestPathData;

namespace AdrPlus.Tests.Commands.UndoStatus;

/// <summary>
/// Unit tests for UndoStatusCommandHandler class.
/// Tests demonstrate undo command execution, wizard flows, and validation patterns using NSubstitute.
/// </summary>
public class UndoStatusCommandHandlerTests
{
    private readonly ILogger<UndoStatusCommandHandler> _mockLogger;
    private readonly IFileSystemService _mockFileSystem;
    private readonly IConsoleWriter _mockConsole;
    private readonly IValidateJsonConfig _mockValidateConfig;
    private readonly IAdrServices _mockAdrServices;
    private readonly AdrPlusConfig _config;
    private readonly UndoStatusCommandHandler _handler;

    public UndoStatusCommandHandlerTests()
    {
        _mockLogger = Substitute.For<ILogger<UndoStatusCommandHandler>>();
        _mockFileSystem = Substitute.For<IFileSystemService>();
        _mockConsole = Substitute.For<IConsoleWriter>();
        _mockValidateConfig = Substitute.For<IValidateJsonConfig>();
        _mockAdrServices = Substitute.For<IAdrServices>();

        _config = new AdrPlusConfig
        {
            FolderRepo = "docs/adr",
            Language = "en-US"
        };

        _handler = new UndoStatusCommandHandler(
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
        var handler = new UndoStatusCommandHandler(
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
        var args = new[] { "--file", "adr-0001.md" };
        var parsedArgs = new Dictionary<Arguments, string> { { Arguments.FileAdr, "adr-0001.md" } };

        _mockAdrServices.ParseArgs(args, Arg.Any<Arguments[]>()).Returns(parsedArgs);
        _mockValidateConfig.HasTemplateRepoFile().Returns(false);

        // Act & Assert
        await _handler.Invoking(h => h.ExecuteAsync(args, CancellationToken.None))
            .Should().ThrowAsync<FileNotFoundException>();
    }

    #endregion

    #region ExecuteAsync - Direct File Tests

    [Fact]
    public async Task ExecuteAsync_WithValidFile_UndoesAdrStatus()
    {
        // Arrange
        var args = new[] { "--file", ValidAdrFilePath };
        var parsedArgs = new Dictionary<Arguments, string> { { Arguments.FileAdr, ValidAdrFilePath } };
        var jsonConfig = """{"Prefix": "ADR", "LenSeq": 4, "LenVersion": 2, "StatusNew": "Proposed", "StatusAcc": "Accepted"}""";

        SetupBasicMocks(parsedArgs, jsonConfig);

        var adrInfo = CreateAdrFileNameComponentsEligibleForUndo(ValidAdrFilePath);
        _mockAdrServices.ParseFileName(ValidAdrFilePath, Arg.Any<AdrPlusRepoConfig>(), _mockFileSystem)
            .Returns(adrInfo);
        _mockAdrServices.StatusUpdateAdrAsync(Arg.Any<string>(), AdrStatus.Unknown, Arg.Any<DateTime>(), Arg.Any<AdrPlusRepoConfig>(), _mockFileSystem, Arg.Any<CancellationToken>())
            .Returns((true, string.Empty));

        // Act
        await _handler.ExecuteAsync(args, CancellationToken.None);

        // Assert
        await _mockAdrServices.Received(1).StatusUpdateAdrAsync(
            Arg.Any<string>(),
            AdrStatus.Unknown,
            Arg.Any<DateTime>(),
            Arg.Any<AdrPlusRepoConfig>(),
            _mockFileSystem,
            Arg.Any<CancellationToken>());
        _mockConsole.Received(1).WriteSuccess(Arg.Any<string>());
    }

    [Fact]
    public async Task ExecuteAsync_WithFileWithoutExtension_AddsMarkdownExtension()
    {
        // Arrange
        var args = new[] { "--file", AdrFileWithoutExtensionPath };
        var parsedArgs = new Dictionary<Arguments, string> { { Arguments.FileAdr, AdrFileWithoutExtensionPath } };
        var jsonConfig = """{"Prefix": "ADR", "LenSeq": 4, "LenVersion": 2, "StatusNew": "Proposed", "StatusAcc": "Accepted"}""";

        SetupBasicMocks(parsedArgs, jsonConfig);
        _mockFileSystem.FileExists(Arg.Is<string>(s => s.EndsWith(".md"))).Returns(true);

        var adrInfo = CreateAdrFileNameComponentsEligibleForUndo(AdrFileWithExtensionPath);
        _mockAdrServices.ParseFileName(Arg.Is<string>(s => s.EndsWith(".md")), Arg.Any<AdrPlusRepoConfig>(), _mockFileSystem)
            .Returns(adrInfo);
        _mockAdrServices.StatusUpdateAdrAsync(Arg.Any<string>(), AdrStatus.Unknown, Arg.Any<DateTime>(), Arg.Any<AdrPlusRepoConfig>(), _mockFileSystem, Arg.Any<CancellationToken>())
            .Returns((true, string.Empty));

        // Act
        await _handler.ExecuteAsync(args, CancellationToken.None);

        // Assert
        _mockFileSystem.Received().FileExists(Arg.Is<string>(s => s.EndsWith(".md")));
    }

    [Fact]
    public async Task ExecuteAsync_WithCustomDate_UsesProvidedDate()
    {
        // Arrange
        var customDate = "2026-01-15";
        var args = new[] { "--file", ValidAdrFilePath, "--refdate", customDate };
        var parsedArgs = new Dictionary<Arguments, string>
        {
            { Arguments.FileAdr, ValidAdrFilePath },
            { Arguments.DateRefAdr, customDate }
        };
        var jsonConfig = """{"Prefix": "ADR", "LenSeq": 4, "LenVersion": 2, "StatusNew": "Proposed", "StatusAcc": "Accepted"}""";

        SetupBasicMocks(parsedArgs, jsonConfig);

        var adrInfo = CreateAdrFileNameComponentsEligibleForUndo(ValidAdrFilePath);
        _mockAdrServices.ParseFileName(ValidAdrFilePath, Arg.Any<AdrPlusRepoConfig>(), _mockFileSystem)
            .Returns(adrInfo);
        _mockAdrServices.StatusUpdateAdrAsync(Arg.Any<string>(), AdrStatus.Unknown, Arg.Any<DateTime>(), Arg.Any<AdrPlusRepoConfig>(), _mockFileSystem, Arg.Any<CancellationToken>())
            .Returns((true, string.Empty));

        // Act
        await _handler.ExecuteAsync(args, CancellationToken.None);

        // Assert
        await _mockAdrServices.Received(1).StatusUpdateAdrAsync(
            Arg.Any<string>(),
            AdrStatus.Unknown,
            Arg.Is<DateTime>(d => d.Year == 2026 && d.Month == 1 && d.Day == 15),
            Arg.Any<AdrPlusRepoConfig>(),
            _mockFileSystem,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WithoutDateArgument_UsesCurrentUtcDate()
    {
        // Arrange
        var args = new[] { "--file", ValidAdrFilePath };
        var parsedArgs = new Dictionary<Arguments, string> { { Arguments.FileAdr, ValidAdrFilePath } };
        var jsonConfig = """{"Prefix": "ADR", "LenSeq": 4, "LenVersion": 2, "StatusNew": "Proposed", "StatusAcc": "Accepted"}""";

        SetupBasicMocks(parsedArgs, jsonConfig);

        var adrInfo = CreateAdrFileNameComponentsEligibleForUndo(ValidAdrFilePath);
        _mockAdrServices.ParseFileName(ValidAdrFilePath, Arg.Any<AdrPlusRepoConfig>(), _mockFileSystem)
            .Returns(adrInfo);
        _mockAdrServices.StatusUpdateAdrAsync(Arg.Any<string>(), AdrStatus.Unknown, Arg.Any<DateTime>(), Arg.Any<AdrPlusRepoConfig>(), _mockFileSystem, Arg.Any<CancellationToken>())
            .Returns((true, string.Empty));

        var before = DateTime.UtcNow;

        // Act
        await _handler.ExecuteAsync(args, CancellationToken.None);

        var after = DateTime.UtcNow;

        // Assert
        await _mockAdrServices.Received(1).StatusUpdateAdrAsync(
            Arg.Any<string>(),
            AdrStatus.Unknown,
            Arg.Is<DateTime>(d => d >= before && d <= after),
            Arg.Any<AdrPlusRepoConfig>(),
            _mockFileSystem,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WhenFileNotFound_ThrowsFileNotFoundException()
    {
        // Arrange
        var args = new[] { "--file", MissingAdrFilePath };
        var parsedArgs = new Dictionary<Arguments, string> { { Arguments.FileAdr, MissingAdrFilePath } };

        _mockAdrServices.ParseArgs(args, Arg.Any<Arguments[]>()).Returns(parsedArgs);
        _mockValidateConfig.HasTemplateRepoFile().Returns(true);
        _mockFileSystem.FileExists(Arg.Any<string>()).Returns(false);

        // Act & Assert
        await _handler.Invoking(h => h.ExecuteAsync(args, CancellationToken.None))
            .Should().ThrowAsync<FileNotFoundException>();
    }

    [Fact]
    public async Task ExecuteAsync_WhenFileNotInAdrFolder_ThrowsInvalidDataException()
    {
        // Arrange
        var args = new[] { "--file", FileOutsideAdrFolderPath };
        var parsedArgs = new Dictionary<Arguments, string> { { Arguments.FileAdr, FileOutsideAdrFolderPath } };

        _mockAdrServices.ParseArgs(args, Arg.Any<Arguments[]>()).Returns(parsedArgs);
        _mockValidateConfig.HasTemplateRepoFile().Returns(true);
        _mockFileSystem.FileExists(FileOutsideAdrFolderPath).Returns(true);

        // Act & Assert
        await _handler.Invoking(h => h.ExecuteAsync(args, CancellationToken.None))
            .Should().ThrowAsync<InvalidDataException>();
    }

    [Fact]
    public async Task ExecuteAsync_WhenConfigFileNotFound_ThrowsFileNotFoundException()
    {
        // Arrange
        var args = new[] { "--file", ValidAdrFilePath };
        var parsedArgs = new Dictionary<Arguments, string> { { Arguments.FileAdr, ValidAdrFilePath } };

        _mockAdrServices.ParseArgs(args, Arg.Any<Arguments[]>()).Returns(parsedArgs);
        _mockValidateConfig.HasTemplateRepoFile().Returns(true);
        _mockFileSystem.FileExists(ValidAdrFilePath).Returns(true);
        _mockValidateConfig.GetFileNameRepoConfig().Returns(".adrplus");
        _mockFileSystem.FileExists(Arg.Is<string>(s => s.EndsWith(".adrplus"))).Returns(false);

        // Act & Assert
        await _handler.Invoking(h => h.ExecuteAsync(args, CancellationToken.None))
            .Should().ThrowAsync<FileNotFoundException>();
    }

    [Fact]
    public async Task ExecuteAsync_WhenInvalidRepoConfig_ThrowsInvalidDataException()
    {
        // Arrange
        var args = new[] { "--file", ValidAdrFilePath };
        var parsedArgs = new Dictionary<Arguments, string> { { Arguments.FileAdr, ValidAdrFilePath } };
        var configPath = ConfigFilePath;
        var jsonConfig = """{"Invalid": "config"}""";
        var errors = new[] { "Missing Prefix field" };

        _mockAdrServices.ParseArgs(args, Arg.Any<Arguments[]>()).Returns(parsedArgs);
        _mockValidateConfig.HasTemplateRepoFile().Returns(true);
        _mockFileSystem.FileExists(ValidAdrFilePath).Returns(true);
        _mockValidateConfig.GetFileNameRepoConfig().Returns(".adrplus");
        _mockFileSystem.FileExists(configPath).Returns(true);
        _mockFileSystem.ReadAllTextAsync(configPath, Arg.Any<CancellationToken>()).Returns(jsonConfig);
        _mockValidateConfig.ValidateRepoStructure(jsonConfig).Returns((false, errors));

        // Act & Assert
        await _handler.Invoking(h => h.ExecuteAsync(args, CancellationToken.None))
            .Should().ThrowAsync<InvalidDataException>();

        _mockConsole.Received(1).WriteError("Missing Prefix field");
    }

    [Fact]
    public async Task ExecuteAsync_WhenInvalidFileName_ThrowsInvalidDataException()
    {
        // Arrange
        var args = new[] { "--file", InvalidFileNameAdrPath };
        var parsedArgs = new Dictionary<Arguments, string> { { Arguments.FileAdr, InvalidFileNameAdrPath } };
        var configPath = ConfigFilePath;
        var jsonConfig = """{"Prefix": "ADR", "LenSeq": 4, "LenVersion": 2}""";

        _mockAdrServices.ParseArgs(args, Arg.Any<Arguments[]>()).Returns(parsedArgs);
        _mockValidateConfig.HasTemplateRepoFile().Returns(true);
        _mockFileSystem.FileExists(InvalidFileNameAdrPath).Returns(true);
        _mockValidateConfig.GetFileNameRepoConfig().Returns(".adrplus");
        _mockFileSystem.FileExists(configPath).Returns(true);
        _mockFileSystem.ReadAllTextAsync(configPath, Arg.Any<CancellationToken>()).Returns(jsonConfig);
        _mockValidateConfig.ValidateRepoStructure(jsonConfig).Returns((true, []));

        var adrInfo = new AdrFileNameComponents
        {
            IsValid = false,
            ErrorMessage = "Invalid file name format"
        };
        _mockAdrServices.ParseFileName(InvalidFileNameAdrPath, Arg.Any<AdrPlusRepoConfig>(), _mockFileSystem)
            .Returns(adrInfo);

        // Act & Assert
        await _handler.Invoking(h => h.ExecuteAsync(args, CancellationToken.None))
            .Should().ThrowAsync<InvalidDataException>()
            .WithMessage("Invalid file name format");
    }

    [Fact]
    public async Task ExecuteAsync_WhenInvalidHeader_ThrowsInvalidDataException()
    {
        // Arrange
        var args = new[] { "--file", ValidAdrFilePath };
        var parsedArgs = new Dictionary<Arguments, string> { { Arguments.FileAdr, ValidAdrFilePath } };
        var configPath = ConfigFilePath;
        var jsonConfig = """{"Prefix": "ADR", "LenSeq": 4, "LenVersion": 2}""";

        _mockAdrServices.ParseArgs(args, Arg.Any<Arguments[]>()).Returns(parsedArgs);
        _mockValidateConfig.HasTemplateRepoFile().Returns(true);
        _mockFileSystem.FileExists(ValidAdrFilePath).Returns(true);
        _mockValidateConfig.GetFileNameRepoConfig().Returns(".adrplus");
        _mockFileSystem.FileExists(configPath).Returns(true);
        _mockFileSystem.ReadAllTextAsync(configPath, Arg.Any<CancellationToken>()).Returns(jsonConfig);
        _mockValidateConfig.ValidateRepoStructure(jsonConfig).Returns((true, []));

        var adrInfo = new AdrFileNameComponents
        {
            IsValid = true,
            Header = new AdrHeader
            {
                IsValid = false,
                ErrorMessage = "Invalid header format"
            }
        };
        _mockAdrServices.ParseFileName(ValidAdrFilePath, Arg.Any<AdrPlusRepoConfig>(), _mockFileSystem)
            .Returns(adrInfo);

        // Act & Assert
        await _handler.Invoking(h => h.ExecuteAsync(args, CancellationToken.None))
            .Should().ThrowAsync<InvalidDataException>()
            .WithMessage("Invalid header format");
    }

    #endregion

    #region ExecuteAsync - SelectionCondition Tests

    [Fact]
    public async Task ExecuteAsync_WhenAdrHasNoStatusUpdate_ThrowsInvalidDataException()
    {
        // Arrange - StatusUpdate is Unknown, so it has never been updated -> not eligible for undo
        var args = new[] { "--file", ValidAdrFilePath };
        var parsedArgs = new Dictionary<Arguments, string> { { Arguments.FileAdr, ValidAdrFilePath } };
        var jsonConfig = """{"Prefix": "ADR", "LenSeq": 4, "LenVersion": 2, "StatusNew": "Proposed", "StatusAcc": "Accepted"}""";

        SetupBasicMocks(parsedArgs, jsonConfig);

        var adrInfo = new AdrFileNameComponents
        {
            FileName = ValidAdrFilePath,
            IsValid = true,
            Header = new AdrHeader
            {
                IsValid = true,
                StatusUpdate = AdrStatus.Unknown,   // no status update -> not eligible
                StatusChange = AdrStatus.Unknown,
                StatusCreate = AdrStatus.Proposed
            }
        };
        _mockAdrServices.ParseFileName(ValidAdrFilePath, Arg.Any<AdrPlusRepoConfig>(), _mockFileSystem)
            .Returns(adrInfo);

        // Act & Assert
        await _handler.Invoking(h => h.ExecuteAsync(args, CancellationToken.None))
            .Should().ThrowAsync<InvalidDataException>();
    }

    [Fact]
    public async Task ExecuteAsync_WhenAdrHasStatusChange_ThrowsInvalidDataException()
    {
        // Arrange - StatusChange is set (superseded), so it is not eligible for undo
        var args = new[] { "--file", ValidAdrFilePath };
        var parsedArgs = new Dictionary<Arguments, string> { { Arguments.FileAdr, ValidAdrFilePath } };
        var jsonConfig = """{"Prefix": "ADR", "LenSeq": 4, "LenVersion": 2, "StatusNew": "Proposed", "StatusAcc": "Accepted"}""";

        SetupBasicMocks(parsedArgs, jsonConfig);

        var adrInfo = new AdrFileNameComponents
        {
            FileName = ValidAdrFilePath,
            IsValid = true,
            Header = new AdrHeader
            {
                IsValid = true,
                StatusUpdate = AdrStatus.Accepted,    // has been updated
                StatusChange = AdrStatus.Superseded,  // but also changed -> not eligible
                StatusCreate = AdrStatus.Proposed
            }
        };
        _mockAdrServices.ParseFileName(ValidAdrFilePath, Arg.Any<AdrPlusRepoConfig>(), _mockFileSystem)
            .Returns(adrInfo);

        // Act & Assert
        await _handler.Invoking(h => h.ExecuteAsync(args, CancellationToken.None))
            .Should().ThrowAsync<InvalidDataException>();
    }

    #endregion

    #region ExecuteAsync - Status Update Failure Tests

    [Fact]
    public async Task ExecuteAsync_WhenStatusUpdateFails_ThrowsInvalidDataException()
    {
        // Arrange
        var args = new[] { "--file", ValidAdrFilePath };
        var parsedArgs = new Dictionary<Arguments, string> { { Arguments.FileAdr, ValidAdrFilePath } };
        var jsonConfig = """{"Prefix": "ADR", "LenSeq": 4, "LenVersion": 2, "StatusNew": "Proposed", "StatusAcc": "Accepted"}""";

        SetupBasicMocks(parsedArgs, jsonConfig);

        var adrInfo = CreateAdrFileNameComponentsEligibleForUndo(ValidAdrFilePath);
        _mockAdrServices.ParseFileName(ValidAdrFilePath, Arg.Any<AdrPlusRepoConfig>(), _mockFileSystem)
            .Returns(adrInfo);
        _mockAdrServices.StatusUpdateAdrAsync(Arg.Any<string>(), AdrStatus.Unknown, Arg.Any<DateTime>(), Arg.Any<AdrPlusRepoConfig>(), _mockFileSystem, Arg.Any<CancellationToken>())
            .Returns((false, "Update failed"));

        // Act & Assert
        await _handler.Invoking(h => h.ExecuteAsync(args, CancellationToken.None))
            .Should().ThrowAsync<InvalidDataException>()
            .WithMessage("Update failed");
    }

    [Fact]
    public async Task ExecuteAsync_WithInvalidDateFormat_ThrowsFormatException()
    {
        // Arrange
        var args = new[] { "--file", ValidAdrFilePath, "--refdate", "invalid-date" };
        var parsedArgs = new Dictionary<Arguments, string>
        {
            { Arguments.FileAdr, ValidAdrFilePath },
            { Arguments.DateRefAdr, "invalid-date" }
        };
        var jsonConfig = """{"Prefix": "ADR", "LenSeq": 4, "LenVersion": 2, "StatusNew": "Proposed", "StatusAcc": "Accepted"}""";

        SetupBasicMocks(parsedArgs, jsonConfig);

        var adrInfo = CreateAdrFileNameComponentsEligibleForUndo(ValidAdrFilePath);
        _mockAdrServices.ParseFileName(ValidAdrFilePath, Arg.Any<AdrPlusRepoConfig>(), _mockFileSystem)
            .Returns(adrInfo);

        // Act & Assert
        await _handler.Invoking(h => h.ExecuteAsync(args, CancellationToken.None))
            .Should().ThrowAsync<FormatException>();
    }

    #endregion

    #region ExecuteAsync - Wizard Mode Cancellation Tests

    [Fact]
    public async Task ExecuteAsync_WithWizardModeDriveSelectionAborted_ThrowsOperationCanceledException()
    {
        // Arrange
        var args = new[] { "--wizard" };
        var parsedArgs = new Dictionary<Arguments, string> { { Arguments.WizardUndoStatus, string.Empty } };
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
        var parsedArgs = new Dictionary<Arguments, string> { { Arguments.WizardUndoStatus, string.Empty } };
        var drives = new[] { SingleTestDrive };

        _mockAdrServices.ParseArgs(args, Arg.Any<Arguments[]>()).Returns(parsedArgs);
        _mockValidateConfig.HasTemplateRepoFile().Returns(true);
        _mockFileSystem.GetDrives().Returns(drives);
        _mockConsole.PromptSelectFolderRepositoryAdr(true, SingleTestDrive, _mockFileSystem, _mockValidateConfig, _config, Arg.Any<CancellationToken>())
            .Returns((true, string.Empty));

        // Act & Assert
        await _handler.Invoking(h => h.ExecuteAsync(args, CancellationToken.None))
            .Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task ExecuteAsync_WithWizardModeNoEligibleFiles_ThrowsFileNotFoundException()
    {
        // Arrange
        var args = new[] { "--wizard" };
        var parsedArgs = new Dictionary<Arguments, string> { { Arguments.WizardUndoStatus, string.Empty } };
        var drives = new[] { SingleTestDrive };
        var jsonConfig = """{"Prefix": "ADR", "LenSeq": 4, "LenVersion": 2, "StatusNew": "Proposed"}""";

        _mockAdrServices.ParseArgs(args, Arg.Any<Arguments[]>()).Returns(parsedArgs);
        _mockValidateConfig.HasTemplateRepoFile().Returns(true);
        _mockFileSystem.GetDrives().Returns(drives);
        _mockConsole.PromptSelectFolderRepositoryAdr(true, SingleTestDrive, _mockFileSystem, _mockValidateConfig, _config, Arg.Any<CancellationToken>())
            .Returns((false, RepositoryPath));
        _mockValidateConfig.GetFileNameRepoConfig().Returns(".adrplus");
        _mockFileSystem.ReadAllTextAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(jsonConfig);
        _mockValidateConfig.ValidateRepoStructure(jsonConfig).Returns((true, []));
        _mockAdrServices.ReadLatestAdrFiles(_mockFileSystem, Arg.Any<string>(), Arg.Any<AdrPlusRepoConfig>())
            .Returns([]);

        // Act & Assert
        await _handler.Invoking(h => h.ExecuteAsync(args, CancellationToken.None))
            .Should().ThrowAsync<FileNotFoundException>();
    }

    [Fact]
    public async Task ExecuteAsync_WithWizardModeAdrSelectionAborted_ThrowsOperationCanceledException()
    {
        // Arrange
        var args = new[] { "--wizard" };
        var parsedArgs = new Dictionary<Arguments, string> { { Arguments.WizardUndoStatus, string.Empty } };
        var drives = new[] { SingleTestDrive };
        var jsonConfig = """{"Prefix": "ADR", "LenSeq": 4, "LenVersion": 2, "StatusNew": "Proposed"}""";
        var eligibleAdr = CreateAdrFileNameComponentsEligibleForUndo(ValidAdrFilePath);

        _mockAdrServices.ParseArgs(args, Arg.Any<Arguments[]>()).Returns(parsedArgs);
        _mockValidateConfig.HasTemplateRepoFile().Returns(true);
        _mockFileSystem.GetDrives().Returns(drives);
        _mockConsole.PromptSelectFolderRepositoryAdr(true, SingleTestDrive, _mockFileSystem, _mockValidateConfig, _config, Arg.Any<CancellationToken>())
            .Returns((false, RepositoryPath));
        _mockValidateConfig.GetFileNameRepoConfig().Returns(".adrplus");
        _mockFileSystem.ReadAllTextAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(jsonConfig);
        _mockValidateConfig.ValidateRepoStructure(jsonConfig).Returns((true, []));
        _mockAdrServices.ReadLatestAdrFiles(_mockFileSystem, Arg.Any<string>(), Arg.Any<AdrPlusRepoConfig>())
            .Returns([eligibleAdr]);
        _mockConsole.PromptSelecLatesAdrs(Arg.Any<AdrFileNameComponents[]>(), Arg.Any<AdrPlusRepoConfig>(), Arg.Any<Func<AdrFileNameComponents, (bool, string?)>>(), Arg.Any<CancellationToken>())
            .Returns((true, (AdrFileNameComponents?)null));

        // Act & Assert
        await _handler.Invoking(h => h.ExecuteAsync(args, CancellationToken.None))
            .Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task ExecuteAsync_WithWizardModeDateSelectionAborted_ThrowsOperationCanceledException()
    {
        // Arrange
        var args = new[] { "--wizard" };
        var parsedArgs = new Dictionary<Arguments, string> { { Arguments.WizardUndoStatus, string.Empty } };
        var drives = new[] { SingleTestDrive };
        var jsonConfig = """{"Prefix": "ADR", "LenSeq": 4, "LenVersion": 2, "StatusNew": "Proposed"}""";
        var eligibleAdr = CreateAdrFileNameComponentsEligibleForUndo(ValidAdrFilePath);

        _mockAdrServices.ParseArgs(args, Arg.Any<Arguments[]>()).Returns(parsedArgs);
        _mockValidateConfig.HasTemplateRepoFile().Returns(true);
        _mockFileSystem.GetDrives().Returns(drives);
        _mockConsole.PromptSelectFolderRepositoryAdr(true, SingleTestDrive, _mockFileSystem, _mockValidateConfig, _config, Arg.Any<CancellationToken>())
            .Returns((false, RepositoryPath));
        _mockValidateConfig.GetFileNameRepoConfig().Returns(".adrplus");
        _mockFileSystem.ReadAllTextAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(jsonConfig);
        _mockValidateConfig.ValidateRepoStructure(jsonConfig).Returns((true, []));
        _mockAdrServices.ReadLatestAdrFiles(_mockFileSystem, Arg.Any<string>(), Arg.Any<AdrPlusRepoConfig>())
            .Returns([eligibleAdr]);
        _mockConsole.PromptSelecLatesAdrs(Arg.Any<AdrFileNameComponents[]>(), Arg.Any<AdrPlusRepoConfig>(), Arg.Any<Func<AdrFileNameComponents, (bool, string?)>>(), Arg.Any<CancellationToken>())
            .Returns((false, eligibleAdr));
        _mockConsole.PrompCalendar(Arg.Any<string>(), Arg.Any<DateTime>(), _config, Arg.Any<CancellationToken>())
            .Returns((true, DateTime.UtcNow));

        // Act & Assert
        await _handler.Invoking(h => h.ExecuteAsync(args, CancellationToken.None))
            .Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task ExecuteAsync_WithWizardModeConfirmationAborted_ThrowsOperationCanceledException()
    {
        // Arrange
        var args = new[] { "--wizard" };
        var parsedArgs = new Dictionary<Arguments, string> { { Arguments.WizardUndoStatus, string.Empty } };
        var drives = new[] { SingleTestDrive };
        var jsonConfig = """{"Prefix": "ADR", "LenSeq": 4, "LenVersion": 2, "StatusNew": "Proposed"}""";
        var eligibleAdr = CreateAdrFileNameComponentsEligibleForUndo(ValidAdrFilePath);

        _mockAdrServices.ParseArgs(args, Arg.Any<Arguments[]>()).Returns(parsedArgs);
        _mockValidateConfig.HasTemplateRepoFile().Returns(true);
        _mockFileSystem.GetDrives().Returns(drives);
        _mockConsole.PromptSelectFolderRepositoryAdr(true, SingleTestDrive, _mockFileSystem, _mockValidateConfig, _config, Arg.Any<CancellationToken>())
            .Returns((false, RepositoryPath));
        _mockValidateConfig.GetFileNameRepoConfig().Returns(".adrplus");
        _mockFileSystem.ReadAllTextAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(jsonConfig);
        _mockValidateConfig.ValidateRepoStructure(jsonConfig).Returns((true, []));
        _mockAdrServices.ReadLatestAdrFiles(_mockFileSystem, Arg.Any<string>(), Arg.Any<AdrPlusRepoConfig>())
            .Returns([eligibleAdr]);
        _mockConsole.PromptSelecLatesAdrs(Arg.Any<AdrFileNameComponents[]>(), Arg.Any<AdrPlusRepoConfig>(), Arg.Any<Func<AdrFileNameComponents, (bool, string?)>>(), Arg.Any<CancellationToken>())
            .Returns((false, eligibleAdr));
        _mockConsole.PrompCalendar(Arg.Any<string>(), Arg.Any<DateTime>(), _config, Arg.Any<CancellationToken>())
            .Returns((false, DateTime.UtcNow));
        _mockConsole.PromptConfirm(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((true, false));

        // Act & Assert
        await _handler.Invoking(h => h.ExecuteAsync(args, CancellationToken.None))
            .Should().ThrowAsync<OperationCanceledException>();
    }

    #endregion

    #region Cancellation Tests

    [Fact]
    public async Task ExecuteAsync_WhenCancelled_ThrowsOperationCanceledException()
    {
        // Arrange
        var args = new[] { "--file", ValidAdrFilePath };
        var parsedArgs = new Dictionary<Arguments, string> { { Arguments.FileAdr, ValidAdrFilePath } };
        var cts = new CancellationTokenSource();
        cts.Cancel();

        _mockAdrServices.ParseArgs(args, Arg.Any<Arguments[]>()).Returns(parsedArgs);
        _mockValidateConfig.HasTemplateRepoFile().Returns(true);
        _mockFileSystem.FileExists(Arg.Any<string>())
            .Returns<bool>(callInfo => throw new OperationCanceledException());

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
        var args = new[] { "--file", ValidAdrFilePath };
        var parsedArgs = new Dictionary<Arguments, string> { { Arguments.FileAdr, ValidAdrFilePath } };
        var exception = new InvalidOperationException("Test exception");

        _mockAdrServices.ParseArgs(args, Arg.Any<Arguments[]>()).Returns(parsedArgs);
        _mockValidateConfig.When(x => x.HasTemplateRepoFile()).Do(x => throw exception);

        // Act & Assert
        await _handler.Invoking(h => h.ExecuteAsync(args, CancellationToken.None))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Test exception");
    }

    #endregion

    #region Helper Methods

    private void SetupBasicMocks(Dictionary<Arguments, string> parsedArgs, string jsonConfig)
    {
        _mockAdrServices.ParseArgs(Arg.Any<string[]>(), Arg.Any<Arguments[]>()).Returns(parsedArgs);
        _mockValidateConfig.HasTemplateRepoFile().Returns(true);
        _mockFileSystem.FileExists(Arg.Is<string>(s => s.EndsWith(".md"))).Returns(true);
        _mockValidateConfig.GetFileNameRepoConfig().Returns(".adrplus");
        _mockFileSystem.FileExists(Arg.Is<string>(s => s.EndsWith(".adrplus"))).Returns(true);
        _mockFileSystem.ReadAllTextAsync(Arg.Is<string>(s => s.EndsWith(".adrplus")), Arg.Any<CancellationToken>()).Returns(jsonConfig);
        _mockValidateConfig.ValidateRepoStructure(jsonConfig).Returns((true, []));
        _mockFileSystem.GetFullNameFile(Arg.Any<string>()).Returns(callInfo => callInfo.Arg<string>());
    }

    /// <summary>
    /// Creates an ADR with StatusUpdate set (non-Unknown) and StatusChange Unknown,
    /// satisfying the SelectionCondition for undo eligibility.
    /// </summary>
    private static AdrFileNameComponents CreateAdrFileNameComponentsEligibleForUndo(string fileName)
    {
        return new AdrFileNameComponents
        {
            FileName = fileName,
            IsValid = true,
            Header = new AdrHeader
            {
                IsValid = true,
                StatusUpdate = AdrStatus.Accepted, // has been status-updated
                StatusChange = AdrStatus.Unknown,  // not changed (e.g. not superseded)
                StatusCreate = AdrStatus.Proposed
            }
        };
    }

    #endregion
}
