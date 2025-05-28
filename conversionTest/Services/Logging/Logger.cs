// C# 10 File-Scoped Namespace
namespace QuoteConversionReportAutomation.Services.Logging;

using Microsoft.Extensions.Configuration; // Added for IConfiguration
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Provides static methods for logging messages to daily rolling log files,
/// with user-specific directories and archiving of old logs. Reads base directory
/// and minimum log level from configuration. Uses LogLevelDebug in DEBUG builds
/// and LogLevel in RELEASE builds.
/// </summary>
public static class Logger
{
    // Configuration - Read from IConfiguration during initialization
    private static string s_baseLogDirectory = string.Empty; // Set in Initialize
    private const int ArchiveLogsOlderThanDays = 7;
    private const string ConfigKeyLogDirectory = "settings:LogDirectory";
    private const string ConfigKeyLogLevelRelease = "settings:LogLevel"; // Key for Release level
    private const string ConfigKeyLogLevelDebug = "settings:LogLevelDebug"; // Key for Debug level
    private static readonly string s_defaultFallbackDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "conversionTest", "Logs");
    private static LogLevel s_minimumLogLevel = LogLevel.Info; // Default level

    // State variables
    private static string s_logFilePath = string.Empty;
    private static DateTime s_currentDate = DateTime.MinValue;
    private static readonly object s_lockObject = new();
    private static bool s_isInitialized = false; // Flag to prevent re-initialization
    private static Task? s_archivingTask; // Hold reference to the background task

    /// <summary>
    /// Initializes the Logger with configuration settings. Must be called once at application startup.
    /// Reads different LogLevel settings based on DEBUG or RELEASE build configuration.
    /// </summary>
    /// <param name="configuration">The application configuration instance.</param>
    public static void Initialize(IConfiguration configuration)
    {
        lock (s_lockObject) // Ensure thread-safe initialization
        {
            if (s_isInitialized)
            {
                Debug.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff zzz}] WARNING: Logger already initialized. Skipping re-initialization.");
                return;
            }

            ArgumentNullException.ThrowIfNull(configuration);

            try
            {
                // Read base log directory from configuration
                s_baseLogDirectory = configuration[ConfigKeyLogDirectory] ?? string.Empty;

                if (string.IsNullOrWhiteSpace(s_baseLogDirectory))
                {
                    s_baseLogDirectory = s_defaultFallbackDirectory;
                    Debug.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff zzz}] WARNING: Configuration key '{ConfigKeyLogDirectory}' not found or empty. Using fallback directory: {s_baseLogDirectory}");
                }
                else
                {
                    Debug.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff zzz}] INFO: Logger initialized. Base log directory set to: {s_baseLogDirectory}");
                }

                // *** Read and parse minimum log level based on build config ***
                string? logLevelString = null;
                string configKeyUsed = string.Empty;

#if DEBUG
                configKeyUsed = ConfigKeyLogLevelDebug;
                logLevelString = configuration[configKeyUsed];
                // Fallback to Release level if Debug level is missing
                if (string.IsNullOrEmpty(logLevelString))
                {
                    configKeyUsed = ConfigKeyLogLevelRelease;
                    logLevelString = configuration[configKeyUsed];
                    Debug.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff zzz}] DEBUG: Configuration key '{ConfigKeyLogLevelDebug}' not found. Falling back to '{configKeyUsed}'.");
                }
#else
                configKeyUsed = ConfigKeyLogLevelRelease;
                logLevelString = configuration[configKeyUsed];
#endif

                LogLevel defaultLevel = LogLevel.Info; // Default for both modes if config is invalid
                if (!string.IsNullOrEmpty(logLevelString) &&
                    Enum.TryParse(logLevelString, true, out LogLevel parsedLevel)) // Case-insensitive parse
                {
                    s_minimumLogLevel = parsedLevel;
                    Debug.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff zzz}] INFO: Minimum log level set from configuration key '{configKeyUsed}': {s_minimumLogLevel}");
                }
                else
                {
                    s_minimumLogLevel = defaultLevel;
                    Debug.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff zzz}] WARNING: Configuration key '{configKeyUsed}' missing or invalid ('{logLevelString}'). Defaulting minimum log level to: {s_minimumLogLevel}");
                }
                // *** END Updated ***

                // Ensure the initial log file path is set correctly
                EnsureLogFileIsCurrent(isInitializing: true); // Pass flag to avoid logging during init log

                // Start archiving in the background only after successful initialization
                s_archivingTask = Task.Run(() => ArchiveOldLogsAsync()).ContinueWith(t =>
                {
                    // Use Debug.WriteLine here as logging might fail if the error is file-related
                    if (t.IsFaulted && t.Exception != null)
                    {
                        var flatEx = t.Exception.Flatten();
                        Debug.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff zzz}] ERROR: Background log archiving failed: {flatEx.InnerExceptions.FirstOrDefault()?.Message ?? flatEx.Message}");
                        Debug.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff zzz}] DEBUG: Background log archiving exception details: {flatEx}");
                    }
                    else if (t.IsCanceled) Debug.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff zzz}] WARNING: Background log archiving task cancelled.");
                }, TaskScheduler.Default);

                s_isInitialized = true;
                // Log initialization success *after* marking as initialized
                Log(LogLevel.Info, $"--- Logger Initialized (v{System.Reflection.Assembly.GetExecutingAssembly().GetName().Version}, MinLevel: {s_minimumLogLevel}) ---");
            }
            catch (Exception ex)
            {
                // Log initialization errors to Debug output
                Debug.WriteLine($"FATAL: Logger initialization failed: {ex}");
                // Attempt to set a very basic fallback path if initialization failed badly
                if (string.IsNullOrEmpty(s_baseLogDirectory)) s_baseLogDirectory = Path.GetTempPath();
                EnsureLogFileIsCurrent(isInitializing: true); // Try setting path even on error
                // Log critical error using the potentially broken logger (it might write to fallback)
                Log(LogLevel.Critical, $"Logger initialization failed: {ex.Message}. Logging may be impaired.");
                s_isInitialized = true; // Mark as initialized even on error to prevent loops, but logging might be broken
            }
        }
    }


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
        /// <summary>No messages should be logged.</summary>
        None = 6 // Added None level
    }

    /// <summary>
    /// Ensures the log file path is set correctly for the current day.
    /// Creates necessary directories if they don't exist.
    /// Should be called within a lock.
    /// </summary>
    /// <param name="isInitializing">Flag to suppress certain log messages during initial setup.</param>
    private static void EnsureLogFileIsCurrent(bool isInitializing = false)
    {
        if (!s_isInitialized && !isInitializing)
        {
            Debug.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff zzz}] WARNING: Logger not initialized. Log call ignored.");
            return; // Don't attempt to log if not initialized
        }

        if (DateTime.Today != s_currentDate || string.IsNullOrEmpty(s_logFilePath) || s_logFilePath.Contains("FallbackLog"))
        {
            DateTime previousDate = s_currentDate;
            s_currentDate = DateTime.Today;
            string? previousLogFilePath = s_logFilePath; // Store old path before changing

            try
            {
                // Base directory should be set by Initialize()
                if (string.IsNullOrEmpty(s_baseLogDirectory))
                {
                    throw new InvalidOperationException("Base log directory is not set. Logger not initialized correctly.");
                }

                string userLogDirectory = GetUserLogDirectory(s_baseLogDirectory);
                CreateDirectoryIfNotExists(userLogDirectory);

                string dayToday = s_currentDate.ToString("yyyy-MM-dd");
                s_logFilePath = Path.Combine(userLogDirectory, $"{dayToday}_LogFile.log");

                // Log rollover info (only if not the very first initialization)
                if (!isInitializing && previousDate != DateTime.MinValue && !string.IsNullOrEmpty(previousLogFilePath))
                {
                    // Log rollover to previous file (best effort)
                    string rolloverMsg = CreateLogMessage(LogLevel.Info, $"Log rolled over to new file: {s_logFilePath}");
                    try { File.AppendAllText(previousLogFilePath, rolloverMsg + Environment.NewLine); } catch { /* Ignore error writing to old log */ }

                    // Log start message to new file (if level allows)
                    string startingMsg = CreateLogMessage(LogLevel.Info, $"Starting log for {s_currentDate:yyyy-MM-dd}. Previous log file: {previousLogFilePath}");
                    if (LogLevel.Info >= s_minimumLogLevel) // Check level before writing
                    {
                        WriteLogMessage(startingMsg);
                    }
                }
                else if (!isInitializing) // Log file path if called after initial setup
                {
                    Debug.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff zzz}] INFO: Logging to file: {s_logFilePath}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"FATAL: Failed to update log file path: {ex}");
                // Attempt fallback path
                string fallbackDir = s_defaultFallbackDirectory;
                try { Directory.CreateDirectory(fallbackDir); } catch { fallbackDir = Path.GetTempPath(); }
                s_logFilePath = Path.Combine(fallbackDir, $"{DateTime.Today:yyyy-MM-dd}_FallbackLog.log");
                Debug.WriteLine($"FATAL: Falling back to log path: {s_logFilePath}");
                // Log the error to the fallback path if possible (Critical level should always be logged unless MinLevel is None)
                if (!isInitializing && LogLevel.Critical >= s_minimumLogLevel)
                {
                    // Use the Log method directly here, it will handle the level check internally
                    Log(LogLevel.Critical, $"Failed to set primary log path. Falling back to: {s_logFilePath}. Error: {ex.Message}");
                }
            }
        }
    }

    /// <summary>
    /// Gets the user-specific log directory path.
    /// </summary>
    private static string GetUserLogDirectory(string baseLogDirectory)
    {
        string sanitizedUserName = string.Join("_", Environment.UserName.Split(Path.GetInvalidFileNameChars()));
        return Path.Combine(baseLogDirectory, sanitizedUserName);
    }

    /// <summary>
    /// Creates the directory if it does not exist.
    /// </summary>
    private static void CreateDirectoryIfNotExists(string directoryPath)
    {
        if (!Directory.Exists(directoryPath))
        {
            try
            {
                Directory.CreateDirectory(directoryPath);
                Debug.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff zzz}] INFO: Created directory: {directoryPath}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ERROR: Failed to create directory '{directoryPath}': {ex.Message}");
                throw;
            }
        }
    }

    /// <summary>
    /// Creates the formatted log message string.
    /// </summary>
    private static string CreateLogMessage(LogLevel level, string message)
    {
        // Added Thread ID for better async debugging
        return $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff zzz}] [User:{Environment.UserName}] [PID:{Environment.ProcessId},TID:{Environment.CurrentManagedThreadId}] [{level.ToString().ToUpperInvariant(),-8}] {message}";
    }

    /// <summary>
    /// Writes the log message to the configured log file. Assumes called within a lock.
    /// </summary>
    private static void WriteLogMessage(string logMessage)
    {
        if (string.IsNullOrEmpty(s_logFilePath)) // Safety check
        {
            Debug.WriteLine($"ERROR: Log file path not set. Message lost: {logMessage}");
            return;
        }
        try
        {
            File.AppendAllText(s_logFilePath, logMessage + Environment.NewLine);
            // Also write to Debug output for immediate visibility during debugging
            // (Consider making this conditional based on log level too if Debug output gets too noisy)
            Debug.WriteLine(logMessage);
        }
        catch (Exception ex) // Catch specific exceptions if needed (IO, UnauthorizedAccess)
        {
            Debug.WriteLine($"ERROR writing to log file '{s_logFilePath}': {ex}");
            Console.Error.WriteLine($"ERROR writing to log file '{s_logFilePath}': {ex}");
        }
    }

    /// <summary>
    /// Logs a message with the specified log level, respecting the configured minimum level.
    /// Handles file rolling and thread safety.
    /// </summary>
    public static void Log(LogLevel level, string message)
    {
        // Check minimum log level first
        if (level < s_minimumLogLevel)
        {
            return; // Skip logging if the message level is below the configured minimum
        }

        if (!s_isInitialized)
        {
            // Only log to Debug output if not initialized, as file logging won't work
            Debug.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff zzz}] WARNING: Logger not initialized. Message lost: [{level}] {message}");
            return;
        }
        if (string.IsNullOrWhiteSpace(message)) return;

        try
        {
            lock (s_lockObject)
            {
                EnsureLogFileIsCurrent(); // Check/roll file if needed
                string logMessage = CreateLogMessage(level, message);
                WriteLogMessage(logMessage);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"FATAL: Unexpected error during Log method: {ex}");
            Console.Error.WriteLine($"FATAL: Unexpected error during Log method: {ex}");
        }
    }

    // --- Helper methods for specific log levels ---
    // These helpers remain unchanged, the filtering happens in the main Log method.
    /// <summary>Logs a detailed diagnostic message if MinLevel is Trace.</summary>
    public static void LogTrace(string message) => Log(LogLevel.Trace, message);
    /// <summary>Logs a message useful for debugging if MinLevel is Debug or lower.</summary>
    public static void LogDebug(string message) => Log(LogLevel.Debug, message);
    /// <summary>Logs an informational message about application progress if MinLevel is Info or lower.</summary>
    public static void LogInfo(string message) => Log(LogLevel.Info, message);
    /// <summary>Logs a warning about a potentially harmful situation if MinLevel is Warning or lower.</summary>
    public static void LogWarning(string message) => Log(LogLevel.Warning, message);
    /// <summary>Logs an error message if MinLevel is Error or lower.</summary>
    public static void LogError(string message) => Log(LogLevel.Error, message);
    /// <summary>Logs an error message along with exception details if MinLevel is Error or lower.</summary>
    public static void LogError(string message, Exception ex) => Log(LogLevel.Error, $"{message} Exception: {ex}"); // Consider logging full exception details
    /// <summary>Logs a critical error message if MinLevel is Critical or lower.</summary>
    public static void LogCritical(string message) => Log(LogLevel.Critical, message);
    /// <summary>Logs a critical error message along with exception details if MinLevel is Critical or lower.</summary>
    public static void LogCritical(string message, Exception ex) => Log(LogLevel.Critical, $"{message} Exception: {ex}"); // Consider logging full exception details

    #region Log Archiving (Async)

    private static async Task ArchiveOldLogsAsync(CancellationToken cancellationToken = default)
    {
        // Use Log method which respects minimum level
        Log(LogLevel.Info, "Starting background log archiving process...");
        try
        {
            // Ensure base directory is set before proceeding
            string baseDirectory = s_baseLogDirectory;
            if (string.IsNullOrEmpty(baseDirectory) || !Directory.Exists(baseDirectory))
            {
                Log(LogLevel.Warning, $"Base log directory '{baseDirectory}' invalid or does not exist. Skipping archiving.");
                return;
            }

            DateTime cutoffDate = DateTime.Now.Date.AddDays(-ArchiveLogsOlderThanDays);
            Log(LogLevel.Info, $"Archiving logs with last write time older than {cutoffDate:yyyy-MM-dd}.");

            foreach (string userDirectory in Directory.EnumerateDirectories(baseDirectory, "*", SearchOption.TopDirectoryOnly))
            {
                cancellationToken.ThrowIfCancellationRequested();
                await ArchiveLogsInUserDirectoryAsync(userDirectory, cutoffDate, cancellationToken);
            }
            Log(LogLevel.Info, "Background log archiving process completed.");
        }
        catch (OperationCanceledException) { Log(LogLevel.Warning, "Log archiving process cancelled."); }
        catch (UnauthorizedAccessException uaEx) { Log(LogLevel.Error, $"Access denied during log archiving task (base directory '{s_baseLogDirectory}'): {uaEx.Message}"); }
        catch (Exception ex) { Log(LogLevel.Error, $"Error during background log archiving task: {ex}"); }
    }

    private static async Task ArchiveLogsInUserDirectoryAsync(string userDirectory, DateTime cutoffDate, CancellationToken cancellationToken)
    {
        try
        {
            DirectoryInfo userDirInfo = new DirectoryInfo(userDirectory);
            if (!userDirInfo.Exists) return;

            Log(LogLevel.Debug, $"Checking directory for archiving: {userDirectory}");
            int archivedCount = 0;

            // Ensure "Archive" directory itself is not enumerated if it exists at this level
            string archiveBaseDirName = "Archive";
            var filesToArchive = userDirInfo.EnumerateFiles("*.log", SearchOption.TopDirectoryOnly)
                                            .Where(f => f.LastWriteTime < cutoffDate);

            foreach (FileInfo file in filesToArchive)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await ArchiveLogFileAsync(file, userDirectory, archiveBaseDirName, cancellationToken);
                archivedCount++;
            }
            if (archivedCount > 0) Log(LogLevel.Info, $"Archived {archivedCount} log file(s) from {userDirectory}.");
        }
        catch (OperationCanceledException) { throw; }
        catch (UnauthorizedAccessException uaEx) { Log(LogLevel.Error, $"Access denied archiving logs in {userDirectory}: {uaEx.Message}"); }
        catch (Exception ex) { Log(LogLevel.Error, $"Error archiving logs in {userDirectory}: {ex}"); }
    }

    private static async Task ArchiveLogFileAsync(FileInfo fileToArchive, string baseUserDirectory, string archiveBaseDirName, CancellationToken cancellationToken)
    {
        try
        {
            DateTime fileDate = fileToArchive.LastWriteTime;
            string year = fileDate.ToString("yyyy");
            string month = fileDate.ToString("MM"); // Use MM for consistent sorting
            int weekOfMonth = GetWeekOfMonth(fileDate);
            string archiveDir = Path.Combine(baseUserDirectory, archiveBaseDirName, year, month, $"Week{weekOfMonth}");
            Directory.CreateDirectory(archiveDir); // Ensure target exists
            string archiveFilePath = Path.Combine(archiveDir, fileToArchive.Name);

            if (File.Exists(archiveFilePath))
            {
                // Handle collision - rename the file being archived
                string uniqueName = $"{Path.GetFileNameWithoutExtension(fileToArchive.Name)}_{DateTime.Now:yyyyMMddHHmmssfff}{fileToArchive.Extension}";
                archiveFilePath = Path.Combine(archiveDir, uniqueName);
                Log(LogLevel.Warning, $"Archive file '{fileToArchive.Name}' already exists in target. Archiving as '{uniqueName}'.");
            }

            await Task.Run(() => fileToArchive.MoveTo(archiveFilePath), cancellationToken); // Use MoveTo
            Log(LogLevel.Info, $"Archived log file: {fileToArchive.Name} to {archiveFilePath}");
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex) { Log(LogLevel.Error, $"Error archiving file {fileToArchive.FullName}: {ex}"); }
    }

    // Using a simpler week of month calculation consistent with previous code
    private static int GetWeekOfMonth(DateTime date)
    {
        DateTime firstDayOfMonth = new DateTime(date.Year, date.Month, 1);
        // Adjust first day to be Monday-based (0=Mon, 6=Sun) for calculation
        int firstDayOfWeekMondayBased = ((int)firstDayOfMonth.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
        // Calculate week number
        int weekOfMonth = (date.Day + firstDayOfWeekMondayBased - 1) / 7 + 1;
        // Cap at 5 for folder naming consistency if desired, though some months have 6 partial weeks
        return Math.Min(weekOfMonth, 5);
    }


    #endregion
}
