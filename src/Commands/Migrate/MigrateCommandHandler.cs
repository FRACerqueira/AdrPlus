// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the AdrPlus project under MIT license
// ***************************************************************************************

using AdrPlus.Core;
using AdrPlus.Domain;
using AdrPlus.Infrastructure.FileSystem;
using AdrPlus.Infrastructure.UI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AdrPlus.Commands.Migrate
{
    internal sealed class MigrateCommandHandler(
        ILogger<MigrateCommandHandler> logger,
        IOptions<AdrPlusConfig> config,
        IFileSystemService fileSystem,
        IValidateJsonConfig validateConfig,
        IConsoleWriter console,
        IAdrServices adrServices) : ICommandHandler
    {
        private readonly ILogger<MigrateCommandHandler> _logger = logger;
        private readonly IOptions<AdrPlusConfig> _config = config;
        private readonly IFileSystemService _fileSystem = fileSystem;
        private readonly IValidateJsonConfig _validateConfig = validateConfig;
        private readonly IConsoleWriter _console = console;
        private readonly IAdrServices _adrServices = adrServices;
        private static readonly Arguments[] ValidCommandArgs =
            [Arguments.WizardMigrate,
             Arguments.TargetRepo,
             Arguments.CountAdr,
             Arguments.Help];
        public Task ExecuteAsync(string[] args, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}
