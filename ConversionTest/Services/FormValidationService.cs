#region Using Directives

// System-related namespaces for core functionalities.
using System;
using System.Windows.Forms;

// Project-specific namespaces for application components.
using QuoteConversionReportAutomation.Helpers;
using QuoteConversionReportAutomation.Services.Interfaces;
using QuoteConversionReportAutomation.Services.Logging;

#endregion

namespace QuoteConversionReportAutomation.Services
{
    #region Class Definition
    /// <summary>
    /// Implements the <see cref="IFormValidationService"/> to provide concrete methods
    /// for validating user input from the main application form.
    /// </summary>
    public class FormValidationService : IFormValidationService
    {
        #region Fields

        /// <summary>
        /// The service responsible for financial year calculations, used for validation.
        /// </summary>
        private readonly IFinancialYearService _financialYearService;

        #endregion

        #region Constructor

        /// <summary>
        /// Initialises a new instance of the <see cref="FormValidationService"/> class.
        /// </summary>
        /// <param name="financialYearService">The service for financial year logic, injected via dependency injection.</param>
        public FormValidationService(IFinancialYearService financialYearService)
        {
            _financialYearService = financialYearService ?? throw new ArgumentNullException(nameof(financialYearService));
            Logger.LogTrace("FormValidationService instance created.");
        }

        #endregion

        #region IFormValidationService Implementation

        /// <inheritdoc/>
        public bool ValidateInputDates(DateTime startDate, DateTime endDate, IWin32Window owner)
        {
            // Check if the start date is after the end date.
            if (startDate.Date > endDate.Date)
            {
                // If the date range is invalid, show an error message to the user.
                FlexibleMessageBox.Show(owner, "The 'From' date cannot be after the 'To' date.",
                                        "Date Range Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false; // Validation fails.
            }
            // If the dates are valid, return true.
            return true;
        }

        /// <inheritdoc/>
        public bool ValidateFinancialYearSelection(bool isFinancialYearControlVisible, string? selectedFinancialYear, DateTime startDate, DateTime endDate, IWin32Window owner)
        {
            // If the financial year control is not visible or no year is selected, no validation is needed.
            if (!isFinancialYearControlVisible || string.IsNullOrWhiteSpace(selectedFinancialYear))
            {
                return true;
            }

            // Use the injected financial year service to check if the date range fits within the selected financial year.
            if (!_financialYearService.IsFinancialYearValid(selectedFinancialYear, startDate, endDate))
            {
                // If there is a mismatch, ask the user if they want to proceed anyway.
                DialogResult fdr = FlexibleMessageBox.Show(owner, $"Date range ({startDate:d} - {endDate:d}) does not fall within the selected Financial Year ({selectedFinancialYear}).\n\nDo you want to continue?",
                                                            "Financial Year Mismatch Warning", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

                // Validation passes only if the user explicitly clicks "Yes".
                return fdr == DialogResult.Yes;
            }

            // If the dates are valid for the financial year, validation passes.
            return true;
        }

        #endregion
    }
    #endregion
}