#region Using Directives

// System-related namespaces for core functionalities.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

// Third-party namespaces for external libraries.
using OfficeOpenXml;
using OfficeOpenXml.Style;

// Project-specific namespaces for application components.
using QuoteConversionReportAutomation.Helpers;
using QuoteConversionReportAutomation.Models;
using QuoteConversionReportAutomation.Services.Interfaces;
using QuoteConversionReportAutomation.Services.Logging;

#endregion

namespace QuoteConversionReportAutomation.Services.Excel
{
    #region Class Definition
    /// <summary>
    /// Implements the <see cref="IExcelAnalysisService"/> to provide the logic for creating
    /// the main "Analysis" worksheet in a processed report. This service encapsulates
    /// extracting unique data, writing formulae, and formatting the analysis results.
    /// </summary>
    public class ExcelAnalysisService : IExcelAnalysisService
    {
        #region Fields

        /// <summary>
        /// Provides business logic for financial year calculations.
        /// </summary>
        private readonly IFinancialYearService _financialYearService;

        #endregion

        #region Constructor

        /// <summary>
        /// Initialises a new instance of the <see cref="ExcelAnalysisService"/> class.
        /// </summary>
        /// <param name="financialYearService">
        /// The service responsible for financial year calculations, injected via dependency injection.
        /// </param>
        public ExcelAnalysisService(IFinancialYearService financialYearService)
        {
            _financialYearService = financialYearService ?? throw new ArgumentNullException(nameof(financialYearService));
            Logger.LogTrace("ExcelAnalysisService instance created.");
        }

        #endregion

        #region IExcelAnalysisService Implementation

        /// <inheritdoc/>
        public async Task CreateAnalysisSheetAsync(
            ExcelWorksheet dataSheet,
            ExcelWorksheet analysisSheet,
            Dictionary<string, int> dataColumnMap,
            Dictionary<string, int> analysisColumnMap,
            DateTime reportDate,
            string sourceFileName,
            CancellationToken cancellationToken)
        {
            // This public method orchestrates the private helper methods to build the analysis sheet.
            
            // 1. Extract unique customers from the 'DATA' sheet and populate them into the 'Analysis' sheet,
            //    along with all required metadata and formulae.
            await ExtractUniqueCustomersAndWriteFormulasAsync(
                dataSheet, 
                analysisSheet, 
                dataColumnMap, 
                analysisColumnMap, 
                reportDate, 
                sourceFileName, 
                cancellationToken);

            // 2. Trigger the calculation of all formulae in the workbook.
            //    Note: EPPlus has limitations; complex formulae may still require a manual refresh in Excel.
            await Task.Run(() => CalculateWorkbook(analysisSheet.Workbook), cancellationToken);

            // 3. Clean up any unused template rows below the last populated customer in the 'Analysis' sheet.
            await Task.Run(() => ClearContentBelowLastCustomer(analysisSheet, analysisColumnMap), cancellationToken);
        }

        #endregion

        #region Private Helper Methods

        /// <summary>
        /// Extracts unique customer and posting code pairs from the 'DATA' sheet, populates them into the 'Analysis' sheet,
        /// and writes all necessary formulae and metadata for the analysis.
        /// </summary>
        private async Task ExtractUniqueCustomersAndWriteFormulasAsync(
            ExcelWorksheet dataSheet,
            ExcelWorksheet analysisSheet,
            Dictionary<string, int> dataColumnMap,
            Dictionary<string, int> analysisColumnMap,
            DateTime reportDate,
            string originalSourceFilePath,
            CancellationToken cancellationToken)
        {
            // --- 1. Get Column Indices from the provided maps for clarity ---
            int customerColData = dataColumnMap["Customer"];
            int postingCodeColData = dataColumnMap["Posting Code"];
            int repColData = dataColumnMap["Rep"];
            int orderedColData = dataColumnMap["Ordered"];
            int priceColData = dataColumnMap["Price"];

            int customerColAnalysis = analysisColumnMap["Customer"];
            int postingCodeColAnalysis = analysisColumnMap["Posting Code"];
            int contractStatusColAnalysis = analysisColumnMap["Contract Status"];
            int repColAnalysis = analysisColumnMap["Rep"];
            int numEstimatesColAnalysis = analysisColumnMap["Number of Estimates"];
            int estimatesWonColAnalysis = analysisColumnMap["Estimates Won"];
            int estimatesNotWonColAnalysis = analysisColumnMap["Estimates Not Won"];
            int winPercentColAnalysis = analysisColumnMap["% Win"];
            int valueOfEstimatesColAnalysis = analysisColumnMap["Value of Estimates"];
            int valueWonColAnalysis = analysisColumnMap["Value of Estimates Won"];
            int valueNotConfirmedColAnalysis = analysisColumnMap["Value of Est Not Confirmed"];
            int valueWonPercentColAnalysis = analysisColumnMap["Value Won %"];
            int sourceFileColAnalysis = analysisColumnMap["SOURCE FILE"];
            int dateColAnalysis = analysisColumnMap["CONVERTED DATE"];
            int finYearColAnalysis = analysisColumnMap["FY"];

            // --- 2. Define Sheet Structure Constants ---
            const int analysisStartRow = 6;
            int dataSheetRowCount = dataSheet.Dimension?.Rows ?? 0;
            const int dataRangeSize = 50000; // A large number to ensure formulae cover the entire possible data range.

            // --- 3. Prepare Metadata Values ---
            string sourceFileNameForAnalysis = Path.GetFileName(originalSourceFilePath);
            string currentFY = _financialYearService.GetCurrentFinancialYear(false);

            // --- 4. Extract Unique Data on a background thread ---
            List<(string CustomerName, string PostingCode, string Rep)> uniqueCustomerData = await Task.Run(() =>
            {
                // Use a dictionary to efficiently find unique pairs of (CustomerName, PostingCode)
                // and store their associated Rep.
                var customerDataDict = new Dictionary<(string, string), string>();
                if (dataSheetRowCount >= 2) // Ensure there is data beyond the header row.
                {
                    for (int row = 2; row <= dataSheetRowCount; row++)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        string? customerName = dataSheet.Cells[row, customerColData].Value?.ToString()?.Trim();
                        string? postingCode = dataSheet.Cells[row, postingCodeColData].Value?.ToString()?.Trim();

                        // Only process rows with valid customer and posting code information.
                        if (!string.IsNullOrWhiteSpace(customerName) && !string.IsNullOrWhiteSpace(postingCode))
                        {
                            var key = (customerName, postingCode);
                            // If this unique pair has not been seen before, add it with its Rep.
                            if (!customerDataDict.ContainsKey(key))
                            {
                                string rep = dataSheet.Cells[row, repColData].Value?.ToString()?.Trim() ?? "Not Found";
                                customerDataDict.Add(key, rep);
                            }
                        }
                    }
                }
                // Convert the dictionary to a list and sort it for ordered output in the Analysis sheet.
                return customerDataDict.Select(kvp => (kvp.Key.Item1, kvp.Key.Item2, kvp.Value))
                                     .OrderBy(d => d.Item1).ThenBy(d => d.Item2).ToList();
            }, cancellationToken);

            Logger.LogInfo($"Found {uniqueCustomerData.Count} unique customer/posting code pairs.");

            // --- 5. Pre-clear the data area of the Analysis Sheet ---
            if (analysisSheet.Dimension != null && analysisSheet.Dimension.Rows >= analysisStartRow)
            {
                analysisSheet.Cells[analysisStartRow, 1, analysisSheet.Dimension.End.Row, analysisSheet.Dimension.End.Column].Clear();
            }

            // --- 6. Populate the Analysis Sheet with Data, Formulae, and Formatting ---
            for (int i = 0; i < uniqueCustomerData.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var (customerName, postingCode, rep) = uniqueCustomerData[i];
                int targetRow = analysisStartRow + i;

                // Populate static data and metadata columns.
                analysisSheet.Cells[targetRow, customerColAnalysis].Value = customerName;
                analysisSheet.Cells[targetRow, postingCodeColAnalysis].Value = postingCode;
                analysisSheet.Cells[targetRow, repColAnalysis].Value = rep;
                analysisSheet.Cells[targetRow, dateColAnalysis].Value = reportDate.Date;
                analysisSheet.Cells[targetRow, finYearColAnalysis].Value = currentFY;
                analysisSheet.Cells[targetRow, sourceFileColAnalysis].Value = sourceFileNameForAnalysis;

                // --- Build and Write Formulae ---
                // Create cell addresses for use in the formulae, making them dynamic to the current row.
                string customerAddress = $"Analysis!{ExcelCellAddress.GetColumnLetter(customerColAnalysis)}{targetRow}";
                string postingCodeAddress = $"Analysis!{ExcelCellAddress.GetColumnLetter(postingCodeColAnalysis)}{targetRow}";

                // Formula for 'Contract Status': Checks if the customer name contains "NON-CONTRACT".
                string contractStatusFormula = $"IF(ISNUMBER(SEARCH(\"NON-CONTRACT\",{customerAddress})),\"NON-CONTRACT\",\"CONTRACT\")";
                analysisSheet.Cells[targetRow, contractStatusColAnalysis].Formula = contractStatusFormula;
                
                // Define the data ranges in the 'DATA' sheet that the formulae will reference.
                string customerCritRange = $"DATA!${ExcelCellAddress.GetColumnLetter(customerColData)}$2:${ExcelCellAddress.GetColumnLetter(customerColData)}${dataRangeSize}";
                string postingCodeCritRange = $"DATA!${ExcelCellAddress.GetColumnLetter(postingCodeColData)}$2:${ExcelCellAddress.GetColumnLetter(postingCodeColData)}${dataRangeSize}";
                string orderedCritRange = $"DATA!${ExcelCellAddress.GetColumnLetter(orderedColData)}$2:${ExcelCellAddress.GetColumnLetter(orderedColData)}${dataRangeSize}";
                string priceSumRange = $"DATA!${ExcelCellAddress.GetColumnLetter(priceColData)}$2:${ExcelCellAddress.GetColumnLetter(priceColData)}${dataRangeSize}";
                
                // Write the COUNTIFS and SUMIFS formulae for the main analysis calculations.
                analysisSheet.Cells[targetRow, numEstimatesColAnalysis].Formula = $"COUNTIFS({customerCritRange},{customerAddress},{postingCodeCritRange},{postingCodeAddress},{orderedCritRange},\"<>Superseded\")";
                analysisSheet.Cells[targetRow, estimatesWonColAnalysis].Formula = $"COUNTIFS({customerCritRange},{customerAddress},{postingCodeCritRange},{postingCodeAddress},{orderedCritRange},\"Yes\")";
                analysisSheet.Cells[targetRow, estimatesNotWonColAnalysis].Formula = $"COUNTIFS({customerCritRange},{customerAddress},{postingCodeCritRange},{postingCodeAddress},{orderedCritRange},\"No\")";
                analysisSheet.Cells[targetRow, winPercentColAnalysis].Formula = $"IF({ExcelCellAddress.GetColumnLetter(numEstimatesColAnalysis)}{targetRow}>0,{ExcelCellAddress.GetColumnLetter(estimatesWonColAnalysis)}{targetRow}/{ExcelCellAddress.GetColumnLetter(numEstimatesColAnalysis)}{targetRow},0)";
                analysisSheet.Cells[targetRow, valueOfEstimatesColAnalysis].Formula = $"SUMIFS({priceSumRange},{customerCritRange},{customerAddress},{postingCodeCritRange},{postingCodeAddress},{orderedCritRange},\"<>Superseded\")";
                analysisSheet.Cells[targetRow, valueWonColAnalysis].Formula = $"SUMIFS({priceSumRange},{customerCritRange},{customerAddress},{postingCodeCritRange},{postingCodeAddress},{orderedCritRange},\"Yes\")";
                analysisSheet.Cells[targetRow, valueNotConfirmedColAnalysis].Formula = $"SUMIFS({priceSumRange},{customerCritRange},{customerAddress},{postingCodeCritRange},{postingCodeAddress},{orderedCritRange},\"No\")";
                analysisSheet.Cells[targetRow, valueWonPercentColAnalysis].Formula = $"IF({ExcelCellAddress.GetColumnLetter(valueOfEstimatesColAnalysis)}{targetRow}>0,{ExcelCellAddress.GetColumnLetter(valueWonColAnalysis)}{targetRow}/{ExcelCellAddress.GetColumnLetter(valueOfEstimatesColAnalysis)}{targetRow},0)";
            }

            // --- 7. Apply Number Formatting to the newly populated data columns ---
            if (uniqueCustomerData.Any())
            {
                int endRow = analysisStartRow + uniqueCustomerData.Count - 1;
                // Date format
                analysisSheet.Cells[analysisStartRow, dateColAnalysis, endRow, dateColAnalysis].Style.Numberformat.Format = "dd/MM/yyyy";
                // Currency format
                analysisSheet.Cells[analysisStartRow, valueOfEstimatesColAnalysis, endRow, valueNotConfirmedColAnalysis].Style.Numberformat.Format = "£#,##0.00";
                // Percentage format
                analysisSheet.Cells[analysisStartRow, winPercentColAnalysis, endRow, winPercentColAnalysis].Style.Numberformat.Format = "0%";
                analysisSheet.Cells[analysisStartRow, valueWonPercentColAnalysis, endRow, valueWonPercentColAnalysis].Style.Numberformat.Format = "0%";
                
                // --- 8. Apply alternating row colour formatting ---
                ApplyAlternatingRowFormatting(analysisSheet, analysisStartRow, endRow);
            }
        }

        /// <summary>
        /// Triggers the calculation of all formulae in the workbook.
        /// </summary>
        private void CalculateWorkbook(ExcelWorkbook workbook)
        {
            try
            {
                workbook.Calculate();
                Logger.LogInfo("Workbook formula calculation triggered successfully.");
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"Error during Excel workbook calculation: {ex.Message}. Manual refresh in Excel may be required.", ex);
            }
        }

        /// <summary>
        /// Clears content from rows in the 'Analysis' sheet that are below the last row containing data.
        /// This is used to clean up any placeholder rows from the original template.
        /// </summary>
        private void ClearContentBelowLastCustomer(ExcelWorksheet worksheet, Dictionary<string, int> analysisColumnMap)
        {
            if (worksheet.Dimension == null) return;
            
            // Get the column index to check for data.
            int customerNameColIdx = analysisColumnMap["Customer"];
            const int customerDataStartRow = 6;
            int lastActualDataRow = customerDataStartRow - 1;

            // Find the last row that actually contains a customer name.
            for (int r = worksheet.Dimension.End.Row; r >= customerDataStartRow; r--)
            {
                if (worksheet.Cells[r, customerNameColIdx].Value != null && !string.IsNullOrWhiteSpace(worksheet.Cells[r, customerNameColIdx].Value.ToString()))
                {
                    lastActualDataRow = r;
                    break;
                }
            }

            // Determine the starting row for clearing content.
            int startClearTargetRow = Math.Max(lastActualDataRow + 1, customerDataStartRow);

            // If there are rows to clear, clear their content.
            if (startClearTargetRow <= worksheet.Dimension.End.Row)
            {
                worksheet.Cells[startClearTargetRow, 1, worksheet.Dimension.End.Row, worksheet.Dimension.End.Column].Clear();
            }
        }

        /// <summary>
        /// Applies alternating row colours (banded rows) to the data range of the Analysis sheet
        /// for improved readability, using Excel's conditional formatting.
        /// </summary>
        /// <param name="worksheet">The 'Analysis' worksheet to apply formatting to.</param>
        /// <param name="startDataRow">The first row of data to be formatted (1-based index).</param>
        /// <param name="endDataRow">The last row of data to be formatted (1-based index).</param>
        private void ApplyAlternatingRowFormatting(ExcelWorksheet worksheet, int startDataRow, int endDataRow)
        {
            // Exit if there is no data range to format.
            if (worksheet.Dimension == null || endDataRow < startDataRow)
            {
                return;
            }

            // Define the full address range for the data table.
            var dataRangeAddress = new ExcelAddress(startDataRow, 1, endDataRow, worksheet.Dimension.End.Column).Address;

            // Create a new conditional formatting rule.
            // The formula =MOD(ROW(),2)=0 checks if the row number is even.
            var conditionalFormatting = worksheet.ConditionalFormatting.AddExpression(dataRangeAddress);
            conditionalFormatting.Formula = "MOD(ROW(),2)=0";

            // Define the style to be applied when the condition is true (i.e., for even rows).
            // This sets a light grey background fill.
            conditionalFormatting.Style.Fill.PatternType = ExcelFillStyle.Solid;
            conditionalFormatting.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(215, 215, 215)); // A light grey color

            Logger.LogDebug($"Applied alternating row formatting to range '{dataRangeAddress}' in sheet '{worksheet.Name}'.");
        }

        #endregion
    }
    #endregion
}