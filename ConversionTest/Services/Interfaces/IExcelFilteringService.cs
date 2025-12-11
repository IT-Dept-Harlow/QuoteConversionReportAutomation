using OfficeOpenXml;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace QuoteConversionReportAutomation.Services.Interfaces
{
    /// <summary>
    /// Defines the contract for a service responsible for filtering data within an Excel worksheet.
    /// </summary>
    public interface IExcelFilteringService
    {
        /// <summary>
        /// Filters a worksheet to keep only rows where a numeric value in a specified column meets a threshold.
        /// As this method may replace the worksheet object, it returns the new, valid worksheet reference.
        /// </summary>
        /// <param name="worksheet">The worksheet to filter.</param>
        /// <param name="priceColumnIndex">The 1-based index of the column containing numeric values to check.</param>
        /// <param name="threshold">The decimal threshold. Rows with values less than this will be removed.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>
        /// A task representing the asynchronous operation. The task result is the
        /// new <see cref="ExcelWorksheet"/> instance that contains the filtered data.
        /// </returns>
        Task<ExcelWorksheet> FilterDataSheetByValueAsync(ExcelWorksheet worksheet, int priceColumnIndex, decimal threshold, CancellationToken cancellationToken);

        /// <summary>
        /// Filters a worksheet to keep only rows where a string value in a specified column matches a list of valid codes.
        /// As this method may replace the worksheet object, it returns the new, valid worksheet reference.
        /// </summary>
        /// <param name="worksheet">The worksheet to filter.</param>
        /// <param name="postingCodeColumnIndex">The 1-based index of the column to check for posting codes.</param>
        /// <param name="validPostingCodes">A HashSet containing the list of valid posting codes for efficient lookup.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>
        /// A task representing the asynchronous operation. The task result is the
        /// new <see cref="ExcelWorksheet"/> instance that contains the filtered data.
        /// </returns>
        Task<ExcelWorksheet> FilterDataSheetByPostingCodeAsync(ExcelWorksheet worksheet, int postingCodeColumnIndex, HashSet<string> validPostingCodes, CancellationToken cancellationToken);
    }
}