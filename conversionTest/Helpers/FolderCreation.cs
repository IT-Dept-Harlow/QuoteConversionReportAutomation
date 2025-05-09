// C# 10+ Features
using conversionTest;
using System;
using System.Globalization; // Added for month formatting
using System.IO;

namespace QuoteConversionReportAutomation.Helpers
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
        private const int CustomReportIndex = 5; // <<< ADDED Custom Index

        /// <summary>
        /// Creates the specific folder structure for the report type based on the provided date and returns the full path.
        /// Handles Daily, Weekly, Monthly, Quarterly, Annual, and Custom reports.
        /// </summary>
        /// <param name="reportType">The report type index (0=Daily, 1=Weekly, etc.).</param>
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
        /// - Daily/Weekly: {Base}\{ReportType}\{Year}\{MonthName}\Week {Num}
        /// - Monthly:      {Base}\{ReportType}\{Year}\{MMM yy}
        /// - Quarterly:    {Base}\{ReportType}\{Year}\{Mmm to Mmm}
        /// - Annual:       {Base}\{ReportType}\{Year}
        /// - Custom:       {Base}\Custom Reports\{Year}\{YYYY-MM-DD_HHMMSS}
        /// </summary>
        /// <param name="reportType">The report type index (0=Daily, 1=Weekly, etc.).</param>
        /// <param name="baseSaveLocation">The root directory (e.g., ...\Estimates\).</param>
        /// <param name="folderDate">The date to use for determining year/month/week/timestamp subfolders.</param>
        /// <returns>The full path to the target folder, or null if type is invalid or path error.</returns>
        public static string? GetReportSpecificFolderPath(int reportType, string baseSaveLocation, DateTime folderDate) // Made public, added date param
        {
            Logger.LogDebug($"Entering FolderCreation.GetReportSpecificFolderPath(reportType: {reportType}, base: {baseSaveLocation}, folderDate: {folderDate:d})");
            // Validate base location first
            if (string.IsNullOrWhiteSpace(baseSaveLocation))
            {
                Logger.LogError("Base save location provided to GetReportSpecificFolderPath is null or empty.");
                return null; // Cannot proceed without a base path
            }

            string reportTypeFolder;
            string yearFolder = string.Empty;     // e.g., "2025"
            string subFolder = string.Empty;    // e.g., "April" or "Apr 25" or "Jan to Mar" or "Week 4" or "2025-04-29_103055"
            string weekFolder = string.Empty;     // Only used for Daily/Weekly for third level

            // Determine folder names based on report type and folderDate
            switch (reportType)
            {
                case DailyReportIndex: // 0 = Daily
                case WeeklyReportIndex: // 1 = Weekly
                    reportTypeFolder = reportType == DailyReportIndex ? "Daily Reports" : "Weekly Reports";
                    yearFolder = folderDate.ToString("yyyy");         // Full year "2025"
                    subFolder = folderDate.ToString("MMMM");        // Full month name "April"
                    int weekNum = GetWeekOfMonth(folderDate);         // Use helper
                    weekFolder = $"Week {weekNum}";                   // "Week 4"
                    break;

                case MonthlyReportIndex: // 2 = Monthly
                    reportTypeFolder = "Monthly Reports";
                    yearFolder = folderDate.ToString("yyyy");
                    subFolder = folderDate.ToString("MMM yy", CultureInfo.InvariantCulture); // e.g., Apr 25
                    break;
                case QuarterlyReportIndex: // 3 = Quarterly
                    reportTypeFolder = "Quarterly reports";
                    yearFolder = folderDate.ToString("yyyy");
                    int quarter = (folderDate.Month - 1) / 3 + 1;
                    DateTime quarterStartDate = new(folderDate.Year, (quarter - 1) * 3 + 1, 1);
                    DateTime quarterEndDate = quarterStartDate.AddMonths(3).AddDays(-1);
                    subFolder = $"{quarterStartDate:MMM} to {quarterEndDate:MMM}"; // e.g., Jan to Mar
                    break;
                case AnnualReportIndex: // 4 = Annual
                    reportTypeFolder = "Annual Reports";
                    yearFolder = folderDate.ToString("yyyy");
                    break;
                case CustomReportIndex: // 5 = Custom <<< ADDED CASE
                    reportTypeFolder = "Custom Reports"; // Specific top-level folder
                    yearFolder = folderDate.ToString("yyyy"); // Group by year
                    subFolder = folderDate.ToString("yyyy-MM-dd_HHmmss"); // Unique timestamp folder
                    break;
                default:
                    Logger.LogWarning($"Invalid report type '{reportType}' for folder creation. Using 'Other Reports'.");
                    reportTypeFolder = "Other Reports";
                    break;
            }

            // Construct the full path safely
            string? fullPath = null;
            try
            {
                // Start with Base -> ReportType
                fullPath = Path.Combine(baseSaveLocation, reportTypeFolder);

                // Add Year if applicable
                if (!string.IsNullOrEmpty(yearFolder))
                {
                    fullPath = Path.Combine(fullPath, yearFolder);
                }
                // Add Month/Quarter/Timestamp folder if applicable
                if (!string.IsNullOrEmpty(subFolder))
                {
                    fullPath = Path.Combine(fullPath, subFolder);
                }
                // Add Week if applicable (only Daily/Weekly)
                if (!string.IsNullOrEmpty(weekFolder))
                {
                    fullPath = Path.Combine(fullPath, weekFolder);
                }
            }
            catch (ArgumentException ex) // Catch errors during Path.Combine
            {
                Logger.LogError($"Error combining path segments: {ex.Message}. Base='{baseSaveLocation}', Type='{reportTypeFolder}', Year='{yearFolder}', Sub='{subFolder}', Week='{weekFolder}'");
            }
            Logger.LogDebug($"Exiting FolderCreation.GetReportSpecificFolderPath. Result: {fullPath ?? "null"}");
            return fullPath;
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
            // Get the day of the week for the first day (Monday = 1, Sunday = 7)
            int firstDayOfWeekIso = firstOfMonth.DayOfWeek == 0 ? 7 : (int)firstOfMonth.DayOfWeek;
            // Calculate week number
            int weekOfMonth = (date.Day + firstDayOfWeekIso - 1 - 1) / 7 + 1;
            return weekOfMonth;
        }
    }
}