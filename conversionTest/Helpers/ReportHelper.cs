// C# 10+ Features
using conversionTest; // Assuming Logger is in this namespace or globally available

// Ensure this namespace matches your project structure
namespace QuoteConversionReportAutomation.Helpers
{
    // --- Using Statements ---
    using System;
    using System.Diagnostics; // For Process
    using System.IO;          // For File, Path
    // using System.Threading;   // For CancellationToken - Not directly used in this version of ReportHelper
    // using System.Threading.Tasks; // For Task - Not directly used in this version of ReportHelper
    using System.Windows.Forms; // For MessageBoxButtons, DialogResult etc. (used by FlexibleMessageBox)
    using QuoteConversionReportAutomation.Helpers;

    /// <summary>
    /// Provides static helper methods for common tasks like date calculations,
    /// string formatting, and basic file/process operations used across the application.
    /// GetPreviousWorkday now considers bank holidays.
    /// </summary>
    public static class ReportHelper
    {
        #region Date Calculation Helpers

        /// <summary>
        /// Calculates the previous working day, skipping weekends and bank holidays.
        /// Bank holidays are checked using BankHolidayHelper.
        /// </summary>
        /// <param name="currentDate">The date to calculate from (usually Today).</param>
        /// <returns>The DateTime representing the previous workday.</returns>
        public static DateTime GetPreviousWorkday(DateTime currentDate)
        {
            Logger.LogTrace($"ReportHelper.GetPreviousWorkday: Calculating previous workday for {currentDate:yyyy-MM-dd}");
            DateTime previousDay = currentDate.AddDays(-1);

            // Loop backwards until a non-weekend, non-bank holiday is found
            while (true)
            {
                // Check for weekends first
                if (previousDay.DayOfWeek == DayOfWeek.Saturday)
                {
                    Logger.LogTrace($"ReportHelper.GetPreviousWorkday: {previousDay:yyyy-MM-dd} is Saturday, moving to Friday.");
                    previousDay = previousDay.AddDays(-1); // Move to Friday
                }
                else if (previousDay.DayOfWeek == DayOfWeek.Sunday)
                {
                    Logger.LogTrace($"ReportHelper.GetPreviousWorkday: {previousDay:yyyy-MM-dd} is Sunday, moving to Friday.");
                    previousDay = previousDay.AddDays(-2); // Move to Friday (from Sunday)
                }

                // Now check if the (potentially adjusted) previousDay is a bank holiday
                // Ensure BankHolidayHelper is initialized (typically done at app startup)
                if (!BankHolidayHelper.IsBankHoliday(previousDay))
                {
                    // Not a weekend and not a bank holiday, so this is our workday
                    Logger.LogInfo($"ReportHelper.GetPreviousWorkday: Previous workday for {currentDate:yyyy-MM-dd} is {previousDay:yyyy-MM-dd}.");
                    return previousDay;
                }
                else
                {
                    Logger.LogTrace($"ReportHelper.GetPreviousWorkday: {previousDay:yyyy-MM-dd} is a bank holiday. Checking day before.");
                    // If it was a bank holiday, subtract another day and check again in the next iteration
                    previousDay = previousDay.AddDays(-1);
                }
            }
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
            if (referenceDate.Day <= 15)
            {
                DateTime firstDayOfCurrentMonth = new(referenceDate.Year, referenceDate.Month, 1);
                dateTo = firstDayOfCurrentMonth.AddDays(-1);
                dateFrom = dateTo.AddDays(1).AddMonths(-1);
            }
            else
            {
                dateFrom = new DateTime(referenceDate.Year, referenceDate.Month, 1);
                dateTo = referenceDate;
            }
            Logger.LogDebug($"ReportHelper.CalculateMonthlyRange for {referenceDate:yyyy-MM-dd}: From {dateFrom:yyyy-MM-dd} To {dateTo:yyyy-MM-dd}");
            return (dateFrom, dateTo);
        }

        /// <summary>
        /// Calculates the date range for the Quarterly report type, returning the *previous* full quarter.
        /// </summary>
        /// <param name="referenceDate">The date used as a reference (usually Today).</param>
        /// <returns>A tuple containing the start date (DateFrom) and end date (DateTo) for the previous quarter.</returns>
        public static (DateTime DateFrom, DateTime DateTo) CalculateQuarterlyRange(DateTime referenceDate)
        {
            int currentQuarter = (referenceDate.Month - 1) / 3 + 1;
            DateTime firstDayOfCurrentQuarter = new(referenceDate.Year, (currentQuarter - 1) * 3 + 1, 1);
            DateTime dateTo = firstDayOfCurrentQuarter.AddDays(-1);
            DateTime dateFrom = firstDayOfCurrentQuarter.AddMonths(-3);
            Logger.LogDebug($"ReportHelper.CalculateQuarterlyRange for {referenceDate:yyyy-MM-dd}: From {dateFrom:yyyy-MM-dd} To {dateTo:yyyy-MM-dd}");
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
            return text switch
            {
                null => string.Empty,
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

                Process.Start(new ProcessStartInfo(filePath) { UseShellExecute = true });
                Logger.LogInfo($"Successfully initiated opening of {fileTypeDescription} file.");
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error opening {fileTypeDescription} file '{filePath}': {ex.Message}", ex);
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
                processes = Process.GetProcessesByName(processName);
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error getting processes by name '{processName}': {ex.Message}");
                return;
            }

            if (processes.Length == 0)
            {
                Logger.LogInfo($"No running '{processName}' processes found to close.");
                return;
            }

            Logger.LogInfo($"Found {processes.Length} '{processName}' processes. Attempting to terminate...");
            foreach (var process in processes)
            {
                using (process)
                {
                    try
                    {
                        if (!process.HasExited)
                        {
                            Logger.LogInfo($"Attempting to terminate '{processName}' process ID: {process.Id} (MainWindowTitle: '{process.MainWindowTitle}')");
                            process.Kill(true);
                            if (process.WaitForExit(5000))
                                Logger.LogInfo($"Successfully terminated '{processName}' process ID: {process.Id}");
                            else
                                Logger.LogWarning($"'{processName}' process ID: {process.Id} did not terminate within 5 seconds after Kill.");
                        }
                    }
                    catch (InvalidOperationException ex) when (ex.Message.Contains("Process has exited"))
                    {
                        Logger.LogInfo($"'{processName}' process ID: {process.Id} already exited.");
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError($"Error terminating '{processName}' process ID {process.Id}: {ex.Message}");
                    }
                }
            }
            Logger.LogInfo($"Finished attempting to terminate '{processName}' processes.");
        }
        #endregion
    }
}
