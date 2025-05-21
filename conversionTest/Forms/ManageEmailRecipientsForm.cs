// ManageEmailRecipientsForm.cs
namespace QuoteConversionReportAutomation
{
    using QuoteConversionReportAutomation.Helpers;
    using QuoteConversionReportAutomation.Managers;
    using QuoteConversionReportAutomation.Models;
    using QuoteConversionReportAutomation.Services.Logging;
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Linq;
    using System.Windows.Forms;

    public partial class ManageEmailRecipientsForm : Form
    {
        private readonly EmailRecipientManager _emailRecipientManager;
        private readonly bool _isDarkMode;

        // Theme Colors
        private static readonly Color DM_ControlBackColor = Color.FromArgb(45, 45, 48);
        private static readonly Color DM_TabPageBackColor = Color.FromArgb(37, 37, 38);
        private static readonly Color DM_TabControlBackColor = Color.FromArgb(28, 28, 28);
        private static readonly Color DM_ButtonBackColor = Color.FromArgb(60, 60, 63);
        private static readonly Color DM_ControlForeColor = Color.WhiteSmoke; // Lighter white for better contrast
        private static readonly Color DM_LabelForeColor = Color.FromArgb(200, 200, 200); // Slightly dimmer for labels

        private static readonly Color LM_ControlBackColor = SystemColors.Window;
        private static readonly Color LM_TabPageBackColor = SystemColors.Control;
        private static readonly Color LM_TabControlBackColor = SystemColors.Control;
        private static readonly Color LM_ButtonBackColor = SystemColors.ControlLight;
        private static readonly Color LM_ControlForeColor = SystemColors.ControlText;


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
            ApplyThemeToTabbedLayout(_isDarkMode);
            LoadSettingsToForm();
            SetupToolTips();

#if !DEBUG
            if (mainTabControl.TabPages.Contains(debugTabPage))
            {
                mainTabControl.TabPages.Remove(debugTabPage);
                Logger.LogInfo("Release mode: Removed Debug recipients tab page.");
            }
#endif
            Logger.LogInfo("ManageEmailRecipientsForm loaded and themed.");
        }

        private void ApplyThemeToTabbedLayout(bool isDarkModeEnabled)
        {
            this.BackColor = isDarkModeEnabled ? DM_TabControlBackColor : LM_TabControlBackColor; // Form background
            this.lblInstructions.ForeColor = isDarkModeEnabled ? DM_LabelForeColor : LM_ControlForeColor;
            this.lblInstructions.BackColor = Color.Transparent;


            mainTabControl.BackColor = isDarkModeEnabled ? DM_TabControlBackColor : LM_TabControlBackColor;

            foreach (TabPage tabPage in mainTabControl.TabPages)
            {
                tabPage.BackColor = isDarkModeEnabled ? DM_TabPageBackColor : LM_TabPageBackColor;
                // For Tab text color, you might need to handle DrawItem event if default theming isn't enough
                tabPage.ForeColor = isDarkModeEnabled ? DM_ControlForeColor : LM_ControlForeColor;

                if (tabPage.Controls.Count > 0 && tabPage.Controls[0] is TableLayoutPanel tlp)
                {
                    tlp.BackColor = tabPage.BackColor;
                    ApplyThemeToControlsRecursive(tlp, isDarkModeEnabled);
                }
            }

            buttonsFlowLayoutPanel.BackColor = this.BackColor;
            ApplyThemeToControlsRecursive(buttonsFlowLayoutPanel, isDarkModeEnabled);
        }

        private void ApplyThemeToControlsRecursive(Control parentControl, bool isDarkModeEnabled)
        {
            Color controlBackColor = isDarkModeEnabled ? DM_ControlBackColor : LM_ControlBackColor;
            Color buttonBackColor = isDarkModeEnabled ? DM_ButtonBackColor : LM_ButtonBackColor;
            Color controlForeColor = isDarkModeEnabled ? DM_ControlForeColor : LM_ControlForeColor;
            Color labelForeColor = isDarkModeEnabled ? DM_LabelForeColor : LM_ControlForeColor;


            foreach (Control control in parentControl.Controls)
            {
                if (control is Button button)
                {
                    button.BackColor = buttonBackColor;
                    button.ForeColor = controlForeColor;
                    button.FlatStyle = FlatStyle.Flat;
                    button.FlatAppearance.BorderColor = isDarkModeEnabled ? Color.FromArgb(100, 100, 100) : SystemColors.ControlDarkDark;
                    button.FlatAppearance.BorderSize = 1;
                }
                else if (control is TextBox || control is RichTextBox)
                {
                    control.BackColor = controlBackColor;
                    control.ForeColor = controlForeColor;
                    if (control is TextBox tb)
                    {
                        tb.BorderStyle = BorderStyle.FixedSingle;
                    }
                }
                else if (control is Label)
                {
                    control.BackColor = Color.Transparent;
                    control.ForeColor = labelForeColor; // Use specific label color
                }
                else if (control.HasChildren)
                {
                    if (!(control is TableLayoutPanel || control is TabPage || control is TabControl))
                    {
                        control.BackColor = parentControl.BackColor; // Match parent for other containers
                    }
                    ApplyThemeToControlsRecursive(control, isDarkModeEnabled);
                }
            }
        }


        private void SetupToolTips()
        {
            this.toolTipProvider ??= new System.Windows.Forms.ToolTip(this.components ??= new System.ComponentModel.Container());

            // Automated Reports Tab
            toolTipProvider.SetToolTip(this.txtProdAutoRunDailyTo, "Default 'To' for AUTOMATED standard daily reports. Separate multiple emails with comma or semicolon.");
            toolTipProvider.SetToolTip(this.txtProdAutoRunDailyCC, "Default 'CC' for AUTOMATED standard daily reports. Separate multiple emails with comma or semicolon.");
            toolTipProvider.SetToolTip(this.txtProdAutoRunDaily5Day1kTo, "Default 'To' for 'Daily (5days >= £1000)' automated reports. Separate multiple emails with comma or semicolon.");
            toolTipProvider.SetToolTip(this.txtProdAutoRunDaily5Day1kCC, "Default 'CC' for 'Daily (5days >= £1000)' automated reports. Separate multiple emails with comma or semicolon.");
            toolTipProvider.SetToolTip(this.txtProdAutoRunWeeklyTo, "Default 'To' for AUTOMATED weekly (15-day) reports. Separate multiple emails with comma or semicolon.");
            toolTipProvider.SetToolTip(this.txtProdAutoRunWeeklyCC, "Default 'CC' for AUTOMATED weekly (15-day) reports. Separate multiple emails with comma or semicolon.");

            // Manual Reports Tab
            toolTipProvider.SetToolTip(this.txtProdManualRunDailyTo, "Default 'To' for MANUALLY RUN standard daily reports. Separate multiple emails with comma or semicolon.");
            toolTipProvider.SetToolTip(this.txtProdManualRunDailyCC, "Default 'CC' for MANUALLY RUN standard daily reports. Separate multiple emails with comma or semicolon.");
            toolTipProvider.SetToolTip(this.txtProdFemiTo, "'To' recipients for manual non-daily reports when 'Send to Femi Only' is checked. Separate multiple emails with comma or semicolon.");
            toolTipProvider.SetToolTip(this.txtProdFemiCC, "'CC' recipients for manual non-daily reports when 'Send to Femi Only' is checked. Separate multiple emails with comma or semicolon.");
            toolTipProvider.SetToolTip(this.txtProdTeamTo, "'To' recipients for manual non-daily reports (team list). Separate multiple emails with comma or semicolon.");
            toolTipProvider.SetToolTip(this.txtProdTeamCC, "'CC' recipients for manual non-daily reports (team list). Separate multiple emails with comma or semicolon.");

#if DEBUG
            // Debug Tab
            toolTipProvider.SetToolTip(this.txtDebugTo, "Primary 'To' recipient for ALL reports in DEBUG mode. Separate multiple emails with comma or semicolon.");
            toolTipProvider.SetToolTip(this.txtDebugCC1, "First 'CC' recipient for ALL reports in DEBUG mode. Separate multiple emails with comma or semicolon.");
            toolTipProvider.SetToolTip(this.txtDebugCC2, "Second 'CC' recipient for ALL reports in DEBUG mode. Separate multiple emails with comma or semicolon.");
#endif

            toolTipProvider.SetToolTip(this.btnSave, "Save the current email settings. These will override application defaults.");
            toolTipProvider.SetToolTip(this.btnRestoreDefaults, "Clear all custom settings and revert to the application's built-in default email lists.");
            toolTipProvider.SetToolTip(this.btnClose, "Close this window without saving any changes made since the last save.");
        }

        private void LoadSettingsToForm()
        {
            UserEmailSettings currentSettings = _emailRecipientManager.GetCurrentEffectiveSettings();

            // Automated Reports Tab
            txtProdAutoRunDailyTo.Text = string.Join(", ", currentSettings.ProdAutoRunDailyTo ?? Enumerable.Empty<string>());
            txtProdAutoRunDailyCC.Text = string.Join(", ", currentSettings.ProdAutoRunDailyCC ?? Enumerable.Empty<string>());
            txtProdAutoRunDaily5Day1kTo.Text = string.Join(", ", currentSettings.ProdAutoRunDaily5Day1kTo ?? Enumerable.Empty<string>());
            txtProdAutoRunDaily5Day1kCC.Text = string.Join(", ", currentSettings.ProdAutoRunDaily5Day1kCC ?? Enumerable.Empty<string>());
            txtProdAutoRunWeeklyTo.Text = string.Join(", ", currentSettings.ProdAutoRunWeeklyTo ?? Enumerable.Empty<string>());
            txtProdAutoRunWeeklyCC.Text = string.Join(", ", currentSettings.ProdAutoRunWeeklyCC ?? Enumerable.Empty<string>());

            // Manual Reports Tab
            txtProdManualRunDailyTo.Text = string.Join(", ", currentSettings.ProdManualRunDailyTo ?? Enumerable.Empty<string>());
            txtProdManualRunDailyCC.Text = string.Join(", ", currentSettings.ProdManualRunDailyCC ?? Enumerable.Empty<string>());
            txtProdFemiTo.Text = string.Join(", ", currentSettings.ProdFemiTo ?? Enumerable.Empty<string>());
            txtProdFemiCC.Text = string.Join(", ", currentSettings.ProdFemiCC ?? Enumerable.Empty<string>());
            txtProdTeamTo.Text = string.Join(", ", currentSettings.ProdTeamTo ?? Enumerable.Empty<string>());
            txtProdTeamCC.Text = string.Join(", ", currentSettings.ProdTeamCC ?? Enumerable.Empty<string>());

#if DEBUG
            // Debug Tab
            txtDebugTo.Text = currentSettings.DebugTo ?? string.Empty;
            txtDebugCC1.Text = currentSettings.DebugCC1 ?? string.Empty;
            txtDebugCC2.Text = currentSettings.DebugCC2 ?? string.Empty;
#endif
            Logger.LogInfo("Loaded current email settings into ManageEmailRecipientsForm.");
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            Logger.LogInfo("Save button clicked on ManageEmailRecipientsForm.");
            var newSettings = new UserEmailSettings
            {
                // Automated Reports
                ProdAutoRunDailyTo = StringToEmailList(txtProdAutoRunDailyTo.Text),
                ProdAutoRunDailyCC = StringToEmailList(txtProdAutoRunDailyCC.Text),
                ProdAutoRunDaily5Day1kTo = StringToEmailList(txtProdAutoRunDaily5Day1kTo.Text),
                ProdAutoRunDaily5Day1kCC = StringToEmailList(txtProdAutoRunDaily5Day1kCC.Text),
                ProdAutoRunWeeklyTo = StringToEmailList(txtProdAutoRunWeeklyTo.Text),
                ProdAutoRunWeeklyCC = StringToEmailList(txtProdAutoRunWeeklyCC.Text),
                // Manual Reports
                ProdManualRunDailyTo = StringToEmailList(txtProdManualRunDailyTo.Text),
                ProdManualRunDailyCC = StringToEmailList(txtProdManualRunDailyCC.Text),
                ProdFemiTo = StringToEmailList(txtProdFemiTo.Text),
                ProdFemiCC = StringToEmailList(txtProdFemiCC.Text),
                ProdTeamTo = StringToEmailList(txtProdTeamTo.Text),
                ProdTeamCC = StringToEmailList(txtProdTeamCC.Text)
            };

#if DEBUG
            // Debug Settings
            newSettings.DebugTo = txtDebugTo.Text.Trim();
            newSettings.DebugCC1 = txtDebugCC1.Text.Trim();
            newSettings.DebugCC2 = txtDebugCC2.Text.Trim();
#else
            // Retain existing debug settings if not in DEBUG mode to prevent accidental clearing
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
            allEmailsToValidate.AddRange(newSettings.ProdAutoRunWeeklyTo ?? Enumerable.Empty<string>());
            allEmailsToValidate.AddRange(newSettings.ProdAutoRunWeeklyCC ?? Enumerable.Empty<string>());
            allEmailsToValidate.AddRange(newSettings.ProdFemiTo ?? Enumerable.Empty<string>());
            allEmailsToValidate.AddRange(newSettings.ProdFemiCC ?? Enumerable.Empty<string>());
            allEmailsToValidate.AddRange(newSettings.ProdTeamTo ?? Enumerable.Empty<string>());
            allEmailsToValidate.AddRange(newSettings.ProdTeamCC ?? Enumerable.Empty<string>());

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
                    LoadSettingsToForm();
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
