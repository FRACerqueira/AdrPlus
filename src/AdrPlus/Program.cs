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
                Console.WriteLine($"AdrPlus {GetAppVersion(Assembly.GetExecutingAssembly())}");
                return 0;
            }

            // Hook into Console lifetime events (Ctrl+C)
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
            var assemblyver = GetAppVersion(assembly);

            try
            {

                //Setup and Build Configuration
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
            catch (OperationCanceledException)
            {
                PromptConsole.PromptShowError(Resources.AdrPlus.CancelledByUser);
                Helper.ExitCode = 1;
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
            return Helper.ExitCode;
        }

        /// <summary>
        /// Gets the application's full semantic version, including any pre-release suffix
        /// (e.g. "1.0.0-beta"). <see cref="AssemblyName.Version"/> is a numeric-only
        /// <see cref="Version"/> that MSBuild truncates from the csproj's semver
        /// <c>&lt;Version&gt;</c> (dropping "-beta"), so this reads
        /// <see cref="AssemblyInformationalVersionAttribute"/> instead, which preserves it.
        /// </summary>
        /// <param name="assembly">The assembly to read the version from.</param>
        /// <returns>The full semantic version string, or "0.0.0" if unavailable.</returns>
        internal static string GetAppVersion(Assembly assembly)
        {
            var informational = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
            return ResolveAppVersion(informational, assembly.GetName()?.Version);
        }

        /// <summary>
        /// Resolves the app version string from an <see cref="AssemblyInformationalVersionAttribute"/>
        /// value (preferred, preserves semver pre-release suffixes) or falls back to the numeric
        /// <see cref="AssemblyName.Version"/>.
        /// </summary>
        /// <param name="informationalVersion">The raw <see cref="AssemblyInformationalVersionAttribute.InformationalVersion"/> value, if any.</param>
        /// <param name="assemblyVersion">The numeric assembly version to fall back to.</param>
        /// <returns>The resolved version string, or "0.0.0" if neither source is available.</returns>
        internal static string ResolveAppVersion(string? informationalVersion, Version? assemblyVersion)
        {
            if (!string.IsNullOrWhiteSpace(informationalVersion))
            {
                // Strip a source-control metadata suffix (e.g. "+abc1234"), which
                // Deterministic/SourceLink builds append but isn't part of the semver.
                var plusIndex = informationalVersion.IndexOf('+', StringComparison.Ordinal);
                return plusIndex >= 0 ? informationalVersion[..plusIndex] : informationalVersion;
            }
            return assemblyVersion != null ? $"{assemblyVersion.Major}.{assemblyVersion.Minor}.{assemblyVersion.Build}" : "0.0.0";
        }
    }
}
