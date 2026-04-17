using AdrPlus.Domain;
using AdrPlus.Infrastructure.FileSystem;
using AdrPlus.Infrastructure.Formatting;
using System.Text;

namespace AdrPlus.Core
{
    internal sealed class AdrFileParserService : IAdrFileParser
    {
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

                if (lines.Length < 10)
                {
                    result.ErrorMessage = Resources.AdrPlus.ErrMsgAdrTooShort;
                    return (result, string.Empty);
                }

                if (!lines[0].StartsWith("###### ", ordinal))
                {
                    result.ErrorMessage = Resources.AdrPlus.ErrMsgAdrHeaderDisclaimerInvalid;
                    return (result, string.Empty);
                }
                result.Disclaimer = lines[0][7..].Trim();

                if (!lines[1].StartsWith("##### ", ordinal))
                {
                    result.ErrorMessage = Resources.AdrPlus.ErrMsgAdrHeaderVersionInvalid;
                    return (result, string.Empty);
                }
                var index1 = lines[1].IndexOf(':', ordinal);
                if (index1 < 0)
                {
                    result.ErrorMessage = Resources.AdrPlus.ErrMsgAdrHeaderVersionFormatInvalid;
                    return (result, string.Empty);
                }
                var versionText = lines[1][(index1 + 1)..].Trim();
                if (versionText != "-" && int.TryParse(versionText, null, out var version))
                {
                    result.Version = version;
                }

                if (!lines[2].StartsWith("##### ", ordinal))
                {
                    result.ErrorMessage = Resources.AdrPlus.ErrMsgAdrHeaderRevisionInvalid;
                    return (result, string.Empty);
                }
                index1 = lines[2].IndexOf(':', ordinal);
                if (index1 < 0)
                {
                    result.ErrorMessage = Resources.AdrPlus.ErrMsgAdrHeaderRevisionFormatInvalid;
                    return (result, string.Empty);
                }
                if (config.LenRevision > 0)
                {
                    var revisionText = lines[2][(index1 + 1)..].Trim();
                    if (revisionText != "-" && int.TryParse(revisionText, null, out var revision))
                    {
                        result.Revision = revision;
                    }
                }

                if (!lines[3].StartsWith("##### ", ordinal))
                {
                    result.ErrorMessage = Resources.AdrPlus.ErrMsgAdrHeaderScopeInvalid;
                    return (result, string.Empty);
                }
                var scopeDomainLine = lines[3][6..].Trim();
                if (scopeDomainLine != "-")
                {
                    var scopeParts = scopeDomainLine.Split(':', StringSplitOptions.TrimEntries);
                    if (scopeParts.Length > 0)
                    {
                        result.Scope = scopeParts[0];
                    }
                    if (scopeParts.Length > 1)
                    {
                        result.Domain = scopeParts[1];
                    }
                }

                if (!lines[4].StartsWith("##### ", ordinal))
                {
                    result.ErrorMessage = Resources.AdrPlus.ErrMsgAdrHeaderStatusInvalid;
                    return (result, string.Empty);
                }

                if (!lines[5].StartsWith("- ", StringComparison.OrdinalIgnoreCase))
                {
                    result.ErrorMessage = Resources.AdrPlus.ErrMsgAdrHeaderStatusCreateLineInvalid;
                    return (result, string.Empty);
                }
                if (!lines[6].StartsWith("- ", StringComparison.OrdinalIgnoreCase))
                {
                    result.ErrorMessage = Resources.AdrPlus.ErrMsgAdrHeaderStatusUpdateLineInvalid;
                    return (result, string.Empty);
                }
                if (!lines[7].StartsWith("- ", StringComparison.OrdinalIgnoreCase))
                {
                    result.ErrorMessage = Resources.AdrPlus.ErrMsgAdrHeaderStatusChangeLineInvalid;
                    return (result, string.Empty);
                }

                var linestacreate = lines[5][2..];
                var (statusCreate, dateCreate, errorCreate) = Helper.ParseStatusLine(linestacreate, config);
                if (!string.IsNullOrEmpty(errorCreate))
                {
                    result.ErrorMessage = errorCreate;
                    return (result, string.Empty);
                }
                result.StatusCreate = statusCreate;
                result.DateCreate = dateCreate;

                var linestapdate = lines[6][2..];
                if (linestapdate != AppConstants.AdrEmptyStatusMarker)
                {
                    var (statusUpdate, dateUpdate, errorUpdate) = Helper.ParseStatusLine(linestapdate, config);
                    if (!string.IsNullOrEmpty(errorUpdate))
                    {
                        result.ErrorMessage = errorUpdate;
                        return (result, string.Empty);
                    }
                    result.StatusUpdate = statusUpdate;
                    result.DateUpdate = dateUpdate;
                }

                var linestachange = lines[7][2..];
                if (linestachange != "\\-")
                {
                    var (statusChange, dateChange, errorChange) = Helper.ParseStatusLine(linestachange, config);
                    if (!string.IsNullOrEmpty(errorChange))
                    {
                        result.ErrorMessage = errorChange;
                        return (result, string.Empty);
                    }
                    result.StatusChange = statusChange;
                    result.DateChange = dateChange;
                    if (statusChange == AdrStatus.Superseded)
                    {
                        var indexfile = linestachange.IndexOf(':', ordinal);
                        if (indexfile < 0)
                        {
                            result.ErrorMessage = Resources.AdrPlus.ErrMsgAdrStatusLineSupersedeFormatInvalid;
                            return (result, string.Empty);
                        }
                        var fileSuperSedes = linestachange[(indexfile + 1)..].Trim();
                        result.FileSuperSedes = fileSuperSedes;
                    }
                }

                if (!lines[8].StartsWith("# ", ordinal))
                {
                    result.ErrorMessage = Resources.AdrPlus.ErrMsgAdrHeaderTitleInvalid;
                    return (result, string.Empty);
                }
                result.Title = lines[8][2..].Trim();

                if (lines[9] != "---")
                {
                    result.ErrorMessage = Resources.AdrPlus.ErrMsgAdrHeaderSeparatorInvalid;
                    return (result, string.Empty);
                }

                result.IsValid = true;
            }
            catch (Exception ex)
            {
                result.IsValid = false;
                result.ErrorMessage = string.Format(null, CompositeFormat.Parse(Resources.AdrPlus.ErrMsgAdrHeaderParsingError), ex.Message);
            }
            var content = string.Join(Environment.NewLine, lines.Skip(10));
            if (lines.Length > 10)
            {
                content += Environment.NewLine;
            }
            return (result, content);
        }

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

                if (parts.Length < 2)
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
                        result.ErrorMessage = string.Format(null, FormatMessages.ErrorFilenameNoPrefixFormat, config.Prefix);
                        return result;
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

                part = parts[currentIndex];
                result.Title = part;
                currentIndex++;

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
                        if (currentIndex < parts.Length && !config.Getskipdomains().Contains(matchingScope))
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
    }
}
