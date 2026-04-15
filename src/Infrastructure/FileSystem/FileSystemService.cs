// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the AdrPlus project under MIT license
// ***************************************************************************************

using AdrPlus.Core;
using System.Reflection;
using System.Text.Json;
using static System.Environment;

namespace AdrPlus.Infrastructure.FileSystem
{
    /// <summary>
    /// Default implementation of <see cref="IFileSystemService"/> that delegates to <see cref="System.IO"/> APIs.
    /// </summary>
    internal sealed class FileSystemService : IFileSystemService
    {
        private static readonly JsonSerializerOptions s_jsonOptions = new() { WriteIndented = false };
        /// <inheritdoc/>
        public bool DirectoryExists(string path) => Directory.Exists(path);

        /// <inheritdoc/>
        public string CreateDirectory(string path) => Directory.CreateDirectory(path).FullName;

        /// <inheritdoc/>
        public string GetFullNameDirectory(string path) => new DirectoryInfo(path).FullName;

        /// <inheritdoc/>
        public bool FileExists(string path) => File.Exists(path);

        /// <inheritdoc/>
        public Task<string[]> ReadAllLinesAsync(string path, CancellationToken cancellationToken = default) =>
            File.ReadAllLinesAsync(path, cancellationToken);    

        /// <inheritdoc/>
        public Task<string> ReadAllTextAsync(string path, CancellationToken cancellationToken = default) =>
            File.ReadAllTextAsync(path, cancellationToken);

        /// <inheritdoc/>
        public Task WriteAllTextAsync(string path, string content, CancellationToken cancellationToken = default) =>
            File.WriteAllTextAsync(path, content, cancellationToken);

        /// <inheritdoc/>
        public IEnumerable<string> EnumerateFiles(string path, string searchPattern) =>
            Directory.EnumerateFiles(path, searchPattern);

        /// <inheritdoc/>
        public string GetFullNameFile(string path) => new FileInfo(path).FullName;

        /// <inheritdoc/>
        public string[] GetFiles(string path, string searchPattern, SearchOption searchOption = SearchOption.AllDirectories) =>
            Directory.GetFiles(path, searchPattern, searchOption);

        /// <inheritdoc/>
        public string[] GetDrives() => Directory.GetLogicalDrives();

        /// <inheritdoc/>
        public async Task SaveHistoryAsync<T>(string fileKey, T content, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(fileKey);

            var filePath = GetHistoryFilePath(fileKey);
            var folderPath = Path.GetDirectoryName(filePath)!;

            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            var stringContent = JsonSerializer.Serialize(content, s_jsonOptions);
            await File.WriteAllTextAsync(filePath, stringContent, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<(bool Success, T? Result)> ReadHistoryAsync<T>(string fileKey, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(fileKey);

            var filePath = GetHistoryFilePath(fileKey);

            if (!File.Exists(filePath))
            {
                return (false, default);
            }

            var stringContent = await File.ReadAllTextAsync(filePath, cancellationToken);
            var result = JsonSerializer.Deserialize<T>(stringContent, s_jsonOptions);
            return (true, result);
        }

        /// <summary>
        /// Builds the full file path for a history file in the user profile directory.
        /// Format: <c>%USERPROFILE%\&lt;appFolderHistory&gt;\&lt;assemblyName&gt;.&lt;fileKey&gt;.txt</c>.
        /// </summary>
        /// <param name="fileKey">A key that uniquely identifies the history entry.</param>
        /// <returns>The fully qualified path for the history file.</returns>
        private static string GetHistoryFilePath(string fileKey)
        {
            var assemblyName = Assembly.GetEntryAssembly()?.GetName().Name ?? AppConstants.NameApp;
            var uniqueFile = $"{assemblyName}.{fileKey}.txt";
            var folderPath = Path.Combine(GetFolderPath(SpecialFolder.UserProfile), AppConstants.Folderhistory);
            return Path.Combine(folderPath, uniqueFile);
        }
    }
}
