using System;
using System.Diagnostics;
using System.Drawing; // Required for Font, Point, Size, etc.
using System.Windows.Forms;

namespace conversionTest // Use the namespace of your main project
{
    /// <summary>
    /// A dedicated form to display help information using a RichTextBox.
    /// </summary>
    public partial class HelpForm : Form
    {
        // Controls (typically added via the designer, but defined here for clarity)
        private RichTextBox rtbHelpContent;
        private Button btnClose;
        private readonly string _rtfContent; // Store the RTF content
        private readonly bool _isDarkMode; // Store the theme state

        /// <summary>
        /// Initializes a new instance of the HelpForm class.
        /// </summary>
        /// <param name="title">The title for the help window.</param>
        /// <param name="rtfContent">The help content formatted as RTF.</param>
        public HelpForm(string title, string rtfContent, bool isDarkMode)
        {
            InitializeComponent(); // Standard method to initialize controls from designer

            this.Text = title; // Set the window title
            _rtfContent = rtfContent; // Store the RTF content passed from Form1
            _isDarkMode = isDarkMode; // Store the theme flag

            // --- Configure Form Properties ---
            this.StartPosition = FormStartPosition.CenterParent; // Open centered over the parent (Form1)
            this.Size = new Size(650, 500); // Set a reasonable default size
            this.MinimumSize = new Size(450, 350); // Set a minimum size
            this.FormBorderStyle = FormBorderStyle.Sizable; // Allow resizing
            this.ShowIcon = false; // Optional: Hide icon from title bar
            this.ShowInTaskbar = false; // Optional: Don't show as separate taskbar item when modal
            // AutoScaleMode is set in InitializeComponent
        }

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// (This would normally be in HelpForm.Designer.cs)
        /// </summary>
        private void InitializeComponent()
        {
            // Required for designer support base class if overriding Dispose
            this.components = new System.ComponentModel.Container();
            this.rtbHelpContent = new System.Windows.Forms.RichTextBox();
            this.btnClose = new System.Windows.Forms.Button();
            this.SuspendLayout();
            //
            // rtbHelpContent
            //
            this.rtbHelpContent.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.rtbHelpContent.BackColor = System.Drawing.SystemColors.Window;
            this.rtbHelpContent.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.rtbHelpContent.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0))); // Set default font
            this.rtbHelpContent.Location = new System.Drawing.Point(12, 12);
            this.rtbHelpContent.Name = "rtbHelpContent";
            this.rtbHelpContent.ReadOnly = true; // Make it read-only
            this.rtbHelpContent.ScrollBars = System.Windows.Forms.RichTextBoxScrollBars.Vertical; // Ensure vertical scrollbar
            this.rtbHelpContent.Size = new System.Drawing.Size(610, 400); // Adjust size based on new form size
            this.rtbHelpContent.TabIndex = 0;
            this.rtbHelpContent.Text = ""; // Initial text
            this.rtbHelpContent.DetectUrls = true; // Automatically detect and format URLs
            this.rtbHelpContent.LinkClicked += new System.Windows.Forms.LinkClickedEventHandler(this.RtbHelpContent_LinkClicked); // Add event handler
            //
            // btnClose
            //
            this.btnClose.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnClose.DialogResult = System.Windows.Forms.DialogResult.OK; // Closes form when clicked if shown modally
            this.btnClose.Location = new System.Drawing.Point(547, 426); // Adjust position
            this.btnClose.Name = "btnClose";
            this.btnClose.Size = new System.Drawing.Size(75, 23);
            this.btnClose.TabIndex = 1;
            this.btnClose.Text = "Close";
            this.btnClose.UseVisualStyleBackColor = true;
            this.btnClose.Click += new System.EventHandler(this.BtnClose_Click); // Add event handler
            //
            // HelpForm
            //
            this.AcceptButton = this.btnClose; // Allow Enter key to close the form
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font; // Set scaling mode based on Font
            this.ClientSize = new System.Drawing.Size(634, 461); // Adjust initial client size
            this.Controls.Add(this.btnClose);
            this.Controls.Add(this.rtbHelpContent);
            this.Name = "HelpForm";
            this.Text = "Help"; // Default title
            this.Load += new System.EventHandler(this.HelpForm_Load); // Add Load event handler
            this.ResumeLayout(false);

        }

        /// <summary>
        /// Handles the Load event of the form. Sets the RichTextBox content.
        /// </summary>
        private void HelpForm_Load(object sender, EventArgs e)
        {
            Logger.LogTrace("HelpForm loading.");
            ApplyTheme(_isDarkMode); // Apply theme based on passed flag

            // Load the RTF content
            try
            {
                // Use Rtf property to load RTF formatted string
                this.rtbHelpContent.Rtf = _rtfContent;
            }
            catch (ArgumentException ex)
            {
                // Fallback to plain text if RTF is invalid
                Logger.LogError($"Invalid RTF content provided to HelpForm: {ex.Message}");
                this.rtbHelpContent.Text = "Error loading help content. Invalid RTF format.";
            }
            catch (Exception ex)
            {
                Logger.LogError($"Unexpected error loading RTF content in HelpForm: {ex.Message}");
                this.rtbHelpContent.Text = "An unexpected error occurred loading help content.";
            }
        }

        /// <summary>
        /// Handles clicking the Close button.
        /// </summary>
        private void BtnClose_Click(object sender, EventArgs e)
        {
            Logger.LogTrace("HelpForm close button clicked.");
            this.Close(); // Close the form
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
                    // Use Process.Start with UseShellExecute = true for safety
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

        /// <summary>
        /// Applies basic dark/light theme colors to the form controls.
        /// </summary>
        /// <param name="isDarkMode">True to apply dark mode, false for light mode.</param>
        private void ApplyTheme(bool isDarkMode)
        {
            Logger.LogDebug($"Applying theme to HelpForm. DarkMode: {isDarkMode}");
            // Define colors
            Color backColor = isDarkMode ? Color.FromArgb(45, 45, 48) : SystemColors.Control;
            Color foreColor = isDarkMode ? Color.White : SystemColors.ControlText;
            Color rtbBackColor = isDarkMode ? Color.FromArgb(60, 60, 63) : SystemColors.Window;       
            // RTF content's own formatting might override ForeColor.
            Color btnBackColor = isDarkMode ? Color.FromArgb(63, 63, 70) : SystemColors.Control;
            Color btnForeColor = foreColor;

            // Apply colors
            this.BackColor = backColor;
            this.ForeColor = foreColor;
            this.btnClose.BackColor = btnBackColor;
            this.btnClose.ForeColor = btnForeColor;
            this.btnClose.FlatStyle = FlatStyle.Flat;
            this.btnClose.FlatAppearance.BorderColor = isDarkMode ? Color.Gray : Color.DarkGray;
        }
    }
    // Add this partial class definition if it doesn't exist,
    // otherwise ensure your existing partial class is correct.
    // This is usually in HelpForm.Designer.cs
    partial class HelpForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
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