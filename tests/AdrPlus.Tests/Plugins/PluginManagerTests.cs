// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the AdrPlus project under MIT license
// ***************************************************************************************

using AdrPlus.Domain;
using AdrPlus.Infrastructure.FileSystem;
using AdrPlus.Infrastructure.UI;
using AdrPlus.Plugins;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AdrPlus.Tests.Plugins;

/// <summary>
/// Unit tests for <see cref="PluginManager"/>: discovery, and orchestration of the allowlist and
/// duplicate-name checks across multiple candidate plugin subfolders (duplicates are rejected in full,
/// not just the second one found). Fixture-based, JSON-only.
/// </summary>
public class PluginManagerTests
{
    private const string PluginsRoot = "/repo/plugins";
    private readonly IFileSystemService _fileSystem = Substitute.For<IFileSystemService>();
    private readonly ILogger<PluginManager> _logger = Substitute.For<ILogger<PluginManager>>();
    private readonly IConsoleWriter _console = Substitute.For<IConsoleWriter>();

    private static string ValidManifestJson(string name = "confluence") =>
        $$"""
        {
          "name": "{{name}}",
          "version": "1.0.0",
          "entryAssembly": "Plugin.dll",
          "entryType": "Plugin.ConfluencePlugin",
          "abstractionsVersion": "1.0.0",
          "subscribedEvents": [ "Approved" ]
        }
        """;

    private PluginManager CreateManager(AdrPlusConfig? config = null) =>
        new(_fileSystem, Options.Create(config ?? new AdrPlusConfig()), _logger, _console, userPluginsRoot: PluginsRoot);

    [Fact]
    public async Task LoadPluginsAsync_WithMissingPluginsFolder_ProducesNoResults()
    {
        _fileSystem.DirectoryExists(PluginsRoot).Returns(false);

        var manager = CreateManager();
        await manager.LoadPluginsAsync(TestContext.Current.CancellationToken);

        manager.LoadedPlugins.Should().BeEmpty();
        manager.Rejections.Should().BeEmpty();
    }

    [Fact]
    public async Task LoadPluginsAsync_WithEmptyPluginsFolder_ProducesNoResults()
    {
        _fileSystem.DirectoryExists(PluginsRoot).Returns(true);
        _fileSystem.GetDirectories(PluginsRoot, SearchOption.TopDirectoryOnly).Returns([]);

        var manager = CreateManager();
        await manager.LoadPluginsAsync(TestContext.Current.CancellationToken);

        manager.LoadedPlugins.Should().BeEmpty();
        manager.Rejections.Should().BeEmpty();
    }

    [Fact]
    public async Task LoadPluginsAsync_WithNullAllowlist_DoesNotRejectForAllowlist()
    {
        var folder = Path.Combine(PluginsRoot, "confluence");
        _fileSystem.DirectoryExists(PluginsRoot).Returns(true);
        _fileSystem.GetDirectories(PluginsRoot, SearchOption.TopDirectoryOnly).Returns([folder]);
        _fileSystem.ReadAllTextAsync(Path.Combine(folder, "plugin.json"), Arg.Any<CancellationToken>()).Returns(ValidManifestJson());

        var manager = CreateManager(new AdrPlusConfig { PluginAllowlist = null });
        await manager.LoadPluginsAsync(TestContext.Current.CancellationToken);

        manager.Rejections.Should().NotContain(r => r.Reason == PluginRejectionReason.NotInAllowlist);
    }

    [Fact]
    public async Task LoadPluginsAsync_WithEmptyAllowlist_RejectsEveryPlugin()
    {
        var folder = Path.Combine(PluginsRoot, "confluence");
        _fileSystem.DirectoryExists(PluginsRoot).Returns(true);
        _fileSystem.GetDirectories(PluginsRoot, SearchOption.TopDirectoryOnly).Returns([folder]);
        _fileSystem.ReadAllTextAsync(Path.Combine(folder, "plugin.json"), Arg.Any<CancellationToken>()).Returns(ValidManifestJson());

        var manager = CreateManager(new AdrPlusConfig { PluginAllowlist = [] });
        await manager.LoadPluginsAsync(TestContext.Current.CancellationToken);

        manager.LoadedPlugins.Should().BeEmpty();
        manager.Rejections.Should().ContainSingle(r => r.Reason == PluginRejectionReason.NotInAllowlist);
    }

    [Fact]
    public async Task LoadPluginsAsync_WithAllowlistMatchingName_DoesNotRejectForAllowlist()
    {
        var folder = Path.Combine(PluginsRoot, "confluence");
        _fileSystem.DirectoryExists(PluginsRoot).Returns(true);
        _fileSystem.GetDirectories(PluginsRoot, SearchOption.TopDirectoryOnly).Returns([folder]);
        _fileSystem.ReadAllTextAsync(Path.Combine(folder, "plugin.json"), Arg.Any<CancellationToken>()).Returns(ValidManifestJson());

        var manager = CreateManager(new AdrPlusConfig { PluginAllowlist = [new PluginAllowlistEntry { Name = "confluence" }] });
        await manager.LoadPluginsAsync(TestContext.Current.CancellationToken);

        manager.Rejections.Should().NotContain(r => r.Reason == PluginRejectionReason.NotInAllowlist);
    }

    [Fact]
    public async Task LoadPluginsAsync_WithDuplicateNamesAcrossCase_RejectsBothAsDuplicate()
    {
        // Duplicate name (both rejected) — neither candidate sharing a name should load, regardless
        // of discovery order.
        var firstFolder = Path.Combine(PluginsRoot, "jira-1");
        var secondFolder = Path.Combine(PluginsRoot, "jira-2");
        _fileSystem.DirectoryExists(PluginsRoot).Returns(true);
        _fileSystem.GetDirectories(PluginsRoot, SearchOption.TopDirectoryOnly).Returns([firstFolder, secondFolder]);
        _fileSystem.ReadAllTextAsync(Path.Combine(firstFolder, "plugin.json"), Arg.Any<CancellationToken>()).Returns(ValidManifestJson(name: "Jira"));
        _fileSystem.ReadAllTextAsync(Path.Combine(secondFolder, "plugin.json"), Arg.Any<CancellationToken>()).Returns(ValidManifestJson(name: "jira"));

        var manager = CreateManager();
        await manager.LoadPluginsAsync(TestContext.Current.CancellationToken);

        manager.LoadedPlugins.Should().BeEmpty();
        manager.Rejections.Should().HaveCount(2);
        manager.Rejections.Should().OnlyContain(r => r.Reason == PluginRejectionReason.DuplicateName);
        manager.Rejections.Should().Contain(r => r.FolderPath == firstFolder);
        manager.Rejections.Should().Contain(r => r.FolderPath == secondFolder);
    }
}
