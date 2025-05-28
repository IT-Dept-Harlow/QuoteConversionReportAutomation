
#region Using Directives
// System related namespaces
using System;
using System.Drawing;
using System.Windows.Forms;

// Project specific namespaces
using QuoteConversionReportAutomation.Helpers; // For FlexibleMessageBox
using QuoteConversionReportAutomation.Managers; // For GreetingManager and UIManager
using QuoteConversionReportAutomation.Models;   // For UserGreetingSettings
using QuoteConversionReportAutomation.Services.Logging; // For Logger
#endregion

namespace QuoteConversionReportAutomation
{
    /// <summary>
    /// A Windows Form that allows users to view and modify email greeting messages
    /// for different report generation contexts. User-defined greetings are saved
    /// and will override any application defaults.
    /// </summary>
    public partial class ManageGreetingsForm : Form
    {
        #region Fields and Constants

        private readonly GreetingManager _greetingManager; // Service for loading/saving greeting settings.
        private readonly bool _isDarkMode; // Flag indicating if dark mode is active, passed from parent.

        // --- Theme Colours ---
        // Define colours for dark and light modes for UI theming.
        // Dark Mode Colours
        private static readonly Color DM_ControlBackColor = Color.FromArgb(60, 60, 63);      // Background for input controls.
        private static readonly Color DM_ButtonBackColor = Color.FromArgb(80, 80, 80);       // Background for buttons.
        private static readonly Color DM_ControlForeColor = Color.White;                      // Text colour for controls.
        // Light Mode Colours
        private static readonly Color LM_ControlBackColor = SystemColors.Window;            // Standard window background.
        private static readonly Color LM_ButtonBackColor = SystemColors.Control;            // Standard control background.
        private static readonly Color LM_ControlForeColor = SystemColors.ControlText;       // Standard control text colour.

        // --- UI Control Field for Manual Custom Greeting ---
        // This field holds a reference to the dynamically added or designer-placed TextBox
        // for managing the greeting for manually run "Custom" type reports.
        private TextBox txtManualCustom;

        #endregion

        #region Constructor
        /// <summary>
        /// Initialises a new instance of the <see cref="ManageGreetingsForm"/> class.
        /// </summary>
        /// <param name="greetingManager">The manager responsible for greeting settings logic.</param>
        /// <param name="isDarkMode">A flag indicating whether dark mode should be applied to the form.</param>
        public ManageGreetingsForm(GreetingManager greetingManager, bool isDarkMode)
        {
            _greetingManager = greetingManager ?? throw new ArgumentNullException(nameof(greetingManager));
            _isDarkMode = isDarkMode;

            InitializeComponent(); // Standard WinForms method from ManageGreetingsForm.Designer.cs.

            // Attempt to find or dynamically create the control for the "Manual Custom" greeting.
            InitializeManualCustomGreetingControl();

            // Configure basic form properties.
            this.ShowIcon = false; // Do not show an icon in the title bar.
            this.StartPosition = FormStartPosition.CenterParent; // Centre the form relative to its parent.
            this.Text = "Manage Email Greetings"; // Set the window title.
        }
        #endregion

        #region Form Load and Theming
        /// <summary>
        /// Handles the Load event of the form. This is called once when the form is first displayed.
        /// It applies the visual theme, loads current greeting settings into the UI, and sets up tooltips.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">An <see cref="EventArgs"/> that contains no event data.</param>
        private void ManageGreetingsForm_Load(object sender, EventArgs e)
        {
            Logger.LogInfo($"ManageGreetingsForm loading. Initial DarkMode state: {_isDarkMode}");
            // Apply the theme to the form itself (title bar, main background).
            UIManager.ApplyThemeToExternalForm(this, _isDarkMode);
            // Apply the theme to child controls within this form.
            ApplyChildControlTheme(_isDarkMode);
            // Load current greeting settings into the TextBoxes.
            LoadGreetingsToForm();
            // Set up informational tooltips.
            SetupToolTips();

#if !DEBUG
            // In Release mode, hide the debug-specific greeting field.
            HideDebugGreetingField();
#endif
            Logger.LogInfo("ManageGreetingsForm loaded and themed.");
        }

        /// <summary>
        /// Hides the UI elements related to the debug greeting when the application is not in DEBUG mode.
        /// </summary>
        private void HideDebugGreetingField()
        {
            Logger.LogInfo("Release mode: Hiding debug greeting field.");
            // Hide the label and textbox for the debug greeting.
            if (lblDebugDefault != null) lblDebugDefault.Visible = false;
            if (txtDebugDefault != null) txtDebugDefault.Visible = false;

            // If these controls were in a specific row of a TableLayoutPanel,
            // that row might need to be adjusted to collapse its height.
            // For this example, simply hiding the controls is the primary action.
            // Assuming mainTableLayoutPanel is the container.
            // Row 6 (0-indexed) is assumed for the debug greeting based on typical designer layout.
            if (mainTableLayoutPanel.RowCount > 6)
            {
                // To truly collapse the row, its RowStyle SizeType would need to be Absolute and Height set to 0,
                // or percentages of other rows adjusted. This is complex if other rows are also Percentage based.
                // For now, hiding controls is sufficient for visual effect.
                Logger.LogDebug("Debug greeting controls hidden. Row collapsing in TableLayoutPanel is not explicitly handled here beyond control visibility.");
            }
        }

        /// <summary>
        /// Applies the current theme (dark or light) specifically to the child controls of this form.
        /// </summary>
        /// <param name="isDarkModeEnabled">True if dark mode is enabled, false otherwise.</param>
        private void ApplyChildControlTheme(bool isDarkModeEnabled)
        {
            // Determine appropriate colours based on the theme.
            Color controlBackColor = isDarkModeEnabled ? DM_ControlBackColor : LM_ControlBackColor;
            Color buttonBackColor = isDarkModeEnabled ? DM_ButtonBackColor : LM_ButtonBackColor;
            Color controlForeColor = isDarkModeEnabled ? DM_ControlForeColor : LM_ControlForeColor;
            // Recursively apply these colours to all controls on the form.
            UpdateControlThemeRecursive(this, controlBackColor, buttonBackColor, controlForeColor, isDarkModeEnabled);
        }

        /// <summary>
        /// Recursive helper method to apply theme colours to a control and all its child controls.
        /// </summary>
        /// <param name="parentControl">The control to start theming from.</param>
        /// <param name="controlBackColor">The background colour for input-type controls.</param>
        /// <param name="buttonBackColor">The background colour for buttons.</param>
        /// <param name="controlForeColor">The general foreground (text) colour.</param>
        /// <param name="isDarkMode">A flag indicating if dark mode is currently being applied.</param>
        private void UpdateControlThemeRecursive(Control parentControl, Color controlBackColor, Color buttonBackColor, Color controlForeColor, bool isDarkMode)
        {
            foreach (Control control in parentControl.Controls)
            {
                if (control.IsDisposed) continue; // Skip disposed controls.

                // Apply theme based on control type.
                if (control is Button button)
                {
                    button.BackColor = buttonBackColor;
                    button.ForeColor = controlForeColor;
                    button.FlatStyle = FlatStyle.Flat;
                    button.FlatAppearance.BorderColor = isDarkMode ? Color.FromArgb(100, 100, 100) : SystemColors.ControlDarkDark;
                    button.FlatAppearance.BorderSize = 1;
                }
                else if (control is TextBox) // Handles TextBox controls.
                {
                    control.BackColor = controlBackColor;
                    control.ForeColor = controlForeColor;
                    ((TextBox)control).BorderStyle = isDarkMode ? BorderStyle.FixedSingle : BorderStyle.Fixed3D;
                }
                else if (control is Label)
                {
                    control.BackColor = Color.Transparent; // Labels should typically be transparent.
                    control.ForeColor = controlForeColor;
                }
                else if (control is GroupBox gb) // GroupBoxes are containers.
                {
                    gb.ForeColor = controlForeColor; // Text colour for the GroupBox title.
                    gb.BackColor = parentControl.BackColor; // Match parent's background.
                    UpdateControlThemeRecursive(gb, controlBackColor, buttonBackColor, controlForeColor, isDarkMode); // Recurse.
                }
                else if (control is Panel || control is TableLayoutPanel) // Other common containers.
                {
                    control.BackColor = parentControl.BackColor; // Match parent's background.
                    control.ForeColor = controlForeColor;
                    UpdateControlThemeRecursive(control, controlBackColor, buttonBackColor, controlForeColor, isDarkMode); // Recurse.
                }
                else // For other simple controls not explicitly handled.
                {
                    if (control.Visible) // Only theme visible controls.
                    {
                        control.BackColor = controlBackColor;
                        control.ForeColor = controlForeColor;
                    }
                }
            }
        }
        #endregion

        #region UI Initialisation and Data Loading
        /// <summary>
        /// Initialises or finds the control for the "Manual Custom" greeting.
        /// If `txtManualCustom` is not found (e.g., not added via the WinForms designer),
        /// this method creates it programmatically along with its label and adds them
        /// to the `mainTableLayoutPanel`.
        /// </summary>
        private void InitializeManualCustomGreetingControl()
        {
            // Attempt to find the TextBox by name, assuming it might have been added in the designer.
            Control[] foundControls = this.Controls.Find("txtManualCustom", true);
            if (foundControls.Length > 0 && foundControls[0] is TextBox)
            {
                txtManualCustom = (TextBox)foundControls[0];
                Logger.LogDebug("Manual Custom greeting TextBox found by name (likely from designer).");
            }
            else // If the TextBox was not found, create it programmatically.
            {
                Logger.LogDebug("Manual Custom greeting TextBox not found by name. Creating it programmatically.");

                // Create the Label for the "Manual Custom" greeting.
                Label lblManualCustom = new Label
                {
                    Text = "Manual Custom Greeting:",
                    Anchor = AnchorStyles.Right | AnchorStyles.Top, // Align like other labels.
                    AutoSize = true,
                    Margin = new Padding(3, 6, 3, 3) // Consistent margin.
                };

                // Create the TextBox for the "Manual Custom" greeting.
                txtManualCustom = new TextBox
                {
                    Name = "txtManualCustom",
                    Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top, // Fill width.
                    Height = 20 // Standard height.
                };

                // Add a new row to the TableLayoutPanel for these controls.
                // This assumes mainTableLayoutPanel is correctly initialised from the designer.
                if (mainTableLayoutPanel != null)
                {
                    int newRowIndex = mainTableLayoutPanel.RowCount;
                    mainTableLayoutPanel.RowCount = newRowIndex + 1;
                    // Define the style for the new row (e.g., percentage-based height).
                    // This should match the style of other data rows for consistency.
                    mainTableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F / (mainTableLayoutPanel.RowCount - 1))); // Adjust percentage based on new total rows (excluding header)

                    // Add the new label and textbox to the TableLayoutPanel.
                    mainTableLayoutPanel.Controls.Add(lblManualCustom, 0, newRowIndex);
                    mainTableLayoutPanel.Controls.Add(txtManualCustom, 1, newRowIndex);
                    Logger.LogInfo("Programmatically added UI elements for Manual Custom greeting.");
                }
                else
                {
                    Logger.LogError("mainTableLayoutPanel is null. Cannot add Manual Custom greeting controls programmatically.");
                }
            }
        }

        /// <summary>
        /// Loads the current effective greeting settings (merged from defaults and user overrides)
        /// into the corresponding TextBox controls on the form.
        /// </summary>
        private void LoadGreetingsToForm()
        {
            UserGreetingSettings effectiveGreetings = _greetingManager.GetCurrentEffectiveGreetings();

            // Populate TextBoxes with greetings for standard scenarios.
            txtAutoRunDaily.Text = effectiveGreetings.AutoRunDaily;
            txtManualStdDaily.Text = effectiveGreetings.ManualStdDaily;
            txtAutoRunDaily5Day1k.Text = effectiveGreetings.AutoRunDaily5Day1k;
            txtManualFemi.Text = effectiveGreetings.ManualFemi;
            txtManualTeam.Text = effectiveGreetings.ManualTeam;

            // Populate the "Manual Custom" greeting TextBox (check for null if dynamically added).
            if (txtManualCustom != null)
            {
                txtManualCustom.Text = effectiveGreetings.ManualCustom;
            }

#if DEBUG
            // Populate the debug greeting TextBox (only compiled in Debug mode).
            if (txtDebugDefault != null) 
            {
                txtDebugDefault.Text = effectiveGreetings.DebugDefault;
            }
#endif
            Logger.LogInfo("Loaded current greetings into ManageGreetingsForm.");
        }

        /// <summary>
        /// Sets up informational tooltips for the various greeting TextBox controls and action buttons.
        /// </summary>
        private void SetupToolTips()
        {
            // Ensure the ToolTipProvider component is initialised.
            if (this.toolTipProvider == null)
            {
                this.toolTipProvider = new ToolTip(this.components ?? (this.components = new System.ComponentModel.Container()));
            }
            // Set tooltips for each greeting field.
            toolTipProvider.SetToolTip(txtAutoRunDaily, "Greeting for automated standard daily reports.");
            toolTipProvider.SetToolTip(txtManualStdDaily, "Greeting for manually run standard daily reports.");
            toolTipProvider.SetToolTip(txtAutoRunDaily5Day1k, "Greeting for automated 'Daily (5days >= £1k)' reports.");
            toolTipProvider.SetToolTip(txtManualFemi, "Greeting for manual non-daily reports when 'Femi Only' is selected.");
            toolTipProvider.SetToolTip(txtManualTeam, "Greeting for manual non-daily reports for the general team.");

            // Tooltip for "Manual Custom" greeting (check for null if dynamically added).
            if (txtManualCustom != null)
            {
                toolTipProvider.SetToolTip(txtManualCustom, "Greeting for manually run 'Custom' type reports.");
            }

#if DEBUG
            // Tooltip for debug greeting (only compiled in Debug mode).
            if (txtDebugDefault != null) toolTipProvider.SetToolTip(txtDebugDefault, "Default greeting for all reports in DEBUG mode.");
#endif
            // Tooltips for action buttons.
            toolTipProvider.SetToolTip(btnSave, "Save your custom greetings. They will override app defaults.");
            toolTipProvider.SetToolTip(btnRestoreDefaults, "Remove all custom greetings and revert to those defined in appsettings.json.");
            toolTipProvider.SetToolTip(btnClose, "Close this window without saving current changes.");
        }
        #endregion

        #region Button Event Handlers
        /// <summary>
        /// Handles the Click event for the "Save" button.
        /// Gathers the greeting texts from the form, creates a <see cref="UserGreetingSettings"/> object,
        /// and saves it using the <see cref="GreetingManager"/>.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">An <see cref="EventArgs"/> that contains no event data.</param>
        private void BtnSave_Click(object sender, EventArgs e)
        {
            Logger.LogInfo("Save button clicked on ManageGreetingsForm.");
            // Create a new UserGreetingSettings object from the current form values.
            // Use null if a TextBox is empty, so the GreetingManager can fall back to defaults.
            var newOverrides = new UserGreetingSettings
            {
                AutoRunDaily = string.IsNullOrWhiteSpace(txtAutoRunDaily.Text) ? null : txtAutoRunDaily.Text.Trim(),
                ManualStdDaily = string.IsNullOrWhiteSpace(txtManualStdDaily.Text) ? null : txtManualStdDaily.Text.Trim(),
                AutoRunDaily5Day1k = string.IsNullOrWhiteSpace(txtAutoRunDaily5Day1k.Text) ? null : txtAutoRunDaily5Day1k.Text.Trim(),
                ManualFemi = string.IsNullOrWhiteSpace(txtManualFemi.Text) ? null : txtManualFemi.Text.Trim(),
                ManualTeam = string.IsNullOrWhiteSpace(txtManualTeam.Text) ? null : txtManualTeam.Text.Trim(),
                // Get value for "Manual Custom" greeting (check for null if dynamically added).
                ManualCustom = (txtManualCustom != null && !string.IsNullOrWhiteSpace(txtManualCustom.Text)) ? txtManualCustom.Text.Trim() : null
            };

#if DEBUG
            // Handle debug greeting (only compiled in Debug mode).
            if (txtDebugDefault != null)
            {
                newOverrides.DebugDefault = string.IsNullOrWhiteSpace(txtDebugDefault.Text) ? null : txtDebugDefault.Text.Trim();
            }
#else
            // In Release mode, preserve the existing debug override to prevent accidental clearing.
            // This requires getting the current user-specific override for DebugDefault if one exists.
            // For simplicity here, we'll assume if not in DEBUG, the DebugDefault from effective settings (which could be app default) is preserved.
            // A more robust way would be for GreetingManager to expose a method to get *only* user overrides.
            newOverrides.DebugDefault = _greetingManager.GetCurrentEffectiveGreetings().DebugDefault;
#endif

            // Confirm with the user before saving.
            DialogResult confirmSaveResult = FlexibleMessageBox.Show(this, "Do you want to save these email greetings?\nEmpty fields will revert to application defaults.",
                "Confirm Save Greetings", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (confirmSaveResult == DialogResult.Yes)
            {
                try
                {
                    // Save the new overrides using the GreetingManager.
                    _greetingManager.SaveUserGreetingOverrides(newOverrides);
                    Logger.LogInfo("User confirmed and email greetings saved successfully.");
                    FlexibleMessageBox.Show(this, "Email greeting settings have been saved.",
                        "Settings Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    DialogResult = DialogResult.OK; // Set DialogResult for the calling form.
                    Close(); // Close the form.
                }
                catch (Exception ex) // Handle potential errors during saving.
                {
                    Logger.LogError($"Failed to save email greeting settings: {ex.Message}", ex);
                    FlexibleMessageBox.Show(this, $"An error occurred while saving the greeting settings:\n\n{ex.Message}",
                        "Save Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        /// <summary>
        /// Handles the Click event for the "Restore Defaults" button.
        /// Clears all user-defined greeting overrides, causing the application to revert to
        /// the default greetings specified in `appsettings.json`.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">An <see cref="EventArgs"/> that contains no event data.</param>
        private void BtnRestoreDefaults_Click(object sender, EventArgs e)
        {
            Logger.LogInfo("Restore Defaults button clicked on ManageGreetingsForm.");
            // Confirm with the user before clearing their custom settings.
            DialogResult confirmRestoreResult = FlexibleMessageBox.Show(this, "Are you sure you want to restore all greetings to application defaults?\nThis will remove any custom greetings you have saved.",
                "Confirm Restore Defaults", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

            if (confirmRestoreResult == DialogResult.Yes)
            {
                try
                {
                    // Clear user overrides via the GreetingManager.
                    _greetingManager.ClearUserGreetingOverrides();
                    // Reload the form fields to reflect the restored default settings.
                    LoadGreetingsToForm();
                    Logger.LogInfo("User confirmed and email greetings restored to defaults.");
                    FlexibleMessageBox.Show(this, "Email greeting settings have been restored to application defaults.",
                        "Defaults Restored", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex) // Handle potential errors during the restore process.
                {
                    Logger.LogError($"Failed to restore default email greeting settings: {ex.Message}", ex);
                    FlexibleMessageBox.Show(this, $"An error occurred while restoring default settings:\n\n{ex.Message}",
                        "Restore Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        /// <summary>
        /// Handles the Click event for the "Close" button.
        /// Closes the form without saving any pending changes.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">An <see cref="EventArgs"/> that contains no event data.</param>
        private void BtnClose_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel; // Set DialogResult to Cancel.
            Close(); // Close the form.
        }
        #endregion
    }
}
