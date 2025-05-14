// C# 10+ Features
// --- Global Usings ---
using Microsoft.Extensions.Configuration;
using QuoteConversionReportAutomation; // For EmailRecipientManager, ManageEmailRecipientsForm etc.
using QuoteConversionReportAutomation.Managers;
using QuoteConversionReportAutomation.Services.Communication;
using QuoteConversionReportAutomation.Services.Excel;
using QuoteConversionReportAutomation.Services.Logging;
using System.Diagnostics;
using System.Text; // Required for StringBuilder
using Microsoft.VisualBasic; // Required for Interaction.InputBox
using QuoteConversionReportAutomation.Helpers; 

namespace conversionTest
{
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
    /// Annual report now uses financial year (May-April).
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
        private const string AppVersion = "1.8.1"; // Ensure this is up-to-date

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
                    MonthlyReportIndex or QuarterlyReportIndex or AnnualReportIndex or CustomReportIndex => "TEMPLATE_Estimate Success Rate_Monthly.xlsx",
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
                    _uiManager, _excelProcessor, _appSettingsPath, _emailRecipientManager, _currentAutoRunHour
                );

                Logger.LogDebug("Event handlers wired up.");
            }
            catch (Exception ex)
            {
                Logger.LogCritical($"CRITICAL ERROR during Form Initialization: {ex.Message}", ex);
                // Use standard MessageBox for critical startup errors before FlexibleMessageBox might be ready or if it's part of the issue.
                System.Windows.Forms.MessageBox.Show($"A critical error occurred initializing the application:\n\n{ex.Message}\n\nThe application cannot continue.",
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

                if (!reportTypeComboBox.Items.Contains("Custom"))
                {
                    reportTypeComboBox.Items.Add("Custom");
                }
                if (reportTypeComboBox.Items.Count > DailyReportIndex) reportTypeComboBox.SelectedIndex = DailyReportIndex;
                else if (reportTypeComboBox.Items.Count > 0) reportTypeComboBox.SelectedIndex = 0;
                reportTypeComboBox.DropDownStyle = ComboBoxStyle.DropDownList;

                bool useDarkMode = UIManager.IsWindowsDarkModeEnabled();
                darkModeToolStripMenuItem.Checked = useDarkMode;
                _uiManager.ApplyTheme(useDarkMode);

                _uiManager.UpdateAutoRunUI(dailyCheckTimer.Enabled, false, useDarkMode, $"Auto Run: {(dailyCheckTimer.Enabled ? $"Enabled (Next check ~{_currentAutoRunHour}:00)" : "Disabled")}");

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
                FlexibleMessageBox.Show(this, $"A critical error occurred loading the application:\n\n{ex.Message}\n\nThe application may not function correctly.",
                    "Application Load Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                Logger.LogWarning("1-Click Process: Raw report generation failed or was cancelled. Aborting.");
                ResetUIStateOnError("Generate, Process && Email Report");
                return;
            }

            await PerformProcessAndEmailAsync(skipEmail: skipEmailCheckBox.Checked);

            Logger.LogInfo("1-Click Process sequence completed (or aborted).");
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
            string originalButtonText = string.Empty;
            UIManager.SafeControlUpdate(currentActionButton, () => originalButtonText = currentActionButton.Text);

            UIManager.SafeControlUpdate(currentActionButton, () => currentActionButton.Enabled = false);
            if (currentActionButton == createReportButton)
            {
                UIManager.SafeControlUpdate(processEmailButton, () => processEmailButton.Enabled = false);
            }
            else
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
                if (!ValidateInputDates()) { ResetUIStateOnError(originalButtonText); return; }
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

                    if (oneClickProcessButton.Visible)
                    {
                        // Button remains disabled
                    }
                    else
                    {
                        UIManager.SafeControlUpdate(createReportButton, () => createReportButton.Text = "Report Created");
                        UIManager.SafeControlUpdate(processEmailButton, () => processEmailButton.Enabled = true);
                        _uiManager.SetOtherControlsEnabled(true, financialYearComboBox.Visible);
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
                FlexibleMessageBox.Show(this, "The report generation request timed out or was cancelled.",
                    "Timeout / Cancelled", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                ResetUIStateOnError("Cancelled");
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error during Create Report operation: {ex.Message}", ex);
                FlexibleMessageBox.Show(this, $"An error occurred while requesting the report:\n\n{ex.Message}",
                    "Report Request Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                ResetUIStateOnError("Error");
            }
        }

        private async Task PerformProcessAndEmailAsync(bool skipEmail = false)
        {
            Logger.LogTrace($"Entering PerformProcessAndEmailAsync logic (skipEmail: {skipEmail})");
            Button currentActionButton = oneClickProcessButton.Visible ? oneClickProcessButton : processEmailButton;
            string originalButtonText = string.Empty;
            UIManager.SafeControlUpdate(currentActionButton, () => originalButtonText = currentActionButton.Text);

            UIManager.SafeControlUpdate(currentActionButton, () => currentActionButton.Enabled = false);
            if (currentActionButton == processEmailButton)
            {
                UIManager.SafeControlUpdate(createReportButton, () => createReportButton.Enabled = false);
            }
            else
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

            DateTime dateForFilenameAndExcelProcessing = (reportType == AnnualReportIndex) ? startDatePicker.Value : endDatePicker.Value;


            try
            {
                if (!ValidateInputDates()) { ResetUIStateOnError(originalButtonText); return; }
                if (string.IsNullOrEmpty(_generatedReportPath) || !File.Exists(_generatedReportPath))
                {
                    FlexibleMessageBox.Show(this, "The raw report file has not been generated or cannot be found. Please create the report first.",
                        "Raw Report Missing", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    ResetUIStateOnError(oneClickProcessButton.Visible ? "Generate, Process && Email Report" : "Create Report");
                    return;
                }

                string? expectedFinalPath = _excelProcessor.GetExpectedFinalFilePath(reportType, baseSaveLocation, dateForFilenameAndExcelProcessing);
                if (expectedFinalPath != null && File.Exists(expectedFinalPath))
                {
                    generalProgress.Report("Found existing file. Prompting user...");
                    DialogResult fdr = FlexibleMessageBox.Show(this, $"The report file '{Path.GetFileName(expectedFinalPath)}' already exists for this period.\n\nDo you want to skip processing and use this existing file?",
                        "File Already Exists", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

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
                        if (proceedToEmail || skipEmail)
                        {
                            _uiManager.SetUICompleted(CheckConfigValidity(), IsDailySelected(), dailyCheckTimer.Enabled, darkModeToolStripMenuItem.Checked, false, autoRunStatusLabel.Text ?? "");
                        }
                        ResetUIStateOnError(oneClickProcessButton.Visible ? "Generate, Process && Email Report" : "Create Report");
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
                    1, 1, excelProgress, dateForFilenameAndExcelProcessing, token);

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
                else if (skipEmail)
                {
                    _uiManager.UpdateStatusMain("Process completed. Email skipped by user.");
                    Logger.LogInfo("Email sending skipped by user checkbox.");
                }

                if (proceedToEmailAfterGenerate || skipEmail)
                {
                    _uiManager.SetUICompleted(CheckConfigValidity(), IsDailySelected(), dailyCheckTimer.Enabled, darkModeToolStripMenuItem.Checked, false, autoRunStatusLabel.Text ?? "");
                }
                ResetUIStateOnError(oneClickProcessButton.Visible ? "Generate, Process && Email Report" : "Create Report");

            }
            catch (OperationCanceledException)
            {
                Logger.LogWarning("Excel processing or subsequent step cancelled.");
                ResetUIStateOnError("Cancelled");
            }
            catch (FileNotFoundException fnfEx)
            {
                Logger.LogError($"File not found during Process & Email operation: {fnfEx.Message}", fnfEx);
                FlexibleMessageBox.Show(this, fnfEx.Message, "File Not Found Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                ResetUIStateOnError("File Error");
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error during Process & Email operation: {ex.Message}", ex);
                FlexibleMessageBox.Show(this, $"An unexpected error occurred during processing:\n\n{ex.Message}",
                    "Processing Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                ResetUIStateOnError("Error");
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
                        dateTo = ReportHelper.GetPreviousWorkday(todayValue);
                        showFinYear = false;
                        break;
                    case WeeklyReportIndex:
                        dateFrom = todayValue.AddDays(-14);
                        dateTo = todayValue;
                        showFinYear = true;
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
                        dateFrom = todayValue;
                        dateTo = todayValue;
                        showFinYear = true;
                        Logger.LogWarning($"Unexpected reportTypeComboBox index: {selectedIndex}. Defaulting dates.");
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

                bool isDaily = IsDailySelected();
                UIManager.SafeControlUpdate(sendToFemiOnlyCheckBox, () => { sendToFemiOnlyCheckBox.Visible = !isDaily; });
                UIManager.SafeControlUpdate(emailRecipientLabel, () =>
                {
                    emailRecipientLabel.Visible = isDaily;
                    if (isDaily) emailRecipientLabel.Text = "Emailing Daily report to Paul";
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
            Logger.LogInfo("Daily Check Timer Ticked.");

            try
            {
                await _autoRunManager.PerformDailyCheckAsync(originallyEnabled, _currentAutoRunHour);
            }
            catch (Exception ex)
            {
                Logger.LogCritical($"CRITICAL ERROR during AutoRunManager.PerformDailyCheckAsync dispatch: {ex.Message}", ex);
                _uiManager.UpdateStatusMain("Critical AutoRun Error! Check Logs.");
                _uiManager.UpdateStatusRight("AutoRun: FAILED");
                originallyEnabled = false;
            }
            finally
            {
                if (originallyEnabled)
                {
                    dailyCheckTimer.Start();
                    Logger.LogInfo("Daily Check Timer Restarted.");

                    Logger.LogDebug("dailyCheckTimer_Tick: Calling ResetUIStateOnError after auto-run check.");
                    ResetUIStateOnError(oneClickProcessButton.Visible ? "Generate, Process && Email Report" : "Create Report");
                }
                else
                {
                    Logger.LogInfo("Daily Check Timer remains stopped.");
                    if (!dailyCheckTimer.Enabled)
                    {
                        bool isFinalToday = (autoRunStatusLabel.Text?.Contains("Completed") ?? false) ||
                                            (autoRunStatusLabel.Text?.Contains("Done for") ?? false) ||
                                            (autoRunStatusLabel.Text?.Contains("FAILED") ?? false);

                        _uiManager.UpdateAutoRunUI(false, isFinalToday, darkModeToolStripMenuItem.Checked,
                                                  isFinalToday ? (autoRunStatusLabel.Text ?? "Auto Run: Disabled") : "Auto Run: Disabled");
                    }
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
            string helpTitle = $"Help - Quote Conversion v{AppVersion}";
            StringBuilder helpMessageBuilder = new StringBuilder();
            bool isDarkModeActive = darkModeToolStripMenuItem.Checked;
            string rtfDefaultTextColor = isDarkModeActive ? @"\red220\green220\blue220;" : @"\red0\green0\blue0;";
            string rtfAccentColor = isDarkModeActive ? @"\red255\green100\blue100;" : @"\red255\green0\blue0;";

            helpMessageBuilder.AppendLine(@"{\rtf1\ansi\ansicpg1252\deff0\nouicompat{\fonttbl{\f0\fnil\fcharset0 Segoe UI;}{\f1\fnil\fcharset2 Symbol;}}");
            helpMessageBuilder.AppendLine($@"{{\colortbl ;{rtfDefaultTextColor}{rtfAccentColor}}}");
            helpMessageBuilder.AppendLine(@"\pard\cf1\sa200\sl276\slmult1");
            helpMessageBuilder.AppendLine($@"\b\f0\fs28 Quote Conversion Automation Tool v{AppVersion}\b0\fs20\par");
            helpMessageBuilder.AppendLine(@"\par");
            helpMessageBuilder.AppendLine(@"\b\fs22 Welcome!\b0\fs20\par");
            helpMessageBuilder.AppendLine(@"This tool automates the generation, processing, and emailing of Estimate Success Rate reports, streamlining your workflow.\par");
            // ... (rest of the RTF help text generation, ensuring it uses FlexibleMessageBox.ShowRtf if needed within HelpForm) ...
            helpMessageBuilder.AppendLine(@"Thank you for using the Quote Conversion Automation Tool!\par");
            helpMessageBuilder.AppendLine(@"}");

            string helpMessage = helpMessageBuilder.ToString();
            try
            {
                // Assuming HelpForm uses FlexibleMessageBox internally or is simple enough not to require it.
                // If HelpForm itself uses FlexibleMessageBox, its calls would also need updating if they used advanced features.
                // For this exercise, we assume HelpForm is compatible or uses standard MessageBox for its internal dialogs.
                using var helpForm = new HelpForm(helpTitle, helpMessage, darkModeToolStripMenuItem.Checked);
                helpForm.ShowDialog(this);
            }
            catch (Exception ex)
            {
                Logger.LogError($"Failed to show HelpForm: {ex.Message}", ex);
                FlexibleMessageBox.Show(this, "Could not display help window. Please check application logs.",
                    "Help Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void DatePicker_ValueChanged(object sender, EventArgs e)
        {
            if (_programmaticallyChangingDates) return;

            if (reportTypeComboBox.SelectedIndex != CustomReportIndex)
            {
                Logger.LogDebug("DatePicker_ValueChanged: Manual date change detected. Setting Report Type to Custom.");
                UIManager.SafeControlUpdate(reportTypeComboBox, () =>
                {
                    if (reportTypeComboBox.Items.Count > CustomReportIndex)
                    {
                        reportTypeComboBox.SelectedIndex = CustomReportIndex;
                    }
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
                FlexibleMessageBox.Show(this, $"Could not open logs folder: {ex.Message}",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                    FlexibleMessageBox.Show(this, $"appsettings.json not found at the expected location:\n{_appSettingsPath}",
                        "File Not Found", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error opening appsettings.json: {ex.Message}", ex);
                FlexibleMessageBox.Show(this, $"Could not open appsettings.json: {ex.Message}",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                FlexibleMessageBox.Show(this, "Could not open the email recipient management window. Please check logs.",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }


        private void enable1ClickProcessingToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Update1ClickProcessingModeUI();
            ResetUIStateOnError(enable1ClickProcessingToolStripMenuItem.Checked ? "Generate, Process && Email Report" : "Create Report");
        }

        private async void setAutoRunHourToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Logger.LogInfo("Options -> Set Auto-Run Hour clicked.");
            string currentHourPrompt = _currentAutoRunHour.ToString();

            // Using Microsoft.VisualBasic.Interaction.InputBox for input as FlexibleMessageBox v1.5 does not have input fields
            string? inputText = Interaction.InputBox("Enter the new hour (0-23) for the daily auto-run check:",
                                                     "Set Auto-Run Hour",
                                                     currentHourPrompt);

            if (!string.IsNullOrWhiteSpace(inputText)) // Check if user provided input (didn't cancel or leave empty)
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
                            FlexibleMessageBox.Show(this, $"Auto-Run hour has been set to {newHour}:00.\nThe change will take effect from the next daily check cycle.",
                                "Auto-Run Hour Updated", MessageBoxButtons.OK, MessageBoxIcon.Information);

                            _uiManager.SetAutoRunHour(_currentAutoRunHour);
                            _uiManager.UpdateAutoRunUI(dailyCheckTimer.Enabled, false, darkModeToolStripMenuItem.Checked, $"Auto Run: {(dailyCheckTimer.Enabled ? $"Enabled (Next check ~{_currentAutoRunHour}:00)" : "Disabled")}");
                        }
                        else
                        {
                            Logger.LogError("Failed to save the new auto-run hour to configuration.");
                            FlexibleMessageBox.Show(this, "Failed to save the new auto-run hour. Please check logs and file permissions for appsettings.json.",
                                "Error Saving Setting", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                    else
                    {
                        FlexibleMessageBox.Show(this, "The new hour is the same as the current auto-run hour. No change made.",
                            "No Change", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
                else
                {
                    FlexibleMessageBox.Show(this, "Invalid hour entered. Please enter a number between 0 and 23.",
                        "Invalid Input", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            else
            {
                Logger.LogInfo("Set Auto-Run Hour cancelled by user or no input provided.");
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

            UIManager.SafeControlUpdate(oneClickProcessButton, () =>
            {
                oneClickProcessButton.Visible = oneClickEnabled;
                if (oneClickEnabled && oneClickProcessButton.Visible) oneClickProcessButton.BringToFront();
            });
            UIManager.SafeControlUpdate(createReportButton, () =>
            {
                createReportButton.Visible = !oneClickEnabled;
            });
            UIManager.SafeControlUpdate(processEmailButton, () =>
            {
                processEmailButton.Visible = !oneClickEnabled;
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
                FlexibleMessageBox.Show(this, "The 'From' date cannot be after the 'To' date.",
                    "Date Range Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
            return true;
        }

        private bool ValidateFinancialYearSelection()
        {
            if (!financialYearComboBox.Visible || financialYearComboBox.SelectedItem == null)
            {
                return true;
            }

            string selectedFinYear = financialYearComboBox.SelectedItem.ToString()!;
            if (!_excelProcessor.IsFinancialYearValid(selectedFinYear, startDatePicker.Value, endDatePicker.Value))
            {
                DialogResult fdr = FlexibleMessageBox.Show(this, $"The selected date range ({startDatePicker.Value:d} - {endDatePicker.Value:d}) " +
                             $"does not fall entirely within the selected Financial Year ({selectedFinYear}).\n\n" +
                             "Do you want to continue anyway?", "Financial Year Mismatch Warning",
                             MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                return fdr == DialogResult.Yes;
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

            UIManager.SafeControlUpdate(this, () =>
            {
                if (isOneClickMode)
                {
                    oneClickProcessButton.Text = configValid ? mainButtonText : "Config Error";
                    oneClickProcessButton.Enabled = configValid;
                    createReportButton.Enabled = false;
                    processEmailButton.Enabled = false;
                }
                else
                {
                    createReportButton.Text = configValid ? mainButtonText : "Config Error";
                    createReportButton.Enabled = configValid;
                    processEmailButton.Text = "Process and Email";
                    processEmailButton.Enabled = configValid && !string.IsNullOrEmpty(_generatedReportPath) && File.Exists(_generatedReportPath);
                    oneClickProcessButton.Enabled = false;
                }
            });

            _uiManager.ResetUIOnError(
                mainButtonText,
                configValid,
                !string.IsNullOrEmpty(_generatedReportPath) && File.Exists(_generatedReportPath),
                !string.IsNullOrEmpty(_generatedAnalysisFilePath) && File.Exists(_generatedAnalysisFilePath),
                IsDailySelected(),
                dailyCheckTimer.Enabled,
                darkModeToolStripMenuItem.Checked,
                (autoRunStatusLabel.Text?.Contains("Completed") ?? false) || (autoRunStatusLabel.Text?.Contains("Done for") ?? false) || (autoRunStatusLabel.Text?.Contains("FAILED") ?? false),
                autoRunStatusLabel.Text ?? ""
            );
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
                if (to.Count == 0 && cc.Count == 0 && !IsDebug)
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
                    Logger.LogError("Email sending failed. Check logs for details from EmailUtility. Continuing operation if possible.");
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
                Logger.LogWarning("Email sending operation was cancelled.");
                progress.Report("Email sending cancelled.");
                throw;
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error sending email: {ex.Message}", ex);
                FlexibleMessageBox.Show(this, $"Failed to send email: {ex.Message}",
                    "Email Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                throw;
            }
        }

        private (List<string> To, List<string> Cc) GetEmailRecipients()
        {
            Logger.LogTrace("Form1: Entering GetEmailRecipients, deferring to EmailRecipientManager...");
            bool isFemiOnly = sendToFemiOnlyCheckBox.Checked;
            int currentReportType = reportTypeComboBox.SelectedIndex;

            var recipients = _emailRecipientManager.GetRecipients(currentReportType, isFemiOnly, IsDebug);

            Logger.LogDebug($"Form1: Recipients from Manager - To: {string.Join("; ", recipients.To)}, CC: {string.Join("; ", recipients.Cc)}");
            Logger.LogTrace("Form1: Exiting GetEmailRecipients.");
            return recipients;
        }

        private (string Subject, string Body) GetEmailSubjectAndBody(DateTime reportStartDate, DateTime reportEndDate)
        {
            string typeName = "Estimate Success Rate";
            string reportTypeString = "";
            UIManager.SafeControlUpdate(reportTypeComboBox, () => reportTypeString = reportTypeComboBox.Text);

            int type = reportTypeComboBox.SelectedIndex;
            bool femiOnlyChecked = sendToFemiOnlyCheckBox.Checked;

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
                    rangeInfo = $"for {reportStartDate:MMMM yy}";
                    break;
                case QuarterlyReportIndex:
                    rangeInfo = $"for {ReportHelper.GetQuarterString(reportStartDate)} {reportStartDate.Year}";
                    break;
                case AnnualReportIndex:
                    rangeInfo = $"for Financial Year {reportStartDate.Year}-{reportEndDate.Year}";
                    subjectPrefix = $"Annual {typeName}";
                    break;
                case CustomReportIndex:
                    rangeInfo = $"for period {reportStartDate:dd MMM yy} to {reportEndDate:dd MMM yy}";
                    break;
                default:
                    rangeInfo = $"for period {reportStartDate:dd MMM yy} to {reportEndDate:dd MMM yy}";
                    subjectPrefix = $"Report {typeName}";
                    break;
            }

            string subject = (type != CustomReportIndex && type != -1 ? "AUTOMATED: " : "") +
                             $"{subjectPrefix} Report ({reportEndDate:yyyy-MM-dd})";
            if (type == AnnualReportIndex)
            {
                subject = (type != CustomReportIndex && type != -1 ? "AUTOMATED: " : "") +
                          $"{subjectPrefix} Report ({reportStartDate.Year}-{reportEndDate.Year})";
            }

            string body = $"{greeting}\n\nPlease find attached the {subjectPrefix.ToLower()} report {rangeInfo}.\n\nThis report includes quotes data for review.\n\nThank you,\nAutomation Service";
            return (subject, body);
        }

        private async Task<bool> HandleManualExcelRefreshAsync(string filePath, CancellationToken token)
        {
            _uiManager.UpdateProgress("Checking for running Excel instances...");
            if (await Task.Run(() => Process.GetProcessesByName("EXCEL").Length > 0, token))
            {
                DialogResult fdr = FlexibleMessageBox.Show(this, "Other Excel instances are running. Close them before proceeding with the manual refresh?",
                    "Close Other Excel Instances?", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning);

                if (fdr == DialogResult.Cancel) { Logger.LogInfo("User cancelled manual refresh due to other Excel instances."); return false; }
                if (fdr == DialogResult.Yes)
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
