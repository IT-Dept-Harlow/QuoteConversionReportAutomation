using QuoteConversionReportAutomation.Managers; // Required to access UIManager
using QuoteConversionReportAutomation.Services.Logging;
using System;
using System.Diagnostics;
using System.Drawing;
// using System.Runtime.InteropServices; // No longer needed here for title bar P/Invokes
using System.Windows.Forms;


namespace conversionTest // Use the namespace of your main project
{
    /// <summary>
    /// A dedicated form to display help information using a RichTextBox.
    /// Includes functionality to adapt its title bar to dark/light mode via UIManager.
    /// </summary>
    public partial class HelpForm : Form
    {
        // Note: P/Invoke declarations for DwmSetWindowAttribute, RedrawWindow, SendMessageTimeout
        // have been removed from this form and will be called via UIManager.ApplyThemeToExternalForm.

        // --- Fields ---
        private RichTextBox rtbHelpContent;
        private Button btnClose;
        private readonly string _rtfContent; // Store the RTF content
        private readonly bool _isDarkMode; // Store the theme state passed from the parent form

        /// <summary>
        /// Initializes a new instance of the HelpForm class.
        /// </summary>
        /// <param name="title">The title for the help window.</param>
        /// <param name="rtfContent">The help content formatted as RTF.</param>
        /// <param name="isDarkMode">Indicates whether dark mode should be applied to this form.</param>
        public HelpForm(string title, string rtfContent, bool isDarkMode)
        {
            InitializeComponent();

            Text = title;
            _rtfContent = rtfContent;
            _isDarkMode = isDarkMode;

            // --- Configure Form Properties ---
            StartPosition = FormStartPosition.CenterParent;
            Size = new Size(650, 500);
            MinimumSize = new Size(450, 350);
            FormBorderStyle = FormBorderStyle.Sizable;
            ShowIcon = false;
            ShowInTaskbar = false;
        }

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            rtbHelpContent = new System.Windows.Forms.RichTextBox();
            btnClose = new System.Windows.Forms.Button();
            SuspendLayout();
            // 
            // rtbHelpContent
            // 
            rtbHelpContent.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            rtbHelpContent.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            rtbHelpContent.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            rtbHelpContent.Location = new System.Drawing.Point(12, 12);
            rtbHelpContent.Name = "rtbHelpContent";
            rtbHelpContent.ReadOnly = true;
            rtbHelpContent.ScrollBars = System.Windows.Forms.RichTextBoxScrollBars.Vertical;
            rtbHelpContent.Size = new System.Drawing.Size(610, 400);
            rtbHelpContent.TabIndex = 0;
            rtbHelpContent.Text = "";
            rtbHelpContent.DetectUrls = true;
            rtbHelpContent.LinkClicked += new System.Windows.Forms.LinkClickedEventHandler(RtbHelpContent_LinkClicked);
            // 
            // btnClose
            // 
            btnClose.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            btnClose.DialogResult = System.Windows.Forms.DialogResult.OK;
            btnClose.Location = new System.Drawing.Point(547, 426);
            btnClose.Name = "btnClose";
            btnClose.Size = new System.Drawing.Size(75, 23);
            btnClose.TabIndex = 1;
            btnClose.Text = "Close";
            btnClose.UseVisualStyleBackColor = true;
            btnClose.Click += new System.EventHandler(BtnClose_Click);
            // 
            // HelpForm
            // 
            AcceptButton = btnClose;
            AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(634, 461);
            Controls.Add(btnClose);
            Controls.Add(rtbHelpContent);
            Name = "HelpForm";
            Text = "Help";
            Load += new System.EventHandler(HelpForm_Load);
            ResumeLayout(false);
        }

        /// <summary>
        /// Handles the Load event of the form. Applies the theme and sets the RichTextBox content.
        /// </summary>
        private void HelpForm_Load(object sender, EventArgs e)
        {
            Logger.LogTrace($"HelpForm loading. Initial DarkMode state: {_isDarkMode}");

            // Apply the overall form theme (including title bar and basic BackColor/ForeColor) using UIManager.
            // This centralizes the P/Invoke logic for title bar theming.
            UIManager.ApplyThemeToExternalForm(this, _isDarkMode);

            // Now, apply specific theming to child controls of this HelpForm.
            ApplyChildControlTheme(_isDarkMode);

            // Load the RTF content
            try
            {
                rtbHelpContent.Rtf = _rtfContent;
                // After loading RTF, if in dark mode, try to set a default light ForeColor.
                // This will only affect text not explicitly colored by the RTF itself.
                if (_isDarkMode)
                {
                    rtbHelpContent.ForeColor = Color.FromArgb(220, 220, 220); // Light gray/off-white
                }
            }
            catch (ArgumentException ex)
            {
                Logger.LogError($"Invalid RTF content provided to HelpForm: {ex.Message}");
                rtbHelpContent.Text = "Error loading help content. Invalid RTF format.";
            }
            catch (Exception ex)
            {
                Logger.LogError($"Unexpected error loading RTF content in HelpForm: {ex.Message}");
                rtbHelpContent.Text = "An unexpected error occurred loading help content.";
            }
        }

        /// <summary>
        /// Applies theme colors specifically to the child controls of the HelpForm.
        /// The main form's BackColor, ForeColor, and title bar are handled by UIManager.ApplyThemeToExternalForm.
        /// </summary>
        /// <param name="isDarkModeEnabled">True to apply dark mode, false for light mode.</param>
        private void ApplyChildControlTheme(bool isDarkModeEnabled)
        {
            Logger.LogDebug($"Applying child control theme to HelpForm. DarkMode: {isDarkModeEnabled}");

            // Define colors for child controls
            Color rtbBackColor = isDarkModeEnabled ? Color.FromArgb(50, 50, 53) : SystemColors.Window;
            Color rtbForeColor = isDarkModeEnabled ? Color.FromArgb(220, 220, 220) : SystemColors.WindowText; // Default text color

            Color btnBackColor = isDarkModeEnabled ? Color.FromArgb(80, 80, 80) : SystemColors.Control;
            Color btnForeColor = isDarkModeEnabled ? Color.White : SystemColors.ControlText;
            Color btnBorderColor = isDarkModeEnabled ? Color.FromArgb(100, 100, 100) : Color.DarkGray;


            // Apply colors to RichTextBox
            rtbHelpContent.BackColor = rtbBackColor;
            // Setting ForeColor here is a fallback. RTF content often defines its own colors.
            // We also set it after loading RTF in HelpForm_Load for a better chance.
            rtbHelpContent.ForeColor = rtbForeColor;

            // Apply colors to Button
            btnClose.BackColor = btnBackColor;
            btnClose.ForeColor = btnForeColor;
            btnClose.FlatStyle = FlatStyle.Flat;
            btnClose.FlatAppearance.BorderColor = btnBorderColor;
            btnClose.FlatAppearance.BorderSize = 1;
        }


        /// <summary>
        /// Handles clicking the Close button.
        /// </summary>
        private void BtnClose_Click(object sender, EventArgs e)
        {
            Logger.LogTrace("HelpForm close button clicked.");
            Close(); // Close the form
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
                    MessageBox.Show($"Could not open the link:\n{e.LinkText}\n\nError: {ex.Message}",
                                    "Link Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
    }

    // Partial class for designer components (usually in HelpForm.Designer.cs)
    partial class HelpForm
    {
        private System.ComponentModel.IContainer components = null;
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
