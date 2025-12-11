// AutoReportDefinition.cs (Legacy EnableConfigKey Removed)
#region Using Directives
// System related namespaces
using System; // Required for DayOfWeek enum and Guid.
using Newtonsoft.Json; // Added for JsonProperty
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
        /// Gets or sets a unique identifier for this report definition.
        /// This is crucial for reliably updating and deleting specific definitions.
        /// Should be generated once (e.g., as a GUID string) when a definition is created.
        /// </summary>
        [JsonProperty("ReportId")]
        public string ReportId { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Gets or sets a value indicating whether this automated report definition is currently enabled.
        /// If false, the AutoRunManager will skip processing this report.
        /// </summary>
        [JsonProperty("IsEnabled")]
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// Gets or sets the unique numeric index for the report type.
        /// </summary>
        [JsonProperty("ReportTypeIndex")]
        public int ReportTypeIndex { get; set; }

        /// <summary>
        /// Gets or sets a user-friendly, descriptive name for the report.
        /// </summary>
        [JsonProperty("ReportName")]
        public string ReportName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the JSON property name used to track daily success in appsettings.json.
        /// </summary>
        [JsonProperty("SuccessFlagJsonName")]
        public string SuccessFlagJsonName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the key for retrieving email greeting messages.
        /// </summary>
        [JsonProperty("GreetingKey")]
        public string GreetingKey { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the key identifying the email recipient category.
        /// </summary>
        [JsonProperty("RecipientCategoryKey")]
        public string? RecipientCategoryKey { get; set; }

        /// <summary>
        /// Gets or sets the prefix for the email subject line.
        /// </summary>
        [JsonProperty("SubjectPrefix")]
        public string SubjectPrefix { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the name of the Excel template file.
        /// </summary>
        [JsonProperty("TemplateName")]
        public string TemplateName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the offset in working days for the report's end date.
        /// Nullable if not based on a simple offset.
        /// </summary>
        [JsonProperty("ReportEndDateOffsetDays")]
        public int? ReportEndDateOffsetDays { get; set; }

        /// <summary>
        /// Gets or sets the duration of the report in working days.
        /// Defaults to 1 if not specified.
        /// </summary>
        [JsonProperty("ReportDurationDays")]
        public int? ReportDurationDays { get; set; } = 1;

        /// <summary>
        /// Gets or sets the specific day of the week for the report to run.
        /// Null if the report runs daily (if enabled).
        /// </summary>
        [JsonProperty("RunOnDayOfWeek")]
        public DayOfWeek? RunOnDayOfWeek { get; set; }

        /// <summary>
        /// Gets or sets whether Net Value filtering (>= £1000) should be applied.
        /// </summary>
        [JsonProperty("RequiresNetValueFiltering")]
        public bool RequiresNetValueFiltering { get; set; }

        /// <summary>
        /// Gets or sets whether this report's data should be appended to a Power BI source file.
        /// </summary>
        [JsonProperty("AppendToPowerBi")]
        public bool AppendToPowerBi { get; set; }

        /// <summary>
        /// Gets or sets whether the "Lead Time Analysis" sheet should be included in the final report.
        /// This is configured for each automated report definition.
        /// </summary>
        [JsonProperty("IncludeLeadTimeAnalysis")]
        public bool IncludeLeadTimeAnalysis { get; set; }

        /// <summary>
        /// Returns the report name for display purposes.
        /// </summary>
        /// <returns>The report name or a default string if the name is not set.</returns>
        public override string ToString()
        {
            return ReportName ?? "[Unnamed Report Definition]";
        }
    }
}