// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the AdrPlus project under MIT license
// ***************************************************************************************

namespace AdrPlus.Infrastructure.Configuration
{
    /// <summary>
    /// Defines the contract for services that handle configuration migration between application versions.
    /// </summary>
    internal interface IConfigurationMigrator
    {
        /// <summary>
        /// Checks for version file in template directory and performs migration if needed.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>True if migration was successful; false otherwise.</returns>
        Task<bool> CheckAndMigrateConfigAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Recreates the version file in the history directory to reflect the current application version. 
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task RecreateVersionFileAsync(CancellationToken cancellationToken = default);
    }
}
