// Form1.cs
// Main application form, fully refactored to delegate business logic to specialised services.

#region Using Directives

// System related namespaces
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
using QuoteConversionReportAutomation.Services.Communication;
using QuoteConversionReportAutomation.Services.Interfaces;
using QuoteConversionReportAutomation.Services.Logging;
using QuoteConversionReportAutomation.Theming;
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

#endregion

namespace conversionTest
{
    #region Class Definition
    /// <summary>
    /// Represents the main form of the Quote Conversion Report Automation (QCRA) application.
    /// It serves as the primary user interface for manual report generation and for monitoring
    /// the automated report processes. This refactored version delegates business logic
    /// for date calculations and validation to dedicated services to adhere to the Single Responsibility Principle.
    /// </summary>
    public partial class Form1 : Form, IAutoRunUIContext
    {
        #region Fields and Properties

        // --- Injected Dependencies ---
        private readonly IConfiguration _configuration;
        private readonly IReportPathService _reportPathService;
        private readonly IManualReportOrchestrator _manualReportOrchestrator;
        private readonly IRetrospectiveAnalysisOrchestrator _retrospectiveAnalysisOrchestrator;
        private readonly IBatchRegenerationOrchestrator _batchRegenerator;
        private readonly AutoRunManager _autoRunManager;
        private readonly IServiceProvider _serviceProvider;
        private readonly IStatusManagerService _statusManager;
        private readonly UIManager _uiManager;
        private readonly IReportPeriodService _reportPeriodService;
        private readonly IFormValidationService _formValidationService;
        private readonly IFinancialYearService _financialYearService;

        // --- Form State ---
        private string _appName;
        private string _appVersion;
        private bool _programmaticallyChangingDates = false;
        private int _currentAutoRunHour;
        private HelpForm? _helpFormInstance;
        private string? _lastGeneratedRawReportPath = null;
        private string? _lastGeneratedAnalysisFilePath = null;

        /// <summary>
        /// Gets a value indicating whether the application is running in a DEBUG build configuration.
        /// </summary>
        private static bool IsDebug =>
#if DEBUG
            true;
#else
            false;
#endif
        #endregion

        #region Constructor

        /// <summary>
        /// Initialises a new instance of the <see cref="Form1"/> class, injecting all required dependencies.
        /// </summary>
        public Form1(
            IConfiguration configuration,
            IReportPathService reportPathService,
            IManualReportOrchestrator manualReportOrchestrator,
            IRetrospectiveAnalysisOrchestrator retrospectiveAnalysisOrchestrator,
            IBatchRegenerationOrchestrator batchRegenerator,
            AutoRunManager autoRunManager,
            IServiceProvider serviceProvider,
            IStatusManagerService statusManager,
            IReportPeriodService reportPeriodService,
            IFormValidationService formValidationService,
            IFinancialYearService financialYearService)
        {
            // Assign all injected dependencies.
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _reportPathService = reportPathService ?? throw new ArgumentNullException(nameof(reportPathService));
            _manualReportOrchestrator = manualReportOrchestrator ?? throw new ArgumentNullException(nameof(manualReportOrchestrator));
            _retrospectiveAnalysisOrchestrator = retrospectiveAnalysisOrchestrator ?? throw new ArgumentNullException(nameof(retrospectiveAnalysisOrchestrator));
            _batchRegenerator = batchRegenerator ?? throw new ArgumentNullException(nameof(batchRegenerator));
            _autoRunManager = autoRunManager ?? throw new ArgumentNullException(nameof(autoRunManager));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _statusManager = statusManager ?? throw new ArgumentNullException(nameof(statusManager));
            _reportPeriodService = reportPeriodService ?? throw new ArgumentNullException(nameof(reportPeriodService));
            _formValidationService = formValidationService ?? throw new ArgumentNullException(nameof(formValidationService));
            _financialYearService = financialYearService ?? throw new ArgumentNullException(nameof(financialYearService));

            // Initialise form state from configuration.
            _appName = _configuration.GetValue<string>(AppConfigKeys.ApplicationInfo.AppName, "QCRA")!;
            _appVersion = _configuration.GetValue<string>(AppConfigKeys.ApplicationInfo.AppVersion, "1.0.0")!;

            // Standard Windows Forms initialisation.
            InitializeComponent();

            // Initialise the UI Manager with references to all the controls it needs to manage.
            _uiManager = new UIManager(this, menuStrip1, mainStatusStrip, autoRunStatusLabel,
                darkModeToolStripMenuItem, createReportButton, processEmailButton, oneClickProcessButton,
                toggleAutoRunButton, viewReportButton, viewAnalysisButton, reportTypeComboBox, startDatePicker,
                endDatePicker, financialYearComboBox, financialYearLabel, sendToFemiOnlyCheckBox,
                skipEmailCheckBox, chkIncludeLeadTimeAnalysis, emailRecipientLabel, toolTip1);

            // Subscribe to the status manager's event to update the UI.
            _statusManager.StatusChanged += OnStatusChanged;

            // Set the initial auto-run hour from configuration.
            _currentAutoRunHour = _configuration.GetValue<int>(AppConfigKeys.AutoRunProcess.CheckHour, 8);
            _uiManager.SetAutoRunHour(_currentAutoRunHour);
        }

        #endregion

        #region Status Event Handler and UI Context Implementation

        /// <summary>
        /// Handles the StatusChanged event from the IStatusManagerService.
        /// This is the single point of entry for updating the main status label on the UI thread.
        /// </summary>
        private void OnStatusChanged(object? sender, StatusPayload payload)
        {
            // Use the UIManager's safe update method to marshal the call to the UI thread if necessary.
            UIManager.SafeToolStripItemUpdate(statusLabel, () =>
            {
                statusLabel.Text = payload.Message;
                // Colour-code the status message based on its type.
                statusLabel.ForeColor = payload.Type switch
                {
                    MessageType.Success => Color.Green,
                    MessageType.Warning => Color.Goldenrod,
                    MessageType.Error => Color.Firebrick,
                    _ => ThemeSettings.CurrentPalette.StatusStripForeColor
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

        /// <summary>
        /// Handles the main form's Load event.
        /// </summary>
        private async void Form1_Load(object sender, EventArgs e)
        {
            _statusManager.Post("Loading application...", MessageType.InProgress);
            PopulateReportTypeComboBox();
            BankHolidayHelper.Initialize();
            bool configValid = _reportPathService.IsEssentialPathConfigurationValid();
            Text = $"{_appName} - {(IsDebug ? "DEBUG" : "RELEASE")} - v{_appVersion}";
            StartPosition = FormStartPosition.CenterScreen;
            ThemeSettings.SyncThemeWithSystem();
            darkModeToolStripMenuItem.Checked = ThemeSettings.IsCurrentlyDark();
            _uiManager.ApplyTheme();
            UpdateAutoRunButtonAndStatus(dailyCheckTimer.Enabled, false, string.Empty);
            if (reportTypeComboBox.Items.Count > 0)
            {
                reportTypeComboBox.SelectedIndex = 0;
            }
            _uiManager.ResetButtonStatesAfterTypeChange(configValid);
            Update1ClickProcessingModeUI();
            if (!configValid) _statusManager.Post("Config Error: Check Options menu.", MessageType.Error);

            // Launch a background task to archive old reports.
            _ = Task.Run(() => ReportArchiver.ArchiveOldReportsAsync(
                _reportPathService.FinalReportOutputBaseDirectory,
                _reportPathService.RawReportExportBaseDirectory,
                _configuration.GetValue<int?>(AppConfigKeys.OperationalParameters.ArchiveRawReportsOlderThanDays),
                _configuration.GetValue<string>(AppConfigKeys.OperationalParameters.ReportArchiveFolderName)));

            if (configValid) _statusManager.Clear();
            else _statusManager.Post("Config Error (Service Check Skipped)", MessageType.Error);
        }

        /// <summary>
        /// Handles the form's Closing event to shut down background processes.
        /// </summary>
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            dailyCheckTimer.Stop();
            // Terminate the external wrapper process to ensure a clean exit.
            ProcessHelper.CloseProcessesByName("CrystalReportWrapper");
            _statusManager.StatusChanged -= OnStatusChanged;
        }

        /// <summary>
        /// Handles the Click event for the "Create Report" button.
        /// </summary>
        private async void createReportButton_Click(object sender, EventArgs e)
        {
            // Delegate validation to the form validation service.
            if (!_formValidationService.ValidateInputDates(startDatePicker.Value, endDatePicker.Value, this) ||
                !_formValidationService.ValidateFinancialYearSelection(financialYearComboBox.Visible, financialYearComboBox.SelectedItem?.ToString(), startDatePicker.Value, endDatePicker.Value, this))
            {
                // Reset UI if validation fails.
                _uiManager.ResetUIOnError("Create Report", _reportPathService.IsEssentialPathConfigurationValid(), File.Exists(_lastGeneratedRawReportPath), File.Exists(_lastGeneratedAnalysisFilePath), IsAnyDailySelected(), dailyCheckTimer.Enabled, false, _uiManager.GetAutoRunStatusLabelText());
                return;
            }

            _statusManager.Post("Requesting raw report...", MessageType.InProgress);
            _uiManager.SetActionButtonsEnabled(false);
            _uiManager.SetOtherControlsEnabled(false, financialYearComboBox.Visible);

            var parameters = GatherManualReportParameters();
            using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(_configuration.GetValue<int>(AppConfigKeys.OperationalParameters.ProcessTimeoutMinutes, 6)));

            // Call the orchestrator to perform the action.
            ReportCreationResult result = await _manualReportOrchestrator.CreateRawReportAsync(parameters, cts.Token);

            // Handle the result of the operation.
            if (result.Success && !string.IsNullOrEmpty(result.GeneratedRawPath))
            {
                _lastGeneratedRawReportPath = result.GeneratedRawPath;
                _lastGeneratedAnalysisFilePath = null; // Clear previous analysis path.
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

            // Reset the UI unless this was part of a successful 1-Click process.
            if (!oneClickProcessButton.Visible || !result.Success)
            {
                _uiManager.ResetUIOnError("Create Report", _reportPathService.IsEssentialPathConfigurationValid(), File.Exists(_lastGeneratedRawReportPath), File.Exists(_lastGeneratedAnalysisFilePath), IsAnyDailySelected(), dailyCheckTimer.Enabled, false, _uiManager.GetAutoRunStatusLabelText());
            }
        }

        /// <summary>
        /// Handles the Click event for the "Process & Email" button.
        /// </summary>
        private async void processEmailButton_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_lastGeneratedRawReportPath) || !File.Exists(_lastGeneratedRawReportPath))
            {
                FlexibleMessageBox.Show(this, "The raw report file has not been generated or cannot be found. Please create the report first.", "Raw Report Missing", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _uiManager.ResetUIOnError("Create Report", _reportPathService.IsEssentialPathConfigurationValid(), false, File.Exists(_lastGeneratedAnalysisFilePath), IsAnyDailySelected(), dailyCheckTimer.Enabled, false, _uiManager.GetAutoRunStatusLabelText());
                return;
            }

            if (!_formValidationService.ValidateInputDates(startDatePicker.Value, endDatePicker.Value, this) ||
                !_formValidationService.ValidateFinancialYearSelection(financialYearComboBox.Visible, financialYearComboBox.SelectedItem?.ToString(), startDatePicker.Value, endDatePicker.Value, this))
            {
                _uiManager.ResetUIOnError("Process & Email", _reportPathService.IsEssentialPathConfigurationValid(), File.Exists(_lastGeneratedRawReportPath), File.Exists(_lastGeneratedAnalysisFilePath), IsAnyDailySelected(), dailyCheckTimer.Enabled, false, _uiManager.GetAutoRunStatusLabelText());
                return;
            }

            _statusManager.Post("Processing report and preparing email...", MessageType.InProgress);
            _uiManager.SetActionButtonsEnabled(false);
            _uiManager.SetOtherControlsEnabled(false, financialYearComboBox.Visible);

            var parameters = GatherManualReportParameters();
            using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(_configuration.GetValue<int>(AppConfigKeys.OperationalParameters.ProcessTimeoutMinutes, 15)));

            ReportProcessingResult result = await _manualReportOrchestrator.ProcessAndEmailReportAsync(_lastGeneratedRawReportPath, parameters, cts.Token);
            await HandleReportProcessingResult(result, parameters, cts.Token);
        }

        /// <summary>
        /// Handles the Click event for the "1-Click Process" button.
        /// </summary>
        private async void oneClickProcessButton_Click(object sender, EventArgs e)
        {
            if (!_formValidationService.ValidateInputDates(startDatePicker.Value, endDatePicker.Value, this) ||
                !_formValidationService.ValidateFinancialYearSelection(financialYearComboBox.Visible, financialYearComboBox.SelectedItem?.ToString(), startDatePicker.Value, endDatePicker.Value, this))
            {
                _uiManager.ResetUIOnError("Generate, Process & Email Report", _reportPathService.IsEssentialPathConfigurationValid(), File.Exists(_lastGeneratedRawReportPath), File.Exists(_lastGeneratedAnalysisFilePath), IsAnyDailySelected(), dailyCheckTimer.Enabled, false, _uiManager.GetAutoRunStatusLabelText());
                return;
            }

            _statusManager.Post("1-Click Process: Starting...", MessageType.InProgress);
            _uiManager.SetActionButtonsEnabled(false);
            _uiManager.SetOtherControlsEnabled(false, financialYearComboBox.Visible);

            var parameters = GatherManualReportParameters();
            using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(_configuration.GetValue<int>(AppConfigKeys.OperationalParameters.ProcessTimeoutMinutes, 15)));

            // Step 1: Create the raw report.
            ReportCreationResult creationResult = await _manualReportOrchestrator.CreateRawReportAsync(parameters, cts.Token);
            if (!creationResult.Success || string.IsNullOrEmpty(creationResult.GeneratedRawPath))
            {
                _lastGeneratedRawReportPath = null;
                FlexibleMessageBox.Show(this, creationResult.ErrorMessage ?? "Raw report creation failed in 1-Click process.", "1-Click Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                _uiManager.ResetUIOnError("Generate, Process & Email Report", _reportPathService.IsEssentialPathConfigurationValid(), false, File.Exists(_lastGeneratedAnalysisFilePath), IsAnyDailySelected(), dailyCheckTimer.Enabled, false, _uiManager.GetAutoRunStatusLabelText());
                return;
            }

            // Step 2: Process the created report.
            _lastGeneratedRawReportPath = creationResult.GeneratedRawPath;
            _uiManager.ShowViewReportButton(true, _lastGeneratedRawReportPath);
            _statusManager.Post("1-Click: Raw report created. Processing...", MessageType.InProgress);
            ReportProcessingResult processingResult = await _manualReportOrchestrator.ProcessAndEmailReportAsync(_lastGeneratedRawReportPath, parameters, cts.Token);
            await HandleReportProcessingResult(processingResult, parameters, cts.Token);
        }

        /// <summary>
        /// Handles the result of a report processing operation, updating UI and showing messages.
        /// </summary>
        private async Task HandleReportProcessingResult(ReportProcessingResult result, ManualReportParameters parameters, CancellationToken originalCts)
        {
            if (result.Success)
            {
                _lastGeneratedAnalysisFilePath = result.GeneratedAnalysisPath;
                _uiManager.ShowViewAnalysisButton(true, _lastGeneratedAnalysisFilePath);

                if (result.EmailResult?.Success == true || parameters.SkipEmail)
                {
                    _statusManager.Post("Process completed successfully.", MessageType.Success, TimeSpan.FromSeconds(5));
                }
                else if (result.EmailResult?.Success == false)
                {
                    FlexibleMessageBox.Show(this, result.EmailResult.ErrorMessage ?? "Email sending failed.", "Email Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    _statusManager.Post(result.EmailResult.ErrorMessage ?? "Email sending failed.", MessageType.Error);
                }
                else
                {
                    _statusManager.Post("Processing complete. Email status unknown.", MessageType.Warning, TimeSpan.FromSeconds(5));
                }
            }
            else
            {
                _lastGeneratedAnalysisFilePath = null;
                FlexibleMessageBox.Show(this, result.ErrorMessage ?? "Report processing failed for an unknown reason.", "Processing Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                _statusManager.Post(result.ErrorMessage ?? "Report processing failed.", MessageType.Error);
            }

            // Reset the UI after the operation is complete.
            _uiManager.ResetUIOnError(oneClickProcessButton.Visible ? "Generate, Process & Email Report" : "Create Report", _reportPathService.IsEssentialPathConfigurationValid(), File.Exists(_lastGeneratedRawReportPath), File.Exists(_lastGeneratedAnalysisFilePath), IsAnyDailySelected(), dailyCheckTimer.Enabled, false, _uiManager.GetAutoRunStatusLabelText());
        }

        /// <summary>
        /// Handles the Click event for the "View Raw Report" button.
        /// </summary>
        private void viewReportButton_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(_lastGeneratedRawReportPath))
            {
                try
                {
                    // Use the new, dedicated ProcessHelper.
                    ProcessHelper.OpenFileWithDefaultApp(_lastGeneratedRawReportPath, "raw report output");
                }
                catch (Exception ex)
                {
                    FlexibleMessageBox.Show(this, ex.Message, "Error Opening File", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                FlexibleMessageBox.Show(this, "No raw report has been generated yet in this session.", "File Not Available", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        /// <summary>
        /// Handles the Click event for the "View Processed Analysis" button.
        /// </summary>
        private void viewAnalysisButton_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(_lastGeneratedAnalysisFilePath))
            {
                try
                {
                    // Use the new, dedicated ProcessHelper.
                    ProcessHelper.OpenFileWithDefaultApp(_lastGeneratedAnalysisFilePath, "processed analysis file");
                }
                catch (Exception ex)
                {
                    FlexibleMessageBox.Show(this, ex.Message, "Error Opening File", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                FlexibleMessageBox.Show(this, "No analysis file has been generated or successfully processed yet in this session.", "File Not Available", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
        #endregion

        #region UI Event Handlers (ComboBoxes, DatePickers, MenuItems)

        /// <summary>
        /// Populates the report type combo box.
        /// </summary>
        private void PopulateReportTypeComboBox()
        {
            reportTypeComboBox.Items.Clear();
            foreach (ReportType type in Enum.GetValues(typeof(ReportType)))
            {
                if (type == ReportType.Unknown) continue;
                reportTypeComboBox.Items.Add(ReportTypeHelper.GetDisplayString(type, _configuration));
            }
            reportTypeComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        }

        /// <summary>
        /// Handles the SelectedIndexChanged event for the report type ComboBox.
        /// </summary>
        private void reportTypeComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (sender is not ComboBox comboBox || comboBox.SelectedItem == null) return;
            ReportType selectedReportType = GetSelectedReportType();

            if (selectedReportType == ReportType.Custom)
            {
                // For custom reports, no dates are calculated automatically.
                UIManager.SafeControlUpdate(sendToFemiOnlyCheckBox, () => { sendToFemiOnlyCheckBox.Visible = true; });
                UIManager.SafeControlUpdate(emailRecipientLabel, () => { emailRecipientLabel.Visible = false; });
                _uiManager.ResetButtonStatesAfterTypeChange(_reportPathService.IsEssentialPathConfigurationValid());
                Update1ClickProcessingModeUI();
                return;
            }

            _programmaticallyChangingDates = true;
            try
            {
                // --- MODIFIED: Delegate date calculation to the new service ---
                var (dateFrom, dateTo) = _reportPeriodService.GetPeriodForReportType(selectedReportType);

                UIManager.SafeControlUpdate(startDatePicker, () => { startDatePicker.Value = dateFrom; });
                UIManager.SafeControlUpdate(endDatePicker, () => { endDatePicker.Value = dateTo; });

                // Determine if the financial year control should be visible.
                bool showFinYear = selectedReportType is ReportType.Weekly or ReportType.Custom;
                UIManager.SafeControlUpdate(financialYearLabel, () => { financialYearLabel.Visible = showFinYear; });
                UIManager.SafeControlUpdate(financialYearComboBox, () =>
                {
                    financialYearComboBox.Visible = showFinYear;
                    financialYearComboBox.Enabled = showFinYear;
                    if (showFinYear) PopulateFinancialYearDropdown();
                });

                bool isStandardDailyOnly = selectedReportType == ReportType.Daily;
                UIManager.SafeControlUpdate(emailRecipientLabel, () =>
                {
                    emailRecipientLabel.Visible = isStandardDailyOnly;
                    if (isStandardDailyOnly) emailRecipientLabel.Text = "Manual Daily: Uses configured list.";
                });
                UIManager.SafeControlUpdate(sendToFemiOnlyCheckBox, () => { sendToFemiOnlyCheckBox.Visible = !isStandardDailyOnly; });

                _uiManager.ResetButtonStatesAfterTypeChange(_reportPathService.IsEssentialPathConfigurationValid());
                Update1ClickProcessingModeUI();
            }
            finally
            {
                _programmaticallyChangingDates = false;
            }
        }

        /// <summary>
        /// Handles date changes to switch the report type to "Custom".
        /// </summary>
        private void DatePicker_ValueChanged(object sender, EventArgs e)
        {
            if (_programmaticallyChangingDates) return;
            UIManager.SafeControlUpdate(reportTypeComboBox, () => { reportTypeComboBox.SelectedItem = ReportTypeHelper.GetDisplayString(ReportType.Custom, _configuration); });
        }

        /// <summary>
        /// Handles the Click event for the Dark Mode menu item.
        /// </summary>
        private void darkModeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ThemeSettings.CurrentThemeMode = darkModeToolStripMenuItem.Checked ? ApplicationThemeMode.Dark : ApplicationThemeMode.Light;
            _uiManager.ApplyTheme();
        }

        /// <summary>
        /// Handles the Click event for the main "Settings..." menu item.
        /// </summary>
        private void settingsToolStripMenuItem_Click(object? sender, EventArgs e)
        {
            using var settingsFormInstance = _serviceProvider.GetRequiredService<SettingsForm>();
            if (settingsFormInstance.ShowDialog(this) == DialogResult.OK)
            {
                if (_configuration is IConfigurationRoot configurationRoot)
                {
                    configurationRoot.Reload();
                    FlexibleMessageBox.Show(this, "Settings saved and configuration reloaded.", "Settings Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    ReinitialiseConfigurableComponents();
                }
                else
                {
                    FlexibleMessageBox.Show(this, "Settings saved. Please restart the application for changes to take effect.", "Settings Saved", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }

        /// <summary>
        /// Handles the Click event for the "Help" menu item.
        /// </summary>
        private void helpToolStripMenuItem_Click(object? sender, EventArgs e)
        {
            // Use the new, dedicated HelpContentHelper.
            string helpTitle = HelpContentHelper.GetHelpTitle(_appName, _appVersion);
            string helpContent = HelpContentHelper.GetHelpContent(_configuration, _appName, _appVersion);
            try
            {
                if (_helpFormInstance == null || _helpFormInstance.IsDisposed)
                {
                    _helpFormInstance = new HelpForm(helpTitle, helpContent);
                    _helpFormInstance.Show(this);
                }
                else
                {
                    _helpFormInstance.Activate();
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Failed to show HelpForm: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Handles the Click event for the "View Configuration" menu item.
        /// </summary>
        private void viewConfigToolStripMenuItem_Click(object sender, EventArgs e)
        {
            bool configValid = _reportPathService.IsEssentialPathConfigurationValid();
            var sb = new StringBuilder();
            sb.AppendLine("Configuration Details (Paths are relative to user profile where applicable):").AppendLine("--------------------------------------------------").AppendLine($"1. Crystal Report Path (.rpt): '{_reportPathService.CrystalReportRptFilePath}' - Exists: {File.Exists(_reportPathService.CrystalReportRptFilePath)}").AppendLine($"2. Wrapper EXE Path: '{_reportPathService.WrapperExecutablePath}' - Exists: {File.Exists(_reportPathService.WrapperExecutablePath)}").AppendLine($"3. Template Base Directory: '{_reportPathService.TemplateBaseDirectory}' - Exists: {Directory.Exists(_reportPathService.TemplateBaseDirectory)}").AppendLine($"4. Raw Report Export Base Directory: '{_reportPathService.RawReportExportBaseDirectory}' - Exists: {Directory.Exists(_reportPathService.RawReportExportBaseDirectory)}").AppendLine($"5. Final Excel Save Location Base: '{_reportPathService.FinalReportOutputBaseDirectory}' - Exists: {Directory.Exists(_reportPathService.FinalReportOutputBaseDirectory)}").AppendLine($"6. Auto-Run Check Hour: {_configuration.GetValue<int>(AppConfigKeys.AutoRunProcess.CheckHour, _currentAutoRunHour)} (Current in-memory: {_currentAutoRunHour})").AppendLine($"7. Automated Report Definitions File: '{_reportPathService.GetReportDefinitionsFilePath() ?? "N/A"}' - Exists: {File.Exists(_reportPathService.GetReportDefinitionsFilePath() ?? string.Empty)}").AppendLine($"8. Application Log Directory (User Specific): '{_reportPathService.GetUserSpecificLogDirectory()}' - Exists: {Directory.Exists(_reportPathService.GetUserSpecificLogDirectory())}").AppendLine($"9. appsettings.json Directory: '{_reportPathService.AppSettingsDirectory}' - appsettings.json Exists: {File.Exists(Path.Combine(_reportPathService.AppSettingsDirectory, "appsettings.json"))}").AppendLine("--------------------------------------------------").AppendLine($"Overall Essential Config Valid (for report generation): {configValid}");
            FlexibleMessageBox.Show(this, sb.ToString(), "Configuration Details", MessageBoxButtons.OK, configValid ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
        }

        /// <summary>
        /// Handles the Click event for the "Validate Configuration" menu item.
        /// </summary>
        private void validateConfigToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _statusManager.Post("Validating configuration...", MessageType.InProgress);
            bool isValid = _reportPathService.IsEssentialPathConfigurationValid();
            string statusMessage = isValid ? "Configuration OK." : "Configuration Error: Essential paths missing or invalid.";
            MessageType type = isValid ? MessageType.Success : MessageType.Error;
            _statusManager.Post(statusMessage, type, TimeSpan.FromSeconds(5));
            if (!isValid) Logger.LogError("Configuration validation failed.");
        }

        /// <summary>
        /// Handles the Click event for the "Open Logs" menu item.
        /// </summary>
        private void openLogsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                string userLogDir = _reportPathService.GetUserSpecificLogDirectory();
                if (!Directory.Exists(userLogDir)) Directory.CreateDirectory(userLogDir);
                Process.Start("explorer.exe", userLogDir);
            }
            catch (Exception ex)
            {
                FlexibleMessageBox.Show(this, $"Could not open logs folder: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Handles the Click event for the "Edit Config" menu item.
        /// </summary>
        private void editConfigToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                string appSettingsJsonPath = Path.Combine(_reportPathService.AppSettingsDirectory, "appsettings.json");
                if (File.Exists(appSettingsJsonPath)) Process.Start(new ProcessStartInfo(appSettingsJsonPath) { UseShellExecute = true });
                else FlexibleMessageBox.Show(this, $"appsettings.json not found at '{appSettingsJsonPath}'", "File Not Found", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                FlexibleMessageBox.Show(this, $"Could not open appsettings.json: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Handles the Click event for the "Exit" menu item.
        /// </summary>
        private void exitToolStripMenuItem_Click(object sender, EventArgs e) => Close();

        /// <summary>
        /// Handles the Click event for the "Manage Bank Holidays" menu item.
        /// </summary>
        private void manageCustomBankHolidaysToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                using var form = _serviceProvider.GetRequiredService<ManageBankHolidaysForm>();
                form.ShowDialog(this);
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error opening ManageBankHolidaysForm: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Handles the Click event for the "Manage Email Recipients" menu item.
        /// </summary>
        private void manageEmailRecipientsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                using var form = _serviceProvider.GetRequiredService<ManageEmailRecipientsForm>();
                form.ShowDialog(this);
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error opening ManageEmailRecipientsForm: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Handles the Click event for the "Manage Greetings" menu item.
        /// </summary>
        private void manageGreetingsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                using var form = _serviceProvider.GetRequiredService<ManageGreetingsForm>();
                form.ShowDialog(this);
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error opening ManageGreetingsForm: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Handles the Click event for the "Manage Tender Exclusions" menu item.
        /// </summary>
        private void manageTenderExclusionsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                // Resolve the form from the DI container and show it as a dialog.
                using var form = _serviceProvider.GetRequiredService<ManageTenderExclusionsForm>();
                form.ShowDialog(this);
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error opening ManageTenderExclusionsForm: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Handles the Click event for the "1-Click Processing" menu item.
        /// </summary>
        private void enable1ClickProcessingToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Update1ClickProcessingModeUI();
            string mainButtonTextForReset = enable1ClickProcessingToolStripMenuItem.Checked ? (_reportPathService.IsEssentialPathConfigurationValid() ? "Generate, Process & Email Report" : "Config Error") : (_reportPathService.IsEssentialPathConfigurationValid() ? "Create Report" : "Config Error");
            _uiManager.ResetUIOnError(mainButtonTextForReset, _reportPathService.IsEssentialPathConfigurationValid(), File.Exists(_lastGeneratedRawReportPath), File.Exists(_lastGeneratedAnalysisFilePath), IsAnyDailySelected(), dailyCheckTimer.Enabled, false, _uiManager.GetAutoRunStatusLabelText());
        }

        /// <summary>
        /// Handles the Click event for the "Set Auto-Run Hour" menu item.
        /// </summary>
        private async void setAutoRunHourToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string? inputText = Microsoft.VisualBasic.Interaction.InputBox("Enter new hour (0-23) for daily auto-run check:", "Set Auto-Run Hour", _currentAutoRunHour.ToString());

            if (int.TryParse(inputText, out int newHour) && newHour >= 0 && newHour <= 23)
            {
                if (newHour != _currentAutoRunHour && await _autoRunManager.SetAutoRunHourAsync(newHour))
                {
                    _currentAutoRunHour = newHour;
                    _uiManager.SetAutoRunHour(_currentAutoRunHour);
                    FlexibleMessageBox.Show(this, $"Auto-Run hour set to {newHour}:00.", "Auto-Run Hour Updated", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    _uiManager.UpdateAutoRunUI(dailyCheckTimer.Enabled, false, string.Empty);
                }
            }
            else if (!string.IsNullOrWhiteSpace(inputText))
            {
                FlexibleMessageBox.Show(this, "Invalid hour. Please enter a number between 0 and 23.", "Invalid Input", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        /// <summary>
        /// Handles the Click event for the "Manage Automated Reports" menu item.
        /// </summary>
        private void manageAutomatedReportsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                using var form = _serviceProvider.GetRequiredService<ManageAutoReportDefinitionsForm>();
                form.ShowDialog(this);
                _autoRunManager.ReloadReportDefinitions();
                _autoRunManager.SynchronizeSuccessFlags();
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error opening ManageAutoReportDefinitionsForm: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Handles the Click event for the "Open Auto Report Definitions" menu item.
        /// </summary>
        private void openAutoReportDefinitionsFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                string? filePath = _reportPathService.GetReportDefinitionsFilePath();
                if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath)) Process.Start(new ProcessStartInfo(filePath) { UseShellExecute = true });
                else FlexibleMessageBox.Show(this, $"Auto report definitions file not found at: {filePath ?? "N/A"}", "File Not Found", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error opening auto report definitions file: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Handles the Click event for the "Retrospective Analysis" menu item.
        /// </summary>
        private async void retrospectiveAnalysisToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using var optionsForm = _serviceProvider.GetRequiredService<AnalysisOptionsForm>();
            if (optionsForm.ShowDialog(this) == DialogResult.OK)
            {
                _uiManager.SetActionButtonsEnabled(false);
                _uiManager.SetOtherControlsEnabled(false, financialYearComboBox.Visible);

                try
                {
                    await _retrospectiveAnalysisOrchestrator.GenerateAnalysisAsync(
                        optionsForm.SelectedFolder,
                        optionsForm.FileNamePattern,
                        CancellationToken.None);
                }
                catch (Exception ex)
                {
                    _statusManager.Post($"Analysis failed: {ex.Message}", MessageType.Error);
                    Logger.LogError("Retrospective Analysis failed with a critical exception.", ex);
                }
                finally
                {
                    _uiManager.ResetUIOnError("Create Report", _reportPathService.IsEssentialPathConfigurationValid(), File.Exists(_lastGeneratedRawReportPath), File.Exists(_lastGeneratedAnalysisFilePath), IsAnyDailySelected(), dailyCheckTimer.Enabled, false, _uiManager.GetAutoRunStatusLabelText());
                }
            }
        }

        /// <summary>
        /// Handles the Click event for the "Batch Regenerate Reports" menu item.
        /// </summary>
        private async void batchRegenerateToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using var dateForm = _serviceProvider.GetRequiredService<DateRangeSelectionForm>();
            if (dateForm.ShowDialog(this) == DialogResult.OK)
            {
                ReportType selectedType = dateForm.SelectedReportType;
                if (selectedType == ReportType.Unknown)
                {
                    FlexibleMessageBox.Show(this, "You must select a valid report type.", "Invalid Selection", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (dateForm.EndDate < dateForm.StartDate)
                {
                    FlexibleMessageBox.Show(this, "The end date cannot be before the start date.", "Invalid Date Range", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var confirmResult = FlexibleMessageBox.Show(this,
                    $"This will regenerate all '{ReportTypeHelper.GetDisplayString(selectedType, _configuration)}' reports from {dateForm.StartDate:d} to {dateForm.EndDate:d}.\n\nThis can take a very long time and will overwrite existing files. Are you sure you want to continue?",
                    "Confirm Batch Regeneration", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

                if (confirmResult == DialogResult.No) return;

                _uiManager.SetActionButtonsEnabled(false);
                _uiManager.SetOtherControlsEnabled(false, financialYearComboBox.Visible);

                try
                {
                    await _batchRegenerator.RegenerateReportsAsync(selectedType, dateForm.StartDate, dateForm.EndDate, CancellationToken.None);
                }
                catch (Exception ex)
                {
                    _statusManager.Post($"Batch regeneration failed: {ex.Message}", MessageType.Error);
                    Logger.LogError("Batch regeneration failed with a critical exception.", ex);
                }
                finally
                {
                    _uiManager.ResetUIOnError("Create Report", _reportPathService.IsEssentialPathConfigurationValid(), File.Exists(_lastGeneratedRawReportPath), File.Exists(_lastGeneratedAnalysisFilePath), IsAnyDailySelected(), dailyCheckTimer.Enabled, false, _uiManager.GetAutoRunStatusLabelText());
                }
            }
        }
        #endregion

        #region Auto-Run Timer Event Handler

        /// <summary>
        /// Handles the Tick event for the daily auto-run timer.
        /// </summary>
        private async void dailyCheckTimer_Tick(object sender, EventArgs e)
        {
            if (!dailyCheckTimer.Enabled || _autoRunManager == null) return;
            bool originallyEnabled = dailyCheckTimer.Enabled;
            dailyCheckTimer.Stop();
            AutoRunActionResult autoRunResult = AutoRunActionResult.NoActionNeeded;
            try
            {
                autoRunResult = await _autoRunManager.PerformDailyCheckAsync(originallyEnabled, _currentAutoRunHour);
            }
            catch (Exception ex)
            {
                Logger.LogCritical($"CRITICAL ERROR during AutoRunManager.PerformDailyCheckAsync: {ex.Message}", ex);
                _statusManager.Post("Critical AutoRun Error! Check Logs.", MessageType.Error);
                _uiManager.UpdateStatusRight("AutoRun: FAILED (Timer Error)");
                UpdateAutoRunButtonAndStatus(dailyCheckTimer.Enabled, true, "AutoRun: FAILED (Timer Error)");
                autoRunResult = AutoRunActionResult.CriticalError;
            }
            finally
            {
                if (originallyEnabled && autoRunResult != AutoRunActionResult.CriticalError) dailyCheckTimer.Start();
                if (autoRunResult is AutoRunActionResult.ActionAttempted or AutoRunActionResult.CriticalError)
                {
                    _uiManager.ResetUIOnError(oneClickProcessButton.Visible ? "Generate, Process & Email Report" : "Create Report", _reportPathService.IsEssentialPathConfigurationValid(), File.Exists(_lastGeneratedRawReportPath), File.Exists(_lastGeneratedAnalysisFilePath), IsAnyDailySelected(), dailyCheckTimer.Enabled, autoRunResult == AutoRunActionResult.CriticalError, _uiManager.GetAutoRunStatusLabelText());
                }
            }
        }

        /// <summary>
        /// Handles the Click event for the auto-run toggle button.
        /// </summary>
        private void toggleAutoRunButton_Click(object sender, EventArgs e)
        {
            dailyCheckTimer.Enabled = !dailyCheckTimer.Enabled;
            string statusText = _uiManager.GetAutoRunStatusLabelText();
            bool isAutoRunCompletedForToday = statusText.Contains("Completed", StringComparison.OrdinalIgnoreCase) || statusText.Contains("Done for", StringComparison.OrdinalIgnoreCase) || statusText.Contains("FAILED", StringComparison.OrdinalIgnoreCase);
            UpdateAutoRunButtonAndStatus(dailyCheckTimer.Enabled, isAutoRunCompletedForToday, string.Empty);
            Logger.LogInfo($"AutoRun timer {(dailyCheckTimer.Enabled ? "Enabled" : "Disabled")} by user.");
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Gathers all parameters for a manual report run from the UI controls.
        /// </summary>
        private ManualReportParameters GatherManualReportParameters() => new() { StartDate = startDatePicker.Value, EndDate = endDatePicker.Value, ReportType = GetSelectedReportType(), FinancialYear = financialYearComboBox.Visible ? financialYearComboBox.SelectedItem?.ToString() : null, IsFemiOnlyChecked = sendToFemiOnlyCheckBox.Checked && sendToFemiOnlyCheckBox.Visible, SkipEmail = skipEmailCheckBox.Checked, IncludeLeadTimeAnalysis = chkIncludeLeadTimeAnalysis.Checked, ReportBaseName = "EstimateSuccessReport", IsDebugBuild = IsDebug };

        /// <summary>
        /// Gets the currently selected ReportType from the ComboBox.
        /// </summary>
        private ReportType GetSelectedReportType()
        {
            string? selectedText = null;
            UIManager.SafeControlUpdate(reportTypeComboBox, () => selectedText = reportTypeComboBox.SelectedItem?.ToString() ?? reportTypeComboBox.Text);
            return ReportTypeHelper.FromString(selectedText);
        }

        /// <summary>
        /// Updates the UI to show either the 1-Click button or the two-step buttons.
        /// </summary>
        private void Update1ClickProcessingModeUI()
        {
            bool oneClickEnabled = enable1ClickProcessingToolStripMenuItem.Checked;
            UIManager.SafeControlUpdate(oneClickProcessButton, () => oneClickProcessButton.Visible = oneClickEnabled);
            UIManager.SafeControlUpdate(createReportButton, () => createReportButton.Visible = !oneClickEnabled);
            UIManager.SafeControlUpdate(processEmailButton, () => processEmailButton.Visible = !oneClickEnabled);
            if (oneClickEnabled && oneClickProcessButton != null) UIManager.SafeControlUpdate(oneClickProcessButton, () => oneClickProcessButton.BringToFront());
        }

        /// <summary>
        /// Populates the financial year dropdown based on the current date.
        /// </summary>
        private void PopulateFinancialYearDropdown()
        {
            UIManager.SafeControlUpdate(financialYearComboBox, () =>
            {
                string? previouslySelected = financialYearComboBox.SelectedItem?.ToString();
                financialYearComboBox.Items.Clear();

                // --- MODIFIED: Uses the new IFinancialYearService ---
                string currentFY = _financialYearService.GetCurrentFinancialYear(true);
                if (!string.IsNullOrEmpty(currentFY))
                {
                    financialYearComboBox.Items.Add(currentFY);
                    string? previousFY = _financialYearService.GetPreviousFinancialYear(currentFY);
                    if (!string.IsNullOrEmpty(previousFY)) financialYearComboBox.Items.Add(previousFY);
                }
                else
                {
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
        }

        /// <summary>
        /// Checks if one of the daily report types is currently selected.
        /// </summary>
        private bool IsAnyDailySelected() => GetSelectedReportType() is ReportType.Daily or ReportType.Daily5Day1k;

        /// <summary>
        /// Re-initialises components and UI text that depend on configuration values after settings have changed.
        /// </summary>
        private void ReinitialiseConfigurableComponents()
        {
            _appName = _configuration.GetValue<string>(AppConfigKeys.ApplicationInfo.AppName, "QCRA")!;
            _appVersion = _configuration.GetValue<string>(AppConfigKeys.ApplicationInfo.AppVersion, "1.0.0")!;
            this.Text = $"{_appName} - {(IsDebug ? "DEBUG" : "RELEASE")} - v{_appVersion}";
            _currentAutoRunHour = _configuration.GetValue<int>(AppConfigKeys.AutoRunProcess.CheckHour, 8);
            _uiManager.SetAutoRunHour(_currentAutoRunHour);
            UpdateAutoRunButtonAndStatus(dailyCheckTimer.Enabled, false, string.Empty);
            bool configIsValid = _reportPathService.IsEssentialPathConfigurationValid();
            _uiManager.ResetButtonStatesAfterTypeChange(configIsValid);
            Update1ClickProcessingModeUI();
        }
        #endregion
    }
    #endregion
}

