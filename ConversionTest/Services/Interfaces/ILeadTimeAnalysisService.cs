// QuoteConversionReportAutomation/Services/Interfaces/ILeadTimeAnalysisService.cs

#region Using Directives
using OfficeOpenXml;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using QuoteConversionReportAutomation.Models;
#endregion

namespace QuoteConversionReportAutomation.Services.Interfaces
{
    #region Interface Definition
    /// <summary>
    /// Defines the contract for a service that generates the "Lead Time Analysis" worksheet
    /// and extracts lead time data from report files.
    /// </summary>
    public interface ILeadTimeAnalysisService
    {
        #region Methods
        /// <summary>
        /// Creates the 'Lead Time Analysis' worksheet within an existing Excel package.
        /// </summary>
        /// <param name="package">The Excel package to which the new sheet will be added.</param>
        /// <param name="sourceDataSheetName">The name of the worksheet containing the source 'DATA'.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task CreateLeadTimeAnalysisSheetAsync(ExcelPackage package, string sourceDataSheetName, CancellationToken cancellationToken);

        /// <summary>
        /// Extracts lead time records from a specified Excel file path. This is used for retrospective analysis.
        /// </summary>
        /// <param name="filePath">The full path to the Excel file to process.</param>
        /// <returns>A list of <see cref="LeadTimeRecord"/> objects extracted from the file.</returns>
        List<LeadTimeRecord> ExtractLeadTimeRecords(string filePath);
        #endregion
    }
    #endregion
}