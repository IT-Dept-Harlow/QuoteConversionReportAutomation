using System;

namespace QuoteConversionReportAutomation.Models.Status
{
    /// <summary>
    /// Defines the different types of status messages the application can have.
    /// </summary>
    public enum MessageType
    {
        // A standard, persistent information message.
        Info,
        // A temporary message indicating a successful operation.
        Success,
        // A persistent warning message.
        Warning,
        // A persistent error message.
        Error,
        // A message indicating a background task is running.
        InProgress,
        // A special message type for "Ready" or cleared states.
        Cleared
    }

    /// <summary>
    /// Represents the payload for a status update event.
    /// </summary>
    /// <param name="Message">The text of the status message.</param>
    /// <param name="Type">The type of the message, used for colour-coding and behaviour.</param>
    /// <param name="Duration">If specified, the message is transient and will be cleared after this duration.</param>
    public record StatusPayload(
        string Message,
        MessageType Type,
        TimeSpan? Duration = null
    );
}