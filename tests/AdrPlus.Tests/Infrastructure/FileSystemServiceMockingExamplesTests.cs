// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the AdrPlus project under MIT license
// ***************************************************************************************

using AdrPlus.Infrastructure.FileSystem;

namespace AdrPlus.Tests.Infrastructure;

/// <summary>
/// Examples of how to mock IFileSystemService for unit testing.
/// These tests demonstrate various mocking patterns using NSubstitute.
/// </summary>
public class FileSystemServiceMockingExamplesTests
{
    #region Basic File Operations

    [Fact]
    public void MockFileExists_ReturnsTrue()
    {
        // Arrange
        var mockFileSystem = Substitute.For<IFileSystemService>();
        var filePath = "C:\\project\\test.txt";
        mockFileSystem.FileExists(filePath).Returns(true);

        // Act
        var result = mockFileSystem.FileExists(filePath);

        // Assert
        result.Should().BeTrue();
        mockFileSystem.Received(1).FileExists(filePath);
    }

    [Fact]
    public void MockFileExists_ReturnsFalse()
    {
        // Arrange
        var mockFileSystem = Substitute.For<IFileSystemService>();
        var filePath = "C:\\project\\nonexistent.txt";
        mockFileSystem.FileExists(filePath).Returns(false);

        // Act
        var result = mockFileSystem.FileExists(filePath);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void MockFileExists_WithAnyPath_ReturnsTrue()
    {
        // Arrange
        var mockFileSystem = Substitute.For<IFileSystemService>();
        mockFileSystem.FileExists(Arg.Any<string>()).Returns(true);

        // Act
        var result = mockFileSystem.FileExists("any-path.txt");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task MockReadAllTextAsync_ReturnsContent()
    {
        // Arrange
        var mockFileSystem = Substitute.For<IFileSystemService>();
        var filePath = "C:\\project\\test.txt";
        var expectedContent = "Test content";

        mockFileSystem.ReadAllTextAsync(filePath, Arg.Any<CancellationToken>())
            .Returns(expectedContent);

        // Act
        var result = await mockFileSystem.ReadAllTextAsync(filePath, CancellationToken.None);

        // Assert
        result.Should().Be(expectedContent);
        await mockFileSystem.Received(1).ReadAllTextAsync(filePath, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task MockReadAllLinesAsync_ReturnsLines()
    {
        // Arrange
        var mockFileSystem = Substitute.For<IFileSystemService>();
        var filePath = "C:\\project\\test.txt";
        var expectedLines = new[] { "Line 1", "Line 2", "Line 3" };

        mockFileSystem.ReadAllLinesAsync(filePath, Arg.Any<CancellationToken>())
            .Returns(expectedLines);

        // Act
        var result = await mockFileSystem.ReadAllLinesAsync(filePath, CancellationToken.None);

        // Assert
        result.Should().BeEquivalentTo(expectedLines);
        result.Should().HaveCount(3);
    }

    [Fact]
    public async Task MockWriteAllTextAsync_VerifiesCall()
    {
        // Arrange
        var mockFileSystem = Substitute.For<IFileSystemService>();
        var filePath = "C:\\project\\test.txt";
        var content = "Test content";

        // Act
        await mockFileSystem.WriteAllTextAsync(filePath, content, CancellationToken.None);

        // Assert
        await mockFileSystem.Received(1).WriteAllTextAsync(filePath, content, Arg.Any<CancellationToken>());
    }

    #endregion

    #region Directory Operations

    [Fact]
    public void MockDirectoryExists_ReturnsTrue()
    {
        // Arrange
        var mockFileSystem = Substitute.For<IFileSystemService>();
        var dirPath = "C:\\project\\docs";
        mockFileSystem.DirectoryExists(dirPath).Returns(true);

        // Act
        var result = mockFileSystem.DirectoryExists(dirPath);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void MockCreateDirectory_ReturnsFullPath()
    {
        // Arrange
        var mockFileSystem = Substitute.For<IFileSystemService>();
        var dirPath = "C:\\project\\newdir";
        var fullPath = "C:\\project\\newdir";

        mockFileSystem.CreateDirectory(dirPath).Returns(fullPath);

        // Act
        var result = mockFileSystem.CreateDirectory(dirPath);

        // Assert
        result.Should().Be(fullPath);
        mockFileSystem.Received(1).CreateDirectory(dirPath);
    }

    [Fact]
    public void MockGetFullNameDirectory_ReturnsAbsolutePath()
    {
        // Arrange
        var mockFileSystem = Substitute.For<IFileSystemService>();
        var relativePath = "docs\\adr";
        var absolutePath = "C:\\project\\docs\\adr";

        mockFileSystem.GetFullNameDirectory(relativePath).Returns(absolutePath);

        // Act
        var result = mockFileSystem.GetFullNameDirectory(relativePath);

        // Assert
        result.Should().Be(absolutePath);
        result.Should().StartWith("C:\\");
    }

    #endregion

    #region File Enumeration

    [Fact]
    public void MockEnumerateFiles_ReturnsFileList()
    {
        // Arrange
        var mockFileSystem = Substitute.For<IFileSystemService>();
        var dirPath = "C:\\project\\docs";
        var searchPattern = "*.md";
        var expectedFiles = new[]
        {
            "C:\\project\\docs\\ADR-0001.md",
            "C:\\project\\docs\\ADR-0002.md",
            "C:\\project\\docs\\ADR-0003.md"
        };

        mockFileSystem.EnumerateFiles(dirPath, searchPattern).Returns(expectedFiles);

        // Act
        var result = mockFileSystem.EnumerateFiles(dirPath, searchPattern).ToArray();

        // Assert
        result.Should().BeEquivalentTo(expectedFiles);
        result.Should().HaveCount(3);
        result.Should().AllSatisfy(f => f.Should().EndWith(".md"));
    }

    [Fact]
    public void MockGetFiles_ReturnsFileArray()
    {
        // Arrange
        var mockFileSystem = Substitute.For<IFileSystemService>();
        var dirPath = "C:\\project\\docs\\adr";
        var searchPattern = "ADR-*.md";
        var expectedFiles = new[]
        {
            "C:\\project\\docs\\adr\\ADR-0001-Test.md",
            "C:\\project\\docs\\adr\\ADR-0002-Another.md"
        };

        mockFileSystem.GetFiles(dirPath, searchPattern, SearchOption.AllDirectories)
            .Returns(expectedFiles);

        // Act
        var result = mockFileSystem.GetFiles(dirPath, searchPattern, SearchOption.AllDirectories);

        // Assert
        result.Should().BeEquivalentTo(expectedFiles);
        result.Length.Should().Be(2);
    }

    [Fact]
    public void MockGetFiles_WithTopDirectoryOnly_ReturnsFiles()
    {
        // Arrange
        var mockFileSystem = Substitute.For<IFileSystemService>();
        var dirPath = "C:\\project\\docs";
        var searchPattern = "*.md";
        var expectedFiles = new[] { "C:\\project\\docs\\README.md" };

        mockFileSystem.GetFiles(dirPath, searchPattern, SearchOption.TopDirectoryOnly)
            .Returns(expectedFiles);

        // Act
        var result = mockFileSystem.GetFiles(dirPath, searchPattern, SearchOption.TopDirectoryOnly);

        // Assert
        result.Should().ContainSingle();
        result[0].Should().Be("C:\\project\\docs\\README.md");
    }

    [Fact]
    public void MockGetFullNameFile_ReturnsAbsolutePath()
    {
        // Arrange
        var mockFileSystem = Substitute.For<IFileSystemService>();
        var relativePath = "docs\\adr\\test.md";
        var absolutePath = "C:\\project\\docs\\adr\\test.md";

        mockFileSystem.GetFullNameFile(relativePath).Returns(absolutePath);

        // Act
        var result = mockFileSystem.GetFullNameFile(relativePath);

        // Assert
        result.Should().Be(absolutePath);
        Path.IsPathRooted(result).Should().BeTrue();
    }

    #endregion

    #region Drive Operations

    [Fact]
    public void MockGetDrives_ReturnsSingleDrive()
    {
        // Arrange
        var mockFileSystem = Substitute.For<IFileSystemService>();
        var expectedDrives = new[] { "C:\\" };

        mockFileSystem.GetDrives().Returns(expectedDrives);

        // Act
        var result = mockFileSystem.GetDrives();

        // Assert
        result.Should().ContainSingle();
        result[0].Should().Be("C:\\");
    }

    [Fact]
    public void MockGetDrives_ReturnsMultipleDrives()
    {
        // Arrange
        var mockFileSystem = Substitute.For<IFileSystemService>();
        var expectedDrives = new[] { "C:\\", "D:\\", "E:\\" };

        mockFileSystem.GetDrives().Returns(expectedDrives);

        // Act
        var result = mockFileSystem.GetDrives();

        // Assert
        result.Should().HaveCount(3);
        result.Should().Contain("C:\\");
        result.Should().Contain("D:\\");
        result.Should().AllSatisfy(d => d.Should().EndWith("\\"));
    }

    #endregion

    #region History Operations

    [Fact]
    public async Task MockSaveHistoryAsync_VerifiesCall()
    {
        // Arrange
        var mockFileSystem = Substitute.For<IFileSystemService>();
        var fileKey = "test-history";
        var content = new { Name = "Test", Value = 123 };

        // Act
        await mockFileSystem.SaveHistoryAsync(fileKey, content, CancellationToken.None);

        // Assert
        await mockFileSystem.Received(1).SaveHistoryAsync(
            fileKey,
            Arg.Is<object>(x => x == content),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task MockReadHistoryAsync_ReturnsSuccess()
    {
        // Arrange
        var mockFileSystem = Substitute.For<IFileSystemService>();
        var fileKey = "test-history";
        var expectedData = new { Name = "Test", Value = 123 };

        mockFileSystem.ReadHistoryAsync<object>(fileKey, Arg.Any<CancellationToken>())
            .Returns((Success: true, Result: expectedData));

        // Act
        var (Success, Result) = await mockFileSystem.ReadHistoryAsync<object>(fileKey, CancellationToken.None);

        // Assert
        Success.Should().BeTrue();
        Result.Should().Be(expectedData);
    }

    [Fact]
    public async Task MockReadHistoryAsync_ReturnsFailure()
    {
        // Arrange
        var mockFileSystem = Substitute.For<IFileSystemService>();
        var fileKey = "nonexistent-history";

        mockFileSystem.ReadHistoryAsync<object>(fileKey, Arg.Any<CancellationToken>())
            .Returns((Success: false, Result: (object?)null));

        // Act
        var (Success, Result) = await mockFileSystem.ReadHistoryAsync<object>(fileKey, CancellationToken.None);

        // Assert
        Success.Should().BeFalse();
        Result.Should().BeNull();
    }

    [Fact]
    public async Task MockReadHistoryAsync_WithGenericType_ReturnsTypedData()
    {
        // Arrange
        var mockFileSystem = Substitute.For<IFileSystemService>();
        var fileKey = "config-history";
        var expectedConfig = new TestConfig { Setting1 = "value1", Setting2 = 42 };

        mockFileSystem.ReadHistoryAsync<TestConfig>(fileKey, Arg.Any<CancellationToken>())
            .Returns((Success: true, Result: expectedConfig));

        // Act
        var (Success, Result) = await mockFileSystem.ReadHistoryAsync<TestConfig>(fileKey, CancellationToken.None);

        // Assert
        Success.Should().BeTrue();
        Result.Should().NotBeNull();
        Result!.Setting1.Should().Be("value1");
        Result.Setting2.Should().Be(42);
    }

    #endregion

    #region Complex Scenarios

    [Fact]
    public async Task MockCompleteFileWorkflow_CreateReadWrite()
    {
        // Arrange
        var mockFileSystem = Substitute.For<IFileSystemService>();
        var dirPath = "C:\\project\\docs";
        var filePath = "C:\\project\\docs\\test.txt";
        var content = "Initial content";

        mockFileSystem.DirectoryExists(dirPath).Returns(false, true);
        mockFileSystem.CreateDirectory(dirPath).Returns(dirPath);
        mockFileSystem.FileExists(filePath).Returns(false, true);
        mockFileSystem.ReadAllTextAsync(filePath, Arg.Any<CancellationToken>())
            .Returns(content);

        // Act - Create directory
        var dirExists = mockFileSystem.DirectoryExists(dirPath);
        dirExists.Should().BeFalse();

        var createdDir = mockFileSystem.CreateDirectory(dirPath);
        createdDir.Should().Be(dirPath);

        var dirExistsAfter = mockFileSystem.DirectoryExists(dirPath);
        dirExistsAfter.Should().BeTrue();

        // Act - Write file
        var fileExists = mockFileSystem.FileExists(filePath);
        fileExists.Should().BeFalse();

        await mockFileSystem.WriteAllTextAsync(filePath, content, CancellationToken.None);

        var fileExistsAfter = mockFileSystem.FileExists(filePath);
        fileExistsAfter.Should().BeTrue();

        // Act - Read file
        var readContent = await mockFileSystem.ReadAllTextAsync(filePath, CancellationToken.None);
        readContent.Should().Be(content);

        // Assert
        mockFileSystem.Received(2).DirectoryExists(dirPath);
        mockFileSystem.Received(1).CreateDirectory(dirPath);
        mockFileSystem.Received(2).FileExists(filePath);
        await mockFileSystem.Received(1).WriteAllTextAsync(filePath, content, Arg.Any<CancellationToken>());
        await mockFileSystem.Received(1).ReadAllTextAsync(filePath, Arg.Any<CancellationToken>());
    }

    [Fact]
    public void MockMultipleFilePatterns_ReturnsFilteredResults()
    {
        // Arrange
        var mockFileSystem = Substitute.For<IFileSystemService>();
        var docsPath = "C:\\project\\docs";

        mockFileSystem.GetFiles(docsPath, "*.md", Arg.Any<SearchOption>())
            .Returns(["file1.md", "file2.md"]);

        mockFileSystem.GetFiles(docsPath, "*.txt", Arg.Any<SearchOption>())
            .Returns(["file3.txt"]);

        mockFileSystem.GetFiles(docsPath, "ADR-*.md", Arg.Any<SearchOption>())
            .Returns(["ADR-0001.md", "ADR-0002.md"]);

        // Act
        var mdFiles = mockFileSystem.GetFiles(docsPath, "*.md", SearchOption.AllDirectories);
        var txtFiles = mockFileSystem.GetFiles(docsPath, "*.txt", SearchOption.AllDirectories);
        var adrFiles = mockFileSystem.GetFiles(docsPath, "ADR-*.md", SearchOption.AllDirectories);

        // Assert
        mdFiles.Should().HaveCount(2);
        txtFiles.Should().ContainSingle();
        adrFiles.Should().HaveCount(2);
        adrFiles.Should().AllSatisfy(f => f.Should().StartWith("ADR-"));
    }

    [Fact]
    public async Task MockFileOperation_ThrowsException()
    {
        // Arrange
        var mockFileSystem = Substitute.For<IFileSystemService>();
        var filePath = "C:\\protected\\file.txt";

        mockFileSystem.ReadAllTextAsync(filePath, Arg.Any<CancellationToken>())
            .Returns<string>(x => throw new UnauthorizedAccessException("Access denied"));

        // Act
        var act = async () => await mockFileSystem.ReadAllTextAsync(filePath, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Access denied");
    }

    [Fact]
    public void MockConditionalFileExists_BasedOnPath()
    {
        // Arrange
        var mockFileSystem = Substitute.For<IFileSystemService>();

        mockFileSystem.FileExists(Arg.Is<string>(path => path.EndsWith(".md")))
            .Returns(true);

        mockFileSystem.FileExists(Arg.Is<string>(path => !path.EndsWith(".md")))
            .Returns(false);

        // Act & Assert
        mockFileSystem.FileExists("test.md").Should().BeTrue();
        mockFileSystem.FileExists("test.txt").Should().BeFalse();
        mockFileSystem.FileExists("ADR-0001.md").Should().BeTrue();
        mockFileSystem.FileExists("config.json").Should().BeFalse();
    }

    #endregion

    #region Helper Classes

    private class TestConfig
    {
        public string Setting1 { get; set; } = string.Empty;
        public int Setting2 { get; set; }
    }

    #endregion
}
