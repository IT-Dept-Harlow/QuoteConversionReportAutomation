// Form1.Designer.cs
// This version corrects the layout to restore the missing "Report Type" ComboBox
// and its label, while maintaining the responsive, centered layout.

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
            this.settingsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.helpToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
            this.rootTableLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
            this.contentPanel = new System.Windows.Forms.Panel();
            this.contentCenterLayout = new System.Windows.Forms.TableLayoutPanel();
            this.centerStackPanel = new System.Windows.Forms.TableLayoutPanel();
            this.reportTypePanel = new System.Windows.Forms.FlowLayoutPanel();
            this.actionButtonsPanel = new System.Windows.Forms.FlowLayoutPanel();
            this.viewButtonsPanel = new System.Windows.Forms.FlowLayoutPanel();
            this.mainStatusStrip.SuspendLayout();
            this.reportSettingsGroupBox.SuspendLayout();
            this.menuStrip1.SuspendLayout();
            this.rootTableLayoutPanel.SuspendLayout();
            this.contentPanel.SuspendLayout();
            this.contentCenterLayout.SuspendLayout();
            this.centerStackPanel.SuspendLayout();
            this.reportTypePanel.SuspendLayout();
            this.actionButtonsPanel.SuspendLayout();
            this.viewButtonsPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // rootTableLayoutPanel
            // 
            this.rootTableLayoutPanel.ColumnCount = 1;
            this.rootTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.rootTableLayoutPanel.Controls.Add(this.menuStrip1, 0, 0);
            this.rootTableLayoutPanel.Controls.Add(this.mainStatusStrip, 0, 2);
            this.rootTableLayoutPanel.Controls.Add(this.contentPanel, 0, 1);
            this.rootTableLayoutPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rootTableLayoutPanel.Location = new System.Drawing.Point(0, 0);
            this.rootTableLayoutPanel.Name = "rootTableLayoutPanel";
            this.rootTableLayoutPanel.RowCount = 3;
            this.rootTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.rootTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.rootTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.rootTableLayoutPanel.Size = new System.Drawing.Size(784, 561);
            this.rootTableLayoutPanel.TabIndex = 0;
            // 
            // contentPanel
            // 
            this.contentPanel.Controls.Add(this.toggleAutoRunButton);
            this.contentPanel.Controls.Add(this.contentCenterLayout);
            this.contentPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.contentPanel.Location = new System.Drawing.Point(3, 27);
            this.contentPanel.Name = "contentPanel";
            this.contentPanel.Size = new System.Drawing.Size(778, 509);
            this.contentPanel.TabIndex = 0;
            // 
            // contentCenterLayout
            // 
            this.contentCenterLayout.ColumnCount = 3;
            this.contentCenterLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.contentCenterLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.contentCenterLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.contentCenterLayout.Controls.Add(this.centerStackPanel, 1, 1);
            this.contentCenterLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.contentCenterLayout.Location = new System.Drawing.Point(0, 0);
            this.contentCenterLayout.Name = "contentCenterLayout";
            this.contentCenterLayout.RowCount = 3;
            this.contentCenterLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.contentCenterLayout.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.contentCenterLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.contentCenterLayout.Size = new System.Drawing.Size(778, 509);
            this.contentCenterLayout.TabIndex = 0;
            // 
            // centerStackPanel
            // 
            this.centerStackPanel.AutoSize = true;
            this.centerStackPanel.ColumnCount = 1;
            this.centerStackPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.centerStackPanel.Controls.Add(this.reportTypePanel, 0, 0);
            this.centerStackPanel.Controls.Add(this.reportSettingsGroupBox, 0, 1);
            this.centerStackPanel.Controls.Add(this.actionButtonsPanel, 0, 2);
            this.centerStackPanel.Controls.Add(this.viewButtonsPanel, 0, 3);
            this.centerStackPanel.Location = new System.Drawing.Point(157, 18);
            this.centerStackPanel.Name = "centerStackPanel";
            this.centerStackPanel.RowCount = 4;
            this.centerStackPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.centerStackPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.centerStackPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.centerStackPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.centerStackPanel.Size = new System.Drawing.Size(464, 472);
            this.centerStackPanel.TabIndex = 1;
            // 
            // reportTypePanel
            // 
            // This FlowLayoutPanel holds the "Report Type" label and ComboBox, keeping them on the same line.
            this.reportTypePanel.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.reportTypePanel.AutoSize = true;
            this.reportTypePanel.Controls.Add(this.reportTypeLabel);
            this.reportTypePanel.Controls.Add(this.reportTypeComboBox);
            this.reportTypePanel.Location = new System.Drawing.Point(69, 3);
            this.reportTypePanel.Name = "reportTypePanel";
            this.reportTypePanel.Padding = new System.Windows.Forms.Padding(0, 0, 0, 10);
            this.reportTypePanel.Size = new System.Drawing.Size(326, 41);
            this.reportTypePanel.TabIndex = 24;
            // 
            // actionButtonsPanel
            // 
            this.actionButtonsPanel.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.actionButtonsPanel.AutoSize = true;
            this.actionButtonsPanel.Controls.Add(this.oneClickProcessButton);
            this.actionButtonsPanel.Controls.Add(this.createReportButton);
            this.actionButtonsPanel.Controls.Add(this.processEmailButton);
            this.actionButtonsPanel.Location = new System.Drawing.Point(3, 319);
            this.actionButtonsPanel.Name = "actionButtonsPanel";
            this.actionButtonsPanel.Size = new System.Drawing.Size(458, 77);
            this.actionButtonsPanel.TabIndex = 22;
            // 
            // viewButtonsPanel
            // 
            this.viewButtonsPanel.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.viewButtonsPanel.AutoSize = true;
            this.viewButtonsPanel.Controls.Add(this.viewReportButton);
            this.viewButtonsPanel.Controls.Add(this.viewAnalysisButton);
            this.viewButtonsPanel.Location = new System.Drawing.Point(117, 402);
            this.viewButtonsPanel.Name = "viewButtonsPanel";
            this.viewButtonsPanel.Size = new System.Drawing.Size(229, 29);
            this.viewButtonsPanel.TabIndex = 23;
            // 
            // reportSettingsGroupBox
            // 
            this.reportSettingsGroupBox.Controls.Add(this.startDatePicker);
            this.reportSettingsGroupBox.Controls.Add(this.endDatePicker);
            this.reportSettingsGroupBox.Controls.Add(this.startDateLabel);
            this.reportSettingsGroupBox.Controls.Add(this.endDateLabel);
            this.reportSettingsGroupBox.Controls.Add(this.skipEmailCheckBox);
            this.reportSettingsGroupBox.Controls.Add(this.emailRecipientLabel);
            this.reportSettingsGroupBox.Controls.Add(this.financialYearLabel);
            this.reportSettingsGroupBox.Controls.Add(this.sendToFemiOnlyCheckBox);
            this.reportSettingsGroupBox.Controls.Add(this.financialYearComboBox);
            this.reportSettingsGroupBox.Font = new System.Drawing.Font("Arial", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.reportSettingsGroupBox.Location = new System.Drawing.Point(3, 50);
            this.reportSettingsGroupBox.Name = "reportSettingsGroupBox";
            this.reportSettingsGroupBox.Size = new System.Drawing.Size(458, 263);
            this.reportSettingsGroupBox.TabIndex = 14;
            this.reportSettingsGroupBox.TabStop = false;
            this.reportSettingsGroupBox.Text = "Report Settings";
            // 
            // reportTypeLabel
            // 
            this.reportTypeLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.reportTypeLabel.AutoSize = true;
            this.reportTypeLabel.Location = new System.Drawing.Point(3, 7);
            this.reportTypeLabel.Name = "reportTypeLabel";
            this.reportTypeLabel.Size = new System.Drawing.Size(71, 13);
            this.reportTypeLabel.TabIndex = 13;
            this.reportTypeLabel.Text = "Report Type:";
            // 
            // reportTypeComboBox
            // 
            this.reportTypeComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.reportTypeComboBox.FormattingEnabled = true;
            this.reportTypeComboBox.Items.AddRange(new object[] { "Daily", "Daily(5days >= £1000)", "Weekly", "Monthly", "Quarterly (3 Months)", "Annual", "Custom" });
            this.reportTypeComboBox.Location = new System.Drawing.Point(80, 3);
            this.reportTypeComboBox.Name = "reportTypeComboBox";
            this.reportTypeComboBox.Size = new System.Drawing.Size(243, 21);
            this.reportTypeComboBox.TabIndex = 12;
            this.toolTip1.SetToolTip(this.reportTypeComboBox, "Select a predefined report type. Dates will adjust automatically. Changing dates manually sets this to 'Custom'.");
            this.reportTypeComboBox.SelectedIndexChanged += new System.EventHandler(this.reportTypeComboBox_SelectedIndexChanged);
            // 
            // (The rest of the control initializations are unchanged)
            #region Unchanged Control Initializations
            this.startDatePicker.Location = new System.Drawing.Point(223, 27);
            this.startDatePicker.Name = "startDatePicker";
            this.startDatePicker.Size = new System.Drawing.Size(200, 22);
            this.startDatePicker.TabIndex = 0;
            this.toolTip1.SetToolTip(this.startDatePicker, "Select the start date for the report period. Modifying this will set the Report Type to \'Custom\'.");
            this.startDatePicker.ValueChanged += new System.EventHandler(this.DatePicker_ValueChanged);
            this.endDatePicker.Location = new System.Drawing.Point(223, 59);
            this.endDatePicker.Name = "endDatePicker";
            this.endDatePicker.Size = new System.Drawing.Size(200, 22);
            this.endDatePicker.TabIndex = 1;
            this.toolTip1.SetToolTip(this.endDatePicker, "Select the end date for the report period. Modifying this will set the Report Type to \'Custom\'.");
            this.endDatePicker.ValueChanged += new System.EventHandler(this.DatePicker_ValueChanged);
            this.startDateLabel.AutoSize = true;
            this.startDateLabel.Location = new System.Drawing.Point(119, 33);
            this.startDateLabel.Name = "startDateLabel";
            this.startDateLabel.Size = new System.Drawing.Size(93, 13);
            this.startDateLabel.TabIndex = 2;
            this.startDateLabel.Text = "Enter From Date:";
            this.endDateLabel.AutoSize = true;
            this.endDateLabel.Location = new System.Drawing.Point(119, 65);
            this.endDateLabel.Name = "endDateLabel";
            this.endDateLabel.Size = new System.Drawing.Size(79, 13);
            this.endDateLabel.TabIndex = 3;
            this.endDateLabel.Text = "Enter To Date:";
            this.createReportButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.createReportButton.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Bold);
            this.createReportButton.Location = new System.Drawing.Point(209, 3);
            this.createReportButton.Name = "createReportButton";
            this.createReportButton.Size = new System.Drawing.Size(130, 71);
            this.createReportButton.TabIndex = 5;
            this.createReportButton.Text = "Create Report";
            this.toolTip1.SetToolTip(this.createReportButton, "Click to generate the raw Crystal Report based on the selected dates and report type.");
            this.createReportButton.UseVisualStyleBackColor = true;
            this.createReportButton.Click += new System.EventHandler(this.createReportButton_Click);
            this.processEmailButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.processEmailButton.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Bold);
            this.processEmailButton.Location = new System.Drawing.Point(345, 3);
            this.processEmailButton.Name = "processEmailButton";
            this.processEmailButton.Size = new System.Drawing.Size(110, 71);
            this.processEmailButton.TabIndex = 6;
            this.processEmailButton.Text = "Process &\r\nEmail";
            this.toolTip1.SetToolTip(this.processEmailButton, "Click to process the generated raw report, create the final analysis, and email it.");
            this.processEmailButton.UseVisualStyleBackColor = true;
            this.processEmailButton.Click += new System.EventHandler(this.processEmailButton_Click);
            this.oneClickProcessButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.oneClickProcessButton.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.oneClickProcessButton.Location = new System.Drawing.Point(3, 3);
            this.oneClickProcessButton.Name = "oneClickProcessButton";
            this.oneClickProcessButton.Size = new System.Drawing.Size(200, 71);
            this.oneClickProcessButton.TabIndex = 20;
            this.oneClickProcessButton.Text = "Generate, Process && Email Report";
            this.toolTip1.SetToolTip(this.oneClickProcessButton, "Performs all steps: generates the raw report, processes it into the final analysis, and emails it (unless skipped).");
            this.oneClickProcessButton.UseVisualStyleBackColor = true;
            this.oneClickProcessButton.Click += new System.EventHandler(this.oneClickProcessButton_Click);
            this.viewReportButton.AutoSize = true;
            this.viewReportButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.viewReportButton.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Bold);
            this.viewReportButton.Location = new System.Drawing.Point(3, 3);
            this.viewReportButton.Name = "viewReportButton";
            this.viewReportButton.Size = new System.Drawing.Size(92, 23);
            this.viewReportButton.TabIndex = 8;
            this.viewReportButton.Text = "View Raw File";
            this.toolTip1.SetToolTip(this.viewReportButton, "Click to open the generated raw report file.");
            this.viewReportButton.UseVisualStyleBackColor = true;
            this.viewReportButton.Click += new System.EventHandler(this.viewReportButton_Click);
            this.viewAnalysisButton.AutoSize = true;
            this.viewAnalysisButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.viewAnalysisButton.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Bold);
            this.viewAnalysisButton.Location = new System.Drawing.Point(101, 3);
            this.viewAnalysisButton.Name = "viewAnalysisButton";
            this.viewAnalysisButton.Size = new System.Drawing.Size(122, 23);
            this.viewAnalysisButton.TabIndex = 9;
            this.viewAnalysisButton.Text = "View Processed File";
            this.toolTip1.SetToolTip(this.viewAnalysisButton, "Click to open the final processed analysis file.");
            this.viewAnalysisButton.UseVisualStyleBackColor = true;
            this.viewAnalysisButton.Click += new System.EventHandler(this.viewAnalysisButton_Click);
            this.mainStatusStrip.Dock = System.Windows.Forms.DockStyle.Fill;
            this.mainStatusStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { this.statusLabel, this.autoRunStatusLabel });
            this.mainStatusStrip.Location = new System.Drawing.Point(0, 539);
            this.mainStatusStrip.Name = "mainStatusStrip";
            this.mainStatusStrip.Size = new System.Drawing.Size(784, 22);
            this.mainStatusStrip.TabIndex = 10;
            this.statusLabel.Name = "statusLabel";
            this.statusLabel.Size = new System.Drawing.Size(661, 17);
            this.statusLabel.Spring = true;
            this.statusLabel.Text = "Ready";
            this.statusLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.autoRunStatusLabel.Name = "autoRunStatusLabel";
            this.autoRunStatusLabel.Size = new System.Drawing.Size(108, 17);
            this.autoRunStatusLabel.Text = "Auto Run: Disabled";
            this.autoRunStatusLabel.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.sendToFemiOnlyCheckBox.AutoSize = true;
            this.sendToFemiOnlyCheckBox.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.sendToFemiOnlyCheckBox.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.sendToFemiOnlyCheckBox.Location = new System.Drawing.Point(119, 147);
            this.sendToFemiOnlyCheckBox.Name = "sendToFemiOnlyCheckBox";
            this.sendToFemiOnlyCheckBox.Size = new System.Drawing.Size(142, 21);
            this.sendToFemiOnlyCheckBox.TabIndex = 11;
            this.sendToFemiOnlyCheckBox.Text = "Send to only Femi?";
            this.toolTip1.SetToolTip(this.sendToFemiOnlyCheckBox, "If checked, the report is sent to a restricted recipient list.");
            this.sendToFemiOnlyCheckBox.UseVisualStyleBackColor = true;
            this.skipEmailCheckBox.AutoSize = true;
            this.skipEmailCheckBox.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.skipEmailCheckBox.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.skipEmailCheckBox.Location = new System.Drawing.Point(15, 225);
            this.skipEmailCheckBox.Name = "skipEmailCheckBox";
            this.skipEmailCheckBox.Size = new System.Drawing.Size(130, 18);
            this.skipEmailCheckBox.TabIndex = 21;
            this.skipEmailCheckBox.Text = "Skip Sending Email";
            this.toolTip1.SetToolTip(this.skipEmailCheckBox, "If checked, the email sending step will be skipped.");
            this.skipEmailCheckBox.UseVisualStyleBackColor = true;
            this.emailRecipientLabel.AutoSize = true;
            this.emailRecipientLabel.Font = new System.Drawing.Font("Arial", 9.75F, System.Drawing.FontStyle.Bold);
            this.emailRecipientLabel.Location = new System.Drawing.Point(119, 147);
            this.emailRecipientLabel.Name = "emailRecipientLabel";
            this.emailRecipientLabel.Size = new System.Drawing.Size(0, 16);
            this.emailRecipientLabel.TabIndex = 17;
            this.financialYearLabel.AutoSize = true;
            this.financialYearLabel.Location = new System.Drawing.Point(119, 93);
            this.financialYearLabel.Name = "financialYearLabel";
            this.financialYearLabel.Size = new System.Drawing.Size(78, 14);
            this.financialYearLabel.TabIndex = 16;
            this.financialYearLabel.Text = "Financial Year:";
            this.financialYearComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.financialYearComboBox.FormattingEnabled = true;
            this.financialYearComboBox.Location = new System.Drawing.Point(223, 89);
            this.financialYearComboBox.Name = "financialYearComboBox";
            this.financialYearComboBox.Size = new System.Drawing.Size(200, 22);
            this.financialYearComboBox.TabIndex = 15;
            this.toolTip1.SetToolTip(this.financialYearComboBox, "Select the financial year for the report. Only applicable for certain report types.");
            this.toggleAutoRunButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.toggleAutoRunButton.Location = new System.Drawing.Point(12, 443);
            this.toggleAutoRunButton.Name = "toggleAutoRunButton";
            this.toggleAutoRunButton.Size = new System.Drawing.Size(120, 54);
            this.toggleAutoRunButton.TabIndex = 16;
            this.toggleAutoRunButton.Text = "Enable Daily Auto Run @ 8 AM";
            this.toolTip1.SetToolTip(this.toggleAutoRunButton, "Enable or disable the automated daily report generation.");
            this.toggleAutoRunButton.UseVisualStyleBackColor = true;
            this.toggleAutoRunButton.Click += new System.EventHandler(this.toggleAutoRunButton_Click);
            this.dailyCheckTimer.Interval = 60000;
            this.dailyCheckTimer.Tick += new System.EventHandler(this.dailyCheckTimer_Tick);
            this.menuStrip1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { this.optionsToolStripMenuItem, this.settingsToolStripMenuItem, this.helpToolStripMenuItem });
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(784, 24);
            this.menuStrip1.TabIndex = 18;
            this.menuStrip1.Text = "menuStrip1";
            this.optionsToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { this.darkModeToolStripMenuItem, this.toolStripSeparator7, this.enable1ClickProcessingToolStripMenuItem, this.toolStripSeparator6, this.setAutoRunHourToolStripMenuItem, this.manageAutomatedReportsToolStripMenuItem, this.toolStripSeparator8, this.viewConfigToolStripMenuItem, this.validateConfigToolStripMenuItem, this.toolStripSeparator4, this.manageCustomBankHolidaysToolStripMenuItem, this.toolStripSeparator3, this.manageEmailRecipientsToolStripMenuItem, this.manageGreetingsToolStripMenuItem, this.toolStripSeparator5, this.openLogsToolStripMenuItem, this.openAutoReportDefinitionsFileToolStripMenuItem, this.toolStripSeparator1, this.editConfigToolStripMenuItem, this.toolStripSeparator2, this.exitToolStripMenuItem });
            this.optionsToolStripMenuItem.Name = "optionsToolStripMenuItem";
            this.optionsToolStripMenuItem.Size = new System.Drawing.Size(61, 20);
            this.optionsToolStripMenuItem.Text = "&Options";
            this.darkModeToolStripMenuItem.CheckOnClick = true;
            this.darkModeToolStripMenuItem.Name = "darkModeToolStripMenuItem";
            this.darkModeToolStripMenuItem.Size = new System.Drawing.Size(251, 22);
            this.darkModeToolStripMenuItem.Text = "&Dark Mode";
            this.darkModeToolStripMenuItem.Click += new System.EventHandler(this.darkModeToolStripMenuItem_Click);
            this.toolStripSeparator7.Name = "toolStripSeparator7";
            this.toolStripSeparator7.Size = new System.Drawing.Size(248, 6);
            this.enable1ClickProcessingToolStripMenuItem.CheckOnClick = true;
            this.enable1ClickProcessingToolStripMenuItem.Name = "enable1ClickProcessingToolStripMenuItem";
            this.enable1ClickProcessingToolStripMenuItem.Size = new System.Drawing.Size(251, 22);
            this.enable1ClickProcessingToolStripMenuItem.Text = "Enable &1-Click Processing";
            this.enable1ClickProcessingToolStripMenuItem.Click += new System.EventHandler(this.enable1ClickProcessingToolStripMenuItem_Click);
            this.toolStripSeparator6.Name = "toolStripSeparator6";
            this.toolStripSeparator6.Size = new System.Drawing.Size(248, 6);
            this.setAutoRunHourToolStripMenuItem.Name = "setAutoRunHourToolStripMenuItem";
            this.setAutoRunHourToolStripMenuItem.Size = new System.Drawing.Size(251, 22);
            this.setAutoRunHourToolStripMenuItem.Text = "Set Auto-Run &Hour...";
            this.setAutoRunHourToolStripMenuItem.Click += new System.EventHandler(this.setAutoRunHourToolStripMenuItem_Click);
            this.manageAutomatedReportsToolStripMenuItem.Name = "manageAutomatedReportsToolStripMenuItem";
            this.manageAutomatedReportsToolStripMenuItem.Size = new System.Drawing.Size(251, 22);
            this.manageAutomatedReportsToolStripMenuItem.Text = "Manage Automated Reports...";
            this.manageAutomatedReportsToolStripMenuItem.Click += new System.EventHandler(this.manageAutomatedReportsToolStripMenuItem_Click);
            this.toolStripSeparator8.Name = "toolStripSeparator8";
            this.toolStripSeparator8.Size = new System.Drawing.Size(248, 6);
            this.viewConfigToolStripMenuItem.Name = "viewConfigToolStripMenuItem";
            this.viewConfigToolStripMenuItem.Size = new System.Drawing.Size(251, 22);
            this.viewConfigToolStripMenuItem.Text = "&View Configuration";
            this.viewConfigToolStripMenuItem.Click += new System.EventHandler(this.viewConfigToolStripMenuItem_Click);
            this.validateConfigToolStripMenuItem.Name = "validateConfigToolStripMenuItem";
            this.validateConfigToolStripMenuItem.Size = new System.Drawing.Size(251, 22);
            this.validateConfigToolStripMenuItem.Text = "V&alidate Configuration";
            this.validateConfigToolStripMenuItem.Click += new System.EventHandler(this.validateConfigToolStripMenuItem_Click);
            this.toolStripSeparator4.Name = "toolStripSeparator4";
            this.toolStripSeparator4.Size = new System.Drawing.Size(248, 6);
            this.manageCustomBankHolidaysToolStripMenuItem.Name = "manageCustomBankHolidaysToolStripMenuItem";
            this.manageCustomBankHolidaysToolStripMenuItem.Size = new System.Drawing.Size(251, 22);
            this.manageCustomBankHolidaysToolStripMenuItem.Text = "Manage Custom &Bank Holidays";
            this.manageCustomBankHolidaysToolStripMenuItem.Click += new System.EventHandler(this.manageCustomBankHolidaysToolStripMenuItem_Click);
            this.toolStripSeparator3.Name = "toolStripSeparator3";
            this.toolStripSeparator3.Size = new System.Drawing.Size(248, 6);
            this.manageEmailRecipientsToolStripMenuItem.Name = "manageEmailRecipientsToolStripMenuItem";
            this.manageEmailRecipientsToolStripMenuItem.Size = new System.Drawing.Size(251, 22);
            this.manageEmailRecipientsToolStripMenuItem.Text = "Manage Email &Recipients";
            this.manageEmailRecipientsToolStripMenuItem.Click += new System.EventHandler(this.manageEmailRecipientsToolStripMenuItem_Click);
            this.manageGreetingsToolStripMenuItem.Name = "manageGreetingsToolStripMenuItem";
            this.manageGreetingsToolStripMenuItem.Size = new System.Drawing.Size(251, 22);
            this.manageGreetingsToolStripMenuItem.Text = "Manage Email &Greetings";
            this.manageGreetingsToolStripMenuItem.Click += new System.EventHandler(this.manageGreetingsToolStripMenuItem_Click);
            this.toolStripSeparator5.Name = "toolStripSeparator5";
            this.toolStripSeparator5.Size = new System.Drawing.Size(248, 6);
            this.openLogsToolStripMenuItem.Name = "openLogsToolStripMenuItem";
            this.openLogsToolStripMenuItem.Size = new System.Drawing.Size(251, 22);
            this.openLogsToolStripMenuItem.Text = "Open &Logs Folder";
            this.openLogsToolStripMenuItem.Click += new System.EventHandler(this.openLogsToolStripMenuItem_Click);
            this.openAutoReportDefinitionsFileToolStripMenuItem.Name = "openAutoReportDefinitionsFileToolStripMenuItem";
            this.openAutoReportDefinitionsFileToolStripMenuItem.Size = new System.Drawing.Size(251, 22);
            this.openAutoReportDefinitionsFileToolStripMenuItem.Text = "Open Auto Report Definitions File";
            this.openAutoReportDefinitionsFileToolStripMenuItem.Click += new System.EventHandler(this.openAutoReportDefinitionsFileToolStripMenuItem_Click);
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(248, 6);
            this.editConfigToolStripMenuItem.Name = "editConfigToolStripMenuItem";
            this.editConfigToolStripMenuItem.Size = new System.Drawing.Size(251, 22);
            this.editConfigToolStripMenuItem.Text = "&Edit appsettings.json";
            this.editConfigToolStripMenuItem.Click += new System.EventHandler(this.editConfigToolStripMenuItem_Click);
            this.toolStripSeparator2.Name = "toolStripSeparator2";
            this.toolStripSeparator2.Size = new System.Drawing.Size(248, 6);
            this.exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            this.exitToolStripMenuItem.Size = new System.Drawing.Size(251, 22);
            this.exitToolStripMenuItem.Text = "E&xit";
            this.exitToolStripMenuItem.Click += new System.EventHandler(this.exitToolStripMenuItem_Click);
            this.settingsToolStripMenuItem.Name = "settingsToolStripMenuItem";
            this.settingsToolStripMenuItem.Size = new System.Drawing.Size(61, 20);
            this.settingsToolStripMenuItem.Text = "&Settings";
            this.settingsToolStripMenuItem.Click += new System.EventHandler(this.settingsToolStripMenuItem_Click);
            this.helpToolStripMenuItem.Name = "helpToolStripMenuItem";
            this.helpToolStripMenuItem.Size = new System.Drawing.Size(44, 20);
            this.helpToolStripMenuItem.Text = "&Help";
            this.helpToolStripMenuItem.Click += new System.EventHandler(this.helpToolStripMenuItem_Click);
            this.toolTip1.AutomaticDelay = 700;
            this.toolTip1.AutoPopDelay = 7000;
            this.toolTip1.InitialDelay = 500;
            this.toolTip1.ReshowDelay = 140;
            #endregion
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(784, 561);
            this.Controls.Add(this.rootTableLayoutPanel);
            this.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Sizable;
            this.MaximizeBox = true;
            this.MinimumSize = new System.Drawing.Size(720, 520);
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "Form1";
            this.Text = "Quote Conversion Automation";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Form1_FormClosing);
            this.Load += new System.EventHandler(this.Form1_Load);
            this.mainStatusStrip.ResumeLayout(false);
            this.mainStatusStrip.PerformLayout();
            this.reportSettingsGroupBox.ResumeLayout(false);
            this.reportSettingsGroupBox.PerformLayout();
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.rootTableLayoutPanel.ResumeLayout(false);
            this.rootTableLayoutPanel.PerformLayout();
            this.contentPanel.ResumeLayout(false);
            this.contentCenterLayout.ResumeLayout(false);
            this.contentCenterLayout.PerformLayout();
            this.centerStackPanel.ResumeLayout(false);
            this.centerStackPanel.PerformLayout();
            this.reportTypePanel.ResumeLayout(false);
            this.reportTypePanel.PerformLayout();
            this.actionButtonsPanel.ResumeLayout(false);
            this.viewButtonsPanel.ResumeLayout(false);
            this.ResumeLayout(false);

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
        private System.Windows.Forms.ToolStripMenuItem settingsToolStripMenuItem;
        private System.Windows.Forms.TableLayoutPanel rootTableLayoutPanel;
        private System.Windows.Forms.Panel contentPanel;
        private System.Windows.Forms.TableLayoutPanel contentCenterLayout;
        private System.Windows.Forms.TableLayoutPanel centerStackPanel;
        private System.Windows.Forms.FlowLayoutPanel actionButtonsPanel;
        private System.Windows.Forms.FlowLayoutPanel viewButtonsPanel;
        private System.Windows.Forms.FlowLayoutPanel reportTypePanel;
    }
}