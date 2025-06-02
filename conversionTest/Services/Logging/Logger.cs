// Logger.cs
// Provides static methods for logging messages to daily rolling log files.
// Supports user-specific log directories and automated archiving of old logs.
// Configuration for base directory, log levels, filename format, and archive days
// is read from appsettings.json using the new structured format.
// Utilises C# 10+ features.

#region Using Directives
// System related namespaces
// Third-party namespaces
using Microsoft.Extensions.Configuration; // Required for IConfiguration
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
#endregion

namespace QuoteConversionReportAutomation.Services.Logging
{
    /// <summary>
    /// Provides static methods for application-wide logging.
    /// Supports different log levels, daily log file rolling, user-specific log directories,
    /// and automatic archiving of old log files. Configuration is driven by `appsettings.json`.
    /// </summary>
    public static class Logger
    {
        #region Fields and Constants

        // --- Configuration-driven settings with defaults ---
        /// <summary>
        /// The base directory where user-specific log folders will be created.
        /// Read from "Paths:LogDirectoryBase" in appsettings.json.
        /// </summary>
        private static string s_configuredBaseLogDirectory = string.Empty;

        /// <summary>
        /// The number of days after which log files should be archived.
        /// Read from "Logging:LogArchiveOlderThanDays" in appsettings.json.
        /// </summary>
        private static int s_archiveLogsOlderThanDays = 7; // Default

        /// <summary>
        /// The format string for generating log file names. Should include a date placeholder like {0:yyyy-MM-dd}.
        /// Read from "Logging:LogFileNameFormat" in appsettings.json.
        /// </summary>
        private static string s_logFileNameFormat = "{0:yyyy-MM-dd}_LogFile.log"; // Default

        /// <summary>
        /// The minimum log level required for a message to be written to the log file.
        /// Read from "Logging:DebugBuildLogLevel" (for DEBUG builds) or "Logging:DefaultLogLevel" (for RELEASE builds).
        /// </summary>
        private static LogLevel s_minimumLogLevel = LogLevel.Info; // Default

        /// <summary>
        /// The fallback directory to use if the configured base log directory is invalid or inaccessible.
        /// Attempts to read from "Logging:DefaultFallbackLogDirectory" in appsettings.json, otherwise uses a hardcoded default.
        /// </summary>
        private static string s_effectiveFallbackDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "QCRA_Logs_Fallback", "Logs"); // Hardcoded default fallback

        // --- Constants for appsettings.json keys ---
        private const string ConfigKeyBaseLogDirectory = "Paths:LogDirectoryBase";
        private const string ConfigKeyArchiveDays = "Logging:LogArchiveOlderThanDays";
        private const string ConfigKeyLogNameFormat = "Logging:LogFileNameFormat";
        private const string ConfigKeyLogLevelRelease = "Logging:DefaultLogLevel";
        private const string ConfigKeyLogLevelDebug = "Logging:DebugBuildLogLevel";
        private const string ConfigKeyFallbackLogDir = "Logging:DefaultFallbackLogDirectory";

        // --- Internal State Variables ---
        /// <summary>
        /// The full path to the current day's log file.
        /// </summary>
        private static string s_logFilePath = string.Empty;

        /// <summary>
        /// The date for which the current s_logFilePath is valid. Used for daily rolling.
        /// </summary>
        private static DateTime s_currentDateForLogFile = DateTime.MinValue;

        /// <summary>
        /// Lock object to ensure thread-safe write operations to the log file and state variables.
        /// </summary>
        private static readonly object s_lockObject = new object();

        /// <summary>
        /// Flag to indicate if the Logger has been successfully initialized.
        /// Prevents multiple initializations and ensures logging only occurs after setup.
        /// </summary>
        private static bool s_isInitialized = false;

        /// <summary>
        /// Holds a reference to the background task responsible for archiving old log files.
        /// </summary>
        private static Task? s_archivingTask;
        #endregion

        #region Public Properties
        /// <summary>
        /// Gets a value indicating whether the Logger's <see cref="Initialize"/> method has been called.
        /// This indicates that an attempt to configure the logger has been made.
        /// If true, logging will proceed (either to configured paths or fallbacks).
        /// If false, logging calls will typically be ignored or only output to Debug.
        /// </summary>
        public static bool IsInitialized => s_isInitialized;
        #endregion

        #region Initialization
        /// <summary>
        /// Initializes the Logger with configuration settings from the provided <see cref="IConfiguration"/> instance.
        /// This method **must** be called once at application startup before any logging calls are made.
        /// It reads settings for the log directory, minimum log level, log filename format, and archiving parameters.
        /// </summary>
        /// <param name="configuration">The application configuration instance. If null, the logger will attempt to use defaults and log critical errors to Debug output.</param>
        public static void Initialize(IConfiguration? configuration)
        {
            lock (s_lockObject) // Ensure thread-safe initialization.
            {
                if (s_isInitialized)
                {
                    Debug.WriteLine($"[{GetTimestamp()}] WARNING: Logger.Initialize called more than once. Skipping re-initialization.");
                    return;
                }

                try
                {
                    if (configuration == null)
                    {
                        Debug.WriteLine($"[{GetTimestamp()}] CRITICAL: Logger.Initialize called with null configuration. Using hardcoded defaults and minimal functionality.");
                    }
                    else
                    {
                        s_configuredBaseLogDirectory = configuration[ConfigKeyBaseLogDirectory] ?? string.Empty;
                        if (string.IsNullOrWhiteSpace(s_configuredBaseLogDirectory))
                        {
                            Debug.WriteLine($"[{GetTimestamp()}] WARNING: Config key '{ConfigKeyBaseLogDirectory}' not found or empty. Effective log path will use fallback logic.");
                        }
                        else
                        {
                            Debug.WriteLine($"[{GetTimestamp()}] INFO: Base log directory from config: '{s_configuredBaseLogDirectory}'");
                        }

                        string? configuredFallbackDir = configuration[ConfigKeyFallbackLogDir];
                        if (!string.IsNullOrWhiteSpace(configuredFallbackDir))
                        {
                            try
                            {
                                s_effectiveFallbackDirectory = Environment.ExpandEnvironmentVariables(configuredFallbackDir);
                                Debug.WriteLine($"[{GetTimestamp()}] INFO: Using configured fallback log directory: '{s_effectiveFallbackDirectory}'");
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine($"[{GetTimestamp()}] WARNING: Error expanding configured fallback log directory '{configuredFallbackDir}': {ex.Message}. Using hardcoded fallback '{s_effectiveFallbackDirectory}'.");
                            }
                        }
                        else
                        {
                            Debug.WriteLine($"[{GetTimestamp()}] INFO: Config key '{ConfigKeyFallbackLogDir}' not found or empty. Using hardcoded fallback log directory: '{s_effectiveFallbackDirectory}'");
                        }

                        string? logLevelString;
                        string configKeyUsed;
#if DEBUG
                        configKeyUsed = ConfigKeyLogLevelDebug;
                        logLevelString = configuration[configKeyUsed];
                        if (string.IsNullOrWhiteSpace(logLevelString))
                        {
                            Debug.WriteLine($"[{GetTimestamp()}] DEBUG: Config key '{ConfigKeyLogLevelDebug}' not found. Falling back to '{ConfigKeyLogLevelRelease}'.");
                            configKeyUsed = ConfigKeyLogLevelRelease;
                            logLevelString = configuration[configKeyUsed];
                        }
#else
                        configKeyUsed = ConfigKeyLogLevelRelease;
                        logLevelString = configuration[configKeyUsed];
#endif
                        if (!string.IsNullOrWhiteSpace(logLevelString) && Enum.TryParse(logLevelString, true, out LogLevel parsedLevel))
                        {
                            s_minimumLogLevel = parsedLevel;
                            Debug.WriteLine($"[{GetTimestamp()}] INFO: Minimum log level set from '{configKeyUsed}': {s_minimumLogLevel}");
                        }
                        else
                        {
                            Debug.WriteLine($"[{GetTimestamp()}] WARNING: Config key '{configKeyUsed}' missing or invalid ('{logLevelString}'). Defaulting minimum log level to: {s_minimumLogLevel}");
                        }

                        s_logFileNameFormat = configuration[ConfigKeyLogNameFormat] ?? s_logFileNameFormat;
                        if (!s_logFileNameFormat.Contains("{0"))
                        {
                            Debug.WriteLine($"[{GetTimestamp()}] WARNING: LogFileNameFormat '{s_logFileNameFormat}' from config does not seem to contain a date placeholder. Using default: '{{0:yyyy-MM-dd}}_LogFile.log'");
                            s_logFileNameFormat = "{0:yyyy-MM-dd}_LogFile.log";
                        }
                        Debug.WriteLine($"[{GetTimestamp()}] INFO: Log filename format set to: '{s_logFileNameFormat}'");

                        s_archiveLogsOlderThanDays = configuration.GetValue<int>(ConfigKeyArchiveDays, s_archiveLogsOlderThanDays);
                        Debug.WriteLine($"[{GetTimestamp()}] INFO: Archive logs older than {s_archiveLogsOlderThanDays} days.");
                    }

                    EnsureLogFileIsCurrent(isInitializing: true);

                    s_archivingTask = Task.Run(() => ArchiveOldLogsAsync()).ContinueWith(t =>
                    {
                        if (t.IsFaulted && t.Exception != null)
                        {
                            var flatEx = t.Exception.Flatten();
                            string errorDetails = flatEx.InnerExceptions.FirstOrDefault()?.Message ?? flatEx.Message;
                            Debug.WriteLine($"[{GetTimestamp()}] ERROR: Background log archiving task failed: {errorDetails}");
                            Debug.WriteLine($"[{GetTimestamp()}] DEBUG: Background log archiving exception: {flatEx}");
                            Log(LogLevel.Error, $"Background log archiving task failed: {errorDetails} - Exception: {flatEx}");
                        }
                        else if (t.IsCanceled)
                        {
                            Debug.WriteLine($"[{GetTimestamp()}] WARNING: Background log archiving task was cancelled.");
                            Log(LogLevel.Warning, "Background log archiving task was cancelled.");
                        }
                    }, TaskScheduler.Default);

                    s_isInitialized = true;
                    Log(LogLevel.Info, $"--- Logger Initialized (Version: {System.Reflection.Assembly.GetExecutingAssembly().GetName().Version}, MinLevel: {s_minimumLogLevel}, LogPath: '{Path.GetDirectoryName(s_logFilePath)}') ---");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[{GetTimestamp()}] FATAL: Logger initialization failed: {ex}");
                    if (string.IsNullOrEmpty(s_configuredBaseLogDirectory)) s_configuredBaseLogDirectory = string.Empty;
                    EnsureLogFileIsCurrent(isInitializing: true);
                    Log(LogLevel.Critical, $"Logger initialization critically failed: {ex.Message}. Logging may be impaired or using fallback path '{s_logFilePath}'. Exception: {ex}");
                    s_isInitialized = true;
                }
            }
        }
        #endregion

        #region LogLevel Enum
        /// <summary>
        /// Enumerates the different levels of logging, ordered from least to most severe.
        /// </summary>
        public enum LogLevel
        {
            /// <summary>Detailed diagnostic information, typically for developers during debugging.</summary>
            Trace = 0,
            /// <summary>Information useful for debugging but potentially too verbose for normal operation.</summary>
            Debug = 1,
            /// <summary>Informational messages highlighting the progress of the application.</summary>
            Info = 2,
            /// <summary>Indicates potentially harmful situations or non-critical errors.</summary>
            Warning = 3,
            /// <summary>Indicates errors that might allow the application to continue running.</summary>
            Error = 4,
            /// <summary>Indicates severe errors causing the application to terminate or function incorrectly.</summary>
            Critical = 5,
            /// <summary>No messages should be logged. Setting this as MinimumLogLevel effectively disables logging.</summary>
            None = 6
        }
        #endregion

        #region Core Logging Methods
        /// <summary>
        /// Ensures the log file path is set correctly for the current day and that necessary directories exist.
        /// This method should be called within a lock (<see cref="s_lockObject"/>) to ensure thread safety.
        /// </summary>
        /// <param name="isInitializing">A flag to indicate if this call is part of the initial logger setup.</param>
        private static void EnsureLogFileIsCurrent(bool isInitializing = false)
        {
            if (!s_isInitialized && !isInitializing)
            {
                Debug.WriteLine($"[{GetTimestamp()}] WARNING: Logger not initialized. Log call ignored during EnsureLogFileIsCurrent.");
                return;
            }

            if (DateTime.Today != s_currentDateForLogFile || string.IsNullOrEmpty(s_logFilePath) || s_logFilePath.Contains("FallbackLog") || s_logFilePath.Contains("EmergencyFallback"))
            {
                DateTime previousDate = s_currentDateForLogFile;
                s_currentDateForLogFile = DateTime.Today;
                string? previousLogFilePath = s_logFilePath;

                try
                {
                    string effectiveBaseDir;
                    if (!string.IsNullOrWhiteSpace(s_configuredBaseLogDirectory) && Directory.Exists(s_configuredBaseLogDirectory))
                    {
                        effectiveBaseDir = s_configuredBaseLogDirectory;
                    }
                    else
                    {
                        if (!string.IsNullOrWhiteSpace(s_configuredBaseLogDirectory) && !isInitializing)
                        {
                            Debug.WriteLine($"[{GetTimestamp()}] WARNING: Configured base log directory '{s_configuredBaseLogDirectory}' is invalid. Using fallback: '{s_effectiveFallbackDirectory}'.");
                        }
                        effectiveBaseDir = s_effectiveFallbackDirectory;
                    }

                    string userLogDirectory = GetUserLogDirectory(effectiveBaseDir);
                    CreateDirectoryIfNotExists(userLogDirectory);

                    string logFileName = string.Format(CultureInfo.InvariantCulture, s_logFileNameFormat, s_currentDateForLogFile);
                    s_logFilePath = Path.Combine(userLogDirectory, logFileName);

                    if (!isInitializing && previousDate != DateTime.MinValue && !string.IsNullOrEmpty(previousLogFilePath) && File.Exists(previousLogFilePath))
                    {
                        string rolloverMsg = CreateLogMessage(LogLevel.Info, $"Log rolled over to new file: {s_logFilePath}");
                        try { File.AppendAllText(previousLogFilePath, rolloverMsg + Environment.NewLine); }
                        catch (Exception ex) { Debug.WriteLine($"[{GetTimestamp()}] ERROR: Could not write rollover to old log '{previousLogFilePath}': {ex.Message}"); }
                    }

                    if (!isInitializing && LogLevel.Info >= s_minimumLogLevel)
                    {
                        string startingMsg = CreateLogMessage(LogLevel.Info, $"--- Log session started for {s_currentDateForLogFile:yyyy-MM-dd} at {s_logFilePath} (Previous: {previousLogFilePath ?? "N/A"}) ---");
                        WriteLogMessage(startingMsg);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[{GetTimestamp()}] FATAL: Failed to update log file path: {ex.Message}.");
                    string emergencyFallbackDir = Path.Combine(Path.GetTempPath(), "QCRA_EmergencyLogs", Environment.UserName);
                    try { Directory.CreateDirectory(emergencyFallbackDir); } catch { /* Best effort */ }
                    s_logFilePath = Path.Combine(emergencyFallbackDir, $"{DateTime.Today:yyyy-MM-dd}_EmergencyFallback.log");
                    Debug.WriteLine($"[{GetTimestamp()}] FATAL: Falling back to EMERGENCY log path: {s_logFilePath}");

                    if (!isInitializing && LogLevel.Critical >= s_minimumLogLevel)
                    {
                        string criticalErrorMsg = CreateLogMessage(LogLevel.Critical, $"CRITICAL FAILURE to set log path. Using emergency: {s_logFilePath}. Error: {ex.Message} --- Exception: {ex}");
                        WriteLogMessage(criticalErrorMsg);
                    }
                }
            }
        }

        /// <summary>
        /// Gets the user-specific log directory path under a given base directory.
        /// </summary>
        private static string GetUserLogDirectory(string baseLogDirectory)
        {
            string sanitizedUserName = string.Join("_", Environment.UserName.Split(Path.GetInvalidFileNameChars()));
            return Path.Combine(baseLogDirectory, sanitizedUserName);
        }

        /// <summary>
        /// Creates the specified directory if it does not already exist.
        /// </summary>
        private static void CreateDirectoryIfNotExists(string directoryPath)
        {
            if (!Directory.Exists(directoryPath))
            {
                try
                {
                    Directory.CreateDirectory(directoryPath);
                    Debug.WriteLine($"[{GetTimestamp()}] INFO: Created directory: {directoryPath}");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[{GetTimestamp()}] ERROR: Failed to create directory '{directoryPath}': {ex.Message}");
                    throw;
                }
            }
        }

        /// <summary>
        /// Creates a formatted log message string.
        /// </summary>
        private static string CreateLogMessage(LogLevel level, string message)
        {
            return $"[{GetTimestamp()}] [User:{Environment.UserName}] [PID:{Environment.ProcessId},TID:{Environment.CurrentManagedThreadId}] [{level.ToString().ToUpperInvariant(),-8}] {message}";
        }

        /// <summary>
        /// Writes the already formatted log message to the configured log file.
        /// Assumes called within a lock.
        /// </summary>
        private static void WriteLogMessage(string formattedLogMessage)
        {
            if (string.IsNullOrEmpty(s_logFilePath))
            {
                Debug.WriteLine($"[{GetTimestamp()}] ERROR: Log file path not set. Message lost: {formattedLogMessage}");
                return;
            }
            try
            {
                File.AppendAllText(s_logFilePath, formattedLogMessage + Environment.NewLine);
                Debug.WriteLine(formattedLogMessage);
            }
            catch (IOException ioEx)
            {
                Debug.WriteLine($"[{GetTimestamp()}] ERROR writing to log '{s_logFilePath}' (IO Error): {ioEx.Message}. Msg: {formattedLogMessage}");
                Console.Error.WriteLine($"[{GetTimestamp()}] ERROR writing to log '{s_logFilePath}' (IO Error): {ioEx.Message}");
            }
            catch (UnauthorizedAccessException uaEx)
            {
                Debug.WriteLine($"[{GetTimestamp()}] ERROR writing to log '{s_logFilePath}' (Access Denied): {uaEx.Message}. Msg: {formattedLogMessage}");
                Console.Error.WriteLine($"[{GetTimestamp()}] ERROR writing to log '{s_logFilePath}' (Access Denied): {uaEx.Message}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[{GetTimestamp()}] ERROR writing to log '{s_logFilePath}': {ex.Message}. Msg: {formattedLogMessage}");
                Console.Error.WriteLine($"[{GetTimestamp()}] ERROR writing to log '{s_logFilePath}': {ex.Message}");
            }
        }

        /// <summary>
        /// Logs a message with the specified log level, respecting the configured minimum level.
        /// Handles file rolling and thread safety.
        /// </summary>
        /// <param name="level">The <see cref="LogLevel"/> of the message.</param>
        /// <param name="message">The message string to log.</param>
        public static void Log(LogLevel level, string message)
        {
            if (level < s_minimumLogLevel || s_minimumLogLevel == LogLevel.None)
            {
                return;
            }
            if (!s_isInitialized)
            {
                Debug.WriteLine($"[{GetTimestamp()}] WARNING: Logger not initialized. Message lost: [{level}] {message}");
                return;
            }
            if (string.IsNullOrWhiteSpace(message)) return;

            try
            {
                lock (s_lockObject)
                {
                    EnsureLogFileIsCurrent();
                    string logMessage = CreateLogMessage(level, message);
                    WriteLogMessage(logMessage);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[{GetTimestamp()}] FATAL: Unexpected error in Logger.Log: {ex.Message}. Original: [{level}] {message}");
                Console.Error.WriteLine($"[{GetTimestamp()}] FATAL: Unexpected error in Logger.Log: {ex.Message}. Original: [{level}] {message}");
            }
        }
        #endregion

        #region Convenience Logging Methods
        /// <summary>Logs a detailed diagnostic message (Trace level).</summary>
        /// <param name="message">The message to log.</param>
        public static void LogTrace(string message) => Log(LogLevel.Trace, message);

        /// <summary>Logs a message useful for debugging (Debug level).</summary>
        /// <param name="message">The message to log.</param>
        public static void LogDebug(string message) => Log(LogLevel.Debug, message);

        /// <summary>Logs an informational message about application progress (Info level).</summary>
        /// <param name="message">The message to log.</param>
        public static void LogInfo(string message) => Log(LogLevel.Info, message);

        /// <summary>Logs a warning about a potentially harmful situation (Warning level).</summary>
        /// <param name="message">The message to log.</param>
        public static void LogWarning(string message) => Log(LogLevel.Warning, message);

        /// <summary>
        /// Logs a warning message along with exception details (Warning level).
        /// </summary>
        /// <param name="message">The warning message to log.</param>
        /// <param name="ex">The exception associated with the warning.</param>
        public static void LogWarning(string message, Exception ex) => Log(LogLevel.Warning, $"{message} Exception: {ex.ToString()}"); 

        /// <summary>Logs an error message (Error level).</summary>
        /// <param name="message">The message to log.</param>
        public static void LogError(string message) => Log(LogLevel.Error, message);

        /// <summary>Logs an error message along with exception details (Error level).</summary>
        /// <param name="message">The error message to log.</param>
        /// <param name="ex">The exception associated with the error.</param>
        public static void LogError(string message, Exception ex) => Log(LogLevel.Error, $"{message} Exception: {ex.ToString()}"); 

        /// <summary>Logs a critical error message (Critical level).</summary>
        /// <param name="message">The message to log.</param>
        public static void LogCritical(string message) => Log(LogLevel.Critical, message);

        /// <summary>Logs a critical error message along with exception details (Critical level).</summary>
        /// <param name="message">The critical error message to log.</param>
        /// <param name="ex">The exception associated with the critical error.</param>
        public static void LogCritical(string message, Exception ex) => Log(LogLevel.Critical, $"{message} Exception: {ex.ToString()}");
        #endregion

        #region Log Archiving (Private Async Methods)
        /// <summary>
        /// Asynchronously archives log files older than the configured number of days.
        /// This method is typically run as a background task.
        /// </summary>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        private static async Task ArchiveOldLogsAsync(CancellationToken cancellationToken = default)
        {
            Log(LogLevel.Info, "Starting background log archiving process...");
            try
            {
                string baseDirForArchiving;
                if (!string.IsNullOrWhiteSpace(s_configuredBaseLogDirectory) && Directory.Exists(s_configuredBaseLogDirectory))
                {
                    baseDirForArchiving = s_configuredBaseLogDirectory;
                }
                else
                {
                    baseDirForArchiving = s_effectiveFallbackDirectory;
                    if (!Directory.Exists(baseDirForArchiving))
                    {
                        Log(LogLevel.Warning, $"Base log directory for archiving ('{baseDirForArchiving}') invalid or does not exist. Skipping archiving.");
                        return;
                    }
                    Log(LogLevel.Warning, $"Configured base log directory was invalid. Archiving will proceed using fallback: '{baseDirForArchiving}'.");
                }

                DateTime cutoffDate = DateTime.Now.Date.AddDays(-s_archiveLogsOlderThanDays); // Use configured archive days
                Log(LogLevel.Info, $"Archiving logs with last write time older than {cutoffDate:yyyy-MM-dd} from base '{baseDirForArchiving}'.");

                foreach (string userDirectory in Directory.EnumerateDirectories(baseDirForArchiving, "*", SearchOption.TopDirectoryOnly))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await ArchiveLogsInUserDirectoryAsync(userDirectory, cutoffDate, cancellationToken);
                }
                Log(LogLevel.Info, "Background log archiving process completed.");
            }
            catch (OperationCanceledException) { Log(LogLevel.Warning, "Log archiving process cancelled by request."); }
            catch (UnauthorizedAccessException uaEx) { Log(LogLevel.Error, $"Access denied during log archiving task (Base: '{s_configuredBaseLogDirectory}', Fallback: '{s_effectiveFallbackDirectory}'): {uaEx.Message}"); }
            catch (Exception ex) { Log(LogLevel.Error, $"Unexpected error during background log archiving task: {ex.ToString()}"); }
        }

        /// <summary>
        /// Archives old log files within a specific user's log directory.
        /// </summary>
        private static async Task ArchiveLogsInUserDirectoryAsync(string userDirectory, DateTime cutoffDate, CancellationToken cancellationToken)
        {
            try
            {
                DirectoryInfo userDirInfo = new DirectoryInfo(userDirectory);
                if (!userDirInfo.Exists) return;

                Log(LogLevel.Debug, $"Checking user log directory for archiving: {userDirectory}");
                int archivedCount = 0;
                string archiveSubDirName = "Archive";

                var filesToArchive = userDirInfo.EnumerateFiles("*.log", SearchOption.TopDirectoryOnly)
                                                .Where(f => f.LastWriteTime < cutoffDate);

                foreach (FileInfo file in filesToArchive)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await ArchiveLogFileAsync(file, userDirectory, archiveSubDirName, cancellationToken);
                    archivedCount++;
                }
                if (archivedCount > 0) Log(LogLevel.Info, $"Archived {archivedCount} log file(s) from '{userDirectory}'.");
            }
            catch (OperationCanceledException) { throw; }
            catch (UnauthorizedAccessException uaEx) { Log(LogLevel.Error, $"Access denied archiving logs in '{userDirectory}': {uaEx.Message}"); }
            catch (Exception ex) { Log(LogLevel.Error, $"Error archiving logs in user directory '{userDirectory}': {ex.ToString()}"); }
        }

        /// <summary>
        /// Archives a single log file to a structured "Archive/YYYY/MM/WeekN" subfolder.
        /// Handles filename collisions by appending a timestamp.
        /// </summary>
        private static async Task ArchiveLogFileAsync(FileInfo fileToArchive, string baseUserDirectory, string archiveBaseDirName, CancellationToken cancellationToken)
        {
            try
            {
                DateTime fileDate = fileToArchive.LastWriteTime;
                string yearFolder = fileDate.ToString("yyyy");
                string monthFolder = fileDate.ToString("MM");
                int weekOfMonth = GetWeekOfMonthForArchive(fileDate);
                string weekFolder = $"Week{weekOfMonth}";

                string targetArchiveDir = Path.Combine(baseUserDirectory, archiveBaseDirName, yearFolder, monthFolder, weekFolder);
                Directory.CreateDirectory(targetArchiveDir);

                string archiveFilePath = Path.Combine(targetArchiveDir, fileToArchive.Name);

                if (File.Exists(archiveFilePath))
                {
                    string uniqueName = $"{Path.GetFileNameWithoutExtension(fileToArchive.Name)}_{DateTime.Now:yyyyMMddHHmmssfff}{fileToArchive.Extension}";
                    archiveFilePath = Path.Combine(targetArchiveDir, uniqueName);
                    Log(LogLevel.Warning, $"Archive file '{fileToArchive.Name}' already exists in target. Archiving as '{uniqueName}'.");
                }

                await Task.Run(() => fileToArchive.MoveTo(archiveFilePath), cancellationToken);
                Log(LogLevel.Info, $"Archived log file: '{fileToArchive.Name}' to '{archiveFilePath}'");
            }
            catch (OperationCanceledException) { throw; }
            catch (IOException ioEx) { Log(LogLevel.Error, $"IO error archiving file '{fileToArchive.FullName}': {ioEx.Message}"); }
            catch (Exception ex) { Log(LogLevel.Error, $"Unexpected error archiving file '{fileToArchive.FullName}': {ex.ToString()}"); }
        }

        /// <summary>
        /// Calculates the week number of a given date within its month for archiving purposes.
        /// </summary>
        private static int GetWeekOfMonthForArchive(DateTime date)
        {
            DateTime firstDayOfMonth = new DateTime(date.Year, date.Month, 1);
            int firstDayOfWeekMondayBased = ((int)firstDayOfMonth.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
            int week = (date.Day + firstDayOfWeekMondayBased - 1) / 7 + 1;
            return Math.Min(week, 5); // Cap at 5 for simplicity, adjust if more precision needed.
        }

        /// <summary>
        /// Helper to get a consistent timestamp string for debug/console outputs.
        /// </summary>
        private static string GetTimestamp() => DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff zzz");

        #endregion
    }
}