// C# 10+ Features (using file-scoped namespace, global using directives if applicable elsewhere)
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
        }
        #endregion

        #region Public Instance Methods

        public async Task<string?> ProcessExcelReportAsync(
            string selectedFinYear,
            int reportType,
            string sourceFilePath,
            string sourceSheetName,
            string baseFileSaveLocation,
            string templateFilePath,
            string destinationDataSheetName, // Typically "DATA"
            int startRow = 1,
            int startCol = 1,
            IProgress<ProgressReport>? progress = null,
            DateTime reportDate = default,
            CancellationToken cancellationToken = default)
        {
            Logger.LogTrace($"Entering ProcessExcelReportAsync. ReportType: {reportType}, Source: {sourceFilePath}, Template: {templateFilePath}, ReportDate: {reportDate:yyyy-MM-dd}");
            var stopwatch = Stopwatch.StartNew();

            ArgumentException.ThrowIfNullOrEmpty(sourceFilePath);
            ArgumentException.ThrowIfNullOrEmpty(sourceSheetName);
            ArgumentException.ThrowIfNullOrEmpty(baseFileSaveLocation);
            ArgumentException.ThrowIfNullOrEmpty(templateFilePath);
            ArgumentException.ThrowIfNullOrEmpty(destinationDataSheetName);

            if (reportType == WeeklyReportIndex || reportType == DailyReportIndex || reportType == NewDailyReportOver1kIndex)
            {
                ArgumentException.ThrowIfNullOrEmpty(selectedFinYear);
            }

            if (reportDate == default && reportType != CustomReportIndex)
            {
                reportDate = DateTime.Today;
                Logger.LogWarning($"ProcessExcelReportAsync called without a specific reportDate for non-custom report. Defaulting to Today for filename generation: {reportDate:yyyy-MM-dd}");
            }

            string? finalFilePath = null;
            string? tempFilePath = null;
            string? fullOutputFolderPath = null;

            try
            {
                progress?.Report(new ProgressReport("Starting Excel processing...", 0));
                cancellationToken.ThrowIfCancellationRequested();

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

                stopwatch.Stop();
                Logger.LogInfo($"ProcessExcelReportAsync completed successfully. Duration: {stopwatch.ElapsedMilliseconds}ms.");
                Logger.LogDebug($"Exiting ProcessExcelReportAsync. Result: {finalFilePath}");
                return finalFilePath;
            }
            catch (OperationCanceledException)
            {
                stopwatch.Stop();
                Logger.LogWarning($"Excel processing was cancelled. Duration: {stopwatch.ElapsedMilliseconds}ms.");
                progress?.Report(new ProgressReport("Operation cancelled."));
                Logger.LogTrace($"Exiting ProcessExcelReportAsync due to cancellation.");
                return null;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                Logger.LogError($"Error during Excel processing: {ex}. Duration: {stopwatch.ElapsedMilliseconds}ms.");
                progress?.Report(new ProgressReport($"Error: {ex.Message}"));
                Logger.LogTrace($"Exiting ProcessExcelReportAsync due to error.");
                return null;
            }
            finally
            {
                if (tempFilePath != null && File.Exists(tempFilePath))
                {
                    try
                    {
                        Logger.LogDebug($"ProcessExcelReportAsync: Cleaning up temporary file '{tempFilePath}'...");
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

        public string GetCurrentFinancialYear(bool useUnderscoreFormat = false)
        {
            Logger.LogTrace($"Entering GetCurrentFinancialYear(useUnderscoreFormat: {useUnderscoreFormat})");
            DateTime today = DateTime.Today;
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
            string[] parts = selectedFinYearUnderscore.Split('_');
            bool isValid = false;
            if (parts.Length == 2 && int.TryParse(parts[0], out int startYear))
            {
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
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error getting expected final file path: {ex.Message}");
            }
            Logger.LogTrace($"Exiting GetExpectedFinalFilePath. Result: {result ?? "null"}");
            return result;
        }

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
                cancellationToken.ThrowIfCancellationRequested();
            }
            stopwatch.Stop();
            Logger.LogDebug($"Exiting ProcessPostCopyOperationsAsync. Duration: {stopwatch.ElapsedMilliseconds}ms");
        }

        private ExcelWorksheet GetOrCreateDestinationWorksheet(ExcelPackage package, string sheetName, ExcelWorksheet sourceWorksheet)
        {
            Logger.LogTrace($"Entering GetOrCreateDestinationWorksheet(sheetName: {sheetName}, sourceSheet: {sourceWorksheet.Name})");
            ExcelWorksheet? worksheet = package.Workbook.Worksheets[sheetName];
            if (worksheet == null)
            {
                worksheet = package.Workbook.Worksheets.Add(sheetName);
                if (sourceWorksheet.Dimension != null && sourceWorksheet.Dimension.Rows >= 1)
                {
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
                }
            }
            else
            {
                if (worksheet.Dimension != null && worksheet.Dimension.Rows > 1)
                {
                    worksheet.DeleteRow(2, worksheet.Dimension.Rows - 1);
                    Logger.LogInfo($"Cleared existing data (rows 2 onwards) from sheet '{sheetName}'. Headers in row 1 preserved.");
                }
                else
                {
                    Logger.LogDebug($"Sheet '{sheetName}' already existed but had no data below header row (or was empty).");
                }
            }
            Logger.LogTrace($"Exiting GetOrCreateDestinationWorksheet. Returning sheet: {worksheet.Name}");
            return worksheet;
        }

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
                }
            }

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
            }

            Logger.LogInfo($"Populated {uniqueCustomers.Count} unique customers into '{targetAnalysisSheetName}'. Report date: {reportDate:dd/MM/yyyy}.");
            Logger.LogTrace($"Exiting ExtractUniqueCustomersAsync for sheet '{targetAnalysisSheetName}'.");
        }

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
            ExcelWorksheet? worksheet = package.Workbook.Worksheets[sheetName];

            if (worksheet == null || worksheet.Dimension == null)
            {
                Logger.LogWarning($"Sheet '{sheetName}' not found or is empty. Nothing to clear by ClearContentBelowLastCustomer.");
                return;
            }

            const int customerDataStartRow = 6;
            int lastActualDataRow = customerDataStartRow - 1;

            for (int r = worksheet.Dimension.End.Row; r >= customerDataStartRow; r--)
            {
                if (worksheet.Cells[r, customerNameColIdx].Value != null &&
                    !string.IsNullOrWhiteSpace(worksheet.Cells[r, customerNameColIdx].Value.ToString()))
                {
                    lastActualDataRow = r;
                    break;
                }
            }
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
        }

        private void RefreshPivotTable(ExcelPackage package, string sheetName, string pivotTableName)
        {
            Logger.LogTrace($"Entering RefreshPivotTable(sheetName: {sheetName}, pivotTable: {pivotTableName})");
            ExcelWorksheet? worksheet = package.Workbook.Worksheets[sheetName];
            if (worksheet == null)
            {
                Logger.LogWarning($"Sheet '{sheetName}' not found for pivot table refresh setting.");
                Logger.LogTrace($"Exiting RefreshPivotTable early - sheet not found.");
                return;
            }

            ExcelPivotTable? pivotTable = worksheet.PivotTables.FirstOrDefault(pt => pt.Name == pivotTableName);

            if (pivotTable != null)
            {
                try
                {
                    Logger.LogTrace($"Attempting to set RefreshDataOnOpen for pivot table '{pivotTableName}' in sheet '{sheetName}'.");
                    pivotTable.CacheDefinition.Refresh();
                    Logger.LogInfo($"Set pivot table '{pivotTableName}' in sheet '{sheetName}' to refresh on load (RefreshDataOnOpen=true).");
                }
                catch (Exception ex)
                {
                    Logger.LogError($"Error setting RefreshDataOnOpen for pivot table '{pivotTableName}' in '{sheetName}': {ex.Message}");
                }
            }
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
                return;
            }

            try
            {
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
                            {
                                destinationWorksheet.Cells[nextFreeRow, col].Value = sourceWorksheet.Cells[sourceRow, col].Value;
                            }
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

            }
            catch (OperationCanceledException)
            {
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
        }

        private void CopyHeaders(ExcelWorksheet sourceSheet, ExcelWorksheet destinationSheet, int startHeaderRow = 1, int endHeaderRow = 1)
        {
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
            }
            Logger.LogTrace($"Exiting CopyHeaders.");
        }

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

            for (int r = lastUsedRow; r >= 1; r--)
            {
                var cell = worksheet.Cells[r, checkColumn].Value;
                if (cell != null && !string.IsNullOrWhiteSpace(cell.ToString()))
                {
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
            for (int i = 0; i < maxRetries; i++)
            {
                try
                {
                    cancellationToken.ThrowIfCancellationRequested();
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
        }

        #endregion
    }
}
