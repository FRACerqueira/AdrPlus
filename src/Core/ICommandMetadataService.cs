// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the AdrPlus project under MIT license
// ***************************************************************************************

using AdrPlus.Commands;

namespace AdrPlus.Core
{
    internal interface ICommandMetadataService
    {
        /// <summary>
        /// Generates a mapping of command names to their handler types.
        /// </summary>
        /// <returns>A dictionary mapping command names to their corresponding handler types.</returns>
        Dictionary<string, Type> GenerateCommandsMap();

        /// <summary>
        /// Opens a file using the specified command.
        /// </summary>
        /// <param name="filepath">The path to the file to open.</param>
        /// <param name="command">The command to use for opening the file.</param>
        /// <returns>The result or confirmation message of the file open operation.</returns>
        string OpenFile(string filepath, string command);

        /// <summary>
        /// Gets all available commands with their metadata.
        /// </summary>
        /// <returns>An array of tuples containing the command, its alias, handler type, and description.</returns>
        (CommandsAdr Command, string Alias, Type ConfigCommandHandler, string Description)[] GetCommands();

        /// <summary>
        /// Parses command-line arguments based on expected argument definitions.
        /// </summary>
        /// <param name="args">The command-line arguments to parse.</param>
        /// <param name="argsForCommand">The expected argument definitions for the command.</param>
        /// <returns>A dictionary mapping argument types to their parsed values.</returns>
        Dictionary<Arguments, string> ParseArgs(string[] args, Arguments[] argsForCommand);

        /// <summary>
        /// Generates help text for a command.
        /// </summary>
        /// <param name="command">The command name for which to generate help.</param>
        /// <param name="argsForCommand">The arguments supported by the command.</param>
        /// <param name="examples">Example usage strings for the command.</param>
        /// <returns>Formatted help text for the command.</returns>
        string GetHelpText(string command, Arguments[] argsForCommand, string[] examples);
    }
}
