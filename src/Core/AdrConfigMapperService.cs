// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the AdrPlus project under MIT license
// ***************************************************************************************

using AdrPlus.Domain;
using System.Text.Json;

namespace AdrPlus.Core
{
    internal sealed class AdrConfigMapperService : IAdrConfigMapper
    {
        /// <inheritdoc/>
        public AdrPlusRepoConfig FromJson(
            string jsonString,
            string template)
        {
            if (string.IsNullOrWhiteSpace(jsonString))
                throw new ArgumentNullException(nameof(jsonString), Resources.AdrPlus.ExceptionJsonStringNull);

            using var jsonDoc = JsonDocument.Parse(jsonString, AppConstants.DocumentOptions);
            var root = jsonDoc.RootElement;

            var config = new AdrPlusRepoConfig(AppConstants.DefaultFolderAdr,template);

            if (Helper.TryGetPropertyCaseInsensitive(root, AppConstants.FieldFolderAdr, out var folderadrElement) && folderadrElement.ValueKind == JsonValueKind.String)
            {
                config.FolderAdr = folderadrElement.GetString()!;
            }

            if (Helper.TryGetPropertyCaseInsensitive(root, AppConstants.FieldPrefix, out var prefixElement) && prefixElement.ValueKind == JsonValueKind.String)
            { 
                config.Prefix = prefixElement.GetString()!;
            }

            if (Helper.TryGetPropertyCaseInsensitive(root, AppConstants.FieldLenSeq, out var lenseqElement) && lenseqElement.ValueKind == JsonValueKind.Number)
            {
                var lenseq = lenseqElement.GetInt32();
                if (lenseq > 0) config.LenSeq = lenseq;
            }

            if (Helper.TryGetPropertyCaseInsensitive(root, AppConstants.FieldLenVersion, out var lenversionElement) && lenversionElement.ValueKind == JsonValueKind.Number)
            {
                var lenversion = lenversionElement.GetInt32();
                if (lenversion >= 0) config.LenVersion = lenversion;
            }

            if (Helper.TryGetPropertyCaseInsensitive(root, AppConstants.FieldLenRevision, out var lenrevisionElement) && lenrevisionElement.ValueKind == JsonValueKind.Number)
            {
                var lenrevision = lenrevisionElement.GetInt32();
                if (lenrevision >= 0) config.LenRevision = lenrevision;
            }

            if (Helper.TryGetPropertyCaseInsensitive(root, AppConstants.FieldLenScope, out var lenscopeElement) && lenscopeElement.ValueKind == JsonValueKind.Number)
            {
                var lenscope = lenscopeElement.GetInt32();
                if (lenscope >= 0) config.LenScope = lenscope;
            }

            if (Helper.TryGetPropertyCaseInsensitive(root, AppConstants.FieldSeparator, out var separatorElement) && separatorElement.ValueKind == JsonValueKind.String)
            {
                var separator = separatorElement.GetString();
                if (!string.IsNullOrWhiteSpace(separator) && separator.Length == 1)
                    config.Separator = separator[0];
            }

            if (Helper.TryGetPropertyCaseInsensitive(root, AppConstants.FieldCaseTransform, out var caseTransformElement) && caseTransformElement.ValueKind == JsonValueKind.String)
            {
                var caseTransform = caseTransformElement.GetString();
                if (!string.IsNullOrWhiteSpace(caseTransform) &&
                    Enum.TryParse<CaseFormat>(caseTransform, ignoreCase: true, out var caseFormat))
                    config.CaseTransform = caseFormat;
            }

            if (Helper.TryGetPropertyCaseInsensitive(root, AppConstants.FieldStatusNew, out var statusNewElement) && statusNewElement.ValueKind == JsonValueKind.String)
                config.StatusNew = statusNewElement.GetString()!;

            if (Helper.TryGetPropertyCaseInsensitive(root, AppConstants.FieldStatusAccepted, out var statusAccElement) && statusAccElement.ValueKind == JsonValueKind.String)
                config.StatusAcc = statusAccElement.GetString()!;

            if (Helper.TryGetPropertyCaseInsensitive(root, AppConstants.FieldStatusRejected, out var statusRejElement) && statusRejElement.ValueKind == JsonValueKind.String)
                config.StatusRej = statusRejElement.GetString()!;

            if (Helper.TryGetPropertyCaseInsensitive(root, AppConstants.FieldStatusSuperseded, out var statusSupElement) && statusSupElement.ValueKind == JsonValueKind.String)
                config.StatusSup = statusSupElement.GetString()!;

            if (Helper.TryGetPropertyCaseInsensitive(root, AppConstants.FieldScopes, out var scopesElement) && scopesElement.ValueKind == JsonValueKind.String)
                config.Scopes = scopesElement.GetString()!;

            if (Helper.TryGetPropertyCaseInsensitive(root, AppConstants.FieldFolderByScope, out var folderByScopeElement))
            {
                if (folderByScopeElement.ValueKind == JsonValueKind.True)
                    config.FolderByScope = true;
                else if (folderByScopeElement.ValueKind == JsonValueKind.False)
                    config.FolderByScope = false;
                else if (folderByScopeElement.ValueKind == JsonValueKind.String &&
                         bool.TryParse(folderByScopeElement.GetString(), out bool folderByScopeValue))
                    config.FolderByScope = folderByScopeValue;
            }

            if (Helper.TryGetPropertyCaseInsensitive(root, AppConstants.FieldSkipDomain, out var skipdomainElement) && skipdomainElement.ValueKind == JsonValueKind.String)
                config.SkipDomain = skipdomainElement.GetString()!;

            if (Helper.TryGetPropertyCaseInsensitive(root, AppConstants.FieldHeaderDisclaimer, out var headerDisclaimerElement) && headerDisclaimerElement.ValueKind == JsonValueKind.String)
                config.HeaderDisclaimer = headerDisclaimerElement.GetString()!;

            if (Helper.TryGetPropertyCaseInsensitive(root, AppConstants.FieldHeaderTitleFile, out var headerTitleFileElement) && headerTitleFileElement.ValueKind == JsonValueKind.String)
                config.HeaderTitleFile = headerTitleFileElement.GetString()!;

            if (Helper.TryGetPropertyCaseInsensitive(root, AppConstants.FieldHeaderVersion, out var headerVersionElement) && headerVersionElement.ValueKind == JsonValueKind.String)
                config.HeaderVersion = headerVersionElement.GetString()!;

            if (Helper.TryGetPropertyCaseInsensitive(root, AppConstants.FieldHeaderRevision, out var headerRevisionElement) && headerRevisionElement.ValueKind == JsonValueKind.String)
                config.HeaderRevision = headerRevisionElement.GetString()!;

            if (Helper.TryGetPropertyCaseInsensitive(root, AppConstants.FieldHeaderScope, out var headerScopeElement) && headerScopeElement.ValueKind == JsonValueKind.String)
                config.HeaderScope = headerScopeElement.GetString()!;

            if (Helper.TryGetPropertyCaseInsensitive(root, AppConstants.FieldHeaderDomain, out var headerDomainElement) && headerDomainElement.ValueKind == JsonValueKind.String)
                config.HeaderDomain = headerDomainElement.GetString()!;

            if (Helper.TryGetPropertyCaseInsensitive(root, AppConstants.FieldHeaderStatusCreated, out var headerStatusCreatedElement) && headerStatusCreatedElement.ValueKind == JsonValueKind.String)
                config.HeaderTitleStatusCreated = headerStatusCreatedElement.GetString()!;

            if (Helper.TryGetPropertyCaseInsensitive(root, AppConstants.FieldHeaderStatusChanged, out var headerStatusChangedElement) && headerStatusChangedElement.ValueKind == JsonValueKind.String)
                config.HeaderTitleStatusChanged = headerStatusChangedElement.GetString()!;

            if (Helper.TryGetPropertyCaseInsensitive(root, AppConstants.FieldHeaderStatusSuperseded, out var headerStatusSupersededElement) && headerStatusSupersededElement.ValueKind == JsonValueKind.String)
                config.HeaderTitleStatusSuperseded = headerStatusSupersededElement.GetString()!;

            if (Helper.TryGetPropertyCaseInsensitive(root, AppConstants.FieldHeaderTableFields, out var headerTableFieldsElement) && headerTableFieldsElement.ValueKind == JsonValueKind.String)
                config.HeaderTableFields = headerTableFieldsElement.GetString()!;

            if (Helper.TryGetPropertyCaseInsensitive(root, AppConstants.FieldHeaderTableValues, out var headerTableValuesElement) && headerTableValuesElement.ValueKind == JsonValueKind.String)
                config.HeaderTableValues = headerTableValuesElement.GetString()!;

            if (Helper.TryGetPropertyCaseInsensitive(root, AppConstants.FieldHeaderMigrated, out var headerMigratedElement) && headerMigratedElement.ValueKind == JsonValueKind.String)
                config.HeaderMigrated = headerMigratedElement.GetString()!;

            return config;
        }
    }
}
