// QuoteConversionReportAutomation - WPF/Services/BuildConfiguration.cs

using QuoteConversionReportAutomation.WPF.Services.Interfaces;

namespace QuoteConversionReportAutomation.WPF.Services
{
    /// <summary>
    /// Provides information about the application's build configuration.
    /// </summary>
    public class BuildConfiguration : IBuildConfiguration
    {
        /// <summary>
        /// Gets a value indicating whether the application was built in DEBUG mode.
        /// This is determined at compile time.
        /// </summary>
        public bool IsDebugBuild { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="BuildConfiguration"/> class.
        /// </summary>
        public BuildConfiguration()
        {
#if DEBUG
            IsDebugBuild = true;
#else
            IsDebugBuild = false;
#endif
        }
    }
}