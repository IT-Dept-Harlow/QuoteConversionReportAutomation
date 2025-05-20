// ManageGreetingsForm.cs
namespace QuoteConversionReportAutomation
{
    using QuoteConversionReportAutomation.Managers;
    using QuoteConversionReportAutomation.Models;
    using QuoteConversionReportAutomation.Services.Logging;
    using System;
    using System.Drawing;
    using System.Windows.Forms;
    using QuoteConversionReportAutomation.Helpers; // For FlexibleMessageBox

    public partial class ManageGreetingsForm : Form
    {
        private readonly GreetingManager _greetingManager;
        private readonly bool _isDarkMode;

        // Theme Colors (can be centralized or passed if more forms use them)
        private static readonly Color DM_ControlBackColor = Color.FromArgb(60, 60, 63);
        private static readonly Color DM_ButtonBackColor = Color.FromArgb(80, 80, 80);
        private static readonly Color DM_ControlForeColor = Color.White;
        private static readonly Color LM_ControlBackColor = SystemColors.Window;
        private static readonly Color LM_ButtonBackColor = SystemColors.Control;
        private static readonly Color LM_ControlForeColor = SystemColors.ControlText;

        public ManageGreetingsForm(GreetingManager greetingManager, bool isDarkMode)
        {
            _greetingManager = greetingManager ?? throw new ArgumentNullException(nameof(greetingManager));
            _isDarkMode = isDarkMode;

            InitializeComponent(); // From ManageGreetingsForm.Designer.cs
            this.ShowIcon = false;
            this.StartPosition = FormStartPosition.CenterParent;
            this.Text = "Manage Email Greetings";
        }

        private void ManageGreetingsForm_Load(object sender, EventArgs e)
        {
            Logger.LogInfo($"ManageGreetingsForm loading. Initial DarkMode state: {_isDarkMode}");
            UIManager.ApplyThemeToExternalForm(this, _isDarkMode); // Theme the form itself
            ApplyChildControlTheme(_isDarkMode); // Theme controls within this form
            LoadGreetingsToForm();
            SetupToolTips();

#if !DEBUG
            HideDebugGreetingField();
#endif
            Logger.LogInfo("ManageGreetingsForm loaded and themed.");
        }

        private void HideDebugGreetingField()
        {
            Logger.LogInfo("Release mode: Hiding debug greeting field.");
            if (lblDebugDefault != null) lblDebugDefault.Visible = false;
            if (txtDebugDefault != null) txtDebugDefault.Visible = false;

            // Adjust TableLayoutPanel row for debug greeting to take no space
            // Assuming row 6 (0-indexed) is for the debug greeting based on the designer.
            // Row 0: Instructions, Row 1-5: Prod Greetings, Row 6: Debug Greeting
            if (mainTableLayoutPanel.RowCount > 6) // Check if the row exists
            {
                // Get current height of a typical row to maintain consistency if we were to resize others
                float typicalHeight = mainTableLayoutPanel.RowStyles[1].Height; // Example from a percent row
                if (mainTableLayoutPanel.RowStyles[6].SizeType == SizeType.Percent)
                {
                    // To effectively hide a percentage row, you can't set height to 0 directly
                    // if other rows are also percentage based and sum to 100%.
                    // One approach is to change it to Absolute and height 0.
                    // Or, adjust percentages of other rows (more complex).
                    // For simplicity, if just hiding, making controls invisible is often enough.
                    // If layout must collapse, TableLayoutPanel might need dynamic row removal/re-addition
                    // or a different layout panel.
                    // For now, visibility is the primary goal. If TableLayoutPanel still reserves space,
                    // more advanced layout adjustment would be needed.
                }
                // For now, just ensuring controls are hidden is the main goal.
                // If the row itself needs to visually collapse, that's a more involved TableLayoutPanel change.
            }
        }


        private void ApplyChildControlTheme(bool isDarkModeEnabled)
        {
            Color controlBackColor = isDarkModeEnabled ? DM_ControlBackColor : LM_ControlBackColor;
            Color buttonBackColor = isDarkModeEnabled ? DM_ButtonBackColor : LM_ButtonBackColor;
            Color controlForeColor = isDarkModeEnabled ? DM_ControlForeColor : LM_ControlForeColor;
            UpdateControlThemeRecursive(this, controlBackColor, buttonBackColor, controlForeColor, isDarkModeEnabled);
        }

        private void UpdateControlThemeRecursive(Control parentControl, Color controlBackColor, Color buttonBackColor, Color controlForeColor, bool isDarkMode)
        {
            foreach (Control control in parentControl.Controls)
            {
                if (control.IsDisposed) continue;

                if (control is Button button)
                {
                    button.BackColor = buttonBackColor;
                    button.ForeColor = controlForeColor;
                    button.FlatStyle = FlatStyle.Flat;
                    button.FlatAppearance.BorderColor = isDarkMode ? Color.FromArgb(100, 100, 100) : SystemColors.ControlDarkDark;
                    button.FlatAppearance.BorderSize = 1;
                }
                else if (control is TextBox)
                {
                    control.BackColor = controlBackColor;
                    control.ForeColor = controlForeColor;
                    ((TextBox)control).BorderStyle = isDarkMode ? BorderStyle.FixedSingle : BorderStyle.Fixed3D;
                }
                else if (control is Label)
                {
                    control.BackColor = Color.Transparent;
                    control.ForeColor = controlForeColor;
                }
                else if (control is GroupBox gb)
                {
                    gb.ForeColor = controlForeColor;
                    gb.BackColor = parentControl.BackColor;
                    UpdateControlThemeRecursive(gb, controlBackColor, buttonBackColor, controlForeColor, isDarkMode);
                }
                else if (control is Panel || control is System.Windows.Forms.TableLayoutPanel)
                {
                    control.BackColor = parentControl.BackColor;
                    control.ForeColor = controlForeColor;
                    UpdateControlThemeRecursive(control, controlBackColor, buttonBackColor, controlForeColor, isDarkMode);
                }
                else
                {
                    if (control.Visible) // Apply to other visible controls
                    {
                        control.BackColor = controlBackColor;
                        control.ForeColor = controlForeColor;
                    }
                }
            }
        }

        private void LoadGreetingsToForm()
        {
            UserGreetingSettings effectiveGreetings = _greetingManager.GetCurrentEffectiveGreetings();

            txtAutoRunDaily.Text = effectiveGreetings.AutoRunDaily;
            txtManualStdDaily.Text = effectiveGreetings.ManualStdDaily;
            txtAutoRunDaily5Day1k.Text = effectiveGreetings.AutoRunDaily5Day1k;
            txtManualFemi.Text = effectiveGreetings.ManualFemi;
            txtManualTeam.Text = effectiveGreetings.ManualTeam;

#if DEBUG
            if (txtDebugDefault != null) // Check if control exists (it should if designer is correct)
            {
                txtDebugDefault.Text = effectiveGreetings.DebugDefault;
            }
#endif
            Logger.LogInfo("Loaded current greetings into ManageGreetingsForm.");
        }

        private void SetupToolTips()
        {
            if (this.toolTipProvider == null)
            {
                this.toolTipProvider = new ToolTip(this.components ?? (this.components = new System.ComponentModel.Container()));
            }
            toolTipProvider.SetToolTip(txtAutoRunDaily, "Greeting for automated standard daily reports.");
            toolTipProvider.SetToolTip(txtManualStdDaily, "Greeting for manually run standard daily reports.");
            toolTipProvider.SetToolTip(txtAutoRunDaily5Day1k, "Greeting for automated 'Daily (5days >= £1k)' reports.");
            toolTipProvider.SetToolTip(txtManualFemi, "Greeting for manual non-daily reports when 'Femi Only' is selected.");
            toolTipProvider.SetToolTip(txtManualTeam, "Greeting for manual non-daily reports for the general team.");
#if DEBUG
            if (txtDebugDefault != null) toolTipProvider.SetToolTip(txtDebugDefault, "Default greeting for all reports in DEBUG mode.");
#endif
            toolTipProvider.SetToolTip(btnSave, "Save your custom greetings. They will override app defaults.");
            toolTipProvider.SetToolTip(btnRestoreDefaults, "Remove all custom greetings and revert to those defined in appsettings.json.");
            toolTipProvider.SetToolTip(btnClose, "Close this window without saving current changes.");
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            Logger.LogInfo("Save button clicked on ManageGreetingsForm.");
            var newOverrides = new UserGreetingSettings
            {
                AutoRunDaily = string.IsNullOrWhiteSpace(txtAutoRunDaily.Text) ? null : txtAutoRunDaily.Text.Trim(),
                ManualStdDaily = string.IsNullOrWhiteSpace(txtManualStdDaily.Text) ? null : txtManualStdDaily.Text.Trim(),
                AutoRunDaily5Day1k = string.IsNullOrWhiteSpace(txtAutoRunDaily5Day1k.Text) ? null : txtAutoRunDaily5Day1k.Text.Trim(),
                ManualFemi = string.IsNullOrWhiteSpace(txtManualFemi.Text) ? null : txtManualFemi.Text.Trim(),
                ManualTeam = string.IsNullOrWhiteSpace(txtManualTeam.Text) ? null : txtManualTeam.Text.Trim(),
            };

#if DEBUG
            if (txtDebugDefault != null)
            {
                newOverrides.DebugDefault = string.IsNullOrWhiteSpace(txtDebugDefault.Text) ? null : txtDebugDefault.Text.Trim();
            }
#else
            // Preserve existing debug override if not in debug mode and saving
            // This prevents accidental clearing of debug settings by a release mode save.
            // However, if the user *wants* to clear it, they'd need to do it in debug or we need a different strategy.
            // For now, let's assume we preserve it if not in debug mode.
            UserGreetingSettings currentOverrides = _greetingManager.GetCurrentEffectiveGreetings(); // This gets merged values
                                                                                                     // To get only user overrides, GreetingManager would need a method like GetOnlyUserOverrides()
                                                                                                     // For simplicity, if not in debug, we won't touch the DebugDefault override.
                                                                                                     // If txtDebugDefault is hidden, its value won't be in newOverrides anyway unless we explicitly load it.
                                                                                                     // The current _userGreetingOverrides in GreetingManager holds the last loaded/saved user values.
            newOverrides.DebugDefault = _greetingManager.GetCurrentEffectiveGreetings().DebugDefault; // Preserve existing or default
#endif

            DialogResult confirmSaveResult = FlexibleMessageBox.Show(this, "Do you want to save these email greetings?\nEmpty fields will revert to application defaults.",
                "Confirm Save Greetings", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (confirmSaveResult == DialogResult.Yes)
            {
                try
                {
                    _greetingManager.SaveUserGreetingOverrides(newOverrides);
                    Logger.LogInfo("User confirmed and email greetings saved.");
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
        }

        private void BtnRestoreDefaults_Click(object sender, EventArgs e)
        {
            Logger.LogInfo("Restore Defaults button clicked on ManageGreetingsForm.");
            DialogResult confirmRestoreResult = FlexibleMessageBox.Show(this, "Are you sure you want to restore all greetings to application defaults?\nThis will remove any custom greetings you have saved.",
                "Confirm Restore Defaults", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

            if (confirmRestoreResult == DialogResult.Yes)
            {
                try
                {
                    _greetingManager.ClearUserGreetingOverrides();
                    LoadGreetingsToForm(); // Reloads defaults (as overrides are now empty)
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
        }

        private void BtnClose_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }
    }
}
