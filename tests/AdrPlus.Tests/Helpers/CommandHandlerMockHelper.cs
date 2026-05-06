// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the AdrPlus project under MIT license
// ***************************************************************************************

using AdrPlus.Commands;
using AdrPlus.Core;
using AdrPlus.Domain;
using AdrPlus.Infrastructure.FileSystem;
using NSubstitute;

namespace AdrPlus.Tests.Helpers;

/// <summary>
/// Provides helper methods for setting up common mocks used across CommandHandler tests.
/// Ensures consistent mock behavior for file system operations and configuration validation.
/// </summary>
internal static class CommandHandlerMockHelper
{
    /// <summary>
    /// Sets up basic mocks for CommandHandler tests with all necessary file system and validation mocks.
    /// Includes mocks for: ParseArgs, HasTemplateRepoFile, FileExists, ReadAllTextAsync, ValidateRepoStructure,
    /// GetFileRootRepositoryPath, and GetFullNameDirectoryByFile.
    /// </summary>
    /// <param name="mockAdrServices">The mock IAdrServices instance to configure.</param>
    /// <param name="mockFileSystem">The mock IFileSystemService instance to configure.</param>
    /// <param name="mockValidateConfig">The mock IValidateJsonConfig instance to configure.</param>
    /// <param name="parsedArgs">The dictionary of parsed arguments to return from ParseArgs.</param>
    /// <param name="jsonConfig">The JSON configuration string to return from ReadAllTextAsync.</param>
    public static void SetupBasicCommandMocks(
        IAdrServices mockAdrServices,
        IFileSystemService mockFileSystem,
        IValidateJsonConfig mockValidateConfig,
        Dictionary<Arguments, string> parsedArgs,
        string jsonConfig)
    {
        mockAdrServices.ParseArgs(Arg.Any<string[]>(), Arg.Any<Arguments[]>()).Returns(parsedArgs);
        mockValidateConfig.HasTemplateRepoFile().Returns(true);
        mockFileSystem.FileExists(Arg.Is<string>(s => s.EndsWith(".md"))).Returns(true);
        mockValidateConfig.GetFileNameRepoConfig().Returns(".adrplus");
        mockFileSystem.FileExists(Arg.Is<string>(s => s.EndsWith(".adrplus"))).Returns(true);
        mockFileSystem.ReadAllTextAsync(Arg.Is<string>(s => s.EndsWith(".adrplus")), Arg.Any<CancellationToken>()).Returns(jsonConfig);
        mockValidateConfig.ValidateRepoStructure(jsonConfig).Returns((true, []));
        mockFileSystem.GetFullNameFile(Arg.Any<string>()).Returns(callInfo =>
        {
            var path = callInfo.Arg<string>();
            return string.IsNullOrEmpty(path) ? "/repo/.adrplus" : Path.GetFullPath(path);
        });

        // Setup GetFileRootRepositoryPath to return a config file path
        mockFileSystem.GetFileRootRepositoryPath(Arg.Any<string>())
            .Returns(callInfo =>
            {
                var filePath = callInfo.Arg<string>();
                var directory = Path.GetDirectoryName(filePath);
                return string.IsNullOrEmpty(directory) ? null : Path.Combine(directory, ".adrplus");
            });

        // Setup GetFullNameDirectoryByFile to return the directory of a file
        mockFileSystem.GetFullNameDirectoryByFile(Arg.Any<string>())
            .Returns(callInfo =>
            {
                var filePath = callInfo.Arg<string>();
                return Path.GetDirectoryName(filePath) ?? string.Empty;
            });
    }

    /// <summary>
    /// Creates an AdrFileNameComponents instance with default valid values for testing.
    /// </summary>
    /// <param name="fileName">The file name for the ADR.</param>
    /// <param name="status">The ADR status update value.</param>
    /// <param name="supersededValue">The optional superseded ADR sequence number.</param>
    /// <returns>A properly configured AdrFileNameComponents instance.</returns>
    public static AdrFileNameComponents CreateValidAdrFileNameComponents(
        string fileName,
        AdrStatus status,
        int? supersededValue = null)
    {
        return new AdrFileNameComponents
        {
            FileName = fileName,
            Number = 1,
            IsValid = true,
            Header = new AdrHeader
            {
                IsValid = true,
                StatusUpdate = status,
                StatusCreate = AdrStatus.Proposed
            },
            SupersededValue = supersededValue
        };
    }

    /// <summary>
    /// Creates an AdrFileNameComponents instance for a migrated ADR with custom properties.
    /// </summary>
    /// <param name="fileName">The file name for the ADR.</param>
    /// <param name="statusCreate">The ADR creation status (typically Unknown for migrated ADRs).</param>
    /// <param name="statusUpdate">The ADR update status value.</param>
    /// <param name="isMigrated">Whether the ADR is marked as migrated.</param>
    /// <returns>A properly configured AdrFileNameComponents instance for a migrated ADR.</returns>
    public static AdrFileNameComponents CreateMigratedAdrFileNameComponents(
        string fileName,
        AdrStatus statusCreate,
        AdrStatus statusUpdate,
        bool isMigrated = true)
    {
        return new AdrFileNameComponents
        {
            FileName = fileName,
            Number = 1,
            IsValid = true,
            Header = new AdrHeader
            {
                IsValid = true,
                StatusCreate = statusCreate,
                StatusUpdate = statusUpdate,
                StatusChange = AdrStatus.Unknown,
                IsMigrated = isMigrated
            }
        };
    }

    /// <summary>
    /// Creates an AdrFileNameComponents instance with invalid values for testing error scenarios.
    /// </summary>
    /// <param name="errorMessage">The error message describing why parsing failed.</param>
    /// <returns>An AdrFileNameComponents instance marked as invalid.</returns>
    public static AdrFileNameComponents CreateInvalidAdrFileNameComponents(string errorMessage)
    {
        return new AdrFileNameComponents
        {
            IsValid = false,
            ErrorMessage = errorMessage
        };
    }

    /// <summary>
    /// Builds a minimal valid JSON configuration string for testing.
    /// </summary>
    /// <returns>A JSON string representing a valid ADR repository configuration.</returns>
    public static string BuildValidJsonConfig()
    {
        return """{"Prefix":"ADR","LenSeq":4,"LenVersion":2,"LenRevision":1,"LenScope":0,"Separator":"-","CaseTransform":"PascalCase","FolderAdr":"adr","Template":"# Context\n","StatusNew":"Proposed","StatusAcc":"Accepted","StatusRej":"Rejected","StatusSup":"Superseded"}""";
    }

    /// <summary>
    /// Sets up GetFileRootRepositoryPath mock to simulate finding the repository config file.
    /// </summary>
    public static void SetupGetFileRootRepositoryPathMock(IFileSystemService mockFileSystem)
    {
        mockFileSystem.GetFileRootRepositoryPath(Arg.Any<string>())
            .Returns(callInfo =>
            {
                var filePath = callInfo.Arg<string>();
                var directory = Path.GetDirectoryName(filePath);
                return string.IsNullOrEmpty(directory) ? null : Path.Combine(directory, ".adrplus");
            });
    }

    /// <summary>
    /// Sets up GetFullNameDirectoryByFile mock to extract directory from file path.
    /// </summary>
    public static void SetupGetFullNameDirectoryByFileMock(IFileSystemService mockFileSystem)
    {
        mockFileSystem.GetFullNameDirectoryByFile(Arg.Any<string>())
            .Returns(callInfo =>
            {
                var filePath = callInfo.Arg<string>();
                return Path.GetDirectoryName(filePath) ?? string.Empty;
            });
    }

    /// <summary>
    /// Sets up GetFullNameFile mock with proper path handling.
    /// </summary>
    public static void SetupGetFullNameFileMock(IFileSystemService mockFileSystem)
    {
        mockFileSystem.GetFullNameFile(Arg.Any<string>()).Returns(callInfo =>
        {
            var path = callInfo.Arg<string>();
            return string.IsNullOrEmpty(path) ? "/repo/.adrplus" : Path.GetFullPath(path);
        });
    }
}
