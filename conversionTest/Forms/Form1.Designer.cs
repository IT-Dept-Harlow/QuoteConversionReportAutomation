// Form1.Designer.cs
// This is the final, corrected version incorporating a responsive layout,
// new menu items, and all UI control fixes.

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
            startDatePicker = new DateTimePicker();
            endDatePicker = new DateTimePicker();
            startDateLabel = new Label();
            endDateLabel = new Label();
            createReportButton = new Button();
            processEmailButton = new Button();
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
            chkIncludeLeadTimeAnalysis = new CheckBox();
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
            manageAutomatedReportsToolStripMenuItem = new ToolStripMenuItem();
            batchRegenerateToolStripMenuItem = new ToolStripMenuItem();
            retrospectiveAnalysisToolStripMenuItem = new ToolStripMenuItem();
            toolStripSeparator8 = new ToolStripSeparator();
            viewConfigToolStripMenuItem = new ToolStripMenuItem();
            validateConfigToolStripMenuItem = new ToolStripMenuItem();
            toolStripSeparator4 = new ToolStripSeparator();
            manageCustomBankHolidaysToolStripMenuItem = new ToolStripMenuItem();
            manageEmailRecipientsToolStripMenuItem = new ToolStripMenuItem();
            manageGreetingsToolStripMenuItem = new ToolStripMenuItem();
            manageTenderExclusionsToolStripMenuItem = new ToolStripMenuItem();
            toolStripSeparator3 = new ToolStripSeparator();
            toolStripSeparator5 = new ToolStripSeparator();
            openLogsToolStripMenuItem = new ToolStripMenuItem();
            openAutoReportDefinitionsFileToolStripMenuItem = new ToolStripMenuItem();
            toolStripSeparator1 = new ToolStripSeparator();
            editConfigToolStripMenuItem = new ToolStripMenuItem();
            toolStripSeparator2 = new ToolStripSeparator();
            exitToolStripMenuItem = new ToolStripMenuItem();
            settingsToolStripMenuItem = new ToolStripMenuItem();
            helpToolStripMenuItem = new ToolStripMenuItem();
            toolTip1 = new ToolTip(components);
            rootTableLayoutPanel = new TableLayoutPanel();
            contentPanel = new Panel();
            contentCenterLayout = new TableLayoutPanel();
            centerStackPanel = new TableLayoutPanel();
            reportTypePanel = new FlowLayoutPanel();
            actionButtonsPanel = new FlowLayoutPanel();
            viewButtonsPanel = new FlowLayoutPanel();
            mainStatusStrip.SuspendLayout();
            reportSettingsGroupBox.SuspendLayout();
            menuStrip1.SuspendLayout();
            rootTableLayoutPanel.SuspendLayout();
            contentPanel.SuspendLayout();
            contentCenterLayout.SuspendLayout();
            centerStackPanel.SuspendLayout();
            reportTypePanel.SuspendLayout();
            actionButtonsPanel.SuspendLayout();
            viewButtonsPanel.SuspendLayout();
            SuspendLayout();
            // 
            // startDatePicker
            // 
            startDatePicker.Location = new Point(223, 27);
            startDatePicker.Name = "startDatePicker";
            startDatePicker.Size = new Size(200, 22);
            startDatePicker.TabIndex = 0;
            toolTip1.SetToolTip(startDatePicker, "Select the start date for the report period. Modifying this will set the Report Type to 'Custom'.");
            startDatePicker.ValueChanged += DatePicker_ValueChanged;
            // 
            // endDatePicker
            // 
            endDatePicker.Location = new Point(223, 59);
            endDatePicker.Name = "endDatePicker";
            endDatePicker.Size = new Size(200, 22);
            endDatePicker.TabIndex = 1;
            toolTip1.SetToolTip(endDatePicker, "Select the end date for the report period. Modifying this will set the Report Type to 'Custom'.");
            endDatePicker.ValueChanged += DatePicker_ValueChanged;
            // 
            // startDateLabel
            // 
            startDateLabel.AutoSize = true;
            startDateLabel.Location = new Point(119, 33);
            startDateLabel.Name = "startDateLabel";
            startDateLabel.Size = new Size(93, 13);
            startDateLabel.TabIndex = 2;
            startDateLabel.Text = "Enter From Date:";
            // 
            // endDateLabel
            // 
            endDateLabel.AutoSize = true;
            endDateLabel.Location = new Point(119, 65);
            endDateLabel.Name = "endDateLabel";
            endDateLabel.Size = new Size(78, 13);
            endDateLabel.TabIndex = 3;
            endDateLabel.Text = "Enter To Date:";
            // 
            // createReportButton
            // 
            createReportButton.FlatStyle = FlatStyle.System;
            createReportButton.Font = new Font("Segoe UI", 8.25F, FontStyle.Bold);
            createReportButton.Location = new Point(209, 3);
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
            processEmailButton.Location = new Point(345, 3);
            processEmailButton.Name = "processEmailButton";
            processEmailButton.Size = new Size(110, 71);
            processEmailButton.TabIndex = 6;
            processEmailButton.Text = "Process &\r\nEmail";
            toolTip1.SetToolTip(processEmailButton, "Click to process the generated raw report, create the final analysis, and email it.");
            processEmailButton.UseVisualStyleBackColor = true;
            processEmailButton.Click += processEmailButton_Click;
            // 
            // oneClickProcessButton
            // 
            oneClickProcessButton.FlatStyle = FlatStyle.System;
            oneClickProcessButton.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
            oneClickProcessButton.Location = new Point(3, 3);
            oneClickProcessButton.Name = "oneClickProcessButton";
            oneClickProcessButton.Size = new Size(200, 71);
            oneClickProcessButton.TabIndex = 20;
            oneClickProcessButton.Text = "Generate, Process && Email Report";
            toolTip1.SetToolTip(oneClickProcessButton, "Performs all steps: generates the raw report, processes it into the final analysis, and emails it (unless skipped).");
            oneClickProcessButton.UseVisualStyleBackColor = true;
            oneClickProcessButton.Click += oneClickProcessButton_Click;
            // 
            // viewReportButton
            // 
            viewReportButton.AutoSize = true;
            viewReportButton.FlatStyle = FlatStyle.System;
            viewReportButton.Font = new Font("Segoe UI", 8.25F, FontStyle.Bold);
            viewReportButton.Location = new Point(3, 3);
            viewReportButton.Name = "viewReportButton";
            viewReportButton.Size = new Size(92, 23);
            viewReportButton.TabIndex = 8;
            viewReportButton.Text = "View Raw File";
            toolTip1.SetToolTip(viewReportButton, "Click to open the generated raw report file.");
            viewReportButton.UseVisualStyleBackColor = true;
            viewReportButton.Click += viewReportButton_Click;
            // 
            // viewAnalysisButton
            // 
            viewAnalysisButton.AutoSize = true;
            viewAnalysisButton.FlatStyle = FlatStyle.System;
            viewAnalysisButton.Font = new Font("Segoe UI", 8.25F, FontStyle.Bold);
            viewAnalysisButton.Location = new Point(101, 3);
            viewAnalysisButton.Name = "viewAnalysisButton";
            viewAnalysisButton.Size = new Size(122, 23);
            viewAnalysisButton.TabIndex = 9;
            viewAnalysisButton.Text = "View Processed File";
            toolTip1.SetToolTip(viewAnalysisButton, "Click to open the final processed analysis file.");
            viewAnalysisButton.UseVisualStyleBackColor = true;
            viewAnalysisButton.Click += viewAnalysisButton_Click;
            // 
            // statusLabel
            // 
            statusLabel.Name = "statusLabel";
            statusLabel.Size = new Size(661, 17);
            statusLabel.Spring = true;
            statusLabel.Text = "Ready";
            statusLabel.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // mainStatusStrip
            // 
            mainStatusStrip.Dock = DockStyle.Fill;
            mainStatusStrip.Items.AddRange(new ToolStripItem[] { statusLabel, autoRunStatusLabel });
            mainStatusStrip.Location = new Point(0, 539);
            mainStatusStrip.Name = "mainStatusStrip";
            mainStatusStrip.Size = new Size(784, 22);
            mainStatusStrip.TabIndex = 10;
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
            toolTip1.SetToolTip(sendToFemiOnlyCheckBox, "If checked, the report is sent to a restricted recipient list.");
            sendToFemiOnlyCheckBox.UseVisualStyleBackColor = true;
            // 
            // skipEmailCheckBox
            // 
            skipEmailCheckBox.AutoSize = true;
            skipEmailCheckBox.FlatStyle = FlatStyle.System;
            skipEmailCheckBox.Font = new Font("Segoe UI", 8.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
            skipEmailCheckBox.Location = new Point(15, 225);
            skipEmailCheckBox.Name = "skipEmailCheckBox";
            skipEmailCheckBox.Size = new Size(130, 18);
            skipEmailCheckBox.TabIndex = 21;
            skipEmailCheckBox.Text = "Skip Sending Email";
            toolTip1.SetToolTip(skipEmailCheckBox, "If checked, the email sending step will be skipped.");
            skipEmailCheckBox.UseVisualStyleBackColor = true;
            // 
            // reportTypeComboBox
            // 
            reportTypeComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            reportTypeComboBox.FormattingEnabled = true;
            reportTypeComboBox.Location = new Point(79, 3);
            reportTypeComboBox.Name = "reportTypeComboBox";
            reportTypeComboBox.Size = new Size(243, 21);
            reportTypeComboBox.TabIndex = 12;
            toolTip1.SetToolTip(reportTypeComboBox, "Select a predefined report type. Dates will adjust automatically. Changing dates manually sets this to 'Custom'.");
            reportTypeComboBox.SelectedIndexChanged += reportTypeComboBox_SelectedIndexChanged;
            // 
            // reportTypeLabel
            // 
            reportTypeLabel.Anchor = AnchorStyles.Left;
            reportTypeLabel.AutoSize = true;
            reportTypeLabel.Location = new Point(3, 7);
            reportTypeLabel.Name = "reportTypeLabel";
            reportTypeLabel.Size = new Size(70, 13);
            reportTypeLabel.TabIndex = 13;
            reportTypeLabel.Text = "Report Type:";
            // 
            // reportSettingsGroupBox
            // 
            reportSettingsGroupBox.Controls.Add(chkIncludeLeadTimeAnalysis);
            reportSettingsGroupBox.Controls.Add(startDatePicker);
            reportSettingsGroupBox.Controls.Add(endDatePicker);
            reportSettingsGroupBox.Controls.Add(startDateLabel);
            reportSettingsGroupBox.Controls.Add(endDateLabel);
            reportSettingsGroupBox.Controls.Add(skipEmailCheckBox);
            reportSettingsGroupBox.Controls.Add(emailRecipientLabel);
            reportSettingsGroupBox.Controls.Add(financialYearLabel);
            reportSettingsGroupBox.Controls.Add(sendToFemiOnlyCheckBox);
            reportSettingsGroupBox.Controls.Add(financialYearComboBox);
            reportSettingsGroupBox.Font = new Font("Segoe UI", 8.25F);
            reportSettingsGroupBox.Location = new Point(3, 46);
            reportSettingsGroupBox.Name = "reportSettingsGroupBox";
            reportSettingsGroupBox.Size = new Size(458, 263);
            reportSettingsGroupBox.TabIndex = 14;
            reportSettingsGroupBox.TabStop = false;
            reportSettingsGroupBox.Text = "Report Settings";
            // 
            // chkIncludeLeadTimeAnalysis
            // 
            chkIncludeLeadTimeAnalysis.AutoSize = true;
            chkIncludeLeadTimeAnalysis.FlatStyle = FlatStyle.System;
            chkIncludeLeadTimeAnalysis.Font = new Font("Segoe UI", 8.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
            chkIncludeLeadTimeAnalysis.Location = new Point(119, 185);
            chkIncludeLeadTimeAnalysis.Name = "chkIncludeLeadTimeAnalysis";
            chkIncludeLeadTimeAnalysis.Size = new Size(199, 18);
            chkIncludeLeadTimeAnalysis.TabIndex = 22;
            chkIncludeLeadTimeAnalysis.Text = "Include Lead Time Analysis Sheet";
            toolTip1.SetToolTip(chkIncludeLeadTimeAnalysis, "If checked, an extra sheet calculating the time from estimate to order will be added to the report.");
            chkIncludeLeadTimeAnalysis.UseVisualStyleBackColor = true;
            // 
            // emailRecipientLabel
            // 
            emailRecipientLabel.AutoSize = true;
            emailRecipientLabel.Font = new Font("Segoe UI", 9.75F, FontStyle.Bold);
            emailRecipientLabel.Location = new Point(119, 147);
            emailRecipientLabel.Name = "emailRecipientLabel";
            emailRecipientLabel.Size = new Size(0, 17);
            emailRecipientLabel.TabIndex = 17;
            // 
            // financialYearLabel
            // 
            financialYearLabel.AutoSize = true;
            financialYearLabel.Location = new Point(119, 93);
            financialYearLabel.Name = "financialYearLabel";
            financialYearLabel.Size = new Size(79, 13);
            financialYearLabel.TabIndex = 16;
            financialYearLabel.Text = "Financial Year:";
            // 
            // financialYearComboBox
            // 
            financialYearComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            financialYearComboBox.FormattingEnabled = true;
            financialYearComboBox.Location = new Point(223, 89);
            financialYearComboBox.Name = "financialYearComboBox";
            financialYearComboBox.Size = new Size(200, 21);
            financialYearComboBox.TabIndex = 15;
            toolTip1.SetToolTip(financialYearComboBox, "Select the financial year for the report. Only applicable for certain report types.");
            // 
            // toggleAutoRunButton
            // 
            toggleAutoRunButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            toggleAutoRunButton.Location = new Point(12, 443);
            toggleAutoRunButton.Name = "toggleAutoRunButton";
            toggleAutoRunButton.Size = new Size(120, 54);
            toggleAutoRunButton.TabIndex = 16;
            toggleAutoRunButton.Text = "Enable Daily Auto Run @ 8 AM";
            toolTip1.SetToolTip(toggleAutoRunButton, "Enable or disable the automated daily report generation.");
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
            menuStrip1.Dock = DockStyle.Fill;
            menuStrip1.Items.AddRange(new ToolStripItem[] { optionsToolStripMenuItem, settingsToolStripMenuItem, helpToolStripMenuItem });
            menuStrip1.Location = new Point(0, 0);
            menuStrip1.Name = "menuStrip1";
            menuStrip1.Size = new Size(784, 24);
            menuStrip1.TabIndex = 18;
            menuStrip1.Text = "menuStrip1";
            // 
            // optionsToolStripMenuItem
            // 
            optionsToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { darkModeToolStripMenuItem, toolStripSeparator7, enable1ClickProcessingToolStripMenuItem, toolStripSeparator6, setAutoRunHourToolStripMenuItem, manageAutomatedReportsToolStripMenuItem, batchRegenerateToolStripMenuItem, retrospectiveAnalysisToolStripMenuItem, toolStripSeparator8, viewConfigToolStripMenuItem, validateConfigToolStripMenuItem, toolStripSeparator4, manageCustomBankHolidaysToolStripMenuItem, manageEmailRecipientsToolStripMenuItem, manageGreetingsToolStripMenuItem, toolStripSeparator5, manageTenderExclusionsToolStripMenuItem, toolStripSeparator3, openLogsToolStripMenuItem, openAutoReportDefinitionsFileToolStripMenuItem, toolStripSeparator1, editConfigToolStripMenuItem, toolStripSeparator2, exitToolStripMenuItem });
            optionsToolStripMenuItem.Name = "optionsToolStripMenuItem";
            optionsToolStripMenuItem.Size = new Size(61, 20);
            optionsToolStripMenuItem.Text = "&Options";
            // 
            // darkModeToolStripMenuItem
            // 
            darkModeToolStripMenuItem.CheckOnClick = true;
            darkModeToolStripMenuItem.Name = "darkModeToolStripMenuItem";
            darkModeToolStripMenuItem.Size = new Size(308, 22);
            darkModeToolStripMenuItem.Text = "&Dark Mode";
            darkModeToolStripMenuItem.Click += darkModeToolStripMenuItem_Click;
            // 
            // toolStripSeparator7
            // 
            toolStripSeparator7.Name = "toolStripSeparator7";
            toolStripSeparator7.Size = new Size(305, 6);
            // 
            // enable1ClickProcessingToolStripMenuItem
            // 
            enable1ClickProcessingToolStripMenuItem.CheckOnClick = true;
            enable1ClickProcessingToolStripMenuItem.Name = "enable1ClickProcessingToolStripMenuItem";
            enable1ClickProcessingToolStripMenuItem.Size = new Size(308, 22);
            enable1ClickProcessingToolStripMenuItem.Text = "Enable &1-Click Processing";
            enable1ClickProcessingToolStripMenuItem.Click += enable1ClickProcessingToolStripMenuItem_Click;
            // 
            // toolStripSeparator6
            // 
            toolStripSeparator6.Name = "toolStripSeparator6";
            toolStripSeparator6.Size = new Size(305, 6);
            // 
            // setAutoRunHourToolStripMenuItem
            // 
            setAutoRunHourToolStripMenuItem.Name = "setAutoRunHourToolStripMenuItem";
            setAutoRunHourToolStripMenuItem.Size = new Size(308, 22);
            setAutoRunHourToolStripMenuItem.Text = "Set Auto-Run &Hour...";
            setAutoRunHourToolStripMenuItem.Click += setAutoRunHourToolStripMenuItem_Click;
            // 
            // manageAutomatedReportsToolStripMenuItem
            // 
            manageAutomatedReportsToolStripMenuItem.Name = "manageAutomatedReportsToolStripMenuItem";
            manageAutomatedReportsToolStripMenuItem.Size = new Size(308, 22);
            manageAutomatedReportsToolStripMenuItem.Text = "Manage Automated Reports...";
            manageAutomatedReportsToolStripMenuItem.Click += manageAutomatedReportsToolStripMenuItem_Click;
            // 
            // batchRegenerateToolStripMenuItem
            // 
            batchRegenerateToolStripMenuItem.Name = "batchRegenerateToolStripMenuItem";
            batchRegenerateToolStripMenuItem.Size = new Size(308, 22);
            batchRegenerateToolStripMenuItem.Text = "Batch Regenerate Reports...";
            batchRegenerateToolStripMenuItem.Click += batchRegenerateToolStripMenuItem_Click;
            // 
            // retrospectiveAnalysisToolStripMenuItem
            // 
            retrospectiveAnalysisToolStripMenuItem.Name = "retrospectiveAnalysisToolStripMenuItem";
            retrospectiveAnalysisToolStripMenuItem.Size = new Size(308, 22);
            retrospectiveAnalysisToolStripMenuItem.Text = "Generate Retrospective Lead Time Analysis...";
            retrospectiveAnalysisToolStripMenuItem.Click += retrospectiveAnalysisToolStripMenuItem_Click;
            // 
            // toolStripSeparator8
            // 
            toolStripSeparator8.Name = "toolStripSeparator8";
            toolStripSeparator8.Size = new Size(305, 6);
            // 
            // viewConfigToolStripMenuItem
            // 
            viewConfigToolStripMenuItem.Name = "viewConfigToolStripMenuItem";
            viewConfigToolStripMenuItem.Size = new Size(308, 22);
            viewConfigToolStripMenuItem.Text = "&View Configuration";
            viewConfigToolStripMenuItem.Click += viewConfigToolStripMenuItem_Click;
            // 
            // validateConfigToolStripMenuItem
            // 
            validateConfigToolStripMenuItem.Name = "validateConfigToolStripMenuItem";
            validateConfigToolStripMenuItem.Size = new Size(308, 22);
            validateConfigToolStripMenuItem.Text = "V&alidate Configuration";
            validateConfigToolStripMenuItem.Click += validateConfigToolStripMenuItem_Click;
            // 
            // toolStripSeparator4
            // 
            toolStripSeparator4.Name = "toolStripSeparator4";
            toolStripSeparator4.Size = new Size(305, 6);
            // 
            // manageCustomBankHolidaysToolStripMenuItem
            // 
            manageCustomBankHolidaysToolStripMenuItem.Name = "manageCustomBankHolidaysToolStripMenuItem";
            manageCustomBankHolidaysToolStripMenuItem.Size = new Size(308, 22);
            manageCustomBankHolidaysToolStripMenuItem.Text = "Manage Custom &Bank Holidays...";
            manageCustomBankHolidaysToolStripMenuItem.Click += manageCustomBankHolidaysToolStripMenuItem_Click;
            // 
            // manageEmailRecipientsToolStripMenuItem
            // 
            manageEmailRecipientsToolStripMenuItem.Name = "manageEmailRecipientsToolStripMenuItem";
            manageEmailRecipientsToolStripMenuItem.Size = new Size(308, 22);
            manageEmailRecipientsToolStripMenuItem.Text = "Manage Email &Recipients...";
            manageEmailRecipientsToolStripMenuItem.Click += manageEmailRecipientsToolStripMenuItem_Click;
            // 
            // manageGreetingsToolStripMenuItem
            // 
            manageGreetingsToolStripMenuItem.Name = "manageGreetingsToolStripMenuItem";
            manageGreetingsToolStripMenuItem.Size = new Size(308, 22);
            manageGreetingsToolStripMenuItem.Text = "Manage Email &Greetings...";
            manageGreetingsToolStripMenuItem.Click += manageGreetingsToolStripMenuItem_Click;
            // 
            // manageTenderExclusionsToolStripMenuItem
            // 
            manageTenderExclusionsToolStripMenuItem.Name = "manageTenderExclusionsToolStripMenuItem";
            manageTenderExclusionsToolStripMenuItem.Size = new Size(308, 22);
            manageTenderExclusionsToolStripMenuItem.Text = "Manage &Tender Exclusions...";
            manageTenderExclusionsToolStripMenuItem.Click += manageTenderExclusionsToolStripMenuItem_Click;
            // 
            // toolStripSeparator3
            // 
            toolStripSeparator3.Name = "toolStripSeparator3";
            toolStripSeparator3.Size = new Size(305, 6);
            // 
            // toolStripSeparator5
            // 
            toolStripSeparator5.Name = "toolStripSeparator5";
            toolStripSeparator5.Size = new Size(305, 6);
            // 
            // openLogsToolStripMenuItem
            // 
            openLogsToolStripMenuItem.Name = "openLogsToolStripMenuItem";
            openLogsToolStripMenuItem.Size = new Size(308, 22);
            openLogsToolStripMenuItem.Text = "Open &Logs Folder";
            openLogsToolStripMenuItem.Click += openLogsToolStripMenuItem_Click;
            // 
            // openAutoReportDefinitionsFileToolStripMenuItem
            // 
            openAutoReportDefinitionsFileToolStripMenuItem.Name = "openAutoReportDefinitionsFileToolStripMenuItem";
            openAutoReportDefinitionsFileToolStripMenuItem.Size = new Size(308, 22);
            openAutoReportDefinitionsFileToolStripMenuItem.Text = "Open Auto Report Definitions File";
            openAutoReportDefinitionsFileToolStripMenuItem.Click += openAutoReportDefinitionsFileToolStripMenuItem_Click;
            // 
            // toolStripSeparator1
            // 
            toolStripSeparator1.Name = "toolStripSeparator1";
            toolStripSeparator1.Size = new Size(305, 6);
            // 
            // editConfigToolStripMenuItem
            // 
            editConfigToolStripMenuItem.Name = "editConfigToolStripMenuItem";
            editConfigToolStripMenuItem.Size = new Size(308, 22);
            editConfigToolStripMenuItem.Text = "&Edit appsettings.json";
            editConfigToolStripMenuItem.Click += editConfigToolStripMenuItem_Click;
            // 
            // toolStripSeparator2
            // 
            toolStripSeparator2.Name = "toolStripSeparator2";
            toolStripSeparator2.Size = new Size(305, 6);
            // 
            // exitToolStripMenuItem
            // 
            exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            exitToolStripMenuItem.Size = new Size(308, 22);
            exitToolStripMenuItem.Text = "E&xit";
            exitToolStripMenuItem.Click += exitToolStripMenuItem_Click;
            // 
            // settingsToolStripMenuItem
            // 
            settingsToolStripMenuItem.Name = "settingsToolStripMenuItem";
            settingsToolStripMenuItem.Size = new Size(61, 20);
            settingsToolStripMenuItem.Text = "&Settings";
            settingsToolStripMenuItem.Click += settingsToolStripMenuItem_Click;
            // 
            // helpToolStripMenuItem
            // 
            helpToolStripMenuItem.Name = "helpToolStripMenuItem";
            helpToolStripMenuItem.Size = new Size(44, 20);
            helpToolStripMenuItem.Text = "&Help";
            helpToolStripMenuItem.Click += helpToolStripMenuItem_Click;
            // 
            // toolTip1
            // 
            toolTip1.AutomaticDelay = 700;
            toolTip1.AutoPopDelay = 7000;
            toolTip1.InitialDelay = 500;
            toolTip1.ReshowDelay = 140;
            // 
            // rootTableLayoutPanel
            // 
            rootTableLayoutPanel.ColumnCount = 1;
            rootTableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            rootTableLayoutPanel.Controls.Add(menuStrip1, 0, 0);
            rootTableLayoutPanel.Controls.Add(mainStatusStrip, 0, 2);
            rootTableLayoutPanel.Controls.Add(contentPanel, 0, 1);
            rootTableLayoutPanel.Dock = DockStyle.Fill;
            rootTableLayoutPanel.Location = new Point(0, 0);
            rootTableLayoutPanel.Name = "rootTableLayoutPanel";
            rootTableLayoutPanel.RowCount = 3;
            rootTableLayoutPanel.RowStyles.Add(new RowStyle());
            rootTableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            rootTableLayoutPanel.RowStyles.Add(new RowStyle());
            rootTableLayoutPanel.Size = new Size(784, 561);
            rootTableLayoutPanel.TabIndex = 0;
            // 
            // contentPanel
            // 
            contentPanel.Controls.Add(toggleAutoRunButton);
            contentPanel.Controls.Add(contentCenterLayout);
            contentPanel.Dock = DockStyle.Fill;
            contentPanel.Location = new Point(3, 27);
            contentPanel.Name = "contentPanel";
            contentPanel.Size = new Size(778, 509);
            contentPanel.TabIndex = 0;
            // 
            // contentCenterLayout
            // 
            contentCenterLayout.ColumnCount = 3;
            contentCenterLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            contentCenterLayout.ColumnStyles.Add(new ColumnStyle());
            contentCenterLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            contentCenterLayout.Controls.Add(centerStackPanel, 1, 1);
            contentCenterLayout.Dock = DockStyle.Fill;
            contentCenterLayout.Location = new Point(0, 0);
            contentCenterLayout.Name = "contentCenterLayout";
            contentCenterLayout.RowCount = 3;
            contentCenterLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            contentCenterLayout.RowStyles.Add(new RowStyle());
            contentCenterLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            contentCenterLayout.Size = new Size(778, 509);
            contentCenterLayout.TabIndex = 0;
            // 
            // centerStackPanel
            // 
            centerStackPanel.AutoSize = true;
            centerStackPanel.ColumnCount = 1;
            centerStackPanel.ColumnStyles.Add(new ColumnStyle());
            centerStackPanel.Controls.Add(reportTypePanel, 0, 0);
            centerStackPanel.Controls.Add(reportSettingsGroupBox, 0, 1);
            centerStackPanel.Controls.Add(actionButtonsPanel, 0, 2);
            centerStackPanel.Controls.Add(viewButtonsPanel, 0, 3);
            centerStackPanel.Location = new Point(157, 39);
            centerStackPanel.Name = "centerStackPanel";
            centerStackPanel.RowCount = 4;
            centerStackPanel.RowStyles.Add(new RowStyle());
            centerStackPanel.RowStyles.Add(new RowStyle());
            centerStackPanel.RowStyles.Add(new RowStyle());
            centerStackPanel.RowStyles.Add(new RowStyle());
            centerStackPanel.Size = new Size(464, 430);
            centerStackPanel.TabIndex = 1;
            // 
            // reportTypePanel
            // 
            reportTypePanel.Anchor = AnchorStyles.Top;
            reportTypePanel.AutoSize = true;
            reportTypePanel.Controls.Add(reportTypeLabel);
            reportTypePanel.Controls.Add(reportTypeComboBox);
            reportTypePanel.Location = new Point(69, 3);
            reportTypePanel.Name = "reportTypePanel";
            reportTypePanel.Padding = new Padding(0, 0, 0, 10);
            reportTypePanel.Size = new Size(325, 37);
            reportTypePanel.TabIndex = 24;
            // 
            // actionButtonsPanel
            // 
            actionButtonsPanel.Anchor = AnchorStyles.Top;
            actionButtonsPanel.AutoSize = true;
            actionButtonsPanel.Controls.Add(oneClickProcessButton);
            actionButtonsPanel.Controls.Add(createReportButton);
            actionButtonsPanel.Controls.Add(processEmailButton);
            actionButtonsPanel.Location = new Point(3, 315);
            actionButtonsPanel.Name = "actionButtonsPanel";
            actionButtonsPanel.Size = new Size(458, 77);
            actionButtonsPanel.TabIndex = 22;
            // 
            // viewButtonsPanel
            // 
            viewButtonsPanel.Anchor = AnchorStyles.Top;
            viewButtonsPanel.AutoSize = true;
            viewButtonsPanel.Controls.Add(viewReportButton);
            viewButtonsPanel.Controls.Add(viewAnalysisButton);
            viewButtonsPanel.Location = new Point(119, 398);
            viewButtonsPanel.Name = "viewButtonsPanel";
            viewButtonsPanel.Size = new Size(226, 29);
            viewButtonsPanel.TabIndex = 23;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(6F, 13F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(784, 561);
            Controls.Add(rootTableLayoutPanel);
            Font = new Font("Segoe UI", 8.25F);
            MainMenuStrip = menuStrip1;
            MinimumSize = new Size(720, 520);
            Name = "Form1";
            Text = "Quote Conversion Automation";
            FormClosing += Form1_FormClosing;
            Load += Form1_Load;
            mainStatusStrip.ResumeLayout(false);
            mainStatusStrip.PerformLayout();
            reportSettingsGroupBox.ResumeLayout(false);
            reportSettingsGroupBox.PerformLayout();
            menuStrip1.ResumeLayout(false);
            menuStrip1.PerformLayout();
            rootTableLayoutPanel.ResumeLayout(false);
            rootTableLayoutPanel.PerformLayout();
            contentPanel.ResumeLayout(false);
            contentCenterLayout.ResumeLayout(false);
            contentCenterLayout.PerformLayout();
            centerStackPanel.ResumeLayout(false);
            centerStackPanel.PerformLayout();
            reportTypePanel.ResumeLayout(false);
            reportTypePanel.PerformLayout();
            actionButtonsPanel.ResumeLayout(false);
            viewButtonsPanel.ResumeLayout(false);
            viewButtonsPanel.PerformLayout();
            ResumeLayout(false);

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
        private System.Windows.Forms.ToolStripMenuItem manageTenderExclusionsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem enable1ClickProcessingToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem setAutoRunHourToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator7;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator6;
        private System.Windows.Forms.ToolStripMenuItem manageAutomatedReportsToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator8;
        private System.Windows.Forms.ToolStripMenuItem openAutoReportDefinitionsFileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem settingsToolStripMenuItem;
        private System.Windows.Forms.TableLayoutPanel rootTableLayoutPanel;
        private System.Windows.Forms.Panel contentPanel;
        private System.Windows.Forms.TableLayoutPanel contentCenterLayout;
        private System.Windows.Forms.TableLayoutPanel centerStackPanel;
        private System.Windows.Forms.FlowLayoutPanel actionButtonsPanel;
        private System.Windows.Forms.FlowLayoutPanel viewButtonsPanel;
        private System.Windows.Forms.FlowLayoutPanel reportTypePanel;
        private System.Windows.Forms.CheckBox chkIncludeLeadTimeAnalysis;
        private System.Windows.Forms.ToolStripMenuItem batchRegenerateToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem retrospectiveAnalysisToolStripMenuItem;
    }
}