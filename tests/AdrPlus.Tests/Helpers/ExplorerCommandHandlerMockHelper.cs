// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the AdrPlus project under MIT license
// ***************************************************************************************

using AdrPlus.Commands;
using AdrPlus.Core;
using AdrPlus.Domain;
using AdrPlus.Infrastructure.FileSystem;
using AdrPlus.Infrastructure.UI;
using NSubstitute;

namespace AdrPlus.Tests.Helpers;

/// <summary>
/// Provides helper methods for setting up mocks and test fixtures specific to ExplorerCommandHandler tests.
/// Ensures consistent, cross-platform mock behavior and reduces test boilerplate.
/// </summary>
internal static class ExplorerCommandHandlerMockHelper
{
    /// <summary>
    /// Creates a minimal valid ADR repository configuration JSON string that includes FolderAdr.
    /// </summary>
    /// <remarks>
    /// This is used for report generation tests that require the FolderAdr field.
    /// </remarks>
    public static string BuildValidJsonConfigForExplorer()
    {
        return """{"Prefix":"ADR","LenSeq":4,"FolderAdr":"adr"}""";
    }

    /// <summary>
    /// Sets up basic explorer command mocks for a standard test scenario.
    /// </summary>
    public static void SetupBasicExplorerMocks(
        IAdrServices mockAdrServices,
        IFileSystemService mockFileSystem,
        IValidateJsonConfig mockValidateConfig,
        Dictionary<Arguments, string> parsedArgs,
        string targetPath)
    {
        var configPath = Path.Combine(targetPath, "adr-config.adrplus");
        var jsonConfig = BuildValidJsonConfigForExplorer();

        mockAdrServices.ParseArgs(Arg.Any<string[]>(), Arg.Any<Arguments[]>()).Returns(parsedArgs);
        mockValidateConfig.HasTemplateRepoFile().Returns(true);
        mockValidateConfig.GetFileNameRepoConfig().Returns("adr-config.adrplus");

        mockFileSystem.DirectoryExists(targetPath).Returns(true);
        mockFileSystem.FileExists(configPath).Returns(true);
        mockFileSystem.ReadAllTextAsync(Arg.Is<string>(s => s == configPath), Arg.Any<CancellationToken>())
            .Returns(jsonConfig);

        mockValidateConfig.ValidateRepoStructure(jsonConfig).Returns((true, []));
    }

    /// <summary>
    /// Sets up explorer mocks for report generation test scenario.
    /// </summary>
    public static void SetupExplorerReportMocks(
        IAdrServices mockAdrServices,
        IFileSystemService mockFileSystem,
        IValidateJsonConfig mockValidateConfig,
        Dictionary<Arguments, string> parsedArgs,
        string targetPath,
        string reportPath,
        AdrFileNameComponents[] adrFiles)
    {
        SetupBasicExplorerMocks(mockAdrServices, mockFileSystem, mockValidateConfig,  parsedArgs, targetPath);

        var reportDir = Path.GetDirectoryName(reportPath) ?? string.Empty;
        mockFileSystem.DirectoryExists(reportDir).Returns(true);
        mockFileSystem.ReadAllTextAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(BuildValidJsonConfigForExplorer());

        mockAdrServices.ReadAllAdr(mockFileSystem, targetPath, Arg.Any<AdrPlusRepoConfig>(), Arg.Any<bool>())
            .Returns(adrFiles);
    }

    /// <summary>
    /// Creates a valid ADR file with minimal properties for testing.
    /// </summary>
    public static AdrFileNameComponents CreateTestAdrFile(
        string fileName = "ADR-001-test.md",
        DateTime? dateCreate = null,
        AdrStatus statusCreate = AdrStatus.Accepted)
    {
        return new AdrFileNameComponents
        {
            FileName = fileName,
            Header = new AdrHeader
            {
                DateCreate = dateCreate ?? DateTime.Now,
                StatusCreate = statusCreate
            }
        };
    }

    /// <summary>
    /// Creates multiple ADR files for testing report generation with varied content.
    /// </summary>
    public static AdrFileNameComponents[] CreateTestAdrFiles(int count = 3)
    {
        var files = new AdrFileNameComponents[count];
        for (int i = 1; i <= count; i++)
        {
            files[i - 1] = CreateTestAdrFile(
                $"ADR-{i:0000}-test.md",
                DateTime.Now.AddDays(-(count - i)),
                i % 2 == 0 ? AdrStatus.Accepted : AdrStatus.Proposed
            );
        }
        return files;
    }

    /// <summary>
    /// Gets cross-platform explorer field strings for report generation testing.
    /// Field format: "1-File", "2-Folder", "3-Format", "4-Status", etc.
    /// </summary>
    public static string[] GetExplorerFields(params int[] fieldNumbers)
    {
        var fieldNames = new[] { "File", "Folder", "Format", "Status", "Created", "Updated", "Scope", "Domain" };
        return [.. fieldNumbers.Select(n => $"{n}-{fieldNames[n - 1]}")];
    }

    /// <summary>
    /// Gets all default explorer fields.
    /// </summary>
    public static string[] GetAllExplorerFields() => GetExplorerFields(1, 2, 3, 4, 5, 6, 7, 8);

    /// <summary>
    /// Gets a minimal set of explorer fields (File and Status only).
    /// </summary>
    public static string[] GetMinimalExplorerFields() => GetExplorerFields(1, 4);
}
