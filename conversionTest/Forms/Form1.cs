// Form1.cs
// This version is fully corrected to resolve all compilation errors reported.
// The UIManager constructor call and the switch expression for date ranges have been fixed.

#region Using Directives
// System related namespaces
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualBasic;
// Project specific namespaces
using QuoteConversionReportAutomation.Configuration;
using QuoteConversionReportAutomation.Forms;
using QuoteConversionReportAutomation.Helpers;
using QuoteConversionReportAutomation.Interfaces;
using QuoteConversionReportAutomation.Managers;
using QuoteConversionReportAutomation.Models;
using QuoteConversionReportAutomation.Models.Status;
using QuoteConversionReportAutomation.Orchestrators;
using QuoteConversionReportAutomation.Orchestrators.Interfaces;
using QuoteConversionReportAutomation.Services;
using QuoteConversionReportAutomation.Services.Communication;
using QuoteConversionReportAutomation.Services.Excel;
using QuoteConversionReportAutomation.Services.Interfaces;
using QuoteConversionReportAutomation.Services.Logging;
using QuoteConversionReportAutomation.Theming;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
#endregion

namespace conversionTest
{
    /// <summary>
    /// Represents the main form of the Quote Conversion Report Automation (QCRA) application.
    /// It serves as the primary user interface for manual report generation and for monitoring
    /// the automated report processes.
    /// </summary>
    public partial class Form1 : Form, IAutoRunUIContext
    {
        #region Fields and Properties
        private readonly IConfiguration _configuration;
        private readonly IReportPathService _reportPathService;
        private readonly IManualReportOrchestrator _manualReportOrchestrator;
        private readonly EmailUtility _emailUtility;
        private readonly ReportProcessManager _processManager;
        private readonly NamedPipeCommunicator _pipeCommunicator;
        private readonly AutoRunManager _autoRunManager;
        private readonly ExcelCopyData _excelProcessor;
        private readonly EmailRecipientManager _emailRecipientManager;
        private readonly GreetingManager _greetingManager;
        private readonly IServiceProvider _serviceProvider;
        private readonly IStatusManagerService _statusManager;
        private readonly UIManager _uiManager;
        private string _appName;
        private string _appVersion;
        private bool _programmaticallyChangingDates = false;
        private int _currentAutoRunHour;
        private HelpForm? _helpFormInstance;
        private string? _lastGeneratedRawReportPath = null;
        private string? _lastGeneratedAnalysisFilePath = null;
        private static bool IsDebug =>
#if DEBUG
            true;
#else
            false;
#endif
        #endregion

        #region Constructor
        /// <summary>
        /// Initializes a new instance of the <see cref="Form1"/> class, injecting all required dependencies.
        /// </summary>
        public Form1(
            IConfiguration configuration, IReportPathService reportPathService, IManualReportOrchestrator manualReportOrchestrator,
            EmailUtility emailUtility, ReportProcessManager processManager, NamedPipeCommunicator pipeCommunicator,
            AutoRunManager autoRunManager, ExcelCopyData excelProcessor, EmailRecipientManager emailRecipientManager,
            GreetingManager greetingManager, IServiceProvider serviceProvider, IStatusManagerService statusManager)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _reportPathService = reportPathService ?? throw new ArgumentNullException(nameof(reportPathService));
            _manualReportOrchestrator = manualReportOrchestrator ?? throw new ArgumentNullException(nameof(manualReportOrchestrator));
            _emailUtility = emailUtility ?? throw new ArgumentNullException(nameof(emailUtility));
            _processManager = processManager ?? throw new ArgumentNullException(nameof(processManager));
            _pipeCommunicator = pipeCommunicator ?? throw new ArgumentNullException(nameof(pipeCommunicator));
            _autoRunManager = autoRunManager ?? throw new ArgumentNullException(nameof(autoRunManager));
            _excelProcessor = excelProcessor ?? throw new ArgumentNullException(nameof(excelProcessor));
            _emailRecipientManager = emailRecipientManager ?? throw new ArgumentNullException(nameof(emailRecipientManager));
            _greetingManager = greetingManager ?? throw new ArgumentNullException(nameof(greetingManager));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _statusManager = statusManager ?? throw new ArgumentNullException(nameof(statusManager));
            _appName = _configuration.GetValue<string>(AppConfigKeys.ApplicationInfo.AppName, "QCRA")!;
            _appVersion = _configuration.GetValue<string>(AppConfigKeys.ApplicationInfo.AppVersion, "1.0.0")!;

            InitializeComponent();

            // *** THIS IS THE FIX for CS1503 ***
            // The UIManager is instantiated without the main statusLabel, as it no longer manages it.
            _uiManager = new UIManager(this, menuStrip1, mainStatusStrip, autoRunStatusLabel,
                darkModeToolStripMenuItem, createReportButton, processEmailButton, oneClickProcessButton,
                toggleAutoRunButton, viewReportButton, viewAnalysisButton, reportTypeComboBox, startDatePicker,
                endDatePicker, financialYearComboBox, financialYearLabel, sendToFemiOnlyCheckBox,
                skipEmailCheckBox, emailRecipientLabel, toolTip1);

            _statusManager.StatusChanged += OnStatusChanged;
            _currentAutoRunHour = _configuration.GetValue<int>(AppConfigKeys.AutoRunProcess.CheckHour, 8);
            _uiManager.SetAutoRunHour(_currentAutoRunHour);
        }
        #endregion

        #region Status Event Handler and Interface Implementation
        /// <summary>
        /// Handles the StatusChanged event from the IStatusManagerService.
        /// This is now the ONLY place in the application that updates the main status label.
        /// </summary>
        private void OnStatusChanged(object sender, StatusPayload payload)
        {
            UIManager.SafeToolStripItemUpdate(statusLabel, () =>
            {
                statusLabel.Text = payload.Message;
                statusLabel.ForeColor = payload.Type switch
                {
                    MessageType.Success => Color.Green,
                    MessageType.Warning => Color.Goldenrod,
                    MessageType.Error => Color.Firebrick,
                    _ => ThemeSettings.CurrentPalette.StatusStripForeColor // Default color
                };
            });
        }

        /// <inheritdoc/>
        public void ReportAutoRunProgress(string message) => _statusManager.Post(message, MessageType.InProgress);

        /// <inheritdoc/>
        public void ReportAutoRunStatusRight(string message) => _uiManager.UpdateStatusRight(message);

        /// <inheritdoc/>
        public void SetControlsForAutoRunInProgress(bool inProgress) { if (inProgress) _uiManager.DisableControlsForAutoRun(); }

        /// <inheritdoc/>
        public void UpdateAutoRunButtonAndStatus(bool isTimerEnabled, bool isJobDoneOrFailedForToday, string statusTextToDisplay) => _uiManager.UpdateAutoRunUI(isTimerEnabled, isJobDoneOrFailedForToday, statusTextToDisplay);

        /// <inheritdoc/>
        public bool IsWindowsDarkModeEnabled() => ThemeSettings.IsWindowsDarkModeEnabled();
        #endregion

        #region Form Lifecycle and Main Action Handlers
        private async void Form1_Load(object sender, EventArgs e)
        {
            _statusManager.Post("Loading application...", MessageType.InProgress);
            BankHolidayHelper.Initialize();
            bool configValid = _reportPathService.IsEssentialPathConfigurationValid();
            Text = $"{_appName} - {(IsDebug ? "DEBUG" : "RELEASE")} - v{_appVersion}";
            StartPosition = FormStartPosition.CenterScreen;
            financialYearComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            reportTypeComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            if (!reportTypeComboBox.Items.Contains("Custom")) reportTypeComboBox.Items.Add("Custom");
            reportTypeComboBox.SelectedItem = "Daily";
            ThemeSettings.SyncThemeWithSystem();
            darkModeToolStripMenuItem.Checked = ThemeSettings.IsCurrentlyDark();
            _uiManager.ApplyTheme();
            UpdateAutoRunButtonAndStatus(dailyCheckTimer.Enabled, false, $"Auto Run: {(dailyCheckTimer.Enabled ? $"Enabled (Next check ~{_currentAutoRunHour}:00)" : "Disabled")}");
            reportTypeComboBox_SelectedIndexChanged(reportTypeComboBox, EventArgs.Empty);
            _uiManager.ResetButtonStatesAfterTypeChange(configValid);
            Update1ClickProcessingModeUI();
            if (!configValid) _statusManager.Post("Config Error: Check Options menu.", MessageType.Error);
            _statusManager.Post("Checking report service...", MessageType.InProgress);
            bool wrapperOk = await _processManager.EnsureWrapperIsRunningAsync(new Progress<string>(status => _statusManager.Post(status, MessageType.InProgress)));
            if (!wrapperOk && configValid) _statusManager.Post("Report service failed to start. Report generation may fail.", MessageType.Warning);
            _ = Task.Run(() => ReportArchiver.ArchiveOldReportsAsync(_reportPathService.FinalReportOutputBaseDirectory, _reportPathService.RawReportExportBaseDirectory, _configuration.GetValue<int?>(AppConfigKeys.OperationalParameters.ArchiveRawReportsOlderThanDays), _configuration.GetValue<string>(AppConfigKeys.OperationalParameters.ReportArchiveFolderName)));
            if (configValid && wrapperOk) _statusManager.Clear();
            else if (configValid && !wrapperOk) _statusManager.Post("Ready (Report Service Issue)", MessageType.Warning);
            else _statusManager.Post("Config Error (Service Check Skipped)", MessageType.Error);
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            dailyCheckTimer.Stop();
            _processManager.TerminateWrapperProcess();
            _statusManager.StatusChanged -= OnStatusChanged;
        }

        private async void createReportButton_Click(object sender, EventArgs e)
        {
            if (!ValidateInputDates() || !ValidateFinancialYearSelection()) { _uiManager.ResetUIOnError("Create Report", _reportPathService.IsEssentialPathConfigurationValid(), File.Exists(_lastGeneratedRawReportPath), File.Exists(_lastGeneratedAnalysisFilePath), IsAnyDailySelected(), dailyCheckTimer.Enabled, false, _uiManager.GetAutoRunStatusLabelText()); return; }
            _statusManager.Post("Requesting raw report...", MessageType.InProgress);
            _uiManager.SetActionButtonsEnabled(false);
            _uiManager.SetOtherControlsEnabled(false, financialYearComboBox.Visible);
            var parameters = GatherManualReportParameters();
            using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(_configuration.GetValue<int>(AppConfigKeys.OperationalParameters.ProcessTimeoutMinutes, 6)));
            ReportCreationResult result = await _manualReportOrchestrator.CreateRawReportAsync(parameters, cts.Token);
            if (result.Success && !string.IsNullOrEmpty(result.GeneratedRawPath))
            {
                _lastGeneratedRawReportPath = result.GeneratedRawPath;
                _lastGeneratedAnalysisFilePath = null;
                _uiManager.ShowViewReportButton(true, _lastGeneratedRawReportPath);
                _uiManager.ShowViewAnalysisButton(false);
                _statusManager.Post("Raw report created successfully.", MessageType.Success, TimeSpan.FromSeconds(5));
                UIManager.SafeControlUpdate(createReportButton, () => createReportButton.Text = "Report Created");
                UIManager.SafeControlUpdate(processEmailButton, () => processEmailButton.Enabled = true);
            }
            else
            {
                _lastGeneratedRawReportPath = null;
                FlexibleMessageBox.Show(this, result.ErrorMessage ?? "Raw report creation failed for an unknown reason.", "Report Creation Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                _statusManager.Post(result.ErrorMessage ?? "Raw report creation failed.", MessageType.Error);
            }
            if (!oneClickProcessButton.Visible || !result.Success)
            {
                _uiManager.ResetUIOnError("Create Report", _reportPathService.IsEssentialPathConfigurationValid(), File.Exists(_lastGeneratedRawReportPath), File.Exists(_lastGeneratedAnalysisFilePath), IsAnyDailySelected(), dailyCheckTimer.Enabled, false, _uiManager.GetAutoRunStatusLabelText());
            }
        }

        private async void processEmailButton_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_lastGeneratedRawReportPath) || !File.Exists(_lastGeneratedRawReportPath)) { FlexibleMessageBox.Show(this, "The raw report file has not been generated or cannot be found. Please create the report first.", "Raw Report Missing", MessageBoxButtons.OK, MessageBoxIcon.Warning); _uiManager.ResetUIOnError("Create Report", _reportPathService.IsEssentialPathConfigurationValid(), false, File.Exists(_lastGeneratedAnalysisFilePath), IsAnyDailySelected(), dailyCheckTimer.Enabled, false, _uiManager.GetAutoRunStatusLabelText()); return; }
            if (!ValidateInputDates() || !ValidateFinancialYearSelection()) { _uiManager.ResetUIOnError("Process & Email", _reportPathService.IsEssentialPathConfigurationValid(), File.Exists(_lastGeneratedRawReportPath), File.Exists(_lastGeneratedAnalysisFilePath), IsAnyDailySelected(), dailyCheckTimer.Enabled, false, _uiManager.GetAutoRunStatusLabelText()); return; }
            _statusManager.Post("Processing report and preparing email...", MessageType.InProgress);
            _uiManager.SetActionButtonsEnabled(false);
            _uiManager.SetOtherControlsEnabled(false, financialYearComboBox.Visible);
            var parameters = GatherManualReportParameters();
            using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(_configuration.GetValue<int>(AppConfigKeys.OperationalParameters.ProcessTimeoutMinutes, 15)));
            ReportProcessingResult result = await _manualReportOrchestrator.ProcessAndEmailReportAsync(_lastGeneratedRawReportPath, parameters, cts.Token);
            await HandleReportProcessingResult(result, parameters, cts.Token);
        }

        private async void oneClickProcessButton_Click(object sender, EventArgs e)
        {
            if (!ValidateInputDates() || !ValidateFinancialYearSelection()) { _uiManager.ResetUIOnError("Generate, Process & Email Report", _reportPathService.IsEssentialPathConfigurationValid(), File.Exists(_lastGeneratedRawReportPath), File.Exists(_lastGeneratedAnalysisFilePath), IsAnyDailySelected(), dailyCheckTimer.Enabled, false, _uiManager.GetAutoRunStatusLabelText()); return; }
            _statusManager.Post("1-Click Process: Starting...", MessageType.InProgress);
            _uiManager.SetActionButtonsEnabled(false);
            _uiManager.SetOtherControlsEnabled(false, financialYearComboBox.Visible);
            var parameters = GatherManualReportParameters();
            using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(_configuration.GetValue<int>(AppConfigKeys.OperationalParameters.ProcessTimeoutMinutes, 15)));
            ReportCreationResult creationResult = await _manualReportOrchestrator.CreateRawReportAsync(parameters, cts.Token);
            if (!creationResult.Success || string.IsNullOrEmpty(creationResult.GeneratedRawPath))
            {
                _lastGeneratedRawReportPath = null;
                FlexibleMessageBox.Show(this, creationResult.ErrorMessage ?? "Raw report creation failed in 1-Click process.", "1-Click Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                _uiManager.ResetUIOnError("Generate, Process & Email Report", _reportPathService.IsEssentialPathConfigurationValid(), false, File.Exists(_lastGeneratedAnalysisFilePath), IsAnyDailySelected(), dailyCheckTimer.Enabled, false, _uiManager.GetAutoRunStatusLabelText());
                return;
            }
            _lastGeneratedRawReportPath = creationResult.GeneratedRawPath;
            _uiManager.ShowViewReportButton(true, _lastGeneratedRawReportPath);
            _statusManager.Post("1-Click: Raw report created. Processing...", MessageType.InProgress);
            ReportProcessingResult processingResult = await _manualReportOrchestrator.ProcessAndEmailReportAsync(_lastGeneratedRawReportPath, parameters, cts.Token);
            await HandleReportProcessingResult(processingResult, parameters, cts.Token);
        }

        private async Task HandleReportProcessingResult(ReportProcessingResult result, ManualReportParameters parameters, CancellationToken originalCts)
        {
            if (result.Success)
            {
                _lastGeneratedAnalysisFilePath = result.GeneratedAnalysisPath;
                _uiManager.ShowViewAnalysisButton(true, _lastGeneratedAnalysisFilePath);
                if (result.ManualRefreshRequired)
                {
                    _statusManager.Post("Manual Excel refresh needed. Please follow prompts.", MessageType.Warning);
                    bool userConfirmedEmail = await HandleManualExcelRefreshInteractionAsync(result.GeneratedAnalysisPath!, parameters, originalCts);
                    if (userConfirmedEmail && !parameters.SkipEmail)
                    {
                        _statusManager.Post("Sending email after manual refresh...", MessageType.InProgress);
                        EmailSendResult emailResult = await _manualReportOrchestrator.SendEmailAfterManualRefreshAsync(result.GeneratedAnalysisPath!, parameters, originalCts);
                        if (emailResult.Success) { _statusManager.Post("Process completed successfully.", MessageType.Success, TimeSpan.FromSeconds(5)); }
                        else { FlexibleMessageBox.Show(this, emailResult.ErrorMessage ?? "Email sending failed after manual refresh.", "Email Error", MessageBoxButtons.OK, MessageBoxIcon.Error); _statusManager.Post(emailResult.ErrorMessage ?? "Email sending failed.", MessageType.Error); }
                    }
                    else if (!userConfirmedEmail) { _statusManager.Post("Email sending cancelled by user.", MessageType.Warning, TimeSpan.FromSeconds(5)); }
                    else { _statusManager.Post("Process completed successfully (email skipped).", MessageType.Success, TimeSpan.FromSeconds(5)); }
                }
                else if (result.EmailResult?.Success == true || parameters.SkipEmail) { _statusManager.Post("Process completed successfully.", MessageType.Success, TimeSpan.FromSeconds(5)); }
                else if (result.EmailResult?.Success == false) { FlexibleMessageBox.Show(this, result.EmailResult.ErrorMessage ?? "Email sending failed.", "Email Error", MessageBoxButtons.OK, MessageBoxIcon.Error); _statusManager.Post(result.EmailResult.ErrorMessage ?? "Email sending failed.", MessageType.Error); }
                else { _statusManager.Post("Processing complete. Email status unknown.", MessageType.Warning, TimeSpan.FromSeconds(5)); }
            }
            else
            {
                _lastGeneratedAnalysisFilePath = null;
                FlexibleMessageBox.Show(this, result.ErrorMessage ?? "Report processing failed for an unknown reason.", "Processing Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                _statusManager.Post(result.ErrorMessage ?? "Report processing failed.", MessageType.Error);
            }
            _uiManager.ResetUIOnError(oneClickProcessButton.Visible ? "Generate, Process & Email Report" : "Create Report", _reportPathService.IsEssentialPathConfigurationValid(), File.Exists(_lastGeneratedRawReportPath), File.Exists(_lastGeneratedAnalysisFilePath), IsAnyDailySelected(), dailyCheckTimer.Enabled, false, _uiManager.GetAutoRunStatusLabelText());
        }

        private async Task<bool> HandleManualExcelRefreshInteractionAsync(string filePath, ManualReportParameters originalParameters, CancellationToken cancellationToken)
        {
            _statusManager.Post("Checking for running Excel instances...", MessageType.InProgress);
            if (Process.GetProcessesByName("EXCEL").Any())
            {
                DialogResult closeExcelResult = FlexibleMessageBox.Show(this, "Other Excel instances are running. It's recommended to close them before proceeding with the manual refresh to avoid conflicts.\n\nAttempt to close other Excel instances automatically?", "Close Other Excel Instances?", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning);
                if (closeExcelResult == DialogResult.Cancel) { _statusManager.Post("Manual refresh cancelled by user.", MessageType.Warning, TimeSpan.FromSeconds(5)); return false; }
                if (closeExcelResult == DialogResult.Yes) { _statusManager.Post("Attempting to close other Excel instances...", MessageType.InProgress); await Task.Run(() => ReportHelper.CloseProcessesByName("EXCEL"), cancellationToken); await Task.Delay(1500, cancellationToken); }
            }
            FlexibleMessageBox.Show(this, "The report will now open in Excel.\n\n*** IMPORTANT ***\n1. Enable Editing if prompted by Excel.\n2. Go to the Pivot sheets and right-click each Table and Slicer > 'Refresh'.\n3. Ensure all PivotTables and data connections are updated.\n4. SAVE the file.\n5. CLOSE Excel.\n\nThe application will wait for you to close Excel before continuing.", "Manual Refresh Required", MessageBoxButtons.OK, MessageBoxIcon.Information);
            cancellationToken.ThrowIfCancellationRequested();
            _statusManager.Post("Opening Excel for manual refresh...", MessageType.InProgress);
            Process? excelProc = null;
            try
            {
                excelProc = await Task.Run(() => Process.Start(new ProcessStartInfo(filePath) { UseShellExecute = true }), cancellationToken);
                if (excelProc == null) { FlexibleMessageBox.Show(this, "Failed to start Excel. Ensure it's installed and .xlsx files are associated.", "Excel Error", MessageBoxButtons.OK, MessageBoxIcon.Error); return false; }
                _statusManager.Post("Excel opened. Waiting for user to Refresh, Save, and Close Excel...", MessageType.Info);
                await excelProc.WaitForExitAsync(cancellationToken);
                _statusManager.Post("Excel closed by user.", MessageType.Success, TimeSpan.FromSeconds(5));
                if (originalParameters.SkipEmail) return false;
                DialogResult sendResult = FlexibleMessageBox.Show(this, "Excel has been closed.\n\nProceed with sending the email?", "Confirm Email Send", MessageBoxButtons.OKCancel, MessageBoxIcon.Question);
                return (sendResult == DialogResult.OK || sendResult == DialogResult.Yes);
            }
            catch (OperationCanceledException) { _statusManager.Post("Manual refresh cancelled.", MessageType.Warning, TimeSpan.FromSeconds(5)); if (excelProc != null && !excelProc.HasExited) { try { excelProc.Kill(true); } catch (Exception killEx) { Logger.LogWarning($"Could not kill Excel process during cancellation: {killEx.Message}"); } } return false; }
            catch (Exception ex) { Logger.LogError($"Error during manual Excel handling: {ex.Message}", ex); FlexibleMessageBox.Show(this, $"An unexpected error occurred managing the Excel refresh step:\n\n{ex.Message}", "Excel Interaction Error", MessageBoxButtons.OK, MessageBoxIcon.Error); return false; }
            finally { excelProc?.Dispose(); }
        }

        private void viewReportButton_Click(object sender, EventArgs e) { if (!string.IsNullOrEmpty(_lastGeneratedRawReportPath)) { try { ReportHelper.OpenFileWithDefaultApp(_lastGeneratedRawReportPath, "raw report output"); } catch (Exception ex) { FlexibleMessageBox.Show(this, ex.Message, "Error Opening File", MessageBoxButtons.OK, MessageBoxIcon.Error); } } else { FlexibleMessageBox.Show(this, "No raw report has been generated yet in this session.", "File Not Available", MessageBoxButtons.OK, MessageBoxIcon.Information); } }
        private void viewAnalysisButton_Click(object sender, EventArgs e) { if (!string.IsNullOrEmpty(_lastGeneratedAnalysisFilePath)) { try { ReportHelper.OpenFileWithDefaultApp(_lastGeneratedAnalysisFilePath, "processed analysis file"); } catch (Exception ex) { FlexibleMessageBox.Show(this, ex.Message, "Error Opening File", MessageBoxButtons.OK, MessageBoxIcon.Error); } } else { FlexibleMessageBox.Show(this, "No analysis file has been generated or successfully processed yet in this session.", "File Not Available", MessageBoxButtons.OK, MessageBoxIcon.Information); } }
        #endregion

        #region UI Event Handlers (ComboBoxes, DatePickers, MenuItems)
        private void reportTypeComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (sender is not ComboBox comboBox || comboBox.SelectedItem == null) return;
            ReportType selectedReportType = GetSelectedReportType();
            if (selectedReportType == ReportType.Custom)
            {
                UIManager.SafeControlUpdate(sendToFemiOnlyCheckBox, () => { sendToFemiOnlyCheckBox.Visible = true; });
                UIManager.SafeControlUpdate(emailRecipientLabel, () => { emailRecipientLabel.Visible = false; });
                _uiManager.ResetButtonStatesAfterTypeChange(_reportPathService.IsEssentialPathConfigurationValid());
                Update1ClickProcessingModeUI();
                return;
            }
            DateTime todayValue = DateTime.Today;
            _programmaticallyChangingDates = true;
            try
            {
                // *** THIS IS THE FIX for CS8130, CS8131, CS8506 ***
                // The switch expression is explicitly typed to (DateTime, DateTime, bool)
                // and all branches now return this same tuple type.
                (DateTime dateFrom, DateTime dateTo, bool showFinYear) = selectedReportType switch
                {
                    ReportType.Daily => (ReportHelper.GetPreviousWorkday(todayValue), ReportHelper.GetPreviousWorkday(todayValue), false),
                    ReportType.Daily5Day1k => (ReportHelper.GetNthPreviousWorkday(ReportHelper.GetPreviousWorkday(todayValue), 4), ReportHelper.GetPreviousWorkday(todayValue), false),
                    ReportType.Weekly => (todayValue.AddDays(-14), todayValue, true),
                    ReportType.Monthly => (ReportHelper.CalculateMonthlyRange(todayValue).DateFrom, ReportHelper.CalculateMonthlyRange(todayValue).DateTo, false),
                    ReportType.Quarterly => (ReportHelper.CalculateQuarterlyRange(todayValue).DateFrom, ReportHelper.CalculateQuarterlyRange(todayValue).DateTo, false),
                    ReportType.Annual => (ReportHelper.GetFinancialYearDates(ReportHelper.GetFinancialYearStartCalendarYear(todayValue, _configuration) - 1, _configuration).DateFrom, ReportHelper.GetFinancialYearDates(ReportHelper.GetFinancialYearStartCalendarYear(todayValue, _configuration) - 1, _configuration).DateTo, false),
                    _ => (startDatePicker.Value, endDatePicker.Value, true)
                };
                UIManager.SafeControlUpdate(startDatePicker, () => { startDatePicker.Value = dateFrom; });
                UIManager.SafeControlUpdate(endDatePicker, () => { endDatePicker.Value = dateTo; });
                UIManager.SafeControlUpdate(financialYearLabel, () => { financialYearLabel.Visible = showFinYear; });
                UIManager.SafeControlUpdate(financialYearComboBox, () => { financialYearComboBox.Visible = showFinYear; financialYearComboBox.Enabled = showFinYear; if (showFinYear) PopulateFinancialYearDropdown(); });
                bool isAnyDaily = IsAnyDailySelected();
                UIManager.SafeControlUpdate(sendToFemiOnlyCheckBox, () => { sendToFemiOnlyCheckBox.Visible = !isAnyDaily && selectedReportType != ReportType.Custom; });
                UIManager.SafeControlUpdate(emailRecipientLabel, () => { emailRecipientLabel.Visible = isAnyDaily; if (isAnyDaily) emailRecipientLabel.Text = selectedReportType == ReportType.Daily ? "Manual Daily: Uses configured list." : "Daily (5d>=1k): Femi/Team (manual) or Auto (config)."; });
                _uiManager.ResetButtonStatesAfterTypeChange(_reportPathService.IsEssentialPathConfigurationValid());
                Update1ClickProcessingModeUI();
            }
            finally { _programmaticallyChangingDates = false; }
        }

        private void DatePicker_ValueChanged(object sender, EventArgs e) { if (_programmaticallyChangingDates) return; if (GetSelectedReportType() != ReportType.Custom) { UIManager.SafeControlUpdate(reportTypeComboBox, () => { reportTypeComboBox.SelectedItem = "Custom"; }); } }
        private void darkModeToolStripMenuItem_Click(object sender, EventArgs e) { ThemeSettings.CurrentThemeMode = darkModeToolStripMenuItem.Checked ? ApplicationThemeMode.Dark : ApplicationThemeMode.Light; _uiManager.ApplyTheme(); }
        private void settingsToolStripMenuItem_Click(object? sender, EventArgs e) { using var settingsFormInstance = _serviceProvider.GetRequiredService<SettingsForm>(); if (settingsFormInstance.ShowDialog(this) == DialogResult.OK) { if (_configuration is IConfigurationRoot configurationRoot) { configurationRoot.Reload(); FlexibleMessageBox.Show(this, "Settings saved and configuration reloaded.\nA restart may be needed for some changes to fully apply.", "Settings Saved", MessageBoxButtons.OK, MessageBoxIcon.Information); ReinitializeConfigurableComponents(); } else { FlexibleMessageBox.Show(this, "Settings saved. Please restart the application for changes to take effect.", "Settings Saved - Restart Required", MessageBoxButtons.OK, MessageBoxIcon.Warning); } } }
        private void helpToolStripMenuItem_Click(object? sender, EventArgs e) { string helpTitle = ReportHelper.GetHelpTitle(_appName, _appVersion); string helpContent = ReportHelper.GetHelpContent(_configuration, _appName, _appVersion); try { if (_helpFormInstance == null || _helpFormInstance.IsDisposed) { _helpFormInstance = new HelpForm(helpTitle, helpContent); _helpFormInstance.FormClosed += (s, args) => _helpFormInstance = null; _helpFormInstance.Show(this); } else { _helpFormInstance.Activate(); } } catch (Exception ex) { Logger.LogError($"Failed to show HelpForm: {ex.Message}", ex); FlexibleMessageBox.Show(this, "Could not display help window. Please check application logs.", "Help Error", MessageBoxButtons.OK, MessageBoxIcon.Error); } }
        private void viewConfigToolStripMenuItem_Click(object sender, EventArgs e) { bool configValid = _reportPathService.IsEssentialPathConfigurationValid(); var sb = new StringBuilder(); sb.AppendLine("Configuration Details (Paths are relative to user profile where applicable):").AppendLine("--------------------------------------------------").AppendLine($"1. Crystal Report Path (.rpt): '{_reportPathService.CrystalReportRptFilePath}' - Exists: {File.Exists(_reportPathService.CrystalReportRptFilePath)}").AppendLine($"2. Wrapper EXE Path: '{_reportPathService.WrapperExecutablePath}' - Exists: {File.Exists(_reportPathService.WrapperExecutablePath)}").AppendLine($"3. Template Base Directory: '{_reportPathService.TemplateBaseDirectory}' - Exists: {Directory.Exists(_reportPathService.TemplateBaseDirectory)}").AppendLine($"4. Raw Report Export Base Directory: '{_reportPathService.RawReportExportBaseDirectory}' - Exists: {Directory.Exists(_reportPathService.RawReportExportBaseDirectory)}").AppendLine($"5. Final Excel Save Location Base: '{_reportPathService.FinalReportOutputBaseDirectory}' - Exists: {Directory.Exists(_reportPathService.FinalReportOutputBaseDirectory)}").AppendLine($"6. Auto-Run Check Hour: {_configuration.GetValue<int>(AppConfigKeys.AutoRunProcess.CheckHour, _currentAutoRunHour)} (Current in-memory: {_currentAutoRunHour})").AppendLine($"7. Automated Report Definitions File: '{_reportPathService.GetReportDefinitionsFilePath() ?? "N/A"}' - Exists: {File.Exists(_reportPathService.GetReportDefinitionsFilePath() ?? string.Empty)}").AppendLine($"8. Application Log Directory (User Specific): '{_reportPathService.GetUserSpecificLogDirectory()}' - Exists: {Directory.Exists(_reportPathService.GetUserSpecificLogDirectory())}").AppendLine($"9. appsettings.json Directory: '{_reportPathService.AppSettingsDirectory}' - appsettings.json Exists: {File.Exists(Path.Combine(_reportPathService.AppSettingsDirectory, "appsettings.json"))}").AppendLine("--------------------------------------------------").AppendLine($"Overall Essential Config Valid (for report generation): {configValid}"); FlexibleMessageBox.Show(this, sb.ToString(), "Configuration Details", MessageBoxButtons.OK, configValid ? MessageBoxIcon.Information : MessageBoxIcon.Warning); }
        private void validateConfigToolStripMenuItem_Click(object sender, EventArgs e) { _statusManager.Post("Validating configuration...", MessageType.InProgress); bool isValid = _reportPathService.IsEssentialPathConfigurationValid(); string statusMessage = isValid ? "Configuration OK." : "Configuration Error: Essential paths missing or invalid."; MessageType type = isValid ? MessageType.Success : MessageType.Error; _statusManager.Post(statusMessage, type, TimeSpan.FromSeconds(5)); if (!isValid) Logger.LogError("Configuration validation failed."); }
        private void openLogsToolStripMenuItem_Click(object sender, EventArgs e) { try { string userLogDir = _reportPathService.GetUserSpecificLogDirectory(); if (!Directory.Exists(userLogDir)) Directory.CreateDirectory(userLogDir); Process.Start("explorer.exe", userLogDir); } catch (Exception ex) { FlexibleMessageBox.Show(this, $"Could not open logs folder: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); } }
        private void editConfigToolStripMenuItem_Click(object sender, EventArgs e) { try { string appSettingsJsonPath = Path.Combine(_reportPathService.AppSettingsDirectory, "appsettings.json"); if (File.Exists(appSettingsJsonPath)) Process.Start(new ProcessStartInfo(appSettingsJsonPath) { UseShellExecute = true }); else FlexibleMessageBox.Show(this, $"appsettings.json not found at '{appSettingsJsonPath}'", "File Not Found", MessageBoxButtons.OK, MessageBoxIcon.Warning); } catch (Exception ex) { FlexibleMessageBox.Show(this, $"Could not open appsettings.json: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); } }
        private void exitToolStripMenuItem_Click(object sender, EventArgs e) => Close();
        private void manageCustomBankHolidaysToolStripMenuItem_Click(object sender, EventArgs e) { try { using var form = _serviceProvider.GetRequiredService<ManageBankHolidaysForm>(); form.ShowDialog(this); } catch (Exception ex) { Logger.LogError($"Error opening ManageBankHolidaysForm: {ex.Message}", ex); } }
        private void manageEmailRecipientsToolStripMenuItem_Click(object sender, EventArgs e) { try { using var form = _serviceProvider.GetRequiredService<ManageEmailRecipientsForm>(); form.ShowDialog(this); } catch (Exception ex) { Logger.LogError($"Error opening ManageEmailRecipientsForm: {ex.Message}", ex); } }
        private void manageGreetingsToolStripMenuItem_Click(object sender, EventArgs e) { try { using var form = _serviceProvider.GetRequiredService<ManageGreetingsForm>(); form.ShowDialog(this); } catch (Exception ex) { Logger.LogError($"Error opening ManageGreetingsForm: {ex.Message}", ex); } }
        private void enable1ClickProcessingToolStripMenuItem_Click(object sender, EventArgs e) { Update1ClickProcessingModeUI(); string mainButtonTextForReset = enable1ClickProcessingToolStripMenuItem.Checked ? (_reportPathService.IsEssentialPathConfigurationValid() ? "Generate, Process & Email Report" : "Config Error") : (_reportPathService.IsEssentialPathConfigurationValid() ? "Create Report" : "Config Error"); _uiManager.ResetUIOnError(mainButtonTextForReset, _reportPathService.IsEssentialPathConfigurationValid(), File.Exists(_lastGeneratedRawReportPath), File.Exists(_lastGeneratedAnalysisFilePath), IsAnyDailySelected(), dailyCheckTimer.Enabled, false, _uiManager.GetAutoRunStatusLabelText()); }
        private async void setAutoRunHourToolStripMenuItem_Click(object sender, EventArgs e) { string? inputText = Interaction.InputBox("Enter new hour (0-23) for daily auto-run check:", "Set Auto-Run Hour", _currentAutoRunHour.ToString()); if (int.TryParse(inputText, out int newHour) && newHour >= 0 && newHour <= 23) { if (newHour != _currentAutoRunHour && await _autoRunManager.SetAutoRunHourAsync(newHour)) { _currentAutoRunHour = newHour; _uiManager.SetAutoRunHour(_currentAutoRunHour); FlexibleMessageBox.Show(this, $"Auto-Run hour set to {newHour}:00.", "Auto-Run Hour Updated", MessageBoxButtons.OK, MessageBoxIcon.Information); _uiManager.UpdateAutoRunUI(dailyCheckTimer.Enabled, false, $"Auto Run: {(dailyCheckTimer.Enabled ? $"Enabled (Next check ~{_currentAutoRunHour}:00)" : "Disabled")}"); } } else if (!string.IsNullOrWhiteSpace(inputText)) { FlexibleMessageBox.Show(this, "Invalid hour. Enter 0-23.", "Invalid Input", MessageBoxButtons.OK, MessageBoxIcon.Warning); } }
        private void manageAutomatedReportsToolStripMenuItem_Click(object sender, EventArgs e) { try { using var form = _serviceProvider.GetRequiredService<ManageAutoReportDefinitionsForm>(); form.ShowDialog(this); _autoRunManager.ReloadReportDefinitions(); } catch (Exception ex) { Logger.LogError($"Error opening ManageAutoReportDefinitionsForm: {ex.Message}", ex); } }
        private void openAutoReportDefinitionsFileToolStripMenuItem_Click(object sender, EventArgs e) { try { string? filePath = _reportPathService.GetReportDefinitionsFilePath(); if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath)) Process.Start(new ProcessStartInfo(filePath) { UseShellExecute = true }); else FlexibleMessageBox.Show(this, $"Auto report definitions file not found at: {filePath ?? "N/A"}", "File Not Found", MessageBoxButtons.OK, MessageBoxIcon.Warning); } catch (Exception ex) { Logger.LogError($"Error opening auto report definitions file: {ex.Message}", ex); } }
        #endregion

        #region Auto-Run Timer Event Handler
        private async void dailyCheckTimer_Tick(object sender, EventArgs e)
        {
            if (!dailyCheckTimer.Enabled || _autoRunManager == null) return;
            bool originallyEnabled = dailyCheckTimer.Enabled;
            dailyCheckTimer.Stop();
            // *** THIS IS THE FIX for CS0165 ***
            // Initialise the local variable at declaration.
            AutoRunActionResult autoRunResult = AutoRunActionResult.NoActionNeeded;
            try
            {
                autoRunResult = await _autoRunManager.PerformDailyCheckAsync(originallyEnabled, _currentAutoRunHour);
            }
            catch (Exception ex)
            {
                Logger.LogCritical($"CRITICAL ERROR during AutoRunManager.PerformDailyCheckAsync dispatch: {ex.Message}", ex);
                _statusManager.Post("Critical AutoRun Error! Check Logs.", MessageType.Error);
                _uiManager.UpdateStatusRight("AutoRun: FAILED (Timer Error)");
                UpdateAutoRunButtonAndStatus(dailyCheckTimer.Enabled, true, "AutoRun: FAILED (Timer Error)");
                autoRunResult = AutoRunActionResult.CriticalError;
            }
            finally
            {
                if (originallyEnabled && autoRunResult != AutoRunActionResult.CriticalError) dailyCheckTimer.Start();
                if (autoRunResult == AutoRunActionResult.ActionAttempted || autoRunResult == AutoRunActionResult.CriticalError)
                {
                    _uiManager.ResetUIOnError(oneClickProcessButton.Visible ? "Generate, Process & Email Report" : "Create Report", _reportPathService.IsEssentialPathConfigurationValid(), File.Exists(_lastGeneratedRawReportPath), File.Exists(_lastGeneratedAnalysisFilePath), IsAnyDailySelected(), dailyCheckTimer.Enabled, autoRunResult == AutoRunActionResult.CriticalError, _uiManager.GetAutoRunStatusLabelText());
                }
            }
        }
        private void toggleAutoRunButton_Click(object sender, EventArgs e) { dailyCheckTimer.Enabled = !dailyCheckTimer.Enabled; string statusText = _uiManager.GetAutoRunStatusLabelText(); bool isAutoRunCompletedForToday = (statusText.Contains("Completed", StringComparison.OrdinalIgnoreCase)) || (statusText.Contains("Done for", StringComparison.OrdinalIgnoreCase)) || (statusText.Contains("FAILED", StringComparison.OrdinalIgnoreCase)); UpdateAutoRunButtonAndStatus(dailyCheckTimer.Enabled, isAutoRunCompletedForToday, string.Empty); Logger.LogInfo($"AutoRun timer {(dailyCheckTimer.Enabled ? "Enabled" : "Disabled")} by user."); }
        #endregion

        #region Helper Methods (Unchanged)
        private ManualReportParameters GatherManualReportParameters() => new() { StartDate = startDatePicker.Value, EndDate = endDatePicker.Value, ReportType = GetSelectedReportType(), FinancialYear = financialYearComboBox.Visible ? financialYearComboBox.SelectedItem?.ToString() : null, IsFemiOnlyChecked = sendToFemiOnlyCheckBox.Checked && sendToFemiOnlyCheckBox.Visible, SkipEmail = skipEmailCheckBox.Checked, ReportBaseName = "EstimateSuccessReport", IsDebugBuild = IsDebug };
        private ReportType GetSelectedReportType() { string? selectedText = null; UIManager.SafeControlUpdate(reportTypeComboBox, () => selectedText = reportTypeComboBox.SelectedItem?.ToString() ?? reportTypeComboBox.Text); return ReportTypeHelper.FromString(selectedText); }
        private void Update1ClickProcessingModeUI() { bool oneClickEnabled = enable1ClickProcessingToolStripMenuItem.Checked; UIManager.SafeControlUpdate(oneClickProcessButton, () => oneClickProcessButton.Visible = oneClickEnabled); UIManager.SafeControlUpdate(createReportButton, () => createReportButton.Visible = !oneClickEnabled); UIManager.SafeControlUpdate(processEmailButton, () => processEmailButton.Visible = !oneClickEnabled); if (oneClickEnabled && oneClickProcessButton != null) UIManager.SafeControlUpdate(oneClickProcessButton, () => oneClickProcessButton.BringToFront()); }
        private void PopulateFinancialYearDropdown() { UIManager.SafeControlUpdate(financialYearComboBox, () => { string? previouslySelected = financialYearComboBox.SelectedItem?.ToString(); financialYearComboBox.Items.Clear(); string currentFY = _excelProcessor.GetCurrentFinancialYear(true); if (!string.IsNullOrEmpty(currentFY)) { financialYearComboBox.Items.Add(currentFY); string? previousFY = _excelProcessor.GetPreviousFinancialYear(currentFY); if (!string.IsNullOrEmpty(previousFY)) financialYearComboBox.Items.Add(previousFY); } else { financialYearComboBox.Items.Add("FY Unknown"); } if (!string.IsNullOrEmpty(previouslySelected) && financialYearComboBox.Items.Contains(previouslySelected)) { financialYearComboBox.SelectedItem = previouslySelected; } else if (financialYearComboBox.Items.Count > 0) { financialYearComboBox.SelectedIndex = 0; } }); }
        private bool ValidateInputDates() { if (startDatePicker.Value.Date > endDatePicker.Value.Date) { FlexibleMessageBox.Show(this, "The 'From' date cannot be after the 'To' date.", "Date Range Error", MessageBoxButtons.OK, MessageBoxIcon.Error); return false; } return true; }
        private bool ValidateFinancialYearSelection() { if (!financialYearComboBox.Visible || financialYearComboBox.SelectedItem == null) return true; string selectedFinYear = financialYearComboBox.SelectedItem.ToString()!; if (!_excelProcessor.IsFinancialYearValid(selectedFinYear, startDatePicker.Value, endDatePicker.Value)) { DialogResult fdr = FlexibleMessageBox.Show(this, $"Date range ({startDatePicker.Value:d} - {endDatePicker.Value:d}) not in Financial Year ({selectedFinYear}).\nContinue?", "FY Mismatch Warning", MessageBoxButtons.YesNo, MessageBoxIcon.Warning); return fdr == DialogResult.Yes; } return true; }
        private bool IsAnyDailySelected() { ReportType selectedType = GetSelectedReportType(); return selectedType == ReportType.Daily || selectedType == ReportType.Daily5Day1k; }
        private void ReinitializeConfigurableComponents() { _appName = _configuration.GetValue<string>(AppConfigKeys.ApplicationInfo.AppName, "QCRA")!; _appVersion = _configuration.GetValue<string>(AppConfigKeys.ApplicationInfo.AppVersion, "1.0.0")!; this.Text = $"{_appName} - {(IsDebug ? "DEBUG" : "RELEASE")} - v{_appVersion}"; _currentAutoRunHour = _configuration.GetValue<int>(AppConfigKeys.AutoRunProcess.CheckHour, 8); _uiManager.SetAutoRunHour(_currentAutoRunHour); UpdateAutoRunButtonAndStatus(dailyCheckTimer.Enabled, false, $"Auto Run: {(dailyCheckTimer.Enabled ? $"Enabled (Next check ~{_currentAutoRunHour}:00)" : "Disabled")}"); bool configIsValid = _reportPathService.IsEssentialPathConfigurationValid(); _uiManager.ResetButtonStatesAfterTypeChange(configIsValid); Update1ClickProcessingModeUI(); }
        #endregion
    }
}