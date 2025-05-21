namespace QuoteConversionReportAutomation.Helpers
{
    using QuoteConversionReportAutomation.Services.Logging;
    // --- Using Statements ---
    using System;
    using System.Collections.Generic; // Required for List
    using System.Diagnostics; // For Process
    using System.Globalization; // Required for CultureInfo
    using System.IO;          // For File, Path
    using System.Linq;        // Required for LINQ
    using System.Windows.Forms; // For MessageBoxButtons, DialogResult etc. (used by FlexibleMessageBox)

    /// <summary>
    /// Provides static helper methods for common tasks like date calculations,
    /// string formatting, and basic file/process operations used across the application.
    /// GetPreviousWorkday now considers bank holidays.
    /// GetFinancialYearDates calculates financial year start and end dates (defaulting to May-April).
    /// Added GetNthPreviousWorkday for calculating a date N working days prior.
    /// Added GetPreviousDayOfWeek to find the date of the last specified day of the week.
    /// </summary>
    public static class ReportHelper
    {
        #region Date Calculation Helpers

        /// <summary>
        /// Calculates the start and end dates of a financial year.
        /// The financial year is defined by its starting year and the month/day it begins.
        /// For example, if financialYearStartYear is 2023, startMonth is 5 (May), startDay is 1,
        /// it represents the financial year from May 1, 2023, to April 30, 2024.
        /// </summary>
        /// <param name="financialYearStartYear">The calendar year in which the financial year starts (e.g., 2023 for FY May 2023 - April 2024).</param>
        /// <param name="startMonth">The month the financial year starts (default is 5 for May).</param>
        /// <param name="startDay">The day of the month the financial year starts (default is 1).</param>
        /// <returns>A tuple containing the start (DateFrom) and end (DateTo) dates of the specified financial year.</returns>
        public static (DateTime DateFrom, DateTime DateTo) GetFinancialYearDates(int financialYearStartYear, int startMonth = 5, int startDay = 1)
        {
            Logger.LogTrace($"ReportHelper.GetFinancialYearDates: Calculating for FY starting in {financialYearStartYear}, month {startMonth}, day {startDay}");
            DateTime dateFrom = new DateTime(financialYearStartYear, startMonth, startDay);
            DateTime dateTo = new DateTime(financialYearStartYear + 1, startMonth, startDay).AddDays(-1);
            Logger.LogDebug($"ReportHelper.GetFinancialYearDates: Calculated FY (starting {startMonth}/{startDay}) {financialYearStartYear}-{financialYearStartYear + 1} as {dateFrom:yyyy-MM-dd} to {dateTo:yyyy-MM-dd}");
            return (dateFrom, dateTo);
        }


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

            while (true)
            {
                // Check for Saturday
                if (previousDay.DayOfWeek == DayOfWeek.Saturday)
                {
                    Logger.LogTrace($"ReportHelper.GetPreviousWorkday: {previousDay:yyyy-MM-dd} is Saturday, moving to Friday.");
                    previousDay = previousDay.AddDays(-1); // Move to Friday
                }
                // Check for Sunday
                else if (previousDay.DayOfWeek == DayOfWeek.Sunday)
                {
                    Logger.LogTrace($"ReportHelper.GetPreviousWorkday: {previousDay:yyyy-MM-dd} is Sunday, moving to Friday.");
                    previousDay = previousDay.AddDays(-2); // Move to Friday (from Sunday)
                }

                // Check if the current 'previousDay' is a bank holiday
                if (!BankHolidayHelper.IsBankHoliday(previousDay))
                {
                    // If it's not a weekend (already handled) and not a bank holiday, it's a working day.
                    Logger.LogDebug($"ReportHelper.GetPreviousWorkday: Previous workday for {currentDate:yyyy-MM-dd} is {previousDay:yyyy-MM-dd}.");
                    return previousDay;
                }
                else
                {
                    // If it's a bank holiday, log it and subtract another day to check again.
                    Logger.LogTrace($"ReportHelper.GetPreviousWorkday: {previousDay:yyyy-MM-dd} is a bank holiday. Checking day before.");
                    previousDay = previousDay.AddDays(-1);
                }
            }
        }

        /// <summary>
        /// Calculates the Nth previous working day from a given date, skipping weekends and bank holidays.
        /// </summary>
        /// <param name="currentDate">The date to calculate from (usually Today).</param>
        /// <param name="n">The number of working days to go back (e.g., 0 for the current date if it's a workday, 1 for the first previous workday, etc.).
        /// If n is 0, it returns currentDate if it's a workday, otherwise the previous workday.
        /// </param>
        /// <returns>The DateTime representing the Nth previous workday.</returns>
        public static DateTime GetNthPreviousWorkday(DateTime currentDate, int n)
        {
            if (n < 0)
            {
                Logger.LogWarning($"GetNthPreviousWorkday called with n < 0 ({n}). Returning currentDate.");
                return currentDate; // Or throw ArgumentOutOfRangeException
            }

            Logger.LogTrace($"ReportHelper.GetNthPreviousWorkday: Calculating {n}th previous workday from {currentDate:yyyy-MM-dd}");
            DateTime resultDate = currentDate;

            // If n is 0, check if current date is a workday. If not, find the first previous one.
            if (n == 0)
            {
                while (resultDate.DayOfWeek == DayOfWeek.Saturday ||
                       resultDate.DayOfWeek == DayOfWeek.Sunday ||
                       BankHolidayHelper.IsBankHoliday(resultDate))
                {
                    resultDate = resultDate.AddDays(-1);
                }
                Logger.LogDebug($"ReportHelper.GetNthPreviousWorkday (n=0): Effective workday for {currentDate:yyyy-MM-dd} is {resultDate:yyyy-MM-dd}.");
                return resultDate;
            }

            // If n > 0, find n previous workdays
            int workdaysToGoBack = n;
            while (workdaysToGoBack > 0)
            {
                resultDate = resultDate.AddDays(-1);
                if (resultDate.DayOfWeek != DayOfWeek.Saturday &&
                    resultDate.DayOfWeek != DayOfWeek.Sunday &&
                    !BankHolidayHelper.IsBankHoliday(resultDate))
                {
                    workdaysToGoBack--;
                }
                Logger.LogTrace($"ReportHelper.GetNthPreviousWorkday: Step, workdaysToGoBack: {workdaysToGoBack}, current resultDate: {resultDate:yyyy-MM-dd}");
            }

            Logger.LogInfo($"ReportHelper.GetNthPreviousWorkday: {n}th previous workday for {currentDate:yyyy-MM-dd} is {resultDate:yyyy-MM-dd}.");
            return resultDate;
        }


        /// <summary>
        /// Calculates the date of the last occurrence of a specific day of the week, on or before the given reference date.
        /// For example, getting the previous Friday from today.
        /// </summary>
        /// <param name="referenceDate">The date to start searching backwards from.</param>
        /// <param name="targetDayOfWeek">The desired day of the week.</param>
        /// <returns>The date of the last occurrence of the targetDayOfWeek.</returns>
        public static DateTime GetPreviousDayOfWeek(DateTime referenceDate, DayOfWeek targetDayOfWeek)
        {
            Logger.LogTrace($"ReportHelper.GetPreviousDayOfWeek: Finding previous {targetDayOfWeek} from {referenceDate:yyyy-MM-dd}");
            DateTime resultDate = referenceDate;
            while (resultDate.DayOfWeek != targetDayOfWeek)
            {
                resultDate = resultDate.AddDays(-1);
            }
            Logger.LogDebug($"ReportHelper.GetPreviousDayOfWeek: Previous {targetDayOfWeek} from {referenceDate:yyyy-MM-dd} is {resultDate:yyyy-MM-dd}.");
            return resultDate;
        }


        /// <summary>
        /// Calculates the date range for the Monthly report type, returning the *previous* full month.
        /// </summary>
        /// <param name="referenceDate">The date used as a reference (usually Today).</param>
        /// <returns>A tuple containing the start date (DateFrom) and end date (DateTo) for the previous month.</returns>
        public static (DateTime DateFrom, DateTime DateTo) CalculateMonthlyRange(DateTime referenceDate)
        {
            DateTime firstDayOfCurrentMonth = new(referenceDate.Year, referenceDate.Month, 1);
            DateTime dateTo = firstDayOfCurrentMonth.AddDays(-1); // Last day of previous month
            DateTime dateFrom = dateTo.AddDays(1).AddMonths(-1); // First day of previous month

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
            DateTime dateTo = firstDayOfCurrentQuarter.AddDays(-1); // Last day of previous quarter
            DateTime dateFrom = firstDayOfCurrentQuarter.AddMonths(-3); // First day of previous quarter
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
        public static void OpenFileWithDefaultApp(string? filePath, string fileTypeDescription)
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
                using (process) // Ensure process object is disposed
                {
                    try
                    {
                        if (!process.HasExited)
                        {
                            Logger.LogInfo($"Attempting to terminate '{processName}' process ID: {process.Id} (MainWindowTitle: '{process.MainWindowTitle}')");
                            process.Kill(true); // Request graceful shutdown first if possible, then forceful
                            if (process.WaitForExit(5000)) // Wait up to 5 seconds
                                Logger.LogInfo($"Successfully terminated '{processName}' process ID: {process.Id}");
                            else
                                Logger.LogWarning($"'{processName}' process ID: {process.Id} did not terminate within 5 seconds after Kill.");
                        }
                    }
                    catch (InvalidOperationException ex) when (ex.Message.Contains("Process has exited") || ex.Message.Contains("No process is associated"))
                    {
                        // Process already exited or is not accessible.
                        Logger.LogInfo($"'{processName}' process ID: {process.Id} likely already exited or no longer accessible.");
                    }
                    catch (System.ComponentModel.Win32Exception ex) when ((uint)ex.ErrorCode == 0x80004005 && ex.NativeErrorCode == 5) // Access is denied (NativeErrorCode 5)
                    {
                        Logger.LogWarning($"Access denied when trying to terminate '{processName}' process ID: {process.Id}. It might be a system process or require higher privileges.");
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
