using QuoteConversionReportAutomation.Models.Status;
using System;

namespace QuoteConversionReportAutomation.Interfaces
{
    /// <summary>
    /// Defines the contract for a centralized service that manages application status messages.
    /// </summary>
    public interface IStatusManagerService
    {
        /// <summary>
        /// Occurs when the application status has changed.
        /// UI components should subscribe to this event to display updates.
        /// </summary>
        event EventHandler<StatusPayload> StatusChanged;

        /// <summary>
        /// Posts a new status message to the application.
        /// </summary>
        /// <param name="message">The text of the message to display.</param>
        /// <param name="type">The type of message (e.g., Success, Error).</param>
        /// <param name="duration">If provided, the message will automatically clear after this timespan.</param>
        void Post(string message, MessageType type, TimeSpan? duration = null);

        /// <summary>
        /// Posts a simple informational message.
        /// </summary>
        void Post(string message);

        /// <summary>
        /// Clears the current status, typically resetting it to a "Ready" state.
        /// </summary>
        void Clear();
    }
}