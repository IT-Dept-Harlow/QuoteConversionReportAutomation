#region Using Directives

// System-related namespaces for core functionalities.
using System;

// Third-party namespaces for external libraries.
using Microsoft.Extensions.Configuration;

// Project-specific namespaces for application components.
using QuoteConversionReportAutomation.Helpers;
using QuoteConversionReportAutomation.Models;
using QuoteConversionReportAutomation.Services.Interfaces;
using QuoteConversionReportAutomation.Services.Logging;

#endregion

namespace QuoteConversionReportAutomation.Services
{
    #region Class Definition
    /// <summary>
    /// Implements the <see cref="IReportPeriodService"/> to provide concrete logic
    /// for calculating the start and end dates for predefined report periods.
    /// </summary>
    public class ReportPeriodService : IReportPeriodService
    {
        #region Fields

        /// <summary>
        /// Provides read-only access to the application's configuration settings.
        /// </summary>
        private readonly IConfiguration _configuration;

        #endregion

        #region Constructor

        /// <summary>
        /// Initialises a new instance of the <see cref="ReportPeriodService"/> class.
        /// </summary>
        /// <param name="configuration">
        /// The application's configuration settings, injected via dependency injection.
        /// This is required for calculations that depend on the financial year definition.
        /// </param>
        public ReportPeriodService(IConfiguration configuration)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            Logger.LogTrace("ReportPeriodService instance created.");
        }

        #endregion

        #region IReportPeriodService Implementation

        /// <inheritdoc/>
        public (DateTime StartDate, DateTime EndDate) GetPeriodForReportType(ReportType reportType)
        {
            // Get the current date to use as a reference for all calculations.
            DateTime today = DateTime.Today;

            // Use a switch expression to determine the date range based on the report type.
            // This logic was previously located inside the Form1.reportTypeComboBox_SelectedIndexChanged event handler.
            (DateTime dateFrom, DateTime dateTo) = reportType switch
            {
                // For a standard Daily report, the period is the previous working day.
                ReportType.Daily => (ReportHelper.GetPreviousWorkday(today), ReportHelper.GetPreviousWorkday(today)),

                // For a Daily5Day1k report, the period is the last 5 working days.
                // It ends on the previous working day and goes back 4 more working days from there.
                ReportType.Daily5Day1k => (ReportHelper.GetNthPreviousWorkday(ReportHelper.GetPreviousWorkday(today), 4), ReportHelper.GetPreviousWorkday(today)),

                // For a Weekly report, the period is a rolling 14 days ending on the most recent Friday.
                // The ReportHelper contains the logic to find this period.
                ReportType.Weekly => ReportHelper.GetReportDateRange(ReportType.Weekly, today, _configuration),

                // For a Monthly report, the period is the entire previous calendar month.
                ReportType.Monthly => ReportHelper.CalculateMonthlyRange(today),

                // For a Quarterly report, the period is the entire previous calendar quarter.
                ReportType.Quarterly => ReportHelper.CalculateQuarterlyRange(today),

                // For an Annual report, the period is the previous full financial year.
                ReportType.Annual => ReportHelper.GetFinancialYearDates(ReportHelper.GetFinancialYearStartCalendarYear(today, _configuration) - 1, _configuration),

                // For Custom or Unknown types, return a default range (e.g., today).
                // The UI will typically override this with user-selected dates.
                _ => (today, today)
            };

            // Return the calculated start and end dates as a tuple.
            return (dateFrom, dateTo);
        }

        #endregion
    }
    #endregion
}