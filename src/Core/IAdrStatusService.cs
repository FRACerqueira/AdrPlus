// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the AdrPlus project under MIT license
// ***************************************************************************************

using AdrPlus.Domain;
using AdrPlus.Infrastructure.FileSystem;

namespace AdrPlus.Core
{
    internal interface IAdrStatusService
    {
        /// <summary>
        /// Updates the status of an ADR file asynchronously.
        /// </summary>
        /// <param name="fullpath">The full path to the ADR file.</param>
        /// <param name="adrStatus">The new status to set for the ADR.</param>
        /// <param name="dref">The reference date for the status update.</param>
        /// <param name="config">The ADR Plus repository configuration.</param>
        /// <param name="fileSystemService">The file system service used to access and modify the file.</param>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <returns>A tuple containing a boolean indicating success and an error message if applicable.</returns>
        Task<(bool Isvalid, string Error)> StatusUpdateAdrAsync(string fullpath, AdrStatus adrStatus, DateTime dref, AdrPlusRepoConfig config, IFileSystemService fileSystemService, CancellationToken cancellationToken);

        /// <summary>
        /// Changes the status of an ADR to superseded by another ADR asynchronously.
        /// </summary>
        /// <param name="fullpath">The full path to the ADR file to be marked as superseded.</param>
        /// <param name="filename">The filename of the superseding ADR.</param>
        /// <param name="dref">The reference date for the status change.</param>
        /// <param name="config">The ADR Plus repository configuration.</param>
        /// <param name="fileSystemService">The file system service used to access and modify the file.</param>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <returns>A tuple containing a boolean indicating success and an error message if applicable.</returns>
        Task<(bool IsValid, string Error)> StatusChangeSupersedeAdrAsync(string fullpath, string filename, DateTime dref, AdrPlusRepoConfig config, IFileSystemService fileSystemService, CancellationToken cancellationToken);

        /// <summary>
        /// Changes the status of an ADR asynchronously.
        /// </summary>
        /// <param name="fullpath">The full path to the ADR file.</param>
        /// <param name="adrStatus">The new status to set for the ADR.</param>
        /// <param name="dref">The reference date for the status change.</param>
        /// <param name="config">The ADR Plus repository configuration.</param>
        /// <param name="fileSystemService">The file system service used to access and modify the file.</param>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <returns>A tuple containing a boolean indicating success and an error message if applicable.</returns>
        Task<(bool IsValid, string Error)> StatusChangeAdrAsync(string fullpath, AdrStatus adrStatus, DateTime dref, AdrPlusRepoConfig config, IFileSystemService fileSystemService, CancellationToken cancellationToken);
    }
}
