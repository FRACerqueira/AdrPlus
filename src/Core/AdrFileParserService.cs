// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the AdrPlus project under MIT license
// ***************************************************************************************

using AdrPlus.Domain;
using AdrPlus.Infrastructure.FileSystem;
using AdrPlus.Infrastructure.Formatting;
using System.Globalization;
using System.Net.Http.Headers;
using System.Text;

namespace AdrPlus.Core
{
    internal sealed class AdrFileParserService : IAdrFileParser
    {
        /// <inheritdoc/>
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
                    result.ErrorMessage = Resources.AdrPlus.ErrMsgAdrHeaderDisclaimerInvalid;
                    return (result, string.Empty);
                }
                result.Disclaimer = lines[0].Replace("<!-- ", string.Empty, ordinal).Replace(" -->", string.Empty, ordinal).Trim();

                //table header
                if (!lines[1].StartsWith("|Adr-Plus ", ordinal))
                {
                    result.ErrorMessage = Resources.AdrPlus.ErrMsgAdrInvalidHeader;
                    return (result, string.Empty);
                }
                if (lines[1].TrimEnd().EndsWith(" -->|", ordinal) && lines[1].Contains("<!-- ", ordinal))
                {
                    result.IsMigrated = true;
                }
                //table header separator
                if (!lines[2].StartsWith("|--|--|", ordinal))
                {
                    result.ErrorMessage = Resources.AdrPlus.ErrMsgAdrInvalidHeader;
                    return (result, string.Empty);
                }

                //title header
                if (!lines[3].StartsWith('|'))
                {
                    result.ErrorMessage = Resources.AdrPlus.ErrMsgAdrHeaderTitleInvalid;
                    return (result, string.Empty);
                }
                var indexstart = lines[3].IndexOf('|', 1);
                if (indexstart == -1)
                {
                    result.ErrorMessage = Resources.AdrPlus.ErrMsgAdrHeaderTitleInvalid;
                    return (result, string.Empty);
                }
                var indexend = lines[3].IndexOf('|', indexstart + 1);
                if (indexend == -1)
                {
                    result.ErrorMessage = Resources.AdrPlus.ErrMsgAdrHeaderTitleInvalid;
                    return (result, string.Empty);
                }
                result.Title = lines[3][(indexstart + 1)..indexend].Trim();

                //version header
                if (!lines[4].StartsWith('|'))
                {
                    result.ErrorMessage = Resources.AdrPlus.ErrMsgAdrHeaderVersionInvalid;
                    return (result, string.Empty);
                }
                indexstart = lines[4].IndexOf('|', 1);
                if (indexstart == -1)
                {
                    result.ErrorMessage = Resources.AdrPlus.ErrMsgAdrHeaderVersionInvalid;
                    return (result, string.Empty);
                }
                indexend = lines[4].IndexOf('|', indexstart + 1);
                if (indexend == -1)
                {
                    result.ErrorMessage = Resources.AdrPlus.ErrMsgAdrHeaderVersionInvalid;
                    return (result, string.Empty);
                }
                var versionText = lines[4][(indexstart + 1)..indexend].Trim();
                if (int.TryParse(versionText, null, out var version))
                {
                    result.Version = version;
                }
                else if (versionText.Length > 0)
                {
                    result.ErrorMessage = Resources.AdrPlus.ErrMsgAdrHeaderVersionInvalid;
                    return (result, string.Empty);
                }

                // revision header
                if (!lines[5].StartsWith('|'))
                {
                    result.ErrorMessage = Resources.AdrPlus.ErrMsgAdrHeaderRevisionInvalid;
                    return (result, string.Empty);
                }
                indexstart = lines[5].IndexOf('|', 1);
                if (indexstart == -1)
                {
                    result.ErrorMessage = Resources.AdrPlus.ErrMsgAdrHeaderRevisionInvalid;
                    return (result, string.Empty);
                }
                indexend = lines[5].IndexOf('|', indexstart + 1);
                if (indexend == -1)
                {
                    result.ErrorMessage = Resources.AdrPlus.ErrMsgAdrHeaderRevisionInvalid;
                    return (result, string.Empty);
                }
                var revisionText = lines[5][(indexstart + 1)..indexend].Trim();
                if (int.TryParse(revisionText, null, out var revision))
                {
                    result.Revision = revision;
                }
                else if (revisionText.Length > 0)
                {
                    result.ErrorMessage = Resources.AdrPlus.ErrMsgAdrHeaderRevisionInvalid;
                    return (result, string.Empty);
                }

                //scope header
                if (!lines[6].StartsWith('|'))
                {
                    result.ErrorMessage = Resources.AdrPlus.ErrMsgAdrHeaderScopeInvalid;
                    return (result, string.Empty);
                }
                indexstart = lines[6].IndexOf('|', 1);
                if (indexstart == -1)
                {
                    result.ErrorMessage = Resources.AdrPlus.ErrMsgAdrHeaderScopeInvalid;
                    return (result, string.Empty);
                }
                indexend = lines[6].IndexOf('|', indexstart + 1);
                if (indexend == -1)
                {
                    result.ErrorMessage = Resources.AdrPlus.ErrMsgAdrHeaderScopeInvalid;
                    return (result, string.Empty);
                }
                result.Scope = lines[6][(indexstart + 1)..indexend].Trim();

                //domain header
                if (!lines[7].StartsWith('|'))
                {
                    result.ErrorMessage = Resources.AdrPlus.ErrMsgAdrHeaderDomainInvalid;
                    return (result, string.Empty);
                }
                indexstart = lines[7].IndexOf('|', 1);
                if (indexstart == -1)
                {
                    result.ErrorMessage = Resources.AdrPlus.ErrMsgAdrHeaderDomainInvalid;
                    return (result, string.Empty);
                }
                indexend = lines[7].IndexOf('|', indexstart + 1);
                if (indexend == -1)
                {
                    result.ErrorMessage = Resources.AdrPlus.ErrMsgAdrHeaderDomainInvalid;
                    return (result, string.Empty);
                }
                result.Domain = lines[7][(indexstart + 1)..indexend].Trim();

                //status create header
                if (!lines[8].StartsWith('|'))
                {
                    result.ErrorMessage = Resources.AdrPlus.ErrMsgAdrHeaderStatusCreateLineInvalid;
                    return (result, string.Empty);
                }
                indexstart = lines[8].IndexOf('|', 1);
                if (indexstart == -1)
                {
                    result.ErrorMessage = Resources.AdrPlus.ErrMsgAdrHeaderStatusCreateLineInvalid;
                    return (result, string.Empty);
                }
                indexend = lines[8].IndexOf('|', indexstart + 1);
                if (indexend == -1)
                {
                    result.ErrorMessage = Resources.AdrPlus.ErrMsgAdrHeaderStatusCreateLineInvalid;
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
                    result.ErrorMessage = Resources.AdrPlus.ErrMsgAdrHeaderStatusChangeLineInvalid;
                    return (result, string.Empty);
                }
                indexstart = lines[9].IndexOf('|', 1);
                if (indexstart == -1)
                {
                    result.ErrorMessage = Resources.AdrPlus.ErrMsgAdrHeaderStatusChangeLineInvalid;
                    return (result, string.Empty);
                }
                indexend = lines[9].IndexOf('|', indexstart + 1);
                if (indexend == -1)
                {
                    result.ErrorMessage = Resources.AdrPlus.ErrMsgAdrHeaderStatusChangeLineInvalid;
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
                    result.ErrorMessage = Resources.AdrPlus.ErrMsgAdrHeaderStatusSupersededLineInvalid;
                    return (result, string.Empty);
                }
                indexstart = lines[10].IndexOf('|', 1);
                if (indexstart == -1)
                {
                    result.ErrorMessage = Resources.AdrPlus.ErrMsgAdrHeaderStatusSupersededLineInvalid;
                    return (result, string.Empty);
                }
                indexend = lines[10].IndexOf('|', indexstart + 1);
                if (indexend == -1)
                {
                    result.ErrorMessage = Resources.AdrPlus.ErrMsgAdrHeaderStatusSupersededLineInvalid;
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
                    result.ErrorMessage = Resources.AdrPlus.ErrMsgAdrHeaderDisclaimerInvalid;
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

        /// <inheritdoc/>
        public async Task<AdrFileNameComponents> ParseFileName(string filePath, AdrPlusRepoConfig config, IFileSystemService fileSystemService)
        {
            const StringComparison ordinalIgnoreCase = StringComparison.OrdinalIgnoreCase;
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
                var (header, content) = await ParseAdrHeaderAndContentAsync(filePath, config, fileSystemService);
                result.Header = header;
                result.ContentAdr = content;
                result.IsValid = true;
            }
            result.ErrorMessage = parseResult.Result.ErrorMessage;
            return result;
        }

        private static (bool Success, AdrFileNameComponents Result) ParseAdrPlusFileNameAsync(string filePath, AdrPlusRepoConfig config)
        {
            const StringComparison ordinalIgnoreCase = StringComparison.OrdinalIgnoreCase;
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
            if (supersedeParts.Length == 2 && int.TryParse(supersedeParts[1], out _))
            {
                supersedeNumber = supersedeParts[1];
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
            var currentIndex = 0;
            string part = parts[currentIndex] ?? string.Empty;
            //prefix
            result.Prefix = config.Prefix ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(config.Prefix) && !part.StartsWith(config.Prefix, ordinalIgnoreCase))
            {
                result.ErrorMessage = string.Format(null, FormatMessages.ErrorFilenameNoPrefixFormat, config.Prefix);
                return (false, result);
            }

            if (!string.IsNullOrWhiteSpace(config.Prefix) && part.StartsWith(result.Prefix, ordinalIgnoreCase))
            {
                part = part[config.Prefix.Length..];
            }
            //sequence number
            if (part.Length < config.LenSeq)
            {
                result.ErrorMessage = string.Format(null, FormatMessages.ErrorInvalidNumberFormatMsg, part);
                return (false, result);
            }
            var tryNumberString = part[..config.LenSeq];
            if (!int.TryParse(tryNumberString, out var numberseq))
            {
                result.ErrorMessage = string.Format(null, FormatMessages.ErrorInvalidNumberFormatMsg, part);
                return (false, result);
            }
            result.Number = numberseq;
            part = part[config.LenSeq..];

            //version
            if (part.Length < config.LenVersion + 1)
            {
                result.ErrorMessage = string.Format(null, FormatMessages.ErrorInvalidVersionFormatMsg, part);
                return (false, result);
            }
            if (!part.StartsWith('V') && !part.StartsWith('v'))
            {
                result.ErrorMessage = string.Format(null, FormatMessages.ErrorInvalidVersionFormatMsg, part);
                return (false, result);
            }
            string tryversionNumberString = part[1..];
            if (!int.TryParse(tryversionNumberString, out var numberver))
            {
                result.ErrorMessage = string.Format(null, FormatMessages.ErrorInvalidVersionFormatMsg, tryversionNumberString);
                return (false, result);
            }
            result.Version = numberver;
            part = part[(config.LenVersion + 1)..];
            //revision
            if (config.LenRevision > 0 && part.Length < config.LenRevision + 1)
            {
                result.ErrorMessage = string.Format(null, FormatMessages.ErrorInvalidRevisionFormatMsg, part);
                return (false, result);
            }
            if (config.LenRevision > 0)
            {
                if (!part.StartsWith('R') && !part.StartsWith('r'))
                {
                    result.ErrorMessage = string.Format(null, FormatMessages.ErrorInvalidRevisionFormatMsg, part);
                    return (false, result);
                }
                string tryrevisionNumberString = part[1..];
                if (!int.TryParse(tryrevisionNumberString, out var numberrev))
                {
                    result.ErrorMessage = string.Format(null, FormatMessages.ErrorInvalidRevisionFormatMsg, tryrevisionNumberString);
                    return (false, result);
                }
                result.Revision = numberrev;
                part = part[(config.LenRevision + 1)..];
            }
            //scope
            if (config.LenScope > 0 && part.Length < config.LenScope)
            {
                result.ErrorMessage = string.Format(null, FormatMessages.ErrorInvalidScopeFormatMsg, part);
                return (false, result);
            }
            if (config.LenScope > 0)
            {
                result.Scope = part[(config.LenScope)..];
                part = part[(config.LenScope)..];
            }
            if (part.Length != 0)
            {
                result.ErrorMessage = Resources.AdrPlus.ErrorInvalidFilenameFormat;
                return (false, result);
            }
            //title and domain
            currentIndex = 1;
            part = parts[currentIndex];
            index = part.IndexOf('@');
            if (index != -1)
            {
                result.Title = Helper.Humanize(part[..index]);
                result.Domain = Helper.Humanize(part[(index + 1)..]);
            }
            else
            {
                result.Title = Helper.Humanize(part);
            }
            result.SupersededValue = string.IsNullOrEmpty(supersedeNumber) ? null : int.Parse(supersedeNumber, CultureInfo.InvariantCulture);
            return (true, result);
        }
    }
}
