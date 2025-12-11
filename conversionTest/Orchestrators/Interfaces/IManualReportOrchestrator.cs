// IManualReportOrchestrator.cs
// This interface defines the contract for the ManualReportOrchestrator.
// The method signatures have been updated to remove the IProgress<T> parameters,
// as progress reporting is now handled by the injected IStatusManagerService.

#region Using Directives
using QuoteConversionReportAutomation.Services.Communication;
using QuoteConversionReportAutomation.Models; // For parameter and result models
using System;
using System.Threading;
using System.Threading.Tasks;
#endregion

namespace QuoteConversionReportAutomation.Orchestrators.Interfaces
{
    /// <summary>
    /// Defines the contract for a class that orchestrates the manual creation,
    /// processing, and emailing of quote conversion reports.
    /// </summary>
    public interface IManualReportOrchestrator
    {
        /// <summary>
        /// Asynchronously creates a raw data report based on the provided parameters.
        /// </summary>
        /// <param name="parameters">The parameters defining the report to be created.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A <see cref="Task{TResult}"/> that represents the asynchronous operation.
        /// The task result contains a <see cref="ReportCreationResult"/> object with the outcome.</returns>
        Task<ReportCreationResult> CreateRawReportAsync(
            ManualReportParameters parameters,
            CancellationToken cancellationToken);

        /// <summary>
        /// Asynchronously processes a raw report file and, if configured, sends it via email.
        /// </summary>
        /// <param name="rawReportPath">The full path to the raw report file to be processed.</param>
        /// <param name="parameters">The parameters that guided the report's creation.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A <see cref="Task{TResult}"/> that represents the asynchronous operation.
        /// The task result contains a <see cref="ReportProcessingResult"/> object with the outcome.</returns>
        Task<ReportProcessingResult> ProcessAndEmailReportAsync(
            string rawReportPath,
            ManualReportParameters parameters,
            CancellationToken cancellationToken);

        // Commented out as not currently used
        ///// <summary>
        ///// Asynchronously sends an email with the specified analysis file after a manual user refresh.
        ///// </summary>
        ///// <param name="analysisFilePath">The full path to the analysis file to be attached.</param>
        ///// <param name="parameters">The original parameters for the report.</param>
        ///// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        ///// <returns>A <see cref="Task{TResult}"/> that represents the asynchronous operation.
        ///// The task result contains an <see cref="EmailSendResult"/> object with the outcome of the email operation.</returns>
        //Task<EmailSendResult> SendEmailAfterManualRefreshAsync(
        //    string analysisFilePath,
        //    ManualReportParameters parameters,
        //    CancellationToken cancellationToken);
    }
}