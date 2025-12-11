// QuoteConversionReportAutomation/Helpers/ReportHelper.cs

#region Using Directives

// System-related namespaces for core functionalities.
using System;

// Third-party namespaces for external libraries.
using Microsoft.Extensions.Configuration;

// Project-specific namespaces for application components.
using QuoteConversionReportAutomation.Configuration;
using QuoteConversionReportAutomation.Models;

#endregion

namespace QuoteConversionReportAutomation.Helpers
{
    #region Static Class Definition
    /// <summary>
    /// Provides static helper methods for various report-related calculations and formatting,
    /// focusing primarily on date logic such as determining workdays, financial years, and report periods.
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
            // Retrieve the financial year start month and day from configuration, with defaults.
            int startMonth = configuration.GetValue<int>(AppConfigKeys.OperationalParameters.FinancialYearStartMonth, 5);
            int startDay = configuration.GetValue<int>(AppConfigKeys.OperationalParameters.FinancialYearStartDay, 1);

            // If the reference date is before this year's financial start date, then the financial year started last year.
            if (referenceDate.Month < startMonth || (referenceDate.Month == startMonth && referenceDate.Day < startDay))
            {
                return referenceDate.Year - 1;
            }

            // Otherwise, the financial year started in the current calendar year.
            return referenceDate.Year;
        }

        /// <summary>
        /// Calculates the start and end dates of a financial year based on its starting calendar year.
        /// </summary>
        /// <param name="financialYearStartCalendarYear">The calendar year in which the financial year starts.</param>
        /// <param name="configuration">The application configuration to source the start month and day from.</param>
        /// <returns>A tuple containing the start and end date of the specified financial year.</returns>
        public static (DateTime DateFrom, DateTime DateTo) GetFinancialYearDates(int financialYearStartCalendarYear, IConfiguration configuration)
        {
            // Retrieve the specific start month and day from configuration.
            int startMonth = configuration.GetValue<int>(AppConfigKeys.OperationalParameters.FinancialYearStartMonth, 5);
            int startDay = configuration.GetValue<int>(AppConfigKeys.OperationalParameters.FinancialYearStartDay, 1);

            // Construct the start date.
            DateTime dateFrom = new DateTime(financialYearStartCalendarYear, startMonth, startDay);
            // The end date is one year later, minus one day.
            DateTime dateTo = dateFrom.AddYears(1).AddDays(-1);

            return (dateFrom, dateTo);
        }

        /// <summary>
        /// Calculates the previous working day from a given date, skipping weekends and bank holidays.
        /// </summary>
        /// <param name="currentDate">The date from which to calculate the previous workday.</param>
        /// <returns>A <see cref="DateTime"/> object representing the previous working day.</returns>
        public static DateTime GetPreviousWorkday(DateTime currentDate)
        {
            DateTime previousDay = currentDate.AddDays(-1);
            // Keep moving back one day at a time until the day is not a Saturday, Sunday, or a bank holiday.
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
            // Call GetPreviousWorkday() 'n' times to find the target date.
            for (int i = 0; i < nWorkdaysBack; i++)
            {
                resultDate = GetPreviousWorkday(resultDate);
            }
            return resultDate;
        }

        /// <summary>
        /// Calculates the standard start and end date for a given report type based on a reference date.
        /// This centralises date logic to ensure consistency across automated and manual runs.
        /// </summary>
        /// <param name="reportType">The type of report.</param>
        /// <param name="referenceDate">The date to calculate the period from (e.g., today's date).</param>
        /// <param name="configuration">Application configuration, required for financial year calculations.</param>
        /// <returns>A tuple containing the calculated StartDate and EndDate.</returns>
        public static (DateTime StartDate, DateTime EndDate) GetReportDateRange(ReportType reportType, DateTime referenceDate, IConfiguration configuration)
        {
            // Use a switch expression for a concise way to handle the different report types.
            return reportType switch
            {
                ReportType.Daily => (referenceDate.Date, referenceDate.Date),
                ReportType.Daily5Day1k => (GetNthPreviousWorkday(referenceDate.Date, 4), referenceDate.Date),
                ReportType.Weekly => CalculateWeeklyRange(referenceDate.Date),
                ReportType.Monthly => CalculateMonthlyRange(referenceDate.Date),
                ReportType.Quarterly => CalculateQuarterlyRange(referenceDate.Date),
                ReportType.Annual => GetFinancialYearDates(GetFinancialYearStartCalendarYear(referenceDate, configuration) - 1, configuration),
                // For custom or unknown types, the range is determined by user input, not calculation.
                _ => (referenceDate.Date, referenceDate.Date)
            };
        }

        /// <summary>
        /// Calculates the date range for the Weekly report type (a 14-day period ending on the most recent Friday).
        /// </summary>
        private static (DateTime DateFrom, DateTime DateTo) CalculateWeeklyRange(DateTime referenceDate)
        {
            DateTime endDate = referenceDate;
            // Find the most recent Friday on or before the reference date.
            while (endDate.DayOfWeek != DayOfWeek.Friday)
            {
                endDate = endDate.AddDays(-1);
            }
            // The start date is 14 days before the calculated end date.
            DateTime startDate = endDate.AddDays(-14);
            return (startDate, endDate);
        }

        /// <summary>
        /// Calculates the date range for the Monthly report type (the previous full calendar month).
        /// </summary>
        public static (DateTime DateFrom, DateTime DateTo) CalculateMonthlyRange(DateTime referenceDate)
        {
            DateTime firstDayOfCurrentMonth = new DateTime(referenceDate.Year, referenceDate.Month, 1);
            DateTime lastDayOfPreviousMonth = firstDayOfCurrentMonth.AddDays(-1);
            DateTime firstDayOfPreviousMonth = new DateTime(lastDayOfPreviousMonth.Year, lastDayOfPreviousMonth.Month, 1);
            return (firstDayOfPreviousMonth, lastDayOfPreviousMonth);
        }

        /// <summary>
        /// Calculates the date range for the Quarterly report type (the previous full calendar quarter).
        /// </summary>
        public static (DateTime DateFrom, DateTime DateTo) CalculateQuarterlyRange(DateTime referenceDate)
        {
            int currentQuarter = (referenceDate.Month - 1) / 3 + 1;
            DateTime firstDayOfCurrentQuarter = new DateTime(referenceDate.Year, (currentQuarter - 1) * 3 + 1, 1);
            DateTime lastDayOfPreviousQuarter = firstDayOfCurrentQuarter.AddDays(-1);
            // The first day of the previous quarter is 3 months before its last day, plus one day.
            DateTime firstDayOfPreviousQuarter = lastDayOfPreviousQuarter.AddMonths(-3).AddDays(1);
            return (firstDayOfPreviousQuarter, lastDayOfPreviousQuarter);
        }
        #endregion

        #region String Formatting Helpers
        /// <summary>
        /// Gets a string representation of the quarter for a given date (e.g., "Q1 2023").
        /// </summary>
        public static string GetQuarterString(DateTime date)
        {
            int quarter = (date.Month - 1) / 3 + 1;
            return $"Q{quarter} {date.Year}";
        }
        #endregion
    }
    #endregion
}