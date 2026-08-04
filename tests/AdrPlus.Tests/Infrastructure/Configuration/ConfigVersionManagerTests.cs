// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the AdrPlus project under MIT license
// ***************************************************************************************

using AdrPlus.Core;
using AdrPlus.Domain;
using AdrPlus.Infrastructure.Configuration;
using AdrPlus.Infrastructure.FileSystem;
using AdrPlus.Infrastructure.UI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace AdrPlus.Tests.Infrastructure.Configuration;

/// <summary>
/// Unit tests for <see cref="ConfigVersionManager"/>'s version-migration path, focused on the
/// <see cref="AdrPlusConfig.PluginAllowlist"/> round trip (an array field, previously unhandled by the
/// migration's per-property <c>JsonValueKind</c> switch).
/// </summary>
public class ConfigVersionManagerTests
{
    private const string HistoryPath = "/history";
    private const string AppConfigPath = "/app/adrplus.json";
    private const string RepoConfigPath = "/app/template/adr-config.adrplus";

    private readonly IConsoleWriter _prompt = Substitute.For<IConsoleWriter>();
    private readonly ILogger<ConfigVersionManager> _logger = Substitute.For<ILogger<ConfigVersionManager>>();
    private readonly IValidateConfig _validateConfig = Substitute.For<IValidateConfig>();
    private readonly IFileSystemService _fileSystem = Substitute.For<IFileSystemService>();

    private ConfigVersionManager CreateManager(string currentVersion)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { [AppConstants.CfgNameVersionApp] = currentVersion })
            .Build();

        return new ConfigVersionManager(_prompt, _logger, _validateConfig, _fileSystem, configuration);
    }

    [Fact]
    public async Task CheckAndMigrateConfigAsync_WithPluginAllowlistInOldConfig_PreservesItAfterMigration()
    {
        const string oldAppConfigJson = """
            {"DefaultSettings":{"language":"en-us","comandopenadr":"code {0}","withoutargs":"Help","pluginallowlist":[{"name":"confluence","hash":null}]}}
            """;
        const string oldRepoConfigJson = "{}";
        var oldVersionFileContent = JsonSerializer.Serialize(new[] { oldAppConfigJson, oldRepoConfigJson });

        _validateConfig.GetHistoryPath().Returns(HistoryPath);
        _fileSystem.GetFiles(HistoryPath, $"{AppConstants.VersionFilePrefix}*.txt", SearchOption.TopDirectoryOnly)
            .Returns([Path.Combine(HistoryPath, $"{AppConstants.VersionFilePrefix}0.5.0.txt")]);
        _fileSystem.ReadAllTextAsync(Path.Combine(HistoryPath, $"{AppConstants.VersionFilePrefix}0.5.0.txt"), Arg.Any<CancellationToken>())
            .Returns(oldVersionFileContent);
        _validateConfig.GetConfigAdrTemplateAsync(Arg.Any<CancellationToken>()).Returns("# ADR {0}");
        _validateConfig.GetConfigAppFilePath().Returns(AppConfigPath);
        _validateConfig.GetDefaultConfigRepoFilePath().Returns(RepoConfigPath);
        _validateConfig.RecreateVersionFileAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);

        string? writtenAppConfigJson = null;
        _fileSystem.WriteAllTextAsync(AppConfigPath, Arg.Do<string>(json => writtenAppConfigJson = json), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        _fileSystem.WriteAllTextAsync(RepoConfigPath, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var manager = CreateManager("0.6.0");

        var result = await manager.CheckAndMigrateConfigAsync(TestContext.Current.CancellationToken);

        result.Should().BeTrue();
        writtenAppConfigJson.Should().NotBeNull();

        using var writtenDoc = JsonDocument.Parse(writtenAppConfigJson!);
        var pluginAllowlistElement = writtenDoc.RootElement.GetProperty(AppConstants.DefaultSettingsRoot).GetProperty(AppConstants.FieldPluginAllowlist);
        var pluginAllowlist = JsonSerializer.Deserialize<List<PluginAllowlistEntry>>(pluginAllowlistElement.GetRawText(), AppConstants.RepoSerializerOptions);

        pluginAllowlist.Should().ContainSingle(entry => entry.Name == "confluence");
    }
}
