// ReportTypeHelper.cs
// Provides static helper methods for working with the ReportType enum,
// including conversions to/from strings, integer indices, and configuration keys.

#region Using Directives
using QuoteConversionReportAutomation.Models; // For ReportType enum
using QuoteConversionReportAutomation.Services.Logging; // For Logger
using System;
#endregion

namespace QuoteConversionReportAutomation.Helpers
{
    /// <summary>
    /// Provides static helper methods for handling <see cref="ReportType"/> enum conversions
    /// and retrieving related string representations.
    /// </summary>
    public static class ReportTypeHelper
    {
        #region Enum to String Conversions

        /// <summary>
        /// Gets a user-friendly display string for a given <see cref="ReportType"/>.
        /// This is typically used for UI elements like ComboBox items or display labels.
        /// </summary>
        /// <param name="reportType">The <see cref="ReportType"/> enum value.</param>
        /// <returns>A string representation suitable for display.</returns>
        public static string GetDisplayString(ReportType reportType)
        {
            return reportType switch
            {
                ReportType.Daily => "Daily",
                ReportType.Daily5Day1k => "Daily (5days >= £1000)",
                ReportType.Weekly => "Weekly",
                ReportType.Monthly => "Monthly",
                ReportType.Quarterly => "Quarterly (3 Months)", // Matches ComboBox text in Form1.Designer
                ReportType.Annual => "Annual",
                ReportType.Custom => "Custom",
                ReportType.Unknown => "Unknown",
                _ => reportType.ToString() // Fallback to enum member name
            };
        }

        /// <summary>
        /// Gets the string key used in configuration files (e.g., appsettings.json)
        /// to look up folder names or other settings related to a specific <see cref="ReportType"/>.
        /// For example, for ReportType.Daily, this might return "Daily".
        /// </summary>
        /// <param name="reportType">The <see cref="ReportType"/> enum value.</param>
        /// <returns>A string key for configuration lookups.</returns>
        public static string GetConfigKeyForFolderName(ReportType reportType)
        {
            return reportType switch
            {
                ReportType.Daily => "Daily",
                ReportType.Daily5Day1k => "Daily5Day1k", // Key used in appsettings.json for ReportTypeFolderNames
                ReportType.Weekly => "Weekly",
                ReportType.Monthly => "Monthly",
                ReportType.Quarterly => "Quarterly",
                ReportType.Annual => "Annual",
                ReportType.Custom => "Custom",
                _ => "Other" // Fallback key for unknown or unmapped types
            };
        }

        #endregion

        #region String to Enum Conversion

        /// <summary>
        /// Converts a string representation (typically from UI or configuration) to a <see cref="ReportType"/> enum value.
        /// This method is case-insensitive and handles common display strings.
        /// </summary>
        /// <param name="reportTypeString">The string to convert.</param>
        /// <returns>The corresponding <see cref="ReportType"/> enum value, or <see cref="ReportType.Unknown"/> if no match is found.</returns>
        public static ReportType FromString(string? reportTypeString)
        {
            if (string.IsNullOrWhiteSpace(reportTypeString))
            {
                return ReportType.Unknown;
            }

            return reportTypeString.Trim().ToLowerInvariant() switch
            {
                "daily" => ReportType.Daily,
                "daily (5days >= £1000)" => ReportType.Daily5Day1k, // Match exact display string
                "daily5day1k" => ReportType.Daily5Day1k,           // Match config key string
                "weekly" => ReportType.Weekly,
                "monthly" => ReportType.Monthly,
                "quarterly (3 months)" => ReportType.Quarterly,    // Match exact display string
                "quarterly" => ReportType.Quarterly,               // Match config key string
                "annual" => ReportType.Annual,
                "custom" => ReportType.Custom,
                _ => ReportType.Unknown // Default for unrecognized strings
            };
        }

        #endregion

        #region Enum to/from Integer Index Conversion

        /// <summary>
        /// Converts a <see cref="ReportType"/> enum value to its underlying integer representation.
        /// </summary>
        /// <param name="reportType">The <see cref="ReportType"/> enum value.</param>
        /// <returns>The integer value of the enum member.</returns>
        public static int ToInt(ReportType reportType)
        {
            return (int)reportType;
        }

        /// <summary>
        /// Converts an integer index to its corresponding <see cref="ReportType"/> enum value.
        /// </summary>
        /// <param name="reportTypeIndex">The integer index.</param>
        /// <returns>The <see cref="ReportType"/> enum value. Returns <see cref="ReportType.Unknown"/> if the index is not defined.</returns>
        public static ReportType FromInt(int reportTypeIndex)
        {
            if (Enum.IsDefined(typeof(ReportType), reportTypeIndex))
            {
                return (ReportType)reportTypeIndex;
            }
            Logger.LogWarning($"ReportTypeHelper.FromInt: Unknown report type index '{reportTypeIndex}'. Defaulting to ReportType.Unknown.");
            return ReportType.Unknown;
        }

        #endregion
    }
}
