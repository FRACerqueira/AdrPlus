// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the AdrPlus project under MIT license
// ***************************************************************************************

using AdrPlus.Commands;
using AdrPlus.Commands.Migrate;
using AdrPlus.Core;
using AdrPlus.Domain;
using AdrPlus.Infrastructure.FileSystem;
using AdrPlus.Infrastructure.UI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using static AdrPlus.Tests.Helpers.TestPathData;

namespace AdrPlus.Tests.Commands.Migrate;

/// <summary>
/// Unit tests for MigrateCommandHandler class.
/// Tests demonstrate migrate command execution, validation, and ADR file migration using NSubstitute.
/// All tests are designed to run on Windows and Linux with cross-platform path handling.
/// </summary>
public class MigrateCommandHandlerTests
{
    private ILogger<MigrateCommandHandler> _mockLogger = null!;
    private IFileSystemService _mockFileSystem = null!;
    private IPromptConsole _mockConsole = null!;
    private IValidateJsonConfig _mockValidateConfig = null!;
    private IAdrServices _mockAdrServices = null!;
    private AdrPlusConfig _config = null!;
    private MigrateCommandHandler _handler = null!;

    public MigrateCommandHandlerTests()
    {
        InitializeMocks();
    }

    private void InitializeMocks()
    {
        _mockLogger = Substitute.For<ILogger<MigrateCommandHandler>>();
        _mockFileSystem = Substitute.For<IFileSystemService>();
        _mockConsole = Substitute.For<IPromptConsole>();
        _mockValidateConfig = Substitute.For<IValidateJsonConfig>();
        _mockAdrServices = Substitute.For<IAdrServices>();

        _config = new AdrPlusConfig
        {
            Language = "en-US"
        };

        _handler = new MigrateCommandHandler(
            _mockLogger,
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
        var handler = new MigrateCommandHandler(
            _mockLogger,
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

    #endregion

    #region ExecuteAsync - Null Args Tests

    [Fact]
    public async Task ExecuteAsync_WithNullArgs_ThrowsArgumentNullException()
    {
        // Act & Assert
        await _handler.Invoking(h => h.ExecuteAsync(null!, CancellationToken.None))
            .Should().ThrowAsync<ArgumentNullException>();
    }

    #endregion

    #region ExecuteAsync - Template File Not Found Tests

    [Fact]
    public async Task ExecuteAsync_WhenTemplateRepoFileNotFound_ThrowsFileNotFoundException()
    {
        // Arrange
        var args = new[] { "--path", RepositoryPath };
        var parsedArgs = new Dictionary<Arguments, string> { { Arguments.TargetRepo, RepositoryPath } };

        _mockAdrServices.ParseArgs(args, Arg.Any<Arguments[]>()).Returns(parsedArgs);
        _mockValidateConfig.HasTemplateRepoFile().Returns(false);

        // Act & Assert
        await _handler.Invoking(h => h.ExecuteAsync(args, CancellationToken.None))
            .Should().ThrowAsync<FileNotFoundException>();
    }

    #endregion

    #region ExecuteAsync - Direct Path Tests

    [Fact]
    public async Task ExecuteAsync_WithInvalidDirectory_ThrowsDirectoryNotFoundException()
    {
        // Arrange
        var args = new[] { "--path", "nonexistent/path" };
        var parsedArgs = new Dictionary<Arguments, string> { { Arguments.TargetRepo, "nonexistent/path" } };

        _mockAdrServices.ParseArgs(args, Arg.Any<Arguments[]>()).Returns(parsedArgs);
        _mockValidateConfig.HasTemplateRepoFile().Returns(true);
        _mockFileSystem.DirectoryExists(Arg.Any<string>()).Returns(false);

        // Act & Assert
        await _handler.Invoking(h => h.ExecuteAsync(args, CancellationToken.None))
            .Should().ThrowAsync<DirectoryNotFoundException>();
    }

    [Fact]
    public async Task ExecuteAsync_WithMissingConfigFile_ThrowsFileNotFoundException()
    {
        // Arrange
        var args = new[] { "--path", RepositoryPath };
        var parsedArgs = new Dictionary<Arguments, string> { { Arguments.TargetRepo, RepositoryPath } };
        var configPath = Path.Combine(RepositoryPath, ".adrplus");

        _mockAdrServices.ParseArgs(args, Arg.Any<Arguments[]>()).Returns(parsedArgs);
        _mockValidateConfig.HasTemplateRepoFile().Returns(true);
        _mockFileSystem.DirectoryExists(RepositoryPath).Returns(true);
        _mockValidateConfig.GetFileNameRepoConfig().Returns(".adrplus");
        _mockFileSystem.FileExists(configPath).Returns(false);

        // Act & Assert
        await _handler.Invoking(h => h.ExecuteAsync(args, CancellationToken.None))
            .Should().ThrowAsync<FileNotFoundException>();
    }

    [Fact]
    public async Task ExecuteAsync_WithInvalidConfigStructure_ThrowsInvalidDataException()
    {
        // Arrange
        var args = new[] { "--path", RepositoryPath };
        var parsedArgs = new Dictionary<Arguments, string> { { Arguments.TargetRepo, RepositoryPath } };
        var jsonConfig = """{"Invalid": "config"}""";
        var errors = new[] { "Config error" };

        _mockAdrServices.ParseArgs(args, Arg.Any<Arguments[]>()).Returns(parsedArgs);
        _mockValidateConfig.HasTemplateRepoFile().Returns(true);
        _mockFileSystem.DirectoryExists(RepositoryPath).Returns(true);
        _mockValidateConfig.GetFileNameRepoConfig().Returns(".adrplus");
        _mockFileSystem.FileExists(Arg.Is<string>(s => s.EndsWith(".adrplus"))).Returns(true);
        _mockFileSystem.ReadAllTextAsync(Arg.Is<string>(s => s.EndsWith(".adrplus")), Arg.Any<CancellationToken>()).Returns(jsonConfig);
        _mockValidateConfig.ValidateRepoStructure(jsonConfig).Returns((false, errors));

        // Act & Assert
        await _handler.Invoking(h => h.ExecuteAsync(args, CancellationToken.None))
            .Should().ThrowAsync<InvalidDataException>();
        _mockConsole.Received(1).PromptWriteError("Config error");
    }

    #endregion

    #region ExecuteAsync - Migration Logic Tests

    [Fact]
    public async Task ExecuteAsync_WithUnknownStatusAndInvalidHeader_MigratesFile()
    {
        // Arrange - StatusCreate=Unknown, invalid header ? should migrate
        var args = new[] { "--path", RepositoryPath };
        var parsedArgs = new Dictionary<Arguments, string> { { Arguments.TargetRepo, RepositoryPath } };
        var jsonConfig = """{"Prefix": "ADR", "LenSeq": 4, "LenVersion": 2}""";

        _mockAdrServices.ParseArgs(args, Arg.Any<Arguments[]>()).Returns(parsedArgs);
        _mockValidateConfig.HasTemplateRepoFile().Returns(true);
        _mockFileSystem.DirectoryExists(RepositoryPath).Returns(true);
        _mockValidateConfig.GetFileNameRepoConfig().Returns(".adrplus");
        _mockFileSystem.FileExists(Arg.Is<string>(s => s.EndsWith(".adrplus"))).Returns(true);
        _mockFileSystem.ReadAllTextAsync(Arg.Is<string>(s => s.EndsWith(".adrplus")), Arg.Any<CancellationToken>()).Returns(jsonConfig);
        _mockValidateConfig.ValidateRepoStructure(jsonConfig).Returns((true, []));

        var testFile = new AdrFileNameComponents
        {
            FileName = Path.Combine(RepositoryPath, "adr-0001.md"),
            IsValid = true,
            Number = 1,
            Title = "Test ADR",
            ContentAdr = "## Context\n\nTest decision.",
            Header = new AdrHeader
            {
                IsValid = false,
                StatusCreate = AdrStatus.Unknown,
                IsMigrated = false,
                StatusUpdate = AdrStatus.Unknown,
                StatusChange = AdrStatus.Unknown,
                Version = 1
            }
        };

        _mockAdrServices.ReadAllAdr(_mockFileSystem, RepositoryPath, Arg.Any<AdrPlusRepoConfig>(), true).Returns([testFile]);
        _mockFileSystem.ReadAllTextAsync(testFile.FileName, Arg.Any<CancellationToken>()).Returns("## Context\n\nContent.");
        _mockFileSystem.WriteAllTextAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);

        // Act
        await _handler.ExecuteAsync(args, CancellationToken.None);

        // Assert
        await _mockFileSystem.Received(1).ReadAllTextAsync(testFile.FileName, Arg.Any<CancellationToken>());
        await _mockFileSystem.Received(1).WriteAllTextAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        _mockConsole.Received(1).PromptWriteSuccess(Arg.Any<string>());
    }

    [Fact]
    public async Task ExecuteAsync_WithNonUnknownStatusCode_SkipsFile()
    {
        // Arrange - StatusCreate=Proposed (not Unknown) ? should skip
        var args = new[] { "--path", RepositoryPath };
        var parsedArgs = new Dictionary<Arguments, string> { { Arguments.TargetRepo, RepositoryPath } };
        var jsonConfig = """{"Prefix": "ADR", "LenSeq": 4, "LenVersion": 2}""";

        _mockAdrServices.ParseArgs(args, Arg.Any<Arguments[]>()).Returns(parsedArgs);
        _mockValidateConfig.HasTemplateRepoFile().Returns(true);
        _mockFileSystem.DirectoryExists(RepositoryPath).Returns(true);
        _mockValidateConfig.GetFileNameRepoConfig().Returns(".adrplus");
        _mockFileSystem.FileExists(Arg.Is<string>(s => s.EndsWith(".adrplus"))).Returns(true);
        _mockFileSystem.ReadAllTextAsync(Arg.Is<string>(s => s.EndsWith(".adrplus")), Arg.Any<CancellationToken>()).Returns(jsonConfig);
        _mockValidateConfig.ValidateRepoStructure(jsonConfig).Returns((true, []));

        var testFile = new AdrFileNameComponents
        {
            FileName = Path.Combine(RepositoryPath, "adr-0001.md"),
            IsValid = true,
            Number = 1,
            Header = new AdrHeader
            {
                IsValid = false,
                StatusCreate = AdrStatus.Proposed,
                IsMigrated = false,
                StatusUpdate = AdrStatus.Unknown,
                StatusChange = AdrStatus.Unknown
            }
        };

        _mockAdrServices.ReadAllAdr(_mockFileSystem, RepositoryPath, Arg.Any<AdrPlusRepoConfig>(), true).Returns([testFile]);

        // Act
        await _handler.ExecuteAsync(args, CancellationToken.None);

        // Assert
        await _mockFileSystem.DidNotReceive().WriteAllTextAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        _mockConsole.DidNotReceive().PromptWriteSuccess(Arg.Any<string>());
    }

    [Fact]
    public async Task ExecuteAsync_WithInvalidAdrFile_SkipsFile()
    {
        // Arrange - file with IsValid=false should be skipped
        var args = new[] { "--path", RepositoryPath };
        var parsedArgs = new Dictionary<Arguments, string> { { Arguments.TargetRepo, RepositoryPath } };
        var jsonConfig = """{"Prefix": "ADR", "LenSeq": 4, "LenVersion": 2}""";

        _mockAdrServices.ParseArgs(args, Arg.Any<Arguments[]>()).Returns(parsedArgs);
        _mockValidateConfig.HasTemplateRepoFile().Returns(true);
        _mockFileSystem.DirectoryExists(RepositoryPath).Returns(true);
        _mockValidateConfig.GetFileNameRepoConfig().Returns(".adrplus");
        _mockFileSystem.FileExists(Arg.Is<string>(s => s.EndsWith(".adrplus"))).Returns(true);
        _mockFileSystem.ReadAllTextAsync(Arg.Is<string>(s => s.EndsWith(".adrplus")), Arg.Any<CancellationToken>()).Returns(jsonConfig);
        _mockValidateConfig.ValidateRepoStructure(jsonConfig).Returns((true, []));

        var invalidFile = new AdrFileNameComponents
        {
            FileName = Path.Combine(RepositoryPath, "invalid.md"),
            IsValid = false, // Invalid file
            Number = 1,
            Header = new AdrHeader { IsValid = false }
        };

        _mockAdrServices.ReadAllAdr(_mockFileSystem, RepositoryPath, Arg.Any<AdrPlusRepoConfig>(), true).Returns([invalidFile]);

        // Act & Assert - should throw InvalidDataException because no valid files found
        await _handler.Invoking(h => h.ExecuteAsync(args, CancellationToken.None))
            .Should().ThrowAsync<InvalidDataException>();
    }

    [Fact]
    public async Task ExecuteAsync_WithAlreadyMigratedFile_SkipsFile()
    {
        // Arrange - file already migrated (IsMigrated=true) should be skipped
        var args = new[] { "--path", RepositoryPath };
        var parsedArgs = new Dictionary<Arguments, string> { { Arguments.TargetRepo, RepositoryPath } };
        var jsonConfig = """{"Prefix": "ADR", "LenSeq": 4, "LenVersion": 2}""";

        _mockAdrServices.ParseArgs(args, Arg.Any<Arguments[]>()).Returns(parsedArgs);
        _mockValidateConfig.HasTemplateRepoFile().Returns(true);
        _mockFileSystem.DirectoryExists(RepositoryPath).Returns(true);
        _mockValidateConfig.GetFileNameRepoConfig().Returns(".adrplus");
        _mockFileSystem.FileExists(Arg.Is<string>(s => s.EndsWith(".adrplus"))).Returns(true);
        _mockFileSystem.ReadAllTextAsync(Arg.Is<string>(s => s.EndsWith(".adrplus")), Arg.Any<CancellationToken>()).Returns(jsonConfig);
        _mockValidateConfig.ValidateRepoStructure(jsonConfig).Returns((true, []));

        var migratedFile = new AdrFileNameComponents
        {
            FileName = Path.Combine(RepositoryPath, "adr-0001.md"),
            IsValid = true,
            Number = 1,
            Header = new AdrHeader
            {
                IsValid = true, // Already valid header = already migrated
                IsMigrated = true,
                StatusCreate = AdrStatus.Proposed,
                StatusUpdate = AdrStatus.Unknown,
                StatusChange = AdrStatus.Unknown
            }
        };

        _mockAdrServices.ReadAllAdr(_mockFileSystem, RepositoryPath, Arg.Any<AdrPlusRepoConfig>(), true).Returns([migratedFile]);

        // Act
        await _handler.ExecuteAsync(args, CancellationToken.None);

        // Assert - no write operations, already migrated
        await _mockFileSystem.DidNotReceive().WriteAllTextAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        _mockConsole.DidNotReceive().PromptWriteSuccess(Arg.Any<string>());
    }

    #endregion

    #region ExecuteAsync - Wizard Mode Tests

    [Fact]
    public async Task ExecuteAsync_WithWizardModeDriveSelectionAborted_ThrowsOperationCanceledException()
    {
        // Arrange
        var args = new[] { "--wizard" };
        var parsedArgs = new Dictionary<Arguments, string> { { Arguments.WizardMigrate, string.Empty } };
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
    public async Task ExecuteAsync_WithWizardModeAdrSelectionAborted_ThrowsOperationCanceledException()
    {
        // Arrange - test when user aborts during ADR selection
        var args = new[] { "--wizard" };
        var parsedArgs = new Dictionary<Arguments, string> { { Arguments.WizardMigrate, string.Empty } };
        var drives = TestDrives;
        var jsonConfig = """{"Prefix": "ADR", "LenSeq": 4, "LenVersion": 2}""";

        _mockAdrServices.ParseArgs(args, Arg.Any<Arguments[]>()).Returns(parsedArgs);
        _mockValidateConfig.HasTemplateRepoFile().Returns(true);
        _mockFileSystem.GetDrives().Returns(drives);
        _mockConsole.PromptSelectLogicalDrive(Arg.Any<string>(), _mockFileSystem, Arg.Any<CancellationToken>())
            .Returns((false, SingleTestDrive));
        _mockConsole.PromptSelectFolderPath(Arg.Any<string>(),Arg.Any<bool>(), Arg.Any<string>(), _mockFileSystem, _mockValidateConfig, Arg.Any<CancellationToken>())
            .Returns((false, RepositoryPath));
        _mockFileSystem.DirectoryExists(RepositoryPath).Returns(true);
        _mockValidateConfig.GetFileNameRepoConfig().Returns(".adrplus");
        _mockFileSystem.FileExists(Arg.Is<string>(s => s.EndsWith(".adrplus"))).Returns(true);
        _mockFileSystem.ReadAllTextAsync(Arg.Is<string>(s => s.EndsWith(".adrplus")), Arg.Any<CancellationToken>()).Returns(jsonConfig);
        _mockValidateConfig.ValidateRepoStructure(jsonConfig).Returns((true, []));

        var adrFile = CreateAdrFileComponentForMigration(Path.Combine(RepositoryPath, "adr-0001.md"), 1, AdrStatus.Unknown, false);
        _mockAdrServices.ReadAllAdr(_mockFileSystem, RepositoryPath, Arg.Any<AdrPlusRepoConfig>(), true).Returns([adrFile]);
        _mockConsole.PromptShowAdrsMigrations(Arg.Any<AdrFileNameComponents[]>(), Arg.Any<AdrPlusRepoConfig>(), Arg.Any<CancellationToken>())
            .Returns((true, 0)); // IsAborted = true

        // Act & Assert
        await _handler.Invoking(h => h.ExecuteAsync(args, CancellationToken.None))
            .Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task ExecuteAsync_WithWizardModeConfirmYes_CompleteMigration()
    {
        // Arrange - complete wizard flow with confirmation YES
        var args = new[] { "--wizard" };
        var parsedArgs = new Dictionary<Arguments, string> { { Arguments.WizardMigrate, string.Empty } };
        var drives = TestDrives;
        var jsonConfig = """{"Prefix": "ADR", "LenSeq": 4, "LenVersion": 2}""";

        _mockAdrServices.ParseArgs(args, Arg.Any<Arguments[]>()).Returns(parsedArgs);
        _mockValidateConfig.HasTemplateRepoFile().Returns(true);
        _mockFileSystem.GetDrives().Returns(drives);
        _mockConsole.PromptSelectLogicalDrive(Arg.Any<string>(), _mockFileSystem, Arg.Any<CancellationToken>())
            .Returns((false, SingleTestDrive));
        _mockConsole.PromptSelectFolderPath(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<string>(), _mockFileSystem, _mockValidateConfig, Arg.Any<CancellationToken>())
            .Returns((false, RepositoryPath));
        _mockFileSystem.DirectoryExists(RepositoryPath).Returns(true);
        _mockValidateConfig.GetFileNameRepoConfig().Returns(".adrplus");
        _mockFileSystem.FileExists(Arg.Is<string>(s => s.EndsWith(".adrplus"))).Returns(true);
        _mockFileSystem.ReadAllTextAsync(Arg.Is<string>(s => s.EndsWith(".adrplus")), Arg.Any<CancellationToken>()).Returns(jsonConfig);
        _mockValidateConfig.ValidateRepoStructure(jsonConfig).Returns((true, []));

        var adrFile = CreateAdrFileComponentForMigration(Path.Combine(RepositoryPath, "adr-0001.md"), 1, AdrStatus.Unknown, false);
        _mockAdrServices.ReadAllAdr(_mockFileSystem, RepositoryPath, Arg.Any<AdrPlusRepoConfig>(), true).Returns([adrFile]);
        _mockConsole.PromptShowAdrsMigrations(Arg.Any<AdrFileNameComponents[]>(), Arg.Any<AdrPlusRepoConfig>(), Arg.Any<CancellationToken>())
            .Returns((false, 1));
        _mockConsole.PromptConfirm(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((false, true)); // Confirm YES
        _mockFileSystem.ReadAllTextAsync(Arg.Is<string>(s => !s.EndsWith(".adrplus")), Arg.Any<CancellationToken>()).Returns("## Context\n\nDecision.");
        _mockFileSystem.WriteAllTextAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);

        // Act
        await _handler.ExecuteAsync(args, CancellationToken.None);

        // Assert
        _mockConsole.Received(1).PromptWriteSuccess(Arg.Any<string>());
        await _mockFileSystem.Received(1).WriteAllTextAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WithWizardModeMultipleDrives_SelectsDriveAndContinues()
    {
        // Arrange - test with multiple drives available
        var args = new[] { "--wizard" };
        var parsedArgs = new Dictionary<Arguments, string> { { Arguments.WizardMigrate, string.Empty } };
        var multipleDrives = TestDrives.Length > 1 ? TestDrives : ["C:", "D:"];
        var jsonConfig = """{"Prefix": "ADR", "LenSeq": 4, "LenVersion": 2}""";

        _mockAdrServices.ParseArgs(args, Arg.Any<Arguments[]>()).Returns(parsedArgs);
        _mockValidateConfig.HasTemplateRepoFile().Returns(true);
        _mockFileSystem.GetDrives().Returns(multipleDrives);
        _mockConsole.PromptSelectLogicalDrive(Arg.Any<string>(), _mockFileSystem, Arg.Any<CancellationToken>())
            .Returns((false, multipleDrives[1]));
        _mockConsole.PromptSelectFolderPath(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<string>(), _mockFileSystem, _mockValidateConfig, Arg.Any<CancellationToken>())
            .Returns((false, RepositoryPath));
        _mockFileSystem.DirectoryExists(RepositoryPath).Returns(true);
        _mockValidateConfig.GetFileNameRepoConfig().Returns(".adrplus");
        _mockFileSystem.FileExists(Arg.Is<string>(s => s.EndsWith(".adrplus"))).Returns(true);
        _mockFileSystem.ReadAllTextAsync(Arg.Is<string>(s => s.EndsWith(".adrplus")), Arg.Any<CancellationToken>()).Returns(jsonConfig);
        _mockValidateConfig.ValidateRepoStructure(jsonConfig).Returns((true, []));

        var adrFile = CreateAdrFileComponentForMigration(Path.Combine(RepositoryPath, "adr-0001.md"), 1, AdrStatus.Unknown, false);
        _mockAdrServices.ReadAllAdr(_mockFileSystem, RepositoryPath, Arg.Any<AdrPlusRepoConfig>(), true).Returns([adrFile]);
        _mockConsole.PromptShowAdrsMigrations(Arg.Any<AdrFileNameComponents[]>(), Arg.Any<AdrPlusRepoConfig>(), Arg.Any<CancellationToken>())
            .Returns((false, 1));
        _mockConsole.PromptConfirm(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((false, true));
        _mockFileSystem.ReadAllTextAsync(Arg.Is<string>(s => !s.EndsWith(".adrplus")), Arg.Any<CancellationToken>()).Returns("## Context\n\nDecision.");
        _mockFileSystem.WriteAllTextAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);

        // Act
        await _handler.ExecuteAsync(args, CancellationToken.None);

        // Assert
        _mockConsole.Received(1).PromptSelectLogicalDrive(Arg.Any<string>(), _mockFileSystem, Arg.Any<CancellationToken>());
        _mockConsole.Received(1).PromptWriteSuccess(Arg.Any<string>());
    }

    #endregion

    #region Cancellation Tests

    [Fact]
    public async Task ExecuteAsync_WhenCancelled_ThrowsOperationCanceledException()
    {
        // Arrange
        var args = new[] { "--path", RepositoryPath };
        var parsedArgs = new Dictionary<Arguments, string> { { Arguments.TargetRepo, RepositoryPath } };
        var cts = new CancellationTokenSource();
        cts.Cancel();

        _mockAdrServices.ParseArgs(args, Arg.Any<Arguments[]>()).Returns(parsedArgs);
        _mockValidateConfig.When(x => x.HasTemplateRepoFile()).Do(_ => throw new OperationCanceledException());

        // Act & Assert
        await _handler.Invoking(h => h.ExecuteAsync(args, cts.Token))
            .Should().ThrowAsync<OperationCanceledException>();
    }

    #endregion

    #region Exception Handling Tests

    [Fact]
    public async Task ExecuteAsync_WhenExceptionOccurs_RethrowsException()
    {
        // Arrange
        var args = new[] { "--path", RepositoryPath };
        var parsedArgs = new Dictionary<Arguments, string> { { Arguments.TargetRepo, RepositoryPath } };
        var exception = new InvalidOperationException("Test exception");

        _mockAdrServices.ParseArgs(args, Arg.Any<Arguments[]>()).Returns(parsedArgs);
        _mockValidateConfig.When(x => x.HasTemplateRepoFile()).Do(_ => throw exception);

        // Act & Assert
        await _handler.Invoking(h => h.ExecuteAsync(args, CancellationToken.None))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Test exception");
    }

    #endregion


    #region Error Logging and Validation Tests

    #endregion

    #region Helper Methods

    private static AdrFileNameComponents CreateAdrFileComponentForMigration(
        string fileName,
        int number,
        AdrStatus statusCreate,
        bool isMigrated,
        string scope = "default",
        int? revision = null)
    {
        return new AdrFileNameComponents
        {
            FileName = fileName,
            IsValid = true,
            Number = number,
            Title = "Use New Database",
            ContentAdr = "## Context\n\nDecision content.",
            Header = new AdrHeader
            {
                IsValid = isMigrated,
                Title = "Use New Database",
                Scope = scope,
                StatusCreate = statusCreate,
                StatusUpdate = AdrStatus.Unknown,
                StatusChange = AdrStatus.Unknown,
                IsMigrated = isMigrated,
                Version = 1,
                Revision = revision ?? 0
            }
        };
    }

    #endregion
}
