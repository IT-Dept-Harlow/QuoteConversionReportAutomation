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
    /// Removed internal file cleanup/archiving logic.
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
        /// <param name="statusStrip">The StatusStrip control (optional, ignored in wrapper).</param>
        /// <exception cref="ArgumentException">Thrown if required paths are null or empty.</exception>
        /// <exception cref="ReportLoadingException">Thrown if the report fails to load.</exception>
        /// <exception cref="ReportExportException">Thrown if the report fails to export.</exception>
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
                    // *** REMOVED Call to CleanupOldFiles ***
                    // string outputDir = Path.GetDirectoryName(reportOutputLocation);
                    // if (!string.IsNullOrEmpty(outputDir))
                    // {
                    //     CleanupOldFiles(outputDir, statusStrip); // Pass null for statusStrip if unavailable
                    // }

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
    }

    // --- Custom Exception Classes (Keep these) ---
    public class ReportLoadingException : Exception { public ReportLoadingException(string message, Exception innerException) : base(message, innerException) { } }
    public class ReportExportException : Exception { public ReportExportException(string message, Exception innerException) : base(message, innerException) { } }
}
