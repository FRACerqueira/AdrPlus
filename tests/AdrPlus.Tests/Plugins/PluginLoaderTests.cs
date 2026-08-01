// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the AdrPlus project under MIT license
// ***************************************************************************************

using AdrPlus.Domain;
using AdrPlus.Infrastructure.FileSystem;
using AdrPlus.Plugins;
using System.Text.Json;

namespace AdrPlus.Tests.Plugins;

/// <summary>
/// Unit tests for <see cref="PluginLoader"/>. Fixture-based, JSON-only — cases that require a real compiled
/// plugin assembly (entryType/Name/Version/abstractionsVersion compatibility) are deferred to Fase 11; the
/// exception is <see cref="LoadAssembly_WithNoAssemblyOnDisk_IsRejectedAsEntryTypeIncompatible"/>, which only
/// needs to confirm the missing-file path, not a real plugin.
/// </summary>
public class PluginLoaderTests
{
    private const string FolderPath = "/plugins/confluence";
    private readonly IFileSystemService _fileSystem = Substitute.For<IFileSystemService>();

    private static PluginManifest ValidManifest(string name = "confluence", string entryAssembly = "Plugin.dll") => new()
    {
        Name = name,
        Version = "1.0.0",
        EntryAssembly = entryAssembly,
        EntryType = "Plugin.ConfluencePlugin",
        AbstractionsVersion = "1.0.0",
        SubscribedEvents = ["Approved"]
    };

    private static string ValidManifestJson(string name = "confluence", string entryAssembly = "Plugin.dll") =>
        JsonSerializer.Serialize(new
        {
            name,
            version = "1.0.0",
            entryAssembly,
            entryType = "Plugin.ConfluencePlugin",
            abstractionsVersion = "1.0.0",
            subscribedEvents = new[] { "Approved" }
        });

    private void SetManifest(string folderPath, string json) =>
        _fileSystem.ReadAllTextAsync(Path.Combine(folderPath, "plugin.json"), Arg.Any<CancellationToken>()).Returns(json);

    private PluginLoader CreateLoader() => new(_fileSystem);

    [Fact]
    public async Task ValidateManifestAsync_WithValidManifest_ReturnsManifest()
    {
        SetManifest(FolderPath, ValidManifestJson());

        var outcome = await CreateLoader().ValidateManifestAsync(FolderPath, null, _ => { }, TestContext.Current.CancellationToken);

        outcome.Rejection.Should().BeNull();
        outcome.Manifest.Should().NotBeNull();
        outcome.Manifest!.Name.Should().Be("confluence");
    }

    [Fact]
    public async Task ValidateManifestAsync_WithMissingRequiredField_IsRejectedAsManifestInvalid()
    {
        const string json = """
            {
              "version": "1.0.0",
              "entryAssembly": "Plugin.dll",
              "entryType": "Plugin.ConfluencePlugin",
              "abstractionsVersion": "1.0.0",
              "subscribedEvents": [ "Approved" ]
            }
            """;
        SetManifest(FolderPath, json);

        var outcome = await CreateLoader().ValidateManifestAsync(FolderPath, null, _ => { }, TestContext.Current.CancellationToken);

        outcome.Manifest.Should().BeNull();
        outcome.Rejection.Should().NotBeNull();
        outcome.Rejection!.Reason.Should().Be(PluginRejectionReason.ManifestInvalid);
    }

    [Fact]
    public async Task ValidateManifestAsync_WithMalformedJson_IsRejectedAsManifestInvalid()
    {
        SetManifest(FolderPath, "{ not valid json");

        var outcome = await CreateLoader().ValidateManifestAsync(FolderPath, null, _ => { }, TestContext.Current.CancellationToken);

        outcome.Rejection.Should().NotBeNull();
        outcome.Rejection!.Reason.Should().Be(PluginRejectionReason.ManifestInvalid);
    }

    [Theory]
    [InlineData("../evil.dll")]
    [InlineData("sub/evil.dll")]
    [InlineData(@"sub\evil.dll")]
    public async Task ValidateManifestAsync_WithPathTraversalInEntryAssembly_IsRejectedWithoutCombiningPath(string entryAssembly)
    {
        SetManifest(FolderPath, ValidManifestJson(entryAssembly: entryAssembly));

        var outcome = await CreateLoader().ValidateManifestAsync(FolderPath, null, _ => { }, TestContext.Current.CancellationToken);

        outcome.Manifest.Should().BeNull();
        outcome.Rejection.Should().NotBeNull();
        outcome.Rejection!.Reason.Should().Be(PluginRejectionReason.EntryAssemblyPathTraversal);
    }

    [Fact]
    public async Task ValidateManifestAsync_WithNullAllowlist_DoesNotRejectForAllowlist()
    {
        SetManifest(FolderPath, ValidManifestJson());

        var outcome = await CreateLoader().ValidateManifestAsync(FolderPath, null, _ => { }, TestContext.Current.CancellationToken);

        outcome.Rejection?.Reason.Should().NotBe(PluginRejectionReason.NotInAllowlist);
    }

    [Fact]
    public async Task ValidateManifestAsync_WithEmptyAllowlist_IsRejectedAsNotInAllowlist()
    {
        SetManifest(FolderPath, ValidManifestJson());

        var outcome = await CreateLoader().ValidateManifestAsync(FolderPath, [], _ => { }, TestContext.Current.CancellationToken);

        outcome.Manifest.Should().BeNull();
        outcome.Rejection.Should().NotBeNull();
        outcome.Rejection!.Reason.Should().Be(PluginRejectionReason.NotInAllowlist);
    }

    [Fact]
    public async Task ValidateManifestAsync_WithAllowlistMissingName_IsRejectedAsNotInAllowlist()
    {
        SetManifest(FolderPath, ValidManifestJson(name: "confluence"));
        var allowlist = new List<PluginAllowlistEntry> { new() { Name = "jira" } };

        var outcome = await CreateLoader().ValidateManifestAsync(FolderPath, allowlist, _ => { }, TestContext.Current.CancellationToken);

        outcome.Rejection.Should().NotBeNull();
        outcome.Rejection!.Reason.Should().Be(PluginRejectionReason.NotInAllowlist);
    }

    [Fact]
    public async Task ValidateManifestAsync_WithAllowlistMatchingNameDifferentCase_DoesNotRejectForAllowlist()
    {
        SetManifest(FolderPath, ValidManifestJson(name: "Confluence"));
        var allowlist = new List<PluginAllowlistEntry> { new() { Name = "confluence" } };

        var outcome = await CreateLoader().ValidateManifestAsync(FolderPath, allowlist, _ => { }, TestContext.Current.CancellationToken);

        outcome.Rejection?.Reason.Should().NotBe(PluginRejectionReason.NotInAllowlist);
    }

    [Fact]
    public async Task ValidateManifestAsync_WithAllowlistHashSet_InvokesHashNotEnforcedWarningButDoesNotBlockLoading()
    {
        SetManifest(FolderPath, ValidManifestJson(name: "confluence"));
        var allowlist = new List<PluginAllowlistEntry> { new() { Name = "confluence", Hash = "deadbeef" } };
        string? warnedName = null;

        var outcome = await CreateLoader().ValidateManifestAsync(FolderPath, allowlist, name => warnedName = name, TestContext.Current.CancellationToken);

        warnedName.Should().Be("confluence");
        outcome.Rejection?.Reason.Should().NotBe(PluginRejectionReason.NotInAllowlist);
    }

    [Fact]
    public void LoadAssembly_WithNoAssemblyOnDisk_IsRejectedAsEntryTypeIncompatible()
    {
        var outcome = PluginLoader.LoadAssembly(FolderPath, ValidManifest());

        outcome.Loaded.Should().BeNull();
        outcome.Rejection.Should().NotBeNull();
        outcome.Rejection!.Reason.Should().Be(PluginRejectionReason.EntryTypeIncompatible);
    }

    [Fact]
    public void RejectDuplicateName_ProducesADuplicateNameRejectionForTheGivenFolder()
    {
        var rejection = PluginLoader.RejectDuplicateName(FolderPath, "confluence");

        rejection.FolderPath.Should().Be(FolderPath);
        rejection.Reason.Should().Be(PluginRejectionReason.DuplicateName);
    }
}
