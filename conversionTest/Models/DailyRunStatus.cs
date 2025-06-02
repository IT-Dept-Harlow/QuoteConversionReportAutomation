// DailyRunStatus.cs

#region Using Directives
using Microsoft.Extensions.Configuration; // Keep for now if other methods use it, or remove if not needed anywhere else in this class
using Newtonsoft.Json;
using Newtonsoft.Json.Linq; // Required for JObject
using System;
using System.Collections.Generic;
using System.Linq;
using QuoteConversionReportAutomation.Services.Logging;
using QuoteConversionReportAutomation.Models; // Ensure this is present if AutoReportDefinition is used directly
#endregion

namespace QuoteConversionReportAutomation.Models
{
    public class DailyReportRunStatus
    {
        /// <summary>
        /// The date for which these statuses apply, in "yyyy-MM-dd" format.
        /// </summary>
        public string StatusDate { get; set; } = string.Empty;

        /// <summary>
        /// Stores the success status for specific reports, keyed by their SuccessFlagJsonName.
        /// This allows for dynamic addition of report statuses.
        /// </summary>
        [JsonExtensionData]
        public IDictionary<string, JToken> AdditionalReportStatuses { get; set; } = new Dictionary<string, JToken>();

        // --- Convenience properties (can be kept or removed if direct Get/Set is preferred) ---
        [JsonIgnore]
        public bool StandardDailyReportSucceeded
        {
            get => GetReportSuccessStatus("StandardDailyReportSucceeded");
            set => SetReportSuccessStatus("StandardDailyReportSucceeded", value);
        }

        [JsonIgnore]
        public bool Daily5Day1kReportSucceeded
        {
            get => GetReportSuccessStatus("Daily5Day1kReportSucceeded");
            set => SetReportSuccessStatus("Daily5Day1kReportSucceeded", value);
        }

        [JsonIgnore]
        public bool WeeklyReportSucceeded
        {
            get => GetReportSuccessStatus("WeeklyReportSucceeded");
            set => SetReportSuccessStatus("WeeklyReportSucceeded", value);
        }

        /// <summary>
        /// Gets the success status for a report identified by its success flag JSON name.
        /// </summary>
        /// <param name="successFlagJsonName">The JSON property name for the report's success flag.</param>
        /// <returns>True if the report succeeded, false otherwise (or if not found).</returns>
        public bool GetReportSuccessStatus(string successFlagJsonName)
        {
            if (AdditionalReportStatuses.TryGetValue(successFlagJsonName, out JToken? token) && token != null)
            {
                return token.Type == JTokenType.Boolean && token.Value<bool>();
            }
            return false; // Default to false if not found or not a boolean
        }

        /// <summary>
        /// Sets the success status for a report.
        /// </summary>
        /// <param name="successFlagJsonName">The JSON property name for the report's success flag.</param>
        /// <param name="succeeded">The success status.</param>
        public void SetReportSuccessStatus(string successFlagJsonName, bool succeeded)
        {
            AdditionalReportStatuses[successFlagJsonName] = succeeded;
        }

        /// <summary>
        /// Checks if all currently enabled AND DUE automated reports (based on ReportDefinitions) have succeeded for the StatusDate.
        /// </summary>
        /// <param name="reportDefinitions">A list of configured report definitions.</param>
        /// <param name="currentDayOfWeek">The current day of the week to determine if day-specific reports were due.</param>
        /// <returns>True if all enabled and due reports have succeeded, false otherwise.</returns>
        public bool AllCurrentlyEnabledAndDueReportsSucceeded(
            IEnumerable<AutoReportDefinition> reportDefinitions, // IConfiguration parameter removed
            DayOfWeek currentDayOfWeek)
        {
            if (reportDefinitions == null || !reportDefinitions.Any())
            {
                return true; // No reports defined, so vacuously true.
            }

            foreach (var definition in reportDefinitions)
            {
                if (definition == null) continue; // Skip null definitions

                // Use the IsEnabled property directly from the definition
                if (definition.IsEnabled)
                {
                    // Check if the report was supposed to run today
                    bool wasDueToday = !definition.RunOnDayOfWeek.HasValue || definition.RunOnDayOfWeek.Value == currentDayOfWeek;

                    if (wasDueToday)
                    {
                        // If it was due today, its success flag must be true
                        if (!GetReportSuccessStatus(definition.SuccessFlagJsonName))
                        {
                            Logger.LogDebug($"AllCurrentlyEnabledAndDueReportsSucceeded: Report '{definition.ReportName}' was enabled and due today but did not succeed.");
                            return false; // Found an enabled and due report that has not succeeded.
                        }
                    }
                }
            }
            return true; // All enabled and due reports (for today) have succeeded.
        }
    }
}
