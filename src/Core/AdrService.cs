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

        public Task<(bool Isvalid, string Error)> StatusUpdateAdrAsync(string fullpath, AdrStatus adrStatus, DateTime dref, AdrPlusRepoConfig config, IFileSystemService fileSystemService, CancellationToken cancellationToken)
            => _statusService.StatusUpdateAdrAsync(fullpath, adrStatus, dref, config, fileSystemService, cancellationToken);

        public Task<(bool IsValid, string Error)> StatusChangeSupersedeAdrAsync(string fullpath, string filename, DateTime dref, AdrPlusRepoConfig config, IFileSystemService fileSystemService, CancellationToken cancellationToken)
            => _statusService.StatusChangeSupersedeAdrAsync(fullpath, filename, dref, config, fileSystemService, cancellationToken);

        public Task<(bool IsValid, string Error)> StatusChangeAdrAsync(string fullpath, AdrStatus adrStatus, DateTime dref, AdrPlusRepoConfig config, IFileSystemService fileSystemService, CancellationToken cancellationToken)
            => _statusService.StatusChangeAdrAsync(fullpath, adrStatus, dref, config, fileSystemService, cancellationToken);

        public AdrPlusRepoConfig FromJson(string jsonString, string template, string defaultFolder)
            => _configMapper.FromJson(jsonString, template, defaultFolder);

        public Task<AdrFileNameComponents[]> ReadAllAdrByNumber(int sequence, IFileSystemService fileSystemService, string directoryPath, AdrPlusRepoConfig config)
            => _queryService.ReadAllAdrByNumber(sequence, fileSystemService, directoryPath, config);

        public Task<AdrFileNameComponents[]> ReadLatestAdrFiles(IFileSystemService fileSystemService, string directoryPath, AdrPlusRepoConfig config)
            => _queryService.ReadLatestAdrFiles(fileSystemService, directoryPath, config);

        public Task<string> GetFileByUniqueTitle(string title, string domain, IFileSystemService fileSystemService, string directoryPath, AdrPlusRepoConfig config)
            => _queryService.GetFileByUniqueTitle(title, domain, fileSystemService, directoryPath, config);

        public Task<int> GetNextNumber(IFileSystemService fileSystemService, string directoryPath, AdrPlusRepoConfig config)
            => _queryService.GetNextNumber(fileSystemService, directoryPath, config);

        public Task<AdrFileNameComponents?> GetLatestADRSequence(int sequence, IFileSystemService fileSystemService, string directoryPath, AdrPlusRepoConfig config)
            => _queryService.GetLatestADRSequence(sequence, fileSystemService, directoryPath, config);

        public Task<string[]> GetDomains(IFileSystemService fileSystemService, string directoryPath, AdrPlusRepoConfig config)
            => _queryService.GetDomains(fileSystemService, directoryPath, config);

        public Task<AdrFileNameComponents> ParseFileName(string filePath, AdrPlusRepoConfig config, IFileSystemService fileSystemService)
            => _fileParser.ParseFileName(filePath, config, fileSystemService);

        public Dictionary<string, Type> GenerateCommandsMap()
            => _commandMetadataService.GenerateCommandsMap();

        public string OpenFile(string filepath, string command)
            => _commandMetadataService.OpenFile(filepath, command);

        public (CommandsAdr Command, string Alias, Type ConfigCommandHandler, string Description)[] GetCommands()
            => _commandMetadataService.GetCommands();

        public Dictionary<Arguments, string> ParseArgs(string[] args, Arguments[] argsForCommand)
            => _commandMetadataService.ParseArgs(args, argsForCommand);

        public string GetHelpText(string command, Arguments[] argsForCommand, string[] examples)
            => _commandMetadataService.GetHelpText(command, argsForCommand, examples);
    }
}
