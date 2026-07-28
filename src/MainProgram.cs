// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the AdrPlus project under MIT license
// ***************************************************************************************

using AdrPlus.Commands;
using AdrPlus.Core;
using AdrPlus.Domain;
using AdrPlus.Infrastructure.Configuration;
using AdrPlus.Infrastructure.Logging;
using AdrPlus.Infrastructure.UI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Globalization;

namespace AdrPlus
{
    /// <summary>
    /// Main program class that implements lifecycle for AdrPlus application.
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of the <see cref="MainProgram"/> class.
    /// </remarks>
    /// <param name="logger">The logger instance.</param>
    /// <param name="optionsconfig">The options monitor for ADR Plus configuration.</param>
    /// <param name="commandRouter">The command router to route commands.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <param name="configurationMigrator">The configuration migrator to handle configuration migrations.</param>
    /// <param name="validateConfig">The JSON configuration validator.</param>
    /// <param name="prompt">The prompt console for user interactions.</param>
    internal sealed class MainProgram(
            ILogger<MainProgram> logger,
            IOptionsMonitor<AdrPlusConfig> optionsconfig,
            CommandRouter commandRouter,
            IConfiguration configuration,
            IConfigurationMigrator configurationMigrator,
            IValidateConfig validateConfig,
            IConsoleWriter prompt) : IMainProgram
    {
        private readonly ILogger<MainProgram> _logger = logger;
        private readonly CommandRouter _commandRouter = commandRouter;
        private readonly IConfiguration _configuration = configuration;
        private readonly IConsoleWriter _prompt = prompt;
        private readonly IConfigurationMigrator _configurationMigrator = configurationMigrator;
        private readonly IOptionsMonitor<AdrPlusConfig> _adrPlusConfig = optionsconfig;
        private readonly IValidateConfig _validateConfig = validateConfig;

        /// <summary>
        /// Executes the background command processing loop for the application host lifecycle.
        /// </summary>
        /// <param name="stoppingToken">Indicates that the execution process has been aborted.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var commandName = _configuration[AppConstants.CfgCommandName] ?? string.Empty;
            var argsString = _configuration[AppConstants.CfgCommandArgs] ?? string.Empty;
            var args = argsString.Split(AppConstants.CommandArgsSeparator, StringSplitOptions.RemoveEmptyEntries);
            var appVersion = _configuration[AppConstants.CfgNameVersionApp]!;
            var cultureInfo = new CultureInfo(_adrPlusConfig.CurrentValue.Language);

            LogMessages.LogApplicationStarting(_logger, AppConstants.NameApp, appVersion, cultureInfo.Name);

            var (isValid, errorReport) = await _validateConfig.ValidateAsync(stoppingToken);
            if (!isValid)
            {
                foreach (var error in errorReport)
                {
                    LogMessages.LogError(_logger, error);
                }
                LogMessages.LogStoppedAdrPlus(_logger);
                throw new InvalidOperationException(Resources.AdrPlus.ErrMsgConfigValidationFailed);
            }

            try
            {
                await _configurationMigrator.CheckAndMigrateConfigAsync(stoppingToken);
            }
            catch
            {
                Helper.ExitCode = 1;
                LogMessages.LogStoppedAdrPlus(_logger);
                throw;
            }

            if (commandName.Length == 0 || commandName != "help")
            {
                if (!await _prompt.TryExecuteFistInstall(stoppingToken))
                {
                    if (stoppingToken.IsCancellationRequested)
                    {
                        LogMessages.LogStoppedAdrPlus(_logger);
                        throw new InvalidOperationException(Resources.AdrPlus.CancelledByUser);
                    }
                }
            }

            do
            {
                Helper.HasAppConfigChange = false;
                _prompt.PromptEnsureCulture(_adrPlusConfig.CurrentValue);
                _prompt.PromptConfigure(_adrPlusConfig.CurrentValue);
                _prompt.PromptShowBanner(AppConstants.BannerText);
                _prompt.PromptShowWellcome(appVersion);
                try
                {
                    await _commandRouter.RouteAsync(commandName, args, stoppingToken);
                }
                catch
                {
                    Helper.ExitCode = 1;
                    if (!stoppingToken.IsCancellationRequested)
                    {
                        LogMessages.LogStoppedAdrPlus(_logger);
                        throw;
                    }
                    break;
                }
            } while (Helper.HasAppConfigChange);
            LogMessages.LogStoppedAdrPlus(_logger);
        }
    }
}
