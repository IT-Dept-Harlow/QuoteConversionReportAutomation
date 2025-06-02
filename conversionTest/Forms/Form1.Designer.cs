// Form1.Designer.cs (Menu Fix & Added Settings Menu Item)
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
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            this.startDatePicker = new System.Windows.Forms.DateTimePicker();
            this.endDatePicker = new System.Windows.Forms.DateTimePicker();
            this.startDateLabel = new System.Windows.Forms.Label();
            this.endDateLabel = new System.Windows.Forms.Label();
            this.createReportButton = new System.Windows.Forms.Button();
            this.processEmailButton = new System.Windows.Forms.Button();
            this.oneClickProcessButton = new System.Windows.Forms.Button();
            this.viewReportButton = new System.Windows.Forms.Button();
            this.viewAnalysisButton = new System.Windows.Forms.Button();
            this.statusLabel = new System.Windows.Forms.ToolStripStatusLabel();
            this.mainStatusStrip = new System.Windows.Forms.StatusStrip();
            this.autoRunStatusLabel = new System.Windows.Forms.ToolStripStatusLabel();
            this.sendToFemiOnlyCheckBox = new System.Windows.Forms.CheckBox();
            this.skipEmailCheckBox = new System.Windows.Forms.CheckBox();
            this.reportTypeComboBox = new System.Windows.Forms.ComboBox();
            this.reportTypeLabel = new System.Windows.Forms.Label();
            this.reportSettingsGroupBox = new System.Windows.Forms.GroupBox();
            this.emailRecipientLabel = new System.Windows.Forms.Label();
            this.financialYearLabel = new System.Windows.Forms.Label();
            this.financialYearComboBox = new System.Windows.Forms.ComboBox();
            this.toggleAutoRunButton = new System.Windows.Forms.Button();
            this.dailyCheckTimer = new System.Windows.Forms.Timer(this.components);
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.optionsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.darkModeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator7 = new System.Windows.Forms.ToolStripSeparator();
            this.enable1ClickProcessingToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator6 = new System.Windows.Forms.ToolStripSeparator();
            this.setAutoRunHourToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.manageAutomatedReportsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator8 = new System.Windows.Forms.ToolStripSeparator();
            this.viewConfigToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.validateConfigToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator4 = new System.Windows.Forms.ToolStripSeparator();
            this.manageCustomBankHolidaysToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator3 = new System.Windows.Forms.ToolStripSeparator();
            this.manageEmailRecipientsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.manageGreetingsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator5 = new System.Windows.Forms.ToolStripSeparator();
            this.openLogsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.openAutoReportDefinitionsFileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.editConfigToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            this.exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.settingsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem(); // <<< NEWLY DECLARED (moved declaration below)
            this.helpToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
            this.mainStatusStrip.SuspendLayout();
            this.reportSettingsGroupBox.SuspendLayout();
            this.menuStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // startDatePicker
            // 
            this.startDatePicker.Location = new System.Drawing.Point(261, 103);
            this.startDatePicker.Name = "startDatePicker";
            this.startDatePicker.Size = new System.Drawing.Size(200, 22);
            this.startDatePicker.TabIndex = 0;
            this.toolTip1.SetToolTip(this.startDatePicker, "Select the start date for the report period. Modifying this will set the Report " +
        "Type to \'Custom\'.");
            this.startDatePicker.ValueChanged += new System.EventHandler(this.DatePicker_ValueChanged);
            // 
            // endDatePicker
            // 
            this.endDatePicker.Location = new System.Drawing.Point(261, 135);
            this.endDatePicker.Name = "endDatePicker";
            this.endDatePicker.Size = new System.Drawing.Size(200, 22);
            this.endDatePicker.TabIndex = 1;
            this.toolTip1.SetToolTip(this.endDatePicker, "Select the end date for the report period. Modifying this will set the Report Typ" +
        "e to \'Custom\'.");
            this.endDatePicker.ValueChanged += new System.EventHandler(this.DatePicker_ValueChanged);
            // 
            // startDateLabel
            // 
            this.startDateLabel.AutoSize = true;
            this.startDateLabel.Location = new System.Drawing.Point(157, 109);
            this.startDateLabel.Name = "startDateLabel";
            this.startDateLabel.Size = new System.Drawing.Size(93, 13);
            this.startDateLabel.TabIndex = 2;
            this.startDateLabel.Text = "Enter From Date:";
            // 
            // endDateLabel
            // 
            this.endDateLabel.AutoSize = true;
            this.endDateLabel.Location = new System.Drawing.Point(157, 141);
            this.endDateLabel.Name = "endDateLabel";
            this.endDateLabel.Size = new System.Drawing.Size(79, 13);
            this.endDateLabel.TabIndex = 3;
            this.endDateLabel.Text = "Enter To Date:";
            // 
            // createReportButton
            // 
            this.createReportButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.createReportButton.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Bold);
            this.createReportButton.Location = new System.Drawing.Point(142, 260);
            this.createReportButton.Name = "createReportButton";
            this.createReportButton.Size = new System.Drawing.Size(130, 71);
            this.createReportButton.TabIndex = 5;
            this.createReportButton.Text = "Create Report";
            this.toolTip1.SetToolTip(this.createReportButton, "Click to generate the raw Crystal Report based on the selected dates and report t" +
        "ype.");
            this.createReportButton.UseVisualStyleBackColor = true;
            this.createReportButton.Click += new System.EventHandler(this.createReportButton_Click);
            // 
            // processEmailButton
            // 
            this.processEmailButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.processEmailButton.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Bold);
            this.processEmailButton.Location = new System.Drawing.Point(358, 260);
            this.processEmailButton.Name = "processEmailButton";
            this.processEmailButton.Size = new System.Drawing.Size(130, 71);
            this.processEmailButton.TabIndex = 6;
            this.processEmailButton.Text = "Create Analysis &\r\nSend Email";
            this.toolTip1.SetToolTip(this.processEmailButton, "Click to process the generated raw report, create the final analysis, and email " +
        "it.");
            this.processEmailButton.UseVisualStyleBackColor = true;
            this.processEmailButton.Click += new System.EventHandler(this.processEmailButton_Click);
            // 
            // oneClickProcessButton
            // 
            this.oneClickProcessButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.oneClickProcessButton.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.oneClickProcessButton.Location = new System.Drawing.Point(220, 260);
            this.oneClickProcessButton.Name = "oneClickProcessButton";
            this.oneClickProcessButton.Size = new System.Drawing.Size(200, 71);
            this.oneClickProcessButton.TabIndex = 20;
            this.oneClickProcessButton.Text = "Generate, Process && Email Report";
            this.toolTip1.SetToolTip(this.oneClickProcessButton, "Performs all steps: generates the raw report, processes it into the final analysi" +
        "s, and emails it (unless skipped).");
            this.oneClickProcessButton.UseVisualStyleBackColor = true;
            this.oneClickProcessButton.Click += new System.EventHandler(this.oneClickProcessButton_Click);
            // 
            // viewReportButton
            // 
            this.viewReportButton.AutoSize = true;
            this.viewReportButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.viewReportButton.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Bold);
            this.viewReportButton.Location = new System.Drawing.Point(165, 339);
            this.viewReportButton.Name = "viewReportButton";
            this.viewReportButton.Size = new System.Drawing.Size(92, 23);
            this.viewReportButton.TabIndex = 8;
            this.viewReportButton.Text = "View Raw File";
            this.toolTip1.SetToolTip(this.viewReportButton, "Click to open the generated raw report file.");
            this.viewReportButton.UseVisualStyleBackColor = true;
            this.viewReportButton.Click += new System.EventHandler(this.viewReportButton_Click);
            // 
            // viewAnalysisButton
            // 
            this.viewAnalysisButton.AutoSize = true;
            this.viewAnalysisButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.viewAnalysisButton.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Bold);
            this.viewAnalysisButton.Location = new System.Drawing.Point(355, 339);
            this.viewAnalysisButton.Name = "viewAnalysisButton";
            this.viewAnalysisButton.Size = new System.Drawing.Size(122, 23);
            this.viewAnalysisButton.TabIndex = 9;
            this.viewAnalysisButton.Text = "View Processed File";
            this.toolTip1.SetToolTip(this.viewAnalysisButton, "Click to open the final processed analysis file.");
            this.viewAnalysisButton.UseVisualStyleBackColor = true;
            this.viewAnalysisButton.Click += new System.EventHandler(this.viewAnalysisButton_Click);
            // 
            // statusLabel
            // 
            this.statusLabel.Name = "statusLabel";
            this.statusLabel.Size = new System.Drawing.Size(512, 17);
            this.statusLabel.Spring = true;
            this.statusLabel.Text = "Ready";
            this.statusLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // mainStatusStrip
            // 
            this.mainStatusStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.statusLabel,
            this.autoRunStatusLabel});
            this.mainStatusStrip.Location = new System.Drawing.Point(0, 437);
            this.mainStatusStrip.Name = "mainStatusStrip";
            this.mainStatusStrip.Size = new System.Drawing.Size(635, 22);
            this.mainStatusStrip.TabIndex = 10;
            this.mainStatusStrip.Text = "mainStatusStrip";
            // 
            // autoRunStatusLabel
            // 
            this.autoRunStatusLabel.Name = "autoRunStatusLabel";
            this.autoRunStatusLabel.Size = new System.Drawing.Size(108, 17);
            this.autoRunStatusLabel.Text = "Auto Run: Disabled";
            this.autoRunStatusLabel.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // sendToFemiOnlyCheckBox
            // 
            this.sendToFemiOnlyCheckBox.AutoSize = true;
            this.sendToFemiOnlyCheckBox.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.sendToFemiOnlyCheckBox.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.sendToFemiOnlyCheckBox.Location = new System.Drawing.Point(119, 147); // Inside GroupBox
            this.sendToFemiOnlyCheckBox.Name = "sendToFemiOnlyCheckBox";
            this.sendToFemiOnlyCheckBox.Size = new System.Drawing.Size(142, 21);
            this.sendToFemiOnlyCheckBox.TabIndex = 11; // Adjust TabIndex within GroupBox
            this.sendToFemiOnlyCheckBox.Text = "Send to only Femi?";
            this.toolTip1.SetToolTip(this.sendToFemiOnlyCheckBox, "Check this to send the email report only to Femi (and relevant CCs based on build" +
        " mode). Uncheck to send to the broader team.");
            this.sendToFemiOnlyCheckBox.UseVisualStyleBackColor = true;
            // 
            // skipEmailCheckBox
            // 
            this.skipEmailCheckBox.AutoSize = true;
            this.skipEmailCheckBox.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.skipEmailCheckBox.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.skipEmailCheckBox.Location = new System.Drawing.Point(15, 168); // Inside GroupBox
            this.skipEmailCheckBox.Name = "skipEmailCheckBox";
            this.skipEmailCheckBox.Size = new System.Drawing.Size(130, 18);
            this.skipEmailCheckBox.TabIndex = 21; // Adjust TabIndex within GroupBox
            this.skipEmailCheckBox.Text = "Skip Sending Email";
            this.toolTip1.SetToolTip(this.skipEmailCheckBox, "If checked, the email sending step will be skipped during processing.");
            this.skipEmailCheckBox.UseVisualStyleBackColor = true;
            // 
            // reportTypeComboBox
            // 
            this.reportTypeComboBox.AutoCompleteCustomSource.AddRange(new string[] {
            "Weekly",
            "Monthly",
            "Quarterly (3 Months)",
            "Annual"});
            this.reportTypeComboBox.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.SuggestAppend;
            this.reportTypeComboBox.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.CustomSource;
            this.reportTypeComboBox.FormattingEnabled = true;
            this.reportTypeComboBox.Items.AddRange(new object[] {
            "Daily",
            "Daily(5days >= £1000)",
            "Weekly",
            "Monthly",
            "Quarterly (3 Months)",
            "Annual",
            "Custom"});
            this.reportTypeComboBox.Location = new System.Drawing.Point(261, 72);
            this.reportTypeComboBox.Name = "reportTypeComboBox";
            this.reportTypeComboBox.Size = new System.Drawing.Size(200, 21);
            this.reportTypeComboBox.TabIndex = 12;
            this.toolTip1.SetToolTip(this.reportTypeComboBox, "Select the type of report to generate (Daily, Weekly, etc.). Dates will adjust a" +
        "utomatically based on the current date. Manual date changes will set this to \'Cu" +
        "stom\'.");
            this.reportTypeComboBox.SelectedIndexChanged += new System.EventHandler(this.reportTypeComboBox_SelectedIndexChanged);
            // 
            // reportTypeLabel
            // 
            this.reportTypeLabel.AutoSize = true;
            this.reportTypeLabel.Location = new System.Drawing.Point(157, 75);
            this.reportTypeLabel.Name = "reportTypeLabel";
            this.reportTypeLabel.Size = new System.Drawing.Size(71, 13);
            this.reportTypeLabel.TabIndex = 13;
            this.reportTypeLabel.Text = "Report Type:";
            // 
            // reportSettingsGroupBox
            // 
            this.reportSettingsGroupBox.Controls.Add(this.skipEmailCheckBox);
            this.reportSettingsGroupBox.Controls.Add(this.emailRecipientLabel);
            this.reportSettingsGroupBox.Controls.Add(this.financialYearLabel);
            this.reportSettingsGroupBox.Controls.Add(this.sendToFemiOnlyCheckBox);
            this.reportSettingsGroupBox.Controls.Add(this.financialYearComboBox);
            this.reportSettingsGroupBox.Font = new System.Drawing.Font("Arial", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.reportSettingsGroupBox.Location = new System.Drawing.Point(142, 50);
            this.reportSettingsGroupBox.Name = "reportSettingsGroupBox";
            this.reportSettingsGroupBox.Size = new System.Drawing.Size(346, 200);
            this.reportSettingsGroupBox.TabIndex = 14;
            this.reportSettingsGroupBox.TabStop = false;
            this.reportSettingsGroupBox.Text = "Report Settings";
            // 
            // emailRecipientLabel
            // 
            this.emailRecipientLabel.AutoSize = true;
            this.emailRecipientLabel.Font = new System.Drawing.Font("Arial", 9.75F, System.Drawing.FontStyle.Bold);
            this.emailRecipientLabel.Location = new System.Drawing.Point(119, 147);
            this.emailRecipientLabel.Name = "emailRecipientLabel";
            this.emailRecipientLabel.Size = new System.Drawing.Size(0, 16);
            this.emailRecipientLabel.TabIndex = 17;
            // 
            // financialYearLabel
            // 
            this.financialYearLabel.AutoSize = true;
            this.financialYearLabel.Location = new System.Drawing.Point(15, 117);
            this.financialYearLabel.Name = "financialYearLabel";
            this.financialYearLabel.Size = new System.Drawing.Size(78, 14);
            this.financialYearLabel.TabIndex = 16;
            this.financialYearLabel.Text = "Financial Year:";
            // 
            // financialYearComboBox
            // 
            this.financialYearComboBox.AutoCompleteCustomSource.AddRange(new string[] {
            "Daily",
            "Weekly",
            "Monthly",
            "Quarterly (3 Months)",
            "Annual"});
            this.financialYearComboBox.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.SuggestAppend;
            this.financialYearComboBox.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.CustomSource;
            this.financialYearComboBox.FormattingEnabled = true;
            this.financialYearComboBox.Location = new System.Drawing.Point(119, 114);
            this.financialYearComboBox.Name = "financialYearComboBox";
            this.financialYearComboBox.Size = new System.Drawing.Size(200, 22);
            this.financialYearComboBox.TabIndex = 15;
            this.toolTip1.SetToolTip(this.financialYearComboBox, "Select the financial year for the report. Only applicable for certain report type" +
        "s.");
            // 
            // toggleAutoRunButton
            // 
            this.toggleAutoRunButton.Location = new System.Drawing.Point(12, 359);
            this.toggleAutoRunButton.Name = "toggleAutoRunButton";
            this.toggleAutoRunButton.Size = new System.Drawing.Size(107, 54);
            this.toggleAutoRunButton.TabIndex = 16;
            this.toggleAutoRunButton.Text = "Enable Daily Auto Run @ 8 AM";
            this.toolTip1.SetToolTip(this.toggleAutoRunButton, "Enable or disable the automated daily report generation. The report runs around 8" +
        " AM for the previous workday.");
            this.toggleAutoRunButton.UseVisualStyleBackColor = true;
            this.toggleAutoRunButton.Click += new System.EventHandler(this.toggleAutoRunButton_Click);
            // 
            // dailyCheckTimer
            // 
            this.dailyCheckTimer.Interval = 60000;
            this.dailyCheckTimer.Tick += new System.EventHandler(this.dailyCheckTimer_Tick);
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.optionsToolStripMenuItem,
            this.settingsToolStripMenuItem, // <<< NEWLY ADDED TOP-LEVEL MENU ITEM
            this.helpToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(635, 24);
            this.menuStrip1.TabIndex = 18;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // optionsToolStripMenuItem
            // 
            this.optionsToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.darkModeToolStripMenuItem,
            this.toolStripSeparator7,
            this.enable1ClickProcessingToolStripMenuItem,
            this.toolStripSeparator6,
            this.setAutoRunHourToolStripMenuItem,
            this.manageAutomatedReportsToolStripMenuItem,
            this.toolStripSeparator8,
            this.viewConfigToolStripMenuItem,
            this.validateConfigToolStripMenuItem,
            this.toolStripSeparator4,
            this.manageCustomBankHolidaysToolStripMenuItem,
            this.toolStripSeparator3,
            this.manageEmailRecipientsToolStripMenuItem,
            this.manageGreetingsToolStripMenuItem,
            this.toolStripSeparator5,
            this.openLogsToolStripMenuItem,
            this.openAutoReportDefinitionsFileToolStripMenuItem,
            this.toolStripSeparator1,
            this.editConfigToolStripMenuItem,
            this.toolStripSeparator2,
            this.exitToolStripMenuItem});
            this.optionsToolStripMenuItem.Name = "optionsToolStripMenuItem";
            this.optionsToolStripMenuItem.Size = new System.Drawing.Size(61, 20);
            this.optionsToolStripMenuItem.Text = "&Options";
            // 
            // darkModeToolStripMenuItem
            // 
            this.darkModeToolStripMenuItem.CheckOnClick = true;
            this.darkModeToolStripMenuItem.Name = "darkModeToolStripMenuItem";
            this.darkModeToolStripMenuItem.Size = new System.Drawing.Size(258, 22);
            this.darkModeToolStripMenuItem.Text = "&Dark Mode";
            this.darkModeToolStripMenuItem.ToolTipText = "Toggle between light and dark visual themes for the application.";
            this.darkModeToolStripMenuItem.Click += new System.EventHandler(this.darkModeToolStripMenuItem_Click);
            // 
            // toolStripSeparator7
            // 
            this.toolStripSeparator7.Name = "toolStripSeparator7";
            this.toolStripSeparator7.Size = new System.Drawing.Size(255, 6);
            // 
            // enable1ClickProcessingToolStripMenuItem
            // 
            this.enable1ClickProcessingToolStripMenuItem.CheckOnClick = true;
            this.enable1ClickProcessingToolStripMenuItem.Name = "enable1ClickProcessingToolStripMenuItem";
            this.enable1ClickProcessingToolStripMenuItem.Size = new System.Drawing.Size(258, 22);
            this.enable1ClickProcessingToolStripMenuItem.Text = "Enable &1-Click Processing";
            this.enable1ClickProcessingToolStripMenuItem.ToolTipText = "Toggle between 2-button and 1-button processing mode.";
            this.enable1ClickProcessingToolStripMenuItem.Click += new System.EventHandler(this.enable1ClickProcessingToolStripMenuItem_Click);
            // 
            // toolStripSeparator6
            // 
            this.toolStripSeparator6.Name = "toolStripSeparator6";
            this.toolStripSeparator6.Size = new System.Drawing.Size(255, 6);
            // 
            // setAutoRunHourToolStripMenuItem
            // 
            this.setAutoRunHourToolStripMenuItem.Name = "setAutoRunHourToolStripMenuItem";
            this.setAutoRunHourToolStripMenuItem.Size = new System.Drawing.Size(258, 22);
            this.setAutoRunHourToolStripMenuItem.Text = "Set Auto-Run &Hour...";
            this.setAutoRunHourToolStripMenuItem.ToolTipText = "Change the hour at which the daily auto-run task executes.";
            this.setAutoRunHourToolStripMenuItem.Click += new System.EventHandler(this.setAutoRunHourToolStripMenuItem_Click);
            // 
            // manageAutomatedReportsToolStripMenuItem
            // 
            this.manageAutomatedReportsToolStripMenuItem.Name = "manageAutomatedReportsToolStripMenuItem";
            this.manageAutomatedReportsToolStripMenuItem.Size = new System.Drawing.Size(258, 22);
            this.manageAutomatedReportsToolStripMenuItem.Text = "Manage Automated Reports...";
            this.manageAutomatedReportsToolStripMenuItem.ToolTipText = "Configure, add, or remove automated report definitions.";
            this.manageAutomatedReportsToolStripMenuItem.Click += new System.EventHandler(this.manageAutomatedReportsToolStripMenuItem_Click);
            // 
            // toolStripSeparator8
            // 
            this.toolStripSeparator8.Name = "toolStripSeparator8";
            this.toolStripSeparator8.Size = new System.Drawing.Size(255, 6);
            // 
            // viewConfigToolStripMenuItem
            // 
            this.viewConfigToolStripMenuItem.Name = "viewConfigToolStripMenuItem";
            this.viewConfigToolStripMenuItem.Size = new System.Drawing.Size(258, 22);
            this.viewConfigToolStripMenuItem.Text = "&View Configuration";
            this.viewConfigToolStripMenuItem.ToolTipText = "Show detailed status of configuration settings like file paths.";
            this.viewConfigToolStripMenuItem.Click += new System.EventHandler(this.viewConfigToolStripMenuItem_Click);
            // 
            // validateConfigToolStripMenuItem
            // 
            this.validateConfigToolStripMenuItem.Name = "validateConfigToolStripMenuItem";
            this.validateConfigToolStripMenuItem.Size = new System.Drawing.Size(258, 22);
            this.validateConfigToolStripMenuItem.Text = "V&alidate Configuration";
            this.validateConfigToolStripMenuItem.ToolTipText = "Quickly validate essential configuration and update status bar.";
            this.validateConfigToolStripMenuItem.Click += new System.EventHandler(this.validateConfigToolStripMenuItem_Click);
            // 
            // toolStripSeparator4
            // 
            this.toolStripSeparator4.Name = "toolStripSeparator4";
            this.toolStripSeparator4.Size = new System.Drawing.Size(255, 6);
            // 
            // manageCustomBankHolidaysToolStripMenuItem
            // 
            this.manageCustomBankHolidaysToolStripMenuItem.Name = "manageCustomBankHolidaysToolStripMenuItem";
            this.manageCustomBankHolidaysToolStripMenuItem.Size = new System.Drawing.Size(258, 22);
            this.manageCustomBankHolidaysToolStripMenuItem.Text = "Manage Custom &Bank Holidays";
            this.manageCustomBankHolidaysToolStripMenuItem.ToolTipText = "Add or remove custom bank holidays.";
            this.manageCustomBankHolidaysToolStripMenuItem.Click += new System.EventHandler(this.manageCustomBankHolidaysToolStripMenuItem_Click);
            // 
            // toolStripSeparator3
            // 
            this.toolStripSeparator3.Name = "toolStripSeparator3";
            this.toolStripSeparator3.Size = new System.Drawing.Size(255, 6);
            // 
            // manageEmailRecipientsToolStripMenuItem
            // 
            this.manageEmailRecipientsToolStripMenuItem.Name = "manageEmailRecipientsToolStripMenuItem";
            this.manageEmailRecipientsToolStripMenuItem.Size = new System.Drawing.Size(258, 22);
            this.manageEmailRecipientsToolStripMenuItem.Text = "Manage Email &Recipients";
            this.manageEmailRecipientsToolStripMenuItem.ToolTipText = "Configure custom email recipients for different report types.";
            this.manageEmailRecipientsToolStripMenuItem.Click += new System.EventHandler(this.manageEmailRecipientsToolStripMenuItem_Click);
            // 
            // manageGreetingsToolStripMenuItem
            // 
            this.manageGreetingsToolStripMenuItem.Name = "manageGreetingsToolStripMenuItem";
            this.manageGreetingsToolStripMenuItem.Size = new System.Drawing.Size(258, 22);
            this.manageGreetingsToolStripMenuItem.Text = "Manage Email &Greetings";
            this.manageGreetingsToolStripMenuItem.ToolTipText = "Configure custom email greetings for different report scenarios.";
            this.manageGreetingsToolStripMenuItem.Click += new System.EventHandler(this.manageGreetingsToolStripMenuItem_Click);
            // 
            // toolStripSeparator5
            // 
            this.toolStripSeparator5.Name = "toolStripSeparator5";
            this.toolStripSeparator5.Size = new System.Drawing.Size(255, 6);
            // 
            // openLogsToolStripMenuItem
            // 
            this.openLogsToolStripMenuItem.Name = "openLogsToolStripMenuItem";
            this.openLogsToolStripMenuItem.Size = new System.Drawing.Size(258, 22);
            this.openLogsToolStripMenuItem.Text = "Open &Logs Folder";
            this.openLogsToolStripMenuItem.ToolTipText = "Open the folder containing application log files.";
            this.openLogsToolStripMenuItem.Click += new System.EventHandler(this.openLogsToolStripMenuItem_Click);
            // 
            // openAutoReportDefinitionsFileToolStripMenuItem
            // 
            this.openAutoReportDefinitionsFileToolStripMenuItem.Name = "openAutoReportDefinitionsFileToolStripMenuItem";
            this.openAutoReportDefinitionsFileToolStripMenuItem.Size = new System.Drawing.Size(258, 22);
            this.openAutoReportDefinitionsFileToolStripMenuItem.Text = "Open Auto Report Definitions File";
            this.openAutoReportDefinitionsFileToolStripMenuItem.ToolTipText = "Opens the autoReportDefinitions.json file for viewing.";
            this.openAutoReportDefinitionsFileToolStripMenuItem.Click += new System.EventHandler(this.openAutoReportDefinitionsFileToolStripMenuItem_Click);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(255, 6);
            // 
            // editConfigToolStripMenuItem
            // 
            this.editConfigToolStripMenuItem.Name = "editConfigToolStripMenuItem";
            this.editConfigToolStripMenuItem.Size = new System.Drawing.Size(258, 22);
            this.editConfigToolStripMenuItem.Text = "&Edit appsettings.json";
            this.editConfigToolStripMenuItem.ToolTipText = "Open the appsettings.json file for manual editing (use with caution).";
            this.editConfigToolStripMenuItem.Click += new System.EventHandler(this.editConfigToolStripMenuItem_Click);
            // 
            // toolStripSeparator2
            // 
            this.toolStripSeparator2.Name = "toolStripSeparator2";
            this.toolStripSeparator2.Size = new System.Drawing.Size(255, 6);
            // 
            // exitToolStripMenuItem
            // 
            this.exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            this.exitToolStripMenuItem.Size = new System.Drawing.Size(258, 22);
            this.exitToolStripMenuItem.Text = "E&xit";
            this.exitToolStripMenuItem.ToolTipText = "Close the application.";
            this.exitToolStripMenuItem.Click += new System.EventHandler(this.exitToolStripMenuItem_Click);
            //
            // settingsToolStripMenuItem  // <<< NEWLY ADDED/CONFIGURED
            //
            this.settingsToolStripMenuItem.Name = "settingsToolStripMenuItem";
            this.settingsToolStripMenuItem.Size = new System.Drawing.Size(61, 20); // Adjust size if needed
            this.settingsToolStripMenuItem.Text = "&Settings";
            this.settingsToolStripMenuItem.ToolTipText = "Configure application settings.";
            this.settingsToolStripMenuItem.Click += new System.EventHandler(this.settingsToolStripMenuItem_Click);
            // 
            // helpToolStripMenuItem
            // 
            this.helpToolStripMenuItem.Name = "helpToolStripMenuItem";
            this.helpToolStripMenuItem.Size = new System.Drawing.Size(44, 20);
            this.helpToolStripMenuItem.Text = "&Help";
            this.helpToolStripMenuItem.ToolTipText = "Show the help window with instructions and troubleshooting tips.";
            this.helpToolStripMenuItem.Click += new System.EventHandler(this.helpToolStripMenuItem_Click);
            // 
            // toolTip1
            // 
            this.toolTip1.AutomaticDelay = 700;
            this.toolTip1.AutoPopDelay = 7000;
            this.toolTip1.InitialDelay = 500;
            this.toolTip1.ReshowDelay = 140;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.ControlLightLight;
            this.ClientSize = new System.Drawing.Size(635, 459);
            this.Controls.Add(this.oneClickProcessButton);
            this.Controls.Add(this.toggleAutoRunButton);
            this.Controls.Add(this.reportTypeLabel);
            this.Controls.Add(this.reportTypeComboBox);
            this.Controls.Add(this.mainStatusStrip);
            this.Controls.Add(this.menuStrip1); // Must be before other controls that dock or anchor
            this.Controls.Add(this.viewAnalysisButton);
            this.Controls.Add(this.viewReportButton);
            this.Controls.Add(this.processEmailButton);
            this.Controls.Add(this.createReportButton);
            this.Controls.Add(this.endDateLabel);
            this.Controls.Add(this.startDateLabel);
            this.Controls.Add(this.endDatePicker);
            this.Controls.Add(this.startDatePicker);
            this.Controls.Add(this.reportSettingsGroupBox);
            this.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "Form1";
            this.Text = "Quote Conversion Automation"; // This will be updated from appsettings.json by Form1.cs logic
            this.Load += new System.EventHandler(this.Form1_Load);
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Form1_FormClosing);
            this.mainStatusStrip.ResumeLayout(false);
            this.mainStatusStrip.PerformLayout();
            this.reportSettingsGroupBox.ResumeLayout(false);
            this.reportSettingsGroupBox.PerformLayout();
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.DateTimePicker startDatePicker;
        private System.Windows.Forms.DateTimePicker endDatePicker;
        private System.Windows.Forms.Label startDateLabel;
        private System.Windows.Forms.Label endDateLabel;
        private System.Windows.Forms.Button createReportButton;
        private System.Windows.Forms.Button processEmailButton;
        private System.Windows.Forms.Button oneClickProcessButton;
        private System.Windows.Forms.Button viewReportButton;
        private System.Windows.Forms.Button viewAnalysisButton;
        private System.Windows.Forms.ToolStripStatusLabel statusLabel;
        private System.Windows.Forms.StatusStrip mainStatusStrip;
        private System.Windows.Forms.CheckBox sendToFemiOnlyCheckBox;
        private System.Windows.Forms.CheckBox skipEmailCheckBox;
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
        private System.Windows.Forms.ToolStripMenuItem manageGreetingsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem enable1ClickProcessingToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem setAutoRunHourToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator7;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator6;
        private System.Windows.Forms.ToolStripMenuItem manageAutomatedReportsToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator8;
        private System.Windows.Forms.ToolStripMenuItem openAutoReportDefinitionsFileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem settingsToolStripMenuItem; // <<< FIELD DECLARATION FOR NEW MENU ITEM
    }
}