// QuoteConversionReportAutomation/Services/Interfaces/IFormValidationService.cs

#region Using Directives

// System-related namespaces for core functionalities.
using System;
using System.Windows.Forms;

#endregion

namespace QuoteConversionReportAutomation.Services.Interfaces
{
    #region Interface Definition
    /// <summary>
    /// Defines the contract for a service that handles user input validation for the main application form.
    /// </summary>
    public interface IFormValidationService
    {
        #region Methods
        /// <summary>
        /// Validates that the selected start date is not after the end date.
        /// </summary>
        /// <param name="startDate">The start date selected by the user.</param>
        /// <param name="endDate">The end date selected by the user.</param>
        /// <param name="owner">The parent window that will own the message box if validation fails.</param>
        /// <returns>True if the date range is valid; otherwise, false.</returns>
        bool ValidateInputDates(DateTime startDate, DateTime endDate, IWin32Window owner);

        /// <summary>
        /// Validates that the selected date range aligns with the selected financial year, if applicable.
        /// Prompts the user for confirmation if there is a mismatch.
        /// </summary>
        /// <param name="isFinancialYearControlVisible">A flag indicating if the financial year selection is currently active for the selected report type.</param>
        /// <param name="selectedFinancialYear">The financial year string selected by the user (e.g., "2023_24").</param>
        /// <param name="startDate">The start date of the report period.</param>
        /// <param name="endDate">The end date of the report period.</param>
        /// <param name="owner">The parent window that will own the confirmation message box.</param>
        /// <returns>
        /// True if the selection is valid or if the user confirms to proceed despite a mismatch; otherwise, false.
        /// </returns>
        bool ValidateFinancialYearSelection(bool isFinancialYearControlVisible, string? selectedFinancialYear, DateTime startDate, DateTime endDate, IWin32Window owner);
        #endregion
    }
    #endregion
}