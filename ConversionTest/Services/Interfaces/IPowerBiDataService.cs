using OfficeOpenXml;
using System.Threading;
using System.Threading.Tasks;

namespace QuoteConversionReportAutomation.Services.Interfaces
{
    /// <summary>
    /// Defines the contract for a service that manages interactions with the Power BI data source file.
    /// </summary>
    public interface IPowerBiDataService
    {
        /// <summary>
        /// Copies data from the 'Analysis' sheet of the processed report to a central Power BI source Excel file,
        /// handling file locking to prevent data corruption.
        /// </summary>
        Task AppendDataToPowerBIReportAsync(ExcelPackage sourcePackage, ExcelWorksheet sourceAnalysisWorksheet, string targetPowerBiSheetName, CancellationToken cancellationToken);
    }
}