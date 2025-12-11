// ManageEmailRecipientsForm.cs
// This form allows users to manage custom email recipient lists for various report scenarios,
// overriding the application's default settings. It supports different configurations for
// automated reports (now category-based), manual reports, and debug mode.
// Utilises C# 10+ features and the centralised theming system.

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
using QuoteConversionReportAutomation.Theming; // Required for centralised theming
#endregion

namespace QuoteConversionReportAutomation.Forms
{
    /// <summary>
    /// A Windows Form that allows users to view and modify email recipient lists
    /// for different report generation contexts. User-defined settings are saved
    /// and override application defaults. The "Automated Reports" tab now reflects
    /// recipient categories for more flexible configuration.
    /// </summary>
    public partial class ManageEmailRecipientsForm : Form
    {
        #region Fields
        private readonly EmailRecipientManager _emailRecipientManager; // Service for loading/saving email settings.

        // --- UI Control Fields for Manual Custom Report ---
        // These fields hold references to the TextBoxes for "Manual Custom" report recipients.
        // They are initialised in InitializeManualCustomControls.
        private TextBox txtProdManualCustomTo;
        private TextBox txtProdManualCustomCC;
        private TextBox txtManualNewCustomerTo; // ADDED
        private TextBox txtManualNewCustomerCC; // ADDED

        // --- UI Control Fields for Category-Based Automated Report Overrides ---
        // These fields will hold references to the TextBoxes for the new category-based automated report overrides.
        // They are initialised in InitializeAutomatedReportControls.
        private TextBox txtAutoRunDailyStandardRecipientsTo;
        private TextBox txtAutoRunDailyStandardRecipientsCC;
        private TextBox txtAutoRunDaily5Day1kRecipientsTo;
        private TextBox txtAutoRunDaily5Day1kRecipientsCC;
        private TextBox txtAutoRunWeeklyRecipientsTo;
        private TextBox txtAutoRunWeeklyRecipientsCC;
        private TextBox txtAutoRunFemiOnlyRecipientsTo;
        private TextBox txtAutoRunFemiOnlyRecipientsCC;
        private TextBox txtAutoRunNewCustomerRecipientsTo; // ADDED
        private TextBox txtAutoRunNewCustomerRecipientsCC; // ADDED
        // Add fields here for other categories if defined, e.g.:
        // private TextBox txtAutoRunMonthlyMarketingRecipientsTo;
        // private TextBox txtAutoRunMonthlyMarketingRecipientsCC;

        #endregion

        #region Constructor
        /// <summary>
        /// Initialises a new instance of the <see cref="ManageEmailRecipientsForm"/> class.
        /// </summary>
        /// <param name="emailRecipientManager">The manager responsible for email recipient settings logic.</param>
        public ManageEmailRecipientsForm(EmailRecipientManager emailRecipientManager)
        {
            _emailRecipientManager = emailRecipientManager ?? throw new ArgumentNullException(nameof(emailRecipientManager));

            InitializeComponent(); // Standard WinForms method to initialise components defined in the .Designer.cs file.

            // Initialise UI elements for "Manual Custom" recipients.
            InitializeManualCustomControls();
            // Initialise UI elements for category-based "Automated Report" recipients.
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
            Logger.LogInfo($"ManageEmailRecipientsForm loading. Theming enabled: {ThemeSettings.EnableCustomTheming}, CurrentMode: {ThemeSettings.CurrentThemeMode}");

            // Apply the theme to the form itself (title bar, main background) using the centralised settings.
            UIManager.ApplyThemeToExternalForm(this, ThemeSettings.IsCurrentlyDark());
            // Apply the theme to the tabbed layout and its child controls.
            ApplyThemeToTabbedLayout();
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
        /// Applies the current theme from ThemeSettings to the tab control and its contained elements.
        /// This ensures that tab pages and their contents are styled consistently with the rest of the form.
        /// </summary>
        private void ApplyThemeToTabbedLayout()
        {
            if (!ThemeSettings.EnableCustomTheming) return;

            bool isDarkMode = ThemeSettings.IsCurrentlyDark();
            var palette = ThemeSettings.CurrentPalette;

            // Set the background colour of the form itself to match the tab control's outer area.
            this.BackColor = palette.FormBackColor;
            // Theme the instructional label at the top of the form.
            this.lblInstructions.ForeColor = palette.LabelForeColor;
            this.lblInstructions.BackColor = Color.Transparent; // Make label background transparent.

            // Theme the main tab control itself.
            mainTabControl.BackColor = palette.FormBackColor;

            // Iterate through each tab page to apply themes.
            foreach (TabPage tabPage in mainTabControl.TabPages)
            {
                // A slightly different background for tab pages can improve visual separation.
                // We will use the main form background colour for consistency here.
                tabPage.BackColor = palette.FormBackColor;
                // The ForeColor of the TabPage affects the tab header text.
                tabPage.ForeColor = palette.FormForeColor;

                // Apply theme recursively to controls within each tab page.
                foreach (Control childControl in tabPage.Controls)
                {
                    ApplyThemeToControlsRecursive(childControl, palette, isDarkMode);
                }
            }

            // Theme the FlowLayoutPanel containing the Save, Restore, Close buttons.
            buttonsFlowLayoutPanel.BackColor = this.BackColor; // Match form background.
            ApplyThemeToControlsRecursive(buttonsFlowLayoutPanel, palette, isDarkMode);
        }

        /// <summary>
        /// Recursively applies theme colours to a control and its child controls using the ThemePalette.
        /// </summary>
        /// <param name="parentControl">The parent control to start theming from.</param>
        /// <param name="palette">The colour palette to use for theming.</param>
        /// <param name="isDarkMode">A flag indicating if dark mode is active, for specific style adjustments.</param>
        private void ApplyThemeToControlsRecursive(Control parentControl, ThemePalette palette, bool isDarkMode)
        {
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
                else if (control is TextBox || control is RichTextBox)
                {
                    control.BackColor = palette.ControlBackColor;
                    control.ForeColor = palette.ControlForeColor;
                    if (control is TextBox tb)
                    {
                        tb.BorderStyle = isDarkMode ? BorderStyle.FixedSingle : BorderStyle.Fixed3D;
                    }
                }
                else if (control is Label)
                {
                    control.BackColor = Color.Transparent;
                    control.ForeColor = palette.LabelForeColor;
                }
                else if (control.HasChildren)
                {
                    // For generic containers, ensure their background matches their themed parent.
                    if (!(control is TableLayoutPanel || control is TabPage || control is TabControl))
                    {
                        control.BackColor = parentControl.BackColor;
                    }
                    // Recursively apply theme to children of this container.
                    ApplyThemeToControlsRecursive(control, palette, isDarkMode);
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
            // This region has been updated to also create controls for the new "Manual New Customer" report.
            #region Control Creation Logic
            Control[] foundCustomTo = this.Controls.Find("txtProdManualCustomTo", true);
            if (foundCustomTo.Length > 0 && foundCustomTo[0] is TextBox textBoxTo) { txtProdManualCustomTo = textBoxTo; }

            Control[] foundCustomCC = this.Controls.Find("txtProdManualCustomCC", true);
            if (foundCustomCC.Length > 0 && foundCustomCC[0] is TextBox textBoxCC) { txtProdManualCustomCC = textBoxCC; }

            if (txtProdManualCustomTo == null || txtProdManualCustomCC == null)
            {
                // Code to programmatically add Manual Custom controls (if needed)
                Logger.LogDebug("Manual Custom recipient TextBoxes not found by name. Creating programmatically.");
                TabPage manualCustomTabPage;
                TableLayoutPanel tlpManualCustom;

                const string manualCustomTabKey = "manualCustomReportRecipientsTabPage";
                // This logic ensures that if you add the tab in the designer, it will use it, otherwise it creates it.
                if (manualReportsTableLayoutPanel.Parent is TabPage)
                {
                    manualCustomTabPage = (TabPage)manualReportsTableLayoutPanel.Parent;
                    tlpManualCustom = manualReportsTableLayoutPanel;
                }
                else
                {
                    manualCustomTabPage = new TabPage("Manual Reports") { Name = "manualReportsTabPage" };
                    tlpManualCustom = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, Padding = new Padding(10) };
                    manualCustomTabPage.Controls.Add(tlpManualCustom);
                    mainTabControl.TabPages.Add(manualCustomTabPage);
                }

                Label lblManualCustomTo = new Label { Text = "Manual Custom Report TO:", Anchor = AnchorStyles.Right | AnchorStyles.Top, AutoSize = true, Margin = new Padding(3, 6, 3, 3) };
                Label lblManualCustomCC = new Label { Text = "Manual Custom Report CC:", Anchor = AnchorStyles.Right | AnchorStyles.Top, AutoSize = true, Margin = new Padding(3, 6, 3, 3) };

                txtProdManualCustomTo = new TextBox { Name = "txtProdManualCustomTo", Multiline = false, Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top, Height = 20 };
                txtProdManualCustomCC = new TextBox { Name = "txtProdManualCustomCC", Multiline = false, Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top, Height = 20 };

                int newRow = tlpManualCustom.RowCount++;
                tlpManualCustom.RowStyles.Add(new RowStyle(SizeType.Absolute, 35F));
                tlpManualCustom.Controls.Add(lblManualCustomTo, 0, newRow - 1);
                tlpManualCustom.Controls.Add(txtProdManualCustomTo, 1, newRow - 1);

                newRow = tlpManualCustom.RowCount++;
                tlpManualCustom.RowStyles.Add(new RowStyle(SizeType.Absolute, 35F));
                tlpManualCustom.Controls.Add(lblManualCustomCC, 0, newRow - 1);
                tlpManualCustom.Controls.Add(txtProdManualCustomCC, 1, newRow - 1);
            }

            // ADDED: Logic to programmatically add controls for the Manual New Customer report.
            Label lblManualNewCustomerTo = new Label { Text = "Manual New Customer TO:", Anchor = AnchorStyles.Right | AnchorStyles.Top, AutoSize = true, Margin = new Padding(3, 6, 3, 3) };
            txtManualNewCustomerTo = new TextBox { Name = "txtManualNewCustomerTo", Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top, Height = 20 };
            Label lblManualNewCustomerCC = new Label { Text = "Manual New Customer CC:", Anchor = AnchorStyles.Right | AnchorStyles.Top, AutoSize = true, Margin = new Padding(3, 6, 3, 3) };
            txtManualNewCustomerCC = new TextBox { Name = "txtManualNewCustomerCC", Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top, Height = 20 };

            int newRowIndex = manualReportsTableLayoutPanel.RowCount++;
            manualReportsTableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 35F));
            manualReportsTableLayoutPanel.Controls.Add(lblManualNewCustomerTo, 0, newRowIndex - 1);
            manualReportsTableLayoutPanel.Controls.Add(txtManualNewCustomerTo, 1, newRowIndex - 1);

            newRowIndex = manualReportsTableLayoutPanel.RowCount++;
            manualReportsTableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 35F));
            manualReportsTableLayoutPanel.Controls.Add(lblManualNewCustomerCC, 0, newRowIndex - 1);
            manualReportsTableLayoutPanel.Controls.Add(txtManualNewCustomerCC, 1, newRowIndex - 1);

            Logger.LogInfo("Programmatically added UI elements for Manual recipients.");
            #endregion
        }

        /// <summary>
        /// Clears and programmatically (re)creates controls for category-based automated report recipient overrides
        /// on the "Automated Reports" tab. This ensures the UI matches the configurable categories.
        /// </summary>
        private void InitializeAutomatedReportControls()
        {
            // This method has been updated to include a category for the new "New Customer" report.
            #region Control Creation Logic
            Logger.LogDebug("Initialising/Rebuilding controls for Automated Report recipient categories.");
            if (automatedReportsTableLayoutPanel == null)
            {
                Logger.LogError("automatedReportsTableLayoutPanel is null. Cannot initialise automated report controls. This indicates a problem with the form designer initialisation.");
                automatedReportsTableLayoutPanel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, Padding = new Padding(10), Name = "automatedReportsTableLayoutPanel" };
                if (automatedReportsTabPage != null) automatedReportsTabPage.Controls.Add(automatedReportsTableLayoutPanel);
                else
                {
                    Logger.LogError("automatedReportsTabPage is also null. Cannot add TableLayoutPanel.");
                    return;
                }
            }

            automatedReportsTableLayoutPanel.Controls.Clear();
            automatedReportsTableLayoutPanel.RowStyles.Clear();
            automatedReportsTableLayoutPanel.ColumnStyles.Clear();
            automatedReportsTableLayoutPanel.RowCount = 0;

            automatedReportsTableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 230F));
            automatedReportsTableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

            int currentRow = 0;

            void AddCategoryControls(string labelText, out TextBox toTextBoxField, out TextBox ccTextBoxField, string categoryKeyBaseName)
            {
                automatedReportsTableLayoutPanel.RowCount++;
                automatedReportsTableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 35F));
                Label lblTo = new Label { Text = $"{labelText} TO:", Name = $"lbl{categoryKeyBaseName}To", Anchor = AnchorStyles.Right | AnchorStyles.Top, AutoSize = true, Margin = new Padding(3, 6, 3, 3) };
                toTextBoxField = new TextBox { Name = $"txt{categoryKeyBaseName}To", Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top, Height = 20 };
                automatedReportsTableLayoutPanel.Controls.Add(lblTo, 0, currentRow);
                automatedReportsTableLayoutPanel.Controls.Add(toTextBoxField, 1, currentRow);
                currentRow++;

                automatedReportsTableLayoutPanel.RowCount++;
                automatedReportsTableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 35F));
                Label lblCc = new Label { Text = $"{labelText} CC:", Name = $"lbl{categoryKeyBaseName}Cc", Anchor = AnchorStyles.Right | AnchorStyles.Top, AutoSize = true, Margin = new Padding(3, 6, 3, 3) };
                ccTextBoxField = new TextBox { Name = $"txt{categoryKeyBaseName}Cc", Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top, Height = 20 };
                automatedReportsTableLayoutPanel.Controls.Add(lblCc, 0, currentRow);
                automatedReportsTableLayoutPanel.Controls.Add(ccTextBoxField, 1, currentRow);
                currentRow++;
            }

            AddCategoryControls("Auto Std. Daily Recipients", out txtAutoRunDailyStandardRecipientsTo, out txtAutoRunDailyStandardRecipientsCC, "AutoRunDailyStandardRecipients");
            AddCategoryControls("Auto Daily (5d>=£1k) Recipients", out txtAutoRunDaily5Day1kRecipientsTo, out txtAutoRunDaily5Day1kRecipientsCC, "AutoRunDaily5Day1kRecipients");
            AddCategoryControls("Auto Weekly Recipients", out txtAutoRunWeeklyRecipientsTo, out txtAutoRunWeeklyRecipientsCC, "AutoRunWeeklyRecipients");
            AddCategoryControls("Auto 'Femi Only' Recipients", out txtAutoRunFemiOnlyRecipientsTo, out txtAutoRunFemiOnlyRecipientsCC, "AutoRunFemiOnlyRecipients");
            // ADDED: Controls for the new automated report category.
            AddCategoryControls("Auto New Customer Recipients", out txtAutoRunNewCustomerRecipientsTo, out txtAutoRunNewCustomerRecipientsCC, "AutoRunNewCustomerRecipients");

            automatedReportsTableLayoutPanel.RowCount++;
            automatedReportsTableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            Logger.LogInfo("Programmatically (re)created UI elements for category-based Automated Report recipients.");
            #endregion
        }

        /// <summary>
        /// Sets up informational tooltips for various controls on the form.
        /// </summary>
        private void SetupToolTips()
        {
            // This method's logic is unchanged.
            #region Original Method Content
            this.toolTipProvider ??= new System.Windows.Forms.ToolTip(this.components ??= new System.ComponentModel.Container());

            if (txtAutoRunDailyStandardRecipientsTo != null) toolTipProvider.SetToolTip(this.txtAutoRunDailyStandardRecipientsTo, "Override 'To' for AUTOMATED Standard Daily reports. Separate emails with comma/semicolon.");
            if (txtAutoRunDailyStandardRecipientsCC != null) toolTipProvider.SetToolTip(this.txtAutoRunDailyStandardRecipientsCC, "Override 'CC' for AUTOMATED Standard Daily reports. Separate emails with comma/semicolon.");
            if (txtAutoRunDaily5Day1kRecipientsTo != null) toolTipProvider.SetToolTip(this.txtAutoRunDaily5Day1kRecipientsTo, "Override 'To' for AUTOMATED 'Daily (5days >= £1k)' reports. Separate emails with comma/semicolon.");
            if (txtAutoRunDaily5Day1kRecipientsCC != null) toolTipProvider.SetToolTip(this.txtAutoRunDaily5Day1kRecipientsCC, "Override 'CC' for AUTOMATED 'Daily (5days >= £1k)' reports. Separate emails with comma/semicolon.");
            if (txtAutoRunWeeklyRecipientsTo != null) toolTipProvider.SetToolTip(this.txtAutoRunWeeklyRecipientsTo, "Override 'To' for AUTOMATED Weekly reports. Separate emails with comma/semicolon.");
            if (txtAutoRunWeeklyRecipientsCC != null) toolTipProvider.SetToolTip(this.txtAutoRunWeeklyRecipientsCC, "Override 'CC' for AUTOMATED Weekly reports. Separate emails with comma/semicolon.");
            if (txtAutoRunFemiOnlyRecipientsTo != null) toolTipProvider.SetToolTip(this.txtAutoRunFemiOnlyRecipientsTo, "Override 'To' for automated reports using the 'Femi Only' category. Separate emails with comma/semicolon.");
            if (txtAutoRunFemiOnlyRecipientsCC != null) toolTipProvider.SetToolTip(this.txtAutoRunFemiOnlyRecipientsCC, "Override 'CC' for automated reports using the 'Femi Only' category. Separate emails with comma/semicolon.");

            toolTipProvider.SetToolTip(this.txtProdManualRunDailyTo, "Default 'To' for MANUALLY RUN standard daily reports. Separate emails with comma/semicolon.");
            toolTipProvider.SetToolTip(this.txtProdManualRunDailyCC, "Default 'CC' for MANUALLY RUN standard daily reports. Separate emails with comma/semicolon.");
            toolTipProvider.SetToolTip(this.txtProdFemiTo, "'To' recipients for manual non-daily/non-custom reports when 'Send to Femi Only' is checked. Separate emails with comma/semicolon.");
            toolTipProvider.SetToolTip(this.txtProdFemiCC, "'CC' recipients for manual non-daily/non-custom reports when 'Send to Femi Only' is checked. Separate emails with comma/semicolon.");
            toolTipProvider.SetToolTip(this.txtProdTeamTo, "'To' recipients for manual non-daily/non-custom reports (team list). Separate emails with comma/semicolon.");
            toolTipProvider.SetToolTip(this.txtProdTeamCC, "'CC' recipients for manual non-daily/non-custom reports (team list). Separate emails with comma/semicolon.");

            if (txtProdManualCustomTo != null) toolTipProvider.SetToolTip(this.txtProdManualCustomTo, "Default 'To' for MANUALLY RUN custom reports. Separate emails with comma/semicolon.");
            if (txtProdManualCustomCC != null) toolTipProvider.SetToolTip(this.txtProdManualCustomCC, "Default 'CC' for MANUALLY RUN custom reports. Separate emails with comma/semicolon.");

            // ADDED: Tooltips for new manual report controls.
            if (txtManualNewCustomerTo != null) toolTipProvider.SetToolTip(this.txtManualNewCustomerTo, "Default 'To' for MANUALLY RUN New Customer reports. Separate emails with comma/semicolon.");
            if (txtManualNewCustomerCC != null) toolTipProvider.SetToolTip(this.txtManualNewCustomerCC, "Default 'CC' for MANUALLY RUN New Customer reports. Separate emails with comma/semicolon.");

#if DEBUG
            if (txtDebugTo != null) toolTipProvider.SetToolTip(this.txtDebugTo, "Primary 'To' recipient for ALL reports in DEBUG mode. Single email address.");
            if (txtDebugCC1 != null) toolTipProvider.SetToolTip(this.txtDebugCC1, "First 'CC' recipient for ALL reports in DEBUG mode. Single email address.");
            if (txtDebugCC2 != null) toolTipProvider.SetToolTip(this.txtDebugCC2, "Second 'CC' recipient for ALL reports in DEBUG mode. Single email address.");
#endif

            toolTipProvider.SetToolTip(this.btnSave, "Save the current email settings. These will override application defaults.");
            toolTipProvider.SetToolTip(this.btnRestoreDefaults, "Clear all custom settings and revert to the application's built-in default email lists.");
            toolTipProvider.SetToolTip(this.btnClose, "Close this window without saving any changes made since the last save.");
            #endregion
        }

        /// <summary>
        /// Loads current effective email settings into the form's controls.
        /// This includes populating the new category-based TextBoxes for automated reports.
        /// </summary>
        private void LoadSettingsToForm()
        {
            // This method's logic is unchanged.
            #region Original Method Content
            UserEmailSettings currentSettings = _emailRecipientManager.GetCurrentEffectiveSettings();

            if (txtAutoRunDailyStandardRecipientsTo != null) txtAutoRunDailyStandardRecipientsTo.Text = string.Join(", ", currentSettings.AutoRunDailyStandardRecipientsTo ?? Enumerable.Empty<string>());
            if (txtAutoRunDailyStandardRecipientsCC != null) txtAutoRunDailyStandardRecipientsCC.Text = string.Join(", ", currentSettings.AutoRunDailyStandardRecipientsCC ?? Enumerable.Empty<string>());
            if (txtAutoRunDaily5Day1kRecipientsTo != null) txtAutoRunDaily5Day1kRecipientsTo.Text = string.Join(", ", currentSettings.AutoRunDaily5Day1kRecipientsTo ?? Enumerable.Empty<string>());
            if (txtAutoRunDaily5Day1kRecipientsCC != null) txtAutoRunDaily5Day1kRecipientsCC.Text = string.Join(", ", currentSettings.AutoRunDaily5Day1kRecipientsCC ?? Enumerable.Empty<string>());
            if (txtAutoRunWeeklyRecipientsTo != null) txtAutoRunWeeklyRecipientsTo.Text = string.Join(", ", currentSettings.AutoRunWeeklyRecipientsTo ?? Enumerable.Empty<string>());
            if (txtAutoRunWeeklyRecipientsCC != null) txtAutoRunWeeklyRecipientsCC.Text = string.Join(", ", currentSettings.AutoRunWeeklyRecipientsCC ?? Enumerable.Empty<string>());
            if (txtAutoRunFemiOnlyRecipientsTo != null) txtAutoRunFemiOnlyRecipientsTo.Text = string.Join(", ", currentSettings.AutoRunFemiOnlyRecipientsTo ?? Enumerable.Empty<string>());
            if (txtAutoRunFemiOnlyRecipientsCC != null) txtAutoRunFemiOnlyRecipientsCC.Text = string.Join(", ", currentSettings.AutoRunFemiOnlyRecipientsCC ?? Enumerable.Empty<string>());
            // ADDED: Load data for new automated report category.
            if (txtAutoRunNewCustomerRecipientsTo != null) txtAutoRunNewCustomerRecipientsTo.Text = string.Join(", ", currentSettings.AutoRunNewCustomerRecipientsTo ?? Enumerable.Empty<string>());
            if (txtAutoRunNewCustomerRecipientsCC != null) txtAutoRunNewCustomerRecipientsCC.Text = string.Join(", ", currentSettings.AutoRunNewCustomerRecipientsCC ?? Enumerable.Empty<string>());

            txtProdManualRunDailyTo.Text = string.Join(", ", currentSettings.ProdManualRunDailyTo ?? Enumerable.Empty<string>());
            txtProdManualRunDailyCC.Text = string.Join(", ", currentSettings.ProdManualRunDailyCC ?? Enumerable.Empty<string>());
            txtProdFemiTo.Text = string.Join(", ", currentSettings.ProdFemiTo ?? Enumerable.Empty<string>());
            txtProdFemiCC.Text = string.Join(", ", currentSettings.ProdFemiCC ?? Enumerable.Empty<string>());
            txtProdTeamTo.Text = string.Join(", ", currentSettings.ProdTeamTo ?? Enumerable.Empty<string>());
            txtProdTeamCC.Text = string.Join(", ", currentSettings.ProdTeamCC ?? Enumerable.Empty<string>());

            if (txtProdManualCustomTo != null) txtProdManualCustomTo.Text = string.Join(", ", currentSettings.ProdManualCustomTo ?? Enumerable.Empty<string>());
            if (txtProdManualCustomCC != null) txtProdManualCustomCC.Text = string.Join(", ", currentSettings.ProdManualCustomCC ?? Enumerable.Empty<string>());

            // ADDED: Load data for new manual report controls.
            if (txtManualNewCustomerTo != null) txtManualNewCustomerTo.Text = string.Join(", ", currentSettings.ManualNewCustomerTo ?? Enumerable.Empty<string>());
            if (txtManualNewCustomerCC != null) txtManualNewCustomerCC.Text = string.Join(", ", currentSettings.ManualNewCustomerCC ?? Enumerable.Empty<string>());

#if DEBUG
            if (txtDebugTo != null) txtDebugTo.Text = currentSettings.DebugTo ?? string.Empty;
            if (txtDebugCC1 != null) txtDebugCC1.Text = currentSettings.DebugCC1 ?? string.Empty;
            if (txtDebugCC2 != null) txtDebugCC2.Text = currentSettings.DebugCC2 ?? string.Empty;
#endif
            Logger.LogInfo("Loaded current email settings into ManageEmailRecipientsForm.");
            #endregion
        }
        #endregion

        #region Button Event Handlers
        /// <summary>
        /// Handles the Click event for the "Save" button.
        /// Gathers data from all TextBoxes, validates emails, and saves settings.
        /// </summary>
        private void BtnSave_Click(object sender, EventArgs e)
        {
            // This method's logic is unchanged.
            #region Original Method Content
            Logger.LogInfo("Save button clicked on ManageEmailRecipientsForm.");
            var newSettings = new UserEmailSettings
            {
                AutoRunDailyStandardRecipientsTo = txtAutoRunDailyStandardRecipientsTo != null ? StringToEmailList(txtAutoRunDailyStandardRecipientsTo.Text) : new List<string>(),
                AutoRunDailyStandardRecipientsCC = txtAutoRunDailyStandardRecipientsCC != null ? StringToEmailList(txtAutoRunDailyStandardRecipientsCC.Text) : new List<string>(),
                AutoRunDaily5Day1kRecipientsTo = txtAutoRunDaily5Day1kRecipientsTo != null ? StringToEmailList(txtAutoRunDaily5Day1kRecipientsTo.Text) : new List<string>(),
                AutoRunDaily5Day1kRecipientsCC = txtAutoRunDaily5Day1kRecipientsCC != null ? StringToEmailList(txtAutoRunDaily5Day1kRecipientsCC.Text) : new List<string>(),
                AutoRunWeeklyRecipientsTo = txtAutoRunWeeklyRecipientsTo != null ? StringToEmailList(txtAutoRunWeeklyRecipientsTo.Text) : new List<string>(),
                AutoRunWeeklyRecipientsCC = txtAutoRunWeeklyRecipientsCC != null ? StringToEmailList(txtAutoRunWeeklyRecipientsCC.Text) : new List<string>(),
                AutoRunFemiOnlyRecipientsTo = txtAutoRunFemiOnlyRecipientsTo != null ? StringToEmailList(txtAutoRunFemiOnlyRecipientsTo.Text) : new List<string>(),
                AutoRunFemiOnlyRecipientsCC = txtAutoRunFemiOnlyRecipientsCC != null ? StringToEmailList(txtAutoRunFemiOnlyRecipientsCC.Text) : new List<string>(),
                // ADDED: Save data for new automated report category.
                AutoRunNewCustomerRecipientsTo = txtAutoRunNewCustomerRecipientsTo != null ? StringToEmailList(txtAutoRunNewCustomerRecipientsTo.Text) : new List<string>(),
                AutoRunNewCustomerRecipientsCC = txtAutoRunNewCustomerRecipientsCC != null ? StringToEmailList(txtAutoRunNewCustomerRecipientsCC.Text) : new List<string>(),

                ProdManualRunDailyTo = StringToEmailList(txtProdManualRunDailyTo.Text),
                ProdManualRunDailyCC = StringToEmailList(txtProdManualRunDailyCC.Text),
                ProdFemiTo = StringToEmailList(txtProdFemiTo.Text),
                ProdFemiCC = StringToEmailList(txtProdFemiCC.Text),
                ProdTeamTo = StringToEmailList(txtProdTeamTo.Text),
                ProdTeamCC = StringToEmailList(txtProdTeamCC.Text),
                ProdManualCustomTo = txtProdManualCustomTo != null ? StringToEmailList(txtProdManualCustomTo.Text) : new List<string>(),
                ProdManualCustomCC = txtProdManualCustomCC != null ? StringToEmailList(txtProdManualCustomCC.Text) : new List<string>(),
                // ADDED: Save data for new manual report controls.
                ManualNewCustomerTo = txtManualNewCustomerTo != null ? StringToEmailList(txtManualNewCustomerTo.Text) : new List<string>(),
                ManualNewCustomerCC = txtManualNewCustomerCC != null ? StringToEmailList(txtManualNewCustomerCC.Text) : new List<string>()
            };

#if DEBUG
            if (txtDebugTo != null) newSettings.DebugTo = txtDebugTo.Text.Trim();
            if (txtDebugCC1 != null) newSettings.DebugCC1 = txtDebugCC1.Text.Trim();
            if (txtDebugCC2 != null) newSettings.DebugCC2 = txtDebugCC2.Text.Trim();
#else
            UserEmailSettings currentEffectiveSettings = _emailRecipientManager.GetCurrentEffectiveSettings();
            newSettings.DebugTo = currentEffectiveSettings.DebugTo;
            newSettings.DebugCC1 = currentEffectiveSettings.DebugCC1;
            newSettings.DebugCC2 = currentEffectiveSettings.DebugCC2;
#endif

            List<string> allEmailsToValidate = new List<string>();
            allEmailsToValidate.AddRange(newSettings.AutoRunDailyStandardRecipientsTo ?? Enumerable.Empty<string>());
            allEmailsToValidate.AddRange(newSettings.AutoRunDailyStandardRecipientsCC ?? Enumerable.Empty<string>());
            allEmailsToValidate.AddRange(newSettings.AutoRunDaily5Day1kRecipientsTo ?? Enumerable.Empty<string>());
            allEmailsToValidate.AddRange(newSettings.AutoRunDaily5Day1kRecipientsCC ?? Enumerable.Empty<string>());
            allEmailsToValidate.AddRange(newSettings.AutoRunWeeklyRecipientsTo ?? Enumerable.Empty<string>());
            allEmailsToValidate.AddRange(newSettings.AutoRunWeeklyRecipientsCC ?? Enumerable.Empty<string>());
            allEmailsToValidate.AddRange(newSettings.AutoRunFemiOnlyRecipientsTo ?? Enumerable.Empty<string>());
            allEmailsToValidate.AddRange(newSettings.AutoRunFemiOnlyRecipientsCC ?? Enumerable.Empty<string>());
            // ADDED: Include new lists in validation.
            allEmailsToValidate.AddRange(newSettings.AutoRunNewCustomerRecipientsTo ?? Enumerable.Empty<string>());
            allEmailsToValidate.AddRange(newSettings.AutoRunNewCustomerRecipientsCC ?? Enumerable.Empty<string>());

            allEmailsToValidate.AddRange(newSettings.ProdManualRunDailyTo ?? Enumerable.Empty<string>());
            allEmailsToValidate.AddRange(newSettings.ProdManualRunDailyCC ?? Enumerable.Empty<string>());
            allEmailsToValidate.AddRange(newSettings.ProdFemiTo ?? Enumerable.Empty<string>());
            allEmailsToValidate.AddRange(newSettings.ProdFemiCC ?? Enumerable.Empty<string>());
            allEmailsToValidate.AddRange(newSettings.ProdTeamTo ?? Enumerable.Empty<string>());
            allEmailsToValidate.AddRange(newSettings.ProdTeamCC ?? Enumerable.Empty<string>());
            allEmailsToValidate.AddRange(newSettings.ProdManualCustomTo ?? Enumerable.Empty<string>());
            allEmailsToValidate.AddRange(newSettings.ProdManualCustomCC ?? Enumerable.Empty<string>());
            // ADDED: Include new lists in validation.
            allEmailsToValidate.AddRange(newSettings.ManualNewCustomerTo ?? Enumerable.Empty<string>());
            allEmailsToValidate.AddRange(newSettings.ManualNewCustomerCC ?? Enumerable.Empty<string>());

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
            #endregion
        }

        /// <summary>
        /// Handles the Click event for the "Restore Defaults" button.
        /// </summary>
        private void BtnRestoreDefaults_Click(object sender, EventArgs e)
        {
            // This method's logic is unchanged.
            #region Original Method Content
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
            #endregion
        }

        /// <summary>
        /// Handles the Click event for the "Close" button.
        /// </summary>
        private void BtnClose_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
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
            // This method's logic is unchanged.
            #region Original Method Content
            if (string.IsNullOrWhiteSpace(emailString))
            {
                return new List<string>();
            }
            return emailString.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                              .Select(email => email.Trim())
                              .Where(email => !string.IsNullOrWhiteSpace(email))
                              .ToList();
            #endregion
        }
        #endregion
    }
}