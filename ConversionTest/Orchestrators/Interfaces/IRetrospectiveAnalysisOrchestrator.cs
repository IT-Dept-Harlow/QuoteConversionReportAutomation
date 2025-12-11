using QuoteConversionReportAutomation.Models;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace QuoteConversionReportAutomation.Orchestrators.Interfaces
{
    public interface IRetrospectiveAnalysisOrchestrator
    {
        /// <summary>
        /// Finds all historical reports matching a pattern within a specified folder,
        /// extracts lead time data, and generates a single summary spreadsheet.
        /// </summary>
        /// <param name="targetFolder">The root folder to scan for report files.</param>
        /// <param name="fileNamePattern">The file name pattern to search for (e.g., "*_Report.xlsx").</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task GenerateAnalysisAsync(string targetFolder, string fileNamePattern, CancellationToken cancellationToken);
    }
}