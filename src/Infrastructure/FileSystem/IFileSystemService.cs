// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the AdrPlus project under MIT license
// ***************************************************************************************

namespace AdrPlus.Infrastructure.FileSystem
{
    /// <summary>
    /// Abstraction for file system operations.
    /// This interface allows for easy testing and mocking of file operations.
    /// </summary>
    internal interface IFileSystemService
    {
        /// <summary>
        /// Serializes <paramref name="content"/> to JSON and writes it to a per-user history file identified by <paramref name="fileKey"/>.
        /// </summary>
        /// <typeparam name="T">The type of the object to serialize.</typeparam>
        /// <param name="fileKey">A key that uniquely identifies the history file (used as part of the filename).</param>
        /// <param name="content">The object to serialize and save.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        Task SaveHistoryAsync<T>(string fileKey, T content, CancellationToken cancellationToken = default);

        /// <summary>
        /// Reads and deserializes a history file identified by <paramref name="fileKey"/>.
        /// Returns <c>(false, default)</c> when the file does not exist.
        /// </summary>
        /// <typeparam name="T">The type to deserialize the history data into.</typeparam>
        /// <param name="fileKey">A key that uniquely identifies the history file.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A tuple: <c>Success = true</c> and the deserialized <c>Result</c> when the file exists; otherwise <c>Success = false</c> and <c>Result = default</c>.</returns>
        Task<(bool Success, T? Result)> ReadHistoryAsync<T>(string fileKey, CancellationToken cancellationToken = default);

        /// <summary>
        /// Determines whether the specified directory exists.
        /// </summary>
        /// <param name="path">The directory path to check.</param>
        /// <returns><see langword="true"/> if the directory exists; otherwise <see langword="false"/>.</returns>
        bool DirectoryExists(string path);

        /// <summary>
        /// Creates the specified directory, including any necessary parent directories, and returns its full path.
        /// </summary>
        /// <param name="path">The path of the directory to create.</param>
        /// <returns>The fully qualified path of the created (or existing) directory.</returns>
        string CreateDirectory(string path);

        /// <summary>
        /// Gets the full path of the parent directory for the specified path.
        /// </summary>
        /// <param name="path">The file or directory path for which to retrieve the parent directory. Cannot be null or an empty string.</param>
        /// <returns>The full path of the parent directory, or null if the specified path does not have a parent directory.</returns>
        string? GetParentDirectory(string path);
        

        /// <summary>
        /// Gets the root directory path of the repository that contains the specified file path.
        /// </summary>
        /// <param name="pathfileadr">The full file system path to the file for which to determine the containing repository's root directory.
        /// Cannot be null or empty.</param>
        /// <returns>The full path to the root directory of the repository containing the specified file, or null if the file is
        /// not located within a recognized repository.</returns>
        string? GetFileRootRepositoryPath(string pathfileadr);

        /// <summary>
        /// Returns the fully qualified name of the specified directory.
        /// </summary>
        /// <param name="path">The relative or absolute file path to resolve.</param>
        /// <returns>The fully qualified directory path.</returns>
        string GetFullNameDirectoryByFile(string path);

        /// <summary>
        /// Determines whether the specified file exists.
        /// </summary>
        /// <param name="path">The file path to check.</param>
        /// <returns><see langword="true"/> if the file exists; otherwise <see langword="false"/>.</returns>
        bool FileExists(string path);

        /// <summary>
        /// Reads all text from the specified file asynchronously using UTF-8 encoding.
        /// </summary>
        /// <param name="path">The path to the file to read.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A task representing the asynchronous operation, containing the file content as a string.</returns>
        Task<string> ReadAllTextAsync(string path, CancellationToken cancellationToken = default);

        /// <summary>
        /// Reads all lines from a file asynchronously. 
        /// </summary>
        /// <param name="path">The path to the file to read.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>An array of strings, where each string is a line from the file.</returns>
        Task<string[]> ReadAllLinesAsync(string path, CancellationToken cancellationToken = default);   

        /// <summary>
        /// Writes the specified text to a file asynchronously, creating the file if it does not exist and overwriting it otherwise.
        /// </summary>
        /// <param name="path">The path to the file to write.</param>
        /// <param name="content">The text content to write to the file.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        Task WriteAllTextAsync(string path, string content, CancellationToken cancellationToken = default);

        /// <summary>
        /// Lazily enumerates files in <paramref name="path"/> that match <paramref name="searchPattern"/>.
        /// </summary>
        /// <param name="path">The directory to search in.</param>
        /// <param name="searchPattern">The search pattern to match file names against (e.g. <c>"*.md"</c>).</param>
        /// <returns>An enumerable of file paths matching the search pattern in the specified directory.</returns>
        IEnumerable<string> EnumerateFiles(string path, string searchPattern);

        /// <summary>
        /// Returns the fully qualified name of the specified file.
        /// </summary>
        /// <param name="path">The relative or absolute file path to resolve.</param>
        /// <returns>The fully qualified file path.</returns>
        string GetFullNameFile(string path);

        /// <summary>
        /// Retrieves the names of files in the specified directory that match the given search pattern and search
        /// option.
        /// </summary>
        /// <param name="path">The relative or absolute path to the directory to search.</param>
        /// <param name="searchPattern">The search string to match against the names of files in the directory.</param>
        /// <param name="searchOption">Specifies whether to search only the current directory or all subdirectories.</param>
        /// <returns>An array of file names that match the search criteria.</returns>
        string[] GetFiles(string path, string searchPattern, SearchOption searchOption = SearchOption.AllDirectories);

        /// <summary>
        /// Retrieves the names of all logical drives on the current computer.
        /// </summary>
        /// <returns>An array of drive name strings (e.g. <c>"C:\"</c>, <c>"D:\"</c>).</returns>
        string[] GetDrives();
    }
}
