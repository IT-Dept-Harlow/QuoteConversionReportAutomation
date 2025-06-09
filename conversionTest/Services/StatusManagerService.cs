using QuoteConversionReportAutomation.Interfaces;
using QuoteConversionReportAutomation.Models.Status;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace QuoteConversionReportAutomation.Services
{
    /// <summary>
    /// A centralized service that manages and broadcasts application-wide status messages.
    /// </summary>
    public class StatusManagerService : IStatusManagerService
    {
        public event EventHandler<StatusPayload> StatusChanged;

        private CancellationTokenSource _resetTimerCts;

        public StatusManagerService()
        {
            _resetTimerCts = new CancellationTokenSource();
        }

        public void Post(string message, MessageType type, TimeSpan? duration = null)
        {
            // Cancel any pending reset timer.
            _resetTimerCts.Cancel();
            _resetTimerCts.Dispose();
            _resetTimerCts = new CancellationTokenSource();

            // Raise the event to notify subscribers of the new message.
            StatusChanged?.Invoke(this, new StatusPayload(message, type, duration));

            // If a duration is specified, start a new timer to clear the message.
            if (duration.HasValue)
            {
                Task.Delay(duration.Value, _resetTimerCts.Token).ContinueWith(t =>
                {
                    // Only clear if the task wasn't cancelled.
                    if (t.IsCompletedSuccessfully)
                    {
                        Clear();
                    }
                }, TaskScheduler.Default); // Can use default scheduler, Clear() will handle UI thread.
            }
        }

        public void Post(string message)
        {
            Post(message, MessageType.Info);
        }

        public void Clear()
        {
            // Post a special "Cleared" message, which the UI can interpret as "Ready".
            Post("Ready", MessageType.Cleared);
        }
    }
}