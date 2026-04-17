// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the AdrPlus project under MIT license
// ***************************************************************************************

using AdrPlus.Core;
using AdrPlus.Domain;
using AdrPlus.Extensions;
using AdrPlus.Infrastructure.Logging;
using AdrPlus.Infrastructure.UI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Globalization;
using System.Reflection;

namespace AdrPlus
{
    /// <summary>
    /// Entry point class for the AdrPlus application.
    /// </summary>
    internal sealed class Program
    {
        /// <summary>
        /// Main entry point for the application.
        /// </summary>
        /// <param name="args">Command-line arguments.</param>
        /// <returns>A task representing the asynchronous operation with process exit code.</returns>
        static async Task<int> Main(string[] args)
        {
            //normalize the path of the executing assembly to ensure it works correctly even if the app is run from a different directory
            var assembly = Assembly.GetExecutingAssembly()!;
            var basepath = Path.GetDirectoryName(assembly.Location)!;

            string Command = args.Length > 0 ? args[0] : string.Empty;
            string commandArgsString = string.Join(AppConstants.CommandArgsSeparator, args.Length > 1 ? [.. args.Skip(1)] : []);
            ILogger? logger = null;
            IHost? host = null;
            var exitcode = 0;
            try
            {
                host = Host.CreateDefaultBuilder()
                            .UseConsoleLifetime()
                            .ConfigureLogging((hostContext, services) =>
                            {
                                services.ClearProviders();
                                services.AddFile(Path.Combine(basepath, "logs", $"{AppConstants.NameApp}.log"),
                                    retainedFileCountLimit: 3,
                                    outputTemplate: "{Timestamp:o} [{Level:u3}-{SourceContext}] {Message} {NewLine}{Exception}");
                                services.AddFilter("Microsoft.AspNetCore", LogLevel.Error);
                            })
                            .ConfigureServices((hostContext, services) =>
                            {
                                services.Configure<AdrPlusConfig>(hostContext.Configuration.GetSection(AppConstants.DefaultSettingsRoot));
                                services.AddHostedService<MainProgram>();
                                services.AddAdrPlusServices();
                            })
                            .ConfigureHostOptions(options =>
                            {
                                options.ShutdownTimeout = TimeSpan.FromSeconds(10);
                            })
                            .ConfigureAppConfiguration((hostingContext, config) =>
                            {
                                config.SetBasePath(basepath);
                                var assemblyver = assembly.GetName()?.Version?.ToString() ?? "0.0.0.0";
                                config.AddJsonFile(AppConstants.AppConfigfileName, optional: false, reloadOnChange: true);
                                config.AddInMemoryCollection(new Dictionary<string, string?>
                                {
                                    { AppConstants.CfgNameVersionApp,assemblyver },
                                    { AppConstants.CfgCommandName,Command },
                                    { AppConstants.CfgCommandArgs,commandArgsString }
                                });
                            }).Build();

                logger = host.Services.GetRequiredService<ILoggerFactory>().CreateLogger<Program>();

                var configapp = host.Services.GetRequiredService<IOptions<AdrPlusConfig>>().Value;

                var consoleservice = host.Services.GetRequiredService<IConsoleWriter>();
                consoleservice.ConfigurePrompt(configapp);
                consoleservice.ShowBanner(AppConstants.BannerText);

                var appculture = configapp.Language;
                var cultureInfo = new CultureInfo("en-us");
                if (!string.IsNullOrEmpty(appculture) && Helper.IsValidCultureName(appculture))
                {
                    cultureInfo = new CultureInfo(appculture);
                }
                CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
                CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;

                var validator = host.Services.GetRequiredService<IValidateJsonConfig>();
                var (isValid, errorReport) = await validator.ValidateAsync();
                if (!isValid)
                {
                    ConsoleWriter.ShowError(Resources.AdrPlus.ErrMsgConfigValidationFailed);
                    foreach (var error in errorReport)
                    {
                        LogMessages.LogError(logger, error);
                        ConsoleWriter.ShowError(error);
                    }
                    exitcode = 1;
                    return exitcode;
                }

                var appVersion = host.Services.GetRequiredService<IConfiguration>()[AppConstants.CfgNameVersionApp]!;

                LogMessages.LogApplicationStarting(logger, AppConstants.NameApp, appVersion, cultureInfo.Name);
                consoleservice.ShowWellcome(appVersion);

                await host.RunAsync();

            }
            catch (Exception ex)
            {
                if (logger is not null)
                {
                    LogMessages.LogCriticalError(logger, ex);
                }
                ConsoleWriter.ShowError(Resources.AdrPlus.ErrMsgCritical);
                ConsoleWriter.ShowError(ex.Message);
                exitcode = 1;
            }
            finally
            {
                host?.Dispose();
            }

            return exitcode;
        }
    }
}
