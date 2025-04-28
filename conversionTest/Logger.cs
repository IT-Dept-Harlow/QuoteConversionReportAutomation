// C# 10 File-Scoped Namespace
namespace conversionTest;

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
/// from configuration.
/// </summary>
public static class Logger
{
    // Configuration - Read from IConfiguration during initialization
    private static string s_baseLogDirectory = string.Empty; // Set in Initialize
    private const int ArchiveLogsOlderThanDays = 30;
    private const string ConfigKeyLogDirectory = "settings:LogDirectory"; // Key in appsettings.json
    private static readonly string s_defaultFallbackDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "conversionTest", "Logs");


    // State variables
    private static string s_logFilePath = string.Empty;
    private static DateTime s_currentDate = DateTime.MinValue;
    private static readonly object s_lockObject = new();
    private static bool s_isInitialized = false; // Flag to prevent re-initialization
    private static Task? s_archivingTask; // Hold reference to the background task

    /// <summary>
    /// Initializes the Logger with configuration settings. Must be called once at application startup.
    /// </summary>
    /// <param name="configuration">The application configuration instance.</param>
    public static void Initialize(IConfiguration configuration)
    {
        lock (s_lockObject) // Ensure thread-safe initialization
        {
            if (s_isInitialized)
            {
                Log(LogLevel.Warning, "Logger already initialized. Skipping re-initialization.");
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
                    Log(LogLevel.Warning, $"Configuration key '{ConfigKeyLogDirectory}' not found or empty. Using fallback directory: {s_baseLogDirectory}");
                }
                else
                {
                    // Optional: Resolve relative paths based on application directory if needed
                    // if (!Path.IsPathRooted(s_baseLogDirectory))
                    // {
                    //    s_baseLogDirectory = Path.Combine(AppContext.BaseDirectory, s_baseLogDirectory);
                    // }
                    Log(LogLevel.Info, $"Logger initialized. Base log directory set to: {s_baseLogDirectory}");
                }

                // Ensure the initial log file path is set correctly
                EnsureLogFileIsCurrent(isInitializing: true); // Pass flag to avoid logging during init log

                // Start archiving in the background only after successful initialization
                s_archivingTask = Task.Run(() => ArchiveOldLogsAsync()).ContinueWith(t =>
                {
                    if (t.IsFaulted && t.Exception != null)
                    {
                        var flatEx = t.Exception.Flatten();
                        Log(LogLevel.Error, $"Background log archiving failed: {flatEx.InnerExceptions.FirstOrDefault()?.Message ?? flatEx.Message}");
                        Log(LogLevel.Debug, $"Background log archiving exception details: {flatEx}");
                    }
                    else if (t.IsCanceled) Log(LogLevel.Warning, "Background log archiving task cancelled.");
                }, TaskScheduler.Default);

                s_isInitialized = true;
            }
            catch (Exception ex)
            {
                // Log initialization errors to Debug output
                Debug.WriteLine($"FATAL: Logger initialization failed: {ex}");
                // Attempt to set a very basic fallback path if initialization failed badly
                if (string.IsNullOrEmpty(s_baseLogDirectory)) s_baseLogDirectory = Path.GetTempPath();
                EnsureLogFileIsCurrent(isInitializing: true); // Try setting path even on error
                Log(LogLevel.Critical, $"Logger initialization failed: {ex.Message}. Logging may be impaired.");
                s_isInitialized = true; // Mark as initialized even on error to prevent loops, but logging might be broken
            }
        }
    }


    /// <summary>
    /// Enumerates the different levels of logging.
    /// </summary>
    public enum LogLevel { Debug, Info, Warning, Error, Critical }

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
                    string rolloverMsg = CreateLogMessage(LogLevel.Info, $"Log rolled over to new file: {s_logFilePath}");
                    try { File.AppendAllText(previousLogFilePath, rolloverMsg + Environment.NewLine); } catch { /* Ignore error writing to old log */ }

                    string startingMsg = CreateLogMessage(LogLevel.Info, $"Starting log for {s_currentDate:yyyy-MM-dd}. Previous log file: {previousLogFilePath}");
                    WriteLogMessage(startingMsg); // Write to the new file
                }
                else if (!isInitializing) // Log initialization message if called after initial setup
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
                // Log the error to the fallback path if possible
                if (!isInitializing) Log(LogLevel.Critical, $"Failed to set primary log path. Falling back to: {s_logFilePath}. Error: {ex.Message}");
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
        return $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff zzz}] [{Environment.UserName}] [PID:{Environment.ProcessId}] [{level.ToString().ToUpperInvariant(),-8}] {message}";
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
#if DEBUG
            Debug.WriteLine(logMessage);
#endif
        }
        catch (Exception ex) // Catch specific exceptions if needed (IO, UnauthorizedAccess)
        {
            Debug.WriteLine($"ERROR writing to log file '{s_logFilePath}': {ex}");
            Console.Error.WriteLine($"ERROR writing to log file '{s_logFilePath}': {ex}");
        }
    }

    /// <summary>
    /// Logs a message with the specified log level. Handles file rolling and thread safety.
    /// </summary>
    public static void Log(LogLevel level, string message)
    {
        if (!s_isInitialized)
        {
            Debug.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff zzz}] WARNING: Logger not initialized. Message lost: [{level}] {message}");
            return; // Do not log if not initialized
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
    public static void LogDebug(string message) => Log(LogLevel.Debug, message);
    public static void LogInfo(string message) => Log(LogLevel.Info, message);
    public static void LogWarning(string message) => Log(LogLevel.Warning, message);
    public static void LogError(string message) => Log(LogLevel.Error, message);
    public static void LogError(string message, Exception ex) => Log(LogLevel.Error, $"{message} Exception: {ex}");
    public static void LogCritical(string message) => Log(LogLevel.Critical, message);
    public static void LogCritical(string message, Exception ex) => Log(LogLevel.Critical, $"{message} Exception: {ex}");

    #region Log Archiving (Async)

    private static async Task ArchiveOldLogsAsync(CancellationToken cancellationToken = default)
    {
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

            foreach (FileInfo file in userDirInfo.EnumerateFiles("*.log", SearchOption.TopDirectoryOnly)
                                                .Where(f => f.LastWriteTime < cutoffDate))
            {
                cancellationToken.ThrowIfCancellationRequested();
                await ArchiveLogFileAsync(file, userDirectory, cancellationToken);
                archivedCount++;
            }
            if (archivedCount > 0) Log(LogLevel.Info, $"Archived {archivedCount} log file(s) from {userDirectory}.");
        }
        catch (OperationCanceledException) { throw; }
        catch (UnauthorizedAccessException uaEx) { Log(LogLevel.Error, $"Access denied archiving logs in {userDirectory}: {uaEx.Message}"); }
        catch (Exception ex) { Log(LogLevel.Error, $"Error archiving logs in {userDirectory}: {ex}"); }
    }

    private static async Task ArchiveLogFileAsync(FileInfo fileToArchive, string baseUserDirectory, CancellationToken cancellationToken)
    {
        try
        {
            DateTime fileDate = fileToArchive.LastWriteTime;
            string year = fileDate.ToString("yyyy");
            string month = fileDate.ToString("MM");
            int weekOfMonth = GetWeekOfMonth(fileDate);
            string archiveDir = Path.Combine(baseUserDirectory, "Archive", year, month, $"Week{weekOfMonth}");
            Directory.CreateDirectory(archiveDir);
            string archiveFilePath = Path.Combine(archiveDir, fileToArchive.Name);

            if (File.Exists(archiveFilePath))
            {
                string uniqueName = $"{Path.GetFileNameWithoutExtension(fileToArchive.Name)}_{DateTime.Now:yyyyMMddHHmmssfff}{fileToArchive.Extension}";
                archiveFilePath = Path.Combine(archiveDir, uniqueName);
                Log(LogLevel.Warning, $"Archive file '{fileToArchive.Name}' already exists. Saving as '{uniqueName}'.");
            }

            await Task.Run(() => File.Move(fileToArchive.FullName, archiveFilePath), cancellationToken);
            Log(LogLevel.Info, $"Archived log file: {fileToArchive.Name} to {archiveFilePath}");
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex) { Log(LogLevel.Error, $"Error archiving file {fileToArchive.FullName}: {ex}"); }
    }

    private static int GetWeekOfMonth(DateTime date)
    {
        DateTime firstDayOfMonth = new DateTime(date.Year, date.Month, 1);
        int firstDayOfMonthDayOfWeek = ((int)firstDayOfMonth.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
        int weekOfMonth = (date.Day + firstDayOfMonthDayOfWeek - 1) / 7 + 1;
        return Math.Min(weekOfMonth, 5);
    }

    #endregion
}
