using Microsoft.Extensions.Configuration;
using QuoteConversionReportAutomation.Helpers;
using QuoteConversionReportAutomation.Managers;
using QuoteConversionReportAutomation.Models;
using System;
using System.Windows.Forms;

namespace QuoteConversionReportAutomation.Forms
{
    public partial class DateRangeSelectionForm : Form
    {
        private readonly IConfiguration _configuration;

        public DateTime StartDate => dtpStartDate.Value.Date;
        public DateTime EndDate => dtpEndDate.Value.Date;
        public ReportType SelectedReportType => ReportTypeHelper.FromString(reportTypeComboBox.SelectedItem?.ToString());

        public DateRangeSelectionForm(IConfiguration configuration)
        {
            _configuration = configuration;
            InitializeComponent();
        }

        private void DateRangeSelectionForm_Load(object sender, EventArgs e)
        {
            UIManager.ApplyThemeToExternalForm(this, Theming.ThemeSettings.IsCurrentlyDark());
            PopulateReportTypeComboBox();
            dtpEndDate.Value = DateTime.Today;
            dtpStartDate.Value = DateTime.Today.AddMonths(-1);
        }

        private void PopulateReportTypeComboBox()
        {
            reportTypeComboBox.Items.Clear();
            // Loop through all defined ReportType enum values
            foreach (ReportType type in Enum.GetValues(typeof(ReportType)))
            {
                // Exclude types that don't make sense for batch regeneration
                if (type == ReportType.Unknown || type == ReportType.Custom) continue;

                reportTypeComboBox.Items.Add(ReportTypeHelper.GetDisplayString(type, _configuration));
            }
            // Default to the first item
            if (reportTypeComboBox.Items.Count > 0)
            {
                reportTypeComboBox.SelectedIndex = 0;
            }
        }
    }
}