// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the AdrPlus project under MIT license
// ***************************************************************************************

namespace AdrPlus.Tests.Helpers;

/// <summary>
/// Provides cross-platform path utilities for tests that work correctly on both Windows and Linux.
/// </summary>
internal static class PathHelper
{
    /// <summary>
    /// Creates a cross-platform repository path that works on both Windows and Linux.
    /// </summary>
    /// <remarks>
    /// On Windows, returns "C:\repo\docs\adr".
    /// On Linux, returns "/repo/docs/adr".
    /// </remarks>
    public static string GetRepositoryAdrPath()
    {
        if (OperatingSystem.IsWindows())
        {
            return @"C:\repo\docs\adr";
        }
        return "/repo/docs/adr";
    }

    /// <summary>
    /// Creates a cross-platform file path within the ADR repository.
    /// </summary>
    public static string GetAdrFilePath(string fileName)
    {
        var basePath = GetRepositoryAdrPath();
        return Path.Combine(basePath, fileName);
    }

    /// <summary>
    /// Creates a cross-platform alternative folder path for testing file not in ADR folder scenarios.
    /// </summary>
    /// <remarks>
    /// On Windows, returns "C:\repo\other".
    /// On Linux, returns "/repo/other".
    /// </remarks>
    public static string GetAlternativeFolderPath()
    {
        if (OperatingSystem.IsWindows())
        {
            return @"C:\repo\other";
        }
        return "/repo/other";
    }

    /// <summary>
    /// Gets a cross-platform file path in the alternative folder.
    /// </summary>
    public static string GetAlternativeFolderFilePath(string fileName)
    {
        var basePath = GetAlternativeFolderPath();
        return Path.Combine(basePath, fileName);
    }

    /// <summary>
    /// Gets array of cross-platform drive paths for testing drive selection.
    /// </summary>
    /// <remarks>
    /// On Windows, returns ["C:\", "D:\"].
    /// On Linux, returns ["/", "/mnt"].
    /// </remarks>
    public static string[] GetTestDrives()
    {
        if (OperatingSystem.IsWindows())
        {
            return [@"C:\", @"D:\"];
        }
        return ["/", "/mnt"];
    }

    /// <summary>
    /// Gets a single test drive path.
    /// </summary>
    /// <remarks>
    /// On Windows, returns "C:\".
    /// On Linux, returns "/".
    /// </remarks>
    public static string GetSingleTestDrive()
    {
        if (OperatingSystem.IsWindows())
        {
            return @"C:\";
        }
        return "/";
    }

    /// <summary>
    /// Gets the cross-platform application config file path.
    /// </summary>
    /// <remarks>
    /// On Windows, returns "C:\config\app.json".
    /// On Linux, returns "/config/app.json".
    /// </remarks>
    public static string GetAppConfigPath()
    {
        if (OperatingSystem.IsWindows())
        {
            return @"C:\config\app.json";
        }
        return "/config/app.json";
    }

    /// <summary>
    /// Gets the cross-platform repository config file path.
    /// </summary>
    /// <remarks>
    /// On Windows, returns "C:\repo\.adrplus".
    /// On Linux, returns "/repo/.adrplus".
    /// </remarks>
    public static string GetRepoConfigPath()
    {
        if (OperatingSystem.IsWindows())
        {
            return @"C:\repo\.adrplus";
        }
        return "/repo/.adrplus";
    }

    /// <summary>
    /// Gets the cross-platform ADR template config path.
    /// </summary>
    /// <remarks>
    /// On Windows, returns "C:\config\adr-template.md".
    /// On Linux, returns "/config/adr-template.md".
    /// </remarks>
    public static string GetAdrTemplatePath()
    {
        if (OperatingSystem.IsWindows())
        {
            return @"C:\config\adr-template.md";
        }
        return "/config/adr-template.md";
    }

    /// <summary>
    /// Gets the cross-platform templates directory path.
    /// </summary>
    /// <remarks>
    /// On Windows, returns "C:\templates".
    /// On Linux, returns "/templates".
    /// </remarks>
    public static string GetTemplatesDirectoryPath()
    {
        if (OperatingSystem.IsWindows())
        {
            return @"C:\templates";
        }
        return "/templates";
    }

    /// <summary>
    /// Gets a cross-platform template file path.
    /// </summary>
    public static string GetTemplateFilePath(string fileName)
    {
        var basePath = GetTemplatesDirectoryPath();
        return Path.Combine(basePath, fileName);
    }

    /// <summary>
    /// Gets a cross-platform project repository path (for init wizard scenarios).
    /// </summary>
    /// <remarks>
    /// On Windows, returns "C:\projects\{projectName}".
    /// On Linux, returns "/projects/{projectName}".
    /// </remarks>
    public static string GetProjectRepositoryPath(string projectName)
    {
        if (OperatingSystem.IsWindows())
        {
            return Path.Combine(@"C:\projects", projectName);
        }
        return Path.Combine("/projects", projectName);
    }

    /// <summary>
    /// Gets a cross-platform nonexistent path for error testing.
    /// </summary>
    /// <remarks>
    /// On Windows, returns "C:\nonexistent".
    /// On Linux, returns "/nonexistent".
    /// </remarks>
    public static string GetNonexistentPath()
    {
        if (OperatingSystem.IsWindows())
        {
            return @"C:\nonexistent";
        }
        return "/nonexistent";
    }

    /// <summary>
    /// Gets an alternative drive path for multi-drive scenarios.
    /// </summary>
    /// <remarks>
    /// On Windows, returns "D:\".
    /// On Linux, returns "/mnt".
    /// </remarks>
    public static string GetAlternativeDrivePath()
    {
        if (OperatingSystem.IsWindows())
        {
            return @"D:\";
        }
        return "/mnt";
    }

    /// <summary>
    /// Gets the init base repository path (for --path argument scenarios).
    /// </summary>
    /// <remarks>
    /// On Windows, returns "C:\repo".
    /// On Linux, returns "/repo".
    /// </remarks>
    public static string GetInitRepositoryPath()
    {
        if (OperatingSystem.IsWindows())
        {
            return @"C:\repo";
        }
        return "/repo";
    }

    /// <summary>
    /// Gets a scope-specific directory path within a repository.
    /// </summary>
    /// <remarks>
    /// On Windows, returns "{basePath}\{scope}".
    /// On Linux, returns "{basePath}/{scope}".
    /// </remarks>
    public static string GetScopeDirectoryPath(string basePath, string scope)
    {
        return Path.Combine(basePath, scope);
    }
}
