// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the AdrPlus project under MIT license
// ***************************************************************************************

using System.Globalization;
using System.Text;

namespace AdrPlus.Core
{
    /// <summary>
    /// String similarity extensions, used to offer non-blocking "did you mean" suggestions
    /// (e.g. for Scope/Domain values already used elsewhere in a repository) without enforcing
    /// any vocabulary or rejecting user input.
    /// </summary>
    internal static class StringSimilarityExtensions
    {
        /// <summary>
        /// Maximum common-prefix length considered for the Jaro-Winkler boost, per Winkler's original definition.
        /// </summary>
        private const int MaxPrefixLength = 4;

        /// <summary>
        /// Scaling factor applied to the common-prefix boost, per Winkler's original definition.
        /// Must stay at or below 0.25, or the resulting score could exceed 1.0.
        /// </summary>
        private const double PrefixScalingFactor = 0.1;

        /// <summary>
        /// Computes the Jaro-Winkler similarity between <paramref name="source"/> and <paramref name="target"/>,
        /// case- and diacritic-insensitively (e.g. <c>"Não"</c> and <c>"Nao"</c> compare as equal letters).
        /// Compares by Unicode codepoint, not UTF-16 code unit, so an astral-plane character (e.g. an emoji
        /// surrogate pair) is never split into two independent "characters".
        /// </summary>
        /// <param name="source">The first string to compare.</param>
        /// <param name="target">The second string to compare.</param>
        /// <returns>A similarity score from <c>0.0</c> (completely different) to <c>1.0</c> (identical).</returns>
        public static double JaroWinklerSimilarity(this string source, string target)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(target);

            var a = ToComparableRunes(source);
            var b = ToComparableRunes(target);

            var jaro = JaroSimilarity(a, b);
            if (jaro == 0)
            {
                return 0;
            }

            var maxPrefix = Math.Min(MaxPrefixLength, Math.Min(a.Length, b.Length));
            var prefixLength = 0;
            while (prefixLength < maxPrefix && a[prefixLength] == b[prefixLength])
            {
                prefixLength++;
            }

            return jaro + (prefixLength * PrefixScalingFactor * (1 - jaro));
        }

        /// <summary>
        /// Case-folds, decomposes accented letters into base letter + combining mark (Unicode NFD) and drops
        /// the combining marks, then enumerates by Unicode codepoint (<see cref="Rune"/>) rather than UTF-16
        /// code unit.
        /// </summary>
        private static Rune[] ToComparableRunes(string value)
        {
            var normalized = value.ToUpperInvariant().Normalize(NormalizationForm.FormD);
            var runes = new List<Rune>(normalized.Length);
            foreach (var rune in normalized.EnumerateRunes())
            {
                if (Rune.GetUnicodeCategory(rune) == UnicodeCategory.NonSpacingMark)
                {
                    continue;
                }
                runes.Add(rune);
            }
            return [.. runes];
        }

        private static double JaroSimilarity(Rune[] a, Rune[] b)
        {
            if (a.Length == 0 && b.Length == 0)
            {
                return 1;
            }
            if (a.Length == 0 || b.Length == 0)
            {
                return 0;
            }

            var matchDistance = Math.Max(0, (Math.Max(a.Length, b.Length) / 2) - 1);

            var aMatches = new bool[a.Length];
            var bMatches = new bool[b.Length];
            var matches = 0;

            for (var i = 0; i < a.Length; i++)
            {
                var start = Math.Max(0, i - matchDistance);
                var end = Math.Min(i + matchDistance + 1, b.Length);
                for (var j = start; j < end; j++)
                {
                    if (bMatches[j] || a[i] != b[j])
                    {
                        continue;
                    }
                    aMatches[i] = true;
                    bMatches[j] = true;
                    matches++;
                    break;
                }
            }

            if (matches == 0)
            {
                return 0;
            }

            var transpositions = 0;
            var k = 0;
            for (var i = 0; i < a.Length; i++)
            {
                if (!aMatches[i])
                {
                    continue;
                }
                while (!bMatches[k])
                {
                    k++;
                }
                if (a[i] != b[k])
                {
                    transpositions++;
                }
                k++;
            }

            var m = (double)matches;
            return ((m / a.Length) + (m / b.Length) + ((m - (transpositions / 2.0)) / m)) / 3.0;
        }
    }
}
