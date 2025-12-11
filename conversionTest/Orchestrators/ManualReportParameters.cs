// ManualReportParameters.cs
// Defines the data transfer object for parameters required by the ManualReportOrchestrator.

using System;
using QuoteConversionReportAutomation.Models; // For ReportType enum

namespace QuoteConversionReportAutomation.Orchestrators
{
    /// <summary>
    /// Represents the input parameters for initiating a manual report generation and processing workflow.
    /// </summary>
    public record ManualReportParameters
    {
        /// <summary>
        /// Gets or sets the start date for the report period.
        /// </summary>
        public DateTime StartDate { get; init; }

        /// <summary>
        /// Gets or sets the end date for the report period.
        /// </summary>
        public DateTime EndDate { get; init; }

        /// <summary>
        /// Gets or sets the type of report to be generated.
        /// Uses the <see cref="ReportType"/> enum for type safety.
        /// </summary>
        public ReportType ReportType { get; init; }

        /// <summary>
        /// Gets or sets the selected financial year string (e.g., "2023_24").
        /// This is relevant for certain report types, particularly weekly or custom reports
        /// that might need to be associated with a specific financial year for Power BI or analysis.
        /// Can be null if not applicable for the report type.
        /// </summary>
        public string? FinancialYear { get; init; }

        /// <summary>
        /// Gets or sets a value indicating whether the email should be sent only to a restricted list (e.g., "Femi only").
        /// Relevant for manual non-daily/non-custom reports.
        /// </summary>
        public bool IsFemiOnlyChecked { get; init; }

        /// <summary>
        /// Gets or sets a value indicating whether the email sending step should be skipped.
        /// </summary>
        public bool SkipEmail { get; init; }

        /// <summary>
        /// Gets or sets a value indicating whether to include lead time analysis in the report.
        /// </summary>
        public bool IncludeLeadTimeAnalysis { get; init; } // Add this property

        /// <summary>
        /// Gets or sets the name of the report, used for file naming and logging.
        /// For example, "EstimateSuccessReport".
        /// </summary>
        public string ReportBaseName { get; init; } = "EstimateSuccessReport";

        /// <summary>
        /// Gets or sets a value indicating whether the current build is a DEBUG build.
        /// This affects email recipient determination.
        /// </summary>
        public bool IsDebugBuild { get; init; }
    }
}
