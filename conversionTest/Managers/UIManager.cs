// UIManager.cs
// This is the complete, corrected version of the UIManager class.
// It restores the missing private helper method 'UseImmersiveDarkModeInternal'
// to resolve compilation errors.

#region Using Directives
// System related namespaces
using System;
using System.Drawing;
using System.IO;
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
    /// It also provides static methods for theming other forms.
    /// </summary>
    public class UIManager
    {
        #region Fields and Control References
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
        private int _currentAutoRunHour = 8;
        #endregion

        #region P/Invoke Declarations and Constants
        // This region contains P/Invoke declarations for interacting with Windows APIs,
        // primarily for custom window theming (title bar).
        [DllImport("dwmapi.dll", PreserveSig = true)]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);
        private const int DWMWA_USE_IMMERSIVE_DARK_MODE_WINDOWS_10_1903 = 19;
        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

        [DllImport("user32.dll")]
        private static extern bool RedrawWindow(IntPtr hWnd, IntPtr lprcUpdate, IntPtr hrgnUpdate, uint flags);
        private const uint RDW_INVALIDATE = 0x0001;
        private const uint RDW_ERASE = 0x0004;
        private const uint RDW_UPDATENOW = 0x0100;
        private const uint RDW_ERASENOW = 0x0200;
        private const uint RDW_FRAME = 0x0400;
        #endregion

        #region Constructor
        /// <summary>
        /// Initializes a new instance of the UIManager class for managing the main form's UI.
        /// </summary>
        public UIManager(
            Form parentForm, MenuStrip menuStrip, StatusStrip statusStrip,
            ToolStripStatusLabel autoRunStatusLabel,
            ToolStripMenuItem darkModeMenuItem, Button createReportButton, Button processEmailButton,
            Button oneClickProcessButton, Button toggleAutoRunButton, Button viewReportButton,
            Button viewAnalysisButton, ComboBox reportTypeComboBox, DateTimePicker startDatePicker,
            DateTimePicker endDatePicker, ComboBox financialYearComboBox, Label financialYearLabel,
            CheckBox sendToFemiOnlyCheckBox, CheckBox skipEmailCheckBox, Label emailRecipientLabel, ToolTip toolTip)
        {
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
            bool isCurrentlyDark = ThemeSettings.IsCurrentlyDark();
            ThemePalette palette = ThemeSettings.CurrentPalette;

            SafeControlUpdate(_parentForm, () =>
            {
                _parentForm.BackColor = palette.FormBackColor;
                _parentForm.ForeColor = palette.FormForeColor;

                if (UseImmersiveDarkModeInternal(_parentForm.Handle, isCurrentlyDark))
                {
                    RedrawWindow(_parentForm.Handle, IntPtr.Zero, IntPtr.Zero, RDW_FRAME | RDW_INVALIDATE | RDW_UPDATENOW);
                }

                UpdateControlThemeRecursive(_parentForm, palette, isCurrentlyDark);

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
                _parentForm.Refresh();
            });

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
        /// This method centralizes the P/Invoke calls for theming any Form and uses the new ThemeSettings palettes.
        /// It should be called by other forms (e.g., Settings, Help) in their Load event.
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

            Logger.LogDebug($"ApplyThemeToExternalForm: Applying FRAME theme to '{formToTheme.Name}'. DarkMode: {isDarkModeEnabled}");

            // Select the correct palette based on the parameter.
            ThemePalette palette = isDarkModeEnabled ? ThemeSettings.DarkPalette : ThemeSettings.LightPalette;

            // Apply basic form colors directly from the selected palette
            formToTheme.BackColor = palette.FormBackColor;
            formToTheme.ForeColor = palette.FormForeColor;

            // Apply title bar and frame theme
            if (UseImmersiveDarkModeInternal(formToTheme.Handle, isDarkModeEnabled))
            {
                RedrawWindow(formToTheme.Handle, IntPtr.Zero, IntPtr.Zero, RDW_FRAME | RDW_INVALIDATE | RDW_UPDATENOW | RDW_ERASENOW);
            }
        }

        /// <summary>
        /// Recursively applies theme colors to a control and its child controls using a <see cref="ThemePalette"/>.
        /// </summary>
        private void UpdateControlThemeRecursive(Control parentControl, ThemePalette palette, bool isCurrentlyDark)
        {
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
        /// Updates the text of the auto-run status label on the status strip.
        /// </summary>
        public void UpdateStatusRight(string message) { if (_autoRunStatusLabel != null) SafeToolStripItemUpdate(_autoRunStatusLabel, () => { _autoRunStatusLabel.Text = message; }); }

        /// <summary>
        /// Gets the current text of the auto-run status label. This method is thread-safe.
        /// </summary>
        public string GetAutoRunStatusLabelText() { if (_autoRunStatusLabel == null) return string.Empty; ToolStrip? owner = _autoRunStatusLabel.Owner; if (owner != null && owner.IsHandleCreated && !owner.IsDisposed && !owner.Disposing) { if (owner.InvokeRequired) { try { return (string)owner.Invoke(new Func<string>(() => _autoRunStatusLabel.Text ?? string.Empty)); } catch (Exception ex) { Logger.LogError($"Error during sync fetch of AutoRunStatusLabel text: {ex}"); return string.Empty; } } else { return _autoRunStatusLabel.Text ?? string.Empty; } } return string.Empty; }
        
        /// <summary>
        /// Enables or disables the main action buttons.
        /// </summary>
        public void SetActionButtonsEnabled(bool enable) { SafeControlUpdate(_createReportButton, () => { _createReportButton.Enabled = enable; }); SafeControlUpdate(_processEmailButton, () => { _processEmailButton.Enabled = enable; }); SafeControlUpdate(_oneClickProcessButton, () => { _oneClickProcessButton.Enabled = enable; }); }

        /// <summary>
        /// Enables or disables other input controls on the form.
        /// </summary>
        public void SetOtherControlsEnabled(bool enable, bool isFinancialYearVisible) { SafeControlUpdate(_reportTypeComboBox, () => { _reportTypeComboBox.Enabled = enable; }); SafeControlUpdate(_startDatePicker, () => { _startDatePicker.Enabled = enable; }); SafeControlUpdate(_endDatePicker, () => { _endDatePicker.Enabled = enable; }); SafeControlUpdate(_financialYearComboBox, () => { _financialYearComboBox.Enabled = enable && isFinancialYearVisible; }); SafeControlUpdate(_sendToFemiOnlyCheckBox, () => { _sendToFemiOnlyCheckBox.Enabled = enable; }); SafeControlUpdate(_skipEmailCheckBox, () => { _skipEmailCheckBox.Enabled = enable; }); }

        /// <summary>
        /// Resets the state of the main action buttons when the report type is changed by the user.
        /// </summary>
        public void ResetButtonStatesAfterTypeChange(bool configValid)
        {
            if (_parentForm == null) return;
            SafeControlUpdate(_parentForm, () =>
            {
                if (_createReportButton != null) { _createReportButton.Text = configValid ? "Create Report" : "Config Error"; _createReportButton.Enabled = configValid; }
                if (_processEmailButton != null) { _processEmailButton.Text = "Process && Email"; _processEmailButton.Enabled = false; }
                if (_oneClickProcessButton != null) { _oneClickProcessButton.Text = configValid ? "Generate, Process & Email Report" : "Config Error"; _oneClickProcessButton.Enabled = configValid; }
                ShowViewReportButton(false);
                ShowViewAnalysisButton(false);
            });
        }

        /// <summary>
        /// Resets the UI to an initial or error state after an operation.
        /// </summary>
        public void ResetUIOnError(string button1Text, bool configValid, bool rawReportExists, bool analysisExists, bool isDailySelected, bool isTimerEnabled, bool isFinalStatusForToday, string currentAutoRunStatusText) { SafeControlUpdate(_parentForm, () => { if (_createReportButton != null) { _createReportButton.Text = configValid ? button1Text : "Config Error"; _createReportButton.Enabled = configValid; } if (_processEmailButton != null) { _processEmailButton.Text = "Process && Email"; _processEmailButton.Enabled = rawReportExists; } if (_oneClickProcessButton != null) _oneClickProcessButton.Enabled = configValid; if (_toggleAutoRunButton != null) _toggleAutoRunButton.Enabled = true; SetOtherControlsEnabled(true, _financialYearComboBox?.Visible ?? false); ShowViewReportButton(rawReportExists, _viewReportButton?.Tag?.ToString()); ShowViewAnalysisButton(analysisExists, _viewAnalysisButton?.Tag?.ToString()); if (_toggleAutoRunButton != null && _autoRunStatusLabel != null) { UpdateAutoRunUI(isTimerEnabled, isFinalStatusForToday, currentAutoRunStatusText); } }); }
        
        /// <summary>
        /// Shows or hides the "View Analysis" button and sets its file path.
        /// </summary>
        public void ShowViewAnalysisButton(bool show, string? filePath = null) { if (_viewAnalysisButton != null) SafeControlUpdate(_viewAnalysisButton, () => { _viewAnalysisButton.Visible = show; _viewAnalysisButton.Enabled = show; _viewAnalysisButton.Tag = filePath; }); }
        
        /// <summary>
        /// Shows or hides the "View Report" button and sets its file path.
        /// </summary>
        public void ShowViewReportButton(bool show, string? filePath = null) { if (_viewReportButton != null) SafeControlUpdate(_viewReportButton, () => { _viewReportButton.Visible = show; _viewReportButton.Enabled = show; _viewReportButton.Tag = filePath; }); }
        #endregion

        #region Auto Run UI Management
        /// <summary>
        /// Sets the current auto-run hour for UI display purposes.
        /// </summary>
        public void SetAutoRunHour(int hour) { if (hour >= 0 && hour <= 23) _currentAutoRunHour = hour; }

        /// <summary>
        /// Updates the UI elements related to the auto-run feature.
        /// </summary>
        public void UpdateAutoRunUI(bool isTimerEnabled, bool isFinalStatusForToday, string statusText = "") { if (_toggleAutoRunButton == null || _autoRunStatusLabel == null || _toolTip == null) return; ThemePalette palette = ThemeSettings.CurrentPalette; SafeControlUpdate(_toggleAutoRunButton, () => { _toggleAutoRunButton.Text = isTimerEnabled ? $"Disable Daily Auto Run @ {_currentAutoRunHour}:00" : $"Enable Daily Auto Run @ {_currentAutoRunHour}:00"; _toggleAutoRunButton.BackColor = isTimerEnabled ? palette.AutoRunEnabledButtonBackColor : palette.AutoRunDisabledButtonBackColor; _toggleAutoRunButton.ForeColor = palette.AutoRunButtonForeColor; _toolTip.SetToolTip(_toggleAutoRunButton, $"Enable or disable the automated daily report generation. The report runs around {_currentAutoRunHour}:00 for the previous workday."); }); SafeToolStripItemUpdate(_autoRunStatusLabel, () => { string textToShow = statusText; if (string.IsNullOrEmpty(textToShow)) { textToShow = isTimerEnabled ? (isFinalStatusForToday ? GetAutoRunStatusLabelText() ?? $"Auto Run: Enabled (Next check ~{_currentAutoRunHour}:00)" : $"Auto Run: Enabled (Next check ~{_currentAutoRunHour}:00)") : (isFinalStatusForToday ? GetAutoRunStatusLabelText() ?? "Auto Run: Disabled" : "Auto Run: Disabled"); } _autoRunStatusLabel.Text = textToShow; if (textToShow.Contains("FAILED") || textToShow.Contains("ERROR")) _autoRunStatusLabel.ForeColor = palette.ErrorStatusColor; else if (isTimerEnabled && !isFinalStatusForToday) _autoRunStatusLabel.ForeColor = palette.SuccessStatusColor; else _autoRunStatusLabel.ForeColor = palette.StatusStripForeColor; }); }
        
        /// <summary>
        /// Disables primary UI controls during an automated report execution.
        /// </summary>
        public void DisableControlsForAutoRun() { SetActionButtonsEnabled(false); SetOtherControlsEnabled(false, _financialYearComboBox?.Visible ?? false); SafeControlUpdate(_toggleAutoRunButton, () => _toggleAutoRunButton.Enabled = false); SafeControlUpdate(_viewReportButton, () => _viewReportButton.Enabled = false); SafeControlUpdate(_viewAnalysisButton, () => _viewAnalysisButton.Enabled = false); }
        #endregion
        
        #region Safe UI Update Utilities
        /// <summary>
        /// Safely updates a standard <see cref="Control"/> by marshalling the call to the UI thread if necessary.
        /// </summary>
        public static void SafeControlUpdate(Control ctrl, Action action) { ArgumentNullException.ThrowIfNull(action); if (ctrl == null || ctrl.IsDisposed) return; if (ctrl.IsHandleCreated && !ctrl.Disposing) { if (ctrl.InvokeRequired) { try { ctrl.BeginInvoke(action); } catch (Exception ex) { Logger.LogError($"Error during SafeControlUpdate Invoke/BeginInvoke: {ex}"); } } else { try { action(); } catch (Exception ex) { Logger.LogError($"Error during SafeControlUpdate direct action: {ex}"); } } } }

        /// <summary>
        /// Safely updates a <see cref="ToolStripItem"/> by marshalling the call to the UI thread of its owner if necessary.
        /// </summary>
        public static void SafeToolStripItemUpdate(ToolStripItem item, Action action) { ArgumentNullException.ThrowIfNull(action); if (item == null || item.IsDisposed) return; ToolStrip? owner = item.Owner; if (owner != null && owner.IsHandleCreated && !owner.IsDisposed && !owner.Disposing) { if (owner.InvokeRequired) { try { owner.BeginInvoke(action); } catch (Exception ex) { Logger.LogError($"Error during SafeToolStripItemUpdate Invoke/BeginInvoke: {ex}"); } } else { try { action(); } catch (Exception ex) { Logger.LogError($"Error during SafeToolStripItemUpdate direct action: {ex}"); } } } }
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
            if (osVersion.Major >= 10 && osVersion.Build >= 19041)
            {
                attribute = DWMWA_USE_IMMERSIVE_DARK_MODE;
            }
            else if (osVersion.Major >= 10 && osVersion.Build >= 18362)
            {
                attribute = DWMWA_USE_IMMERSIVE_DARK_MODE_WINDOWS_10_1903;
            }
            else
            {
                return false;
            }
            int useImmersiveDarkMode = enabled ? 1 : 0;
            return DwmSetWindowAttribute(handle, attribute, ref useImmersiveDarkMode, sizeof(int)) == 0;
        }
        #endregion
    }
}