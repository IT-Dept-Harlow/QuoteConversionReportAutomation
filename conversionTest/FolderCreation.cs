// C# 10+ Features
using conversionTest;

namespace QuoteConversionReportAutomation
{
    /// <summary>
    /// Utility class for creating report-specific folder structures.
    /// </summary>
    public static class FolderCreation
    {
        // --- Report Type Indices (Must match Form1.cs and ExcelCopyData.cs) ---
        private const int DailyReportIndex = 0;
        private const int WeeklyReportIndex = 1;
        private const int MonthlyReportIndex = 2;
        private const int QuarterlyReportIndex = 3;
        private const int AnnualReportIndex = 4;

        /// <summary>
        /// Creates the specific folder structure for the report type and returns the full path.
        /// Handles Daily, Weekly, Monthly, Quarterly, and Annual reports.
        /// </summary>
        /// <param name="reportType">The report type index (0=Daily, 1=Weekly, etc.).</param>
        /// <param name="baseSaveLocation">The root directory (e.g., ...\Estimates\).</param>
        /// <returns>The full path to the target folder, or null on error.</returns>
        public static string? CreateReportSpecificFolder(int reportType, string baseSaveLocation)
        {
            try
            {
                string? targetFolderPath = GetReportSpecificFolderPath(reportType, baseSaveLocation);

                if (string.IsNullOrEmpty(targetFolderPath))
                {
                    Logger.LogError($"Could not determine target folder path for report type {reportType}.");
                    return null;
                }

                // Ensure the directory exists
                Directory.CreateDirectory(targetFolderPath);
                Logger.LogInfo($"Ensured report output folder exists: {targetFolderPath}");
                return targetFolderPath;
            }
            catch (ArgumentNullException ex) // Catch specific exceptions
            {
                Logger.LogError($"Error creating report folder: Base save location cannot be null or empty. {ex.Message}");
                return null;
            }
            catch (ArgumentException ex)
            {
                Logger.LogError($"Error creating report folder: Invalid path characters or format. {ex.Message}");
                return null;
            }
            catch (PathTooLongException ex)
            {
                Logger.LogError($"Error creating report folder: The resulting path is too long. {ex.Message}");
                return null;
            }
            catch (DirectoryNotFoundException ex)
            {
                Logger.LogError($"Error creating report folder: Part of the path could not be found. {ex.Message}");
                return null;
            }
            catch (IOException ex) // General IO errors
            {
                Logger.LogError($"Error creating report folder (IO): {ex.Message}");
                return null;
            }
            catch (UnauthorizedAccessException ex)
            {
                Logger.LogError($"Error creating report folder: Permission denied. {ex.Message}");
                return null;
            }
            catch (NotSupportedException ex)
            {
                Logger.LogError($"Error creating report folder: Path format not supported. {ex.Message}");
                return null;
            }
            catch (Exception ex) // Catch-all for unexpected errors
            {
                Logger.LogError($"Unexpected error creating report folder for type {reportType}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Determines the specific folder path based on the report type without creating it.
        /// Internal helper method.
        /// Structure:
        /// - Daily/Weekly: {Base}\{ReportType}\{Year}\{MonthName}\Week {Num}
        /// - Monthly/Quarterly/Annual: {Base}\{ReportType}
        /// </summary>
        /// <param name="reportType">The report type index (0=Daily, 1=Weekly, etc.).</param>
        /// <param name="baseSaveLocation">The root directory (e.g., ...\Estimates\).</param>
        /// <returns>The full path to the target folder, or null if type is invalid.</returns>
        private static string? GetReportSpecificFolderPath(int reportType, string baseSaveLocation)
        {
            // Validate base location first
            if (string.IsNullOrWhiteSpace(baseSaveLocation))
            {
                Logger.LogError("Base save location provided to GetReportSpecificFolderPath is null or empty.");
                return null; // Cannot proceed without a base path
            }

            DateTime now = DateTime.Now;
            string reportTypeFolder;
            string yearFolder = string.Empty;     // e.g., "2025"
            string monthFolder = string.Empty;    // e.g., "April"
            string weekFolder = string.Empty;     // e.g., "Week 4"

            // Determine folder names based on report type
            switch (reportType)
            {
                case DailyReportIndex: // 0 = Daily
                case WeeklyReportIndex: // 1 = Weekly
                    reportTypeFolder = reportType == DailyReportIndex ? "Daily Reports" : "Weekly Reports";
                    yearFolder = now.ToString("yyyy");         // Full year "2025"
                    monthFolder = now.ToString("MMMM");        // Full month name "April"
                    int weekNum = GetWeekOfMonth(now);          // Use helper
                    weekFolder = $"Week {weekNum}";             // "Week 4"
                    break;

                case MonthlyReportIndex: // 2 = Monthly
                    reportTypeFolder = "Monthly Reports";
                    // Monthly reports go directly into the Monthly Reports folder (no year/month subfolders)
                    break;
                case QuarterlyReportIndex: // 3 = Quarterly
                    reportTypeFolder = "Quarterly reports";
                    // Quarterly reports go directly into the Quarterly reports folder
                    break;
                case AnnualReportIndex: // 4 = Annual
                    reportTypeFolder = "Annual Reports";
                    // Annual reports go directly into the Annual Reports folder
                    break;
                default:
                    Logger.LogWarning($"Invalid report type '{reportType}' for folder creation. Using 'Other Reports'.");
                    reportTypeFolder = "Other Reports";
                    break;
            }

            // Construct the full path safely
            try
            {
                // Start with Base -> ReportType
                string fullPath = Path.Combine(baseSaveLocation, reportTypeFolder);

                // Add Year for Daily/Weekly
                if (!string.IsNullOrEmpty(yearFolder))
                {
                    fullPath = Path.Combine(fullPath, yearFolder);
                }
                // Add Month for Daily/Weekly
                if (!string.IsNullOrEmpty(monthFolder))
                {
                    fullPath = Path.Combine(fullPath, monthFolder);
                }
                // Add Week for Daily/Weekly
                if (!string.IsNullOrEmpty(weekFolder))
                {
                    fullPath = Path.Combine(fullPath, weekFolder);
                }
                return fullPath;
            }
            catch (ArgumentException ex) // Catch errors during Path.Combine
            {
                Logger.LogError($"Error combining path segments: {ex.Message}. Base='{baseSaveLocation}', Type='{reportTypeFolder}', Year='{yearFolder}', Month='{monthFolder}', Week='{weekFolder}'");
                return null;
            }
        }


        /// <summary>
        /// Calculates the week number of a given date within its month.
        /// Assumes weeks start on Monday.
        /// </summary>
        /// <param name="date">The date to check.</param>
        /// <returns>The week number (1-5/6).</returns>
        public static int GetWeekOfMonth(DateTime date)
        {
            // Get the first day of the month
            DateTime firstOfMonth = new(date.Year, date.Month, 1);

            // Get the day of the week for the first day (Monday = 1, Sunday = 7, using ISO 8601 standard)
            // DayOfWeek enum: Sunday = 0, Monday = 1, ..., Saturday = 6
            int firstDayOfWeekIso = firstOfMonth.DayOfWeek == 0 ? 7 : (int)firstOfMonth.DayOfWeek; // Convert Sunday to 7

            // Calculate the week number using integer division.
            // Add the offset of the first day (1-based, where Monday is 1) minus 1.
            // Add the day of the month (1-based).
            // Subtract 1 because we want the result to be 0-based for the division.
            // Divide by 7.
            // Add 1 to make the final result 1-based (Week 1, Week 2, etc.).
            int weekOfMonth = (date.Day + firstDayOfWeekIso - 1 - 1) / 7 + 1;

            // Alternative simpler calculation (often sufficient if exact ISO week definition isn't critical):
            // int weekOfMonth = (date.Day + (int)firstOfMonth.DayOfWeek - (int)DayOfWeek.Monday -1) / 7 + 1;
            // This might need adjustment based on whether DayOfWeek.Monday is 0 or 1 in your context.

            return weekOfMonth;
        }
    }
}
