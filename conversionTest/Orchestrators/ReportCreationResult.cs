using QuoteConversionReportAutomation.Helpers;

namespace QuoteConversionReportAutomation.Orchestrators
{
    /// <summary>
    /// Represents the result of a raw report creation operation.
    /// </summary>
    public record ReportCreationResult
    {
        /// <summary>
        /// Gets a value indicating whether the raw report creation was successful.
        /// </summary>
        public bool Success { get; init; }

        /// <summary>
        /// Gets the error message if the creation failed. Null if successful.
        /// </summary>
        public string? ErrorMessage { get; init; }

        /// <summary>
        /// Gets the full path to the generated raw report file if creation was successful. Null otherwise.
        /// </summary>
        public string? GeneratedRawPath { get; init; }

        /// <summary>
        /// Factory method to create a successful result.
        /// </summary>
        /// <param name="generatedRawPath">The path to the generated raw report file.</param>
        /// <returns>A successful <see cref="ReportCreationResult"/> instance.</returns>
        public static ReportCreationResult SuccessResult(string generatedRawPath) =>
            new ReportCreationResult { Success = true, GeneratedRawPath = generatedRawPath, ErrorMessage = null };

        /// <summary>
        /// Factory method to create a failed result.
        /// </summary>
        /// <param name="errorMessage">The error message describing the failure.</param>
        /// <returns>A failed <see cref="ReportCreationResult"/> instance.</returns>
        public static ReportCreationResult FailureResult(string errorMessage) =>
            new ReportCreationResult { Success = false, GeneratedRawPath = null, ErrorMessage = errorMessage };

        /// <summary>
        /// Factory method to create a successful result.
        /// </summary>
        /// <param name="generatedRawPath">The path to the generated raw report file.</param>
        /// <param name="errorMessage">The error message describing the success.</param>
        /// <returns>A successful <see cref="ReportCreationResult"/> instance.</returns>
        public static ReportCreationResult CreateSuccess(string generatedRawPath, string errorMessage)
        {
            return new ReportCreationResult
            {
                Success = true,
                GeneratedRawPath = generatedRawPath,
                ErrorMessage = null
            };
        }

        /// <summary>
        /// Factory method to create a failed result.
        /// </summary>
        /// <param name="generatedRawPath">The path to the generated raw report file.</param>
        /// <param name="errorMessage">The error message describing the failure.</param>
        /// <returns>A failed <see cref="ReportCreationResult"/> instance.</returns>
        public static ReportCreationResult CreateFailure(string generatedRawPath, string errorMessage)
        {
            return new ReportCreationResult
            {
                Success = false,
                GeneratedRawPath = null,
                ErrorMessage = errorMessage
            };
        }
    }
}