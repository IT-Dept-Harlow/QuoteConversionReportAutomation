#region Using Directives
// System related namespaces
using System;
using System.Drawing;
using System.Windows.Forms;

// Project specific namespaces
using QuoteConversionReportAutomation.Helpers; // For FlexibleMessageBox
using QuoteConversionReportAutomation.Managers; // For GreetingManager and UIManager
using QuoteConversionReportAutomation.Models;   // For UserGreetingSettings
using QuoteConversionReportAutomation.Services.Logging; // For Logger
using QuoteConversionReportAutomation.Theming; // Required for centralised theming
#endregion

namespace QuoteConversionReportAutomation.Forms
{
    /// <summary>
    /// A Windows Form that allows users to view and modify email greeting messages
    /// for different report generation contexts. User-defined greetings are saved
    /// and will override any application defaults. Theming is handled by the centralised ThemeSettings.
    /// </summary>
    public partial class ManageGreetingsForm : Form
    {
        #region Fields
        private readonly GreetingManager _greetingManager; // Service for loading/saving greeting settings.

        // --- UI Control Field for Manual Custom Greeting ---
        // This field holds a reference to the dynamically added or designer-placed TextBox
        // for managing the greeting for manually run "Custom" type reports.
        private TextBox txtManualCustom;
        #endregion

        #region Constructor
        /// <summary>
        /// Initialises a new instance of the <see cref="ManageGreetingsForm"/> class.
        /// </summary>
        /// <param name="greetingManager">The manager responsible for greeting settings logic.</param>
        public ManageGreetingsForm(GreetingManager greetingManager)
        {
            _greetingManager = greetingManager ?? throw new ArgumentNullException(nameof(greetingManager));

            InitializeComponent(); // Standard WinForms method from ManageGreetingsForm.Designer.cs.

            // Attempt to find or dynamically create the control for the "Manual Custom" greeting.
            InitializeManualCustomGreetingControl();

            // Configure basic form properties.
            this.ShowIcon = false; // Do not show an icon in the title bar.
            this.StartPosition = FormStartPosition.CenterParent; // Centre the form relative to its parent.
            this.Text = "Manage Email Greetings"; // Set the window title.
        }
        #endregion

        #region Form Load and Theming
        /// <summary>
        /// Handles the Load event of the form. This is called once when the form is first displayed.
        /// It applies the visual theme, loads current greeting settings into the UI, and sets up tooltips.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">An <see cref="EventArgs"/> that contains no event data.</param>
        private void ManageGreetingsForm_Load(object sender, EventArgs e)
        {
            Logger.LogInfo($"ManageGreetingsForm loading. Theming enabled: {ThemeSettings.EnableCustomTheming}, CurrentMode: {ThemeSettings.CurrentThemeMode}");

            // Apply the theme to the form itself (title bar, main background) using the centralised settings.
            UIManager.ApplyThemeToExternalForm(this, ThemeSettings.IsCurrentlyDark());
            // Apply the theme to child controls within this form.
            ApplyChildControlTheme();
            // Load current greeting settings into the TextBoxes.
            LoadGreetingsToForm();
            // Set up informational tooltips.
            SetupToolTips();

#if !DEBUG
            // In Release mode, hide the debug-specific greeting field.
            HideDebugGreetingField();
#endif
            Logger.LogInfo("ManageGreetingsForm loaded and themed.");
        }

        /// <summary>
        /// Hides the UI elements related to the debug greeting when the application is not in DEBUG mode.
        /// </summary>
        private void HideDebugGreetingField()
        {
            Logger.LogInfo("Release mode: Hiding debug greeting field.");
            // Hide the label and textbox for the debug greeting.
            if (lblDebugDefault != null) lblDebugDefault.Visible = false;
            if (txtDebugDefault != null) txtDebugDefault.Visible = false;

            // The original logic noted that simply hiding controls is sufficient for the visual effect.
            // More complex row collapsing logic is not implemented by default.
            if (mainTableLayoutPanel.RowCount > 6)
            {
                Logger.LogDebug("Debug greeting controls hidden. Row collapsing in TableLayoutPanel is not explicitly handled here beyond control visibility.");
            }
        }

        /// <summary>
        /// Applies the current theme from ThemeSettings specifically to the child controls of this form.
        /// </summary>
        private void ApplyChildControlTheme()
        {
            if (!ThemeSettings.EnableCustomTheming) return;

            bool isDarkMode = ThemeSettings.IsCurrentlyDark();
            var palette = ThemeSettings.CurrentPalette;

            // Recursively apply these colours to all controls on the form.
            UpdateControlThemeRecursive(this, palette, isDarkMode);
        }

        /// <summary>
        /// Recursive helper method to apply theme colours to a control and all its child controls using the ThemePalette.
        /// </summary>
        /// <param name="parentControl">The control to start theming from.</param>
        /// <param name="palette">The colour palette to use for theming.</param>
        /// <param name="isDarkMode">A flag indicating if dark mode is currently being applied.</param>
        private void UpdateControlThemeRecursive(Control parentControl, ThemePalette palette, bool isDarkMode)
        {
            // The form's own BackColor is set by UIManager.ApplyThemeToExternalForm.
            // Child container controls should match it.
            if (parentControl != this)
            {
                parentControl.BackColor = this.BackColor;
            }

            foreach (Control control in parentControl.Controls)
            {
                if (control.IsDisposed) continue; // Skip disposed controls.

                // Apply theme based on control type.
                if (control is Button button)
                {
                    button.BackColor = palette.ButtonBackColor;
                    button.ForeColor = palette.ButtonForeColor;
                    button.FlatStyle = FlatStyle.Flat;
                    button.FlatAppearance.BorderColor = palette.ButtonBorderColor;
                    button.FlatAppearance.BorderSize = 1;
                }
                else if (control is TextBox textBox)
                {
                    textBox.BackColor = palette.ControlBackColor;
                    textBox.ForeColor = palette.ControlForeColor;
                    textBox.BorderStyle = isDarkMode ? BorderStyle.FixedSingle : BorderStyle.Fixed3D;
                }
                else if (control is Label label)
                {
                    label.BackColor = Color.Transparent; // Labels should typically be transparent.
                    label.ForeColor = palette.LabelForeColor;
                }
                else if (control is GroupBox gb)
                {
                    gb.ForeColor = palette.GroupBoxForeColor; // Text colour for the GroupBox title.
                    gb.BackColor = this.BackColor; // Match form's background.
                    UpdateControlThemeRecursive(gb, palette, isDarkMode); // Recurse.
                }
                else if (control is Panel || control is TableLayoutPanel)
                {
                    control.BackColor = this.BackColor; // Match form's background.
                    control.ForeColor = palette.FormForeColor;
                    UpdateControlThemeRecursive(control, palette, isDarkMode); // Recurse.
                }
            }
        }
        #endregion

        #region UI Initialisation and Data Loading
        /// <summary>
        /// Initialises or finds the control for the "Manual Custom" greeting.
        /// If `txtManualCustom` is not found (e.g., not added via the WinForms designer),
        /// this method creates it programmatically along with its label and adds them
        /// to the `mainTableLayoutPanel`.
        /// </summary>
        private void InitializeManualCustomGreetingControl()
        {
            // This method's logic is for control creation, not theming, so it remains unchanged.
            // Theming is applied globally in Form_Load after this method runs.
            #region Original Method Content
            Control[] foundControls = this.Controls.Find("txtManualCustom", true);
            if (foundControls.Length > 0 && foundControls[0] is TextBox)
            {
                txtManualCustom = (TextBox)foundControls[0];
                Logger.LogDebug("Manual Custom greeting TextBox found by name (likely from designer).");
            }
            else
            {
                Logger.LogDebug("Manual Custom greeting TextBox not found by name. Creating it programmatically.");

                Label lblManualCustom = new Label
                {
                    Text = "Manual Custom Greeting:",
                    Anchor = AnchorStyles.Right | AnchorStyles.Top,
                    AutoSize = true,
                    Margin = new Padding(3, 6, 3, 3)
                };

                txtManualCustom = new TextBox
                {
                    Name = "txtManualCustom",
                    Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top,
                    Height = 20
                };

                if (mainTableLayoutPanel != null)
                {
                    int newRowIndex = mainTableLayoutPanel.RowCount;
                    mainTableLayoutPanel.RowCount = newRowIndex + 1;
                    mainTableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F / (mainTableLayoutPanel.RowCount - 1)));

                    mainTableLayoutPanel.Controls.Add(lblManualCustom, 0, newRowIndex);
                    mainTableLayoutPanel.Controls.Add(txtManualCustom, 1, newRowIndex);
                    Logger.LogInfo("Programmatically added UI elements for Manual Custom greeting.");
                }
                else
                {
                    Logger.LogError("mainTableLayoutPanel is null. Cannot add Manual Custom greeting controls programmatically.");
                }
            }
            #endregion
        }

        /// <summary>
        /// Loads the current effective greeting settings (merged from defaults and user overrides)
        /// into the corresponding TextBox controls on the form.
        /// </summary>
        private void LoadGreetingsToForm()
        {
            // This method's logic is unchanged.
            #region Original Method Content
            UserGreetingSettings effectiveGreetings = _greetingManager.GetCurrentEffectiveGreetings();

            txtAutoRunDaily.Text = effectiveGreetings.AutoRunDaily;
            txtManualStdDaily.Text = effectiveGreetings.ManualStdDaily;
            txtAutoRunDaily5Day1k.Text = effectiveGreetings.AutoRunDaily5Day1k;
            txtManualFemi.Text = effectiveGreetings.ManualFemi;
            txtManualTeam.Text = effectiveGreetings.ManualTeam;

            if (txtManualCustom != null)
            {
                txtManualCustom.Text = effectiveGreetings.ManualCustom;
            }

#if DEBUG
            if (txtDebugDefault != null)
            {
                txtDebugDefault.Text = effectiveGreetings.DebugDefault;
            }
#endif
            Logger.LogInfo("Loaded current greetings into ManageGreetingsForm.");
            #endregion
        }

        /// <summary>
        /// Sets up informational tooltips for the various greeting TextBox controls and action buttons.
        /// </summary>
        private void SetupToolTips()
        {
            // This method's logic is unchanged.
            #region Original Method Content
            if (this.toolTipProvider == null)
            {
                this.toolTipProvider = new ToolTip(this.components ?? (this.components = new System.ComponentModel.Container()));
            }
            toolTipProvider.SetToolTip(txtAutoRunDaily, "Greeting for automated standard daily reports.");
            toolTipProvider.SetToolTip(txtManualStdDaily, "Greeting for manually run standard daily reports.");
            toolTipProvider.SetToolTip(txtAutoRunDaily5Day1k, "Greeting for automated 'Daily (5days >= £1k)' reports.");
            toolTipProvider.SetToolTip(txtManualFemi, "Greeting for manual non-daily reports when 'Femi Only' is selected.");
            toolTipProvider.SetToolTip(txtManualTeam, "Greeting for manual non-daily reports for the general team.");

            if (txtManualCustom != null)
            {
                toolTipProvider.SetToolTip(txtManualCustom, "Greeting for manually run 'Custom' type reports.");
            }

#if DEBUG
            if (txtDebugDefault != null) toolTipProvider.SetToolTip(txtDebugDefault, "Default greeting for all reports in DEBUG mode.");
#endif
            toolTipProvider.SetToolTip(btnSave, "Save your custom greetings. They will override app defaults.");
            toolTipProvider.SetToolTip(btnRestoreDefaults, "Remove all custom greetings and revert to those defined in appsettings.json.");
            toolTipProvider.SetToolTip(btnClose, "Close this window without saving current changes.");
            #endregion
        }
        #endregion

        #region Button Event Handlers
        /// <summary>
        /// Handles the Click event for the "Save" button.
        /// Gathers the greeting texts from the form, creates a <see cref="UserGreetingSettings"/> object,
        /// and saves it using the <see cref="GreetingManager"/>.
        /// </summary>
        private void BtnSave_Click(object sender, EventArgs e)
        {
            // This method's logic is unchanged.
            #region Original Method Content
            Logger.LogInfo("Save button clicked on ManageGreetingsForm.");
            var newOverrides = new UserGreetingSettings
            {
                AutoRunDaily = string.IsNullOrWhiteSpace(txtAutoRunDaily.Text) ? null : txtAutoRunDaily.Text.Trim(),
                ManualStdDaily = string.IsNullOrWhiteSpace(txtManualStdDaily.Text) ? null : txtManualStdDaily.Text.Trim(),
                AutoRunDaily5Day1k = string.IsNullOrWhiteSpace(txtAutoRunDaily5Day1k.Text) ? null : txtAutoRunDaily5Day1k.Text.Trim(),
                ManualFemi = string.IsNullOrWhiteSpace(txtManualFemi.Text) ? null : txtManualFemi.Text.Trim(),
                ManualTeam = string.IsNullOrWhiteSpace(txtManualTeam.Text) ? null : txtManualTeam.Text.Trim(),
                ManualCustom = (txtManualCustom != null && !string.IsNullOrWhiteSpace(txtManualCustom.Text)) ? txtManualCustom.Text.Trim() : null
            };

#if DEBUG
            if (txtDebugDefault != null)
            {
                newOverrides.DebugDefault = string.IsNullOrWhiteSpace(txtDebugDefault.Text) ? null : txtDebugDefault.Text.Trim();
            }
#else
            newOverrides.DebugDefault = _greetingManager.GetCurrentEffectiveGreetings().DebugDefault;
#endif

            DialogResult confirmSaveResult = FlexibleMessageBox.Show(this, "Do you want to save these email greetings?\nEmpty fields will revert to application defaults.",
                "Confirm Save Greetings", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (confirmSaveResult == DialogResult.Yes)
            {
                try
                {
                    _greetingManager.SaveUserGreetingOverrides(newOverrides);
                    Logger.LogInfo("User confirmed and email greetings saved successfully.");
                    FlexibleMessageBox.Show(this, "Email greeting settings have been saved.",
                        "Settings Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    DialogResult = DialogResult.OK;
                    Close();
                }
                catch (Exception ex)
                {
                    Logger.LogError($"Failed to save email greeting settings: {ex.Message}", ex);
                    FlexibleMessageBox.Show(this, $"An error occurred while saving the greeting settings:\n\n{ex.Message}",
                        "Save Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            #endregion
        }

        /// <summary>
        /// Handles the Click event for the "Restore Defaults" button.
        /// Clears all user-defined greeting overrides, causing the application to revert to
        /// the default greetings specified in `appsettings.json`.
        /// </summary>
        private void BtnRestoreDefaults_Click(object sender, EventArgs e)
        {
            // This method's logic is unchanged.
            #region Original Method Content
            Logger.LogInfo("Restore Defaults button clicked on ManageGreetingsForm.");
            DialogResult confirmRestoreResult = FlexibleMessageBox.Show(this, "Are you sure you want to restore all greetings to application defaults?\nThis will remove any custom greetings you have saved.",
                "Confirm Restore Defaults", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

            if (confirmRestoreResult == DialogResult.Yes)
            {
                try
                {
                    _greetingManager.ClearUserGreetingOverrides();
                    LoadGreetingsToForm();
                    Logger.LogInfo("User confirmed and email greetings restored to defaults.");
                    FlexibleMessageBox.Show(this, "Email greeting settings have been restored to application defaults.",
                        "Defaults Restored", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    Logger.LogError($"Failed to restore default email greeting settings: {ex.Message}", ex);
                    FlexibleMessageBox.Show(this, $"An error occurred while restoring default settings:\n\n{ex.Message}",
                        "Restore Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            #endregion
        }

        /// <summary>
        /// Handles the Click event for the "Close" button.
        /// Closes the form without saving any pending changes.
        /// </summary>
        private void BtnClose_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }
        #endregion
    }
}