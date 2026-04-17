// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the AdrPlus project under MIT license
// ***************************************************************************************

using AdrPlus.Commands;
using AdrPlus.Commands.Review;
using AdrPlus.Core;
using AdrPlus.Domain;
using AdrPlus.Infrastructure.FileSystem;
using AdrPlus.Infrastructure.UI;
using AdrPlus.Tests.Helpers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AdrPlus.Tests.Commands.Review;

/// <summary>
/// Unit tests for ReviewCommandHandler class.
/// Tests cover review command execution, wizard flows, and validation patterns using NSubstitute.
/// </summary>
public class ReviewCommandHandlerTests
{
    private readonly ILogger<ReviewCommandHandler> _mockLogger;
    private readonly IFileSystemService _mockFileSystem;
    private readonly IConsoleWriter _mockConsole;
    private readonly IValidateJsonConfig _mockValidateConfig;
    private readonly IAdrServices _mockAdrServices;
    private readonly AdrPlusConfig _config;
    private readonly ReviewCommandHandler _handler;

    public ReviewCommandHandlerTests()
    {
        _mockLogger = Substitute.For<ILogger<ReviewCommandHandler>>();
        _mockFileSystem = Substitute.For<IFileSystemService>();
        _mockConsole = Substitute.For<IConsoleWriter>();
        _mockValidateConfig = Substitute.For<IValidateJsonConfig>();
        _mockAdrServices = Substitute.For<IAdrServices>();

        _config = new AdrPlusConfig
        {
            FolderRepo = "docs/adr",
            Language = "en-US"
        };

        _mockConsole.GetCursorPosition().Returns((0, 0));

        var options = Options.Create(_config);

        _handler = new ReviewCommandHandler(
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
        var handler = new ReviewCommandHandler(
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

    [Fact]
    public async Task ExecuteAsync_WithHelpArgument_DoesNotValidateTemplateFile()
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
        _mockValidateConfig.DidNotReceive().HasTemplateRepoFile();
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

    #region ExecuteAsync - File Validation Tests

    [Fact]
    public async Task ExecuteAsync_WhenFileNotFound_ThrowsFileNotFoundException()
    {
        // Arrange
        var args = new[] { "--file", TestPathData.MissingAdrFilePath };
        var parsedArgs = new Dictionary<Arguments, string> { { Arguments.FileAdr, TestPathData.MissingAdrFilePath } };

        _mockAdrServices.ParseArgs(args, Arg.Any<Arguments[]>()).Returns(parsedArgs);
        _mockValidateConfig.HasTemplateRepoFile().Returns(true);
        _mockFileSystem.FileExists(Arg.Any<string>()).Returns(false);

        // Act & Assert
        await _handler.Invoking(h => h.ExecuteAsync(args, CancellationToken.None))
            .Should().ThrowAsync<FileNotFoundException>();
    }

    [Fact]
    public async Task ExecuteAsync_WithFileWithoutExtension_AddsMarkdownExtension()
    {
        // Arrange
        var args = new[] { "--file", TestPathData.AdrFileWithoutExtensionPath };
        var parsedArgs = new Dictionary<Arguments, string> { { Arguments.FileAdr, TestPathData.AdrFileWithoutExtensionPath } };
        var jsonConfig = BuildJsonConfig();

        SetupBasicMocks(parsedArgs, jsonConfig);
        _mockFileSystem.FileExists(Arg.Is<string>(s => s.EndsWith(".md"))).Returns(true);

        var adrInfo = CreateAcceptedAdrComponents(TestPathData.ValidAdrFilePath);
        SetupLatestAdrSequence(adrInfo);
        _mockAdrServices.ParseFileName(Arg.Is<string>(s => s.EndsWith(".md")), Arg.Any<AdrPlusRepoConfig>(), _mockFileSystem)
            .Returns(adrInfo);

        SetupCreateAdrFile();

        // Act
        await _handler.ExecuteAsync(args, CancellationToken.None);

        // Assert
        _mockFileSystem.Received().FileExists(Arg.Is<string>(s => s.EndsWith(".md")));
    }

    [Fact]
    public async Task ExecuteAsync_WhenFileNotInAdrFolder_ThrowsInvalidDataException()
    {
        // Arrange
        var args = new[] { "--file", TestPathData.FileOutsideAdrFolderPath };
        var parsedArgs = new Dictionary<Arguments, string> { { Arguments.FileAdr, TestPathData.FileOutsideAdrFolderPath } };

        _mockAdrServices.ParseArgs(args, Arg.Any<Arguments[]>()).Returns(parsedArgs);
        _mockValidateConfig.HasTemplateRepoFile().Returns(true);
        _mockFileSystem.FileExists(TestPathData.FileOutsideAdrFolderPath).Returns(true);

        // Act & Assert
        await _handler.Invoking(h => h.ExecuteAsync(args, CancellationToken.None))
            .Should().ThrowAsync<InvalidDataException>();
    }

    #endregion

    #region ExecuteAsync - Config Validation Tests

    [Fact]
    public async Task ExecuteAsync_WhenConfigFileNotFound_ThrowsFileNotFoundException()
    {
        // Arrange
        var args = new[] { "--file", TestPathData.ValidAdrFilePath };
        var parsedArgs = new Dictionary<Arguments, string> { { Arguments.FileAdr, TestPathData.ValidAdrFilePath } };

        _mockAdrServices.ParseArgs(args, Arg.Any<Arguments[]>()).Returns(parsedArgs);
        _mockValidateConfig.HasTemplateRepoFile().Returns(true);
        _mockFileSystem.FileExists(TestPathData.ValidAdrFilePath).Returns(true);
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
        var args = new[] { "--file", TestPathData.ValidAdrFilePath };
        var parsedArgs = new Dictionary<Arguments, string> { { Arguments.FileAdr, TestPathData.ValidAdrFilePath } };
        var configPath = TestPathData.ConfigFilePath;
        var jsonConfig = """{"Invalid": "config"}""";
        var errors = new[] { "Missing Prefix field" };

        _mockAdrServices.ParseArgs(args, Arg.Any<Arguments[]>()).Returns(parsedArgs);
        _mockValidateConfig.HasTemplateRepoFile().Returns(true);
        _mockFileSystem.FileExists(TestPathData.ValidAdrFilePath).Returns(true);
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
    public async Task ExecuteAsync_WhenRevisionNotConfigured_ThrowsInvalidDataException()
    {
        // Arrange
        var args = new[] { "--file", TestPathData.ValidAdrFilePath };
        var parsedArgs = new Dictionary<Arguments, string> { { Arguments.FileAdr, "C:\\repo\\docs\\adr\\adr-0001.md" } };
        var jsonConfig = """{"Prefix": "ADR", "LenSeq": 4, "LenVersion": 2, "LenRevision": 0}""";

        _mockAdrServices.ParseArgs(args, Arg.Any<Arguments[]>()).Returns(parsedArgs);
        _mockValidateConfig.HasTemplateRepoFile().Returns(true);
        _mockFileSystem.FileExists("C:\\repo\\docs\\adr\\adr-0001.md").Returns(true);
        _mockValidateConfig.GetFileNameRepoConfig().Returns(".adrplus");
        _mockFileSystem.FileExists(Arg.Is<string>(s => s.EndsWith(".adrplus"))).Returns(true);
        _mockFileSystem.ReadAllTextAsync(Arg.Is<string>(s => s.EndsWith(".adrplus")), Arg.Any<CancellationToken>()).Returns(jsonConfig);
        _mockValidateConfig.ValidateRepoStructure(jsonConfig).Returns((true, []));

        // Act & Assert
        await _handler.Invoking(h => h.ExecuteAsync(args, CancellationToken.None))
            .Should().ThrowAsync<InvalidDataException>();
    }

    #endregion

    #region ExecuteAsync - ADR Parsing Tests

    [Fact]
    public async Task ExecuteAsync_WhenInvalidFileName_ThrowsInvalidDataException()
    {
        // Arrange
        var args = new[] { "--file", TestPathData.InvalidFileNameAdrPath };
        var parsedArgs = new Dictionary<Arguments, string> { { Arguments.FileAdr, TestPathData.InvalidFileNameAdrPath } };
        var jsonConfig = BuildJsonConfig();

        _mockAdrServices.ParseArgs(args, Arg.Any<Arguments[]>()).Returns(parsedArgs);
        _mockValidateConfig.HasTemplateRepoFile().Returns(true);
        _mockFileSystem.FileExists(TestPathData.InvalidFileNameAdrPath).Returns(true);
        _mockValidateConfig.GetFileNameRepoConfig().Returns(".adrplus");
        _mockFileSystem.FileExists(Arg.Is<string>(s => s.EndsWith(".adrplus"))).Returns(true);
        _mockFileSystem.ReadAllTextAsync(Arg.Is<string>(s => s.EndsWith(".adrplus")), Arg.Any<CancellationToken>()).Returns(jsonConfig);
        _mockValidateConfig.ValidateRepoStructure(jsonConfig).Returns((true, []));

        var adrInfo = new AdrFileNameComponents
        {
            IsValid = false,
            ErrorMessage = "Invalid file name format"
        };
        _mockAdrServices.ParseFileName(TestPathData.InvalidFileNameAdrPath, Arg.Any<AdrPlusRepoConfig>(), _mockFileSystem)
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
        var args = new[] { "--file", TestPathData.ValidAdrFilePath };
        var parsedArgs = new Dictionary<Arguments, string> { { Arguments.FileAdr, TestPathData.ValidAdrFilePath } };
        var jsonConfig = BuildJsonConfig();

        _mockAdrServices.ParseArgs(args, Arg.Any<Arguments[]>()).Returns(parsedArgs);
        _mockValidateConfig.HasTemplateRepoFile().Returns(true);
        _mockFileSystem.FileExists("C:\\repo\\docs\\adr\\adr-0001.md").Returns(true);
        _mockValidateConfig.GetFileNameRepoConfig().Returns(".adrplus");
        _mockFileSystem.FileExists(Arg.Is<string>(s => s.EndsWith(".adrplus"))).Returns(true);
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
        _mockAdrServices.ParseFileName(TestPathData.ValidAdrFilePath, Arg.Any<AdrPlusRepoConfig>(), _mockFileSystem)
            .Returns(adrInfo);

        // Act & Assert
        await _handler.Invoking(h => h.ExecuteAsync(args, CancellationToken.None))
            .Should().ThrowAsync<InvalidDataException>()
            .WithMessage("Invalid header format");
    }

    #endregion

    #region ExecuteAsync - Latest Version Validation Tests

    [Fact]
    public async Task ExecuteAsync_WhenNotLatestVersion_ThrowsInvalidOperationException()
    {
        // Arrange
        var args = new[] { "--file", TestPathData.GetScopePath("adr-0001-v01r00.md") };
        var parsedArgs = new Dictionary<Arguments, string> { { Arguments.FileAdr, TestPathData.GetScopePath("adr-0001-v01r00.md") } };
        var jsonConfig = BuildJsonConfig();

        SetupBasicMocks(parsedArgs, jsonConfig);

        var adrInfo = CreateAcceptedAdrComponents(TestPathData.GetScopePath("adr-0001-v01r00.md"));
        var latestAdrInfo = CreateAcceptedAdrComponents(TestPathData.GetScopePath("adr-0001-v01r01.md"));

        _mockAdrServices.ParseFileName(TestPathData.GetScopePath("adr-0001-v01r00.md"), Arg.Any<AdrPlusRepoConfig>(), _mockFileSystem)
            .Returns(adrInfo);
        _mockAdrServices.GetLatestADRSequence(Arg.Any<int>(), _mockFileSystem, Arg.Any<string>(), Arg.Any<AdrPlusRepoConfig>())
            .Returns(latestAdrInfo);

        // Act & Assert
        await _handler.Invoking(h => h.ExecuteAsync(args, CancellationToken.None))
            .Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task ExecuteAsync_WhenLatestAdrInfoIsInvalid_ThrowsInvalidDataException()
    {
        // Arrange
        var args = new[] { "--file", TestPathData.ValidAdrFilePath };
        var parsedArgs = new Dictionary<Arguments, string> { { Arguments.FileAdr, TestPathData.ValidAdrFilePath } };
        var jsonConfig = BuildJsonConfig();

        SetupBasicMocks(parsedArgs, jsonConfig);

        var adrInfo = CreateAcceptedAdrComponents("C:\\repo\\docs\\adr\\adr-0001.md");
        _mockAdrServices.ParseFileName("C:\\repo\\docs\\adr\\adr-0001.md", Arg.Any<AdrPlusRepoConfig>(), _mockFileSystem)
            .Returns(adrInfo);

        var invalidLatest = new AdrFileNameComponents
        {
            FileName = TestPathData.ValidAdrFilePath,
            IsValid = false,
            ErrorMessage = "Could not determine latest"
        };
        _mockAdrServices.GetLatestADRSequence(Arg.Any<int>(), _mockFileSystem, Arg.Any<string>(), Arg.Any<AdrPlusRepoConfig>())
            .Returns(invalidLatest);

        // Act & Assert
        await _handler.Invoking(h => h.ExecuteAsync(args, CancellationToken.None))
            .Should().ThrowAsync<InvalidDataException>()
            .WithMessage("Could not determine latest");
    }

    [Fact]
    public async Task ExecuteAsync_WhenLatestAdrHeaderIsInvalid_ThrowsInvalidDataException()
    {
        // Arrange
        var args = new[] { "--file", TestPathData.ValidAdrFilePath };
        var parsedArgs = new Dictionary<Arguments, string> { { Arguments.FileAdr, TestPathData.ValidAdrFilePath } };
        var jsonConfig = BuildJsonConfig();

        SetupBasicMocks(parsedArgs, jsonConfig);

        var adrInfo = CreateAcceptedAdrComponents("C:\\repo\\docs\\adr\\adr-0001.md");
        _mockAdrServices.ParseFileName("C:\\repo\\docs\\adr\\adr-0001.md", Arg.Any<AdrPlusRepoConfig>(), _mockFileSystem)
            .Returns(adrInfo);

        var latestWithInvalidHeader = new AdrFileNameComponents
        {
            FileName = TestPathData.ValidAdrFilePath,
            IsValid = true,
            Header = new AdrHeader
            {
                IsValid = false,
                ErrorMessage = "Header parse error"
            }
        };
        _mockAdrServices.GetLatestADRSequence(Arg.Any<int>(), _mockFileSystem, Arg.Any<string>(), Arg.Any<AdrPlusRepoConfig>())
            .Returns(latestWithInvalidHeader);

        // Act & Assert
        await _handler.Invoking(h => h.ExecuteAsync(args, CancellationToken.None))
            .Should().ThrowAsync<InvalidDataException>()
            .WithMessage("Header parse error");
    }

    #endregion

    #region ExecuteAsync - Status Validation Tests

    [Fact]
    public async Task ExecuteAsync_WhenAdrStatusIsProposed_ThrowsInvalidDataException()
    {
        // Arrange
        var args = new[] { "--file", TestPathData.ValidAdrFilePath };
        var parsedArgs = new Dictionary<Arguments, string> { { Arguments.FileAdr, TestPathData.ValidAdrFilePath } };
        var jsonConfig = BuildJsonConfig();

        SetupBasicMocks(parsedArgs, jsonConfig);

        var adrInfo = CreateAdrComponents(TestPathData.ValidAdrFilePath, AdrStatus.Proposed, AdrStatus.Unknown);
        SetupLatestAdrSequence(adrInfo);
        _mockAdrServices.ParseFileName(TestPathData.ValidAdrFilePath, Arg.Any<AdrPlusRepoConfig>(), _mockFileSystem)
            .Returns(adrInfo);

        // Act & Assert
        await _handler.Invoking(h => h.ExecuteAsync(args, CancellationToken.None))
            .Should().ThrowAsync<InvalidDataException>();
    }

    [Fact]
    public async Task ExecuteAsync_WhenAcceptedAdrHasStatusChange_ThrowsInvalidDataException()
    {
        // Arrange
        var args = new[] { "--file", TestPathData.ValidAdrFilePath };
        var parsedArgs = new Dictionary<Arguments, string> { { Arguments.FileAdr, TestPathData.ValidAdrFilePath } };
        var jsonConfig = BuildJsonConfig();

        SetupBasicMocks(parsedArgs, jsonConfig);

        // Accepted but with a status change (e.g. Superseded) — not eligible for review
        var adrInfo = CreateAdrComponents(TestPathData.ValidAdrFilePath, AdrStatus.Accepted, AdrStatus.Superseded);
        SetupLatestAdrSequence(adrInfo);
        _mockAdrServices.ParseFileName(TestPathData.ValidAdrFilePath, Arg.Any<AdrPlusRepoConfig>(), _mockFileSystem)
            .Returns(adrInfo);

        // Act & Assert
        await _handler.Invoking(h => h.ExecuteAsync(args, CancellationToken.None))
            .Should().ThrowAsync<InvalidDataException>();
    }

    #endregion

    #region ExecuteAsync - Successful Review Tests

    [Fact]
    public async Task ExecuteAsync_WithAcceptedAdr_CreatesNewRevisionFile()
    {
        // Arrange
        var args = new[] { "--file", TestPathData.ValidAdrFilePath };
        var parsedArgs = new Dictionary<Arguments, string> { { Arguments.FileAdr, TestPathData.ValidAdrFilePath } };
        var jsonConfig = BuildJsonConfig();

        SetupBasicMocks(parsedArgs, jsonConfig);

        var adrInfo = CreateAcceptedAdrComponents("C:\\repo\\docs\\adr\\adr-0001.md");
        SetupLatestAdrSequence(adrInfo);
        _mockAdrServices.ParseFileName("C:\\repo\\docs\\adr\\adr-0001.md", Arg.Any<AdrPlusRepoConfig>(), _mockFileSystem)
            .Returns(adrInfo);
        SetupCreateAdrFile();

        // Act
        await _handler.ExecuteAsync(args, CancellationToken.None);

        // Assert
        await _mockFileSystem.Received(1).WriteAllTextAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        _mockConsole.Received(1).WriteSuccess(Arg.Any<string>());
    }

    [Fact]
    public async Task ExecuteAsync_WithRejectedAdr_CreatesNewRevisionFile()
    {
        // Arrange
        var args = new[] { "--file", TestPathData.ValidAdrFilePath };
        var parsedArgs = new Dictionary<Arguments, string> { { Arguments.FileAdr, TestPathData.ValidAdrFilePath } };
        var jsonConfig = BuildJsonConfig();

        SetupBasicMocks(parsedArgs, jsonConfig);

        var adrInfo = CreateAdrComponents(TestPathData.ValidAdrFilePath, AdrStatus.Rejected, AdrStatus.Unknown);
        SetupLatestAdrSequence(adrInfo);
        _mockAdrServices.ParseFileName(TestPathData.ValidAdrFilePath, Arg.Any<AdrPlusRepoConfig>(), _mockFileSystem)
            .Returns(adrInfo);
        SetupCreateAdrFile();

        // Act
        await _handler.ExecuteAsync(args, CancellationToken.None);

        // Assert
        await _mockFileSystem.Received(1).WriteAllTextAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        _mockConsole.Received(1).WriteSuccess(Arg.Any<string>());
    }

    [Fact]
    public async Task ExecuteAsync_WithCustomDate_UsesProvidedDate()
    {
        // Arrange
        var customDate = "2026-06-15";
        var args = new[] { "--file", TestPathData.ValidAdrFilePath, "--refdate", customDate };
        var parsedArgs = new Dictionary<Arguments, string>
        {
            { Arguments.FileAdr, TestPathData.ValidAdrFilePath },
            { Arguments.DateRefAdr, customDate }
        };
        var jsonConfig = BuildJsonConfig();

        SetupBasicMocks(parsedArgs, jsonConfig);

        var adrInfo = CreateAcceptedAdrComponents("C:\\repo\\docs\\adr\\adr-0001.md");
        SetupLatestAdrSequence(adrInfo);
        _mockAdrServices.ParseFileName("C:\\repo\\docs\\adr\\adr-0001.md", Arg.Any<AdrPlusRepoConfig>(), _mockFileSystem)
            .Returns(adrInfo);
        SetupCreateAdrFile();

        // Act
        await _handler.ExecuteAsync(args, CancellationToken.None);

        // Assert — the write must occur (date is embedded in the header via AdrRecord.GetHeader)
        await _mockFileSystem.Received(1).WriteAllTextAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [MemberData(nameof(CultureData.Cultures), MemberType = typeof(CultureData))]
    public async Task ExecuteAsync_WithInvalidDateFormat_ThrowsFormatException(string cultureName)
    {
        await CultureData.WithCultureAsync(cultureName, async () =>
        {
            // Arrange
            var args = new[] { "--file", TestPathData.ValidAdrFilePath, "--refdate", "not-a-date" };
            var parsedArgs = new Dictionary<Arguments, string>
            {
                { Arguments.FileAdr, TestPathData.ValidAdrFilePath },
                { Arguments.DateRefAdr, "not-a-date" }
            };
            var jsonConfig = BuildJsonConfig();

            SetupBasicMocks(parsedArgs, jsonConfig);

            var adrInfo = CreateAcceptedAdrComponents(TestPathData.ValidAdrFilePath);
            SetupLatestAdrSequence(adrInfo);
            _mockAdrServices.ParseFileName(TestPathData.ValidAdrFilePath, Arg.Any<AdrPlusRepoConfig>(), _mockFileSystem)
                .Returns(adrInfo);

            // Act & Assert
            await _handler.Invoking(h => h.ExecuteAsync(args, CancellationToken.None))
                .Should().ThrowAsync<FormatException>();
        });
    }

    [Fact]
    public async Task ExecuteAsync_WithNoDateArgument_UsesCurrentDate()
    {
        // Arrange
        var args = new[] { "--file", TestPathData.ValidAdrFilePath };
        var parsedArgs = new Dictionary<Arguments, string> { { Arguments.FileAdr, TestPathData.ValidAdrFilePath } };
        var jsonConfig = BuildJsonConfig();

        SetupBasicMocks(parsedArgs, jsonConfig);

        var adrInfo = CreateAcceptedAdrComponents("C:\\repo\\docs\\adr\\adr-0001.md");
        SetupLatestAdrSequence(adrInfo);
        _mockAdrServices.ParseFileName("C:\\repo\\docs\\adr\\adr-0001.md", Arg.Any<AdrPlusRepoConfig>(), _mockFileSystem)
            .Returns(adrInfo);
        SetupCreateAdrFile();

        // Act
        await _handler.ExecuteAsync(args, CancellationToken.None);

        // Assert — file is written using the default (today's) date
        await _mockFileSystem.Received(1).WriteAllTextAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    #endregion

    #region ExecuteAsync - Open File Tests

    [Fact]
    public async Task ExecuteAsync_WithOpenArgument_AndCommandSucceeds_WritesSuccessMessage()
    {
        // Arrange
        var (handler, mocks) = CreateIsolatedHandlerWithOpenCommand("code {0}");
        var jsonConfig = BuildJsonConfig();
        var args = new[] { "--file", TestPathData.ValidAdrFilePath, "--open" };
        var parsedArgs = new Dictionary<Arguments, string>
        {
            { Arguments.FileAdr, TestPathData.ValidAdrFilePath },
            { Arguments.OpenAdr, string.Empty }
        };

        mocks.AdrServices.ParseArgs(default!, default!).ReturnsForAnyArgs(parsedArgs);
        mocks.ValidateConfig.HasTemplateRepoFile().Returns(true);
        mocks.FileSystem.FileExists(default!).ReturnsForAnyArgs(true);
        mocks.ValidateConfig.GetFileNameRepoConfig().Returns(".adrplus");
        mocks.FileSystem.ReadAllTextAsync(default!, default).ReturnsForAnyArgs(jsonConfig);
        mocks.ValidateConfig.ValidateRepoStructure(default!).ReturnsForAnyArgs((true, []));
        mocks.FileSystem.GetFullNameFile(default!).ReturnsForAnyArgs(ci => ci.ArgAt<string>(0));

        var adrInfo = CreateAcceptedAdrComponents(TestPathData.ValidAdrFilePath);
        mocks.AdrServices.ParseFileName(default!, default!, default!).ReturnsForAnyArgs(adrInfo);
        mocks.AdrServices.GetLatestADRSequence(default, default!, default!, default!).ReturnsForAnyArgs(adrInfo);
        mocks.FileSystem.WriteAllTextAsync(default!, default!, default).ReturnsForAnyArgs(Task.CompletedTask);
        // OpenFile returning empty string signals success — handler calls WriteSuccess for the open command
        mocks.AdrServices.OpenFile(string.Empty, string.Empty).ReturnsForAnyArgs(string.Empty);

        // Act
        await handler.ExecuteAsync(args, CancellationToken.None);

        // Assert: two WriteSuccess calls — one for the created file, one for the open command
        mocks.Console.ReceivedCalls()
            .Where(c => c.GetMethodInfo().Name == nameof(IConsoleWriter.WriteSuccess))
            .Should().HaveCountGreaterOrEqualTo(2);
    }

    [Fact]
    public async Task ExecuteAsync_WithOpenArgumentAndCommandFails_WritesError()
    {
        // Arrange
        var (handler, mocks) = CreateIsolatedHandlerWithOpenCommand("code {0}");
        var jsonConfig = BuildJsonConfig();
        var args = new[] { "--file", TestPathData.ValidAdrFilePath, "--open" };
        var parsedArgs = new Dictionary<Arguments, string>
        {
            { Arguments.FileAdr, TestPathData.ValidAdrFilePath },
            { Arguments.OpenAdr, string.Empty }
        };

        mocks.AdrServices.ParseArgs(default!, default!).ReturnsForAnyArgs(parsedArgs);
        mocks.ValidateConfig.HasTemplateRepoFile().Returns(true);
        mocks.FileSystem.FileExists(default!).ReturnsForAnyArgs(true);
        mocks.ValidateConfig.GetFileNameRepoConfig().Returns(".adrplus");
        mocks.FileSystem.ReadAllTextAsync(default!, default).ReturnsForAnyArgs(jsonConfig);
        mocks.ValidateConfig.ValidateRepoStructure(default!).ReturnsForAnyArgs((true, []));
        mocks.FileSystem.GetFullNameFile(default!).ReturnsForAnyArgs(ci => ci.ArgAt<string>(0));

        var adrInfo = CreateAcceptedAdrComponents(TestPathData.ValidAdrFilePath);
        mocks.AdrServices.ParseFileName(default!, default!, default!).ReturnsForAnyArgs(adrInfo);
        mocks.AdrServices.GetLatestADRSequence(default, default!, default!, default!).ReturnsForAnyArgs(adrInfo);
        mocks.FileSystem.WriteAllTextAsync(default!, default!, default).ReturnsForAnyArgs(Task.CompletedTask);
        // OpenFile returning a non-empty string signals failure — handler calls WriteError
        mocks.AdrServices.OpenFile(string.Empty, string.Empty).ReturnsForAnyArgs("Failed to open editor");

        // Act
        await handler.ExecuteAsync(args, CancellationToken.None);

        // Assert
        mocks.Console.ReceivedCalls()
            .Where(c => c.GetMethodInfo().Name == nameof(IConsoleWriter.WriteError))
            .Should().HaveCount(1);
    }

    [Fact]
    public async Task ExecuteAsync_WithOpenArgumentButNoCommandConfigured_DoesNotCallOpenFile()
    {
        // Arrange (default config has no ComandOpenAdr)
        var args = new[] { "--file", TestPathData.ValidAdrFilePath, "--open" };
        var parsedArgs = new Dictionary<Arguments, string>
        {
            { Arguments.FileAdr, TestPathData.ValidAdrFilePath },
            { Arguments.OpenAdr, string.Empty }
        };
        var jsonConfig = BuildJsonConfig();

        SetupBasicMocks(parsedArgs, jsonConfig);

        var adrInfo = CreateAcceptedAdrComponents("C:\\repo\\docs\\adr\\adr-0001.md");
        SetupLatestAdrSequence(adrInfo);
        _mockAdrServices.ParseFileName(Arg.Any<string>(), Arg.Any<AdrPlusRepoConfig>(), Arg.Any<IFileSystemService>())
            .Returns(adrInfo);
        SetupCreateAdrFile();

        // Act
        await _handler.ExecuteAsync(args, CancellationToken.None);

        // Assert
        _mockAdrServices.ReceivedCalls()
            .Where(c => c.GetMethodInfo().Name == nameof(IAdrServices.OpenFile))
            .Should().BeEmpty();
    }

    #endregion

    #region ExecuteAsync - Wizard Mode Tests

    [Fact]
    public async Task ExecuteAsync_WithWizardModeDriveSelectionAborted_ThrowsOperationCanceledException()
    {
        // Arrange
        var args = new[] { "--wizard" };
        var parsedArgs = new Dictionary<Arguments, string> { { Arguments.WizardReview, string.Empty } };
        var drives = TestPathData.TestDrives;

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
        var parsedArgs = new Dictionary<Arguments, string> { { Arguments.WizardReview, string.Empty } };
        var drives = new[] { TestPathData.SingleTestDrive };

        _mockAdrServices.ParseArgs(args, Arg.Any<Arguments[]>()).Returns(parsedArgs);
        _mockValidateConfig.HasTemplateRepoFile().Returns(true);
        _mockFileSystem.GetDrives().Returns(drives);
        _mockConsole.PromptSelectFolderRepositoryAdr(true, TestPathData.SingleTestDrive, _mockFileSystem, _mockValidateConfig, _config, Arg.Any<CancellationToken>())
            .Returns((true, string.Empty));

        // Act & Assert
        await _handler.Invoking(h => h.ExecuteAsync(args, CancellationToken.None))
            .Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task ExecuteAsync_WithWizardModeNoEligibleAdrs_ThrowsFileNotFoundException()
    {
        // Arrange
        var args = new[] { "--wizard" };
        var parsedArgs = new Dictionary<Arguments, string> { { Arguments.WizardReview, string.Empty } };
        var drives = new[] { TestPathData.SingleTestDrive };
        var jsonConfig = BuildJsonConfig();

        _mockAdrServices.ParseArgs(args, Arg.Any<Arguments[]>()).Returns(parsedArgs);
        _mockValidateConfig.HasTemplateRepoFile().Returns(true);
        _mockFileSystem.GetDrives().Returns(drives);
        _mockConsole.PromptSelectFolderRepositoryAdr(Arg.Any<bool>(), Arg.Any<string>(), _mockFileSystem, _mockValidateConfig, _config, Arg.Any<CancellationToken>())
            .Returns((false, TestPathData.RepositoryPath));
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
    public async Task ExecuteAsync_WithWizardModeFileSelectionAborted_ThrowsOperationCanceledException()
    {
        // Arrange
        var args = new[] { "--wizard" };
        var parsedArgs = new Dictionary<Arguments, string> { { Arguments.WizardReview, string.Empty } };
        var drives = new[] { TestPathData.SingleTestDrive };
        var jsonConfig = BuildJsonConfig();
        var eligibleAdr = CreateAcceptedAdrComponents(TestPathData.ValidAdrFilePath);

        _mockAdrServices.ParseArgs(args, Arg.Any<Arguments[]>()).Returns(parsedArgs);
        _mockValidateConfig.HasTemplateRepoFile().Returns(true);
        _mockFileSystem.GetDrives().Returns(drives);
        _mockConsole.PromptSelectFolderRepositoryAdr(Arg.Any<bool>(), Arg.Any<string>(), _mockFileSystem, _mockValidateConfig, _config, Arg.Any<CancellationToken>())
            .Returns((false, TestPathData.RepositoryPath));
        _mockValidateConfig.GetFileNameRepoConfig().Returns(".adrplus");
        _mockFileSystem.ReadAllTextAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(jsonConfig);
        _mockValidateConfig.ValidateRepoStructure(jsonConfig).Returns((true, []));
        _mockAdrServices.ReadLatestAdrFiles(_mockFileSystem, Arg.Any<string>(), Arg.Any<AdrPlusRepoConfig>())
            .Returns([eligibleAdr]);
        _mockConsole.PromptSelecLatesAdrs(Arg.Any<AdrFileNameComponents[]>(), Arg.Any<AdrPlusRepoConfig>(), Arg.Any<Func<AdrFileNameComponents, (bool, string?)>>(), Arg.Any<CancellationToken>())
            .Returns((true, null as AdrFileNameComponents));

        // Act & Assert
        await _handler.Invoking(h => h.ExecuteAsync(args, CancellationToken.None))
            .Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task ExecuteAsync_WithWizardModeDateSelectionAborted_ThrowsOperationCanceledException()
    {
        // Arrange
        var args = new[] { "--wizard" };
        var parsedArgs = new Dictionary<Arguments, string> { { Arguments.WizardReview, string.Empty } };
        var drives = new[] { TestPathData.SingleTestDrive };
        var jsonConfig = BuildJsonConfig();
        var eligibleAdr = CreateAcceptedAdrComponents("C:\\repo\\docs\\adr\\adr-0001.md");

        _mockAdrServices.ParseArgs(args, Arg.Any<Arguments[]>()).Returns(parsedArgs);
        _mockValidateConfig.HasTemplateRepoFile().Returns(true);
        _mockFileSystem.GetDrives().Returns(drives);
        _mockConsole.PromptSelectFolderRepositoryAdr(Arg.Any<bool>(), Arg.Any<string>(), _mockFileSystem, _mockValidateConfig, _config, Arg.Any<CancellationToken>())
            .Returns((false, TestPathData.RepositoryPath));
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
        var parsedArgs = new Dictionary<Arguments, string> { { Arguments.WizardReview, string.Empty } };
        var drives = new[] { TestPathData.SingleTestDrive };
        var jsonConfig = BuildJsonConfig();
        var eligibleAdr = CreateAcceptedAdrComponents(TestPathData.ValidAdrFilePath);

        _mockAdrServices.ParseArgs(args, Arg.Any<Arguments[]>()).Returns(parsedArgs);
        _mockValidateConfig.HasTemplateRepoFile().Returns(true);
        _mockFileSystem.GetDrives().Returns(drives);
        _mockConsole.PromptSelectFolderRepositoryAdr(Arg.Any<bool>(), Arg.Any<string>(), _mockFileSystem, _mockValidateConfig, _config, Arg.Any<CancellationToken>())
            .Returns((false, TestPathData.RepositoryPath));
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
        var args = new[] { "--file", "C:\\repo\\docs\\adr\\adr-0001.md" };
        var parsedArgs = new Dictionary<Arguments, string> { { Arguments.FileAdr, "C:\\repo\\docs\\adr\\adr-0001.md" } };
        var cts = new CancellationTokenSource();
        cts.Cancel();

        _mockAdrServices.ParseArgs(args, Arg.Any<Arguments[]>()).Returns(parsedArgs);
        _mockValidateConfig.HasTemplateRepoFile().Returns(true);
        _mockFileSystem.FileExists(Arg.Any<string>())
            .Returns<bool>(_ => throw new OperationCanceledException());

        // Act & Assert
        await _handler.Invoking(h => h.ExecuteAsync(args, cts.Token))
            .Should().ThrowAsync<OperationCanceledException>();
    }

    #endregion

    #region Exception Handling Tests

    [Fact]
    public async Task ExecuteAsync_WhenExceptionOccurs_LogsAndRethrows()
    {
        // Arrange
        var args = new[] { "--file", "C:\\repo\\docs\\adr\\adr-0001.md" };
        var parsedArgs = new Dictionary<Arguments, string> { { Arguments.FileAdr, "C:\\repo\\docs\\adr\\adr-0001.md" } };
        var exception = new InvalidOperationException("Unexpected error");

        _mockAdrServices.ParseArgs(args, Arg.Any<Arguments[]>()).Returns(parsedArgs);
        _mockValidateConfig.When(x => x.HasTemplateRepoFile()).Do(_ => throw exception);

        // Act & Assert
        await _handler.Invoking(h => h.ExecuteAsync(args, CancellationToken.None))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Unexpected error");
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

    private void SetupLatestAdrSequence(AdrFileNameComponents adrInfo)
    {
        _mockAdrServices.GetLatestADRSequence(Arg.Any<int>(), _mockFileSystem, Arg.Any<string>(), Arg.Any<AdrPlusRepoConfig>())
            .Returns(adrInfo);
    }

    private void SetupCreateAdrFile()
    {
        _mockFileSystem.WriteAllTextAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(System.Threading.Tasks.Task.CompletedTask);
    }

    private sealed record IsolatedMocks(
        IFileSystemService FileSystem,
        IConsoleWriter Console,
        IValidateJsonConfig ValidateConfig,
        IAdrServices AdrServices);

    private (ReviewCommandHandler Handler, IsolatedMocks Mocks) CreateIsolatedHandlerWithOpenCommand(string openCommand)
    {
        var fs = Substitute.For<IFileSystemService>();
        var console = Substitute.For<IConsoleWriter>();
        var validate = Substitute.For<IValidateJsonConfig>();
        var adrSvc = Substitute.For<IAdrServices>();
        console.GetCursorPosition().Returns((0, 0));

        var config = new AdrPlusConfig
        {
            FolderRepo = "docs/adr",
            Language = "en-US",
            ComandOpenAdr = openCommand
        };
        var handler = new ReviewCommandHandler(
            _mockLogger,
            Options.Create(config),
            fs,
            validate,
            console,
            adrSvc);

        return (handler, new IsolatedMocks(fs, console, validate, adrSvc));
    }

    private static string BuildJsonConfig() =>
        """{"Prefix": "ADR", "LenSeq": 4, "LenVersion": 2, "LenRevision": 2, "StatusNew": "Proposed", "StatusAcc": "Accepted", "StatusRej": "Rejected"}""";

    private static AdrFileNameComponents CreateAcceptedAdrComponents(string fileName) =>
        CreateAdrComponents(fileName, AdrStatus.Accepted, AdrStatus.Unknown);

    private static AdrFileNameComponents CreateAdrComponents(string fileName, AdrStatus statusUpdate, AdrStatus statusChange)
    {
        return new AdrFileNameComponents
        {
            FileName = fileName,
            IsValid = true,
            Number = 1,
            Header = new AdrHeader
            {
                IsValid = true,
                Title = "My ADR",
                StatusCreate = AdrStatus.Proposed,
                StatusUpdate = statusUpdate,
                StatusChange = statusChange,
                Version = 1,
                Revision = 0
            },
            ContentAdr = "## Context\nSome content."
        };
    }

    #endregion
}
