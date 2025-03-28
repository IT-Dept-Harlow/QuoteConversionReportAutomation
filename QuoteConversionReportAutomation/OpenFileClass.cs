using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;

namespace QuoteConversionReportAutomation
{
    /// <summary>
    /// Provides functionality to open a file using the default associated application.
    /// </summary>
    public class OpenFileClass
    {
        /// <summary>
        /// Opens the specified file using the default associated application.
        /// Displays an error message if the file does not exist or cannot be opened.
        /// </summary>
        /// <param name="filePath">The path of the file to open.</param>
        /// <returns>True if the file was opened successfully; otherwise, false.</returns>
        public bool OpenFile(string filePath)
        {
            // Check if the file exists
            if (File.Exists(filePath))
            {
                try
                {
                    // Attempt to open the file using the default associated application
                    Process.Start(filePath);
                    return true; // File opened successfully
                }
                catch (FileNotFoundException ex)
                {
                    // Handle file not found exception
                    MessageBox.Show($"File not found: {filePath}\n{ex.Message}", "File Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }
                catch (UnauthorizedAccessException ex)
                {
                    // Handle access denied exception
                    MessageBox.Show($"Access denied: {filePath}\n{ex.Message}", "Access Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }
                catch (Win32Exception ex)
                {
                    // Handle other Windows-specific errors
                    MessageBox.Show($"Error opening file: {filePath}\n{ex.Message}", "File Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }
                catch (Exception ex)
                {
                    // Handle any other unexpected exceptions
                    MessageBox.Show($"An unexpected error occurred: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }
            }
            else
            {
                // File does not exist
                MessageBox.Show($"File not found: {filePath}", "File Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }
    }
}