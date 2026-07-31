// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the AdrPlus project under MIT license
// ***************************************************************************************

namespace AdrPlus.Domain
{
    /// <summary>
    /// Enumeration of string case formats.
    /// </summary>
    internal enum CaseFormat
    {
        /// <summary>
        /// camelCase: The first letter of the first word is lowercase, and the first letter of each subsequent word is uppercase. Example: "helloWorld".
        /// </summary>
        CamelCase,

        /// <summary>
        /// PascalCase: The first letter of each word is uppercase. Example: "HelloWorld".
        /// </summary>
        PascalCase,

        /// <summary>
        /// snake_case: All letters are lowercase, and words are separated by underscores. Example: "hello_world".
        /// </summary>
        SnakeCase,

        /// <summary>
        /// kebab-case: All letters are lowercase, and words are separated by hyphens. Example: "hello-world".
        /// </summary>
        KebabCase
    }
}
