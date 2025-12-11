// UserEmailSettings.cs (Legacy Code Removed)
// Defines the structure for user-specific overrides of email recipient lists.
// This allows users to customise who receives reports for various scenarios,
// including both manual and automated report runs using RecipientCategoryKey.
// Utilises C# 10+ features.

#region Using Directives
using System.Collections.Generic;
using System.Text.Json.Serialization; // For JsonPropertyName attribute.
#endregion

namespace QuoteConversionReportAutomation.Models
{
    /// <summary>
    /// Represents user-defined email recipient settings that can override application defaults.
    /// Includes settings for various production scenarios (manual and automated runs) and debug configurations.
    /// The lists store email addresses; 'To' and 'CC' are typically managed separately.
    /// </summary>
    public class UserEmailSettings
    {
        #region Production Email Settings - Automated Reports (Category-Based)
        // These properties correspond to RecipientCategoryKey values defined in appsettings.json
        // and allow users to override the default recipient lists for specific automated reports.

        /// <summary>
        /// Gets or sets the user-override 'To' recipients for the automated "Standard Daily" report.
        /// Corresponds to the "AutoRunDailyStandardRecipients" category key.
        /// </summary>
        [JsonPropertyName("AutoRunDailyStandardRecipientsTo")]
        public List<string>? AutoRunDailyStandardRecipientsTo { get; set; }

        /// <summary>
        /// Gets or sets the user-override 'CC' recipients for the automated "Standard Daily" report.
        /// Corresponds to the "AutoRunDailyStandardRecipients" category key.
        /// </summary>
        [JsonPropertyName("AutoRunDailyStandardRecipientsCC")]
        public List<string>? AutoRunDailyStandardRecipientsCC { get; set; }

        /// <summary>
        /// Gets or sets the user-override 'To' recipients for the automated "Daily (5days >= £1000)" report.
        /// Corresponds to the "AutoRunDaily5Day1kRecipients" category key.
        /// </summary>
        [JsonPropertyName("AutoRunDaily5Day1kRecipientsTo")]
        public List<string>? AutoRunDaily5Day1kRecipientsTo { get; set; }

        /// <summary>
        /// Gets or sets the user-override 'CC' recipients for the automated "Daily (5days >= £1000)" report.
        /// Corresponds to the "AutoRunDaily5Day1kRecipients" category key.
        /// </summary>
        [JsonPropertyName("AutoRunDaily5Day1kRecipientsCC")]
        public List<string>? AutoRunDaily5Day1kRecipientsCC { get; set; }

        /// <summary>
        /// Gets or sets the user-override 'To' recipients for the automated "Weekly" report.
        /// Corresponds to the "AutoRunWeeklyRecipients" category key.
        /// </summary>
        [JsonPropertyName("AutoRunWeeklyRecipientsTo")]
        public List<string>? AutoRunWeeklyRecipientsTo { get; set; }

        /// <summary>
        /// Gets or sets the user-override 'CC' recipients for the automated "Weekly" report.
        /// Corresponds to the "AutoRunWeeklyRecipients" category key.
        /// </summary>
        [JsonPropertyName("AutoRunWeeklyRecipientsCC")]
        public List<string>? AutoRunWeeklyRecipientsCC { get; set; }

        /// <summary>
        /// Gets or sets the user-override 'To' recipients for the automated "Femi Only" report.
        /// corresponds to the "AutoRunFemiOnlyRecipients" category key.
        /// </summary>
        [JsonPropertyName("AutoRunFemiOnlyRecipientsTo")]
        public List<string>? AutoRunFemiOnlyRecipientsTo { get; set; }

        /// <summary>
        /// Gets or sets the user-override 'CC' recipients for the automated "Femi Only" report.
        /// corresponds to the "AutoRunFemiOnlyRecipients" category key.
        /// </summary>
        [JsonPropertyName("AutoRunFemiOnlyRecipientsCC")]
        public List<string>? AutoRunFemiOnlyRecipientsCC { get; set; }

        /// <summary>
        /// Gets or sets the user-override 'To' recipients for the automated "New Customer" report.
        /// corresponds to the "AutoRunNewCustomerRecipients" category key.
        /// </summary>
        [JsonPropertyName("AutoRunNewCustomerRecipientsTo")]
        public List<string>? AutoRunNewCustomerRecipientsTo { get; set; }

        /// <summary>
        /// Gets or sets the user-override 'CC' recipients for the automated "New Customer" report.
        /// corresponds to the "AutoRunNewCustomerRecipients" category key.
        /// </summary>
        [JsonPropertyName("AutoRunNewCustomerRecipientsCC")]
        public List<string>? AutoRunNewCustomerRecipientsCC { get; set; }

        // Add more properties here if new RecipientCategoryKeys are defined for other automated reports.
        // Example:
        // [JsonPropertyName("AutoRunMonthlyMarketingRecipientsTo")]
        // public List<string>? AutoRunMonthlyMarketingRecipientsTo { get; set; }
        // [JsonPropertyName("AutoRunMonthlyMarketingRecipientsCC")]
        // public List<string>? AutoRunMonthlyMarketingRecipientsCC { get; set; }

        #endregion

        #region Production Email Settings - Manual Reports

        /// <summary>
        /// Gets or sets the user-override 'To' recipients for the standard MANUALLY RUN daily report.
        /// </summary>
        [JsonPropertyName("ProdManualRunDailyTo")]
        public List<string>? ProdManualRunDailyTo { get; set; }

        /// <summary>
        /// Gets or sets the user-override 'CC' recipients for the standard MANUALLY RUN daily report.
        /// </summary>
        [JsonPropertyName("ProdManualRunDailyCC")]
        public List<string>? ProdManualRunDailyCC { get; set; }

        /// <summary>
        /// Gets or sets the user-override 'To' recipients for MANUALLY RUN "Custom" type reports.
        /// </summary>
        [JsonPropertyName("ProdManualCustomTo")]
        public List<string>? ProdManualCustomTo { get; set; }

        /// <summary>
        /// Gets or sets the user-override 'CC' recipients for MANUALLY RUN "Custom" type reports.
        /// </summary>
        [JsonPropertyName("ProdManualCustomCC")]
        public List<string>? ProdManualCustomCC { get; set; }

        /// <summary>
        /// Gets or sets the user-override 'To' recipients for MANUALLY RUN "New Customer" type reports.
        /// </summary>
        [JsonPropertyName("ManualNewCustomerTo")]
        public List<string>? ManualNewCustomerTo { get; set; }

        /// <summary>
        /// Gets or sets the user-override 'CC' recipients for MANUALLY RUN "New Customer" type reports.
        /// </summary>
        [JsonPropertyName("ManualNewCustomerCC")]
        public List<string>? ManualNewCustomerCC { get; set; }

        /// <summary>
        /// Gets or sets the user-override 'To' recipients for manual production reports when "Send to Femi Only" is checked.
        /// (Typically for non-daily, non-custom manual reports).
        /// </summary>
        public List<string>? ProdFemiTo { get; set; }

        /// <summary>
        /// Gets or sets the user-override 'CC' recipients for manual production reports when "Send to Femi Only" is checked.
        /// </summary>
        public List<string>? ProdFemiCC { get; set; }

        /// <summary>
        /// Gets or sets the user-override 'To' recipients for manual production reports for the general team list.
        /// (Typically for non-daily, non-custom manual reports when "Femi Only" is NOT checked).
        /// </summary>
        public List<string>? ProdTeamTo { get; set; }

        /// <summary>
        /// Gets or sets the user-override 'CC' recipients for manual production reports for the general team list.
        /// </summary>
        public List<string>? ProdTeamCC { get; set; }


        #endregion

        #region Debug Email Settings
        /// <summary>
        /// Gets or sets the primary 'To' recipient for debug builds.
        /// </summary>
        public string? DebugTo { get; set; }

        /// <summary>
        /// Gets or sets the first 'CC' recipient for debug builds.
        /// </summary>
        public string? DebugCC1 { get; set; }

        /// <summary>
        /// Gets or sets the second 'CC' recipient for debug builds.
        /// </summary>
        public string? DebugCC2 { get; set; }
        #endregion

        #region Constructor
        /// <summary>
        /// Initialises a new instance of the <see cref="UserEmailSettings"/> class,
        /// ensuring all list properties are initialised to empty lists to prevent null reference issues.
        /// </summary>
        public UserEmailSettings()
        {
            // Initialise lists for automated report categories
            AutoRunDailyStandardRecipientsTo = new List<string>();
            AutoRunDailyStandardRecipientsCC = new List<string>();
            AutoRunDaily5Day1kRecipientsTo = new List<string>();
            AutoRunDaily5Day1kRecipientsCC = new List<string>();
            AutoRunWeeklyRecipientsTo = new List<string>();
            AutoRunWeeklyRecipientsCC = new List<string>();
            AutoRunFemiOnlyRecipientsTo = new List<string>();
            AutoRunFemiOnlyRecipientsCC = new List<string>();
            // Initialise example for a future custom automated report
            // AutoRunMonthlyMarketingRecipientsTo = new List<string>();
            // AutoRunMonthlyMarketingRecipientsCC = new List<string>();

            // Initialise lists for manual reports
            ProdManualRunDailyTo = new List<string>();
            ProdManualRunDailyCC = new List<string>();
            ProdManualCustomTo = new List<string>();
            ProdManualCustomCC = new List<string>();
            ProdFemiTo = new List<string>();
            ProdFemiCC = new List<string>();
            ProdTeamTo = new List<string>();
            ProdTeamCC = new List<string>();

            // Initialise debug settings
            DebugTo = string.Empty;
            DebugCC1 = string.Empty;
            DebugCC2 = string.Empty;
        }
        #endregion
    }
}