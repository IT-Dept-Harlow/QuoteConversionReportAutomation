using QuoteConversionReportAutomation.Models; // Add this
using System;
using System.Threading;
using System.Threading.Tasks;

namespace QuoteConversionReportAutomation.Orchestrators.Interfaces
{
    public interface IBatchRegenerationOrchestrator
    {
        // Update the signature to accept a ReportType
        Task RegenerateReportsAsync(ReportType reportType, DateTime startDate, DateTime endDate, CancellationToken cancellationToken);
    }
}