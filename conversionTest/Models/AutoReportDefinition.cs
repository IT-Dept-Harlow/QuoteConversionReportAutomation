namespace QuoteConversionReportAutomation.Models
{
    public class AutoReportDefinition
    {
        /// <summary>
        /// Unique numeric index for the report type (matches constants in Form1/ExcelCopyData if needed for legacy compatibility).
        /// </summary>
        public int ReportTypeIndex { get; set; }

        /// <summary>
        /// A descriptive name for the report (e.g., "Standard Daily", "Weekly PowerBI").
        /// </summary>
        public string ReportName { get; set; } = string.Empty;

        /// <summary>
        /// The configuration key in appsettings.json (under "AutoReport" section) to enable/disable this report.
        /// Example: "EnableStandardDailyAutoReport".
        /// </summary>
        public string EnableConfigKey { get; set; } = string.Empty;

        /// <summary>
        /// The JSON property name within "DailyRunStatus" used to track the success of this report for the current day.
        /// Example: "StandardDailyReportSucceeded".
        /// </summary>
        public string SuccessFlagJsonName { get; set; } = string.Empty;

        /// <summary>
        /// The key used to retrieve the email greeting for this automated report.
        /// Example: "AutoRunDaily".
        /// </summary>
        public string GreetingKey { get; set; } = string.Empty;

        /// <summary>
        /// The prefix for the email subject line for this report.
        /// Example: "Daily Estimate Success Rate".
        /// </summary>
        public string SubjectPrefix { get; set; } = string.Empty;

        /// <summary>
        /// The name of the Excel template file to be used.
        /// Example: "TEMPLATE_Estimate Success Rate.xlsx".
        /// </summary>
        public string TemplateName { get; set; } = string.Empty;

        /// <summary>
        /// For daily-type reports, specifies the offset from the current day to determine the report's end date.
        /// 0 = previous workday. 4 = 5th previous workday (for a 5-day report ending on the previous workday).
        /// Null if not applicable or if date calculation is more complex.
        /// </summary>
        public int? ReportEndDateOffsetDays { get; set; }

        /// <summary>
        /// For reports spanning multiple days, specifies the number of days the report should cover, ending on the calculated reportEndDate.
        /// Example: 1 for a single day report, 5 for a 5-day report.
        /// Null if not applicable (e.g. weekly report has specific logic).
        /// </summary>
        public int? ReportDurationDays { get; set; }

        /// <summary>
        /// Specific day of the week this report should run on (if applicable).
        /// Null if it runs on any day it's due.
        /// </summary>
        public DayOfWeek? RunOnDayOfWeek { get; set; }

        /// <summary>
        /// Indicates if the special filtering logic for "Net Value >= 1000" in ExcelCopyData should be applied.
        /// </summary>
        public bool RequiresNetValueFiltering { get; set; }

        /// <summary>
        /// Indicates if this report's data should be appended to the Power BI weekly file.
        /// (This flag will be used by ExcelCopyData if it's refactored, or by AutoRunManager to call the correct ExcelCopyData method/parameters).
        /// </summary>
        public bool AppendToPowerBi { get; set; }

        // Future properties can be added here, e.g.:
        // public string SpecificCrystalReportPath { get; set; }
        // public Dictionary<string, string> CustomReportParameters { get; set; }
    }
}
