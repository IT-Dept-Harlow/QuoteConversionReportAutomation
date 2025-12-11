#region Using Directives

// System-related namespaces for core functionalities.
using System;

// Project-specific namespaces for application components.
using QuoteConversionReportAutomation.Models;

#endregion

namespace QuoteConversionReportAutomation.Services.Interfaces
{
    #region Interface Definition
    /// <summary>
    /// Defines the contract for a service responsible for calculating the
    /// start and end dates for predefined report periods.
    /// </summary>
    public interface IReportPeriodService
    {
        #region Methods
        /// <summary>
        /// Calculates the start and end dates for a given report type based on the current date.
        /// </summary>
        /// <param name="reportType">The <see cref="ReportType"/> for which to calculate the period.</param>
        /// <returns>
        /// A tuple containing the <see cref="DateTime"/> for the StartDate and EndDate of the report period.
        /// For custom or unknown types, it may return a default range.
        /// </returns>
        (DateTime StartDate, DateTime EndDate) GetPeriodForReportType(ReportType reportType);
        #endregion
    }
    #endregion
}