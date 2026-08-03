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
/// — the bulk read/write primitives the Fase 5 retry engine uses (one read, one write, per plugin per
/// <c>sync</c> run). <see cref="PendingStateStore.UpsertAsync"/> (Fase 4, unchanged) already has coverage via
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
