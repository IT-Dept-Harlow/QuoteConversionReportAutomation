// Form1.cs
// Main application form for the QCRA (Quote Conversion Report Automation) application.
// This form serves as the primary user interface for generating, processing, and emailing
// quote conversion reports, managing settings, and monitoring automated tasks.
// Utilises C# 10+ features and reads configuration from a structured appsettings.json.

#region Using Directives
// System related namespaces
// Third-party namespaces
using Microsoft.Extensions.Configuration; // For IConfiguration
using Microsoft.VisualBasic; // For Interaction.InputBox (Consider replacing with a custom form for better theming/control if time permits)
using QuoteConversionReportAutomation;
// Project specific namespaces
using QuoteConversionReportAutomation.Forms;    // For ManageAutoReportDefinitionsForm, HelpForm, etc.
using QuoteConversionReportAutomation.Helpers;  // For ReportHelper, FlexibleMessageBox, EmailSendResult
using QuoteConversionReportAutomation.Managers; // For UIManager, ReportProcessManager, AutoRunManager, EmailRecipientManager, GreetingManager
using QuoteConversionReportAutomation.Models;   // For ReportRequest, ReportResponse, AutoReportDefinition
using QuoteConversionReportAutomation.Services.Communication; // For NamedPipeCommunicator
using QuoteConversionReportAutomation.Services.Excel;         // For ExcelCopyData, ProgressReport
using QuoteConversionReportAutomation.Services.Logging;     // For Logger
using System;
using System.Collections.Generic;
using System.Diagnostics; // For Process, Stopwatch
using System.Globalization; // For CultureInfo
using System.IO;          // For Path, File, Directory operations
using System.Linq;        // For Enumerable.Any()
using System.Text;        // For StringBuilder
using System.Threading;   // For CancellationToken, CancellationTokenSource
using System.Threading.Tasks; // For Task, Task.Run, Task.Delay
using System.Windows.Forms; // For Form, Control, MessageBox, etc.
#endregion

// Namespace for the main UI of the application.
namespace conversionTest
{
    /// <summary>
    /// Represents the main form of the Quote Conversion Report Automation (QCRA) application.
    /// This form serves as the primary user interface for:
    /// - Manually generating, processing, and emailing various quote conversion reports.
    /// - Configuring report parameters such as type, date range, and financial year.
    /// - Managing application settings including dark mode, 1-click processing, auto-run hour,
    ///   and configurations for automated reports, email recipients, greetings, and custom bank holidays.
    /// - Toggling and monitoring the status of automated report generation.
    /// - Viewing generated reports and application logs.
    /// It co-ordinates various manager classes (UIManager, ReportProcessManager, AutoRunManager, etc.)
    /// to perform these operations and handles UI events and updates.
    /// The application name and version are now read from configuration.
    /// </summary>
    public partial class Form1 : Form
    {
        #region Constants and Fields

        #region Dependencies
        // These fields hold instances of services and managers required by the form.
        // They are typically initialised in the constructor and are marked 'readonly'
        // to indicate they are set once and not changed thereafter.
        private readonly IConfiguration _configuration;             // Provides access to application configuration settings from appsettings.json.
        private readonly EmailUtility _emailUtility;                 // Handles sending emails.
        private readonly UIManager _uiManager;                       // Manages UI updates, theming, and control states.
        private readonly ReportProcessManager _processManager;       // Manages the external Crystal Report Wrapper process.
        private readonly NamedPipeCommunicator _pipeCommunicator;    // Handles IPC with the Crystal Report Wrapper via named pipes.
        private readonly AutoRunManager _autoRunManager;             // Manages automated (scheduled) report generation.
        private readonly ExcelCopyData _excelProcessor;              // Performs Excel data manipulation and processing.
        private readonly EmailRecipientManager _emailRecipientManager; // Manages email recipient lists.
        private readonly GreetingManager _greetingManager;           // Manages email greetings.
        #endregion

        #region Application Info
        /// <summary>
        /// Current version of the application, read from configuration.
        /// Used for display purposes (e.g., title bar, help).
        /// </summary>
        private readonly string _appVersion;
        /// <summary>
        /// Name of the application, read from configuration.
        /// Used for display purposes.
        /// </summary>
        private readonly string _appName;
        #endregion

        #region State Variables
        // These fields store the runtime state of the application specific to Form1's operations.
        private string _generatedReportPath = string.Empty;         // Stores the full path to the last successfully generated raw report file.
        private string _generatedAnalysisFilePath = string.Empty;   // Stores the full path to the last successfully processed analysis file.
        private bool _programmaticallyChangingDates = false;        // Flag to prevent date picker ValueChanged events from re-triggering logic during programmatic date changes.
        private int _currentAutoRunHour;                            // Stores the configured hour (0-23) for the daily automated report check.
        private HelpForm? _helpFormInstance;                        // Holds a reference to the HelpForm to ensure only one instance is open.
        #endregion

        #region Configuration Paths
        // Paths related to application configuration files.
        // s_appSettingsBasePath is determined once at startup and should point to the directory of appsettings.json.
        private static readonly string s_appSettingsBasePath = DetermineAppSettingsBasePath();
        private readonly string _appSettingsPath; // Full path to appsettings.json
        private readonly string _autoReportDefinitionsFilePath; // Full path to autoReportDefinitions.json
        #endregion

        #region Report Type Constants
        // Integer indices for different report types selected in the UI ComboBox.
        // These must align with ComboBox item order and logic using these indices.
        private const int DailyReportIndex = 0;
        private const int NewDailyReportOver1kIndex = 1; // "Daily (5days >= £1000)"
        private const int WeeklyReportIndex = 2;
        private const int MonthlyReportIndex = 3;
        private const int QuarterlyReportIndex = 4;
        private const int AnnualReportIndex = 5;
        private const int CustomReportIndex = 6;
        #endregion

        #region Build Configuration
        /// <summary>
        /// Gets a value indicating whether the application is running in DEBUG mode.
        /// Determined by preprocessor directives. Useful for conditional logic or logging.
        /// </summary>
        private static bool IsDebug =>
#if DEBUG
            true;
#else
            false;
#endif
        #endregion

        #region Configuration-derived Properties (using new config structure)
        // Properties providing access to paths and settings from the reorganized appsettings.json.
        // They combine UserProfilePath with relative paths from configuration and include default fallbacks.

        /// <summary>Gets the current user's profile directory path (e.g., "C:\Users\YourUser").</summary>
        private string UserProfilePath => Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        /// <summary>Gets the base directory for exporting raw Crystal Reports.</summary>
        private string RawReportExportBaseDir =>
            Path.Combine(UserProfilePath, _configuration["Paths:RawReportOutputBase"]
                ?.TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                ?? @"Harlow Printing\IT Projects - Documents\Dashboard Datasets\Raw_data\Quotes conversion\Estimate Reports Exports");

        /// <summary>Gets the base directory for saving final processed Excel analysis files.</summary>
        public string ExcelFinalSaveLocation =>
            Path.Combine(UserProfilePath, _configuration["Paths:FinalReportOutputBase"]
                ?.TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                ?? @"Harlow Printing\IT Projects - Documents\Dashboard Datasets\Raw_data\Quotes conversion\Estimates");

        /// <summary>Gets the full path to the Crystal Report definition file (.rpt).</summary>
        private string CrystalReportLocation => _configuration["Paths:CrystalReportRptFile"] ?? string.Empty;

        /// <summary>Gets the base directory where Excel template files are stored.</summary>
        public string ExcelTemplateBaseDir =>
            Path.Combine(UserProfilePath, _configuration["Paths:TemplateBase"]
                ?.TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                ?? @"Harlow Printing\IT Projects - Documents\Dashboard Datasets\Raw_data\Quotes conversion\TEMPLATE");

        /// <summary>Gets the configured base directory for application logs.</summary>
        private string ConfiguredLogDirectoryBase => _configuration["Paths:LogDirectoryBase"] ?? string.Empty;

        /// <summary>Gets the configured filename for the automated report definitions JSON file.</summary>
        private string ReportDefinitionsFileName => _configuration.GetValue<string>("Paths:ReportDefinitionsFileName", "autoReportDefinitions.json")!; // Default if not in config
        #endregion

        #region Dynamic Path Properties
        // Properties that dynamically determine full file paths based on UI selections and configuration.

        /// <summary>
        /// Gets the full output path for the raw Crystal Report export file based on current UI selections.
        /// Uses <see cref="FolderCreation.GetReportSpecificFolderPath"/> for structured subfolder paths.
        /// </summary>
        public string ReportOutputLocation
        {
            get
            {
                string baseDir = RawReportExportBaseDir;
                DateTime dateForFilename = endDatePicker.Value;
                string fileName = $"{dateForFilename:yyyyMMdd}_EstimateSuccessReport_Raw.xlsx"; // Standardized raw report filename part
                int currentReportTypeIndex = GetSelectedReportTypeIndex();

                DateTime folderTimestampDate = (currentReportTypeIndex == CustomReportIndex) ? DateTime.Now : endDatePicker.Value;
                if (currentReportTypeIndex == NewDailyReportOver1kIndex)
                {
                    folderTimestampDate = endDatePicker.Value;
                }

                // Pass IConfiguration to FolderCreation helper as it now reads folder names from config.
                string? specificFolder = FolderCreation.GetReportSpecificFolderPath(currentReportTypeIndex, baseDir, folderTimestampDate, _configuration);

                if (string.IsNullOrEmpty(specificFolder))
                {
                    Logger.LogError($"Could not determine specific folder path for ReportOutputLocation. ReportType: {currentReportTypeIndex}, Base: {baseDir}. Using fallback based on report type name.");
                    // Fallback logic using ReportTypeFolderNames from config
                    string reportTypeFolderName = _configuration[$"OperationalParameters:ReportTypeFolderNames:{GetReportTypeNameForFolder(currentReportTypeIndex)}"] ?? "Other Reports";
                    specificFolder = Path.Combine(baseDir, reportTypeFolderName);
                    try { Directory.CreateDirectory(specificFolder); }
                    catch (Exception ex) { Logger.LogError($"Failed to create fallback directory '{specificFolder}': {ex.Message}"); }
                }
                return Path.Combine(specificFolder, fileName);
            }
        }

        /// <summary>
        /// Gets the full path to the Excel template file based on the current report type selection.
        /// Longer period reports may use a different template (e.g., "_Monthly.xlsx").
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
                        => "TEMPLATE_Estimate Success Rate_Monthly.xlsx", // Template for reports needing manual pivot refresh
                    _ => "TEMPLATE_Estimate Success Rate.xlsx" // Standard template for other types
                };
                return Path.Combine(baseDir, templateName);
            }
        }
        #endregion

        #endregion // End of Constants and Fields

        #region Constructor
        /// <summary>
        /// Initialises a new instance of the <see cref="Form1"/> class.
        /// This is the main entry point for the form. It sets up dependencies by instantiating manager classes
        /// and initialises UI components defined in the form's designer.
        /// It also loads configuration settings required for the form and its components, including application name and version.
        /// </summary>
        /// <param name="configuration">The application's configuration settings, loaded from `appsettings.json`.
        /// This provides access to paths, connection strings, and other operational parameters.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="configuration"/> is null, as it's essential for operation.</exception>
        /// <exception cref="InvalidOperationException">Thrown if critical configuration settings required by manager classes are missing or invalid,
        /// or if essential paths cannot be determined, potentially leaving the application in an unusable state.</exception>
        public Form1(IConfiguration configuration)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _appSettingsPath = Path.Combine(s_appSettingsBasePath, "appsettings.json");

            // Load AppName and AppVersion from configuration
            _appName = _configuration.GetValue<string>("ApplicationInfo:AppName", "QCRA")!; // Default to "QCRA" if not found
            _appVersion = _configuration.GetValue<string>("ApplicationInfo:AppVersion", "1.0.0")!; // Default to "1.0.0" if not found

            // Determine the path for the dedicated report definitions file
            string? appSettingsDir = Path.GetDirectoryName(_appSettingsPath);
            if (string.IsNullOrEmpty(appSettingsDir))
            {
                string errorMsg = $"Could not determine directory from appSettingsPath: '{_appSettingsPath}'. Cannot locate report definitions file.";
                Logger.LogCritical(errorMsg);
                throw new DirectoryNotFoundException(errorMsg);
            }
            _autoReportDefinitionsFilePath = Path.Combine(appSettingsDir, ReportDefinitionsFileName); // Uses property that reads from config
            Logger.LogInfo($"Form1: Automated report definitions will be managed via: '{_autoReportDefinitionsFilePath}'");


            Logger.LogTrace("Entering Form1 Constructor");
            try
            {
                InitializeComponent();
                Logger.LogDebug("InitializeComponent completed.");

                _emailUtility = new EmailUtility(_configuration);
                _excelProcessor = new ExcelCopyData(_configuration); // Pass IConfiguration to ExcelCopyData if it needs config (e.g., sheet names)                                     
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

                string wrapperExePathConfig = _configuration["Paths:WrapperExecutable"] ?? "CrystalReportWrapper.exe";
                string wrapperExeFullPath = Path.GetFullPath(wrapperExePathConfig);
                _processManager = new ReportProcessManager(wrapperExeFullPath);

                _pipeCommunicator = new NamedPipeCommunicator(_configuration); // Pass IConfiguration if NamedPipeCommunicator needs config (e.g. pipe name)

                _currentAutoRunHour = _configuration.GetValue<int>("AutoRunProcess:CheckHour", 8);
                _uiManager.SetAutoRunHour(_currentAutoRunHour);
                _autoRunManager = new AutoRunManager(
                    _configuration, _emailUtility, _processManager, _pipeCommunicator,
                    _uiManager, _excelProcessor, _appSettingsPath, _emailRecipientManager, _greetingManager,
                     _currentAutoRunHour
                );

                Logger.LogDebug("Service and Manager classes instantiated successfully.");
            }
            catch (Exception ex)
            {
                Logger.LogCritical($"CRITICAL ERROR during Form Initialisation: {ex.Message}", ex);
                System.Windows.Forms.MessageBox.Show(
                    $"A critical error occurred initialising the application:\n\n{ex.Message}\n\nThe application cannot continue and will now exit.",
                    "Initialisation Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                throw;
            }
            Logger.LogTrace("Exiting Form1 Constructor");
        }
        #endregion

        #region Form Lifecycle Events
        /// <summary>
        /// Handles the Load event of the form. This method is called once when the form is first displayed.
        /// It's responsible for initialising the application's state, applying the visual theme,
        /// performing start-up checks (like ensuring the report service can be started), and initiating background tasks like report archiving.
        /// </summary>
        /// <param name="sender">The source of the event (typically the Form itself).</param>
        /// <param name="e">An <see cref="EventArgs"/> that contains no event data.</param>
        private async void Form1_Load(object sender, EventArgs e)
        {
            Logger.LogTrace("Entering Form1_Load");
            _uiManager.UpdateStatusMain("Loading application...");

            try
            {
                BankHolidayHelper.Initialize();
                Logger.LogInfo("BankHolidayHelper initialised successfully.");

                string crystalReportPath = CrystalReportLocation;
                string wrapperExePathConfig = _configuration["Paths:WrapperExecutable"] ?? string.Empty;
                string wrapperExeFullPath = string.IsNullOrEmpty(wrapperExePathConfig) ? string.Empty : Path.GetFullPath(wrapperExePathConfig);
                bool configValid = true;

                if (string.IsNullOrEmpty(crystalReportPath) || !File.Exists(crystalReportPath))
                {
                    Logger.LogError($"Configuration Error: 'Paths:CrystalReportRptFile' is missing or the file was not found at '{crystalReportPath}'. Report generation will be affected.");
                    configValid = false;
                }
                if (string.IsNullOrEmpty(wrapperExeFullPath) || !File.Exists(wrapperExeFullPath))
                {
                    Logger.LogError($"Configuration Error: 'Paths:WrapperExecutable' is missing or the file was not found at '{wrapperExeFullPath}'. Report generation will be affected.");
                    configValid = false;
                }

                // Set Form Title using AppName and AppVersion from configuration
                Text = $"{_appName} - {(IsDebug ? "DEBUG" : "RELEASE")} - v{_appVersion}";
                StartPosition = FormStartPosition.CenterScreen;

                financialYearComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
                reportTypeComboBox.DropDownStyle = ComboBoxStyle.DropDownList;

                if (!reportTypeComboBox.Items.Contains("Custom"))
                {
                    reportTypeComboBox.Items.Add("Custom");
                }
                if (reportTypeComboBox.Items.Count > DailyReportIndex && reportTypeComboBox.Items.Contains("Daily"))
                {
                    reportTypeComboBox.SelectedIndex = reportTypeComboBox.Items.IndexOf("Daily");
                }
                else if (reportTypeComboBox.Items.Count > 0)
                {
                    reportTypeComboBox.SelectedIndex = 0;
                }

                bool useDarkMode = UIManager.IsWindowsDarkModeEnabled();
                darkModeToolStripMenuItem.Checked = useDarkMode;
                _uiManager.ApplyTheme(useDarkMode);

                _uiManager.UpdateAutoRunUI(dailyCheckTimer.Enabled, false, useDarkMode, $"Auto Run: {(dailyCheckTimer.Enabled ? $"Enabled (Next check ~{_currentAutoRunHour}:00)" : "Disabled")}");

                reportTypeComboBox_SelectedIndexChanged(reportTypeComboBox, EventArgs.Empty);
                _uiManager.ResetButtonStatesAfterTypeChange(configValid);

                enable1ClickProcessingToolStripMenuItem.Checked = false;
                Update1ClickProcessingModeUI();

                if (!configValid)
                {
                    _uiManager.UpdateStatusMain("Config Error: Check Options menu.");
                }

                _uiManager.UpdateStatusMain("Checking report service...");
                IProgress<string> loadProgress = new Progress<string>(status => _uiManager.UpdateProgress(status));
                bool wrapperOk = await _processManager.EnsureWrapperIsRunningAsync(loadProgress);

                if (!wrapperOk && configValid)
                {
                    _uiManager.UpdateStatusMain("Report service failed to start. Report generation may fail.");
                }

                string? finalDir = ExcelFinalSaveLocation;
                string? rawDir = RawReportExportBaseDir;
                // Read archive days from new config path
                int? archiveDays = _configuration.GetValue<int?>("OperationalParameters:ArchiveRawReportsOlderThanDays") ?? 30;
                // Read the configured archive folder name from IConfiguration
                string? configuredArchiveFolder = _configuration.GetValue<string>("OperationalParameters:ReportArchiveFolderName") ?? "Archive";

                _ = Task.Run(async () => await ReportArchiver.ArchiveOldReportsAsync(finalDir, rawDir, archiveDays, configuredArchiveFolder)) // <-- MAKE SURE THIS LINE PASSES configuredArchiveFolder
                        .ContinueWith(t =>
                        {
                            if (t.IsFaulted && t.Exception != null) Logger.LogError($"Background report archiving task failed: {t.Exception.GetBaseException().Message}");
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
        /// Handles the FormClosing event, which is triggered when the form is about to be closed.
        /// This method is used for clean-up operations, such as stopping timers and terminating any managed background processes.
        /// </summary>
        /// <param name="sender">The source of the event (typically the Form itself).</param>
        /// <param name="e">A <see cref="FormClosingEventArgs"/> that contains data related to the closing event.
        /// This can be used to cancel the closing operation if needed (e.g., e.Cancel = true).</param>
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            Logger.LogInfo("Form closing. Stopping timer and terminating wrapper process.");
            dailyCheckTimer.Stop();
            _processManager.TerminateWrapperProcess();
        }
        #endregion

        #region Main Action Button Event Handlers
        // This region contains event handlers for the primary action buttons on the form,
        // such as "Create Report", "Process & Email", and "1-Click Process".

        /// <summary>
        /// Handles the Click event for the "Create Report" button.
        /// This initiates the asynchronous process of generating the raw report data.
        /// </summary>
        /// <param name="sender">The source of the event (the "Create Report" button).</param>
        /// <param name="e">An <see cref="EventArgs"/> that contains no event data.</param>
        private async void createReportButton_Click(object sender, EventArgs e)
        {
            await PerformCreateReportAsync();
        }

        /// <summary>
        /// Handles the Click event for the "Process & Email" button.
        /// This initiates the asynchronous process of taking a previously generated raw report,
        /// processing it into a final analysis file, and then emailing it.
        /// The email step can be skipped based on the state of the `skipEmailCheckBox`.
        /// </summary>
        /// <param name="sender">The source of the event (the "Process & Email" button).</param>
        /// <param name="e">An <see cref="EventArgs"/> that contains no event data.</param>
        private async void processEmailButton_Click(object sender, EventArgs e)
        {
            await PerformProcessAndEmailAsync(skipEmail: skipEmailCheckBox.Checked);
        }

        /// <summary>
        /// Handles the Click event for the "1-Click Process" button.
        /// This button, when visible (1-Click mode enabled), performs the entire sequence:
        /// 1. Generate raw report.
        /// 2. Process raw report into analysis file.
        /// 3. Email the analysis file (unless skipped).
        /// </summary>
        /// <param name="sender">The source of the event (the "1-Click Process" button).</param>
        /// <param name="e">An <see cref="EventArgs"/> that contains no event data.</param>
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
                string buttonText = CheckConfigValidity() ? "Generate, Process & Email Report" : "Config Error";
                ResetUIStateOnError(buttonText);
                return;
            }

            await PerformProcessAndEmailAsync(skipEmail: skipEmailCheckBox.Checked);
            Logger.LogInfo("1-Click Process sequence completed (or aborted if errors occurred).");
        }


        /// <summary>
        /// Handles the Click event for the "View Raw File" button.
        /// Opens the last generated raw report file (path stored in <see cref="_generatedReportPath"/>)
        /// using the system's default application for .xlsx files (usually Excel).
        /// </summary>
        /// <param name="sender">The source of the event (the "View Raw File" button).</param>
        /// <param name="e">An <see cref="EventArgs"/> that contains no event data.</param>
        private void viewReportButton_Click(object sender, EventArgs e)
        {
            ReportHelper.OpenFileWithDefaultApp(_generatedReportPath, "raw report output");
        }

        /// <summary>
        /// Handles the Click event for the "View Processed File" button.
        /// Opens the last generated final analysis file (path stored in <see cref="_generatedAnalysisFilePath"/>)
        /// using the system's default application.
        /// </summary>
        /// <param name="sender">The source of the event (the "View Processed File" button).</param>
        /// <param name="e">An <see cref="EventArgs"/> that contains no event data.</param>
        private void viewAnalysisButton_Click(object sender, EventArgs e)
        {
            ReportHelper.OpenFileWithDefaultApp(_generatedAnalysisFilePath, "processed analysis file");
        }
        #endregion

        #region Core Report Logic Methods
        // This region contains the core asynchronous methods responsible for the main
        // report generation and processing workflows.

        /// <summary>
        /// Asynchronously performs the steps to create the raw report data.
        /// This involves validating inputs, ensuring the Crystal Report Wrapper service is running,
        /// constructing and sending a <see cref="ReportRequest"/>, handling the <see cref="ReportResponse"/>,
        /// and updating UI elements.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
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

            int timeoutMinutes = _configuration.GetValue<int>("OperationalParameters:ProcessTimeoutMinutes", 6);
            using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(timeoutMinutes));
            IProgress<string> progress = new Progress<string>(status => _uiManager.UpdateProgress(status));

            try
            {
                if (!ValidateInputDates()) { ResetUIStateOnError(originalButtonText); return; }
                if (!ValidateFinancialYearSelection()) { ResetUIStateOnError(originalButtonText); return; }

                string crystalReportPath = CrystalReportLocation;
                if (string.IsNullOrEmpty(crystalReportPath) || !File.Exists(crystalReportPath))
                {
                    throw new InvalidOperationException($"Crystal Report location ('{crystalReportPath}') is invalid or file not found. Check configuration path 'Paths:CrystalReportRptFile'.");
                }

                if (!await _processManager.EnsureWrapperIsRunningAsync(progress, cts.Token))
                {
                    throw new InvalidOperationException($"Failed to start or connect to the report service (CrystalReportWrapper).");
                }

                string reportOutputPath = ReportOutputLocation;
                var request = new ReportRequest
                {
                    CrystalReportLocation = crystalReportPath,
                    ReportOutputLocation = reportOutputPath,
                    ReportDateFrom = startDatePicker.Value,
                    ReportDateTo = endDatePicker.Value
                };

                Logger.LogInfo($"Attempting Named Pipe communication with CrystalReportWrapper. Requesting report for: {request.ReportDateFrom:d} to {request.ReportDateTo:d}, Output: {request.ReportOutputLocation}");
                ReportResponse? response = await _pipeCommunicator.SendRequestReceiveResponseAsync(request, progress, cts.Token);

                if (response?.Success == true && !string.IsNullOrEmpty(response.OutputPath) && File.Exists(response.OutputPath))
                {
                    _generatedReportPath = response.OutputPath;
                    Logger.LogInfo($"Raw report generated successfully by wrapper: {_generatedReportPath}");

                    if (oneClickProcessButton.Visible)
                    {
                        // In 1-Click mode, status is updated by subsequent steps.
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
        /// Asynchronously processes a previously generated raw report into a final analysis Excel file,
        /// and then (optionally) emails it. This method now checks the .Success property of EmailSendResult.
        /// </summary>
        /// <param name="skipEmail">If true, the email sending step will be bypassed after processing.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
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

            int timeoutMinutes = _configuration.GetValue<int>("OperationalParameters:ProcessTimeoutMinutes", 15);
            using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(timeoutMinutes));
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
                    string resetText = oneClickProcessButton.Visible ? (CheckConfigValidity() ? "Generate, Process & Email Report" : "Config Error")
                                                                  : (CheckConfigValidity() ? "Create Report" : "Config Error");
                    ResetUIStateOnError(resetText);
                    return;
                }

                string? expectedFinalPath = _excelProcessor.GetExpectedFinalFilePath(reportType, baseSaveLocation, dateForFilenameAndExcelProcessing);
                bool useExistingFile = false;

                if (expectedFinalPath != null && File.Exists(expectedFinalPath))
                {
                    generalProgress.Report("Found existing file. Prompting user...");
                    DialogResult fdr = FlexibleMessageBox.Show(this,
                        $"The report file '{Path.GetFileName(expectedFinalPath)}' already exists for this period.\n\nDo you want to skip processing and use this existing file?",
                        "File Already Exists", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                    if (fdr == DialogResult.Yes)
                    {
                        Logger.LogInfo($"User chose to use existing file: {expectedFinalPath}");
                        finalFilePath = expectedFinalPath;
                        _generatedAnalysisFilePath = finalFilePath;
                        _uiManager.ShowViewAnalysisButton(true, finalFilePath);
                        useExistingFile = true;
                    }
                    else
                    {
                        generalProgress.Report("Deleting existing file to regenerate...");
                        Logger.LogInfo($"User chose to overwrite/regenerate the existing file: {expectedFinalPath}");
                        try
                        {
                            File.Delete(expectedFinalPath);
                            Logger.LogInfo($"Successfully deleted existing file: {expectedFinalPath}");
                        }
                        catch (Exception delEx)
                        {
                            Logger.LogError($"Failed to delete existing file '{expectedFinalPath}': {delEx.Message}");
                            FlexibleMessageBox.Show(this, $"Could not delete the existing report file:\n{expectedFinalPath}\n\nPlease ensure the file is not open and try again.\n\nError: {delEx.Message}", "File Deletion Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            ResetUIStateOnError(originalButtonText); return;
                        }
                    }
                }

                if (!useExistingFile)
                {
                    generalProgress.Report("Processing new report...");
                    finalFilePath = await _excelProcessor.ProcessExcelReportAsync(
                        financialYearComboBox.SelectedItem?.ToString() ?? _excelProcessor.GetCurrentFinancialYear(true),
                        reportType,
                        _generatedReportPath,
                        _configuration.GetValue<string>("OperationalParameters:ExcelSheetNames:RawDataSourceSheet", "Sheet1")!,
                        baseSaveLocation,
                        ExcelTemplateLocation,
                        _configuration.GetValue<string>("OperationalParameters:ExcelSheetNames:TemplateDataCopySheet", "DATA")!,
                        1, 1,
                        excelProgress,
                        dateForFilenameAndExcelProcessing,
                        token);

                    if (string.IsNullOrEmpty(finalFilePath) || !File.Exists(finalFilePath))
                    {
                        if (token.IsCancellationRequested) throw new OperationCanceledException("Excel processing was cancelled.");
                        throw new Exception("Excel processing failed to produce a final file. Check logs for details.");
                    }
                    _generatedAnalysisFilePath = finalFilePath;
                    _uiManager.ShowViewAnalysisButton(true, finalFilePath);
                }

                bool proceedToEmail = true;
                if (requiresManualRefresh && finalFilePath != null)
                {
                    generalProgress.Report("Waiting for manual Excel refresh...");
                    proceedToEmail = await HandleManualExcelRefreshAsync(finalFilePath, token);
                    if (!proceedToEmail && !token.IsCancellationRequested)
                    { _uiManager.UpdateStatusMain("Manual refresh/confirmation cancelled."); ResetUIStateOnError(originalButtonText); return; }
                    if (token.IsCancellationRequested) throw new OperationCanceledException("Operation cancelled during manual refresh prompt.");
                    generalProgress.Report("Manual refresh confirmed.");
                }

                if (!skipEmail && proceedToEmail && !string.IsNullOrEmpty(finalFilePath))
                {
                    EmailSendResult emailResult = await SendCompletionEmailAsync(finalFilePath, generalProgress, token);
                    if (!emailResult.Success)
                    {
                        Logger.LogError($"Email sending failed after processing. Error: {emailResult.ErrorMessage}, Code: {emailResult.SmtpErrorCode}");
                    }
                }
                else if (skipEmail)
                {
                    _uiManager.UpdateStatusMain("Process completed. Email skipped by user.");
                    Logger.LogInfo("Email sending skipped by user checkbox.");
                }

                if (proceedToEmail || skipEmail)
                {
                    _uiManager.SetUICompleted(CheckConfigValidity(), IsAnyDailySelected(), dailyCheckTimer.Enabled, darkModeToolStripMenuItem.Checked, false, autoRunStatusLabel.Text ?? "");
                }
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

        /// <summary>
        /// Asynchronously sends the completion email with the specified report file as an attachment.
        /// Retrieves recipients and email content based on the current report context.
        /// This method now handles the EmailSendResult object returned by EmailUtility.
        /// </summary>
        /// <param name="attachmentPath">The full path to the file to be attached.</param>
        /// <param name="progress">An <see cref="IProgress{T}"/> interface to report string-based progress updates.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe for cancellation requests.</param>
        /// <returns>An <see cref="EmailSendResult"/> object indicating the outcome of the email sending operation.</returns>
        private async Task<EmailSendResult> SendCompletionEmailAsync(string attachmentPath, IProgress<string> progress, CancellationToken cancellationToken)
        {
            Logger.LogTrace("Form1.SendCompletionEmailAsync: Entering method.");
            _uiManager.UpdateProgress("Preparing email...");

            if (!File.Exists(attachmentPath))
            {
                string errorMsg = $"Attachment file not found for email: {attachmentPath}";
                Logger.LogError(errorMsg);
                progress.Report($"Error: {errorMsg}");
                return new EmailSendResult(false, errorMsg);
            }

            try
            {
                var (to, cc) = GetEmailRecipients();

                if (!to.Any() && !cc.Any() && !IsDebug)
                {
                    Logger.LogWarning("Form1.SendCompletionEmailAsync: No email recipients determined for Release mode. Skipping email send.");
                    progress.Report("No recipients configured. Email not sent.");
                    return new EmailSendResult(true, "No recipients configured, email not sent.");
                }
                if (!to.Any() && !cc.Any() && IsDebug)
                {
                    Logger.LogInfo("Form1.SendCompletionEmailAsync: DEBUG MODE: No explicit recipients, but will proceed using debug list from EmailRecipientManager if configured there.");
                }

                var (subject, body) = GetEmailSubjectAndBody(startDatePicker.Value, endDatePicker.Value);
                progress.Report("Sending email...");

                EmailSendResult emailSendResult = await _emailUtility.SendEmailAsync(to, cc, subject, body, attachmentPath, progress, cancellationToken);

                if (!emailSendResult.Success)
                {
                    Logger.LogError($"Form1.SendCompletionEmailAsync: Email sending failed. Error: {emailSendResult.ErrorMessage}, SmtpCode: {emailSendResult.SmtpErrorCode}");
                }
                else
                {
                    Logger.LogInfo("Form1.SendCompletionEmailAsync: Email sent successfully (as reported by EmailUtility).");
                }
                return emailSendResult;
            }
            catch (OperationCanceledException)
            {
                Logger.LogWarning("Form1.SendCompletionEmailAsync: Email sending operation was cancelled.");
                progress.Report("Email sending cancelled.");
                throw;
            }
            catch (Exception ex)
            {
                Logger.LogError($"Form1.SendCompletionEmailAsync: Error preparing or dispatching email: {ex.Message}", ex);
                FlexibleMessageBox.Show(this, $"Failed to send email: {ex.Message}", "Email Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                progress.Report($"Error: {ex.Message}");
                return new EmailSendResult(false, $"Unexpected error: {ex.Message}");
            }
        }

        /// <summary>
        /// Handles the process of prompting the user for manual Excel refresh if required by the report type.
        /// Opens the Excel file, waits for the user to close it, and confirms if emailing should proceed.
        /// </summary>
        /// <param name="filePath">The path to the Excel file requiring manual refresh.</param>
        /// <param name="token">A <see cref="CancellationToken"/> to observe for cancellation requests.</param>
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
                "2. Go to the 2 Pivot sheets and right click each Table and Slicer > 'Refresh'.\n" +
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

        #region UI Event Handlers
        // This region contains event handlers for various UI controls on the form.
        // These handlers respond to user interactions like button clicks, ComboBox selections, etc.

        #region Report Configuration UI Handlers
        // Event handlers specifically related to configuring the report type, dates, and financial year.

        /// <summary>
        /// Handles the SelectedIndexChanged event of the reportTypeComboBox.
        /// This method is crucial for dynamically updating the UI (especially date pickers and financial year visibility)
        /// based on the type of report the user selects.
        /// </summary>
        /// <param name="sender">The source of the event (the reportTypeComboBox itself).</param>
        /// <param name="e">An <see cref="EventArgs"/> that contains no event data for this event.</param>
        private void reportTypeComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            Logger.LogTrace("Entering reportTypeComboBox_SelectedIndexChanged");
            if (sender is not ComboBox comboBox || comboBox.SelectedItem == null) return;

            int selectedIndex = GetSelectedReportTypeIndex(comboBox.Text);

            if (selectedIndex == CustomReportIndex)
            {
                Logger.LogDebug("Report Type changed to Custom. Manual date entry is expected.");
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
                        Logger.LogInfo($"Daily (5days >= £1000) report selected. Dates automatically set to: {dateFrom:dd/MM/yyyy} - {dateTo:dd/MM/yyyy}");
                        break;
                    case WeeklyReportIndex:
                        dateTo = todayValue;
                        dateFrom = todayValue.AddDays(-14);
                        showFinYear = true;
                        Logger.LogInfo($"Manual Weekly (15-day) report selected. Dates automatically set to: {dateFrom:dd/MM/yyyy} - {dateTo:dd/MM/yyyy}");
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
                        int prevFinancialYearStartCalendarYear = (todayValue.Month >= _configuration.GetValue<int>("OperationalParameters:FinancialYearStartMonth", 5)) ? todayValue.Year - 1 : todayValue.Year - 2;
                        (dateFrom, dateTo) = ReportHelper.GetFinancialYearDates(prevFinancialYearStartCalendarYear,
                                                                                _configuration.GetValue<int>("OperationalParameters:FinancialYearStartMonth", 5),
                                                                                _configuration.GetValue<int>("OperationalParameters:FinancialYearStartDay", 1));
                        showFinYear = false;
                        Logger.LogInfo($"Annual report selected. Dates automatically set for Financial Year: {dateFrom:dd/MM/yyyy} - {dateTo:dd/MM/yyyy}");
                        break;
                    default:
                        Logger.LogWarning($"Unexpected reportTypeComboBox index: {selectedIndex} or unmapped item: {comboBox.Text}. Defaulting dates to current picker values.");
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

        /// <summary>
        /// Handles the ValueChanged event for both the startDatePicker and endDatePicker.
        /// If the user manually changes a date (i.e., not programmatically), this method
        /// automatically switches the selected report type in the `reportTypeComboBox` to "Custom".
        /// </summary>
        /// <param name="sender">The source of the event (either startDatePicker or endDatePicker).</param>
        /// <param name="e">An <see cref="EventArgs"/> that contains no event data.</param>
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
                    if (customIdx != -1)
                    {
                        reportTypeComboBox.SelectedIndex = customIdx;
                    }
                    else
                    {
                        Logger.LogWarning("DatePicker_ValueChanged: 'Custom' item not found in reportTypeComboBox. This should not happen if UI is initialised correctly.");
                    }
                });
            }
        }
        #endregion

        #region Auto-Run UI Handlers
        // Event handlers related to the automated report generation feature.

        /// <summary>
        /// Handles the Click event for the `toggleAutoRunButton`.
        /// This button allows the user to enable or disable the daily automated report generation timer.
        /// The method updates the timer's state and refreshes the UI to reflect the change.
        /// </summary>
        /// <param name="sender">The source of the event (the toggleAutoRunButton).</param>
        /// <param name="e">An <see cref="EventArgs"/> that contains no event data.</param>
        private void toggleAutoRunButton_Click(object sender, EventArgs e)
        {
            dailyCheckTimer.Enabled = !dailyCheckTimer.Enabled;

            bool isAutoRunCompletedForToday = (autoRunStatusLabel.Text?.Contains("Completed") ?? false) ||
                                              (autoRunStatusLabel.Text?.Contains("Done for") ?? false) ||
                                              (autoRunStatusLabel.Text?.Contains("FAILED") ?? false);

            _uiManager.UpdateAutoRunUI(
                dailyCheckTimer.Enabled,
                isAutoRunCompletedForToday,
                darkModeToolStripMenuItem.Checked,
                $"Auto Run: {(dailyCheckTimer.Enabled ? $"Enabled (Next check ~{_currentAutoRunHour}:00)" : "Disabled")}"
            );
            Logger.LogInfo($"AutoRun timer {(dailyCheckTimer.Enabled ? "Enabled" : "Disabled")} by user via toggle button.");
        }

        /// <summary>
        /// Handles the Tick event for the `dailyCheckTimer`.
        /// This event fires at regular intervals (defined by the timer's Interval property).
        /// Its primary purpose is to trigger the <see cref="AutoRunManager.PerformDailyCheckAsync"/>
        /// method if the current time matches the configured auto-run hour and the timer is enabled.
        /// </summary>
        /// <param name="sender">The source of the event (the dailyCheckTimer).</param>
        /// <param name="e">An <see cref="EventArgs"/> that contains no event data.</param>
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
                                                 (CheckConfigValidity() ? "Generate, Process & Email Report" : "Config Error") :
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
        #endregion

        #region Menu Item Event Handlers
        // Event handlers for items in the main menu strip (e.g., Options, Help).

        /// <summary>
        /// Handles the Click event for the `darkModeToolStripMenuItem`.
        /// Toggles the application's visual theme between dark and light mode.
        /// </summary>
        /// <param name="sender">The source of the event (the darkModeToolStripMenuItem).</param>
        /// <param name="e">An <see cref="EventArgs"/> that contains no event data.</param>
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
                autoRunStatusTextToShow = isAutoRunFinalStatusForToday ?
                                          (autoRunStatusLabel.Text ?? $"Auto Run: Enabled (Next check ~{_currentAutoRunHour}:00)") :
                                          $"Auto Run: Enabled (Next check ~{_currentAutoRunHour}:00)";
            }
            else
            {
                autoRunStatusTextToShow = isAutoRunFinalStatusForToday ?
                                          (autoRunStatusLabel.Text ?? "Auto Run: Disabled") :
                                          "Auto Run: Disabled";
            }

            _uiManager.UpdateAutoRunUI(dailyCheckTimer.Enabled, isAutoRunFinalStatusForToday, isChecked, autoRunStatusTextToShow);
            Logger.LogInfo($"Dark Mode toggled via menu. New state: {(isChecked ? "Enabled" : "Disabled")}");
        }

        // Settings Menu Item Click Handler
        private void settingsToolStripMenuItem_Click(object? sender, EventArgs e) // Made sender nullable
        {
            Logger.LogInfo("Form1: Options -> Settings... clicked.");
            try
            {
                using (var settingsForm = new SettingsForm(_configuration, _appSettingsPath, darkModeToolStripMenuItem.Checked))
                {
                    DialogResult result = settingsForm.ShowDialog(this);
                    if (result == DialogResult.OK)
                    {
                        Logger.LogInfo("Form1: SettingsForm closed with OK. Attempting to reload configuration.");
                        if (Program.Configuration is IConfigurationRoot configurationRoot)
                        {
                            configurationRoot.Reload();
                            Logger.LogInfo("Form1: Application configuration reloaded.");
                            FlexibleMessageBox.Show(this, "Settings saved and configuration reloaded.\nA restart may be needed for some changes to fully apply.", "Settings Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            ReinitializeConfigurableComponents(); // Re-read critical settings
                        }
                        else
                        {
                            Logger.LogWarning("Form1: Program.Configuration is not IConfigurationRoot. Cannot reload. Restart required.");
                            FlexibleMessageBox.Show(this, "Settings saved. Please restart the application for changes to take effect.", "Settings Saved - Restart Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }
                    }
                    else { Logger.LogInfo("Form1: SettingsForm closed without saving."); }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Form1: Error opening SettingsForm: {ex.Message}", ex);
                FlexibleMessageBox.Show(this, "Could not open Settings. Check logs.", "Settings Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Handles the Click event for the `helpToolStripMenuItem`.
        /// Displays the application's help information in a separate, non-modal <see cref="HelpForm"/>.
        /// The help content is dynamically generated as RTF (Rich Text Format) to include formatting.
        /// </summary>
        /// <param name="sender">The source of the event (the helpToolStripMenuItem).</param>
        /// <param name="e">An <see cref="EventArgs"/> that contains no event data.</param>
        private void helpToolStripMenuItem_Click(object? sender, EventArgs e) // Made sender nullable
        {
            Logger.LogTrace("Form1: Help menu item clicked.");
            // Construct the title for the help window, now using _appName and _appVersion.
            string helpTitle = $"Help - {_appName} v{_appVersion}";

            StringBuilder helpMessageBuilder = new StringBuilder();
            bool isDarkModeActive = darkModeToolStripMenuItem.Checked; // Get current theme for RTF colour definitions.

            // Define RTF colour codes based on the current theme (dark/light).
            string rtfDefaultTextColor = isDarkModeActive ? @"\red220\green220\blue220;" : @"\red0\green0\blue0;";
            string rtfHeaderColor = isDarkModeActive ? @"\red120\green220\blue250;" : @"\red0\green0\blue128;";
            string rtfSubHeaderColor = isDarkModeActive ? @"\red100\green180\blue220;" : @"\red0\green100\blue0;";
            string rtfAccentColor = isDarkModeActive ? @"\red255\green160\blue160;" : @"\red200\green0\blue0;";
            string rtfBulletColor = isDarkModeActive ? @"\red180\green180\blue180;" : @"\red80\green80\blue80;";
            string rtfCodeColor = isDarkModeActive ? @"\red180\green210\blue180;" : @"\red40\green100\green40;";
            string rtfEmphasisColor = isDarkModeActive ? @"\red255\green210\blue100;" : @"\red139\green69\blue19;";

            // Start the RTF document structure.
            helpMessageBuilder.AppendLine(@"{\rtf1\ansi\ansicpg1252\deff0\nouicompat{\fonttbl{\f0\fnil\fcharset0 Segoe UI;}{\f1\fnil\fcharset2 Symbol;}}");
            helpMessageBuilder.AppendLine($@"{{\colortbl ;{rtfDefaultTextColor}{rtfHeaderColor}{rtfSubHeaderColor}{rtfAccentColor}{rtfBulletColor}{rtfCodeColor}{rtfEmphasisColor}}}");
            helpMessageBuilder.AppendLine(@"\pard\cf1\sa200\sl276\slmult1\f0\fs20"); // Default paragraph settings

            // --- Main Title (Now includes _appName) ---
            helpMessageBuilder.AppendLine($@"\b\fs32\cf2 {_appName} v{_appVersion}\b0\fs20\cf1\par");
            helpMessageBuilder.AppendLine(@"\par");

            // --- Introduction ---
            helpMessageBuilder.AppendLine(@"\b\fs24\cf2 Introduction\b0\fs20\cf1\par");
            helpMessageBuilder.AppendLine(@"Welcome to the Quote Conversion Report Automation programme! This tool is designed to streamline and automate the process of generating, processing, and distributing various quote conversion reports. It aims to reduce manual effort, improve consistency, and provide timely information.\par");
            helpMessageBuilder.AppendLine(@"The programme offers both manual control via a user interface and powerful automated capabilities for scheduled report generation.\par");
            helpMessageBuilder.AppendLine(@"\par");

            // --- How the Application Works ---
            helpMessageBuilder.AppendLine(@"\b\fs24\cf2 How the Application Works (Overview)\b0\fs20\cf1\par");
            helpMessageBuilder.AppendLine(@"The application orchestrates several components:\par");
            helpMessageBuilder.AppendLine(@"   \pard\fi-360\li720{\pncf5\pntext\f1\'B7\tab}\cf1 \b User Interface (UI):\b0  Allows for manual selection of report types, date ranges, and processing options. It also provides access to configuration settings and logs.\par");
            helpMessageBuilder.AppendLine(@"   \pard\fi-360\li720{\pncf5\pntext\f1\'B7\tab}\cf1 \b Crystal Report Wrapper:\b0  An external service used to extract raw report data from the primary business system (via Crystal Reports).\par");
            helpMessageBuilder.AppendLine(@"   \pard\fi-360\li720{\pncf5\pntext\f1\'B7\tab}\cf1 \b Excel Processing:\b0  Utilises templates to process the raw data into final, formatted Excel reports. This includes data cleaning, calculations, and potentially filtering.\par");
            helpMessageBuilder.AppendLine(@"   \pard\fi-360\li720{\pncf5\pntext\f1\'B7\tab}\cf1 \b Email Distribution:\b0  Sends the final reports to configured recipients via SMTP.\par");
            helpMessageBuilder.AppendLine(@"   \pard\fi-360\li720{\pncf5\pntext\f1\'B7\tab}\cf1 \b Automation Engine:\b0  Handles scheduled, automated generation and emailing of predefined reports based on configuration.\par");
            helpMessageBuilder.AppendLine(@"   \pard\fi-360\li720{\pncf5\pntext\f1\'B7\tab}\cf1 \b Configuration Files:\b0  Uses {\cf6 `appsettings.json`} for main application default settings (like SMTP, paths, auto-run hour, daily run status, logging parameters, operational settings) and a separate {\cf6 `autoReportDefinitions.json`} (in the same folder) to store the list of all automated report configurations. User-specific JSON files (in your AppData folder) are used for customisations like email recipients, greetings, and bank holidays.\par");
            helpMessageBuilder.AppendLine(@"\pard\sa200\sl276\slmult1\cf1\par");

            // --- How to Use (Manual) ---
            helpMessageBuilder.AppendLine(@"\b\fs24\cf2 How to Use the Application (Manual Operation)\b0\fs20\cf1\par");
            helpMessageBuilder.AppendLine(@"\b\fs22\cf3 Main Interface Elements:\b0\fs20\cf1\par");
            helpMessageBuilder.AppendLine(@"   \pard\fi-360\li720{\pncf5\pntext\f1\'B7\tab}\cf1 {\cf7\b Report Type:}\b0\cf1  Dropdown to select the desired report period (e.g., Daily, Weekly).\par");
            helpMessageBuilder.AppendLine(@"   \pard\fi-360\li720{\pncf5\pntext\f1\'B7\tab}\cf1 {\cf7\b From / To Dates:}\b0\cf1  Date pickers for defining the report range. These are often auto-filled based on the Report Type.\par");
            helpMessageBuilder.AppendLine(@"   \pard\fi-360\li720{\pncf5\pntext\f1\'B7\tab}\cf1 {\cf7\b Financial Year:}\b0\cf1  Dropdown (visible for certain report types like Weekly or Custom) to specify the financial year context.\par");
            helpMessageBuilder.AppendLine(@"   \pard\fi-360\li720{\pncf5\pntext\f1\'B7\tab}\cf1 {\cf7\b Report Settings Group:}\b0\cf1\par");
            helpMessageBuilder.AppendLine(@"      \pard\fi-720\li1080{\pncf5\pntext\f1\'B7\tab}\cf1 {\cf7\b Send to only Femi?:}\b0\cf1  (Visible for non-Daily, non-Custom manual reports) Restricts email distribution.\par");
            helpMessageBuilder.AppendLine(@"      \pard\fi-720\li1080{\pncf5\pntext\f1\'B7\tab}\cf1 {\cf7\b Skip Sending Email:}\b0\cf1  If ticked, the programme will generate and process the report files locally but will not send an email.\par");
            helpMessageBuilder.AppendLine(@"   \pard\fi-360\li720{\pncf5\pntext\f1\'B7\tab}\cf1 {\cf7\b Action Buttons:}\b0\cf1  Used to generate and process reports. Appearance depends on '1-Click Processing' mode (see Options Menu).\par");
            helpMessageBuilder.AppendLine(@"      \pard\fi-720\li1080{\pncf5\pntext\f1\'B7\tab}\cf1 {\i Standard Mode (2-button):}\b0  {\cf7\b Create Report}\b0\cf1  then {\cf7\b Process & Email Report}\b0\cf1 .\par");
            helpMessageBuilder.AppendLine(@"      \pard\fi-720\li1080{\pncf5\pntext\f1\'B7\tab}\cf1 {\i 1-Click Mode:}\b0  A single {\cf7\b Generate, Process & Email Report}\b0\cf1  button.\par");
            helpMessageBuilder.AppendLine(@"   \pard\fi-360\li720{\pncf5\pntext\f1\'B7\tab}\cf1 {\cf7\b View Buttons:}\b0\cf1  Appear after successful generation: {\cf7\b View Raw Report}\b0\cf1  and {\cf7\b View Processed Analysis}\b0\cf1 .\par");
            helpMessageBuilder.AppendLine($@"   \pard\fi-360\li720{{\pncf5\pntext\f1\'B7\tab}}\cf1 {{\cf7\b Enable/Disable Daily Auto Run @ {{\b {_currentAutoRunHour}:00\b0}}:}}\b0\cf1  Toggles the automated daily report generation feature.\par");
            helpMessageBuilder.AppendLine(@"   \pard\fi-360\li720{\pncf5\pntext\f1\'B7\tab}\cf1 {\cf7\b Status Bar:}\b0\cf1  Displays the current status of operations (left) and auto-run status (right).\par");
            helpMessageBuilder.AppendLine(@"\pard\sa200\sl276\slmult1\cf1\par");

            helpMessageBuilder.AppendLine(@"\b\fs22\cf3 Step-by-Step Guide for Manual Report Generation:\b0\fs20\cf1\par");
            helpMessageBuilder.AppendLine(@"\b 1. Select Report Type:\b0\par");
            helpMessageBuilder.AppendLine(@"Choose from the {\cf7\b Report Type}\b0\cf1  dropdown. Dates will often adjust automatically. Bank holidays (England & Wales, plus custom) are considered for 'previous working day' calculations.\par");
            helpMessageBuilder.AppendLine(@"   \pard\fi-360\li720{\pncf5\pntext\f1\'B7\tab}\cf1 {\cf7\b Daily:}\b0\cf1  Report for the {\i previous working day}.\par");
            helpMessageBuilder.AppendLine(@"   \pard\fi-360\li720{\pncf5\pntext\f1\'B7\tab}\cf1 {\cf7\b Daily (5days >= £1000):}\b0\cf1  Covers the {\i previous five working days}. After raw data generation, it filters for estimates with a 'Net Value' of £1000 or more before final analysis.\par");
            helpMessageBuilder.AppendLine(@"   \pard\fi-360\li720{\pncf5\pntext\f1\'B7\tab}\cf1 {\cf7\b Weekly:}\b0\cf1  Covers a {\i 15-day rolling period ending on the current day}. Data is appended to a central Excel file used by Power BI.\par");
            helpMessageBuilder.AppendLine(@"   \pard\fi-360\li720{\pncf5\pntext\f1\'B7\tab}\cf1 {\cf7\b Monthly:}\b0\cf1  Report for the {\i previous full calendar month}.\par");
            helpMessageBuilder.AppendLine(@"   \pard\fi-360\li720{\pncf5\pntext\f1\'B7\tab}\cf1 {\cf7\b Quarterly:}\b0\cf1  Report for the {\i previous full calendar quarter}.\par");
            helpMessageBuilder.AppendLine($@"   \pard\fi-360\li720{{\pncf5\pntext\f1\'B7\tab}}\cf1 {{\cf7\b Annual:}}\b0\cf1  Report for the {{\i previous full financial year ({_configuration.GetValue<int>("OperationalParameters:FinancialYearStartDay", 1)}/{_configuration.GetValue<int>("OperationalParameters:FinancialYearStartMonth", 5)} - 30/4 or next day)}}.\par");
            helpMessageBuilder.AppendLine(@"   \pard\fi-360\li720{\pncf5\pntext\f1\'B7\tab}\cf1 {\cf7\b Custom:}\b0\cf1  Allows manual selection of 'From' and 'To' dates. If you change dates for any other report type, it will automatically switch to 'Custom'.\par");
            helpMessageBuilder.AppendLine(@"\pard\sa200\sl276\slmult1\cf1\par");
            helpMessageBuilder.AppendLine(@"\b 2. Adjust Dates (Optional):\b0\par");
            helpMessageBuilder.AppendLine(@"If 'Custom' is selected, or if you wish to override the auto-calculated dates for other types, use the {\cf7\b From Date}\b0\cf1  and {\cf7\b To Date}\b0\cf1  pickers. Changing dates will set the Report Type to 'Custom'.\par");
            helpMessageBuilder.AppendLine(@"\par");
            helpMessageBuilder.AppendLine(@"\b 3. Select Financial Year (If Applicable):\b0\par");
            helpMessageBuilder.AppendLine(@"For 'Weekly' or 'Custom' reports, the {\cf7\b Financial Year}\b0\cf1  dropdown may be visible. Select the appropriate financial year if the report data needs to be associated with a specific year for Power BI updates or analysis.\par");
            helpMessageBuilder.AppendLine(@"\par");
            helpMessageBuilder.AppendLine(@"\b 4. Configure Report Settings:\b0\par");
            helpMessageBuilder.AppendLine(@"   \pard\fi-360\li720{\pncf5\pntext\f1\'B7\tab}\cf1 Tick {\cf7\b Send to only Femi?:}\b0\cf1  if you want to restrict the email distribution to a predefined IT/admin list. This is typically used for testing or specific non-standard reports.\par");
            helpMessageBuilder.AppendLine(@"   \pard\fi-360\li720{\pncf5\pntext\f1\'B7\tab}\cf1 Tick {\cf7\b Skip Sending Email}\b0\cf1  if you only want to generate the report files locally and do not wish for an email to be sent.\par");
            helpMessageBuilder.AppendLine(@"\pard\sa200\sl276\slmult1\cf1\par");
            helpMessageBuilder.AppendLine(@"\b 5. Process the Report:\b0\par");
            helpMessageBuilder.AppendLine(@"   \pard\fi-360\li720{\pncf5\pntext\f1\'B7\tab}\cf1 If using {\i Standard Mode}, first click {\cf7\b Create Report}\b0\cf1 . Wait for the status to indicate completion. Then, click {\cf7\b Process & Email Report}\b0\cf1 .\par");
            helpMessageBuilder.AppendLine(@"   \pard\fi-360\li720{\pncf5\pntext\f1\'B7\tab}\cf1 If using {\i 1-Click Mode} (see Options Menu to enable), click the single {\cf7\b Generate, Process & Email Report}\b0\cf1  button.\par");
            helpMessageBuilder.AppendLine(@"\pard\sa200\sl276\slmult1\cf1\par");
            helpMessageBuilder.AppendLine(@"\b 6. Manual Excel Refresh (for Monthly, Quarterly, Annual, Custom reports):\b0\par");
            helpMessageBuilder.AppendLine(@"For these report types, the Excel template contains PivotTables that require manual refreshing after the data is populated. You will be prompted:\par");
            helpMessageBuilder.AppendLine(@"   \pard\fi-360\li720{\pncf5\pntext\f1\'B7\tab}\cf1 The Excel file will open automatically.\par");
            helpMessageBuilder.AppendLine(@"   \pard\fi-360\li720{\pncf5\pntext\f1\'B7\tab}\cf1 If prompted by Excel, click {\b 'Enable Editing'}.\par");
            helpMessageBuilder.AppendLine(@"   \pard\fi-360\li720{\pncf5\pntext\f1\'B7\tab}\cf1 Specifically, ensure PivotTables on the 'OrderPivot' and 'Estimate Success PivotTable' sheets are updated. You may need to right-click them and select 'Refresh'. Check any Slicers as well.\par");
            helpMessageBuilder.AppendLine(@"   \pard\fi-360\li720{\pncf5\pntext\f1\'B7\tab}\cf1 {\b SAVE}\b0  the Excel file.\par");
            helpMessageBuilder.AppendLine(@"   \pard\fi-360\li720{\pncf5\pntext\f1\'B7\tab}\cf1 {\b CLOSE}\b0  Excel.\par");
            helpMessageBuilder.AppendLine(@"   \pard\fi-360\li720{\pncf5\pntext\f1\'B7\tab}\cf1 The application will then ask you to confirm if you want to proceed with emailing the (now refreshed) report.\par");
            helpMessageBuilder.AppendLine(@"\pard\sa200\sl276\slmult1\cf1\par");
            helpMessageBuilder.AppendLine(@"\b 7. Viewing Reports:\b0\par");
            helpMessageBuilder.AppendLine(@"   \pard\fi-360\li720{\pncf5\pntext\f1\'B7\tab}\cf1 After the raw report is generated, the {\cf7\b View Raw Report}\b0\cf1  button becomes active. Click it to open the raw Excel data file.\par");
            helpMessageBuilder.AppendLine(@"   \pard\fi-360\li720{\pncf5\pntext\f1\'B7\tab}\cf1 After the report is processed (and emailed, if not skipped), the {\cf7\b View Processed Analysis}\b0\cf1  button becomes active. Click it to open the final, formatted Excel report.\par");
            helpMessageBuilder.AppendLine(@"\pard\sa200\sl276\slmult1\cf1\par");

            // --- Options Menu Explained (UPDATED with Settings) ---
            helpMessageBuilder.AppendLine(@"\b\fs24\cf2 Options & Settings Menus Explained\b0\fs20\cf1\par");
            helpMessageBuilder.AppendLine(@"The menus provide access to various settings and tools:\par");
            helpMessageBuilder.AppendLine(@"\b Main Menu:\b0\par");
            helpMessageBuilder.AppendLine(@"   \pard\fi-360\li720{\pncf5\pntext\f1\'B7\tab}\cf1 {\cf7\b Options}\b0\cf1  (Sub-menu):\par");
            helpMessageBuilder.AppendLine(@"      \pard\fi-720\li1080{\pncf5\pntext\f1\'B7\tab}\cf1 {\cf7\b Enable 1-Click Processing:}\b0\cf1  Toggles between two-button and single-button processing mode.\par");
            helpMessageBuilder.AppendLine($@"      \pard\fi-720\li1080{{\pncf5\pntext\f1\'B7\tab}}\cf1 {{\cf7\b Set Auto-Run Hour...:}}\b0\cf1  Change the hour (0-23) for the daily auto-run check. Current: {{\b {_currentAutoRunHour}:00\b0}}.\par");
            helpMessageBuilder.AppendLine(@"      \pard\fi-720\li1080{\pncf5\pntext\f1\'B7\tab}\cf1 {\cf7\b Manage Automated Reports...:}\b0\cf1  Manage all automated report definitions (add, edit, delete, enable/disable).\par");
            helpMessageBuilder.AppendLine(@"      \pard\fi-720\li1080{\pncf5\pntext\f1\'B7\tab}\cf1 {\cf7\b Dark Mode:}\b0\cf1  Toggles the application's visual theme.\par");
            helpMessageBuilder.AppendLine(@"      \pard\fi-720\li1080{\pncf5\pntext\f1\'B7\tab}\cf1 {\cf7\b View Configuration:}\b0\cf1  Displays critical file paths and settings from `appsettings.json`.\par");
            helpMessageBuilder.AppendLine(@"      \pard\fi-720\li1080{\pncf5\pntext\f1\'B7\tab}\cf1 {\cf7\b Validate Configuration:}\b0\cf1  Quick check of essential configurations.\par");
            helpMessageBuilder.AppendLine(@"      \pard\fi-720\li1080{\pncf5\pntext\f1\'B7\tab}\cf1 {\cf7\b Manage Custom Bank Holidays:}\b0\cf1  Add/remove custom bank holidays.\par");
            helpMessageBuilder.AppendLine(@"      \pard\fi-720\li1080{\pncf5\pntext\f1\'B7\tab}\cf1 {\cf7\b Manage Email Recipients:}\b0\cf1  Customise *user-specific overrides* for email lists.\par");
            helpMessageBuilder.AppendLine(@"      \pard\fi-720\li1080{\pncf5\pntext\f1\'B7\tab}\cf1 {\cf7\b Manage Email Greetings:}\b0\cf1  Customise *user-specific overrides* for email greetings.\par");
            helpMessageBuilder.AppendLine(@"      \pard\fi-720\li1080{\pncf5\pntext\f1\'B7\tab}\cf1 {\cf7\b Open Logs Folder:}\b0\cf1  Opens the directory with detailed log files.\par");
            helpMessageBuilder.AppendLine(@"      \pard\fi-720\li1080{\pncf5\pntext\f1\'B7\tab}\cf1 {\cf7\b Open Auto Report Definitions File:}\b0\cf1  Opens the {\cf6 `autoReportDefinitions.json`} file for viewing.\par");
            helpMessageBuilder.AppendLine($@"      \pard\fi-720\li1080{{\pncf5\pntext\f1\'B7\tab}}\cf1 {{\cf7\b Edit appsettings.json:}}\b0\cf1  Opens the main configuration file (`appsettings.json`) for advanced settings. {{\i\cf4 Use with caution!}}\cf1\par");
            helpMessageBuilder.AppendLine(@"      \pard\fi-720\li1080{\pncf5\pntext\f1\'B7\tab}\cf1 {\cf7\b Exit:}\b0\cf1  Closes the application.\par");
            helpMessageBuilder.AppendLine(@"   \pard\fi-360\li720{\pncf5\pntext\f1\'B7\tab}\cf1 {\cf7\b Settings...:}\b0\cf1  {{\b (NEW)}}\\ Opens a new window to view and modify all application-wide default settings stored in {{\cf6 `appsettings.json`}}. This includes default paths, SMTP server details, default logging levels, operational parameters (like timeouts, financial year start), IPC settings, and the default AutoRun check hour. Changes saved here affect the application's default behavior for all users unless overridden by user-specific settings (e.g., custom email lists). {{\i\cf4 Modifying these settings requires understanding their impact. Some changes may require an application restart.}}\cf1\par");
            helpMessageBuilder.AppendLine(@"   \pard\fi-360\li720{\pncf5\pntext\f1\'B7\tab}\cf1 {\cf7\b Help:}\b0\cf1  Displays this help information.\par");
            helpMessageBuilder.AppendLine(@"\pard\sa200\sl276\slmult1\cf1\par");

            // --- Automated Features ---
            helpMessageBuilder.AppendLine(@"\b\fs24\cf2 Automated Features\b0\fs20\cf1\par");
            helpMessageBuilder.AppendLine(@"\b\fs22\cf3 Auto-Run Feature:\b0\fs20\cf1\par");
            helpMessageBuilder.AppendLine($@"When the {{\cf7\b Enable Daily Auto Run @ {{\b {_currentAutoRunHour}:00\b0}}}}\b0\cf1  button shows green (enabled), the application will automatically check around {{\b {_currentAutoRunHour}:00\b0}}\b0\cf1  each day to run any pending automated reports.\par");
            helpMessageBuilder.AppendLine(@"Automated reports are defined and managed via the {\cf7\b Options -> Manage Automated Reports...}\b0\cf1  window. In that window, you can add new report types, edit their properties (like schedule, template, email details), and enable or disable them individually.\par");
            helpMessageBuilder.AppendLine(@"The definitions for these automated reports are stored in a separate file named {\cf6 `autoReportDefinitions.json`}, located in the same directory as `appsettings.json`.\par");
            helpMessageBuilder.AppendLine(@"Automated reports use email recipients and greetings configured for their specific 'Recipient Category Key' and 'Greeting Key' (see 'Manage Email Recipients' and 'Manage Email Greetings' forms, and default settings in `appsettings.json`). The status of the auto-run process is displayed in the right-hand side of the status bar.\par");
            helpMessageBuilder.AppendLine(@"The application keeps track of which reports have successfully run for the day in `appsettings.json` (under the `AutoRunProcess:DailyRunStatus` section) to avoid duplicate runs if the application is restarted.\par");
            helpMessageBuilder.AppendLine(@"\pard\sa200\sl276\slmult1\cf1\par");

            helpMessageBuilder.AppendLine(@"\b\fs22\cf3 Automated Archiving:\b0\fs20\cf1\par");
            helpMessageBuilder.AppendLine(@"The application performs automated archiving on start-up:\par");
            helpMessageBuilder.AppendLine(@"   \pard\fi-360\li720{\pncf5\pntext\f1\'B7\tab}\cf1 \b Log Files:\b0  Log files older than the configured number of days (default 7, see {{\cf6 Logging:LogArchiveOlderThanDays}} in `appsettings.json`) are moved to an 'Archive' subfolder.\par");
            helpMessageBuilder.AppendLine(@"   \pard\fi-360\li720{\pncf5\pntext\f1\'B7\tab}\cf1 \b Report Files:\b0 \par");
            helpMessageBuilder.AppendLine(@"      \pard\fi-720\li1080{\pncf5\pntext\f1\'B7\tab}\cf1 {\i Final Reports:}\b0  Previous calendar year folders are moved into a main archive folder (name configurable via {{\cf6 OperationalParameters:ReportArchiveFolderName}}).\par");
            helpMessageBuilder.AppendLine($@"      \pard\fi-720\li1080{{\pncf5\pntext\f1\'B7\tab}}\cf1 {{\i Raw Reports:}}\b0  Files older than a configured number of days (default 30, see {{\cf6 OperationalParameters:ArchiveRawReportsOlderThanDays}}) are moved into an 'Archive\\YYYY-MM' subfolder within their respective report type directory.\par");
            helpMessageBuilder.AppendLine(@"\pard\sa200\sl276\slmult1\cf1\par");

            helpMessageBuilder.AppendLine(@"\b\fs24\cf2 Configuration Files Overview\b0\fs20\cf1\par");
            helpMessageBuilder.AppendLine(@"The application uses several configuration files:\par");
            helpMessageBuilder.AppendLine(@"   \pard\fi-360\li720{\pncf5\pntext\f1\'B7\tab}\cf1 {\cf6\b appsettings.json:}\b0\cf1  The main configuration file. Stores application-wide defaults: critical paths, SMTP details, default email recipients/greetings, auto-run operational state (like `LastRunDate`, `DailyRunStatus`, `CheckHour` under `AutoRunProcess`), logging levels, and other operational parameters. Most of these can now be edited via the new {{\cf7\b Settings...}}\b0\cf1  menu. {{\cf4\i Modifying this file directly requires caution.}}\b0\cf1\par");
            helpMessageBuilder.AppendLine(@"   \pard\fi-360\li720{\pncf5\pntext\f1\'B7\tab}\cf1 {\cf6\b autoReportDefinitions.json:}\b0\cf1  Located in the same directory as `appsettings.json`, this file stores the list of all automated report definitions. It is managed via the {\cf7\b Options -> Manage Automated Reports...}\b0\cf1  window.\par");
            helpMessageBuilder.AppendLine(@"   \pard\fi-360\li720{\pncf5\pntext\f1\'B7\tab}\cf1 \b User-Specific JSON Files:\b0  Stored in your user profile's AppData directory (typically {\cf6 %APPDATA%\\HarlowSolutions\\QuoteConversionReportAutomation\\}). They allow for personal customisations:\par");
            helpMessageBuilder.AppendLine(@"      \pard\fi-720\li1080{\pncf5\pntext\f1\'B7\tab}\cf1 {\cf6 user_email_settings.json:}\b0\cf1  Custom email recipient lists (overrides app defaults).\par");
            helpMessageBuilder.AppendLine(@"      \pard\fi-720\li1080{\pncf5\pntext\f1\'B7\tab}\cf1 {\cf6 user_greeting_settings.json:}\b0\cf1  Custom email greetings (overrides app defaults).\par");
            helpMessageBuilder.AppendLine(@"      \pard\fi-720\li1080{\pncf5\pntext\f1\'B7\tab}\cf1 {\cf6 custom_bank_holidays.json:}\b0\cf1  Custom bank holidays.\par");
            helpMessageBuilder.AppendLine(@"\pard\sa200\sl276\slmult1\cf1\par");

            helpMessageBuilder.AppendLine(@"\b\fs24\cf2 Troubleshooting\b0\fs20\cf1\par");
            helpMessageBuilder.AppendLine(@"If you encounter issues, consider the following steps:\par");
            helpMessageBuilder.AppendLine(@"   \pard\fi-360\li720{\pncf5\pntext\f1\'B7\tab}\cf1 \b ""Config Error"" Status:\b0  Check essential file paths via {\cf7\b Options -> View Configuration}\b0\cf1  or edit them via {\cf7\b Settings...}\b0\cf1 . Ensure files/folders exist and are accessible.\par");
            helpMessageBuilder.AppendLine(@"   \pard\fi-360\li720{\pncf5\pntext\f1\'B7\tab}\cf1 \b Report Generation Fails:\b0  Ensure Crystal Report Wrapper service can run (check path in Settings), .rpt file path is correct, and check logs.\par");
            helpMessageBuilder.AppendLine(@"   \pard\fi-360\li720{\pncf5\pntext\f1\'B7\tab}\cf1 \b Excel Processing Fails:\b0  Check template paths/files (in Settings) and write permissions for output directories.\par");
            helpMessageBuilder.AppendLine(@"   \pard\fi-360\li720{\pncf5\pntext\f1\'B7\tab}\cf1 \b Email Sending Fails:\b0  Verify SMTP settings (in Settings) and network connectivity. Check recipient lists and greetings via their respective 'Manage...' options.\par");
            helpMessageBuilder.AppendLine(@"   \pard\fi-360\li720{\pncf5\pntext\f1\'B7\tab}\cf1 \b Auto-Run Not Working:\b0  Confirm main auto-run is enabled, individual reports are enabled and correctly configured (via {\cf7\b Manage Automated Reports...}\b0\cf1 ), and check the auto-run hour (in Settings). Review `appsettings.json` (`AutoRunProcess` section) and `autoReportDefinitions.json` if issues persist.\par");
            helpMessageBuilder.AppendLine(@"   \pard\fi-360\li720{\pncf5\pntext\f1\'B7\tab}\cf1 {\cf4\b Check Application Logs:}\b0\cf1  Always the best source for detailed errors. Access via {\cf7\b Options -> Open Logs Folder}\b0\cf1 .\par");
            helpMessageBuilder.AppendLine(@"   \pard\fi-360\li720{\pncf5\pntext\f1\'B7\tab}\cf1 \b Incorrect Formulae in Excel:\b0  The application copies formulae from row 6 of the 'Analysis' sheet in the template. Ensure these are correct in your template files.\par");
            helpMessageBuilder.AppendLine(@"   \pard\fi-360\li720{\pncf5\pntext\f1\'B7\tab}\cf1 \b User Settings Not Applying:\b0  Check the JSON files in your AppData folder mentioned under 'Configuration Files Overview'. Deleting a corrupted user settings file reverts that specific setting type to application defaults.\par");
            helpMessageBuilder.AppendLine(@"   \pard\fi-360\li720{\pncf5\pntext\f1\'B7\tab}\cf1 \b Manual Refresh Reports (Pivots/Slicers):\b0  For Monthly, Quarterly, Annual, Custom reports, carefully follow on-screen prompts to {\b Enable Editing}, {\b Refresh Pivots/Slicers} in Excel, {\b Save}, and {\b Close} before confirming in the app.\par");
            helpMessageBuilder.AppendLine(@"   \pard\fi-360\li720{\pncf5\pntext\f1\'B7\tab}\cf1 \b Contact IT Dept:\b0  If problems persist, contact IT with details and relevant logs.\par");
            helpMessageBuilder.AppendLine(@"\pard\sa200\sl276\slmult1\cf1\par");

            helpMessageBuilder.AppendLine($@"Thank you for using the {_appName} program!\par");
            helpMessageBuilder.AppendLine(@"}");

            string helpMessage = helpMessageBuilder.ToString();

            try
            {
                if (_helpFormInstance == null || _helpFormInstance.IsDisposed)
                {
                    _helpFormInstance = new HelpForm(helpTitle, helpMessage, darkModeToolStripMenuItem.Checked);
                    _helpFormInstance.FormClosed += (s, args) => _helpFormInstance = null;
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
                FlexibleMessageBox.Show(this, "Could not display help window. Please check application logs.", "Help Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Handles the Click event for the `viewConfigToolStripMenuItem`.
        /// Displays a summary of critical application configuration settings (file paths, auto-run hour, etc.)
        /// in a <see cref="FlexibleMessageBox"/>. This is useful for diagnostics.
        /// Reads values from the reorganized configuration structure.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">An <see cref="EventArgs"/> that contains no event data.</param>
        private void viewConfigToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Logger.LogInfo("Options -> View Configuration clicked.");
            bool configValid = CheckConfigValidity();
            var sb = new System.Text.StringBuilder();

            sb.AppendLine("Configuration Details (Paths are relative to user profile where applicable):");
            sb.AppendLine("--------------------------------------------------");
            sb.AppendLine($"1. Crystal Report Path (.rpt): '{CrystalReportLocation}'");
            sb.AppendLine($"   - Exists: {File.Exists(CrystalReportLocation)}");
            sb.AppendLine($"2. Wrapper EXE Path: '{Path.GetFullPath(_configuration["Paths:WrapperExecutable"] ?? string.Empty)}'");
            sb.AppendLine($"   - Exists: {File.Exists(Path.GetFullPath(_configuration["Paths:WrapperExecutable"] ?? string.Empty))}");
            sb.AppendLine($"3. Template Base Directory: '{ExcelTemplateBaseDir}'");
            sb.AppendLine($"   - Exists: {Directory.Exists(ExcelTemplateBaseDir)}");
            sb.AppendLine($"4. Raw Report Export Base Directory: '{RawReportExportBaseDir}'");
            sb.AppendLine($"   - Exists: {Directory.Exists(RawReportExportBaseDir)}");
            sb.AppendLine($"5. Final Excel Save Location Base: '{ExcelFinalSaveLocation}'");
            sb.AppendLine($"   - Exists: {Directory.Exists(ExcelFinalSaveLocation)}");
            sb.AppendLine($"6. Auto-Run Check Hour: {_configuration.GetValue<int>("AutoRunProcess:CheckHour", _currentAutoRunHour)} (Current in-memory: {_currentAutoRunHour})");
            sb.AppendLine($"7. Automated Report Definitions File: '{Path.Combine(Path.GetDirectoryName(_appSettingsPath) ?? "", ReportDefinitionsFileName)}'");
            sb.AppendLine($"   - Exists: {File.Exists(Path.Combine(Path.GetDirectoryName(_appSettingsPath) ?? "", ReportDefinitionsFileName))}");


            string baseLogDir = ConfiguredLogDirectoryBase;
            string actualUserLogDir = string.IsNullOrEmpty(baseLogDir)
                ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "QCRA", "Logs", Environment.UserName) // Using AppName from config for folder
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

        /// <summary>
        /// Handles the Click event for the `validateConfigToolStripMenuItem`.
        /// Performs a quick validation of essential configuration paths (Crystal Report, Wrapper EXE)
        /// and updates the main status bar with the result.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">An <see cref="EventArgs"/> that contains no event data.</param>
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

        /// <summary>
        /// Handles the Click event for the `openLogsToolStripMenuItem`.
        /// Opens the application's user-specific log folder in the default File Explorer.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">An <see cref="EventArgs"/> that contains no event data.</param>
        private void openLogsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Logger.LogInfo("Options -> Open Logs Folder clicked.");
            try
            {
                string baseLogDir = ConfiguredLogDirectoryBase; // Reads from "Paths:LogDirectoryBase"
                string fallbackLogDir = _configuration.GetValue<string>("Logging:DefaultFallbackLogDirectory", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "QCRA", "Logs"))!;

                string actualUserLogDir;
                if (!string.IsNullOrEmpty(baseLogDir) && Directory.Exists(Environment.ExpandEnvironmentVariables(baseLogDir)))
                {
                    actualUserLogDir = Path.Combine(Environment.ExpandEnvironmentVariables(baseLogDir), string.Join("_", Environment.UserName.Split(Path.GetInvalidFileNameChars())));
                }
                else
                {
                    actualUserLogDir = Path.Combine(Environment.ExpandEnvironmentVariables(fallbackLogDir), string.Join("_", Environment.UserName.Split(Path.GetInvalidFileNameChars())));
                    Logger.LogWarning($"Primary log directory base ('{baseLogDir}') not found or invalid. Using fallback: '{actualUserLogDir}'");
                }

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

        /// <summary>
        /// Handles the Click event for the `editConfigToolStripMenuItem`.
        /// Opens the main application configuration file (`appsettings.json`) in the system's
        /// default text editor.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">An <see cref="EventArgs"/> that contains no event data.</param>
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

        /// <summary>
        /// Handles the Click event for the `exitToolStripMenuItem`.
        /// Closes the application.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">An <see cref="EventArgs"/> that contains no event data.</param>
        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Logger.LogInfo("Options -> Exit clicked. Closing application.");
            Close();
        }

        /// <summary>
        /// Handles the Click event for the `manageCustomBankHolidaysToolStripMenuItem`.
        /// Opens the <see cref="ManageBankHolidaysForm"/> dialog, allowing the user to
        /// add, view, or remove custom bank holidays.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">An <see cref="EventArgs"/> that contains no event data.</param>
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

        /// <summary>
        /// Handles the Click event for the `manageEmailRecipientsToolStripMenuItem`.
        /// Opens the <see cref="ManageEmailRecipientsForm"/> dialog, allowing the user to
        /// customise email recipient lists for various report scenarios.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">An <see cref="EventArgs"/> that contains no event data.</param>
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

        /// <summary>
        /// Handles the Click event for the `manageGreetingsToolStripMenuItem`.
        /// Opens the <see cref="ManageGreetingsForm"/> dialog, allowing the user to
        /// customise email greeting messages.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">An <see cref="EventArgs"/> that contains no event data.</param>
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

        /// <summary>
        /// Handles the Click event for the `enable1ClickProcessingToolStripMenuItem`.
        /// Toggles the "1-Click Processing" mode, which changes the main action button layout
        /// (either one combined button or separate "Create" and "Process" buttons).
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">An <see cref="EventArgs"/> that contains no event data.</param>
        private void enable1ClickProcessingToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Update1ClickProcessingModeUI();
            string mainButtonTextForReset = enable1ClickProcessingToolStripMenuItem.Checked ?
                                            (CheckConfigValidity() ? "Generate, Process & Email Report" : "Config Error") :
                                            (CheckConfigValidity() ? "Create Report" : "Config Error");
            ResetUIStateOnError(mainButtonTextForReset);
            Logger.LogInfo($"1-Click Processing Mode {(enable1ClickProcessingToolStripMenuItem.Checked ? "Enabled" : "Disabled")}.");
        }

        /// <summary>
        /// Handles the Click event for the `setAutoRunHourToolStripMenuItem`.
        /// Prompts the user to enter a new hour (0-23) for the daily automated report check.
        /// If valid input is provided and the hour is different from the current setting,
        /// it updates the configuration file and the application's runtime state.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">An <see cref="EventArgs"/> that contains no event data.</param>
        private async void setAutoRunHourToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Logger.LogInfo("Options -> Set Auto-Run Hour clicked.");
            string currentHourPrompt = _currentAutoRunHour.ToString();

            string? inputText = Interaction.InputBox(
                "Enter the new hour (0-23) for the daily auto-run check:",
                "Set Auto-Run Hour",
                currentHourPrompt
            );

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
                            Logger.LogError("Failed to save the new auto-run hour to configuration. Check AutoRunManager logs and file permissions for appsettings.json.");
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

        // REMOVED: Old specific auto-run toggle menu item handlers (enableStandardDailyAutoReportToolStripMenuItem_Click, etc.)
        // This functionality is now handled by ManageAutoReportDefinitionsForm.

        /// <summary>
        /// Handles the Click event for the new "Manage Automated Reports" menu item.
        /// Opens the ManageAutoReportDefinitionsForm.
        /// </summary>
        private void manageAutomatedReportsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Logger.LogInfo("Options -> Manage Automated Reports clicked.");
            try
            {
                // The _appSettingsPath is passed to determine the directory for autoReportDefinitions.json
                using (var manageForm = new ManageAutoReportDefinitionsForm(_configuration, _appSettingsPath, darkModeToolStripMenuItem.Checked))
                {
                    manageForm.ShowDialog(this);
                    // After the form closes, reload definitions in AutoRunManager in case changes were saved.
                    // The form's DialogResult isn't strictly checked here, assuming any interaction might lead to changes
                    // that should be picked up by the AutoRunManager for its next cycle.
                    // A more refined approach might involve the form returning a specific DialogResult if changes were saved.
                    _autoRunManager.ReloadReportDefinitions();
                    Logger.LogInfo("Report definitions reloaded in AutoRunManager after ManageAutoReportDefinitionsForm closed.");
                }
                Logger.LogInfo("ManageAutoReportDefinitionsForm closed.");
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error opening or handling ManageAutoReportDefinitionsForm: {ex.Message}", ex);
                FlexibleMessageBox.Show(this, "Could not open the automated report management window. Please check logs.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Handles the Click event for opening the autoReportDefinitions.json file.
        /// </summary>
        private void openAutoReportDefinitionsFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Logger.LogInfo("Options -> Open Auto Report Definitions File clicked.");
            try
            {
                // _autoReportDefinitionsFilePath is already constructed in Form1 constructor
                if (File.Exists(_autoReportDefinitionsFilePath))
                {
                    Process.Start(new ProcessStartInfo(_autoReportDefinitionsFilePath) { UseShellExecute = true });
                }
                else
                {
                    FlexibleMessageBox.Show(this, $"The auto report definitions file was not found:\n{_autoReportDefinitionsFilePath}", "File Not Found", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error opening auto report definitions file '{_autoReportDefinitionsFilePath}': {ex.Message}", ex);
                FlexibleMessageBox.Show(this, $"Could not open the auto report definitions file.\nError: {ex.Message}", "File Open Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        #endregion // Menu Item Event Handlers

        #endregion // UI Event Handlers (consolidated)

        #region Helper Methods
        // This region contains various private helper methods used by the Form1 class
        // to encapsulate common logic, improve readability, and manage internal state.

        /// <summary>
        /// Re-initializes components or re-reads settings within Form1 that might have changed
        /// after the application configuration has been reloaded from appsettings.json.
        /// This method is called after settings are successfully saved via the SettingsForm.
        /// </summary>
        private void ReinitializeConfigurableComponents()
        {
            Logger.LogInfo("Form1: Re-initializing/refreshing configurable components due to settings change...");

            // Re-read and apply ApplicationInfo for the form title
            string appName = _configuration.GetValue<string>("ApplicationInfo:AppName", "QCRA - Quote Conversion Report Automation")!;
            string appVersion = _configuration.GetValue<string>("ApplicationInfo:AppVersion", "1.9.x")!;
            this.Text = $"{appName} - {(IsDebug ? "DEBUG" : "RELEASE")} - v{appVersion}";
            Logger.LogInfo($"Form1: Title updated to: {this.Text}");

            // Example: Re-read auto-run hour and update related UI elements.
            _currentAutoRunHour = _configuration.GetValue<int>("AutoRunProcess:CheckHour", 8); // Default to 8 AM
            _uiManager.SetAutoRunHour(_currentAutoRunHour);
            // For the status text, we might not know if a run was "Done for today" without more complex state,
            // so we'll just update based on whether the timer is enabled.
            bool isAutoRunStatusFinal = (autoRunStatusLabel.Text?.Contains("Completed") ?? false) ||
                                        (autoRunStatusLabel.Text?.Contains("Done for") ?? false) ||
                                        (autoRunStatusLabel.Text?.Contains("FAILED") ?? false);
            _uiManager.UpdateAutoRunUI(dailyCheckTimer.Enabled, isAutoRunStatusFinal, darkModeToolStripMenuItem.Checked,
                $"Auto Run: {(dailyCheckTimer.Enabled ? $"Enabled (Next check ~{_currentAutoRunHour}:00)" : "Disabled")}");
            Logger.LogInfo($"Form1: Auto-run hour re-read and UI updated to: {_currentAutoRunHour}:00");

            // Re-check overall configuration validity for UI elements (button states, status messages, etc.)
            // This is important because paths (like to the wrapper or .rpt file) might have changed.
            bool configIsValid = CheckConfigValidity(); // CheckConfigValidity already uses _configuration.
            _uiManager.ResetButtonStatesAfterTypeChange(configIsValid); // This resets buttons based on current state.
            Update1ClickProcessingModeUI(); // Ensure the 1-Click/2-button mode is correctly displayed.

            // Note: Path properties in Form1 (like CrystalReportLocation, ExcelFinalSaveLocation, etc.)
            // are get-only properties that read directly from _configuration each time they are accessed.
            // So, they will automatically use the new values after _configuration.Reload() has been called.
            // No explicit action is needed here to "refresh" those properties themselves.

            // IMPORTANT: Other managers or services (like EmailUtility, NamedPipeCommunicator, ExcelCopyData, Logger)
            // are initialized once in Form1's constructor and cache their configuration values at that time.
            // If settings relevant to *them* change (e.g., SMTP server, pipe name, log file path format),
            // they will NOT pick up these changes just from IConfiguration.Reload() unless:
            //   a) They are re-instantiated (which would be a significant change here).
            //   b) They expose a public method like `RefreshConfiguration(IConfiguration newConfig)` which Form1 could call.
            //   c) They are changed to read from IConfiguration dynamically every time a setting is needed (less performant for frequently accessed settings).
            // For now, we've mostly focused on Form1's direct dependencies. A full application restart is the most
            // foolproof way to ensure *all* components pick up *all* changes from appsettings.json.
            // The message box already informs the user about this.

            Logger.LogInfo("Form1: Configurable components within Form1 have been refreshed with new settings.");
        }

        /// <summary>
        /// Gets the integer index corresponding to the currently selected report type in the `reportTypeComboBox`.
        /// It first tries to match the `selectedText` (if provided), then the `ComboBox.SelectedItem`,
        /// and finally `ComboBox.Text`. This provides robustness.
        /// </summary>
        /// <param name="selectedText">Optional. The text of the selected item. If null or empty,
        /// the method will use the `reportTypeComboBox`'s current selection or text.</param>
        /// <returns>The integer index for the report type (e.g., <see cref="DailyReportIndex"/>).
        /// Returns the `reportTypeComboBox.SelectedIndex` as a fallback if no specific text match is found.</returns>
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
        /// Helper method to get the report type name string based on its index.
        /// Used for constructing folder names from configuration.
        /// </summary>
        private string GetReportTypeNameForFolder(int reportTypeIndex)
        {
            return reportTypeIndex switch
            {
                DailyReportIndex => "Daily",
                NewDailyReportOver1kIndex => "Daily5Day1k",
                WeeklyReportIndex => "Weekly",
                MonthlyReportIndex => "Monthly",
                QuarterlyReportIndex => "Quarterly",
                AnnualReportIndex => "Annual",
                CustomReportIndex => "Custom",
                _ => "Other"
            };
        }


        /// <summary>
        /// Updates the UI to reflect the 1-Click processing mode (single button vs. two buttons).
        /// Shows/hides the appropriate action buttons.
        /// </summary>
        private void Update1ClickProcessingModeUI()
        {
            bool oneClickEnabled = enable1ClickProcessingToolStripMenuItem.Checked;
            Logger.LogDebug($"Update1ClickProcessingModeUI called. 1-Click Mode Checked: {oneClickEnabled}");

            if (oneClickProcessButton == null || createReportButton == null || processEmailButton == null)
            {
                Logger.LogError("One or more action buttons are NULL in Update1ClickProcessingModeUI. UI update for 1-Click mode skipped. This may indicate a problem with form initialisation.");
                return;
            }

            UIManager.SafeControlUpdate(oneClickProcessButton, () =>
            {
                oneClickProcessButton.Visible = oneClickEnabled;
                if (oneClickEnabled && oneClickProcessButton.Visible) oneClickProcessButton.BringToFront();
            });
            UIManager.SafeControlUpdate(createReportButton, () => { createReportButton.Visible = !oneClickEnabled; });
            UIManager.SafeControlUpdate(processEmailButton, () => { processEmailButton.Visible = !oneClickEnabled; });

            if (oneClickEnabled) Logger.LogInfo("1-Click Processing Mode UI Enabled (single button visible).");
            else Logger.LogInfo("1-Click Processing Mode UI Disabled (two standard buttons visible).");
        }

        /// <summary>
        /// Populates the `financialYearComboBox` with the current and previous financial years.
        /// The financial year strings (e.g., "2023_24") are obtained using helper methods
        /// from the <see cref="ExcelCopyData"/> class.
        /// </summary>
        private void PopulateFinancialYearDropdown()
        {
            Logger.LogTrace("Entering PopulateFinancialYearDropdown");
            UIManager.SafeControlUpdate(financialYearComboBox, () =>
            {
                string? previouslySelected = financialYearComboBox.SelectedItem?.ToString();
                financialYearComboBox.Items.Clear();

                string currentFY = _excelProcessor.GetCurrentFinancialYear(useUnderscoreFormat: true);
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
                    Logger.LogWarning("Could not determine current financial year for dropdown population.");
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

        /// <summary>
        /// Validates that the selected start date in `startDatePicker` is not after the end date in `endDatePicker`.
        /// If the validation fails, it displays an error message to the user using <see cref="FlexibleMessageBox"/>.
        /// </summary>
        /// <returns>True if the date range is valid (start date is not after end date); otherwise, false.</returns>
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
        /// Validates if the selected date range (from `startDatePicker` and `endDatePicker`)
        /// falls entirely within the financial year selected in `financialYearComboBox`.
        /// This validation is only performed if the `financialYearComboBox` is visible and has an item selected.
        /// If there's a mismatch, it prompts the user with a warning and allows them to continue or cancel.
        /// </summary>
        /// <returns>True if the date range is valid for the selected financial year, or if the user chooses to continue despite a warning,
        /// or if the financial year validation is not applicable (ComboBox hidden/empty). False if the user cancels due to a mismatch.</returns>
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
        /// Determines the base path for the `appsettings.json` configuration file.
        /// For robustness in different deployment scenarios (e.g., running from VS Debug, installed location),
        /// it's best to make this relative to the application's execution directory or use a known location.
        /// Currently, it uses a hardcoded network path.
        /// </summary>
        /// <returns>The base path string for `appsettings.json`.</returns>
        private static string DetermineAppSettingsBasePath() =>
            // IMPORTANT: This is a hardcoded network path.
            // For production, consider a path relative to AppDomain.CurrentDomain.BaseDirectory
            // or a path from an environment variable for better deployment flexibility.
            @"\\harlow.local\DFS\IT Department\Applications\Development 2025\QuoteConversionReportAutomation\conversionTest";

        /// <summary>
        /// Checks the validity of essential configuration settings, specifically the paths
        /// to the Crystal Report file (.rpt) and the Crystal Report Wrapper executable (.exe).
        /// This is used to enable/disable UI elements and provide feedback to the user.
        /// Reads paths from the new "Paths" section in configuration.
        /// </summary>
        /// <returns>True if both the Crystal Report file and the Wrapper EXE exist at their configured paths; otherwise, false.</returns>
        private bool CheckConfigValidity()
        {
            string crPath = CrystalReportLocation; // Uses property that reads "Paths:CrystalReportRptFile"
            string wrapPathCfg = _configuration["Paths:WrapperExecutable"] ?? ""; // Reads "Paths:WrapperExecutable"
            string wrapPathFull = string.IsNullOrEmpty(wrapPathCfg) ? "" : Path.GetFullPath(wrapPathCfg);

            bool crystalReportFileExists = !string.IsNullOrEmpty(crPath) && File.Exists(crPath);
            bool wrapperExeFileExists = !string.IsNullOrEmpty(wrapPathFull) && File.Exists(wrapPathFull);

            if (!crystalReportFileExists) Logger.LogWarning($"ConfigCheck: Crystal Report file not found or path invalid: '{crPath}' (from Paths:CrystalReportRptFile)");
            if (!wrapperExeFileExists) Logger.LogWarning($"ConfigCheck: Wrapper EXE not found or path invalid: '{wrapPathFull}' (from Paths:WrapperExecutable)");

            return crystalReportFileExists && wrapperExeFileExists;
        }

        /// <summary>
        /// Checks if any "Daily" report type (either "Daily" or "Daily (5days >= £1000)")
        /// is currently selected in the `reportTypeComboBox`.
        /// </summary>
        /// <returns>True if a daily-type report is selected; otherwise, false.</returns>
        private bool IsAnyDailySelected()
        {
            string selectedText = "";
            UIManager.SafeControlUpdate(reportTypeComboBox, () => selectedText = reportTypeComboBox.Text);
            return selectedText == "Daily" || selectedText == "Daily (5days >= £1000)";
        }

        /// <summary>
        /// Resets the UI to an appropriate state after an operation error, cancellation, or successful completion.
        /// This method updates the text and enabled states of various buttons and controls
        /// based on the current application context (e.g., 1-Click mode, configuration validity).
        /// </summary>
        /// <param name="mainButtonTextIfNoError">The text to display on the primary action button
        /// if the configuration is valid and no error occurred during the preceding operation.
        /// For example, "Create Report" or "Generate, Process && Email Report".</param>
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
                    (autoRunStatusLabel.Text?.Contains("Completed") ?? false) ||
                        (autoRunStatusLabel.Text?.Contains("Done for") ?? false) ||
                        (autoRunStatusLabel.Text?.Contains("FAILED") ?? false),
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
        /// Retrieves the "To" and "CC" email recipients for the current report context during a manual run.
        /// It delegates the core logic to the <see cref="EmailRecipientManager"/>, passing relevant
        /// context such as the selected report type, whether the "Send to Femi Only" option is checked,
        /// and whether the application is running in Debug mode.
        /// </summary>
        /// <returns>A tuple containing two <see cref="List{T}"/> of strings:
        /// the first for "To" recipients and the second for "CC" recipients.
        /// Lists may be empty if no recipients are configured for the context.</returns>
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
        /// Constructs the email subject line and body content for a manually generated report.
        /// It uses the <see cref="GreetingManager"/> to retrieve a configurable greeting message
        /// and formats the subject and body based on the selected report type, date range,
        /// and whether the "Send to Femi Only" option is active.
        /// </summary>
        /// <param name="reportStartDate">The start date of the report period.</param>
        /// <param name="reportEndDate">The end date of the report period.</param>
        /// <returns>A tuple containing the generated email subject (string) and body (string).</returns>
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
                greetingKeyName = "DebugDefault"; // Key for debug greeting from EmailSettings:DebugRecipients:EmailGreetings
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
                else if (currentReportTypeIndex == CustomReportIndex)
                {
                    greetingKeyName = "ManualCustom";
                }
                else if (currentReportTypeIndex == WeeklyReportIndex ||
                         currentReportTypeIndex == MonthlyReportIndex ||
                         currentReportTypeIndex == QuarterlyReportIndex ||
                         currentReportTypeIndex == AnnualReportIndex)
                {
                    greetingKeyName = femiOnlyChecked ? "ManualFemi" : "ManualTeam";
                }
                else
                {
                    greetingKeyName = "ManualTeam";
                    Logger.LogWarning($"Manual run for unexpected report type '{reportTypeString}' (Index: {currentReportTypeIndex}). Using fallback greeting key '{greetingKeyName}'.");
                }
                greeting = _greetingManager.GetGreeting(greetingKeyName); // Retrieve from EmailSettings:ProductionRecipients:EmailGreetings
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
                case AnnualReportIndex:
                    rangeInfo = $"for Financial Year {reportStartDate.Year}-{reportEndDate.Year}";
                    subjectPrefix = $"Annual {typeName}";
                    break;
                case CustomReportIndex: rangeInfo = $"for period {reportStartDate:dd MMM yy} to {reportEndDate:dd MMM yy}"; break;
                default:
                    rangeInfo = $"for period {reportStartDate:dd MMM yy} to {reportEndDate:dd MMM yy}";
                    subjectPrefix = $"Report {typeName}";
                    break;
            }

            string subjectDateSuffix = (reportStartDate.Date == reportEndDate.Date) ?
                                       $"({reportEndDate:yyyy-MM-dd})" :
                                       $"({reportStartDate:yyyy-MM-dd} to {reportEndDate:yyyy-MM-dd})";
            if (currentReportTypeIndex == AnnualReportIndex) subjectDateSuffix = $"({reportStartDate.Year}-{reportEndDate.Year})";

            string manualPrefix = (currentReportTypeIndex != CustomReportIndex && currentReportTypeIndex != -1) ? "MANUAL: " : "";
            string appNamePrefix = _configuration.GetValue<string>("ApplicationInfo:AppName", "QCRA")!;
            string subject = $"{manualPrefix}{appNamePrefix} - {subjectPrefix} Report {subjectDateSuffix}";
            if (IsDebug) subject = $"DEBUG - {subject}";

            string emailSignature = _configuration.GetValue<string>("EmailSettings:DefaultEmailSignature", "Thank you,\nAutomation Service")!;
            string body = $"{greeting}\n\nPlease find attached the {subjectPrefix.ToLower()} report {rangeInfo}.\n\nThis report includes quotes data for review.\n\n{emailSignature}";

            Logger.LogDebug($"GetEmailSubjectAndBody: GreetingKey='{greetingKeyName}', Subject='{subject}'");
            return (subject, body);
        }
        #endregion
    }
}
