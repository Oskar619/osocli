using System.Diagnostics;

namespace osocli
{
    public interface ILogger
    {
        /// <summary>
        /// Logs a message using the logger.
        /// </summary>
        /// <param name="serializedMessage">The serialized message to log.</param>
        void Log(TraceEventType type, string msg, int logId = 0);

        /// <summary>
        /// Logs a message flagged as Info.
        /// </summary>
        /// <param name="message">The message to log.</param>
        void LogInfo(string message);

        /// <summary>
        /// Logs a message flagged as Error.
        /// </summary>
        /// <param name="message">The message to log.</param>
        void LogError(string message);

        /// <summary>
        /// Logs a message flagged as Verbose.
        /// </summary>
        /// <param name="message">The message to log.</param>
        void LogVerbose(string message);

        /// <summary>
        /// Logs a message flagged as Warning.
        /// </summary>
        /// <param name="message">The message to log.</param>
        void LogWarning(string message);

        /// <summary>
        /// Logs a message flagged as Start.
        /// </summary>
        /// <param name="message">The message to log.</param>
        void LogStart(string message);

        /// <summary>
        /// Logs a message flagged as Critical.
        /// </summary>
        /// <param name="message">The message to log.</param>
        void LogCritical(string message);
    }
}
