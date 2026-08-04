// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the AdrPlus project under MIT license
// ***************************************************************************************

using System.Text.Json;

namespace AdrPlus.Domain
{
    /// <summary>
    /// Represents a JSON field with metadata for editing and validation.
    /// </summary>
    internal sealed class FieldsJson
    {
        /// <summary>
        /// Gets or sets the name of the field.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the value of the field.
        /// </summary>
        public string Value { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the JSON value type of the field.
        /// </summary>
        public JsonValueKind Type { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether editing has ended for this field.
        /// </summary>
        public bool IsEndEdit { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this field is enabled for editing.
        /// </summary>
        public bool IsEnabled { get; set; }
    }
}
