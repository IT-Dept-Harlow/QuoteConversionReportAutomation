// C# 10+ Features
namespace conversionTest
{
    // --- Global Usings ---
    using Microsoft.Extensions.Configuration;
    using System;
    using System.Collections.Generic; // Added back for List<>
    using System.Diagnostics;
    using System.Drawing; // Keep for Color
    using System.IO;
    using System.Windows.Forms;
    using System.Threading;
    using System.Threading.Tasks; // Keep for Task
    using ReportWrapperCommon; // For ReportRequest/Response
    using JR.Utils.GUI.Forms; // Added for MessageBox

    using QuoteConversionReportAutomation; // For EmailUtility, ExcelCopyData

    /// <summary>
    /// Represents the main form of the Quote Conversion Report Automation application.
    /// Orchestrates report generation and processing by coordinating with manager classes.
    /// Handles UI events and delegates UI updates to UIManager.
    /// Manages the Auto-Run timer, delegating the check logic to AutoRunManager.
    /// Includes handling for a "Custom" report type triggered by manual date changes.
    /// </summary>
    public partial class Form1 : Form
    {
        #region Fields and Properties

        // --- Dependencies ---
        private readonly IConfiguration _configuration;
        private readonly EmailUtility _emailUtility;
        private readonly UIManager _uiManager;
        private readonly ReportProcessManager _processManager;
        private readonly NamedPipeCommunicator _pipeCommunicator;
        private readonly AutoRunManager _autoRunManager;
        private readonly ExcelCopyData _excelProcessor;

        // --- Application Info ---
        private const string AppVersion = "1.6.2"; // Reflects Logger changes

        // --- State Variables (Remaining in Form1) ---
        /// <summary>Stores the file path of the raw Excel report generated (output of Button 1).</summary>
        private string _generatedReportPath = string.Empty;
        /// <summary>Stores the file path of the final processed Excel analysis file (output of Button 2).</summary>
        private string _generatedAnalysisFilePath = string.Empty;
        /// <summary>Stores the date the application was loaded, used for default date calculations.</summary>
        private DateTime _today;
        /// <summary>Stores the current financial year string (e.g., "2023_24"), initialized in Form1_Load.</summary>
        private string _financialYear = string.Empty;
        /// <summary>Flag to prevent date change events from triggering combo box change when code is setting dates.</summary>
        private bool _programmaticallyChangingDates = false;


        // --- Configuration Paths (Needed for Instantiation) ---
        private static readonly string appSettingsBasePath = DetermineAppSettingsBasePath(); // Helper to find path
        private readonly string _appSettingsPath = Path.Combine(appSettingsBasePath, "appsettings.json");

        // --- Report Type Constants ---
        private const int DailyReportIndex = 0;
        private const int WeeklyReportIndex = 1;
        private const int MonthlyReportIndex = 2;
        private const int QuarterlyReportIndex = 3;
        private const int AnnualReportIndex = 4;
        private const int CustomReportIndex = 5; // <<< ADDED Custom Index (Ensure this matches ComboBox item order)

        // --- Build Configuration Helper ---
        /// <summary>Gets a value indicating whether the application is running in DEBUG configuration.</summary>
        private static bool IsDebug =>
#if DEBUG
            true;
#else
            false;
#endif

        // --- Configuration Properties (Read from _configuration) ---
        /// <summary>Gets the path to the Crystal Report file (.rpt) from configuration.</summary>
        private string CrystalReportLocation => _configuration["settings:CrystalReportPath"] ?? string.Empty;

        // --- Dynamic Path Properties (Depend on UI state or config) ---
        /// <summary>Gets the calculated output path for the raw Crystal Report export file.</summary>
        public string ReportOutputLocation
        {
            get
            {
                // Consider moving base dir to config
                string baseDir = _configuration["settings:RawReportExportBaseDir"]
                    ?? $@"C:\Users\{Environment.UserName}\Harlow Printing\IT Projects - Documents\Dashboard Datasets\Raw_data\Quotes conversion\Estimate Reports Exports";
                string fileName = $"{endDatePicker.Value:yyyyMMdd}_EstimateSuccessReport_Raw.xlsx";
                string subFolder = reportTypeComboBox.SelectedIndex switch
                {
                    DailyReportIndex => "Daily Reports",
                    WeeklyReportIndex => "Weekly Reports",
                    MonthlyReportIndex => "Monthly Reports",
                    QuarterlyReportIndex => "Quarterly reports",
                    AnnualReportIndex => "Annual Reports",
                    CustomReportIndex => "Custom Reports", // <<< ADDED Custom Folder
                    _ => "Other Reports",
                };
                string fullPath = Path.Combine(baseDir, subFolder, fileName);
                try
                {
                    // Folder structure for Custom reports is handled by FolderCreation utility
                    // Only create top-level folder here if needed, subfolders handled later
                    if (reportTypeComboBox.SelectedIndex != CustomReportIndex)
                    {
                        string? directoryPath = Path.GetDirectoryName(fullPath);
                        if (!string.IsNullOrEmpty(directoryPath)) { Directory.CreateDirectory(directoryPath); }
                        else { Logger.LogWarning($"Could not determine directory path from '{fullPath}' for raw report output."); }
                    }
                    // For Custom, the timestamped folder will be created by FolderCreation later
                }
                catch (Exception ex) { Logger.LogError($"Failed to create directory '{Path.GetDirectoryName(fullPath)}': {ex.Message}"); }
                return fullPath;
            }
        }
        /// <summary>Gets the calculated path to the appropriate Excel template file.</summary>
        public string ExcelTemplateLocation
        {
            get
            {
                string baseDir = _configuration["settings:TemplateBaseDir"]
                   ?? $@"C:\Users\{Environment.UserName}\Harlow Printing\IT Projects - Documents\Dashboard Datasets\Raw_data\Quotes conversion\TEMPLATE\";
                string templateName = reportTypeComboBox.SelectedIndex switch
                {
                    MonthlyReportIndex or QuarterlyReportIndex or AnnualReportIndex => "TEMPLATE_Estimate Success Rate_Monthly.xlsx",
                    CustomReportIndex => "TEMPLATE_Estimate Success Rate_Monthly.xlsx", // <<< Use Monthly template for Custom? Adjust if needed
                    _ => "TEMPLATE_Estimate Success Rate.xlsx" // Daily and Weekly use the same template
                };
                return Path.Combine(baseDir, templateName);
            }
        }
        /// <summary>Gets the base directory where the final processed analysis file will be saved.</summary>
        public string ExcelFinalSaveLocation => _configuration["settings:ExcelFinalSaveLocation"]
            ?? $@"C:\Users\{Environment.UserName}\Harlow Printing\IT Projects - Documents\Dashboard Datasets\Raw_data\Quotes conversion\Estimates\";


        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="Form1"/> class.
        /// Instantiates manager classes and sets up dependencies.
        /// </summary>
        /// <param name="configuration">The application configuration provider.</param>
        /// <exception cref="ArgumentNullException">Thrown if configuration is null.</exception>
        public Form1(IConfiguration configuration)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

            Debug.WriteLine($"[{DateTime.Now:HH:mm:ss.fff zzz}] Form1 Constructor: Initializing components...");
            try
            {
                InitializeComponent(); // Initialize controls defined in the designer
                Debug.WriteLine($"[{DateTime.Now:HH:mm:ss.fff zzz}] Form1 Constructor: InitializeComponent() completed.");

                // --- Instantiate Dependencies and Managers ---
                _emailUtility = new EmailUtility(_configuration);
                _excelProcessor = new ExcelCopyData(); // Instantiate non-static Excel processor

                // Instantiate UIManager, passing all required controls
                _uiManager = new UIManager(
                    this, menuStrip1, mainStatusStrip, statusLabel, autoRunStatusLabel,
                    darkModeToolStripMenuItem, createReportButton, processEmailButton,
                    toggleAutoRunButton, viewReportButton, viewAnalysisButton,
                    reportTypeComboBox, startDatePicker, endDatePicker,
                    financialYearComboBox, financialYearLabel, sendToFemiOnlyCheckBox,
                    emailRecipientLabel
                );

                // Instantiate Process Manager
                string wrapperExePath = Path.GetFullPath(_configuration["settings:WrapperExePath"] ?? "CrystalReportWrapper.exe");
                _processManager = new ReportProcessManager(wrapperExePath);

                // Instantiate Pipe Communicator
                _pipeCommunicator = new NamedPipeCommunicator();

                // Instantiate AutoRun Manager
                _autoRunManager = new AutoRunManager(
                    _configuration,
                    _emailUtility,
                    _processManager,
                    _pipeCommunicator,
                    _uiManager,
                    _excelProcessor, // Pass the Excel processor instance
                    _appSettingsPath
                );

                // *** ADDED: Wire up date picker events AFTER managers are created ***
                this.startDatePicker.ValueChanged += new System.EventHandler(this.DatePicker_ValueChanged);
                this.endDatePicker.ValueChanged += new System.EventHandler(this.DatePicker_ValueChanged);
                Logger.LogDebug("Date picker event handlers wired up.");

            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[{DateTime.Now:HH:mm:ss.fff zzz}] Form1 Constructor: CRITICAL ERROR during InitializeComponent or Manager Instantiation! Exception: {ex}");
                Logger.LogCritical($"CRITICAL ERROR during Form Initialization: {ex.Message}", ex);
                MessageBox.Show($"A critical error occurred initializing the application:\n\n{ex.Message}\n\nThe application cannot continue.",
                                        "Initialization Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                throw; // Re-throw to terminate
            }
            Debug.WriteLine($"[{DateTime.Now:HH:mm:ss.fff zzz}] Form1 Constructor: Exiting.");
        }

        #endregion

        #region Form Load / Closing

        /// <summary>
        /// Handles the Load event of the form. Initializes UI state via UIManager,
        /// validates configuration paths, ensures the wrapper service is running,
        /// and sets up the initial form state.
        /// </summary>
        private async void Form1_Load(object sender, EventArgs e)
        {
            Debug.WriteLine($"[{DateTime.Now:HH:mm:ss.fff zzz}] Form1_Load: Entered.");
            _uiManager.UpdateStatusMain("Loading application...");
            try
            {
                // Initialize date/year fields
                _today = DateTime.Today;
                _financialYear = _excelProcessor.GetCurrentFinancialYear(true); // Use instance
                Debug.WriteLine($"[{DateTime.Now:HH:mm:ss.fff zzz}] Form1_Load: Date='{_today:yyyy-MM-dd}', FY='{_financialYear}'");

                Logger.LogInfo("Form Loading...");

                // --- Configuration Validation (Paths needed for core functionality) ---
                string crystalReportPath = CrystalReportLocation; // Use the property here
                string wrapperExePath = _configuration["settings:WrapperExePath"] ?? string.Empty; // Get raw path for check
                bool configValid = true;

                if (string.IsNullOrEmpty(crystalReportPath) || !File.Exists(crystalReportPath))
                {
                    Logger.LogError($"Config 'settings:CrystalReportPath' missing or file not found: '{crystalReportPath}'. Report generation disabled.");
                    MessageBox.Show($"Warning: Crystal Report file path is missing or invalid ('{crystalReportPath}').\n\nReport generation (Button 1) will be disabled.",
                                            "Configuration Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    configValid = false;
                }
                if (string.IsNullOrEmpty(wrapperExePath) || !File.Exists(Path.GetFullPath(wrapperExePath)))
                {
                    Logger.LogError($"Config 'settings:WrapperExePath' missing or file not found: '{wrapperExePath}'. Report generation disabled.");
                    MessageBox.Show($"Warning: Crystal Report Wrapper executable path is missing or invalid ('{wrapperExePath}').\n\nReport generation (Button 1) will be disabled.",
                                            "Configuration Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    configValid = false;
                }

                // --- UI Initialization ---
                Text = $"Quote Conversion Automation - {(IsDebug ? "DEBUG" : "RELEASE")} - v{AppVersion}";
                StartPosition = FormStartPosition.CenterScreen;

                // Populate Dropdowns
                PopulateFinancialYearDropdown(); // Keep this simple logic here or move if complex
                if (financialYearComboBox.Items.Count > 0) financialYearComboBox.SelectedIndex = 0;
                financialYearComboBox.DropDownStyle = ComboBoxStyle.DropDownList;

                // Add "Custom" to ComboBox if not already present (ensure Designer has items 0-4)
                if (reportTypeComboBox.Items.Count == 5) // Only add if the first 5 exist
                {
                    reportTypeComboBox.Items.Add("Custom");
                    Logger.LogDebug("Added 'Custom' to reportTypeComboBox items.");
                }
                else if (reportTypeComboBox.Items.Count < 5)
                {
                    Logger.LogWarning("reportTypeComboBox has fewer than 5 items. 'Custom' item may not have correct index.");
                }


                // Set default report type *before* applying theme or checking template
                if (reportTypeComboBox.Items.Count > DailyReportIndex)
                    reportTypeComboBox.SelectedIndex = DailyReportIndex;
                else if (reportTypeComboBox.Items.Count > 0)
                    reportTypeComboBox.SelectedIndex = 0;
                reportTypeComboBox.DropDownStyle = ComboBoxStyle.DropDownList;


                // --- Theme & Menu Setup ---
                bool useDarkMode = UIManager.IsWindowsDarkModeEnabled();
                darkModeToolStripMenuItem.Checked = useDarkMode;
                _uiManager.ApplyTheme(useDarkMode);

                // --- Auto Run Setup ---
                // Event handlers wired in designer
                _uiManager.UpdateAutoRunUI(dailyCheckTimer.Enabled, false, useDarkMode); // Initial UI update

                // Trigger SelectedIndexChanged AFTER setting up theme and auto-run UI
                reportTypeComboBox_SelectedIndexChanged(reportTypeComboBox, EventArgs.Empty);

                // Initial button states reset
                _uiManager.ResetButtonStatesAfterTypeChange(configValid);


                // --- Ensure Wrapper Service is Running ---
                _uiManager.UpdateStatusMain("Checking report service...");
                IProgress<string> loadProgress = new Progress<string>(status => _uiManager.UpdateStatusMain(status));
                bool wrapperOk = await _processManager.EnsureWrapperIsRunningAsync(loadProgress); // Pass progress reporter
                if (!wrapperOk)
                {
                    // If wrapper failed to start, ensure create button is disabled
                    _uiManager.ResetUIOnError("Config Error", false, false, false, IsDailySelected(), dailyCheckTimer.Enabled, useDarkMode, false, string.Empty);
                }


                Logger.LogInfo("Form Load Initialisation Complete.");
                _uiManager.UpdateStatusMain("Ready");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[{DateTime.Now:HH:mm:ss.fff zzz}] Form1_Load: CRITICAL ERROR! Exception: {ex}");
                Logger.LogCritical($"CRITICAL ERROR during Form_Load: {ex.Message}", ex);
                MessageBox.Show($"A critical error occurred loading the application:\n\n{ex.Message}\n\nThe application may not function correctly.",
                                        "Application Load Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                _uiManager.UpdateStatusMain("Error during load.");
            }
        }

        /// <summary>
        /// Handles the FormClosing event. Ensures the background wrapper process is terminated
        /// and stops the auto-run timer.
        /// </summary>
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            dailyCheckTimer.Stop(); // Stop the timer
            _processManager.TerminateWrapperProcess(); // Terminate background service via manager
        }

        #endregion

        #region Event Handlers (Delegating to Managers/Helpers)

        /// <summary>
        /// Handles the Click event for Button 1 (Create Report).
        /// Validates input, ensures wrapper service, sends request via NamedPipeCommunicator,
        /// and updates UI via UIManager.
        /// </summary>
        private async void createReportButton_Click(object sender, EventArgs e)
        {
            // Disable buttons and update status via UIManager
            _uiManager.SetActionButtonsEnabled(false);
            _uiManager.SetOtherControlsEnabled(false, financialYearComboBox.Visible);
            _uiManager.UpdateStatusMain("Validating request...");
            createReportButton.Text = "Requesting...";
            Logger.LogDebug("Create Report Button Clicked: Requesting Crystal Report generation.");

            using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(6)); // Timeout for this operation
            IProgress<string> progress = new Progress<string>(status => _uiManager.UpdateStatusMain(status));

            try
            {
                // Validation (keep simple validation here or move to helper/manager)
                if (!ValidateInputDates()) { ResetUIStateOnError("Date Error"); return; }
                if (!ValidateFinancialYearSelection()) { ResetUIStateOnError("FY Mismatch"); return; }

                string crystalReportPath = CrystalReportLocation; // Use the property here
                if (string.IsNullOrEmpty(crystalReportPath) || !File.Exists(crystalReportPath))
                { throw new InvalidOperationException("Crystal Report location is invalid or file not found."); }

                // Ensure wrapper is running via ProcessManager
                // _uiManager.UpdateStatusMain("Checking report service..."); // Progress reporter handles this now
                if (!await _processManager.EnsureWrapperIsRunningAsync(progress, cts.Token))
                { throw new InvalidOperationException($"Failed to start or connect to the report service."); }

                // Prepare request
                string reportOutputPath = ReportOutputLocation; // Get path based on current UI state
                var request = new ReportRequest
                {
                    CrystalReportLocation = crystalReportPath, // Pass the path
                    ReportOutputLocation = reportOutputPath,
                    ReportDateFrom = startDatePicker.Value,
                    ReportDateTo = endDatePicker.Value
                };

                // Send request via PipeCommunicator
                // _uiManager.UpdateStatusMain("Connecting to report service..."); // Progress reporter handles this now
                Logger.LogInfo("Attempting Named Pipe communication...");
                ReportResponse? response = await _pipeCommunicator.SendRequestReceiveResponseAsync(request, progress, cts.Token);

                // Process response
                if (response?.Success == true && !string.IsNullOrEmpty(response.OutputPath) && File.Exists(response.OutputPath))
                {
                    _generatedReportPath = response.OutputPath; // Store result path
                    Logger.LogInfo($"Report generated successfully by wrapper: {_generatedReportPath}");
                    // Update UI for successful report creation
                    _uiManager.SetActionButtonsEnabled(false); // Keep create disabled, enable process
                    _uiManager.SetOtherControlsEnabled(true, financialYearComboBox.Visible); // Re-enable inputs
                    createReportButton.Text = "Report Created";
                    processEmailButton.Enabled = true;
                    _uiManager.ShowViewReportButton(true, _generatedReportPath); // Show view button with path
                    _uiManager.ShowViewAnalysisButton(false); // Hide analysis button
                    _generatedAnalysisFilePath = string.Empty; // Clear analysis path

                    _uiManager.UpdateStatusMain("Report created successfully.");
                }
                else
                {
                    string errorMessage = response?.ErrorMessage ?? "Unknown error from report service.";
                    if (response?.Success == true && (string.IsNullOrEmpty(response.OutputPath) || !File.Exists(response.OutputPath)))
                    { errorMessage = $"Report service indicated success, but the output file path ('{response?.OutputPath ?? "NULL"}') is invalid or the file does not exist."; Logger.LogError(errorMessage); }
                    throw new Exception($"Report generation failed: {errorMessage}");
                }
            }
            catch (OperationCanceledException)
            {
                Logger.LogWarning("Report generation request cancelled or timed out.");
                MessageBox.Show("The report generation request timed out or was cancelled.", "Timeout / Cancelled", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                ResetUIStateOnError("Cancelled");
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error during Create Report operation: {ex}");
                MessageBox.Show($"An error occurred while requesting the report:\n\n{ex.Message}", "Report Request Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                ResetUIStateOnError("Error");
            }
        }


        /// <summary>
        /// Handles the Click event for Button 2 (Process & Email).
        /// Checks existing files, processes raw report using ExcelCopyData (instance),
        /// handles manual refresh via ReportHelper, sends email via EmailUtility, updates UI via UIManager.
        /// </summary>
        private async void processEmailButton_Click(object sender, EventArgs e)
        {
            // Disable buttons and update status via UIManager
            _uiManager.SetActionButtonsEnabled(false);
            _uiManager.SetOtherControlsEnabled(false, financialYearComboBox.Visible);
            processEmailButton.Text = "Processing..."; // Direct update ok
            _uiManager.UpdateStatusMain("Starting Excel processing...");
            Logger.LogDebug("Process & Email Button Clicked: Processing Excel report.");

            using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(15));
            var token = cts.Token;

            // Progress reporters
            IProgress<ProgressReport> excelProgress = new Progress<ProgressReport>(report => _uiManager.UpdateStatusMain(report.Message));
            IProgress<string> progress = new Progress<string>(message => _uiManager.UpdateStatusMain(message)); // General progress

            string? finalFilePath = null;
            int reportType = reportTypeComboBox.SelectedIndex;
            // *** UPDATED: Custom reports also require manual refresh ***
            bool requiresManualRefresh = reportType is MonthlyReportIndex or QuarterlyReportIndex or AnnualReportIndex or CustomReportIndex;
            string baseSaveLocation = ExcelFinalSaveLocation;
            DateTime reportDate = endDatePicker.Value; // Use the end date from the picker

            try
            {
                // Check if final file exists (using ExcelCopyData instance)
                string? expectedFinalPath = _excelProcessor.GetExpectedFinalFilePath(reportType, baseSaveLocation, reportDate);
                if (expectedFinalPath != null && File.Exists(expectedFinalPath))
                {
                    Logger.LogWarning($"Expected final file already exists: {expectedFinalPath}");
                    DialogResult dr = MessageBox.Show(
                        $"The report file '{Path.GetFileName(expectedFinalPath)}' already exists for this period.\n\n" +
                        "Do you want to skip processing and send this existing file?",
                        "File Already Exists", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                    if (dr == DialogResult.Yes)
                    {
                        Logger.LogInfo("User chose to send existing file.");
                        finalFilePath = expectedFinalPath;
                        _generatedAnalysisFilePath = finalFilePath; // Store path
                        _uiManager.ShowViewAnalysisButton(true, finalFilePath); // Update UI

                        bool proceedToEmail = true;
                        if (requiresManualRefresh)
                        {
                            // _uiManager.UpdateStatusMain("Waiting for manual Excel refresh..."); // Set within HandleManualExcelRefreshAsync
                            // Use ReportHelper for manual refresh logic
                            proceedToEmail = await HandleManualExcelRefreshAsync(finalFilePath, token);
                            if (!proceedToEmail) { _uiManager.UpdateStatusMain("Manual refresh/confirmation cancelled."); ResetUIStateOnError("Cancelled"); return; }
                            _uiManager.UpdateStatusMain("Manual refresh confirmed. Preparing email...");
                        }
                        if (proceedToEmail) { await SendCompletionEmailAsync(finalFilePath, progress, token); } // Pass IProgress<string>
                        else { ResetUIStateOnError("Create Report"); } // Reset UI if email skipped
                        return; // Exit after handling existing file
                    }
                    else
                    {
                        Logger.LogInfo("User chose to overwrite/regenerate the existing file.");
                        try { File.Delete(expectedFinalPath); Logger.LogInfo($"Deleted existing file: {expectedFinalPath}"); }
                        catch (Exception delEx)
                        {
                            Logger.LogError($"Failed to delete existing file '{expectedFinalPath}': {delEx.Message}");
                            MessageBox.Show($"Could not delete the existing report file:\n{expectedFinalPath}\n\nPlease ensure the file is not open and try again.\n\nError: {delEx.Message}",
                                            "File Deletion Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            ResetUIStateOnError("File Error"); return;
                        }
                    }
                }

                // --- Proceed with generating a new file ---
                string reportToProcessPath = _generatedReportPath; // Path from step 1
                string templatePath = ExcelTemplateLocation;
                if (string.IsNullOrEmpty(templatePath) || !File.Exists(templatePath))
                { throw new FileNotFoundException($"The required Excel template file was not found.", templatePath); }
                if (string.IsNullOrEmpty(reportToProcessPath) || !File.Exists(reportToProcessPath))
                { throw new FileNotFoundException("The raw report file to process was not found. Please generate the report first.", reportToProcessPath); }
                if (string.IsNullOrEmpty(baseSaveLocation))
                { throw new InvalidOperationException("The base save location for the final report is not configured correctly."); }

                string financialYear = financialYearComboBox.SelectedItem?.ToString() ?? _financialYear;
                string sourceSheet = "Sheet1"; // Consider making configurable
                string destSheet = "DATA";     // Consider making configurable

                // Process Excel using instance method
                finalFilePath = await _excelProcessor.ProcessExcelReportAsync(
                    financialYear, reportType,
                    reportToProcessPath, sourceSheet, baseSaveLocation, templatePath, destSheet,
                    1, 1, excelProgress, reportDate, token); // Pass IProgress<ProgressReport>

                if (string.IsNullOrEmpty(finalFilePath) || !File.Exists(finalFilePath))
                {
                    if (token.IsCancellationRequested) { throw new OperationCanceledException("Excel processing was cancelled."); }
                    else { throw new Exception("Excel processing failed to produce a final file. Check logs for details."); }
                }

                _generatedAnalysisFilePath = finalFilePath; // Store path
                Logger.LogInfo($"Excel processing completed. Final file: {finalFilePath}");
                _uiManager.UpdateStatusMain("Excel processing complete.");
                _uiManager.ShowViewAnalysisButton(true, finalFilePath); // Update UI

                bool proceedToEmailAfterGenerate = true;
                if (requiresManualRefresh)
                {
                    // _uiManager.UpdateStatusMain("Waiting for manual Excel refresh..."); // Set within HandleManualExcelRefreshAsync
                    // Use ReportHelper for manual refresh logic
                    proceedToEmailAfterGenerate = await HandleManualExcelRefreshAsync(finalFilePath, token);
                    if (!proceedToEmailAfterGenerate) { _uiManager.UpdateStatusMain("Manual refresh/confirmation cancelled."); ResetUIStateOnError("Cancelled"); return; }
                    _uiManager.UpdateStatusMain("Manual refresh confirmed. Preparing email...");
                }

                if (proceedToEmailAfterGenerate)
                { await SendCompletionEmailAsync(finalFilePath, progress, token); } // Pass IProgress<string>
                else { ResetUIStateOnError("Create Report"); } // Reset UI if email skipped

            }
            catch (OperationCanceledException)
            {
                Logger.LogWarning("Excel processing or subsequent step cancelled.");
                ResetUIStateOnError("Cancelled");
            }
            catch (FileNotFoundException fnfEx)
            {
                Logger.LogError($"File not found during Process & Email operation: {fnfEx}");
                MessageBox.Show(fnfEx.Message, "File Not Found Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                ResetUIStateOnError("File Error");
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error during Process & Email operation: {ex}");
                MessageBox.Show($"An unexpected error occurred during processing:\n\n{ex.Message}", "Processing Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                ResetUIStateOnError("Error");
            }
        }

        /// <summary>
        /// Handles the Click event for the "View Report" button. Uses ReportHelper.
        /// </summary>
        private void viewReportButton_Click(object sender, EventArgs e)
        {
            ReportHelper.OpenFileWithDefaultApp(_generatedReportPath, "raw report output");
        }

        /// <summary>
        /// Handles the Click event for the "View Analysis" button. Uses ReportHelper.
        /// </summary>
        private void viewAnalysisButton_Click(object sender, EventArgs e)
        {
            ReportHelper.OpenFileWithDefaultApp(_generatedAnalysisFilePath, "processed analysis file");
        }

        /// <summary>
        /// Handles changes in the Report Type dropdown. Updates date pickers and UI visibility via UIManager.
        /// Sets a flag to prevent date picker events from re-triggering changes.
        /// </summary>
        private void reportTypeComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (sender is not ComboBox comboBox || comboBox.SelectedItem == null) return;

            int selectedIndex = comboBox.SelectedIndex;
            // If user selects "Custom" directly, don't change dates
            if (selectedIndex == CustomReportIndex)
            {
                Logger.LogInfo("Custom report type selected directly. Dates not changed.");
                // Update Femi/Paul labels based on Custom selection (assuming same as non-Daily)
                UIManager.SafeControlUpdate(sendToFemiOnlyCheckBox, () => { sendToFemiOnlyCheckBox.Visible = true; });
                UIManager.SafeControlUpdate(emailRecipientLabel, () => { emailRecipientLabel.Visible = false; });
                // Reset buttons for the new type
                bool configValid = CheckConfigValidity();
                _uiManager.ResetButtonStatesAfterTypeChange(configValid);
                return;
            }


            DateTime todayValue = _today; // Use the stored load date
            bool showFinYear = true; // Default
            DateTime dateFrom = todayValue;
            DateTime dateTo = todayValue;

            // Set flag to indicate code is changing dates
            _programmaticallyChangingDates = true;
            Logger.LogTrace("reportTypeComboBox_SelectedIndexChanged: Setting _programmaticallyChangingDates = true");

            try
            {
                // Calculate date range using ReportHelper or local logic
                (dateFrom, dateTo, showFinYear) = selectedIndex switch
                {
                    DailyReportIndex => (ReportHelper.GetPreviousWorkday(todayValue), ReportHelper.GetPreviousWorkday(todayValue), true),
                    WeeklyReportIndex => (todayValue.AddDays(-13), todayValue, true),
                    MonthlyReportIndex => (ReportHelper.CalculateMonthlyRange(todayValue).DateFrom, ReportHelper.CalculateMonthlyRange(todayValue).DateTo, false),
                    QuarterlyReportIndex => (ReportHelper.CalculateQuarterlyRange(todayValue).DateFrom, ReportHelper.CalculateQuarterlyRange(todayValue).DateTo, false),
                    AnnualReportIndex => (new DateTime(todayValue.Year - 1, 1, 1), new DateTime(todayValue.Year - 1, 12, 31), false),
                    _ => (todayValue, todayValue, true) // Default fallback (shouldn't hit if Custom is handled above)
                };

                Logger.LogInfo($"Report Type changed (Index {selectedIndex}). Range: {dateFrom:d} to {dateTo:d}. ShowFinYear: {showFinYear}");

                // Safely update UI controls via UIManager
                UIManager.SafeControlUpdate(startDatePicker, () => { startDatePicker.Value = dateFrom; });
                UIManager.SafeControlUpdate(endDatePicker, () => { endDatePicker.Value = dateTo; });
                UIManager.SafeControlUpdate(financialYearLabel, () => { financialYearLabel.Visible = showFinYear; });
                UIManager.SafeControlUpdate(financialYearComboBox, () =>
                {
                    financialYearComboBox.Visible = showFinYear;
                    financialYearComboBox.Enabled = showFinYear; // Enable based on visibility
                    if (showFinYear && financialYearComboBox.Items.Count == 0)
                    { PopulateFinancialYearDropdown(); } // Repopulate if needed
                });

                // Update visibility of Femi checkbox vs Paul label via UIManager
                bool isDaily = IsDailySelected(); // Check based on current index
                UIManager.SafeControlUpdate(sendToFemiOnlyCheckBox, () => { sendToFemiOnlyCheckBox.Visible = !isDaily; });
                UIManager.SafeControlUpdate(emailRecipientLabel, () =>
                {
                    emailRecipientLabel.Visible = isDaily;
                    if (isDaily) { emailRecipientLabel.Text = "Emailing Daily report to Paul"; }
                });

                // Check template path (keep this check here as it depends on current selection)
                string currentTemplatePath = ExcelTemplateLocation;
                if (string.IsNullOrEmpty(currentTemplatePath) || !File.Exists(currentTemplatePath))
                    Logger.LogWarning($"Excel template file for the selected report type ({comboBox.SelectedItem}) is missing or invalid ('{currentTemplatePath}'). Processing might fail.");
                else
                    Logger.LogDebug($"Template path for type {selectedIndex}: {currentTemplatePath}");

                // Reset button states via UIManager
                bool configValid = CheckConfigValidity(); // Check current config state
                _uiManager.ResetButtonStatesAfterTypeChange(configValid);
            }
            finally
            {
                // Always unset the flag
                _programmaticallyChangingDates = false;
                Logger.LogTrace("reportTypeComboBox_SelectedIndexChanged: Setting _programmaticallyChangingDates = false");
            }
        }


        /// <summary>
        /// Handles the Click event for the Auto Run toggle button.
        /// Enables or disables the daily check timer and updates UI via UIManager.
        /// </summary>
        private void toggleAutoRunButton_Click(object sender, EventArgs e)
        {
            dailyCheckTimer.Enabled = !dailyCheckTimer.Enabled;
            // AutoRunManager handles resetting its internal 'done for today' flag if needed
            _uiManager.UpdateAutoRunUI(dailyCheckTimer.Enabled, false, darkModeToolStripMenuItem.Checked); // Update UI based on new timer state
            Logger.LogInfo($"Daily Auto Run {(dailyCheckTimer.Enabled ? "Enabled" : "Disabled")} by user.");
        }

        /// <summary>
        /// Handles the Tick event for the daily check timer. Delegates the core logic to AutoRunManager.
        /// Manages stopping/starting the timer around the async check.
        /// </summary>
        private async void dailyCheckTimer_Tick(object sender, EventArgs e)
        {
            // Prevent re-entrancy if the previous tick's task is somehow still running
            if (!dailyCheckTimer.Enabled) return; // Should not happen if tick fires, but safety check

            bool originallyEnabled = dailyCheckTimer.Enabled; // Store state
            dailyCheckTimer.Stop(); // Stop timer during check
            Logger.LogTrace("dailyCheckTimer_Tick: Timer stopped, calling AutoRunManager.PerformDailyCheckAsync.");

            try
            {
                // Delegate the check and execution logic to the AutoRunManager
                // Pass the original timer state so AutoRunManager knows if it was user-enabled
                await _autoRunManager.PerformDailyCheckAsync(originallyEnabled);
            }
            catch (Exception ex)
            {
                // Catch unexpected errors from the manager itself (should be rare if manager handles errors)
                Logger.LogCritical($"CRITICAL ERROR during AutoRunManager.PerformDailyCheckAsync: {ex}");
                _uiManager.UpdateStatusMain("Critical AutoRun Error!");
                // Consider permanently disabling timer or showing critical error message
                originallyEnabled = false; // Prevent restart after critical failure
            }
            finally
            {
                Logger.LogTrace("dailyCheckTimer_Tick: AutoRunManager.PerformDailyCheckAsync completed.");
                // --- Decide whether to restart timer AFTER execution ---
                if (originallyEnabled)
                {
                    // Check if it should still be enabled (e.g., no critical error occurred)
                    // This logic might need refinement based on how errors are handled in AutoRunManager
                    dailyCheckTimer.Start(); // Restart timer for subsequent days' checks
                    Logger.LogDebug("Auto Run: Timer restarted for future checks.");
                }
                else
                {
                    Logger.LogDebug("Auto Run: Timer remains stopped as it wasn't originally enabled OR critical error occurred.");
                }

                // --- Reset UI State ---
                // Reset UI to reflect the state after the auto-run attempt (or lack thereof)
                // UIManager's ResetUIOnError handles re-enabling controls correctly
                ResetUIStateOnError("Create Report"); // Resets based on current file existence etc.

                // Ensure main status is reset to Ready after a short delay if needed
                // (ResetUIOnError handles scheduling this)
                // _uiManager.UpdateStatusMain("Ready"); // Might be set too soon here
            }
        }


        /// <summary>
        /// Handles the Click event for the Dark Mode menu item. Toggles theme via UIManager.
        /// </summary>
        private void darkModeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // CheckOnClick property handles toggling the checked state automatically.
            _uiManager.ApplyTheme(darkModeToolStripMenuItem.Checked);
            // Re-apply auto-run UI colors based on the new theme
            _uiManager.UpdateAutoRunUI(dailyCheckTimer.Enabled, false, darkModeToolStripMenuItem.Checked); // Need to get actual 'isFinalStatus' if possible
        }

        /// <summary>
        /// Handles the Click event for the Help menu item. Displays help information.
        /// </summary>
        private void helpToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string helpTitle = $"Help - Quote Conversion v{AppVersion}";

            // Use StringBuilder to build the RTF string
            var helpMessageBuilder = new System.Text.StringBuilder();

            // --- FIX: Restore the full help text generation ---
            helpMessageBuilder.AppendLine("{\\rtf1\\ansi\\deff0{\\fonttbl{\\f0 Segoe UI;}}");
            helpMessageBuilder.AppendLine("{\\colortbl ;\\red0\\green0\\blue0;}");
            helpMessageBuilder.AppendLine("\\pard\\sa200\\sl276\\slmult1\\b\\fs24 Quote Conversion Automation Tool\\b0\\fs20\\par");
            helpMessageBuilder.AppendLine("\\par");
            helpMessageBuilder.AppendLine("This tool automates the process of generating and processing Estimate Success Rate reports.\\par");
            helpMessageBuilder.AppendLine("\\par");
            helpMessageBuilder.AppendLine("\\b How to Use:\\b0\\par");
            helpMessageBuilder.AppendLine("\\par");
            helpMessageBuilder.AppendLine("1.  \\b Select Report Type:\\b0  Choose Daily, Weekly, Monthly, Quarterly, Annual, or Custom from the dropdown. Dates will adjust automatically for standard types.\\par"); // Updated
            helpMessageBuilder.AppendLine("    * \\b Daily:\\b0  Dates will be set to the \\i previous working day\\i0  (Friday if today is Monday, otherwise yesterday).\\par");
            helpMessageBuilder.AppendLine("    * \\b Weekly/Daily:\\b0  Ensure the correct Financial Year is selected if visible.\\par");
            helpMessageBuilder.AppendLine("    * \\b Custom:\\b0  Select this or manually change the dates in the date pickers.\\par"); // Added
            helpMessageBuilder.AppendLine("\\par");
            helpMessageBuilder.AppendLine("2.  \\b Adjust Dates (Optional/Custom report):\\b0  You can manually change the 'From' and 'To' dates. Doing so will automatically select the 'Custom' report type.\\par"); // Updated
            helpMessageBuilder.AppendLine("\\par");
            // Escape the quotes within the string
            helpMessageBuilder.AppendLine("3.  \\b Create Raw Report:\\b0  Click the \\\"Create Report\\\" button. This contacts a background service to generate the raw data export from Crystal Reports. Wait for the status to show \\\"Report Created\\\". The filename will reflect the 'To' date.\\par");
            helpMessageBuilder.AppendLine("\\par");
            helpMessageBuilder.AppendLine("4.  \\b Process & Email:\\b0  Once the raw report is created, click the \\\"Process and Email\\\" button. This will:\\par");
            helpMessageBuilder.AppendLine("    * Copy data into the appropriate template.\\par");
            helpMessageBuilder.AppendLine("    * Extract unique customers.\\par");
            helpMessageBuilder.AppendLine("    * Perform calculations.\\par");
            helpMessageBuilder.AppendLine("    * Clean up unused rows.\\par");
            helpMessageBuilder.AppendLine("    * (For Weekly reports) Append data to the central Power BI source file.\\par");
            helpMessageBuilder.AppendLine("    * (For Monthly/Quarterly/Annual/Custom) Prompt you to open the file in Excel to Refresh All pivot tables, Save, and Close.\\par"); // Updated
            helpMessageBuilder.AppendLine("    * Send the final report via email to the configured recipients (or just Paul S. for automated Daily reports). The final filename will reflect the 'To' date (and timestamp for Custom).\\par"); // Updated
            helpMessageBuilder.AppendLine("\\par");
            helpMessageBuilder.AppendLine("5.  \\b View Files (Optional):\\b0  Use the \\\"View Report\\\" and \\\"View Analysis\\\" buttons after the corresponding steps are complete to open the generated files.\\par");
            helpMessageBuilder.AppendLine("\\par");
            helpMessageBuilder.AppendLine("6.  \\b Options Menu:\\b0\\par");
            helpMessageBuilder.AppendLine("    * \\b Dark Mode:\\b0  Toggle the visual theme.\\par");
            helpMessageBuilder.AppendLine("\\par");
            helpMessageBuilder.AppendLine("7.  \\b Auto Run Button:\\b0  Enable/Disable the automated daily report generation (runs around 8 AM for the \\i previous working day\\i0 ). The status is shown on the right of the status bar. The application checks the `appsettings.json` file to avoid running more than once per day. If the report has already run for the day, the timer stops checking until the next day/app restart.\\par");
            helpMessageBuilder.AppendLine("\\par");
            helpMessageBuilder.AppendLine("\\b Troubleshooting:\\b0\\par");
            helpMessageBuilder.AppendLine("\\par");
            helpMessageBuilder.AppendLine("* Ensure the Crystal Report Wrapper service is running (the app tries to start it).\\par");
            helpMessageBuilder.AppendLine("* Check file paths in `appsettings.json` if errors occur finding reports or templates.\\par");
            helpMessageBuilder.AppendLine("* Ensure the central weekly report file is accessible and not locked if appending fails.\\par");
            helpMessageBuilder.AppendLine("* Check the application logs located in the 'Logs' subfolder for detailed error information.\\par");
            helpMessageBuilder.AppendLine("* If auto-run fails to update `appsettings.json`, check file permissions for the application directory.\\par");
            helpMessageBuilder.AppendLine("* If you get an error refreshing a Slicer, remove it, then click into the Pivot table, in the PivotTable Fields on the right, Right Click customers and select add as slicer, move it back to where it was.\\par");
            // --- End of restored help text ---
            helpMessageBuilder.Append('}'); // Append the final closing brace without a newline

            string helpMessage = helpMessageBuilder.ToString();
            FlexibleMessageBox.ShowRtf(helpMessage, helpTitle, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        /// <summary>
        /// Event handler for when the value of either date picker changes.
        /// If the change was likely manual (not programmatic), sets the report type to Custom.
        /// </summary>
        private void DatePicker_ValueChanged(object sender, EventArgs e)
        {
            // If the code is currently changing the dates (e.g., in reportTypeComboBox_SelectedIndexChanged), ignore this event
            if (_programmaticallyChangingDates)
            {
                Logger.LogTrace("DatePicker_ValueChanged: Ignoring event as _programmaticallyChangingDates is true.");
                return;
            }

            // Check if the current selection is already Custom - no need to change it again
            if (reportTypeComboBox.SelectedIndex == CustomReportIndex)
            {
                Logger.LogTrace("DatePicker_ValueChanged: Report type is already Custom. Ignoring event.");
                return;
            }

            Logger.LogDebug("DatePicker_ValueChanged: Manual date change detected. Setting Report Type to Custom.");
            // Set the ComboBox to the Custom index
            // Use SafeControlUpdate in case this event somehow fires off the UI thread (less likely but safer)
            UIManager.SafeControlUpdate(reportTypeComboBox, () => {
                // Check index validity before setting
                if (reportTypeComboBox.Items.Count > CustomReportIndex)
                {
                    reportTypeComboBox.SelectedIndex = CustomReportIndex;
                }
                else
                {
                    Logger.LogError($"Cannot set Report Type to Custom. Index {CustomReportIndex} is out of bounds for ComboBox items ({reportTypeComboBox.Items.Count}).");
                }
            });

            // Note: Setting SelectedIndex will trigger reportTypeComboBox_SelectedIndexChanged,
            // but that handler now has logic to ignore the "Custom" selection directly.
        }


        #endregion

        #region Helper Methods (Remaining in Form1 or Adapted)

        /// <summary>
        /// Populates the Financial Year dropdown. Simple enough to keep here.
        /// </summary>
        private void PopulateFinancialYearDropdown()
        {
            UIManager.SafeControlUpdate(financialYearComboBox, () =>
            {
                financialYearComboBox.Items.Clear();
                string currentFY = _financialYear; // Use the stored FY
                if (!string.IsNullOrEmpty(currentFY))
                {
                    financialYearComboBox.Items.Add(currentFY);
                    string? previousFY = _excelProcessor.GetPreviousFinancialYear(currentFY);
                    if (!string.IsNullOrEmpty(previousFY)) { financialYearComboBox.Items.Add(previousFY); }
                }
                else { Logger.LogWarning("Could not determine current financial year for dropdown population."); financialYearComboBox.Items.Add("FY Unknown"); }
                if (financialYearComboBox.Items.Count > 0) { financialYearComboBox.SelectedIndex = 0; }
            });
        }

        /// <summary>
        /// Validates that the 'From' date is not after the 'To' date.
        /// </summary>
        private bool ValidateInputDates()
        {
            if (startDatePicker.Value.Date > endDatePicker.Value.Date)
            {
                Logger.LogError("Validation Failed: 'From' date cannot be after 'To' date.");
                MessageBox.Show("The 'From' date cannot be after the 'To' date.", "Date Range Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
            return true;
        }

        /// <summary>
        /// Validates if the selected date range falls within the selected Financial Year (if applicable).
        /// Uses instance method from ExcelCopyData. Skips validation for Custom reports.
        /// </summary>
        private bool ValidateFinancialYearSelection()
        {
            // Skip validation if Custom type is selected or if FY controls are hidden
            if (reportTypeComboBox.SelectedIndex == CustomReportIndex || !financialYearComboBox.Visible)
            {
                return true;
            }

            if (financialYearComboBox.SelectedItem != null)
            {
                string selectedFinYear = financialYearComboBox.SelectedItem.ToString()!;
                if (!_excelProcessor.IsFinancialYearValid(selectedFinYear, startDatePicker.Value, endDatePicker.Value))
                {
                    Logger.LogWarning($"Potential FY mismatch: Selected FY '{selectedFinYear}', Date Range '{startDatePicker.Value:d}' to '{endDatePicker.Value:d}'. Prompting user.");
                    DialogResult dr = MessageBox.Show($"The selected date range ({startDatePicker.Value:d} - {endDatePicker.Value:d}) does not fall entirely within the selected Financial Year ({selectedFinYear}).\n\nDo you want to continue anyway?", "Financial Year Mismatch Warning", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                    if (dr == DialogResult.No) { Logger.LogInfo("User chose not to proceed due to FY mismatch."); return false; }
                    Logger.LogWarning("User chose to proceed despite FY mismatch warning.");
                }
            }
            return true;
        }

        /// <summary>
        /// Helper to determine the base path for appsettings.json.
        /// Adjust this logic based on your deployment strategy.
        /// </summary>
        private static string DetermineAppSettingsBasePath()
        {
            // Example: Use current directory in Debug, specific network path in Release
            //return IsDebug
            //   ? AppDomain.CurrentDomain.BaseDirectory
            //   : @"\\harlow.local\DFS\IT Department\Applications\Development 2025\QuoteConversionReportAutomation\conversionTest";

            // Or always use the network path:
            return @"\\harlow.local\DFS\IT Department\Applications\Development 2025\QuoteConversionReportAutomation\conversionTest";

            // Or use Application.StartupPath (for WinForms)
            // return Application.StartupPath;
        }

        /// <summary>
        /// Checks if the core configuration paths are valid.
        /// </summary>
        private bool CheckConfigValidity()
        {
            string crystalReportPath = CrystalReportLocation;
            string wrapperExePath = _configuration["settings:WrapperExePath"] ?? string.Empty;
            return !string.IsNullOrEmpty(crystalReportPath)
                && File.Exists(crystalReportPath)
                && !string.IsNullOrEmpty(wrapperExePath)
                && File.Exists(Path.GetFullPath(wrapperExePath));
        }

        /// <summary>
        /// Checks if the Daily report type is currently selected.
        /// </summary>
        private bool IsDailySelected()
        {
            // Custom reports are treated like non-Daily for Femi checkbox visibility
            return reportTypeComboBox.SelectedIndex == DailyReportIndex;
        }

        /// <summary>
        /// Centralized method to reset the UI state via UIManager after errors or cancellations.
        /// </summary>
        /// <param name="button1Text">Text for the create report button.</param>
        private void ResetUIStateOnError(string button1Text)
        {
            bool configValid = CheckConfigValidity();
            bool rawExists = !string.IsNullOrEmpty(_generatedReportPath) && File.Exists(_generatedReportPath);
            bool analysisExists = !string.IsNullOrEmpty(_generatedAnalysisFilePath) && File.Exists(_generatedAnalysisFilePath);
            bool isDaily = IsDailySelected();
            bool timerEnabled = dailyCheckTimer.Enabled;
            bool isDark = darkModeToolStripMenuItem.Checked;
            // Determining if auto-run status is "final" is complex without direct access to AutoRunManager state.
            // We might pass a simpler flag or let UIManager handle default text.
            bool isFinalStatus = false; // Simplification - UIManager will handle text based on timer state mostly
            string currentAutoRunStatus = autoRunStatusLabel.Text ?? string.Empty; // Get current text

            _uiManager.ResetUIOnError(button1Text, configValid, rawExists, analysisExists, isDaily, timerEnabled, isDark, isFinalStatus, currentAutoRunStatus);
        }

        /// <summary>
        /// Asynchronously sends the completion email using EmailUtility.
        /// </summary>
        private async Task SendCompletionEmailAsync(string attachmentPath, IProgress<string> progress, CancellationToken cancellationToken)
        {
            Logger.LogInfo($"Preparing completion email with attachment: {attachmentPath}");
            progress?.Report("Preparing email...");

            if (!File.Exists(attachmentPath))
            {
                Logger.LogError($"Attachment file not found: {attachmentPath}");
                throw new FileNotFoundException("The attachment file for the email was not found.", attachmentPath);
            }

            try
            {
                // Determine Recipients (Use helper or keep logic here if simple)
                var (toAddresses, ccAddresses) = GetEmailRecipients(); // Using local helper
                if (toAddresses.Count == 0 && ccAddresses.Count == 0)
                { throw new InvalidOperationException("No valid email recipients configured or found."); }

                // Determine Subject and Body (Use helper or keep logic here)
                // *** Pass date pickers directly for Custom report range ***
                var (subject, body) = GetEmailSubjectAndBody(startDatePicker.Value, endDatePicker.Value); // Using local helper

                // Send Email via Utility
                bool success = await _emailUtility.SendEmailAsync(
                    toAddresses, ccAddresses, subject, body, attachmentPath, progress, cancellationToken);

                if (success)
                {
                    Logger.LogInfo("Email sent successfully.");
                    // Update UI for completion via UIManager
                    _uiManager.SetUICompleted(CheckConfigValidity(), IsDailySelected(), dailyCheckTimer.Enabled, darkModeToolStripMenuItem.Checked, false, autoRunStatusLabel.Text ?? "");
                }
                else if (!cancellationToken.IsCancellationRequested)
                {
                    throw new Exception("Email sending failed. Check logs for details.");
                }
                // Cancellation is handled by the calling method's catch block
            }
            catch (OperationCanceledException)
            {
                Logger.LogWarning("Email sending was cancelled.");
                throw; // Re-throw for the main catch block
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error sending completion email: {ex}");
                MessageBox.Show($"Failed to send the completion email:\n\n{ex.Message}", "Email Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                throw; // Re-throw for the main catch block to reset UI
            }
        }

        /// <summary>
        /// Determines email recipients based on report type, checkbox, and build mode.
        /// </summary>
        private (List<string> To, List<string> Cc) GetEmailRecipients()
        {
            Logger.LogTrace("Entering GetEmailRecipients...");
            List<string> toAddresses = [];
            List<string> ccAddresses = [];
            // Ensure checkbox state is read correctly (consider reading directly if needed)
            bool sendToFemiOnly = sendToFemiOnlyCheckBox.Checked;
            int currentReportType = reportTypeComboBox.SelectedIndex;
            Logger.LogDebug($"GetEmailRecipients: sendToFemiOnlyCheckBox.Checked = {sendToFemiOnly}, ReportType = {currentReportType}, IsDebug = {IsDebug}");

            if (currentReportType == DailyReportIndex && !IsDebug)
            {
                // Special rule for Daily Release
                toAddresses.Add(_configuration["settings:ProductionEmails:AutoRunDailyTo"] ?? "pauls@harlowsolutions.co.uk");
                ccAddresses = GetStringListFromConfig("settings:ProductionEmails:AutoRunDailyCC") ?? ["itdept@harlowsolutions.co.uk"];
                Logger.LogInfo("RELEASE Build & Daily Report: Sending only to PaulS & IT.");
            }
            else
            {
                // Default Logic (Applies to DEBUG Mode OR Non-Daily/Non-Custom Reports in RELEASE Mode)
#if DEBUG
                // --- DEBUG Build Recipients ---
                Logger.LogInfo("DEBUG Build: Using debug email recipients.");
                // Always send To: chrisp (or config override)
                toAddresses.Add(_configuration["settings:DebugEmails:To"] ?? "chrisp@harlowsolutions.co.uk");

                // *** UPDATED DEBUG CC Logic ***
                string? debugCC1 = _configuration["settings:DebugEmails:CC1"] ?? "chrisp@harlowsolutions.co.uk"; // Chris P default
                string? debugCC2 = _configuration["settings:DebugEmails:CC2"] ?? "jamier@harlowsolutions.co.uk"; // Jamie R default

                if (sendToFemiOnly) // Checkbox IS checked
                {
                    Logger.LogDebug("DEBUG Build: Femi checkbox CHECKED. Adding CC1 and CC2.");
                    // Add both CC1 (chrisp) and CC2 (jamier)
                    if (!string.IsNullOrWhiteSpace(debugCC1)) ccAddresses.Add(debugCC1);
                    if (!string.IsNullOrWhiteSpace(debugCC2)) ccAddresses.Add(debugCC2);
                }
                else // Checkbox is NOT checked
                {
                    Logger.LogDebug("DEBUG Build: Femi checkbox NOT CHECKED. Adding CC1 only.");
                    // Add only CC1 (chrisp)
                    if (!string.IsNullOrWhiteSpace(debugCC1)) ccAddresses.Add(debugCC1);
                }
                // *** End UPDATED DEBUG CC Logic ***

#else
                    // --- RELEASE Build Recipients (for non-Daily reports) ---
                     Logger.LogInfo($"RELEASE Build (Non-Daily/Custom): SendToFemiOnly = {sendToFemiOnly}");
                    if (sendToFemiOnly)
                    {
                        toAddresses.Add(_configuration["settings:ProductionEmails:FemiTo"] ?? "femi@harlowsolutions.co.uk");
                        ccAddresses = GetStringListFromConfig("settings:ProductionEmails:FemiCC") ?? ["itsystems@harlowsolutions.co.uk"];
                        Logger.LogInfo("Sending to Femi (and FemiCC list).");
                    }
                    else
                    {
                        toAddresses = GetStringListFromConfig("settings:ProductionEmails:TeamTo") ?? ["andrewp@harlowsolutions.co.uk", "kirstym@harlowsolutions.co.uk", "stuartm@harlowsolutions.co.uk"];
                        ccAddresses = GetStringListFromConfig("settings:ProductionEmails:TeamCC") ?? ["emmanuel@harlowsolutions.co.uk", "femi@harlowsolutions.co.uk", "jackh@harlowsolutions.co.uk", "pauls@harlowsolutions.co.uk", "itsystems@harlowsolutions.co.uk", "gordonb@harlowsolutions.co.uk"];
                        Logger.LogInfo("Sending to Team list.");
                    }
#endif
            }
            // Remove duplicates just in case To and CC overlap
            ccAddresses = ccAddresses.Except(toAddresses).ToList();

            Logger.LogDebug($"Final To Addresses: {string.Join("; ", toAddresses)}");
            Logger.LogDebug($"Final CC Addresses: {string.Join("; ", ccAddresses)}");
            Logger.LogTrace("Exiting GetEmailRecipients.");
            return (toAddresses, ccAddresses);
        }

        /// <summary>
        /// Generates email subject and body based on report type and dates.
        /// </summary>
        /// <param name="reportStartDate">The start date for the report range.</param>
        /// <param name="reportEndDate">The end date for the report range.</param>
        /// <returns>A tuple containing the email Subject string and the email Body string.</returns>
        private (string Subject, string Body) GetEmailSubjectAndBody(DateTime reportStartDate, DateTime reportEndDate) // Updated signature
        {
            string reportTypeName = "Estimate Success Rate";
            int currentReportType = reportTypeComboBox.SelectedIndex;
            bool isFemiOnlyChecked = sendToFemiOnlyCheckBox.Checked;

            string greeting = IsDebug ? "Hi Debug," : (isFemiOnlyChecked ? "Hi Femi," : "Hi All,");
            if (currentReportType == DailyReportIndex && !IsDebug)
            {
                greeting = _configuration["settings:ProductionEmails:AutoRunDailyGreeting"] ?? "Hi Paul,";
            }

            string dateRangeInfo = string.Empty;
            string subjectPrefix = string.Empty;
            // Use the passed dates, not necessarily the picker values directly
            DateTime fromDate = reportStartDate;
            DateTime toDate = reportEndDate;

            switch (currentReportType)
            {
                case DailyReportIndex:
                    subjectPrefix = $"Daily {reportTypeName}";
                    dateRangeInfo = $"for {toDate:dd MMM yyyy}"; // Use the actual report date
                    break;
                case WeeklyReportIndex:
                    subjectPrefix = $"Weekly {reportTypeName}";
                    dateRangeInfo = $"for the period ending {toDate:dd MMM yyyy}";
                    break;
                case MonthlyReportIndex:
                    subjectPrefix = $"Monthly {reportTypeName}";
                    dateRangeInfo = $"for {fromDate:MMMM yyyy}"; // Use fromDate for month name
                    break;
                case QuarterlyReportIndex:
                    subjectPrefix = $"Quarterly {reportTypeName}";
                    dateRangeInfo = $"for {ReportHelper.GetQuarterString(fromDate)} {fromDate.Year}"; // Use fromDate for quarter
                    break;
                case AnnualReportIndex:
                    subjectPrefix = $"Annual {reportTypeName}";
                    dateRangeInfo = $"for {fromDate.Year}"; // Use fromDate for year
                    break;
                case CustomReportIndex: // <<< ADDED CASE
                    subjectPrefix = $"Custom {reportTypeName}";
                    // Show the exact date range used
                    if (fromDate.Date == toDate.Date)
                    {
                        dateRangeInfo = $"for {toDate:dd MMM yyyy}";
                    }
                    else
                    {
                        dateRangeInfo = $"for period {fromDate:dd MMM yyyy} to {toDate:dd MMM yyyy}";
                    }
                    break;
                default:
                    subjectPrefix = reportTypeName;
                    dateRangeInfo = $"from {fromDate:d} to {toDate:d}";
                    break;
            }

            // Add AUTOMATED prefix unless it's a Custom report
            string subject = (currentReportType != CustomReportIndex ? "AUTOMATED: " : "")
                             + $"{subjectPrefix} Report ({toDate:yyyy-MM-dd})";
            string body = $"{greeting}\n\nPlease find attached the {subjectPrefix.ToLower()} report {dateRangeInfo}.\n\nThis report includes quotes data for review.\n\nThank you,\nAutomation Service";

            return (subject, body);
        }

        /// <summary>
        /// Reads a configuration value and splits it into a list of strings.
        /// </summary>
        private List<string>? GetStringListFromConfig(string key)
        {
            // Keep this helper here as it's tightly coupled with IConfiguration used in Form1
            string? configValue = _configuration[key];
            if (string.IsNullOrWhiteSpace(configValue)) return null;
            return [.. configValue.Split([',', ';', ' '], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)];
        }

        /// <summary>
        /// Handles the manual Excel refresh step by calling the helper method.
        /// Updates status messages via UIManager.
        /// </summary>
        private async Task<bool> HandleManualExcelRefreshAsync(string filePath, CancellationToken token)
        {
            Logger.LogDebug("Entering HandleManualExcelRefreshAsync");
            _uiManager.UpdateStatusMain("Checking for running Excel instances...");
            // Use Task.Run for potentially blocking Process.GetProcessesByName
            bool excelRunning = await Task.Run(() => Process.GetProcessesByName("EXCEL").Length > 0, token);
            if (excelRunning)
            {
                Logger.LogDebug("HandleManualExcelRefreshAsync: Found running Excel instances.");
                // *** FIX: Specify owner window 'this' ***
                DialogResult closeResult = MessageBox.Show(this,
                   "Other Excel instances are running. Close them before proceeding?",
                   "Close Other Excel Instances?", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning);

                if (closeResult == DialogResult.Cancel)
                {
                    Logger.LogDebug("HandleManualExcelRefreshAsync: User cancelled closing other Excel instances.");
                    return false;
                }
                if (closeResult == DialogResult.Yes)
                {
                    _uiManager.UpdateStatusMain("Attempting to close other Excel instances...");
                    Logger.LogDebug("HandleManualExcelRefreshAsync: Attempting to close other Excel instances via ReportHelper...");
                    await Task.Run(() => ReportHelper.CloseProcessesByName("EXCEL"), token); // Use helper
                    await Task.Delay(1500, token);
                    Logger.LogDebug("HandleManualExcelRefreshAsync: Finished attempting to close other Excel instances.");
                }
            }
            else
            {
                Logger.LogDebug("HandleManualExcelRefreshAsync: No other Excel instances found running.");
            }

            // *** FIX: Specify owner window 'this' ***
            MessageBox.Show(this,
               "The report will open in Excel.\n\n*** IMPORTANT ***\n1. Refresh All Pivots/Slicers.\n2. SAVE the file.\n3. CLOSE Excel.\n\nThe application will wait.",
               "Manual Refresh Required", MessageBoxButtons.OK, MessageBoxIcon.Information);

            token.ThrowIfCancellationRequested();

            _uiManager.UpdateStatusMain("Opening Excel...");
            Logger.LogDebug($"HandleManualExcelRefreshAsync: Attempting to start Excel process for: {filePath}");
            Process? excelProcess = null;
            try
            {
                // Use Task.Run to avoid blocking UI thread if Process.Start hangs
                excelProcess = await Task.Run(() => Process.Start(new ProcessStartInfo(filePath) { UseShellExecute = true }), token);
                if (excelProcess == null) throw new Exception("Process.Start returned null.");
                Logger.LogDebug($"HandleManualExcelRefreshAsync: Excel process started (PID: {excelProcess.Id}).");

                // Set specific waiting message before waiting
                _uiManager.UpdateStatusMain("Excel opened. Waiting for you to Refresh All, Save, and Close...");
                Logger.LogDebug($"HandleManualExcelRefreshAsync: Waiting for Excel process (PID: {excelProcess.Id}) to exit...");
                await excelProcess.WaitForExitAsync(token); // Asynchronously wait
                Logger.LogInfo("Excel process has exited.");
                _uiManager.UpdateStatusMain("Excel closed.");

                // *** FIX: Specify owner window 'this' ***
                DialogResult sendResult = MessageBox.Show(this,
                   "Excel closed.\n\nProceed with sending the email?",
                   "Confirm Email Send", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                bool result = (sendResult == DialogResult.Yes);
                Logger.LogDebug($"Exiting HandleManualExcelRefreshAsync. User confirmation: {result}");
                return result;
            }
            catch (OperationCanceledException)
            {
                Logger.LogWarning("Manual refresh cancelled.");
                Logger.LogDebug($"Exiting HandleManualExcelRefreshAsync due to cancellation.");
                return false;
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error during manual Excel handling: {ex}");
                MessageBox.Show($"An error occurred managing Excel refresh:\n\n{ex.Message}", "Excel Interaction Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Logger.LogDebug($"Exiting HandleManualExcelRefreshAsync due to error.");
                return false;
            }
            finally
            {
                Logger.LogDebug($"HandleManualExcelRefreshAsync: Entering finally block. Process null? {excelProcess == null}");
                excelProcess?.Dispose();
            }
        }


        #endregion
    }
}