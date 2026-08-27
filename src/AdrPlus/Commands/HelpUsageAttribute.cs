// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the AdrPlus project under MIT license
// ***************************************************************************************

using System.Globalization;

namespace AdrPlus.Commands
{
    /// <summary>
    /// Attribute used to decorate argument enum fields with usage metadata and help descriptions.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    internal sealed class HelpUsageAttribute(UsageArgumments usageArgs, string resourcekey) : Attribute
    {
        private readonly string _resourcekey = resourcekey;

        /// <summary>
        /// Gets the usage category of the argument.
        /// </summary>
        public UsageArgumments Usage { get; } = usageArgs;

        /// <summary>
        /// Gets the localized description of the argument from resources.
        /// </summary>
        public string Description
        {
            get
            {
                string? description = Resources.AdrPlus.ResourceManager.GetString(_resourcekey, CultureInfo.CurrentCulture);
                return string.IsNullOrEmpty(description) ? _resourcekey : description;
            }
        }
    }
}
