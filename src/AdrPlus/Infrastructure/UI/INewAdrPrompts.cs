// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the AdrPlus project under MIT license
// ***************************************************************************************

using AdrPlus.Domain;

namespace AdrPlus.Infrastructure.UI
{
    /// <summary>
    /// Prompts used exclusively by the <c>new</c> command's wizard flow.
    /// </summary>
    internal interface INewAdrPrompts
    {
        /// <summary>
        /// Prompts the user to edit the title of an ADR.
        /// </summary>
        /// <param name="defaultTitle">The default title to display.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A tuple containing a boolean indicating if the operation was aborted and the entered title.</returns>
        (bool IsAborted, string Content) PromptEditTitleAdr(string defaultTitle, CancellationToken cancellationToken = default);

        /// <summary>
        /// Prompts the user to edit the scope of an ADR with suggested scopes. Scope is a free-text header field;
        /// suggestions are advisory only and never restrict what the user can type.
        /// </summary>
        /// <param name="defaultScope">The default scope to display.</param>
        /// <param name="sugestscopes">An array of suggested scopes to choose from.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A tuple containing a boolean indicating if the operation was aborted and the entered scope.</returns>
        (bool IsAborted, string Content) PromptEditScopeAdr(string defaultScope, string[] sugestscopes, CancellationToken cancellationToken = default);

        /// <summary>
        /// Prompts to retrieve the array of available domains from an already-read set of ADR files.
        /// </summary>
        /// <param name="adrFiles">
        /// A previously read set of ADR files (e.g. via <c>IAdrServices.ReadAllAdr</c>) - callers own the
        /// single read shared across scope/domain/title-uniqueness/next-number lookups; this method never
        /// reads the repository itself.
        /// </param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A tuple containing abort status, array of domains, and any exception that occurred.</returns>
        (bool IsAborted, string[] domains, Exception? Content) PromptGetArrayDomainsAdr(AdrFileNameComponents[] adrFiles, CancellationToken cancellationToken = default);

        /// <summary>
        /// Prompts to retrieve the array of available scopes from an already-read set of ADR files.
        /// </summary>
        /// <param name="adrFiles">
        /// A previously read set of ADR files (e.g. via <c>IAdrServices.ReadAllAdr</c>) - callers own the
        /// single read shared across scope/domain/title-uniqueness/next-number lookups; this method never
        /// reads the repository itself.
        /// </param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A tuple containing abort status, array of scopes, and any exception that occurred.</returns>
        (bool IsAborted, string[] scopes, Exception? Content) PromptGetArrayScopesAdr(AdrFileNameComponents[] adrFiles, CancellationToken cancellationToken = default);

        /// <summary>
        /// Prompts the user to edit the domain of an ADR with suggested domains.
        /// </summary>
        /// <param name="defaultdomain">The default domain to display.</param>
        /// <param name="sugestdomains">An array of suggested domains to choose from.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A tuple containing a boolean indicating if the operation was aborted and the entered domain.</returns>
        (bool IsAborted, string Content) PromptEditDomainAdr(string defaultdomain, string[] sugestdomains, CancellationToken cancellationToken = default);
    }
}
