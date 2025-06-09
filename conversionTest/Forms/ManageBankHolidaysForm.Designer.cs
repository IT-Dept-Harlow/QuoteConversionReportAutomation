namespace QuoteConversionReportAutomation.Forms
{
    partial class ManageBankHolidaysForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.grpOneOffHolidays = new System.Windows.Forms.GroupBox();
            this.btnRemoveOneOff = new System.Windows.Forms.Button();
            this.btnAddOneOff = new System.Windows.Forms.Button();
            this.txtOneOffDescription = new System.Windows.Forms.TextBox();
            this.lblOneOffDescription = new System.Windows.Forms.Label();
            this.dtpOneOffDate = new System.Windows.Forms.DateTimePicker();
            this.lblOneOffDate = new System.Windows.Forms.Label();
            this.lstOneOffHolidays = new System.Windows.Forms.ListView();
            this.colOneOffDate = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.colOneOffDescription = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.grpRecurringHolidays = new System.Windows.Forms.GroupBox();
            this.btnRemoveRecurring = new System.Windows.Forms.Button();
            this.btnAddRecurring = new System.Windows.Forms.Button();
            this.txtRecurringDescription = new System.Windows.Forms.TextBox();
            this.lblRecurringDescription = new System.Windows.Forms.Label();
            this.cmbRecurringMonth = new System.Windows.Forms.ComboBox();
            this.lblRecurringMonth = new System.Windows.Forms.Label();
            this.numRecurringDay = new System.Windows.Forms.NumericUpDown();
            this.lblRecurringDay = new System.Windows.Forms.Label();
            this.lstRecurringHolidays = new System.Windows.Forms.ListView();
            this.colRecurringDay = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.colRecurringMonth = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.colRecurringDescription = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.btnClose = new System.Windows.Forms.Button();
            this.toolTipManager = new System.Windows.Forms.ToolTip(this.components);
            this.grpOneOffHolidays.SuspendLayout();
            this.grpRecurringHolidays.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numRecurringDay)).BeginInit();
            this.SuspendLayout();
            // 
            // grpOneOffHolidays
            // 
            this.grpOneOffHolidays.Controls.Add(this.btnRemoveOneOff);
            this.grpOneOffHolidays.Controls.Add(this.btnAddOneOff);
            this.grpOneOffHolidays.Controls.Add(this.txtOneOffDescription);
            this.grpOneOffHolidays.Controls.Add(this.lblOneOffDescription);
            this.grpOneOffHolidays.Controls.Add(this.dtpOneOffDate);
            this.grpOneOffHolidays.Controls.Add(this.lblOneOffDate);
            this.grpOneOffHolidays.Controls.Add(this.lstOneOffHolidays);
            this.grpOneOffHolidays.Location = new System.Drawing.Point(12, 12);
            this.grpOneOffHolidays.Name = "grpOneOffHolidays";
            this.grpOneOffHolidays.Size = new System.Drawing.Size(360, 330);
            this.grpOneOffHolidays.TabIndex = 0;
            this.grpOneOffHolidays.TabStop = false;
            this.grpOneOffHolidays.Text = "One-Off Custom Bank Holidays";
            // 
            // btnRemoveOneOff
            // 
            this.btnRemoveOneOff.Location = new System.Drawing.Point(250, 295);
            this.btnRemoveOneOff.Name = "btnRemoveOneOff";
            this.btnRemoveOneOff.Size = new System.Drawing.Size(100, 23);
            this.btnRemoveOneOff.TabIndex = 6;
            this.btnRemoveOneOff.Text = "Remove Selected";
            this.toolTipManager.SetToolTip(this.btnRemoveOneOff, "Remove the selected one-off holiday from the list.");
            this.btnRemoveOneOff.UseVisualStyleBackColor = true;
            this.btnRemoveOneOff.Click += new System.EventHandler(this.btnRemoveOneOff_Click);
            // 
            // btnAddOneOff
            // 
            this.btnAddOneOff.Location = new System.Drawing.Point(275, 78);
            this.btnAddOneOff.Name = "btnAddOneOff";
            this.btnAddOneOff.Size = new System.Drawing.Size(75, 23);
            this.btnAddOneOff.TabIndex = 5;
            this.btnAddOneOff.Text = "Add";
            this.toolTipManager.SetToolTip(this.btnAddOneOff, "Add the specified one-off holiday.");
            this.btnAddOneOff.UseVisualStyleBackColor = true;
            this.btnAddOneOff.Click += new System.EventHandler(this.btnAddOneOff_Click);
            // 
            // txtOneOffDescription
            // 
            this.txtOneOffDescription.Location = new System.Drawing.Point(88, 52);
            this.txtOneOffDescription.Name = "txtOneOffDescription";
            this.txtOneOffDescription.Size = new System.Drawing.Size(262, 20);
            this.txtOneOffDescription.TabIndex = 4;
            this.toolTipManager.SetToolTip(this.txtOneOffDescription, "Enter a description for the one-off holiday (e.g., Royal Wedding).");
            // 
            // lblOneOffDescription
            // 
            this.lblOneOffDescription.AutoSize = true;
            this.lblOneOffDescription.Location = new System.Drawing.Point(7, 55);
            this.lblOneOffDescription.Name = "lblOneOffDescription";
            this.lblOneOffDescription.Size = new System.Drawing.Size(63, 13);
            this.lblOneOffDescription.TabIndex = 3;
            this.lblOneOffDescription.Text = "Description:";
            // 
            // dtpOneOffDate
            // 
            this.dtpOneOffDate.Location = new System.Drawing.Point(88, 25);
            this.dtpOneOffDate.Name = "dtpOneOffDate";
            this.dtpOneOffDate.Size = new System.Drawing.Size(200, 20);
            this.dtpOneOffDate.TabIndex = 2;
            this.toolTipManager.SetToolTip(this.dtpOneOffDate, "Select the date for the one-off holiday.");
            // 
            // lblOneOffDate
            // 
            this.lblOneOffDate.AutoSize = true;
            this.lblOneOffDate.Location = new System.Drawing.Point(7, 28);
            this.lblOneOffDate.Name = "lblOneOffDate";
            this.lblOneOffDate.Size = new System.Drawing.Size(33, 13);
            this.lblOneOffDate.TabIndex = 1;
            this.lblOneOffDate.Text = "Date:";
            // 
            // lstOneOffHolidays
            // 
            this.lstOneOffHolidays.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.colOneOffDate,
            this.colOneOffDescription});
            this.lstOneOffHolidays.FullRowSelect = true;
            this.lstOneOffHolidays.HideSelection = false;
            this.lstOneOffHolidays.Location = new System.Drawing.Point(10, 107);
            this.lstOneOffHolidays.MultiSelect = false;
            this.lstOneOffHolidays.Name = "lstOneOffHolidays";
            this.lstOneOffHolidays.Size = new System.Drawing.Size(340, 182);
            this.lstOneOffHolidays.TabIndex = 0;
            this.toolTipManager.SetToolTip(this.lstOneOffHolidays, "List of currently defined one-off custom bank holidays.");
            this.lstOneOffHolidays.UseCompatibleStateImageBehavior = false;
            this.lstOneOffHolidays.View = System.Windows.Forms.View.Details;
            // 
            // colOneOffDate
            // 
            this.colOneOffDate.Text = "Date";
            this.colOneOffDate.Width = 100;
            // 
            // colOneOffDescription
            // 
            this.colOneOffDescription.Text = "Description";
            this.colOneOffDescription.Width = 220;
            // 
            // grpRecurringHolidays
            // 
            this.grpRecurringHolidays.Controls.Add(this.btnRemoveRecurring);
            this.grpRecurringHolidays.Controls.Add(this.btnAddRecurring);
            this.grpRecurringHolidays.Controls.Add(this.txtRecurringDescription);
            this.grpRecurringHolidays.Controls.Add(this.lblRecurringDescription);
            this.grpRecurringHolidays.Controls.Add(this.cmbRecurringMonth);
            this.grpRecurringHolidays.Controls.Add(this.lblRecurringMonth);
            this.grpRecurringHolidays.Controls.Add(this.numRecurringDay);
            this.grpRecurringHolidays.Controls.Add(this.lblRecurringDay);
            this.grpRecurringHolidays.Controls.Add(this.lstRecurringHolidays);
            this.grpRecurringHolidays.Location = new System.Drawing.Point(388, 12);
            this.grpRecurringHolidays.Name = "grpRecurringHolidays";
            this.grpRecurringHolidays.Size = new System.Drawing.Size(400, 330);
            this.grpRecurringHolidays.TabIndex = 1;
            this.grpRecurringHolidays.TabStop = false;
            this.grpRecurringHolidays.Text = "Recurring Custom Bank Holidays (Same Day/Month Each Year)";
            // 
            // btnRemoveRecurring
            // 
            this.btnRemoveRecurring.Location = new System.Drawing.Point(285, 295);
            this.btnRemoveRecurring.Name = "btnRemoveRecurring";
            this.btnRemoveRecurring.Size = new System.Drawing.Size(100, 23);
            this.btnRemoveRecurring.TabIndex = 8;
            this.btnRemoveRecurring.Text = "Remove Selected";
            this.toolTipManager.SetToolTip(this.btnRemoveRecurring, "Remove the selected recurring holiday from the list.");
            this.btnRemoveRecurring.UseVisualStyleBackColor = true;
            this.btnRemoveRecurring.Click += new System.EventHandler(this.btnRemoveRecurring_Click);
            // 
            // btnAddRecurring
            // 
            this.btnAddRecurring.Location = new System.Drawing.Point(310, 78);
            this.btnAddRecurring.Name = "btnAddRecurring";
            this.btnAddRecurring.Size = new System.Drawing.Size(75, 23);
            this.btnAddRecurring.TabIndex = 7;
            this.btnAddRecurring.Text = "Add";
            this.toolTipManager.SetToolTip(this.btnAddRecurring, "Add the specified recurring holiday.");
            this.btnAddRecurring.UseVisualStyleBackColor = true;
            this.btnAddRecurring.Click += new System.EventHandler(this.btnAddRecurring_Click);
            // 
            // txtRecurringDescription
            // 
            this.txtRecurringDescription.Location = new System.Drawing.Point(88, 52);
            this.txtRecurringDescription.Name = "txtRecurringDescription";
            this.txtRecurringDescription.Size = new System.Drawing.Size(297, 20);
            this.txtRecurringDescription.TabIndex = 6;
            this.toolTipManager.SetToolTip(this.txtRecurringDescription, "Enter a description for the recurring holiday (e.g., Founder\'s Day).");
            // 
            // lblRecurringDescription
            // 
            this.lblRecurringDescription.AutoSize = true;
            this.lblRecurringDescription.Location = new System.Drawing.Point(7, 55);
            this.lblRecurringDescription.Name = "lblRecurringDescription";
            this.lblRecurringDescription.Size = new System.Drawing.Size(63, 13);
            this.lblRecurringDescription.TabIndex = 5;
            this.lblRecurringDescription.Text = "Description:";
            // 
            // cmbRecurringMonth
            // 
            this.cmbRecurringMonth.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbRecurringMonth.FormattingEnabled = true;
            this.cmbRecurringMonth.Location = new System.Drawing.Point(194, 25);
            this.cmbRecurringMonth.Name = "cmbRecurringMonth";
            this.cmbRecurringMonth.Size = new System.Drawing.Size(121, 21);
            this.cmbRecurringMonth.TabIndex = 4;
            this.toolTipManager.SetToolTip(this.cmbRecurringMonth, "Select the month for the recurring holiday.");
            // 
            // lblRecurringMonth
            // 
            this.lblRecurringMonth.AutoSize = true;
            this.lblRecurringMonth.Location = new System.Drawing.Point(148, 28);
            this.lblRecurringMonth.Name = "lblRecurringMonth";
            this.lblRecurringMonth.Size = new System.Drawing.Size(40, 13);
            this.lblRecurringMonth.TabIndex = 3;
            this.lblRecurringMonth.Text = "Month:";
            // 
            // numRecurringDay
            // 
            this.numRecurringDay.Location = new System.Drawing.Point(88, 26);
            this.numRecurringDay.Maximum = new decimal(new int[] {
            31,
            0,
            0,
            0});
            this.numRecurringDay.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numRecurringDay.Name = "numRecurringDay";
            this.numRecurringDay.Size = new System.Drawing.Size(45, 20);
            this.numRecurringDay.TabIndex = 2;
            this.toolTipManager.SetToolTip(this.numRecurringDay, "Select the day of the month for the recurring holiday.");
            this.numRecurringDay.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            // 
            // lblRecurringDay
            // 
            this.lblRecurringDay.AutoSize = true;
            this.lblRecurringDay.Location = new System.Drawing.Point(7, 28);
            this.lblRecurringDay.Name = "lblRecurringDay";
            this.lblRecurringDay.Size = new System.Drawing.Size(29, 13);
            this.lblRecurringDay.TabIndex = 1;
            this.lblRecurringDay.Text = "Day:";
            // 
            // lstRecurringHolidays
            // 
            this.lstRecurringHolidays.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.colRecurringDay,
            this.colRecurringMonth,
            this.colRecurringDescription});
            this.lstRecurringHolidays.FullRowSelect = true;
            this.lstRecurringHolidays.HideSelection = false;
            this.lstRecurringHolidays.Location = new System.Drawing.Point(10, 107);
            this.lstRecurringHolidays.MultiSelect = false;
            this.lstRecurringHolidays.Name = "lstRecurringHolidays";
            this.lstRecurringHolidays.Size = new System.Drawing.Size(375, 182);
            this.lstRecurringHolidays.TabIndex = 0;
            this.toolTipManager.SetToolTip(this.lstRecurringHolidays, "List of currently defined recurring custom bank holidays.");
            this.lstRecurringHolidays.UseCompatibleStateImageBehavior = false;
            this.lstRecurringHolidays.View = System.Windows.Forms.View.Details;
            // 
            // colRecurringDay
            // 
            this.colRecurringDay.Text = "Day";
            this.colRecurringDay.Width = 40;
            // 
            // colRecurringMonth
            // 
            this.colRecurringMonth.Text = "Month";
            this.colRecurringMonth.Width = 80;
            // 
            // colRecurringDescription
            // 
            this.colRecurringDescription.Text = "Description";
            this.colRecurringDescription.Width = 230;
            // 
            // btnClose
            // 
            this.btnClose.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnClose.Location = new System.Drawing.Point(713, 348);
            this.btnClose.Name = "btnClose";
            this.btnClose.Size = new System.Drawing.Size(75, 23);
            this.btnClose.TabIndex = 2;
            this.btnClose.Text = "Close";
            this.toolTipManager.SetToolTip(this.btnClose, "Close this window. Changes are saved automatically when adding/removing.");
            this.btnClose.UseVisualStyleBackColor = true;
            // 
            // ManageBankHolidaysForm
            // 
            this.AcceptButton = this.btnClose;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 383);
            this.Controls.Add(this.btnClose);
            this.Controls.Add(this.grpRecurringHolidays);
            this.Controls.Add(this.grpOneOffHolidays);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "ManageBankHolidaysForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Manage Custom Bank Holidays";
            this.Load += new System.EventHandler(this.ManageBankHolidaysForm_Load);
            this.grpOneOffHolidays.ResumeLayout(false);
            this.grpOneOffHolidays.PerformLayout();
            this.grpRecurringHolidays.ResumeLayout(false);
            this.grpRecurringHolidays.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numRecurringDay)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox grpOneOffHolidays;
        private System.Windows.Forms.ListView lstOneOffHolidays;
        private System.Windows.Forms.GroupBox grpRecurringHolidays;
        private System.Windows.Forms.ListView lstRecurringHolidays;
        private System.Windows.Forms.Button btnClose;
        private System.Windows.Forms.Button btnAddOneOff;
        private System.Windows.Forms.TextBox txtOneOffDescription;
        private System.Windows.Forms.Label lblOneOffDescription;
        private System.Windows.Forms.DateTimePicker dtpOneOffDate;
        private System.Windows.Forms.Label lblOneOffDate;
        private System.Windows.Forms.Button btnRemoveOneOff;
        private System.Windows.Forms.Button btnRemoveRecurring;
        private System.Windows.Forms.Button btnAddRecurring;
        private System.Windows.Forms.TextBox txtRecurringDescription;
        private System.Windows.Forms.Label lblRecurringDescription;
        private System.Windows.Forms.ComboBox cmbRecurringMonth;
        private System.Windows.Forms.Label lblRecurringMonth;
        private System.Windows.Forms.NumericUpDown numRecurringDay;
        private System.Windows.Forms.Label lblRecurringDay;
        private System.Windows.Forms.ColumnHeader colOneOffDate;
        private System.Windows.Forms.ColumnHeader colOneOffDescription;
        private System.Windows.Forms.ColumnHeader colRecurringDay;
        private System.Windows.Forms.ColumnHeader colRecurringMonth;
        private System.Windows.Forms.ColumnHeader colRecurringDescription;
        private System.Windows.Forms.ToolTip toolTipManager;
    }
}
