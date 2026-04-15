// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the AdrPlus project under MIT license
// ***************************************************************************************

namespace AdrPlus.Commands
{
    /// <summary>
    /// Enumeration of usage categories for command-line arguments.
    /// Defines whether arguments are optional, required, or wizard-specific.
    /// </summary>
    internal enum UsageArgumments
    {
        /// <summary>
        /// Argument that triggers wizard mode.
        /// </summary>
        Wizard,

        /// <summary>
        /// Optional argument that can be omitted.
        /// </summary>
        Optional,

        /// <summary>
        /// Optional argument that requires a value when provided.
        /// </summary>
        OptionalWithValue,

        /// <summary>
        /// Argument that requires a value, optional in wizard mode but required otherwise.
        /// </summary>
        OptionalWithValueWhenWizard
    }
}
