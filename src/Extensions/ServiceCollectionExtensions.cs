// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the AdrPlus project under MIT license
// ***************************************************************************************

using AdrPlus.Commands;
using AdrPlus.Commands.Approve;
using AdrPlus.Commands.Config;
using AdrPlus.Commands.Explorer;
using AdrPlus.Commands.Help;
using AdrPlus.Commands.Init;
using AdrPlus.Commands.Migrate;
using AdrPlus.Commands.NewAdr;
using AdrPlus.Commands.Reject;
using AdrPlus.Commands.Review;
using AdrPlus.Commands.Supersede;
using AdrPlus.Commands.UndoStatus;
using AdrPlus.Commands.Version;
using AdrPlus.Commands.Wizard;
using AdrPlus.Core;
using AdrPlus.Infrastructure.Configuration;
using AdrPlus.Infrastructure.FileSystem;
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
            services.AddSingleton<IMainProgram, MainProgram>();
            services.AddSingleton<IConfigurationMigrator, ConfigVersionManager>();
            services.AddSingleton<IFileSystemService, FileSystemService>();
            services.AddSingleton<IValidateJsonConfig, ValidateJsonConfig>();
            services.AddSingleton<IPromptConsole, PromptConsole>();
            services.AddSingleton<IAdrServices, AdrService>();
            services.AddSingleton<CommandRouter>();
            services.AddSingleton<ExplorerCommandHandler>();
            services.AddSingleton<HelpCommandHandler>();
            services.AddSingleton<InitCommandHandler>();
            services.AddSingleton<MigrateCommandHandler>();
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
