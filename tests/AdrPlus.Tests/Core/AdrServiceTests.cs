// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the AdrPlus project under MIT license
// ***************************************************************************************

using AdrPlus.Core;
using AdrPlus.Domain;
using AdrPlus.Infrastructure.FileSystem;
using AdrPlus.Tests.Helpers;

namespace AdrPlus.Tests.Core;

public class AdrServiceTests
{
    private readonly IFileSystemService _fileSystemService;
    private readonly AdrService _adrUtil;
    private readonly AdrPlusRepoConfig _repoConfig;

    public AdrServiceTests()
    {
        _fileSystemService = Substitute.For<IFileSystemService>();
        _adrUtil = new AdrService();
        _repoConfig = CreateDefaultRepoConfig();
    }

    private static AdrPlusRepoConfig CreateDefaultRepoConfig()
    {
        return new AdrPlusRepoConfig(
            defaultTemplate: "# ADR Template\n\n## Context\n\n## Decision\n\n## Consequences",
            defaultFolder: "docs/adr")
        {
            Prefix = "ADR",
            LenSeq = 4,
            LenVersion = 2,
            LenRevision = 2,
            LenScope = 0, // Sem scope para simplificar os testes
            Separator = '-',
            StatusNew = "Proposed",
            StatusAcc = "Accepted",
            StatusRej = "Rejected",
            StatusSup = "Superseded",
            Scopes = string.Empty,
            SkipDomain = string.Empty,
            FolderByScope = false
        };
    }

    #region StatusUpdateAdrAsync Tests

    [Theory]
    [MemberData(nameof(CultureData.Cultures), MemberType = typeof(CultureData))]
    public async Task StatusUpdateAdrAsync_WhenFileIsValid_UpdatesStatusAndReturnsSuccess(string cultureName)
    {
        await CultureData.WithCultureAsync(cultureName, async () =>
        {
            // Arrange
            var filePath = "docs/adr/ADR0001-test-V01R01.md";
            var adrStatus = AdrStatus.Accepted;
            var dref = DateTime.UtcNow;
            var cancellationToken = CancellationToken.None;

            var fileContent = CreateValidAdrFileContent();
            _fileSystemService.ReadAllLinesAsync(filePath).Returns(fileContent);
            _fileSystemService.FileExists(filePath).Returns(true);

            // Act
            var (isValid, error) = await _adrUtil.StatusUpdateAdrAsync(
                filePath, adrStatus, dref, _repoConfig, _fileSystemService, cancellationToken);

            // Assert
            isValid.Should().BeTrue();
            error.Should().BeEmpty();
            await _fileSystemService.Received(1).WriteAllTextAsync(
                filePath, Arg.Any<string>(), cancellationToken);
        });
    }

    [Fact]
    public async Task StatusUpdateAdrAsync_WhenFileIsInvalid_ReturnsError()
    {
        // Arrange
        var filePath = "docs/adr/invalid-file.md";
        var adrStatus = AdrStatus.Accepted;
        var dref = DateTime.UtcNow;
        var cancellationToken = CancellationToken.None;

        _fileSystemService.ReadAllLinesAsync(filePath).Returns(["# Invalid"]);

        // Act
        var (isValid, error) = await _adrUtil.StatusUpdateAdrAsync(
            filePath, adrStatus, dref, _repoConfig, _fileSystemService, cancellationToken);

        // Assert
        isValid.Should().BeFalse();
        error.Should().NotBeEmpty();
        await _fileSystemService.DidNotReceive().WriteAllTextAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    #endregion

    #region StatusChangeSupersedeAdrAsync Tests

    [Theory]
    [MemberData(nameof(CultureData.Cultures), MemberType = typeof(CultureData))]
    public async Task StatusChangeSupersedeAdrAsync_WhenFileIsValid_UpdatesStatusToSuperseded(string cultureName)
    {
        await CultureData.WithCultureAsync(cultureName, async () =>
        {
            // Arrange
            var filePath = "docs/adr/ADR0001-test-V01R01.md";
            var supersedeFileName = "ADR0002-new-V01R01.md";
            var dref = DateTime.UtcNow;
            var cancellationToken = CancellationToken.None;

            var fileContent = CreateValidAdrFileContent();
            _fileSystemService.ReadAllLinesAsync(filePath).Returns(fileContent);

            // Act
            var (isValid, error) = await _adrUtil.StatusChangeSupersedeAdrAsync(
                filePath, supersedeFileName, dref, _repoConfig, _fileSystemService, cancellationToken);

            // Assert
            isValid.Should().BeTrue();
            error.Should().BeEmpty();
            await _fileSystemService.Received(1).WriteAllTextAsync(
                filePath, Arg.Is<string>(s => s.Contains("Superseded")), cancellationToken);
        });
    }

    #endregion

    #region StatusChangeAdrAsync Tests

    [Theory]
    [MemberData(nameof(CultureData.Cultures), MemberType = typeof(CultureData))]
    public async Task StatusChangeAdrAsync_WhenFileIsValid_UpdatesStatusChange(string cultureName)
    {
        await CultureData.WithCultureAsync(cultureName, async () =>
        {
            // Arrange
            var filePath = "docs/adr/ADR0001-test-V01R01.md";
            var adrStatus = AdrStatus.Rejected;
            var dref = DateTime.UtcNow;
            var cancellationToken = CancellationToken.None;

            var fileContent = CreateValidAdrFileContent();
            _fileSystemService.ReadAllLinesAsync(filePath).Returns(fileContent);

            // Act
            var (IsValid, Error) = await _adrUtil.StatusChangeAdrAsync(
                filePath, adrStatus, dref, _repoConfig, _fileSystemService, cancellationToken);

            // Assert
            IsValid.Should().BeTrue();
            Error.Should().BeEmpty();
            await _fileSystemService.Received(1).WriteAllTextAsync(
                filePath, Arg.Any<string>(), cancellationToken);
        });
    }

    #endregion

    #region ParseAdrHeaderAndContentAsync Tests

    [Theory]
    [MemberData(nameof(CultureData.Cultures), MemberType = typeof(CultureData))]
    public async Task ParseAdrHeaderAndContentAsync_WhenValidFile_ParsesHeaderCorrectly(string cultureName)
    {
        await CultureData.WithCultureAsync(cultureName, async () =>
        {
            // Arrange
            var filePath = "docs/adr/ADR0001-test-adr.md";
            var fileContent = CreateValidAdrFileContent();
            _fileSystemService.ReadAllLinesAsync(filePath).Returns(fileContent);

            // Act
            var (header, content) = await _adrUtil.ParseAdrHeaderAndContentAsync(
                filePath, _repoConfig, _fileSystemService);

            // Assert
            header.IsValid.Should().BeTrue();
            header.Title.Should().Be("Test ADR");
            header.StatusCreate.Should().Be(AdrStatus.Proposed);
            header.Version.Should().Be(1);
            header.Revision.Should().Be(1);
            content.Should().NotBeEmpty();
        });
    }

    [Theory]
    [MemberData(nameof(CultureData.Cultures), MemberType = typeof(CultureData))]
    public async Task ParseAdrHeaderAndContentAsync_WithDomainOnly_ParsesCorrectly(string cultureName)
    {
        await CultureData.WithCultureAsync(cultureName, async () =>
        {
            // Arrange - Sample: "##### MyDomain" (without scope)
            var filePath = "docs/adr/ADR0001-test.md";
            var fileContent = CreateValidAdrFileContentWithDomainOnly("MyDomain");
            _fileSystemService.ReadAllLinesAsync(filePath).Returns(fileContent);

            // Act
            var (header, content) = await _adrUtil.ParseAdrHeaderAndContentAsync(
                filePath, _repoConfig, _fileSystemService);

            // Assert
            header.IsValid.Should().BeTrue();
            header.Scope.Should().Be("MyDomain");
            header.Domain.Should().BeNullOrEmpty();
            content.Should().NotBeEmpty();
        });
    }

    [Theory]
    [MemberData(nameof(CultureData.Cultures), MemberType = typeof(CultureData))]
    public async Task ParseAdrHeaderAndContentAsync_WithDomainAndScope_ParsesCorrectly(string cultureName)
    {
        await CultureData.WithCultureAsync(cultureName, async () =>
        {
            // Arrange - Sample: "##### MyDomain: MyScope"
            var filePath = "docs/adr/ADR0001-test.md";
            var fileContent = CreateValidAdrFileContentWithScopeAndDomain("MyDomain", "MyScope");
            _fileSystemService.ReadAllLinesAsync(filePath).Returns(fileContent);

            // Act
            var (header, content) = await _adrUtil.ParseAdrHeaderAndContentAsync(
                filePath, _repoConfig, _fileSystemService);

            // Assert
            header.IsValid.Should().BeTrue();
            header.Scope.Should().Be("MyDomain");
            header.Domain.Should().Be("MyScope");
            content.Should().NotBeEmpty();
        });
    }

    [Theory]
    [MemberData(nameof(CultureData.Cultures), MemberType = typeof(CultureData))]
    public async Task ParseAdrHeaderAndContentAsync_WithScopeNoRevision_ParsesCorrectly(string cultureName)
    {
        await CultureData.WithCultureAsync(cultureName, async () =>
        {
            // Arrange - Sample with "##### Revision: -"
            var filePath = "docs/adr/ADR0001-test-adr.md";
            var configNoRevision = CreateRepoConfigNoRevision();
            var fileContent = CreateValidAdrFileContentNoRevision();
            _fileSystemService.ReadAllLinesAsync(filePath).Returns(fileContent);

            // Act
            var (header, content) = await _adrUtil.ParseAdrHeaderAndContentAsync(
                filePath, configNoRevision, _fileSystemService);

            // Assert
            header.IsValid.Should().BeTrue();
            header.Title.Should().Be("Test ADR");
            header.Version.Should().Be(1);
            header.Revision.Should().BeNull();
            content.Should().NotBeEmpty();
        });
    }

    [Theory]
    [MemberData(nameof(CultureData.Cultures), MemberType = typeof(CultureData))]
    public async Task ParseAdrHeaderAndContentAsync_WithScopeInskipdomain_ParsesWithoutDomain(string cultureName)
    {
        await CultureData.WithCultureAsync(cultureName, async () =>
        {
        // Arrange - Scope without domain when in skipdomain
        var filePath = "docs/adr/ADR0001-test-adr.md";
        var configWithskipdomain = CreateRepoConfigWithskipdomain();
        var fileContent = CreateValidAdrFileContentWithScopeOnly("DB");
        _fileSystemService.ReadAllLinesAsync(filePath).Returns(fileContent);

        // Act
        var (header, content) = await _adrUtil.ParseAdrHeaderAndContentAsync(
            filePath, configWithskipdomain, _fileSystemService);

        // Assert
        header.IsValid.Should().BeTrue();
        header.Title.Should().Be("Test ADR");
        header.Scope.Should().Be("DB");
        header.Domain.Should().BeNullOrEmpty();
        content.Should().NotBeEmpty();
        });
    }


    [Theory]
    [MemberData(nameof(CultureData.Cultures), MemberType = typeof(CultureData))]
    public async Task ParseAdrHeaderAndContentAsync_WithStatusProposedOnly_ParsesCorrectly(string cultureName)
    {
        await CultureData.WithCultureAsync(cultureName, async () =>
        {
            // Arrange - Sample: "- Proposed (01/04/2026)" only
            var filePath = "docs/adr/ADR0001-test.md";
            var fileContent = CreateValidAdrFileContentProposedOnly();
            _fileSystemService.ReadAllLinesAsync(filePath).Returns(fileContent);

            // Act
            var (header, content) = await _adrUtil.ParseAdrHeaderAndContentAsync(
                filePath, _repoConfig, _fileSystemService);

            // Assert
            header.IsValid.Should().BeTrue();
            header.StatusCreate.Should().Be(AdrStatus.Proposed);
            header.DateCreate.Should().NotBeNull();
            header.StatusUpdate.Should().Be(AdrStatus.Unknown);
            header.StatusChange.Should().Be(AdrStatus.Unknown);
            content.Should().NotBeEmpty();
        });
    }

    [Theory]
    [MemberData(nameof(CultureData.Cultures), MemberType = typeof(CultureData))]
    public async Task ParseAdrHeaderAndContentAsync_WithStatusAccepted_ParsesCorrectly(string cultureName)
    {
        await CultureData.WithCultureAsync(cultureName, async () =>
        {
            // Arrange - Sample: "- Accepted (02/04/2026)"
            var filePath = "docs/adr/ADR0001-test.md";
            var fileContent = CreateValidAdrFileContentWithStatusUpdate(AdrStatus.Accepted);
            _fileSystemService.ReadAllLinesAsync(filePath).Returns(fileContent);

            // Act
            var (header, content) = await _adrUtil.ParseAdrHeaderAndContentAsync(
                filePath, _repoConfig, _fileSystemService);

            // Assert
            header.IsValid.Should().BeTrue();
            header.StatusCreate.Should().Be(AdrStatus.Proposed);
            header.StatusUpdate.Should().Be(AdrStatus.Accepted);
            header.DateUpdate.Should().NotBeNull();
            header.StatusChange.Should().Be(AdrStatus.Unknown);
            content.Should().NotBeEmpty();
        });
    }

    [Theory]
    [MemberData(nameof(CultureData.Cultures), MemberType = typeof(CultureData))]
    public async Task ParseAdrHeaderAndContentAsync_WithStatusRejected_ParsesCorrectly(string cultureName)
    {
        await CultureData.WithCultureAsync(cultureName, async () =>
        {
            // Arrange - Sample: "- Rejected (02/04/2026)"
            var filePath = "docs/adr/ADR0001-test.md";
            var fileContent = CreateValidAdrFileContentWithStatusUpdate(AdrStatus.Rejected);
            _fileSystemService.ReadAllLinesAsync(filePath).Returns(fileContent);

            // Act
            var (header, content) = await _adrUtil.ParseAdrHeaderAndContentAsync(
                filePath, _repoConfig, _fileSystemService);

            // Assert
            header.IsValid.Should().BeTrue();
            header.StatusCreate.Should().Be(AdrStatus.Proposed);
            header.StatusUpdate.Should().Be(AdrStatus.Rejected);
            header.DateUpdate.Should().NotBeNull();
            header.StatusChange.Should().Be(AdrStatus.Unknown);
            content.Should().NotBeEmpty();
        });
    }

    [Theory]
    [MemberData(nameof(CultureData.Cultures), MemberType = typeof(CultureData))]
    public async Task ParseAdrHeaderAndContentAsync_WithStatusSuperseded_ParsesFileReference(string cultureName)
    {
        await CultureData.WithCultureAsync(cultureName, async () =>
        {
            // Arrange - Sample: "- Supersede (01/04/2026) : filename.md"
            var filePath = "docs/adr/ADR0001-test.md";
            var fileContent = CreateValidAdrFileContentWithSuperseded();
            _fileSystemService.ReadAllLinesAsync(filePath).Returns(fileContent);

            // Act
            var (header, content) = await _adrUtil.ParseAdrHeaderAndContentAsync(
                filePath, _repoConfig, _fileSystemService);

            // Assert
            header.IsValid.Should().BeTrue();
            header.StatusCreate.Should().Be(AdrStatus.Proposed);
            header.StatusChange.Should().Be(AdrStatus.Superseded);
            header.DateChange.Should().NotBeNull();
            header.FileSuperSedes.Should().Be("ADR0002-new-decision.md");
            content.Should().NotBeEmpty();
        });
    }

    [Theory]
    [MemberData(nameof(CultureData.Cultures), MemberType = typeof(CultureData))]
    public async Task ParseAdrHeaderAndContentAsync_WithAllStatusesFilled_ParsesCorrectly(string cultureName)
    {
        await CultureData.WithCultureAsync(cultureName, async () =>
        {
            // Arrange - Sample with all status lines filled
            var filePath = "docs/adr/ADR0001-test.md";
            var fileContent = CreateValidAdrFileContentAllStatusesFilled();
            _fileSystemService.ReadAllLinesAsync(filePath).Returns(fileContent);

            // Act
            var (header, content) = await _adrUtil.ParseAdrHeaderAndContentAsync(
                filePath, _repoConfig, _fileSystemService);

            // Assert
            header.IsValid.Should().BeTrue();
            header.StatusCreate.Should().Be(AdrStatus.Proposed);
            header.DateCreate.Should().NotBeNull();
            header.StatusUpdate.Should().Be(AdrStatus.Accepted);
            header.DateUpdate.Should().NotBeNull();
            header.StatusChange.Should().Be(AdrStatus.Superseded);
            header.DateChange.Should().NotBeNull();
            header.FileSuperSedes.Should().NotBeEmpty();
            content.Should().NotBeEmpty();
        });
    }

    public static TheoryData<string, string[], DateTime, DateTime, DateTime> CompleteSampleData => new()
    {
        // en-US: "01/04/2026" is parsed as January 4, 2026
        { "en-US", CreateCompleteSampleEnUs(), new DateTime(2026, 1, 4), new DateTime(2026, 4, 2), new DateTime(2026, 1, 4) },
        // pt-BR: "01/04/2026" is parsed as April 1, 2026
        { "pt-BR", CreateCompleteSamplePtBr(), new DateTime(2026, 4, 1), new DateTime(2026, 4, 2), new DateTime(2026, 4, 1) },
    };

    [Theory]
    [MemberData(nameof(CompleteSampleData))]
    public async Task ParseAdrHeaderAndContentAsync_WithDomainScopeAndAllStatuses_ParsesCompleteSample(
        string cultureName, string[] fileContent, DateTime expectedCreate, DateTime expectedUpdate, DateTime expectedChange)
    {
        await CultureData.WithCultureAsync(cultureName, async () =>
        {
            // Arrange - Complete sample with culture-specific date format
            var filePath = "docs/adr/ADR0001-test.md";
            _fileSystemService.ReadAllLinesAsync(filePath).Returns(fileContent);

            // Act
            var (header, content) = await _adrUtil.ParseAdrHeaderAndContentAsync(
                filePath, _repoConfig, _fileSystemService);

            // Assert
            header.IsValid.Should().BeTrue();
            header.Title.Should().Be("Teste Exemplo");
            header.Version.Should().Be(1);
            header.Revision.Should().Be(1);
            header.Scope.Should().Be("MyDomain");
            header.Domain.Should().Be("MyScope");
            header.StatusCreate.Should().Be(AdrStatus.Proposed);
            header.DateCreate.Should().Be(expectedCreate);
            header.StatusUpdate.Should().Be(AdrStatus.Accepted);
            header.DateUpdate.Should().Be(expectedUpdate);
            header.StatusChange.Should().Be(AdrStatus.Superseded);
            header.DateChange.Should().Be(expectedChange);
            header.FileSuperSedes.Should().Be("filename.md");
            content.Should().NotBeEmpty();
        });
    }

    [Fact]
    public async Task ParseAdrHeaderAndContentAsync_WhenEmptyFile_ReturnsError()
    {
        // Arrange
        var filePath = "docs/adr/empty.md";
        _fileSystemService.ReadAllLinesAsync(filePath).Returns([]);

        // Act
        var (header, _) = await _adrUtil.ParseAdrHeaderAndContentAsync(
            filePath, _repoConfig, _fileSystemService);

        // Assert
        header.IsValid.Should().BeFalse();
        header.ErrorMessage.Should().NotBeEmpty();
    }

    [Fact]
    public async Task ParseAdrHeaderAndContentAsync_WhenFileTooShort_ReturnsError()
    {
        // Arrange
        var filePath = "docs/adr/short.md";
        _fileSystemService.ReadAllLinesAsync(filePath).Returns([
            "# Title",
            "Some content"
        ]);

        // Act
        var (header, _) = await _adrUtil.ParseAdrHeaderAndContentAsync(
            filePath, _repoConfig, _fileSystemService);

        // Assert
        header.IsValid.Should().BeFalse();
        header.ErrorMessage.Should().NotBeEmpty();
    }

    [Fact]
    public async Task ParseAdrHeaderAndContentAsync_WhenInvalidDisclaimer_ReturnsError()
    {
        // Arrange
        var filePath = "docs/adr/invalid.md";
        var fileContent = CreateInvalidAdrFileContent_NoDisclaimer();
        _fileSystemService.ReadAllLinesAsync(filePath).Returns(fileContent);

        // Act
        var (header, _) = await _adrUtil.ParseAdrHeaderAndContentAsync(
            filePath, _repoConfig, _fileSystemService);

        // Assert
        header.IsValid.Should().BeFalse();
        header.ErrorMessage.Should().NotBeEmpty();
    }

    [Fact]
    public async Task ParseAdrHeaderAndContentAsync_WhenInvalidSeparator_ReturnsError()
    {
        // Arrange
        var filePath = "docs/adr/invalid.md";
        var fileContent = CreateInvalidAdrFileContent_InvalidSeparator();
        _fileSystemService.ReadAllLinesAsync(filePath).Returns(fileContent);

        // Act
        var (header, _) = await _adrUtil.ParseAdrHeaderAndContentAsync(
            filePath, _repoConfig, _fileSystemService);

        // Assert
        header.IsValid.Should().BeFalse();
        header.ErrorMessage.Should().NotBeEmpty();
    }

    #endregion

    #region FromJson Tests

    [Fact]
    public void FromJson_WhenValidJson_CreatesRepoConfig()
    {
        // Arrange
        var json = @"{
            ""prefix"": ""ADR"",
            ""lenseq"": 4,
            ""lenversion"": 2,
            ""lenrevision"": 2,
            ""separator"": ""-"",
            ""statusnew"": ""Proposed"",
            ""statusaccepted"": ""Accepted""
        }";

        // Act
        var config = _adrUtil.FromJson(json, "template", "folder");

        // Assert
        config.Should().NotBeNull();
        config.Prefix.Should().Be("ADR");
        config.LenSeq.Should().Be(4);
        config.LenVersion.Should().Be(2);
    }

    [Fact]
    public void FromJson_WhenNullJson_ThrowsArgumentNullException()
    {
        // Act
        var act = () => _adrUtil.FromJson(null!, "template", "folder");

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void FromJson_WhenEmptyJson_ThrowsArgumentNullException()
    {
        // Act
        var act = () => _adrUtil.FromJson("", "template", "folder");

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    #endregion

    #region ReadAllAdrByNumber Tests

    [Fact]
    public async Task ReadAllAdrByNumber_WhenDirectoryNotExists_ThrowsDirectoryNotFoundException()
    {
        // Arrange
        var sequence = 1;
        var directoryPath = "nonexistent";
        _fileSystemService.DirectoryExists(directoryPath).Returns(false);

        // Act
        var act = async () => await _adrUtil.ReadAllAdrByNumber(
            sequence, _fileSystemService, directoryPath, _repoConfig);

        // Assert
        await act.Should().ThrowAsync<DirectoryNotFoundException>();
    }

    [Fact]
    public async Task ReadAllAdrByNumber_WhenNullConfig_ThrowsArgumentNullException()
    {
        // Arrange
        var sequence = 1;
        var directoryPath = "docs/adr";

        // Act
        var act = async () => await _adrUtil.ReadAllAdrByNumber(
            sequence, _fileSystemService, directoryPath, null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    #endregion

    #region ReadLatestAdrFiles Tests

    [Fact]
    public async Task ReadLatestAdrFiles_WhenDirectoryEmpty_ReturnsEmptyArray()
    {
        // Arrange
        var directoryPath = "docs/adr";
        _fileSystemService.DirectoryExists(directoryPath).Returns(true);
        _fileSystemService.GetFiles(directoryPath, "*.md", SearchOption.AllDirectories)
            .Returns([]);

        // Act
        var result = await _adrUtil.ReadLatestAdrFiles(
            _fileSystemService, directoryPath, _repoConfig);

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region ReadAllAdrFiles Tests

    [Fact]
    public async Task ReadAllAdrFiles_WhenDirectoryEmpty_ReturnsEmptyArray()
    {
        // Arrange
        var directoryPath = "docs/adr";
        _fileSystemService.DirectoryExists(directoryPath).Returns(true);
        _fileSystemService.GetFiles(directoryPath, "*.md", SearchOption.AllDirectories)
            .Returns([]);

        // Act
        var result = await _adrUtil.ReadAllAdrFiles(
            _fileSystemService, directoryPath, _repoConfig);

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region GetFileByUniqueTitle Tests

    [Fact]
    public async Task GetFileByUniqueTitle_WhenTitleNotExists_ReturnsEmpty()
    {
        // Arrange
        var title = "nonexistent";
        var domain = "api";
        var directoryPath = "docs/adr";
        _fileSystemService.DirectoryExists(directoryPath).Returns(true);
        _fileSystemService.GetFiles(directoryPath, "*.md", SearchOption.AllDirectories)
            .Returns([]);

        // Act
        var result = await _adrUtil.GetFileByUniqueTitle(
            title, domain, _fileSystemService, directoryPath, _repoConfig);

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region GetNextNumber Tests

    [Fact]
    public async Task GetNextNumber_WhenNoFilesExist_ReturnsOne()
    {
        // Arrange
        var directoryPath = "docs/adr";
        _fileSystemService.DirectoryExists(directoryPath).Returns(true);
        _fileSystemService.GetFiles(directoryPath, "*.md", SearchOption.AllDirectories)
            .Returns([]);

        // Act
        var result = await _adrUtil.GetNextNumber(
            _fileSystemService, directoryPath, _repoConfig);

        // Assert
        result.Should().Be(1);
    }

    #endregion

    #region GetLatestADRSequence Tests

    [Fact]
    public async Task GetLatestADRSequence_WhenNoFilesExist_ReturnsNull()
    {
        // Arrange
        var sequence = 1;
        var directoryPath = "docs/adr";
        _fileSystemService.DirectoryExists(directoryPath).Returns(true);
        _fileSystemService.GetFiles(directoryPath, "ADR0001*.md")
            .Returns([]);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _adrUtil.GetLatestADRSequence(sequence, _fileSystemService, directoryPath, _repoConfig));
    }

    #endregion

    #region GetDomains Tests

    [Fact]
    public async Task GetDomains_WhenNoDomainsExist_ReturnsEmpty()
    {
        // Arrange
        var directoryPath = "docs/adr";
        _fileSystemService.DirectoryExists(directoryPath).Returns(true);
        _fileSystemService.GetFiles(directoryPath, "*.md", SearchOption.AllDirectories)
            .Returns([]);

        // Act
        var result = await _adrUtil.GetDomains(
            _fileSystemService, directoryPath, _repoConfig);

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region ParseFileName Tests

    [Fact]
    public async Task ParseFileName_WhenValidFileName_ParsesCorrectly()
    {
        // Arrange
        var filePath = "docs/adr/ADR0001-test-V01R01.md";
        var fileContent = CreateValidAdrFileContent();
        _fileSystemService.ReadAllLinesAsync(filePath).Returns(fileContent);

        // Act
        var result = await _adrUtil.ParseFileName(filePath, _repoConfig, _fileSystemService);

        // Assert
        result.IsValid.Should().BeTrue($"ErrorMessage: {result.ErrorMessage}");
        result.Number.Should().Be(1);
        result.Title.Should().Be("test");
        result.Prefix.Should().Be("ADR");
        result.Version.Should().Be(1);
        result.Revision.Should().Be(1);
    }

    [Fact]
    public async Task ParseFileName_WhenEmptyPath_ReturnsError()
    {
        // Act
        var result = await _adrUtil.ParseFileName("", _repoConfig, _fileSystemService);

        // Assert
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().NotBeEmpty();
    }

    [Fact]
    public async Task ParseFileName_WhenNoMdExtension_ReturnsError()
    {
        // Arrange
        var filePath = "docs/adr/ADR0001-test.txt";

        // Act
        var result = await _adrUtil.ParseFileName(filePath, _repoConfig, _fileSystemService);

        // Assert
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().NotBeEmpty();
    }

    [Fact]
    public async Task ParseFileName_WhenInvalidFormat_ReturnsError()
    {
        // Arrange
        var filePath = "docs/adr/invalid.md";
        var fileContent = CreateValidAdrFileContent();
        _fileSystemService.ReadAllLinesAsync(filePath).Returns(fileContent);

        // Act
        var result = await _adrUtil.ParseFileName(filePath, _repoConfig, _fileSystemService);

        // Assert
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().NotBeEmpty();
    }

    [Fact]
    public async Task ParseFileName_WhenFileNameContainsSupersede_ParsesSupersededValue()
    {
        // Arrange
        var filePath = "docs/adr/ADR0001-test-V01R01-SUP0002.md";
        var fileContent = CreateValidAdrFileContent();
        _fileSystemService.ReadAllLinesAsync(filePath).Returns(fileContent);

        // Act
        var result = await _adrUtil.ParseFileName(filePath, _repoConfig, _fileSystemService);

        // Assert
        result.IsValid.Should().BeTrue($"ErrorMessage: {result.ErrorMessage}");
        result.SupersededValue.Should().Be(2);
    }

    [Fact]
    public async Task ParseFileName_WhenSupersedeHasNoNumber_ReturnsError()
    {
        // Arrange
        var filePath = "docs/adr/ADR0001-test-V01R01-SUP.md";
        var fileContent = CreateValidAdrFileContent();
        _fileSystemService.ReadAllLinesAsync(filePath).Returns(fileContent);

        // Act
        var result = await _adrUtil.ParseFileName(filePath, _repoConfig, _fileSystemService);

        // Assert
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().NotBeEmpty();
    }

    #endregion

    #region Helper Methods

    private static string[] CreateValidAdrFileContent()
    {
        return [
            "###### (Do not remove this template. It is used and must remain unchanged to ensure consistency in documentation.)",
            "##### Version: 01",
            "##### Revision: 01",
            "##### -",
            "##### Status:",
            "- Proposed (2024-01-01)",
            "- \\-",
            "- \\-",
            "# Test ADR",
            "---",
            "## Context",
            "This is the context of the ADR.",
            "",
            "## Decision",
            "This is the decision made.",
            "",
            "## Consequences",
            "These are the consequences."
        ];
    }

    private static string[] CreateValidAdrFileContentWithVersion(int number, int version)
    {
        return [
            "###### (Do not remove this template. It is used and must remain unchanged to ensure consistency in documentation.)",
            $"##### Version: {version:D2}",
            "##### Revision: 01",
            "##### -",
            "##### Status:",
            "- Proposed (2024-01-01)",
            "- \\-",
            "- \\-",
            $"# Test ADR {number}",
            "---",
            "## Context",
            "This is the context of the ADR.",
            "",
            "## Decision",
            "This is the decision made.",
            "",
            "## Consequences",
            "These are the consequences."
        ];
    }

    private static string[] CreateValidAdrFileContentWithDomain(string domain)
    {
        return [
            "###### (Do not remove this template. It is used and must remain unchanged to ensure consistency in documentation.)",
            "##### Version: 01",
            "##### Revision: 01",
            $"##### API: {domain}",
            "##### Status:",
            "- Proposed (2024-01-01)",
            "- \\-",
            "- \\-",
            "# Test ADR",
            "---",
            "## Context",
            "This is the context of the ADR.",
            "",
            "## Decision",
            "This is the decision made.",
            "",
            "## Consequences",
            "These are the consequences."
        ];
    }

    private static AdrPlusRepoConfig CreateRepoConfigWithScope()
    {
        return new AdrPlusRepoConfig(
            defaultTemplate: "# ADR Template",
            defaultFolder: "docs/adr")
        {
            Prefix = "ADR",
            LenSeq = 4,
            LenVersion = 2,
            LenRevision = 2,
            LenScope = 3,
            Separator = '-',
            StatusNew = "Proposed",
            StatusAcc = "Accepted",
            StatusRej = "Rejected",
            StatusSup = "Superseded",
            Scopes = "API;UI;DB",
            SkipDomain = string.Empty,
            FolderByScope = false
        };
    }

    private static AdrPlusRepoConfig CreateRepoConfigNoRevision()
    {
        return new AdrPlusRepoConfig(
            defaultTemplate: "# ADR Template",
            defaultFolder: "docs/adr")
        {
            Prefix = "ADR",
            LenSeq = 4,
            LenVersion = 2,
            LenRevision = 0,
            LenScope = 0,
            Separator = '-',
            StatusNew = "Proposed",
            StatusAcc = "Accepted",
            StatusRej = "Rejected",
            StatusSup = "Superseded",
            Scopes = string.Empty,
            SkipDomain = string.Empty,
            FolderByScope = false
        };
    }

    private static AdrPlusRepoConfig CreateRepoConfigWithskipdomain()
    {
        return new AdrPlusRepoConfig(
            defaultTemplate: "# ADR Template",
            defaultFolder: "docs/adr")
        {
            Prefix = "ADR",
            LenSeq = 4,
            LenVersion = 2,
            LenRevision = 2,
            LenScope = 3,
            Separator = '-',
            StatusNew = "Proposed",
            StatusAcc = "Accepted",
            StatusRej = "Rejected",
            StatusSup = "Superseded",
            Scopes = "API;UI;DB",
            SkipDomain = "DB",
            FolderByScope = false
        };
    }

    private static string[] CreateValidAdrFileContentWithScopeAndDomain(string scope, string domain)
    {
        return [
            "###### (Do not remove this template. It is used and must remain unchanged to ensure consistency in documentation.)",
            "##### Version: 01",
            "##### Revision: 01",
            $"##### {scope}: {domain}",
            "##### Status:",
            "- Proposed (2024-01-01)",
            "- \\-",
            "- \\-",
            "# Test ADR",
            "---",
            "## Context",
            "This is the context of the ADR.",
            "",
            "## Decision",
            "This is the decision made.",
            "",
            "## Consequences",
            "These are the consequences."
        ];
    }

    private static string[] CreateValidAdrFileContentNoRevision()
    {
        return [
            "###### (Do not remove this template. It is used and must remain unchanged to ensure consistency in documentation.)",
            "##### Version: 01",
            "##### Revision: \\-",
            "##### -",
            "##### Status:",
            "- Proposed (2024-01-01)",
            "- \\-",
            "- \\-",
            "# Test ADR",
            "---",
            "## Context",
            "This is the context of the ADR.",
            "",
            "## Decision",
            "This is the decision made.",
            "",
            "## Consequences",
            "These are the consequences."
        ];
    }

    private static string[] CreateValidAdrFileContentWithScopeOnly(string scope)
    {
        return [
            "###### (Do not remove this template. It is used and must remain unchanged to ensure consistency in documentation.)",
            "##### Version: 01",
            "##### Revision: 01",
            $"##### {scope}",
            "##### Status:",
            "- Proposed (2024-01-01)",
            "- \\-",
            "- \\-",
            "# Test ADR",
            "---",
            "## Context",
            "This is the context of the ADR.",
            "",
            "## Decision",
            "This is the decision made.",
            "",
            "## Consequences",
            "These are the consequences."
        ];
    }

    private static string[] CreateValidAdrFileContentWithStatusUpdate()
    {
        return CreateValidAdrFileContentWithStatusUpdate(AdrStatus.Accepted);
    }

    private static string[] CreateValidAdrFileContentWithStatusUpdate(AdrStatus status)
    {
        var statusText = status == AdrStatus.Accepted ? "Accepted" : "Rejected";
        return [
            "###### (Do not remove this template. It is used and must remain unchanged to ensure consistency in documentation.)",
            "##### Version: 01",
            "##### Revision: 01",
            "##### -",
            "##### Status:",
            "- Proposed (2024-01-01)",
            $"- {statusText} (2024-02-15)",
            "- \\-",
            "# Test ADR",
            "---",
            "## Context",
            "This is the context of the ADR.",
            "",
            "## Decision",
            "This is the decision made.",
            "",
            "## Consequences",
            "These are the consequences."
        ];
    }

    private static string[] CreateValidAdrFileContentWithStatusChange()
    {
        return [
            "###### (Do not remove this template. It is used and must remain unchanged to ensure consistency in documentation.)",
            "##### Version: 01",
            "##### Revision: 01",
            "##### -",
            "##### Status:",
            "- Proposed (2024-01-01)",
            "- \\-",
            "- Rejected (2024-03-20)",
            "# Test ADR",
            "---",
            "## Context",
            "This is the context of the ADR.",
            "",
            "## Decision",
            "This is the decision made.",
            "",
            "## Consequences",
            "These are the consequences."
        ];
    }

    private static string[] CreateValidAdrFileContentWithSuperseded()
    {
        return [
            "###### (Do not remove this template. It is used and must remain unchanged to ensure consistency in documentation.)",
            "##### Version: 01",
            "##### Revision: 01",
            "##### -",
            "##### Status:",
            "- Proposed (2024-01-01)",
            "- \\-",
            "- Superseded (2024-04-10): ADR0002-new-decision.md",
            "# Test ADR",
            "---",
            "## Context",
            "This is the context of the ADR.",
            "",
            "## Decision",
            "This is the decision made.",
            "",
            "## Consequences",
            "These are the consequences."
        ];
    }

    private static string[] CreateValidAdrFileContentWithDomainOnly(string domain)
    {
        return [
            "###### (Do not remove this template. It is used and must remain unchanged to ensure consistency in documentation.)",
            "##### Version: 01",
            "##### Revision: 01",
            $"##### {domain}",
            "##### Status:",
            "- Proposed (2024-01-01)",
            "- \\-",
            "- \\-",
            "# Test ADR",
            "---",
            "## Context",
            "This is the context of the ADR."
        ];
    }

    private static string[] CreateValidAdrFileContentProposedOnly()
    {
        return [
            "###### (Do not remove this template. It is used and must remain unchanged to ensure consistency in documentation.)",
            "##### Version: 01",
            "##### Revision: 01",
            "##### -",
            "##### Status:",
            "- Proposed (2024-01-01)",
            "- \\-",
            "- \\-",
            "# Test ADR",
            "---",
            "## Context",
            "This is the context of the ADR."
        ];
    }

    private static string[] CreateValidAdrFileContentAllStatusesFilled()
    {
        return [
            "###### (Do not remove this template. It is used and must remain unchanged to ensure consistency in documentation.)",
            "##### Version: 01",
            "##### Revision: 01",
            "##### -",
            "##### Status:",
            "- Proposed (2024-01-01)",
            "- Accepted (2024-02-15)",
            "- Superseded (2024-04-10): ADR0002-new-decision.md",
            "# Test ADR",
            "---",
            "## Context",
            "This is the context of the ADR."
        ];
    }

    private static string[] CreateCompleteSampleEnUs()
    {
        return [
            "###### (Do not remove this template. It is used and must remain unchanged to ensure consistency in documentation.)",
            "##### Version: 01",
            "##### Revision: 01",
            "##### MyDomain: MyScope",
            "##### Status:",
            "- Proposed (01/04/2026)",   // en-US: January 4
            "- Accepted (04/02/2026)",   // en-US: April 2
            "- Superseded (01/04/2026): filename.md", // en-US: January 4
            "# Teste Exemplo",
            "---",
            "## Context",
            "This is the context of the ADR."
        ];
    }

    private static string[] CreateCompleteSamplePtBr()
    {
        return [
            "###### (Do not remove this template. It is used and must remain unchanged to ensure consistency in documentation.)",
            "##### Version: 01",
            "##### Revision: 01",
            "##### MyDomain: MyScope",
            "##### Status:",
            "- Proposed (01/04/2026)",   // pt-BR: April 1
            "- Accepted (02/04/2026)",   // pt-BR: April 2
            "- Superseded (01/04/2026): filename.md", // pt-BR: April 1
            "# Teste Exemplo",
            "---",
            "## Context",
            "This is the context of the ADR."
        ];
    }

    private static string[] CreateInvalidAdrFileContent_NoDisclaimer()
    {
        return [
            "# Missing disclaimer marker",
            "##### Version: 01",
            "##### Revision: 01",
            "##### -",
            "##### Status:",
            "- Proposed (2024-01-01)",
            "- \\-",
            "- \\-",
            "# Test ADR",
            "---"
        ];
    }

    private static string[] CreateInvalidAdrFileContent_InvalidSeparator()
    {
        return [
            "###### (Do not remove this template. It is used and must remain unchanged to ensure consistency in documentation.)",
            "##### Version: 01",
            "##### Revision: 01",
            "##### -",
            "##### Status:",
            "- Proposed (2024-01-01)",
            "- \\-",
            "- \\-",
            "# Test ADR",
            "===",  // Invalid separator - should be "---"
            "## Context"
        ];
    }

    #endregion
}
