// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the AdrPlus project under MIT license
// ***************************************************************************************

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
        /// case-insensitively.
        /// </summary>
        /// <param name="source">The first string to compare.</param>
        /// <param name="target">The second string to compare.</param>
        /// <returns>A similarity score from <c>0.0</c> (completely different) to <c>1.0</c> (identical).</returns>
        public static double JaroWinklerSimilarity(this string source, string target)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(target);

            var a = source.ToUpperInvariant();
            var b = target.ToUpperInvariant();

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

        private static double JaroSimilarity(string a, string b)
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
