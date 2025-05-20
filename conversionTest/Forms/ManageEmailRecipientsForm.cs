// ManageEmailRecipientsForm.cs
// Ensure this namespace matches your project structure
namespace QuoteConversionReportAutomation
{
    using QuoteConversionReportAutomation.Helpers; // For FlexibleMessageBox, EmailUtility
    using QuoteConversionReportAutomation.Managers;
    using QuoteConversionReportAutomation.Models;
    using QuoteConversionReportAutomation.Services.Logging;
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Linq;
    using System.Windows.Forms;

    /// <summary>
    /// Form for managing user-defined email recipients.
    /// Allows users to override default email lists for various report scenarios.
    /// Title bar and basic form theme are applied via UIManager.
    /// Added fields for "Daily (5days >= £1000)" automated report recipients.
    /// Added fields for "Manual Standard Daily" report recipients.
    /// Debug recipient fields are only visible in DEBUG builds.
    /// </summary>
    public partial class ManageEmailRecipientsForm : Form
    {
        private readonly EmailRecipientManager _emailRecipientManager;
        private readonly bool _isDarkMode;

        // Theme Colors are defined here for reference if needed by child control specific styling,
        // but UIManager and ApplyChildControlTheme will primarily use them.
        private static readonly Color DM_ControlBackColor = Color.FromArgb(60, 60, 63);
        private static readonly Color DM_ButtonBackColor = Color.FromArgb(80, 80, 80);
        private static readonly Color DM_ControlForeColor = Color.White;
        private static readonly Color LM_ControlBackColor = SystemColors.Window;
        private static readonly Color LM_ButtonBackColor = SystemColors.Control;
        private static readonly Color LM_ControlForeColor = SystemColors.ControlText;

        // Controls are defined in the Designer.cs file as part of the partial class.
        // No need to re-declare them here.

        public ManageEmailRecipientsForm(EmailRecipientManager emailRecipientManager, bool isDarkMode)
        {
            _emailRecipientManager = emailRecipientManager ?? throw new ArgumentNullException(nameof(emailRecipientManager));
            _isDarkMode = isDarkMode;

            InitializeComponent();
            this.ShowIcon = false;
            this.StartPosition = FormStartPosition.CenterParent;
            this.Text = "Manage Email Recipients";
        }

        private void ManageEmailRecipientsForm_Load(object sender, EventArgs e)
        {
            Logger.LogInfo($"ManageEmailRecipientsForm loading. Initial DarkMode state: {_isDarkMode}");
            UIManager.ApplyThemeToExternalForm(this, _isDarkMode);
            ApplyChildControlTheme(_isDarkMode);
            LoadSettingsToForm();
            SetupToolTips();

            // Conditionally hide debug fields if not in DEBUG mode
#if !DEBUG
            HideDebugFields();
#endif
            Logger.LogInfo("ManageEmailRecipientsForm loaded and themed.");
        }

        private void HideDebugFields()
        {
            Logger.LogInfo("Release mode: Hiding debug email recipient fields.");
            if (lblDebugTo != null) lblDebugTo.Visible = false;
            if (txtDebugTo != null) txtDebugTo.Visible = false;
            if (lblDebugCC1 != null) lblDebugCC1.Visible = false;
            if (txtDebugCC1 != null) txtDebugCC1.Visible = false;
            if (lblDebugCC2 != null) lblDebugCC2.Visible = false;
            if (txtDebugCC2 != null) txtDebugCC2.Visible = false;

            // Optionally, adjust TableLayoutPanel row visibility or height if desired
            // For simplicity, this example just hides the controls, leaving empty space.
            // To remove rows from TableLayoutPanel (more complex):
            // mainTableLayoutPanel.RowStyles[9].Height = 0; // Assuming row 9 is Debug TO
            // mainTableLayoutPanel.RowStyles[9].SizeType = SizeType.Absolute; 
            // ... and so on for other debug rows. This requires careful index management.
        }


        private void SetupToolTips()
        {
            this.toolTipProvider ??= new System.Windows.Forms.ToolTip(this.components ??= new System.ComponentModel.Container());

            toolTipProvider.SetToolTip(this.txtProdAutoRunDailyTo, "Default 'To' for AUTOMATED standard daily reports. Separate multiple emails with comma or semicolon.");
            toolTipProvider.SetToolTip(this.txtProdAutoRunDailyCC, "Default 'CC' for AUTOMATED standard daily reports. Separate multiple emails with comma or semicolon.");

            if (this.txtProdManualRunDailyTo != null)
                toolTipProvider.SetToolTip(this.txtProdManualRunDailyTo, "Default 'To' for MANUALLY RUN standard daily reports. Separate multiple emails with comma or semicolon.");
            if (this.txtProdManualRunDailyCC != null)
                toolTipProvider.SetToolTip(this.txtProdManualRunDailyCC, "Default 'CC' for MANUALLY RUN standard daily reports. Separate multiple emails with comma or semicolon.");

            toolTipProvider.SetToolTip(this.txtProdAutoRunDaily5Day1kTo, "Default 'To' for 'Daily (5days >= £1000)' automated reports. Separate multiple emails with comma or semicolon.");
            toolTipProvider.SetToolTip(this.txtProdAutoRunDaily5Day1kCC, "Default 'CC' for 'Daily (5days >= £1000)' automated reports. Separate multiple emails with comma or semicolon.");

            toolTipProvider.SetToolTip(this.txtProdFemiTo, "'To' recipients for manual non-daily reports when 'Send to Femi Only' is checked. Separate multiple emails with comma or semicolon.");
            toolTipProvider.SetToolTip(this.txtProdFemiCC, "'CC' recipients for manual non-daily reports when 'Send to Femi Only' is checked. Separate multiple emails with comma or semicolon.");
            toolTipProvider.SetToolTip(this.txtProdTeamTo, "'To' recipients for manual non-daily reports (team list). Separate multiple emails with comma or semicolon.");
            toolTipProvider.SetToolTip(this.txtProdTeamCC, "'CC' recipients for manual non-daily reports (team list). Separate multiple emails with comma or semicolon.");

#if DEBUG
            if (this.txtDebugTo != null) toolTipProvider.SetToolTip(this.txtDebugTo, "Primary 'To' recipient for ALL reports in DEBUG mode. Separate multiple emails with comma or semicolon.");
            if (this.txtDebugCC1 != null) toolTipProvider.SetToolTip(this.txtDebugCC1, "First 'CC' recipient for ALL reports in DEBUG mode. Separate multiple emails with comma or semicolon.");
            if (this.txtDebugCC2 != null) toolTipProvider.SetToolTip(this.txtDebugCC2, "Second 'CC' recipient for ALL reports in DEBUG mode. Separate multiple emails with comma or semicolon.");
#endif

            toolTipProvider.SetToolTip(this.btnSave, "Save the current email settings. These will override application defaults.");
            toolTipProvider.SetToolTip(this.btnRestoreDefaults, "Clear all custom settings and revert to the application's built-in default email lists.");
            toolTipProvider.SetToolTip(this.btnClose, "Close this window without saving any changes made since the last save.");
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
                if (control is Button button)
                {
                    button.BackColor = buttonBackColor;
                    button.ForeColor = controlForeColor;
                    button.FlatStyle = FlatStyle.Flat;
                    button.FlatAppearance.BorderColor = isDarkMode ? Color.FromArgb(100, 100, 100) : SystemColors.ControlDarkDark;
                    button.FlatAppearance.BorderSize = 1;
                }
                else if (control is TextBox || control is RichTextBox)
                {
                    control.BackColor = controlBackColor;
                    control.ForeColor = controlForeColor;
                    if (control is TextBox tb)
                    {
                        tb.BorderStyle = isDarkMode ? BorderStyle.FixedSingle : BorderStyle.Fixed3D;
                    }
                }
                else if (control is ComboBox cb)
                {
                    cb.BackColor = controlBackColor;
                    cb.ForeColor = controlForeColor;
                    cb.FlatStyle = FlatStyle.Flat;
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
                    if (gb.Controls.Count > 0)
                    {
                        UpdateControlThemeRecursive(gb, controlBackColor, buttonBackColor, controlForeColor, isDarkMode);
                    }
                }
                else if (control is Panel || control is TabControl || control is TabPage || control is System.Windows.Forms.TableLayoutPanel)
                {
                    control.BackColor = parentControl.BackColor;
                    control.ForeColor = controlForeColor;
                    if (control.Controls.Count > 0)
                    {
                        UpdateControlThemeRecursive(control, controlBackColor, buttonBackColor, controlForeColor, isDarkMode);
                    }
                }
                else
                {
                    // Only apply to visible controls to prevent issues if debug fields are hidden
                    if (control.Visible)
                    {
                        control.BackColor = controlBackColor;
                        control.ForeColor = controlForeColor;
                    }
                }
            }
        }


        private void LoadSettingsToForm()
        {
            UserEmailSettings currentSettings = _emailRecipientManager.GetCurrentEffectiveSettings();

            txtProdAutoRunDailyTo.Text = string.Join(", ", currentSettings.ProdAutoRunDailyTo ?? Enumerable.Empty<string>());
            txtProdAutoRunDailyCC.Text = string.Join(", ", currentSettings.ProdAutoRunDailyCC ?? Enumerable.Empty<string>());

            if (this.txtProdManualRunDailyTo != null)
                txtProdManualRunDailyTo.Text = string.Join(", ", currentSettings.ProdManualRunDailyTo ?? Enumerable.Empty<string>());
            if (this.txtProdManualRunDailyCC != null)
                txtProdManualRunDailyCC.Text = string.Join(", ", currentSettings.ProdManualRunDailyCC ?? Enumerable.Empty<string>());

            txtProdAutoRunDaily5Day1kTo.Text = string.Join(", ", currentSettings.ProdAutoRunDaily5Day1kTo ?? Enumerable.Empty<string>());
            txtProdAutoRunDaily5Day1kCC.Text = string.Join(", ", currentSettings.ProdAutoRunDaily5Day1kCC ?? Enumerable.Empty<string>());

            txtProdFemiTo.Text = string.Join(", ", currentSettings.ProdFemiTo ?? Enumerable.Empty<string>());
            txtProdFemiCC.Text = string.Join(", ", currentSettings.ProdFemiCC ?? Enumerable.Empty<string>());
            txtProdTeamTo.Text = string.Join(", ", currentSettings.ProdTeamTo ?? Enumerable.Empty<string>());
            txtProdTeamCC.Text = string.Join(", ", currentSettings.ProdTeamCC ?? Enumerable.Empty<string>());

#if DEBUG
            if (txtDebugTo != null) txtDebugTo.Text = currentSettings.DebugTo ?? string.Empty;
            if (txtDebugCC1 != null) txtDebugCC1.Text = currentSettings.DebugCC1 ?? string.Empty;
            if (txtDebugCC2 != null) txtDebugCC2.Text = currentSettings.DebugCC2 ?? string.Empty;
#endif
            Logger.LogInfo("Loaded current email settings into ManageEmailRecipientsForm.");
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            Logger.LogInfo("Save button clicked on ManageEmailRecipientsForm.");
            var newSettings = new UserEmailSettings
            {
                ProdAutoRunDailyTo = StringToEmailList(txtProdAutoRunDailyTo.Text),
                ProdAutoRunDailyCC = StringToEmailList(txtProdAutoRunDailyCC.Text),
                ProdManualRunDailyTo = this.txtProdManualRunDailyTo != null ? StringToEmailList(txtProdManualRunDailyTo.Text) : new List<string>(),
                ProdManualRunDailyCC = this.txtProdManualRunDailyCC != null ? StringToEmailList(txtProdManualRunDailyCC.Text) : new List<string>(),
                ProdAutoRunDaily5Day1kTo = StringToEmailList(txtProdAutoRunDaily5Day1kTo.Text),
                ProdAutoRunDaily5Day1kCC = StringToEmailList(txtProdAutoRunDaily5Day1kCC.Text),
                ProdFemiTo = StringToEmailList(txtProdFemiTo.Text),
                ProdFemiCC = StringToEmailList(txtProdFemiCC.Text),
                ProdTeamTo = StringToEmailList(txtProdTeamTo.Text),
                ProdTeamCC = StringToEmailList(txtProdTeamCC.Text)
            };

            // Only include debug settings if they are visible (i.e., in a DEBUG build)
#if DEBUG
            if (txtDebugTo != null) newSettings.DebugTo = txtDebugTo.Text.Trim();
            if (txtDebugCC1 != null) newSettings.DebugCC1 = txtDebugCC1.Text.Trim();
            if (txtDebugCC2 != null) newSettings.DebugCC2 = txtDebugCC2.Text.Trim();
#else
            // If not in debug mode, retain existing debug settings from loaded _userOverrides
            // or from GetCurrentEffectiveSettings if _userOverrides is fresh.
            // This prevents accidentally clearing debug settings when saving in release mode.
            UserEmailSettings currentEffectiveSettings = _emailRecipientManager.GetCurrentEffectiveSettings();
            newSettings.DebugTo = currentEffectiveSettings.DebugTo;
            newSettings.DebugCC1 = currentEffectiveSettings.DebugCC1;
            newSettings.DebugCC2 = currentEffectiveSettings.DebugCC2;
#endif

            List<string> allEmailsToValidate = new List<string>();
            allEmailsToValidate.AddRange(newSettings.ProdAutoRunDailyTo ?? Enumerable.Empty<string>());
            allEmailsToValidate.AddRange(newSettings.ProdAutoRunDailyCC ?? Enumerable.Empty<string>());
            allEmailsToValidate.AddRange(newSettings.ProdManualRunDailyTo ?? Enumerable.Empty<string>());
            allEmailsToValidate.AddRange(newSettings.ProdManualRunDailyCC ?? Enumerable.Empty<string>());
            allEmailsToValidate.AddRange(newSettings.ProdAutoRunDaily5Day1kTo ?? Enumerable.Empty<string>());
            allEmailsToValidate.AddRange(newSettings.ProdAutoRunDaily5Day1kCC ?? Enumerable.Empty<string>());
            allEmailsToValidate.AddRange(newSettings.ProdFemiTo ?? Enumerable.Empty<string>());
            allEmailsToValidate.AddRange(newSettings.ProdFemiCC ?? Enumerable.Empty<string>());
            allEmailsToValidate.AddRange(newSettings.ProdTeamTo ?? Enumerable.Empty<string>());
            allEmailsToValidate.AddRange(newSettings.ProdTeamCC ?? Enumerable.Empty<string>());

            // Only validate debug emails if they were part of the save operation (i.e., in DEBUG build)
#if DEBUG
            if (!string.IsNullOrWhiteSpace(newSettings.DebugTo)) allEmailsToValidate.Add(newSettings.DebugTo);
            if (!string.IsNullOrWhiteSpace(newSettings.DebugCC1)) allEmailsToValidate.Add(newSettings.DebugCC1);
            if (!string.IsNullOrWhiteSpace(newSettings.DebugCC2)) allEmailsToValidate.Add(newSettings.DebugCC2);
#endif

            if (!EmailRecipientManager.ValidateEmailAddresses(allEmailsToValidate, out List<string> invalidEmails))
            {
                Logger.LogWarning($"Invalid email addresses found: {string.Join(", ", invalidEmails)}");
                FlexibleMessageBox.Show(this, $"The following email addresses are invalid:\n\n{string.Join("\n", invalidEmails)}\n\nPlease correct them and try again.",
                    "Invalid Email Addresses", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            DialogResult confirmSaveResult = FlexibleMessageBox.Show(this, "Do you want to save these email recipient settings for future reports?",
                "Confirm Save", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (confirmSaveResult == DialogResult.Yes)
            {
                try
                {
                    _emailRecipientManager.SaveUserOverrides(newSettings);
                    Logger.LogInfo("User confirmed and email settings saved.");
                    FlexibleMessageBox.Show(this, "Email recipient settings have been saved.",
                        "Settings Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    DialogResult = DialogResult.OK;
                    Close();
                }
                catch (Exception ex)
                {
                    Logger.LogError($"Failed to save email recipient settings: {ex.Message}", ex);
                    FlexibleMessageBox.Show(this, $"An error occurred while saving the settings:\n\n{ex.Message}",
                        "Save Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void BtnRestoreDefaults_Click(object sender, EventArgs e)
        {
            Logger.LogInfo("Restore Defaults button clicked on ManageEmailRecipientsForm.");
            DialogResult confirmRestoreResult = FlexibleMessageBox.Show(this, "Are you sure you want to restore all email recipients to the application defaults?\n\nThis will remove any custom settings you have saved.",
                "Confirm Restore Defaults", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

            if (confirmRestoreResult == DialogResult.Yes)
            {
                try
                {
                    _emailRecipientManager.ClearUserOverrides();
                    LoadSettingsToForm(); // Reloads defaults (as overrides are now empty)
                    Logger.LogInfo("User confirmed and email settings restored to defaults.");
                    FlexibleMessageBox.Show(this, "Email recipient settings have been restored to application defaults.",
                        "Defaults Restored", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    Logger.LogError($"Failed to restore default email recipient settings: {ex.Message}", ex);
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

        private List<string> StringToEmailList(string emailString)
        {
            if (string.IsNullOrWhiteSpace(emailString))
            {
                return new List<string>();
            }
            return emailString.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                              .Select(email => email.Trim())
                              .Where(email => !string.IsNullOrWhiteSpace(email))
                              .ToList();
        }
    }
}
