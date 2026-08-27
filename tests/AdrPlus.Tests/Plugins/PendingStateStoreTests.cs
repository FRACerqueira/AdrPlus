// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the AdrPlus project under MIT license
// ***************************************************************************************

using AdrPlus.Infrastructure.FileSystem;
using AdrPlus.Plugins;
using System.Text.Json;

namespace AdrPlus.Tests.Plugins;

/// <summary>
/// Unit tests for <see cref="PendingStateStore.ReadAllAsync"/> and <see cref="PendingStateStore.WriteAllAsync"/>
/// — the bulk read/write primitives the retry engine uses (one read, one write, per plugin per
/// <c>sync</c> run). <see cref="PendingStateStore.UpsertAsync"/> (unchanged) already has coverage via
/// <c>PluginManagerDispatchTests</c>.
/// </summary>
public class PendingStateStoreTests
{
    private const string PluginFolderPath = "/repo/plugins-state/test-plugin";
    private readonly IFileSystemService _fileSystem = Substitute.For<IFileSystemService>();

    [Fact]
    public async Task ReadAllAsync_WhenFileDoesNotExist_ReturnsEmptyList()
    {
        _fileSystem.FileExists(Arg.Any<string>()).Returns(false);

        var entries = await PendingStateStore.ReadAllAsync(_fileSystem, PluginFolderPath, TestContext.Current.CancellationToken);

        entries.Should().BeEmpty();
    }

    [Fact]
    public async Task ReadAllAsync_WhenFileExists_DeserializesEntries()
    {
        var json = JsonSerializer.Serialize(new List<PendingEntry> { new() { AdrKey = "0001-v1-r0", EventType = "Approved", Attempts = 2 } }, PluginManifest.SerializerOptions);
        _fileSystem.FileExists(Arg.Any<string>()).Returns(true);
        _fileSystem.ReadAllTextAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(json);

        var entries = await PendingStateStore.ReadAllAsync(_fileSystem, PluginFolderPath, TestContext.Current.CancellationToken);

        entries.Should().ContainSingle(e => e.AdrKey == "0001-v1-r0" && e.Attempts == 2);
    }

    [Fact]
    public async Task ReadAllAsync_WhenFileContainsInvalidJson_ReturnsEmptyListAndInvokesWarning()
    {
        _fileSystem.FileExists(Arg.Any<string>()).Returns(true);
        _fileSystem.ReadAllTextAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns("{ not valid json");

        string? warning = null;
        var entries = await PendingStateStore.ReadAllAsync(_fileSystem, PluginFolderPath, TestContext.Current.CancellationToken, w => warning = w);

        entries.Should().BeEmpty();
        warning.Should().NotBeNull();
    }

    [Fact]
    public async Task WriteAllAsync_CreatesStateDirectoryAndSerializesEntries()
    {
        string? writtenJson = null;
        _fileSystem.WriteAllTextAsync(Arg.Any<string>(), Arg.Do<string>(j => writtenJson = j), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        await PendingStateStore.WriteAllAsync(_fileSystem, PluginFolderPath, [new PendingEntry { AdrKey = "0002-v1-r0", EventType = "Rejected" }], TestContext.Current.CancellationToken);

        _fileSystem.Received(1).CreateDirectory(PluginFolderPath);
        writtenJson.Should().NotBeNull();
        var entries = JsonSerializer.Deserialize<List<PendingEntry>>(writtenJson!, PluginManifest.SerializerOptions);
        entries.Should().ContainSingle(e => e.AdrKey == "0002-v1-r0");
    }

    [Fact]
    public async Task WriteAllAsync_WritesToTempFileThenMovesIntoPlaceOfTheRealPendingFile()
    {
        // A process killed mid-write must never leave a truncated pending.json — writing to a temp path
        // first and only then renaming it into place (an atomic File.Move) guarantees the previous, still-valid
        // file survives any interruption before the rename.
        var expectedPendingPath = Path.Combine(PluginFolderPath, "pending.json");
        string? writtenPath = null;
        _fileSystem.WriteAllTextAsync(Arg.Do<string>(p => writtenPath = p), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        await PendingStateStore.WriteAllAsync(_fileSystem, PluginFolderPath, [new PendingEntry { AdrKey = "0002-v1-r0", EventType = "Rejected" }], TestContext.Current.CancellationToken);

        writtenPath.Should().NotBeNull();
        writtenPath.Should().StartWith(expectedPendingPath).And.EndWith(".tmp").And.NotBe(expectedPendingPath + ".tmp");
        _fileSystem.Received(1).MoveFile(writtenPath!, expectedPendingPath);
    }

    [Fact]
    public async Task WriteAllAsync_CalledTwiceConcurrently_UsesDistinctTempFileNames()
    {
        // Regression: a fixed temp file name (e.g. always "pending.json.tmp") makes two concurrent writers to
        // the same plugin's state folder collide on the same temp path — a unique-per-call suffix avoids that.
        var writtenPaths = new List<string>();
        _fileSystem.WriteAllTextAsync(Arg.Do<string>(p => writtenPaths.Add(p)), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        await PendingStateStore.WriteAllAsync(_fileSystem, PluginFolderPath, [new PendingEntry { AdrKey = "0001-v1-r0", EventType = "Approved" }], TestContext.Current.CancellationToken);
        await PendingStateStore.WriteAllAsync(_fileSystem, PluginFolderPath, [new PendingEntry { AdrKey = "0002-v1-r0", EventType = "Rejected" }], TestContext.Current.CancellationToken);

        writtenPaths.Should().HaveCount(2);
        writtenPaths.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public async Task WriteAllAsync_WithEmptyList_WritesEmptyArray()
    {
        string? writtenJson = null;
        _fileSystem.WriteAllTextAsync(Arg.Any<string>(), Arg.Do<string>(j => writtenJson = j), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        await PendingStateStore.WriteAllAsync(_fileSystem, PluginFolderPath, [], TestContext.Current.CancellationToken);

        var entries = JsonSerializer.Deserialize<List<PendingEntry>>(writtenJson!, PluginManifest.SerializerOptions);
        entries.Should().BeEmpty();
    }
}
