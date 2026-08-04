// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the AdrPlus project under MIT license
// ***************************************************************************************

using AdrPlus.Commands;
using AdrPlus.Commands.Wizard;
using AdrPlus.Core;

namespace AdrPlus.Tests.Commands.Wizard;

/// <summary>
/// Unit tests for <see cref="WizardCommandHandler.GetBuiltinPluginsSummary"/>: the install-level (not
/// per-repo) list of plugins bundled with the adrplus package itself, shown on every top-level wizard menu
/// screen regardless of which repository — if any — is currently selected.
/// </summary>
public class WizardCommandHandlerBuiltinPluginsTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "adrplus-wizard-builtinplugins-" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    private static WizardCommandHandler CreateHandler(string builtinPluginsRoot)
    {
        var mockAdrServices = Substitute.For<IAdrServices>();
        mockAdrServices.GetCommands().Returns(Array.Empty<(CommandsAdr, string, Type, string)>());
        return new(null!, null!, null!, null!, null!, null!, null!, mockAdrServices, builtinPluginsRoot);
    }

    [Fact]
    public void GetBuiltinPluginsSummary_WithBundledPlugin_ReturnsNameAndVersion()
    {
        var indexerSource = Path.Combine(_root, "adr-indexer");
        Directory.CreateDirectory(indexerSource);
        File.WriteAllText(Path.Combine(indexerSource, "plugin.json"), """{"name": "AdrIndexer", "version": "1.0.0"}""");

        var handler = CreateHandler(_root);

        handler.GetBuiltinPluginsSummary().Should().BeEquivalentTo(["AdrIndexer v1.0.0"]);
    }

    [Fact]
    public void GetBuiltinPluginsSummary_WithNoBuiltinPluginsRootConfigured_ReturnsEmpty()
    {
        var handler = CreateHandler(string.Empty);

        handler.GetBuiltinPluginsSummary().Should().BeEmpty();
    }

    [Fact]
    public void GetBuiltinPluginsSummary_WithBuiltinPluginsRootAbsent_ReturnsEmpty()
    {
        var handler = CreateHandler(Path.Combine(_root, "does-not-exist"));

        handler.GetBuiltinPluginsSummary().Should().BeEmpty();
    }

    [Fact]
    public void GetBuiltinPluginsSummary_WithFolderMissingManifest_SkipsIt()
    {
        Directory.CreateDirectory(Path.Combine(_root, "no-manifest"));

        var handler = CreateHandler(_root);

        handler.GetBuiltinPluginsSummary().Should().BeEmpty();
    }
}
