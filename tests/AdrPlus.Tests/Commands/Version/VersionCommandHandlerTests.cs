// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the AdrPlus project under MIT license
// ***************************************************************************************

using AdrPlus.Commands;
using AdrPlus.Commands.Version;
using AdrPlus.Core;
using AdrPlus.Domain;
using AdrPlus.Infrastructure.FileSystem;
using AdrPlus.Infrastructure.Logging;
using AdrPlus.Infrastructure.UI;
using AdrPlus.Tests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace AdrPlus.Tests.Commands.Version
{
    /// <summary>
    /// Unit tests for VersionCommandHandler class.
    /// Tests cover command execution, wizard flows, validation, and error handling using NSubstitute.
    /// </summary>
    public class VersionCommandHandlerTests
    {
        private readonly ILogger<VersionCommandHandler> _mockLogger;
        private readonly IFileSystemService _mockFileSystem;
        private readonly IPromptConsole _mockConsole;
        private readonly IValidateJsonConfig _mockValidateConfig;
        private readonly IAdrServices _mockAdrServices;
        private readonly AdrPlusConfig _config;
        private readonly VersionCommandHandler _handler;

        private const string ConfigFileName = ".adrplus";
        private const string RepoPath = "/repo";
        private const string AdrFileName = "ADR0001V01-test.md";
        private const string AdrFilePath = "/repo/adr/ADR0001V01-test.md";
            
        private static readonly string BasicJsonConfig =
            """{"Prefix": "ADR", "LenSeq": 4, "LenVersion": 2, "LenRevision": 1, "FolderAdr": "adr", "StatusNew": "Proposed", "StatusAcc": "Accepted", "StatusRej": "Rejected", "StatusSup": "Superseded", "template":"# ADR"}""";

        public VersionCommandHandlerTests()
        {
            _mockLogger = Substitute.For<ILogger<VersionCommandHandler>>();
            _mockFileSystem = Substitute.For<IFileSystemService>();
            _mockConsole = Substitute.For<IPromptConsole>();
            _mockValidateConfig = Substitute.For<IValidateJsonConfig>();
            _mockAdrServices = Substitute.For<IAdrServices>();

            _config = new AdrPlusConfig
            {
                Language = "en-US",
                ComandOpenAdr = "notepad {0}"
            };

            _mockValidateConfig.GetFileNameRepoConfig().Returns(ConfigFileName);

            _handler = new VersionCommandHandler(
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
            var handler = new VersionCommandHandler(
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
        public async Task ExecuteAsync_WithHelpArgument_DisplaysHelpAndReturns()
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

            _mockFileSystem.FileExists(Arg.Any<string>()).Returns(callInfo =>
            {
                var path = Path.GetFullPath(callInfo.Arg<string>());
                if (path == normalizedAdrPath)
                {
                    return true;
                }
                return path.EndsWith(".adrplus");
            });

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

            _mockValidateConfig.ValidateRepoStructure(Arg.Any<string>()).Returns((false, errors));

            // Act
            await _handler.Invoking(h => h.ExecuteAsync(args, CancellationToken.None))
                .Should().ThrowAsync<InvalidDataException>();

            // Assert
            _mockConsole.Received(1).PromptWriteError("Error one");
            _mockConsole.Received(1).PromptWriteError("Error two");
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

            var invalidAdr = CommandHandlerMockHelper.CreateInvalidAdrFileNameComponents("Invalid file name");
            _mockAdrServices.ParseFileName(AdrFilePath, Arg.Any<AdrPlusRepoConfig>(), _mockFileSystem).ReturnsForAnyArgs(invalidAdr);

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
                    StatusUpdate = AdrStatus.Proposed,
                    StatusCreate = AdrStatus.Proposed,
                    StatusChange = AdrStatus.Unknown,
                    Title = "Test ADR"
                },
                ContentAdr = "Test content"
            };

            _mockAdrServices.ParseFileName(AdrFilePath, Arg.Any<AdrPlusRepoConfig>(), _mockFileSystem)
                .ReturnsForAnyArgs(infoadr);

            _mockAdrServices.ReadAllAdrByNumber(1, _mockFileSystem, RepoPath, Arg.Any<AdrPlusRepoConfig>())
                .ReturnsForAnyArgs([]);

            // Act & Assert
            await _handler.Invoking(h => h.ExecuteAsync(args, CancellationToken.None))
                .Should().ThrowAsync<InvalidDataException>();
        }

        [Fact]
        public async Task ExecuteAsync_WhenAdrHasSupersededStatus_ThrowsInvalidDataException()
        {
            // Arrange
            var configPath = "/repo/.adrplus";
            var args = new[] { "--file", AdrFilePath };
            var parsedArgs = new Dictionary<Arguments, string>
            {
                { Arguments.FileAdr, AdrFilePath }
            };

            var infoadr = CommandHandlerMockHelper.CreateValidAdrFileNameComponents(
                AdrFileName,
                AdrStatus.Accepted);
            infoadr.Number = 1;

            var supersededAdr = CommandHandlerMockHelper.CreateValidAdrFileNameComponents(AdrFileName, AdrStatus.Accepted);
            supersededAdr.Header.StatusChange = AdrStatus.Superseded;

            // Setup: config path mocks
            _mockAdrServices.ParseArgs(Arg.Any<string[]>(), Arg.Any<Arguments[]>()).Returns(parsedArgs);
            _mockValidateConfig.HasTemplateRepoFile().Returns(true);
            _mockValidateConfig.GetFileNameRepoConfig().Returns(ConfigFileName);

            // Setup: file system path mocks (complex logic required)
            var configNormalized = Path.GetFullPath(configPath);
            var adrNormalized = Path.GetFullPath(AdrFilePath);
            _mockFileSystem.FileExists(Arg.Any<string>()).Returns(callInfo =>
            {
                var path = callInfo.Arg<string>();
                var normalized = Path.GetFullPath(path);
                if (normalized == configNormalized || normalized == adrNormalized) return true;
                return path.EndsWith(".adrplus");
            });

            _mockFileSystem.ReadAllTextAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(callInfo =>
                {
                    var path = callInfo.Arg<string>();
                    return Task.FromResult(path.EndsWith(".adrplus") ? BasicJsonConfig : "");
                });
            _mockValidateConfig.ValidateRepoStructure(BasicJsonConfig).Returns((true, []));
            _mockFileSystem.GetFileRootRepositoryPath(Arg.Any<string>()).Returns(configPath);
            _mockFileSystem.GetFullNameDirectoryByFile(Arg.Any<string>()).Returns("/repo/adr");
            _mockFileSystem.GetFullNameFile(Arg.Any<string>()).Returns(AdrFilePath);

            // Setup: console mocks
            var cursorPos = (0, 0);
            _mockConsole.PromptGetCursorPosition().Returns(cursorPos);
            _mockConsole.PromptWriteWait(Arg.Any<string>());
            _mockConsole.PromptClearWaitText(cursorPos);
            _mockConsole.PromptWriteError(Arg.Any<string>());

            // Setup: config parsing
            var repoConfig = CreateMockAdrPlusRepoConfig();
            _mockAdrServices.FromJson(Arg.Any<string>(), Arg.Any<string>()).Returns(repoConfig);

            // Setup: Test-specific mocks that cause the validation error
#pragma warning disable CS8620 // Argument cannot be used for parameter due to differences in the nullability of reference types.
            _mockAdrServices.ParseFileName(Path.GetFullPath(AdrFilePath), Arg.Any<AdrPlusRepoConfig>(), _mockFileSystem)
                .Returns(Task.FromResult<AdrFileNameComponents?>(infoadr));
#pragma warning restore CS8620 // Argument cannot be used for parameter due to differences in the nullability of reference types.
            _mockAdrServices.GetLatestADRSequence(1, _mockFileSystem, "/repo/adr", Arg.Any<AdrPlusRepoConfig>())
                .Returns(Task.FromResult<AdrFileNameComponents?>(supersededAdr));
            _mockAdrServices.ReadAllAdrByNumber(1, _mockFileSystem, "/repo/adr", Arg.Any<AdrPlusRepoConfig>())
                .Returns([supersededAdr]);

            // Act & Assert
            await _handler.Invoking(h => h.ExecuteAsync(args, CancellationToken.None))
                .Should().ThrowAsync<InvalidDataException>();
        }

        [Fact]
        public async Task ExecuteAsync_WhenAdrHasUnknownStatusNotMigrated_ThrowsInvalidDataException()
        {
            // Arrange
            var configPath = "/repo/.adrplus";
            var args = new[] { "--file", AdrFilePath };
            var parsedArgs = new Dictionary<Arguments, string>
            {
                { Arguments.FileAdr, AdrFilePath }
            };

            var infoadr = CommandHandlerMockHelper.CreateValidAdrFileNameComponents(
                AdrFileName,
                AdrStatus.Accepted);
            infoadr.Number = 1;

            var unknownStatusAdr = CommandHandlerMockHelper.CreateValidAdrFileNameComponents(AdrFileName, AdrStatus.Accepted);
            unknownStatusAdr.Header.StatusUpdate = AdrStatus.Unknown;
            unknownStatusAdr.Header.IsMigrated = false;
            unknownStatusAdr.Header.StatusChange = AdrStatus.Unknown;

            // Setup: config path mocks
            _mockAdrServices.ParseArgs(Arg.Any<string[]>(), Arg.Any<Arguments[]>()).Returns(parsedArgs);
            _mockValidateConfig.HasTemplateRepoFile().Returns(true);
            _mockValidateConfig.GetFileNameRepoConfig().Returns(ConfigFileName);

            // Setup: file system path mocks (complex logic required)
            var configNormalized = Path.GetFullPath(configPath);
            var adrNormalized = Path.GetFullPath(AdrFilePath);
            _mockFileSystem.FileExists(Arg.Any<string>()).Returns(callInfo =>
            {
                var path = callInfo.Arg<string>();
                var normalized = Path.GetFullPath(path);
                if (normalized == configNormalized || normalized == adrNormalized) return true;
                return path.EndsWith(".adrplus");
            });

            _mockFileSystem.ReadAllTextAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(callInfo =>
                {
                    var path = callInfo.Arg<string>();
                    return Task.FromResult(path.EndsWith(".adrplus") ? BasicJsonConfig : "");
                });
            _mockValidateConfig.ValidateRepoStructure(BasicJsonConfig).Returns((true, []));
            _mockFileSystem.GetFileRootRepositoryPath(Arg.Any<string>()).Returns(configPath);
            _mockFileSystem.GetFullNameDirectoryByFile(Arg.Any<string>()).Returns("/repo/adr");
            _mockFileSystem.GetFullNameFile(Arg.Any<string>()).Returns(AdrFilePath);

            // Setup: console mocks
            var cursorPos = (0, 0);
            _mockConsole.PromptGetCursorPosition().Returns(cursorPos);
            _mockConsole.PromptWriteWait(Arg.Any<string>());
            _mockConsole.PromptClearWaitText(cursorPos);
            _mockConsole.PromptWriteError(Arg.Any<string>());

            // Setup: config parsing
            var repoConfig = CreateMockAdrPlusRepoConfig();
            _mockAdrServices.FromJson(Arg.Any<string>(), Arg.Any<string>()).Returns(repoConfig);

            // Setup: Test-specific mocks that cause the validation error
#pragma warning disable CS8620 // Argument cannot be used for parameter due to differences in the nullability of reference types.
            _mockAdrServices.ParseFileName(Path.GetFullPath(AdrFilePath), Arg.Any<AdrPlusRepoConfig>(), _mockFileSystem)
                .Returns(Task.FromResult<AdrFileNameComponents?>(infoadr));
#pragma warning restore CS8620 // Argument cannot be used for parameter due to differences in the nullability of reference types.
            _mockAdrServices.GetLatestADRSequence(1, _mockFileSystem, "/repo/adr", Arg.Any<AdrPlusRepoConfig>())
                .Returns(Task.FromResult<AdrFileNameComponents?>(unknownStatusAdr));
            _mockAdrServices.ReadAllAdrByNumber(1, _mockFileSystem, "/repo/adr", Arg.Any<AdrPlusRepoConfig>())
                .Returns([unknownStatusAdr]);

            // Act & Assert
            await _handler.Invoking(h => h.ExecuteAsync(args, CancellationToken.None))
                .Should().ThrowAsync<InvalidDataException>();
        }

        #endregion

        #region ExecuteAsync - Latest Version Validation Tests

        [Fact]
        public async Task ExecuteAsync_WhenLatestAdrNotFound_ThrowsInvalidDataException()
        {
            // Arrange
            var configPath = "/repo/.adrplus";
            var args = new[] { "--file", AdrFilePath };
            var parsedArgs = new Dictionary<Arguments, string>
            {
                { Arguments.FileAdr, AdrFilePath }
            };

            var infoadr = CreateTestAdrFileNameComponents(AdrFileName, AdrStatus.Accepted, number: 1);
            var invalidLatest = new AdrFileNameComponents { IsValid = false, ErrorMessage = "Not found" };

            // Setup: config path mocks
            _mockAdrServices.ParseArgs(Arg.Any<string[]>(), Arg.Any<Arguments[]>()).Returns(parsedArgs);
            _mockValidateConfig.HasTemplateRepoFile().Returns(true);
            _mockValidateConfig.GetFileNameRepoConfig().Returns(ConfigFileName);

            // Setup: file system path mocks (complex logic required)
            var configNormalized = Path.GetFullPath(configPath);
            var adrNormalized = Path.GetFullPath(AdrFilePath);
            _mockFileSystem.FileExists(Arg.Any<string>()).Returns(callInfo =>
            {
                var path = callInfo.Arg<string>();
                var normalized = Path.GetFullPath(path);
                if (normalized == configNormalized || normalized == adrNormalized) return true;
                return path.EndsWith(".adrplus");
            });

            _mockFileSystem.ReadAllTextAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(callInfo =>
                {
                    var path = callInfo.Arg<string>();
                    return Task.FromResult(path.EndsWith(".adrplus") ? BasicJsonConfig : "");
                });
            _mockValidateConfig.ValidateRepoStructure(BasicJsonConfig).Returns((true, []));
            _mockFileSystem.GetFileRootRepositoryPath(Arg.Any<string>()).Returns(configPath);
            _mockFileSystem.GetFullNameDirectoryByFile(Arg.Any<string>()).Returns("/repo/adr");
            _mockFileSystem.GetFullNameFile(Arg.Any<string>()).Returns(AdrFilePath);

            // Setup: console mocks
            var cursorPos = (0, 0);
            _mockConsole.PromptGetCursorPosition().Returns(cursorPos);
            _mockConsole.PromptWriteWait(Arg.Any<string>());
            _mockConsole.PromptClearWaitText(cursorPos);

            // Setup: config parsing
            var repoConfig = CreateMockAdrPlusRepoConfig();
            _mockAdrServices.FromJson(Arg.Any<string>(), Arg.Any<string>()).Returns(repoConfig);

            // Setup: Test-specific mocks that cause the validation error
#pragma warning disable CS8620 // Argument cannot be used for parameter due to differences in the nullability of reference types.
            _mockAdrServices.ParseFileName(Path.GetFullPath(AdrFilePath), Arg.Any<AdrPlusRepoConfig>(), _mockFileSystem)
                .Returns(Task.FromResult<AdrFileNameComponents?>(infoadr));
#pragma warning restore CS8620 // Argument cannot be used for parameter due to differences in the nullability of reference types.
            _mockAdrServices.GetLatestADRSequence(1, _mockFileSystem, "/repo/adr", Arg.Any<AdrPlusRepoConfig>())
                .Returns(Task.FromResult<AdrFileNameComponents?>(invalidLatest));
            _mockAdrServices.ReadAllAdrByNumber(1, _mockFileSystem, "/repo/adr", Arg.Any<AdrPlusRepoConfig>())
                .Returns([]);

            // Act & Assert
            await _handler.Invoking(h => h.ExecuteAsync(args, CancellationToken.None))
                .Should().ThrowAsync<InvalidDataException>();
        }

        [Fact]
        public async Task ExecuteAsync_WhenLatestAdrHeaderInvalid_ThrowsInvalidDataException()
        {
            // Arrange
            var configPath = "/repo/.adrplus";
            var args = new[] { "--file", AdrFilePath };
            var parsedArgs = new Dictionary<Arguments, string>
            {
                { Arguments.FileAdr, AdrFilePath }
            };

            var infoadr = CreateTestAdrFileNameComponents(AdrFileName, AdrStatus.Accepted, number: 1);
            var latestWithInvalidHeader = CommandHandlerMockHelper.CreateValidAdrFileNameComponents(AdrFileName, AdrStatus.Accepted);
            latestWithInvalidHeader.Header.IsValid = false;
            latestWithInvalidHeader.Header.ErrorMessage = "Invalid header";

            // Setup: config path mocks
            _mockAdrServices.ParseArgs(Arg.Any<string[]>(), Arg.Any<Arguments[]>()).Returns(parsedArgs);
            _mockValidateConfig.HasTemplateRepoFile().Returns(true);
            _mockValidateConfig.GetFileNameRepoConfig().Returns(ConfigFileName);

            // Setup: file system path mocks (complex logic required)
            var configNormalized = Path.GetFullPath(configPath);
            var adrNormalized = Path.GetFullPath(AdrFilePath);
            _mockFileSystem.FileExists(Arg.Any<string>()).Returns(callInfo =>
            {
                var path = callInfo.Arg<string>();
                var normalized = Path.GetFullPath(path);
                if (normalized == configNormalized || normalized == adrNormalized) return true;
                return path.EndsWith(".adrplus");
            });

            _mockFileSystem.ReadAllTextAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(callInfo =>
                {
                    var path = callInfo.Arg<string>();
                    return Task.FromResult(path.EndsWith(".adrplus") ? BasicJsonConfig : "");
                });
            _mockValidateConfig.ValidateRepoStructure(BasicJsonConfig).Returns((true, []));
            _mockFileSystem.GetFileRootRepositoryPath(Arg.Any<string>()).Returns(configPath);
            _mockFileSystem.GetFullNameDirectoryByFile(Arg.Any<string>()).Returns("/repo/adr");
            _mockFileSystem.GetFullNameFile(Arg.Any<string>()).Returns(AdrFilePath);

            // Setup: console mocks
            var cursorPos = (0, 0);
            _mockConsole.PromptGetCursorPosition().Returns(cursorPos);
            _mockConsole.PromptWriteWait(Arg.Any<string>());
            _mockConsole.PromptClearWaitText(cursorPos);

            // Setup: config parsing
            var repoConfig = CreateMockAdrPlusRepoConfig();
            _mockAdrServices.FromJson(Arg.Any<string>(), Arg.Any<string>()).Returns(repoConfig);

            // Setup: Test-specific mocks that cause the validation error
#pragma warning disable CS8620 // Argument cannot be used for parameter due to differences in the nullability of reference types.
            _mockAdrServices.ParseFileName(Path.GetFullPath(AdrFilePath), Arg.Any<AdrPlusRepoConfig>(), _mockFileSystem)
                .Returns(Task.FromResult<AdrFileNameComponents?>(infoadr));
#pragma warning restore CS8620 // Argument cannot be used for parameter due to differences in the nullability of reference types.
            _mockAdrServices.GetLatestADRSequence(1, _mockFileSystem, "/repo/adr", Arg.Any<AdrPlusRepoConfig>())
                .Returns(Task.FromResult<AdrFileNameComponents?>(latestWithInvalidHeader));
            _mockAdrServices.ReadAllAdrByNumber(1, _mockFileSystem, "/repo/adr", Arg.Any<AdrPlusRepoConfig>())
                .Returns([]);

            // Act & Assert
            await _handler.Invoking(h => h.ExecuteAsync(args, CancellationToken.None))
                .Should().ThrowAsync<InvalidDataException>();
        }

        [Fact]
        public async Task ExecuteAsync_WhenLatestAdrNotSameSequence_ThrowsInvalidOperationException()
        {
            // Arrange
            var configPath = "/repo/.adrplus";
            var args = new[] { "--file", AdrFilePath };
            var parsedArgs = new Dictionary<Arguments, string>
            {
                { Arguments.FileAdr, AdrFilePath }
            };

            var infoadr = CreateTestAdrFileNameComponents(AdrFileName, AdrStatus.Accepted, number: 1);
            var latestDifferent = CommandHandlerMockHelper.CreateValidAdrFileNameComponents("ADR-0002-other.md", AdrStatus.Accepted);
            latestDifferent.Number = 2;
            latestDifferent.Title = "other";

            // Setup: config path mocks
            _mockAdrServices.ParseArgs(Arg.Any<string[]>(), Arg.Any<Arguments[]>()).Returns(parsedArgs);
            _mockValidateConfig.HasTemplateRepoFile().Returns(true);
            _mockValidateConfig.GetFileNameRepoConfig().Returns(ConfigFileName);

            // Setup: file system path mocks (complex logic required)
            var configNormalized = Path.GetFullPath(configPath);
            var adrNormalized = Path.GetFullPath(AdrFilePath);
            _mockFileSystem.FileExists(Arg.Any<string>()).Returns(callInfo =>
            {
                var path = callInfo.Arg<string>();
                var normalized = Path.GetFullPath(path);
                if (normalized == configNormalized || normalized == adrNormalized) return true;
                return path.EndsWith(".adrplus");
            });

            _mockFileSystem.ReadAllTextAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(callInfo =>
                {
                    var path = callInfo.Arg<string>();
                    return Task.FromResult(path.EndsWith(".adrplus") ? BasicJsonConfig : "");
                });
            _mockValidateConfig.ValidateRepoStructure(BasicJsonConfig).Returns((true, []));
            _mockFileSystem.GetFileRootRepositoryPath(Arg.Any<string>()).Returns(configPath);
            _mockFileSystem.GetFullNameDirectoryByFile(Arg.Any<string>()).Returns("/repo/adr");
            _mockFileSystem.GetFullNameFile(Arg.Any<string>()).Returns(AdrFilePath);

            // Setup: console mocks
            var cursorPos = (0, 0);
            _mockConsole.PromptGetCursorPosition().Returns(cursorPos);
            _mockConsole.PromptWriteWait(Arg.Any<string>());
            _mockConsole.PromptClearWaitText(cursorPos);

            // Setup: config parsing
            var repoConfig = CreateMockAdrPlusRepoConfig();
            _mockAdrServices.FromJson(Arg.Any<string>(), Arg.Any<string>()).Returns(repoConfig);

            // Setup: Test-specific mocks that cause the validation error
#pragma warning disable CS8620 // Argument cannot be used for parameter due to differences in the nullability of reference types.
            _mockAdrServices.ParseFileName(Path.GetFullPath(AdrFilePath), Arg.Any<AdrPlusRepoConfig>(), _mockFileSystem)
                .Returns(Task.FromResult<AdrFileNameComponents?>(infoadr));
#pragma warning restore CS8620 // Argument cannot be used for parameter due to differences in the nullability of reference types.
            _mockAdrServices.GetLatestADRSequence(1, _mockFileSystem, "/repo/adr", Arg.Any<AdrPlusRepoConfig>())
                .Returns(Task.FromResult<AdrFileNameComponents?>(latestDifferent));
            _mockAdrServices.ReadAllAdrByNumber(1, _mockFileSystem, "/repo/adr", Arg.Any<AdrPlusRepoConfig>())
                .Returns([]);

            // Act & Assert
            await _handler.Invoking(h => h.ExecuteAsync(args, CancellationToken.None))
                .Should().ThrowAsync<InvalidOperationException>();
        }

        [Fact]
        public async Task ExecuteAsync_WhenNotLatestVersion_ThrowsInvalidOperationException()
        {
            // Arrange
            var configPath = "/repo/.adrplus";
            var args = new[] { "--file", AdrFilePath };
            var parsedArgs = new Dictionary<Arguments, string>
            {
                { Arguments.FileAdr, AdrFilePath }
            };

            var infoadr = CreateTestAdrFileNameComponents(AdrFileName, AdrStatus.Accepted, number: 1, revision: 1);
            var latestNewer = CommandHandlerMockHelper.CreateValidAdrFileNameComponents("ADR-0001-v1-r2.md", AdrStatus.Accepted);
            latestNewer.Number = 1;
            latestNewer.Revision = 2;

            // Setup: config path mocks
            _mockAdrServices.ParseArgs(Arg.Any<string[]>(), Arg.Any<Arguments[]>()).Returns(parsedArgs);
            _mockValidateConfig.HasTemplateRepoFile().Returns(true);
            _mockValidateConfig.GetFileNameRepoConfig().Returns(ConfigFileName);

            // Setup: file system path mocks (complex logic required)
            var configNormalized = Path.GetFullPath(configPath);
            var adrNormalized = Path.GetFullPath(AdrFilePath);
            _mockFileSystem.FileExists(Arg.Any<string>()).Returns(callInfo =>
            {
                var path = callInfo.Arg<string>();
                var normalized = Path.GetFullPath(path);
                if (normalized == configNormalized || normalized == adrNormalized) return true;
                return path.EndsWith(".adrplus");
            });

            _mockFileSystem.ReadAllTextAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(callInfo =>
                {
                    var path = callInfo.Arg<string>();
                    return Task.FromResult(path.EndsWith(".adrplus") ? BasicJsonConfig : "");
                });
            _mockValidateConfig.ValidateRepoStructure(BasicJsonConfig).Returns((true, []));
            _mockFileSystem.GetFileRootRepositoryPath(Arg.Any<string>()).Returns(configPath);
            _mockFileSystem.GetFullNameDirectoryByFile(Arg.Any<string>()).Returns("/repo/adr");
            _mockFileSystem.GetFullNameFile(Arg.Any<string>()).Returns(AdrFilePath);

            // Setup: console mocks
            var cursorPos = (0, 0);
            _mockConsole.PromptGetCursorPosition().Returns(cursorPos);
            _mockConsole.PromptWriteWait(Arg.Any<string>());
            _mockConsole.PromptClearWaitText(cursorPos);

            // Setup: config parsing
            var repoConfig = CreateMockAdrPlusRepoConfig();
            _mockAdrServices.FromJson(Arg.Any<string>(), Arg.Any<string>()).Returns(repoConfig);

            // Setup: Test-specific mocks that cause the validation error
#pragma warning disable CS8620 // Argument cannot be used for parameter due to differences in the nullability of reference types.
            _mockAdrServices.ParseFileName(Path.GetFullPath(AdrFilePath), Arg.Any<AdrPlusRepoConfig>(), _mockFileSystem)
                .Returns(Task.FromResult<AdrFileNameComponents?>(infoadr));
#pragma warning restore CS8620 // Argument cannot be used for parameter due to differences in the nullability of reference types.
            _mockAdrServices.GetLatestADRSequence(1, _mockFileSystem, "/repo/adr", Arg.Any<AdrPlusRepoConfig>())
                .Returns(Task.FromResult<AdrFileNameComponents?>(latestNewer));
            _mockAdrServices.ReadAllAdrByNumber(1, _mockFileSystem, "/repo/adr", Arg.Any<AdrPlusRepoConfig>())
                .Returns([]);

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

            var infoadr = CreateTestAdrFileNameComponents(AdrFileName, AdrStatus.Accepted, number: 1, revision: 1);

            // Set up specific mocks before generic ones to ensure they take precedence
            _mockAdrServices.ParseFileName(AdrFilePath, Arg.Any<AdrPlusRepoConfig>(), _mockFileSystem)
                .Returns(infoadr);
            _mockAdrServices.ReadAllAdrByNumber(1, _mockFileSystem, RepoPath, Arg.Any<AdrPlusRepoConfig>())
                .Returns([]);
            _mockAdrServices.GetLatestADRSequence(1, _mockFileSystem, RepoPath, Arg.Any<AdrPlusRepoConfig>())
                .Returns(callInfo => Task.FromResult<AdrFileNameComponents?>(infoadr));

            SetupBasicMocks(parsedArgs, BasicJsonConfig, configPath);

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

            _ = CreateTestAdrFileNameComponents(AdrFileName, AdrStatus.Accepted, number: 1, revision: 1);

            SetupBasicMocks(parsedArgs, BasicJsonConfig, configPath);

            // Act
            await _handler.ExecuteAsync(args, CancellationToken.None);

            // Assert
            _mockConsole.Received(1).PromptWriteSuccess(Arg.Any<string>());
        }

        [Fact]
        public async Task ExecuteAsync_WithValidAcceptedAdr_CreatesNewVersion()
        {
            // Arrange
            var configPath = "/repo/.adrplus";
            var args = new[] { "--file", AdrFilePath };
            var parsedArgs = new Dictionary<Arguments, string>
            {
                { Arguments.FileAdr, AdrFilePath }
            };

            _ = CreateTestAdrFileNameComponents(AdrFileName, AdrStatus.Accepted, number: 1, revision: 1);

            SetupBasicMocks(parsedArgs, BasicJsonConfig, configPath);

            // Act
            await _handler.ExecuteAsync(args, CancellationToken.None);

            // Assert
            await _mockFileSystem.Received(1).WriteAllTextAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ExecuteAsync_WithValidRejectedAdr_CreatesNewVersion()
        {
            // Arrange
            var configPath = "/repo/.adrplus";
            var args = new[] { "--file", AdrFilePath };
            var parsedArgs = new Dictionary<Arguments, string>
            {
                { Arguments.FileAdr, AdrFilePath }
            };

            _ = CreateTestAdrFileNameComponents(AdrFileName, AdrStatus.Rejected, number: 1, revision: 1);

            SetupBasicMocks(parsedArgs, BasicJsonConfig, configPath);

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

            var infoadr = CreateTestAdrFileNameComponents(AdrFileName, AdrStatus.Accepted, number: 1, revision: 1);

            // Set up specific mocks before generic ones to ensure they take precedence
            _mockAdrServices.ParseFileName(AdrFilePath, Arg.Any<AdrPlusRepoConfig>(), _mockFileSystem)
                .Returns(infoadr);
            _mockAdrServices.ReadAllAdrByNumber(1, _mockFileSystem, RepoPath, Arg.Any<AdrPlusRepoConfig>())
                .Returns([]);
            _mockAdrServices.GetLatestADRSequence(1, _mockFileSystem, RepoPath, Arg.Any<AdrPlusRepoConfig>())
                .Returns(callInfo => Task.FromResult<AdrFileNameComponents?>(infoadr));

            SetupBasicMocks(parsedArgs, BasicJsonConfig, configPath);

            string? capturedContent = null;
            _mockFileSystem.WriteAllTextAsync(Arg.Any<string>(), Arg.Do<string>(c => capturedContent = c), Arg.Any<CancellationToken>())
                .Returns(Task.CompletedTask);

            // Act
            await _handler.ExecuteAsync(args, CancellationToken.None);

            // Assert
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

            var infoadr = CreateTestAdrFileNameComponents(AdrFileName, AdrStatus.Accepted, number: 1, revision: 1);

            // Set up specific mocks before generic ones to ensure they take precedence
            _mockAdrServices.ParseFileName(AdrFilePath, Arg.Any<AdrPlusRepoConfig>(), _mockFileSystem)
                .Returns(infoadr);
            _mockAdrServices.ReadAllAdrByNumber(1, _mockFileSystem, RepoPath, Arg.Any<AdrPlusRepoConfig>())
                .Returns([]);
            _mockAdrServices.GetLatestADRSequence(1, _mockFileSystem, RepoPath, Arg.Any<AdrPlusRepoConfig>())
                .Returns(callInfo => Task.FromResult<AdrFileNameComponents?>(infoadr));

            SetupBasicMocks(parsedArgs, BasicJsonConfig, configPath);

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

            var infoadr = CreateTestAdrFileNameComponents(AdrFileName, AdrStatus.Accepted, number: 1, revision: 1);

            // Set up specific mocks before generic ones to ensure they take precedence
            _mockAdrServices.ParseFileName(AdrFilePath, Arg.Any<AdrPlusRepoConfig>(), _mockFileSystem)
                .Returns(infoadr);
            _mockAdrServices.ReadAllAdrByNumber(1, _mockFileSystem, RepoPath, Arg.Any<AdrPlusRepoConfig>())
                .Returns([]);
            _mockAdrServices.GetLatestADRSequence(1, _mockFileSystem, RepoPath, Arg.Any<AdrPlusRepoConfig>())
                .Returns(callInfo => Task.FromResult<AdrFileNameComponents?>(infoadr));

            SetupBasicMocks(parsedArgs, BasicJsonConfig, configPath);

            var beforeCall = DateTime.UtcNow.Year;

            // Act
            await _handler.ExecuteAsync(args, CancellationToken.None);

            // Assert
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
            var configPath = "/repo/.adrplus";
            var args = new[] { "--file", AdrFilePath, "--open" };
            var parsedArgs = new Dictionary<Arguments, string>
            {
                { Arguments.FileAdr, AdrFilePath },
                { Arguments.OpenFile, string.Empty }
            };

            var infoadr = CreateTestAdrFileNameComponents(AdrFileName, AdrStatus.Accepted, number: 1, revision: 1);

            // Set up specific mocks before generic ones to ensure they take precedence
            _mockAdrServices.ParseFileName(AdrFilePath, Arg.Any<AdrPlusRepoConfig>(), _mockFileSystem)
                .Returns(infoadr);
            _mockAdrServices.ReadAllAdrByNumber(1, _mockFileSystem, RepoPath, Arg.Any<AdrPlusRepoConfig>())
                .Returns([]);
            _mockAdrServices.GetLatestADRSequence(1, _mockFileSystem, RepoPath, Arg.Any<AdrPlusRepoConfig>())
                .Returns(callInfo => Task.FromResult<AdrFileNameComponents?>(infoadr));
            _mockAdrServices.OpenFile(Arg.Any<string>(), Arg.Any<string>()).Returns(string.Empty);

            SetupBasicMocks(parsedArgs, BasicJsonConfig, configPath);

            // Act
            await _handler.ExecuteAsync(args, CancellationToken.None);

            // Assert
            _mockAdrServices.Received(1).OpenFile(Arg.Any<string>(), Arg.Any<string>());
        }

        [Fact]
        public async Task ExecuteAsync_WhenOpenFileSucceeds_WritesSuccessMessage()
        {
            // Arrange
            var configPath = "/repo/.adrplus";
            var args = new[] { "--file", AdrFilePath, "--open" };
            var parsedArgs = new Dictionary<Arguments, string>
            {
                { Arguments.FileAdr, AdrFilePath },
                { Arguments.OpenFile, string.Empty }
            };

            var infoadr = CreateTestAdrFileNameComponents(AdrFileName, AdrStatus.Accepted, number: 1, revision: 1);

            // Set up specific mocks before generic ones to ensure they take precedence
            _mockAdrServices.ParseFileName(AdrFilePath, Arg.Any<AdrPlusRepoConfig>(), _mockFileSystem)
                .Returns(infoadr);
            _mockAdrServices.ReadAllAdrByNumber(1, _mockFileSystem, RepoPath, Arg.Any<AdrPlusRepoConfig>())
                .Returns([]);
            _mockAdrServices.GetLatestADRSequence(1, _mockFileSystem, RepoPath, Arg.Any<AdrPlusRepoConfig>())
                .Returns(callInfo => Task.FromResult<AdrFileNameComponents?>(infoadr));
            _mockAdrServices.OpenFile(Arg.Any<string>(), Arg.Any<string>()).Returns(string.Empty);

            SetupBasicMocks(parsedArgs, BasicJsonConfig, configPath);

            // Act
            await _handler.ExecuteAsync(args, CancellationToken.None);

            // Assert: one success for ADR created, one for open success
            _mockConsole.Received(2).PromptWriteSuccess(Arg.Any<string>());
        }

        [Fact]
        public async Task ExecuteAsync_WhenOpenFileFails_WritesErrorMessage()
        {
            // Arrange
            var configPath = "/repo/.adrplus";
            var args = new[] { "--file", AdrFilePath, "--open" };
            var parsedArgs = new Dictionary<Arguments, string>
            {
                { Arguments.FileAdr, AdrFilePath },
                { Arguments.OpenFile, string.Empty }
            };

            var infoadr = CreateTestAdrFileNameComponents(AdrFileName, AdrStatus.Accepted, number: 1, revision: 1);

            // Set up specific mocks before generic ones to ensure they take precedence
            _mockAdrServices.ParseFileName(AdrFilePath, Arg.Any<AdrPlusRepoConfig>(), _mockFileSystem)
                .Returns(infoadr);
            _mockAdrServices.ReadAllAdrByNumber(1, _mockFileSystem, RepoPath, Arg.Any<AdrPlusRepoConfig>())
                .Returns([]);
            _mockAdrServices.GetLatestADRSequence(1, _mockFileSystem, RepoPath, Arg.Any<AdrPlusRepoConfig>())
                .Returns(callInfo => Task.FromResult<AdrFileNameComponents?>(infoadr));
            _mockAdrServices.OpenFile(Arg.Any<string>(), Arg.Any<string>()).Returns("open command failed");

            SetupBasicMocks(parsedArgs, BasicJsonConfig, configPath);

            // Act
            await _handler.ExecuteAsync(args, CancellationToken.None);

            // Assert
            _mockConsole.Received(1).PromptWriteError(Arg.Any<string>());
        }

        [Fact]
        public async Task ExecuteAsync_WithOpenArgButNoCommandConfigured_DoesNotCallOpenFile()
        {
            // Arrange
            var configNoCommand = new AdrPlusConfig { Language = "en-US" };
            var handler = CreateHandlerWith(configNoCommand);

            var configPath = "/repo/.adrplus";
            var args = new[] { "--file", AdrFilePath };
            var parsedArgs = new Dictionary<Arguments, string>
            {
                { Arguments.FileAdr, AdrFilePath }
            };

            var infoadr = CreateTestAdrFileNameComponents(AdrFileName, AdrStatus.Accepted, number: 1, revision: 1);

            // Set up specific mocks before generic ones to ensure they take precedence
            _mockAdrServices.ParseFileName(AdrFilePath, Arg.Any<AdrPlusRepoConfig>(), _mockFileSystem)
                .Returns(infoadr);
            _mockAdrServices.ReadAllAdrByNumber(1, _mockFileSystem, RepoPath, Arg.Any<AdrPlusRepoConfig>())
                .Returns([]);
            _mockAdrServices.GetLatestADRSequence(1, _mockFileSystem, RepoPath, Arg.Any<AdrPlusRepoConfig>())
                .Returns(callInfo => Task.FromResult<AdrFileNameComponents?>(infoadr));

            SetupBasicMocks(parsedArgs, BasicJsonConfig, configPath);

            // Act
            await handler.ExecuteAsync(args, CancellationToken.None);

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

            var infoadr = CreateTestAdrFileNameComponents(AdrFileName, AdrStatus.Accepted, number: 1, revision: 1);

            // Set up specific mocks before generic ones to ensure they take precedence
            _mockAdrServices.ParseFileName(AdrFilePath, Arg.Any<AdrPlusRepoConfig>(), _mockFileSystem)
                .Returns(infoadr);
            _mockAdrServices.ReadAllAdrByNumber(1, _mockFileSystem, RepoPath, Arg.Any<AdrPlusRepoConfig>())
                .Returns([]);
            _mockAdrServices.GetLatestADRSequence(1, _mockFileSystem, RepoPath, Arg.Any<AdrPlusRepoConfig>())
                .Returns(callInfo => Task.FromResult<AdrFileNameComponents?>(infoadr));

            SetupBasicMocks(parsedArgs, BasicJsonConfig, configPath);

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

            var infoadr = CreateTestAdrFileNameComponents(AdrFileName, AdrStatus.Accepted, number: 1, revision: 1);
            infoadr.ContentAdr = "Test content from file";

            SetupMinimalMocksWithPathNormalization(parsedArgs, BasicJsonConfig, configPath);

            _mockAdrServices.ParseFileName(Arg.Any<string>(), Arg.Any<AdrPlusRepoConfig>(), Arg.Any<IFileSystemService>())
                .ReturnsForAnyArgs(infoadr);
            _mockAdrServices.ReadAllAdrByNumber(Arg.Any<int>(), Arg.Any<IFileSystemService>(), Arg.Any<string>(), Arg.Any<AdrPlusRepoConfig>())
                .ReturnsForAnyArgs([]);
            _mockAdrServices.GetLatestADRSequence(Arg.Any<int>(), Arg.Any<IFileSystemService>(), Arg.Any<string>(), Arg.Any<AdrPlusRepoConfig>())
                .ReturnsForAnyArgs(callInfo => Task.FromResult<AdrFileNameComponents?>(infoadr));

            string? capturedContent = null;
            _mockFileSystem.WriteAllTextAsync(Arg.Any<string>(), Arg.Do<string>(c => capturedContent = c), Arg.Any<CancellationToken>())
                .Returns(Task.CompletedTask);

            // Act
            await _handler.ExecuteAsync(args, CancellationToken.None);

            // Assert
            capturedContent.Should().NotBeNull();
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
            _mockAdrServices.When(x => x.ParseArgs(Arg.Any<string[]>(), Arg.Any<Arguments[]>()))
                .Do(x => throw exception);

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
            var parsedArgs = new Dictionary<Arguments, string> { { Arguments.WizardVersion, string.Empty } };
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
            var parsedArgs = new Dictionary<Arguments, string> { { Arguments.WizardVersion, string.Empty } };
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
            var parsedArgs = new Dictionary<Arguments, string> { { Arguments.WizardVersion, string.Empty } };
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
            var parsedArgs = new Dictionary<Arguments, string> { { Arguments.WizardVersion, string.Empty } };
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
            var parsedArgs = new Dictionary<Arguments, string> { { Arguments.WizardVersion, string.Empty } };

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
            var parsedArgs = new Dictionary<Arguments, string> { { Arguments.WizardVersion, string.Empty } };

            SetupWizardMocksUpToConfirmation(BasicJsonConfig);
            _mockAdrServices.ParseArgs(args, Arg.Any<Arguments[]>()).Returns(parsedArgs);

            _mockConsole.PromptConfirm(Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns((false, false), (true, false));

            // Act & Assert
            await _handler.Invoking(h => h.ExecuteAsync(args, CancellationToken.None))
                .Should().ThrowAsync<OperationCanceledException>();

            _mockConsole.Received(2).PromptSelectFolderPath(
                Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<string>(), _mockFileSystem, _mockValidateConfig, Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ExecuteAsync_WithWizardMode_ConfirmedYes_CreatesAdrFile()
        {
            // Arrange
            var args = new[] { "--wizard" };
            var parsedArgs = new Dictionary<Arguments, string> { { Arguments.WizardVersion, string.Empty } };

            SetupWizardMocksUpToConfirmation(BasicJsonConfig);
            _mockAdrServices.ParseArgs(args, Arg.Any<Arguments[]>()).Returns(parsedArgs);
            _mockConsole.PromptConfirm(Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns((false, true));

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
            var parsedArgs = new Dictionary<Arguments, string> { { Arguments.WizardVersion, string.Empty } };

            SetupWizardMocksUpToConfirmation(BasicJsonConfig);
            _mockAdrServices.ParseArgs(args, Arg.Any<Arguments[]>()).Returns(parsedArgs);
            _mockConsole.PromptConfirm(Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns((true, false));

            // Act
            await _handler.Invoking(h => h.ExecuteAsync(args, CancellationToken.None))
                .Should().ThrowAsync<OperationCanceledException>();

            // Assert
            _mockConsole.Received(4).PromptWriteSummary(Arg.Any<string>());
        }

        [Fact]
        public async Task ExecuteAsync_WithWizardMode_InvalidRepoConfig_ThrowsInvalidDataException()
        {
            // Arrange
            var args = new[] { "--wizard" };
            var parsedArgs = new Dictionary<Arguments, string> { { Arguments.WizardVersion, string.Empty } };
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
            var parsedArgs = new Dictionary<Arguments, string> { { Arguments.WizardVersion, string.Empty } };
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

            _mockFileSystem.FileExists(Arg.Any<string>()).Returns(callInfo =>
            {
                var path = callInfo.Arg<string>();
                var normalized = Path.GetFullPath(path);
                var configNormalized = Path.GetFullPath(configPath);
                var adrNormalized = Path.GetFullPath(AdrFilePath);

                if (normalized == configNormalized || normalized == adrNormalized)
                {
                    return true;
                }

                if (path.Contains("-v") && path.Contains("-r"))
                {
                    return false;
                }

                return path.EndsWith(".adrplus");
            });

            _mockFileSystem.ReadAllTextAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(callInfo =>
            {
                var path = callInfo.Arg<string>();
                return Task.FromResult(path.EndsWith(".adrplus") ? jsonConfig : "");
            });

            _mockValidateConfig.ValidateRepoStructure(jsonConfig).Returns((true, []));

            _mockFileSystem.GetFileRootRepositoryPath(Arg.Any<string>()).Returns(callInfo =>
            {
                var filePath = callInfo.Arg<string>();
                var dir = Path.GetDirectoryName(filePath);
                return string.IsNullOrEmpty(dir) ? null : Path.Combine(dir, ConfigFileName);
            });

            _mockFileSystem.GetFullNameDirectoryByFile(Arg.Any<string>()).Returns(callInfo =>
            {
                var filePath = callInfo.Arg<string>();
                return Path.GetDirectoryName(filePath) ?? string.Empty;
            });

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

            var validAdr = CommandHandlerMockHelper.CreateValidAdrFileNameComponents(AdrFileName, AdrStatus.Accepted);
            validAdr.Number = 1;

#pragma warning disable CS8620 // Argument cannot be used for parameter due to differences in the nullability of reference types.
            _mockAdrServices.ParseFileName(Arg.Any<string>(), Arg.Any<AdrPlusRepoConfig>(), Arg.Any<IFileSystemService>())
                .Returns(Task.FromResult<AdrFileNameComponents?>(validAdr));
#pragma warning restore CS8620 // Argument cannot be used for parameter due to differences in the nullability of reference types.

            _mockAdrServices.ReadAllAdrByNumber(Arg.Any<int>(), Arg.Any<IFileSystemService>(), Arg.Any<string>(), Arg.Any<AdrPlusRepoConfig>())
                .Returns([validAdr]);

            _mockAdrServices.GetLatestADRSequence(Arg.Any<int>(), Arg.Any<IFileSystemService>(), Arg.Any<string>(), Arg.Any<AdrPlusRepoConfig>())
                .Returns(callInfo => Task.FromResult<AdrFileNameComponents?>(validAdr));

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

            _mockFileSystem.FileExists(Arg.Any<string>()).Returns(callInfo =>
            {
                var path = callInfo.Arg<string>();
                var normalized = Path.GetFullPath(path);
                var configNormalized = Path.GetFullPath(Path.Combine(RepoPath, ConfigFileName));
                var adrNormalized = Path.GetFullPath(AdrFileName);

                if (normalized == configNormalized || normalized == adrNormalized)
                {
                    return true;
                }

                if (path.Contains("-v") && path.Contains("-r"))
                {
                    return false;
                }

                return path.EndsWith(".adrplus");
            });

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

            _mockFileSystem.GetFileRootRepositoryPath(Arg.Any<string>()).Returns(callInfo =>
            {
                var filePath = callInfo.Arg<string>();
                var dir = Path.GetDirectoryName(filePath);
                return string.IsNullOrEmpty(dir) ? null : Path.Combine(dir, ConfigFileName);
            });

            _mockFileSystem.GetFullNameDirectoryByFile(Arg.Any<string>()).Returns(callInfo =>
            {
                var filePath = callInfo.Arg<string>();
                return Path.GetDirectoryName(filePath) ?? string.Empty;
            });

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

            _mockAdrServices.ParseFileName(Arg.Any<string>(), Arg.Any<AdrPlusRepoConfig>(), Arg.Any<IFileSystemService>())
                .Returns(selectedAdr);

            _mockAdrServices.ReadAllAdrByNumber(1, _mockFileSystem, RepoPath, Arg.Any<AdrPlusRepoConfig>())
                .Returns([selectedAdr]);
            _mockAdrServices.GetLatestADRSequence(1, _mockFileSystem, RepoPath, Arg.Any<AdrPlusRepoConfig>())
                .Returns(callInfo => Task.FromResult<AdrFileNameComponents?>(selectedAdr));
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

        private VersionCommandHandler CreateHandlerWith(AdrPlusConfig config)
        {
            return new VersionCommandHandler(
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

            var repoConfig = CreateMockAdrPlusRepoConfig();
            _mockAdrServices.FromJson(Arg.Any<string>(), Arg.Any<string>()).Returns(repoConfig);

            _mockConsole.PromptWriteError(Arg.Any<string>());
            _mockConsole.PromptWriteSuccess(Arg.Any<string>());
            _mockConsole.PromptWriteSummary(Arg.Any<string>());
            _mockConsole.PromptWriteWait(Arg.Any<string>());

            var cursorPos = (0, 0);
            _mockConsole.PromptGetCursorPosition().Returns(cursorPos);

            var defaultAdr = CreateTestAdrFileNameComponents(AdrFileName, AdrStatus.Accepted, number: 1);
            _mockAdrServices.ParseFileName(Arg.Any<string>(), Arg.Any<AdrPlusRepoConfig>(), Arg.Any<IFileSystemService>())
                .Returns(defaultAdr);

            _mockAdrServices.GetLatestADRSequence(Arg.Any<int>(), Arg.Any<IFileSystemService>(), Arg.Any<string>(), Arg.Any<AdrPlusRepoConfig>())
                .Returns(callInfo => Task.FromResult<AdrFileNameComponents?>(defaultAdr));

            _mockAdrServices.ReadAllAdrByNumber(Arg.Any<int>(), Arg.Any<IFileSystemService>(), Arg.Any<string>(), Arg.Any<AdrPlusRepoConfig>())
                .Returns([]);
        }

        #endregion
    }
}
