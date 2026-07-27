// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the AdrPlus project under MIT license
// ***************************************************************************************

using AdrPlus.Core;
using AdrPlus.Domain;
using AdrPlus.Extensions;
using AdrPlus.Infrastructure.UI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace AdrPlus
{
    /// <summary>
    /// Entry point class for the AdrPlus application.
    /// </summary>
    internal sealed class Program
    {
        // Acts as our manual application lifetime signal
        private static readonly CancellationTokenSource _cts = new();


        /// <summary>
        /// Main entry point for the application.
        /// </summary>
        /// <param name="args">Command-line arguments.</param>
        /// <returns>A task representing the asynchronous operation with process exit code.</returns>
        static async Task<int> Main(string[] args)
        {
            if (args.Length > 0 && (args[0] == "--version" || args[0] == "-v"))
            {
                var version = Assembly.GetExecutingAssembly().GetName().Version;
                Console.WriteLine($"AdrPlus {version?.Major}.{version?.Minor}.{version?.Build}");
                return 0;
            }

            // Hook into Console lifetime events (Ctrl+C / SIGTERM)
            Console.CancelKeyPress += (sender, eventArgs) =>
            {
                eventArgs.Cancel = true; // Prevent immediate process termination
                _cts.Cancel(); // Signal our application to stop
            };

            //Hook into Process Exit for other termination signals
            AppDomain.CurrentDomain.ProcessExit += (s, e) =>
            {
                if (!_cts.IsCancellationRequested) _cts.Cancel();
            };

            string Command = args.Length > 0 ? args[0] : string.Empty;
            string commandArgsString = string.Join(AppConstants.CommandArgsSeparator, args.Length > 1 ? [.. args.Skip(1)] : []);

            var assembly = Assembly.GetExecutingAssembly()!;
            var assemblyver = "0.0.0";
            var structver = assembly.GetName()?.Version;
            if (structver != null)
            {
                assemblyver = $"{structver.Major}.{structver.Minor}.{structver.Build}";
            }

            try
            {

                //Setup anbd Build Configuration
                var configuration = new ConfigurationBuilder()
                    .SetBasePath(AppContext.BaseDirectory)
                    .AddJsonFile(AppConstants.AppConfigfileName, optional: false, reloadOnChange: true)
                    .AddInMemoryCollection(new Dictionary<string, string?>
                    {
                            { AppConstants.CfgNameVersionApp,assemblyver },
                            { AppConstants.CfgCommandName,Command },
                            { AppConstants.CfgCommandArgs,commandArgsString }
                    }).Build();

                //Setup DI Container
                var serviceProvider = new ServiceCollection()
                    .Configure<AdrPlusConfig>(configuration.GetSection(AppConstants.DefaultSettingsRoot))
                    .AddSingleton<IConfiguration>(configuration)
                    .AddAdrPlusServices()
                    .AddLogging(builder =>
                    {
                        builder.ClearProviders();
                        builder.AddFile(Path.Combine(AppContext.BaseDirectory, "logs", $"{AppConstants.NameApp}.log"),
                            retainedFileCountLimit: 3,
                            outputTemplate: "{Timestamp:o} [{Level:u3}-{SourceContext}] {Message} {NewLine}{Exception}");
                        builder.AddFilter("Microsoft.AspNetCore", LogLevel.Error);
                    }).BuildServiceProvider();

                var main = serviceProvider.GetRequiredService<IMainProgram>();
                await main.ExecuteAsync(_cts.Token);
            }
            catch (Exception ex)
            {
                PromptConsole.PromptShowError(Resources.AdrPlus.ErrMsgCritical);
                PromptConsole.PromptShowError(ex.Message);
                Helper.ExitCode = 1;
            }
            finally
            {
                Console.Out.Flush();
            }
            // Flushes any buffered output directly to the console window
            return Helper.ExitCode;
        }
    }
}
