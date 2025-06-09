// ReportHelper.cs
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
            return (dateFrom, dateTo);
        }

        /// <summary>
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
            }
            return previousDay.Date;
        }

        /// <summary>
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
            return resultDate;
        }

        /// <summary>
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
        }
        #endregion

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
        }
        #endregion

        #region File and Process Helpers
        /// <summary>
        /// Attempts to open the specified file using the default system application.
        /// </summary>
        public static void OpenFileWithDefaultApp(string? filePath, string fileTypeDescription)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
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
            }
        }

        /// <summary>
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
                {
                    using (process)
                    {
                        if (!process.HasExited) process.Kill(true);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error during CloseProcessesByName for '{processName}': {ex.Message}", ex);
            }
        }
        #endregion
    }
}