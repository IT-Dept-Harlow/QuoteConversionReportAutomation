using Microsoft.Extensions.Configuration; // Add this using directive
using QuoteConversionReportAutomation.Helpers;
using QuoteConversionReportAutomation.Interfaces;
using QuoteConversionReportAutomation.Models;
using QuoteConversionReportAutomation.Models.Status;
using QuoteConversionReportAutomation.Orchestrators.Interfaces;
using QuoteConversionReportAutomation.Services.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace QuoteConversionReportAutomation.Orchestrators
{
    public class BatchRegenerationOrchestrator : IBatchRegenerationOrchestrator
    {
        private readonly IStatusManagerService _statusManager;
        private readonly IManualReportOrchestrator _manualOrchestrator;
        private readonly IConfiguration _configuration; // New dependency

        public BatchRegenerationOrchestrator(IStatusManagerService statusManager, IManualReportOrchestrator manualOrchestrator, IConfiguration configuration)
        {
            _statusManager = statusManager;
            _manualOrchestrator = manualOrchestrator;
            _configuration = configuration; // Store the dependency
            BankHolidayHelper.Initialize();
        }

        public async Task RegenerateReportsAsync(ReportType reportType, DateTime startDate, DateTime endDate, CancellationToken cancellationToken)
        {
            var processingDates = GetProcessingPeriods(reportType, startDate, endDate);

            if (processingDates.Count == 0)
            {
                _statusManager.Post("No valid periods found in the selected date range.", MessageType.Warning, TimeSpan.FromSeconds(5));
                return;
            }

            _statusManager.Post($"Found {processingDates.Count} periods to process. Starting batch...", MessageType.InProgress);
            await Task.Delay(1500, cancellationToken);

            for (int i = 0; i < processingDates.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var (periodStart, periodEnd) = processingDates[i];
                _statusManager.Post($"Processing {i + 1} of {processingDates.Count}: {periodEnd:dd-MMM-yyyy}", MessageType.InProgress);

                var parameters = new ManualReportParameters
                {
                    ReportType = reportType,
                    EndDate = periodEnd,
                    StartDate = periodStart,
                    SkipEmail = true, // Skip email for batch regeneration
                    IncludeLeadTimeAnalysis = true, // Include lead time analysis
                    IsDebugBuild = false // Assuming this is a production run, set to false
                };

                try
                {
                    var creationResult = await _manualOrchestrator.CreateRawReportAsync(parameters, cancellationToken);
                    if (!creationResult.Success)
                    {
                        Logger.LogWarning($"Batch Regeneration: Failed to create raw report for {periodEnd:d}. Error: {creationResult.ErrorMessage}");
                        continue;
                    }

                    var processingResult = await _manualOrchestrator.ProcessAndEmailReportAsync(creationResult.GeneratedRawPath, parameters, cancellationToken);
                    if (!processingResult.Success)
                    {
                        Logger.LogWarning($"Batch Regeneration: Failed to process report for {periodEnd:d}. Error: {processingResult.ErrorMessage}");
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError($"Batch Regeneration: A critical error occurred while processing date {periodEnd:d}.", ex);
                }
            }
            _statusManager.Post("Batch regeneration complete!", MessageType.Success, TimeSpan.FromSeconds(10));
        }

        #region Private Methods
        /// <summary>
        /// Determines the distinct report periods that need to be generated based on a user-selected date range.
        /// It uses the centralised ReportHelper to ensure calculations are consistent.
        /// </summary>
        /// <param name="reportType">The type of report to generate.</param>
        /// <param name="startDate">The start of the user-selected date range.</param>
        /// <param name="endDate">The end of the user-selected date range.</param>
        /// <returns>A list of distinct report periods (StartDate, EndDate) to be processed.</returns>
        private List<(DateTime Start, DateTime End)> GetProcessingPeriods(ReportType reportType, DateTime startDate, DateTime endDate)
        {
            var periods = new HashSet<(DateTime, DateTime)>();

            // For Daily reports, we only care about workdays.
            bool isDailyType = reportType is ReportType.Daily or ReportType.Daily5Day1k;

            // Iterate through each day in the user's selected range.
            for (var dt = startDate.Date; dt <= endDate.Date; dt = dt.AddDays(1))
            {
                // For Daily reports, skip non-workdays.
                if (isDailyType && (dt.DayOfWeek == DayOfWeek.Saturday || dt.DayOfWeek == DayOfWeek.Sunday || BankHolidayHelper.IsBankHoliday(dt)))
                {
                    continue;
                }

                // For each valid day, calculate the standard report period it belongs to.
                // This call now uses the new, centralised logic.
                var (periodStart, periodEnd) = ReportHelper.GetReportDateRange(reportType, dt, _configuration);

                // Add the calculated period to a HashSet.
                // The HashSet automatically handles duplicates. For example, selecting multiple
                // days within the same week will resolve to the same single weekly period.
                periods.Add((periodStart, periodEnd));
            }

            // Return the distinct periods, ordered by their end date.
            return periods.OrderBy(p => p.Item2).ToList();
        }
        #endregion
    }
}