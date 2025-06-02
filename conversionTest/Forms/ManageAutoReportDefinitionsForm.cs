// ManageAutoReportDefinitionsForm.cs
#region Using Directives
using QuoteConversionReportAutomation.Models;
using QuoteConversionReportAutomation.Managers;
using QuoteConversionReportAutomation.Helpers;
using QuoteConversionReportAutomation.Services.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Microsoft.Extensions.Configuration; // Required for IConfiguration
using System.IO;
#endregion

namespace QuoteConversionReportAutomation.Forms
{
    /// <summary>
    /// Provides a user interface for managing automated report definitions for the QCRA application.
    /// Users can view, add, edit, and delete report definitions. These definitions determine
    /// how automated reports are generated, processed, and distributed.
    /// Changes are persisted in a dedicated JSON file (`autoReportDefinitions.json`).
    /// This form also allows users to enable or disable individual automated reports.
    /// </summary>
    public partial class ManageAutoReportDefinitionsForm : Form
    {
        #region Fields
        /// <summary>
        /// Provides access to the application's configuration settings (e.g., from appsettings.json),
        /// though primarily used here to pass to other components if needed or for future expansion.
        /// Report definitions themselves are loaded from a dedicated file.
        /// </summary>
        private readonly IConfiguration _configuration;

        /// <summary>
        /// The full path to the main `appsettings.json` file. Used to determine the directory
        /// where the `autoReportDefinitions.json` file is located.
        /// </summary>
        private readonly string _appSettingsPath;

        /// <summary>
        /// The full path to the dedicated `autoReportDefinitions.json` file, which stores the list
        /// of <see cref="AutoReportDefinition"/> objects.
        /// </summary>
        private readonly string _definitionsFilePath;

        /// <summary>
        /// The primary list of <see cref="AutoReportDefinition"/> objects currently loaded and being managed by the form.
        /// </summary>
        private List<AutoReportDefinition> _reportDefinitions;

        /// <summary>
        /// A <see cref="BindingList{T}"/> that wraps `_reportDefinitions`. This list is bound to the
        /// DataGridView, allowing UI updates to reflect changes in the list and vice-versa.
        /// </summary>
        private BindingList<AutoReportDefinition> _bindingList;

        /// <summary>
        /// Flag indicating whether the dark mode theme is currently active, passed from the parent form.
        /// </summary>
        private readonly bool _isDarkMode;

        /// <summary>
        /// Holds a reference to the <see cref="AutoReportDefinition"/> object that is currently
        /// selected in the DataGridView and whose details are populated in the input fields for editing or viewing.
        /// </summary>
        private AutoReportDefinition? _selectedDefinitionInFields;

        /// <summary>
        /// A flag to track whether there are any unsaved changes made by the user
        /// (e.g., adding, editing, deleting definitions, or toggling their enabled status).
        /// Used to prompt the user to save before closing the form.
        /// </summary>
        private bool _hasUnsavedChanges = false;
        #endregion

        #region Constructor
        /// <summary>
        /// Initializes a new instance of the <see cref="ManageAutoReportDefinitionsForm"/> class.
        /// Sets up dependencies, determines file paths, loads initial report definitions,
        /// and configures UI components.
        /// </summary>
        /// <param name="configuration">The application's main configuration settings.</param>
        /// <param name="appSettingsPath">The full path to the main `appsettings.json` file. This is used to locate the directory for `autoReportDefinitions.json`.</param>
        /// <param name="isDarkMode">A flag indicating whether dark mode should be applied to the form's visual theme.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="configuration"/> or <paramref name="appSettingsPath"/> is null.</exception>
        /// <exception cref="DirectoryNotFoundException">Thrown if the directory containing `appSettingsPath` cannot be determined, which prevents locating the `autoReportDefinitions.json` file.</exception>
        public ManageAutoReportDefinitionsForm(IConfiguration configuration, string appSettingsPath, bool isDarkMode)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _appSettingsPath = appSettingsPath ?? throw new ArgumentNullException(nameof(appSettingsPath));
            _isDarkMode = isDarkMode;

            // Determine the path for the dedicated report definitions file.
            // It's expected to be in the same directory as the main appsettings.json.
            string? appSettingsDir = Path.GetDirectoryName(_appSettingsPath);
            if (string.IsNullOrEmpty(appSettingsDir))
            {
                string errorMsg = $"Could not determine directory from appSettingsPath: '{_appSettingsPath}'. Cannot locate report definitions file.";
                Logger.LogCritical(errorMsg);
                throw new DirectoryNotFoundException(errorMsg);
            }
            _definitionsFilePath = Path.Combine(appSettingsDir, "autoReportDefinitions.json"); // Assuming "autoReportDefinitions.json" is the agreed-upon name.
            Logger.LogInfo($"ManageAutoReportDefinitionsForm: Report definitions will be loaded from/saved to: '{_definitionsFilePath}'");

            // Standard Windows Forms designer initialization.
            InitializeComponent();
            // Populate ComboBoxes with their static choice lists (e.g., ReportTypeIndex, DayOfWeek).
            PopulateComboBoxes();
            // Load existing report definitions from the dedicated JSON file and initialize the BindingList.
            LoadReportDefinitionsAndInitializeBindingList();

            // Configure the DataGridView columns, data binding, and event handlers.
            SetupDataGridView();

            // Subscribe to form events.
            this.Load += ManageAutoReportDefinitionsForm_Load;
            this.FormClosing += ManageAutoReportDefinitionsForm_FormClosing;
        }
        #endregion

        #region Form Load and Theming
        /// <summary>
        /// Handles the Load event of the form. This method is called once when the form is first displayed.
        /// It applies the visual theme (dark/light mode), initializes the state of input fields,
        /// and sets the initial state for UI elements like buttons.
        /// </summary>
        /// <param name="sender">The source of the event (the form itself).</param>
        /// <param name="e">An <see cref="EventArgs"/> that contains no event data.</param>
        private void ManageAutoReportDefinitionsForm_Load(object? sender, EventArgs e)
        {
            Logger.LogInfo($"ManageAutoReportDefinitionsForm loading. DarkMode: {_isDarkMode}");
            // Apply the theme to the form itself (title bar, main background/foreground).
            UIManager.ApplyThemeToExternalForm(this, _isDarkMode);
            // Apply the theme to all child controls within this form.
            ApplyChildControlTheme(_isDarkMode);
            // Set input fields to a default empty/initial state.
            ClearInputFields();
            // Ensure no row in the DataGridView is initially selected.
            dgvReportDefinitions.ClearSelection();
            // Disable Delete and Update buttons as no item is selected initially.
            btnDelete.Enabled = false;
            btnUpdate.Enabled = false;
            // Initialize the unsaved changes flag to false.
            SetHasUnsavedChanges(false);
            Logger.LogInfo("ManageAutoReportDefinitionsForm loaded and themed.");
        }

        /// <summary>
        /// Applies the current theme (dark or light mode) specifically to the child controls of this form.
        /// This method iterates through controls and customizes their appearance (BackColor, ForeColor, etc.)
        /// based on the active theme.
        /// </summary>
        /// <param name="isDarkModeEnabled">True if dark mode is enabled; otherwise, false for light mode.</param>
        private void ApplyChildControlTheme(bool isDarkModeEnabled)
        {
            // Define color palettes for dark and light modes for various control types.
            Color formBackColor = isDarkModeEnabled ? Color.FromArgb(45, 45, 48) : SystemColors.Control;
            Color controlBackColor = isDarkModeEnabled ? Color.FromArgb(60, 60, 63) : SystemColors.Window;
            Color buttonBackColor = isDarkModeEnabled ? Color.FromArgb(80, 80, 80) : SystemColors.Control;
            Color controlForeColor = isDarkModeEnabled ? Color.WhiteSmoke : SystemColors.ControlText;
            Color labelForeColor = isDarkModeEnabled ? Color.FromArgb(200, 200, 200) : SystemColors.ControlText;

            // Recursive action to apply themes to a control and all its children.
            Action<Control> themeAction = null!;
            themeAction = (parentControl) =>
            {
                foreach (Control control in parentControl.Controls)
                {
                    if (control.IsDisposed) continue; // Skip already disposed controls.

                    // Apply theme based on the type of the control.
                    if (control is Button button)
                    {
                        button.BackColor = buttonBackColor;
                        button.ForeColor = controlForeColor;
                        button.FlatStyle = FlatStyle.Flat; // Use flat style for better custom theme appearance.
                        button.FlatAppearance.BorderColor = isDarkModeEnabled ? Color.FromArgb(100, 100, 100) : SystemColors.ControlDarkDark;
                        button.FlatAppearance.BorderSize = 1;
                    }
                    else if (control is TextBox || control is RichTextBox || control is NumericUpDown)
                    {
                        control.BackColor = controlBackColor;
                        control.ForeColor = controlForeColor;
                        if (control is TextBox tb) tb.BorderStyle = isDarkModeEnabled ? BorderStyle.FixedSingle : BorderStyle.Fixed3D;
                    }
                    else if (control is ComboBox cb)
                    {
                        cb.BackColor = controlBackColor;
                        cb.ForeColor = controlForeColor;
                        cb.FlatStyle = FlatStyle.Flat; // Flat style for ComboBox.
                    }
                    else if (control is CheckBox chk)
                    {
                        chk.BackColor = Color.Transparent; // Checkboxes often look better with transparent backgrounds.
                        chk.ForeColor = labelForeColor;
                    }
                    else if (control is Label lbl)
                    {
                        lbl.BackColor = Color.Transparent; // Labels should have transparent backgrounds.
                        lbl.ForeColor = labelForeColor;
                    }
                    else if (control is DataGridView dgv)
                    {
                        // Apply detailed theming to the DataGridView.
                        dgv.EnableHeadersVisualStyles = false; // Required for custom header styling.
                        dgv.ColumnHeadersDefaultCellStyle.BackColor = isDarkModeEnabled ? Color.FromArgb(55, 55, 58) : SystemColors.Control;
                        dgv.ColumnHeadersDefaultCellStyle.ForeColor = controlForeColor;
                        dgv.ColumnHeadersDefaultCellStyle.SelectionBackColor = dgv.ColumnHeadersDefaultCellStyle.BackColor; // Prevent selection highlight on header.
                        dgv.RowHeadersDefaultCellStyle.BackColor = isDarkModeEnabled ? Color.FromArgb(55, 55, 58) : SystemColors.Control;

                        dgv.DefaultCellStyle.BackColor = controlBackColor; // Background for cells.
                        dgv.DefaultCellStyle.ForeColor = controlForeColor; // Text color for cells.
                        dgv.DefaultCellStyle.SelectionBackColor = isDarkModeEnabled ? Color.FromArgb(0, 120, 215) : SystemColors.Highlight; // Selection background.
                        dgv.DefaultCellStyle.SelectionForeColor = isDarkModeEnabled ? Color.White : SystemColors.HighlightText; // Selection text color.

                        dgv.BackgroundColor = formBackColor; // Grid background to match form.
                        dgv.GridColor = isDarkModeEnabled ? Color.FromArgb(80, 80, 80) : SystemColors.ControlDark; // Grid line color.
                    }
                    else if (control is GroupBox gb)
                    {
                        gb.ForeColor = labelForeColor; // GroupBox title text color.
                        gb.BackColor = formBackColor;  // GroupBox background matches form.
                        themeAction(gb);               // Recursively theme controls within the GroupBox.
                    }
                    else if (control is Panel pnl) // Handle Panels (like FlowLayoutPanel for buttons)
                    {
                        pnl.BackColor = formBackColor; // Panel background matches form.
                        themeAction(pnl);              // Recursively theme controls within the Panel.
                    }
                    else if (control.HasChildren) // For other container controls.
                    {
                        themeAction(control);
                    }
                }
            };
            themeAction(this); // Start theming from the form itself.
        }
        #endregion

        #region DataGridView Setup and Handling
        /// <summary>
        /// Configures the DataGridView columns, data binding, properties, and event handlers.
        /// This method defines how <see cref="AutoReportDefinition"/> objects are displayed and interacted with in the grid.
        /// </summary>
        private void SetupDataGridView()
        {
            dgvReportDefinitions.AutoGenerateColumns = false; // We define columns manually for better control.
            dgvReportDefinitions.Columns.Clear();             // Clear any columns added by the designer or previous setups.

            // Define and add the "IsEnabled" checkbox column.
            var enabledCol = new DataGridViewCheckBoxColumn
            {
                Name = "colIsEnabled",
                HeaderText = "Enabled",
                DataPropertyName = "IsEnabled", // Binds this column to the IsEnabled property of AutoReportDefinition.
                AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells // Adjust column width based on content and header.
            };
            dgvReportDefinitions.Columns.Add(enabledCol);

            // Define and add the "ReportName" text column.
            var nameCol = new DataGridViewTextBoxColumn
            {
                Name = "colReportName",
                HeaderText = "Report Name",
                DataPropertyName = "ReportName", // Binds to the ReportName property.
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, // Allows this column to take up remaining available width.
                MinimumWidth = 150 // Ensure a reasonable minimum width for readability.
            };
            dgvReportDefinitions.Columns.Add(nameCol);

            // Define and add the "ReportTypeIndex" text column.
            var typeIndexCol = new DataGridViewTextBoxColumn
            {
                Name = "colReportTypeIndex",
                HeaderText = "Type Index",
                DataPropertyName = "ReportTypeIndex", // Binds to the ReportTypeIndex property.
                AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells
            };
            dgvReportDefinitions.Columns.Add(typeIndexCol);

            // Define and add the "RunOnDayOfWeek" text column.
            // This column will be formatted by DgvReportDefinitions_CellFormatting to display the day name.
            var dayOfWeekCol = new DataGridViewTextBoxColumn
            {
                Name = "colRunOnDayOfWeek",
                HeaderText = "Run Day",
                DataPropertyName = "RunOnDayOfWeek", // Binds to the RunOnDayOfWeek property.
                AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells
            };
            dgvReportDefinitions.Columns.Add(dayOfWeekCol);

            // Set the DataSource for the DataGridView to the BindingList.
            // Changes to the BindingList will reflect in the DGV, and user edits in the DGV (like checkbox)
            // can update the BindingList if configured correctly (e.g., via CommitEdit).
            dgvReportDefinitions.DataSource = _bindingList;

            // Configure DataGridView behavior.
            dgvReportDefinitions.AllowUserToAddRows = false;    // Users add new definitions via the input fields, not directly in the grid.
            dgvReportDefinitions.AllowUserToDeleteRows = false; // Deletion is handled by a dedicated "Delete" button.
            dgvReportDefinitions.MultiSelect = false;           // Only allow selection of a single row at a time.
            dgvReportDefinitions.SelectionMode = DataGridViewSelectionMode.FullRowSelect; // Clicking a cell selects the entire row.

            // Subscribe to DataGridView events for custom handling.
            dgvReportDefinitions.CellFormatting += DgvReportDefinitions_CellFormatting;       // For custom display of data (e.g., enums).
            dgvReportDefinitions.SelectionChanged += DgvReportDefinitions_SelectionChanged;   // When the selected row changes.
            dgvReportDefinitions.CellValueChanged += DgvReportDefinitions_CellValueChanged;   // When a cell's value is changed and committed (e.g., IsEnabled checkbox).
            dgvReportDefinitions.CurrentCellDirtyStateChanged += DgvReportDefinitions_CurrentCellDirtyStateChanged; // To commit checkbox changes immediately.
        }

        /// <summary>
        /// Handles the CurrentCellDirtyStateChanged event of the DataGridView.
        /// This event is primarily used to immediately commit changes made in checkbox cells (like the "IsEnabled" column)
        /// without requiring the user to navigate away from the cell.
        /// </summary>
        /// <param name="sender">The source of the event (the DataGridView).</param>
        /// <param name="e">An <see cref="EventArgs"/> that contains no event data.</param>
        private void DgvReportDefinitions_CurrentCellDirtyStateChanged(object? sender, EventArgs e)
        {
            // If the currently edited cell is dirty (has uncommitted changes) and it's a checkbox cell,
            // commit the edit immediately. This makes the checkbox behave more intuitively.
            if (dgvReportDefinitions.IsCurrentCellDirty && dgvReportDefinitions.CurrentCell is DataGridViewCheckBoxCell)
            {
                dgvReportDefinitions.CommitEdit(DataGridViewDataErrorContexts.Commit);
            }
        }

        /// <summary>
        /// Handles the CellValueChanged event of the DataGridView.
        /// This event fires after a cell value has been changed and committed by the user.
        /// It's used here to detect changes to the "IsEnabled" status of a report definition
        /// and flag that there are unsaved changes.
        /// </summary>
        /// <param name="sender">The source of the event (the DataGridView).</param>
        /// <param name="e">A <see cref="DataGridViewCellEventArgs"/> that contains the event data (row and column index).</param>
        private void DgvReportDefinitions_CellValueChanged(object? sender, DataGridViewCellEventArgs e)
        {
            // Ensure the event is for a valid row and the "IsEnabled" column.
            if (e.RowIndex >= 0 && e.ColumnIndex == dgvReportDefinitions.Columns["colIsEnabled"]?.Index)
            {
                // Access the underlying AutoReportDefinition object from the BindingList.
                if (_bindingList != null && e.RowIndex < _bindingList.Count)
                {
                    Logger.LogInfo($"Auto-report '{_bindingList[e.RowIndex].ReportName}' IsEnabled changed to: {_bindingList[e.RowIndex].IsEnabled} in DataGridView.");
                    SetHasUnsavedChanges(true); // Mark that there are unsaved changes.
                }
            }
        }

        /// <summary>
        /// Handles the CellFormatting event of the DataGridView.
        /// This allows for custom formatting of cell values before they are displayed.
        /// Used here to display the string name of <see cref="DayOfWeek"/> enums instead of their underlying integer values.
        /// </summary>
        /// <param name="sender">The source of the event (the DataGridView).</param>
        /// <param name="e">A <see cref="DataGridViewCellFormattingEventArgs"/> that contains data for the event, including the cell value and its desired display format.</param>
        private void DgvReportDefinitions_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return; // Ignore header cells or invalid indices.

            // Format the "RunOnDayOfWeek" column to display the day name (e.g., "Friday") or empty if null.
            if (dgvReportDefinitions.Columns[e.ColumnIndex].Name == "colRunOnDayOfWeek")
            {
                if (e.Value is DayOfWeek day)
                {
                    e.Value = day.ToString();
                    e.FormattingApplied = true; // Indicate that formatting has been handled.
                }
                else if (e.Value == null)
                {
                    e.Value = string.Empty; // Display empty for null DayOfWeek.
                    e.FormattingApplied = true;
                }
            }
            // Add formatting for other columns (e.g., ReportTypeIndex to a more descriptive string) if desired.
        }

        /// <summary>
        /// Handles the SelectionChanged event of the DataGridView.
        /// When a row is selected, this method populates the input fields in the "Definition Details" group box
        /// with the properties of the selected <see cref="AutoReportDefinition"/>.
        /// It also manages the enabled state of the "Update" and "Delete" buttons.
        /// </summary>
        /// <param name="sender">The source of the event (the DataGridView).</param>
        /// <param name="e">An <see cref="EventArgs"/> that contains no event data.</param>
        private void DgvReportDefinitions_SelectionChanged(object? sender, EventArgs e)
        {
            // Check if any row is selected and if the DataBoundItem is a valid AutoReportDefinition.
            if (dgvReportDefinitions.SelectedRows.Count > 0 &&
                dgvReportDefinitions.SelectedRows[0].DataBoundItem is AutoReportDefinition selectedDef)
            {
                _selectedDefinitionInFields = selectedDef;  // Store the selected definition for editing.
                PopulateInputFields(_selectedDefinitionInFields); // Load its details into the input fields.
                btnDelete.Enabled = true;                   // Enable the Delete button.
                btnUpdate.Enabled = true;                   // Enable the Update button.
                btnAdd.Text = "New";                        // Change "Add" button text to "New", indicating it will clear fields for a new entry.
            }
            else // No row selected or invalid DataBoundItem.
            {
                _selectedDefinitionInFields = null; // No definition is selected for editing.
                // Optionally, clear input fields when selection is lost. Current behavior leaves last selected data.
                // ClearInputFields(); 
                btnDelete.Enabled = false;  // Disable Delete button.
                btnUpdate.Enabled = false;  // Disable Update button.
                btnAdd.Text = "Add";      // Reset "New" button text to "Add".
            }
        }
        #endregion

        #region Data Loading and Saving
        /// <summary>
        /// Loads the list of <see cref="AutoReportDefinition"/> objects from the dedicated JSON file
        /// (e.g., `autoReportDefinitions.json`) and initializes the internal `_reportDefinitions` list
        /// and the `_bindingList` used for data binding with the DataGridView.
        /// </summary>
        private void LoadReportDefinitionsAndInitializeBindingList()
        {
            try
            {
                // Call the static method from AutoRunManager to load definitions from the specified file.
                _reportDefinitions = AutoRunManager.LoadReportDefinitions(_definitionsFilePath);
                Logger.LogInfo($"Loaded {_reportDefinitions.Count} report definitions from '{_definitionsFilePath}'.");
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error loading report definitions from '{_definitionsFilePath}': {ex.Message}", ex);
                FlexibleMessageBox.Show(this, $"Could not load report definitions.\nError: {ex.Message}", "Load Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                _reportDefinitions = new List<AutoReportDefinition>(); // Initialize with an empty list on error to prevent null issues.
            }
            // Initialize the BindingList with the loaded (or empty) list of report definitions.
            // Ensure _reportDefinitions is never null before passing to BindingList constructor.
            _bindingList = new BindingList<AutoReportDefinition>(_reportDefinitions ?? new List<AutoReportDefinition>());
        }


        /// <summary>
        /// Saves the current list of <see cref="AutoReportDefinition"/> objects (from the DataGridView's binding list)
        /// back to the dedicated report definitions JSON file (e.g., `autoReportDefinitions.json`).
        /// This method is called when the user clicks the "Save All Changes" button or confirms saving on form close.
        /// </summary>
        private void SaveReportDefinitions()
        {
            Logger.LogInfo($"Attempting to save {_bindingList.Count} report definitions to {_definitionsFilePath}");
            try
            {
                // Call the static method from AutoRunManager to save the current list of definitions.
                AutoRunManager.SaveReportDefinitions(_definitionsFilePath, _bindingList.ToList());
                SetHasUnsavedChanges(false); // Reset the unsaved changes flag after successful save.
                Logger.LogInfo("Report definitions saved successfully to dedicated file.");
                FlexibleMessageBox.Show(this, "Report definitions saved successfully.", "Save Successful", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error saving report definitions to '{_definitionsFilePath}': {ex.Message}", ex);
                FlexibleMessageBox.Show(this, $"Could not save report definitions to '{_definitionsFilePath}'.\nPlease check file permissions and logs.\nError: {ex.Message}", "Save Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                // _hasUnsavedChanges remains true if save failed, prompting user again on close if they try.
            }
        }
        #endregion

        #region Input Field Management
        /// <summary>
        /// Populates the ComboBox controls (`cmbReportTypeIndex`, `cmbRunOnDayOfWeek`) with their static choice lists.
        /// This is typically called once during form initialization.
        /// </summary>
        private void PopulateComboBoxes()
        {
            // Populate ReportTypeIndex ComboBox with predefined report types and their corresponding integer values.
            cmbReportTypeIndex.Items.Clear();
            cmbReportTypeIndex.Items.Add(new { Text = "0 - Daily", Value = 0 });
            cmbReportTypeIndex.Items.Add(new { Text = "1 - Daily (5d >= £1k)", Value = 1 });
            cmbReportTypeIndex.Items.Add(new { Text = "2 - Weekly", Value = 2 });
            cmbReportTypeIndex.Items.Add(new { Text = "3 - Monthly", Value = 3 });
            cmbReportTypeIndex.Items.Add(new { Text = "4 - Quarterly", Value = 4 });
            cmbReportTypeIndex.Items.Add(new { Text = "5 - Annual", Value = 5 });
            // Add other report types here if they have specific indices and behaviors.
            cmbReportTypeIndex.DisplayMember = "Text"; // Property of the anonymous type to display.
            cmbReportTypeIndex.ValueMember = "Value";   // Property of the anonymous type for the underlying value.

            // Populate RunOnDayOfWeek ComboBox with days of the week and a "Not Specific" option.
            cmbRunOnDayOfWeek.Items.Clear();
            cmbRunOnDayOfWeek.Items.Add("Not Specific (Runs Daily if Enabled)"); // This string represents a null DayOfWeek.
            foreach (DayOfWeek day in Enum.GetValues(typeof(DayOfWeek))) // Add all DayOfWeek enum values.
            {
                cmbRunOnDayOfWeek.Items.Add(day);
            }
        }

        /// <summary>
        /// Populates the input fields in the "Definition Details" group box with the properties
        /// of the provided <see cref="AutoReportDefinition"/> object.
        /// If the provided definition is null, it calls <see cref="ClearInputFields"/> to reset the form.
        /// </summary>
        /// <param name="definition">The <see cref="AutoReportDefinition"/> object whose details are to be displayed.
        /// Can be null to clear the fields.</param>
        private void PopulateInputFields(AutoReportDefinition? definition)
        {
            if (definition == null)
            {
                ClearInputFields(); // If no definition is provided, clear all input fields.
                return;
            }

            // Populate basic text fields.
            txtReportName.Text = definition.ReportName;
            chkIsEnabled.Checked = definition.IsEnabled; // Set IsEnabled checkbox.

            // Select the correct item in the ReportTypeIndex ComboBox based on the definition's value.
            SelectComboBoxItemByValue(cmbReportTypeIndex, definition.ReportTypeIndex);

            // Select the correct item in the RunOnDayOfWeek ComboBox.
            if (definition.RunOnDayOfWeek.HasValue)
            {
                cmbRunOnDayOfWeek.SelectedItem = definition.RunOnDayOfWeek.Value; // Select the specific day.
            }
            else // If RunOnDayOfWeek is null, select the "Not Specific" option.
            {
                if (cmbRunOnDayOfWeek.Items.Count > 0)
                {
                    cmbRunOnDayOfWeek.SelectedIndex = 0; // Index 0 is "Not Specific...".
                }
                else // Safety check if ComboBox is somehow empty.
                {
                    cmbRunOnDayOfWeek.SelectedIndex = -1; // No selection.
                }
            }

            // Populate other text fields from the definition.
            // txtEnableConfigKey.Text = definition.EnableConfigKey; // REMOVED - EnableConfigKey is obsolete.
            txtSuccessFlagJsonName.Text = definition.SuccessFlagJsonName;
            txtGreetingKey.Text = definition.GreetingKey;
            txtRecipientCategoryKey.Text = definition.RecipientCategoryKey;
            txtSubjectPrefix.Text = definition.SubjectPrefix;
            txtTemplateName.Text = definition.TemplateName;

            // Populate NumericUpDown controls, providing defaults if nullable properties are null.
            numReportEndDateOffsetDays.Value = definition.ReportEndDateOffsetDays ?? 0;
            numReportDurationDays.Value = definition.ReportDurationDays ?? 1; // Default to 1 day if null.      

            // Populate CheckBox controls.
            chkRequiresNetValueFiltering.Checked = definition.RequiresNetValueFiltering;
            chkAppendToPowerBi.Checked = definition.AppendToPowerBi;

            // Store the ReportId (typically in a hidden label or Tag) for update operations.
            lblReportId.Text = definition.ReportId;
        }

        /// <summary>
        /// Clears all input fields in the "Definition Details" group box to their default states,
        /// deselects any row in the DataGridView, and resets button states.
        /// This is used when preparing for a new entry or after an operation.
        /// </summary>
        private void ClearInputFields()
        {
            txtReportName.Clear();
            chkIsEnabled.Checked = true; // Default new definitions to be enabled.
            cmbReportTypeIndex.SelectedIndex = -1; // No selection for report type.

            // Default RunOnDayOfWeek to "Not Specific".
            if (cmbRunOnDayOfWeek.Items.Count > 0)
            {
                cmbRunOnDayOfWeek.SelectedIndex = 0;
            }
            else
            {
                cmbRunOnDayOfWeek.SelectedIndex = -1; // Safety if ComboBox is empty.
            }

            // txtEnableConfigKey.Clear(); // REMOVED - EnableConfigKey is obsolete.
            txtSuccessFlagJsonName.Clear();
            txtGreetingKey.Clear();
            txtRecipientCategoryKey.Clear();
            txtSubjectPrefix.Clear();
            txtTemplateName.Clear();
            numReportEndDateOffsetDays.Value = 0; // Default offset.
            numReportDurationDays.Value = 1;    // Default duration.
            chkRequiresNetValueFiltering.Checked = false;
            chkAppendToPowerBi.Checked = false;
            lblReportId.Text = string.Empty; // Clear any stored ReportId.

            _selectedDefinitionInFields = null; // No definition is currently loaded in the fields.
            dgvReportDefinitions.ClearSelection();  // Deselect any row in the grid.
            btnAdd.Text = "Add";                    // Reset "New" button text back to "Add".
            btnUpdate.Enabled = false;              // Disable Update button as no item is selected for update.
            btnDelete.Enabled = false;              // Disable Delete button.
            txtReportName.Focus();                  // Set focus to the Report Name field for new entry.
        }

        /// <summary>
        /// Helper method to select an item in a ComboBox based on its underlying value.
        /// This is used for ComboBoxes that are populated with anonymous types having "Text" and "Value" properties.
        /// </summary>
        /// <param name="comboBox">The ComboBox control whose selection is to be set.</param>
        /// <param name="value">The value to match against the "Value" property of the ComboBox items.</param>
        private void SelectComboBoxItemByValue(ComboBox comboBox, object? value)
        {
            if (value == null)
            {
                comboBox.SelectedIndex = -1; // If value is null, deselect.
                return;
            }
            // Iterate through ComboBox items to find a match.
            for (int i = 0; i < comboBox.Items.Count; i++)
            {
                // ComboBox items are anonymous types like new { Text = "...", Value = ... }.
                // Use 'dynamic' to access the 'Value' property without explicit casting.
                dynamic? item = comboBox.Items[i];
                if (item != null && item.Value.Equals(value))
                {
                    comboBox.SelectedIndex = i; // Select the matching item.
                    return;
                }
            }
            comboBox.SelectedIndex = -1; // If no match found, deselect.
        }

        /// <summary>
        /// Creates an <see cref="AutoReportDefinition"/> object from the current values in the input fields.
        /// Performs basic validation on required fields.
        /// </summary>
        /// <param name="existingReportId">The ID of an existing report definition if this is an update operation; 
        /// null or empty if creating a new definition (a new GUID will be generated).</param>
        /// <returns>A new or updated <see cref="AutoReportDefinition"/> object populated with data from the form.</returns>
        /// <exception cref="InvalidOperationException">Thrown if validation fails (e.g., required fields like Report Name or Report Type Index are empty).</exception>
        private AutoReportDefinition GetDefinitionFromInputFields(string? existingReportId = null)
        {
            // --- Basic Validation ---
            if (string.IsNullOrWhiteSpace(txtReportName.Text))
            {
                FlexibleMessageBox.Show(this, "Report Name cannot be empty.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtReportName.Focus();
                throw new InvalidOperationException("Report Name is required.");
            }
            if (cmbReportTypeIndex.SelectedItem == null)
            {
                FlexibleMessageBox.Show(this, "Report Type Index must be selected.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                cmbReportTypeIndex.Focus();
                throw new InvalidOperationException("Report Type Index is required.");
            }

            // Determine RunOnDayOfWeek value from ComboBox.
            object? selectedDayOfWeekItem = cmbRunOnDayOfWeek.SelectedItem;
            DayOfWeek? runOnDay = null;
            if (selectedDayOfWeekItem is DayOfWeek dow) // If the selected item is already a DayOfWeek enum value.
            {
                runOnDay = dow;
            }
            // If "Not Specific" (which is a string at index 0) is selected, runOnDay remains null.

            // Create and populate the AutoReportDefinition object.
            return new AutoReportDefinition
            {
                ReportId = string.IsNullOrWhiteSpace(existingReportId) ? Guid.NewGuid().ToString() : existingReportId, // Use existing ID or generate new.
                ReportName = txtReportName.Text.Trim(),
                IsEnabled = chkIsEnabled.Checked,
                ReportTypeIndex = (int)((dynamic)cmbReportTypeIndex.SelectedItem).Value, // Get value from anonymous type.
                RunOnDayOfWeek = runOnDay,
                // EnableConfigKey is removed.
                // Auto-generate SuccessFlagJsonName and GreetingKey if left empty, based on ReportName, for convenience.
                SuccessFlagJsonName = string.IsNullOrWhiteSpace(txtSuccessFlagJsonName.Text) ? $"{txtReportName.Text.Replace(" ", "")}Succeeded" : txtSuccessFlagJsonName.Text.Trim(),
                GreetingKey = string.IsNullOrWhiteSpace(txtGreetingKey.Text) ? $"AutoRun_{txtReportName.Text.Replace(" ", "")}" : txtGreetingKey.Text.Trim(),
                RecipientCategoryKey = string.IsNullOrWhiteSpace(txtRecipientCategoryKey.Text) ? null : txtRecipientCategoryKey.Text.Trim(), // Null if empty.
                SubjectPrefix = txtSubjectPrefix.Text.Trim(),
                TemplateName = txtTemplateName.Text.Trim(),
                // For nullable numeric fields, use value from NumericUpDown.
                // If a specific default (like null for 0/1) is needed, adjust here based on report type or user intent.
                ReportEndDateOffsetDays = (int)numReportEndDateOffsetDays.Value,
                ReportDurationDays = (int)numReportDurationDays.Value,
                RequiresNetValueFiltering = chkRequiresNetValueFiltering.Checked,
                AppendToPowerBi = chkAppendToPowerBi.Checked
            };
        }

        /// <summary>
        /// Sets the internal flag indicating whether there are unsaved changes on the form.
        /// Also logs the change of this flag's state.
        /// </summary>
        /// <param name="hasChanges">True if there are unsaved changes, false otherwise.</param>
        private void SetHasUnsavedChanges(bool hasChanges)
        {
            if (_hasUnsavedChanges != hasChanges) // Log only if state actually changes
            {
                _hasUnsavedChanges = hasChanges;
                Logger.LogTrace($"SetHasUnsavedChanges: {_hasUnsavedChanges}");
            }
        }
        #endregion

        #region Button Event Handlers
        /// <summary>
        /// Handles the Click event for the "Add" / "New" button.
        /// If the button text is "New" (meaning a definition is currently selected and displayed in fields),
        /// it clears the input fields to prepare for adding a brand new definition.
        /// Otherwise (if button text is "Add"), it attempts to create a new <see cref="AutoReportDefinition"/>
        /// from the input fields and add it to the `_bindingList`.
        /// </summary>
        /// <param name="sender">The source of the event (the "Add" button).</param>
        /// <param name="e">An <see cref="EventArgs"/> that contains no event data.</param>
        private void btnAdd_Click(object? sender, EventArgs e)
        {
            // If button text is "New", it means user wants to clear fields to start a new definition.
            if (btnAdd.Text == "New")
            {
                ClearInputFields(); // Clears fields, deselects grid, resets button text to "Add".
                return;
            }

            // Proceed to add a new definition based on current field values.
            try
            {
                AutoReportDefinition newDefinition = GetDefinitionFromInputFields(); // Validates and creates object.

                // Check for duplicate ReportName before adding.
                if (_bindingList.Any(d => d.ReportName.Equals(newDefinition.ReportName, StringComparison.OrdinalIgnoreCase)))
                {
                    FlexibleMessageBox.Show(this, $"A report definition with the name '{newDefinition.ReportName}' already exists. Please use a unique name.", "Duplicate Name", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                _bindingList.Add(newDefinition); // Add to the binding list (which updates the DataGridView).
                SetHasUnsavedChanges(true);      // Mark that there are unsaved changes.
                ClearInputFields();              // Clear input fields for the next potential entry.
                Logger.LogInfo($"New auto-report definition '{newDefinition.ReportName}' added to UI list. Save needed to persist.");
            }
            catch (InvalidOperationException valEx) // Catch validation errors from GetDefinitionFromInputFields.
            {
                Logger.LogWarning($"Validation failed while adding report definition: {valEx.Message}");
                // User has already been shown a FlexibleMessageBox by GetDefinitionFromInputFields.
            }
            catch (Exception ex) // Catch other unexpected errors.
            {
                Logger.LogError($"Error adding report definition: {ex.Message}", ex);
                FlexibleMessageBox.Show(this, $"Could not add report definition.\nError: {ex.Message}", "Add Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Handles the Click event for the "Update" button.
        /// Validates the input fields and updates the properties of the currently selected 
        /// (and displayed in fields via `_selectedDefinitionInFields`) report definition in the `_bindingList`.
        /// </summary>
        /// <param name="sender">The source of the event (the "Update" button).</param>
        /// <param name="e">An <see cref="EventArgs"/> that contains no event data.</param>
        private void btnUpdate_Click(object? sender, EventArgs e)
        {
            // Ensure a definition is actually loaded into the fields for update.
            if (_selectedDefinitionInFields == null || string.IsNullOrEmpty(lblReportId.Text))
            {
                FlexibleMessageBox.Show(this, "Please select a report definition from the list to load its details before updating.", "No Definition Loaded", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                // Get the updated definition details from input fields, using the existing ReportId.
                AutoReportDefinition updatedDefinitionFromFields = GetDefinitionFromInputFields(lblReportId.Text);

                // Find the definition object in the binding list using its unique ReportId.
                var definitionToUpdate = _bindingList.FirstOrDefault(d => d.ReportId == lblReportId.Text);
                if (definitionToUpdate != null)
                {
                    // Check for duplicate ReportName if the name is being changed.
                    if (!definitionToUpdate.ReportName.Equals(updatedDefinitionFromFields.ReportName, StringComparison.OrdinalIgnoreCase) &&
                        _bindingList.Any(d => d.ReportId != definitionToUpdate.ReportId && d.ReportName.Equals(updatedDefinitionFromFields.ReportName, StringComparison.OrdinalIgnoreCase)))
                    {
                        FlexibleMessageBox.Show(this, $"Another report definition with the name '{updatedDefinitionFromFields.ReportName}' already exists. Please use a unique name.", "Duplicate Name", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    // Update properties of the existing object in the BindingList.
                    // This ensures the DataGridView updates correctly due to data binding.
                    definitionToUpdate.ReportName = updatedDefinitionFromFields.ReportName;
                    definitionToUpdate.IsEnabled = updatedDefinitionFromFields.IsEnabled;
                    definitionToUpdate.ReportTypeIndex = updatedDefinitionFromFields.ReportTypeIndex;
                    definitionToUpdate.RunOnDayOfWeek = updatedDefinitionFromFields.RunOnDayOfWeek;
                    definitionToUpdate.SuccessFlagJsonName = updatedDefinitionFromFields.SuccessFlagJsonName;
                    definitionToUpdate.GreetingKey = updatedDefinitionFromFields.GreetingKey;
                    definitionToUpdate.RecipientCategoryKey = updatedDefinitionFromFields.RecipientCategoryKey;
                    definitionToUpdate.SubjectPrefix = updatedDefinitionFromFields.SubjectPrefix;
                    definitionToUpdate.TemplateName = updatedDefinitionFromFields.TemplateName;
                    definitionToUpdate.ReportEndDateOffsetDays = updatedDefinitionFromFields.ReportEndDateOffsetDays;
                    definitionToUpdate.ReportDurationDays = updatedDefinitionFromFields.ReportDurationDays;
                    definitionToUpdate.RequiresNetValueFiltering = updatedDefinitionFromFields.RequiresNetValueFiltering;
                    definitionToUpdate.AppendToPowerBi = updatedDefinitionFromFields.AppendToPowerBi;

                    // Notify the BindingList (and thus DataGridView) that the item has changed.
                    int indexToUpdate = _bindingList.IndexOf(definitionToUpdate);
                    if (indexToUpdate != -1)
                    {
                        _bindingList.ResetItem(indexToUpdate); // More targeted refresh for the specific item.
                    }
                    else
                    {
                        _bindingList.ResetBindings(); // Fallback if index not found (should not happen).
                    }

                    SetHasUnsavedChanges(true); // Mark that there are unsaved changes.
                    Logger.LogInfo($"Auto-report definition '{definitionToUpdate.ReportName}' (ID: {definitionToUpdate.ReportId}) updated in UI list. Save needed to persist.");
                    ClearInputFields(); // Clear input fields and reset selection after update.
                }
                else // Should not happen if _selectedDefinitionInFields was valid.
                {
                    Logger.LogWarning($"Could not find report definition with ID '{lblReportId.Text}' in the binding list to update. This indicates a sync issue.");
                    FlexibleMessageBox.Show(this, "Selected report definition not found for update. It might have been removed or there's a sync issue.", "Update Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (InvalidOperationException valEx) // Catch validation errors from GetDefinitionFromInputFields.
            {
                Logger.LogWarning($"Validation failed while updating report definition: {valEx.Message}");
            }
            catch (Exception ex) // Catch other unexpected errors.
            {
                Logger.LogError($"Error updating report definition: {ex.Message}", ex);
                FlexibleMessageBox.Show(this, $"Could not update report definition.\nError: {ex.Message}", "Update Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Handles the Click event for the "Delete" button.
        /// Confirms with the user and removes the currently selected report definition (from the DataGridView)
        /// from the `_bindingList`.
        /// </summary>
        /// <param name="sender">The source of the event (the "Delete" button).</param>
        /// <param name="e">An <see cref="EventArgs"/> that contains no event data.</param>
        private void btnDelete_Click(object? sender, EventArgs e)
        {
            if (_bindingList == null)
            {
                Logger.LogError("btnDelete_Click: _bindingList is null. This indicates an initialization problem with the form or data loading.");
                FlexibleMessageBox.Show(this, "An internal error occurred (BindingList not ready). Cannot delete.", "Critical Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Determine the definition to delete based on the current DataGridView selection.
            AutoReportDefinition? definitionToDeleteFromGrid = null;
            if (dgvReportDefinitions.SelectedRows.Count > 0 &&
                dgvReportDefinitions.SelectedRows[0].DataBoundItem is AutoReportDefinition dgvSelectedDef)
            {
                definitionToDeleteFromGrid = dgvSelectedDef;
            }

            if (definitionToDeleteFromGrid == null)
            {
                FlexibleMessageBox.Show(this, "Please select a report definition from the list to delete.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Ensure the definition to delete has a valid ReportId (it should, due to model constructor and load logic).
            if (string.IsNullOrEmpty(definitionToDeleteFromGrid.ReportId))
            {
                Logger.LogError($"btnDelete_Click: Selected definition (Name: '{definitionToDeleteFromGrid.ReportName}') has a null or empty ReportId. This is unexpected and prevents reliable deletion.");
                FlexibleMessageBox.Show(this, "The selected report definition has an invalid ID. Cannot delete.", "Data Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            string reportNameToDisplay = definitionToDeleteFromGrid.ReportName ?? "[Unnamed Report]";
            Logger.LogDebug($"Attempting to delete definition: ID='{definitionToDeleteFromGrid.ReportId}', Name='{reportNameToDisplay}'");

            // Confirm deletion with the user.
            if (FlexibleMessageBox.Show(this, $"Are you sure you want to delete the report definition '{reportNameToDisplay}'?", "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                try
                {
                    // Find the exact item in the _bindingList using its unique ReportId to ensure correct removal.
                    // This is safer than relying on _selectedDefinitionInFields which might be stale if user interactions are complex.
                    var itemInBindingList = _bindingList.FirstOrDefault(def => def.ReportId == definitionToDeleteFromGrid.ReportId);
                    if (itemInBindingList != null)
                    {
                        bool removed = _bindingList.Remove(itemInBindingList); // Attempt to remove the found item.
                        if (removed)
                        {
                            SetHasUnsavedChanges(true); // Mark unsaved changes.
                            Logger.LogInfo($"Report definition '{reportNameToDisplay}' (ID: '{definitionToDeleteFromGrid.ReportId}') removed from UI list. Save needed to persist.");
                        }
                        else
                        {
                            // This case should ideally not happen if itemInBindingList was found by FirstOrDefault.
                            Logger.LogWarning($"_bindingList.Remove returned false for definition '{reportNameToDisplay}' (ID: '{definitionToDeleteFromGrid.ReportId}') even after finding it by ID. List state might be inconsistent.");
                        }
                    }
                    else
                    {
                        Logger.LogWarning($"Could not find definition with ID '{definitionToDeleteFromGrid.ReportId}' in _bindingList to remove. The DataGridView selection might be out of sync with the binding list, or the item was already removed by another means.");
                    }
                }
                catch (Exception ex) // Catch any unexpected errors during the remove operation.
                {
                    Logger.LogError($"Exception during delete operation for ID '{definitionToDeleteFromGrid.ReportId}': {ex.Message}", ex);
                    FlexibleMessageBox.Show(this, $"An error occurred while trying to delete: {ex.Message}", "Delete Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return; // Exit if deletion fails catastrophically.
                }
                ClearInputFields(); // Reset input fields and UI state after deletion.
            }
        }

        /// <summary>
        /// Handles the Click event for the "Save All Changes" button.
        /// Persists all current definitions (additions, updates, deletions reflected in `_bindingList`)
        /// to the dedicated configuration file.
        /// </summary>
        /// <param name="sender">The source of the event (the "Save All Changes" button).</param>
        /// <param name="e">An <see cref="EventArgs"/> that contains no event data.</param>
        private void btnSaveChanges_Click(object? sender, EventArgs e)
        {
            SaveReportDefinitions(); // Calls the method that saves to autoReportDefinitions.json.
        }

        /// <summary>
        /// Handles the Click event for the "Close" button.
        /// This method simply calls `this.Close()`, which will trigger the `FormClosing` event
        /// where the logic for handling unsaved changes resides.
        /// </summary>
        /// <param name="sender">The source of the event (the "Close" button).</param>
        /// <param name="e">An <see cref="EventArgs"/> that contains no event data.</param>
        private void btnClose_Click(object? sender, EventArgs e)
        {
            this.Close();
        }
        #endregion

        #region Form Closing Event
        /// <summary>
        /// Handles the FormClosing event, which occurs when the form is about to be closed.
        /// If there are unsaved changes (`_hasUnsavedChanges` is true), this method prompts the user
        /// to save them. Based on the user's response (Yes, No, Cancel), it either saves the changes,
        /// allows the form to close without saving, or cancels the closing operation.
        /// </summary>
        /// <param name="sender">The source of the event (the form itself).</param>
        /// <param name="e">A <see cref="FormClosingEventArgs"/> that contains data for the event and allows cancelling the close.</param>
        private void ManageAutoReportDefinitionsForm_FormClosing(object? sender, FormClosingEventArgs e)
        {
            if (_hasUnsavedChanges)
            {
                DialogResult result = FlexibleMessageBox.Show(this,
                    "You have unsaved changes. Would you like to save them before closing?",
                    "Unsaved Changes",
                    MessageBoxButtons.YesNoCancel,
                    MessageBoxIcon.Warning);

                if (result == DialogResult.Yes)
                {
                    SaveReportDefinitions(); // Attempt to save changes.
                    // After attempting to save, re-check if changes are still pending (e.g., if save failed).
                    if (_hasUnsavedChanges)
                    {
                        Logger.LogWarning("FormClosing: Save was attempted but changes might still be pending (e.g., save operation failed and showed an error). Cancelling form close to allow user to address.");
                        e.Cancel = true; // Prevent closing if save failed and changes are still considered pending.
                    }
                    // If save was successful, _hasUnsavedChanges will be false, and form will close.
                }
                else if (result == DialogResult.Cancel)
                {
                    e.Cancel = true; // Prevent the form from closing if user cancels.
                }
                // If DialogResult.No, do nothing, _hasUnsavedChanges remains true, but we allow form to close without saving.
            }
        }
        #endregion
    }
}
