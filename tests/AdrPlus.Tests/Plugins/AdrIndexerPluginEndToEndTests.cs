// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the AdrPlus project under MIT license
// ***************************************************************************************

using AdrPlus.Abstractions;
using AdrPlus.Abstractions.Domain;
using AdrPlus.Infrastructure.FileSystem;
using AdrPlus.Plugins;
using AdrPlus.Plugins.AdrIndexer;

namespace AdrPlus.Tests.Plugins;

/// <summary>
/// End-to-end test for the Phase 11 reference plugin (<see cref="AdrIndexerPlugin"/>): exercises the real
/// <see cref="PluginLoader.ValidateManifestAsync"/>/<see cref="PluginLoader.LoadAssembly"/> path against the
/// actually-compiled plugin DLL on disk (via the test project's own <c>ProjectReference</c>), closing the
/// entryType/Name/Version/abstractionsVersion coverage gap that Fase 3/9's fixture-only tests deferred here.
/// </summary>
public class AdrIndexerPluginEndToEndTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "adrplus-adrindexer-e2e-" + Guid.NewGuid().ToString("N"));
    private System.Runtime.Loader.AssemblyLoadContext? _loadContext;

    public void Dispose()
    {
        // The plugin's assembly is loaded from a file under _root, which keeps that file locked on Windows
        // until its collectible ALC is actually unloaded — unload (and wait for it) before deleting the tree.
        if (_loadContext is { } loadContext)
        {
            var weakRef = new WeakReference(loadContext);
            loadContext.Unload();
            for (var i = 0; weakRef.IsAlive && i < 10; i++)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
        }

        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    private static void WriteAdrFile(string path, string title, string version, string revision, string created, string changed)
    {
        var content = $"""
            <!-- Do not remove this comment, lines and table (1-12) -->
            |Adr-Plus Fields|Values Migrated |
            |--|--|
            |File title md|{title}|
            |Version|{version}|
            |Revision|{revision}|
            |Scope||
            |Domain||
            |Created|{created}|
            |Changed|{changed}|
            |Superseded||
            <!-- Do not remove this comment, lines and table (1-12) -->
            ---
            # {title}
            """;
        File.WriteAllText(path, content);
    }

    [Fact]
    public async Task RealPlugin_LoadedViaPluginLoader_GeneratesIndexFromAllAdrFilesInRepo()
    {
        var adrFolder = Path.Combine(_root, "doc", "adr");
        Directory.CreateDirectory(adrFolder);
        WriteAdrFile(Path.Combine(adrFolder, "ADR001V01-first-decision.md"), "First decision", "01", "", "Proposed (2026-07-29)", "Accepted (2026-07-29)");
        WriteAdrFile(Path.Combine(adrFolder, "ADR002V01-second-decision.md"), "Second decision", "01", "02", "Proposed (2026-07-30)", "");

        var pluginFolder = Path.Combine(_root, "plugins", "adr-indexer");
        Directory.CreateDirectory(pluginFolder);
        // AdrPlus.Abstractions is deliberately NOT copied: per PluginAssemblyLoadContext's contract, its types
        // are resolved by the host's default context, so the plugin must share the host's copy — copying a
        // second one here would give the plugin ALC a distinct IAdrPlugin type identity and fail the
        // Name/Version/entryType compatibility check in PluginLoader.LoadAssembly.
        var compiledDllPath = typeof(AdrIndexerPlugin).Assembly.Location;
        foreach (var file in Directory.EnumerateFiles(Path.GetDirectoryName(compiledDllPath)!))
        {
            if (Path.GetFileName(file).StartsWith("AdrPlus.Abstractions.", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            File.Copy(file, Path.Combine(pluginFolder, Path.GetFileName(file)), overwrite: true);
        }

        var fileSystem = new FileSystemService();
        var loader = new PluginLoader(fileSystem);
        var manifestOutcome = await loader.ValidateManifestAsync(pluginFolder, allowlist: null, _ => { }, TestContext.Current.CancellationToken);
        manifestOutcome.Rejection.Should().BeNull();
        manifestOutcome.Manifest.Should().NotBeNull();

        var loadOutcome = PluginLoader.LoadAssembly(pluginFolder, manifestOutcome.Manifest!);
        loadOutcome.Rejection.Should().BeNull();
        loadOutcome.Loaded.Should().NotBeNull();

        var plugin = loadOutcome.Loaded!.Instance;
        _loadContext = loadOutcome.Loaded.LoadContext;
        var config = Substitute.For<IPluginConfiguration>();
        var context = Substitute.For<IPluginContext>();
        context.Logger.Returns(Substitute.For<IPluginLogger>());
        await plugin.InitializeAsync(context, config, TestContext.Current.CancellationToken);

        var eventContext = new AdrEventContext
        {
            EventType = AdrEventType.Approved,
            IsReplay = false,
            Adr = new AdrRecordSnapshot
            {
                Number = 1,
                Version = 1,
                Title = "First decision",
                Domain = string.Empty,
                Scope = string.Empty,
                StatusCreate = AdrStatus.Proposed,
                StatusUpdate = AdrStatus.Accepted,
                StatusChange = AdrStatus.Accepted
            },
            AdrFilePath = Path.Combine(adrFolder, "ADR001V01-first-decision.md"),
            GetAdrRenderedContent = () => string.Empty,
            Repo = new RepoInfoSnapshot
            {
                FolderAdr = "doc/adr",
                Scopes = [],
                StatusMapping = new Dictionary<AdrStatus, string>
                {
                    [AdrStatus.Proposed] = "Proposed",
                    [AdrStatus.Accepted] = "Accepted",
                    [AdrStatus.Rejected] = "Rejected",
                    [AdrStatus.Superseded] = "Superseded"
                }
            },
            CorrelationId = Guid.NewGuid().ToString()
        };

        var result = await plugin.OnAdrEventAsync(eventContext, TestContext.Current.CancellationToken);

        result.Status.Should().Be(PluginResultStatus.Success);
        var indexPath = Path.Combine(adrFolder, "indexadrs.md");
        File.Exists(indexPath).Should().BeTrue();
        var indexContent = await File.ReadAllTextAsync(indexPath, TestContext.Current.CancellationToken);
        indexContent.Should().NotContain("[content]");
        indexContent.Should().Contain("| [ADR001V01-first-decision](ADR001V01-first-decision.md) | First decision | V01 | Accepted (2026-07-29) |");
        indexContent.Should().Contain("| [ADR002V01-second-decision](ADR002V01-second-decision.md) | Second decision | V01 (R02) | Proposed (2026-07-30) |");

        await plugin.DisposeAsync();
    }
}
