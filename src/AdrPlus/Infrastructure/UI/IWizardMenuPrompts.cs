// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the AdrPlus project under MIT license
// ***************************************************************************************

using AdrPlus.Core;

namespace AdrPlus.Infrastructure.UI
{
    /// <summary>
    /// Prompts used exclusively by the <c>wizard</c> command's top-level menu navigation.
    /// </summary>
    internal interface IWizardMenuPrompts
    {
        /// <summary>
        /// Prompts the user to select an option from a menu.
        /// </summary>
        /// <param name="IsHasconfig">Indicates whether configuration is already available to influence menu behavior.</param>
        /// <param name="itemMenus">The array of menu items to choose from.</param>
        /// <param name="defaultvalue">The default menu option selected when the prompt starts.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A tuple indicating whether the operation was aborted and the user's selected menu item.</returns>
        (bool IsAborted, ItemMenuWizard? Content) PromptSelectMenu(bool IsHasconfig, ItemMenuWizard[] itemMenus, ItemMenuWizard defaultvalue, CancellationToken cancellationToken = default);
    }
}
