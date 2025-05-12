// C# 10+ Features
using QuoteConversionReportAutomation.Services.Logging; // For Logger

namespace QuoteConversionReportAutomation.Managers
{
    /// <summary>
    /// Provides static methods for archiving old report files.
    /// </summary>
    public static class ReportArchiver
    {
        private const string ArchiveFolderName = "Archive";
        private const int DefaultArchiveRawOlderThanDays = 30;

        /// <summary>
        /// Asynchronously archives old final report folders (previous years) and raw report files.
        /// </summary>
        /// <param name="finalReportBaseDir">The base directory for final processed reports (e.g., ExcelFinalSaveLocation).</param>
        /// <param name="rawReportBaseDir">The base directory for raw exported reports (e.g., RawReportExportBaseDir).</param>
        /// <param name="archiveRawOlderThanDays">Raw files older than this number of days will be archived.</param>
        public static async Task ArchiveOldReportsAsync(string? finalReportBaseDir, string? rawReportBaseDir, int? archiveRawOlderThanDays)
        {
            Logger.LogInfo("Starting report archiving process...");
            int daysThreshold = archiveRawOlderThanDays ?? DefaultArchiveRawOlderThanDays;

            // --- Archive Final Reports (Previous Years) ---
            if (!string.IsNullOrWhiteSpace(finalReportBaseDir) && Directory.Exists(finalReportBaseDir))
            {
                await ArchivePreviousYearFoldersAsync(finalReportBaseDir);
            }
            else
            {
                Logger.LogWarning($"Final report base directory '{finalReportBaseDir}' is invalid or does not exist. Skipping final report archiving.");
            }

            // --- Archive Raw Reports (Older Files) ---
            if (!string.IsNullOrWhiteSpace(rawReportBaseDir) && Directory.Exists(rawReportBaseDir))
            {
                await ArchiveOldRawFilesAsync(rawReportBaseDir, daysThreshold);
            }
            else
            {
                Logger.LogWarning($"Raw report base directory '{rawReportBaseDir}' is invalid or does not exist. Skipping raw report archiving.");
            }

            Logger.LogInfo("Report archiving process finished.");
        }

        /// <summary>
        /// Archives year folders (e.g., "2024") found directly within report type subfolders
        /// (e.g., "Weekly Reports", "Monthly Reports") if the year is before the current year.
        /// Moves them to an "Archive" folder at the same level. If the destination year folder
        /// already exists, it merges the contents by moving non-existing files/subfolders.
        /// Example: Moves ..\Estimates\Weekly Reports\2024 to ..\Estimates\Archive\Weekly Reports\2024
        /// </summary>
        private static async Task ArchivePreviousYearFoldersAsync(string finalReportBaseDir)
        {
            Logger.LogInfo($"Checking final report directory for previous year folders to archive/merge: {finalReportBaseDir}");
            int currentYear = DateTime.Now.Year;
            string archiveBaseDir = Path.Combine(finalReportBaseDir, ArchiveFolderName);

            try
            {
                Directory.CreateDirectory(archiveBaseDir); // Ensure base archive folder exists

                // Iterate through report type subdirectories (e.g., Daily Reports, Weekly Reports)
                foreach (var reportTypeDir in Directory.EnumerateDirectories(finalReportBaseDir))
                {
                    string reportTypeName = Path.GetFileName(reportTypeDir);
                    // Skip the Archive folder itself
                    if (reportTypeName.Equals(ArchiveFolderName, StringComparison.OrdinalIgnoreCase)) continue;

                    Logger.LogDebug($"Checking report type folder: {reportTypeName}");

                    // Iterate through year subdirectories within the report type folder
                    foreach (var yearDir in Directory.EnumerateDirectories(reportTypeDir))
                    {
                        string yearFolderName = Path.GetFileName(yearDir);
                        if (int.TryParse(yearFolderName, out int year) && year < currentYear)
                        {
                            // This is a previous year folder, attempt to archive/merge it
                            string archiveReportTypeDir = Path.Combine(archiveBaseDir, reportTypeName);
                            string destinationYearDir = Path.Combine(archiveReportTypeDir, yearFolderName);

                            try
                            {
                                Directory.CreateDirectory(archiveReportTypeDir); // Ensure target report type archive folder exists

                                // *** UPDATED LOGIC: Check if destination exists, then merge or move ***
                                if (Directory.Exists(destinationYearDir))
                                {
                                    // Destination exists - Merge contents
                                    Logger.LogInfo($"Archive destination '{destinationYearDir}' already exists. Merging contents from '{yearDir}'.");
                                    await MergeDirectoryContentsAsync(yearDir, destinationYearDir);

                                    // Attempt to delete the original source directory *only if empty* after merge
                                    try
                                    {
                                        if (!Directory.EnumerateFileSystemEntries(yearDir).Any())
                                        {
                                            Directory.Delete(yearDir, false); // Delete non-recursively (should be empty)
                                            Logger.LogInfo($"Successfully deleted empty source folder after merge: '{yearDir}'");
                                        }
                                        else
                                        {
                                            Logger.LogWarning($"Source folder '{yearDir}' was not empty after merge attempt. Manual cleanup might be needed.");
                                        }
                                    }
                                    catch (Exception deleteEx)
                                    {
                                        Logger.LogWarning($"Failed to delete source folder '{yearDir}' after merge attempt: {deleteEx.Message}");
                                    }
                                }
                                else
                                {
                                    // Destination does not exist - Move the entire directory
                                    Logger.LogInfo($"Archiving previous year folder: '{yearDir}' to '{destinationYearDir}'");
                                    await Task.Run(() => Directory.Move(yearDir, destinationYearDir)); // Use Move for atomic operation if possible
                                    Logger.LogInfo($"Successfully archived year folder: {yearFolderName} for {reportTypeName}");
                                }
                                // *** END UPDATED LOGIC ***
                            }
                            catch (Exception ex) // Catch errors during move/merge
                            {
                                Logger.LogError($"Failed to archive or merge year folder '{yearDir}' to '{destinationYearDir}': {ex.Message}");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error during final report year folder archiving in '{finalReportBaseDir}': {ex.Message}");
            }
        }

        /// <summary>
        /// Merges the contents of a source directory into a destination directory.
        /// Moves files and subdirectories from source to destination only if they don't already exist in the destination.
        /// </summary>
        private static async Task MergeDirectoryContentsAsync(string sourceDir, string destDir)
        {
            Logger.LogDebug($"Starting merge from '{sourceDir}' to '{destDir}'");

            // Move subdirectories first (recursively, although structure is likely flat here)
            foreach (var dirPath in Directory.GetDirectories(sourceDir, "*", SearchOption.TopDirectoryOnly)) // Assuming flat structure within year folder (e.g., month/week)
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
                        // If subdirectory exists, could implement recursive merge here if needed
                        Logger.LogWarning($"Subdirectory '{dirName}' already exists in archive destination '{destDir}'. Skipping merge for this subdirectory.");
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError($"Failed to merge subdirectory '{dirPath}' to '{destSubDirPath}': {ex.Message}");
                }
            }

            // Move files
            foreach (var filePath in Directory.GetFiles(sourceDir, "*", SearchOption.TopDirectoryOnly)) // Assuming flat structure within year folder
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
                        Logger.LogWarning($"File '{fileName}' already exists in archive destination '{destDir}'. Skipping merge for this file.");
                        // Optionally delete the source file if skipping? Or leave it for manual check.
                        // try { File.Delete(filePath); } catch { /* Ignore delete error */ }
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError($"Failed to merge file '{filePath}' to '{destFilePath}': {ex.Message}");
                }
            }
            Logger.LogDebug($"Finished merge attempt for '{sourceDir}'");
        }


        /// <summary>
        /// Archives individual raw report files (.xlsx) older than the specified number of days.
        /// It searches recursively within each report type folder (e.g., Daily Reports) and moves
        /// old files to an "Archive\YYYY-MM" subfolder within that report type folder, based on the file's LastWriteTime.
        /// Example: Moves ..\Exports\Daily Reports\...\OldFile.xlsx to ..\Exports\Daily Reports\Archive\2025-03\OldFile.xlsx
        /// </summary>
        private static async Task ArchiveOldRawFilesAsync(string rawReportBaseDir, int daysThreshold)
        {
            Logger.LogInfo($"Checking raw report directory for files older than {daysThreshold} days to archive: {rawReportBaseDir}");
            DateTime cutoffDate = DateTime.Now.Date.AddDays(-daysThreshold);

            try
            {
                // Iterate through report type subdirectories (e.g., Daily Reports, Weekly Reports)
                foreach (var reportTypeDir in Directory.EnumerateDirectories(rawReportBaseDir))
                {
                    string reportTypeName = Path.GetFileName(reportTypeDir);
                    // Skip potential Archive folder at this level if it somehow exists
                    if (reportTypeName.Equals(ArchiveFolderName, StringComparison.OrdinalIgnoreCase)) continue;

                    Logger.LogDebug($"Checking raw report type folder: {reportTypeName}");

                    // Base archive folder within the report type directory
                    string archiveBaseDirForType = Path.Combine(reportTypeDir, ArchiveFolderName);

                    // Find all .xlsx files recursively within the report type directory, EXCLUDING the Archive folder itself
                    var filesToArchive = Directory.EnumerateFiles(reportTypeDir, "*.xlsx", SearchOption.AllDirectories)
                                                  .Where(f => !f.StartsWith(archiveBaseDirForType, StringComparison.OrdinalIgnoreCase) && // Exclude files already in Archive
                                                              File.GetLastWriteTime(f) < cutoffDate);

                    foreach (var filePath in filesToArchive)
                    {
                        try
                        {
                            FileInfo fileInfo = new FileInfo(filePath);
                            DateTime fileDate = fileInfo.LastWriteTime;
                            string yearMonthFolder = fileDate.ToString("yyyy-MM"); // Format for subfolder name

                            // Construct the final destination directory including year-month
                            string destinationDir = Path.Combine(archiveBaseDirForType, yearMonthFolder);
                            Directory.CreateDirectory(destinationDir); // Ensure yyyy-MM archive subfolder exists

                            string fileName = fileInfo.Name;
                            string destinationPath = Path.Combine(destinationDir, fileName);

                            // Handle potential name collisions in the archive folder
                            if (File.Exists(destinationPath))
                            {
                                string uniqueName = $"{Path.GetFileNameWithoutExtension(fileName)}_{DateTime.Now:yyyyMMddHHmmssfff}{Path.GetExtension(fileName)}";
                                destinationPath = Path.Combine(destinationDir, uniqueName);
                                Logger.LogWarning($"Archive file '{fileName}' already exists in target '{destinationDir}'. Archiving as '{uniqueName}'.");
                            }

                            Logger.LogInfo($"Archiving raw file: '{filePath}' to '{destinationPath}'");
                            await Task.Run(() => fileInfo.MoveTo(destinationPath)); // Use MoveTo
                        }
                        catch (Exception ex)
                        {
                            Logger.LogError($"Failed to archive raw file '{filePath}': {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error during raw report file archiving in '{rawReportBaseDir}': {ex.Message}");
            }
        }
    }
}