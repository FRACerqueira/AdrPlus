// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the AdrPlus project under MIT license
// ***************************************************************************************

using AdrPlus.Commands;
using AdrPlus.Commands.Approve;
using AdrPlus.Commands.Config;
using AdrPlus.Commands.Explore;
using AdrPlus.Commands.Help;
using AdrPlus.Commands.Init;
using AdrPlus.Commands.Migrate;
using AdrPlus.Commands.NewAdr;
using AdrPlus.Commands.Plugins;
using AdrPlus.Commands.Reject;
using AdrPlus.Commands.Revise;
using AdrPlus.Commands.Supersede;
using AdrPlus.Commands.Sync;
using AdrPlus.Commands.UndoStatus;
using AdrPlus.Commands.Version;
using AdrPlus.Commands.Wizard;
using AdrPlus.Core;
using AdrPlus.Infrastructure.Configuration;
using AdrPlus.Infrastructure.FileSystem;
using AdrPlus.Infrastructure.UI;
using AdrPlus.Plugins;
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
            services.AddSingleton<IValidateConfig, ValidateConfig>();
            services.AddSingleton<IPluginManager, PluginManager>();
            services.AddSingleton<PromptConsole>();
            services.AddSingleton<IConsoleWriter>(sp => sp.GetRequiredService<PromptConsole>());
            services.AddSingleton<IConfigPrompts>(sp => sp.GetRequiredService<PromptConsole>());
            services.AddSingleton<IMigratePrompts>(sp => sp.GetRequiredService<PromptConsole>());
            services.AddSingleton<INewAdrPrompts>(sp => sp.GetRequiredService<PromptConsole>());
            services.AddSingleton<IExplorePrompts>(sp => sp.GetRequiredService<PromptConsole>());
            services.AddSingleton<IWizardMenuPrompts>(sp => sp.GetRequiredService<PromptConsole>());
            services.AddSingleton<IAdrServices, AdrService>();
            services.AddSingleton<CommandRouter>();
            services.AddSingleton<ExploreCommandHandler>();
            services.AddSingleton<HelpCommandHandler>();
            services.AddSingleton<InitCommandHandler>();
            services.AddSingleton<MigrateCommandHandler>();
            services.AddSingleton<WizardCommandHandler>();
            services.AddSingleton<ConfigCommandHandler>();
            services.AddSingleton<NewAdrCommandHandler>();
            services.AddSingleton<VersionCommandHandler>();
            services.AddSingleton<ReviseCommandHandler>();
            services.AddSingleton<RejectCommandHandler>();
            services.AddSingleton<ApproveCommandHandler>();
            services.AddSingleton<UndoStatusCommandHandler>();
            services.AddSingleton<SupersedeCommandHandler>();
            services.AddSingleton<SyncCommandHandler>();
            services.AddSingleton<PluginsCommandHandler>();
            return services;
        }
    }
}
