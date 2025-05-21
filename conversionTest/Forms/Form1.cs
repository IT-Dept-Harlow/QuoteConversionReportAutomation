// Form1.cs
// C# 10+ Features

// --- Standard and Third-Party Using Statements ---
using Microsoft.Extensions.Configuration;
using Microsoft.VisualBasic; // For Interaction.InputBox
using Newtonsoft.Json.Linq; // For JObject specifically
using QuoteConversionReportAutomation;
using QuoteConversionReportAutomation.Helpers;
using QuoteConversionReportAutomation.Managers; // Required for AutoRunActionResult
using QuoteConversionReportAutomation.Models;
using QuoteConversionReportAutomation.Services.Communication;
using QuoteConversionReportAutomation.Services.Excel;
using QuoteConversionReportAutomation.Services.Logging;
using System;
using System.Collections.Generic; // For List
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace conversionTest // Main namespace for the UI
{
    /// <summary>
    /// Represents the main form of the Quote Conversion Report Automation application.
    /// This form serves as the primary user interface for generating, processing,
    /// and emailing quote conversion reports. It coordinates various manager classes
    /// to perform these operations and handles UI events and updates.
    /// Key features include:
    /// - Selection of various report types (Daily, Weekly, Custom, etc.).
    /// - Automated date calculations, including bank holiday considerations.
    /// - Manual and automated report generation modes.
    /// - Configuration options for email recipients, greetings, and auto-run settings.
    /// - Dark mode and UI theming.
    /// - Background archiving of old reports and logs.
    /// </summary>
    public partial class Form1 : Form
    {
        #region Fields and Properties

        // --- Dependencies (Injected or Instantiated) ---
        private readonly IConfiguration _configuration;
        private readonly EmailUtility _emailUtility;
        private readonly UIManager _uiManager;
        private readonly ReportProcessManager _processManager;
        private readonly NamedPipeCommunicator _pipeCommunicator;
        private readonly AutoRunManager _autoRunManager;
        private readonly ExcelCopyData _excelProcessor;
        private readonly EmailRecipientManager _emailRecipientManager;
        private readonly GreetingManager _greetingManager;

        // --- Application Info ---
        /// <summary>
        /// Current version of the application. Used for display purposes (e.g., title bar, help).
        /// </summary>
        private const string AppVersion = "1.8.10"; // Update as necessary

        // --- State Variables ---
        private string _generatedReportPath = string.Empty; // Stores the path to the last generated raw report file.
        private string _generatedAnalysisFilePath = string.Empty; // Stores the path to the last processed analysis file.
        private bool _programmaticallyChangingDates = false; // Flag to prevent event recursion when date pickers are updated by code.
        private int _currentAutoRunHour; // Stores the configured hour for automated daily checks, loaded from settings.

        // --- Configuration Paths ---
        private static readonly string appSettingsBasePath = DetermineAppSettingsBasePath(); // Base path for appsettings.json
        private readonly string _appSettingsPath = Path.Combine(appSettingsBasePath, "appsettings.json"); // Full path to appsettings.json

        // --- Report Type Constants ---
        // These constants define indices for different report types selected in the UI ComboBox.
        // They should align with the ComboBox items and the logic in GetSelectedReportTypeIndex().
        // Also used by AutoReportDefinition.ReportTypeIndex for mapping if needed.
        private const int DailyReportIndex = 0;
        private const int NewDailyReportOver1kIndex = 1;
        private const int WeeklyReportIndex = 2; // Represents the 15-day rolling report run weekly
        private const int MonthlyReportIndex = 3;
        private const int QuarterlyReportIndex = 4;
        private const int AnnualReportIndex = 5;
        private const int CustomReportIndex = 6;

        // --- Build Configuration Helper ---
        /// <summary>
        /// Gets a value indicating whether the application is running in DEBUG mode.
        /// This is determined by preprocessor directives.
        /// </summary>
        private static bool IsDebug =>
#if DEBUG
            true;
#else
            false;
#endif

        // --- Configuration-derived Properties (Convenience Accessors) ---

        /// <summary>Gets the current user's profile directory path.</summary>
        private string UserProfilePath => Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        /// <summary>Gets the base directory for exporting raw Crystal Reports, resolved from configuration.</summary>
        private string RawReportExportBaseDir => Path.Combine(UserProfilePath, _configuration["settings:RawReportExportBaseDir"]?.TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) ?? @"Harlow Printing\IT Projects - Documents\Dashboard Datasets\Raw_data\Quotes conversion\Estimate Reports Exports");

        /// <summary>Gets the base directory for saving final processed Excel analysis files, resolved from configuration.</summary>
        public string ExcelFinalSaveLocation => Path.Combine(UserProfilePath, _configuration["settings:ExcelFinalSaveLocation"]?.TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) ?? @"Harlow Printing\IT Projects - Documents\Dashboard Datasets\Raw_data\Quotes conversion\Estimates");

        /// <summary>Gets the full path to the Crystal Report definition file (.rpt), resolved from configuration.</summary>
        private string CrystalReportLocation => _configuration["settings:CrystalReportPath"] ?? string.Empty;

        /// <summary>Gets the base directory where Excel template files are stored, resolved from configuration.</summary>
        public string ExcelTemplateBaseDir => Path.Combine(UserProfilePath, _configuration["settings:ExcelTemplateFolder"]?.TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) ?? @"Harlow Printing\IT Projects - Documents\Dashboard Datasets\Raw_data\Quotes conversion\TEMPLATE");

        /// <summary>Gets the configured base directory for application logs, resolved from configuration.</summary>
        private string ConfiguredLogDirectoryBase => _configuration["settings:LogDirectory"] ?? string.Empty;

        // --- Dynamic Path Properties (Calculated based on UI state and configuration) ---

        /// <summary>
        /// Gets the full output path for the raw Crystal Report export, dynamically determined
        /// based on the selected report type and dates.
        /// </summary>
        public string ReportOutputLocation
        {
            get
            {
                string baseDir = RawReportExportBaseDir;
                DateTime dateForFilename = endDatePicker.Value;
                string fileName = $"{dateForFilename:yyyyMMdd}_EstimateSuccessReport_Raw.xlsx"; // Generic name, consider making it more specific if needed
                int currentReportTypeIndex = GetSelectedReportTypeIndex();

                DateTime folderTimestampDate = (currentReportTypeIndex == CustomReportIndex) ? DateTime.Now : endDatePicker.Value;
                if (currentReportTypeIndex == NewDailyReportOver1kIndex) // Specific case for this report type
                {
                    folderTimestampDate = endDatePicker.Value;
                }

                string? specificFolder = FolderCreation.GetReportSpecificFolderPath(currentReportTypeIndex, baseDir, folderTimestampDate);

                if (string.IsNullOrEmpty(specificFolder))
                {
                    Logger.LogError($"Could not determine specific folder path for ReportOutputLocation. ReportType: {currentReportTypeIndex}, Base: {baseDir}. Using fallback.");
                    string reportTypeSubFolder = currentReportTypeIndex switch // Fallback subfolder naming
                    {
                        DailyReportIndex => "Daily Reports",
                        NewDailyReportOver1kIndex => "Daily Reports (5day 1k)",
                        WeeklyReportIndex => "Weekly Reports", // Folder for the 15-day report
                        MonthlyReportIndex => "Monthly Reports",
                        QuarterlyReportIndex => "Quarterly reports",
                        AnnualReportIndex => "Annual Reports",
                        CustomReportIndex => "Custom Reports",
                        _ => "Other Reports"
                    };
                    specificFolder = Path.Combine(baseDir, reportTypeSubFolder);
                    try { Directory.CreateDirectory(specificFolder); }
                    catch (Exception ex) { Logger.LogError($"Failed to create fallback directory '{specificFolder}': {ex.Message}"); }
                }
                return Path.Combine(specificFolder, fileName);
            }
        }

        /// <summary>
        /// Gets the full path to the Excel template file to be used for processing,
        /// based on the selected report type.
        /// </summary>
        public string ExcelTemplateLocation
        {
            get
            {
                string baseDir = ExcelTemplateBaseDir;
                int currentReportTypeIndex = GetSelectedReportTypeIndex();
                string templateName = currentReportTypeIndex switch
                {
                    MonthlyReportIndex or QuarterlyReportIndex or AnnualReportIndex or CustomReportIndex
                        => "TEMPLATE_Estimate Success Rate_Monthly.xlsx",
                    _ => "TEMPLATE_Estimate Success Rate.xlsx"
                };
                return Path.Combine(baseDir, templateName);
            }
        }
        #endregion

        #region Constructor
        /// <summary>
        /// Initializes a new instance of the <see cref="Form1"/> class.
        /// Sets up dependencies by instantiating manager classes and initializes UI components.
        /// </summary>
        /// <param name="configuration">The application's configuration settings, typically loaded from `appsettings.json`.</param>
        public Form1(IConfiguration configuration)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            Logger.LogTrace("Entering Form1 Constructor");
            try
            {
                InitializeComponent();
                Logger.LogDebug("InitializeComponent completed.");

                _emailUtility = new EmailUtility(_configuration);
                _excelProcessor = new ExcelCopyData();
                _emailRecipientManager = new EmailRecipientManager(_configuration);
                _greetingManager = new GreetingManager(_configuration);

                _uiManager = new UIManager(
                    this, menuStrip1, mainStatusStrip, statusLabel, autoRunStatusLabel,
                    darkModeToolStripMenuItem, createReportButton, processEmailButton,
                    oneClickProcessButton,
                    toggleAutoRunButton, viewReportButton, viewAnalysisButton,
                    reportTypeComboBox, startDatePicker, endDatePicker,
                    financialYearComboBox, financialYearLabel, sendToFemiOnlyCheckBox,
                    skipEmailCheckBox, emailRecipientLabel, toolTip1
                );

                string wrapperExePath = Path.GetFullPath(_configuration["settings:WrapperExePath"] ?? "CrystalReportWrapper.exe");
                _processManager = new ReportProcessManager(wrapperExePath);
                _pipeCommunicator = new NamedPipeCommunicator();

                _currentAutoRunHour = _configuration.GetValue<int>("settings:AutoRunCheckHour", 8);
                _uiManager.SetAutoRunHour(_currentAutoRunHour);
                _autoRunManager = new AutoRunManager(
                    _configuration, _emailUtility, _processManager, _pipeCommunicator,
                    _uiManager, _excelProcessor, _appSettingsPath, _emailRecipientManager, _greetingManager,
                     _currentAutoRunHour
                );

                Logger.LogDebug("Service and Manager classes instantiated.");
            }
            catch (Exception ex)
            {
                Logger.LogCritical($"CRITICAL ERROR during Form Initialization: {ex.Message}", ex);
                System.Windows.Forms.MessageBox.Show($"A critical error occurred initializing the application:\n\n{ex.Message}\n\nThe application cannot continue.",
                                        "Initialization Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                throw;
            }
            Logger.LogTrace("Exiting Form1 Constructor");
        }
        #endregion

        #region Form Load / Closing Events
        /// <summary>
        /// Handles the Load event of the form. This is called once when the form is first displayed.
        /// Initializes application state, applies themes, performs startup checks (like report service and archiving).
        /// </summary>
        private async void Form1_Load(object sender, EventArgs e)
        {
            Logger.LogTrace("Entering Form1_Load");
            _uiManager.UpdateStatusMain("Loading application...");
            try
            {
                BankHolidayHelper.Initialize();
                Logger.LogInfo("BankHolidayHelper initialized.");

                string crystalReportPath = CrystalReportLocation;
                string wrapperExePath = _configuration["settings:WrapperExePath"] ?? string.Empty;
                bool configValid = true;

                if (string.IsNullOrEmpty(crystalReportPath) || !File.Exists(crystalReportPath))
                {
                    Logger.LogError($"Configuration Error: 'settings:CrystalReportPath' missing or file not found: '{crystalReportPath}'. Report generation will be affected.");
                    configValid = false;
                }
                if (string.IsNullOrEmpty(wrapperExePath) || !File.Exists(Path.GetFullPath(wrapperExePath)))
                {
                    Logger.LogError($"Configuration Error: 'settings:WrapperExePath' missing or file not found: '{wrapperExePath}'. Report generation will be affected.");
                    configValid = false;
                }

                Text = $"Quote Conversion Automation - {(IsDebug ? "DEBUG" : "RELEASE")} - v{AppVersion}";
                StartPosition = FormStartPosition.CenterScreen;
                financialYearComboBox.DropDownStyle = ComboBoxStyle.DropDownList;

                if (!reportTypeComboBox.Items.Contains("Custom"))
                {
                    reportTypeComboBox.Items.Add("Custom");
                }
                if (reportTypeComboBox.Items.Count > DailyReportIndex && reportTypeComboBox.Items.Contains("Daily"))
                {
                    reportTypeComboBox.SelectedIndex = reportTypeComboBox.Items.IndexOf("Daily");
                }
                else if (reportTypeComboBox.Items.Count > 0) reportTypeComboBox.SelectedIndex = 0;
                reportTypeComboBox.DropDownStyle = ComboBoxStyle.DropDownList;

                bool useDarkMode = UIManager.IsWindowsDarkModeEnabled();
                darkModeToolStripMenuItem.Checked = useDarkMode;
                _uiManager.ApplyTheme(useDarkMode);
                _uiManager.UpdateAutoRunUI(dailyCheckTimer.Enabled, false, useDarkMode, $"Auto Run: {(dailyCheckTimer.Enabled ? $"Enabled (Next check ~{_currentAutoRunHour}:00)" : "Disabled")}");


                LoadAutoReportToggleStates();

                reportTypeComboBox_SelectedIndexChanged(reportTypeComboBox, EventArgs.Empty);
                _uiManager.ResetButtonStatesAfterTypeChange(configValid);
                enable1ClickProcessingToolStripMenuItem.Checked = false;
                Update1ClickProcessingModeUI();
                if (!configValid) _uiManager.UpdateStatusMain("Config Error: Check Options menu.");


                _uiManager.UpdateStatusMain("Checking report service...");
                IProgress<string> loadProgress = new Progress<string>(status => _uiManager.UpdateProgress(status));
                bool wrapperOk = await _processManager.EnsureWrapperIsRunningAsync(loadProgress);

                if (!wrapperOk && configValid)
                {
                    _uiManager.UpdateStatusMain("Report service failed to start. Report generation may fail.");
                }

                string? finalDir = ExcelFinalSaveLocation;
                string? rawDir = RawReportExportBaseDir;
                int? archiveDays = _configuration.GetValue<int?>("settings:ArchiveRawOlderThanDays");
                _ = Task.Run(async () => await ReportArchiver.ArchiveOldReportsAsync(finalDir, rawDir, archiveDays))
                        .ContinueWith(t =>
                        {
                            if (t.IsFaulted) Logger.LogError($"Background report archiving task failed: {t.Exception?.GetBaseException().Message}");
                            else Logger.LogInfo("Background report archiving task completed.");
                        }, TaskScheduler.Default);

                Logger.LogInfo("Form Load Initialisation Complete.");
                if (configValid && wrapperOk) _uiManager.UpdateStatusMain("Ready");
                else if (configValid && !wrapperOk) _uiManager.UpdateStatusMain("Ready (Report Service Issue)");
                else _uiManager.UpdateStatusMain("Config Error (Service Check Skipped)");
            }
            catch (Exception ex)
            {
                Logger.LogCritical($"CRITICAL ERROR during Form_Load: {ex.Message}", ex);
                FlexibleMessageBox.Show(this, $"A critical error occurred loading the application:\n\n{ex.Message}\n\nThe application may not function correctly.",
                    "Application Load Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                _uiManager.UpdateStatusMain("Error during load. Application may be unstable.");
            }
            Logger.LogTrace("Exiting Form1_Load");
        }

        /// <summary>
        /// Handles the FormClosing event.
        /// Ensures cleanup by stopping timers and terminating background processes like the Crystal Report Wrapper.
        /// </summary>
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            Logger.LogInfo("Form closing. Stopping timer and terminating wrapper process.");
            dailyCheckTimer.Stop();
            _processManager.TerminateWrapperProcess();
        }
        #endregion

        #region Event Handlers for Main Action Buttons
        /// <summary>
        /// Handles the Click event for the "Create Report" button.
        /// Initiates the raw report generation process.
        /// </summary>
        private async void createReportButton_Click(object sender, EventArgs e)
        {
            await PerformCreateReportAsync();
        }

        /// <summary>
        /// Handles the Click event for the "Process & Email" button.
        /// Initiates the processing of the raw report and subsequent emailing.
        /// </summary>
        private async void processEmailButton_Click(object sender, EventArgs e)
        {
            await PerformProcessAndEmailAsync(skipEmail: skipEmailCheckBox.Checked);
        }

        /// <summary>
        /// Handles the Click event for the "1-Click Process" button (visible when 1-Click mode is enabled).
        /// Performs both raw report creation and processing/emailing sequentially.
        /// </summary>
        private async void oneClickProcessButton_Click(object sender, EventArgs e)
        {
            Logger.LogInfo("1-Click Process button clicked.");
            _uiManager.UpdateStatusMain("1-Click Process: Starting...");

            UIManager.SafeControlUpdate(oneClickProcessButton, () => oneClickProcessButton.Enabled = false);
            UIManager.SafeControlUpdate(createReportButton, () => createReportButton.Enabled = false);
            UIManager.SafeControlUpdate(processEmailButton, () => processEmailButton.Enabled = false);
            _uiManager.SetOtherControlsEnabled(false, financialYearComboBox.Visible);

            await PerformCreateReportAsync();

            if (string.IsNullOrEmpty(_generatedReportPath) || !File.Exists(_generatedReportPath))
            {
                Logger.LogWarning("1-Click Process: Raw report generation failed or was cancelled. Aborting further steps.");
                string buttonText = CheckConfigValidity() ? "Generate, Process && Email Report" : "Config Error";
                ResetUIStateOnError(buttonText);
                return;
            }

            await PerformProcessAndEmailAsync(skipEmail: skipEmailCheckBox.Checked);
            Logger.LogInfo("1-Click Process sequence completed (or aborted if errors occurred).");
        }


        /// <summary>
        /// Handles the Click event for the "View Raw File" button.
        /// Opens the last generated raw report file using the system's default application.
        /// </summary>
        private void viewReportButton_Click(object sender, EventArgs e)
        {
            ReportHelper.OpenFileWithDefaultApp(_generatedReportPath, "raw report output");
        }

        /// <summary>
        /// Handles the Click event for the "View Processed File" button.
        /// Opens the last generated final analysis file using the system's default application.
        /// </summary>
        private void viewAnalysisButton_Click(object sender, EventArgs e)
        {
            ReportHelper.OpenFileWithDefaultApp(_generatedAnalysisFilePath, "processed analysis file");
        }
        #endregion

        #region Core Report Logic Methods
        /// <summary>
        /// Asynchronously initiates the process of creating the raw report data via the Crystal Report Wrapper.
        /// Handles UI updates to reflect the process state and provides error reporting.
        /// </summary>
        private async Task PerformCreateReportAsync()
        {
            Button currentActionButton = oneClickProcessButton.Visible ? oneClickProcessButton : createReportButton;
            string originalButtonText = string.Empty;
            UIManager.SafeControlUpdate(currentActionButton, () => originalButtonText = currentActionButton.Text);

            UIManager.SafeControlUpdate(currentActionButton, () => currentActionButton.Enabled = false);
            if (currentActionButton == createReportButton)
            {
                UIManager.SafeControlUpdate(processEmailButton, () => processEmailButton.Enabled = false);
            }
            _uiManager.SetOtherControlsEnabled(false, financialYearComboBox.Visible);
            _uiManager.UpdateProgress("Validating request...");
            UIManager.SafeControlUpdate(currentActionButton, () => currentActionButton.Text = "Requesting...");
            Logger.LogDebug("Create Report Logic: Requesting Crystal Report generation.");

            using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(6));
            IProgress<string> progress = new Progress<string>(status => _uiManager.UpdateProgress(status));

            try
            {
                if (!ValidateInputDates()) { ResetUIStateOnError(originalButtonText); return; }
                if (!ValidateFinancialYearSelection()) { ResetUIStateOnError(originalButtonText); return; }

                string crystalReportPath = CrystalReportLocation;
                if (string.IsNullOrEmpty(crystalReportPath) || !File.Exists(crystalReportPath))
                { throw new InvalidOperationException("Crystal Report location is invalid or file not found. Check configuration."); }

                if (!await _processManager.EnsureWrapperIsRunningAsync(progress, cts.Token))
                { throw new InvalidOperationException($"Failed to start or connect to the report service (CrystalReportWrapper)."); }

                string reportOutputPath = ReportOutputLocation;
                var request = new ReportRequest
                {
                    CrystalReportLocation = crystalReportPath,
                    ReportOutputLocation = reportOutputPath,
                    ReportDateFrom = startDatePicker.Value,
                    ReportDateTo = endDatePicker.Value
                };

                Logger.LogInfo("Attempting Named Pipe communication with CrystalReportWrapper...");
                ReportResponse? response = await _pipeCommunicator.SendRequestReceiveResponseAsync(request, progress, cts.Token);

                if (response?.Success == true && !string.IsNullOrEmpty(response.OutputPath) && File.Exists(response.OutputPath))
                {
                    _generatedReportPath = response.OutputPath;
                    Logger.LogInfo($"Raw report generated successfully by wrapper: {_generatedReportPath}");

                    if (oneClickProcessButton.Visible)
                    {
                        // UI handled by subsequent steps or error handling
                    }
                    else
                    {
                        UIManager.SafeControlUpdate(createReportButton, () => createReportButton.Text = "Report Created");
                        UIManager.SafeControlUpdate(processEmailButton, () => processEmailButton.Enabled = CheckConfigValidity());
                        _uiManager.SetOtherControlsEnabled(true, financialYearComboBox.Visible);
                    }
                    _uiManager.ShowViewReportButton(true, _generatedReportPath);
                    _uiManager.ShowViewAnalysisButton(false);
                    _generatedAnalysisFilePath = string.Empty;
                    _uiManager.UpdateStatusMain("Raw report created successfully.");
                }
                else
                {
                    string errorMessage = response?.ErrorMessage ?? "Unknown error from report service.";
                    if (response?.Success == true && (string.IsNullOrEmpty(response.OutputPath) || !File.Exists(response.OutputPath)))
                    {
                        errorMessage = $"Report service indicated success, but the output file ('{response?.OutputPath ?? "NULL"}') is invalid or missing.";
                        Logger.LogError(errorMessage);
                    }
                    throw new Exception($"Raw report generation failed: {errorMessage}");
                }
            }
            catch (OperationCanceledException)
            {
                Logger.LogWarning("Report generation request cancelled or timed out.");
                FlexibleMessageBox.Show(this, "The report generation request timed out or was cancelled.", "Timeout / Cancelled", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                ResetUIStateOnError(originalButtonText);
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error during Create Report operation: {ex.Message}", ex);
                FlexibleMessageBox.Show(this, $"An error occurred while requesting the report:\n\n{ex.Message}", "Report Request Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                ResetUIStateOnError(originalButtonText);
            }
        }

        /// <summary>
        /// Asynchronously processes the previously generated raw report into a final analysis Excel file
        /// and then emails it, unless skipped by the user. Handles manual Excel refresh prompts if required.
        /// </summary>
        /// <param name="skipEmail">If true, the email sending step will be bypassed after processing.</param>
        private async Task PerformProcessAndEmailAsync(bool skipEmail = false)
        {
            Logger.LogTrace($"Entering PerformProcessAndEmailAsync (skipEmail: {skipEmail})");
            Button currentActionButton = oneClickProcessButton.Visible ? oneClickProcessButton : processEmailButton;
            string originalButtonText = string.Empty;
            UIManager.SafeControlUpdate(currentActionButton, () => originalButtonText = currentActionButton.Text);

            UIManager.SafeControlUpdate(currentActionButton, () => currentActionButton.Enabled = false);
            if (currentActionButton == processEmailButton)
            {
                UIManager.SafeControlUpdate(createReportButton, () => createReportButton.Enabled = false);
            }
            _uiManager.SetOtherControlsEnabled(false, financialYearComboBox.Visible);
            UIManager.SafeControlUpdate(currentActionButton, () => currentActionButton.Text = "Processing...");

            IProgress<ProgressReport> excelProgress = new Progress<ProgressReport>(report => _uiManager.UpdateProgress(report));
            IProgress<string> generalProgress = new Progress<string>(message => _uiManager.UpdateProgress(message));
            _uiManager.UpdateProgress("Starting Excel processing...");

            using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(15));
            var token = cts.Token;
            string? finalFilePath = null;
            int reportType = GetSelectedReportTypeIndex();
            bool requiresManualRefresh = reportType is MonthlyReportIndex or QuarterlyReportIndex or AnnualReportIndex or CustomReportIndex;
            string baseSaveLocation = ExcelFinalSaveLocation;
            DateTime dateForFilenameAndExcelProcessing = (reportType == AnnualReportIndex) ? startDatePicker.Value : endDatePicker.Value;

            try
            {
                if (!ValidateInputDates()) { ResetUIStateOnError(originalButtonText); return; }
                if (string.IsNullOrEmpty(_generatedReportPath) || !File.Exists(_generatedReportPath))
                {
                    FlexibleMessageBox.Show(this, "The raw report file has not been generated or cannot be found. Please create the report first.", "Raw Report Missing", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    string resetText = oneClickProcessButton.Visible ? (CheckConfigValidity() ? "Generate, Process && Email Report" : "Config Error")
                                                                  : (CheckConfigValidity() ? "Create Report" : "Config Error");
                    ResetUIStateOnError(resetText);
                    return;
                }

                string? expectedFinalPath = _excelProcessor.GetExpectedFinalFilePath(reportType, baseSaveLocation, dateForFilenameAndExcelProcessing);
                if (expectedFinalPath != null && File.Exists(expectedFinalPath))
                {
                    generalProgress.Report("Found existing file. Prompting user...");
                    DialogResult fdr = FlexibleMessageBox.Show(this, $"The report file '{Path.GetFileName(expectedFinalPath)}' already exists for this period.\n\nDo you want to skip processing and use this existing file?", "File Already Exists", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                    if (fdr == DialogResult.Yes)
                    {
                        Logger.LogInfo("User chose to use existing file.");
                        finalFilePath = expectedFinalPath;
                        _generatedAnalysisFilePath = finalFilePath;
                        _uiManager.ShowViewAnalysisButton(true, finalFilePath);
                        bool proceedToEmail = true;
                        if (requiresManualRefresh)
                        {
                            generalProgress.Report("Waiting for manual Excel refresh...");
                            proceedToEmail = await HandleManualExcelRefreshAsync(finalFilePath, token);
                            if (!proceedToEmail && !token.IsCancellationRequested) { _uiManager.UpdateStatusMain("Manual refresh/confirmation cancelled."); ResetUIStateOnError(originalButtonText); return; }
                            if (token.IsCancellationRequested) throw new OperationCanceledException("Operation cancelled during manual refresh prompt.");
                            generalProgress.Report("Manual refresh confirmed.");
                        }
                        if (!skipEmail && proceedToEmail) await SendCompletionEmailAsync(finalFilePath, generalProgress, token);
                        else if (skipEmail) { _uiManager.UpdateStatusMain("Process completed. Email skipped by user."); Logger.LogInfo("Email sending skipped by user checkbox."); }

                        if (proceedToEmail || skipEmail) _uiManager.SetUICompleted(CheckConfigValidity(), IsAnyDailySelected(), dailyCheckTimer.Enabled, darkModeToolStripMenuItem.Checked, false, autoRunStatusLabel.Text ?? "");
                        ResetUIStateOnError(originalButtonText);
                        return;
                    }
                    else
                    {
                        generalProgress.Report("Deleting existing file...");
                        Logger.LogInfo("User chose to overwrite/regenerate the existing file.");
                        try { File.Delete(expectedFinalPath); Logger.LogInfo($"Deleted existing file: {expectedFinalPath}"); }
                        catch (Exception delEx)
                        {
                            Logger.LogError($"Failed to delete existing file '{expectedFinalPath}': {delEx.Message}");
                            FlexibleMessageBox.Show(this, $"Could not delete the existing report file:\n{expectedFinalPath}\n\nPlease ensure the file is not open and try again.\n\nError: {delEx.Message}", "File Deletion Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            ResetUIStateOnError(originalButtonText); return;
                        }
                    }
                }

                generalProgress.Report("Processing new report...");
                finalFilePath = await _excelProcessor.ProcessExcelReportAsync(
                    financialYearComboBox.SelectedItem?.ToString() ?? _excelProcessor.GetCurrentFinancialYear(true),
                    reportType,
                    _generatedReportPath,
                    "Sheet1",
                    baseSaveLocation,
                    ExcelTemplateLocation,
                    "DATA",
                    1, 1,
                    excelProgress,
                    dateForFilenameAndExcelProcessing,
                    token);

                if (string.IsNullOrEmpty(finalFilePath) || !File.Exists(finalFilePath))
                {
                    if (token.IsCancellationRequested) throw new OperationCanceledException("Excel processing was cancelled.");
                    else throw new Exception("Excel processing failed to produce a final file. Check logs for details.");
                }
                _generatedAnalysisFilePath = finalFilePath;
                _uiManager.ShowViewAnalysisButton(true, finalFilePath);

                bool proceedToEmailAfterGenerate = true;
                if (requiresManualRefresh)
                {
                    generalProgress.Report("Waiting for manual Excel refresh...");
                    proceedToEmailAfterGenerate = await HandleManualExcelRefreshAsync(finalFilePath, token);
                    if (!proceedToEmailAfterGenerate && !token.IsCancellationRequested) { _uiManager.UpdateStatusMain("Manual refresh/confirmation cancelled."); ResetUIStateOnError(originalButtonText); return; }
                    if (token.IsCancellationRequested) throw new OperationCanceledException("Operation cancelled during manual refresh prompt.");
                    generalProgress.Report("Manual refresh confirmed.");
                }

                if (!skipEmail && proceedToEmailAfterGenerate)
                {
                    await SendCompletionEmailAsync(finalFilePath, generalProgress, token);
                }
                else if (skipEmail)
                {
                    _uiManager.UpdateStatusMain("Process completed. Email skipped by user.");
                    Logger.LogInfo("Email sending skipped by user checkbox.");
                }

                if (proceedToEmailAfterGenerate || skipEmail) _uiManager.SetUICompleted(CheckConfigValidity(), IsAnyDailySelected(), dailyCheckTimer.Enabled, darkModeToolStripMenuItem.Checked, false, autoRunStatusLabel.Text ?? "");
                ResetUIStateOnError(originalButtonText);
            }
            catch (OperationCanceledException)
            {
                Logger.LogWarning("Excel processing or subsequent step cancelled.");
                ResetUIStateOnError(originalButtonText);
            }
            catch (FileNotFoundException fnfEx)
            {
                Logger.LogError($"File not found during Process & Email operation: {fnfEx.Message}", fnfEx);
                FlexibleMessageBox.Show(this, fnfEx.Message, "File Not Found Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                ResetUIStateOnError(originalButtonText);
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error during Process & Email operation: {ex.Message}", ex);
                FlexibleMessageBox.Show(this, $"An unexpected error occurred during processing:\n\n{ex.Message}", "Processing Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                ResetUIStateOnError(originalButtonText);
            }
            Logger.LogTrace("Exiting PerformProcessAndEmailAsync logic");
        }
        #endregion

        #region UI Event Handlers

        /// <summary>
        /// Handles the SelectedIndexChanged event of the reportTypeComboBox.
        /// Adjusts date pickers and UI elements (like Financial Year visibility and email recipient info label) 
        /// based on the selected report type.
        /// </summary>
        private void reportTypeComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            Logger.LogTrace("Entering reportTypeComboBox_SelectedIndexChanged");
            if (sender is not ComboBox comboBox || comboBox.SelectedItem == null) return;

            int selectedIndex = GetSelectedReportTypeIndex(comboBox.Text);

            if (selectedIndex == CustomReportIndex)
            {
                Logger.LogDebug("Report Type changed to Custom. Manual date entry expected.");
                UIManager.SafeControlUpdate(sendToFemiOnlyCheckBox, () => { sendToFemiOnlyCheckBox.Visible = true; });
                UIManager.SafeControlUpdate(emailRecipientLabel, () => { emailRecipientLabel.Visible = false; });
                _uiManager.ResetButtonStatesAfterTypeChange(CheckConfigValidity());
                Update1ClickProcessingModeUI();
                return;
            }

            DateTime todayValue = DateTime.Today;
            _programmaticallyChangingDates = true;
            try
            {
                DateTime dateFrom = todayValue;
                DateTime dateTo = todayValue;
                bool showFinYear = true;

                switch (selectedIndex)
                {
                    case DailyReportIndex:
                        dateFrom = ReportHelper.GetPreviousWorkday(todayValue);
                        dateTo = dateFrom;
                        showFinYear = false;
                        break;
                    case NewDailyReportOver1kIndex:
                        dateTo = ReportHelper.GetPreviousWorkday(todayValue);
                        dateFrom = ReportHelper.GetNthPreviousWorkday(dateTo, 4);
                        showFinYear = false;
                        Logger.LogInfo($"Daily (5days >= £1000) report selected. Dates: {dateFrom:dd/MM/yyyy} - {dateTo:dd/MM/yyyy}");
                        break;
                    case WeeklyReportIndex:
                        // For manual "Weekly" (15-day) report: End date is today, Start date is 14 days prior.
                        dateTo = todayValue;
                        dateFrom = todayValue.AddDays(-14); // Covers 15 days including today
                        showFinYear = true; // Typically, a weekly summary might need FY context for Power BI
                        Logger.LogInfo($"Manual Weekly (15-day) report selected. Dates: {dateFrom:dd/MM/yyyy} - {dateTo:dd/MM/yyyy}");
                        break;
                    case MonthlyReportIndex:
                        (dateFrom, dateTo) = ReportHelper.CalculateMonthlyRange(todayValue);
                        showFinYear = false;
                        break;
                    case QuarterlyReportIndex:
                        (dateFrom, dateTo) = ReportHelper.CalculateQuarterlyRange(todayValue);
                        showFinYear = false;
                        break;
                    case AnnualReportIndex:
                        int prevFinancialYearStartCalendarYear = (todayValue.Month >= 5) ? todayValue.Year - 1 : todayValue.Year - 2;
                        (dateFrom, dateTo) = ReportHelper.GetFinancialYearDates(prevFinancialYearStartCalendarYear);
                        showFinYear = false;
                        Logger.LogInfo($"Annual report selected. Dates set for Financial Year: {dateFrom:dd/MM/yyyy} - {dateTo:dd/MM/yyyy}");
                        break;
                    default:
                        Logger.LogWarning($"Unexpected reportTypeComboBox index: {selectedIndex} or unmapped item: {comboBox.Text}. Defaulting dates.");
                        dateFrom = startDatePicker.Value;
                        dateTo = endDatePicker.Value;
                        showFinYear = true;
                        break;
                }

                UIManager.SafeControlUpdate(startDatePicker, () => { startDatePicker.Value = dateFrom; });
                UIManager.SafeControlUpdate(endDatePicker, () => { endDatePicker.Value = dateTo; });
                UIManager.SafeControlUpdate(financialYearLabel, () => { financialYearLabel.Visible = showFinYear; });
                UIManager.SafeControlUpdate(financialYearComboBox, () =>
                {
                    financialYearComboBox.Visible = showFinYear;
                    financialYearComboBox.Enabled = showFinYear;
                    if (showFinYear) PopulateFinancialYearDropdown();
                });

                bool isAnyDailyType = IsAnyDailySelected();
                UIManager.SafeControlUpdate(sendToFemiOnlyCheckBox, () => { sendToFemiOnlyCheckBox.Visible = !isAnyDailyType && selectedIndex != CustomReportIndex; });

                UIManager.SafeControlUpdate(emailRecipientLabel, () =>
                {
                    emailRecipientLabel.Visible = isAnyDailyType;
                    if (selectedIndex == DailyReportIndex)
                    {
                        emailRecipientLabel.Text = "Manual Daily: Uses configured list.";
                    }
                    else if (selectedIndex == NewDailyReportOver1kIndex)
                    {
                        emailRecipientLabel.Text = "Daily (5d>=1k): Femi/Team (manual) or Auto (config).";
                    }
                    else
                    {
                        emailRecipientLabel.Visible = false;
                    }
                });

                _uiManager.ResetButtonStatesAfterTypeChange(CheckConfigValidity());
                Update1ClickProcessingModeUI();
            }
            finally
            {
                _programmaticallyChangingDates = false;
            }
            Logger.LogTrace("Exiting reportTypeComboBox_SelectedIndexChanged");
        }

        private void toggleAutoRunButton_Click(object sender, EventArgs e)
        {
            dailyCheckTimer.Enabled = !dailyCheckTimer.Enabled;
            bool isAutoRunCompletedForToday = (autoRunStatusLabel.Text?.Contains("Completed") ?? false) ||
                                              (autoRunStatusLabel.Text?.Contains("Done for") ?? false) ||
                                              (autoRunStatusLabel.Text?.Contains("FAILED") ?? false);
            _uiManager.UpdateAutoRunUI(dailyCheckTimer.Enabled,
                                      isAutoRunCompletedForToday,
                                      darkModeToolStripMenuItem.Checked,
                                      $"Auto Run: {(dailyCheckTimer.Enabled ? $"Enabled (Next check ~{_currentAutoRunHour}:00)" : "Disabled")}");
            Logger.LogInfo($"AutoRun {(dailyCheckTimer.Enabled ? "Enabled" : "Disabled")} by user via toggle button.");
        }

        private async void dailyCheckTimer_Tick(object sender, EventArgs e)
        {
            if (!dailyCheckTimer.Enabled) return;

            bool originallyEnabled = dailyCheckTimer.Enabled;
            dailyCheckTimer.Stop();
            Logger.LogDebug("Daily Check Timer Ticked. Attempting to perform daily auto-run check.");

            AutoRunActionResult autoRunResult = AutoRunActionResult.NoActionNeeded;

            try
            {
                autoRunResult = await _autoRunManager.PerformDailyCheckAsync(originallyEnabled, _currentAutoRunHour);
            }
            catch (Exception ex)
            {
                Logger.LogCritical($"CRITICAL ERROR during AutoRunManager.PerformDailyCheckAsync dispatch from timer: {ex.Message}", ex);
                _uiManager.UpdateStatusMain("Critical AutoRun Error! Check Logs.");
                _uiManager.UpdateStatusRight("AutoRun: FAILED");
                _uiManager.UpdateAutoRunUI(dailyCheckTimer.Enabled, true, darkModeToolStripMenuItem.Checked, "AutoRun: FAILED (Timer Error)");
                autoRunResult = AutoRunActionResult.CriticalError;
            }
            finally
            {
                if (originallyEnabled && autoRunResult != AutoRunActionResult.CriticalError)
                {
                    dailyCheckTimer.Start();
                    Logger.LogDebug("Daily Check Timer Restarted after auto-run check.");
                }
                else if (autoRunResult == AutoRunActionResult.CriticalError)
                {
                    Logger.LogWarning("Daily Check Timer remains stopped due to a critical error during the auto-run check.");
                }


                if (autoRunResult == AutoRunActionResult.ActionAttempted || autoRunResult == AutoRunActionResult.CriticalError)
                {
                    Logger.LogInfo($"AutoRun action result '{autoRunResult}' indicates UI may need reset.");
                    string mainButtonResetText = enable1ClickProcessingToolStripMenuItem.Checked ?
                                                 (CheckConfigValidity() ? "Generate, Process && Email Report" : "Config Error") :
                                                 (CheckConfigValidity() ? "Create Report" : "Config Error");
                    ResetUIStateOnError(mainButtonResetText);
                }
                else
                {
                    if (_uiManager != null && toggleAutoRunButton != null && !toggleAutoRunButton.IsDisposed)
                    {
                        UIManager.SafeControlUpdate(toggleAutoRunButton, () => toggleAutoRunButton.Enabled = true);
                    }
                    Logger.LogDebug("AutoRun result is NoActionNeeded. Full UI reset skipped. Timer restart managed.");
                }
            }
        }

        private void darkModeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            bool isChecked = darkModeToolStripMenuItem.Checked;
            _uiManager.ApplyTheme(isChecked);

            bool isAutoRunFinalStatusForToday = (autoRunStatusLabel.Text?.Contains("Completed") ?? false) ||
                                              (autoRunStatusLabel.Text?.Contains("Done for") ?? false) ||
                                              (autoRunStatusLabel.Text?.Contains("FAILED") ?? false);

            string autoRunStatusTextToShow;
            if (dailyCheckTimer.Enabled)
            {
                autoRunStatusTextToShow = isAutoRunFinalStatusForToday ? (autoRunStatusLabel.Text ?? $"Auto Run: Enabled (Next check ~{_currentAutoRunHour}:00)")
                                                                 : $"Auto Run: Enabled (Next check ~{_currentAutoRunHour}:00)";
            }
            else
            {
                autoRunStatusTextToShow = isAutoRunFinalStatusForToday ? (autoRunStatusLabel.Text ?? "Auto Run: Disabled")
                                                                 : "Auto Run: Disabled";
            }

            _uiManager.UpdateAutoRunUI(dailyCheckTimer.Enabled, isAutoRunFinalStatusForToday, isChecked, autoRunStatusTextToShow);
            Logger.LogInfo($"Dark Mode toggled via menu. New state: {(isChecked ? "Enabled" : "Disabled")}");
        }

        private void helpToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Logger.LogTrace("Help menu item clicked.");
            string helpTitle = $"Help - Quote Conversion Automation v{AppVersion}";

            StringBuilder helpMessageBuilder = new StringBuilder();
            bool isDarkModeActive = darkModeToolStripMenuItem.Checked;

            string rtfDefaultTextColor = isDarkModeActive ? @"\red220\green220\blue220;" : @"\red0\green0\blue0;";
            string rtfHeaderColor = isDarkModeActive ? @"\red120\green220\blue250;" : @"\red0\green0\blue128;";
            string rtfSubHeaderColor = isDarkModeActive ? @"\red120\green220\blue180;" : @"\red0\green100\blue0;";
            string rtfAccentColor = isDarkModeActive ? @"\red255\green160\blue160;" : @"\red200\green0\blue0;";
            string rtfBulletColor = isDarkModeActive ? @"\red180\green180\blue180;" : @"\red80\green80\blue80;";


            helpMessageBuilder.AppendLine(@"{\rtf1\ansi\ansicpg1252\deff0\nouicompat{\fonttbl{\f0\fnil\fcharset0 Segoe UI;}{\f1\fnil\fcharset2 Symbol;}}");
            helpMessageBuilder.AppendLine($@"{{\colortbl ;{rtfDefaultTextColor}{rtfHeaderColor}{rtfSubHeaderColor}{rtfAccentColor}{rtfBulletColor}}}");
            helpMessageBuilder.AppendLine(@"\pard\cf1\sa200\sl276\slmult1\f0\fs20");

            helpMessageBuilder.AppendLine($@"\b\fs28\cf2 Quote Conversion Automation Tool v{AppVersion}\b0\fs20\cf1\par");
            helpMessageBuilder.AppendLine(@"\par");
            helpMessageBuilder.AppendLine(@"\b\fs22\cf3 Welcome!\b0\fs20\cf1\par");
            helpMessageBuilder.AppendLine(@"This tool automates the generation, processing, and emailing of Estimate Success Rate reports, streamlining your workflow.\par");
            helpMessageBuilder.AppendLine(@"\par");
            helpMessageBuilder.AppendLine(@"\b\fs22\cf3 How to Use the Application\b0\fs20\cf1\par");
            helpMessageBuilder.AppendLine(@"\par");
            helpMessageBuilder.AppendLine(@"\b 1. Select Report Type:\b0\cf1\  \par");
            helpMessageBuilder.AppendLine(@"Choose the desired report period from the \b Report Type\b0\cf1\ dropdown menu. The options are:\par");
            helpMessageBuilder.AppendLine(@"    \pard\fi-360\li720{\pntext\f1\'B7\tab}\cf1 \b Daily:\b0\  Generates a report for the {\i previous working day}. This automatically accounts for weekends and bank holidays.\par");
            helpMessageBuilder.AppendLine(@"    \pard\fi-360\li720{\pntext\f1\'B7\tab}\cf1 \b Daily (5days >= £1000):\b0\  Covers the {\i previous five working days}, filtering for 'Net Value' >= £1000.\par");
            helpMessageBuilder.AppendLine(@"    \pard\fi-360\li720{\pntext\f1\'B7\tab}\cf1 \b Weekly:\b0\  Covers the {\i 15-day period ending on the current day}. Appends data to a central Power BI Excel file.\par");
            helpMessageBuilder.AppendLine(@"    \pard\fi-360\li720{\pntext\f1\'B7\tab}\cf1 \b Monthly:\b0\  For the {\i previous full calendar month}.\par");
            helpMessageBuilder.AppendLine(@"    \pard\fi-360\li720{\pntext\f1\'B7\tab}\cf1 \b Quarterly:\b0\  For the {\i previous full calendar quarter}.\par");
            helpMessageBuilder.AppendLine(@"    \pard\fi-360\li720{\pntext\f1\'B7\tab}\cf1 \b Annual:\b0\  For the {\i previous full financial year (May 1st - April 30th)}.\par");
            helpMessageBuilder.AppendLine(@"    \pard\fi-360\li720{\pntext\f1\'B7\tab}\cf1 \b Custom:\b0\  Allows manual 'From' and 'To' date range selection.\par");
            helpMessageBuilder.AppendLine(@"\pard\sa200\sl276\slmult1\cf1 Dates adjust automatically for standard types. Manual date changes switch type to 'Custom'.\par");
            helpMessageBuilder.AppendLine(@"\par");
            helpMessageBuilder.AppendLine(@"\b 2. Adjust Dates (Optional for 'Custom'):\b0\cf1\  Manually set 'From' and 'To' dates. This changes report type to 'Custom'.\par");
            helpMessageBuilder.AppendLine(@"\b 3. Financial Year (If Applicable):\b0\cf1\  Select for 'Weekly' or 'Custom' reports if needed.\par");
            helpMessageBuilder.AppendLine(@"\b 4. Report Processing Options:\b0\cf1\  \par");
            helpMessageBuilder.AppendLine(@"    \pard\fi-360\li720{\pntext\f1\'B7\tab}\cf1 \b Send to only Femi?:\b0\  (Visible for non-Daily, non-Custom manual reports) Restricts email to Femi/IT.\par");
            helpMessageBuilder.AppendLine(@"    \pard\fi-360\li720{\pntext\f1\'B7\tab}\cf1 \b Skip Sending Email:\b0\  Generates report files locally without emailing.\par");
            helpMessageBuilder.AppendLine(@"\pard\sa200\sl276\slmult1\cf1\par");
            helpMessageBuilder.AppendLine(@"\b 5. Choose Your Processing Mode (via Options Menu):\b0\cf1\  \par");
            helpMessageBuilder.AppendLine(@"    \pard\fi-360\li720{\pntext\f1\'B7\tab}\cf1 \b Standard 2-Button Mode (Default):\b0\  'Create Report', then 'Create Analysis & Send Email'.\par");
            helpMessageBuilder.AppendLine(@"    \pard\fi-360\li720{\pntext\f1\'B7\tab}\cf1 \b 1-Click Processing Mode:\b0\  Enable via Options menu for a single 'Generate, Process & Email Report' button.\par");
            helpMessageBuilder.AppendLine(@"\pard\sa200\sl276\slmult1\cf1\par");
            helpMessageBuilder.AppendLine(@"\b 6. Manual Excel Refresh (Monthly, Quarterly, Annual, Custom):\b0\cf1\  After file generation, you'll be prompted to open Excel, refresh PivotTables/Slicers on 'OrderPivot' and 'Estimate Success PivotTable' sheets, save, and close Excel.\par");
            helpMessageBuilder.AppendLine(@"\par");
            helpMessageBuilder.AppendLine(@"\b\fs22\cf3 Options Menu Explained\b0\fs20\cf1\par");
            helpMessageBuilder.AppendLine(@"    \pard\fi-360\li720{\pntext\f1\'B7\tab}\cf1 \b Enable 1-Click Processing:\b0\  Toggles between 1-button and 2-button modes.\par");
            helpMessageBuilder.AppendLine($@"    \pard\fi-360\li720{{\pntext\f1\'B7\tab}}\cf1 \b Set Auto-Run Hour...:\b0\  Change the daily auto-run check time (currently ~ \b {_currentAutoRunHour}:00\b0\cf1 ).\par");
            helpMessageBuilder.AppendLine(@"    \pard\fi-360\li720{\pntext\f1\'B7\tab}\cf1 \b Configure Auto-Run Reports:\b0\  Sub-menu to enable/disable automated reports (Standard Daily, Daily 5days >= £1000, Weekly).\par");
            helpMessageBuilder.AppendLine(@"    \pard\fi-360\li720{\pntext\f1\'B7\tab}\cf1 \b Dark Mode:\b0\  Toggle application theme.\par");
            helpMessageBuilder.AppendLine(@"    \pard\fi-360\li720{\pntext\f1\'B7\tab}\cf1 \b View Configuration:\b0\  Show critical file paths and settings.\par");
            helpMessageBuilder.AppendLine(@"    \pard\fi-360\li720{\pntext\f1\'B7\tab}\cf1 \b Validate Configuration:\b0\  Quick check of essential configurations.\par");
            helpMessageBuilder.AppendLine(@"    \pard\fi-360\li720{\pntext\f1\'B7\tab}\cf1 \b Manage Custom Bank Holidays:\b0\  Add/remove custom bank holidays.\par");
            helpMessageBuilder.AppendLine(@"    \pard\fi-360\li720{\pntext\f1\'B7\tab}\cf1 \b Manage Email Recipients:\b0\  Customize To/CC lists. User overrides saved to `user_email_settings.json`. Debug fields hidden in Release mode.\par");
            helpMessageBuilder.AppendLine(@"    \pard\fi-360\li720{\pntext\f1\'B7\tab}\cf1 \b Manage Email Greetings:\b0\  Customize email greetings. User overrides saved to `user_greeting_settings.json`. Debug field hidden in Release mode.\par");
            helpMessageBuilder.AppendLine(@"    \pard\fi-360\li720{\pntext\f1\'B7\tab}\cf1 \b Open Logs Folder:\b0\  Access application log files.\par");
            helpMessageBuilder.AppendLine($@"    \pard\fi-360\li720{{\pntext\f1\'B7\tab}}\cf1 \b Edit appsettings.json:\b0\  Open main config file. {{\i\cf4 Use with caution!}}\cf1\par");
            helpMessageBuilder.AppendLine(@"    \pard\fi-360\li720{\pntext\f1\'B7\tab}\cf1 \b Exit:\b0\  Close application.\par");
            helpMessageBuilder.AppendLine(@"\pard\sa200\sl276\slmult1\cf1\par");
            helpMessageBuilder.AppendLine(@"\b\fs22\cf3 Auto-Run Feature\b0\fs20\cf1\par");
            helpMessageBuilder.AppendLine($@"When enabled, checks daily around \b {_currentAutoRunHour}:00\b0\cf1\ to run pending enabled reports (as configured in Options and `appsettings.json`) for their respective periods. Uses configured recipients and greetings. Status displayed in the status bar.\par");
            helpMessageBuilder.AppendLine(@"\par");
            helpMessageBuilder.AppendLine(@"\b\fs22\cf3 Email Configuration Notes\b0\fs20\cf1\par");
            helpMessageBuilder.AppendLine(@"    \pard\fi-360\li720{\pntext\f1\'B7\tab}\cf1 \b Recipients & Greetings:\b0\  Both email recipients and greetings are configurable for various report scenarios via the 'Options' menu. User customizations are saved in separate JSON files in your AppData folder and override `appsettings.json` defaults.\par");
            helpMessageBuilder.AppendLine($@"    \pard\fi-360\li720{{\pntext\f1\'B7\tab}}\cf1 \b Debug Mode:\b0\  When running a DEBUG build, {{\i all}} emails (manual or automated) will be sent to the configured Debug recipients using the Debug greeting, overriding all other settings. Debug configuration fields in management forms are hidden in Release builds.\par");
            helpMessageBuilder.AppendLine(@"\pard\sa200\sl276\slmult1\cf1\par");
            helpMessageBuilder.AppendLine(@"\b\fs22\cf3 Troubleshooting Tips\b0\fs20\cf1\par");
            helpMessageBuilder.AppendLine(@"If you encounter issues, consider the following:\par");
            helpMessageBuilder.AppendLine(@"    \pard\fi-360\li720{\pntext\f1\'B7\tab}\cf1 \b ""Config Error"" Status:\b0\  This usually means a critical file path (like the Crystal Report file or the Wrapper EXE) is missing or incorrect. Use \b Options -> View Configuration\b0\cf1\ to check paths. Ensure all listed files/folders exist and are accessible.\par");
            helpMessageBuilder.AppendLine(@"    \pard\fi-360\li720{\pntext\f1\'B7\tab}\cf1 \b Report Generation Fails:\b0\  \par");
            helpMessageBuilder.AppendLine(@"        \pard\fi-720\li1080{\pntext\f1\'B7\tab}\cf1 Ensure Crystal Report Wrapper service (`CrystalReportWrapper.exe`) is running. The application attempts to start it.\par");
            helpMessageBuilder.AppendLine(@"        \pard\fi-720\li1080{\pntext\f1\'B7\tab}\cf1 Verify Crystal Report file path in `appsettings.json` (via Options -> Edit appsettings.json) is correct.\par");
            helpMessageBuilder.AppendLine(@"        \pard\fi-720\li1080{\pntext\f1\'B7\tab}\cf1 Check application logs (\b Options -> Open Logs Folder\b0\cf1 ) for specific error messages.\par");
            helpMessageBuilder.AppendLine(@"    \pard\fi-360\li720{\pntext\f1\'B7\tab}\cf1 \b Excel Processing Fails:\b0\  \par");
            helpMessageBuilder.AppendLine(@"        \pard\fi-720\li1080{\pntext\f1\'B7\tab}\cf1 Ensure Excel template files exist in the configured 'TEMPLATE' directory and are not corrupted.\par");
            helpMessageBuilder.AppendLine(@"        \pard\fi-720\li1080{\pntext\f1\'B7\tab}\cf1 Ensure the application has write permissions to the 'Raw Report Export' and 'Final Excel Save Location' directories.\par");
            helpMessageBuilder.AppendLine(@"        \pard\fi-720\li1080{\pntext\f1\'B7\tab}\cf1 For Weekly reports, ensure the central Power BI source Excel file is accessible and not locked.\par");
            helpMessageBuilder.AppendLine(@"    \pard\fi-360\li720{\pntext\f1\'B7\tab}\cf1 \b Email Sending Fails:\b0\  \par");
            helpMessageBuilder.AppendLine(@"        \pard\fi-720\li1080{\pntext\f1\'B7\tab}\cf1 Check SMTP settings in `appsettings.json` (server, port, credentials if used).\par");
            helpMessageBuilder.AppendLine(@"        \pard\fi-720\li1080{\pntext\f1\'B7\tab}\cf1 Ensure network connection allows SMTP traffic.\par");
            helpMessageBuilder.AppendLine(@"        \pard\fi-720\li1080{\pntext\f1\'B7\tab}\cf1 Verify email recipients via \b Options -> Manage Email Recipients\b0\cf1\ and greetings via \b Options -> Manage Email Greetings\b0\cf1 .\par");
            helpMessageBuilder.AppendLine(@"    \pard\fi-360\li720{\pntext\f1\'B7\tab}\cf1 \b Auto-Run Not Working as Expected:\b0\  \par");
            helpMessageBuilder.AppendLine(@"        \pard\fi-720\li1080{\pntext\f1\'B7\tab}\cf1 Confirm it's enabled via the button and status bar. Check which specific auto-run reports are enabled via \b Options -> Configure Auto-Run Reports\b0\cf1 .\par");
            helpMessageBuilder.AppendLine(@"        \pard\fi-720\li1080{\pntext\f1\'B7\tab}\cf1 Check configured 'Auto-Run Hour' via \b Options -> Set Auto-Run Hour...\b0\cf1 .\par");
            helpMessageBuilder.AppendLine(@"        \pard\fi-720\li1080{\pntext\f1\'B7\tab}\cf1 Review `appsettings.json` for `AutoReport:LastRunDate` and `AutoReport:DailyRunStatus`. If `LastRunDate` is today, or if the specific report's success flag in `DailyRunStatus` (for today's `StatusDate`) is true, it won't run again until the next due time/day.\par");
            helpMessageBuilder.AppendLine(@"    \pard\fi-360\li720{\pntext\f1\'B7\tab}\cf1 \b Incorrect Formulas in Excel Output:\b0\  Ensure the template file (`TEMPLATE_Estimate Success Rate.xlsx` or `..._Monthly.xlsx`) has the correct relative formulas in its 'Analysis' sheet, especially in the first data row (typically row 6). The application copies this row to propagate formulas.\par");
            helpMessageBuilder.AppendLine(@"    \pard\fi-360\li720{\pntext\f1\'B7\tab}\cf1 \b User Settings Not Taking Effect (Recipients/Greetings):\b0\  User settings are stored in JSON files in `%APPDATA%\HarlowSolutions\QuoteConversionReportAutomation\`. If changes aren't sticking, check if these files (`user_email_settings.json`, `user_greeting_settings.json`) are writable. Corrupted files can be deleted (the application will revert to `appsettings.json` defaults).\par");
            helpMessageBuilder.AppendLine(@"    \pard\fi-360\li720{\pntext\f1\'B7\tab}\cf1 \b Excel Slicers/Pivot Tables Not Updating:\b0\  For reports requiring manual refresh, follow the on-screen prompts carefully: open Excel, Enable Editing, Refresh All (Data tab), Save, and Close.\par");
            helpMessageBuilder.AppendLine(@"    \pard\fi-360\li720{\pntext\f1\'B7\tab}\cf1 \b Check Logs:\b0\  The most detailed error information is usually in the log files (\b Options -> Open Logs Folder\b0\cf1 ).\par");
            helpMessageBuilder.AppendLine(@"    \pard\fi-360\li720{\pntext\f1\'B7\tab}\cf1 \b Contact IT Support:\b0\  If problems persist, please contact IT support with details.\par");
            helpMessageBuilder.AppendLine(@"\pard\sa200\sl276\slmult1\cf1\par");
            helpMessageBuilder.AppendLine(@"Thank you for using the Quote Conversion Automation Tool!\par");
            helpMessageBuilder.AppendLine(@"}");

            string helpMessage = helpMessageBuilder.ToString();
            try
            {
                using var helpForm = new HelpForm(helpTitle, helpMessage, darkModeToolStripMenuItem.Checked);
                helpForm.ShowDialog(this);
            }
            catch (Exception ex)
            {
                Logger.LogError($"Failed to show HelpForm: {ex.Message}", ex);
                FlexibleMessageBox.Show(this, "Could not display help window. Please check application logs.", "Help Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void DatePicker_ValueChanged(object sender, EventArgs e)
        {
            if (_programmaticallyChangingDates) return;

            int currentReportTypeIndex = GetSelectedReportTypeIndex();
            if (currentReportTypeIndex != CustomReportIndex)
            {
                Logger.LogDebug("DatePicker_ValueChanged: Manual date change detected. Setting Report Type to Custom.");
                UIManager.SafeControlUpdate(reportTypeComboBox, () =>
                {
                    int customIdx = -1;
                    for (int i = 0; i < reportTypeComboBox.Items.Count; i++)
                    {
                        if (reportTypeComboBox.Items[i].ToString() == "Custom")
                        {
                            customIdx = i;
                            break;
                        }
                    }
                    if (customIdx != -1) reportTypeComboBox.SelectedIndex = customIdx;
                    else Logger.LogWarning("DatePicker_ValueChanged: 'Custom' item not found in reportTypeComboBox.");
                });
            }
        }

        private void viewConfigToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Logger.LogInfo("Options -> View Configuration clicked.");
            bool configValid = CheckConfigValidity();
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("Configuration Details (Paths are relative to user profile where applicable):");
            sb.AppendLine("--------------------------------------------------");
            sb.AppendLine($"1. Crystal Report Path (.rpt): '{CrystalReportLocation}'");
            sb.AppendLine($"   - Exists: {File.Exists(CrystalReportLocation)}");
            sb.AppendLine($"2. Wrapper EXE Path: '{Path.GetFullPath(_configuration["settings:WrapperExePath"] ?? string.Empty)}'");
            sb.AppendLine($"   - Exists: {File.Exists(Path.GetFullPath(_configuration["settings:WrapperExePath"] ?? string.Empty))}");
            sb.AppendLine($"3. Template Base Directory: '{ExcelTemplateBaseDir}'");
            sb.AppendLine($"   - Exists: {Directory.Exists(ExcelTemplateBaseDir)}");
            sb.AppendLine($"4. Raw Report Export Base Directory: '{RawReportExportBaseDir}'");
            sb.AppendLine($"   - Exists: {Directory.Exists(RawReportExportBaseDir)}");
            sb.AppendLine($"5. Final Excel Save Location Base: '{ExcelFinalSaveLocation}'");
            sb.AppendLine($"   - Exists: {Directory.Exists(ExcelFinalSaveLocation)}");
            sb.AppendLine($"6. Auto-Run Check Hour (from appsettings): {_configuration.GetValue<int>("settings:AutoRunCheckHour", _currentAutoRunHour)} (Current in-memory: {_currentAutoRunHour})");

            var reportDefinitions = _configuration.GetSection("AutoReport:ReportDefinitions").Get<List<AutoReportDefinition>>() ?? new List<AutoReportDefinition>();
            if (reportDefinitions.Any())
            {
                sb.AppendLine("7. Auto-Run Report States (from appsettings.json):");
                foreach (var def in reportDefinitions)
                {
                    sb.AppendLine($"   - {def.ReportName} (Key: {def.EnableConfigKey}): Enabled = {_configuration.GetValue<bool>($"AutoReport:{def.EnableConfigKey}", false)}");
                }
            }
            else
            {
                sb.AppendLine("7. Auto-Run Report States: No report definitions found in configuration.");
            }


            string baseLogDir = ConfiguredLogDirectoryBase;
            string actualUserLogDir = string.IsNullOrEmpty(baseLogDir)
                ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "conversionTest", "Logs", Environment.UserName)
                : Path.Combine(baseLogDir, string.Join("_", Environment.UserName.Split(Path.GetInvalidFileNameChars())));

            actualUserLogDir = Path.GetFullPath(actualUserLogDir);

            sb.AppendLine($"8. Application Log Directory (User Specific): '{actualUserLogDir}'");
            sb.AppendLine($"   - Exists: {Directory.Exists(actualUserLogDir)}");
            sb.AppendLine($"9. appsettings.json Path: '{_appSettingsPath}'");
            sb.AppendLine($"    - Exists: {File.Exists(_appSettingsPath)}");
            sb.AppendLine("--------------------------------------------------");
            sb.AppendLine($"Overall Essential Config Valid (for report generation): {configValid}");

            FlexibleMessageBox.Show(this, sb.ToString(), "Configuration Details",
                MessageBoxButtons.OK, configValid ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
        }

        private void validateConfigToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Logger.LogInfo("Options -> Validate Configuration clicked.");
            _uiManager.UpdateProgress("Validating configuration...");
            bool isValid = CheckConfigValidity();
            string statusMessage = isValid ? "Configuration OK." : "Configuration Error: Essential paths missing or invalid. Check View Configuration.";

            if (isValid) Logger.LogInfo("Configuration validation successful.");
            else Logger.LogError("Configuration validation failed. Essential paths are missing or invalid.");

            _uiManager.UpdateStatusMain(statusMessage);
            if (isValid)
            {
                _ = Task.Delay(7000).ContinueWith(t =>
                {
                    if (_uiManager.GetCurrentStatusMain() == statusMessage)
                    {
                        _uiManager.UpdateStatusMain("Ready");
                    }
                }, TaskScheduler.FromCurrentSynchronizationContext());
            }
        }

        private void openLogsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Logger.LogInfo("Options -> Open Logs Folder clicked.");
            try
            {
                string baseLogDir = ConfiguredLogDirectoryBase;
                string actualUserLogDir = string.IsNullOrEmpty(baseLogDir)
                    ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "conversionTest", "Logs", Environment.UserName)
                    : Path.Combine(baseLogDir, string.Join("_", Environment.UserName.Split(Path.GetInvalidFileNameChars())));

                actualUserLogDir = Path.GetFullPath(actualUserLogDir);

                if (!Directory.Exists(actualUserLogDir))
                {
                    Directory.CreateDirectory(actualUserLogDir);
                    Logger.LogInfo($"Created log directory as it did not exist: {actualUserLogDir}");
                }
                Process.Start("explorer.exe", actualUserLogDir);
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error opening logs folder: {ex.Message}", ex);
                FlexibleMessageBox.Show(this, $"Could not open logs folder: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void editConfigToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Logger.LogInfo("Options -> Edit appsettings.json clicked.");
            try
            {
                if (File.Exists(_appSettingsPath))
                {
                    Process.Start(new ProcessStartInfo(_appSettingsPath) { UseShellExecute = true });
                }
                else
                {
                    FlexibleMessageBox.Show(this, $"appsettings.json not found at the expected location:\n{_appSettingsPath}", "File Not Found", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error opening appsettings.json: {ex.Message}", ex);
                FlexibleMessageBox.Show(this, $"Could not open appsettings.json: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Logger.LogInfo("Options -> Exit clicked. Closing application.");
            Close();
        }

        private void manageCustomBankHolidaysToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Logger.LogInfo("Options -> Manage Custom Bank Holidays clicked.");
            try
            {
                using (var manageForm = new ManageBankHolidaysForm(darkModeToolStripMenuItem.Checked))
                {
                    manageForm.ShowDialog(this);
                }
                Logger.LogInfo("ManageBankHolidaysForm closed.");
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error opening or handling ManageBankHolidaysForm: {ex.Message}", ex);
                FlexibleMessageBox.Show(this, "Could not open the bank holiday management window. Please check logs.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void manageEmailRecipientsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Logger.LogInfo("Options -> Manage Email Recipients clicked.");
            try
            {
                using (var manageEmailsForm = new ManageEmailRecipientsForm(_emailRecipientManager, darkModeToolStripMenuItem.Checked))
                {
                    manageEmailsForm.ShowDialog(this);
                }
                Logger.LogInfo("ManageEmailRecipientsForm closed.");
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error opening or handling ManageEmailRecipientsForm: {ex.Message}", ex);
                FlexibleMessageBox.Show(this, "Could not open the email recipient management window. Please check logs.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void manageGreetingsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Logger.LogInfo("Options -> Manage Email Greetings clicked.");
            try
            {
                using (var manageGreetingsForm = new ManageGreetingsForm(_greetingManager, darkModeToolStripMenuItem.Checked))
                {
                    manageGreetingsForm.ShowDialog(this);
                }
                Logger.LogInfo("ManageGreetingsForm closed.");
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error opening or handling ManageGreetingsForm: {ex.Message}", ex);
                FlexibleMessageBox.Show(this, "Could not open the email greetings management window. Please check logs.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void enable1ClickProcessingToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Update1ClickProcessingModeUI();
            string mainButtonTextForReset = enable1ClickProcessingToolStripMenuItem.Checked ?
                                            (CheckConfigValidity() ? "Generate, Process && Email Report" : "Config Error") :
                                            (CheckConfigValidity() ? "Create Report" : "Config Error");
            ResetUIStateOnError(mainButtonTextForReset);
            Logger.LogInfo($"1-Click Processing Mode {(enable1ClickProcessingToolStripMenuItem.Checked ? "Enabled" : "Disabled")}.");
        }

        private async void setAutoRunHourToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Logger.LogInfo("Options -> Set Auto-Run Hour clicked.");
            string currentHourPrompt = _currentAutoRunHour.ToString();

            string? inputText = Interaction.InputBox("Enter the new hour (0-23) for the daily auto-run check:", "Set Auto-Run Hour", currentHourPrompt);

            if (!string.IsNullOrWhiteSpace(inputText))
            {
                if (int.TryParse(inputText, out int newHour) && newHour >= 0 && newHour <= 23)
                {
                    if (newHour != _currentAutoRunHour)
                    {
                        bool success = await _autoRunManager.SetAutoRunHourAsync(newHour);
                        if (success)
                        {
                            _currentAutoRunHour = newHour;
                            Logger.LogInfo($"Auto-Run hour successfully updated to {newHour} in configuration and manager.");
                            FlexibleMessageBox.Show(this, $"Auto-Run hour has been set to {newHour}:00.\nThe change will take effect from the next daily check cycle.", "Auto-Run Hour Updated", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            _uiManager.SetAutoRunHour(_currentAutoRunHour);
                            bool isAutoRunFinal = (autoRunStatusLabel.Text?.Contains("Done for") ?? false) || (autoRunStatusLabel.Text?.Contains("FAILED") ?? false);
                            _uiManager.UpdateAutoRunUI(dailyCheckTimer.Enabled, isAutoRunFinal, darkModeToolStripMenuItem.Checked, $"Auto Run: {(dailyCheckTimer.Enabled ? $"Enabled (Next check ~{_currentAutoRunHour}:00)" : "Disabled")}");
                        }
                        else
                        {
                            Logger.LogError("Failed to save the new auto-run hour to configuration.");
                            FlexibleMessageBox.Show(this, "Failed to save the new auto-run hour. Please check logs and file permissions for appsettings.json.", "Error Saving Setting", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                    else
                    {
                        FlexibleMessageBox.Show(this, "The new hour is the same as the current auto-run hour. No change made.", "No Change", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
                else
                {
                    FlexibleMessageBox.Show(this, "Invalid hour entered. Please enter a number between 0 and 23.", "Invalid Input", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            else
            {
                Logger.LogInfo("Set Auto-Run Hour cancelled by user or no input provided.");
            }
        }

        #endregion

        #region UI Event Handlers for Auto-Run Configuration
        /// <summary>
        /// Handles the Click event for enabling/disabling the Standard Daily Auto Report.
        /// </summary>
        private async void enableStandardDailyAutoReportToolStripMenuItem_Click(object sender, EventArgs e)
        {
            bool newState = enableStandardDailyAutoReportToolStripMenuItem.Checked;
            await UpdateAutoReportToggleSettingAsync("EnableStandardDailyAutoReport", newState);
            Logger.LogInfo($"Standard Daily Auto-Report {(newState ? "Enabled" : "Disabled")} by user.");
            FlexibleMessageBox.Show(this, $"Standard Daily Auto-Report has been {(newState ? "ENABLED" : "DISABLED")}.", "Setting Updated", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        /// <summary>
        /// Handles the Click event for enabling/disabling the "Daily (5days >= £1000)" Auto Report.
        /// </summary>
        private async void enableDaily5Day1kAutoReportToolStripMenuItem_Click(object sender, EventArgs e)
        {
            bool newState = enableDaily5Day1kAutoReportToolStripMenuItem.Checked;
            await UpdateAutoReportToggleSettingAsync("EnableDaily5Day1kAutoReport", newState);
            Logger.LogInfo($"Daily (5days >= £1000) Auto-Report {(newState ? "Enabled" : "Disabled")} by user.");
            FlexibleMessageBox.Show(this, $"Daily (5days >= £1000) Auto-Report has been {(newState ? "ENABLED" : "DISABLED")}.", "Setting Updated", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        /// <summary>
        /// Handles the Click event for enabling/disabling the Weekly Auto Report.
        /// Assumes 'enableWeeklyAutoReportToolStripMenuItem' is the Name property of the ToolStripMenuItem in the designer.
        /// </summary>
        private async void enableWeeklyAutoReportToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (enableWeeklyAutoReportToolStripMenuItem == null)
            {
                Logger.LogError("enableWeeklyAutoReportToolStripMenuItem_Click: The menu item for weekly auto-report is null. Ensure it's correctly added and named in the Form Designer.");
                FlexibleMessageBox.Show(this, "UI element for weekly auto-report toggle not found.", "UI Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            bool newState = enableWeeklyAutoReportToolStripMenuItem.Checked;
            await UpdateAutoReportToggleSettingAsync("EnableWeeklyAutoReport", newState);
            Logger.LogInfo($"Weekly Auto-Report {(newState ? "Enabled" : "Disabled")} by user.");
            FlexibleMessageBox.Show(this, $"Weekly Auto-Report has been {(newState ? "ENABLED" : "DISABLED")}.", "Setting Updated", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        #endregion

        #region Helper Methods
        /// <summary>
        /// Loads the initial checked states for the auto-report toggle menu items from configuration.
        /// </summary>
        private void LoadAutoReportToggleStates()
        {
            enableStandardDailyAutoReportToolStripMenuItem.Checked = _configuration.GetValue<bool>("AutoReport:EnableStandardDailyAutoReport", true);
            enableDaily5Day1kAutoReportToolStripMenuItem.Checked = _configuration.GetValue<bool>("AutoReport:EnableDaily5Day1kAutoReport", true);

            if (enableWeeklyAutoReportToolStripMenuItem != null)
            {
                enableWeeklyAutoReportToolStripMenuItem.Checked = _configuration.GetValue<bool>("AutoReport:EnableWeeklyAutoReport", true);
            }
            else
            {
                Logger.LogWarning("LoadAutoReportToggleStates: enableWeeklyAutoReportToolStripMenuItem is null. UI toggle for weekly report will not be set. Check Form Designer.");
            }
            Logger.LogDebug($"Loaded Auto-Report Toggle States: StandardDaily={enableStandardDailyAutoReportToolStripMenuItem.Checked}, Daily5Day1k={enableDaily5Day1kAutoReportToolStripMenuItem.Checked}, Weekly={enableWeeklyAutoReportToolStripMenuItem?.Checked ?? false}");
        }

        /// <summary>
        /// Asynchronously updates a boolean toggle setting in the "AutoReport" section of `appsettings.json`.
        /// </summary>
        /// <param name="key">The specific configuration key name (e.g., "EnableStandardDailyAutoReport").</param>
        /// <param name="value">The new boolean value for the setting.</param>
        private async Task UpdateAutoReportToggleSettingAsync(string key, bool value)
        {
            try
            {
                if (!File.Exists(_appSettingsPath))
                {
                    Logger.LogError($"appsettings.json not found at '{_appSettingsPath}'. Cannot update setting '{key}'.");
                    FlexibleMessageBox.Show(this, $"Configuration file not found. Cannot save setting.", "File Not Found", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                string jsonContent = await File.ReadAllTextAsync(_appSettingsPath);
                JObject? json = JObject.Parse(jsonContent);

                JObject? autoReportSection = json?["AutoReport"] as JObject;
                if (autoReportSection == null)
                {
                    autoReportSection = new JObject();
                    if (json != null) json["AutoReport"] = autoReportSection;
                    else json = new JObject { ["AutoReport"] = autoReportSection };
                    Logger.LogWarning($"UpdateAutoReportToggleSettingAsync: 'AutoReport' section not found. Creating it for key '{key}'.");
                }

                if (autoReportSection != null) autoReportSection[key] = value;

                await File.WriteAllTextAsync(_appSettingsPath, json?.ToString(Newtonsoft.Json.Formatting.Indented) ?? "{}");
                Logger.LogInfo($"Successfully updated '{key}' to '{value}' in appsettings.json");
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error updating '{key}' in appsettings.json: {ex.Message}", ex);
                FlexibleMessageBox.Show(this, $"Failed to save setting for '{key}'. Please check logs and file permissions for appsettings.json.", "Error Saving Setting", MessageBoxButtons.OK, MessageBoxIcon.Error);
                if (key == "EnableStandardDailyAutoReport") enableStandardDailyAutoReportToolStripMenuItem.Checked = !value;
                else if (key == "EnableDaily5Day1kAutoReport") enableDaily5Day1kAutoReportToolStripMenuItem.Checked = !value;
                else if (key == "EnableWeeklyAutoReport" && enableWeeklyAutoReportToolStripMenuItem != null) enableWeeklyAutoReportToolStripMenuItem.Checked = !value;
            }
        }

        /// <summary>
        /// Gets the integer index corresponding to the currently selected report type in the ComboBox.
        /// Uses the ComboBox's Text property for robustness if SelectedItem is null.
        /// </summary>
        /// <param name="selectedText">Optional. The text of the selected item. If null, uses ComboBox's current selection text.</param>
        /// <returns>The integer index for the report type, or the SelectedIndex if text matching fails but an item is selected.</returns>
        private int GetSelectedReportTypeIndex(string? selectedText = null)
        {
            string currentText = selectedText ?? "";
            if (string.IsNullOrEmpty(currentText) && reportTypeComboBox.SelectedItem != null)
            {
                currentText = reportTypeComboBox.SelectedItem.ToString() ?? "";
            }
            else if (string.IsNullOrEmpty(currentText) && !string.IsNullOrEmpty(reportTypeComboBox.Text))
            {
                currentText = reportTypeComboBox.Text;
            }

            return currentText switch
            {
                "Daily" => DailyReportIndex,
                "Daily (5days >= £1000)" => NewDailyReportOver1kIndex,
                "Weekly" => WeeklyReportIndex,
                "Monthly" => MonthlyReportIndex,
                "Quarterly (3 Months)" => QuarterlyReportIndex,
                "Annual" => AnnualReportIndex,
                "Custom" => CustomReportIndex,
                _ => reportTypeComboBox.SelectedIndex
            };
        }

        /// <summary>
        /// Updates the UI to reflect the 1-Click processing mode (single button vs. two buttons).
        /// </summary>
        private void Update1ClickProcessingModeUI()
        {
            bool oneClickEnabled = enable1ClickProcessingToolStripMenuItem.Checked;
            Logger.LogDebug($"Update1ClickProcessingModeUI called. 1-Click Mode Checked: {oneClickEnabled}");

            if (oneClickProcessButton == null || createReportButton == null || processEmailButton == null)
            {
                Logger.LogError("One or more action buttons are NULL in Update1ClickProcessingModeUI. UI update skipped.");
                return;
            }

            UIManager.SafeControlUpdate(oneClickProcessButton, () => { oneClickProcessButton.Visible = oneClickEnabled; if (oneClickEnabled && oneClickProcessButton.Visible) oneClickProcessButton.BringToFront(); });
            UIManager.SafeControlUpdate(createReportButton, () => { createReportButton.Visible = !oneClickEnabled; });
            UIManager.SafeControlUpdate(processEmailButton, () => { processEmailButton.Visible = !oneClickEnabled; });

            if (oneClickEnabled) Logger.LogInfo("1-Click Processing Mode Enabled (UI updated).");
            else Logger.LogInfo("1-Click Processing Mode Disabled (UI updated to 2-button mode).");
        }

        /// <summary>
        /// Populates the financial year ComboBox with the current and previous financial years.
        /// </summary>
        private void PopulateFinancialYearDropdown()
        {
            Logger.LogTrace("Entering PopulateFinancialYearDropdown");
            UIManager.SafeControlUpdate(financialYearComboBox, () =>
            {
                string? previouslySelected = financialYearComboBox.SelectedItem?.ToString();
                financialYearComboBox.Items.Clear();
                string currentFY = _excelProcessor.GetCurrentFinancialYear(true);
                if (!string.IsNullOrEmpty(currentFY))
                {
                    financialYearComboBox.Items.Add(currentFY);
                    string? previousFY = _excelProcessor.GetPreviousFinancialYear(currentFY);
                    if (!string.IsNullOrEmpty(previousFY))
                    {
                        financialYearComboBox.Items.Add(previousFY);
                    }
                }
                else
                {
                    Logger.LogWarning("Could not determine current financial year for dropdown.");
                    financialYearComboBox.Items.Add("FY Unknown");
                }
                if (!string.IsNullOrEmpty(previouslySelected) && financialYearComboBox.Items.Contains(previouslySelected))
                    financialYearComboBox.SelectedItem = previouslySelected;
                else if (financialYearComboBox.Items.Count > 0)
                    financialYearComboBox.SelectedIndex = 0;
            });
            Logger.LogTrace("Exiting PopulateFinancialYearDropdown");
        }

        /// <summary>
        /// Validates that the selected start date is not after the end date.
        /// </summary>
        /// <returns>True if dates are valid, false otherwise (and shows an error message).</returns>
        private bool ValidateInputDates()
        {
            if (startDatePicker.Value.Date > endDatePicker.Value.Date)
            {
                FlexibleMessageBox.Show(this, "The 'From' date cannot be after the 'To' date.", "Date Range Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
            return true;
        }

        /// <summary>
        /// Validates if the selected date range falls within the selected financial year, if applicable.
        /// Prompts the user if there's a mismatch.
        /// </summary>
        /// <returns>True if valid or user chooses to continue despite warning; false otherwise.</returns>
        private bool ValidateFinancialYearSelection()
        {
            if (!financialYearComboBox.Visible || financialYearComboBox.SelectedItem == null) return true;

            string selectedFinYear = financialYearComboBox.SelectedItem.ToString()!;
            if (!_excelProcessor.IsFinancialYearValid(selectedFinYear, startDatePicker.Value, endDatePicker.Value))
            {
                DialogResult fdr = FlexibleMessageBox.Show(this,
                    $"The selected date range ({startDatePicker.Value:d} - {endDatePicker.Value:d}) " +
                    $"does not fall entirely within the selected Financial Year ({selectedFinYear}).\n\n" +
                    "Do you want to continue anyway?",
                    "Financial Year Mismatch Warning", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                return fdr == DialogResult.Yes;
            }
            return true;
        }

        /// <summary>
        /// Determines the base path for the `appsettings.json` file.
        /// </summary>
        /// <returns>The base path for `appsettings.json`.</returns>
        private static string DetermineAppSettingsBasePath() => @"\\harlow.local\DFS\IT Department\Applications\Development 2025\QuoteConversionReportAutomation\conversionTest";

        /// <summary>
        /// Checks the validity of essential configuration settings (e.g., Crystal Report path, Wrapper EXE path).
        /// </summary>
        /// <returns>True if essential configurations are valid; otherwise, false.</returns>
        private bool CheckConfigValidity()
        {
            string crPath = CrystalReportLocation;
            string wrapPathCfg = _configuration["settings:WrapperExePath"] ?? "";
            string wrapPathFull = string.IsNullOrEmpty(wrapPathCfg) ? "" : Path.GetFullPath(wrapPathCfg);

            return !string.IsNullOrEmpty(crPath) && File.Exists(crPath) &&
                   !string.IsNullOrEmpty(wrapPathFull) && File.Exists(wrapPathFull);
        }

        /// <summary>
        /// Checks if any "Daily" report type (standard or 5day>=1k) is currently selected in the UI.
        /// </summary>
        /// <returns>True if a daily report type is selected; otherwise, false.</returns>
        private bool IsAnyDailySelected()
        {
            string selectedText = "";
            UIManager.SafeControlUpdate(reportTypeComboBox, () => selectedText = reportTypeComboBox.Text);
            return selectedText == "Daily" || selectedText == "Daily (5days >= £1000)";
        }

        /// <summary>
        /// Resets the UI to an appropriate state after an operation error, cancellation, or completion.
        /// Updates button texts and enabled states based on current context and configuration validity.
        /// </summary>
        /// <param name="mainButtonTextIfNoError">The text to display on the primary action button if config is valid.</param>
        private void ResetUIStateOnError(string mainButtonTextIfNoError)
        {
            bool isOneClickMode = enable1ClickProcessingToolStripMenuItem.Checked;
            bool configValid = CheckConfigValidity();
            string actualMainButtonText = configValid ? mainButtonTextIfNoError : "Config Error";

            UIManager.SafeControlUpdate(this, () =>
            {
                if (isOneClickMode)
                {
                    if (oneClickProcessButton != null)
                    {
                        oneClickProcessButton.Text = actualMainButtonText;
                        oneClickProcessButton.Enabled = configValid;
                    }
                    if (createReportButton != null) createReportButton.Enabled = false;
                    if (processEmailButton != null) processEmailButton.Enabled = false;
                }
                else
                {
                    if (createReportButton != null)
                    {
                        createReportButton.Text = actualMainButtonText;
                        createReportButton.Enabled = configValid;
                    }
                    if (processEmailButton != null)
                    {
                        processEmailButton.Text = "Process && Email";
                        processEmailButton.Enabled = configValid && !string.IsNullOrEmpty(_generatedReportPath) && File.Exists(_generatedReportPath);
                    }
                    if (oneClickProcessButton != null) oneClickProcessButton.Enabled = false;
                }

                if (toggleAutoRunButton != null) toggleAutoRunButton.Enabled = true;


                _uiManager.ResetUIOnError(
                    mainButtonTextIfNoError,
                    configValid,
                    !string.IsNullOrEmpty(_generatedReportPath) && File.Exists(_generatedReportPath),
                    !string.IsNullOrEmpty(_generatedAnalysisFilePath) && File.Exists(_generatedAnalysisFilePath),
                    IsAnyDailySelected(),
                    dailyCheckTimer.Enabled,
                    darkModeToolStripMenuItem.Checked,
                    (autoRunStatusLabel.Text?.Contains("Completed") ?? false) || (autoRunStatusLabel.Text?.Contains("Done for") ?? false) || (autoRunStatusLabel.Text?.Contains("FAILED") ?? false),
                    autoRunStatusLabel.Text ?? ""
                );

                string currentStatus = _uiManager.GetCurrentStatusMain();
                if (!currentStatus.Equals("Ready", StringComparison.OrdinalIgnoreCase) &&
                    !currentStatus.StartsWith("Config Error", StringComparison.OrdinalIgnoreCase) &&
                    !currentStatus.StartsWith("Auto Run:", StringComparison.OrdinalIgnoreCase) &&
                    !currentStatus.Contains("Successfully") && !currentStatus.Contains("Completed"))
                {
                    _uiManager.UpdateStatusMain(configValid ? "Ready" : "Config Error: Check Options menu.");
                }
                else if (string.IsNullOrEmpty(currentStatus))
                {
                    _uiManager.UpdateStatusMain(configValid ? "Ready" : "Config Error: Check Options menu.");
                }
            });
        }

        /// <summary>
        /// Asynchronously sends the completion email with the specified report file as an attachment.
        /// </summary>
        /// <param name="attachmentPath">The full path to the file to be attached.</param>
        /// <param name="progress">An IProgress interface to report string-based progress updates.</param>
        /// <param name="cancellationToken">A CancellationToken to observe for cancellation requests.</param>
        private async Task SendCompletionEmailAsync(string attachmentPath, IProgress<string> progress, CancellationToken cancellationToken)
        {
            Logger.LogTrace("Entering SendCompletionEmailAsync");
            _uiManager.UpdateProgress("Preparing email...");
            if (!File.Exists(attachmentPath))
            {
                Logger.LogError($"Attachment file not found for email: {attachmentPath}");
                throw new FileNotFoundException("Attachment file for email not found.", attachmentPath);
            }
            try
            {
                var (to, cc) = GetEmailRecipients();

                if (to.Count == 0 && cc.Count == 0 && !IsDebug)
                {
                    Logger.LogWarning("No email recipients determined for Release mode. Skipping email send.");
                    progress.Report("No recipients configured. Email not sent.");
                    return;
                }
                if (to.Count == 0 && cc.Count == 0 && IsDebug)
                {
                    Logger.LogInfo("DEBUG MODE: No explicit recipients, but will proceed using debug list from EmailRecipientManager.");
                }

                var (subj, body) = GetEmailSubjectAndBody(startDatePicker.Value, endDatePicker.Value);
                progress.Report("Sending email...");

                bool emailSent = await _emailUtility.SendEmailAsync(to, cc, subj, body, attachmentPath, progress, cancellationToken);

                if (!emailSent && !cancellationToken.IsCancellationRequested)
                {
                    Logger.LogError("Email sending failed. Check EmailUtility logs for details.");
                    progress.Report("Email sending failed. Check logs.");
                }
                else if (emailSent)
                {
                    Logger.LogInfo("Email sent successfully.");
                    progress.Report("Email sent successfully.");
                }
                else if (cancellationToken.IsCancellationRequested)
                {
                    Logger.LogWarning("Email sending cancelled.");
                    progress.Report("Email sending cancelled.");
                }
            }
            catch (OperationCanceledException)
            {
                Logger.LogWarning("Email sending operation was cancelled (caught in SendCompletionEmailAsync).");
                progress.Report("Email sending cancelled.");
                throw;
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error sending email: {ex.Message}", ex);
                FlexibleMessageBox.Show(this, $"Failed to send email: {ex.Message}", "Email Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                throw;
            }
        }

        /// <summary>
        /// Retrieves the To and CC email recipients for the current report context (manual run).
        /// Delegates to <see cref="EmailRecipientManager"/> for the actual logic.
        /// </summary>
        /// <returns>A tuple containing a list of 'To' addresses and a list of 'CC' addresses.</returns>
        private (List<string> To, List<string> Cc) GetEmailRecipients()
        {
            Logger.LogTrace("Form1: Entering GetEmailRecipients for manual run, deferring to EmailRecipientManager...");
            bool isFemiOnly = sendToFemiOnlyCheckBox.Checked && sendToFemiOnlyCheckBox.Visible;
            int currentReportTypeIndex = GetSelectedReportTypeIndex();

            var recipients = _emailRecipientManager.GetRecipients(currentReportTypeIndex, isFemiOnly, IsDebug, isAutoRunContext: false);

            Logger.LogDebug($"Form1: Recipients from Manager for manual run - To: {string.Join("; ", recipients.To)}, CC: {string.Join("; ", recipients.Cc)} (FemiOnly: {isFemiOnly}, IsDebug: {IsDebug})");
            Logger.LogTrace("Form1: Exiting GetEmailRecipients.");
            return recipients;
        }

        /// <summary>
        /// Constructs the email subject and body for the current report context (manual run).
        /// Uses <see cref="GreetingManager"/> to retrieve configurable greetings.
        /// </summary>
        /// <param name="reportStartDate">The start date of the report period.</param>
        /// <param name="reportEndDate">The end date of the report period.</param>
        /// <returns>A tuple containing the email subject and body strings.</returns>
        private (string Subject, string Body) GetEmailSubjectAndBody(DateTime reportStartDate, DateTime reportEndDate)
        {
            string typeName = "Estimate Success Rate";
            string reportTypeString = "";
            UIManager.SafeControlUpdate(reportTypeComboBox, () => reportTypeString = reportTypeComboBox.Text);

            int currentReportTypeIndex = GetSelectedReportTypeIndex();
            bool femiOnlyChecked = sendToFemiOnlyCheckBox.Checked && sendToFemiOnlyCheckBox.Visible;

            string greeting;
            string greetingKeyName;

            if (IsDebug)
            {
                greetingKeyName = "DebugDefault";
                greeting = _greetingManager.GetGreeting(greetingKeyName, isForDebugSection: true);
            }
            else
            {
                if (currentReportTypeIndex == DailyReportIndex)
                {
                    greetingKeyName = "ManualStdDaily";
                }
                else if (currentReportTypeIndex == NewDailyReportOver1kIndex)
                {
                    greetingKeyName = femiOnlyChecked ? "ManualFemi" : "ManualTeam";
                }
                else if (currentReportTypeIndex == WeeklyReportIndex ||
                         currentReportTypeIndex == MonthlyReportIndex ||
                         currentReportTypeIndex == QuarterlyReportIndex ||
                         currentReportTypeIndex == AnnualReportIndex ||
                         currentReportTypeIndex == CustomReportIndex)
                {
                    greetingKeyName = femiOnlyChecked ? "ManualFemi" : "ManualTeam";
                }
                else
                {
                    greetingKeyName = "ManualTeam";
                    Logger.LogWarning($"Manual run for unexpected report type '{reportTypeString}' (Index: {currentReportTypeIndex}). Using fallback greeting key '{greetingKeyName}'.");
                }
                greeting = _greetingManager.GetGreeting(greetingKeyName);
            }

            if (!string.IsNullOrWhiteSpace(greeting) && !greeting.TrimEnd().EndsWith(","))
            {
                greeting = greeting.TrimEnd() + ",";
            }

            string rangeInfo;
            string subjectPrefix = $"{reportTypeString} {typeName}";

            switch (currentReportTypeIndex)
            {
                case DailyReportIndex: rangeInfo = $"for {reportEndDate:dd MMM yy}"; break;
                case NewDailyReportOver1kIndex: rangeInfo = $"for period {reportStartDate:dd MMM yy} to {reportEndDate:dd MMM yy}"; break;
                case WeeklyReportIndex: rangeInfo = $"for period {reportStartDate:dd MMM yy} to {reportEndDate:dd MMM yy}"; break;
                case MonthlyReportIndex: rangeInfo = $"for {reportStartDate:MMMM yy}"; break;
                case QuarterlyReportIndex: rangeInfo = $"for {ReportHelper.GetQuarterString(reportStartDate)} {reportStartDate.Year}"; break;
                case AnnualReportIndex: rangeInfo = $"for Financial Year {reportStartDate.Year}-{reportEndDate.Year}"; subjectPrefix = $"Annual {typeName}"; break;
                case CustomReportIndex: rangeInfo = $"for period {reportStartDate:dd MMM yy} to {reportEndDate:dd MMM yy}"; break;
                default: rangeInfo = $"for period {reportStartDate:dd MMM yy} to {reportEndDate:dd MMM yy}"; subjectPrefix = $"Report {typeName}"; break;
            }

            string subjectDateSuffix = (reportStartDate.Date == reportEndDate.Date) ? $"({reportEndDate:yyyy-MM-dd})" : $"({reportStartDate:yyyy-MM-dd} to {reportEndDate:yyyy-MM-dd})";
            if (currentReportTypeIndex == AnnualReportIndex) subjectDateSuffix = $"({reportStartDate.Year}-{reportEndDate.Year})";
            // For weekly, the general suffix is fine as rangeInfo and subjectPrefix already clarify.

            string manualPrefix = (currentReportTypeIndex != CustomReportIndex && currentReportTypeIndex != -1) ? "MANUAL: " : "";
            string subject = $"{manualPrefix}{subjectPrefix} Report {subjectDateSuffix}";
            if (IsDebug) subject = $"DEBUG - {subject}";

            string body = $"{greeting}\n\nPlease find attached the {subjectPrefix.ToLower()} report {rangeInfo}.\n\nThis report includes quotes data for review.\n\nThank you,\nAutomation Service";
            Logger.LogDebug($"GetEmailSubjectAndBody: GreetingKey='{greetingKeyName}', Subject='{subject}'");
            return (subject, body);
        }

        /// <summary>
        /// Handles the process of prompting the user for manual Excel refresh if required by the report type.
        /// Opens the Excel file, waits for the user to close it, and confirms if emailing should proceed.
        /// </summary>
        /// <param name="filePath">The path to the Excel file requiring manual refresh.</param>
        /// <param name="token">A CancellationToken to observe for cancellation requests.</param>
        /// <returns>True if the user confirms to proceed after refresh (or if no refresh was needed); false if cancelled or an error occurs.</returns>
        private async Task<bool> HandleManualExcelRefreshAsync(string filePath, CancellationToken token)
        {
            _uiManager.UpdateProgress("Checking for running Excel instances...");
            if (await Task.Run(() => Process.GetProcessesByName("EXCEL").Length > 0, token))
            {
                DialogResult fdr = FlexibleMessageBox.Show(this,
                    "Other Excel instances are running. It's recommended to close them before proceeding with the manual refresh to avoid conflicts.\n\nAttempt to close other Excel instances automatically?",
                    "Close Other Excel Instances?", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning);

                if (fdr == DialogResult.Cancel) { Logger.LogInfo("User cancelled manual refresh due to other Excel instances."); return false; }
                if (fdr == DialogResult.Yes)
                {
                    _uiManager.UpdateProgress("Attempting to close other Excel instances...");
                    await Task.Run(() => ReportHelper.CloseProcessesByName("EXCEL"), token);
                    await Task.Delay(1500, token);
                }
            }

            FlexibleMessageBox.Show(this,
                "The report will now open in Excel.\n\n" +
                "*** IMPORTANT ***\n" +
                "1. Enable Editing if prompted by Excel.\n" +
                "2. Go to the 'Data' tab and click 'Refresh All'.\n" +
                "3. Ensure all PivotTables and data connections are updated.\n" +
                "4. SAVE the file.\n" +
                "5. CLOSE Excel.\n\n" +
                "The application will wait for you to close Excel before continuing.",
                "Manual Refresh Required", MessageBoxButtons.OK, MessageBoxIcon.Information);

            token.ThrowIfCancellationRequested();
            _uiManager.UpdateProgress("Opening Excel for manual refresh...");
            Process? excelProc = null;
            try
            {
                excelProc = await Task.Run(() => Process.Start(new ProcessStartInfo(filePath) { UseShellExecute = true }), token);
                if (excelProc == null) throw new InvalidOperationException("Failed to start Excel process. Ensure Excel is installed and .xlsx files are associated.");

                _uiManager.UpdateProgress("Excel opened. Waiting for you to Refresh All, Save, and Close Excel...");
                await excelProc.WaitForExitAsync(token);
                _uiManager.UpdateStatusMain("Excel closed by user.");

                DialogResult sendResult = FlexibleMessageBox.Show(this, "Excel has been closed.\n\nProceed with sending the email (if not skipped)?", "Confirm Email Send", MessageBoxButtons.OKCancel, MessageBoxIcon.Question);
                return (sendResult == DialogResult.OK || sendResult == DialogResult.Yes);
            }
            catch (OperationCanceledException)
            {
                Logger.LogWarning("Manual Excel refresh process was cancelled by timeout or user action.");
                if (excelProc != null && !excelProc.HasExited) { try { excelProc.Kill(true); } catch (Exception killEx) { Logger.LogWarning($"Could not kill Excel process during cancellation: {killEx.Message}"); } }
                return false;
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error during manual Excel handling: {ex.Message}", ex);
                FlexibleMessageBox.Show(this, $"An unexpected error occurred managing the Excel refresh step:\n\n{ex.Message}", "Excel Interaction Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
            finally
            {
                excelProc?.Dispose();
            }
        }
        #endregion
    }
}
