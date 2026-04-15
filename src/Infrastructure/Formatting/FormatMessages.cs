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

        public static CompositeFormat InvalidMenuOption => Get(() => Resources.AdrPlus.InvalidMenuOption);
        public static CompositeFormat NotValidStatusForSupersede => Get(() => Resources.AdrPlus.NotValidStatusForSupersede);
        public static CompositeFormat NotValidStatusForUndo => Get(() => Resources.AdrPlus.NotValidStatusForUndo);
        public static CompositeFormat ExceptionFileNotFound => Get(() => Resources.AdrPlus.ExceptionFileNotFound);
        public static CompositeFormat ErrorInConfigFile => Get(() => Resources.AdrPlus.ErrorInConfigFile);
        public static CompositeFormat ErrorDateFormat => Get(() => Resources.AdrPlus.ErrorDateFormat);
        public static CompositeFormat ErrorVersionNotConfig => Get(() => Resources.AdrPlus.ErrorVersionNotconfig);
        public static CompositeFormat FileMustBeOverFolderFormat => Get(() => Resources.AdrPlus.FileMustBeOverFolder);
        public static CompositeFormat ExceptionDirectoryNotFoundPathFormat => Get(() => Resources.AdrPlus.ExceptionDirectoryNotFound);
        public static CompositeFormat ErrorFilenameNoPrefixFormat => Get(() => Resources.AdrPlus.ErrorFilenameNoPrefix);
        public static CompositeFormat ErrorInvalidNumberFormatMsg => Get(() => Resources.AdrPlus.ErrorInvalidNumberFormat);
        public static CompositeFormat ErrorInvalidVersionFormatMsg => Get(() => Resources.AdrPlus.ErrorInvalidVersionFormat);
        public static CompositeFormat ErrorInvalidRevisionFormatMsg => Get(() => Resources.AdrPlus.ErrorInvalidRevisionFormat);
        public static CompositeFormat ErrorInvalidScopeFormatMsg => Get(() => Resources.AdrPlus.ErrorInvalidScopeFormat);
        public static CompositeFormat ErrorInvalidSupersededNumberFormatMsg => Get(() => Resources.AdrPlus.ErrorInvalidSupersededNumberFormat);
        public static CompositeFormat ErrorUnexpectedPartInFilenameMsg => Get(() => Resources.AdrPlus.ErrorUnexpectedPartInFilename);
        public static CompositeFormat NotValidStatusForUpdate => Get(() => Resources.AdrPlus.NotValidStatusForUpdate);
        public static CompositeFormat NotValidStatusForApproveAndReject => Get(() => Resources.AdrPlus.NotValidStatusForApproveAndReject);
        public static CompositeFormat NotValidStatusForAdr => Get(() => Resources.AdrPlus.NotValidStatusForAdr);
        public static CompositeFormat ErrorParsingFilenameMsg => Get(() => Resources.AdrPlus.ErrorParsingFilename);
        public static CompositeFormat ExceptionInvalidCaseFormatMsg => Get(() => Resources.AdrPlus.ExceptionInvalidCaseFormat);
        public static CompositeFormat WelcomeFormat => Get(() => Resources.AdrPlus.Welcome);
        public static CompositeFormat ValidationDateFormatInvalidFormat => Get(() => Resources.AdrPlus.ValidationDateFormatInvalidFormat);
        public static CompositeFormat ErrMsgFolderRepoMustBeRelativeFormat => Get(() => Resources.AdrPlus.ErrMsgFolderRepoMustBeRelative);
        public static CompositeFormat ValidationLanguageInvalidFormat => Get(() => Resources.AdrPlus.ValidationLanguageInvalidFormat);
        public static CompositeFormat ExceptionDirectoryNotFound => Get(() => Resources.AdrPlus.ExceptionDirectoryNotFound);
        public static CompositeFormat ErrorSequenceAdrNotFound => Get(() => Resources.AdrPlus.ErrorSequenceAdrNotFound);
        public static CompositeFormat NewAdrErrorUniqueTitleAlreadyExists => Get(() => Resources.AdrPlus.NewAdrErrorUniqueTitleAlreadyExists);
        public static CompositeFormat ExceptionMissingRequiredArgument => Get(() => Resources.AdrPlus.ExceptionMissingRequiredArgument);
        public static CompositeFormat InvalidScopeError => Get(() => Resources.AdrPlus.NewAdrErrorInvalidScope);
        public static CompositeFormat InitCmdConfigFileAlreadyExists => Get(() => Resources.AdrPlus.InitCmdConfigFileAlreadyExists);
        public static CompositeFormat ErrMsgInvalidRepoConfig => Get(() => Resources.AdrPlus.ErrMsgInvalidRepoConfig);
        public static CompositeFormat ExceptionInvalidFilename => Get(() => Resources.AdrPlus.ExceptionConfigFileNotFound);
        public static CompositeFormat ConfigInfoSelectedField => Get(() => Resources.AdrPlus.ConfigInfoSelectedField);
        public static CompositeFormat ConfigInfoCurrentValue => Get(() => Resources.AdrPlus.ConfigInfoCurrentValue);
        public static CompositeFormat ConfigErrorInvalidNumber => Get(() => Resources.AdrPlus.ConfigErrorInvalidNumber);
        public static CompositeFormat ConfigErrorInvalidBoolean => Get(() => Resources.AdrPlus.ConfigErrorInvalidBoolean);
        public static CompositeFormat ErrorRevisionNotconfig => Get(() => Resources.AdrPlus.ErrorRevisionNotconfig);
        public static CompositeFormat ExceptionUnknownCommandFormat => Get(() => Resources.AdrPlus.ExceptionUnknownCommand);
        public static CompositeFormat MsgCommandStartedFormat => Get(() => Resources.AdrPlus.MsgCommandStarted);
        public static CompositeFormat MsgCommandFinishedFormat => Get(() => Resources.AdrPlus.MsgCommandFinished);
        public static CompositeFormat ErrMsgInvalidLanguageCodeFormat => Get(() => Resources.AdrPlus.ErrMsgInvalidLanguageCode);
        public static CompositeFormat ErrMsgDateFormatInvalidFormat => Get(() => Resources.AdrPlus.ErrMsgDateFormatInvalid);
        public static CompositeFormat ErrMsgConfigFileNotFoundFormat => Get(() => Resources.AdrPlus.ErrMsgTemplateRepoFileNotFound);
        public static CompositeFormat ErrMsgContentInvalidPathFormat => Get(() => Resources.AdrPlus.ErrMsgContentInvalidPath);
        public static CompositeFormat ErrMsgContentPathTooLongFormat => Get(() => Resources.AdrPlus.ErrMsgContentPathTooLong);
        public static CompositeFormat ErrMsgContentPathNotSupportedFormat => Get(() => Resources.AdrPlus.ErrMsgContentPathNotSupported);
        public static CompositeFormat ValidationMissingRequiredFieldFormat => Get(() => Resources.AdrPlus.ValidationMissingRequiredField);
        public static CompositeFormat ValidationFieldMustBeBooleanFormat => Get(() => Resources.AdrPlus.ValidationFieldMustBeBoolean);
        public static CompositeFormat ValidationFieldWrongTypeFormat => Get(() => Resources.AdrPlus.ValidationFieldWrongType);
        public static CompositeFormat ValidationUnexpectedFieldsFormat => Get(() => Resources.AdrPlus.ValidationUnexpectedFields);
        public static CompositeFormat ValidationInvalidJsonFormatMsg => Get(() => Resources.AdrPlus.ValidationInvalidJsonFormat);
        public static CompositeFormat ValidationFieldMustBeNonNegativeFormat => Get(() => Resources.AdrPlus.ValidationFieldMustBeNonNegative);
        public static CompositeFormat ValidationFieldMinimumValueFormat => Get(() => Resources.AdrPlus.ValidationFieldMinimumValue);
        public static CompositeFormat ValidationScopesMustBeEmptyWhenLenScopeZeroFormat => Get(() => Resources.AdrPlus.ValidationScopesMustBeEmptyWhenLenScopeZero);
        public static CompositeFormat ValidationScopesMustNotBeEmptyWhenLenScopePositiveFormat => Get(() => Resources.AdrPlus.ValidationScopesMustNotBeEmptyWhenLenScopePositive);
        public static CompositeFormat ValidationScopeMinLengthFormat => Get(() => Resources.AdrPlus.ValidationScopeMinLength);
        public static CompositeFormat ValidationskipdomainInvalidScopesFormat => Get(() => Resources.AdrPlus.ValidationskipdomainInvalidScopes);
        public static CompositeFormat ValidationFolderByScopeRequiresScopesFormat => Get(() => Resources.AdrPlus.ValidationFolderByScopeRequiresScopes);
        public static CompositeFormat ValidationMustbeFollowing => Get(() => Resources.AdrPlus.ValidationMustbeFollowing);
        public static CompositeFormat ValidationFieldCannotBeEmptyFormat => Get(() => Resources.AdrPlus.ValidationFieldCannotBeEmpty);
        public static CompositeFormat ValidationFieldMaxCharValue => Get(() => Resources.AdrPlus.ValidationFieldMaxCharValue);
    }
}
