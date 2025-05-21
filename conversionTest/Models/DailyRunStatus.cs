using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq; // Required for JObject
using System; // Required for DayOfWeek
using System.Collections.Generic;
using System.Linq;
using QuoteConversionReportAutomation.Services.Logging;

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

        // --- Convenience properties for existing known reports (can be phased out or kept for direct access) ---
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
        /// <param name="configuration">The application configuration to check which reports are enabled.</param>
        /// <param name="reportDefinitions">A list of configured report definitions.</param>
        /// <param name="currentDayOfWeek">The current day of the week to determine if day-specific reports were due.</param>
        /// <returns>True if all enabled and due reports have succeeded, false otherwise.</returns>
        public bool AllCurrentlyEnabledAndDueReportsSucceeded(
            IConfiguration configuration,
            IEnumerable<AutoReportDefinition> reportDefinitions,
            DayOfWeek currentDayOfWeek)
        {
            if (reportDefinitions == null || !reportDefinitions.Any())
            {
                return true; // No reports defined, so vacuously true.
            }

            foreach (var definition in reportDefinitions)
            {
                if (definition == null) continue; // Skip null definitions

                bool isEnabled = configuration.GetValue<bool>($"AutoReport:{definition.EnableConfigKey}", false);
                if (isEnabled)
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
                    // If it's enabled but wasn't due today, its success status for *this specific check* is ignored.
                    // Its actual flag (e.g., WeeklyReportSucceeded=false on a Wednesday) is correct as it didn't run.
                }
            }
            return true; // All enabled and due reports (for today) have succeeded.
        }
    }
}