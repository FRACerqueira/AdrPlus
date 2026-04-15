// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the AdrPlus project under MIT license
// ***************************************************************************************

using Microsoft.Extensions.Logging;

namespace AdrPlus.Infrastructure.Logging
{
    /// <summary>
    /// Centralized logging messages using compile-time LoggerMessage source generation.
    /// Provides high-performance structured logging across all command handlers.
    /// </summary>
    internal static partial class LogMessages
    {
        [LoggerMessage(
            Level = LogLevel.Information, 
            Message = "Starting {NameApp} Version {Version} Culture {Culture}")]
        public static partial void LogApplicationStarting(ILogger logger, string nameApp, string version, string culture);

        [LoggerMessage(
            Level = LogLevel.Error, 
            Message = "{ErrorMessage}")]
        public static partial void LogError(ILogger logger, string errorMessage);

        [LoggerMessage(
            Level = LogLevel.Information,
            Message = "{ErrorMessage}")]
        public static partial void LogInfo(ILogger logger, string errorMessage);

        [LoggerMessage(
            Level = LogLevel.Critical, 
            Message = "Critical error occurred")]
        public static partial void LogCriticalError(ILogger logger, Exception exception);

        [LoggerMessage(
            Level = LogLevel.Information,
            Message = "Stopped AdrPlus")]
        public static partial void LogStoppedAdrPlus(ILogger logger);

        [LoggerMessage(
            Level = LogLevel.Warning, 
            Message = "Unknown command: {CommandName}")]
        public static partial void LogUnknownCommand(ILogger logger, string commandName);

        [LoggerMessage(
            Level = LogLevel.Information, 
            Message = "Executing command: {CommandName}")]
        public static partial void LogExecutingCommand(ILogger logger, string commandName);

        [LoggerMessage(
            Level = LogLevel.Information, 
            Message = "Command completed: {CommandName}")]
        public static partial void LogCommandCompleted(ILogger logger, string commandName);

        [LoggerMessage(
            Level = LogLevel.Error,
            Message = "Command Exception")]
        public static partial void LogCommandException(ILogger logger, Exception ex);

        [LoggerMessage(
            Level = LogLevel.Error,
            Message = "Command Failure: {error}")]
        public static partial void LogCommandFailure(ILogger logger, string error);

        [LoggerMessage(
            Level = LogLevel.Information,
            Message = "Command Successful: {message}")]
        public static partial void LogCommandSuccessful(ILogger logger, string message);


        [LoggerMessage(
            Level = LogLevel.Error,
            Message = "Error calculating number: {message}")]
        public static partial void LogErrorCalculatingNextNumber(ILogger logger, string message);

        [LoggerMessage(
            Level = LogLevel.Information,
            Message = "Initializing ADR repository at: {path}")]
        public static partial void LogInitializingRepository(ILogger logger, string path);

        [LoggerMessage(
            Level = LogLevel.Information,
            Message = "Config file already exists at: {path}")]
        public static partial void LogConfigFileAlreadyExists(ILogger logger, string path);

        [LoggerMessage(
            Level = LogLevel.Error,
            Message = "Invalid repository configuration: {errors}")]
        public static partial void LogInvalidRepoConfiguration(ILogger logger, string errors);

        [LoggerMessage(
            Level = LogLevel.Error,
            Message = "Error format date for culture: {culture}")]
        public static partial void LogErrorFormatDateForCulture(ILogger logger, string culture);

    }
}
