// Form1.Designer.cs
// Ensure this namespace matches your project structure, e.g., conversionTest
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
            generateAndSendButton = new Button();
            oneClickProcessButton = new Button();
            viewReportButton = new Button();
            viewAnalysisButton = new Button();
            statusLabel = new ToolStripStatusLabel();
            mainStatusStrip = new StatusStrip();
            autoRunStatusLabel = new ToolStripStatusLabel();
            sendToFemiOnlyCheckBox = new CheckBox();
            skipEmailCheckBox = new CheckBox();
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
            toolStripSeparator7 = new ToolStripSeparator();
            enable1ClickProcessingToolStripMenuItem = new ToolStripMenuItem();
            toolStripSeparator6 = new ToolStripSeparator();
            setAutoRunHourToolStripMenuItem = new ToolStripMenuItem();
            toolStripSeparator3 = new ToolStripSeparator();
            viewConfigToolStripMenuItem = new ToolStripMenuItem();
            validateConfigToolStripMenuItem = new ToolStripMenuItem();
            toolStripSeparator4 = new ToolStripSeparator();
            manageCustomBankHolidaysToolStripMenuItem = new ToolStripMenuItem();
            manageEmailRecipientsToolStripMenuItem = new ToolStripMenuItem();
            toolStripSeparator5 = new ToolStripSeparator();
            openLogsToolStripMenuItem = new ToolStripMenuItem();
            toolStripSeparator1 = new ToolStripSeparator();
            editConfigToolStripMenuItem = new ToolStripMenuItem();
            toolStripSeparator2 = new ToolStripSeparator();
            exitToolStripMenuItem = new ToolStripMenuItem();
            helpToolStripMenuItem = new ToolStripMenuItem();
            toolTip1 = new ToolTip(components);
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
            toolTip1.SetToolTip(startDatePicker, "Select the start date for the report period. Modifying this will set the Report Type to 'Custom'.");
            // 
            // endDatePicker
            // 
            endDatePicker.Location = new Point(261, 135);
            endDatePicker.Name = "endDatePicker";
            endDatePicker.Size = new Size(200, 22);
            endDatePicker.TabIndex = 1;
            toolTip1.SetToolTip(endDatePicker, "Select the end date for the report period. Modifying this will set the Report Type to 'Custom'.");
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
            createReportButton.FlatStyle = FlatStyle.System;
            createReportButton.Font = new Font("Segoe UI", 8.25F, FontStyle.Bold);
            createReportButton.Location = new Point(142, 260);
            createReportButton.Name = "createReportButton";
            createReportButton.Size = new Size(130, 71);
            createReportButton.TabIndex = 5;
            createReportButton.Text = "Create Report";
            toolTip1.SetToolTip(createReportButton, "Click to generate the raw Crystal Report based on the selected dates and report type.");
            createReportButton.UseVisualStyleBackColor = true;
            createReportButton.Click += createReportButton_Click;
            // 
            // processEmailButton
            // 
            processEmailButton.FlatStyle = FlatStyle.System;
            processEmailButton.Font = new Font("Segoe UI", 8.25F, FontStyle.Bold);
            processEmailButton.Location = new Point(358, 260);
            processEmailButton.Name = "processEmailButton";
            processEmailButton.Size = new Size(130, 71);
            processEmailButton.TabIndex = 6;
            processEmailButton.Text = "Create Analysis &\r\nSend Email";
            toolTip1.SetToolTip(processEmailButton, "Click to process the generated raw report, create the final analysis, and email it.");
            processEmailButton.UseMnemonic = false;
            processEmailButton.UseVisualStyleBackColor = true;
            processEmailButton.Click += processEmailButton_Click;
            // 
            // generateAndSendButton
            // 
            generateAndSendButton.Location = new Point(0, 0);
            generateAndSendButton.Name = "generateAndSendButton";
            generateAndSendButton.Size = new Size(75, 23);
            generateAndSendButton.TabIndex = 19;
            generateAndSendButton.Text = "Gen & Send";
            generateAndSendButton.Visible = false;
            // 
            // oneClickProcessButton
            // 
            oneClickProcessButton.FlatStyle = FlatStyle.System;
            oneClickProcessButton.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
            oneClickProcessButton.Location = new Point(220, 260);
            oneClickProcessButton.Name = "oneClickProcessButton";
            oneClickProcessButton.Size = new Size(200, 71);
            oneClickProcessButton.TabIndex = 20;
            oneClickProcessButton.Text = "Generate, Process && Email Report";
            toolTip1.SetToolTip(oneClickProcessButton, "Performs all steps: generates the raw report, processes it into the final analysis, and emails it (unless skipped).");
            oneClickProcessButton.UseVisualStyleBackColor = true;
            // 
            // viewReportButton
            // 
            viewReportButton.FlatStyle = FlatStyle.System;
            viewReportButton.Font = new Font("Segoe UI", 8.25F, FontStyle.Bold);
            viewReportButton.Location = new Point(175, 339);
            viewReportButton.Name = "viewReportButton";
            viewReportButton.Size = new Size(75, 23);
            viewReportButton.TabIndex = 8;
            viewReportButton.Text = "View File";
            toolTip1.SetToolTip(viewReportButton, "Click to open the generated raw report file.");
            viewReportButton.UseVisualStyleBackColor = true;
            viewReportButton.Click += viewReportButton_Click;
            // 
            // viewAnalysisButton
            // 
            viewAnalysisButton.FlatStyle = FlatStyle.System;
            viewAnalysisButton.Font = new Font("Segoe UI", 8.25F, FontStyle.Bold);
            viewAnalysisButton.Location = new Point(386, 339);
            viewAnalysisButton.Name = "viewAnalysisButton";
            viewAnalysisButton.Size = new Size(75, 23);
            viewAnalysisButton.TabIndex = 9;
            viewAnalysisButton.Text = "View File";
            toolTip1.SetToolTip(viewAnalysisButton, "Click to open the final processed analysis file.");
            viewAnalysisButton.UseVisualStyleBackColor = true;
            viewAnalysisButton.Click += viewAnalysisButton_Click;
            // 
            // statusLabel
            // 
            statusLabel.Name = "statusLabel";
            statusLabel.Size = new Size(512, 17);
            statusLabel.Spring = true;
            statusLabel.Text = "Ready";
            statusLabel.TextAlign = ContentAlignment.MiddleLeft;
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
            autoRunStatusLabel.Name = "autoRunStatusLabel";
            autoRunStatusLabel.Size = new Size(108, 17);
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
            toolTip1.SetToolTip(sendToFemiOnlyCheckBox, "Check this to send the email report only to Femi (and relevant CCs based on build mode). Uncheck to send to the broader team.");
            sendToFemiOnlyCheckBox.UseVisualStyleBackColor = true;
            // 
            // skipEmailCheckBox
            // 
            skipEmailCheckBox.AutoSize = true;
            skipEmailCheckBox.FlatStyle = FlatStyle.System;
            skipEmailCheckBox.Font = new Font("Segoe UI", 8.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
            skipEmailCheckBox.Location = new Point(15, 168);
            skipEmailCheckBox.Name = "skipEmailCheckBox";
            skipEmailCheckBox.Size = new Size(130, 18);
            skipEmailCheckBox.TabIndex = 21;
            skipEmailCheckBox.Text = "Skip Sending Email";
            toolTip1.SetToolTip(skipEmailCheckBox, "If checked, the email sending step will be skipped during processing.");
            skipEmailCheckBox.UseVisualStyleBackColor = true;
            // 
            // reportTypeComboBox
            // 
            reportTypeComboBox.AutoCompleteCustomSource.AddRange(new string[] { "Weekly", "Monthly", "Quarterly (3 Months)", "Annual" });
            reportTypeComboBox.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
            reportTypeComboBox.AutoCompleteSource = AutoCompleteSource.CustomSource;
            reportTypeComboBox.FormattingEnabled = true;
            reportTypeComboBox.Items.AddRange(new object[] { "Daily", "Weekly", "Monthly", "Quarterly (3 Months)", "Annual", "Custom" });
            reportTypeComboBox.Location = new Point(261, 72);
            reportTypeComboBox.Name = "reportTypeComboBox";
            reportTypeComboBox.Size = new Size(200, 21);
            reportTypeComboBox.TabIndex = 12;
            toolTip1.SetToolTip(reportTypeComboBox, "Select the type of report to generate (Daily, Weekly, etc.). Dates will adjust automatically based on the current date. Manual date changes will set this to 'Custom'.");
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
            reportSettingsGroupBox.Controls.Add(skipEmailCheckBox);
            reportSettingsGroupBox.Controls.Add(emailRecipientLabel);
            reportSettingsGroupBox.Controls.Add(financialYearLabel);
            reportSettingsGroupBox.Controls.Add(sendToFemiOnlyCheckBox);
            reportSettingsGroupBox.Controls.Add(financialYearComboBox);
            reportSettingsGroupBox.Font = new Font("Arial", 8.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
            reportSettingsGroupBox.Location = new Point(142, 50);
            reportSettingsGroupBox.Name = "reportSettingsGroupBox";
            reportSettingsGroupBox.Size = new Size(346, 200);
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
            toolTip1.SetToolTip(financialYearComboBox, "Select the financial year for the report. Only applicable for certain report types.");
            // 
            // toggleAutoRunButton
            // 
            toggleAutoRunButton.Location = new Point(12, 359);
            toggleAutoRunButton.Name = "toggleAutoRunButton";
            toggleAutoRunButton.Size = new Size(107, 54);
            toggleAutoRunButton.TabIndex = 16;
            toggleAutoRunButton.Text = "Enable Daily Auto Run @ 8 AM";
            toolTip1.SetToolTip(toggleAutoRunButton, "Enable or disable the automated daily report generation. The report runs around 8 AM for the previous workday.");
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
            optionsToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { darkModeToolStripMenuItem, toolStripSeparator7, enable1ClickProcessingToolStripMenuItem, toolStripSeparator6, setAutoRunHourToolStripMenuItem, toolStripSeparator3, viewConfigToolStripMenuItem, validateConfigToolStripMenuItem, toolStripSeparator4, manageCustomBankHolidaysToolStripMenuItem, manageEmailRecipientsToolStripMenuItem, toolStripSeparator5, openLogsToolStripMenuItem, toolStripSeparator1, editConfigToolStripMenuItem, toolStripSeparator2, exitToolStripMenuItem });
            optionsToolStripMenuItem.Name = "optionsToolStripMenuItem";
            optionsToolStripMenuItem.Size = new Size(61, 20);
            optionsToolStripMenuItem.Text = "&Options";
            // 
            // darkModeToolStripMenuItem
            // 
            darkModeToolStripMenuItem.CheckOnClick = true;
            darkModeToolStripMenuItem.Name = "darkModeToolStripMenuItem";
            darkModeToolStripMenuItem.Size = new Size(240, 22);
            darkModeToolStripMenuItem.Text = "&Dark Mode";
            darkModeToolStripMenuItem.ToolTipText = "Toggle between light and dark visual themes for the application.";
            darkModeToolStripMenuItem.Click += darkModeToolStripMenuItem_Click;
            // 
            // toolStripSeparator7
            // 
            toolStripSeparator7.Name = "toolStripSeparator7";
            toolStripSeparator7.Size = new Size(237, 6);
            // 
            // enable1ClickProcessingToolStripMenuItem
            // 
            enable1ClickProcessingToolStripMenuItem.CheckOnClick = true;
            enable1ClickProcessingToolStripMenuItem.Name = "enable1ClickProcessingToolStripMenuItem";
            enable1ClickProcessingToolStripMenuItem.Size = new Size(240, 22);
            enable1ClickProcessingToolStripMenuItem.Text = "Enable &1-Click Processing";
            enable1ClickProcessingToolStripMenuItem.ToolTipText = "Toggle between 2-button and 1-button processing mode.";
            // 
            // toolStripSeparator6
            // 
            toolStripSeparator6.Name = "toolStripSeparator6";
            toolStripSeparator6.Size = new Size(237, 6);
            // 
            // setAutoRunHourToolStripMenuItem
            // 
            setAutoRunHourToolStripMenuItem.Name = "setAutoRunHourToolStripMenuItem";
            setAutoRunHourToolStripMenuItem.Size = new Size(240, 22);
            setAutoRunHourToolStripMenuItem.Text = "Set Auto-Run &Hour...";
            setAutoRunHourToolStripMenuItem.ToolTipText = "Change the hour at which the daily auto-run task executes.";
            // 
            // toolStripSeparator3
            // 
            toolStripSeparator3.Name = "toolStripSeparator3";
            toolStripSeparator3.Size = new Size(237, 6);
            // 
            // viewConfigToolStripMenuItem
            // 
            viewConfigToolStripMenuItem.Name = "viewConfigToolStripMenuItem";
            viewConfigToolStripMenuItem.Size = new Size(240, 22);
            viewConfigToolStripMenuItem.Text = "&View Configuration";
            viewConfigToolStripMenuItem.ToolTipText = "Show detailed status of configuration settings like file paths.";
            viewConfigToolStripMenuItem.Click += viewConfigToolStripMenuItem_Click;
            // 
            // validateConfigToolStripMenuItem
            // 
            validateConfigToolStripMenuItem.Name = "validateConfigToolStripMenuItem";
            validateConfigToolStripMenuItem.Size = new Size(240, 22);
            validateConfigToolStripMenuItem.Text = "V&alidate Configuration";
            validateConfigToolStripMenuItem.ToolTipText = "Quickly validate essential configuration and update status bar.";
            validateConfigToolStripMenuItem.Click += validateConfigToolStripMenuItem_Click;
            // 
            // toolStripSeparator4
            // 
            toolStripSeparator4.Name = "toolStripSeparator4";
            toolStripSeparator4.Size = new Size(237, 6);
            // 
            // manageCustomBankHolidaysToolStripMenuItem
            // 
            manageCustomBankHolidaysToolStripMenuItem.Name = "manageCustomBankHolidaysToolStripMenuItem";
            manageCustomBankHolidaysToolStripMenuItem.Size = new Size(240, 22);
            manageCustomBankHolidaysToolStripMenuItem.Text = "Manage Custom &Bank Holidays";
            manageCustomBankHolidaysToolStripMenuItem.ToolTipText = "Add or remove custom bank holidays.";
            manageCustomBankHolidaysToolStripMenuItem.Click += manageCustomBankHolidaysToolStripMenuItem_Click;
            // 
            // manageEmailRecipientsToolStripMenuItem
            // 
            manageEmailRecipientsToolStripMenuItem.Name = "manageEmailRecipientsToolStripMenuItem";
            manageEmailRecipientsToolStripMenuItem.Size = new Size(240, 22);
            manageEmailRecipientsToolStripMenuItem.Text = "Manage Email &Recipients";
            manageEmailRecipientsToolStripMenuItem.ToolTipText = "Configure custom email recipients for different report types.";
            manageEmailRecipientsToolStripMenuItem.Click += manageEmailRecipientsToolStripMenuItem_Click;
            // 
            // toolStripSeparator5
            // 
            toolStripSeparator5.Name = "toolStripSeparator5";
            toolStripSeparator5.Size = new Size(237, 6);
            // 
            // openLogsToolStripMenuItem
            // 
            openLogsToolStripMenuItem.Name = "openLogsToolStripMenuItem";
            openLogsToolStripMenuItem.Size = new Size(240, 22);
            openLogsToolStripMenuItem.Text = "Open &Logs Folder";
            openLogsToolStripMenuItem.ToolTipText = "Open the folder containing application log files.";
            openLogsToolStripMenuItem.Click += openLogsToolStripMenuItem_Click;
            // 
            // toolStripSeparator1
            // 
            toolStripSeparator1.Name = "toolStripSeparator1";
            toolStripSeparator1.Size = new Size(237, 6);
            // 
            // editConfigToolStripMenuItem
            // 
            editConfigToolStripMenuItem.Name = "editConfigToolStripMenuItem";
            editConfigToolStripMenuItem.Size = new Size(240, 22);
            editConfigToolStripMenuItem.Text = "&Edit appsettings.json";
            editConfigToolStripMenuItem.ToolTipText = "Open the appsettings.json file for manual editing (use with caution).";
            editConfigToolStripMenuItem.Click += editConfigToolStripMenuItem_Click;
            // 
            // toolStripSeparator2
            // 
            toolStripSeparator2.Name = "toolStripSeparator2";
            toolStripSeparator2.Size = new Size(237, 6);
            // 
            // exitToolStripMenuItem
            // 
            exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            exitToolStripMenuItem.Size = new Size(240, 22);
            exitToolStripMenuItem.Text = "E&xit";
            exitToolStripMenuItem.ToolTipText = "Close the application.";
            exitToolStripMenuItem.Click += exitToolStripMenuItem_Click;
            // 
            // helpToolStripMenuItem
            // 
            helpToolStripMenuItem.Name = "helpToolStripMenuItem";
            helpToolStripMenuItem.Size = new Size(44, 20);
            helpToolStripMenuItem.Text = "&Help";
            helpToolStripMenuItem.ToolTipText = "Show the help window with instructions and troubleshooting tips.";
            helpToolStripMenuItem.Click += helpToolStripMenuItem_Click;
            // 
            // toolTip1
            // 
            toolTip1.AutomaticDelay = 700;
            toolTip1.AutoPopDelay = 7000;
            toolTip1.InitialDelay = 500;
            toolTip1.ReshowDelay = 140;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(6F, 13F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = SystemColors.ControlLightLight;
            ClientSize = new Size(635, 459);
            Controls.Add(oneClickProcessButton);
            Controls.Add(toggleAutoRunButton);
            Controls.Add(reportTypeLabel);
            Controls.Add(reportTypeComboBox);
            Controls.Add(mainStatusStrip);
            Controls.Add(menuStrip1);
            Controls.Add(viewAnalysisButton);
            Controls.Add(viewReportButton);
            Controls.Add(generateAndSendButton);
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
        private System.Windows.Forms.Button generateAndSendButton;
        private System.Windows.Forms.Button oneClickProcessButton; // New field
        private System.Windows.Forms.Button viewReportButton;
        private System.Windows.Forms.Button viewAnalysisButton;
        private System.Windows.Forms.ToolStripStatusLabel statusLabel;
        private System.Windows.Forms.StatusStrip mainStatusStrip;
        private System.Windows.Forms.CheckBox sendToFemiOnlyCheckBox;
        private System.Windows.Forms.CheckBox skipEmailCheckBox; // New field
        private System.Windows.Forms.ComboBox reportTypeComboBox;
        private System.Windows.Forms.Label reportTypeLabel;
        private System.Windows.Forms.GroupBox reportSettingsGroupBox;
        private System.Windows.Forms.Label financialYearLabel;
        private System.Windows.Forms.ComboBox financialYearComboBox;
        private System.Windows.Forms.Label emailRecipientLabel;
        private System.Windows.Forms.Button toggleAutoRunButton;
        private System.Windows.Forms.Timer dailyCheckTimer;
        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem optionsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem darkModeToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem helpToolStripMenuItem;
        private System.Windows.Forms.ToolStripStatusLabel autoRunStatusLabel;
        private System.Windows.Forms.ToolTip toolTip1;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripMenuItem viewConfigToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem openLogsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem editConfigToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem validateConfigToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
        private System.Windows.Forms.ToolStripMenuItem exitToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem manageCustomBankHolidaysToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator3;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator4;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator5;
        private System.Windows.Forms.ToolStripMenuItem manageEmailRecipientsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem enable1ClickProcessingToolStripMenuItem; // New field
        private System.Windows.Forms.ToolStripMenuItem setAutoRunHourToolStripMenuItem; // New field
        private ToolStripSeparator toolStripSeparator7;
        private ToolStripSeparator toolStripSeparator6;
    }
}
