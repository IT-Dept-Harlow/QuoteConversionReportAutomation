using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace QuoteConversionReportAutomation
{
    /// <summary>
    /// Enumerates the different levels of logging.
    /// </summary>
    public enum LogLevel
    {
        /// <summary>
        /// Debug level logging, used for detailed development information.
        /// </summary>
        Debug,

        /// <summary>
        /// Informational level logging, used for general application information.
        /// </summary>
        Info,

        /// <summary>
        /// Warning level logging, used for potential issues or non-critical errors.
        /// </summary>
        Warning,

        /// <summary>
        /// Error level logging, used for critical errors that may impact application functionality.
        /// </summary>
        Error
    }

    /// <summary>
    /// Provides static methods for logging messages to a daily rolling log file, with user-specific directories.
    /// </summary>
    public static class Logger
    {
        // Static field to store the base log directory.
        private static readonly string baseLogDirectory = @"\\harlow.local\DFS\IT Department\Applications\Development 2025\QuoteConversionReportAutomation\Logs\";

        // Static field to store the current log file path.
        private static string logFilePath;

        // Static object for thread synchronization.
        private static readonly object lockObject = new object();

        // Static field to store the current date.
        private static DateTime currentDate = DateTime.Today;

        /// <summary>
        /// Static constructor to initialize the logFilePath and ensure the directory exists.
        /// </summary>
        static Logger()
        {
            InitializeLogFilePath();
        }

        /// <summary>
        /// Initializes the log file path, creating the directory if it doesn't exist.
        /// </summary>
        private static void InitializeLogFilePath()
        {
            // Get the current user's name to create a user-specific directory.
            string userName = Environment.UserName;

            // Get the current date.
            currentDate = DateTime.Today;

            // Format the date as "yyyy-MM-dd" for consistent file naming.
            string dayToday = currentDate.ToString("yyyy-MM-dd");

            // Construct the user-specific log directory.
            string userLogDirectory = Path.Combine(baseLogDirectory, userName);

            // Ensure the user-specific log directory exists. If not, create it.
            if (!Directory.Exists(userLogDirectory))
            {
                Directory.CreateDirectory(userLogDirectory);
            }

            // Combine the directory and file name to create the full log file path.
            logFilePath = Path.Combine(userLogDirectory, dayToday + "_LogFile.log");
        }

        /// <summary>
        /// Logs a message with the specified log level.  Handles rolling logs.
        /// </summary>
        /// <param name="level">The log level of the message.</param>
        /// <param name="message">The message to log.</param>
        public static void Log(LogLevel level, string message)
        {
            try
            {
                // Use a lock to ensure thread safety, especially when checking and creating new log files.
                lock (lockObject)
                {
                    // Check if the date has changed, indicating a new day.
                    if (DateTime.Today != currentDate)
                    {
                        InitializeLogFilePath(); // Initialize the log file path for the new day.
                    }

                    // Construct the log message with username, timestamp, level, and message.
                    string logMessage = $"[{Environment.UserName}] [{DateTime.Now}] {level}: {message}";

#if DEBUG       // Conditional compilation for debug builds
                    Debug.WriteLine($"DEBUG LOG: {logMessage}"); // Output to the debug console
#endif
                    // Append the log message to the log file, adding a new line.
                    File.AppendAllText(logFilePath, logMessage + Environment.NewLine);
                }
            }
            catch (Exception ex)
            {
                // If an error occurs during logging, print the formatted error message to the console.
                Console.WriteLine($"Error logging the message: {ex.Message}");
            }
        }

        /// <summary>
        /// Logs an error message.
        /// </summary>
        /// <param name="message">The error message to log.</param>
        public static void LogError(string message) => Log(LogLevel.Error, message);

        /// <summary>
        /// Logs an informational message.
        /// </summary>
        /// <param name="message">The informational message to log.</param>
        public static void LogInfo(string message) => Log(LogLevel.Info, message);

        /// <summary>
        /// Logs a debug message.
        /// </summary>
        /// <param name="message">The debug message to log.</param>
        public static void LogDebug(string message) => Log(LogLevel.Debug, message);

        /// <summary>
        /// Logs a warning message.
        /// </summary>
        /// <param name="message">The warning message to log.</param>
        public static void LogWarning(string message) => Log(LogLevel.Warning, message);
    }
}
