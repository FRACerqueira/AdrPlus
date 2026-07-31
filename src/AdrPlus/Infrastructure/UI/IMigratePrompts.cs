// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the AdrPlus project under MIT license
// ***************************************************************************************

using AdrPlus.Domain;

namespace AdrPlus.Infrastructure.UI
{
    /// <summary>
    /// Prompts used exclusively by the <c>migrate</c> command.
    /// </summary>
    internal interface IMigratePrompts
    {
        /// <summary>
        /// Prompts the user to select ADR migrations to display and returns the result of the selection.
        /// </summary>
        /// <param name="adrs">An array of ADR file name components representing the available ADR migrations to choose from.</param>
        /// <param name="adrPlusRepo">The repository configuration used to resolve ADR migration details.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
        /// <returns>A tuple containing a boolean indicating whether the operation was aborted and an integer representing the
        /// number of selected ADR migrations.</returns>
        (bool IsAborted, int CountSelected) PromptShowAdrsMigrations(AdrFileNameComponents[] adrs, AdrPlusRepoConfig adrPlusRepo, CancellationToken cancellationToken = default);
    }
}
