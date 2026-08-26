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
/// End-to-end test for the reference plugin (<see cref="AdrIndexerPlugin"/>): exercises the real
/// <see cref="PluginLoader.ValidateManifestAsync"/>/<see cref="PluginLoader.LoadAssembly"/> path against the
/// actually-compiled plugin DLL on disk (via the test project's own <c>ProjectReference</c>), closing the
/// entryType/Name/Version/abstractionsVersion coverage gap that the other suites' fixture-only tests deferred here.
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

        // Even after the ALC is collected, Windows can briefly keep the plugin DLL's file handle open
        // (async unload / AV scanning on CI runners), and a still-open handle can leave the directory in a
        // delete-pending state where Directory.Exists keeps reporting true - retry a bounded number of times
        // and give up silently rather than fail the test over leftover temp-folder cleanup.
        for (var attempt = 0; attempt < 5; attempt++)
        {
            try
            {
                Directory.Delete(_root, recursive: true);
                return;
            }
            catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
            {
                Thread.Sleep(100);
            }
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

    /// <summary>
    /// Copies the actually-compiled plugin DLL (plus its non-Abstractions dependencies) into
    /// <paramref name="pluginFolder"/> and loads it via the real <see cref="PluginLoader"/> path, mirroring
    /// what the host does at runtime.
    /// </summary>
    /// <remarks>
    /// <c>AdrPlus.Abstractions</c> is deliberately NOT copied: per <c>PluginAssemblyLoadContext</c>'s contract,
    /// its types are resolved by the host's default context, so the plugin must share the host's copy — copying
    /// a second one here would give the plugin ALC a distinct <c>IAdrPlugin</c> type identity and fail the
    /// Name/Version/entryType compatibility check in <see cref="PluginLoader.LoadAssembly"/>.
    /// </remarks>
    private async Task<IAdrPlugin> LoadRealPluginAsync(string pluginFolder, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(pluginFolder);
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
        var manifestOutcome = await loader.ValidateManifestAsync(pluginFolder, allowlist: null, _ => { }, cancellationToken);
        manifestOutcome.Rejection.Should().BeNull();
        manifestOutcome.Manifest.Should().NotBeNull();

        var loadOutcome = PluginLoader.LoadAssembly(pluginFolder, manifestOutcome.Manifest!);
        loadOutcome.Rejection.Should().BeNull();
        loadOutcome.Loaded.Should().NotBeNull();

        _loadContext = loadOutcome.Loaded!.LoadContext;
        return loadOutcome.Loaded.Instance;
    }

    [Fact]
    public async Task RealPlugin_LoadedViaPluginLoader_GeneratesIndexFromAllAdrFilesInRepo()
    {
        var adrFolder = Path.Combine(_root, "doc", "adr");
        Directory.CreateDirectory(adrFolder);
        WriteAdrFile(Path.Combine(adrFolder, "ADR001V01-first-decision.md"), "First decision", "01", "", "Proposed (2026-07-29)", "Accepted (2026-07-29)");
        WriteAdrFile(Path.Combine(adrFolder, "ADR002V01-second-decision.md"), "Second decision", "01", "02", "Proposed (2026-07-30)", "");

        var pluginFolder = Path.Combine(_root, "plugins", "adr-indexer");
        var plugin = await LoadRealPluginAsync(pluginFolder, TestContext.Current.CancellationToken);

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

    /// <summary>
    /// Unlike <see cref="RealPlugin_LoadedViaPluginLoader_GeneratesIndexFromAllAdrFilesInRepo"/>, this doesn't need
    /// the real <see cref="PluginLoader"/>/<see cref="System.Runtime.Loader.AssemblyLoadContext"/> path — <c>outputFolder</c> is a
    /// file-writing behavior internal to the plugin, not something the loading mechanism affects — so it
    /// instantiates <see cref="AdrIndexerPlugin"/> directly (the test project already references it) and avoids
    /// the ALC-unload dance entirely.
    /// </summary>
    [Fact]
    public async Task RealPlugin_WithOutputFolderSetting_WritesIndexToConfiguredSubfolderInsteadOfAdrRoot()
    {
        var adrFolder = Path.Combine(_root, "doc", "adr");
        Directory.CreateDirectory(adrFolder);
        WriteAdrFile(Path.Combine(adrFolder, "ADR001V01-first-decision.md"), "First decision", "01", "", "Proposed (2026-07-29)", "Accepted (2026-07-29)");

        var plugin = new AdrIndexerPlugin();
        var config = Substitute.For<IPluginConfiguration>();
        config.GetValue<string>("outputFolder").Returns("reports");
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
        File.Exists(Path.Combine(adrFolder, "reports", "indexadrs.md")).Should().BeTrue();
        File.Exists(Path.Combine(adrFolder, "indexadrs.md")).Should().BeFalse();

        await plugin.DisposeAsync();
    }
}
