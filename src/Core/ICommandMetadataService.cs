using AdrPlus.Commands;

namespace AdrPlus.Core
{
    internal interface ICommandMetadataService
    {
        Dictionary<string, Type> GenerateCommandsMap();
        string OpenFile(string filepath, string command);
        (CommandsAdr Command, string Alias, Type ConfigCommandHandler, string Description)[] GetCommands();
        Dictionary<Arguments, string> ParseArgs(string[] args, Arguments[] argsForCommand);
        string GetHelpText(string command, Arguments[] argsForCommand, string[] examples);
    }
}
