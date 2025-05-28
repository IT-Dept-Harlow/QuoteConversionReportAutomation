// ManageEmailRecipientsForm.cs
// This form allows users to manage custom email recipient lists for various report scenarios,
// overriding the application's default settings. It supports different configurations for
// automated reports (now category-based), manual reports, and debug mode.
// Utilises C# 10+ features.

#region Using Directives
// System related namespaces
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

// Project specific namespaces
using QuoteConversionReportAutomation.Helpers; // For FlexibleMessageBox
using QuoteConversionReportAutomation.Managers; // For EmailRecipientManager and UIManager
using QuoteConversionReportAutomation.Models;   // For UserEmailSettings
using QuoteConversionReportAutomation.Services.Logging; // For Logger
#endregion

namespace QuoteConversionReportAutomation
{
    /// <summary>
    /// A Windows Form that allows users to view and modify email recipient lists
    /// for different report generation contexts. User-defined settings are saved
    /// and override application defaults. The "Automated Reports" tab now reflects
    /// recipient categories for more flexible configuration.
    /// </summary>
    public partial class ManageEmailRecipientsForm : Form
    {
        #region Fields and Constants

        private readonly EmailRecipientManager _emailRecipientManager; // Service for loading/saving email settings.
        private readonly bool _isDarkMode; // Flag indicating if dark mode is active, passed from parent.

        // --- Theme Colours ---
        // Define colours for dark and light modes to ensure consistent UI theming.
        // Dark Mode Colours
        private static readonly Color DM_ControlBackColor = Color.FromArgb(45, 45, 48);         // Background for input controls.
        private static readonly Color DM_TabPageBackColor = Color.FromArgb(37, 37, 38);         // Background for tab pages.
        private static readonly Color DM_TabControlBackColor = Color.FromArgb(28, 28, 28);      // Background for the tab control itself.
        private static readonly Color DM_ButtonBackColor = Color.FromArgb(60, 60, 63);          // Background for buttons.
        private static readonly Color DM_ControlForeColor = Color.WhiteSmoke;                   // Text colour for controls.
        private static readonly Color DM_LabelForeColor = Color.FromArgb(200, 200, 200);        // Text colour for labels.

        // Light Mode Colours
        private static readonly Color LM_ControlBackColor = SystemColors.Window;                // Standard window background.
        private static readonly Color LM_TabPageBackColor = SystemColors.Control;               // Standard control background.
        private static readonly Color LM_TabControlBackColor = SystemColors.Control;            // Standard control background.
        private static readonly Color LM_ButtonBackColor = SystemColors.ControlLight;           // Standard light button background.
        private static readonly Color LM_ControlForeColor = SystemColors.ControlText;           // Standard control text colour.

        // --- UI Control Fields for Manual Custom Report ---
        // These fields hold references to the TextBoxes for "Manual Custom" report recipients.
        // They are initialised in InitializeManualCustomControls.
        private TextBox txtProdManualCustomTo;
        private TextBox txtProdManualCustomCC;

        // --- UI Control Fields for Category-Based Automated Report Overrides ---
        // These fields will hold references to the TextBoxes for the new category-based automated report overrides.
        // They are initialised in InitializeAutomatedReportControls.
        private TextBox txtAutoRunDailyStandardRecipientsTo;
        private TextBox txtAutoRunDailyStandardRecipientsCC;
        private TextBox txtAutoRunDaily5Day1kRecipientsTo;
        private TextBox txtAutoRunDaily5Day1kRecipientsCC;
        private TextBox txtAutoRunWeeklyRecipientsTo;
        private TextBox txtAutoRunWeeklyRecipientsCC;
        // Add fields here for other categories if defined, e.g.:
        // private TextBox txtAutoRunMonthlyMarketingRecipientsTo;
        // private TextBox txtAutoRunMonthlyMarketingRecipientsCC;

        #endregion

        #region Constructor
        /// <summary>
        /// Initialises a new instance of the <see cref="ManageEmailRecipientsForm"/> class.
        /// </summary>
        /// <param name="emailRecipientManager">The manager responsible for email recipient settings logic.</param>
        /// <param name="isDarkMode">A flag indicating whether dark mode should be applied to the form.</param>
        public ManageEmailRecipientsForm(EmailRecipientManager emailRecipientManager, bool isDarkMode)
        {
            _emailRecipientManager = emailRecipientManager ?? throw new ArgumentNullException(nameof(emailRecipientManager));
            _isDarkMode = isDarkMode;

            InitializeComponent(); // Standard WinForms method to initialise components defined in the .Designer.cs file.

            // Initialise UI elements for "Manual Custom" recipients (Phase 1).
            InitializeManualCustomControls();
            // Initialise UI elements for category-based "Automated Report" recipients (Phase 2).
            InitializeAutomatedReportControls();

            // Configure basic form properties.
            this.ShowIcon = false;
            this.StartPosition = FormStartPosition.CenterParent;
            this.Text = "Manage Email Recipients";
        }
        #endregion

        #region Form Load and Theming
        /// <summary>
        /// Handles the Load event of the form. This is called once when the form is first displayed.
        /// It applies the visual theme, loads current settings into the UI controls, and sets up tooltips.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">An <see cref="EventArgs"/> that contains no event data.</param>
        private void ManageEmailRecipientsForm_Load(object sender, EventArgs e)
        {
            Logger.LogInfo($"ManageEmailRecipientsForm loading. Initial DarkMode state: {_isDarkMode}");
            // Apply the theme to the form itself (title bar, main background).
            UIManager.ApplyThemeToExternalForm(this, _isDarkMode);
            // Apply the theme to the tabbed layout and its child controls.
            ApplyThemeToTabbedLayout(_isDarkMode);
            // Load the current email recipient settings into the form's textboxes.
            LoadSettingsToForm();
            // Set up informational tooltips for various controls.
            SetupToolTips();

#if !DEBUG
            // In Release mode, remove the "Debug Recipients" tab page if it exists.
            if (mainTabControl.TabPages.ContainsKey("debugTabPage")) // Check by Name property.
            {
                mainTabControl.TabPages.RemoveByKey("debugTabPage");
                Logger.LogInfo("Release mode: Removed Debug recipients tab page.");
            }
#endif
            Logger.LogInfo("ManageEmailRecipientsForm loaded and themed.");
        }

        /// <summary>
        /// Applies the current theme (dark or light) to the tab control and its contained elements.
        /// This ensures that tab pages and their contents are styled consistently with the rest of the form.
        /// </summary>
        /// <param name="isDarkModeEnabled">True if dark mode is enabled, false otherwise.</param>
        private void ApplyThemeToTabbedLayout(bool isDarkModeEnabled)
        {
            // Set the background colour of the form itself to match the tab control's outer area.
            this.BackColor = isDarkModeEnabled ? DM_TabControlBackColor : LM_TabControlBackColor;
            // Theme the instructional label at the top of the form.
            this.lblInstructions.ForeColor = isDarkModeEnabled ? DM_LabelForeColor : LM_ControlForeColor;
            this.lblInstructions.BackColor = Color.Transparent; // Make label background transparent.

            // Theme the main tab control itself.
            mainTabControl.BackColor = isDarkModeEnabled ? DM_TabControlBackColor : LM_TabControlBackColor;

            // Iterate through each tab page to apply themes.
            foreach (TabPage tabPage in mainTabControl.TabPages)
            {
                tabPage.BackColor = isDarkModeEnabled ? DM_TabPageBackColor : LM_TabPageBackColor;
                // The ForeColor of the TabPage affects the tab header text.
                tabPage.ForeColor = isDarkModeEnabled ? DM_ControlForeColor : LM_ControlForeColor;

                // Apply theme recursively to controls within each tab page.
                // This typically targets a primary layout panel (e.g., TableLayoutPanel) within the tab.
                foreach (Control childControl in tabPage.Controls)
                {
                    ApplyThemeToControlsRecursive(childControl, isDarkModeEnabled);
                }
            }

            // Theme the FlowLayoutPanel containing the Save, Restore, Close buttons.
            buttonsFlowLayoutPanel.BackColor = this.BackColor; // Match form background.
            ApplyThemeToControlsRecursive(buttonsFlowLayoutPanel, isDarkModeEnabled);
        }

        /// <summary>
        /// Recursively applies theme colours to a control and its child controls.
        /// This method handles common control types like Buttons, TextBoxes, and Labels.
        /// </summary>
        /// <param name="parentControl">The parent control to start theming from.</param>
        /// <param name="isDarkModeEnabled">True if dark mode is enabled, false otherwise.</param>
        private void ApplyThemeToControlsRecursive(Control parentControl, bool isDarkModeEnabled)
        {
            // Determine appropriate colours based on the current theme.
            Color controlBackColor = isDarkModeEnabled ? DM_ControlBackColor : LM_ControlBackColor;
            Color buttonBackColor = isDarkModeEnabled ? DM_ButtonBackColor : LM_ButtonBackColor;
            Color controlForeColor = isDarkModeEnabled ? DM_ControlForeColor : LM_ControlForeColor;
            Color labelForeColor = isDarkModeEnabled ? DM_LabelForeColor : LM_ControlForeColor; // Specific colour for labels.

            // Iterate through each control within the parentControl.
            foreach (Control control in parentControl.Controls)
            {
                if (control.IsDisposed) continue; // Skip disposed controls.

                // Apply theme based on control type.
                if (control is Button button)
                {
                    button.BackColor = buttonBackColor;
                    button.ForeColor = controlForeColor;
                    button.FlatStyle = FlatStyle.Flat; // Flat style often looks better with custom colours.
                    button.FlatAppearance.BorderColor = isDarkModeEnabled ? Color.FromArgb(100, 100, 100) : SystemColors.ControlDarkDark;
                    button.FlatAppearance.BorderSize = 1;
                }
                else if (control is TextBox || control is RichTextBox) // Handles both TextBox and RichTextBox.
                {
                    control.BackColor = controlBackColor;
                    control.ForeColor = controlForeColor;
                    if (control is TextBox tb) // Specific border style for TextBox.
                    {
                        // Use FixedSingle in dark mode for better visibility against dark backgrounds.
                        tb.BorderStyle = isDarkModeEnabled ? BorderStyle.FixedSingle : BorderStyle.Fixed3D;
                    }
                }
                else if (control is Label)
                {
                    control.BackColor = Color.Transparent; // Labels should typically be transparent to show parent's colour.
                    control.ForeColor = labelForeColor;    // Use specific label text colour.
                }
                else if (control.HasChildren) // If the control is a container (e.g., Panel, GroupBox, TableLayoutPanel).
                {
                    // For generic containers not explicitly styled (like TabPage, TabControl, which are handled by ApplyThemeToTabbedLayout),
                    // make their background match their immediate parent if that parent is part of the main layout.
                    if (!(control is TableLayoutPanel || control is TabPage || control is TabControl))
                    {
                        control.BackColor = parentControl.BackColor;
                    }
                    // Recursively apply theme to children of this container.
                    ApplyThemeToControlsRecursive(control, isDarkModeEnabled);
                }
            }
        }
        #endregion

        #region UI Initialisation and Data Loading
        /// <summary>
        /// Initialises or finds controls for "Manual Custom" report recipients.
        /// If these controls (`txtProdManualCustomTo`, `txtProdManualCustomCC`) are not found
        /// (e.g., not added via the WinForms designer), this method creates them programmatically
        /// and adds them to a new "Manual Custom" tab page.
        /// </summary>
        private void InitializeManualCustomControls()
        {
            // Attempt to find the TextBoxes by name. This allows them to be added via the designer.
            Control[] foundTo = this.Controls.Find("txtProdManualCustomTo", true); // Search recursively.
            if (foundTo.Length > 0 && foundTo[0] is TextBox textBoxTo) { txtProdManualCustomTo = textBoxTo; }

            Control[] foundCC = this.Controls.Find("txtProdManualCustomCC", true);
            if (foundCC.Length > 0 && foundCC[0] is TextBox textBoxCC) { txtProdManualCustomCC = textBoxCC; }

            // If either TextBox was not found (is still null), proceed to create them programmatically.
            if (txtProdManualCustomTo == null || txtProdManualCustomCC == null)
            {
                Logger.LogDebug("Manual Custom recipient TextBoxes not found by name. Creating programmatically.");
                TabPage manualCustomTabPage;
                TableLayoutPanel tlpManualCustom;

                const string manualCustomTabKey = "manualCustomReportRecipientsTabPage"; // Unique key for the tab.

                // Check if a TabPage for manual custom settings already exists.
                if (mainTabControl.TabPages.ContainsKey(manualCustomTabKey))
                {
                    manualCustomTabPage = mainTabControl.TabPages[manualCustomTabKey];
                    // If the tab exists, try to find its TableLayoutPanel, or create one if missing.
                    tlpManualCustom = manualCustomTabPage.Controls.OfType<TableLayoutPanel>().FirstOrDefault() ??
                                      new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, Padding = new Padding(10) };
                    if (tlpManualCustom.Parent == null) manualCustomTabPage.Controls.Add(tlpManualCustom);
                }
                else // If the TabPage doesn't exist, create it and its TableLayoutPanel.
                {
                    manualCustomTabPage = new TabPage("Manual Custom") { Name = manualCustomTabKey };
                    tlpManualCustom = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, Padding = new Padding(10) };
                    manualCustomTabPage.Controls.Add(tlpManualCustom);
                    mainTabControl.TabPages.Add(manualCustomTabPage); // Add the new tab to the TabControl.
                }

                // Configure the columns and rows of the TableLayoutPanel.
                tlpManualCustom.ColumnStyles.Clear();
                tlpManualCustom.RowStyles.Clear();
                tlpManualCustom.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 200F)); // Fixed width for labels.
                tlpManualCustom.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));  // TextBox takes remaining width.
                tlpManualCustom.RowCount = 3; // Rows for: To Label/TextBox, CC Label/TextBox, Spacer.
                tlpManualCustom.RowStyles.Add(new RowStyle(SizeType.Absolute, 35F));
                tlpManualCustom.RowStyles.Add(new RowStyle(SizeType.Absolute, 35F));
                tlpManualCustom.RowStyles.Add(new RowStyle(SizeType.Percent, 100F)); // Spacer row fills remaining space.

                // Create Labels for the TextBoxes.
                Label lblManualCustomTo = new Label { Text = "Manual Custom Report TO:", Anchor = AnchorStyles.Right | AnchorStyles.Top, AutoSize = true, Margin = new Padding(3, 6, 3, 3) };
                Label lblManualCustomCC = new Label { Text = "Manual Custom Report CC:", Anchor = AnchorStyles.Right | AnchorStyles.Top, AutoSize = true, Margin = new Padding(3, 6, 3, 3) };

                // Create TextBoxes if they weren't found and assigned earlier.
                if (txtProdManualCustomTo == null)
                {
                    txtProdManualCustomTo = new TextBox { Name = "txtProdManualCustomTo", Multiline = false, Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top, Height = 20 };
                    tlpManualCustom.Controls.Add(lblManualCustomTo, 0, 0); // Add label to column 0, row 0.
                    tlpManualCustom.Controls.Add(txtProdManualCustomTo, 1, 0); // Add textbox to column 1, row 0.
                }

                if (txtProdManualCustomCC == null)
                {
                    txtProdManualCustomCC = new TextBox { Name = "txtProdManualCustomCC", Multiline = false, Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top, Height = 20 };
                    tlpManualCustom.Controls.Add(lblManualCustomCC, 0, 1); // Add label to column 0, row 1.
                    tlpManualCustom.Controls.Add(txtProdManualCustomCC, 1, 1); // Add textbox to column 1, row 1.
                }
                Logger.LogInfo("Programmatically added/configured UI elements for Manual Custom recipients.");
            }
            else
            {
                Logger.LogDebug("Manual Custom recipient TextBoxes found by name (likely from designer).");
            }
        }

        /// <summary>
        /// Clears and programmatically (re)creates controls for category-based automated report recipient overrides
        /// on the "Automated Reports" tab. This ensures the UI matches the configurable categories.
        /// </summary>
        private void InitializeAutomatedReportControls()
        {
            Logger.LogDebug("Initialising/Rebuilding controls for Automated Report recipient categories.");
            // Ensure the TableLayoutPanel for automated reports exists.
            if (automatedReportsTableLayoutPanel == null)
            {
                Logger.LogError("automatedReportsTableLayoutPanel is null. Cannot initialise automated report controls. This indicates a problem with the form designer initialisation.");
                // Attempt to create it if absolutely necessary, though this is a fallback for a designer issue.
                automatedReportsTableLayoutPanel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, Padding = new Padding(10), Name = "automatedReportsTableLayoutPanel" };
                if (automatedReportsTabPage != null) automatedReportsTabPage.Controls.Add(automatedReportsTableLayoutPanel);
                else
                {
                    Logger.LogError("automatedReportsTabPage is also null. Cannot add TableLayoutPanel.");
                    return; // Cannot proceed.
                }
            }

            // Clear existing controls and styles from the TableLayoutPanel.
            automatedReportsTableLayoutPanel.Controls.Clear();
            automatedReportsTableLayoutPanel.RowStyles.Clear();
            automatedReportsTableLayoutPanel.ColumnStyles.Clear();
            automatedReportsTableLayoutPanel.RowCount = 0; // Reset row count.

            // Configure columns for the TableLayoutPanel (Label column, TextBox column).
            automatedReportsTableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 230F)); // Wider label column for descriptive text.
            automatedReportsTableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));  // TextBox takes remaining width.

            int currentRow = 0; // Keep track of the current row being added to the TableLayoutPanel.

            // Helper action to add a Label and TextBox pair for a recipient category.
            // This reduces code duplication.
            void AddCategoryControls(string labelText, out TextBox toTextBoxField, out TextBox ccTextBoxField, string categoryKeyBaseName)
            {
                // --- TO Recipients ---
                automatedReportsTableLayoutPanel.RowCount++; // Increment row count for the "To" label and textbox.
                automatedReportsTableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 35F)); // Set fixed height for the row.
                Label lblTo = new Label { Text = $"{labelText} TO:", Name = $"lbl{categoryKeyBaseName}To", Anchor = AnchorStyles.Right | AnchorStyles.Top, AutoSize = true, Margin = new Padding(3, 6, 3, 3) };
                toTextBoxField = new TextBox { Name = $"txt{categoryKeyBaseName}To", Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top, Height = 20 };
                automatedReportsTableLayoutPanel.Controls.Add(lblTo, 0, currentRow);        // Add label to column 0.
                automatedReportsTableLayoutPanel.Controls.Add(toTextBoxField, 1, currentRow); // Add textbox to column 1.
                currentRow++; // Move to the next row index.

                // --- CC Recipients ---
                automatedReportsTableLayoutPanel.RowCount++; // Increment row count for the "CC" label and textbox.
                automatedReportsTableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 35F)); // Set fixed height for the row.
                Label lblCc = new Label { Text = $"{labelText} CC:", Name = $"lbl{categoryKeyBaseName}Cc", Anchor = AnchorStyles.Right | AnchorStyles.Top, AutoSize = true, Margin = new Padding(3, 6, 3, 3) };
                ccTextBoxField = new TextBox { Name = $"txt{categoryKeyBaseName}Cc", Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top, Height = 20 };
                automatedReportsTableLayoutPanel.Controls.Add(lblCc, 0, currentRow);        // Add label to column 0.
                automatedReportsTableLayoutPanel.Controls.Add(ccTextBoxField, 1, currentRow); // Add textbox to column 1.
                currentRow++; // Move to the next row index.
            }

            // Add controls for each defined recipient category using the helper.
            // The out parameters assign the created TextBoxes to the class fields.
            AddCategoryControls("Auto Std. Daily Recipients", out txtAutoRunDailyStandardRecipientsTo, out txtAutoRunDailyStandardRecipientsCC, "AutoRunDailyStandardRecipients");
            AddCategoryControls("Auto Daily (5d>=£1k) Recipients", out txtAutoRunDaily5Day1kRecipientsTo, out txtAutoRunDaily5Day1kRecipientsCC, "AutoRunDaily5Day1kRecipients");
            AddCategoryControls("Auto Weekly Recipients", out txtAutoRunWeeklyRecipientsTo, out txtAutoRunWeeklyRecipientsCC, "AutoRunWeeklyRecipients");

            // Example: If you add more categories like "AutoRunMonthlyMarketingRecipients" in UserEmailSettings and appsettings.json:
            // AddCategoryControls("Auto Monthly Marketing Recipients", out txtAutoRunMonthlyMarketingRecipientsTo, out txtAutoRunMonthlyMarketingRecipientsCC, "AutoRunMonthlyMarketingRecipients");

            // Add a final spacer row to push controls upwards if the panel is taller than needed.
            automatedReportsTableLayoutPanel.RowCount++;
            automatedReportsTableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            Logger.LogInfo("Programmatically (re)created UI elements for category-based Automated Report recipients.");
        }

        /// <summary>
        /// Sets up informational tooltips for various controls on the form.
        /// </summary>
        private void SetupToolTips()
        {
            // Ensure the ToolTipProvider component is initialised.
            this.toolTipProvider ??= new System.Windows.Forms.ToolTip(this.components ??= new System.ComponentModel.Container());

            // --- Automated Reports Tab (New Category-Based Tooltips) ---
            // Check if TextBoxes are not null before setting tooltips (they are created in InitializeAutomatedReportControls).
            if (txtAutoRunDailyStandardRecipientsTo != null) toolTipProvider.SetToolTip(this.txtAutoRunDailyStandardRecipientsTo, "Override 'To' for AUTOMATED Standard Daily reports. Separate emails with comma/semicolon.");
            if (txtAutoRunDailyStandardRecipientsCC != null) toolTipProvider.SetToolTip(this.txtAutoRunDailyStandardRecipientsCC, "Override 'CC' for AUTOMATED Standard Daily reports. Separate emails with comma/semicolon.");
            if (txtAutoRunDaily5Day1kRecipientsTo != null) toolTipProvider.SetToolTip(this.txtAutoRunDaily5Day1kRecipientsTo, "Override 'To' for AUTOMATED 'Daily (5days >= £1k)' reports. Separate emails with comma/semicolon.");
            if (txtAutoRunDaily5Day1kRecipientsCC != null) toolTipProvider.SetToolTip(this.txtAutoRunDaily5Day1kRecipientsCC, "Override 'CC' for AUTOMATED 'Daily (5days >= £1k)' reports. Separate emails with comma/semicolon.");
            if (txtAutoRunWeeklyRecipientsTo != null) toolTipProvider.SetToolTip(this.txtAutoRunWeeklyRecipientsTo, "Override 'To' for AUTOMATED Weekly reports. Separate emails with comma/semicolon.");
            if (txtAutoRunWeeklyRecipientsCC != null) toolTipProvider.SetToolTip(this.txtAutoRunWeeklyRecipientsCC, "Override 'CC' for AUTOMATED Weekly reports. Separate emails with comma/semicolon.");
            // Add tooltips for other new automated category textboxes if they were added.

            // --- Manual Reports Tab ---
            toolTipProvider.SetToolTip(this.txtProdManualRunDailyTo, "Default 'To' for MANUALLY RUN standard daily reports. Separate emails with comma/semicolon.");
            toolTipProvider.SetToolTip(this.txtProdManualRunDailyCC, "Default 'CC' for MANUALLY RUN standard daily reports. Separate emails with comma/semicolon.");
            toolTipProvider.SetToolTip(this.txtProdFemiTo, "'To' recipients for manual non-daily/non-custom reports when 'Send to Femi Only' is checked. Separate emails with comma/semicolon.");
            toolTipProvider.SetToolTip(this.txtProdFemiCC, "'CC' recipients for manual non-daily/non-custom reports when 'Send to Femi Only' is checked. Separate emails with comma/semicolon.");
            toolTipProvider.SetToolTip(this.txtProdTeamTo, "'To' recipients for manual non-daily/non-custom reports (team list). Separate emails with comma/semicolon.");
            toolTipProvider.SetToolTip(this.txtProdTeamCC, "'CC' recipients for manual non-daily/non-custom reports (team list). Separate emails with comma/semicolon.");

            // Tooltip for "Manual Custom Report" fields (check for null).
            if (txtProdManualCustomTo != null) toolTipProvider.SetToolTip(this.txtProdManualCustomTo, "Default 'To' for MANUALLY RUN custom reports. Separate emails with comma/semicolon.");
            if (txtProdManualCustomCC != null) toolTipProvider.SetToolTip(this.txtProdManualCustomCC, "Default 'CC' for MANUALLY RUN custom reports. Separate emails with comma/semicolon.");

#if DEBUG
            // --- Debug Tab ---
            // Check for null for debug controls as well, although they are usually in the designer.
            if (txtDebugTo != null) toolTipProvider.SetToolTip(this.txtDebugTo, "Primary 'To' recipient for ALL reports in DEBUG mode. Single email address.");
            if (txtDebugCC1 != null) toolTipProvider.SetToolTip(this.txtDebugCC1, "First 'CC' recipient for ALL reports in DEBUG mode. Single email address.");
            if (txtDebugCC2 != null) toolTipProvider.SetToolTip(this.txtDebugCC2, "Second 'CC' recipient for ALL reports in DEBUG mode. Single email address.");
#endif

            // --- Buttons ---
            toolTipProvider.SetToolTip(this.btnSave, "Save the current email settings. These will override application defaults.");
            toolTipProvider.SetToolTip(this.btnRestoreDefaults, "Clear all custom settings and revert to the application's built-in default email lists.");
            toolTipProvider.SetToolTip(this.btnClose, "Close this window without saving any changes made since the last save.");
        }

        /// <summary>
        /// Loads current effective email settings into the form's controls.
        /// This includes populating the new category-based TextBoxes for automated reports.
        /// </summary>
        private void LoadSettingsToForm()
        {
            UserEmailSettings currentSettings = _emailRecipientManager.GetCurrentEffectiveSettings();

            // --- Automated Reports Tab (New Category-Based Properties) ---
            // Check for null before accessing Text property, as these are now programmatically managed.
            if (txtAutoRunDailyStandardRecipientsTo != null) txtAutoRunDailyStandardRecipientsTo.Text = string.Join(", ", currentSettings.AutoRunDailyStandardRecipientsTo ?? Enumerable.Empty<string>());
            if (txtAutoRunDailyStandardRecipientsCC != null) txtAutoRunDailyStandardRecipientsCC.Text = string.Join(", ", currentSettings.AutoRunDailyStandardRecipientsCC ?? Enumerable.Empty<string>());
            if (txtAutoRunDaily5Day1kRecipientsTo != null) txtAutoRunDaily5Day1kRecipientsTo.Text = string.Join(", ", currentSettings.AutoRunDaily5Day1kRecipientsTo ?? Enumerable.Empty<string>());
            if (txtAutoRunDaily5Day1kRecipientsCC != null) txtAutoRunDaily5Day1kRecipientsCC.Text = string.Join(", ", currentSettings.AutoRunDaily5Day1kRecipientsCC ?? Enumerable.Empty<string>());
            if (txtAutoRunWeeklyRecipientsTo != null) txtAutoRunWeeklyRecipientsTo.Text = string.Join(", ", currentSettings.AutoRunWeeklyRecipientsTo ?? Enumerable.Empty<string>());
            if (txtAutoRunWeeklyRecipientsCC != null) txtAutoRunWeeklyRecipientsCC.Text = string.Join(", ", currentSettings.AutoRunWeeklyRecipientsCC ?? Enumerable.Empty<string>());
            // Load other new automated category textboxes if they were added.

            // --- Manual Reports Tab ---
            txtProdManualRunDailyTo.Text = string.Join(", ", currentSettings.ProdManualRunDailyTo ?? Enumerable.Empty<string>());
            txtProdManualRunDailyCC.Text = string.Join(", ", currentSettings.ProdManualRunDailyCC ?? Enumerable.Empty<string>());
            txtProdFemiTo.Text = string.Join(", ", currentSettings.ProdFemiTo ?? Enumerable.Empty<string>());
            txtProdFemiCC.Text = string.Join(", ", currentSettings.ProdFemiCC ?? Enumerable.Empty<string>());
            txtProdTeamTo.Text = string.Join(", ", currentSettings.ProdTeamTo ?? Enumerable.Empty<string>());
            txtProdTeamCC.Text = string.Join(", ", currentSettings.ProdTeamCC ?? Enumerable.Empty<string>());

            // Load "Manual Custom Report" fields (check for null).
            if (txtProdManualCustomTo != null) txtProdManualCustomTo.Text = string.Join(", ", currentSettings.ProdManualCustomTo ?? Enumerable.Empty<string>());
            if (txtProdManualCustomCC != null) txtProdManualCustomCC.Text = string.Join(", ", currentSettings.ProdManualCustomCC ?? Enumerable.Empty<string>());

#if DEBUG
            // --- Debug Tab ---
            // Check for null for debug controls.
            if (txtDebugTo != null) txtDebugTo.Text = currentSettings.DebugTo ?? string.Empty;
            if (txtDebugCC1 != null) txtDebugCC1.Text = currentSettings.DebugCC1 ?? string.Empty;
            if (txtDebugCC2 != null) txtDebugCC2.Text = currentSettings.DebugCC2 ?? string.Empty;
#endif
            Logger.LogInfo("Loaded current email settings into ManageEmailRecipientsForm.");
        }
        #endregion

        #region Button Event Handlers
        /// <summary>
        /// Handles the Click event for the "Save" button.
        /// Gathers data from all TextBoxes, validates emails, and saves settings.
        /// </summary>
        private void BtnSave_Click(object sender, EventArgs e)
        {
            Logger.LogInfo("Save button clicked on ManageEmailRecipientsForm.");
            var newSettings = new UserEmailSettings
            {
                // --- Automated Reports (New Category-Based Properties) ---
                // Ensure TextBoxes are not null before accessing Text.
                AutoRunDailyStandardRecipientsTo = txtAutoRunDailyStandardRecipientsTo != null ? StringToEmailList(txtAutoRunDailyStandardRecipientsTo.Text) : new List<string>(),
                AutoRunDailyStandardRecipientsCC = txtAutoRunDailyStandardRecipientsCC != null ? StringToEmailList(txtAutoRunDailyStandardRecipientsCC.Text) : new List<string>(),
                AutoRunDaily5Day1kRecipientsTo = txtAutoRunDaily5Day1kRecipientsTo != null ? StringToEmailList(txtAutoRunDaily5Day1kRecipientsTo.Text) : new List<string>(),
                AutoRunDaily5Day1kRecipientsCC = txtAutoRunDaily5Day1kRecipientsCC != null ? StringToEmailList(txtAutoRunDaily5Day1kRecipientsCC.Text) : new List<string>(),
                AutoRunWeeklyRecipientsTo = txtAutoRunWeeklyRecipientsTo != null ? StringToEmailList(txtAutoRunWeeklyRecipientsTo.Text) : new List<string>(),
                AutoRunWeeklyRecipientsCC = txtAutoRunWeeklyRecipientsCC != null ? StringToEmailList(txtAutoRunWeeklyRecipientsCC.Text) : new List<string>(),
                // Save other new automated category textboxes if added.

                // --- Manual Reports ---
                ProdManualRunDailyTo = StringToEmailList(txtProdManualRunDailyTo.Text),
                ProdManualRunDailyCC = StringToEmailList(txtProdManualRunDailyCC.Text),
                ProdFemiTo = StringToEmailList(txtProdFemiTo.Text),
                ProdFemiCC = StringToEmailList(txtProdFemiCC.Text),
                ProdTeamTo = StringToEmailList(txtProdTeamTo.Text),
                ProdTeamCC = StringToEmailList(txtProdTeamCC.Text),
                ProdManualCustomTo = txtProdManualCustomTo != null ? StringToEmailList(txtProdManualCustomTo.Text) : new List<string>(),
                ProdManualCustomCC = txtProdManualCustomCC != null ? StringToEmailList(txtProdManualCustomCC.Text) : new List<string>()
            };

#if DEBUG
            // --- Debug Settings ---
            // Check for null for debug controls.
            if (txtDebugTo != null) newSettings.DebugTo = txtDebugTo.Text.Trim();
            if (txtDebugCC1 != null) newSettings.DebugCC1 = txtDebugCC1.Text.Trim();
            if (txtDebugCC2 != null) newSettings.DebugCC2 = txtDebugCC2.Text.Trim();
#else
            // In Release, preserve existing debug settings to avoid accidental clearing.
            UserEmailSettings currentEffectiveSettings = _emailRecipientManager.GetCurrentEffectiveSettings();
            newSettings.DebugTo = currentEffectiveSettings.DebugTo;
            newSettings.DebugCC1 = currentEffectiveSettings.DebugCC1;
            newSettings.DebugCC2 = currentEffectiveSettings.DebugCC2;
#endif

            // Consolidate all email addresses for validation.
            List<string> allEmailsToValidate = new List<string>();
            allEmailsToValidate.AddRange(newSettings.AutoRunDailyStandardRecipientsTo ?? Enumerable.Empty<string>());
            allEmailsToValidate.AddRange(newSettings.AutoRunDailyStandardRecipientsCC ?? Enumerable.Empty<string>());
            allEmailsToValidate.AddRange(newSettings.AutoRunDaily5Day1kRecipientsTo ?? Enumerable.Empty<string>());
            allEmailsToValidate.AddRange(newSettings.AutoRunDaily5Day1kRecipientsCC ?? Enumerable.Empty<string>());
            allEmailsToValidate.AddRange(newSettings.AutoRunWeeklyRecipientsTo ?? Enumerable.Empty<string>());
            allEmailsToValidate.AddRange(newSettings.AutoRunWeeklyRecipientsCC ?? Enumerable.Empty<string>());
            // Add other new automated category lists to validation.

            allEmailsToValidate.AddRange(newSettings.ProdManualRunDailyTo ?? Enumerable.Empty<string>());
            allEmailsToValidate.AddRange(newSettings.ProdManualRunDailyCC ?? Enumerable.Empty<string>());
            allEmailsToValidate.AddRange(newSettings.ProdFemiTo ?? Enumerable.Empty<string>());
            allEmailsToValidate.AddRange(newSettings.ProdFemiCC ?? Enumerable.Empty<string>());
            allEmailsToValidate.AddRange(newSettings.ProdTeamTo ?? Enumerable.Empty<string>());
            allEmailsToValidate.AddRange(newSettings.ProdTeamCC ?? Enumerable.Empty<string>());
            allEmailsToValidate.AddRange(newSettings.ProdManualCustomTo ?? Enumerable.Empty<string>());
            allEmailsToValidate.AddRange(newSettings.ProdManualCustomCC ?? Enumerable.Empty<string>());

#if DEBUG
            if (!string.IsNullOrWhiteSpace(newSettings.DebugTo)) allEmailsToValidate.Add(newSettings.DebugTo);
            if (!string.IsNullOrWhiteSpace(newSettings.DebugCC1)) allEmailsToValidate.Add(newSettings.DebugCC1);
            if (!string.IsNullOrWhiteSpace(newSettings.DebugCC2)) allEmailsToValidate.Add(newSettings.DebugCC2);
#endif

            // Validate all collected email addresses.
            if (!EmailRecipientManager.ValidateEmailAddresses(allEmailsToValidate, out List<string> invalidEmails))
            {
                Logger.LogWarning($"Invalid email addresses found: {string.Join(", ", invalidEmails)}");
                FlexibleMessageBox.Show(this, $"The following email addresses are invalid:\n\n{string.Join("\n", invalidEmails)}\n\nPlease correct them and try again.",
                    "Invalid Email Addresses", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return; // Stop the save process.
            }

            // Confirm with the user before saving.
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
                    DialogResult = DialogResult.OK; // Set DialogResult for the calling form.
                    Close(); // Close the form.
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
        /// Handles the Click event for the "Restore Defaults" button.
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
                    LoadSettingsToForm(); // Reloads defaults into the form.
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
        /// Handles the Click event for the "Close" button.
        /// </summary>
        private void BtnClose_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel; // Set DialogResult to Cancel.
            Close(); // Close the form.
        }
        #endregion

        #region Utility Methods
        /// <summary>
        /// Converts a comma or semicolon-separated string of email addresses into a list of strings.
        /// Trims whitespace from each email and removes any empty entries.
        /// </summary>
        /// <param name="emailString">The string containing email addresses.</param>
        /// <returns>A <see cref="List{T}"/> of trimmed, non-empty email addresses.</returns>
        private List<string> StringToEmailList(string emailString)
        {
            if (string.IsNullOrWhiteSpace(emailString))
            {
                return new List<string>(); // Return an empty list if the input string is null or whitespace.
            }
            // Split the string by comma or semicolon, remove empty entries, trim whitespace,
            // and filter out any remaining whitespace-only strings.
            return emailString.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                              .Select(email => email.Trim())
                              .Where(email => !string.IsNullOrWhiteSpace(email))
                              .ToList();
        }
        #endregion
    }
}
