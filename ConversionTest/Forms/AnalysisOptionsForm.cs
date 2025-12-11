using QuoteConversionReportAutomation.Managers;
using System;
using System.IO;
using System.Windows.Forms;
using System.ComponentModel;

namespace QuoteConversionReportAutomation.Forms
{
    public partial class AnalysisOptionsForm : Form
    {
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string SelectedFolder { get; private set; } = string.Empty;
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string FileNamePattern { get; private set; } = string.Empty;

        public AnalysisOptionsForm()
        {
            InitializeComponent();
        }

        private void AnalysisOptionsForm_Load(object sender, EventArgs e)
        {
            UIManager.ApplyThemeToExternalForm(this, Theming.ThemeSettings.IsCurrentlyDark());
        }

        private void btnSelectFolder_Click(object sender, EventArgs e)
        {
            using var dialog = new FolderBrowserDialog();
            dialog.Description = "Select the root folder containing the reports to analyse.";
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                SelectedFolder = dialog.SelectedPath;
                txtFolderPath.Text = SelectedFolder;
            }
        }

        private void btnSelectFile_Click(object sender, EventArgs e)
        {
            using var dialog = new OpenFileDialog();
            dialog.Title = "Select an example report file";
            dialog.Filter = "Excel Files (*.xlsx)|*.xlsx";
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                txtFilePath.Text = Path.GetFileName(dialog.FileName);
                // Generate the search pattern from the filename
                string fileName = Path.GetFileName(dialog.FileName);
                int underscoreIndex = fileName.IndexOf('_');
                if (underscoreIndex > 0)
                {
                    FileNamePattern = "*" + fileName.Substring(underscoreIndex);
                }
                else
                {
                    FileNamePattern = fileName; // Fallback if no underscore
                }
            }
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(SelectedFolder) || string.IsNullOrWhiteSpace(FileNamePattern))
            {
                MessageBox.Show("Please select both a folder and an example file.", "Input Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                this.DialogResult = DialogResult.None; // Prevent form from closing
                return;
            }
            this.DialogResult = DialogResult.OK;
        }
    }
}