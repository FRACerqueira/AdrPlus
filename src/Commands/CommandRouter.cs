// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the AdrPlus project under MIT license
// ***************************************************************************************

using AdrPlus.Commands.Help;
using AdrPlus.Core;
using AdrPlus.Infrastructure.Formatting;
using AdrPlus.Infrastructure.Logging;
using AdrPlus.Infrastructure.UI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AdrPlus.Commands
{
    /// <summary>
    /// Routes incoming command requests to their appropriate handlers.
    /// Replaces the large switch statement in the old AdrServices.CommandSelector method.
    /// </summary>
    internal sealed class CommandRouter(
        IServiceProvider serviceProvider,
        ILogger<CommandRouter> logger,
        IConsoleWriter console,
        IAdrServices adrServices)
    {
        private readonly IServiceProvider _serviceProvider = serviceProvider;
        private readonly ILogger<CommandRouter> _logger = logger;
        private readonly IConsoleWriter _console = console;
        private readonly IAdrServices _adrServices = adrServices;
        private readonly Dictionary<string, Type> _commandMap = adrServices.GenerateCommandsMap();

        /// <summary>
        /// Routes a command to its handler and executes it.
        /// </summary>
        /// <param name="commandName">The name of the command to execute.</param>
        /// <param name="args">Arguments to pass to the command.</param>
        /// <param name="cancellationToken">Cancellation token for the async operation.</param>
        public async Task RouteAsync(string commandName, string[] args, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(commandName))
            {
                try
                {
                    LogMessages.LogExecutingCommand(_logger, "help");
                    var helpHandler = _serviceProvider.GetRequiredService<HelpCommandHandler>();
                    _console.WriteStartCommand(string.Format(null, FormatMessages.MsgCommandStartedFormat, "help"));
                    await helpHandler.ExecuteAsync([], cancellationToken);
                    _console.WriteFinishedCommand(string.Format(null, FormatMessages.MsgCommandFinishedFormat, "help"));
                    LogMessages.LogCommandCompleted(_logger, "help" );
                }
                catch (Exception ex)
                {
                    LogMessages.LogCommandException(_logger, ex);
                    _console.WriteError(ex.Message);
                    throw;
                }
                return;
            }

            var handlerType = GetHandlerType(commandName);

            if (handlerType == null)
            {
                LogMessages.LogUnknownCommand(_logger, commandName);
                var msg = string.Format(null, FormatMessages.ExceptionUnknownCommandFormat, commandName);
                _console.WriteError(msg);
                throw new InvalidOperationException(msg);
            }

            try
            {
                LogMessages.LogExecutingCommand(_logger, commandName);
                _console.WriteStartCommand(string.Format(null, FormatMessages.MsgCommandStartedFormat, commandName));
                var handler = (ICommandHandler)_serviceProvider.GetRequiredService(handlerType);
                await handler.ExecuteAsync(args, cancellationToken);
            }
            catch (Exception ex)
            {
                LogMessages.LogCommandException(_logger, ex );
                _console.WriteError(ex.Message);
                throw;
            }
            finally
            {
                LogMessages.LogCommandCompleted(_logger, commandName);
                _console.WriteFinishedCommand(string.Format(null, FormatMessages.MsgCommandFinishedFormat, commandName));
            }
        }


        /// <summary>
        /// Gets the handler type for a given command name.
        /// </summary>
        /// <param name="commandName">The command name (case-insensitive).</param>
        /// <returns>The handler type, or null if command not found.</returns>
        private Type? GetHandlerType(string commandName)
        {
            _commandMap.TryGetValue(commandName, out var handlerType);
            return handlerType;
        }
    }
}
