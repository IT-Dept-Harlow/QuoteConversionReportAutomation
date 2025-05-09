// C# 10+ Features
// Ensure this namespace matches your project structure, e.g., conversionTest
namespace conversionTest
{
    // --- Global Usings ---
    using Microsoft.Extensions.Configuration;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Drawing;
    using System.IO;
    using System.Windows.Forms;
    using System.Threading;
    using System.Threading.Tasks;
    using ReportWrapperCommon;
    using QuoteConversionReportAutomation; // For EmailRecipientManager, ManageEmailRecipientsForm etc.
    using QuoteConversionReportAutomation.Helpers;
    using QuoteConversionReportAutomation.Managers;
    using Microsoft.VisualBasic; // For Interaction.InputBox
    using System.Text; // Required for StringBuilder

    /// <summary>
    /// Represents the main form of the Quote Conversion Report Automation application.
    /// Orchestrates report generation and processing by coordinating with manager classes.
    /// Handles UI events and delegates UI updates to UIManager.
    /// Manages the Auto-Run timer, delegating the check logic to AutoRunManager.
    /// Includes handling for a "Custom" report type triggered by manual date changes.
    /// Includes background archiving of old report files on startup.
    /// Daily report date calculation now considers bank holidays.
    /// Added new options menu items including "Manage Email Recipients".
    /// Added 1-Click processing mode, skip email checkbox, and option to set auto-run hour.
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
        private readonly EmailRecipientManager _emailRecipientManager;

        // --- Application Info ---
        // AppVersion is used in the help text.
        private const string AppVersion = "1.7.9";

        // --- State Variables (Remaining in Form1) ---
        private string _generatedReportPath = string.Empty;
        private string _generatedAnalysisFilePath = string.Empty;
        private bool _programmaticallyChangingDates = false;
        // _currentAutoRunHour is used in the help text.
        private int _currentAutoRunHour;

        // --- Configuration Paths (Needed for Instantiation) ---
        private static readonly string appSettingsBasePath = DetermineAppSettingsBasePath();
        private readonly string _appSettingsPath = Path.Combine(appSettingsBasePath, "appsettings.json");

        // --- Report Type Constants ---
        private const int DailyReportIndex = 0;
        private const int WeeklyReportIndex = 1;
        private const int MonthlyReportIndex = 2;
        private const int QuarterlyReportIndex = 3;
        private const int AnnualReportIndex = 4;
        private const int CustomReportIndex = 5;

        // --- Build Configuration Helper ---
        private static bool IsDebug =>
#if DEBUG
            true;
#else
            false;
#endif

        // --- Configuration Properties (Read from _configuration) ---
        private string UserProfilePath => Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        private string RawReportExportBaseDir => Path.Combine(UserProfilePath, _configuration["settings:RawReportExportBaseDir"]?.TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) ?? @"Harlow Printing\IT Projects - Documents\Dashboard Datasets\Raw_data\Quotes conversion\Estimate Reports Exports");
        public string ExcelFinalSaveLocation => Path.Combine(UserProfilePath, _configuration["settings:ExcelFinalSaveLocation"]?.TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) ?? @"Harlow Printing\IT Projects - Documents\Dashboard Datasets\Raw_data\Quotes conversion\Estimates");
        private string CrystalReportLocation => _configuration["settings:CrystalReportPath"] ?? string.Empty;
        public string ExcelTemplateBaseDir => Path.Combine(UserProfilePath, _configuration["settings:ExcelTemplateFolder"]?.TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) ?? @"Harlow Printing\IT Projects - Documents\Dashboard Datasets\Raw_data\Quotes conversion\TEMPLATE");
        private string ConfiguredLogDirectoryBase => _configuration["settings:LogDirectory"] ?? string.Empty;

        // --- Dynamic Path Properties (Depend on UI state or config) ---
        public string ReportOutputLocation
        {
            get
            {
                string baseDir = RawReportExportBaseDir;
                string fileName = $"{endDatePicker.Value:yyyyMMdd}_EstimateSuccessReport_Raw.xlsx";
                DateTime folderTimestampDate = (reportTypeComboBox.SelectedIndex == CustomReportIndex) ? DateTime.Now : endDatePicker.Value;
                string? specificFolder = FolderCreation.GetReportSpecificFolderPath(reportTypeComboBox.SelectedIndex, baseDir, folderTimestampDate);

                if (string.IsNullOrEmpty(specificFolder))
                {
                    Logger.LogError($"Could not determine specific folder path for ReportOutputLocation. ReportType: {reportTypeComboBox.SelectedIndex}, Base: {baseDir}");
                    string reportTypeSubFolder = reportTypeComboBox.SelectedIndex switch
                    {
                        DailyReportIndex => "Daily Reports",
                        WeeklyReportIndex => "Weekly Reports",
                        MonthlyReportIndex => "Monthly Reports",
                        QuarterlyReportIndex => "Quarterly reports",
                        AnnualReportIndex => "Annual Reports",
                        CustomReportIndex => "Custom Reports",
                        _ => "Other Reports"
                    };
                    specificFolder = Path.Combine(baseDir, reportTypeSubFolder);
                    try { Directory.CreateDirectory(specificFolder); } catch (Exception ex) { Logger.LogError($"Failed to create fallback directory '{specificFolder}': {ex.Message}"); }
                }
                return Path.Combine(specificFolder, fileName);
            }
        }
        public string ExcelTemplateLocation
        {
            get
            {
                string baseDir = ExcelTemplateBaseDir;
                string templateName = reportTypeComboBox.SelectedIndex switch
                {
                    MonthlyReportIndex or QuarterlyReportIndex or AnnualReportIndex => "TEMPLATE_Estimate Success Rate_Monthly.xlsx",
                    CustomReportIndex => "TEMPLATE_Estimate Success Rate_Monthly.xlsx",
                    _ => "TEMPLATE_Estimate Success Rate.xlsx"
                };
                return Path.Combine(baseDir, templateName);
            }
        }
        #endregion

        #region Constructor
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

                _uiManager = new UIManager(
                    this, menuStrip1, mainStatusStrip, statusLabel, autoRunStatusLabel,
                    darkModeToolStripMenuItem, createReportButton, processEmailButton,
                    generateAndSendButton, oneClickProcessButton,
                    toggleAutoRunButton, viewReportButton, viewAnalysisButton,
                    reportTypeComboBox, startDatePicker, endDatePicker,
                    financialYearComboBox, financialYearLabel, sendToFemiOnlyCheckBox,
                    skipEmailCheckBox, emailRecipientLabel, toolTip1
                );

                string wrapperExePath = Path.GetFullPath(_configuration["settings:WrapperExePath"] ?? "CrystalReportWrapper.exe");
                _processManager = new ReportProcessManager(wrapperExePath);
                _pipeCommunicator = new NamedPipeCommunicator();
                _currentAutoRunHour = _configuration.GetValue<int>("settings:AutoRunCheckHour", 8);
                _autoRunManager = new AutoRunManager(
                    _configuration, _emailUtility, _processManager, _pipeCommunicator,
                    _uiManager, _excelProcessor, _appSettingsPath, _emailRecipientManager, _currentAutoRunHour
                );

                this.enable1ClickProcessingToolStripMenuItem.Click += new System.EventHandler(this.enable1ClickProcessingToolStripMenuItem_Click);
                this.oneClickProcessButton.Click += new System.EventHandler(this.oneClickProcessButton_Click);
                this.setAutoRunHourToolStripMenuItem.Click += new System.EventHandler(this.setAutoRunHourToolStripMenuItem_Click);
                this.startDatePicker.ValueChanged += new System.EventHandler(this.DatePicker_ValueChanged);
                this.endDatePicker.ValueChanged += new System.EventHandler(this.DatePicker_ValueChanged);

                Logger.LogDebug("Event handlers wired up.");
            }
            catch (Exception ex)
            {
                Logger.LogCritical($"CRITICAL ERROR during Form Initialization: {ex.Message}", ex);
                MessageBox.Show($"A critical error occurred initializing the application:\n\n{ex.Message}\n\nThe application cannot continue.",
                                        "Initialization Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                throw;
            }
            Logger.LogTrace("Exiting Form1 Constructor");
        }
        #endregion

        #region Form Load / Closing
        private async void Form1_Load(object sender, EventArgs e)
        {
            Logger.LogTrace("Entering Form1_Load");
            _uiManager.UpdateStatusMain("Loading application...");
            try
            {
                BankHolidayHelper.Initialize();
                Logger.LogInfo("BankHolidayHelper initialized.");
                Logger.LogInfo("Form Loading...");

                string crystalReportPath = CrystalReportLocation;
                string wrapperExePath = _configuration["settings:WrapperExePath"] ?? string.Empty;
                bool configValid = true;

                if (string.IsNullOrEmpty(crystalReportPath) || !File.Exists(crystalReportPath))
                {
                    Logger.LogError($"Config 'settings:CrystalReportPath' missing or file not found: '{crystalReportPath}'. Report generation disabled.");
                    configValid = false;
                }
                if (string.IsNullOrEmpty(wrapperExePath) || !File.Exists(Path.GetFullPath(wrapperExePath)))
                {
                    Logger.LogError($"Config 'settings:WrapperExePath' missing or file not found: '{wrapperExePath}'. Report generation disabled.");
                    configValid = false;
                }

                Text = $"Quote Conversion Automation - {(IsDebug ? "DEBUG" : "RELEASE")} - v{AppVersion}";
                StartPosition = FormStartPosition.CenterScreen;
                financialYearComboBox.DropDownStyle = ComboBoxStyle.DropDownList;

                if (reportTypeComboBox.Items.Count == 5) { reportTypeComboBox.Items.Add("Custom"); }
                if (reportTypeComboBox.Items.Count > DailyReportIndex) reportTypeComboBox.SelectedIndex = DailyReportIndex;
                else if (reportTypeComboBox.Items.Count > 0) reportTypeComboBox.SelectedIndex = 0;
                reportTypeComboBox.DropDownStyle = ComboBoxStyle.DropDownList;

                bool useDarkMode = UIManager.IsWindowsDarkModeEnabled();
                darkModeToolStripMenuItem.Checked = useDarkMode;
                _uiManager.ApplyTheme(useDarkMode);

                // Update AutoRun button text and tooltip on load
                string autoRunButtonText = $"{(dailyCheckTimer.Enabled ? "Disable" : "Enable")} Daily Auto Run @ {_currentAutoRunHour}:00";
                UIManager.SafeControlUpdate(toggleAutoRunButton, () => toggleAutoRunButton.Text = autoRunButtonText);
                toolTip1.SetToolTip(toggleAutoRunButton, $"Enable or disable the automated daily report generation. The report runs around {_currentAutoRunHour}:00 for the previous workday.");

                // Update AutoRun UI status label
                _uiManager.UpdateAutoRunUI(dailyCheckTimer.Enabled, false, useDarkMode, $"Auto Run: {(dailyCheckTimer.Enabled ? $"Enabled (Next check ~{_currentAutoRunHour}:00)" : "Disabled")}");

                reportTypeComboBox_SelectedIndexChanged(reportTypeComboBox, EventArgs.Empty);
                _uiManager.ResetButtonStatesAfterTypeChange(configValid);

                enable1ClickProcessingToolStripMenuItem.Checked = false;
                Update1ClickProcessingModeUI(); // Call after other UI setups

                if (!configValid) _uiManager.UpdateStatusMain("Config Error: Check Options menu.");

                _uiManager.UpdateStatusMain("Checking report service...");
                IProgress<string> loadProgress = new Progress<string>(status => _uiManager.UpdateProgress(status));
                bool wrapperOk = await _processManager.EnsureWrapperIsRunningAsync(loadProgress);

                if (!wrapperOk && configValid)
                {
                    _uiManager.UpdateStatusMain("Report service failed to start.");
                    _uiManager.ResetUIOnError("Config Error", false, false, false, IsDailySelected(), dailyCheckTimer.Enabled, useDarkMode, false, string.Empty);
                }

                string? finalDir = ExcelFinalSaveLocation;
                string? rawDir = RawReportExportBaseDir;
                int? archiveDays = _configuration.GetValue<int?>("settings:ArchiveRawOlderThanDays");
                _ = Task.Run(async () => await ReportArchiver.ArchiveOldReportsAsync(finalDir, rawDir, archiveDays))
                        .ContinueWith(t => { if (t.IsFaulted) Logger.LogError($"Background report archiving task failed: {t.Exception?.Flatten().InnerException?.Message}"); }, TaskScheduler.Default);

                Logger.LogInfo("Form Load Initialisation Complete.");
                if (configValid && wrapperOk) _uiManager.UpdateStatusMain("Ready");
                else if (configValid && !wrapperOk) _uiManager.UpdateStatusMain("Ready (Service Issue)");
                else _uiManager.UpdateStatusMain("Config Error (Service Check Skipped)");
            }
            catch (Exception ex)
            {
                Logger.LogCritical($"CRITICAL ERROR during Form_Load: {ex.Message}", ex);
                FlexibleMessageBox.Show($"A critical error occurred loading the application:\n\n{ex.Message}\n\nThe application may not function correctly.", "Application Load Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                _uiManager.UpdateStatusMain("Error during load.");
            }
            Logger.LogTrace("Exiting Form1_Load");
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            Logger.LogInfo("Form closing. Stopping timer and terminating wrapper process.");
            dailyCheckTimer.Stop();
            _processManager.TerminateWrapperProcess();
        }
        #endregion

        #region Event Handlers (Create, Process, 1-Click, View)

        private async void createReportButton_Click(object sender, EventArgs e)
        {
            await PerformCreateReportAsync();
        }

        private async void processEmailButton_Click(object sender, EventArgs e)
        {
            await PerformProcessAndEmailAsync(skipEmail: skipEmailCheckBox.Checked);
        }

        private async void generateAndSendButton_Click(object sender, EventArgs e) // Legacy button
        {
            Logger.LogInfo("Legacy Generate & Send button clicked. This button is being phased out by 1-Click Process.");
            _uiManager.UpdateStatusMain("Generate & Send: Starting...");

            UIManager.SafeControlUpdate(oneClickProcessButton, () => oneClickProcessButton.Enabled = false);
            UIManager.SafeControlUpdate(createReportButton, () => createReportButton.Enabled = false);
            UIManager.SafeControlUpdate(processEmailButton, () => processEmailButton.Enabled = false);
            UIManager.SafeControlUpdate(generateAndSendButton, () => generateAndSendButton.Enabled = false);

            _uiManager.SetOtherControlsEnabled(false, financialYearComboBox.Visible);

            await PerformCreateReportAsync();
            if (string.IsNullOrEmpty(_generatedReportPath) || !File.Exists(_generatedReportPath))
            {
                Logger.LogWarning("Generate & Send: Raw report generation failed or was cancelled. Aborting.");
                ResetUIStateOnError("Create Report");
                return;
            }

            await PerformProcessAndEmailAsync(skipEmail: skipEmailCheckBox.Checked);
            Logger.LogInfo("Generate & Send sequence completed (or aborted).");
        }

        private async void oneClickProcessButton_Click(object sender, EventArgs e)
        {
            Logger.LogInfo("1-Click Process button clicked.");
            _uiManager.UpdateStatusMain("1-Click Process: Starting...");

            UIManager.SafeControlUpdate(oneClickProcessButton, () => oneClickProcessButton.Enabled = false);
            // Ensure other buttons are also disabled if somehow visible (defensive)
            UIManager.SafeControlUpdate(createReportButton, () => createReportButton.Enabled = false);
            UIManager.SafeControlUpdate(processEmailButton, () => processEmailButton.Enabled = false);
            _uiManager.SetOtherControlsEnabled(false, financialYearComboBox.Visible);

            await PerformCreateReportAsync();

            if (string.IsNullOrEmpty(_generatedReportPath) || !File.Exists(_generatedReportPath))
            {
                Logger.LogWarning("1-Click Process: Raw report generation failed or was cancelled. Aborting.");
                ResetUIStateOnError("1-Click Process"); // Reset UI with appropriate button text
                return;
            }

            await PerformProcessAndEmailAsync(skipEmail: skipEmailCheckBox.Checked);

            Logger.LogInfo("1-Click Process sequence completed (or aborted).");
            // ResetUIStateOnError is typically called within PerformProcessAndEmailAsync or PerformCreateReportAsync on error/completion
        }

        private void viewReportButton_Click(object sender, EventArgs e)
        {
            ReportHelper.OpenFileWithDefaultApp(_generatedReportPath, "raw report output");
        }

        private void viewAnalysisButton_Click(object sender, EventArgs e)
        {
            ReportHelper.OpenFileWithDefaultApp(_generatedAnalysisFilePath, "processed analysis file");
        }
        #endregion

        #region Core Report Logic (PerformCreateReportAsync, PerformProcessAndEmailAsync)

        private async Task PerformCreateReportAsync()
        {
            Button currentActionButton = oneClickProcessButton.Visible ? oneClickProcessButton : createReportButton;
            string originalButtonText = string.Empty; // To restore text on non-1-click mode if only create fails
            UIManager.SafeControlUpdate(currentActionButton, () => originalButtonText = currentActionButton.Text);

            // Disable the active button and other controls
            UIManager.SafeControlUpdate(currentActionButton, () => currentActionButton.Enabled = false);
            if (currentActionButton == createReportButton) // If in 2-button mode, also disable the other main action button
            {
                UIManager.SafeControlUpdate(processEmailButton, () => processEmailButton.Enabled = false);
            }
            else // oneClickProcessButton is the current action button, ensure the others are disabled too
            {
                UIManager.SafeControlUpdate(createReportButton, () => createReportButton.Enabled = false);
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
                if (!ValidateInputDates()) { ResetUIStateOnError(originalButtonText); return; } // Use original text for reset
                if (!ValidateFinancialYearSelection()) { ResetUIStateOnError(originalButtonText); return; }

                string crystalReportPath = CrystalReportLocation;
                if (string.IsNullOrEmpty(crystalReportPath) || !File.Exists(crystalReportPath))
                { throw new InvalidOperationException("Crystal Report location is invalid or file not found."); }

                if (!await _processManager.EnsureWrapperIsRunningAsync(progress, cts.Token))
                { throw new InvalidOperationException($"Failed to start or connect to the report service."); }

                string reportOutputPath = ReportOutputLocation;
                var request = new ReportRequest
                {
                    CrystalReportLocation = crystalReportPath,
                    ReportOutputLocation = reportOutputPath,
                    ReportDateFrom = startDatePicker.Value,
                    ReportDateTo = endDatePicker.Value
                };

                Logger.LogInfo("Attempting Named Pipe communication...");
                ReportResponse? response = await _pipeCommunicator.SendRequestReceiveResponseAsync(request, progress, cts.Token);

                if (response?.Success == true && !string.IsNullOrEmpty(response.OutputPath) && File.Exists(response.OutputPath))
                {
                    _generatedReportPath = response.OutputPath;
                    Logger.LogInfo($"Report generated successfully by wrapper: {_generatedReportPath}");

                    if (oneClickProcessButton.Visible) // In 1-click mode
                    {
                        // Button remains disabled and text "Requesting..." or "Processing..."
                        // It will be fully reset at the end of PerformProcessAndEmailAsync
                    }
                    else // In 2-button mode
                    {
                        UIManager.SafeControlUpdate(createReportButton, () => createReportButton.Text = "Report Created");
                        // processEmailButton is already disabled, will be enabled if create is successful
                        UIManager.SafeControlUpdate(processEmailButton, () => processEmailButton.Enabled = true);
                        _uiManager.SetOtherControlsEnabled(true, financialYearComboBox.Visible); // Re-enable other controls
                    }
                    _uiManager.ShowViewReportButton(true, _generatedReportPath);
                    _uiManager.ShowViewAnalysisButton(false);
                    _generatedAnalysisFilePath = string.Empty;
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
                FlexibleMessageBox.Show("The report generation request timed out or was cancelled.", "Timeout / Cancelled", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                ResetUIStateOnError("Cancelled"); // Resets based on current 1-click mode
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error during Create Report operation: {ex.Message}", ex);
                FlexibleMessageBox.Show($"An error occurred while requesting the report:\n\n{ex.Message}", "Report Request Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                ResetUIStateOnError("Error"); // Resets based on current 1-click mode
            }
        }

        private async Task PerformProcessAndEmailAsync(bool skipEmail = false)
        {
            Logger.LogTrace($"Entering PerformProcessAndEmailAsync logic (skipEmail: {skipEmail})");
            Button currentActionButton = oneClickProcessButton.Visible ? oneClickProcessButton : processEmailButton;
            string originalButtonText = string.Empty;
            UIManager.SafeControlUpdate(currentActionButton, () => originalButtonText = currentActionButton.Text);

            // Ensure the correct button is targeted for updates and all relevant buttons are disabled
            UIManager.SafeControlUpdate(currentActionButton, () => currentActionButton.Enabled = false);
            if (currentActionButton == processEmailButton) // If in 2-button mode, createReportButton should also be disabled
            {
                UIManager.SafeControlUpdate(createReportButton, () => createReportButton.Enabled = false);
            }
            else // oneClickProcessButton is active, ensure the others are disabled.
            {
                UIManager.SafeControlUpdate(createReportButton, () => createReportButton.Enabled = false);
                UIManager.SafeControlUpdate(processEmailButton, () => processEmailButton.Enabled = false);
            }
            _uiManager.SetOtherControlsEnabled(false, financialYearComboBox.Visible);
            UIManager.SafeControlUpdate(currentActionButton, () => currentActionButton.Text = "Processing...");

            IProgress<ProgressReport> excelProgress = new Progress<ProgressReport>(report => _uiManager.UpdateProgress(report));
            IProgress<string> generalProgress = new Progress<string>(message => _uiManager.UpdateProgress(message));

            _uiManager.UpdateProgress("Starting Excel processing...");

            using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(15));
            var token = cts.Token;
            string? finalFilePath = null;
            int reportType = reportTypeComboBox.SelectedIndex;
            bool requiresManualRefresh = reportType is MonthlyReportIndex or QuarterlyReportIndex or AnnualReportIndex or CustomReportIndex;
            string baseSaveLocation = ExcelFinalSaveLocation;
            DateTime reportDate = endDatePicker.Value;

            try
            {
                if (!ValidateInputDates()) { ResetUIStateOnError(originalButtonText); return; }
                if (string.IsNullOrEmpty(_generatedReportPath) || !File.Exists(_generatedReportPath))
                {
                    FlexibleMessageBox.Show("The raw report file has not been generated or cannot be found. Please create the report first.", "Raw Report Missing", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    ResetUIStateOnError(oneClickProcessButton.Visible ? "1-Click Process" : "Create Report");
                    return;
                }

                string? expectedFinalPath = _excelProcessor.GetExpectedFinalFilePath(reportType, baseSaveLocation, reportDate);
                if (expectedFinalPath != null && File.Exists(expectedFinalPath))
                {
                    generalProgress.Report("Found existing file. Prompting user...");
                    DialogResult dr = FlexibleMessageBox.Show(
                        this,
                        $"The report file '{Path.GetFileName(expectedFinalPath)}' already exists for this period.\n\n" +
                        "Do you want to skip processing and use this existing file?", // Simplified prompt
                        "File Already Exists", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                    if (dr == DialogResult.Yes)
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
                            if (!proceedToEmail && !token.IsCancellationRequested) { _uiManager.UpdateStatusMain("Manual refresh/confirmation cancelled."); ResetUIStateOnError("Cancelled"); return; }
                            if (token.IsCancellationRequested) throw new OperationCanceledException("Operation cancelled during manual refresh prompt.");
                            generalProgress.Report("Manual refresh confirmed.");
                        }

                        if (!skipEmail && proceedToEmail)
                        {
                            await SendCompletionEmailAsync(finalFilePath, generalProgress, token);
                        }
                        else if (skipEmail)
                        {
                            _uiManager.UpdateStatusMain("Process completed. Email skipped by user.");
                            Logger.LogInfo("Email sending skipped by user checkbox.");
                        }
                        // If process was not cancelled and completed (even if email skipped)
                        if (proceedToEmail || skipEmail)
                        {
                            _uiManager.SetUICompleted(CheckConfigValidity(), IsDailySelected(), dailyCheckTimer.Enabled, darkModeToolStripMenuItem.Checked, false, autoRunStatusLabel.Text ?? "");
                        }
                        ResetUIStateOnError(oneClickProcessButton.Visible ? "1-Click Process" : "Create Report"); // Reset based on current mode
                        return;
                    }
                    else // User chose No, overwrite/regenerate
                    {
                        generalProgress.Report("Deleting existing file...");
                        Logger.LogInfo("User chose to overwrite/regenerate the existing file.");
                        try { File.Delete(expectedFinalPath); Logger.LogInfo($"Deleted existing file: {expectedFinalPath}"); }
                        catch (Exception delEx)
                        {
                            Logger.LogError($"Failed to delete existing file '{expectedFinalPath}': {delEx.Message}");
                            FlexibleMessageBox.Show(this, $"Could not delete the existing report file:\n{expectedFinalPath}\n\nPlease ensure the file is not open and try again.\n\nError: {delEx.Message}",
                                            "File Deletion Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            ResetUIStateOnError("File Error"); return;
                        }
                    }
                }

                generalProgress.Report("Processing new report...");
                finalFilePath = await _excelProcessor.ProcessExcelReportAsync(
                     financialYearComboBox.SelectedItem?.ToString() ?? _excelProcessor.GetCurrentFinancialYear(true),
                     reportType,
                    _generatedReportPath, "Sheet1", baseSaveLocation, ExcelTemplateLocation, "DATA",
                    1, 1, excelProgress, reportDate, token);

                if (string.IsNullOrEmpty(finalFilePath) || !File.Exists(finalFilePath))
                {
                    if (token.IsCancellationRequested) { throw new OperationCanceledException("Excel processing was cancelled."); }
                    else { throw new Exception("Excel processing failed to produce a final file. Check logs for details."); }
                }
                _generatedAnalysisFilePath = finalFilePath;
                _uiManager.ShowViewAnalysisButton(true, finalFilePath);

                bool proceedToEmailAfterGenerate = true;
                if (requiresManualRefresh)
                {
                    generalProgress.Report("Waiting for manual Excel refresh...");
                    proceedToEmailAfterGenerate = await HandleManualExcelRefreshAsync(finalFilePath, token);
                    if (!proceedToEmailAfterGenerate && !token.IsCancellationRequested) { _uiManager.UpdateStatusMain("Manual refresh/confirmation cancelled."); ResetUIStateOnError("Cancelled"); return; }
                    if (token.IsCancellationRequested) throw new OperationCanceledException("Operation cancelled during manual refresh prompt.");
                    generalProgress.Report("Manual refresh confirmed.");
                }

                if (!skipEmail && proceedToEmailAfterGenerate)
                {
                    await SendCompletionEmailAsync(finalFilePath, generalProgress, token);
                }
                else if (skipEmail) // If email is skipped, still log and update status
                {
                    _uiManager.UpdateStatusMain("Process completed. Email skipped by user.");
                    Logger.LogInfo("Email sending skipped by user checkbox.");
                }

                // If the process wasn't cancelled during manual refresh and we reached here
                if (proceedToEmailAfterGenerate || skipEmail)
                {
                    _uiManager.SetUICompleted(CheckConfigValidity(), IsDailySelected(), dailyCheckTimer.Enabled, darkModeToolStripMenuItem.Checked, false, autoRunStatusLabel.Text ?? "");
                }
                ResetUIStateOnError(oneClickProcessButton.Visible ? "1-Click Process" : "Create Report"); // Reset based on current mode

            }
            catch (OperationCanceledException)
            {
                Logger.LogWarning("Excel processing or subsequent step cancelled.");
                ResetUIStateOnError("Cancelled"); // Reset based on current mode
            }
            catch (FileNotFoundException fnfEx)
            {
                Logger.LogError($"File not found during Process & Email operation: {fnfEx.Message}", fnfEx);
                FlexibleMessageBox.Show(this, fnfEx.Message, "File Not Found Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                ResetUIStateOnError("File Error"); // Reset based on current mode
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error during Process & Email operation: {ex.Message}", ex);
                FlexibleMessageBox.Show(this, $"An unexpected error occurred during processing:\n\n{ex.Message}", "Processing Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                ResetUIStateOnError("Error"); // Reset based on current mode
            }
            Logger.LogTrace("Exiting PerformProcessAndEmailAsync logic");
        }
        #endregion

        #region UI Event Handlers (ComboBox, Timer, MenuItems)

        private void reportTypeComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            Logger.LogTrace("Entering reportTypeComboBox_SelectedIndexChanged");
            if (sender is not ComboBox comboBox || comboBox.SelectedItem == null) return;

            int selectedIndex = comboBox.SelectedIndex;

            if (selectedIndex == CustomReportIndex)
            {
                Logger.LogDebug("Report Type changed to Custom. Manual date entry expected.");
                UIManager.SafeControlUpdate(sendToFemiOnlyCheckBox, () => { sendToFemiOnlyCheckBox.Visible = true; });
                UIManager.SafeControlUpdate(emailRecipientLabel, () => { emailRecipientLabel.Visible = false; });
                // When type changes to Custom, reset buttons but maintain current 1-click mode visibility
                _uiManager.ResetButtonStatesAfterTypeChange(CheckConfigValidity()); //This resets button texts and enabled states
                Update1ClickProcessingModeUI(); // This ensures the correct set of buttons is visible
                return;
            }

            DateTime todayValue = DateTime.Today;
            _programmaticallyChangingDates = true;
            try
            {
                (DateTime DateFrom, DateTime DateTo, bool ShowFinYear) rangeInfo = selectedIndex switch
                {
                    DailyReportIndex => (ReportHelper.GetPreviousWorkday(todayValue), ReportHelper.GetPreviousWorkday(todayValue), false),
                    WeeklyReportIndex => (todayValue.AddDays(-15), todayValue, true),
                    MonthlyReportIndex => (ReportHelper.CalculateMonthlyRange(todayValue).DateFrom, ReportHelper.CalculateMonthlyRange(todayValue).DateTo, false),
                    QuarterlyReportIndex => (ReportHelper.CalculateQuarterlyRange(todayValue).DateFrom, ReportHelper.CalculateQuarterlyRange(todayValue).DateTo, false),
                    AnnualReportIndex => (new DateTime(todayValue.Year - 1, 1, 1), new DateTime(todayValue.Year - 1, 12, 31), false),
                    _ => (todayValue, todayValue, true) // Default or Custom (though Custom is handled above)
                };

                UIManager.SafeControlUpdate(startDatePicker, () => { startDatePicker.Value = rangeInfo.DateFrom; });
                UIManager.SafeControlUpdate(endDatePicker, () => { endDatePicker.Value = rangeInfo.DateTo; });
                UIManager.SafeControlUpdate(financialYearLabel, () => { financialYearLabel.Visible = rangeInfo.ShowFinYear; });
                UIManager.SafeControlUpdate(financialYearComboBox, () =>
                {
                    financialYearComboBox.Visible = rangeInfo.ShowFinYear;
                    financialYearComboBox.Enabled = rangeInfo.ShowFinYear; // Ensure enabled state matches visibility
                    if (rangeInfo.ShowFinYear) PopulateFinancialYearDropdown();
                });

                bool isDaily = IsDailySelected();
                UIManager.SafeControlUpdate(sendToFemiOnlyCheckBox, () => { sendToFemiOnlyCheckBox.Visible = !isDaily; });
                UIManager.SafeControlUpdate(emailRecipientLabel, () =>
                {
                    emailRecipientLabel.Visible = isDaily;
                    if (isDaily) emailRecipientLabel.Text = "Emailing Daily report to Paul S"; // Example text
                });

                _uiManager.ResetButtonStatesAfterTypeChange(CheckConfigValidity()); // Resets button texts/enabled
                Update1ClickProcessingModeUI(); // Ensures correct button visibility for the mode
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
            // Update UI to reflect new state and potentially next check time
            _uiManager.UpdateAutoRunUI(dailyCheckTimer.Enabled,
                                      (autoRunStatusLabel.Text?.Contains("Completed") ?? false) || (autoRunStatusLabel.Text?.Contains("Done for") ?? false),
                                      darkModeToolStripMenuItem.Checked,
                                      $"Auto Run: {(dailyCheckTimer.Enabled ? $"Enabled (Next check ~{_currentAutoRunHour}:00)" : "Disabled")}");
            Logger.LogInfo($"AutoRun {(dailyCheckTimer.Enabled ? "Enabled" : "Disabled")} by user.");
        }

        private async void dailyCheckTimer_Tick(object sender, EventArgs e)
        {
            if (!dailyCheckTimer.Enabled) return;

            bool originallyEnabled = dailyCheckTimer.Enabled;
            dailyCheckTimer.Stop();
            Logger.LogInfo("Daily Check Timer Ticked. Performing auto run check...");

            try
            {
                await _autoRunManager.PerformDailyCheckAsync(originallyEnabled, _currentAutoRunHour);
            }
            catch (Exception ex)
            {
                Logger.LogCritical($"CRITICAL ERROR during AutoRunManager.PerformDailyCheckAsync: {ex.Message}", ex);
                _uiManager.UpdateStatusMain("Critical AutoRun Error! Check Logs.");
                _uiManager.UpdateStatusRight("AutoRun: FAILED");
                originallyEnabled = false; // Prevent timer restart on critical error
            }
            finally
            {
                if (originallyEnabled) // Only restart if it was meant to be running and no critical error stopped it
                {
                    dailyCheckTimer.Start();
                    Logger.LogInfo("Daily Check Timer Restarted.");
                }
                else
                {
                    Logger.LogInfo("Daily Check Timer remains stopped after check completion or error.");
                    // If it was disabled due to an error, ensure the UI reflects this "Disabled" state.
                    if (!dailyCheckTimer.Enabled)
                    {
                        _uiManager.UpdateAutoRunUI(false, true, darkModeToolStripMenuItem.Checked, autoRunStatusLabel.Text.Contains("FAILED") ? autoRunStatusLabel.Text : "Auto Run: Disabled (Error)");
                    }
                }

                // Reset main UI buttons if auto-run is not actively indicated as "Running..."
                if (!(autoRunStatusLabel.Text?.Contains("Running") ?? false) &&
                    !(autoRunStatusLabel.Text?.Contains("Starting") ?? false) &&
                    !(autoRunStatusLabel.Text?.Contains("Completed") ?? false) &&
                    !(autoRunStatusLabel.Text?.Contains("Done for") ?? false))
                {
                    // This will set the correct button text and visibility based on current 1-click mode
                    ResetUIStateOnError(oneClickProcessButton.Visible ? "1-Click Process" : "Create Report");
                }
            }
        }

        private void darkModeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _uiManager.ApplyTheme(darkModeToolStripMenuItem.Checked);
            _uiManager.UpdateAutoRunUI(dailyCheckTimer.Enabled,
                                      (autoRunStatusLabel.Text?.Contains("Completed") ?? false) || (autoRunStatusLabel.Text?.Contains("Done for") ?? false),
                                      darkModeToolStripMenuItem.Checked,
                                      $"Auto Run: {(dailyCheckTimer.Enabled ? $"Enabled (Next check ~{_currentAutoRunHour}:00)" : "Disabled")}");
        }

        private void helpToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string helpTitle = $"Help - Quote Conversion v{AppVersion}";
            StringBuilder helpMessageBuilder = new StringBuilder();

            // Using verbatim string literals (@"...") simplifies handling of backslashes in RTF.
            // For RTF, backslashes are significant, so they are kept as single backslashes in the C# string.

            helpMessageBuilder.AppendLine(@"{\rtf1\ansi\ansicpg1252\deff0\nouicompat{\fonttbl{\f0\fnil\fcharset0 Segoe UI;}{\f1\fnil\fcharset2 Symbol;}}");
            helpMessageBuilder.AppendLine(@"{\colortbl ;\red0\green0\blue0;\red255\green0\blue0;}"); // Ensure this color table is correct if you use \cfX commands
            helpMessageBuilder.AppendLine($@"\pard\sa200\sl276\slmult1\b\f0\fs28 Quote Conversion Automation Tool v{AppVersion}\b0\fs20\par");
            helpMessageBuilder.AppendLine(@"\par");
            helpMessageBuilder.AppendLine(@"\b\fs22 Welcome!\b0\fs20\par");
            helpMessageBuilder.AppendLine(@"This tool automates the generation, processing, and emailing of Estimate Success Rate reports, streamlining your workflow.\par");
            helpMessageBuilder.AppendLine(@"\par");
            helpMessageBuilder.AppendLine(@"\b\fs22 How to Use the Application\b0\fs20\par");
            helpMessageBuilder.AppendLine(@"\par");
            helpMessageBuilder.AppendLine(@"\b 1. Select Report Type:\b0\  \par");
            helpMessageBuilder.AppendLine(@"Choose the desired report period from the \b Report Type\b0 dropdown menu. The options are:\par");
            // Using RTF explicit space "\ " after \b0
            helpMessageBuilder.AppendLine(@"    \pard\fi-360\li720{\pntext\f1\'B7\tab} \b Daily:\b0\  Generates a report for the {\i previous working day}. This automatically accounts for weekends (Saturday, Sunday) and official bank holidays (England & Wales, including custom-managed ones). It correctly handles holidays that fall on weekends and dynamic holidays like Easter.\par");
            helpMessageBuilder.AppendLine(@"    \pard\fi-360\li720{\pntext\f1\'B7\tab} \b Weekly:\b0\  Sets the 'To' date to today and the 'From' date to 15 days prior. {\i (Note: For Weekly reports, data is also appended to a 'powerBI' sheet in a central Excel file for Power BI integration.)}\par");
            helpMessageBuilder.AppendLine(@"    \pard\fi-360\li720{\pntext\f1\'B7\tab} \b Monthly:\b0\  Calculates the 'From' and 'To' dates for the {\i previous full calendar month}.\par");
            helpMessageBuilder.AppendLine(@"    \pard\fi-360\li720{\pntext\f1\'B7\tab} \b Quarterly:\b0\  Calculates the 'From' and 'To' dates for the {\i previous full calendar quarter}.\par");
            helpMessageBuilder.AppendLine(@"    \pard\fi-360\li720{\pntext\f1\'B7\tab} \b Annual:\b0\  Sets the 'From' and 'To' dates for the {\i previous full calendar year}.\par");
            helpMessageBuilder.AppendLine(@"    \pard\fi-360\li720{\pntext\f1\'B7\tab} \b Custom:\b0\  Allows you to manually set any 'From' and 'To' date range.\par");
            helpMessageBuilder.AppendLine(@"\pard\sa200\sl276\slmult1 When you select a standard report type (Daily, Weekly, etc.), the 'From' and 'To' dates will adjust automatically based on the {\i current date}.\par");
            helpMessageBuilder.AppendLine(@"\par");
            helpMessageBuilder.AppendLine(@"\b 2. Adjust Dates (Optional for 'Custom' Report Type):\b0\  \par");
            helpMessageBuilder.AppendLine(@"You can manually change the \b Enter From Date\b0 and \b Enter To Date\b0 fields. If you modify these dates after selecting a standard report type, the \b Report Type\b0 will automatically switch to \b Custom\b0.\par");
            helpMessageBuilder.AppendLine(@"\par");
            helpMessageBuilder.AppendLine(@"\b 3. Financial Year (If Applicable):\b0\  \par");
            helpMessageBuilder.AppendLine(@"For \b Weekly\b0 and \b Daily\b0 reports (or if manually made visible for Custom), ensure the correct \b Financial Year\b0 is selected from its dropdown. It usually defaults based on the current date.\par");
            helpMessageBuilder.AppendLine(@"\par");
            helpMessageBuilder.AppendLine(@"\b 4. Report Processing Options:\b0\  \par");
            helpMessageBuilder.AppendLine(@"    \pard\fi-360\li720{\pntext\f1\'B7\tab} \b Send to only Femi?:\b0\  (Checkbox, visible for non-Daily reports) Check this to restrict the email recipient list primarily to Femi (and relevant IT CCs depending on Debug/Release mode). Uncheck to send to the broader configured team for that report type.\par");
            helpMessageBuilder.AppendLine(@"    \pard\fi-360\li720{\pntext\f1\'B7\tab} \b Skip Sending Email:\b0\  (Checkbox) If checked, the application will generate and process the report but will skip the final step of sending the completion email. This is useful if you only need the files locally.\par");
            helpMessageBuilder.AppendLine(@"\pard\sa200\sl276\slmult1\par");
            helpMessageBuilder.AppendLine(@"\b 5. Choose Your Processing Mode (via Options Menu):\b0\  \par");
            helpMessageBuilder.AppendLine(@"    \pard\fi-360\li720{\pntext\f1\'B7\tab} \b Standard 2-Button Mode (Default):\b0\  \par");
            helpMessageBuilder.AppendLine(@"        \pard\fi-720\li1080{\pntext\f0\'31\tab}i. Click the \b Create Report\b0 button. This generates the raw data export from the reporting system. Wait for the status bar to indicate ""Report Created"".\par");
            helpMessageBuilder.AppendLine(@"        \pard\fi-720\li1080{\pntext\f0\'32\tab}ii. Once the raw report is ready, the \b Create Analysis & Send Email\b0 button becomes active. Click it to process the raw data, create the final analysis Excel file, and (if not skipped) email it to the configured recipients.\par");
            helpMessageBuilder.AppendLine(@"    \pard\fi-360\li720{\pntext\f1\'B7\tab} \b 1-Click Processing Mode:\b0\  \par");
            helpMessageBuilder.AppendLine(@"        \pard\fi-720\li1080{\pntext\f0\'31\tab}i. Go to \b Options -> Enable 1-Click Processing\b0 (a checkmark will appear next to it).\par");
            helpMessageBuilder.AppendLine(@"        \pard\fi-720\li1080{\pntext\f0\'32\tab}ii. The two buttons will be replaced by a single button: \b Generate, Process & Email Report\b0 . Clicking this button performs all steps sequentially: creates the raw report, processes it into the final analysis, and (if not skipped) emails it.\par");
            helpMessageBuilder.AppendLine(@"\pard\sa200\sl276\slmult1\par");
            helpMessageBuilder.AppendLine(@"\b 6. Manual Excel Refresh (for Monthly, Quarterly, Annual, Custom reports):\b0\  \par");
            helpMessageBuilder.AppendLine(@"For these report types, after the analysis file is generated (and before emailing), you will be prompted to:\par");
            helpMessageBuilder.AppendLine(@"    \pard\fi-360\li720{\pntext\f1\'B7\tab} Open the generated Excel file.\par");
            helpMessageBuilder.AppendLine(@"    \pard\fi-360\li720{\pntext\f1\'B7\tab} {\i Enable Editing} if prompted by Excel.\par");
            helpMessageBuilder.AppendLine(@"    \pard\fi-360\li720{\pntext\f1\'B7\tab} In the Excel file, navigate to the \b 'OrderPivot'\b0 sheet. Right-click on each PivotTable and any Slicers present, then select \b 'Refresh'\b0 from the context menu.\par");
            helpMessageBuilder.AppendLine(@"    \pard\fi-360\li720{\pntext\f1\'B7\tab} Repeat this process for the \b 'Estimate Success PivotTable'\b0 sheet (or similarly named sheet containing the main estimate success pivot).\par");
            helpMessageBuilder.AppendLine(@"    \pard\fi-360\li720{\pntext\f1\'B7\tab} \b SAVE\b0 the Excel file.\par");
            helpMessageBuilder.AppendLine(@"    \pard\fi-360\li720{\pntext\f1\'B7\tab} \b CLOSE\b0 Excel.\par");
            helpMessageBuilder.AppendLine(@"\pard\sa200\sl276\slmult1 The application will wait for you to close Excel before proceeding to the email step (if not skipped).\par");
            helpMessageBuilder.AppendLine(@"\par");
            helpMessageBuilder.AppendLine(@"\b 7. View Generated Files (Optional):\b0\  \par");
            helpMessageBuilder.AppendLine(@"After the relevant steps are completed, buttons may appear below the main action buttons:\par");
            helpMessageBuilder.AppendLine(@"    \pard\fi-360\li720{\pntext\f1\'B7\tab} \b View File\b0 (below ""Create Report"" area): Opens the generated raw report Excel file.\par");
            helpMessageBuilder.AppendLine(@"    \pard\fi-360\li720{\pntext\f1\'B7\tab} \b View File\b0 (below ""Process and Email"" area): Opens the final processed analysis Excel file.\par");
            helpMessageBuilder.AppendLine(@"\pard\sa200\sl276\slmult1\par");
            helpMessageBuilder.AppendLine(@"\b\fs22 Options Menu Explained\b0\fs20\par");
            helpMessageBuilder.AppendLine(@"The \b Options\b0 menu provides access to various settings and tools:\par");
            helpMessageBuilder.AppendLine(@"    \pard\fi-360\li720{\pntext\f1\'B7\tab} \b Enable 1-Click Processing:\b0\  (Checkable) Toggles between the standard 2-button operation and the single 1-Click button for report generation and processing.\par");
            helpMessageBuilder.AppendLine($@"    \pard\fi-360\li720{{\pntext\f1\'B7\tab}} \b Set Auto-Run Hour...:\b0\  Allows you to change the hour (0-23) at which the automated daily report task attempts to run. The current configured hour is approximately \b {_currentAutoRunHour}:00\b0 .\par");
            helpMessageBuilder.AppendLine(@"    \pard\fi-360\li720{\pntext\f1\'B7\tab} \b Dark Mode:\b0\  (Checkable) Toggles the application's visual theme between light and dark mode.\par");
            helpMessageBuilder.AppendLine(@"    \pard\fi-360\li720{\pntext\f1\'B7\tab} \b View Configuration:\b0\  Displays a summary of critical file paths and settings used by the application, and whether they are valid.\par");
            helpMessageBuilder.AppendLine(@"    \pard\fi-360\li720{\pntext\f1\'B7\tab} \b Validate Configuration:\b0\  Performs a quick check of essential configurations and updates the status bar with the result.\par");
            helpMessageBuilder.AppendLine(@"    \pard\fi-360\li720{\pntext\f1\'B7\tab} \b Manage Custom Bank Holidays:\b0\  Opens a window to add or remove custom one-off or recurring bank holidays that affect the 'previous working day' calculation for Daily reports.\par");
            helpMessageBuilder.AppendLine(@"    \pard\fi-360\li720{\pntext\f1\'B7\tab} \b Manage Email Recipients:\b0\  Opens a window to customize the To/CC email lists for different report types and scenarios (e.g., Daily Auto-Run, Femi-Only, Team reports).\par");
            helpMessageBuilder.AppendLine(@"    \pard\fi-360\li720{\pntext\f1\'B7\tab} \b Open Logs Folder:\b0\  Opens the directory where the application stores its detailed log files. Useful for troubleshooting.\par");
            helpMessageBuilder.AppendLine(@"    \pard\fi-360\li720{\pntext\f1\'B7\tab} \b Edit appsettings.json:\b0\  Opens the main configuration file (`appsettings.json`) in your default text editor. {\i Use with extreme caution, as incorrect changes can break the application.}\par");
            helpMessageBuilder.AppendLine(@"    \pard\fi-360\li720{\pntext\f1\'B7\tab} \b Exit:\b0\  Closes the application.\par");
            helpMessageBuilder.AppendLine(@"\pard\sa200\sl276\slmult1\par");
            helpMessageBuilder.AppendLine(@"\b\fs22 Auto-Run Feature\b0\fs20\par");
            helpMessageBuilder.AppendLine(@"Located on the left side of the main window, the \b Enable/Disable Daily Auto Run\b0 button controls the automated daily report generation.\par");
            helpMessageBuilder.AppendLine($@"    \pard\fi-360\li720{{\pntext\f1\'B7\tab}} \b Functionality:\b0\  When enabled, the application will check daily around the configured hour (e.g., {_currentAutoRunHour}:00). If the \b Daily\b0 report for the {{\i previous working day}} (accounting for weekends and bank holidays) has not yet been run for the current calendar day, the system will automatically generate the raw report, process it into the analysis file, and email it to the configured 'AutoRun Daily' recipients.\par");
            helpMessageBuilder.AppendLine(@"    \pard\fi-360\li720{\pntext\f1\'B7\tab} \b Status:\b0\  The status of the Auto-Run feature (e.g., Enabled, Disabled, Running, Completed for today, Failed) is displayed on the right side of the status bar at the bottom of the window.\par");
            helpMessageBuilder.AppendLine(@"    \pard\fi-360\li720{\pntext\f1\'B7\tab} \b Configuration:\b0\  The hour for the auto-run check can be changed via \b Options -> Set Auto-Run Hour...\b0 . The last successful run date is stored in `appsettings.json` to prevent duplicate runs on the same day.\par");
            helpMessageBuilder.AppendLine(@"\pard\sa200\sl276\slmult1\par");
            helpMessageBuilder.AppendLine(@"\b\fs22 Automated Background Features\b0\fs20\par");
            helpMessageBuilder.AppendLine(@"The application performs several tasks automatically:\par");
            helpMessageBuilder.AppendLine(@"    \pard\fi-360\li720{\pntext\f1\'B7\tab} \b Folder Creation:\b0\  Automatically creates necessary subfolders for storing raw reports and final analysis files, organized by report type (Daily, Weekly, etc.) and date/month/year as appropriate.\par");
            helpMessageBuilder.AppendLine(@"    \pard\fi-360\li720{\pntext\f1\'B7\tab} \b Log Archiving:\b0\  On startup, old application log files are archived to a subfolder to keep the main log directory tidy.\par");
            helpMessageBuilder.AppendLine(@"    \pard\fi-360\li720{\pntext\f1\'B7\tab} \b Report Archiving:\b0\  On startup, older raw report export files/folders and final processed Excel files/folders (beyond a configurable number of days, typically 30) are moved to an 'Archived' subfolder within their respective base directories.\par");
            helpMessageBuilder.AppendLine(@"    \pard\fi-360\li720{\pntext\f1\'B7\tab} \b Power BI Sheet Update (Weekly Reports):\b0\  When processing a \b Weekly\b0 report, data from the 'Analysis' sheet of the template is appended to a sheet named \b 'powerBI'\b0 in the central Excel file. If this sheet doesn't exist, it's created using headers from the 'Analysis' sheet.\par");
            helpMessageBuilder.AppendLine(@"    \pard\fi-360\li720{\pntext\f1\'B7\tab} \b Bank Holiday Adjustments:\b0\  Calculations for the 'previous working day' (used for Daily reports and Auto-Run) automatically consider standard UK bank holidays and any custom bank holidays you've defined via the Options menu.\par");
            helpMessageBuilder.AppendLine(@"\pard\sa200\sl276\slmult1\par");
            helpMessageBuilder.AppendLine(@"\b\fs22 Troubleshooting Tips\b0\fs20\par");
            helpMessageBuilder.AppendLine(@"If you encounter issues, consider the following:\par");
            helpMessageBuilder.AppendLine(@"    \pard\fi-360\li720{\pntext\f1\'B7\tab} \b ""Config Error"" Status:\b0\  This usually means a critical file path (like the Crystal Report file or the Wrapper EXE) is missing or incorrect. Use \b Options -> View Configuration\b0 to check paths. Ensure all listed files/folders exist and are accessible.\par");
            helpMessageBuilder.AppendLine(@"    \pard\fi-360\li720{\pntext\f1\'B7\tab} \b Report Generation Fails:\b0\  \par");
            helpMessageBuilder.AppendLine(@"        \pard\fi-720\li1080{\pntext\f0\'31\tab}i. Ensure the Crystal Report Wrapper service (a background .exe process) is running. The application attempts to start it, but if it fails, report generation won't work. Check Task Manager for `CrystalReportWrapper.exe`.\par");
            helpMessageBuilder.AppendLine(@"        \pard\fi-720\li1080{\pntext\f0\'32\tab}ii. Verify the Crystal Report file path in `appsettings.json` (via Options -> Edit appsettings.json) is correct and the `.rpt` file exists at that location.\par");
            helpMessageBuilder.AppendLine(@"        \pard\fi-720\li1080{\pntext\f0\'33\tab}iii. Check application logs (\b Options -> Open Logs Folder\b0 ) for specific error messages from the wrapper or pipe communication.\par");
            helpMessageBuilder.AppendLine(@"    \pard\fi-360\li720{\pntext\f1\'B7\tab} \b Excel Processing Fails:\b0\  \par");
            helpMessageBuilder.AppendLine(@"        \pard\fi-720\li1080{\pntext\f0\'31\tab}i. Ensure the Excel template files exist in the configured 'TEMPLATE' directory and are not corrupted.\par");
            helpMessageBuilder.AppendLine(@"        \pard\fi-720\li1080{\pntext\f0\'32\tab}ii. Ensure the application has write permissions to the 'Raw Report Export' and 'Final Excel Save Location' directories.\par");
            helpMessageBuilder.AppendLine(@"        \pard\fi-720\li1080{\pntext\f0\'33\tab}iii. For Weekly reports, ensure the central Power BI source Excel file is accessible and not locked by another user/process if data needs to be appended to the 'powerBI' sheet.\par");
            helpMessageBuilder.AppendLine(@"    \pard\fi-360\li720{\pntext\f1\'B7\tab} \b Email Sending Fails:\b0\  \par");
            helpMessageBuilder.AppendLine(@"        \pard\fi-720\li1080{\pntext\f0\'31\tab}i. Check SMTP settings in `appsettings.json` (server, port, credentials if used).\par");
            helpMessageBuilder.AppendLine(@"        \pard\fi-720\li1080{\pntext\f0\'32\tab}ii. Ensure your network connection is active and allows SMTP traffic.\par");
            helpMessageBuilder.AppendLine(@"        \pard\fi-720\li1080{\pntext\f0\'33\tab}iii. Verify email recipients are correctly configured via \b Options -> Manage Email Recipients\b0 .\par");
            helpMessageBuilder.AppendLine(@"    \pard\fi-360\li720{\pntext\f1\'B7\tab} \b Auto-Run Not Working as Expected:\b0\  \par");
            helpMessageBuilder.AppendLine(@"        \pard\fi-720\li1080{\pntext\f0\'31\tab}i. Confirm it's enabled via the button and status bar.\par");
            helpMessageBuilder.AppendLine(@"        \pard\fi-720\li1080{\pntext\f0\'32\tab}ii. Check the configured 'Auto-Run Hour' via \b Options -> Set Auto-Run Hour...\b0 .\par");
            helpMessageBuilder.AppendLine(@"        \pard\fi-720\li1080{\pntext\f0\'33\tab}iii. Review `appsettings.json` for the `AutoReport:LastRunDate`. If it's today's date, it won't run again until tomorrow.\par");
            helpMessageBuilder.AppendLine(@"        \pard\fi-720\li1080{\pntext\f0\'34\tab}iv. Ensure the application has permissions to write to `appsettings.json` to update `LastRunDate`.\par");
            // Updated Excel Slicers/Pivot Tables troubleshooting point
            helpMessageBuilder.AppendLine(@"    \pard\fi-360\li720{\pntext\f1\'B7\tab} \b Excel Slicers/Pivot Tables Not Updating:\b0\  For reports requiring manual refresh (Monthly, Quarterly, etc.), you may need to manually refresh data. To do this, open the Excel file. In the \b 'OrderPivot'\b0\ sheet, right-click each PivotTable and any Slicers present, then select \b 'Refresh'\b0 from the context menu. Repeat this process for the \b 'Estimate Success PivotTable'\b0 sheet (or the similarly named sheet containing the main estimate success pivot table). After refreshing all necessary items, \b SAVE\b0\ and \b CLOSE\b0\ the Excel file.\par");
            helpMessageBuilder.AppendLine(@"    \pard\fi-360\li720{\pntext\f1\'B7\tab} \b Check Logs:\b0\  The most detailed error information is usually in the log files. Access them via \b Options -> Open Logs Folder\b0\ . Look for files corresponding to the date the error occurred.\par");
            helpMessageBuilder.AppendLine(@"    \pard\fi-360\li720{\pntext\f1\'B7\tab} \b Contact IT Support:\b0\  If problems persist, please contact IT support with details of the error message and steps taken.\par");
            helpMessageBuilder.AppendLine(@"\pard\sa200\sl276\slmult1\par");
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

            // If date is changed manually, always switch to "Custom" report type.
            // This will also trigger reportTypeComboBox_SelectedIndexChanged which handles UI updates.
            if (reportTypeComboBox.SelectedIndex != CustomReportIndex)
            {
                Logger.LogDebug("DatePicker_ValueChanged: Manual date change detected. Setting Report Type to Custom.");
                UIManager.SafeControlUpdate(reportTypeComboBox, () =>
                {
                    if (reportTypeComboBox.Items.Count > CustomReportIndex) // Ensure "Custom" item exists
                    {
                        reportTypeComboBox.SelectedIndex = CustomReportIndex;
                    }
                });
            }
        }

        // Other Menu Item Click Handlers (viewConfig, validateConfig, openLogs, editConfig, exit, manageBankHolidays, manageEmailRecipients)
        // remain largely the same, ensure they are complete from previous versions.
        // ... (previous menu item handlers)
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


            string baseLogDir = ConfiguredLogDirectoryBase;
            string userLogDir = string.IsNullOrEmpty(baseLogDir)
                ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "conversionTest", "Logs", Environment.UserName)
                : Path.Combine(baseLogDir, string.Join("_", Environment.UserName.Split(Path.GetInvalidFileNameChars())));
            sb.AppendLine($"7. Application Log Directory (User Specific): '{Path.GetFullPath(userLogDir)}'");
            sb.AppendLine($"   - Exists: {Directory.Exists(Path.GetFullPath(userLogDir))}");
            sb.AppendLine($"8. appsettings.json Path: '{_appSettingsPath}'");
            sb.AppendLine($"   - Exists: {File.Exists(_appSettingsPath)}");
            sb.AppendLine("--------------------------------------------------");
            sb.AppendLine($"Overall Essential Config Valid (for report generation): {configValid}");

            FlexibleMessageBox.Show(this, sb.ToString(), "Configuration Details", MessageBoxButtons.OK, configValid ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
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
            this.Close();
        }

        private void manageCustomBankHolidaysToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Logger.LogInfo("Options -> Manage Custom Bank Holidays clicked.");
            using (var manageForm = new ManageBankHolidaysForm(darkModeToolStripMenuItem.Checked))
            {
                manageForm.ShowDialog(this);
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
                    Logger.LogInfo("ManageEmailRecipientsForm closed.");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error opening or handling ManageEmailRecipientsForm: {ex.Message}", ex);
                FlexibleMessageBox.Show(this, "Could not open the email recipient management window. Please check logs.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }


        private void enable1ClickProcessingToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // The Checked state is toggled automatically by WinForms for CheckOnClick menu items.
            // We then update the UI based on this new state.
            Update1ClickProcessingModeUI();
            // Reset button states to reflect the new mode and their default texts/enabled states.
            ResetUIStateOnError(enable1ClickProcessingToolStripMenuItem.Checked ? "1-Click Process" : "Create Report");
        }

        private async void setAutoRunHourToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Logger.LogInfo("Options -> Set Auto-Run Hour clicked.");
            string currentHourPrompt = _currentAutoRunHour.ToString();
            string? input = Interaction.InputBox("Enter the new hour (0-23) for the daily auto-run check:", "Set Auto-Run Hour", currentHourPrompt);

            if (string.IsNullOrWhiteSpace(input))
            {
                Logger.LogInfo("Set Auto-Run Hour cancelled by user.");
                return;
            }

            if (int.TryParse(input, out int newHour) && newHour >= 0 && newHour <= 23)
            {
                if (newHour != _currentAutoRunHour)
                {
                    bool success = await _autoRunManager.SetAutoRunHourAsync(newHour);
                    if (success)
                    {
                        _currentAutoRunHour = newHour;
                        Logger.LogInfo($"Auto-Run hour successfully updated to {newHour} in configuration and manager.");
                        FlexibleMessageBox.Show(this, $"Auto-Run hour has been set to {newHour}:00.\nThe change will take effect from the next daily check cycle.", "Auto-Run Hour Updated", MessageBoxButtons.OK, MessageBoxIcon.Information);

                        string autoRunButtonText = $"{(dailyCheckTimer.Enabled ? "Disable" : "Enable")} Daily Auto Run @ {_currentAutoRunHour}:00";
                        UIManager.SafeControlUpdate(toggleAutoRunButton, () => toggleAutoRunButton.Text = autoRunButtonText);
                        toolTip1.SetToolTip(toggleAutoRunButton, $"Enable or disable the automated daily report generation. The report runs around {_currentAutoRunHour}:00 for the previous workday.");
                        _uiManager.UpdateAutoRunUI(dailyCheckTimer.Enabled, false, darkModeToolStripMenuItem.Checked, $"Auto Run: {(dailyCheckTimer.Enabled ? $"Enabled (Next check ~{_currentAutoRunHour}:00)" : "Disabled")}");
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

        #endregion

        #region Helper Methods (UI Update, Validation, Config, Email)

        private void Update1ClickProcessingModeUI()
        {
            bool oneClickEnabled = enable1ClickProcessingToolStripMenuItem.Checked;
            Logger.LogDebug($"Update1ClickProcessingModeUI called. 1-Click Mode Checked: {oneClickEnabled}");

            if (oneClickProcessButton == null)
            {
                Logger.LogError("oneClickProcessButton is NULL in Update1ClickProcessingModeUI. This should not happen if InitializeComponent was successful.");
                return;
            }
            Logger.LogDebug($"oneClickProcessButton IsDisposed: {oneClickProcessButton.IsDisposed}, HandleCreated: {oneClickProcessButton.IsHandleCreated}, Parent: {(oneClickProcessButton.Parent?.Name ?? "null")}, In Form.Controls: {this.Controls.Contains(oneClickProcessButton)}");
            Logger.LogDebug($"createReportButton Parent: {(createReportButton.Parent?.Name ?? "null")}, In Form.Controls: {this.Controls.Contains(createReportButton)}");
            Logger.LogDebug($"processEmailButton Parent: {(processEmailButton.Parent?.Name ?? "null")}, In Form.Controls: {this.Controls.Contains(processEmailButton)}");


            UIManager.SafeControlUpdate(oneClickProcessButton, () =>
            {
                oneClickProcessButton.Visible = oneClickEnabled;
                Logger.LogDebug($"ATTEMPTED oneClickProcessButton.Visible set to: {oneClickEnabled}. ACTUAL oneClickProcessButton.Visible: {oneClickProcessButton.Visible}");
                if (oneClickEnabled && oneClickProcessButton.Visible) oneClickProcessButton.BringToFront();
            });
            UIManager.SafeControlUpdate(createReportButton, () =>
            {
                createReportButton.Visible = !oneClickEnabled;
                Logger.LogDebug($"ATTEMPTED createReportButton.Visible set to: {!oneClickEnabled}. ACTUAL createReportButton.Visible: {createReportButton.Visible}");

            });
            UIManager.SafeControlUpdate(processEmailButton, () =>
            {
                processEmailButton.Visible = !oneClickEnabled;
                Logger.LogDebug($"ATTEMPTED processEmailButton.Visible set to: {!oneClickEnabled}. ACTUAL processEmailButton.Visible: {processEmailButton.Visible}");
            });

            if (oneClickEnabled)
            {
                Logger.LogInfo("1-Click Processing Mode Enabled (UI updated).");
            }
            else
            {
                Logger.LogInfo("1-Click Processing Mode Disabled (UI updated to 2-button mode).");
            }
        }

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
                    if (!string.IsNullOrEmpty(previousFY)) { financialYearComboBox.Items.Add(previousFY); }
                }
                else
                {
                    Logger.LogWarning("Could not determine current financial year for dropdown.");
                    financialYearComboBox.Items.Add("FY Unknown");
                }

                if (!string.IsNullOrEmpty(previouslySelected) && financialYearComboBox.Items.Contains(previouslySelected))
                {
                    financialYearComboBox.SelectedItem = previouslySelected;
                }
                else if (financialYearComboBox.Items.Count > 0)
                {
                    financialYearComboBox.SelectedIndex = 0;
                }
            });
            Logger.LogTrace("Exiting PopulateFinancialYearDropdown");
        }

        private bool ValidateInputDates()
        {
            if (startDatePicker.Value.Date > endDatePicker.Value.Date)
            {
                FlexibleMessageBox.Show(this, "The 'From' date cannot be after the 'To' date.", "Date Range Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
            return true;
        }

        private bool ValidateFinancialYearSelection()
        {
            if (!financialYearComboBox.Visible || financialYearComboBox.SelectedItem == null)
            {
                return true; // Not applicable or not selected, so valid in this context
            }

            string selectedFinYear = financialYearComboBox.SelectedItem.ToString()!;
            if (!_excelProcessor.IsFinancialYearValid(selectedFinYear, startDatePicker.Value, endDatePicker.Value))
            {
                DialogResult dr = FlexibleMessageBox.Show(this,
                    $"The selected date range ({startDatePicker.Value:d} - {endDatePicker.Value:d}) " +
                    $"does not fall entirely within the selected Financial Year ({selectedFinYear}).\n\n" +
                    "Do you want to continue anyway?",
                    "Financial Year Mismatch Warning", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                return dr == DialogResult.Yes;
            }
            return true;
        }

        private static string DetermineAppSettingsBasePath() => @"\\harlow.local\DFS\IT Department\Applications\Development 2025\QuoteConversionReportAutomation\conversionTest";

        private bool CheckConfigValidity()
        {
            string crPath = CrystalReportLocation;
            string wrapPath = _configuration["settings:WrapperExePath"] ?? "";
            return !string.IsNullOrEmpty(crPath) && File.Exists(crPath) &&
                   !string.IsNullOrEmpty(wrapPath) && File.Exists(Path.GetFullPath(wrapPath));
        }

        private bool IsDailySelected() => reportTypeComboBox.SelectedIndex == DailyReportIndex;

        private void ResetUIStateOnError(string mainButtonText)
        {
            bool isOneClickMode = enable1ClickProcessingToolStripMenuItem.Checked;
            bool configValid = CheckConfigValidity();

            // This block directly updates the Form's controls for text and enabled state
            // based on the current 1-click mode.
            UIManager.SafeControlUpdate(this, () => { // 'this' refers to Form1
                if (isOneClickMode)
                {
                    oneClickProcessButton.Text = configValid ? mainButtonText : "Config Error";
                    oneClickProcessButton.Enabled = configValid;
                    // Ensure other buttons are disabled (though they should be invisible)
                    createReportButton.Enabled = false;
                    processEmailButton.Enabled = false;
                }
                else // Standard 2-button mode
                {
                    createReportButton.Text = configValid ? mainButtonText : "Config Error";
                    createReportButton.Enabled = configValid;
                    processEmailButton.Text = "Process and Email"; // Reset text
                    // Enable process button only if config is valid AND a raw report has been generated
                    processEmailButton.Enabled = configValid && !string.IsNullOrEmpty(_generatedReportPath) && File.Exists(_generatedReportPath);
                    // Ensure 1-click button is disabled (though it should be invisible)
                    oneClickProcessButton.Enabled = false;
                }
            });

            // Call UIManager's generic reset for other UI elements (status bar, view buttons etc.)
            _uiManager.ResetUIOnError(
                mainButtonText, // This text is a fallback for UIManager if it needs a generic button text
                configValid,
                !string.IsNullOrEmpty(_generatedReportPath) && File.Exists(_generatedReportPath),
                !string.IsNullOrEmpty(_generatedAnalysisFilePath) && File.Exists(_generatedAnalysisFilePath),
                IsDailySelected(),
                dailyCheckTimer.Enabled,
                darkModeToolStripMenuItem.Checked,
                // Determine if auto-run status is final for today
                (autoRunStatusLabel.Text?.Contains("Completed") ?? false) || (autoRunStatusLabel.Text?.Contains("Done for") ?? false) || (autoRunStatusLabel.Text?.Contains("FAILED") ?? false),
                autoRunStatusLabel.Text ?? ""
            );

            // CRITICAL: After all other state resets, re-apply the visibility rules for 1-click mode.
            // This ensures the correct buttons are visible based on the menu item's checked state.
            Update1ClickProcessingModeUI();
        }

        private async Task SendCompletionEmailAsync(string attachmentPath, IProgress<string> progress, CancellationToken cancellationToken)
        {
            Logger.LogTrace("Entering SendCompletionEmailAsync");
            _uiManager.UpdateProgress("Preparing email...");
            if (!File.Exists(attachmentPath))
            {
                Logger.LogError($"Attachment file not found: {attachmentPath}");
                throw new FileNotFoundException("Attachment file for email not found.", attachmentPath);
            }

            try
            {
                var (to, cc) = GetEmailRecipients();
                if (to.Count == 0 && cc.Count == 0 && !IsDebug) // In Debug, allow sending even if no "real" recipients
                {
                    Logger.LogWarning("No email recipients determined (and not in debug mode). Skipping email send.");
                    progress.Report("No recipients configured. Email not sent.");
                    return;
                }
                if (to.Count == 0 && cc.Count == 0 && IsDebug)
                {
                    Logger.LogInfo("DEBUG MODE: No explicit recipients, but proceeding with email send attempt (likely to configured debug addresses).");
                }


                var (subj, body) = GetEmailSubjectAndBody(startDatePicker.Value, endDatePicker.Value);

                progress.Report("Sending email...");
                bool emailSent = await _emailUtility.SendEmailAsync(to, cc, subj, body, attachmentPath, progress, cancellationToken);

                if (!emailSent && !cancellationToken.IsCancellationRequested)
                {
                    // Avoid throwing an exception that halts the flow if email is just one part of a larger process.
                    // Log the error and perhaps update UI, but let the calling method decide on overall success.
                    Logger.LogError("Email sending failed. Check logs for details from EmailUtility. Continuing operation if possible.");
                    progress.Report("Email sending failed. Check logs.");
                    // Consider how to signal this failure to the user without stopping everything if it's non-critical.
                    // For now, we just log and report progress. The method doesn't throw here.
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
                    // Do not throw here if cancellation is handled by the caller.
                }
            }
            catch (OperationCanceledException) // Should be caught by the caller ideally
            {
                Logger.LogWarning("Email sending operation was cancelled.");
                progress.Report("Email sending cancelled.");
                throw;
            }
            catch (Exception ex) // Catch other unexpected exceptions during email prep/send
            {
                Logger.LogError($"Error sending email: {ex.Message}", ex);
                FlexibleMessageBox.Show(this, $"Failed to send email: {ex.Message}", "Email Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                // Depending on severity, you might re-throw or handle.
                // If SendCompletionEmailAsync is part of a larger flow, re-throwing might be appropriate.
                throw;
            }
        }

        private (List<string> To, List<string> Cc) GetEmailRecipients()
        {
            Logger.LogTrace("Form1: Entering GetEmailRecipients, deferring to EmailRecipientManager...");
            bool isFemiOnly = sendToFemiOnlyCheckBox.Checked;
            int currentReportType = reportTypeComboBox.SelectedIndex; // Assumes this is correctly set

            var recipients = _emailRecipientManager.GetRecipients(currentReportType, isFemiOnly, IsDebug);

            Logger.LogDebug($"Form1: Recipients from Manager - To: {string.Join("; ", recipients.To)}, CC: {string.Join("; ", recipients.Cc)}");
            Logger.LogTrace("Form1: Exiting GetEmailRecipients.");
            return recipients;
        }

        private (string Subject, string Body) GetEmailSubjectAndBody(DateTime reportStartDate, DateTime reportEndDate)
        {
            string typeName = "Estimate Success Rate";
            // Ensure reportTypeComboBox.Text is accessed on the UI thread if called from a background thread.
            // However, this method is typically called from UI event handlers or tasks that originated there.
            string reportTypeString = "";
            UIManager.SafeControlUpdate(reportTypeComboBox, () => reportTypeString = reportTypeComboBox.Text);


            int type = reportTypeComboBox.SelectedIndex; // This should be safe if called from UI context
            bool femiOnlyChecked = sendToFemiOnlyCheckBox.Checked; // Also UI context

            string greeting;
            if (IsDebug)
            {
                greeting = "Hi Debug,";
            }
            else if (type == DailyReportIndex)
            {
                greeting = _configuration["settings:ProductionEmails:AutoRunDailyGreeting"] ?? "Hi Paul,";
            }
            else if (femiOnlyChecked)
            {
                greeting = "Hi Femi,";
            }
            else
            {
                greeting = "Hi All,";
            }

            string rangeInfo;
            string subjectPrefix = $"{reportTypeString} {typeName}";

            switch (type)
            {
                case DailyReportIndex:
                    rangeInfo = $"for {reportEndDate:dd MMM yy}";
                    break;
                case WeeklyReportIndex:
                    rangeInfo = $"for period {reportStartDate:dd MMM yy} to {reportEndDate:dd MMM yy}";
                    break;
                case MonthlyReportIndex:
                    rangeInfo = $"for {reportStartDate:MMMM yyyy}";
                    break;
                case QuarterlyReportIndex:
                    rangeInfo = $"for {ReportHelper.GetQuarterString(reportStartDate)} {reportStartDate.Year}";
                    break;
                case AnnualReportIndex:
                    rangeInfo = $"for {reportStartDate.Year}";
                    break;
                case CustomReportIndex: // Custom report
                    rangeInfo = $"for period {reportStartDate:dd MMM yy} to {reportEndDate:dd MMM yy}";
                    break;
                default: // Fallback, should ideally not be reached if ComboBox is well-defined
                    rangeInfo = $"for period {reportStartDate:dd MMM yy} to {reportEndDate:dd MMM yy}";
                    subjectPrefix = $"Report {typeName}"; // Generic prefix if type is unknown
                    break;
            }

            string subject = (type != CustomReportIndex && type != -1 ? "AUTOMATED: " : "") + // Add AUTOMATED unless custom or unselected
                             $"{subjectPrefix} Report ({reportEndDate:yyyy-MM-dd})";

            string body = $"{greeting}\n\n" +
                          $"Please find attached the {subjectPrefix.ToLower()} report {rangeInfo}.\n\n" +
                          "This report includes quotes data for review.\n\n" +
                          "Thank you,\n" +
                          "Automation Service";

            return (subject, body);
        }

        private async Task<bool> HandleManualExcelRefreshAsync(string filePath, CancellationToken token)
        {
            _uiManager.UpdateProgress("Checking for running Excel instances...");
            if (await Task.Run(() => Process.GetProcessesByName("EXCEL").Length > 0, token))
            {
                var dr = FlexibleMessageBox.Show(this, "Other Excel instances are running. Close them before proceeding with the manual refresh?",
                    "Close Other Excel Instances?", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning);
                if (dr == DialogResult.Cancel) { Logger.LogInfo("User cancelled manual refresh due to other Excel instances."); return false; }
                if (dr == DialogResult.Yes)
                {
                    _uiManager.UpdateProgress("Attempting to close other Excel instances...");
                    await Task.Run(() => ReportHelper.CloseProcessesByName("EXCEL"), token);
                    await Task.Delay(1500, token);
                }
            }

            FlexibleMessageBox.Show(this, "The report will now open in Excel.\n\n" +
                "*** IMPORTANT ***\n" +
                "1. Enable Editing if prompted.\n" +
                "2. Refresh All Pivot Tables and Data Connections (Data tab -> Refresh All).\n" +
                "3. SAVE the file.\n" +
                "4. CLOSE Excel.\n\n" +
                "The application will wait for you to close Excel before continuing.",
                "Manual Refresh Required", MessageBoxButtons.OK, MessageBoxIcon.Information);

            token.ThrowIfCancellationRequested();
            _uiManager.UpdateProgress("Opening Excel for manual refresh...");
            Process? excelProc = null;
            try
            {
                excelProc = await Task.Run(() => Process.Start(new ProcessStartInfo(filePath) { UseShellExecute = true }), token);
                if (excelProc == null)
                {
                    throw new InvalidOperationException("Failed to start Excel process. Ensure Excel is installed and .xlsx files are associated.");
                }
                _uiManager.UpdateProgress("Excel opened. Waiting for you to Refresh All, Save, and Close Excel...");

                await excelProc.WaitForExitAsync(token);

                _uiManager.UpdateStatusMain("Excel closed by user.");
                DialogResult sendResult = FlexibleMessageBox.Show(this, "Excel has been closed.\n\nProceed with sending the email (if not skipped)?",
                    "Confirm Email Send", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                return (sendResult == DialogResult.Yes);
            }
            catch (OperationCanceledException)
            {
                Logger.LogWarning("Manual Excel refresh process was cancelled by timeout or user action.");
                if (excelProc != null && !excelProc.HasExited)
                {
                    try { excelProc.Kill(true); }
                    catch (Exception killEx) { Logger.LogWarning($"Could not kill Excel process during cancellation: {killEx.Message}"); }
                }
                return false;
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error during manual Excel handling: {ex.Message}", ex);
                FlexibleMessageBox.Show(this, $"An error occurred managing the Excel refresh step:\n\n{ex.Message}",
                    "Excel Interaction Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
