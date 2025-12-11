#region Using Directives

// System-related namespaces for core functionalities.
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

// Third-party namespaces for external libraries.
using Microsoft.Extensions.Configuration;

// Project-specific namespaces for application components.
using QuoteConversionReportAutomation.Configuration;
using QuoteConversionReportAutomation.Services.Logging;
using QuoteConversionReportAutomation.Theming;

#endregion

namespace QuoteConversionReportAutomation.Helpers
{
    #region Static Class Definition
    /// <summary>
    /// Provides static helper methods dedicated to generating the content for the application's Help window.
    /// This class encapsulates the logic for loading Rich Text Format (RTF) templates,
    /// handling theme-specific variations, and replacing placeholders with dynamic configuration values.
    /// </summary>
    public static class HelpContentHelper
    {
        #region Public Static Methods

        /// <summary>
        /// Generates the title for the Help window, including the application name and version.
        /// </summary>
        /// <param name="appName">The name of the application.</param>
        /// <param name="appVersion">The version of the application.</param>
        /// <returns>A formatted title string (e.g., "Help - QCRA v1.9.7").</returns>
        public static string GetHelpTitle(string appName, string appVersion)
        {
            // Simply combines the provided app name and version into a standard title format.
            return $"Help - {appName} v{appVersion}";
        }

        /// <summary>
        /// Loads, formats, and returns the rich text content for the Help window.
        /// It selects a dark or light theme template and injects current configuration values into it.
        /// </summary>
        /// <param name="configuration">The application configuration for reading settings.</param>
        /// <param name="appName">The name of the application to display in the content.</param>
        /// <param name="appVersion">The version of the application to display in the content.</param>
        /// <returns>A string containing the fully formatted RTF help content.</returns>
        public static string GetHelpContent(IConfiguration configuration, string appName, string appVersion)
        {
            // Determine which RTF template file to use based on the current theme setting.
            bool isDarkMode = ThemeSettings.IsCurrentlyDark();
            string rtfFileName = isDarkMode ? "Help_Template_Dark.rtf" : "Help_Template_Light.rtf";

            // Construct the full path to the template file, assuming it's in a "Resources" subfolder.
            string rtfFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", rtfFileName);
            string helpMessageRtf;

            // Attempt to read the content of the template file.
            if (File.Exists(rtfFilePath))
            {
                try
                {
                    helpMessageRtf = File.ReadAllText(rtfFilePath);
                }
                catch (Exception ex)
                {
                    // If reading fails, log the error and return a simple RTF error message.
                    Logger.LogError($"Error reading help file '{rtfFilePath}': {ex.Message}", ex);
                    return @"{\rtf1\ansi Oops! Could not load help content.}";
                }
            }
            else
            {
                // If the template file doesn't exist, return a descriptive RTF error message.
                return $@"{{ \rtf1\ansi Help file '{rtfFileName}' not found.}}";
            }

            // Define a dictionary of placeholders and their corresponding values from the configuration.
            var replacements = new Dictionary<string, string>
            {
                { "{APP_NAME}", appName },
                { "{APP_VERSION}", appVersion },
                { "{AUTO_RUN_HOUR}", configuration.GetValue<int>(AppConfigKeys.AutoRunProcess.CheckHour, 8).ToString() },
                { "{FINANCIAL_YEAR_START_DAY}", configuration.GetValue<int>(AppConfigKeys.OperationalParameters.FinancialYearStartDay, 1).ToString() },
                { "{FINANCIAL_YEAR_START_MONTH}", configuration.GetValue<int>(AppConfigKeys.OperationalParameters.FinancialYearStartMonth, 5).ToString() },
                { "{LOG_ARCHIVE_DAYS}", configuration.GetValue<int?>(AppConfigKeys.Logging.LogArchiveOlderThanDays, 7)?.ToString() ?? "7" },
                { "{RAW_REPORTS_ARCHIVE_DAYS}", configuration.GetValue<int?>(AppConfigKeys.OperationalParameters.ArchiveRawReportsOlderThanDays, 30)?.ToString() ?? "30" }
            };

            // Use a StringBuilder for efficient string replacement.
            var helpBuilder = new StringBuilder(helpMessageRtf);
            foreach (var replacement in replacements)
            {
                helpBuilder.Replace(replacement.Key, replacement.Value);
            }

            // Return the final, populated RTF string.
            return helpBuilder.ToString();
        }

        #endregion
    }
    #endregion
}