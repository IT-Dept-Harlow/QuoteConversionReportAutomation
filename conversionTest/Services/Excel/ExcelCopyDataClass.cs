// ExcelCopyDataClass.cs - Renamed to ExcelCopyData.cs for consistency with class name
// Provides methods for copying data between Excel sheets, filtering,
// and performing related operations asynchronously using EPPlus.
// Aligned with the new appsettings.json structure for operational parameters.

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
using Microsoft.Extensions.Configuration; // For IConfiguration
using OfficeOpenXml; // EPPlus library for Excel manipulation
using OfficeOpenXml.Table.PivotTable; // For pivot table operations

// Project specific namespaces
using QuoteConversionReportAutomation.Helpers; // For FolderCreation
using QuoteConversionReportAutomation.Services.Logging; // For Logger
#endregion

namespace QuoteConversionReportAutomation.Services.Excel
{
    /// <summary>
    /// Represents progress information for Excel operations, including a message and an optional percentage.
    /// </summary>
    /// <param name="Message">The status message describing the current operation.</param>
    /// <param name="Percentage">Optional progress percentage (0-100). Defaults to -1 if not applicable or unknown.</param>
    public record ProgressReport(string Message, int Percentage = -1);

    /// <summary>
    /// Provides methods for copying data between Excel sheets and performing related operations asynchronously.
    /// Uses OfficeOpenXml (EPPlus) for Excel manipulation.
    /// Reads operational parameters like sheet names, filtering thresholds, and financial year settings from <see cref="IConfiguration"/>.
    /// </summary>
    public class ExcelCopyData
    {
        #region Fields
        /// <summary>
        /// Provides access to the application's configuration settings.
        /// </summary>
        private readonly IConfiguration _configuration;
        #endregion

        #region Constants

        // --- Report Type Indices (Must match Form1.cs) ---
        // These help in routing logic based on the report type being processed.
        private const int DailyReportIndex = 0;
        private const int NewDailyReportOver1kIndex = 1; // "Daily (5days >= £1000)"
        private const int WeeklyReportIndex = 2;
        private const int MonthlyReportIndex = 3;
        private const int QuarterlyReportIndex = 4;
        private const int AnnualReportIndex = 5;
        private const int CustomReportIndex = 6;

        // --- Default Column Indices (1-based for EPPlus) ---
        // These are fundamental to the structure of the processed Excel files.
        // If these column structures change, these constants and related logic must be updated.
        private const int CustomerColumnIndex = 1;       // Column A (Customer Name in DATA and Analysis sheets)
        private const int NetValueColumnIndexDataSheet = 7; // Column G in DATA sheet (Net Value for filtering)

        // Analysis Sheet Column Indices
        private const int AnalysisSheetNoOfEstimatesColumnIndex = 4; // Column D ("No of Estimates")
        private const int AnalysisSheetSourceFileNameColumnIndex = 12; // Column L ("Source File Name")
        private const int AnalysisSheetDateColumnIndex = 13;           // Column M ("Date")
        private const int AnalysisSheetFinancialYearColumnIndex = 14;  // Column N ("Financial Year")
        private const int AnalysisSheetFirstClearableColumn = 1;      // Column A (Start of range to clear unused rows)
        private const int AnalysisSheetLastClearableColumn = 14;       // Column N (End of range to clear unused rows)
        #endregion

        #region Constructor
        /// <summary>
        /// Initializes a new instance of the <see cref="ExcelCopyData"/> class.
        /// Sets the EPPlus license context and stores the application configuration.
        /// </summary>
        /// <param name="configuration">The application's configuration settings, used to retrieve operational parameters.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="configuration"/> is null.</exception>
        public ExcelCopyData(IConfiguration configuration)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

            // Set EPPlus License context. Important for EPPlus 5 and later.
            // "Harlow" seems to be a specific identifier used here; ensure this is intended.
            // For non-commercial use, LicenseContext.NonCommercial is standard.
            // For commercial use, a commercial license and LicenseContext.Commercial is required.
            ExcelPackage.License.SetNonCommercialPersonal("Harlow");
            Logger.LogTrace("ExcelCopyData instance created and EPPlus license context set.");
        }
        #endregion

        #region Public Instance Methods

        /// <summary>
        /// Asynchronously processes an Excel report by copying data from a source file to a template,
        /// performing filtering, calculations, and other operations based on the report type.
        /// </summary>
        /// <param name="selectedFinYear">The financial year string (e.g., "2023_24") relevant for some report types.</param>
        /// <param name="reportType">The integer index representing the type of report being processed.</param>
        /// <param name="sourceFilePath">The full path to the raw source Excel file.</param>
        /// <param name="sourceSheetNameConfigKey">The configuration key for the name of the sheet in the source file (e.g., "RawDataSourceSheet").</param>
        /// <param name="baseFileSaveLocation">The base directory where the final processed report will be saved.</param>
        /// <param name="templateFilePath">The full path to the Excel template file.</param>
        /// <param name="destinationDataSheetNameConfigKey">The configuration key for the name of the sheet in the template where data is copied (e.g., "TemplateDataCopySheet").</param>
        /// <param name="startRow">The starting row (1-based) in the source sheet from which to begin copying data.</param>
        /// <param name="startCol">The starting column (1-based) in the source sheet from which to begin copying data.</param>
        /// <param name="progress">An optional progress reporter for UI updates.</param>
        /// <param name="reportDate">The primary date associated with the report, used for filename generation and some internal logic. Defaults to today if not specified for non-custom reports.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A task that represents the asynchronous operation. The task result is the full path to the final processed Excel file, or null if processing fails or is cancelled.</returns>
        /// <exception cref="ArgumentException">Thrown if required path or name parameters are null or empty.</exception>
        /// <exception cref="FileNotFoundException">Thrown if essential files (source, template) are not found.</exception>
        /// <exception cref="InvalidOperationException">Thrown for critical processing errors, such as failure to create output folders or save files.</exception>
        public async Task<string?> ProcessExcelReportAsync(
            string selectedFinYear,
            int reportType,
            string sourceFilePath,
            string sourceSheetNameConfigKey, // Now a config key
            string baseFileSaveLocation,
            string templateFilePath,
            string destinationDataSheetNameConfigKey, // Now a config key
            int startRow = 1,
            int startCol = 1,
            IProgress<ProgressReport>? progress = null,
            DateTime reportDate = default,
            CancellationToken cancellationToken = default)
        {
            Logger.LogTrace($"Entering ProcessExcelReportAsync. ReportType: {reportType}, Source: {sourceFilePath}, Template: {templateFilePath}, ReportDate: {reportDate:yyyy-MM-dd}");
            var stopwatch = Stopwatch.StartNew();

            // Validate required parameters
            ArgumentException.ThrowIfNullOrEmpty(sourceFilePath, nameof(sourceFilePath));
            ArgumentException.ThrowIfNullOrEmpty(sourceSheetNameConfigKey, nameof(sourceSheetNameConfigKey));
            ArgumentException.ThrowIfNullOrEmpty(baseFileSaveLocation, nameof(baseFileSaveLocation));
            ArgumentException.ThrowIfNullOrEmpty(templateFilePath, nameof(templateFilePath));
            ArgumentException.ThrowIfNullOrEmpty(destinationDataSheetNameConfigKey, nameof(destinationDataSheetNameConfigKey));

            // Retrieve sheet names from configuration
            string sourceSheetName = _configuration.GetValue<string>($"OperationalParameters:ExcelSheetNames:{sourceSheetNameConfigKey}", "Sheet1")!;
            string destinationDataSheetName = _configuration.GetValue<string>($"OperationalParameters:ExcelSheetNames:{destinationDataSheetNameConfigKey}", "DATA")!;
            string analysisSheetName = _configuration.GetValue<string>("OperationalParameters:ExcelSheetNames:TemplateAnalysisSheet", "Analysis")!;


            if (reportType == WeeklyReportIndex || reportType == DailyReportIndex || reportType == NewDailyReportOver1kIndex)
            {
                // Financial year is critical for these types if specific logic depends on it,
                // though for automated runs, it's often derived.
                ArgumentException.ThrowIfNullOrEmpty(selectedFinYear, nameof(selectedFinYear));
            }

            if (reportDate == default && reportType != CustomReportIndex)
            {
                reportDate = DateTime.Today; // Default to today for non-custom reports if not specified.
                Logger.LogWarning($"ProcessExcelReportAsync: reportDate not specified for non-custom report. Defaulting to Today for filename: {reportDate:yyyy-MM-dd}");
            }

            string? finalFilePath = null;
            string? tempFilePath = null; // Path for the temporary working copy of the template.
            string? fullOutputFolderPath = null;

            try
            {
                progress?.Report(new ProgressReport("Starting Excel processing...", 0));
                cancellationToken.ThrowIfCancellationRequested();

                // 1. Determine and Create Report-Specific Output Folder
                Logger.LogDebug("ProcessExcelReportAsync: Determining output folder...");
                DateTime folderTimestampDate = reportType == CustomReportIndex ? DateTime.Now : reportDate;
                // The folder names used by FolderCreation might need to align with "OperationalParameters:ReportTypeFolderNames"
                // from appsettings.json if dynamic folder naming is desired from config.
                // Currently, FolderCreation uses its own internal logic or defaults for folder names.
                fullOutputFolderPath = FolderCreation.CreateReportSpecificFolder(reportType, baseFileSaveLocation, folderTimestampDate, _configuration);
                if (string.IsNullOrEmpty(fullOutputFolderPath))
                {
                    throw new InvalidOperationException("Failed to create or determine the report output folder. Check logs for details from FolderCreation utility.");
                }
                progress?.Report(new ProgressReport("Output folder prepared.", 5));
                cancellationToken.ThrowIfCancellationRequested();

                // 2. Define Temporary File Path (in the final output folder to simplify rename)
                tempFilePath = Path.Combine(fullOutputFolderPath, $"temp_processing_{Guid.NewGuid()}.xlsx");
                Logger.LogDebug($"ProcessExcelReportAsync: Using temporary file: {tempFilePath}");

                // 3. Copy Template to Temporary Location
                if (!File.Exists(templateFilePath))
                {
                    throw new FileNotFoundException($"Excel template file not found: {templateFilePath}", templateFilePath);
                }
                Logger.LogDebug($"ProcessExcelReportAsync: Copying template '{templateFilePath}' to '{tempFilePath}'...");
                await Task.Run(() => File.Copy(templateFilePath, tempFilePath, true), cancellationToken); // Overwrite if temp file exists (should be rare due to GUID).
                progress?.Report(new ProgressReport("Template copied to temporary location.", 10));
                cancellationToken.ThrowIfCancellationRequested();

                // 4. Open Excel Packages and Perform Data Copy/Filtering
                progress?.Report(new ProgressReport("Opening Excel files for processing...", 15));
                Logger.LogDebug($"ProcessExcelReportAsync: Opening source '{sourceFilePath}' and destination (temp) '{tempFilePath}' packages...");

                if (!File.Exists(sourceFilePath))
                {
                    throw new FileNotFoundException($"Raw source data file not found: {sourceFilePath}", sourceFilePath);
                }

                using (var sourcePackage = new ExcelPackage(new FileInfo(sourceFilePath)))
                using (var destinationPackage = new ExcelPackage(new FileInfo(tempFilePath)))
                {
                    Logger.LogDebug("ProcessExcelReportAsync: Excel packages opened.");
                    ExcelWorksheet? sourceWorksheet = sourcePackage.Workbook.Worksheets[sourceSheetName];
                    if (sourceWorksheet == null)
                    {
                        throw new FileNotFoundException($"Source sheet '{sourceSheetName}' not found in source file '{sourceFilePath}'.");
                    }

                    // Get or create the destination "DATA" sheet and clear existing data below headers.
                    ExcelWorksheet destinationDataWorksheet = GetOrCreateDestinationWorksheet(destinationPackage, destinationDataSheetName, sourceWorksheet);

                    int sourceRowCount = sourceWorksheet.Dimension?.Rows ?? 0;
                    int sourceColCount = sourceWorksheet.Dimension?.Columns ?? 0;
                    Logger.LogDebug($"ProcessExcelReportAsync: Source '{sourceSheetName}' dimensions: {sourceRowCount} rows, {sourceColCount} cols. Copying from R{startRow}C{startCol}.");

                    progress?.Report(new ProgressReport("Copying data from source to template...", 20));
                    if (sourceRowCount >= startRow && sourceColCount >= startCol)
                    {
                        // Determine the actual start row for data, skipping header if startRow is 1.
                        int sourceDataActualStartRow = startRow;
                        if (startRow == 1 && sourceRowCount > 1) // If copying from row 1 and there's more than one row, assume row 1 is header.
                        {
                            sourceDataActualStartRow = 2; // Start copying from row 2.
                        }
                        else if (startRow == 1 && sourceRowCount <= 1) // Only header or empty.
                        {
                            Logger.LogInfo($"Source sheet '{sourceSheetName}' has only headers or is empty. No data rows to copy.");
                            sourceDataActualStartRow = sourceRowCount + 1; // Ensures no copy attempt if only header.
                        }

                        if (sourceRowCount >= sourceDataActualStartRow) // Check if there are actual data rows to copy.
                        {
                            ExcelRange sourceRangeToCopy = sourceWorksheet.Cells[sourceDataActualStartRow, startCol, sourceRowCount, sourceColCount];
                            ExcelRange destStartCellForData = destinationDataWorksheet.Cells[2, 1]; // Copy data starting from row 2 in "DATA" sheet.
                            sourceRangeToCopy.Copy(destStartCellForData);
                            Logger.LogInfo($"Data copied from '{sourceSheetName}' (Row {sourceDataActualStartRow}+) to '{destinationDataSheetName}' (Row 2+).");
                        }
                    }
                    else
                    {
                        Logger.LogWarning($"Source sheet '{sourceSheetName}' has insufficient data (Rows: {sourceRowCount}, StartRow: {startRow}) or start column is out of bounds (Cols: {sourceColCount}, StartCol: {startCol}). No data copied.");
                    }
                    progress?.Report(new ProgressReport("Initial data copy to 'DATA' sheet complete.", 30));
                    cancellationToken.ThrowIfCancellationRequested();

                    // Specific filtering for "Daily (5days >= £1000)" report type.
                    if (reportType == NewDailyReportOver1kIndex)
                    {
                        decimal filterThreshold = _configuration.GetValue<decimal>("OperationalParameters:Daily5Day1kFilteringThreshold", 1000m);
                        progress?.Report(new ProgressReport($"Filtering 'DATA' sheet for values >= £{filterThreshold}...", 35));
                        await FilterDataSheetAsync(destinationDataWorksheet, NetValueColumnIndexDataSheet, filterThreshold, progress, cancellationToken);
                        Logger.LogInfo($"'{destinationDataSheetName}' sheet filtered for report type {NewDailyReportOver1kIndex} with threshold {filterThreshold}.");
                        cancellationToken.ThrowIfCancellationRequested();
                    }
                    progress?.Report(new ProgressReport("Data preparation in 'DATA' sheet complete.", 40));

                    // Perform post-copy operations (populating Analysis sheet, calculations, etc.)
                    Logger.LogDebug("ProcessExcelReportAsync: Starting post-copy operations on Analysis sheet...");
                    await ProcessPostCopyOperationsAsync(destinationPackage, destinationDataSheetName, analysisSheetName, reportType, progress, selectedFinYear, sourceFilePath, reportDate, cancellationToken);
                    Logger.LogDebug("ProcessExcelReportAsync: Post-copy operations finished.");

                    progress?.Report(new ProgressReport("Saving processed file...", 85));
                    Logger.LogDebug($"ProcessExcelReportAsync: Saving temporary destination package: {tempFilePath}");
                    try
                    {
                        await destinationPackage.SaveAsync(cancellationToken); // Save changes to the temporary file.
                    }
                    catch (Exception saveEx) // Catch specific save errors if EPPlus throws them.
                    {
                        Logger.LogError($"Error saving temporary Excel package '{tempFilePath}': {saveEx.Message}", saveEx);
                        throw; // Re-throw to be caught by the main try-catch.
                    }
                    Logger.LogDebug($"ProcessExcelReportAsync: Temporary file saved: {tempFilePath}");
                } // End of using sourcePackage and destinationPackage
                Logger.LogDebug("ProcessExcelReportAsync: Excel packages disposed.");

                // Brief delay, sometimes helpful if file system operations are very rapid.
                await Task.Delay(200, cancellationToken); // Reduced delay.
                Logger.LogTrace("ProcessExcelReportAsync: Brief delay after disposing packages.");

                progress?.Report(new ProgressReport("Generating final filename...", 90));
                string generatedFileName = await Task.Run(() => GenerateFinalFileName(reportType, reportDate, DateTime.Now), cancellationToken);
                finalFilePath = Path.Combine(fullOutputFolderPath, generatedFileName);
                Logger.LogInfo($"ProcessExcelReportAsync: Generated final filename: {generatedFileName}. Full path: {finalFilePath}");

                // Rename temporary file to the final filename.
                Logger.LogDebug($"ProcessExcelReportAsync: Attempting to move/rename '{tempFilePath}' to '{finalFilePath}'...");
                await RenameFileWithRetryAsync(tempFilePath, finalFilePath, progress, cancellationToken);
                Logger.LogInfo($"ProcessExcelReportAsync: File successfully moved/renamed to final path: {finalFilePath}");
                tempFilePath = null; // Mark temp file as handled.

                progress?.Report(new ProgressReport("Excel processing complete.", 100));
                Logger.LogInfo($"Excel processing finished. Final file: {finalFilePath}");
                stopwatch.Stop();
                Logger.LogInfo($"ProcessExcelReportAsync completed successfully. Duration: {stopwatch.ElapsedMilliseconds}ms.");
                return finalFilePath;
            }
            catch (OperationCanceledException)
            {
                stopwatch.Stop();
                Logger.LogWarning($"Excel processing was cancelled. Duration: {stopwatch.ElapsedMilliseconds}ms.");
                progress?.Report(new ProgressReport("Operation cancelled."));
                return null;
            }
            catch (FileNotFoundException fnfEx)
            {
                stopwatch.Stop();
                Logger.LogError($"File not found during Excel processing: {fnfEx.FileName}. Message: {fnfEx.Message}", fnfEx);
                progress?.Report(new ProgressReport($"Error: Required file not found - {Path.GetFileName(fnfEx.FileName)}"));
                return null;
            }
            catch (InvalidOperationException opEx) // Catch specific operational errors.
            {
                stopwatch.Stop();
                Logger.LogError($"Operational error during Excel processing: {opEx.Message}", opEx);
                progress?.Report(new ProgressReport($"Error: {opEx.Message}"));
                return null;
            }
            catch (Exception ex) // Catch-all for other unexpected errors.
            {
                stopwatch.Stop();
                Logger.LogError($"Unexpected error during Excel processing: {ex.Message}. Duration: {stopwatch.ElapsedMilliseconds}ms.", ex);
                progress?.Report(new ProgressReport($"Error: {ex.Message}"));
                return null;
            }
            finally
            {
                // Clean up temporary file if it still exists (e.g., due to an error before rename).
                if (tempFilePath != null && File.Exists(tempFilePath))
                {
                    try
                    {
                        Logger.LogDebug($"ProcessExcelReportAsync: Cleaning up lingering temporary file '{tempFilePath}' in finally block...");
                        File.Delete(tempFilePath);
                        Logger.LogInfo($"Deleted temporary file due to error or cancellation: {tempFilePath}");
                    }
                    catch (Exception cleanupEx)
                    {
                        Logger.LogWarning($"Failed to delete temporary file '{tempFilePath}' during cleanup: {cleanupEx.Message}");
                    }
                }
            }
        }

        /// <summary>
        /// Filters rows in the specified worksheet based on a numeric threshold in a given column.
        /// Rows where the value is less than the threshold, or cannot be parsed as a decimal, are deleted.
        /// This method is typically used for the "Daily (5days >= £1000)" report on the "DATA" sheet.
        /// </summary>
        /// <param name="worksheet">The Excel worksheet to filter.</param>
        /// <param name="numericColumnIndex">The 1-based index of the column containing numeric values to check.</param>
        /// <param name="threshold">The decimal threshold. Rows with values less than this will be removed.</param>
        /// <param name="progress">Optional progress reporter.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        private async Task FilterDataSheetAsync(ExcelWorksheet worksheet, int numericColumnIndex, decimal threshold, IProgress<ProgressReport>? progress, CancellationToken cancellationToken)
        {
            Logger.LogInfo($"Starting to filter sheet '{worksheet.Name}' on column {numericColumnIndex} for values >= {threshold}.");
            if (worksheet.Dimension == null || worksheet.Dimension.Rows < 2) // Row 1 is header.
            {
                Logger.LogInfo($"Sheet '{worksheet.Name}' is empty or has only headers. No filtering needed.");
                return;
            }

            // Process rows from bottom up to avoid issues with row index changes after deletion.
            await Task.Run(() =>
            {
                int initialRowCount = worksheet.Dimension.Rows;
                int rowsDeleted = 0;
                int totalDataRows = initialRowCount - 1; // Exclude header row.
                if (totalDataRows <= 0) return; // No data rows to process.

                for (int r = initialRowCount; r >= 2; r--) // Start from last row, down to row 2.
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var cellValue = worksheet.Cells[r, numericColumnIndex].Value;
                    bool deleteRow = true; // Assume row will be deleted unless it meets criteria.

                    if (cellValue != null)
                    {
                        // Attempt to parse cell value as decimal, handling currency symbols and commas.
                        string valStr = cellValue.ToString()!
                                            .Replace("£", "").Replace("$", "").Replace("€", "") // Remove common currency symbols.
                                            .Replace(",", "") // Remove thousand separators.
                                            .Trim();

                        if (decimal.TryParse(valStr, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal amount))
                        {
                            if (amount >= threshold)
                            {
                                deleteRow = false; // Keep row if value meets/exceeds threshold.
                            }
                        }
                        else
                        {
                            Logger.LogDebug($"FilterDataSheetAsync: Could not parse value in Col {numericColumnIndex}, Row {r}: '{cellValue}'. Row will be deleted.");
                        }
                    }
                    else
                    {
                        Logger.LogDebug($"FilterDataSheetAsync: Value in Col {numericColumnIndex}, Row {r} is null/empty. Row will be deleted.");
                    }

                    if (deleteRow)
                    {
                        worksheet.DeleteRow(r, 1); // Delete the entire row.
                        rowsDeleted++;
                    }

                    // Report progress periodically.
                    if ((initialRowCount - r) % 100 == 0 && progress != null)
                    {
                        int processedRows = initialRowCount - r + 1;
                        int percentage = (int)((double)processedRows / totalDataRows * 5); // Scale percentage relative to this filtering step (e.g., 0-5% of total progress bar)
                        progress.Report(new ProgressReport($"Filtering 'DATA' sheet... {processedRows}/{totalDataRows}", 35 + percentage)); // Base progress at 35%
                    }
                }
                Logger.LogInfo($"Filtering of sheet '{worksheet.Name}' complete. {rowsDeleted} rows deleted. {worksheet.Dimension?.Rows - 1 ?? 0} data rows remaining.");
                progress?.Report(new ProgressReport($"Filtering 'DATA' sheet complete.", 40)); // End of this step's progress.

            }, cancellationToken);
        }

        /// <summary>
        /// Filters the "Analysis" sheet to remove rows (customers) where the "No of Estimates" (Column D) is zero or empty.
        /// This is typically called after initial data population and calculation on the Analysis sheet.
        /// </summary>
        /// <param name="package">The Excel package containing the workbook.</param>
        /// <param name="analysisSheetName">The name of the "Analysis" worksheet.</param>
        /// <param name="progress">Optional progress reporter.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        private async Task FilterAnalysisSheetForZeroEstimatesAsync(
            ExcelPackage package,
            string analysisSheetName, // Get from config
            IProgress<ProgressReport>? progress,
            CancellationToken cancellationToken)
        {
            Logger.LogInfo($"Starting to filter Analysis sheet '{analysisSheetName}' for customers with zero estimates (Col D).");
            ExcelWorksheet? worksheet = package.Workbook.Worksheets[analysisSheetName];

            if (worksheet == null || worksheet.Dimension == null)
            {
                Logger.LogWarning($"Analysis sheet '{analysisSheetName}' not found or is empty. No filtering for zero estimates applied.");
                return;
            }

            await Task.Run(() =>
            {
                const int customerDataStartRow = 6; // Customer data typically starts at row 6 in Analysis sheet.
                if (worksheet.Dimension.Rows < customerDataStartRow)
                {
                    Logger.LogInfo($"Analysis sheet '{analysisSheetName}' has no data rows from row {customerDataStartRow}. No zero-estimate filtering needed.");
                    return;
                }

                int initialRowCount = worksheet.Dimension.Rows;
                int rowsDeleted = 0;
                int totalRowsToProcess = Math.Max(1, initialRowCount - customerDataStartRow + 1); // Avoid division by zero.

                Logger.LogDebug($"FilterAnalysisSheetForZeroEstimates: Initial rows: {initialRowCount}, Data starts at: {customerDataStartRow}. Processing {totalRowsToProcess} potential data rows.");

                // Iterate from bottom up to safely delete rows.
                for (int r = initialRowCount; r >= customerDataStartRow; r--)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    // Check if customer name in Column A is empty; if so, it might be a blank template row to skip or already cleared.
                    var customerCell = worksheet.Cells[r, CustomerColumnIndex].Value;
                    if (customerCell == null || string.IsNullOrWhiteSpace(customerCell.ToString()))
                    {
                        // If customer name is blank, check if the whole row is effectively blank (up to relevant columns).
                        // This avoids deleting template formula rows if they appear blank in Col A but have formulas.
                        // However, ClearContentBelowLastCustomer should handle most of this.
                        // For this filter, if Col A is blank, we generally assume it's not a valid customer row to check for deletion based on Col D.
                        continue;
                    }

                    var noOfEstimatesCell = worksheet.Cells[r, AnalysisSheetNoOfEstimatesColumnIndex].Value;
                    decimal numberOfEstimates = 0;
                    if (noOfEstimatesCell != null) // Attempt to parse the "No of Estimates" value.
                    {
                        object cellVal = noOfEstimatesCell;
                        if (cellVal is double dVal) numberOfEstimates = (decimal)dVal;
                        else if (cellVal is int iVal) numberOfEstimates = iVal;
                        else if (cellVal is decimal decVal) numberOfEstimates = decVal;
                        else decimal.TryParse(cellVal.ToString()?.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out numberOfEstimates);
                    }

                    // Delete row if "No of Estimates" is zero or less.
                    if (numberOfEstimates <= 0)
                    {
                        Logger.LogDebug($"FilterAnalysisSheet: Deleting row {r} for customer '{customerCell}' due to zero or invalid estimates (Col D value: '{noOfEstimatesCell}', Parsed: {numberOfEstimates}).");
                        worksheet.DeleteRow(r, 1);
                        rowsDeleted++;
                    }

                    // Report progress periodically.
                    if ((initialRowCount - r) % 20 == 0 && progress != null)
                    {
                        int processedIteration = initialRowCount - r + 1;
                        int percentage = (int)((double)processedIteration / totalRowsToProcess * 5); // Scale for this step (e.g., 0-5%)
                        progress.Report(new ProgressReport($"Filtering Analysis sheet (zero estimates)... {processedIteration}/{totalRowsToProcess}", 55 + percentage)); // Base progress at 55%
                    }
                }
                Logger.LogInfo($"Filtering of Analysis sheet '{analysisSheetName}' for zero estimates complete. {rowsDeleted} customer rows deleted. Current rows: {worksheet.Dimension?.Rows ?? 0}");
                progress?.Report(new ProgressReport("Filtering Analysis sheet (zero estimates) complete.", 60)); // End of this step's progress.
            }, cancellationToken);
        }

        /// <summary>
        /// Gets the current financial year string (e.g., "2023_24" or "FY 23/24").
        /// Uses configured financial year start month and day.
        /// </summary>
        /// <param name="useUnderscoreFormat">If true, returns format "YYYY_YY" (e.g., "2023_24"). Otherwise, "FY YY/YY" (e.g., "FY 23/24").</param>
        /// <returns>The formatted financial year string.</returns>
        public string GetCurrentFinancialYear(bool useUnderscoreFormat = false)
        {
            Logger.LogTrace($"Entering GetCurrentFinancialYear(useUnderscoreFormat: {useUnderscoreFormat})");
            DateTime today = DateTime.Today;
            // Read financial year start month/day from configuration.
            int finYearStartMonth = _configuration.GetValue<int>("OperationalParameters:FinancialYearStartMonth", 5); // Default May
            int finYearStartDay = _configuration.GetValue<int>("OperationalParameters:FinancialYearStartDay", 1);   // Default 1st

            // Determine the calendar year in which the current financial year started.
            int startYear = (today.Month > finYearStartMonth || (today.Month == finYearStartMonth && today.Day >= finYearStartDay))
                            ? today.Year
                            : today.Year - 1;
            int endYear = startYear + 1;

            string result = useUnderscoreFormat
                ? $"{startYear}_{endYear.ToString()[2..]}" // Format: 2023_24
                : $"FY {startYear.ToString()[2..]}/{endYear.ToString()[2..]}"; // Format: FY 23/24

            Logger.LogTrace($"Exiting GetCurrentFinancialYear. Result: {result} (using StartMonth: {finYearStartMonth}, StartDay: {finYearStartDay})");
            return result;
        }

        /// <summary>
        /// Gets the previous financial year string from a given current financial year string (underscore format).
        /// Uses configured financial year start month and day for context if needed, though direct calculation is usually possible.
        /// </summary>
        /// <param name="currentFinancialYearUnderscore">The current financial year in "YYYY_YY" format (e.g., "2023_24").</param>
        /// <returns>The previous financial year in "YYYY_YY" format, or null if input is invalid.</returns>
        public string? GetPreviousFinancialYear(string currentFinancialYearUnderscore)
        {
            Logger.LogTrace($"Entering GetPreviousFinancialYear(currentFinancialYearUnderscore: {currentFinancialYearUnderscore})");
            if (string.IsNullOrEmpty(currentFinancialYearUnderscore))
            {
                Logger.LogWarning("GetPreviousFinancialYear: Input currentFinancialYearUnderscore was null or empty.");
                return null;
            }
            string[] parts = currentFinancialYearUnderscore.Split('_');
            string? result = null;
            if (parts.Length == 2 && int.TryParse(parts[0], out int startYear))
            {
                int prevStartYear = startYear - 1;
                // The second part of the previous FY (e.g., "23" for "2022_23") comes from the startYear.
                result = $"{prevStartYear}_{startYear.ToString()[2..]}";
            }
            else
            {
                Logger.LogWarning($"Invalid financial year format for calculating previous: {currentFinancialYearUnderscore}. Expected YYYY_YY.");
            }
            Logger.LogTrace($"Exiting GetPreviousFinancialYear. Result: {result ?? "null"}");
            return result;
        }

        /// <summary>
        /// Validates if a given date range falls entirely within a specified financial year (underscore format).
        /// Uses configured financial year start month and day.
        /// </summary>
        /// <param name="selectedFinYearUnderscore">The financial year to validate against, in "YYYY_YY" format (e.g., "2023_24").</param>
        /// <param name="fromDate">The start date of the range to check.</param>
        /// <param name="toDate">The end date of the range to check.</param>
        /// <returns>True if the date range is valid for the financial year; otherwise, false.</returns>
        public bool IsFinancialYearValid(string selectedFinYearUnderscore, DateTime fromDate, DateTime toDate)
        {
            Logger.LogTrace($"Entering IsFinancialYearValid(selectedFinYear: {selectedFinYearUnderscore}, from: {fromDate:d}, to: {toDate:d})");
            if (string.IsNullOrEmpty(selectedFinYearUnderscore))
            {
                Logger.LogWarning("IsFinancialYearValid: selectedFinYearUnderscore was null or empty.");
                return false;
            }
            string[] parts = selectedFinYearUnderscore.Split('_');
            bool isValid = false;
            if (parts.Length == 2 && int.TryParse(parts[0], out int startYear))
            {
                // Read financial year start month/day from configuration.
                int finYearStartMonth = _configuration.GetValue<int>("OperationalParameters:FinancialYearStartMonth", 5);
                int finYearStartDay = _configuration.GetValue<int>("OperationalParameters:FinancialYearStartDay", 1);

                DateTime fyStartDate = new DateTime(startYear, finYearStartMonth, finYearStartDay);
                DateTime fyEndDate = fyStartDate.AddYears(1).AddDays(-1); // End of the financial year.

                isValid = fromDate >= fyStartDate && toDate <= fyEndDate;
                if (!isValid)
                {
                    Logger.LogWarning($"Date range {fromDate:yyyy-MM-dd} to {toDate:yyyy-MM-dd} is outside selected FY {selectedFinYearUnderscore} (Effective: {fyStartDate:yyyy-MM-dd} to {fyEndDate:yyyy-MM-dd}).");
                }
            }
            else
            {
                Logger.LogWarning($"Invalid financial year format for validation: {selectedFinYearUnderscore}. Expected YYYY_YY.");
            }
            Logger.LogTrace($"Exiting IsFinancialYearValid. Result: {isValid}");
            return isValid;
        }

        /// <summary>
        /// Gets the expected full file path for a final processed report.
        /// Used for checking if a report for the period already exists.
        /// </summary>
        /// <param name="reportType">The integer index of the report type.</param>
        /// <param name="baseFileSaveLocation">The base directory where final reports are saved.</param>
        /// <param name="reportDate">The primary date for the report (e.g., end date).</param>
        /// <returns>The expected full file path, or null if an error occurs.</returns>
        public string? GetExpectedFinalFilePath(int reportType, string baseFileSaveLocation, DateTime reportDate)
        {
            Logger.LogTrace($"Entering GetExpectedFinalFilePath(reportType: {reportType}, base: {baseFileSaveLocation}, date: {reportDate:d})");
            string? result = null;
            try
            {
                if (reportDate == default && reportType != CustomReportIndex)
                {
                    reportDate = DateTime.Today; // Default for non-custom if not specified.
                }

                DateTime folderTimestampDate = reportType == CustomReportIndex ? DateTime.Now : reportDate;
                // FolderCreation uses its internal logic for folder names. This could be aligned with config if needed.
                string? folderPath = FolderCreation.GetReportSpecificFolderPath(reportType, baseFileSaveLocation, folderTimestampDate, _configuration);
                if (!string.IsNullOrEmpty(folderPath))
                {
                    string fileName = GenerateFinalFileName(reportType, reportDate, DateTime.Now); // Uses current time for timestamp in custom filenames.
                    result = Path.Combine(folderPath, fileName);
                }
                else
                {
                    Logger.LogError("GetExpectedFinalFilePath: Failed to determine folder path using FolderCreation utility.");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error in GetExpectedFinalFilePath: {ex.Message}", ex);
            }
            Logger.LogTrace($"Exiting GetExpectedFinalFilePath. Result: {result ?? "null"}");
            return result;
        }

        // Note: GetWeekOfMonth is already present in FolderCreation.cs, so it can be used from there
        // if needed within this class, or duplicated if preferred for encapsulation.
        // For now, assuming FolderCreation.GetWeekOfMonth is the primary source.

        #endregion

        #region Internal Processing Steps (Private Methods)

        // --- Method Summaries for Internal Processing Steps ---
        // ProcessPostCopyOperationsAsync: Orchestrates steps after initial data copy (customer extraction, calculations, pivot refresh, PowerBI append).
        // GetOrCreateDestinationWorksheet: Ensures the target "DATA" sheet exists in the template and clears old data.
        // ExtractUniqueCustomersAsync: Extracts unique customer names from "DATA" and populates them into "Analysis" sheet.
        // CalculateSheet: Triggers Excel calculation engine for the workbook.
        // ClearContentBelowLastCustomer: Cleans up unused rows in the "Analysis" sheet below the last customer.
        // RefreshPivotTable: Sets pivot tables in specified sheets to refresh when the file is opened.
        // CopyAnalysisDataToPowerBIReportAsync: Appends data from "Analysis" sheet to a central Power BI source file (for Weekly reports).
        // CopyHeaders: Utility to copy header rows between sheets.
        // GetNextFreeRow: Finds the next available row in a sheet for appending data.
        // GetWeeklyReportPath: Determines the path to the central Power BI weekly report file (currently hardcoded with DEBUG/RELEASE paths).

        /// <summary>
        /// Orchestrates various operations after the initial data copy from the raw report to the template's "DATA" sheet.
        /// This includes extracting unique customers to the "Analysis" sheet, performing calculations,
        /// cleaning up unused rows, refreshing pivot tables (if applicable), and appending data to a Power BI source file (for weekly reports).
        /// </summary>
        private async Task ProcessPostCopyOperationsAsync(
            ExcelPackage package,
            string sourceDataSheetName, // Name of the sheet containing the copied raw data (e.g., "DATA")
            string targetAnalysisSheetName, // Name of the sheet for analysis (e.g., "Analysis")
            int reportType,
            IProgress<ProgressReport>? progress,
            string selectedFinYear, // Passed for context, e.g. for PowerBI append or Analysis sheet population
            string originalSourceFilePath, // Filename of the raw report, for logging or writing to Analysis sheet
            DateTime reportDate, // Primary date of the report, for logging or writing to Analysis sheet
            CancellationToken cancellationToken)
        {
            Logger.LogTrace($"Entering ProcessPostCopyOperationsAsync(sourceSheet: {sourceDataSheetName}, targetAnalysisSheet: {targetAnalysisSheetName}, reportType: {reportType})");
            var stopwatch = Stopwatch.StartNew();

            // Retrieve other relevant sheet names from configuration
            string powerBiSheetName = _configuration.GetValue<string>("OperationalParameters:ExcelSheetNames:PowerBiDataSheet", "powerBI")!;
            string monthlyOrderPivotSheetName = _configuration.GetValue<string>("OperationalParameters:ExcelSheetNames:MonthlyOrderPivotSheet", "OrderPivot")!;
            string monthlyEstimatePivotSheetName = _configuration.GetValue<string>("OperationalParameters:ExcelSheetNames:MonthlyEstimatePivotSheet", "Estimate Success PivotTable")!;
            string monthlyOrderPivotName = _configuration.GetValue<string>("OperationalParameters:PivotTableNames:MonthlyOrderPivot", "PivotTable1")!;
            string monthlyEstimatePivotName = _configuration.GetValue<string>("OperationalParameters:PivotTableNames:MonthlyEstimatePivot", "PivotTable3")!;


            progress?.Report(new ProgressReport("Extracting unique customers to Analysis sheet...", 40));
            await ExtractUniqueCustomersAsync(package, sourceDataSheetName, targetAnalysisSheetName, reportType, progress, originalSourceFilePath, reportDate, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            progress?.Report(new ProgressReport("Calculating formulas in Analysis sheet...", 50));
            await Task.Run(() => CalculateSheet(package, targetAnalysisSheetName), cancellationToken); // Calculate after populating customers
            cancellationToken.ThrowIfCancellationRequested();

            // Specific filtering for "Daily (5days >= £1000)" report type on the Analysis sheet.
            if (reportType == NewDailyReportOver1kIndex)
            {
                progress?.Report(new ProgressReport("Filtering Analysis sheet for zero estimates/values...", 55));
                await FilterAnalysisSheetForZeroEstimatesAsync(package, targetAnalysisSheetName, progress, cancellationToken);
                Logger.LogInfo($"Analysis sheet '{targetAnalysisSheetName}' filtered for zero estimates/values for report type {NewDailyReportOver1kIndex}.");
                cancellationToken.ThrowIfCancellationRequested();
            }

            progress?.Report(new ProgressReport("Cleaning unused rows in Analysis sheet...", 60));
            await Task.Run(() => ClearContentBelowLastCustomer(package, targetAnalysisSheetName, CustomerColumnIndex, AnalysisSheetFirstClearableColumn, AnalysisSheetLastClearableColumn), cancellationToken);
            Logger.LogDebug($"Cleaned content below last customer in Analysis sheet '{targetAnalysisSheetName}'.");
            cancellationToken.ThrowIfCancellationRequested();

            // Refresh Pivot Tables for relevant report types that use templates with pivots.
            if (reportType is MonthlyReportIndex or QuarterlyReportIndex or AnnualReportIndex or CustomReportIndex)
            {
                // For Custom, assume it might use the monthly template with pivots.
                // A more robust way would be to check the template name or have a specific flag in AutoReportDefinition.
                Logger.LogInfo($"Report type {reportType} may require pivot table refresh. Setting pivots to refresh on open.");
                progress?.Report(new ProgressReport("Setting pivot tables to refresh on load...", 70));
                await Task.Run(() => RefreshPivotTable(package, monthlyOrderPivotSheetName, monthlyOrderPivotName), cancellationToken);
                await Task.Run(() => RefreshPivotTable(package, monthlyEstimatePivotSheetName, monthlyEstimatePivotName), cancellationToken);
                Logger.LogInfo($"Pivot tables in sheets '{monthlyOrderPivotSheetName}' and '{monthlyEstimatePivotSheetName}' set to refresh on load.");
                cancellationToken.ThrowIfCancellationRequested();
            }
            else
            {
                Logger.LogInfo($"Skipping Pivot Table refresh setting for report type {reportType} as it likely uses a standard template without these specific pivots.");
            }

            // Append data to Power BI report for Weekly reports.
            if (reportType == WeeklyReportIndex)
            {
                progress?.Report(new ProgressReport("Appending data to Power BI source file...", 75));
                await CopyAnalysisDataToPowerBIReportAsync(package, targetAnalysisSheetName, powerBiSheetName, progress, reportType, originalSourceFilePath, reportDate, cancellationToken);
                Logger.LogInfo("Data appended to Power BI report source file.");
                cancellationToken.ThrowIfCancellationRequested();
            }
            stopwatch.Stop();
            Logger.LogDebug($"Exiting ProcessPostCopyOperationsAsync. Duration: {stopwatch.ElapsedMilliseconds}ms");
        }

        /// <summary>
        /// Ensures the destination "DATA" sheet exists in the package. If it exists, clears data rows (below header).
        /// If it doesn't exist, creates it and copies headers from the source worksheet.
        /// </summary>
        private ExcelWorksheet GetOrCreateDestinationWorksheet(ExcelPackage package, string sheetName, ExcelWorksheet sourceWorksheet)
        {
            Logger.LogTrace($"Ensuring destination sheet '{sheetName}' exists and is prepared.");
            ExcelWorksheet? worksheet = package.Workbook.Worksheets[sheetName];
            if (worksheet == null) // Sheet doesn't exist, create it and copy headers.
            {
                worksheet = package.Workbook.Worksheets.Add(sheetName);
                Logger.LogInfo($"Created destination sheet '{sheetName}'.");
                // Copy headers from row 1 of sourceWorksheet to row 1 of new worksheet.
                if (sourceWorksheet.Dimension != null && sourceWorksheet.Dimension.Rows >= 1)
                {
                    int headerColCount = sourceWorksheet.Dimension.Columns;
                    if (headerColCount > 0)
                    {
                        ExcelRange sourceHeaderRow = sourceWorksheet.Cells[1, 1, 1, headerColCount];
                        ExcelRange destHeader = worksheet.Cells[1, 1, 1, headerColCount];
                        sourceHeaderRow.Copy(destHeader);
                        Logger.LogInfo($"Copied headers from '{sourceWorksheet.Name}' to new sheet '{sheetName}'.");
                    }
                    else
                    {
                        Logger.LogWarning($"Source sheet '{sourceWorksheet.Name}' has no columns in header row. No headers copied to '{sheetName}'.");
                    }
                }
                else
                {
                    // Add a minimal default header if source is empty.
                    worksheet.Cells[1, 1].Value = "Default_Header_Column_1";
                    Logger.LogWarning($"Source sheet '{sourceWorksheet.Name}' was empty or had no header row. Added minimal default header to '{sheetName}'.");
                }
            }
            else // Sheet exists, clear existing data rows (from row 2 downwards).
            {
                Logger.LogInfo($"Destination sheet '{sheetName}' already exists. Clearing data rows (from row 2).");
                if (worksheet.Dimension != null && worksheet.Dimension.Rows > 1)
                {
                    worksheet.DeleteRow(2, worksheet.Dimension.Rows - 1); // Delete all rows from 2 to the end.
                    Logger.LogDebug($"Cleared existing data (rows 2 to {worksheet.Dimension.Rows + (worksheet.Dimension.Rows - 1)}) from sheet '{sheetName}'. Headers in row 1 preserved.");
                }
                else
                {
                    Logger.LogDebug($"Sheet '{sheetName}' existed but had no data below header row (or was empty). No rows deleted.");
                }
            }
            return worksheet;
        }

        /// <summary>
        /// Extracts unique customer names from the source "DATA" sheet and populates them into the "Analysis" sheet.
        /// Also populates related data columns (Date, Financial Year, Source File Name) in the "Analysis" sheet.
        /// Preserves or copies formulas from a template row for new customer entries.
        /// </summary>
        private async Task ExtractUniqueCustomersAsync(
             ExcelPackage package,
             string sourceDataSheetName,  // e.g., "DATA"
             string targetAnalysisSheetName, // e.g., "Analysis"
             int reportType,
             IProgress<ProgressReport>? progress,
             string originalSourceFilePath, // For "Source File Name" column
             DateTime reportDate,           // For "Date" column
             CancellationToken cancellationToken)
        {
            Logger.LogTrace($"Entering ExtractUniqueCustomersAsync: Source='{sourceDataSheetName}', Target='{targetAnalysisSheetName}'");
            ExcelWorksheet? dataSheet = package.Workbook.Worksheets[sourceDataSheetName];
            ExcelWorksheet analysisSheet = package.Workbook.Worksheets[targetAnalysisSheetName]
                                           ?? throw new InvalidOperationException($"Target Analysis sheet '{targetAnalysisSheetName}' not found in workbook. Template might be missing this sheet.");

            if (dataSheet == null)
            {
                Logger.LogError($"Source data sheet ('{sourceDataSheetName}') not found for customer extraction. Cannot populate Analysis sheet.");
                return;
            }

            const int analysisPopulateStartRow = 6;    // Customer data starts at row 6 in "Analysis" sheet.
            const int templateFormulaRow = 6;          // Row in "Analysis" sheet that contains template formulas.
                                                       // This limit defines how many rows of pre-existing formulas we expect in the template.
                                                       // Rows added beyond this will have row 6 copied to them.
            const int templateFormulaLimitRow = 2000;

            // Check if the template formula row (row 6) exists and has content/formulas.
            bool templateRowExistsAndNotEmpty = analysisSheet.Dimension != null &&
                                               analysisSheet.Dimension.Rows >= templateFormulaRow &&
                                               analysisSheet.Cells[templateFormulaRow, CustomerColumnIndex, templateFormulaRow, AnalysisSheetLastClearableColumn]
                                                          .Any(cell => cell.Value != null || !string.IsNullOrEmpty(cell.Formula));

            if (!templateRowExistsAndNotEmpty)
            {
                Logger.LogWarning($"Analysis sheet '{targetAnalysisSheetName}' template row {templateFormulaRow} is missing or empty. Formula propagation for new customer rows might not work as expected. Ensure template has formulas in this row.");
            }

            int dataSheetStartRow = 2; // Data starts from row 2 in "DATA" sheet (assuming row 1 is header).
            int dataSheetRowCount = dataSheet.Dimension?.Rows ?? 0;

            string sourceFileNameForAnalysis = Path.GetFileName(originalSourceFilePath);
            string currentFY = GetCurrentFinancialYear(false); // Get "FY YY/YY" format.

            Logger.LogDebug("ExtractUniqueCustomersAsync: Extracting unique customer names from DATA sheet...");
            List<string> uniqueCustomers;
            if (dataSheetRowCount < dataSheetStartRow)
            {
                Logger.LogWarning($"Source data sheet '{sourceDataSheetName}' has no data rows. No customers to extract.");
                uniqueCustomers = new List<string>();
            }
            else
            {
                uniqueCustomers = await Task.Run(() =>
                {
                    var customers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    for (int row = dataSheetStartRow; row <= dataSheetRowCount; row++)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        string? customerName = dataSheet.Cells[row, CustomerColumnIndex].Value?.ToString()?.Trim();
                        if (!string.IsNullOrWhiteSpace(customerName))
                        {
                            customers.Add(customerName);
                        }
                    }
                    return customers.OrderBy(c => c, StringComparer.OrdinalIgnoreCase).ToList(); // Sort customers.
                }, cancellationToken);
            }

            Logger.LogInfo($"Found {uniqueCustomers.Count} unique customers from '{sourceDataSheetName}'.");
            progress?.Report(new ProgressReport($"Extracted {uniqueCustomers.Count} unique customers.", 45));

            // Pre-clear direct input columns (A, L, M, N) in the Analysis sheet's existing data area
            // to avoid carrying over old values if fewer customers are populated this time.
            // Only clear up to the templateFormulaLimitRow or actual sheet end, whichever is smaller.
            if (analysisSheet.Dimension != null)
            {
                int endClearRange = Math.Min(templateFormulaLimitRow, analysisSheet.Dimension.End.Row);
                if (endClearRange >= analysisPopulateStartRow)
                {
                    Logger.LogDebug($"Pre-clearing direct input columns (Customer, SourceFile, Date, FinYear) in Analysis sheet from row {analysisPopulateStartRow} to {endClearRange}.");
                    for (int r = analysisPopulateStartRow; r <= endClearRange; r++)
                    {
                        analysisSheet.Cells[r, CustomerColumnIndex].Value = null;
                        analysisSheet.Cells[r, AnalysisSheetSourceFileNameColumnIndex].Value = null;
                        analysisSheet.Cells[r, AnalysisSheetDateColumnIndex].Value = null;
                        analysisSheet.Cells[r, AnalysisSheetFinancialYearColumnIndex].Value = null;
                    }
                }
            }

            Logger.LogDebug("ExtractUniqueCustomersAsync: Populating Analysis sheet...");
            for (int i = 0; i < uniqueCustomers.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                string customer = uniqueCustomers[i];
                int targetRow = analysisPopulateStartRow + i; // Current row in Analysis sheet to populate.

                // If targetRow is beyond the pre-formatted template area (templateFormulaLimitRow)
                // AND also beyond the current actual end of the sheet,
                // copy the entire template formula row (row 6) to this new targetRow.
                // EPPlus's Copy() method should adjust relative formulas correctly.
                if (targetRow > templateFormulaLimitRow && templateRowExistsAndNotEmpty && targetRow > (analysisSheet.Dimension?.Rows ?? 0))
                {
                    ExcelRange templateRowRange = analysisSheet.Cells[templateFormulaRow, AnalysisSheetFirstClearableColumn, templateFormulaRow, AnalysisSheetLastClearableColumn];
                    ExcelRange targetRowCells = analysisSheet.Cells[targetRow, AnalysisSheetFirstClearableColumn, targetRow, AnalysisSheetLastClearableColumn];
                    templateRowRange.Copy(targetRowCells); // Copy formulas and formatting.
                    Logger.LogTrace($"Copied template row {templateFormulaRow} to new row {targetRow} in Analysis sheet.");
                }
                // If targetRow is within the existing template area (up to templateFormulaLimitRow),
                // formulas in columns B, C, D-K etc. are assumed to be correctly set by the template.
                // We only need to populate the customer-specific data (A, L, M, N).

                // Populate customer name and other direct data fields for the current targetRow.
                analysisSheet.Cells[targetRow, CustomerColumnIndex].Value = customer;
                analysisSheet.Cells[targetRow, AnalysisSheetDateColumnIndex].Value = reportDate.Date; // Store date part only.
                analysisSheet.Cells[targetRow, AnalysisSheetDateColumnIndex].Style.Numberformat.Format = "dd/mm/yyyy"; // Ensure date format.
                analysisSheet.Cells[targetRow, AnalysisSheetFinancialYearColumnIndex].Value = currentFY;
                analysisSheet.Cells[targetRow, AnalysisSheetSourceFileNameColumnIndex].Value = sourceFileNameForAnalysis;

                // Columns B, C, D-K etc. are expected to calculate based on formulas referencing column A and the "DATA" sheet.
            }

            Logger.LogInfo($"Populated {uniqueCustomers.Count} unique customers into '{targetAnalysisSheetName}'. Report date: {reportDate:dd/MM/yyyy}.");
            Logger.LogTrace($"Exiting ExtractUniqueCustomersAsync for sheet '{targetAnalysisSheetName}'.");
        }

        /// <summary>
        /// Triggers calculation of all formulas in the specified worksheet.
        /// EPPlus has limitations with complex cross-sheet formulas or certain functions;
        /// full recalculation by Excel upon opening might still be necessary for some templates.
        /// </summary>
        private void CalculateSheet(ExcelPackage package, string sheetName)
        {
            Logger.LogTrace($"Entering CalculateSheet for sheet '{sheetName}'");
            ExcelWorksheet? worksheet = package.Workbook.Worksheets[sheetName];
            if (worksheet != null)
            {
                try
                {
                    Logger.LogInfo($"Attempting to calculate formulas in workbook (relevant for sheet '{sheetName}').");
                    // Calculate the entire workbook as some formulas might depend on other sheets (like "DATA").
                    package.Workbook.Calculate();
                    Logger.LogInfo($"Workbook calculation triggered. Formulas in '{sheetName}' should reflect updated data.");
                }
                catch (Exception ex) // EPPlus calculation can sometimes throw errors with complex formulas.
                {
                    Logger.LogWarning($"Error during Excel workbook calculation (for sheet '{sheetName}'): {ex.Message}. Manual refresh in Excel might be needed.", ex);
                }
            }
            else
            {
                Logger.LogWarning($"Sheet '{sheetName}' not found in workbook. Cannot trigger calculation.");
            }
            Logger.LogTrace($"Exiting CalculateSheet for sheet '{sheetName}'.");
        }

        /// <summary>
        /// Clears content from rows in the specified sheet that are below the last row containing a customer name.
        /// This is used to clean up unused template rows in the "Analysis" sheet.
        /// </summary>
        private void ClearContentBelowLastCustomer(ExcelPackage package, string sheetName, int customerNameColIdx, int firstColToClear, int lastColToClear)
        {
            Logger.LogTrace($"Entering ClearContentBelowLastCustomer for sheet '{sheetName}'. CustomerCol: {customerNameColIdx}, ClearRange: {firstColToClear}-{lastColToClear}.");
            ExcelWorksheet? worksheet = package.Workbook.Worksheets[sheetName];

            if (worksheet == null || worksheet.Dimension == null)
            {
                Logger.LogWarning($"Sheet '{sheetName}' not found or is empty. Nothing to clear.");
                return;
            }

            const int customerDataStartRow = 6; // Assumed start row for customer data.
            int lastActualDataRow = customerDataStartRow - 1; // Initialize to before the first data row.

            // Find the last row that actually contains a customer name.
            for (int r = worksheet.Dimension.End.Row; r >= customerDataStartRow; r--)
            {
                if (worksheet.Cells[r, customerNameColIdx].Value != null &&
                    !string.IsNullOrWhiteSpace(worksheet.Cells[r, customerNameColIdx].Value.ToString()))
                {
                    lastActualDataRow = r;
                    break; // Found the last customer.
                }
            }
            Logger.LogDebug($"ClearContent: Last row with customer name in '{sheetName}' is {lastActualDataRow}.");

            // Determine the starting row for clearing content.
            int startClearTargetRow = lastActualDataRow + 1;
            // Ensure clearing doesn't start above where customer data is supposed to begin.
            startClearTargetRow = Math.Max(startClearTargetRow, customerDataStartRow);

            if (startClearTargetRow <= worksheet.Dimension.End.Row) // If there are rows to clear.
            {
                Logger.LogInfo($"ClearContent: Clearing content from row {startClearTargetRow} to {worksheet.Dimension.End.Row} (columns {firstColToClear}-{lastColToClear}) in sheet '{sheetName}'.");
                worksheet.Cells[startClearTargetRow, firstColToClear, worksheet.Dimension.End.Row, lastColToClear].Clear(); // Clear cell values and formulas.
            }
            else
            {
                Logger.LogInfo($"No rows to clear below last customer data in '{sheetName}' (Last data at {lastActualDataRow}, sheet ends at {worksheet.Dimension.End.Row}).");
            }
            Logger.LogTrace($"Exiting ClearContentBelowLastCustomer for sheet '{sheetName}'.");
        }

        /// <summary>
        /// Sets the specified pivot table to refresh its data when the Excel file is opened.
        /// </summary>
        private void RefreshPivotTable(ExcelPackage package, string sheetName, string pivotTableName)
        {
            Logger.LogTrace($"Entering RefreshPivotTable for PivotTable '{pivotTableName}' in sheet '{sheetName}'.");
            ExcelWorksheet? worksheet = package.Workbook.Worksheets[sheetName];
            if (worksheet == null)
            {
                Logger.LogWarning($"Sheet '{sheetName}' not found. Cannot set pivot table '{pivotTableName}' to refresh on open.");
                return;
            }

            ExcelPivotTable? pivotTable = worksheet.PivotTables.FirstOrDefault(pt => pt.Name.Equals(pivotTableName, StringComparison.OrdinalIgnoreCase));
            if (pivotTable != null)
            {
                try
                {
                    // Setting RefreshDataOnOpen to true ensures Excel attempts to refresh it when the user opens the file.
                    pivotTable.CacheDefinition.Refresh();;
                    // pivotTable.CacheDefinition.Refresh(); // This would attempt to refresh now using EPPlus, which might have limitations.
                    // RefreshOnLoad = true is generally safer for user-driven refresh in Excel.
                    Logger.LogInfo($"Set pivot table '{pivotTableName}' in sheet '{sheetName}' to RefreshDataOnOpen = true.");
                }
                catch (Exception ex)
                {
                    Logger.LogError($"Error setting RefreshDataOnOpen for pivot table '{pivotTableName}' in '{sheetName}': {ex.Message}", ex);
                }
            }
            else
            {
                Logger.LogWarning($"Pivot table '{pivotTableName}' not found in sheet '{sheetName}'. Available pivot tables: {string.Join(", ", worksheet.PivotTables.Select(pt => pt.Name))}");
            }
            Logger.LogTrace($"Exiting RefreshPivotTable for '{pivotTableName}'.");
        }

        /// <summary>
        /// Copies data from the "Analysis" sheet of the processed report to a central Power BI source Excel file.
        /// This is typically used for Weekly reports.
        /// </summary>
        private async Task CopyAnalysisDataToPowerBIReportAsync(
            ExcelPackage sourcePackage,     // The package of the currently processed report.
            string sourceAnalysisSheetName, // Name of the "Analysis" sheet in sourcePackage.
            string targetPowerBiSheetName,  // Name of the sheet in the Power BI file (from config).
            IProgress<ProgressReport>? progress,
            int reportType,                 // For context, mainly to generate a source filename.
            string originalSourceFilePath,  // Path of the raw report, used to derive a source filename.
            DateTime reportDate,            // Primary date of the report.
            CancellationToken cancellationToken)
        {
            Logger.LogTrace($"Entering CopyAnalysisDataToPowerBIReportAsync (SourceSheet: '{sourceAnalysisSheetName}', TargetPowerBiSheet: '{targetPowerBiSheetName}')");
            string username = Environment.UserName;
            // Path to the central Power BI file. Consider making this configurable in appsettings.json.
            string destinationPowerBiFilePath = GetWeeklyReportPath(username); // Currently hardcoded with DEBUG/RELEASE paths.

            if (string.IsNullOrEmpty(destinationPowerBiFilePath))
            {
                Logger.LogError("Central Power BI report path is invalid or could not be determined. Cannot append data.");
                progress?.Report(new ProgressReport("Error: Central Power BI report path invalid."));
                return;
            }
            if (!File.Exists(destinationPowerBiFilePath))
            {
                Logger.LogError($"Central Power BI report file not found: '{destinationPowerBiFilePath}'. Cannot append data.");
                progress?.Report(new ProgressReport("Error: Central Power BI report file not found."));
                return;
            }

            ExcelWorksheet? sourceWorksheet = sourcePackage.Workbook.Worksheets[sourceAnalysisSheetName];
            if (sourceWorksheet == null || sourceWorksheet.Dimension == null)
            {
                Logger.LogWarning($"Source analysis sheet '{sourceAnalysisSheetName}' not found or empty. Cannot copy to Power BI report.");
                progress?.Report(new ProgressReport("Warning: No analysis data to copy to Power BI."));
                return;
            }

            try
            {
                Logger.LogInfo($"Opening Power BI source file for appending: {destinationPowerBiFilePath}");
                // Open the Power BI file. This needs to be handled carefully to avoid locking issues if multiple users/processes might access it.
                // Consider using a lock file or specific strategy if concurrent access is a concern.
                using var destinationPackage = await Task.Run(() => new ExcelPackage(new FileInfo(destinationPowerBiFilePath)), cancellationToken);
                Logger.LogDebug($"Power BI source file '{destinationPowerBiFilePath}' opened.");

                ExcelWorksheet? destinationWorksheet = destinationPackage.Workbook.Worksheets[targetPowerBiSheetName];
                if (destinationWorksheet == null) // If target sheet doesn't exist, create it and copy headers.
                {
                    Logger.LogInfo($"Target sheet '{targetPowerBiSheetName}' not found in Power BI file. Creating it and copying headers from '{sourceAnalysisSheetName}' (rows 1-5).");
                    destinationWorksheet = destinationPackage.Workbook.Worksheets.Add(targetPowerBiSheetName);
                    CopyHeaders(sourceWorksheet, destinationWorksheet, 1, 5); // Assuming headers are in rows 1-5 of Analysis sheet.
                }

                int nextFreeRowInPowerBiSheet = await Task.Run(() => GetNextFreeRow(destinationWorksheet, CustomerColumnIndex), cancellationToken);
                Logger.LogDebug($"Next free row in Power BI sheet '{targetPowerBiSheetName}' is {nextFreeRowInPowerBiSheet}.");

                // Generate a filename to write into the "Source File Name" column of the Power BI sheet for traceability.
                string filenameForPowerBiColumn = GenerateFinalFileName(reportType, reportDate, DateTime.Now);
                Logger.LogDebug($"Using filename for Power BI 'Source File Name' column: {filenameForPowerBiColumn}");

                await Task.Run(() =>
                {
                    int sourceAnalysisRowCount = sourceWorksheet.Dimension.Rows;
                    // Determine the number of columns to copy from the Analysis sheet's header row (row 5).
                    int sourceAnalysisColCount = 0;
                    const int headerRowForAnalysisColumnCount = 5; // Headers are in row 5 of Analysis sheet.
                    if (sourceWorksheet.Dimension.Rows >= headerRowForAnalysisColumnCount)
                    {
                        for (int c = sourceWorksheet.Dimension.Columns; c >= 1; c--) // Find last non-empty header cell in row 5.
                        {
                            if (sourceWorksheet.Cells[headerRowForAnalysisColumnCount, c].Value != null &&
                                !string.IsNullOrWhiteSpace(sourceWorksheet.Cells[headerRowForAnalysisColumnCount, c].Value.ToString()))
                            {
                                sourceAnalysisColCount = c;
                                break;
                            }
                        }
                    }
                    if (sourceAnalysisColCount == 0) sourceAnalysisColCount = AnalysisSheetLastClearableColumn; // Fallback if headers are all blank.
                    Logger.LogDebug($"Determined {sourceAnalysisColCount} columns to copy from Analysis sheet '{sourceAnalysisSheetName}' to Power BI sheet.");

                    const int startDataRowInAnalysis = 6; // Data starts from row 6 in Analysis sheet.
                    if (sourceAnalysisRowCount < startDataRowInAnalysis)
                    {
                        Logger.LogWarning($"Source analysis sheet '{sourceAnalysisSheetName}' has no data rows from row {startDataRowInAnalysis}.");
                        return; // No data to copy.
                    }

                    int copiedRowCount = 0;
                    for (int sourceRow = startDataRowInAnalysis; sourceRow <= sourceAnalysisRowCount; sourceRow++)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        // Check if the first cell (Customer Name) in the source row has data.
                        var firstCellVal = sourceWorksheet.Cells[sourceRow, CustomerColumnIndex].Value;
                        if (firstCellVal != null && !string.IsNullOrWhiteSpace(firstCellVal.ToString()))
                        {
                            // Copy all determined columns from source row to destination row.
                            for (int col = 1; col <= sourceAnalysisColCount; col++)
                            {
                                destinationWorksheet.Cells[nextFreeRowInPowerBiSheet, col].Value = sourceWorksheet.Cells[sourceRow, col].Value;
                            }
                            // Populate the "Source File Name" column in the Power BI sheet.
                            destinationWorksheet.Cells[nextFreeRowInPowerBiSheet, AnalysisSheetSourceFileNameColumnIndex].Value = filenameForPowerBiColumn;
                            nextFreeRowInPowerBiSheet++; // Move to the next row in Power BI sheet.
                            copiedRowCount++;
                        }

                        // Report progress periodically.
                        if ((sourceRow - startDataRowInAnalysis + 1) % 50 == 0 && sourceAnalysisRowCount > startDataRowInAnalysis)
                        {
                            int percent = (int)((double)(sourceRow - startDataRowInAnalysis + 1) / (sourceAnalysisRowCount - startDataRowInAnalysis + 1) * 20); // Scale for this step (e.g., 0-20%)
                            progress?.Report(new ProgressReport($"Copying to Power BI source... {Math.Min(100, percent)}%", 75 + percent)); // Base progress at 75%
                        }
                    }
                    Logger.LogInfo($"Copied values for {copiedRowCount} rows from '{sourceAnalysisSheetName}' to Power BI sheet '{targetPowerBiSheetName}'.");
                    if (sourceAnalysisRowCount >= startDataRowInAnalysis)
                        progress?.Report(new ProgressReport("Copying to Power BI source complete.", 95)); // Progress after this step.
                }, cancellationToken);

                Logger.LogDebug($"Attempting to save Power BI source file: {destinationPowerBiFilePath}");
                await destinationPackage.SaveAsync(cancellationToken); // Save changes to the Power BI file.
                Logger.LogInfo($"Successfully appended data to sheet '{targetPowerBiSheetName}' in '{destinationPowerBiFilePath}'.");
                progress?.Report(new ProgressReport("Data appended to Power BI source."));
            }
            catch (OperationCanceledException)
            {
                Logger.LogWarning("Operation cancelled during copy to Power BI report source.");
                progress?.Report(new ProgressReport("Cancelled copy to Power BI source."));
                throw; // Re-throw to be handled by caller.
            }
            catch (IOException ioEx) // Catch common IO errors (file locked, etc.)
            {
                Logger.LogError($"IO Error accessing Power BI source file '{destinationPowerBiFilePath}': {ioEx.Message}", ioEx);
                progress?.Report(new ProgressReport($"Error (Power BI): {ioEx.Message}"));
            }
            catch (Exception ex) // Catch other unexpected errors.
            {
                Logger.LogError($"Unexpected error copying data to Power BI source file '{destinationPowerBiFilePath}': {ex.Message}", ex);
                progress?.Report(new ProgressReport($"Error (Power BI): {ex.Message}"));
            }
            Logger.LogTrace($"Exiting CopyAnalysisDataToPowerBIReportAsync.");
        }

        /// <summary>
        /// Copies header rows from a source worksheet to a destination worksheet.
        /// </summary>
        private void CopyHeaders(ExcelWorksheet sourceSheet, ExcelWorksheet destinationSheet, int startHeaderRow = 1, int endHeaderRow = 1)
        {
            Logger.LogTrace($"Copying headers from '{sourceSheet.Name}' (rows {startHeaderRow}-{endHeaderRow}) to '{destinationSheet.Name}'.");
            if (sourceSheet.Dimension != null && sourceSheet.Dimension.Rows >= endHeaderRow)
            {
                // Determine actual number of columns with data in the header rows.
                int actualHeaderColCount = 0;
                for (int r = startHeaderRow; r <= endHeaderRow; r++)
                {
                    for (int c = sourceSheet.Dimension.Columns; c >= 1; c--) // Iterate backwards to find last non-empty cell.
                    {
                        if (sourceSheet.Cells[r, c].Value != null && !string.IsNullOrWhiteSpace(sourceSheet.Cells[r, c].Value.ToString()))
                        {
                            actualHeaderColCount = Math.Max(actualHeaderColCount, c);
                            break; // Found last content cell for this row.
                        }
                    }
                }
                if (actualHeaderColCount == 0 && sourceSheet.Dimension.Columns > 0) actualHeaderColCount = sourceSheet.Dimension.Columns; // Fallback if all header cells are empty.
                else if (actualHeaderColCount == 0) actualHeaderColCount = 1; // Absolute fallback.

                if (actualHeaderColCount > 0)
                {
                    ExcelRange sourceHeaderRange = sourceSheet.Cells[startHeaderRow, 1, endHeaderRow, actualHeaderColCount];
                    ExcelRange destHeaderRange = destinationSheet.Cells[startHeaderRow, 1, endHeaderRow, actualHeaderColCount];
                    sourceHeaderRange.Copy(destHeaderRange); // Copy values, formulas, and styles.
                    Logger.LogDebug($"Copied header rows {startHeaderRow}-{endHeaderRow} (up to column {actualHeaderColCount}) from '{sourceSheet.Name}' to '{destinationSheet.Name}'.");
                }
                else
                {
                    Logger.LogWarning($"No header columns found to copy in '{sourceSheet.Name}'.");
                }
            }
            else
            {
                // Add a minimal default header if source sheet is too small or empty.
                destinationSheet.Cells[1, 1].Value = "Default_Header";
                Logger.LogWarning($"Source sheet '{sourceSheet.Name}' for header copy was too small or empty. Added minimal default header to '{destinationSheet.Name}'.");
            }
        }

        /// <summary>
        /// Finds the next available (empty) row in a worksheet, starting from a specified data row.
        /// It checks a specific column (typically the first data column) to determine if a row is used.
        /// </summary>
        /// <param name="worksheet">The worksheet to check.</param>
        /// <param name="checkColumn">The 1-based index of the column to check for content.</param>
        /// <returns>The 1-based index of the next free row.</returns>
        private int GetNextFreeRow(ExcelWorksheet worksheet, int checkColumn = 1)
        {
            Logger.LogTrace($"Getting next free row in worksheet '{worksheet.Name}', checking column {checkColumn}.");
            if (worksheet.Dimension == null) // If sheet is completely empty.
            {
                Logger.LogDebug($"Worksheet '{worksheet.Name}' is empty. Next free row is 1.");
                return 1; // Start at row 1.
            }

            // Define where data rows typically start (after any headers).
            // This value might need adjustment based on your specific Power BI sheet layout.
            const int firstDataRowAfterHeaders = 2; // Assuming row 1 is header in Power BI sheet.

            int lastUsedRow = worksheet.Dimension.End.Row; // Get the last row that has any cell content.

            // If the sheet has fewer rows than where data starts, the next free row is the start of data.
            if (lastUsedRow < firstDataRowAfterHeaders)
            {
                Logger.LogDebug($"Worksheet '{worksheet.Name}' has only headers or less. Last used row: {lastUsedRow}. Next free row: {firstDataRowAfterHeaders}.");
                return firstDataRowAfterHeaders;
            }

            // Iterate backwards from the last used row to find the first row with content in the checkColumn.
            for (int r = lastUsedRow; r >= 1; r--)
            {
                var cell = worksheet.Cells[r, checkColumn].Value;
                if (cell != null && !string.IsNullOrWhiteSpace(cell.ToString()))
                {
                    // The next free row is one after this. Ensure it's not before where data should start.
                    int nextRow = Math.Max(r + 1, firstDataRowAfterHeaders);
                    Logger.LogDebug($"Last used row in Col {checkColumn} of '{worksheet.Name}' is {r}. Next free row: {nextRow}.");
                    return nextRow;
                }
            }

            // If the checkColumn is entirely empty or no data found below headers, start at the first data row.
            Logger.LogDebug($"Column {checkColumn} in '{worksheet.Name}' is empty or no data found below headers. Next free row: {firstDataRowAfterHeaders}.");
            return firstDataRowAfterHeaders;
        }

        /// <summary>
        /// Gets the path to the weekly Power BI report file.
        /// This path is currently hardcoded with different versions for DEBUG and RELEASE builds.
        /// CONSIDER MOVING THIS PATH TO appsettings.json for better configurability.
        /// </summary>
        /// <param name="username">The current system username, used to construct the path.</param>
        /// <returns>The full path to the weekly Power BI report file.</returns>
        private string GetWeeklyReportPath(string username)
        {
            Logger.LogTrace($"Entering GetWeeklyReportPath for username: {username}");
            // IMPORTANT: This path is hardcoded. For better maintainability and deployment flexibility,
            // this path should ideally be configurable in appsettings.json.
            // Example key: "Paths:PowerBiWeeklyReportFile" (could include {username} placeholder).
            string path;
#if DEBUG
            // Path for DEBUG builds (e.g., a copy or test file).
            path = $@"C:\Users\{username}\Harlow Printing\IT - Documents\PowerBI\Quote Conversion Report\Quotes conversion data_wrangled\weekly report quotes conversion merged - copy.xlsx";
            Logger.LogDebug($"GetWeeklyReportPath (DEBUG mode) resolved to: {path}");
#else
            // Path for RELEASE builds (the production file).
            path = $@"C:\Users\{username}\Harlow Printing\IT - Documents\PowerBI\Quote Conversion Report\Quotes conversion data_wrangled\weekly report quotes conversion merged.xlsx";
            Logger.LogDebug($"GetWeeklyReportPath (RELEASE mode) resolved to: {path}");
#endif
            return path;
        }
        #endregion

        #region File and Folder Naming Helpers (Static or instance based on usage)

        /// <summary>
        /// Generates the final filename for a processed report based on its type, primary date, and a run timestamp (for uniqueness in custom reports).
        /// </summary>
        /// <param name="reportType">The integer index of the report type.</param>
        /// <param name="reportDate">The primary date associated with the report (e.g., end date of the period).</param>
        /// <param name="runTimestamp">The timestamp of when the report generation/processing was initiated. Used primarily for custom reports to ensure unique filenames.</param>
        /// <returns>A string representing the generated filename (e.g., "20230530_Estimate_Success_Rate_Daily.xlsx").</returns>
        private string GenerateFinalFileName(int reportType, DateTime reportDate, DateTime runTimestamp)
        {
            Logger.LogTrace($"Generating final filename for ReportType: {reportType}, ReportDate: {reportDate:d}, RunTimestamp: {runTimestamp:G}");
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
                    // Weekly report filename might be just the date, or a specific format.
                    // Current logic from Form1 appears to be "YYYYMMDD Estimate Success Rate.xlsx"
                    fileName = $"{reportDate:yyyyMMdd} Estimate Success Rate.xlsx";
                    break;
                case MonthlyReportIndex:
                    fileName = $"Estimate Success Rate {reportDate:MMM yy}.xlsx"; // e.g., "Estimate Success Rate May 23.xlsx"
                    break;
                case QuarterlyReportIndex:
                    int quarter = (reportDate.Month - 1) / 3 + 1;
                    DateTime quarterStartDate = new DateTime(reportDate.Year, (quarter - 1) * 3 + 1, 1);
                    DateTime quarterEndDate = quarterStartDate.AddMonths(3).AddDays(-1);
                    // Format: "Estimate Success Rate Mmm to Mmm YYYY.xlsx" or "Mmm YYYY to Mmm YYYY.xlsx" if跨year
                    string qtrFolderNamePart = $"{quarterStartDate:MMM} to {quarterEndDate:MMM}{(quarterStartDate.Year != quarterEndDate.Year ? $" {quarterStartDate.Year}-{quarterEndDate.Year}" : $" {quarterStartDate.Year}")}";
                    fileName = $"Estimate Success Rate {qtrFolderNamePart}.xlsx";
                    break;
                case AnnualReportIndex:
                    // Determine financial year based on configured start month/day
                    int finYearStartMonth = _configuration.GetValue<int>("OperationalParameters:FinancialYearStartMonth", 5);
                    // reportDate here is the *end date* of the annual period.
                    // The financial year is typically named by its starting year.
                    int finStartYear = reportDate.Month < finYearStartMonth ? reportDate.Year - 1 : reportDate.Year;
                    if (reportDate.Month == finYearStartMonth && reportDate.Day < _configuration.GetValue<int>("OperationalParameters:FinancialYearStartDay", 1))
                    {
                        finStartYear = reportDate.Year - 1;
                    }

                    fileName = $"Estimate Success Rate FY {finStartYear}-{finStartYear + 1}.xlsx";
                    break;
                case CustomReportIndex:
                    // Custom reports include a timestamp for uniqueness if run multiple times for same dates.
                    fileName = $"{reportDate:yyyyMMdd}_{runTimestamp:HHmmss}_Estimate_Success_Rate_Custom.xlsx";
                    break;
                default:
                    Logger.LogWarning($"Unknown report type '{reportType}' for filename generation. Using generic format.");
                    fileName = $"{reportDate:yyyyMMdd}_Estimate_Success_Rate_UnknownType_{runTimestamp:HHmmss}.xlsx";
                    break;
            }
            Logger.LogDebug($"Generated final filename: {fileName}");
            return fileName;
        }

        /// <summary>
        /// Asynchronously renames or moves a file, with retry logic for transient IO errors (like file locks).
        /// </summary>
        /// <param name="sourcePath">The full path to the source file.</param>
        /// <param name="destinationPath">The full path to the destination file.</param>
        /// <param name="progress">Optional progress reporter.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <param name="maxRetriesConfigKey">Configuration key for maximum number of retries.</param>
        /// <param name="delayMsConfigKey">Configuration key for initial delay in milliseconds between retries.</param>
        /// <exception cref="IOException">Thrown if the file move fails after all retries.</exception>
        /// <exception cref="OperationCanceledException">Thrown if the operation is cancelled.</exception>
        private async Task RenameFileWithRetryAsync(
            string sourcePath,
            string destinationPath,
            IProgress<ProgressReport>? progress,
            CancellationToken cancellationToken,
            string maxRetriesConfigKey = "OperationalParameters:GeneralFileOperationMaxRetries",
            string delayMsConfigKey = "OperationalParameters:GeneralFileOperationDelayMs")
        {
            Logger.LogDebug($"Attempting to move/rename file from '{sourcePath}' to '{destinationPath}' with retries.");
            int maxRetries = _configuration.GetValue<int>(maxRetriesConfigKey, 5); // Default to 5 retries.
            int initialDelayMs = _configuration.GetValue<int>(delayMsConfigKey, 500); // Default to 500ms initial delay.
            int currentDelayMs = initialDelayMs;

            for (int i = 0; i < maxRetries; i++)
            {
                try
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    // Use Task.Run to ensure File.Move (synchronous) doesn't block the async method's thread for long periods if it stalls.
                    await Task.Run(() =>
                    {
                        if (File.Exists(destinationPath)) // Delete destination if it exists before moving.
                        {
                            Logger.LogWarning($"Destination file '{destinationPath}' already exists. Deleting before move.");
                            File.Delete(destinationPath);
                        }
                        File.Move(sourcePath, destinationPath);
                    }, cancellationToken);
                    Logger.LogInfo($"Successfully moved/renamed '{sourcePath}' to '{destinationPath}'.");
                    return; // Success.
                }
                catch (IOException ex) when (i < maxRetries - 1) // Retry only if not the last attempt.
                {
                    Logger.LogWarning($"Attempt {i + 1}/{maxRetries} failed to move/rename '{sourcePath}' due to IO error: {ex.Message}. Retrying in {currentDelayMs}ms...");
                    progress?.Report(new ProgressReport($"Waiting for file access (Attempt {i + 1})..."));
                    await Task.Delay(currentDelayMs, cancellationToken);
                    currentDelayMs = Math.Min(currentDelayMs * 2, 5000); // Exponential backoff, capped at 5 seconds.
                }
                catch (OperationCanceledException)
                {
                    Logger.LogWarning($"Move/Rename operation cancelled while trying to process '{sourcePath}'.");
                    throw; // Re-throw to be handled by the caller.
                }
                // Other exceptions (UnauthorizedAccessException, etc.) will fall through on the last attempt or if not IOException.
            }
            // If loop completes without returning, it means all retries failed.
            Logger.LogError($"Failed to move/rename file '{sourcePath}' to '{destinationPath}' after {maxRetries} attempts. The file might still be locked or another persistent IO error occurred.");
            throw new IOException($"Failed to move/rename file '{sourcePath}' to '{destinationPath}' after {maxRetries} attempts. Check logs for specific IO errors.");
        }
        #endregion
    }
}