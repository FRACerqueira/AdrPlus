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
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace AdrPlus.Tests.Commands.Review;

/// <summary>
/// Unit tests for ReviewCommandHandler class.
/// Tests cover command execution, wizard flows, validation, and error handling using NSubstitute.
/// </summary>
public class ReviewCommandHandlerTests
{
    private readonly ILogger<ReviewCommandHandler> _mockLogger;
    private readonly IFileSystemService _mockFileSystem;
    private readonly IPromptConsole _mockConsole;
    private readonly IValidateJsonConfig _mockValidateConfig;
    private readonly IAdrServices _mockAdrServices;
    private readonly AdrPlusConfig _config;
    private readonly ReviewCommandHandler _handler;

    private const string ConfigFileName = ".adrplus";
    private const string RepoPath = "/repo";
    private const string AdrFileName = "ADR0001V01-test.md";
    private const string AdrFilePath = "/repo/adr/ADR0001V01-test.md";

    private static readonly string BasicJsonConfig =
        """{"Prefix": "ADR", "LenSeq": 4, "LenVersion": 2, "LenRevision": 1, "FolderAdr": "adr", "StatusNew": "Proposed", "StatusAcc": "Accepted", "StatusRej": "Rejected", "StatusSup": "Superseded", "template":"# ADR"}""";

    public ReviewCommandHandlerTests()
    {
        _mockLogger = Substitute.For<ILogger<ReviewCommandHandler>>();
        _mockFileSystem = Substitute.For<IFileSystemService>();
        _mockConsole = Substitute.For<IPromptConsole>();
        _mockValidateConfig = Substitute.For<IValidateJsonConfig>();
        _mockAdrServices = Substitute.For<IAdrServices>();

        _config = new AdrPlusConfig
        {
            Language = "en-US"
        };

        _mockValidateConfig.GetFileNameRepoConfig().Returns(ConfigFileName);

        _handler = new ReviewCommandHandler(
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
        _mockConsole.Received(1).PromptWriteHelp("Help text");
    }

    [Fact]
    public async Task ExecuteAsync_WithHelpArgument_DoesNotCreateAnyFile()
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
        await _mockFileSystem.DidNotReceive().WriteAllTextAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    #endregion

    #region ExecuteAsync - Template File Not Found Tests

    [Fact]
    public async Task ExecuteAsync_WhenTemplateRepoFileNotFound_ThrowsFileNotFoundException()
    {
        // Arrange
        var args = new[] { "--file", AdrFilePath };
        var parsedArgs = new Dictionary<Arguments, string>
        {
            { Arguments.FileAdr, AdrFilePath }
        };
        _mockAdrServices.ParseArgs(args, Arg.Any<Arguments[]>()).Returns(parsedArgs);
        _mockValidateConfig.HasTemplateRepoFile().Returns(false);

        // Act & Assert
        await _handler.Invoking(h => h.ExecuteAsync(args, CancellationToken.None))
            .Should().ThrowAsync<FileNotFoundException>();
    }

    #endregion

    #region ExecuteAsync - ADR File Validation Tests

    [Fact]
    public async Task ExecuteAsync_WhenAdrFileNotFound_ThrowsFileNotFoundException()
    {
        // Arrange
        var args = new[] { "--file", AdrFilePath };
        var parsedArgs = new Dictionary<Arguments, string>
        {
            { Arguments.FileAdr, AdrFilePath }
        };
        _mockAdrServices.ParseArgs(args, Arg.Any<Arguments[]>()).Returns(parsedArgs);
        _mockValidateConfig.HasTemplateRepoFile().Returns(true);
        _mockFileSystem.FileExists(AdrFilePath).Returns(false);

        // Act & Assert
        await _handler.Invoking(h => h.ExecuteAsync(args, CancellationToken.None))
            .Should().ThrowAsync<FileNotFoundException>();
    }

    [Fact]
    public async Task ExecuteAsync_WhenAdrFileHasNoExtension_AddsMarkdownExtension()
    {
        // Arrange
        var fileWithoutExt = "/repo/adr/ADR0001V01-test";
        var fileWithExt = "/repo/adr/ADR0001V01-test.md";
        var args = new[] { "--file", fileWithoutExt };
        var parsedArgs = new Dictionary<Arguments, string>
        {
            { Arguments.FileAdr, fileWithoutExt }
        };

        _mockAdrServices.ParseArgs(args, Arg.Any<Arguments[]>()).Returns(parsedArgs);
        _mockValidateConfig.HasTemplateRepoFile().Returns(true);
        _mockFileSystem.FileExists(fileWithExt).Returns(false);

        // Act & Assert
        await _handler.Invoking(h => h.ExecuteAsync(args, CancellationToken.None))
            .Should().ThrowAsync<FileNotFoundException>();
    }

    #endregion

    #region ExecuteAsync - Config Validation Tests

    [Fact]
    public async Task ExecuteAsync_WhenConfigCannotBeDetermined_ThrowsInvalidDataException()
    {
        // Arrange
        var args = new[] { "--file", AdrFilePath };
        var parsedArgs = new Dictionary<Arguments, string>
        {
            { Arguments.FileAdr, AdrFilePath }
        };
        var normalizedAdrPath = Path.GetFullPath(AdrFilePath);

        _mockAdrServices.ParseArgs(args, Arg.Any<Arguments[]>()).Returns(parsedArgs);
        _mockValidateConfig.HasTemplateRepoFile().Returns(true);
        _mockValidateConfig.GetFileNameRepoConfig().Returns(ConfigFileName);

        // FileExists MUST return true for the ADR file initially (line 137 check)
        // but return false when checking if it's a .adrplus config file
        _mockFileSystem.FileExists(Arg.Any<string>()).Returns(callInfo =>
        {
            var path = Path.GetFullPath(callInfo.Arg<string>());
            // Return true for the ADR file path, false for config paths
            if (path == normalizedAdrPath)
            {
                return true;
            }
            return path.EndsWith(".adrplus");
        });

        // Return null from GetFileRootRepositoryPath to trigger error at line 140
        _mockFileSystem.GetFileRootRepositoryPath(Arg.Any<string>()).Returns((string?)null);

        // Act & Assert
        await _handler.Invoking(h => h.ExecuteAsync(args, CancellationToken.None))
            .Should().ThrowAsync<InvalidDataException>();
    }

    [Fact]
    public async Task ExecuteAsync_WhenRepoConfigIsInvalid_ThrowsInvalidDataException()
    {
        // Arrange
        var configPath = "/repo/.adrplus";
        var args = new[] { "--file", AdrFilePath };
        var parsedArgs = new Dictionary<Arguments, string>
        {
            { Arguments.FileAdr, AdrFilePath }
        };
        var errors = new[] { "Missing Prefix field" };

        SetupMinimalMocksWithPathNormalization(parsedArgs, BasicJsonConfig, configPath);

        // Override to return invalid config
        _mockValidateConfig.ValidateRepoStructure(Arg.Any<string>()).Returns((false, errors));

        // Act & Assert
        await _handler.Invoking(h => h.ExecuteAsync(args, CancellationToken.None))
            .Should().ThrowAsync<InvalidDataException>();
    }

    [Fact]
    public async Task ExecuteAsync_WhenRepoConfigIsInvalid_WritesEachErrorToConsole()
    {
        // Arrange
        var configPath = "/repo/.adrplus";
        var args = new[] { "--file", AdrFilePath };
        var parsedArgs = new Dictionary<Arguments, string>
        {
            { Arguments.FileAdr, AdrFilePath }
        };
        var errors = new[] { "Error one", "Error two" };

        SetupMinimalMocksWithPathNormalization(parsedArgs, BasicJsonConfig, configPath);

        // Return invalid config
        _mockValidateConfig.ValidateRepoStructure(Arg.Any<string>()).Returns((false, errors));

        // Act
        await _handler.Invoking(h => h.ExecuteAsync(args, CancellationToken.None))
            .Should().ThrowAsync<InvalidDataException>();

        // Assert
        _mockConsole.Received(1).PromptWriteError("Error one");
        _mockConsole.Received(1).PromptWriteError("Error two");
    }

    [Fact]
    public async Task ExecuteAsync_WhenRevisionNotConfigured_ThrowsInvalidDataException()
    {
        // Arrange
        var configPath = "/repo/.adrplus";
        var configWithoutRevision = """{"Prefix": "ADR", "LenSeq": 4, "LenVersion": 2, "LenRevision": 0, "FolderAdr": "adr"}""";
        var args = new[] { "--file", AdrFilePath };
        var parsedArgs = new Dictionary<Arguments, string>
        {
            { Arguments.FileAdr, AdrFilePath }
        };

        SetupMinimalMocksWithPathNormalization(parsedArgs, configWithoutRevision, configPath);

        // Act & Assert
        await _handler.Invoking(h => h.ExecuteAsync(args, CancellationToken.None))
            .Should().ThrowAsync<InvalidDataException>();
    }

    #endregion

    #region ExecuteAsync - ADR Parse and Status Validation Tests

    [Fact]
    public async Task ExecuteAsync_WhenAdrFileNameCannotBeParsed_ThrowsInvalidDataException()
    {
        // Arrange
        var configPath = "/repo/.adrplus";
        var args = new[] { "--file", AdrFilePath };
        var parsedArgs = new Dictionary<Arguments, string>
        {
            { Arguments.FileAdr, AdrFilePath }
        };

        SetupMinimalMocksWithPathNormalization(parsedArgs, BasicJsonConfig, configPath);

        // Override ParseFileName to return an invalid ADR - must be AFTER SetupMinimalMocks
        var invalidAdr = CommandHandlerMockHelper.CreateInvalidAdrFileNameComponents("Invalid file name");
        _mockAdrServices.ParseFileName(AdrFilePath, Arg.Any<AdrPlusRepoConfig>(), _mockFileSystem).ReturnsForAnyArgs(Task.FromResult(invalidAdr));

        // Act & Assert
        await _handler.Invoking(h => h.ExecuteAsync(args, CancellationToken.None))
            .Should().ThrowAsync<InvalidDataException>();
    }

    [Fact]
    public async Task ExecuteAsync_WhenAdrHeaderIsInvalid_ThrowsInvalidDataException()
    {
        // Arrange
        var configPath = "/repo/.adrplus";
        var args = new[] { "--file", AdrFilePath };
        var parsedArgs = new Dictionary<Arguments, string>
        {
            { Arguments.FileAdr, AdrFilePath }
        };

        SetupMinimalMocksWithPathNormalization(parsedArgs, BasicJsonConfig, configPath);

        var invalidHeader = new AdrHeader { IsValid = false, ErrorMessage = "Invalid header" };
        var infoadr = new AdrFileNameComponents
        {
            FileName = AdrFileName,
            Number = 1,
            IsValid = true,
            Header = invalidHeader
        };

        _mockAdrServices.ParseFileName(AdrFilePath, Arg.Any<AdrPlusRepoConfig>(), _mockFileSystem)
            .ReturnsForAnyArgs(infoadr);

        // Act & Assert
        await _handler.Invoking(h => h.ExecuteAsync(args, CancellationToken.None))
            .Should().ThrowAsync<InvalidDataException>();
    }

    [Fact]
    public async Task ExecuteAsync_WhenAdrStatusNotAcceptedOrRejected_ThrowsInvalidDataException()
    {
        // Arrange
        var configPath = "/repo/.adrplus";
        var args = new[] { "--file", AdrFilePath };
        var parsedArgs = new Dictionary<Arguments, string>
        {
            { Arguments.FileAdr, AdrFilePath }
        };

        SetupMinimalMocksWithPathNormalization(parsedArgs, BasicJsonConfig, configPath);

        var infoadr = new AdrFileNameComponents
        {
            FileName = AdrFileName,
            Number = 1,
            IsValid = true,
            Revision = 1,
            Title = "test",
            Header = new AdrHeader
            {
                IsValid = true,
                StatusUpdate = AdrStatus.Proposed,  // This should fail - not Accepted or Rejected
                StatusCreate = AdrStatus.Proposed,
                StatusChange = AdrStatus.Unknown,
                Title = "Test ADR"
            },
            ContentAdr = "Test content"
        };

        _mockAdrServices.ParseFileName(AdrFilePath, Arg.Any<AdrPlusRepoConfig>(), _mockFileSystem)
            .ReturnsForAnyArgs(Task.FromResult(infoadr));

        // Return empty list so it proceeds to validation
        _mockAdrServices.ReadAllAdrByNumber(1, _mockFileSystem, RepoPath, Arg.Any<AdrPlusRepoConfig>())
            .ReturnsForAnyArgs(Task.FromResult(Array.Empty<AdrFileNameComponents>()));

        // Act & Assert
        await _handler.Invoking(h => h.ExecuteAsync(args, CancellationToken.None))
            .Should().ThrowAsync<InvalidDataException>();
    }

    [Fact]
    public async Task ExecuteAsync_WhenAdrHasSupersededStatus_ThrowsInvalidDataException()
    {
        // Arrange - minimal setup without SetupBasicMocks
        var configPath = "/repo/.adrplus";
        var args = new[] { "--file", AdrFilePath };
        var parsedArgs = new Dictionary<Arguments, string>
        {
            { Arguments.FileAdr, AdrFilePath }
        };

        SetupMinimalMocksWithPathNormalization(parsedArgs, BasicJsonConfig, configPath);

        var infoadr = CommandHandlerMockHelper.CreateValidAdrFileNameComponents(
            AdrFileName,
            AdrStatus.Accepted);
        infoadr.Number = 1;

        _mockAdrServices.ParseFileName(AdrFilePath, Arg.Any<AdrPlusRepoConfig>(), _mockFileSystem)
            .Returns(Task.FromResult(infoadr));
        var supersededAdr = CommandHandlerMockHelper.CreateValidAdrFileNameComponents(AdrFileName, AdrStatus.Accepted);
        supersededAdr.Header.StatusChange = AdrStatus.Superseded;
        _mockAdrServices.ReadAllAdrByNumber(1, _mockFileSystem, RepoPath, Arg.Any<AdrPlusRepoConfig>())
            .Returns(Task.FromResult(new[] { supersededAdr }));

        // Act & Assert
        await _handler.Invoking(h => h.ExecuteAsync(args, CancellationToken.None))
            .Should().ThrowAsync<InvalidDataException>();
    }

    [Fact]
    public async Task ExecuteAsync_WhenAdrHasUnknownStatusNotMigrated_ThrowsInvalidDataException()
    {
        // Arrange - minimal setup without SetupBasicMocks
        var configPath = "/repo/.adrplus";
        var args = new[] { "--file", AdrFilePath };
        var parsedArgs = new Dictionary<Arguments, string>
        {
            { Arguments.FileAdr, AdrFilePath }
        };

        SetupMinimalMocksWithPathNormalization(parsedArgs, BasicJsonConfig, configPath);

        var infoadr = CommandHandlerMockHelper.CreateValidAdrFileNameComponents(
            AdrFileName,
            AdrStatus.Accepted);
        infoadr.Number = 1;

        _mockAdrServices.ParseFileName(AdrFilePath, Arg.Any<AdrPlusRepoConfig>(), _mockFileSystem)
            .Returns(Task.FromResult(infoadr));
        var unknownStatusAdr = CommandHandlerMockHelper.CreateValidAdrFileNameComponents(AdrFileName, AdrStatus.Accepted);
        unknownStatusAdr.Header.StatusUpdate = AdrStatus.Unknown;
        unknownStatusAdr.Header.IsMigrated = false;
        unknownStatusAdr.Header.StatusChange = AdrStatus.Unknown;
        _mockAdrServices.ReadAllAdrByNumber(1, _mockFileSystem, RepoPath, Arg.Any<AdrPlusRepoConfig>())
            .Returns(Task.FromResult(new[] { unknownStatusAdr }));

        // Act & Assert
        await _handler.Invoking(h => h.ExecuteAsync(args, CancellationToken.None))
            .Should().ThrowAsync<InvalidDataException>();
    }

    #endregion

    #region ExecuteAsync - Latest Version Validation Tests

    [Fact]
    public async Task ExecuteAsync_WhenLatestAdrNotFound_ThrowsInvalidDataException()
    {
        // Arrange - minimal setup without SetupBasicMocks
        var configPath = "/repo/.adrplus";
        var args = new[] { "--file", AdrFilePath };
        var parsedArgs = new Dictionary<Arguments, string>
        {
            { Arguments.FileAdr, AdrFilePath }
        };

        SetupMinimalMocksWithPathNormalization(parsedArgs, BasicJsonConfig, configPath);

        var infoadr = ReviewCommandHandlerTests.CreateTestAdrFileNameComponents(AdrFileName, AdrStatus.Accepted, number: 1);

        _mockAdrServices.ParseFileName(AdrFilePath, Arg.Any<AdrPlusRepoConfig>(), _mockFileSystem)
            .Returns(Task.FromResult(infoadr));
        _mockAdrServices.ReadAllAdrByNumber(1, _mockFileSystem, RepoPath, Arg.Any<AdrPlusRepoConfig>())
            .Returns(Task.FromResult(Array.Empty<AdrFileNameComponents>()));
        var invalidLatest = new AdrFileNameComponents { IsValid = false, ErrorMessage = "Not found" };
        _mockAdrServices.GetLatestADRSequence(1, _mockFileSystem, RepoPath, Arg.Any<AdrPlusRepoConfig>())
            .Returns(Task.FromResult<AdrFileNameComponents?>(invalidLatest));

        // Act & Assert
        await _handler.Invoking(h => h.ExecuteAsync(args, CancellationToken.None))
            .Should().ThrowAsync<InvalidDataException>();
    }

    [Fact]
    public async Task ExecuteAsync_WhenLatestAdrHeaderInvalid_ThrowsInvalidDataException()
    {
        // Arrange - minimal setup without SetupBasicMocks
        var configPath = "/repo/.adrplus";
        var args = new[] { "--file", AdrFilePath };
        var parsedArgs = new Dictionary<Arguments, string>
        {
            { Arguments.FileAdr, AdrFilePath }
        };

        SetupMinimalMocksWithPathNormalization(parsedArgs, BasicJsonConfig, configPath);

        var infoadr = ReviewCommandHandlerTests.CreateTestAdrFileNameComponents(AdrFileName, AdrStatus.Accepted, number: 1);

        _mockAdrServices.ParseFileName(AdrFilePath, Arg.Any<AdrPlusRepoConfig>(), _mockFileSystem)
            .Returns(Task.FromResult(infoadr));
        _mockAdrServices.ReadAllAdrByNumber(1, _mockFileSystem, RepoPath, Arg.Any<AdrPlusRepoConfig>())
            .Returns(Task.FromResult(Array.Empty<AdrFileNameComponents>()));
        var latestWithInvalidHeader = CommandHandlerMockHelper.CreateValidAdrFileNameComponents(AdrFileName, AdrStatus.Accepted);
        latestWithInvalidHeader.Header.IsValid = false;
        latestWithInvalidHeader.Header.ErrorMessage = "Invalid header";
        _mockAdrServices.GetLatestADRSequence(1, _mockFileSystem, RepoPath, Arg.Any<AdrPlusRepoConfig>())
            .Returns(Task.FromResult<AdrFileNameComponents?>(latestWithInvalidHeader));

        // Act & Assert
        await _handler.Invoking(h => h.ExecuteAsync(args, CancellationToken.None))
            .Should().ThrowAsync<InvalidDataException>();
    }

    [Fact]
    public async Task ExecuteAsync_WhenLatestAdrNotSameSequence_ThrowsInvalidOperationException()
    {
        // Arrange - minimal setup without SetupBasicMocks
        var configPath = "/repo/.adrplus";
        var args = new[] { "--file", AdrFilePath };
        var parsedArgs = new Dictionary<Arguments, string>
        {
            { Arguments.FileAdr, AdrFilePath }
        };

        SetupMinimalMocksWithPathNormalization(parsedArgs, BasicJsonConfig, configPath);

        var infoadr = ReviewCommandHandlerTests.CreateTestAdrFileNameComponents(AdrFileName, AdrStatus.Accepted, number: 1);

        _mockAdrServices.ParseFileName(AdrFilePath, Arg.Any<AdrPlusRepoConfig>(), _mockFileSystem)
            .Returns(Task.FromResult(infoadr));
        _mockAdrServices.ReadAllAdrByNumber(1, _mockFileSystem, RepoPath, Arg.Any<AdrPlusRepoConfig>())
            .Returns(Task.FromResult(Array.Empty<AdrFileNameComponents>()));
        var latestDifferent = CommandHandlerMockHelper.CreateValidAdrFileNameComponents("ADR-0002-other.md", AdrStatus.Accepted);
        latestDifferent.Number = 2;
        latestDifferent.Title = "other";
        _mockAdrServices.GetLatestADRSequence(1, _mockFileSystem, RepoPath, Arg.Any<AdrPlusRepoConfig>())
            .Returns(Task.FromResult<AdrFileNameComponents?>(latestDifferent));

        // Act & Assert
        await _handler.Invoking(h => h.ExecuteAsync(args, CancellationToken.None))
            .Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task ExecuteAsync_WhenNotLatestVersion_ThrowsInvalidOperationException()
    {
        // Arrange - minimal setup without SetupBasicMocks
        var configPath = "/repo/.adrplus";
        var args = new[] { "--file", AdrFilePath };
        var parsedArgs = new Dictionary<Arguments, string>
        {
            { Arguments.FileAdr, AdrFilePath }
        };

        SetupMinimalMocksWithPathNormalization(parsedArgs, BasicJsonConfig, configPath);

        var infoadr = ReviewCommandHandlerTests.CreateTestAdrFileNameComponents(AdrFileName, AdrStatus.Accepted, number: 1, revision: 1);

        _mockAdrServices.ParseFileName(AdrFilePath, Arg.Any<AdrPlusRepoConfig>(), _mockFileSystem)
            .Returns(Task.FromResult(infoadr));
        _mockAdrServices.ReadAllAdrByNumber(1, _mockFileSystem, RepoPath, Arg.Any<AdrPlusRepoConfig>())
            .Returns(Task.FromResult(Array.Empty<AdrFileNameComponents>()));
        var latestNewer = CommandHandlerMockHelper.CreateValidAdrFileNameComponents("ADR-0001-v1-r2.md", AdrStatus.Accepted);
        latestNewer.Number = 1;
        latestNewer.Revision = 2;
        _mockAdrServices.GetLatestADRSequence(1, _mockFileSystem, RepoPath, Arg.Any<AdrPlusRepoConfig>())
            .Returns(Task.FromResult<AdrFileNameComponents?>(latestNewer));

        // Act & Assert
        await _handler.Invoking(h => h.ExecuteAsync(args, CancellationToken.None))
            .Should().ThrowAsync<InvalidOperationException>();
    }

    #endregion

    #region ExecuteAsync - Successful Creation Tests

    [Fact]
    public async Task ExecuteAsync_WithValidArgs_WritesAdrFileToFileSystem()
    {
        // Arrange
        var configPath = "/repo/.adrplus";
        var args = new[] { "--file", AdrFilePath };
        var parsedArgs = new Dictionary<Arguments, string>
        {
            { Arguments.FileAdr, AdrFilePath }
        };

        SetupBasicMocks(parsedArgs, BasicJsonConfig, configPath);
        var infoadr = CommandHandlerMockHelper.CreateCompleteAdrFileNameComponents(
            AdrFileName, AdrStatus.Accepted, number: 1, version: 1, revision: 1, title: "test", domain: "");

        _mockAdrServices.ParseFileName(AdrFilePath, Arg.Any<AdrPlusRepoConfig>(), _mockFileSystem)
            .Returns(Task.FromResult(infoadr));
        _mockAdrServices.ReadAllAdrByNumber(1, _mockFileSystem, RepoPath, Arg.Any<AdrPlusRepoConfig>())
            .Returns(Task.FromResult(Array.Empty<AdrFileNameComponents>()));
        _mockAdrServices.GetLatestADRSequence(1, _mockFileSystem, RepoPath, Arg.Any<AdrPlusRepoConfig>())
            .Returns(Task.FromResult<AdrFileNameComponents?>(infoadr));
        // Act
        await _handler.ExecuteAsync(args, CancellationToken.None);

        // Assert
        await _mockFileSystem.Received(1).WriteAllTextAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WithValidArgs_WritesSuccessMessageToConsole()
    {
        // Arrange
        var configPath = "/repo/.adrplus";
        var args = new[] { "--file", AdrFilePath };
        var parsedArgs = new Dictionary<Arguments, string>
        {
            { Arguments.FileAdr, AdrFilePath }
        };

        SetupBasicMocks(parsedArgs, BasicJsonConfig, configPath);
        var infoadr = CommandHandlerMockHelper.CreateCompleteAdrFileNameComponents(
            AdrFileName, AdrStatus.Accepted, number: 1, version: 1, revision: 1, title: "test", domain: "");

        _mockAdrServices.ParseFileName(AdrFilePath, Arg.Any<AdrPlusRepoConfig>(), _mockFileSystem)
            .Returns(Task.FromResult(infoadr));
        _mockAdrServices.ReadAllAdrByNumber(1, _mockFileSystem, RepoPath, Arg.Any<AdrPlusRepoConfig>())
            .Returns(Task.FromResult(Array.Empty<AdrFileNameComponents>()));
        _mockAdrServices.GetLatestADRSequence(1, _mockFileSystem, RepoPath, Arg.Any<AdrPlusRepoConfig>())
            .Returns(Task.FromResult<AdrFileNameComponents?>(infoadr));

        // Act
        await _handler.ExecuteAsync(args, CancellationToken.None);

        // Assert
        _mockConsole.Received(1).PromptWriteSuccess(Arg.Any<string>());
    }

    [Fact]
    public async Task ExecuteAsync_WithValidAcceptedAdr_CreatesNewRevision()
    {
        // Arrange
        var configPath = "/repo/.adrplus";
        var args = new[] { "--file", AdrFilePath };
        var parsedArgs = new Dictionary<Arguments, string>
        {
            { Arguments.FileAdr, AdrFilePath }
        };

        SetupBasicMocks(parsedArgs, BasicJsonConfig, configPath);
        var infoadr = CommandHandlerMockHelper.CreateCompleteAdrFileNameComponents(
            AdrFileName, AdrStatus.Accepted, number: 1, version: 1, revision: 1, title: "test", domain: "");

        _mockAdrServices.ParseFileName(AdrFilePath, Arg.Any<AdrPlusRepoConfig>(), _mockFileSystem)
            .Returns(Task.FromResult(infoadr));
        _mockAdrServices.ReadAllAdrByNumber(1, _mockFileSystem, RepoPath, Arg.Any<AdrPlusRepoConfig>())
            .Returns(Task.FromResult(Array.Empty<AdrFileNameComponents>()));
        _mockAdrServices.GetLatestADRSequence(1, _mockFileSystem, RepoPath, Arg.Any<AdrPlusRepoConfig>())
            .Returns(Task.FromResult<AdrFileNameComponents?>(infoadr));

        // Act
        await _handler.ExecuteAsync(args, CancellationToken.None);

        // Assert
        await _mockFileSystem.Received(1).WriteAllTextAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WithValidRejectedAdr_CreatesNewRevision()
    {
        // Arrange
        var configPath = "/repo/.adrplus";
        var args = new[] { "--file", AdrFilePath };
        var parsedArgs = new Dictionary<Arguments, string>
        {
            { Arguments.FileAdr, AdrFilePath }
        };

        SetupBasicMocks(parsedArgs, BasicJsonConfig, configPath);
        var infoadr = CommandHandlerMockHelper.CreateCompleteAdrFileNameComponents(
            AdrFileName, AdrStatus.Rejected, number: 1, version: 1, revision: 1, title: "test", domain: "");

        _mockAdrServices.ParseFileName(AdrFilePath, Arg.Any<AdrPlusRepoConfig>(), _mockFileSystem)
            .Returns(Task.FromResult(infoadr));
        _mockAdrServices.ReadAllAdrByNumber(1, _mockFileSystem, RepoPath, Arg.Any<AdrPlusRepoConfig>())
            .Returns(Task.FromResult(Array.Empty<AdrFileNameComponents>()));
        _mockAdrServices.GetLatestADRSequence(1, _mockFileSystem, RepoPath, Arg.Any<AdrPlusRepoConfig>())
            .Returns(Task.FromResult<AdrFileNameComponents?>(infoadr));

        // Act
        await _handler.ExecuteAsync(args, CancellationToken.None);

        // Assert
        await _mockFileSystem.Received(1).WriteAllTextAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    #endregion

    #region ExecuteAsync - Date Parsing Tests

    [Fact]
    public async Task ExecuteAsync_WithCustomDate_PassesDateToFileCreation()
    {
        // Arrange
        var customDate = "2026-01-15";
        var configPath = "/repo/.adrplus";
        var args = new[] { "--file", AdrFilePath, "--refdate", customDate };
        var parsedArgs = new Dictionary<Arguments, string>
        {
            { Arguments.FileAdr, AdrFilePath },
            { Arguments.DateRefAdr, customDate }
        };

        SetupBasicMocks(parsedArgs, BasicJsonConfig, configPath);
        var infoadr = CommandHandlerMockHelper.CreateCompleteAdrFileNameComponents(
            AdrFileName, AdrStatus.Accepted, number: 1, version: 1, revision: 1, title: "test", domain: "");

        _mockAdrServices.ParseFileName(AdrFilePath, Arg.Any<AdrPlusRepoConfig>(), _mockFileSystem)
            .Returns(Task.FromResult(infoadr));
        _mockAdrServices.ReadAllAdrByNumber(1, _mockFileSystem, RepoPath, Arg.Any<AdrPlusRepoConfig>())
            .Returns(Task.FromResult(Array.Empty<AdrFileNameComponents>()));
        _mockAdrServices.GetLatestADRSequence(1, _mockFileSystem, RepoPath, Arg.Any<AdrPlusRepoConfig>())
            .Returns(Task.FromResult<AdrFileNameComponents?>(infoadr));

        string? capturedContent = null;
        _mockFileSystem.WriteAllTextAsync(Arg.Any<string>(), Arg.Do<string>(c => capturedContent = c), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        // Act
        await _handler.ExecuteAsync(args, CancellationToken.None);

        // Assert: file was written (date used during AdrRecord creation)
        capturedContent.Should().NotBeNull();
        capturedContent.Should().Contain("2026");
    }

    [Fact]
    public async Task ExecuteAsync_WithInvalidDateFormat_ThrowsFormatException()
    {
        // Arrange
        var configPath = "/repo/.adrplus";
        var args = new[] { "--file", AdrFilePath, "--refdate", "not-a-date" };
        var parsedArgs = new Dictionary<Arguments, string>
        {
            { Arguments.FileAdr, AdrFilePath },
            { Arguments.DateRefAdr, "not-a-date" }
        };

        SetupBasicMocks(parsedArgs, BasicJsonConfig, configPath);
        var infoadr = CommandHandlerMockHelper.CreateCompleteAdrFileNameComponents(
            AdrFileName, AdrStatus.Accepted, number: 1, version: 1, revision: 1, title: "test", domain: "");

        _mockAdrServices.ParseFileName(AdrFilePath, Arg.Any<AdrPlusRepoConfig>(), _mockFileSystem)
            .Returns(Task.FromResult(infoadr));
        _mockAdrServices.ReadAllAdrByNumber(1, _mockFileSystem, RepoPath, Arg.Any<AdrPlusRepoConfig>())
            .Returns(Task.FromResult(Array.Empty<AdrFileNameComponents>()));
         _mockAdrServices.GetLatestADRSequence(1, _mockFileSystem, RepoPath, Arg.Any<AdrPlusRepoConfig>())
            .Returns(Task.FromResult<AdrFileNameComponents?>(infoadr));

        // Act & Assert
        await _handler.Invoking(h => h.ExecuteAsync(args, CancellationToken.None))
            .Should().ThrowAsync<FormatException>();
    }

    [Fact]
    public async Task ExecuteAsync_WithNoDateArgument_UsesCurrentDate()
    {
        // Arrange
        var configPath = "/repo/.adrplus";
        var args = new[] { "--file", AdrFilePath };
        var parsedArgs = new Dictionary<Arguments, string>
        {
            { Arguments.FileAdr, AdrFilePath }
        };

        SetupBasicMocks(parsedArgs, BasicJsonConfig, configPath);
        var infoadr = CommandHandlerMockHelper.CreateCompleteAdrFileNameComponents(
            AdrFileName, AdrStatus.Accepted, number: 1, version: 1, revision: 1, title: "test", domain: "");

        _mockAdrServices.ParseFileName(AdrFilePath, Arg.Any<AdrPlusRepoConfig>(), _mockFileSystem)
            .Returns(Task.FromResult(infoadr));
        _mockAdrServices.ReadAllAdrByNumber(1, _mockFileSystem, RepoPath, Arg.Any<AdrPlusRepoConfig>())
            .Returns(Task.FromResult(Array.Empty<AdrFileNameComponents>()));
        _mockAdrServices.GetLatestADRSequence(1, _mockFileSystem, RepoPath, Arg.Any<AdrPlusRepoConfig>())
            .Returns(Task.FromResult<AdrFileNameComponents?>(infoadr));

        var beforeCall = DateTime.UtcNow.Year;

        // Act
        await _handler.ExecuteAsync(args, CancellationToken.None);

        // Assert: file write occurred, meaning UTC now was used without throwing
        await _mockFileSystem.Received(1).WriteAllTextAsync(
            Arg.Any<string>(),
            Arg.Is<string>(c => c.Contains(beforeCall.ToString())),
            Arg.Any<CancellationToken>());
    }

    #endregion

    #region ExecuteAsync - Open File Tests

    [Fact]
    public async Task ExecuteAsync_WithOpenArgAndConfiguredCommand_OpensCreatedFile()
    {
        // Arrange
        var configWithCommand = new AdrPlusConfig
        {
            Language = "en-US",
            ComandOpenAdr = "code {0}"
        };
        var handler = CreateHandlerWith(configWithCommand);

        var configPath = "/repo/.adrplus";
        var args = new[] { "--file", AdrFilePath, "--open" };
        var parsedArgs = new Dictionary<Arguments, string>
        {
            { Arguments.FileAdr, AdrFilePath },
            { Arguments.OpenFile, string.Empty }
        };

        SetupBasicMocks(parsedArgs, BasicJsonConfig, configPath);
        var infoadr = CommandHandlerMockHelper.CreateCompleteAdrFileNameComponents(
            AdrFileName, AdrStatus.Accepted, number: 1, version: 1, revision: 1, title: "test", domain: "");

        _mockAdrServices.ParseFileName(AdrFilePath, Arg.Any<AdrPlusRepoConfig>(), _mockFileSystem)
            .Returns(Task.FromResult(infoadr));
        _mockAdrServices.ReadAllAdrByNumber(1, _mockFileSystem, RepoPath, Arg.Any<AdrPlusRepoConfig>())
            .Returns(Task.FromResult(Array.Empty<AdrFileNameComponents>()));
        _mockAdrServices.GetLatestADRSequence(1, _mockFileSystem, RepoPath, Arg.Any<AdrPlusRepoConfig>())
            .Returns(Task.FromResult<AdrFileNameComponents?>(infoadr));
        _mockAdrServices.OpenFile(Arg.Any<string>(), Arg.Any<string>()).Returns(string.Empty);

        // Act
        await handler.ExecuteAsync(args, CancellationToken.None);

        // Assert
        _mockAdrServices.Received(1).OpenFile(Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public async Task ExecuteAsync_WhenOpenFileSucceeds_WritesSuccessMessage()
    {
        // Arrange
        var configWithCommand = new AdrPlusConfig
        {
            Language = "en-US",
            ComandOpenAdr = "code {0}"
        };
        var handler = CreateHandlerWith(configWithCommand);

        var configPath = "/repo/.adrplus";
        var args = new[] { "--file", AdrFilePath, "--open" };
        var parsedArgs = new Dictionary<Arguments, string>
        {
            { Arguments.FileAdr, AdrFilePath },
            { Arguments.OpenFile, string.Empty }
        };

        SetupBasicMocks(parsedArgs, BasicJsonConfig, configPath);
        var infoadr = CommandHandlerMockHelper.CreateCompleteAdrFileNameComponents(
            AdrFileName, AdrStatus.Accepted, number: 1, version: 1, revision: 1, title: "test", domain: "");

        _mockAdrServices.ParseFileName(AdrFilePath, Arg.Any<AdrPlusRepoConfig>(), _mockFileSystem)
            .Returns(Task.FromResult(infoadr));
        _mockAdrServices.ReadAllAdrByNumber(1, _mockFileSystem, RepoPath, Arg.Any<AdrPlusRepoConfig>())
            .Returns(Task.FromResult(Array.Empty<AdrFileNameComponents>()));
        _mockAdrServices.GetLatestADRSequence(1, _mockFileSystem, RepoPath, Arg.Any<AdrPlusRepoConfig>())
            .Returns(Task.FromResult<AdrFileNameComponents?>(infoadr));
        _mockAdrServices.OpenFile(Arg.Any<string>(), Arg.Any<string>()).Returns(string.Empty);

        // Act
        await handler.ExecuteAsync(args, CancellationToken.None);

        // Assert: one success for ADR created, one for open success
        _mockConsole.Received(2).PromptWriteSuccess(Arg.Any<string>());
    }

    [Fact]
    public async Task ExecuteAsync_WhenOpenFileFails_WritesErrorMessage()
    {
        // Arrange
        var configWithCommand = new AdrPlusConfig
        {
            ComandOpenAdr = "code {0}"
        };
        var handler = CreateHandlerWith(configWithCommand);

        var configPath = "/repo/.adrplus";
        var args = new[] { "--file", AdrFilePath, "--open" };
        var parsedArgs = new Dictionary<Arguments, string>
        {
            { Arguments.FileAdr, AdrFilePath },
            { Arguments.OpenFile, string.Empty }
        };

        SetupBasicMocks(parsedArgs, BasicJsonConfig, configPath);
        var infoadr = CommandHandlerMockHelper.CreateCompleteAdrFileNameComponents(
            AdrFileName, AdrStatus.Accepted, number: 1, version: 1, revision: 1, title: "test", domain: "");

        _mockAdrServices.ParseFileName(AdrFilePath, Arg.Any<AdrPlusRepoConfig>(), _mockFileSystem)
            .Returns(Task.FromResult(infoadr));
        _mockAdrServices.ReadAllAdrByNumber(1, _mockFileSystem, RepoPath, Arg.Any<AdrPlusRepoConfig>())
            .Returns(Task.FromResult(Array.Empty<AdrFileNameComponents>()));
        _mockAdrServices.GetLatestADRSequence(1, _mockFileSystem, RepoPath, Arg.Any<AdrPlusRepoConfig>())
            .Returns(Task.FromResult<AdrFileNameComponents?>(infoadr));
        _mockAdrServices.OpenFile(Arg.Any<string>(), Arg.Any<string>()).Returns("open command failed");

        // Act
        await handler.ExecuteAsync(args, CancellationToken.None);

        // Assert
        _mockConsole.Received(1).PromptWriteError(Arg.Any<string>());
    }

    [Fact]
    public async Task ExecuteAsync_WithOpenArgButNoCommandConfigured_DoesNotCallOpenFile()
    {
        // Arrange - config with no ComandOpenAdr
        var configPath = "/repo/.adrplus";
        var args = new[] { "--file", AdrFilePath };
        var parsedArgs = new Dictionary<Arguments, string>
        {
            { Arguments.FileAdr, AdrFilePath }
        };

        SetupBasicMocks(parsedArgs, BasicJsonConfig, configPath);
        var infoadr = ReviewCommandHandlerTests.CreateTestAdrFileNameComponents(AdrFileName, AdrStatus.Accepted, number: 1, revision: 1);

        _mockAdrServices.ParseFileName(AdrFilePath, Arg.Any<AdrPlusRepoConfig>(), _mockFileSystem)
            .Returns(Task.FromResult(infoadr));
        _mockAdrServices.ReadAllAdrByNumber(1, _mockFileSystem, RepoPath, Arg.Any<AdrPlusRepoConfig>())
            .Returns(Task.FromResult(Array.Empty<AdrFileNameComponents>()));
        _mockAdrServices.GetLatestADRSequence(1, _mockFileSystem, RepoPath, Arg.Any<AdrPlusRepoConfig>())
            .Returns(Task.FromResult<AdrFileNameComponents?>(infoadr));

        // Act
        await _handler.ExecuteAsync(args, CancellationToken.None);

        // Assert
        _mockAdrServices.DidNotReceive().OpenFile(Arg.Any<string>(), Arg.Any<string>());
    }

    #endregion

    #region ExecuteAsync - Empty Template Tests

    [Fact]
    public async Task ExecuteAsync_WithEmptyTemplateArg_UsesEmptyTemplate()
    {
        // Arrange
        var configPath = "/repo/.adrplus";
        var args = new[] { "--file", AdrFilePath, "--empty" };
        var parsedArgs = new Dictionary<Arguments, string>
        {
            { Arguments.FileAdr, AdrFilePath },
            { Arguments.EmptyAdr, string.Empty }
        };

        SetupBasicMocks(parsedArgs, BasicJsonConfig, configPath);
        var infoadr = CommandHandlerMockHelper.CreateCompleteAdrFileNameComponents(
            AdrFileName, AdrStatus.Accepted, number: 1, version: 1, revision: 1, title: "test", domain: "");

        _mockAdrServices.ParseFileName(AdrFilePath, Arg.Any<AdrPlusRepoConfig>(), _mockFileSystem)
            .Returns(Task.FromResult(infoadr));
        _mockAdrServices.ReadAllAdrByNumber(1, _mockFileSystem, RepoPath, Arg.Any<AdrPlusRepoConfig>())
            .Returns(Task.FromResult(Array.Empty<AdrFileNameComponents>()));
        _mockAdrServices.GetLatestADRSequence(1, _mockFileSystem, RepoPath, Arg.Any<AdrPlusRepoConfig>())
            .Returns(Task.FromResult<AdrFileNameComponents?>(infoadr));

        string? capturedContent = null;
        _mockFileSystem.WriteAllTextAsync(Arg.Any<string>(), Arg.Do<string>(c => capturedContent = c), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        // Act
        await _handler.ExecuteAsync(args, CancellationToken.None);

        // Assert
        capturedContent.Should().NotBeNull();
        capturedContent.Should().Contain("# ADR");
    }

    [Fact]
    public async Task ExecuteAsync_WithoutEmptyTemplateArg_UsesOriginalContent()
    {
        // Arrange
        var configPath = "/repo/.adrplus";
        var args = new[] { "--file", AdrFilePath };
        var parsedArgs = new Dictionary<Arguments, string>
        {
            { Arguments.FileAdr, AdrFilePath }
        };

        var infoadr = ReviewCommandHandlerTests.CreateTestAdrFileNameComponents(AdrFileName, AdrStatus.Accepted, number: 1, revision: 1);
        infoadr.ContentAdr = "Test content from file";

        // Setup mocks directly to avoid conflicts with SetupBasicMocks
        SetupMinimalMocksWithPathNormalization(parsedArgs, BasicJsonConfig, configPath);

        // Override the default ADR setup with test-specific ADR
        _mockAdrServices.ParseFileName(Arg.Any<string>(), Arg.Any<AdrPlusRepoConfig>(), Arg.Any<IFileSystemService>())
            .ReturnsForAnyArgs(Task.FromResult(infoadr));
        _mockAdrServices.ReadAllAdrByNumber(Arg.Any<int>(), Arg.Any<IFileSystemService>(), Arg.Any<string>(), Arg.Any<AdrPlusRepoConfig>())
            .ReturnsForAnyArgs(Task.FromResult(Array.Empty<AdrFileNameComponents>()));
        _mockAdrServices.GetLatestADRSequence(Arg.Any<int>(), Arg.Any<IFileSystemService>(), Arg.Any<string>(), Arg.Any<AdrPlusRepoConfig>())
            .ReturnsForAnyArgs(Task.FromResult<AdrFileNameComponents?>(infoadr));

        string? capturedContent = null;
        _mockFileSystem.WriteAllTextAsync(Arg.Any<string>(), Arg.Do<string>(c => capturedContent = c), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        // Act
        await _handler.ExecuteAsync(args, CancellationToken.None);

        // Assert
        capturedContent.Should().NotBeNull();
        // The handler generates header + content; check that original content is included
        capturedContent.Should().Contain("Test content from file");
    }

    #endregion

    #region ExecuteAsync - Exception Handling Tests

    [Fact]
    public async Task ExecuteAsync_WhenExceptionOccurs_RethrowsException()
    {
        // Arrange
        var args = new[] { "--file", AdrFilePath };
        var exception = new InvalidOperationException("Unexpected error");
        _mockAdrServices.ParseArgs(args, Arg.Any<Arguments[]>())
            .Throws(exception);

        // Act & Assert
        await _handler.Invoking(h => h.ExecuteAsync(args, CancellationToken.None))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Unexpected error");
    }

    #endregion

    #region ExecuteAsync - Wizard Mode Tests

    [Fact]
    public async Task ExecuteAsync_WithWizardMode_DriveSelectionAborted_ThrowsOperationCanceledException()
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
    public async Task ExecuteAsync_WithWizardMode_FolderSelectionAborted_ThrowsOperationCanceledException()
    {
        // Arrange
        var args = new[] { "--wizard" };
        var parsedArgs = new Dictionary<Arguments, string> { { Arguments.WizardReview, string.Empty } };
        var drives = new[] { TestPathData.SingleTestDrive };

        _mockAdrServices.ParseArgs(args, Arg.Any<Arguments[]>()).Returns(parsedArgs);
        _mockValidateConfig.HasTemplateRepoFile().Returns(true);
        _mockFileSystem.GetDrives().Returns(drives);
        _mockConsole.PromptSelectFolderPath(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<string>(), _mockFileSystem, _mockValidateConfig, Arg.Any<CancellationToken>())
            .Returns((true, string.Empty));

        // Act & Assert
        await _handler.Invoking(h => h.ExecuteAsync(args, CancellationToken.None))
            .Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task ExecuteAsync_WithWizardMode_AdrSelectionAborted_ThrowsOperationCanceledException()
    {
        // Arrange
        var args = new[] { "--wizard" };
        var parsedArgs = new Dictionary<Arguments, string> { { Arguments.WizardReview, string.Empty } };
        var drives = new[] { TestPathData.SingleTestDrive };

        _mockAdrServices.ParseArgs(args, Arg.Any<Arguments[]>()).Returns(parsedArgs);
        _mockValidateConfig.HasTemplateRepoFile().Returns(true);
        _mockFileSystem.GetDrives().Returns(drives);
        _mockConsole.PromptSelectFolderPath(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<string>(), _mockFileSystem, _mockValidateConfig, Arg.Any<CancellationToken>())
            .Returns((false, RepoPath));
        _mockFileSystem.ReadAllTextAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(BasicJsonConfig);
        _mockValidateConfig.ValidateRepoStructure(BasicJsonConfig).Returns((true, []));
        var cursorPos = (0, 0);
        _mockConsole.PromptGetCursorPosition().Returns(cursorPos);
        _mockConsole.PromptWriteWait(Arg.Any<string>());
        _mockConsole.PromptClearWaitText(cursorPos);
        _mockAdrServices.ReadAllAdr(_mockFileSystem, RepoPath, Arg.Any<AdrPlusRepoConfig>(), false)
            .Returns([CommandHandlerMockHelper.CreateValidAdrFileNameComponents(AdrFileName, AdrStatus.Accepted)]);
        _mockConsole.PromptSelecAdrs(Arg.Any<AdrFileNameComponents[]>(), Arg.Any<AdrPlusRepoConfig>(), Arg.Any<Func<AdrFileNameComponents, (bool, string?)>>(), Arg.Any<CancellationToken>())
            .Returns((true, (AdrFileNameComponents?)null));

        // Act & Assert
        await _handler.Invoking(h => h.ExecuteAsync(args, CancellationToken.None))
            .Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task ExecuteAsync_WithWizardMode_DatePromptAborted_ThrowsOperationCanceledException()
    {
        // Arrange
        var args = new[] { "--wizard" };
        var parsedArgs = new Dictionary<Arguments, string> { { Arguments.WizardReview, string.Empty } };
        var drives = new[] { TestPathData.SingleTestDrive };

        _mockAdrServices.ParseArgs(args, Arg.Any<Arguments[]>()).Returns(parsedArgs);
        _mockValidateConfig.HasTemplateRepoFile().Returns(true);
        _mockFileSystem.GetDrives().Returns(drives);
        _mockConsole.PromptSelectFolderPath(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<string>(), _mockFileSystem, _mockValidateConfig, Arg.Any<CancellationToken>())
            .Returns((false, RepoPath));
        _mockFileSystem.ReadAllTextAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(BasicJsonConfig);
        _mockValidateConfig.ValidateRepoStructure(BasicJsonConfig).Returns((true, []));
        var cursorPos = (0, 0);
        _mockConsole.PromptGetCursorPosition().Returns(cursorPos);
        _mockConsole.PromptWriteWait(Arg.Any<string>());
        _mockConsole.PromptClearWaitText(cursorPos);
        var selectedAdr = CommandHandlerMockHelper.CreateValidAdrFileNameComponents(AdrFileName, AdrStatus.Accepted);
        selectedAdr.Number = 1;
        _mockAdrServices.ReadAllAdr(_mockFileSystem, RepoPath, Arg.Any<AdrPlusRepoConfig>(), false)
            .Returns([selectedAdr]);
        _mockConsole.PromptSelecAdrs(Arg.Any<AdrFileNameComponents[]>(), Arg.Any<AdrPlusRepoConfig>(), Arg.Any<Func<AdrFileNameComponents, (bool, string?)>>(), Arg.Any<CancellationToken>())
            .Returns((false, selectedAdr));
        _mockAdrServices.ReadAllAdrByNumber(1, _mockFileSystem, RepoPath, Arg.Any<AdrPlusRepoConfig>())
            .Returns([]);
        _mockConsole.PromptCalendar(Arg.Any<string>(), Arg.Any<DateTime>(), _config, Arg.Any<CancellationToken>())
            .Returns((true, DateTime.UtcNow));

        // Act & Assert
        await _handler.Invoking(h => h.ExecuteAsync(args, CancellationToken.None))
            .Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task ExecuteAsync_WithWizardMode_ConfirmationAborted_ThrowsOperationCanceledException()
    {
        // Arrange
        var args = new[] { "--wizard" };
        var parsedArgs = new Dictionary<Arguments, string> { { Arguments.WizardReview, string.Empty } };

        SetupWizardMocksUpToConfirmation(BasicJsonConfig);
        _mockAdrServices.ParseArgs(args, Arg.Any<Arguments[]>()).Returns(parsedArgs);
        _mockConsole.PromptConfirm(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((true, false));

        // Act & Assert
        await _handler.Invoking(h => h.ExecuteAsync(args, CancellationToken.None))
            .Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task ExecuteAsync_WithWizardMode_ConfirmationDeclined_RetriesWizard()
    {
        // Arrange
        var args = new[] { "--wizard" };
        var parsedArgs = new Dictionary<Arguments, string> { { Arguments.WizardReview, string.Empty } };

        SetupWizardMocksUpToConfirmation(BasicJsonConfig);
        _mockAdrServices.ParseArgs(args, Arg.Any<Arguments[]>()).Returns(parsedArgs);

        // First confirmation is declined (No), second aborts
        _mockConsole.PromptConfirm(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((false, false), (true, false));

        // Act & Assert
        await _handler.Invoking(h => h.ExecuteAsync(args, CancellationToken.None))
            .Should().ThrowAsync<OperationCanceledException>();

        // Confirm folder prompts were called at least twice (second loop iteration)
        _mockConsole.Received(2).PromptSelectFolderPath(
            Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<string>(), _mockFileSystem, _mockValidateConfig, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WithWizardMode_ConfirmedYes_CreatesAdrFile()
    {
        // Arrange
        var args = new[] { "--wizard" };
        var parsedArgs = new Dictionary<Arguments, string> { { Arguments.WizardReview, string.Empty } };

        SetupWizardMocksUpToConfirmation(BasicJsonConfig);
        _mockAdrServices.ParseArgs(args, Arg.Any<Arguments[]>()).Returns(parsedArgs);
        _mockConsole.PromptConfirm(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((false, true));

        // Force the wizard setup to use ReturnsForAnyArgs for ADR-service methods to ensure they're called correctly
        _mockAdrServices.ReadAllAdrByNumber(Arg.Any<int>(), Arg.Any<IFileSystemService>(), Arg.Any<string>(), Arg.Any<AdrPlusRepoConfig>())
            .ReturnsForAnyArgs(callInfo =>
            {
                var selectedAdr = CommandHandlerMockHelper.CreateValidAdrFileNameComponents(AdrFileName, AdrStatus.Accepted);
                selectedAdr.Number = 1;
                return [selectedAdr];
            });
        _mockAdrServices.GetLatestADRSequence(Arg.Any<int>(), Arg.Any<IFileSystemService>(), Arg.Any<string>(), Arg.Any<AdrPlusRepoConfig>())
            .ReturnsForAnyArgs(callInfo =>
            {
                var selectedAdr = CommandHandlerMockHelper.CreateValidAdrFileNameComponents(AdrFileName, AdrStatus.Accepted);
                selectedAdr.Number = 1;
                return Task.FromResult<AdrFileNameComponents?>(selectedAdr);
            });

        // Act
        await _handler.ExecuteAsync(args, CancellationToken.None);

        // Assert
        await _mockFileSystem.Received(1).WriteAllTextAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WithWizardMode_DisplaysSummaryBeforeConfirmation()
    {
        // Arrange
        var args = new[] { "--wizard" };
        var parsedArgs = new Dictionary<Arguments, string> { { Arguments.WizardReview, string.Empty } };

        SetupWizardMocksUpToConfirmation(BasicJsonConfig);
        _mockAdrServices.ParseArgs(args, Arg.Any<Arguments[]>()).Returns(parsedArgs);
        _mockConsole.PromptConfirm(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((true, false));

        // Act
        await _handler.Invoking(h => h.ExecuteAsync(args, CancellationToken.None))
            .Should().ThrowAsync<OperationCanceledException>();

        // Assert: PromptWriteSummary called for repo, file, date, and empty line separator (4 calls)
        _mockConsole.Received(4).PromptWriteSummary(Arg.Any<string>());
    }

    [Fact]
    public async Task ExecuteAsync_WithWizardMode_InvalidRepoConfig_ThrowsInvalidDataException()
    {
        // Arrange
        var args = new[] { "--wizard" };
        var parsedArgs = new Dictionary<Arguments, string> { { Arguments.WizardReview, string.Empty } };
        var drives = new[] { TestPathData.SingleTestDrive };

        _mockAdrServices.ParseArgs(args, Arg.Any<Arguments[]>()).Returns(parsedArgs);
        _mockValidateConfig.HasTemplateRepoFile().Returns(true);
        _mockFileSystem.GetDrives().Returns(drives);
        _mockConsole.PromptSelectFolderPath(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<string>(), _mockFileSystem, _mockValidateConfig, Arg.Any<CancellationToken>())
            .Returns((false, RepoPath));
        _mockFileSystem.ReadAllTextAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(BasicJsonConfig);
        _mockValidateConfig.ValidateRepoStructure(BasicJsonConfig).Returns((false, ["Config error"]));

        // Act & Assert
        await _handler.Invoking(h => h.ExecuteAsync(args, CancellationToken.None))
            .Should().ThrowAsync<InvalidDataException>();
    }

    [Fact]
    public async Task ExecuteAsync_WithWizardMode_NoAdrFilesFound_ThrowsFileNotFoundException()
    {
        // Arrange
        var args = new[] { "--wizard" };
        var parsedArgs = new Dictionary<Arguments, string> { { Arguments.WizardReview, string.Empty } };
        var drives = new[] { TestPathData.SingleTestDrive };

        _mockAdrServices.ParseArgs(args, Arg.Any<Arguments[]>()).Returns(parsedArgs);
        _mockValidateConfig.HasTemplateRepoFile().Returns(true);
        _mockFileSystem.GetDrives().Returns(drives);
        _mockConsole.PromptSelectFolderPath(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<string>(), _mockFileSystem, _mockValidateConfig, Arg.Any<CancellationToken>())
            .Returns((false, RepoPath));
        _mockFileSystem.ReadAllTextAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(BasicJsonConfig);
        _mockValidateConfig.ValidateRepoStructure(BasicJsonConfig).Returns((true, []));
        var cursorPos = (0, 0);
        _mockConsole.PromptGetCursorPosition().Returns(cursorPos);
        _mockConsole.PromptWriteWait(Arg.Any<string>());
        _mockConsole.PromptClearWaitText(cursorPos);
        _mockAdrServices.ReadAllAdr(_mockFileSystem, RepoPath, Arg.Any<AdrPlusRepoConfig>(), false)
            .Returns([]);

        // Act & Assert
        await _handler.Invoking(h => h.ExecuteAsync(args, CancellationToken.None))
            .Should().ThrowAsync<FileNotFoundException>();
    }

    #endregion

    #region Helper Methods

    private static AdrFileNameComponents CreateTestAdrFileNameComponents(
        string fileName,
        AdrStatus status,
        int number = 1,
        int revision = 1)
    {
        return new AdrFileNameComponents
        {
            FileName = fileName,
            Number = number,
            IsValid = true,
            Revision = revision,
            Title = "test",
            Header = new AdrHeader
            {
                IsValid = true,
                StatusUpdate = status,
                StatusCreate = AdrStatus.Proposed,
                StatusChange = AdrStatus.Unknown,
                Title = "Test ADR"
            },
            ContentAdr = "Test content"
        };
    }

    private void SetupBasicMocks(Dictionary<Arguments, string> parsedArgs, string jsonConfig, string configPath)
    {
        _mockAdrServices.ParseArgs(Arg.Any<string[]>(), Arg.Any<Arguments[]>()).Returns(parsedArgs);
        _mockValidateConfig.HasTemplateRepoFile().Returns(true);
        _mockValidateConfig.GetFileNameRepoConfig().Returns(ConfigFileName);

        // FileExists: return true for existing files (config, selected ADR), false for new revisions
        _mockFileSystem.FileExists(Arg.Any<string>()).Returns(callInfo =>
        {
            var path = callInfo.Arg<string>();
            var normalized = Path.GetFullPath(path);
            var configNormalized = Path.GetFullPath(configPath);
            var adrNormalized = Path.GetFullPath(AdrFilePath);

            // Config file and existing ADR file exist, new revision files do not
            if (normalized == configNormalized || normalized == adrNormalized)
            {
                return true;
            }

            // Check if this looks like a revision file (contains -v and -r pattern)
            if (path.Contains("-v") && path.Contains("-r"))
            {
                return false; // New revision files don't exist yet
            }

            return path.EndsWith(".adrplus");
        });

        // ReadAllTextAsync: return config for any path ending in .adrplus
        _mockFileSystem.ReadAllTextAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(callInfo =>
        {
            var path = callInfo.Arg<string>();
            return Task.FromResult(path.EndsWith(".adrplus") ? jsonConfig : "");
        });

        _mockValidateConfig.ValidateRepoStructure(jsonConfig).Returns((true, []));

        // GetFileRootRepositoryPath: extract directory and append config filename
        _mockFileSystem.GetFileRootRepositoryPath(Arg.Any<string>()).Returns(callInfo =>
        {
            var filePath = callInfo.Arg<string>();
            var dir = Path.GetDirectoryName(filePath);
            return string.IsNullOrEmpty(dir) ? null : Path.Combine(dir, ConfigFileName);
        });

        // GetFullNameDirectoryByFile: extract directory from file path
        _mockFileSystem.GetFullNameDirectoryByFile(Arg.Any<string>()).Returns(callInfo =>
        {
            var filePath = callInfo.Arg<string>();
            return Path.GetDirectoryName(filePath) ?? string.Empty;
        });

        // GetFullNameFile: normalize path using Path.GetFullPath
        _mockFileSystem.GetFullNameFile(Arg.Any<string>()).Returns(callInfo =>
        {
            var path = callInfo.Arg<string>();
            return string.IsNullOrEmpty(path) ? Path.GetFullPath(configPath) : Path.GetFullPath(path);
        });

        var cursorPos = (0, 0);
        _mockConsole.PromptGetCursorPosition().Returns(cursorPos);
        _mockConsole.PromptWriteWait(Arg.Any<string>());
        _mockConsole.PromptClearWaitText(cursorPos);
        _mockConsole.PromptWriteSuccess(Arg.Any<string>());
        _mockConsole.PromptWriteError(Arg.Any<string>());
        _mockConsole.PromptWriteSummary(Arg.Any<string>());

        // Create a valid ADR component for testing
        var validAdr = CommandHandlerMockHelper.CreateValidAdrFileNameComponents(AdrFileName, AdrStatus.Accepted);
        validAdr.Number = 1;

        // Mock ParseFileName to return valid ADR (async method must return Task)
        _mockAdrServices.ParseFileName(Arg.Any<string>(), Arg.Any<AdrPlusRepoConfig>(), _mockFileSystem)
            .Returns(Task.FromResult(validAdr));

        // Mock ReadAllAdrByNumber to return collection with valid ADR (async method must return Task)
        _mockAdrServices.ReadAllAdrByNumber(Arg.Any<int>(), _mockFileSystem, Arg.Any<string>(), Arg.Any<AdrPlusRepoConfig>())
            .Returns(Task.FromResult(new[] { validAdr }));

        // Mock GetLatestADRSequence to return the valid ADR (async method must return Task)
        _mockAdrServices.GetLatestADRSequence(Arg.Any<int>(), _mockFileSystem, Arg.Any<string>(), Arg.Any<AdrPlusRepoConfig>())
            .Returns(Task.FromResult<AdrFileNameComponents?>(validAdr));

        // Mock FromJson for repo configuration
        var repoConfig = new AdrPlusRepoConfig("", "")
        {
            Prefix = "ADR",
            LenSeq = 4,
            LenVersion = 2,
            LenRevision = 0,
            FolderAdr = "adr",
            FolderByScope = false,
            Separator = '-',
            LenScope = 0,
            Scopes = "",
            SkipDomain = "",
            CaseTransform = CaseFormat.PascalCase,
            Template = "# ADR",
            StatusNew = "Proposed"
        };
        _mockAdrServices.FromJson(Arg.Any<string>(), Arg.Any<string>()).Returns(repoConfig);
    }

    private void SetupWizardMocksUpToConfirmation(string jsonConfig)
    {
        var drives = new[] { TestPathData.SingleTestDrive };
        _mockValidateConfig.HasTemplateRepoFile().Returns(true);
        _mockValidateConfig.GetFileNameRepoConfig().Returns(ConfigFileName);
        _mockFileSystem.GetDrives().Returns(drives);
        _mockConsole.PromptSelectFolderPath(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<string>(), _mockFileSystem, _mockValidateConfig, Arg.Any<CancellationToken>())
            .Returns((false, RepoPath));
        _mockFileSystem.DirectoryExists(Arg.Any<string>()).Returns(true);

        // FileExists: return true for existing files, false for new revisions
        _mockFileSystem.FileExists(Arg.Any<string>()).Returns(callInfo =>
        {
            var path = callInfo.Arg<string>();
            var normalized = Path.GetFullPath(path);
            var configNormalized = Path.GetFullPath(Path.Combine(RepoPath, ConfigFileName));
            var adrNormalized = Path.GetFullPath(AdrFileName);

            // Config file and existing ADR file exist, new revision files do not
            if (normalized == configNormalized || normalized == adrNormalized)
            {
                return true;
            }

            // Check if this looks like a revision file (contains -v and -r pattern)
            if (path.Contains("-v") && path.Contains("-r"))
            {
                return false; // New revision files don't exist yet
            }

            return path.EndsWith(".adrplus");
        });

        // ReadAllTextAsync: return config for any path ending in .adrplus
        _mockFileSystem.ReadAllTextAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(callInfo =>
        {
            var path = callInfo.Arg<string>();
            return Task.FromResult(path.EndsWith(".adrplus") ? jsonConfig : "");
        });

        _mockValidateConfig.ValidateRepoStructure(jsonConfig).Returns((true, []));
        _mockConsole.PromptWriteWait(Arg.Any<string>());
        var cursorPos = (0, 0);
        _mockConsole.PromptGetCursorPosition().Returns(cursorPos);
        _mockConsole.PromptClearWaitText(cursorPos);

        // GetFileRootRepositoryPath: extract directory and append config filename
        _mockFileSystem.GetFileRootRepositoryPath(Arg.Any<string>()).Returns(callInfo =>
        {
            var filePath = callInfo.Arg<string>();
            var dir = Path.GetDirectoryName(filePath);
            return string.IsNullOrEmpty(dir) ? null : Path.Combine(dir, ConfigFileName);
        });

        // GetFullNameDirectoryByFile: extract directory from file path
        _mockFileSystem.GetFullNameDirectoryByFile(Arg.Any<string>()).Returns(callInfo =>
        {
            var filePath = callInfo.Arg<string>();
            return Path.GetDirectoryName(filePath) ?? string.Empty;
        });

        // GetFullNameFile: normalize path using Path.GetFullPath
        _mockFileSystem.GetFullNameFile(Arg.Any<string>()).Returns(callInfo =>
        {
            var path = callInfo.Arg<string>();
            return string.IsNullOrEmpty(path) ? Path.GetFullPath(Path.Combine(RepoPath, ConfigFileName)) : Path.GetFullPath(path);
        });

        var selectedAdr = CommandHandlerMockHelper.CreateValidAdrFileNameComponents(AdrFileName, AdrStatus.Accepted);
        selectedAdr.Number = 1;

        _mockAdrServices.ReadAllAdr(_mockFileSystem, RepoPath, Arg.Any<AdrPlusRepoConfig>(), false)
            .Returns([selectedAdr]);
        _mockConsole.PromptSelecAdrs(Arg.Any<AdrFileNameComponents[]>(), Arg.Any<AdrPlusRepoConfig>(), Arg.Any<Func<AdrFileNameComponents, (bool, string?)>>(), Arg.Any<CancellationToken>())
            .Returns((false, selectedAdr));

        // Mock ParseFileName to return the selected ADR (async method must return Task)
        _mockAdrServices.ParseFileName(Arg.Any<string>(), Arg.Any<AdrPlusRepoConfig>(), Arg.Any<IFileSystemService>())
            .Returns(Task.FromResult(selectedAdr));

        _mockAdrServices.ReadAllAdrByNumber(1, _mockFileSystem, RepoPath, Arg.Any<AdrPlusRepoConfig>())
            .Returns(Task.FromResult(new[] { selectedAdr }));
        _mockAdrServices.GetLatestADRSequence(1, _mockFileSystem, RepoPath, Arg.Any<AdrPlusRepoConfig>())
            .Returns(Task.FromResult<AdrFileNameComponents?>(selectedAdr));
        _mockConsole.PromptCalendar(Arg.Any<string>(), Arg.Any<DateTime>(), _config, Arg.Any<CancellationToken>())
            .Returns((false, new DateTime(2026, 1, 15)));
        _mockConsole.PromptEmptyTemplate(Arg.Any<CancellationToken>())
            .Returns((false, false));
        var (_, Top) = ((0, 10));
        _mockConsole.PromptCursorPosition().Returns((0, Top));
        _mockConsole.PromptMovePosition(0, Top);

        var repoConfig = new AdrPlusRepoConfig("", "")
        {
            Prefix = "ADR",
            LenSeq = 4,
            LenVersion = 2,
            LenRevision = 0,
            FolderAdr = "adr",
            FolderByScope = false,
            Separator = '-',
            LenScope = 0,
            Scopes = "",
            SkipDomain = "",
            CaseTransform = CaseFormat.PascalCase,
            Template = "# ADR",
            StatusNew = "Proposed"
        };
        _mockAdrServices.FromJson(Arg.Any<string>(), Arg.Any<string>()).Returns(repoConfig);
    }

    private ReviewCommandHandler CreateHandlerWith(AdrPlusConfig config)
    {
        return new ReviewCommandHandler(
            _mockLogger,
            Options.Create(config),
            _mockFileSystem,
            _mockValidateConfig,
            _mockConsole,
            _mockAdrServices);
    }

    private static AdrPlusRepoConfig CreateMockAdrPlusRepoConfig()
    {
        return new AdrPlusRepoConfig("", "")
        {
            Prefix = "ADR",
            LenSeq = 4,
            LenVersion = 2,
            LenRevision = 0,
            FolderAdr = "adr",
            FolderByScope = false,
            Separator = '-',
            LenScope = 0,
            Scopes = "",
            SkipDomain = "",
            CaseTransform = CaseFormat.PascalCase,
            Template = "# ADR",
            StatusNew = "Proposed"
        };
    }

    private void SetupMinimalMocksWithPathNormalization(Dictionary<Arguments, string> parsedArgs, string jsonConfig, string configPath)
    {
        var normalizedConfigPath = Path.GetFullPath(configPath);
        var normalizedAdrPath = Path.GetFullPath(AdrFilePath);

        _mockAdrServices.ParseArgs(Arg.Any<string[]>(), Arg.Any<Arguments[]>()).Returns(parsedArgs);
        _mockValidateConfig.HasTemplateRepoFile().Returns(true);
        _mockValidateConfig.GetFileNameRepoConfig().Returns(ConfigFileName);

        // Mock FileExists with path normalization - returns true for config and ADR paths
        _mockFileSystem.FileExists(Arg.Any<string>()).Returns(callInfo =>
        {
            var path = Path.GetFullPath(callInfo.Arg<string>());
            return path == normalizedConfigPath || path == normalizedAdrPath;
        });

        _mockFileSystem.GetFileRootRepositoryPath(Arg.Any<string>()).Returns(configPath);
        _mockFileSystem.ReadAllTextAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(callInfo =>
        {
            var path = callInfo.Arg<string>();
            return Task.FromResult(path.EndsWith(".adrplus") ? jsonConfig : "");
        });
        _mockValidateConfig.ValidateRepoStructure(jsonConfig).Returns((true, []));
        _mockFileSystem.GetFullNameDirectoryByFile(Arg.Any<string>()).Returns("/repo");
        _mockFileSystem.GetFullNameFile(Arg.Any<string>()).Returns(callInfo =>
        {
            var path = callInfo.Arg<string>();
            return Path.GetFullPath(string.IsNullOrEmpty(path) ? configPath : path);
        });

        var repoConfig = ReviewCommandHandlerTests.CreateMockAdrPlusRepoConfig();
        _mockAdrServices.FromJson(Arg.Any<string>(), Arg.Any<string>()).Returns(repoConfig);

        // Mock ALL console methods to prevent null ref errors (void methods don't use .Returns)
        _mockConsole.PromptWriteError(Arg.Any<string>());
        _mockConsole.PromptWriteSuccess(Arg.Any<string>());
        _mockConsole.PromptWriteSummary(Arg.Any<string>());
        _mockConsole.PromptWriteWait(Arg.Any<string>());

        // Mock cursor position to prevent NullReferenceException
        var cursorPos = (0, 0);
        _mockConsole.PromptGetCursorPosition().Returns(cursorPos);

        // Provide a default ParseFileName that tests should override - ensures it doesn't return null (async method must return Task)
        var defaultAdr = ReviewCommandHandlerTests.CreateTestAdrFileNameComponents(AdrFileName, AdrStatus.Accepted, number: 1);
        _mockAdrServices.ParseFileName(Arg.Any<string>(), Arg.Any<AdrPlusRepoConfig>(), Arg.Any<IFileSystemService>())
            .Returns(Task.FromResult(defaultAdr));

        // Provide a default GetLatestADRSequence for negative tests that should override it (async method must return Task)
        _mockAdrServices.GetLatestADRSequence(Arg.Any<int>(), Arg.Any<IFileSystemService>(), Arg.Any<string>(), Arg.Any<AdrPlusRepoConfig>())
            .Returns(Task.FromResult<AdrFileNameComponents?>(defaultAdr));

        // Provide a default ReadAllAdrByNumber that tests should override (async method must return Task)
        _mockAdrServices.ReadAllAdrByNumber(Arg.Any<int>(), Arg.Any<IFileSystemService>(), Arg.Any<string>(), Arg.Any<AdrPlusRepoConfig>())
            .Returns(Task.FromResult(Array.Empty<AdrFileNameComponents>()));
    }

    #endregion
}
