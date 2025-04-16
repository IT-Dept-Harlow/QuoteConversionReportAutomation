using CrystalDecisions.CrystalReports.Engine;
using CrystalDecisions.Shared;
using System;
using System.IO;
using System.Windows.Forms;

namespace QuoteConversionReportAutomation
{
    /// <summary>
    /// Class to handle running and exporting Crystal Reports with progress updates.
    /// </summary>
    public class RunCrystalReportClass
    {
        private int reportingPeriod; // Store the checkbox // value not used anymore see useMonthly

        /// <summary>
        /// Gets the bool for check box value
        /// </summary>
        /// <param name="reportType">Indicates whether to use (0)weekly, (1)monthly, (2)quaterly or (3)annual report processing. </param>
        // Constructor to accept the checkbox value.
        public RunCrystalReportClass(int reportType)
        {
            reportingPeriod = reportType;
        }

        /// <summary>
        /// Runs a Crystal Report, sets parameters, and exports it to an Excel workbook, providing progress updates.
        /// </summary>
        /// <param name="crystalReportLocation">The file path to the Crystal Report (.rpt) file.</param>
        /// <param name="reportOutputLocation">The file path where the exported Excel workbook should be saved.</param>
        /// <param name="reportDateFrom">The start date for the report.</param>
        /// <param name="reportDateTo">The end date for the report.</param>
        /// <param name="statusStrip">The StatusStrip control to display progress updates.</param>
        public void RunReport(string crystalReportLocation, string reportOutputLocation, DateTime reportDateFrom, DateTime reportDateTo, StatusStrip statusStrip = null)
        {
            try
            {
                // Ensure proper resource management using 'using' statement.
                using (ReportDocument quoteReport = new ReportDocument())
                {
                    // Clean up old files in the report directory.
                    CleanupOldFiles(Path.GetDirectoryName(reportOutputLocation), statusStrip);

                    // Load the Crystal Report from the specified file path.
                    quoteReport.Load(crystalReportLocation);

                    // Set the report parameters with the provided date range.
                    quoteReport.SetParameterValue("From", reportDateFrom);
                    quoteReport.SetParameterValue("To", reportDateTo);
                    quoteReport.SetParameterValue("Customer", "");
                    quoteReport.SetParameterValue("Ordered", "Both");
                    quoteReport.SetParameterValue("Revisions", "Yes");

                    // Log the report loading and parameter setting.
                    Logger.LogInfo($"Report Loaded Correctly with dates from - {reportDateFrom} To - {reportDateTo}");

                    try
                    {
                        //check for null status strip
                        statusStrip?.Invoke((MethodInvoker)delegate { statusStrip.Items[0].Text = "Exporting Report..."; });

                        // Export the report to an Excel workbook at the specified output location.
                        quoteReport.ExportToDisk(ExportFormatType.ExcelWorkbook, reportOutputLocation);

                        // Log and display a success message.
                        Logger.LogInfo("Report Created Successfully.");

                        //check for null status strip
                        statusStrip?.Invoke((MethodInvoker)delegate { statusStrip.Items[0].Text = "Report Created Successfully."; });

                    }
                    catch (Exception ex)
                    {
                        // Handle and log any exceptions during the export process.
                        Logger.LogError("Error exporting report: " + ex.Message);
                        MessageBox.Show("Error exporting report: " + ex.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                // Handle and log any exceptions during report loading or parameter setting.
                Logger.LogError($"Error loading or setting parameters in Crystal Report: {ex}");
            }
        }

        /// <summary>
        /// Cleans up files older than 30 days in the specified report directory by archiving them, providing progress updates.
        /// </summary>
        /// <param name="reportDirectory">The directory containing the report files.</param>
        /// <param name="statusStrip">The StatusStrip control to display progress updates.</param>
        private void CleanupOldFiles(string reportDirectory, StatusStrip statusStrip = null)
        {
            try
            {
                // Check if the directory path is valid.
                if (string.IsNullOrEmpty(reportDirectory)) return;

                // Create a DirectoryInfo object for the report directory.
                DirectoryInfo directory = new DirectoryInfo(reportDirectory);

                // Calculate the cutoff date for archiving (30 days ago).
                DateTime cutoffDate = DateTime.Now.AddDays(-30);
                int fileCount = directory.GetFiles().Length;
                int filesProcessed = 0;

                // Iterate through each file in the directory.
                foreach (FileInfo file in directory.GetFiles())
                {
                    // Check if the file's last write time is older than the cutoff date.
                    if (file.LastWriteTime < cutoffDate)
                    {
                        // Create the archive directory path based on the file's year and month.
                        string archiveDirectory = Path.Combine(reportDirectory, "Archive", file.LastWriteTime.ToString("yyyy-MM"));

                        // Create the archive directory if it doesn't exist.
                        if (!Directory.Exists(archiveDirectory))
                        {
                            Directory.CreateDirectory(archiveDirectory);
                        }

                        // Create the full archive file path.
                        string archiveFilePath = Path.Combine(archiveDirectory, file.Name);

                        // Move the file to the archive directory.
                        File.Move(file.FullName, archiveFilePath);

                        // Log the archiving action.
                        Logger.LogInfo($"Archived file: {file.Name} to {archiveFilePath}");
                    }
                    filesProcessed++;
                    statusStrip?.Invoke((MethodInvoker)delegate
                    {
                        int percentage = (int)((double)filesProcessed / fileCount * 100);
                        statusStrip.Items[0].Text = $"Archiving files: {percentage}%";
                    });
                }
                statusStrip?.Invoke((MethodInvoker)delegate { statusStrip.Items[0].Text = "Archiving Complete"; });
            }
            catch (Exception ex)
            {
                // Handle and log any exceptions during the cleanup process.
                Logger.LogError($"Error cleaning up old files: {ex}");
            }
        }
    }
}