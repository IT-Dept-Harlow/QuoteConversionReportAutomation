<<<<<<< HEAD
﻿// ReportHelper.cs
// Provides static helper methods for common tasks such as date calculations,
// string formatting, and basic file/process operations used across the application.
// This version adds the missing financial year and help content helper methods.

#region Using Directives
// System related namespaces
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

// Project specific namespaces
using QuoteConversionReportAutomation.Configuration;
using QuoteConversionReportAutomation.Services.Logging;
using QuoteConversionReportAutomation.Theming; // For ThemeSettings
#endregion

namespace QuoteConversionReportAutomation.Helpers
=======
﻿namespace QuoteConversionReportAutomation.Helpers
>>>>>>> parent of 171b8e4 (v1.9.2)
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
<<<<<<< HEAD
        /// Determines the calendar year in which the financial year for a given date starts.
        /// This is based on the financial year start month and day from the application configuration.
        /// </summary>
        /// <param name="referenceDate">The date to check (e.g., today's date).</param>
        /// <param name="configuration">The application configuration instance to read settings from.</param>
        /// <returns>The four-digit calendar year (e.g., 2023) in which the financial year begins.</returns>
        public static int GetFinancialYearStartCalendarYear(DateTime referenceDate, IConfiguration configuration)
        {
            int startMonth = configuration.GetValue<int>(AppConfigKeys.OperationalParameters.FinancialYearStartMonth, 5);
            int startDay = configuration.GetValue<int>(AppConfigKeys.OperationalParameters.FinancialYearStartDay, 1);

            // If the reference date is before the financial year start for the current calendar year,
            // then the financial year started in the previous calendar year.
            if (referenceDate.Month < startMonth || (referenceDate.Month == startMonth && referenceDate.Day < startDay))
            {
                return referenceDate.Year - 1;
            }
            // Otherwise, the financial year started in the current calendar year.
            return referenceDate.Year;
        }

        /// <summary>
        /// Calculates the start and end dates of a financial year based on the provided parameters.
        /// </summary>
        /// <param name="financialYearStartCalendarYear">The calendar year in which the financial year starts.</param>
        /// <param name="startMonth">The month the financial year starts (1-12).</param>
        /// <param name="startDay">The day of the month the financial year starts (1-31).</param>
        /// <returns>A tuple containing the start and end date of the specified financial year.</returns>
        public static (DateTime DateFrom, DateTime DateTo) GetFinancialYearDates(int financialYearStartCalendarYear, int startMonth, int startDay)
        {
            DateTime dateFrom = new DateTime(financialYearStartCalendarYear, startMonth, startDay);
            DateTime dateTo = dateFrom.AddYears(1).AddDays(-1);
=======
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
>>>>>>> parent of 171b8e4 (v1.9.2)
            return (dateFrom, dateTo);
        }


        /// <summary>
<<<<<<< HEAD
        /// Overload that calculates the start and end dates of a financial year using settings from IConfiguration.
        /// This simplifies calls from other parts of the application.
        /// </summary>
        /// <param name="financialYearStartCalendarYear">The calendar year in which the financial year starts.</param>
        /// <param name="configuration">The application configuration to source the start month and day from.</param>
        /// <returns>A tuple containing the start and end date of the specified financial year.</returns>
        public static (DateTime DateFrom, DateTime DateTo) GetFinancialYearDates(int financialYearStartCalendarYear, IConfiguration configuration)
        {
            int startMonth = configuration.GetValue<int>(AppConfigKeys.OperationalParameters.FinancialYearStartMonth, 5);
            int startDay = configuration.GetValue<int>(AppConfigKeys.OperationalParameters.FinancialYearStartDay, 1);
            return GetFinancialYearDates(financialYearStartCalendarYear, startMonth, startDay);
        }

        /// <summary>
        /// Calculates the previous working day from a given date, skipping weekends and bank holidays.
        /// </summary>
        /// <param name="currentDate">The date from which to calculate the previous workday.</param>
        /// <returns>A <see cref="DateTime"/> object representing the previous working day.</returns>
        public static DateTime GetPreviousWorkday(DateTime currentDate)
        {
            DateTime previousDay = currentDate.AddDays(-1);
            while (previousDay.DayOfWeek == DayOfWeek.Saturday || previousDay.DayOfWeek == DayOfWeek.Sunday || BankHolidayHelper.IsBankHoliday(previousDay))
            {
                previousDay = previousDay.AddDays(-1);
=======
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
>>>>>>> parent of 171b8e4 (v1.9.2)
            }
            return previousDay.Date;
        }

        /// <summary>
<<<<<<< HEAD
        /// Calculates the Nth previous working day from a given reference date.
        /// </summary>
        /// <param name="referenceDate">The date to calculate backwards from.</param>
        /// <param name="nWorkdaysBack">The number of working days to go back.</param>
        /// <returns>A <see cref="DateTime"/> object representing the Nth previous working day.</returns>
        public static DateTime GetNthPreviousWorkday(DateTime referenceDate, int nWorkdaysBack)
        {
            if (nWorkdaysBack < 0) throw new ArgumentOutOfRangeException(nameof(nWorkdaysBack), "Cannot be negative.");
            DateTime resultDate = referenceDate.Date;
            for (int i = 0; i < nWorkdaysBack; i++)
            {
                resultDate = GetPreviousWorkday(resultDate);
            }
=======
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
>>>>>>> parent of 171b8e4 (v1.9.2)
            return resultDate;
        }


        /// <summary>
<<<<<<< HEAD
        /// Calculates the date range for the Monthly report type (previous full calendar month).
        /// </summary>
        public static (DateTime DateFrom, DateTime DateTo) CalculateMonthlyRange(DateTime referenceDate)
        {
            DateTime firstDayOfCurrentMonth = new DateTime(referenceDate.Year, referenceDate.Month, 1);
            DateTime lastDayOfPreviousMonth = firstDayOfCurrentMonth.AddDays(-1);
            DateTime firstDayOfPreviousMonth = new DateTime(lastDayOfPreviousMonth.Year, lastDayOfPreviousMonth.Month, 1);
            return (firstDayOfPreviousMonth, lastDayOfPreviousMonth);
        }

        /// <summary>
        /// Calculates the date range for the Quarterly report type (previous full calendar quarter).
        /// </summary>
        public static (DateTime DateFrom, DateTime DateTo) CalculateQuarterlyRange(DateTime referenceDate)
        {
            int currentQuarter = (referenceDate.Month - 1) / 3 + 1;
            DateTime firstDayOfCurrentQuarter = new DateTime(referenceDate.Year, (currentQuarter - 1) * 3 + 1, 1);
            DateTime lastDayOfPreviousQuarter = firstDayOfCurrentQuarter.AddDays(-1);
            DateTime firstDayOfPreviousQuarter = lastDayOfPreviousQuarter.AddMonths(-3).AddDays(1);
            return (firstDayOfPreviousQuarter, lastDayOfPreviousQuarter);
=======
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
>>>>>>> parent of 171b8e4 (v1.9.2)
        }

        #endregion

<<<<<<< HEAD
        #region String and Help Content Helpers

        /// <summary>
        /// Generates the title for the Help window.
        /// </summary>
        /// <param name="appName">The name of the application.</param>
        /// <param name="appVersion">The version of the application.</param>
        /// <returns>A formatted title string.</returns>
        public static string GetHelpTitle(string appName, string appVersion)
        {
            return $"Help - {appName} v{appVersion}";
        }

        /// <summary>
        /// Loads, formats, and returns the rich text content for the Help window.
        /// </summary>
        /// <param name="configuration">The application configuration for reading settings.</param>
        /// <param name="appName">The name of the application.</param>
        /// <param name="appVersion">The version of the application.</param>
        /// <returns>A string containing the formatted RTF help content.</returns>
        public static string GetHelpContent(IConfiguration configuration, string appName, string appVersion)
        {
            bool isDarkMode = ThemeSettings.IsCurrentlyDark();
            string rtfFileName = isDarkMode ? "Help_Template_Dark.rtf" : "Help_Template_Light.rtf";
            string rtfFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", rtfFileName);
            string helpMessageRtf;

            if (File.Exists(rtfFilePath))
            {
                try
                {
                    helpMessageRtf = File.ReadAllText(rtfFilePath);
                }
                catch (Exception ex)
                {
                    Logger.LogError($"Error reading help file '{rtfFilePath}': {ex.Message}", ex);
                    return @"{\rtf1\ansi Oops! Could not load help content.}";
                }
            }
            else
            {
                return $@"{{ \rtf1\ansi Help file '{rtfFileName}' not found.}}";
            }

            // Replace placeholders with dynamic values from configuration
            var replacements = new Dictionary<string, string>
            {
                { "{APP_NAME}", appName },
                { "{APP_VERSION}", appVersion },
                { "{AUTO_RUN_HOUR}", configuration.GetValue<int>(AppConfigKeys.AutoRunProcess.CheckHour, 8).ToString() },
                { "{FINANCIAL_YEAR_START_DAY}", configuration.GetValue<int>(AppConfigKeys.OperationalParameters.FinancialYearStartDay, 1).ToString() },
                { "{FINANCIAL_YEAR_START_MONTH}", configuration.GetValue<int>(AppConfigKeys.OperationalParameters.FinancialYearStartMonth, 5).ToString() },
                { "{LOG_ARCHIVE_DAYS}", configuration.GetValue<int?>("Logging:LogArchiveOlderThanDays", 7)?.ToString() ?? "7" },
                { "{REPORT_ARCHIVE_FOLDER_NAME}", configuration.GetValue<string>(AppConfigKeys.OperationalParameters.ReportArchiveFolderName, "Archive") ?? "Archive" },
                { "{RAW_REPORTS_ARCHIVE_DAYS}", configuration.GetValue<int?>(AppConfigKeys.OperationalParameters.ArchiveRawReportsOlderThanDays, 30)?.ToString() ?? "30" }
            };

            var helpBuilder = new StringBuilder(helpMessageRtf);
            foreach (var replacement in replacements)
            {
                helpBuilder.Replace(replacement.Key, replacement.Value);
            }

            return helpBuilder.ToString();
        }

        /// <summary>
        /// Gets a string representation of the quarter for a given date (e.g., "Q1 2023").
        /// </summary>
        public static string GetQuarterString(DateTime date)
        {
            int quarter = (date.Month - 1) / 3 + 1;
            return $"Q{quarter} {date.Year}";
=======
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
>>>>>>> parent of 171b8e4 (v1.9.2)
        }

        #endregion

        #region File and Process Helpers

        /// <summary>
<<<<<<< HEAD
        /// Attempts to open the specified file using the default system application.
        /// </summary>
=======
        /// Opens the specified file using the default system application.
        /// Logs errors and shows a message box on failure.
        /// </summary>
        /// <param name="filePath">The full path to the file to open.</param>
        /// <param name="fileTypeDescription">A user-friendly description of the file type (e.g., "raw report output", "processed analysis file").</param>
>>>>>>> parent of 171b8e4 (v1.9.2)
        public static void OpenFileWithDefaultApp(string? filePath, string fileTypeDescription)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
<<<<<<< HEAD
                throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));
            }
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"{Capitalize(fileTypeDescription)} file was not found.", filePath);
            }
            try
            {
                Process.Start(new ProcessStartInfo(filePath) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                throw new Exception($"Could not open the {fileTypeDescription} file '{filePath}'.", ex);
=======
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
>>>>>>> parent of 171b8e4 (v1.9.2)
            }
        }

        /// <summary>
<<<<<<< HEAD
        /// Capitalizes the first letter of a given string.
        /// </summary>
        public static string Capitalize(string? text) => text switch
        {
            null => string.Empty,
            "" => string.Empty,
            _ => char.ToUpperInvariant(text[0]) + text.Substring(1)
        };

        /// <summary>
        /// Attempts to find and terminate all running processes with the specified name.
        /// </summary>
        public static void CloseProcessesByName(string processName)
        {
            if (string.IsNullOrWhiteSpace(processName)) return;
            try
            {
                foreach (var process in Process.GetProcessesByName(processName))
=======
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
>>>>>>> parent of 171b8e4 (v1.9.2)
                {
                    using (process)
                    {
<<<<<<< HEAD
                        if (!process.HasExited) process.Kill(true);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error during CloseProcessesByName for '{processName}': {ex.Message}", ex);
            }
=======
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
>>>>>>> parent of 171b8e4 (v1.9.2)
        }
        #endregion
    }
}
