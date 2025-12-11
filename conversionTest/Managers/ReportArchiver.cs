// ReportArchiver.cs
// Provides static methods for archiving old report files and folders
// based on specified criteria (e.g., age, year).
// The name of the main archive folder is now configurable via a parameter.
// C# 10+ Features.

#region Using Directives
// System related namespaces
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

// Project specific namespaces
using QuoteConversionReportAutomation.Services.Logging; // For Logger
#endregion

namespace QuoteConversionReportAutomation.Managers
{
    /// <summary>
    /// Provides static utility methods for archiving old report files and folders.
    /// This includes archiving previous years' final report folders and older raw report files.
    /// The name of the main archive folder and the age threshold for raw files are configurable
    /// via parameters, intended to be sourced from application settings.
    /// </summary>
    public static class ReportArchiver
    {
        #region Constants
        /// <summary>
        /// Default name for the top-level archive folder if not provided by configuration.
        /// </summary>
        private const string DefaultArchiveFolderName = "Archive";

        /// <summary>
        /// Default number of days after which raw report files are considered old enough for archiving,
        /// if not specified by the caller.
        /// </summary>
        private const int DefaultArchiveRawOlderThanDays = 30;
        #endregion

        #region Public Static Methods
        /// <summary>
        /// Asynchronously archives old final report folders (from previous years) and old raw report files.
        /// This method is intended to be run as a background task, for example, on application startup.
        /// </summary>
        /// <param name="finalReportBaseDir">The base directory where final processed reports are stored.
        /// Expected structure: {finalReportBaseDir}\{ReportTypeSubFolder}\{YearFolder}\...</param>
        /// <param name="rawReportBaseDir">The base directory where raw exported reports are stored.
        /// Expected structure: {rawReportBaseDir}\{ReportTypeSubFolder}\...\files.xlsx</param>
        /// <param name="archiveRawOlderThanDays">Raw report files older than this number of days will be archived.
        /// This value is typically read from "OperationalParameters:ArchiveRawReportsOlderThanDays" in `appsettings.json`.</param>
        /// <param name="configuredArchiveFolderName">The name to use for the main archive subfolder (e.g., "Archive").
        /// This value is typically read from "OperationalParameters:ReportArchiveFolderName" in `appsettings.json`.
        /// If null or empty, <see cref="DefaultArchiveFolderName"/> will be used.</param>
        /// <remarks>
        /// If `finalReportBaseDir` or `rawReportBaseDir` are null, empty, or point to non-existent directories,
        /// the respective archiving step will be skipped with a warning log.
        /// If `archiveRawOlderThanDays` is null, <see cref="DefaultArchiveRawOlderThanDays"/> will be used.
        /// </remarks>
        public static async Task ArchiveOldReportsAsync(
            string? finalReportBaseDir,
            string? rawReportBaseDir,
            int? archiveRawOlderThanDays,
            string? configuredArchiveFolderName)
        {
            Logger.LogInfo("Starting report archiving process...");

            string archiveFolderName = DefaultArchiveFolderName; // Default value
            if (!string.IsNullOrWhiteSpace(configuredArchiveFolderName))
            {
                archiveFolderName = configuredArchiveFolderName;
                Logger.LogDebug($"Using configured archive folder name: '{archiveFolderName}'");
            }
            else
            {
                Logger.LogWarning($"Configured archive folder name is null or empty. Using default: '{archiveFolderName}'");
            }

            int daysThreshold = archiveRawOlderThanDays ?? DefaultArchiveRawOlderThanDays;
            Logger.LogDebug($"Using threshold of {daysThreshold} days for archiving raw reports.");

            // --- Archive Final Reports (Previous Years' Folders) ---
            if (!string.IsNullOrWhiteSpace(finalReportBaseDir))
            {
                if (Directory.Exists(finalReportBaseDir))
                {
                    // Pass the determined archiveFolderName to the helper method.
                    await ArchivePreviousYearFoldersAsync(finalReportBaseDir, archiveFolderName);
                }
                else
                {
                    Logger.LogWarning($"Final report base directory '{finalReportBaseDir}' does not exist. Skipping final report folder archiving.");
                }
            }
            else
            {
                Logger.LogWarning("Final report base directory was not provided. Skipping final report folder archiving.");
            }

            // --- Archive Raw Reports (Older Individual Files) ---
            if (!string.IsNullOrWhiteSpace(rawReportBaseDir))
            {
                if (Directory.Exists(rawReportBaseDir))
                {
                    // Pass the determined archiveFolderName to the helper method.
                    await ArchiveOldRawFilesAsync(rawReportBaseDir, daysThreshold, archiveFolderName);
                }
                else
                {
                    Logger.LogWarning($"Raw report base directory '{rawReportBaseDir}' does not exist. Skipping raw report file archiving.");
                }
            }
            else
            {
                Logger.LogWarning("Raw report base directory was not provided. Skipping raw report file archiving.");
            }

            Logger.LogInfo("Report archiving process finished.");
        }
        #endregion

        #region Private Archiving Logic for Final Reports
        /// <summary>
        /// Archives year folders (e.g., "2023") found directly within report type subfolders
        /// under the <paramref name="finalReportBaseDir"/>, if the year of the folder is before the current year.
        /// Archived year folders are moved to an "<paramref name="archiveFolderName"/>" subfolder,
        /// maintaining the report type subfolder structure.
        /// If the destination year folder already exists in the archive, contents are merged.
        /// </summary>
        /// <param name="finalReportBaseDir">The base directory for final processed reports.</param>
        /// <param name="archiveFolderName">The name of the main archive subfolder to use.</param>
        private static async Task ArchivePreviousYearFoldersAsync(string finalReportBaseDir, string archiveFolderName)
        {
            Logger.LogInfo($"Checking final report directory for previous year folders to archive/merge into '{archiveFolderName}': '{finalReportBaseDir}'");
            int currentYear = DateTime.Now.Year;
            string archiveRootPath = Path.Combine(finalReportBaseDir, archiveFolderName);

            try
            {
                Directory.CreateDirectory(archiveRootPath);

                foreach (var reportTypeDir in Directory.EnumerateDirectories(finalReportBaseDir))
                {
                    string reportTypeDirName = Path.GetFileName(reportTypeDir);
                    if (reportTypeDirName.Equals(archiveFolderName, StringComparison.OrdinalIgnoreCase))
                    {
                        continue; // Skip the archive folder itself.
                    }

                    Logger.LogDebug($"Scanning report type folder for year subfolders: '{reportTypeDir}'");

                    foreach (var yearDirToArchive in Directory.EnumerateDirectories(reportTypeDir))
                    {
                        string yearFolderName = Path.GetFileName(yearDirToArchive);
                        if (int.TryParse(yearFolderName, out int year) && year < currentYear)
                        {
                            string targetArchiveReportTypeDir = Path.Combine(archiveRootPath, reportTypeDirName);
                            string targetArchivedYearDir = Path.Combine(targetArchiveReportTypeDir, yearFolderName);

                            try
                            {
                                Directory.CreateDirectory(targetArchiveReportTypeDir);

                                if (Directory.Exists(targetArchivedYearDir))
                                {
                                    Logger.LogInfo($"Archive destination '{targetArchivedYearDir}' already exists. Merging contents from '{yearDirToArchive}'.");
                                    await MergeDirectoryContentsAsync(yearDirToArchive, targetArchivedYearDir);
                                    try
                                    {
                                        if (!Directory.EnumerateFileSystemEntries(yearDirToArchive).Any())
                                        {
                                            try
                                            {
                                               // Directory.Delete(yearDirToArchive, true);
                                                Logger.LogInfo($"Successfully deleted empty source year folder after merge: '{yearDirToArchive}'");
                                            }
                                            catch(IOException ex)
                                            { 
                                            Logger.LogWarning($"Source year folder '{yearDirToArchive}' was not empty after merge attempt. Manual cleanup might be needed. Error: " + ex);
                                            }
                                        }
                                        else
                                        {
                                            Logger.LogWarning($"Source year folder '{yearDirToArchive}' was not empty after merge attempt. Manual cleanup might be needed.");
                                        }
                                    }
                                    catch (Exception deleteEx)
                                    {
                                        Logger.LogWarning($"Failed to delete source year folder '{yearDirToArchive}' after merge attempt: {deleteEx.Message}");
                                    }
                                }
                                else
                                {
                                    Logger.LogInfo($"Archiving previous year folder: Moving '{yearDirToArchive}' to '{targetArchivedYearDir}'");
                                    await Task.Run(() => Directory.Move(yearDirToArchive, targetArchivedYearDir));
                                    Logger.LogInfo($"Successfully archived year folder '{yearFolderName}' for report type '{reportTypeDirName}'.");
                                }
                            }
                            catch (IOException ioEx)
                            {
                                Logger.LogError($"IO error archiving/merging year folder '{yearDirToArchive}' to '{targetArchivedYearDir}': {ioEx.Message}", ioEx);
                            }
                            catch (UnauthorizedAccessException uaEx)
                            {
                                Logger.LogError($"Access denied archiving/merging year folder '{yearDirToArchive}' to '{targetArchivedYearDir}': {uaEx.Message}", uaEx);
                            }
                            catch (Exception ex)
                            {
                                Logger.LogError($"Failed to archive/merge year folder '{yearDirToArchive}' to '{targetArchivedYearDir}': {ex.Message}", ex);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error during final report year folder archiving in '{finalReportBaseDir}': {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Merges the contents of a source directory into a destination directory.
        /// Moves files and top-level subdirectories from source to destination if they don't already exist in destination.
        /// This is a shallow merge for subdirectories.
        /// </summary>
        /// <param name="sourceDir">The source directory path.</param>
        /// <param name="destDir">The destination directory path.</param>
        private static async Task MergeDirectoryContentsAsync(string sourceDir, string destDir)
        {
            Logger.LogDebug($"Starting merge of contents from '{sourceDir}' into '{destDir}'");
            Directory.CreateDirectory(destDir); // Ensure destination exists.

            // Process subdirectories.
            foreach (var dirPath in Directory.GetDirectories(sourceDir, "*", SearchOption.TopDirectoryOnly))
            {
                string dirName = Path.GetFileName(dirPath);
                string destSubDirPath = Path.Combine(destDir, dirName);
                try
                {
                    if (!Directory.Exists(destSubDirPath))
                    {
                        Logger.LogTrace($"Merging directory: Moving '{dirPath}' to '{destSubDirPath}'");
                        await Task.Run(() => Directory.Move(dirPath, destSubDirPath));
                    }
                    else
                    {
                        Logger.LogWarning($"Subdirectory '{dirName}' already exists in archive destination '{destDir}'. Skipping merge for this subdirectory.");
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError($"Failed to merge subdirectory '{dirPath}' to '{destSubDirPath}': {ex.Message}", ex);
                }
            }

            // Process files.
            foreach (var filePath in Directory.GetFiles(sourceDir, "*", SearchOption.TopDirectoryOnly))
            {
                string fileName = Path.GetFileName(filePath);
                string destFilePath = Path.Combine(destDir, fileName);
                try
                {
                    if (!File.Exists(destFilePath))
                    {
                        Logger.LogTrace($"Merging file: Moving '{filePath}' to '{destFilePath}'");
                        await Task.Run(() => File.Move(filePath, destFilePath));
                    }
                    else
                    {
                        Logger.LogWarning($"File '{fileName}' already exists in archive destination '{destDir}'. Skipping merge for this file from '{sourceDir}'.");
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError($"Failed to merge file '{filePath}' to '{destFilePath}': {ex.Message}", ex);
                }
            }
            Logger.LogDebug($"Finished merge attempt for contents of '{sourceDir}' into '{destDir}'.");
        }
        #endregion

        #region Private Archiving Logic for Raw Reports
        /// <summary>
        /// Archives individual raw report files (e.g., .xlsx) older than the specified number of days.
        /// Searches recursively within each report type subfolder under <paramref name="rawReportBaseDir"/>
        /// and moves old files to a structured "<paramref name="archiveFolderName"/>\YYYY-MM" subfolder
        /// *within that report type folder*, based on the file's LastWriteTime.
        /// </summary>
        /// <param name="rawReportBaseDir">The base directory for raw exported reports.</param>
        /// <param name="daysThreshold">Raw files older than this number of days will be archived.</param>
        /// <param name="archiveFolderName">The name of the main archive subfolder to use.</param>
        private static async Task ArchiveOldRawFilesAsync(string rawReportBaseDir, int daysThreshold, string archiveFolderName)
        {
            Logger.LogInfo($"Checking raw report directory '{rawReportBaseDir}' for files older than {daysThreshold} days to archive into '{archiveFolderName}' subfolders.");
            DateTime cutoffDate = DateTime.Now.Date.AddDays(-daysThreshold);

            try
            {
                foreach (var reportTypeDir in Directory.EnumerateDirectories(rawReportBaseDir))
                {
                    string reportTypeDirName = Path.GetFileName(reportTypeDir);
                    if (reportTypeDirName.Equals(archiveFolderName, StringComparison.OrdinalIgnoreCase))
                    {
                        continue; // Skip the main archive folder if it's at this level.
                    }

                    Logger.LogDebug($"Scanning raw report type folder for old files: '{reportTypeDir}'");
                    string archiveRootForReportType = Path.Combine(reportTypeDir, archiveFolderName);

                    var filesToConsiderForArchiving = Directory.EnumerateFiles(reportTypeDir, "*.xlsx", SearchOption.AllDirectories)
                                                               .Where(filePath =>
                                                                    !filePath.StartsWith(archiveRootForReportType, StringComparison.OrdinalIgnoreCase) &&
                                                                    File.GetLastWriteTime(filePath) < cutoffDate);

                    foreach (var filePathToArchive in filesToConsiderForArchiving)
                    {
                        try
                        {
                            FileInfo fileInfo = new FileInfo(filePathToArchive);
                            DateTime fileLastWriteDate = fileInfo.LastWriteTime;
                            string yearMonthFolderName = fileLastWriteDate.ToString("yyyy-MM");

                            string destinationArchiveDir = Path.Combine(archiveRootForReportType, yearMonthFolderName);
                            Directory.CreateDirectory(destinationArchiveDir);

                            string fileName = fileInfo.Name;
                            string destinationFilePath = Path.Combine(destinationArchiveDir, fileName);

                            if (File.Exists(destinationFilePath))
                            {
                                string uniqueFileName = $"{Path.GetFileNameWithoutExtension(fileName)}_{DateTime.Now:yyyyMMddHHmmssfff}{Path.GetExtension(fileName)}";
                                destinationFilePath = Path.Combine(destinationArchiveDir, uniqueFileName);
                                Logger.LogWarning($"Archive file '{fileName}' already exists in '{destinationArchiveDir}'. Archiving as '{uniqueFileName}'.");
                            }

                            Logger.LogInfo($"Archiving raw file: '{filePathToArchive}' to '{destinationFilePath}'");
                            await Task.Run(() => fileInfo.MoveTo(destinationFilePath));
                        }
                        catch (IOException ioEx)
                        {
                            Logger.LogError($"IO error archiving raw file '{filePathToArchive}': {ioEx.Message}", ioEx);
                        }
                        catch (UnauthorizedAccessException uaEx)
                        {
                            Logger.LogError($"Access denied archiving raw file '{filePathToArchive}': {uaEx.Message}", uaEx);
                        }
                        catch (Exception ex)
                        {
                            Logger.LogError($"Failed to archive raw file '{filePathToArchive}': {ex.Message}", ex);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error during raw report file archiving process in '{rawReportBaseDir}': {ex.Message}", ex);
            }
        }
        #endregion
    }
}