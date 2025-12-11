#region Using Directives

// System-related namespaces for core functionalities.
using System.Threading;
using System.Threading.Tasks;

// Third-party namespaces for external libraries.
using OfficeOpenXml;

#endregion

namespace QuoteConversionReportAutomation.Services.Interfaces
{
    #region Interface Definition
    /// <summary>
    /// Defines the contract for a service responsible for excluding specific data,
    /// such as tender accounts, from an Excel worksheet based on a configurable list of posting codes.
    /// </summary>
    public interface IExcelDataExclusionService
    {
        #region Methods
        /// <summary>
        /// Asynchronously filters a worksheet to remove rows corresponding to tender accounts.
        /// The list of tender account posting codes to be excluded is read from the application configuration.
        /// Because this method replaces the original worksheet, it returns the new, valid worksheet reference.
        /// </summary>
        /// <param name="worksheet">The 'DATA' worksheet to be filtered.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>
        /// A task representing the asynchronous filtering operation. The task result is the
        /// new <see cref="ExcelWorksheet"/> instance that contains the filtered data.
        /// </returns>
        Task<ExcelWorksheet> ExcludeTenderAccountsAsync(ExcelWorksheet worksheet, CancellationToken cancellationToken);
        #endregion
    }
    #endregion
}