// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the AdrPlus project under MIT license
// ***************************************************************************************

using AdrPlus.Domain;
using AdrPlus.Infrastructure.Formatting;
using System.Text;
using System.Text.RegularExpressions;

namespace AdrPlus.Core
{
    /// <summary>
    /// String case transformation extensions
    /// </summary>
    internal static partial class StringCaseExtensions
    {
        /// <summary>
        /// Converts a string to camelCase
        /// Example: "Hello World" -> "helloWorld"
        /// </summary>
        public static string ToCamelCase(this string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return input;

            var words = SplitIntoWords(input);
            if (words.Length == 0)
                return input;

            var result = new StringBuilder();
            result.Append(words[0].ToLowerInvariant());

            for (int i = 1; i < words.Length; i++)
            {
                result.Append(char.ToUpperInvariant(words[i][0]));
                if (words[i].Length > 1)
                    result.Append(words[i].AsSpan(1).ToString().ToLowerInvariant());
            }

            return result.ToString();
        }

        /// <summary>
        /// Converts a string to PascalCase
        /// Example: "Hello World" -> "HelloWorld"
        /// </summary>
        public static string ToPascalCase(this string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return input;

            var words = SplitIntoWords(input);
            if (words.Length == 0)
                return input;

            var result = new StringBuilder();
            foreach (var word in words)
            {
                result.Append(char.ToUpperInvariant(word[0]));
                if (word.Length > 1)
                    result.Append(word.AsSpan(1).ToString().ToLowerInvariant());
            }

            return result.ToString();
        }

        /// <summary>
        /// Converts a string to snake_case
        /// Example: "Hello World" -> "hello_world"
        /// </summary>
        public static string ToSnakeCase(this string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return input;

            var words = SplitIntoWords(input);
            if (words.Length == 0)
                return input;

            return string.Join("_", words.Select(w => w.ToLowerInvariant()));
        }

        /// <summary>
        /// Converts a string to kebab-case
        /// Example: "Hello World" -> "hello-world"
        /// </summary>
        public static string ToKebabCase(this string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return input;

            var words = SplitIntoWords(input);
            if (words.Length == 0)
                return input;

            return string.Join("-", words.Select(w => w.ToLowerInvariant()));
        }

        /// <summary>
        /// Splits a string into words considering different formats (camelCase, PascalCase, snake_case, kebab-case, spaces).
        /// </summary>
        /// <param name="input">The input string to split into words.</param>
        /// <returns>An array of words extracted from the input string.</returns>
        private static string[] SplitIntoWords(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return [];

            // Remove leading and trailing spaces
            input = input.Trim();

            // Pattern that identifies transitions between words in different formats
            // 1. Lowercase to uppercase (camelCase/PascalCase): helloWorld -> hello World
            // 2. Consecutive uppercase to lowercase (PascalCase): XMLParser -> XML Parser
            // 3. Separators like _, -, or spaces
            var words = WordSplitRegex().Split(input)
                .Where(w => !string.IsNullOrWhiteSpace(w))
                .ToArray();

            return words;
        }

        /// <summary>
        /// Converts a string to the specified case format.
        /// </summary>
        /// <param name="input">The input string to convert.</param>
        /// <param name="caseFormat">The desired case format (CamelCase, PascalCase, SnakeCase, or KebabCase).</param>
        /// <returns>The string converted to the specified format.</returns>
        /// <exception cref="NotImplementedException">Thrown when an invalid case format is specified.</exception>
        public static string ToCase(this string input, CaseFormat caseFormat)
        {
            return caseFormat switch
            {
                CaseFormat.CamelCase => input.ToCamelCase(),
                CaseFormat.PascalCase => input.ToPascalCase(),
                CaseFormat.SnakeCase => input.ToSnakeCase(),
                CaseFormat.KebabCase => input.ToKebabCase(),
                _ => throw new NotImplementedException(string.Format(null, FormatMessages.ExceptionInvalidCaseFormatMsg, caseFormat))
            };
        }

        /// <summary>
        /// Source-generated regex for splitting words with optimal performance.
        /// Pattern identifies transitions between words in different formats:
        /// - Lowercase to uppercase (camelCase/PascalCase): helloWorld -> hello World
        /// - Consecutive uppercase to lowercase (PascalCase): XMLParser -> XML Parser
        /// - Separators like _, -, or spaces
        /// </summary>
        [GeneratedRegex(@"(?<!^)(?=[A-Z][a-z])|(?<=[a-z])(?=[A-Z])|[\s_-]+", RegexOptions.Compiled)]
        private static partial Regex WordSplitRegex();
    }
}
