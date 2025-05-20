// DailyReportRunStatus.cs
// Place this in a Models folder or an appropriate namespace
namespace QuoteConversionReportAutomation.Models
{
    using Microsoft.Extensions.Configuration;
    using System;

    /// <summary>
    /// Represents the success status of individual automated reports for a specific date.
    /// </summary>
    public class DailyReportRunStatus
    {
        /// <summary>
        /// The date for which these statuses are recorded (YYYY-MM-DD format).
        /// </summary>
        public string StatusDate { get; set; } = DateTime.MinValue.ToString("yyyy-MM-dd");

        /// <summary>
        /// Indicates if the Standard Daily Report succeeded for the StatusDate.
        /// </summary>
        public bool StandardDailyReportSucceeded { get; set; } = false;

        /// <summary>
        /// Indicates if the Daily (5days >= £1000) Report succeeded for the StatusDate.
        /// </summary>
        public bool Daily5Day1kReportSucceeded { get; set; } = false;

        // Add properties for other report types here if they become auto-runnable
        // e.g., public bool WeeklyReportSucceeded { get; set; } = false;

        /// <summary>
        /// Checks if all enabled reports (based on current configuration) have succeeded.
        /// This method would need access to IConfiguration to check which reports are currently enabled.
        /// For simplicity in AutoRunManager, we'll check enabled status there directly against these flags.
        /// </summary>
        public bool AllCurrentlyEnabledReportsSucceeded(IConfiguration config)
        {
            bool standardDailyEnabled = config.GetValue<bool>("AutoReport:EnableStandardDailyAutoReport", true);
            bool daily5Day1kEnabled = config.GetValue<bool>("AutoReport:EnableDaily5Day1kAutoReport", true);

            if (standardDailyEnabled && !StandardDailyReportSucceeded) return false;
            if (daily5Day1kEnabled && !Daily5Day1kReportSucceeded) return false;

            // Add checks for other reports if they are tracked
            // if (config.GetValue<bool>("AutoReport:EnableWeeklyReport", false) && !WeeklyReportSucceeded) return false;

            return true; // All enabled and tracked reports have succeeded
        }
    }
}
