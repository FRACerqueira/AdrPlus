// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the AdrPlus project under MIT license
// ***************************************************************************************

using AdrPlus.Domain;
using AdrPlus.Infrastructure.FileSystem;
using AdrPlus.Infrastructure.Formatting;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace AdrPlus.Core
{
    internal sealed partial class AdrFileParserService : IAdrFileParser
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

                //table header separator
                if (!lines[2].StartsWith("|--|--|", ordinal))
                {
                    result.ErrorMessage = Resources.AdrPlus.ErrMsgAdrInvalidHeader;
                    return (result, string.Empty);
                }
                if (lines[2].StartsWith("|--|--|<!-- ", ordinal) && lines[2].TrimEnd().EndsWith(" -->", ordinal))
                { 
                    result.IsMigrated = true;
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
                else if(versionText.Length > 0)
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
                    result.FileSuperSedes = fileSuperSedes;
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
            try
            {
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

                var nameWithoutExtension = Path.GetFileName(filePath)[..^3];
                var separator = config.Separator;
                var parts = nameWithoutExtension.Split(separator, StringSplitOptions.RemoveEmptyEntries);

                if (parts.Length < 1)
                {
                    result.ErrorMessage = Resources.AdrPlus.ErrorInvalidFilenameFormat;
                    return result;
                }

                var currentIndex = 0;
                var part = parts[currentIndex];
                if (!string.IsNullOrWhiteSpace(config.Prefix))
                {
                    if (!part.StartsWith(config.Prefix, ordinalIgnoreCase))
                    {
                        var tryNumberString = part[..config.LenSeq];
                        if (int.TryParse(tryNumberString, out var number))
                        {
                            result.Prefix = config.Prefix;
                            result.Number = number;
                            if (part.Length > config.LenSeq)
                            {
                                var titlePart = part[config.LenSeq..];
                                result.Title = Humanizar(titlePart);
                            }
                            else
                            {
                                result.ErrorMessage = Resources.AdrPlus.ErrorInvalidFilenameFormat;
                                return result;
                            }
                            currentIndex++;
                        }
                        else
                        {
                            result.ErrorMessage = string.Format(null, FormatMessages.ErrorFilenameNoPrefixFormat, config.Prefix);
                            return result;
                        }
                    }
                    else
                    {
                        result.Prefix = config.Prefix;
                        var numberString = part[config.Prefix.Length..];
                        if (!int.TryParse(numberString, out var number))
                        {
                            result.ErrorMessage = string.Format(null, FormatMessages.ErrorInvalidNumberFormatMsg, numberString);
                            return result;
                        }
                        result.Number = number;
                        currentIndex++;
                    }
                }
                else
                {
                    if (!int.TryParse(part, out var number))
                    {
                        result.ErrorMessage = string.Format(null, FormatMessages.ErrorInvalidNumberFormatMsg, part);
                        result.IsValid = false;
                        return result;
                    }
                    result.Number = number;
                    currentIndex++;
                }

                if (result.Title.Length == 0)
                {
                    part = parts[currentIndex];
                    result.Title = part;
                    currentIndex++;
                }

                if (currentIndex < parts.Length && config.LenVersion > 0)
                {
                    part = parts[currentIndex];
                    if (part.StartsWith('V'))
                    {
                        var versionString = part[1..];

                        var revisionIndex = versionString.IndexOf('R', ordinalIgnoreCase);
                        if (revisionIndex > 0)
                        {
                            var versionPart = versionString[..revisionIndex];
                            if (int.TryParse(versionPart, out var version))
                            {
                                result.Version = version;
                            }
                            else
                            {
                                result.ErrorMessage = string.Format(null, FormatMessages.ErrorInvalidVersionFormatMsg, versionPart);
                                return result;
                            }

                            if (config.LenRevision > 0)
                            {
                                var revisionPart = versionString[(revisionIndex + 1)..];
                                if (int.TryParse(revisionPart, out var revision))
                                {
                                    result.Revision = revision;
                                }
                                else
                                {
                                    result.ErrorMessage = string.Format(null, FormatMessages.ErrorInvalidRevisionFormatMsg, revisionPart);
                                    return result;
                                }
                            }
                        }
                        else
                        {
                            if (int.TryParse(versionString, out var version))
                            {
                                result.Version = version;
                            }
                            else
                            {
                                result.ErrorMessage = string.Format(null, FormatMessages.ErrorInvalidVersionFormatMsg, versionString);
                                return result;
                            }
                        }
                        currentIndex++;
                    }
                }

                if (currentIndex < parts.Length && config.LenScope > 0)
                {
                    part = parts[currentIndex];
                    var matchingScope = config.GetScopes()
                        .FirstOrDefault(s => s.StartsWith(part, ordinalIgnoreCase));

                    if (matchingScope == null)
                    {
                        result.ErrorMessage = string.Format(null, FormatMessages.ErrorInvalidScopeFormatMsg, part);
                        return result;
                    }
                    else
                    {
                        result.Scope = matchingScope;
                        currentIndex++;
                        if (currentIndex < parts.Length && !config.GetSkipDomains().Contains(matchingScope))
                        {
                            var domainPart = parts[currentIndex];
                            result.Domain = domainPart;
                            currentIndex++;
                        }
                    }
                }

                if (currentIndex < parts.Length)
                {
                    part = parts[currentIndex];
                    if (part.StartsWith("SUP", ordinalIgnoreCase))
                    {
                        if (part.Length <= 3)
                        {
                            result.ErrorMessage = string.Format(null, FormatMessages.ErrorInvalidSupersededNumberFormatMsg, string.Empty);
                            return result;
                        }

                        var numberString = part[3..];
                        if (!int.TryParse(numberString, out var number))
                        {
                            result.ErrorMessage = string.Format(null, FormatMessages.ErrorInvalidSupersededNumberFormatMsg, numberString);
                            return result;
                        }

                        result.SupersededValue = number;
                        currentIndex++;
                    }
                    else
                    {
                        result.ErrorMessage = string.Format(null, FormatMessages.ErrorUnexpectedPartInFilenameMsg, part);
                        return result;
                    }
                }

                var (header, content) = await ParseAdrHeaderAndContentAsync(filePath, config, fileSystemService);
                result.Header = header;
                result.ContentAdr = content;
                result.IsValid = true;
            }
            catch (Exception ex)
            {
                result.IsValid = false;
                result.ErrorMessage = string.Format(null, FormatMessages.ErrorParsingFilenameMsg, ex.Message);
            }
            return result;
        }


        private static string Humanizar(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return string.Empty;

            string texto = input;

            // 1. Detectar e converter PascalCase ou camelCase
            // Insere espaço antes de letras maiúsculas (exceto no início)
            texto = RegexConvertPascalAndCamelCase().Replace(texto, " $1");

            // 2. Converter snake_case e kebab-case para espaços
            texto = texto.Replace("_", " ").Replace("-", " ");

            // 3. Normalizar múltiplos espaços
            texto = RegexSpaces().Replace(texto, " ").Trim();

            // 4. Capitalizar primeira letra
            if (texto.Length > 0)
            {
                texto = char.ToUpper(texto[0],CultureInfo.CurrentCulture) + texto[1..].ToLower(CultureInfo.CurrentCulture);
            }

            return texto;
        }

        [GeneratedRegex("(?<!^)([A-Z])")]
        private static partial Regex RegexConvertPascalAndCamelCase();

        [GeneratedRegex(@"\s+")]
        private static partial Regex RegexSpaces();
    }
}
