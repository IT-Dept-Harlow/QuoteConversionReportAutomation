<<<<<<< HEAD
﻿// FolderCreation.cs
// Utility class for creating and determining report-specific folder structures.
// Updated to use AppConfigKeys and ReportTypeHelper.

#region Using Directives
// System related namespaces
using System;
using System.Globalization; // Added for month formatting
using System.IO;

// Third-party namespaces
using Microsoft.Extensions.Configuration; // For IConfiguration

// Project specific namespaces
using QuoteConversionReportAutomation.Configuration; // For AppConfigKeys
using QuoteConversionReportAutomation.Models;     // For ReportType enum
using QuoteConversionReportAutomation.Services.Logging; // For Logger
#endregion
=======
﻿// C# 10+ Features
using QuoteConversionReportAutomation.Services.Logging;
using System.Globalization; // Added for month formatting
>>>>>>> parent of 171b8e4 (v1.9.2)

namespace QuoteConversionReportAutomation.Helpers
{
    /// <summary>
<<<<<<< HEAD
    /// Utility class for creating and determining paths for report-specific folder structures.
    /// Handles various report types by leveraging <see cref="ReportTypeHelper"/> and
    /// retrieving folder name configurations via <see cref="AppConfigKeys"/>.
    /// </summary>
    public static class FolderCreation
    {
        // Report Type Integer Index constants are removed. ReportType enum is used directly.
=======
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
>>>>>>> parent of 171b8e4 (v1.9.2)

        /// <summary>
        /// Creates the specific folder structure for the report type based on the provided date and returns the full path.
        /// Handles Daily, "Daily (5days >= £1000)", Weekly, Monthly, Quarterly, Annual, and Custom reports.
        /// </summary>
<<<<<<< HEAD
        /// <param name="reportType">The <see cref="ReportType"/> enum value representing the report type.</param>
        /// <param name="baseSaveLocation">The root directory where report type folders will be created.</param>
        /// <param name="folderDate">The date used to determine year, month, week, or timestamp subfolders.</param>
        /// <param name="configuration">The application's <see cref="IConfiguration"/> instance to retrieve folder name settings.</param>
        /// <returns>The full path to the created target folder, or null if an error occurs.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="baseSaveLocation"/> or <paramref name="configuration"/> is null.</exception>
        public static string? CreateReportSpecificFolder(ReportType reportType, string baseSaveLocation, DateTime folderDate, IConfiguration configuration)
=======
        /// <param name="reportType">The report type index (e.g., Form1.DailyReportIndex).</param>
        /// <param name="baseSaveLocation">The root directory (e.g., ...\Estimates\).</param>
        /// <param name="folderDate">The date to use for determining year/month/week subfolders.</param>
        /// <returns>The full path to the target folder, or null on error.</returns>
        public static string? CreateReportSpecificFolder(int reportType, string baseSaveLocation, DateTime folderDate)
>>>>>>> parent of 171b8e4 (v1.9.2)
        {
            Logger.LogDebug($"Entering FolderCreation.CreateReportSpecificFolder(reportType: {reportType}, base: {baseSaveLocation}, folderDate: {folderDate:d})");
            try
            {
<<<<<<< HEAD
                string? targetFolderPath = GetReportSpecificFolderPath(reportType, baseSaveLocation, folderDate, configuration);
=======
                // Get the path using the helper, passing the specific date
                string? targetFolderPath = GetReportSpecificFolderPath(reportType, baseSaveLocation, folderDate);
>>>>>>> parent of 171b8e4 (v1.9.2)

                if (string.IsNullOrEmpty(targetFolderPath))
                {
                    Logger.LogError($"Could not determine target folder path for report type {reportType}.");
                    return null;
                }

<<<<<<< HEAD
                Directory.CreateDirectory(targetFolderPath); // Ensure the directory structure exists.
=======
                // Ensure the directory exists
                Directory.CreateDirectory(targetFolderPath);
>>>>>>> parent of 171b8e4 (v1.9.2)

                Logger.LogInfo($"Ensured report output folder exists: {targetFolderPath}");
                return targetFolderPath;
            }
<<<<<<< HEAD
            catch (ArgumentException ex)
=======
            catch (ArgumentNullException ex) // Catch specific exceptions
>>>>>>> parent of 171b8e4 (v1.9.2)
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
<<<<<<< HEAD
                Logger.LogError($"Error creating report folder (PathTooLongException): Resulting path too long. Base: '{baseSaveLocation}'. Error: {ex.Message}", ex);
=======
                Logger.LogError($"Error creating report folder: The resulting path is too long. {ex.Message}");
>>>>>>> parent of 171b8e4 (v1.9.2)
                return null;
            }
            catch (DirectoryNotFoundException ex)
            {
<<<<<<< HEAD
                Logger.LogError($"Error creating report folder (DirectoryNotFoundException): Base path part not found. Base: '{baseSaveLocation}'. Error: {ex.Message}", ex);
                return null;
            }
            catch (IOException ioEx)
=======
                Logger.LogError($"Error creating report folder: Part of the path could not be found. {ex.Message}");
                return null;
            }
            catch (IOException ex) // General IO errors
>>>>>>> parent of 171b8e4 (v1.9.2)
            {
                Logger.LogError($"Error creating report folder (IO): {ex.Message}");
                return null;
            }
            catch (UnauthorizedAccessException ex)
            {
                Logger.LogError($"Error creating report folder: Permission denied. {ex.Message}");
                return null;
            }
<<<<<<< HEAD
            catch (NotSupportedException nsEx)
=======
            catch (NotSupportedException ex)
>>>>>>> parent of 171b8e4 (v1.9.2)
            {
                Logger.LogError($"Error creating report folder: Path format not supported. {ex.Message}");
                return null;
            }
<<<<<<< HEAD
            catch (Exception ex)
=======
            catch (Exception ex) // Catch-all for unexpected errors
>>>>>>> parent of 171b8e4 (v1.9.2)
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
<<<<<<< HEAD
        /// Determines the specific folder path based on the report type, date, and configuration, without creating the folder.
        /// The folder name for the report type itself is read from <paramref name="configuration"/>
        /// using keys from <see cref="AppConfigKeys.OperationalParameters.ReportTypeFolderNames"/>.
        /// </summary>
        /// <param name="reportType">The <see cref="ReportType"/> enum value representing the report type.</param>
        /// <param name="baseSaveLocation">The root directory (e.g., ...\Estimates\).</param>
        /// <param name="folderDate">The date used for determining year, month, week, or timestamp subfolders.</param>
        /// <param name="configuration">The application's <see cref="IConfiguration"/> instance.</param>
        /// <returns>The full path to the target folder, or null if path construction fails.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="baseSaveLocation"/> or <paramref name="configuration"/> is null.</exception>
        public static string? GetReportSpecificFolderPath(ReportType reportType, string baseSaveLocation, DateTime folderDate, IConfiguration configuration)
=======
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
>>>>>>> parent of 171b8e4 (v1.9.2)
        {
            Logger.LogDebug($"Entering FolderCreation.GetReportSpecificFolderPath(reportType: {reportType}, base: {baseSaveLocation}, folderDate: {folderDate:d})");

<<<<<<< HEAD
            Logger.LogDebug($"Entering FolderCreation.GetReportSpecificFolderPath(reportType: {reportType}, base: '{baseSaveLocation}', folderDate: {folderDate:d})");

            string reportTypeConfigKey = ReportTypeHelper.GetConfigKeyForFolderName(reportType); // e.g., "Daily", "Daily5Day1k"
            string fullConfigPathForFolderName = $"{AppConfigKeys.OperationalParameters.ReportTypeFolderNames.Base}:{reportTypeConfigKey}";

            // Default folder name if not found in config (e.g., "Daily Reports", "Custom Reports")
            string defaultFolderName = ReportTypeHelper.GetDisplayString(reportType); // Using display string as a base for default
            if (reportType != ReportType.Unknown && !defaultFolderName.EndsWith("Reports", StringComparison.OrdinalIgnoreCase) && !defaultFolderName.EndsWith("Report", StringComparison.OrdinalIgnoreCase))
            {
                defaultFolderName += " Reports"; // Append " Reports" for a more descriptive default folder
            }


            string reportTypeFolder = configuration.GetValue<string>(fullConfigPathForFolderName, defaultFolderName) ?? defaultFolderName;

            if (reportTypeFolder == defaultFolderName && configuration[fullConfigPathForFolderName] == null)
            {
                Logger.LogWarning($"Configuration key '{fullConfigPathForFolderName}' not found for report type {reportType}. Using default folder name: '{defaultFolderName}'.");
            }
            else
            {
                Logger.LogDebug($"Using folder name '{reportTypeFolder}' for report type {reportType} (from config key '{fullConfigPathForFolderName}' or default).");
            }

=======
            if (string.IsNullOrWhiteSpace(baseSaveLocation))
            {
                Logger.LogError("Base save location provided to GetReportSpecificFolderPath is null or empty.");
                return null;
            }

            string reportTypeFolder;
>>>>>>> parent of 171b8e4 (v1.9.2)
            string yearFolder = string.Empty;
            string subFolder = string.Empty;
            string weekFolder = string.Empty;

            switch (reportType)
            {
<<<<<<< HEAD
                case ReportType.Daily:
                case ReportType.Daily5Day1k:
                case ReportType.Weekly:
                    yearFolder = folderDate.ToString("yyyy");
                    subFolder = folderDate.ToString("MMMM", CultureInfo.InvariantCulture);
                    weekFolder = $"Week {GetWeekOfMonth(folderDate)}";
                    break;
                case ReportType.Monthly:
                    yearFolder = folderDate.ToString("yyyy");
                    subFolder = folderDate.ToString("MMM yy", CultureInfo.InvariantCulture);
                    break;
                case ReportType.Quarterly:
=======
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
>>>>>>> parent of 171b8e4 (v1.9.2)
                    yearFolder = folderDate.ToString("yyyy");
                    int quarter = (folderDate.Month - 1) / 3 + 1;
                    DateTime quarterStartDate = new(folderDate.Year, (quarter - 1) * 3 + 1, 1);
                    DateTime quarterEndDate = quarterStartDate.AddMonths(3).AddDays(-1);
                    subFolder = $"{quarterStartDate:MMM} to {quarterEndDate:MMM}";
                    break;
<<<<<<< HEAD
                case ReportType.Annual:
                    yearFolder = folderDate.ToString("yyyy");
                    break;
                case ReportType.Custom:
                    yearFolder = folderDate.ToString("yyyy");
                    subFolder = folderDate.ToString("yyyy-MM-dd_HHmmss");
                    break;
                default: // Includes ReportType.Unknown
                    Logger.LogWarning($"Unhandled report type '{reportType}' in GetReportSpecificFolderPath switch. Path will be '{baseSaveLocation}\\{reportTypeFolder}'.");
=======
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
>>>>>>> parent of 171b8e4 (v1.9.2)
                    break;
            }

            string? fullPath = null;
            try
            {
                fullPath = Path.Combine(baseSaveLocation, reportTypeFolder);
<<<<<<< HEAD
                if (!string.IsNullOrEmpty(yearFolder)) fullPath = Path.Combine(fullPath, yearFolder);
                if (!string.IsNullOrEmpty(subFolder)) fullPath = Path.Combine(fullPath, subFolder);
                if (!string.IsNullOrEmpty(weekFolder)) fullPath = Path.Combine(fullPath, weekFolder);
            }
            catch (ArgumentException ex)
            {
                Logger.LogError($"Error combining path segments for report type {reportType}: {ex.Message}. Segments: Base='{baseSaveLocation}', TypeFolder='{reportTypeFolder}', Year='{yearFolder}', Sub='{subFolder}', Week='{weekFolder}'", ex);
                return null;
=======

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
>>>>>>> parent of 171b8e4 (v1.9.2)
            }
            Logger.LogDebug($"Exiting FolderCreation.GetReportSpecificFolderPath. Result: {fullPath ?? "null"}");
            return fullPath;
        }


        /// <summary>
        /// Calculates the week number of a given date within its month.
<<<<<<< HEAD
        /// Assumes weeks start on Monday.
        /// </summary>
        /// <param name="date">The date for which to calculate the week number within its month.</param>
        /// <returns>The week number (typically 1-5).</returns>
        public static int GetWeekOfMonth(DateTime date)
        {
            DateTime firstDayOfMonth = new DateTime(date.Year, date.Month, 1);
            int firstDayOfWeekValue = ((int)firstDayOfMonth.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
            int weekOfMonth = (date.Day + firstDayOfWeekValue - 1) / 7 + 1;
            return weekOfMonth;
        }
        #endregion

        // Private helper methods GetReportTypeKeyByIndex and GetDefaultReportTypeFolderName are removed
        // as their functionality is now provided by ReportTypeHelper.
=======
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
>>>>>>> parent of 171b8e4 (v1.9.2)
    }
}
