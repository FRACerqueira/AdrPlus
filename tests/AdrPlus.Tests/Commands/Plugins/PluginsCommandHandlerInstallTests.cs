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
/// Tests for <see cref="PluginsCommandHandler"/>'s <c>--install</c>/<c>--uninstall</c> flags (spec D35, Fase
/// 13). Unlike <see cref="PluginsCommandHandlerTests"/>, these exercise real disk I/O — zip extraction and
/// folder copy/delete go through raw <see cref="System.IO"/>/<see cref="ZipFile"/>, not
/// <see cref="IFileSystemService"/> (same precedent as <c>InitCommandHandler.InstallBuiltinPlugins</c> and the
/// Fase 11 <c>AdrIndexerPluginEndToEndTests</c>) — so only the repo-config read/write side is mocked.
/// </summary>
public class PluginsCommandHandlerInstallTests : IDisposable
{
    private readonly string _repoRoot = Path.Combine(Path.GetTempPath(), "adrplus-plugins-install-" + Guid.NewGuid().ToString("N"));

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

        _handler = new PluginsCommandHandler(_mockLogger, _mockFileSystem, _mockConsole, _mockAdrServices, _mockValidateConfig, Options.Create(new AdrPlusConfig()), _mockPluginManager);

        _mockFileSystem.DirectoryExists(_repoRoot).Returns(true);
    }

    public void Dispose()
    {
        if (Directory.Exists(_repoRoot))
        {
            Directory.Delete(_repoRoot, recursive: true);
        }
    }

    private void SetupArgs(Dictionary<Arguments, string> parsed) =>
        _mockAdrServices.ParseArgs(Arg.Any<string[]>(), Arg.Any<Arguments[]>()).Returns(parsed);

    private void ArrangeValidRepoConfig(IEnumerable<string>? activePlugins = null)
    {
        var namesJson = string.Join(", ", (activePlugins ?? []).Select(n => $"\"{n}\""));
        var json = $$"""{"activeplugins": [{{namesJson}}], "disableplugins": false}""";
        _mockValidateConfig.GetFileNameRepoConfig().Returns(".adrplus");
        _mockFileSystem.FileExists(Arg.Is<string>(s => s.EndsWith(".adrplus"))).Returns(true);
        _mockFileSystem.ReadAllTextAsync(Arg.Is<string>(s => s.EndsWith(".adrplus")), Arg.Any<CancellationToken>()).Returns(json);
        _mockValidateConfig.ValidateRepoStructure(json).Returns((true, Array.Empty<string>()));
    }

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
        SetupArgs(new Dictionary<Arguments, string> { [Arguments.PluginsInstall] = zipPath, [Arguments.TargetRepo] = _repoRoot });

        await _handler.ExecuteAsync(["--install", zipPath, "--path", _repoRoot], TestContext.Current.CancellationToken);

        var destDir = Path.Combine(_repoRoot, "plugins", "TestPlugin");
        File.Exists(Path.Combine(destDir, "plugin.json")).Should().BeTrue();
        File.Exists(Path.Combine(destDir, "TestPlugin.dll")).Should().BeTrue();
        _mockConsole.Received(1).PromptWriteSuccess(Arg.Is<string>(s => s.Contains("TestPlugin") && s.Contains("1.0.0")));
    }

    [Fact]
    public async Task Install_ZipNameDoesNotMatchPattern_ThrowsArgumentException()
    {
        var zipPath = CreatePluginZip("not-a-valid-name.zip", "TestPlugin", "1.0.0");
        SetupArgs(new Dictionary<Arguments, string> { [Arguments.PluginsInstall] = zipPath, [Arguments.TargetRepo] = _repoRoot });

        await _handler.Invoking(h => h.ExecuteAsync(["--install", zipPath, "--path", _repoRoot], TestContext.Current.CancellationToken))
            .Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task Install_ManifestNameMismatchesFileName_ThrowsAndLeavesNoDestination()
    {
        var zipPath = CreatePluginZip("TestPlugin-1.0.0.zip", "SomeOtherName", "1.0.0");
        SetupArgs(new Dictionary<Arguments, string> { [Arguments.PluginsInstall] = zipPath, [Arguments.TargetRepo] = _repoRoot });

        await _handler.Invoking(h => h.ExecuteAsync(["--install", zipPath, "--path", _repoRoot], TestContext.Current.CancellationToken))
            .Should().ThrowAsync<InvalidDataException>();

        Directory.Exists(Path.Combine(_repoRoot, "plugins", "TestPlugin")).Should().BeFalse();
    }

    [Fact]
    public async Task Install_ZipEntryWithTraversalPath_ThrowsInvalidDataException()
    {
        var zipPath = CreatePluginZip("TestPlugin-1.0.0.zip", "TestPlugin", "1.0.0", extraEntryPath: "sub/evil.txt");
        SetupArgs(new Dictionary<Arguments, string> { [Arguments.PluginsInstall] = zipPath, [Arguments.TargetRepo] = _repoRoot });

        await _handler.Invoking(h => h.ExecuteAsync(["--install", zipPath, "--path", _repoRoot], TestContext.Current.CancellationToken))
            .Should().ThrowAsync<InvalidDataException>();
    }

    [Fact]
    public async Task Install_DestinationAlreadyExistsWithoutForce_ThrowsInvalidOperationException()
    {
        var destDir = Path.Combine(_repoRoot, "plugins", "TestPlugin");
        Directory.CreateDirectory(destDir);
        File.WriteAllText(Path.Combine(destDir, "old.txt"), "old");
        var zipPath = CreatePluginZip("TestPlugin-1.0.0.zip", "TestPlugin", "1.0.0");
        SetupArgs(new Dictionary<Arguments, string> { [Arguments.PluginsInstall] = zipPath, [Arguments.TargetRepo] = _repoRoot });

        await _handler.Invoking(h => h.ExecuteAsync(["--install", zipPath, "--path", _repoRoot], TestContext.Current.CancellationToken))
            .Should().ThrowAsync<InvalidOperationException>();

        File.Exists(Path.Combine(destDir, "old.txt")).Should().BeTrue();
    }

    [Fact]
    public async Task Install_DestinationExistsWithForce_OverwritesEntirely()
    {
        var destDir = Path.Combine(_repoRoot, "plugins", "TestPlugin");
        Directory.CreateDirectory(destDir);
        File.WriteAllText(Path.Combine(destDir, "old.txt"), "old");
        var zipPath = CreatePluginZip("TestPlugin-1.0.0.zip", "TestPlugin", "1.0.0");
        SetupArgs(new Dictionary<Arguments, string> { [Arguments.PluginsInstall] = zipPath, [Arguments.PluginsForce] = string.Empty, [Arguments.TargetRepo] = _repoRoot });

        await _handler.ExecuteAsync(["--install", zipPath, "--force", "--path", _repoRoot], TestContext.Current.CancellationToken);

        File.Exists(Path.Combine(destDir, "old.txt")).Should().BeFalse();
        File.Exists(Path.Combine(destDir, "plugin.json")).Should().BeTrue();
    }

    [Fact]
    public async Task Uninstall_ExistingFolder_DeletesFolderAndDeactivates()
    {
        var destDir = Path.Combine(_repoRoot, "plugins", "TestPlugin");
        Directory.CreateDirectory(destDir);
        File.WriteAllText(Path.Combine(destDir, "plugin.json"), "{}");
        ArrangeValidRepoConfig(["TestPlugin", "OtherPlugin"]);
        SetupArgs(new Dictionary<Arguments, string> { [Arguments.PluginsUninstall] = "TestPlugin", [Arguments.TargetRepo] = _repoRoot });
        string? written = null;
        _mockFileSystem.WriteAllTextAsync(Arg.Is<string>(s => s.EndsWith(".adrplus")), Arg.Do<string>(c => written = c), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        await _handler.ExecuteAsync(["--uninstall", "TestPlugin", "--path", _repoRoot], TestContext.Current.CancellationToken);

        Directory.Exists(destDir).Should().BeFalse();
        written.Should().NotBeNull();
        using var doc = JsonDocument.Parse(written!);
        doc.RootElement.GetProperty("activeplugins").EnumerateArray().Select(e => e.GetString())
            .Should().BeEquivalentTo(["OtherPlugin"]);
        _mockConsole.Received(1).PromptWriteSuccess(Arg.Is<string>(s => s.Contains("TestPlugin")));
    }

    [Fact]
    public async Task Uninstall_FolderAlreadyGone_StillDeactivatesWithoutThrowing()
    {
        ArrangeValidRepoConfig(["TestPlugin"]);
        SetupArgs(new Dictionary<Arguments, string> { [Arguments.PluginsUninstall] = "TestPlugin", [Arguments.TargetRepo] = _repoRoot });
        string? written = null;
        _mockFileSystem.WriteAllTextAsync(Arg.Is<string>(s => s.EndsWith(".adrplus")), Arg.Do<string>(c => written = c), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        await _handler.ExecuteAsync(["--uninstall", "TestPlugin", "--path", _repoRoot], TestContext.Current.CancellationToken);

        written.Should().NotBeNull();
        using var doc = JsonDocument.Parse(written!);
        doc.RootElement.GetProperty("activeplugins").EnumerateArray().Should().BeEmpty();
    }

    [Fact]
    public async Task Wizard_InstallSelected_PromptsForZipAndInstalls()
    {
        var zipPath = CreatePluginZip("WizardPlugin-1.0.0.zip", "WizardPlugin", "1.0.0");
        _mockAdrServices.ParseArgs(Arg.Any<string[]>(), Arg.Any<Arguments[]>())
            .Returns(new Dictionary<Arguments, string> { [Arguments.WizardPlugins] = string.Empty });
        _mockFileSystem.GetDrives().Returns(["C:\\"]);
        _mockConsole.PromptSelectFolderPath(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<string>(), _mockFileSystem, _mockValidateConfig, Arg.Any<CancellationToken>())
            .Returns((false, _repoRoot));
        _mockConsole.PromptSelectPluginsMode(Arg.Any<CancellationToken>()).Returns((false, PluginsWizardMode.Install));
        _mockConsole.PromptInputPluginZipPath(_mockFileSystem, Arg.Any<CancellationToken>()).Returns((false, zipPath));
        _mockConsole.PromptConfirm(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((false, false));

        await _handler.ExecuteAsync(["--wizard"], TestContext.Current.CancellationToken);

        var destDir = Path.Combine(_repoRoot, "plugins", "WizardPlugin");
        File.Exists(Path.Combine(destDir, "plugin.json")).Should().BeTrue();
        _mockConsole.Received(1).PromptWriteSuccess(Arg.Is<string>(s => s.Contains("WizardPlugin")));
    }

    [Fact]
    public async Task Wizard_UninstallSelected_MultiSelect_UninstallsEachOneAtATime()
    {
        var destDirA = Path.Combine(_repoRoot, "plugins", "WizardPluginA");
        var destDirB = Path.Combine(_repoRoot, "plugins", "WizardPluginB");
        Directory.CreateDirectory(destDirA);
        Directory.CreateDirectory(destDirB);
        File.WriteAllText(Path.Combine(destDirA, "plugin.json"), "{}");
        File.WriteAllText(Path.Combine(destDirB, "plugin.json"), "{}");
        ArrangeValidRepoConfig(["WizardPluginA", "WizardPluginB"]);
        _mockAdrServices.ParseArgs(Arg.Any<string[]>(), Arg.Any<Arguments[]>())
            .Returns(new Dictionary<Arguments, string> { [Arguments.WizardPlugins] = string.Empty });
        _mockFileSystem.GetDrives().Returns(["C:\\"]);
        _mockConsole.PromptSelectFolderPath(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<string>(), _mockFileSystem, _mockValidateConfig, Arg.Any<CancellationToken>())
            .Returns((false, _repoRoot));
        _mockConsole.PromptSelectPluginsMode(Arg.Any<CancellationToken>()).Returns((false, PluginsWizardMode.Uninstall));
        _mockConsole.PromptSelectPluginsToUninstall(Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>())
            .Returns((false, new[] { "WizardPluginA", "WizardPluginB" }));
        _mockFileSystem.WriteAllTextAsync(Arg.Is<string>(s => s.EndsWith(".adrplus")), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        await _handler.ExecuteAsync(["--wizard"], TestContext.Current.CancellationToken);

        Directory.Exists(destDirA).Should().BeFalse();
        Directory.Exists(destDirB).Should().BeFalse();
        _mockConsole.Received(1).PromptWriteSuccess(Arg.Is<string>(s => s.StartsWith("Plugin uninstalled: WizardPluginA")));
        _mockConsole.Received(1).PromptWriteSuccess(Arg.Is<string>(s => s.StartsWith("Plugin uninstalled: WizardPluginB")));
    }
}
