using System;
using System.Diagnostics;         // Required for Process and Debug
using System.Globalization;       // Used for date formatting, ToUpperInvariant
using System.IO;                  // Required for Path, File, Directory operations
using System.Linq;                // Used by string.Split in GetUserLogDirectory indirectly
// using System.Threading.Tasks; // No longer needed as archiving Task is removed
using System.Configuration;       // Required for ConfigurationManager

// Standard namespace declaration for .NET Framework
namespace CrystalReportWrapper // Use the namespace of your wrapper project
{
    /// <summary>
    /// Provides static methods for logging messages to daily rolling log files,
    /// within user-specific directories. Reads base directory from App.config.
    /// Compatible with .NET Framework 4.8. Does NOT perform log archiving.
    /// </summary>
    public static class Logger
    {
        // Configuration
        private static string s_baseLogDirectory = string.Empty;

        private const string AppConfigKeyLogDirectory = "LogDirectory";

        // Fallback directory if App.config key is missing or invalid
        private static readonly string s_defaultFallbackDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CrystalReportWrapper", "Logs");

        // State variables
        private static string s_logFilePath = string.Empty;
        private static DateTime s_currentDate = DateTime.MinValue;
        private static readonly object s_lockObject = new object();
        private static bool s_isInitialized = false;
        // Archiving task variable removed

        // Static constructor to initialize automatically on first use
        static Logger()
        {
            Initialize();
        }

        /// <summary>
        /// Initializes the Logger. Reads settings from App.config or uses fallback.
        /// Called automatically by the static constructor or manually if needed.
        /// </summary>
        public static void Initialize()
        {
            lock (s_lockObject)
            {
                if (s_isInitialized)
                {
                    Debug.WriteLine("Logger already initialized. Skipping re-initialization.");
                    return;
                }

                try
                {
                    // Read base log directory from App.config
                    try
                    {
                        s_baseLogDirectory = ConfigurationManager.AppSettings[AppConfigKeyLogDirectory];
                    }
                    catch (ConfigurationErrorsException configEx)
                    {
                        Debug.WriteLine($"WARNING: Error reading App.config key '{AppConfigKeyLogDirectory}': {configEx.Message}");
                    }
                    catch (Exception appConfigEx) // Catch other potential errors reading config
                    {
                        Debug.WriteLine($"INFO: Could not read '{AppConfigKeyLogDirectory}' from App.config: {appConfigEx.Message}");
                    }

                    // Fallback to default path if not found or empty in App.config
                    if (string.IsNullOrWhiteSpace(s_baseLogDirectory))
                    {
                        s_baseLogDirectory = s_defaultFallbackDirectory;
                        Debug.WriteLine($"WARNING: Log directory not found or empty in App.config key '{AppConfigKeyLogDirectory}'. Using fallback directory: {s_baseLogDirectory}");
                    }
                    else
                    {
                        Debug.WriteLine($"INFO: Logger initialized. Base log directory configured to: {s_baseLogDirectory}");
                    }

                    // Ensure the initial log file path state is set correctly (creates directory if needed)
                    EnsureLogFileIsCurrent(isInitializing: true);

                    // --- Archiving task initiation removed ---

                    s_isInitialized = true;
                    Debug.WriteLine($"INFO: Logger initialization complete. Logging to directory structure under: {s_baseLogDirectory}");

                }
                catch (Exception ex)
                {
                    // Catch potential critical errors during initialization (e.g., path issues, permissions)
                    Debug.WriteLine($"FATAL: Logger initialization failed: {ex}");

                    // Attempt to set a last-resort directory if baseLogDirectory is still bad
                    if (string.IsNullOrEmpty(s_baseLogDirectory)) s_baseLogDirectory = Path.GetTempPath();

                    // Try to set the log path even on failure, using fallback logic within EnsureLogFileIsCurrent
                    EnsureLogFileIsCurrent(isInitializing: true);

                    // Log critical error if possible (might fail if path is bad/permission issues)
                    try { Log(LogLevel.Critical, $"Logger initialization failed: {ex.Message}. Logging may be impaired or use fallback path."); } catch { }

                    s_isInitialized = true; // Mark initialized even on failure to prevent repeated init attempts
                }
            }
        }


        /// <summary>
        /// Enumerates the different levels of logging.
        /// </summary>
        public enum LogLevel { Debug, Info, Warning, Error, Critical }

        /// <summary>
        /// Ensures the log file path is set correctly for the current day.
        /// Creates necessary directories if they don't exist. Should be called within a lock.
        /// </summary>
        /// <param name="isInitializing">Flag to indicate if called during initial setup.</param>
        private static void EnsureLogFileIsCurrent(bool isInitializing = false)
        {
            // Avoid execution if logger failed basic initialization, unless called *during* init
            if (!s_isInitialized && !isInitializing)
            {
                Debug.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff zzz}] WARNING: Logger not initialized. Log call ignored.");
                return;
            }

            // Check if date changed OR if path is missing/invalid OR if we are currently using the fallback path name
            // This triggers directory/path setup on first call or date change.
            if (DateTime.Today != s_currentDate || string.IsNullOrEmpty(s_logFilePath) || s_logFilePath.Contains("FallbackLog"))
            {
                DateTime previousDate = s_currentDate;
                s_currentDate = DateTime.Today;
                string previousLogFilePath = s_logFilePath; // Store old path for rollover message

                try
                {
                    // Base directory should have been set during Initialize()
                    if (string.IsNullOrEmpty(s_baseLogDirectory))
                    {
                        // This indicates a severe initialization failure
                        throw new InvalidOperationException("Base log directory is not set. Cannot determine log file path.");
                    }

                    // Get user specific path and ensure the directory exists
                    string userLogDirectory = GetUserLogDirectory(s_baseLogDirectory);
                    CreateDirectoryIfNotExists(userLogDirectory); // Throws on failure, caught below

                    // Construct the full path for today's log file
                    string dayToday = s_currentDate.ToString("yyyy-MM-dd");
                    s_logFilePath = Path.Combine(userLogDirectory, $"{dayToday}_LogFile.log");

                    // Log rollover information if applicable
                    if (!isInitializing && previousDate != DateTime.MinValue && !string.IsNullOrEmpty(previousLogFilePath) && !previousLogFilePath.Contains("FallbackLog"))
                    {
                        string rolloverMsg = CreateLogMessage(LogLevel.Info, $"Log rolled over to new file: {Path.GetFileName(s_logFilePath)}");
                        // Try writing to the *old* file one last time, but don't fail critically if it doesn't work
                        try { File.AppendAllText(previousLogFilePath, rolloverMsg + Environment.NewLine); } catch (Exception ex) { Debug.WriteLine($"INFO: Could not write rollover message to previous log '{previousLogFilePath}': {ex.Message}"); }

                        // Write a starting message to the *new* file
                        string startingMsg = CreateLogMessage(LogLevel.Info, $"Starting log for {s_currentDate:yyyy-MM-dd}. File: {Path.GetFileName(s_logFilePath)}");
                        WriteLogMessage(startingMsg); // Write to the new s_logFilePath
                    }
                    else if (!isInitializing) // First log entry after init or after fixing a fallback situation
                    {
                        string startingMsg = CreateLogMessage(LogLevel.Info, $"Logging started for {s_currentDate:yyyy-MM-dd}. File: {Path.GetFileName(s_logFilePath)}");
                        WriteLogMessage(startingMsg); // Write to the new s_logFilePath
                        Debug.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff zzz}] INFO: Logging to file: {s_logFilePath}");
                    }
                }
                catch (Exception ex) // Catches issues from GetUserLogDirectory, CreateDirectoryIfNotExists, Path operations
                {
                    // --- Fallback Mechanism ---
                    Debug.WriteLine($"CRITICAL: Failed to set/update primary log file path: {ex}");
                    string fallbackDir = s_defaultFallbackDirectory; // Use the predefined fallback base

                    try
                    {
                        // Attempt to create the fallback user directory structure
                        fallbackDir = GetUserLogDirectory(fallbackDir);
                        Directory.CreateDirectory(fallbackDir);
                    }
                    catch (Exception fallbackEx)
                    {
                        Debug.WriteLine($"CRITICAL: Failed to create default fallback directory '{fallbackDir}': {fallbackEx.Message}. Falling back to TempPath.");
                        try { fallbackDir = Path.GetTempPath(); } catch { fallbackDir = "."; } // Absolute last resort is current dir
                    }

                    // Set the fallback log file path (including a marker in the name)
                    s_logFilePath = Path.Combine(fallbackDir, $"{DateTime.Today:yyyy-MM-dd}_FallbackLog.log");
                    Debug.WriteLine($"CRITICAL: Falling back to log path: {s_logFilePath}");

                    // Attempt to log the critical error to the fallback file, but don't let this fail catastrophically
                    if (!isInitializing)
                    {
                        try { Log(LogLevel.Critical, $"Failed to set primary log path. Falling back to: {s_logFilePath}. Initial Error: {ex.Message}"); } catch { }
                    }
                }
            }
        }

        /// <summary>
        /// Gets the user-specific log directory path based on the base directory.
        /// Handles invalid characters in usernames.
        /// </summary>
        /// <param name="baseLogDirectory">The configured base log directory.</param>
        /// <returns>The full path to the user-specific log directory.</returns>
        /// <exception cref="ArgumentNullException">Thrown if baseLogDirectory is null or empty.</exception>
        /// <exception cref="IOException">Can be thrown by Path.Combine on invalid characters if sanitization fails.</exception>
        private static string GetUserLogDirectory(string baseLogDirectory)
        {
            if (string.IsNullOrWhiteSpace(baseLogDirectory))
            {
                // Should have been handled by Initialize, but double-check
                throw new ArgumentNullException(nameof(baseLogDirectory), "Base log directory cannot be null or whitespace.");
            }
            try
            {
                // Sanitize username to remove characters invalid for path names
                string userName = Environment.UserName;
                string sanitizedUserName = string.Join("_", userName.Split(Path.GetInvalidFileNameChars()));

                // Handle edge case of empty/invalid username after sanitization
                if (string.IsNullOrWhiteSpace(sanitizedUserName))
                {
                    sanitizedUserName = "DefaultUser";
                    Debug.WriteLine($"WARNING: Could not determine valid username ('{userName}'). Using '{sanitizedUserName}'.");
                }
                return Path.Combine(baseLogDirectory, sanitizedUserName);
            }
            catch (Exception ex) // Catch potential issues getting username or combining path
            {
                Debug.WriteLine($"ERROR: Failed to determine user-specific log directory using base '{baseLogDirectory}': {ex.Message}");
                // Rethrow to allow fallback mechanism in EnsureLogFileIsCurrent
                throw new IOException($"Failed to get user-specific log directory based on '{baseLogDirectory}'.", ex);
            }
        }

        /// <summary>
        /// Creates the directory if it does not exist. Throws exceptions on failure.
        /// </summary>
        /// <param name="directoryPath">The full path of the directory to create.</param>
        /// <exception cref="ArgumentException">Thrown if directoryPath is null or whitespace.</exception>
        /// <exception cref="IOException">Thrown if directory creation fails (covers various underlying exceptions like PathTooLong, Security, UnauthorizedAccess etc.).</exception>
        private static void CreateDirectoryIfNotExists(string directoryPath)
        {
            if (string.IsNullOrWhiteSpace(directoryPath))
            {
                throw new ArgumentException("Directory path cannot be null or whitespace.", nameof(directoryPath));
            }

            // Check existence first to potentially avoid exception/logging if already there
            if (!Directory.Exists(directoryPath))
            {
                try
                {
                    Directory.CreateDirectory(directoryPath);
                    // Internal debug message; avoid calling Log() here during initialization phases
                    Debug.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff zzz}] INFO: Created directory: {directoryPath}");
                }
                catch (Exception ex) // Catch common reasons for failure
                {
                    Debug.WriteLine($"ERROR: Failed to create directory '{directoryPath}': {ex.Message}");
                    // Rethrow a more specific exception to be handled by the caller (EnsureLogFileIsCurrent)
                    // This allows the fallback mechanism to trigger.
                    throw new IOException($"Failed to create directory '{directoryPath}'. See inner exception for details.", ex);
                }
            }
        }


        /// <summary>
        /// Creates the formatted log message string including timestamp, user, PID, and level.
        /// </summary>
        /// <param name="level">The log level.</param>
        /// <param name="message">The raw log message.</param>
        /// <returns>The fully formatted log string.</returns>
        private static string CreateLogMessage(LogLevel level, string message)
        {
            int processId = 0;
            string userName = "WRAPPER";
            try { processId = Process.GetCurrentProcess().Id; } catch { /* Ignore error getting process ID */ }       

            // Using string interpolation for readability
            // Pad the level string to align messages visually
            return $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff zzz}] [{userName}] [PID:{processId}] [{level.ToString().ToUpperInvariant(),-8}] {message}";
        }

        /// <summary>
        /// Writes the log message to the configured log file using File.AppendAllText.
        /// This method implicitly creates the file if it doesn't exist.
        /// Assumes called within a lock. Handles exceptions during file writing.
        /// </summary>
        /// <param name="logMessage">The fully formatted message to write.</param>
        private static void WriteLogMessage(string logMessage)
        {
            if (string.IsNullOrEmpty(s_logFilePath))
            {
                Debug.WriteLine($"ERROR: Log file path is not set. Cannot write message: {logMessage}");
                return; // Cannot write if path is unknown
            }

            try
            {
                // Append text to the file. Creates the file if it doesn't exist. Includes newline.
                File.AppendAllText(s_logFilePath, logMessage + Environment.NewLine);

                // Also write to Debug output when in DEBUG configuration for immediate feedback
#if DEBUG
                Debug.WriteLine(logMessage);
#endif
            }
            // Catch exceptions that might occur during file writing (permissions, disk full, path issues)
            catch (Exception ex)
            {
                string errorMsg = $"ERROR writing to log file '{s_logFilePath}': {ex.Message}";
                Debug.WriteLine(errorMsg);
                Console.Error.WriteLine(errorMsg); // Write to standard error stream as logging failed

                // Avoid recursive logging attempts here. If file writing fails, report to Debug/Console.
            }
        }

        /// <summary>
        /// Logs a message with the specified log level. Handles file rolling and thread safety.
        /// This is the main public entry point for logging.
        /// </summary>
        /// <param name="level">The severity level of the message.</param>
        /// <param name="message">The message content to log.</param>
        public static void Log(LogLevel level, string message)
        {
            // Attempt late initialization if necessary
            if (!s_isInitialized)
            {
                Debug.WriteLine($"WARNING: Logger not initialized when Log called. Attempting late init. Msg: [{level}] {message}");
                Initialize(); // Try to initialize now
                if (!s_isInitialized) // Check if late init failed
                {
                    // If still not initialized, logging is impossible. Output to Debug/Console and exit.
                    Debug.WriteLine($"CRITICAL: Late logger initialization failed. Message lost: [{level}] {message}");
                    Console.Error.WriteLine($"CRITICAL: Logger not initialized. Message lost: [{level}] {message}");
                    return;
                }
                Debug.WriteLine($"INFO: Late logger initialization successful.");
            }

            // Avoid logging null or empty messages
            if (string.IsNullOrWhiteSpace(message)) return;

            try
            {
                // Lock to ensure thread safety for checking date, setting file path, and writing
                lock (s_lockObject)
                {
                    // Ensure the log file path is correct for the current day (handles date rollover)
                    EnsureLogFileIsCurrent();

                    // Create the formatted log message
                    string logMessage = CreateLogMessage(level, message);

                    // Write the message to the file (AppendAllText handles file creation)
                    WriteLogMessage(logMessage);
                }
            }
            catch (Exception ex) // Catch any unexpected errors during the logging process itself
            {
                string errorMsg = $"FATAL: Unexpected error during Log(LogLevel, string) execution: {ex}";
                Debug.WriteLine(errorMsg);
                Console.Error.WriteLine(errorMsg);
                // Avoid trying to log this error via Log() again to prevent potential infinite loop
            }
        }

        // --- Helper methods for specific log levels ---
        public static void LogDebug(string message) => Log(LogLevel.Debug, message);
        public static void LogInfo(string message) => Log(LogLevel.Info, message);
        public static void LogWarning(string message) => Log(LogLevel.Warning, message);
        public static void LogError(string message) => Log(LogLevel.Error, message);
        public static void LogError(string message, Exception ex)
        {
            if (ex == null) Log(LogLevel.Error, message);
            // Include exception details, potentially using ToString() for stack trace if needed
            else Log(LogLevel.Error, $"{message} Exception: {ex.GetType().Name}: {ex.Message}");
        }
        public static void LogCritical(string message) => Log(LogLevel.Critical, message);
        public static void LogCritical(string message, Exception ex)
        {
            if (ex == null) Log(LogLevel.Critical, message);
            else Log(LogLevel.Critical, $"{message} Exception: {ex.GetType().Name}: {ex.Message} Details: {ex}"); // Include full ex details
        }

        // --- Log Archiving Region Removed ---

    } // End class Logger
} // End namespace