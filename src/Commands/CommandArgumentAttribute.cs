// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the AdrPlus project under MIT license
// ***************************************************************************************

namespace AdrPlus.Commands
{
    /// <summary>
    /// Attribute used to decorate argument enum fields with command-line argument metadata.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    internal sealed class CommandArgumentAttribute(string shortCommand, string longCommand, string[]? aliasesvalues = null) : Attribute
    {
        /// <summary>
        /// Gets the short command alias (e.g., single letter or abbreviated form).
        /// </summary>
        public string ShortCommand { get; } = shortCommand;

        /// <summary>
        /// Gets the long command alias (e.g., full descriptive command name).
        /// </summary>
        public string LongCommand { get; } = longCommand;

        /// <summary>
        /// Gets the array of alias values that can be used for the command argument. 
        /// </summary>
        public string[]? AliasesValues { get; } = aliasesvalues;
    }
}
