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
/// Provides Supersede-specific mock setup for CommandHandler tests.
/// Handles file path and existence checks with proper domain semantics for supersede operations.
/// </summary>
internal static class SupersedeCommandHandlerMockHelper
{
    /// <summary>
    /// Sets up mocks for Supersede command handler with proper file system behavior.
    /// Configures FileExists to return false for the new superseded file (before write),
    /// allowing the handler to proceed with file creation.
    /// </summary>
    /// <param name="mockAdrServices">The mock IAdrServices instance to configure.</param>
    /// <param name="mockFileSystem">The mock IFileSystemService instance to configure.</param>
    /// <param name="mockValidateConfig">The mock IValidateJsonConfig instance to configure.</param>
    /// <param name="parsedArgs">The dictionary of parsed arguments to return from ParseArgs.</param>
    /// <param name="jsonConfig">The JSON configuration string to return from ReadAllTextAsync.</param>
    public static void SetupSupersedeCommandMocks(
        IAdrServices mockAdrServices,
        IFileSystemService mockFileSystem,
        IValidateJsonConfig mockValidateConfig,
        Dictionary<Arguments, string> parsedArgs,
        string jsonConfig)
    {
        // First, setup the base mocks using the standard helper
        CommandHandlerMockHelper.SetupBasicCommandMocks(
            mockAdrServices,
            mockFileSystem,
            mockValidateConfig,
            parsedArgs,
            jsonConfig);

        // Get the input file path for reference
        var inputFilePath = parsedArgs.TryGetValue(Arguments.FileAdr, out var filePath)
            ? NormalizePath(filePath)
            : "/repo/adr/adr-0001.md";

        // Override FileExists for Supersede-specific behavior
        // New superseded ADR files should NOT exist (they're being created)
        // Input ADR files should exist (they're being superseded)
        mockFileSystem.FileExists(Arg.Any<string>())
            .Returns(callInfo =>
            {
                var path = callInfo.Arg<string>();

                // Config files should exist
                if (path.EndsWith(".adrplus"))
                    return true;

                // Input ADR files (the ones being superseded) should exist
                // Check both with and without .md extension
                if (path.EndsWith(".md"))
                {
                    var normalized = NormalizePath(path).ToLower();

                    // Direct match with input file
                    if (normalized == inputFilePath.ToLower())
                        return true;

                    // Match if input file without extension + .md
                    if (inputFilePath.ToLower().EndsWith(".md"))
                    {
                        var inputWithoutExt = inputFilePath[..^3];
                        if (normalized == (inputWithoutExt + ".md").ToLower())
                            return true;
                    }
                    else
                    {
                        // Input without .md, so .md version should be "input with extension"
                        if (normalized == (inputFilePath + ".md").ToLower())
                            return true;
                    }
                }

                // New superseded ADR files should NOT exist (they're being created)
                if (path.EndsWith(".md"))
                    return false;

                return false;
            });

        // Setup GetFullNameFile to return the provided path as-is
        // This allows tests to control file paths explicitly
        mockFileSystem.GetFullNameFile(Arg.Any<string>())
            .Returns(callInfo =>
            {
                var path = callInfo.Arg<string>();
                return string.IsNullOrEmpty(path) ? "/repo/.adrplus" : path;
            });

        // Ensure GetFileRootRepositoryPath works in all cases
        // Should return the .adrplus config file path
        mockFileSystem.GetFileRootRepositoryPath(Arg.Any<string>())
            .Returns(callInfo =>
            {
                var filePath = callInfo.Arg<string>();
                if (string.IsNullOrEmpty(filePath))
                    return "/repo/.adrplus";

                var directory = Path.GetDirectoryName(filePath);
                if (string.IsNullOrEmpty(directory))
                    return "/repo/.adrplus";

                // Return the config file path in the same directory
                return Path.Combine(directory, ".adrplus");
            });

        // Ensure GetFullNameDirectoryByFile works in all cases
        mockFileSystem.GetFullNameDirectoryByFile(Arg.Any<string>())
            .Returns(callInfo =>
            {
                var filePath = callInfo.Arg<string>();
                if (string.IsNullOrEmpty(filePath))
                    return "/repo";

                var dir = Path.GetDirectoryName(filePath);
                return string.IsNullOrEmpty(dir) ? "/repo" : dir;
            });
    }

    /// <summary>
    /// Determines if a file path corresponds to an input ADR file from parsed arguments.
    /// Handles both cases where the input file has .md extension or not.
    /// </summary>
    private static bool IsInputFile(string filePath, Dictionary<Arguments, string> parsedArgs)
    {
        if (!parsedArgs.TryGetValue(Arguments.FileAdr, out var inputFile))
            return false;

        // Normalize paths for comparison
        var normalizedInput = NormalizePath(inputFile).ToLower();
        var normalizedPath = NormalizePath(filePath).ToLower();

        // Check direct match
        if (normalizedPath == normalizedInput)
            return true;

        // Check if input file without .md matches path with .md
        if (!inputFile.EndsWith(".md"))
        {
            var inputWithMd = NormalizePath(inputFile + ".md").ToLower();
            if (normalizedPath == inputWithMd)
                return true;
        }

        // Check if input file with .md matches path without .md
        if (inputFile.EndsWith(".md"))
        {
            var inputWithoutMd = NormalizePath(inputFile[..^3]).ToLower();
            if (normalizedPath == inputWithoutMd)
                return true;
        }

        return false;
    }

    /// <summary>
    /// Normalizes a file path for consistent comparison.
    /// </summary>
    private static string NormalizePath(string path)
    {
        return Path.GetFullPath(path);
    }
}

