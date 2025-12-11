using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace QuoteConversionReportAutomation.Services.Interfaces
{
    /// <summary>
    /// Defines the contract for a service that generates the main "Analysis" worksheet.
    /// </summary>
    public interface IExcelAnalysisService
    {
        /// <summary>
        /// Extracts unique customer and posting code pairs from the 'DATA' sheet, populates them into the 'Analysis' sheet,
        /// and writes dynamic, two-criteria formulas to perform the analysis, including formatting.
        /// </summary>
        Task CreateAnalysisSheetAsync(
            ExcelWorksheet dataSheet,
            ExcelWorksheet analysisSheet,
            Dictionary<string, int> dataColumnMap,
            Dictionary<string, int> analysisColumnMap,
            DateTime reportDate,
            string sourceFileName,
            CancellationToken cancellationToken
        );
    }
}