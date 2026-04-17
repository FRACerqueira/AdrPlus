using AdrPlus.Domain;
using AdrPlus.Infrastructure.FileSystem;

namespace AdrPlus.Core
{
    internal interface IAdrQueryService
    {
        Task<AdrFileNameComponents[]> ReadAllAdrByNumber(int sequence, IFileSystemService fileSystemService, string directoryPath, AdrPlusRepoConfig config);
        Task<AdrFileNameComponents[]> ReadLatestAdrFiles(IFileSystemService fileSystemService, string directoryPath, AdrPlusRepoConfig config);
        Task<AdrFileNameComponents[]> ReadAllAdrFiles(IFileSystemService fileSystemService, string directoryPath, AdrPlusRepoConfig config);
        Task<string> GetFileByUniqueTitle(string title, string domain, IFileSystemService fileSystemService, string directoryPath, AdrPlusRepoConfig config);
        Task<int> GetNextNumber(IFileSystemService fileSystemService, string directoryPath, AdrPlusRepoConfig config);
        Task<AdrFileNameComponents?> GetLatestADRSequence(int sequence, IFileSystemService fileSystemService, string directoryPath, AdrPlusRepoConfig config);
        Task<string[]> GetDomains(IFileSystemService fileSystemService, string directoryPath, AdrPlusRepoConfig config);
    }
}
