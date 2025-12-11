#region Using Directives

// System-related namespaces for core functionalities.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Globalization;

// Third-party namespaces for external libraries.
using OfficeOpenXml;
using OfficeOpenXml.Style;

// Project-specific namespaces for application components.
using QuoteConversionReportAutomation.Helpers;
using QuoteConversionReportAutomation.Interfaces;
using QuoteConversionReportAutomation.Services.Interfaces;
using QuoteConversionReportAutomation.Models.Status;
using QuoteConversionReportAutomation.Models;

#endregion

namespace QuoteConversionReportAutomation.Services.Excel
{
    #region Class Definition
    /// <summary>
    /// Implements the <see cref="ILeadTimeAnalysisService"/> to provide the logic for creating
    /// the "Lead Time Analysis" worksheet and extracting lead time data from reports.
    /// </summary>
    public class LeadTimeAnalysisService : ILeadTimeAnalysisService
    {
        #region Fields
        /// <summary>
        /// Provides access to the application's status manager for progress reporting.
        /// </summary>
        private readonly IStatusManagerService _statusManager;
        #endregion

        #region Constructor
        /// <summary>
        /// Initialises a new instance of the <see cref="LeadTimeAnalysisService"/> class.
        /// </summary>
        /// <param name="statusManager">The centralised service for status reporting.</param>
        public LeadTimeAnalysisService(IStatusManagerService statusManager)
        {
            _statusManager = statusManager ?? throw new ArgumentNullException(nameof(statusManager));
        }
        #endregion

        #region ILeadTimeAnalysisService Implementation

        /// <inheritdoc/>
        public async Task CreateLeadTimeAnalysisSheetAsync(ExcelPackage package, string sourceDataSheetName, CancellationToken cancellationToken)
        {
            // Define the name for the new worksheet.
            const string newSheetName = "Lead Time Analysis";
            _statusManager.Post($"Creating '{newSheetName}' sheet...", MessageType.InProgress);

            // Asynchronously extract the lead time records from the 'DATA' sheet within the provided package.
            var leadTimeEntries = await Task.Run(() => ExtractLeadTimeRecordsFromPackage(package, sourceDataSheetName), cancellationToken);

            // Create the new worksheet within the workbook.
            var analysisSheet = package.Workbook.Worksheets.Add(newSheetName);

            // Generate the content of the new summary sheet using the extracted data.
            GenerateSummarySheet(analysisSheet, leadTimeEntries);

            _statusManager.Post($"'{newSheetName}' sheet created successfully.", MessageType.Success);
        }

        /// <inheritdoc/>
        public List<LeadTimeRecord> ExtractLeadTimeRecords(string filePath)
        {
            // This is a public entry point for external services like the RetrospectiveAnalyser.
            // It opens the Excel file from the given path and calls the internal extraction logic.
            using var package = new ExcelPackage(new FileInfo(filePath));
            return ExtractLeadTimeRecordsFromPackage(package, Path.GetFileName(filePath));
        }

        #endregion

        #region Private Helper Methods

        /// <summary>
        /// Extracts lead time records from a given worksheet within an Excel package.
        /// </summary>
        /// <param name="package">The EPPlus ExcelPackage object to read from.</param>
        /// <param name="sourceFileName">The name of the original file, used for the SourceFile property in the record.</param>
        /// <returns>A list of <see cref="LeadTimeRecord"/> objects.</returns>
        private List<LeadTimeRecord> ExtractLeadTimeRecordsFromPackage(ExcelPackage package, string sourceFileName)
        {
            var records = new List<LeadTimeRecord>();
            // Target the "DATA" sheet for raw information.
            var dataSheet = package.Workbook.Worksheets["DATA"];
            if (dataSheet == null || dataSheet.Dimension == null) return records;

            // Use the static helper to map required columns by their header names for robustness.
            var columnMap = ExcelHelper.MapColumnIndices(dataSheet, 1, new[] { "Customer", "Estimate No.", "Date", "Price", "Order Date", "Job No" });
            int endRow = dataSheet.Dimension.End.Row;

            // Iterate through each data row (starting from row 2).
            for (int row = 2; row <= endRow; row++)
            {
                var orderDateValue = dataSheet.Cells[row, columnMap["Order Date"]].Value;
                var estimateDateValue = dataSheet.Cells[row, columnMap["Date"]].Value;

                // Process the row only if it has both a valid estimate date and a valid order date.
                if (TryGetDateTime(orderDateValue, out DateTime orderDate) && TryGetDateTime(estimateDateValue, out DateTime estimateDate))
                {
                    // Calculate the calendar lead time.
                    double leadTimeDays = (orderDate - estimateDate).TotalDays;

                    // Only include records with a non-negative lead time.
                    if (leadTimeDays >= 0)
                    {
                        // Extract all required data fields from the row.
                        string customerName = dataSheet.Cells[row, columnMap["Customer"]].Value?.ToString() ?? "N/A";
                        decimal.TryParse(dataSheet.Cells[row, columnMap["Price"]].Value?.ToString(), out decimal value);

                        // Create and add a new LeadTimeRecord to the list.
                        records.Add(new LeadTimeRecord(
                            sourceFileName,
                            customerName,
                            GetCustomerType(customerName),
                            dataSheet.Cells[row, columnMap["Estimate No."]].Value?.ToString() ?? "N/A",
                            dataSheet.Cells[row, columnMap["Job No"]].Value?.ToString() ?? "N/A",
                            value,
                            estimateDate,
                            orderDate,
                            leadTimeDays,
                            CalculateBusinessDays(estimateDate, orderDate)
                        ));
                    }
                }
            }
            return records;
        }

        /// <summary>
        /// Generates the content and formatting for the lead time analysis summary sheet.
        /// </summary>
        /// <param name="worksheet">The Excel worksheet to write the summary to.</param>
        /// <param name="data">The list of lead time records to summarise.</param>
        public void GenerateSummarySheet(ExcelWorksheet worksheet, List<LeadTimeRecord> data)
        {
            // 1. Write Headers for the raw data section and make them bold.
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
                // 2. Write the main data body from the collection, starting at cell A2.
                worksheet.Cells["A2"].LoadFromCollection(data, false);

                // 3. Apply appropriate number formatting to the data columns.
                int dataRowCount = data.Count + 1;
                worksheet.Cells[2, 6, dataRowCount, 6].Style.Numberformat.Format = "£#,##0.00"; // Currency
                worksheet.Cells[2, 7, dataRowCount, 8].Style.Numberformat.Format = "dd/MM/yyyy"; // Date
                worksheet.Cells[2, 9, dataRowCount, 10].Style.Numberformat.Format = "0.00"; // Number with 2 decimal places

                // 4. Generate the summary section with calculated averages.
                int summaryStartRow = data.Count + 4;
                worksheet.Cells[summaryStartRow, 8].Value = "Summary of Averages";
                worksheet.Cells[summaryStartRow, 8, summaryStartRow, 10].Merge = true;
                worksheet.Cells[summaryStartRow, 8, summaryStartRow, 10].Style.Font.Bold = true;
                worksheet.Cells[summaryStartRow, 8, summaryStartRow, 10].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                summaryStartRow++;

                // Add summary table headers.
                worksheet.Cells[summaryStartRow, 7].Value = "Category";
                worksheet.Cells[summaryStartRow, 8].Value = "Avg. Calendar Days";
                worksheet.Cells[summaryStartRow, 9].Value = "Avg. Business Days";
                worksheet.Cells[summaryStartRow, 10].Value = "Avg. Value";
                worksheet.Cells[summaryStartRow, 7, summaryStartRow, 10].Style.Font.Italic = true;
                summaryStartRow++;

                // Group data by customer type and calculate averages for each group.
                var groupedData = data.GroupBy(d => d.CustomerType);
                foreach (var group in groupedData.OrderBy(g => g.Key))
                {
                    worksheet.Cells[summaryStartRow, 7].Value = group.Key;
                    worksheet.Cells[summaryStartRow, 8].Value = group.Average(g => g.LeadTimeCalendarDays);
                    worksheet.Cells[summaryStartRow, 9].Value = group.Average(g => g.LeadTimeBusinessDays);
                    worksheet.Cells[summaryStartRow, 10].Value = group.Average(g => g.Value);
                    summaryStartRow++;
                }

                // Add a separator line.
                worksheet.Cells[summaryStartRow, 7, summaryStartRow, 10].Style.Border.Top.Style = ExcelBorderStyle.Thin;
                summaryStartRow++;

                // Add Overall Averages.
                worksheet.Cells[summaryStartRow, 7].Value = "Overall Average";
                worksheet.Cells[summaryStartRow, 7].Style.Font.Bold = true;
                worksheet.Cells[summaryStartRow, 8].Value = data.Average(d => d.LeadTimeCalendarDays);
                worksheet.Cells[summaryStartRow, 9].Value = data.Average(d => d.LeadTimeBusinessDays);
                worksheet.Cells[summaryStartRow, 10].Value = data.Average(d => d.Value);
                worksheet.Cells[summaryStartRow, 8, summaryStartRow, 10].Style.Font.Bold = true;

                // Format the summary numbers.
                worksheet.Cells[summaryStartRow - groupedData.Count() - 1, 8, summaryStartRow, 9].Style.Numberformat.Format = "0.00";
                worksheet.Cells[summaryStartRow - groupedData.Count() - 1, 10, summaryStartRow, 10].Style.Numberformat.Format = "£#,##0.00";
            }

            // Auto-fit all columns for better readability.
            worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
        }

        /// <summary>
        /// Calculates the number of business days between two dates, excluding weekends and bank holidays.
        /// </summary>
        /// <param name="startDate">The start date of the period.</param>
        /// <param name="endDate">The end date of the period.</param>
        /// <returns>The total number of business days.</returns>
        private int CalculateBusinessDays(DateTime startDate, DateTime endDate)
        {
            int businessDays = 0;
            // Iterate through each day in the period (excluding the end date itself).
            for (var date = startDate.Date; date < endDate.Date; date = date.AddDays(1))
            {
                // If the day is not a weekend or a bank holiday, increment the counter.
                if (date.DayOfWeek != DayOfWeek.Saturday &&
                    date.DayOfWeek != DayOfWeek.Sunday &&
                    !BankHolidayHelper.IsBankHoliday(date))
                {
                    businessDays++;
                }
            }
            return businessDays;
        }

        /// <summary>
        /// Parses the customer type (e.g., "contract") from the customer name string.
        /// </summary>
        /// <param name="customerName">The full customer name string (e.g., "NHS Trust (Contract)").</param>
        /// <returns>The parsed customer type, or "non-contract" if not found.</returns>
        private string GetCustomerType(string customerName)
        {
            if (string.IsNullOrWhiteSpace(customerName)) return "Unknown";
            // Use a regular expression to find text within the last pair of parentheses in the string.
            var match = Regex.Match(customerName, @"\(([^)]+)\)$");
            if (match.Success)
            {
                // Extract the captured group, trim it, and convert to lower case.
                string type = match.Groups[1].Value.Trim().ToLowerInvariant();
                // Normalise "contract-direct" to "contract" for consistent grouping.
                return type == "contract-direct" ? "contract" : type;
            }
            // If no match is found, assume it is a non-contract customer.
            return "non-contract";
        }

        /// <summary>
        /// Tries to convert a cell value from EPPlus into a DateTime object.
        /// It can handle native DateTime objects, doubles (OLE Automation Date), and standard string formats.
        /// </summary>
        /// <param name="excelCellValue">The value from the worksheet cell.</param>
        /// <param name="result">The resulting DateTime if conversion is successful.</param>
        /// <returns>True if conversion was successful, otherwise false.</returns>
        private bool TryGetDateTime(object? excelCellValue, out DateTime result)
        {
            result = DateTime.MinValue;
            if (excelCellValue == null) return false;

            // Case 1: The cell value is already a DateTime object.
            if (excelCellValue is DateTime dt)
            {
                result = dt;
                return true;
            }

            string dateString = excelCellValue.ToString()!.Trim();
            if (string.IsNullOrWhiteSpace(dateString)) return false;

            // Case 2: The cell value is a number (OLE Automation Date format used by Excel).
            if (double.TryParse(dateString, out double d) && d > 0)
            {
                result = DateTime.FromOADate(d);
                return true;
            }

            // Case 3: The cell value is a string in a common format.
            if (DateTime.TryParseExact(dateString, "dd/MM/yyyy", CultureInfo.GetCultureInfo("en-GB"), DateTimeStyles.None, out result)) return true;

            // Fallback to a general TryParse.
            return DateTime.TryParse(dateString, out result);
        }

        #endregion
    }
    #endregion
}