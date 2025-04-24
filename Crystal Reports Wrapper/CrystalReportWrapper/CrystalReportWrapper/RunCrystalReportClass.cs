using CrystalDecisions.CrystalReports.Engine;
using CrystalDecisions.Shared;
using System;
using System.IO;
using System.Windows.Forms; // Assuming Logger might use this or for consistency
using System.Configuration;
using System.Linq; // Needed for FirstOrDefault

namespace CrystalReportWrapper // Use the namespace of your wrapper project
{
    /// <summary>
    /// Provides functionality to run and export Crystal Reports with progress updates.
    /// Targeted for .NET Framework 4.8.
    /// </summary>
    public class RunCrystalReportClass
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RunCrystalReportClass"/> class.
        /// </summary>
        /// <param name="reportType">
        /// Indicates the report type (currently unused within this class).
        /// </param>
        public RunCrystalReportClass(int reportType)
        {
            // _reportingPeriod = reportType; // Assign if needed later
        }

        /// <summary>
        /// Runs the Crystal Report, sets parameters, and exports it to an Excel workbook.
        /// </summary>
        /// <param name="crystalReportLocation">The file path to the Crystal Report (.rpt) file.</param>
        /// <param name="reportOutputLocation">The file path where the exported Excel workbook should be saved.</param>
        /// <param name="reportDateFrom">The start date for the report.</param>
        /// <param name="reportDateTo">The end date for the report.</param>
        /// <param name="statusStrip">The StatusStrip control (optional, likely null when called from wrapper).</param>
        /// <exception cref="ArgumentException">Thrown if required paths are null or empty.</exception>
        /// <exception cref="ReportLoadingException">Thrown if the report fails to load.</exception>
        /// <exception cref="ReportExportException">Thrown if the report fails to export.</exception>
        /// <exception cref="FileCleanupException">Thrown if cleaning old files fails.</exception>
        public void RunReport(string crystalReportLocation, string reportOutputLocation, DateTime reportDateFrom, DateTime reportDateTo, StatusStrip statusStrip = null)
        {
            // StatusStrip updates won't work when called from the wrapper process.
            // Logging should be used instead within the wrapper if status is needed.
            void UpdateStatusStripText(string text)
            {
                // This logic remains but will likely do nothing if statusStrip is null.
                if (statusStrip != null && statusStrip.InvokeRequired)
                {
                    statusStrip.Invoke((MethodInvoker)delegate { if (statusStrip.Items.Count > 0) statusStrip.Items[0].Text = text; });
                }
                else if (statusStrip != null && statusStrip.Items.Count > 0)
                {
                    statusStrip.Items[0].Text = text;
                }
                Console.WriteLine($"Status Update (Wrapper): {text}"); // Log status to console instead
                Logger.LogInfo($"Status Update: {text}"); // Use Logger if available
            }

            ReportDocument quoteReport = null;
            try
            {
                // --- Validate input parameters (.NET Framework style) ---
                if (string.IsNullOrEmpty(crystalReportLocation)) throw new ArgumentException($"'{nameof(crystalReportLocation)}' cannot be null or empty.", nameof(crystalReportLocation));
                if (string.IsNullOrEmpty(reportOutputLocation)) throw new ArgumentException($"'{nameof(reportOutputLocation)}' cannot be null or empty.", nameof(reportOutputLocation));
                // --- End Validation ---

                // Use standard using block for IDisposable
                using (quoteReport = new ReportDocument())
                {
                    // Clean up old files (run before loading/exporting)
                    string outputDir = Path.GetDirectoryName(reportOutputLocation);
                    if (!string.IsNullOrEmpty(outputDir))
                    {
                        CleanupOldFiles(outputDir, statusStrip); // Pass null for statusStrip if unavailable
                    }

                    // Load the report.
                    LoadReport(quoteReport, crystalReportLocation);

                    // Set report parameters.
                    SetReportParameters(quoteReport, reportDateFrom, reportDateTo);

                    // Export the report.
                    ExportReport(quoteReport, reportOutputLocation, statusStrip); // Pass null for statusStrip if unavailable
                } // quoteReport is disposed here
            }
            catch (Exception ex)
            {
                string errorMessage = $"An error occurred while running the report: {ex.Message}";
                Console.WriteLine($"ERROR: {errorMessage}");
                Logger.LogError(errorMessage, ex);
                throw; // Re-throw to signal failure back to the pipe server handler
            }
        }

        /// <summary>
        /// Loads the Crystal Report from the specified file path.
        /// </summary>
        private static void LoadReport(ReportDocument reportDocument, string crystalReportLocation)
        {
            try
            {
                reportDocument.Load(crystalReportLocation);
                Console.WriteLine($"Report loaded successfully from: {crystalReportLocation}");
                Logger.LogInfo($"Report loaded successfully from: {crystalReportLocation}");
            }
            catch (Exception ex)
            {
                string errorMessage = $"Error loading Crystal Report '{crystalReportLocation}': {ex.Message}";
                Console.WriteLine($"ERROR: {errorMessage}");
                Logger.LogError(errorMessage, ex);
                throw new ReportLoadingException(errorMessage, ex);
            }
        }

        /// <summary>
        /// Sets the parameters for the Crystal Report.
        /// </summary>
        private static void SetReportParameters(ReportDocument reportDocument, DateTime reportDateFrom, DateTime reportDateTo)
        {
            try
            {
                reportDocument.SetParameterValue("From", reportDateFrom);
                reportDocument.SetParameterValue("To", reportDateTo);
                reportDocument.SetParameterValue("Customer", "");
                reportDocument.SetParameterValue("Ordered", "Both");
                reportDocument.SetParameterValue("Revisions", "Yes");
                Console.WriteLine($"Report parameters set: From = {reportDateFrom:yyyy-MM-dd}, To = {reportDateTo:yyyy-MM-dd}");
                Logger.LogInfo($"Report parameters set: From = {reportDateFrom}, To = {reportDateTo}");
            }
            catch (Exception ex)
            {
                string errorMessage = $"Error setting report parameters: {ex.Message}";
                Console.WriteLine($"ERROR: {errorMessage}");
                 Logger.LogError(errorMessage, ex);
                throw new Exception(errorMessage, ex);
            }
        }

        /// <summary>
        /// Exports the Crystal Report to an Excel workbook (.xlsx).
        /// </summary>
        private void ExportReport(ReportDocument reportDocument, string reportOutputLocation, StatusStrip statusStrip = null)
        {
            void UpdateStatusStripText(string text) { /* ... */ Console.WriteLine($"Status Update (Wrapper - Export): {text}"); }

            try
            {
                UpdateStatusStripText("Exporting Report...");
                string outputDir = Path.GetDirectoryName(reportOutputLocation);
                if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
                {
                    Directory.CreateDirectory(outputDir);
                    Console.WriteLine($"Created output directory: {outputDir}");
                    Logger.LogInfo($"Created output directory: {outputDir}");
                }

                // --- FIX: Use correct options for .xlsx export ---
                ExportOptions exportOpts = new ExportOptions();
                // ExcelFormatOptions formatOpts = new ExcelFormatOptions(); // Use this for general Excel options if needed
                DiskFileDestinationOptions diskOpts = new DiskFileDestinationOptions();

                exportOpts.ExportFormatType = ExportFormatType.ExcelWorkbook; // Correct type for .xlsx
                // exportOpts.FormatOptions = formatOpts; // Assign general options if needed, often null is fine
                exportOpts.ExportDestinationType = ExportDestinationType.DiskFile;
                diskOpts.DiskFileName = reportOutputLocation;
                exportOpts.ExportDestinationOptions = diskOpts;
                // --- End Fix ---

                reportDocument.Export(exportOpts);

                Console.WriteLine($"Report exported successfully to: {reportOutputLocation}");
                Logger.LogInfo($"Report exported successfully to: {reportOutputLocation}");
                UpdateStatusStripText("Report Created Successfully.");
            }
            catch (Exception ex)
            {
                string errorMessage = $"Error exporting report to '{reportOutputLocation}': {ex.Message}";
                Console.WriteLine($"ERROR: {errorMessage}");
                Logger.LogError(errorMessage, ex);
                throw new ReportExportException(errorMessage, ex);
            }
        }

        /// <summary>
        /// Cleans up files older than 30 days in the specified report directory by archiving them.
        /// </summary>
        private void CleanupOldFiles(string reportDirectory, StatusStrip statusStrip = null)
        {
            void UpdateStatusStripText(string text) { Console.WriteLine($"Status Update (Wrapper - Cleanup): {text}"); }

            if (string.IsNullOrEmpty(reportDirectory))
            {
                Console.WriteLine("Warning: Report directory is null or empty. Skipping cleanup.");
                Logger.LogWarning("Report directory is null or empty. Skipping cleanup.");
                return;
            }

            try
            {
                if (!Directory.Exists(reportDirectory))
                {
                    Console.WriteLine($"Warning: Report directory '{reportDirectory}' does not exist. Skipping cleanup.");
                    Logger.LogWarning($"Report directory '{reportDirectory}' does not exist. Skipping cleanup.");
                    return;
                }

                var directory = new DirectoryInfo(reportDirectory);
                var cutoffDate = DateTime.Now.AddDays(-30);
                var files = directory.GetFiles("*.xlsx"); // Assuming output is always .xlsx
                int fileCount = files.Length;
                int filesProcessed = 0;
                int archivedCount = 0;

                UpdateStatusStripText($"Checking {fileCount} file(s) for archiving...");

                foreach (var file in files)
                {
                    try
                    {
                        if (file.LastWriteTime < cutoffDate)
                        {
                            ArchiveFile(file, reportDirectory);
                            archivedCount++;
                        }
                    }
                    catch (Exception fileEx)
                    {
                        Console.WriteLine($"ERROR archiving file '{file.Name}': {fileEx.Message}");
                        Logger.LogError($"Error archiving file '{file.Name}': {fileEx.Message}");
                    }
                    filesProcessed++;
                }
                if (archivedCount > 0) Console.WriteLine($"Archiving complete. Archived {archivedCount} file(s).");
                UpdateStatusStripText("Archiving Complete");
            }
            catch (Exception ex)
            {
                string errorMessage = $"Error cleaning up old files in '{reportDirectory}': {ex.Message}";
                Console.WriteLine($"ERROR: {errorMessage}");
                 Logger.LogError(errorMessage, ex);
            }
        }

        /// <summary>
        /// Archives the specified file.
        /// </summary>
        private static void ArchiveFile(FileInfo file, string reportDirectory)
        {
            try
            {
                string archiveDirectory = Path.Combine(reportDirectory, "Archive", file.LastWriteTime.ToString("yyyy-MM"));
                if (!Directory.Exists(archiveDirectory))
                {
                    Directory.CreateDirectory(archiveDirectory);
                }

                string archiveFilePath = Path.Combine(archiveDirectory, file.Name);

                if (File.Exists(archiveFilePath))
                {
                    string uniqueName = string.Format("{0}_{1:yyyyMMddHHmmss}{2}", Path.GetFileNameWithoutExtension(file.Name), DateTime.Now, file.Extension);
                    archiveFilePath = Path.Combine(archiveDirectory, uniqueName);
                    Logger.LogWarning($"Archive file '{file.Name}' exists. Saving as '{uniqueName}'.");
                }

                File.Move(file.FullName, archiveFilePath);
                Console.WriteLine($"Archived file: {file.Name} to {archiveFilePath}");
                Logger.LogInfo($"Archived file: {file.Name} to {archiveFilePath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR archiving file '{file.FullName}': {ex.Message}");
                Logger.LogError($"Error archiving file {file.FullName}: {ex.Message}");
            }
        }

    }

    // --- Custom Exception Classes (Keep these) ---
    public class ReportLoadingException : Exception { public ReportLoadingException(string message, Exception innerException) : base(message, innerException) { } }
    public class ReportExportException : Exception { public ReportExportException(string message, Exception innerException) : base(message, innerException) { } }
    public class FileCleanupException : Exception { public FileCleanupException(string message, Exception innerException) : base(message, innerException) { } }
}
