// UIManager.cs

#region Using Directives
// System related namespaces
using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

// Project specific namespaces
using QuoteConversionReportAutomation.Services.Logging;
using QuoteConversionReportAutomation.Theming;
#endregion

namespace QuoteConversionReportAutomation.Managers
{
    /// <summary>
    /// Manages UI updates, theme application, and control state for the main form.
    /// This class encapsulates the direct manipulation of UI controls, centralising the logic
    /// for enabling/disabling controls, applying visual themes, and updating status labels.
    /// It also provides static helper methods for applying themes to other forms in the application.
    /// </summary>
    public class UIManager
    {
        #region Fields and Control References
        // These fields hold direct references to the UI controls on Form1 that this manager will manipulate.
        // This approach centralises UI logic and decouples it from the main Form1 event handlers.

        private readonly Form _parentForm;
        private readonly MenuStrip _menuStrip;
        private readonly StatusStrip _statusStrip;
        private readonly ToolStripStatusLabel _autoRunStatusLabel;
        private readonly ToolStripMenuItem _darkModeMenuItem;
        private readonly Button _createReportButton;
        private readonly Button _processEmailButton;
        private readonly Button _oneClickProcessButton;
        private readonly Button _toggleAutoRunButton;
        private readonly Button _viewReportButton;
        private readonly Button _viewAnalysisButton;
        private readonly ComboBox _reportTypeComboBox;
        private readonly DateTimePicker _startDatePicker;
        private readonly DateTimePicker _endDatePicker;
        private readonly ComboBox _financialYearComboBox;
        private readonly Label _financialYearLabel;
        private readonly CheckBox _sendToFemiOnlyCheckBox;
        private readonly CheckBox _skipEmailCheckBox;
        private readonly Label _emailRecipientLabel;
        private readonly ToolTip _toolTip;
        private readonly CheckBox _includeLeadTimeAnalysisCheckBox;

        /// <summary>
        /// Stores the currently configured hour for the auto-run check, used for display purposes in the UI.
        /// </summary>
        private int _currentAutoRunHour = 8;
        #endregion

        #region P/Invoke Declarations and Constants
        // This region contains Platform Invoke (P/Invoke) declarations for interacting with native Windows APIs,
        // specifically the Desktop Window Manager (DWM) API. This is used to apply the dark theme to the
        // non-client area of the window (i.e., the title bar and border), which is not possible with standard WinForms properties.
        // TODO: As new versions of Windows are released, this P/Invoke logic may need to be updated to support future DWM attributes for theming.

        /// <summary>
        /// Sets the value of a Desktop Window Manager (DWM) window attribute.
        /// </summary>
        [DllImport("dwmapi.dll", PreserveSig = true)]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        /// <summary>
        /// DWM attribute to control the dark mode for the window caption in Windows 10 build 18362 (1903) and later.
        /// </summary>
        private const int DWMWA_USE_IMMERSIVE_DARK_MODE_WINDOWS_10_1903 = 19;

        /// <summary>
        /// DWM attribute to control the dark mode for the window caption in Windows 10 build 19041 (2004) and later.
        /// </summary>
        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

        /// <summary>
        /// The RedrawWindow function updates the specified rectangle or region in a window's client area.
        /// </summary>
        [DllImport("user32.dll")]
        private static extern bool RedrawWindow(IntPtr hWnd, IntPtr lprcUpdate, IntPtr hrgnUpdate, uint flags);

        // Flags for the RedrawWindow function.
        private const uint RDW_INVALIDATE = 0x0001;
        private const uint RDW_ERASE = 0x0004;
        private const uint RDW_UPDATENOW = 0x0100;
        private const uint RDW_ERASENOW = 0x0200;
        private const uint RDW_FRAME = 0x0400;
        #endregion

        #region Constructor
        /// <summary>
        /// Initialises a new instance of the UIManager class for managing the main form's UI.
        /// </summary>
        /// <param name="parentForm">A reference to the main form.</param>
        /// <param name="menuStrip">A reference to the main MenuStrip.</param>
        /// <param name="statusStrip">A reference to the main StatusStrip.</param>
        /// <param name="autoRunStatusLabel">A reference to the ToolStripStatusLabel for auto-run status.</param>
        /// <param name="darkModeMenuItem">A reference to the dark mode toggle ToolStripMenuItem.</param>
        /// <param name="createReportButton">A reference to the 'Create Report' button.</param>
        /// <param name="processEmailButton">A reference to the 'Process & Email' button.</param>
        /// <param name="oneClickProcessButton">A reference to the '1-Click Process' button.</param>
        /// <param name="toggleAutoRunButton">A reference to the 'Enable/Disable Auto Run' button.</param>
        /// <param name="viewReportButton">A reference to the 'View Raw File' button.</param>
        /// <param name="viewAnalysisButton">A reference to the 'View Processed File' button.</param>
        /// <param name="reportTypeComboBox">A reference to the report type ComboBox.</param>
        /// <param name="startDatePicker">A reference to the start date DateTimePicker.</param>
        /// <param name="endDatePicker">A reference to the end date DateTimePicker.</param>
        /// <param name="financialYearComboBox">A reference to the financial year ComboBox.</param>
        /// <param name="financialYearLabel">A reference to the financial year Label.</param>
        /// <param name="sendToFemiOnlyCheckBox">A reference to the 'Send to Femi Only' CheckBox.</param>
        /// <param name="skipEmailCheckBox">A reference to the 'Skip Sending Email' CheckBox.</param>
        /// <param name="emailRecipientLabel">A reference to the email recipient info Label.</param>
        /// <param name="toolTip">A reference to the form's ToolTip component.</param>
        public UIManager(
            Form parentForm, MenuStrip menuStrip, StatusStrip statusStrip,
            ToolStripStatusLabel autoRunStatusLabel,
            ToolStripMenuItem darkModeMenuItem, Button createReportButton, Button processEmailButton,
            Button oneClickProcessButton, Button toggleAutoRunButton, Button viewReportButton,
            Button viewAnalysisButton, ComboBox reportTypeComboBox, DateTimePicker startDatePicker,
            DateTimePicker endDatePicker, ComboBox financialYearComboBox, Label financialYearLabel,
            CheckBox sendToFemiOnlyCheckBox, CheckBox skipEmailCheckBox, CheckBox includeLeadTimeAnalysisCheckBox, Label emailRecipientLabel, ToolTip toolTip)
        {
            // TODO: The constructor has a large number of parameters. If more controls are added in the future,
            // consider refactoring to pass a single context object or a dictionary of controls to simplify the signature.

            _parentForm = parentForm ?? throw new ArgumentNullException(nameof(parentForm));
            _menuStrip = menuStrip ?? throw new ArgumentNullException(nameof(menuStrip));
            _statusStrip = statusStrip ?? throw new ArgumentNullException(nameof(statusStrip));
            _autoRunStatusLabel = autoRunStatusLabel ?? throw new ArgumentNullException(nameof(autoRunStatusLabel));
            _darkModeMenuItem = darkModeMenuItem ?? throw new ArgumentNullException(nameof(darkModeMenuItem));
            _createReportButton = createReportButton ?? throw new ArgumentNullException(nameof(createReportButton));
            _processEmailButton = processEmailButton ?? throw new ArgumentNullException(nameof(processEmailButton));
            _oneClickProcessButton = oneClickProcessButton ?? throw new ArgumentNullException(nameof(oneClickProcessButton));
            _toggleAutoRunButton = toggleAutoRunButton ?? throw new ArgumentNullException(nameof(toggleAutoRunButton));
            _viewReportButton = viewReportButton ?? throw new ArgumentNullException(nameof(viewReportButton));
            _viewAnalysisButton = viewAnalysisButton ?? throw new ArgumentNullException(nameof(viewAnalysisButton));
            _reportTypeComboBox = reportTypeComboBox ?? throw new ArgumentNullException(nameof(reportTypeComboBox));
            _startDatePicker = startDatePicker ?? throw new ArgumentNullException(nameof(startDatePicker));
            _endDatePicker = endDatePicker ?? throw new ArgumentNullException(nameof(endDatePicker));
            _financialYearComboBox = financialYearComboBox ?? throw new ArgumentNullException(nameof(financialYearComboBox));
            _financialYearLabel = financialYearLabel ?? throw new ArgumentNullException(nameof(financialYearLabel));
            _sendToFemiOnlyCheckBox = sendToFemiOnlyCheckBox ?? throw new ArgumentNullException(nameof(sendToFemiOnlyCheckBox));
            _skipEmailCheckBox = skipEmailCheckBox ?? throw new ArgumentNullException(nameof(skipEmailCheckBox));
            _includeLeadTimeAnalysisCheckBox = includeLeadTimeAnalysisCheckBox ?? throw new ArgumentNullException(nameof(includeLeadTimeAnalysisCheckBox));
            _emailRecipientLabel = emailRecipientLabel ?? throw new ArgumentNullException(nameof(emailRecipientLabel));
            _toolTip = toolTip ?? throw new ArgumentNullException(nameof(toolTip));
        }
        #endregion

        #region Theme Management
        /// <summary>
        /// Applies the currently selected theme from <see cref="ThemeSettings"/> to the UIManager's parent form and its controls.
        /// </summary>
        public void ApplyTheme()
        {
            // Determine the current theme state and get the corresponding colour palette.
            bool isCurrentlyDark = ThemeSettings.IsCurrentlyDark();
            ThemePalette palette = ThemeSettings.CurrentPalette;

            // Perform all UI updates on the parent form's thread for safety.
            SafeControlUpdate(_parentForm, () =>
            {
                // Apply base colours to the form itself.
                _parentForm.BackColor = palette.FormBackColor;
                _parentForm.ForeColor = palette.FormForeColor;

                // Use the P/Invoke method to apply the theme to the window's title bar.
                if (UseImmersiveDarkModeInternal(_parentForm.Handle, isCurrentlyDark))
                {
                    // Force the window to redraw its frame to reflect the change.
                    RedrawWindow(_parentForm.Handle, IntPtr.Zero, IntPtr.Zero, RDW_FRAME | RDW_INVALIDATE | RDW_UPDATENOW);
                }

                // Recursively apply the theme to all child controls on the form.
                UpdateControlThemeRecursive(_parentForm, palette, isCurrentlyDark);

                // Apply specific theming to the MenuStrip and StatusStrip.
                if (_menuStrip != null)
                {
                    _menuStrip.BackColor = palette.MenuStripBackColor;
                    _menuStrip.ForeColor = palette.MenuStripForeColor;
                    _menuStrip.Renderer = new CustomThemeMenuRenderer(palette, ThemeSettings.EnableCustomTheming);
                    UpdateMenuItemsTheme(_menuStrip.Items, palette.MenuStripBackColor, palette.MenuStripForeColor);
                }
                if (_statusStrip != null)
                {
                    _statusStrip.BackColor = palette.StatusStripBackColor;
                    _statusStrip.ForeColor = palette.StatusStripForeColor;
                }
                if (_autoRunStatusLabel != null)
                {
                    _autoRunStatusLabel.ForeColor = palette.StatusStripForeColor;
                    _autoRunStatusLabel.BackColor = Color.Transparent;
                }

                // Force a refresh to ensure all visual changes are painted.
                _parentForm.Refresh();
            });

            // Re-apply the specific colours for the Auto-Run button after the general theme is applied.
            if (_toggleAutoRunButton != null && _autoRunStatusLabel != null)
            {
                bool isTimerCurrentlyEnabled = false;
                SafeControlUpdate(_toggleAutoRunButton, () => isTimerCurrentlyEnabled = _toggleAutoRunButton.Text.StartsWith("Disable"));
                string statusText = GetAutoRunStatusLabelText() ?? "";
                bool isAutoRunStatusFinal = statusText.Contains("Completed") || statusText.Contains("Done for") || statusText.Contains("FAILED");
                UpdateAutoRunUI(isTimerCurrentlyEnabled, isAutoRunStatusFinal, statusText);
            }
        }

        /// <summary>
        /// Applies window frame theming (title bar, basic background/foreground) to an external form.
        /// This static method centralises the P/Invoke calls for theming any Form and uses the new ThemeSettings palettes.
        /// </summary>
        /// <param name="formToTheme">The Form instance to apply the theme to.</param>
        /// <param name="isDarkModeEnabled">True to apply dark mode, false for light mode.</param>
        public static void ApplyThemeToExternalForm(Form formToTheme, bool isDarkModeEnabled)
        {
            if (formToTheme == null || formToTheme.IsDisposed)
            {
                Logger.LogWarning("ApplyThemeToExternalForm: Attempted to theme a null or disposed form.");
                return;
            }

            // Select the correct palette based on the parameter.
            ThemePalette palette = isDarkModeEnabled ? ThemeSettings.DarkPalette : ThemeSettings.LightPalette;

            // Apply basic form colours directly from the selected palette.
            formToTheme.BackColor = palette.FormBackColor;
            formToTheme.ForeColor = palette.FormForeColor;

            // Apply title bar and frame theme using the P/Invoke helper.
            if (UseImmersiveDarkModeInternal(formToTheme.Handle, isDarkModeEnabled))
            {
                RedrawWindow(formToTheme.Handle, IntPtr.Zero, IntPtr.Zero, RDW_FRAME | RDW_INVALIDATE | RDW_UPDATENOW | RDW_ERASENOW);
            }
        }

        /// <summary>
        /// Recursively applies theme colours to a control and all its child controls using a <see cref="ThemePalette"/>.
        /// </summary>
        private void UpdateControlThemeRecursive(Control parentControl, ThemePalette palette, bool isCurrentlyDark)
        {
            // TODO: The list of themed controls is extensive. If new custom controls are added to the project,
            // they will need to be added to this theming logic to ensure a consistent look and feel.
            foreach (Control control in parentControl.Controls)
            {
                SafeControlUpdate(control, () =>
                {
                    if (control == _toggleAutoRunButton) { control.ForeColor = palette.AutoRunButtonForeColor; }
                    else if (control is Button button) { button.BackColor = palette.ButtonBackColor; button.ForeColor = palette.ButtonForeColor; button.FlatStyle = FlatStyle.Flat; button.FlatAppearance.BorderColor = palette.ButtonBorderColor; button.FlatAppearance.BorderSize = 1; }
                    else if (control is TextBox tb) { tb.BackColor = palette.ControlBackColor; tb.ForeColor = palette.ControlForeColor; tb.BorderStyle = isCurrentlyDark ? BorderStyle.FixedSingle : BorderStyle.Fixed3D; }
                    else if (control is RichTextBox rtb) { rtb.BackColor = palette.ControlBackColor; rtb.ForeColor = palette.ControlForeColor; rtb.BorderStyle = isCurrentlyDark ? BorderStyle.FixedSingle : BorderStyle.Fixed3D; }
                    else if (control is ComboBox cb) { cb.BackColor = palette.ControlBackColor; cb.ForeColor = palette.ControlForeColor; cb.FlatStyle = FlatStyle.Flat; }
                    else if (control is DateTimePicker dtp) { dtp.BackColor = palette.ControlBackColor; dtp.ForeColor = palette.ControlForeColor; dtp.CalendarForeColor = palette.ControlForeColor; dtp.CalendarMonthBackground = palette.ControlBackColor; dtp.CalendarTitleBackColor = palette.ButtonBackColor; dtp.CalendarTitleForeColor = palette.ButtonForeColor; dtp.CalendarTrailingForeColor = Color.Gray; }
                    else if (control is CheckBox chkBox) { chkBox.BackColor = palette.FormBackColor; chkBox.ForeColor = palette.LabelForeColor; chkBox.FlatStyle = FlatStyle.Standard; }
                    else if (control is Label) { control.BackColor = Color.Transparent; control.ForeColor = palette.LabelForeColor; }
                    else if (control is GroupBox gb) { gb.BackColor = palette.FormBackColor; gb.ForeColor = palette.GroupBoxForeColor; UpdateControlThemeRecursive(gb, palette, isCurrentlyDark); }
                    else if (control is Panel or TableLayoutPanel or TabControl)
                    {
                        control.BackColor = palette.FormBackColor;
                        control.ForeColor = palette.FormForeColor;
                        if (control is TabControl tabControl) { foreach (TabPage tabPage in tabControl.TabPages) { tabPage.BackColor = palette.FormBackColor; tabPage.ForeColor = palette.FormForeColor; UpdateControlThemeRecursive(tabPage, palette, isCurrentlyDark); } }
                        else { UpdateControlThemeRecursive(control, palette, isCurrentlyDark); }
                    }
                    else if (!(control is MenuStrip || control is StatusStrip || control is ToolStrip))
                    {
                        if (control.HasChildren) { UpdateControlThemeRecursive(control, palette, isCurrentlyDark); }
                        else { control.BackColor = palette.FormBackColor; control.ForeColor = palette.FormForeColor; }
                    }
                });
            }
        }

        /// <summary>
        /// Updates the theme for a collection of <see cref="ToolStripItem"/> objects on the main menu bar.
        /// </summary>
        private void UpdateMenuItemsTheme(ToolStripItemCollection items, Color menuStripBackColor, Color menuStripForeColor)
        {
            foreach (ToolStripItem item in items)
            {
                if (item.IsDisposed) continue;
                if (item.Owner == _menuStrip)
                {
                    item.BackColor = menuStripBackColor;
                    item.ForeColor = menuStripForeColor;
                }
            }
        }
        #endregion

        #region UI State Management
        /// <summary>
        /// Updates the text of the auto-run status label on the status strip in a thread-safe manner.
        /// </summary>
        /// <param name="message">The message to display.</param>
        public void UpdateStatusRight(string message)
        {
            if (_autoRunStatusLabel != null)
            {
                SafeToolStripItemUpdate(_autoRunStatusLabel, () => { _autoRunStatusLabel.Text = message; });
            }
        }

        /// <summary>
        /// Gets the current text of the auto-run status label in a thread-safe manner.
        /// </summary>
        /// <returns>The text of the auto-run status label.</returns>
        public string GetAutoRunStatusLabelText()
        {
            if (_autoRunStatusLabel == null) return string.Empty;

            ToolStrip? owner = _autoRunStatusLabel.Owner;
            if (owner != null && owner.IsHandleCreated && !owner.IsDisposed && !owner.Disposing)
            {
                if (owner.InvokeRequired)
                {
                    try
                    {
                        return (string)owner.Invoke(new Func<string>(() => _autoRunStatusLabel.Text ?? string.Empty));
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError($"Error during sync fetch of AutoRunStatusLabel text: {ex}");
                        return string.Empty;
                    }
                }
                else
                {
                    return _autoRunStatusLabel.Text ?? string.Empty;
                }
            }
            return string.Empty;
        }

        /// <summary>
        /// Enables or disables the main action buttons (Create Report, Process & Email, 1-Click Process).
        /// </summary>
        /// <param name="enable">True to enable the buttons, false to disable them.</param>
        public void SetActionButtonsEnabled(bool enable)
        {
            SafeControlUpdate(_createReportButton, () => { _createReportButton.Enabled = enable; });
            SafeControlUpdate(_processEmailButton, () => { _processEmailButton.Enabled = enable; });
            SafeControlUpdate(_oneClickProcessButton, () => { _oneClickProcessButton.Enabled = enable; });
        }

        /// <summary>
        /// Enables or disables other input controls on the form, such as date pickers and combo boxes.
        /// </summary>
        /// <param name="enable">True to enable the controls, false to disable them.</param>
        /// <param name="isFinancialYearVisible">Indicates if the financial year control is currently visible, affecting its enabled state.</param>
        public void SetOtherControlsEnabled(bool enable, bool isFinancialYearVisible)
        {
            SafeControlUpdate(_reportTypeComboBox, () => { _reportTypeComboBox.Enabled = enable; });
            SafeControlUpdate(_startDatePicker, () => { _startDatePicker.Enabled = enable; });
            SafeControlUpdate(_endDatePicker, () => { _endDatePicker.Enabled = enable; });
            SafeControlUpdate(_financialYearComboBox, () => { _financialYearComboBox.Enabled = enable && isFinancialYearVisible; });
            SafeControlUpdate(_sendToFemiOnlyCheckBox, () => { _sendToFemiOnlyCheckBox.Enabled = enable; });
            SafeControlUpdate(_skipEmailCheckBox, () => { _skipEmailCheckBox.Enabled = enable; });
        }

        /// <summary>
        /// Resets the state of the main action buttons when the report type is changed by the user.
        /// Also resets the visibility of the "View" buttons.
        /// </summary>
        /// <param name="configValid">Indicates if the essential configuration is valid.</param>
        public void ResetButtonStatesAfterTypeChange(bool configValid)
        {
            if (_parentForm == null) return;
            SafeControlUpdate(_parentForm, () =>
            {
                if (_createReportButton != null) { _createReportButton.Text = configValid ? "Create Report" : "Config Error"; _createReportButton.Enabled = configValid; }
                if (_processEmailButton != null) { _processEmailButton.Text = "Process & Email"; _processEmailButton.Enabled = false; }
                if (_oneClickProcessButton != null) { _oneClickProcessButton.Text = configValid ? "Generate, Process & Email Report" : "Config Error"; _oneClickProcessButton.Enabled = configValid; }
                ShowViewReportButton(false);
                ShowViewAnalysisButton(false);
            });
        }

        /// <summary>
        /// Resets the UI to an initial or error state after an operation completes or fails.
        /// Re-enables controls and updates button states based on the context.
        /// </summary>
        /// <param name="button1Text">The text to set for the main action button (e.g., "Create Report").</param>
        /// <param name="configValid">Whether the essential configuration is valid.</param>
        /// <param name="rawReportExists">Whether a raw report file currently exists from a previous step.</param>
        /// <param name="analysisExists">Whether a final analysis file currently exists.</param>
        /// <param name="isDailySelected">Whether a daily report type is currently selected.</param>
        /// <param name="isTimerEnabled">The current state of the auto-run timer.</param>
        /// <param name="isFinalStatusForToday">Whether the auto-run process has reached a final state for the day (completed or failed).</param>
        /// <param name="currentAutoRunStatusText">The current text of the auto-run status label.</param>
        public void ResetUIOnError(string button1Text, bool configValid, bool rawReportExists, bool analysisExists, bool isDailySelected, bool isTimerEnabled, bool isFinalStatusForToday, string currentAutoRunStatusText)
        {
            SafeControlUpdate(_parentForm, () =>
            {
                if (_createReportButton != null) { _createReportButton.Text = configValid ? button1Text : "Config Error"; _createReportButton.Enabled = configValid; }
                if (_processEmailButton != null) { _processEmailButton.Text = "Process & Email"; _processEmailButton.Enabled = rawReportExists; }
                if (_oneClickProcessButton != null) _oneClickProcessButton.Enabled = configValid;
                if (_toggleAutoRunButton != null) _toggleAutoRunButton.Enabled = true;

                SetOtherControlsEnabled(true, _financialYearComboBox?.Visible ?? false);
                ShowViewReportButton(rawReportExists, _viewReportButton?.Tag?.ToString());
                ShowViewAnalysisButton(analysisExists, _viewAnalysisButton?.Tag?.ToString());

                if (_toggleAutoRunButton != null && _autoRunStatusLabel != null)
                {
                    UpdateAutoRunUI(isTimerEnabled, isFinalStatusForToday, currentAutoRunStatusText);
                }
            });
        }

        /// <summary>
        /// Shows or hides the "View Analysis" button and sets its associated file path in the Tag property.
        /// </summary>
        /// <param name="show">True to show the button, false to hide it.</param>
        /// <param name="filePath">The file path to store in the button's Tag property.</param>
        public void ShowViewAnalysisButton(bool show, string? filePath = null)
        {
            if (_viewAnalysisButton != null)
            {
                SafeControlUpdate(_viewAnalysisButton, () =>
                {
                    _viewAnalysisButton.Visible = show;
                    _viewAnalysisButton.Enabled = show;
                    _viewAnalysisButton.Tag = filePath;
                });
            }
        }

        /// <summary>
        /// Shows or hides the "View Report" button and sets its associated file path in the Tag property.
        /// </summary>
        /// <param name="show">True to show the button, false to hide it.</param>
        /// <param name="filePath">The file path to store in the button's Tag property.</param>
        public void ShowViewReportButton(bool show, string? filePath = null)
        {
            if (_viewReportButton != null)
            {
                SafeControlUpdate(_viewReportButton, () =>
                {
                    _viewReportButton.Visible = show;
                    _viewReportButton.Enabled = show;
                    _viewReportButton.Tag = filePath;
                });
            }
        }
        #endregion

        #region Auto Run UI Management
        /// <summary>
        /// Sets the current auto-run hour for UI display purposes.
        /// </summary>
        /// <param name="hour">The hour (0-23) to display.</param>
        public void SetAutoRunHour(int hour)
        {
            if (hour >= 0 && hour <= 23)
            {
                _currentAutoRunHour = hour;
            }
        }

        /// <summary>
        /// Updates the UI elements related to the auto-run feature, including the toggle button's text and colour,
        /// and the status label's text and colour.
        /// </summary>
        /// <param name="isTimerEnabled">The current enabled state of the auto-run timer.</param>
        /// <param name="isFinalStatusForToday">Indicates if the auto-run job is finished for the day (completed or failed).</param>
        /// <param name="statusText">The specific status text to display. If empty, a default message is constructed.</param>
        public void UpdateAutoRunUI(bool isTimerEnabled, bool isFinalStatusForToday, string statusText = "")
        {
            if (_toggleAutoRunButton == null || _autoRunStatusLabel == null || _toolTip == null) return;

            ThemePalette palette = ThemeSettings.CurrentPalette;

            // Update the toggle button's text and colour.
            SafeControlUpdate(_toggleAutoRunButton, () =>
            {
                _toggleAutoRunButton.Text = isTimerEnabled ? $"Disable Daily Auto Run @ {_currentAutoRunHour}:00" : $"Enable Daily Auto Run @ {_currentAutoRunHour}:00";
                _toggleAutoRunButton.BackColor = isTimerEnabled ? palette.AutoRunEnabledButtonBackColor : palette.AutoRunDisabledButtonBackColor;
                _toggleAutoRunButton.ForeColor = palette.AutoRunButtonForeColor;
                _toolTip.SetToolTip(_toggleAutoRunButton, $"Enable or disable the automated daily report generation. The report runs around {_currentAutoRunHour}:00 for the previous workday.");
            });

            // Update the auto-run status label's text and colour.
            SafeToolStripItemUpdate(_autoRunStatusLabel, () =>
            {
                string textToShow = statusText;
                if (string.IsNullOrEmpty(textToShow))
                {
                    textToShow = isTimerEnabled
                        ? (isFinalStatusForToday ? GetAutoRunStatusLabelText() ?? $"Auto Run: Enabled (Next check ~{_currentAutoRunHour}:00)" : $"Auto Run: Enabled (Next check ~{_currentAutoRunHour}:00)")
                        : (isFinalStatusForToday ? GetAutoRunStatusLabelText() ?? "Auto Run: Disabled" : "Auto Run: Disabled");
                }
                _autoRunStatusLabel.Text = textToShow;

                if (textToShow.Contains("FAILED") || textToShow.Contains("ERROR"))
                    _autoRunStatusLabel.ForeColor = palette.ErrorStatusColor;
                else if (isTimerEnabled && !isFinalStatusForToday)
                    _autoRunStatusLabel.ForeColor = palette.SuccessStatusColor;
                else
                    _autoRunStatusLabel.ForeColor = palette.StatusStripForeColor;
            });
        }

        /// <summary>
        /// Disables primary UI controls during an automated report execution to prevent user interference.
        /// </summary>
        public void DisableControlsForAutoRun()
        {
            SetActionButtonsEnabled(false);
            SetOtherControlsEnabled(false, _financialYearComboBox?.Visible ?? false);
            SafeControlUpdate(_toggleAutoRunButton, () => _toggleAutoRunButton.Enabled = false);
            SafeControlUpdate(_viewReportButton, () => _viewReportButton.Enabled = false);
            SafeControlUpdate(_viewAnalysisButton, () => _viewAnalysisButton.Enabled = false);
        }
        #endregion

        #region Safe UI Update Utilities
        /// <summary>
        /// Safely updates a standard <see cref="Control"/> by marshalling the call to the UI thread if necessary.
        /// This prevents cross-thread operation exceptions.
        /// </summary>
        /// <param name="ctrl">The control to update.</param>
        /// <param name="action">The action to perform on the control.</param>
        public static void SafeControlUpdate(Control ctrl, Action action)
        {
            ArgumentNullException.ThrowIfNull(action);
            if (ctrl == null || ctrl.IsDisposed) return;

            if (ctrl.IsHandleCreated && !ctrl.Disposing)
            {
                if (ctrl.InvokeRequired)
                {
                    try { ctrl.BeginInvoke(action); }
                    catch (Exception ex) { Logger.LogError($"Error during SafeControlUpdate Invoke/BeginInvoke: {ex}"); }
                }
                else
                {
                    try { action(); }
                    catch (Exception ex) { Logger.LogError($"Error during SafeControlUpdate direct action: {ex}"); }
                }
            }
        }

        /// <summary>
        /// Safely updates a <see cref="ToolStripItem"/> by marshalling the call to the UI thread of its owner <see cref="ToolStrip"/>.
        /// </summary>
        /// <param name="item">The ToolStripItem to update.</param>
        /// <param name="action">The action to perform on the item.</param>
        public static void SafeToolStripItemUpdate(ToolStripItem item, Action action)
        {
            ArgumentNullException.ThrowIfNull(action);
            if (item == null || item.IsDisposed) return;

            ToolStrip? owner = item.Owner;
            if (owner != null && owner.IsHandleCreated && !owner.IsDisposed && !owner.Disposing)
            {
                if (owner.InvokeRequired)
                {
                    try { owner.BeginInvoke(action); }
                    catch (Exception ex) { Logger.LogError($"Error during SafeToolStripItemUpdate Invoke/BeginInvoke: {ex}"); }
                }
                else
                {
                    try { action(); }
                    catch (Exception ex) { Logger.LogError($"Error during SafeToolStripItemUpdate direct action: {ex}"); }
                }
            }
        }
        #endregion

        #region Windows API Helpers for Theming
        /// <summary>
        /// A private static helper method that uses P/Invoke to set the dark mode attribute on a window's title bar.
        /// </summary>
        /// <param name="handle">The window handle (HWND).</param>
        /// <param name="enabled">True to enable dark mode, false to disable it.</param>
        /// <returns>True if the attribute was set successfully; otherwise, false.</returns>
        private static bool UseImmersiveDarkModeInternal(IntPtr handle, bool enabled)
        {
            if (handle == IntPtr.Zero) return false;

            int attribute;
            Version osVersion = Environment.OSVersion.Version;

            // Use the appropriate DWM attribute based on the Windows 10/11 build number.
            if (osVersion.Major >= 10 && osVersion.Build >= 19041) // Windows 10 2004+ / Windows 11
            {
                attribute = DWMWA_USE_IMMERSIVE_DARK_MODE;
            }
            else if (osVersion.Major >= 10 && osVersion.Build >= 18362) // Windows 10 1903
            {
                attribute = DWMWA_USE_IMMERSIVE_DARK_MODE_WINDOWS_10_1903;
            }
            else
            {
                // Dark mode title bar is not supported on older versions of Windows.
                return false;
            }

            int useImmersiveDarkMode = enabled ? 1 : 0;
            return DwmSetWindowAttribute(handle, attribute, ref useImmersiveDarkMode, sizeof(int)) == 0;
        }
        #endregion
    }
}