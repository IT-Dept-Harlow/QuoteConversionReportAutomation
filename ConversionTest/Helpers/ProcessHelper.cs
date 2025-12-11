#region Using Directives

// System-related namespaces for core functionalities.
using System;
using System.Diagnostics;
using System.IO;

// Project-specific namespaces for application components.
using QuoteConversionReportAutomation.Services.Logging;

#endregion

namespace QuoteConversionReportAutomation.Helpers
{
    #region Static Class Definition
    /// <summary>
    /// Provides static helper methods for common operations related to system processes and files.
    /// This class centralises functionality for tasks like opening files with their default
    /// application and managing running processes.
    /// </summary>
    public static class ProcessHelper
    {
        #region Public Static Methods

        /// <summary>
        /// Attempts to open the specified file using the default system application associated with its file type.
        /// </summary>
        /// <param name="filePath">The full path to the file to be opened.</param>
        /// <param name="fileTypeDescription">A user-friendly description of the file type (e.g., "raw report output") for use in error messages.</param>
        /// <exception cref="ArgumentException">Thrown if the provided <paramref name="filePath"/> is null or empty.</exception>
        /// <exception cref="FileNotFoundException">Thrown if the file at the specified <paramref name="filePath"/> does not exist.</exception>
        /// <exception cref="Exception">A general exception is thrown if the operating system fails to start the process for other reasons.</exception>
        public static void OpenFileWithDefaultApp(string? filePath, string fileTypeDescription)
        {
            // Validate that the file path is not null or empty.
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));
            }

            // Check if the file exists at the specified path before attempting to open it.
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"{Capitalise(fileTypeDescription)} file was not found.", filePath);
            }

            try
            {
                // Create a new process start info object with the file path.
                // Setting UseShellExecute to true allows the OS to find and use the default application.
                Process.Start(new ProcessStartInfo(filePath) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                // If the process fails to start, wrap the original exception in a more descriptive one.
                throw new Exception($"Could not open the {fileTypeDescription} file '{filePath}'.", ex);
            }
        }

        /// <summary>
        /// Attempts to find and forcefully terminate all running processes that match the specified name.
        /// </summary>
        /// <param name="processName">The name of the process to terminate (without the .exe extension).</param>
        public static void CloseProcessesByName(string processName)
        {
            // Do nothing if the process name is invalid.
            if (string.IsNullOrWhiteSpace(processName))
            {
                return;
            }

            try
            {
                // Get all processes currently running on the system with the given name.
                foreach (var process in Process.GetProcessesByName(processName))
                {
                    // Ensure the process object is properly disposed of after use.
                    using (process)
                    {
                        // Check if the process has already exited before attempting to kill it.
                        if (!process.HasExited)
                        {
                            // Forcefully terminate the process and any child processes it has spawned.
                            process.Kill(true);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Log any errors that occur during the process termination.
                Logger.LogError($"Error during CloseProcessesByName for '{processName}': {ex.Message}", ex);
            }
        }

        #endregion

        #region Private Static Methods

        /// <summary>
        /// Capitalises the first letter of a given string.
        /// </summary>
        /// <param name="text">The string to capitalise.</param>
        /// <returns>The capitalised string, or an empty string if the input is null or empty.</returns>
        private static string Capitalise(string? text) => text switch
        {
            null => string.Empty,
            "" => string.Empty,
            // For any other string, convert the first character to upper case and append the rest of the string.
            _ => char.ToUpperInvariant(text[0]) + text.Substring(1)
        };

        #endregion
    }
    #endregion
}