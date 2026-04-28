// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the AdrPlus project under MIT license
// ***************************************************************************************

using AdrPlus.Infrastructure.FileSystem;

namespace AdrPlus.Tests.Infrastructure;

public class FileSystemServiceTests
{
    private readonly FileSystemService _fileSystemService;
    private readonly string _testDirectory;

    public FileSystemServiceTests()
    {
        _fileSystemService = new FileSystemService();
        _testDirectory = Path.Combine(Path.GetTempPath(), $"AdrPlusTests_{Guid.NewGuid():N}");
    }

    [Fact]
    public void DirectoryExists_WhenDirectoryDoesNotExist_ReturnsFalse()
    {
        // Arrange
        var nonExistentPath = Path.Combine(_testDirectory, "nonexistent");

        // Act
        var result = _fileSystemService.DirectoryExists(nonExistentPath);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void DirectoryExists_WhenDirectoryExists_ReturnsTrue()
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
            Directory.Delete(_testDirectory, true);
        }
    }

    [Fact]
    public void CreateDirectory_CreatesDirectoryAndReturnsFullName()
    {
        // Arrange
        var newDir = Path.Combine(_testDirectory, "newdir");

        try
        {
            // Act
            var result = _fileSystemService.CreateDirectory(newDir);

            // Assert
            result.Should().NotBeNullOrEmpty();
            Directory.Exists(newDir).Should().BeTrue();
            Path.IsPathRooted(result).Should().BeTrue();
        }
        finally
        {
            if (Directory.Exists(_testDirectory))
            {
                Directory.Delete(_testDirectory, true);
            }
        }
    }

    [Fact]
    public void GetFullNameDirectory_ReturnsFullPath()
    {
        // Arrange
        Directory.CreateDirectory(_testDirectory);

        try
        {
            // Act
            var result = _fileSystemService.GetFullNameDirectoryByFile(_testDirectory);

            // Assert
            result.Should().NotBeNullOrEmpty();
            Path.IsPathRooted(result).Should().BeTrue();
        }
        finally
        {
            Directory.Delete(_testDirectory, true);
        }
    }

    [Fact]
    public void FileExists_WhenFileDoesNotExist_ReturnsFalse()
    {
        // Arrange
        var nonExistentFile = Path.Combine(_testDirectory, "nonexistent.txt");

        // Act
        var result = _fileSystemService.FileExists(nonExistentFile);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task WriteAllTextAsync_CreatesFileWithContent()
    {
        // Arrange
        Directory.CreateDirectory(_testDirectory);
        var filePath = Path.Combine(_testDirectory, "test.txt");
        var content = "Hello, World!";

        try
        {
            // Act
            await _fileSystemService.WriteAllTextAsync(filePath, content);

            // Assert
            File.Exists(filePath).Should().BeTrue();
            var readContent = await File.ReadAllTextAsync(filePath);
            readContent.Should().Be(content);
        }
        finally
        {
            Directory.Delete(_testDirectory, true);
        }
    }

    [Fact]
    public async Task ReadAllTextAsync_ReturnsFileContent()
    {
        // Arrange
        Directory.CreateDirectory(_testDirectory);
        var filePath = Path.Combine(_testDirectory, "test.txt");
        var content = "Test content";
        await File.WriteAllTextAsync(filePath, content);

        try
        {
            // Act
            var result = await _fileSystemService.ReadAllTextAsync(filePath);

            // Assert
            result.Should().Be(content);
        }
        finally
        {
            Directory.Delete(_testDirectory, true);
        }
    }

    [Fact]
    public async Task ReadAllLinesAsync_ReturnsFileLines()
    {
        // Arrange
        Directory.CreateDirectory(_testDirectory);
        var filePath = Path.Combine(_testDirectory, "test.txt");
        var lines = new[] { "Line 1", "Line 2", "Line 3" };
        await File.WriteAllLinesAsync(filePath, lines);

        try
        {
            // Act
            var result = await _fileSystemService.ReadAllLinesAsync(filePath);

            // Assert
            result.Should().BeEquivalentTo(lines);
        }
        finally
        {
            Directory.Delete(_testDirectory, true);
        }
    }

    [Fact]
    public void GetFullNameFile_ReturnsFullPath()
    {
        // Arrange
        Directory.CreateDirectory(_testDirectory);
        var filePath = Path.Combine(_testDirectory, "test.txt");
        File.WriteAllText(filePath, "content");

        try
        {
            // Act
            var result = _fileSystemService.GetFullNameFile(filePath);

            // Assert
            result.Should().NotBeNullOrEmpty();
            Path.IsPathRooted(result).Should().BeTrue();
        }
        finally
        {
            Directory.Delete(_testDirectory, true);
        }
    }

    [Fact]
    public void EnumerateFiles_ReturnsMatchingFiles()
    {
        // Arrange
        Directory.CreateDirectory(_testDirectory);
        var file1 = Path.Combine(_testDirectory, "test1.txt");
        var file2 = Path.Combine(_testDirectory, "test2.txt");
        var file3 = Path.Combine(_testDirectory, "other.md");
        File.WriteAllText(file1, "content");
        File.WriteAllText(file2, "content");
        File.WriteAllText(file3, "content");

        try
        {
            // Act
            var result = _fileSystemService.EnumerateFiles(_testDirectory, "*.txt").ToList();

            // Assert
            result.Should().HaveCount(2);
            result.Should().Contain(file1);
            result.Should().Contain(file2);
        }
        finally
        {
            Directory.Delete(_testDirectory, true);
        }
    }

    [Fact]
    public void GetFiles_ReturnsMatchingFiles()
    {
        // Arrange
        Directory.CreateDirectory(_testDirectory);
        var file1 = Path.Combine(_testDirectory, "test1.md");
        var file2 = Path.Combine(_testDirectory, "test2.md");
        File.WriteAllText(file1, "content");
        File.WriteAllText(file2, "content");

        try
        {
            // Act
            var result = _fileSystemService.GetFiles(_testDirectory, "*.md");

            // Assert
            result.Should().HaveCount(2);
        }
        finally
        {
            Directory.Delete(_testDirectory, true);
        }
    }

    [Fact]
    public void GetDrives_ReturnsLogicalDrives()
    {
        // Act
        var result = _fileSystemService.GetDrives();

        // Assert
        result.Should().NotBeNull();
        result.Should().NotBeEmpty();
    }

    [Fact]
    public async Task SaveHistoryAsync_CreatesFileWithSerializedContent()
    {
        // Arrange
        var fileKey = $"test_{Guid.NewGuid():N}";
        var testData = new { Name = "Test", Value = 42 };

        // Act
        await _fileSystemService.SaveHistoryAsync(fileKey, testData);

        // Assert
        var (success, result) = await _fileSystemService.ReadHistoryAsync<Dictionary<string, object>>(fileKey);
        success.Should().BeTrue();
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task ReadHistoryAsync_WhenFileDoesNotExist_ReturnsFailure()
    {
        // Arrange
        var fileKey = $"nonexistent_{Guid.NewGuid():N}";

        // Act
        var (success, result) = await _fileSystemService.ReadHistoryAsync<string>(fileKey);

        // Assert
        success.Should().BeFalse();
        result.Should().BeNull();
    }

    [Fact]
    public async Task SaveHistoryAsync_WithNullFileKey_ThrowsArgumentNullException()
    {
        // Arrange
        string? fileKey = null;
        var testData = new { Name = "Test" };

        // Act
        var act = async () => await _fileSystemService.SaveHistoryAsync(fileKey!, testData);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task ReadHistoryAsync_WithNullFileKey_ThrowsArgumentNullException()
    {
        // Arrange
        string? fileKey = null;

        // Act
        var act = async () => await _fileSystemService.ReadHistoryAsync<string>(fileKey!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }
}
