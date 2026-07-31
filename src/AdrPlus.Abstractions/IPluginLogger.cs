// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the AdrPlus project under MIT license
// ***************************************************************************************

namespace AdrPlus.Abstractions
{
    /// <summary>
    /// Host-provided logger given to a plugin via <see cref="IPluginContext"/>. Entries are unified with the
    /// host's own file log; plugin authors do not need to pass a correlation id explicitly — use
    /// <see cref="AdrEventContext.CorrelationId"/> in the message if it should be cross-referenced.
    /// </summary>
    public interface IPluginLogger
    {
        /// <summary>
        /// Logs an informational message.
        /// </summary>
        void LogInformation(string message);

        /// <summary>
        /// Logs a warning message.
        /// </summary>
        void LogWarning(string message);

        /// <summary>
        /// Logs an error message, with an optional associated exception.
        /// </summary>
        void LogError(string message, Exception? exception = null);
    }
}
