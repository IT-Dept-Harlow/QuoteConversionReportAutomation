// ReportProcessingResult.cs
// Defines the data transfer object for the result of processing and emailing a report.

using QuoteConversionReportAutomation.Services.Communication; // For EmailSendResult

namespace QuoteConversionReportAutomation.Orchestrators
{
    /// <summary>
    /// Represents the result of a report processing and emailing operation.
    /// </summary>
    public record ReportProcessingResult
    {
        /// <summary>
        /// Gets a value indicating whether the Excel processing part was successful.
        /// </summary>
        public bool Success { get; init; }

        /// <summary>
        /// Gets the error message if processing failed. Null if successful.
        /// </summary>
        public string? ErrorMessage { get; init; }

        /// <summary>
        /// Gets the full path to the generated final analysis file if processing was successful. Null otherwise.
        /// </summary>
        public string? GeneratedAnalysisPath { get; init; }

        /// <summary>
        /// Gets a value indicating whether a manual refresh of the Excel file is required by the user
        /// before the email can be sent (if not skipped).
        /// </summary>
        public bool ManualRefreshRequired { get; init; }

        /// <summary>
        /// Gets the result of the email sending operation. Null if email was skipped, if processing failed before email attempt,
        /// or if manual refresh is required and email sending is deferred.
        /// </summary>
        public EmailSendResult? EmailResult { get; init; }

        /// <summary>
        /// Prevents direct instantiation. Use factory methods.
        /// </summary>
        private ReportProcessingResult() { }

        /// <summary>
        /// Factory method to create a successful result.
        /// </summary>
        /// <param name="generatedAnalysisPath">The path to the generated analysis file.</param>
        /// <param name="emailResult">The result of the email sending operation (can be null if email was skipped or deferred).</param>
        /// <param name="manualRefreshRequired">True if manual Excel refresh is required before potential emailing.</param>
        /// <returns>A successful <see cref="ReportProcessingResult"/> instance.</returns>
        public static ReportProcessingResult SuccessResult(string generatedAnalysisPath, EmailSendResult? emailResult = null, bool manualRefreshRequired = false) =>
            new ReportProcessingResult
            {
                Success = true,
                GeneratedAnalysisPath = generatedAnalysisPath,
                EmailResult = emailResult,
                ManualRefreshRequired = manualRefreshRequired,
                ErrorMessage = emailResult is { Success: false } ? emailResult.ErrorMessage : null
            };

        /// <summary>
        /// Factory method to create a failed result.
        /// </summary>
        /// <param name="errorMessage">The error message describing the failure.</param>
        /// <param name="generatedAnalysisPath">Optional. The path to the analysis file if processing reached that stage before failing.</param>
        /// <param name="emailResult">Optional email send result if the failure occurred during or after an email attempt.</param>
        /// <returns>A failed <see cref="ReportProcessingResult"/> instance.</returns>
        public static ReportProcessingResult FailureResult(string errorMessage, string? generatedAnalysisPath = null, EmailSendResult? emailResult = null) =>
            new ReportProcessingResult
            {
                Success = false,
                ErrorMessage = errorMessage,
                GeneratedAnalysisPath = generatedAnalysisPath, // Can be set if Excel processing partially succeeded but email failed
                EmailResult = emailResult,
                ManualRefreshRequired = false // Typically false on failure, but could be true if failure happened after identifying refresh need
            };
    }
}
