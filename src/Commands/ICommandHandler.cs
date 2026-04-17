// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the AdrPlus project under MIT license
// ***************************************************************************************

namespace AdrPlus.Commands
{
    /// <summary>
    /// Base interface for all command handlers.
    /// Each command handler is responsible for executing a specific CLI command.
    /// </summary>
    internal interface ICommandHandler
    {
        /// <summary>
        /// Executes the command with the provided arguments.
        /// </summary>
        /// <param name="args">Command line arguments passed to this command</param>
        /// <param name="cancellationToken">Cancellation token for the async operation</param>
        /// <returns>Task representing the async operation</returns>
        Task ExecuteAsync(string[] args, CancellationToken cancellationToken = default);

    }
}
