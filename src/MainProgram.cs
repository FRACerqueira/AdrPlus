// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the AdrPlus project under MIT license
// ***************************************************************************************

using AdrPlus.Commands;
using AdrPlus.Core;
using AdrPlus.Infrastructure.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AdrPlus
{
    /// <summary>
    /// Main program class that implements the hosted service lifecycle for AdrPlus application.
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of the <see cref="MainProgram"/> class.
    /// </remarks>
    /// <param name="logger">The logger instance.</param>
    /// <param name="commandRouter">The command router to route commands.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <param name="appLifetime">The application lifetime manager.</param>
    internal sealed class MainProgram(
            ILogger<MainProgram> logger,
            CommandRouter commandRouter,
            IConfiguration configuration,
            IHostApplicationLifetime appLifetime) : BackgroundService
    {
        private readonly ILogger<MainProgram> _logger = logger;
        private readonly CommandRouter _commandRouter = commandRouter;
        private readonly IConfiguration _configuration = configuration;
        private readonly IHostApplicationLifetime _applicationLifetime = appLifetime;


        /// <summary>
        /// Executes the background command processing loop for the application host lifecycle.
        /// </summary>
        /// <param name="stoppingToken">Indicates that the execution process has been aborted.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var appToken = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken, _applicationLifetime.ApplicationStopping);
            var commandName = _configuration[AppConstants.CfgCommandName] ?? string.Empty;
            var argsString = _configuration[AppConstants.CfgCommandArgs] ?? string.Empty;
            var args = argsString.Split(AppConstants.CommandArgsSeparator, StringSplitOptions.RemoveEmptyEntries);
            try
            {
                await _commandRouter.RouteAsync(commandName, args, appToken.Token);
            }
            finally
            {
                LogMessages.LogStoppedAdrPlus(_logger);
                _applicationLifetime.StopApplication();
            }
        }
    }
}
