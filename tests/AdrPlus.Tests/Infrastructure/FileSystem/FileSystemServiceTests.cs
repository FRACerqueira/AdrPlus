// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the AdrPlus project under MIT license
// ***************************************************************************************

using AdrPlus.Infrastructure.FileSystem;

namespace AdrPlus.Tests.Infrastructure.FileSystem;

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
    public void MoveFile_MovesFileToDestination()
    {
        // Arrange
        Directory.CreateDirectory(_testDirectory);
        var sourcePath = Path.Combine(_testDirectory, "source.txt");
        var destinationPath = Path.Combine(_testDirectory, "destination.txt");
        File.WriteAllText(sourcePath, "content");

        try
        {
            // Act
            _fileSystemService.MoveFile(sourcePath, destinationPath);

            // Assert
            File.Exists(sourcePath).Should().BeFalse();
            File.Exists(destinationPath).Should().BeTrue();
            File.ReadAllText(destinationPath).Should().Be("content");
        }
        finally
        {
            Directory.Delete(_testDirectory, true);
        }
    }

    [Fact]
    public void MoveFile_WhenDestinationExists_Overwrites()
    {
        // Arrange
        Directory.CreateDirectory(_testDirectory);
        var sourcePath = Path.Combine(_testDirectory, "source.txt");
        var destinationPath = Path.Combine(_testDirectory, "destination.txt");
        File.WriteAllText(sourcePath, "new content");
        File.WriteAllText(destinationPath, "stale content");

        try
        {
            // Act
            _fileSystemService.MoveFile(sourcePath, destinationPath);

            // Assert
            File.ReadAllText(destinationPath).Should().Be("new content");
        }
        finally
        {
            Directory.Delete(_testDirectory, true);
        }
    }

    [Fact]
    public void GetDirectories_ReturnsSubdirectories()
    {
        // Arrange
        Directory.CreateDirectory(_testDirectory);
        var subDir1 = Directory.CreateDirectory(Path.Combine(_testDirectory, "plugin-one")).FullName;
        var subDir2 = Directory.CreateDirectory(Path.Combine(_testDirectory, "plugin-two")).FullName;

        try
        {
            // Act
            var result = _fileSystemService.GetDirectories(_testDirectory);

            // Assert
            result.Should().HaveCount(2);
            result.Should().Contain(subDir1);
            result.Should().Contain(subDir2);
        }
        finally
        {
            Directory.Delete(_testDirectory, true);
        }
    }

    [Fact]
    public async Task SaveHistoryAsync_CreatesFileWithSerializedContent()
    {
        // Arrange
        var fileKey = $"test_{Guid.NewGuid():N}";
        var testData = new { Name = "Test", Value = 42 };

        // Act
        await _fileSystemService.SaveHistoryAsync(fileKey, testData, TestContext.Current.CancellationToken);

        // Assert
        var (success, result) = await _fileSystemService.ReadHistoryAsync<Dictionary<string, object>>(fileKey, TestContext.Current.CancellationToken);
        success.Should().BeTrue();
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task ReadHistoryAsync_WhenFileDoesNotExist_ReturnsFailure()
    {
        // Arrange
        var fileKey = $"nonexistent_{Guid.NewGuid():N}";

        // Act
        var (success, result) = await _fileSystemService.ReadHistoryAsync<string>(fileKey, TestContext.Current.CancellationToken);

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
        var act = async () => await _fileSystemService.SaveHistoryAsync(fileKey!, testData, TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task ReadHistoryAsync_WithNullFileKey_ThrowsArgumentNullException()
    {
        // Arrange
        string? fileKey = null;

        // Act
        var act = async () => await _fileSystemService.ReadHistoryAsync<string>(fileKey!, TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }
}
