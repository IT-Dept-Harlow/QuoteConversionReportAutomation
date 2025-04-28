using System;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;

namespace JR.Utils.GUI.Forms
{
    /* FlexibleMessageBox – A flexible replacement for the .NET MessageBox
     *
     * Author:         Jörg Reichert (public@jreichert.de)
     * Contributors:   Thanks to: David Hall, Roink
     * RTF Mod Author: Chris Pringle
     * Version:        1.5
     * Published at:   http://www.codeproject.com/Articles/601900/FlexibleMessageBox
     *
     ************************************************************************************************************
     * Features:
     * - It can be simply used instead of MessageBox since all important static "Show"-Functions are supported
     * - NEW: Supports displaying Rich Text Format (RTF) content via ShowRtf methods.
     * - It is small, only one source file, which could be added easily to each solution
     * - It can be resized and the content is correctly word-wrapped
     * - NEW: Improved auto-sizing logic to better fit content (including RTF).
     * - It never exceeds the current desktop working area (respecting Max Factors)
     * - It displays a vertical scrollbar when needed
     * - It does support hyperlinks in text
     * - NEW: Modernized codebase using newer C# features.
     *
     * Because the interface is identical to MessageBox, you can add this single source file to your project
     * and use the FlexibleMessageBox almost everywhere you use a standard MessageBox.
     * The goal was NOT to produce as many features as possible but to provide a simple replacement to fit my
     * own needs. Feel free to add additional features on your own, but please left my credits in this class.
     *
     ************************************************************************************************************
     * Usage examples:
     *
     * FlexibleMessageBox.Show("Just a text");
     *
     * FlexibleMessageBox.Show("A text",
     * "A caption");
     *
     * FlexibleMessageBox.Show("Some text with a link: www.google.com",
     * "Some caption",
     * MessageBoxButtons.AbortRetryIgnore,
     * MessageBoxIcon.Information,
     * MessageBoxDefaultButton.Button2);
     *
     * // RTF Example:
     * string rtf = @"{\rtf1\ansi{\fonttbl\f0 Arial;}\f0\pard This is \b bold\b0 , \i italic\i0 , and {\cf2 red text}.\par New line.}";
     * FlexibleMessageBox.ShowRtf(rtf, "RTF Example", MessageBoxButtons.OK, MessageBoxIcon.Information);
     *
     * var dialogResult = FlexibleMessageBox.Show("Do you know the answer to life the universe and everything?",
     * "One short question",
     * MessageBoxButtons.YesNo);
     *
     ************************************************************************************************************
     * THE SOFTWARE IS PROVIDED BY THE AUTHOR "AS IS", WITHOUT WARRANTY
     * OF ANY KIND, EXPRESS OR IMPLIED. IN NO EVENT SHALL THE AUTHOR BE
     * LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY ARISING FROM,
     * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OF THIS
     * SOFTWARE.
     *
     ************************************************************************************************************
     * History:
     * Version 1.5 - 28.April 2025 (Chris)
     * - Added ShowRtf methods for displaying Rich Text Format content.
     * - Modernized code using C# features like var, switch expressions, etc.
     * - Significantly improved auto-sizing logic using RichTextBox.GetPreferredSize for better accuracy with word wrap and RTF.
     * - Refactored internal form initialization and layout logic.
     * - Added more comments.
     *
     * Version 1.3 - 19.December 2014
     * - Added refactoring function GetButtonText()
     * - Used CurrentUICulture instead of InstalledUICulture
     * - Added more button localizations. Supported languages are now: ENGLISH, GERMAN, SPANISH, ITALIAN
     * - Added standard MessageBox handling for "copy to clipboard" with <Ctrl> + <C> and <Ctrl> + <Insert>
     * - Tab handling is now corrected (only tabbing over the visible buttons)
     * - Added standard MessageBox handling for ALT-Keyboard shortcuts
     * - SetDialogSizes: Refactored completely: Corrected sizing and added caption driven sizing (Note: Sizing logic reworked again in 1.5)
     *
     * Version 1.2 - 10.August 2013
     * - Do not ShowInTaskbar anymore (original MessageBox is also hidden in taskbar)
     * - Added handling for Escape-Button
     * - Adapted top right close button (red X) to behave like MessageBox (but hidden instead of deactivated)
     *
     * Version 1.1 - 14.June 2013
     * - Some Refactoring
     * - Added internal form class
     * - Added missing code comments, etc.
     *
     * Version 1.0 - 15.April 2013
     * - Initial Version
    */
    public static class FlexibleMessageBox
    {
        #region Public statics

        /// <summary>
        /// Defines the maximum width for all FlexibleMessageBox instances in percent of the working area.
        /// Allowed values are 0.2 - 1.0. Default is 0.7 (70%).
        /// </summary>
        public static double MAX_WIDTH_FACTOR { get; set; } = 0.7;

        /// <summary>
        /// Defines the maximum height for all FlexibleMessageBox instances in percent of the working area.
        /// Allowed values are 0.2 - 1.0. Default is 0.9 (90%).
        /// </summary>
        public static double MAX_HEIGHT_FACTOR { get; set; } = 0.9;

        /// <summary>
        /// Defines the font for all FlexibleMessageBox instances. Default is SystemFonts.MessageBoxFont.
        /// </summary>
        public static Font FONT { get; set; } = SystemFonts.MessageBoxFont ?? SystemFonts.DefaultFont; // Fallback to DefaultFont if MessageBoxFont is null

        #endregion

        #region Public Show methods (Plain Text)

        /// <summary>Displays a message box with specified text.</summary>
        public static DialogResult Show(string text) =>
            FlexibleMessageBoxForm.Show(null, text, string.Empty, MessageBoxButtons.OK, MessageBoxIcon.None, MessageBoxDefaultButton.Button1, false);

        /// <summary>Displays a message box in front of the specified object and with the specified text.</summary>
        public static DialogResult Show(IWin32Window owner, string text) =>
            FlexibleMessageBoxForm.Show(owner, text, string.Empty, MessageBoxButtons.OK, MessageBoxIcon.None, MessageBoxDefaultButton.Button1, false);

        /// <summary>Displays a message box with specified text and caption.</summary>
        public static DialogResult Show(string text, string caption) =>
            FlexibleMessageBoxForm.Show(null, text, caption, MessageBoxButtons.OK, MessageBoxIcon.None, MessageBoxDefaultButton.Button1, false);

        /// <summary>Displays a message box in front of the specified object and with the specified text and caption.</summary>
        public static DialogResult Show(IWin32Window owner, string text, string caption) =>
            FlexibleMessageBoxForm.Show(owner, text, caption, MessageBoxButtons.OK, MessageBoxIcon.None, MessageBoxDefaultButton.Button1, false);

        /// <summary>Displays a message box with specified text, caption, and buttons.</summary>
        public static DialogResult Show(string text, string caption, MessageBoxButtons buttons) =>
            FlexibleMessageBoxForm.Show(null, text, caption, buttons, MessageBoxIcon.None, MessageBoxDefaultButton.Button1, false);

        /// <summary>Displays a message box in front of the specified object and with the specified text, caption, and buttons.</summary>
        public static DialogResult Show(IWin32Window owner, string text, string caption, MessageBoxButtons buttons) =>
            FlexibleMessageBoxForm.Show(owner, text, caption, buttons, MessageBoxIcon.None, MessageBoxDefaultButton.Button1, false);

        /// <summary>Displays a message box with specified text, caption, buttons, and icon.</summary>
        public static DialogResult Show(string text, string caption, MessageBoxButtons buttons, MessageBoxIcon icon) =>
            FlexibleMessageBoxForm.Show(null, text, caption, buttons, icon, MessageBoxDefaultButton.Button1, false);

        /// <summary>Displays a message box in front of the specified object and with the specified text, caption, buttons, and icon.</summary>
        public static DialogResult Show(IWin32Window owner, string text, string caption, MessageBoxButtons buttons, MessageBoxIcon icon) =>
            FlexibleMessageBoxForm.Show(owner, text, caption, buttons, icon, MessageBoxDefaultButton.Button1, false);

        /// <summary>Displays a message box with specified text, caption, buttons, icon, and default button.</summary>
        public static DialogResult Show(string text, string caption, MessageBoxButtons buttons, MessageBoxIcon icon, MessageBoxDefaultButton defaultButton) =>
            FlexibleMessageBoxForm.Show(null, text, caption, buttons, icon, defaultButton, false);

        /// <summary>Displays a message box in front of the specified object and with the specified text, caption, buttons, icon, and default button.</summary>
        public static DialogResult Show(IWin32Window owner, string text, string caption, MessageBoxButtons buttons, MessageBoxIcon icon, MessageBoxDefaultButton defaultButton) =>
            FlexibleMessageBoxForm.Show(owner, text, caption, buttons, icon, defaultButton, false);

        #endregion

        #region Public ShowRtf methods (RTF Text)

        /// <summary>Displays a message box with specified RTF text.</summary>
        public static DialogResult ShowRtf(string rtfText) =>
            FlexibleMessageBoxForm.Show(null, rtfText, string.Empty, MessageBoxButtons.OK, MessageBoxIcon.None, MessageBoxDefaultButton.Button1, true);

        /// <summary>Displays a message box in front of the specified object and with the specified RTF text.</summary>
        public static DialogResult ShowRtf(IWin32Window owner, string rtfText) =>
            FlexibleMessageBoxForm.Show(owner, rtfText, string.Empty, MessageBoxButtons.OK, MessageBoxIcon.None, MessageBoxDefaultButton.Button1, true);

        /// <summary>Displays a message box with specified RTF text and caption.</summary>
        public static DialogResult ShowRtf(string rtfText, string caption) =>
            FlexibleMessageBoxForm.Show(null, rtfText, caption, MessageBoxButtons.OK, MessageBoxIcon.None, MessageBoxDefaultButton.Button1, true);

        /// <summary>Displays a message box in front of the specified object and with the specified RTF text and caption.</summary>
        public static DialogResult ShowRtf(IWin32Window owner, string rtfText, string caption) =>
            FlexibleMessageBoxForm.Show(owner, rtfText, caption, MessageBoxButtons.OK, MessageBoxIcon.None, MessageBoxDefaultButton.Button1, true);

        /// <summary>Displays a message box with specified RTF text, caption, and buttons.</summary>
        public static DialogResult ShowRtf(string rtfText, string caption, MessageBoxButtons buttons) =>
            FlexibleMessageBoxForm.Show(null, rtfText, caption, buttons, MessageBoxIcon.None, MessageBoxDefaultButton.Button1, true);

        /// <summary>Displays a message box in front of the specified object and with the specified RTF text, caption, and buttons.</summary>
        public static DialogResult ShowRtf(IWin32Window owner, string rtfText, string caption, MessageBoxButtons buttons) =>
            FlexibleMessageBoxForm.Show(owner, rtfText, caption, buttons, MessageBoxIcon.None, MessageBoxDefaultButton.Button1, true);

        /// <summary>Displays a message box with specified RTF text, caption, buttons, and icon.</summary>
        public static DialogResult ShowRtf(string rtfText, string caption, MessageBoxButtons buttons, MessageBoxIcon icon) =>
            FlexibleMessageBoxForm.Show(null, rtfText, caption, buttons, icon, MessageBoxDefaultButton.Button1, true);

        /// <summary>Displays a message box in front of the specified object and with the specified RTF text, caption, buttons, and icon.</summary>
        public static DialogResult ShowRtf(IWin32Window owner, string rtfText, string caption, MessageBoxButtons buttons, MessageBoxIcon icon) =>
            FlexibleMessageBoxForm.Show(owner, rtfText, caption, buttons, icon, MessageBoxDefaultButton.Button1, true);

        /// <summary>Displays a message box with specified RTF text, caption, buttons, icon, and default button.</summary>
        public static DialogResult ShowRtf(string rtfText, string caption, MessageBoxButtons buttons, MessageBoxIcon icon, MessageBoxDefaultButton defaultButton) =>
            FlexibleMessageBoxForm.Show(null, rtfText, caption, buttons, icon, defaultButton, true);

        /// <summary>Displays a message box in front of the specified object and with the specified RTF text, caption, buttons, icon, and default button.</summary>
        public static DialogResult ShowRtf(IWin32Window owner, string rtfText, string caption, MessageBoxButtons buttons, MessageBoxIcon icon, MessageBoxDefaultButton defaultButton) =>
            FlexibleMessageBoxForm.Show(owner, rtfText, caption, buttons, icon, defaultButton, true);

        #endregion

        #region Internal form class

        /// <summary>
        /// The internal form to show the customized message box.
        /// </summary>
        internal class FlexibleMessageBoxForm : Form
        {
            #region Form-Designer generated code

            // IMPORTANT: This code is generated by the WinForms designer.
            // Do not modify it manually, as changes might be overwritten.
            // Use the designer view in Visual Studio to modify the form's layout.

            private readonly System.ComponentModel.IContainer? components = null;
            private System.Windows.Forms.Button button1;
            private System.Windows.Forms.RichTextBox richTextBoxMessage;
            private System.Windows.Forms.Panel panelButtons; // Renamed for clarity
            private System.Windows.Forms.PictureBox pictureBoxIcon; // Renamed for clarity
            private System.Windows.Forms.Button button2;
            private System.Windows.Forms.Button button3;
            private System.Windows.Forms.Panel panelTop; // Added for icon and text

            protected override void Dispose(bool disposing)
            {
                if (disposing && (components != null))
                {
                    components.Dispose();
                }
                base.Dispose(disposing);
            }

            private void InitializeComponent()
            {
                this.panelButtons = new System.Windows.Forms.Panel();
                this.button1 = new System.Windows.Forms.Button();
                this.button2 = new System.Windows.Forms.Button();
                this.button3 = new System.Windows.Forms.Button();
                this.panelTop = new System.Windows.Forms.Panel();
                this.pictureBoxIcon = new System.Windows.Forms.PictureBox();
                this.richTextBoxMessage = new System.Windows.Forms.RichTextBox();
                this.panelButtons.SuspendLayout();
                this.panelTop.SuspendLayout();
                ((System.ComponentModel.ISupportInitialize)(this.pictureBoxIcon)).BeginInit();
                this.SuspendLayout();
                //
                // panelButtons
                //
                this.panelButtons.BackColor = System.Drawing.SystemColors.Control;
                this.panelButtons.Controls.Add(this.button1);
                this.panelButtons.Controls.Add(this.button2);
                this.panelButtons.Controls.Add(this.button3);
                this.panelButtons.Dock = System.Windows.Forms.DockStyle.Bottom;
                this.panelButtons.Location = new System.Drawing.Point(0, 76); // Adjusted initial position
                this.panelButtons.Name = "panelButtons";
                this.panelButtons.Padding = new System.Windows.Forms.Padding(10, 10, 10, 10); // Add padding for buttons
                this.panelButtons.Size = new System.Drawing.Size(284, 46); // Adjusted initial size
                this.panelButtons.TabIndex = 1;
                //
                // button1
                //
                this.button1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
                this.button1.AutoSize = true;
                this.button1.DialogResult = System.Windows.Forms.DialogResult.OK; // Default, will be overridden
                this.button1.Location = new System.Drawing.Point(37, 10); // Adjusted position
                this.button1.Margin = new System.Windows.Forms.Padding(3, 3, 3, 3);
                this.button1.MinimumSize = new System.Drawing.Size(75, 26); // Increased min height
                this.button1.Name = "button1";
                this.button1.Size = new System.Drawing.Size(75, 26);
                this.button1.TabIndex = 2;
                this.button1.Text = "B1"; // Placeholder
                this.button1.UseVisualStyleBackColor = true;
                this.button1.Visible = false;
                //
                // button2
                //
                this.button2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
                this.button2.AutoSize = true;
                this.button2.DialogResult = System.Windows.Forms.DialogResult.OK; // Default, will be overridden
                this.button2.Location = new System.Drawing.Point(118, 10); // Adjusted position
                this.button2.Margin = new System.Windows.Forms.Padding(3, 3, 3, 3);
                this.button2.MinimumSize = new System.Drawing.Size(75, 26); // Increased min height
                this.button2.Name = "button2";
                this.button2.Size = new System.Drawing.Size(75, 26);
                this.button2.TabIndex = 3;
                this.button2.Text = "B2"; // Placeholder
                this.button2.UseVisualStyleBackColor = true;
                this.button2.Visible = false;
                //
                // button3
                //
                this.button3.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
                this.button3.AutoSize = true;
                this.button3.DialogResult = System.Windows.Forms.DialogResult.OK; // Default, will be overridden
                this.button3.Location = new System.Drawing.Point(199, 10); // Adjusted position
                this.button3.Margin = new System.Windows.Forms.Padding(3, 3, 3, 3);
                this.button3.MinimumSize = new System.Drawing.Size(75, 26); // Increased min height
                this.button3.Name = "button3";
                this.button3.Size = new System.Drawing.Size(75, 26);
                this.button3.TabIndex = 0; // Changed TabIndex for flow
                this.button3.Text = "B3"; // Placeholder
                this.button3.UseVisualStyleBackColor = true;
                this.button3.Visible = false;
                //
                // panelTop
                //
                this.panelTop.BackColor = System.Drawing.Color.White;
                this.panelTop.Controls.Add(this.pictureBoxIcon);
                this.panelTop.Controls.Add(this.richTextBoxMessage);
                this.panelTop.Dock = System.Windows.Forms.DockStyle.Fill;
                this.panelTop.Location = new System.Drawing.Point(0, 0);
                this.panelTop.Name = "panelTop";
                this.panelTop.Size = new System.Drawing.Size(284, 76); // Adjusted initial size
                this.panelTop.TabIndex = 0; // Changed TabIndex
                //
                // pictureBoxIcon
                //
                this.pictureBoxIcon.BackColor = System.Drawing.Color.Transparent;
                this.pictureBoxIcon.Location = new System.Drawing.Point(15, 15); // Adjusted position
                this.pictureBoxIcon.Margin = new System.Windows.Forms.Padding(3, 3, 3, 3);
                this.pictureBoxIcon.Name = "pictureBoxIcon";
                this.pictureBoxIcon.Size = new System.Drawing.Size(32, 32);
                this.pictureBoxIcon.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage; // Ensure icon fits
                this.pictureBoxIcon.TabIndex = 8;
                this.pictureBoxIcon.TabStop = false;
                this.pictureBoxIcon.Visible = false; // Initially hidden
                //
                // richTextBoxMessage
                //
                this.richTextBoxMessage.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
                this.richTextBoxMessage.BackColor = System.Drawing.Color.White;
                this.richTextBoxMessage.BorderStyle = System.Windows.Forms.BorderStyle.None;
                this.richTextBoxMessage.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0))); // Default, will be overridden
                this.richTextBoxMessage.Location = new System.Drawing.Point(53, 15); // Adjusted initial position
                this.richTextBoxMessage.Margin = new System.Windows.Forms.Padding(0);
                this.richTextBoxMessage.Name = "richTextBoxMessage";
                this.richTextBoxMessage.ReadOnly = true;
                this.richTextBoxMessage.ScrollBars = System.Windows.Forms.RichTextBoxScrollBars.Vertical;
                this.richTextBoxMessage.Size = new System.Drawing.Size(219, 46); // Adjusted initial size
                this.richTextBoxMessage.TabIndex = 0;
                this.richTextBoxMessage.TabStop = false;
                this.richTextBoxMessage.Text = "<Message>"; // Placeholder
                this.richTextBoxMessage.LinkClicked += new System.Windows.Forms.LinkClickedEventHandler(this.richTextBoxMessage_LinkClicked);
                //
                // FlexibleMessageBoxForm
                //
                this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
                this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
                this.ClientSize = new System.Drawing.Size(284, 122); // Adjusted initial size
                this.Controls.Add(this.panelTop);
                this.Controls.Add(this.panelButtons);
                this.MaximizeBox = false;
                this.MinimizeBox = false;
                this.MinimumSize = new System.Drawing.Size(200, 150); // Adjusted minimum size
                this.Name = "FlexibleMessageBoxForm";
                this.ShowIcon = false;
                this.ShowInTaskbar = false;
                this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Show;
                this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent; // Default, may be overridden
                this.Text = "<Caption>"; // Placeholder
                this.Shown += new System.EventHandler(this.FlexibleMessageBoxForm_Shown);
                this.panelButtons.ResumeLayout(false);
                this.panelButtons.PerformLayout();
                this.panelTop.ResumeLayout(false);
                ((System.ComponentModel.ISupportInitialize)(this.pictureBoxIcon)).EndInit();
                this.ResumeLayout(false);

            }

            #endregion

            #region Private constants and enums

            private const string STANDARD_MESSAGEBOX_SEPARATOR_LINES = "---------------------------\n";
            private const string STANDARD_MESSAGEBOX_SEPARATOR_SPACES = "   ";

            private enum ButtonID { OK = 0, CANCEL, YES, NO, ABORT, RETRY, IGNORE }

            // Using ISO language codes for clarity
            private enum LanguageID { en, de, es, it }

            // Store button texts in a dictionary for easier lookup
            private static readonly System.Collections.Generic.Dictionary<LanguageID, string[]> ButtonTexts = new()
            {
                { LanguageID.en, new[] { "OK", "Cancel", "&Yes", "&No", "&Abort", "&Retry", "&Ignore" } }, // Fallback
                { LanguageID.de, new[] { "OK", "Abbrechen", "&Ja", "&Nein", "&Abbrechen", "&Wiederholen", "&Ignorieren" } },
                { LanguageID.es, new[] { "Aceptar", "Cancelar", "&Sí", "&No", "&Abortar", "&Reintentar", "&Ignorar" } },
                { LanguageID.it, new[] { "OK", "Annulla", "&Sì", "&No", "&Interrompi", "&Riprova", "&Ignora" } }
            };

            // Default icon size - used for layout calculations
            private const int DEFAULT_ICON_SIZE = 32;
            // Margins and padding - adjust as needed for visual spacing
            private const int MARGIN_X = 15;
            private const int MARGIN_Y = 15;
            private const int PADDING_X = 10; // Padding inside button panel
            private const int PADDING_Y = 10; // Padding inside button panel
            private const int ICON_TEXT_SPACING = 10; // Space between icon and text

            #endregion

            #region Private members

            private MessageBoxDefaultButton _defaultButton;
            private int _visibleButtonsCount;
            private bool _isRtf; // Flag to indicate if the message is RTF
            private readonly LanguageID _languageID = LanguageID.en; // Default to English

            #endregion

            #region Private constructor

            private FlexibleMessageBoxForm()
            {
                InitializeComponent();

                // Determine UI language
                var cultureName = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
                if (Enum.TryParse<LanguageID>(cultureName, out var langId) && ButtonTexts.ContainsKey(langId))
                {
                    _languageID = langId;
                }
                // English (en) is the default fallback

                KeyPreview = true;
                KeyUp += FlexibleMessageBoxForm_KeyUp;
            }

            #endregion

            #region Private helper functions

            /// <summary>
            /// Gets the button text for the determined UI language.
            /// </summary>
            private string GetButtonText(ButtonID buttonID)
            {
                int index = (int)buttonID;
                if (ButtonTexts.TryGetValue(_languageID, out var texts) && index >= 0 && index < texts.Length)
                {
                    return texts[index];
                }
                // Fallback to English if language or index is invalid
                return ButtonTexts[LanguageID.en][index];
            }

            /// <summary>
            /// Ensures the working area factor is within the valid range [0.2, 1.0].
            /// </summary>
            private static double GetCorrectedWorkingAreaFactor(double factor) => Math.Max(0.2, Math.Min(1.0, factor));

            /// <summary>
            /// Sets the dialog's start position. Centers on the owner or the current screen.
            /// </summary>
            private static void SetDialogStartPosition(FlexibleMessageBoxForm form, IWin32Window? owner)
            {
                form.StartPosition = FormStartPosition.Manual;
                Screen screen;

                if (owner != null)
                {
                    // Center on the owner form
                    var ownerForm = owner as Form ?? Control.FromHandle(owner.Handle)?.FindForm();
                    if (ownerForm != null)
                    {
                        screen = Screen.FromControl(ownerForm);
                        form.Left = ownerForm.Left + (ownerForm.Width - form.Width) / 2;
                        form.Top = ownerForm.Top + (ownerForm.Height - form.Height) / 2;
                    }
                    else
                    {
                        // Fallback if owner is not a Form (e.g., a control)
                        screen = Screen.FromPoint(Cursor.Position);
                        form.Left = screen.WorkingArea.Left + (screen.WorkingArea.Width - form.Width) / 2;
                        form.Top = screen.WorkingArea.Top + (screen.WorkingArea.Height - form.Height) / 2;
                    }
                }
                else
                {
                    // Center on the current screen containing the cursor
                    screen = Screen.FromPoint(Cursor.Position);
                    form.Left = screen.WorkingArea.Left + (screen.WorkingArea.Width - form.Width) / 2;
                    form.Top = screen.WorkingArea.Top + (screen.WorkingArea.Height - form.Height) / 2;
                }

                // Ensure the form is within the screen bounds
                form.Left = Math.Max(screen.WorkingArea.Left, Math.Min(form.Left, screen.WorkingArea.Right - form.Width));
                form.Top = Math.Max(screen.WorkingArea.Top, Math.Min(form.Top, screen.WorkingArea.Bottom - form.Height));
            }

            /// <summary>
            /// Calculates and sets the dialog's size based on content, icon, and buttons.
            /// Uses GetPreferredSize for accurate measurement after content is set.
            /// </summary>
            private void SetDialogSizes(MessageBoxIcon icon)
            {
                // Calculate maximum allowed size based on screen and factors
                var screen = Screen.FromPoint(Cursor.Position); // Or Screen.FromControl(this) if preferred
                var maxFactorWidth = GetCorrectedWorkingAreaFactor(MAX_WIDTH_FACTOR);
                var maxFactorHeight = GetCorrectedWorkingAreaFactor(MAX_HEIGHT_FACTOR);
                int maxFormWidth = (int)(screen.WorkingArea.Width * maxFactorWidth);
                int maxFormHeight = (int)(screen.WorkingArea.Height * maxFactorHeight);

                // Determine icon width (0 if no icon)
                int iconWidth = (icon == MessageBoxIcon.None) ? 0 : DEFAULT_ICON_SIZE + ICON_TEXT_SPACING;

                // Calculate available width for the RichTextBox
                int richTextBoxMaxWidth = maxFormWidth - MARGIN_X - iconWidth - MARGIN_X - SystemInformation.VerticalScrollBarWidth; // Account for margins and potential scrollbar

                // Get the preferred size of the RichTextBox based on its content (Text or Rtf)
                // This accurately accounts for word wrapping
                Size preferredRichTextSize = richTextBoxMessage.GetPreferredSize(new Size(richTextBoxMaxWidth, 0)); // Height = 0 means calculate based on width

                // Calculate required width for the text area
                int requiredTextWidth = preferredRichTextSize.Width + iconWidth;

                // Calculate required width for buttons
                int requiredButtonWidth = 0;
                if (button1.Visible) requiredButtonWidth += button1.Width;
                if (button2.Visible) requiredButtonWidth += button2.Width;
                if (button3.Visible) requiredButtonWidth += button3.Width;
                requiredButtonWidth += PADDING_X * 2; // Panel padding
                if (_visibleButtonsCount > 1) requiredButtonWidth += (_visibleButtonsCount - 1) * button1.Margin.Horizontal; // Spacing between buttons

                // Calculate required width for the caption
                int captionWidth = TextRenderer.MeasureText(this.Text, SystemFonts.CaptionFont).Width + SystemInformation.CaptionButtonSize.Width * 2; // Estimate caption width

                // Determine final form width
                int requiredTotalWidth = Math.Max(requiredTextWidth, requiredButtonWidth);
                requiredTotalWidth = Math.Max(requiredTotalWidth, captionWidth); // Ensure caption fits
                int formWidth = requiredTotalWidth + MARGIN_X * 2 + SystemInformation.VerticalScrollBarWidth; // Add margins and scrollbar allowance

                // Calculate required height
                int formHeight = MARGIN_Y + preferredRichTextSize.Height + panelButtons.Height + MARGIN_Y + SystemInformation.CaptionHeight + (SystemInformation.FrameBorderSize.Height * 2);

                // Apply maximum size constraints
                formWidth = Math.Min(formWidth, maxFormWidth);
                formHeight = Math.Min(formHeight, maxFormHeight);

                // Apply minimum size constraints
                formWidth = Math.Max(formWidth, this.MinimumSize.Width);
                formHeight = Math.Max(formHeight, this.MinimumSize.Height);

                // Set the final size
                this.Size = new Size(formWidth, formHeight);

                // Adjust RichTextBox position based on icon visibility
                if (icon != MessageBoxIcon.None)
                {
                    richTextBoxMessage.Left = MARGIN_X + DEFAULT_ICON_SIZE + ICON_TEXT_SPACING;
                    richTextBoxMessage.Width = this.panelTop.ClientSize.Width - richTextBoxMessage.Left - MARGIN_X;
                }
                else
                {
                    richTextBoxMessage.Left = MARGIN_X;
                    richTextBoxMessage.Width = this.panelTop.ClientSize.Width - MARGIN_X - MARGIN_X;
                }
                richTextBoxMessage.Height = this.panelTop.ClientSize.Height - MARGIN_Y - MARGIN_Y;

            }


            /// <summary>
            /// Sets the dialog icon visibility and image.
            /// </summary>
            private void SetDialogIcon(MessageBoxIcon icon)
            {
                // Use a switch expression for conciseness
                pictureBoxIcon.Image = icon switch
                {
                    MessageBoxIcon.Information => SystemIcons.Information.ToBitmap(),
                    MessageBoxIcon.Warning => SystemIcons.Warning.ToBitmap(),
                    MessageBoxIcon.Error => SystemIcons.Error.ToBitmap(),
                    MessageBoxIcon.Question => SystemIcons.Question.ToBitmap(),
                    _ => null // Includes MessageBoxIcon.None
                };

                pictureBoxIcon.Visible = (pictureBoxIcon.Image != null);

                // Adjust layout based on icon visibility (done in SetDialogSizes now)
            }

            /// <summary>
            /// Configures button visibility, text, DialogResult, and sets the CancelButton.
            /// </summary>
            private void SetDialogButtons(MessageBoxButtons buttons)
            {
                // Hide all buttons initially
                button1.Visible = false;
                button2.Visible = false;
                button3.Visible = false;
                this.CancelButton = null; // Reset cancel button

                switch (buttons)
                {
                    case MessageBoxButtons.AbortRetryIgnore:
                        _visibleButtonsCount = 3;
                        SetupButton(button1, ButtonID.ABORT, DialogResult.Abort);
                        SetupButton(button2, ButtonID.RETRY, DialogResult.Retry);
                        SetupButton(button3, ButtonID.IGNORE, DialogResult.Ignore);
                        ControlBox = false; // No close button for AbortRetryIgnore
                        break;

                    case MessageBoxButtons.OKCancel:
                        _visibleButtonsCount = 2;
                        SetupButton(button2, ButtonID.OK, DialogResult.OK);
                        SetupButton(button3, ButtonID.CANCEL, DialogResult.Cancel);
                        this.CancelButton = button3;
                        break;

                    case MessageBoxButtons.RetryCancel:
                        _visibleButtonsCount = 2;
                        SetupButton(button2, ButtonID.RETRY, DialogResult.Retry);
                        SetupButton(button3, ButtonID.CANCEL, DialogResult.Cancel);
                        this.CancelButton = button3;
                        break;

                    case MessageBoxButtons.YesNo:
                        _visibleButtonsCount = 2;
                        SetupButton(button2, ButtonID.YES, DialogResult.Yes);
                        SetupButton(button3, ButtonID.NO, DialogResult.No);
                        ControlBox = false; // No close button for YesNo
                        break;

                    case MessageBoxButtons.YesNoCancel:
                        _visibleButtonsCount = 3;
                        SetupButton(button1, ButtonID.YES, DialogResult.Yes);
                        SetupButton(button2, ButtonID.NO, DialogResult.No);
                        SetupButton(button3, ButtonID.CANCEL, DialogResult.Cancel);
                        this.CancelButton = button3;
                        break;

                    case MessageBoxButtons.OK:
                    default: // Treat unknown combinations as OK
                        _visibleButtonsCount = 1;
                        SetupButton(button3, ButtonID.OK, DialogResult.OK);
                        this.CancelButton = button3; // OK button also acts as Cancel
                        break;
                }

                // Reposition buttons based on visibility - Right align them
                int currentRight = panelButtons.ClientSize.Width - PADDING_X;
                if (button3.Visible)
                {
                    button3.Location = new Point(currentRight - button3.Width, PADDING_Y);
                    currentRight -= (button3.Width + button3.Margin.Horizontal);
                }
                if (button2.Visible)
                {
                    button2.Location = new Point(currentRight - button2.Width, PADDING_Y);
                    currentRight -= (button2.Width + button2.Margin.Horizontal);
                }
                if (button1.Visible)
                {
                    button1.Location = new Point(currentRight - button1.Width, PADDING_Y);
                }
            }

            /// <summary>
            /// Helper to set properties for a button.
            /// </summary>
            private void SetupButton(Button button, ButtonID buttonID, DialogResult dialogResult)
            {
                button.Text = GetButtonText(buttonID);
                button.DialogResult = dialogResult;
                button.Visible = true;
                button.AutoSize = true; // Allow button to resize based on text
            }

            #endregion

            #region Private event handlers

            /// <summary>
            /// Sets focus to the default button when the form is shown.
            /// </summary>
            private void FlexibleMessageBoxForm_Shown(object? sender, EventArgs e)
            {
                // Determine which button should get focus based on _defaultButton and visibility
                Button? buttonToFocus = _defaultButton switch
                {
                    MessageBoxDefaultButton.Button1 when button1.Visible => button1,
                    MessageBoxDefaultButton.Button2 when button2.Visible => button2,
                    MessageBoxDefaultButton.Button3 when button3.Visible => button3,
                    // Fallbacks if the designated default is not visible
                    _ when button3.Visible => button3, // Default to last visible button (often OK or Cancel)
                    _ when button2.Visible => button2,
                    _ when button1.Visible => button1,
                    _ => null
                };

                buttonToFocus?.Focus();
            }

            /// <summary>
            /// Handles link clicks in the RichTextBox to open them in the default browser.
            /// </summary>
            private void richTextBoxMessage_LinkClicked(object? sender, LinkClickedEventArgs e)
            {
                if (e.LinkText != null)
                {
                    try
                    {
                        Cursor.Current = Cursors.WaitCursor;
                        // Use ProcessStartInfo for better control and security
                        var psi = new ProcessStartInfo
                        {
                            FileName = e.LinkText,
                            UseShellExecute = true // Important for opening URLs
                        };
                        Process.Start(psi);
                    }
                    catch (Exception ex)
                    {
                        // Log or handle the exception appropriately
                        // Optionally, show another message box about the error
                        Debug.WriteLine($"Error opening link: {ex.Message}");
                        // Consider showing a simpler error message to the user
                        // MessageBox.Show($"Could not open link:\n{e.LinkText}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    finally
                    {
                        Cursor.Current = Cursors.Default;
                    }
                }
            }

            /// <summary>
            /// Handles Ctrl+C and Ctrl+Insert for copying message content to clipboard.
            /// Handles Escape key to close the dialog if a Cancel button exists.
            /// </summary>
            private void FlexibleMessageBoxForm_KeyUp(object? sender, KeyEventArgs e)
            {
                // Handle standard key strikes for clipboard copy: "Ctrl + C" and "Ctrl + Insert"
                if (e.Control && (e.KeyCode == Keys.C || e.KeyCode == Keys.Insert))
                {
                    // Build the text string to copy, mimicking the standard MessageBox format
                    var buttonsText = string.Join(STANDARD_MESSAGEBOX_SEPARATOR_SPACES,
                        new[] { button1, button2, button3 }
                        .Where(btn => btn.Visible)
                        .Select(btn => btn.Text.Replace("&", ""))); // Remove ampersands

                    var textForClipboard =
                        STANDARD_MESSAGEBOX_SEPARATOR_LINES +
                        this.Text + Environment.NewLine + // Caption
                        STANDARD_MESSAGEBOX_SEPARATOR_LINES +
                        (_isRtf ? richTextBoxMessage.Text : richTextBoxMessage.Rtf) + Environment.NewLine + // Message (use Text for RTF for plain representation)
                        STANDARD_MESSAGEBOX_SEPARATOR_LINES +
                        buttonsText + Environment.NewLine +
                        STANDARD_MESSAGEBOX_SEPARATOR_LINES;

                    try
                    {
                        Clipboard.SetText(textForClipboard);
                    }
                    catch (System.Runtime.InteropServices.ExternalException ex)
                    {
                        Debug.WriteLine($"Clipboard error: {ex.Message}");
                        // Handle clipboard access issues if necessary
                    }
                }
                // Handle Escape key to trigger the Cancel button
                else if (e.KeyCode == Keys.Escape)
                {
                    // Find the button assigned as the Cancel button or the one with DialogResult.Cancel
                    var cancelButton = this.CancelButton as Button ??
                                       new[] { button1, button2, button3 }.FirstOrDefault(b => b.Visible && b.DialogResult == DialogResult.Cancel);

                    if (cancelButton != null)
                    {
                        // Simulate clicking the cancel button
                        this.DialogResult = cancelButton.DialogResult;
                        this.Close();
                    }
                    else if (ControlBox && _visibleButtonsCount <= 1) // If only OK button and ControlBox is visible, Escape closes
                    {
                        this.DialogResult = button3.Visible ? button3.DialogResult : DialogResult.Cancel; // Default to Cancel if OK not visible?
                        this.Close();
                    }
                }
            }

            #endregion

            #region Public static Show function (internal)

            /// <summary>
            /// The internal static Show function that creates and displays the form.
            /// </summary>
            internal static DialogResult Show(IWin32Window? owner, string textOrRtf, string caption, MessageBoxButtons buttons, MessageBoxIcon icon, MessageBoxDefaultButton defaultButton, bool isRtf)
            {
                // Using statement ensures the form is disposed even if exceptions occur
                using var form = new FlexibleMessageBoxForm();
                form._isRtf = isRtf;
                form._defaultButton = defaultButton;

                // Set base properties
                form.Text = caption ?? string.Empty; // Use empty string if caption is null
                form.Font = FlexibleMessageBox.FONT; // Use the static font property

                // Set message content (must be done before sizing)
                form.richTextBoxMessage.Font = form.Font; // Ensure RichTextBox uses the correct font
                if (isRtf)
                {
                    try
                    {
                        form.richTextBoxMessage.Rtf = textOrRtf;
                    }
                    catch (ArgumentException ex)
                    {
                        // Handle invalid RTF - show as plain text instead
                        Debug.WriteLine($"Invalid RTF format: {ex.Message}. Displaying as plain text.");
                        form.richTextBoxMessage.Text = textOrRtf;
                        form._isRtf = false; // Treat as plain text now
                    }
                }
                else
                {
                    form.richTextBoxMessage.Text = textOrRtf;
                }

                // Configure UI elements (order matters for layout calculations)
                form.SetDialogIcon(icon);
                form.SetDialogButtons(buttons); // Sets button visibility, text, and CancelButton

                // Calculate and set size *after* content and buttons are set
                form.SetDialogSizes(icon);

                // Set position *after* size is determined
                SetDialogStartPosition(form, owner);

                // Show the dialog modally
                return form.ShowDialog(owner);
            }
            #endregion

        } // End of internal class FlexibleMessageBoxForm

        #endregion
    }
}
