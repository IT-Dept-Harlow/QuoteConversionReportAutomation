// QuoteConversionReportAutomation - WPF/Services/Interfaces/IBuildConfiguration.cs

namespace QuoteConversionReportAutomation.WPF.Services.Interfaces
{
    /// <summary>
    /// Defines a contract for a service that provides information about the application's build configuration.
    /// </summary>
    public interface IBuildConfiguration
    {
        /// <summary>
        /// Gets a value indicating whether the application was built in DEBUG mode.
        /// </summary>
        bool IsDebugBuild { get; }
    }
}