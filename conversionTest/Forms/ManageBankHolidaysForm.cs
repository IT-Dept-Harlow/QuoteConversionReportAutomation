// C# 10+ Features
#region Using Directives
using QuoteConversionReportAutomation.Helpers;
using QuoteConversionReportAutomation.Managers; // Required to access UIManager
using QuoteConversionReportAutomation.Services.Logging; // Assuming Logger is here
using QuoteConversionReportAutomation.Theming; // Required for ThemeSettings and ThemePalette
using System.Data;
using System.Globalization;
using System.Windows.Forms;
#endregion

namespace QuoteConversionReportAutomation.Forms
{
    /// <summary>
    /// Form to manage custom one-off and recurring bank holidays.
    /// Allows users to add, view, and remove custom bank holidays.
    /// Changes are persisted via BankHolidayHelper.
    /// Theming is now handled by the centralised ThemeSettings.
    /// </summary>
    public partial class ManageBankHolidaysForm : Form
    {
        #region Constructor
        /// <summary>
        /// Initialises a new instance of the <see cref="ManageBankHolidaysForm"/> class.
        /// </summary>
        public ManageBankHolidaysForm()
        {
            InitializeComponent();

            this.ShowIcon = false;
            this.StartPosition = FormStartPosition.CenterParent;

            // The Load event handler is connected in the designer.
            // this.Load += ManageBankHolidaysForm_Load; 
        }
        #endregion

        #region Form Events
        /// <summary>
        /// Handles the Load event of the form.
        /// Applies the theme (including title bar via UIManager), populates controls, and loads existing custom bank holidays.
        /// </summary>
        private void ManageBankHolidaysForm_Load(object sender, EventArgs e)
        {
            Logger.LogInfo($"ManageBankHolidaysForm loading. Theming enabled: {ThemeSettings.EnableCustomTheming}, CurrentMode: {ThemeSettings.CurrentThemeMode}");

            // Apply the overall form theme (title bar, main BackColor/ForeColor) using UIManager.
            // This now uses the static ThemeSettings to determine if dark mode is active.
            UIManager.ApplyThemeToExternalForm(this, ThemeSettings.IsCurrentlyDark());

            // Apply theme specifically to the child controls of this form using the new palette.
            ApplyChildControlTheme();

            PopulateMonthComboBox();
            LoadOneOffHolidays();
            LoadRecurringHolidays();

            this.ShowIcon = false;
            this.StartPosition = FormStartPosition.CenterParent;

            // Set default selection for ComboBox if items exist
            if (cmbRecurringMonth.Items.Count > 0)
            {
                cmbRecurringMonth.SelectedIndex = DateTime.Today.Month - 1; // Default to current month
            }
            dtpOneOffDate.Value = DateTime.Today; // Default to today for new one-off
            Logger.LogInfo("ManageBankHolidaysForm loaded and themed.");
        }
        #endregion

        #region Theme Application
        /// <summary>
        /// Applies the current theme (dark or light) specifically to the child controls of this form
        /// by using the centralised ThemeSettings.CurrentPalette.
        /// The main form's BackColor, ForeColor, and title bar are handled by UIManager.ApplyThemeToExternalForm.
        /// </summary>
        private void ApplyChildControlTheme()
        {
            // Only apply custom themes if the feature is enabled.
            if (!ThemeSettings.EnableCustomTheming)
                return;

            bool isDarkMode = ThemeSettings.IsCurrentlyDark();
            ThemePalette palette = ThemeSettings.CurrentPalette;

            // Apply to all controls recursively within this form
            UpdateControlThemeRecursive(this, palette, isDarkMode);
        }

        /// <summary>
        /// Recursive helper to apply theme colours to child controls using the provided ThemePalette.
        /// </summary>
        /// <param name="parentControl">The control to apply the theme to and recurse through its children.</param>
        /// <param name="palette">The colour palette to use for theming.</param>
        /// <param name="isDarkMode">A flag indicating if dark mode is active, for specific style adjustments.</param>
        private void UpdateControlThemeRecursive(Control parentControl, ThemePalette palette, bool isDarkMode)
        {
            // For containers, ensure their background matches the main form, which is set by UIManager.
            if (parentControl is GroupBox || parentControl is Panel || parentControl is TabControl || parentControl is TabPage)
            {
                parentControl.BackColor = this.BackColor;
            }

            foreach (Control control in parentControl.Controls)
            {
                if (control is Button button)
                {
                    button.BackColor = palette.ButtonBackColor;
                    button.ForeColor = palette.ButtonForeColor;
                    button.FlatStyle = FlatStyle.Flat;
                    button.FlatAppearance.BorderColor = palette.ButtonBorderColor;
                    button.FlatAppearance.BorderSize = 1;
                }
                else if (control is TextBox tb)
                {
                    tb.BackColor = palette.ControlBackColor;
                    tb.ForeColor = palette.ControlForeColor;
                    tb.BorderStyle = isDarkMode ? BorderStyle.FixedSingle : BorderStyle.Fixed3D;
                }
                else if (control is ComboBox cb)
                {
                    cb.BackColor = palette.ControlBackColor;
                    cb.ForeColor = palette.ControlForeColor;
                    cb.FlatStyle = FlatStyle.Flat;
                }
                else if (control is DateTimePicker dtp)
                {
                    dtp.BackColor = palette.ControlBackColor;
                    dtp.ForeColor = palette.ControlForeColor;
                    // Calendar theming using the palette
                    dtp.CalendarMonthBackground = palette.ControlBackColor;
                    dtp.CalendarForeColor = palette.ControlForeColor;
                    dtp.CalendarTitleBackColor = palette.ButtonBackColor; // Use button colour for title
                    dtp.CalendarTitleForeColor = palette.ButtonForeColor;
                    dtp.CalendarTrailingForeColor = isDarkMode ? Color.Gray : SystemColors.GrayText;
                }
                else if (control is NumericUpDown nud)
                {
                    nud.BackColor = palette.ControlBackColor;
                    nud.ForeColor = palette.ControlForeColor;
                }
                else if (control is ListView lv)
                {
                    lv.BackColor = palette.ControlBackColor;
                    lv.ForeColor = palette.ControlForeColor;
                    lv.OwnerDraw = isDarkMode; // Enable owner draw for dark mode for better selection/header
                    if (isDarkMode)
                    {
                        // Remove existing handlers before adding to prevent duplicates
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
                else if (control is Label label)
                {
                    label.BackColor = Color.Transparent;
                    label.ForeColor = palette.LabelForeColor;
                }
                else if (control is GroupBox gb)
                {
                    gb.ForeColor = palette.GroupBoxForeColor;
                    // Recurse into the GroupBox
                    UpdateControlThemeRecursive(gb, palette, isDarkMode);
                }
                else if (control is Panel || control is TabControl || control is TabPage)
                {
                    // Recurse into other container controls
                    UpdateControlThemeRecursive(control, palette, isDarkMode);
                }
            }
        }
        #endregion

        #region ListView Owner Draw for Dark Mode
        /// <summary>
        /// Custom drawing for ListView column headers in dark mode.
        /// </summary>
        private void ListView_DrawColumnHeader_Dark(object? sender, DrawListViewColumnHeaderEventArgs e)
        {
            if (ThemeSettings.IsCurrentlyDark())
            {
                var palette = ThemeSettings.CurrentPalette;
                // Use a colour from the palette analogous to a header, like for DataGridViews
                e.Graphics.FillRectangle(new SolidBrush(palette.DataGridViewHeaderBackColor), e.Bounds);
                TextRenderer.DrawText(e.Graphics, e.Header.Text, e.Font, e.Bounds, palette.DataGridViewHeaderForeColor, TextFormatFlags.VerticalCenter | TextFormatFlags.Left);
            }
            else
            {
                e.DrawDefault = true; // Let the system draw it for light mode
            }
        }

        /// <summary>
        /// Custom drawing for a ListView item's background in dark mode, handling selection colour.
        /// </summary>
        private void ListView_DrawItem_Dark(object? sender, DrawListViewItemEventArgs e)
        {
            if (ThemeSettings.IsCurrentlyDark())
            {
                var palette = ThemeSettings.CurrentPalette;
                // Determine if the item is selected and draw the appropriate background from the palette.
                if ((e.State & ListViewItemStates.Selected) != 0)
                {
                    e.Graphics.FillRectangle(new SolidBrush(palette.DataGridViewSelectionBackColor), e.Bounds);
                }
                else
                {
                    e.Graphics.FillRectangle(new SolidBrush(palette.ControlBackColor), e.Bounds);
                }
                e.DrawFocusRectangle(); // Draw focus cues if the item has focus
            }
            else
            {
                e.DrawDefault = true;
            }
        }

        /// <summary>
        /// Custom drawing for a ListView sub-item's text in dark mode, handling selection colour.
        /// </summary>
        private void ListView_DrawSubItem_Dark(object? sender, DrawListViewSubItemEventArgs e)
        {
            if (ThemeSettings.IsCurrentlyDark())
            {
                var palette = ThemeSettings.CurrentPalette;
                // Determine the correct text colour based on whether the item is selected.
                Color textColor = ((e.ItemState & ListViewItemStates.Selected) != 0)
                                    ? palette.DataGridViewSelectionForeColor
                                    : palette.ControlForeColor;

                // The background is already handled by ListView_DrawItem_Dark. Here we just draw the text.
                TextRenderer.DrawText(e.Graphics, e.SubItem.Text, e.SubItem.Font, e.Bounds, textColor, TextFormatFlags.VerticalCenter | TextFormatFlags.Left);
            }
            else
            {
                e.DrawDefault = true;
            }
        }
        #endregion

        #region Data Loading
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
        #endregion

        #region UI Event Handlers
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
        #endregion
    }
}