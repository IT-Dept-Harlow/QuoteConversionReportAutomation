namespace QuoteConversionReportAutomation.Forms
{
    partial class DateRangeSelectionForm
    {
        private System.ComponentModel.IContainer components = null;
        protected override void Dispose(bool disposing) { if (disposing && (components != null)) { components.Dispose(); } base.Dispose(disposing); }
        private void InitializeComponent()
        {
            this.dtpStartDate = new System.Windows.Forms.DateTimePicker();
            this.dtpEndDate = new System.Windows.Forms.DateTimePicker();
            this.lblFrom = new System.Windows.Forms.Label();
            this.lblTo = new System.Windows.Forms.Label();
            this.btnOK = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.reportTypeComboBox = new System.Windows.Forms.ComboBox();
            this.lblReportType = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // dtpStartDate
            this.dtpStartDate.Location = new System.Drawing.Point(95, 42); this.dtpStartDate.Name = "dtpStartDate"; this.dtpStartDate.Size = new System.Drawing.Size(200, 20); this.dtpStartDate.TabIndex = 1;
            // dtpEndDate
            this.dtpEndDate.Location = new System.Drawing.Point(95, 72); this.dtpEndDate.Name = "dtpEndDate"; this.dtpEndDate.Size = new System.Drawing.Size(200, 20); this.dtpEndDate.TabIndex = 2;
            // lblFrom
            this.lblFrom.AutoSize = true; this.lblFrom.Location = new System.Drawing.Point(12, 46); this.lblFrom.Name = "lblFrom"; this.lblFrom.Size = new System.Drawing.Size(61, 13); this.lblFrom.TabIndex = 2; this.lblFrom.Text = "Start Date:";
            // lblTo
            this.lblTo.AutoSize = true; this.lblTo.Location = new System.Drawing.Point(12, 76); this.lblTo.Name = "lblTo"; this.lblTo.Size = new System.Drawing.Size(58, 13); this.lblTo.TabIndex = 3; this.lblTo.Text = "End Date:";
            // btnOK
            this.btnOK.DialogResult = System.Windows.Forms.DialogResult.OK; this.btnOK.Location = new System.Drawing.Point(139, 110); this.btnOK.Name = "btnOK"; this.btnOK.Size = new System.Drawing.Size(75, 23); this.btnOK.TabIndex = 3; this.btnOK.Text = "OK"; this.btnOK.UseVisualStyleBackColor = true;
            // btnCancel
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel; this.btnCancel.Location = new System.Drawing.Point(220, 110); this.btnCancel.Name = "btnCancel"; this.btnCancel.Size = new System.Drawing.Size(75, 23); this.btnCancel.TabIndex = 4; this.btnCancel.Text = "Cancel"; this.btnCancel.UseVisualStyleBackColor = true;
            // reportTypeComboBox
            this.reportTypeComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList; this.reportTypeComboBox.FormattingEnabled = true; this.reportTypeComboBox.Location = new System.Drawing.Point(95, 12); this.reportTypeComboBox.Name = "reportTypeComboBox"; this.reportTypeComboBox.Size = new System.Drawing.Size(200, 21); this.reportTypeComboBox.TabIndex = 0;
            // lblReportType
            this.lblReportType.AutoSize = true; this.lblReportType.Location = new System.Drawing.Point(12, 16); this.lblReportType.Name = "lblReportType"; this.lblReportType.Size = new System.Drawing.Size(70, 13); this.lblReportType.TabIndex = 7; this.lblReportType.Text = "Report Type:";
            // DateRangeSelectionForm
            this.AcceptButton = this.btnOK; this.CancelButton = this.btnCancel; this.ClientSize = new System.Drawing.Size(309, 147);
            this.Controls.Add(this.lblReportType); this.Controls.Add(this.reportTypeComboBox);
            this.Controls.Add(this.btnCancel); this.Controls.Add(this.btnOK); this.Controls.Add(this.lblTo);
            this.Controls.Add(this.lblFrom); this.Controls.Add(this.dtpEndDate); this.Controls.Add(this.dtpStartDate);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog; this.MaximizeBox = false; this.MinimizeBox = false; this.Name = "DateRangeSelectionForm"; this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent; this.Text = "Select Batch Process Options"; this.Load += new System.EventHandler(this.DateRangeSelectionForm_Load);
            this.ResumeLayout(false); this.PerformLayout();
        }
        private System.Windows.Forms.DateTimePicker dtpStartDate; private System.Windows.Forms.DateTimePicker dtpEndDate;
        private System.Windows.Forms.Label lblFrom; private System.Windows.Forms.Label lblTo;
        private System.Windows.Forms.Button btnOK; private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.ComboBox reportTypeComboBox; private System.Windows.Forms.Label lblReportType;
    }
}