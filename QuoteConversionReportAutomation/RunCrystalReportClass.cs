using CrystalDecisions.CrystalReports.Engine;
using CrystalDecisions.Shared;
using System;
using System.IO;
using System.Windows.Forms;

namespace QuoteConversionReportAutomation
{
    /// <summary>
    /// Provides functionality to run and export Crystal Reports with progress updates.
    /// </summary>
    public class RunCrystalReportClass
    {
        private readonly int _reportingPeriod; // Store the value of report type

        /// <summary>
        /// Initializes a new instance of the <see cref="RunCrystalReportClass"/> class.
        /// </summary>
        /// <param name="reportType">
        /// Indicates the report type: 0 for weekly, 1 for monthly, 2 for quarterly, or 3 for annual.
        /// </param>
        public RunCrystalReportClass(int reportType)
        {
            _reportingPeriod = reportType;
        }

        /// <summary>
        /// Runs the Crystal Report, sets parameters, and exports it to an Excel workbook, providing progress updates.
        /// </summary>
        /// <param name="crystalReportLocation">The file path to the Crystal Report (.rpt) file.</param>
        /// <param name="reportOutputLocation">The file path where the exported Excel workbook should be saved.</param>
        /// <param name="reportDateFrom">The start date for the report.</param>
        /// <param name="reportDateTo">The end date for the report.</param>
        /// <param name="statusStrip">The StatusStrip control to display progress updates (optional).</param>
        public void RunReport(string crystalReportLocation, string reportOutputLocation, DateTime reportDateFrom, DateTime reportDateTo, StatusStrip statusStrip = null)
        {
            try
            {
                
                // Validate input parameters.
                if (string.IsNullOrEmpty(crystalReportLocation))
                    throw new ArgumentException("Crystal Report location cannot be null or empty.", nameof(crystalReportLocation));
                if (string.IsNullOrEmpty(reportOutputLocation))
                    throw new ArgumentException("Report output location cannot be null or empty.", nameof(reportOutputLocation));

                using (ReportDocument quoteReport = new ReportDocument())
                {
                    //clean up old files
                    CleanupOldFiles(Path.GetDirectoryName(reportOutputLocation), statusStrip);

                    // Load the report.
                    LoadReport(quoteReport, crystalReportLocation);

                    // Set report parameters.
                    SetReportParameters(quoteReport, reportDateFrom, reportDateTo);

                    // Export the report.
                    ExportReport(quoteReport, reportOutputLocation, statusStrip);
                }
            }
            catch (Exception ex)
            {
                // Log the error and show a message to the user.  Include the original exception.
                string errorMessage = $"An error occurred while running the report: {ex.Message}";
                Logger.LogError(errorMessage);
                MessageBox.Show(errorMessage, "Report Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                throw; // Re-throw the exception to allow the caller to handle it, if necessary.
            }
        }

        /// <summary>
        /// Loads the Crystal Report from the specified file path.
        /// </summary>
        /// <param name="reportDocument">The Crystal Reports ReportDocument object.</param>
        /// <param name="crystalReportLocation">The file path to the Crystal Report (.rpt) file.</param>
        private static void LoadReport(ReportDocument reportDocument, string crystalReportLocation)
        {
            try
            {
                reportDocument.Load(crystalReportLocation);
                Logger.LogInfo($"Report loaded successfully from: {crystalReportLocation}");
            }
            catch (Exception ex)
            {
                string errorMessage = $"Error loading Crystal Report: {ex.Message}";
                Logger.LogError(errorMessage);
                throw new ReportLoadingException(errorMessage, ex); // Custom exception for report loading errors
            }
        }

        /// <summary>
        /// Sets the parameters for the Crystal Report.
        /// </summary>
        /// <param name="reportDocument">The Crystal Reports ReportDocument object.</param>
        /// <param name="reportDateFrom">The start date for the report.</param>
        /// <param name="reportDateTo">The end date for the report.</param>
        private static void SetReportParameters(ReportDocument reportDocument, DateTime reportDateFrom, DateTime reportDateTo)
        {
            reportDocument.SetParameterValue("From", reportDateFrom);
            reportDocument.SetParameterValue("To", reportDateTo);
            reportDocument.SetParameterValue("Customer", ""); // Consider making this a parameter
            reportDocument.SetParameterValue("Ordered", "Both"); // Consider making this a parameter
            reportDocument.SetParameterValue("Revisions", "Yes"); // Consider making this a parameter
            Logger.LogInfo($"Report parameters set: From = {reportDateFrom}, To = {reportDateTo}");
        }

        /// <summary>
        /// Exports the Crystal Report to an Excel workbook.
        /// </summary>
        /// <param name="reportDocument">The Crystal Reports ReportDocument object.</param>
        /// <param name="reportOutputLocation">The file path where the exported Excel workbook should be saved.</param>
        /// <param name="statusStrip">The StatusStrip control to display progress updates (optional).</param>
        private void ExportReport(ReportDocument reportDocument, string reportOutputLocation, StatusStrip statusStrip = null)
        {
            try
            {
                statusStrip?.Invoke((MethodInvoker)delegate { statusStrip.Items[0].Text = "Exporting Report..."; });
                reportDocument.ExportToDisk(ExportFormatType.ExcelWorkbook, reportOutputLocation);
                Logger.LogInfo("Report exported successfully.");
                statusStrip?.Invoke((MethodInvoker)delegate { statusStrip.Items[0].Text = "Report Created Successfully."; });
            }
            catch (Exception ex)
            {
                string errorMessage = $"Error exporting report: {ex.Message}";
                Logger.LogError(errorMessage);
                throw new ReportExportException(errorMessage, ex); // Custom exception for export errors.
            }
        }

        /// <summary>
        /// Cleans up files older than 30 days in the specified report directory by archiving them.
        /// </summary>
        /// <param name="reportDirectory">The directory containing the report files.</param>
        /// <param name="statusStrip">The StatusStrip control to display progress updates (optional).</param>
        private void CleanupOldFiles(string reportDirectory, StatusStrip statusStrip = null)
        {
            if (string.IsNullOrEmpty(reportDirectory))
            {
                Logger.LogWarning("Report directory is null or empty. Skipping cleanup.");
                return;
            }

            try
            {
                DirectoryInfo directory = new DirectoryInfo(reportDirectory);
                DateTime cutoffDate = DateTime.Now.AddDays(-30);
                int fileCount = directory.GetFiles().Length;
                int filesProcessed = 0;

                foreach (FileInfo file in directory.GetFiles())
                {
                    if (file.LastWriteTime < cutoffDate)
                    {
                        ArchiveFile(file, reportDirectory);
                    }
                    filesProcessed++;
                    UpdateStatusStrip(statusStrip, filesProcessed, fileCount);
                }
                statusStrip?.Invoke((MethodInvoker)delegate { statusStrip.Items[0].Text = "Archiving Complete"; });
            }
            catch (Exception ex)
            {
                string errorMessage = $"Error cleaning up old files: {ex.Message}";
                Logger.LogError(errorMessage);
                throw new FileCleanupException(errorMessage, ex); // Custom exception for file cleanup
            }
        }

        /// <summary>
        /// Archives the specified file.
        /// </summary>
        /// <param name="file">The FileInfo object representing the file to archive.</param>
        /// <param name="reportDirectory">The main directory where reports are stored.</param>
        private static void ArchiveFile(FileInfo file, string reportDirectory)
        {
            string archiveDirectory = Path.Combine(reportDirectory, "Archive", file.LastWriteTime.ToString("yyyy-MM"));
            if (!Directory.Exists(archiveDirectory))
            {
                Directory.CreateDirectory(archiveDirectory);
                Logger.LogInfo($"Created archive directory: {archiveDirectory}");
            }

            string archiveFilePath = Path.Combine(archiveDirectory, file.Name);
            File.Move(file.FullName, archiveFilePath);
            Logger.LogInfo($"Archived file: {file.Name} to {archiveFilePath}");
        }

        /// <summary>
        /// Updates the StatusStrip control with the current archiving progress.
        /// </summary>
        /// <param name="statusStrip">The StatusStrip control to update.</param>
        /// <param name="filesProcessed">The number of files processed.</param>
        /// <param name="fileCount">The total number of files.</param>
        private static void UpdateStatusStrip(StatusStrip statusStrip, int filesProcessed, int fileCount)
        {
            if (statusStrip == null) return;

            statusStrip.Invoke((MethodInvoker)delegate
            {
                int percentage = (int)((double)filesProcessed / fileCount * 100);
                statusStrip.Items[0].Text = $"Archiving files: {percentage}%";
            });
        }
    }

    /// <summary>
    /// Custom exception for errors that occur during report loading.
    /// </summary>
    public class ReportLoadingException : Exception
    {
        /// <summary>
        /// Custom exception for errors that occur during report loading.
        /// </summary>
        public ReportLoadingException(string message, Exception innerException) : base(message, innerException) { }
    }

    /// <summary>
    /// Custom exception for errors that occur during report exporting.
    /// </summary>
    public class ReportExportException : Exception
    {
        /// <summary>
        /// Custom exception for errors that occur during report exporting.
        /// </summary>
        public ReportExportException(string message, Exception innerException) : base(message, innerException) { }
    }

    /// <summary>
    /// Custom exception for errors that occur during file cleanup.
    /// </summary>
    public class FileCleanupException : Exception
    {
        /// <summary>
        /// Custom exception for errors that occur during file cleanup.
        /// </summary>
        public FileCleanupException(string message, Exception innerException) : base(message, innerException) { }
    }
}
