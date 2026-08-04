// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the AdrPlus project under MIT license
// ***************************************************************************************

using AdrPlus.Domain;

namespace AdrPlus.Infrastructure.UI
{
    /// <summary>
    /// Prompts used exclusively by the <c>explore</c> command's wizard flow.
    /// </summary>
    internal interface IExplorePrompts
    {
        /// <summary>
        /// Prompts the user to select fields using an interactive explorer and returns the result along with an
        /// indicator of whether the operation was aborted.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation before completion.</param>
        /// <returns>A tuple containing a boolean value that indicates whether the operation was aborted, and an array of strings
        /// representing the selected fields. If the operation is aborted, the array may be empty.</returns>
        (bool IsAborted, string[] FieldsExplore) PromptFieldsExplore(CancellationToken cancellationToken);

        /// <summary>
        /// Prompts the user to choose whether to display an existing report or create a new one.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the prompt operation.</param>
        /// <returns>A tuple indicating whether the operation was aborted and whether the user chose to create a new report. The
        /// first value is <see langword="true"/> if the operation was aborted; otherwise, <see langword="false"/>. The
        /// second value is <see langword="true"/> if the user chose to create a new report; otherwise, <see
        /// langword="false"/>.</returns>
        (bool IsAborted, bool IsCreatingReport) PromptOptionShowOrCreateReport(CancellationToken cancellationToken);

        /// <summary>
        /// Prompts the user to select an input file for generating a report and returns the result along with an indicator of whether the operation was aborted.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the prompt operation.</param>
        /// <returns>A tuple containing a boolean indicating whether the operation was aborted and a string representing the selected file name.</returns>
        (bool IsAborted, string Filename) PromptInputFileReport(CancellationToken cancellationToken);

        /// <summary>
        /// Prompts the user to select a file from a list of found files and returns the result of the selection.
        /// </summary>
        /// <param name="foundfiles">An array of ADR file name components representing the available files to choose from.</param>
        /// <param name="fields">An array of field names to display in the table.</param>
        /// <param name="folderrepoadr">The folder path of the ADR repository.</param>
        /// <param name="adrPlusRepoConfig">The repository configuration used to resolve ADR file details.</param>
        /// <returns>A tuple containing a boolean indicating whether the operation was aborted and a string representing the selected file name.</returns>
        (bool IsAborted, string FileSelectd) PromptTableExplore(AdrFileNameComponents[] foundfiles, string[] fields, string folderrepoadr, AdrPlusRepoConfig adrPlusRepoConfig);
    }
}
