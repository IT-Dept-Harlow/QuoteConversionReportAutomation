// IAutoRunUIContext.cs
// This interface has been updated to reflect changes in the UIManager.
// The 'isDarkModeCurrentlyActive' parameter has been removed from the 
// UpdateAutoRunButtonAndStatus method signature, as this state is now
// managed centrally by the ThemeSettings class.

namespace QuoteConversionReportAutomation.Interfaces
{
    /// <summary>
    /// Defines the contract for UI updates required by the AutoRunManager.
    /// Form1 will implement this interface.
    /// </summary>
    public interface IAutoRunUIContext
    {
        /// <summary>
        /// Reports general progress or status messages from the AutoRunManager.
        /// Typically updates the main status label.
        /// </summary>
        /// <param name="message">The progress message to display.</param>
        void ReportAutoRunProgress(string message);

        /// <summary>
        /// Reports status messages specifically for the right-hand side of the status bar (AutoRun status).
        /// </summary>
        /// <param name="message">The status message to display.</param>
        void ReportAutoRunStatusRight(string message);

        /// <summary>
        /// Sets the state of UI controls based on whether an auto-run process is in progress.
        /// </summary>
        /// <param name="inProgress">True if auto-run is starting/in progress (disable controls),
        /// false if auto-run is finished (re-enable relevant controls).</param>
        void SetControlsForAutoRunInProgress(bool inProgress);

        /// <summary>
        /// Updates the AutoRun toggle button and the detailed AutoRun status label.
        /// The dark mode state is now handled internally by the UIManager and ThemeSettings.
        /// </summary>
        /// <param name="isTimerEnabled">Current state of the auto-run timer.</param>
        /// <param name="isJobDoneOrFailedForToday">Indicates if all due jobs for the day are done or if a critical failure occurred.</param>
        /// <param name="statusTextToDisplay">The specific status text to display in the right status label.</param>
        void UpdateAutoRunButtonAndStatus(bool isTimerEnabled, bool isJobDoneOrFailedForToday, string statusTextToDisplay);

        /// <summary>
        /// Gets a value indicating whether Windows Dark Mode is currently enabled.
        /// </summary>
        /// <returns>True if dark mode is enabled, otherwise false.</returns>
        bool IsWindowsDarkModeEnabled();
    }
}