// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the AdrPlus project under MIT license
// ***************************************************************************************

using AdrPlus.Core;
using AdrPlus.Domain;
using AdrPlus.Infrastructure.FileSystem;
using AdrPlus.Tests.Helpers;

namespace AdrPlus.Tests.Commands;

public class AdrQueryServiceTests
{
    private readonly IFileSystemService _fileSystemService = Substitute.For<IFileSystemService>();
    private readonly IAdrFileParser _fileParser = Substitute.For<IAdrFileParser>();
    private readonly AdrQueryService _queryService;
    private readonly string _directoryPath = PathHelper.GetRepositoryAdrPath();
    private readonly AdrPlusRepoConfig _config;

    public AdrQueryServiceTests()
    {
        _queryService = new AdrQueryService(_fileParser);
        _config = new AdrPlusRepoConfig("# {0}", _directoryPath)
        {
            Prefix = "ADR",
            LenSeq = 4,
            LenVersion = 2,
            LenRevision = 0,
            Separator = '-',
            CaseTransform = CaseFormat.PascalCase,
            StatusNew = "New",
            StatusAcc = "Accepted",
            StatusRej = "Rejected",
            StatusSup = "Superseded"
        };
    }

    #region ReadAllAdrByNumber Tests

    [Fact]
    public async Task ReadAllAdrByNumber_WithValidSequenceAndFiles_ReturnsMatchingAdrComponents()
    {
        // Arrange
        const int sequence = 1;
        const string fileName1 = "ADR-0001-Decision1.md";
        const string fileName2 = "ADR-0001-v02-Decision1.md";
        var filePath1 = PathHelper.GetAdrFilePath(fileName1);
        var filePath2 = PathHelper.GetAdrFilePath(fileName2);
        var searchPattern = $"{_config.Prefix}{sequence:D4}{_config.Separator}*.md";

        _fileSystemService.DirectoryExists(_directoryPath).Returns(true);
        _fileSystemService.GetFiles(_directoryPath, searchPattern).Returns([filePath1, filePath2]);

        var adrComponent1 = CreateAdrFileNameComponents(number: 1, version: 1, fileName: fileName1);
        var adrComponent2 = CreateAdrFileNameComponents(number: 1, version: 2, fileName: fileName2);

        _fileParser.ParseFileName(filePath1, _config, _fileSystemService).Returns(adrComponent1);
        _fileParser.ParseFileName(filePath2, _config, _fileSystemService).Returns(adrComponent2);

        // Act
        var result = await _queryService.ReadAllAdrByNumber(sequence, _fileSystemService, _directoryPath, _config);

        // Assert
        result.Should().HaveCount(2);
        result[0].Number.Should().Be(1);
        result[1].Number.Should().Be(1);
    }

    [Fact]
    public async Task ReadAllAdrByNumber_WithSequenceAndNoMatchingFiles_ReturnsEmptyArray()
    {
        // Arrange
        const int sequence = 99;
        var searchPattern = $"{_config.Prefix}{sequence:D4}{_config.Separator}*.md";

        _fileSystemService.DirectoryExists(_directoryPath).Returns(true);
        _fileSystemService.GetFiles(_directoryPath, searchPattern).Returns([]);

        // Act
        var result = await _queryService.ReadAllAdrByNumber(sequence, _fileSystemService, _directoryPath, _config);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task ReadAllAdrByNumber_WithInvalidFileComponents_FiltersOutInvalidFiles()
    {
        // Arrange
        const int sequence = 1;
        const string fileName1 = "ADR-0001-Valid.md";
        const string fileName2 = "ADR-0001-Invalid.md";
        var filePath1 = PathHelper.GetAdrFilePath(fileName1);
        var filePath2 = PathHelper.GetAdrFilePath(fileName2);
        var searchPattern = $"{_config.Prefix}{sequence:D4}{_config.Separator}*.md";

        _fileSystemService.DirectoryExists(_directoryPath).Returns(true);
        _fileSystemService.GetFiles(_directoryPath, searchPattern).Returns([filePath1, filePath2]);

        var validComponent = CreateAdrFileNameComponents(number: 1, fileName: fileName1, isValid: true);
        var invalidComponent = CreateAdrFileNameComponents(number: 1, fileName: fileName2, isValid: false);

        _fileParser.ParseFileName(filePath1, _config, _fileSystemService).Returns(validComponent);
        _fileParser.ParseFileName(filePath2, _config, _fileSystemService).Returns(invalidComponent);

        // Act
        var result = await _queryService.ReadAllAdrByNumber(sequence, _fileSystemService, _directoryPath, _config);

        // Assert
        result.Should().HaveCount(1);
        result[0].FileName.Should().Be(fileName1);
    }

    [Fact]
    public async Task ReadAllAdrByNumber_WithInvalidHeaderInFileComponents_FiltersOutInvalidHeaders()
    {
        // Arrange
        const int sequence = 1;
        const string filePath1 = "ADR-0001-Valid.md";
        const string filePath2 = "ADR-0001-InvalidHeader.md";
        var searchPattern = $"{_config.Prefix}{sequence:D4}{_config.Separator}*.md";

        _fileSystemService.DirectoryExists(_directoryPath).Returns(true);
        _fileSystemService.GetFiles(_directoryPath, searchPattern).Returns([filePath1, filePath2]);

        var validComponent = CreateAdrFileNameComponents(number: 1, isValid: true, headerIsValid: true);
        var invalidHeaderComponent = CreateAdrFileNameComponents(number: 1, isValid: true, headerIsValid: false);

        _fileParser.ParseFileName(filePath1, _config, _fileSystemService).Returns(validComponent);
        _fileParser.ParseFileName(filePath2, _config, _fileSystemService).Returns(invalidHeaderComponent);

        // Act
        var result = await _queryService.ReadAllAdrByNumber(sequence, _fileSystemService, _directoryPath, _config);

        // Assert
        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task ReadAllAdrByNumber_WithNullConfig_ThrowsArgumentNullException()
    {
        // Act & Assert
        var action = () => _queryService.ReadAllAdrByNumber(1, _fileSystemService, _directoryPath, null!);
        await action.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("config");
    }

    [Fact]
    public async Task ReadAllAdrByNumber_WithNullFileSystemService_ThrowsArgumentNullException()
    {
        // Act & Assert
        var action = () => _queryService.ReadAllAdrByNumber(1, null!, _directoryPath, _config);
        await action.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("fileSystemService");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ReadAllAdrByNumber_WithEmptyOrWhitespaceDirectoryPath_ThrowsArgumentException(string emptyPath)
    {
        // Act & Assert
        var action = () => _queryService.ReadAllAdrByNumber(1, _fileSystemService, emptyPath, _config);
        await action.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("directoryPath");
    }

    [Fact]
    public async Task ReadAllAdrByNumber_WithNonExistentDirectory_ThrowsDirectoryNotFoundException()
    {
        // Arrange
        _fileSystemService.DirectoryExists(_directoryPath).Returns(false);

        // Act & Assert
        var action = () => _queryService.ReadAllAdrByNumber(1, _fileSystemService, _directoryPath, _config);
        await action.Should().ThrowAsync<DirectoryNotFoundException>();
    }

    #endregion

    #region ReadAllAdrFiles Tests

    [Fact]
    public async Task ReadAllAdrFiles_WithValidDirectory_ReturnsAllValidAdrFiles()
    {
        // Arrange
        var filePath1 = PathHelper.GetAdrFilePath("ADR-0001-Decision1.md");
        var filePath2 = PathHelper.GetAdrFilePath("ADR-0002-Decision2.md");
        var filePath3 = PathHelper.GetAdrFilePath("subfolder/ADR-0003-Decision3.md");

        _fileSystemService.DirectoryExists(_directoryPath).Returns(true);
        _fileSystemService.GetFiles(_directoryPath, "*.md", SearchOption.AllDirectories)
            .Returns([filePath1, filePath2, filePath3]);

        var adrComponent1 = CreateAdrFileNameComponents(number: 1);
        var adrComponent2 = CreateAdrFileNameComponents(number: 2);
        var adrComponent3 = CreateAdrFileNameComponents(number: 3);

        _fileParser.ParseFileName(filePath1, _config, _fileSystemService).Returns(adrComponent1);
        _fileParser.ParseFileName(filePath2, _config, _fileSystemService).Returns(adrComponent2);
        _fileParser.ParseFileName(filePath3, _config, _fileSystemService).Returns(adrComponent3);

        // Act
        var result = await _queryService.ReadAllAdrFiles(_fileSystemService, _directoryPath, _config);

        // Assert
        result.Should().HaveCount(3);
        result.Should().ContainInOrder(adrComponent1, adrComponent2, adrComponent3);
    }

    [Fact]
    public async Task ReadAllAdrFiles_WithEmptyDirectory_ReturnsEmptyArray()
    {
        // Arrange
        _fileSystemService.DirectoryExists(_directoryPath).Returns(true);
        _fileSystemService.GetFiles(_directoryPath, "*.md", SearchOption.AllDirectories).Returns([]);

        // Act
        var result = await _queryService.ReadAllAdrFiles(_fileSystemService, _directoryPath, _config);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task ReadAllAdrFiles_WithInvalidFileComponents_FiltersOutInvalid()
    {
        // Arrange
        var filePath1 = PathHelper.GetAdrFilePath("ADR-0001-Valid.md");
        var filePath2 = PathHelper.GetAdrFilePath("ADR-0002-Invalid.md");

        _fileSystemService.DirectoryExists(_directoryPath).Returns(true);
        _fileSystemService.GetFiles(_directoryPath, "*.md", SearchOption.AllDirectories)
            .Returns([filePath1, filePath2]);

        var validComponent = CreateAdrFileNameComponents(number: 1, isValid: true);
        var invalidComponent = CreateAdrFileNameComponents(number: 2, isValid: false);

        _fileParser.ParseFileName(filePath1, _config, _fileSystemService).Returns(validComponent);
        _fileParser.ParseFileName(filePath2, _config, _fileSystemService).Returns(invalidComponent);

        // Act
        var result = await _queryService.ReadAllAdrFiles(_fileSystemService, _directoryPath, _config);

        // Assert
        result.Should().HaveCount(1);
        result[0].Number.Should().Be(1);
    }

    [Fact]
    public async Task ReadAllAdrFiles_WithNullConfig_ThrowsArgumentNullException()
    {
        // Act & Assert
        var action = () => _queryService.ReadAllAdrFiles(_fileSystemService, _directoryPath, null!);
        await action.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task ReadAllAdrFiles_WithNonExistentDirectory_ThrowsDirectoryNotFoundException()
    {
        // Arrange
        _fileSystemService.DirectoryExists(_directoryPath).Returns(false);

        // Act & Assert
        var action = () => _queryService.ReadAllAdrFiles(_fileSystemService, _directoryPath, _config);
        await action.Should().ThrowAsync<DirectoryNotFoundException>();
    }

    #endregion

    #region ReadLatestAdrFiles Tests

    [Fact]
    public async Task ReadLatestAdrFiles_WithMultipleVersions_ReturnsLatestVersionOfEachAdr()
    {
        // Arrange
        var filePath1 = PathHelper.GetAdrFilePath("ADR-0001-Decision1.md");
        var filePath2 = PathHelper.GetAdrFilePath("ADR-0001-v02-Decision1.md");
        var filePath3 = PathHelper.GetAdrFilePath("ADR-0002-Decision2.md");

        _fileSystemService.DirectoryExists(_directoryPath).Returns(true);
        _fileSystemService.GetFiles(_directoryPath, "*.md", SearchOption.AllDirectories)
            .Returns([filePath1, filePath2, filePath3]);

        var adrComponent1V1 = CreateAdrFileNameComponents(number: 1, version: 1, statusUpdate: AdrStatus.Proposed);
        var adrComponent1V2 = CreateAdrFileNameComponents(number: 1, version: 2, statusUpdate: AdrStatus.Proposed);
        var adrComponent2V1 = CreateAdrFileNameComponents(number: 2, version: 1, statusUpdate: AdrStatus.Proposed);

        _fileParser.ParseFileName(filePath1, _config, _fileSystemService).Returns(adrComponent1V1);
        _fileParser.ParseFileName(filePath2, _config, _fileSystemService).Returns(adrComponent1V2);
        _fileParser.ParseFileName(filePath3, _config, _fileSystemService).Returns(adrComponent2V1);

        // Act
        var result = await _queryService.ReadLatestAdrFiles(_fileSystemService, _directoryPath, _config);

        // Assert
        result.Should().HaveCount(2);
        result[0].Number.Should().Be(2); // Ordered by number descending
        result[1].Number.Should().Be(1);
        result[1].Version.Should().Be(2); // V2 selected for ADR 1
    }

    [Fact]
    public async Task ReadLatestAdrFiles_WithMultipleRevisionsAndVersions_ReturnsLatestRevision()
    {
        // Arrange
        _config.LenRevision = 2;
        var filePath1 = PathHelper.GetAdrFilePath("ADR-0001-Decision1.md");
        var filePath2 = PathHelper.GetAdrFilePath("ADR-0001-v01-rev01-Decision1.md");
        var filePath3 = PathHelper.GetAdrFilePath("ADR-0001-v01-rev02-Decision1.md");

        _fileSystemService.DirectoryExists(_directoryPath).Returns(true);
        _fileSystemService.GetFiles(_directoryPath, "*.md", SearchOption.AllDirectories)
            .Returns([filePath1, filePath2, filePath3]);

        var adrComponent1 = CreateAdrFileNameComponents(number: 1, version: 1, revision: null, statusUpdate: AdrStatus.Proposed);
        var adrComponent2 = CreateAdrFileNameComponents(number: 1, version: 1, revision: 1, statusUpdate: AdrStatus.Proposed);
        var adrComponent3 = CreateAdrFileNameComponents(number: 1, version: 1, revision: 2, statusUpdate: AdrStatus.Proposed);

        _fileParser.ParseFileName(filePath1, _config, _fileSystemService).Returns(adrComponent1);
        _fileParser.ParseFileName(filePath2, _config, _fileSystemService).Returns(adrComponent2);
        _fileParser.ParseFileName(filePath3, _config, _fileSystemService).Returns(adrComponent3);

        // Act
        var result = await _queryService.ReadLatestAdrFiles(_fileSystemService, _directoryPath, _config);

        // Assert
        result.Should().HaveCount(1);
        result[0].Revision.Should().Be(2); // Latest revision
    }

    [Fact]
    public async Task ReadLatestAdrFiles_WithDifferentStatusUpdates_GroupsByNumberAndStatusUpdate()
    {
        // Arrange
        var filePath1 = PathHelper.GetAdrFilePath("ADR-0001-Proposed.md");
        var filePath2 = PathHelper.GetAdrFilePath("ADR-0001-Accepted.md");

        _fileSystemService.DirectoryExists(_directoryPath).Returns(true);
        _fileSystemService.GetFiles(_directoryPath, "*.md", SearchOption.AllDirectories)
            .Returns([filePath1, filePath2]);

        var adrComponent1 = CreateAdrFileNameComponents(number: 1, version: 1, statusUpdate: AdrStatus.Proposed);
        var adrComponent2 = CreateAdrFileNameComponents(number: 1, version: 1, statusUpdate: AdrStatus.Accepted);

        _fileParser.ParseFileName(filePath1, _config, _fileSystemService).Returns(adrComponent1);
        _fileParser.ParseFileName(filePath2, _config, _fileSystemService).Returns(adrComponent2);

        // Act
        var result = await _queryService.ReadLatestAdrFiles(_fileSystemService, _directoryPath, _config);

        // Assert
        result.Should().HaveCount(2); // Both statuses present
    }

    [Fact]
    public async Task ReadLatestAdrFiles_WithEmptyDirectory_ReturnsEmptyArray()
    {
        // Arrange
        _fileSystemService.DirectoryExists(_directoryPath).Returns(true);
        _fileSystemService.GetFiles(_directoryPath, "*.md", SearchOption.AllDirectories).Returns([]);

        // Act
        var result = await _queryService.ReadLatestAdrFiles(_fileSystemService, _directoryPath, _config);

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region GetFileByUniqueTitle Tests

    [Fact]
    public async Task GetFileByUniqueTitle_WithMatchingTitle_ReturnsFileName()
    {
        // Arrange
        const string title = "Decision";
        const string domain = "Enterprise";
        var uniqueTitle = AdrFileNameComponents.CreateUniqueTitle(
            title.ToCase(_config.CaseTransform),
            domain.ToCase(_config.CaseTransform));

        var filePath = PathHelper.GetAdrFilePath("ADR-0001-Decision.md");
        _fileSystemService.DirectoryExists(_directoryPath).Returns(true);
        _fileSystemService.GetFiles(_directoryPath, "*.md", SearchOption.AllDirectories).Returns([filePath]);

        var adrComponent = CreateAdrFileNameComponents(number: 1, title: "Decision", domain: "Enterprise", fileName: "ADR-0001-Decision.md");
        _fileParser.ParseFileName(filePath, _config, _fileSystemService).Returns(adrComponent);

        // Act
        var result = await _queryService.GetFileByUniqueTitle(title, domain, _fileSystemService, _directoryPath, _config);

        // Assert
        result.Should().Be("ADR-0001-Decision.md");
    }

    [Fact]
    public async Task GetFileByUniqueTitle_WithNoMatchingTitle_ReturnsEmptyString()
    {
        // Arrange
        const string title = "NonExistent";
        const string domain = "Unknown";
        var filePath = PathHelper.GetAdrFilePath("ADR-0001-DecisionEnterprise.md");

        _fileSystemService.DirectoryExists(_directoryPath).Returns(true);
        _fileSystemService.GetFiles(_directoryPath, "*.md", SearchOption.AllDirectories).Returns([filePath]);

        var adrComponent = CreateAdrFileNameComponents(number: 1, title: "Decision", domain: "Enterprise");
        _fileParser.ParseFileName(filePath, _config, _fileSystemService).Returns(adrComponent);

        // Act
        var result = await _queryService.GetFileByUniqueTitle(title, domain, _fileSystemService, _directoryPath, _config);

        // Assert
        result.Should().Be(string.Empty);
    }

    [Fact]
    public async Task GetFileByUniqueTitle_WithEmptyDirectory_ReturnsEmptyString()
    {
        // Arrange
        _fileSystemService.DirectoryExists(_directoryPath).Returns(true);
        _fileSystemService.GetFiles(_directoryPath, "*.md", SearchOption.AllDirectories).Returns([]);

        // Act
        var result = await _queryService.GetFileByUniqueTitle("Title", "Domain", _fileSystemService, _directoryPath, _config);

        // Assert
        result.Should().Be(string.Empty);
    }

    #endregion

    #region GetNextNumber Tests

    [Fact]
    public async Task GetNextNumber_WithExistingFiles_ReturnsMaxNumberPlusOne()
    {
        // Arrange
        var filePath1 = PathHelper.GetAdrFilePath("ADR-0001-Decision1.md");
        var filePath2 = PathHelper.GetAdrFilePath("ADR-0003-Decision3.md");
        var filePath3 = PathHelper.GetAdrFilePath("ADR-0002-Decision2.md");

        _fileSystemService.DirectoryExists(_directoryPath).Returns(true);
        _fileSystemService.GetFiles(_directoryPath, "*.md", SearchOption.AllDirectories)
            .Returns([filePath1, filePath2, filePath3]);

        var adrComponent1 = CreateAdrFileNameComponents(number: 1);
        var adrComponent2 = CreateAdrFileNameComponents(number: 3);
        var adrComponent3 = CreateAdrFileNameComponents(number: 2);

        _fileParser.ParseFileName(filePath1, _config, _fileSystemService).Returns(adrComponent1);
        _fileParser.ParseFileName(filePath2, _config, _fileSystemService).Returns(adrComponent2);
        _fileParser.ParseFileName(filePath3, _config, _fileSystemService).Returns(adrComponent3);

        // Act
        var result = await _queryService.GetNextNumber(_fileSystemService, _directoryPath, _config);

        // Assert
        result.Should().Be(4);
    }

    [Fact]
    public async Task GetNextNumber_WithEmptyDirectory_ReturnsOne()
    {
        // Arrange
        _fileSystemService.DirectoryExists(_directoryPath).Returns(true);
        _fileSystemService.GetFiles(_directoryPath, "*.md", SearchOption.AllDirectories).Returns([]);

        // Act
        var result = await _queryService.GetNextNumber(_fileSystemService, _directoryPath, _config);

        // Assert
        result.Should().Be(1);
    }

    [Fact]
    public async Task GetNextNumber_WithMultipleFiles_ReturnsCorrectNextNumber()
    {
        // Arrange
        var filePaths = Enumerable.Range(1, 10)
            .Select(i => PathHelper.GetAdrFilePath($"ADR-{i:D4}-Decision{i}.md"))
            .ToArray();

        _fileSystemService.DirectoryExists(_directoryPath).Returns(true);
        _fileSystemService.GetFiles(_directoryPath, "*.md", SearchOption.AllDirectories).Returns(filePaths);

        for (int i = 1; i <= 10; i++)
        {
            var adrComponent = CreateAdrFileNameComponents(number: i);
            _fileParser.ParseFileName(filePaths[i - 1], _config, _fileSystemService).Returns(adrComponent);
        }

        // Act
        var result = await _queryService.GetNextNumber(_fileSystemService, _directoryPath, _config);

        // Assert
        result.Should().Be(11);
    }

    #endregion

    #region GetLatestADRSequence Tests

    [Fact]
    public async Task GetLatestADRSequence_WithMultipleVersions_ReturnsLatestVersion()
    {
        // Arrange
        const int sequence = 1;
        var filePath1 = PathHelper.GetAdrFilePath("ADR-0001-v01-Decision1.md");
        var filePath2 = PathHelper.GetAdrFilePath("ADR-0001-v03-Decision1.md");
        var filePath3 = PathHelper.GetAdrFilePath("ADR-0001-v02-Decision1.md");
        var searchPattern = $"{_config.Prefix}{sequence:D4}{_config.Separator}*.md";

        _fileSystemService.DirectoryExists(_directoryPath).Returns(true);
        _fileSystemService.GetFiles(_directoryPath, searchPattern).Returns([filePath1, filePath2, filePath3]);

        var adrComponent1 = CreateAdrFileNameComponents(number: 1, version: 1);
        var adrComponent2 = CreateAdrFileNameComponents(number: 1, version: 3);
        var adrComponent3 = CreateAdrFileNameComponents(number: 1, version: 2);

        _fileParser.ParseFileName(filePath1, _config, _fileSystemService).Returns(adrComponent1);
        _fileParser.ParseFileName(filePath2, _config, _fileSystemService).Returns(adrComponent2);
        _fileParser.ParseFileName(filePath3, _config, _fileSystemService).Returns(adrComponent3);

        // Act
        var result = await _queryService.GetLatestADRSequence(sequence, _fileSystemService, _directoryPath, _config);

        // Assert
        result.Should().NotBeNull();
        result!.Version.Should().Be(3);
    }

    [Fact]
    public async Task GetLatestADRSequence_WithMultipleRevisionsAndVersions_ReturnsHighestVersionAndRevision()
    {
        // Arrange
        _config.LenRevision = 2;
        const int sequence = 1;
        var filePath1 = PathHelper.GetAdrFilePath("ADR-0001-v01-rev01-Decision1.md");
        var filePath2 = PathHelper.GetAdrFilePath("ADR-0001-v02-rev03-Decision1.md");
        var filePath3 = PathHelper.GetAdrFilePath("ADR-0001-v02-rev01-Decision1.md");
        var searchPattern = $"{_config.Prefix}{sequence:D4}{_config.Separator}*.md";

        _fileSystemService.DirectoryExists(_directoryPath).Returns(true);
        _fileSystemService.GetFiles(_directoryPath, searchPattern).Returns([filePath1, filePath2, filePath3]);

        var adrComponent1 = CreateAdrFileNameComponents(number: 1, version: 1, revision: 1);
        var adrComponent2 = CreateAdrFileNameComponents(number: 1, version: 2, revision: 3);
        var adrComponent3 = CreateAdrFileNameComponents(number: 1, version: 2, revision: 1);

        _fileParser.ParseFileName(filePath1, _config, _fileSystemService).Returns(adrComponent1);
        _fileParser.ParseFileName(filePath2, _config, _fileSystemService).Returns(adrComponent2);
        _fileParser.ParseFileName(filePath3, _config, _fileSystemService).Returns(adrComponent3);

        // Act
        var result = await _queryService.GetLatestADRSequence(sequence, _fileSystemService, _directoryPath, _config);

        // Assert
        result.Should().NotBeNull();
        result!.Version.Should().Be(2);
        result.Revision.Should().Be(3);
    }

    [Fact]
    public async Task GetLatestADRSequence_WithNoFiles_ThrowsInvalidOperationException()
    {
        // Arrange
        const int sequence = 99;
        var searchPattern = $"{_config.Prefix}{sequence:D4}{_config.Separator}*.md";

        _fileSystemService.DirectoryExists(_directoryPath).Returns(true);
        _fileSystemService.GetFiles(_directoryPath, searchPattern).Returns([]);

        // Act & Assert
        var action = () => _queryService.GetLatestADRSequence(sequence, _fileSystemService, _directoryPath, _config);
        await action.Should().ThrowAsync<InvalidOperationException>();
    }

    #endregion

    #region GetDomains Tests

    [Fact]
    public async Task GetDomains_WithMultipleDomainsInFiles_ReturnsDistinctDomains()
    {
        // Arrange
        var filePath1 = PathHelper.GetAdrFilePath("ADR-0001-Decision1.md");
        var filePath2 = PathHelper.GetAdrFilePath("ADR-0002-Decision2.md");
        var filePath3 = PathHelper.GetAdrFilePath("ADR-0003-Decision3.md");
        var filePath4 = PathHelper.GetAdrFilePath("ADR-0004-Decision4.md");

        _fileSystemService.DirectoryExists(_directoryPath).Returns(true);
        _fileSystemService.GetFiles(_directoryPath, "*.md", SearchOption.AllDirectories)
            .Returns([filePath1, filePath2, filePath3, filePath4]);

        var adrComponent1 = CreateAdrFileNameComponents(number: 1, domain: "Enterprise");
        var adrComponent2 = CreateAdrFileNameComponents(number: 2, domain: "Team");
        var adrComponent3 = CreateAdrFileNameComponents(number: 3, domain: "Enterprise");
        var adrComponent4 = CreateAdrFileNameComponents(number: 4, domain: "Project");

        _fileParser.ParseFileName(filePath1, _config, _fileSystemService).Returns(adrComponent1);
        _fileParser.ParseFileName(filePath2, _config, _fileSystemService).Returns(adrComponent2);
        _fileParser.ParseFileName(filePath3, _config, _fileSystemService).Returns(adrComponent3);
        _fileParser.ParseFileName(filePath4, _config, _fileSystemService).Returns(adrComponent4);

        // Act
        var result = await _queryService.GetDomains(_fileSystemService, _directoryPath, _config);

        // Assert
        result.Should().HaveCount(3);
        result.Should().Contain(new[] { "Enterprise", "Team", "Project" });
        result.Distinct().Should().HaveCount(result.Length);
    }

    [Fact]
    public async Task GetDomains_WithNoDomainsInFiles_ReturnsEmptyArray()
    {
        // Arrange
        var filePath1 = PathHelper.GetAdrFilePath("ADR-0001-Decision1.md");

        _fileSystemService.DirectoryExists(_directoryPath).Returns(true);
        _fileSystemService.GetFiles(_directoryPath, "*.md", SearchOption.AllDirectories).Returns([filePath1]);

        var adrComponent1 = CreateAdrFileNameComponents(number: 1, domain: null);
        _fileParser.ParseFileName(filePath1, _config, _fileSystemService).Returns(adrComponent1);

        // Act
        var result = await _queryService.GetDomains(_fileSystemService, _directoryPath, _config);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetDomains_WithEmptyDirectory_ReturnsEmptyArray()
    {
        // Arrange
        _fileSystemService.DirectoryExists(_directoryPath).Returns(true);
        _fileSystemService.GetFiles(_directoryPath, "*.md", SearchOption.AllDirectories).Returns([]);

        // Act
        var result = await _queryService.GetDomains(_fileSystemService, _directoryPath, _config);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetDomains_WithEmptyDomainStrings_FiltersThemOut()
    {
        // Arrange
        var filePath1 = PathHelper.GetAdrFilePath("ADR-0001-Decision1.md");
        var filePath2 = PathHelper.GetAdrFilePath("ADR-0002-Decision2.md");

        _fileSystemService.DirectoryExists(_directoryPath).Returns(true);
        _fileSystemService.GetFiles(_directoryPath, "*.md", SearchOption.AllDirectories)
            .Returns([filePath1, filePath2]);

        var adrComponent1 = CreateAdrFileNameComponents(number: 1, domain: string.Empty);
        var adrComponent2 = CreateAdrFileNameComponents(number: 2, domain: "Enterprise");

        _fileParser.ParseFileName(filePath1, _config, _fileSystemService).Returns(adrComponent1);
        _fileParser.ParseFileName(filePath2, _config, _fileSystemService).Returns(adrComponent2);

        // Act
        var result = await _queryService.GetDomains(_fileSystemService, _directoryPath, _config);

        // Assert
        result.Should().HaveCount(1);
        result[0].Should().Be("Enterprise");
    }

    #endregion

    #region Helper Methods

    private static AdrFileNameComponents CreateAdrFileNameComponents(
        int number = 1,
        int version = 1,
        int? revision = null,
        string fileName = "ADR-0001-Decision.md",
        string? title = null,
        string? domain = null,
        bool isValid = true,
        bool headerIsValid = true,
        AdrStatus statusUpdate = AdrStatus.Unknown)
    {
        return new AdrFileNameComponents
        {
            Number = number,
            Version = version,
            Revision = revision,
            FileName = fileName,
            Title = title ?? "Decision",
            Domain = domain,
            IsValid = isValid,
            Header = new AdrHeader
            {
                IsValid = headerIsValid,
                StatusUpdate = statusUpdate,
                Title = title ?? "Decision",
                Domain = domain ?? string.Empty
            },
            Prefix = "ADR"
        };
    }

    #endregion
}
