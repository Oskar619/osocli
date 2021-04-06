// <copyright file="Logger.cs" company="Microsoft Corporation">
// Copyright (C) Microsoft Corporation. All rights reserved.
// </copyright>

namespace osocli
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;

    /// <summary>
    /// Intellicode CLI Logger.
    /// </summary>
    public class Logger : ILogger, IDisposable
    {
        /// <summary>
        /// Console Listener name.
        /// </summary>
        protected const string ConsoleListener = nameof(ConsoleListener);

        /// <summary>
        /// File Listener name.
        /// </summary>
        protected const string FileListener = nameof(FileListener);

        private readonly string[] validVerbosities = new string[] { "q", "quiet", "m", "minimal", "n", "normal", "d", "detailed", "diag", "diagnostic" };
        private Lazy<TraceSource> loggerSource;
        private StringWriter stringWriter;

        /// <summary>
        /// Initializes a new instance of the <see cref="Logger"/> class.
        /// </summary>
        /// <param name="commandOptions">The injected command options of the operation.</param>
        public Logger(ILoggingOptions options)
        {
            loggerSource = new Lazy<TraceSource>(() =>
            {
                return InitTraceSource();
            });
        }

        /// <summary>
        /// Gets The TraceSource for the logger.
        /// </summary>
        protected TraceSource Source => loggerSource.Value;

        private ILoggingOptions Options { get; set; }

        /// <summary>
        /// Logs a message using the logger.
        /// </summary>
        /// <param name="serializedMessage">The serialized message to log.</param>
        public void Log(TraceEventType type, string msg, int logId = 0)
        {
            loggerSource.Value.TraceData(type, logId, msg);
        }

        /// <summary>
        /// Logs a message flagged as Info.
        /// </summary>
        /// <param name="message">The message to log.</param>
        public void LogInfo(string message)
        {
            Log(TraceEventType.Information, message);
        }

        /// <summary>
        /// Logs a message flagged as Error.
        /// </summary>
        /// <param name="message">The message to log.</param>
        public void LogError(string message)
        {
            Log(TraceEventType.Error, message);
        }

        /// <summary>
        /// Logs a message flagged as Verbose.
        /// </summary>
        /// <param name="message">The message to log.</param>
        public void LogVerbose(string message)
        {
            Log(TraceEventType.Verbose, message);
        }

        /// <summary>
        /// Logs a message flagged as Warning.
        /// </summary>
        /// <param name="message">The message to log.</param>
        public void LogWarning(string message)
        {
            Log(TraceEventType.Warning, message);
        }

        /// <summary>
        /// Logs a message flagged as Start.
        /// </summary>
        /// <param name="message">The message to log.</param>
        public void LogStart(string message)
        {
            Log(TraceEventType.Start, message);
        }

        /// <summary>
        /// Logs a message flagged as Critical.
        /// </summary>
        /// <param name="message">The message to log.</param>
        public void LogCritical(string message)
        {
            Log(TraceEventType.Critical, message);
        }

        /// <summary>
        /// Dumps the log string writer into the log output file.
        /// </summary>
        public void Dispose()
        {
            if (this.stringWriter != null)
            {
                this.WriteFile(stringWriter);
                stringWriter.Dispose();
            }
        }

        /// <summary>
        /// Sets the level of verbosity on the logger.
        /// </summary>
        /// <param name="verbosity">The level of verbosity.</param>
        /// <returns>Returns a level given a verbosity.</returns>
        private EventTypeFilter GetLevel(string verbosity = null)
        {
            // Take predefined verbosity by default.
            if (string.IsNullOrWhiteSpace(verbosity))
            {
                verbosity = this.Options.Verbosity;
            }

            // filter possible verbosity options
            if (!validVerbosities.Contains(verbosity))
            {
                LogError($"Error. Invalid value {verbosity} for verbosity. Please see train --help.");
                throw new ArgumentException($"Invalid argument! Got value {verbosity} for verbosity");
            }

            switch (verbosity)
            {
                case "q":
                case "quiet":
                    return new EventTypeFilter(SourceLevels.Critical);
                case "m":
                case "minimal":
                    return new EventTypeFilter(SourceLevels.Error);
                case "n":
                case "normal":
                    return new EventTypeFilter(SourceLevels.Information);
                case "d":
                case "detailed":
                    return new EventTypeFilter(SourceLevels.Verbose);
                case "diag":
                case "diagnostic":
                    return new EventTypeFilter(SourceLevels.All);
                default:
                    return new EventTypeFilter(SourceLevels.Off);
            }
        }

        /// <summary>
        /// Creates a log file listener stream.
        /// </summary>
        /// <param name="logFilePath">The log file to be used.</param>
        /// <param name="writer">The string writer that contains all the log data.</param>
        private void WriteToFile(string logFilePath, StringWriter writer)
        {
            if (!Directory.Exists(Path.GetDirectoryName(logFilePath)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(logFilePath));
            }

            try
            {
                File.WriteAllText(logFilePath, writer.ToString());
            }
            catch (Exception ex)
            {
                if (ex is UnauthorizedAccessException || ex is PathTooLongException || ex is DirectoryNotFoundException || ex is IOException)
                {
                    var tempFile = Path.GetTempFileName();
                    Console.Error.WriteLine($"Error occurred trying to create file {Options.LogFile}. Using temporary log file {tempFile} instead.");
                    File.WriteAllText(tempFile, writer.ToString());
                }
                else
                {
                    throw;
                }
            }
        }

        private void WriteFile(StringWriter writer)
        {
            if (writer == null)
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(Options?.LogFile))
            {
                WriteToFile(Options.LogFile, writer);
            }
        }

        private TraceSource InitTraceSource()
        {
            var source = CreateTraceSourceLogger(GetLevel(Options.Verbosity));
            AddSourceStringWriter(source);
            return source;
        }

        private void AddSourceStringWriter(TraceSource source)
        {
            source.Listeners.Remove(FileListener);
            this.stringWriter = new StringWriter();
            var fileListener = new TextWriterTraceListener(stringWriter, FileListener);
            fileListener.Filter = new EventTypeFilter(SourceLevels.All);
            source.Listeners.Add(fileListener);
        }

        private static TraceSource CreateTraceSourceLogger(EventTypeFilter level)
        {
            Trace.AutoFlush = true;

            // Initializing Tracesource
            var mySource = new TraceSource("CLI")
            {
                Switch = new SourceSwitch("CLI-Switch")
                {
                    Level = SourceLevels.All
                }
            };

            mySource.Listeners.Remove("Default");

            // Generating Default STD output listener.
            var consoleListener = new TextWriterTraceListener(Console.Out, ConsoleListener)
            {
                Filter = level
            };

            // Adding listeners to the tracesource.
            mySource.Listeners.Add(consoleListener);

            return mySource;
        }
    }
}
