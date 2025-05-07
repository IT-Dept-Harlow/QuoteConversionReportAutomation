// C# 10+ Features
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
    using QuoteConversionReportAutomation;
    using JR.Utils.GUI.Forms;

    /// <summary>
    /// Represents the main form of the Quote Conversion Report Automation application.
    /// Orchestrates report generation and processing by coordinating with manager classes.
    /// Handles UI events and delegates UI updates to UIManager.
    /// Manages the Auto-Run timer, delegating the check logic to AutoRunManager.
    /// Includes handling for a "Custom" report type triggered by manual date changes.
    /// Includes background archiving of old report files on startup.
    /// Daily report date calculation now considers bank holidays.
    /// Added new options menu items: View Configuration, Validate Configuration, Open Logs, Edit Config, Manage Custom Bank Holidays, Exit.
    /// ProgressBar functionality has been removed.
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
        private const string AppVersion = "1.7.1"; // Reflects latest changes

        // --- State Variables (Remaining in Form1) ---
        private string _generatedReportPath = string.Empty;
        private string _generatedAnalysisFilePath = string.Empty;
        private bool _programmaticallyChangingDates = false;

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
        public string ExcelTemplateBaseDir => Path.Combine(UserProfilePath, _configuration["settings:TemplateBaseDir"]?.TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) ?? @"Harlow Printing\IT Projects - Documents\Dashboard Datasets\Raw_data\Quotes conversion\TEMPLATE");
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
        /// <summary>
        /// Initializes a new instance of the <see cref="Form1"/> class.
        /// Instantiates manager classes and sets up dependencies.
        /// </summary>
        /// <param name="configuration">The application configuration provider.</param>
        /// <exception cref="ArgumentNullException">Thrown if configuration is null.</exception>
        public Form1(IConfiguration configuration)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            Logger.LogTrace("Entering Form1 Constructor");
            try
            {
                InitializeComponent();
                Logger.LogDebug("InitializeComponent completed.");

                // --- Instantiate Dependencies and Managers ---
                _emailUtility = new EmailUtility(_configuration);
                _excelProcessor = new ExcelCopyData();
                _uiManager = new UIManager(
                    this, menuStrip1, mainStatusStrip, statusLabel, autoRunStatusLabel,
                    darkModeToolStripMenuItem, createReportButton, processEmailButton,
                    generateAndSendButton,
                    toggleAutoRunButton, viewReportButton, viewAnalysisButton,
                    reportTypeComboBox, startDatePicker, endDatePicker,
                    financialYearComboBox, financialYearLabel, sendToFemiOnlyCheckBox,
                    emailRecipientLabel, toolTip1
                );
                string wrapperExePath = Path.GetFullPath(_configuration["settings:WrapperExePath"] ?? "CrystalReportWrapper.exe");
                _processManager = new ReportProcessManager(wrapperExePath);
                _pipeCommunicator = new NamedPipeCommunicator();
                _autoRunManager = new AutoRunManager(
                    _configuration, _emailUtility, _processManager, _pipeCommunicator,
                    _uiManager, _excelProcessor, _appSettingsPath
                );

                // --- Wire up event handlers ---
                this.startDatePicker.ValueChanged += new System.EventHandler(this.DatePicker_ValueChanged);
                this.endDatePicker.ValueChanged += new System.EventHandler(this.DatePicker_ValueChanged);
                // Event handlers for menu items like exitToolStripMenuItem, manageCustomBankHolidaysToolStripMenuItem etc.
                // are typically wired up in Form1.Designer.cs by the designer when you double-click them.
                // If not, they should be added here or in the designer.
                // Example (ensure these match your Designer.cs or add them here):
                // this.exitToolStripMenuItem.Click += new System.EventHandler(this.exitToolStripMenuItem_Click);
                // this.manageCustomBankHolidaysToolStripMenuItem.Click += new System.EventHandler(this.manageCustomBankHolidaysToolStripMenuItem_Click);
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
        /// <summary>
        /// Handles the Load event of the form. Initializes UI state via UIManager,
        /// validates configuration paths, ensures the wrapper service is running,
        /// sets up the initial form state, and triggers background archiving.
        /// </summary>
        private async void Form1_Load(object sender, EventArgs e)
        {
            Logger.LogTrace("Entering Form1_Load");
            _uiManager.UpdateStatusMain("Loading application...");
            try
            {
                // Initialize BankHolidayHelper - crucial for loading custom holidays
                BankHolidayHelper.Initialize();
                Logger.LogInfo("BankHolidayHelper initialized.");

                Logger.LogInfo("Form Loading...");

                // --- Configuration Validation ---
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

                // --- UI Initialization ---
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
                _uiManager.UpdateAutoRunUI(dailyCheckTimer.Enabled, false, useDarkMode);
                reportTypeComboBox_SelectedIndexChanged(reportTypeComboBox, EventArgs.Empty);
                _uiManager.ResetButtonStatesAfterTypeChange(configValid);

                if (!configValid) _uiManager.UpdateStatusMain("Config Error: Check Options menu.");

                // --- Ensure Wrapper Service is Running ---
                _uiManager.UpdateStatusMain("Checking report service...");
                IProgress<string> loadProgress = new Progress<string>(status => _uiManager.UpdateProgress(status));
                bool wrapperOk = await _processManager.EnsureWrapperIsRunningAsync(loadProgress);

                if (!wrapperOk && configValid)
                {
                    _uiManager.UpdateStatusMain("Report service failed to start.");
                    _uiManager.ResetUIOnError("Config Error", false, false, false, IsDailySelected(), dailyCheckTimer.Enabled, useDarkMode, false, string.Empty);
                }

                // --- Trigger Background Archiving ---
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
                MessageBox.Show($"A critical error occurred loading the application:\n\n{ex.Message}\n\nThe application may not function correctly.", "Application Load Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                _uiManager.UpdateStatusMain("Error during load.");
            }
            Logger.LogTrace("Exiting Form1_Load");
        }

        /// <summary>
        /// Handles the FormClosing event. Ensures the background wrapper process is terminated
        /// and stops the auto-run timer.
        /// </summary>
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            Logger.LogInfo("Form closing. Stopping timer and terminating wrapper process.");
            dailyCheckTimer.Stop();
            _processManager.TerminateWrapperProcess();
        }
        #endregion

        #region Event Handlers
        /// <summary>
        /// Handles the Click event for Button 1 (Create Report).
        /// Validates input, ensures wrapper service, sends request via NamedPipeCommunicator,
        /// and updates UI via UIManager.
        /// </summary>
        private async void createReportButton_Click(object sender, EventArgs e)
        {
            _uiManager.SetActionButtonsEnabled(false);
            _uiManager.SetOtherControlsEnabled(false, financialYearComboBox.Visible);
            _uiManager.UpdateProgress("Validating request...");
            createReportButton.Text = "Requesting...";
            Logger.LogDebug("Create Report Button Clicked: Requesting Crystal Report generation.");

            using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(6));
            IProgress<string> progress = new Progress<string>(status => _uiManager.UpdateProgress(status));

            try
            {
                if (!ValidateInputDates()) { ResetUIStateOnError("Date Error"); return; }
                if (!ValidateFinancialYearSelection()) { ResetUIStateOnError("FY Mismatch"); return; }

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
                    _uiManager.SetActionButtonsEnabled(false);
                    _uiManager.SetOtherControlsEnabled(true, financialYearComboBox.Visible);
                    createReportButton.Text = "Report Created";
                    processEmailButton.Enabled = true;
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
                ResetUIStateOnError("Cancelled");
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error during Create Report operation: {ex}");
                FlexibleMessageBox.Show($"An error occurred while requesting the report:\n\n{ex.Message}", "Report Request Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                ResetUIStateOnError("Error");
            }
        }

        /// <summary>
        /// Handles the Click event for Button 2 (Process & Email).
        /// Checks existing files, processes raw report using ExcelCopyData,
        /// handles manual refresh via ReportHelper, sends email via EmailUtility, updates UI via UIManager.
        /// </summary>
        private async void processEmailButton_Click(object sender, EventArgs e)
        {
            Logger.LogTrace("Entering processEmailButton_Click");
            _uiManager.SetActionButtonsEnabled(false);
            _uiManager.SetOtherControlsEnabled(false, financialYearComboBox.Visible);
            processEmailButton.Text = "Processing...";
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
                if (!ValidateInputDates()) { ResetUIStateOnError("Date Error"); return; }

                string? expectedFinalPath = _excelProcessor.GetExpectedFinalFilePath(reportType, baseSaveLocation, reportDate);
                if (expectedFinalPath != null && File.Exists(expectedFinalPath))
                {
                    generalProgress.Report("Found existing file. Prompting user...");
                    DialogResult dr = MessageBox.Show(
                        $"The report file '{Path.GetFileName(expectedFinalPath)}' already exists for this period.\n\n" +
                        "Do you want to skip processing and send this existing file?",
                        "File Already Exists", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                    if (dr == DialogResult.Yes)
                    {
                        Logger.LogInfo("User chose to send existing file.");
                        finalFilePath = expectedFinalPath;
                        _generatedAnalysisFilePath = finalFilePath;
                        _uiManager.ShowViewAnalysisButton(true, finalFilePath);

                        bool proceedToEmail = true;
                        if (requiresManualRefresh)
                        {
                            generalProgress.Report("Waiting for manual Excel refresh...");
                            proceedToEmail = await HandleManualExcelRefreshAsync(finalFilePath, token);
                            if (!proceedToEmail) { _uiManager.UpdateStatusMain("Manual refresh/confirmation cancelled."); ResetUIStateOnError("Cancelled"); return; }
                            generalProgress.Report("Manual refresh confirmed.");
                        }
                        await SendCompletionEmailAsync(finalFilePath, generalProgress, token);
                        _uiManager.SetUICompleted(CheckConfigValidity(), IsDailySelected(), dailyCheckTimer.Enabled, darkModeToolStripMenuItem.Checked, false, autoRunStatusLabel.Text ?? "");
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
                            MessageBox.Show($"Could not delete the existing report file:\n{expectedFinalPath}\n\nPlease ensure the file is not open and try again.\n\nError: {delEx.Message}",
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
                    if (!proceedToEmailAfterGenerate) { _uiManager.UpdateStatusMain("Manual refresh/confirmation cancelled."); ResetUIStateOnError("Cancelled"); return; }
                    generalProgress.Report("Manual refresh confirmed.");
                }

                if (proceedToEmailAfterGenerate)
                {
                    await SendCompletionEmailAsync(finalFilePath, generalProgress, token);
                    _uiManager.SetUICompleted(CheckConfigValidity(), IsDailySelected(), dailyCheckTimer.Enabled, darkModeToolStripMenuItem.Checked, false, autoRunStatusLabel.Text ?? "");
                }
                else { ResetUIStateOnError("Create Report"); }
            }
            catch (OperationCanceledException)
            {
                Logger.LogWarning("Excel processing or subsequent step cancelled.");
                ResetUIStateOnError("Cancelled");
            }
            catch (FileNotFoundException fnfEx)
            {
                Logger.LogError($"File not found during Process & Email operation: {fnfEx}", fnfEx);
                MessageBox.Show(fnfEx.Message, "File Not Found Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                ResetUIStateOnError("File Error");
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error during Process & Email operation: {ex}", ex);
                MessageBox.Show($"An unexpected error occurred during processing:\n\n{ex.Message}", "Processing Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                ResetUIStateOnError("Error");
            }
            Logger.LogTrace("Exiting processEmailButton_Click");
        }

        /// <summary>
        /// Handles the Click event for the "Generate & Send" button.
        /// This is a placeholder and needs to be implemented if the button is active.
        /// </summary>
        private async void generateAndSendButton_Click(object sender, EventArgs e)
        {
            Logger.LogInfo("Generate & Send button clicked. Functionality not yet fully implemented.");
            _uiManager.UpdateStatusMain("Generate & Send: Not implemented.");
            // Placeholder: You would chain createReportButton_Click and then processEmailButton_Click logic here.
            // Ensure proper error handling and UI updates throughout the combined process.
            await Task.Delay(2000); // Simulate work
            _uiManager.UpdateStatusMain("Ready.");
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
        /// Uses DateTime.Today to calculate default dates.
        /// Recalculates Financial Year when needed.
        /// Daily report date calculation now considers bank holidays via ReportHelper.GetPreviousWorkday.
        /// </summary>
        private void reportTypeComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            Logger.LogTrace("Entering reportTypeComboBox_SelectedIndexChanged");
            if (sender is not ComboBox comboBox || comboBox.SelectedItem == null) return;
            int selectedIndex = comboBox.SelectedIndex;
            if (selectedIndex == CustomReportIndex)
            {
                UIManager.SafeControlUpdate(sendToFemiOnlyCheckBox, () => { sendToFemiOnlyCheckBox.Visible = true; });
                UIManager.SafeControlUpdate(emailRecipientLabel, () => { emailRecipientLabel.Visible = false; });
                _uiManager.ResetButtonStatesAfterTypeChange(CheckConfigValidity());
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
                    _ => (todayValue, todayValue, true)
                };
                UIManager.SafeControlUpdate(startDatePicker, () => { startDatePicker.Value = rangeInfo.DateFrom; });
                UIManager.SafeControlUpdate(endDatePicker, () => { endDatePicker.Value = rangeInfo.DateTo; });
                UIManager.SafeControlUpdate(financialYearLabel, () => { financialYearLabel.Visible = rangeInfo.ShowFinYear; });
                UIManager.SafeControlUpdate(financialYearComboBox, () => { financialYearComboBox.Visible = rangeInfo.ShowFinYear; financialYearComboBox.Enabled = rangeInfo.ShowFinYear; if (rangeInfo.ShowFinYear) PopulateFinancialYearDropdown(); });
                bool isDaily = IsDailySelected();
                UIManager.SafeControlUpdate(sendToFemiOnlyCheckBox, () => { sendToFemiOnlyCheckBox.Visible = !isDaily; });
                UIManager.SafeControlUpdate(emailRecipientLabel, () => { emailRecipientLabel.Visible = isDaily; if (isDaily) emailRecipientLabel.Text = "Emailing Daily report to Paul"; });
                _uiManager.ResetButtonStatesAfterTypeChange(CheckConfigValidity());
            }
            finally { _programmaticallyChangingDates = false; }
            Logger.LogTrace("Exiting reportTypeComboBox_SelectedIndexChanged");
        }

        /// <summary>
        /// Handles the Click event for the Auto Run toggle button.
        /// Enables or disables the daily check timer and updates UI via UIManager.
        /// </summary>
        private void toggleAutoRunButton_Click(object sender, EventArgs e)
        {
            dailyCheckTimer.Enabled = !dailyCheckTimer.Enabled;
            _uiManager.UpdateAutoRunUI(dailyCheckTimer.Enabled, false, darkModeToolStripMenuItem.Checked);
        }

        /// <summary>
        /// Handles the Tick event for the daily check timer. Delegates the core logic to AutoRunManager.
        /// Manages stopping/starting the timer around the async check.
        /// </summary>
        private async void dailyCheckTimer_Tick(object sender, EventArgs e)
        {
            if (!dailyCheckTimer.Enabled) return;
            bool originallyEnabled = dailyCheckTimer.Enabled;
            dailyCheckTimer.Stop();
            try { await _autoRunManager.PerformDailyCheckAsync(originallyEnabled); }
            catch (Exception ex)
            {
                Logger.LogCritical($"CRITICAL ERROR during AutoRunManager.PerformDailyCheckAsync: {ex}", ex);
                _uiManager.UpdateStatusMain("Critical AutoRun Error!");
                originallyEnabled = false;
            }
            finally
            {
                if (originallyEnabled) dailyCheckTimer.Start();
                ResetUIStateOnError("Create Report");
            }
        }

        /// <summary>
        /// Handles the Click event for the Dark Mode menu item. Toggles theme via UIManager.
        /// </summary>
        private void darkModeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _uiManager.ApplyTheme(darkModeToolStripMenuItem.Checked);
            _uiManager.UpdateAutoRunUI(dailyCheckTimer.Enabled, false, darkModeToolStripMenuItem.Checked);
        }

        /// <summary>
        /// Handles the Click event for the Help menu item. Displays help information
        /// using the dedicated HelpForm.
        /// </summary>
        private void helpToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string helpTitle = $"Help - Quote Conversion v{AppVersion}";
            var helpMessageBuilder = new System.Text.StringBuilder();

            // --- Start RTF Content ---
            helpMessageBuilder.AppendLine(@"{\rtf1\ansi\ansicpg1252\deff0\nouicompat{\fonttbl{\f0\fnil\fcharset0 Segoe UI;}}");
            helpMessageBuilder.AppendLine(@"{\colortbl ;\red0\green0\blue0;}");
            helpMessageBuilder.AppendLine(@"\pard\sa200\sl276\slmult1\b\f0\fs24 Quote Conversion Automation Tool\b0\fs20\par");
            helpMessageBuilder.AppendLine(@"\par");
            helpMessageBuilder.AppendLine(@"This tool automates the process of generating and processing Estimate Success Rate reports.\par");
            helpMessageBuilder.AppendLine(@"\par");
            helpMessageBuilder.AppendLine(@"\b How to Use: \b0\par");
            helpMessageBuilder.AppendLine(@"\par");
            helpMessageBuilder.AppendLine(@"1.  \b Select Report Type: \b0 Choose Daily, Weekly, Monthly, Quarterly, Annual, or Custom from the dropdown. The 'From' and 'To' dates, along with the Financial Year (if applicable), will adjust automatically based on the {\i current date} when you select a standard report type.\par");
            helpMessageBuilder.AppendLine(@"    * \b Daily: \b0 Dates will be set to the {\i previous working day}. This calculation automatically skips weekends (Saturdays/Sundays) and official bank holidays for England and Wales. It correctly handles bank holidays that fall on a weekend (substituting them to the following Monday/Tuesday) and moving holidays like Easter. {\i (Note: Custom/one-off bank holidays like Jubilees require code updates in BankHolidayHelper.cs).}\par");
            helpMessageBuilder.AppendLine(@"    * \b Weekly/Daily: \b0 Ensure the correct Financial Year is selected if visible (it defaults based on the {\i current date}).\par");
            helpMessageBuilder.AppendLine(@"    * \b Custom: \b0 Select this type, or simply change the 'From' or 'To' dates manually.\par");
            helpMessageBuilder.AppendLine(@"\par");
            helpMessageBuilder.AppendLine(@"2.  \b Adjust Dates (Optional/Custom report): \b0 You can manually change the 'From' and 'To' dates. Doing so will automatically select the 'Custom' report type.\par");
            helpMessageBuilder.AppendLine(@"\par");
            helpMessageBuilder.AppendLine(@"3.  \b Create Raw Report: \b0 Click the \""Create Report\"" button. This contacts a background service to generate the raw data export from Crystal Reports. Wait for the status to show \""Report Created\"". The filename will reflect the 'To' date.\par");
            helpMessageBuilder.AppendLine(@"\par");
            helpMessageBuilder.AppendLine(@"4.  \b Process & Email: \b0 Once the raw report is created, click the \""Process and Email\"" button. This performs data processing (including appending to the central weekly file for Weekly reports) and emails the final report.\par");
            helpMessageBuilder.AppendLine(@"    * (For Monthly/Quarterly/Annual/Custom) You will be prompted to open the file in Excel to Refresh All pivot tables, Save, and Close before the email is sent.\par");
            helpMessageBuilder.AppendLine(@"\par");
            helpMessageBuilder.AppendLine(@"5.  \b View Files (Optional): \b0 Use the \""View Report\"" and \""View Analysis\"" buttons after the corresponding steps are complete to open the generated files.\par");
            helpMessageBuilder.AppendLine(@"\par");
            helpMessageBuilder.AppendLine(@"6.  \b Options Menu: \b0\par");
            helpMessageBuilder.AppendLine(@"    * \b Dark Mode: \b0 Toggle the visual theme.\par");
            helpMessageBuilder.AppendLine(@"    * \b View Configuration: \b0 Show detailed status of configuration settings.\par");
            helpMessageBuilder.AppendLine(@"    * \b Validate Configuration: \b0 Quickly validate essential configuration and update status bar.\par");
            helpMessageBuilder.AppendLine(@"    * \b Manage Custom Bank Holidays: \b0 Add or remove custom one-off or recurring bank holidays.\par");
            helpMessageBuilder.AppendLine(@"    * \b Open Logs Folder: \b0 Open the folder containing application logs.\par");
            helpMessageBuilder.AppendLine(@"    * \b Edit appsettings.json: \b0 Open the main configuration file for manual editing (use with caution!).\par");
            helpMessageBuilder.AppendLine(@"    * \b Exit: \b0 Close the application.\par");
            helpMessageBuilder.AppendLine(@"\par");
            helpMessageBuilder.AppendLine(@"7.  \b Auto Run Button: \b0 Enable/Disable the automated daily report generation. When enabled, the application checks around 8 AM each day. If the report for the {\i previous working day} (considering weekends and England/Wales bank holidays) hasn't run yet for the current date, it will generate and email it automatically. The status is shown on the right of the status bar.\par");
            helpMessageBuilder.AppendLine(@"\par");
            helpMessageBuilder.AppendLine(@"\b Automated Features: \b0\par");
            helpMessageBuilder.AppendLine(@"* \b Folder Creation: \b0 The application automatically creates the necessary folder structure within the configured base directories (e.g., `ExcelFinalSaveLocation`, `RawReportExportBaseDir`) when generating or processing reports. The structure depends on the report type:\par");
            helpMessageBuilder.AppendLine(@"    * \b Daily/Weekly: \b0 `..\\\[Report Type Folder]\\\[Year]\\\[Month Name]\Week [Week Number]\` (e.g., `..\\Estimates\\Weekly Reports\\2025\\April\\Week 2\\`)\par");
            helpMessageBuilder.AppendLine(@"    * \b Monthly: \b0 `..\\\[Report Type Folder]\\\[Year]\\\[MMM yy]\` (e.g., `..\\Estimates\\Monthly Reports\\2025\\Apr 25\\`)\par");
            helpMessageBuilder.AppendLine(@"    * \b Quarterly: \b0 `..\\\[Report Type Folder]\\\[Year]\\\[Mmm to Mmm]\` (e.g., `..\\Estimates\\Quarterly reports\\2025\\Jan to Mar\\`)\par");
            helpMessageBuilder.AppendLine(@"    * \b Annual: \b0 `..\\\[Report Type Folder]\\\[Year]\` (e.g., `..\\Estimates\\Annual Reports\\2025\\`)\par");
            helpMessageBuilder.AppendLine(@"    * \b Custom: \b0 `..\\Custom Reports\[Year]\\\[yyyy-MM-dd_HHmmss]\` (e.g., `..\\Estimates\\Custom Reports\\2025\\2025-04-30_101500\\`)\par");
            helpMessageBuilder.AppendLine(@"* \b Log Archiving: \b0 Old log files (older than 30 days) are automatically moved to an 'Archive' subfolder within your user's log directory during application startup to keep the main log folder clean.\par");
            helpMessageBuilder.AppendLine(@"* \b Report Archiving: \b0 On startup, older report files/folders are archived: Final reports from previous years (e.g., `..\\Estimates\\Weekly Reports\\2024`) are moved into an `Archive` folder (`..\\Estimates\\Archive\\Weekly Reports\\2024`), merging if the destination exists. Raw report files older than 30 days (configurable) are moved into an `Archive\\YYYY-MM` subfolder within their report type folder (e.g., `..\\Exports\\Daily Reports\\Archive\\2025-03`).\par");
            helpMessageBuilder.AppendLine(@"* \b Weekly Sheet Creation: \b0 When processing a Weekly report, if the sheet for the corresponding Financial Year (e.g., '2024_25') doesn't exist in the central Power BI source file (`weekly report quotes conversion merged.xlsx`), the application will create it automatically, copying headers from the 'Analysis' sheet of the template.\par");
            helpMessageBuilder.AppendLine(@"\par");
            helpMessageBuilder.AppendLine(@"\b Troubleshooting: \b0\par");
            helpMessageBuilder.AppendLine(@"* Ensure the Crystal Report Wrapper service is running (the app tries to start it).\par");
            helpMessageBuilder.AppendLine(@"* Check file paths in `appsettings.json` if errors occur finding reports or templates. Use Options -> Edit appsettings.json to open it.\par");
            helpMessageBuilder.AppendLine(@"* Ensure the central weekly report file is accessible and not locked if appending fails.\par");
            helpMessageBuilder.AppendLine(@"* Check the application logs located in the 'Logs' subfolder (within the configured LogDirectory, specific to your username) for detailed error information. Use Options -> Open Logs Folder for quick access.\par");
            helpMessageBuilder.AppendLine(@"* If auto-run fails to update `appsettings.json`, check file permissions for the application directory.\par");
            helpMessageBuilder.AppendLine(@"* If you get an error refreshing a Slicer, remove it, then click into the Pivot table, in the PivotTable Fields on the right, Right Click customers and select add as slicer, move it back to where it was.\par");
            helpMessageBuilder.Append(@"}");

            string helpMessage = helpMessageBuilder.ToString();

            try
            {
                using var helpForm = new HelpForm(helpTitle, helpMessage, darkModeToolStripMenuItem.Checked);
                helpForm.ShowDialog(this);
            }
            catch (Exception ex)
            {
                Logger.LogError($"Failed to show HelpForm: {ex.Message}", ex);
                MessageBox.Show("Could not display help window. Please check application logs.", "Help Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Event handler for when the value of either date picker changes.
        /// If the change was likely manual (not programmatic), sets the report type to Custom.
        /// </summary>
        private void DatePicker_ValueChanged(object sender, EventArgs e)
        {
            if (_programmaticallyChangingDates) return;
            if (reportTypeComboBox.SelectedIndex == CustomReportIndex) return;
            Logger.LogDebug("DatePicker_ValueChanged: Manual date change detected. Setting Report Type to Custom.");
            UIManager.SafeControlUpdate(reportTypeComboBox, () => { if (reportTypeComboBox.Items.Count > CustomReportIndex) reportTypeComboBox.SelectedIndex = CustomReportIndex; });
        }

        /// <summary>
        /// Handles the Click event for the "View Configuration" menu item.
        /// Displays detailed configuration status in a message box.
        /// </summary>
        private void viewConfigToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Logger.LogInfo("Options -> View Configuration clicked.");
            bool configValid = CheckConfigValidity();
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("Configuration Details:");
            sb.AppendLine("--------------------------------------------------");
            sb.AppendLine($"1. Crystal Report Path (.rpt): '{CrystalReportLocation}' - Exists: {File.Exists(CrystalReportLocation)}");
            sb.AppendLine($"2. Wrapper EXE Path: '{Path.GetFullPath(_configuration["settings:WrapperExePath"] ?? string.Empty)}' - Exists: {File.Exists(Path.GetFullPath(_configuration["settings:WrapperExePath"] ?? string.Empty))}");
            sb.AppendLine($"3. Template Base Directory: '{ExcelTemplateBaseDir}' - Exists: {Directory.Exists(ExcelTemplateBaseDir)}");
            sb.AppendLine($"4. Raw Report Export Base Directory: '{RawReportExportBaseDir}' - Exists: {Directory.Exists(RawReportExportBaseDir)}");
            sb.AppendLine($"5. Final Excel Save Location Base: '{ExcelFinalSaveLocation}' - Exists: {Directory.Exists(ExcelFinalSaveLocation)}");
            string baseLogDir = ConfiguredLogDirectoryBase;
            string userLogDir = string.IsNullOrEmpty(baseLogDir) ? Path.Combine(UserProfilePath, "Logs", Environment.UserName) : Path.Combine(baseLogDir, string.Join("_", Environment.UserName.Split(Path.GetInvalidFileNameChars())));
            sb.AppendLine($"6. Application Log Directory (User Specific): '{Path.GetFullPath(userLogDir)}' - Exists: {Directory.Exists(Path.GetFullPath(userLogDir))}");
            sb.AppendLine($"7. appsettings.json Path: '{_appSettingsPath}' - Exists: {File.Exists(_appSettingsPath)}");
            sb.AppendLine("--------------------------------------------------");
            sb.AppendLine($"Overall Essential Config Valid (for report generation): {configValid}");
            FlexibleMessageBox.Show(sb.ToString(), "Configuration Details", MessageBoxButtons.OK, configValid ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
        }

        /// <summary>
        /// Handles the Click event for the "Validate Configuration" menu item.
        /// Performs a quick validation and updates the status bar.
        /// </summary>
        private void validateConfigToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Logger.LogInfo("Options -> Validate Configuration clicked.");
            _uiManager.UpdateProgress("Validating configuration...");
            bool isValid = CheckConfigValidity();
            string statusMessage = isValid ? "Configuration OK." : "Configuration Error: Essential paths missing or invalid. Check View Configuration.";
            if (isValid) Logger.LogInfo("Configuration validation successful."); else Logger.LogError("Configuration validation failed.");
            _uiManager.UpdateStatusMain(statusMessage);
            _ = Task.Delay(7000).ContinueWith(t => { if (_uiManager.GetCurrentStatusMain() == statusMessage) _uiManager.UpdateStatusMain("Ready"); }, TaskScheduler.FromCurrentSynchronizationContext());
        }

        /// <summary>
        /// Handles the Click event for the "Open Logs Folder" menu item.
        /// Opens the application's log directory.
        /// </summary>
        private void openLogsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Logger.LogInfo("Options -> Open Logs Folder clicked.");
            try
            {
                string baseLogDir = ConfiguredLogDirectoryBase;
                string actualUserLogDir = string.IsNullOrEmpty(baseLogDir)
                    ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Logs", Environment.UserName)
                    : Path.Combine(baseLogDir, string.Join("_", Environment.UserName.Split(Path.GetInvalidFileNameChars())));
                actualUserLogDir = Path.GetFullPath(actualUserLogDir);
                if (!Directory.Exists(actualUserLogDir)) Directory.CreateDirectory(actualUserLogDir);
                Process.Start("explorer.exe", actualUserLogDir);
            }
            catch (Exception ex) { Logger.LogError($"Error opening logs folder: {ex.Message}", ex); MessageBox.Show($"Could not open logs folder: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }

        /// <summary>
        /// Handles the Click event for the "Edit appsettings.json" menu item.
        /// Opens the appsettings.json file for editing.
        /// </summary>
        private void editConfigToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Logger.LogInfo("Options -> Edit appsettings.json clicked.");
            try
            {
                if (File.Exists(_appSettingsPath)) Process.Start(new ProcessStartInfo(_appSettingsPath) { UseShellExecute = true });
                else MessageBox.Show($"appsettings.json not found: {_appSettingsPath}", "File Not Found", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (Exception ex) { Logger.LogError($"Error opening appsettings.json: {ex.Message}", ex); MessageBox.Show($"Could not open appsettings.json: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }

        /// <summary>
        /// Handles the Click event for the "Exit" menu item.
        /// Closes the application.
        /// </summary>
        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Logger.LogInfo("Options -> Exit clicked. Closing application.");
            this.Close();
        }

        /// <summary>
        /// Handles the Click event for the "Manage Custom Bank Holidays" menu item.
        /// Opens the form for managing custom bank holidays.
        /// </summary>
        private void manageCustomBankHolidaysToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Logger.LogInfo("Options -> Manage Custom Bank Holidays clicked.");
            using (var manageForm = new ManageBankHolidaysForm(darkModeToolStripMenuItem.Checked))
            {
                manageForm.ShowDialog(this);
                // BankHolidayHelper's cache is cleared internally when holidays are added/removed.
                // This ensures subsequent date calculations use the updated list.
            }
        }
        #endregion

        #region Helper Methods
        /// <summary>
        /// Populates the Financial Year dropdown based on the current date.
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
                    if (!string.IsNullOrEmpty(previousFY)) { financialYearComboBox.Items.Add(previousFY); }
                }
                else { financialYearComboBox.Items.Add("FY Unknown"); }
                if (!string.IsNullOrEmpty(previouslySelected) && financialYearComboBox.Items.Contains(previouslySelected)) financialYearComboBox.SelectedItem = previouslySelected;
                else if (financialYearComboBox.Items.Count > 0) financialYearComboBox.SelectedIndex = 0;
            });
        }

        /// <summary>
        /// Validates that the 'From' date is not after the 'To' date.
        /// </summary>
        /// <returns>True if dates are valid, false otherwise.</returns>
        private bool ValidateInputDates()
        {
            if (startDatePicker.Value.Date > endDatePicker.Value.Date)
            {
                MessageBox.Show("The 'From' date cannot be after the 'To' date.", "Date Range Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
            return true;
        }

        /// <summary>
        /// Validates if the selected date range falls within the selected Financial Year (if applicable).
        /// </summary>
        /// <returns>True if the financial year is valid for the selected dates, or if validation is not applicable; false otherwise.</returns>
        private bool ValidateFinancialYearSelection()
        {
            if (reportTypeComboBox.SelectedIndex == CustomReportIndex || !financialYearComboBox.Visible) return true;
            if (financialYearComboBox.SelectedItem != null)
            {
                string selectedFinYear = financialYearComboBox.SelectedItem.ToString()!;
                if (!_excelProcessor.IsFinancialYearValid(selectedFinYear, startDatePicker.Value, endDatePicker.Value))
                {
                    DialogResult dr = MessageBox.Show($"The selected date range ({startDatePicker.Value:d} - {endDatePicker.Value:d}) does not fall entirely within the selected Financial Year ({selectedFinYear}).\n\nDo you want to continue anyway?", "Financial Year Mismatch Warning", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                    if (dr == DialogResult.No) return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Helper to determine the base path for appsettings.json.
        /// </summary>
        /// <returns>The base path for appsettings.json.</returns>
        private static string DetermineAppSettingsBasePath() => @"\\harlow.local\DFS\IT Department\Applications\Development 2025\QuoteConversionReportAutomation\conversionTest";

        /// <summary>
        /// Checks if the core configuration paths (Crystal Report and Wrapper EXE) are valid.
        /// </summary>
        /// <returns>True if essential configuration paths are valid; false otherwise.</returns>
        private bool CheckConfigValidity()
        {
            string crPath = CrystalReportLocation; string wrapPath = _configuration["settings:WrapperExePath"] ?? "";
            return !string.IsNullOrEmpty(crPath) && File.Exists(crPath) && !string.IsNullOrEmpty(wrapPath) && File.Exists(Path.GetFullPath(wrapPath));
        }

        /// <summary>
        /// Checks if the Daily report type is currently selected.
        /// </summary>
        /// <returns>True if "Daily" is selected; false otherwise.</returns>
        private bool IsDailySelected() => reportTypeComboBox.SelectedIndex == DailyReportIndex;

        /// <summary>
        /// Centralized method to reset the UI state via UIManager after errors or cancellations.
        /// </summary>
        /// <param name="button1Text">Text for the create report button.</param>
        private void ResetUIStateOnError(string button1Text)
        {
            _uiManager.ResetUIOnError(button1Text, CheckConfigValidity(),
                !string.IsNullOrEmpty(_generatedReportPath) && File.Exists(_generatedReportPath),
                !string.IsNullOrEmpty(_generatedAnalysisFilePath) && File.Exists(_generatedAnalysisFilePath),
                IsDailySelected(), dailyCheckTimer.Enabled, darkModeToolStripMenuItem.Checked, false, autoRunStatusLabel.Text ?? "");
        }

        /// <summary>
        /// Asynchronously sends the completion email using EmailUtility.
        /// </summary>
        /// <param name="attachmentPath">Path to the file to attach.</param>
        /// <param name="progress">Progress reporter for status updates.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        private async Task SendCompletionEmailAsync(string attachmentPath, IProgress<string> progress, CancellationToken cancellationToken)
        {
            Logger.LogTrace("Entering SendCompletionEmailAsync");
            _uiManager.UpdateProgress("Sending email...");
            if (!File.Exists(attachmentPath)) throw new FileNotFoundException("Attachment file not found.", attachmentPath);
            try
            {
                var (to, cc) = GetEmailRecipients();
                if (to.Count == 0 && cc.Count == 0) throw new InvalidOperationException("No recipients.");
                var (subj, body) = GetEmailSubjectAndBody(startDatePicker.Value, endDatePicker.Value);
                if (!await _emailUtility.SendEmailAsync(to, cc, subj, body, attachmentPath, progress, cancellationToken) && !cancellationToken.IsCancellationRequested)
                    throw new Exception("Email sending failed.");
                Logger.LogInfo("Email sent successfully.");
            }
            catch (OperationCanceledException) { Logger.LogWarning("Email sending cancelled."); throw; }
            catch (Exception ex) { Logger.LogError($"Error sending email: {ex}", ex); MessageBox.Show($"Failed to send email: {ex.Message}", "Email Error", MessageBoxButtons.OK, MessageBoxIcon.Error); throw; }
        }

        /// <summary>
        /// Determines email recipients based on report type, checkbox, and build mode.
        /// This version is provided by the user.
        /// </summary>
        private (List<string> To, List<string> Cc) GetEmailRecipients()
        {
            Logger.LogTrace("Entering GetEmailRecipients...");
            List<string> toAddresses = [];
            List<string> ccAddresses = [];
            // Ensure checkbox state is read correctly
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
                // Default Logic (Applies to DEBUG Mode OR Non-Daily Reports in RELEASE Mode)
#if DEBUG
                // --- DEBUG Build Recipients ---
                Logger.LogInfo("DEBUG Build: Using debug email recipients.");
                // Always send To: chrisp (or config override)
                toAddresses.Add(_configuration["settings:DebugEmails:To"] ?? "chrisp@harlowsolutions.co.uk");

                string? debugCC1 = _configuration["settings:DebugEmails:CC1"] ?? "chrisp@harlowsolutions.co.uk"; // Chris P default
                string? debugCC2 = _configuration["settings:DebugEmails:CC2"] ?? "jamier@harlowsolutions.co.uk"; // Jamie R default

                if (sendToFemiOnly) // Checkbox IS checked
                {
                    Logger.LogDebug("DEBUG Build: Femi checkbox CHECKED. Adding CC1 and CC2.");
                    if (!string.IsNullOrWhiteSpace(debugCC1)) ccAddresses.Add(debugCC1);
                    if (!string.IsNullOrWhiteSpace(debugCC2)) ccAddresses.Add(debugCC2);
                }
                else // Checkbox is NOT checked
                {
                    Logger.LogDebug("DEBUG Build: Femi checkbox NOT CHECKED. Adding CC1 only.");
                    if (!string.IsNullOrWhiteSpace(debugCC1)) ccAddresses.Add(debugCC1);
                }
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
        private (string Subject, string Body) GetEmailSubjectAndBody(DateTime reportStartDate, DateTime reportEndDate)
        {
            string typeName = "Estimate Success Rate"; int type = reportTypeComboBox.SelectedIndex; bool femi = sendToFemiOnlyCheckBox.Checked;
            string greeting = IsDebug ? "Hi Debug," : (femi ? "Hi Femi," : "Hi All,");
            if (type == DailyReportIndex && !IsDebug) greeting = _configuration["settings:ProductionEmails:AutoRunDailyGreeting"] ?? "Hi Paul,";
            string rangeInfo = reportStartDate.Date == reportEndDate.Date ? $"for {reportEndDate:dd MMM yy}" : $"for period {reportStartDate:dd MMM yy} to {reportEndDate:dd MMM yy}";
            string subjectPrefix = $"{reportTypeComboBox.Text} {typeName}";
            if (type == MonthlyReportIndex) rangeInfo = $"for {reportStartDate:MMMM yy}"; else if (type == QuarterlyReportIndex) rangeInfo = $"for {ReportHelper.GetQuarterString(reportStartDate)} {reportStartDate.Year}"; else if (type == AnnualReportIndex) rangeInfo = $"for {reportStartDate.Year}";
            string subject = (type != CustomReportIndex ? "AUTOMATED: " : "") + $"{subjectPrefix} Report ({reportEndDate:yyyy-MM-dd})";
            return (subject, $"{greeting}\n\nPlease find attached the {subjectPrefix.ToLower()} report {rangeInfo}.\n\nThis report includes quotes data for review.\n\nThank you,\nAutomation Service");
        }

        /// <summary>
        /// Reads a configuration value and splits it into a list of strings.
        /// </summary>
        /// <param name="key">The configuration key.</param>
        /// <returns>A list of strings, or null if the key is not found or the value is empty.</returns>
        private List<string>? GetStringListFromConfig(string key) => _configuration[key]?.Split([',', ';', ' '], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();

        /// <summary>
        /// Handles the manual Excel refresh step by opening Excel and waiting for the user.
        /// </summary>
        /// <param name="filePath">The path to the Excel file to open.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>True if the user confirms to proceed after closing Excel; false otherwise.</returns>
        private async Task<bool> HandleManualExcelRefreshAsync(string filePath, CancellationToken token)
        {
            _uiManager.UpdateProgress("Checking for running Excel instances...");
            if (await Task.Run(() => Process.GetProcessesByName("EXCEL").Length > 0, token))
            {
                var dr = MessageBox.Show(this, "Other Excel instances are running. Close them before proceeding?", "Close Other Excel Instances?", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning);
                if (dr == DialogResult.Cancel) { return false; }
                if (dr == DialogResult.Yes) { _uiManager.UpdateProgress("Attempting to close other Excel instances..."); await Task.Run(() => ReportHelper.CloseProcessesByName("EXCEL"), token); await Task.Delay(1500, token); }
            }
            MessageBox.Show(this, "The report will open in Excel.\n\n*** IMPORTANT ***\n1. Refresh All Pivots/Slicers.\n2. SAVE the file.\n3. CLOSE Excel.\n\nThe application will wait.", "Manual Refresh Required", MessageBoxButtons.OK, MessageBoxIcon.Information);
            token.ThrowIfCancellationRequested();
            _uiManager.UpdateProgress("Opening Excel...");
            Process? excelProc = null;
            try
            {
                excelProc = await Task.Run(() => Process.Start(new ProcessStartInfo(filePath) { UseShellExecute = true }), token);
                if (excelProc == null) throw new Exception("Process.Start returned null for Excel.");
                _uiManager.UpdateProgress("Excel opened. Waiting for you to Refresh All, Save, and Close...");
                await excelProc.WaitForExitAsync(token);
                _uiManager.UpdateStatusMain("Excel closed.");
                DialogResult sendResult = MessageBox.Show(this, "Excel closed.\n\nProceed with sending the email?", "Confirm Email Send", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                return (sendResult == DialogResult.Yes);
            }
            catch (OperationCanceledException) { Logger.LogWarning("Manual Excel refresh cancelled."); return false; }
            catch (Exception ex) { Logger.LogError($"Error during manual Excel handling: {ex}"); MessageBox.Show($"An error occurred managing Excel refresh:\n\n{ex.Message}", "Excel Interaction Error", MessageBoxButtons.OK, MessageBoxIcon.Error); return false; }
            finally { excelProc?.Dispose(); }
        }
        #endregion
    }
}
