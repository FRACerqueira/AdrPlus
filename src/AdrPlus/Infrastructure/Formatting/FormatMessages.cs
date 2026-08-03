// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the AdrPlus project under MIT license
// ***************************************************************************************

using System.Collections.Concurrent;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;

namespace AdrPlus.Infrastructure.Formatting
{
    /// <summary>
    /// Centralized CompositeFormat provider with cache per current UI culture.
    /// </summary>
    internal sealed class FormatMessages
    {
        private static readonly ConcurrentDictionary<string, CompositeFormat> Cache = new(StringComparer.Ordinal);

        private static CompositeFormat Get(Func<string> resourceAccessor, [CallerMemberName] string key = "")
        {
            var culture = CultureInfo.CurrentUICulture.Name;
            var cacheKey = $"{culture}:{key}";
            return Cache.GetOrAdd(cacheKey, _ => CompositeFormat.Parse(resourceAccessor()));
        }

        // ==================== ERROR MESSAGES ====================

        public static CompositeFormat ErrMsgNotFoundArgsOrMissing => Get(() => Resources.AdrPlus.ErrMsgNotFoundArgsOrMissing);
        public static CompositeFormat ErrAdrFieldHeaderNotFound => Get(() => Resources.AdrPlus.ErrMsgAdrFieldHeaderNotFound);
        public static CompositeFormat ErrNewLenSeqGreaterThanConfig => Get(() => Resources.AdrPlus.ErrMsgNewLenSeqGreatConfigSetting);
        public static CompositeFormat ErrNewLenVersionGreaterThanConfig => Get(() => Resources.AdrPlus.ErrMsgNewLenVerGreatConfigSetting);
        public static CompositeFormat ErrNewLenRevisionGreaterThanConfig => Get(() => Resources.AdrPlus.ErrMsgNewLenRevGreatConfigSetting);
        public static CompositeFormat ErrFileAlreadyExists => Get(() => Resources.AdrPlus.ErrMsgFileAlreadyExists);
        public static CompositeFormat ErrLenFileSampleMigration => Get(() => Resources.AdrPlus.ErrorLenFileSampleMigration);
        public static CompositeFormat ErrCannotDetermineRootPath => Get(() => Resources.AdrPlus.ErrorCannotDetermineRootPath);
        public static CompositeFormat ErrInvalidMenuOption => Get(() => Resources.AdrPlus.InvalidMenuOption);
        public static CompositeFormat ErrInvalidStatusForSupersede => Get(() => Resources.AdrPlus.NotValidStatusForSupersede);
        public static CompositeFormat ErrInvalidStatusForUndo => Get(() => Resources.AdrPlus.NotValidStatusForUndo);
        public static CompositeFormat ErrFileNotFound => Get(() => Resources.AdrPlus.ExceptionFileNotFound);
        public static CompositeFormat ErrConfigFileInvalid => Get(() => Resources.AdrPlus.ErrorInConfigFile);
        public static CompositeFormat ErrInvalidDateFormat => Get(() => Resources.AdrPlus.ErrorDateFormat);
        public static CompositeFormat ErrDirectoryNotFoundFormat => Get(() => Resources.AdrPlus.ExceptionDirectoryNotFound);
        public static CompositeFormat ErrInvalidStatusForUpdate => Get(() => Resources.AdrPlus.NotValidStatusForUpdate);
        public static CompositeFormat ErrInvalidStatusForApproveReject => Get(() => Resources.AdrPlus.NotValidStatusForApproveAndReject);
        public static CompositeFormat ErrInvalidCaseFormat => Get(() => Resources.AdrPlus.ExceptionInvalidCaseFormat);
        public static CompositeFormat ErrFolderRepositoryMustBeRelativeFormat => Get(() => Resources.AdrPlus.ErrMsgFolderRepoMustBeRelative);
        public static CompositeFormat ErrDirectoryNotFound => Get(() => Resources.AdrPlus.ExceptionDirectoryNotFound);
        public static CompositeFormat ErrAdrSequenceNotFound => Get(() => Resources.AdrPlus.ErrorSequenceAdrNotFound);
        public static CompositeFormat ErrAdrUniqueTitleAlreadyExists => Get(() => Resources.AdrPlus.NewAdrErrorUniqueTitleAlreadyExists);
        public static CompositeFormat ErrMissingRequiredArgumentFormat => Get(() => Resources.AdrPlus.ExceptionMissingRequiredArgument);
        public static CompositeFormat ErrInvalidScope => Get(() => Resources.AdrPlus.NewAdrErrorInvalidScope);
        public static CompositeFormat ErrConfigFileAlreadyExists => Get(() => Resources.AdrPlus.InitCmdConfigFileAlreadyExists);
        public static CompositeFormat ErrInvalidRepositoryConfig => Get(() => Resources.AdrPlus.ErrMsgInvalidRepoConfig);
        public static CompositeFormat ErrConfigFileNotFound => Get(() => Resources.AdrPlus.ExceptionConfigFileNotFound);
        public static CompositeFormat ErrConfigInvalidNumber => Get(() => Resources.AdrPlus.ConfigErrorInvalidNumber);
        public static CompositeFormat ErrConfigInvalidBoolean => Get(() => Resources.AdrPlus.ConfigErrorInvalidBoolean);
        public static CompositeFormat ErrRevisionNotConfigured => Get(() => Resources.AdrPlus.ErrorRevisionNotconfig);
        public static CompositeFormat ErrUnknownCommandFormat => Get(() => Resources.AdrPlus.ExceptionUnknownCommand);
        public static CompositeFormat ErrInvalidLanguageCodeFormat => Get(() => Resources.AdrPlus.ErrMsgInvalidLanguageCode);
        public static CompositeFormat ErrInvalidWithoutArgsFormat => Get(() => Resources.AdrPlus.ErrMsgWithoutArgs);   
        public static CompositeFormat ErrContentInvalidPathFormat => Get(() => Resources.AdrPlus.ErrMsgContentInvalidPath);
        public static CompositeFormat ErrContentPathTooLongFormat => Get(() => Resources.AdrPlus.ErrMsgContentPathTooLong);
        public static CompositeFormat ErrContentPathNotSupportedFormat => Get(() => Resources.AdrPlus.ErrMsgContentPathNotSupported);
        public static CompositeFormat ErrMigrationVersionFailed => Get(() => Resources.AdrPlus.ErrMigrationVersionFailed);


        // ==================== INFORMATIONAL MESSAGES ====================
        public static CompositeFormat MigrationVersionSuccess => Get(() => Resources.AdrPlus.MigrationVersionSuccess);
        public static CompositeFormat NotFoundRecreatedVersionMigration => Get(() => Resources.AdrPlus.NotFoundRecreatedVersionMigration);
        public static CompositeFormat MsgWelcome => Get(() => Resources.AdrPlus.Welcome);
        public static CompositeFormat MsgCommandStarted => Get(() => Resources.AdrPlus.MsgCommandStarted);
        public static CompositeFormat MsgCommandFinished => Get(() => Resources.AdrPlus.MsgCommandFinished);

        // ==================== VALIDATION MESSAGES ====================
        public static CompositeFormat ValidationLanguageInvalid => Get(() => Resources.AdrPlus.ValidationLanguageInvalidFormat);
        public static CompositeFormat ValidationMissingRequiredField => Get(() => Resources.AdrPlus.ValidationMissingRequiredField);
        public static CompositeFormat ValidationFieldWrongType => Get(() => Resources.AdrPlus.ValidationFieldWrongType);
        public static CompositeFormat ValidationUnexpectedFields => Get(() => Resources.AdrPlus.ValidationUnexpectedFields);
        public static CompositeFormat ValidationInvalidJsonFormat => Get(() => Resources.AdrPlus.ValidationInvalidJsonFormat);
        public static CompositeFormat ValidationFieldMustBeNonNegative => Get(() => Resources.AdrPlus.ValidationFieldMustBeNonNegative);
        public static CompositeFormat ValidationFieldMinimumValue => Get(() => Resources.AdrPlus.ValidationFieldMinimumValue);
        public static CompositeFormat ValidationScopesMustBeEmptyWhenLenScopeZero => Get(() => Resources.AdrPlus.ValidationScopesMustBeEmptyWhenLenScopeZero);
        public static CompositeFormat ValidationScopesMustNotBeEmptyWhenLenScopePositive => Get(() => Resources.AdrPlus.ValidationScopesMustNotBeEmptyWhenLenScopePositive);
        public static CompositeFormat ValidationScopeMinLength => Get(() => Resources.AdrPlus.ValidationScopeMinLength);
        public static CompositeFormat ValidationSkipDomainInvalidScopes => Get(() => Resources.AdrPlus.ValidationskipdomainInvalidScopes);
        public static CompositeFormat ValidationFolderByScopeRequiresScopes => Get(() => Resources.AdrPlus.ValidationFolderByScopeRequiresScopes);
        public static CompositeFormat ValidationMustFollowPattern => Get(() => Resources.AdrPlus.ValidationMustbeFollowing);
        public static CompositeFormat ValidationFieldCannotBeEmpty => Get(() => Resources.AdrPlus.ValidationFieldCannotBeEmpty);
        public static CompositeFormat ValidationPluginAllowlistEntryMissingName => Get(() => Resources.AdrPlus.ValidationPluginAllowlistEntryMissingName);

        // ==================== PLUGIN MESSAGES ====================
        public static CompositeFormat PluginRejectedManifestInvalid => Get(() => Resources.AdrPlus.PluginRejectedManifestInvalid);
        public static CompositeFormat PluginRejectedEntryAssemblyPathTraversal => Get(() => Resources.AdrPlus.PluginRejectedEntryAssemblyPathTraversal);
        public static CompositeFormat PluginRejectedNotInAllowlist => Get(() => Resources.AdrPlus.PluginRejectedNotInAllowlist);
        public static CompositeFormat PluginAllowlistHashNotEnforced => Get(() => Resources.AdrPlus.PluginAllowlistHashNotEnforced);
        public static CompositeFormat PluginRejectedDuplicateName => Get(() => Resources.AdrPlus.PluginRejectedDuplicateName);
        public static CompositeFormat PluginRejectedEntryTypeIncompatible => Get(() => Resources.AdrPlus.PluginRejectedEntryTypeIncompatible);
        public static CompositeFormat PluginRejectedAbstractionsVersionIncompatible => Get(() => Resources.AdrPlus.PluginRejectedAbstractionsVersionIncompatible);
        public static CompositeFormat PluginQueuedForRetry => Get(() => Resources.AdrPlus.PluginQueuedForRetry);
        public static CompositeFormat PluginPermanentFailure => Get(() => Resources.AdrPlus.PluginPermanentFailure);
        public static CompositeFormat PluginPendingAdrNotFound => Get(() => Resources.AdrPlus.PluginPendingAdrNotFound);
        public static CompositeFormat SyncSummaryReport => Get(() => Resources.AdrPlus.SyncSummaryReport);
        public static CompositeFormat PluginBackfillExhausted => Get(() => Resources.AdrPlus.PluginBackfillExhausted);
        public static CompositeFormat BackfillSummaryReport => Get(() => Resources.AdrPlus.BackfillSummaryReport);
        public static CompositeFormat PluginsModeRequired => Get(() => Resources.AdrPlus.PluginsModeRequired);
        public static CompositeFormat PluginsModeAmbiguous => Get(() => Resources.AdrPlus.PluginsModeAmbiguous);
        public static CompositeFormat PluginsAllowlisted => Get(() => Resources.AdrPlus.PluginsAllowlisted);
        public static CompositeFormat PluginsNoAllowlistConfigured => Get(() => Resources.AdrPlus.PluginsNoAllowlistConfigured);
        public static CompositeFormat PluginsListEntry => Get(() => Resources.AdrPlus.PluginsListEntry);
        public static CompositeFormat PluginsListEmpty => Get(() => Resources.AdrPlus.PluginsListEmpty);
        public static CompositeFormat PluginsListSummary => Get(() => Resources.AdrPlus.PluginsListSummary);
        public static CompositeFormat PluginsValidateEntryValid => Get(() => Resources.AdrPlus.PluginsValidateEntryValid);
        public static CompositeFormat PluginsValidateEntryRejected => Get(() => Resources.AdrPlus.PluginsValidateEntryRejected);
        public static CompositeFormat PluginsValidateEmpty => Get(() => Resources.AdrPlus.PluginsValidateEmpty);
        public static CompositeFormat PluginsValidateSummary => Get(() => Resources.AdrPlus.PluginsValidateSummary);
        public static CompositeFormat PluginsValidateStatusValid => Get(() => Resources.AdrPlus.PluginsValidateStatusValid);
        public static CompositeFormat PluginsValidateStatusRejected => Get(() => Resources.AdrPlus.PluginsValidateStatusRejected);
        public static CompositeFormat PluginsActiveMissing => Get(() => Resources.AdrPlus.PluginsActiveMissing);
        public static CompositeFormat PluginsStatusActive => Get(() => Resources.AdrPlus.PluginsStatusActive);
        public static CompositeFormat PluginsStatusInactive => Get(() => Resources.AdrPlus.PluginsStatusInactive);
        public static CompositeFormat PluginsStatusMissing => Get(() => Resources.AdrPlus.PluginsStatusMissing);
        public static CompositeFormat PluginsStatusDisabled => Get(() => Resources.AdrPlus.PluginsStatusDisabled);
        public static CompositeFormat PluginsActiveUpdated => Get(() => Resources.AdrPlus.PluginsActiveUpdated);
        public static CompositeFormat PluginsActiveSummary => Get(() => Resources.AdrPlus.PluginsActiveSummary);
        public static CompositeFormat WizardBuiltinPluginsAvailable => Get(() => Resources.AdrPlus.WizardBuiltinPluginsAvailable);
    }
}
