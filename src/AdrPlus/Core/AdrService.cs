// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the AdrPlus project under MIT license
// ***************************************************************************************

using AdrPlus.Commands;
using AdrPlus.Domain;
using AdrPlus.Infrastructure.FileSystem;
using AdrPlus.Infrastructure.Formatting;
using Microsoft.Extensions.Configuration;
using System.Globalization;
using System.Text;
using System.Text.Json;

namespace AdrPlus.Core
{
    internal class AdrService(IConfiguration configuration) : IAdrServices
    {
        private readonly IConfiguration _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        private const StringComparison ordinalIgnoreCase = StringComparison.OrdinalIgnoreCase;

        public AdrPlusRepoConfig FromJson(string jsonString,string template)
        {
            if (string.IsNullOrWhiteSpace(jsonString))
                throw new ArgumentNullException(nameof(jsonString), Resources.AdrPlus.ExceptionJsonStringNull);

            using var jsonDoc = JsonDocument.Parse(jsonString, AppConstants.DocumentOptions);
            var root = jsonDoc.RootElement;

            var config = new AdrPlusRepoConfig(AppConstants.DefaultFolderAdr, template);

            if (Helper.TryGetPropertyCaseInsensitive(root, AppConstants.FieldFolderAdr, out var folderadrElement) && folderadrElement.ValueKind == JsonValueKind.String)
            {
                config.FolderAdr = folderadrElement.GetString()!;
            }

            if (Helper.TryGetPropertyCaseInsensitive(root, AppConstants.FieldMigrationPattern, out var migrationPatternElement) && migrationPatternElement.ValueKind == JsonValueKind.String)
            {
                config.MigrationPattern = migrationPatternElement.GetString()!;
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

        public async Task<(AdrHeader header, string content)> ParseAdrHeaderAndContentAsync(string filePath, AdrPlusRepoConfig config, IFileSystemService fileSystemService)
        {
            var lines = await fileSystemService.ReadAllLinesAsync(filePath);

            var result = new AdrHeader();
            const StringComparison ordinal = StringComparison.Ordinal;
            try
            {
                if (lines.Length == 0)
                {
                    result.ErrorMessage = Resources.AdrPlus.ErrMsgAdrFileEmpty;
                    return (result, string.Empty);
                }

                if (lines.Length < AppConstants.LenghtHeader)
                {
                    result.ErrorMessage = Resources.AdrPlus.ErrMsgAdrTooShort;
                    return (result, string.Empty);
                }
                //disclaimer
                if (!lines[0].StartsWith("<!-- ", ordinal) || !lines[0].TrimEnd().EndsWith(" -->", ordinal))
                {
                    result.ErrorMessage = string.Format(CultureInfo.CurrentCulture, FormatMessages.ErrAdrFieldHeaderNotFound, Resources.AdrPlus.Comment);
                    return (result, string.Empty);
                }
                result.Disclaimer = lines[0].Replace("<!-- ", string.Empty, ordinal).Replace(" -->", string.Empty, ordinal).Trim();

                //table header
                if (!lines[1].StartsWith("|Adr-Plus ", ordinal))
                {
                    result.ErrorMessage = Resources.AdrPlus.InvalidFormatHeader;
                    return (result, string.Empty);
                }
                if (lines[1].TrimEnd().EndsWith(" -->|", ordinal) && lines[1].Contains("<!-- ", ordinal))
                {
                    result.IsMigrated = true;
                }
                //table header separator
                if (!lines[2].StartsWith("|--|--|", ordinal))
                {
                    result.ErrorMessage = Resources.AdrPlus.InvalidFormatHeader;
                    return (result, string.Empty);
                }

                //title header
                if (!lines[3].StartsWith('|'))
                {
                    result.ErrorMessage = Resources.AdrPlus.ErrMsgAdrFieldHeaderNotFound;
                    return (result, string.Empty);
                }
                var indexstart = lines[3].IndexOf('|', 1);
                if (indexstart == -1)
                {
                    result.ErrorMessage = string.Format(CultureInfo.CurrentCulture, FormatMessages.ErrAdrFieldHeaderNotFound, Resources.AdrPlus.Title);
                    return (result, string.Empty);
                }
                var indexend = lines[3].IndexOf('|', indexstart + 1);
                if (indexend == -1)
                {
                    result.ErrorMessage = string.Format(CultureInfo.CurrentCulture, FormatMessages.ErrAdrFieldHeaderNotFound, Resources.AdrPlus.Title);
                    return (result, string.Empty);
                }
                result.Title = lines[3][(indexstart + 1)..indexend].Trim();

                //version header
                if (!lines[4].StartsWith('|'))
                {
                    result.ErrorMessage = string.Format(CultureInfo.CurrentCulture, FormatMessages.ErrAdrFieldHeaderNotFound, Resources.AdrPlus.Version);
                    return (result, string.Empty);
                }
                indexstart = lines[4].IndexOf('|', 1);
                if (indexstart == -1)
                {
                    result.ErrorMessage = string.Format(CultureInfo.CurrentCulture, FormatMessages.ErrAdrFieldHeaderNotFound, Resources.AdrPlus.Version);
                    return (result, string.Empty);
                }
                indexend = lines[4].IndexOf('|', indexstart + 1);
                if (indexend == -1)
                {
                    result.ErrorMessage = string.Format(CultureInfo.CurrentCulture, FormatMessages.ErrAdrFieldHeaderNotFound, Resources.AdrPlus.Version);
                    return (result, string.Empty);
                }
                var versionText = lines[4][(indexstart + 1)..indexend].Trim();
                if (int.TryParse(versionText, null, out var version))
                {
                    result.Version = version;
                }
                else if (versionText.Length > 0)
                {
                    result.ErrorMessage = string.Format(CultureInfo.CurrentCulture, FormatMessages.ErrAdrFieldHeaderNotFound, Resources.AdrPlus.Version);
                    return (result, string.Empty);
                }

                // revision header
                if (!lines[5].StartsWith('|'))
                {
                    result.ErrorMessage = string.Format(CultureInfo.CurrentCulture, FormatMessages.ErrAdrFieldHeaderNotFound, Resources.AdrPlus.Revision);
                    return (result, string.Empty);
                }
                indexstart = lines[5].IndexOf('|', 1);
                if (indexstart == -1)
                {
                    result.ErrorMessage = string.Format(CultureInfo.CurrentCulture, FormatMessages.ErrAdrFieldHeaderNotFound, Resources.AdrPlus.Revision);
                    return (result, string.Empty);
                }
                indexend = lines[5].IndexOf('|', indexstart + 1);
                if (indexend == -1)
                {
                    result.ErrorMessage = string.Format(CultureInfo.CurrentCulture, FormatMessages.ErrAdrFieldHeaderNotFound, Resources.AdrPlus.Revision);
                    return (result, string.Empty);
                }
                var revisionText = lines[5][(indexstart + 1)..indexend].Trim();
                if (int.TryParse(revisionText, null, out var revision))
                {
                    result.Revision = revision;
                }
                else if (revisionText.Length > 0)
                {
                    result.ErrorMessage = string.Format(CultureInfo.CurrentCulture, FormatMessages.ErrAdrFieldHeaderNotFound, Resources.AdrPlus.Revision);
                    return (result, string.Empty);
                }

                //scope header
                if (!lines[6].StartsWith('|'))
                {
                    result.ErrorMessage = string.Format(CultureInfo.CurrentCulture, FormatMessages.ErrAdrFieldHeaderNotFound, Resources.AdrPlus.Scope);
                    return (result, string.Empty);
                }
                indexstart = lines[6].IndexOf('|', 1);
                if (indexstart == -1)
                {
                    result.ErrorMessage = string.Format(CultureInfo.CurrentCulture, FormatMessages.ErrAdrFieldHeaderNotFound, Resources.AdrPlus.Scope);
                    return (result, string.Empty);
                }
                indexend = lines[6].IndexOf('|', indexstart + 1);
                if (indexend == -1)
                {
                    result.ErrorMessage = string.Format(CultureInfo.CurrentCulture, FormatMessages.ErrAdrFieldHeaderNotFound, Resources.AdrPlus.Scope);
                    return (result, string.Empty);
                }
                result.Scope = lines[6][(indexstart + 1)..indexend].Trim();

                //domain header
                if (!lines[7].StartsWith('|'))
                {
                    result.ErrorMessage = string.Format(CultureInfo.CurrentCulture, FormatMessages.ErrAdrFieldHeaderNotFound, Resources.AdrPlus.Domain);
                    return (result, string.Empty);
                }
                indexstart = lines[7].IndexOf('|', 1);
                if (indexstart == -1)
                {
                    result.ErrorMessage = string.Format(CultureInfo.CurrentCulture, FormatMessages.ErrAdrFieldHeaderNotFound, Resources.AdrPlus.Domain);
                    return (result, string.Empty);
                }
                indexend = lines[7].IndexOf('|', indexstart + 1);
                if (indexend == -1)
                {
                    result.ErrorMessage = string.Format(CultureInfo.CurrentCulture, FormatMessages.ErrAdrFieldHeaderNotFound, Resources.AdrPlus.Domain);
                    return (result, string.Empty);
                }
                result.Domain = lines[7][(indexstart + 1)..indexend].Trim();

                //status create header
                if (!lines[8].StartsWith('|'))
                {
                    result.ErrorMessage = string.Format(CultureInfo.CurrentCulture, FormatMessages.ErrAdrFieldHeaderNotFound, Resources.AdrPlus.StatusCreated);
                    return (result, string.Empty);
                }
                indexstart = lines[8].IndexOf('|', 1);
                if (indexstart == -1)
                {
                    result.ErrorMessage = string.Format(CultureInfo.CurrentCulture, FormatMessages.ErrAdrFieldHeaderNotFound, Resources.AdrPlus.StatusCreated);
                    return (result, string.Empty);
                }
                indexend = lines[8].IndexOf('|', indexstart + 1);
                if (indexend == -1)
                {
                    result.ErrorMessage = string.Format(CultureInfo.CurrentCulture, FormatMessages.ErrAdrFieldHeaderNotFound, Resources.AdrPlus.StatusCreated);
                    return (result, string.Empty);
                }
                var linestatus = lines[8][(indexstart + 1)..indexend].Trim();
                if (linestatus.Length > 0)
                {
                    var (statusCreate, dateCreate, errorCreate) = Helper.ParseStatusLine(linestatus, config);
                    if (!string.IsNullOrEmpty(errorCreate))
                    {
                        result.ErrorMessage = errorCreate;
                        return (result, string.Empty);
                    }
                    result.StatusCreate = statusCreate;
                    result.DateCreate = dateCreate;
                }


                // status update header 
                if (!lines[9].StartsWith('|'))
                {
                    result.ErrorMessage = string.Format(CultureInfo.CurrentCulture, FormatMessages.ErrAdrFieldHeaderNotFound, Resources.AdrPlus.StatusUpdated);
                    return (result, string.Empty);
                }
                indexstart = lines[9].IndexOf('|', 1);
                if (indexstart == -1)
                {
                    result.ErrorMessage = string.Format(CultureInfo.CurrentCulture, FormatMessages.ErrAdrFieldHeaderNotFound, Resources.AdrPlus.StatusUpdated);
                    return (result, string.Empty);
                }
                indexend = lines[9].IndexOf('|', indexstart + 1);
                if (indexend == -1)
                {
                    result.ErrorMessage = string.Format(CultureInfo.CurrentCulture, FormatMessages.ErrAdrFieldHeaderNotFound, Resources.AdrPlus.StatusUpdated);
                    return (result, string.Empty);
                }
                linestatus = lines[9][(indexstart + 1)..indexend].Trim();
                if (linestatus.Length > 0)
                {
                    var (statusChange, dateChange, errorChange) = Helper.ParseStatusLine(linestatus, config);
                    if (!string.IsNullOrEmpty(errorChange))
                    {
                        result.ErrorMessage = errorChange;
                        return (result, string.Empty);
                    }
                    result.StatusUpdate = statusChange;
                    result.DateUpdate = dateChange;
                }

                // status Superseded header 
                if (!lines[10].StartsWith('|'))
                {
                    result.ErrorMessage = string.Format(CultureInfo.CurrentCulture, FormatMessages.ErrAdrFieldHeaderNotFound, Resources.AdrPlus.StatusSuperseded);
                    return (result, string.Empty);
                }
                indexstart = lines[10].IndexOf('|', 1);
                if (indexstart == -1)
                {
                    result.ErrorMessage = string.Format(CultureInfo.CurrentCulture, FormatMessages.ErrAdrFieldHeaderNotFound, Resources.AdrPlus.StatusSuperseded);
                    return (result, string.Empty);
                }
                indexend = lines[10].IndexOf('|', indexstart + 1);
                if (indexend == -1)
                {
                    result.ErrorMessage = string.Format(CultureInfo.CurrentCulture, FormatMessages.ErrAdrFieldHeaderNotFound, Resources.AdrPlus.StatusSuperseded);
                    return (result, string.Empty);
                }
                linestatus = lines[10][(indexstart + 1)..indexend].Trim();
                var (statusSuperseded, dateSuperseded, errorSuperseded) = Helper.ParseStatusLine(linestatus, config);
                if (linestatus.Length > 0)
                {
                    if (!string.IsNullOrEmpty(errorSuperseded))
                    {
                        result.ErrorMessage = errorSuperseded;
                        return (result, string.Empty);
                    }
                    result.StatusChange = statusSuperseded;
                    result.DateChange = dateSuperseded;
                    var indexfile = linestatus.IndexOf(':', ordinal);
                    if (indexfile < 0)
                    {
                        result.ErrorMessage = Resources.AdrPlus.ErrMsgAdrStatusLineSupersedeFormatInvalid;
                        return (result, string.Empty);
                    }
                    var fileSuperSedes = linestatus[(indexfile + 1)..].Trim();
                    result.NumberSuperSedes = fileSuperSedes;
                }

                //disclaimer
                if (!lines[11].StartsWith("<!-- ", ordinal) || !lines[11].TrimEnd().EndsWith(" -->", ordinal))
                {
                    result.ErrorMessage = string.Format(CultureInfo.CurrentCulture, FormatMessages.ErrAdrFieldHeaderNotFound, Resources.AdrPlus.Comment);
                    return (result, string.Empty);
                }
                result.IsValid = true;
            }
            catch (Exception ex)
            {
                result.IsValid = false;
                result.ErrorMessage = string.Format(null, CompositeFormat.Parse(Resources.AdrPlus.ErrMsgAdrHeaderParsingError), ex.Message);
            }
            var content = string.Join(Environment.NewLine, lines.Skip(AppConstants.LenghtHeader));
            if (lines.Length > AppConstants.LenghtHeader)
            {
                content += Environment.NewLine;
            }
            return (result, content);
        }

        public async Task<AdrFileNameComponents> ParseFileName(string filePath, AdrPlusRepoConfig config, IFileSystemService fileSystemService)
        {
            var result = new AdrFileNameComponents
            {
                FileName = filePath
            };
            if (string.IsNullOrWhiteSpace(filePath))
            {
                result.ErrorMessage = Resources.AdrPlus.ExceptionFilenameEmpty;
                return result;
            }
            if (!filePath.EndsWith(".md", ordinalIgnoreCase))
            {
                result.ErrorMessage = Resources.AdrPlus.ExceptionFilenameMustHaveMdExtension;
                return result;
            }
            //try parse with configured ADRLUS format
            var parseResult = ParseAdrPlusFileNameAsync(filePath, config);
            if (parseResult.Success)
            {
                result = parseResult.Result;
                var (header, content) = await ParseAdrHeaderAndContentAsync(filePath, config, fileSystemService);
                result.Header = header;
                result.ContentAdr = content;
                result.IsValid = true;
                return result;
            }
            else
            {
                result = parseResult.Result;
            }
            if (config.MigrationPattern.Length > 0 && PatternParser.ParseMigratePattern(config.MigrationPattern) != null)
            {
                var (Success, resultMigration) = ParseMigrationFileNameAsync(filePath, config);
                if (Success)
                {
                    result = resultMigration;
                    var (header, content) = await ParseAdrHeaderAndContentAsync(filePath, config, fileSystemService);
                    result.Header = header;
                    result.ContentAdr = content;
                    result.IsValid = true;
                    return result;
                }
                else
                {
                    result = resultMigration;
                }
            }
            // If filename parsing failed, try to load the file header anyway to report header errors ?
            return result;
        }

        private static (bool Success, AdrFileNameComponents Result) ParseMigrationFileNameAsync(string filePath, AdrPlusRepoConfig config)
        {
            var result = new AdrFileNameComponents
            {
                FileName = filePath
            };
            var nameWithoutExtension = Path.GetFileNameWithoutExtension(filePath);
            var pattern = PatternParser.ParseMigratePattern(config.MigrationPattern)!;
            if (!pattern.TryGetValue("P", out var valueprefix) && nameWithoutExtension.Length < valueprefix.Position + valueprefix.Length)
            {
                result.ErrorMessage = Resources.AdrPlus.ErrorInvalidFilenameFormat;
                return (false, result);
            }
            if (!pattern.TryGetValue("N", out var valueseq) || nameWithoutExtension.Length < valueseq.Position + valueseq.Length)
            {
                result.ErrorMessage = Resources.AdrPlus.ErrorInvalidFilenameFormat;
                return (false, result);
            }
            if (!pattern.TryGetValue("V", out var valueversion) && nameWithoutExtension.Length < valueversion.Position + valueversion.Length)
            {
                result.ErrorMessage = Resources.AdrPlus.ErrorInvalidFilenameFormat;
                return (false, result);
            }
            if (!pattern.TryGetValue("R", out var valuerevison) && nameWithoutExtension.Length < valuerevison.Position + valuerevison.Length)
            {
                result.ErrorMessage = Resources.AdrPlus.ErrorInvalidFilenameFormat;
                return (false, result);
            }
            if (!pattern.TryGetValue("T", out var valuetitle) || nameWithoutExtension.Length < valuetitle.Position)
            {
                result.ErrorMessage = Resources.AdrPlus.ErrorInvalidFilenameFormat;
                return (false, result);
            }
            result.Prefix = valueprefix.Length > 0 ? nameWithoutExtension.Substring(valueprefix.Position, valueprefix.Length) : string.Empty;
            result.Number = valueseq.Length > 0 && int.TryParse(nameWithoutExtension.AsSpan(valueseq.Position, valueseq.Length), CultureInfo.InvariantCulture, out var numberseq) ? numberseq : 0;
            result.Version = valueversion.Length > 0 && int.TryParse(nameWithoutExtension.AsSpan(valueversion.Position, valueversion.Length), CultureInfo.InvariantCulture, out var numberver) ? numberver : 0;
            result.Revision = valuerevison.Length > 0 && int.TryParse(nameWithoutExtension.AsSpan(valuerevison.Position, valuerevison.Length), CultureInfo.InvariantCulture, out var numberrev) ? numberrev : 0;
            result.Title = valuetitle.Position < nameWithoutExtension.Length ? nameWithoutExtension[valuetitle.Position..] : string.Empty;
            return (true, result);
        }

        private static (bool Success, AdrFileNameComponents Result) ParseAdrPlusFileNameAsync(string filePath, AdrPlusRepoConfig config)
        {
            var result = new AdrFileNameComponents
            {
                FileName = filePath
            };
            var nameWithoutExtension = Path.GetFileNameWithoutExtension(filePath);
            var separator = $"{config.Separator}";
            var supersedeSeparator = $"{config.Separator}{config.Separator}";
            var supersedeParts = nameWithoutExtension.Split(supersedeSeparator, StringSplitOptions.RemoveEmptyEntries);
            var parts = new List<string>();
            var supersedeNumber = string.Empty;
            if (supersedeParts.Length > 2)
            {
                result.ErrorMessage = Resources.AdrPlus.ErrorInvalidFilenameFormat;
                return (false, result);
            }
            if (supersedeParts.Length == 2)
            {
                if (int.TryParse(supersedeParts[1], out var supersedeNumberValue))
                {
                    supersedeNumber = supersedeParts[1];
                }
                else
                {
                    result.ErrorMessage = Resources.AdrPlus.ErrorInvalidFilenameFormat;
                    return (false, result);
                }
            }
            var index = supersedeParts[0].IndexOf(separator, ordinalIgnoreCase);
            if (index < 0)
            {
                result.ErrorMessage = Resources.AdrPlus.ErrorInvalidFilenameFormat;
                return (false, result);
            }
            parts.Add(supersedeParts[0][..index]);
            parts.AddRange(supersedeParts[0][(index + separator.Length)..].Split(separator, StringSplitOptions.RemoveEmptyEntries));
            if (parts.Count > 2)
            {
                string part1 = parts[1..].Aggregate((a, b) => $"{a}{config.Separator}{b}");
                parts.Clear();
                parts.Add(supersedeParts[0][..index]);
                parts.Add(part1);
            }
            if (parts.Count != 2)
            {
                result.ErrorMessage = Resources.AdrPlus.ErrorInvalidFilenameFormat;
                return (false, result);
            }
            //first part is prefix, number, version, revision and scope
            string part = parts[0] ?? string.Empty;
            var pattern = PatternParser.ParseAdrPattern(part);
            if (pattern == null)
            {
                result.ErrorMessage = Resources.AdrPlus.ErrorInvalidFilenameFormat;
                return (false, result);
            }
            //prefix
            result.Prefix = pattern["P"];
            //number
            if (!int.TryParse(pattern["N"], CultureInfo.InvariantCulture, out var _))
            {
                result.ErrorMessage = Resources.AdrPlus.ErrorInvalidFilenameFormat;
                return (false, result);
            }
            if (pattern["V"].Length > 0 && !int.TryParse(pattern["V"], CultureInfo.InvariantCulture, out var _))
            {
                result.ErrorMessage = Resources.AdrPlus.ErrorInvalidFilenameFormat;
                return (false, result);
            }
            if (pattern["R"].Length > 0 && !int.TryParse(pattern["R"], CultureInfo.InvariantCulture, out var _))
            {
                result.ErrorMessage = Resources.AdrPlus.ErrorInvalidFilenameFormat;
                return (false, result);
            }
            result.Number = int.Parse(pattern["N"], CultureInfo.InvariantCulture);
            //version
            result.Version = pattern["V"].Length == 0 ? 0 : int.Parse(pattern["V"], CultureInfo.InvariantCulture);
            //revision
            result.Revision = pattern["R"].Length == 0 ? 0 : int.Parse(pattern["R"], CultureInfo.InvariantCulture);
            //scope
            result.Scope = pattern["S"].Length == 0 ? "" : pattern["S"];

            //invalid version
            if (result.Scope.Length > 0 && pattern["V"].Length == 0 && config.LenVersion > 0 && result.Scope.StartsWith("V", StringComparison.OrdinalIgnoreCase))
            {
                result.ErrorMessage = Resources.AdrPlus.ErrorInvalidFilenameFormat;
                return (false, result);
            }

            //invalid revision
            if (result.Scope.Length > 0 && pattern["R"].Length == 0 && config.LenRevision > 0 && result.Scope.StartsWith("R", StringComparison.OrdinalIgnoreCase))
            {
                result.ErrorMessage = Resources.AdrPlus.ErrorInvalidFilenameFormat;
                return (false, result);
            }
            //title and domain
            part = parts[1];
            if (part.Length == 0)
            {
                result.ErrorMessage = Resources.AdrPlus.ErrorInvalidFilenameFormat;
                return (false, result);
            }
            index = part.LastIndexOf('@');
            if (index != -1)
            {
                result.Title = part[..index];
                if (index + 1 < part.Length)
                {
                    result.Domain = part[(index + 1)..];
                }
            }
            else
            {
                result.Title = part;
            }
            result.SupersededValue = string.IsNullOrEmpty(supersedeNumber) ? null : int.Parse(supersedeNumber, CultureInfo.InvariantCulture);
            return (true, result);
        }

        public async Task<AdrFileNameComponents[]> ReadAllAdrByNumber(int sequence, IFileSystemService fileSystemService, string rootpath, AdrPlusRepoConfig config)
        {
            ArgumentNullException.ThrowIfNull(config);
            ArgumentNullException.ThrowIfNull(fileSystemService);

            if (string.IsNullOrWhiteSpace(rootpath))
            {
                throw new ArgumentException(Resources.AdrPlus.ExceptionDirectoryPathEmpty, nameof(rootpath));
            }

            if (!fileSystemService.DirectoryExists(rootpath))
            {
                throw new DirectoryNotFoundException(string.Format(null, FormatMessages.ErrDirectoryNotFoundFormat, rootpath));
            }

            var result = new List<AdrFileNameComponents>();
            var searchPattern = $"*{sequence}*.md";
            var adrfolder = Path.GetFullPath(Path.Combine(rootpath, config.FolderAdr));
            if (!fileSystemService.DirectoryExists(adrfolder))
            {
                return [];
            }
            var mdFiles = fileSystemService.GetFiles(adrfolder, searchPattern);

            foreach (var filePath in mdFiles)
            {
                var aux = await ParseFileName(filePath, config, fileSystemService);
                if (aux.IsValid && (aux.Header.IsValid || aux.Header.IsMigrated) && aux.Number == sequence)
                {
                    result.Add(aux);
                }
            }
            return [.. result];
        }

        public async Task<AdrFileNameComponents[]> ReadAllAdr(IFileSystemService fileSystemService, string directoryPath, AdrPlusRepoConfig config, bool includeNotMatched = false)
        {
            ArgumentNullException.ThrowIfNull(config);
            ArgumentNullException.ThrowIfNull(fileSystemService);

            if (string.IsNullOrWhiteSpace(directoryPath))
            {
                throw new ArgumentException(Resources.AdrPlus.ExceptionDirectoryPathEmpty, nameof(directoryPath));
            }

            if (!fileSystemService.DirectoryExists(directoryPath))
            {
                throw new DirectoryNotFoundException(string.Format(null, FormatMessages.ErrDirectoryNotFoundFormat, directoryPath));
            }
            var result = new List<AdrFileNameComponents>();
            var folderadr = Path.GetFullPath(Path.Combine(directoryPath, config.FolderAdr));
            if (!fileSystemService.DirectoryExists(folderadr))
            {
                return [];
            }
            var mdFiles = fileSystemService.GetFiles(folderadr, "*.md", SearchOption.AllDirectories);

            foreach (var filePath in mdFiles)
            {
                var parsedComponents = await ParseFileName(filePath, config, fileSystemService);
                if (!parsedComponents.IsValid && !includeNotMatched)
                {
                    continue;
                }
                result.Add(parsedComponents);
            }
            return [.. result
                .OrderByDescending(x => x.Header.IsValid)
                .ThenBy(x => x.Header.IsMigrated)
                .ThenByDescending(x=> x.Number)
                .ThenByDescending(x=> x.Version)
                .ThenByDescending(x=> x.Revision ?? 0)];
        }

        public async Task<string> GetFileByUniqueTitle(string title, string domain, IFileSystemService fileSystemService, string rootrepo, AdrPlusRepoConfig config)
        {
            var uniqueTitle = AdrFileNameComponents.CreateUniqueTitle(title.ToCase(config.CaseTransform), domain.ToCase(config.CaseTransform));
            AdrFileNameComponents[] adrFiles = await ReadAllAdr(fileSystemService, rootrepo, config);
            var aux = adrFiles
                .FirstOrDefault(f => f.UniqueTitle == uniqueTitle);
            return aux?.FileName ?? string.Empty;
        }

        public async Task<int> GetNextNumber(IFileSystemService fileSystemService, string directoryPath, AdrPlusRepoConfig config)
        {
            AdrFileNameComponents[] adrFiles = await ReadAllAdr(fileSystemService, directoryPath, config);
            return adrFiles.Length == 0 ? 1 : adrFiles.Max(f => f.Number) + 1;
        }

        public async Task<AdrFileNameComponents?> GetLatestADRSequence(int sequence, IFileSystemService fileSystemService, string rootpath, AdrPlusRepoConfig config)
        {
            return (await ReadAllAdrByNumber(sequence, fileSystemService, rootpath, config))
                .OrderBy(x => x.Version)
                .ThenBy(x => x.Revision ?? 0)
                .Last();
        }

        public async Task<string[]> GetDomains(IFileSystemService fileSystemService, string directoryPath, AdrPlusRepoConfig config)
        {
            AdrFileNameComponents[] adrFiles = await ReadAllAdr(fileSystemService, directoryPath, config);
            return adrFiles.Length == 0
                ? []
                : [.. adrFiles
                    .Where(f => !string.IsNullOrWhiteSpace(f.Domain))
                    .DistinctBy(x => x.Domain!.ToPascalCase())
                    .Select(f => f.Domain!)];
        }

        public async Task<(bool Isvalid, string Error, AdrRecord? Record, string? Content)> StatusUpdateAdrAsync(string fullpath, AdrStatus adrStatus, DateTime dref, AdrPlusRepoConfig config, IFileSystemService fileSystemService, CancellationToken cancellationToken)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(fullpath);
            ArgumentNullException.ThrowIfNull(config);
            ArgumentNullException.ThrowIfNull(fileSystemService);
            var parsefile = await ParseFileName(fullpath, config, fileSystemService);
            if (!parsefile.IsValid)
            {
                return (false, parsefile.ErrorMessage, null, null);
            }
            if (!parsefile.Header.IsValid)
            {
                return (false, parsefile.Header.ErrorMessage, null, null);
            }
            parsefile.Header.StatusUpdate = adrStatus;
            if (adrStatus != AdrStatus.Unknown)
            {
                parsefile.Header.DateUpdate = dref;
            }

            var record = Helper.CreateAdrRecord(parsefile, config);
            var contentfile = $"{record.GetHeader(config, null, parsefile.Header.IsMigrated)}{record.Template}";
            await fileSystemService.WriteAllTextAsync(fullpath, contentfile, cancellationToken);
            return (true, string.Empty, record, contentfile);
        }

        public async Task<(bool IsValid, string Error, AdrRecord? Record, string? Content)> StatusChangeSupersedeAdrAsync(string fullpath, string seqsupersede, DateTime dref, AdrPlusRepoConfig config, IFileSystemService fileSystemService, CancellationToken cancellationToken)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(fullpath);
            ArgumentException.ThrowIfNullOrWhiteSpace(seqsupersede);
            ArgumentNullException.ThrowIfNull(config);
            ArgumentNullException.ThrowIfNull(fileSystemService);
            var parsefile = await ParseFileName(fullpath, config, fileSystemService);
            if (!parsefile.IsValid)
            {
                return (false, parsefile.ErrorMessage, null, null);
            }
            if (!parsefile.Header.IsValid)
            {
                return (false, parsefile.Header.ErrorMessage, null, null);
            }
            parsefile.Header.StatusChange = AdrStatus.Superseded;
            parsefile.Header.DateChange = dref;
            var record = Helper.CreateAdrRecord(parsefile, config);
            var contentfile = $"{record.GetHeader(config, seqsupersede, parsefile.Header.IsMigrated)}{record.Template}";
            await fileSystemService.WriteAllTextAsync(fullpath, contentfile, cancellationToken);
            return (true, string.Empty, record, contentfile);
        }

        public async Task<(bool IsValid, string Error, AdrRecord? Record, string? Content)> StatusChangeAdrAsync(string fullpath, AdrStatus adrStatus, DateTime dref, AdrPlusRepoConfig config, IFileSystemService fileSystemService, CancellationToken cancellationToken)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(fullpath);
            ArgumentNullException.ThrowIfNull(config);
            ArgumentNullException.ThrowIfNull(fileSystemService);
            var parsefile = await ParseFileName(fullpath, config, fileSystemService);
            if (!parsefile.IsValid)
            {
                return (false, parsefile.ErrorMessage, null, null);
            }
            if (!parsefile.Header.IsValid)
            {
                return (false, parsefile.Header.ErrorMessage, null, null);
            }
            parsefile.Header.StatusChange = adrStatus;
            parsefile.Header.DateChange = dref;
            var record = Helper.CreateAdrRecord(parsefile, config);
            var contentfile = $"{record.GetHeader(config, null, parsefile.Header.IsMigrated)}{record.Template}";
            await fileSystemService.WriteAllTextAsync(fullpath, contentfile, cancellationToken);
            return (true, string.Empty, record, contentfile);
        }

        public Dictionary<string, Type> GenerateCommandsMap()
        {
            var cmds = GetCommands();
            var map = new Dictionary<string, Type>(cmds.Length, StringComparer.OrdinalIgnoreCase);
            foreach (var (_, alias, handlerCommand, _) in cmds)
            {
                map[alias] = handlerCommand;
            }
            return map;
        }

        public (CommandsAdr Command, string Alias, Type ConfigCommandHandler, string Description)[] GetCommands()
        {
            var enumType = typeof(CommandsAdr);
            var fields = enumType.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            var result = new List<(CommandsAdr Command, string Alias, Type HandlerCommand, string Description)>(fields.Length);
            foreach (var field in fields)
            {
                var cmd = (CommandsAdr)field.GetValue(null)!;
                if (field.GetCustomAttributes(typeof(CommandAttribute), false).FirstOrDefault() is CommandAttribute attribute)
                {
                    result.Add((cmd, attribute.AliasCommand, attribute.HandlerCommand, attribute.Description));
                }
            }
            return [.. result];
        }

        public Dictionary<Arguments, string> ParseArgs(string[] args, Arguments[] argsForCommand, string? defaultarg = null)
        {
            ArgumentNullException.ThrowIfNull(args);
            ArgumentNullException.ThrowIfNull(argsForCommand);

            var parsedArgs = new Dictionary<Arguments, string>(args.Length);

            if (args.Length == 0)
            {
                var section = _configuration.GetSection(AppConstants.DefaultSettingsRoot);
                if (!section.Exists())
                {
                    throw new InvalidDataException(Resources.AdrPlus.ErrMsgDefaultSettingsMissing);
                }
                var behaviorWithoutArgs = section[AppConstants.FieldWithoutArgs];
                Enum.TryParse<BehaviorWithoutArg>(behaviorWithoutArgs, true, out var behavior);
                switch (behavior)
                {
                    case BehaviorWithoutArg.Help:
                        args = ["-h"];
                        break;
                    case BehaviorWithoutArg.Wizard:
                        if (defaultarg != null)
                        {
                            if (!string.IsNullOrWhiteSpace(defaultarg))
                            {
                                args = [defaultarg];
                            }
                        }
                        else
                        {
                            args = ["-w"];
                        }
                        break;
                }
            }
            if (Array.IndexOf(args, "-h") >= 0 || Array.IndexOf(args, "--help") >= 0)
            {
                parsedArgs[Arguments.Help] = string.Empty;
                return parsedArgs;
            }

            var argsForCommandSet = new HashSet<Arguments>(argsForCommand);
            var haswizard = false;

            for (int i = 0; i < args.Length; i++)
            {
                var arg = args[i];
                bool matched = false;

                foreach (var metadata in s_argumentMetadata)
                {
                    if (arg == metadata.ShortCommand || arg == metadata.LongCommand)
                    {
                        if (!argsForCommandSet.Contains(metadata.CommandArg))
                        {
                            continue;
                        }
                        matched = true;
                        if (!haswizard)
                        {
                            haswizard = metadata.Usage == UsageArgumments.Wizard;
                        }
                        switch (metadata.Usage)
                        {
                            case UsageArgumments.Wizard:
                            case UsageArgumments.Optional:
                                parsedArgs[metadata.CommandArg] = string.Empty;
                                break;
                            case UsageArgumments.OptionalWithValue:
                            case UsageArgumments.OptionalWithValueWhenWizard:
                                if (i + 1 < args.Length && !args[i + 1].StartsWith('-'))
                                {
                                    parsedArgs[metadata.CommandArg] = args[i + 1];
                                    i++;
                                }
                                else
                                {
                                    throw new ArgumentException(
                                        string.Format(null, s_exceptionMissingArgumentValueFormat,
                                        arg, metadata.LongCommand));
                                }
                                break;
                        }
                        break;
                    }
                }
                if (!matched)
                {
                    throw new ArgumentException(
                        string.Format(null, s_exceptionInvalidArgumentTokenFormat, arg));
                }
            }
            if (!haswizard)
            {
                foreach (var metadata in parsedArgs.Keys)
                {
                    var argMetadata = s_argumentMetadata.First(x => x.CommandArg == metadata);
                    if (parsedArgs[argMetadata.CommandArg].Length == 0 && argMetadata.Usage == UsageArgumments.OptionalWithValueWhenWizard)
                    {
                        throw new ArgumentException(
                            string.Format(null, s_exceptionMissingRequiredArgumentFormat,
                            argMetadata.LongCommand, argMetadata.ShortCommand));
                    }
                }
            }
            return parsedArgs;
        }

        public string GetHelpText(string command, Arguments[] argsForCommand, string[] examples)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(command);
            ArgumentNullException.ThrowIfNull(argsForCommand);
            ArgumentNullException.ThrowIfNull(examples);

            var (_, alias, _, description) = GetCommands().FirstOrDefault(c => c.Alias.Equals(command, StringComparison.OrdinalIgnoreCase));
            if (string.IsNullOrEmpty(alias))
            {
                return string.Empty;
            }

            var argsForCommandSet = new HashSet<Arguments>(argsForCommand);
            var sb = new StringBuilder(512);
            sb.Append(Resources.AdrPlus.Usage);
            sb.AppendLine(" : ");
            sb.Append("  adrplus ");
            sb.Append(alias);
            sb.Append(" [");
            sb.Append(Resources.AdrPlus.Arguments);
            sb.AppendLine("]");
            sb.AppendLine();
            sb.Append(Resources.AdrPlus.Description);
            sb.AppendLine(" : ");
            sb.AppendLine(null, $"  {description}");
            sb.AppendLine();
            sb.Append(Resources.AdrPlus.Arguments);
            sb.AppendLine(" : ");

            foreach (var metadata in s_argumentMetadata)
            {
                if (!argsForCommandSet.Contains(metadata.CommandArg))
                {
                    continue;
                }

                var required = $" ({Resources.AdrPlus.Optional})";
                if (metadata.Usage == UsageArgumments.OptionalWithValueWhenWizard)
                {
                    required = $" ({Resources.AdrPlus.Required} {Resources.AdrPlus.WhenNotWizard})";
                }
                if (metadata.ValidValues.Length > 0)
                {
                    required += $" [{string.Join("|", metadata.ValidValues)}]";
                }
                sb.AppendLine(null, $"  {metadata.ShortCommand}, {metadata.LongCommand}{required}");
                sb.AppendLine(null, $"      {metadata.Description}");
                sb.AppendLine();
            }

            sb.Append(Resources.AdrPlus.Examples);
            sb.AppendLine(" : ");
            foreach (var example in examples)
            {
                sb.AppendLine(null, $"  {example}");
            }
            return sb.ToString();
        }

        private readonly record struct ArgumentMetadata(
              Arguments CommandArg,
              string ShortCommand,
              string LongCommand,
              UsageArgumments Usage,
              string[] ValidValues,
              string Description);

        private static readonly ArgumentMetadata[] s_argumentMetadata = InitializeArgumentMetadata();

        private static readonly CompositeFormat s_exceptionMissingArgumentValueFormat =
            CompositeFormat.Parse(Resources.AdrPlus.ExceptionMissingArgumentValue);

        private static readonly CompositeFormat s_exceptionInvalidArgumentTokenFormat =
            CompositeFormat.Parse(Resources.AdrPlus.ExceptionInvalidArgumentToken);

        private static readonly CompositeFormat s_exceptionMissingRequiredArgumentFormat =
            CompositeFormat.Parse(Resources.AdrPlus.ExceptionMissingRequiredArgument);

        private static ArgumentMetadata[] InitializeArgumentMetadata()
        {
            var enumType = typeof(Arguments);
            var fields = enumType.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            var metadataList = new List<ArgumentMetadata>(fields.Length);

            foreach (var field in fields)
            {
                if (field.GetCustomAttributes(typeof(CommandArgumentAttribute), false).FirstOrDefault() is CommandArgumentAttribute attribute)
                {
                    if (field.GetCustomAttributes(typeof(HelpUsageAttribute), false).FirstOrDefault() is HelpUsageAttribute usageattribute)
                    {
                        metadataList.Add(new ArgumentMetadata(
                            (Arguments)field.GetValue(null)!,
                            attribute.ShortCommand,
                            attribute.LongCommand,
                            usageattribute.Usage,
                            attribute.AliasesValues ?? [],
                            usageattribute.Description));
                    }
                }
            }
            return [.. metadataList];
        }

        public string OpenFile(string filepath, string command)
        {
            ArgumentNullException.ThrowIfNull(filepath);
            ArgumentNullException.ThrowIfNull(command);

            try
            {
                var startInfo = new System.Diagnostics.ProcessStartInfo
                {
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                if (OperatingSystem.IsWindows())
                {
                    startInfo.FileName = "cmd.exe";
                    startInfo.Arguments = $"/c \"{command}\"";
                }
                else if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
                {
                    startInfo.FileName = "sh";
                    startInfo.Arguments = $"-c \"{command}\"";
                }
                else
                {
                    startInfo.FileName = filepath;
                    startInfo.UseShellExecute = true;
                    startInfo.RedirectStandardOutput = false;
                    startInfo.RedirectStandardError = false;
                }

                using var process = System.Diagnostics.Process.Start(startInfo);
                if (process == null)
                {
                    return Resources.AdrPlus.NewAdrErrorFailedToStartProcess;
                }

                if (startInfo.RedirectStandardError)
                {
                    var error = process.StandardError.ReadToEnd();
                    process.WaitForExit();

                    if (process.ExitCode != 0 && !string.IsNullOrWhiteSpace(error))
                    {
                        return error;
                    }
                }

                return string.Empty;
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }


    }
}
