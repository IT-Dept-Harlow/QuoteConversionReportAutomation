// C# 10+ Features
using conversionTest;

namespace QuoteConversionReportAutomation
{
    // --- Using Statements ---
    using System;
    using System.Drawing;
    using System.Windows.Forms;
    using Microsoft.Win32; // For Registry access
    using System.Threading.Tasks; // For Task.Delay

    /// <summary>
    /// Manages UI updates, theme application, and control state for Form1.
    /// Reduces the UI logic complexity within Form1 itself.
    /// </summary>
    public class UIManager
    {
        #region Fields and Controls References

        // --- Control References (Passed in Constructor) ---
        private readonly Form _parentForm;
        private readonly MenuStrip _menuStrip;
        private readonly StatusStrip _statusStrip;
        private readonly ToolStripStatusLabel _statusLabel;
        private readonly ToolStripStatusLabel _autoRunStatusLabel;
        private readonly ToolStripMenuItem _darkModeMenuItem;
        private readonly Button _createReportButton;
        private readonly Button _processEmailButton;
        private readonly Button _toggleAutoRunButton;
        private readonly Button _viewReportButton;
        private readonly Button _viewAnalysisButton;
        private readonly ComboBox _reportTypeComboBox;
        private readonly DateTimePicker _startDatePicker;
        private readonly DateTimePicker _endDatePicker;
        private readonly ComboBox _financialYearComboBox;
        private readonly Label _financialYearLabel;
        private readonly CheckBox _sendToFemiOnlyCheckBox;
        private readonly Label _emailRecipientLabel;

        // --- Theme Colors ---
        // Dark Mode
        private static readonly Color _darkModeBackColor = Color.FromArgb(45, 45, 48);
        private static readonly Color _darkModeForeColor = Color.White;
        private static readonly Color _darkModeButtonBackColor = Color.FromArgb(63, 63, 70);
        private static readonly Color _darkModeTextBoxBackColor = Color.FromArgb(60, 60, 63);
        private static readonly Color _darkModeMenuBackColor = Color.FromArgb(60, 60, 63);
        private static readonly Color _darkModeMenuForeColor = Color.White;
        private static readonly Color _darkModeCheckBoxBackColor = Color.FromArgb(45, 45, 48);
        // Light Mode
        private static readonly Color _lightModeBackColor = SystemColors.Control;
        private static readonly Color _lightModeForeColor = SystemColors.ControlText;
        private static readonly Color _lightModeButtonBackColor = SystemColors.Control;
        private static readonly Color _lightModeTextBoxBackColor = SystemColors.Window;
        private static readonly Color _lightModeMenuBackColor = SystemColors.Control;
        private static readonly Color _lightModeMenuForeColor = SystemColors.ControlText;
        // Auto Run Button Specific Colors
        private static readonly Color _autoRunEnabledColor = Color.LightGreen;
        private static readonly Color _autoRunDisabledColor = Color.LightCoral;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the UIManager class.
        /// </summary>
        /// <param name="parentForm">The main form.</param>
        /// <param name="menuStrip">The main menu strip.</param>
        /// <param name="statusStrip">The main status strip.</param>
        /// <param name="statusLabel">The primary status label (left).</param>
        /// <param name="autoRunStatusLabel">The auto-run status label (right).</param>
        /// <param name="darkModeMenuItem">The dark mode toggle menu item.</param>
        /// <param name="createReportButton">The 'Create Report' button.</param>
        /// <param name="processEmailButton">The 'Process & Email' button.</param>
        /// <param name="toggleAutoRunButton">The 'Toggle Auto Run' button.</param>
        /// <param name="viewReportButton">The 'View Report' button.</param>
        /// <param name="viewAnalysisButton">The 'View Analysis' button.</param>
        /// <param name="reportTypeComboBox">The report type dropdown.</param>
        /// <param name="startDatePicker">The start date picker.</param>
        /// <param name="endDatePicker">The end date picker.</param>
        /// <param name="financialYearComboBox">The financial year dropdown.</param>
        /// <param name="financialYearLabel">The financial year label.</param>
        /// <param name="sendToFemiOnlyCheckBox">The 'Send to Femi Only' checkbox.</param>
        /// <param name="emailRecipientLabel">The label indicating the daily email recipient.</param>
        public UIManager(
            Form parentForm, MenuStrip menuStrip, StatusStrip statusStrip,
            ToolStripStatusLabel statusLabel, ToolStripStatusLabel autoRunStatusLabel,
            ToolStripMenuItem darkModeMenuItem, Button createReportButton, Button processEmailButton,
            Button toggleAutoRunButton, Button viewReportButton, Button viewAnalysisButton,
            ComboBox reportTypeComboBox, DateTimePicker startDatePicker, DateTimePicker endDatePicker,
            ComboBox financialYearComboBox, Label financialYearLabel, CheckBox sendToFemiOnlyCheckBox,
            Label emailRecipientLabel)
        {
            // Store references to controls
            _parentForm = parentForm ?? throw new ArgumentNullException(nameof(parentForm));
            _menuStrip = menuStrip ?? throw new ArgumentNullException(nameof(menuStrip));
            _statusStrip = statusStrip ?? throw new ArgumentNullException(nameof(statusStrip));
            _statusLabel = statusLabel ?? throw new ArgumentNullException(nameof(statusLabel));
            _autoRunStatusLabel = autoRunStatusLabel ?? throw new ArgumentNullException(nameof(autoRunStatusLabel));
            _darkModeMenuItem = darkModeMenuItem ?? throw new ArgumentNullException(nameof(darkModeMenuItem));
            _createReportButton = createReportButton ?? throw new ArgumentNullException(nameof(createReportButton));
            _processEmailButton = processEmailButton ?? throw new ArgumentNullException(nameof(processEmailButton));
            _toggleAutoRunButton = toggleAutoRunButton ?? throw new ArgumentNullException(nameof(toggleAutoRunButton));
            _viewReportButton = viewReportButton ?? throw new ArgumentNullException(nameof(viewReportButton));
            _viewAnalysisButton = viewAnalysisButton ?? throw new ArgumentNullException(nameof(viewAnalysisButton));
            _reportTypeComboBox = reportTypeComboBox ?? throw new ArgumentNullException(nameof(reportTypeComboBox));
            _startDatePicker = startDatePicker ?? throw new ArgumentNullException(nameof(startDatePicker));
            _endDatePicker = endDatePicker ?? throw new ArgumentNullException(nameof(endDatePicker));
            _financialYearComboBox = financialYearComboBox ?? throw new ArgumentNullException(nameof(financialYearComboBox));
            _financialYearLabel = financialYearLabel ?? throw new ArgumentNullException(nameof(financialYearLabel));
            _sendToFemiOnlyCheckBox = sendToFemiOnlyCheckBox ?? throw new ArgumentNullException(nameof(sendToFemiOnlyCheckBox));
            _emailRecipientLabel = emailRecipientLabel ?? throw new ArgumentNullException(nameof(emailRecipientLabel));
        }

        #endregion

        #region Theme Management

        /// <summary>
        /// Applies the selected theme (Dark or Light) to the form and its relevant controls.
        /// </summary>
        /// <param name="isDarkMode">True to apply dark mode, false to apply light mode.</param>
        public void ApplyTheme(bool isDarkMode)
        {
            // Determine colors based on the mode
            Color backColor = isDarkMode ? _darkModeBackColor : _lightModeBackColor;
            Color foreColor = isDarkMode ? _darkModeForeColor : _lightModeForeColor;
            Color buttonBackColor = isDarkMode ? _darkModeButtonBackColor : _lightModeButtonBackColor;
            Color buttonForeColor = foreColor;
            Color textBoxBackColor = isDarkMode ? _darkModeTextBoxBackColor : _lightModeTextBoxBackColor;
            Color textBoxForeColor = foreColor;
            Color menuBackColor = isDarkMode ? _darkModeMenuBackColor : _lightModeMenuBackColor;
            Color menuForeColor = isDarkMode ? _darkModeMenuForeColor : _lightModeMenuForeColor;
            Color statusStripBackColor = isDarkMode ? _darkModeBackColor : _lightModeBackColor;
            Color statusStripForeColor = foreColor;
            Color checkBoxBackColor = isDarkMode ? _darkModeCheckBoxBackColor : Color.Transparent;

            // Apply to Form itself
            _parentForm.BackColor = backColor;
            _parentForm.ForeColor = foreColor;

            // Apply colors to child controls recursively/selectively
            UpdateControlColors(_parentForm.Controls, backColor, foreColor, buttonBackColor, buttonForeColor, textBoxBackColor, textBoxForeColor, checkBoxBackColor);

            // Apply specific styling for MenuStrip and StatusStrip
            _menuStrip.BackColor = menuBackColor;
            _menuStrip.ForeColor = menuForeColor;
            UpdateMenuItemsTheme(_menuStrip.Items, menuBackColor, menuForeColor);

            _statusStrip.BackColor = statusStripBackColor;
            _statusStrip.ForeColor = statusStripForeColor;
            _statusLabel.ForeColor = statusStripForeColor;
            // AutoRun label color is handled by UpdateAutoRunUI

            // Re-apply Auto Run UI state which includes color logic based on the new theme
            // Form1 needs to call this after ApplyTheme with the correct state
            // Example: _uiManager.UpdateAutoRunUI(isTimerEnabled, isFinalStatus, isDarkMode, currentStatusText);

            Logger.LogInfo($"Theme applied: {(isDarkMode ? "Dark Mode" : "Light Mode")}");
        }

        /// <summary>
        /// Recursively updates the BackColor and ForeColor of menu items.
        /// </summary>
        private static void UpdateMenuItemsTheme(ToolStripItemCollection items, Color backColor, Color foreColor)
        {
            foreach (ToolStripItem item in items)
            {
                item.BackColor = backColor;
                item.ForeColor = foreColor;
                if (item is ToolStripMenuItem menuItem && menuItem.HasDropDownItems)
                {
                    UpdateMenuItemsTheme(menuItem.DropDownItems, backColor, foreColor);
                }
            }
        }

        /// <summary>
        /// Recursively updates the BackColor and ForeColor of controls within a collection,
        /// applying specific styles for known control types. Skips AutoRun button background.
        /// </summary>
        private void UpdateControlColors(
            Control.ControlCollection controls,
            Color backColor, Color foreColor,
            Color buttonBackColor, Color buttonForeColor,
            Color textBoxBackColor, Color textBoxForeColor,
            Color checkBoxBackColor)
        {
            foreach (Control control in controls)
            {
                if (control is Button button)
                {
                    // Skip setting background color for the AutoRun button
                    if (control == _toggleAutoRunButton)
                    {
                        button.ForeColor = buttonForeColor; // Set text color only
                    }
                    else
                    {
                        button.BackColor = buttonBackColor;
                        button.ForeColor = buttonForeColor;
                    }
                }
                else if (control is TextBox || control is RichTextBox)
                {
                    control.BackColor = textBoxBackColor;
                    control.ForeColor = textBoxForeColor;
                }
                else if (control is DateTimePicker dtp)
                {
                    dtp.BackColor = textBoxBackColor;
                    dtp.ForeColor = textBoxForeColor;
                }
                else if (control is ComboBox comboBox)
                {
                    comboBox.BackColor = textBoxBackColor;
                    comboBox.ForeColor = textBoxForeColor;
                }
                else if (control is CheckBox checkbox)
                {
                    checkbox.BackColor = checkBoxBackColor;
                    checkbox.ForeColor = foreColor;
                }
                else if (control is Label label)
                {
                    if (control == _emailRecipientLabel)
                    {
                        label.BackColor = backColor;
                        label.ForeColor = foreColor;
                    }
                    else
                    {
                        label.BackColor = Color.Transparent;
                        label.ForeColor = foreColor;
                    }
                }
                else if (control is GroupBox groupBox)
                {
                    groupBox.ForeColor = foreColor;
                    UpdateControlColors(groupBox.Controls, backColor, foreColor, buttonBackColor, buttonForeColor, textBoxBackColor, textBoxForeColor, checkBoxBackColor);
                }
                else if (control is Panel panel)
                {
                    panel.BackColor = backColor;
                    UpdateControlColors(panel.Controls, backColor, foreColor, buttonBackColor, buttonForeColor, textBoxBackColor, textBoxForeColor, checkBoxBackColor);
                }
                else if (control is StatusStrip || control is ToolStrip || control is MenuStrip)
                {
                    continue; // Handled separately
                }
                else
                {
                    try
                    {
                        control.BackColor = backColor;
                        control.ForeColor = foreColor;
                    }
                    catch (Exception ex)
                    {
                        Logger.LogWarning($"Could not set theme colors for control '{control.Name}' of type {control.GetType().Name}: {ex.Message}");
                    }
                }
            }
        }

        /// <summary>
        /// Checks the Windows Registry to determine if the Apps theme is set to dark mode.
        /// </summary>
        /// <returns>True if dark mode is enabled for apps, false otherwise (or if key is not found).</returns>
        public static bool IsWindowsDarkModeEnabled()
        {
            try
            {
                const string keyPath = @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
                const string valueName = "AppsUseLightTheme";
                object? registryValue = Registry.GetValue(keyPath, valueName, 1); // Default to 1 (Light)
                return registryValue is int intValue && intValue == 0; // Dark mode if value is 0
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error reading Windows theme setting from registry: {ex.Message}");
                return false; // Default to Light mode on error
            }
        }

        #endregion

        #region UI State Management

        /// <summary>
        /// Updates the text of the main status label (left).
        /// </summary>
        /// <param name="message">The status message to display.</param>
        public void UpdateStatusMain(string message)
        {
            SafeControlUpdate(_statusStrip, () => { _statusLabel.Text = message; });
        }

        /// <summary>
        /// Updates the text of the auto run status label (right).
        /// </summary>
        /// <param name="message">The status message to display.</param>
        public void UpdateStatusRight(string message)
        {
            SafeControlUpdate(_statusStrip, () => { _autoRunStatusLabel.Text = message; });
        }

        /// <summary>
        /// Enables or disables the main action buttons (Create Report, Process & Email).
        /// </summary>
        /// <param name="enable">True to enable, false to disable.</param>
        public void SetActionButtonsEnabled(bool enable)
        {
            SafeControlUpdate(_createReportButton, () => { _createReportButton.Enabled = enable; });
            SafeControlUpdate(_processEmailButton, () => { _processEmailButton.Enabled = enable; });
        }

        /// <summary>
        /// Enables or disables secondary input controls (dropdowns, date pickers, checkboxes, view buttons).
        /// </summary>
        /// <param name="enable">True to enable, false to disable.</param>
        /// <param name="isFinancialYearVisible">Current visibility state of the financial year controls.</param>
        public void SetOtherControlsEnabled(bool enable, bool isFinancialYearVisible)
        {
            SafeControlUpdate(_reportTypeComboBox, () => { _reportTypeComboBox.Enabled = enable; });
            SafeControlUpdate(_startDatePicker, () => { _startDatePicker.Enabled = enable; });
            SafeControlUpdate(_endDatePicker, () => { _endDatePicker.Enabled = enable; });
            SafeControlUpdate(_financialYearComboBox, () => { _financialYearComboBox.Enabled = enable && isFinancialYearVisible; });
            SafeControlUpdate(_sendToFemiOnlyCheckBox, () => { _sendToFemiOnlyCheckBox.Enabled = enable; });
            SafeControlUpdate(_viewReportButton, () => { _viewReportButton.Enabled = enable; });
            SafeControlUpdate(_viewAnalysisButton, () => { _viewAnalysisButton.Enabled = enable; });
        }

        /// <summary>
        /// Resets the UI to an initial state after an error, cancellation, or completion.
        /// Schedules the main status label reset.
        /// </summary>
        /// <param name="button1Text">Text for the 'Create Report' button.</param>
        /// <param name="configValid">Indicates if essential configuration is valid.</param>
        /// <param name="rawReportExists">Indicates if the raw report file exists.</param>
        /// <param name="analysisExists">Indicates if the analysis file exists.</param>
        /// <param name="isDailySelected">Indicates if the 'Daily' report type is selected.</param>
        /// <param name="isTimerEnabled">Indicates if the auto-run timer is currently enabled.</param>
        /// <param name="isDarkMode">Indicates if dark mode is currently active.</param>
        /// <param name="isFinalStatusForToday">Indicates if the auto-run status is final for today.</param>
        /// <param name="currentAutoRunStatusText">The current text of the auto-run status label.</param>
        public void ResetUIOnError(string button1Text, bool configValid, bool rawReportExists, bool analysisExists, bool isDailySelected, bool isTimerEnabled, bool isDarkMode, bool isFinalStatusForToday, string currentAutoRunStatusText)
        {
            SafeControlUpdate(_parentForm, () =>
            {
                Logger.LogDebug($"Resetting UI state. Button 1 text: '{button1Text}'");

                // --- Enable/Disable Primary Buttons ---
                _createReportButton.Enabled = configValid;
                _createReportButton.Text = configValid ? button1Text : "Config Error";
                _processEmailButton.Enabled = rawReportExists;
                _processEmailButton.Text = "Process and Email";
                _toggleAutoRunButton.Enabled = true; // Always re-enable toggle after manual ops

                // --- Enable Other Controls ---
                SetOtherControlsEnabled(true, _financialYearComboBox.Visible); // Pass current visibility

                // --- Update Visibility/Enabled for View buttons ---
                _viewReportButton.Visible = rawReportExists;
                _viewReportButton.Enabled = rawReportExists;
                _viewAnalysisButton.Visible = analysisExists;
                _viewAnalysisButton.Enabled = analysisExists;

                // --- Update Femi/Paul labels ---
                _sendToFemiOnlyCheckBox.Visible = !isDailySelected;
                _emailRecipientLabel.Visible = isDailySelected;
                if (isDailySelected) _emailRecipientLabel.Text = "Emailing Daily report to Paul";

                // --- Update Status ---
                // Reset AutoRun UI based on timer state
                UpdateAutoRunUI(isTimerEnabled, isFinalStatusForToday, isDarkMode, currentAutoRunStatusText);

                // Schedule main status reset after delay
                string currentMainStatus = _statusLabel.Text ?? string.Empty;
                if (currentMainStatus != "Ready" && !currentMainStatus.StartsWith("Auto Run:"))
                {
                    _ = Task.Delay(5000).ContinueWith(t => {
                        string statusNow = _statusLabel.Text ?? string.Empty;
                        if (statusNow == currentMainStatus && !statusNow.StartsWith("Auto Run:"))
                        {
                            UpdateStatusMain("Ready");
                        }
                    }, TaskScheduler.FromCurrentSynchronizationContext());
                }
            });
        }

        /// <summary>
        /// Sets the UI state after a successful manual process completion (including email).
        /// </summary>
        /// <param name="configValid">Indicates if essential configuration is valid.</param>
        /// <param name="isDailySelected">Indicates if the 'Daily' report type is selected.</param>
        /// <param name="isTimerEnabled">Indicates if the auto-run timer is currently enabled.</param>
        /// <param name="isDarkMode">Indicates if dark mode is currently active.</param>
        /// <param name="isFinalStatusForToday">Indicates if the auto-run status is final for today.</param>
        /// <param name="currentAutoRunStatusText">The current text of the auto-run status label.</param>
        public void SetUICompleted(bool configValid, bool isDailySelected, bool isTimerEnabled, bool isDarkMode, bool isFinalStatusForToday, string currentAutoRunStatusText)
        {
            UpdateStatusMain("Process Completed Successfully.");
            // Reset most controls, passing current state flags
            ResetUIOnError("Create Report", configValid, File.Exists(_viewReportButton.Tag?.ToString() ?? ""), File.Exists(_viewAnalysisButton.Tag?.ToString() ?? ""), isDailySelected, isTimerEnabled, isDarkMode, isFinalStatusForToday, currentAutoRunStatusText);

            // Explicitly set button states AFTER ResetUIOnError
            SafeControlUpdate(_parentForm, () => {
                _createReportButton.Enabled = configValid;
                _processEmailButton.Enabled = false; // Ensure Process is disabled after completion
            });
        }

        /// <summary>
        /// Resets button states when the report type changes.
        /// </summary>
        /// <param name="configValid">Indicates if essential configuration is valid.</param>
        public void ResetButtonStatesAfterTypeChange(bool configValid)
        {
            SafeControlUpdate(_parentForm, () =>
            {
                Logger.LogDebug("Resetting button states due to report type change.");
                _createReportButton.Enabled = configValid;
                _createReportButton.Text = configValid ? "Create Report" : "Config Error";
                _processEmailButton.Text = "Process and Email";
                _processEmailButton.Enabled = false;

                _viewReportButton.Visible = false;
                _viewReportButton.Enabled = false;
                _viewAnalysisButton.Visible = false;
                _viewAnalysisButton.Enabled = false;

                // Clear any stored paths associated with view buttons (using Tag property as an example)
                _viewReportButton.Tag = null;
                _viewAnalysisButton.Tag = null;

                UpdateStatusMain("Ready");
            });
        }

        /// <summary>
        /// Shows or hides the "View Analysis" button.
        /// </summary>
        /// <param name="show">True to show, false to hide.</param>
        /// <param name="filePath">The file path to associate with the button (e.g., stored in Tag).</param>
        public void ShowViewAnalysisButton(bool show, string? filePath = null)
        {
            SafeControlUpdate(_viewAnalysisButton, () =>
            {
                _viewAnalysisButton.Visible = show;
                _viewAnalysisButton.Enabled = show;
                _viewAnalysisButton.Tag = filePath; // Store path for potential use by click handler
            });
        }

        /// <summary>
        /// Shows or hides the "View Report" button.
        /// </summary>
        /// <param name="show">True to show, false to hide.</param>
        /// <param name="filePath">The file path to associate with the button (e.g., stored in Tag).</param>
        public void ShowViewReportButton(bool show, string? filePath = null)
        {
            SafeControlUpdate(_viewReportButton, () =>
            {
                _viewReportButton.Visible = show;
                _viewReportButton.Enabled = show;
                _viewReportButton.Tag = filePath; // Store path
            });
        }

        #endregion

        #region Auto Run UI

        /// <summary>
        /// Updates the UI elements related to the auto-run feature (button text/color, status label text/color).
        /// </summary>
        /// <param name="enable">True if auto-run timer is enabled.</param>
        /// <param name="isFinalStatusForToday">True if a final status ("Completed", "FAILED", "Done") is set for today.</param>
        /// <param name="isDarkMode">True if dark mode is active.</param>
        /// <param name="statusText">The text to display if not using default Enabled/Disabled text.</param>
        public void UpdateAutoRunUI(bool enable, bool isFinalStatusForToday, bool isDarkMode, string statusText = "")
        {
            SafeControlUpdate(_statusStrip, () =>
            {
                if (_autoRunStatusLabel == null || _autoRunStatusLabel.IsDisposed || _toggleAutoRunButton == null || _toggleAutoRunButton.IsDisposed) return;

                string currentStatusText = _autoRunStatusLabel.Text ?? string.Empty;
                string textToShow = statusText; // Use provided text if available

                if (enable)
                {
                    _toggleAutoRunButton.Text = "Disable Daily Auto Run @ 8 AM";
                    _toggleAutoRunButton.BackColor = _autoRunEnabledColor;
                    if (!isFinalStatusForToday && string.IsNullOrEmpty(textToShow)) // Only use default if no final status and no specific text given
                    {
                        textToShow = "Auto Run: Enabled";
                    }
                    _autoRunStatusLabel.ForeColor = Color.Green; // Always green if enabled
                }
                else // Timer is disabled
                {
                    _toggleAutoRunButton.Text = "Enable Daily Auto Run @ 8 AM";
                    _toggleAutoRunButton.BackColor = _autoRunDisabledColor;
                    if (!isFinalStatusForToday && string.IsNullOrEmpty(textToShow)) // Only use default if no final status and no specific text given
                    {
                        textToShow = "Auto Run: Disabled";
                    }
                    // Use theme-appropriate color for disabled/final status text
                    _autoRunStatusLabel.ForeColor = isDarkMode ? _darkModeForeColor : _lightModeForeColor;
                }

                // Set the status label text (use existing if final, otherwise use calculated textToShow)
                _autoRunStatusLabel.Text = isFinalStatusForToday ? currentStatusText : textToShow;


                // Ensure button text color contrasts with background
                _toggleAutoRunButton.ForeColor = _toggleAutoRunButton.BackColor.GetBrightness() > 0.5f
                                                ? Color.Black
                                                : Color.White;
            });
        }


        /// <summary>
        /// Disables primary UI controls during automated report execution.
        /// </summary>
        public void DisableControlsForAutoRun()
        {
            Logger.LogDebug("Disabling controls for Auto Run.");
            SafeControlUpdate(_parentForm, () =>
            {
                _createReportButton.Enabled = false;
                _processEmailButton.Enabled = false;
                _toggleAutoRunButton.Enabled = false; // Disable toggling during run
                _reportTypeComboBox.Enabled = false;
                _startDatePicker.Enabled = false;
                _endDatePicker.Enabled = false;
                _financialYearComboBox.Enabled = false;
                _sendToFemiOnlyCheckBox.Enabled = false;
                _viewReportButton.Enabled = false;
                _viewAnalysisButton.Enabled = false;
                // Keep menu items enabled
            });
        }

        // EnableControlsAfterAutoRun is effectively handled by calling ResetUIOnError
        // from the calling code (e.g., Form1 or AutoRunManager) after the auto-run completes.

        #endregion

        #region Safe UI Update Utility

        /// <summary>
        /// Safely updates a control's property or state by executing an action.
        /// Automatically marshals the call to the UI thread if required.
        /// Handles potential errors during invocation or if the control is disposed.
        /// </summary>
        /// <param name="ctrl">The control to update.</param>
        /// <param name="action">The action to perform on the control.</param>
        public static void SafeControlUpdate(Control ctrl, Action action)
        {
            ArgumentNullException.ThrowIfNull(action);
            if (ctrl == null || ctrl.IsDisposed || !ctrl.IsHandleCreated)
            {
                return; // Ignore update if control is not valid
            }

            if (ctrl.InvokeRequired)
            {
                try { ctrl.BeginInvoke(action); }
                catch (ObjectDisposedException) { /* Ignore */ }
                catch (InvalidOperationException ex) when (ex.Message.Contains("Invoke") || ex.Message.Contains("Handle"))
                { Logger.LogWarning($"SafeControlUpdate ignored invoke/handle error: {ex.Message}"); }
                catch (Exception ex) { Logger.LogError($"Unexpected error during SafeControlUpdate Invoke/BeginInvoke: {ex}"); }
            }
            else
            {
                try { action(); }
                catch (Exception ex) { Logger.LogError($"Unexpected error during SafeControlUpdate direct action: {ex}"); }
            }
        }

        #endregion
    }
}
