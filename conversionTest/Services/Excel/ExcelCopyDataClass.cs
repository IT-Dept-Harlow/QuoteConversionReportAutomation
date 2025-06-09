// ExcelCopyData.cs
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
        }
        #endregion

        #region Public Instance Methods
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
        public async Task<string?> ProcessExcelReportAsync(
            string selectedFinYear,
            ReportType reportType,
            string sourceFilePath,
            string sourceSheetNameConfigKey,
            string baseFileSaveLocation,
            string templateFilePath,
            string destinationDataSheetNameConfigKey,
            int startRow = 1,
            int startCol = 1,
            DateTime reportDate = default,
            CancellationToken cancellationToken = default)
        {
            Logger.LogTrace($"Entering ProcessExcelReportAsync. ReportType: {reportType}, Source: {sourceFilePath}");
            var stopwatch = Stopwatch.StartNew();

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
            }

            // Default report date if not provided for standard reports.
            if (reportDate == default && reportType != ReportType.Custom)
            {
                reportDate = DateTime.Today;
            }

            string? finalFilePath = null;
            string? tempFilePath = null;
            string? fullOutputFolderPath = null;

            try
            {
                _statusManager.Post("Starting Excel processing...", MessageType.InProgress);
                cancellationToken.ThrowIfCancellationRequested();

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
                stopwatch.Stop();
                Logger.LogInfo($"ProcessExcelReportAsync completed successfully. Duration: {stopwatch.ElapsedMilliseconds}ms.");
                return finalFilePath;
            }
            catch (OperationCanceledException)
            {
                stopwatch.Stop();
                Logger.LogWarning($"Excel processing was cancelled. Duration: {stopwatch.ElapsedMilliseconds}ms.");
                _statusManager.Post("Operation cancelled.", MessageType.Warning, TimeSpan.FromSeconds(5));
                return null;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                Logger.LogError($"Error during Excel processing: {ex.Message}. Duration: {stopwatch.ElapsedMilliseconds}ms.", ex);
                // Post a persistent error message to the UI.
                _statusManager.Post($"Excel Error: {ex.Message}", MessageType.Error);
                return null;
            }
            finally
            {
                // Ensure temporary files are cleaned up in case of an error.
                if (tempFilePath != null && File.Exists(tempFilePath))
                {
                    try
                    {
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
        /// Gets the current financial year string (e.g., "2023_24" or "FY 23/24").
        /// </summary>
        /// <param name="useUnderscoreFormat">If true, returns "YYYY_YY" format; otherwise, returns "FY YY/YY" format.</param>
        /// <returns>The formatted financial year string.</returns>
        public string GetCurrentFinancialYear(bool useUnderscoreFormat = false)
        {
            DateTime today = DateTime.Today;
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
            string[] parts = currentFinancialYearUnderscore.Split('_');
            if (parts.Length == 2 && int.TryParse(parts[0], out int startYear))
            {
                int prevStartYear = startYear - 1;
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
            string[] parts = selectedFinYearUnderscore.Split('_');
            if (parts.Length == 2 && int.TryParse(parts[0], out int startYear))
            {
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
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error in GetExpectedFinalFilePath: {ex.Message}", ex);
            }
            return null;
        }
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
                cancellationToken.ThrowIfCancellationRequested();
            }
        }

        /// <summary>
        /// Ensures the destination "DATA" sheet exists in the package, creating and formatting it if necessary.
        /// </summary>
        private ExcelWorksheet GetOrCreateDestinationWorksheet(ExcelPackage package, string sheetName, ExcelWorksheet sourceWorksheet)
        {
            ExcelWorksheet? worksheet = package.Workbook.Worksheets[sheetName];
            if (worksheet == null)
            {
                worksheet = package.Workbook.Worksheets.Add(sheetName);
                if (sourceWorksheet.Dimension != null && sourceWorksheet.Dimension.Rows >= 1)
                {
                    sourceWorksheet.Cells[1, 1, 1, sourceWorksheet.Dimension.Columns].Copy(worksheet.Cells[1, 1]);
                }
            }
            else
            {
                if (worksheet.Dimension != null && worksheet.Dimension.Rows > 1)
                {
                    worksheet.DeleteRow(2, worksheet.Dimension.Rows - 1);
                }
            }
            return worksheet;
        }

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
                    if (deleteRow) worksheet.DeleteRow(r, 1);
                }
            }, cancellationToken);
        }

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
            }
        }

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
            ExcelWorksheet? worksheet = package.Workbook.Worksheets[sheetName];
            if (worksheet == null || worksheet.Dimension == null) return;
            const int customerDataStartRow = 6;
            int lastActualDataRow = customerDataStartRow - 1;

            for (int r = worksheet.Dimension.End.Row; r >= customerDataStartRow; r--)
            {
                if (worksheet.Cells[r, customerNameColIdx].Value != null && !string.IsNullOrWhiteSpace(worksheet.Cells[r, customerNameColIdx].Value.ToString()))
                {
                    lastActualDataRow = r;
                    break;
                }
            }

            int startClearTargetRow = lastActualDataRow + 1;
            if (startClearTargetRow <= worksheet.Dimension.End.Row)
            {
                worksheet.Cells[startClearTargetRow, firstColToClear, worksheet.Dimension.End.Row, lastColToClear].Clear();
            }
        }

        /// <summary>
        /// Sets the specified pivot table to refresh its data when the Excel file is opened.
        /// </summary>
        private void RefreshPivotTable(ExcelPackage package, string sheetName, string pivotTableName)
        {
            ExcelWorksheet? worksheet = package.Workbook.Worksheets[sheetName];
            if (worksheet == null) return;
            ExcelPivotTable? pivotTable = worksheet.PivotTables.FirstOrDefault(pt => pt.Name.Equals(pivotTableName, StringComparison.OrdinalIgnoreCase));
            if (pivotTable != null)
            {
                try
                {
                    pivotTable.CacheDefinition.Refresh();
                }
                catch (Exception ex)
                {
                    Logger.LogError($"Error setting RefreshDataOnOpen for pivot table '{pivotTableName}' in '{sheetName}': {ex.Message}", ex);
                }
            }
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
                return;
            }
            try
            {
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
                            {
                                destinationWorksheet.Cells[nextFreeRowInPowerBiSheet, col].Value = sourceWorksheet.Cells[sourceRow, col].Value;
                            }
                            destinationWorksheet.Cells[nextFreeRowInPowerBiSheet, AnalysisSheetSourceFileNameColumnIndex].Value = filenameForPowerBiColumn;
                            nextFreeRowInPowerBiSheet++;
                        }
                    }
                }, cancellationToken);
                await destinationPackage.SaveAsync(cancellationToken);
                _statusManager.Post("Data appended to Power BI source.", MessageType.Success, TimeSpan.FromSeconds(5));
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error copying data to Power BI source file: {ex.Message}", ex);
                _statusManager.Post($"Error (Power BI): {ex.Message}", MessageType.Error);
            }
        }

        /// <summary>
        /// Copies header rows from a source worksheet to a destination worksheet.
        /// </summary>
        private void CopyHeaders(ExcelWorksheet sourceSheet, ExcelWorksheet destinationSheet, int startHeaderRow = 1, int endHeaderRow = 1)
        {
            if (sourceSheet.Dimension != null && sourceSheet.Dimension.Rows >= endHeaderRow)
            {
                sourceSheet.Cells[startHeaderRow, 1, endHeaderRow, sourceSheet.Dimension.Columns].Copy(destinationSheet.Cells[startHeaderRow, 1]);
            }
        }

        /// <summary>
        /// Finds the next available (empty) row in a worksheet, starting from a specified data row.
        /// </summary>
        private int GetNextFreeRow(ExcelWorksheet worksheet, int checkColumn = 1)
        {
            if (worksheet.Dimension == null) return 1;
            int lastUsedRow = worksheet.Dimension.End.Row;
            for (int r = lastUsedRow; r >= 1; r--)
            {
                if (worksheet.Cells[r, checkColumn].Value != null && !string.IsNullOrWhiteSpace(worksheet.Cells[r, checkColumn].Value.ToString()))
                {
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

            for (int i = 0; i < maxRetries; i++)
            {
                try
                {
                    cancellationToken.ThrowIfCancellationRequested();
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
        }
        #endregion
    }
}