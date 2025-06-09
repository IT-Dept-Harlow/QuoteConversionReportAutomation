<<<<<<< HEAD
﻿// ExcelCopyData.cs
// Provides methods for copying data between Excel sheets, filtering,
// and performing related operations asynchronously using EPPlus.
// This version is fully refactored to use the IStatusManagerService for all progress reporting.

#region Using Directives
// System related namespaces
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

// Third-party namespaces
using Microsoft.Extensions.Configuration;
using OfficeOpenXml;
using OfficeOpenXml.Table.PivotTable;

// Project specific namespaces
using QuoteConversionReportAutomation.Configuration;
using QuoteConversionReportAutomation.Helpers;
using QuoteConversionReportAutomation.Interfaces; // For IStatusManagerService
using QuoteConversionReportAutomation.Models;
using QuoteConversionReportAutomation.Models.Status; // For MessageType
using QuoteConversionReportAutomation.Services.Logging;
#endregion

namespace QuoteConversionReportAutomation.Services.Excel
{
    /// <summary>
    /// Provides methods for copying data between Excel sheets and performing related operations asynchronously.
    /// Uses OfficeOpenXml (EPPlus) for Excel manipulation. This class reports its progress via the
    /// injected <see cref="IStatusManagerService"/>.
    /// </summary>
    public class ExcelCopyData
    {
        #region Fields
        /// <summary>
        /// Provides access to the application's configuration settings.
        /// </summary>
        private readonly IConfiguration _configuration;

        /// <summary>
        /// The centralised service for managing and broadcasting application status messages.
        /// </summary>
        private readonly IStatusManagerService _statusManager;
        #endregion

        #region Constants
        // --- Default Column Indices (1-based for EPPlus) ---
        private const int CustomerColumnIndex = 1;
        private const int NetValueColumnIndexDataSheet = 7;

        // --- Analysis Sheet Column Indices ---
        private const int AnalysisSheetNoOfEstimatesColumnIndex = 4;
        private const int AnalysisSheetSourceFileNameColumnIndex = 12;
        private const int AnalysisSheetDateColumnIndex = 13;
        private const int AnalysisSheetFinancialYearColumnIndex = 14;
        private const int AnalysisSheetFirstClearableColumn = 1;
        private const int AnalysisSheetLastClearableColumn = 14;
        #endregion

        #region Constructor
        /// <summary>
        /// Initialises a new instance of the <see cref="ExcelCopyData"/> class.
        /// Sets the EPPlus license context and stores the application configuration and status manager service.
        /// </summary>
        /// <param name="configuration">The application's configuration settings.</param>
        /// <param name="statusManager">The centralised service for status reporting.</param>
        /// <exception cref="ArgumentNullException">Thrown if configuration or statusManager is null.</exception>
        public ExcelCopyData(IConfiguration configuration, IStatusManagerService statusManager)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _statusManager = statusManager ?? throw new ArgumentNullException(nameof(statusManager));
            ExcelPackage.License.SetNonCommercialPersonal("Harlow"); // Set EPPlus license context.
            Logger.LogTrace("ExcelCopyData instance created and EPPlus license context set.");
=======
﻿// C# 10+ Features (using file-scoped namespace, global using directives if applicable elsewhere)
using OfficeOpenXml; // EPPlus library for Excel manipulation
using OfficeOpenXml.Table.PivotTable;
using QuoteConversionReportAutomation.Helpers;
using QuoteConversionReportAutomation.Services.Logging;
using System.Diagnostics; // Added for Stopwatch
using System.Globalization; // Added for NumberStyles and CultureInfo

namespace QuoteConversionReportAutomation.Services.Excel // File-scoped namespace
{
    /// <summary>
    /// Represents progress information for Excel operations.
    /// </summary>
    /// <param name="Message">The status message to display.</param>
    /// <param name="Percentage">Optional progress percentage (0-100), -1 if not applicable.</param>
    public record ProgressReport(string Message, int Percentage = -1);

    /// <summary>
    /// Provides methods for copying data between Excel sheets and performing related operations asynchronously using Tasks.
    /// Uses OfficeOpenXml (EPPlus). Ensure EPPlus license context is set in your application startup.
    /// Uses FolderCreation utility for directory structure logic.
    /// Implements filtering for "Daily (5days >= £1000)" report type by filtering the "DATA" sheet after initial copy,
    /// and subsequently filtering the "Analysis" sheet to remove customers with zero estimates.
    /// </summary>
    public class ExcelCopyData
    {
        #region Constants

        // --- Report Type Indices (Must match Form1.cs) ---
        private const int DailyReportIndex = 0;
        private const int NewDailyReportOver1kIndex = 1; // "Daily (5days >= £1000)"
        private const int WeeklyReportIndex = 2;
        private const int MonthlyReportIndex = 3;
        private const int QuarterlyReportIndex = 4;
        private const int AnnualReportIndex = 5;
        private const int CustomReportIndex = 6;


        // Constants for column indices (1-based for EPPlus access).
        private const int CustomerColumnIndex = 1;       // Column A (Used in both DATA and Analysis sheets for customer name)
        private const int NetValueColumnIndex = 7;       // Column G in DATA sheet (for filtering >= £1000) 

        // Columns in Analysis Sheet
        private const int AnalysisSheetContractStatusColumnIndex = 2; // Column B
        private const int AnalysisSheetRepColumnIndex = 3;            // Column C
        private const int AnalysisSheetNoOfEstimatesColumnIndex = 4; // Column D in Analysis Sheet ("No of Estimates")
        // Columns E, F, G are "Estimates Won", "Estimates Not Won", "% Win"
        private const int AnalysisSheetEstimatesWonColumnIndex = 5;      // Column E
        private const int AnalysisSheetEstimatesNotWonColumnIndex = 6;   // Column F
        private const int AnalysisSheetPercentWinColumnIndex = 7;        // Column G
        private const int AnalysisSheetEstimateValueColumnIndex = 8; // Column H in Analysis Sheet ("Value of Estimates")
        // Columns I, J, K are "Value of Estimates Won", "Value of Est Not Confirmed", "Value of Est Not Won" (K is often blank or error if D is 0)
        private const int AnalysisSheetValueOfEstimatesWonColumnIndex = 9;    // Column I
        private const int AnalysisSheetValueOfEstNotConfirmedColumnIndex = 10; // Column J
        private const int AnalysisSheetValueOfEstNotWonColumnIndex = 11;       // Column K

        // Other general column indices (primarily for Analysis sheet population/clearing)
        private const int DateColumnIndex = 13;          // Column M (Analysis sheet) - This is where the reportDate for comparison is written
        private const int FinancialYearColumnIndex = 14; // Column N (Analysis sheet)
        private const int SourceFileNameColumnIndex = 12; // Column L (Analysis sheet)
        private const int FirstClearableColumnAnalysis = 1; // Column A - Start of range to clear for unused rows
        private const int LastClearableColumnAnalysis = 14;  // Column N - End of range to clear for unused rows


        // --- Sheet Names ---
        private const string AnalysisSheetName = "Analysis";
        private const string MonthlyOrderPivotSheetName = "OrderPivot";
        private const string MonthlyEstimatePivotSheetName = "Estimate Success PivotTable";
        private const string PowerBISheetName = "powerBI";

        // --- Pivot Table Names ---
        private const string MonthlyOrderPivotName = "PivotTable1";
        private const string MonthlyEstimatePivotName = "PivotTable3";

        #endregion Constants

        #region Constructor
        public ExcelCopyData()
        {
            ExcelPackage.License.SetNonCommercialPersonal("Harlow");
            Logger.LogTrace("ExcelCopyData instance created.");
>>>>>>> parent of 171b8e4 (v1.9.2)
        }
        #endregion

        #region Public Instance Methods
<<<<<<< HEAD
        /// <summary>
        /// Asynchronously processes an Excel report by copying data from a source file to a template,
        /// performing filtering, calculations, and other operations based on the report type.
        /// </summary>
        /// <param name="selectedFinYear">The financial year string (e.g., "2023_24") relevant for some report types.</param>
        /// <param name="reportType">The <see cref="ReportType"/> enum value representing the type of report being processed.</param>
        /// <param name="sourceFilePath">The full path to the raw source Excel file.</param>
        /// <param name="sourceSheetNameConfigKey">The simple configuration key for the name of the sheet in the source file.</param>
        /// <param name="baseFileSaveLocation">The base directory where the final processed report will be saved.</param>
        /// <param name="templateFilePath">The full path to the Excel template file.</param>
        /// <param name="destinationDataSheetNameConfigKey">The simple configuration key for the name of the sheet in the template where data is copied.</param>
        /// <param name="startRow">The starting row (1-based) in the source sheet from which to begin copying data.</param>
        /// <param name="startCol">The starting column (1-based) in the source sheet from which to begin copying data.</param>
        /// <param name="reportDate">The primary date associated with the report, used for filename generation. Defaults to today if not specified for non-custom reports.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A task that represents the asynchronous operation. The task result is the full path to the final processed Excel file, or null if processing fails or is cancelled.</returns>
=======

>>>>>>> parent of 171b8e4 (v1.9.2)
        public async Task<string?> ProcessExcelReportAsync(
            string selectedFinYear,
            ReportType reportType,
            string sourceFilePath,
<<<<<<< HEAD
            string sourceSheetNameConfigKey,
            string baseFileSaveLocation,
            string templateFilePath,
            string destinationDataSheetNameConfigKey,
=======
            string sourceSheetName,
            string baseFileSaveLocation,
            string templateFilePath,
            string destinationDataSheetName, // Typically "DATA"
>>>>>>> parent of 171b8e4 (v1.9.2)
            int startRow = 1,
            int startCol = 1,
            DateTime reportDate = default,
            CancellationToken cancellationToken = default)
        {
            Logger.LogTrace($"Entering ProcessExcelReportAsync. ReportType: {reportType}, Source: {sourceFilePath}");
            var stopwatch = Stopwatch.StartNew();

<<<<<<< HEAD
            // Validate required string parameters to prevent downstream errors.
            ArgumentException.ThrowIfNullOrEmpty(sourceFilePath, nameof(sourceFilePath));
            ArgumentException.ThrowIfNullOrEmpty(sourceSheetNameConfigKey, nameof(sourceSheetNameConfigKey));
            ArgumentException.ThrowIfNullOrEmpty(baseFileSaveLocation, nameof(baseFileSaveLocation));
            ArgumentException.ThrowIfNullOrEmpty(templateFilePath, nameof(templateFilePath));
            ArgumentException.ThrowIfNullOrEmpty(destinationDataSheetNameConfigKey, nameof(destinationDataSheetNameConfigKey));

            // Retrieve sheet names from configuration.
            string sourceSheetName = _configuration.GetValue<string>($"{AppConfigKeys.OperationalParameters.ExcelSheetNames.Base}:{sourceSheetNameConfigKey}", "Sheet1")!;
            string destinationDataSheetName = _configuration.GetValue<string>($"{AppConfigKeys.OperationalParameters.ExcelSheetNames.Base}:{destinationDataSheetNameConfigKey}", "DATA")!;
            string analysisSheetName = _configuration.GetValue<string>(AppConfigKeys.OperationalParameters.ExcelSheetNames.TemplateAnalysisSheet, "Analysis")!;

            if (reportType == ReportType.Weekly || reportType == ReportType.Daily || reportType == ReportType.Daily5Day1k)
            {
                ArgumentException.ThrowIfNullOrEmpty(selectedFinYear, nameof(selectedFinYear));
=======
            ArgumentException.ThrowIfNullOrEmpty(sourceFilePath);
            ArgumentException.ThrowIfNullOrEmpty(sourceSheetName);
            ArgumentException.ThrowIfNullOrEmpty(baseFileSaveLocation);
            ArgumentException.ThrowIfNullOrEmpty(templateFilePath);
            ArgumentException.ThrowIfNullOrEmpty(destinationDataSheetName);

            if (reportType == WeeklyReportIndex || reportType == DailyReportIndex || reportType == NewDailyReportOver1kIndex)
            {
                ArgumentException.ThrowIfNullOrEmpty(selectedFinYear);
>>>>>>> parent of 171b8e4 (v1.9.2)
            }

            // Default report date if not provided for standard reports.
            if (reportDate == default && reportType != ReportType.Custom)
            {
                reportDate = DateTime.Today;
<<<<<<< HEAD
=======
                Logger.LogWarning($"ProcessExcelReportAsync called without a specific reportDate for non-custom report. Defaulting to Today for filename generation: {reportDate:yyyy-MM-dd}");
>>>>>>> parent of 171b8e4 (v1.9.2)
            }

            string? finalFilePath = null;
            string? tempFilePath = null;
            string? fullOutputFolderPath = null;

            try
            {
                _statusManager.Post("Starting Excel processing...", MessageType.InProgress);
                cancellationToken.ThrowIfCancellationRequested();

<<<<<<< HEAD
                // Determine and create the output folder for the report.
                Logger.LogDebug("ProcessExcelReportAsync: Determining output folder...");
                DateTime folderTimestampDate = reportType == ReportType.Custom ? DateTime.Now : reportDate;
                fullOutputFolderPath = FolderCreation.CreateReportSpecificFolder(reportType, baseFileSaveLocation, folderTimestampDate, _configuration);
                if (string.IsNullOrEmpty(fullOutputFolderPath))
                {
                    throw new InvalidOperationException("Failed to create or determine the report output folder.");
                }
                _statusManager.Post("Output folder prepared.", MessageType.InProgress);
                cancellationToken.ThrowIfCancellationRequested();

                // Create a temporary file path to work with, to avoid corrupting the template.
                tempFilePath = Path.Combine(fullOutputFolderPath, $"temp_processing_{Guid.NewGuid()}.xlsx");
                Logger.LogDebug($"ProcessExcelReportAsync: Using temporary file: {tempFilePath}");

                if (!File.Exists(templateFilePath)) throw new FileNotFoundException($"Excel template file not found: {templateFilePath}", templateFilePath);

                // Copy the template to the temporary location.
                await Task.Run(() => File.Copy(templateFilePath, tempFilePath, true), cancellationToken);
                _statusManager.Post("Template copied.", MessageType.InProgress);
                cancellationToken.ThrowIfCancellationRequested();

                _statusManager.Post("Opening Excel files...", MessageType.InProgress);
                if (!File.Exists(sourceFilePath)) throw new FileNotFoundException($"Raw source data file not found: {sourceFilePath}", sourceFilePath);

                // Open both the source (raw data) and destination (copied template) Excel packages.
                using (var sourcePackage = new ExcelPackage(new FileInfo(sourceFilePath)))
                using (var destinationPackage = new ExcelPackage(new FileInfo(tempFilePath)))
                {
                    ExcelWorksheet? sourceWorksheet = sourcePackage.Workbook.Worksheets[sourceSheetName];
                    if (sourceWorksheet == null) throw new FileNotFoundException($"Source sheet '{sourceSheetName}' not found in file '{sourceFilePath}'.");

                    ExcelWorksheet destinationDataWorksheet = GetOrCreateDestinationWorksheet(destinationPackage, destinationDataSheetName, sourceWorksheet);

                    int sourceRowCount = sourceWorksheet.Dimension?.Rows ?? 0;
                    int sourceColCount = sourceWorksheet.Dimension?.Columns ?? 0;

                    _statusManager.Post("Copying data to template...", MessageType.InProgress);
                    if (sourceRowCount >= startRow && sourceColCount >= startCol)
                    {
                        // Logic to determine the actual start row of data (skipping headers).
                        int sourceDataActualStartRow = (startRow == 1 && sourceRowCount > 1) ? 2 : startRow;
                        if (sourceRowCount >= sourceDataActualStartRow)
                        {
                            ExcelRange sourceRangeToCopy = sourceWorksheet.Cells[sourceDataActualStartRow, startCol, sourceRowCount, sourceColCount];
                            destinationDataWorksheet.Cells[2, 1].Copy(sourceRangeToCopy);
                        }
                    }

                    // Perform filtering for specific report types.
                    if (reportType == ReportType.Daily5Day1k)
                    {
                        decimal filterThreshold = _configuration.GetValue<decimal>(AppConfigKeys.OperationalParameters.Daily5Day1kFilteringThreshold, 1000m);
                        _statusManager.Post($"Filtering for values >= £{filterThreshold:N0}...", MessageType.InProgress);
                        await FilterDataSheetAsync(destinationDataWorksheet, NetValueColumnIndexDataSheet, filterThreshold, cancellationToken);
                        cancellationToken.ThrowIfCancellationRequested();
                    }

                    // Perform post-processing on the analysis sheet (e.g., populating customers, calculating formulas).
                    await ProcessPostCopyOperationsAsync(destinationPackage, destinationDataSheetName, analysisSheetName, reportType, selectedFinYear, sourceFilePath, reportDate, cancellationToken);

                    _statusManager.Post("Saving processed file...", MessageType.InProgress);
                    await destinationPackage.SaveAsync(cancellationToken);
                }

                // Wait briefly to ensure file handles are released.
                await Task.Delay(200, cancellationToken);

                // Generate the final filename and rename the temporary file.
                _statusManager.Post("Finalising file...", MessageType.InProgress);
                string generatedFileName = await Task.Run(() => GenerateFinalFileName(reportType, reportDate, DateTime.Now), cancellationToken);
                finalFilePath = Path.Combine(fullOutputFolderPath, generatedFileName);
                Logger.LogInfo($"ProcessExcelReportAsync: Generated final filename: {generatedFileName}.");

                await RenameFileWithRetryAsync(tempFilePath, finalFilePath, cancellationToken);
                tempFilePath = null; // Prevent deletion in finally block

                // Final success message is handled by the calling orchestrator.
=======
                // 1. Determine and Create Report-Specific Folder
                Logger.LogTrace("ProcessExcelReportAsync: Determining output folder using FolderCreation...");
                DateTime folderTimestampDate = reportType == CustomReportIndex ? DateTime.Now : reportDate;
                fullOutputFolderPath = FolderCreation.CreateReportSpecificFolder(reportType, baseFileSaveLocation, folderTimestampDate);
                if (fullOutputFolderPath == null)
                {
                    throw new InvalidOperationException("Failed to create or determine the report output folder using FolderCreation utility.");
                }
                progress?.Report(new ProgressReport("Output folder prepared."));
                cancellationToken.ThrowIfCancellationRequested();

                // 2. Define temporary file path
                tempFilePath = Path.Combine(fullOutputFolderPath, $"temp_{Guid.NewGuid()}.xlsx");
                Logger.LogDebug($"ProcessExcelReportAsync: Using temporary file: {tempFilePath}");

                // 3. Copy Template to Temp Location
                Logger.LogTrace($"ProcessExcelReportAsync: Copying template '{templateFilePath}' to '{tempFilePath}'...");
                await Task.Run(() => File.Copy(templateFilePath, tempFilePath, true), cancellationToken);
                progress?.Report(new ProgressReport("Template copied."));
                cancellationToken.ThrowIfCancellationRequested();

                // 4. Open Packages and Copy/Filter Data
                progress?.Report(new ProgressReport("Opening Excel files..."));
                Logger.LogTrace($"ProcessExcelReportAsync: Opening source '{sourceFilePath}' and destination '{tempFilePath}' packages...");
                using (var sourcePackage = new ExcelPackage(new FileInfo(sourceFilePath)))
                using (var destinationPackage = new ExcelPackage(new FileInfo(tempFilePath)))
                {
                    Logger.LogDebug("ProcessExcelReportAsync: Packages opened.");
                    ExcelWorksheet? sourceWorksheet = sourcePackage.Workbook.Worksheets[sourceSheetName] ?? throw new FileNotFoundException($"Source sheet '{sourceSheetName}' not found in '{sourceFilePath}'.");
                    ExcelWorksheet destinationDataWorksheet = GetOrCreateDestinationWorksheet(destinationPackage, destinationDataSheetName, sourceWorksheet); // This is the "DATA" sheet

                    int sourceRowCount = sourceWorksheet.Dimension?.Rows ?? 0;
                    int sourceColCount = sourceWorksheet.Dimension?.Columns ?? 0;
                    Logger.LogDebug($"ProcessExcelReportAsync: Source dimensions: {sourceRowCount} rows, {sourceColCount} cols. Start copy from R{startRow}C{startCol}.");

                    progress?.Report(new ProgressReport("Copying data from source to template...", 10));
                    if (sourceRowCount >= startRow && sourceColCount >= startCol)
                    {
                        int sourceDataActualStartRow = startRow;
                        if (startRow == 1 && sourceRowCount > 1)
                        {
                            sourceDataActualStartRow = startRow + 1;
                        }
                        else if (startRow == 1 && sourceRowCount <= 1)
                        {
                            Logger.LogInfo($"Source sheet '{sourceSheetName}' has only headers or is empty. No data rows to copy.");
                            sourceDataActualStartRow = sourceRowCount + 1;
                        }

                        if (sourceRowCount >= sourceDataActualStartRow)
                        {
                            ExcelRange sourceRangeToCopy = sourceWorksheet.Cells[sourceDataActualStartRow, startCol, sourceRowCount, sourceColCount];
                            ExcelRange destStartCellForData = destinationDataWorksheet.Cells[2, 1];
                            sourceRangeToCopy.Copy(destStartCellForData);
                            Logger.LogInfo($"Full data copied from '{sourceSheetName}' (Row {sourceDataActualStartRow} onwards) to '{destinationDataSheetName}' (Row 2 onwards).");
                        }
                    }
                    else
                    {
                        Logger.LogWarning($"Source sheet '{sourceSheetName}' has no data to copy (Rows: {sourceRowCount}, StartRow: {startRow}) or start column is out of bounds.");
                    }
                    progress?.Report(new ProgressReport("Initial data copy complete.", 20));
                    cancellationToken.ThrowIfCancellationRequested();

                    if (reportType == NewDailyReportOver1kIndex)
                    {
                        progress?.Report(new ProgressReport($"Filtering 'DATA' sheet for values >= £1000...", 25));
                        await FilterDataSheetAsync(destinationDataWorksheet, NetValueColumnIndex, 1000m, progress, cancellationToken);
                        Logger.LogInfo($"'DATA' sheet filtered for report type {NewDailyReportOver1kIndex}.");
                        cancellationToken.ThrowIfCancellationRequested();
                    }

                    progress?.Report(new ProgressReport("Data preparation complete.", 30));

                    Logger.LogDebug("ProcessExcelReportAsync: Starting post-copy operations...");
                    await ProcessPostCopyOperationsAsync(destinationPackage, destinationDataSheetName, AnalysisSheetName, reportType, progress, selectedFinYear, sourceFilePath, reportDate, cancellationToken);
                    Logger.LogDebug("ProcessExcelReportAsync: Post-copy operations finished.");

                    progress?.Report(new ProgressReport("Saving processed file...", 85));
                    Logger.LogDebug("ProcessExcelReportAsync: Saving destination package...");
                    try
                    {
                        await destinationPackage.SaveAsync(cancellationToken);
                        Logger.LogDebug($"ProcessExcelReportAsync: Saved changes to temporary file: {tempFilePath}");
                    }
                    catch (Exception saveEx)
                    {
                        Logger.LogError($"Error saving temporary Excel package '{tempFilePath}': {saveEx}");
                        throw;
                    }
                    Logger.LogDebug("ProcessExcelReportAsync: Destination package saved.");
                }
                Logger.LogDebug("ProcessExcelReportAsync: Excel packages disposed.");
                await Task.Delay(500, cancellationToken);
                Logger.LogTrace("ProcessExcelReportAsync: Brief delay completed after disposing destination package.");

                progress?.Report(new ProgressReport("Generating final filename...", 90));
                Logger.LogTrace("ProcessExcelReportAsync: Generating final filename...");
                string generatedFileName = await Task.Run(() => GenerateFinalFileName(reportType, reportDate, DateTime.Now), cancellationToken);
                finalFilePath = Path.Combine(fullOutputFolderPath, generatedFileName);
                Logger.LogDebug($"ProcessExcelReportAsync: Generated final filename: {generatedFileName}");
                Logger.LogDebug($"ProcessExcelReportAsync: Full final file path: {finalFilePath}");

                Logger.LogInfo($"Attempting to rename file.");
                Logger.LogDebug($"Source (Temp): '{tempFilePath}'");
                Logger.LogDebug($"Destination (Final): '{finalFilePath}'");

                Logger.LogTrace($"ProcessExcelReportAsync: Attempting rename from '{tempFilePath}' to '{finalFilePath}'...");
                await RenameFileWithRetryAsync(tempFilePath, finalFilePath, progress, cancellationToken);
                Logger.LogTrace($"ProcessExcelReportAsync: Rename successful.");
                tempFilePath = null;

                progress?.Report(new ProgressReport("Excel processing complete.", 100));
                Logger.LogInfo($"Excel processing finished. Final file: {finalFilePath}");

>>>>>>> parent of 171b8e4 (v1.9.2)
                stopwatch.Stop();
                Logger.LogInfo($"ProcessExcelReportAsync completed successfully. Duration: {stopwatch.ElapsedMilliseconds}ms.");
                Logger.LogDebug($"Exiting ProcessExcelReportAsync. Result: {finalFilePath}");
                return finalFilePath;
            }
            catch (OperationCanceledException)
            {
                stopwatch.Stop();
                Logger.LogWarning($"Excel processing was cancelled. Duration: {stopwatch.ElapsedMilliseconds}ms.");
<<<<<<< HEAD
                _statusManager.Post("Operation cancelled.", MessageType.Warning, TimeSpan.FromSeconds(5));
=======
                progress?.Report(new ProgressReport("Operation cancelled."));
                Logger.LogTrace($"Exiting ProcessExcelReportAsync due to cancellation.");
>>>>>>> parent of 171b8e4 (v1.9.2)
                return null;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
<<<<<<< HEAD
                Logger.LogError($"Error during Excel processing: {ex.Message}. Duration: {stopwatch.ElapsedMilliseconds}ms.", ex);
                // Post a persistent error message to the UI.
                _statusManager.Post($"Excel Error: {ex.Message}", MessageType.Error);
=======
                Logger.LogError($"Error during Excel processing: {ex}. Duration: {stopwatch.ElapsedMilliseconds}ms.");
                progress?.Report(new ProgressReport($"Error: {ex.Message}"));
                Logger.LogTrace($"Exiting ProcessExcelReportAsync due to error.");
>>>>>>> parent of 171b8e4 (v1.9.2)
                return null;
            }
            finally
            {
<<<<<<< HEAD
                // Ensure temporary files are cleaned up in case of an error.
=======
>>>>>>> parent of 171b8e4 (v1.9.2)
                if (tempFilePath != null && File.Exists(tempFilePath))
                {
                    try
                    {
<<<<<<< HEAD
=======
                        Logger.LogDebug($"ProcessExcelReportAsync: Cleaning up temporary file '{tempFilePath}'...");
>>>>>>> parent of 171b8e4 (v1.9.2)
                        File.Delete(tempFilePath);
                        Logger.LogInfo($"Deleted temporary file due to incomplete process: {tempFilePath}");
                    }
                    catch (Exception cleanupEx)
                    {
                        Logger.LogWarning($"Failed to delete temporary file '{tempFilePath}': {cleanupEx.Message}");
                    }
                }
            }
        }

<<<<<<< HEAD
        /// <summary>
        /// Gets the current financial year string (e.g., "2023_24" or "FY 23/24").
        /// </summary>
        /// <param name="useUnderscoreFormat">If true, returns "YYYY_YY" format; otherwise, returns "FY YY/YY" format.</param>
        /// <returns>The formatted financial year string.</returns>
=======
        private async Task FilterDataSheetAsync(ExcelWorksheet worksheet, int numericColumnIndex, decimal threshold, IProgress<ProgressReport>? progress, CancellationToken cancellationToken)
        {
            Logger.LogInfo($"Starting to filter sheet '{worksheet.Name}' on column {numericColumnIndex} for values >= {threshold}.");
            if (worksheet.Dimension == null || worksheet.Dimension.Rows < 2)
            {
                Logger.LogInfo($"Sheet '{worksheet.Name}' is empty or has only headers. No filtering needed.");
                return;
            }

            await Task.Run(() =>
            {
                int initialRowCount = worksheet.Dimension.Rows;
                int rowsDeleted = 0;
                for (int r = initialRowCount; r >= 2; r--)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var cellValue = worksheet.Cells[r, numericColumnIndex].Value;
                    bool deleteRow = true;

                    if (cellValue != null)
                    {
                        string valStr = cellValue.ToString()!
                                            .Replace("£", "")
                                            .Replace("$", "")
                                            .Replace(",", "")
                                            .Trim();

                        if (decimal.TryParse(valStr, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal amount))
                        {
                            if (amount >= threshold)
                            {
                                deleteRow = false;
                            }
                        }
                        else
                        {
                            Logger.LogDebug($"FilterDataSheetAsync: Could not parse value in Column {numericColumnIndex}, Row {r}: '{cellValue}'. Row will be deleted.");
                        }
                    }
                    else
                    {
                        Logger.LogDebug($"FilterDataSheetAsync: Value in Column {numericColumnIndex}, Row {r} is null/empty. Row will be deleted.");
                    }

                    if (deleteRow)
                    {
                        worksheet.DeleteRow(r, 1);
                        rowsDeleted++;
                    }

                    if ((initialRowCount - r) % 100 == 0 && progress != null)
                    {
                        int processedRows = initialRowCount - r + 1;
                        int percentage = (initialRowCount > 1) ? (int)((double)processedRows / (initialRowCount - 1) * 100) : 100;
                        progress.Report(new ProgressReport($"Filtering 'DATA' sheet... {processedRows}/{initialRowCount - 1}", Math.Min(100, percentage)));
                    }
                }
                Logger.LogInfo($"Filtering of sheet '{worksheet.Name}' complete. {rowsDeleted} rows deleted. {worksheet.Dimension?.Rows - 1 ?? 0} data rows remaining.");
                progress?.Report(new ProgressReport($"Filtering 'DATA' sheet complete.", 100));

            }, cancellationToken);
        }

        private async Task FilterAnalysisSheetForZeroEstimatesAsync(
            ExcelPackage package,
            string analysisSheetName,
            IProgress<ProgressReport>? progress,
            CancellationToken cancellationToken)
        {
            Logger.LogInfo($"Starting to filter Analysis sheet '{analysisSheetName}' for customers with zero estimates (Col D).");
            ExcelWorksheet? worksheet = package.Workbook.Worksheets[analysisSheetName];

            if (worksheet == null || worksheet.Dimension == null)
            {
                Logger.LogWarning($"Analysis sheet '{analysisSheetName}' not found or is empty. No filtering applied.");
                return;
            }

            await Task.Run(() =>
            {
                const int customerDataStartRow = 6;
                if (worksheet.Dimension.Rows < customerDataStartRow)
                {
                    Logger.LogInfo($"Analysis sheet '{analysisSheetName}' has no data rows starting from row {customerDataStartRow}. No filtering needed.");
                    return;
                }

                int initialRowCount = worksheet.Dimension.Rows;
                int rowsDeleted = 0;
                int totalRowsToProcess = initialRowCount - customerDataStartRow + 1;
                if (totalRowsToProcess <= 0) totalRowsToProcess = 1;

                Logger.LogDebug($"FilterAnalysisSheetForZeroEstimatesAsync: Initial rows: {initialRowCount}, Data starts at: {customerDataStartRow}. Processing {totalRowsToProcess} potential data rows.");

                for (int r = initialRowCount; r >= customerDataStartRow; r--)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var customerCell = worksheet.Cells[r, CustomerColumnIndex].Value;
                    if (customerCell == null || string.IsNullOrWhiteSpace(customerCell.ToString()))
                    {
                        bool isRowEffectivelyBlank = true;
                        for (int col = CustomerColumnIndex; col <= LastClearableColumnAnalysis; col++)
                        {
                            if (worksheet.Cells[r, col].Value != null || !string.IsNullOrEmpty(worksheet.Cells[r, col].Formula))
                            {
                                isRowEffectivelyBlank = false;
                                break;
                            }
                        }
                        if (isRowEffectivelyBlank)
                        {
                            continue;
                        }
                    }

                    var noOfEstimatesCell = worksheet.Cells[r, AnalysisSheetNoOfEstimatesColumnIndex].Value;

                    decimal numberOfEstimates = 0;
                    if (noOfEstimatesCell != null)
                    {
                        object cellVal = noOfEstimatesCell;
                        if (cellVal is double dVal) numberOfEstimates = (decimal)dVal;
                        else if (cellVal is int iVal) numberOfEstimates = iVal;
                        else if (cellVal is decimal decVal) numberOfEstimates = decVal;
                        else if (cellVal != null) decimal.TryParse(cellVal.ToString()?.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out numberOfEstimates);
                    }

                    if (numberOfEstimates <= 0)
                    {
                        if (customerCell != null && !string.IsNullOrWhiteSpace(customerCell.ToString()))
                        {
                            Logger.LogDebug($"FilterAnalysisSheet: Deleting row {r} for customer '{customerCell}' due to zero estimates (Col D: {numberOfEstimates}).");
                            worksheet.DeleteRow(r, 1);
                            rowsDeleted++;
                        }
                    }

                    if ((initialRowCount - r) % 20 == 0 && progress != null)
                    {
                        int processedIteration = initialRowCount - r + 1;
                        int percentage = (int)((double)processedIteration / totalRowsToProcess * 100);
                        progress.Report(new ProgressReport($"Filtering Analysis sheet... {processedIteration}/{totalRowsToProcess}", Math.Min(100, percentage)));
                    }
                }
                Logger.LogInfo($"Filtering of Analysis sheet '{analysisSheetName}' complete. {rowsDeleted} customer rows deleted. Current rows: {worksheet.Dimension?.Rows ?? 0}");
                if (progress != null)
                {
                    progress.Report(new ProgressReport($"Filtering Analysis sheet complete.", 100));
                }
            }, cancellationToken);
        }

>>>>>>> parent of 171b8e4 (v1.9.2)
        public string GetCurrentFinancialYear(bool useUnderscoreFormat = false)
        {
            DateTime today = DateTime.Today;
<<<<<<< HEAD
            int startYear = ReportHelper.GetFinancialYearStartCalendarYear(today, _configuration);
            int endYear = startYear + 1;

            return useUnderscoreFormat
                ? $"{startYear}_{endYear.ToString().Substring(2, 2)}"
                : $"FY {startYear.ToString().Substring(2, 2)}/{endYear.ToString().Substring(2, 2)}";
        }

        /// <summary>
        /// Gets the previous financial year string from a given current financial year string.
        /// </summary>
        /// <param name="currentFinancialYearUnderscore">The current financial year in "YYYY_YY" format.</param>
        /// <returns>The previous financial year in the same format, or null if the input is invalid.</returns>
        public string? GetPreviousFinancialYear(string currentFinancialYearUnderscore)
        {
            if (string.IsNullOrEmpty(currentFinancialYearUnderscore)) return null;
=======
            int year = today.Year;
            int startYear = today.Month >= 5 ? year : year - 1;
            int endYear = startYear + 1;
            string result = useUnderscoreFormat ? $"{startYear}_{endYear.ToString()[2..]}" : $"FY {startYear.ToString()[2..]}/{endYear.ToString()[2..]}";
            Logger.LogTrace($"Exiting GetCurrentFinancialYear. Result: {result}");
            return result;
        }

        public string? GetPreviousFinancialYear(string currentFinancialYearUnderscore)
        {
            Logger.LogTrace($"Entering GetPreviousFinancialYear(currentFinancialYearUnderscore: {currentFinancialYearUnderscore})");
            if (string.IsNullOrEmpty(currentFinancialYearUnderscore))
            {
                Logger.LogTrace("Exiting GetPreviousFinancialYear. Input was null/empty.");
                return null;
            }
>>>>>>> parent of 171b8e4 (v1.9.2)
            string[] parts = currentFinancialYearUnderscore.Split('_');
            if (parts.Length == 2 && int.TryParse(parts[0], out int startYear))
            {
                int prevStartYear = startYear - 1;
<<<<<<< HEAD
                return $"{prevStartYear}_{startYear.ToString().Substring(2, 2)}";
            }
            return null;
        }

        /// <summary>
        /// Validates if a given date range falls entirely within a specified financial year.
        /// </summary>
        /// <param name="selectedFinYearUnderscore">The financial year to validate against, in "YYYY_YY" format.</param>
        /// <param name="fromDate">The start date of the range.</param>
        /// <param name="toDate">The end date of the range.</param>
        /// <returns>True if the date range is valid for the specified financial year.</returns>
        public bool IsFinancialYearValid(string selectedFinYearUnderscore, DateTime fromDate, DateTime toDate)
        {
            if (string.IsNullOrEmpty(selectedFinYearUnderscore)) return false;
=======
                result = $"{prevStartYear}_{startYear.ToString()[2..]}";
            }
            else
            {
                Logger.LogWarning($"Invalid financial year format for calculating previous: {currentFinancialYearUnderscore}");
            }
            Logger.LogTrace($"Exiting GetPreviousFinancialYear. Result: {result ?? "null"}");
            return result;
        }

        public bool IsFinancialYearValid(string selectedFinYearUnderscore, DateTime fromDate, DateTime toDate)
        {
            Logger.LogTrace($"Entering IsFinancialYearValid(selectedFinYearUnderscore: {selectedFinYearUnderscore}, fromDate: {fromDate:d}, toDate: {toDate:d})");
            if (string.IsNullOrEmpty(selectedFinYearUnderscore))
            {
                Logger.LogTrace("Exiting IsFinancialYearValid. Selected FY was null/empty. Result: false");
                return false;
            }
>>>>>>> parent of 171b8e4 (v1.9.2)
            string[] parts = selectedFinYearUnderscore.Split('_');
            if (parts.Length == 2 && int.TryParse(parts[0], out int startYear))
            {
<<<<<<< HEAD
                (DateTime fyStartDate, DateTime fyEndDate) = ReportHelper.GetFinancialYearDates(startYear, _configuration);
                return fromDate >= fyStartDate && toDate <= fyEndDate;
            }
            return false;
        }

        /// <summary>
        /// Gets the expected full file path for a final processed report.
        /// </summary>
        /// <param name="reportType">The type of the report.</param>
        /// <param name="baseFileSaveLocation">The base directory for saving the report.</param>
        /// <param name="reportDate">The primary date of the report.</param>
        /// <returns>The full, expected path of the final report file, or null on error.</returns>
        public string? GetExpectedFinalFilePath(ReportType reportType, string baseFileSaveLocation, DateTime reportDate)
        {
            try
            {
                DateTime folderTimestampDate = reportType == ReportType.Custom ? DateTime.Now : reportDate;
                string? folderPath = FolderCreation.GetReportSpecificFolderPath(reportType, baseFileSaveLocation, folderTimestampDate, _configuration);
                if (!string.IsNullOrEmpty(folderPath))
                {
                    string fileName = GenerateFinalFileName(reportType, reportDate, DateTime.Now);
                    return Path.Combine(folderPath, fileName);
=======
                int endYear = startYear + 1;
                DateTime fyStartDate = new DateTime(startYear, 5, 1);
                DateTime fyEndDate = new DateTime(endYear, 4, 30);
                isValid = fromDate >= fyStartDate && toDate <= fyEndDate;
                if (!isValid)
                {
                    Logger.LogWarning($"Date range {fromDate:yyyy-MM-dd} to {toDate:yyyy-MM-dd} is outside selected FY {selectedFinYearUnderscore} ({fyStartDate:yyyy-MM-dd} to {fyEndDate:yyyy-MM-dd}).");
                }
            }
            else
            {
                Logger.LogWarning($"Invalid financial year format for validation: {selectedFinYearUnderscore}");
            }
            Logger.LogTrace($"Exiting IsFinancialYearValid. Result: {isValid}");
            return isValid;
        }

        public string? GetExpectedFinalFilePath(int reportType, string baseFileSaveLocation, DateTime reportDate)
        {
            Logger.LogTrace($"Entering GetExpectedFinalFilePath(reportType: {reportType}, baseFileSaveLocation: {baseFileSaveLocation}, reportDate: {reportDate:d})");
            string? result = null;
            try
            {
                if (reportDate == default && reportType != CustomReportIndex)
                {
                    reportDate = DateTime.Today;
                    Logger.LogWarning($"GetExpectedFinalFilePath called without a specific reportDate for non-custom report. Defaulting to Today for filename generation: {reportDate:yyyy-MM-dd}");
                }

                DateTime folderTimestampDate = reportType == CustomReportIndex ? DateTime.Now : reportDate;
                string? folderPath = FolderCreation.GetReportSpecificFolderPath(reportType, baseFileSaveLocation, folderTimestampDate);
                if (folderPath != null)
                {
                    string fileName = GenerateFinalFileName(reportType, reportDate, DateTime.Now);
                    result = Path.Combine(folderPath, fileName);
                }
                else
                {
                    Logger.LogError("GetExpectedFinalFilePath: Failed to determine folder path using FolderCreation utility.");
>>>>>>> parent of 171b8e4 (v1.9.2)
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error getting expected final file path: {ex.Message}");
            }
            return null;
        }
<<<<<<< HEAD
        #endregion

        #region Internal Processing Steps (Private Methods)
        /// <summary>
        /// Orchestrates various operations after the initial data copy to the template's "DATA" sheet.
        /// </summary>
        private async Task ProcessPostCopyOperationsAsync(
            ExcelPackage package, string sourceDataSheetName, string targetAnalysisSheetName,
            ReportType reportType, string selectedFinYear, string originalSourceFilePath,
            DateTime reportDate, CancellationToken cancellationToken)
        {
            _statusManager.Post("Extracting unique customers...", MessageType.InProgress);
            await ExtractUniqueCustomersAsync(package, sourceDataSheetName, targetAnalysisSheetName, reportType, originalSourceFilePath, reportDate, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            _statusManager.Post("Calculating formulas...", MessageType.InProgress);
            await Task.Run(() => CalculateSheet(package, targetAnalysisSheetName), cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            if (reportType == ReportType.Daily5Day1k)
            {
                _statusManager.Post("Filtering zero-estimate customers...", MessageType.InProgress);
                await FilterAnalysisSheetForZeroEstimatesAsync(package, targetAnalysisSheetName, cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
            }

            _statusManager.Post("Cleaning unused rows...", MessageType.InProgress);
            await Task.Run(() => ClearContentBelowLastCustomer(package, targetAnalysisSheetName, CustomerColumnIndex, AnalysisSheetFirstClearableColumn, AnalysisSheetLastClearableColumn), cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            if (reportType is ReportType.Monthly or ReportType.Quarterly or ReportType.Annual or ReportType.Custom)
            {
                _statusManager.Post("Setting pivot tables to refresh...", MessageType.InProgress);
                string monthlyOrderPivotSheetName = _configuration.GetValue<string>(AppConfigKeys.OperationalParameters.ExcelSheetNames.MonthlyOrderPivotSheet, "OrderPivot")!;
                string monthlyEstimatePivotSheetName = _configuration.GetValue<string>(AppConfigKeys.OperationalParameters.ExcelSheetNames.MonthlyEstimatePivotSheet, "Estimate Success PivotTable")!;
                string monthlyOrderPivotName = _configuration.GetValue<string>(AppConfigKeys.OperationalParameters.PivotTableNames.MonthlyOrderPivot, "PivotTable1")!;
                string monthlyEstimatePivotName = _configuration.GetValue<string>(AppConfigKeys.OperationalParameters.PivotTableNames.MonthlyEstimatePivot, "PivotTable3")!;
                await Task.Run(() => RefreshPivotTable(package, monthlyOrderPivotSheetName, monthlyOrderPivotName), cancellationToken);
                await Task.Run(() => RefreshPivotTable(package, monthlyEstimatePivotSheetName, monthlyEstimatePivotName), cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
            }

            if (reportType == ReportType.Weekly)
            {
                _statusManager.Post("Appending data to Power BI source...", MessageType.InProgress);
                string powerBiSheetName = _configuration.GetValue<string>(AppConfigKeys.OperationalParameters.ExcelSheetNames.PowerBiDataSheet, "powerBI")!;
                string powerBiDataFilePathConfigKey = AppConfigKeys.Paths.Base + ":PowerBiDataFile";
                await CopyAnalysisDataToPowerBIReportAsync(package, targetAnalysisSheetName, powerBiSheetName, reportType, originalSourceFilePath, reportDate, powerBiDataFilePathConfigKey, cancellationToken);
=======

        public int GetWeekOfMonth(DateTime date)
        {
            Logger.LogTrace($"Entering GetWeekOfMonth(date: {date:d})");
            DateTime firstOfMonth = new DateTime(date.Year, date.Month, 1);
            int firstDayOfWeekIso = firstOfMonth.DayOfWeek == 0 ? 7 : (int)firstOfMonth.DayOfWeek;
            int weekOfMonth = (date.Day + firstDayOfWeekIso - 1 - 1) / 7 + 1;
            Logger.LogTrace($"Exiting GetWeekOfMonth. Result: {weekOfMonth}");
            return weekOfMonth;
        }
        #endregion

        #region Internal Processing Steps

        private async Task ProcessPostCopyOperationsAsync(
            ExcelPackage package,
            string sourceDataSheetName,
            string targetAnalysisSheetName,
            int reportType,
            IProgress<ProgressReport>? progress,
            string selectedFinYear,
            string originalSourceFilePath,
            DateTime reportDate,
            CancellationToken cancellationToken)
        {
            Logger.LogTrace($"Entering ProcessPostCopyOperationsAsync(sourceSheet: {sourceDataSheetName}, targetSheet: {targetAnalysisSheetName}, reportType: {reportType})");
            var stopwatch = Stopwatch.StartNew();

            progress?.Report(new ProgressReport("Extracting unique customers...", 40));
            Logger.LogTrace("ProcessPostCopyOperationsAsync: Calling ExtractUniqueCustomersAsync...");
            await ExtractUniqueCustomersAsync(package, sourceDataSheetName, targetAnalysisSheetName, reportType, progress, originalSourceFilePath, reportDate, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            progress?.Report(new ProgressReport("Calculating analysis sheet...", 50));
            Logger.LogTrace("ProcessPostCopyOperationsAsync: Calling CalculateSheet...");
            await Task.Run(() => CalculateSheet(package, targetAnalysisSheetName), cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            if (reportType == NewDailyReportOver1kIndex)
            {
                progress?.Report(new ProgressReport("Filtering Analysis sheet for zero estimates/values...", 55));
                await FilterAnalysisSheetForZeroEstimatesAsync(package, targetAnalysisSheetName, progress, cancellationToken);
                Logger.LogInfo($"Analysis sheet filtered for zero estimates/values for report type {NewDailyReportOver1kIndex}.");
                cancellationToken.ThrowIfCancellationRequested();
            }

            progress?.Report(new ProgressReport("Cleaning analysis sheet...", 60));
            Logger.LogTrace("ProcessPostCopyOperationsAsync: Calling ClearContentBelowLastCustomer...");
            await Task.Run(() => ClearContentBelowLastCustomer(package, targetAnalysisSheetName, CustomerColumnIndex, FirstClearableColumnAnalysis, LastClearableColumnAnalysis), cancellationToken);
            Logger.LogTrace($"Cleaned content below last customer in sheet '{targetAnalysisSheetName}'.");
            cancellationToken.ThrowIfCancellationRequested();

            if (reportType is MonthlyReportIndex or QuarterlyReportIndex or AnnualReportIndex)
            {
                progress?.Report(new ProgressReport("Setting pivot tables to refresh on load...", 70));
                Logger.LogTrace("ProcessPostCopyOperationsAsync: Calling RefreshPivotTable (Order)...");
                await Task.Run(() => RefreshPivotTable(package, MonthlyOrderPivotSheetName, MonthlyOrderPivotName), cancellationToken);
                Logger.LogTrace("ProcessPostCopyOperationsAsync: Calling RefreshPivotTable (Estimate)...");
                await Task.Run(() => RefreshPivotTable(package, MonthlyEstimatePivotSheetName, MonthlyEstimatePivotName), cancellationToken);
                Logger.LogInfo("Pivot tables set to refresh on load.");
                cancellationToken.ThrowIfCancellationRequested();
            }
            else if (reportType == CustomReportIndex)
            {
                Logger.LogInfo("Checking if Custom report uses Monthly template for Pivot Table refresh.");
                string templateNameInUse = package.File.Name;
                bool usesMonthlyTemplate = templateNameInUse.Contains("Monthly", StringComparison.OrdinalIgnoreCase);
                if (templateNameInUse.Contains("Monthly", StringComparison.OrdinalIgnoreCase) || usesMonthlyTemplate)
                {
                    progress?.Report(new ProgressReport("Setting pivot tables to refresh on load (Custom - Monthly Template)...", 70));
                    await Task.Run(() => RefreshPivotTable(package, MonthlyOrderPivotSheetName, MonthlyOrderPivotName), cancellationToken);
                    await Task.Run(() => RefreshPivotTable(package, MonthlyEstimatePivotSheetName, MonthlyEstimatePivotName), cancellationToken);
                    Logger.LogInfo("Pivot tables set to refresh on load for Custom report (assumed Monthly Template).");
                }
                else
                {
                    Logger.LogInfo("Custom report does not appear to use Monthly template. Skipping Pivot Table refresh for it.");
                }
            }
            else
            {
                Logger.LogInfo($"Skipping Pivot Table refresh for report type {reportType} as it uses standard template without these pivots.");
            }

            if (reportType == WeeklyReportIndex)
            {
                progress?.Report(new ProgressReport("Appending data to Power BI report...", 75));
                Logger.LogTrace("ProcessPostCopyOperationsAsync: Calling CopyAnalysisDataToPowerBIReportAsync...");
                await CopyAnalysisDataToPowerBIReportAsync(package, targetAnalysisSheetName, progress, reportType, originalSourceFilePath, reportDate, cancellationToken);
                Logger.LogInfo("Data appended to Power BI report.");
>>>>>>> parent of 171b8e4 (v1.9.2)
                cancellationToken.ThrowIfCancellationRequested();
            }
        }

<<<<<<< HEAD
        /// <summary>
        /// Ensures the destination "DATA" sheet exists in the package, creating and formatting it if necessary.
        /// </summary>
        private ExcelWorksheet GetOrCreateDestinationWorksheet(ExcelPackage package, string sheetName, ExcelWorksheet sourceWorksheet)
        {
=======
        private ExcelWorksheet GetOrCreateDestinationWorksheet(ExcelPackage package, string sheetName, ExcelWorksheet sourceWorksheet)
        {
            Logger.LogTrace($"Entering GetOrCreateDestinationWorksheet(sheetName: {sheetName}, sourceSheet: {sourceWorksheet.Name})");
>>>>>>> parent of 171b8e4 (v1.9.2)
            ExcelWorksheet? worksheet = package.Workbook.Worksheets[sheetName];
            if (worksheet == null)
            {
                worksheet = package.Workbook.Worksheets.Add(sheetName);
                if (sourceWorksheet.Dimension != null && sourceWorksheet.Dimension.Rows >= 1)
                {
<<<<<<< HEAD
                    sourceWorksheet.Cells[1, 1, 1, sourceWorksheet.Dimension.Columns].Copy(worksheet.Cells[1, 1]);
=======
                    int headerColCount = sourceWorksheet.Dimension.Columns;
                    ExcelRange sourceHeaderRow = sourceWorksheet.Cells[1, 1, 1, headerColCount];
                    ExcelRange destHeader = worksheet.Cells[1, 1, 1, headerColCount];
                    sourceHeaderRow.Copy(destHeader);
                    Logger.LogInfo($"Created sheet '{sheetName}' and copied headers from '{sourceWorksheet.Name}' row 1.");
                }
                else
                {
                    worksheet.Cells[1, 1].Value = "DefaultHeader";
                    Logger.LogWarning($"Created sheet '{sheetName}', source sheet '{sourceWorksheet.Name}' was empty or had no header row, added default header.");
>>>>>>> parent of 171b8e4 (v1.9.2)
                }
            }
            else
            {
                if (worksheet.Dimension != null && worksheet.Dimension.Rows > 1)
                {
                    worksheet.DeleteRow(2, worksheet.Dimension.Rows - 1);
<<<<<<< HEAD
=======
                    Logger.LogInfo($"Cleared existing data (rows 2 onwards) from sheet '{sheetName}'. Headers in row 1 preserved.");
                }
                else
                {
                    Logger.LogDebug($"Sheet '{sheetName}' already existed but had no data below header row (or was empty).");
>>>>>>> parent of 171b8e4 (v1.9.2)
                }
            }
            Logger.LogTrace($"Exiting GetOrCreateDestinationWorksheet. Returning sheet: {worksheet.Name}");
            return worksheet;
        }

<<<<<<< HEAD
        /// <summary>
        /// Filters rows in the data worksheet based on a numeric threshold in a given column.
        /// </summary>
        private async Task FilterDataSheetAsync(ExcelWorksheet worksheet, int numericColumnIndex, decimal threshold, CancellationToken cancellationToken)
        {
            if (worksheet.Dimension == null || worksheet.Dimension.Rows < 2) return;
            await Task.Run(() =>
            {
                // Iterate backwards when deleting rows to avoid index shifting issues.
                for (int r = worksheet.Dimension.Rows; r >= 2; r--)
=======
        private async Task ExtractUniqueCustomersAsync(
             ExcelPackage package,
             string sourceDataSheetName,
             string targetAnalysisSheetName,
             int reportType,
             IProgress<ProgressReport>? progress,
             string originalSourceFilePath,
             DateTime reportDate,
             CancellationToken cancellationToken)
        {
            Logger.LogTrace($"Entering ExtractUniqueCustomersAsync for sheet '{targetAnalysisSheetName}'");
            ExcelWorksheet? dataSheet = package.Workbook.Worksheets[sourceDataSheetName];
            ExcelWorksheet analysisSheet = package.Workbook.Worksheets[targetAnalysisSheetName]
                                           ?? package.Workbook.Worksheets.Add(targetAnalysisSheetName);

            if (dataSheet == null)
            {
                Logger.LogError($"Source data sheet ('{sourceDataSheetName}') not found for customer extraction.");
                return;
            }

            const int analysisPopulateStartRow = 6;
            const int templateFormulaLimitRow = 2000;

            bool templateRow6Exists = analysisSheet.Dimension != null && analysisSheet.Dimension.Rows >= analysisPopulateStartRow;
            if (!templateRow6Exists)
            {
                Logger.LogWarning($"Analysis sheet '{targetAnalysisSheetName}' has fewer than {analysisPopulateStartRow} rows. Template formulas/values in row {analysisPopulateStartRow} will not be available for propagation. Ensure template is correctly structured.");
            }

            int dataSheetStartRow = 2;
            int dataSheetRowCount = dataSheet.Dimension?.Rows ?? 0;

            string sourceFileNameForAnalysisColumn = Path.GetFileName(originalSourceFilePath);
            string currentFY = GetCurrentFinancialYear(false);

            Logger.LogTrace("ExtractUniqueCustomersAsync: Extracting unique customer names from DATA sheet...");
            List<string> uniqueCustomers;
            if (dataSheetRowCount < dataSheetStartRow)
            {
                Logger.LogWarning($"Source data sheet '{sourceDataSheetName}' has no data rows. No customers to extract.");
                uniqueCustomers = new List<string>();
            }
            else
            {
                uniqueCustomers = await Task.Run(() =>
>>>>>>> parent of 171b8e4 (v1.9.2)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var cellValue = worksheet.Cells[r, numericColumnIndex].Value;
                    bool deleteRow = true;
                    if (cellValue != null)
                    {
                        // Try to parse the cell value as a decimal, removing currency symbols.
                        if (decimal.TryParse(cellValue.ToString()!.Replace("£", "").Replace(",", "").Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal amount))
                        {
                            if (amount >= threshold) deleteRow = false;
                        }
                    }
<<<<<<< HEAD
                    if (deleteRow) worksheet.DeleteRow(r, 1);
=======
                    return customers.OrderBy(c => c).ToList();
                }, cancellationToken);
            }

            Logger.LogInfo($"Found {uniqueCustomers.Count} unique customers from '{sourceDataSheetName}'.");
            progress?.Report(new ProgressReport($"Extracted {uniqueCustomers.Count} unique customers.", 45));

            // Pre-clear only essential input columns (A, L, M, N) in the template's existing data area.
            // Formulas in B, C, D-K etc. in the template rows (6 to templateFormulaLimitRow) will be preserved.
            if (analysisSheet.Dimension != null)
            {
                int endClearRange = Math.Min(templateFormulaLimitRow, analysisSheet.Dimension.End.Row);
                if (endClearRange >= analysisPopulateStartRow)
                {
                    Logger.LogDebug($"Pre-clearing direct input columns (A,L,M,N) in Analysis sheet from row {analysisPopulateStartRow} to {endClearRange}. Template formulas in other columns will be preserved.");
                    for (int r = analysisPopulateStartRow; r <= endClearRange; r++)
                    {
                        analysisSheet.Cells[r, CustomerColumnIndex].Value = null;       // Customer Name
                        analysisSheet.Cells[r, SourceFileNameColumnIndex].Value = null; // Source File Name
                        analysisSheet.Cells[r, DateColumnIndex].Value = null;           // Date
                        analysisSheet.Cells[r, FinancialYearColumnIndex].Value = null;  // Financial Year
                    }
>>>>>>> parent of 171b8e4 (v1.9.2)
                }
            }, cancellationToken);
        }

<<<<<<< HEAD
        /// <summary>
        /// Filters the "Analysis" sheet to remove rows where the "No of Estimates" column is zero or empty.
        /// </summary>
        private async Task FilterAnalysisSheetForZeroEstimatesAsync(ExcelPackage package, string analysisSheetName, CancellationToken cancellationToken)
        {
            ExcelWorksheet? worksheet = package.Workbook.Worksheets[analysisSheetName];
            if (worksheet == null || worksheet.Dimension == null) return;
            await Task.Run(() =>
            {
                const int customerDataStartRow = 6;
                if (worksheet.Dimension.Rows < customerDataStartRow) return;
                for (int r = worksheet.Dimension.Rows; r >= customerDataStartRow; r--)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    // Skip blank rows.
                    if (string.IsNullOrWhiteSpace(worksheet.Cells[r, CustomerColumnIndex].Value?.ToString())) continue;
                    var noOfEstimatesCell = worksheet.Cells[r, AnalysisSheetNoOfEstimatesColumnIndex].Value;
                    decimal.TryParse(noOfEstimatesCell?.ToString(), out decimal numberOfEstimates);
                    if (numberOfEstimates <= 0) worksheet.DeleteRow(r, 1);
                }
            }, cancellationToken);
        }

        /// <summary>
        /// Extracts unique customer names from the raw data and populates them into the "Analysis" sheet.
        /// </summary>
        private async Task ExtractUniqueCustomersAsync(ExcelPackage package, string sourceDataSheetName, string targetAnalysisSheetName, ReportType reportType, string originalSourceFilePath, DateTime reportDate, CancellationToken cancellationToken)
        {
            ExcelWorksheet? dataSheet = package.Workbook.Worksheets[sourceDataSheetName];
            ExcelWorksheet analysisSheet = package.Workbook.Worksheets[targetAnalysisSheetName] ?? throw new InvalidOperationException($"Target Analysis sheet '{targetAnalysisSheetName}' not found.");
            if (dataSheet == null) return;

            int dataSheetStartRow = 2;
            int dataSheetRowCount = dataSheet.Dimension?.Rows ?? 0;
            if (dataSheetRowCount < dataSheetStartRow) return;

            var uniqueCustomers = await Task.Run(() =>
            {
                var customers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                for (int row = dataSheetStartRow; row <= dataSheetRowCount; row++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    string? customerName = dataSheet.Cells[row, CustomerColumnIndex].Value?.ToString()?.Trim();
                    if (!string.IsNullOrWhiteSpace(customerName)) customers.Add(customerName);
                }
                return customers.OrderBy(c => c, StringComparer.OrdinalIgnoreCase).ToList();
            }, cancellationToken);

            string sourceFileNameForAnalysis = Path.GetFileName(originalSourceFilePath);
            string currentFY = GetCurrentFinancialYear(false);
            const int analysisPopulateStartRow = 6;
            for (int i = 0; i < uniqueCustomers.Count; i++)
            {
                int targetRow = analysisPopulateStartRow + i;
                analysisSheet.Cells[targetRow, CustomerColumnIndex].Value = uniqueCustomers[i];
                analysisSheet.Cells[targetRow, AnalysisSheetDateColumnIndex].Value = reportDate.Date;
                analysisSheet.Cells[targetRow, AnalysisSheetDateColumnIndex].Style.Numberformat.Format = "dd/mm/yyyy";
                analysisSheet.Cells[targetRow, AnalysisSheetFinancialYearColumnIndex].Value = currentFY;
                analysisSheet.Cells[targetRow, AnalysisSheetSourceFileNameColumnIndex].Value = sourceFileNameForAnalysis;
=======
            Logger.LogTrace("ExtractUniqueCustomersAsync: Populating Analysis sheet with unique customers and data columns...");
            for (int i = 0; i < uniqueCustomers.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                string customer = uniqueCustomers[i];
                int targetRow = analysisPopulateStartRow + i;

                // If targetRow is beyond the pre-existing template rows that might have formulas,
                // and also beyond the current actual end of the sheet, copy the entire template row (row 6)
                // to the new targetRow. EPPlus's Copy() method should handle adjusting relative formulas.
                if (targetRow > templateFormulaLimitRow && templateRow6Exists && targetRow > (analysisSheet.Dimension?.Rows ?? 0))
                {
                    // Ensure we copy all columns that might contain formulas or required formatting from the template row.
                    ExcelRange templateRowRange = analysisSheet.Cells[analysisPopulateStartRow, FirstClearableColumnAnalysis, analysisPopulateStartRow, LastClearableColumnAnalysis];
                    ExcelRange targetRowCells = analysisSheet.Cells[targetRow, FirstClearableColumnAnalysis, targetRow, LastClearableColumnAnalysis];
                    templateRowRange.Copy(targetRowCells); // This is where relative formulas should be adjusted by EPPlus.
                    Logger.LogTrace($"Copied template row {analysisPopulateStartRow} to new row {targetRow}. Formulas should be adjusted by EPPlus copy.");
                }
                // If the targetRow is one of the existing template rows (up to templateFormulaLimitRow),
                // its formulas in B, C, D-K etc. are assumed to be correct from the template.
                // We only need to fill in the customer-specific data in columns A, L, M, N.

                // Populate customer name and other direct input data for the current targetRow.
                analysisSheet.Cells[targetRow, CustomerColumnIndex].Value = customer;
                analysisSheet.Cells[targetRow, DateColumnIndex].Value = reportDate.Date;
                analysisSheet.Cells[targetRow, DateColumnIndex].Style.Numberformat.Format = "dd/mm/yyyy";
                analysisSheet.Cells[targetRow, FinancialYearColumnIndex].Value = currentFY;
                analysisSheet.Cells[targetRow, SourceFileNameColumnIndex].Value = sourceFileNameForAnalysisColumn;

                // Columns B and C (and D-K) are now expected to be correctly populated either by:
                // 1. The pre-existing formulas in the template rows (if targetRow <= templateFormulaLimitRow).
                // 2. The .Copy() operation for new rows (if targetRow > templateFormulaLimitRow), which adjusts relative formulas.
                // No explicit .Formula or .Value setting for B and C is done in this loop anymore.
>>>>>>> parent of 171b8e4 (v1.9.2)
            }
        }

<<<<<<< HEAD
        /// <summary>
        /// Triggers calculation of all formulas in the specified worksheet.
        /// </summary>
        private void CalculateSheet(ExcelPackage package, string sheetName)
        {
            ExcelWorksheet? worksheet = package.Workbook.Worksheets[sheetName];
            if (worksheet != null)
            {
                try
                {
                    package.Workbook.Calculate();
                }
                catch (Exception ex)
                {
                    Logger.LogWarning($"Error during Excel workbook calculation (for sheet '{sheetName}'): {ex.Message}. Manual refresh might be needed.", ex);
                }
            }
        }

        /// <summary>
        /// Clears content from rows in the specified sheet that are below the last row containing a customer name.
        /// </summary>
        private void ClearContentBelowLastCustomer(ExcelPackage package, string sheetName, int customerNameColIdx, int firstColToClear, int lastColToClear)
        {
=======
        private void CalculateSheet(ExcelPackage package, string sheetName)
        {
            Logger.LogTrace($"Entering CalculateSheet(sheetName: {sheetName})");
            if (package != null && package.Workbook.Worksheets[sheetName] != null)
            {
                try
                {
                    Logger.LogInfo($"Attempting to calculate entire workbook to ensure '{sheetName}' formulas are updated.");
                    package.Workbook.Calculate();
                    Logger.LogInfo($"Workbook calculation triggered. Formulas in '{sheetName}' should now be updated.");
                }
                catch (Exception ex)
                {
                    Logger.LogError($"Error during workbook calculation (intended for sheet '{sheetName}'): {ex.Message}", ex);
                }
            }
            else
            {
                if (package == null) Logger.LogWarning($"Excel package is null, cannot calculate.");
                else Logger.LogWarning($"Sheet '{sheetName}' not found for calculation.");
            }
            Logger.LogTrace($"Exiting CalculateSheet.");
        }

        private void ClearContentBelowLastCustomer(ExcelPackage package, string sheetName, int customerNameColIdx, int firstColToClear, int lastColToClear)
        {
            Logger.LogTrace($"Entering ClearContentBelowLastCustomer for sheet '{sheetName}'. Will clear from Col {firstColToClear} to {lastColToClear}.");
>>>>>>> parent of 171b8e4 (v1.9.2)
            ExcelWorksheet? worksheet = package.Workbook.Worksheets[sheetName];
            if (worksheet == null || worksheet.Dimension == null) return;
            const int customerDataStartRow = 6;
            int lastActualDataRow = customerDataStartRow - 1;

<<<<<<< HEAD
=======
            if (worksheet == null || worksheet.Dimension == null)
            {
                Logger.LogWarning($"Sheet '{sheetName}' not found or is empty. Nothing to clear by ClearContentBelowLastCustomer.");
                return;
            }

            const int customerDataStartRow = 6;
            int lastActualDataRow = customerDataStartRow - 1;

>>>>>>> parent of 171b8e4 (v1.9.2)
            for (int r = worksheet.Dimension.End.Row; r >= customerDataStartRow; r--)
            {
                if (worksheet.Cells[r, customerNameColIdx].Value != null && !string.IsNullOrWhiteSpace(worksheet.Cells[r, customerNameColIdx].Value.ToString()))
                {
                    lastActualDataRow = r;
                    break;
                }
            }
<<<<<<< HEAD

            int startClearTargetRow = lastActualDataRow + 1;
            if (startClearTargetRow <= worksheet.Dimension.End.Row)
            {
                worksheet.Cells[startClearTargetRow, firstColToClear, worksheet.Dimension.End.Row, lastColToClear].Clear();
            }
=======
            Logger.LogDebug($"ClearContentBelowLastCustomer: Last row with customer name in '{sheetName}' is {lastActualDataRow}.");

            int startClearTargetRow = lastActualDataRow + 1;
            startClearTargetRow = Math.Max(startClearTargetRow, customerDataStartRow);

            if (startClearTargetRow <= worksheet.Dimension.End.Row)
            {
                Logger.LogInfo($"ClearContentBelowLastCustomer: Fully clearing rows from {startClearTargetRow} to {worksheet.Dimension.End.Row} (cols {firstColToClear}-{lastColToClear}).");
                worksheet.Cells[startClearTargetRow, firstColToClear, worksheet.Dimension.End.Row, lastColToClear].Clear();
            }
            else
            {
                Logger.LogInfo($"No rows to clear below last customer data by ClearContentBelowLastCustomer (last data at {lastActualDataRow}, sheet ends at {worksheet.Dimension.End.Row}).");
            }

            Logger.LogTrace($"Exiting ClearContentBelowLastCustomer for sheet '{sheetName}'.");
>>>>>>> parent of 171b8e4 (v1.9.2)
        }

        private void RefreshPivotTable(ExcelPackage package, string sheetName, string pivotTableName)
        {
<<<<<<< HEAD
            ExcelWorksheet? worksheet = package.Workbook.Worksheets[sheetName];
            if (worksheet == null) return;
            ExcelPivotTable? pivotTable = worksheet.PivotTables.FirstOrDefault(pt => pt.Name.Equals(pivotTableName, StringComparison.OrdinalIgnoreCase));
=======
            Logger.LogTrace($"Entering RefreshPivotTable(sheetName: {sheetName}, pivotTable: {pivotTableName})");
            ExcelWorksheet? worksheet = package.Workbook.Worksheets[sheetName];
            if (worksheet == null)
            {
                Logger.LogWarning($"Sheet '{sheetName}' not found for pivot table refresh setting.");
                Logger.LogTrace($"Exiting RefreshPivotTable early - sheet not found.");
                return;
            }

            ExcelPivotTable? pivotTable = worksheet.PivotTables.FirstOrDefault(pt => pt.Name == pivotTableName);

>>>>>>> parent of 171b8e4 (v1.9.2)
            if (pivotTable != null)
            {
                try
                {
<<<<<<< HEAD
                    pivotTable.CacheDefinition.Refresh();
=======
                    Logger.LogTrace($"Attempting to set RefreshDataOnOpen for pivot table '{pivotTableName}' in sheet '{sheetName}'.");
                    pivotTable.CacheDefinition.Refresh();
                    Logger.LogInfo($"Set pivot table '{pivotTableName}' in sheet '{sheetName}' to refresh on load (RefreshDataOnOpen=true).");
>>>>>>> parent of 171b8e4 (v1.9.2)
                }
                catch (Exception ex)
                {
                    Logger.LogError($"Error setting RefreshDataOnOpen for pivot table '{pivotTableName}' in '{sheetName}': {ex.Message}");
                }
            }
<<<<<<< HEAD
        }

        /// <summary>
        /// Copies data from the "Analysis" sheet of the processed report to a central Power BI source Excel file.
        /// </summary>
        private async Task CopyAnalysisDataToPowerBIReportAsync(
            ExcelPackage sourcePackage, string sourceAnalysisSheetName, string targetPowerBiSheetName,
            ReportType reportType, string originalSourceFilePath, DateTime reportDate,
            string powerBiDataFilePathConfigKey, CancellationToken cancellationToken)
        {
            string? destinationPowerBiFilePath = _configuration[powerBiDataFilePathConfigKey];
            if (string.IsNullOrEmpty(destinationPowerBiFilePath))
            {
                _statusManager.Post("Error: Central Power BI report path invalid.", MessageType.Error);
                return;
            }
            destinationPowerBiFilePath = Environment.ExpandEnvironmentVariables(destinationPowerBiFilePath);
            if (!File.Exists(destinationPowerBiFilePath))
            {
                _statusManager.Post("Error: Central Power BI report file not found.", MessageType.Error);
                return;
            }
            ExcelWorksheet? sourceWorksheet = sourcePackage.Workbook.Worksheets[sourceAnalysisSheetName];
            if (sourceWorksheet == null || sourceWorksheet.Dimension == null)
            {
                _statusManager.Post("Warning: No analysis data to copy to Power BI.", MessageType.Warning, TimeSpan.FromSeconds(5));
=======
            else
            {
                Logger.LogWarning($"Pivot table '{pivotTableName}' not found in sheet '{sheetName}'. Available tables: {string.Join(", ", worksheet.PivotTables.Select(pt => pt.Name))}");
            }
            Logger.LogTrace($"Exiting RefreshPivotTable.");
        }

        private async Task CopyAnalysisDataToPowerBIReportAsync(
            ExcelPackage sourcePackage,
            string sourceSheetName,
            IProgress<ProgressReport>? progress,
            int reportType,
            string originalSourceFilePath,
            DateTime reportDate,
            CancellationToken cancellationToken)
        {
            Logger.LogTrace($"Entering CopyAnalysisDataToPowerBIReportAsync(sourceSheet: {sourceSheetName})");
            string username = Environment.UserName;
            string destinationFilePath = GetWeeklyReportPath(username);

            if (string.IsNullOrEmpty(destinationFilePath))
            {
                Logger.LogError($"Central Power BI report path is invalid or could not be determined. Cannot append data.");
                progress?.Report(new ProgressReport("Error: Central Power BI report path invalid."));
                Logger.LogTrace($"Exiting CopyAnalysisDataToPowerBIReportAsync early - invalid destination path.");
                return;
            }
            if (!File.Exists(destinationFilePath))
            {
                Logger.LogError($"Central Power BI report file not found: '{destinationFilePath}'. Cannot append data.");
                progress?.Report(new ProgressReport("Error: Central Power BI report file not found."));
                Logger.LogTrace($"Exiting CopyAnalysisDataToPowerBIReportAsync early - destination file not found.");
                return;
            }

            ExcelWorksheet? sourceWorksheet = sourcePackage.Workbook.Worksheets[sourceSheetName];
            if (sourceWorksheet == null || sourceWorksheet.Dimension == null)
            {
                Logger.LogWarning($"Source analysis sheet '{sourceSheetName}' not found or is empty. Cannot copy to Power BI report.");
                progress?.Report(new ProgressReport("Warning: No analysis data to copy to Power BI report."));
                Logger.LogTrace($"Exiting CopyAnalysisDataToPowerBIReportAsync early - source sheet not found or empty.");
>>>>>>> parent of 171b8e4 (v1.9.2)
                return;
            }
            try
            {
<<<<<<< HEAD
                using var destinationPackage = await Task.Run(() => new ExcelPackage(new FileInfo(destinationPowerBiFilePath)), cancellationToken);
                ExcelWorksheet? destinationWorksheet = destinationPackage.Workbook.Worksheets[targetPowerBiSheetName];
                if (destinationWorksheet == null)
                {
                    destinationWorksheet = destinationPackage.Workbook.Worksheets.Add(targetPowerBiSheetName);
                    CopyHeaders(sourceWorksheet, destinationWorksheet, 1, 5);
                }
                int nextFreeRowInPowerBiSheet = await Task.Run(() => GetNextFreeRow(destinationWorksheet, CustomerColumnIndex), cancellationToken);
                string filenameForPowerBiColumn = GenerateFinalFileName(reportType, reportDate, DateTime.Now);
                await Task.Run(() =>
                {
                    int sourceAnalysisRowCount = sourceWorksheet.Dimension.Rows;
                    int sourceAnalysisColCount = sourceWorksheet.Dimension.Columns;
                    const int startDataRowInAnalysis = 6;
                    if (sourceAnalysisRowCount < startDataRowInAnalysis) return;
                    for (int sourceRow = startDataRowInAnalysis; sourceRow <= sourceAnalysisRowCount; sourceRow++)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        if (sourceWorksheet.Cells[sourceRow, CustomerColumnIndex].Value != null && !string.IsNullOrWhiteSpace(sourceWorksheet.Cells[sourceRow, CustomerColumnIndex].Value.ToString()))
                        {
                            for (int col = 1; col <= sourceAnalysisColCount; col++)
=======
                Logger.LogInfo($"Opening Power BI report file for appending: {destinationFilePath}");
                using var destinationPackage = await Task.Run(() => new ExcelPackage(new FileInfo(destinationFilePath)), cancellationToken);
                Logger.LogTrace($"CopyAnalysisDataToPowerBIReportAsync: Destination package opened.");

                string targetSheetName = PowerBISheetName;
                ExcelWorksheet? destinationWorksheet = destinationPackage.Workbook.Worksheets[targetSheetName];

                if (destinationWorksheet == null)
                {
                    Logger.LogTrace($"CopyAnalysisDataToPowerBIReportAsync: Destination sheet '{targetSheetName}' not found, creating...");
                    destinationWorksheet = destinationPackage.Workbook.Worksheets.Add(targetSheetName);
                    CopyHeaders(sourceWorksheet, destinationWorksheet, 1, 5);
                    Logger.LogInfo($"Created sheet '{targetSheetName}' in Power BI report and copied headers from '{sourceSheetName}'.");
                }

                int nextFreeRow = await Task.Run(() => GetNextFreeRow(destinationWorksheet, CustomerColumnIndex), cancellationToken);
                Logger.LogDebug($"Next free row in Power BI report sheet '{targetSheetName}' is {nextFreeRow}.");

                string filenameToWriteIntoColumn = GenerateFinalFileName(reportType, reportDate, DateTime.Now);
                Logger.LogDebug($"Using filename for Power BI report append (Source File Name column): {filenameToWriteIntoColumn}");

                Logger.LogTrace($"CopyAnalysisDataToPowerBIReportAsync: Starting row copy task...");
                await Task.Run(() =>
                {
                    int sourceRowCount = sourceWorksheet.Dimension.Rows;
                    int sourceColCount = 0;
                    const int headerRowForColumnCount = 5;

                    if (sourceWorksheet.Dimension.Rows >= headerRowForColumnCount)
                    {
                        for (int c = sourceWorksheet.Dimension.Columns; c >= 1; c--)
                        {
                            if (sourceWorksheet.Cells[headerRowForColumnCount, c].Value != null &&
                                !string.IsNullOrWhiteSpace(sourceWorksheet.Cells[headerRowForColumnCount, c].Value.ToString()))
                            {
                                sourceColCount = c;
                                break;
                            }
                        }
                    }
                    if (sourceColCount == 0) sourceColCount = LastClearableColumnAnalysis;

                    Logger.LogDebug($"Determined {sourceColCount} columns to copy from Analysis sheet '{sourceSheetName}'.");

                    const int startDataRowInAnalysisSheet = 6;

                    if (sourceRowCount < startDataRowInAnalysisSheet)
                    {
                        Logger.LogWarning($"Source analysis sheet '{sourceSheetName}' has no data rows starting from row {startDataRowInAnalysisSheet}.");
                        return;
                    }

                    int copiedRowCount = 0;
                    for (int sourceRow = startDataRowInAnalysisSheet; sourceRow <= sourceRowCount; sourceRow++)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        var firstCellVal = sourceWorksheet.Cells[sourceRow, CustomerColumnIndex].Value;
                        if (firstCellVal != null && !string.IsNullOrWhiteSpace(firstCellVal.ToString()))
                        {
                            for (int col = 1; col <= sourceColCount; col++)
>>>>>>> parent of 171b8e4 (v1.9.2)
                            {
                                destinationWorksheet.Cells[nextFreeRow, col].Value = sourceWorksheet.Cells[sourceRow, col].Value;
                            }
<<<<<<< HEAD
                            destinationWorksheet.Cells[nextFreeRowInPowerBiSheet, AnalysisSheetSourceFileNameColumnIndex].Value = filenameForPowerBiColumn;
                            nextFreeRowInPowerBiSheet++;
                        }
                    }
                }, cancellationToken);
                await destinationPackage.SaveAsync(cancellationToken);
                _statusManager.Post("Data appended to Power BI source.", MessageType.Success, TimeSpan.FromSeconds(5));
=======
                            destinationWorksheet.Cells[nextFreeRow, SourceFileNameColumnIndex].Value = filenameToWriteIntoColumn;
                            nextFreeRow++;
                            copiedRowCount++;
                        }

                        if ((sourceRow - startDataRowInAnalysisSheet + 1) % 50 == 0 && sourceRowCount > startDataRowInAnalysisSheet)
                        {
                            int percent = (int)((double)(sourceRow - startDataRowInAnalysisSheet + 1) / (sourceRowCount - startDataRowInAnalysisSheet + 1) * 100);
                            progress?.Report(new ProgressReport($"Copying to Power BI report... {Math.Min(100, percent)}%", Math.Min(100, percent)));
                        }
                    }
                    Logger.LogInfo($"Copied values for {copiedRowCount} rows from '{sourceSheetName}' to Power BI report sheet '{targetSheetName}'.");
                    if (sourceRowCount >= startDataRowInAnalysisSheet)
                        progress?.Report(new ProgressReport($"Copying to Power BI report... 100%", 100));
                }, cancellationToken);
                Logger.LogTrace($"CopyAnalysisDataToPowerBIReportAsync: Row copy task finished.");

                Logger.LogTrace($"CopyAnalysisDataToPowerBIReportAsync: Saving destination package...");
                await destinationPackage.SaveAsync(cancellationToken);
                Logger.LogInfo($"Successfully appended data to sheet '{targetSheetName}' in '{destinationFilePath}'.");
                progress?.Report(new ProgressReport("Data appended to Power BI report."));
                Logger.LogTrace($"CopyAnalysisDataToPowerBIReportAsync: Destination package saved.");

>>>>>>> parent of 171b8e4 (v1.9.2)
            }
            catch (Exception ex)
            {
<<<<<<< HEAD
                Logger.LogError($"Error copying data to Power BI source file: {ex.Message}", ex);
                _statusManager.Post($"Error (Power BI): {ex.Message}", MessageType.Error);
            }
=======
                Logger.LogWarning("Operation cancelled during copy to Power BI report.");
                progress?.Report(new ProgressReport("Cancelled copy to Power BI report."));
                throw;
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error copying data to Power BI report '{destinationFilePath}': {ex}");
                progress?.Report(new ProgressReport($"Error copying to Power BI report: {ex.Message}"));
            }
            Logger.LogTrace($"Exiting CopyAnalysisDataToPowerBIReportAsync.");
>>>>>>> parent of 171b8e4 (v1.9.2)
        }

        private void CopyHeaders(ExcelWorksheet sourceSheet, ExcelWorksheet destinationSheet, int startHeaderRow = 1, int endHeaderRow = 1)
        {
<<<<<<< HEAD
            if (sourceSheet.Dimension != null && sourceSheet.Dimension.Rows >= endHeaderRow)
            {
                sourceSheet.Cells[startHeaderRow, 1, endHeaderRow, sourceSheet.Dimension.Columns].Copy(destinationSheet.Cells[startHeaderRow, 1]);
=======
            Logger.LogTrace($"Entering CopyHeaders(source: {sourceSheet.Name}, destination: {destinationSheet.Name}, startRow: {startHeaderRow}, endRow: {endHeaderRow})");
            if (sourceSheet.Dimension != null && sourceSheet.Dimension.Rows >= endHeaderRow)
            {
                int headerColCount = sourceSheet.Dimension.Columns;
                int actualHeaderColCount = 0;
                for (int r = startHeaderRow; r <= endHeaderRow; r++)
                {
                    for (int c = headerColCount; c >= 1; c--)
                    {
                        if (sourceSheet.Cells[r, c].Value != null && !string.IsNullOrWhiteSpace(sourceSheet.Cells[r, c].Value.ToString()))
                        {
                            actualHeaderColCount = Math.Max(actualHeaderColCount, c);
                            break;
                        }
                    }
                }
                if (actualHeaderColCount == 0) actualHeaderColCount = headerColCount;

                ExcelRange sourceHeaderRange = sourceSheet.Cells[startHeaderRow, 1, endHeaderRow, actualHeaderColCount];
                ExcelRange destHeaderRange = destinationSheet.Cells[startHeaderRow, 1, endHeaderRow, actualHeaderColCount];
                sourceHeaderRange.Copy(destHeaderRange);
                Logger.LogTrace($"Copied header rows {startHeaderRow}-{endHeaderRow} (up to column {actualHeaderColCount}) from {sourceSheet.Name} to {destinationSheet.Name}");
            }
            else
            {
                destinationSheet.Cells[1, 1].Value = "DefaultHeader";
                Logger.LogWarning($"Source sheet '{sourceSheet.Name}' for header copy was too small or empty. Added minimal default header to {destinationSheet.Name}.");
>>>>>>> parent of 171b8e4 (v1.9.2)
            }
            Logger.LogTrace($"Exiting CopyHeaders.");
        }

<<<<<<< HEAD
        /// <summary>
        /// Finds the next available (empty) row in a worksheet, starting from a specified data row.
        /// </summary>
        private int GetNextFreeRow(ExcelWorksheet worksheet, int checkColumn = 1)
        {
            if (worksheet.Dimension == null) return 1;
            int lastUsedRow = worksheet.Dimension.End.Row;
=======
        private int GetNextFreeRow(ExcelWorksheet worksheet, int checkColumn = 1)
        {
            Logger.LogTrace($"Entering GetNextFreeRow(worksheet: {worksheet.Name}, checkColumn: {checkColumn})");
            if (worksheet.Dimension == null)
            {
                Logger.LogTrace($"Exiting GetNextFreeRow. Worksheet empty. Result: 1");
                return 1;
            }

            const int firstDataRowAfterHeaders = 6;

            int lastUsedRow = worksheet.Dimension.End.Row;

            if (lastUsedRow < firstDataRowAfterHeaders)
            {
                Logger.LogTrace($"Exiting GetNextFreeRow. Worksheet has only headers or less. Last used row {lastUsedRow}. Result: {firstDataRowAfterHeaders}");
                return firstDataRowAfterHeaders;
            }

>>>>>>> parent of 171b8e4 (v1.9.2)
            for (int r = lastUsedRow; r >= 1; r--)
            {
                if (worksheet.Cells[r, checkColumn].Value != null && !string.IsNullOrWhiteSpace(worksheet.Cells[r, checkColumn].Value.ToString()))
                {
<<<<<<< HEAD
                    return Math.Max(r + 1, 2);
                }
            }
            return 2;
        }
        #endregion

        #region File and Folder Naming Helpers
        /// <summary>
        /// Generates the final filename for a processed report based on its type, primary date, and a run timestamp.
        /// </summary>
        private string GenerateFinalFileName(ReportType reportType, DateTime reportDate, DateTime runTimestamp)
        {
            return reportType switch
            {
                ReportType.Daily => $"{reportDate:yyyyMMdd}_Estimate_Success_Rate_Daily.xlsx",
                ReportType.Daily5Day1k => $"{reportDate:yyyyMMdd}_Estimate_Success_Rate_Daily_5day_1k.xlsx",
                ReportType.Weekly => $"{reportDate:yyyyMMdd} Estimate Success Rate.xlsx",
                ReportType.Monthly => $"Estimate Success Rate {reportDate:MMM yy}.xlsx",
                ReportType.Quarterly => $"Estimate Success Rate {ReportHelper.GetQuarterString(reportDate)}.xlsx",
                ReportType.Annual => $"Estimate Success Rate FY {ReportHelper.GetFinancialYearStartCalendarYear(reportDate, _configuration)}-{ReportHelper.GetFinancialYearStartCalendarYear(reportDate, _configuration) + 1}.xlsx",
                ReportType.Custom => $"{reportDate:yyyyMMdd}_{runTimestamp:HHmmss}_Estimate_Success_Rate_Custom.xlsx",
                _ => $"{reportDate:yyyyMMdd}_Estimate_Success_Rate_UnknownType_{runTimestamp:HHmmss}.xlsx",
            };
        }

        /// <summary>
        /// Asynchronously renames or moves a file, with retry logic for transient IO errors.
        /// </summary>
        private async Task RenameFileWithRetryAsync(string sourcePath, string destinationPath, CancellationToken cancellationToken)
        {
            int maxRetries = _configuration.GetValue<int>(AppConfigKeys.OperationalParameters.GeneralFileOperationMaxRetries, 5);
            int currentDelayMs = _configuration.GetValue<int>(AppConfigKeys.OperationalParameters.GeneralFileOperationDelayMs, 500);

=======
                    int nextRow = Math.Max(r + 1, firstDataRowAfterHeaders);
                    Logger.LogTrace($"Exiting GetNextFreeRow. Last used row in Col{checkColumn}: {r}. Result: {nextRow}");
                    return nextRow;
                }
            }

            Logger.LogTrace($"Exiting GetNextFreeRow. Column {checkColumn} empty or no data found below headers. Result: {firstDataRowAfterHeaders}");
            return firstDataRowAfterHeaders;
        }

        private string GetWeeklyReportPath(string username)
        {
            Logger.LogTrace($"Entering GetWeeklyReportPath(username: {username})");
#if DEBUG
            string path = $@"C:\Users\{username}\Harlow Printing\IT - Documents\PowerBI\Quote Conversion Report\Quotes conversion data_wrangled\weekly report quotes conversion merged - copy.xlsx";
            Logger.LogTrace($"Exiting GetWeeklyReportPath (DEBUG). Result: {path}");
            return path;
#else
            string path = $@"C:\Users\{username}\Harlow Printing\IT - Documents\PowerBI\Quote Conversion Report\Quotes conversion data_wrangled\weekly report quotes conversion merged.xlsx";
            Logger.LogTrace($"Exiting GetWeeklyReportPath (RELEASE). Result: {path}"); 
            return path;
#endif
        }
        #endregion

        #region File and Folder Helpers

        private string GenerateFinalFileName(int reportType, DateTime reportDate, DateTime runTimestamp)
        {
            Logger.LogTrace($"Entering GenerateFinalFileName(reportType: {reportType}, reportDate: {reportDate:d})");
            string fileName;
            switch (reportType)
            {
                case DailyReportIndex:
                    fileName = $"{reportDate:yyyyMMdd}_Estimate_Success_Rate_Daily.xlsx";
                    break;
                case NewDailyReportOver1kIndex:
                    fileName = $"{reportDate:yyyyMMdd}_Estimate_Success_Rate_Daily_5day_1k.xlsx";
                    break;
                case WeeklyReportIndex:
                    fileName = $"{reportDate:yyyyMMdd} Estimate Success Rate.xlsx";
                    break;
                case MonthlyReportIndex:
                    fileName = $"Estimate Success Rate {reportDate:MMM yy}.xlsx";
                    break;
                case QuarterlyReportIndex:
                    int quarter = (reportDate.Month - 1) / 3 + 1;
                    DateTime quarterStartDate = new DateTime(reportDate.Year, (quarter - 1) * 3 + 1, 1);
                    DateTime quarterEndDate = quarterStartDate.AddMonths(3).AddDays(-1);
                    string qtrFolderName = $"{quarterStartDate:MMM} to {quarterEndDate:MMM}{(quarterStartDate.Year != quarterEndDate.Year ? $" {quarterStartDate.Year}-{quarterEndDate.Year}" : $" {quarterStartDate.Year}")}";
                    fileName = $"Estimate Success Rate {qtrFolderName}.xlsx";
                    break;
                case AnnualReportIndex:
                    int finStartYear = reportDate.Month >= 5 ? reportDate.Year : reportDate.Year - 1;
                    fileName = $"Estimate Success Rate FY {finStartYear}-{finStartYear + 1}.xlsx";
                    break;
                case CustomReportIndex:
                    fileName = $"{reportDate:yyyyMMdd}_{runTimestamp:HHmmss}_Estimate_Success_Rate_Custom.xlsx";
                    break;
                default:
                    Logger.LogWarning($"Invalid report type '{reportType}' for filename generation, defaulting to generic format using report date.");
                    fileName = $"{reportDate:yyyyMMdd}_Estimate_Success_Rate_UnknownType.xlsx";
                    break;
            }
            Logger.LogTrace($"Exiting GenerateFinalFileName. Result: {fileName}");
            return fileName;
        }

        private async Task RenameFileWithRetryAsync(string sourcePath, string destinationPath, IProgress<ProgressReport>? progress, CancellationToken cancellationToken, int maxRetries = 5, int delayMs = 500)
        {
            Logger.LogTrace($"Entering RenameFileWithRetryAsync(source: {sourcePath}, dest: {destinationPath})");
>>>>>>> parent of 171b8e4 (v1.9.2)
            for (int i = 0; i < maxRetries; i++)
            {
                try
                {
                    cancellationToken.ThrowIfCancellationRequested();
<<<<<<< HEAD
                    await Task.Run(() =>
                    {
                        if (File.Exists(destinationPath)) File.Delete(destinationPath);
                        File.Move(sourcePath, destinationPath);
                    }, cancellationToken);
                    return; // Success
                }
                catch (IOException) when (i < maxRetries - 1)
                {
                    _statusManager.Post($"Waiting for file access (Attempt {i + 1})...", MessageType.InProgress);
                    await Task.Delay(currentDelayMs, cancellationToken);
                    currentDelayMs *= 2; // Exponential backoff
                }
            }
            throw new IOException($"Failed to move/rename file '{sourcePath}' to '{destinationPath}' after {maxRetries} attempts.");
=======
                    await Task.Run(() => File.Move(sourcePath, destinationPath, true), cancellationToken);
                    Logger.LogInfo($"Successfully renamed/moved '{sourcePath}' to '{destinationPath}'.");
                    Logger.LogTrace($"Exiting RenameFileWithRetryAsync - Success.");
                    return;
                }
                catch (IOException ex) when (i < maxRetries - 1)
                {
                    Logger.LogWarning($"Attempt {i + 1} failed to rename '{sourcePath}' due to lock/IO error: {ex.Message}. Retrying in {delayMs}ms...");
                    progress?.Report(new ProgressReport($"Waiting for file lock release (Attempt {i + 1})..."));
                    await Task.Delay(delayMs, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    Logger.LogWarning($"Rename operation cancelled while trying to move '{sourcePath}'.");
                    Logger.LogTrace($"Exiting RenameFileWithRetryAsync - Cancelled.");
                    throw;
                }
            }
            Logger.LogTrace($"Exiting RenameFileWithRetryAsync - Failed after retries.");
            throw new IOException($"Failed to rename file '{sourcePath}' to '{destinationPath}' after {maxRetries} attempts. The file might still be locked or another IO error occurred.");
>>>>>>> parent of 171b8e4 (v1.9.2)
        }

        #endregion
    }
}
