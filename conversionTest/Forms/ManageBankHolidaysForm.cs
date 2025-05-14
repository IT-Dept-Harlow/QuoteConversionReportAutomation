// C# 10+ Features
using QuoteConversionReportAutomation.Helpers;
using QuoteConversionReportAutomation.Managers; // Required to access UIManager
using QuoteConversionReportAutomation.Services.Logging; // Assuming Logger is here
using System.Data;
using System.Globalization;


namespace QuoteConversionReportAutomation
{
    /// <summary>
    /// Form to manage custom one-off and recurring bank holidays.
    /// Allows users to add, view, and remove custom bank holidays.
    /// Changes are persisted via BankHolidayHelper.
    /// Title bar and basic form theme are applied via UIManager.
    /// </summary>
    public partial class ManageBankHolidaysForm : Form
    {
        private readonly bool _isDarkMode; // Stores the theme state passed from the parent form

        // Theme Colors for child controls (consistent with UIManager's control/button colors)
        // Form's direct BackColor/ForeColor will be set by UIManager.ApplyThemeToExternalForm
        private static readonly Color DM_ControlBackColor = Color.FromArgb(60, 60, 63);
        private static readonly Color DM_ButtonBackColor = Color.FromArgb(80, 80, 80);
        private static readonly Color DM_ControlForeColor = Color.White; // General foreground for controls in dark mode
        private static readonly Color DM_ListViewHeaderBackColor = Color.FromArgb(70, 70, 73); // Slightly different for ListView header

        private static readonly Color LM_ControlBackColor = SystemColors.Window;
        private static readonly Color LM_ButtonBackColor = SystemColors.Control;
        private static readonly Color LM_ControlForeColor = SystemColors.ControlText; // General foreground for controls in light mode


        /// <summary>
        /// Initializes a new instance of the <see cref="ManageBankHolidaysForm"/> class.
        /// </summary>
        /// <param name="isDarkMode">Indicates whether dark mode should be applied to the form.</param>
        public ManageBankHolidaysForm(bool isDarkMode)
        {
            InitializeComponent();
            _isDarkMode = isDarkMode;

            this.ShowIcon = false;
            this.StartPosition = FormStartPosition.CenterParent;
            // The Load event handler is connected in the designer or can be added here if not:
            // this.Load += ManageBankHolidaysForm_Load; 
        }

        /// <summary>
        /// Handles the Load event of the form.
        /// Applies the theme (including title bar via UIManager), populates controls, and loads existing custom bank holidays.
        /// </summary>
        private void ManageBankHolidaysForm_Load(object sender, EventArgs e)
        {
            Logger.LogInfo($"ManageBankHolidaysForm loading. Initial DarkMode state: {_isDarkMode}");

            // Apply the overall form theme (title bar, main BackColor/ForeColor) using UIManager.
            UIManager.ApplyThemeToExternalForm(this, _isDarkMode);

            // Apply theme specifically to the child controls of this form.
            ApplyChildControlTheme(_isDarkMode);

            PopulateMonthComboBox();
            LoadOneOffHolidays();
            LoadRecurringHolidays();

            // Set default selection for ComboBox if items exist
            if (cmbRecurringMonth.Items.Count > 0)
            {
                cmbRecurringMonth.SelectedIndex = DateTime.Today.Month - 1; // Default to current month
            }
            dtpOneOffDate.Value = DateTime.Today; // Default to today for new one-off
            Logger.LogInfo("ManageBankHolidaysForm loaded and themed.");
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
            // For the form itself, its BackColor/ForeColor is set by UIManager.ApplyThemeToExternalForm.
            // For child controls, we apply specific theming.
            if (parentControl != this) // Don't re-apply to the form itself here
            {
                parentControl.ForeColor = controlForeColor; // Set ForeColor for most children
                                                            // Background for containers like GroupBox or Panel should match the form's background
                if (parentControl is GroupBox || parentControl is Panel || parentControl is TabControl || parentControl is TabPage)
                {
                    parentControl.BackColor = this.BackColor;
                }
                else if (!(parentControl is Button || parentControl is TextBox || parentControl is ComboBox ||
                           parentControl is DateTimePicker || parentControl is NumericUpDown || parentControl is ListView ||
                           parentControl is Label)) // Avoid re-coloring specific controls handled below
                {
                    // Fallback for other simple controls if any
                    parentControl.BackColor = controlBackColor;
                }
            }


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
                else if (control is TextBox tb)
                {
                    tb.BackColor = controlBackColor;
                    tb.ForeColor = controlForeColor;
                    tb.BorderStyle = isDarkMode ? BorderStyle.FixedSingle : BorderStyle.Fixed3D;
                }
                else if (control is ComboBox cb)
                {
                    cb.BackColor = controlBackColor;
                    cb.ForeColor = controlForeColor;
                    cb.FlatStyle = FlatStyle.Flat; // Or System for better OS consistency if preferred
                }
                else if (control is DateTimePicker dtp)
                {
                    dtp.BackColor = controlBackColor;
                    dtp.ForeColor = controlForeColor;
                    // Calendar theming (basic)
                    dtp.CalendarMonthBackground = controlBackColor;
                    dtp.CalendarForeColor = controlForeColor;
                    dtp.CalendarTitleBackColor = isDarkMode ? DM_ButtonBackColor : LM_ButtonBackColor; // Use button color for title
                    dtp.CalendarTitleForeColor = controlForeColor;
                    dtp.CalendarTrailingForeColor = isDarkMode ? Color.Gray : SystemColors.GrayText;
                }
                else if (control is NumericUpDown nud)
                {
                    nud.BackColor = controlBackColor;
                    nud.ForeColor = controlForeColor;
                }
                else if (control is ListView lv)
                {
                    lv.BackColor = controlBackColor;
                    lv.ForeColor = controlForeColor;
                    lv.OwnerDraw = isDarkMode; // Enable owner draw for dark mode for better selection/header
                    if (isDarkMode)
                    {
                        // Remove existing handlers before adding to prevent duplicates if called multiple times
                        lv.DrawItem -= ListView_DrawItem_Dark;
                        lv.DrawSubItem -= ListView_DrawSubItem_Dark;
                        lv.DrawColumnHeader -= ListView_DrawColumnHeader_Dark;
                        // Add new handlers
                        lv.DrawItem += ListView_DrawItem_Dark;
                        lv.DrawSubItem += ListView_DrawSubItem_Dark;
                        lv.DrawColumnHeader += ListView_DrawColumnHeader_Dark;
                    }
                    else
                    {
                        // Remove dark mode handlers if they were attached
                        lv.DrawItem -= ListView_DrawItem_Dark;
                        lv.DrawSubItem -= ListView_DrawSubItem_Dark;
                        lv.DrawColumnHeader -= ListView_DrawColumnHeader_Dark;
                    }
                    lv.BorderStyle = isDarkMode ? BorderStyle.FixedSingle : BorderStyle.Fixed3D;
                }
                else if (control is Label)
                {
                    control.BackColor = Color.Transparent;
                    control.ForeColor = controlForeColor;
                }
                else if (control is GroupBox gb)
                {
                    gb.ForeColor = controlForeColor;
                    gb.BackColor = this.BackColor;
                    if (gb.Controls.Count > 0)
                    {
                        UpdateControlThemeRecursive(gb, controlBackColor, buttonBackColor, controlForeColor, isDarkMode);
                    }
                }
                else if (control is Panel || control is TabControl || control is TabPage)
                {
                    control.BackColor = this.BackColor;
                    control.ForeColor = controlForeColor;
                    if (control.Controls.Count > 0)
                    {
                        UpdateControlThemeRecursive(control, controlBackColor, buttonBackColor, controlForeColor, isDarkMode);
                    }
                }
                // If there are other specific control types, add their theming here.
            }
        }


        #region ListView Owner Draw for Dark Mode
        private void ListView_DrawColumnHeader_Dark(object? sender, DrawListViewColumnHeaderEventArgs e)
        {
            // Only apply custom drawing if in dark mode
            if (_isDarkMode && sender is ListView lv)
            {
                e.Graphics.FillRectangle(new SolidBrush(DM_ListViewHeaderBackColor), e.Bounds); // Use a specific dark header color
                TextRenderer.DrawText(e.Graphics, e.Header.Text, e.Font, e.Bounds, DM_ControlForeColor, TextFormatFlags.VerticalCenter | TextFormatFlags.Left);
                // Optionally draw a border for the header
                // e.Graphics.DrawRectangle(Pens.DarkGray, e.Bounds.X, e.Bounds.Y, e.Bounds.Width -1, e.Bounds.Height -1);
            }
            else
            {
                e.DrawDefault = true; // Let the system draw it for light mode
            }
        }

        private void ListView_DrawItem_Dark(object? sender, DrawListViewItemEventArgs e)
        {
            // Only apply custom drawing if in dark mode
            if (_isDarkMode && sender is ListView lv)
            {
                e.DrawBackground(); // This handles the selection background correctly based on system colors or OwnerDraw settings
                                    // For dark mode, if you want a custom selection color not tied to system highlight, you'd fill it here.
                                    // e.g., if (e.Item.Selected) e.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(90,90,90)), e.Bounds);
                                    // else e.Graphics.FillRectangle(new SolidBrush(DM_ControlBackColor), e.Bounds);
                                    // Text is drawn by DrawSubItem
            }
            else
            {
                e.DrawDefault = true;
            }
        }

        private void ListView_DrawSubItem_Dark(object? sender, DrawListViewSubItemEventArgs e)
        {
            // Only apply custom drawing if in dark mode
            if (_isDarkMode && sender is ListView lv)
            {
                Color textColor;
                if (e.Item.Selected)
                {
                    // For selected items, use a color that contrasts with the system highlight or your custom selection background.
                    // SystemColors.HighlightText is usually a good choice.
                    textColor = SystemColors.HighlightText;
                    // The background for selected items is drawn by e.DrawBackground() in ListView_DrawItem
                    // or you can fill it here if you want full control over selection color.
                    // e.Graphics.FillRectangle(SystemBrushes.Highlight, e.Bounds); // If DrawBackground isn't doing what you want for selection
                }
                else
                {
                    textColor = DM_ControlForeColor; // Default dark mode text color
                    // Fill background for non-selected items if not handled by DrawItem or if DrawItem's e.DrawBackground isn't desired
                    // e.Graphics.FillRectangle(new SolidBrush(DM_ControlBackColor), e.Bounds); 
                }
                TextRenderer.DrawText(e.Graphics, e.SubItem.Text, e.SubItem.Font, e.Bounds, textColor, TextFormatFlags.VerticalCenter | TextFormatFlags.Left);
            }
            else
            {
                e.DrawDefault = true;
            }
        }
        #endregion


        /// <summary>
        /// Populates the month ComboBox for recurring holidays.
        /// </summary>
        private void PopulateMonthComboBox()
        {
            cmbRecurringMonth.Items.Clear();
            for (int i = 1; i <= 12; i++)
            {
                cmbRecurringMonth.Items.Add(CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(i));
            }
        }

        /// <summary>
        /// Loads and displays one-off custom bank holidays in the ListView.
        /// </summary>
        private void LoadOneOffHolidays()
        {
            lstOneOffHolidays.Items.Clear();
            var oneOffHolidays = BankHolidayHelper.GetCustomOneOffHolidays();
            foreach (var holiday in oneOffHolidays.OrderBy(h => h.Date))
            {
                var item = new ListViewItem(holiday.Date.ToString("yyyy-MM-dd"));
                item.SubItems.Add(holiday.Description);
                item.Tag = holiday.Date; // Store the date for easy removal
                lstOneOffHolidays.Items.Add(item);
            }
        }

        /// <summary>
        /// Loads and displays recurring custom bank holidays in the ListView.
        /// </summary>
        private void LoadRecurringHolidays()
        {
            lstRecurringHolidays.Items.Clear();
            var recurringHolidays = BankHolidayHelper.GetCustomRecurringHolidays();
            foreach (var holiday in recurringHolidays.OrderBy(h => h.Month).ThenBy(h => h.Day))
            {
                var item = new ListViewItem(holiday.Day.ToString());
                item.SubItems.Add(CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(holiday.Month));
                item.SubItems.Add(holiday.Description);
                item.Tag = holiday; // Store the whole entry for easy removal
                lstRecurringHolidays.Items.Add(item);
            }
        }

        /// <summary>
        /// Handles the Click event for the "Add" button for one-off holidays.
        /// </summary>
        private void btnAddOneOff_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtOneOffDescription.Text))
            {
                FlexibleMessageBox.Show(this, "Please enter a description for the one-off holiday.", "Input Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtOneOffDescription.Focus();
                return;
            }

            DateTime selectedDate = dtpOneOffDate.Value.Date;
            if (BankHolidayHelper.AddCustomBankHoliday(selectedDate, txtOneOffDescription.Text))
            {
                LoadOneOffHolidays(); // Refresh the list
                txtOneOffDescription.Clear();
            }
            else
            {
                FlexibleMessageBox.Show(this, $"A custom one-off holiday for {selectedDate:yyyy-MM-dd} already exists.", "Duplicate Holiday", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        /// <summary>
        /// Handles the Click event for the "Remove Selected" button for one-off holidays.
        /// </summary>
        private void btnRemoveOneOff_Click(object sender, EventArgs e)
        {
            if (lstOneOffHolidays.SelectedItems.Count == 0)
            {
                FlexibleMessageBox.Show(this, "Please select a one-off holiday to remove.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            ListViewItem selectedItem = lstOneOffHolidays.SelectedItems[0];
            if (selectedItem.Tag is DateTime holidayDate)
            {
                if (FlexibleMessageBox.Show(this, $"Are you sure you want to remove the holiday on {holidayDate:yyyy-MM-dd} ({selectedItem.SubItems[1].Text})?",
                                     "Confirm Removal", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    if (BankHolidayHelper.RemoveCustomOneOffHoliday(holidayDate))
                    {
                        LoadOneOffHolidays(); // Refresh the list
                    }
                }
            }
        }

        /// <summary>
        /// Handles the Click event for the "Add" button for recurring holidays.
        /// </summary>
        private void btnAddRecurring_Click(object sender, EventArgs e)
        {
            if (cmbRecurringMonth.SelectedItem == null)
            {
                FlexibleMessageBox.Show(this, "Please select a month for the recurring holiday.", "Input Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                cmbRecurringMonth.Focus();
                return;
            }
            if (string.IsNullOrWhiteSpace(txtRecurringDescription.Text))
            {
                FlexibleMessageBox.Show(this, "Please enter a description for the recurring holiday.", "Input Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtRecurringDescription.Focus();
                return;
            }

            int day = (int)numRecurringDay.Value;
            int month = cmbRecurringMonth.SelectedIndex + 1; // ComboBox is 0-indexed

            if (BankHolidayHelper.AddRecurringCustomBankHoliday(day, month, txtRecurringDescription.Text))
            {
                LoadRecurringHolidays(); // Refresh the list
                txtRecurringDescription.Clear();
            }
            else
            {
                FlexibleMessageBox.Show(this, $"A custom recurring holiday for Day {day}, Month {month} already exists.", "Duplicate Holiday", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        /// <summary>
        /// Handles the Click event for the "Remove Selected" button for recurring holidays.
        /// </summary>
        private void btnRemoveRecurring_Click(object sender, EventArgs e)
        {
            if (lstRecurringHolidays.SelectedItems.Count == 0)
            {
                FlexibleMessageBox.Show(this, "Please select a recurring holiday to remove.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            ListViewItem selectedItem = lstRecurringHolidays.SelectedItems[0];
            if (selectedItem.Tag is RecurringHolidayEntry holidayEntry)
            {
                if (FlexibleMessageBox.Show(this, $"Are you sure you want to remove the recurring holiday: {holidayEntry.Day} {CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(holidayEntry.Month)} ({holidayEntry.Description})?",
                                    "Confirm Removal", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    if (BankHolidayHelper.RemoveCustomRecurringHoliday(holidayEntry.Day, holidayEntry.Month))
                    {
                        LoadRecurringHolidays(); // Refresh the list
                    }
                }
            }
        }
    }
}
