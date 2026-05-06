// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the AdrPlus project under MIT license
// ***************************************************************************************

using AdrPlus.Domain;
using AdrPlus.Infrastructure.FileSystem;
using AdrPlus.Infrastructure.Formatting;
using System.Globalization;

namespace AdrPlus.Core
{
    internal sealed class AdrQueryService(IAdrFileParser fileParser) : IAdrQueryService
    {
        private readonly IAdrFileParser _fileParser = fileParser;

        /// <inheritdoc/>
        public async Task<AdrFileNameComponents[]> ReadAllAdrByNumber(int sequence, IFileSystemService fileSystemService, string rootpath, AdrPlusRepoConfig config)
        {
            ArgumentNullException.ThrowIfNull(config);
            ArgumentNullException.ThrowIfNull(fileSystemService);

            if (string.IsNullOrWhiteSpace(rootpath))
            {
                throw new ArgumentException(Resources.AdrPlus.ExceptionDirectoryPathEmpty, nameof(rootpath));
            }

            if (!fileSystemService.DirectoryExists(rootpath))
            {
                throw new DirectoryNotFoundException(string.Format(null, FormatMessages.ExceptionDirectoryNotFoundPathFormat, rootpath));
            }

            var result = new List<AdrFileNameComponents>();
            var searchPattern = $"*{sequence.ToString($"D{config.LenSeq}", CultureInfo.CurrentCulture)}*.md";
            var adrfolder = Path.GetFullPath(Path.Combine(rootpath, config.FolderAdr));
            var mdFiles = fileSystemService.GetFiles(adrfolder, searchPattern);

            foreach (var filePath in mdFiles)
            {
                var aux = await _fileParser.ParseFileName(filePath, config, fileSystemService);
                if (aux.IsValid && aux.Header.IsValid)
                {
                    result.Add(aux);
                }
            }
            return [.. result];
        }

        /// <inheritdoc/>
        public async Task<AdrFileNameComponents[]> ReadLatestAdrFiles(IFileSystemService fileSystemService, string directoryPath, AdrPlusRepoConfig config)
        {
            var allAdrFiles = await ReadAllAdrFiles(fileSystemService, directoryPath, config);
            return [.. allAdrFiles
               .GroupBy(adr => $"{adr.Number}{adr.Header.StatusUpdate}")
               .Select(group => group
                   .OrderByDescending(adr => adr.Version)
                   .ThenByDescending(adr => adr.Revision ?? 0)
                   .First())
               .OrderByDescending(adr => adr.Number)
               .ThenByDescending(adr => adr.Version)
               .ThenByDescending(adr => adr.Revision ?? 0)];
        }

        /// <inheritdoc/>
        public async Task<AdrFileNameComponents[]> ReadAllAdrFiles(IFileSystemService fileSystemService, string directoryPath, AdrPlusRepoConfig config, bool includeNotMatched = false)
        {
            ArgumentNullException.ThrowIfNull(config);
            ArgumentNullException.ThrowIfNull(fileSystemService);

            if (string.IsNullOrWhiteSpace(directoryPath))
            {
                throw new ArgumentException(Resources.AdrPlus.ExceptionDirectoryPathEmpty, nameof(directoryPath));
            }

            if (!fileSystemService.DirectoryExists(directoryPath))
            {
                throw new DirectoryNotFoundException(string.Format(null, FormatMessages.ExceptionDirectoryNotFoundPathFormat, directoryPath));
            }
            var result = new List<AdrFileNameComponents>();
            var folderadr = Path.GetFullPath(Path.Combine(directoryPath, config.FolderAdr));
            var mdFiles = fileSystemService.GetFiles(folderadr, "*.md", SearchOption.AllDirectories);

            foreach (var filePath in mdFiles)
            {
                var parsedComponents = await _fileParser.ParseFileName(filePath, config, fileSystemService);
                if (!parsedComponents.IsValid && !includeNotMatched)
                {
                    continue;
                }
                result.Add(parsedComponents);
            }
            return [.. result.OrderBy(x => x.Header.IsMigrated).ThenByDescending(x=> x.Number)];
        }

        /// <inheritdoc/>
        public async Task<string> GetFileByUniqueTitle(string title, string domain, IFileSystemService fileSystemService, string rootrepo, AdrPlusRepoConfig config)
        {
            var uniqueTitle = AdrFileNameComponents.CreateUniqueTitle(title.ToCase(config.CaseTransform), domain.ToCase(config.CaseTransform));
            AdrFileNameComponents[] adrFiles = await ReadAllAdrFiles(fileSystemService, rootrepo, config);
            var aux = adrFiles
                .FirstOrDefault(f => f.UniqueTitle == uniqueTitle);
            return aux?.FileName ?? string.Empty;
        }

        /// <inheritdoc/>
        public async Task<int> GetNextNumber(IFileSystemService fileSystemService, string directoryPath, AdrPlusRepoConfig config)
        {
            AdrFileNameComponents[] adrFiles = await ReadAllAdrFiles(fileSystemService, directoryPath, config);
            return adrFiles.Length == 0 ? 1 : adrFiles.Max(f => f.Number) + 1;
        }

        /// <inheritdoc/>
        public async Task<AdrFileNameComponents?> GetLatestADRSequence(int sequence, IFileSystemService fileSystemService, string rootpath, AdrPlusRepoConfig config)
        {
            return (await ReadAllAdrByNumber(sequence, fileSystemService, rootpath, config))
                .OrderBy(x => x.Version)
                .ThenBy(x => x.Revision ?? 0)
                .Last();
        }

        /// <inheritdoc/>
        public async Task<string[]> GetDomains(IFileSystemService fileSystemService, string directoryPath, AdrPlusRepoConfig config)
        {
            AdrFileNameComponents[] adrFiles = await ReadAllAdrFiles(fileSystemService, directoryPath, config);
            return adrFiles.Length == 0
                ? []
                : [.. adrFiles
                    .Where(f => !string.IsNullOrWhiteSpace(f.Domain))
                    .Select(f => f.Domain!)
                    .Distinct()];
        }
    }
}
