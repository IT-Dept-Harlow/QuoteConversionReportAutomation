// C# 10+ Features
using conversionTest;

namespace QuoteConversionReportAutomation
{
    // --- Using Statements ---
    using System;
    using System.Diagnostics; // For Process
    using System.IO;          // For File, Path
    using System.Threading;   // For CancellationToken
    using System.Threading.Tasks; // For Task
    using System.Windows.Forms; // For MessageBoxButtons, DialogResult etc. (used by FlexibleMessageBox)
    using JR.Utils.GUI.Forms; // For FlexibleMessageBox

    /// <summary>
    /// Provides static helper methods for common tasks like date calculations,
    /// string formatting, and basic file/process operations used across the application.
    /// </summary>
    public static class ReportHelper
    {
        #region Date Calculation Helpers

        /// <summary>
        /// Calculates the previous working day (Monday -> Friday, otherwise Day - 1).
        /// </summary>
        /// <param name="currentDate">The date to calculate from (usually Today).</param>
        /// <returns>The DateTime representing the previous workday.</returns>
        public static DateTime GetPreviousWorkday(DateTime currentDate)
        {
            DateTime previousDay = currentDate.AddDays(-1);
            return currentDate.DayOfWeek switch
            {
                DayOfWeek.Monday => currentDate.AddDays(-3), // If today is Monday, go back 3 days to Friday
                DayOfWeek.Sunday => currentDate.AddDays(-2), // If today is Sunday, go back 2 days to Friday
                _ => previousDay,                           // Otherwise (Tue-Sat), just go back 1 day
            };
        }

        /// <summary>
        /// Calculates the date range for the Monthly report type based on common business logic.
        /// If run early in the month (<= 15th), it returns the range for the *previous* full month.
        /// Otherwise, it returns the range from the start of the *current* month up to the given date.
        /// </summary>
        /// <param name="referenceDate">The date used as a reference (usually Today).</param>
        /// <returns>A tuple containing the start date (DateFrom) and end date (DateTo) for the monthly period.</returns>
        public static (DateTime DateFrom, DateTime DateTo) CalculateMonthlyRange(DateTime referenceDate)
        {
            DateTime dateFrom, dateTo;
            // Calculate based on the *previous* month if today is early in the current month
            if (referenceDate.Day <= 15)
            {
                // End date is the last day of the previous month
                DateTime firstDayOfCurrentMonth = new(referenceDate.Year, referenceDate.Month, 1);
                dateTo = firstDayOfCurrentMonth.AddDays(-1);
                // Start date is the first day of the previous month
                dateFrom = dateTo.AddDays(1).AddMonths(-1);
            }
            else // Otherwise, use the current month up to today
            {
                dateFrom = new DateTime(referenceDate.Year, referenceDate.Month, 1);
                dateTo = referenceDate; // End date is the reference date
            }
            return (dateFrom, dateTo);
        }

        /// <summary>
        /// Calculates the date range for the Quarterly report type, returning the *previous* full quarter.
        /// </summary>
        /// <param name="referenceDate">The date used as a reference (usually Today).</param>
        /// <returns>A tuple containing the start date (DateFrom) and end date (DateTo) for the previous quarter.</returns>
        public static (DateTime DateFrom, DateTime DateTo) CalculateQuarterlyRange(DateTime referenceDate)
        {
            // Report for the *previous* full quarter
            int currentQuarter = (referenceDate.Month - 1) / 3 + 1;
            // First day of the current quarter
            DateTime firstDayOfCurrentQuarter = new(referenceDate.Year, (currentQuarter - 1) * 3 + 1, 1);
            // End date is the day before the current quarter started (last day of previous quarter)
            DateTime dateTo = firstDayOfCurrentQuarter.AddDays(-1);
            // Start date is the first day of the previous quarter
            DateTime dateFrom = firstDayOfCurrentQuarter.AddMonths(-3);
            return (dateFrom, dateTo);
        }

        #endregion

        #region String Helpers

        /// <summary>
        /// Capitalizes the first letter of a string. Returns the original string if null or empty.
        /// </summary>
        /// <param name="text">The input string.</param>
        /// <returns>The string with the first letter capitalized, or the original string.</returns>
        public static string Capitalize(string? text)
        {
            // Use pattern matching for null/empty check
            return text switch
            {
                null => string.Empty, // Or return null based on desired behavior
                "" => string.Empty,
                _ => char.ToUpperInvariant(text[0]) + text[1..]
            };
        }

        /// <summary>
        /// Gets the quarter number string (e.g., "Q1", "Q2") for a given date.
        /// </summary>
        /// <param name="date">The date to determine the quarter for.</param>
        /// <returns>A string representing the quarter (e.g., "Q1").</returns>
        public static string GetQuarterString(DateTime date)
        {
            // Calculate quarter number (1-4)
            int quarter = (date.Month - 1) / 3 + 1;
            return $"Q{quarter}";
        }

        #endregion

        #region File and Process Helpers

        /// <summary>
        /// Opens the specified file using the default system application.
        /// Logs errors and shows a message box on failure.
        /// </summary>
        /// <param name="filePath">The full path to the file to open.</param>
        /// <param name="fileTypeDescription">A user-friendly description of the file type (e.g., "raw report output", "processed analysis file").</param>
        public static void OpenFileWithDefaultApp(string filePath, string fileTypeDescription)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                Logger.LogWarning($"Attempted to open {fileTypeDescription} but file path was null or empty.");
                FlexibleMessageBox.Show($"Cannot open {fileTypeDescription}: file path is missing.", "File Path Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            Logger.LogInfo($"Attempting to open {fileTypeDescription}: {filePath}");
            try
            {
                if (!File.Exists(filePath))
                {
                    Logger.LogWarning($"{Capitalize(fileTypeDescription)} file not found at path: {filePath}");
                    FlexibleMessageBox.Show($"{Capitalize(fileTypeDescription)} file was not found:\n{filePath}", "File Not Found", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Use Process.Start with UseShellExecute = true to open with default app
                Process.Start(new ProcessStartInfo(filePath) { UseShellExecute = true });

                Logger.LogInfo($"Successfully initiated opening of {fileTypeDescription} file.");
            }
            catch (Exception ex) // Catch potential errors like file access issues, no associated app, etc.
            {
                Logger.LogError($"Error opening {fileTypeDescription} file '{filePath}': {ex}");
                FlexibleMessageBox.Show($"Could not open the {fileTypeDescription} file.\nError: {ex.Message}", "File Open Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Attempts to find and terminate all running processes with the specified name.
        /// This is a forceful approach (Kill) and should be used with caution.
        /// Synchronous method.
        /// </summary>
        /// <param name="processName">The name of the process to terminate (e.g., "EXCEL").</param>
        public static void CloseProcessesByName(string processName)
        {
            if (string.IsNullOrWhiteSpace(processName))
            {
                Logger.LogWarning("CloseProcessesByName called with null or empty process name.");
                return;
            }

            Process[] processes;
            try
            {
                // Get all processes with the specified name
                processes = Process.GetProcessesByName(processName);
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error getting processes by name '{processName}': {ex.Message}");
                return; // Cannot proceed if getting processes fails
            }

            if (processes.Length == 0)
            {
                Logger.LogInfo($"No running '{processName}' processes found to close.");
                return;
            }

            Logger.LogInfo($"Found {processes.Length} '{processName}' processes. Attempting to terminate...");
            foreach (var process in processes)
            {
                using (process) // Ensure process object is disposed
                {
                    try
                    {
                        // Check if the process is still running before trying to kill
                        if (!process.HasExited)
                        {
                            Logger.LogInfo($"Attempting to terminate '{processName}' process ID: {process.Id} (MainWindowTitle: '{process.MainWindowTitle}')");
                            // Forcefully terminate the process and its child processes (if any)
                            process.Kill(true);
                            // Wait briefly for termination to complete
                            if (process.WaitForExit(5000)) // Wait up to 5 seconds, returns true if exited
                                Logger.LogInfo($"Successfully terminated '{processName}' process ID: {process.Id}");
                            else
                                Logger.LogWarning($"'{processName}' process ID: {process.Id} did not terminate within 5 seconds after Kill.");
                        }
                    }
                    catch (InvalidOperationException ex) when (ex.Message.Contains("Process has exited"))
                    {
                        // Ignore error if process already exited between check and kill attempt
                        Logger.LogInfo($"'{processName}' process ID: {process.Id} already exited.");
                    }
                    catch (Exception ex)
                    {
                        // Log errors during termination (e.g., access denied)
                        Logger.LogError($"Error terminating '{processName}' process ID {process.Id}: {ex.Message}");
                    }
                }
            }
            Logger.LogInfo($"Finished attempting to terminate '{processName}' processes.");
        }

        #endregion
    }
}
