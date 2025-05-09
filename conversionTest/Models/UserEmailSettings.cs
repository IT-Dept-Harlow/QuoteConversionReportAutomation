namespace QuoteConversionReportAutomation.Models
{
    /// <summary>
    /// Holds user-configurable email recipient settings.
    /// Properties are nullable to distinguish between not set (use app default) and explicitly set to empty.
    /// For simplicity in this example, we'll treat null/empty strings similarly when loading,
    /// but for saving, only non-null values will indicate an override.
    /// </summary>
    public class UserEmailSettings
    {
        // Production Email Settings
        public List<string>? ProdAutoRunDailyTo { get; set; }
        public List<string>? ProdAutoRunDailyCC { get; set; }
        public List<string>? ProdFemiTo { get; set; }
        public List<string>? ProdFemiCC { get; set; }
        public List<string>? ProdTeamTo { get; set; }
        public List<string>? ProdTeamCC { get; set; }

        // Debug Email Settings
        public string? DebugTo { get; set; } // Typically single string
        public string? DebugCC1 { get; set; }
        public string? DebugCC2 { get; set; }

        public UserEmailSettings()
        {
            ProdAutoRunDailyTo = new List<string>();
            ProdAutoRunDailyCC = new List<string>();
            ProdFemiTo = new List<string>();
            ProdFemiCC = new List<string>();
            ProdTeamTo = new List<string>();
            ProdTeamCC = new List<string>();
        }
    }
}