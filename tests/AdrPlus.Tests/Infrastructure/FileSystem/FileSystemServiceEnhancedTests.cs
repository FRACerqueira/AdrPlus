// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the AdrPlus project under MIT license
// ***************************************************************************************

using AdrPlus.Infrastructure.FileSystem;

namespace AdrPlus.Tests.Infrastructure.FileSystem;

/// <summary>
/// Unit tests for FileSystemService enhancements.
/// Tests demonstrate cross-platform file operations and edge cases using real file operations.
/// </summary>
public class FileSystemServiceEnhancedTests
{
    private readonly FileSystemService _fileSystemService;
    private readonly string _testDirectory;

    public FileSystemServiceEnhancedTests()
    {
        _fileSystemService = new FileSystemService();
        _testDirectory = Path.Combine(Path.GetTempPath(), $"AdrPlusTestsEnhanced_{Guid.NewGuid():N}");
    }

    #region Directory Handling Tests

    [Fact]
    public void DirectoryExists_WithValidDirectory_ReturnsTrue()
    {
        // Arrange
        Directory.CreateDirectory(_testDirectory);

        try
        {
            // Act
            var result = _fileSystemService.DirectoryExists(_testDirectory);

            // Assert
            result.Should().BeTrue();
        }
        finally
        {
            if (Directory.Exists(_testDirectory))
                Directory.Delete(_testDirectory, true);
        }
    }

    [Fact]
    public void DirectoryExists_WithInvalidDirectory_ReturnsFalse()
    {
        // Arrange
        var invalidPath = Path.Combine(_testDirectory, "nonexistent", "deeply", "nested");

        // Act
        var result = _fileSystemService.DirectoryExists(invalidPath);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void DirectoryExists_WithEmptyString_ReturnsFalse()
    {
        // Act
        var result = _fileSystemService.DirectoryExists(string.Empty);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void CreateDirectory_WithValidPath_CreatesDirectorySuccessfully()
    {
        // Arrange
        var newDir = Path.Combine(_testDirectory, "new", "nested", "dir");

        try
        {
            // Act
            var result = _fileSystemService.CreateDirectory(newDir);

            // Assert
            Directory.Exists(result).Should().BeTrue();
        }
        finally
        {
            if (Directory.Exists(_testDirectory))
                Directory.Delete(_testDirectory, true);
        }
    }

    #endregion

    #region File Handling Tests

    [Fact]
    public void FileExists_WithValidFile_ReturnsTrue()
    {
        // Arrange
        Directory.CreateDirectory(_testDirectory);
        var filePath = Path.Combine(_testDirectory, "test.txt");
        File.WriteAllText(filePath, "test content");

        try
        {
            // Act
            var result = _fileSystemService.FileExists(filePath);

            // Assert
            result.Should().BeTrue();
        }
        finally
        {
            if (Directory.Exists(_testDirectory))
                Directory.Delete(_testDirectory, true);
        }
    }

    [Fact]
    public void FileExists_WithNonexistentFile_ReturnsFalse()
    {
        // Arrange
        var filePath = Path.Combine(_testDirectory, "nonexistent.txt");

        // Act
        var result = _fileSystemService.FileExists(filePath);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void FileExists_WithEmptyString_ReturnsFalse()
    {
        // Act
        var result = _fileSystemService.FileExists(string.Empty);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region Path Handling Tests

    [Fact]
    public void GetFullNameFile_WithValidPath_ReturnsFullPath()
    {
        // Arrange
        var filePath = "test.txt";

        // Act
        var result = _fileSystemService.GetFullNameFile(filePath);

        // Assert
        result.Should().NotBeNullOrEmpty();
        Path.IsPathRooted(result).Should().BeTrue();
    }

    [Fact]
    public void GetFullNameDirectoryByFile_WithFilePath_ReturnsDirectory()
    {
        // Arrange
        var filePath = Path.Combine(_testDirectory, "test.txt");

        // Act
        var result = _fileSystemService.GetFullNameDirectoryByFile(filePath);

        // Assert
        result.Should().Be(_testDirectory);
    }

    #endregion

    #region Cross-Platform Path Tests

    [Fact]
    [Trait("Category", "CrossPlatform")]
    public void GetFiles_WithSearchPattern_ReturnsMatchingFiles()
    {
        // Arrange
        Directory.CreateDirectory(_testDirectory);
        File.WriteAllText(Path.Combine(_testDirectory, "file1.txt"), "content");
        File.WriteAllText(Path.Combine(_testDirectory, "file2.md"), "content");
        File.WriteAllText(Path.Combine(_testDirectory, "file3.txt"), "content");

        try
        {
            // Act
            var files = _fileSystemService.GetFiles(_testDirectory, "*.txt");

            // Assert
            files.Should().HaveCount(2);
            files.Should().AllSatisfy(f => f.EndsWith(".txt", StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            if (Directory.Exists(_testDirectory))
                Directory.Delete(_testDirectory, true);
        }
    }

    [Fact]
    [Trait("Category", "CrossPlatform")]
    public void EnumerateFiles_WithSearchPattern_ReturnsMatchingFiles()
    {
        // Arrange
        Directory.CreateDirectory(_testDirectory);
        File.WriteAllText(Path.Combine(_testDirectory, "file1.md"), "content");
        File.WriteAllText(Path.Combine(_testDirectory, "file2.md"), "content");
        File.WriteAllText(Path.Combine(_testDirectory, "file3.txt"), "content");

        try
        {
            // Act
            var files = _fileSystemService.EnumerateFiles(_testDirectory, "*.md").ToList();

            // Assert
            files.Should().HaveCount(2);
            files.Should().AllSatisfy(f => f.EndsWith(".md", StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            if (Directory.Exists(_testDirectory))
                Directory.Delete(_testDirectory, true);
        }
    }

    #endregion

    #region Async File Operations Tests

    [Fact]
    public async Task ReadAllTextAsync_WithExistingFile_ReturnsContent()
    {
        // Arrange
        Directory.CreateDirectory(_testDirectory);
        var filePath = Path.Combine(_testDirectory, "test.txt");
        var content = "Test content for async read";
        File.WriteAllText(filePath, content);

        try
        {
            // Act
            var result = await _fileSystemService.ReadAllTextAsync(filePath, CancellationToken.None);

            // Assert
            result.Should().Be(content);
        }
        finally
        {
            if (Directory.Exists(_testDirectory))
                Directory.Delete(_testDirectory, true);
        }
    }

    [Fact]
    public async Task ReadAllTextAsync_WithNonexistentFile_ThrowsException()
    {
        // Arrange
        var filePath = Path.Combine(_testDirectory, "nonexistent.txt");

        // Act & Assert
        // File not found could throw FileNotFoundException or other IO exceptions
        await Assert.ThrowsAnyAsync<Exception>(() =>
            _fileSystemService.ReadAllTextAsync(filePath, CancellationToken.None));
    }

    [Fact]
    public async Task ReadAllLinesAsync_WithExistingFile_ReturnsLines()
    {
        // Arrange
        Directory.CreateDirectory(_testDirectory);
        var filePath = Path.Combine(_testDirectory, "test.txt");
        var content = "Line 1\nLine 2\nLine 3";
        File.WriteAllText(filePath, content);

        try
        {
            // Act
            var result = await _fileSystemService.ReadAllLinesAsync(filePath, CancellationToken.None);

            // Assert
            result.Should().HaveCount(3);
            result[0].Should().Be("Line 1");
            result[1].Should().Be("Line 2");
            result[2].Should().Be("Line 3");
        }
        finally
        {
            if (Directory.Exists(_testDirectory))
                Directory.Delete(_testDirectory, true);
        }
    }

    [Fact]
    public async Task WriteAllTextAsync_WithValidPath_WritesContent()
    {
        // Arrange
        Directory.CreateDirectory(_testDirectory);
        var filePath = Path.Combine(_testDirectory, "test.txt");
        var content = "Test content for async write";

        try
        {
            // Act
            await _fileSystemService.WriteAllTextAsync(filePath, content, CancellationToken.None);

            // Assert
            File.Exists(filePath).Should().BeTrue();
            File.ReadAllText(filePath).Should().Be(content);
        }
        finally
        {
            if (Directory.Exists(_testDirectory))
                Directory.Delete(_testDirectory, true);
        }
    }

    #endregion

    #region Parent Directory Tests

    [Fact]
    public void GetParentDirectory_WithValidPath_ReturnsParent()
    {
        // Arrange
        var filePath = Path.Combine(_testDirectory, "subdir", "file.txt");

        // Act
        var result = _fileSystemService.GetParentDirectory(filePath);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().EndWith("subdir");
    }

    [Fact]
    public void GetParentDirectory_WithRootPath_ReturnsNull()
    {
        // Arrange
        var rootPath = Path.GetPathRoot(Path.GetTempPath())??string.Empty;

        // Act
        var result = _fileSystemService.GetParentDirectory(rootPath);

        // Assert
        result.Should().BeNullOrEmpty();
    }

    #endregion

    #region Edge Cases Tests

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    public void DirectoryExists_WithWhitespaceString_ReturnsFalse(string path)
    {
        // Act
        var result = _fileSystemService.DirectoryExists(path);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void GetFullNameFile_WithSpecialCharacters_HandlesCorrectly()
    {
        // Arrange
        var pathWithSpecialChars = "test-file_123.txt";

        // Act
        var result = _fileSystemService.GetFullNameFile(pathWithSpecialChars);

        // Assert
        result.Should().NotBeNullOrEmpty();
        Path.IsPathRooted(result).Should().BeTrue();
    }

    [Fact]
    public void GetFiles_WithNonexistentDirectory_ThrowsDirectoryNotFoundException()
    {
        // Arrange
        var nonexistentDir = Path.Combine(_testDirectory, "nonexistent");

        // Act & Assert
        // GetFiles throws when directory doesn't exist
        var exception = Record.Exception(() =>
            _fileSystemService.GetFiles(nonexistentDir, "*.txt"));

        exception.Should().NotBeNull();
    }

    #endregion

    #region GetDrives Tests

    [Fact]
    [Trait("Category", "CrossPlatform")]
    public void GetDrives_ReturnsDriveArray()
    {
        // Act
        var result = _fileSystemService.GetDrives();

        // Assert
        result.Should().NotBeEmpty();
        result.Should().AllSatisfy(drive => drive.Should().NotBeNullOrEmpty());
    }

    #endregion
}
