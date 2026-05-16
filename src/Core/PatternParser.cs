// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the AdrPlus project under MIT license
// ***************************************************************************************

using AdrPlus.Domain;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace AdrPlus.Core
{
    internal static partial class PatternParser
    {
        public static string CreateMigratePattern(ConfigMigration configMigration)
        {
            if (configMigration.LenNumber == 0 || configMigration.Title == 0)
            {
                return string.Empty;
            }
            var result = new StringBuilder();
            result.Append(CultureInfo.InvariantCulture, $"N{configMigration.Number:D2}:{configMigration.LenNumber:D2}");
            result.Append(CultureInfo.InvariantCulture, $"T{configMigration.Title:D2}");
            if (configMigration.LenVersion > 0)
            {
                result.Append(CultureInfo.InvariantCulture, $"V{configMigration.Version:D2}:{configMigration.LenVersion:D2}");
            }
            if (configMigration.LenRevision > 0)
            {
                result.Append(CultureInfo.InvariantCulture, $"R{configMigration.Revision:D2}:{configMigration.LenRevision:D2}");
            }
            if (configMigration.LenPrefix > 0)
            {
                result.Append(CultureInfo.InvariantCulture, $"P{configMigration.Prefix:D2}:{configMigration.LenPrefix:D2}");
            }
            return result.ToString();
        }

        /// <summary>
        /// Parses an ADR pattern string into its constituent components.
        /// The expected format of the pattern is: [Prefix]{Number}V{Version}[R{Revision}][Scope]
        /// Where:
        /// - Prefix (P): optional, letters only
        /// - Number (N): mandatory, digits only
        /// - Version (V): mandatory, digits only
        /// - Revision (R): optional, digits only
        /// - Scope (S): optional, letters only
        /// </summary>
        /// <param name="pattern">The ADR pattern string to parse.</param>
        /// <returns>A dictionary containing the parsed components with keys "P", "N", "V", "R" and "S", or <see langword="null"/> if the pattern is invalid or empty.</returns>
        public static Dictionary<string, string>? ParseAdrPattern(string pattern)
        {
            if (string.IsNullOrWhiteSpace(pattern))
            {
                return null;
            }

            var match = ExpAdrPattern().Match(pattern);
            if (!match.Success)
            {
                return null;
            }

            return new Dictionary<string, string>
            {
                { "P", match.Groups[1].Value ?? string.Empty },
                { "N", match.Groups[2].Value ?? string.Empty },
                { "V", match.Groups[3].Value ?? string.Empty },
                { "R", match.Groups[4].Value ?? string.Empty },
                { "S", match.Groups[5].Value ?? string.Empty }
            };
        }



        /// <summary>
        /// Parses the pattern text "N99:99T99[V99:99][R99:99][P99:99]" where:
        /// - N=Number (mandatory with position:length)
        /// - T=Title (mandatory with position)
        /// - V=Version (optional with position:length)
        /// - R=Revision (optional with position:length)
        /// - P=Prefix (optional with position:length)
        /// - Position and Length must have exactly 2 digits when present
        /// </summary>
        /// <param name="pattern">The pattern text to parse</param>
        /// <returns>A dictionary with element names and their position/length, or null if pattern is invalid</returns>
        public static Dictionary<string, (int Position, int Length)>? ParseMigratePattern(string pattern)
        {
            if (string.IsNullOrWhiteSpace(pattern))
            {
                return null;
            }

            var regex = ExpMigratePattern();
            var match = regex.Match(pattern);
            if (!match.Success)
            {
                return null;
            }

            try
            {
                var result = new Dictionary<string, (int Position, int Length)>
                {
                    // N is mandatory with position:length (groups 2, 3)
                    ["N"] = (int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture),
                               int.Parse(match.Groups[3].Value, CultureInfo.InvariantCulture)),

                    // T is mandatory with position (group 5)
                    ["T"] = (int.Parse(match.Groups[5].Value, CultureInfo.InvariantCulture), 0)
                };

                // V is optional with position:length (groups 7, 8)
                if (match.Groups[6].Success)
                {
                    result["V"] = (int.Parse(match.Groups[7].Value, CultureInfo.InvariantCulture), 
                                   int.Parse(match.Groups[8].Value, CultureInfo.InvariantCulture));
                }

                // R is optional with position:length (groups 10, 11)
                if (match.Groups[9].Success)
                {
                    result["R"] = (int.Parse(match.Groups[10].Value, CultureInfo.InvariantCulture), 
                                   int.Parse(match.Groups[11].Value, CultureInfo.InvariantCulture));
                }

                // P is optional with position:length (groups 13, 14)
                if (match.Groups[12].Success)
                {
                    result["P"] = (int.Parse(match.Groups[13].Value, CultureInfo.InvariantCulture), 
                                   int.Parse(match.Groups[14].Value, CultureInfo.InvariantCulture));
                }

                return result;
            }
            catch
            {
                return null;
            }
        }

        [GeneratedRegex(@"^(N)(\d{2}):(\d{2})(T)(\d{2})(V(\d{2}):(\d{2}))?(R(\d{2}):(\d{2}))?(P(\d{2}):(\d{2}))?$", RegexOptions.CultureInvariant)]
        private static partial Regex ExpMigratePattern();

        [GeneratedRegex(@"^([A-Za-z]*)(\d+)V(\d+)(?:R(\d+))?([A-Za-z]*)$", RegexOptions.CultureInvariant)]
        private static partial Regex ExpAdrPattern();

    }
}
