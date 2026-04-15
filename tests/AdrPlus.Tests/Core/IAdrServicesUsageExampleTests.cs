// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the AdrPlus project under MIT license
// ***************************************************************************************

using AdrPlus.Core;
using AdrPlus.Domain;
using AdrPlus.Infrastructure.FileSystem;

namespace AdrPlus.Tests.Core;

/// <summary>
/// Demonstrates how to use IAdrServices interface with mocking.
/// These tests show the correct pattern for mocking the interface instead of using static methods.
/// </summary>
public class IAdrServicesUsageExampleTests
{
    private readonly IAdrServices _adrServices;
    private readonly IFileSystemService _fileSystemService;
    private readonly AdrPlusRepoConfig _repoConfig;

    public IAdrServicesUsageExampleTests()
    {
        // Mock the IAdrServices interface
        _adrServices = Substitute.For<IAdrServices>();
        _fileSystemService = Substitute.For<IFileSystemService>();
        _repoConfig = CreateDefaultRepoConfig();
    }

    private static AdrPlusRepoConfig CreateDefaultRepoConfig()
    {
        return new AdrPlusRepoConfig(
            defaultTemplate: "# ADR Template",
            defaultFolder: "docs/adr")
        {
            Prefix = "ADR",
            LenSeq = 4
        };
    }

    [Fact]
    public async Task Example_MockingIAdrServices_StatusUpdateAsync()
    {
        // Arrange - Configure the mock to return expected values
        var filePath = "docs/adr/ADR0001-test.md";
        var status = AdrStatus.Accepted;
        var date = DateTime.UtcNow;
        var cancellationToken = CancellationToken.None;

        _adrServices
            .StatusUpdateAdrAsync(filePath, status, date, _repoConfig, _fileSystemService, cancellationToken)
            .Returns((true, string.Empty));

        // Act
        var (isValid, error) = await _adrServices.StatusUpdateAdrAsync(
            filePath, status, date, _repoConfig, _fileSystemService, cancellationToken);

        // Assert
        isValid.Should().BeTrue();
        error.Should().BeEmpty();
        await _adrServices.Received(1).StatusUpdateAdrAsync(
            filePath, status, date, _repoConfig, _fileSystemService, cancellationToken);
    }

    [Fact]
    public async Task Example_MockingIAdrServices_GetNextNumber()
    {
        // Arrange
        var directoryPath = "docs/adr";

        _adrServices
            .GetNextNumber(_fileSystemService, directoryPath, _repoConfig)
            .Returns(5);

        // Act
        var nextNumber = await _adrServices.GetNextNumber(_fileSystemService, directoryPath, _repoConfig);

        // Assert
        nextNumber.Should().Be(5);
        await _adrServices.Received(1).GetNextNumber(_fileSystemService, directoryPath, _repoConfig);
    }

    [Fact]
    public async Task Example_MockingIAdrServices_ReadAllAdrFiles()
    {
        // Arrange
        var directoryPath = "docs/adr";
        var expectedFiles = new[]
        {
            new AdrFileNameComponents { Number = 1, Title = "First ADR", IsValid = true },
            new AdrFileNameComponents { Number = 2, Title = "Second ADR", IsValid = true }
        };

        _adrServices
            .ReadAllAdrFiles(_fileSystemService, directoryPath, _repoConfig)
            .Returns(expectedFiles);

        // Act
        var files = await _adrServices.ReadAllAdrFiles(_fileSystemService, directoryPath, _repoConfig);

        // Assert
        files.Should().HaveCount(2);
        files.Should().BeEquivalentTo(expectedFiles);
    }

    [Fact]
    public async Task Example_MockingIAdrServices_GetDomains()
    {
        // Arrange
        var directoryPath = "docs/adr";
        var expectedDomains = new[] { "auth", "payment", "orders" };

        _adrServices
            .GetDomains(_fileSystemService, directoryPath, _repoConfig)
            .Returns(expectedDomains);

        // Act
        var domains = await _adrServices.GetDomains(_fileSystemService, directoryPath, _repoConfig);

        // Assert
        domains.Should().HaveCount(3);
        domains.Should().Contain(expectedDomains);
    }

    [Fact]
    public void Example_MockingIAdrServices_FromJson()
    {
        // Arrange
        var json = @"{""prefix"": ""ADR"", ""lenseq"": 4}";
        var template = "# Template";
        var folder = "docs/adr";

        var expectedConfig = new AdrPlusRepoConfig(template, folder)
        {
            Prefix = "ADR",
            LenSeq = 4
        };

        _adrServices
            .FromJson(json, template, folder)
            .Returns(expectedConfig);

        // Act
        var config = _adrServices.FromJson(json, template, folder);

        // Assert
        config.Should().NotBeNull();
        config.Prefix.Should().Be("ADR");
        config.LenSeq.Should().Be(4);
    }

    [Fact]
    public async Task Example_MockingIAdrServices_ParseFileName()
    {
        // Arrange
        var filePath = "docs/adr/ADR0001-my-decision.md";
        var expectedComponent = new AdrFileNameComponents
        {
            FileName = filePath,
            Number = 1,
            Title = "my-decision",
            Prefix = "ADR",
            IsValid = true,
            Header = new AdrHeader { IsValid = true, Title = "My Decision" }
        };

        _adrServices
            .ParseFileName(filePath, _repoConfig, _fileSystemService)
            .Returns(expectedComponent);

        // Act
        var component = await _adrServices.ParseFileName(filePath, _repoConfig, _fileSystemService);

        // Assert
        component.Should().NotBeNull();
        component.IsValid.Should().BeTrue();
        component.Number.Should().Be(1);
        component.Title.Should().Be("my-decision");
    }

    [Fact]
    public async Task Example_MockingIAdrServices_StatusChangeSupersedeAsync()
    {
        // Arrange
        var filePath = "docs/adr/ADR0001-old.md";
        var newFileName = "ADR0002-new.md";
        var date = DateTime.UtcNow;
        var cancellationToken = CancellationToken.None;

        _adrServices
            .StatusChangeSupersedeAdrAsync(filePath, newFileName, date, _repoConfig, _fileSystemService, cancellationToken)
            .Returns((true, string.Empty));

        // Act
        var (isValid, error) = await _adrServices.StatusChangeSupersedeAdrAsync(
            filePath, newFileName, date, _repoConfig, _fileSystemService, cancellationToken);

        // Assert
        isValid.Should().BeTrue();
        error.Should().BeEmpty();
    }

    [Fact]
    public async Task Example_MockingIAdrServices_ErrorHandling()
    {
        // Arrange - Simulate an error scenario
        var filePath = "docs/adr/invalid.md";
        var status = AdrStatus.Accepted;
        var date = DateTime.UtcNow;
        var cancellationToken = CancellationToken.None;
        var errorMessage = "File not found";

        _adrServices
            .StatusUpdateAdrAsync(filePath, status, date, _repoConfig, _fileSystemService, cancellationToken)
            .Returns((false, errorMessage));

        // Act
        var (isValid, error) = await _adrServices.StatusUpdateAdrAsync(
            filePath, status, date, _repoConfig, _fileSystemService, cancellationToken);

        // Assert
        isValid.Should().BeFalse();
        error.Should().Be(errorMessage);
    }

    [Fact]
    public async Task Example_UsingConcreteImplementation_RealInstance()
    {
        // This shows how to use the real implementation (AdrUtil) 
        // which implements IAdrServices
        var realImplementation = new AdrService();
        var mockFileSystem = Substitute.For<IFileSystemService>();

        mockFileSystem.DirectoryExists(Arg.Any<string>()).Returns(true);
        mockFileSystem.GetFiles(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<SearchOption>())
            .Returns([]);

        // Act
        var nextNumber = await realImplementation.GetNextNumber(mockFileSystem, "docs/adr", _repoConfig);

        // Assert
        nextNumber.Should().Be(1); // No files exist, so next number is 1
    }
}
