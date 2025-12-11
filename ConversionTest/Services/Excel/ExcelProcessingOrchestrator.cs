// QuoteConversionReportAutomation/Services/Excel/ExcelProcessingOrchestrator.cs

#region Using Directives

// System-related namespaces for core functionalities.
// Third-party namespaces for external libraries.
using Microsoft.Extensions.Configuration;
using OfficeOpenXml;
using OfficeOpenXml.Table.PivotTable;
using QuoteConversionReportAutomation.Configuration;
// Project-specific namespaces for application components.
using QuoteConversionReportAutomation.Helpers;
using QuoteConversionReportAutomation.Interfaces;
using QuoteConversionReportAutomation.Models;
using QuoteConversionReportAutomation.Models.Status;
using QuoteConversionReportAutomation.Orchestrators;
using QuoteConversionReportAutomation.Services.Interfaces;
using QuoteConversionReportAutomation.Services.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

#endregion

namespace QuoteConversionReportAutomation.Services.Excel
{
    #region Class Definition
    /// <summary>
    /// Implements <see cref="IExcelProcessingOrchestrator"/> to manage the high-level workflow of processing an Excel report.
    /// This class acts as a coordinator, delegating specific tasks like filtering, analysis, and data appending
    /// to specialised services.
    /// </summary>
    public class ExcelProcessingOrchestrator : IExcelProcessingOrchestrator
    {
        #region Fields

        private readonly IConfiguration _configuration;
        private readonly IStatusManagerService _statusManager;
        private readonly IReportPathService _reportPathService;
        private readonly IExcelFilteringService _filteringService;
        private readonly IExcelAnalysisService _analysisService;
        private readonly ILeadTimeAnalysisService _leadTimeService;
        private readonly IPowerBiDataService _powerBiService;
        private readonly IExcelDataExclusionService _exclusionService; // New dependency

        #endregion

        #region Constructor

        /// <summary>
        /// Initialises a new instance of the <see cref="ExcelProcessingOrchestrator"/> class.
        /// </summary>
        /// <param name="configuration">The application's configuration settings.</param>
        /// <param name="statusManager">The centralised service for status reporting.</param>
        /// <param name="reportPathService">The service for generating file paths and names.</param>
        /// <param name="filteringService">The service responsible for data filtering.</param>
        /// <param name="analysisService">The service responsible for generating the main analysis sheet.</param>
        /// <param name="leadTimeService">The service responsible for generating the lead time analysis sheet.</param>
        /// <param name="powerBiService">The service responsible for appending data to the Power BI source file.</param>
        /// <param name="exclusionService">The service responsible for excluding tender account data.</param>
        public ExcelProcessingOrchestrator(
            IConfiguration configuration,
            IStatusManagerService statusManager,
            IReportPathService reportPathService,
            IExcelFilteringService filteringService,
            IExcelAnalysisService analysisService,
            ILeadTimeAnalysisService leadTimeService,
            IPowerBiDataService powerBiService,
            IExcelDataExclusionService exclusionService) // New dependency
        {
            // Assign all injected dependencies.
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _statusManager = statusManager ?? throw new ArgumentNullException(nameof(statusManager));
            _reportPathService = reportPathService ?? throw new ArgumentNullException(nameof(reportPathService));
            _filteringService = filteringService ?? throw new ArgumentNullException(nameof(filteringService));
            _analysisService = analysisService ?? throw new ArgumentNullException(nameof(analysisService));
            _leadTimeService = leadTimeService ?? throw new ArgumentNullException(nameof(leadTimeService));
            _powerBiService = powerBiService ?? throw new ArgumentNullException(nameof(powerBiService));
            _exclusionService = exclusionService ?? throw new ArgumentNullException(nameof(exclusionService)); // New dependency

            // Set the license context for the EPPlus library.
            ExcelPackage.License.SetNonCommercialPersonal("Harlow");
            Logger.LogTrace("ExcelProcessingOrchestrator instance created.");
        }

        #endregion

        #region IExcelProcessingOrchestrator Implementation

        /// <inheritdoc/>
        public async Task<string?> ProcessExcelReportAsync(
            string selectedFinYear,
            ReportType reportType,
            string sourceFilePath,
            string sourceSheetNameConfigKey,
            string baseFileSaveLocation,
            string templateFilePath,
            string destinationDataSheetNameConfigKey,
            int startRow,
            int startCol,
            DateTime reportDate,
            ManualReportParameters? manualParams,
            AutoReportDefinition? autoRunDef,
            CancellationToken cancellationToken)
        {
            var stopwatch = Stopwatch.StartNew();
            Logger.LogTrace($"Entering ProcessExcelReportAsync. ReportType: {reportType}, Source: {sourceFilePath}");

            // --- Parameter Validation ---
            ArgumentException.ThrowIfNullOrEmpty(sourceFilePath, nameof(sourceFilePath));
            ArgumentException.ThrowIfNullOrEmpty(baseFileSaveLocation, nameof(baseFileSaveLocation));
            ArgumentException.ThrowIfNullOrEmpty(templateFilePath, nameof(templateFilePath));

            // --- Configuration Retrieval ---
            string sourceSheetName = _configuration.GetValue<string>($"OperationalParameters:ExcelSheetNames:{sourceSheetNameConfigKey}", "Sheet1")!;
            string destinationDataSheetName = _configuration.GetValue<string>($"OperationalParameters:ExcelSheetNames:{destinationDataSheetNameConfigKey}", "DATA")!;
            string analysisSheetName = _configuration.GetValue<string>("OperationalParameters:ExcelSheetNames:TemplateAnalysisSheet", "Analysis")!;
            string powerBiSheetName = _configuration.GetValue<string>("OperationalParameters:ExcelSheetNames:PowerBiDataSheet", "powerBI")!;

            string? finalFilePath = null;
            string? tempFilePath = null;

            try
            {
                _statusManager.Post("Starting Excel processing...", MessageType.InProgress);
                cancellationToken.ThrowIfCancellationRequested();

                // 1. Prepare Environment: Determine and create output folder, create temporary file.
                DateTime folderTimestampDate = reportType == ReportType.Custom ? DateTime.Now : reportDate;
                string? fullOutputFolderPath = FolderCreation.CreateReportSpecificFolder(reportType, baseFileSaveLocation, folderTimestampDate, _configuration);
                if (string.IsNullOrEmpty(fullOutputFolderPath)) throw new InvalidOperationException("Failed to create the report output folder.");

                tempFilePath = Path.Combine(fullOutputFolderPath, $"temp_processing_{Guid.NewGuid()}.xlsx");
                await Task.Run(() => File.Copy(templateFilePath, tempFilePath, true), cancellationToken);
                _statusManager.Post("Template prepared.", MessageType.InProgress);

                // 2. Open Excel Packages and perform core operations.
                using (var sourcePackage = new ExcelPackage(new FileInfo(sourceFilePath)))
                using (var destinationPackage = new ExcelPackage(new FileInfo(tempFilePath)))
                {
                    // Get worksheet references.
                    ExcelWorksheet sourceWorksheet = sourcePackage.Workbook.Worksheets[sourceSheetName] ?? throw new FileNotFoundException($"Source sheet '{sourceSheetName}' not found in '{sourceFilePath}'.");
                    ExcelWorksheet destinationDataWorksheet = GetOrCreateDestinationWorksheet(destinationPackage, destinationDataSheetName, sourceWorksheet);
                    ExcelWorksheet analysisSheet = destinationPackage.Workbook.Worksheets[analysisSheetName] ?? throw new InvalidOperationException($"Analysis sheet '{analysisSheetName}' not found in template.");

                    // Copy raw data from source to the template's DATA sheet.
                    await CopyRawDataAsync(sourceWorksheet, destinationDataWorksheet, startRow, startCol, cancellationToken);

                    // --- DELEGATE TO SPECIALISED SERVICES ---

                    // 3. Exclude tender accounts first.
                    destinationDataWorksheet = await _exclusionService.ExcludeTenderAccountsAsync(destinationDataWorksheet, cancellationToken);

                    // 4. Filter the DATA sheet if required by the report type.
                    destinationDataWorksheet = await FilterDataIfNeededAsync(reportType, destinationDataWorksheet, cancellationToken);

                    // 5. Create the main Analysis sheet content.
                    _statusManager.Post("Generating analysis...", MessageType.InProgress);
                    var dataColumnMap = ExcelHelper.MapColumnIndices(destinationDataWorksheet, 1, new[] { "Customer", "Price", "Posting Code", "Rep", "Ordered" });
                    var analysisColumnMap = ExcelHelper.MapColumnIndices(analysisSheet, 5, new[] { "Customer", "Number of Estimates", "SOURCE FILE", "CONVERTED DATE", "FY", "Posting Code", "Contract Status", "Rep", "Estimates Won", "Estimates Not Won", "% Win", "Value of Estimates", "Value of Estimates Won", "Value of Est Not Confirmed", "Value Won %" });
                    await _analysisService.CreateAnalysisSheetAsync(destinationDataWorksheet, analysisSheet, dataColumnMap, analysisColumnMap, reportDate, sourceFilePath, cancellationToken);

                    // 6. Create the Lead Time Analysis sheet if required.
                    bool shouldIncludeLeadTimeSheet = (manualParams?.IncludeLeadTimeAnalysis ?? autoRunDef?.IncludeLeadTimeAnalysis) ?? false;
                    if (shouldIncludeLeadTimeSheet)
                    {
                        await _leadTimeService.CreateLeadTimeAnalysisSheetAsync(destinationPackage, destinationDataSheetName, cancellationToken);
                    }

                    // 7. Refresh Pivot Tables if the template uses them.
                    await RefreshPivotsIfNeededAsync(reportType, destinationPackage, cancellationToken);

                    // 8. Append data to the central Power BI file if required.
                    if (reportType == ReportType.Weekly)
                    {
                        await _powerBiService.AppendDataToPowerBIReportAsync(destinationPackage, analysisSheet, powerBiSheetName, cancellationToken);
                    }

                    // 9. Save all changes to the temporary file.
                    _statusManager.Post("Saving processed file...", MessageType.InProgress);
                    await destinationPackage.SaveAsync(cancellationToken);
                }

                // 10. Finalise: Rename the temporary file to its final, permanent name.
                _statusManager.Post("Finalising file...", MessageType.InProgress);
                string generatedFileName = _reportPathService.GenerateFinalFileName(reportType, reportDate, DateTime.Now);
                finalFilePath = Path.Combine(fullOutputFolderPath, generatedFileName);
                await RenameFileWithRetryAsync(tempFilePath, finalFilePath, cancellationToken);
                tempFilePath = null; // Prevent deletion in the finally block.

                _statusManager.Post("Excel processing complete.", MessageType.Success, TimeSpan.FromSeconds(5));
                stopwatch.Stop();
                Logger.LogInfo($"Excel processing finished. Final file: {finalFilePath}. Duration: {stopwatch.ElapsedMilliseconds}ms.");
                return finalFilePath;
            }
            catch (OperationCanceledException)
            {
                stopwatch.Stop();
                Logger.LogWarning($"Excel processing was cancelled. Duration: {stopwatch.ElapsedMilliseconds}ms.");
                _statusManager.Post("Operation cancelled.", MessageType.Warning);
                return null;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                Logger.LogError($"Unexpected error during Excel processing: {ex.Message}. Duration: {stopwatch.ElapsedMilliseconds}ms.", ex);
                _statusManager.Post($"Error: {ex.Message.Split('\n').FirstOrDefault()}", MessageType.Error);
                return null;
            }
            finally
            {
                // Cleanup: Ensure the temporary file is deleted if an error occurred before it was renamed.
                if (tempFilePath != null && File.Exists(tempFilePath))
                {
                    try { File.Delete(tempFilePath); } catch (Exception cleanupEx) { Logger.LogWarning($"Failed to delete temporary file '{tempFilePath}': {cleanupEx.Message}"); }
                }
            }
        }
        #endregion

        #region Private Orchestration Helpers

        /// <summary>
        /// Copies raw data from the source worksheet to the destination data worksheet.
        /// </summary>
        private async Task CopyRawDataAsync(ExcelWorksheet source, ExcelWorksheet destination, int startRow, int startCol, CancellationToken cancellationToken)
        {
            _statusManager.Post("Copying data from source to template...", MessageType.InProgress);
            await Task.Run(() =>
            {
                int sourceRowCount = source.Dimension?.Rows ?? 0;
                int sourceColCount = source.Dimension?.Columns ?? 0;

                if (sourceRowCount >= startRow && sourceColCount >= startCol)
                {
                    int sourceDataActualStartRow = (startRow == 1 && sourceRowCount > 1) ? 2 : startRow;
                    if (sourceRowCount >= sourceDataActualStartRow)
                    {
                        ExcelRange sourceRangeToCopy = source.Cells[sourceDataActualStartRow, startCol, sourceRowCount, sourceColCount];
                        ExcelRange destStartCellForData = destination.Cells[2, 1];
                        sourceRangeToCopy.Copy(destStartCellForData);
                        Logger.LogInfo($"Data copied from '{source.Name}' to '{destination.Name}'.");
                    }
                }
            }, cancellationToken);
            _statusManager.Post("Initial data copy complete.", MessageType.InProgress);
        }

        /// <summary>
        /// Delegates to the filtering service if the report type requires filtering and returns the new worksheet reference.
        /// </summary>
        private async Task<ExcelWorksheet> FilterDataIfNeededAsync(ReportType reportType, ExcelWorksheet worksheet, CancellationToken cancellationToken)
        {
            if (reportType == ReportType.Daily5Day1k)
            {
                // Get the filtering threshold from configuration.
                decimal filterThreshold = _configuration.GetValue<decimal>("OperationalParameters:Daily5Day1kFilteringThreshold", 1000m);
                _statusManager.Post($"Filtering for values >= £{filterThreshold:N0}...", MessageType.InProgress);
                // Map the required column and call the filtering service.
                var columnMap = ExcelHelper.MapColumnIndices(worksheet, 1, new[] { "Price" });
                // IMPORTANT: Reassign the worksheet variable with the result.
                return await _filteringService.FilterDataSheetByValueAsync(worksheet, columnMap["Price"], filterThreshold, cancellationToken);
            }
            else if (reportType == ReportType.NewCustomer)
            {
                // Get the valid posting codes from configuration.
                _statusManager.Post("Filtering for New Customers by Posting Code...", MessageType.InProgress);
                var postingCodes = _configuration.GetSection(AppConfigKeys.OperationalParameters.NewCustomerPostingCodes).Get<List<string>>();
                if (postingCodes == null || !postingCodes.Any())
                {
                    throw new InvalidOperationException("New Customer posting codes are not configured in appsettings.json.");
                }
                // Map the required column and call the filtering service.
                var columnMap = ExcelHelper.MapColumnIndices(worksheet, 1, new[] { "Posting Code" });
                // IMPORTANT: Reassign the worksheet variable with the result.
                return await _filteringService.FilterDataSheetByPostingCodeAsync(worksheet, columnMap["Posting Code"], new HashSet<string>(postingCodes, StringComparer.OrdinalIgnoreCase), cancellationToken);
            }

            // If no filtering was applied, return the original worksheet.
            return worksheet;
        }

        /// <summary>
        /// Refreshes pivot tables if the report type is one that uses them.
        /// </summary>
        private async Task RefreshPivotsIfNeededAsync(ReportType reportType, ExcelPackage package, CancellationToken cancellationToken)
        {
            if (reportType is ReportType.Monthly or ReportType.Quarterly or ReportType.Annual or ReportType.Custom)
            {
                _statusManager.Post("Setting pivot tables to refresh...", MessageType.InProgress);
                // Get pivot table names from configuration.
                string monthlyOrderPivotSheetName = _configuration.GetValue<string>("OperationalParameters:ExcelSheetNames:MonthlyOrderPivotSheet", "OrderPivot")!;
                string monthlyEstimatePivotSheetName = _configuration.GetValue<string>("OperationalParameters:ExcelSheetNames:MonthlyEstimatePivotSheet", "Estimate Success PivotTable")!;
                string monthlyOrderPivotName = _configuration.GetValue<string>("OperationalParameters:PivotTableNames:MonthlyOrderPivot", "PivotTable1")!;
                string monthlyEstimatePivotName = _configuration.GetValue<string>("OperationalParameters:PivotTableNames:MonthlyEstimatePivot", "PivotTable3")!;

                // Refresh both pivot tables.
                await Task.Run(() =>
                {
                    RefreshPivotTable(package, monthlyOrderPivotSheetName, monthlyOrderPivotName);
                    RefreshPivotTable(package, monthlyEstimatePivotSheetName, monthlyEstimatePivotName);
                }, cancellationToken);
            }
        }

        #region Unchanged Private Helpers

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

        private void RefreshPivotTable(ExcelPackage package, string sheetName, string pivotTableName)
        {
            ExcelWorksheet? worksheet = package.Workbook.Worksheets[sheetName];
            if (worksheet == null) return;
            ExcelPivotTable? pivotTable = worksheet.PivotTables.FirstOrDefault(pt => pt.Name.Equals(pivotTableName, StringComparison.OrdinalIgnoreCase));
            if (pivotTable != null)
            {
                pivotTable.CacheDefinition.Refresh();
            }
        }

        private async Task RenameFileWithRetryAsync(string sourcePath, string destinationPath, CancellationToken cancellationToken)
        {
            int maxRetries = _configuration.GetValue<int>("OperationalParameters:GeneralFileOperationMaxRetries", 5);
            int initialDelayMs = _configuration.GetValue<int>("OperationalParameters:GeneralFileOperationDelayMs", 500);
            int currentDelayMs = initialDelayMs;
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
                    return;
                }
                catch (IOException ex) when (i < maxRetries - 1)
                {
                    Logger.LogWarning($"Attempt {i + 1}/{maxRetries} failed to move file: {ex.Message}. Retrying in {currentDelayMs}ms...");
                    await Task.Delay(currentDelayMs, cancellationToken);
                    currentDelayMs *= 2;
                }
            }
            throw new IOException($"Failed to move file '{sourcePath}' to '{destinationPath}' after {maxRetries} attempts.");
        }

        #endregion

        #endregion
    }
    #endregion
}