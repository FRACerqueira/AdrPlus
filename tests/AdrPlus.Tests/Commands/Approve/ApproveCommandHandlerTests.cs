// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the AdrPlus project under MIT license
// ***************************************************************************************

using AdrPlus.Commands;
using AdrPlus.Commands.Approve;
using AdrPlus.Core;
using AdrPlus.Domain;
using AdrPlus.Infrastructure.FileSystem;
using AdrPlus.Infrastructure.UI;
using AdrPlus.Tests.Helpers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using static AdrPlus.Tests.Helpers.TestPathData;

namespace AdrPlus.Tests.Commands.Approve;

/// <summary>
/// Unit tests for ApproveCommandHandler class.
/// Tests demonstrate approve command execution, wizard flows, and validation patterns using NSubstitute.
/// </summary>
public class ApproveCommandHandlerTests
{
    private readonly ILogger<ApproveCommandHandler> _mockLogger;
    private readonly IFileSystemService _mockFileSystem;
    private readonly IConsoleWriter _mockConsole;
    private readonly IValidateJsonConfig _mockValidateConfig;
    private readonly IAdrServices _mockAdrServices;
    private readonly AdrPlusConfig _config;
    private readonly ApproveCommandHandler _handler;

    public ApproveCommandHandlerTests()
    {
        _mockLogger = Substitute.For<ILogger<ApproveCommandHandler>>();
        _mockFileSystem = Substitute.For<IFileSystemService>();
        _mockConsole = Substitute.For<IConsoleWriter>();
        _mockValidateConfig = Substitute.For<IValidateJsonConfig>();
        _mockAdrServices = Substitute.For<IAdrServices>();

        _config = new AdrPlusConfig
        {
            FolderRepo = "docs/adr",
           Language = "en-US"
        };

        // Setup default console cursor position
        _mockConsole.GetCursorPosition().Returns((0, 0));

        var options = Options.Create(_config);

        _handler = new ApproveCommandHandler(
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
        var handler = new ApproveCommandHandler(
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
    public async Task ExecuteAsync_WithValidFile_ApprovesAdr()
    {
        // Arrange
        var args = new[] { "--file", ValidAdrFilePath };
        var parsedArgs = new Dictionary<Arguments, string> { { Arguments.FileAdr, ValidAdrFilePath } };
        var jsonConfig = """{"Prefix": "ADR", "LenSeq": 4, "LenVersion": 2, "StatusNew": "Proposed", "StatusAcc": "Accepted"}""";

        SetupBasicMocks(parsedArgs, jsonConfig);

        var adrInfo = CreateAdrFileNameComponents(ValidAdrFilePath, AdrStatus.Unknown);
        _mockAdrServices.ParseFileName(ValidAdrFilePath, Arg.Any<AdrPlusRepoConfig>(), _mockFileSystem)
            .Returns(adrInfo);
        _mockAdrServices.StatusUpdateAdrAsync(Arg.Any<string>(), AdrStatus.Accepted, Arg.Any<DateTime>(), Arg.Any<AdrPlusRepoConfig>(), _mockFileSystem, Arg.Any<CancellationToken>())
            .Returns((true, string.Empty));

        // Act
        await _handler.ExecuteAsync(args, CancellationToken.None);

        // Assert
        await _mockAdrServices.Received(1).StatusUpdateAdrAsync(
            Arg.Any<string>(),
            AdrStatus.Accepted,
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

        var adrInfo = CreateAdrFileNameComponents(AdrFileWithExtensionPath, AdrStatus.Unknown);
        _mockAdrServices.ParseFileName(Arg.Is<string>(s => s.EndsWith(".md")), Arg.Any<AdrPlusRepoConfig>(), _mockFileSystem)
            .Returns(adrInfo);
        _mockAdrServices.StatusUpdateAdrAsync(Arg.Any<string>(), AdrStatus.Accepted, Arg.Any<DateTime>(), Arg.Any<AdrPlusRepoConfig>(), _mockFileSystem, Arg.Any<CancellationToken>())
            .Returns((true, string.Empty));

        // Act
        await _handler.ExecuteAsync(args, CancellationToken.None);

        // Assert
        _mockFileSystem.Received().FileExists(Arg.Is<string>(s => s.EndsWith(".md")));
    }

    [Theory]
    [MemberData(nameof(CultureData.Cultures), MemberType = typeof(CultureData))]
    public async Task ExecuteAsync_WithCustomDate_UsesProvidedDate(string cultureName)
    {
        await CultureData.WithCultureAsync(cultureName, async () =>
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

            var adrInfo = CreateAdrFileNameComponents(ValidAdrFilePath, AdrStatus.Unknown);
            _mockAdrServices.ParseFileName(ValidAdrFilePath, Arg.Any<AdrPlusRepoConfig>(), _mockFileSystem)
                .Returns(adrInfo);
            _mockAdrServices.StatusUpdateAdrAsync(Arg.Any<string>(), AdrStatus.Accepted, Arg.Any<DateTime>(), Arg.Any<AdrPlusRepoConfig>(), _mockFileSystem, Arg.Any<CancellationToken>())
                .Returns((true, string.Empty));

            // Act
            await _handler.ExecuteAsync(args, CancellationToken.None);

            // Assert
            await _mockAdrServices.Received(1).StatusUpdateAdrAsync(
                Arg.Any<string>(),
                AdrStatus.Accepted,
                Arg.Is<DateTime>(d => d.Year == 2026 && d.Month == 1 && d.Day == 15),
                Arg.Any<AdrPlusRepoConfig>(),
                _mockFileSystem,
                Arg.Any<CancellationToken>());
        });
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
    public async Task ExecuteAsync_WhenVersionNotConfigured_ThrowsInvalidDataException()
    {
        // Arrange
        var args = new[] { "--file", ValidAdrFilePath };
        var parsedArgs = new Dictionary<Arguments, string> { { Arguments.FileAdr, ValidAdrFilePath } };
        var configPath = ConfigFilePath;
        var jsonConfig = """{"Prefix": "ADR", "LenSeq": 4, "LenVersion": 0}""";

        _mockAdrServices.ParseArgs(args, Arg.Any<Arguments[]>()).Returns(parsedArgs);
        _mockValidateConfig.HasTemplateRepoFile().Returns(true);
        _mockFileSystem.FileExists(ValidAdrFilePath).Returns(true);
        _mockValidateConfig.GetFileNameRepoConfig().Returns(".adrplus");
        _mockFileSystem.FileExists(configPath).Returns(true);
        _mockFileSystem.ReadAllTextAsync(configPath, Arg.Any<CancellationToken>()).Returns(jsonConfig);
        _mockValidateConfig.ValidateRepoStructure(jsonConfig).Returns((true, []));

        // Act & Assert
        await _handler.Invoking(h => h.ExecuteAsync(args, CancellationToken.None))
            .Should().ThrowAsync<InvalidDataException>();
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

    [Fact]
    public async Task ExecuteAsync_WhenAdrAlreadyApproved_ThrowsInvalidDataException()
    {
        // Arrange
        var args = new[] { "--file", ValidAdrFilePath };
        var parsedArgs = new Dictionary<Arguments, string> { { Arguments.FileAdr, ValidAdrFilePath } };
        var jsonConfig = """{"Prefix": "ADR", "LenSeq": 4, "LenVersion": 2, "StatusNew": "Proposed", "StatusAcc": "Accepted"}""";

        SetupBasicMocks(parsedArgs, jsonConfig);

        var adrInfo = CreateAdrFileNameComponents(ValidAdrFilePath, AdrStatus.Accepted);
        _mockAdrServices.ParseFileName(ValidAdrFilePath, Arg.Any<AdrPlusRepoConfig>(), _mockFileSystem)
            .Returns(adrInfo);

        // Act & Assert
        await _handler.Invoking(h => h.ExecuteAsync(args, CancellationToken.None))
            .Should().ThrowAsync<InvalidDataException>();
    }

    [Fact]
    public async Task ExecuteAsync_WhenStatusUpdateFails_ThrowsInvalidDataException()
    {
        // Arrange
        var args = new[] { "--file", ValidAdrFilePath };
        var parsedArgs = new Dictionary<Arguments, string> { { Arguments.FileAdr, ValidAdrFilePath } };
        var jsonConfig = """{"Prefix": "ADR", "LenSeq": 4, "LenVersion": 2, "StatusNew": "Proposed", "StatusAcc": "Accepted"}""";

        SetupBasicMocks(parsedArgs, jsonConfig);

        var adrInfo = CreateAdrFileNameComponents(ValidAdrFilePath, AdrStatus.Unknown);
        _mockAdrServices.ParseFileName(ValidAdrFilePath, Arg.Any<AdrPlusRepoConfig>(), _mockFileSystem)
            .Returns(adrInfo);
        _mockAdrServices.StatusUpdateAdrAsync(Arg.Any<string>(), AdrStatus.Accepted, Arg.Any<DateTime>(), Arg.Any<AdrPlusRepoConfig>(), _mockFileSystem, Arg.Any<CancellationToken>())
            .Returns((false, "Update failed"));

        // Act & Assert
        await _handler.Invoking(h => h.ExecuteAsync(args, CancellationToken.None))
            .Should().ThrowAsync<InvalidDataException>()
            .WithMessage("Update failed");
    }

    [Theory]
    [MemberData(nameof(CultureData.Cultures), MemberType = typeof(CultureData))]
    public async Task ExecuteAsync_WithInvalidDateFormat_ThrowsFormatException(string cultureName)
    {
        await CultureData.WithCultureAsync(cultureName, async () =>
        {
            // Arrange
            var args = InvalidDateFormatArgs;
            var parsedArgs = new Dictionary<Arguments, string>
            {
                { Arguments.FileAdr, ValidAdrFilePath },
                { Arguments.DateRefAdr, "invalid-date" }
            };
            var jsonConfig = """{"Prefix": "ADR", "LenSeq": 4, "LenVersion": 2, "StatusNew": "Proposed", "StatusAcc": "Accepted"}""";

            SetupBasicMocks(parsedArgs, jsonConfig);

            var adrInfo = CreateAdrFileNameComponents(ValidAdrFilePath, AdrStatus.Unknown);
            _mockAdrServices.ParseFileName(ValidAdrFilePath, Arg.Any<AdrPlusRepoConfig>(), _mockFileSystem)
                .Returns(adrInfo);

            // Act & Assert
            await _handler.Invoking(h => h.ExecuteAsync(args, CancellationToken.None))
                .Should().ThrowAsync<FormatException>();
        });
    }

    #endregion

    #region ExecuteAsync - Wizard Mode Tests - Cancellation Only

    [Fact]
    public async Task ExecuteAsync_WithWizardModeDriveSelectionAborted_ThrowsOperationCanceledException()
    {
        // Arrange
        var args = new[] { "--wizard" };
        var parsedArgs = new Dictionary<Arguments, string> { { Arguments.WizardApprove, string.Empty } };
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
        var parsedArgs = new Dictionary<Arguments, string> { { Arguments.WizardApprove, string.Empty } };
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

    private static AdrFileNameComponents CreateAdrFileNameComponents(string fileName, AdrStatus status)
    {
        return new AdrFileNameComponents
        {
            FileName = fileName,
            IsValid = true,
            Header = new AdrHeader
            {
                IsValid = true,
                StatusUpdate = status,
                StatusCreate = AdrStatus.Proposed
            }
        };
    }

    #endregion
}
