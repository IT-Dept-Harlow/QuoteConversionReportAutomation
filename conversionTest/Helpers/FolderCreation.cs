// C# 10+ Features
using QuoteConversionReportAutomation.Services.Logging;
using System.Globalization; // Added for month formatting

namespace QuoteConversionReportAutomation.Helpers
{
    /// <summary>
    /// Utility class for creating report-specific folder structures.
    /// Handles Daily, "Daily (5days >= £1000)", Weekly, Monthly, Quarterly, Annual, and Custom reports.
    /// </summary>
    public static class FolderCreation
    {
        // --- Report Type Indices (Must match Form1.cs and ExcelCopyData.cs) ---
        private const int DailyReportIndex = 0;
        private const int NewDailyReportOver1kIndex = 1; // New Report Type: "Daily (5days >= £1000)"
        private const int WeeklyReportIndex = 2;
        private const int MonthlyReportIndex = 3;
        private const int QuarterlyReportIndex = 4;
        private const int AnnualReportIndex = 5;
        private const int CustomReportIndex = 6;

        /// <summary>
        /// Creates the specific folder structure for the report type based on the provided date and returns the full path.
        /// Handles Daily, "Daily (5days >= £1000)", Weekly, Monthly, Quarterly, Annual, and Custom reports.
        /// </summary>
        /// <param name="reportType">The report type index (e.g., Form1.DailyReportIndex).</param>
        /// <param name="baseSaveLocation">The root directory (e.g., ...\Estimates\).</param>
        /// <param name="folderDate">The date to use for determining year/month/week subfolders.</param>
        /// <returns>The full path to the target folder, or null on error.</returns>
        public static string? CreateReportSpecificFolder(int reportType, string baseSaveLocation, DateTime folderDate)
        {
            Logger.LogDebug($"Entering FolderCreation.CreateReportSpecificFolder(reportType: {reportType}, base: {baseSaveLocation}, folderDate: {folderDate:d})");
            try
            {
                // Get the path using the helper, passing the specific date
                string? targetFolderPath = GetReportSpecificFolderPath(reportType, baseSaveLocation, folderDate);

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
            finally
            {
                Logger.LogDebug($"Exiting FolderCreation.CreateReportSpecificFolder");
            }
        }

        /// <summary>
        /// Determines the specific folder path based on the report type and date, without creating it.
        /// Structure:
        /// - Daily/Weekly/"Daily (5days >= £1000)": {Base}\{ReportTypeFolder}\{Year}\{MonthName}\Week {Num}
        /// - Monthly:      {Base}\{ReportTypeFolder}\{Year}\{MMM yy}
        /// - Quarterly:    {Base}\{ReportTypeFolder}\{Year}\{Mmm to Mmm}
        /// - Annual:       {Base}\{ReportTypeFolder}\{Year}
        /// - Custom:       {Base}\Custom Reports\{Year}\{YYYY-MM-DD_HHMMSS}
        /// </summary>
        /// <param name="reportType">The report type index (e.g., Form1.DailyReportIndex).</param>
        /// <param name="baseSaveLocation">The root directory (e.g., ...\Estimates\).</param>
        /// <param name="folderDate">The date to use for determining year/month/week/timestamp subfolders.</param>
        /// <returns>The full path to the target folder, or null if type is invalid or path error.</returns>
        public static string? GetReportSpecificFolderPath(int reportType, string baseSaveLocation, DateTime folderDate)
        {
            Logger.LogDebug($"Entering FolderCreation.GetReportSpecificFolderPath(reportType: {reportType}, base: {baseSaveLocation}, folderDate: {folderDate:d})");

            if (string.IsNullOrWhiteSpace(baseSaveLocation))
            {
                Logger.LogError("Base save location provided to GetReportSpecificFolderPath is null or empty.");
                return null;
            }

            string reportTypeFolder;
            string yearFolder = string.Empty;
            string subFolder = string.Empty;
            string weekFolder = string.Empty;

            switch (reportType)
            {
                case DailyReportIndex:
                    reportTypeFolder = "Daily Reports";
                    yearFolder = folderDate.ToString("yyyy");
                    subFolder = folderDate.ToString("MMMM", CultureInfo.InvariantCulture); // Use InvariantCulture for month name consistency       
                    weekFolder = $"Week {GetWeekOfMonth(folderDate)}";
                    break;
                case NewDailyReportOver1kIndex: // New Report Type
                    reportTypeFolder = "Daily Reports (5day 1k)"; // Specific folder name as discussed
                    yearFolder = folderDate.ToString("yyyy");
                    subFolder = folderDate.ToString("MMMM", CultureInfo.InvariantCulture);
                    weekFolder = $"Week {GetWeekOfMonth(folderDate)}";
                    break;
                case WeeklyReportIndex:
                    reportTypeFolder = "Weekly Reports";
                    yearFolder = folderDate.ToString("yyyy");
                    subFolder = folderDate.ToString("MMMM", CultureInfo.InvariantCulture);
                    weekFolder = $"Week {GetWeekOfMonth(folderDate)}";
                    break;
                case MonthlyReportIndex:
                    reportTypeFolder = "Monthly Reports";
                    yearFolder = folderDate.ToString("yyyy");
                    subFolder = folderDate.ToString("MMM yy", CultureInfo.InvariantCulture);
                    break;
                case QuarterlyReportIndex:
                    reportTypeFolder = "Quarterly reports";
                    yearFolder = folderDate.ToString("yyyy");
                    int quarter = (folderDate.Month - 1) / 3 + 1;
                    DateTime quarterStartDate = new(folderDate.Year, (quarter - 1) * 3 + 1, 1);
                    DateTime quarterEndDate = quarterStartDate.AddMonths(3).AddDays(-1);
                    subFolder = $"{quarterStartDate:MMM} to {quarterEndDate:MMM}";
                    break;
                case AnnualReportIndex:
                    reportTypeFolder = "Annual Reports";
                    yearFolder = folderDate.ToString("yyyy");
                    break;
                case CustomReportIndex:
                    reportTypeFolder = "Custom Reports";
                    yearFolder = folderDate.ToString("yyyy");
                    subFolder = folderDate.ToString("yyyy-MM-dd_HHmmss");
                    break;
                default:
                    Logger.LogWarning($"Invalid report type '{reportType}' for folder creation. Using 'Other Reports'.");
                    reportTypeFolder = "Other Reports";
                    // For unknown types, perhaps just use the base and reportTypeFolder directly, or a generic year/month.
                    // For now, it will fall through and potentially not add year/sub/week folders if they remain empty.
                    break;
            }

            string? fullPath = null;
            try
            {
                fullPath = Path.Combine(baseSaveLocation, reportTypeFolder);

                if (!string.IsNullOrEmpty(yearFolder))
                {
                    fullPath = Path.Combine(fullPath, yearFolder);
                }
                if (!string.IsNullOrEmpty(subFolder))
                {
                    fullPath = Path.Combine(fullPath, subFolder);
                }
                if (!string.IsNullOrEmpty(weekFolder)) // Only for Daily, NewDailyReportOver1kIndex, Weekly
                {
                    fullPath = Path.Combine(fullPath, weekFolder);
                }
            }
            catch (ArgumentException ex)
            {
                Logger.LogError($"Error combining path segments: {ex.Message}. Base='{baseSaveLocation}', Type='{reportTypeFolder}', Year='{yearFolder}', Sub='{subFolder}', Week='{weekFolder}'");
                return null; // Return null if path combination fails
            }
            Logger.LogDebug($"Exiting FolderCreation.GetReportSpecificFolderPath. Result: {fullPath ?? "null"}");
            return fullPath;
        }


        /// <summary>
        /// Calculates the week number of a given date within its month.
        /// Assumes weeks start on Monday. (ISO 8601 week date system defines Monday as the first day of the week)
        /// </summary>
        /// <param name="date">The date to check.</param>
        /// <returns>The week number (1-5/6).</returns>
        public static int GetWeekOfMonth(DateTime date)
        {
            // Get the first day of the month
            DateTime firstOfMonth = new(date.Year, date.Month, 1);

            // DayOfWeek returns Sunday = 0, Monday = 1, ..., Saturday = 6.
            // We want Monday = 0, ..., Sunday = 6 for easier calculation with firstDayOfWeekIso.
            // Or, more directly, use CultureInfo to determine week rules.
            // For simplicity and consistency with previous logic, let's stick to a direct calculation
            // that roughly aligns with common business week understanding if not strictly ISO.

            // A common approach:
            // Day of the month + (number of days from the start of the week to the first of the month - 1) / 7 + 1
            // Example: If 1st is Wednesday (DayOfWeek=3), and date is 10th:
            // (10 + (3-1) -1) / 7 + 1 = (10 + 2 - 1)/7 + 1 = 11/7 + 1 = 1 + 1 = 2 (if integer division)
            // This needs to be careful.

            // Using CultureInfo.CurrentCulture.Calendar.GetWeekOfYear:
            // This gets the week of the year. We need week of the month.
            // CalendarWeekRule.FirstDay and DayOfWeek.Monday can be used.

            // Simplified approach based on the existing logic:
            // Get the day of the week for the first day (Monday = 1, Sunday = 7 for this calculation logic)
            int firstDayOfWeekValue = (int)firstOfMonth.DayOfWeek; // Sunday = 0, Monday = 1 ... Saturday = 6
            if (firstDayOfWeekValue == 0) firstDayOfWeekValue = 7; // Adjust Sunday to be 7

            // Calculate week number. (date.Day + days before first of month in its week - 1) / 7 + 1
            int weekOfMonth = (date.Day + firstDayOfWeekValue - 1 - 1) / 7 + 1; // Subtract 1 from firstDayOfWeekValue to make it 0-indexed for offset
                                                                                // then subtract another 1 because day 1-7 is week 1.

            // Example: date = 1st May 2023 (Monday). firstDayOfWeekValue = 1. (1 + 1 - 1 - 1)/7 + 1 = 0/7 + 1 = 1. Correct.
            // Example: date = 8th May 2023 (Monday). firstDayOfWeekValue = 1. (8 + 1 - 1 - 1)/7 + 1 = 7/7 + 1 = 2. Correct.
            // Example: date = 7th May 2023 (Sunday). firstDayOfWeekValue = 1. (7 + 1 - 1 - 1)/7 + 1 = 6/7 + 1 = 1. Correct.
            // Example: date = 3rd May 2023 (Wednesday). firstDayOfMonth is Monday (May 1st). firstDayOfWeekValue = 1.
            //           (3 + 1 - 1 -1) / 7 + 1 = 2/7 + 1 = 1. Correct.

            // Ensure CultureInfo is used for month name if that's also desired for folder structure
            // For GetWeekOfMonth, this direct calculation is often sufficient for simple bucketing.
            // Using InvariantCulture for month name in GetReportSpecificFolderPath ensures consistency across systems.

            return weekOfMonth;
        }
    }
}
