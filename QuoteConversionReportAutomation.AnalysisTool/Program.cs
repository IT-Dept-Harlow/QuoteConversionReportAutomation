using OfficeOpenXml;
using QuoteConversionReportAutomation.Helpers; // Required to access BankHolidayHelper
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace QuoteConversionReportAutomation.AnalysisTool
{
    /// <summary>
    /// A standalone console application to perform a retrospective analysis of historical
    /// report files to calculate lead times between estimate and order dates.
    /// </summary>
    internal class Program
    {
        #region Data Structures
        /// <summary>
        /// A simple class to hold the extracted data from each relevant row in a report file.
        /// </summary>
        public class LeadTimeRecord
        {
            public string SourceFile { get; }
            public string CustomerName { get; }
            public string CustomerType { get; }
            public string EstimateNumber { get; }
            public string OrderNumber { get; }
            public decimal Value { get; }
            public DateTime EstimateDate { get; }
            public DateTime OrderDate { get; }
            public double LeadTimeCalendarDays { get; }
            public int LeadTimeBusinessDays { get; }

            public LeadTimeRecord(string sourceFile, string customerName, string customerType, string estimateNumber, string orderNumber, decimal value, DateTime estimateDate, DateTime orderDate, double leadTimeDays, int leadTimeBusinessDays)
            {
                SourceFile = sourceFile;
                CustomerName = customerName;
                CustomerType = customerType;
                EstimateNumber = estimateNumber;
                OrderNumber = orderNumber;
                Value = value;
                EstimateDate = estimateDate;
                OrderDate = orderDate;
                LeadTimeCalendarDays = leadTimeDays;
                LeadTimeBusinessDays = leadTimeBusinessDays;
            }
        }
        #endregion

        #region Main Execution
        /// <summary>
        /// The main entry point for the analysis tool.
        /// </summary>
        static void Main(string[] args)
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("Starting Retrospective Lead Time Analysis...");
            Console.WriteLine("==============================================");

            string reportsRootDirectory = @"C:\Users\ChrisP\Harlow Printing\IT Projects - Documents\Dashboard Datasets\Raw_data\Quotes conversion\Estimates\Daily Reports (5day 500)";
            string summaryFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "Retrospective_Lead_Time_Summary.xlsx");

            ExcelPackage.License.SetNonCommercialPersonal("Harlow");
            BankHolidayHelper.Initialize();

            Console.WriteLine($"\nSearching for report files in: {reportsRootDirectory}");
            var reportFiles = Directory.EnumerateFiles(reportsRootDirectory, "*_Estimate_Success_Rate_Daily_5day_1k.xlsx", SearchOption.AllDirectories).ToList();

            if (!reportFiles.Any())
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("\nWarning: No report files found matching the pattern.");
                Console.ResetColor();
                Console.ReadKey();
                return;
            }

            var sortedReportFiles = reportFiles.OrderBy(f => Path.GetFileName(f)).ToList();
            Console.WriteLine($"Found {sortedReportFiles.Count} files to process. Processing in chronological order...");

            var allLeadTimeData = new List<LeadTimeRecord>();
            int filesProcessed = 0;

            foreach (var filePath in sortedReportFiles)
            {
                try
                {
                    Console.WriteLine($"  -> Processing: {Path.GetFileName(filePath)}");
                    allLeadTimeData.AddRange(ExtractLeadTimesFromFile(filePath));
                    filesProcessed++;
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"     --> ERROR processing file {Path.GetFileName(filePath)}: {ex.Message}");
                    Console.ResetColor();
                }
            }

            Console.WriteLine($"\nSuccessfully processed {filesProcessed} files and extracted {allLeadTimeData.Count} valid lead time records.");

            if (allLeadTimeData.Any())
            {
                Console.WriteLine($"Generating summary file at: {summaryFilePath}");
                try
                {
                    GenerateSummarySheet(allLeadTimeData, summaryFilePath);
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("\nAnalysis Complete! Summary spreadsheet created successfully on your desktop.");
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"\nFailed to generate summary spreadsheet: {ex.Message}");
                }
                finally
                {
                    Console.ResetColor();
                }
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("\nNo valid lead time records were found to create a summary.");
                Console.ResetColor();
            }

            Console.WriteLine("\nPress any key to exit.");
            Console.ReadKey();
        }
        #endregion

        #region Helper Methods
        private static List<LeadTimeRecord> ExtractLeadTimesFromFile(string filePath)
        {
            var records = new List<LeadTimeRecord>();
            using var package = new ExcelPackage(new FileInfo(filePath));
            var dataSheet = package.Workbook.Worksheets["DATA"];
            if (dataSheet == null || dataSheet.Dimension == null) return records;

            int endRow = dataSheet.Dimension.End.Row;
            const int CustomerColumnIndex = 1;
            const int EstimateNumberColumnIndex = 2;
            const int EstimateDateColumnIndex = 5;
            const int ValueColumnIndex = 6;
            const int OrderDateColumnIndex = 14;
            const int OrderNumberColumnIndex = 15;

            for (int row = 2; row <= endRow; row++)
            {
                var orderDateValue = dataSheet.Cells[row, OrderDateColumnIndex].Value;
                var estimateDateValue = dataSheet.Cells[row, EstimateDateColumnIndex].Value;

                if (orderDateValue != null && estimateDateValue != null)
                {
                    if (TryGetDateTime(orderDateValue, out DateTime orderDate) &&
                        TryGetDateTime(estimateDateValue, out DateTime estimateDate))
                    {
                        double leadTimeDays = (orderDate - estimateDate).TotalDays;
                        if (leadTimeDays >= 0)
                        {
                            string customerName = dataSheet.Cells[row, CustomerColumnIndex].Value?.ToString() ?? "N/A";
                            decimal.TryParse(dataSheet.Cells[row, ValueColumnIndex].Value?.ToString(), out decimal value);
                            records.Add(new LeadTimeRecord(
                                Path.GetFileName(filePath),
                                customerName,
                                GetCustomerType(customerName),
                                dataSheet.Cells[row, EstimateNumberColumnIndex].Value?.ToString() ?? "N/A",
                                dataSheet.Cells[row, OrderNumberColumnIndex].Value?.ToString() ?? "N/A",
                                value,
                                estimateDate,
                                orderDate,
                                leadTimeDays,
                                CalculateBusinessDays(estimateDate, orderDate)
                            ));
                        }
                    }
                }
            }
            return records;
        }

        private static void GenerateSummarySheet(List<LeadTimeRecord> data, string outputFilePath)
        {
            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Lead Time Summary");

            // Write Headers
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
                worksheet.Cells["A2"].LoadFromCollection(data);
                worksheet.Cells[2, 6, data.Count + 1, 6].Style.Numberformat.Format = "£#,##0.00";
                worksheet.Cells[2, 7, data.Count + 1, 8].Style.Numberformat.Format = "dd/MM/yyyy";
                worksheet.Cells[2, 9, data.Count + 1, 10].Style.Numberformat.Format = "0.00";

                // --- Generate Summary Section ---
                int summaryStartRow = data.Count + 4;
                worksheet.Cells[summaryStartRow, 8].Value = "Summary of Averages";
                worksheet.Cells[summaryStartRow, 8, summaryStartRow, 10].Merge = true;
                worksheet.Cells[summaryStartRow, 8, summaryStartRow, 10].Style.Font.Bold = true;
                worksheet.Cells[summaryStartRow, 8, summaryStartRow, 10].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
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

                worksheet.Cells[summaryStartRow, 7, summaryStartRow, 10].Style.Border.Top.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                summaryStartRow++;

                worksheet.Cells[summaryStartRow, 7].Value = "Overall Average";
                worksheet.Cells[summaryStartRow, 7].Style.Font.Bold = true;
                worksheet.Cells[summaryStartRow, 8].Value = data.Average(d => d.LeadTimeCalendarDays);
                worksheet.Cells[summaryStartRow, 9].Value = data.Average(d => d.LeadTimeBusinessDays);
                worksheet.Cells[summaryStartRow, 10].Value = data.Average(d => d.Value);
                worksheet.Cells[summaryStartRow, 8, summaryStartRow, 10].Style.Font.Bold = true;

                // --- APPLY NUMBER FORMATTING TO AVERAGES ---
                worksheet.Cells[summaryStartRow - groupedData.Count() - 1, 8, summaryStartRow, 9].Style.Numberformat.Format = "0.00";
                worksheet.Cells[summaryStartRow - groupedData.Count() - 1, 10, summaryStartRow, 10].Style.Numberformat.Format = "£#,##0.00";

                summaryStartRow += 2;
                worksheet.Cells[summaryStartRow, 1].Value = "Note: \"Business Days\" excludes Saturdays, Sundays, and all official England & Wales bank holidays.";
                worksheet.Cells[summaryStartRow, 1, summaryStartRow, 10].Merge = true;
                worksheet.Cells[summaryStartRow, 1].Style.Font.Italic = true;
                worksheet.Cells[summaryStartRow, 1].Style.Font.Size = 9;
            }

            worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
            if (File.Exists(outputFilePath)) { File.Delete(outputFilePath); }
            File.WriteAllBytes(outputFilePath, package.GetAsByteArray());
        }

        private static string GetCustomerType(string customerName)
        {
            if (string.IsNullOrWhiteSpace(customerName)) return "Unknown";
            var match = Regex.Match(customerName, @"\(([^)]+)\)$");
            if (match.Success)
            {
                string type = match.Groups[1].Value.Trim().ToLower();
                return type == "contract-direct" ? "contract" : type;
            }
            return "non-contract";
        }

        private static int CalculateBusinessDays(DateTime startDate, DateTime endDate)
        {
            int businessDays = 0;
            for (var date = startDate.Date; date < endDate.Date; date = date.AddDays(1))
            {
                if (date.DayOfWeek != DayOfWeek.Saturday &&
                    date.DayOfWeek != DayOfWeek.Sunday &&
                    !BankHolidayHelper.IsBankHoliday(date))
                {
                    businessDays++;
                }
            }
            return businessDays;
        }

        private static bool TryGetDateTime(object excelCellValue, out DateTime result)
        {
            result = DateTime.MinValue;
            if (excelCellValue == null) return false;

            if (excelCellValue is DateTime dt) { result = dt; return true; }

            string dateString = excelCellValue.ToString().Trim().TrimStart('\'');
            if (string.IsNullOrWhiteSpace(dateString)) return false;

            if (double.TryParse(dateString, out double d) && d > 0)
            {
                result = DateTime.FromOADate(d);
                return true;
            }

            if (DateTime.TryParseExact(dateString, "dd/MM/yyyy", CultureInfo.GetCultureInfo("en-GB"), DateTimeStyles.None, out result))
            {
                return true;
            }
            return DateTime.TryParse(dateString, out result);
        }
        #endregion
    }
}