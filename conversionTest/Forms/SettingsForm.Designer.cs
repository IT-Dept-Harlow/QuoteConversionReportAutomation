// SettingsForm.Designer.cs
// Contains the Windows Forms Designer generated code for the SettingsForm.
// This form allows users to view and modify application settings.

namespace QuoteConversionReportAutomation.Forms
{
    partial class SettingsForm
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
            this.mainTabControl = new System.Windows.Forms.TabControl();
            this.tabPageAppInfo = new System.Windows.Forms.TabPage();
            this.tlpAppInfo = new System.Windows.Forms.TableLayoutPanel();
            this.lblAppName = new System.Windows.Forms.Label();
            this.txtAppName = new System.Windows.Forms.TextBox();
            this.lblAppVersion = new System.Windows.Forms.Label();
            this.txtAppVersion = new System.Windows.Forms.TextBox();
            this.tabPagePaths = new System.Windows.Forms.TabPage();
            this.tlpPaths = new System.Windows.Forms.TableLayoutPanel();
            this.lblCrystalReportRptFile = new System.Windows.Forms.Label();
            this.txtCrystalReportRptFile = new System.Windows.Forms.TextBox();
            this.btnBrowseCrystalReport = new System.Windows.Forms.Button();
            this.lblFinalReportOutputBase = new System.Windows.Forms.Label();
            this.txtFinalReportOutputBase = new System.Windows.Forms.TextBox();
            this.btnBrowseFinalReportOutputBase = new System.Windows.Forms.Button();
            this.lblTemplateBase = new System.Windows.Forms.Label();
            this.txtTemplateBase = new System.Windows.Forms.TextBox();
            this.btnBrowseTemplateBase = new System.Windows.Forms.Button();
            this.lblLogDirectoryBase = new System.Windows.Forms.Label();
            this.txtLogDirectoryBase = new System.Windows.Forms.TextBox();
            this.btnBrowseLogDirectoryBase = new System.Windows.Forms.Button();
            this.lblRawReportOutputBase = new System.Windows.Forms.Label();
            this.txtRawReportOutputBase = new System.Windows.Forms.TextBox();
            this.btnBrowseRawReportOutputBase = new System.Windows.Forms.Button();
            this.lblWrapperExecutable = new System.Windows.Forms.Label();
            this.txtWrapperExecutable = new System.Windows.Forms.TextBox();
            this.btnBrowseWrapperExecutable = new System.Windows.Forms.Button();
            this.lblReportDefinitionsFileName = new System.Windows.Forms.Label();
            this.txtReportDefinitionsFileName = new System.Windows.Forms.TextBox();
            this.lblFallbackLogDirectory = new System.Windows.Forms.Label();
            this.txtFallbackLogDirectory = new System.Windows.Forms.TextBox();
            this.btnBrowseFallbackLogDir = new System.Windows.Forms.Button();
            this.tabPageSmtp = new System.Windows.Forms.TabPage();
            this.tlpSmtp = new System.Windows.Forms.TableLayoutPanel();
            this.lblSmtpServer = new System.Windows.Forms.Label();
            this.txtSmtpServer = new System.Windows.Forms.TextBox();
            this.lblSmtpPort = new System.Windows.Forms.Label();
            this.numSmtpPort = new System.Windows.Forms.NumericUpDown();
            this.lblSmtpUsername = new System.Windows.Forms.Label();
            this.txtSmtpUsername = new System.Windows.Forms.TextBox();
            this.lblSmtpPassword = new System.Windows.Forms.Label();
            this.txtSmtpPassword = new System.Windows.Forms.TextBox();
            this.chkSmtpEnableSsl = new System.Windows.Forms.CheckBox();
            this.lblSmtpMaxSendRetries = new System.Windows.Forms.Label();
            this.numSmtpMaxSendRetries = new System.Windows.Forms.NumericUpDown();
            this.lblSmtpSendRetryDelayMs = new System.Windows.Forms.Label();
            this.numSmtpSendRetryDelayMs = new System.Windows.Forms.NumericUpDown();
            this.lblSmtpTimeoutMs = new System.Windows.Forms.Label();
            this.numSmtpTimeoutMs = new System.Windows.Forms.NumericUpDown();
            this.tabPageEmailDefaults = new System.Windows.Forms.TabPage();
            this.tlpEmailDefaults = new System.Windows.Forms.TableLayoutPanel();
            this.lblSenderAddress = new System.Windows.Forms.Label();
            this.txtSenderAddress = new System.Windows.Forms.TextBox();
            this.lblSenderDisplayName = new System.Windows.Forms.Label();
            this.txtSenderDisplayName = new System.Windows.Forms.TextBox();
            this.lblMaxAttachmentSizeBytes = new System.Windows.Forms.Label();
            this.numMaxAttachmentSizeBytes = new System.Windows.Forms.NumericUpDown();
            this.lblDefaultEmailSignature = new System.Windows.Forms.Label();
            this.txtDefaultEmailSignature = new System.Windows.Forms.TextBox();
            this.lblAttachmentReadMaxRetries = new System.Windows.Forms.Label();
            this.numAttachmentReadMaxRetries = new System.Windows.Forms.NumericUpDown();
            this.lblAttachmentReadDelayMs = new System.Windows.Forms.Label();
            this.numAttachmentReadDelayMs = new System.Windows.Forms.NumericUpDown();
            this.tabPageLogging = new System.Windows.Forms.TabPage();
            this.tlpLogging = new System.Windows.Forms.TableLayoutPanel();
            this.lblDefaultLogLevel = new System.Windows.Forms.Label();
            this.cmbDefaultLogLevel = new System.Windows.Forms.ComboBox();
            this.lblDebugBuildLogLevel = new System.Windows.Forms.Label();
            this.cmbDebugBuildLogLevel = new System.Windows.Forms.ComboBox();
            this.lblLogArchiveOlderThanDays = new System.Windows.Forms.Label();
            this.numLogArchiveOlderThanDays = new System.Windows.Forms.NumericUpDown();
            this.lblLogFileNameFormat = new System.Windows.Forms.Label();
            this.txtLogFileNameFormat = new System.Windows.Forms.TextBox();
            this.tabPageOperational = new System.Windows.Forms.TabPage();
            this.tlpOperational = new System.Windows.Forms.TableLayoutPanel();
            this.lblArchiveRawReportsOlderThanDays = new System.Windows.Forms.Label();
            this.numArchiveRawReportsOlderThanDays = new System.Windows.Forms.NumericUpDown();
            this.lblReportArchiveFolderName = new System.Windows.Forms.Label();
            this.txtReportArchiveFolderName = new System.Windows.Forms.TextBox();
            this.lblProcessTimeoutMinutes = new System.Windows.Forms.Label();
            this.numProcessTimeoutMinutes = new System.Windows.Forms.NumericUpDown();
            this.lblFinancialYearStartMonth = new System.Windows.Forms.Label();
            this.numFinancialYearStartMonth = new System.Windows.Forms.NumericUpDown();
            this.lblFinancialYearStartDay = new System.Windows.Forms.Label();
            this.numFinancialYearStartDay = new System.Windows.Forms.NumericUpDown();
            this.lblDaily5Day1kFilteringThreshold = new System.Windows.Forms.Label();
            this.numDaily5Day1kFilteringThreshold = new System.Windows.Forms.NumericUpDown();
            this.lblGeneralFileOpMaxRetries = new System.Windows.Forms.Label();
            this.numGeneralFileOpMaxRetries = new System.Windows.Forms.NumericUpDown();
            this.lblGeneralFileOpDelayMs = new System.Windows.Forms.Label();
            this.numGeneralFileOpDelayMs = new System.Windows.Forms.NumericUpDown();
            this.lblRawDataSourceSheet = new System.Windows.Forms.Label();
            this.txtRawDataSourceSheet = new System.Windows.Forms.TextBox();
            this.lblTemplateDataCopySheet = new System.Windows.Forms.Label();
            this.txtTemplateDataCopySheet = new System.Windows.Forms.TextBox();
            this.lblTemplateAnalysisSheet = new System.Windows.Forms.Label();
            this.txtTemplateAnalysisSheet = new System.Windows.Forms.TextBox();
            this.lblPowerBiDataSheet = new System.Windows.Forms.Label();
            this.txtPowerBiDataSheet = new System.Windows.Forms.TextBox();
            this.lblMonthlyOrderPivotSheet = new System.Windows.Forms.Label();
            this.txtMonthlyOrderPivotSheet = new System.Windows.Forms.TextBox();
            this.lblMonthlyEstimatePivotSheet = new System.Windows.Forms.Label();
            this.txtMonthlyEstimatePivotSheet = new System.Windows.Forms.TextBox();
            this.lblMonthlyOrderPivotName = new System.Windows.Forms.Label();
            this.txtMonthlyOrderPivotName = new System.Windows.Forms.TextBox();
            this.lblMonthlyEstimatePivotName = new System.Windows.Forms.Label();
            this.txtMonthlyEstimatePivotName = new System.Windows.Forms.TextBox();
            this.lblFolderNamingDaily = new System.Windows.Forms.Label();
            this.txtFolderNamingDaily = new System.Windows.Forms.TextBox();
            this.lblFolderNamingDaily5Day1k = new System.Windows.Forms.Label();
            this.txtFolderNamingDaily5Day1k = new System.Windows.Forms.TextBox();
            this.lblFolderNamingWeekly = new System.Windows.Forms.Label();
            this.txtFolderNamingWeekly = new System.Windows.Forms.TextBox();
            this.lblFolderNamingMonthly = new System.Windows.Forms.Label();
            this.txtFolderNamingMonthly = new System.Windows.Forms.TextBox();
            this.lblFolderNamingQuarterly = new System.Windows.Forms.Label();
            this.txtFolderNamingQuarterly = new System.Windows.Forms.TextBox();
            this.lblFolderNamingAnnual = new System.Windows.Forms.Label();
            this.txtFolderNamingAnnual = new System.Windows.Forms.TextBox();
            this.lblFolderNamingCustom = new System.Windows.Forms.Label();
            this.txtFolderNamingCustom = new System.Windows.Forms.TextBox();
            this.lblFolderNamingOther = new System.Windows.Forms.Label();
            this.txtFolderNamingOther = new System.Windows.Forms.TextBox();
            this.tabPageIPC = new System.Windows.Forms.TabPage();
            this.tlpIPC = new System.Windows.Forms.TableLayoutPanel();
            this.lblNamedPipeName = new System.Windows.Forms.Label();
            this.txtNamedPipeName = new System.Windows.Forms.TextBox();
            this.lblPipeConnectTimeoutMs = new System.Windows.Forms.Label();
            this.numPipeConnectTimeoutMs = new System.Windows.Forms.NumericUpDown();
            this.lblMaxPipeResponseSizeBytes = new System.Windows.Forms.Label();
            this.numMaxPipeResponseSizeBytes = new System.Windows.Forms.NumericUpDown();
            this.tabPageAutoRun = new System.Windows.Forms.TabPage();
            this.tlpAutoRun = new System.Windows.Forms.TableLayoutPanel();
            this.lblAutoRunCheckHour = new System.Windows.Forms.Label();
            this.numAutoRunCheckHour = new System.Windows.Forms.NumericUpDown();
            this.panelButtons = new System.Windows.Forms.Panel();
            this.btnSaveChanges = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
            this.mainTabControl.SuspendLayout();
            this.tabPageAppInfo.SuspendLayout();
            this.tlpAppInfo.SuspendLayout();
            this.tabPagePaths.SuspendLayout();
            this.tlpPaths.SuspendLayout();
            this.tabPageSmtp.SuspendLayout();
            this.tlpSmtp.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numSmtpPort)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numSmtpMaxSendRetries)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numSmtpSendRetryDelayMs)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numSmtpTimeoutMs)).BeginInit();
            this.tabPageEmailDefaults.SuspendLayout();
            this.tlpEmailDefaults.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numMaxAttachmentSizeBytes)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numAttachmentReadMaxRetries)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numAttachmentReadDelayMs)).BeginInit();
            this.tabPageLogging.SuspendLayout();
            this.tlpLogging.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numLogArchiveOlderThanDays)).BeginInit();
            this.tabPageOperational.SuspendLayout();
            this.tlpOperational.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numArchiveRawReportsOlderThanDays)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numProcessTimeoutMinutes)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numFinancialYearStartMonth)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numFinancialYearStartDay)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numDaily5Day1kFilteringThreshold)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numGeneralFileOpMaxRetries)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numGeneralFileOpDelayMs)).BeginInit();
            this.tabPageIPC.SuspendLayout();
            this.tlpIPC.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numPipeConnectTimeoutMs)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numMaxPipeResponseSizeBytes)).BeginInit();
            this.tabPageAutoRun.SuspendLayout();
            this.tlpAutoRun.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numAutoRunCheckHour)).BeginInit();
            this.panelButtons.SuspendLayout();
            this.SuspendLayout();
            // 
            // mainTabControl
            // 
            this.mainTabControl.Controls.Add(this.tabPageAppInfo);
            this.mainTabControl.Controls.Add(this.tabPagePaths);
            this.mainTabControl.Controls.Add(this.tabPageSmtp);
            this.mainTabControl.Controls.Add(this.tabPageEmailDefaults);
            this.mainTabControl.Controls.Add(this.tabPageLogging);
            this.mainTabControl.Controls.Add(this.tabPageOperational);
            this.mainTabControl.Controls.Add(this.tabPageIPC);
            this.mainTabControl.Controls.Add(this.tabPageAutoRun);
            this.mainTabControl.Dock = System.Windows.Forms.DockStyle.Fill;
            this.mainTabControl.Location = new System.Drawing.Point(0, 0);
            this.mainTabControl.Name = "mainTabControl";
            this.mainTabControl.SelectedIndex = 0;
            this.mainTabControl.Size = new System.Drawing.Size(784, 411); // Adjusted height for potentially more content
            this.mainTabControl.TabIndex = 0;
            // 
            // tabPageAppInfo
            // 
            this.tabPageAppInfo.AutoScroll = true;
            this.tabPageAppInfo.Controls.Add(this.tlpAppInfo);
            this.tabPageAppInfo.Location = new System.Drawing.Point(4, 22);
            this.tabPageAppInfo.Name = "tabPageAppInfo";
            this.tabPageAppInfo.Padding = new System.Windows.Forms.Padding(10);
            this.tabPageAppInfo.Size = new System.Drawing.Size(776, 385);
            this.tabPageAppInfo.TabIndex = 0;
            this.tabPageAppInfo.Text = "Application Info";
            this.tabPageAppInfo.UseVisualStyleBackColor = true;
            // 
            // tlpAppInfo
            // 
            this.tlpAppInfo.AutoSize = true;
            this.tlpAppInfo.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.tlpAppInfo.ColumnCount = 2;
            this.tlpAppInfo.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 150F));
            this.tlpAppInfo.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tlpAppInfo.Controls.Add(this.lblAppName, 0, 0);
            this.tlpAppInfo.Controls.Add(this.txtAppName, 1, 0);
            this.tlpAppInfo.Controls.Add(this.lblAppVersion, 0, 1);
            this.tlpAppInfo.Controls.Add(this.txtAppVersion, 1, 1);
            this.tlpAppInfo.Dock = System.Windows.Forms.DockStyle.Top;
            this.tlpAppInfo.Location = new System.Drawing.Point(10, 10);
            this.tlpAppInfo.Name = "tlpAppInfo";
            this.tlpAppInfo.RowCount = 2;
            this.tlpAppInfo.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 35F));
            this.tlpAppInfo.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 35F));
            this.tlpAppInfo.Size = new System.Drawing.Size(756, 70);
            this.tlpAppInfo.TabIndex = 0;
            // 
            // lblAppName
            // 
            this.lblAppName.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.lblAppName.AutoSize = true;
            this.lblAppName.Location = new System.Drawing.Point(58, 11);
            this.lblAppName.Name = "lblAppName";
            this.lblAppName.Size = new System.Drawing.Size(89, 13);
            this.lblAppName.TabIndex = 0;
            this.lblAppName.Text = "Application Name:";
            this.lblAppName.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // txtAppName
            // 
            this.txtAppName.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.txtAppName.Location = new System.Drawing.Point(153, 7);
            this.txtAppName.Name = "txtAppName";
            this.txtAppName.Size = new System.Drawing.Size(600, 20);
            this.txtAppName.TabIndex = 1;
            // 
            // lblAppVersion
            // 
            this.lblAppVersion.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.lblAppVersion.AutoSize = true;
            this.lblAppVersion.Location = new System.Drawing.Point(50, 46);
            this.lblAppVersion.Name = "lblAppVersion";
            this.lblAppVersion.Size = new System.Drawing.Size(100, 13);
            this.lblAppVersion.TabIndex = 2;
            this.lblAppVersion.Text = "Application Version:";
            this.lblAppVersion.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // txtAppVersion
            // 
            this.txtAppVersion.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.txtAppVersion.Location = new System.Drawing.Point(153, 42);
            this.txtAppVersion.Name = "txtAppVersion";
            this.txtAppVersion.Size = new System.Drawing.Size(600, 20);
            this.txtAppVersion.TabIndex = 3;
            // 
            // tabPagePaths
            // 
            this.tabPagePaths.AutoScroll = true;
            this.tabPagePaths.Controls.Add(this.tlpPaths);
            this.tabPagePaths.Location = new System.Drawing.Point(4, 22);
            this.tabPagePaths.Name = "tabPagePaths";
            this.tabPagePaths.Padding = new System.Windows.Forms.Padding(10);
            this.tabPagePaths.Size = new System.Drawing.Size(776, 385);
            this.tabPagePaths.TabIndex = 1;
            this.tabPagePaths.Text = "Paths";
            this.tabPagePaths.UseVisualStyleBackColor = true;
            // 
            // tlpPaths
            // 
            this.tlpPaths.AutoSize = true;
            this.tlpPaths.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.tlpPaths.ColumnCount = 3;
            this.tlpPaths.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 220F));
            this.tlpPaths.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tlpPaths.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 95F));
            this.tlpPaths.Controls.Add(this.lblCrystalReportRptFile, 0, 0);
            this.tlpPaths.Controls.Add(this.txtCrystalReportRptFile, 1, 0);
            this.tlpPaths.Controls.Add(this.btnBrowseCrystalReport, 2, 0);
            this.tlpPaths.Controls.Add(this.lblFinalReportOutputBase, 0, 1);
            this.tlpPaths.Controls.Add(this.txtFinalReportOutputBase, 1, 1);
            this.tlpPaths.Controls.Add(this.btnBrowseFinalReportOutputBase, 2, 1);
            this.tlpPaths.Controls.Add(this.lblTemplateBase, 0, 2);
            this.tlpPaths.Controls.Add(this.txtTemplateBase, 1, 2);
            this.tlpPaths.Controls.Add(this.btnBrowseTemplateBase, 2, 2);
            this.tlpPaths.Controls.Add(this.lblLogDirectoryBase, 0, 3);
            this.tlpPaths.Controls.Add(this.txtLogDirectoryBase, 1, 3);
            this.tlpPaths.Controls.Add(this.btnBrowseLogDirectoryBase, 2, 3);
            this.tlpPaths.Controls.Add(this.lblRawReportOutputBase, 0, 4);
            this.tlpPaths.Controls.Add(this.txtRawReportOutputBase, 1, 4);
            this.tlpPaths.Controls.Add(this.btnBrowseRawReportOutputBase, 2, 4);
            this.tlpPaths.Controls.Add(this.lblWrapperExecutable, 0, 5);
            this.tlpPaths.Controls.Add(this.txtWrapperExecutable, 1, 5);
            this.tlpPaths.Controls.Add(this.btnBrowseWrapperExecutable, 2, 5);
            this.tlpPaths.Controls.Add(this.lblReportDefinitionsFileName, 0, 6);
            this.tlpPaths.Controls.Add(this.txtReportDefinitionsFileName, 1, 6);
            this.tlpPaths.Controls.Add(this.lblFallbackLogDirectory, 0, 7);
            this.tlpPaths.Controls.Add(this.txtFallbackLogDirectory, 1, 7);
            this.tlpPaths.Controls.Add(this.btnBrowseFallbackLogDir, 2, 7);
            this.tlpPaths.Dock = System.Windows.Forms.DockStyle.Top;
            this.tlpPaths.Location = new System.Drawing.Point(10, 10);
            this.tlpPaths.Name = "tlpPaths";
            this.tlpPaths.RowCount = 8;
            for (int i = 0; i < this.tlpPaths.RowCount; i++) { this.tlpPaths.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 35F)); }
            this.tlpPaths.Size = new System.Drawing.Size(756, 280); // 8 rows * 35F
            this.tlpPaths.TabIndex = 0;
            // 
            // lblCrystalReportRptFile
            // 
            this.lblCrystalReportRptFile.Anchor = System.Windows.Forms.AnchorStyles.Right; this.lblCrystalReportRptFile.AutoSize = true; this.lblCrystalReportRptFile.Text = "Crystal Report .RPT File:"; this.lblCrystalReportRptFile.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // txtCrystalReportRptFile
            // 
            this.txtCrystalReportRptFile.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right))); this.txtCrystalReportRptFile.Name = "txtCrystalReportRptFile"; this.txtCrystalReportRptFile.Size = new System.Drawing.Size(438, 20); this.txtCrystalReportRptFile.TabIndex = 1;
            // 
            // btnBrowseCrystalReport
            // 
            this.btnBrowseCrystalReport.Anchor = System.Windows.Forms.AnchorStyles.Left; this.btnBrowseCrystalReport.Text = "Browse..."; this.btnBrowseCrystalReport.Name = "btnBrowseCrystalReport"; this.btnBrowseCrystalReport.Size = new System.Drawing.Size(85, 23); this.btnBrowseCrystalReport.TabIndex = 2; this.btnBrowseCrystalReport.UseVisualStyleBackColor = true; this.btnBrowseCrystalReport.Click += new System.EventHandler(this.btnBrowseCrystalReport_Click);
            // 
            // lblFinalReportOutputBase
            // 
            this.lblFinalReportOutputBase.Anchor = System.Windows.Forms.AnchorStyles.Right; this.lblFinalReportOutputBase.AutoSize = true; this.lblFinalReportOutputBase.Text = "Final Report Output Base Dir:"; this.lblFinalReportOutputBase.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // txtFinalReportOutputBase
            // 
            this.txtFinalReportOutputBase.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right))); this.txtFinalReportOutputBase.Name = "txtFinalReportOutputBase"; this.txtFinalReportOutputBase.Size = new System.Drawing.Size(438, 20); this.txtFinalReportOutputBase.TabIndex = 4;
            // 
            // btnBrowseFinalReportOutputBase
            // 
            this.btnBrowseFinalReportOutputBase.Anchor = System.Windows.Forms.AnchorStyles.Left; this.btnBrowseFinalReportOutputBase.Text = "Browse..."; this.btnBrowseFinalReportOutputBase.Name = "btnBrowseFinalReportOutputBase"; this.btnBrowseFinalReportOutputBase.Size = new System.Drawing.Size(85, 23); this.btnBrowseFinalReportOutputBase.TabIndex = 5; this.btnBrowseFinalReportOutputBase.UseVisualStyleBackColor = true; this.btnBrowseFinalReportOutputBase.Click += new System.EventHandler(this.btnBrowseFinalReportOutputBase_Click);
            // 
            // lblTemplateBase
            // 
            this.lblTemplateBase.Anchor = System.Windows.Forms.AnchorStyles.Right; this.lblTemplateBase.AutoSize = true; this.lblTemplateBase.Text = "Template Base Dir:"; this.lblTemplateBase.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // txtTemplateBase
            // 
            this.txtTemplateBase.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right))); this.txtTemplateBase.Name = "txtTemplateBase"; this.txtTemplateBase.Size = new System.Drawing.Size(438, 20); this.txtTemplateBase.TabIndex = 7;
            // 
            // btnBrowseTemplateBase
            // 
            this.btnBrowseTemplateBase.Anchor = System.Windows.Forms.AnchorStyles.Left; this.btnBrowseTemplateBase.Text = "Browse..."; this.btnBrowseTemplateBase.Name = "btnBrowseTemplateBase"; this.btnBrowseTemplateBase.Size = new System.Drawing.Size(85, 23); this.btnBrowseTemplateBase.TabIndex = 8; this.btnBrowseTemplateBase.UseVisualStyleBackColor = true; this.btnBrowseTemplateBase.Click += new System.EventHandler(this.btnBrowseTemplateBase_Click);
            // 
            // lblLogDirectoryBase
            // 
            this.lblLogDirectoryBase.Anchor = System.Windows.Forms.AnchorStyles.Right; this.lblLogDirectoryBase.AutoSize = true; this.lblLogDirectoryBase.Text = "Log Directory Base:"; this.lblLogDirectoryBase.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // txtLogDirectoryBase
            // 
            this.txtLogDirectoryBase.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right))); this.txtLogDirectoryBase.Name = "txtLogDirectoryBase"; this.txtLogDirectoryBase.Size = new System.Drawing.Size(438, 20); this.txtLogDirectoryBase.TabIndex = 10;
            // 
            // btnBrowseLogDirectoryBase
            // 
            this.btnBrowseLogDirectoryBase.Anchor = System.Windows.Forms.AnchorStyles.Left; this.btnBrowseLogDirectoryBase.Text = "Browse..."; this.btnBrowseLogDirectoryBase.Name = "btnBrowseLogDirectoryBase"; this.btnBrowseLogDirectoryBase.Size = new System.Drawing.Size(85, 23); this.btnBrowseLogDirectoryBase.TabIndex = 11; this.btnBrowseLogDirectoryBase.UseVisualStyleBackColor = true; this.btnBrowseLogDirectoryBase.Click += new System.EventHandler(this.btnBrowseLogDirectoryBase_Click);
            // 
            // lblRawReportOutputBase
            // 
            this.lblRawReportOutputBase.Anchor = System.Windows.Forms.AnchorStyles.Right; this.lblRawReportOutputBase.AutoSize = true; this.lblRawReportOutputBase.Text = "Raw Report Output Base Dir:"; this.lblRawReportOutputBase.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // txtRawReportOutputBase
            // 
            this.txtRawReportOutputBase.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right))); this.txtRawReportOutputBase.Name = "txtRawReportOutputBase"; this.txtRawReportOutputBase.Size = new System.Drawing.Size(438, 20); this.txtRawReportOutputBase.TabIndex = 13;
            // 
            // btnBrowseRawReportOutputBase
            // 
            this.btnBrowseRawReportOutputBase.Anchor = System.Windows.Forms.AnchorStyles.Left; this.btnBrowseRawReportOutputBase.Text = "Browse..."; this.btnBrowseRawReportOutputBase.Name = "btnBrowseRawReportOutputBase"; this.btnBrowseRawReportOutputBase.Size = new System.Drawing.Size(85, 23); this.btnBrowseRawReportOutputBase.TabIndex = 14; this.btnBrowseRawReportOutputBase.UseVisualStyleBackColor = true; this.btnBrowseRawReportOutputBase.Click += new System.EventHandler(this.btnBrowseRawReportOutputBase_Click);
            // 
            // lblWrapperExecutable
            // 
            this.lblWrapperExecutable.Anchor = System.Windows.Forms.AnchorStyles.Right; this.lblWrapperExecutable.AutoSize = true; this.lblWrapperExecutable.Text = "Wrapper Executable Path:"; this.lblWrapperExecutable.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // txtWrapperExecutable
            // 
            this.txtWrapperExecutable.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right))); this.txtWrapperExecutable.Name = "txtWrapperExecutable"; this.txtWrapperExecutable.Size = new System.Drawing.Size(438, 20); this.txtWrapperExecutable.TabIndex = 16;
            // 
            // btnBrowseWrapperExecutable
            // 
            this.btnBrowseWrapperExecutable.Anchor = System.Windows.Forms.AnchorStyles.Left; this.btnBrowseWrapperExecutable.Text = "Browse..."; this.btnBrowseWrapperExecutable.Name = "btnBrowseWrapperExecutable"; this.btnBrowseWrapperExecutable.Size = new System.Drawing.Size(85, 23); this.btnBrowseWrapperExecutable.TabIndex = 17; this.btnBrowseWrapperExecutable.UseVisualStyleBackColor = true; this.btnBrowseWrapperExecutable.Click += new System.EventHandler(this.btnBrowseWrapperExecutable_Click);
            // 
            // lblReportDefinitionsFileName
            // 
            this.lblReportDefinitionsFileName.Anchor = System.Windows.Forms.AnchorStyles.Right; this.lblReportDefinitionsFileName.AutoSize = true; this.lblReportDefinitionsFileName.Text = "Report Definitions Filename:"; this.lblReportDefinitionsFileName.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // txtReportDefinitionsFileName
            // 
            this.txtReportDefinitionsFileName.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right))); this.tlpPaths.SetColumnSpan(this.txtReportDefinitionsFileName, 2); this.txtReportDefinitionsFileName.Name = "txtReportDefinitionsFileName"; this.txtReportDefinitionsFileName.Size = new System.Drawing.Size(533, 20); this.txtReportDefinitionsFileName.TabIndex = 19; // Spans 2 columns now
            // 
            // lblFallbackLogDirectory
            // 
            this.lblFallbackLogDirectory.Anchor = System.Windows.Forms.AnchorStyles.Right; this.lblFallbackLogDirectory.AutoSize = true; this.lblFallbackLogDirectory.Text = "Fallback Log Directory:"; this.lblFallbackLogDirectory.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // txtFallbackLogDirectory
            // 
            this.txtFallbackLogDirectory.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right))); this.txtFallbackLogDirectory.Name = "txtFallbackLogDirectory"; this.txtFallbackLogDirectory.Size = new System.Drawing.Size(438, 20); this.txtFallbackLogDirectory.TabIndex = 21;
            // 
            // btnBrowseFallbackLogDir
            // 
            this.btnBrowseFallbackLogDir.Anchor = System.Windows.Forms.AnchorStyles.Left; this.btnBrowseFallbackLogDir.Text = "Browse..."; this.btnBrowseFallbackLogDir.Name = "btnBrowseFallbackLogDir"; this.btnBrowseFallbackLogDir.Size = new System.Drawing.Size(85, 23); this.btnBrowseFallbackLogDir.TabIndex = 22; this.btnBrowseFallbackLogDir.UseVisualStyleBackColor = true; this.btnBrowseFallbackLogDir.Click += new System.EventHandler(this.btnBrowseFallbackLogDir_Click);
            // 
            // tabPageSmtp
            // 
            this.tabPageSmtp.AutoScroll = true;
            this.tabPageSmtp.Controls.Add(this.tlpSmtp);
            this.tabPageSmtp.Location = new System.Drawing.Point(4, 22);
            this.tabPageSmtp.Name = "tabPageSmtp";
            this.tabPageSmtp.Padding = new System.Windows.Forms.Padding(10);
            this.tabPageSmtp.Size = new System.Drawing.Size(776, 385);
            this.tabPageSmtp.TabIndex = 2;
            this.tabPageSmtp.Text = "SMTP Configuration";
            this.tabPageSmtp.UseVisualStyleBackColor = true;
            // 
            // tlpSmtp
            // 
            this.tlpSmtp.AutoSize = true;
            this.tlpSmtp.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.tlpSmtp.ColumnCount = 2;
            this.tlpSmtp.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 180F));
            this.tlpSmtp.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tlpSmtp.Controls.Add(this.lblSmtpServer, 0, 0);
            this.tlpSmtp.Controls.Add(this.txtSmtpServer, 1, 0);
            this.tlpSmtp.Controls.Add(this.lblSmtpPort, 0, 1);
            this.tlpSmtp.Controls.Add(this.numSmtpPort, 1, 1);
            this.tlpSmtp.Controls.Add(this.lblSmtpUsername, 0, 2);
            this.tlpSmtp.Controls.Add(this.txtSmtpUsername, 1, 2);
            this.tlpSmtp.Controls.Add(this.lblSmtpPassword, 0, 3);
            this.tlpSmtp.Controls.Add(this.txtSmtpPassword, 1, 3);
            this.tlpSmtp.Controls.Add(this.chkSmtpEnableSsl, 1, 4); // Aligned to second column for better visual
            this.tlpSmtp.Controls.Add(this.lblSmtpMaxSendRetries, 0, 5);
            this.tlpSmtp.Controls.Add(this.numSmtpMaxSendRetries, 1, 5);
            this.tlpSmtp.Controls.Add(this.lblSmtpSendRetryDelayMs, 0, 6);
            this.tlpSmtp.Controls.Add(this.numSmtpSendRetryDelayMs, 1, 6);
            this.tlpSmtp.Controls.Add(this.lblSmtpTimeoutMs, 0, 7);
            this.tlpSmtp.Controls.Add(this.numSmtpTimeoutMs, 1, 7);
            this.tlpSmtp.Dock = System.Windows.Forms.DockStyle.Top;
            this.tlpSmtp.Location = new System.Drawing.Point(10, 10);
            this.tlpSmtp.Name = "tlpSmtp";
            this.tlpSmtp.RowCount = 8;
            for (int i = 0; i < this.tlpSmtp.RowCount; i++) { this.tlpSmtp.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 35F)); }
            this.tlpSmtp.Size = new System.Drawing.Size(756, 280);
            this.tlpSmtp.TabIndex = 0;
            // 
            // lblSmtpServer
            // 
            this.lblSmtpServer.Anchor = System.Windows.Forms.AnchorStyles.Right; this.lblSmtpServer.AutoSize = true; this.lblSmtpServer.Location = new System.Drawing.Point(107, 11); this.lblSmtpServer.Name = "lblSmtpServer"; this.lblSmtpServer.Size = new System.Drawing.Size(70, 13); this.lblSmtpServer.TabIndex = 0; this.lblSmtpServer.Text = "SMTP Server:"; this.lblSmtpServer.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // txtSmtpServer
            // 
            this.txtSmtpServer.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right))); this.txtSmtpServer.Location = new System.Drawing.Point(183, 7); this.txtSmtpServer.Name = "txtSmtpServer"; this.txtSmtpServer.Size = new System.Drawing.Size(570, 20); this.txtSmtpServer.TabIndex = 1;
            // 
            // lblSmtpPort
            // 
            this.lblSmtpPort.Anchor = System.Windows.Forms.AnchorStyles.Right; this.lblSmtpPort.AutoSize = true; this.lblSmtpPort.Location = new System.Drawing.Point(117, 46); this.lblSmtpPort.Name = "lblSmtpPort"; this.lblSmtpPort.Size = new System.Drawing.Size(60, 13); this.lblSmtpPort.TabIndex = 2; this.lblSmtpPort.Text = "SMTP Port:"; this.lblSmtpPort.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // numSmtpPort
            // 
            this.numSmtpPort.Anchor = System.Windows.Forms.AnchorStyles.Left; this.numSmtpPort.Location = new System.Drawing.Point(183, 42); this.numSmtpPort.Maximum = new decimal(new int[] { 65535, 0, 0, 0 }); this.numSmtpPort.Minimum = new decimal(new int[] { 1, 0, 0, 0 }); this.numSmtpPort.Name = "numSmtpPort"; this.numSmtpPort.Size = new System.Drawing.Size(120, 20); this.numSmtpPort.TabIndex = 3; this.numSmtpPort.Value = new decimal(new int[] { 25, 0, 0, 0 });
            // 
            // lblSmtpUsername
            // 
            this.lblSmtpUsername.Anchor = System.Windows.Forms.AnchorStyles.Right; this.lblSmtpUsername.AutoSize = true; this.lblSmtpUsername.Location = new System.Drawing.Point(88, 81); this.lblSmtpUsername.Name = "lblSmtpUsername"; this.lblSmtpUsername.Size = new System.Drawing.Size(89, 13); this.lblSmtpUsername.TabIndex = 4; this.lblSmtpUsername.Text = "SMTP Username:"; this.lblSmtpUsername.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // txtSmtpUsername
            // 
            this.txtSmtpUsername.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right))); this.txtSmtpUsername.Location = new System.Drawing.Point(183, 77); this.txtSmtpUsername.Name = "txtSmtpUsername"; this.txtSmtpUsername.Size = new System.Drawing.Size(570, 20); this.txtSmtpUsername.TabIndex = 5;
            // 
            // lblSmtpPassword
            // 
            this.lblSmtpPassword.Anchor = System.Windows.Forms.AnchorStyles.Right; this.lblSmtpPassword.AutoSize = true; this.lblSmtpPassword.Location = new System.Drawing.Point(90, 116); this.lblSmtpPassword.Name = "lblSmtpPassword"; this.lblSmtpPassword.Size = new System.Drawing.Size(87, 13); this.lblSmtpPassword.TabIndex = 6; this.lblSmtpPassword.Text = "SMTP Password:"; this.lblSmtpPassword.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // txtSmtpPassword
            // 
            this.txtSmtpPassword.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right))); this.txtSmtpPassword.Location = new System.Drawing.Point(183, 112); this.txtSmtpPassword.Name = "txtSmtpPassword"; this.txtSmtpPassword.Size = new System.Drawing.Size(570, 20); this.txtSmtpPassword.TabIndex = 7; this.txtSmtpPassword.UseSystemPasswordChar = true;
            // 
            // chkSmtpEnableSsl
            // 
            this.chkSmtpEnableSsl.Anchor = System.Windows.Forms.AnchorStyles.Left; this.chkSmtpEnableSsl.AutoSize = true; this.chkSmtpEnableSsl.Location = new System.Drawing.Point(183, 150); this.chkSmtpEnableSsl.Name = "chkSmtpEnableSsl"; this.chkSmtpEnableSsl.Size = new System.Drawing.Size(103, 17); this.chkSmtpEnableSsl.TabIndex = 8; this.chkSmtpEnableSsl.Text = "Enable SSL/TLS"; this.chkSmtpEnableSsl.UseVisualStyleBackColor = true;
            // 
            // lblSmtpMaxSendRetries
            // 
            this.lblSmtpMaxSendRetries.Anchor = System.Windows.Forms.AnchorStyles.Right; this.lblSmtpMaxSendRetries.AutoSize = true; this.lblSmtpMaxSendRetries.Location = new System.Drawing.Point(82, 186); this.lblSmtpMaxSendRetries.Name = "lblSmtpMaxSendRetries"; this.lblSmtpMaxSendRetries.Size = new System.Drawing.Size(95, 13); this.lblSmtpMaxSendRetries.TabIndex = 9; this.lblSmtpMaxSendRetries.Text = "Max Send Retries:"; this.lblSmtpMaxSendRetries.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // numSmtpMaxSendRetries
            // 
            this.numSmtpMaxSendRetries.Anchor = System.Windows.Forms.AnchorStyles.Left; this.numSmtpMaxSendRetries.Location = new System.Drawing.Point(183, 182); this.numSmtpMaxSendRetries.Name = "numSmtpMaxSendRetries"; this.numSmtpMaxSendRetries.Size = new System.Drawing.Size(120, 20); this.numSmtpMaxSendRetries.TabIndex = 10; this.numSmtpMaxSendRetries.Value = new decimal(new int[] { 3, 0, 0, 0 });
            // 
            // lblSmtpSendRetryDelayMs
            // 
            this.lblSmtpSendRetryDelayMs.Anchor = System.Windows.Forms.AnchorStyles.Right; this.lblSmtpSendRetryDelayMs.AutoSize = true; this.lblSmtpSendRetryDelayMs.Location = new System.Drawing.Point(61, 221); this.lblSmtpSendRetryDelayMs.Name = "lblSmtpSendRetryDelayMs"; this.lblSmtpSendRetryDelayMs.Size = new System.Drawing.Size(116, 13); this.lblSmtpSendRetryDelayMs.TabIndex = 11; this.lblSmtpSendRetryDelayMs.Text = "Send Retry Delay (ms):"; this.lblSmtpSendRetryDelayMs.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // numSmtpSendRetryDelayMs
            // 
            this.numSmtpSendRetryDelayMs.Anchor = System.Windows.Forms.AnchorStyles.Left; this.numSmtpSendRetryDelayMs.Location = new System.Drawing.Point(183, 217); this.numSmtpSendRetryDelayMs.Maximum = new decimal(new int[] { 60000, 0, 0, 0 }); this.numSmtpSendRetryDelayMs.Name = "numSmtpSendRetryDelayMs"; this.numSmtpSendRetryDelayMs.Size = new System.Drawing.Size(120, 20); this.numSmtpSendRetryDelayMs.TabIndex = 12; this.numSmtpSendRetryDelayMs.Value = new decimal(new int[] { 2000, 0, 0, 0 });
            // 
            // lblSmtpTimeoutMs
            // 
            this.lblSmtpTimeoutMs.Anchor = System.Windows.Forms.AnchorStyles.Right; this.lblSmtpTimeoutMs.AutoSize = true; this.lblSmtpTimeoutMs.Location = new System.Drawing.Point(105, 256); this.lblSmtpTimeoutMs.Name = "lblSmtpTimeoutMs"; this.lblSmtpTimeoutMs.Size = new System.Drawing.Size(72, 13); this.lblSmtpTimeoutMs.TabIndex = 13; this.lblSmtpTimeoutMs.Text = "Timeout (ms):"; this.lblSmtpTimeoutMs.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // numSmtpTimeoutMs
            // 
            this.numSmtpTimeoutMs.Anchor = System.Windows.Forms.AnchorStyles.Left; this.numSmtpTimeoutMs.Location = new System.Drawing.Point(183, 252); this.numSmtpTimeoutMs.Maximum = new decimal(new int[] { 300000, 0, 0, 0 }); this.numSmtpTimeoutMs.Minimum = new decimal(new int[] { 1000, 0, 0, 0 }); this.numSmtpTimeoutMs.Name = "numSmtpTimeoutMs"; this.numSmtpTimeoutMs.Size = new System.Drawing.Size(120, 20); this.numSmtpTimeoutMs.TabIndex = 14; this.numSmtpTimeoutMs.Value = new decimal(new int[] { 30000, 0, 0, 0 });
            // 
            // tabPageEmailDefaults
            // 
            this.tabPageEmailDefaults.AutoScroll = true;
            this.tabPageEmailDefaults.Controls.Add(this.tlpEmailDefaults);
            this.tabPageEmailDefaults.Location = new System.Drawing.Point(4, 22);
            this.tabPageEmailDefaults.Name = "tabPageEmailDefaults";
            this.tabPageEmailDefaults.Padding = new System.Windows.Forms.Padding(10);
            this.tabPageEmailDefaults.Size = new System.Drawing.Size(776, 385);
            this.tabPageEmailDefaults.TabIndex = 3;
            this.tabPageEmailDefaults.Text = "Email Defaults";
            this.tabPageEmailDefaults.UseVisualStyleBackColor = true;
            // 
            // tlpEmailDefaults
            // 
            this.tlpEmailDefaults.AutoSize = true;
            this.tlpEmailDefaults.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.tlpEmailDefaults.ColumnCount = 2;
            this.tlpEmailDefaults.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 200F));
            this.tlpEmailDefaults.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tlpEmailDefaults.Controls.Add(this.lblSenderAddress, 0, 0);
            this.tlpEmailDefaults.Controls.Add(this.txtSenderAddress, 1, 0);
            this.tlpEmailDefaults.Controls.Add(this.lblSenderDisplayName, 0, 1);
            this.tlpEmailDefaults.Controls.Add(this.txtSenderDisplayName, 1, 1);
            this.tlpEmailDefaults.Controls.Add(this.lblMaxAttachmentSizeBytes, 0, 2);
            this.tlpEmailDefaults.Controls.Add(this.numMaxAttachmentSizeBytes, 1, 2);
            this.tlpEmailDefaults.Controls.Add(this.lblDefaultEmailSignature, 0, 3);
            this.tlpEmailDefaults.Controls.Add(this.txtDefaultEmailSignature, 1, 3);
            this.tlpEmailDefaults.Controls.Add(this.lblAttachmentReadMaxRetries, 0, 5); // Corrected row index
            this.tlpEmailDefaults.Controls.Add(this.numAttachmentReadMaxRetries, 1, 5); // Corrected row index
            this.tlpEmailDefaults.Controls.Add(this.lblAttachmentReadDelayMs, 0, 6);    // Corrected row index
            this.tlpEmailDefaults.Controls.Add(this.numAttachmentReadDelayMs, 1, 6);   // Corrected row index
            this.tlpEmailDefaults.Dock = System.Windows.Forms.DockStyle.Top;
            this.tlpEmailDefaults.Location = new System.Drawing.Point(10, 10);
            this.tlpEmailDefaults.Name = "tlpEmailDefaults";
            this.tlpEmailDefaults.RowCount = 7;
            this.tlpEmailDefaults.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 35F)); // Row 0
            this.tlpEmailDefaults.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 35F)); // Row 1
            this.tlpEmailDefaults.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 35F)); // Row 2
            this.tlpEmailDefaults.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 35F)); // Row 3 (Start of Signature)
            this.tlpEmailDefaults.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 35F)); // Row 4 (End of Signature)
            this.tlpEmailDefaults.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 35F)); // Row 5
            this.tlpEmailDefaults.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 35F)); // Row 6
            // Removed spacer row style to make it fixed, can add back if needed: this.tlpEmailDefaults.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tlpEmailDefaults.Size = new System.Drawing.Size(756, 245); // 7 rows * 35F
            this.tlpEmailDefaults.TabIndex = 0;
            // 
            // lblSenderAddress
            // 
            this.lblSenderAddress.Anchor = System.Windows.Forms.AnchorStyles.Right; this.lblSenderAddress.AutoSize = true; this.lblSenderAddress.Location = new System.Drawing.Point(85, 11); this.lblSenderAddress.Name = "lblSenderAddress"; this.lblSenderAddress.Size = new System.Drawing.Size(112, 13); this.lblSenderAddress.TabIndex = 0; this.lblSenderAddress.Text = "Sender Email Address:"; this.lblSenderAddress.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // txtSenderAddress
            // 
            this.txtSenderAddress.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right))); this.txtSenderAddress.Location = new System.Drawing.Point(203, 7); this.txtSenderAddress.Name = "txtSenderAddress"; this.txtSenderAddress.Size = new System.Drawing.Size(550, 20); this.txtSenderAddress.TabIndex = 1;
            // 
            // lblSenderDisplayName
            // 
            this.lblSenderDisplayName.Anchor = System.Windows.Forms.AnchorStyles.Right; this.lblSenderDisplayName.AutoSize = true; this.lblSenderDisplayName.Location = new System.Drawing.Point(85, 46); this.lblSenderDisplayName.Name = "lblSenderDisplayName"; this.lblSenderDisplayName.Size = new System.Drawing.Size(112, 13); this.lblSenderDisplayName.TabIndex = 2; this.lblSenderDisplayName.Text = "Sender Display Name:"; this.lblSenderDisplayName.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // txtSenderDisplayName
            // 
            this.txtSenderDisplayName.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right))); this.txtSenderDisplayName.Location = new System.Drawing.Point(203, 42); this.txtSenderDisplayName.Name = "txtSenderDisplayName"; this.txtSenderDisplayName.Size = new System.Drawing.Size(550, 20); this.txtSenderDisplayName.TabIndex = 3;
            // 
            // lblMaxAttachmentSizeBytes
            // 
            this.lblMaxAttachmentSizeBytes.Anchor = System.Windows.Forms.AnchorStyles.Right; this.lblMaxAttachmentSizeBytes.AutoSize = true; this.lblMaxAttachmentSizeBytes.Location = new System.Drawing.Point(56, 81); this.lblMaxAttachmentSizeBytes.Name = "lblMaxAttachmentSizeBytes"; this.lblMaxAttachmentSizeBytes.Size = new System.Drawing.Size(141, 13); this.lblMaxAttachmentSizeBytes.TabIndex = 4; this.lblMaxAttachmentSizeBytes.Text = "Max Attachment Size (Bytes):"; this.lblMaxAttachmentSizeBytes.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // numMaxAttachmentSizeBytes
            // 
            this.numMaxAttachmentSizeBytes.Anchor = System.Windows.Forms.AnchorStyles.Left; this.numMaxAttachmentSizeBytes.Location = new System.Drawing.Point(203, 77); this.numMaxAttachmentSizeBytes.Maximum = new decimal(new int[] { 52428800, 0, 0, 0 }); this.numMaxAttachmentSizeBytes.Name = "numMaxAttachmentSizeBytes"; this.numMaxAttachmentSizeBytes.Size = new System.Drawing.Size(120, 20); this.numMaxAttachmentSizeBytes.TabIndex = 5; this.numMaxAttachmentSizeBytes.ThousandsSeparator = true;
            // 
            // lblDefaultEmailSignature
            // 
            this.lblDefaultEmailSignature.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right))); this.lblDefaultEmailSignature.AutoSize = true; this.lblDefaultEmailSignature.Location = new System.Drawing.Point(82, 105); this.lblDefaultEmailSignature.Name = "lblDefaultEmailSignature"; this.lblDefaultEmailSignature.Padding = new System.Windows.Forms.Padding(0, 8, 3, 0); this.lblDefaultEmailSignature.Size = new System.Drawing.Size(115, 18); this.lblDefaultEmailSignature.TabIndex = 6; this.lblDefaultEmailSignature.Text = "Default Email Signature:"; this.lblDefaultEmailSignature.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // txtDefaultEmailSignature
            // 
            this.txtDefaultEmailSignature.Dock = System.Windows.Forms.DockStyle.Fill; // Changed to Fill
            this.txtDefaultEmailSignature.Location = new System.Drawing.Point(203, 108);
            this.txtDefaultEmailSignature.Multiline = true;
            this.txtDefaultEmailSignature.Name = "txtDefaultEmailSignature";
            this.tlpEmailDefaults.SetRowSpan(this.txtDefaultEmailSignature, 2);
            this.txtDefaultEmailSignature.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtDefaultEmailSignature.Size = new System.Drawing.Size(550, 64); // Height for 2 rows (35*2 - margins)
            this.txtDefaultEmailSignature.TabIndex = 7;
            // 
            // lblAttachmentReadMaxRetries
            // 
            this.lblAttachmentReadMaxRetries.Anchor = System.Windows.Forms.AnchorStyles.Right; this.lblAttachmentReadMaxRetries.AutoSize = true; this.lblAttachmentReadMaxRetries.Location = new System.Drawing.Point(49, 186); this.lblAttachmentReadMaxRetries.Name = "lblAttachmentReadMaxRetries"; this.lblAttachmentReadMaxRetries.Size = new System.Drawing.Size(148, 13); this.lblAttachmentReadMaxRetries.TabIndex = 8; this.lblAttachmentReadMaxRetries.Text = "Attachment Read Max Retries:"; this.lblAttachmentReadMaxRetries.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // numAttachmentReadMaxRetries
            // 
            this.numAttachmentReadMaxRetries.Anchor = System.Windows.Forms.AnchorStyles.Left; this.numAttachmentReadMaxRetries.Location = new System.Drawing.Point(203, 182); this.numAttachmentReadMaxRetries.Name = "numAttachmentReadMaxRetries"; this.numAttachmentReadMaxRetries.Size = new System.Drawing.Size(120, 20); this.numAttachmentReadMaxRetries.TabIndex = 9;
            // 
            // lblAttachmentReadDelayMs
            // 
            this.lblAttachmentReadDelayMs.Anchor = System.Windows.Forms.AnchorStyles.Right; this.lblAttachmentReadDelayMs.AutoSize = true; this.lblAttachmentReadDelayMs.Location = new System.Drawing.Point(49, 221); this.lblAttachmentReadDelayMs.Name = "lblAttachmentReadDelayMs"; this.lblAttachmentReadDelayMs.Size = new System.Drawing.Size(148, 13); this.lblAttachmentReadDelayMs.TabIndex = 10; this.lblAttachmentReadDelayMs.Text = "Attachment Read Delay (ms):"; this.lblAttachmentReadDelayMs.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // numAttachmentReadDelayMs
            // 
            this.numAttachmentReadDelayMs.Anchor = System.Windows.Forms.AnchorStyles.Left; this.numAttachmentReadDelayMs.Location = new System.Drawing.Point(203, 217); this.numAttachmentReadDelayMs.Maximum = new decimal(new int[] { 10000, 0, 0, 0 }); this.numAttachmentReadDelayMs.Name = "numAttachmentReadDelayMs"; this.numAttachmentReadDelayMs.Size = new System.Drawing.Size(120, 20); this.numAttachmentReadDelayMs.TabIndex = 11;
            // 
            // tabPageLogging
            // 
            this.tabPageLogging.AutoScroll = true;
            this.tabPageLogging.Controls.Add(this.tlpLogging);
            this.tabPageLogging.Location = new System.Drawing.Point(4, 22);
            this.tabPageLogging.Name = "tabPageLogging";
            this.tabPageLogging.Padding = new System.Windows.Forms.Padding(10);
            this.tabPageLogging.Size = new System.Drawing.Size(776, 385);
            this.tabPageLogging.TabIndex = 4;
            this.tabPageLogging.Text = "Logging";
            this.tabPageLogging.UseVisualStyleBackColor = true;
            // 
            // tlpLogging
            // 
            this.tlpLogging.AutoSize = true;
            this.tlpLogging.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.tlpLogging.ColumnCount = 2;
            this.tlpLogging.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 200F));
            this.tlpLogging.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tlpLogging.Controls.Add(this.lblDefaultLogLevel, 0, 0);
            this.tlpLogging.Controls.Add(this.cmbDefaultLogLevel, 1, 0);
            this.tlpLogging.Controls.Add(this.lblDebugBuildLogLevel, 0, 1);
            this.tlpLogging.Controls.Add(this.cmbDebugBuildLogLevel, 1, 1);
            this.tlpLogging.Controls.Add(this.lblLogArchiveOlderThanDays, 0, 2);
            this.tlpLogging.Controls.Add(this.numLogArchiveOlderThanDays, 1, 2);
            this.tlpLogging.Controls.Add(this.lblLogFileNameFormat, 0, 3);
            this.tlpLogging.Controls.Add(this.txtLogFileNameFormat, 1, 3);
            this.tlpLogging.Dock = System.Windows.Forms.DockStyle.Top;
            this.tlpLogging.Location = new System.Drawing.Point(10, 10);
            this.tlpLogging.Name = "tlpLogging";
            this.tlpLogging.RowCount = 4;
            for (int i = 0; i < this.tlpLogging.RowCount; i++) { this.tlpLogging.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 35F)); }
            this.tlpLogging.Size = new System.Drawing.Size(756, 140); // 4 rows * 35F
            this.tlpLogging.TabIndex = 0;
            // 
            // lblDefaultLogLevel
            // 
            this.lblDefaultLogLevel.Anchor = System.Windows.Forms.AnchorStyles.Right; this.lblDefaultLogLevel.AutoSize = true; this.lblDefaultLogLevel.Location = new System.Drawing.Point(61, 11); this.lblDefaultLogLevel.Name = "lblDefaultLogLevel"; this.lblDefaultLogLevel.Size = new System.Drawing.Size(136, 13); this.lblDefaultLogLevel.TabIndex = 0; this.lblDefaultLogLevel.Text = "Default Log Level (Release):"; this.lblDefaultLogLevel.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // cmbDefaultLogLevel
            // 
            this.cmbDefaultLogLevel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right))); this.cmbDefaultLogLevel.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList; this.cmbDefaultLogLevel.FormattingEnabled = true; this.cmbDefaultLogLevel.Location = new System.Drawing.Point(203, 7); this.cmbDefaultLogLevel.Name = "cmbDefaultLogLevel"; this.cmbDefaultLogLevel.Size = new System.Drawing.Size(550, 21); this.cmbDefaultLogLevel.TabIndex = 1;
            // 
            // lblDebugBuildLogLevel
            // 
            this.lblDebugBuildLogLevel.Anchor = System.Windows.Forms.AnchorStyles.Right; this.lblDebugBuildLogLevel.AutoSize = true; this.lblDebugBuildLogLevel.Location = new System.Drawing.Point(89, 46); this.lblDebugBuildLogLevel.Name = "lblDebugBuildLogLevel"; this.lblDebugBuildLogLevel.Size = new System.Drawing.Size(108, 13); this.lblDebugBuildLogLevel.TabIndex = 2; this.lblDebugBuildLogLevel.Text = "Debug Build Log Level:"; this.lblDebugBuildLogLevel.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // cmbDebugBuildLogLevel
            // 
            this.cmbDebugBuildLogLevel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right))); this.cmbDebugBuildLogLevel.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList; this.cmbDebugBuildLogLevel.FormattingEnabled = true; this.cmbDebugBuildLogLevel.Location = new System.Drawing.Point(203, 42); this.cmbDebugBuildLogLevel.Name = "cmbDebugBuildLogLevel"; this.cmbDebugBuildLogLevel.Size = new System.Drawing.Size(550, 21); this.cmbDebugBuildLogLevel.TabIndex = 3;
            // 
            // lblLogArchiveOlderThanDays
            // 
            this.lblLogArchiveOlderThanDays.Anchor = System.Windows.Forms.AnchorStyles.Right; this.lblLogArchiveOlderThanDays.AutoSize = true; this.lblLogArchiveOlderThanDays.Location = new System.Drawing.Point(48, 81); this.lblLogArchiveOlderThanDays.Name = "lblLogArchiveOlderThanDays"; this.lblLogArchiveOlderThanDays.Size = new System.Drawing.Size(149, 13); this.lblLogArchiveOlderThanDays.TabIndex = 4; this.lblLogArchiveOlderThanDays.Text = "Archive Logs Older Than (Days):"; this.lblLogArchiveOlderThanDays.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // numLogArchiveOlderThanDays
            // 
            this.numLogArchiveOlderThanDays.Anchor = System.Windows.Forms.AnchorStyles.Left; this.numLogArchiveOlderThanDays.Location = new System.Drawing.Point(203, 77); this.numLogArchiveOlderThanDays.Maximum = new decimal(new int[] { 3650, 0, 0, 0 }); this.numLogArchiveOlderThanDays.Minimum = new decimal(new int[] { 1, 0, 0, 0 }); this.numLogArchiveOlderThanDays.Name = "numLogArchiveOlderThanDays"; this.numLogArchiveOlderThanDays.Size = new System.Drawing.Size(120, 20); this.numLogArchiveOlderThanDays.TabIndex = 5; this.numLogArchiveOlderThanDays.Value = new decimal(new int[] { 7, 0, 0, 0 });
            // 
            // lblLogFileNameFormat
            // 
            this.lblLogFileNameFormat.Anchor = System.Windows.Forms.AnchorStyles.Right; this.lblLogFileNameFormat.AutoSize = true; this.lblLogFileNameFormat.Location = new System.Drawing.Point(97, 116); this.lblLogFileNameFormat.Name = "lblLogFileNameFormat"; this.lblLogFileNameFormat.Size = new System.Drawing.Size(100, 13); this.lblLogFileNameFormat.TabIndex = 6; this.lblLogFileNameFormat.Text = "Log Filename Format:"; this.lblLogFileNameFormat.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // txtLogFileNameFormat
            // 
            this.txtLogFileNameFormat.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right))); this.txtLogFileNameFormat.Location = new System.Drawing.Point(203, 112); this.txtLogFileNameFormat.Name = "txtLogFileNameFormat"; this.txtLogFileNameFormat.Size = new System.Drawing.Size(550, 20); this.txtLogFileNameFormat.TabIndex = 7;
            // 
            // tabPageOperational
            // 
            this.tabPageOperational.AutoScroll = true;
            this.tabPageOperational.Controls.Add(this.tlpOperational);
            this.tabPageOperational.Location = new System.Drawing.Point(4, 22);
            this.tabPageOperational.Name = "tabPageOperational";
            this.tabPageOperational.Padding = new System.Windows.Forms.Padding(10);
            this.tabPageOperational.Size = new System.Drawing.Size(776, 385);
            this.tabPageOperational.TabIndex = 5;
            this.tabPageOperational.Text = "Operational Parameters";
            this.tabPageOperational.UseVisualStyleBackColor = true;
            // 
            // tlpOperational
            // 
            this.tlpOperational.AutoSize = true;
            this.tlpOperational.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.tlpOperational.ColumnCount = 2;
            this.tlpOperational.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 270F));
            this.tlpOperational.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tlpOperational.Controls.Add(this.lblArchiveRawReportsOlderThanDays, 0, 0);
            this.tlpOperational.Controls.Add(this.numArchiveRawReportsOlderThanDays, 1, 0);
            this.tlpOperational.Controls.Add(this.lblReportArchiveFolderName, 0, 1);
            this.tlpOperational.Controls.Add(this.txtReportArchiveFolderName, 1, 1);
            this.tlpOperational.Controls.Add(this.lblProcessTimeoutMinutes, 0, 2);
            this.tlpOperational.Controls.Add(this.numProcessTimeoutMinutes, 1, 2);
            this.tlpOperational.Controls.Add(this.lblFinancialYearStartMonth, 0, 3);
            this.tlpOperational.Controls.Add(this.numFinancialYearStartMonth, 1, 3);
            this.tlpOperational.Controls.Add(this.lblFinancialYearStartDay, 0, 4);
            this.tlpOperational.Controls.Add(this.numFinancialYearStartDay, 1, 4);
            this.tlpOperational.Controls.Add(this.lblDaily5Day1kFilteringThreshold, 0, 5);
            this.tlpOperational.Controls.Add(this.numDaily5Day1kFilteringThreshold, 1, 5);
            this.tlpOperational.Controls.Add(this.lblGeneralFileOpMaxRetries, 0, 6);
            this.tlpOperational.Controls.Add(this.numGeneralFileOpMaxRetries, 1, 6);
            this.tlpOperational.Controls.Add(this.lblGeneralFileOpDelayMs, 0, 7);
            this.tlpOperational.Controls.Add(this.numGeneralFileOpDelayMs, 1, 7);
            this.tlpOperational.Controls.Add(this.lblRawDataSourceSheet, 0, 8);
            this.tlpOperational.Controls.Add(this.txtRawDataSourceSheet, 1, 8);
            this.tlpOperational.Controls.Add(this.lblTemplateDataCopySheet, 0, 9);
            this.tlpOperational.Controls.Add(this.txtTemplateDataCopySheet, 1, 9);
            this.tlpOperational.Controls.Add(this.lblTemplateAnalysisSheet, 0, 10);
            this.tlpOperational.Controls.Add(this.txtTemplateAnalysisSheet, 1, 10);
            this.tlpOperational.Controls.Add(this.lblPowerBiDataSheet, 0, 11);
            this.tlpOperational.Controls.Add(this.txtPowerBiDataSheet, 1, 11);
            this.tlpOperational.Controls.Add(this.lblMonthlyOrderPivotSheet, 0, 12);
            this.tlpOperational.Controls.Add(this.txtMonthlyOrderPivotSheet, 1, 12);
            this.tlpOperational.Controls.Add(this.lblMonthlyEstimatePivotSheet, 0, 13);
            this.tlpOperational.Controls.Add(this.txtMonthlyEstimatePivotSheet, 1, 13);
            this.tlpOperational.Controls.Add(this.lblMonthlyOrderPivotName, 0, 14);
            this.tlpOperational.Controls.Add(this.txtMonthlyOrderPivotName, 1, 14);
            this.tlpOperational.Controls.Add(this.lblMonthlyEstimatePivotName, 0, 15);
            this.tlpOperational.Controls.Add(this.txtMonthlyEstimatePivotName, 1, 15);
            this.tlpOperational.Controls.Add(this.lblFolderNamingDaily, 0, 16);
            this.tlpOperational.Controls.Add(this.txtFolderNamingDaily, 1, 16);
            this.tlpOperational.Controls.Add(this.lblFolderNamingDaily5Day1k, 0, 17);
            this.tlpOperational.Controls.Add(this.txtFolderNamingDaily5Day1k, 1, 17);
            this.tlpOperational.Controls.Add(this.lblFolderNamingWeekly, 0, 18);
            this.tlpOperational.Controls.Add(this.txtFolderNamingWeekly, 1, 18);
            this.tlpOperational.Controls.Add(this.lblFolderNamingMonthly, 0, 19);
            this.tlpOperational.Controls.Add(this.txtFolderNamingMonthly, 1, 19);
            this.tlpOperational.Controls.Add(this.lblFolderNamingQuarterly, 0, 20);
            this.tlpOperational.Controls.Add(this.txtFolderNamingQuarterly, 1, 20);
            this.tlpOperational.Controls.Add(this.lblFolderNamingAnnual, 0, 21);
            this.tlpOperational.Controls.Add(this.txtFolderNamingAnnual, 1, 21);
            this.tlpOperational.Controls.Add(this.lblFolderNamingCustom, 0, 22);
            this.tlpOperational.Controls.Add(this.txtFolderNamingCustom, 1, 22);
            this.tlpOperational.Controls.Add(this.lblFolderNamingOther, 0, 23);
            this.tlpOperational.Controls.Add(this.txtFolderNamingOther, 1, 23);
            this.tlpOperational.Dock = System.Windows.Forms.DockStyle.Top;
            this.tlpOperational.Location = new System.Drawing.Point(10, 10);
            this.tlpOperational.Name = "tlpOperational";
            this.tlpOperational.RowCount = 24;
            for (int i = 0; i < this.tlpOperational.RowCount; i++) { this.tlpOperational.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 35F)); } // Increased row height
            this.tlpOperational.Size = new System.Drawing.Size(756, 840); // 24 rows * 35F
            this.tlpOperational.TabIndex = 0;
            // 
            // lblArchiveRawReportsOlderThanDays
            // 
            this.lblArchiveRawReportsOlderThanDays.Anchor = System.Windows.Forms.AnchorStyles.Right; this.lblArchiveRawReportsOlderThanDays.AutoSize = true; this.lblArchiveRawReportsOlderThanDays.Location = new System.Drawing.Point(50, 11); this.lblArchiveRawReportsOlderThanDays.Name = "lblArchiveRawReportsOlderThanDays"; this.lblArchiveRawReportsOlderThanDays.Size = new System.Drawing.Size(217, 13); this.lblArchiveRawReportsOlderThanDays.TabIndex = 0; this.lblArchiveRawReportsOlderThanDays.Text = "Archive Raw Reports Older Than (Days):"; this.lblArchiveRawReportsOlderThanDays.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // numArchiveRawReportsOlderThanDays
            // 
            this.numArchiveRawReportsOlderThanDays.Anchor = System.Windows.Forms.AnchorStyles.Left; this.numArchiveRawReportsOlderThanDays.Location = new System.Drawing.Point(273, 7); this.numArchiveRawReportsOlderThanDays.Maximum = new decimal(new int[] { 3650, 0, 0, 0 }); this.numArchiveRawReportsOlderThanDays.Minimum = new decimal(new int[] { 1, 0, 0, 0 }); this.numArchiveRawReportsOlderThanDays.Name = "numArchiveRawReportsOlderThanDays"; this.numArchiveRawReportsOlderThanDays.Size = new System.Drawing.Size(120, 20); this.numArchiveRawReportsOlderThanDays.TabIndex = 1; this.numArchiveRawReportsOlderThanDays.Value = new decimal(new int[] { 30, 0, 0, 0 });
            // 
            // lblReportArchiveFolderName
            // 
            this.lblReportArchiveFolderName.Anchor = System.Windows.Forms.AnchorStyles.Right; this.lblReportArchiveFolderName.AutoSize = true; this.lblReportArchiveFolderName.Location = new System.Drawing.Point(130, 46); this.lblReportArchiveFolderName.Name = "lblReportArchiveFolderName"; this.lblReportArchiveFolderName.Size = new System.Drawing.Size(137, 13); this.lblReportArchiveFolderName.TabIndex = 2; this.lblReportArchiveFolderName.Text = "Report Archive Folder Name:"; this.lblReportArchiveFolderName.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // txtReportArchiveFolderName
            // 
            this.txtReportArchiveFolderName.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right))); this.txtReportArchiveFolderName.Location = new System.Drawing.Point(273, 42); this.txtReportArchiveFolderName.Name = "txtReportArchiveFolderName"; this.txtReportArchiveFolderName.Size = new System.Drawing.Size(480, 20); this.txtReportArchiveFolderName.TabIndex = 3;
            // 
            // lblProcessTimeoutMinutes
            // 
            this.lblProcessTimeoutMinutes.Anchor = System.Windows.Forms.AnchorStyles.Right; this.lblProcessTimeoutMinutes.AutoSize = true; this.lblProcessTimeoutMinutes.Location = new System.Drawing.Point(132, 81); this.lblProcessTimeoutMinutes.Name = "lblProcessTimeoutMinutes"; this.lblProcessTimeoutMinutes.Size = new System.Drawing.Size(135, 13); this.lblProcessTimeoutMinutes.TabIndex = 4; this.lblProcessTimeoutMinutes.Text = "Process Timeout (Minutes):"; this.lblProcessTimeoutMinutes.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // numProcessTimeoutMinutes
            // 
            this.numProcessTimeoutMinutes.Anchor = System.Windows.Forms.AnchorStyles.Left; this.numProcessTimeoutMinutes.Location = new System.Drawing.Point(273, 77); this.numProcessTimeoutMinutes.Maximum = new decimal(new int[] { 120, 0, 0, 0 }); this.numProcessTimeoutMinutes.Minimum = new decimal(new int[] { 1, 0, 0, 0 }); this.numProcessTimeoutMinutes.Name = "numProcessTimeoutMinutes"; this.numProcessTimeoutMinutes.Size = new System.Drawing.Size(120, 20); this.numProcessTimeoutMinutes.TabIndex = 5;
            // 
            // lblFinancialYearStartMonth
            // 
            this.lblFinancialYearStartMonth.Anchor = System.Windows.Forms.AnchorStyles.Right; this.lblFinancialYearStartMonth.AutoSize = true; this.lblFinancialYearStartMonth.Location = new System.Drawing.Point(109, 116); this.lblFinancialYearStartMonth.Name = "lblFinancialYearStartMonth"; this.lblFinancialYearStartMonth.Size = new System.Drawing.Size(158, 13); this.lblFinancialYearStartMonth.TabIndex = 6; this.lblFinancialYearStartMonth.Text = "Financial Year Start Month (1-12):"; this.lblFinancialYearStartMonth.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // numFinancialYearStartMonth
            // 
            this.numFinancialYearStartMonth.Anchor = System.Windows.Forms.AnchorStyles.Left; this.numFinancialYearStartMonth.Location = new System.Drawing.Point(273, 112); this.numFinancialYearStartMonth.Maximum = new decimal(new int[] { 12, 0, 0, 0 }); this.numFinancialYearStartMonth.Minimum = new decimal(new int[] { 1, 0, 0, 0 }); this.numFinancialYearStartMonth.Name = "numFinancialYearStartMonth"; this.numFinancialYearStartMonth.Size = new System.Drawing.Size(120, 20); this.numFinancialYearStartMonth.TabIndex = 7;
            // 
            // lblFinancialYearStartDay
            // 
            this.lblFinancialYearStartDay.Anchor = System.Windows.Forms.AnchorStyles.Right; this.lblFinancialYearStartDay.AutoSize = true; this.lblFinancialYearStartDay.Location = new System.Drawing.Point(123, 151); this.lblFinancialYearStartDay.Name = "lblFinancialYearStartDay"; this.lblFinancialYearStartDay.Size = new System.Drawing.Size(144, 13); this.lblFinancialYearStartDay.TabIndex = 8; this.lblFinancialYearStartDay.Text = "Financial Year Start Day (1-31):"; this.lblFinancialYearStartDay.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // numFinancialYearStartDay
            // 
            this.numFinancialYearStartDay.Anchor = System.Windows.Forms.AnchorStyles.Left; this.numFinancialYearStartDay.Location = new System.Drawing.Point(273, 147); this.numFinancialYearStartDay.Maximum = new decimal(new int[] { 31, 0, 0, 0 }); this.numFinancialYearStartDay.Minimum = new decimal(new int[] { 1, 0, 0, 0 }); this.numFinancialYearStartDay.Name = "numFinancialYearStartDay"; this.numFinancialYearStartDay.Size = new System.Drawing.Size(120, 20); this.numFinancialYearStartDay.TabIndex = 9;
            // 
            // lblDaily5Day1kFilteringThreshold
            // 
            this.lblDaily5Day1kFilteringThreshold.Anchor = System.Windows.Forms.AnchorStyles.Right; this.lblDaily5Day1kFilteringThreshold.AutoSize = true; this.lblDaily5Day1kFilteringThreshold.Location = new System.Drawing.Point(84, 186); this.lblDaily5Day1kFilteringThreshold.Name = "lblDaily5Day1kFilteringThreshold"; this.lblDaily5Day1kFilteringThreshold.Size = new System.Drawing.Size(183, 13); this.lblDaily5Day1kFilteringThreshold.TabIndex = 10; this.lblDaily5Day1kFilteringThreshold.Text = "Daily >= £1k Filtering Threshold (£):"; this.lblDaily5Day1kFilteringThreshold.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // numDaily5Day1kFilteringThreshold
            // 
            this.numDaily5Day1kFilteringThreshold.Anchor = System.Windows.Forms.AnchorStyles.Left; this.numDaily5Day1kFilteringThreshold.DecimalPlaces = 2; this.numDaily5Day1kFilteringThreshold.Location = new System.Drawing.Point(273, 182); this.numDaily5Day1kFilteringThreshold.Maximum = new decimal(new int[] { 10000000, 0, 0, 0 }); this.numDaily5Day1kFilteringThreshold.Name = "numDaily5Day1kFilteringThreshold"; this.numDaily5Day1kFilteringThreshold.Size = new System.Drawing.Size(120, 20); this.numDaily5Day1kFilteringThreshold.TabIndex = 11; this.numDaily5Day1kFilteringThreshold.ThousandsSeparator = true;
            // 
            // lblGeneralFileOpMaxRetries
            // 
            this.lblGeneralFileOpMaxRetries.Anchor = System.Windows.Forms.AnchorStyles.Right; this.lblGeneralFileOpMaxRetries.AutoSize = true; this.lblGeneralFileOpMaxRetries.Location = new System.Drawing.Point(120, 221); this.lblGeneralFileOpMaxRetries.Name = "lblGeneralFileOpMaxRetries"; this.lblGeneralFileOpMaxRetries.Size = new System.Drawing.Size(147, 13); this.lblGeneralFileOpMaxRetries.TabIndex = 12; this.lblGeneralFileOpMaxRetries.Text = "General File Op Max Retries:"; this.lblGeneralFileOpMaxRetries.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // numGeneralFileOpMaxRetries
            // 
            this.numGeneralFileOpMaxRetries.Anchor = System.Windows.Forms.AnchorStyles.Left; this.numGeneralFileOpMaxRetries.Location = new System.Drawing.Point(273, 217); this.numGeneralFileOpMaxRetries.Maximum = new decimal(new int[] { 20, 0, 0, 0 }); this.numGeneralFileOpMaxRetries.Name = "numGeneralFileOpMaxRetries"; this.numGeneralFileOpMaxRetries.Size = new System.Drawing.Size(120, 20); this.numGeneralFileOpMaxRetries.TabIndex = 13;
            // 
            // lblGeneralFileOpDelayMs
            // 
            this.lblGeneralFileOpDelayMs.Anchor = System.Windows.Forms.AnchorStyles.Right; this.lblGeneralFileOpDelayMs.AutoSize = true; this.lblGeneralFileOpDelayMs.Location = new System.Drawing.Point(118, 256); this.lblGeneralFileOpDelayMs.Name = "lblGeneralFileOpDelayMs"; this.lblGeneralFileOpDelayMs.Size = new System.Drawing.Size(149, 13); this.lblGeneralFileOpDelayMs.TabIndex = 14; this.lblGeneralFileOpDelayMs.Text = "General File Op Delay (ms):"; this.lblGeneralFileOpDelayMs.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // numGeneralFileOpDelayMs
            // 
            this.numGeneralFileOpDelayMs.Anchor = System.Windows.Forms.AnchorStyles.Left; this.numGeneralFileOpDelayMs.Location = new System.Drawing.Point(273, 252); this.numGeneralFileOpDelayMs.Maximum = new decimal(new int[] { 10000, 0, 0, 0 }); this.numGeneralFileOpDelayMs.Minimum = new decimal(new int[] { 100, 0, 0, 0 }); this.numGeneralFileOpDelayMs.Name = "numGeneralFileOpDelayMs"; this.numGeneralFileOpDelayMs.Size = new System.Drawing.Size(120, 20); this.numGeneralFileOpDelayMs.TabIndex = 15;
            // 
            // lblRawDataSourceSheet
            // 
            this.lblRawDataSourceSheet.Anchor = System.Windows.Forms.AnchorStyles.Right; this.lblRawDataSourceSheet.AutoSize = true; this.lblRawDataSourceSheet.Location = new System.Drawing.Point(126, 291); this.lblRawDataSourceSheet.Name = "lblRawDataSourceSheet"; this.lblRawDataSourceSheet.Size = new System.Drawing.Size(141, 13); this.lblRawDataSourceSheet.TabIndex = 16; this.lblRawDataSourceSheet.Text = "Excel Raw Data Sheet Name:"; this.lblRawDataSourceSheet.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // txtRawDataSourceSheet
            // 
            this.txtRawDataSourceSheet.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right))); this.txtRawDataSourceSheet.Location = new System.Drawing.Point(273, 287); this.txtRawDataSourceSheet.Name = "txtRawDataSourceSheet"; this.txtRawDataSourceSheet.Size = new System.Drawing.Size(480, 20); this.txtRawDataSourceSheet.TabIndex = 17;
            // 
            // lblTemplateDataCopySheet
            // 
            this.lblTemplateDataCopySheet.Anchor = System.Windows.Forms.AnchorStyles.Right; this.lblTemplateDataCopySheet.AutoSize = true; this.lblTemplateDataCopySheet.Location = new System.Drawing.Point(100, 326); this.lblTemplateDataCopySheet.Name = "lblTemplateDataCopySheet"; this.lblTemplateDataCopySheet.Size = new System.Drawing.Size(167, 13); this.lblTemplateDataCopySheet.TabIndex = 18; this.lblTemplateDataCopySheet.Text = "Excel Template Data Copy Sheet:"; this.lblTemplateDataCopySheet.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // txtTemplateDataCopySheet
            // 
            this.txtTemplateDataCopySheet.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right))); this.txtTemplateDataCopySheet.Location = new System.Drawing.Point(273, 322); this.txtTemplateDataCopySheet.Name = "txtTemplateDataCopySheet"; this.txtTemplateDataCopySheet.Size = new System.Drawing.Size(480, 20); this.txtTemplateDataCopySheet.TabIndex = 19;
            // 
            // lblTemplateAnalysisSheet
            // 
            this.lblTemplateAnalysisSheet.Anchor = System.Windows.Forms.AnchorStyles.Right; this.lblTemplateAnalysisSheet.AutoSize = true; this.lblTemplateAnalysisSheet.Location = new System.Drawing.Point(117, 361); this.lblTemplateAnalysisSheet.Name = "lblTemplateAnalysisSheet"; this.lblTemplateAnalysisSheet.Size = new System.Drawing.Size(150, 13); this.lblTemplateAnalysisSheet.TabIndex = 20; this.lblTemplateAnalysisSheet.Text = "Excel Template Analysis Sheet:"; this.lblTemplateAnalysisSheet.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // txtTemplateAnalysisSheet
            // 
            this.txtTemplateAnalysisSheet.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right))); this.txtTemplateAnalysisSheet.Location = new System.Drawing.Point(273, 357); this.txtTemplateAnalysisSheet.Name = "txtTemplateAnalysisSheet"; this.txtTemplateAnalysisSheet.Size = new System.Drawing.Size(480, 20); this.txtTemplateAnalysisSheet.TabIndex = 21;
            // 
            // lblPowerBiDataSheet
            // 
            this.lblPowerBiDataSheet.Anchor = System.Windows.Forms.AnchorStyles.Right; this.lblPowerBiDataSheet.AutoSize = true; this.lblPowerBiDataSheet.Location = new System.Drawing.Point(129, 396); this.lblPowerBiDataSheet.Name = "lblPowerBiDataSheet"; this.lblPowerBiDataSheet.Size = new System.Drawing.Size(138, 13); this.lblPowerBiDataSheet.TabIndex = 22; this.lblPowerBiDataSheet.Text = "Excel Power BI Data Sheet:"; this.lblPowerBiDataSheet.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // txtPowerBiDataSheet
            // 
            this.txtPowerBiDataSheet.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right))); this.txtPowerBiDataSheet.Location = new System.Drawing.Point(273, 392); this.txtPowerBiDataSheet.Name = "txtPowerBiDataSheet"; this.txtPowerBiDataSheet.Size = new System.Drawing.Size(480, 20); this.txtPowerBiDataSheet.TabIndex = 23;
            // 
            // lblMonthlyOrderPivotSheet
            // 
            this.lblMonthlyOrderPivotSheet.Anchor = System.Windows.Forms.AnchorStyles.Right; this.lblMonthlyOrderPivotSheet.AutoSize = true; this.lblMonthlyOrderPivotSheet.Location = new System.Drawing.Point(106, 431); this.lblMonthlyOrderPivotSheet.Name = "lblMonthlyOrderPivotSheet"; this.lblMonthlyOrderPivotSheet.Size = new System.Drawing.Size(161, 13); this.lblMonthlyOrderPivotSheet.TabIndex = 24; this.lblMonthlyOrderPivotSheet.Text = "Excel Monthly Order Pivot Sheet:"; this.lblMonthlyOrderPivotSheet.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // txtMonthlyOrderPivotSheet
            // 
            this.txtMonthlyOrderPivotSheet.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right))); this.txtMonthlyOrderPivotSheet.Location = new System.Drawing.Point(273, 427); this.txtMonthlyOrderPivotSheet.Name = "txtMonthlyOrderPivotSheet"; this.txtMonthlyOrderPivotSheet.Size = new System.Drawing.Size(480, 20); this.txtMonthlyOrderPivotSheet.TabIndex = 25;
            // 
            // lblMonthlyEstimatePivotSheet
            // 
            this.lblMonthlyEstimatePivotSheet.Anchor = System.Windows.Forms.AnchorStyles.Right; this.lblMonthlyEstimatePivotSheet.AutoSize = true; this.lblMonthlyEstimatePivotSheet.Location = new System.Drawing.Point(86, 466); this.lblMonthlyEstimatePivotSheet.Name = "lblMonthlyEstimatePivotSheet"; this.lblMonthlyEstimatePivotSheet.Size = new System.Drawing.Size(181, 13); this.lblMonthlyEstimatePivotSheet.TabIndex = 26; this.lblMonthlyEstimatePivotSheet.Text = "Excel Monthly Estimate Pivot Sheet:"; this.lblMonthlyEstimatePivotSheet.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // txtMonthlyEstimatePivotSheet
            // 
            this.txtMonthlyEstimatePivotSheet.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right))); this.txtMonthlyEstimatePivotSheet.Location = new System.Drawing.Point(273, 462); this.txtMonthlyEstimatePivotSheet.Name = "txtMonthlyEstimatePivotSheet"; this.txtMonthlyEstimatePivotSheet.Size = new System.Drawing.Size(480, 20); this.txtMonthlyEstimatePivotSheet.TabIndex = 27;
            // 
            // lblMonthlyOrderPivotName
            // 
            this.lblMonthlyOrderPivotName.Anchor = System.Windows.Forms.AnchorStyles.Right; this.lblMonthlyOrderPivotName.AutoSize = true; this.lblMonthlyOrderPivotName.Location = new System.Drawing.Point(106, 501); this.lblMonthlyOrderPivotName.Name = "lblMonthlyOrderPivotName"; this.lblMonthlyOrderPivotName.Size = new System.Drawing.Size(161, 13); this.lblMonthlyOrderPivotName.TabIndex = 28; this.lblMonthlyOrderPivotName.Text = "Excel Monthly Order Pivot Name:"; this.lblMonthlyOrderPivotName.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // txtMonthlyOrderPivotName
            // 
            this.txtMonthlyOrderPivotName.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right))); this.txtMonthlyOrderPivotName.Location = new System.Drawing.Point(273, 497); this.txtMonthlyOrderPivotName.Name = "txtMonthlyOrderPivotName"; this.txtMonthlyOrderPivotName.Size = new System.Drawing.Size(480, 20); this.txtMonthlyOrderPivotName.TabIndex = 29;
            // 
            // lblMonthlyEstimatePivotName
            // 
            this.lblMonthlyEstimatePivotName.Anchor = System.Windows.Forms.AnchorStyles.Right; this.lblMonthlyEstimatePivotName.AutoSize = true; this.lblMonthlyEstimatePivotName.Location = new System.Drawing.Point(86, 536); this.lblMonthlyEstimatePivotName.Name = "lblMonthlyEstimatePivotName"; this.lblMonthlyEstimatePivotName.Size = new System.Drawing.Size(181, 13); this.lblMonthlyEstimatePivotName.TabIndex = 30; this.lblMonthlyEstimatePivotName.Text = "Excel Monthly Estimate Pivot Name:"; this.lblMonthlyEstimatePivotName.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // txtMonthlyEstimatePivotName
            // 
            this.txtMonthlyEstimatePivotName.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right))); this.txtMonthlyEstimatePivotName.Location = new System.Drawing.Point(273, 532); this.txtMonthlyEstimatePivotName.Name = "txtMonthlyEstimatePivotName"; this.txtMonthlyEstimatePivotName.Size = new System.Drawing.Size(480, 20); this.txtMonthlyEstimatePivotName.TabIndex = 31;
            // 
            // lblFolderNamingDaily
            // 
            this.lblFolderNamingDaily.Anchor = System.Windows.Forms.AnchorStyles.Right; this.lblFolderNamingDaily.AutoSize = true; this.lblFolderNamingDaily.Location = new System.Drawing.Point(112, 571); this.lblFolderNamingDaily.Name = "lblFolderNamingDaily"; this.lblFolderNamingDaily.Size = new System.Drawing.Size(155, 13); this.lblFolderNamingDaily.TabIndex = 32; this.lblFolderNamingDaily.Text = "Folder Name for \"Daily\" Reports:"; this.lblFolderNamingDaily.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // txtFolderNamingDaily
            // 
            this.txtFolderNamingDaily.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right))); this.txtFolderNamingDaily.Location = new System.Drawing.Point(273, 567); this.txtFolderNamingDaily.Name = "txtFolderNamingDaily"; this.txtFolderNamingDaily.Size = new System.Drawing.Size(480, 20); this.txtFolderNamingDaily.TabIndex = 33;
            // 
            // lblFolderNamingDaily5Day1k
            // 
            this.lblFolderNamingDaily5Day1k.Anchor = System.Windows.Forms.AnchorStyles.Right; this.lblFolderNamingDaily5Day1k.AutoSize = true; this.lblFolderNamingDaily5Day1k.Location = new System.Drawing.Point(63, 606); this.lblFolderNamingDaily5Day1k.Name = "lblFolderNamingDaily5Day1k"; this.lblFolderNamingDaily5Day1k.Size = new System.Drawing.Size(204, 13); this.lblFolderNamingDaily5Day1k.TabIndex = 34; this.lblFolderNamingDaily5Day1k.Text = "Folder Name for \"Daily (5day >= £1k)\" Reports:"; this.lblFolderNamingDaily5Day1k.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // txtFolderNamingDaily5Day1k
            // 
            this.txtFolderNamingDaily5Day1k.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right))); this.txtFolderNamingDaily5Day1k.Location = new System.Drawing.Point(273, 602); this.txtFolderNamingDaily5Day1k.Name = "txtFolderNamingDaily5Day1k"; this.txtFolderNamingDaily5Day1k.Size = new System.Drawing.Size(480, 20); this.txtFolderNamingDaily5Day1k.TabIndex = 35;
            // 
            // lblFolderNamingWeekly
            // 
            this.lblFolderNamingWeekly.Anchor = System.Windows.Forms.AnchorStyles.Right; this.lblFolderNamingWeekly.AutoSize = true; this.lblFolderNamingWeekly.Location = new System.Drawing.Point(99, 641); this.lblFolderNamingWeekly.Name = "lblFolderNamingWeekly"; this.lblFolderNamingWeekly.Size = new System.Drawing.Size(168, 13); this.lblFolderNamingWeekly.TabIndex = 36; this.lblFolderNamingWeekly.Text = "Folder Name for \"Weekly\" Reports:"; this.lblFolderNamingWeekly.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // txtFolderNamingWeekly
            // 
            this.txtFolderNamingWeekly.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right))); this.txtFolderNamingWeekly.Location = new System.Drawing.Point(273, 637); this.txtFolderNamingWeekly.Name = "txtFolderNamingWeekly"; this.txtFolderNamingWeekly.Size = new System.Drawing.Size(480, 20); this.txtFolderNamingWeekly.TabIndex = 37;
            // 
            // lblFolderNamingMonthly
            // 
            this.lblFolderNamingMonthly.Anchor = System.Windows.Forms.AnchorStyles.Right; this.lblFolderNamingMonthly.AutoSize = true; this.lblFolderNamingMonthly.Location = new System.Drawing.Point(93, 676); this.lblFolderNamingMonthly.Name = "lblFolderNamingMonthly"; this.lblFolderNamingMonthly.Size = new System.Drawing.Size(174, 13); this.lblFolderNamingMonthly.TabIndex = 38; this.lblFolderNamingMonthly.Text = "Folder Name for \"Monthly\" Reports:"; this.lblFolderNamingMonthly.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // txtFolderNamingMonthly
            // 
            this.txtFolderNamingMonthly.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right))); this.txtFolderNamingMonthly.Location = new System.Drawing.Point(273, 672); this.txtFolderNamingMonthly.Name = "txtFolderNamingMonthly"; this.txtFolderNamingMonthly.Size = new System.Drawing.Size(480, 20); this.txtFolderNamingMonthly.TabIndex = 39;
            // 
            // lblFolderNamingQuarterly
            // 
            this.lblFolderNamingQuarterly.Anchor = System.Windows.Forms.AnchorStyles.Right; this.lblFolderNamingQuarterly.AutoSize = true; this.lblFolderNamingQuarterly.Location = new System.Drawing.Point(86, 711); this.lblFolderNamingQuarterly.Name = "lblFolderNamingQuarterly"; this.lblFolderNamingQuarterly.Size = new System.Drawing.Size(181, 13); this.lblFolderNamingQuarterly.TabIndex = 40; this.lblFolderNamingQuarterly.Text = "Folder Name for \"Quarterly\" Reports:"; this.lblFolderNamingQuarterly.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // txtFolderNamingQuarterly
            // 
            this.txtFolderNamingQuarterly.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right))); this.txtFolderNamingQuarterly.Location = new System.Drawing.Point(273, 707); this.txtFolderNamingQuarterly.Name = "txtFolderNamingQuarterly"; this.txtFolderNamingQuarterly.Size = new System.Drawing.Size(480, 20); this.txtFolderNamingQuarterly.TabIndex = 41;
            // 
            // lblFolderNamingAnnual
            // 
            this.lblFolderNamingAnnual.Anchor = System.Windows.Forms.AnchorStyles.Right; this.lblFolderNamingAnnual.AutoSize = true; this.lblFolderNamingAnnual.Location = new System.Drawing.Point(101, 746); this.lblFolderNamingAnnual.Name = "lblFolderNamingAnnual"; this.lblFolderNamingAnnual.Size = new System.Drawing.Size(166, 13); this.lblFolderNamingAnnual.TabIndex = 42; this.lblFolderNamingAnnual.Text = "Folder Name for \"Annual\" Reports:"; this.lblFolderNamingAnnual.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // txtFolderNamingAnnual
            // 
            this.txtFolderNamingAnnual.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right))); this.txtFolderNamingAnnual.Location = new System.Drawing.Point(273, 742); this.txtFolderNamingAnnual.Name = "txtFolderNamingAnnual"; this.txtFolderNamingAnnual.Size = new System.Drawing.Size(480, 20); this.txtFolderNamingAnnual.TabIndex = 43;
            // 
            // lblFolderNamingCustom
            // 
            this.lblFolderNamingCustom.Anchor = System.Windows.Forms.AnchorStyles.Right; this.lblFolderNamingCustom.AutoSize = true; this.lblFolderNamingCustom.Location = new System.Drawing.Point(99, 781); this.lblFolderNamingCustom.Name = "lblFolderNamingCustom"; this.lblFolderNamingCustom.Size = new System.Drawing.Size(168, 13); this.lblFolderNamingCustom.TabIndex = 44; this.lblFolderNamingCustom.Text = "Folder Name for \"Custom\" Reports:"; this.lblFolderNamingCustom.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // txtFolderNamingCustom
            // 
            this.txtFolderNamingCustom.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right))); this.txtFolderNamingCustom.Location = new System.Drawing.Point(273, 777); this.txtFolderNamingCustom.Name = "txtFolderNamingCustom"; this.txtFolderNamingCustom.Size = new System.Drawing.Size(480, 20); this.txtFolderNamingCustom.TabIndex = 45;
            // 
            // lblFolderNamingOther
            // 
            this.lblFolderNamingOther.Anchor = System.Windows.Forms.AnchorStyles.Right; this.lblFolderNamingOther.AutoSize = true; this.lblFolderNamingOther.Location = new System.Drawing.Point(106, 816); this.lblFolderNamingOther.Name = "lblFolderNamingOther"; this.lblFolderNamingOther.Size = new System.Drawing.Size(161, 13); this.lblFolderNamingOther.TabIndex = 46; this.lblFolderNamingOther.Text = "Folder Name for \"Other\" Reports:"; this.lblFolderNamingOther.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // txtFolderNamingOther
            // 
            this.txtFolderNamingOther.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right))); this.txtFolderNamingOther.Location = new System.Drawing.Point(273, 812); this.txtFolderNamingOther.Name = "txtFolderNamingOther"; this.txtFolderNamingOther.Size = new System.Drawing.Size(480, 20); this.txtFolderNamingOther.TabIndex = 47;
            // 
            // tabPageIPC
            // 
            this.tabPageIPC.AutoScroll = true;
            this.tabPageIPC.Controls.Add(this.tlpIPC);
            this.tabPageIPC.Location = new System.Drawing.Point(4, 22);
            this.tabPageIPC.Name = "tabPageIPC";
            this.tabPageIPC.Padding = new System.Windows.Forms.Padding(10);
            this.tabPageIPC.Size = new System.Drawing.Size(776, 385);
            this.tabPageIPC.TabIndex = 6;
            this.tabPageIPC.Text = "Inter-Process Communication";
            this.tabPageIPC.UseVisualStyleBackColor = true;
            // 
            // tlpIPC
            // 
            this.tlpIPC.AutoSize = true;
            this.tlpIPC.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.tlpIPC.ColumnCount = 2;
            this.tlpIPC.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 220F));
            this.tlpIPC.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tlpIPC.Controls.Add(this.lblNamedPipeName, 0, 0);
            this.tlpIPC.Controls.Add(this.txtNamedPipeName, 1, 0);
            this.tlpIPC.Controls.Add(this.lblPipeConnectTimeoutMs, 0, 1);
            this.tlpIPC.Controls.Add(this.numPipeConnectTimeoutMs, 1, 1);
            this.tlpIPC.Controls.Add(this.lblMaxPipeResponseSizeBytes, 0, 2);
            this.tlpIPC.Controls.Add(this.numMaxPipeResponseSizeBytes, 1, 2);
            this.tlpIPC.Dock = System.Windows.Forms.DockStyle.Top;
            this.tlpIPC.Location = new System.Drawing.Point(10, 10);
            this.tlpIPC.Name = "tlpIPC";
            this.tlpIPC.RowCount = 3;
            for (int i = 0; i < this.tlpIPC.RowCount; i++) { this.tlpIPC.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 35F)); }
            this.tlpIPC.Size = new System.Drawing.Size(756, 105); // 3 rows * 35F
            this.tlpIPC.TabIndex = 0;
            // 
            // lblNamedPipeName
            // 
            this.lblNamedPipeName.Anchor = System.Windows.Forms.AnchorStyles.Right; this.lblNamedPipeName.AutoSize = true; this.lblNamedPipeName.Location = new System.Drawing.Point(116, 11); this.lblNamedPipeName.Name = "lblNamedPipeName"; this.lblNamedPipeName.Size = new System.Drawing.Size(98, 13); this.lblNamedPipeName.TabIndex = 0; this.lblNamedPipeName.Text = "Named Pipe Name:"; this.lblNamedPipeName.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // txtNamedPipeName
            // 
            this.txtNamedPipeName.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right))); this.txtNamedPipeName.Location = new System.Drawing.Point(223, 7); this.txtNamedPipeName.Name = "txtNamedPipeName"; this.txtNamedPipeName.Size = new System.Drawing.Size(530, 20); this.txtNamedPipeName.TabIndex = 1;
            // 
            // lblPipeConnectTimeoutMs
            // 
            this.lblPipeConnectTimeoutMs.Anchor = System.Windows.Forms.AnchorStyles.Right; this.lblPipeConnectTimeoutMs.AutoSize = true; this.lblPipeConnectTimeoutMs.Location = new System.Drawing.Point(77, 46); this.lblPipeConnectTimeoutMs.Name = "lblPipeConnectTimeoutMs"; this.lblPipeConnectTimeoutMs.Size = new System.Drawing.Size(137, 13); this.lblPipeConnectTimeoutMs.TabIndex = 2; this.lblPipeConnectTimeoutMs.Text = "Pipe Connect Timeout (ms):"; this.lblPipeConnectTimeoutMs.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // numPipeConnectTimeoutMs
            // 
            this.numPipeConnectTimeoutMs.Anchor = System.Windows.Forms.AnchorStyles.Left; this.numPipeConnectTimeoutMs.Location = new System.Drawing.Point(223, 42); this.numPipeConnectTimeoutMs.Maximum = new decimal(new int[] { 60000, 0, 0, 0 }); this.numPipeConnectTimeoutMs.Minimum = new decimal(new int[] { 500, 0, 0, 0 }); this.numPipeConnectTimeoutMs.Name = "numPipeConnectTimeoutMs"; this.numPipeConnectTimeoutMs.Size = new System.Drawing.Size(120, 20); this.numPipeConnectTimeoutMs.TabIndex = 3; this.numPipeConnectTimeoutMs.Value = new decimal(new int[] { 5000, 0, 0, 0 });
            // 
            // lblMaxPipeResponseSizeBytes
            // 
            this.lblMaxPipeResponseSizeBytes.Anchor = System.Windows.Forms.AnchorStyles.Right; this.lblMaxPipeResponseSizeBytes.AutoSize = true; this.lblMaxPipeResponseSizeBytes.Location = new System.Drawing.Point(42, 81); this.lblMaxPipeResponseSizeBytes.Name = "lblMaxPipeResponseSizeBytes"; this.lblMaxPipeResponseSizeBytes.Size = new System.Drawing.Size(172, 13); this.lblMaxPipeResponseSizeBytes.TabIndex = 4; this.lblMaxPipeResponseSizeBytes.Text = "Max Pipe Response Size (Bytes):"; this.lblMaxPipeResponseSizeBytes.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // numMaxPipeResponseSizeBytes
            // 
            this.numMaxPipeResponseSizeBytes.Anchor = System.Windows.Forms.AnchorStyles.Left; this.numMaxPipeResponseSizeBytes.Location = new System.Drawing.Point(223, 77); this.numMaxPipeResponseSizeBytes.Maximum = new decimal(new int[] { 52428800, 0, 0, 0 }); this.numMaxPipeResponseSizeBytes.Minimum = new decimal(new int[] { 1024, 0, 0, 0 }); this.numMaxPipeResponseSizeBytes.Name = "numMaxPipeResponseSizeBytes"; this.numMaxPipeResponseSizeBytes.Size = new System.Drawing.Size(120, 20); this.numMaxPipeResponseSizeBytes.TabIndex = 5; this.numMaxPipeResponseSizeBytes.ThousandsSeparator = true; this.numMaxPipeResponseSizeBytes.Value = new decimal(new int[] { 10485760, 0, 0, 0 });
            // 
            // tabPageAutoRun
            //
            this.tabPageAutoRun.AutoScroll = true;
            this.tabPageAutoRun.Controls.Add(this.tlpAutoRun);
            this.tabPageAutoRun.Location = new System.Drawing.Point(4, 22);
            this.tabPageAutoRun.Name = "tabPageAutoRun";
            this.tabPageAutoRun.Padding = new System.Windows.Forms.Padding(10);
            this.tabPageAutoRun.Size = new System.Drawing.Size(776, 385);
            this.tabPageAutoRun.TabIndex = 7;
            this.tabPageAutoRun.Text = "AutoRun Process";
            this.tabPageAutoRun.UseVisualStyleBackColor = true;
            // 
            // tlpAutoRun
            // 
            this.tlpAutoRun.AutoSize = true;
            this.tlpAutoRun.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.tlpAutoRun.ColumnCount = 2;
            this.tlpAutoRun.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 180F));
            this.tlpAutoRun.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tlpAutoRun.Controls.Add(this.lblAutoRunCheckHour, 0, 0);
            this.tlpAutoRun.Controls.Add(this.numAutoRunCheckHour, 1, 0);
            this.tlpAutoRun.Dock = System.Windows.Forms.DockStyle.Top;
            this.tlpAutoRun.Location = new System.Drawing.Point(10, 10);
            this.tlpAutoRun.Name = "tlpAutoRun";
            this.tlpAutoRun.RowCount = 1;
            this.tlpAutoRun.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 35F));
            this.tlpAutoRun.Size = new System.Drawing.Size(756, 35); // 1 row * 35F
            this.tlpAutoRun.TabIndex = 0;
            // 
            // lblAutoRunCheckHour
            // 
            this.lblAutoRunCheckHour.Anchor = System.Windows.Forms.AnchorStyles.Right; this.lblAutoRunCheckHour.AutoSize = true; this.lblAutoRunCheckHour.Location = new System.Drawing.Point(49, 11); this.lblAutoRunCheckHour.Name = "lblAutoRunCheckHour"; this.lblAutoRunCheckHour.Size = new System.Drawing.Size(128, 13); this.lblAutoRunCheckHour.TabIndex = 0; this.lblAutoRunCheckHour.Text = "AutoRun Check Hour (0-23):"; this.lblAutoRunCheckHour.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // numAutoRunCheckHour
            // 
            this.numAutoRunCheckHour.Anchor = System.Windows.Forms.AnchorStyles.Left; this.numAutoRunCheckHour.Location = new System.Drawing.Point(183, 7); this.numAutoRunCheckHour.Maximum = new decimal(new int[] { 23, 0, 0, 0 }); this.numAutoRunCheckHour.Name = "numAutoRunCheckHour"; this.numAutoRunCheckHour.Size = new System.Drawing.Size(120, 20); this.numAutoRunCheckHour.TabIndex = 1; this.numAutoRunCheckHour.Value = new decimal(new int[] { 8, 0, 0, 0 });
            // 
            // panelButtons
            // 
            this.panelButtons.Controls.Add(this.btnSaveChanges);
            this.panelButtons.Controls.Add(this.btnCancel);
            this.panelButtons.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panelButtons.Location = new System.Drawing.Point(0, 411); // Will adjust if mainTabControl height changes
            this.panelButtons.Name = "panelButtons";
            this.panelButtons.Padding = new System.Windows.Forms.Padding(10);
            this.panelButtons.Size = new System.Drawing.Size(784, 50);
            this.panelButtons.TabIndex = 1;
            // 
            // btnSaveChanges
            // 
            this.btnSaveChanges.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnSaveChanges.Location = new System.Drawing.Point(596, 10);
            this.btnSaveChanges.Name = "btnSaveChanges";
            this.btnSaveChanges.Size = new System.Drawing.Size(85, 28);
            this.btnSaveChanges.TabIndex = 0;
            this.btnSaveChanges.Text = "&Save";
            this.btnSaveChanges.UseVisualStyleBackColor = true;
            this.btnSaveChanges.Click += new System.EventHandler(this.btnSaveChanges_Click);
            // 
            // btnCancel
            // 
            this.btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Location = new System.Drawing.Point(687, 10);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(85, 28);
            this.btnCancel.TabIndex = 1;
            this.btnCancel.Text = "&Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // SettingsForm
            // 
            this.AcceptButton = this.btnSaveChanges;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(784, 461); // Adjusted if needed based on content
            this.Controls.Add(this.mainTabControl);
            this.Controls.Add(this.panelButtons);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "SettingsForm";
            this.Text = "Application Settings";
            this.mainTabControl.ResumeLayout(false);
            this.tabPageAppInfo.ResumeLayout(false);
            this.tabPageAppInfo.PerformLayout();
            this.tlpAppInfo.ResumeLayout(false);
            this.tlpAppInfo.PerformLayout();
            this.tabPagePaths.ResumeLayout(false);
            this.tabPagePaths.PerformLayout();
            this.tlpPaths.ResumeLayout(false);
            this.tlpPaths.PerformLayout();
            this.tabPageSmtp.ResumeLayout(false);
            this.tabPageSmtp.PerformLayout();
            this.tlpSmtp.ResumeLayout(false);
            this.tlpSmtp.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numSmtpPort)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numSmtpMaxSendRetries)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numSmtpSendRetryDelayMs)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numSmtpTimeoutMs)).EndInit();
            this.tabPageEmailDefaults.ResumeLayout(false);
            this.tabPageEmailDefaults.PerformLayout();
            this.tlpEmailDefaults.ResumeLayout(false);
            this.tlpEmailDefaults.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numMaxAttachmentSizeBytes)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numAttachmentReadMaxRetries)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numAttachmentReadDelayMs)).EndInit();
            this.tabPageLogging.ResumeLayout(false);
            this.tabPageLogging.PerformLayout();
            this.tlpLogging.ResumeLayout(false);
            this.tlpLogging.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numLogArchiveOlderThanDays)).EndInit();
            this.tabPageOperational.ResumeLayout(false);
            this.tabPageOperational.PerformLayout();
            this.tlpOperational.ResumeLayout(false);
            this.tlpOperational.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numArchiveRawReportsOlderThanDays)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numProcessTimeoutMinutes)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numFinancialYearStartMonth)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numFinancialYearStartDay)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numDaily5Day1kFilteringThreshold)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numGeneralFileOpMaxRetries)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numGeneralFileOpDelayMs)).EndInit();
            this.tabPageIPC.ResumeLayout(false);
            this.tabPageIPC.PerformLayout();
            this.tlpIPC.ResumeLayout(false);
            this.tlpIPC.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numPipeConnectTimeoutMs)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numMaxPipeResponseSizeBytes)).EndInit();
            this.tabPageAutoRun.ResumeLayout(false);
            this.tabPageAutoRun.PerformLayout();
            this.tlpAutoRun.ResumeLayout(false);
            this.tlpAutoRun.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numAutoRunCheckHour)).EndInit();
            this.panelButtons.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        // Field declarations for all controls
        private System.Windows.Forms.TabControl mainTabControl;
        private System.Windows.Forms.Panel panelButtons;
        private System.Windows.Forms.Button btnSaveChanges;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.ToolTip toolTip1;

        // AppInfo Tab
        private System.Windows.Forms.TabPage tabPageAppInfo;
        private System.Windows.Forms.TableLayoutPanel tlpAppInfo;
        private System.Windows.Forms.Label lblAppName;
        private System.Windows.Forms.TextBox txtAppName;
        private System.Windows.Forms.Label lblAppVersion;
        private System.Windows.Forms.TextBox txtAppVersion;

        // Paths Tab
        private System.Windows.Forms.TabPage tabPagePaths;
        private System.Windows.Forms.TableLayoutPanel tlpPaths;
        private System.Windows.Forms.Label lblCrystalReportRptFile;
        private System.Windows.Forms.TextBox txtCrystalReportRptFile;
        private System.Windows.Forms.Button btnBrowseCrystalReport;
        private System.Windows.Forms.Label lblFinalReportOutputBase;
        private System.Windows.Forms.TextBox txtFinalReportOutputBase;
        private System.Windows.Forms.Button btnBrowseFinalReportOutputBase;
        private System.Windows.Forms.Label lblTemplateBase;
        private System.Windows.Forms.TextBox txtTemplateBase;
        private System.Windows.Forms.Button btnBrowseTemplateBase;
        private System.Windows.Forms.Label lblLogDirectoryBase;
        private System.Windows.Forms.TextBox txtLogDirectoryBase;
        private System.Windows.Forms.Button btnBrowseLogDirectoryBase;
        private System.Windows.Forms.Label lblRawReportOutputBase;
        private System.Windows.Forms.TextBox txtRawReportOutputBase;
        private System.Windows.Forms.Button btnBrowseRawReportOutputBase;
        private System.Windows.Forms.Label lblWrapperExecutable;
        private System.Windows.Forms.TextBox txtWrapperExecutable;
        private System.Windows.Forms.Button btnBrowseWrapperExecutable;
        private System.Windows.Forms.Label lblReportDefinitionsFileName;
        private System.Windows.Forms.TextBox txtReportDefinitionsFileName;
        private System.Windows.Forms.Label lblFallbackLogDirectory;
        private System.Windows.Forms.TextBox txtFallbackLogDirectory;
        private System.Windows.Forms.Button btnBrowseFallbackLogDir;

        // SMTP Tab
        private System.Windows.Forms.TabPage tabPageSmtp;
        private System.Windows.Forms.TableLayoutPanel tlpSmtp;
        private System.Windows.Forms.Label lblSmtpServer;
        private System.Windows.Forms.TextBox txtSmtpServer;
        private System.Windows.Forms.Label lblSmtpPort;
        private System.Windows.Forms.NumericUpDown numSmtpPort;
        private System.Windows.Forms.Label lblSmtpUsername;
        private System.Windows.Forms.TextBox txtSmtpUsername;
        private System.Windows.Forms.Label lblSmtpPassword;
        private System.Windows.Forms.TextBox txtSmtpPassword;
        private System.Windows.Forms.CheckBox chkSmtpEnableSsl;
        private System.Windows.Forms.Label lblSmtpMaxSendRetries;
        private System.Windows.Forms.NumericUpDown numSmtpMaxSendRetries;
        private System.Windows.Forms.Label lblSmtpSendRetryDelayMs;
        private System.Windows.Forms.NumericUpDown numSmtpSendRetryDelayMs;
        private System.Windows.Forms.Label lblSmtpTimeoutMs;
        private System.Windows.Forms.NumericUpDown numSmtpTimeoutMs;

        // Email Defaults Tab
        private System.Windows.Forms.TabPage tabPageEmailDefaults;
        private System.Windows.Forms.TableLayoutPanel tlpEmailDefaults;
        private System.Windows.Forms.Label lblSenderAddress;
        private System.Windows.Forms.TextBox txtSenderAddress;
        private System.Windows.Forms.Label lblSenderDisplayName;
        private System.Windows.Forms.TextBox txtSenderDisplayName;
        private System.Windows.Forms.Label lblMaxAttachmentSizeBytes;
        private System.Windows.Forms.NumericUpDown numMaxAttachmentSizeBytes;
        private System.Windows.Forms.Label lblDefaultEmailSignature;
        private System.Windows.Forms.TextBox txtDefaultEmailSignature;
        private System.Windows.Forms.Label lblAttachmentReadMaxRetries;
        private System.Windows.Forms.NumericUpDown numAttachmentReadMaxRetries;
        private System.Windows.Forms.Label lblAttachmentReadDelayMs;
        private System.Windows.Forms.NumericUpDown numAttachmentReadDelayMs;

        // Logging Tab
        private System.Windows.Forms.TabPage tabPageLogging;
        private System.Windows.Forms.TableLayoutPanel tlpLogging;
        private System.Windows.Forms.Label lblDefaultLogLevel;
        private System.Windows.Forms.ComboBox cmbDefaultLogLevel;
        private System.Windows.Forms.Label lblDebugBuildLogLevel;
        private System.Windows.Forms.ComboBox cmbDebugBuildLogLevel;
        private System.Windows.Forms.Label lblLogArchiveOlderThanDays;
        private System.Windows.Forms.NumericUpDown numLogArchiveOlderThanDays;
        private System.Windows.Forms.Label lblLogFileNameFormat;
        private System.Windows.Forms.TextBox txtLogFileNameFormat;

        // Operational Parameters Tab
        private System.Windows.Forms.TabPage tabPageOperational;
        private System.Windows.Forms.TableLayoutPanel tlpOperational;
        private System.Windows.Forms.Label lblArchiveRawReportsOlderThanDays;
        private System.Windows.Forms.NumericUpDown numArchiveRawReportsOlderThanDays;
        private System.Windows.Forms.Label lblReportArchiveFolderName;
        private System.Windows.Forms.TextBox txtReportArchiveFolderName;
        private System.Windows.Forms.Label lblProcessTimeoutMinutes;
        private System.Windows.Forms.NumericUpDown numProcessTimeoutMinutes;
        private System.Windows.Forms.Label lblFinancialYearStartMonth;
        private System.Windows.Forms.NumericUpDown numFinancialYearStartMonth;
        private System.Windows.Forms.Label lblFinancialYearStartDay;
        private System.Windows.Forms.NumericUpDown numFinancialYearStartDay;
        private System.Windows.Forms.Label lblDaily5Day1kFilteringThreshold;
        private System.Windows.Forms.NumericUpDown numDaily5Day1kFilteringThreshold;
        private System.Windows.Forms.Label lblGeneralFileOpMaxRetries;
        private System.Windows.Forms.NumericUpDown numGeneralFileOpMaxRetries;
        private System.Windows.Forms.Label lblGeneralFileOpDelayMs;
        private System.Windows.Forms.NumericUpDown numGeneralFileOpDelayMs;
        private System.Windows.Forms.Label lblRawDataSourceSheet;
        private System.Windows.Forms.TextBox txtRawDataSourceSheet;
        private System.Windows.Forms.Label lblTemplateDataCopySheet;
        private System.Windows.Forms.TextBox txtTemplateDataCopySheet;
        private System.Windows.Forms.Label lblTemplateAnalysisSheet;
        private System.Windows.Forms.TextBox txtTemplateAnalysisSheet;
        private System.Windows.Forms.Label lblPowerBiDataSheet;
        private System.Windows.Forms.TextBox txtPowerBiDataSheet;
        private System.Windows.Forms.Label lblMonthlyOrderPivotSheet;
        private System.Windows.Forms.TextBox txtMonthlyOrderPivotSheet;
        private System.Windows.Forms.Label lblMonthlyEstimatePivotSheet;
        private System.Windows.Forms.TextBox txtMonthlyEstimatePivotSheet;
        private System.Windows.Forms.Label lblMonthlyOrderPivotName;
        private System.Windows.Forms.TextBox txtMonthlyOrderPivotName;
        private System.Windows.Forms.Label lblMonthlyEstimatePivotName;
        private System.Windows.Forms.TextBox txtMonthlyEstimatePivotName;
        private System.Windows.Forms.Label lblFolderNamingDaily;
        private System.Windows.Forms.TextBox txtFolderNamingDaily;
        private System.Windows.Forms.Label lblFolderNamingDaily5Day1k;
        private System.Windows.Forms.TextBox txtFolderNamingDaily5Day1k;
        private System.Windows.Forms.Label lblFolderNamingWeekly;
        private System.Windows.Forms.TextBox txtFolderNamingWeekly;
        private System.Windows.Forms.Label lblFolderNamingMonthly;
        private System.Windows.Forms.TextBox txtFolderNamingMonthly;
        private System.Windows.Forms.Label lblFolderNamingQuarterly;
        private System.Windows.Forms.TextBox txtFolderNamingQuarterly;
        private System.Windows.Forms.Label lblFolderNamingAnnual;
        private System.Windows.Forms.TextBox txtFolderNamingAnnual;
        private System.Windows.Forms.Label lblFolderNamingCustom;
        private System.Windows.Forms.TextBox txtFolderNamingCustom;
        private System.Windows.Forms.Label lblFolderNamingOther;
        private System.Windows.Forms.TextBox txtFolderNamingOther;

        // Inter-Process Communication (IPC) Tab
        private System.Windows.Forms.TabPage tabPageIPC;
        private System.Windows.Forms.TableLayoutPanel tlpIPC;
        private System.Windows.Forms.Label lblNamedPipeName;
        private System.Windows.Forms.TextBox txtNamedPipeName;
        private System.Windows.Forms.Label lblPipeConnectTimeoutMs;
        private System.Windows.Forms.NumericUpDown numPipeConnectTimeoutMs;
        private System.Windows.Forms.Label lblMaxPipeResponseSizeBytes;
        private System.Windows.Forms.NumericUpDown numMaxPipeResponseSizeBytes;

        // AutoRun Process Tab
        private System.Windows.Forms.TabPage tabPageAutoRun;
        private System.Windows.Forms.TableLayoutPanel tlpAutoRun;
        private System.Windows.Forms.Label lblAutoRunCheckHour;
        private System.Windows.Forms.NumericUpDown numAutoRunCheckHour;
    }
}
