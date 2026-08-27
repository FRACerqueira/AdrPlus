// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the AdrPlus project under MIT license
// ***************************************************************************************
using System.Globalization;

namespace AdrPlus.Commands
{
    /// <summary>
    /// Attribute used to decorate command enum fields with command metadata.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    internal sealed class CommandAttribute(string textcommand, Type handlercommand, string resourcekey) : Attribute
    {
        private readonly string _resourcekey = resourcekey;

        /// <summary>
        /// Gets the command alias.
        /// </summary>
        public string AliasCommand { get; } = textcommand;

        /// <summary>
        /// Gets the command handler type.
        /// </summary>
        public Type HandlerCommand { get; } = handlercommand;

        /// <summary>
        /// Gets the localized description of the command from resources.
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
