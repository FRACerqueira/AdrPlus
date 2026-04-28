// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the AdrPlus project under MIT license
// ***************************************************************************************

using AdrPlus.Commands;
using AdrPlus.Domain;
using AdrPlus.Infrastructure.FileSystem;

namespace AdrPlus.Core
{
    internal class AdrService(
        IAdrStatusService statusService,
        IAdrFileParser fileParser,
        IAdrConfigMapper configMapper,
        IAdrQueryService queryService,
        ICommandMetadataService commandMetadataService) : IAdrServices
    {
        private readonly IAdrStatusService _statusService = statusService;
        private readonly IAdrFileParser _fileParser = fileParser;
        private readonly IAdrConfigMapper _configMapper = configMapper;
        private readonly IAdrQueryService _queryService = queryService;
        private readonly ICommandMetadataService _commandMetadataService = commandMetadataService;

        /// <inheritdoc/>
        public Task<(bool Isvalid, string Error)> StatusUpdateAdrAsync(string fullpath, AdrStatus adrStatus, DateTime dref, AdrPlusRepoConfig config, IFileSystemService fileSystemService, CancellationToken cancellationToken)
            => _statusService.StatusUpdateAdrAsync(fullpath, adrStatus, dref, config, fileSystemService, cancellationToken);

        /// <inheritdoc/>
        public Task<(bool IsValid, string Error)> StatusChangeSupersedeAdrAsync(string fullpath, string filename, DateTime dref, AdrPlusRepoConfig config, IFileSystemService fileSystemService, CancellationToken cancellationToken)
            => _statusService.StatusChangeSupersedeAdrAsync(fullpath, filename, dref, config, fileSystemService, cancellationToken);

        /// <inheritdoc/>
        public Task<(bool IsValid, string Error)> StatusChangeAdrAsync(string fullpath, AdrStatus adrStatus, DateTime dref, AdrPlusRepoConfig config, IFileSystemService fileSystemService, CancellationToken cancellationToken)
            => _statusService.StatusChangeAdrAsync(fullpath, adrStatus, dref, config, fileSystemService, cancellationToken);

        /// <inheritdoc/>
        public AdrPlusRepoConfig FromJson(string jsonString, string template)
            => _configMapper.FromJson(jsonString, template);

        /// <inheritdoc/>
        public Task<AdrFileNameComponents[]> ReadAllAdrByNumber(int sequence, IFileSystemService fileSystemService, string directoryPath, AdrPlusRepoConfig config)
            => _queryService.ReadAllAdrByNumber(sequence, fileSystemService, directoryPath, config);

        /// <inheritdoc/>
        public Task<AdrFileNameComponents[]> ReadLatestAdrFiles(IFileSystemService fileSystemService, string directoryPath, AdrPlusRepoConfig config)
            => _queryService.ReadLatestAdrFiles(fileSystemService, directoryPath, config);

        /// <inheritdoc/>
        public Task<string> GetFileByUniqueTitle(string title, string domain, IFileSystemService fileSystemService,string rootrepo, AdrPlusRepoConfig config)
            => _queryService.GetFileByUniqueTitle(title, domain, fileSystemService, rootrepo, config);

        /// <inheritdoc/>
        public Task<int> GetNextNumber(IFileSystemService fileSystemService, string directoryPath, AdrPlusRepoConfig config)
            => _queryService.GetNextNumber(fileSystemService, directoryPath, config);

        /// <inheritdoc/>
        public Task<AdrFileNameComponents?> GetLatestADRSequence(int sequence, IFileSystemService fileSystemService, string rootPath, AdrPlusRepoConfig config)
            => _queryService.GetLatestADRSequence(sequence, fileSystemService, rootPath, config);

        /// <inheritdoc/>
        public Task<string[]> GetDomains(IFileSystemService fileSystemService, string directoryPath, AdrPlusRepoConfig config)
            => _queryService.GetDomains(fileSystemService, directoryPath, config);

        /// <inheritdoc/>
        public Task<AdrFileNameComponents> ParseFileName(string filePath, AdrPlusRepoConfig config, IFileSystemService fileSystemService)
            => _fileParser.ParseFileName(filePath, config, fileSystemService);

        /// <inheritdoc/>
        public Dictionary<string, Type> GenerateCommandsMap()
            => _commandMetadataService.GenerateCommandsMap();

        /// <inheritdoc/>
        public string OpenFile(string filepath, string command)
            => _commandMetadataService.OpenFile(filepath, command);

        /// <inheritdoc/>
        public (CommandsAdr Command, string Alias, Type ConfigCommandHandler, string Description)[] GetCommands()
            => _commandMetadataService.GetCommands();

        /// <inheritdoc/>
        public Dictionary<Arguments, string> ParseArgs(string[] args, Arguments[] argsForCommand)
            => _commandMetadataService.ParseArgs(args, argsForCommand);

        /// <inheritdoc/>
        public string GetHelpText(string command, Arguments[] argsForCommand, string[] examples)
            => _commandMetadataService.GetHelpText(command, argsForCommand, examples);
    }
}
