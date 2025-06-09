#region Using Directives
// System related namespaces
using System; // Required for DayOfWeek enum.
#endregion

namespace QuoteConversionReportAutomation.Models
{
    /// <summary>
    /// Defines the configuration for a single type of automated report.
    /// This includes how it's identified, enabled, its success tracked,
    /// and how its email notifications are constructed (recipients and greetings).
    /// </summary>
    public class AutoReportDefinition
    {
        /// <summary>
        /// Gets or sets the unique numeric index for the report type.
        /// This can be used for mapping to legacy systems or specific processing logic
        /// (e.g., selecting a particular Excel template or output folder structure).
        /// A value like -1 could indicate a fully custom report not tied to predefined indices.
        /// </summary>
        public int ReportTypeIndex { get; set; }

        /// <summary>
        /// Gets or sets a user-friendly, descriptive name for the report
        /// (e.g., "Standard Daily Estimate Success", "Weekly PowerBI Source").
        /// Used for logging and potentially in UI elements if these definitions were displayed.
        /// </summary>
        public string ReportName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the configuration key (path within `appsettings.json`, typically under the "AutoReport" section)
        /// that controls whether this specific automated report is enabled or disabled.
        /// Example: "EnableStandardDailyAutoReport".
        /// </summary>
        public string EnableConfigKey { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the JSON property name within the "AutoReport:DailyRunStatus" section of `appsettings.json`
        /// that is used to track whether this report has successfully run for the current day.
        /// Example: "StandardDailyReportSucceeded".
        /// </summary>
        public string SuccessFlagJsonName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the key used to retrieve the appropriate email greeting message
        /// from the `GreetingManager` (which checks user overrides and then `appsettings.json`).
        /// Example: "AutoRunDailyGreeting", "AutoRunWeeklyGreeting".
        /// </summary>
        public string GreetingKey { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the key used to identify the category of email recipients for this automated report.
        /// This key will be used by the <see cref="EmailRecipientManager"/> to look up the specific
        /// "To" and "CC" lists from `appsettings.json` (e.g., under `settings:ProductionEmails`)
        /// and any user overrides.
        /// Example: "AutoRunDailyRecipients", "AutoRunWeeklyMarketingRecipients".
        /// </summary>
        public string? RecipientCategoryKey { get; set; } // New property for Phase 2

        /// <summary>
        /// Gets or sets the prefix for the email subject line for this report.
        /// The date range or specific date will typically be appended to this prefix.
        /// Example: "Daily Estimate Success Rate", "Weekly Marketing Summary".
        /// </summary>
        public string SubjectPrefix { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the name of the Excel template file (e.g., "TEMPLATE_Estimate_Success_Rate.xlsx")
        /// to be used when processing this report. This file should exist in the configured template directory.
        /// </summary>
        public string TemplateName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the offset in working days from the current day to determine the report's end date.
        /// For example, an offset of 1 means the report ends on the previous working day.
        /// An offset of 0 could mean the report ends on the current day (if it's a workday).
        /// Null if the end date calculation is more complex or not based on a simple offset (e.g., fixed weekly run).
        /// </summary>
        public int? ReportEndDateOffsetDays { get; set; }

        /// <summary>
        /// Gets or sets the duration of the report in working days, ending on the calculated `ReportEndDate`.
        /// For example, a value of 1 indicates a single-day report. A value of 5 for a report ending on
        /// Friday would cover Monday to Friday.
        /// Null if the report duration is not defined by a simple number of days (e.g., a weekly report might have specific logic).
        /// </summary>
        public int? ReportDurationDays { get; set; }

        /// <summary>
        /// Gets or sets the specific day of the week on which this report should run, if applicable.
        /// If null, the report is assumed to be due on any day it's enabled and its success flag is not yet set for the day.
        /// Example: `DayOfWeek.Friday` for a report that only runs on Fridays.
        /// </summary>
        public DayOfWeek? RunOnDayOfWeek { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the special filtering logic for "Net Value >= £1000"
        /// (as implemented in <see cref="ExcelCopyData"/>) should be applied when processing this report.
        /// </summary>
        public bool RequiresNetValueFiltering { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this report's data should be appended
        /// to a central Power BI data source file (e.g., the weekly merged report).
        /// This flag helps the <see cref="ExcelCopyData"/> or <see cref="AutoRunManager"/>
        /// to trigger the correct data appending logic.
        /// </summary>
        public bool AppendToPowerBi { get; set; }

        // Future properties could be added here for more advanced configurations, e.g.:
        // public string? SpecificCrystalReportPath { get; set; } // To override the global Crystal Report path for this specific definition.
        // public Dictionary<string, string>? CustomReportParameters { get; set; } // For passing specific parameters to Crystal Reports.
    }
}
