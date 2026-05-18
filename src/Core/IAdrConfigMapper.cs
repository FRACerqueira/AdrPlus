// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the AdrPlus project under MIT license
// ***************************************************************************************

using AdrPlus.Domain;

namespace AdrPlus.Core
{
    internal interface IAdrConfigMapper
    {
        /// <summary>
        /// Converts a JSON string to an <see cref="AdrPlusRepoConfig"/> object.
        /// </summary>
        /// <param name="jsonString">The JSON string to deserialize.</param>
        /// <param name="template">The template name to associate with the configuration.</param>
        /// <returns>An <see cref="AdrPlusRepoConfig"/> object created from the JSON data.</returns>
        AdrPlusRepoConfig FromJson(string jsonString, string template);
    }
}
