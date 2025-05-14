// ManageEmailRecipientsForm.cs
// Make sure the namespace matches your project structure, e.g., QuoteConversionReportAutomation or conversionTest
namespace QuoteConversionReportAutomation
{
    using QuoteConversionReportAutomation.Helpers;
    using QuoteConversionReportAutomation.Managers; // Required to access UIManager
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
    /// </summary>
    public partial class ManageEmailRecipientsForm : Form
    {
        private readonly EmailRecipientManager _emailRecipientManager;
        private readonly bool _isDarkMode; // Stores the theme state passed from the parent form

        // Theme Colors for child controls (consistent with UIManager's control/button colors)
        // Form's direct BackColor/ForeColor will be set by UIManager.ApplyThemeToExternalForm
        private static readonly Color DM_ControlBackColor = Color.FromArgb(60, 60, 63);
        private static readonly Color DM_ButtonBackColor = Color.FromArgb(80, 80, 80);
        private static readonly Color DM_ControlForeColor = Color.White; // General foreground for controls in dark mode

        private static readonly Color LM_ControlBackColor = SystemColors.Window;
        private static readonly Color LM_ButtonBackColor = SystemColors.Control;
        private static readonly Color LM_ControlForeColor = SystemColors.ControlText; // General foreground for controls in light mode


        /// <summary>
        /// Initializes a new instance of the <see cref="ManageEmailRecipientsForm"/> class.
        /// </summary>
        /// <param name="emailRecipientManager">The manager responsible for handling email recipient logic.</param>
        /// <param name="isDarkMode">Flag indicating if dark mode should be applied.</param>
        public ManageEmailRecipientsForm(EmailRecipientManager emailRecipientManager, bool isDarkMode)
        {
            _emailRecipientManager = emailRecipientManager ?? throw new ArgumentNullException(nameof(emailRecipientManager));
            _isDarkMode = isDarkMode;

            InitializeComponent();
            // Set ShowIcon to false if you don't want an icon in the title bar
            this.ShowIcon = false;
            this.StartPosition = FormStartPosition.CenterParent;
        }

        /// <summary>
        /// Handles the Load event of the form.
        /// Applies the theme (including title bar via UIManager) and loads settings.
        /// </summary>
        private void ManageEmailRecipientsForm_Load(object sender, EventArgs e)
        {
            Logger.LogInfo($"ManageEmailRecipientsForm loading. Initial DarkMode state: {_isDarkMode}");

            // Apply the overall form theme (title bar, main BackColor/ForeColor) using UIManager.
            UIManager.ApplyThemeToExternalForm(this, _isDarkMode);

            // Apply theme specifically to the child controls of this form.
            ApplyChildControlTheme(_isDarkMode);

            LoadSettingsToForm();
            Logger.LogInfo("ManageEmailRecipientsForm loaded and themed.");
        }

        /// <summary>
        /// Applies the current theme (dark or light) specifically to the child controls of this form.
        /// The main form's BackColor, ForeColor, and title bar are handled by UIManager.ApplyThemeToExternalForm.
        /// </summary>
        private void ApplyChildControlTheme(bool isDarkModeEnabled)
        {
            // Determine colors for child controls based on the theme
            Color controlBackColor = isDarkModeEnabled ? DM_ControlBackColor : LM_ControlBackColor;
            Color buttonBackColor = isDarkModeEnabled ? DM_ButtonBackColor : LM_ButtonBackColor;
            Color controlForeColor = isDarkModeEnabled ? DM_ControlForeColor : LM_ControlForeColor;
            // Form's direct BackColor and ForeColor are already set by UIManager.ApplyThemeToExternalForm

            // Apply to all controls recursively within this form
            UpdateControlThemeRecursive(this, controlBackColor, buttonBackColor, controlForeColor, isDarkModeEnabled);
        }

        /// <summary>
        /// Recursive helper to apply theme colors to child controls.
        /// </summary>
        private void UpdateControlThemeRecursive(Control parentControl, Color controlBackColor, Color buttonBackColor, Color controlForeColor, bool isDarkMode)
        {
            // Do not change parentControl.BackColor here, as it's set by UIManager.ApplyThemeToExternalForm
            // parentControl.ForeColor is also set by UIManager.ApplyThemeToExternalForm

            foreach (Control control in parentControl.Controls)
            {
                if (control is Button button)
                {
                    button.BackColor = buttonBackColor;
                    button.ForeColor = controlForeColor;
                    button.FlatStyle = FlatStyle.Flat;
                    button.FlatAppearance.BorderColor = isDarkMode ? Color.DarkGray : SystemColors.ControlDarkDark;
                    button.FlatAppearance.BorderSize = 1;
                }
                else if (control is TextBox || control is RichTextBox) // Assuming RichTextBox might be used
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
                else if (control is Label) // Labels should typically have transparent background
                {
                    control.BackColor = Color.Transparent; // Make label background transparent
                    control.ForeColor = controlForeColor;  // Use the general control foreground color
                }
                else if (control is GroupBox gb)
                {
                    gb.ForeColor = controlForeColor; // GroupBox text color
                    // GroupBox background should match the form's background for a blended look
                    gb.BackColor = this.BackColor; // Use the form's already set BackColor
                    if (gb.Controls.Count > 0)
                    {
                        UpdateControlThemeRecursive(gb, controlBackColor, buttonBackColor, controlForeColor, isDarkMode);
                    }
                }
                else if (control is Panel || control is TabControl || control is TabPage)
                {
                    // Container controls' background should match the form's background
                    control.BackColor = this.BackColor;
                    control.ForeColor = controlForeColor;
                    if (control.Controls.Count > 0)
                    {
                        UpdateControlThemeRecursive(control, controlBackColor, buttonBackColor, controlForeColor, isDarkMode);
                    }
                }
                else // For other simple controls not explicitly handled
                {
                    control.BackColor = controlBackColor; // Fallback, might need adjustment
                    control.ForeColor = controlForeColor;
                }
            }
        }


        /// <summary>
        /// Loads the current effective email settings into the form's textboxes.
        /// </summary>
        private void LoadSettingsToForm()
        {
            UserEmailSettings currentSettings = _emailRecipientManager.GetCurrentEffectiveSettings();

            txtProdAutoRunDailyTo.Text = string.Join(", ", currentSettings.ProdAutoRunDailyTo ?? Enumerable.Empty<string>());
            txtProdAutoRunDailyCC.Text = string.Join(", ", currentSettings.ProdAutoRunDailyCC ?? Enumerable.Empty<string>());
            txtProdFemiTo.Text = string.Join(", ", currentSettings.ProdFemiTo ?? Enumerable.Empty<string>());
            txtProdFemiCC.Text = string.Join(", ", currentSettings.ProdFemiCC ?? Enumerable.Empty<string>());
            txtProdTeamTo.Text = string.Join(", ", currentSettings.ProdTeamTo ?? Enumerable.Empty<string>());
            txtProdTeamCC.Text = string.Join(", ", currentSettings.ProdTeamCC ?? Enumerable.Empty<string>());

            txtDebugTo.Text = currentSettings.DebugTo ?? string.Empty;
            txtDebugCC1.Text = currentSettings.DebugCC1 ?? string.Empty;
            txtDebugCC2.Text = currentSettings.DebugCC2 ?? string.Empty;
            Logger.LogInfo("Loaded current email settings into ManageEmailRecipientsForm.");
        }

        /// <summary>
        /// Handles the Click event of the Save button.
        /// Validates and saves the user's changes to email recipients.
        /// </summary>
        private void BtnSave_Click(object sender, EventArgs e)
        {
            Logger.LogInfo("Save button clicked on ManageEmailRecipientsForm.");
            UserEmailSettings newSettings = new UserEmailSettings
            {
                ProdAutoRunDailyTo = StringToEmailList(txtProdAutoRunDailyTo.Text),
                ProdAutoRunDailyCC = StringToEmailList(txtProdAutoRunDailyCC.Text),
                ProdFemiTo = StringToEmailList(txtProdFemiTo.Text),
                ProdFemiCC = StringToEmailList(txtProdFemiCC.Text),
                ProdTeamTo = StringToEmailList(txtProdTeamTo.Text),
                ProdTeamCC = StringToEmailList(txtProdTeamCC.Text),
                DebugTo = txtDebugTo.Text.Trim(),
                DebugCC1 = txtDebugCC1.Text.Trim(),
                DebugCC2 = txtDebugCC2.Text.Trim()
            };

            // Consolidate all emails for validation
            List<string> allEmailsToValidate = new List<string>();
            allEmailsToValidate.AddRange(newSettings.ProdAutoRunDailyTo ?? Enumerable.Empty<string>());
            allEmailsToValidate.AddRange(newSettings.ProdAutoRunDailyCC ?? Enumerable.Empty<string>());
            allEmailsToValidate.AddRange(newSettings.ProdFemiTo ?? Enumerable.Empty<string>());
            allEmailsToValidate.AddRange(newSettings.ProdFemiCC ?? Enumerable.Empty<string>());
            allEmailsToValidate.AddRange(newSettings.ProdTeamTo ?? Enumerable.Empty<string>());
            allEmailsToValidate.AddRange(newSettings.ProdTeamCC ?? Enumerable.Empty<string>());
            if (!string.IsNullOrWhiteSpace(newSettings.DebugTo)) allEmailsToValidate.Add(newSettings.DebugTo);
            if (!string.IsNullOrWhiteSpace(newSettings.DebugCC1)) allEmailsToValidate.Add(newSettings.DebugCC1);
            if (!string.IsNullOrWhiteSpace(newSettings.DebugCC2)) allEmailsToValidate.Add(newSettings.DebugCC2);

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
                    DialogResult = DialogResult.OK; // Indicate settings were changed
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

        /// <summary>
        /// Handles the Click event of the Restore Defaults button.
        /// Clears any user-defined overrides and reloads application defaults.
        /// </summary>
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
                    LoadSettingsToForm(); // Reload defaults into the form
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

        /// <summary>
        /// Handles the Click event of the Close button.
        /// </summary>
        private void BtnClose_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        /// <summary>
        /// Converts a comma or semicolon-separated string of emails into a list of strings.
        /// </summary>
        /// <param name="emailString">The string containing email addresses.</param>
        /// <returns>A list of trimmed email addresses. Returns an empty list if input is null or whitespace.</returns>
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
