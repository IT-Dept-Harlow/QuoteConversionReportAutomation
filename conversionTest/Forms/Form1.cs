// Form1.cs
// Main application form for Quote Conversion Report Automation.
// Utilises C# 10+ features.

// --- Using Statements ---
using Microsoft.Extensions.Configuration;
using Microsoft.VisualBasic; // For Interaction.InputBox (consider replacing with a custom form if possible for better theming/control)
using Newtonsoft.Json.Linq; // For JObject manipulation (specifically for appsettings.json updates)
using QuoteConversionReportAutomation;
using QuoteConversionReportAutomation.Helpers;
using QuoteConversionReportAutomation.Managers;
using QuoteConversionReportAutomation.Models;
using QuoteConversionReportAutomation.Services.Communication;
using QuoteConversionReportAutomation.Services.Excel;
using QuoteConversionReportAutomation.Services.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq; // Added for Enumerable.Any() in PerformProcessAndEmailAsync
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

// Namespace for the main UI of the application.
namespace conversionTest
{
    /// <summary>
    /// Represents the main form of the Quote Conversion Report Automation application.
    /// This form serves as the primary user interface for generating, processing,
    /// and emailing quote conversion reports. It co-ordinates various manager classes
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
        #region Constants and Fields

        #region Dependencies
        // These fields hold instances of services and managers required by the form.
        // They are typically initialised in the constructor and are marked 'readonly'
        // to indicate they are set once and not changed thereafter.
        private readonly IConfiguration _configuration;             // Provides access to application configuration settings (e.g., from appsettings.json).
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
        /// Current version of the application. Used for display purposes (e.g., title bar, help).
        /// </summary>
        private const string AppVersion = "1.9.0"; // Update this constant as the application version changes.
        #endregion

        #region State Variables
        // These fields store the runtime state of the application.
        private string _generatedReportPath = string.Empty;         // Stores the full path to the last successfully generated raw report file. Used for subsequent processing or viewing.
        private string _generatedAnalysisFilePath = string.Empty;   // Stores the full path to the last successfully processed analysis file. Used for viewing.
        private bool _programmaticallyChangingDates = false;        // A flag to prevent date picker ValueChanged events from re-triggering logic when dates are being set by code (e.g., after report type selection).
        private int _currentAutoRunHour;                            // Stores the hour (0-23) configured for the daily automated report check. Loaded from settings.
        private HelpForm? _helpFormInstance;                        // Holds a reference to the HelpForm instance to ensure only one is open at a time and to manage its lifecycle. Null if no help form is currently open.
        #endregion

        #region Configuration Paths
        // Paths related to application configuration files.
        private static readonly string s_appSettingsBasePath = DetermineAppSettingsBasePath(); // Base path for appsettings.json. Determined once at start-up. Static as it's application-wide.
        private readonly string _appSettingsPath;                                               // Full path to appsettings.json, constructed using the base path.
        #endregion

        #region Report Type Constants
        // These constants define integer indices for different report types selected in the UI ComboBox.
        // Using constants improves readability and maintainability by avoiding "magic numbers" in the code.
        // They must align with the ComboBox item order and any logic that uses these indices (e.g., AutoReportDefinition).
        private const int DailyReportIndex = 0;                     // Index for "Daily" report type.
        private const int NewDailyReportOver1kIndex = 1;            // Index for "Daily (5days >= £1000)" report type.
        private const int WeeklyReportIndex = 2;                    // Index for "Weekly" (15-day rolling) report type.
        private const int MonthlyReportIndex = 3;                   // Index for "Monthly" report type.
        private const int QuarterlyReportIndex = 4;                 // Index for "Quarterly" report type.
        private const int AnnualReportIndex = 5;                    // Index for "Annual" report type.
        private const int CustomReportIndex = 6;                    // Index for "Custom" report type (manual date selection).
        #endregion

        #region Build Configuration
        /// <summary>
        /// Gets a value indicating whether the application is running in DEBUG mode.
        /// This is determined by preprocessor directives set during compilation.
        /// Useful for enabling/disabling debug-specific features or logging.
        /// </summary>
        private static bool IsDebug =>
#if DEBUG   // This block is compiled only if the DEBUG symbol is defined.
            true;
#else       // This block is compiled if the DEBUG symbol is NOT defined (e.g., in Release builds).
            false;
#endif
        #endregion

        #region Configuration-derived Properties
        // These properties provide convenient, strongly-typed access to various paths and settings
        // that are derived from the IConfiguration instance (typically loaded from appsettings.json).
        // They often combine a base path (like UserProfilePath) with a relative path from configuration,
        // and provide default fallback values if the configuration keys are missing.

        /// <summary>Gets the current user's profile directory path (e.g., "C:\Users\YourUser").</summary>
        private string UserProfilePath => Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        /// <summary>
        /// Gets the base directory for exporting raw Crystal Reports.
        /// The path is constructed by combining the UserProfilePath with a relative path from configuration ("settings:RawReportExportBaseDir").
        /// Defaults to a specific path under "Harlow Printing" if the configuration value is missing.
        /// The TrimStart is used to handle potential leading slashes in the configured relative path, ensuring correct Path.Combine behaviour.
        /// </summary>
        private string RawReportExportBaseDir =>
            Path.Combine(UserProfilePath, _configuration["settings:RawReportExportBaseDir"]
                ?.TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) // Remove leading slashes from config value if present.
                ?? @"Harlow Printing\IT Projects - Documents\Dashboard Datasets\Raw_data\Quotes conversion\Estimate Reports Exports"); // Default path.

        /// <summary>
        /// Gets the base directory for saving final processed Excel analysis files.
        /// Constructed similarly to RawReportExportBaseDir, using UserProfilePath and the "settings:ExcelFinalSaveLocation" configuration key.
        /// Defaults to a specific path under "Harlow Printing" if the configuration value is missing.
        /// </summary>
        public string ExcelFinalSaveLocation => // Public as it might be accessed by other components like ReportArchiver.
            Path.Combine(UserProfilePath, _configuration["settings:ExcelFinalSaveLocation"]
                ?.TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                ?? @"Harlow Printing\IT Projects - Documents\Dashboard Datasets\Raw_data\Quotes conversion\Estimates");

        /// <summary>
        /// Gets the full path to the Crystal Report definition file (.rpt) from the "settings:CrystalReportPath" configuration key.
        /// Returns an empty string if the configuration key is missing, allowing for checks like string.IsNullOrEmpty or File.Exists.
        /// </summary>
        private string CrystalReportLocation => _configuration["settings:CrystalReportPath"] ?? string.Empty;

        /// <summary>
        /// Gets the base directory where Excel template files are stored.
        /// Constructed using UserProfilePath and the "settings:ExcelTemplateFolder" configuration key, with a default fallback path.
        /// </summary>
        public string ExcelTemplateBaseDir => // Public as it might be used by Excel processing logic.
            Path.Combine(UserProfilePath, _configuration["settings:ExcelTemplateFolder"]
                ?.TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                ?? @"Harlow Printing\IT Projects - Documents\Dashboard Datasets\Raw_data\Quotes conversion\TEMPLATE");

        /// <summary>
        /// Gets the configured base directory for application logs from the "settings:LogDirectory" configuration key.
        /// Returns an empty string if the key is missing. The Logger class itself handles fallback logic if this path is invalid.
        /// </summary>
        private string ConfiguredLogDirectoryBase => _configuration["settings:LogDirectory"] ?? string.Empty;
        #endregion

        #region Dynamic Path Properties
        // These properties dynamically determine full file or folder paths based on the current UI selections (like report type and dates)
        // and the base paths derived from configuration. They are essential for saving reports to the correct, structured locations.

        /// <summary>
        /// Gets the full output path for the raw Crystal Report export file.
        /// This path is dynamically determined based on the selected report type and dates from the UI.
        /// It uses <see cref="FolderCreation.GetReportSpecificFolderPath"/> to construct a structured subfolder path (e.g., based on year, month, week).
        /// The filename itself is typically based on the report's end date.
        /// </summary>
        public string ReportOutputLocation
        {
            get
            {
                string baseDir = RawReportExportBaseDir; // Base directory for raw reports.
                DateTime dateForFilename = endDatePicker.Value; // Use the selected end date for the filename.
                // Standardised part of the filename for raw reports.
                string fileName = $"{dateForFilename:yyyyMMdd}_EstimateSuccessReport_Raw.xlsx";
                int currentReportTypeIndex = GetSelectedReportTypeIndex(); // Get the currently selected report type.

                // Determine the date to use for folder naming.
                // Custom reports use the current timestamp for uniqueness.
                // Other predefined reports use the selected end date.
                DateTime folderTimestampDate = (currentReportTypeIndex == CustomReportIndex) ? DateTime.Now : endDatePicker.Value;
                // A specific adjustment for the "Daily (5days >= £1k)" report, its folder is based on its end date.
                if (currentReportTypeIndex == NewDailyReportOver1kIndex)
                {
                    folderTimestampDate = endDatePicker.Value;
                }

                // Get the specific subfolder path (e.g., "Daily Reports/2023/May/Week1") using a helper utility.
                string? specificFolder = FolderCreation.GetReportSpecificFolderPath(currentReportTypeIndex, baseDir, folderTimestampDate);

                // Fallback logic if the specific folder path could not be determined (e.g., invalid report type).
                if (string.IsNullOrEmpty(specificFolder))
                {
                    Logger.LogError($"Could not determine specific folder path for ReportOutputLocation. ReportType: {currentReportTypeIndex}, Base: {baseDir}. Using fallback.");
                    // Determine a fallback subfolder name based on the report type.
                    string reportTypeSubFolder = currentReportTypeIndex switch
                    {
                        DailyReportIndex => "Daily Reports",
                        NewDailyReportOver1kIndex => "Daily Reports (5day 1k)",
                        WeeklyReportIndex => "Weekly Reports",
                        MonthlyReportIndex => "Monthly Reports",
                        QuarterlyReportIndex => "Quarterly reports",
                        AnnualReportIndex => "Annual Reports",
                        CustomReportIndex => "Custom Reports",
                        _ => "Other Reports" // Default for any unknown or unhandled types.
                    };
                    specificFolder = Path.Combine(baseDir, reportTypeSubFolder);
                    try { Directory.CreateDirectory(specificFolder); } // Attempt to create the fallback directory.
                    catch (Exception ex) { Logger.LogError($"Failed to create fallback directory '{specificFolder}': {ex.Message}"); } // Log error if creation fails.
                }
                // Combine the specific folder path and the filename to get the full output location.
                return Path.Combine(specificFolder, fileName);
            }
        }

        /// <summary>
        /// Gets the full path to the Excel template file to be used for processing the current report.
        /// The specific template selected depends on the report type. Longer period reports (Monthly, Quarterly, Annual, Custom)
        /// typically use a different template ("_Monthly.xlsx") than shorter period reports.
        /// </summary>
        public string ExcelTemplateLocation
        {
            get
            {
                string baseDir = ExcelTemplateBaseDir; // Base directory for templates.
                int currentReportTypeIndex = GetSelectedReportTypeIndex(); // Get current report type.

                // Select the template filename based on the report type.
                string templateName = currentReportTypeIndex switch
                {
                    // Monthly, Quarterly, Annual, and Custom reports use the "Monthly" template.
                    MonthlyReportIndex or QuarterlyReportIndex or AnnualReportIndex or CustomReportIndex
                        => "TEMPLATE_Estimate Success Rate_Monthly.xlsx",
                    // All other report types (Daily, Weekly, etc.) use the standard template.
                    _ => "TEMPLATE_Estimate Success Rate.xlsx"
                };
                // Combine the base template directory and the selected template filename.
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
        /// </summary>
        /// <param name="configuration">The application's configuration settings, typically loaded from `appsettings.json`.
        /// This provides access to paths, connection strings, and other operational parameters.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="configuration"/> is null, as it's essential for operation.</exception>
        /// <exception cref="InvalidOperationException">Thrown if critical configuration settings required by manager classes are missing or invalid,
        /// potentially leaving the application in an unusable state.</exception>
        public Form1(IConfiguration configuration)
        {
            // Ensure configuration is provided.
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            // Construct the full path to appsettings.json using the statically determined base path.
            _appSettingsPath = Path.Combine(s_appSettingsBasePath, "appsettings.json");

            Logger.LogTrace("Entering Form1 Constructor");
            try
            {
                InitializeComponent(); // Standard WinForms method to initialise all controls placed on the form in the designer.
                Logger.LogDebug("InitializeComponent completed.");

                // Instantiate core services and manager classes.
                // These objects encapsulate specific functionalities of the application.
                _emailUtility = new EmailUtility(_configuration);                           // Handles email sending logic.
                _excelProcessor = new ExcelCopyData();                                      // Manages Excel file creation and data manipulation.
                _emailRecipientManager = new EmailRecipientManager(_configuration);         // Manages lists of email recipients for various reports.
                _greetingManager = new GreetingManager(_configuration);                     // Manages email greeting messages.

                // Instantiate UIManager, passing references to all relevant UI controls it will manage.
                // This centralises UI update logic and theming.
                _uiManager = new UIManager(
                    this, menuStrip1, mainStatusStrip, statusLabel, autoRunStatusLabel,
                    darkModeToolStripMenuItem, createReportButton, processEmailButton,
                    oneClickProcessButton,
                    toggleAutoRunButton, viewReportButton, viewAnalysisButton,
                    reportTypeComboBox, startDatePicker, endDatePicker,
                    financialYearComboBox, financialYearLabel, sendToFemiOnlyCheckBox,
                    skipEmailCheckBox, emailRecipientLabel, toolTip1
                );

                // Instantiate ReportProcessManager, which handles the lifecycle of the external Crystal Report Wrapper executable.
                string wrapperExePathConfig = _configuration["settings:WrapperExePath"] ?? "CrystalReportWrapper.exe"; // Get wrapper path from config, with a default.
                string wrapperExeFullPath = Path.GetFullPath(wrapperExePathConfig); // Resolve to an absolute path.
                _processManager = new ReportProcessManager(wrapperExeFullPath);

                // Instantiate NamedPipeCommunicator for Inter-Process Communication (IPC) with the Crystal Report Wrapper.
                _pipeCommunicator = new NamedPipeCommunicator();

                // Instantiate AutoRunManager, responsible for scheduled, automated report generation.
                _currentAutoRunHour = _configuration.GetValue<int>("settings:AutoRunCheckHour", 8); // Get configured auto-run hour, default to 8 AM.
                _uiManager.SetAutoRunHour(_currentAutoRunHour); // Inform UIManager for display purposes.
                _autoRunManager = new AutoRunManager(
                    _configuration, _emailUtility, _processManager, _pipeCommunicator,
                    _uiManager, _excelProcessor, _appSettingsPath, _emailRecipientManager, _greetingManager,
                     _currentAutoRunHour
                );

                Logger.LogDebug("Service and Manager classes instantiated successfully.");
            }
            catch (Exception ex)
            {
                // Log any critical error that occurs during the constructor.
                Logger.LogCritical($"CRITICAL ERROR during Form Initialisation: {ex.Message}", ex);
                // Display a message to the user indicating a fatal initialisation error.
                // Using System.Windows.Forms.MessageBox directly as FlexibleMessageBox or UIManager might not be initialised yet.
                System.Windows.Forms.MessageBox.Show(
                    $"A critical error occurred initialising the application:\n\n{ex.Message}\n\nThe application cannot continue and will now exit.",
                    "Initialisation Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                // Re-throw the exception to allow higher-level error handling or to crash the application,
                // as it might be in an unrecoverable state.
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
            _uiManager.UpdateStatusMain("Loading application..."); // Provide initial feedback to the user.

            try
            {
                // Initialise the BankHolidayHelper to load any custom bank holidays from storage.
                // This is crucial for correct "previous working day" calculations.
                BankHolidayHelper.Initialize();
                Logger.LogInfo("BankHolidayHelper initialised successfully.");

                // Validate critical configuration paths required for report generation.
                string crystalReportPath = CrystalReportLocation; // Path to the .rpt file.
                string wrapperExePathConfig = _configuration["settings:WrapperExePath"] ?? string.Empty; // Path to the wrapper .exe from config.
                string wrapperExeFullPath = string.IsNullOrEmpty(wrapperExePathConfig) ? string.Empty : Path.GetFullPath(wrapperExePathConfig); // Absolute path.
                bool configValid = true; // Assume config is valid initially.

                // Check if the Crystal Report file exists.
                if (string.IsNullOrEmpty(crystalReportPath) || !File.Exists(crystalReportPath))
                {
                    Logger.LogError($"Configuration Error: 'settings:CrystalReportPath' is missing or the file was not found at '{crystalReportPath}'. Report generation will be affected.");
                    configValid = false;
                }
                // Check if the Wrapper Executable file exists.
                if (string.IsNullOrEmpty(wrapperExeFullPath) || !File.Exists(wrapperExeFullPath))
                {
                    Logger.LogError($"Configuration Error: 'settings:WrapperExePath' is missing or the file was not found at '{wrapperExeFullPath}'. Report generation will be affected.");
                    configValid = false;
                }

                // Set the form's title bar text to include application version and build mode (Debug/Release).
                Text = $"Quote Conversion Automation - {(IsDebug ? "DEBUG" : "RELEASE")} - v{AppVersion}";
                StartPosition = FormStartPosition.CenterScreen; // Ensure the form appears centred on the screen.

                // Configure ComboBoxes to be non-editable dropdown lists.
                financialYearComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
                reportTypeComboBox.DropDownStyle = ComboBoxStyle.DropDownList;

                // Ensure the "Custom" report type is available in the ComboBox.
                if (!reportTypeComboBox.Items.Contains("Custom"))
                {
                    reportTypeComboBox.Items.Add("Custom");
                }
                // Attempt to set "Daily" as the default selected report type.
                if (reportTypeComboBox.Items.Count > DailyReportIndex && reportTypeComboBox.Items.Contains("Daily"))
                {
                    reportTypeComboBox.SelectedIndex = reportTypeComboBox.Items.IndexOf("Daily");
                }
                else if (reportTypeComboBox.Items.Count > 0) // If "Daily" isn't found, select the first available item.
                {
                    reportTypeComboBox.SelectedIndex = 0;
                }

                // Apply the initial visual theme (Dark/Light Mode) based on Windows system settings.
                bool useDarkMode = UIManager.IsWindowsDarkModeEnabled();
                darkModeToolStripMenuItem.Checked = useDarkMode; // Synchronise the "Dark Mode" menu item's checked state.
                _uiManager.ApplyTheme(useDarkMode); // Apply the theme to the form and its controls.

                // Update the Auto-Run UI elements (button text, status label) based on the initial timer state and theme.
                _uiManager.UpdateAutoRunUI(dailyCheckTimer.Enabled, false, useDarkMode, $"Auto Run: {(dailyCheckTimer.Enabled ? $"Enabled (Next check ~{_currentAutoRunHour}:00)" : "Disabled")}");

                // Load the persisted states for the auto-run report toggle menu items (e.g., which auto-reports are enabled).
                LoadAutoReportToggleStates();

                // Trigger the SelectedIndexChanged event handler for the reportTypeComboBox to set initial dates and UI visibility.
                reportTypeComboBox_SelectedIndexChanged(reportTypeComboBox, EventArgs.Empty);
                _uiManager.ResetButtonStatesAfterTypeChange(configValid); // Reset action button states based on the initial report type and config validity.

                // Initialise the "1-Click Processing" mode (default to disabled).
                enable1ClickProcessingToolStripMenuItem.Checked = false;
                Update1ClickProcessingModeUI(); // Update the UI to reflect this mode.

                // If configuration is found to be invalid, update the main status label to inform the user.
                if (!configValid)
                {
                    _uiManager.UpdateStatusMain("Config Error: Check Options menu.");
                }

                // Attempt to ensure the Crystal Report Wrapper service is running.
                _uiManager.UpdateStatusMain("Checking report service...");
                IProgress<string> loadProgress = new Progress<string>(status => _uiManager.UpdateProgress(status)); // For progress updates during service check.
                bool wrapperOk = await _processManager.EnsureWrapperIsRunningAsync(loadProgress);

                // If the wrapper service failed to start but the configuration is otherwise valid, update the status.
                if (!wrapperOk && configValid)
                {
                    _uiManager.UpdateStatusMain("Report service failed to start. Report generation may fail.");
                }

                // Initiate background archiving of old report files. This runs on a separate thread.
                string? finalDir = ExcelFinalSaveLocation; // Path to final reports.
                string? rawDir = RawReportExportBaseDir;   // Path to raw reports.
                int? archiveDays = _configuration.GetValue<int?>("settings:ArchiveRawOlderThanDays"); // Threshold for archiving raw files.

                // Use Task.Run to execute archiving asynchronously.
                _ = Task.Run(async () => await ReportArchiver.ArchiveOldReportsAsync(finalDir, rawDir, archiveDays))
                        .ContinueWith(t => // Handle completion or failure of the archiving task.
                        {
                            if (t.IsFaulted) Logger.LogError($"Background report archiving task failed: {t.Exception?.GetBaseException().Message}");
                            else Logger.LogInfo("Background report archiving task completed.");
                        }, TaskScheduler.Default); // Use the default task scheduler for the continuation.

                Logger.LogInfo("Form Load Initialisation Complete.");
                // Set the final status message based on the overall success of configuration and service checks.
                if (configValid && wrapperOk) _uiManager.UpdateStatusMain("Ready");
                else if (configValid && !wrapperOk) _uiManager.UpdateStatusMain("Ready (Report Service Issue)");
                else _uiManager.UpdateStatusMain("Config Error (Service Check Skipped)"); // If config was invalid, service check might be skipped.
            }
            catch (Exception ex)
            {
                // Log any critical error that occurs during the Form_Load process.
                Logger.LogCritical($"CRITICAL ERROR during Form_Load: {ex.Message}", ex);
                // Display an error message to the user, as the application might be in an unstable state.
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
            dailyCheckTimer.Stop(); // Stop the daily auto-run check timer to prevent it from firing after the form is closed.
            _processManager.TerminateWrapperProcess(); // Attempt to terminate the external Crystal Report Wrapper process to ensure clean shut-down.
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
            // Delegate the core logic to the PerformCreateReportAsync method.
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
            // Delegate the core logic, passing the current state of the skipEmailCheckBox.
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
            _uiManager.UpdateStatusMain("1-Click Process: Starting..."); // Update UI status.

            // Disable relevant UI elements during the 1-Click operation to prevent concurrent actions.
            UIManager.SafeControlUpdate(oneClickProcessButton, () => oneClickProcessButton.Enabled = false);
            UIManager.SafeControlUpdate(createReportButton, () => createReportButton.Enabled = false); // Also disable the standard create button.
            UIManager.SafeControlUpdate(processEmailButton, () => processEmailButton.Enabled = false); // Also disable the standard process button.
            _uiManager.SetOtherControlsEnabled(false, financialYearComboBox.Visible); // Disable date pickers, report type selector, etc.

            // Step 1: Create the raw report.
            await PerformCreateReportAsync();

            // After PerformCreateReportAsync, check if the raw report was successfully generated.
            // _generatedReportPath will be set by PerformCreateReportAsync on success.
            if (string.IsNullOrEmpty(_generatedReportPath) || !File.Exists(_generatedReportPath))
            {
                Logger.LogWarning("1-Click Process: Raw report generation failed or was cancelled. Aborting further steps.");
                // Determine the appropriate text for the button to reset to, based on configuration validity.
                string buttonText = CheckConfigValidity() ? "Generate, Process & Email Report" : "Config Error";
                ResetUIStateOnError(buttonText); // Reset the UI to an appropriate state.
                return; // Abort the 1-Click process if raw report generation failed.
            }

            // Step 2: Process the generated raw report and email it (if not skipped).
            await PerformProcessAndEmailAsync(skipEmail: skipEmailCheckBox.Checked);
            Logger.LogInfo("1-Click Process sequence completed (or aborted if errors occurred).");
            // The UI state (buttons enabled/disabled, status messages) is typically reset within
            // PerformProcessAndEmailAsync or its error handling routines.
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
            // Use a helper method to encapsulate the logic for opening a file,
            // including error handling and logging.
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
            // Use the same helper method as viewReportButton_Click.
            ReportHelper.OpenFileWithDefaultApp(_generatedAnalysisFilePath, "processed analysis file");
        }
        #endregion

        #region Core Report Logic Methods
        // This region contains the core asynchronous methods responsible for the main
        // report generation and processing workflows.

        /// <summary>
        /// Asynchronously performs the steps to create the raw report data.
        /// This involves:
        /// 1. Validating user inputs (dates, financial year).
        /// 2. Ensuring the Crystal Report Wrapper service is running.
        /// 3. Constructing a <see cref="ReportRequest"/> object.
        /// 4. Sending the request to the wrapper service via named pipes.
        /// 5. Handling the <see cref="ReportResponse"/> from the service.
        /// 6. Updating UI elements to reflect the outcome (success, failure, progress).
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        private async Task PerformCreateReportAsync()
        {
            // Determine which action button is currently active (standard "Create Report" or "1-Click Process").
            Button currentActionButton = oneClickProcessButton.Visible ? oneClickProcessButton : createReportButton;
            string originalButtonText = string.Empty;
            UIManager.SafeControlUpdate(currentActionButton, () => originalButtonText = currentActionButton.Text); // Store original text to restore on error/completion.

            // Disable UI elements to prevent concurrent operations.
            UIManager.SafeControlUpdate(currentActionButton, () => currentActionButton.Enabled = false);
            if (currentActionButton == createReportButton) // If not in 1-Click mode, also disable the "Process & Email" button.
            {
                UIManager.SafeControlUpdate(processEmailButton, () => processEmailButton.Enabled = false);
            }
            _uiManager.SetOtherControlsEnabled(false, financialYearComboBox.Visible); // Disable input controls like date pickers.
            _uiManager.UpdateProgress("Validating request..."); // Update status label.
            UIManager.SafeControlUpdate(currentActionButton, () => currentActionButton.Text = "Requesting..."); // Change button text to indicate action.
            Logger.LogDebug("Create Report Logic: Requesting Crystal Report generation.");

            // Create a CancellationTokenSource with a timeout (e.g., 6 minutes) for the entire operation.
            using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(6));
            // Create a progress reporter to update the UI status label.
            IProgress<string> progress = new Progress<string>(status => _uiManager.UpdateProgress(status));

            try
            {
                // Perform input validations. If any fail, reset UI and return.
                if (!ValidateInputDates()) { ResetUIStateOnError(originalButtonText); return; }
                if (!ValidateFinancialYearSelection()) { ResetUIStateOnError(originalButtonText); return; }

                // Validate the Crystal Report file path from configuration.
                string crystalReportPath = CrystalReportLocation;
                if (string.IsNullOrEmpty(crystalReportPath) || !File.Exists(crystalReportPath))
                {
                    // This is a critical configuration error.
                    throw new InvalidOperationException("Crystal Report location is invalid or file not found. Check configuration.");
                }

                // Ensure the external Crystal Report Wrapper service is running.
                if (!await _processManager.EnsureWrapperIsRunningAsync(progress, cts.Token))
                {
                    // If the wrapper service cannot be started or connected to.
                    throw new InvalidOperationException($"Failed to start or connect to the report service (CrystalReportWrapper).");
                }

                // Determine the output path for the raw report (this is a dynamic property).
                string reportOutputPath = ReportOutputLocation;
                // Create the request object to send to the wrapper service.
                var request = new ReportRequest
                {
                    CrystalReportLocation = crystalReportPath,
                    ReportOutputLocation = reportOutputPath,
                    ReportDateFrom = startDatePicker.Value,
                    ReportDateTo = endDatePicker.Value
                };

                Logger.LogInfo($"Attempting Named Pipe communication with CrystalReportWrapper. Requesting report for: {request.ReportDateFrom:d} to {request.ReportDateTo:d}, Output: {request.ReportOutputLocation}");
                // Send the request and receive the response asynchronously via named pipes.
                ReportResponse? response = await _pipeCommunicator.SendRequestReceiveResponseAsync(request, progress, cts.Token);

                // Process the response from the wrapper.
                if (response?.Success == true && !string.IsNullOrEmpty(response.OutputPath) && File.Exists(response.OutputPath))
                {
                    // Raw report generation was successful.
                    _generatedReportPath = response.OutputPath; // Store the path to the generated file.
                    Logger.LogInfo($"Raw report generated successfully by wrapper: {_generatedReportPath}");

                    if (oneClickProcessButton.Visible)
                    {
                        // In 1-Click mode, no immediate UI changes here, as PerformProcessAndEmailAsync will follow.
                        // The status will be updated by that subsequent step or its error handling.
                    }
                    else // Standard "Create Report" button was clicked.
                    {
                        // Update UI to reflect successful raw report creation.
                        UIManager.SafeControlUpdate(createReportButton, () => createReportButton.Text = "Report Created");
                        // Enable the "Process & Email" button if configuration is valid.
                        UIManager.SafeControlUpdate(processEmailButton, () => processEmailButton.Enabled = CheckConfigValidity());
                        _uiManager.SetOtherControlsEnabled(true, financialYearComboBox.Visible); // Re-enable input controls.
                    }
                    // Show the "View Raw Report" button and store the file path in its Tag.
                    _uiManager.ShowViewReportButton(true, _generatedReportPath);
                    _uiManager.ShowViewAnalysisButton(false); // Hide "View Analysis" button as it's not generated yet.
                    _generatedAnalysisFilePath = string.Empty; // Clear any previous analysis file path.
                    _uiManager.UpdateStatusMain("Raw report created successfully.");
                }
                else // Raw report generation failed or the response was invalid.
                {
                    string errorMessage = response?.ErrorMessage ?? "Unknown error from report service.";
                    // If the service reported success but the output path is invalid/missing, refine the error message.
                    if (response?.Success == true && (string.IsNullOrEmpty(response.OutputPath) || !File.Exists(response.OutputPath)))
                    {
                        errorMessage = $"Report service indicated success, but the output file ('{response?.OutputPath ?? "NULL"}') is invalid or missing.";
                        Logger.LogError(errorMessage); // Log this specific inconsistency.
                    }
                    throw new Exception($"Raw report generation failed: {errorMessage}");
                }
            }
            catch (OperationCanceledException) // Handle cancellation (e.g., timeout).
            {
                Logger.LogWarning("Report generation request cancelled or timed out.");
                FlexibleMessageBox.Show(this, "The report generation request timed out or was cancelled.", "Timeout / Cancelled", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                ResetUIStateOnError(originalButtonText); // Reset UI to a consistent state.
            }
            catch (Exception ex) // Handle any other exceptions during the process.
            {
                Logger.LogError($"Error during Create Report operation: {ex.Message}", ex);
                FlexibleMessageBox.Show(this, $"An error occurred while requesting the report:\n\n{ex.Message}", "Report Request Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                ResetUIStateOnError(originalButtonText); // Reset UI.
            }
            // Note: If this method is part of a 1-Click process and succeeds, the UI reset
            // is effectively deferred to the end of PerformProcessAndEmailAsync or its error handling.
            // If it's a standalone "Create Report" and succeeds, the UI is updated within the success block above.
        }

        /// <summary>
        /// Asynchronously processes a previously generated raw report into a final analysis Excel file,
        /// and then (optionally) emails it. This method handles:
        /// 1. Validating inputs and the existence of the raw report.
        /// 2. Checking for and handling existing final files (prompting user to overwrite or use existing).
        /// 3. Calling <see cref="ExcelCopyData.ProcessExcelReportAsync"/> to perform the Excel processing.
        /// 4. Handling manual Excel refresh steps if required by the report type.
        /// 5. Calling <see cref="SendCompletionEmailAsync"/> if emailing is not skipped.
        /// 6. Updating UI elements throughout the process and on completion/error.
        /// </summary>
        /// <param name="skipEmail">If true, the email sending step will be bypassed after processing.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        private async Task PerformProcessAndEmailAsync(bool skipEmail = false)
        {
            Logger.LogTrace($"Entering PerformProcessAndEmailAsync (skipEmail: {skipEmail})");
            // Determine the primary action button (standard "Process & Email" or "1-Click Process").
            Button currentActionButton = oneClickProcessButton.Visible ? oneClickProcessButton : processEmailButton;
            string originalButtonText = string.Empty;
            UIManager.SafeControlUpdate(currentActionButton, () => originalButtonText = currentActionButton.Text); // Store original text.

            // Disable UI elements during processing.
            UIManager.SafeControlUpdate(currentActionButton, () => currentActionButton.Enabled = false);
            if (currentActionButton == processEmailButton) // If not in 1-Click mode, also disable the "Create Report" button.
            {
                UIManager.SafeControlUpdate(createReportButton, () => createReportButton.Enabled = false);
            }
            _uiManager.SetOtherControlsEnabled(false, financialYearComboBox.Visible); // Disable input controls.
            UIManager.SafeControlUpdate(currentActionButton, () => currentActionButton.Text = "Processing..."); // Update button text.

            // Setup progress reporters for UI updates.
            IProgress<ProgressReport> excelProgress = new Progress<ProgressReport>(report => _uiManager.UpdateProgress(report)); // For detailed Excel progress.
            IProgress<string> generalProgress = new Progress<string>(message => _uiManager.UpdateProgress(message)); // For general status messages.
            _uiManager.UpdateProgress("Starting Excel processing...");

            // CancellationTokenSource for timeout (e.g., 15 minutes for processing and emailing).
            using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(15));
            var token = cts.Token;

            string? finalFilePath = null; // Will store the path to the successfully processed Excel file.
            int reportType = GetSelectedReportTypeIndex(); // Get the currently selected report type.
            // Determine if the report type requires manual refresh of Excel (e.g., for PivotTables).
            bool requiresManualRefresh = reportType is MonthlyReportIndex or QuarterlyReportIndex or AnnualReportIndex or CustomReportIndex;
            string baseSaveLocation = ExcelFinalSaveLocation; // Base directory for saving final reports.
            // Determine the date to use for filename generation and internal Excel processing logic.
            // Annual reports often use the start date of the financial year for naming.
            DateTime dateForFilenameAndExcelProcessing = (reportType == AnnualReportIndex) ? startDatePicker.Value : endDatePicker.Value;

            try
            {
                // Validate input dates.
                if (!ValidateInputDates()) { ResetUIStateOnError(originalButtonText); return; }
                // Ensure the raw report file (_generatedReportPath) exists before attempting to process it.
                if (string.IsNullOrEmpty(_generatedReportPath) || !File.Exists(_generatedReportPath))
                {
                    FlexibleMessageBox.Show(this, "The raw report file has not been generated or cannot be found. Please create the report first.", "Raw Report Missing", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    // Determine the appropriate text to reset the main button to.
                    string resetText = oneClickProcessButton.Visible ? (CheckConfigValidity() ? "Generate, Process & Email Report" : "Config Error")
                                                                  : (CheckConfigValidity() ? "Create Report" : "Config Error");
                    ResetUIStateOnError(resetText); // Reset UI.
                    return; // Abort if raw report is missing.
                }

                // Check if a final processed file for this report period already exists.
                string? expectedFinalPath = _excelProcessor.GetExpectedFinalFilePath(reportType, baseSaveLocation, dateForFilenameAndExcelProcessing);
                if (expectedFinalPath != null && File.Exists(expectedFinalPath))
                {
                    generalProgress.Report("Found existing file. Prompting user...");
                    // Ask the user if they want to use the existing file or overwrite/regenerate it.
                    DialogResult fdr = FlexibleMessageBox.Show(this,
                        $"The report file '{Path.GetFileName(expectedFinalPath)}' already exists for this period.\n\nDo you want to skip processing and use this existing file?",
                        "File Already Exists", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                    if (fdr == DialogResult.Yes) // User chose to use the existing file.
                    {
                        Logger.LogInfo($"User chose to use existing file: {expectedFinalPath}");
                        finalFilePath = expectedFinalPath;
                        _generatedAnalysisFilePath = finalFilePath; // Update the state variable.
                        _uiManager.ShowViewAnalysisButton(true, finalFilePath); // Make the "View Analysis" button visible.
                        bool proceedToEmail = true; // Assume we will proceed to email unless manual refresh is cancelled.

                        // If the report type (and thus the existing file) requires manual refresh.
                        if (requiresManualRefresh)
                        {
                            generalProgress.Report("Waiting for manual Excel refresh of existing file...");
                            proceedToEmail = await HandleManualExcelRefreshAsync(finalFilePath, token); // Prompt user for manual refresh.
                            if (!proceedToEmail && !token.IsCancellationRequested) // If refresh was cancelled by user.
                            { _uiManager.UpdateStatusMain("Manual refresh/confirmation cancelled."); ResetUIStateOnError(originalButtonText); return; }
                            if (token.IsCancellationRequested) throw new OperationCanceledException("Operation cancelled during manual refresh prompt."); // If cancelled by timeout/token.
                            generalProgress.Report("Manual refresh confirmed.");
                        }

                        // Send email if not skipped by user and if manual refresh was successful (or not required).
                        if (!skipEmail && proceedToEmail)
                        {
                            await SendCompletionEmailAsync(finalFilePath, generalProgress, token);
                        }
                        else if (skipEmail)
                        {
                            _uiManager.UpdateStatusMain("Process completed. Email skipped by user.");
                            Logger.LogInfo("Email sending skipped by user checkbox.");
                        }

                        // Update UI to reflect completion.
                        if (proceedToEmail || skipEmail)
                        {
                            _uiManager.SetUICompleted(CheckConfigValidity(), IsAnyDailySelected(), dailyCheckTimer.Enabled, darkModeToolStripMenuItem.Checked, false, autoRunStatusLabel.Text ?? "");
                        }
                        ResetUIStateOnError(originalButtonText); // Reset UI elements.
                        return; // Operation complete using existing file.
                    }
                    else // User chose to overwrite/regenerate the existing file.
                    {
                        generalProgress.Report("Deleting existing file to regenerate...");
                        Logger.LogInfo($"User chose to overwrite/regenerate the existing file: {expectedFinalPath}");
                        try
                        {
                            File.Delete(expectedFinalPath); // Attempt to delete the old file.
                            Logger.LogInfo($"Successfully deleted existing file: {expectedFinalPath}");
                        }
                        catch (Exception delEx) // Handle failure to delete (e.g., file locked).
                        {
                            Logger.LogError($"Failed to delete existing file '{expectedFinalPath}': {delEx.Message}");
                            FlexibleMessageBox.Show(this, $"Could not delete the existing report file:\n{expectedFinalPath}\n\nPlease ensure the file is not open and try again.\n\nError: {delEx.Message}", "File Deletion Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            ResetUIStateOnError(originalButtonText); return; // Abort if deletion fails.
                        }
                    }
                }

                // Process the new report (either because no existing file was found, or user chose to overwrite).
                generalProgress.Report("Processing new report...");
                finalFilePath = await _excelProcessor.ProcessExcelReportAsync(
                    financialYearComboBox.SelectedItem?.ToString() ?? _excelProcessor.GetCurrentFinancialYear(true), // Financial year.
                    reportType,                 // Report type index.
                    _generatedReportPath,       // Path to the raw data file.
                    "Sheet1",                   // Name of the sheet in the raw data file.
                    baseSaveLocation,           // Base directory to save the processed file.
                    ExcelTemplateLocation,      // Path to the Excel template file.
                    "DATA",                     // Name of the sheet in the template where raw data is copied.
                    1, 1,                       // Start row and column for copying data.
                    excelProgress,              // Progress reporter for Excel operations.
                    dateForFilenameAndExcelProcessing, // Date used for filename and internal processing.
                    token);                     // Cancellation token.

                // Check if Excel processing was successful and produced a file.
                if (string.IsNullOrEmpty(finalFilePath) || !File.Exists(finalFilePath))
                {
                    if (token.IsCancellationRequested) throw new OperationCanceledException("Excel processing was cancelled.");
                    throw new Exception("Excel processing failed to produce a final file. Check logs for details.");
                }
                _generatedAnalysisFilePath = finalFilePath; // Store path to the newly processed file.
                _uiManager.ShowViewAnalysisButton(true, finalFilePath); // Make "View Analysis" button visible.

                bool proceedToEmailAfterGenerate = true; // Assume email will be sent.
                // If the newly generated report requires manual refresh.
                if (requiresManualRefresh)
                {
                    generalProgress.Report("Waiting for manual Excel refresh...");
                    proceedToEmailAfterGenerate = await HandleManualExcelRefreshAsync(finalFilePath, token); // Prompt for manual refresh.
                    if (!proceedToEmailAfterGenerate && !token.IsCancellationRequested) // If refresh cancelled by user.
                    { _uiManager.UpdateStatusMain("Manual refresh/confirmation cancelled."); ResetUIStateOnError(originalButtonText); return; }
                    if (token.IsCancellationRequested) throw new OperationCanceledException("Operation cancelled during manual refresh prompt."); // If cancelled by timeout/token.
                    generalProgress.Report("Manual refresh confirmed.");
                }

                // Send email if not skipped and processing/refresh was successful.
                if (!skipEmail && proceedToEmailAfterGenerate)
                {
                    await SendCompletionEmailAsync(finalFilePath, generalProgress, token);
                }
                else if (skipEmail)
                {
                    _uiManager.UpdateStatusMain("Process completed. Email skipped by user.");
                    Logger.LogInfo("Email sending skipped by user checkbox.");
                }

                // Update UI to reflect completion.
                if (proceedToEmailAfterGenerate || skipEmail)
                {
                    _uiManager.SetUICompleted(CheckConfigValidity(), IsAnyDailySelected(), dailyCheckTimer.Enabled, darkModeToolStripMenuItem.Checked, false, autoRunStatusLabel.Text ?? "");
                }
                ResetUIStateOnError(originalButtonText); // Reset UI elements.
            }
            catch (OperationCanceledException) // Handle cancellation of the overall process.
            {
                Logger.LogWarning("Excel processing or subsequent step cancelled.");
                ResetUIStateOnError(originalButtonText); // Reset UI.
            }
            catch (FileNotFoundException fnfEx) // Handle specific file not found errors.
            {
                Logger.LogError($"File not found during Process & Email operation: {fnfEx.Message}", fnfEx);
                FlexibleMessageBox.Show(this, fnfEx.Message, "File Not Found Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                ResetUIStateOnError(originalButtonText);
            }
            catch (Exception ex) // Handle any other exceptions.
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
        /// </summary>
        /// <param name="attachmentPath">The full path to the file to be attached.</param>
        /// <param name="progress">An <see cref="IProgress{T}"/> interface to report string-based progress updates.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe for cancellation requests.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        /// <exception cref="FileNotFoundException">Thrown if the <paramref name="attachmentPath"/> is invalid or the file does not exist.</exception>
        /// <exception cref="FormatException">Thrown if email addresses are invalid.</exception>
        /// <exception cref="System.Net.Mail.SmtpException">Thrown if the SMTP client encounters an error.</exception>
        /// <exception cref="OperationCanceledException">Thrown if the operation is cancelled via the <paramref name="cancellationToken"/>.</exception>
        private async Task SendCompletionEmailAsync(string attachmentPath, IProgress<string> progress, CancellationToken cancellationToken)
        {
            Logger.LogTrace("Entering SendCompletionEmailAsync");
            _uiManager.UpdateProgress("Preparing email..."); // Update UI status.

            // Validate attachment path.
            if (!File.Exists(attachmentPath))
            {
                Logger.LogError($"Attachment file not found for email: {attachmentPath}");
                throw new FileNotFoundException("Attachment file for email not found.", attachmentPath);
            }

            try
            {
                // Get email recipients (To and CC) based on current UI selections and configuration.
                var (to, cc) = GetEmailRecipients();

                // Check if there are any recipients, especially in Release mode.
                if (!to.Any() && !cc.Any() && !IsDebug) // Using Enumerable.Any() for clarity
                {
                    Logger.LogWarning("No email recipients determined for Release mode. Skipping email send.");
                    progress.Report("No recipients configured. Email not sent.");
                    return;
                }
                if (!to.Any() && !cc.Any() && IsDebug) // Using Enumerable.Any()
                {
                    Logger.LogInfo("DEBUG MODE: No explicit recipients, but will proceed using debug list from EmailRecipientManager if configured there.");
                }

                // Get email subject and body.
                var (subject, body) = GetEmailSubjectAndBody(startDatePicker.Value, endDatePicker.Value);
                progress.Report("Sending email..."); // Update UI status.

                // Send the email using EmailUtility.
                bool emailSent = await _emailUtility.SendEmailAsync(to, cc, subject, body, attachmentPath, progress, cancellationToken);

                // Log and report outcome.
                if (!emailSent && !cancellationToken.IsCancellationRequested)
                {
                    Logger.LogError("Email sending failed. Check EmailUtility logs for details.");
                    progress.Report("Email sending failed. Check logs."); // User-facing status.
                }
                else if (emailSent)
                {
                    Logger.LogInfo("Email sent successfully.");
                    progress.Report("Email sent successfully!");
                }
                else if (cancellationToken.IsCancellationRequested)
                {
                    Logger.LogWarning("Email sending cancelled.");
                    progress.Report("Email sending cancelled.");
                }
            }
            catch (OperationCanceledException) // Catch cancellation specifically.
            {
                Logger.LogWarning("Email sending operation was cancelled (caught in SendCompletionEmailAsync).");
                progress.Report("Email sending cancelled.");
                throw; // Re-throw to be handled by the caller.
            }
            catch (Exception ex) // Catch other exceptions during email preparation or sending.
            {
                Logger.LogError($"Error sending email: {ex.Message}", ex);
                FlexibleMessageBox.Show(this, $"Failed to send email: {ex.Message}", "Email Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                throw; // Re-throw to be handled by the caller.
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
            // Check if other Excel instances are running, as they might interfere.
            if (await Task.Run(() => Process.GetProcessesByName("EXCEL").Length > 0, token))
            {
                DialogResult fdr = FlexibleMessageBox.Show(this,
                    "Other Excel instances are running. It's recommended to close them before proceeding with the manual refresh to avoid conflicts.\n\nAttempt to close other Excel instances automatically?",
                    "Close Other Excel Instances?", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning);

                if (fdr == DialogResult.Cancel) { Logger.LogInfo("User cancelled manual refresh due to other Excel instances."); return false; }
                if (fdr == DialogResult.Yes) // Attempt to close other Excel instances if user agrees.
                {
                    _uiManager.UpdateProgress("Attempting to close other Excel instances...");
                    await Task.Run(() => ReportHelper.CloseProcessesByName("EXCEL"), token); // Helper method to close processes.
                    await Task.Delay(1500, token); // Brief delay to allow processes to close.
                }
            }

            // Inform the user about the manual refresh steps.
            FlexibleMessageBox.Show(this,
                "The report will now open in Excel.\n\n" +
                "*** IMPORTANT ***\n" +
                "1. Enable Editing if prompted by Excel.\n" +
                "2. Go to the 2 Pivot sheets and right click each Table and Slicer > 'Refresh'.\n" + // Specific instructions for user.
                "3. Ensure all PivotTables and data connections are updated.\n" +
                "4. SAVE the file.\n" +
                "5. CLOSE Excel.\n\n" +
                "The application will wait for you to close Excel before continuing.",
                "Manual Refresh Required", MessageBoxButtons.OK, MessageBoxIcon.Information);

            token.ThrowIfCancellationRequested(); // Check for cancellation before opening Excel.
            _uiManager.UpdateProgress("Opening Excel for manual refresh...");
            Process? excelProc = null; // To hold the Excel process instance.
            try
            {
                // Start Excel with the specified file.
                excelProc = await Task.Run(() => Process.Start(new ProcessStartInfo(filePath) { UseShellExecute = true }), token);
                if (excelProc == null) throw new InvalidOperationException("Failed to start Excel process. Ensure Excel is installed and .xlsx files are associated.");

                _uiManager.UpdateProgress("Excel opened. Waiting for you to Refresh All, Save, and Close Excel...");
                await excelProc.WaitForExitAsync(token); // Wait for the user to close Excel.
                _uiManager.UpdateStatusMain("Excel closed by user.");

                // Confirm if the user wants to proceed with emailing after closing Excel.
                DialogResult sendResult = FlexibleMessageBox.Show(this, "Excel has been closed.\n\nProceed with sending the email (if not skipped)?", "Confirm Email Send", MessageBoxButtons.OKCancel, MessageBoxIcon.Question);
                return (sendResult == DialogResult.OK || sendResult == DialogResult.Yes);
            }
            catch (OperationCanceledException) // Handle cancellation during Excel interaction.
            {
                Logger.LogWarning("Manual Excel refresh process was cancelled by timeout or user action.");
                // Attempt to kill the Excel process if it's still running and cancellation occurred.
                if (excelProc != null && !excelProc.HasExited) { try { excelProc.Kill(true); } catch (Exception killEx) { Logger.LogWarning($"Could not kill Excel process during cancellation: {killEx.Message}"); } }
                return false;
            }
            catch (Exception ex) // Handle other errors during Excel interaction.
            {
                Logger.LogError($"Error during manual Excel handling: {ex.Message}", ex);
                FlexibleMessageBox.Show(this, $"An unexpected error occurred managing the Excel refresh step:\n\n{ex.Message}", "Excel Interaction Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
            finally
            {
                excelProc?.Dispose(); // Dispose the process object.
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
            // Ensure the sender is a ComboBox and an item is actually selected.
            if (sender is not ComboBox comboBox || comboBox.SelectedItem == null) return;

            // Get the integer index corresponding to the selected report type text.
            int selectedIndex = GetSelectedReportTypeIndex(comboBox.Text);

            // Special handling if "Custom" report type is selected.
            if (selectedIndex == CustomReportIndex)
            {
                Logger.LogDebug("Report Type changed to Custom. Manual date entry is expected.");
                // Show the "Send to Femi Only?" checkbox, as it's applicable for manual custom reports.
                UIManager.SafeControlUpdate(sendToFemiOnlyCheckBox, () => { sendToFemiOnlyCheckBox.Visible = true; });
                // Hide the informational label about daily report recipients, as it's not relevant for custom.
                UIManager.SafeControlUpdate(emailRecipientLabel, () => { emailRecipientLabel.Visible = false; });
                _uiManager.ResetButtonStatesAfterTypeChange(CheckConfigValidity()); // Reset action button states.
                Update1ClickProcessingModeUI(); // Update visibility of the 1-click button.
                return; // Exit early, as dates are manually controlled for "Custom".
            }

            // For predefined report types, automatically calculate and set the date range.
            DateTime todayValue = DateTime.Today; // Use today as the reference for calculations.
            _programmaticallyChangingDates = true; // Set flag to prevent date picker events from re-triggering this handler.
            try
            {
                DateTime dateFrom = todayValue; // Initialise default start date.
                DateTime dateTo = todayValue;   // Initialise default end date.
                bool showFinYear = true;        // Default to showing the Financial Year ComboBox.

                // Calculate dateFrom, dateTo, and showFinYear based on the selected report type.
                switch (selectedIndex)
                {
                    case DailyReportIndex:
                        dateFrom = ReportHelper.GetPreviousWorkday(todayValue); // Report for the previous working day.
                        dateTo = dateFrom;
                        showFinYear = false; // Financial year is not typically relevant for standard daily reports.
                        break;
                    case NewDailyReportOver1kIndex: // "Daily (5days >= £1k)" report.
                        dateTo = ReportHelper.GetPreviousWorkday(todayValue);     // End date is the previous working day.
                        dateFrom = ReportHelper.GetNthPreviousWorkday(dateTo, 4); // Start date is 4 working days before the end date (total 5 days).
                        showFinYear = false;
                        Logger.LogInfo($"Daily (5days >= £1000) report selected. Dates automatically set to: {dateFrom:dd/MM/yyyy} - {dateTo:dd/MM/yyyy}");
                        break;
                    case WeeklyReportIndex: // Represents a 15-day rolling report.
                        dateTo = todayValue;                // End date is today.
                        dateFrom = todayValue.AddDays(-14); // Start date is 14 days prior, covering a 15-day period.
                        showFinYear = true; // Financial year context might be useful, e.g., for Power BI source files.
                        Logger.LogInfo($"Manual Weekly (15-day) report selected. Dates automatically set to: {dateFrom:dd/MM/yyyy} - {dateTo:dd/MM/yyyy}");
                        break;
                    case MonthlyReportIndex:
                        (dateFrom, dateTo) = ReportHelper.CalculateMonthlyRange(todayValue); // Report for the previous full calendar month.
                        showFinYear = false;
                        break;
                    case QuarterlyReportIndex:
                        (dateFrom, dateTo) = ReportHelper.CalculateQuarterlyRange(todayValue); // Report for the previous full calendar quarter.
                        showFinYear = false;
                        break;
                    case AnnualReportIndex:
                        // Calculate the previous full financial year (assuming May 1st to April 30th).
                        int prevFinancialYearStartCalendarYear = (todayValue.Month >= 5) ? todayValue.Year - 1 : todayValue.Year - 2;
                        (dateFrom, dateTo) = ReportHelper.GetFinancialYearDates(prevFinancialYearStartCalendarYear);
                        showFinYear = false;
                        Logger.LogInfo($"Annual report selected. Dates automatically set for Financial Year: {dateFrom:dd/MM/yyyy} - {dateTo:dd/MM/yyyy}");
                        break;
                    default: // Fallback for any unexpected or unhandled report type index.
                        Logger.LogWarning($"Unexpected reportTypeComboBox index: {selectedIndex} or unmapped item: {comboBox.Text}. Defaulting dates to current picker values.");
                        dateFrom = startDatePicker.Value; // Keep current date picker values.
                        dateTo = endDatePicker.Value;
                        showFinYear = true; // Default to showing financial year in this case.
                        break;
                }

                // Safely update the UI controls with the calculated dates and visibility settings.
                UIManager.SafeControlUpdate(startDatePicker, () => { startDatePicker.Value = dateFrom; });
                UIManager.SafeControlUpdate(endDatePicker, () => { endDatePicker.Value = dateTo; });
                UIManager.SafeControlUpdate(financialYearLabel, () => { financialYearLabel.Visible = showFinYear; });
                UIManager.SafeControlUpdate(financialYearComboBox, () =>
                {
                    financialYearComboBox.Visible = showFinYear;
                    financialYearComboBox.Enabled = showFinYear; // Enable/disable along with visibility.
                    if (showFinYear) PopulateFinancialYearDropdown(); // Populate the dropdown if it's visible.
                });

                // Determine visibility of the "Send to Femi Only?" checkbox.
                // It's generally visible for non-Daily and non-Custom manual reports.
                bool isAnyDailyType = IsAnyDailySelected();
                UIManager.SafeControlUpdate(sendToFemiOnlyCheckBox, () => { sendToFemiOnlyCheckBox.Visible = !isAnyDailyType && selectedIndex != CustomReportIndex; });

                // Update the informational label about email recipients for daily reports.
                UIManager.SafeControlUpdate(emailRecipientLabel, () =>
                {
                    emailRecipientLabel.Visible = isAnyDailyType; // Visible only for daily report types.
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
                        emailRecipientLabel.Visible = false; // Hide for non-daily types.
                    }
                });

                _uiManager.ResetButtonStatesAfterTypeChange(CheckConfigValidity()); // Reset action button states.
                Update1ClickProcessingModeUI(); // Update 1-click button visibility.
            }
            finally
            {
                _programmaticallyChangingDates = false; // Clear the flag after updates are done.
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
            // If _programmaticallyChangingDates is true, it means the date change was initiated by code
            // (e.g., in reportTypeComboBox_SelectedIndexChanged), so we should ignore this event to prevent recursion
            // or unintended side effects.
            if (_programmaticallyChangingDates) return;

            // Get the index of the currently selected report type.
            int currentReportTypeIndex = GetSelectedReportTypeIndex();
            // If the current report type is not already "Custom", change it.
            if (currentReportTypeIndex != CustomReportIndex)
            {
                Logger.LogDebug("DatePicker_ValueChanged: Manual date change detected. Setting Report Type to Custom.");
                // Safely update the reportTypeComboBox on the UI thread.
                UIManager.SafeControlUpdate(reportTypeComboBox, () =>
                {
                    int customIdx = -1;
                    // Iterate through the ComboBox items to find the index of "Custom".
                    for (int i = 0; i < reportTypeComboBox.Items.Count; i++)
                    {
                        if (reportTypeComboBox.Items[i].ToString() == "Custom")
                        {
                            customIdx = i;
                            break; // Found "Custom", no need to continue loop.
                        }
                    }
                    // If "Custom" is found, select it.
                    if (customIdx != -1)
                    {
                        reportTypeComboBox.SelectedIndex = customIdx;
                    }
                    else // Log a warning if "Custom" item is somehow missing.
                    {
                        Logger.LogWarning("DatePicker_ValueChanged: 'Custom' item not found in reportTypeComboBox. This should not happen if UI is initialised correctly.");
                    }
                });
                // Note: Changing reportTypeComboBox.SelectedIndex will itself trigger the
                // reportTypeComboBox_SelectedIndexChanged event, which will then handle
                // further UI adjustments appropriate for the "Custom" report type.
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
            dailyCheckTimer.Enabled = !dailyCheckTimer.Enabled; // Toggle the Enabled state of the timer.

            // Determine if the auto-run process has already reached a final state for the current day
            // (e.g., all reports completed, or a critical failure occurred).
            bool isAutoRunCompletedForToday = (autoRunStatusLabel.Text?.Contains("Completed") ?? false) ||
                                              (autoRunStatusLabel.Text?.Contains("Done for") ?? false) ||
                                              (autoRunStatusLabel.Text?.Contains("FAILED") ?? false);

            // Call the UIManager to update all relevant UI elements (button text, colour, status label text and colour).
            _uiManager.UpdateAutoRunUI(
                dailyCheckTimer.Enabled,          // Current state of the timer.
                isAutoRunCompletedForToday,       // Whether a final status for today has been reached.
                darkModeToolStripMenuItem.Checked, // Current dark mode state (for text colour decisions).
                                                   // Construct the base status message.
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
            // Do nothing if the timer is currently disabled.
            if (!dailyCheckTimer.Enabled) return;

            bool originallyEnabled = dailyCheckTimer.Enabled; // Store the timer's state before stopping it.
            dailyCheckTimer.Stop(); // Stop the timer temporarily to prevent re-entrant calls during the check.
            Logger.LogDebug("Daily Check Timer Ticked. Attempting to perform daily auto-run check.");

            AutoRunActionResult autoRunResult = AutoRunActionResult.NoActionNeeded; // Initialise to a default state.

            try
            {
                // Delegate the core auto-run check logic to the AutoRunManager.
                // This keeps the Form1 class cleaner and separates concerns.
                autoRunResult = await _autoRunManager.PerformDailyCheckAsync(originallyEnabled, _currentAutoRunHour);
            }
            catch (Exception ex) // Catch any unhandled exceptions from the auto-run process.
            {
                Logger.LogCritical($"CRITICAL ERROR during AutoRunManager.PerformDailyCheckAsync dispatch from timer: {ex.Message}", ex);
                // Update UI to reflect the critical error.
                _uiManager.UpdateStatusMain("Critical AutoRun Error! Check Logs.");
                _uiManager.UpdateStatusRight("AutoRun: FAILED");
                _uiManager.UpdateAutoRunUI(dailyCheckTimer.Enabled, true, darkModeToolStripMenuItem.Checked, "AutoRun: FAILED (Timer Error)");
                autoRunResult = AutoRunActionResult.CriticalError; // Set result to indicate critical error.
            }
            finally // This block executes regardless of whether an exception occurred.
            {
                // Restart the timer if it was originally enabled and no critical error occurred during the check.
                if (originallyEnabled && autoRunResult != AutoRunActionResult.CriticalError)
                {
                    dailyCheckTimer.Start();
                    Logger.LogDebug("Daily Check Timer Restarted after auto-run check.");
                }
                else if (autoRunResult == AutoRunActionResult.CriticalError)
                {
                    // If a critical error occurred, the timer remains stopped to prevent further issues.
                    Logger.LogWarning("Daily Check Timer remains stopped due to a critical error during the auto-run check.");
                }

                // If the auto-run process attempted an action or encountered a critical error,
                // the main UI state (action buttons, etc.) might need to be reset.
                if (autoRunResult == AutoRunActionResult.ActionAttempted || autoRunResult == AutoRunActionResult.CriticalError)
                {
                    Logger.LogInfo($"AutoRun action result '{autoRunResult}' indicates UI may need reset.");
                    // Determine the appropriate text for the main action button upon reset.
                    string mainButtonResetText = enable1ClickProcessingToolStripMenuItem.Checked ?
                                                 (CheckConfigValidity() ? "Generate, Process & Email Report" : "Config Error") :
                                                 (CheckConfigValidity() ? "Create Report" : "Config Error");
                    ResetUIStateOnError(mainButtonResetText); // Call the UI reset helper.
                }
                else // If no action was needed by the auto-run manager.
                {
                    // Ensure the auto-run toggle button is re-enabled if it was disabled during the check.
                    // This check is important if the AutoRunManager or UI manager might disable it.
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
            bool isChecked = darkModeToolStripMenuItem.Checked; // Get the new checked state of the menu item.
            _uiManager.ApplyTheme(isChecked); // Apply the selected theme to the entire UI.

            // After applying the theme, update the AutoRun UI elements as their appearance
            // (e.g., text colour) might depend on the current theme.
            bool isAutoRunFinalStatusForToday = (autoRunStatusLabel.Text?.Contains("Completed") ?? false) ||
                                              (autoRunStatusLabel.Text?.Contains("Done for") ?? false) ||
                                              (autoRunStatusLabel.Text?.Contains("FAILED") ?? false);

            string autoRunStatusTextToShow;
            // Determine the text for the auto-run status label based on timer state and completion status.
            if (dailyCheckTimer.Enabled)
            {
                // If timer is enabled, show "Enabled" status, preserving final status text if applicable.
                autoRunStatusTextToShow = isAutoRunFinalStatusForToday ?
                                          (autoRunStatusLabel.Text ?? $"Auto Run: Enabled (Next check ~{_currentAutoRunHour}:00)") :
                                          $"Auto Run: Enabled (Next check ~{_currentAutoRunHour}:00)";
            }
            else
            {
                // If timer is disabled, show "Disabled" status, preserving final status text if applicable.
                autoRunStatusTextToShow = isAutoRunFinalStatusForToday ?
                                          (autoRunStatusLabel.Text ?? "Auto Run: Disabled") :
                                          "Auto Run: Disabled";
            }

            // Update the auto-run UI elements with the determined text and current theme.
            _uiManager.UpdateAutoRunUI(dailyCheckTimer.Enabled, isAutoRunFinalStatusForToday, isChecked, autoRunStatusTextToShow);
            Logger.LogInfo($"Dark Mode toggled via menu. New state: {(isChecked ? "Enabled" : "Disabled")}");
        }

        /// <summary>
        /// Handles the Click event for the `helpToolStripMenuItem`.
        /// Displays the application's help information in a separate, non-modal <see cref="HelpForm"/>.
        /// The help content is dynamically generated as RTF (Rich Text Format) to include formatting.
        /// </summary>
        /// <param name="sender">The source of the event (the helpToolStripMenuItem).</param>
        /// <param name="e">An <see cref="EventArgs"/> that contains no event data.</param>
        private void helpToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Logger.LogTrace("Help menu item clicked.");
            // Construct the title for the help window, including the application version.
            string helpTitle = $"Help - Quote Conversion Automation v{AppVersion}";

            // Use a StringBuilder to efficiently construct the RTF content for the help message.
            StringBuilder helpMessageBuilder = new StringBuilder();
            bool isDarkModeActive = darkModeToolStripMenuItem.Checked; // Get current theme for RTF colour definitions.

            // Define RTF colour codes based on the current theme (dark/light).
            // This allows the help content to visually match the application's theme.
            string rtfDefaultTextColor = isDarkModeActive ? @"\red220\green220\blue220;" : @"\red0\green0\blue0;";        // Default text colour.
            string rtfHeaderColor = isDarkModeActive ? @"\red120\green220\blue250;" : @"\red0\green0\blue128;";       // Colour for main section headers.
            string rtfSubHeaderColor = isDarkModeActive ? @"\red100\green180\blue220;" : @"\red0\green100\blue0;";     // Colour for sub-section headers.
            string rtfAccentColor = isDarkModeActive ? @"\red255\green160\blue160;" : @"\red200\green0\blue0;";       // Colour for warnings or important notes.
            string rtfBulletColor = isDarkModeActive ? @"\red180\green180\blue180;" : @"\red80\green80\blue80;";       // Colour for bullet points.
            string rtfCodeColor = isDarkModeActive ? @"\red180\green210\blue180;" : @"\red40\green100\green40;";         // Colour for file paths or code-like text.
            string rtfEmphasisColor = isDarkModeActive ? @"\red255\green210\blue100;" : @"\red139\green69\blue19;";    // Colour for emphasised UI element names.

            // Start the RTF document structure: font table, colour table, default paragraph settings.
            helpMessageBuilder.AppendLine(@"{\rtf1\ansi\ansicpg1252\deff0\nouicompat{\fonttbl{\f0\fnil\fcharset0 Segoe UI;}{\f1\fnil\fcharset2 Symbol;}}"); // Font table (Segoe UI, Symbol for bullets).
            helpMessageBuilder.AppendLine($@"{{\colortbl ;{rtfDefaultTextColor}{rtfHeaderColor}{rtfSubHeaderColor}{rtfAccentColor}{rtfBulletColor}{rtfCodeColor}{rtfEmphasisColor}}}"); // Colour table definition.
            helpMessageBuilder.AppendLine(@"\pard\cf1\sa200\sl276\slmult1\f0\fs20"); // Default paragraph: colour 1 (default text), spacing, Segoe UI font, 10pt size.

            // --- Add RTF content for each help section ---
            helpMessageBuilder.AppendLine($@"\b\fs32\cf2 Quote Conversion Report Automation v{AppVersion}\b0\fs20\cf1\par"); // Main Title
            helpMessageBuilder.AppendLine(@"\par"); // Paragraph break

            helpMessageBuilder.AppendLine(@"\b\fs24\cf2 Introduction\b0\fs20\cf1\par"); // Section Header
            helpMessageBuilder.AppendLine(@"Welcome to the Quote Conversion Report Automation programme! This tool is designed to streamline and automate the process of generating, processing, and distributing various quote conversion reports. It aims to reduce manual effort, improve consistency, and provide timely information.\par");
            helpMessageBuilder.AppendLine(@"The programme offers both manual control via a user interface and powerful automated capabilities for scheduled report generation.\par");
            helpMessageBuilder.AppendLine(@"\par"); // Paragraph break

            helpMessageBuilder.AppendLine(@"\b\fs24\cf2 How the Application Works (Overview)\b0\fs20\cf1\par");
            helpMessageBuilder.AppendLine(@"The application orchestrates several components:\par");
            helpMessageBuilder.AppendLine(@"   \pard\fi-360\li720{\pncf5\pntext\f1\'B7\tab}\cf1 \b User Interface (UI):\b0  Allows for manual selection of report types, date ranges, and processing options. It also provides access to configuration settings and logs.\par");
            helpMessageBuilder.AppendLine(@"   \pard\fi-360\li720{\pncf5\pntext\f1\'B7\tab}\cf1 \b Crystal Report Wrapper:\b0  An external service used to extract raw report data from the primary business system (via Crystal Reports).\par");
            helpMessageBuilder.AppendLine(@"   \pard\fi-360\li720{\pncf5\pntext\f1\'B7\tab}\cf1 \b Excel Processing:\b0  Utilises templates to process the raw data into final, formatted Excel reports. This includes data cleaning, calculations, and potentially filtering (e.g., for the 'Daily 5-days >= £1k' report).\par");
            helpMessageBuilder.AppendLine(@"   \pard\fi-360\li720{\pncf5\pntext\f1\'B7\tab}\cf1 \b Email Distribution:\b0  Sends the final reports to configured recipients via SMTP.\par");
            helpMessageBuilder.AppendLine(@"   \pard\fi-360\li720{\pncf5\pntext\f1\'B7\tab}\cf1 \b Automation Engine:\b0  Handles scheduled, automated generation and emailing of predefined reports based on configuration.\par");
            helpMessageBuilder.AppendLine(@"   \pard\fi-360\li720{\pncf5\pntext\f1\'B7\tab}\cf1 \b Configuration Files:\b0  Uses {\cf6 `appsettings.json`} for main settings and user-specific JSON files (in your AppData folder) for customisations like email recipients, greetings, and bank holidays.\par");
            helpMessageBuilder.AppendLine(@"\pard\sa200\sl276\slmult1\cf1\par"); // Reset paragraph for next section

            helpMessageBuilder.AppendLine(@"\b\fs24\cf2 How to Use the Application (Manual Operation)\b0\fs20\cf1\par");
            helpMessageBuilder.AppendLine(@"\b\fs22\cf3 Main Interface Elements:\b0\fs20\cf1\par");
            helpMessageBuilder.AppendLine(@"   \pard\fi-360\li720{\pncf5\pntext\f1\'B7\tab}\cf1 {\cf7\b Report Type:}\b0\cf1  Dropdown to select the desired report period (e.g., Daily, Weekly).\par");
            helpMessageBuilder.AppendLine(@"   \pard\fi-360\li720{\pncf5\pntext\f1\'B7\tab}\cf1 {\cf7\b From / To Dates:}\b0\cf1  Date pickers for defining the report range. These are often auto-filled based on the Report Type.\par");
            helpMessageBuilder.AppendLine(@"   \pard\fi-360\li720{\pncf5\pntext\f1\'B7\tab}\cf1 {\cf7\b Financial Year:}\b0\cf1  Dropdown (visible for certain report types like Weekly or Custom) to specify the financial year context, primarily for Power BI source file updates.\par");
            helpMessageBuilder.AppendLine(@"   \pard\fi-360\li720{\pncf5\pntext\f1\'B7\tab}\cf1 {\cf7\b Report Settings Group:}\b0\cf1\par");
            helpMessageBuilder.AppendLine(@"      \pard\fi-720\li1080{\pncf5\pntext\f1\'B7\tab}\cf1 {\cf7\b Send to only Femi?:}\b0\cf1  (Visible for non-Daily, non-Custom manual reports) Restricts email distribution to a specific IT/admin list.\par");
            helpMessageBuilder.AppendLine(@"      \pard\fi-720\li1080{\pncf5\pntext\f1\'B7\tab}\cf1 {\cf7\b Skip Sending Email:}\b0\cf1  If ticked, the programme will generate and process the report files locally but will not send an email.\par");
            helpMessageBuilder.AppendLine(@"   \pard\fi-360\li720{\pncf5\pntext\f1\'B7\tab}\cf1 {\cf7\b Action Buttons:}\b0\cf1  Used to generate and process reports. The appearance depends on the '1-Click Processing' mode (see Options Menu):\par");
            helpMessageBuilder.AppendLine(@"      \pard\fi-720\li1080{\pncf5\pntext\f1\'B7\tab}\cf1 {\i Standard Mode (2-button):}\b0  {\cf7\b Create Report}\b0\cf1  (generates raw data) then {\cf7\b Process & Email Report}\b0\cf1  (processes raw data and emails).\par");
            helpMessageBuilder.AppendLine(@"      \pard\fi-720\li1080{\pncf5\pntext\f1\'B7\tab}\cf1 {\i 1-Click Mode:}\b0  A single {\cf7\b Generate, Process & Email Report}\b0\cf1  button performs all steps.\par");
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
            helpMessageBuilder.AppendLine(@"   \pard\fi-360\li720{\pncf5\pntext\f1\'B7\tab}\cf1 {\cf7\b Annual:}\b0\cf1  Report for the {\i previous full financial year (1st May - 30th April)}.\par");
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
            helpMessageBuilder.AppendLine(@"   \pard\fi-360\li720{\pncf5\pntext\f1\'B7\tab}\cf1 Go to the {\b 'Data'}\b0  tab in Excel and click {\b 'Refresh All'}.\par");
            helpMessageBuilder.AppendLine(@"   \pard\fi-360\li720{\pncf5\pntext\f1\'B7\tab}\cf1 Specifically, ensure PivotTables on the 'OrderPivot' and 'Estimate Success PivotTable' sheets are updated. You may need to right-click them and select 'Refresh'. Check any Slicers as well.\par");
            helpMessageBuilder.AppendLine(@"   \pard\fi-360\li720{\pncf5\pntext\f1\'B7\tab}\cf1 {\b SAVE}\b0  the Excel file.\par");
            helpMessageBuilder.AppendLine(@"   \pard\fi-360\li720{\pncf5\pntext\f1\'B7\tab}\cf1 {\b CLOSE}\b0  Excel.\par");
            helpMessageBuilder.AppendLine(@"   \pard\fi-360\li720{\pncf5\pntext\f1\'B7\tab}\cf1 The application will then ask you to confirm if you want to proceed with emailing the (now refreshed) report.\par");
            helpMessageBuilder.AppendLine(@"\pard\sa200\sl276\slmult1\cf1\par");
            helpMessageBuilder.AppendLine(@"\b 7. Viewing Reports:\b0\par");
            helpMessageBuilder.AppendLine(@"   \pard\fi-360\li720{\pncf5\pntext\f1\'B7\tab}\cf1 After the raw report is generated, the {\cf7\b View Raw Report}\b0\cf1  button becomes active. Click it to open the raw Excel data file.\par");
            helpMessageBuilder.AppendLine(@"   \pard\fi-360\li720{\pncf5\pntext\f1\'B7\tab}\cf1 After the report is processed (and emailed, if not skipped), the {\cf7\b View Processed Analysis}\b0\cf1  button becomes active. Click it to open the final, formatted Excel report.\par");
            helpMessageBuilder.AppendLine(@"\pard\sa200\sl276\slmult1\cf1\par");

            helpMessageBuilder.AppendLine(@"\b\fs24\cf2 Options Menu Explained\b0\fs20\cf1\par");
            helpMessageBuilder.AppendLine(@"The 'Options' menu provides access to various settings and tools:\par");
            helpMessageBuilder.AppendLine(@"   \pard\fi-360\li720{\pncf5\pntext\f1\'B7\tab}\cf1 {\cf7\b Enable 1-Click Processing:}\b0\cf1  Toggles between the standard two-button mode ('Create Report' then 'Process & Email') and a single 'Generate, Process & Email Report' button.\par");
            helpMessageBuilder.AppendLine($@"   \pard\fi-360\li720{{\pncf5\pntext\f1\'B7\tab}}\cf1 {{\cf7\b Set Auto-Run Hour...:}}\b0\cf1  Allows you to change the hour (0-23) for the daily automated report check. The current setting is approximately {{\b {_currentAutoRunHour}:00\b0}}\b0\cf1 .\par");
            helpMessageBuilder.AppendLine(@"   \pard\fi-360\li720{\pncf5\pntext\f1\'B7\tab}\cf1 {\cf7\b Configure Auto-Run Reports:}\b0\cf1  A sub-menu to individually enable or disable specific automated reports:\par");
            helpMessageBuilder.AppendLine(@"      \pard\fi-720\li1080{\pncf5\pntext\f1\'B7\tab}\cf1 Enable Standard Daily Auto Report\par");
            helpMessageBuilder.AppendLine(@"      \pard\fi-720\li1080{\pncf5\pntext\f1\'B7\tab}\cf1 Enable Daily (5days >= £1000) Auto Report\par");
            helpMessageBuilder.AppendLine(@"      \pard\fi-720\li1080{\pncf5\pntext\f1\'B7\tab}\cf1 Enable Weekly Auto Report\par");
            helpMessageBuilder.AppendLine(@"   \pard\fi-360\li720{\pncf5\pntext\f1\'B7\tab}\cf1 {\cf7\b Dark Mode:}\b0\cf1  Toggles the application's visual theme between light and dark mode.\par");
            helpMessageBuilder.AppendLine(@"   \pard\fi-360\li720{\pncf5\pntext\f1\'B7\tab}\cf1 {\cf7\b View Configuration:}\b0\cf1  Displays a summary of critical file paths and settings used by the application, helping to diagnose configuration issues.\par");
            helpMessageBuilder.AppendLine(@"   \pard\fi-360\li720{\pncf5\pntext\f1\'B7\tab}\cf1 {\cf7\b Validate Configuration:}\b0\cf1  Performs a quick check of essential configurations (e.g., Crystal Report path, Wrapper EXE path) and updates the status bar.\par");
            helpMessageBuilder.AppendLine(@"   \pard\fi-360\li720{\pncf5\pntext\f1\'B7\tab}\cf1 {\cf7\b Manage Custom Bank Holidays:}\b0\cf1  Opens a window to add, view, or remove custom one-off or recurring bank holidays. These are used in 'previous working day' calculations.\par");
            helpMessageBuilder.AppendLine(@"   \pard\fi-360\li720{\pncf5\pntext\f1\'B7\tab}\cf1 {\cf7\b Manage Email Recipients:}\b0\cf1  Opens a window to customise the 'To' and 'CC' email lists for different report scenarios (automated, manual, debug). User changes are saved and override application defaults.\par");
            helpMessageBuilder.AppendLine(@"   \pard\fi-360\li720{\pncf5\pntext\f1\'B7\tab}\cf1 {\cf7\b Manage Email Greetings:}\b0\cf1  Opens a window to customise the introductory greeting text for emails in different scenarios. User changes are saved and override application defaults.\par");
            helpMessageBuilder.AppendLine(@"   \pard\fi-360\li720{\pncf5\pntext\f1\'B7\tab}\cf1 {\cf7\b Open Logs Folder:}\b0\cf1  Opens the directory where the application's detailed log files are stored. This is crucial for troubleshooting.\par");
            helpMessageBuilder.AppendLine($@"   \pard\fi-360\li720{{\pncf5\pntext\f1\'B7\tab}}\cf1 {{\cf7\b Edit appsettings.json:}}\b0\cf1  Opens the main configuration file (`appsettings.json`) in the default text editor. {{\i\cf4 Use with extreme caution! Incorrect changes can break the application.}}\cf1\par");
            helpMessageBuilder.AppendLine(@"   \pard\fi-360\li720{\pncf5\pntext\f1\'B7\tab}\cf1 {\cf7\b Exit:}\b0\cf1  Closes the application.\par");
            helpMessageBuilder.AppendLine(@"\pard\sa200\sl276\slmult1\cf1\par");

            helpMessageBuilder.AppendLine(@"\b\fs24\cf2 Automated Features\b0\fs20\cf1\par");
            helpMessageBuilder.AppendLine(@"\b\fs22\cf3 Auto-Run Feature:\b0\fs20\cf1\par");
            helpMessageBuilder.AppendLine($@"When the {{\cf7\b Enable Daily Auto Run @ {{\b {_currentAutoRunHour}:00\b0}}}}\b0\cf1  button shows green (enabled), the application will automatically check around {{\b {_currentAutoRunHour}:00\b0}}\b0\cf1  each day to run any pending automated reports. \par");
            helpMessageBuilder.AppendLine(@"The following reports can be configured for auto-run via the {\cf7\b Options -> Configure Auto-Run Reports}\b0\cf1  menu:\par");
            helpMessageBuilder.AppendLine(@"   \pard\fi-360\li720{\pncf5\pntext\f1\'B7\tab}\cf1 Standard Daily Report\par");
            helpMessageBuilder.AppendLine(@"   \pard\fi-360\li720{\pncf5\pntext\f1\'B7\tab}\cf1 Daily (5days >= £1000) Report\par");
            helpMessageBuilder.AppendLine(@"   \pard\fi-360\li720{\pncf5\pntext\f1\'B7\tab}\cf1 Weekly Report (typically runs on a specific day like Friday)\par");
            helpMessageBuilder.AppendLine(@"Automated reports use email recipients and greetings configured for their specific auto-run scenario (see 'Manage Email Recipients' and 'Manage Email Greetings'). The status of the auto-run process is displayed in the right-hand side of the status bar.\par");
            helpMessageBuilder.AppendLine(@"The application keeps track of which reports have successfully run for the day in `appsettings.json` to avoid duplicate runs if the application is restarted.\par");
            helpMessageBuilder.AppendLine(@"\pard\sa200\sl276\slmult1\cf1\par");

            helpMessageBuilder.AppendLine(@"\b\fs22\cf3 Automated Archiving:\b0\fs20\cf1\par");
            helpMessageBuilder.AppendLine(@"The application performs automated archiving on start-up:\par");
            helpMessageBuilder.AppendLine(@"   \pard\fi-360\li720{\pncf5\pntext\f1\'B7\tab}\cf1 \b Log Files:\b0  Log files older than 7 days are moved to an 'Archive' subfolder within your user-specific log directory, structured by Year, Month, and Week number (e.g., {\cf6 Logs\\[YourUser]\Archive\\YYYY\\MM\\WeekN}).\par");
            helpMessageBuilder.AppendLine(@"   \pard\fi-360\li720{\pncf5\pntext\f1\'B7\tab}\cf1 \b Report Files:\b0 \par");
            helpMessageBuilder.AppendLine(@"      \pard\fi-720\li1080{\pncf5\pntext\f1\'B7\tab}\cf1 {\i Final Reports (e.g., in 'Estimates' folder):}\b0  Folders representing previous calendar years (e.g., a '2023' folder within 'Weekly Reports') are moved into a main 'Archive' folder at the root of the final reports directory (e.g., {\cf6 Estimates\\Archive\\Weekly Reports\\2023}). If the target archive year folder already exists, contents are merged.\par");
            helpMessageBuilder.AppendLine(@"      \pard\fi-720\li1080{\pncf5\pntext\f1\'B7\tab}\cf1 {\i Raw Reports (e.g., in 'Estimate Reports Exports' folder):}\b0  Individual raw report files (.xlsx) older than a configured number of days (default 30) are moved into an 'Archive\\YYYY-MM' subfolder within their respective report type directory (e.g., {\cf6 Estimate Reports Exports\\Daily Reports\\Archive\\YYYY-MM\\OldFile.xlsx}).\par");
            helpMessageBuilder.AppendLine(@"\pard\sa200\sl276\slmult1\cf1\par");

            helpMessageBuilder.AppendLine(@"\b\fs24\cf2 Configuration Files Overview\b0\fs20\cf1\par");
            helpMessageBuilder.AppendLine(@"The application uses several configuration files:\par");
            helpMessageBuilder.AppendLine(@"   \pard\fi-360\li720{\pncf5\pntext\f1\'B7\tab}\cf1 {\cf6\b appsettings.json:}\b0\cf1  The main configuration file located with the application. It stores critical paths (Crystal Report, Wrapper EXE, Template base), SMTP server details, default email recipients and greetings, auto-run settings (like `LastRunDate`, `DailyRunStatus`, enabled reports), and logging levels. {\cf4\i Modifying this file directly requires caution.}\b0\cf1\par");
            helpMessageBuilder.AppendLine(@"   \pard\fi-360\li720{\pncf5\pntext\f1\'B7\tab}\cf1 \b User-Specific JSON Files:\b0  These are stored in your user profile's AppData directory (typically {\cf6 %APPDATA%\\HarlowSolutions\\QuoteConversionReportAutomation\\}). They allow for personal customisations without altering the main `appsettings.json`:\par");
            helpMessageBuilder.AppendLine(@"      \pard\fi-720\li1080{\pncf5\pntext\f1\'B7\tab}\cf1 {\cf6 user_email_settings.json:}\b0\cf1  Stores your custom email recipient lists, overriding the defaults from `appsettings.json`.\par");
            helpMessageBuilder.AppendLine(@"      \pard\fi-720\li1080{\pncf5\pntext\f1\'B7\tab}\cf1 {\cf6 user_greeting_settings.json:}\b0\cf1  Stores your custom email greetings.\par");
            helpMessageBuilder.AppendLine(@"      \pard\fi-720\li1080{\pncf5\pntext\f1\'B7\tab}\cf1 {\cf6 custom_bank_holidays.json:}\b0\cf1  Stores any custom bank holidays you have defined.\par");
            helpMessageBuilder.AppendLine(@"\pard\sa200\sl276\slmult1\cf1\par");

            helpMessageBuilder.AppendLine(@"\b\fs24\cf2 Troubleshooting\b0\fs20\cf1\par");
            helpMessageBuilder.AppendLine(@"If you encounter issues, consider the following steps:\par");
            helpMessageBuilder.AppendLine(@"   \pard\fi-360\li720{\pncf5\pntext\f1\'B7\tab}\cf1 \b ""Config Error"" Status:\b0  This usually indicates a problem with essential file paths. \par");
            helpMessageBuilder.AppendLine(@"      \pard\fi-720\li1080{\pncf5\pntext\f1\'B7\tab}\cf1 Go to {\cf7\b Options -> View Configuration}\b0\cf1  to check the paths for the Crystal Report file (.rpt), the Crystal Report Wrapper executable (.exe), and the Excel Template base directory. \par");
            helpMessageBuilder.AppendLine(@"      \pard\fi-720\li1080{\pncf5\pntext\f1\'B7\tab}\cf1 Ensure all listed files and folders exist and are accessible from your machine. The application may require network access to some of these paths.\par");
            helpMessageBuilder.AppendLine(@"   \pard\fi-360\li720{\pncf5\pntext\f1\'B7\tab}\cf1 \b Report Generation Fails (Raw Report):\b0 \par");
            helpMessageBuilder.AppendLine(@"      \pard\fi-720\li1080{\pncf5\pntext\f1\'B7\tab}\cf1 Ensure the Crystal Report Wrapper service (`CrystalReportWrapper.exe`) is running or can be started by the application. \par");
            helpMessageBuilder.AppendLine(@"      \pard\fi-720\li1080{\pncf5\pntext\f1\'B7\tab}\cf1 Verify the Crystal Report file path in `appsettings.json` (viewable via {\cf7\b Options -> View Configuration}\b0\cf1 ) is correct and the file is accessible.\par");
            helpMessageBuilder.AppendLine(@"      \pard\fi-720\li1080{\pncf5\pntext\f1\'B7\tab}\cf1 Check the application logs for specific error messages from the wrapper service (see 'Open Logs Folder' below).\par");
            helpMessageBuilder.AppendLine(@"   \pard\fi-360\li720{\pncf5\pntext\f1\'B7\tab}\cf1 \b Excel Processing Fails (Analysis Report):\b0 \par");
            helpMessageBuilder.AppendLine(@"      \pard\fi-720\li1080{\pncf5\pntext\f1\'B7\tab}\cf1 Ensure the Excel template files (e.g., `TEMPLATE_Estimate Success Rate.xlsx`) exist in the configured 'TEMPLATE' directory and are not corrupted or password-protected.\par");
            helpMessageBuilder.AppendLine(@"      \pard\fi-720\li1080{\pncf5\pntext\f1\'B7\tab}\cf1 Verify the application has write permissions to the 'Raw Report Export' and 'Final Excel Save Location' directories (viewable via {\cf7\b Options -> View Configuration}\b0\cf1 ).\par");
            helpMessageBuilder.AppendLine(@"      \pard\fi-720\li1080{\pncf5\pntext\f1\'B7\tab}\cf1 For 'Weekly' reports, ensure the central Power BI source Excel file (typically in your user's `Harlow Printing\\IT - Documents\\PowerBI\\...` path) is accessible and not locked by another user or process.\par");
            helpMessageBuilder.AppendLine(@"   \pard\fi-360\li720{\pncf5\pntext\f1\'B7\tab}\cf1 \b Email Sending Fails:\b0 \par");
            helpMessageBuilder.AppendLine(@"      \pard\fi-720\li1080{\pncf5\pntext\f1\'B7\tab}\cf1 Check SMTP server settings in `appsettings.json` (server address, port, username/password if required, SSL setting). These are usually pre-configured by IT.\par");
            helpMessageBuilder.AppendLine(@"      \pard\fi-720\li1080{\pncf5\pntext\f1\'B7\tab}\cf1 Ensure your network connection allows outgoing SMTP traffic on the configured port.\par");
            helpMessageBuilder.AppendLine(@"      \pard\fi-720\li1080{\pncf5\pntext\f1\'B7\tab}\cf1 Verify email recipients via {\cf7\b Options -> Manage Email Recipients}\b0\cf1  and greetings via {\cf7\b Options -> Manage Email Greetings}\b0\cf1 . Invalid email addresses can cause failures.\par");
            helpMessageBuilder.AppendLine(@"   \pard\fi-360\li720{\pncf5\pntext\f1\'B7\tab}\cf1 \b Auto-Run Not Working as Expected:\b0 \par");
            helpMessageBuilder.AppendLine($@"      \pard\fi-720\li1080{{\pncf5\pntext\f1\'B7\tab}}\cf1 Confirm the main auto-run feature is enabled (button should be green and show 'Disable Daily Auto Run @ {{\b {_currentAutoRunHour}:00\b0}}').\par");
            helpMessageBuilder.AppendLine(@"      \pard\fi-720\li1080{\pncf5\pntext\f1\'B7\tab}\cf1 Check which specific reports are enabled for auto-run via {\cf7\b Options -> Configure Auto-Run Reports}\b0\cf1 .\par");
            helpMessageBuilder.AppendLine($@"      \pard\fi-720\li1080{{\pncf5\pntext\f1\'B7\tab}}\cf1 Check the configured 'Auto-Run Hour' via {{\cf7\b Options -> Set Auto-Run Hour...}}\b0\cf1  (currently ~{{\b {_currentAutoRunHour}:00\b0}}\b0\cf1 ). The check happens around this hour.\par");
            helpMessageBuilder.AppendLine(@"      \pard\fi-720\li1080{\pncf5\pntext\f1\'B7\tab}\cf1 Review `appsettings.json` for `AutoReport:LastRunDate` and `AutoReport:DailyRunStatus`. If `LastRunDate` is today, or if the specific report's success flag in `DailyRunStatus` (for today's `StatusDate`) is true, it generally won't run again until the next due time/day.\par");
            helpMessageBuilder.AppendLine(@"   \pard\fi-360\li720{\pncf5\pntext\f1\'B7\tab}\cf1 \b Incorrect Formulae in Excel Output:\b0  The application copies formulae from the first data row (typically row 6) of the 'Analysis' sheet in the template file. If these formulae are incorrect in the template, the output will also be incorrect.\par");
            helpMessageBuilder.AppendLine(@"   \pard\fi-360\li720{\pncf5\pntext\f1\'B7\tab}\cf1 \b User Settings Not Taking Effect (Recipients/Greetings/Bank Holidays):\b0  Custom settings are stored in JSON files in your user's AppData folder (e.g., {\cf6 %APPDATA%\\HarlowSolutions\\QuoteConversionReportAutomation\\}). If changes aren't applying, check if these files (`user_email_settings.json`, `user_greeting_settings.json`, `custom_bank_holidays.json`) are writable or have become corrupted. Deleting a corrupted user settings file will cause the application to revert to the defaults from `appsettings.json` for that specific setting type.\par");
            helpMessageBuilder.AppendLine(@"   \pard\fi-360\li720{\pncf5\pntext\f1\'B7\tab}\cf1 \b Excel Slicers/Pivot Tables Not Updating (Manual Refresh Reports):\b0  For reports requiring manual refresh (Monthly, Quarterly, Annual, Custom), ensure you follow the on-screen prompts carefully: Open Excel, {\b Enable Editing}, go to the {\b Pivot Sheets} and right click and press {\b Refresh} for all Pivots and Slicers, then {\b Save} and {\b Close} Excel before confirming in the application.\par");
            helpMessageBuilder.AppendLine(@"   \pard\fi-360\li720{\pncf5\pntext\f1\'B7\tab}\cf1 {\cf4\b Check Application Logs:}\b0\cf1  The most detailed error information is usually found in the log files. Access these via {\cf7\b Options -> Open Logs Folder}\b0\cf1 . Log files are created daily and are named with the date (e.g., `YYYY-MM-DD_LogFile.log`).\par");
            helpMessageBuilder.AppendLine(@"   \pard\fi-360\li720{\pncf5\pntext\f1\'B7\tab}\cf1 \b Contact IT Support:\b0  If problems persist after checking these steps, please contact IT support with details of the issue and any relevant error messages from the logs.\par");
            helpMessageBuilder.AppendLine(@"\pard\sa200\sl276\slmult1\cf1\par");

            helpMessageBuilder.AppendLine(@"Thank you for using the Quote Conversion Report Automation programme!\par"); // Closing remark
            helpMessageBuilder.AppendLine(@"}"); // End of RTF document.
            /**END HELP TEXT**/

            string helpMessage = helpMessageBuilder.ToString(); // Get the complete RTF string.

            try
            {
                // Manage the HelpForm instance: create if null/disposed, otherwise activate existing.
                // This prevents multiple help windows from being opened.
                if (_helpFormInstance == null || _helpFormInstance.IsDisposed)
                {
                    _helpFormInstance = new HelpForm(helpTitle, helpMessage, darkModeToolStripMenuItem.Checked); // Pass title, RTF content, and current theme.
                    // Subscribe to FormClosed event to nullify the instance variable, allowing a new HelpForm to be created next time.
                    _helpFormInstance.FormClosed += (s, args) => _helpFormInstance = null;
                    _helpFormInstance.Show(this); // Show the HelpForm non-modally, owned by the main form.
                }
                else
                {
                    _helpFormInstance.Activate(); // If already open, bring it to the front.
                }
            }
            catch (Exception ex) // Handle any errors that occur while trying to display the HelpForm.
            {
                Logger.LogError($"Failed to show HelpForm: {ex.Message}", ex);
                FlexibleMessageBox.Show(this, "Could not display help window. Please check application logs.", "Help Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Handles the Click event for the `viewConfigToolStripMenuItem`.
        /// Displays a summary of critical application configuration settings (file paths, auto-run hour, etc.)
        /// in a <see cref="FlexibleMessageBox"/>. This is useful for diagnostics.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">An <see cref="EventArgs"/> that contains no event data.</param>
        private void viewConfigToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Logger.LogInfo("Options -> View Configuration clicked.");
            bool configValid = CheckConfigValidity(); // First, check if the essential configuration is currently valid.
            var sb = new System.Text.StringBuilder(); // Use StringBuilder for efficient string concatenation.

            // Append various configuration details to the StringBuilder.
            sb.AppendLine("Configuration Details (Paths are relative to user profile where applicable):");
            sb.AppendLine("--------------------------------------------------");
            sb.AppendLine($"1. Crystal Report Path (.rpt): '{CrystalReportLocation}'");
            sb.AppendLine($"   - Exists: {File.Exists(CrystalReportLocation)}"); // Check if the file exists.
            sb.AppendLine($"2. Wrapper EXE Path: '{Path.GetFullPath(_configuration["settings:WrapperExePath"] ?? string.Empty)}'");
            sb.AppendLine($"   - Exists: {File.Exists(Path.GetFullPath(_configuration["settings:WrapperExePath"] ?? string.Empty))}");
            sb.AppendLine($"3. Template Base Directory: '{ExcelTemplateBaseDir}'");
            sb.AppendLine($"   - Exists: {Directory.Exists(ExcelTemplateBaseDir)}"); // Check if the directory exists.
            sb.AppendLine($"4. Raw Report Export Base Directory: '{RawReportExportBaseDir}'");
            sb.AppendLine($"   - Exists: {Directory.Exists(RawReportExportBaseDir)}");
            sb.AppendLine($"5. Final Excel Save Location Base: '{ExcelFinalSaveLocation}'");
            sb.AppendLine($"   - Exists: {Directory.Exists(ExcelFinalSaveLocation)}");
            sb.AppendLine($"6. Auto-Run Check Hour (from appsettings): {_configuration.GetValue<int>("settings:AutoRunCheckHour", _currentAutoRunHour)} (Current in-memory: {_currentAutoRunHour})");

            // Display configured auto-run report definitions and their enabled states.
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

            // Determine and display the actual user-specific log directory path.
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
            sb.AppendLine($"Overall Essential Config Valid (for report generation): {configValid}"); // Overall validity status.

            // Show the gathered configuration details in a message box.
            // The icon changes based on whether the config is considered valid.
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
            _uiManager.UpdateProgress("Validating configuration..."); // Initial status update.
            bool isValid = CheckConfigValidity(); // Perform the configuration check.
            string statusMessage = isValid ? "Configuration OK." : "Configuration Error: Essential paths missing or invalid. Check View Configuration.";

            if (isValid) Logger.LogInfo("Configuration validation successful.");
            else Logger.LogError("Configuration validation failed. Essential paths are missing or invalid.");

            _uiManager.UpdateStatusMain(statusMessage); // Display the validation result in the status bar.

            // If the configuration is valid, schedule the status message to revert to "Ready" after a short delay.
            // This provides temporary feedback without permanently overriding the "Ready" state.
            if (isValid)
            {
                _ = Task.Delay(7000).ContinueWith(t => // Asynchronous delay of 7 seconds.
                {
                    // Check if the status message hasn't been changed by another operation in the meantime.
                    if (_uiManager.GetCurrentStatusMain() == statusMessage)
                    {
                        _uiManager.UpdateStatusMain("Ready"); // Revert to "Ready".
                    }
                }, TaskScheduler.FromCurrentSynchronizationContext()); // Ensure the continuation runs on the UI thread.
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
                // Determine the actual user-specific log directory path.
                // This logic mirrors how the Logger determines its path.
                string baseLogDir = ConfiguredLogDirectoryBase;
                string actualUserLogDir = string.IsNullOrEmpty(baseLogDir)
                    ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "conversionTest", "Logs", Environment.UserName)
                    : Path.Combine(baseLogDir, string.Join("_", Environment.UserName.Split(Path.GetInvalidFileNameChars()))); // Sanitise username for folder name.

                actualUserLogDir = Path.GetFullPath(actualUserLogDir); // Normalise the path.

                // If the log directory doesn't exist, create it.
                if (!Directory.Exists(actualUserLogDir))
                {
                    Directory.CreateDirectory(actualUserLogDir);
                    Logger.LogInfo($"Created log directory as it did not exist: {actualUserLogDir}");
                }
                // Start the File Explorer process to open the log directory.
                Process.Start("explorer.exe", actualUserLogDir);
            }
            catch (Exception ex) // Handle any errors that occur while trying to open the folder.
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
                // Check if the appsettings.json file exists at the configured path.
                if (File.Exists(_appSettingsPath))
                {
                    // Use Process.Start with UseShellExecute = true to open the file
                    // with the default associated application (usually a text editor).
                    Process.Start(new ProcessStartInfo(_appSettingsPath) { UseShellExecute = true });
                }
                else
                {
                    // If the file doesn't exist, inform the user.
                    FlexibleMessageBox.Show(this, $"appsettings.json not found at the expected location:\n{_appSettingsPath}", "File Not Found", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex) // Handle any errors during the file opening process.
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
            Close(); // Standard WinForms method to close the current form, which, if it's the main form, exits the application.
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
                // Create and show the ManageBankHolidaysForm as a modal dialog.
                // Pass the current dark mode state to ensure the dialog is themed consistently.
                using (var manageForm = new ManageBankHolidaysForm(darkModeToolStripMenuItem.Checked))
                {
                    manageForm.ShowDialog(this); // ShowDialog makes it modal, blocking interaction with Form1 until closed.
                }
                Logger.LogInfo("ManageBankHolidaysForm closed."); // Log when the dialog is closed.
            }
            catch (Exception ex) // Handle any errors that occur while opening or interacting with the dialog.
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
                // Create and show the ManageEmailRecipientsForm as a modal dialog.
                // Pass the EmailRecipientManager instance (for loading/saving settings) and the current dark mode state.
                using (var manageEmailsForm = new ManageEmailRecipientsForm(_emailRecipientManager, darkModeToolStripMenuItem.Checked))
                {
                    manageEmailsForm.ShowDialog(this);
                }
                Logger.LogInfo("ManageEmailRecipientsForm closed.");
            }
            catch (Exception ex) // Handle errors.
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
                // Create and show the ManageGreetingsForm as a modal dialog.
                // Pass the GreetingManager instance and the current dark mode state.
                using (var manageGreetingsForm = new ManageGreetingsForm(_greetingManager, darkModeToolStripMenuItem.Checked))
                {
                    manageGreetingsForm.ShowDialog(this);
                }
                Logger.LogInfo("ManageGreetingsForm closed.");
            }
            catch (Exception ex) // Handle errors.
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
            // Update the UI to reflect the new 1-Click mode (shows/hides relevant buttons).
            Update1ClickProcessingModeUI();
            // Determine the appropriate text for the main action button after the mode change,
            // considering the current configuration validity.
            string mainButtonTextForReset = enable1ClickProcessingToolStripMenuItem.Checked ?
                                            (CheckConfigValidity() ? "Generate, Process & Email Report" : "Config Error") : // Text for 1-click mode.
                                            (CheckConfigValidity() ? "Create Report" : "Config Error");                      // Text for standard mode.
            ResetUIStateOnError(mainButtonTextForReset); // Reset the overall UI state to match the new mode.
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
            string currentHourPrompt = _currentAutoRunHour.ToString(); // Use the current hour as the default value in the input box.

            // Use Interaction.InputBox from Microsoft.VisualBasic for simple user input.
            // For a more polished UI or complex validation, a custom dialog form would be preferable.
            string? inputText = Interaction.InputBox(
                "Enter the new hour (0-23) for the daily auto-run check:", // Prompt message.
                "Set Auto-Run Hour",                                      // Dialog title.
                currentHourPrompt                                         // Default value.
            );

            // Process the input if the user provided a value (didn't cancel or leave it empty).
            if (!string.IsNullOrWhiteSpace(inputText))
            {
                // Try to parse the input as an integer and validate its range (0-23).
                if (int.TryParse(inputText, out int newHour) && newHour >= 0 && newHour <= 23)
                {
                    // Only proceed if the new hour is different from the current one.
                    if (newHour != _currentAutoRunHour)
                    {
                        // Attempt to save the new auto-run hour to the configuration file via AutoRunManager.
                        bool success = await _autoRunManager.SetAutoRunHourAsync(newHour);
                        if (success)
                        {
                            _currentAutoRunHour = newHour; // Update the local state variable.
                            Logger.LogInfo($"Auto-Run hour successfully updated to {newHour} in configuration and manager.");
                            FlexibleMessageBox.Show(this, $"Auto-Run hour has been set to {newHour}:00.\nThe change will take effect from the next daily check cycle.", "Auto-Run Hour Updated", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            _uiManager.SetAutoRunHour(_currentAutoRunHour); // Inform the UIManager of the change for UI updates.
                            // Refresh the AutoRun UI elements to reflect the new hour.
                            bool isAutoRunFinal = (autoRunStatusLabel.Text?.Contains("Done for") ?? false) || (autoRunStatusLabel.Text?.Contains("FAILED") ?? false);
                            _uiManager.UpdateAutoRunUI(dailyCheckTimer.Enabled, isAutoRunFinal, darkModeToolStripMenuItem.Checked, $"Auto Run: {(dailyCheckTimer.Enabled ? $"Enabled (Next check ~{_currentAutoRunHour}:00)" : "Disabled")}");
                        }
                        else // Failed to save the new hour to configuration.
                        {
                            Logger.LogError("Failed to save the new auto-run hour to configuration. Check AutoRunManager logs and file permissions for appsettings.json.");
                            FlexibleMessageBox.Show(this, "Failed to save the new auto-run hour. Please check logs and file permissions for appsettings.json.", "Error Saving Setting", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                    else // The entered hour is the same as the current setting.
                    {
                        FlexibleMessageBox.Show(this, "The new hour is the same as the current auto-run hour. No change made.", "No Change", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
                else // Invalid input (not an integer or out of range).
                {
                    FlexibleMessageBox.Show(this, "Invalid hour entered. Please enter a number between 0 and 23.", "Invalid Input", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            else // User cancelled the input dialog or provided no input.
            {
                Logger.LogInfo("Set Auto-Run Hour cancelled by user or no input provided.");
            }
        }

        #region Auto-Run Configuration Menu Item Handlers
        // Event handlers for menu items that toggle the enabled state of specific automated reports.
        // These methods update the corresponding boolean flags in the "AutoReport" section of appsettings.json.

        /// <summary>
        /// Handles the Click event for the `enableStandardDailyAutoReportToolStripMenuItem`.
        /// Toggles the enabled state for the standard daily automated report and saves the change to configuration.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">An <see cref="EventArgs"/> that contains no event data.</param>
        private async void enableStandardDailyAutoReportToolStripMenuItem_Click(object sender, EventArgs e)
        {
            bool newState = enableStandardDailyAutoReportToolStripMenuItem.Checked; // Get the new checked state from the menu item.
            // Update the setting in appsettings.json.
            await UpdateAutoReportToggleSettingAsync("EnableStandardDailyAutoReport", newState);
            Logger.LogInfo($"Standard Daily Auto-Report {(newState ? "Enabled" : "Disabled")} by user.");
            FlexibleMessageBox.Show(this, $"Standard Daily Auto-Report has been {(newState ? "ENABLED" : "DISABLED")}.", "Setting Updated", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        /// <summary>
        /// Handles the Click event for the `enableDaily5Day1kAutoReportToolStripMenuItem`.
        /// Toggles the enabled state for the "Daily (5days >= £1000)" automated report and saves to configuration.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">An <see cref="EventArgs"/> that contains no event data.</param>
        private async void enableDaily5Day1kAutoReportToolStripMenuItem_Click(object sender, EventArgs e)
        {
            bool newState = enableDaily5Day1kAutoReportToolStripMenuItem.Checked;
            await UpdateAutoReportToggleSettingAsync("EnableDaily5Day1kAutoReport", newState);
            Logger.LogInfo($"Daily (5days >= £1000) Auto-Report {(newState ? "Enabled" : "Disabled")} by user.");
            FlexibleMessageBox.Show(this, $"Daily (5days >= £1000) Auto-Report has been {(newState ? "ENABLED" : "DISABLED")}.", "Setting Updated", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        /// <summary>
        /// Handles the Click event for the `enableWeeklyAutoReportToolStripMenuItem`.
        /// Toggles the enabled state for the weekly automated report and saves to configuration.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">An <see cref="EventArgs"/> that contains no event data.</param>
        private async void enableWeeklyAutoReportToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // Defensive check to ensure the menu item is not null (should be initialised by the designer).
            if (enableWeeklyAutoReportToolStripMenuItem == null)
            {
                Logger.LogError("enableWeeklyAutoReportToolStripMenuItem_Click: The menu item for weekly auto-report is null. This indicates an issue with the form designer or initialisation. Ensure it's correctly added and named.");
                FlexibleMessageBox.Show(this, "UI element for weekly auto-report toggle not found. Please report this issue.", "UI Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            bool newState = enableWeeklyAutoReportToolStripMenuItem.Checked;
            await UpdateAutoReportToggleSettingAsync("EnableWeeklyAutoReport", newState);
            Logger.LogInfo($"Weekly Auto-Report {(newState ? "Enabled" : "Disabled")} by user.");
            FlexibleMessageBox.Show(this, $"Weekly Auto-Report has been {(newState ? "ENABLED" : "DISABLED")}.", "Setting Updated", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        #endregion // Auto-Run Configuration Menu Item Handlers

        #endregion // Menu Item Event Handlers

        #endregion // UI Event Handlers (consolidated)

        #region Helper Methods
        // This region contains various private helper methods used by the Form1 class
        // to encapsulate common logic, improve readability, and manage internal state.

        /// <summary>
        /// Loads the initial checked states for the auto-report toggle menu items
        /// (e.g., "Enable Standard Daily Auto Report") from the application configuration (`appsettings.json`).
        /// This ensures that the UI reflects the persisted settings when the application starts.
        /// Defaults to `true` (enabled) if a specific configuration key is not found.
        /// </summary>
        private void LoadAutoReportToggleStates()
        {
            // Read the boolean value for enabling the standard daily auto-report.
            // If "AutoReport:EnableStandardDailyAutoReport" is not found in config, default to true.
            enableStandardDailyAutoReportToolStripMenuItem.Checked = _configuration.GetValue<bool>("AutoReport:EnableStandardDailyAutoReport", true);

            // Read the boolean value for enabling the "Daily (5days >= £1000)" auto-report.
            enableDaily5Day1kAutoReportToolStripMenuItem.Checked = _configuration.GetValue<bool>("AutoReport:EnableDaily5Day1kAutoReport", true);

            // Read the boolean value for enabling the weekly auto-report.
            // Check if the menu item itself is not null (defensive programming).
            if (enableWeeklyAutoReportToolStripMenuItem != null)
            {
                enableWeeklyAutoReportToolStripMenuItem.Checked = _configuration.GetValue<bool>("AutoReport:EnableWeeklyAutoReport", true);
            }
            else
            {
                // Log a warning if the menu item for weekly reports is missing. This usually indicates a designer issue.
                Logger.LogWarning("LoadAutoReportToggleStates: enableWeeklyAutoReportToolStripMenuItem is null. UI toggle for weekly report will not be set. Check Form Designer.");
            }
            // Log the loaded states for debugging purposes.
            Logger.LogDebug($"Loaded Auto-Report Toggle States: StandardDaily={enableStandardDailyAutoReportToolStripMenuItem.Checked}, Daily5Day1k={enableDaily5Day1kAutoReportToolStripMenuItem.Checked}, Weekly={enableWeeklyAutoReportToolStripMenuItem?.Checked ?? false}");
        }

        /// <summary>
        /// Asynchronously updates a specific boolean toggle setting within the "AutoReport" section
        /// of the `appsettings.json` configuration file.
        /// This method reads the existing JSON, modifies the specified key, and writes the changes back.
        /// </summary>
        /// <param name="key">The specific configuration key name under "AutoReport" (e.g., "EnableStandardDailyAutoReport").</param>
        /// <param name="value">The new boolean value (true or false) to set for the key.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous file operation.</returns>
        private async Task UpdateAutoReportToggleSettingAsync(string key, bool value)
        {
            try
            {
                // Ensure the appsettings.json file exists before attempting to modify it.
                if (!File.Exists(_appSettingsPath))
                {
                    Logger.LogError($"appsettings.json not found at '{_appSettingsPath}'. Cannot update setting '{key}'.");
                    FlexibleMessageBox.Show(this, $"Configuration file not found. Cannot save setting.", "File Not Found", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // Read the entire content of appsettings.json.
                string jsonContent = await File.ReadAllTextAsync(_appSettingsPath);
                // Parse the JSON content into a JObject (from Newtonsoft.Json.Linq) for easy manipulation.
                JObject? json = JObject.Parse(jsonContent);

                // Navigate to or create the "AutoReport" section within the JSON structure.
                JObject? autoReportSection = json?["AutoReport"] as JObject;
                if (autoReportSection == null) // If the "AutoReport" section doesn't exist, create it.
                {
                    autoReportSection = new JObject();
                    if (json != null) json["AutoReport"] = autoReportSection; // Add it to the root JSON object.
                    else json = new JObject { ["AutoReport"] = autoReportSection }; // Should not happen if file is valid JSON.
                    Logger.LogWarning($"UpdateAutoReportToggleSettingAsync: 'AutoReport' section not found in appsettings.json. Creating it for key '{key}'.");
                }

                // Set the value of the specified key within the "AutoReport" section.
                if (autoReportSection != null) autoReportSection[key] = value;

                // Write the modified JObject back to the appsettings.json file, with indentation for readability.
                await File.WriteAllTextAsync(_appSettingsPath, json?.ToString(Newtonsoft.Json.Formatting.Indented) ?? "{}");
                Logger.LogInfo($"Successfully updated configuration key '{key}' to '{value}' in appsettings.json");
            }
            catch (Exception ex) // Handle potential errors during file I/O or JSON processing.
            {
                Logger.LogError($"Error updating configuration key '{key}' in appsettings.json: {ex.Message}", ex);
                FlexibleMessageBox.Show(this, $"Failed to save setting for '{key}'. Please check logs and file permissions for appsettings.json.", "Error Saving Setting", MessageBoxButtons.OK, MessageBoxIcon.Error);

                // If saving fails, revert the corresponding UI menu item's checked state to its previous value
                // to keep the UI consistent with the (unchanged) configuration.
                if (key == "EnableStandardDailyAutoReport") enableStandardDailyAutoReportToolStripMenuItem.Checked = !value;
                else if (key == "EnableDaily5Day1kAutoReport") enableDaily5Day1kAutoReportToolStripMenuItem.Checked = !value;
                else if (key == "EnableWeeklyAutoReport" && enableWeeklyAutoReportToolStripMenuItem != null) enableWeeklyAutoReportToolStripMenuItem.Checked = !value;
            }
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
            string currentText = selectedText ?? ""; // Use provided text or initialise to empty.

            // If selectedText was not provided or was empty, try to get text from the ComboBox's SelectedItem or Text property.
            if (string.IsNullOrEmpty(currentText) && reportTypeComboBox.SelectedItem != null)
            {
                currentText = reportTypeComboBox.SelectedItem.ToString() ?? "";
            }
            else if (string.IsNullOrEmpty(currentText) && !string.IsNullOrEmpty(reportTypeComboBox.Text))
            {
                currentText = reportTypeComboBox.Text; // Fallback to the Text property if SelectedItem is null.
            }

            // Use a switch expression to map the report type text to its corresponding integer index.
            return currentText switch
            {
                "Daily" => DailyReportIndex,
                "Daily (5days >= £1000)" => NewDailyReportOver1kIndex,
                "Weekly" => WeeklyReportIndex,
                "Monthly" => MonthlyReportIndex,
                "Quarterly (3 Months)" => QuarterlyReportIndex, // Ensure this string exactly matches the item in the ComboBox.
                "Annual" => AnnualReportIndex,
                "Custom" => CustomReportIndex,
                // If no text match is found, return the current SelectedIndex of the ComboBox as a fallback.
                // This handles cases where items might have been added dynamically or if text doesn't match predefined constants.
                _ => reportTypeComboBox.SelectedIndex
            };
        }

        /// <summary>
        /// Updates the UI to reflect the 1-Click processing mode (single button vs. two buttons).
        /// Shows/hides the appropriate action buttons.
        /// </summary>
        private void Update1ClickProcessingModeUI()
        {
            bool oneClickEnabled = enable1ClickProcessingToolStripMenuItem.Checked; // Get the current state of the 1-Click mode toggle.
            Logger.LogDebug($"Update1ClickProcessingModeUI called. 1-Click Mode Checked: {oneClickEnabled}");

            // Defensive check: ensure all relevant buttons have been initialised by the designer.
            if (oneClickProcessButton == null || createReportButton == null || processEmailButton == null)
            {
                Logger.LogError("One or more action buttons are NULL in Update1ClickProcessingModeUI. UI update for 1-Click mode skipped. This may indicate a problem with form initialisation.");
                return;
            }

            // Safely update button visibility on the UI thread.
            UIManager.SafeControlUpdate(oneClickProcessButton, () =>
            {
                oneClickProcessButton.Visible = oneClickEnabled; // Show 1-Click button if mode is enabled.
                                                                 // If it's made visible, bring it to the front to ensure it's not obscured by other buttons.
                if (oneClickEnabled && oneClickProcessButton.Visible) oneClickProcessButton.BringToFront();
            });
            // The standard "Create Report" and "Process & Email" buttons are visible only if 1-Click mode is *disabled*.
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
            // Perform UI updates safely on the UI thread.
            UIManager.SafeControlUpdate(financialYearComboBox, () =>
            {
                string? previouslySelected = financialYearComboBox.SelectedItem?.ToString(); // Store the currently selected item, if any.
                financialYearComboBox.Items.Clear(); // Clear all existing items from the ComboBox.

                // Get the current financial year string (e.g., "2023_24") using the Excel processor's helper.
                string currentFY = _excelProcessor.GetCurrentFinancialYear(useUnderscoreFormat: true);
                if (!string.IsNullOrEmpty(currentFY))
                {
                    financialYearComboBox.Items.Add(currentFY); // Add current FY to the list.
                                                                // Get the previous financial year string.
                    string? previousFY = _excelProcessor.GetPreviousFinancialYear(currentFY);
                    if (!string.IsNullOrEmpty(previousFY))
                    {
                        financialYearComboBox.Items.Add(previousFY); // Add previous FY if available.
                    }
                }
                else // Handle case where current financial year cannot be determined.
                {
                    Logger.LogWarning("Could not determine current financial year for dropdown population.");
                    financialYearComboBox.Items.Add("FY Unknown"); // Add a placeholder item.
                }

                // Attempt to restore the previously selected item if it's still in the list.
                if (!string.IsNullOrEmpty(previouslySelected) && financialYearComboBox.Items.Contains(previouslySelected))
                {
                    financialYearComboBox.SelectedItem = previouslySelected;
                }
                // Otherwise, if items exist, select the first one (usually the current FY).
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
                return false; // Validation failed.
            }
            return true; // Dates are valid.
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
            // Skip validation if the financial year ComboBox is not visible or no item is selected.
            if (!financialYearComboBox.Visible || financialYearComboBox.SelectedItem == null) return true;

            string selectedFinYear = financialYearComboBox.SelectedItem.ToString()!; // Get the selected financial year string.
                                                                                     // Use a helper from ExcelCopyData to check if the date range is valid for the selected FY.
            if (!_excelProcessor.IsFinancialYearValid(selectedFinYear, startDatePicker.Value, endDatePicker.Value))
            {
                // If the date range is outside the selected financial year, warn the user.
                DialogResult fdr = FlexibleMessageBox.Show(this,
                    $"The selected date range ({startDatePicker.Value:d} - {endDatePicker.Value:d}) " +
                    $"does not fall entirely within the selected Financial Year ({selectedFinYear}).\n\n" +
                    "Do you want to continue anyway?", // Ask if they want to proceed despite the mismatch.
                    "Financial Year Mismatch Warning", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                return fdr == DialogResult.Yes; // Return true if the user clicks "Yes", false for "No".
            }
            return true; // Date range is valid for the selected financial year.
        }

        /// <summary>
        /// Determines the base path for the `appsettings.json` configuration file.
        /// Currently, this path is hardcoded. For more flexibility, this could be made
        /// relative to the application's execution directory or configurable.
        /// </summary>
        /// <returns>The hardcoded base path string for `appsettings.json`.</returns>
        private static string DetermineAppSettingsBasePath() =>
            // IMPORTANT: This is a hardcoded network path.
            // Consider refactoring to use a path relative to the application executable
            // or a path from environment variables/command-line arguments for better deployment flexibility.
            @"\\harlow.local\DFS\IT Department\Applications\Development 2025\QuoteConversionReportAutomation\conversionTest";

        /// <summary>
        /// Checks the validity of essential configuration settings, specifically the paths
        /// to the Crystal Report file (.rpt) and the Crystal Report Wrapper executable (.exe).
        /// This is used to enable/disable UI elements and provide feedback to the user.
        /// </summary>
        /// <returns>True if both the Crystal Report file and the Wrapper EXE exist at their configured paths; otherwise, false.</returns>
        private bool CheckConfigValidity()
        {
            string crPath = CrystalReportLocation; // Get Crystal Report path from property.
            string wrapPathCfg = _configuration["settings:WrapperExePath"] ?? ""; // Get wrapper path from config.
            string wrapPathFull = string.IsNullOrEmpty(wrapPathCfg) ? "" : Path.GetFullPath(wrapPathCfg); // Resolve to full path.

            // Check if both paths are non-empty and the files at these paths actually exist.
            bool crystalReportFileExists = !string.IsNullOrEmpty(crPath) && File.Exists(crPath);
            bool wrapperExeFileExists = !string.IsNullOrEmpty(wrapPathFull) && File.Exists(wrapPathFull);

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
            // Safely get the selected text from the ComboBox on the UI thread.
            UIManager.SafeControlUpdate(reportTypeComboBox, () => selectedText = reportTypeComboBox.Text);
            // Return true if the selected text matches either of the daily report type strings.
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
            bool isOneClickMode = enable1ClickProcessingToolStripMenuItem.Checked; // Get current 1-Click mode state.
            bool configValid = CheckConfigValidity(); // Check current configuration validity.

            // Determine the actual text for the main action button. If config is invalid, it shows "Config Error".
            string actualMainButtonText = configValid ? mainButtonTextIfNoError : "Config Error";

            // Perform all UI updates on the UI thread to prevent cross-thread exceptions.
            UIManager.SafeControlUpdate(this, () =>
            {
                // Update state of main action buttons (Create, Process, 1-Click) based on current mode.
                if (isOneClickMode) // If 1-Click mode is active.
                {
                    if (oneClickProcessButton != null)
                    {
                        oneClickProcessButton.Text = actualMainButtonText;
                        oneClickProcessButton.Enabled = configValid; // Enable only if config is valid.
                    }
                    // Ensure standard buttons are disabled in 1-Click mode.
                    if (createReportButton != null) createReportButton.Enabled = false;
                    if (processEmailButton != null) processEmailButton.Enabled = false;
                }
                else // Standard 2-button mode.
                {
                    if (createReportButton != null)
                    {
                        createReportButton.Text = actualMainButtonText; // This is the primary button in this mode.
                        createReportButton.Enabled = configValid;
                    }
                    if (processEmailButton != null)
                    {
                        processEmailButton.Text = "Process && Email";
                        // Enable "Process & Email" only if config is valid AND a raw report has been generated.
                        processEmailButton.Enabled = configValid && !string.IsNullOrEmpty(_generatedReportPath) && File.Exists(_generatedReportPath);
                    }
                    // Ensure 1-Click button is disabled in standard mode.
                    if (oneClickProcessButton != null) oneClickProcessButton.Enabled = false;
                }

                // Always re-enable the auto-run toggle button.
                if (toggleAutoRunButton != null) toggleAutoRunButton.Enabled = true;

                // Call the UIManager's comprehensive reset method to handle other UI elements
                // (input controls, view buttons, status labels).
                _uiManager.ResetUIOnError(
                    mainButtonTextIfNoError, // Original intended button text.
                    configValid,             // Current configuration validity.
                    !string.IsNullOrEmpty(_generatedReportPath) && File.Exists(_generatedReportPath),         // Does a raw report exist?
                    !string.IsNullOrEmpty(_generatedAnalysisFilePath) && File.Exists(_generatedAnalysisFilePath), // Does an analysis file exist?
                    IsAnyDailySelected(),    // Is a daily report type currently selected?
                    dailyCheckTimer.Enabled, // Is the auto-run timer currently enabled?
                    darkModeToolStripMenuItem.Checked, // Is dark mode currently active?
                                                       // Has auto-run reached a final status for today (completed or failed)?
                    (autoRunStatusLabel.Text?.Contains("Completed") ?? false) ||
                        (autoRunStatusLabel.Text?.Contains("Done for") ?? false) ||
                        (autoRunStatusLabel.Text?.Contains("FAILED") ?? false),
                    autoRunStatusLabel.Text ?? "" // Current text of the auto-run status label.
                );

                // Logic to reset the main status label to "Ready" or "Config Error"
                // if it's currently showing a transient message (like "Processing...").
                string currentStatus = _uiManager.GetCurrentStatusMain();
                // Check if the status is not one of the persistent/final messages.
                if (!currentStatus.Equals("Ready", StringComparison.OrdinalIgnoreCase) &&
                    !currentStatus.StartsWith("Config Error", StringComparison.OrdinalIgnoreCase) &&
                    !currentStatus.StartsWith("Auto Run:", StringComparison.OrdinalIgnoreCase) &&
                    !currentStatus.Contains("Successfully") && !currentStatus.Contains("Completed"))
                {
                    // If transient, reset to default based on config validity.
                    _uiManager.UpdateStatusMain(configValid ? "Ready" : "Config Error: Check Options menu.");
                }
                else if (string.IsNullOrEmpty(currentStatus)) // If status is empty, set a default.
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
            // Determine if the "Send to Femi Only" option is active.
            // This option typically restricts recipients to a specific IT/admin list.
            bool isFemiOnly = sendToFemiOnlyCheckBox.Checked && sendToFemiOnlyCheckBox.Visible;
            // Get the index of the currently selected report type.
            int currentReportTypeIndex = GetSelectedReportTypeIndex();

            // Call the EmailRecipientManager to get the appropriate recipient lists.
            // `isAutoRunContext` is false here because this method is for manual runs.
            var recipients = _emailRecipientManager.GetRecipients(currentReportTypeIndex, isFemiOnly, IsDebug, isAutoRunContext: false);

            // Log the determined recipients for debugging purposes.
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
            string typeName = "Estimate Success Rate"; // A common part of the report name.
            string reportTypeString = "";
            // Safely get the text of the currently selected report type from the ComboBox.
            UIManager.SafeControlUpdate(reportTypeComboBox, () => reportTypeString = reportTypeComboBox.Text);

            int currentReportTypeIndex = GetSelectedReportTypeIndex(); // Get the index of the selected report type.
            bool femiOnlyChecked = sendToFemiOnlyCheckBox.Checked && sendToFemiOnlyCheckBox.Visible; // Check "Femi Only" state.

            string greeting;        // To store the retrieved greeting message.
            string greetingKeyName; // The key used to look up the greeting in configuration/overrides.

            // Determine the appropriate greetingKeyName based on the context.
            if (IsDebug) // If running in Debug mode, use the "DebugDefault" greeting.
            {
                greetingKeyName = "DebugDefault";
                greeting = _greetingManager.GetGreeting(greetingKeyName, isForDebugSection: true);
            }
            else // Release mode.
            {
                // Select greetingKeyName based on the report type and "Femi Only" option.
                if (currentReportTypeIndex == DailyReportIndex)
                {
                    greetingKeyName = "ManualStdDaily"; // Specific greeting for standard daily manual reports.
                }
                else if (currentReportTypeIndex == NewDailyReportOver1kIndex)
                {
                    greetingKeyName = femiOnlyChecked ? "ManualFemi" : "ManualTeam";
                }
                else if (currentReportTypeIndex == CustomReportIndex) // New: Use "ManualCustom" greeting key for Custom reports.
                {
                    greetingKeyName = "ManualCustom";
                }
                else if (currentReportTypeIndex == WeeklyReportIndex ||
                         currentReportTypeIndex == MonthlyReportIndex ||
                         currentReportTypeIndex == QuarterlyReportIndex ||
                         currentReportTypeIndex == AnnualReportIndex)
                {
                    // For these non-standard daily or custom reports, choose between "Femi" and "Team" greetings.
                    greetingKeyName = femiOnlyChecked ? "ManualFemi" : "ManualTeam";
                }
                else // Fallback for any other or unexpected report type.
                {
                    greetingKeyName = "ManualTeam"; // Default to the general team greeting.
                    Logger.LogWarning($"Manual run for unexpected report type '{reportTypeString}' (Index: {currentReportTypeIndex}). Using fallback greeting key '{greetingKeyName}'.");
                }
                greeting = _greetingManager.GetGreeting(greetingKeyName); // Retrieve the greeting.
            }

            // Ensure the greeting, if not empty, ends with a comma for proper sentence flow.
            if (!string.IsNullOrWhiteSpace(greeting) && !greeting.TrimEnd().EndsWith(","))
            {
                greeting = greeting.TrimEnd() + ",";
            }

            // Construct information about the report's date range for inclusion in the email.
            string rangeInfo;
            string subjectPrefix = $"{reportTypeString} {typeName}"; // Base subject prefix (e.g., "Daily Estimate Success Rate").

            switch (currentReportTypeIndex)
            {
                case DailyReportIndex: rangeInfo = $"for {reportEndDate:dd MMM yy}"; break;
                case NewDailyReportOver1kIndex: rangeInfo = $"for period {reportStartDate:dd MMM yy} to {reportEndDate:dd MMM yy}"; break;
                case WeeklyReportIndex: rangeInfo = $"for period {reportStartDate:dd MMM yy} to {reportEndDate:dd MMM yy}"; break;
                case MonthlyReportIndex: rangeInfo = $"for {reportStartDate:MMMM yy}"; break; // e.g., "for May 2023"
                case QuarterlyReportIndex: rangeInfo = $"for {ReportHelper.GetQuarterString(reportStartDate)} {reportStartDate.Year}"; break; // e.g., "for Q2 2023"
                case AnnualReportIndex:
                    rangeInfo = $"for Financial Year {reportStartDate.Year}-{reportEndDate.Year}";
                    subjectPrefix = $"Annual {typeName}"; // More specific subject for annual reports.
                    break;
                case CustomReportIndex: rangeInfo = $"for period {reportStartDate:dd MMM yy} to {reportEndDate:dd MMM yy}"; break;
                default: // Fallback for unknown types.
                    rangeInfo = $"for period {reportStartDate:dd MMM yy} to {reportEndDate:dd MMM yy}";
                    subjectPrefix = $"Report {typeName}"; // Generic prefix.
                    break;
            }

            // Construct a date suffix for the subject line for quick identification.
            string subjectDateSuffix = (reportStartDate.Date == reportEndDate.Date) ?
                                       $"({reportEndDate:yyyy-MM-dd})" : // Single date.
                                       $"({reportStartDate:yyyy-MM-dd} to {reportEndDate:yyyy-MM-dd})"; // Date range.
            if (currentReportTypeIndex == AnnualReportIndex) subjectDateSuffix = $"({reportStartDate.Year}-{reportEndDate.Year})"; // FY format.

            // Add "MANUAL:" prefix to the subject for manually run reports (excluding "Custom" which is inherently manual).
            // For "Custom" reports, the manualPrefix will be empty, so "MANUAL:" won't be prepended.
            string manualPrefix = (currentReportTypeIndex != CustomReportIndex && currentReportTypeIndex != -1) ? "MANUAL: " : "";
            string subject = $"{manualPrefix}{subjectPrefix} Report {subjectDateSuffix}";
            if (IsDebug) subject = $"DEBUG - {subject}"; // Prepend "DEBUG -" if in debug mode.

            // Construct the email body using the greeting, report type, and date range information.
            string body = $"{greeting}\n\nPlease find attached the {subjectPrefix.ToLower()} report {rangeInfo}.\n\nThis report includes quotes data for review.\n\nThank you,\nAutomation Service";

            Logger.LogDebug($"GetEmailSubjectAndBody: GreetingKey='{greetingKeyName}', Subject='{subject}'");
            return (subject, body);
        }
        #endregion
    }
}
