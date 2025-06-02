// Models/AutoRunActionResult.cs
namespace QuoteConversionReportAutomation.Models
{
    /// <summary>
    /// Enum to represent the outcome of an automated run check.
    /// </summary>
    public enum AutoRunActionResult
    {
        /// <summary>Indicates no action was needed or taken during the check.</summary>
        NoActionNeeded,
        /// <summary>Indicates that at least one automated report processing was attempted.</summary>
        ActionAttempted,
        /// <summary>Indicates a critical error occurred during the auto-run process.</summary>
        CriticalError
    }
}
