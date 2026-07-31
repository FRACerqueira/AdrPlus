// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the AdrPlus project under MIT license
// ***************************************************************************************

using AdrPlus.Domain;
using AbstractionsDomain = AdrPlus.Abstractions.Domain;

namespace AdrPlus.Extensions
{
    /// <summary>
    /// Extension methods mapping internal domain types to the immutable public snapshots exposed to plugins.
    /// </summary>
    internal static class PluginSnapshotExtensions
    {
        /// <summary>
        /// Maps an <see cref="AdrRecord"/> to its public, immutable <see cref="AbstractionsDomain.AdrRecordSnapshot"/>.
        /// </summary>
        public static AbstractionsDomain.AdrRecordSnapshot ToSnapshot(this AdrRecord record)
        {
            return new AbstractionsDomain.AdrRecordSnapshot
            {
                Number = record.Number,
                Version = record.Version,
                Revision = record.Revision,
                Title = record.Title,
                Domain = record.Domain,
                Scope = record.Scope,
                StatusCreate = record.StatusCreate.ToSnapshot(),
                StatusUpdate = record.StatusUpdate.ToSnapshot(),
                StatusChange = record.StatusChange.ToSnapshot(),
                CreateRef = record.CreateRef,
                UpdateRef = record.UpdateRef,
                ChangeRef = record.ChangeRef,
                Superseded = record.Superseded
            };
        }

        /// <summary>
        /// Maps the plugin-relevant subset of an <see cref="AdrPlusRepoConfig"/> to its public, immutable <see cref="AbstractionsDomain.RepoInfoSnapshot"/>.
        /// </summary>
        public static AbstractionsDomain.RepoInfoSnapshot ToSnapshot(this AdrPlusRepoConfig config)
        {
            return new AbstractionsDomain.RepoInfoSnapshot
            {
                FolderAdr = config.FolderAdr,
                Scopes = config.GetScopes(),
                StatusMapping = config.StatusMapping.ToDictionary(kv => kv.Key.ToSnapshot(), kv => kv.Value)
            };
        }

        /// <summary>
        /// Maps the internal <see cref="AdrStatus"/> to its public mirror <see cref="AbstractionsDomain.AdrStatus"/>.
        /// </summary>
        public static AbstractionsDomain.AdrStatus ToSnapshot(this AdrStatus status)
        {
            return status switch
            {
                AdrStatus.Unknown => AbstractionsDomain.AdrStatus.Unknown,
                AdrStatus.Proposed => AbstractionsDomain.AdrStatus.Proposed,
                AdrStatus.Accepted => AbstractionsDomain.AdrStatus.Accepted,
                AdrStatus.Rejected => AbstractionsDomain.AdrStatus.Rejected,
                AdrStatus.Superseded => AbstractionsDomain.AdrStatus.Superseded,
                _ => throw new ArgumentOutOfRangeException(nameof(status), status, null)
            };
        }
    }
}
