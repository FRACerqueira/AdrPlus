// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the AdrPlus project under MIT license
// ***************************************************************************************

namespace AdrPlus.Tests.Helpers;

/// <summary>
/// Provides standardized test paths and file names for use across all test suites.
/// Ensures consistency and maintainability of cross-platform test paths.
/// </summary>
internal static class TestPathData
{
    /// <summary>
    /// Gets the cross-platform path to a valid ADR file.
    /// </summary>
    public static string ValidAdrFilePath => PathHelper.GetAdrFilePath("adr-0001.md");

    /// <summary>
    /// Gets the cross-platform path to a missing ADR file.
    /// </summary>
    public static string MissingAdrFilePath => PathHelper.GetAdrFilePath("missing.md");

    /// <summary>
    /// Gets the cross-platform path to an ADR file without extension.
    /// </summary>
    public static string AdrFileWithoutExtensionPath => PathHelper.GetAdrFilePath("adr-0001");

    /// <summary>
    /// Gets the cross-platform path to an ADR file with extension (for file without ext tests).
    /// </summary>
    public static string AdrFileWithExtensionPath => PathHelper.GetAdrFilePath("adr-0001.md");

    /// <summary>
    /// Gets the cross-platform path to an invalid file name ADR file.
    /// </summary>
    public static string InvalidFileNameAdrPath => PathHelper.GetAdrFilePath("invalid.md");

    /// <summary>
    /// Gets the cross-platform path to an ADR file outside the ADR folder.
    /// </summary>
    public static string FileOutsideAdrFolderPath => PathHelper.GetAlternativeFolderFilePath("adr-0001.md");

    /// <summary>
    /// Gets the cross-platform config file path.
    /// </summary>
    public static string ConfigFilePath => Path.Combine(PathHelper.GetRepositoryAdrPath(), ".adrplus");

    /// <summary>
    /// Gets the repository base path.
    /// </summary>
    public static string RepositoryPath => PathHelper.GetRepositoryAdrPath();

    /// <summary>
    /// Gets array of test drives.
    /// </summary>
    public static string[] TestDrives => PathHelper.GetTestDrives();

    /// <summary>
    /// Gets single test drive.
    /// </summary>
    public static string SingleTestDrive => PathHelper.GetSingleTestDrive();

    /// <summary>
    /// Gets the selected drive for mock drive selection tests.
    /// On Windows, returns "C:\\". On Linux, returns "/".
    /// </summary>
    public static string SelectedDrive => PathHelper.GetSingleTestDrive();

    /// <summary>
    /// Gets arguments array for invalid date format test (cross-platform).
    /// </summary>
    public static string[] InvalidDateFormatArgs => ["--file", ValidAdrFilePath, "--refdate", "invalid-date"];

    /// <summary>
    /// Gets arguments array for custom date test (cross-platform).
    /// </summary>
    public static string[] CustomDateArgs => ["--file", ValidAdrFilePath, "--refdate", "2026-01-15"];

    /// <summary>
    /// Gets arguments array for file without extension test (cross-platform).
    /// </summary>
    public static string[] FileWithoutExtensionArgs => ["--file", AdrFileWithoutExtensionPath];

    /// <summary>
    /// Gets arguments array for simple file test (cross-platform).
    /// </summary>
    public static string[] SimpleFileArgs => ["--file", ValidAdrFilePath];

    /// <summary>
    /// Gets the application config file path.
    /// </summary>
    public static string AppConfigPath => PathHelper.GetAppConfigPath();

    /// <summary>
    /// Gets the repository config file path.
    /// </summary>
    public static string RepoConfigPath => PathHelper.GetRepoConfigPath();

    /// <summary>
    /// Gets the ADR template config path.
    /// </summary>
    public static string AdrTemplateConfigPath => PathHelper.GetAdrTemplatePath();

    /// <summary>
    /// Gets the templates directory path.
    /// </summary>
    public static string TemplatesDirectory => PathHelper.GetTemplatesDirectoryPath();

    /// <summary>
    /// Gets a cross-platform template file path.
    /// </summary>
    public static string CustomTemplatePath => PathHelper.GetTemplateFilePath("custom.md");

    /// <summary>
    /// Gets test drives for Windows/Linux.
    /// </summary>
    public static string[] MultipleTestDrives => PathHelper.GetTestDrives();

    /// <summary>
    /// Gets the init repository base path.
    /// </summary>
    public static string InitRepositoryPath => PathHelper.GetInitRepositoryPath();

    /// <summary>
    /// Gets the init repository ADR path.
    /// </summary>
    public static string InitRepositoryAdrPath => Path.Combine(InitRepositoryPath, "docs", "adr");

    /// <summary>
    /// Gets a project repository path for wizard scenarios.
    /// </summary>
    public static string ProjectRepositoryPath => PathHelper.GetProjectRepositoryPath("myrepo");

    /// <summary>
    /// Gets the project repository ADR path.
    /// </summary>
    public static string ProjectRepositoryAdrPath => Path.Combine(ProjectRepositoryPath, "docs", "adr");

    /// <summary>
    /// Gets a nonexistent path for error testing.
    /// </summary>
    public static string NonexistentPath => PathHelper.GetNonexistentPath();

    /// <summary>
    /// Gets an alternative drive path.
    /// </summary>
    public static string AlternativeDrivePath => PathHelper.GetAlternativeDrivePath();

    /// <summary>
    /// Gets an alternative project repository path on alternative drive.
    /// </summary>
    public static string AlternativeDriveProjectPath => 
        OperatingSystem.IsWindows() 
            ? Path.Combine(@"D:\projects", "myrepo")
            : Path.Combine("/mnt/projects", "myrepo");

    /// <summary>
    /// Gets the scope directory path for a given scope name.
    /// </summary>
    public static string GetScopePath(string scope) => PathHelper.GetScopeDirectoryPath(InitRepositoryAdrPath, scope);

    /// <summary>
    /// Gets config file path within init repository.
    /// </summary>
    public static string InitConfigPath => Path.Combine(InitRepositoryAdrPath, ".adrplus");
}
