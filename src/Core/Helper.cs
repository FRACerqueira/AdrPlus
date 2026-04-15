// MIT LICENCE
// The maintenance and evolution is maintained by the AdrPlus project under MIT license
// ***************************************************************************************

using AdrPlus.Domain;
using System.Globalization;
using System.Text;
using System.Text.Json;

namespace AdrPlus.Core
{
    internal static class Helper
    {
        #region Validation

        /// <summary>
        /// Validates whether a culture name is recognized by the runtime.
        /// Uses <c>throwOnError: true</c> via <see cref="CultureInfo.GetCultureInfo(string, bool)"/> to detect invalid names.
        /// </summary>
        /// <param name="cultureName">The IETF culture name to validate (e.g. "en-US"). Null or whitespace returns <see langword="false"/>.</param>
        /// <returns><see langword="true"/> if the culture name is valid; otherwise <see langword="false"/>.</returns>
        public static bool IsValidCultureName(string? cultureName)
        {
            if (string.IsNullOrWhiteSpace(cultureName))
            {
                return false;
            }
            try
            {
                CultureInfo.GetCultureInfo(cultureName, true);
                return true;
            }
            catch (CultureNotFoundException)
            {
                return false;
            }
        }

        #endregion

        #region ADR Record

        /// <summary>
        /// Creates an <see cref="AdrRecord"/> from parsed file-name components and their associated header.
        /// The <c>Revision</c> field is populated only when <see cref="AdrPlusRepoConfig.LenRevision"/> is greater than zero.
        /// </summary>
        /// <param name="parsefile">The parsed filename components, including the header and body content.</param>
        /// <param name="config">The repository configuration used to determine whether revision tracking is active.</param>
        /// <returns>A new <see cref="AdrRecord"/> populated from <paramref name="parsefile"/> and <paramref name="config"/>.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="parsefile"/> or <paramref name="config"/> is <see langword="null"/>.</exception>
        public static AdrRecord CreateAdrRecord(AdrFileNameComponents parsefile, AdrPlusRepoConfig config)
        {
            ArgumentNullException.ThrowIfNull(parsefile);
            ArgumentNullException.ThrowIfNull(config);
            return new AdrRecord
            {
                Number = parsefile.Number,
                Title = parsefile.Header.Title,
                Scope = parsefile.Header.Scope ?? string.Empty,
                Domain = parsefile.Header.Domain ?? string.Empty,
                StatusCreate = parsefile.Header.StatusCreate,
                CreateRef = parsefile.Header.DateCreate,
                StatusUpdate = parsefile.Header.StatusUpdate,
                UpdateRef = parsefile.Header.DateUpdate,
                StatusChange = parsefile.Header.StatusChange,
                ChangeRef = parsefile.Header.DateChange,
                Version = parsefile.Header.Version,
                Revision = config.LenRevision == 0 ? null : parsefile.Header.Revision,
                Template = parsefile.ContentAdr!
            };
        }

        #endregion

        #region Parsing

        /// <summary>
        /// Parses a status line extracted from an ADR header.
        /// Expected format: <c>&lt;StatusText&gt; (dd/MM/yyyy)</c>, or <c>\-</c> for an absent/empty status.
        /// The status text is matched case-insensitively against <see cref="AdrPlusRepoConfig.StatusMapping"/>.
        /// </summary>
        /// <param name="statusLine">The raw status line content (without the leading "- ").</param>
        /// <param name="config">The repository configuration providing the status-to-string mapping and date format.</param>
        /// <returns>
        /// A tuple of (<see cref="AdrStatus"/> status, <see cref="DateTime"/>? date, <see langword="string"/> error).
        /// <c>error</c> is non-empty when parsing fails; in that case <c>status</c> is <see cref="AdrStatus.Unknown"/> and <c>date</c> is <see langword="null"/>.
        /// </returns>
        public static (AdrStatus status, DateTime? date, string error) ParseStatusLine(string statusLine, AdrPlusRepoConfig config)
        {
            var index2 = statusLine.IndexOf('(', StringComparison.Ordinal);
            var index3 = statusLine.IndexOf(')', StringComparison.Ordinal);

            if (index2 < 0 || index3 < 0)
            {
                return (AdrStatus.Unknown, null, Resources.AdrPlus.ErrMsgAdrStatusLineFormatInvalid);
            }

            if (index3 < index2)
            {
                return (AdrStatus.Unknown, null, Resources.AdrPlus.ErrMsgAdrStatusLineParenthesesInvalid);
            }

            var textStatus = statusLine[..index2].Trim();
            if (!config.StatusMapping.Values.Contains(textStatus, StringComparer.OrdinalIgnoreCase))
            {
                return (AdrStatus.Unknown, null, string.Format(null, CompositeFormat.Parse(Resources.AdrPlus.ErrMsgAdrStatusUnknown), textStatus));
            }

            var status = config.StatusMapping.First(x => x.Value.Equals(textStatus, StringComparison.OrdinalIgnoreCase)).Key;

            var dateString = statusLine.Substring(index2 + 1, index3 - index2 - 1).Trim();
            if (!DateTime.TryParse(dateString, null, out var date))
            {
                return (AdrStatus.Unknown, null, string.Format(null, CompositeFormat.Parse(Resources.AdrPlus.ErrMsgAdrStatusDateFormatInvalid), dateString));
            }

            return (status, date, string.Empty);
        }

        #endregion

        #region JSON

        /// <summary>
        /// Tries to retrieve a JSON property using a case-insensitive name lookup.
        /// First performs an exact-match lookup for performance; falls back to a full enumeration when not found.
        /// </summary>
        /// <param name="element">The <see cref="JsonElement"/> to search within.</param>
        /// <param name="propertyName">The property name to find (matched case-insensitively).</param>
        /// <param name="value">When this method returns <see langword="true"/>, contains the matched <see cref="JsonElement"/>; otherwise the default value.</param>
        /// <returns><see langword="true"/> if a property with the specified name was found; otherwise <see langword="false"/>.</returns>
        public static bool TryGetPropertyCaseInsensitive(JsonElement element, string propertyName, out JsonElement value)
        {
            // First try exact match for performance
            if (element.TryGetProperty(propertyName, out value))
                return true;

            // Then try case-insensitive search
            foreach (var property in element.EnumerateObject())
            {
                if (string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase))
                {
                    value = property.Value;
                    return true;
                }
            }

            value = default;
            return false;
        }

        #endregion

        #region Process

        /// <summary>
        /// Opens a file using the platform-appropriate shell command.
        /// On Windows uses <c>cmd.exe /c</c>; on Linux/macOS uses <c>sh -c</c>;
        /// on other platforms falls back to <see cref="System.Diagnostics.ProcessStartInfo.UseShellExecute"/>.
        /// </summary>
        /// <param name="filepath">The full path to the file to open.</param>
        /// <param name="command">The shell command string used to open the file (may contain <c>{0}</c> placeholder substituted externally).</param>
        /// <returns>An empty string on success; otherwise the stderr output or the exception message.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="filepath"/> or <paramref name="command"/> is <see langword="null"/>.</exception>
        public static string OpenFile(string filepath, string command)
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

        #endregion
    }
}
