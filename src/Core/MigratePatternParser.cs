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
    internal static partial class MigratePatternParser
    {
        public static string CreateMigratePattern(ConfigMigration configMigration)
        {
            if (configMigration.LenNumber == 0 || configMigration.Title == 0)
            {
                return string.Empty;
            }
            var result = new StringBuilder();
            result.Append(null, $"N{configMigration.Number:D2}:{configMigration.LenNumber:D2}");
            if (configMigration.LenVersion > 0)
            {
                result.Append(null, $"V{configMigration.Version:D2}:{configMigration.LenVersion:D2}");
            }
            if (configMigration.LenRevision > 0)
            {
                result.Append(null, $"R{configMigration.Revision:D2}:{configMigration.LenRevision:D2}");
            }
            if (configMigration.LenPrefix > 0)
            {
                result.Append(null, $"P{configMigration.Prefix:D2}:{configMigration.LenPrefix:D2}");
            }
            result.Append(null, $"T{configMigration.Title:D2}");
            return result.ToString();
        }

        /// <summary>
        /// Parses the pattern text "N99:99V99:99R99:99:P99:99T99" where:
        /// - N=Number, V=Version, R=Revision, P=Prefix, T=Title
        /// - N and T are required with position (and length for N)
        /// - V, R, P are optional and may not have position and length
        /// - Position and Length must have exactly 2 digits when present
        /// </summary>
        /// <param name="pattern">The pattern text to parse</param>
        /// <returns>A dictionary with element names and their position/length, or null if pattern is invalid</returns>
        public static Dictionary<string, (int Position, int Length)>? ParsePattern(string pattern)
        {
            if (string.IsNullOrWhiteSpace(pattern))
            {
                return null;
            }

            // Pattern: N99:99[V[99:99]][R[99:99]]:[P[99:99]]T99
            // V, R, P can be present with or without position:length
            var regex = ExpMigratePattern();

            var match = regex.Match(pattern);
            if (!match.Success)
            {
                return null;
            }

            try
            {
                var result = new Dictionary<string, (int Position, int Length)>();

                // N is mandatory with position:length
                if (match.Groups[1].Value.StartsWith('N'))
                {
                    result["N"] = (int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture), 
                                   int.Parse(match.Groups[3].Value, CultureInfo.InvariantCulture));
                }
                else
                {
                    return null;
                }

                // V is optional with position:length
                if (match.Groups[4].Success && match.Groups[4].Value.StartsWith('V'))
                {
                    result["V"] = (int.Parse(match.Groups[5].Value, CultureInfo.InvariantCulture), 
                                   int.Parse(match.Groups[6].Value, CultureInfo.InvariantCulture));
                }

                // R is optional with position:length
                if (match.Groups[7].Success && match.Groups[7].Value.StartsWith('R'))
                {
                    result["R"] = (int.Parse(match.Groups[8].Value, CultureInfo.InvariantCulture), 
                                   int.Parse(match.Groups[9].Value, CultureInfo.InvariantCulture));
                }

                // P is optional with position:length
                if (match.Groups[11].Success && match.Groups[10].Value.StartsWith('P'))
                {
                    result["P"] = (int.Parse(match.Groups[11].Value, CultureInfo.InvariantCulture), 
                                   int.Parse(match.Groups[12].Value, CultureInfo.InvariantCulture));
                }

                // T is mandatory with position
                if (match.Groups[13].Value.StartsWith('T'))
                {
                    result["T"] = (int.Parse(match.Groups[14].Value, CultureInfo.InvariantCulture), 0);
                }
                else
                {
                    return null;
                }

                return result;
            }
            catch
            {
                return null;
            }
        }

        [GeneratedRegex(@"^(N)(\d{2}):(\d{2})?(V(\d{2}):(\d{2}))?(R(\d{2}):(\d{2}))?(P(\d{2}):(\d{2}))(T)(\d{2})$",RegexOptions.CultureInvariant)]
        private static partial Regex ExpMigratePattern();
    }
}
