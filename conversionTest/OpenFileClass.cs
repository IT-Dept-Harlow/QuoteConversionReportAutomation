using System.ComponentModel;
using System.Diagnostics;

namespace QuoteConversionReportAutomation
{
    /// <summary>
    /// Provides functionality to open a file using the default associated application.
    /// </summary>
    public class OpenFileClass
    {
        /// <summary>
        /// Opens the specified file using the default associated application.
        /// </summary>
        /// <param name="filePath">The path of the file to open.</param>
        /// <returns>True if the file was opened successfully; otherwise, false.</returns>
        public static bool OpenFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));
            }

            if (!File.Exists(filePath))
            {
                DisplayFileNotFoundMessage(filePath);
                return false;
            }

            try
            {
                StartProcess(filePath);
                return true;
            }
            catch (Win32Exception ex)
            {
                return HandleWin32Exception(filePath, ex);
            }
            catch (Exception ex)
            {
                DisplayErrorMessage($"An unexpected error occurred: {ex.Message}", "Error");
                return false;
            }
        }

        /// <summary>
        /// Opens the file using the default associated application.
        /// </summary>
        /// <param name="filePath">The path of the file to open.</param>
        private static void StartProcess(string filePath)
        {
            Process.Start(filePath);
        }

        /// <summary>
        /// Displays a message box with file not found error.
        /// </summary>
        /// <param name="filePath">The path of the file that was not found.</param>
        private static void DisplayFileNotFoundMessage(string filePath)
        {
            MessageBox.Show($"File not found: {filePath}", "File Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        /// <summary>
        /// Handles Win32 exceptions and displays an appropriate error message.
        /// </summary>
        /// <param name="filePath">The path of the file being opened.</param>
        /// <param name="ex">The Win32Exception that occurred.</param>
        /// <returns>False, indicating that the file was not opened successfully.</returns>
        private static bool HandleWin32Exception(string filePath, Win32Exception ex)
        {
            switch (ex.ErrorCode)
            {
                case -2147024891: // 0x80070005 (Access Denied)
                    DisplayErrorMessage($"Access denied: {filePath}\n{ex.Message}", "Access Error");
                    break;
                default:
                    DisplayErrorMessage($"Error opening file: {filePath}\n{ex.Message}", "File Error");
                    break;
            }
            return false;
        }

        /// <summary>
        /// Displays an error message box with the specified message and title.
        /// </summary>
        /// <param name="message">The message to display.</param>
        /// <param name="title">The title of the message box.</param>
        private static void DisplayErrorMessage(string message, string title)
        {
            MessageBox.Show(message, title, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
