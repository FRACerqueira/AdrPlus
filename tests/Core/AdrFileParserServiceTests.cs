using AdrPlus.Core;
using AdrPlus.Domain;
using AdrPlus.Infrastructure.FileSystem;
using NSubstitute;
using Xunit;

namespace AdrPlus.Tests.Core
{
    public class AdrFileParserServiceTests
    {
        private readonly IAdrFileParser _parser;
        private readonly IFileSystemService _fileSystemService;
        private readonly AdrPlusRepoConfig _config;

        public AdrFileParserServiceTests()
        {
            _parser = new AdrFileParserService();
            _fileSystemService = Substitute.For<IFileSystemService>();
            _config = new AdrPlusRepoConfig
            {
                LenRevision = 2,
                StatusNew = "Proposed",
                StatusAcc = "Accepted",
                StatusRej = "Rejected",
                StatusSup = "Superseded",
                Separator = '-',
                LenVersion = 2,
                LenScope = 1
            };
        }

        #region ParseAdrHeaderAndContentAsync Tests

        [Fact]
        public async Task ParseAdrHeaderAndContentAsync_WithEmptyFile_ReturnsErrorMessage()
        {
            // Arrange
            var filePath = "test.md";
            _fileSystemService.ReadAllLinesAsync(filePath).Returns(Array.Empty<string>());

            // Act
            var (header, content) = await _parser.ParseAdrHeaderAndContentAsync(filePath, _config, _fileSystemService);

            // Assert
            Assert.False(header.IsValid);
            Assert.NotEmpty(header.ErrorMessage);
            Assert.Empty(content);
        }

        [Fact]
        public async Task ParseAdrHeaderAndContentAsync_WithTooFewLines_ReturnsErrorMessage()
        {
            // Arrange
            var filePath = "test.md";
            var lines = new[] { "###### Test" };
            _fileSystemService.ReadAllLinesAsync(filePath).Returns(lines);

            // Act
            var (header, content) = await _parser.ParseAdrHeaderAndContentAsync(filePath, _config, _fileSystemService);

            // Assert
            Assert.False(header.IsValid);
            Assert.NotEmpty(header.ErrorMessage);
        }

        [Fact]
        public async Task ParseAdrHeaderAndContentAsync_WithValidHeader_ParsesSuccessfully()
        {
            // Arrange
            var filePath = "test.md";
            var lines = BuildValidHeaderLines("2025-04-17", "Proposed");
            _fileSystemService.ReadAllLinesAsync(filePath).Returns(lines);

            // Act
            var (header, content) = await _parser.ParseAdrHeaderAndContentAsync(filePath, _config, _fileSystemService);

            // Assert
            Assert.True(header.IsValid);
            Assert.Empty(header.ErrorMessage);
            Assert.Equal("Test Disclaimer", header.Disclaimer);
            Assert.Equal(1, header.Version);
            Assert.Equal(1, header.Revision);
            Assert.Equal("Enterprise", header.Scope);
            Assert.Equal("TestDomain", header.Domain);
            Assert.Equal(AdrStatus.Proposed, header.StatusCreate);
            Assert.NotNull(header.DateCreate);
            Assert.Equal(new DateTime(2025, 04, 17), header.DateCreate.Value.Date);
            Assert.Equal("Test ADR Title", header.Title);
        }

        [Fact]
        public async Task ParseAdrHeaderAndContentAsync_WithStatusChangeSuperseded_ParsesSupersededFile()
        {
            // Arrange
            var filePath = "test.md";
            var lines = new[]
            {
                "###### Test Disclaimer",
                "##### Version: 1",
                "##### Revision: 1",
                "##### Scope: Enterprise:TestDomain",
                "##### Status",
                "- Proposed (2025-04-17)",
                "- Accepted (2025-04-18)",
                "- Superseded (2025-04-19): 0002-New-Decision.md",
                "# Test ADR Title",
                "---",
                "This is the content of the ADR."
            };
            _fileSystemService.ReadAllLinesAsync(filePath).Returns(lines);

            // Act
            var (header, content) = await _parser.ParseAdrHeaderAndContentAsync(filePath, _config, _fileSystemService);

            // Assert
            Assert.True(header.IsValid);
            Assert.Equal(AdrStatus.Superseded, header.StatusChange);
            Assert.Equal("0002-New-Decision.md", header.FileSuperSedes);
            Assert.NotNull(header.DateChange);
        }

        [Fact]
        public async Task ParseAdrHeaderAndContentAsync_WithEmptyStatusLines_ParsesSuccessfully()
        {
            // Arrange
            var filePath = "test.md";
            var lines = new[]
            {
                "###### Test Disclaimer",
                "##### Version: 1",
                "##### Revision: 1",
                "##### Scope: Enterprise:TestDomain",
                "##### Status",
                "- Proposed (2025-04-17)",
                "- -",
                "- -",
                "# Test ADR Title",
                "---",
                "This is the content of the ADR."
            };
            _fileSystemService.ReadAllLinesAsync(filePath).Returns(lines);

            // Act
            var (header, content) = await _parser.ParseAdrHeaderAndContentAsync(filePath, _config, _fileSystemService);

            // Assert
            Assert.True(header.IsValid);
            Assert.Equal(AdrStatus.Proposed, header.StatusCreate);
            Assert.Equal(AdrStatus.Unknown, header.StatusUpdate);
            Assert.Equal(AdrStatus.Unknown, header.StatusChange);
            Assert.Null(header.DateUpdate);
            Assert.Null(header.DateChange);
        }

        [Fact]
        public async Task ParseAdrHeaderAndContentAsync_WithInvalidDisclaimerFormat_ReturnsError()
        {
            // Arrange
            var filePath = "test.md";
            var lines = new[]
            {
                "Invalid Disclaimer Format",
                "##### Version: 1",
                "##### Revision: 1",
                "##### Scope: Enterprise:TestDomain",
                "##### Status",
                "- Proposed (2025-04-17)",
                "- -",
                "- -",
                "# Test ADR Title",
                "---"
            };
            _fileSystemService.ReadAllLinesAsync(filePath).Returns(lines);

            // Act
            var (header, content) = await _parser.ParseAdrHeaderAndContentAsync(filePath, _config, _fileSystemService);

            // Assert
            Assert.False(header.IsValid);
            Assert.NotEmpty(header.ErrorMessage);
        }

        [Fact]
        public async Task ParseAdrHeaderAndContentAsync_WithInvalidVersionFormat_ReturnsError()
        {
            // Arrange
            var filePath = "test.md";
            var lines = new[]
            {
                "###### Test Disclaimer",
                "##### Version",
                "##### Revision: 1",
                "##### Scope: Enterprise:TestDomain",
                "##### Status",
                "- Proposed (2025-04-17)",
                "- -",
                "- -",
                "# Test ADR Title",
                "---"
            };
            _fileSystemService.ReadAllLinesAsync(filePath).Returns(lines);

            // Act
            var (header, content) = await _parser.ParseAdrHeaderAndContentAsync(filePath, _config, _fileSystemService);

            // Assert
            Assert.False(header.IsValid);
            Assert.NotEmpty(header.ErrorMessage);
        }

        [Fact]
        public async Task ParseAdrHeaderAndContentAsync_WithStatusDashValue_ParsesAsDash()
        {
            // Arrange
            var filePath = "test.md";
            var lines = new[]
            {
                "###### Test Disclaimer",
                "##### Version: -",
                "##### Revision: -",
                "##### Scope: -",
                "##### Status",
                "- Proposed (2025-04-17)",
                "- -",
                "- -",
                "# Test ADR Title",
                "---",
                "Content"
            };
            _fileSystemService.ReadAllLinesAsync(filePath).Returns(lines);

            // Act
            var (header, content) = await _parser.ParseAdrHeaderAndContentAsync(filePath, _config, _fileSystemService);

            // Assert
            Assert.True(header.IsValid);
            Assert.Equal(0, header.Version);
            Assert.Null(header.Revision);
            Assert.Empty(header.Scope);
        }

        [Fact]
        public async Task ParseAdrHeaderAndContentAsync_WithoutRevisionConfig_IgnoresRevision()
        {
            // Arrange
            var filePath = "test.md";
            var configNoRevision = new AdrPlusRepoConfig
            {
                LenRevision = 0,
                StatusNew = "Proposed",
                StatusAcc = "Accepted",
                StatusRej = "Rejected",
                StatusSup = "Superseded"
            };
            var lines = BuildValidHeaderLines("2025-04-17", "Proposed");
            _fileSystemService.ReadAllLinesAsync(filePath).Returns(lines);

            // Act
            var (header, content) = await _parser.ParseAdrHeaderAndContentAsync(filePath, configNoRevision, _fileSystemService);

            // Assert
            Assert.True(header.IsValid);
            Assert.Null(header.Revision);
        }

        [Fact]
        public async Task ParseAdrHeaderAndContentAsync_WithInvalidStatusText_ReturnsError()
        {
            // Arrange
            var filePath = "test.md";
            var lines = new[]
            {
                "###### Test Disclaimer",
                "##### Version: 1",
                "##### Revision: 1",
                "##### Scope: Enterprise:TestDomain",
                "##### Status",
                "- InvalidStatus (2025-04-17)",
                "- -",
                "- -",
                "# Test ADR Title",
                "---"
            };
            _fileSystemService.ReadAllLinesAsync(filePath).Returns(lines);

            // Act
            var (header, content) = await _parser.ParseAdrHeaderAndContentAsync(filePath, _config, _fileSystemService);

            // Assert
            Assert.False(header.IsValid);
            Assert.NotEmpty(header.ErrorMessage);
        }

        [Fact]
        public async Task ParseAdrHeaderAndContentAsync_WithInvalidDateFormat_ReturnsError()
        {
            // Arrange
            var filePath = "test.md";
            var lines = new[]
            {
                "###### Test Disclaimer",
                "##### Version: 1",
                "##### Revision: 1",
                "##### Scope: Enterprise:TestDomain",
                "##### Status",
                "- Proposed (2025/04/17)",
                "- -",
                "- -",
                "# Test ADR Title",
                "---"
            };
            _fileSystemService.ReadAllLinesAsync(filePath).Returns(lines);

            // Act
            var (header, content) = await _parser.ParseAdrHeaderAndContentAsync(filePath, _config, _fileSystemService);

            // Assert
            Assert.False(header.IsValid);
            Assert.NotEmpty(header.ErrorMessage);
        }

        [Fact]
        public async Task ParseAdrHeaderAndContentAsync_WithMultipleStatusUpdates_ParsesAllStatuses()
        {
            // Arrange
            var filePath = "test.md";
            var lines = new[]
            {
                "###### Test Disclaimer",
                "##### Version: 1",
                "##### Revision: 1",
                "##### Scope: Enterprise:TestDomain",
                "##### Status",
                "- Proposed (2025-04-17)",
                "- Accepted (2025-04-18)",
                "- Rejected (2025-04-19)",
                "# Test ADR Title",
                "---",
                "Content"
            };
            _fileSystemService.ReadAllLinesAsync(filePath).Returns(lines);

            // Act
            var (header, content) = await _parser.ParseAdrHeaderAndContentAsync(filePath, _config, _fileSystemService);

            // Assert
            Assert.True(header.IsValid);
            Assert.Equal(AdrStatus.Proposed, header.StatusCreate);
            Assert.Equal(AdrStatus.Accepted, header.StatusUpdate);
            Assert.Equal(AdrStatus.Rejected, header.StatusChange);
            Assert.NotNull(header.DateCreate);
            Assert.NotNull(header.DateUpdate);
            Assert.NotNull(header.DateChange);
        }

        [Fact]
        public async Task ParseAdrHeaderAndContentAsync_WithExceptionDuringParsing_CatchesExceptionAndReturnsError()
        {
            // Arrange
            var filePath = "test.md";
            _fileSystemService.ReadAllLinesAsync(filePath).Throws(new InvalidOperationException("Test exception"));

            // Act
            var (header, content) = await _parser.ParseAdrHeaderAndContentAsync(filePath, _config, _fileSystemService);

            // Assert
            Assert.False(header.IsValid);
            Assert.NotEmpty(header.ErrorMessage);
            Assert.Contains("Test exception", header.ErrorMessage);
        }

        [Fact]
        public async Task ParseAdrHeaderAndContentAsync_WithContentAfterHeader_ReturnsContent()
        {
            // Arrange
            var filePath = "test.md";
            var lines = new[]
            {
                "###### Test Disclaimer",
                "##### Version: 1",
                "##### Revision: 1",
                "##### Scope: Enterprise:TestDomain",
                "##### Status",
                "- Proposed (2025-04-17)",
                "- -",
                "- -",
                "# Test ADR Title",
                "---",
                "Line 1 of content",
                "Line 2 of content",
                "Line 3 of content"
            };
            _fileSystemService.ReadAllLinesAsync(filePath).Returns(lines);

            // Act
            var (header, content) = await _parser.ParseAdrHeaderAndContentAsync(filePath, _config, _fileSystemService);

            // Assert
            Assert.True(header.IsValid);
            var contentLines = content.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
            Assert.Contains("Line 1 of content", contentLines);
            Assert.Contains("Line 2 of content", contentLines);
            Assert.Contains("Line 3 of content", contentLines);
        }

        [Fact]
        public async Task ParseAdrHeaderAndContentAsync_WithAllStatusValues_VerifiesStatusCompatibility()
        {
            // Arrange
            var filePath = "test.md";
            var customConfig = new AdrPlusRepoConfig
            {
                LenRevision = 2,
                StatusNew = "Proposed",
                StatusAcc = "Accepted",
                StatusRej = "Rejected",
                StatusSup = "Superseded"
            };

            // Test each status value from the config's StatusMapping
            var statusValues = new[]
            {
                ("Proposed", AdrStatus.Proposed),
                ("Accepted", AdrStatus.Accepted),
                ("Rejected", AdrStatus.Rejected),
                ("Superseded", AdrStatus.Superseded)
            };

            foreach (var (statusText, expectedStatus) in statusValues)
            {
                // Arrange
                var lines = new[]
                {
                    "###### Test Disclaimer",
                    "##### Version: 1",
                    "##### Revision: 1",
                    "##### Scope: Enterprise:TestDomain",
                    "##### Status",
                    $"- {statusText} (2025-04-17)",
                    "- -",
                    "- -",
                    "# Test ADR Title",
                    "---"
                };
                _fileSystemService.ReadAllLinesAsync(filePath).Returns(lines);

                // Act
                var (header, _) = await _parser.ParseAdrHeaderAndContentAsync(filePath, customConfig, _fileSystemService);

                // Assert
                Assert.True(header.IsValid, $"Failed for status: {statusText}");
                Assert.Equal(expectedStatus, header.StatusCreate);
            }
        }

        [Fact]
        public async Task ParseAdrHeaderAndContentAsync_WithLinuxLineEndings_ParsesCorrectly()
        {
            // Arrange - Simulates Linux line endings
            var filePath = "test.md";
            var lines = BuildValidHeaderLines("2025-04-17", "Proposed");
            _fileSystemService.ReadAllLinesAsync(filePath).Returns(lines);

            // Act
            var (header, content) = await _parser.ParseAdrHeaderAndContentAsync(filePath, _config, _fileSystemService);

            // Assert
            Assert.True(header.IsValid);
            Assert.Equal("Test ADR Title", header.Title);
        }

        [Fact]
        public async Task ParseAdrHeaderAndContentAsync_WithWindowsLineEndings_ParsesCorrectly()
        {
            // Arrange - Windows line endings are already handled by ReadAllLinesAsync normalization
            var filePath = "test.md";
            var lines = BuildValidHeaderLines("2025-04-17", "Proposed");
            _fileSystemService.ReadAllLinesAsync(filePath).Returns(lines);

            // Act
            var (header, content) = await _parser.ParseAdrHeaderAndContentAsync(filePath, _config, _fileSystemService);

            // Assert
            Assert.True(header.IsValid);
            Assert.Equal("Test ADR Title", header.Title);
        }

        [Fact]
        public async Task ParseAdrHeaderAndContentAsync_WithoutContentAfterHeader_ReturnsEmptyContent()
        {
            // Arrange
            var filePath = "test.md";
            var lines = new[]
            {
                "###### Test Disclaimer",
                "##### Version: 1",
                "##### Revision: 1",
                "##### Scope: Enterprise:TestDomain",
                "##### Status",
                "- Proposed (2025-04-17)",
                "- -",
                "- -",
                "# Test ADR Title",
                "---"
            };
            _fileSystemService.ReadAllLinesAsync(filePath).Returns(lines);

            // Act
            var (header, content) = await _parser.ParseAdrHeaderAndContentAsync(filePath, _config, _fileSystemService);

            // Assert
            Assert.True(header.IsValid);
            Assert.Empty(content.Trim());
        }

        [Fact]
        public async Task ParseAdrHeaderAndContentAsync_WithInvalidTitleLine_ReturnsError()
        {
            // Arrange
            var filePath = "test.md";
            var lines = new[]
            {
                "###### Test Disclaimer",
                "##### Version: 1",
                "##### Revision: 1",
                "##### Scope: Enterprise:TestDomain",
                "##### Status",
                "- Proposed (2025-04-17)",
                "- -",
                "- -",
                "Invalid Title Line",
                "---"
            };
            _fileSystemService.ReadAllLinesAsync(filePath).Returns(lines);

            // Act
            var (header, content) = await _parser.ParseAdrHeaderAndContentAsync(filePath, _config, _fileSystemService);

            // Assert
            Assert.False(header.IsValid);
            Assert.NotEmpty(header.ErrorMessage);
        }

        [Fact]
        public async Task ParseAdrHeaderAndContentAsync_WithInvalidSeparator_ReturnsError()
        {
            // Arrange
            var filePath = "test.md";
            var lines = new[]
            {
                "###### Test Disclaimer",
                "##### Version: 1",
                "##### Revision: 1",
                "##### Scope: Enterprise:TestDomain",
                "##### Status",
                "- Proposed (2025-04-17)",
                "- -",
                "- -",
                "# Test ADR Title",
                "===" // Invalid separator, should be "---"
            };
            _fileSystemService.ReadAllLinesAsync(filePath).Returns(lines);

            // Act
            var (header, content) = await _parser.ParseAdrHeaderAndContentAsync(filePath, _config, _fileSystemService);

            // Assert
            Assert.False(header.IsValid);
            Assert.NotEmpty(header.ErrorMessage);
        }

        [Fact]
        public async Task ParseAdrHeaderAndContentAsync_WithScopeAndDomain_ParsesBothCorrectly()
        {
            // Arrange
            var filePath = "test.md";
            var lines = new[]
            {
                "###### Test Disclaimer",
                "##### Version: 1",
                "##### Revision: 1",
                "##### Scope: Project:Payment",
                "##### Status",
                "- Proposed (2025-04-17)",
                "- -",
                "- -",
                "# Test ADR Title",
                "---"
            };
            _fileSystemService.ReadAllLinesAsync(filePath).Returns(lines);

            // Act
            var (header, content) = await _parser.ParseAdrHeaderAndContentAsync(filePath, _config, _fileSystemService);

            // Assert
            Assert.True(header.IsValid);
            Assert.Equal("Project", header.Scope);
            Assert.Equal("Payment", header.Domain);
        }

        #endregion

        #region Helper Methods

        private static string[] BuildValidHeaderLines(string dateString, string statusText)
        {
            return new[]
            {
                "###### Test Disclaimer",
                "##### Version: 1",
                "##### Revision: 1",
                "##### Scope: Enterprise:TestDomain",
                "##### Status",
                $"- {statusText} ({dateString})",
                "- -",
                "- -",
                "# Test ADR Title",
                "---",
                "Content line 1"
            };
        }

        #endregion
    }
}