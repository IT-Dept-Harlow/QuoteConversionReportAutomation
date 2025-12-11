#region Using Directives

// System-related namespaces for core functionalities.
using System;

// Third-party namespaces for external libraries.
using Microsoft.Extensions.Configuration;

// Project-specific namespaces for application components.
using QuoteConversionReportAutomation.Services.Interfaces;
using QuoteConversionReportAutomation.Services.Logging;

#endregion

namespace QuoteConversionReportAutomation.Services
{
    #region Class Definition
    /// <summary>
    /// Implements the <see cref="IFinancialYearService"/> to provide concrete business logic
    /// for financial year calculations based on application configuration.
    /// </summary>
    public class FinancialYearService : IFinancialYearService
    {
        #region Fields

        /// <summary>
        /// Provides read-only access to the application's configuration settings
        /// from sources like 'appsettings.json'.
        /// </summary>
        private readonly IConfiguration _configuration;

        #endregion

        #region Constructor

        /// <summary>
        /// Initialises a new instance of the <see cref="FinancialYearService"/> class.
        /// </summary>
        /// <param name="configuration">
        /// The application's configuration settings, injected via dependency injection.
        /// Used to retrieve the financial year start month and day.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown if the <paramref name="configuration"/> is null.
        /// </exception>
        public FinancialYearService(IConfiguration configuration)
        {
            // Assign the injected configuration to the private field.
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            Logger.LogTrace("FinancialYearService instance created.");
        }

        #endregion

        #region IFinancialYearService Implementation

        /// <inheritdoc/>
        public string GetCurrentFinancialYear(bool useUnderscoreFormat = false)
        {
            // Log the entry into the method for tracing purposes.
            Logger.LogTrace($"Entering GetCurrentFinancialYear(useUnderscoreFormat: {useUnderscoreFormat})");

            // Get the current system date.
            DateTime today = DateTime.Today;

            // Retrieve the financial year start month and day from the application configuration.
            // Default to May 1st if the configuration keys are not found.
            int finYearStartMonth = _configuration.GetValue<int>("OperationalParameters:FinancialYearStartMonth", 5);
            int finYearStartDay = _configuration.GetValue<int>("OperationalParameters:FinancialYearStartDay", 1);

            // Determine the calendar year in which the current financial year began.
            // If today's date is before the financial year start date, the financial year started last year.
            int startYear = (today.Month > finYearStartMonth || (today.Month == finYearStartMonth && today.Day >= finYearStartDay))
                            ? today.Year
                            : today.Year - 1;

            // The financial year ends one year after it starts.
            int endYear = startYear + 1;

            // Format the result string based on the 'useUnderscoreFormat' parameter.
            string result = useUnderscoreFormat
                ? $"{startYear}_{endYear.ToString().Substring(2, 2)}" // e.g., "2023_24"
                : $"FY {startYear.ToString().Substring(2, 2)}/{endYear.ToString().Substring(2, 2)}"; // e.g., "FY 23/24"

            Logger.LogTrace($"Exiting GetCurrentFinancialYear. Result: {result}");
            return result;
        }

        /// <inheritdoc/>
        public string? GetPreviousFinancialYear(string currentFinancialYearUnderscore)
        {
            // Return null immediately if the input string is invalid.
            if (string.IsNullOrWhiteSpace(currentFinancialYearUnderscore))
            {
                return null;
            }

            // Split the input string by the underscore to separate the years.
            string[] parts = currentFinancialYearUnderscore.Split('_');

            // Check if the split resulted in two parts and the first part is a valid integer (year).
            if (parts.Length == 2 && int.TryParse(parts[0], out int startYear))
            {
                // Calculate the start year of the previous financial year.
                int prevStartYear = startYear - 1;
                // Construct the previous financial year string in the same "YYYY_YY" format.
                return $"{prevStartYear}_{startYear.ToString().Substring(2, 2)}";
            }

            // If the input format is incorrect, return null.
            return null;
        }

        /// <inheritdoc/>
        public bool IsFinancialYearValid(string selectedFinYearUnderscore, DateTime fromDate, DateTime toDate)
        {
            // Return false immediately if the input financial year string is invalid.
            if (string.IsNullOrWhiteSpace(selectedFinYearUnderscore))
            {
                return false;
            }

            // Split the financial year string to extract the start year.
            string[] parts = selectedFinYearUnderscore.Split('_');

            // Check if the format is correct and the start year is a valid number.
            if (parts.Length == 2 && int.TryParse(parts[0], out int startYear))
            {
                // Retrieve the financial year start month and day from configuration.
                int finYearStartMonth = _configuration.GetValue<int>("OperationalParameters:FinancialYearStartMonth", 5);
                int finYearStartDay = _configuration.GetValue<int>("OperationalParameters:FinancialYearStartDay", 1);

                // Calculate the exact start and end dates of the specified financial year.
                DateTime fyStartDate = new DateTime(startYear, finYearStartMonth, finYearStartDay);
                DateTime fyEndDate = fyStartDate.AddYears(1).AddDays(-1);

                // Check if the provided date range falls completely within the financial year.
                return fromDate >= fyStartDate && toDate <= fyEndDate;
            }

            // If the input format is incorrect, it's not a valid financial year.
            return false;
        }

        #endregion
    }
    #endregion
}