// SettingsForm.cs
// Provides a UI for viewing and modifying application settings stored in appsettings.json.
// Organizes settings into tabs for better user experience.
// Handles loading settings from IConfiguration and saving changes back to the appsettings.json file.
// Includes input validation, theming, and tooltips for all configurable settings.
// C# 10+ Features.

#region Using Directives
// System related namespaces
using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using System.Collections.Generic; // For lists if handling array settings
using System.Linq; // For LINQ operations like FirstOrDefault
using System.Globalization; // For CultureInfo if needed for parsing/formatting specific values

// Third-party namespaces
using Microsoft.Extensions.Configuration; // For IConfiguration
using Newtonsoft.Json;                    // For JSON formatting when saving
using Newtonsoft.Json.Linq;               // For JObject manipulation

// Project specific namespaces
using QuoteConversionReportAutomation.Services.Logging; // For Logger and Logger.LogLevel enum
using QuoteConversionReportAutomation.Helpers;          // For FlexibleMessageBox and EmailUtility (for IsValidEmail)
using QuoteConversionReportAutomation.Managers;         // For UIManager (for theming)
#endregion

namespace QuoteConversionReportAutomation.Forms
{
    /// <summary>
    /// A Windows Form that allows users to view and modify application-wide default settings
    /// which are stored in the `appsettings.json` file.
    /// Settings are organized into categories using a TabControl for improved usability.
    /// The form handles loading current settings from configuration, validating user input,
    /// and persisting changes back to the `appsettings.json` file.
    /// It is recommended that for some settings changes, the application might need a restart to fully apply them.
    /// </summary>
    public partial class SettingsForm : Form
    {
        #region Fields
        /// <summary>
        /// Provides access to the application's current configuration settings, typically loaded from `appsettings.json`.
        /// Used to populate the initial values in the settings form. This instance is read-only within this form;
        /// changes are made by modifying the `appsettings.json` file directly and then reloading the configuration in the main application.
        /// </summary>
        private readonly IConfiguration _configuration;

        /// <summary>
        /// The full file path to the `appsettings.json` file that will be read from and written to by this form.
        /// </summary>
        private readonly string _appSettingsPath;

        /// <summary>
        /// Flag indicating the initial dark mode state of the application, typically passed from the parent form
        /// to ensure consistent theming of this settings dialog.
        /// </summary>
        private readonly bool _initialIsDarkMode;

        /// <summary>
        /// A static lock object to ensure thread-safe read/write operations on the `appsettings.json` file,
        /// preventing potential race conditions if multiple parts of the application were to attempt modification
        /// (though typically only this settings form would directly modify it).
        /// </summary>
        private static readonly object s_appSettingsFileLock = new object();

        /// <summary>
        /// ErrorProvider component used to display validation error icons and messages next to input controls,
        /// providing visual feedback to the user about input errors.
        /// </summary>
        private ErrorProvider errorProvider;
        #endregion

        #region Constructor
        /// <summary>
        /// Initializes a new instance of the <see cref="SettingsForm"/> class.
        /// </summary>
        /// <param name="configuration">The application's current <see cref="IConfiguration"/> instance. This is used to populate the initial values of the settings fields displayed in the form.</param>
        /// <param name="appSettingsPath">The full file path to the `appsettings.json` file. Changes made in this form will be saved to this file.</param>
        /// <param name="isDarkMode">A flag indicating if the parent form (and thus this dialog) should be rendered in dark mode for consistent theming with the main application.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="configuration"/> or <paramref name="appSettingsPath"/> is null, as these are essential for the form's operation.</exception>
        public SettingsForm(IConfiguration configuration, string appSettingsPath, bool isDarkMode = false)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _appSettingsPath = appSettingsPath ?? throw new ArgumentNullException(nameof(appSettingsPath));
            _initialIsDarkMode = isDarkMode;

            InitializeComponent();      // Standard WinForms designer initialization (creates controls from SettingsForm.Designer.cs).
            InitializeCustomComponents(); // Additional setup for components like ErrorProvider and ComboBox data sources.

            // Configure basic form properties.
            this.Text = "Application Settings"; // Set the window title.
            this.FormBorderStyle = FormBorderStyle.FixedDialog; // Prevent resizing for a consistent layout.
            this.StartPosition = FormStartPosition.CenterParent; // Center the dialog relative to its calling form.
            this.ShowInTaskbar = false; // Settings dialogs typically don't appear in the Windows taskbar.
            this.Load += SettingsForm_Load; // Wire up the Load event handler.
        }
        #endregion

        #region Custom Component Initialization
        /// <summary>
        /// Initializes components that require setup beyond what the Windows Forms designer provides,
        /// such as the ErrorProvider for validation messages and populating ComboBoxes with enum values.
        /// This method is called from the constructor after `InitializeComponent()`.
        /// </summary>
        private void InitializeCustomComponents()
        {
            // Initialize the ErrorProvider component used for displaying validation errors next to controls.
            this.errorProvider = new System.Windows.Forms.ErrorProvider
            {
                BlinkStyle = System.Windows.Forms.ErrorBlinkStyle.NeverBlink // No blinking for error icons, less intrusive.
            };

            // Populate LogLevel ComboBoxes (cmbDefaultLogLevel, cmbDebugBuildLogLevel) with values from Logger.LogLevel enum.
            var logLevels = Enum.GetValues(typeof(Logger.LogLevel))
                                .Cast<Logger.LogLevel>()
                                .ToList();

            cmbDefaultLogLevel.DataSource = new List<Logger.LogLevel>(logLevels);
            cmbDebugBuildLogLevel.DataSource = new List<Logger.LogLevel>(logLevels);

            Logger.LogDebug("SettingsForm: Custom components initialized (ErrorProvider created, LogLevel ComboBoxes populated).");
        }
        #endregion

        #region Form Load and Theming
        /// <summary>
        /// Handles the Load event of the form. This method is called once when the form is first displayed.
        /// It's responsible for applying the visual theme (dark or light mode) based on the parent form's state
        /// and loading all current settings from the application configuration into the UI controls.
        /// </summary>
        /// <param name="sender">The source of the event (the form itself).</param>
        /// <param name="e">An <see cref="EventArgs"/> that contains no event data.</param>
        private void SettingsForm_Load(object? sender, EventArgs e)
        {
            Logger.LogInfo("SettingsForm is loading UI and settings...");
            try
            {
                ApplyTheme(_initialIsDarkMode);
                LoadSettingsIntoUI();
                SetupToolTips();
            }
            catch (Exception ex)
            {
                Logger.LogError($"A critical error occurred during SettingsForm_Load: {ex.Message}", ex);
                FlexibleMessageBox.Show(this, $"Could not load application settings into the form: {ex.Message}\nPlease check the application logs for more details.", "Settings Load Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                this.BeginInvoke(new Action(this.Close));
            }
            Logger.LogInfo("SettingsForm loaded successfully. UI populated with current settings and tooltips have been configured.");
        }

        /// <summary>
        /// Applies the visual theme (dark or light mode) to the SettingsForm itself and its child controls.
        /// </summary>
        /// <param name="isDarkModeEnabled">True to apply dark mode styling, false for light mode styling.</param>
        private void ApplyTheme(bool isDarkModeEnabled)
        {
            UIManager.ApplyThemeToExternalForm(this, isDarkModeEnabled);

            Color controlBackColor = isDarkModeEnabled ? Color.FromArgb(60, 60, 63) : SystemColors.Window;
            Color buttonBackColor = isDarkModeEnabled ? Color.FromArgb(80, 80, 80) : SystemColors.Control;
            Color controlForeColor = isDarkModeEnabled ? Color.WhiteSmoke : SystemColors.ControlText;
            Color tabPageBackColor = isDarkModeEnabled ? Color.FromArgb(50, 50, 53) : SystemColors.ControlLight;
            Color tabControlBackColor = isDarkModeEnabled ? Color.FromArgb(45, 45, 48) : SystemColors.Control;

            mainTabControl.BackColor = tabControlBackColor;

            foreach (TabPage tabPage in mainTabControl.TabPages)
            {
                tabPage.BackColor = tabPageBackColor;
                tabPage.ForeColor = controlForeColor;
                ApplyThemeToChildControlsRecursive(tabPage, tabPageBackColor, controlForeColor, controlBackColor, buttonBackColor, isDarkModeEnabled);
            }

            panelButtons.BackColor = this.BackColor;
            ApplyThemeToChildControlsRecursive(panelButtons, this.BackColor, controlForeColor, controlBackColor, buttonBackColor, isDarkModeEnabled);

            Logger.LogDebug($"SettingsForm theme applied: {(isDarkModeEnabled ? "Dark Mode" : "Light Mode")}");
        }

        /// <summary>
        /// Recursively applies theme colors to a control and all its child controls.
        /// </summary>
        private void ApplyThemeToChildControlsRecursive(Control parentControl, Color parentActualBackColor, Color generalForeColor, Color inputBackColor, Color buttonBackColor, bool isDarkMode)
        {
            foreach (Control control in parentControl.Controls)
            {
                if (control.IsDisposed) continue;

                control.ForeColor = generalForeColor;

                if (control is Button button)
                {
                    button.BackColor = buttonBackColor;
                    button.FlatStyle = FlatStyle.Flat;
                    button.FlatAppearance.BorderColor = isDarkMode ? Color.FromArgb(100, 100, 100) : SystemColors.ControlDarkDark;
                    button.FlatAppearance.BorderSize = 1;
                }
                else if (control is TextBox || control is NumericUpDown || control is ComboBox)
                {
                    control.BackColor = inputBackColor;
                    if (control is TextBox tb) tb.BorderStyle = isDarkMode ? BorderStyle.FixedSingle : BorderStyle.Fixed3D;
                    if (control is ComboBox cb) cb.FlatStyle = FlatStyle.Flat;
                }
                else if (control is Label || control is GroupBox || control is CheckBox)
                {
                    control.BackColor = Color.Transparent;
                    if (control is GroupBox gb) gb.ForeColor = generalForeColor;
                    if (control is CheckBox chk) chk.ForeColor = generalForeColor;
                }
                else if (control is TableLayoutPanel specificTlp)
                {
                    specificTlp.BackColor = parentActualBackColor;
                    if (specificTlp.HasChildren)
                    {
                        ApplyThemeToChildControlsRecursive(specificTlp, specificTlp.BackColor, generalForeColor, inputBackColor, buttonBackColor, isDarkMode);
                    }
                }
                else if (control is Panel panel)
                {
                    panel.BackColor = parentActualBackColor;
                    if (panel.HasChildren)
                    {
                        ApplyThemeToChildControlsRecursive(panel, panel.BackColor, generalForeColor, inputBackColor, buttonBackColor, isDarkMode);
                    }
                }
                else if (control.HasChildren)
                {
                    ApplyThemeToChildControlsRecursive(control, control.BackColor, generalForeColor, inputBackColor, buttonBackColor, isDarkMode);
                }
            }
        }
        #endregion

        #region Load Settings into UI
        /// <summary>
        /// Loads current configuration values into UI controls.
        /// Handles path resolution for user-profile relative paths.
        /// </summary>
        private void LoadSettingsIntoUI()
        {
            Logger.LogDebug("SettingsForm: Starting to load all application settings into UI controls...");
            string userProfilePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

            // --- Application Info Tab ---
            txtAppName.Text = _configuration.GetValue<string>("ApplicationInfo:AppName", "Quote Conversion Report Automation");
            txtAppVersion.Text = _configuration.GetValue<string>("ApplicationInfo:AppVersion", "1.9.x");

            // --- Paths Tab ---
            txtCrystalReportRptFile.Text = _configuration["Paths:CrystalReportRptFile"]; // Typically absolute or UNC

            string finalReportOutputBaseConfig = _configuration["Paths:FinalReportOutputBase"];
            if (!string.IsNullOrEmpty(finalReportOutputBaseConfig) &&
                !Path.IsPathRooted(finalReportOutputBaseConfig) &&
                !finalReportOutputBaseConfig.StartsWith("\\\\") &&
                !finalReportOutputBaseConfig.Contains("%"))
            {
                txtFinalReportOutputBase.Text = Path.Combine(userProfilePath, finalReportOutputBaseConfig);
            }
            else
            {
                txtFinalReportOutputBase.Text = finalReportOutputBaseConfig;
            }

            string templateBaseConfig = _configuration["Paths:TemplateBase"];
            if (!string.IsNullOrEmpty(templateBaseConfig) &&
                !Path.IsPathRooted(templateBaseConfig) &&
                !templateBaseConfig.StartsWith("\\\\") &&
                !templateBaseConfig.Contains("%"))
            {
                txtTemplateBase.Text = Path.Combine(userProfilePath, templateBaseConfig);
            }
            else
            {
                txtTemplateBase.Text = templateBaseConfig;
            }

            txtLogDirectoryBase.Text = _configuration["Paths:LogDirectoryBase"]; // Can be UNC, absolute, or contain env vars

            string rawReportOutputBaseConfig = _configuration["Paths:RawReportOutputBase"];
            if (!string.IsNullOrEmpty(rawReportOutputBaseConfig) &&
                !Path.IsPathRooted(rawReportOutputBaseConfig) &&
                !rawReportOutputBaseConfig.StartsWith("\\\\") &&
                !rawReportOutputBaseConfig.Contains("%"))
            {
                txtRawReportOutputBase.Text = Path.Combine(userProfilePath, rawReportOutputBaseConfig);
            }
            else
            {
                txtRawReportOutputBase.Text = rawReportOutputBaseConfig;
            }

            txtWrapperExecutable.Text = _configuration["Paths:WrapperExecutable"]; // Typically absolute or UNC
            txtReportDefinitionsFileName.Text = _configuration.GetValue<string>("Paths:ReportDefinitionsFileName", "autoReportDefinitions.json");
            txtFallbackLogDirectory.Text = _configuration.GetValue<string>("Logging:DefaultFallbackLogDirectory", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "QCRA_Logs_Fallback", "Logs"));


            // --- SMTP Configuration Tab ---
            txtSmtpServer.Text = _configuration["SmtpConfiguration:Server"];
            numSmtpPort.Value = ClampValue(numSmtpPort, _configuration.GetValue<int>("SmtpConfiguration:Port", 25));
            txtSmtpUsername.Text = _configuration["SmtpConfiguration:Username"];
            string? actualSmtpPassword = _configuration["SmtpConfiguration:Password"];
            txtSmtpPassword.Text = !string.IsNullOrEmpty(actualSmtpPassword) ? "********" : string.Empty;
            txtSmtpPassword.Tag = actualSmtpPassword;
            chkSmtpEnableSsl.Checked = _configuration.GetValue<bool>("SmtpConfiguration:EnableSsl", false);
            numSmtpMaxSendRetries.Value = ClampValue(numSmtpMaxSendRetries, _configuration.GetValue<int>("SmtpConfiguration:MaxSendRetries", 3));
            numSmtpSendRetryDelayMs.Value = ClampValue(numSmtpSendRetryDelayMs, _configuration.GetValue<int>("SmtpConfiguration:SendRetryDelayMs", 2000));
            numSmtpTimeoutMs.Value = ClampValue(numSmtpTimeoutMs, _configuration.GetValue<int>("SmtpConfiguration:TimeoutMs", 30000));

            // --- Email Defaults Tab ---
            txtSenderAddress.Text = _configuration["EmailSettings:SenderAddress"];
            txtSenderDisplayName.Text = _configuration["EmailSettings:SenderDisplayName"];
            numMaxAttachmentSizeBytes.Value = ClampValue(numMaxAttachmentSizeBytes, _configuration.GetValue<decimal>("EmailSettings:MaxAttachmentSizeBytes", 10485760M));
            txtDefaultEmailSignature.Text = _configuration.GetValue<string>("EmailSettings:DefaultEmailSignature", "Thank you,\nQCRA Service");
            numAttachmentReadMaxRetries.Value = ClampValue(numAttachmentReadMaxRetries, _configuration.GetValue<int>("EmailSettings:AttachmentReadMaxRetries", 3));
            numAttachmentReadDelayMs.Value = ClampValue(numAttachmentReadDelayMs, _configuration.GetValue<int>("EmailSettings:AttachmentReadDelayMs", 500));

            // --- Logging Tab ---
            cmbDefaultLogLevel.SelectedItem = _configuration.GetValue<Logger.LogLevel>("Logging:DefaultLogLevel", Logger.LogLevel.Info);
            cmbDebugBuildLogLevel.SelectedItem = _configuration.GetValue<Logger.LogLevel>("Logging:DebugBuildLogLevel", Logger.LogLevel.Trace);
            numLogArchiveOlderThanDays.Value = ClampValue(numLogArchiveOlderThanDays, _configuration.GetValue<int>("Logging:LogArchiveOlderThanDays", 7));
            txtLogFileNameFormat.Text = _configuration.GetValue<string>("Logging:LogFileNameFormat", "{0:yyyy-MM-dd}_QCRA_Log.log");

            // --- Operational Parameters Tab ---
            numArchiveRawReportsOlderThanDays.Value = ClampValue(numArchiveRawReportsOlderThanDays, _configuration.GetValue<int>("OperationalParameters:ArchiveRawReportsOlderThanDays", 30));
            txtReportArchiveFolderName.Text = _configuration.GetValue<string>("OperationalParameters:ReportArchiveFolderName", "Archive");
            numProcessTimeoutMinutes.Value = ClampValue(numProcessTimeoutMinutes, _configuration.GetValue<int>("OperationalParameters:ProcessTimeoutMinutes", 15));
            numFinancialYearStartMonth.Value = ClampValue(numFinancialYearStartMonth, _configuration.GetValue<int>("OperationalParameters:FinancialYearStartMonth", 5));
            numFinancialYearStartDay.Value = ClampValue(numFinancialYearStartDay, _configuration.GetValue<int>("OperationalParameters:FinancialYearStartDay", 1));
            numDaily5Day1kFilteringThreshold.Value = ClampValue(numDaily5Day1kFilteringThreshold, _configuration.GetValue<decimal>("OperationalParameters:Daily5Day1kFilteringThreshold", 1000M));
            numGeneralFileOpMaxRetries.Value = ClampValue(numGeneralFileOpMaxRetries, _configuration.GetValue<int>("OperationalParameters:GeneralFileOperationMaxRetries", 5));
            numGeneralFileOpDelayMs.Value = ClampValue(numGeneralFileOpDelayMs, _configuration.GetValue<int>("OperationalParameters:GeneralFileOperationDelayMs", 500));
            txtRawDataSourceSheet.Text = _configuration.GetValue<string>("OperationalParameters:ExcelSheetNames:RawDataSourceSheet", "Sheet1");
            txtTemplateDataCopySheet.Text = _configuration.GetValue<string>("OperationalParameters:ExcelSheetNames:TemplateDataCopySheet", "DATA");
            txtTemplateAnalysisSheet.Text = _configuration.GetValue<string>("OperationalParameters:ExcelSheetNames:TemplateAnalysisSheet", "Analysis");
            txtPowerBiDataSheet.Text = _configuration.GetValue<string>("OperationalParameters:ExcelSheetNames:PowerBiDataSheet", "powerBI");
            txtMonthlyOrderPivotSheet.Text = _configuration.GetValue<string>("OperationalParameters:ExcelSheetNames:MonthlyOrderPivotSheet", "OrderPivot");
            txtMonthlyEstimatePivotSheet.Text = _configuration.GetValue<string>("OperationalParameters:ExcelSheetNames:MonthlyEstimatePivotSheet", "Estimate Success PivotTable");
            txtMonthlyOrderPivotName.Text = _configuration.GetValue<string>("OperationalParameters:PivotTableNames:MonthlyOrderPivot", "PivotTable1");
            txtMonthlyEstimatePivotName.Text = _configuration.GetValue<string>("OperationalParameters:PivotTableNames:MonthlyEstimatePivot", "PivotTable3");
            txtFolderNamingDaily.Text = _configuration.GetValue<string>("OperationalParameters:ReportTypeFolderNames:Daily", "Daily Reports");
            txtFolderNamingDaily5Day1k.Text = _configuration.GetValue<string>("OperationalParameters:ReportTypeFolderNames:Daily5Day1k", "Daily Reports (5day 1k)");
            txtFolderNamingWeekly.Text = _configuration.GetValue<string>("OperationalParameters:ReportTypeFolderNames:Weekly", "Weekly Reports");
            txtFolderNamingMonthly.Text = _configuration.GetValue<string>("OperationalParameters:ReportTypeFolderNames:Monthly", "Monthly Reports");
            txtFolderNamingQuarterly.Text = _configuration.GetValue<string>("OperationalParameters:ReportTypeFolderNames:Quarterly", "Quarterly Reports");
            txtFolderNamingAnnual.Text = _configuration.GetValue<string>("OperationalParameters:ReportTypeFolderNames:Annual", "Annual Reports");
            txtFolderNamingCustom.Text = _configuration.GetValue<string>("OperationalParameters:ReportTypeFolderNames:Custom", "Custom Reports");
            txtFolderNamingOther.Text = _configuration.GetValue<string>("OperationalParameters:ReportTypeFolderNames:Other", "Other Reports");

            // --- Inter-Process Communication Tab ---
            txtNamedPipeName.Text = _configuration.GetValue<string>("InterProcessCommunication:NamedPipeName", "CrystalReportPipe");
            numPipeConnectTimeoutMs.Value = ClampValue(numPipeConnectTimeoutMs, _configuration.GetValue<int>("InterProcessCommunication:PipeConnectTimeoutMs", 5000));
            numMaxPipeResponseSizeBytes.Value = ClampValue(numMaxPipeResponseSizeBytes, _configuration.GetValue<decimal>("InterProcessCommunication:MaxPipeResponseSizeBytes", 10485760M));

            // --- AutoRun Process Tab ---
            numAutoRunCheckHour.Value = ClampValue(numAutoRunCheckHour, _configuration.GetValue<int>("AutoRunProcess:CheckHour", 8));

            Logger.LogInfo("SettingsForm: Finished loading all settings from configuration into UI controls.");
        }

        /// <summary>
        /// Ensures a value intended for a NumericUpDown control is within its defined Minimum and Maximum range.
        /// </summary>
        private T ClampValue<T>(NumericUpDown nud, T value) where T : IComparable<T>
        {
            decimal decValue = Convert.ToDecimal(value);
            if (decValue < nud.Minimum) return (T)Convert.ChangeType(nud.Minimum, typeof(T));
            if (decValue > nud.Maximum) return (T)Convert.ChangeType(nud.Maximum, typeof(T));
            return value;
        }

        /// <summary>
        /// Sets up descriptive tooltips for all UI input controls on the form.
        /// </summary>
        private void SetupToolTips()
        {
            // --- Application Info Tab ---
            toolTip1.SetToolTip(txtAppName, "The name of the application.");
            toolTip1.SetToolTip(txtAppVersion, "The current version of the application (e.g., 1.9.3).");

            // --- Paths Tab ---
            toolTip1.SetToolTip(txtCrystalReportRptFile, "Full path to the main Crystal Report definition file (.rpt).\nExample: C:\\Reports\\MyReport.rpt or \\\\server\\share\\report.rpt");
            toolTip1.SetToolTip(btnBrowseCrystalReport, "Browse to select the Crystal Report file (.rpt).");
            toolTip1.SetToolTip(txtFinalReportOutputBase, "Base directory for final processed Excel reports.\nCan be an absolute path (C:\\...), a UNC path (\\\\server\\share\\...), or a path relative to your user profile (e.g., MyCompany\\Reports\\Final).\nPaths under user profile will be stored relatively.");
            toolTip1.SetToolTip(btnBrowseFinalReportOutputBase, "Browse for the base directory for final report output.");
            toolTip1.SetToolTip(txtTemplateBase, "Base directory for Excel template files (.xlsx).\nHandles absolute, UNC, and user-profile relative paths like 'Final Report Output Base'.");
            toolTip1.SetToolTip(btnBrowseTemplateBase, "Browse for the base directory where Excel templates are located.");
            toolTip1.SetToolTip(txtLogDirectoryBase, "Base directory for application log files.\nCan be a local path, a network share (e.g., \\\\server\\share\\logs\\QCRA), or use environment variables (e.g., %LOCALAPPDATA%\\QCRA\\Logs).\nIf left blank, a default AppData location is used.");
            toolTip1.SetToolTip(btnBrowseLogDirectoryBase, "Browse for the base directory for application log files.");
            toolTip1.SetToolTip(txtRawReportOutputBase, "Base directory for raw exported data files from Crystal Reports.\nHandles absolute, UNC, and user-profile relative paths like 'Final Report Output Base'.");
            toolTip1.SetToolTip(btnBrowseRawReportOutputBase, "Browse for the base directory for raw report exports.");
            toolTip1.SetToolTip(txtWrapperExecutable, "Full path to the Crystal Report Wrapper executable (typically CrystalReportWrapper.exe).");
            toolTip1.SetToolTip(btnBrowseWrapperExecutable, "Browse to select the Crystal Report Wrapper executable file (.exe).");
            toolTip1.SetToolTip(txtReportDefinitionsFileName, "Filename for the JSON file storing automated report definitions (e.g., autoReportDefinitions.json).\nExpected in the same directory as appsettings.json.");
            toolTip1.SetToolTip(txtFallbackLogDirectory, "Directory for logging if 'Log Directory Base' is inaccessible.\nSupports environment variables like %LOCALAPPDATA%.");
            toolTip1.SetToolTip(btnBrowseFallbackLogDir, "Browse for the default fallback log directory.");

            // --- SMTP Configuration Tab ---
            toolTip1.SetToolTip(txtSmtpServer, "Hostname or IP address of your SMTP (email) server.");
            toolTip1.SetToolTip(numSmtpPort, "Port number for the SMTP server (e.g., 25, 587, 465).");
            toolTip1.SetToolTip(txtSmtpUsername, "Username for SMTP authentication (if required).");
            toolTip1.SetToolTip(txtSmtpPassword, "Password for SMTP authentication. Stored in appsettings.json.");
            toolTip1.SetToolTip(chkSmtpEnableSsl, "Check if your SMTP server requires SSL/TLS encryption.");
            toolTip1.SetToolTip(numSmtpMaxSendRetries, "Max retries for sending email on temporary failure.");
            toolTip1.SetToolTip(numSmtpSendRetryDelayMs, "Initial delay (ms) before retrying email send.");
            toolTip1.SetToolTip(numSmtpTimeoutMs, "Timeout (ms) for SMTP operations.");

            // --- Email Defaults Tab ---
            toolTip1.SetToolTip(txtSenderAddress, "Default 'From' email address for report emails.");
            toolTip1.SetToolTip(txtSenderDisplayName, "Display name for the sender email address.");
            toolTip1.SetToolTip(numMaxAttachmentSizeBytes, "Max allowed size for email attachments in bytes (e.g., 10485760 for 10MB).");
            toolTip1.SetToolTip(txtDefaultEmailSignature, "Default signature text for report emails. Supports multiple lines.");
            toolTip1.SetToolTip(numAttachmentReadMaxRetries, "Max retries for reading attachment files if locked.");
            toolTip1.SetToolTip(numAttachmentReadDelayMs, "Delay (ms) between attachment read retries.");

            // --- Logging Tab ---
            toolTip1.SetToolTip(cmbDefaultLogLevel, "Minimum log level for Release builds (Trace, Debug, Info, Warning, Error, Critical, None).");
            toolTip1.SetToolTip(cmbDebugBuildLogLevel, "Minimum log level for Debug builds.");
            toolTip1.SetToolTip(numLogArchiveOlderThanDays, "Log files older than this many days will be archived.");
            toolTip1.SetToolTip(txtLogFileNameFormat, "Format for daily log filenames (e.g., {0:yyyy-MM-dd}_QCRA_Log.log). Must include a date placeholder like {0:yyyy-MM-dd}.");

            // --- Operational Parameters Tab ---
            toolTip1.SetToolTip(numArchiveRawReportsOlderThanDays, "Raw report export files older than this many days will be archived.");
            toolTip1.SetToolTip(txtReportArchiveFolderName, "Subfolder name for archived reports (e.g., 'Archive').");
            toolTip1.SetToolTip(numProcessTimeoutMinutes, "General timeout (minutes) for long-running processes.");
            toolTip1.SetToolTip(numFinancialYearStartMonth, "Month (1-12) when the financial year starts.");
            toolTip1.SetToolTip(numFinancialYearStartDay, "Day (1-31) of the month when the financial year starts.");
            toolTip1.SetToolTip(numDaily5Day1kFilteringThreshold, "Monetary threshold for 'Daily (5days >= £X)' report filtering.");
            toolTip1.SetToolTip(numGeneralFileOpMaxRetries, "Max retries for general file system operations (move, rename).");
            toolTip1.SetToolTip(numGeneralFileOpDelayMs, "Initial delay (ms) between file operation retries.");
            toolTip1.SetToolTip(txtRawDataSourceSheet, "Name of the sheet in raw Crystal Report Excel export with primary data.");
            toolTip1.SetToolTip(txtTemplateDataCopySheet, "Name of the sheet in Excel template where raw data is copied.");
            toolTip1.SetToolTip(txtTemplateAnalysisSheet, "Name of the primary analysis sheet in Excel template.");
            toolTip1.SetToolTip(txtPowerBiDataSheet, "Name of the sheet in Power BI source Excel file for weekly data append.");
            toolTip1.SetToolTip(txtMonthlyOrderPivotSheet, "Name of the sheet with 'Order' pivot table in monthly/quarterly/annual templates.");
            toolTip1.SetToolTip(txtMonthlyEstimatePivotSheet, "Name of the sheet with 'Estimate Success' pivot table in monthly/quarterly/annual templates.");
            toolTip1.SetToolTip(txtMonthlyOrderPivotName, "Actual name of the 'Order' pivot table.");
            toolTip1.SetToolTip(txtMonthlyEstimatePivotName, "Actual name of the 'Estimate Success' pivot table.");
            toolTip1.SetToolTip(txtFolderNamingDaily, "Folder name for 'Daily' report types.");
            toolTip1.SetToolTip(txtFolderNamingDaily5Day1k, "Folder name for 'Daily (5days >= £1k)' reports.");
            toolTip1.SetToolTip(txtFolderNamingWeekly, "Folder name for 'Weekly' reports.");
            toolTip1.SetToolTip(txtFolderNamingMonthly, "Folder name for 'Monthly' reports.");
            toolTip1.SetToolTip(txtFolderNamingQuarterly, "Folder name for 'Quarterly' reports.");
            toolTip1.SetToolTip(txtFolderNamingAnnual, "Folder name for 'Annual' reports.");
            toolTip1.SetToolTip(txtFolderNamingCustom, "Folder name for 'Custom' date range reports.");
            toolTip1.SetToolTip(txtFolderNamingOther, "Default folder name for other report types.");

            // --- Inter-Process Communication (IPC) Tab ---
            toolTip1.SetToolTip(txtNamedPipeName, "Unique name of the named pipe for Crystal Report Wrapper communication.");
            toolTip1.SetToolTip(numPipeConnectTimeoutMs, "Timeout (ms) for connecting to the named pipe server.");
            toolTip1.SetToolTip(numMaxPipeResponseSizeBytes, "Maximum expected size (bytes) for response messages from the pipe server.");

            // --- AutoRun Process Tab ---
            toolTip1.SetToolTip(numAutoRunCheckHour, "Hour of the day (0-23) for automated report check/processing.");

            // --- Buttons ---
            toolTip1.SetToolTip(btnSaveChanges, "Validate and save settings to appsettings.json. Restart may be needed for some changes.");
            toolTip1.SetToolTip(btnCancel, "Close without saving changes.");

            Logger.LogDebug("SettingsForm: Tooltips have been set up for all relevant UI controls.");
        }
        #endregion

        #region Save Settings
        /// <summary>
        /// Handles the Click event for the "Save Changes" button.
        /// Validates inputs, reads values from UI controls, updates the `appsettings.json` file.
        /// </summary>
        private void btnSaveChanges_Click(object? sender, EventArgs e)
        {
            Logger.LogInfo("Save Changes button clicked on SettingsForm. Validating all inputs...");
            if (!ValidateAllInputs())
            {
                Logger.LogWarning("Settings validation failed. Save operation aborted.");
                FlexibleMessageBox.Show(this, "Please correct the highlighted errors on all tabs before saving.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            Logger.LogInfo("All inputs validated successfully. Proceeding to save changes.");
            try
            {
                string currentJson;
                lock (s_appSettingsFileLock)
                {
                    if (!File.Exists(_appSettingsPath))
                    {
                        Logger.LogError($"CRITICAL: Cannot save settings. appsettings.json not found at '{_appSettingsPath}'.");
                        FlexibleMessageBox.Show(this, $"Critical error: The application settings file ({Path.GetFileName(_appSettingsPath)}) was not found.\nCannot save settings.", "Settings File Not Found", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                    currentJson = File.ReadAllText(_appSettingsPath);
                }

                JObject rootObject = JObject.Parse(string.IsNullOrWhiteSpace(currentJson) ? "{}" : currentJson);
                string userProfilePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

                // --- Update Application Info Tab Settings ---
                UpdateJsonValue(rootObject, "ApplicationInfo:AppName", txtAppName.Text.Trim());
                UpdateJsonValue(rootObject, "ApplicationInfo:AppVersion", txtAppVersion.Text.Trim());

                // --- Update Paths Tab Settings ---
                UpdateJsonValue(rootObject, "Paths:CrystalReportRptFile", txtCrystalReportRptFile.Text.Trim());

                string finalReportOutputBaseToSave = txtFinalReportOutputBase.Text.Trim();
                if (Path.IsPathRooted(finalReportOutputBaseToSave) &&
                    !finalReportOutputBaseToSave.StartsWith("\\\\") &&
                    !finalReportOutputBaseToSave.Contains("%") &&
                    finalReportOutputBaseToSave.StartsWith(userProfilePath, StringComparison.OrdinalIgnoreCase))
                {
                    finalReportOutputBaseToSave = finalReportOutputBaseToSave.Substring(userProfilePath.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                }
                UpdateJsonValue(rootObject, "Paths:FinalReportOutputBase", finalReportOutputBaseToSave);

                string templateBaseToSave = txtTemplateBase.Text.Trim();
                if (Path.IsPathRooted(templateBaseToSave) &&
                    !templateBaseToSave.StartsWith("\\\\") &&
                    !templateBaseToSave.Contains("%") &&
                    templateBaseToSave.StartsWith(userProfilePath, StringComparison.OrdinalIgnoreCase))
                {
                    templateBaseToSave = templateBaseToSave.Substring(userProfilePath.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                }
                UpdateJsonValue(rootObject, "Paths:TemplateBase", templateBaseToSave);

                UpdateJsonValue(rootObject, "Paths:LogDirectoryBase", txtLogDirectoryBase.Text.Trim());

                string rawReportOutputBaseToSave = txtRawReportOutputBase.Text.Trim();
                if (Path.IsPathRooted(rawReportOutputBaseToSave) &&
                    !rawReportOutputBaseToSave.StartsWith("\\\\") &&
                    !rawReportOutputBaseToSave.Contains("%") &&
                    rawReportOutputBaseToSave.StartsWith(userProfilePath, StringComparison.OrdinalIgnoreCase))
                {
                    rawReportOutputBaseToSave = rawReportOutputBaseToSave.Substring(userProfilePath.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                }
                UpdateJsonValue(rootObject, "Paths:RawReportOutputBase", rawReportOutputBaseToSave);

                UpdateJsonValue(rootObject, "Paths:WrapperExecutable", txtWrapperExecutable.Text.Trim());
                UpdateJsonValue(rootObject, "Paths:ReportDefinitionsFileName", txtReportDefinitionsFileName.Text.Trim());
                UpdateJsonValue(rootObject, "Logging:DefaultFallbackLogDirectory", txtFallbackLogDirectory.Text.Trim());


                // --- Update SMTP Configuration Tab Settings ---
                UpdateJsonValue(rootObject, "SmtpConfiguration:Server", txtSmtpServer.Text.Trim());
                UpdateJsonValue(rootObject, "SmtpConfiguration:Port", (int)numSmtpPort.Value);
                UpdateJsonValue(rootObject, "SmtpConfiguration:Username", txtSmtpUsername.Text.Trim());
                string originalSmtpPassword = (string?)txtSmtpPassword.Tag ?? string.Empty;
                if (txtSmtpPassword.Text != "********" || string.IsNullOrEmpty(originalSmtpPassword))
                {
                    if (txtSmtpPassword.Text != originalSmtpPassword || (string.IsNullOrEmpty(originalSmtpPassword) && !string.IsNullOrEmpty(txtSmtpPassword.Text)))
                    {
                        UpdateJsonValue(rootObject, "SmtpConfiguration:Password", txtSmtpPassword.Text);
                        Logger.LogWarning("SMTP Password changed and saved.");
                    }
                }
                UpdateJsonValue(rootObject, "SmtpConfiguration:EnableSsl", chkSmtpEnableSsl.Checked);
                UpdateJsonValue(rootObject, "SmtpConfiguration:MaxSendRetries", (int)numSmtpMaxSendRetries.Value);
                UpdateJsonValue(rootObject, "SmtpConfiguration:SendRetryDelayMs", (int)numSmtpSendRetryDelayMs.Value);
                UpdateJsonValue(rootObject, "SmtpConfiguration:TimeoutMs", (int)numSmtpTimeoutMs.Value);

                // --- Update Email Defaults Tab ---
                UpdateJsonValue(rootObject, "EmailSettings:SenderAddress", txtSenderAddress.Text.Trim());
                UpdateJsonValue(rootObject, "EmailSettings:SenderDisplayName", txtSenderDisplayName.Text.Trim());
                UpdateJsonValue(rootObject, "EmailSettings:MaxAttachmentSizeBytes", (long)numMaxAttachmentSizeBytes.Value);
                UpdateJsonValue(rootObject, "EmailSettings:DefaultEmailSignature", txtDefaultEmailSignature.Text); // Text already includes newlines
                UpdateJsonValue(rootObject, "EmailSettings:AttachmentReadMaxRetries", (int)numAttachmentReadMaxRetries.Value);
                UpdateJsonValue(rootObject, "EmailSettings:AttachmentReadDelayMs", (int)numAttachmentReadDelayMs.Value);

                // --- Update Logging Tab ---
                UpdateJsonValue(rootObject, "Logging:DefaultLogLevel", cmbDefaultLogLevel.SelectedItem?.ToString() ?? Logger.LogLevel.Info.ToString());
                UpdateJsonValue(rootObject, "Logging:DebugBuildLogLevel", cmbDebugBuildLogLevel.SelectedItem?.ToString() ?? Logger.LogLevel.Trace.ToString());
                UpdateJsonValue(rootObject, "Logging:LogArchiveOlderThanDays", (int)numLogArchiveOlderThanDays.Value);
                UpdateJsonValue(rootObject, "Logging:LogFileNameFormat", txtLogFileNameFormat.Text.Trim());
                // DefaultFallbackLogDirectory is saved under Paths section earlier

                // --- Update Operational Parameters Tab ---
                UpdateJsonValue(rootObject, "OperationalParameters:ArchiveRawReportsOlderThanDays", (int)numArchiveRawReportsOlderThanDays.Value);
                UpdateJsonValue(rootObject, "OperationalParameters:ReportArchiveFolderName", txtReportArchiveFolderName.Text.Trim());
                UpdateJsonValue(rootObject, "OperationalParameters:ProcessTimeoutMinutes", (int)numProcessTimeoutMinutes.Value);
                UpdateJsonValue(rootObject, "OperationalParameters:FinancialYearStartMonth", (int)numFinancialYearStartMonth.Value);
                UpdateJsonValue(rootObject, "OperationalParameters:FinancialYearStartDay", (int)numFinancialYearStartDay.Value);
                UpdateJsonValue(rootObject, "OperationalParameters:Daily5Day1kFilteringThreshold", numDaily5Day1kFilteringThreshold.Value);
                UpdateJsonValue(rootObject, "OperationalParameters:GeneralFileOperationMaxRetries", (int)numGeneralFileOpMaxRetries.Value);
                UpdateJsonValue(rootObject, "OperationalParameters:GeneralFileOperationDelayMs", (int)numGeneralFileOpDelayMs.Value);
                UpdateJsonValue(rootObject, "OperationalParameters:ExcelSheetNames:RawDataSourceSheet", txtRawDataSourceSheet.Text.Trim());
                UpdateJsonValue(rootObject, "OperationalParameters:ExcelSheetNames:TemplateDataCopySheet", txtTemplateDataCopySheet.Text.Trim());
                UpdateJsonValue(rootObject, "OperationalParameters:ExcelSheetNames:TemplateAnalysisSheet", txtTemplateAnalysisSheet.Text.Trim());
                UpdateJsonValue(rootObject, "OperationalParameters:ExcelSheetNames:PowerBiDataSheet", txtPowerBiDataSheet.Text.Trim());
                UpdateJsonValue(rootObject, "OperationalParameters:ExcelSheetNames:MonthlyOrderPivotSheet", txtMonthlyOrderPivotSheet.Text.Trim());
                UpdateJsonValue(rootObject, "OperationalParameters:ExcelSheetNames:MonthlyEstimatePivotSheet", txtMonthlyEstimatePivotSheet.Text.Trim());
                UpdateJsonValue(rootObject, "OperationalParameters:PivotTableNames:MonthlyOrderPivot", txtMonthlyOrderPivotName.Text.Trim());
                UpdateJsonValue(rootObject, "OperationalParameters:PivotTableNames:MonthlyEstimatePivot", txtMonthlyEstimatePivotName.Text.Trim());
                UpdateJsonValue(rootObject, "OperationalParameters:ReportTypeFolderNames:Daily", txtFolderNamingDaily.Text.Trim());
                UpdateJsonValue(rootObject, "OperationalParameters:ReportTypeFolderNames:Daily5Day1k", txtFolderNamingDaily5Day1k.Text.Trim());
                UpdateJsonValue(rootObject, "OperationalParameters:ReportTypeFolderNames:Weekly", txtFolderNamingWeekly.Text.Trim());
                UpdateJsonValue(rootObject, "OperationalParameters:ReportTypeFolderNames:Monthly", txtFolderNamingMonthly.Text.Trim());
                UpdateJsonValue(rootObject, "OperationalParameters:ReportTypeFolderNames:Quarterly", txtFolderNamingQuarterly.Text.Trim());
                UpdateJsonValue(rootObject, "OperationalParameters:ReportTypeFolderNames:Annual", txtFolderNamingAnnual.Text.Trim());
                UpdateJsonValue(rootObject, "OperationalParameters:ReportTypeFolderNames:Custom", txtFolderNamingCustom.Text.Trim());
                UpdateJsonValue(rootObject, "OperationalParameters:ReportTypeFolderNames:Other", txtFolderNamingOther.Text.Trim());

                // --- Update Inter-Process Communication (IPC) Tab ---
                UpdateJsonValue(rootObject, "InterProcessCommunication:NamedPipeName", txtNamedPipeName.Text.Trim());
                UpdateJsonValue(rootObject, "InterProcessCommunication:PipeConnectTimeoutMs", (int)numPipeConnectTimeoutMs.Value);
                UpdateJsonValue(rootObject, "InterProcessCommunication:MaxPipeResponseSizeBytes", (long)numMaxPipeResponseSizeBytes.Value);

                // --- Update AutoRun Process Tab ---
                UpdateJsonValue(rootObject, "AutoRunProcess:CheckHour", (int)numAutoRunCheckHour.Value);

                string updatedJson = JsonConvert.SerializeObject(rootObject, Formatting.Indented);
                lock (s_appSettingsFileLock)
                {
                    File.WriteAllText(_appSettingsPath, updatedJson);
                }

                Logger.LogInfo("Application settings successfully saved to appsettings.json.");
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                Logger.LogError($"An unexpected error occurred while saving settings: {ex.Message}", ex);
                FlexibleMessageBox.Show(this, $"An unexpected error occurred while saving settings:\n{ex.Message}\n\nYour changes were NOT saved.", "Save Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Helper method to update a value within a JObject using a colon-separated configuration path.
        /// </summary>
        private void UpdateJsonValue(JObject root, string fullConfigPath, object value)
        {
            if (root == null) throw new ArgumentNullException(nameof(root));
            if (string.IsNullOrWhiteSpace(fullConfigPath)) throw new ArgumentException("Configuration path cannot be null or empty.", nameof(fullConfigPath));

            string[] segments = fullConfigPath.Split(':');
            JToken? currentToken = root;

            for (int i = 0; i < segments.Length - 1; i++)
            {
                string segment = segments[i];
                if (currentToken is JObject currentObject)
                {
                    if (!currentObject.TryGetValue(segment, out JToken? nextToken) || !(nextToken is JObject))
                    {
                        var newSection = new JObject();
                        currentObject[segment] = newSection;
                        currentToken = newSection;
                    }
                    else
                    {
                        currentToken = nextToken;
                    }
                }
                else
                {
                    throw new InvalidOperationException($"Path segment '{segment}' in '{fullConfigPath}' implies an object, but parent is not an object.");
                }
            }

            if (currentToken is JObject finalParentObject)
            {
                string finalKey = segments.Last();
                finalParentObject[finalKey] = JToken.FromObject(value);
            }
            else
            {
                throw new InvalidOperationException($"The target path '{fullConfigPath}' did not resolve to a JObject for key '{segments.Last()}'.");
            }
        }

        /// <summary>
        /// Handles the Click event for the "Cancel" button.
        /// </summary>
        private void btnCancel_Click(object? sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
        #endregion

        #region Validation
        /// <summary>
        /// Validates all user inputs across all tabs on the form.
        /// </summary>
        /// <returns>True if all inputs are valid; otherwise, false.</returns>
        private bool ValidateAllInputs()
        {
            errorProvider.Clear();
            bool isValid = true;

            // --- Application Info Tab Validation ---
            isValid &= ValidateRequiredTextBox(txtAppName, "Application Name");
            isValid &= ValidateRequiredTextBox(txtAppVersion, "Application Version");
            if (!string.IsNullOrWhiteSpace(txtAppVersion.Text) && !System.Text.RegularExpressions.Regex.IsMatch(txtAppVersion.Text, @"^\d+(\.\d+){1,3}(\.\w+)?$")) // Allow for suffixes like .beta
            {
                SetError(txtAppVersion, "Version format should be like X.Y or X.Y.Z (e.g., 1.9.3 or 1.9.3.beta).");
                isValid = false;
            }

            // --- Paths Tab Validation ---
            isValid &= ValidatePathControl(txtCrystalReportRptFile, "Crystal Report .RPT File Path", isFile: true, required: true, fileMustExist: true, expectedExtension: ".rpt");
            isValid &= ValidatePathControl(txtFinalReportOutputBase, "Final Report Output Base Directory", isFile: false, required: true, fileMustExist: false); // Existence checked based on resolved path if relative
            isValid &= ValidatePathControl(txtTemplateBase, "Template Base Directory", isFile: false, required: true, fileMustExist: true);
            isValid &= ValidatePathControl(txtLogDirectoryBase, "Log Directory Base", isFile: false, required: false, fileMustExist: false); // Not strictly required to exist beforehand
            isValid &= ValidatePathControl(txtRawReportOutputBase, "Raw Report Output Base Directory", isFile: false, required: true, fileMustExist: false);
            isValid &= ValidatePathControl(txtWrapperExecutable, "Wrapper Executable Path", isFile: true, required: true, fileMustExist: true, expectedExtension: ".exe");
            isValid &= ValidateRequiredTextBox(txtReportDefinitionsFileName, "Report Definitions Filename");
            isValid &= ValidatePathControl(txtFallbackLogDirectory, "Fallback Log Directory", isFile: false, required: false, fileMustExist: false); // Not strictly required to exist

            // --- SMTP Configuration Validation ---
            isValid &= ValidateRequiredTextBox(txtSmtpServer, "SMTP Server");
            isValid &= ValidateNumericUpDown(numSmtpPort, "SMTP Port", 1, 65535);
            // Username and Password are not strictly required by all SMTP servers
            // isValid &= ValidateRequiredTextBox(txtSmtpUsername, "SMTP Username"); 
            isValid &= ValidateNumericUpDown(numSmtpTimeoutMs, "SMTP Timeout (ms)", 1000, 300000);
            isValid &= ValidateNumericUpDown(numSmtpMaxSendRetries, "SMTP Max Retries", 0, 10);
            isValid &= ValidateNumericUpDown(numSmtpSendRetryDelayMs, "SMTP Retry Delay (ms)", 100, 60000);

            // --- Email Defaults Validation ---
            isValid &= ValidateRequiredTextBox(txtSenderAddress, "Sender Email Address");
            if (!string.IsNullOrWhiteSpace(txtSenderAddress.Text) && !EmailUtility.IsValidEmail(txtSenderAddress.Text)) { SetError(txtSenderAddress, "Invalid email address format for Sender Address."); isValid = false; }
            isValid &= ValidateRequiredTextBox(txtSenderDisplayName, "Sender Display Name");
            isValid &= ValidateNumericUpDown(numMaxAttachmentSizeBytes, "Max Attachment Size (Bytes)", 0, 52428800);
            isValid &= ValidateNumericUpDown(numAttachmentReadMaxRetries, "Attachment Read Retries", 0, 10);
            isValid &= ValidateNumericUpDown(numAttachmentReadDelayMs, "Attachment Read Delay (ms)", 100, 10000);

            // --- Logging Validation ---
            isValid &= ValidateNumericUpDown(numLogArchiveOlderThanDays, "Archive Logs Older Than (Days)", 1, 3650);
            isValid &= ValidateRequiredTextBox(txtLogFileNameFormat, "Log Filename Format");
            if (!string.IsNullOrWhiteSpace(txtLogFileNameFormat.Text) && (!txtLogFileNameFormat.Text.Contains("{0") || !txtLogFileNameFormat.Text.EndsWith(".log", StringComparison.OrdinalIgnoreCase)))
            { SetError(txtLogFileNameFormat, "Log Filename Format must include a date placeholder like {0:yyyy-MM-dd} and end with .log."); isValid = false; }

            // --- Operational Parameters Validation ---
            isValid &= ValidateNumericUpDown(numArchiveRawReportsOlderThanDays, "Archive Raw Reports Older Than (Days)", 1, 3650);
            isValid &= ValidateFolderNameChars(txtReportArchiveFolderName, "Report Archive Folder Name"); // Just char validation, not existence
            isValid &= ValidateNumericUpDown(numProcessTimeoutMinutes, "Process Timeout (Minutes)", 1, 120);
            isValid &= ValidateNumericUpDown(numFinancialYearStartMonth, "Financial Year Start Month", 1, 12);
            isValid &= ValidateNumericUpDown(numFinancialYearStartDay, "Financial Year Start Day", 1, 31); // Further validation for days in month could be added if needed
            isValid &= ValidateNumericUpDown(numDaily5Day1kFilteringThreshold, "Daily >= £1k Filtering Threshold", 0, 1000000000);
            isValid &= ValidateNumericUpDown(numGeneralFileOpMaxRetries, "General File Op Max Retries", 0, 20);
            isValid &= ValidateNumericUpDown(numGeneralFileOpDelayMs, "General File Op Delay (ms)", 100, 10000);
            isValid &= ValidateRequiredTextBox(txtRawDataSourceSheet, "Raw Data Source Sheet Name");
            isValid &= ValidateRequiredTextBox(txtTemplateDataCopySheet, "Template Data Copy Sheet Name");
            isValid &= ValidateRequiredTextBox(txtTemplateAnalysisSheet, "Template Analysis Sheet Name");
            isValid &= ValidateRequiredTextBox(txtPowerBiDataSheet, "Power BI Data Sheet Name");
            isValid &= ValidateRequiredTextBox(txtMonthlyOrderPivotSheet, "Monthly Order Pivot Sheet Name");
            isValid &= ValidateRequiredTextBox(txtMonthlyEstimatePivotSheet, "Monthly Estimate Pivot Sheet Name");
            isValid &= ValidateRequiredTextBox(txtMonthlyOrderPivotName, "Monthly Order Pivot Name");
            isValid &= ValidateRequiredTextBox(txtMonthlyEstimatePivotName, "Monthly Estimate Pivot Name");
            isValid &= ValidateFolderNameChars(txtFolderNamingDaily, "Folder Name: Daily");
            isValid &= ValidateFolderNameChars(txtFolderNamingDaily5Day1k, "Folder Name: Daily 5Day1k");
            isValid &= ValidateFolderNameChars(txtFolderNamingWeekly, "Folder Name: Weekly");
            isValid &= ValidateFolderNameChars(txtFolderNamingMonthly, "Folder Name: Monthly");
            isValid &= ValidateFolderNameChars(txtFolderNamingQuarterly, "Folder Name: Quarterly");
            isValid &= ValidateFolderNameChars(txtFolderNamingAnnual, "Folder Name: Annual");
            isValid &= ValidateFolderNameChars(txtFolderNamingCustom, "Folder Name: Custom");
            isValid &= ValidateFolderNameChars(txtFolderNamingOther, "Folder Name: Other");

            // --- Inter-Process Communication (IPC) Validation ---
            isValid &= ValidateRequiredTextBox(txtNamedPipeName, "Named Pipe Name");
            // Basic check for invalid chars, specific pipe name validation might be more complex
            if (!string.IsNullOrWhiteSpace(txtNamedPipeName.Text) && txtNamedPipeName.Text.IndexOfAny(new[] { '\\', '/', ':', '*', '?', '"', '<', '>', '|' }) >= 0)
            { SetError(txtNamedPipeName, "Named Pipe Name contains invalid characters."); isValid = false; }
            isValid &= ValidateNumericUpDown(numPipeConnectTimeoutMs, "Pipe Connect Timeout (ms)", 500, 60000);
            isValid &= ValidateNumericUpDown(numMaxPipeResponseSizeBytes, "Max Pipe Response Size (Bytes)", 1024, 52428800);

            // --- AutoRun Process Validation ---
            isValid &= ValidateNumericUpDown(numAutoRunCheckHour, "AutoRun Check Hour", 0, 23);

            if (!isValid)
            {
                Logger.LogWarning("SettingsForm: Validation failed for one or more input fields.");
            }
            return isValid;
        }

        /// <summary>
        /// Validates a TextBox to ensure it's not empty.
        /// </summary>
        private bool ValidateRequiredTextBox(TextBox textBox, string fieldName, int minLength = 1)
        {
            if (string.IsNullOrWhiteSpace(textBox.Text) || textBox.Text.Trim().Length < minLength)
            {
                SetError(textBox, $"{fieldName} is required.");
                return false;
            }
            return true;
        }

        /// <summary>
        /// Validates a TextBox for valid folder name characters (if not empty).
        /// </summary>
        private bool ValidateFolderNameChars(TextBox textBox, string fieldName)
        {
            if (!string.IsNullOrWhiteSpace(textBox.Text) && textBox.Text.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            {
                SetError(textBox, $"{fieldName} contains invalid characters for a folder name.");
                return false;
            }
            return true;
        }


        /// <summary>
        /// Validates a NumericUpDown control's value against min/max.
        /// </summary>
        private bool ValidateNumericUpDown(NumericUpDown numericUpDown, string fieldName, decimal min, decimal max)
        {
            if (numericUpDown.Value < min || numericUpDown.Value > max)
            {
                SetError(numericUpDown, $"{fieldName} must be between {min} and {max}.");
                return false;
            }
            return true;
        }

        /// <summary>
        /// Validates a path entered into a TextBox control.
        /// Handles absolute, UNC, user-profile relative, and environment variable paths.
        /// </summary>
        private bool ValidatePathControl(TextBox pathTextBox, string fieldName, bool isFile, bool required, bool fileMustExist = false, string? expectedExtension = null)
        {
            string pathText = pathTextBox.Text.Trim();
            errorProvider.SetError(pathTextBox, "");

            if (required && string.IsNullOrWhiteSpace(pathText))
            {
                SetError(pathTextBox, $"{fieldName} is required.");
                return false;
            }

            if (!string.IsNullOrWhiteSpace(pathText))
            {
                string pathForExistenceCheck = pathText;
                bool containsEnvVar = pathText.Contains("%");

                // Preliminary character validation on the raw input
                if (!containsEnvVar && pathText.IndexOfAny(Path.GetInvalidPathChars()) >= 0)
                {
                    SetError(pathTextBox, $"{fieldName} contains invalid path characters: '{pathText}'");
                    return false;
                }
                if (isFile && !containsEnvVar && Path.GetFileName(pathText).IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                {
                    SetError(pathTextBox, $"{fieldName} (filename part) contains invalid characters: '{Path.GetFileName(pathText)}'");
                    return false;
                }


                if (containsEnvVar)
                {
                    try
                    {
                        pathForExistenceCheck = Environment.ExpandEnvironmentVariables(pathText);
                        // Post-expansion validation
                        if (pathForExistenceCheck.IndexOfAny(Path.GetInvalidPathChars()) >= 0)
                        {
                            SetError(pathTextBox, $"{fieldName} (expanded: '{pathForExistenceCheck}') contains invalid path characters.");
                            return false;
                        }
                        if (isFile && Path.GetFileName(pathForExistenceCheck).IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                        {
                            SetError(pathTextBox, $"{fieldName} (filename part, expanded: '{Path.GetFileName(pathForExistenceCheck)}') contains invalid characters.");
                            return false;
                        }
                    }
                    catch (ArgumentException ex)
                    {
                        SetError(pathTextBox, $"{fieldName}: Error expanding variables in '{pathText}': {ex.Message}");
                        return false;
                    }
                }
                else if (!Path.IsPathRooted(pathText) && !pathText.StartsWith("\\\\"))
                {
                    // This applies if it's potentially user-profile relative (e.g. "MyTemplates")
                    // For validation, combine with user profile path if fileMustExist is true.
                    // This specific handling is for txtFinalReportOutputBase, txtTemplateBase, txtRawReportOutputBase
                    if (pathTextBox == txtFinalReportOutputBase || pathTextBox == txtTemplateBase || pathTextBox == txtRawReportOutputBase)
                    {
                        string userProfilePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                        pathForExistenceCheck = Path.Combine(userProfilePath, pathText);
                    }
                    // Otherwise, if it's relative and not one of these special cases, pathForExistenceCheck remains pathText
                    // and Directory.Exists/File.Exists will resolve it against current working directory,
                    // which might be okay for some settings but usually not for these UI-configured base paths.
                    // However, for this form, most "relative" paths are intended to be user-profile relative or env-var based.
                }
                // If pathText is already absolute (local C:\ or UNC \\server\), pathForExistenceCheck is pathText.

                if (fileMustExist)
                {
                    try
                    {
                        // Ensure pathForExistenceCheck is not null or empty after potential modifications
                        if (string.IsNullOrWhiteSpace(pathForExistenceCheck))
                        {
                            SetError(pathTextBox, $"{fieldName}: Path becomes empty after processing input '{pathText}'.");
                            return false;
                        }

                        if (isFile)
                        {
                            if (!File.Exists(pathForExistenceCheck))
                            {
                                SetError(pathTextBox, $"{fieldName}: File not found at '{pathForExistenceCheck}'. (Input: '{pathText}')");
                                return false;
                            }
                        }
                        else // Is Directory
                        {
                            if (!Directory.Exists(pathForExistenceCheck))
                            {
                                SetError(pathTextBox, $"{fieldName}: Directory not found at '{pathForExistenceCheck}'. (Input: '{pathText}')");
                                return false;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        SetError(pathTextBox, $"{fieldName}: Error accessing path '{pathForExistenceCheck}': {ex.Message}. (Input: '{pathText}')");
                        return false;
                    }
                }

                if (isFile && !string.IsNullOrEmpty(expectedExtension))
                {
                    string extensionToCheck = Path.GetExtension(pathText); // Check extension on original input
                    if (!extensionToCheck.Equals(expectedExtension, StringComparison.OrdinalIgnoreCase))
                    {
                        SetError(pathTextBox, $"{fieldName} must be a '{expectedExtension}' file. Current is '{extensionToCheck}'.");
                        return false;
                    }
                }
            }
            return true;
        }


        /// <summary>
        /// Sets an error message for a specified control using the form's ErrorProvider.
        /// </summary>
        private void SetError(Control control, string message)
        {
            errorProvider.SetError(control, message);
        }
        #endregion

        #region File/Folder Browser Event Handlers
        /// <summary>
        /// Opens a file dialog to allow the user to select a file.
        /// </summary>
        private void BrowseFile(TextBox targetTextBox, string title, string filter, bool checkFileExists = true)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Title = title;
                openFileDialog.Filter = filter;
                openFileDialog.CheckFileExists = checkFileExists; // Dialog itself checks
                openFileDialog.InitialDirectory = GetInitialDirectoryFromPath(targetTextBox.Text);

                if (openFileDialog.ShowDialog(this) == DialogResult.OK)
                {
                    targetTextBox.Text = openFileDialog.FileName;
                    errorProvider.SetError(targetTextBox, ""); // Clear error after successful browse
                    // Re-validate the specific control after browsing
                    ValidatePathControl(targetTextBox, targetTextBox.Tag?.ToString() ?? "Path", isFile: true, required: true, fileMustExist: checkFileExists, expectedExtension: Path.GetExtension(filter.Split('|')[1].TrimStart('*')));
                }
            }
        }

        /// <summary>
        /// Opens a folder browser dialog to allow the user to select a directory.
        /// </summary>
        private void BrowseFolder(TextBox targetTextBox, string description)
        {
            using (FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog())
            {
                folderBrowserDialog.Description = description;
                folderBrowserDialog.ShowNewFolderButton = true;
                folderBrowserDialog.SelectedPath = GetInitialDirectoryFromPath(targetTextBox.Text, true);

                if (folderBrowserDialog.ShowDialog(this) == DialogResult.OK)
                {
                    targetTextBox.Text = folderBrowserDialog.SelectedPath;
                    errorProvider.SetError(targetTextBox, ""); // Clear error
                                                               // Re-validate
                    ValidatePathControl(targetTextBox, targetTextBox.Tag?.ToString() ?? "Path", isFile: false, required: true, fileMustExist: (targetTextBox == txtTemplateBase));
                }
            }
        }

        /// <summary>
        /// Gets an initial directory for file/folder dialogs from an existing path string.
        /// </summary>
        private string GetInitialDirectoryFromPath(string path, bool isDirectoryPathHint = false)
        {
            if (!string.IsNullOrWhiteSpace(path))
            {
                try
                {
                    string expandedPath = Environment.ExpandEnvironmentVariables(path);
                    if (isDirectoryPathHint)
                    {
                        if (Directory.Exists(expandedPath)) return expandedPath;
                        string? parent = Path.GetDirectoryName(expandedPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
                        if (!string.IsNullOrEmpty(parent) && Directory.Exists(parent)) return parent;
                    }
                    else // isFile path hint
                    {
                        if (File.Exists(expandedPath)) return Path.GetDirectoryName(expandedPath) ?? Environment.CurrentDirectory;
                        string? parent = Path.GetDirectoryName(expandedPath);
                        if (!string.IsNullOrEmpty(parent) && Directory.Exists(parent)) return parent;
                    }
                }
                catch (ArgumentException) { /* Invalid chars or env var, fallback. */ }
                catch (PathTooLongException) { /* Path too long, fallback. */ }
            }
            // Fallback to a sensible default if path is empty, invalid, or doesn't exist
            string myDocuments = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            if (Directory.Exists(myDocuments)) return myDocuments;
            return Environment.CurrentDirectory; // Absolute fallback
        }

        // Event handlers for "Browse..." buttons
        private void btnBrowseCrystalReport_Click(object? sender, EventArgs e) => BrowseFile(txtCrystalReportRptFile, "Select Crystal Report File", "Report Files (*.rpt)|*.rpt|All Files (*.*)|*.*", true);
        private void btnBrowseFinalReportOutputBase_Click(object? sender, EventArgs e) => BrowseFolder(txtFinalReportOutputBase, "Select Base Directory for Final Report Output");
        private void btnBrowseTemplateBase_Click(object? sender, EventArgs e) => BrowseFolder(txtTemplateBase, "Select Base Directory for Excel Templates");
        private void btnBrowseLogDirectoryBase_Click(object? sender, EventArgs e) => BrowseFolder(txtLogDirectoryBase, "Select Base Directory for Log Files");
        private void btnBrowseRawReportOutputBase_Click(object? sender, EventArgs e) => BrowseFolder(txtRawReportOutputBase, "Select Base Directory for Raw Report Exports");
        private void btnBrowseWrapperExecutable_Click(object? sender, EventArgs e) => BrowseFile(txtWrapperExecutable, "Select Wrapper Executable", "Executable Files (*.exe)|*.exe|All Files (*.*)|*.*", true);
        private void btnBrowseFallbackLogDir_Click(object? sender, EventArgs e) => BrowseFolder(txtFallbackLogDirectory, "Select Default Fallback Log Directory");
        #endregion
    }
}
