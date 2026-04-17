// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the AdrPlus project under MIT license
// ***************************************************************************************

using AdrPlus.Commands;
using AdrPlus.Commands.Approve;
using AdrPlus.Commands.Config;
using AdrPlus.Commands.Help;
using AdrPlus.Commands.Init;
using AdrPlus.Commands.NewAdr;
using AdrPlus.Commands.Review;
using AdrPlus.Commands.Reject;
using AdrPlus.Commands.Supersede;
using AdrPlus.Commands.UndoStatus;
using AdrPlus.Commands.Version;
using AdrPlus.Commands.Wizard;
using AdrPlus.Core;
using AdrPlus.Infrastructure.FileSystem;
using AdrPlus.Infrastructure.Process;
using AdrPlus.Infrastructure.UI;
using Microsoft.Extensions.DependencyInjection;

namespace AdrPlus.Extensions
{
    /// <summary>
    /// Extension methods for registering AdrPlus services with dependency injection.
    /// </summary>
    internal static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds all AdrPlus services to the service collection.
        /// </summary>
        /// <param name="services">The service collection to add services to.</param>
        /// <returns>The service collection for method chaining.</returns>
        public static IServiceCollection AddAdrPlusServices(this IServiceCollection services)
        {
            services.AddSingleton<IFileSystemService, FileSystemService>();
            services.AddSingleton<IProcessService, ProcessService>();
            services.AddSingleton<IValidateJsonConfig, ValidateJsonConfig>();
            services.AddSingleton<IConsoleWriter, ConsoleWriter>();
            services.AddSingleton<IAdrFileParser, AdrFileParserService>();
            services.AddSingleton<IAdrQueryService, AdrQueryService>();
            services.AddSingleton<IAdrStatusService, AdrStatusService>();
            services.AddSingleton<IAdrConfigMapper, AdrConfigMapperService>();
            services.AddSingleton<ICommandMetadataService, CommandMetadataService>();
            services.AddSingleton<IAdrServices, AdrService>();
            services.AddSingleton<CommandRouter>();

            services.AddSingleton<HelpCommandHandler>();
            services.AddSingleton<InitCommandHandler>();
            services.AddSingleton<WizardCommandHandler>();
            services.AddSingleton<ConfigCommandHandler>();
            services.AddSingleton<NewAdrCommandHandler>();
            services.AddSingleton<VersionCommandHandler>();
            services.AddSingleton<ReviewCommandHandler>();
            services.AddSingleton<RejectCommandHandler>();
            services.AddSingleton<ApproveCommandHandler>();
            services.AddSingleton<UndoStatusCommandHandler>();
            services.AddSingleton<SupersedeCommandHandler>();
            return services;
        }
    }
}
