// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the AdrPlus project under MIT license
// ***************************************************************************************

using AdrPlus.Commands;
using AdrPlus.Domain;
using AdrPlus.Infrastructure.FileSystem;

namespace AdrPlus.Core
{
    internal interface IAdrServices
    {
        /// <summary>
        /// Opens a file using the platform-appropriate shell command.
        /// On Windows uses <c>cmd.exe /c</c>; on Linux/macOS uses <c>sh -c</c>;
        /// on other platforms falls back to shell execute.
        /// </summary>
        /// <param name="filepath">The full path to the file to open.</param>
        /// <param name="command">The shell command string used to open the file.</param>
        /// <returns>An empty string on success; otherwise the stderr output or the exception message.</returns>
        string OpenFile(string filepath, string command);

        /// <summary>
        /// Generates a mapping of command names to their handler types.
        /// </summary>
        /// <returns>A dictionary mapping command names to their corresponding handler types.</returns>
        Dictionary<string, Type> GenerateCommandsMap();

        /// <summary>
        /// Gets all available commands with their metadata.
        /// </summary>
        /// <returns>An array of tuples containing the command, its alias, handler type, and description.</returns>
        (CommandsAdr Command, string Alias, Type ConfigCommandHandler, string Description)[] GetCommands();

        /// <summary>
        /// Parses command-line arguments based on expected argument definitions.
        /// </summary>
        /// <param name="args">The command-line arguments to parse.</param>
        /// <param name="argsForCommand">The expected argument definitions for the command.</param>
        /// <returns>A dictionary mapping argument types to their parsed values.</returns>
        Dictionary<Arguments, string> ParseArgs(string[] args, Arguments[] argsForCommand);

        /// <summary>
        /// Generates help text for a command.
        /// </summary>
        /// <param name="command">The command name for which to generate help.</param>
        /// <param name="argsForCommand">The arguments supported by the command.</param>
        /// <param name="examples">Example usage strings for the command.</param>
        /// <returns>Formatted help text for the command.</returns>
        string GetHelpText(string command, Arguments[] argsForCommand, string[] examples);

        /// <summary>
        /// Updates the status of an ADR file asynchronously.
        /// </summary>
        /// <param name="fullpath">The full path to the ADR file.</param>
        /// <param name="adrStatus">The new status to set for the ADR.</param>
        /// <param name="dref">The reference date for the status update.</param>
        /// <param name="config">The ADR Plus repository configuration.</param>
        /// <param name="fileSystemService">The file system service used to access and modify the file.</param>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <returns>A tuple containing a boolean indicating success, an error message if applicable, and the updated <see cref="AdrRecord"/> and rendered file content (both <see langword="null"/> on failure).</returns>
        Task<(bool Isvalid, string Error, AdrRecord? Record, string? Content)> StatusUpdateAdrAsync(string fullpath, AdrStatus adrStatus, DateTime dref, AdrPlusRepoConfig config, IFileSystemService fileSystemService, CancellationToken cancellationToken);

        /// <summary>
        /// Changes the status of an ADR to superseded by another ADR asynchronously.
        /// </summary>
        /// <param name="fullpath">The full path to the ADR file to be marked as superseded.</param>
        /// <param name="filename">The filename of the superseding ADR.</param>
        /// <param name="dref">The reference date for the status change.</param>
        /// <param name="config">The ADR Plus repository configuration.</param>
        /// <param name="fileSystemService">The file system service used to access and modify the file.</param>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <returns>A tuple containing a boolean indicating success, an error message if applicable, and the updated <see cref="AdrRecord"/> and rendered file content (both <see langword="null"/> on failure).</returns>
        Task<(bool IsValid, string Error, AdrRecord? Record, string? Content)> StatusChangeSupersedeAdrAsync(string fullpath, string filename, DateTime dref, AdrPlusRepoConfig config, IFileSystemService fileSystemService, CancellationToken cancellationToken);

        /// <summary>
        /// Changes the status of an ADR asynchronously.
        /// </summary>
        /// <param name="fullpath">The full path to the ADR file.</param>
        /// <param name="adrStatus">The new status to set for the ADR.</param>
        /// <param name="dref">The reference date for the status change.</param>
        /// <param name="config">The ADR Plus repository configuration.</param>
        /// <param name="fileSystemService">The file system service used to access and modify the file.</param>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <returns>A tuple containing a boolean indicating success, an error message if applicable, and the updated <see cref="AdrRecord"/> and rendered file content (both <see langword="null"/> on failure).</returns>
        Task<(bool IsValid, string Error, AdrRecord? Record, string? Content)> StatusChangeAdrAsync(string fullpath, AdrStatus adrStatus, DateTime dref, AdrPlusRepoConfig config, IFileSystemService fileSystemService, CancellationToken cancellationToken);


        /// <summary>
        /// Reads all ADR files with a specific sequence number asynchronously.
        /// </summary>
        /// <param name="sequence">The sequence number to search for.</param>
        /// <param name="fileSystemService">The file system service used to access files.</param>
        /// <param name="directoryPath">The directory path to search in.</param>
        /// <param name="config">The ADR Plus repository configuration.</param>
        /// <returns>An array of <see cref="AdrFileNameComponents"/> matching the sequence.</returns>
        Task<AdrFileNameComponents[]> ReadAllAdrByNumber(int sequence, IFileSystemService fileSystemService, string directoryPath, AdrPlusRepoConfig config);

        /// <summary>
        /// Reads all ADR files asynchronously.
        /// </summary>
        /// <param name="fileSystemService">The file system service used to access files.</param>
        /// <param name="directoryPath">The directory path to search in.</param>
        /// <param name="config">The ADR Plus repository configuration.</param>
        /// <param name="includeNotMatched">Indicates whether to include ADR files that do not match the configured naming conventions.</param>
        /// <param name="includeContent">
        /// Whether to materialize each file's body (<see cref="AdrFileNameComponents.ContentAdr"/>) past the
        /// header table. Pass <see langword="false"/> when the caller only needs filename/header fields (e.g.
        /// scope/domain/number lookups) - skips a wasteful re-join of the file's lines that would otherwise be
        /// allocated and immediately discarded.
        /// </param>
        /// <returns>An array of all <see cref="AdrFileNameComponents"/> found.</returns>
        Task<AdrFileNameComponents[]> ReadAllAdr(IFileSystemService fileSystemService, string directoryPath, AdrPlusRepoConfig config, bool includeNotMatched = false, bool includeContent = true);

        /// <summary>
        /// Gets the file path of an ADR with a unique title asynchronously.
        /// </summary>
        /// <param name="title">The title of the ADR to find.</param>
        /// <param name="fileSystemService">The file system service used to access files.</param>
        /// <param name="rootrepo">The root path repository</param>
        /// <param name="config">The ADR Plus repository configuration.</param>
        /// <returns>The file path of the ADR with the matching title.</returns>
        Task<string> GetFileByUniqueTitle(string title, IFileSystemService fileSystemService, string rootrepo, AdrPlusRepoConfig config);

        /// <summary>
        /// Gets the next available ADR sequence number asynchronously.
        /// </summary>
        /// <param name="fileSystemService">The file system service used to access files.</param>
        /// <param name="directoryPath">The directory path to search in.</param>
        /// <param name="config">The ADR Plus repository configuration.</param>
        /// <returns>The next available sequence number.</returns>
        Task<int> GetNextNumber(IFileSystemService fileSystemService, string directoryPath, AdrPlusRepoConfig config);

        /// <summary>
        /// Gets the latest ADR with a specific sequence number asynchronously.
        /// </summary>
        /// <param name="sequence">The sequence number to search for.</param>
        /// <param name="fileSystemService">The file system service used to access files.</param>
        /// <param name="directoryPath">The directory path to search in.</param>
        /// <param name="config">The ADR Plus repository configuration.</param>
        /// <returns>The <see cref="AdrFileNameComponents"/> of the latest ADR with the specified sequence, or null if not found.</returns>
        Task<AdrFileNameComponents?> GetLatestADRSequence(int sequence, IFileSystemService fileSystemService, string directoryPath, AdrPlusRepoConfig config);

        /// <summary>
        /// Gets all available domains asynchronously.
        /// </summary>
        /// <param name="fileSystemService">The file system service used to access files.</param>
        /// <param name="directoryPath">The directory path to search in.</param>
        /// <param name="config">The ADR Plus repository configuration.</param>
        /// <returns>An array of domain names found in the repository.</returns>
        Task<string[]> GetDomains(IFileSystemService fileSystemService, string directoryPath, AdrPlusRepoConfig config);

        /// <summary>
        /// Gets all available scopes asynchronously.
        /// </summary>
        /// <param name="fileSystemService">The file system service used to access files.</param>
        /// <param name="directoryPath">The directory path to search in.</param>
        /// <param name="config">The ADR Plus repository configuration.</param>
        /// <returns>An array of scope names found in the repository.</returns>
        Task<string[]> GetScopes(IFileSystemService fileSystemService, string directoryPath, AdrPlusRepoConfig config);

        /// <summary>
        /// Pure, I/O-free variant of <see cref="GetFileByUniqueTitle"/> for a caller that already has an
        /// <see cref="AdrFileNameComponents"/> array on hand (e.g. shared across several lookups in the same
        /// command, instead of each one re-reading the repository).
        /// </summary>
        /// <param name="title">The title of the ADR to find.</param>
        /// <param name="adrFiles">A previously read set of ADR files, e.g. from <see cref="ReadAllAdr"/>.</param>
        /// <param name="config">The ADR Plus repository configuration.</param>
        /// <returns>The file path of the ADR with the matching title, or <see cref="string.Empty"/> if none.</returns>
        string GetFileByUniqueTitleFrom(string title, AdrFileNameComponents[] adrFiles, AdrPlusRepoConfig config);

        /// <summary>
        /// Pure, I/O-free variant of <see cref="GetNextNumber"/> for a caller that already has an
        /// <see cref="AdrFileNameComponents"/> array on hand.
        /// </summary>
        /// <param name="adrFiles">A previously read set of ADR files, e.g. from <see cref="ReadAllAdr"/>.</param>
        /// <returns>The next available sequence number.</returns>
        int GetNextNumberFrom(AdrFileNameComponents[] adrFiles);

        /// <summary>
        /// Pure, I/O-free variant of <see cref="GetDomains"/> for a caller that already has an
        /// <see cref="AdrFileNameComponents"/> array on hand.
        /// </summary>
        /// <param name="adrFiles">A previously read set of ADR files, e.g. from <see cref="ReadAllAdr"/>.</param>
        /// <returns>An array of domain names found among <paramref name="adrFiles"/>.</returns>
        string[] GetDomainsFrom(AdrFileNameComponents[] adrFiles);

        /// <summary>
        /// Pure, I/O-free variant of <see cref="GetScopes"/> for a caller that already has an
        /// <see cref="AdrFileNameComponents"/> array on hand.
        /// </summary>
        /// <param name="adrFiles">A previously read set of ADR files, e.g. from <see cref="ReadAllAdr"/>.</param>
        /// <returns>An array of scope names found among <paramref name="adrFiles"/>.</returns>
        string[] GetScopesFrom(AdrFileNameComponents[] adrFiles);

        /// <summary>
        /// Parses the header and content from an ADR file asynchronously.
        /// </summary>
        /// <param name="filePath">The path to the ADR file to parse.</param>
        /// <param name="config">The ADR Plus repository configuration.</param>
        /// <param name="fileSystemService">The file system service used to read the file.</param>
        /// <param name="includeContent">
        /// Whether to materialize the body past the header table. Pass <see langword="false"/> when the caller
        /// only needs header fields - the returned <c>content</c> is <see cref="string.Empty"/> in that case.
        /// </param>
        /// <returns>A tuple containing the parsed <see cref="AdrHeader"/> and the remaining file content.</returns>
        Task<(AdrHeader header, string content)> ParseAdrHeaderAndContentAsync(string filePath, AdrPlusRepoConfig config, IFileSystemService fileSystemService, bool includeContent = true);

        /// <summary>
        /// Parses the file name components from an ADR file path asynchronously.
        /// </summary>
        /// <param name="filePath">The path to the ADR file.</param>
        /// <param name="config">The ADR Plus repository configuration.</param>
        /// <param name="fileSystemService">The file system service used to access file information.</param>
        /// <param name="includeContent">
        /// Whether to materialize <see cref="AdrFileNameComponents.ContentAdr"/> - see
        /// <see cref="ParseAdrHeaderAndContentAsync"/>.
        /// </param>
        /// <returns>An <see cref="AdrFileNameComponents"/> object containing the parsed file name components.</returns>
        Task<AdrFileNameComponents> ParseFileName(string filePath, AdrPlusRepoConfig config, IFileSystemService fileSystemService, bool includeContent = true);

        /// <summary>
        /// Converts a JSON string to an <see cref="AdrPlusRepoConfig"/> object.
        /// </summary>
        /// <param name="jsonString">The JSON string to deserialize.</param>
        /// <param name="template">The template name to associate with the configuration.</param>
        /// <returns>An <see cref="AdrPlusRepoConfig"/> object created from the JSON data.</returns>
        AdrPlusRepoConfig FromJson(string jsonString, string template);
    }
}
