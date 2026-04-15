using AdrPlus.Domain;

namespace AdrPlus.Core
{
    internal interface IAdrConfigMapper
    {
        AdrPlusRepoConfig FromJson(string jsonString, string template, string defaultFolder);
    }
}
