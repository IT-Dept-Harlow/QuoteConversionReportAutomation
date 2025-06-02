// ReportHelper.cs
// Provides static helper methods for common tasks such as date calculations,
// string formatting, and basic file/process operations used across the application.
// Financial year calculations now require start month/day to be passed in,
// allowing for configuration-driven financial year definitions.
// File opening methods now throw exceptions instead of showing UI messages directly.
// C# 10+ Features.

#region Using Directives
// System related namespaces
using System;
using System.Collections.Generic; // Required for List (though not directly used in this version, good for context)
using System.Diagnostics;       // For Process class
using System.Globalization;     // Required for CultureInfo
using System.IO;                // For File, Path operations
using System.Linq;              // Required for LINQ operations (e.g., on Process arrays)
// System.Windows.Forms is removed as FlexibleMessageBox calls are removed.
// The caller (UI layer) will handle displaying messages.

// Project specific namespaces
using QuoteConversionReportAutomation.Services.Logging; // For Logger
#endregion

namespace QuoteConversionReportAutomation.Helpers
{
    /// <summary>
    /// Provides static helper methods for various common tasks across the application,
    /// including date calculations (considering workdays, bank holidays, financial years),
    /// string manipulations, and file/process operations.
    /// </summary>
    public static class ReportHelper
    {
        #region Date Calculation Helpers

        /// <summary>
        /// Calculates the start and end dates of a financial year based on the provided parameters.
        /// The financial year is defined by its starting calendar year and the specific month and day it begins.
        /// For example, if `financialYearStartCalendarYear` is 2023, `startMonth` is 5 (May), and `startDay` is 1,
        /// this method calculates the financial year from May 1, 2023, to April 30, 2024.
        /// </summary>
        /// <param name="financialYearStartCalendarYear">The calendar year in which the financial year starts (e.g., 2023 for FY May 2023 - April 2024).</param>
        /// <param name="startMonth">The month the financial year starts (e.g., 5 for May). This should be read from application configuration.</param>
        /// <param name="startDay">The day of the month the financial year starts (e.g., 1 for the 1st). This should be read from application configuration.</param>
        /// <returns>A tuple containing the start date (<see cref="DateTime.DateFrom"/>) and end date (<see cref="DateTime.DateTo"/>) of the specified financial year.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="startMonth"/> or <paramref name="startDay"/> are invalid for date construction.</exception>
        public static (DateTime DateFrom, DateTime DateTo) GetFinancialYearDates(int financialYearStartCalendarYear, int startMonth, int startDay)
        {
            Logger.LogTrace($"ReportHelper.GetFinancialYearDates: Calculating for FY starting in {financialYearStartCalendarYear}, Configured StartMonth: {startMonth}, Configured StartDay: {startDay}");
            if (startMonth < 1 || startMonth > 12) throw new ArgumentOutOfRangeException(nameof(startMonth), "Start month must be between 1 and 12.");
            // Day validation depends on the month and year, DateTime constructor will handle this.

            DateTime dateFrom;
            DateTime dateTo;
            try
            {
                dateFrom = new DateTime(financialYearStartCalendarYear, startMonth, startDay);
                // The financial year ends one day before the start of the next financial year.
                dateTo = new DateTime(financialYearStartCalendarYear + 1, startMonth, startDay).AddDays(-1);
            }
            catch (ArgumentOutOfRangeException ex)
            {
                Logger.LogError($"ReportHelper.GetFinancialYearDates: Invalid date components for FY calculation. Year: {financialYearStartCalendarYear}, Month: {startMonth}, Day: {startDay}. Error: {ex.Message}", ex);
                throw; // Re-throw to indicate failure to the caller.
            }

            Logger.LogDebug($"ReportHelper.GetFinancialYearDates: Calculated FY (Starting {startMonth}/{startDay}) for calendar year {financialYearStartCalendarYear} as: {dateFrom:yyyy-MM-dd} to {dateTo:yyyy-MM-dd}");
            return (dateFrom, dateTo);
        }

        /// <summary>
        /// Calculates the previous working day from a given date, skipping weekends and bank holidays.
        /// Bank holidays are determined using <see cref="BankHolidayHelper"/>.
        /// </summary>
        /// <param name="currentDate">The date from which to calculate the previous workday (typically <see cref="DateTime.Today"/>).</param>
        /// <returns>A <see cref="DateTime"/> object representing the previous working day.</returns>
        public static DateTime GetPreviousWorkday(DateTime currentDate)
        {
            Logger.LogTrace($"ReportHelper.GetPreviousWorkday: Calculating previous workday for {currentDate:yyyy-MM-dd}");
            DateTime previousDay = currentDate.AddDays(-1); // Start by checking the day immediately before.

            // Loop backwards until a working day is found.
            while (true)
            {
                DayOfWeek dayOfWeek = previousDay.DayOfWeek;
                // Check for weekends first.
                if (dayOfWeek == DayOfWeek.Saturday)
                {
                    Logger.LogTrace($"ReportHelper.GetPreviousWorkday: {previousDay:yyyy-MM-dd} is Saturday, adjusting to Friday.");
                    previousDay = previousDay.AddDays(-1); // Move to Friday.
                }
                else if (dayOfWeek == DayOfWeek.Sunday)
                {
                    Logger.LogTrace($"ReportHelper.GetPreviousWorkday: {previousDay:yyyy-MM-dd} is Sunday, adjusting to Friday.");
                    previousDay = previousDay.AddDays(-2); // Move to Friday (from Sunday).
                }
                // Then, check if the adjusted (or original) 'previousDay' is a bank holiday.
                // BankHolidayHelper.IsBankHoliday should handle its own date normalization if needed.
                else if (!BankHolidayHelper.IsBankHoliday(previousDay.Date)) // Ensure we check Date part only for bank holidays
                {
                    // If it's not a weekend (already handled by falling through the above checks)
                    // AND it's not a bank holiday, then it's a working day.
                    Logger.LogDebug($"ReportHelper.GetPreviousWorkday: Previous workday for {currentDate:yyyy-MM-dd} found: {previousDay:yyyy-MM-dd}.");
                    return previousDay.Date; // Return only the Date part.
                }
                // If it is a bank holiday (and not a weekend day that was already adjusted).
                else
                {
                    Logger.LogTrace($"ReportHelper.GetPreviousWorkday: {previousDay:yyyy-MM-dd} is a bank holiday. Checking day before.");
                    previousDay = previousDay.AddDays(-1); // Move to the day before the bank holiday and re-evaluate.
                }
            }
        }

        /// <summary>
        /// Calculates the Nth previous working day from a given reference date, skipping weekends and bank holidays.
        /// </summary>
        /// <param name="referenceDate">The date to calculate backwards from.</param>
        /// <param name="nWorkdaysBack">The number of working days to go back.
        /// If 0, it returns <paramref name="referenceDate"/> if it's a workday, otherwise the first previous workday.
        /// If 1, it returns the first previous workday, and so on.</param>
        /// <returns>A <see cref="DateTime"/> object representing the Nth previous working day (Date part only).</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="nWorkdaysBack"/> is negative.</exception>
        public static DateTime GetNthPreviousWorkday(DateTime referenceDate, int nWorkdaysBack)
        {
            if (nWorkdaysBack < 0)
            {
                Logger.LogError($"GetNthPreviousWorkday called with negative nWorkdaysBack: {nWorkdaysBack}. This is not supported.");
                throw new ArgumentOutOfRangeException(nameof(nWorkdaysBack), "Number of workdays to go back cannot be negative.");
            }

            Logger.LogTrace($"ReportHelper.GetNthPreviousWorkday: Calculating {nWorkdaysBack}th previous workday from {referenceDate:yyyy-MM-dd}");
            DateTime resultDate = referenceDate.Date; // Start with the Date part of the reference.

            if (nWorkdaysBack == 0) // Special case for n=0: find current or first previous workday.
            {
                while (resultDate.DayOfWeek == DayOfWeek.Saturday ||
                       resultDate.DayOfWeek == DayOfWeek.Sunday ||
                       BankHolidayHelper.IsBankHoliday(resultDate))
                {
                    resultDate = resultDate.AddDays(-1);
                    Logger.LogTrace($"GetNthPreviousWorkday (n=0 adjustment): Current date not workday, moved to {resultDate:yyyy-MM-dd}");
                }
                Logger.LogDebug($"ReportHelper.GetNthPreviousWorkday (n=0): Effective workday for {referenceDate:yyyy-MM-dd} is {resultDate:yyyy-MM-dd}.");
                return resultDate;
            }

            // For n > 0, count back 'nWorkdaysBack' working days.
            int workdaysFound = 0;
            DateTime currentDateToCheck = referenceDate.Date; // Start from reference date to find previous ones.

            while (workdaysFound < nWorkdaysBack)
            {
                currentDateToCheck = currentDateToCheck.AddDays(-1); // Move to the previous day.
                if (currentDateToCheck.DayOfWeek != DayOfWeek.Saturday &&
                    currentDateToCheck.DayOfWeek != DayOfWeek.Sunday &&
                    !BankHolidayHelper.IsBankHoliday(currentDateToCheck))
                {
                    workdaysFound++; // Found a workday.
                    resultDate = currentDateToCheck; // This is a candidate for the Nth previous workday.
                    Logger.LogTrace($"GetNthPreviousWorkday: Found workday #{workdaysFound}: {resultDate:yyyy-MM-dd}");
                }
            }

            Logger.LogInfo($"ReportHelper.GetNthPreviousWorkday: {nWorkdaysBack}th previous workday from {referenceDate:yyyy-MM-dd} is {resultDate:yyyy-MM-dd}.");
            return resultDate;
        }

        /// <summary>
        /// Calculates the date of the last occurrence of a specific day of the week,
        /// on or before the given reference date. For example, to find the previous Friday from today.
        /// </summary>
        /// <param name="referenceDate">The date to start searching backwards from.</param>
        /// <param name="targetDayOfWeek">The desired <see cref="DayOfWeek"/>.</param>
        /// <returns>The <see cref="DateTime"/> of the last occurrence of the <paramref name="targetDayOfWeek"/> (Date part only).</returns>
        public static DateTime GetPreviousDayOfWeek(DateTime referenceDate, DayOfWeek targetDayOfWeek)
        {
            Logger.LogTrace($"ReportHelper.GetPreviousDayOfWeek: Finding previous {targetDayOfWeek} from reference date {referenceDate:yyyy-MM-dd}");
            DateTime resultDate = referenceDate.Date; // Start with the Date part.
            while (resultDate.DayOfWeek != targetDayOfWeek)
            {
                resultDate = resultDate.AddDays(-1);
            }
            Logger.LogDebug($"ReportHelper.GetPreviousDayOfWeek: Previous {targetDayOfWeek} from {referenceDate:yyyy-MM-dd} is {resultDate:yyyy-MM-dd}.");
            return resultDate;
        }

        /// <summary>
        /// Calculates the date range for the Monthly report type, returning the *previous* full calendar month.
        /// </summary>
        /// <param name="referenceDate">The date used as a reference (usually <see cref="DateTime.Today"/>).</param>
        /// <returns>A tuple containing the start date (<see cref="DateTime.DateFrom"/>) and end date (<see cref="DateTime.DateTo"/>) for the previous month.</returns>
        public static (DateTime DateFrom, DateTime DateTo) CalculateMonthlyRange(DateTime referenceDate)
        {
            DateTime firstDayOfCurrentMonth = new DateTime(referenceDate.Year, referenceDate.Month, 1);
            DateTime lastDayOfPreviousMonth = firstDayOfCurrentMonth.AddDays(-1);    // End date of the range.
            DateTime firstDayOfPreviousMonth = lastDayOfPreviousMonth.AddDays(1).AddMonths(-1); // Start date of the range.

            Logger.LogDebug($"ReportHelper.CalculateMonthlyRange for reference {referenceDate:yyyy-MM-dd}: From {firstDayOfPreviousMonth:yyyy-MM-dd} To {lastDayOfPreviousMonth:yyyy-MM-dd}");
            return (firstDayOfPreviousMonth, lastDayOfPreviousMonth);
        }

        /// <summary>
        /// Calculates the date range for the Quarterly report type, returning the *previous* full calendar quarter.
        /// A quarter is defined as a three-month period (Jan-Mar, Apr-Jun, Jul-Sep, Oct-Dec).
        /// </summary>
        /// <param name="referenceDate">The date used as a reference (usually <see cref="DateTime.Today"/>).</param>
        /// <returns>A tuple containing the start date (<see cref="DateTime.DateFrom"/>) and end date (<see cref="DateTime.DateTo"/>) for the previous quarter.</returns>
        public static (DateTime DateFrom, DateTime DateTo) CalculateQuarterlyRange(DateTime referenceDate)
        {
            // Determine the current quarter (1-4).
            int currentQuarter = (referenceDate.Month - 1) / 3 + 1;
            // Determine the first month of the current quarter.
            int firstMonthOfCurrentQuarter = (currentQuarter - 1) * 3 + 1;
            // Get the first day of the current quarter.
            DateTime firstDayOfCurrentQuarter = new DateTime(referenceDate.Year, firstMonthOfCurrentQuarter, 1);

            // The end date of the previous quarter is one day before the first day of the current quarter.
            DateTime lastDayOfPreviousQuarter = firstDayOfCurrentQuarter.AddDays(-1);
            // The start date of the previous quarter is three months before the first day of the current quarter.
            DateTime firstDayOfPreviousQuarter = firstDayOfCurrentQuarter.AddMonths(-3);

            Logger.LogDebug($"ReportHelper.CalculateQuarterlyRange for reference {referenceDate:yyyy-MM-dd}: From {firstDayOfPreviousQuarter:yyyy-MM-dd} To {lastDayOfPreviousQuarter:yyyy-MM-dd}");
            return (firstDayOfPreviousQuarter, lastDayOfPreviousQuarter);
        }
        #endregion

        #region String Helpers
        /// <summary>
        /// Capitalizes the first letter of a given string.
        /// If the string is null, empty, or already starts with an uppercase letter, it's returned as is.
        /// </summary>
        /// <param name="text">The input string to capitalize.</param>
        /// <returns>The string with its first letter capitalized, or the original string if no change is needed or possible.</returns>
        public static string Capitalize(string? text)
        {
            // Use pattern matching for concise null/empty checks.
            return text switch
            {
                null => string.Empty, // Or throw ArgumentNullException if null is not acceptable.
                "" => string.Empty,
                // If already capitalized or not a letter, no change.
                // char.IsUpper(text[0]) check is implicit in string[0].ToString().ToUpper() == string[0].ToString()
                _ => char.ToUpperInvariant(text[0]) + text.Substring(1) // Use Substring for clarity.
            };
        }

        /// <summary>
        /// Gets a string representation of the quarter for a given date (e.g., "Q1", "Q2").
        /// </summary>
        /// <param name="date">The date for which to determine the quarter.</param>
        /// <returns>A string like "Q1", "Q2", "Q3", or "Q4".</returns>
        public static string GetQuarterString(DateTime date)
        {
            int quarter = (date.Month - 1) / 3 + 1; // (Month - 1) makes Jan=0, Feb=0, Mar=0 -> Q1 etc.
            return $"Q{quarter}";
        }
        #endregion

        #region File and Process Helpers
        /// <summary>
        /// Attempts to open the specified file using the default system application associated with its file type.
        /// Logs information about the attempt and throws specific exceptions on failure.
        /// </summary>
        /// <param name="filePath">The full path to the file to open. Must not be null or empty.</param>
        /// <param name="fileTypeDescription">A user-friendly description of the file type (e.g., "raw report output", "processed analysis file"), used for logging.</param>
        /// <exception cref="ArgumentException">Thrown if <paramref name="filePath"/> is null, empty, or whitespace.</exception>
        /// <exception cref="FileNotFoundException">Thrown if the file specified by <paramref name="filePath"/> does not exist.</exception>
        /// <exception cref="Exception">Can throw other exceptions from <see cref="Process.Start()"/>, such as <see cref="System.ComponentModel.Win32Exception"/> (e.g., if no application is associated with the file type or access is denied).</exception>
        public static void OpenFileWithDefaultApp(string? filePath, string fileTypeDescription)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                Logger.LogWarning($"Attempted to open {fileTypeDescription} but file path was null or empty.");
                throw new ArgumentException("File path cannot be null, empty, or whitespace.", nameof(filePath));
            }

            Logger.LogInfo($"Attempting to open {fileTypeDescription}: '{filePath}'");
            if (!File.Exists(filePath))
            {
                Logger.LogError($"{Capitalize(fileTypeDescription)} file not found at path: '{filePath}'");
                throw new FileNotFoundException($"{Capitalize(fileTypeDescription)} file was not found.", filePath);
            }

            try
            {
                // UseShellExecute = true is crucial for opening with the default application.
                Process.Start(new ProcessStartInfo(filePath) { UseShellExecute = true });
                Logger.LogInfo($"Successfully initiated opening of {fileTypeDescription} file: '{filePath}'");
            }
            catch (Exception ex) // Catches Win32Exception, ObjectDisposedException, etc.
            {
                Logger.LogError($"Error opening {fileTypeDescription} file '{filePath}': {ex.Message}", ex);
                // Re-throw a general exception with context, or let the original propagate.
                // For simplicity, re-throwing a new exception with more context.
                throw new Exception($"Could not open the {fileTypeDescription} file '{filePath}'. Error: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Attempts to find and terminate all running processes with the specified name.
        /// This method uses <see cref="Process.Kill(bool)"/> for forceful termination and should be used with caution.
        /// This is a synchronous method.
        /// </summary>
        /// <param name="processName">The name of the process to terminate (e.g., "EXCEL"). Case-insensitive.</param>
        public static void CloseProcessesByName(string processName)
        {
            if (string.IsNullOrWhiteSpace(processName))
            {
                Logger.LogWarning("CloseProcessesByName called with null, empty, or whitespace process name. No action taken.");
                return;
            }

            Logger.LogInfo($"Attempting to find and terminate processes with name: '{processName}'");
            Process[] processesToClose;
            try
            {
                // GetProcessesByName is case-insensitive by default on Windows.
                processesToClose = Process.GetProcessesByName(processName);
            }
            catch (Exception ex) // Catch errors during process enumeration (e.g., access denied).
            {
                Logger.LogError($"Error enumerating processes by name '{processName}': {ex.Message}", ex);
                return; // Cannot proceed if process list cannot be obtained.
            }

            if (processesToClose.Length == 0)
            {
                Logger.LogInfo($"No running processes named '{processName}' found to close.");
                return;
            }

            Logger.LogInfo($"Found {processesToClose.Length} process(es) named '{processName}'. Attempting to terminate...");
            foreach (var process in processesToClose)
            {
                // Use 'using' to ensure the Process object is disposed after use.
                using (process)
                {
                    try
                    {
                        if (!process.HasExited) // Check if the process is still running.
                        {
                            Logger.LogInfo($"Attempting to terminate process ID: {process.Id}, Name: '{process.ProcessName}' (MainWindowTitle: '{process.MainWindowTitle}')");
                            process.Kill(true); // true to kill entire process tree.
                            if (process.WaitForExit(5000)) // Wait up to 5 seconds for termination.
                            {
                                Logger.LogInfo($"Successfully terminated process ID: {process.Id} ('{processName}').");
                            }
                            else
                            {
                                Logger.LogWarning($"Process ID: {process.Id} ('{processName}') did not confirm termination within 5 seconds after Kill command. It might still be shutting down or termination failed.");
                            }
                        }
                        else
                        {
                            Logger.LogDebug($"Process ID: {process.Id} ('{processName}') had already exited before termination attempt.");
                        }
                    }
                    catch (InvalidOperationException ex) // Can occur if process has already exited.
                    {
                        Logger.LogWarning($"Invalid operation while trying to terminate process ID {process.Id} ('{processName}'). It may have already exited. Error: {ex.Message}");
                    }
                    catch (System.ComponentModel.Win32Exception ex) when ((uint)ex.ErrorCode == 0x80004005 && ex.NativeErrorCode == 5) // Access Denied (NativeErrorCode 5).
                    {
                        Logger.LogWarning($"Access denied when trying to terminate process ID {process.Id} ('{processName}'). It might be a system process or require higher privileges. Error: {ex.Message}");
                    }
                    catch (Exception ex) // Catch other potential errors during termination.
                    {
                        Logger.LogError($"Error terminating process ID {process.Id} ('{processName}'): {ex.Message}", ex);
                    }
                }
            }
            Logger.LogInfo($"Finished attempting to terminate processes named '{processName}'.");
        }
        #endregion
    }
}