using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;

namespace QuoteConversionReportAutomation
{
    /// <summary>
    /// Provides static methods for logging messages to a daily rolling log file, with user-specific directories, and archiving of old logs.
    /// </summary>
    public static class Logger
    {
        // Static field to store the base log directory.
        private static readonly string _baseLogDirectory = @"\\harlow.local\DFS\IT Department\Applications\Development 2025\QuoteConversionReportAutomation\Logs\";

        // Static field to store the current log file path.
        private static string _logFilePath;

        // Static object for thread synchronization.
        private static readonly object _lockObject = new object();

        // Static field to store the current date.
        private static DateTime _currentDate = DateTime.Today;

        // Static constructor to initialize the logFilePath and ensure the directory exists.
        static Logger()
        {
            InitializeLogFilePath();
            // Start archiving process on startup
            ArchiveOldLogs();
        }

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
        /// Initializes the log file path, creating the directory if it doesn't exist.
        /// </summary>
        private static void InitializeLogFilePath()
        {
            string userLogDirectory = GetUserLogDirectory(_baseLogDirectory);
            _currentDate = DateTime.Today;
            string dayToday = _currentDate.ToString("yyyy-MM-dd");
            _logFilePath = Path.Combine(userLogDirectory, $"{dayToday}_LogFile.log");
            CreateDirectoryIfNotExists(userLogDirectory);
        }

        /// <summary>
        /// Gets the user specific log directory.
        /// </summary>
        /// <param name="baseLogDirectory">The base directory path.</param>
        /// <returns>The user specific log directory.</returns>
        private static string GetUserLogDirectory(string baseLogDirectory)
        {
            return Path.Combine(baseLogDirectory, Environment.UserName);
        }

        /// <summary>
        /// Creates the directory if it does not exist.
        /// </summary>
        /// <param name="directoryPath">The path of the directory to create.</param>
        private static void CreateDirectoryIfNotExists(string directoryPath)
        {
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
                LogInfo($"Created directory: {directoryPath}");
            }
        }

        /// <summary>
        /// Creates the log message.
        /// </summary>
        /// <param name="level">The log level.</param>
        /// <param name="message">The message.</param>
        /// <returns>The complete log message.</returns>
        private static string CreateLogMessage(LogLevel level, string message)
        {
            return $"[{Environment.UserName}] [{DateTime.Now}] {level}: {message}";
        }

        /// <summary>
        /// Writes the log message to the file.
        /// </summary>
        /// <param name="logMessage">The message to write to the log file.</param>
        private static void WriteLogMessage(string logMessage)
        {
#if DEBUG
            Debug.WriteLine($"DEBUG LOG: {logMessage}");
#endif
            File.AppendAllText(_logFilePath, logMessage + Environment.NewLine);
        }

        /// <summary>
        /// Logs a message with the specified log level.  Handles rolling logs.
        /// </summary>
        /// <param name="level">The log level of the message.</param>
        /// <param name="message">The message to log.</param>
        public static void Log(LogLevel level, string message)
        {
            try
            {
                lock (_lockObject)
                {
                    if (DateTime.Today != _currentDate)
                        InitializeLogFilePath();

                    string logMessage = CreateLogMessage(level, message);
                    WriteLogMessage(logMessage);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error logging the message: {ex.Message}");
                // Consider more robust error handling here, such as:
                // 1. Retry a few times.
                // 2. Log to the Application Event Log.
                // 3. Use a fallback mechanism (e.g., a simple text file in the application's directory).
                // 4. Throw the exception to be handled by the caller.
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

        /// <summary>
        /// Archives log files older than 30 days, organizing them into year, month, and week folders.
        /// </summary>
        private static void ArchiveOldLogs()
        {
            try
            {
                string baseDirectory = _baseLogDirectory;
                if (!Directory.Exists(baseDirectory))
                {
                    LogWarning($"Base log directory does not exist: {baseDirectory}.  Skipping archiving.");
                    return;
                }

                string[] userDirectories = Directory.GetDirectories(baseDirectory);
                foreach (string userDirectory in userDirectories)
                {
                    ArchiveLogsInUserDirectory(userDirectory);
                }
            }
            catch (Exception ex)
            {
                LogError($"Error during log archiving: {ex.Message}");
                // Consider if you want to re-throw the exception.
                // throw;
            }
        }

        /// <summary>
        /// Archives logs within a specific user directory.
        /// </summary>
        /// <param name="userDirectory">The path to the user's log directory.</param>
        private static void ArchiveLogsInUserDirectory(string userDirectory)
        {
            try
            {
                DirectoryInfo userDirInfo = new DirectoryInfo(userDirectory);
                if (!userDirInfo.Exists)
                    return;

                DateTime cutoffDate = DateTime.Now.AddDays(-30);

                foreach (FileInfo file in userDirInfo.GetFiles())
                {
                    if (file.LastWriteTime < cutoffDate)
                    {
                        ArchiveLogFile(file, userDirectory);
                    }
                }
            }
            catch (Exception ex)
            {
                LogError($"Error archiving logs in {userDirectory}: {ex.Message}");
            }
        }

        /// <summary>
        /// Archives a single log file into year, month, and week folders.
        /// </summary>
        /// <param name="fileToArchive">The file to archive.</param>
        /// <param name="baseDirectory">The base directory for archiving (user directory).</param>
        private static void ArchiveLogFile(FileInfo fileToArchive, string baseDirectory)
        {
            try
            {
                DateTime fileCreationTime = fileToArchive.LastWriteTime;
                string year = fileCreationTime.ToString("yyyy");
                string month = fileCreationTime.ToString("MM");
                int weekOfMonth = GetWeekOfMonth(fileCreationTime);

                string archiveDirectory = Path.Combine(baseDirectory, "Archive", year, month, $"Week{weekOfMonth}");
                CreateDirectoryIfNotExists(archiveDirectory);

                string archiveFilePath = Path.Combine(archiveDirectory, fileToArchive.Name);
                File.Move(fileToArchive.FullName, archiveFilePath);
                LogInfo($"Archived log file: {fileToArchive.Name} to {archiveFilePath}");
            }
            catch (Exception ex)
            {
                LogError($"Error archiving file {fileToArchive.Name}: {ex.Message}");
                // Consider if you want to re-throw the exception.
                // throw;
            }
        }

        /// <summary>
        /// Calculates the week of the month for a given date, using Monday as the first day of the week.
        /// </summary>
        /// <param name="date">The date for which to calculate the week of the month.</param>
        /// <returns>The week number of the month (1-5).</returns>
        private static int GetWeekOfMonth(DateTime date)
        {
            CultureInfo culture = CultureInfo.CurrentCulture;
            DayOfWeek firstDayOfWeek = DayOfWeek.Monday;

            DateTime firstDayOfMonth = new DateTime(date.Year, date.Month, 1);
            int firstDayOfMonthDayOfWeek = (int)firstDayOfMonth.DayOfWeek;

            int dayOfWeekAdjustment = (int)firstDayOfWeek - (int)culture.DateTimeFormat.FirstDayOfWeek;
            if (dayOfWeekAdjustment < 0)
            {
                dayOfWeekAdjustment += 7;
            }
            firstDayOfMonthDayOfWeek = (firstDayOfMonthDayOfWeek + dayOfWeekAdjustment) % 7;

            int dayOfMonth = date.Day;
            int weekOfMonth = (dayOfMonth + firstDayOfMonthDayOfWeek - 1) / 7 + 1;

            // Ensure weekInMonth does not exceed 5.
            int daysInMonth = DateTime.DaysInMonth(date.Year, date.Month);
            DateTime lastDayOfMonth = new DateTime(date.Year, date.Month, daysInMonth);
            int lastDayOfMonthDayOfWeek = (int)lastDayOfMonth.DayOfWeek;

            //Adjust last day of week to monday start.
            lastDayOfMonthDayOfWeek = (lastDayOfMonthDayOfWeek + dayOfWeekAdjustment) % 7;

            if ((daysInMonth + firstDayOfMonthDayOfWeek - 1) / 7 + 1 > 5)
            {
                if (weekOfMonth > 5)
                {
                    weekOfMonth = 5;
                }
            }
            return weekOfMonth;
        }
    }
}
