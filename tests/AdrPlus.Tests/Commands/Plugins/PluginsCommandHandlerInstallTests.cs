// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the AdrPlus project under MIT license
// ***************************************************************************************

using AdrPlus.Commands;
using AdrPlus.Commands.Plugins;
using AdrPlus.Core;
using AdrPlus.Domain;
using AdrPlus.Infrastructure.FileSystem;
using AdrPlus.Infrastructure.UI;
using AdrPlus.Plugins;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.IO.Compression;
using System.Text.Json;

namespace AdrPlus.Tests.Commands.Plugins;

/// <summary>
/// Tests for <see cref="PluginsCommandHandler"/>'s <c>--install</c>/<c>--uninstall</c> flags.
/// Host-global, zip-based: neither takes <c>--path</c>, and both target the mocked
/// <see cref="IPluginManager.UserPluginsRoot"/> — a real temp folder distinct from any repo. Unlike
/// <see cref="PluginsCommandHandlerTests"/>, these exercise real disk I/O — zip extraction and folder copy/delete
/// go through raw <see cref="System.IO"/>/<see cref="ZipFile"/>, not <see cref="IFileSystemService"/> (same
/// precedent as <c>AdrIndexerPluginEndToEndTests</c>) — so only the repo-config read/write side
/// (still exercised by <c>--activate</c>/<c>--deactivate</c>, unaffected by this pivot) is mocked.
/// </summary>
public class PluginsCommandHandlerInstallTests : IDisposable
{
    private readonly string _repoRoot = Path.Combine(Path.GetTempPath(), "adrplus-plugins-install-" + Guid.NewGuid().ToString("N"));
    private readonly string _userPluginsRoot = Path.Combine(Path.GetTempPath(), "adrplus-plugins-store-" + Guid.NewGuid().ToString("N"));

    private ILogger<PluginsCommandHandler> _mockLogger = null!;
    private IFileSystemService _mockFileSystem = null!;
    private IConsoleWriter _mockConsole = null!;
    private IAdrServices _mockAdrServices = null!;
    private IValidateConfig _mockValidateConfig = null!;
    private IPluginManager _mockPluginManager = null!;
    private PluginsCommandHandler _handler = null!;

    public PluginsCommandHandlerInstallTests()
    {
        Directory.CreateDirectory(_repoRoot);

        _mockLogger = Substitute.For<ILogger<PluginsCommandHandler>>();
        _mockFileSystem = Substitute.For<IFileSystemService>();
        _mockConsole = Substitute.For<IConsoleWriter>();
        _mockAdrServices = Substitute.For<IAdrServices>();
        _mockValidateConfig = Substitute.For<IValidateConfig>();
        _mockPluginManager = Substitute.For<IPluginManager>();
        _mockPluginManager.LoadedPlugins.Returns(new List<LoadedPlugin>());
        _mockPluginManager.Rejections.Returns(new List<PluginRejection>());
        _mockPluginManager.UserPluginsRoot.Returns(_userPluginsRoot);

        _handler = new PluginsCommandHandler(_mockLogger, _mockFileSystem, _mockConsole, _mockAdrServices, _mockValidateConfig, Options.Create(new AdrPlusConfig()), _mockPluginManager);

        _mockFileSystem.DirectoryExists(_repoRoot).Returns(true);
    }

    public void Dispose()
    {
        if (Directory.Exists(_repoRoot))
        {
            Directory.Delete(_repoRoot, recursive: true);
        }
        if (Directory.Exists(_userPluginsRoot))
        {
            Directory.Delete(_userPluginsRoot, recursive: true);
        }
    }

    private void SetupArgs(Dictionary<Arguments, string> parsed) =>
        _mockAdrServices.ParseArgs(Arg.Any<string[]>(), Arg.Any<Arguments[]>()).Returns(parsed);

    private string CreatePluginZip(string zipFileName, string manifestName, string manifestVersion, string? extraEntryPath = null)
    {
        var zipPath = Path.Combine(_repoRoot, zipFileName);
        using var archive = ZipFile.Open(zipPath, ZipArchiveMode.Create);
        var manifest = JsonSerializer.Serialize(new
        {
            name = manifestName,
            version = manifestVersion,
            entryAssembly = $"{manifestName}.dll",
            entryType = $"{manifestName}.Plugin",
            abstractionsVersion = "1.0.0",
            subscribedEvents = new[] { "Approved" }
        });
        WriteZipEntry(archive, "plugin.json", manifest);
        WriteZipEntry(archive, $"{manifestName}.dll", "fake-binary-content");
        if (extraEntryPath is not null)
        {
            WriteZipEntry(archive, extraEntryPath, "unsafe");
        }
        _mockFileSystem.FileExists(zipPath).Returns(true);
        return zipPath;
    }

    private static void WriteZipEntry(ZipArchive archive, string entryName, string content)
    {
        var entry = archive.CreateEntry(entryName);
        using var writer = new StreamWriter(entry.Open());
        writer.Write(content);
    }

    [Fact]
    public async Task Install_ValidZip_CopiesFilesAndReportsSuccess()
    {
        var zipPath = CreatePluginZip("TestPlugin-1.0.0.zip", "TestPlugin", "1.0.0");
        SetupArgs(new Dictionary<Arguments, string> { [Arguments.PluginsInstall] = zipPath });

        await _handler.ExecuteAsync(["--install", zipPath], TestContext.Current.CancellationToken);

        var destDir = Path.Combine(_userPluginsRoot, "TestPlugin");
        File.Exists(Path.Combine(destDir, "plugin.json")).Should().BeTrue();
        File.Exists(Path.Combine(destDir, "TestPlugin.dll")).Should().BeTrue();
        _mockConsole.Received(1).PromptWriteSuccess(Arg.Is<string>(s => s.Contains("TestPlugin") && s.Contains("1.0.0")));
    }

    [Fact]
    public async Task Install_ZipNameDoesNotMatchPattern_ThrowsArgumentException()
    {
        var zipPath = CreatePluginZip("not-a-valid-name.zip", "TestPlugin", "1.0.0");
        SetupArgs(new Dictionary<Arguments, string> { [Arguments.PluginsInstall] = zipPath });

        await _handler.Invoking(h => h.ExecuteAsync(["--install", zipPath], TestContext.Current.CancellationToken))
            .Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task Install_ManifestNameMismatchesFileName_ThrowsAndLeavesNoDestination()
    {
        var zipPath = CreatePluginZip("TestPlugin-1.0.0.zip", "SomeOtherName", "1.0.0");
        SetupArgs(new Dictionary<Arguments, string> { [Arguments.PluginsInstall] = zipPath });

        await _handler.Invoking(h => h.ExecuteAsync(["--install", zipPath], TestContext.Current.CancellationToken))
            .Should().ThrowAsync<InvalidDataException>();

        Directory.Exists(Path.Combine(_userPluginsRoot, "TestPlugin")).Should().BeFalse();
    }

    [Fact]
    public async Task Install_ZipEntryWithTraversalPath_ThrowsInvalidDataException()
    {
        var zipPath = CreatePluginZip("TestPlugin-1.0.0.zip", "TestPlugin", "1.0.0", extraEntryPath: "sub/evil.txt");
        SetupArgs(new Dictionary<Arguments, string> { [Arguments.PluginsInstall] = zipPath });

        await _handler.Invoking(h => h.ExecuteAsync(["--install", zipPath], TestContext.Current.CancellationToken))
            .Should().ThrowAsync<InvalidDataException>();
    }

    [Fact]
    public async Task Install_DestinationAlreadyExistsWithoutForce_ThrowsInvalidOperationException()
    {
        var destDir = Path.Combine(_userPluginsRoot, "TestPlugin");
        Directory.CreateDirectory(destDir);
        File.WriteAllText(Path.Combine(destDir, "old.txt"), "old");
        var zipPath = CreatePluginZip("TestPlugin-1.0.0.zip", "TestPlugin", "1.0.0");
        SetupArgs(new Dictionary<Arguments, string> { [Arguments.PluginsInstall] = zipPath });

        await _handler.Invoking(h => h.ExecuteAsync(["--install", zipPath], TestContext.Current.CancellationToken))
            .Should().ThrowAsync<InvalidOperationException>();

        File.Exists(Path.Combine(destDir, "old.txt")).Should().BeTrue();
    }

    [Fact]
    public async Task Install_DestinationExistsWithForce_OverwritesEntirely()
    {
        var destDir = Path.Combine(_userPluginsRoot, "TestPlugin");
        Directory.CreateDirectory(destDir);
        File.WriteAllText(Path.Combine(destDir, "old.txt"), "old");
        var zipPath = CreatePluginZip("TestPlugin-1.0.0.zip", "TestPlugin", "1.0.0");
        SetupArgs(new Dictionary<Arguments, string> { [Arguments.PluginsInstall] = zipPath, [Arguments.PluginsForce] = string.Empty });

        await _handler.ExecuteAsync(["--install", zipPath, "--force"], TestContext.Current.CancellationToken);

        File.Exists(Path.Combine(destDir, "old.txt")).Should().BeFalse();
        File.Exists(Path.Combine(destDir, "plugin.json")).Should().BeTrue();
    }

    [Fact]
    public async Task Uninstall_ExistingFolder_DeletesFolderWithoutTouchingAnyRepoConfig()
    {
        var destDir = Path.Combine(_userPluginsRoot, "TestPlugin");
        Directory.CreateDirectory(destDir);
        File.WriteAllText(Path.Combine(destDir, "plugin.json"), "{}");
        SetupArgs(new Dictionary<Arguments, string> { [Arguments.PluginsUninstall] = "TestPlugin" });

        await _handler.ExecuteAsync(["--uninstall", "TestPlugin"], TestContext.Current.CancellationToken);

        Directory.Exists(destDir).Should().BeFalse();
        _mockConsole.Received(1).PromptWriteSuccess(Arg.Is<string>(s => s.Contains("TestPlugin")));
        // Host-global uninstall: no repository is in scope, so activeplugins is never read or written.
        await _mockFileSystem.DidNotReceive().WriteAllTextAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Uninstall_FolderAlreadyGone_IsSafeNoOp()
    {
        SetupArgs(new Dictionary<Arguments, string> { [Arguments.PluginsUninstall] = "TestPlugin" });

        await _handler.ExecuteAsync(["--uninstall", "TestPlugin"], TestContext.Current.CancellationToken);

        _mockConsole.DidNotReceive().PromptWriteSuccess(Arg.Any<string>());
        await _mockFileSystem.DidNotReceive().WriteAllTextAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Wizard_InstallSelected_PromptsForZipAndInstallsWithoutAnyFolderPrompt()
    {
        var zipPath = CreatePluginZip("WizardPlugin-1.0.0.zip", "WizardPlugin", "1.0.0");
        _mockAdrServices.ParseArgs(Arg.Any<string[]>(), Arg.Any<Arguments[]>())
            .Returns(new Dictionary<Arguments, string> { [Arguments.WizardPlugins] = string.Empty });
        _mockConsole.PromptSelectPluginsMode(Arg.Any<CancellationToken>()).Returns((false, PluginsWizardMode.Install));
        _mockConsole.PromptInputPluginZipPath(_mockFileSystem, Arg.Any<CancellationToken>()).Returns((false, zipPath));
        _mockConsole.PromptConfirm(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((false, false));

        await _handler.ExecuteAsync(["--wizard"], TestContext.Current.CancellationToken);

        var destDir = Path.Combine(_userPluginsRoot, "WizardPlugin");
        File.Exists(Path.Combine(destDir, "plugin.json")).Should().BeTrue();
        _mockConsole.Received(1).PromptWriteSuccess(Arg.Is<string>(s => s.Contains("WizardPlugin")));
        _mockConsole.DidNotReceive().PromptSelectFolderPath(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<string>(), Arg.Any<IFileSystemService>(), Arg.Any<IValidateConfig>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Wizard_UninstallSelected_MultiSelect_UninstallsEachOneAtATimeWithoutAnyFolderPrompt()
    {
        var destDirA = Path.Combine(_userPluginsRoot, "WizardPluginA");
        var destDirB = Path.Combine(_userPluginsRoot, "WizardPluginB");
        Directory.CreateDirectory(destDirA);
        Directory.CreateDirectory(destDirB);
        File.WriteAllText(Path.Combine(destDirA, "plugin.json"), "{}");
        File.WriteAllText(Path.Combine(destDirB, "plugin.json"), "{}");
        _mockAdrServices.ParseArgs(Arg.Any<string[]>(), Arg.Any<Arguments[]>())
            .Returns(new Dictionary<Arguments, string> { [Arguments.WizardPlugins] = string.Empty });
        _mockConsole.PromptSelectPluginsMode(Arg.Any<CancellationToken>()).Returns((false, PluginsWizardMode.Uninstall));
        _mockConsole.PromptSelectPluginsToUninstall(Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>())
            .Returns((false, new[] { "WizardPluginA", "WizardPluginB" }));

        await _handler.ExecuteAsync(["--wizard"], TestContext.Current.CancellationToken);

        Directory.Exists(destDirA).Should().BeFalse();
        Directory.Exists(destDirB).Should().BeFalse();
        _mockConsole.Received(1).PromptWriteSuccess(Arg.Is<string>(s => s.StartsWith("Plugin uninstalled: WizardPluginA")));
        _mockConsole.Received(1).PromptWriteSuccess(Arg.Is<string>(s => s.StartsWith("Plugin uninstalled: WizardPluginB")));
        _mockConsole.DidNotReceive().PromptSelectFolderPath(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<string>(), Arg.Any<IFileSystemService>(), Arg.Any<IValidateConfig>(), Arg.Any<CancellationToken>());
    }
}
