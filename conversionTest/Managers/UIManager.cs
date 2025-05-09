// C# 10+ Features
using conversionTest; // Assuming Logger is in this namespace or globally available

namespace QuoteConversionReportAutomation.Managers
{
    // --- Using Statements ---
    using System;
    using System.Drawing;
    using System.Windows.Forms;
    using Microsoft.Win32; // For Registry access
    using System.Threading.Tasks; // For Task.Delay

    #region Custom Menu Renderer for Dark Mode 

    /// <summary> 
    /// Custom renderer to handle dark mode menu item highlighting and appearance. 
    /// </summary> 
    public class DarkModeMenuRenderer : ToolStripProfessionalRenderer
    {
        private static readonly Color _staticMenuItemHoverColor = Color.FromArgb(85, 85, 95);
        private static readonly Color _staticMenuBorderColor = Color.FromArgb(85, 85, 90);
        private readonly Color _instanceMenuForeColor = Color.FromArgb(220, 220, 220);
        private static readonly Color _staticMenuBackColor = Color.FromArgb(45, 45, 48);

        public DarkModeMenuRenderer() : base(new DarkModeColorTable(_staticMenuItemHoverColor, Color.FromArgb(100, 100, 110), _staticMenuBorderColor, _staticMenuBackColor)) { }

        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            if (e.Item != null)
            {
                e.TextColor = _instanceMenuForeColor;
            }
            base.OnRenderItemText(e);
        }

        protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
        {
            if (e.Item == null) return;

            if (!e.Item.Enabled)
            {
                using (SolidBrush brush = new SolidBrush(_staticMenuBackColor))
                {
                    e.Graphics.FillRectangle(brush, new Rectangle(Point.Empty, e.Item.Size));
                }
                if (!string.IsNullOrEmpty(e.Item.Text))
                {
                    TextRenderer.DrawText(e.Graphics, e.Item.Text, e.Item.Font, e.Item.ContentRectangle, SystemColors.GrayText, TextFormatFlags.VerticalCenter | TextFormatFlags.HorizontalCenter);
                }
                return;
            }

            Rectangle rc = new Rectangle(Point.Empty, e.Item.Size);
            if (e.Item.Selected || e.Item is ToolStripMenuItem tsmi && tsmi.DropDown.Visible && tsmi.IsOnDropDown == false)
            {
                using (SolidBrush brush = new SolidBrush(_staticMenuItemHoverColor))
                {
                    e.Graphics.FillRectangle(brush, rc);
                }
            }
            else
            {
                using (SolidBrush brush = new SolidBrush(e.Item.BackColor))
                {
                    e.Graphics.FillRectangle(brush, rc);
                }
            }
        }
        protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
        {
            if (e.ToolStrip is ToolStripDropDown)
            {
                using (SolidBrush brush = new SolidBrush(_staticMenuBackColor))
                {
                    e.Graphics.FillRectangle(brush, e.AffectedBounds);
                }
            }
            else
            {
                base.OnRenderToolStripBackground(e);
            }
        }
        protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
        {
            if (e.ToolStrip is ToolStripDropDown)
            {
                using (Pen pen = new Pen(_staticMenuBorderColor))
                {
                    e.Graphics.DrawRectangle(pen, new Rectangle(0, 0, e.AffectedBounds.Width - 1, e.AffectedBounds.Height - 1));
                }
            }
        }
    }

    /// <summary> 
    /// Custom ColorTable to define specific colors for ToolStripProfessionalRenderer in dark mode.
    /// </summary> 
    public class DarkModeColorTable : ProfessionalColorTable
    {
        private readonly Color _hoverColor;
        private readonly Color _pressedColor;
        private readonly Color _borderColor;
        private readonly Color _menuBackColor;
        private readonly Color _statusStripBackColor;
        public DarkModeColorTable(Color hover, Color pressed, Color border, Color menuBack)
        {
            _hoverColor = hover;
            _pressedColor = pressed;
            _borderColor = border;
            _menuBackColor = menuBack;
            _statusStripBackColor = menuBack;
        }
        public override Color MenuItemSelected => _hoverColor;
        public override Color MenuItemSelectedGradientBegin => _hoverColor;
        public override Color MenuItemSelectedGradientEnd => _hoverColor;
        public override Color MenuItemPressedGradientBegin => _pressedColor;
        public override Color MenuItemPressedGradientEnd => _pressedColor;
        public override Color MenuItemBorder => _borderColor;
        public override Color MenuBorder => _borderColor;
        public override Color ToolStripDropDownBackground => _menuBackColor;
        public override Color ImageMarginGradientBegin => _menuBackColor;
        public override Color ImageMarginGradientMiddle => _menuBackColor;
        public override Color ImageMarginGradientEnd => _menuBackColor;
        public override Color SeparatorDark => _borderColor;
        public override Color SeparatorLight => Color.Transparent;
        public override Color StatusStripGradientBegin => _statusStripBackColor;
        public override Color StatusStripGradientEnd => _statusStripBackColor;
        public override Color MenuStripGradientBegin => _menuBackColor;
        public override Color MenuStripGradientEnd => _menuBackColor;
    }
    #endregion

    /// <summary>
    /// Manages UI updates, theme application, and control state for Form1.
    /// ProgressBar functionality has been removed from this version.
    /// Now aware of 1-Click processing button and Skip Email checkbox.
    /// </summary>
    public class UIManager
    {
        #region Fields and Controls References
        // --- Control References ---
        private readonly Form _parentForm;
        private readonly MenuStrip _menuStrip;
        private readonly StatusStrip _statusStrip;
        private readonly ToolStripStatusLabel _statusLabel;
        private readonly ToolStripStatusLabel _autoRunStatusLabel;
        private readonly ToolStripMenuItem _darkModeMenuItem;
        private readonly Button _createReportButton;
        private readonly Button _processEmailButton;
        private readonly Button _generateAndSendButton; // Original combined button
        private readonly Button _oneClickProcessButton; // New 1-Click button
        private readonly Button _toggleAutoRunButton;
        private readonly Button _viewReportButton;
        private readonly Button _viewAnalysisButton;
        private readonly ComboBox _reportTypeComboBox;
        private readonly DateTimePicker _startDatePicker;
        private readonly DateTimePicker _endDatePicker;
        private readonly ComboBox _financialYearComboBox;
        private readonly Label _financialYearLabel;
        private readonly CheckBox _sendToFemiOnlyCheckBox;
        private readonly CheckBox _skipEmailCheckBox; // New checkbox
        private readonly Label _emailRecipientLabel;
        private readonly ToolTip _toolTip;

        // --- State ---
        private bool _isDarkMode = false;
        private DarkModeMenuRenderer? _darkModeRenderer;

        // --- Theme Colors ---
        private static readonly Color DM_BackColor = Color.FromArgb(45, 45, 48);
        private static readonly Color DM_ForeColor = Color.White;
        private static readonly Color DM_ControlBackColor = Color.FromArgb(60, 60, 63);
        private static readonly Color DM_ButtonBackColor = Color.FromArgb(80, 80, 80);
        private static readonly Color DM_MenuBackColor = DM_BackColor;
        private static readonly Color DM_StatusStripBackColor = DM_BackColor;
        private static readonly Color DM_MenuForeColor = Color.FromArgb(220, 220, 220);

        private static readonly Color LM_BackColor = SystemColors.Control;
        private static readonly Color LM_ForeColor = SystemColors.ControlText;
        private static readonly Color LM_ControlBackColor = SystemColors.Window;
        private static readonly Color LM_ButtonBackColor = SystemColors.Control;
        private static readonly Color LM_MenuBackColor = SystemColors.Control;
        private static readonly Color LM_StatusStripBackColor = SystemColors.Control;

        private static readonly Color AutoRunEnabledColor = Color.LightGreen;
        private static readonly Color AutoRunDisabledColor = Color.LightCoral;
        private static readonly Color AutoRunButtonForeColor = Color.Black;
        #endregion

        #region Constructor
        /// <summary>
        /// Initializes a new instance of the UIManager class.
        /// Now includes oneClickProcessButton and skipEmailCheckBox.
        /// </summary>
        public UIManager(
            Form parentForm, MenuStrip menuStrip, StatusStrip statusStrip,
            ToolStripStatusLabel statusLabel, ToolStripStatusLabel autoRunStatusLabel,
            ToolStripMenuItem darkModeMenuItem, Button createReportButton, Button processEmailButton,
            Button generateAndSendButton, // Original combined button
            Button oneClickProcessButton, // New 1-Click button
            Button toggleAutoRunButton, Button viewReportButton, Button viewAnalysisButton,
            ComboBox reportTypeComboBox, DateTimePicker startDatePicker, DateTimePicker endDatePicker,
            ComboBox financialYearComboBox, Label financialYearLabel, CheckBox sendToFemiOnlyCheckBox,
            CheckBox skipEmailCheckBox, // New checkbox
            Label emailRecipientLabel, ToolTip toolTip
            )
        {
            _parentForm = parentForm ?? throw new ArgumentNullException(nameof(parentForm));
            _menuStrip = menuStrip ?? throw new ArgumentNullException(nameof(menuStrip));
            _statusStrip = statusStrip ?? throw new ArgumentNullException(nameof(statusStrip));
            _statusLabel = statusLabel ?? throw new ArgumentNullException(nameof(statusLabel));
            _autoRunStatusLabel = autoRunStatusLabel ?? throw new ArgumentNullException(nameof(autoRunStatusLabel));
            _darkModeMenuItem = darkModeMenuItem ?? throw new ArgumentNullException(nameof(darkModeMenuItem));
            _createReportButton = createReportButton ?? throw new ArgumentNullException(nameof(createReportButton));
            _processEmailButton = processEmailButton ?? throw new ArgumentNullException(nameof(processEmailButton));
            _generateAndSendButton = generateAndSendButton ?? throw new ArgumentNullException(nameof(generateAndSendButton));
            _oneClickProcessButton = oneClickProcessButton ?? throw new ArgumentNullException(nameof(oneClickProcessButton)); // Store new button
            _toggleAutoRunButton = toggleAutoRunButton ?? throw new ArgumentNullException(nameof(toggleAutoRunButton));
            _viewReportButton = viewReportButton ?? throw new ArgumentNullException(nameof(viewReportButton));
            _viewAnalysisButton = viewAnalysisButton ?? throw new ArgumentNullException(nameof(viewAnalysisButton));
            _reportTypeComboBox = reportTypeComboBox ?? throw new ArgumentNullException(nameof(reportTypeComboBox));
            _startDatePicker = startDatePicker ?? throw new ArgumentNullException(nameof(startDatePicker));
            _endDatePicker = endDatePicker ?? throw new ArgumentNullException(nameof(endDatePicker));
            _financialYearComboBox = financialYearComboBox ?? throw new ArgumentNullException(nameof(financialYearComboBox));
            _financialYearLabel = financialYearLabel ?? throw new ArgumentNullException(nameof(financialYearLabel));
            _sendToFemiOnlyCheckBox = sendToFemiOnlyCheckBox ?? throw new ArgumentNullException(nameof(sendToFemiOnlyCheckBox));
            _skipEmailCheckBox = skipEmailCheckBox ?? throw new ArgumentNullException(nameof(skipEmailCheckBox)); // Store new checkbox
            _emailRecipientLabel = emailRecipientLabel ?? throw new ArgumentNullException(nameof(emailRecipientLabel));
            _toolTip = toolTip ?? throw new ArgumentNullException(nameof(toolTip));
        }
        #endregion

        #region Theme Management
        /// <summary>
        /// Applies the selected theme (Dark or Light) to the form and its relevant controls.
        /// </summary>
        /// <param name="isDarkMode">True to apply dark mode, false to apply light mode.</param>
        public void ApplyTheme(bool isDarkMode)
        {
            _isDarkMode = isDarkMode;

            Color backColor = isDarkMode ? DM_BackColor : LM_BackColor;
            Color foreColor = isDarkMode ? DM_ForeColor : LM_ForeColor;
            Color controlBackColor = isDarkMode ? DM_ControlBackColor : LM_ControlBackColor;
            Color buttonBackColor = isDarkMode ? DM_ButtonBackColor : LM_ButtonBackColor; // Used for all buttons including oneClick
            Color menuBackColor = isDarkMode ? DM_MenuBackColor : LM_MenuBackColor;
            Color menuForeColor = isDarkMode ? DM_MenuForeColor : LM_ForeColor;
            Color statusStripBackColor = isDarkMode ? DM_StatusStripBackColor : LM_StatusStripBackColor;

            SafeControlUpdate(_parentForm, () => {
                UpdateControlThemeRecursive(_parentForm, backColor, foreColor, controlBackColor, buttonBackColor);

                _menuStrip.BackColor = menuBackColor;
                _menuStrip.ForeColor = menuForeColor;

                _statusStrip.BackColor = statusStripBackColor;
                _statusStrip.ForeColor = foreColor;
                _statusLabel.ForeColor = foreColor;
                // _mainProgressBar.BackColor line removed

                if (isDarkMode)
                {
                    _darkModeRenderer ??= new DarkModeMenuRenderer();
                    _menuStrip.Renderer = _darkModeRenderer;
                }
                else
                {
                    _menuStrip.Renderer = new ToolStripProfessionalRenderer(new ProfessionalColorTable());
                }
                UpdateMenuItemsTheme(_menuStrip.Items, menuBackColor, menuForeColor);
            });

            bool isTimerEnabled = _toggleAutoRunButton.Text.StartsWith("Disable"); // Or check timer.Enabled directly if accessible
            bool isAutoRunStatusFinal = _autoRunStatusLabel.Text.Contains("Completed") || _autoRunStatusLabel.Text.Contains("FAILED") || _autoRunStatusLabel.Text.Contains("Done for");
            UpdateAutoRunUI(isTimerEnabled, isAutoRunStatusFinal, isDarkMode, _autoRunStatusLabel.Text);

            Logger.LogInfo($"Theme applied: {(isDarkMode ? "Dark Mode" : "Light Mode")}");
        }

        /// <summary>
        /// Recursive helper to apply theme colors to controls.
        /// Buttons (including _oneClickProcessButton) and CheckBoxes (including _skipEmailCheckBox)
        /// are handled by their respective type checks.
        /// </summary>
        private void UpdateControlThemeRecursive(Control parentControl, Color backColor, Color foreColor, Color controlBackColor, Color buttonBackColor)
        {
            parentControl.BackColor = backColor;
            parentControl.ForeColor = foreColor;

            foreach (Control control in parentControl.Controls)
            {
                SafeControlUpdate(control, () => {
                    if (control == _toggleAutoRunButton) // Special case for toggleAutoRunButton
                    {
                        control.ForeColor = AutoRunButtonForeColor; // Keep its specific ForeColor
                        // BackColor is set by UpdateAutoRunUI
                    }
                    else if (control is Button button) // Handles _createReportButton, _processEmailButton, _oneClickProcessButton, etc.
                    {
                        button.BackColor = buttonBackColor;
                        button.ForeColor = foreColor;
                        button.FlatStyle = FlatStyle.System; // Ensures consistent button appearance
                    }
                    else if (control is TextBox || control is RichTextBox || control is ComboBox || control is DateTimePicker)
                    {
                        control.BackColor = controlBackColor;
                        control.ForeColor = foreColor;
                        if (control is ComboBox cb) cb.FlatStyle = FlatStyle.Standard;
                        if (control is TextBox tb) tb.BorderStyle = _isDarkMode ? BorderStyle.FixedSingle : BorderStyle.Fixed3D;
                        if (control is RichTextBox rtb) rtb.BorderStyle = _isDarkMode ? BorderStyle.FixedSingle : BorderStyle.Fixed3D;
                    }
                    else if (control is CheckBox cb) // Handles _sendToFemiOnlyCheckBox, _skipEmailCheckBox
                    {
                        cb.BackColor = backColor; // CheckBox background should match form background
                        cb.ForeColor = foreColor;
                        cb.FlatStyle = FlatStyle.Standard; // Consistent appearance
                    }
                    else if (control is Label || control is GroupBox)
                    {
                        control.BackColor = backColor;
                        control.ForeColor = foreColor;
                        if (control is GroupBox gb)
                        {
                            UpdateControlThemeRecursive(gb, backColor, foreColor, controlBackColor, buttonBackColor); // Recurse into GroupBox
                        }
                    }
                    else if (!(control is MenuStrip || control is StatusStrip || control is ToolStrip)) // Avoid re-theming these containers
                    {
                        UpdateControlThemeRecursive(control, backColor, foreColor, controlBackColor, buttonBackColor); // Recurse for other controls
                    }
                });
            }
        }


        /// <summary>
        /// Updates menu item colors, including their dropdowns.
        /// </summary>
        private static void UpdateMenuItemsTheme(ToolStripItemCollection items, Color backColor, Color foreColor)
        {
            foreach (ToolStripItem item in items)
            {
                item.BackColor = backColor;
                item.ForeColor = foreColor;
                if (item is ToolStripMenuItem menuItem && menuItem.HasDropDownItems)
                {
                    menuItem.DropDown.BackColor = backColor;
                    menuItem.DropDown.ForeColor = foreColor;
                    UpdateMenuItemsTheme(menuItem.DropDownItems, backColor, foreColor);
                }
            }
        }

        /// <summary>
        /// Checks the Windows Registry to determine if the Apps theme is set to dark mode.
        /// </summary>
        public static bool IsWindowsDarkModeEnabled()
        {
            try
            {
                const string keyPath = @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
                const string valueName = "AppsUseLightTheme";
                object? registryValue = Registry.GetValue(keyPath, valueName, 1);
                return registryValue is int intValue && intValue == 0;
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error reading Windows theme setting from registry: {ex.Message}");
                return false; // Default to light mode on error
            }
        }
        #endregion

        #region UI State Management
        /// <summary>
        /// Updates the text of the main status label.
        /// </summary>
        public void UpdateStatusMain(string message)
        {
            SafeToolStripItemUpdate(_statusLabel, () => {
                _statusLabel.Text = message;
            });
        }

        /// <summary>
        /// Gets the current text of the main status label.
        /// </summary>
        public string GetCurrentStatusMain()
        {
            string currentStatus = string.Empty;
            SafeToolStripItemUpdate(_statusLabel, () => { currentStatus = _statusLabel.Text ?? string.Empty; });
            return currentStatus;
        }

        /// <summary>
        /// Updates the text of the auto run status label.
        /// </summary>
        public void UpdateStatusRight(string message)
        {
            SafeToolStripItemUpdate(_autoRunStatusLabel, () => { _autoRunStatusLabel.Text = message; });
        }

        /// <summary>
        /// Enables or disables the main action buttons, including the new 1-Click button.
        /// </summary>
        public void SetActionButtonsEnabled(bool enable)
        {
            SafeControlUpdate(_createReportButton, () => { _createReportButton.Enabled = enable; });
            SafeControlUpdate(_processEmailButton, () => { _processEmailButton.Enabled = enable; });
            SafeControlUpdate(_generateAndSendButton, () => { _generateAndSendButton.Enabled = enable; });
            SafeControlUpdate(_oneClickProcessButton, () => { _oneClickProcessButton.Enabled = enable; }); // Include 1-Click button
        }

        /// <summary>
        /// Enables or disables secondary input controls.
        /// Now includes _skipEmailCheckBox.
        /// </summary>
        public void SetOtherControlsEnabled(bool enable, bool isFinancialYearVisible)
        {
            SafeControlUpdate(_reportTypeComboBox, () => { _reportTypeComboBox.Enabled = enable; });
            SafeControlUpdate(_startDatePicker, () => { _startDatePicker.Enabled = enable; });
            SafeControlUpdate(_endDatePicker, () => { _endDatePicker.Enabled = enable; });
            SafeControlUpdate(_financialYearComboBox, () => { _financialYearComboBox.Enabled = enable && isFinancialYearVisible; });
            SafeControlUpdate(_sendToFemiOnlyCheckBox, () => { _sendToFemiOnlyCheckBox.Enabled = enable; });
            SafeControlUpdate(_skipEmailCheckBox, () => { _skipEmailCheckBox.Enabled = enable; }); // Include Skip Email checkbox
        }

        /// <summary>
        /// Resets the UI to an initial state after an error, cancellation, or completion.
        /// Form1.cs handles the text/visibility of oneClickProcessButton vs createReportButton.
        /// This method handles common elements.
        /// </summary>
        public void ResetUIOnError(string button1Text, bool configValid, bool rawReportExists, bool analysisExists, bool isDailySelected, bool isTimerEnabled, bool isDarkMode, bool isFinalStatusForToday, string currentAutoRunStatusText)
        {
            SafeControlUpdate(_parentForm, () =>
            {
                Logger.LogDebug($"UIManager: Resetting UI state. Button 1 text (fallback): '{button1Text}'");

                // Form1.cs's ResetUIStateOnError handles the logic for which button (createReportButton or oneClickProcessButton)
                // gets `button1Text` and its enabled state based on the 1-click mode.
                // This UIManager method can set the defaults for the original buttons.
                _createReportButton.Text = configValid ? button1Text : "Config Error"; // Fallback text for createReportButton
                _createReportButton.Enabled = configValid; // General enablement

                _processEmailButton.Text = "Process and Email";
                _processEmailButton.Enabled = rawReportExists;

                _generateAndSendButton.Text = "Generate && Send"; // Old button
                _generateAndSendButton.Enabled = configValid;

                _oneClickProcessButton.Enabled = configValid; // General enablement, Form1 might override text

                _toggleAutoRunButton.Enabled = true; // Always re-enable toggle

                SetOtherControlsEnabled(true, _financialYearComboBox.Visible); // Re-enable date pickers, etc.

                _viewReportButton.Visible = rawReportExists;
                _viewReportButton.Enabled = rawReportExists;
                _viewAnalysisButton.Visible = analysisExists;
                _viewAnalysisButton.Enabled = analysisExists;

                // Visibility of sendToFemiOnlyCheckBox and emailRecipientLabel is handled by Form1 based on report type
                // _sendToFemiOnlyCheckBox.Visible = !isDailySelected;
                // _emailRecipientLabel.Visible = isDailySelected;
                // if (isDailySelected) _emailRecipientLabel.Text = "Emailing Daily report to Paul";

                UpdateAutoRunUI(isTimerEnabled, isFinalStatusForToday, isDarkMode, currentAutoRunStatusText);

                string currentMainStatus = _statusLabel.Text ?? string.Empty;
                if (currentMainStatus != "Ready" &&
                    !currentMainStatus.StartsWith("Auto Run:") &&
                    !currentMainStatus.StartsWith("Configuration O") &&
                    !currentMainStatus.StartsWith("Configuration E") &&
                    !currentMainStatus.Contains("Completed Successfully"))
                {
                    _ = Task.Delay(5000).ContinueWith(t => {
                        SafeToolStripItemUpdate(_statusLabel, () => {
                            if (_statusLabel.Text == currentMainStatus &&
                                !(_statusLabel.Text ?? string.Empty).StartsWith("Auto Run:") &&
                                !(_statusLabel.Text ?? string.Empty).StartsWith("Configuration") &&
                                !(_statusLabel.Text ?? string.Empty).Contains("Completed Successfully"))
                            {
                                _statusLabel.Text = "Ready";
                            }
                        });
                    }, TaskScheduler.FromCurrentSynchronizationContext());
                }
                else if (string.IsNullOrEmpty(currentMainStatus) || currentMainStatus.Contains("in progress") || currentMainStatus.Contains("Validating") || currentMainStatus.Contains("Starting"))
                {
                    UpdateStatusMain("Ready");
                }
            });
        }


        /// <summary>
        /// Sets the UI state after a successful manual process completion.
        /// Form1.cs handles the specific text for oneClickProcessButton vs createReportButton.
        /// </summary>
        public void SetUICompleted(bool configValid, bool isDailySelected, bool isTimerEnabled, bool isDarkMode, bool isFinalStatusForToday, string currentAutoRunStatusText)
        {
            UpdateStatusMain("Process Completed Successfully.");
            // Form1's ResetUIStateOnError will set the correct text for the visible main action button.
            // Pass a generic "Create Report" or "1-Click Process" as appropriate from Form1.
            ResetUIOnError("Create Report", configValid, File.Exists(_viewReportButton.Tag?.ToString() ?? ""), File.Exists(_viewAnalysisButton.Tag?.ToString() ?? ""), isDailySelected, isTimerEnabled, isDarkMode, isFinalStatusForToday, currentAutoRunStatusText);

            // After ResetUIOnError, Form1 will adjust the specific button text and visibility.
            // UIManager ensures general states are reset.
            SafeControlUpdate(_createReportButton, () => _createReportButton.Enabled = configValid); // Re-enable if in 2-button mode
            SafeControlUpdate(_processEmailButton, () => _processEmailButton.Enabled = false); // Process button usually disabled after completion
            SafeControlUpdate(_generateAndSendButton, () => _generateAndSendButton.Enabled = configValid);
            SafeControlUpdate(_oneClickProcessButton, () => _oneClickProcessButton.Enabled = configValid); // Re-enable if in 1-button mode
        }


        /// <summary>
        /// Resets button states when the report type changes.
        /// Form1.cs handles visibility of oneClickProcessButton vs createReportButton.
        /// </summary>
        public void ResetButtonStatesAfterTypeChange(bool configValid)
        {
            SafeControlUpdate(_parentForm, () =>
            {
                Logger.LogDebug("UIManager: Resetting button states due to report type change.");
                // Form1.cs will set the correct text for the visible button (createReportButton or oneClickProcessButton)
                _createReportButton.Text = configValid ? "Create Report" : "Config Error";
                _createReportButton.Enabled = configValid;

                _processEmailButton.Text = "Process and Email";
                _processEmailButton.Enabled = false; // Typically disabled until raw report is made

                _generateAndSendButton.Text = "Generate && Send"; // Old button
                _generateAndSendButton.Enabled = configValid;

                _oneClickProcessButton.Text = configValid ? "1-Click Process" : "Config Error"; // Default text
                _oneClickProcessButton.Enabled = configValid;


                _viewReportButton.Visible = false;
                _viewReportButton.Enabled = false;
                _viewAnalysisButton.Visible = false;
                _viewAnalysisButton.Enabled = false;
                _viewReportButton.Tag = null;
                _viewAnalysisButton.Tag = null;

                UpdateStatusMain("Ready");
            });
        }


        /// <summary>
        /// Shows or hides the "View Analysis" button.
        /// </summary>
        public void ShowViewAnalysisButton(bool show, string? filePath = null)
        {
            SafeControlUpdate(_viewAnalysisButton, () =>
            {
                _viewAnalysisButton.Visible = show;
                _viewAnalysisButton.Enabled = show;
                _viewAnalysisButton.Tag = filePath;
            });
        }

        /// <summary>
        /// Shows or hides the "View Report" button.
        /// </summary>
        public void ShowViewReportButton(bool show, string? filePath = null)
        {
            SafeControlUpdate(_viewReportButton, () =>
            {
                _viewReportButton.Visible = show;
                _viewReportButton.Enabled = show;
                _viewReportButton.Tag = filePath;
            });
        }
        #endregion

        #region Auto Run UI
        /// <summary>
        /// Updates the UI elements related to the auto-run feature.
        /// </summary>
        public void UpdateAutoRunUI(bool enable, bool isFinalStatusForToday, bool isDarkMode, string statusText = "")
        {
            SafeControlUpdate(_toggleAutoRunButton, () => {
                if (_toggleAutoRunButton.IsDisposed) return;
                // The text might now include the hour, which Form1.cs can update if it changes
                // For now, keep the generic text or let Form1 update it.
                // _toggleAutoRunButton.Text = enable ? "Disable Daily Auto Run @ 8 AM" : "Enable Daily Auto Run @ 8 AM";
                if (string.IsNullOrEmpty(_toggleAutoRunButton.Text) || !_toggleAutoRunButton.Text.Contains(":")) // Basic check if hour is already in text
                {
                    _toggleAutoRunButton.Text = enable ? "Disable Daily Auto Run" : "Enable Daily Auto Run";
                }
                _toggleAutoRunButton.BackColor = enable ? AutoRunEnabledColor : AutoRunDisabledColor;
                _toggleAutoRunButton.ForeColor = AutoRunButtonForeColor;
            });

            SafeToolStripItemUpdate(_autoRunStatusLabel, () => {
                if (_autoRunStatusLabel.IsDisposed) return;
                string currentStatusText = _autoRunStatusLabel.Text ?? string.Empty;
                string textToShow = statusText;
                if (string.IsNullOrEmpty(textToShow)) // If no specific status text is provided
                {
                    // Determine text based on enable state and if a final status for today is already set
                    textToShow = enable ? (isFinalStatusForToday ? currentStatusText : "Auto Run: Enabled")
                                        : (isFinalStatusForToday ? currentStatusText : "Auto Run: Disabled");
                }
                _autoRunStatusLabel.Text = textToShow;
                _autoRunStatusLabel.ForeColor = (enable && !isFinalStatusForToday && !textToShow.Contains("FAILED") && !textToShow.Contains("ERROR")) ? Color.Green : (isDarkMode ? DM_ForeColor : LM_ForeColor);
                if (textToShow.Contains("FAILED") || textToShow.Contains("ERROR")) _autoRunStatusLabel.ForeColor = Color.Red;

            });
        }


        /// <summary>
        /// Disables primary UI controls during automated report execution.
        /// </summary>
        public void DisableControlsForAutoRun()
        {
            Logger.LogDebug("Disabling controls for Auto Run.");
            SetActionButtonsEnabled(false); // This will now disable _oneClickProcessButton too
            SetOtherControlsEnabled(false, _financialYearComboBox.Visible); // This now disables _skipEmailCheckBox too
            SafeControlUpdate(_toggleAutoRunButton, () => _toggleAutoRunButton.Enabled = false);
            SafeControlUpdate(_viewReportButton, () => _viewReportButton.Enabled = false);
            SafeControlUpdate(_viewAnalysisButton, () => _viewAnalysisButton.Enabled = false);
            UpdateProgress("Auto Run in progress...");
        }
        #endregion

        #region Progress Reporting (No ProgressBar)
        /// <summary>
        /// Updates the status message. Progress bar functionality removed.
        /// </summary>
        public void UpdateProgress(string message)
        {
            SafeToolStripItemUpdate(_statusLabel, () => { _statusLabel.Text = message; });
        }

        /// <summary>
        /// Updates the status message. Progress bar functionality removed.
        /// </summary>
        public void UpdateProgress(ProgressReport report)
        {
            SafeToolStripItemUpdate(_statusLabel, () => { _statusLabel.Text = report.Message; });
        }
        #endregion

        #region ToolTip Management
        /// <summary>
        /// Sets the ToolTip text for a specified control.
        /// </summary>
        public void SetToolTip(Control control, string tipText)
        {
            SafeControlUpdate(control, () => { _toolTip.SetToolTip(control, tipText); });
        }

        /// <summary>
        /// Sets the ToolTip text for a specified ToolStripItem.
        /// </summary>
        public void SetToolTip(ToolStripItem item, string tipText)
        {
            SafeToolStripItemUpdate(item, () => { item.ToolTipText = tipText; });
        }
        #endregion

        #region Safe UI Update Utility 
        /// <summary>
        /// Safely updates a standard Control's property or state by executing an action.
        /// </summary>
        public static void SafeControlUpdate(Control ctrl, Action action)
        {
            ArgumentNullException.ThrowIfNull(action);
            if (ctrl == null || ctrl.IsDisposed) return;
            if (ctrl.IsHandleCreated && !ctrl.Disposing)
            {
                if (ctrl.InvokeRequired)
                {
                    try { ctrl.BeginInvoke(action); }
                    catch (ObjectDisposedException) { }
                    catch (InvalidOperationException ex) when (ex.Message.Contains("Invoke") || ex.Message.Contains("Handle")) { Logger.LogWarning($"SafeControlUpdate ignored invoke/handle error: {ex.Message}"); }
                    catch (Exception ex) { Logger.LogError($"Unexpected error during SafeControlUpdate Invoke/BeginInvoke: {ex}"); }
                }
                else
                {
                    try { action(); }
                    catch (Exception ex) { Logger.LogError($"Unexpected error during SafeControlUpdate direct action: {ex}"); }
                }
            }
            else
            {
                if (!ctrl.IsHandleCreated) Logger.LogTrace($"SafeControlUpdate skipped for control '{ctrl.Name}' as handle is not created.");
                if (ctrl.Disposing) Logger.LogTrace($"SafeControlUpdate skipped for control '{ctrl.Name}' as it is disposing.");
            }
        }

        /// <summary>
        /// Safely updates a ToolStripItem's property or state by executing an action.
        /// </summary>
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
                    catch (ObjectDisposedException) { }
                    catch (InvalidOperationException ex) when (ex.Message.Contains("Invoke") || ex.Message.Contains("Handle")) { Logger.LogWarning($"SafeToolStripItemUpdate ignored invoke/handle error: {ex.Message}"); }
                    catch (Exception ex) { Logger.LogError($"Unexpected error during SafeToolStripItemUpdate Invoke/BeginInvoke: {ex}"); }
                }
                else
                {
                    try { action(); }
                    catch (Exception ex) { Logger.LogError($"Unexpected error during SafeToolStripItemUpdate direct action: {ex}"); }
                }
            }
            else
            {
                string ownerName = owner?.Name ?? "null";
                if (owner == null) Logger.LogWarning($"SafeToolStripItemUpdate skipped for item '{item.Name}' as owner is null.");
                else if (!owner.IsHandleCreated) Logger.LogTrace($"SafeToolStripItemUpdate skipped for item '{item.Name}' on '{ownerName}' as owner handle not created.");
                else if (owner.IsDisposed || owner.Disposing) Logger.LogTrace($"SafeToolStripItemUpdate skipped for item '{item.Name}' on '{ownerName}' as owner is disposed/disposing.");
            }
        }
        #endregion
    }
}
