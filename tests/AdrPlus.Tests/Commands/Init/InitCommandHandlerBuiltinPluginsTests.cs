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
/// Unit tests for <see cref="InitCommandHandler"/>'s builtin-plugin install step (<c>InstallBuiltinPlugins</c>):
/// the mechanism by which <c>adrplus init</c> copies plugins bundled with the adrplus package itself (currently
/// just the AdrIndexer reference plugin) into a new repository's <c>plugins/</c> folder. Uses a real temp
/// directory for both the bundled-plugins source and the repo destination, since that step deliberately bypasses
/// <see cref="IFileSystemService"/> (see <c>InitCommandHandler.InstallBuiltinPlugins</c> remarks) — everything
/// else in <c>init</c> still goes through the mocked <see cref="IFileSystemService"/>, as in <see cref="InitCommandHandlerTests"/>.
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
        string builtinPluginsRoot,
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

        return new InitCommandHandler(mockLogger, mockFileSystem, mockValidateConfig, mockConsole, mockAdrServices, mockPluginManager, builtinPluginsRoot);
    }

    private static LoadedPlugin CreateLoadedPlugin(string name) => new(
        Substitute.For<IAdrPlugin>(),
        new PluginManifest { Name = name, Version = "1.0.0", EntryAssembly = "x.dll", EntryType = "x", AbstractionsVersion = "1.0.0" },
        "/repo/plugins/" + name);

    [Fact]
    public async Task ExecuteAsync_WithBuiltinPluginsRootConfigured_CopiesBundledPluginIntoRepo()
    {
        var builtinRoot = Path.Combine(_root, "plugins-builtin");
        var indexerSource = Path.Combine(builtinRoot, "adr-indexer");
        Directory.CreateDirectory(indexerSource);
        File.WriteAllText(Path.Combine(indexerSource, "plugin.json"), "{}");
        File.WriteAllText(Path.Combine(indexerSource, "AdrIndexer.dll"), "fake-dll-bytes");

        var repoPath = Path.Combine(_root, "repo", "doc", "adr");
        var targetPath = Path.Combine(_root, "repo");
        Directory.CreateDirectory(targetPath);

        var handler = CreateHandler(builtinRoot, out var mockFileSystem, out var mockConsole, out var mockValidateConfig, out var mockAdrServices);

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
        mockFileSystem.ReadAllTextAsync("template-path", Arg.Any<CancellationToken>()).Returns(jsonConfig);
        mockValidateConfig.ValidateRepoStructure(jsonConfig).Returns((true, []));
        mockValidateConfig.GetMaxNumberVersionRevision(Arg.Any<string>(), Arg.Any<AdrPlusRepoConfig>()).Returns((0, 0, 0));
        mockFileSystem.CreateDirectory(repoPath).Returns(repoPath);
        mockFileSystem.GetFullNameFile(Arg.Any<string>()).Returns(Path.Combine(repoPath, ".adrplus"));

        var repoConfig = new AdrPlusRepoConfig("", "") { FolderByScope = false };
        mockAdrServices.FromJson(jsonConfig, "").Returns(repoConfig);

        await handler.ExecuteAsync(args, TestContext.Current.CancellationToken);

        var destDir = Path.Combine(targetPath, "plugins", "adr-indexer");
        File.Exists(Path.Combine(destDir, "plugin.json")).Should().BeTrue();
        File.Exists(Path.Combine(destDir, "AdrIndexer.dll")).Should().BeTrue();
        mockConsole.Received().PromptWriteSuccess(Path.Combine(destDir, "plugin.json"));
        mockConsole.Received().PromptWriteSuccess(Path.Combine(destDir, "AdrIndexer.dll"));
    }

    [Fact]
    public async Task ExecuteAsync_WithLoadedPlugins_WritesActivePluginsBaseline()
    {
        var targetPath = Path.Combine(_root, "repo");
        Directory.CreateDirectory(targetPath);
        var repoPath = Path.Combine(targetPath, "doc", "adr");

        var handler = CreateHandler(string.Empty, out var mockFileSystem, out _, out var mockValidateConfig, out var mockAdrServices,
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
        mockFileSystem.ReadAllTextAsync("template-path", Arg.Any<CancellationToken>()).Returns(jsonConfig);
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
    public async Task ExecuteAsync_WithPluginFileAlreadyPresentInRepo_DoesNotOverwriteIt()
    {
        var builtinRoot = Path.Combine(_root, "plugins-builtin");
        var indexerSource = Path.Combine(builtinRoot, "adr-indexer");
        Directory.CreateDirectory(indexerSource);
        File.WriteAllText(Path.Combine(indexerSource, "plugin.json"), "{\"settings\":{\"outputFileName\":\"indexadrs.md\"}}");

        var targetPath = Path.Combine(_root, "repo");
        var repoPath = Path.Combine(targetPath, "doc", "adr");
        var destDir = Path.Combine(targetPath, "plugins", "adr-indexer");
        Directory.CreateDirectory(destDir);
        const string handEditedContent = "{\"settings\":{\"outputFileName\":\"my-custom-index.md\"}}";
        File.WriteAllText(Path.Combine(destDir, "plugin.json"), handEditedContent);

        var handler = CreateHandler(builtinRoot, out var mockFileSystem, out _, out var mockValidateConfig, out var mockAdrServices);

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
        mockFileSystem.ReadAllTextAsync("template-path", Arg.Any<CancellationToken>()).Returns(jsonConfig);
        mockValidateConfig.ValidateRepoStructure(jsonConfig).Returns((true, []));
        mockValidateConfig.GetMaxNumberVersionRevision(Arg.Any<string>(), Arg.Any<AdrPlusRepoConfig>()).Returns((0, 0, 0));
        mockFileSystem.CreateDirectory(repoPath).Returns(repoPath);
        mockFileSystem.GetFullNameFile(Arg.Any<string>()).Returns(Path.Combine(repoPath, ".adrplus"));

        var repoConfig = new AdrPlusRepoConfig("", "") { FolderByScope = false };
        mockAdrServices.FromJson(jsonConfig, "").Returns(repoConfig);

        await handler.ExecuteAsync(args, TestContext.Current.CancellationToken);

        File.ReadAllText(Path.Combine(destDir, "plugin.json")).Should().Be(handEditedContent);
    }

    [Fact]
    public async Task ExecuteAsync_WithNoBuiltinPluginsRootConfigured_DoesNotCreatePluginsFolder()
    {
        var targetPath = Path.Combine(_root, "repo");
        Directory.CreateDirectory(targetPath);
        var repoPath = Path.Combine(targetPath, "doc", "adr");

        var handler = CreateHandler(string.Empty, out var mockFileSystem, out _, out var mockValidateConfig, out var mockAdrServices);

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
        mockFileSystem.ReadAllTextAsync("template-path", Arg.Any<CancellationToken>()).Returns(jsonConfig);
        mockValidateConfig.ValidateRepoStructure(jsonConfig).Returns((true, []));
        mockValidateConfig.GetMaxNumberVersionRevision(Arg.Any<string>(), Arg.Any<AdrPlusRepoConfig>()).Returns((0, 0, 0));
        mockFileSystem.CreateDirectory(repoPath).Returns(repoPath);
        mockFileSystem.GetFullNameFile(Arg.Any<string>()).Returns(Path.Combine(repoPath, ".adrplus"));

        var repoConfig = new AdrPlusRepoConfig("", "") { FolderByScope = false };
        mockAdrServices.FromJson(jsonConfig, "").Returns(repoConfig);

        await handler.ExecuteAsync(args, TestContext.Current.CancellationToken);

        Directory.Exists(Path.Combine(targetPath, "plugins")).Should().BeFalse();
    }
}
