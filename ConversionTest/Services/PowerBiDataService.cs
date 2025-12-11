#region Using Directives

// System-related namespaces for core functionalities.
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

// Third-party namespaces for external libraries.
using Microsoft.Extensions.Configuration;
using OfficeOpenXml;

// Project-specific namespaces for application components.
using QuoteConversionReportAutomation.Helpers;
using QuoteConversionReportAutomation.Interfaces;
using QuoteConversionReportAutomation.Models;
using QuoteConversionReportAutomation.Models.Status;
using QuoteConversionReportAutomation.Services.Interfaces;
using QuoteConversionReportAutomation.Services.Logging;

#endregion

namespace QuoteConversionReportAutomation.Services.Excel
{
    #region Class Definition
    /// <summary>
    /// Implements the <see cref="IPowerBiDataService"/> to manage interactions with a central
    /// Power BI data source Excel file. It handles appending new data and ensures safe,
    /// concurrent access by using a file-locking mechanism.
    /// </summary>
    public class PowerBiDataService : IPowerBiDataService
    {
        #region Fields

        /// <summary>
        /// Provides access to the application's configuration settings.
        /// </summary>
        private readonly IConfiguration _configuration;

        /// <summary>
        /// The centralised service for broadcasting application-wide status messages.
        /// </summary>
        private readonly IStatusManagerService _statusManager;

        /// <summary>
        /// The service responsible for generating report filenames.
        /// </summary>
        private readonly IReportPathService _reportPathService;

        #endregion

        #region Constructor

        /// <summary>
        /// Initialises a new instance of the <see cref="PowerBiDataService"/> class.
        /// </summary>
        /// <param name="configuration">The application configuration, injected for settings like timeouts.</param>
        /// <param name="statusManager">The service for reporting progress to the UI.</param>
        /// <param name="reportPathService">The service for generating report filenames.</param>
        public PowerBiDataService(IConfiguration configuration, IStatusManagerService statusManager, IReportPathService reportPathService)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _statusManager = statusManager ?? throw new ArgumentNullException(nameof(statusManager));
            _reportPathService = reportPathService ?? throw new ArgumentNullException(nameof(reportPathService));
            Logger.LogTrace("PowerBiDataService instance created.");
        }

        #endregion

        #region IPowerBiDataService Implementation

        /// <inheritdoc/>
        public async Task AppendDataToPowerBIReportAsync(
            ExcelPackage sourcePackage,
            ExcelWorksheet sourceAnalysisWorksheet,
            string targetPowerBiSheetName,
            CancellationToken cancellationToken)
        {
            // Determine the full path to the central Power BI data source file.
            string destinationPowerBiFilePath = GetWeeklyReportPath();
            if (string.IsNullOrEmpty(destinationPowerBiFilePath) || !File.Exists(destinationPowerBiFilePath))
            {
                _statusManager.Post("Error: Central Power BI report file not found.", MessageType.Error);
                Logger.LogError($"Central Power BI file not found at path: '{destinationPowerBiFilePath}'");
                return;
            }

            // Check if the source worksheet exists and has data.
            if (sourceAnalysisWorksheet.Dimension == null)
            {
                Logger.LogWarning($"Source analysis sheet '{sourceAnalysisWorksheet.Name}' has no data. Skipping Power BI append.");
                return;
            }

            // Define the path for the lock file, which is used to manage concurrent access.
            string lockFilePath = destinationPowerBiFilePath + ".lock";
            FileStream? lockFileStream = null;

            try
            {
                // 1. Acquire an exclusive lock on the Power BI file to prevent other instances from writing at the same time.
                _statusManager.Post("Acquiring lock for Power BI file...", MessageType.InProgress);
                lockFileStream = await AcquireLockFileAsync(lockFilePath, cancellationToken);
                _statusManager.Post("Lock acquired. Processing Power BI file...", MessageType.InProgress);

                // 2. Perform the file modification while the lock is held.
                using (var destinationPackage = await Task.Run(() => new ExcelPackage(new FileInfo(destinationPowerBiFilePath)), cancellationToken))
                {
                    // Use the static helper to map required columns from the source analysis sheet.
                    var analysisColumnMap = ExcelHelper.MapColumnIndices(sourceAnalysisWorksheet, 5, new[] { "Customer", "SOURCE FILE" });
                    int customerCol = analysisColumnMap["Customer"];
                    int sourceFileCol = analysisColumnMap["SOURCE FILE"];

                    // Get the target worksheet in the Power BI file, or create it if it doesn't exist.
                    ExcelWorksheet? destinationWorksheet = destinationPackage.Workbook.Worksheets[targetPowerBiSheetName];
                    if (destinationWorksheet == null)
                    {
                        destinationWorksheet = destinationPackage.Workbook.Worksheets.Add(targetPowerBiSheetName);
                        // If creating the sheet, copy the header rows from the source analysis sheet.
                        sourceAnalysisWorksheet.Cells[1, 1, 5, sourceAnalysisWorksheet.Dimension.End.Column].Copy(destinationWorksheet.Cells[1, 1]);
                    }

                    // Find the next available row in the target sheet to append new data.
                    int nextFreeRowInPowerBiSheet = await Task.Run(() => ExcelHelper.GetNextFreeRow(destinationWorksheet), cancellationToken);

                    // Copy the data row by row from the source analysis sheet to the Power BI sheet.
                    await Task.Run(() =>
                    {
                        int sourceAnalysisRowCount = sourceAnalysisWorksheet.Dimension.Rows;
                        int sourceAnalysisColCount = sourceAnalysisWorksheet.Dimension.End.Column;
                        const int startDataRowInAnalysis = 6;

                        if (sourceAnalysisRowCount >= startDataRowInAnalysis)
                        {
                            // Iterate through each row in the source analysis data.
                            for (int sourceRow = startDataRowInAnalysis; sourceRow <= sourceAnalysisRowCount; sourceRow++)
                            {
                                cancellationToken.ThrowIfCancellationRequested();
                                // Only copy rows that contain a customer name.
                                if (sourceAnalysisWorksheet.Cells[sourceRow, customerCol].Value != null)
                                {
                                    // Copy all columns for the current row.
                                    for (int col = 1; col <= sourceAnalysisColCount; col++)
                                    {
                                        destinationWorksheet.Cells[nextFreeRowInPowerBiSheet, col].Value = sourceAnalysisWorksheet.Cells[sourceRow, col].Value;
                                    }
                                    nextFreeRowInPowerBiSheet++;
                                }
                            }
                        }
                    }, cancellationToken);

                    // Save the changes to the Power BI file.
                    await destinationPackage.SaveAsync(cancellationToken);
                }
                _statusManager.Post("Data appended to Power BI source.", MessageType.Success);
            }
            catch (OperationCanceledException)
            {
                // Handle cancellation requests gracefully.
                Logger.LogWarning("Operation cancelled while processing Power BI report source.");
                _statusManager.Post("Cancelled update to Power BI source.", MessageType.Warning);
            }
            catch (Exception ex)
            {
                // Log and report any unexpected errors.
                Logger.LogError($"Error copying data to Power BI source file: {ex.Message}", ex);
                _statusManager.Post($"Error (Power BI): {ex.Message.Split('\n').FirstOrDefault()}", MessageType.Error);
            }
            finally
            {
                // 3. CRITICAL: Release the lock by disposing of the stream and deleting the lock file.
                if (lockFileStream != null)
                {
                    lockFileStream.Dispose();
                    try
                    {
                        File.Delete(lockFilePath);
                        Logger.LogInfo("Released Power BI file lock.");
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError($"CRITICAL: Failed to delete lock file '{lockFilePath}'. This may require manual intervention.", ex);
                        _statusManager.Post("CRITICAL: Failed to release Power BI file lock!", MessageType.Error);
                    }
                }
            }
        }

        #endregion

        #region Private Helper Methods

        /// <summary>
        /// Asynchronously acquires a lock file for a resource, waiting and retrying if the lock is already held.
        /// This prevents race conditions when accessing shared files.
        /// </summary>
        /// <param name="lockFilePath">The full path of the lock file to create (e.g., "report.xlsx.lock").</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A FileStream for the created lock file. The caller must dispose of this stream to release the lock.</returns>
        private async Task<FileStream> AcquireLockFileAsync(string lockFilePath, CancellationToken cancellationToken)
        {
            // Get timeout and retry settings from configuration, with sensible defaults.
            int timeoutMinutes = _configuration.GetValue<int>("OperationalParameters:FileLockTimeoutMinutes", 2);
            int retryDelayMs = _configuration.GetValue<int>("OperationalParameters:FileLockRetryDelayMs", 5000);

            // Create a combined cancellation token that respects both the user's cancellation and our internal timeout.
            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromMinutes(timeoutMinutes));
            using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

            // Loop indefinitely until the lock is acquired or the operation is cancelled/times out.
            while (true)
            {
                combinedCts.Token.ThrowIfCancellationRequested();
                FileStream? lockStream = null;
                try
                {
                    // Attempt to create a new file atomically. This will fail with an IOException if the file already exists.
                    lockStream = new FileStream(lockFilePath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
                    return lockStream; // Success: we have created the lock file and hold the stream.
                }
                catch (IOException)
                {
                    // This is the expected exception if the lock is already held by another process.
                    lockStream?.Dispose();
                    _statusManager.Post("Waiting for another process to finish with the Power BI file...", MessageType.InProgress);
                    // Wait for the specified delay before retrying.
                    await Task.Delay(retryDelayMs, combinedCts.Token);
                }
                catch
                {
                    // For any other unexpected exception, ensure the stream is disposed and re-throw the exception.
                    lockStream?.Dispose();
                    throw;
                }
            }
        }

        /// <summary>
        /// Gets the path to the weekly Power BI report file.
        /// NOTE: This path is currently hardcoded. For better flexibility, this could be moved to appsettings.json.
        /// </summary>
        /// <returns>The full path to the weekly Power BI report file.</returns>
        private string GetWeeklyReportPath()
        {
            // Use a different path for DEBUG and RELEASE builds to avoid modifying the production file during development.
#if DEBUG
            return $@"\\harlow.local\DFS\Users\{Environment.UserName}\Harlow Printing\IT - Documents\PowerBI\Quote Conversion Report\Quotes conversion data_wrangled\weekly report quotes conversion merged - copy.xlsx";
#else
            return $@"\\harlow.local\DFS\Users\{Environment.UserName}\Harlow Printing\IT - Documents\PowerBI\Quote Conversion Report\Quotes conversion data_wrangled\weekly report quotes conversion merged.xlsx";
#endif
        }

        #endregion
    }
    #endregion
}