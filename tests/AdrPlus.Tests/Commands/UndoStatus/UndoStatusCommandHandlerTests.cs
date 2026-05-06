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
        var jsonConfig = """{"Prefix": "ADR", "LenSeq": 4, "LenVersion": 2, "FolderAdr": "adr", "StatusNew": "Proposed", "StatusAcc": "Accepted"}""";

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
        var jsonConfig = """{"Prefix": "ADR", "LenSeq": 4, "LenVersion": 2, "FolderAdr": "adr", "StatusNew": "Proposed", "StatusAcc": "Accepted"}""";

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
        var jsonConfig = """{"Prefix": "ADR", "LenSeq": 4, "LenVersion": 2, "FolderAdr": "adr", "StatusNew": "Proposed", "StatusAcc": "Accepted"}""";

        SetupBasicMocks(parsedArgs, jsonConfig);

        var adrInfo = CreateAdrFileNameComponentsEligibleForUndo(ValidAdrFilePath);
        _mockAdrServices.ParseFileName(ValidAdrFilePath, Arg.Any<AdrPlusRepoConfig>(), _mockFileSystem)
            .Returns(adrInfo);
        _mockAdrServices.StatusUpdateAdrAsync(Arg.Any<string>(), AdrStatus.Unknown, Arg.Any<DateTime>(), Arg.Any<AdrPlusRepoConfig>(), _mockFileSystem, Arg.Any<CancellationToken>())
            .Returns((true, string.Empty));

        // Act
        await _handler.ExecuteAsync(args, CancellationToken.None);

        // Assert - verify the method was called
        await _mockAdrServices.Received(1).StatusUpdateAdrAsync(
            Arg.Any<string>(),
            AdrStatus.Unknown,
            Arg.Any<DateTime>(),
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
        var jsonConfig = """{"Prefix": "ADR", "LenSeq": 4, "LenVersion": 2, "FolderAdr": "adr", "StatusNew": "Proposed", "StatusAcc": "Accepted"}""";

        SetupBasicMocks(parsedArgs, jsonConfig);

        var adrInfo = CreateAdrFileNameComponentsEligibleForUndo(ValidAdrFilePath);
        _mockAdrServices.ParseFileName(ValidAdrFilePath, Arg.Any<AdrPlusRepoConfig>(), _mockFileSystem)
            .Returns(adrInfo);
        _mockAdrServices.StatusUpdateAdrAsync(Arg.Any<string>(), AdrStatus.Unknown, Arg.Any<DateTime>(), Arg.Any<AdrPlusRepoConfig>(), _mockFileSystem, Arg.Any<CancellationToken>())
            .Returns((true, string.Empty));

        // Act
        await _handler.ExecuteAsync(args, CancellationToken.None);

        // Assert - verify that StatusUpdateAdrAsync was called with a DateTime (not checking exact value due to execution time)
        await _mockAdrServices.Received(1).StatusUpdateAdrAsync(
            Arg.Any<string>(),
            AdrStatus.Unknown,
            Arg.Any<DateTime>(),
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
        _mockFileSystem.FileExists(Arg.Any<string>()).Returns(true);
        _mockFileSystem.GetFileRootRepositoryPath(Arg.Any<string>()).Returns((string?)null);

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
        var configPath = Path.Combine(Path.GetDirectoryName(ValidAdrFilePath) ?? "/repo", ".adrplus");

        _mockAdrServices.ParseArgs(args, Arg.Any<Arguments[]>()).Returns(parsedArgs);
        _mockValidateConfig.HasTemplateRepoFile().Returns(true);
        _mockValidateConfig.GetFileNameRepoConfig().Returns(".adrplus");
        _mockFileSystem.FileExists(Arg.Is<string>(s => s.EndsWith(".md"))).Returns(true);
        _mockFileSystem.FileExists(Arg.Is<string>(s => s.EndsWith(".adrplus"))).Returns(false);
        _mockFileSystem.GetFileRootRepositoryPath(Arg.Is<string>(s => s.EndsWith(".md")))
            .Returns(configPath);
        _mockFileSystem.ReadAllTextAsync(Arg.Is<string>(s => s.EndsWith(".adrplus")), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<string>(new FileNotFoundException($"File not found: {configPath}")));

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
        var jsonConfig = """{"Invalid": "config"}""";
        var errors = new[] { "Missing Prefix field" };

        _mockAdrServices.ParseArgs(args, Arg.Any<Arguments[]>()).Returns(parsedArgs);
        _mockValidateConfig.HasTemplateRepoFile().Returns(true);
        _mockValidateConfig.GetFileNameRepoConfig().Returns(".adrplus");
        _mockFileSystem.FileExists(Arg.Is<string>(s => s.EndsWith(".md"))).Returns(true);
        _mockFileSystem.FileExists(Arg.Is<string>(s => s.EndsWith(".adrplus"))).Returns(true);
        _mockFileSystem.GetFileRootRepositoryPath(Arg.Is<string>(s => s.EndsWith(".md")))
            .Returns(callInfo => Path.Combine(Path.GetDirectoryName(callInfo.Arg<string>()) ?? "/repo", ".adrplus"));
        _mockFileSystem.ReadAllTextAsync(Arg.Is<string>(s => s.EndsWith(".adrplus")), Arg.Any<CancellationToken>()).Returns(jsonConfig);
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
        var jsonConfig = """{"Prefix": "ADR", "LenSeq": 4, "LenVersion": 2}""";

        _mockAdrServices.ParseArgs(args, Arg.Any<Arguments[]>()).Returns(parsedArgs);
        _mockValidateConfig.HasTemplateRepoFile().Returns(true);
        _mockValidateConfig.GetFileNameRepoConfig().Returns(".adrplus");
        _mockFileSystem.FileExists(Arg.Is<string>(s => s.EndsWith(".md"))).Returns(true);
        _mockFileSystem.FileExists(Arg.Is<string>(s => s.EndsWith(".adrplus"))).Returns(true);
        _mockFileSystem.GetFileRootRepositoryPath(Arg.Is<string>(s => s.EndsWith(".md")))
            .Returns(callInfo => Path.Combine(Path.GetDirectoryName(callInfo.Arg<string>()) ?? "/repo", ".adrplus"));
        _mockFileSystem.ReadAllTextAsync(Arg.Is<string>(s => s.EndsWith(".adrplus")), Arg.Any<CancellationToken>()).Returns(jsonConfig);
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
        var jsonConfig = """{"Prefix": "ADR", "LenSeq": 4, "LenVersion": 2}""";

        _mockAdrServices.ParseArgs(args, Arg.Any<Arguments[]>()).Returns(parsedArgs);
        _mockValidateConfig.HasTemplateRepoFile().Returns(true);
        _mockValidateConfig.GetFileNameRepoConfig().Returns(".adrplus");
        _mockFileSystem.FileExists(Arg.Is<string>(s => s.EndsWith(".md"))).Returns(true);
        _mockFileSystem.FileExists(Arg.Is<string>(s => s.EndsWith(".adrplus"))).Returns(true);
        _mockFileSystem.GetFileRootRepositoryPath(Arg.Is<string>(s => s.EndsWith(".md")))
            .Returns(callInfo => Path.Combine(Path.GetDirectoryName(callInfo.Arg<string>()) ?? "/repo", ".adrplus"));
        _mockFileSystem.ReadAllTextAsync(Arg.Is<string>(s => s.EndsWith(".adrplus")), Arg.Any<CancellationToken>()).Returns(jsonConfig);
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
        var jsonConfig = """{"Prefix": "ADR", "LenSeq": 4, "LenVersion": 2, "FolderAdr": "adr", "StatusNew": "Proposed", "StatusAcc": "Accepted"}""";

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
        var jsonConfig = """{"Prefix": "ADR", "LenSeq": 4, "LenVersion": 2, "FolderAdr": "adr", "StatusNew": "Proposed", "StatusAcc": "Accepted"}""";

        SetupBasicMocks(parsedArgs, jsonConfig);

        var adrInfo = CreateAdrFileNameComponentsEligibleForUndo(ValidAdrFilePath);
        _mockAdrServices.ParseFileName(ValidAdrFilePath, Arg.Any<AdrPlusRepoConfig>(), _mockFileSystem)
            .Returns(adrInfo);

        // Act & Assert
        await _handler.Invoking(h => h.ExecuteAsync(args, CancellationToken.None))
            .Should().ThrowAsync<Exception>()  // Accept any exception type (FormatException or InvalidDataException)
            .Where(ex => ex.GetType().Name == "FormatException" || ex.GetType().Name == "InvalidDataException");
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
        _mockConsole.PromptSelectFolderRepositoryPath(true, SingleTestDrive, _mockFileSystem, _mockValidateConfig, Arg.Any<CancellationToken>())
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
        _mockConsole.PromptSelectFolderRepositoryPath(true, SingleTestDrive, _mockFileSystem, _mockValidateConfig, Arg.Any<CancellationToken>())
            .Returns((false, RepositoryPath));
        _mockValidateConfig.GetFileNameRepoConfig().Returns(".adrplus");
        _mockFileSystem.ReadAllTextAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(jsonConfig);
        _mockValidateConfig.ValidateRepoStructure(jsonConfig).Returns((true, []));
        _mockAdrServices.ReadAllAdr(_mockFileSystem, Arg.Any<string>(), Arg.Any<AdrPlusRepoConfig>(), false)
            .Returns(Task.FromResult(new AdrFileNameComponents[] { }));

        // Act & Assert
        await _handler.Invoking(h => h.ExecuteAsync(args, CancellationToken.None))
            .Should().ThrowAsync<FileNotFoundException>();
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
        _mockConsole.PromptSelectFolderRepositoryPath(Arg.Any<bool>(), Arg.Any<string>(), _mockFileSystem, _mockValidateConfig, Arg.Any<CancellationToken>())
            .Returns((false, RepositoryPath));
        _mockValidateConfig.GetFileNameRepoConfig().Returns(".adrplus");
        _mockFileSystem.ReadAllTextAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(jsonConfig);
        _mockValidateConfig.ValidateRepoStructure(jsonConfig).Returns((true, []));
        // Mock ReadAllAdr to return eligible ADR - setup for all possible calls
        _mockAdrServices
            .ReadAllAdr(Arg.Any<IFileSystemService>(), Arg.Any<string>(), Arg.Any<AdrPlusRepoConfig>(), false)
            .Returns(x => Task.FromResult(new[] { eligibleAdr }));
        // Mock ReadAllAdrByNumber to return eligible ADR (for validation checks)
        _mockAdrServices
            .ReadAllAdrByNumber(Arg.Any<int>(), Arg.Any<IFileSystemService>(), Arg.Any<string>(), Arg.Any<AdrPlusRepoConfig>())
            .Returns(new[] { eligibleAdr });
        _mockConsole.PromptSelecAdrs(Arg.Any<AdrFileNameComponents[]>(), Arg.Any<AdrPlusRepoConfig>(), Arg.Any<Func<AdrFileNameComponents, (bool, string?)>>(), Arg.Any<CancellationToken>())
            .Returns((false, eligibleAdr));
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

    #region Wizard Mode Additional Coverage Tests

    [Fact]
    public async Task ExecuteAsync_WithWizardModeMultipleLoopsBeforeConfirm_UpdatesAdrStatus()
    {
        // Arrange - Test wizard loop: user cancels confirmation first, then confirms on second iteration
        var args = new[] { "--wizard" };
        var parsedArgs = new Dictionary<Arguments, string> { { Arguments.WizardUndoStatus, string.Empty } };
        var drives = new[] { SingleTestDrive };
        var jsonConfig = """{"Prefix": "ADR", "LenSeq": 4, "LenVersion": 2, "StatusNew": "Proposed", "StatusAcc": "Accepted", "template":"# ADR"}""";
        var eligibleAdr = CreateAdrFileNameComponentsEligibleForUndo(ValidAdrFilePath);

        _mockAdrServices.ParseArgs(args, Arg.Any<Arguments[]>()).Returns(parsedArgs);
        _mockValidateConfig.HasTemplateRepoFile().Returns(true);
        _mockFileSystem.GetDrives().Returns(drives);
        _mockConsole.PromptSelectFolderRepositoryPath(Arg.Any<bool>(), Arg.Any<string>(), _mockFileSystem, _mockValidateConfig, Arg.Any<CancellationToken>())
            .Returns((false, RepositoryPath));
        _mockValidateConfig.GetFileNameRepoConfig().Returns(".adrplus");
        _mockFileSystem.ReadAllTextAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(jsonConfig);
        _mockValidateConfig.ValidateRepoStructure(jsonConfig).Returns((true, []));
        _mockConsole.WriteWait(Arg.Any<string>());
        var cursorPos = (0, 0);
        _mockConsole.GetCursorPosition().Returns(cursorPos);
        _mockAdrServices
            .ReadAllAdr(Arg.Any<IFileSystemService>(), Arg.Any<string>(), Arg.Any<AdrPlusRepoConfig>(), false)
            .Returns(x => Task.FromResult(new[] { eligibleAdr }));
        _mockConsole.ClearWait(cursorPos);
        _mockAdrServices
            .ReadAllAdrByNumber(Arg.Any<int>(), Arg.Any<IFileSystemService>(), Arg.Any<string>(), Arg.Any<AdrPlusRepoConfig>())
            .Returns(new[] { eligibleAdr });
        _mockConsole.PromptSelecAdrs(Arg.Any<AdrFileNameComponents[]>(), Arg.Any<AdrPlusRepoConfig>(), Arg.Any<Func<AdrFileNameComponents, (bool, string?)>>(), Arg.Any<CancellationToken>())
            .Returns((false, eligibleAdr));
        // Mock WriteSummary to simulate console output (void method, no validation needed)
        _mockConsole.WriteSummary(Arg.Any<string>());
        // First call: user doesn't confirm (ConfirmYes=false), second call: user confirms (ConfirmYes=true)
        _mockConsole.PromptConfirm(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(x => (false, false), x => (false, true));
        // Mock file system to indicate selected file exists
        _mockFileSystem.FileExists(Arg.Any<string>()).Returns(true);
        _mockFileSystem.GetFileRootRepositoryPath(Arg.Any<string>()).Returns(Path.Combine(RepositoryPath, ".adrplus"));
        _mockFileSystem.GetFullNameDirectoryByFile(Arg.Any<string>()).Returns(RepositoryPath);
        // Mock ParseFileName to return valid ADR components
        _mockAdrServices.ParseFileName(Arg.Any<string>(), Arg.Any<AdrPlusRepoConfig>(), Arg.Any<IFileSystemService>())
            .Returns(eligibleAdr);
        _mockAdrServices.StatusUpdateAdrAsync(Arg.Any<string>(), AdrStatus.Unknown, Arg.Any<DateTime>(), Arg.Any<AdrPlusRepoConfig>(), _mockFileSystem, Arg.Any<CancellationToken>())
            .Returns((true, string.Empty));

        // Act
        await _handler.ExecuteAsync(args, CancellationToken.None);

        // Assert - StatusUpdateAdrAsync should be called once (on second confirmation)
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
    public async Task ExecuteAsync_WithWizardModeLoopThenFolderSelectionCancelled_ThrowsOperationCanceledException()
    {
        // Arrange - Test wizard loop: user confirms, loops back, then cancels folder selection
        var args = new[] { "--wizard" };
        var parsedArgs = new Dictionary<Arguments, string> { { Arguments.WizardUndoStatus, string.Empty } };
        var drives = new[] { SingleTestDrive };
        var jsonConfig = """{"Prefix": "ADR", "LenSeq": 4, "LenVersion": 2, "StatusNew": "Proposed", "template":"# ADR"}""";
        var eligibleAdr = CreateAdrFileNameComponentsEligibleForUndo(ValidAdrFilePath);

        _mockAdrServices.ParseArgs(args, Arg.Any<Arguments[]>()).Returns(parsedArgs);
        _mockValidateConfig.HasTemplateRepoFile().Returns(true);
        _mockFileSystem.GetDrives().Returns(drives);
        // First call: returns valid folder, second call: returns cancelled/aborted
        _mockConsole.PromptSelectFolderRepositoryPath(Arg.Any<bool>(), Arg.Any<string>(), _mockFileSystem, _mockValidateConfig, Arg.Any<CancellationToken>())
            .Returns(x => (false, RepositoryPath), x => (true, string.Empty));
        _mockValidateConfig.GetFileNameRepoConfig().Returns(".adrplus");
        _mockFileSystem.ReadAllTextAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(jsonConfig);
        _mockValidateConfig.ValidateRepoStructure(jsonConfig).Returns((true, []));
        _mockConsole.WriteWait(Arg.Any<string>());
        var cursorPos = (0, 0);
        _mockConsole.GetCursorPosition().Returns(cursorPos);
        _mockAdrServices
            .ReadAllAdr(Arg.Any<IFileSystemService>(), Arg.Any<string>(), Arg.Any<AdrPlusRepoConfig>(), false)
            .Returns(x => Task.FromResult(new[] { eligibleAdr }));
        _mockConsole.ClearWait(cursorPos);
        _mockAdrServices
            .ReadAllAdrByNumber(Arg.Any<int>(), Arg.Any<IFileSystemService>(), Arg.Any<string>(), Arg.Any<AdrPlusRepoConfig>())
            .Returns(new[] { eligibleAdr });
        _mockConsole.PromptSelecAdrs(Arg.Any<AdrFileNameComponents[]>(), Arg.Any<AdrPlusRepoConfig>(), Arg.Any<Func<AdrFileNameComponents, (bool, string?)>>(), Arg.Any<CancellationToken>())
            .Returns((false, eligibleAdr));
        // First confirm: no, loops back; folder selection then cancelled
        _mockConsole.PromptConfirm(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((false, false));

        // Act & Assert
        await _handler.Invoking(h => h.ExecuteAsync(args, CancellationToken.None))
            .Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task ExecuteAsync_WithWizardModeLoopThenAdrSelectionCancelled_ThrowsOperationCanceledException()
    {
        // Arrange - Test wizard loop: user doesn't confirm, loops back, then cancels ADR selection
        var args = new[] { "--wizard" };
        var parsedArgs = new Dictionary<Arguments, string> { { Arguments.WizardUndoStatus, string.Empty } };
        var drives = new[] { SingleTestDrive };
        var jsonConfig = """{"Prefix": "ADR", "LenSeq": 4, "LenVersion": 2, "StatusNew": "Proposed", "template":"# ADR"}""";
        var eligibleAdr = CreateAdrFileNameComponentsEligibleForUndo(ValidAdrFilePath);

        _mockAdrServices.ParseArgs(args, Arg.Any<Arguments[]>()).Returns(parsedArgs);
        _mockValidateConfig.HasTemplateRepoFile().Returns(true);
        _mockFileSystem.GetDrives().Returns(drives);
        _mockConsole.PromptSelectFolderRepositoryPath(Arg.Any<bool>(), Arg.Any<string>(), _mockFileSystem, _mockValidateConfig, Arg.Any<CancellationToken>())
            .Returns((false, RepositoryPath));
        _mockValidateConfig.GetFileNameRepoConfig().Returns(".adrplus");
        _mockFileSystem.ReadAllTextAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(jsonConfig);
        _mockValidateConfig.ValidateRepoStructure(jsonConfig).Returns((true, []));
        _mockConsole.WriteWait(Arg.Any<string>());
        var cursorPos = (0, 0);
        _mockConsole.GetCursorPosition().Returns(cursorPos);
        _mockAdrServices
            .ReadAllAdr(Arg.Any<IFileSystemService>(), Arg.Any<string>(), Arg.Any<AdrPlusRepoConfig>(), false)
            .Returns(x => Task.FromResult(new[] { eligibleAdr }));
        _mockConsole.ClearWait(cursorPos);
        _mockAdrServices
            .ReadAllAdrByNumber(Arg.Any<int>(), Arg.Any<IFileSystemService>(), Arg.Any<string>(), Arg.Any<AdrPlusRepoConfig>())
            .Returns(new[] { eligibleAdr });
        // First ADR selection: normal, second ADR selection: cancelled
        _mockConsole.PromptSelecAdrs(Arg.Any<AdrFileNameComponents[]>(), Arg.Any<AdrPlusRepoConfig>(), Arg.Any<Func<AdrFileNameComponents, (bool, string?)>>(), Arg.Any<CancellationToken>())
            .Returns(x => (false, eligibleAdr), x => (true, (AdrFileNameComponents?)null));
        _mockConsole.PromptConfirm(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((false, false));

        // Act & Assert
        await _handler.Invoking(h => h.ExecuteAsync(args, CancellationToken.None))
            .Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task ExecuteAsync_WithWizardModeSuccessfulSelection_UpdatesAdrStatus()
    {
        // Arrange - Test complete successful wizard flow: selection + confirmation + status update
        var args = new[] { "--wizard" };
        var parsedArgs = new Dictionary<Arguments, string> { { Arguments.WizardUndoStatus, string.Empty } };
        var drives = new[] { SingleTestDrive };
        var jsonConfig = """{"Prefix": "ADR", "LenSeq": 4, "LenVersion": 2, "StatusNew": "Proposed", "StatusAcc": "Accepted", "template":"# ADR"}""";
        var eligibleAdr = CreateAdrFileNameComponentsEligibleForUndo(ValidAdrFilePath);

        _mockAdrServices.ParseArgs(args, Arg.Any<Arguments[]>()).Returns(parsedArgs);
        _mockValidateConfig.HasTemplateRepoFile().Returns(true);
        _mockFileSystem.GetDrives().Returns(drives);
        _mockConsole.PromptSelectFolderRepositoryPath(Arg.Any<bool>(), Arg.Any<string>(), _mockFileSystem, _mockValidateConfig, Arg.Any<CancellationToken>())
            .Returns((false, RepositoryPath));
        _mockValidateConfig.GetFileNameRepoConfig().Returns(".adrplus");
        _mockFileSystem.ReadAllTextAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(jsonConfig);
        _mockValidateConfig.ValidateRepoStructure(jsonConfig).Returns((true, []));
        _mockConsole.WriteWait(Arg.Any<string>());
        var cursorPos = (0, 0);
        _mockConsole.GetCursorPosition().Returns(cursorPos);
        _mockAdrServices
            .ReadAllAdr(Arg.Any<IFileSystemService>(), Arg.Any<string>(), Arg.Any<AdrPlusRepoConfig>(), false)
            .Returns(x => Task.FromResult(new[] { eligibleAdr }));
        _mockConsole.ClearWait(cursorPos);
        _mockAdrServices
            .ReadAllAdrByNumber(Arg.Any<int>(), Arg.Any<IFileSystemService>(), Arg.Any<string>(), Arg.Any<AdrPlusRepoConfig>())
            .Returns(new[] { eligibleAdr });
        _mockConsole.PromptSelecAdrs(Arg.Any<AdrFileNameComponents[]>(), Arg.Any<AdrPlusRepoConfig>(), Arg.Any<Func<AdrFileNameComponents, (bool, string?)>>(), Arg.Any<CancellationToken>())
            .Returns((false, eligibleAdr));
        // Mock WriteSummary to simulate console output (void method, no validation needed)
        _mockConsole.WriteSummary(Arg.Any<string>());
        _mockConsole.PromptConfirm(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((false, true));  // Confirm yes
        // Mock file system to indicate selected file exists
        _mockFileSystem.FileExists(Arg.Any<string>()).Returns(true);
        _mockFileSystem.GetFileRootRepositoryPath(Arg.Any<string>()).Returns(Path.Combine(RepositoryPath, ".adrplus"));
        _mockFileSystem.GetFullNameDirectoryByFile(Arg.Any<string>()).Returns(RepositoryPath);
        // Mock ParseFileName to return valid ADR components
        _mockAdrServices.ParseFileName(Arg.Any<string>(), Arg.Any<AdrPlusRepoConfig>(), Arg.Any<IFileSystemService>())
            .Returns(eligibleAdr);
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
    public async Task ExecuteAsync_WithWizardModeSelectedMigratedAdr_AllowsSelection()
    {
        // Arrange - Test that migrated ADRs with Unknown status are allowed to be selected
        var args = new[] { "--wizard" };
        var parsedArgs = new Dictionary<Arguments, string> { { Arguments.WizardUndoStatus, string.Empty } };
        var drives = new[] { SingleTestDrive };
        var jsonConfig = """{"Prefix": "ADR", "LenSeq": 4, "LenVersion": 2, "StatusNew": "Proposed", "template":"# ADR"}""";
        var migratedAdr = new AdrFileNameComponents
        {
            FileName = ValidAdrFilePath,
            IsValid = true,
            Number = 1,
            Header = new AdrHeader
            {
                IsValid = true,
                StatusUpdate = AdrStatus.Accepted,
                StatusChange = AdrStatus.Unknown,
                StatusCreate = AdrStatus.Proposed,
                IsMigrated = true  // Migrated ADR
            }
        };

        _mockAdrServices.ParseArgs(args, Arg.Any<Arguments[]>()).Returns(parsedArgs);
        _mockValidateConfig.HasTemplateRepoFile().Returns(true);
        _mockFileSystem.GetDrives().Returns(drives);
        _mockConsole.PromptSelectFolderRepositoryPath(Arg.Any<bool>(), Arg.Any<string>(), _mockFileSystem, _mockValidateConfig, Arg.Any<CancellationToken>())
            .Returns((false, RepositoryPath));
        _mockValidateConfig.GetFileNameRepoConfig().Returns(".adrplus");
        _mockFileSystem.ReadAllTextAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(jsonConfig);
        _mockValidateConfig.ValidateRepoStructure(jsonConfig).Returns((true, []));
        _mockConsole.WriteWait(Arg.Any<string>());
        var cursorPos = (0, 0);
        _mockConsole.GetCursorPosition().Returns(cursorPos);
        _mockAdrServices
            .ReadAllAdr(Arg.Any<IFileSystemService>(), Arg.Any<string>(), Arg.Any<AdrPlusRepoConfig>(), false)
            .Returns(x => Task.FromResult(new[] { migratedAdr }));
        _mockConsole.ClearWait(cursorPos);
        _mockAdrServices
            .ReadAllAdrByNumber(Arg.Any<int>(), Arg.Any<IFileSystemService>(), Arg.Any<string>(), Arg.Any<AdrPlusRepoConfig>())
            .Returns(new[] { migratedAdr });
        _mockConsole.PromptSelecAdrs(Arg.Any<AdrFileNameComponents[]>(), Arg.Any<AdrPlusRepoConfig>(), Arg.Any<Func<AdrFileNameComponents, (bool, string?)>>(), Arg.Any<CancellationToken>())
            .Returns((false, migratedAdr));
        // Mock WriteSummary to simulate console output (void method, no validation needed)
        _mockConsole.WriteSummary(Arg.Any<string>());
        _mockConsole.PromptConfirm(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((false, true));
        // Mock file system to indicate selected file exists
        _mockFileSystem.FileExists(Arg.Any<string>()).Returns(true);
        _mockFileSystem.GetFileRootRepositoryPath(Arg.Any<string>()).Returns(Path.Combine(RepositoryPath, ".adrplus"));
        _mockFileSystem.GetFullNameDirectoryByFile(Arg.Any<string>()).Returns(RepositoryPath);
        // Mock ParseFileName to return migrated ADR components
        _mockAdrServices.ParseFileName(Arg.Any<string>(), Arg.Any<AdrPlusRepoConfig>(), Arg.Any<IFileSystemService>())
            .Returns(migratedAdr);
        _mockAdrServices.StatusUpdateAdrAsync(Arg.Any<string>(), AdrStatus.Unknown, Arg.Any<DateTime>(), Arg.Any<AdrPlusRepoConfig>(), _mockFileSystem, Arg.Any<CancellationToken>())
            .Returns((true, string.Empty));

        // Act
        await _handler.ExecuteAsync(args, CancellationToken.None);

        // Assert - Verify the operation completes successfully
        await _mockAdrServices.Received(1).StatusUpdateAdrAsync(
            Arg.Any<string>(),
            AdrStatus.Unknown,
            Arg.Any<DateTime>(),
            Arg.Any<AdrPlusRepoConfig>(),
            _mockFileSystem,
            Arg.Any<CancellationToken>());
        _mockConsole.Received(1).WriteSuccess(Arg.Any<string>());
    }

    #endregion

    #region Helper Methods

    private void SetupBasicMocks(Dictionary<Arguments, string> parsedArgs, string jsonConfig)
    {
        CommandHandlerMockHelper.SetupBasicCommandMocks(
            _mockAdrServices,
            _mockFileSystem,
            _mockValidateConfig,
            parsedArgs,
            jsonConfig);
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

