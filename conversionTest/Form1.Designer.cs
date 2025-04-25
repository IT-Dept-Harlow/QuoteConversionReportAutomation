namespace conversionTest
{
    partial class Form1
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
            components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            startDatePicker = new DateTimePicker();
            endDatePicker = new DateTimePicker();
            startDateLabel = new Label();
            endDateLabel = new Label();
            createReportButton = new Button();
            processEmailButton = new Button();
            viewReportButton = new Button();
            viewAnalysisButton = new Button();
            statusLabel = new ToolStripStatusLabel();
            mainStatusStrip = new StatusStrip();
            autoRunStatusLabel = new ToolStripStatusLabel();
            sendToFemiOnlyCheckBox = new CheckBox();
            reportTypeComboBox = new ComboBox();
            reportTypeLabel = new Label();
            reportSettingsGroupBox = new GroupBox();
            emailRecipientLabel = new Label();
            financialYearLabel = new Label();
            financialYearComboBox = new ComboBox();
            toggleAutoRunButton = new Button();
            dailyCheckTimer = new System.Windows.Forms.Timer(components);
            menuStrip1 = new MenuStrip();
            optionsToolStripMenuItem = new ToolStripMenuItem();
            darkModeToolStripMenuItem = new ToolStripMenuItem();
            helpToolStripMenuItem = new ToolStripMenuItem();
            mainStatusStrip.SuspendLayout();
            reportSettingsGroupBox.SuspendLayout();
            menuStrip1.SuspendLayout();
            SuspendLayout();
            // 
            // startDatePicker
            // 
            startDatePicker.Location = new Point(261, 103);
            startDatePicker.Name = "startDatePicker";
            startDatePicker.Size = new Size(200, 22);
            startDatePicker.TabIndex = 0;
            // 
            // endDatePicker
            // 
            endDatePicker.Location = new Point(261, 135);
            endDatePicker.Name = "endDatePicker";
            endDatePicker.Size = new Size(200, 22);
            endDatePicker.TabIndex = 1;
            // 
            // startDateLabel
            // 
            startDateLabel.AutoSize = true;
            startDateLabel.Location = new Point(157, 109);
            startDateLabel.Name = "startDateLabel";
            startDateLabel.Size = new Size(93, 13);
            startDateLabel.TabIndex = 2;
            startDateLabel.Text = "Enter From Date:";
            // 
            // endDateLabel
            // 
            endDateLabel.AutoSize = true;
            endDateLabel.Location = new Point(157, 141);
            endDateLabel.Name = "endDateLabel";
            endDateLabel.Size = new Size(79, 13);
            endDateLabel.TabIndex = 3;
            endDateLabel.Text = "Enter To Date:";
            // 
            // createReportButton
            // 
            createReportButton.FlatAppearance.MouseDownBackColor = Color.FromArgb(128, 255, 128);
            createReportButton.FlatAppearance.MouseOverBackColor = Color.Gray;
            createReportButton.FlatStyle = FlatStyle.System;
            createReportButton.Font = new Font("Segoe UI", 8.25F, FontStyle.Bold);
            createReportButton.Location = new Point(187, 245);
            createReportButton.Name = "createReportButton";
            createReportButton.Size = new Size(134, 71);
            createReportButton.TabIndex = 5;
            createReportButton.Text = "Create Report";
            createReportButton.UseVisualStyleBackColor = true;
            createReportButton.Click += createReportButton_Click;
            // 
            // processEmailButton
            // 
            processEmailButton.FlatAppearance.MouseDownBackColor = Color.FromArgb(128, 255, 128);
            processEmailButton.FlatAppearance.MouseOverBackColor = Color.Gray;
            processEmailButton.FlatStyle = FlatStyle.System;
            processEmailButton.Font = new Font("Segoe UI", 8.25F, FontStyle.Bold);
            processEmailButton.Location = new Point(327, 245);
            processEmailButton.Name = "processEmailButton";
            processEmailButton.Size = new Size(134, 71);
            processEmailButton.TabIndex = 6;
            processEmailButton.Text = "Create Analysis &\r\nSend Email";
            processEmailButton.UseMnemonic = false;
            processEmailButton.UseVisualStyleBackColor = true;
            processEmailButton.Click += processEmailButton_Click;
            // 
            // viewReportButton
            // 
            viewReportButton.FlatAppearance.MouseDownBackColor = Color.FromArgb(128, 255, 128);
            viewReportButton.FlatAppearance.MouseOverBackColor = Color.Gray;
            viewReportButton.FlatStyle = FlatStyle.System;
            viewReportButton.Font = new Font("Segoe UI", 8.25F, FontStyle.Bold);
            viewReportButton.Location = new Point(218, 322);
            viewReportButton.Name = "viewReportButton";
            viewReportButton.Size = new Size(75, 23);
            viewReportButton.TabIndex = 8;
            viewReportButton.Text = "View File";
            viewReportButton.UseVisualStyleBackColor = true;
            viewReportButton.Click += viewReportButton_Click;
            // 
            // viewAnalysisButton
            // 
            viewAnalysisButton.FlatAppearance.MouseDownBackColor = Color.FromArgb(128, 255, 128);
            viewAnalysisButton.FlatAppearance.MouseOverBackColor = Color.Gray;
            viewAnalysisButton.FlatStyle = FlatStyle.System;
            viewAnalysisButton.Font = new Font("Segoe UI", 8.25F, FontStyle.Bold);
            viewAnalysisButton.Location = new Point(357, 324);
            viewAnalysisButton.Name = "viewAnalysisButton";
            viewAnalysisButton.Size = new Size(75, 23);
            viewAnalysisButton.TabIndex = 9;
            viewAnalysisButton.Text = "View File";
            viewAnalysisButton.UseVisualStyleBackColor = true;
            viewAnalysisButton.Click += viewAnalysisButton_Click;
            // 
            // statusLabel
            // 
            statusLabel.Name = "statusLabel";
            statusLabel.Size = new Size(0, 17);
            // 
            // mainStatusStrip
            // 
            mainStatusStrip.Items.AddRange(new ToolStripItem[] { statusLabel, autoRunStatusLabel });
            mainStatusStrip.Location = new Point(0, 437);
            mainStatusStrip.Name = "mainStatusStrip";
            mainStatusStrip.Size = new Size(635, 22);
            mainStatusStrip.TabIndex = 10;
            mainStatusStrip.Text = "mainStatusStrip";
            // 
            // autoRunStatusLabel
            // 
            autoRunStatusLabel.LiveSetting = System.Windows.Forms.Automation.AutomationLiveSetting.Assertive;
            autoRunStatusLabel.Name = "autoRunStatusLabel";
            autoRunStatusLabel.Size = new Size(620, 17);
            autoRunStatusLabel.Spring = true;
            autoRunStatusLabel.Text = "Auto Run: Disabled";
            autoRunStatusLabel.TextAlign = ContentAlignment.MiddleRight;
            // 
            // sendToFemiOnlyCheckBox
            // 
            sendToFemiOnlyCheckBox.AutoSize = true;
            sendToFemiOnlyCheckBox.FlatStyle = FlatStyle.Flat;
            sendToFemiOnlyCheckBox.Font = new Font("Segoe UI", 9.75F, FontStyle.Bold, GraphicsUnit.Point, 0);
            sendToFemiOnlyCheckBox.Location = new Point(119, 147);
            sendToFemiOnlyCheckBox.Name = "sendToFemiOnlyCheckBox";
            sendToFemiOnlyCheckBox.Size = new Size(142, 21);
            sendToFemiOnlyCheckBox.TabIndex = 11;
            sendToFemiOnlyCheckBox.Text = "Send to only Femi?";
            sendToFemiOnlyCheckBox.UseVisualStyleBackColor = true;
            // 
            // reportTypeComboBox
            // 
            reportTypeComboBox.AutoCompleteCustomSource.AddRange(new string[] { "Weekly", "Monthly", "Quarterly (3 Months)", "Annual" });
            reportTypeComboBox.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
            reportTypeComboBox.AutoCompleteSource = AutoCompleteSource.CustomSource;
            reportTypeComboBox.FormattingEnabled = true;
            reportTypeComboBox.Items.AddRange(new object[] { "Daily", "Weekly", "Monthly", "Quarterly (3 Months)", "Annual" });
            reportTypeComboBox.Location = new Point(261, 72);
            reportTypeComboBox.Name = "reportTypeComboBox";
            reportTypeComboBox.Size = new Size(200, 21);
            reportTypeComboBox.TabIndex = 12;
            reportTypeComboBox.SelectedIndexChanged += reportTypeComboBox_SelectedIndexChanged;
            // 
            // reportTypeLabel
            // 
            reportTypeLabel.AutoSize = true;
            reportTypeLabel.Location = new Point(157, 75);
            reportTypeLabel.Name = "reportTypeLabel";
            reportTypeLabel.Size = new Size(71, 13);
            reportTypeLabel.TabIndex = 13;
            reportTypeLabel.Text = "Report Type:";
            // 
            // reportSettingsGroupBox
            // 
            reportSettingsGroupBox.Controls.Add(emailRecipientLabel);
            reportSettingsGroupBox.Controls.Add(financialYearLabel);
            reportSettingsGroupBox.Controls.Add(sendToFemiOnlyCheckBox);
            reportSettingsGroupBox.Controls.Add(financialYearComboBox);
            reportSettingsGroupBox.Font = new Font("Arial", 8.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
            reportSettingsGroupBox.Location = new Point(142, 50);
            reportSettingsGroupBox.Name = "reportSettingsGroupBox";
            reportSettingsGroupBox.Size = new Size(346, 176);
            reportSettingsGroupBox.TabIndex = 14;
            reportSettingsGroupBox.TabStop = false;
            reportSettingsGroupBox.Text = "Report Settings";
            // 
            // emailRecipientLabel
            // 
            emailRecipientLabel.AutoSize = true;
            emailRecipientLabel.Font = new Font("Arial", 9.75F, FontStyle.Bold);
            emailRecipientLabel.Location = new Point(119, 147);
            emailRecipientLabel.Name = "emailRecipientLabel";
            emailRecipientLabel.Size = new Size(0, 16);
            emailRecipientLabel.TabIndex = 17;
            // 
            // financialYearLabel
            // 
            financialYearLabel.AutoSize = true;
            financialYearLabel.Location = new Point(15, 117);
            financialYearLabel.Name = "financialYearLabel";
            financialYearLabel.Size = new Size(78, 14);
            financialYearLabel.TabIndex = 16;
            financialYearLabel.Text = "Financial Year:";
            // 
            // financialYearComboBox
            // 
            financialYearComboBox.AutoCompleteCustomSource.AddRange(new string[] { "Daily", "Weekly", "Monthly", "Quarterly (3 Months)", "Annual" });
            financialYearComboBox.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
            financialYearComboBox.AutoCompleteSource = AutoCompleteSource.CustomSource;
            financialYearComboBox.FormattingEnabled = true;
            financialYearComboBox.Location = new Point(119, 114);
            financialYearComboBox.Name = "financialYearComboBox";
            financialYearComboBox.Size = new Size(200, 22);
            financialYearComboBox.TabIndex = 15;
            // 
            // toggleAutoRunButton
            // 
            toggleAutoRunButton.Location = new Point(12, 359);
            toggleAutoRunButton.Name = "toggleAutoRunButton";
            toggleAutoRunButton.Size = new Size(107, 54);
            toggleAutoRunButton.TabIndex = 16;
            toggleAutoRunButton.Text = "Enable Daily Auto Run @ 8 AM";
            toggleAutoRunButton.UseVisualStyleBackColor = true;
            toggleAutoRunButton.Click += toggleAutoRunButton_Click;
            // 
            // dailyCheckTimer
            // 
            dailyCheckTimer.Interval = 60000;
            dailyCheckTimer.Tick += dailyCheckTimer_Tick;
            // 
            // menuStrip1
            // 
            menuStrip1.Items.AddRange(new ToolStripItem[] { optionsToolStripMenuItem, helpToolStripMenuItem });
            menuStrip1.Location = new Point(0, 0);
            menuStrip1.Name = "menuStrip1";
            menuStrip1.Size = new Size(635, 24);
            menuStrip1.TabIndex = 18;
            menuStrip1.Text = "menuStrip1";
            // 
            // optionsToolStripMenuItem
            // 
            optionsToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { darkModeToolStripMenuItem });
            optionsToolStripMenuItem.Name = "optionsToolStripMenuItem";
            optionsToolStripMenuItem.Size = new Size(61, 20);
            optionsToolStripMenuItem.Text = "&Options";
            // 
            // darkModeToolStripMenuItem
            // 
            darkModeToolStripMenuItem.CheckOnClick = true;
            darkModeToolStripMenuItem.Name = "darkModeToolStripMenuItem";
            darkModeToolStripMenuItem.Size = new Size(132, 22);
            darkModeToolStripMenuItem.Text = "&Dark Mode";
            darkModeToolStripMenuItem.ToolTipText = "Enables/Disables Dark Mode";
            darkModeToolStripMenuItem.Click += darkModeToolStripMenuItem_Click;
            // 
            // helpToolStripMenuItem
            // 
            helpToolStripMenuItem.Name = "helpToolStripMenuItem";
            helpToolStripMenuItem.Size = new Size(44, 20);
            helpToolStripMenuItem.Text = "&Help";
            helpToolStripMenuItem.Click += helpToolStripMenuItem_Click;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(6F, 13F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = SystemColors.ControlLightLight;
            ClientSize = new Size(635, 459);
            Controls.Add(toggleAutoRunButton);
            Controls.Add(reportTypeLabel);
            Controls.Add(reportTypeComboBox);
            Controls.Add(mainStatusStrip);
            Controls.Add(menuStrip1);
            Controls.Add(viewAnalysisButton);
            Controls.Add(viewReportButton);
            Controls.Add(processEmailButton);
            Controls.Add(createReportButton);
            Controls.Add(endDateLabel);
            Controls.Add(startDateLabel);
            Controls.Add(endDatePicker);
            Controls.Add(startDatePicker);
            Controls.Add(reportSettingsGroupBox);
            Font = new Font("Segoe UI", 8.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            Icon = (Icon)resources.GetObject("$this.Icon");
            MainMenuStrip = menuStrip1;
            Name = "Form1";
            Text = "Quote Conversion Automation";
            Load += Form1_Load;
            mainStatusStrip.ResumeLayout(false);
            mainStatusStrip.PerformLayout();
            reportSettingsGroupBox.ResumeLayout(false);
            reportSettingsGroupBox.PerformLayout();
            menuStrip1.ResumeLayout(false);
            menuStrip1.PerformLayout();
            ResumeLayout(false);
            PerformLayout();

        }

        #endregion

        private System.Windows.Forms.DateTimePicker startDatePicker;
        private System.Windows.Forms.DateTimePicker endDatePicker;
        private System.Windows.Forms.Label startDateLabel;
        private System.Windows.Forms.Label endDateLabel;
        private System.Windows.Forms.Button createReportButton;
        private System.Windows.Forms.Button processEmailButton;
        private System.Windows.Forms.Button viewReportButton;
        private System.Windows.Forms.Button viewAnalysisButton;
        private System.Windows.Forms.ToolStripStatusLabel statusLabel;
        private System.Windows.Forms.StatusStrip mainStatusStrip;
        private System.Windows.Forms.CheckBox sendToFemiOnlyCheckBox;
        private System.Windows.Forms.ComboBox reportTypeComboBox;
        private System.Windows.Forms.Label reportTypeLabel;
        private System.Windows.Forms.GroupBox reportSettingsGroupBox;
        private System.Windows.Forms.Label financialYearLabel;
        private System.Windows.Forms.ComboBox financialYearComboBox;
        private Label emailRecipientLabel;
        private Button toggleAutoRunButton;
        private System.Windows.Forms.Timer dailyCheckTimer;
        private MenuStrip menuStrip1;
        private ToolStripMenuItem optionsToolStripMenuItem;
        private ToolStripMenuItem darkModeToolStripMenuItem;
        private ToolStripMenuItem helpToolStripMenuItem;
        private ToolStripStatusLabel autoRunStatusLabel;
    }
}

