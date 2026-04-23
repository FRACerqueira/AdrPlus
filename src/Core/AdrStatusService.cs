// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the AdrPlus project under MIT license
// ***************************************************************************************

using AdrPlus.Domain;
using AdrPlus.Infrastructure.FileSystem;

namespace AdrPlus.Core
{
    internal sealed class AdrStatusService(IAdrFileParser fileParser) : IAdrStatusService
    {
        private readonly IAdrFileParser _fileParser = fileParser;

        public async Task<(bool Isvalid, string Error)> StatusUpdateAdrAsync(string fullpath, AdrStatus adrStatus, DateTime dref, AdrPlusRepoConfig config, IFileSystemService fileSystemService, CancellationToken cancellationToken)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(fullpath);
            ArgumentNullException.ThrowIfNull(config);
            ArgumentNullException.ThrowIfNull(fileSystemService);
            var parsefile = await _fileParser.ParseFileName(fullpath, config, fileSystemService);
            if (!parsefile.IsValid)
            {
                return (false, parsefile.ErrorMessage);
            }
            if (!parsefile.Header.IsValid)
            {
                return (false, parsefile.Header.ErrorMessage);
            }
            parsefile.Header.StatusUpdate = adrStatus;
            parsefile.Header.DateUpdate = dref;

            var record = Helper.CreateAdrRecord(parsefile, config);
            var contentfile = $"{record.GetHeader(config)}{record.Template}";
            await fileSystemService.WriteAllTextAsync(fullpath, contentfile, cancellationToken);
            return (true, string.Empty);
        }

        public async Task<(bool IsValid, string Error)> StatusChangeSupersedeAdrAsync(string fullpath, string filename, DateTime dref, AdrPlusRepoConfig config, IFileSystemService fileSystemService, CancellationToken cancellationToken)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(fullpath);
            ArgumentException.ThrowIfNullOrWhiteSpace(filename);
            ArgumentNullException.ThrowIfNull(config);
            ArgumentNullException.ThrowIfNull(fileSystemService);
            var parsefile = await _fileParser.ParseFileName(fullpath, config, fileSystemService);
            if (!parsefile.IsValid)
            {
                return (false, parsefile.ErrorMessage);
            }
            if (!parsefile.Header.IsValid)
            {
                return (false, parsefile.Header.ErrorMessage);
            }
            parsefile.Header.StatusChange = AdrStatus.Superseded;
            parsefile.Header.DateChange = dref;
            var record = Helper.CreateAdrRecord(parsefile, config);
            var contentfile = $"{record.GetHeader(config, filename)}{record.Template}";
            await fileSystemService.WriteAllTextAsync(fullpath, contentfile, cancellationToken);
            return (true, string.Empty);
        }

        public async Task<(bool IsValid, string Error)> StatusChangeAdrAsync(string fullpath, AdrStatus adrStatus, DateTime dref, AdrPlusRepoConfig config, IFileSystemService fileSystemService, CancellationToken cancellationToken)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(fullpath);
            ArgumentNullException.ThrowIfNull(config);
            ArgumentNullException.ThrowIfNull(fileSystemService);
            var parsefile = await _fileParser.ParseFileName(fullpath, config, fileSystemService);
            if (!parsefile.IsValid)
            {
                return (false, parsefile.ErrorMessage);
            }
            if (!parsefile.Header.IsValid)
            {
                return (false, parsefile.Header.ErrorMessage);
            }
            parsefile.Header.StatusChange = adrStatus;
            parsefile.Header.DateChange = dref;
            var record = Helper.CreateAdrRecord(parsefile, config);
            var contentfile = $"{record.GetHeader(config)}{record.Template}";
            await fileSystemService.WriteAllTextAsync(fullpath, contentfile, cancellationToken);
            return (true, string.Empty);
        }
    }
}
