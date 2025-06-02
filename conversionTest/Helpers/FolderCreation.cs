// FolderCreation.cs
// Utility class for creating and determining report-specific folder structures.
// Folder names for report types are now read from IConfiguration, with fallbacks.
// C# 10+ Features.

#region Using Directives
// System related namespaces
using System;
using System.Globalization; // Added for month formatting
using System.IO;

// Third-party namespaces
using Microsoft.Extensions.Configuration; // For IConfiguration

// Project specific namespaces
using QuoteConversionReportAutomation.Services.Logging; // For Logger
#endregion

namespace QuoteConversionReportAutomation.Helpers
{
    /// <summary>
    /// Utility class for creating and determining paths for report-specific folder structures.
    /// Handles Daily, "Daily (5days >= £1000)", Weekly, Monthly, Quarterly, Annual, and Custom reports.
    /// Report type folder names are read from application configuration.
    /// </summary>
    public static class FolderCreation
    {
        #region Report Type Indices
        // These constants define integer indices for different report types.
        // They must align with their usage in other parts of the application (e.g., Form1.cs, ExcelCopyData.cs).
        private const int DailyReportIndex = 0;
        private const int NewDailyReportOver1kIndex = 1; // "Daily (5days >= £1000)"
        private const int WeeklyReportIndex = 2;
        private const int MonthlyReportIndex = 3;
        private const int QuarterlyReportIndex = 4;
        private const int AnnualReportIndex = 5;
        private const int CustomReportIndex = 6;
        #endregion

        #region Public Static Methods
        /// <summary>
        /// Creates the specific folder structure for the given report type based on the provided date
        /// and returns the full path to the target folder.
        /// Folder names for report types are retrieved from the provided <paramref name="configuration"/>.
        /// </summary>
        /// <param name="reportType">The integer index representing the report type (e.g., Form1.DailyReportIndex).</param>
        /// <param name="baseSaveLocation">The root directory where report type folders will be created (e.g., ...\Estimates\ or ...\RawExports\).</param>
        /// <param name="folderDate">The date used to determine year, month, week, or timestamp subfolders.</param>
        /// <param name="configuration">The application's <see cref="IConfiguration"/> instance to retrieve folder name settings.</param>
        /// <returns>The full path to the created target folder, or null if an error occurs (e.g., invalid path, permissions).</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="baseSaveLocation"/> or <paramref name="configuration"/> is null.</exception>
        public static string? CreateReportSpecificFolder(int reportType, string baseSaveLocation, DateTime folderDate, IConfiguration configuration)
        {
            ArgumentNullException.ThrowIfNull(baseSaveLocation, nameof(baseSaveLocation));
            ArgumentNullException.ThrowIfNull(configuration, nameof(configuration));

            Logger.LogDebug($"Entering FolderCreation.CreateReportSpecificFolder(reportType: {reportType}, base: '{baseSaveLocation}', folderDate: {folderDate:d})");
            try
            {
                // Get the target folder path using the helper method, now passing configuration.
                string? targetFolderPath = GetReportSpecificFolderPath(reportType, baseSaveLocation, folderDate, configuration);

                if (string.IsNullOrEmpty(targetFolderPath))
                {
                    Logger.LogError($"Could not determine target folder path for report type {reportType} using base '{baseSaveLocation}'. Folder creation aborted.");
                    return null;
                }

                // Ensure the determined directory structure exists.
                Directory.CreateDirectory(targetFolderPath);

                Logger.LogInfo($"Ensured report output folder exists: '{targetFolderPath}'");
                return targetFolderPath;
            }
            catch (ArgumentException ex) // Catches issues from Path.Combine or invalid chars.
            {
                Logger.LogError($"Error creating report folder (ArgumentException): Invalid path components. Base: '{baseSaveLocation}'. Error: {ex.Message}", ex);
                return null;
            }
            catch (PathTooLongException ex)
            {
                Logger.LogError($"Error creating report folder (PathTooLongException): The resulting path is too long. Base: '{baseSaveLocation}'. Error: {ex.Message}", ex);
                return null;
            }
            catch (DirectoryNotFoundException ex) // Should be rare if baseSaveLocation is validated by caller.
            {
                Logger.LogError($"Error creating report folder (DirectoryNotFoundException): Part of the base path could not be found. Base: '{baseSaveLocation}'. Error: {ex.Message}", ex);
                return null;
            }
            catch (IOException ioEx) // General IO errors (disk full, etc.).
            {
                Logger.LogError($"Error creating report folder (IOException): {ioEx.Message}. Base: '{baseSaveLocation}'.", ioEx);
                return null;
            }
            catch (UnauthorizedAccessException uaEx)
            {
                Logger.LogError($"Error creating report folder (UnauthorizedAccessException): Permission denied. Base: '{baseSaveLocation}'. Error: {uaEx.Message}", uaEx);
                return null;
            }
            catch (NotSupportedException nsEx) // e.g., path format not supported.
            {
                Logger.LogError($"Error creating report folder (NotSupportedException): Path format not supported. Base: '{baseSaveLocation}'. Error: {nsEx.Message}", nsEx);
                return null;
            }
            catch (Exception ex) // Catch-all for unexpected errors.
            {
                Logger.LogError($"Unexpected error creating report folder for type {reportType} with base '{baseSaveLocation}': {ex.Message}", ex);
                return null;
            }
            finally
            {
                Logger.LogDebug("Exiting FolderCreation.CreateReportSpecificFolder");
            }
        }

        /// <summary>
        /// Determines the specific folder path based on the report type, date, and configuration, without creating the folder.
        /// The folder name for the report type itself is read from <paramref name="configuration"/>
        /// (e.g., "OperationalParameters:ReportTypeFolderNames:Daily").
        /// Structure examples:
        /// - Daily/Weekly: {Base}\{ConfiguredReportTypeFolder}\{Year}\{MonthName}\Week {Num}
        /// - Monthly:      {Base}\{ConfiguredReportTypeFolder}\{Year}\{MMM yy}
        /// - Custom:       {Base}\{ConfiguredReportTypeFolder}\{Year}\{YYYY-MM-DD_HHMMSS}
        /// </summary>
        /// <param name="reportType">The integer index representing the report type.</param>
        /// <param name="baseSaveLocation">The root directory (e.g., ...\Estimates\).</param>
        /// <param name="folderDate">The date used for determining year, month, week, or timestamp subfolders.</param>
        /// <param name="configuration">The application's <see cref="IConfiguration"/> instance.</param>
        /// <returns>The full path to the target folder, or null if the report type is invalid or a path construction error occurs.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="baseSaveLocation"/> or <paramref name="configuration"/> is null.</exception>
        public static string? GetReportSpecificFolderPath(int reportType, string baseSaveLocation, DateTime folderDate, IConfiguration configuration)
        {
            ArgumentNullException.ThrowIfNull(baseSaveLocation, nameof(baseSaveLocation));
            ArgumentNullException.ThrowIfNull(configuration, nameof(configuration));

            Logger.LogDebug($"Entering FolderCreation.GetReportSpecificFolderPath(reportType: {reportType}, base: '{baseSaveLocation}', folderDate: {folderDate:d})");

            string reportTypeKey = GetReportTypeKeyByIndex(reportType); // Get string key like "Daily", "Weekly"
            string configPathForFolderName = $"OperationalParameters:ReportTypeFolderNames:{reportTypeKey}";
            string defaultFolderName = GetDefaultReportTypeFolderName(reportType); // Fallback name

            // Get the folder name for the report type from configuration, using default if not found.
            string reportTypeFolder = configuration.GetValue<string>(configPathForFolderName, defaultFolderName) ?? defaultFolderName;
            if (reportTypeFolder == defaultFolderName && configuration[configPathForFolderName] == null)
            {
                Logger.LogWarning($"Configuration key '{configPathForFolderName}' not found for report type {reportTypeKey}. Using default folder name: '{defaultFolderName}'.");
            }
            else
            {
                Logger.LogDebug($"Using folder name '{reportTypeFolder}' for report type {reportTypeKey} (from config key '{configPathForFolderName}' or default).");
            }


            string yearFolder = string.Empty;
            string subFolder = string.Empty;
            string weekFolder = string.Empty;

            // Determine subfolder structure based on report type.
            switch (reportType)
            {
                case DailyReportIndex:
                case NewDailyReportOver1kIndex:
                case WeeklyReportIndex:
                    yearFolder = folderDate.ToString("yyyy");
                    subFolder = folderDate.ToString("MMMM", CultureInfo.InvariantCulture); // Full month name for consistency.
                    weekFolder = $"Week {GetWeekOfMonth(folderDate)}";
                    break;
                case MonthlyReportIndex:
                    yearFolder = folderDate.ToString("yyyy");
                    subFolder = folderDate.ToString("MMM yy", CultureInfo.InvariantCulture); // e.g., "May 23"
                    break;
                case QuarterlyReportIndex:
                    yearFolder = folderDate.ToString("yyyy");
                    int quarter = (folderDate.Month - 1) / 3 + 1;
                    DateTime quarterStartDate = new DateTime(folderDate.Year, (quarter - 1) * 3 + 1, 1);
                    DateTime quarterEndDate = quarterStartDate.AddMonths(3).AddDays(-1);
                    subFolder = $"{quarterStartDate:MMM} to {quarterEndDate:MMM}"; // e.g., "Apr to Jun"
                    break;
                case AnnualReportIndex:
                    // For Annual, the folderDate is typically the start or end of the financial year.
                    // The yearFolder will be based on this.
                    yearFolder = folderDate.ToString("yyyy");
                    // No further subFolder or weekFolder needed for Annual directly under the year.
                    break;
                case CustomReportIndex:
                    // Custom reports get a timestamped subfolder for uniqueness.
                    yearFolder = folderDate.ToString("yyyy"); // Group custom reports by year.
                    subFolder = folderDate.ToString("yyyy-MM-dd_HHmmss"); // Timestamped subfolder.
                    break;
                default:
                    // reportTypeFolder is already set to "Other Reports" (or configured value for "Other")
                    // No specific year/month/week structure for unknown types by default.
                    Logger.LogWarning($"Unhandled report type index '{reportType}' in GetReportSpecificFolderPath switch. Path will be '{baseSaveLocation}\\{reportTypeFolder}'.");
                    break;
            }

            string? fullPath = null;
            try
            {
                // Construct the full path by combining segments.
                fullPath = Path.Combine(baseSaveLocation, reportTypeFolder);
                if (!string.IsNullOrEmpty(yearFolder))
                {
                    fullPath = Path.Combine(fullPath, yearFolder);
                }
                if (!string.IsNullOrEmpty(subFolder))
                {
                    fullPath = Path.Combine(fullPath, subFolder);
                }
                if (!string.IsNullOrEmpty(weekFolder)) // Applicable for Daily, NewDailyReportOver1k, Weekly.
                {
                    fullPath = Path.Combine(fullPath, weekFolder);
                }
            }
            catch (ArgumentException ex) // Path.Combine can throw if segments have invalid characters.
            {
                Logger.LogError($"Error combining path segments for report type {reportType}: {ex.Message}. Segments: Base='{baseSaveLocation}', TypeFolder='{reportTypeFolder}', Year='{yearFolder}', Sub='{subFolder}', Week='{weekFolder}'", ex);
                return null; // Return null if path construction fails.
            }

            Logger.LogDebug($"Exiting FolderCreation.GetReportSpecificFolderPath. Determined path: {fullPath ?? "null"}");
            return fullPath;
        }

        /// <summary>
        /// Calculates the week number of a given date within its month.
        /// Assumes weeks start on Monday. (ISO 8601 week date system defines Monday as the first day of the week).
        /// </summary>
        /// <param name="date">The date for which to calculate the week number within its month.</param>
        /// <returns>The week number (typically 1-5, can be 6 for some month/start day combinations).</returns>
        public static int GetWeekOfMonth(DateTime date)
        {
            // Get the first day of the month for the given date.
            DateTime firstDayOfMonth = new DateTime(date.Year, date.Month, 1);

            // Determine the DayOfWeek for the first day of the month (Sunday = 0, ..., Saturday = 6).
            // Adjust so Monday = 0, Tuesday = 1, ..., Sunday = 6 for easier calculation.
            int firstDayOfWeekValue = ((int)firstDayOfMonth.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;

            // Calculate the week number.
            // (Day of the month + number of padding days from the start of that week to the first of the month - 1) / 7 + 1
            // Example: If 1st is Wednesday (firstDayOfWeekValue = 2), and date is 10th:
            // (10 + 2 - 1) / 7 + 1 = 11 / 7 + 1 = 1 + 1 = 2 (integer division).
            int weekOfMonth = (date.Day + firstDayOfWeekValue - 1) / 7 + 1;

            return weekOfMonth;
        }
        #endregion

        #region Private Helper Methods
        /// <summary>
        /// Gets a string key for the report type based on its integer index.
        /// This key is used to look up the configured folder name in `appsettings.json`
        /// under "OperationalParameters:ReportTypeFolderNames".
        /// </summary>
        /// <param name="reportTypeIndex">The integer index of the report type.</param>
        /// <returns>A string key corresponding to the report type (e.g., "Daily", "Weekly").</returns>
        private static string GetReportTypeKeyByIndex(int reportTypeIndex)
        {
            return reportTypeIndex switch
            {
                DailyReportIndex => "Daily",
                NewDailyReportOver1kIndex => "Daily5Day1k", // Key used in appsettings.json
                WeeklyReportIndex => "Weekly",
                MonthlyReportIndex => "Monthly",
                QuarterlyReportIndex => "Quarterly",
                AnnualReportIndex => "Annual",
                CustomReportIndex => "Custom",
                _ => "Other" // Fallback key
            };
        }

        /// <summary>
        /// Gets the default folder name for a report type if its name is not found in the configuration.
        /// This serves as a fallback mechanism.
        /// </summary>
        /// <param name="reportTypeIndex">The integer index of the report type.</param>
        /// <returns>A default folder name string (e.g., "Daily Reports", "Weekly Reports").</returns>
        private static string GetDefaultReportTypeFolderName(int reportTypeIndex)
        {
            return reportTypeIndex switch
            {
                DailyReportIndex => "Daily Reports",
                NewDailyReportOver1kIndex => "Daily Reports (5day 1k)",
                WeeklyReportIndex => "Weekly Reports",
                MonthlyReportIndex => "Monthly Reports",
                QuarterlyReportIndex => "Quarterly reports", // Note: "reports" vs "Reports"
                AnnualReportIndex => "Annual Reports",
                CustomReportIndex => "Custom Reports",
                _ => "Other Reports" // Default for unhandled types
            };
        }
        #endregion
    }
}