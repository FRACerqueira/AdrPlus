// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the AdrPlus project under MIT license
// ***************************************************************************************

using AdrPlus.Commands;
using AdrPlus.Commands.NewAdr;
using AdrPlus.Core;
using AdrPlus.Domain;
using AdrPlus.Infrastructure.FileSystem;
using AdrPlus.Infrastructure.UI;
using AdrPlus.Tests.Helpers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute.ExceptionExtensions;

namespace AdrPlus.Tests.Commands.NewAdr;

/// <summary>
/// Unit tests for NewAdrCommandHandler class.
/// Tests cover command execution, wizard flows, validation, and error handling using NSubstitute.
/// </summary>
public class NewAdrCommandHandlerTests
{
    private readonly ILogger<NewAdrCommandHandler> _mockLogger;
    private readonly IFileSystemService _mockFileSystem;
    private readonly IConsoleWriter _mockConsole;
    private readonly IValidateJsonConfig _mockValidateConfig;
    private readonly IAdrServices _mockAdrServices;
    private readonly AdrPlusConfig _config;
    private readonly NewAdrCommandHandler _handler;

    private static readonly string RepoPath = Path.Combine(Path.GetTempPath(), "repo");
    private const string FolderRepo = "docs/adr";
    private const string ConfigFileName = ".adrplus";
    private static readonly string AdrFolderPath = Path.Combine(RepoPath, FolderRepo);
    private static readonly string ConfigFilePath = Path.Combine(AdrFolderPath, ConfigFileName);

    private static readonly string BasicJsonConfig =
        """{"Prefix": "ADR", "LenSeq": 4, "LenVersion": 2, "StatusNew": "Proposed", "StatusAcc": "Accepted", "StatusSup": "Superseded"}""";

    private static readonly string ScopedJsonConfig =
        """{"Prefix": "ADR", "LenSeq": 4, "LenVersion": 2, "LenScope": 1, "Scopes": "Enterprise;Domain;Project", "StatusNew": "Proposed", "StatusAcc": "Accepted", "StatusSup": "Superseded"}""";

    private static readonly string ScopedSkipDomainJsonConfig =
        """{"Prefix": "ADR", "LenSeq": 4, "LenVersion": 2, "LenScope": 1, "Scopes": "Enterprise;Domain;Project", "SkipDomain": "Enterprise", "StatusNew": "Proposed", "StatusAcc": "Accepted", "StatusSup": "Superseded"}""";

    public NewAdrCommandHandlerTests()
    {
        _mockLogger = Substitute.For<ILogger<NewAdrCommandHandler>>();
        _mockFileSystem = Substitute.For<IFileSystemService>();
        _mockConsole = Substitute.For<IConsoleWriter>();
        _mockValidateConfig = Substitute.For<IValidateJsonConfig>();
        _mockAdrServices = Substitute.For<IAdrServices>();

        _config = new AdrPlusConfig
        {
            Language = "en-US"
        };

        _mockValidateConfig.GetFileNameRepoConfig().Returns(ConfigFileName);

        _handler = new NewAdrCommandHandler(
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
        var handler = new NewAdrCommandHandler(
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
        var args = new[] { "--path", RepoPath, "--title", "My ADR" };
        var parsedArgs = new Dictionary<Arguments, string>
        {
            { Arguments.TargetRepo, RepoPath },
            { Arguments.TitleAdr, "My ADR" }
        };
        _mockAdrServices.ParseArgs(args, Arg.Any<Arguments[]>()).Returns(parsedArgs);
        _mockValidateConfig.HasTemplateRepoFile().Returns(false);

        // Act & Assert
        await _handler.Invoking(h => h.ExecuteAsync(args, CancellationToken.None))
            .Should().ThrowAsync<FileNotFoundException>();
    }

    #endregion

    #region ExecuteAsync - Directory Validation Tests

    [Fact]
    public async Task ExecuteAsync_WhenTargetDirectoryNotFound_ThrowsDirectoryNotFoundException()
    {
        // Arrange
        var args = new[] { "--path", RepoPath, "--title", "My ADR" };
        var parsedArgs = new Dictionary<Arguments, string>
        {
            { Arguments.TargetRepo, RepoPath },
            { Arguments.TitleAdr, "My ADR" }
        };
        _mockAdrServices.ParseArgs(args, Arg.Any<Arguments[]>()).Returns(parsedArgs);
        _mockValidateConfig.HasTemplateRepoFile().Returns(true);
        _mockFileSystem.DirectoryExists(Arg.Any<string>()).Returns(false);

        // Act & Assert
        await _handler.Invoking(h => h.ExecuteAsync(args, CancellationToken.None))
            .Should().ThrowAsync<DirectoryNotFoundException>();
    }

    [Fact]
    public async Task ExecuteAsync_WhenRepoConfigFileNotFound_ThrowsFileNotFoundException()
    {
        // Arrange
        var args = new[] { "--path", RepoPath, "--title", "My ADR" };
        var parsedArgs = new Dictionary<Arguments, string>
        {
            { Arguments.TargetRepo, RepoPath },
            { Arguments.TitleAdr, "My ADR" }
        };
        _mockAdrServices.ParseArgs(args, Arg.Any<Arguments[]>()).Returns(parsedArgs);
        _mockValidateConfig.HasTemplateRepoFile().Returns(true);
        _mockFileSystem.DirectoryExists(Arg.Any<string>()).Returns(true);
        _mockFileSystem.FileExists(Arg.Is<string>(s => s.Contains(ConfigFileName))).Returns(false);

        // Act & Assert
        await _handler.Invoking(h => h.ExecuteAsync(args, CancellationToken.None))
            .Should().ThrowAsync<FileNotFoundException>();
    }

    #endregion

    #region ExecuteAsync - Repo Config Validation Tests

    [Fact]
    public async Task ExecuteAsync_WhenRepoConfigIsInvalid_ThrowsInvalidDataException()
    {
        // Arrange
        var args = new[] { "--path", RepoPath, "--title", "My ADR" };
        var parsedArgs = new Dictionary<Arguments, string>
        {
            { Arguments.TargetRepo, RepoPath },
            { Arguments.TitleAdr, "My ADR" }
        };
        var errors = new[] { "Missing Prefix field" };

        _mockAdrServices.ParseArgs(args, Arg.Any<Arguments[]>()).Returns(parsedArgs);
        _mockValidateConfig.HasTemplateRepoFile().Returns(true);
        _mockFileSystem.DirectoryExists(Arg.Any<string>()).Returns(true);
        _mockFileSystem.FileExists(Arg.Is<string>(s => s.Contains(ConfigFileName))).Returns(true);
        _mockFileSystem.ReadAllTextAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(BasicJsonConfig);
        _mockValidateConfig.ValidateRepoStructure(BasicJsonConfig).Returns((false, errors));

        // Act & Assert
        await _handler.Invoking(h => h.ExecuteAsync(args, CancellationToken.None))
            .Should().ThrowAsync<InvalidDataException>();
    }

    [Fact]
    public async Task ExecuteAsync_WhenRepoConfigIsInvalid_WritesEachErrorToConsole()
    {
        // Arrange
        var args = new[] { "--path", RepoPath, "--title", "My ADR" };
        var parsedArgs = new Dictionary<Arguments, string>
        {
            { Arguments.TargetRepo, RepoPath },
            { Arguments.TitleAdr, "My ADR" }
        };
        var errors = new[] { "Error one", "Error two" };

        _mockAdrServices.ParseArgs(args, Arg.Any<Arguments[]>()).Returns(parsedArgs);
        _mockValidateConfig.HasTemplateRepoFile().Returns(true);
        _mockFileSystem.DirectoryExists(Arg.Any<string>()).Returns(true);
        _mockFileSystem.FileExists(Arg.Is<string>(s => s.Contains(ConfigFileName))).Returns(true);
        _mockFileSystem.ReadAllTextAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(BasicJsonConfig);
        _mockValidateConfig.ValidateRepoStructure(BasicJsonConfig).Returns((false, errors));

        // Act
        await _handler.Invoking(h => h.ExecuteAsync(args, CancellationToken.None))
            .Should().ThrowAsync<InvalidDataException>();

        // Assert
        _mockConsole.Received(1).WriteError("Error one");
        _mockConsole.Received(1).WriteError("Error two");
    }

    #endregion

    #region ExecuteAsync - Scope and Domain Validation Tests

    [Fact]
    public async Task ExecuteAsync_WhenScopeRequiredButMissing_ThrowsArgumentException()
    {
        // Arrange
        var args = new[] { "--path", RepoPath, "--title", "My ADR" };
        var parsedArgs = new Dictionary<Arguments, string>
        {
            { Arguments.TargetRepo, RepoPath },
            { Arguments.TitleAdr, "My ADR" }
        };

        SetupBasicMocks(parsedArgs, ScopedJsonConfig);
        _mockAdrServices.GetFileByUniqueTitle(Arg.Any<string>(), Arg.Any<string>(), _mockFileSystem, Arg.Any<string>(), Arg.Any<AdrPlusRepoConfig>())
            .Returns(string.Empty);

        // Act & Assert
        await _handler.Invoking(h => h.ExecuteAsync(args, CancellationToken.None))
            .Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task ExecuteAsync_WhenScopeIsInvalid_ThrowsArgumentException()
    {
        // Arrange
        var args = new[] { "--path", RepoPath, "--title", "My ADR", "--scope", "InvalidScope" };
        var parsedArgs = new Dictionary<Arguments, string>
        {
            { Arguments.TargetRepo, RepoPath },
            { Arguments.TitleAdr, "My ADR" },
            { Arguments.ScopeAdr, "InvalidScope" }
        };

        SetupBasicMocks(parsedArgs, ScopedJsonConfig);

        // Act & Assert
        await _handler.Invoking(h => h.ExecuteAsync(args, CancellationToken.None))
            .Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task ExecuteAsync_WhenDomainRequiredButMissing_ThrowsArgumentException()
    {
        // Arrange
        var args = new[] { "--path", RepoPath, "--title", "My ADR", "--scope", "Domain" };
        var parsedArgs = new Dictionary<Arguments, string>
        {
            { Arguments.TargetRepo, RepoPath },
            { Arguments.TitleAdr, "My ADR" },
            { Arguments.ScopeAdr, "Domain" }
        };

        SetupBasicMocks(parsedArgs, ScopedJsonConfig);

        // Act & Assert
        await _handler.Invoking(h => h.ExecuteAsync(args, CancellationToken.None))
            .Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task ExecuteAsync_WhenScopeIsInSkipDomainList_DoesNotRequireDomain()
    {
        // Arrange
        var args = new[] { "--path", RepoPath, "--title", "My ADR", "--scope", "Enterprise" };
        var parsedArgs = new Dictionary<Arguments, string>
        {
            { Arguments.TargetRepo, RepoPath },
            { Arguments.TitleAdr, "My ADR" },
            { Arguments.ScopeAdr, "Enterprise" }
        };

        SetupBasicMocks(parsedArgs, ScopedSkipDomainJsonConfig);
        _mockAdrServices.GetFileByUniqueTitle(Arg.Any<string>(), Arg.Any<string>(), _mockFileSystem, Arg.Any<string>(), Arg.Any<AdrPlusRepoConfig>())
            .Returns(string.Empty);
        _mockAdrServices.GetNextNumber(_mockFileSystem, Arg.Any<string>(), Arg.Any<AdrPlusRepoConfig>()).Returns(1);
        _mockFileSystem.GetFullNameFile(Arg.Any<string>()).Returns(callInfo => callInfo.Arg<string>());

        // Act & Assert - no ArgumentException means domain was not required
        await _handler.Invoking(h => h.ExecuteAsync(args, CancellationToken.None))
            .Should().NotThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task ExecuteAsync_WhenNoScopeConfigured_DoesNotRequireScopeOrDomain()
    {
        // Arrange
        var args = new[] { "--path", RepoPath, "--title", "My ADR" };
        var parsedArgs = new Dictionary<Arguments, string>
        {
            { Arguments.TargetRepo, RepoPath },
            { Arguments.TitleAdr, "My ADR" }
        };

        SetupBasicMocks(parsedArgs, BasicJsonConfig);
        _mockAdrServices.GetFileByUniqueTitle(Arg.Any<string>(), Arg.Any<string>(), _mockFileSystem, Arg.Any<string>(), Arg.Any<AdrPlusRepoConfig>())
            .Returns(string.Empty);
        _mockAdrServices.GetNextNumber(_mockFileSystem, Arg.Any<string>(), Arg.Any<AdrPlusRepoConfig>()).Returns(1);
        _mockFileSystem.GetFullNameFile(Arg.Any<string>()).Returns(callInfo => callInfo.Arg<string>());

        // Act & Assert - no ArgumentException means neither scope nor domain was required
        await _handler.Invoking(h => h.ExecuteAsync(args, CancellationToken.None))
            .Should().NotThrowAsync<ArgumentException>();
    }

    #endregion

    #region ExecuteAsync - Duplicate Title Tests

    [Fact]
    public async Task ExecuteAsync_WhenAdrWithSameTitleAlreadyExists_ThrowsInvalidOperationException()
    {
        // Arrange
        var args = new[] { "--path", RepoPath, "--title", "Existing ADR" };
        var parsedArgs = new Dictionary<Arguments, string>
        {
            { Arguments.TargetRepo, RepoPath },
            { Arguments.TitleAdr, "Existing ADR" }
        };

        SetupBasicMocks(parsedArgs, BasicJsonConfig);
        _mockAdrServices.GetFileByUniqueTitle("Existing ADR", string.Empty, _mockFileSystem, Arg.Any<string>(), Arg.Any<AdrPlusRepoConfig>())
            .Returns($"{AdrFolderPath}\\ADR-0001-Existing-Adr.md");

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
        var args = new[] { "--path", RepoPath, "--title", "My New ADR" };
        var parsedArgs = new Dictionary<Arguments, string>
        {
            { Arguments.TargetRepo, RepoPath },
            { Arguments.TitleAdr, "My New ADR" }
        };

        SetupBasicMocks(parsedArgs, BasicJsonConfig);
        _mockAdrServices.GetFileByUniqueTitle(Arg.Any<string>(), Arg.Any<string>(), _mockFileSystem, Arg.Any<string>(), Arg.Any<AdrPlusRepoConfig>())
            .Returns(string.Empty);
        _mockAdrServices.GetNextNumber(_mockFileSystem, Arg.Any<string>(), Arg.Any<AdrPlusRepoConfig>()).Returns(1);
        _mockFileSystem.GetFullNameFile(Arg.Any<string>()).Returns(callInfo => callInfo.Arg<string>());

        // Act
        await _handler.ExecuteAsync(args, CancellationToken.None);

        // Assert
        await _mockFileSystem.Received(1).WriteAllTextAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WithValidArgs_WritesSuccessMessageToConsole()
    {
        // Arrange
        var args = new[] { "--path", RepoPath, "--title", "My New ADR" };
        var parsedArgs = new Dictionary<Arguments, string>
        {
            { Arguments.TargetRepo, RepoPath },
            { Arguments.TitleAdr, "My New ADR" }
        };

        SetupBasicMocks(parsedArgs, BasicJsonConfig);
        _mockAdrServices.GetFileByUniqueTitle(Arg.Any<string>(), Arg.Any<string>(), _mockFileSystem, Arg.Any<string>(), Arg.Any<AdrPlusRepoConfig>())
            .Returns(string.Empty);
        _mockAdrServices.GetNextNumber(_mockFileSystem, Arg.Any<string>(), Arg.Any<AdrPlusRepoConfig>()).Returns(1);
        _mockFileSystem.GetFullNameFile(Arg.Any<string>()).Returns(callInfo => callInfo.Arg<string>());

        // Act
        await _handler.ExecuteAsync(args, CancellationToken.None);

        // Assert
        _mockConsole.Received(1).WriteSuccess(Arg.Any<string>());
    }

    #endregion

    #region ExecuteAsync - Date Parsing Tests

    [Fact]
    public async Task ExecuteAsync_WithCustomDate_PassesDateToFileCreation()
    {
        // Arrange
        var customDate = "2026-01-15";
        var args = new[] { "--path", RepoPath, "--title", "My New ADR", "--refdate", customDate };
        var parsedArgs = new Dictionary<Arguments, string>
        {
            { Arguments.TargetRepo, RepoPath },
            { Arguments.TitleAdr, "My New ADR" },
            { Arguments.DateRefAdr, customDate }
        };

        SetupBasicMocks(parsedArgs, BasicJsonConfig);
        _mockAdrServices.GetFileByUniqueTitle(Arg.Any<string>(), Arg.Any<string>(), _mockFileSystem, Arg.Any<string>(), Arg.Any<AdrPlusRepoConfig>())
            .Returns(string.Empty);
        _mockAdrServices.GetNextNumber(_mockFileSystem, Arg.Any<string>(), Arg.Any<AdrPlusRepoConfig>()).Returns(1);

        string? capturedContent = null;
        _mockFileSystem.GetFullNameFile(Arg.Any<string>()).Returns(callInfo => callInfo.Arg<string>());
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
        var args = new[] { "--path", RepoPath, "--title", "My New ADR", "--refdate", "not-a-date" };
        var parsedArgs = new Dictionary<Arguments, string>
        {
            { Arguments.TargetRepo, RepoPath },
            { Arguments.TitleAdr, "My New ADR" },
            { Arguments.DateRefAdr, "not-a-date" }
        };

        SetupBasicMocks(parsedArgs, BasicJsonConfig);
        _mockAdrServices.GetFileByUniqueTitle(Arg.Any<string>(), Arg.Any<string>(), _mockFileSystem, Arg.Any<string>(), Arg.Any<AdrPlusRepoConfig>())
            .Returns(string.Empty);
        _mockAdrServices.GetNextNumber(_mockFileSystem, Arg.Any<string>(), Arg.Any<AdrPlusRepoConfig>()).Returns(1);

        // Act & Assert
        await _handler.Invoking(h => h.ExecuteAsync(args, CancellationToken.None))
            .Should().ThrowAsync<FormatException>();
    }

    [Fact]
    public async Task ExecuteAsync_WithNoDateArgument_UsesCurrentDate()
    {
        // Arrange
        var args = new[] { "--path", RepoPath, "--title", "My New ADR" };
        var parsedArgs = new Dictionary<Arguments, string>
        {
            { Arguments.TargetRepo, RepoPath },
            { Arguments.TitleAdr, "My New ADR" }
        };

        SetupBasicMocks(parsedArgs, BasicJsonConfig);
        _mockAdrServices.GetFileByUniqueTitle(Arg.Any<string>(), Arg.Any<string>(), _mockFileSystem, Arg.Any<string>(), Arg.Any<AdrPlusRepoConfig>())
            .Returns(string.Empty);
        _mockAdrServices.GetNextNumber(_mockFileSystem, Arg.Any<string>(), Arg.Any<AdrPlusRepoConfig>()).Returns(1);
        _mockFileSystem.GetFullNameFile(Arg.Any<string>()).Returns(callInfo => callInfo.Arg<string>());

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

        var args = new[] { "--path", RepoPath, "--title", "My New ADR", "--open" };
        var parsedArgs = new Dictionary<Arguments, string>
        {
            { Arguments.TargetRepo, RepoPath },
            { Arguments.TitleAdr, "My New ADR" },
            { Arguments.OpenAdr, string.Empty }
        };

        SetupBasicMocks(parsedArgs, BasicJsonConfig);
        _mockAdrServices.GetFileByUniqueTitle(Arg.Any<string>(), Arg.Any<string>(), _mockFileSystem, Arg.Any<string>(), Arg.Any<AdrPlusRepoConfig>())
            .Returns(string.Empty);
        _mockAdrServices.GetNextNumber(_mockFileSystem, Arg.Any<string>(), Arg.Any<AdrPlusRepoConfig>()).Returns(1);
        _mockFileSystem.GetFullNameFile(Arg.Any<string>()).Returns(callInfo => callInfo.Arg<string>());
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

        var args = new[] { "--path", RepoPath, "--title", "My New ADR", "--open" };
        var parsedArgs = new Dictionary<Arguments, string>
        {
            { Arguments.TargetRepo, RepoPath },
            { Arguments.TitleAdr, "My New ADR" },
            { Arguments.OpenAdr, string.Empty }
        };

        SetupBasicMocks(parsedArgs, BasicJsonConfig);
        _mockAdrServices.GetFileByUniqueTitle(Arg.Any<string>(), Arg.Any<string>(), _mockFileSystem, Arg.Any<string>(), Arg.Any<AdrPlusRepoConfig>())
            .Returns(string.Empty);
        _mockAdrServices.GetNextNumber(_mockFileSystem, Arg.Any<string>(), Arg.Any<AdrPlusRepoConfig>()).Returns(1);
        _mockFileSystem.GetFullNameFile(Arg.Any<string>()).Returns(callInfo => callInfo.Arg<string>());
        _mockAdrServices.OpenFile(Arg.Any<string>(), Arg.Any<string>()).Returns(string.Empty);

        // Act
        await handler.ExecuteAsync(args, CancellationToken.None);

        // Assert: one success for ADR created, one for open success
        _mockConsole.Received(2).WriteSuccess(Arg.Any<string>());
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

        var args = new[] { "--path", RepoPath, "--title", "My New ADR", "--open" };
        var parsedArgs = new Dictionary<Arguments, string>
        {
            { Arguments.TargetRepo, RepoPath },
            { Arguments.TitleAdr, "My New ADR" },
            { Arguments.OpenAdr, string.Empty }
        };

        SetupBasicMocks(parsedArgs, BasicJsonConfig);
        _mockAdrServices.GetFileByUniqueTitle(Arg.Any<string>(), Arg.Any<string>(), _mockFileSystem, Arg.Any<string>(), Arg.Any<AdrPlusRepoConfig>())
            .Returns(string.Empty);
        _mockAdrServices.GetNextNumber(_mockFileSystem, Arg.Any<string>(), Arg.Any<AdrPlusRepoConfig>()).Returns(1);
        _mockFileSystem.GetFullNameFile(Arg.Any<string>()).Returns(callInfo => callInfo.Arg<string>());
        _mockAdrServices.OpenFile(Arg.Any<string>(), Arg.Any<string>()).Returns("open command failed");

        // Act
        await handler.ExecuteAsync(args, CancellationToken.None);

        // Assert
        _mockConsole.Received(1).WriteError(Arg.Any<string>());
    }

    [Fact]
    public async Task ExecuteAsync_WithOpenArgButNoCommandConfigured_DoesNotCallOpenFile()
    {
        // Arrange - config with no ComandOpenAdr
        var args = new[] { "--path", RepoPath, "--title", "My New ADR", "--open" };
        var parsedArgs = new Dictionary<Arguments, string>
        {
            { Arguments.TargetRepo, RepoPath },
            { Arguments.TitleAdr, "My New ADR" },
            { Arguments.OpenAdr, string.Empty }
        };

        SetupBasicMocks(parsedArgs, BasicJsonConfig);
        _mockAdrServices.GetFileByUniqueTitle(Arg.Any<string>(), Arg.Any<string>(), _mockFileSystem, Arg.Any<string>(), Arg.Any<AdrPlusRepoConfig>())
            .Returns(string.Empty);
        _mockAdrServices.GetNextNumber(_mockFileSystem, Arg.Any<string>(), Arg.Any<AdrPlusRepoConfig>()).Returns(1);
        _mockFileSystem.GetFullNameFile(Arg.Any<string>()).Returns(callInfo => callInfo.Arg<string>());

        // Act
        await _handler.ExecuteAsync(args, CancellationToken.None);

        // Assert
        _mockAdrServices.DidNotReceive().OpenFile(Arg.Any<string>(), Arg.Any<string>());
    }

    #endregion

    #region ExecuteAsync - Exception Handling Tests

    [Fact]
    public async Task ExecuteAsync_WhenExceptionOccurs_RethrowsException()
    {
        // Arrange
        var args = new[] { "--path", RepoPath, "--title", "My ADR" };
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
        var parsedArgs = new Dictionary<Arguments, string> { { Arguments.WizardNew, string.Empty } };
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
        var parsedArgs = new Dictionary<Arguments, string> { { Arguments.WizardNew, string.Empty } };
        var drives = new[] { TestPathData.SingleTestDrive };

        _mockAdrServices.ParseArgs(args, Arg.Any<Arguments[]>()).Returns(parsedArgs);
        _mockValidateConfig.HasTemplateRepoFile().Returns(true);
        _mockFileSystem.GetDrives().Returns(drives);
        _mockConsole.PromptSelectFolderRepositoryPath(true, TestPathData.SingleTestDrive, _mockFileSystem, _mockValidateConfig, Arg.Any<CancellationToken>())
            .Returns((true, string.Empty));

        // Act & Assert
        await _handler.Invoking(h => h.ExecuteAsync(args, CancellationToken.None))
            .Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task ExecuteAsync_WithWizardMode_TitlePromptAborted_ThrowsOperationCanceledException()
    {
        // Arrange
        var args = new[] { "--wizard" };
        var parsedArgs = new Dictionary<Arguments, string> { { Arguments.WizardNew, string.Empty } };
        var drives = new[] { TestPathData.SingleTestDrive };

        _mockAdrServices.ParseArgs(args, Arg.Any<Arguments[]>()).Returns(parsedArgs);
        _mockValidateConfig.HasTemplateRepoFile().Returns(true);
        _mockFileSystem.GetDrives().Returns(drives);
        _mockConsole.PromptSelectFolderRepositoryPath(Arg.Any<bool>(), Arg.Any<string>(), _mockFileSystem, _mockValidateConfig, Arg.Any<CancellationToken>())
            .Returns((false, RepoPath));
        _mockFileSystem.ReadAllTextAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(BasicJsonConfig);
        _mockValidateConfig.ValidateRepoStructure(BasicJsonConfig).Returns((true, []));
        _mockConsole.PromptEditTitleAdr(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((true, string.Empty));

        // Act & Assert
        await _handler.Invoking(h => h.ExecuteAsync(args, CancellationToken.None))
            .Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task ExecuteAsync_WithWizardMode_DatePromptAborted_ThrowsOperationCanceledException()
    {
        // Arrange
        var args = new[] { "--wizard" };
        var parsedArgs = new Dictionary<Arguments, string> { { Arguments.WizardNew, string.Empty } };
        var drives = new[] { TestPathData.SingleTestDrive };

        _mockAdrServices.ParseArgs(args, Arg.Any<Arguments[]>()).Returns(parsedArgs);
        _mockValidateConfig.HasTemplateRepoFile().Returns(true);
        _mockFileSystem.GetDrives().Returns(drives);
        _mockConsole.PromptSelectFolderRepositoryPath(Arg.Any<bool>(), Arg.Any<string>(), _mockFileSystem, _mockValidateConfig, Arg.Any<CancellationToken>())
            .Returns((false, RepoPath));
        _mockFileSystem.ReadAllTextAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(BasicJsonConfig);
        _mockValidateConfig.ValidateRepoStructure(BasicJsonConfig).Returns((true, []));
        _mockConsole.PromptEditTitleAdr(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((false, "My New ADR"));
        _mockConsole.PrompCalendar(Arg.Any<string>(), Arg.Any<DateTime>(), _config, Arg.Any<CancellationToken>())
            .Returns((true, DateTime.UtcNow));

        // Act & Assert
        await _handler.Invoking(h => h.ExecuteAsync(args, CancellationToken.None))
            .Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task ExecuteAsync_WithWizardMode_ScopePromptAborted_ThrowsOperationCanceledException()
    {
        // Arrange
        var args = new[] { "--wizard" };
        var parsedArgs = new Dictionary<Arguments, string> { { Arguments.WizardNew, string.Empty } };
        var drives = new[] { TestPathData.SingleTestDrive };

        _mockAdrServices.ParseArgs(args, Arg.Any<Arguments[]>()).Returns(parsedArgs);
        _mockValidateConfig.HasTemplateRepoFile().Returns(true);
        _mockFileSystem.GetDrives().Returns(drives);
        _mockConsole.PromptSelectFolderRepositoryPath(Arg.Any<bool>(), Arg.Any<string>(), _mockFileSystem, _mockValidateConfig, Arg.Any<CancellationToken>())
            .Returns((false, RepoPath));
        _mockFileSystem.ReadAllTextAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(ScopedJsonConfig);
        _mockValidateConfig.ValidateRepoStructure(ScopedJsonConfig).Returns((true, []));
        _mockConsole.PromptEditTitleAdr(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((false, "My New ADR"));
        _mockConsole.PrompCalendar(Arg.Any<string>(), Arg.Any<DateTime>(), _config, Arg.Any<CancellationToken>())
            .Returns((false, DateTime.UtcNow));
        _mockConsole.PromptEditScopeAdr(Arg.Any<string>(), Arg.Any<AdrPlusRepoConfig>(), Arg.Any<CancellationToken>())
            .Returns((true, string.Empty));

        // Act & Assert
        await _handler.Invoking(h => h.ExecuteAsync(args, CancellationToken.None))
            .Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task ExecuteAsync_WithWizardMode_DomainPromptAborted_ThrowsOperationCanceledException()
    {
        // Arrange
        var args = new[] { "--wizard" };
        var parsedArgs = new Dictionary<Arguments, string> { { Arguments.WizardNew, string.Empty } };
        var drives = new[] { TestPathData.SingleTestDrive };

        _mockAdrServices.ParseArgs(args, Arg.Any<Arguments[]>()).Returns(parsedArgs);
        _mockValidateConfig.HasTemplateRepoFile().Returns(true);
        _mockFileSystem.GetDrives().Returns(drives);
        _mockConsole.PromptSelectFolderRepositoryPath(Arg.Any<bool>(), Arg.Any<string>(), _mockFileSystem, _mockValidateConfig, Arg.Any<CancellationToken>())
            .Returns((false, RepoPath));
        _mockFileSystem.ReadAllTextAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(ScopedJsonConfig);
        _mockValidateConfig.ValidateRepoStructure(ScopedJsonConfig).Returns((true, []));
        _mockConsole.PromptEditTitleAdr(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((false, "My New ADR"));
        _mockConsole.PrompCalendar(Arg.Any<string>(), Arg.Any<DateTime>(), _config, Arg.Any<CancellationToken>())
            .Returns((false, DateTime.UtcNow));
        _mockConsole.PromptEditScopeAdr(Arg.Any<string>(), Arg.Any<AdrPlusRepoConfig>(), Arg.Any<CancellationToken>())
            .Returns((false, "Domain"));
        _mockConsole.PromptGetArrayDomainsAdr(_mockFileSystem, Arg.Any<string>(), _config, Arg.Any<AdrPlusRepoConfig>(), Arg.Any<CancellationToken>())
            .Returns((true, [], (Exception?)null));

        // Act & Assert
        await _handler.Invoking(h => h.ExecuteAsync(args, CancellationToken.None))
            .Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task ExecuteAsync_WithWizardMode_ConfirmationAborted_ThrowsOperationCanceledException()
    {
        // Arrange
        var args = new[] { "--wizard" };
        var parsedArgs = new Dictionary<Arguments, string> { { Arguments.WizardNew, string.Empty } };

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
        var parsedArgs = new Dictionary<Arguments, string> { { Arguments.WizardNew, string.Empty } };

        SetupWizardMocksUpToConfirmation(BasicJsonConfig);
        _mockAdrServices.ParseArgs(args, Arg.Any<Arguments[]>()).Returns(parsedArgs);

        // First confirmation is declined (No), second aborts
        _mockConsole.PromptConfirm(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((false, false), (true, false));

        // Act & Assert
        await _handler.Invoking(h => h.ExecuteAsync(args, CancellationToken.None))
            .Should().ThrowAsync<OperationCanceledException>();

        // Confirm drive/folder prompts were called at least twice (second loop iteration)
        _mockConsole.Received(2).PromptSelectFolderRepositoryPath(
            Arg.Any<bool>(), Arg.Any<string>(), _mockFileSystem, _mockValidateConfig, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WithWizardMode_ConfirmedYes_CreatesAdrFile()
    {
        // Arrange
        var args = new[] { "--wizard" };
        var parsedArgs = new Dictionary<Arguments, string> { { Arguments.WizardNew, string.Empty } };

        SetupWizardMocksUpToConfirmation(BasicJsonConfig);
        _mockAdrServices.ParseArgs(args, Arg.Any<Arguments[]>()).Returns(parsedArgs);
        _mockConsole.PromptConfirm(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((false, true));

        _mockAdrServices.GetFileByUniqueTitle(Arg.Any<string>(), Arg.Any<string>(), _mockFileSystem, Arg.Any<string>(), Arg.Any<AdrPlusRepoConfig>())
            .Returns(string.Empty);
        _mockAdrServices.GetNextNumber(_mockFileSystem, Arg.Any<string>(), Arg.Any<AdrPlusRepoConfig>()).Returns(1);
        _mockFileSystem.GetFullNameFile(Arg.Any<string>()).Returns(callInfo => callInfo.Arg<string>());

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
        var parsedArgs = new Dictionary<Arguments, string> { { Arguments.WizardNew, string.Empty } };

        SetupWizardMocksUpToConfirmation(BasicJsonConfig);
        _mockAdrServices.ParseArgs(args, Arg.Any<Arguments[]>()).Returns(parsedArgs);
        _mockConsole.PromptConfirm(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((true, false));

        // Act
        await _handler.Invoking(h => h.ExecuteAsync(args, CancellationToken.None))
            .Should().ThrowAsync<OperationCanceledException>();

        // Assert: WriteInfo called for each summary field (repo, date, title, and empty separator = 4 calls)
        _mockConsole.Received(4).WriteInfo(Arg.Any<string>());
    }

    [Fact]
    public async Task ExecuteAsync_WithWizardMode_InvalidRepoConfig_ThrowsInvalidOperationException()
    {
        // Arrange
        var args = new[] { "--wizard" };
        var parsedArgs = new Dictionary<Arguments, string> { { Arguments.WizardNew, string.Empty } };
        var drives = new[] { TestPathData.SingleTestDrive };

        _mockAdrServices.ParseArgs(args, Arg.Any<Arguments[]>()).Returns(parsedArgs);
        _mockValidateConfig.HasTemplateRepoFile().Returns(true);
        _mockFileSystem.GetDrives().Returns(drives);
        _mockConsole.PromptSelectFolderRepositoryPath(Arg.Any<bool>(), Arg.Any<string>(), _mockFileSystem, _mockValidateConfig, Arg.Any<CancellationToken>())
            .Returns((false, RepoPath));
        _mockFileSystem.ReadAllTextAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(BasicJsonConfig);
        _mockValidateConfig.ValidateRepoStructure(BasicJsonConfig).Returns((false, ["Config error"]));

        // Act & Assert
        await _handler.Invoking(h => h.ExecuteAsync(args, CancellationToken.None))
            .Should().ThrowAsync<InvalidOperationException>();
    }

    #endregion

    #region Helper Methods

    private void SetupBasicMocks(Dictionary<Arguments, string> parsedArgs, string jsonConfig)
    {
        _mockAdrServices.ParseArgs(Arg.Any<string[]>(), Arg.Any<Arguments[]>()).Returns(parsedArgs);
        _mockValidateConfig.HasTemplateRepoFile().Returns(true);
        _mockFileSystem.DirectoryExists(Arg.Any<string>()).Returns(true);
        _mockFileSystem.FileExists(Arg.Is<string>(s => s.Contains(ConfigFileName))).Returns(true);
        _mockFileSystem.ReadAllTextAsync(Arg.Is<string>(s => s.Contains(ConfigFileName)), Arg.Any<CancellationToken>()).Returns(jsonConfig);
        _mockValidateConfig.ValidateRepoStructure(jsonConfig).Returns((true, []));
        _mockFileSystem.GetFullNameFile(Arg.Any<string>()).Returns(callInfo => callInfo.Arg<string>());
    }

    private void SetupWizardMocksUpToConfirmation(string jsonConfig)
    {
        _mockValidateConfig.HasTemplateRepoFile().Returns(true);
        _mockFileSystem.GetDrives().Returns([TestPathData.SingleTestDrive]);
        _mockConsole.PromptSelectFolderRepositoryPath(Arg.Any<bool>(), Arg.Any<string>(), _mockFileSystem, _mockValidateConfig, Arg.Any<CancellationToken>())
            .Returns((false, RepoPath));
        _mockFileSystem.DirectoryExists(Arg.Any<string>()).Returns(true);
        _mockFileSystem.FileExists(Arg.Is<string>(s => s.Contains(ConfigFileName))).Returns(true);
        _mockFileSystem.ReadAllTextAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(jsonConfig);
        _mockValidateConfig.ValidateRepoStructure(jsonConfig).Returns((true, []));
        _mockConsole.PromptEditTitleAdr(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((false, "My New ADR"));
        _mockConsole.PrompCalendar(Arg.Any<string>(), Arg.Any<DateTime>(), _config, Arg.Any<CancellationToken>())
            .Returns((false, new DateTime(2026, 1, 15)));
        _mockFileSystem.GetFullNameFile(Arg.Any<string>()).Returns(callInfo => callInfo.Arg<string>());
    }

    private NewAdrCommandHandler CreateHandlerWith(AdrPlusConfig config)
    {
        return new NewAdrCommandHandler(
            _mockLogger,
            Options.Create(config),
            _mockFileSystem,
            _mockValidateConfig,
            _mockConsole,
            _mockAdrServices);
    }

    #endregion
}
