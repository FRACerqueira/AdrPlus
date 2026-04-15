// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the AdrPlus project under MIT license
// ***************************************************************************************

using AdrPlus.Core;
using AdrPlus.Infrastructure.UI;

namespace AdrPlus.Commands.Help
{
    /// <summary>
    /// Handles the <c>help</c> command, displaying the list of available commands and their descriptions,
    /// or routing to a specific command handler to show its own detailed help.
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of the <see cref="HelpCommandHandler"/> class.
    /// </remarks>
    /// <param name="console">The console writer for displaying help information.</param>
    /// <param name="commandRouter">The command router used to delegate to a specific command's help output.</param>
    /// <param name="adrServices">The ADR services for accessing command metadata and argument parsing.</param>
    internal sealed class HelpCommandHandler(IConsoleWriter console, CommandRouter commandRouter, IAdrServices adrServices) : ICommandHandler
    {
        private readonly IConsoleWriter _console = console;
        private readonly CommandRouter _commandRouter = commandRouter;
        private readonly IAdrServices _adrServices = adrServices;

        /// <summary>
        /// Executes the <c>help</c> command asynchronously.
        /// With no arguments it prints all available commands; with a single argument it delegates
        /// to that command handler to display its own detailed help; more than one argument is an error.
        /// </summary>
        /// <param name="args">The raw command-line tokens. Expects zero or one command alias.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="args"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">Thrown when more than one argument is provided.</exception>
        public async Task ExecuteAsync(string[] args, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(args);
            if (args.Length == 0)
            {
                GenerateHelpAllCommands();
            }
            else if (args.Length > 1)
            {
                throw new ArgumentException(Resources.AdrPlus.ErrMsgHelpTooManyArguments);
            }
            else if (args.Length == 1)
            {
                await _commandRouter.RouteAsync(args[0], [], cancellationToken);
            }
        }

        /// <summary>
        /// Writes the full list of available commands and their aliases to the console,
        /// aligning aliases to a uniform column width for readability.
        /// </summary>
        public void GenerateHelpAllCommands()
        {
            _console.WriteHelp(Resources.AdrPlus.HelpHeaderAvailableCommands);
            var commands = _adrServices.GetCommands();
            var maxAliasLength = commands.Max(c => c.Alias.Length);
            foreach (var (_, alias, _, description) in commands)
            {
                var aliasPadded = alias.PadRight(maxAliasLength);
                _console.WriteHelp($"  {aliasPadded} # {description}");
            }
        }
    }
}
