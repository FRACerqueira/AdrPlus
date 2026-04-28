// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the AdrPlus project under MIT license
// ***************************************************************************************

using AdrPlus.Core;
using AdrPlus.Domain;
using AdrPlus.Infrastructure.FileSystem;
using AdrPlus.Tests.Helpers;

namespace AdrPlus.Tests.Commands;

public class AdrStatusServiceTests
{
    private readonly IFileSystemService _fileSystemService = Substitute.For<IFileSystemService>();
    private readonly IAdrFileParser _fileParser = Substitute.For<IAdrFileParser>();
    private readonly AdrStatusService _statusService;
    private readonly string _filePath = PathHelper.GetAdrFilePath("ADR-0001-TestDecision-V01.md");
    private readonly AdrPlusRepoConfig _config;

    public AdrStatusServiceTests()
    {
        _statusService = new AdrStatusService(_fileParser);
        _config = new AdrPlusRepoConfig("# {0}","doc/adr")
        {
            Prefix = "ADR",
            LenSeq = 4,
            LenVersion = 2,
            LenRevision = 0,
            Separator = '-',
            CaseTransform = CaseFormat.PascalCase,
            StatusNew = "Proposed",
            StatusAcc = "Accepted",
            StatusRej = "Rejected",
            StatusSup = "Superseded",
            HeaderDisclaimer = "Architecture Decision Record",
            HeaderVersion = "Version",
            HeaderRevision = "Revision",
            HeaderStatus = "Status"
        };
    }

    #region Helpers

    private static AdrFileNameComponents CreateValidParsedFile(int number = 1, int version = 1)
    {
        return new AdrFileNameComponents
        {
            IsValid = true,
            ErrorMessage = string.Empty,
            Number = number,
            Header = new AdrHeader
            {
                IsValid = true,
                ErrorMessage = string.Empty,
                Title = "Test Decision",
                Version = version,
                Revision = 0,
                Domain = "TestDomain",
                Scope = "TestScope",
                StatusCreate = AdrStatus.Proposed,
                DateCreate = new DateTime(2024, 1, 1),
                StatusUpdate = AdrStatus.Unknown,
                DateUpdate = null,
                StatusChange = AdrStatus.Unknown,
                DateChange = null
            },
            ContentAdr = "# Test Decision\n\nThis is a test ADR content."
        };
    }

    private static AdrFileNameComponents CreateInvalidParsedFile()
    {
        return new AdrFileNameComponents
        {
            IsValid = false,
            ErrorMessage = "Invalid file format",
            Header = new AdrHeader { IsValid = true, ErrorMessage = string.Empty }
        };
    }

    private static AdrFileNameComponents CreateParsedFileWithInvalidHeader()
    {
        return new AdrFileNameComponents
        {
            IsValid = true,
            ErrorMessage = string.Empty,
            Number = 1,
            Header = new AdrHeader
            {
                IsValid = false,
                ErrorMessage = "Invalid header format"
            }
        };
    }

    #endregion

    #region StatusUpdateAdrAsync Tests

    [Fact]
    public async Task StatusUpdateAdrAsync_WithValidFileAndStatus_ReturnsSuccessAndWritesFile()
    {
        // Arrange
        var parsedFile = AdrStatusServiceTests.CreateValidParsedFile();
        var newStatus = AdrStatus.Accepted;
        var updateDate = new DateTime(2024, 6, 15);
        var cancellationToken = CancellationToken.None;

        _fileParser.ParseFileName(_filePath, _config, _fileSystemService)
            .Returns(Task.FromResult(parsedFile));

        // Act
        var (isValid, error) = await _statusService.StatusUpdateAdrAsync(
            _filePath, newStatus, updateDate, _config, _fileSystemService, cancellationToken);

        // Assert
        isValid.Should().BeTrue();
        error.Should().BeEmpty();
        await _fileSystemService.Received(1).WriteAllTextAsync(
            Arg.Is(_filePath),
            Arg.Any<string>(),
            Arg.Is(cancellationToken));
    }

    [Fact]
    public async Task StatusUpdateAdrAsync_WithValidFile_UpdatesHeaderStatusUpdate()
    {
        // Arrange
        var parsedFile = AdrStatusServiceTests.CreateValidParsedFile();
        var newStatus = AdrStatus.Rejected;
        var updateDate = new DateTime(2024, 3, 10);
        string capturedContent = string.Empty;

        _fileParser.ParseFileName(_filePath, _config, _fileSystemService)
            .Returns(Task.FromResult(parsedFile));

        _fileSystemService.WriteAllTextAsync(
                Arg.Any<string>(),
                Arg.Do<string>(content => capturedContent = content),
                Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        // Act
        await _statusService.StatusUpdateAdrAsync(
            _filePath, newStatus, updateDate, _config, _fileSystemService, CancellationToken.None);

        // Assert
        capturedContent.Should().Contain(parsedFile.ContentAdr);
        parsedFile.Header.StatusUpdate.Should().Be(newStatus);
        parsedFile.Header.DateUpdate.Should().Be(updateDate);
    }

    [Fact]
    public async Task StatusUpdateAdrAsync_WithNullFullpath_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => _statusService.StatusUpdateAdrAsync(
            null!, AdrStatus.Accepted, DateTime.Now, _config, _fileSystemService, CancellationToken.None));
    }

    [Fact]
    public async Task StatusUpdateAdrAsync_WithEmptyFullpath_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _statusService.StatusUpdateAdrAsync(
            string.Empty, AdrStatus.Accepted, DateTime.Now, _config, _fileSystemService, CancellationToken.None));
    }

    [Fact]
    public async Task StatusUpdateAdrAsync_WithWhitespaceFullpath_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _statusService.StatusUpdateAdrAsync(
            "   ", AdrStatus.Accepted, DateTime.Now, _config, _fileSystemService, CancellationToken.None));
    }

    [Fact]
    public async Task StatusUpdateAdrAsync_WithNullConfig_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => _statusService.StatusUpdateAdrAsync(
            _filePath, AdrStatus.Accepted, DateTime.Now, null!, _fileSystemService, CancellationToken.None));
    }

    [Fact]
    public async Task StatusUpdateAdrAsync_WithNullFileSystemService_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => _statusService.StatusUpdateAdrAsync(
            _filePath, AdrStatus.Accepted, DateTime.Now, _config, null!, CancellationToken.None));
    }

    [Fact]
    public async Task StatusUpdateAdrAsync_WhenParsingFails_ReturnsFalseWithErrorMessage()
    {
        // Arrange
        var invalidFile = CreateInvalidParsedFile();
        var cancellationToken = CancellationToken.None;

        _fileParser.ParseFileName(_filePath, _config, _fileSystemService)
            .Returns(Task.FromResult(invalidFile));

        // Act
        var (isValid, error) = await _statusService.StatusUpdateAdrAsync(
            _filePath, AdrStatus.Accepted, DateTime.Now, _config, _fileSystemService, cancellationToken);

        // Assert
        isValid.Should().BeFalse();
        error.Should().Be(invalidFile.ErrorMessage);
    }

    [Fact]
    public async Task StatusUpdateAdrAsync_WhenHeaderIsInvalid_ReturnsFalseWithHeaderError()
    {
        // Arrange
        var invalidHeaderFile = CreateParsedFileWithInvalidHeader();
        var cancellationToken = CancellationToken.None;

        _fileParser.ParseFileName(_filePath, _config, _fileSystemService)
            .Returns(Task.FromResult(invalidHeaderFile));

        // Act
        var (isValid, error) = await _statusService.StatusUpdateAdrAsync(
            _filePath, AdrStatus.Accepted, DateTime.Now, _config, _fileSystemService, cancellationToken);

        // Assert
        isValid.Should().BeFalse();
        error.Should().Be(invalidHeaderFile.Header.ErrorMessage);
    }

    [Theory]
    [InlineData(0)] // Proposed
    [InlineData(1)] // Accepted
    [InlineData(2)] // Rejected
    [InlineData(3)] // Superseded
    public async Task StatusUpdateAdrAsync_WithDifferentStatuses_UpdatesCorrectly(int statusValue)
    {
        // Arrange
        var parsedFile = AdrStatusServiceTests.CreateValidParsedFile();
        var newStatus = (AdrStatus)statusValue;
        var updateDate = new DateTime(2024, 7, 20);

        _fileParser.ParseFileName(_filePath, _config, _fileSystemService)
            .Returns(Task.FromResult(parsedFile));

        // Act
        var (isValid, error) = await _statusService.StatusUpdateAdrAsync(
            _filePath, newStatus, updateDate, _config, _fileSystemService, CancellationToken.None);

        // Assert
        isValid.Should().BeTrue();
        error.Should().BeEmpty();
        parsedFile.Header.StatusUpdate.Should().Be(newStatus);
    }

    [Fact]
    public async Task StatusUpdateAdrAsync_WithCrossPlatformPath_ExecutesSuccessfully()
    {
        // Arrange
        var crossPlatformPath = PathHelper.GetAdrFilePath("ADR-0002-CrossPlatform-V01.md");
        var parsedFile = AdrStatusServiceTests.CreateValidParsedFile(number: 2);

        _fileParser.ParseFileName(crossPlatformPath, _config, _fileSystemService)
            .Returns(Task.FromResult(parsedFile));

        // Act
        var (isValid, _) = await _statusService.StatusUpdateAdrAsync(
            crossPlatformPath, AdrStatus.Accepted, DateTime.Now, _config, _fileSystemService, CancellationToken.None);

        // Assert
        isValid.Should().BeTrue();
    }

    [Fact]
    public async Task StatusUpdateAdrAsync_WithCancellationToken_PassesTokenToFileSystem()
    {
        // Arrange
        var parsedFile = AdrStatusServiceTests.CreateValidParsedFile();
        var cts = new CancellationTokenSource();
        var cancellationToken = cts.Token;

        _fileParser.ParseFileName(_filePath, _config, _fileSystemService)
            .Returns(Task.FromResult(parsedFile));

        // Act
        await _statusService.StatusUpdateAdrAsync(
            _filePath, AdrStatus.Accepted, DateTime.Now, _config, _fileSystemService, cancellationToken);

        // Assert
        await _fileSystemService.Received(1).WriteAllTextAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Is(cancellationToken));
    }

    #endregion

    #region StatusChangeSupersedeAdrAsync Tests

    [Fact]
    public async Task StatusChangeSupersedeAdrAsync_WithValidFileAndFilename_ReturnsSuccessAndWritesFile()
    {
        // Arrange
        var parsedFile = AdrStatusServiceTests.CreateValidParsedFile();
        var supersedingFilename = "ADR-0002-NewDecision-V01.md";
        var changeDate = new DateTime(2024, 8, 1);
        var cancellationToken = CancellationToken.None;

        _fileParser.ParseFileName(_filePath, _config, _fileSystemService)
            .Returns(Task.FromResult(parsedFile));

        // Act
        var (isValid, error) = await _statusService.StatusChangeSupersedeAdrAsync(
            _filePath, supersedingFilename, changeDate, _config, _fileSystemService, cancellationToken);

        // Assert
        isValid.Should().BeTrue();
        error.Should().BeEmpty();
        await _fileSystemService.Received(1).WriteAllTextAsync(
            Arg.Is(_filePath),
            Arg.Any<string>(),
            Arg.Is(cancellationToken));
    }

    [Fact]
    public async Task StatusChangeSupersedeAdrAsync_WithValidFile_UpdatesSupersededStatus()
    {
        // Arrange
        var parsedFile = AdrStatusServiceTests.CreateValidParsedFile();
        var supersedingFilename = "ADR-0002-NewDecision-V01.md";
        var changeDate = new DateTime(2024, 8, 5);
        string capturedContent = string.Empty;

        _fileParser.ParseFileName(_filePath, _config, _fileSystemService)
            .Returns(Task.FromResult(parsedFile));

        _fileSystemService.WriteAllTextAsync(
                Arg.Any<string>(),
                Arg.Do<string>(content => capturedContent = content),
                Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        // Act
        await _statusService.StatusChangeSupersedeAdrAsync(
            _filePath, supersedingFilename, changeDate, _config, _fileSystemService, CancellationToken.None);

        // Assert
        capturedContent.Should().Contain(parsedFile.ContentAdr);
        parsedFile.Header.StatusChange.Should().Be(AdrStatus.Superseded);
        parsedFile.Header.DateChange.Should().Be(changeDate);
    }

    [Fact]
    public async Task StatusChangeSupersedeAdrAsync_WithNullFullpath_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => _statusService.StatusChangeSupersedeAdrAsync(
            null!, "ADR-0002.md", DateTime.Now, _config, _fileSystemService, CancellationToken.None));
    }

    [Fact]
    public async Task StatusChangeSupersedeAdrAsync_WithEmptyFullpath_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _statusService.StatusChangeSupersedeAdrAsync(
            string.Empty, "ADR-0002.md", DateTime.Now, _config, _fileSystemService, CancellationToken.None));
    }

    [Fact]
    public async Task StatusChangeSupersedeAdrAsync_WithWhitespaceFullpath_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _statusService.StatusChangeSupersedeAdrAsync(
            "   ", "ADR-0002.md", DateTime.Now, _config, _fileSystemService, CancellationToken.None));
    }

    [Fact]
    public async Task StatusChangeSupersedeAdrAsync_WithNullFilename_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => _statusService.StatusChangeSupersedeAdrAsync(
            _filePath, null!, DateTime.Now, _config, _fileSystemService, CancellationToken.None));
    }

    [Fact]
    public async Task StatusChangeSupersedeAdrAsync_WithEmptyFilename_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _statusService.StatusChangeSupersedeAdrAsync(
            _filePath, string.Empty, DateTime.Now, _config, _fileSystemService, CancellationToken.None));
    }

    [Fact]
    public async Task StatusChangeSupersedeAdrAsync_WithWhitespaceFilename_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _statusService.StatusChangeSupersedeAdrAsync(
            _filePath, "   ", DateTime.Now, _config, _fileSystemService, CancellationToken.None));
    }

    [Fact]
    public async Task StatusChangeSupersedeAdrAsync_WithNullConfig_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => _statusService.StatusChangeSupersedeAdrAsync(
            _filePath, "ADR-0002.md", DateTime.Now, null!, _fileSystemService, CancellationToken.None));
    }

    [Fact]
    public async Task StatusChangeSupersedeAdrAsync_WithNullFileSystemService_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => _statusService.StatusChangeSupersedeAdrAsync(
            _filePath, "ADR-0002.md", DateTime.Now, _config, null!, CancellationToken.None));
    }

    [Fact]
    public async Task StatusChangeSupersedeAdrAsync_WhenParsingFails_ReturnsFalseWithErrorMessage()
    {
        // Arrange
        var invalidFile = CreateInvalidParsedFile();
        var cancellationToken = CancellationToken.None;

        _fileParser.ParseFileName(_filePath, _config, _fileSystemService)
            .Returns(Task.FromResult(invalidFile));

        // Act
        var (isValid, error) = await _statusService.StatusChangeSupersedeAdrAsync(
            _filePath, "ADR-0002.md", DateTime.Now, _config, _fileSystemService, cancellationToken);

        // Assert
        isValid.Should().BeFalse();
        error.Should().Be(invalidFile.ErrorMessage);
    }

    [Fact]
    public async Task StatusChangeSupersedeAdrAsync_WhenHeaderIsInvalid_ReturnsFalseWithHeaderError()
    {
        // Arrange
        var invalidHeaderFile = CreateParsedFileWithInvalidHeader();
        var cancellationToken = CancellationToken.None;

        _fileParser.ParseFileName(_filePath, _config, _fileSystemService)
            .Returns(Task.FromResult(invalidHeaderFile));

        // Act
        var (isValid, error) = await _statusService.StatusChangeSupersedeAdrAsync(
            _filePath, "ADR-0002.md", DateTime.Now, _config, _fileSystemService, cancellationToken);

        // Assert
        isValid.Should().BeFalse();
        error.Should().Be(invalidHeaderFile.Header.ErrorMessage);
    }

    [Fact]
    public async Task StatusChangeSupersedeAdrAsync_WithDifferentFilenames_UpdatesCorrectly()
    {
        // Arrange
        var parsedFile = AdrStatusServiceTests.CreateValidParsedFile();
        var filename1 = "ADR-0002-NewDecision-V01.md";
        var filename2 = "ADR-0003-AnotherDecision-V02.md";

        _fileParser.ParseFileName(_filePath, _config, _fileSystemService)
            .Returns(Task.FromResult(parsedFile));

        // Act
        var (isValid1, _) = await _statusService.StatusChangeSupersedeAdrAsync(
            _filePath, filename1, DateTime.Now, _config, _fileSystemService, CancellationToken.None);

        parsedFile = AdrStatusServiceTests.CreateValidParsedFile();
        _fileParser.ParseFileName(_filePath, _config, _fileSystemService)
            .Returns(Task.FromResult(parsedFile));

        var (isValid2, _) = await _statusService.StatusChangeSupersedeAdrAsync(
            _filePath, filename2, DateTime.Now, _config, _fileSystemService, CancellationToken.None);

        // Assert
        isValid1.Should().BeTrue();
        isValid2.Should().BeTrue();
    }

    [Fact]
    public async Task StatusChangeSupersedeAdrAsync_WithCrossPlatformPath_ExecutesSuccessfully()
    {
        // Arrange
        var crossPlatformPath = PathHelper.GetAdrFilePath("ADR-0003-CrossPlatformSupersede-V01.md");
        var parsedFile = AdrStatusServiceTests.CreateValidParsedFile(number: 3);

        _fileParser.ParseFileName(crossPlatformPath, _config, _fileSystemService)
            .Returns(Task.FromResult(parsedFile));

        // Act
        var (isValid, _) = await _statusService.StatusChangeSupersedeAdrAsync(
            crossPlatformPath, "ADR-0004-NewDecision-V01.md", DateTime.Now, _config, _fileSystemService, CancellationToken.None);

        // Assert
        isValid.Should().BeTrue();
    }

    [Fact]
    public async Task StatusChangeSupersedeAdrAsync_WithCancellationToken_PassesTokenToFileSystem()
    {
        // Arrange
        var parsedFile = AdrStatusServiceTests.CreateValidParsedFile();
        var cts = new CancellationTokenSource();
        var cancellationToken = cts.Token;

        _fileParser.ParseFileName(_filePath, _config, _fileSystemService)
            .Returns(Task.FromResult(parsedFile));

        // Act
        await _statusService.StatusChangeSupersedeAdrAsync(
            _filePath, "ADR-0002.md", DateTime.Now, _config, _fileSystemService, cancellationToken);

        // Assert
        await _fileSystemService.Received(1).WriteAllTextAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Is(cancellationToken));
    }

    #endregion

    #region StatusChangeAdrAsync Tests

    [Fact]
    public async Task StatusChangeAdrAsync_WithValidFileAndStatus_ReturnsSuccessAndWritesFile()
    {
        // Arrange
        var parsedFile = AdrStatusServiceTests.CreateValidParsedFile();
        var newStatus = AdrStatus.Accepted;
        var changeDate = new DateTime(2024, 9, 10);
        var cancellationToken = CancellationToken.None;

        _fileParser.ParseFileName(_filePath, _config, _fileSystemService)
            .Returns(Task.FromResult(parsedFile));

        // Act
        var (isValid, error) = await _statusService.StatusChangeAdrAsync(
            _filePath, newStatus, changeDate, _config, _fileSystemService, cancellationToken);

        // Assert
        isValid.Should().BeTrue();
        error.Should().BeEmpty();
        await _fileSystemService.Received(1).WriteAllTextAsync(
            Arg.Is(_filePath),
            Arg.Any<string>(),
            Arg.Is(cancellationToken));
    }

    [Fact]
    public async Task StatusChangeAdrAsync_WithValidFile_UpdatesHeaderStatusChange()
    {
        // Arrange
        var parsedFile = AdrStatusServiceTests.CreateValidParsedFile();
        var newStatus = AdrStatus.Rejected;
        var changeDate = new DateTime(2024, 9, 15);
        string capturedContent = string.Empty;

        _fileParser.ParseFileName(_filePath, _config, _fileSystemService)
            .Returns(Task.FromResult(parsedFile));

        _fileSystemService.WriteAllTextAsync(
                Arg.Any<string>(),
                Arg.Do<string>(content => capturedContent = content),
                Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        // Act
        await _statusService.StatusChangeAdrAsync(
            _filePath, newStatus, changeDate, _config, _fileSystemService, CancellationToken.None);

        // Assert
        capturedContent.Should().Contain(parsedFile.ContentAdr);
        parsedFile.Header.StatusChange.Should().Be(newStatus);
        parsedFile.Header.DateChange.Should().Be(changeDate);
    }

    [Fact]
    public async Task StatusChangeAdrAsync_WithNullFullpath_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => _statusService.StatusChangeAdrAsync(
            null!, AdrStatus.Accepted, DateTime.Now, _config, _fileSystemService, CancellationToken.None));
    }

    [Fact]
    public async Task StatusChangeAdrAsync_WithEmptyFullpath_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _statusService.StatusChangeAdrAsync(
            string.Empty, AdrStatus.Accepted, DateTime.Now, _config, _fileSystemService, CancellationToken.None));
    }

    [Fact]
    public async Task StatusChangeAdrAsync_WithWhitespaceFullpath_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _statusService.StatusChangeAdrAsync(
            "   ", AdrStatus.Accepted, DateTime.Now, _config, _fileSystemService, CancellationToken.None));
    }

    [Fact]
    public async Task StatusChangeAdrAsync_WithNullConfig_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => _statusService.StatusChangeAdrAsync(
            _filePath, AdrStatus.Accepted, DateTime.Now, null!, _fileSystemService, CancellationToken.None));
    }

    [Fact]
    public async Task StatusChangeAdrAsync_WithNullFileSystemService_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => _statusService.StatusChangeAdrAsync(
            _filePath, AdrStatus.Accepted, DateTime.Now, _config, null!, CancellationToken.None));
    }

    [Fact]
    public async Task StatusChangeAdrAsync_WhenParsingFails_ReturnsFalseWithErrorMessage()
    {
        // Arrange
        var invalidFile = CreateInvalidParsedFile();
        var cancellationToken = CancellationToken.None;

        _fileParser.ParseFileName(_filePath, _config, _fileSystemService)
            .Returns(Task.FromResult(invalidFile));

        // Act
        var (isValid, error) = await _statusService.StatusChangeAdrAsync(
            _filePath, AdrStatus.Accepted, DateTime.Now, _config, _fileSystemService, cancellationToken);

        // Assert
        isValid.Should().BeFalse();
        error.Should().Be(invalidFile.ErrorMessage);
    }

    [Fact]
    public async Task StatusChangeAdrAsync_WhenHeaderIsInvalid_ReturnsFalseWithHeaderError()
    {
        // Arrange
        var invalidHeaderFile = CreateParsedFileWithInvalidHeader();
        var cancellationToken = CancellationToken.None;

        _fileParser.ParseFileName(_filePath, _config, _fileSystemService)
            .Returns(Task.FromResult(invalidHeaderFile));

        // Act
        var (isValid, error) = await _statusService.StatusChangeAdrAsync(
            _filePath, AdrStatus.Accepted, DateTime.Now, _config, _fileSystemService, cancellationToken);

        // Assert
        isValid.Should().BeFalse();
        error.Should().Be(invalidHeaderFile.Header.ErrorMessage);
    }

    [Theory]
    [InlineData(0)] // Proposed
    [InlineData(1)] // Accepted
    [InlineData(2)] // Rejected
    [InlineData(3)] // Superseded
    public async Task StatusChangeAdrAsync_WithDifferentStatuses_UpdatesCorrectly(int statusValue)
    {
        // Arrange
        var parsedFile = AdrStatusServiceTests.CreateValidParsedFile();
        var newStatus = (AdrStatus)statusValue;
        var changeDate = new DateTime(2024, 10, 1);

        _fileParser.ParseFileName(_filePath, _config, _fileSystemService)
            .Returns(Task.FromResult(parsedFile));

        // Act
        var (isValid, error) = await _statusService.StatusChangeAdrAsync(
            _filePath, newStatus, changeDate, _config, _fileSystemService, CancellationToken.None);

        // Assert
        isValid.Should().BeTrue();
        error.Should().BeEmpty();
        parsedFile.Header.StatusChange.Should().Be(newStatus);
    }

    [Fact]
    public async Task StatusChangeAdrAsync_WithMultipleDateChanges_UpdatesCorrectly()
    {
        // Arrange
        var parsedFile = AdrStatusServiceTests.CreateValidParsedFile();
        var date1 = new DateTime(2024, 5, 1);
        var date2 = new DateTime(2024, 10, 15);

        _fileParser.ParseFileName(_filePath, _config, _fileSystemService)
            .Returns(Task.FromResult(parsedFile));

        // Act
        var (isValid1, _) = await _statusService.StatusChangeAdrAsync(
            _filePath, AdrStatus.Accepted, date1, _config, _fileSystemService, CancellationToken.None);

        parsedFile = AdrStatusServiceTests.CreateValidParsedFile();
        _fileParser.ParseFileName(_filePath, _config, _fileSystemService)
            .Returns(Task.FromResult(parsedFile));

        var (isValid2, _) = await _statusService.StatusChangeAdrAsync(
            _filePath, AdrStatus.Rejected, date2, _config, _fileSystemService, CancellationToken.None);

        // Assert
        isValid1.Should().BeTrue();
        isValid2.Should().BeTrue();
    }

    [Fact]
    public async Task StatusChangeAdrAsync_WithCrossPlatformPath_ExecutesSuccessfully()
    {
        // Arrange
        var crossPlatformPath = PathHelper.GetAdrFilePath("ADR-0005-CrossPlatformChange-V01.md");
        var parsedFile = AdrStatusServiceTests.CreateValidParsedFile(number: 5);

        _fileParser.ParseFileName(crossPlatformPath, _config, _fileSystemService)
            .Returns(Task.FromResult(parsedFile));

        // Act
        var (isValid, _) = await _statusService.StatusChangeAdrAsync(
            crossPlatformPath, AdrStatus.Accepted, DateTime.Now, _config, _fileSystemService, CancellationToken.None);

        // Assert
        isValid.Should().BeTrue();
    }

    [Fact]
    public async Task StatusChangeAdrAsync_WithCancellationToken_PassesTokenToFileSystem()
    {
        // Arrange
        var parsedFile = AdrStatusServiceTests.CreateValidParsedFile();
        var cts = new CancellationTokenSource();
        var cancellationToken = cts.Token;

        _fileParser.ParseFileName(_filePath, _config, _fileSystemService)
            .Returns(Task.FromResult(parsedFile));

        // Act
        await _statusService.StatusChangeAdrAsync(
            _filePath, AdrStatus.Accepted, DateTime.Now, _config, _fileSystemService, cancellationToken);

        // Assert
        await _fileSystemService.Received(1).WriteAllTextAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Is(cancellationToken));
    }

    [Fact]
    public async Task StatusChangeAdrAsync_WithSupersededStatus_HandlesCorrectly()
    {
        // Arrange
        var parsedFile = AdrStatusServiceTests.CreateValidParsedFile();
        var changeDate = new DateTime(2024, 11, 1);

        _fileParser.ParseFileName(_filePath, _config, _fileSystemService)
            .Returns(Task.FromResult(parsedFile));

        // Act
        var (isValid, error) = await _statusService.StatusChangeAdrAsync(
            _filePath, AdrStatus.Superseded, changeDate, _config, _fileSystemService, CancellationToken.None);

        // Assert
        isValid.Should().BeTrue();
        error.Should().BeEmpty();
        parsedFile.Header.StatusChange.Should().Be(AdrStatus.Superseded);
    }

    #endregion

    #region Cross-Method Integration Tests

    [Fact]
    public async Task MultipleStatusOperations_OnSameFile_AllSucceed()
    {
        // Arrange
        var parsedFile1 = AdrStatusServiceTests.CreateValidParsedFile();
        var parsedFile2 = AdrStatusServiceTests.CreateValidParsedFile();
        var parsedFile3 = AdrStatusServiceTests.CreateValidParsedFile();

        _fileParser.ParseFileName(_filePath, _config, _fileSystemService)
            .ReturnsForAnyArgs(x =>
            {
                return Task.FromResult(new AdrFileNameComponents
                {
                    IsValid = true,
                    ErrorMessage = string.Empty,
                    Number = 1,
                    Header = new AdrHeader
                    {
                        IsValid = true,
                        ErrorMessage = string.Empty,
                        Title = "Test",
                        Version = 1,
                        Domain = "TestDomain",
                        Scope = "TestScope",
                        StatusCreate = AdrStatus.Proposed,
                        DateCreate = new DateTime(2024, 1, 1)
                    },
                    ContentAdr = "# Test"
                });
            });

        // Act
        var (result1, error1) = await _statusService.StatusUpdateAdrAsync(
            _filePath, AdrStatus.Accepted, new DateTime(2024, 5, 1),
            _config, _fileSystemService, CancellationToken.None);

        var (result2, error2) = await _statusService.StatusChangeAdrAsync(
            _filePath, AdrStatus.Rejected, new DateTime(2024, 10, 1),
            _config, _fileSystemService, CancellationToken.None);

        var (result3, error3) = await _statusService.StatusChangeSupersedeAdrAsync(
            _filePath, "ADR-0002-NewDecision-V01.md", new DateTime(2024, 11, 1),
            _config, _fileSystemService, CancellationToken.None);

        // Assert
        result1.Should().BeTrue();
        error1.Should().BeEmpty();
        result2.Should().BeTrue();
        error2.Should().BeEmpty();
        result3.Should().BeTrue();
        error3.Should().BeEmpty();
    }

    [Fact]
    public async Task SequentialStatusUpdates_WithDifferentDates_AllSucceed()
    {
        // Arrange
        var date1 = new DateTime(2024, 1, 15);
        var date2 = new DateTime(2024, 6, 20);
        var date3 = new DateTime(2024, 12, 1);

        _fileParser.ParseFileName(_filePath, _config, _fileSystemService)
            .ReturnsForAnyArgs(x => Task.FromResult(AdrStatusServiceTests.CreateValidParsedFile()));

        // Act
        var (result1, _) = await _statusService.StatusUpdateAdrAsync(
            _filePath, AdrStatus.Proposed, date1, _config, _fileSystemService, CancellationToken.None);

        var (result2, _) = await _statusService.StatusUpdateAdrAsync(
            _filePath, AdrStatus.Accepted, date2, _config, _fileSystemService, CancellationToken.None);

        var (result3, _) = await _statusService.StatusUpdateAdrAsync(
            _filePath, AdrStatus.Rejected, date3, _config, _fileSystemService, CancellationToken.None);

        // Assert
        result1.Should().BeTrue();
        result2.Should().BeTrue();
        result3.Should().BeTrue();
    }

    #endregion
}
