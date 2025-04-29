// C# 10+ Features (using file-scoped namespace, global using directives if applicable elsewhere)
using conversionTest; // Assuming Logger is in this namespace or globally available
using OfficeOpenXml; // EPPlus library for Excel manipulation
using OfficeOpenXml.Table.PivotTable;
using System;
using System.Collections.Generic;
using System.Diagnostics; // Added for Stopwatch
using System.Globalization; // Added for CultureInfo if needed for month names
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace QuoteConversionReportAutomation // File-scoped namespace
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
    /// Now uses a specific report date for filename generation.
    /// This is a non-static version requiring instantiation.
    /// </summary>
    public class ExcelCopyData // Removed static keyword
    {
        #region Constants

        // --- Report Type Indices (Must match Form1.cs) ---
        private const int DailyReportIndex = 0;
        private const int WeeklyReportIndex = 1;
        private const int MonthlyReportIndex = 2;
        private const int QuarterlyReportIndex = 3;
        private const int AnnualReportIndex = 4;
        private const int CustomReportIndex = 5; // <<< ADDED Custom Index

        // Constants for column indices (1-based for EPPlus access).
        private const int CustomerColumnIndex = 1;       // Column A
        private const int DateColumnIndex = 13;          // Column M
        private const int FinancialYearColumnIndex = 14; // Column N
        private const int SourceFileNameColumnIndex = 12; // Column L

        // Define the range of columns containing formulas to be cleared below the last customer
        private const int FirstFormulaColumnIndex = 2;  // Column B
        private const int LastFormulaColumnIndex = 14;  // Column N (or adjust as needed)


        // --- Sheet Names ---
        private const string AnalysisSheetName = "Analysis"; // Sheet containing formulas/unique customers
        private const string MonthlyOrderPivotSheetName = "OrderPivot";
        private const string MonthlyEstimatePivotSheetName = "Estimate Success PivotTable";

        // --- Pivot Table Names ---
        private const string MonthlyOrderPivotName = "PivotTable1";
        private const string MonthlyEstimatePivotName = "PivotTable3";

        #endregion Constants

        #region Constructor
        /// <summary>
        /// Initializes a new instance of the ExcelCopyData class.
        /// </summary>
        public ExcelCopyData()
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial; // Set license context
            Logger.LogTrace("ExcelCopyData instance created."); // Trace: Object creation
        }
        #endregion

        #region Public Instance Methods

        /// <summary>
        /// Asynchronously processes an Excel report: copies data, extracts unique customers, calculates, cleans,
        /// updates weekly summary (if applicable), and handles pivot tables. Saves the final file into a report-type specific folder.
        /// Uses the provided report date for filename generation and folder structure.
        /// </summary>
        public async Task<string?> ProcessExcelReportAsync( // Removed static
            string selectedFinYear,
            int reportType,
            string sourceFilePath,
            string sourceSheetName,
            string baseFileSaveLocation,
            string templateFilePath,
            string destinationDataSheetName,
            int startRow = 1,
            int startCol = 1,
            IProgress<ProgressReport>? progress = null,
            DateTime reportDate = default, // Note: For Custom reports, this will be the END date selected by user
            CancellationToken cancellationToken = default)
        {
            Logger.LogTrace($"Entering ProcessExcelReportAsync. ReportType: {reportType}, Source: {sourceFilePath}, Template: {templateFilePath}, ReportDate: {reportDate:yyyy-MM-dd}"); // Trace: Method entry
            var stopwatch = Stopwatch.StartNew(); // Time the operation

            ArgumentException.ThrowIfNullOrEmpty(sourceFilePath);
            ArgumentException.ThrowIfNullOrEmpty(sourceSheetName);
            ArgumentException.ThrowIfNullOrEmpty(baseFileSaveLocation);
            ArgumentException.ThrowIfNullOrEmpty(templateFilePath);
            ArgumentException.ThrowIfNullOrEmpty(destinationDataSheetName);
            // Financial year validation might not apply to Custom, adjust if needed
            if (reportType == WeeklyReportIndex || reportType == DailyReportIndex)
            {
                ArgumentException.ThrowIfNullOrEmpty(selectedFinYear);
            }

            if (reportDate == default && reportType != CustomReportIndex) // Custom might legitimately have default if not set
            {
                reportDate = DateTime.Today;
                Logger.LogWarning($"ProcessExcelReportAsync called without a specific reportDate for non-custom report. Defaulting to Today for filename generation: {reportDate:yyyy-MM-dd}");
            }
            // For Custom, reportDate will be the user's selected end date (or default if unchanged)

            string? finalFilePath = null;
            string? tempFilePath = null;
            string? fullOutputFolderPath = null;

            try
            {
                progress?.Report(new ProgressReport("Starting Excel processing...", 0));
                cancellationToken.ThrowIfCancellationRequested();

                // 1. Determine and Create Report-Specific Folder using FolderCreation utility
                Logger.LogTrace("ProcessExcelReportAsync: Determining output folder using FolderCreation..."); // Trace: Internal step
                // Use reportDate for folder structure consistency with filename (for Custom, this is the end date)
                // Use DateTime.Now for the timestamped folder name for Custom reports
                DateTime folderTimestampDate = (reportType == CustomReportIndex) ? DateTime.Now : reportDate;
                fullOutputFolderPath = FolderCreation.CreateReportSpecificFolder(reportType, baseFileSaveLocation, folderTimestampDate); // Use static method
                if (fullOutputFolderPath == null)
                {
                    throw new InvalidOperationException("Failed to create or determine the report output folder using FolderCreation utility.");
                }
                progress?.Report(new ProgressReport("Output folder prepared."));
                cancellationToken.ThrowIfCancellationRequested();

                // 2. Define temporary file path
                tempFilePath = Path.Combine(fullOutputFolderPath, $"temp_{Guid.NewGuid()}.xlsx");
                Logger.LogDebug($"ProcessExcelReportAsync: Using temporary file: {tempFilePath}"); // Debug: Important path info

                // 3. Copy Template to Temp Location
                Logger.LogTrace($"ProcessExcelReportAsync: Copying template '{templateFilePath}' to '{tempFilePath}'..."); // Trace: Internal step
                await Task.Run(() => File.Copy(templateFilePath, tempFilePath, true), cancellationToken);
                progress?.Report(new ProgressReport("Template copied."));
                cancellationToken.ThrowIfCancellationRequested();

                // 4. Open Packages and Copy Data
                progress?.Report(new ProgressReport("Opening Excel files..."));
                Logger.LogTrace($"ProcessExcelReportAsync: Opening source '{sourceFilePath}' and destination '{tempFilePath}' packages..."); // Trace: Internal step
                using (var sourcePackage = new ExcelPackage(new FileInfo(sourceFilePath)))
                using (var destinationPackage = new ExcelPackage(new FileInfo(tempFilePath)))
                {
                    Logger.LogDebug("ProcessExcelReportAsync: Packages opened."); // Debug: Milestone
                    ExcelWorksheet? sourceWorksheet = sourcePackage.Workbook.Worksheets[sourceSheetName] ?? throw new FileNotFoundException($"Source sheet '{sourceSheetName}' not found in '{sourceFilePath}'.");
                    ExcelWorksheet destinationWorksheet = GetOrCreateDestinationWorksheet(destinationPackage, destinationDataSheetName, sourceWorksheet); // Use instance method

                    int sourceRowCount = sourceWorksheet.Dimension?.Rows ?? 0;
                    int sourceColCount = sourceWorksheet.Dimension?.Columns ?? 0;
                    Logger.LogDebug($"ProcessExcelReportAsync: Source dimensions: {sourceRowCount} rows, {sourceColCount} cols. Start copy from R{startRow}C{startCol}."); // Debug: Useful info

                    progress?.Report(new ProgressReport("Copying data from source to template...", 10));
                    if (sourceRowCount >= startRow && sourceColCount >= startCol)
                    {
                        Logger.LogTrace("ProcessExcelReportAsync: Starting data copy task..."); // Trace: Internal step
                        await Task.Run(() =>
                        {
                            ExcelRange sourceRange = sourceWorksheet.Cells[startRow, startCol, sourceRowCount, sourceColCount];
                            ExcelRange destStartCell = destinationWorksheet.Cells[2, 1]; // Data starts at row 2
                            sourceRange.Copy(destStartCell);
                            Logger.LogInfo($"Copied range {sourceRange.Address} from {sourceSheetName} to {destinationDataSheetName}!{destStartCell.Address}."); // Info: Significant action complete
                        }, cancellationToken);
                        Logger.LogTrace("ProcessExcelReportAsync: Data copy task finished."); // Trace: Internal step
                    }
                    else
                    {
                        Logger.LogWarning($"Source sheet '{sourceSheetName}' has no data to copy (Rows: {sourceRowCount}, StartRow: {startRow}) or start column is out of bounds.");
                    }
                    progress?.Report(new ProgressReport("Data copy complete.", 30));
                    cancellationToken.ThrowIfCancellationRequested();

                    // 5. Post-Copy Processing
                    Logger.LogDebug("ProcessExcelReportAsync: Starting post-copy operations..."); // Debug: Milestone
                    await ProcessPostCopyOperationsAsync(destinationPackage, destinationDataSheetName, AnalysisSheetName, reportType, progress, selectedFinYear, sourceFilePath, reportDate, cancellationToken); // Use instance method
                    Logger.LogDebug("ProcessExcelReportAsync: Post-copy operations finished."); // Debug: Milestone

                    // 6. Save the destination package
                    progress?.Report(new ProgressReport("Saving processed file...", 85));
                    Logger.LogDebug("ProcessExcelReportAsync: Saving destination package..."); // Debug: Milestone
                    try
                    {
                        await destinationPackage.SaveAsync(cancellationToken);
                        Logger.LogDebug($"ProcessExcelReportAsync: Saved changes to temporary file: {tempFilePath}"); // Debug: Useful info
                    }
                    catch (Exception saveEx)
                    {
                        Logger.LogError($"Error saving temporary Excel package '{tempFilePath}': {saveEx}"); // Error: Operation failed
                        throw;
                    }
                    Logger.LogDebug("ProcessExcelReportAsync: Destination package saved."); // Debug: Milestone
                } // Packages disposed here
                Logger.LogDebug("ProcessExcelReportAsync: Excel packages disposed."); // Debug: Milestone

                await Task.Delay(500, cancellationToken);
                Logger.LogTrace("ProcessExcelReportAsync: Brief delay completed after disposing destination package."); // Trace: Minor detail

                // 7. Generate Final File Name
                progress?.Report(new ProgressReport("Generating final filename...", 90));
                Logger.LogTrace("ProcessExcelReportAsync: Generating final filename..."); // Trace: Internal step
                // Pass the *reportDate* (end date) for filename consistency, but use Now for custom timestamp part
                string generatedFileName = await Task.Run(() => GenerateFinalFileName(reportType, reportDate, DateTime.Now), cancellationToken); // Use instance method
                finalFilePath = Path.Combine(fullOutputFolderPath, generatedFileName);
                Logger.LogDebug($"ProcessExcelReportAsync: Generated final filename: {generatedFileName}"); // Debug: Useful info
                Logger.LogDebug($"ProcessExcelReportAsync: Full final file path: {finalFilePath}"); // Debug: Useful info

                Logger.LogInfo($"Attempting to rename file."); // Info: Significant action starting
                Logger.LogDebug($"Source (Temp): '{tempFilePath}'"); // Debug: Detail for rename
                Logger.LogDebug($"Destination (Final): '{finalFilePath}'"); // Debug: Detail for rename

                // 8. Rename Temp File to Final File
                Logger.LogTrace($"ProcessExcelReportAsync: Attempting rename from '{tempFilePath}' to '{finalFilePath}'..."); // Trace: Internal step
                await RenameFileWithRetryAsync(tempFilePath, finalFilePath, progress, cancellationToken); // Use instance method
                Logger.LogTrace($"ProcessExcelReportAsync: Rename successful."); // Trace: Internal step
                tempFilePath = null; // Prevent deletion

                progress?.Report(new ProgressReport("Excel processing complete.", 100));
                Logger.LogInfo($"Excel processing finished. Final file: {finalFilePath}"); // Info: Major operation complete

                stopwatch.Stop();
                Logger.LogInfo($"ProcessExcelReportAsync completed successfully. Duration: {stopwatch.ElapsedMilliseconds}ms."); // Info: Overall success and timing
                Logger.LogDebug($"Exiting ProcessExcelReportAsync. Result: {finalFilePath}"); // Debug: Final result detail
                return finalFilePath;
            }
            catch (OperationCanceledException)
            {
                stopwatch.Stop();
                Logger.LogWarning($"Excel processing was cancelled. Duration: {stopwatch.ElapsedMilliseconds}ms.");
                progress?.Report(new ProgressReport("Operation cancelled."));
                Logger.LogTrace($"Exiting ProcessExcelReportAsync due to cancellation."); // Trace: Exit path
                return null;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                Logger.LogError($"Error during Excel processing: {ex}. Duration: {stopwatch.ElapsedMilliseconds}ms."); // Error: Operation failed
                progress?.Report(new ProgressReport($"Error: {ex.Message}"));
                Logger.LogTrace($"Exiting ProcessExcelReportAsync due to error."); // Trace: Exit path
                return null;
            }
            finally
            {
                // Cleanup temp file if rename failed or error occurred
                if (tempFilePath != null && File.Exists(tempFilePath))
                {
                    try
                    {
                        Logger.LogDebug($"ProcessExcelReportAsync: Cleaning up temporary file '{tempFilePath}'..."); // Debug: Cleanup step
                        File.Delete(tempFilePath);
                        Logger.LogInfo($"Deleted temporary file due to incomplete process: {tempFilePath}"); // Info: Cleanup action result
                    }
                    catch (Exception cleanupEx)
                    {
                        Logger.LogWarning($"Failed to delete temporary file '{tempFilePath}': {cleanupEx.Message}"); // Warning: Cleanup failed
                    }
                }
            }
        }

        /// <summary>
        /// Calculates and returns the current financial year string based on Harlow's fiscal calendar (starting May).
        /// </summary>
        public string GetCurrentFinancialYear(bool useUnderscoreFormat = false)
        {
            Logger.LogTrace($"Entering GetCurrentFinancialYear(useUnderscoreFormat: {useUnderscoreFormat})"); // Trace: Method entry
            DateTime today = DateTime.Today;
            int year = today.Year;
            int startYear = today.Month >= 5 ? year : year - 1;
            int endYear = startYear + 1;
            string result = useUnderscoreFormat ? $"{startYear}_{endYear.ToString()[2..]}" : $"FY {startYear.ToString()[2..]}/{endYear.ToString()[2..]}";
            Logger.LogTrace($"Exiting GetCurrentFinancialYear. Result: {result}"); // Trace: Method exit
            return result;
        }

        /// <summary>
        /// Calculates the previous financial year string based on the current one.
        /// </summary>
        public string? GetPreviousFinancialYear(string currentFinancialYearUnderscore)
        {
            Logger.LogTrace($"Entering GetPreviousFinancialYear(currentFinancialYearUnderscore: {currentFinancialYearUnderscore})"); // Trace: Method entry
            if (string.IsNullOrEmpty(currentFinancialYearUnderscore))
            {
                Logger.LogTrace("Exiting GetPreviousFinancialYear. Input was null/empty."); // Trace: Exit path
                return null;
            }
            string[] parts = currentFinancialYearUnderscore.Split('_');
            string? result = null;
            if (parts.Length == 2 && int.TryParse(parts[0], out int startYear))
            {
                int prevStartYear = startYear - 1;
                result = $"{prevStartYear}_{startYear.ToString()[2..]}";
            }
            else
            {
                Logger.LogWarning($"Invalid financial year format for calculating previous: {currentFinancialYearUnderscore}");
            }
            Logger.LogTrace($"Exiting GetPreviousFinancialYear. Result: {result ?? "null"}"); // Trace: Method exit
            return result;
        }

        /// <summary>
        /// Validates if the selected date range falls within the specified financial year.
        /// </summary>
        public bool IsFinancialYearValid(string selectedFinYearUnderscore, DateTime fromDate, DateTime toDate)
        {
            Logger.LogTrace($"Entering IsFinancialYearValid(selectedFinYearUnderscore: {selectedFinYearUnderscore}, fromDate: {fromDate:d}, toDate: {toDate:d})"); // Trace: Method entry
            if (string.IsNullOrEmpty(selectedFinYearUnderscore))
            {
                Logger.LogTrace("Exiting IsFinancialYearValid. Selected FY was null/empty. Result: false"); // Trace: Exit path
                return false;
            }
            string[] parts = selectedFinYearUnderscore.Split('_');
            bool isValid = false;
            if (parts.Length == 2 && int.TryParse(parts[0], out int startYear))
            {
                int endYear = startYear + 1;
                DateTime fyStartDate = new(startYear, 5, 1);
                DateTime fyEndDate = new(endYear, 4, 30);
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
            Logger.LogTrace($"Exiting IsFinancialYearValid. Result: {isValid}"); // Trace: Method exit
            return isValid;
        }

        /// <summary>
        /// Gets the expected final file path without creating directories or files.
        /// Uses the FolderCreation utility.
        /// </summary>
        public string? GetExpectedFinalFilePath(int reportType, string baseFileSaveLocation, DateTime reportDate)
        {
            Logger.LogTrace($"Entering GetExpectedFinalFilePath(reportType: {reportType}, baseFileSaveLocation: {baseFileSaveLocation}, reportDate: {reportDate:d})"); // Trace: Method entry
            string? result = null;
            try
            {
                if (reportDate == default && reportType != CustomReportIndex)
                {
                    reportDate = DateTime.Today;
                    Logger.LogWarning($"GetExpectedFinalFilePath called without a specific reportDate for non-custom report. Defaulting to Today for filename generation: {reportDate:yyyy-MM-dd}");
                }

                DateTime folderTimestampDate = (reportType == CustomReportIndex) ? DateTime.Now : reportDate;
                string? folderPath = FolderCreation.GetReportSpecificFolderPath(reportType, baseFileSaveLocation, folderTimestampDate); // Use static method
                if (folderPath != null)
                {
                    string fileName = GenerateFinalFileName(reportType, reportDate, DateTime.Now); // Use instance method
                    result = Path.Combine(folderPath, fileName);
                }
                else
                {
                    Logger.LogError("GetExpectedFinalFilePath: Failed to determine folder path using FolderCreation utility."); // Error: Cannot proceed
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error getting expected final file path: {ex.Message}"); // Error: Unexpected failure
            }
            Logger.LogTrace($"Exiting GetExpectedFinalFilePath. Result: {result ?? "null"}"); // Trace: Method exit
            return result;
        }

        /// <summary>
        /// Calculates the week number of a given date within its month.
        /// </summary>
        public int GetWeekOfMonth(DateTime date)
        {
            Logger.LogTrace($"Entering GetWeekOfMonth(date: {date:d})"); // Trace: Method entry
            DateTime firstOfMonth = new(date.Year, date.Month, 1);
            int firstDayOfWeekIso = firstOfMonth.DayOfWeek == 0 ? 7 : (int)firstOfMonth.DayOfWeek;
            int weekOfMonth = (date.Day + firstDayOfWeekIso - 1 - 1) / 7 + 1;
            Logger.LogTrace($"Exiting GetWeekOfMonth. Result: {weekOfMonth}"); // Trace: Method exit
            return weekOfMonth;
        }


        #endregion Public Instance Methods

        #region Internal Processing Steps (Non-Static)

        /// <summary>
        /// Performs post-copy operations: extracts unique customers, calculates analysis, cleans rows/content,
        /// refreshes pivots, and potentially copies to a weekly summary report.
        /// </summary>
        private async Task ProcessPostCopyOperationsAsync( // Removed static
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
            Logger.LogTrace($"Entering ProcessPostCopyOperationsAsync(sourceSheet: {sourceDataSheetName}, targetSheet: {targetAnalysisSheetName}, reportType: {reportType})"); // Trace: Method entry
            var stopwatch = Stopwatch.StartNew();

            progress?.Report(new ProgressReport("Extracting unique customers...", 40));
            Logger.LogTrace("ProcessPostCopyOperationsAsync: Calling ExtractUniqueCustomersAsync..."); // Trace: Internal step
            await ExtractUniqueCustomersAsync(package, sourceDataSheetName, targetAnalysisSheetName, progress, originalSourceFilePath, reportDate, cancellationToken); // Call instance method
            cancellationToken.ThrowIfCancellationRequested();

            progress?.Report(new ProgressReport("Calculating analysis sheet...", 50));
            Logger.LogTrace("ProcessPostCopyOperationsAsync: Calling CalculateSheet..."); // Trace: Internal step
            await Task.Run(() => CalculateSheet(package, targetAnalysisSheetName), cancellationToken); // Call instance method
            // Logger.LogInfo($"Sheet '{targetAnalysisSheetName}' calculations performed."); // Can be Debug or Trace
            Logger.LogTrace($"Sheet '{targetAnalysisSheetName}' calculations performed.");
            cancellationToken.ThrowIfCancellationRequested();

            progress?.Report(new ProgressReport("Cleaning analysis sheet...", 60));
            Logger.LogTrace("ProcessPostCopyOperationsAsync: Calling ClearContentBelowLastCustomer..."); // Trace: Internal step
            await Task.Run(() => ClearContentBelowLastCustomer(package, targetAnalysisSheetName, CustomerColumnIndex, FirstFormulaColumnIndex, LastFormulaColumnIndex), cancellationToken); // Call instance method
            // Logger.LogInfo($"Cleaned content below last customer in sheet '{targetAnalysisSheetName}'."); // Can be Debug or Trace
            Logger.LogTrace($"Cleaned content below last customer in sheet '{targetAnalysisSheetName}'.");
            cancellationToken.ThrowIfCancellationRequested();

            // Refresh Pivot Tables (only if NOT custom, as template might differ)
            if (reportType != CustomReportIndex && reportType is MonthlyReportIndex or QuarterlyReportIndex or AnnualReportIndex)
            {
                progress?.Report(new ProgressReport("Setting pivot tables to refresh on load...", 70));
                Logger.LogTrace("ProcessPostCopyOperationsAsync: Calling RefreshPivotTable (Order)..."); // Trace: Internal step
                await Task.Run(() => RefreshPivotTable(package, MonthlyOrderPivotSheetName, MonthlyOrderPivotName), cancellationToken); // Call instance method
                Logger.LogTrace("ProcessPostCopyOperationsAsync: Calling RefreshPivotTable (Estimate)..."); // Trace: Internal step
                await Task.Run(() => RefreshPivotTable(package, MonthlyEstimatePivotSheetName, MonthlyEstimatePivotName), cancellationToken); // Call instance method
                Logger.LogInfo("Pivot tables set to refresh on load."); // Info: Significant action
                cancellationToken.ThrowIfCancellationRequested();
            }
            else if (reportType == CustomReportIndex)
            {
                Logger.LogInfo("Skipping Pivot Table refresh setting for Custom report type."); // Info: Explains skipped step
            }


            // Append to Central Weekly Report (only if processing a Weekly report type)
            if (reportType == WeeklyReportIndex)
            {
                progress?.Report(new ProgressReport("Appending data to central weekly report...", 75));
                Logger.LogTrace("ProcessPostCopyOperationsAsync: Calling CopyAnalysisDataToWeeklyReportAsync..."); // Trace: Internal step
                await CopyAnalysisDataToWeeklyReportAsync(package, targetAnalysisSheetName, progress, selectedFinYear, reportType, originalSourceFilePath, reportDate, cancellationToken); // Call instance method
                Logger.LogInfo("Data appended to central weekly report."); // Info: Significant action
                cancellationToken.ThrowIfCancellationRequested();
            }
            stopwatch.Stop();
            Logger.LogDebug($"Exiting ProcessPostCopyOperationsAsync. Duration: {stopwatch.ElapsedMilliseconds}ms"); // Debug: Timing info
        }

        /// <summary>
        /// Gets or creates the destination worksheet, copying headers from the source if creating.
        /// Also clears existing data below row 1 if the sheet already exists.
        /// </summary>
        private ExcelWorksheet GetOrCreateDestinationWorksheet(ExcelPackage package, string sheetName, ExcelWorksheet sourceWorksheet) // Removed static
        {
            Logger.LogTrace($"Entering GetOrCreateDestinationWorksheet(sheetName: {sheetName}, sourceSheet: {sourceWorksheet.Name})"); // Trace: Method entry
            ExcelWorksheet? worksheet = package.Workbook.Worksheets[sheetName];
            if (worksheet == null)
            {
                worksheet = package.Workbook.Worksheets.Add(sheetName);
                if (sourceWorksheet.Dimension != null && sourceWorksheet.Dimension.Rows > 0)
                {
                    int headerColCount = sourceWorksheet.Dimension.Columns;
                    ExcelRange sourceHeaderRow = sourceWorksheet.Cells[1, 1, 1, headerColCount];
                    ExcelRange destHeader = worksheet.Cells[1, 1, 1, headerColCount];
                    sourceHeaderRow.Copy(destHeader);
                    Logger.LogInfo($"Created sheet '{sheetName}' and copied headers from '{sourceWorksheet.Name}'."); // Info: Sheet created
                }
                else
                {
                    worksheet.Cells[1, 1].Value = "DefaultHeader";
                    Logger.LogWarning($"Created sheet '{sheetName}', source sheet '{sourceWorksheet.Name}' was empty, added default header.");
                }
            }
            else
            {
                if (worksheet.Dimension != null && worksheet.Dimension.Rows > 1)
                {
                    worksheet.DeleteRow(2, worksheet.Dimension.Rows - 1);
                    Logger.LogInfo($"Cleared existing data (rows 2 onwards) from sheet '{sheetName}'."); // Info: Significant data change
                }
                else
                {
                    Logger.LogDebug($"Sheet '{sheetName}' already existed but had no data below header row."); // Debug: State info
                }
            }
            Logger.LogTrace($"Exiting GetOrCreateDestinationWorksheet. Returning sheet: {worksheet.Name}"); // Trace: Method exit
            return worksheet;
        }

        /// <summary>
        /// Extracts unique customers from the source data sheet and populates the target analysis sheet asynchronously.
        /// Also populates Date (using reportDate), Financial Year, and Source Filename.
        /// </summary>
        private async Task ExtractUniqueCustomersAsync( // Removed static
            ExcelPackage package,
            string sourceDataSheetName,
            string targetAnalysisSheetName,
            IProgress<ProgressReport>? progress,
            string originalSourceFilePath,
            DateTime reportDate, // This is the END date for the report range
            CancellationToken cancellationToken)
        {
            Logger.LogTrace($"Entering ExtractUniqueCustomersAsync(source: {sourceDataSheetName}, target: {targetAnalysisSheetName})"); // Trace: Method entry
            ExcelWorksheet? dataSheet = package.Workbook.Worksheets[sourceDataSheetName];
            if (dataSheet == null)
            {
                Logger.LogError($"Source data sheet '{sourceDataSheetName}' not found for customer extraction."); // Error: Cannot proceed
                Logger.LogTrace($"Exiting ExtractUniqueCustomersAsync early - data sheet not found."); // Trace: Exit path
                return;
            }

            ExcelWorksheet analysisSheet = package.Workbook.Worksheets[targetAnalysisSheetName]
                                             ?? package.Workbook.Worksheets.Add(targetAnalysisSheetName);

            if (analysisSheet.Dimension == null || analysisSheet.Dimension.Rows < 5)
            {
                Logger.LogWarning($"Analysis sheet '{targetAnalysisSheetName}' might be missing expected headers/structure. Ensure template is correct.");
            }

            int dataRowCount = dataSheet.Dimension?.Rows ?? 0;
            int startDataRowInDataSheet = 2;
            if (dataRowCount < startDataRowInDataSheet)
            {
                Logger.LogWarning($"Source data sheet '{sourceDataSheetName}' has insufficient rows ({dataRowCount}) for customer extraction starting at row {startDataRowInDataSheet}.");
                Logger.LogTrace($"Exiting ExtractUniqueCustomersAsync early - insufficient rows in data sheet."); // Trace: Exit path
                return;
            }

            string sourceFileName = Path.GetFileName(originalSourceFilePath);
            Logger.LogDebug($"Filename determined for Analysis sheet population: {sourceFileName}"); // Debug: Useful info

            Logger.LogTrace("ExtractUniqueCustomersAsync: Starting HashSet population task..."); // Trace: Internal step
            var uniqueCustomers = await Task.Run(() =>
            {
                var customers = new HashSet<string>();
                for (int row = startDataRowInDataSheet; row <= dataRowCount; row++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    string? customerName = dataSheet.Cells[row, CustomerColumnIndex].Value?.ToString()?.Trim();
                    if (!string.IsNullOrWhiteSpace(customerName))
                    {
                        customers.Add(customerName);
                    }
                    if (row % 100 == 0)
                    {
                        int percent = (int)((double)(row - startDataRowInDataSheet + 1) / (dataRowCount - startDataRowInDataSheet + 1) * 100);
                        progress?.Report(new ProgressReport($"Extracting customers... {percent}%", percent));
                    }
                }
                progress?.Report(new ProgressReport($"Extracting customers... 100%", 100));
                Logger.LogTrace($"ExtractUniqueCustomersAsync: HashSet population complete. Count: {customers.Count}"); // Trace: Internal result
                return customers;
            }, cancellationToken);

            Logger.LogTrace("ExtractUniqueCustomersAsync: Starting analysis sheet population task..."); // Trace: Internal step
            await Task.Run(() =>
            {
                int analysisPopulateStartRow = 6;
                // Use the report's END date for the Date column
                string reportDateString = reportDate.ToString("dd/MM/yyyy");
                string currentFY = GetCurrentFinancialYear(false); // Use instance method

                foreach (string customer in uniqueCustomers.OrderBy(c => c))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    analysisSheet.Cells[analysisPopulateStartRow, CustomerColumnIndex].Value = customer;
                    analysisSheet.Cells[analysisPopulateStartRow, DateColumnIndex].Value = reportDateString;
                    analysisSheet.Cells[analysisPopulateStartRow, FinancialYearColumnIndex].Value = currentFY;
                    analysisSheet.Cells[analysisPopulateStartRow, SourceFileNameColumnIndex].Value = sourceFileName;
                    analysisPopulateStartRow++;
                }
                Logger.LogInfo($"Populated '{targetAnalysisSheetName}' with {uniqueCustomers.Count} unique customers starting at row 6, using report date {reportDateString}."); // Info: Significant action result
            }, cancellationToken);
            Logger.LogTrace($"Exiting ExtractUniqueCustomersAsync."); // Trace: Method exit
        }

        /// <summary>
        /// Triggers calculation for a specific worksheet.
        /// </summary>
        private void CalculateSheet(ExcelPackage package, string sheetName) // Removed static
        {
            Logger.LogTrace($"Entering CalculateSheet(sheetName: {sheetName})"); // Trace: Method entry
            ExcelWorksheet? sheet = package.Workbook.Worksheets[sheetName];
            if (sheet != null)
            {
                try
                {
                    sheet.Calculate();
                    Logger.LogTrace($"Triggered calculation for sheet '{sheetName}'."); // Trace: Action performed
                }
                catch (Exception ex)
                {
                    Logger.LogError($"Error during calculation of sheet '{sheetName}': {ex.Message}"); // Error: Calculation failed
                }
            }
            else
            {
                Logger.LogWarning($"Sheet '{sheetName}' not found for calculation.");
            }
            Logger.LogTrace($"Exiting CalculateSheet."); // Trace: Method exit
        }

        /// <summary>
        /// Clears content (values and formulas) in specified columns for rows below the last row containing data
        /// in the primary check column. Leaves formatting intact.
        /// </summary>
        private void ClearContentBelowLastCustomer(ExcelPackage package, string sheetName, int checkColumnIndex, int firstClearColumnIndex, int lastClearColumnIndex) // Removed static
        {
            Logger.LogTrace($"Entering ClearContentBelowLastCustomer(sheetName: {sheetName}, checkCol: {checkColumnIndex})"); // Trace: Method entry
            ExcelWorksheet? worksheet = package.Workbook.Worksheets[sheetName];
            if (worksheet == null || worksheet.Dimension == null)
            {
                Logger.LogWarning($"Sheet '{sheetName}' not found or is empty, cannot clear content.");
                Logger.LogTrace($"Exiting ClearContentBelowLastCustomer early - sheet not found or empty."); // Trace: Exit path
                return;
            }

            int totalRows = worksheet.Dimension.Rows;
            int lastCustomerRow = 0;
            int customerDataStartRow = 6;

            Logger.LogTrace($"ClearContentBelowLastCustomer: Finding last customer row in column {checkColumnIndex}..."); // Trace: Internal step
            for (int row = totalRows; row >= customerDataStartRow; row--)
            {
                var cellValue = worksheet.Cells[row, checkColumnIndex].Value;
                if (cellValue != null && !string.IsNullOrWhiteSpace(cellValue.ToString()))
                {
                    lastCustomerRow = row;
                    break;
                }
            }

            Logger.LogDebug($"ClearContentBelowLastCustomer: Sheet '{sheetName}': Total rows: {totalRows}, Last customer found at row: {lastCustomerRow} (Data starts row {customerDataStartRow})"); // Debug: State info

            if (lastCustomerRow == 0 && totalRows >= customerDataStartRow)
            {
                Logger.LogWarning($"No customer data found in column {checkColumnIndex} starting from row {customerDataStartRow} in sheet '{sheetName}'. Clearing content from row {customerDataStartRow} onwards.");
                lastCustomerRow = customerDataStartRow - 1;
            }
            else if (lastCustomerRow == 0)
            {
                Logger.LogInfo($"No customer data found and few rows exist in sheet '{sheetName}'. No content to clear."); // Info: Normal condition
                Logger.LogTrace($"Exiting ClearContentBelowLastCustomer - no customer data found."); // Trace: Exit path
                return;
            }
            else if (lastCustomerRow >= totalRows)
            {
                Logger.LogInfo($"Last customer is on the last row ({lastCustomerRow}). No content below to clear in sheet '{sheetName}'."); // Info: Normal condition
                Logger.LogTrace($"Exiting ClearContentBelowLastCustomer - last customer on last row."); // Trace: Exit path
                return;
            }

            int startClearRow = lastCustomerRow + 1;
            if (startClearRow > totalRows)
            {
                Logger.LogInfo($"Start clear row ({startClearRow}) is beyond total rows ({totalRows}). No content to clear."); // Info: Normal condition
                Logger.LogTrace($"Exiting ClearContentBelowLastCustomer - start clear row beyond total rows."); // Trace: Exit path
                return;
            }

            ExcelRange rangeToClear = worksheet.Cells[startClearRow, firstClearColumnIndex, totalRows, lastClearColumnIndex];
            Logger.LogInfo($"Clearing cell values (setting to null) in range {rangeToClear.Address} (below last customer row {lastCustomerRow}) in sheet '{sheetName}'."); // Info: Significant action
            rangeToClear.Value = null;
            Logger.LogInfo($"Cleared cell values in {totalRows - startClearRow + 1} rows below the last customer."); // Info: Action result
            Logger.LogTrace($"Exiting ClearContentBelowLastCustomer."); // Trace: Method exit
        }


        /// <summary>
        /// Sets a specific pivot table to refresh when the workbook is opened.
        /// </summary>
        private void RefreshPivotTable(ExcelPackage package, string sheetName, string pivotTableName) // Removed static
        {
            Logger.LogTrace($"Entering RefreshPivotTable(sheetName: {sheetName}, pivotTable: {pivotTableName})"); // Trace: Method entry
            ExcelWorksheet? worksheet = package.Workbook.Worksheets[sheetName];
            if (worksheet == null)
            {
                Logger.LogWarning($"Sheet '{sheetName}' not found for pivot table refresh setting.");
                Logger.LogTrace($"Exiting RefreshPivotTable early - sheet not found."); // Trace: Exit path
                return;
            }

            ExcelPivotTable? pivotTable = worksheet.PivotTables.FirstOrDefault(pt => pt.Name == pivotTableName);

            if (pivotTable != null)
            {
                try
                {
                    Logger.LogTrace($"Attempting to set Refresh for pivot table '{pivotTableName}' in sheet '{sheetName}'."); // Trace: Internal step
                    pivotTable.CacheDefinition.Refresh();
                    Logger.LogInfo($"Set pivot table '{pivotTableName}' in sheet '{sheetName}' to refresh."); // Info: Action performed
                }
                catch (Exception ex)
                {
                    Logger.LogError($"Error setting RefreshOnLoad for pivot table '{pivotTableName}' in '{sheetName}': {ex.Message}"); // Error: Pivot operation failed
                }
            }
            else
            {
                Logger.LogWarning($"Pivot table '{pivotTableName}' not found in sheet '{sheetName}'. Available tables: {string.Join(", ", worksheet.PivotTables.Select(pt => pt.Name))}");
            }
            Logger.LogTrace($"Exiting RefreshPivotTable."); // Trace: Method exit
        }

        /// <summary>
        /// Copies data VALUES from the processed Analysis sheet to the central weekly report file asynchronously.
        /// Appends data to the sheet corresponding to the selected financial year.
        /// Sets the SourceFileName column based on the report type and report date.
        /// </summary>
        private async Task CopyAnalysisDataToWeeklyReportAsync( // Removed static
            ExcelPackage sourcePackage,
            string sourceSheetName,
            IProgress<ProgressReport>? progress,
            string selectedFinYear,
            int reportType,
            string originalSourceFilePath,
            DateTime reportDate,
            CancellationToken cancellationToken)
        {
            Logger.LogTrace($"Entering CopyAnalysisDataToWeeklyReportAsync(sourceSheet: {sourceSheetName}, finYear: {selectedFinYear})"); // Trace: Method entry
            string username = Environment.UserName;
            string destinationFilePath = GetWeeklyReportPath(username); // Use instance method

            if (string.IsNullOrEmpty(destinationFilePath))
            {
                Logger.LogError($"Central weekly report path is invalid or could not be determined. Cannot append data."); // Error: Cannot proceed
                progress?.Report(new ProgressReport("Error: Central weekly report path invalid."));
                Logger.LogTrace($"Exiting CopyAnalysisDataToWeeklyReportAsync early - invalid destination path."); // Trace: Exit path
                return;
            }
            if (!File.Exists(destinationFilePath))
            {
                Logger.LogError($"Central weekly report file not found: '{destinationFilePath}'. Cannot append data."); // Error: Cannot proceed
                progress?.Report(new ProgressReport("Error: Central weekly report file not found."));
                Logger.LogTrace($"Exiting CopyAnalysisDataToWeeklyReportAsync early - destination file not found."); // Trace: Exit path
                return;
            }

            ExcelWorksheet? sourceWorksheet = sourcePackage.Workbook.Worksheets[sourceSheetName];
            if (sourceWorksheet == null || sourceWorksheet.Dimension == null)
            {
                Logger.LogWarning($"Source sheet '{sourceSheetName}' not found or is empty. Cannot copy to weekly report.");
                progress?.Report(new ProgressReport("Warning: No analysis data to copy to weekly report."));
                Logger.LogTrace($"Exiting CopyAnalysisDataToWeeklyReportAsync early - source sheet not found or empty."); // Trace: Exit path
                return;
            }

            try
            {
                Logger.LogInfo($"Opening weekly report file for appending: {destinationFilePath}"); // Info: Starting action
                using var destinationPackage = await Task.Run(() => new ExcelPackage(new FileInfo(destinationFilePath)), cancellationToken);
                Logger.LogTrace($"CopyAnalysisDataToWeeklyReportAsync: Destination package opened."); // Trace: Internal step

                string targetSheetName = selectedFinYear;
                ExcelWorksheet? destinationWorksheet = destinationPackage.Workbook.Worksheets[targetSheetName];

                if (destinationWorksheet == null)
                {
                    Logger.LogTrace($"CopyAnalysisDataToWeeklyReportAsync: Destination sheet '{targetSheetName}' not found, creating..."); // Trace: Internal step
                    destinationWorksheet = destinationPackage.Workbook.Worksheets.Add(targetSheetName);
                    CopyHeaders(sourceWorksheet, destinationWorksheet); // Use instance method
                    Logger.LogInfo($"Created sheet '{targetSheetName}' in weekly report and copied headers."); // Info: Sheet created
                }

                int nextFreeRow = await Task.Run(() => GetNextFreeRow(destinationWorksheet), cancellationToken); // Use instance method
                Logger.LogDebug($"Next free row in weekly report sheet '{targetSheetName}' is {nextFreeRow}."); // Debug: State info

                string filenameToWrite = GenerateFinalFileName(reportType, reportDate, DateTime.Now); // Use instance method
                Logger.LogDebug($"Using filename for weekly report append: {filenameToWrite}"); // Debug: State info

                Logger.LogTrace($"CopyAnalysisDataToWeeklyReportAsync: Starting row copy task..."); // Trace: Internal step
                await Task.Run(() =>
                {
                    int sourceRowCount = sourceWorksheet.Dimension.Rows;
                    int sourceColCount = sourceWorksheet.Dimension.End.Column;
                    int startDataRowInAnalysis = 6;

                    if (sourceRowCount < startDataRowInAnalysis)
                    {
                        Logger.LogWarning($"Source analysis sheet '{sourceSheetName}' has no data rows starting from row {startDataRowInAnalysis}.");
                        return;
                    }

                    int copiedRowCount = 0;
                    for (int sourceRow = startDataRowInAnalysis; sourceRow <= sourceRowCount; sourceRow++)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        var firstCellVal = sourceWorksheet.Cells[sourceRow, CustomerColumnIndex].Value;
                        if (firstCellVal != null && !string.IsNullOrWhiteSpace(firstCellVal.ToString()))
                        {
                            for (int col = 1; col <= sourceColCount; col++)
                            {
                                destinationWorksheet.Cells[nextFreeRow, col].Value = sourceWorksheet.Cells[sourceRow, col].Value;
                            }
                            destinationWorksheet.Cells[nextFreeRow, SourceFileNameColumnIndex].Value = filenameToWrite;
                            nextFreeRow++;
                            copiedRowCount++;
                        }

                        if (sourceRow % 50 == 0)
                        {
                            int percent = (int)((double)(sourceRow - startDataRowInAnalysis + 1) / (sourceRowCount - startDataRowInAnalysis + 1) * 100);
                            progress?.Report(new ProgressReport($"Copying to weekly report... {percent}%", percent));
                        }
                    }
                    Logger.LogInfo($"Copied values for {copiedRowCount} rows from '{sourceSheetName}' to weekly report sheet '{targetSheetName}'."); // Info: Action result
                    progress?.Report(new ProgressReport($"Copying to weekly report... 100%", 100));
                }, cancellationToken);
                Logger.LogTrace($"CopyAnalysisDataToWeeklyReportAsync: Row copy task finished."); // Trace: Internal step

                Logger.LogTrace($"CopyAnalysisDataToWeeklyReportAsync: Saving destination package..."); // Trace: Internal step
                await destinationPackage.SaveAsync(cancellationToken);
                Logger.LogInfo($"Successfully appended data to sheet '{targetSheetName}' in '{destinationFilePath}'."); // Info: Major action complete
                progress?.Report(new ProgressReport("Data appended to central weekly report."));
                Logger.LogTrace($"CopyAnalysisDataToWeeklyReportAsync: Destination package saved."); // Trace: Internal step

            }
            catch (OperationCanceledException)
            {
                Logger.LogWarning("Operation cancelled during copy to weekly report.");
                progress?.Report(new ProgressReport("Cancelled copy to weekly report."));
                throw;
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error copying data to weekly report '{destinationFilePath}': {ex}"); // Error: Operation failed
                progress?.Report(new ProgressReport($"Error copying to weekly report: {ex.Message}"));
            }
            Logger.LogTrace($"Exiting CopyAnalysisDataToWeeklyReportAsync."); // Trace: Method exit
        }


        /// <summary>
        /// Copies headers (Row 1) from a source worksheet to a destination worksheet.
        /// </summary>
        private void CopyHeaders(ExcelWorksheet sourceSheet, ExcelWorksheet destinationSheet) // Removed static
        {
            Logger.LogTrace($"Entering CopyHeaders(source: {sourceSheet.Name}, destination: {destinationSheet.Name})"); // Trace: Method entry
            if (sourceSheet.Dimension != null && sourceSheet.Dimension.Rows >= 1)
            {
                int headerColCount = sourceSheet.Dimension.Columns;
                ExcelRange sourceHeaderRow = sourceSheet.Cells[1, 1, 1, headerColCount];
                ExcelRange destHeader = destinationSheet.Cells[1, 1, 1, headerColCount];
                sourceHeaderRow.Copy(destHeader);
                Logger.LogTrace($"Copied header row (1 to {headerColCount}) from {sourceSheet.Name} to {destinationSheet.Name}"); // Trace: Internal detail
            }
            else
            {
                destinationSheet.Cells[1, 1].Value = "DefaultHeader";
                Logger.LogWarning($"Source sheet '{sourceSheet.Name}' for header copy was empty or had no rows. Added default header to {destinationSheet.Name}.");
            }
            Logger.LogTrace($"Exiting CopyHeaders."); // Trace: Method exit
        }

        /// <summary>
        /// Finds the next empty row in a worksheet by checking Column 1 from the bottom up.
        /// </summary>
        private int GetNextFreeRow(ExcelWorksheet worksheet) // Removed static
        {
            Logger.LogTrace($"Entering GetNextFreeRow(worksheet: {worksheet.Name})"); // Trace: Method entry
            if (worksheet.Dimension == null)
            {
                Logger.LogTrace($"Exiting GetNextFreeRow. Worksheet empty. Result: 1"); // Trace: Exit path
                return 1;
            }
            int lastUsedRow = worksheet.Dimension.End.Row;
            while (lastUsedRow >= 1)
            {
                var cell = worksheet.Cells[lastUsedRow, 1].Value;
                if (cell != null && !string.IsNullOrWhiteSpace(cell.ToString()))
                {
                    Logger.LogTrace($"Exiting GetNextFreeRow. Last used row: {lastUsedRow}. Result: {lastUsedRow + 1}"); // Trace: Exit path
                    return lastUsedRow + 1;
                }
                lastUsedRow--;
            }
            Logger.LogTrace($"Exiting GetNextFreeRow. Column 1 empty. Result: 1"); // Trace: Exit path
            return 1;
        }

        /// <summary>
        /// Gets the path to the central weekly report file based on DEBUG or RELEASE build configuration.
        /// </summary>
        private string GetWeeklyReportPath(string username) // Removed static
        {
            Logger.LogTrace($"Entering GetWeeklyReportPath(username: {username})"); // Trace: Method entry
#if DEBUG
            string path = $@"C:\Users\{username}\Harlow Printing\IT - Documents\PowerBI\Quote Conversion Report\Quotes conversion data_wrangled\weekly report quotes conversion merged - copy.xlsx";
            Logger.LogTrace($"Exiting GetWeeklyReportPath (DEBUG). Result: {path}"); // Trace: Method exit
            return path;
#else
                string path = $@"C:\Users\{username}\Harlow Printing\IT - Documents\PowerBI\Quote Conversion Report\Quotes conversion data_wrangled\weekly report quotes conversion merged.xlsx";
                 Logger.LogTrace($"Exiting GetWeeklyReportPath (RELEASE). Result: {path}"); // Trace: Method exit
                 return path;
#endif
        }

        #endregion Internal Processing Steps (Non-Static)

        #region File and Folder Helpers (REMOVED - Use FolderCreation class)

        // --- Methods removed from here and functionality delegated to FolderCreation ---
        // internal string? CreateReportSpecificFolder(...) // REMOVED
        // internal string? GetReportSpecificFolderPath(...) // REMOVED

        /// <summary>
        /// Generates the final file name based on the report type and the specific report date.
        /// Includes a timestamp for Custom reports.
        /// </summary>
        /// <param name="reportType">The report type index.</param>
        /// <param name="reportDate">The date the report pertains to (usually end date).</param>
        /// <param name="runTimestamp">The timestamp when the process was run (used for Custom filenames).</param>
        /// <returns>The file name string (not the full path).</returns>
        private string GenerateFinalFileName(int reportType, DateTime reportDate, DateTime runTimestamp) // Removed static, added runTimestamp
        {
            Logger.LogTrace($"Entering GenerateFinalFileName(reportType: {reportType}, reportDate: {reportDate:d})"); // Trace: Method entry
            string fileName;
            switch (reportType)
            {
                case DailyReportIndex:
                    fileName = $"{reportDate:yyyyMMdd}_Estimate_Success_Rate_Daily.xlsx";
                    break;
                case WeeklyReportIndex:
                    fileName = $"{reportDate:yyyyMMdd} Estimate Success Rate.xlsx";
                    break;
                case MonthlyReportIndex:
                    fileName = $"Estimate Success Rate {reportDate:MMM yy}.xlsx";
                    break;
                case QuarterlyReportIndex:
                    int quarter = (reportDate.Month - 1) / 3 + 1;
                    DateTime quarterStartDate = new(reportDate.Year, (quarter - 1) * 3 + 1, 1);
                    DateTime quarterEndDate = quarterStartDate.AddMonths(3).AddDays(-1);
                    string qtrFolderName = $"{quarterStartDate:MMM} to {quarterEndDate:MMM}{(quarterStartDate.Year != quarterEndDate.Year ? $" {quarterStartDate.Year}-{quarterEndDate.Year}" : $" {quarterStartDate.Year}")}";
                    fileName = $"Estimate Success Rate {qtrFolderName}.xlsx";
                    break;
                case AnnualReportIndex:
                    fileName = $"Estimate Success Rate {reportDate.Year}.xlsx";
                    break;
                case CustomReportIndex: // <<< ADDED CASE
                    // Use end date and timestamp for uniqueness
                    fileName = $"{reportDate:yyyyMMdd}_{runTimestamp:HHmmss}_Estimate_Success_Rate_Custom.xlsx";
                    break;
                default:
                    Logger.LogWarning($"Invalid report type '{reportType}' for filename generation, defaulting to generic format using report date.");
                    fileName = $"{reportDate:yyyyMMdd}_Estimate_Success_Rate_UnknownType.xlsx";
                    break;
            }
            Logger.LogTrace($"Exiting GenerateFinalFileName. Result: {fileName}"); // Trace: Method exit
            return fileName;
        }

        /// <summary>
        /// Attempts to rename (move) a file with retries on IOException.
        /// </summary>
        private async Task RenameFileWithRetryAsync(string sourcePath, string destinationPath, IProgress<ProgressReport>? progress, CancellationToken cancellationToken, int maxRetries = 5, int delayMs = 500) // Removed static
        {
            Logger.LogTrace($"Entering RenameFileWithRetryAsync(source: {sourcePath}, dest: {destinationPath})"); // Trace: Method entry
            for (int i = 0; i < maxRetries; i++)
            {
                try
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await Task.Run(() => File.Move(sourcePath, destinationPath, true), cancellationToken);
                    Logger.LogInfo($"Successfully renamed/moved '{sourcePath}' to '{destinationPath}'."); // Info: Action success
                    Logger.LogTrace($"Exiting RenameFileWithRetryAsync - Success."); // Trace: Exit path
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
                    Logger.LogTrace($"Exiting RenameFileWithRetryAsync - Cancelled."); // Trace: Exit path
                    throw;
                }
            }
            Logger.LogTrace($"Exiting RenameFileWithRetryAsync - Failed after retries."); // Trace: Exit path
            // Log final failure as Error
            throw new IOException($"Failed to rename file '{sourcePath}' to '{destinationPath}' after {maxRetries} attempts. The file might still be locked or another IO error occurred.");
        }

        #endregion File and Folder Helpers
    }
}
