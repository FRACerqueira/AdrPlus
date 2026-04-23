// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the AdrPlus project under MIT license
// ***************************************************************************************

using AdrPlus.Commands;
using AdrPlus.Infrastructure.Process;
using System.Text;

namespace AdrPlus.Core
{
    internal sealed class CommandMetadataService : ICommandMetadataService
    {
        private readonly IProcessService _processService;

        /// <inheritdoc/>
        public CommandMetadataService(IProcessService processService)
        {
            _processService = processService ?? throw new ArgumentNullException(nameof(processService));
        }

        /// <inheritdoc/>
        public Dictionary<string, Type> GenerateCommandsMap()
        {
            var cmds = GetCommands();
            var map = new Dictionary<string, Type>(cmds.Length, StringComparer.OrdinalIgnoreCase);
            foreach (var (_, alias, handlerCommand, _) in cmds)
            {
                map[alias] = handlerCommand;
            }
            return map;
        }

        /// <inheritdoc/>
        public string OpenFile(string filepath, string command)
            => _processService.OpenFile(filepath, command);

        /// <inheritdoc/>
        public (CommandsAdr Command, string Alias, Type ConfigCommandHandler, string Description)[] GetCommands()
        {
            var enumType = typeof(CommandsAdr);
            var fields = enumType.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            var result = new List<(CommandsAdr Command, string Alias, Type HandlerCommand, string Description)>(fields.Length);
            foreach (var field in fields)
            {
                var cmd = (CommandsAdr)field.GetValue(null)!;
                if (field.GetCustomAttributes(typeof(CommandAttribute), false).FirstOrDefault() is CommandAttribute attribute)
                {
                    result.Add((cmd, attribute.AliasCommand, attribute.HandlerCommand, attribute.Description));
                }
            }
            return [.. result];
        }

        /// <inheritdoc/>
        public Dictionary<Arguments, string> ParseArgs(string[] args, Arguments[] argsForCommand)
        {
            ArgumentNullException.ThrowIfNull(args);
            ArgumentNullException.ThrowIfNull(argsForCommand);

            var parsedArgs = new Dictionary<Arguments, string>(args.Length);

            if (args.Length == 0 || Array.IndexOf(args, "-h") >= 0 || Array.IndexOf(args, "--help") >= 0)
            {
                parsedArgs[Arguments.Help] = string.Empty;
                return parsedArgs;
            }

            var argsForCommandSet = new HashSet<Arguments>(argsForCommand);
            var haswizard = false;

            for (int i = 0; i < args.Length; i++)
            {
                var arg = args[i];
                bool matched = false;

                foreach (var metadata in s_argumentMetadata)
                {
                    if (arg == metadata.ShortCommand || arg == metadata.LongCommand)
                    {
                        if (!argsForCommandSet.Contains(metadata.CommandArg))
                        {
                            continue;
                        }
                        matched = true;
                        if (!haswizard)
                        {
                            haswizard = metadata.Usage == UsageArgumments.Wizard;
                        }
                        switch (metadata.Usage)
                        {
                            case UsageArgumments.Wizard:
                            case UsageArgumments.Optional:
                                parsedArgs[metadata.CommandArg] = string.Empty;
                                break;
                            case UsageArgumments.OptionalWithValue:
                            case UsageArgumments.OptionalWithValueWhenWizard:
                                if (i + 1 < args.Length && !args[i + 1].StartsWith('-'))
                                {
                                    parsedArgs[metadata.CommandArg] = args[i + 1];
                                    i++;
                                }
                                else
                                {
                                    throw new ArgumentException(
                                        string.Format(null, s_exceptionMissingArgumentValueFormat,
                                        arg, metadata.LongCommand));
                                }
                                break;
                        }
                        break;
                    }
                }
                if (!matched)
                {
                    throw new ArgumentException(
                        string.Format(null, s_exceptionInvalidArgumentTokenFormat, arg));
                }
            }
            if (!haswizard)
            {
                foreach (var metadata in parsedArgs.Keys)
                {
                    var argMetadata = s_argumentMetadata.First(x => x.CommandArg == metadata);
                    if (parsedArgs[argMetadata.CommandArg].Length == 0 && argMetadata.Usage == UsageArgumments.OptionalWithValueWhenWizard)
                    {
                        throw new ArgumentException(
                            string.Format(null, s_exceptionMissingRequiredArgumentFormat,
                            argMetadata.LongCommand, argMetadata.ShortCommand));
                    }
                }
            }
            return parsedArgs;
        }

        /// <inheritdoc/>
        public string GetHelpText(string command, Arguments[] argsForCommand, string[] examples)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(command);
            ArgumentNullException.ThrowIfNull(argsForCommand);
            ArgumentNullException.ThrowIfNull(examples);

            var (_, alias, _, description) = GetCommands().FirstOrDefault(c => c.Alias.Equals(command, StringComparison.OrdinalIgnoreCase));
            if (string.IsNullOrEmpty(alias))
            {
                return string.Empty;
            }

            var argsForCommandSet = new HashSet<Arguments>(argsForCommand);
            var sb = new StringBuilder(512);
            sb.Append(Resources.AdrPlus.Usage);
            sb.AppendLine(" : ");
            sb.Append("  adrplus ");
            sb.Append(alias);
            sb.Append(" [");
            sb.Append(Resources.AdrPlus.Arguments);
            sb.AppendLine("]");
            sb.AppendLine();
            sb.Append(Resources.AdrPlus.Description);
            sb.AppendLine(" : ");
            sb.AppendLine(null, $"  {description}");
            sb.AppendLine();
            sb.Append(Resources.AdrPlus.Arguments);
            sb.AppendLine(" : ");

            foreach (var metadata in s_argumentMetadata)
            {
                if (!argsForCommandSet.Contains(metadata.CommandArg))
                {
                    continue;
                }

                var required = $" ({Resources.AdrPlus.Optional})";
                if (metadata.Usage == UsageArgumments.OptionalWithValueWhenWizard)
                {
                    required = $" ({Resources.AdrPlus.Required} {Resources.AdrPlus.WhenNotWizard})";
                }
                if (metadata.ValidValues.Length > 0)
                {
                    required += $" [{string.Join("|", metadata.ValidValues)}]";
                }
                sb.AppendLine(null, $"  {metadata.ShortCommand}, {metadata.LongCommand}{required}");
                sb.AppendLine(null, $"      {metadata.Description}");
                sb.AppendLine();
            }

            sb.Append(Resources.AdrPlus.Examples);
            sb.AppendLine(" : ");
            foreach (var example in examples)
            {
                sb.AppendLine(null, $"  {example}");
            }
            return sb.ToString();
        }

        private readonly record struct ArgumentMetadata(
              Arguments CommandArg,
              string ShortCommand,
              string LongCommand,
              UsageArgumments Usage,
              string[] ValidValues,
              string Description);

        private static readonly ArgumentMetadata[] s_argumentMetadata = InitializeArgumentMetadata();

        private static readonly CompositeFormat s_exceptionMissingArgumentValueFormat =
            CompositeFormat.Parse(Resources.AdrPlus.ExceptionMissingArgumentValue);

        private static readonly CompositeFormat s_exceptionInvalidArgumentTokenFormat =
            CompositeFormat.Parse(Resources.AdrPlus.ExceptionInvalidArgumentToken);

        private static readonly CompositeFormat s_exceptionMissingRequiredArgumentFormat =
            CompositeFormat.Parse(Resources.AdrPlus.ExceptionMissingRequiredArgument);

        private static ArgumentMetadata[] InitializeArgumentMetadata()
        {
            var enumType = typeof(Arguments);
            var fields = enumType.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            var metadataList = new List<ArgumentMetadata>(fields.Length);

            foreach (var field in fields)
            {
                if (field.GetCustomAttributes(typeof(CommandArgumentAttribute), false).FirstOrDefault() is CommandArgumentAttribute attribute)
                {
                    if (field.GetCustomAttributes(typeof(HelpUsageAttribute), false).FirstOrDefault() is HelpUsageAttribute usageattribute)
                    {
                        metadataList.Add(new ArgumentMetadata(
                            (Arguments)field.GetValue(null)!,
                            attribute.ShortCommand,
                            attribute.LongCommand,
                            usageattribute.Usage,
                            attribute.AliasesValues ?? [],
                            usageattribute.Description));
                    }
                }
            }
            return [.. metadataList];
        }
    }
}
