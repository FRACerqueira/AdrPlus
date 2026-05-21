// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the AdrPlus project under MIT license
// ***************************************************************************************

namespace AdrPlus.Domain
{
    /// <summary>
    /// Represents the behavior of the application when no arguments are provided. 
    /// </summary>
    internal enum BehaviorWithoutArg
    {
        /// <summary>
        /// Displays help information.
        /// </summary>
        Help,
        /// <summary>
        /// Represents a wizard component for guiding users through a multi-step process. for command 'config' the argument is 'repository'
        /// </summary>
        Wizard,
        /// <summary>
        /// Required to be informed by the user through the argument.
        /// </summary>
        None,
    }
}
