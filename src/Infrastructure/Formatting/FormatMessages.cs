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

        // ==================== INFORMATIONAL MESSAGES ====================
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
    }
}
