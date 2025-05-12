// C# 10+ Features
using QuoteConversionReportAutomation.Helpers; // Assuming Logger is in this namespace
using System.Data;
using System.Globalization;

namespace QuoteConversionReportAutomation
{
    /// <summary>
    /// Form to manage custom one-off and recurring bank holidays.
    /// Allows users to add, view, and remove custom bank holidays.
    /// Changes are persisted via BankHolidayHelper.
    /// </summary>
    public partial class ManageBankHolidaysForm : Form
    {
        private bool _isDarkMode;

        // Define theme colors (can be shared or passed if more complex theming is needed)
        private static readonly Color DM_BackColor = Color.FromArgb(45, 45, 48);
        private static readonly Color DM_ForeColor = Color.White;
        private static readonly Color DM_ControlBackColor = Color.FromArgb(60, 60, 63);
        private static readonly Color DM_ButtonBackColor = Color.FromArgb(80, 80, 80);

        private static readonly Color LM_BackColor = SystemColors.Control;
        private static readonly Color LM_ForeColor = SystemColors.ControlText;
        private static readonly Color LM_ControlBackColor = SystemColors.Window;
        private static readonly Color LM_ButtonBackColor = SystemColors.Control;


        /// <summary>
        /// Initializes a new instance of the <see cref="ManageBankHolidaysForm"/> class.
        /// </summary>
        /// <param name="isDarkMode">Indicates whether dark mode should be applied to the form.</param>
        public ManageBankHolidaysForm(bool isDarkMode)
        {
            InitializeComponent();
            _isDarkMode = isDarkMode;
            Load += ManageBankHolidaysForm_Load;
        }

        /// <summary>
        /// Handles the Load event of the form.
        /// Populates controls and loads existing custom bank holidays.
        /// </summary>
        private void ManageBankHolidaysForm_Load(object sender, EventArgs e)
        {
            ApplyTheme();
            PopulateMonthComboBox();
            LoadOneOffHolidays();
            LoadRecurringHolidays();

            // Set default selection for ComboBox if items exist
            if (cmbRecurringMonth.Items.Count > 0)
            {
                cmbRecurringMonth.SelectedIndex = DateTime.Today.Month - 1; // Default to current month
            }
            dtpOneOffDate.Value = DateTime.Today; // Default to today for new one-off
        }

        /// <summary>
        /// Applies the current theme (dark or light) to the form and its controls.
        /// </summary>
        private void ApplyTheme()
        {
            Color backColor = _isDarkMode ? DM_BackColor : LM_BackColor;
            Color foreColor = _isDarkMode ? DM_ForeColor : LM_ForeColor;
            Color controlBackColor = _isDarkMode ? DM_ControlBackColor : LM_ControlBackColor;
            Color buttonBackColor = _isDarkMode ? DM_ButtonBackColor : LM_ButtonBackColor;

            BackColor = backColor;
            ForeColor = foreColor;

            foreach (Control control in Controls)
            {
                ApplyThemeToControlRecursive(control, backColor, foreColor, controlBackColor, buttonBackColor);
            }
        }

        /// <summary>
        /// Recursively applies theme colors to a control and its children.
        /// </summary>
        private void ApplyThemeToControlRecursive(Control parentControl, Color backColor, Color foreColor, Color controlBackColor, Color buttonBackColor)
        {
            parentControl.BackColor = backColor;
            parentControl.ForeColor = foreColor;

            if (parentControl is Button button)
            {
                button.BackColor = buttonBackColor;
                button.ForeColor = foreColor;
                button.FlatStyle = FlatStyle.System; // Or FlatStyle.Flat for more custom look
            }
            else if (parentControl is TextBox || parentControl is ComboBox || parentControl is DateTimePicker || parentControl is NumericUpDown || parentControl is ListView)
            {
                parentControl.BackColor = controlBackColor;
                parentControl.ForeColor = foreColor;
                if (parentControl is ListView lv) // Specific for ListView
                {
                    lv.OwnerDraw = _isDarkMode; // Enable owner draw for dark mode to handle selection colors better
                    if (_isDarkMode)
                    {
                        lv.DrawItem += ListView_DrawItem;
                        lv.DrawSubItem += ListView_DrawSubItem;
                        lv.DrawColumnHeader += ListView_DrawColumnHeader;
                    }
                    else
                    {
                        lv.DrawItem -= ListView_DrawItem;
                        lv.DrawSubItem -= ListView_DrawSubItem;
                        lv.DrawColumnHeader -= ListView_DrawColumnHeader;
                    }
                }
            }
            else if (parentControl is GroupBox gb)
            {
                gb.ForeColor = foreColor; // GroupBox title color
            }


            foreach (Control childControl in parentControl.Controls)
            {
                ApplyThemeToControlRecursive(childControl, backColor, foreColor, controlBackColor, buttonBackColor);
            }
        }


        #region ListView Owner Draw for Dark Mode
        private void ListView_DrawColumnHeader(object? sender, DrawListViewColumnHeaderEventArgs e)
        {
            if (_isDarkMode)
            {
                e.Graphics.FillRectangle(new SolidBrush(DM_ControlBackColor), e.Bounds);
                TextRenderer.DrawText(e.Graphics, e.Header.Text, e.Font, e.Bounds, DM_ForeColor, TextFormatFlags.VerticalCenter | TextFormatFlags.Left);
            }
            else
            {
                e.DrawDefault = true;
            }
        }

        private void ListView_DrawItem(object? sender, DrawListViewItemEventArgs e)
        {
            if (_isDarkMode)
            {
                e.DrawBackground(); // Draws the background (selection or default)
                // e.DrawText(); // Default text drawing might not use the right color for selected items
            }
            else
            {
                e.DrawDefault = true;
            }
        }

        private void ListView_DrawSubItem(object? sender, DrawListViewSubItemEventArgs e)
        {
            if (_isDarkMode)
            {
                // If selected, use system highlight text color, otherwise use dark mode forecolor
                Color textColor = e.Item.Selected ? SystemColors.HighlightText : DM_ForeColor;
                if (e.Item.Selected)
                {
                    e.Graphics.FillRectangle(SystemBrushes.Highlight, e.Bounds); // Use system highlight for selection background
                }
                else
                {
                    // Use the ListView's dark background color
                    e.Graphics.FillRectangle(new SolidBrush(DM_ControlBackColor), e.Bounds);
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
                MessageBox.Show("Please enter a description for the one-off holiday.", "Input Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtOneOffDescription.Focus();
                return;
            }

            DateTime selectedDate = dtpOneOffDate.Value.Date;
            if (BankHolidayHelper.AddCustomBankHoliday(selectedDate, txtOneOffDescription.Text))
            {
                LoadOneOffHolidays(); // Refresh the list
                txtOneOffDescription.Clear();
                // Optionally, provide feedback to the user
            }
            else
            {
                MessageBox.Show($"A custom one-off holiday for {selectedDate:yyyy-MM-dd} already exists.", "Duplicate Holiday", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        /// <summary>
        /// Handles the Click event for the "Remove Selected" button for one-off holidays.
        /// </summary>
        private void btnRemoveOneOff_Click(object sender, EventArgs e)
        {
            if (lstOneOffHolidays.SelectedItems.Count == 0)
            {
                MessageBox.Show("Please select a one-off holiday to remove.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            ListViewItem selectedItem = lstOneOffHolidays.SelectedItems[0];
            if (selectedItem.Tag is DateTime holidayDate)
            {
                if (MessageBox.Show($"Are you sure you want to remove the holiday on {holidayDate:yyyy-MM-dd} ({selectedItem.SubItems[1].Text})?",
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
                MessageBox.Show("Please select a month for the recurring holiday.", "Input Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                cmbRecurringMonth.Focus();
                return;
            }
            if (string.IsNullOrWhiteSpace(txtRecurringDescription.Text))
            {
                MessageBox.Show("Please enter a description for the recurring holiday.", "Input Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtRecurringDescription.Focus();
                return;
            }

            int day = (int)numRecurringDay.Value;
            int month = cmbRecurringMonth.SelectedIndex + 1; // ComboBox is 0-indexed

            if (BankHolidayHelper.AddRecurringCustomBankHoliday(day, month, txtRecurringDescription.Text))
            {
                LoadRecurringHolidays(); // Refresh the list
                txtRecurringDescription.Clear();
                // Optionally, provide feedback to the user
            }
            else
            {
                MessageBox.Show($"A custom recurring holiday for Day {day}, Month {month} already exists.", "Duplicate Holiday", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        /// <summary>
        /// Handles the Click event for the "Remove Selected" button for recurring holidays.
        /// </summary>
        private void btnRemoveRecurring_Click(object sender, EventArgs e)
        {
            if (lstRecurringHolidays.SelectedItems.Count == 0)
            {
                MessageBox.Show("Please select a recurring holiday to remove.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            ListViewItem selectedItem = lstRecurringHolidays.SelectedItems[0];
            if (selectedItem.Tag is RecurringHolidayEntry holidayEntry)
            {
                if (MessageBox.Show($"Are you sure you want to remove the recurring holiday: {holidayEntry.Day} {CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(holidayEntry.Month)} ({holidayEntry.Description})?",
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
