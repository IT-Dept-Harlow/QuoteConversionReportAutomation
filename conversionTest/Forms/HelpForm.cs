// HelpForm.cs
// Displays help information using a RichTextBox.
// Now uses the centralized ThemeSettings for all theming decisions and colors.

#region Using Directives
using QuoteConversionReportAutomation.Helpers;
using QuoteConversionReportAutomation.Managers;
using QuoteConversionReportAutomation.Services.Logging;
using QuoteConversionReportAutomation.Theming; // For ThemeSettings
using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
#endregion

namespace QuoteConversionReportAutomation.Forms
{
    /// <summary>
    /// A dedicated form to display help information using a RichTextBox.
    /// Theming is now driven by the central ThemeSettings class.
    /// </summary>
    public partial class HelpForm : Form
    {
        #region Fields
        // Control fields (rtbHelpContent, btnClose) are defined in HelpForm.Designer.cs
        private readonly string _rtfContent; // Store the RTF content
        #endregion

        #region Constructor
        /// <summary>
        /// Initializes a new instance of the HelpForm class.
        /// The theme (dark/light) is determined globally by ThemeSettings.
        /// </summary>
        /// <param name="title">The title for the help window.</param>
        /// <param name="rtfContent">The help content formatted as RTF.</param>
        public HelpForm(string title, string rtfContent)
        {
            InitializeComponent(); // This initializes this.rtbHelpContent and this.btnClose

            this.Text = title;
            _rtfContent = rtfContent;

            // Configure Form Properties
            this.StartPosition = FormStartPosition.CenterParent;
            this.Size = new Size(650, 500);
            this.MinimumSize = new Size(450, 350);
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
        }
        #endregion

        #region Form Load and Theming
        /// <summary>
        /// Handles the Load event of the form. Applies the theme and sets the RichTextBox content.
        /// </summary>
        private void HelpForm_Load(object sender, EventArgs e)
        {
            // Determine effective dark mode based on ThemeSettings for applying to this form.
            bool isEffectivelyDark = ThemeSettings.IsCurrentlyDark();
            Logger.LogInfo($"HelpForm loading. Effective DarkMode: {isEffectivelyDark}, Custom Theming Enabled: {ThemeSettings.EnableCustomTheming}");

            // Apply the overall form theme (frame, title bar) from the central UIManager.
            UIManager.ApplyThemeToExternalForm(this, isEffectivelyDark);

            // Apply specific theming to child controls of this HelpForm.
            ApplyChildControlTheme();

            try
            {
                if (this.rtbHelpContent != null)
                {
                    this.rtbHelpContent.Rtf = _rtfContent;
                    // Set default ForeColor for RTF content if custom dark mode is active
                    if (ThemeSettings.EnableCustomTheming && isEffectivelyDark)
                    {
                        //this.rtbHelpContent.ForeColor = ThemeSettings.DarkPalette.ControlForeColor;
                    }
                    else if (!ThemeSettings.EnableCustomTheming)
                    {
                        this.rtbHelpContent.ResetForeColor();
                    }
                }
                else
                {
                    Logger.LogError("HelpForm_Load: rtbHelpContent is null after InitializeComponent.");
                }
            }
            catch (ArgumentException ex)
            {
                Logger.LogError($"Invalid RTF content provided to HelpForm: {ex.Message}");
                if (this.rtbHelpContent != null) this.rtbHelpContent.Text = "Error loading help content. Invalid RTF format.";
            }
            catch (Exception ex)
            {
                Logger.LogError($"Unexpected error loading RTF content in HelpForm: {ex.Message}");
                if (this.rtbHelpContent != null) this.rtbHelpContent.Text = "An unexpected error occurred loading help content.";
            }
            Logger.LogInfo("HelpForm loaded and themed.");
        }

        /// <summary>
        /// Applies theme colors specifically to the child controls of the HelpForm,
        /// based on global ThemeSettings.
        /// </summary>
        private void ApplyChildControlTheme()
        {
            bool isCurrentlyDarkEffective = ThemeSettings.IsCurrentlyDark();
            ThemePalette palette = ThemeSettings.CurrentPalette;

            Logger.LogDebug($"Applying child control theme to HelpForm. Effective DarkMode: {isCurrentlyDarkEffective}, Custom Theming: {ThemeSettings.EnableCustomTheming}");

            // RichTextBox
            if (this.rtbHelpContent != null)
            {
                if (ThemeSettings.EnableCustomTheming)
                {
                    this.rtbHelpContent.BackColor = palette.ControlBackColor;
                    this.rtbHelpContent.BorderStyle = isCurrentlyDarkEffective ? BorderStyle.FixedSingle : BorderStyle.Fixed3D;
                }
                else
                {
                    this.rtbHelpContent.ResetBackColor();
                    this.rtbHelpContent.ResetForeColor();
                    this.rtbHelpContent.BorderStyle = BorderStyle.Fixed3D;
                }
            }

            // Close Button
            if (this.btnClose != null)
            {
                if (ThemeSettings.EnableCustomTheming)
                {
                    this.btnClose.BackColor = palette.ButtonBackColor;
                    this.btnClose.ForeColor = palette.ButtonForeColor;
                    this.btnClose.FlatStyle = FlatStyle.Flat;
                    this.btnClose.FlatAppearance.BorderColor = palette.ButtonBorderColor;
                    this.btnClose.FlatAppearance.BorderSize = 1;
                }
                else
                {
                    this.btnClose.ResetBackColor();
                    this.btnClose.ResetForeColor();
                    this.btnClose.FlatStyle = FlatStyle.System;
                    this.btnClose.FlatAppearance.BorderSize = 0;
                }
            }
        }
        #endregion

        #region Event Handlers
        /// <summary>
        /// Handles clicking the Close button.
        /// </summary>
        private void BtnClose_Click(object sender, EventArgs e)
        {
            Logger.LogTrace("HelpForm close button clicked.");
            this.Close();
        }

        /// <summary>
        /// Handles clicking on a link within the RichTextBox. Opens the link in the default browser.
        /// </summary>
        private void RtbHelpContent_LinkClicked(object sender, LinkClickedEventArgs e)
        {
            if (e.LinkText != null)
            {
                try
                {
                    Logger.LogInfo($"Opening help link: {e.LinkText}");
                    Process.Start(new ProcessStartInfo(e.LinkText) { UseShellExecute = true });
                }
                catch (Exception ex)
                {
                    Logger.LogError($"Failed to open link '{e.LinkText}' from help form: {ex.Message}");
                    FlexibleMessageBox.Show(this, $"Could not open the link:\n{e.LinkText}\n\nError: {ex.Message}",
                                    "Link Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
        #endregion
    }
}