// MIT LICENCE
// The maintenance and evolution is maintained by the AdrPlus project under MIT license
// ***************************************************************************************

using AdrPlus.Commands;
using AdrPlus.Domain;
using AdrPlus.Infrastructure.FileSystem;
using System.Text.Json;

namespace AdrPlus.Core
{
    internal interface IAdrServices
    {
        /// <summary>
        /// Updates the status of an ADR file by reading its header, setting <c>StatusUpdate</c> and <c>DateUpdate</c>,
        /// and writing the updated content back to disk.
        /// </summary>
        /// <param name="fullpath">The full file-system path to the ADR file.</param>
        /// <param name="adrStatus">The new status to set as the update status.</param>
        /// <param name="dref">The date reference to record as the update date.</param>
        /// <param name="config">The repository configuration used for formatting.</param>
        /// <param name="fileSystemService">The file system service used for reading and writing files.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A tuple: <c>Isvalid = true</c> on success; otherwise <c>Isvalid = false</c> and <c>Error</c> contains the reason.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="config"/> or <paramref name="fileSystemService"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="fullpath"/> is null, empty, or whitespace.</exception>
        Task<(bool Isvalid, string Error)> StatusUpdateAdrAsync(string fullpath, AdrStatus adrStatus, DateTime dref, AdrPlusRepoConfig config, IFileSystemService fileSystemService, CancellationToken cancellationToken);

        /// <summary>
        /// Updates the status of an ADR file to <see cref="AdrStatus.Superseded"/> by reading its header,
        /// setting <c>StatusChange</c> and <c>DateChange</c>, appending the superseding filename reference, and writing the updated content back to disk.
        /// </summary>
        /// <param name="fullpath">The full file-system path to the ADR file being superseded.</param>
        /// <param name="filename">The filename of the new ADR that supersedes this one.</param>
        /// <param name="dref">The date reference to record as the change date.</param>
        /// <param name="config">The repository configuration used for formatting.</param>
        /// <param name="fileSystemService">The file system service used for reading and writing files.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A tuple: <c>IsValid = true</c> on success; otherwise <c>IsValid = false</c> and <c>Error</c> contains the reason.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="config"/> or <paramref name="fileSystemService"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="fullpath"/> or <paramref name="filename"/> is null, empty, or whitespace.</exception>
        Task<(bool IsValid, string Error)> StatusChangeSupersedeAdrAsync(string fullpath, string filename, DateTime dref, AdrPlusRepoConfig config, IFileSystemService fileSystemService, CancellationToken cancellationToken);

        /// <summary>
        /// Updates the status of an ADR file by reading its header, setting <c>StatusChange</c> and <c>DateChange</c>,
        /// and writing the updated content back to disk.
        /// </summary>
        /// <param name="fullpath">The full file-system path to the ADR file.</param>
        /// <param name="adrStatus">The new change status to set (e.g. <see cref="AdrStatus.Rejected"/>).</param>
        /// <param name="dref">The date reference to record as the change date.</param>
        /// <param name="config">The repository configuration used for formatting.</param>
        /// <param name="fileSystemService">The file system service used for reading and writing files.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A tuple: <c>IsValid = true</c> on success; otherwise <c>IsValid = false</c> and <c>Error</c> contains the reason.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="config"/> or <paramref name="fileSystemService"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="fullpath"/> is null, empty, or whitespace.</exception>
        Task<(bool IsValid, string Error)> StatusChangeAdrAsync(string fullpath, AdrStatus adrStatus, DateTime dref, AdrPlusRepoConfig config, IFileSystemService fileSystemService, CancellationToken cancellationToken);

        /// <summary>
        /// Deserializes a JSON string produced by <c>adr-config.adrplus</c> into an <see cref="AdrPlusRepoConfig"/> instance.
        /// Only known fields are mapped; unknown fields are silently ignored.
        /// Property lookup is case-insensitive to tolerate hand-edited files.
        /// </summary>
        /// <param name="jsonString">The JSON string containing the repository settings. Must not be null or whitespace.</param>
        /// <param name="template">The default ADR template content used when the JSON does not specify one.</param>
        /// <param name="defaultFolder">The default folder path used when the JSON does not specify a folder.</param>
        /// <returns>A new <see cref="AdrPlusRepoConfig"/> populated with values from <paramref name="jsonString"/>.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="jsonString"/> is null or whitespace.</exception>
        /// <exception cref="JsonException">Thrown when <paramref name="jsonString"/> is not valid JSON.</exception>
        AdrPlusRepoConfig FromJson(string jsonString, string template, string defaultFolder);

        /// <summary>
        /// Reads all <c>.md</c> ADR files in <paramref name="directoryPath"/> whose filename begins with
        /// the configured prefix and the zero-padded <paramref name="sequence"/> number, then parses each one.
        /// Only files whose filename and header both parse successfully are included in the result.
        /// </summary>
        /// <param name="sequence">The ADR sequence number to filter files by.</param>
        /// <param name="fileSystemService">The file system service used for file operations.</param>
        /// <param name="directoryPath">The directory path to search for <c>.md</c> files.</param>
        /// <param name="config">The ADR Plus configuration containing naming conventions.</param>
        /// <returns>An array of <see cref="AdrFileNameComponents"/> whose sequence number matches <paramref name="sequence"/>.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="config"/> or <paramref name="fileSystemService"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="directoryPath"/> is null, empty, or whitespace.</exception>
        /// <exception cref="DirectoryNotFoundException">Thrown when <paramref name="directoryPath"/> does not exist.</exception>
        Task<AdrFileNameComponents[]> ReadAllAdrByNumber(int sequence, IFileSystemService fileSystemService, string directoryPath, AdrPlusRepoConfig config);

        /// <summary>
        /// Reads all ADR files and returns only the entry with the highest version and revision for each unique sequence number.
        /// The result is ordered descending by sequence number.
        /// </summary>
        /// <param name="fileSystemService">The file system service used for file operations.</param>
        /// <param name="directoryPath">The directory path to search for <c>.md</c> files.</param>
        /// <param name="config">The ADR Plus configuration containing naming conventions.</param>
        /// <returns>An array of <see cref="AdrFileNameComponents"/> containing only the latest version and revision for each sequence number.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="config"/> or <paramref name="fileSystemService"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="directoryPath"/> is null, empty, or whitespace.</exception>
        /// <exception cref="DirectoryNotFoundException">Thrown when <paramref name="directoryPath"/> does not exist.</exception>
        Task<AdrFileNameComponents[]> ReadLatestAdrFiles(IFileSystemService fileSystemService, string directoryPath, AdrPlusRepoConfig config);

        /// <summary>
        /// Searches all ADR files in <paramref name="directoryPath"/> for one whose unique title
        /// (title + domain, after case transformation) matches <paramref name="title"/> and <paramref name="domain"/>.
        /// </summary>
        /// <param name="title">The ADR title to look for (case transformation is applied before matching).</param>
        /// <param name="domain">The ADR domain to combine with the title for matching.</param>
        /// <param name="fileSystemService">The file system service used for file operations.</param>
        /// <param name="directoryPath">The directory path to search for ADR files.</param>
        /// <param name="config">The ADR Plus configuration containing naming conventions and case-transform rules.</param>
        /// <returns>The full file path of the first matching ADR, or an empty string when not found.</returns>
        Task<string> GetFileByUniqueTitle(string title, string domain, IFileSystemService fileSystemService, string directoryPath, AdrPlusRepoConfig config);

        /// <summary>
        /// Calculates the next available ADR sequence number by scanning all existing ADR files in
        /// <paramref name="directoryPath"/> and returning <c>max + 1</c>.
        /// Returns <c>1</c> when no valid ADR files are found.
        /// </summary>
        /// <param name="fileSystemService">The file system service used for file operations.</param>
        /// <param name="directoryPath">The directory path to search for existing ADR files.</param>
        /// <param name="config">The ADR Plus configuration used to correctly parse existing filenames.</param>
        /// <returns>The next available ADR sequence number (at least <c>1</c>).</returns>
        Task<int> GetNextNumber(IFileSystemService fileSystemService, string directoryPath, AdrPlusRepoConfig config);

        /// <summary>
        /// Returns the ADR file with the highest version and revision for the given <paramref name="sequence"/> number.
        /// Returns <see langword="null"/> when no matching files are found.
        /// </summary>
        /// <param name="sequence">The ADR sequence number to filter by.</param>
        /// <param name="fileSystemService">The file system service used to read ADR files.</param>
        /// <param name="directoryPath">The directory path to search for ADR files.</param>
        /// <param name="config">The ADR Plus configuration containing naming conventions.</param>
        /// <returns>The <see cref="AdrFileNameComponents"/> with the highest version/revision, or <see langword="null"/> if none found.</returns>
        Task<AdrFileNameComponents?> GetLatestADRSequence(int sequence, IFileSystemService fileSystemService, string directoryPath, AdrPlusRepoConfig config);

        /// <summary>
        /// Retrieves all distinct non-empty domain names across all ADR files in <paramref name="directoryPath"/>.
        /// </summary>
        /// <param name="fileSystemService">The file system service used for file operations.</param>
        /// <param name="directoryPath">The directory path to search for ADR files.</param>
        /// <param name="config">The ADR Plus configuration containing naming conventions.</param>
        /// <returns>An array of distinct domain strings, or an empty array when no ADR files define a domain.</returns>
        Task<string[]> GetDomains(IFileSystemService fileSystemService, string directoryPath, AdrPlusRepoConfig config);

        /// <summary>
        /// Parses an ADR file path, extracting all filename components (prefix, number, title, version, revision, scope, domain, superseded)
        /// according to the naming conventions in <paramref name="config"/>, and then reads and parses the file's header and body.
        /// </summary>
        /// <param name="filePath">The full or relative path to the ADR <c>.md</c> file.</param>
        /// <param name="config">The ADR Plus configuration defining naming conventions and status mappings.</param>
        /// <param name="fileSystemService">The file system service used to read the file content.</param>
        /// <returns>
        /// An <see cref="AdrFileNameComponents"/> object. When parsing fails, <see cref="AdrFileNameComponents.IsValid"/> is <see langword="false"/>
        /// and <see cref="AdrFileNameComponents.ErrorMessage"/> contains the reason.
        /// </returns>
        Task<AdrFileNameComponents> ParseFileName(string filePath, AdrPlusRepoConfig config, IFileSystemService fileSystemService);

        /// <summary>
        /// Generates a command-alias-to-handler-type map by reflecting over the <see cref="CommandsAdr"/> enum.
        /// The map uses <see cref="StringComparer.OrdinalIgnoreCase"/> so aliases are matched case-insensitively at runtime.
        /// </summary>
        /// <returns>A <see cref="Dictionary{TKey,TValue}"/> that maps each command alias string to its handler <see cref="Type"/>.</returns>
        Dictionary<string, Type> GenerateCommandsMap();

        /// <summary>
        /// Opens a file using the platform-appropriate shell command.
        /// </summary>
        /// <param name="filepath">The full path to the file to open.</param>
        /// <param name="command">The shell command string used to open the file.</param>
        /// <returns>An empty string on success; otherwise the stderr output or the exception message.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="filepath"/> or <paramref name="command"/> is <see langword="null"/>.</exception>
        string OpenFile(string filepath, string command);

        /// <summary>
        /// Retrieves all available commands by reflecting over the <see cref="CommandsAdr"/> enum fields decorated with <see cref="CommandAttribute"/>.
        /// </summary>
        /// <returns>An array of tuples containing the <see cref="CommandsAdr"/> value, alias string, handler <see cref="Type"/>, and description.</returns>
        (CommandsAdr Command, string Alias, Type ConfigCommandHandler, string Description)[] GetCommands();

        /// <summary>
        /// Parses the raw command-line <paramref name="args"/> tokens against the set of arguments declared for a command.
        /// Recognises short (<c>-x</c>) and long (<c>--name</c>) forms. When <c>-h</c> or <c>--help</c> is present,
        /// or when <paramref name="args"/> is empty, only <see cref="Arguments.Help"/> is returned.
        /// </summary>
        /// <param name="args">The raw command-line tokens to parse.</param>
        /// <param name="argsForCommand">The set of <see cref="Arguments"/> recognised by the current command.</param>
        /// <returns>A dictionary mapping each recognised <see cref="Arguments"/> to its value string (empty for flags).</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="args"/> or <paramref name="argsForCommand"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">
        /// Thrown when an unrecognised token is encountered, a required value is missing for an option,
        /// or a required argument is absent outside wizard mode.
        /// </exception>
        Dictionary<Arguments, string> ParseArgs(string[] args, Arguments[] argsForCommand);

        /// <summary>
        /// Builds formatted help text for the given command, including usage line, description, argument list with valid values, and examples.
        /// Returns an empty string when <paramref name="command"/> does not match any registered command.
        /// </summary>
        /// <param name="command">The alias or name of the command to generate help for.</param>
        /// <param name="argsForCommand">The arguments supported by the command.</param>
        /// <param name="examples">Example invocation strings to include in the output.</param>
        /// <returns>A human-readable help string, or <see cref="string.Empty"/> when the command is unknown.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="argsForCommand"/> or <paramref name="examples"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="command"/> is null or whitespace.</exception>
        string GetHelpText(string command, Arguments[] argsForCommand, string[] examples);
    }
}
