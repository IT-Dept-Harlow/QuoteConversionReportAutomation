// FolderCreation.cs
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

namespace QuoteConversionReportAutomation.Helpers
{
    /// <summary>
    /// Utility class for creating and determining paths for report-specific folder structures.
    /// Handles various report types by leveraging <see cref="ReportTypeHelper"/> and
    /// retrieving folder name configurations via <see cref="AppConfigKeys"/>.
    /// </summary>
    public static class FolderCreation
    {
        // Report Type Integer Index constants are removed. ReportType enum is used directly.

        #region Public Static Methods
        /// <summary>
        /// Creates the specific folder structure for the given report type based on the provided date
        /// and returns the full path to the target folder.
        /// Folder names for report types are retrieved from the provided <paramref name="configuration"/>.
        /// </summary>
        /// <param name="reportType">The <see cref="ReportType"/> enum value representing the report type.</param>
        /// <param name="baseSaveLocation">The root directory where report type folders will be created.</param>
        /// <param name="folderDate">The date used to determine year, month, week, or timestamp subfolders.</param>
        /// <param name="configuration">The application's <see cref="IConfiguration"/> instance to retrieve folder name settings.</param>
        /// <returns>The full path to the created target folder, or null if an error occurs.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="baseSaveLocation"/> or <paramref name="configuration"/> is null.</exception>
        public static string? CreateReportSpecificFolder(ReportType reportType, string baseSaveLocation, DateTime folderDate, IConfiguration configuration)
        {
            ArgumentNullException.ThrowIfNull(baseSaveLocation, nameof(baseSaveLocation));
            ArgumentNullException.ThrowIfNull(configuration, nameof(configuration));

            Logger.LogDebug($"Entering FolderCreation.CreateReportSpecificFolder(reportType: {reportType}, base: '{baseSaveLocation}', folderDate: {folderDate:d})");
            try
            {
                string? targetFolderPath = GetReportSpecificFolderPath(reportType, baseSaveLocation, folderDate, configuration);

                if (string.IsNullOrEmpty(targetFolderPath))
                {
                    Logger.LogError($"Could not determine target folder path for report type {reportType} using base '{baseSaveLocation}'. Folder creation aborted.");
                    return null;
                }

                Directory.CreateDirectory(targetFolderPath); // Ensure the directory structure exists.

                Logger.LogInfo($"Ensured report output folder exists: '{targetFolderPath}'");
                return targetFolderPath;
            }
            catch (ArgumentException ex)
            {
                Logger.LogError($"Error creating report folder (ArgumentException): Invalid path components. Base: '{baseSaveLocation}'. Error: {ex.Message}", ex);
                return null;
            }
            catch (PathTooLongException ex)
            {
                Logger.LogError($"Error creating report folder (PathTooLongException): Resulting path too long. Base: '{baseSaveLocation}'. Error: {ex.Message}", ex);
                return null;
            }
            catch (DirectoryNotFoundException ex)
            {
                Logger.LogError($"Error creating report folder (DirectoryNotFoundException): Base path part not found. Base: '{baseSaveLocation}'. Error: {ex.Message}", ex);
                return null;
            }
            catch (IOException ioEx)
            {
                Logger.LogError($"Error creating report folder (IOException): {ioEx.Message}. Base: '{baseSaveLocation}'.", ioEx);
                return null;
            }
            catch (UnauthorizedAccessException uaEx)
            {
                Logger.LogError($"Error creating report folder (UnauthorizedAccessException): Permission denied. Base: '{baseSaveLocation}'. Error: {uaEx.Message}", uaEx);
                return null;
            }
            catch (NotSupportedException nsEx)
            {
                Logger.LogError($"Error creating report folder (NotSupportedException): Path format not supported. Base: '{baseSaveLocation}'. Error: {nsEx.Message}", nsEx);
                return null;
            }
            catch (Exception ex)
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
        /// using keys from <see cref="AppConfigKeys.OperationalParameters.ReportTypeFolderNames"/>.
        /// </summary>
        /// <param name="reportType">The <see cref="ReportType"/> enum value representing the report type.</param>
        /// <param name="baseSaveLocation">The root directory (e.g., ...\Estimates\).</param>
        /// <param name="folderDate">The date used for determining year, month, week, or timestamp subfolders.</param>
        /// <param name="configuration">The application's <see cref="IConfiguration"/> instance.</param>
        /// <returns>The full path to the target folder, or null if path construction fails.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="baseSaveLocation"/> or <paramref name="configuration"/> is null.</exception>
        public static string? GetReportSpecificFolderPath(ReportType reportType, string baseSaveLocation, DateTime folderDate, IConfiguration configuration)
        {
            ArgumentNullException.ThrowIfNull(baseSaveLocation, nameof(baseSaveLocation));
            ArgumentNullException.ThrowIfNull(configuration, nameof(configuration));

            Logger.LogDebug($"Entering FolderCreation.GetReportSpecificFolderPath(reportType: {reportType}, base: '{baseSaveLocation}', folderDate: {folderDate:d})");

            string reportTypeConfigKey = ReportTypeHelper.GetConfigKeyForFolderName(reportType); // e.g., "Daily", "Daily5Day1k"
            string fullConfigPathForFolderName = $"{AppConfigKeys.OperationalParameters.ReportTypeFolderNames.Base}:{reportTypeConfigKey}";

            // Default folder name if not found in config (e.g., "Daily Reports", "Custom Reports")
            string defaultFolderName = ReportTypeHelper.GetDisplayString(reportType, configuration); // Using display string as a base for default
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

            string yearFolder = string.Empty;
            string subFolder = string.Empty;
            string weekFolder = string.Empty;

            switch (reportType)
            {
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
                    yearFolder = folderDate.ToString("yyyy");
                    int quarter = (folderDate.Month - 1) / 3 + 1;
                    DateTime quarterStartDate = new DateTime(folderDate.Year, (quarter - 1) * 3 + 1, 1);
                    DateTime quarterEndDate = quarterStartDate.AddMonths(3).AddDays(-1);
                    subFolder = $"{quarterStartDate:MMM} to {quarterEndDate:MMM}";
                    break;
                case ReportType.Annual:
                    yearFolder = folderDate.ToString("yyyy");
                    break;
                case ReportType.Custom:
                    yearFolder = folderDate.ToString("yyyy");
                    subFolder = folderDate.ToString("yyyy-MM-dd_HHmmss");
                    break;
                default: // Includes ReportType.Unknown
                    Logger.LogWarning($"Unhandled report type '{reportType}' in GetReportSpecificFolderPath switch. Path will be '{baseSaveLocation}\\{reportTypeFolder}'.");
                    break;
            }

            string? fullPath = null;
            try
            {
                fullPath = Path.Combine(baseSaveLocation, reportTypeFolder);
                if (!string.IsNullOrEmpty(yearFolder)) fullPath = Path.Combine(fullPath, yearFolder);
                if (!string.IsNullOrEmpty(subFolder)) fullPath = Path.Combine(fullPath, subFolder);
                if (!string.IsNullOrEmpty(weekFolder)) fullPath = Path.Combine(fullPath, weekFolder);
            }
            catch (ArgumentException ex)
            {
                Logger.LogError($"Error combining path segments for report type {reportType}: {ex.Message}. Segments: Base='{baseSaveLocation}', TypeFolder='{reportTypeFolder}', Year='{yearFolder}', Sub='{subFolder}', Week='{weekFolder}'", ex);
                return null;
            }

            Logger.LogDebug($"Exiting FolderCreation.GetReportSpecificFolderPath. Determined path: {fullPath ?? "null"}");
            return fullPath;
        }

        /// <summary>
        /// Calculates the week number of a given date within its month.
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
    }
}
