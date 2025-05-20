namespace QuoteConversionReportAutomation.Models
{
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Represents user-defined email recipient settings that can override application defaults.
    /// Includes settings for various production scenarios and debug configurations.
    /// </summary>
    public class UserEmailSettings
    {
        // --- Production Email Settings ---

        /// <summary>
        /// Gets or sets the 'To' recipients for the standard automated daily report in production.
        /// </summary>
        public List<string>? ProdAutoRunDailyTo { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the 'CC' recipients for the standard automated daily report in production.
        /// </summary>
        public List<string>? ProdAutoRunDailyCC { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the 'To' recipients for the standard MANUALLY RUN daily report in production.
        /// </summary>
        [JsonPropertyName("ProdManualRunDailyTo")] // Explicit JSON property name for consistency
        public List<string>? ProdManualRunDailyTo { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the 'CC' recipients for the standard MANUALLY RUN daily report in production.
        /// </summary>
        [JsonPropertyName("ProdManualRunDailyCC")] // Explicit JSON property name for consistency
        public List<string>? ProdManualRunDailyCC { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the 'To' recipients for the automated "Daily (5days >= £1000)" report in production.
        /// </summary>
        [JsonPropertyName("ProdAutoRunDaily5Day1kTo")]
        public List<string>? ProdAutoRunDaily5Day1kTo { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the 'CC' recipients for the automated "Daily (5days >= £1000)" report in production.
        /// </summary>
        [JsonPropertyName("ProdAutoRunDaily5Day1kCC")]
        public List<string>? ProdAutoRunDaily5Day1kCC { get; set; } = new List<string>();


        /// <summary>
        /// Gets or sets the 'To' recipients for production reports when "Send to Femi Only" is checked (for non-standard daily reports).
        /// </summary>
        public List<string>? ProdFemiTo { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the 'CC' recipients for production reports when "Send to Femi Only" is checked.
        /// </summary>
        public List<string>? ProdFemiCC { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the 'To' recipients for production reports when "Send to Femi Only" is NOT checked (i.e., team list for non-standard daily reports).
        /// </summary>
        public List<string>? ProdTeamTo { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the 'CC' recipients for production reports when "Send to Femi Only" is NOT checked.
        /// </summary>
        public List<string>? ProdTeamCC { get; set; } = new List<string>();


        // --- Debug Email Settings ---

        /// <summary>
        /// Gets or sets the primary 'To' recipient for debug builds.
        /// </summary>
        public string? DebugTo { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the first 'CC' recipient for debug builds.
        /// </summary>
        public string? DebugCC1 { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the second 'CC' recipient for debug builds.
        /// </summary>
        public string? DebugCC2 { get; set; } = string.Empty;

        /// <summary>
        /// Initializes a new instance of the <see cref="UserEmailSettings"/> class,
        /// ensuring all list properties are initialized to empty lists to prevent null reference issues.
        /// </summary>
        public UserEmailSettings()
        {
            ProdAutoRunDailyTo ??= new List<string>();
            ProdAutoRunDailyCC ??= new List<string>();
            ProdManualRunDailyTo ??= new List<string>(); // Initialize new property
            ProdManualRunDailyCC ??= new List<string>(); // Initialize new property
            ProdAutoRunDaily5Day1kTo ??= new List<string>();
            ProdAutoRunDaily5Day1kCC ??= new List<string>();
            ProdFemiTo ??= new List<string>();
            ProdFemiCC ??= new List<string>();
            ProdTeamTo ??= new List<string>();
            ProdTeamCC ??= new List<string>();
        }
    }
}
