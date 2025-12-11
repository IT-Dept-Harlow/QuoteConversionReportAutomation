#region Using Directives
using System;
#endregion

namespace QuoteConversionReportAutomation.Services.Interfaces
{
    #region Interface Definition
    /// <summary>
    /// Defines the contract for a service that provides business logic
    /// and calculations related to financial years.
    /// </summary>
    public interface IFinancialYearService
    {
        #region Methods
        /// <summary>
        /// Gets the current financial year as a formatted string based on the current date.
        /// </summary>
        /// <param name="useUnderscoreFormat">
        /// If true, returns the financial year in "YYYY_YY" format (e.g., "2023_24").
        /// If false, returns the format "FY YY/YY" (e.g., "FY 23/24").
        /// </param>
        /// <returns>A string representing the current financial year.</returns>
        string GetCurrentFinancialYear(bool useUnderscoreFormat = false);

        /// <summary>
        /// Calculates the previous financial year string from a given financial year string.
        /// </summary>
        /// <param name="currentFinancialYearUnderscore">
        /// The current financial year in the "YYYY_YY" format (e.g., "2023_24").
        /// </param>
        /// <returns>
        /// A string representing the previous financial year in the "YYYY_YY" format,
        /// or null if the input format is invalid.
        /// </returns>
        string? GetPreviousFinancialYear(string currentFinancialYearUnderscore);

        /// <summary>
        /// Validates if a given date range falls entirely within a specified financial year.
        /// </summary>
        /// <param name="selectedFinYearUnderscore">
        /// The financial year to validate against, in "YYYY_YY" format (e.g., "2023_24").
        /// </param>
        /// <param name="fromDate">The start date of the range to check.</param>
        /// <param name="toDate">The end date of the range to check.</param>
        /// <returns>True if the date range is valid for the specified financial year; otherwise, false.</returns>
        bool IsFinancialYearValid(string selectedFinYearUnderscore, DateTime fromDate, DateTime toDate);
        #endregion
    }
    #endregion
}