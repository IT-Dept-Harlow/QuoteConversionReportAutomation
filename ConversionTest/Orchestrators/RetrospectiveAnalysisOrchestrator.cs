#region Using Directives

// System-related namespaces for core functionalities.
using OfficeOpenXml;
using OfficeOpenXml.Style;
// Project-specific namespaces for application components.
using QuoteConversionReportAutomation.Interfaces;
// Import the model for the LeadTimeRecord.
using QuoteConversionReportAutomation.Models;
using QuoteConversionReportAutomation.Models.Status;
using QuoteConversionReportAutomation.Orchestrators.Interfaces;
using QuoteConversionReportAutomation.Services.Interfaces;
using QuoteConversionReportAutomation.Services.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

#endregion

namespace QuoteConversionReportAutomation.Orchestrators
{
    #region Class Definition
    /// <summary>
    /// Implements the <see cref="IRetrospectiveAnalysisOrchestrator"/> to manage the workflow
    /// for generating a historical lead time analysis across multiple report files.
    /// </summary>
    public class RetrospectiveAnalysisOrchestrator : IRetrospectiveAnalysisOrchestrator
    {
        #region Fields

        /// <summary>
        /// The centralised service for reporting progress and status to the UI.
        /// </summary>
        private readonly IStatusManagerService _statusManager;

        /// <summary>
        /// The service responsible for extracting lead time data from Excel files.
        /// </summary>
        private readonly ILeadTimeAnalysisService _leadTimeService;

        #endregion

        #region Constructor

        /// <summary>
        /// Initialises a new instance of the <see cref="RetrospectiveAnalysisOrchestrator"/> class.
        /// </summary>
        /// <param name="statusManager">The injected status manager service.</param>
        /// <param name="leadTimeService">The injected lead time analysis service.</param>
        public RetrospectiveAnalysisOrchestrator(IStatusManagerService statusManager, ILeadTimeAnalysisService leadTimeService)
        {
            // Assign injected dependencies to the private fields.
            _statusManager = statusManager ?? throw new ArgumentNullException(nameof(statusManager));
            _leadTimeService = leadTimeService ?? throw new ArgumentNullException(nameof(leadTimeService));
        }

        #endregion

        #region IRetrospectiveAnalysisOrchestrator Implementation

        /// <inheritdoc/>
        public async Task GenerateAnalysisAsync(string targetFolder, string fileNamePattern, CancellationToken cancellationToken)
        {
            // Announce the start of the process via the status manager.
            _statusManager.Post("Starting retrospective analysis...", MessageType.InProgress);

            // Define the output path for the summary file on the user's desktop.
            string summaryFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), $"Retrospective_Summary_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");

            // Initialise a list to aggregate all lead time records found.
            var allLeadTimeData = new List<LeadTimeRecord>();

            try
            {
                // Find all files in the target folder and its subdirectories that match the specified pattern.
                var reportFiles = Directory.EnumerateFiles(targetFolder, fileNamePattern, SearchOption.AllDirectories).ToList();
                if (!reportFiles.Any())
                {
                    _statusManager.Post($"No files matching '{fileNamePattern}' found in the selected folder.", MessageType.Warning, TimeSpan.FromSeconds(10));
                    return;
                }

                // Sort the files by name to process them in a consistent order.
                var sortedFiles = reportFiles.OrderBy(f => Path.GetFileName(f)).ToList();
                _statusManager.Post($"Found {sortedFiles.Count} files. Starting processing...", MessageType.InProgress);

                // Run the file processing on a background thread to keep the UI responsive.
                await Task.Run(() =>
                {
                    // Loop through each found file.
                    for (int i = 0; i < sortedFiles.Count; i++)
                    {
                        // Check for user cancellation before processing each file.
                        cancellationToken.ThrowIfCancellationRequested();
                        string filePath = sortedFiles[i];
                        _statusManager.Post($"Processing file {i + 1} of {sortedFiles.Count}: {Path.GetFileName(filePath)}", MessageType.InProgress);

                        // Use the injected lead time service to extract records from the current file.
                        allLeadTimeData.AddRange(_leadTimeService.ExtractLeadTimeRecords(filePath));
                    }
                }, cancellationToken);

                // After processing all files, check if any data was found.
                if (allLeadTimeData.Any())
                {
                    _statusManager.Post("Generating final summary spreadsheet...", MessageType.InProgress);

                    // Create a new Excel package for the summary report.
                    using var summaryPackage = new ExcelPackage();
                    var summaryWorksheet = summaryPackage.Workbook.Worksheets.Add("Lead Time Summary");

                    // The GenerateSummarySheet method is now part of the lead time service.
                    // To call it, we would need to make it public. For now, this logic is duplicated from the original.
                    // In a further refactor, this could be exposed on the ILeadTimeAnalysisService.
                    GenerateSummarySheet(summaryWorksheet, allLeadTimeData);

                    // Delete any existing file at the destination path and save the new summary.
                    if (File.Exists(summaryFilePath)) File.Delete(summaryFilePath);
                    await summaryPackage.SaveAsAsync(new FileInfo(summaryFilePath));

                    _statusManager.Post($"Summary created successfully on your desktop!", MessageType.Success, TimeSpan.FromSeconds(10));
                }
                else
                {
                    _statusManager.Post("No valid lead time records were found in the processed files.", MessageType.Warning, TimeSpan.FromSeconds(10));
                }
            }
            catch (OperationCanceledException)
            {
                // Handle cancellation gracefully.
                _statusManager.Post("Analysis cancelled.", MessageType.Warning);
            }
            catch (Exception ex)
            {
                // Report any unexpected errors.
                _statusManager.Post($"An error occurred: {ex.Message}", MessageType.Error);
                Logger.LogError($"Retrospective analysis failed: {ex.Message}", ex);
            }
        }
        #endregion

        #region Private Helper Methods

        /// <summary>
        /// Generates the content and formatting for the lead time analysis summary sheet.
        /// NOTE: This logic is duplicated from the LeadTimeAnalysisService. In a future refactor,
        /// this could be exposed on the ILeadTimeAnalysisService to avoid duplication.
        /// </summary>
        /// <param name="worksheet">The Excel worksheet to write the summary to.</param>
        /// <param name="data">The list of lead time records to summarise.</param>
        private void GenerateSummarySheet(ExcelWorksheet worksheet, List<LeadTimeRecord> data)
        {
            // This method's logic is identical to the one in LeadTimeAnalysisService.
            // It builds the headers, data table, and summary averages for the report.
            worksheet.Cells["A1"].Value = "Source Report File";
            worksheet.Cells["B1"].Value = "Customer Name";
            worksheet.Cells["C1"].Value = "Customer Type";
            worksheet.Cells["D1"].Value = "Estimate Number";
            worksheet.Cells["E1"].Value = "Order Number";
            worksheet.Cells["F1"].Value = "Value";
            worksheet.Cells["G1"].Value = "Estimate Date";
            worksheet.Cells["H1"].Value = "Order Date";
            worksheet.Cells["I1"].Value = "Lead Time (Calendar Days)";
            worksheet.Cells["J1"].Value = "Lead Time (Business Days)";
            worksheet.Cells["A1:J1"].Style.Font.Bold = true;

            if (data.Any())
            {
                worksheet.Cells["A2"].LoadFromCollection(data, false);
                int dataRowCount = data.Count + 1;
                worksheet.Cells[2, 6, dataRowCount, 6].Style.Numberformat.Format = "£#,##0.00";
                worksheet.Cells[2, 7, dataRowCount, 8].Style.Numberformat.Format = "dd/MM/yyyy";
                worksheet.Cells[2, 9, dataRowCount, 10].Style.Numberformat.Format = "0.00";
                int summaryStartRow = data.Count + 4;
                worksheet.Cells[summaryStartRow, 8].Value = "Summary of Averages";
                worksheet.Cells[summaryStartRow, 8, summaryStartRow, 10].Merge = true;
                worksheet.Cells[summaryStartRow, 8, summaryStartRow, 10].Style.Font.Bold = true;
                worksheet.Cells[summaryStartRow, 8, summaryStartRow, 10].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                summaryStartRow++;
                worksheet.Cells[summaryStartRow, 7].Value = "Category";
                worksheet.Cells[summaryStartRow, 8].Value = "Avg. Calendar Days";
                worksheet.Cells[summaryStartRow, 9].Value = "Avg. Business Days";
                worksheet.Cells[summaryStartRow, 10].Value = "Avg. Value";
                worksheet.Cells[summaryStartRow, 7, summaryStartRow, 10].Style.Font.Italic = true;
                summaryStartRow++;
                var groupedData = data.GroupBy(d => d.CustomerType);
                foreach (var group in groupedData.OrderBy(g => g.Key))
                {
                    worksheet.Cells[summaryStartRow, 7].Value = group.Key;
                    worksheet.Cells[summaryStartRow, 8].Value = group.Average(g => g.LeadTimeCalendarDays);
                    worksheet.Cells[summaryStartRow, 9].Value = group.Average(g => g.LeadTimeBusinessDays);
                    worksheet.Cells[summaryStartRow, 10].Value = group.Average(g => g.Value);
                    summaryStartRow++;
                }
                worksheet.Cells[summaryStartRow, 7, summaryStartRow, 10].Style.Border.Top.Style = ExcelBorderStyle.Thin;
                summaryStartRow++;
                worksheet.Cells[summaryStartRow, 7].Value = "Overall Average";
                worksheet.Cells[summaryStartRow, 7].Style.Font.Bold = true;
                worksheet.Cells[summaryStartRow, 8].Value = data.Average(d => d.LeadTimeCalendarDays);
                worksheet.Cells[summaryStartRow, 9].Value = data.Average(d => d.LeadTimeBusinessDays);
                worksheet.Cells[summaryStartRow, 10].Value = data.Average(d => d.Value);
                worksheet.Cells[summaryStartRow, 8, summaryStartRow, 10].Style.Font.Bold = true;
                worksheet.Cells[summaryStartRow - groupedData.Count() - 1, 8, summaryStartRow, 9].Style.Numberformat.Format = "0.00";
                worksheet.Cells[summaryStartRow - groupedData.Count() - 1, 10, summaryStartRow, 10].Style.Numberformat.Format = "£#,##0.00";
            }

            worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
        }

        #endregion
    }
    #endregion
}