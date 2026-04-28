// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the AdrPlus project under MIT license
// ***************************************************************************************

using AdrPlus.Domain;
using AdrPlus.Infrastructure.FileSystem;

namespace AdrPlus.Core
{
    internal interface IAdrQueryService
    {
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
        /// Reads the latest ADR files asynchronously.
        /// </summary>
        /// <param name="fileSystemService">The file system service used to access files.</param>
        /// <param name="directoryPath">The directory path to search in.</param>
        /// <param name="config">The ADR Plus repository configuration.</param>
        /// <returns>An array of <see cref="AdrFileNameComponents"/> representing the latest ADR files.</returns>
        Task<AdrFileNameComponents[]> ReadLatestAdrFiles(IFileSystemService fileSystemService, string directoryPath, AdrPlusRepoConfig config);

        /// <summary>
        /// Reads all ADR files asynchronously.
        /// </summary>
        /// <param name="fileSystemService">The file system service used to access files.</param>
        /// <param name="directoryPath">The directory path to search in.</param>
        /// <param name="config">The ADR Plus repository configuration.</param>
        /// <returns>An array of all <see cref="AdrFileNameComponents"/> found.</returns>
        Task<AdrFileNameComponents[]> ReadAllAdrFiles(IFileSystemService fileSystemService, string directoryPath, AdrPlusRepoConfig config);

        /// <summary>
        /// Gets the file path of an ADR with a unique title in a specific domain asynchronously.
        /// </summary>
        /// <param name="title">The title of the ADR to find.</param>
        /// <param name="domain">The domain to search within.</param>
        /// <param name="fileSystemService">The file system service used to access files.</param>
        /// <param name="rootrepo">The root path repository</param>
        /// <param name="config">The ADR Plus repository configuration.</param>
        /// <returns>The file path of the ADR with the matching title and domain.</returns>
        Task<string> GetFileByUniqueTitle(string title, string domain, IFileSystemService fileSystemService,string rootrepo, AdrPlusRepoConfig config);

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
    }
}
