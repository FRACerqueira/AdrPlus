using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace AdrPlus.Core
{
    public static partial class PatternParser
    {
        /// <summary>
        /// Parses the pattern text "N99:99V99:99R99:99:S99:99T99" where:
        /// - N=Number, V=Version, R=Revision, S=Scope, T=Title
        /// - N and T are required with position (and length for N)
        /// - V, R, S are optional and may not have position and length
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

            // Pattern: N99:99[V[99:99]][R[99:99]]:[S[99:99]]T99
            // V, R, S can be present with or without position:length
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
                if (match.Groups[1].Value == "N")
                {
                    result["N"] = (int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture), 
                                   int.Parse(match.Groups[3].Value, CultureInfo.InvariantCulture));
                }
                else
                {
                    return null;
                }

                // V is optional with position:length
                if (match.Groups[4].Success && match.Groups[4].Value == "V" && match.Groups[5].Success)
                {
                    result["V"] = (int.Parse(match.Groups[5].Value, CultureInfo.InvariantCulture), 
                                   int.Parse(match.Groups[6].Value, CultureInfo.InvariantCulture));
                }

                // R is optional with position:length
                if (match.Groups[7].Success && match.Groups[7].Value == "R" && match.Groups[8].Success)
                {
                    result["R"] = (int.Parse(match.Groups[8].Value, CultureInfo.InvariantCulture), 
                                   int.Parse(match.Groups[9].Value, CultureInfo.InvariantCulture));
                }

                // S is optional with position:length
                if (match.Groups[11].Success && match.Groups[11].Value == "S" && match.Groups[12].Success)
                {
                    result["S"] = (int.Parse(match.Groups[12].Value, CultureInfo.InvariantCulture), 
                                   int.Parse(match.Groups[13].Value, CultureInfo.InvariantCulture));
                }

                // T is mandatory with position
                if (match.Groups[14].Value == "T")
                {
                    result["T"] = (int.Parse(match.Groups[15].Value, CultureInfo.InvariantCulture), 0);
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

        [GeneratedRegex(@"^(N)(\d{2}):(\d{2})(V(\d{2}):(\d{2}))?(R(\d{2}):(\d{2}))?:(S(\d{2}):(\d{2}))?(T)(\d{2})$")]
        private static partial Regex ExpMigratePattern();
    }
}
