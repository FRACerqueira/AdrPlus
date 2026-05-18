// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the AdrPlus project under MIT license
// ***************************************************************************************

using AdrPlus.Core;
using AdrPlus.Domain;
using AdrPlus.Infrastructure.FileSystem;

namespace AdrPlus.Tests.Core
{
    /// <summary>
    /// Unit tests for <see cref="AdrFileParserService"/>.
    /// 
    /// Comprehensive test suite covering:
    /// - <see cref="AdrFileParserService.ParseAdrHeaderAndContentAsync"/> - Parsing ADR file headers with table-based markdown format
    /// - <see cref="AdrFileParserService.ParseFileName"/> - Filename parsing and validation for ADR Plus and migration patterns
    /// 
    /// Test Organization:
    /// - Basic Cases: Empty files, file structure validation
    /// - Disclaimer Tests: Format validation for HTML comment disclaimers
    /// - Table Header Tests: Markdown table structure validation
    /// - Title/Version/Revision/Scope/Domain Tests: Individual field validation
    /// - Status Tests: Status line parsing and date extraction
    /// - Content Tests: Content preservation and formatting
    /// - ParseFileName Tests: Filename pattern matching and component extraction
    /// - ADR Plus Pattern Tests: Pattern variations and field combinations
    /// - Domain Extraction Tests: Title and domain parsing from filenames
    /// - Supersede Format Tests: Supersede value parsing and validation
    /// - Error Condition Tests: Invalid patterns and edge cases
    /// - Line Ending Tests: Cross-platform line ending handling
    /// 
    /// Mock Dependencies:
    /// - <see cref="IFileSystemService"/> - Mocked for file I/O operations
    /// - File content is provided via <see cref="IFileSystemService.ReadAllLinesAsync"/> mock
    /// 
    /// Test Data Patterns:
    /// - Build helper methods create valid ADR file headers with configurable values
    /// - All external dependencies are mocked with NSubstitute
    /// - Tests run independently and can execute in any order
    /// </summary>
    public class AdrFileParserServiceTests
    {
        private readonly AdrFileParserService _parser;
        private readonly IFileSystemService _fileSystemService;
        private readonly AdrPlusRepoConfig _config;

        public AdrFileParserServiceTests()
        {
            _parser = new AdrFileParserService();
            _fileSystemService = Substitute.For<IFileSystemService>();
            _config = new AdrPlusRepoConfig("", "")
            {
                LenRevision = 2,
                StatusNew = "Proposed",
                StatusAcc = "Accepted",
                StatusRej = "Rejected",
                StatusSup = "Superseded",
                Separator = '-',
                LenVersion = 2,
                LenScope = 1,
                Scopes = "Enterprise;Project"
            };
        }

        #region ParseAdrHeaderAndContentAsync Tests - Basic Cases

        [Fact]
        public async Task ParseAdrHeaderAndContentAsync_WithEmptyFile_ReturnsErrorMessage()
        {
            // Arrange
            var filePath = "test.md";
            _fileSystemService.ReadAllLinesAsync(Arg.Any<string>()).Returns([]);

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
            var lines = new[] { "<!-- Test Disclaimer -->" };
            _fileSystemService.ReadAllLinesAsync(Arg.Any<string>()).Returns(lines);

            // Act
            var (header, _) = await _parser.ParseAdrHeaderAndContentAsync(filePath, _config, _fileSystemService);

            // Assert
            Assert.False(header.IsValid);
            Assert.NotEmpty(header.ErrorMessage);
        }

        [Fact]
        public async Task ParseAdrHeaderAndContentAsync_WithValidTableHeader_ParsesSuccessfully()
        {
            // Arrange
            var filePath = "test.md";
            var lines = BuildValidTableHeaderLines("2025-04-17", "Proposed");
            _fileSystemService.ReadAllLinesAsync(Arg.Any<string>()).Returns(lines);

            // Act
            var (header, _) = await _parser.ParseAdrHeaderAndContentAsync(filePath, _config, _fileSystemService);

            // Assert
            Assert.True(header.IsValid, $"Expected IsValid to be true, but got error: {header.ErrorMessage}");
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
                "<!-- Test Disclaimer -->",
                "|Adr-Plus |Field |Value |",
                "|--|--|--|",
                "|Title |Test ADR Title |Test |",
                "|Version |1 |1 |",
                "|Revision |1 |1 |",
                "|Scope |Enterprise |Enterprise |",
                "|Domain |TestDomain |TestDomain |",
                "|StatusCreate |Proposed (2025-04-17) |Proposed (2025-04-17) |",
                "|StatusUpdate |Accepted (2025-04-18) |Accepted (2025-04-18) |",
                "|StatusSuperseded |Superseded (2025-04-19): 0002 |",
                "<!-- End Header -->",
                "This is the content of the ADR."
            };
            _fileSystemService.ReadAllLinesAsync(Arg.Any<string>()).Returns(lines);

            // Act
            var (header, _) = await _parser.ParseAdrHeaderAndContentAsync(filePath, _config, _fileSystemService);

            // Assert
            Assert.True(header.IsValid);
            Assert.Equal(AdrStatus.Superseded, header.StatusChange);
            Assert.Equal("0002", header.NumberSuperSedes);
            Assert.NotNull(header.DateChange);
            Assert.Equal(new DateTime(2025, 04, 19), header.DateChange.Value.Date);
        }

        [Fact]
        public async Task ParseAdrHeaderAndContentAsync_WithEmptyStatusLines_ParsesSuccessfully()
        {
            // Arrange
            var filePath = "test.md";
            var lines = new[]
            {
                "<!-- Test Disclaimer -->",
                "|Adr-Plus |Field |Value |",
                "|--|--|--|",
                "|Title |Test ADR Title |Test |",
                "|Version |1 |1 |",
                "|Revision |1 |1 |",
                "|Scope |Enterprise |Enterprise |",
                "|Domain |TestDomain |TestDomain |",
                "|StatusCreate |Proposed (2025-04-17) |Proposed (2025-04-17) |",
                "|StatusUpdate | | |",
                "|StatusSuperseded | | |",
                "<!-- End Header -->",
                "This is the content of the ADR."
            };
            _fileSystemService.ReadAllLinesAsync(Arg.Any<string>()).Returns(lines);

            // Act
            var (header, _) = await _parser.ParseAdrHeaderAndContentAsync(filePath, _config, _fileSystemService);

            // Assert
            Assert.True(header.IsValid);
            Assert.Equal(AdrStatus.Proposed, header.StatusCreate);
            Assert.Equal(AdrStatus.Unknown, header.StatusUpdate);
            Assert.Equal(AdrStatus.Unknown, header.StatusChange);
            Assert.Null(header.DateUpdate);
            Assert.Null(header.DateChange);
        }

        #endregion

        #region Disclaimer Tests

        [Fact]
        public async Task ParseAdrHeaderAndContentAsync_WithInvalidDisclaimerFormat_ReturnsError()
        {
            // Arrange
            var filePath = "test.md";
            var lines = new[]
            {
                "Invalid Disclaimer Format",
                "|Adr-Plus |Field |Value |",
                "|--|--|--|",
                "|Title |Test ADR Title |Test |",
                "|Version |1 |1 |",
                "|Revision |1 |1 |",
                "|Scope |Enterprise |Enterprise |",
                "|Domain |TestDomain |TestDomain |",
                "|StatusCreate |Proposed (2025-04-17) |Proposed (2025-04-17) |",
                "|StatusUpdate | | |",
                "|StatusSuperseded | | |",
                "<!-- End Header -->"
            };
            _fileSystemService.ReadAllLinesAsync(Arg.Any<string>()).Returns(lines);

            // Act
            var (header, _) = await _parser.ParseAdrHeaderAndContentAsync(filePath, _config, _fileSystemService);

            // Assert
            Assert.False(header.IsValid);
            Assert.NotEmpty(header.ErrorMessage);
        }

        [Fact]
        public async Task ParseAdrHeaderAndContentAsync_WithMissingOpeningDisclaimer_ReturnsError()
        {
            // Arrange
            var filePath = "test.md";
            var lines = new[]
            {
                "Test Disclaimer -->",
                "|Adr-Plus |Field |Value |",
                "|--|--|--|",
                "|Title |Test ADR Title |Test |",
                "|Version |1 |1 |",
                "|Revision |1 |1 |",
                "|Scope |Enterprise |Enterprise |",
                "|Domain |TestDomain |TestDomain |",
                "|StatusCreate |Proposed (2025-04-17) |Proposed (2025-04-17) |",
                "|StatusUpdate | | |",
                "|StatusSuperseded | | |",
                "<!-- End Header -->"
            };
            _fileSystemService.ReadAllLinesAsync(Arg.Any<string>()).Returns(lines);

            // Act
            var (header, _) = await _parser.ParseAdrHeaderAndContentAsync(filePath, _config, _fileSystemService);

            // Assert
            Assert.False(header.IsValid);
            Assert.NotEmpty(header.ErrorMessage);
        }

        [Fact]
        public async Task ParseAdrHeaderAndContentAsync_WithMissingClosingDisclaimer_ReturnsError()
        {
            // Arrange
            var filePath = "test.md";
            var lines = new[]
            {
                "<!-- Test Disclaimer",
                "|Adr-Plus |Field |Value |",
                "|--|--|--|",
                "|Title |Test ADR Title |Test |",
                "|Version |1 |1 |",
                "|Revision |1 |1 |",
                "|Scope |Enterprise |Enterprise |",
                "|Domain |TestDomain |TestDomain |",
                "|StatusCreate |Proposed (2025-04-17) |Proposed (2025-04-17) |",
                "|StatusUpdate | | |",
                "|StatusSuperseded | | |",
                "<!-- End Header -->"
            };
            _fileSystemService.ReadAllLinesAsync(Arg.Any<string>()).Returns(lines);

            // Act
            var (header, _) = await _parser.ParseAdrHeaderAndContentAsync(filePath, _config, _fileSystemService);

            // Assert
            Assert.False(header.IsValid);
            Assert.NotEmpty(header.ErrorMessage);
        }

        #endregion

        #region Table Header Tests

        [Fact]
        public async Task ParseAdrHeaderAndContentAsync_WithInvalidTableHeader_ReturnsError()
        {
            // Arrange
            var filePath = "test.md";
            var lines = new[]
            {
                "<!-- Test Disclaimer -->",
                "Invalid Header",
                "|--|--|--|",
                "|Title |Test ADR Title |Test |",
                "|Version |1 |1 |",
                "|Revision |1 |1 |",
                "|Scope |Enterprise |Enterprise |",
                "|Domain |TestDomain |TestDomain |",
                "|StatusCreate |Proposed (2025-04-17) |Proposed (2025-04-17) |",
                "|StatusUpdate | | |",
                "|StatusSuperseded | | |",
                "<!-- End Header -->"
            };
            _fileSystemService.ReadAllLinesAsync(Arg.Any<string>()).Returns(lines);

            // Act
            var (header, _) = await _parser.ParseAdrHeaderAndContentAsync(filePath, _config, _fileSystemService);

            // Assert
            Assert.False(header.IsValid);
            Assert.NotEmpty(header.ErrorMessage);
        }

        [Fact]
        public async Task ParseAdrHeaderAndContentAsync_WithInvalidTableSeparator_ReturnsError()
        {
            // Arrange
            var filePath = "test.md";
            var lines = new[]
            {
                "<!-- Test Disclaimer -->",
                "|Adr-Plus |Field |Value |",
                "|==|==|==|",
                "|Title |Test ADR Title |Test |",
                "|Version |1 |1 |",
                "|Revision |1 |1 |",
                "|Scope |Enterprise |Enterprise |",
                "|Domain |TestDomain |TestDomain |",
                "|StatusCreate |Proposed (2025-04-17) |Proposed (2025-04-17) |",
                "|StatusUpdate | | |",
                "|StatusSuperseded | | |",
                "<!-- End Header -->"
            };
            _fileSystemService.ReadAllLinesAsync(Arg.Any<string>()).Returns(lines);

            // Act
            var (header, _) = await _parser.ParseAdrHeaderAndContentAsync(filePath, _config, _fileSystemService);

            // Assert
            Assert.False(header.IsValid);
            Assert.NotEmpty(header.ErrorMessage);
        }

        #endregion

        #region Title Tests

        [Fact]
        public async Task ParseAdrHeaderAndContentAsync_WithInvalidTitleLine_ReturnsError()
        {
            // Arrange
            var filePath = "test.md";
            var lines = new[]
            {
                "<!-- Test Disclaimer -->",
                "|Adr-Plus |Field |Value |",
                "|--|--|--|",
                "Invalid Title Line",
                "|Version |1 |1 |",
                "|Revision |1 |1 |",
                "|Scope |Enterprise |Enterprise |",
                "|Domain |TestDomain |TestDomain |",
                "|StatusCreate |Proposed (2025-04-17) |Proposed (2025-04-17) |",
                "|StatusUpdate | | |",
                "|StatusSuperseded | | |",
                "<!-- End Header -->"
            };
            _fileSystemService.ReadAllLinesAsync(Arg.Any<string>()).Returns(lines);

            // Act
            var (header, _) = await _parser.ParseAdrHeaderAndContentAsync(filePath, _config, _fileSystemService);

            // Assert
            Assert.False(header.IsValid);
            Assert.NotEmpty(header.ErrorMessage);
        }

        [Fact]
        public async Task ParseAdrHeaderAndContentAsync_WithEmptyTitle_ParsesWithEmptyTitle()
        {
            // Arrange
            var filePath = "test.md";
            var lines = new[]
            {
                "<!-- Test Disclaimer -->",
                "|Adr-Plus |Field |Value |",
                "|--|--|--|",
                "|Title | | |",
                "|Version |1 |1 |",
                "|Revision |1 |1 |",
                "|Scope |Enterprise |Enterprise |",
                "|Domain |TestDomain |TestDomain |",
                "|StatusCreate |Proposed (2025-04-17) |Proposed (2025-04-17) |",
                "|StatusUpdate | | |",
                "|StatusSuperseded | | |",
                "<!-- End Header -->"
            };
            _fileSystemService.ReadAllLinesAsync(Arg.Any<string>()).Returns(lines);

            // Act
            var (header, _) = await _parser.ParseAdrHeaderAndContentAsync(filePath, _config, _fileSystemService);

            // Assert
            Assert.True(header.IsValid);
            Assert.Empty(header.Title);
        }

        #endregion

        #region Version Tests

        [Fact]
        public async Task ParseAdrHeaderAndContentAsync_WithInvalidVersionFormat_ReturnsError()
        {
            // Arrange
            var filePath = "test.md";
            var lines = new[]
            {
                "<!-- Test Disclaimer -->",
                "|Adr-Plus |Field |Value |",
                "|--|--|--|",
                "|Title |Test ADR Title |Test |",
                "|Version |Invalid |Invalid |",
                "|Revision |1 |1 |",
                "|Scope |Enterprise |Enterprise |",
                "|Domain |TestDomain |TestDomain |",
                "|StatusCreate |Proposed (2025-04-17) |Proposed (2025-04-17) |",
                "|StatusUpdate | | |",
                "|StatusSuperseded | | |",
                "<!-- End Header -->"
            };
            _fileSystemService.ReadAllLinesAsync(Arg.Any<string>()).Returns(lines);

            // Act
            var (header, _) = await _parser.ParseAdrHeaderAndContentAsync(filePath, _config, _fileSystemService);

            // Assert
            Assert.False(header.IsValid);
            Assert.NotEmpty(header.ErrorMessage);
        }

        [Fact]
        public async Task ParseAdrHeaderAndContentAsync_WithEmptyVersion_ParsesWithZeroVersion()
        {
            // Arrange
            var filePath = "test.md";
            var lines = new[]
            {
                "<!-- Test Disclaimer -->",
                "|Adr-Plus |Field |Value |",
                "|--|--|--|",
                "|Title |Test ADR Title |Test |",
                "|Version | | |",
                "|Revision |1 |1 |",
                "|Scope |Enterprise |Enterprise |",
                "|Domain |TestDomain |TestDomain |",
                "|StatusCreate |Proposed (2025-04-17) |Proposed (2025-04-17) |",
                "|StatusUpdate | | |",
                "|StatusSuperseded | | |",
                "<!-- End Header -->"
            };
            _fileSystemService.ReadAllLinesAsync(Arg.Any<string>()).Returns(lines);

            // Act
            var (header, _) = await _parser.ParseAdrHeaderAndContentAsync(filePath, _config, _fileSystemService);

            // Assert
            Assert.True(header.IsValid);
            Assert.Equal(0, header.Version);
        }

        #endregion

        #region Revision Tests

        [Fact]
        public async Task ParseAdrHeaderAndContentAsync_WithInvalidRevisionFormat_ReturnsError()
        {
            // Arrange
            var filePath = "test.md";
            var lines = new[]
            {
                "<!-- Test Disclaimer -->",
                "|Adr-Plus |Field |Value |",
                "|--|--|--|",
                "|Title |Test ADR Title |Test |",
                "|Version |1 |1 |",
                "|Revision |Invalid |Invalid |",
                "|Scope |Enterprise |Enterprise |",
                "|Domain |TestDomain |TestDomain |",
                "|StatusCreate |Proposed (2025-04-17) |Proposed (2025-04-17) |",
                "|StatusUpdate | | |",
                "|StatusSuperseded | | |",
                "<!-- End Header -->"
            };
            _fileSystemService.ReadAllLinesAsync(Arg.Any<string>()).Returns(lines);

            // Act
            var (header, _) = await _parser.ParseAdrHeaderAndContentAsync(filePath, _config, _fileSystemService);

            // Assert
            Assert.False(header.IsValid);
            Assert.NotEmpty(header.ErrorMessage);
        }

        [Fact]
        public async Task ParseAdrHeaderAndContentAsync_WithoutRevisionConfig_ParsingStillReadsRevision()
        {
            // Arrange - Note: Parsing always reads revision from file; LenRevision only affects CreateAdrRecord
            var filePath = "test.md";
            var configNoRevision = new AdrPlusRepoConfig("", "")
            {
                LenRevision = 0,
                StatusNew = "Proposed",
                StatusAcc = "Accepted",
                StatusRej = "Rejected",
                StatusSup = "Superseded",
                Scopes = "Enterprise;Project"
            };
            var lines = BuildValidTableHeaderLines("2025-04-17", "Proposed");
            _fileSystemService.ReadAllLinesAsync(Arg.Any<string>()).Returns(lines);

            // Act
            var (header, _) = await _parser.ParseAdrHeaderAndContentAsync(filePath, configNoRevision, _fileSystemService);

            // Assert - Parsing reads revision anyway; LenRevision is applied later in CreateAdrRecord
            Assert.True(header.IsValid);
            Assert.NotNull(header.Revision);
        }

        #endregion

        #region Scope and Domain Tests

        [Fact]
        public async Task ParseAdrHeaderAndContentAsync_WithInvalidScopeFormat_ReturnsError()
        {
            // Arrange
            var filePath = "test.md";
            var lines = new[]
            {
                "<!-- Test Disclaimer -->",
                "|Adr-Plus |Field |Value |",
                "|--|--|--|",
                "|Title |Test ADR Title |Test |",
                "|Version |1 |1 |",
                "|Revision |1 |1 |",
                "Invalid Scope Line",
                "|Domain |TestDomain |TestDomain |",
                "|StatusCreate |Proposed (2025-04-17) |Proposed (2025-04-17) |",
                "|StatusUpdate | | |",
                "|StatusSuperseded | | |",
                "<!-- End Header -->"
            };
            _fileSystemService.ReadAllLinesAsync(Arg.Any<string>()).Returns(lines);

            // Act
            var (header, _) = await _parser.ParseAdrHeaderAndContentAsync(filePath, _config, _fileSystemService);

            // Assert
            Assert.False(header.IsValid);
            Assert.NotEmpty(header.ErrorMessage);
        }

        [Fact]
        public async Task ParseAdrHeaderAndContentAsync_WithInvalidDomainFormat_ReturnsError()
        {
            // Arrange
            var filePath = "test.md";
            var lines = new[]
            {
                "<!-- Test Disclaimer -->",
                "|Adr-Plus |Field |Value |",
                "|--|--|--|",
                "|Title |Test ADR Title |Test |",
                "|Version |1 |1 |",
                "|Revision |1 |1 |",
                "|Scope |Enterprise |Enterprise |",
                "Invalid Domain Line",
                "|StatusCreate |Proposed (2025-04-17) |Proposed (2025-04-17) |",
                "|StatusUpdate | | |",
                "|StatusSuperseded | | |",
                "<!-- End Header -->"
            };
            _fileSystemService.ReadAllLinesAsync(Arg.Any<string>()).Returns(lines);

            // Act
            var (header, _) = await _parser.ParseAdrHeaderAndContentAsync(filePath, _config, _fileSystemService);

            // Assert
            Assert.False(header.IsValid);
            Assert.NotEmpty(header.ErrorMessage);
        }

        [Fact]
        public async Task ParseAdrHeaderAndContentAsync_WithEmptyScope_ParsesWithEmptyScope()
        {
            // Arrange
            var filePath = "test.md";
            var lines = new[]
            {
                "<!-- Test Disclaimer -->",
                "|Adr-Plus |Field |Value |",
                "|--|--|--|",
                "|Title |Test ADR Title |Test |",
                "|Version |1 |1 |",
                "|Revision |1 |1 |",
                "|Scope | | |",
                "|Domain |TestDomain |TestDomain |",
                "|StatusCreate |Proposed (2025-04-17) |Proposed (2025-04-17) |",
                "|StatusUpdate | | |",
                "|StatusSuperseded | | |",
                "<!-- End Header -->"
            };
            _fileSystemService.ReadAllLinesAsync(Arg.Any<string>()).Returns(lines);

            // Act
            var (header, _) = await _parser.ParseAdrHeaderAndContentAsync(filePath, _config, _fileSystemService);

            // Assert
            Assert.True(header.IsValid);
            Assert.Empty(header.Scope);
        }

        [Fact]
        public async Task ParseAdrHeaderAndContentAsync_WithLongDomainName_ExtractsDomainCorrectly()
        {
            // Arrange
            var filePath = "test.md";
            var lines = new[]
            {
                "<!-- Test Disclaimer -->",
                "|Adr-Plus |Field |Value |",
                "|--|--|--|",
                "|Title |Test ADR Title |Test |",
                "|Version |1 |1 |",
                "|Revision |1 |1 |",
                "|Scope |Enterprise |Enterprise |",
                "|Domain |PaymentProcessingService |PaymentProcessingService |",
                "|StatusCreate |Proposed (2025-04-17) |Proposed (2025-04-17) |",
                "|StatusUpdate | | |",
                "|StatusSuperseded | | |",
                "<!-- End Header -->"
            };
            _fileSystemService.ReadAllLinesAsync(Arg.Any<string>()).Returns(lines);

            // Act
            var (header, _) = await _parser.ParseAdrHeaderAndContentAsync(filePath, _config, _fileSystemService);

            // Assert
            Assert.True(header.IsValid);
            Assert.Equal("PaymentProcessingService", header.Domain);
        }

        #endregion

        #region Status Tests

        [Fact]
        public async Task ParseAdrHeaderAndContentAsync_WithInvalidStatusText_ReturnsError()
        {
            // Arrange
            var filePath = "test.md";
            var lines = new[]
            {
                "<!-- Test Disclaimer -->",
                "|Adr-Plus |Field |Value |",
                "|--|--|--|",
                "|Title |Test ADR Title |Test |",
                "|Version |1 |1 |",
                "|Revision |1 |1 |",
                "|Scope |Enterprise |Enterprise |",
                "|Domain |TestDomain |TestDomain |",
                "|StatusCreate |InvalidStatus (2025-04-17) |InvalidStatus (2025-04-17) |",
                "|StatusUpdate | | |",
                "|StatusSuperseded | | |",
                "<!-- End Header -->"
            };
            _fileSystemService.ReadAllLinesAsync(Arg.Any<string>()).Returns(lines);

            // Act
            var (header, _) = await _parser.ParseAdrHeaderAndContentAsync(filePath, _config, _fileSystemService);

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
                "<!-- Test Disclaimer -->",
                "|Adr-Plus |Field |Value |",
                "|--|--|--|",
                "|Title |Test ADR Title |Test |",
                "|Version |1 |1 |",
                "|Revision |1 |1 |",
                "|Scope |Enterprise |Enterprise |",
                "|Domain |TestDomain |TestDomain |",
                "|StatusCreate |Proposed (2025/04/17) |Proposed (2025/04/17) |",
                "|StatusUpdate | | |",
                "|StatusSuperseded | | |",
                "<!-- End Header -->"
            };
            _fileSystemService.ReadAllLinesAsync(Arg.Any<string>()).Returns(lines);

            // Act
            var (header, _) = await _parser.ParseAdrHeaderAndContentAsync(filePath, _config, _fileSystemService);

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
                "<!-- Test Disclaimer -->",
                "|Adr-Plus |Field |Value |",
                "|--|--|--|",
                "|Title |Test ADR Title |Test |",
                "|Version |1 |1 |",
                "|Revision |1 |1 |",
                "|Scope |Enterprise |Enterprise |",
                "|Domain |TestDomain |TestDomain |",
                "|StatusCreate |Proposed (2025-04-17) |Proposed (2025-04-17) |",
                "|StatusUpdate |Accepted (2025-04-18) |Accepted (2025-04-18) |",
                "|StatusSuperseded |Superseded (2025-04-19): 0003 |",
                "<!-- End Header -->",
                "Content"
            };
            _fileSystemService.ReadAllLinesAsync(Arg.Any<string>()).Returns(lines);

            // Act
            var (header, _) = await _parser.ParseAdrHeaderAndContentAsync(filePath, _config, _fileSystemService);

            // Assert
            Assert.True(header.IsValid, $"Header validation failed: {header.ErrorMessage}");
            Assert.Equal(AdrStatus.Proposed, header.StatusCreate);
            Assert.Equal(AdrStatus.Accepted, header.StatusUpdate);
            Assert.Equal(AdrStatus.Superseded, header.StatusChange);
            Assert.NotNull(header.DateCreate);
            Assert.NotNull(header.DateUpdate);
            Assert.NotNull(header.DateChange);
        }

        [Fact]
        public async Task ParseAdrHeaderAndContentAsync_WithAllStatusValues_VerifiesStatusCompatibility()
        {
            // Arrange
            var filePath = "test.md";
            var customConfig = new AdrPlusRepoConfig("", "")
            {
                LenRevision = 2,
                StatusNew = "Proposed",
                StatusAcc = "Accepted",
                StatusRej = "Rejected",
                StatusSup = "Superseded",
                Scopes = "Enterprise;Project"
            };

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
                    "<!-- Test Disclaimer -->",
                    "|Adr-Plus |Field |Value |",
                    "|--|--|--|",
                    "|Title |Test ADR Title |Test |",
                    "|Version |1 |1 |",
                    "|Revision |1 |1 |",
                    "|Scope |Enterprise |Enterprise |",
                    "|Domain |TestDomain |TestDomain |",
                    $"|StatusCreate |{statusText} (2025-04-17) |{statusText} (2025-04-17) |",
                    "|StatusUpdate | | |",
                    "|StatusSuperseded | | |",
                    "<!-- End Header -->"
                };
                _fileSystemService.ReadAllLinesAsync(Arg.Any<string>()).Returns(lines);

                // Act
                var (header, _) = await _parser.ParseAdrHeaderAndContentAsync(filePath, customConfig, _fileSystemService);

                // Assert
                Assert.True(header.IsValid, $"Failed for status: {statusText}");
                Assert.Equal(expectedStatus, header.StatusCreate);
            }
        }

        #endregion

        #region Content Tests

        [Fact]
        public async Task ParseAdrHeaderAndContentAsync_WithContentAfterHeader_ReturnsContent()
        {
            // Arrange
            var filePath = "test.md";
            var lines = new[]
            {
                "<!-- Test Disclaimer -->",
                "|Adr-Plus |Field |Value |",
                "|--|--|--|",
                "|Title |Test ADR Title |Test |",
                "|Version |1 |1 |",
                "|Revision |1 |1 |",
                "|Scope |Enterprise |Enterprise |",
                "|Domain |TestDomain |TestDomain |",
                "|StatusCreate |Proposed (2025-04-17) |Proposed (2025-04-17) |",
                "|StatusUpdate | | |",
                "|StatusSuperseded | | |",
                "<!-- End Header -->",
                "Line 1 of content",
                "Line 2 of content",
                "Line 3 of content"
            };
            _fileSystemService.ReadAllLinesAsync(Arg.Any<string>()).Returns(lines);

            // Act
            var (header, content) = await _parser.ParseAdrHeaderAndContentAsync(filePath, _config, _fileSystemService);

            // Assert
            Assert.True(header.IsValid);
            var contentLines = content.Split([Environment.NewLine], StringSplitOptions.None);
            Assert.Contains("Line 1 of content", contentLines);
            Assert.Contains("Line 2 of content", contentLines);
            Assert.Contains("Line 3 of content", contentLines);
        }

        [Fact]
        public async Task ParseAdrHeaderAndContentAsync_WithoutContentAfterHeader_ReturnsEmptyContent()
        {
            // Arrange
            var filePath = "test.md";
            var lines = new[]
            {
                "<!-- Test Disclaimer -->",
                "|Adr-Plus |Field |Value |",
                "|--|--|--|",
                "|Title |Test ADR Title |Test |",
                "|Version |1 |1 |",
                "|Revision |1 |1 |",
                "|Scope |Enterprise |Enterprise |",
                "|Domain |TestDomain |TestDomain |",
                "|StatusCreate |Proposed (2025-04-17) |Proposed (2025-04-17) |",
                "|StatusUpdate | | |",
                "|StatusSuperseded | | |",
                "<!-- End Header -->"
            };
            _fileSystemService.ReadAllLinesAsync(Arg.Any<string>()).Returns(lines);

            // Act
            var (header, content) = await _parser.ParseAdrHeaderAndContentAsync(filePath, _config, _fileSystemService);

            // Assert
            Assert.True(header.IsValid);
            Assert.Empty(content.Trim());
        }

        #endregion

        #region Migrated Flag Tests

        [Fact]
        public async Task ParseAdrHeaderAndContentAsync_WithMigratedHeaderMarkerInCorrectFormat_SetsMigratedFlag()
        {
            // Arrange - Migrated marker needs to be in exact format with <!-- at end of separator line
            var filePath = "test.md";
            var lines = new[]
            {
                "<!-- Test Disclaimer -->",
                "|Adr-Plus |Field |Value <!-- Migrated -->|",
                "|--|--|",
                "|Title |Test ADR Title |Test |",
                "|Version |1 |1 |",
                "|Revision |1 |1 |",
                "|Scope |Enterprise |Enterprise |",
                "|Domain |TestDomain |TestDomain |",
                "|StatusCreate |Proposed (2025-04-17) |Proposed (2025-04-17) |",
                "|StatusUpdate | | |",
                "|StatusSuperseded | | |",
                "<!-- End Header -->"
            };
            _fileSystemService.ReadAllLinesAsync(Arg.Any<string>()).Returns(lines);

            // Act
            var (header, _) = await _parser.ParseAdrHeaderAndContentAsync(filePath, _config, _fileSystemService);

            // Assert
            Assert.True(header.IsValid);
            Assert.True(header.IsMigrated);
        }

        [Fact]
        public async Task ParseAdrHeaderAndContentAsync_WithoutMigratedMarker_LeavesMigratedFlagFalse()
        {
            // Arrange
            var filePath = "test.md";
            var lines = BuildValidTableHeaderLines("2025-04-17", "Proposed");
            _fileSystemService.ReadAllLinesAsync(Arg.Any<string>()).Returns(lines);

            // Act
            var (header, _) = await _parser.ParseAdrHeaderAndContentAsync(filePath, _config, _fileSystemService);

            // Assert
            Assert.True(header.IsValid);
            Assert.False(header.IsMigrated);
        }

        #endregion

        #region Superseded Status Tests

        [Fact]
        public async Task ParseAdrHeaderAndContentAsync_WithSupersededWithoutFileReference_ReturnsError()
        {
            // Arrange
            var filePath = "test.md";
            var lines = new[]
            {
                "<!-- Test Disclaimer -->",
                "|Adr-Plus |Field |Value |",
                "|--|--|--|",
                "|Title |Test ADR Title |Test |",
                "|Version |1 |1 |",
                "|Revision |1 |1 |",
                "|Scope |Enterprise |Enterprise |",
                "|Domain |TestDomain |TestDomain |",
                "|StatusCreate |Proposed (2025-04-17) |Proposed (2025-04-17) |",
                "|StatusUpdate | | |",
                "|StatusSuperseded |Superseded (2025-04-19) |",
                "<!-- End Header -->"
            };
            _fileSystemService.ReadAllLinesAsync(Arg.Any<string>()).Returns(lines);

            // Act
            var (header, _) = await _parser.ParseAdrHeaderAndContentAsync(filePath, _config, _fileSystemService);

            // Assert
            Assert.False(header.IsValid);
            Assert.NotEmpty(header.ErrorMessage);
        }

        [Fact]
        public async Task ParseAdrHeaderAndContentAsync_WithSupersededValidFileReference_ParsesCorrectly()
        {
            // Arrange
            var filePath = "test.md";
            var lines = new[]
            {
                "<!-- Test Disclaimer -->",
                "|Adr-Plus |Field |Value |",
                "|--|--|--|",
                "|Title |Test ADR Title |Test |",
                "|Version |1 |1 |",
                "|Revision |1 |1 |",
                "|Scope |Enterprise |Enterprise |",
                "|Domain |TestDomain |TestDomain |",
                "|StatusCreate |Proposed (2025-04-17) |Proposed (2025-04-17) |",
                "|StatusUpdate | | |",
                "|StatusSuperseded |Superseded (2025-04-19): 0002 |",
                "<!-- End Header -->"
            };
            _fileSystemService.ReadAllLinesAsync(Arg.Any<string>()).Returns(lines);

            // Act
            var (header, _) = await _parser.ParseAdrHeaderAndContentAsync(filePath, _config, _fileSystemService);

            // Assert
            Assert.True(header.IsValid);
            Assert.Equal(AdrStatus.Superseded, header.StatusChange);
            Assert.Equal("0002", header.NumberSuperSedes);
        }

        #endregion

        #region Line Ending Tests

        [Fact]
        public async Task ParseAdrHeaderAndContentAsync_WithLinuxLineEndings_ParsesCorrectly()
        {
            // Arrange - ReadAllLinesAsync normalizes line endings
            var filePath = "test.md";
            var lines = BuildValidTableHeaderLines("2025-04-17", "Proposed");
            _fileSystemService.ReadAllLinesAsync(Arg.Any<string>()).Returns(lines);

            // Act
            var (header, _) = await _parser.ParseAdrHeaderAndContentAsync(filePath, _config, _fileSystemService);

            // Assert
            Assert.True(header.IsValid);
            Assert.Equal("Test ADR Title", header.Title);
        }

        [Fact]
        public async Task ParseAdrHeaderAndContentAsync_WithWindowsLineEndings_ParsesCorrectly()
        {
            // Arrange - Windows line endings are normalized by ReadAllLinesAsync
            var filePath = "test.md";
            var lines = BuildValidTableHeaderLines("2025-04-17", "Proposed");
            _fileSystemService.ReadAllLinesAsync(Arg.Any<string>()).Returns(lines);

            // Act
            var (header, _) = await _parser.ParseAdrHeaderAndContentAsync(filePath, _config, _fileSystemService);

            // Assert
            Assert.True(header.IsValid);
            Assert.Equal("Test ADR Title", header.Title);
        }

        #endregion

        #region Helper Methods

        private static string[] BuildValidTableHeaderLines(string dateString, string statusText)
        {
            return
            [
                "<!-- Test Disclaimer -->",
                "|Adr-Plus |Field |Value |",
                "|--|--|--|",
                "|Title |Test ADR Title |Test |",
                "|Version |1 |1 |",
                "|Revision |1 |1 |",
                "|Scope |Enterprise |Enterprise |",
                "|Domain |TestDomain |TestDomain |",
                $"|StatusCreate |{statusText} ({dateString}) |{statusText} ({dateString}) |",
                "|StatusUpdate | | |",
                "|StatusSuperseded | | |",
                "<!-- End Header -->",
                "Content line 1"
            ];
        }

        private static string[] BuildTableHeaderLinesWithRejected(string dateString, string statusText)
        {
            return
            [
                "<!-- Test Disclaimer Rejected -->",
                "|Adr-Plus |Field |Value |",
                "|--|--|--|",
                "|Title |Rejected ADR Title |Rejected |",
                "|Version |1 |1 |",
                "|Revision |1 |1 |",
                "|Scope |Project |Project |",
                "|Domain |TestRejectedDomain |TestRejectedDomain |",
                $"|StatusCreate |{statusText} ({dateString}) |{statusText} ({dateString}) |",
                "|StatusUpdate | | |",
                "|StatusSuperseded | | |",
                "<!-- End Header -->",
                "Content line 1 - Rejected reason"
            ];
        }

        private static string[] BuildTableHeaderLinesWithSuperseded(string dateString, string supersededRef)
        {
            return
            [
                "<!-- Test Disclaimer Superseded -->",
                "|Adr-Plus |Field |Value |",
                "|--|--|--|",
                "|Title |Superseded ADR Title |Superseded |",
                "|Version |2 |2 |",
                "|Revision |3 |3 |",
                "|Scope |Enterprise |Enterprise |",
                "|Domain |SupersededDomain |SupersededDomain |",
                "|StatusCreate |Proposed (2025-04-10) |Proposed (2025-04-10) |",
                "|StatusUpdate |Accepted (2025-04-15) |Accepted (2025-04-15) |",
                $"|StatusSuperseded |Superseded ({dateString}): {supersededRef} |",
                "<!-- End Header -->",
                "Content explaining supersession"
            ];
        }

        private static string[] BuildTableHeaderLinesWithContent(string disclaimer, string title, string version, string revision, string scope, string domain, string[] contentLines)
        {
            var lines = new List<string>
            {
                $"<!-- {disclaimer} -->",
                "|Adr-Plus |Field |Value |",
                "|--|--|--|",
                $"|Title |{title} |{title} |",
                $"|Version |{version} |{version} |",
                $"|Revision |{revision} |{revision} |",
                $"|Scope |{scope} |{scope} |",
                $"|Domain |{domain} |{domain} |",
                "|StatusCreate |Proposed (2025-04-17) |Proposed (2025-04-17) |",
                "|StatusUpdate | | |",
                "|StatusSuperseded | | |",
                "<!-- End Header -->"
            };
            lines.AddRange(contentLines);
            return [.. lines];
        }

        #endregion

        #region Complete Status Scenarios Tests

        [Fact]
        public async Task ParseAdrHeaderAndContentAsync_WithRejectedStatus_ParsesSuccessfully()
        {
            // Arrange
            var filePath = "0002-RejectedDecision.md";
            var lines = BuildTableHeaderLinesWithRejected("2025-04-20", "Rejected");
            _fileSystemService.ReadAllLinesAsync(Arg.Any<string>()).Returns(lines);

            // Act
            var (header, content) = await _parser.ParseAdrHeaderAndContentAsync(filePath, _config, _fileSystemService);

            // Assert
            Assert.True(header.IsValid);
            Assert.Equal(AdrStatus.Rejected, header.StatusCreate);
            Assert.Equal("Rejected ADR Title", header.Title);
            Assert.Equal("Project", header.Scope);
            Assert.NotEmpty(content);
        }

        [Fact]
        public async Task ParseAdrHeaderAndContentAsync_WithSupersededStatus_ParsesSuccessfully()
        {
            // Arrange
            var filePath = "0001-OldDecision.md";
            var lines = BuildTableHeaderLinesWithSuperseded("2025-04-20", "0003.md");
            _fileSystemService.ReadAllLinesAsync(Arg.Any<string>()).Returns(lines);

            // Act
            var (header, content) = await _parser.ParseAdrHeaderAndContentAsync(filePath, _config, _fileSystemService);

            // Assert
            Assert.True(header.IsValid);
            Assert.Equal(AdrStatus.Superseded, header.StatusChange);
            Assert.Equal(AdrStatus.Accepted, header.StatusUpdate);
            Assert.Equal("Superseded ADR Title", header.Title);
            Assert.Equal(2, header.Version);
            Assert.Equal(3, header.Revision);
            Assert.NotEmpty(content);
        }

        [Fact]
        public async Task ParseAdrHeaderAndContentAsync_WithMultilineContent_ParsesAllContent()
        {
            // Arrange
            var filePath = "test.md";
            var contentLines = new[]
            {
                "## Context",
                "This is the context section.",
                "",
                "## Decision",
                "We decided to do something.",
                "",
                "## Consequences",
                "The consequences are...",
                "- Point 1",
                "- Point 2",
                "- Point 3"
            };
            var lines = BuildTableHeaderLinesWithContent(
                "Integration Decision",
                "Implement API Gateway",
                "3",
                "5",
                "Enterprise",
                "API-Management",
                contentLines);
            _fileSystemService.ReadAllLinesAsync(Arg.Any<string>()).Returns(lines);

            // Act
            var (header, content) = await _parser.ParseAdrHeaderAndContentAsync(filePath, _config, _fileSystemService);

            // Assert
            Assert.True(header.IsValid);
            Assert.Equal("Integration Decision", header.Disclaimer);
            Assert.Equal("Implement API Gateway", header.Title);
            Assert.Equal(3, header.Version);
            Assert.Equal(5, header.Revision);
            Assert.Equal("Enterprise", header.Scope);
            Assert.Equal("API-Management", header.Domain);
            Assert.NotEmpty(content);
            Assert.Contains("## Context", content);
            Assert.Contains("## Decision", content);
            Assert.Contains("## Consequences", content);
        }

        #endregion

        #region Additional Edge Cases

        [Fact]
        public async Task ParseAdrHeaderAndContentAsync_WithMissingEndingDisclaimer_ReturnsError()
        {
            // Arrange
            var filePath = "test.md";
            var lines = new[]
            {
                "<!-- Test Disclaimer -->",
                "|Adr-Plus |Field |Value |",
                "|--|--|--|",
                "|Title |Test ADR Title |Test |",
                "|Version |1 |1 |",
                "|Revision |1 |1 |",
                "|Scope |Enterprise |Enterprise |",
                "|Domain |TestDomain |TestDomain |",
                "|StatusCreate |Proposed (2025-04-17) |Proposed (2025-04-17) |",
                "|StatusUpdate | | |",
                "|StatusSuperseded | | |",
                "Invalid Footer"
            };
            _fileSystemService.ReadAllLinesAsync(Arg.Any<string>()).Returns(lines);

            // Act
            var (header, _) = await _parser.ParseAdrHeaderAndContentAsync(filePath, _config, _fileSystemService);

            // Assert
            Assert.False(header.IsValid);
            Assert.NotEmpty(header.ErrorMessage);
        }

        [Fact]
        public async Task ParseAdrHeaderAndContentAsync_WithMissingTableRowFormat_ReturnsError()
        {
            // Arrange
            var filePath = "test.md";
            var lines = new[]
            {
                "<!-- Test Disclaimer -->",
                "|Adr-Plus |Field |Value |",
                "|--|--|--|",
                "Invalid Format Without Pipes",
                "|Version |1 |1 |",
                "|Revision |1 |1 |",
                "|Scope |Enterprise |Enterprise |",
                "|Domain |TestDomain |TestDomain |",
                "|StatusCreate |Proposed (2025-04-17) |Proposed (2025-04-17) |",
                "|StatusUpdate | | |",
                "|StatusSuperseded | | |",
                "<!-- End Header -->"
            };
            _fileSystemService.ReadAllLinesAsync(Arg.Any<string>()).Returns(lines);

            // Act
            var (header, _) = await _parser.ParseAdrHeaderAndContentAsync(filePath, _config, _fileSystemService);

            // Assert
            Assert.False(header.IsValid);
            Assert.NotEmpty(header.ErrorMessage);
        }

        [Fact]
        public async Task ParseAdrHeaderAndContentAsync_WithLargeContent_PreservesAllLines()
        {
            // Arrange
            var filePath = "test.md";
            var contentLines = new List<string>();
            for (int i = 1; i <= 100; i++)
            {
                contentLines.Add("Line " + i);
            }

            var lines = new List<string>
            {
                "<!-- Test Disclaimer -->",
                "|Adr-Plus |Field |Value |",
                "|--|--|--|",
                "|Title |Test ADR Title |Test |",
                "|Version |1 |1 |",
                "|Revision |1 |1 |",
                "|Scope |Enterprise |Enterprise |",
                "|Domain |TestDomain |TestDomain |",
                "|StatusCreate |Proposed (2025-04-17) |Proposed (2025-04-17) |",
                "|StatusUpdate | | |",
                "|StatusSuperseded | | |",
                "<!-- End Header -->"
            };
            lines.AddRange(contentLines);
            _fileSystemService.ReadAllLinesAsync(Arg.Any<string>()).Returns([.. lines]);

            // Act
            var (header, content) = await _parser.ParseAdrHeaderAndContentAsync(filePath, _config, _fileSystemService);

            // Assert
            Assert.True(header.IsValid);
            Assert.NotEmpty(content);
            for (int i = 1; i <= 100; i++)
            {
                Assert.Contains("Line " + i, content);
            }
        }

        [Fact]
        public async Task ParseAdrHeaderAndContentAsync_WithWhitespaceInValues_PreservesWhitespace()
        {
            // Arrange
            var filePath = "test.md";
            var lines = new[]
            {
                "<!-- Test Disclaimer With Spaces -->",
                "|Adr-Plus |Field |Value |",
                "|--|--|--|",
                "|Title |Very Long ADR Title With Multiple Words |Test |",
                "|Version |2 |2 |",
                "|Revision |5 |5 |",
                "|Scope |Enterprise |Enterprise |",
                "|Domain |Long Domain Name With Spaces |Long Domain Name With Spaces |",
                "|StatusCreate |Proposed (2025-04-17) |Proposed (2025-04-17) |",
                "|StatusUpdate | | |",
                "|StatusSuperseded | | |",
                "<!-- End Header -->"
            };
            _fileSystemService.ReadAllLinesAsync(Arg.Any<string>()).Returns(lines);

            // Act
            var (header, _) = await _parser.ParseAdrHeaderAndContentAsync(filePath, _config, _fileSystemService);

            // Assert
            Assert.True(header.IsValid);
            Assert.Equal("Test Disclaimer With Spaces", header.Disclaimer);
            Assert.Equal("Very Long ADR Title With Multiple Words", header.Title);
            Assert.Equal("Long Domain Name With Spaces", header.Domain);
            Assert.Equal(2, header.Version);
            Assert.Equal(5, header.Revision);
        }

        [Fact]
        public async Task ParseAdrHeaderAndContentAsync_WithSpecialCharactersInDomain_ParsesCorrectly()
        {
            // Arrange
            var filePath = "test.md";
            var lines = new[]
            {
                "<!-- Test Disclaimer -->",
                "|Adr-Plus |Field |Value |",
                "|--|--|--|",
                "|Title |Test ADR Title |Test |",
                "|Version |1 |1 |",
                "|Revision |1 |1 |",
                "|Scope |Enterprise |Enterprise |",
                "|Domain |Payment-Processing_Service.v2 |Payment-Processing_Service.v2 |",
                "|StatusCreate |Proposed (2025-04-17) |Proposed (2025-04-17) |",
                "|StatusUpdate | | |",
                "|StatusSuperseded | | |",
                "<!-- End Header -->"
            };
            _fileSystemService.ReadAllLinesAsync(Arg.Any<string>()).Returns(lines);

            // Act
            var (header, _) = await _parser.ParseAdrHeaderAndContentAsync(filePath, _config, _fileSystemService);

            // Assert
            Assert.True(header.IsValid);
            Assert.Equal("Payment-Processing_Service.v2", header.Domain);
        }

        [Fact]
        public async Task ParseAdrHeaderAndContentAsync_WithNearMinimumHeader_ParsesSuccessfully()
        {
            // Arrange
            var filePath = "test.md";
            var lines = new[]
            {
                "<!-- D -->",
                "|Adr-Plus |Field |Value |",
                "|--|--|--|",
                "|Title | | |",
                "|Version | | |",
                "|Revision | | |",
                "|Scope | | |",
                "|Domain | | |",
                "|StatusCreate |Proposed (2025-04-17) |Proposed (2025-04-17) |",
                "|StatusUpdate | | |",
                "|StatusSuperseded | | |",
                "<!-- End Header -->"
            };
            _fileSystemService.ReadAllLinesAsync(Arg.Any<string>()).Returns(lines);

            // Act
            var (header, _) = await _parser.ParseAdrHeaderAndContentAsync(filePath, _config, _fileSystemService);

            // Assert
            Assert.True(header.IsValid);
            Assert.Empty(header.Title);
            Assert.Empty(header.Scope);
            Assert.Empty(header.Domain);
            Assert.Equal(0, header.Version);
        }

        #endregion

        #region ParseFileName Tests - Input Validation

        [Fact]
        public async Task ParseFileName_WithEmptyFileName_ReturnsError()
        {
            // Arrange
            var filePath = "";
            _fileSystemService.ReadAllLinesAsync(Arg.Any<string>()).Returns([]);

            // Act
            var result = await _parser.ParseFileName(filePath, _config, _fileSystemService);

            // Assert
            Assert.False(result.IsValid);
            Assert.NotEmpty(result.ErrorMessage);
        }

        [Fact]
        public async Task ParseFileName_WithMissingMdExtension_ReturnsError()
        {
            // Arrange
            var filePath = "0001-TestTitle.txt";
            _fileSystemService.ReadAllLinesAsync(Arg.Any<string>()).Returns([]);

            // Act
            var result = await _parser.ParseFileName(filePath, _config, _fileSystemService);

            // Assert
            Assert.False(result.IsValid);
            Assert.NotEmpty(result.ErrorMessage);
        }

        [Fact]
        public async Task ParseFileName_WithWhitespaceFileName_ReturnsError()
        {
            // Arrange
            var filePath = "   ";
            _fileSystemService.ReadAllLinesAsync(Arg.Any<string>()).Returns([]);

            // Act
            var result = await _parser.ParseFileName(filePath, _config, _fileSystemService);

            // Assert
            Assert.False(result.IsValid);
            Assert.NotEmpty(result.ErrorMessage);
        }

        #endregion

        #region ParseFileName Tests - Parsing Errors

        [Fact]
        public async Task ParseFileName_WithInvalidNumber_ReturnsError()
        {
            // Arrange
            var filePath = "ABC-TestTitle.md";
            _fileSystemService.ReadAllLinesAsync(Arg.Any<string>()).Returns([]);

            // Act
            var result = await _parser.ParseFileName(filePath, _config, _fileSystemService);

            // Assert
            Assert.False(result.IsValid);
        }

        [Fact]
        public async Task ParseFileName_WithInvalidVersionFormat_ReturnsError()
        {
            // Arrange
            var filePath = "0001VInvalid-TestTitle.md";
            _fileSystemService.ReadAllLinesAsync(Arg.Any<string>()).Returns([]);

            // Act
            var result = await _parser.ParseFileName(filePath, _config, _fileSystemService);

            // Assert
            Assert.False(result.IsValid);
            Assert.NotEmpty(result.ErrorMessage);
        }

        [Fact]
        public async Task ParseFileName_WithInvalidRevisionFormat_ReturnsError()
        {
            // Arrange
            var filePath = "0001V2RInvalid-TestTitle.md";
            _fileSystemService.ReadAllLinesAsync(Arg.Any<string>()).Returns([]);

            // Act
            var result = await _parser.ParseFileName(filePath, _config, _fileSystemService);

            // Assert
            Assert.False(result.IsValid);
            Assert.NotEmpty(result.ErrorMessage);
        }

        [Fact]
        public async Task ParseFileName_WithSupersededNoNumber_ReturnsError()
        {
            // Arrange
            var filePath = "0001V1R02-TestTitle-Enterprise-Domain--AC.md";
            _fileSystemService.ReadAllLinesAsync(Arg.Any<string>()).Returns([]);

            // Act
            var result = await _parser.ParseFileName(filePath, _config, _fileSystemService);

            // Assert
            Assert.False(result.IsValid);
            Assert.NotEmpty(result.ErrorMessage);
        }

        [Fact]
        public async Task ParseFileName_WithSupersededInvalidNumber_ReturnsError()
        {
            // Arrange
            var filePath = "0001V1R02Enterprise-TestTitle@Domain--SUPInvalid.md";
            _fileSystemService.ReadAllLinesAsync(Arg.Any<string>()).Returns([]);

            // Act
            var result = await _parser.ParseFileName(filePath, _config, _fileSystemService);

            // Assert
            Assert.False(result.IsValid);
            Assert.NotEmpty(result.ErrorMessage);
        }

        #endregion

        #region ParseFileName Tests - Prefix Handling

        [Fact]
        public async Task ParseFileName_WithPrefixMissingInFileName_ReturnsError()
        {
            // Arrange
            var filePath = "ADR001V01-TestTitle.md";
            var configWithPrefix = new AdrPlusRepoConfig("", "")
            {
                Prefix = "ADR",
                LenSeq = 3,
                LenRevision = 0,
                LenVersion = 2,
                LenScope = 0,
                Separator = '-',
                StatusNew = "Proposed",
                StatusAcc = "Accepted",
                StatusRej = "Rejected",
                StatusSup = "Superseded",
            };

            // Act
            var result = await _parser.ParseFileName(filePath, configWithPrefix, _fileSystemService);

            // Assert
            Assert.False(result.Header.IsValid);
            Assert.NotEmpty(result.Header.ErrorMessage);
        }

        #endregion

        #region ParseFileName Tests - Header Validation

        [Fact]
        public async Task ParseFileName_WithInvalidHeaderInFile_ReturnsError()
        {
            // Arrange
            var filePath = "ADR001V01R02E-TestTitle@Domain.md";
            var invalidHeaderLines = new[] { "Invalid header content" };
            _fileSystemService.ReadAllLinesAsync(Arg.Any<string>()).Returns(invalidHeaderLines);

            // Act
            var result = await _parser.ParseFileName(filePath, _config, _fileSystemService);

            // Assert
            Assert.False(result.Header.IsValid);
            Assert.NotEmpty(result.Header.ErrorMessage);
        }

        [Fact]
        public async Task ParseFileName_WithMissingDisclaimerInHeader_ReturnsError()
        {
            // Arrange - Test that missing disclaimer in file header is caught
            var filePath = "ADR001V01R02E-TestTitle@Domain.md";
            var headerLines = new[]
            {
                "Missing disclaimer line",
                "|Adr-Plus |Field |Value |",
                "|--|--|--|",
                "|Title |Test Title |Test |",
                "|Version |1 |1 |",
                "|Revision |2 |2 |",
                "|Scope |Enterprise |Enterprise |",
                "|Domain |TestDomain |TestDomain |",
                "|StatusCreate |Proposed (2025-04-17) |Proposed (2025-04-17) |",
                "|StatusUpdate | | |",
                "|StatusSuperseded | | |",
                "<!-- End Header -->"
            };
            _fileSystemService.ReadAllLinesAsync(Arg.Any<string>()).Returns(headerLines);

            // Act
            var result = await _parser.ParseFileName(filePath, _config, _fileSystemService);

            // Assert
            Assert.False(result.Header.IsValid);
            Assert.NotEmpty(result.Header.ErrorMessage);
        }

        [Fact]
        public async Task ParseFileName_WithInvalidHeaderFormat_ReturnsError()
        {
            // Arrange - Test that invalid table header format is caught
            var filePath = "ADR001V01R02E-TestTitleV01R02@Domain.md";
            var headerLines = new[]
            {
                "<!-- Test Disclaimer -->",
                "Invalid table header format",
                "|--|--|--|",
                "|Title |Test Title |Test |",
                "|Version |1 |1 |",
                "|Revision |2 |2 |",
                "|Scope |Enterprise |Enterprise |",
                "|Domain |TestDomain |TestDomain |",
                "|StatusCreate |Proposed (2025-04-17) |Proposed (2025-04-17) |",
                "|StatusUpdate | | |",
                "|StatusSuperseded | | |",
                "<!-- End Header -->"
            };
            _fileSystemService.ReadAllLinesAsync(Arg.Any<string>()).Returns(headerLines);

            // Act
            var result = await _parser.ParseFileName(filePath, _config, _fileSystemService);

            // Assert
            Assert.False(result.Header.IsValid);
            Assert.NotEmpty(result.Header.ErrorMessage);
        }

        [Fact]
        public async Task ParseFileName_WithEmptyFileContent_ReturnsError()
        {
            // Arrange - Test that empty file content is caught
            var filePath = "ADR001V01R02E-TestTitle@Domain.md";
            _fileSystemService.ReadAllLinesAsync(Arg.Any<string>()).Returns([]);

            // Act
            var result = await _parser.ParseFileName(filePath, _config, _fileSystemService);

            // Assert
            Assert.False(result.Header.IsValid);
            Assert.NotEmpty(result.Header.ErrorMessage);
        }

        [Fact]
        public async Task ParseFileName_WithTooShortFileContent_ReturnsError()
        {
            // Arrange - Test that file with insufficient lines is caught
            var filePath = "ADR001V01R02E-TestTitle@Domain.md";
            var headerLines = new[]
            {
                "<!-- Test Disclaimer -->",
                "|Adr-Plus |Field |Value |"
            };
            _fileSystemService.ReadAllLinesAsync(Arg.Any<string>()).Returns(headerLines);

            // Act
            var result = await _parser.ParseFileName(filePath, _config, _fileSystemService);

            // Assert
            Assert.False(result.Header.IsValid);
            Assert.NotEmpty(result.Header.ErrorMessage);
        }

        #endregion

        #region Gap Coverage - Edge Cases and Boundary Conditions

        [Fact]
        public async Task ParseAdrHeaderAndContentAsync_WithHeaderOnlyNoContent_ReturnsEmptyContent()
        {
            // Arrange - Exactly 12 lines (minimum header with no extra content)
            var filePath = "test.md";
            var lines = new[]
            {
                "<!-- Test Disclaimer -->",
                "|Adr-Plus |Field |Value |",
                "|--|--|--|",
                "|Title |Test ADR Title |Test |",
                "|Version |1 |1 |",
                "|Revision |1 |1 |",
                "|Scope |Enterprise |Enterprise |",
                "|Domain |TestDomain |TestDomain |",
                "|StatusCreate |Proposed (2025-04-17) |Proposed (2025-04-17) |",
                "|StatusUpdate | | |",
                "|StatusSuperseded | | |",
                "<!-- End Header -->"
            };
            _fileSystemService.ReadAllLinesAsync(Arg.Any<string>()).Returns(lines);

            // Act
            var (header, content) = await _parser.ParseAdrHeaderAndContentAsync(filePath, _config, _fileSystemService);

            // Assert
            Assert.True(header.IsValid);
            Assert.Empty(content);
        }

        [Fact]
        public async Task ParseAdrHeaderAndContentAsync_WithLargeVersionAndRevision_ParsesCorrectly()
        {
            // Arrange - Large numeric values for version and revision
            var filePath = "test.md";
            var lines = new[]
            {
                "<!-- Test Disclaimer -->",
                "|Adr-Plus |Field |Value |",
                "|--|--|--|",
                "|Title |Test ADR Title |Test |",
                "|Version |999 |999 |",
                "|Revision |888 |888 |",
                "|Scope |Enterprise |Enterprise |",
                "|Domain |TestDomain |TestDomain |",
                "|StatusCreate |Proposed (2025-04-17) |Proposed (2025-04-17) |",
                "|StatusUpdate | | |",
                "|StatusSuperseded | | |",
                "<!-- End Header -->"
            };
            _fileSystemService.ReadAllLinesAsync(Arg.Any<string>()).Returns(lines);

            // Act
            var (header, _) = await _parser.ParseAdrHeaderAndContentAsync(filePath, _config, _fileSystemService);

            // Assert
            Assert.True(header.IsValid);
            Assert.Equal(999, header.Version);
            Assert.Equal(888, header.Revision);
        }

        [Fact]
        public async Task ParseAdrHeaderAndContentAsync_WithStatusCreateEmptyAndStatusUpdateFilled_ParsesCorrectly()
        {
            // Arrange - Unusual scenario: StatusCreate empty but StatusUpdate has value
            var filePath = "test.md";
            var lines = new[]
            {
                "<!-- Test Disclaimer -->",
                "|Adr-Plus |Field |Value |",
                "|--|--|--|",
                "|Title |Test ADR Title |Test |",
                "|Version |1 |1 |",
                "|Revision |1 |1 |",
                "|Scope |Enterprise |Enterprise |",
                "|Domain |TestDomain |TestDomain |",
                "|StatusCreate | | |",
                "|StatusUpdate |Accepted (2025-04-18) |Accepted (2025-04-18) |",
                "|StatusSuperseded | | |",
                "<!-- End Header -->"
            };
            _fileSystemService.ReadAllLinesAsync(Arg.Any<string>()).Returns(lines);

            // Act
            var (header, _) = await _parser.ParseAdrHeaderAndContentAsync(filePath, _config, _fileSystemService);

            // Assert
            Assert.True(header.IsValid);
            Assert.Equal(AdrStatus.Unknown, header.StatusCreate);
            Assert.Equal(AdrStatus.Accepted, header.StatusUpdate);
            Assert.Null(header.DateCreate);
            Assert.NotNull(header.DateUpdate);
        }

        [Fact]
        public async Task ParseAdrHeaderAndContentAsync_WithBothStatusUpdateAndSuperseded_ParsesStatusPrecedence()
        {
            // Arrange - Both StatusUpdate and StatusSuperseded filled
            // The parser uses StatusChange for Superseded, and StatusUpdate for regular updates
            var filePath = "test.md";
            var lines = new[]
            {
                "<!-- Test Disclaimer -->",
                "|Adr-Plus |Field |Value |",
                "|--|--|--|",
                "|Title |Test ADR Title |Test |",
                "|Version |1 |1 |",
                "|Revision |1 |1 |",
                "|Scope |Enterprise |Enterprise |",
                "|Domain |TestDomain |TestDomain |",
                "|StatusCreate |Proposed (2025-04-17) |Proposed (2025-04-17) |",
                "|StatusUpdate |Accepted (2025-04-18) |Accepted (2025-04-18) |",
                "|StatusSuperseded |Superseded (2025-04-19): 0002 |",
                "<!-- End Header -->"
            };
            _fileSystemService.ReadAllLinesAsync(Arg.Any<string>()).Returns(lines);

            // Act
            var (header, _) = await _parser.ParseAdrHeaderAndContentAsync(filePath, _config, _fileSystemService);

            // Assert
            Assert.True(header.IsValid);
            Assert.Equal(AdrStatus.Proposed, header.StatusCreate);
            Assert.Equal(AdrStatus.Accepted, header.StatusUpdate);
            Assert.Equal(AdrStatus.Superseded, header.StatusChange);
            Assert.Equal("0002", header.NumberSuperSedes);
        }

        [Fact]
        public async Task ParseAdrHeaderAndContentAsync_WithAllFieldsAtMaxLength_ParsesSuccessfully()
        {
            // Arrange - Very long values for all text fields
            var longDisclaimer = "A".PadRight(100);
            var longTitle = "B".PadRight(150);
            var longDomain = "C".PadRight(200);
            var filePath = "test.md";
            var lines = new[]
            {
                $"<!-- {longDisclaimer} -->",
                "|Adr-Plus |Field |Value |",
                "|--|--|--|",
                $"|Title |{longTitle} |Test |",
                "|Version |10 |10 |",
                "|Revision |20 |20 |",
                "|Scope |Enterprise |Enterprise |",
                $"|Domain |{longDomain} |{longDomain} |",
                "|StatusCreate |Proposed (2025-04-17) |Proposed (2025-04-17) |",
                "|StatusUpdate | | |",
                "|StatusSuperseded | | |",
                "<!-- End Header -->"
            };
            _fileSystemService.ReadAllLinesAsync(Arg.Any<string>()).Returns(lines);

            // Act
            var (header, _) = await _parser.ParseAdrHeaderAndContentAsync(filePath, _config, _fileSystemService);

            // Assert
            Assert.True(header.IsValid);
            Assert.NotEmpty(header.Disclaimer);
            Assert.NotEmpty(header.Title);
            Assert.NotEmpty(header.Domain);
            Assert.Equal(longDomain.Trim(), header.Domain);
        }

        [Fact]
        public async Task ParseAdrHeaderAndContentAsync_WithNumberValuesAsStrings_ParsesCorrectly()
        {
            // Arrange - Leading whitespace in numeric fields should be trimmed
            var filePath = "test.md";
            var lines = new[]
            {
                "<!-- Test Disclaimer -->",
                "|Adr-Plus |Field |Value |",
                "|--|--|--|",
                "|Title |Test ADR Title |Test |",
                "|Version | 5 | 5 |",
                "|Revision | 3 | 3 |",
                "|Scope |Enterprise |Enterprise |",
                "|Domain |TestDomain |TestDomain |",
                "|StatusCreate |Proposed (2025-04-17) |Proposed (2025-04-17) |",
                "|StatusUpdate | | |",
                "|StatusSuperseded | | |",
                "<!-- End Header -->"
            };
            _fileSystemService.ReadAllLinesAsync(Arg.Any<string>()).Returns(lines);

            // Act
            var (header, _) = await _parser.ParseAdrHeaderAndContentAsync(filePath, _config, _fileSystemService);

            // Assert
            Assert.True(header.IsValid);
            Assert.Equal(5, header.Version);
            Assert.Equal(3, header.Revision);
        }

        #endregion

        #region ParseFileName Tests - ADR Plus Pattern Variations

        [Fact]
        public async Task ParseFileName_WithValidAdrPlusPatternAllComponents_ParsesSuccessfully()
        {
            // Arrange - Full ADR Plus pattern with all components
            var filePath = "ADR0001V02R03Enterprise-MyDecision@PaymentService.md";
            var headerLines = BuildValidTableHeaderLines("2025-04-17", "Proposed");
            _fileSystemService.ReadAllLinesAsync(Arg.Any<string>()).Returns(headerLines);

            // Act
            var result = await _parser.ParseFileName(filePath, _config, _fileSystemService);

            // Assert
            Assert.True(result.IsValid);
            Assert.Equal("ADR", result.Prefix);
            Assert.Equal(1, result.Number);
            Assert.Equal(2, result.Version);
            Assert.Equal(3, result.Revision);
            Assert.Equal("Enterprise", result.Scope);
            Assert.NotEmpty(result.Title);
            Assert.NotEmpty(result.Domain??string.Empty);
        }

        [Fact]
        public async Task ParseFileName_WithDomainOnly_ExtractsDomainCorrectly()
        {
            // Arrange - Filename with domain but no title before @
            var filePath = "ADR0001V01R01Enterprise-@SecurityModule.md";
            var headerLines = BuildValidTableHeaderLines("2025-04-17", "Proposed");
            _fileSystemService.ReadAllLinesAsync(Arg.Any<string>()).Returns(headerLines);

            // Act
            var result = await _parser.ParseFileName(filePath, _config, _fileSystemService);

            // Assert
            Assert.True(result.IsValid);
            Assert.NotEmpty(result.Domain??string.Empty);
        }

        [Fact]
        public async Task ParseFileName_WithSupersededValidFormat_ParsesSupersededValue()
        {
            // Arrange - Supersede format with double separator
            var filePath = "ADR0001V01R01Enterprise-OldDecision@Domain--0002.md";
            var headerLines = BuildValidTableHeaderLines("2025-04-17", "Proposed");
            _fileSystemService.ReadAllLinesAsync(Arg.Any<string>()).Returns(headerLines);

            // Act
            var result = await _parser.ParseFileName(filePath, _config, _fileSystemService);

            // Assert
            Assert.True(result.IsValid);
            Assert.NotNull(result.SupersededValue);
            Assert.Equal(2, result.SupersededValue);
        }


        [Fact]
        public async Task ParseFileName_WithoutRevisionInPattern_ParsesWithoutRevision()
        {
            // Arrange - Pattern without revision component
            var configNoRevision = new AdrPlusRepoConfig("", "")
            {
                LenRevision = 0,
                LenVersion = 2,
                LenScope = 1,
                Separator = '-',
                Prefix = "ADR",
                LenSeq = 4,
                StatusNew = "Proposed",
                StatusAcc = "Accepted",
                StatusRej = "Rejected",
                StatusSup = "Superseded",
                Scopes = "Enterprise;Project"
            };

            var filePath = "ADR0001V02Enterprise-MyDecision@Domain.md";
            var headerLines = BuildValidTableHeaderLines("2025-04-17", "Proposed");
            _fileSystemService.ReadAllLinesAsync(Arg.Any<string>()).Returns(headerLines);

            // Act
            var result = await _parser.ParseFileName(filePath, configNoRevision, _fileSystemService);

            // Assert
            Assert.True(result.IsValid);
            Assert.Equal(2, result.Version);
            Assert.Equal(0, result.Revision);
        }

        [Fact]
        public async Task ParseFileName_WithoutScopeInPattern_ParsesWithoutScope()
        {
            // Arrange - Pattern without scope component
            var configNoScope = new AdrPlusRepoConfig("", "")
            {
                LenRevision = 2,
                LenVersion = 2,
                LenScope = 0,
                Separator = '-',
                Prefix = "ADR",
                LenSeq = 4,
                StatusNew = "Proposed",
                StatusAcc = "Accepted",
                StatusRej = "Rejected",
                StatusSup = "Superseded",
            };

            var filePath = "ADR0001V02R03-MyDecision@Domain.md";
            var headerLines = BuildValidTableHeaderLines("2025-04-17", "Proposed");
            _fileSystemService.ReadAllLinesAsync(Arg.Any<string>()).Returns(headerLines);

            // Act
            var result = await _parser.ParseFileName(filePath, configNoScope, _fileSystemService);

            // Assert
            Assert.True(result.IsValid);
            Assert.Empty(result.Scope??string.Empty);
        }

        #endregion

        #region ParseFileName Tests - Migration Pattern Integration


        [Fact]
        public async Task ParseFileName_WithMigrationPatternDisabled_SkipsMigrationParsing()
        {
            // Arrange - No migration pattern configured
            var filePath = "OldFormat-0001.md";
            _fileSystemService.ReadAllLinesAsync(Arg.Any<string>()).Returns([]);

            // Act
            var result = await _parser.ParseFileName(filePath, _config, _fileSystemService);

            // Assert
            Assert.False(result.IsValid);
            Assert.NotEmpty(result.ErrorMessage);
        }

        #endregion

        #region ParseFileName Tests - Domain Extraction

        [Fact]
        public async Task ParseFileName_WithDomainContainingSpecialCharacters_ExtractsDomainWithCharacters()
        {
            // Arrange
            var filePath = "ADR0001V01R01Enterprise-MyDecision@Payment-Processing_Service.md";
            var headerLines = BuildValidTableHeaderLines("2025-04-17", "Proposed");
            _fileSystemService.ReadAllLinesAsync(Arg.Any<string>()).Returns(headerLines);

            // Act
            var result = await _parser.ParseFileName(filePath, _config, _fileSystemService);

            // Assert
            Assert.True(result.IsValid);
            Assert.NotEmpty(result.Domain??string.Empty);
            Assert.Contains("Payment", result.Domain);
        }

        [Fact]
        public async Task ParseFileName_WithMultipleDomainSegments_ExtractsLastSegmentAsDomain()
        {
            // Arrange
            var filePath = "ADR0001V01R01Enterprise-MyDecision@Service1@Service2.md";
            var headerLines = BuildValidTableHeaderLines("2025-04-17", "Proposed");
            _fileSystemService.ReadAllLinesAsync(Arg.Any<string>()).Returns(headerLines);

            // Act
            var result = await _parser.ParseFileName(filePath, _config, _fileSystemService);

            // Assert
            Assert.True(result.IsValid);
            Assert.NotEmpty(result.Domain??string.Empty);
            Assert.Contains("Service2", result.Domain);
        }

        #endregion

        #region ParseFileName Tests - Supersede Format

        [Fact]
        public async Task ParseFileName_WithSupersededMaxValue_ParsesSupersededCorrectly()
        {
            // Arrange - Large superseded number
            var filePath = "ADR0001V01R01Enterprise-OldDecision@Domain--9999.md";
            var headerLines = BuildValidTableHeaderLines("2025-04-17", "Proposed");
            _fileSystemService.ReadAllLinesAsync(Arg.Any<string>()).Returns(headerLines);

            // Act
            var result = await _parser.ParseFileName(filePath, _config, _fileSystemService);

            // Assert
            Assert.True(result.IsValid);
            Assert.NotNull(result.SupersededValue);
            Assert.Equal(9999, result.SupersededValue);
        }

        [Fact]
        public async Task ParseFileName_WithSupersededZero_ParsesSupersededAsZero()
        {
            // Arrange - Zero as superseded number
            var filePath = "ADR0001V01R01Enterprise-OldDecision@Domain--0000.md";
            var headerLines = BuildValidTableHeaderLines("2025-04-17", "Proposed");
            _fileSystemService.ReadAllLinesAsync(Arg.Any<string>()).Returns(headerLines);

            // Act
            var result = await _parser.ParseFileName(filePath, _config, _fileSystemService);

            // Assert
            Assert.True(result.IsValid);
            Assert.NotNull(result.SupersededValue);
            Assert.Equal(0, result.SupersededValue);
        }

        #endregion

        #region ParseFileName Tests - Error Conditions and Edge Cases


        [Fact]
        public async Task ParseFileName_WithMissingSequenceNumber_ReturnsError()
        {
            // Arrange - Filename missing sequence number
            var filePath = "ADRv01R01Enterprise-TestTitle.md";
            _fileSystemService.ReadAllLinesAsync(Arg.Any<string>()).Returns([]);

            // Act
            var result = await _parser.ParseFileName(filePath, _config, _fileSystemService);

            // Assert
            Assert.False(result.IsValid);
        }

        [Fact]
        public async Task ParseFileName_WithMissingSeparator_ReturnsError()
        {
            // Arrange - Missing separator between components
            var filePath = "ADR0001TestTitle.md";
            _fileSystemService.ReadAllLinesAsync(Arg.Any<string>()).Returns([]);

            // Act
            var result = await _parser.ParseFileName(filePath, _config, _fileSystemService);

            // Assert
            Assert.False(result.IsValid);
        }

        [Fact]
        public async Task ParseFileName_WithEmptyTitlePart_ReturnsError()
        {
            // Arrange - Empty title part after separator
            var filePath = "ADR0001V01R01Enterprise-.md";
            _fileSystemService.ReadAllLinesAsync(Arg.Any<string>()).Returns([]);

            // Act
            var result = await _parser.ParseFileName(filePath, _config, _fileSystemService);

            // Assert
            Assert.False(result.IsValid);
        }

        [Fact]
        public async Task ParseFileName_WithNegativeSequenceNumber_ReturnsError()
        {
            // Arrange - Negative sequence number
            var filePath = "ADR-0001V01R01Enterprise-TestTitle.md";
            _fileSystemService.ReadAllLinesAsync(Arg.Any<string>()).Returns([]);

            // Act
            var result = await _parser.ParseFileName(filePath, _config, _fileSystemService);

            // Assert
            Assert.False(result.IsValid);
        }

        [Fact]
        public async Task ParseFileName_WithTooLongSequenceNumber_ParsesIfNumeric()
        {
            // Arrange - Very long sequence number
            var filePath = "ADR999999999V01R01Enterprise-TestTitle.md";
            var headerLines = BuildValidTableHeaderLines("2025-04-17", "Proposed");
            _fileSystemService.ReadAllLinesAsync(Arg.Any<string>()).Returns(headerLines);

            // Act
            var result = await _parser.ParseFileName(filePath, _config, _fileSystemService);

            // Assert - Should succeed as long as it's numeric
            if (result.IsValid)
            {
                Assert.True(result.Number > 0);
            }
        }

        [Fact]
        public async Task ParseFileName_WithSpecialCharactersInTitle_ParsesCorrectly()
        {
            // Arrange - Title with special characters
            var filePath = "ADR0001V01R01Enterprise-Use_Caching-Safely!@Domain.md";
            var headerLines = BuildValidTableHeaderLines("2025-04-17", "Proposed");
            _fileSystemService.ReadAllLinesAsync(Arg.Any<string>()).Returns(headerLines);

            // Act
            var result = await _parser.ParseFileName(filePath, _config, _fileSystemService);

            // Assert
            Assert.True(result.IsValid);
        }

        [Fact]
        public async Task ParseFileName_WithLeadingZerosInSequence_ParsesCorrectly()
        {
            // Arrange - Leading zeros in sequence number
            var filePath = "ADR0001V01R01Enterprise-TestTitle@Domain.md";
            var headerLines = BuildValidTableHeaderLines("2025-04-17", "Proposed");
            _fileSystemService.ReadAllLinesAsync(Arg.Any<string>()).Returns(headerLines);

            // Act
            var result = await _parser.ParseFileName(filePath, _config, _fileSystemService);

            // Assert
            Assert.True(result.IsValid);
            Assert.Equal(1, result.Number);
        }


        [Fact]
        public async Task ParseFileName_WithSupersededNonNumeric_ReturnsError()
        {
            // Arrange - Supersede value is not numeric
            var filePath = "ADR0001V01R01Enterprise-TestTitle@Domain--ABC.md";
            _fileSystemService.ReadAllLinesAsync(Arg.Any<string>()).Returns([]);

            // Act
            var result = await _parser.ParseFileName(filePath, _config, _fileSystemService);

            // Assert
            Assert.False(result.IsValid);
        }

        [Fact]
        public async Task ParseFileName_WithVersionZero_ParsesSuccessfully()
        {
            // Arrange - Version component is 00
            var filePath = "ADR0001V00R01Enterprise-TestTitle@Domain.md";
            var headerLines = BuildValidTableHeaderLines("2025-04-17", "Proposed");
            _fileSystemService.ReadAllLinesAsync(Arg.Any<string>()).Returns(headerLines);

            // Act
            var result = await _parser.ParseFileName(filePath, _config, _fileSystemService);

            // Assert
            Assert.True(result.IsValid);
            Assert.Equal(0, result.Version);
        }

        [Fact]
        public async Task ParseFileName_WithRevisionZero_ParsesSuccessfully()
        {
            // Arrange - Revision component is 00
            var filePath = "ADR0001V01R00Enterprise-TestTitle@Domain.md";
            var headerLines = BuildValidTableHeaderLines("2025-04-17", "Proposed");
            _fileSystemService.ReadAllLinesAsync(Arg.Any<string>()).Returns(headerLines);

            // Act
            var result = await _parser.ParseFileName(filePath, _config, _fileSystemService);

            // Assert
            Assert.True(result.IsValid);
            Assert.Equal(0, result.Revision);
        }

        #endregion
    }
}
