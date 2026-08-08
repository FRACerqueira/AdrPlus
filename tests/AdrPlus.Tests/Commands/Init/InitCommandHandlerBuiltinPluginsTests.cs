// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the AdrPlus project under MIT license
// ***************************************************************************************

using AdrPlus.Abstractions;
using AdrPlus.Commands;
using AdrPlus.Commands.Init;
using AdrPlus.Core;
using AdrPlus.Domain;
using AdrPlus.Infrastructure.FileSystem;
using AdrPlus.Infrastructure.UI;
using AdrPlus.Plugins;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace AdrPlus.Tests.Commands.Init;

/// <summary>
/// Unit tests for <see cref="InitCommandHandler"/>'s <c>activeplugins</c> baseline seeding: <c>adrplus init</c>
/// discovers whatever is available host-globally and records the loaded names as a fresh repo's
/// baseline. The per-repo copy-into-repo mechanism (<c>InstallBuiltinPlugins</c>) was removed —
/// bundled plugins are discovered host-globally without any copy step, so this file no longer tests a copy.
/// </summary>
public class InitCommandHandlerBuiltinPluginsTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "adrplus-init-builtinplugins-" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    private InitCommandHandler CreateHandler(
        out IFileSystemService mockFileSystem,
        out IConsoleWriter mockConsole,
        out IValidateConfig mockValidateConfig,
        out IAdrServices mockAdrServices,
        IReadOnlyList<LoadedPlugin>? loadedPlugins = null)
    {
        var mockLogger = Substitute.For<ILogger<InitCommandHandler>>();
        mockFileSystem = Substitute.For<IFileSystemService>();
        mockConsole = Substitute.For<IConsoleWriter>();
        mockValidateConfig = Substitute.For<IValidateConfig>();
        mockAdrServices = Substitute.For<IAdrServices>();
        var mockPluginManager = Substitute.For<IPluginManager>();
        mockPluginManager.LoadedPlugins.Returns(loadedPlugins ?? []);

        return new InitCommandHandler(mockLogger, mockFileSystem, mockValidateConfig, mockConsole, mockAdrServices, mockPluginManager);
    }

    private static LoadedPlugin CreateLoadedPlugin(string name) => new(
        Substitute.For<IAdrPlugin>(),
        new PluginManifest { Name = name, Version = "1.0.0", EntryAssembly = "x.dll", EntryType = "x", AbstractionsVersion = "1.0.0" },
        "/host/plugins/" + name);

    [Fact]
    public async Task ExecuteAsync_WithLoadedPlugins_WritesActivePluginsBaseline()
    {
        var targetPath = Path.Combine(_root, "repo");
        Directory.CreateDirectory(targetPath);
        var repoPath = Path.Combine(targetPath, "doc", "adr");

        var handler = CreateHandler(out var mockFileSystem, out _, out var mockValidateConfig, out var mockAdrServices,
            loadedPlugins: [CreateLoadedPlugin("AdrIndexer")]);

        var args = new[] { "--path", targetPath };
        var parsedArgs = new Dictionary<Arguments, string> { { Arguments.TargetRepo, targetPath } };
        var jsonConfig = """{"Prefix": "ADR", "LenSeq": 4, "FolderByScope": false}""";

        mockAdrServices.ParseArgs(args, Arg.Any<Arguments[]>()).Returns(parsedArgs);
        mockValidateConfig.HasTemplateRepoFile().Returns(true);
        mockFileSystem.DirectoryExists(targetPath).Returns(true);
        mockFileSystem.DirectoryExists(repoPath).Returns(false);
        mockFileSystem.FileExists(Arg.Any<string>()).Returns(false);
        mockValidateConfig.GetFileNameRepoConfig().Returns(".adrplus");
        mockValidateConfig.GetDefaultConfigRepoFilePath().Returns("template-path");
        mockValidateConfig.GetConfigDefaultRepoContentAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(jsonConfig);
        mockValidateConfig.ValidateRepoStructure(jsonConfig).Returns((true, []));
        mockValidateConfig.GetMaxNumberVersionRevision(Arg.Any<string>(), Arg.Any<AdrPlusRepoConfig>()).Returns((0, 0, 0));
        mockFileSystem.CreateDirectory(repoPath).Returns(repoPath);
        mockFileSystem.GetFullNameFile(Arg.Any<string>()).Returns(Path.Combine(repoPath, ".adrplus"));

        var repoConfig = new AdrPlusRepoConfig("", "") { FolderByScope = false };
        mockAdrServices.FromJson(jsonConfig, "").Returns(repoConfig);

        var configFilePath = Path.GetFullPath(Path.Combine(targetPath, ".adrplus"));
        mockFileSystem.ReadAllTextAsync(configFilePath, Arg.Any<CancellationToken>()).Returns(jsonConfig);

        string? finalWritten = null;
        mockFileSystem.WriteAllTextAsync(configFilePath, Arg.Do<string>(content => finalWritten = content), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        await handler.ExecuteAsync(args, TestContext.Current.CancellationToken);

        finalWritten.Should().NotBeNull();
        using var doc = JsonDocument.Parse(finalWritten!);
        doc.RootElement.GetProperty("activeplugins").EnumerateArray().Select(e => e.GetString()).Should().BeEquivalentTo(["AdrIndexer"]);
    }

    [Fact]
    public async Task ExecuteAsync_WithNoPluginsLoaded_WritesConfigExactlyOnce()
    {
        // No plugins loaded -> WriteActivePluginsBaselineAsync is a no-op, so the config file written by
        // CreateNewConfigAsync is never patched a second time.
        var targetPath = Path.Combine(_root, "repo");
        Directory.CreateDirectory(targetPath);
        var repoPath = Path.Combine(targetPath, "doc", "adr");

        var handler = CreateHandler(out var mockFileSystem, out _, out var mockValidateConfig, out var mockAdrServices);

        var args = new[] { "--path", targetPath };
        var parsedArgs = new Dictionary<Arguments, string> { { Arguments.TargetRepo, targetPath } };
        var jsonConfig = """{"Prefix": "ADR", "LenSeq": 4, "FolderByScope": false}""";

        mockAdrServices.ParseArgs(args, Arg.Any<Arguments[]>()).Returns(parsedArgs);
        mockValidateConfig.HasTemplateRepoFile().Returns(true);
        mockFileSystem.DirectoryExists(targetPath).Returns(true);
        mockFileSystem.DirectoryExists(repoPath).Returns(false);
        mockFileSystem.FileExists(Arg.Any<string>()).Returns(false);
        mockValidateConfig.GetFileNameRepoConfig().Returns(".adrplus");
        mockValidateConfig.GetDefaultConfigRepoFilePath().Returns("template-path");
        mockValidateConfig.GetConfigDefaultRepoContentAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(jsonConfig);
        mockValidateConfig.ValidateRepoStructure(jsonConfig).Returns((true, []));
        mockValidateConfig.GetMaxNumberVersionRevision(Arg.Any<string>(), Arg.Any<AdrPlusRepoConfig>()).Returns((0, 0, 0));
        mockFileSystem.CreateDirectory(repoPath).Returns(repoPath);
        mockFileSystem.GetFullNameFile(Arg.Any<string>()).Returns(Path.Combine(repoPath, ".adrplus"));

        var repoConfig = new AdrPlusRepoConfig("", "") { FolderByScope = false };
        mockAdrServices.FromJson(jsonConfig, "").Returns(repoConfig);

        await handler.ExecuteAsync(args, TestContext.Current.CancellationToken);

        var configFilePath = Path.GetFullPath(Path.Combine(targetPath, ".adrplus"));
        await mockFileSystem.Received(1).WriteAllTextAsync(configFilePath, Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}
