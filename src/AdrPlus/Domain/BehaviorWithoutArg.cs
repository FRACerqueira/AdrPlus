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
        /// Launches the interactive wizard. For the <c>config</c> command specifically, this maps to its 'repository' argument.
        /// </summary>
        Wizard,
        /// <summary>
        /// Required to be informed by the user through the argument.
        /// </summary>
        None,
    }
}
