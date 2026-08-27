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
        /// <returns>
        /// <see langword="true"/> if a migration was actually performed; <see langword="false"/> if none was
        /// needed (no baseline version file yet, or already on the current version). Failures are thrown, not
        /// returned as <see langword="false"/>.
        /// </returns>
        Task<bool> CheckAndMigrateConfigAsync(CancellationToken cancellationToken = default);
    }
}
