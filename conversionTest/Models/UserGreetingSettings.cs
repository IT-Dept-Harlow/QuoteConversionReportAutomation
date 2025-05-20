// UserGreetingSettings.cs
namespace QuoteConversionReportAutomation.Models
{
    /// <summary>
    /// Represents user-defined overrides for email greetings.
    /// This structure mirrors the EmailGreetings sections in appsettings.json.
    /// </summary>
    public class UserGreetingSettings
    {
        // Production Greetings (can be overridden by user)
        public string? AutoRunDaily { get; set; }
        public string? ManualStdDaily { get; set; }
        public string? AutoRunDaily5Day1k { get; set; }
        public string? ManualFemi { get; set; }
        public string? ManualTeam { get; set; }

        // Debug Greeting (can be overridden by user)
        public string? DebugDefault { get; set; }

        public UserGreetingSettings()
        {
            // Initialize with nulls, manager will handle fallbacks
        }
    }
}