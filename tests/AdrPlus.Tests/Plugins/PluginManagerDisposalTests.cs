// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the AdrPlus project under MIT license
// ***************************************************************************************

using AdrPlus.Abstractions;
using AdrPlus.Domain;
using AdrPlus.Infrastructure.FileSystem;
using AdrPlus.Infrastructure.UI;
using AdrPlus.Plugins;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute.ExceptionExtensions;

namespace AdrPlus.Tests.Plugins;

/// <summary>
/// Unit tests for <see cref="PluginManager.DisposeLoadedPluginsAsync"/> — the CLI's graceful-shutdown hook:
/// disposes every loaded plugin instance (fail-soft per plugin), unloads any retained
/// <c>AssemblyLoadContext</c> best-effort, and clears <see cref="IPluginManager.LoadedPlugins"/>/
/// <see cref="IPluginManager.Rejections"/> so the method is idempotent.
/// </summary>
/// <remarks>
/// Same seeding approach as <see cref="PluginManagerDispatchTests"/>: plugins are added directly to
/// <see cref="PluginManager._loadedPlugins"/>. Fixtures never carry a real <c>AssemblyLoadContext</c> — a real
/// one requires a compiled plugin assembly (deferred to the reference fixture plugin, same limitation already
/// accepted in <c>PluginLoaderTests</c>/<c>PluginManagerTests</c>).
/// </remarks>
public class PluginManagerDisposalTests
{
    private const string FolderPath = "/repo/plugins/test-plugin";
    private readonly IFileSystemService _fileSystem = Substitute.For<IFileSystemService>();
    private readonly ILogger<PluginManager> _logger = Substitute.For<ILogger<PluginManager>>();
    private readonly IConsoleWriter _console = Substitute.For<IConsoleWriter>();

    private static PluginManifest CreateManifest(string name, int foregroundTimeoutMs = 5000) => new()
    {
        Name = name,
        Version = "1.0.0",
        EntryAssembly = "Plugin.dll",
        EntryType = "Plugin.Type",
        AbstractionsVersion = "1.0.0",
        SubscribedEvents = ["Approved"],
        ForegroundTimeoutMs = foregroundTimeoutMs
    };

    private static LoadedPlugin CreateLoadedPlugin(string name, IAdrPlugin instance) =>
        new(instance, CreateManifest(name), FolderPath);

    private PluginManager CreateManager() =>
        new(_fileSystem, Options.Create(new AdrPlusConfig()), _logger, _console);

    [Fact]
    public async Task DisposeLoadedPluginsAsync_WithLoadedPlugins_CallsDisposeAsyncOnEach()
    {
        var manager = CreateManager();
        var plugin1 = Substitute.For<IAdrPlugin>();
        var plugin2 = Substitute.For<IAdrPlugin>();
        manager._loadedPlugins.Add(CreateLoadedPlugin("Plugin1", plugin1));
        manager._loadedPlugins.Add(CreateLoadedPlugin("Plugin2", plugin2));

        await manager.DisposeLoadedPluginsAsync(TestContext.Current.CancellationToken);

        await plugin1.Received(1).DisposeAsync();
        await plugin2.Received(1).DisposeAsync();
    }

    [Fact]
    public async Task DisposeLoadedPluginsAsync_WhenOnePluginThrows_StillDisposesTheOthers()
    {
        var manager = CreateManager();
        var throwing = Substitute.For<IAdrPlugin>();
        throwing.DisposeAsync().Throws(new InvalidOperationException("boom"));
        var healthy = Substitute.For<IAdrPlugin>();
        manager._loadedPlugins.Add(CreateLoadedPlugin("Throwing", throwing));
        manager._loadedPlugins.Add(CreateLoadedPlugin("Healthy", healthy));

        await manager.DisposeLoadedPluginsAsync(TestContext.Current.CancellationToken);

        await healthy.Received(1).DisposeAsync();
    }

    [Fact]
    public async Task DisposeLoadedPluginsAsync_WithNullLoadContext_DoesNotThrow()
    {
        var manager = CreateManager();
        var plugin = Substitute.For<IAdrPlugin>();
        manager._loadedPlugins.Add(CreateLoadedPlugin("Plugin1", plugin));

        var act = () => manager.DisposeLoadedPluginsAsync(TestContext.Current.CancellationToken);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task DisposeLoadedPluginsAsync_ClearsLoadedPlugins()
    {
        var manager = CreateManager();
        var plugin = Substitute.For<IAdrPlugin>();
        manager._loadedPlugins.Add(CreateLoadedPlugin("Plugin1", plugin));

        await manager.DisposeLoadedPluginsAsync(TestContext.Current.CancellationToken);

        manager.LoadedPlugins.Should().BeEmpty();
    }

    [Fact]
    public async Task DisposeLoadedPluginsAsync_WhenDisposeAsyncHangsPastTimeout_DoesNotBlockIndefinitely()
    {
        var manager = CreateManager();
        var hanging = Substitute.For<IAdrPlugin>();
        var neverCompletes = new TaskCompletionSource();
        hanging.DisposeAsync().Returns(new ValueTask(neverCompletes.Task));
        manager._loadedPlugins.Add(new LoadedPlugin(hanging, CreateManifest("Hanging", foregroundTimeoutMs: 30), FolderPath));

        var disposeTask = manager.DisposeLoadedPluginsAsync(TestContext.Current.CancellationToken);
        var completed = await Task.WhenAny(disposeTask, Task.Delay(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken));

        completed.Should().BeSameAs(disposeTask);
        manager.LoadedPlugins.Should().BeEmpty();
    }

    [Fact]
    public async Task DisposeLoadedPluginsAsync_CalledTwice_IsIdempotent()
    {
        var manager = CreateManager();
        var plugin = Substitute.For<IAdrPlugin>();
        manager._loadedPlugins.Add(CreateLoadedPlugin("Plugin1", plugin));

        await manager.DisposeLoadedPluginsAsync(TestContext.Current.CancellationToken);
        var act = () => manager.DisposeLoadedPluginsAsync(TestContext.Current.CancellationToken);

        await act.Should().NotThrowAsync();
        await plugin.Received(1).DisposeAsync();
    }
}
