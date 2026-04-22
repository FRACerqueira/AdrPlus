// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the AdrPlus project under MIT license
// ***************************************************************************************

using AdrPlus.Domain;
using AdrPlus.Infrastructure.FileSystem;

namespace AdrPlus.Core
{
    internal interface IAdrFileParser
    {
        /// <summary>
        /// Parses the header and content from an ADR file asynchronously.
        /// </summary>
        /// <param name="filePath">The path to the ADR file to parse.</param>
        /// <param name="config">The ADR Plus repository configuration.</param>
        /// <param name="fileSystemService">The file system service used to read the file.</param>
        /// <returns>A tuple containing the parsed <see cref="AdrHeader"/> and the remaining file content.</returns>
        Task<(AdrHeader header, string content)> ParseAdrHeaderAndContentAsync(string filePath, AdrPlusRepoConfig config, IFileSystemService fileSystemService);

        /// <summary>
        /// Parses the file name components from an ADR file path asynchronously.
        /// </summary>
        /// <param name="filePath">The path to the ADR file.</param>
        /// <param name="config">The ADR Plus repository configuration.</param>
        /// <param name="fileSystemService">The file system service used to access file information.</param>
        /// <returns>An <see cref="AdrFileNameComponents"/> object containing the parsed file name components.</returns>
        Task<AdrFileNameComponents> ParseFileName(string filePath, AdrPlusRepoConfig config, IFileSystemService fileSystemService);
    }
}
