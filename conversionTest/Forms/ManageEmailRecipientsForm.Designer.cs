namespace QuoteConversionReportAutomation
{
    partial class ManageEmailRecipientsForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.buttonsFlowLayoutPanel = new System.Windows.Forms.FlowLayoutPanel();
            this.btnSave = new System.Windows.Forms.Button();
            this.btnRestoreDefaults = new System.Windows.Forms.Button();
            this.btnClose = new System.Windows.Forms.Button();
            this.toolTipProvider = new System.Windows.Forms.ToolTip(this.components);
            this.mainTabControl = new System.Windows.Forms.TabControl();
            // TabPage for Automated Reports
            this.automatedReportsTabPage = new System.Windows.Forms.TabPage();
            this.automatedReportsTableLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
            this.lblProdAutoRunDailyTo = new System.Windows.Forms.Label();
            this.txtProdAutoRunDailyTo = new System.Windows.Forms.TextBox();
            this.lblProdAutoRunDailyCC = new System.Windows.Forms.Label();
            this.txtProdAutoRunDailyCC = new System.Windows.Forms.TextBox();
            this.lblProdAutoRunDaily5Day1kTo = new System.Windows.Forms.Label();
            this.txtProdAutoRunDaily5Day1kTo = new System.Windows.Forms.TextBox();
            this.lblProdAutoRunDaily5Day1kCC = new System.Windows.Forms.Label();
            this.txtProdAutoRunDaily5Day1kCC = new System.Windows.Forms.TextBox();
            this.lblProdAutoRunWeeklyTo = new System.Windows.Forms.Label();
            this.txtProdAutoRunWeeklyTo = new System.Windows.Forms.TextBox();
            this.lblProdAutoRunWeeklyCC = new System.Windows.Forms.Label();
            this.txtProdAutoRunWeeklyCC = new System.Windows.Forms.TextBox();
            // TabPage for Manual Reports
            this.manualReportsTabPage = new System.Windows.Forms.TabPage();
            this.manualReportsTableLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
            this.lblProdManualRunDailyTo = new System.Windows.Forms.Label();
            this.txtProdManualRunDailyTo = new System.Windows.Forms.TextBox();
            this.lblProdManualRunDailyCC = new System.Windows.Forms.Label();
            this.txtProdManualRunDailyCC = new System.Windows.Forms.TextBox();
            this.lblProdFemiTo = new System.Windows.Forms.Label();
            this.txtProdFemiTo = new System.Windows.Forms.TextBox();
            this.lblProdFemiCC = new System.Windows.Forms.Label();
            this.txtProdFemiCC = new System.Windows.Forms.TextBox();
            this.lblProdTeamTo = new System.Windows.Forms.Label();
            this.txtProdTeamTo = new System.Windows.Forms.TextBox();
            this.lblProdTeamCC = new System.Windows.Forms.Label();
            this.txtProdTeamCC = new System.Windows.Forms.TextBox();
            // TabPage for Debug Settings
            this.debugTabPage = new System.Windows.Forms.TabPage();
            this.debugTableLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
            this.lblDebugTo = new System.Windows.Forms.Label();
            this.txtDebugTo = new System.Windows.Forms.TextBox();
            this.lblDebugCC1 = new System.Windows.Forms.Label();
            this.txtDebugCC1 = new System.Windows.Forms.TextBox();
            this.lblDebugCC2 = new System.Windows.Forms.Label();
            this.txtDebugCC2 = new System.Windows.Forms.TextBox();
            // General Instruction Label
            this.lblInstructions = new System.Windows.Forms.Label();

            this.buttonsFlowLayoutPanel.SuspendLayout();
            this.mainTabControl.SuspendLayout();
            this.automatedReportsTabPage.SuspendLayout();
            this.automatedReportsTableLayoutPanel.SuspendLayout();
            this.manualReportsTabPage.SuspendLayout();
            this.manualReportsTableLayoutPanel.SuspendLayout();
            this.debugTabPage.SuspendLayout();
            this.debugTableLayoutPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // buttonsFlowLayoutPanel
            // 
            this.buttonsFlowLayoutPanel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonsFlowLayoutPanel.Controls.Add(this.btnSave);
            this.buttonsFlowLayoutPanel.Controls.Add(this.btnRestoreDefaults);
            this.buttonsFlowLayoutPanel.Controls.Add(this.btnClose);
            this.buttonsFlowLayoutPanel.FlowDirection = System.Windows.Forms.FlowDirection.RightToLeft;
            this.buttonsFlowLayoutPanel.Location = new System.Drawing.Point(12, 438); // Adjusted Y for new layout
            this.buttonsFlowLayoutPanel.Name = "buttonsFlowLayoutPanel";
            this.buttonsFlowLayoutPanel.Size = new System.Drawing.Size(560, 35);
            this.buttonsFlowLayoutPanel.TabIndex = 2;
            // 
            // btnSave
            // 
            this.btnSave.Location = new System.Drawing.Point(452, 3);
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(105, 28);
            this.btnSave.TabIndex = 0;
            this.btnSave.Text = "&Save and Use";
            this.btnSave.UseVisualStyleBackColor = true;
            this.btnSave.Click += new System.EventHandler(this.BtnSave_Click);
            // 
            // btnRestoreDefaults
            // 
            this.btnRestoreDefaults.Location = new System.Drawing.Point(306, 3);
            this.btnRestoreDefaults.Name = "btnRestoreDefaults";
            this.btnRestoreDefaults.Size = new System.Drawing.Size(140, 28);
            this.btnRestoreDefaults.TabIndex = 1;
            this.btnRestoreDefaults.Text = "&Restore App Defaults";
            this.btnRestoreDefaults.UseVisualStyleBackColor = true;
            this.btnRestoreDefaults.Click += new System.EventHandler(this.BtnRestoreDefaults_Click);
            // 
            // btnClose
            // 
            this.btnClose.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnClose.Location = new System.Drawing.Point(225, 3);
            this.btnClose.Name = "btnClose";
            this.btnClose.Size = new System.Drawing.Size(75, 28);
            this.btnClose.TabIndex = 2;
            this.btnClose.Text = "&Close";
            this.btnClose.UseVisualStyleBackColor = true;
            this.btnClose.Click += new System.EventHandler(this.BtnClose_Click);
            // 
            // mainTabControl
            // 
            this.mainTabControl.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.mainTabControl.Controls.Add(this.automatedReportsTabPage);
            this.mainTabControl.Controls.Add(this.manualReportsTabPage);
            this.mainTabControl.Controls.Add(this.debugTabPage);
            this.mainTabControl.Location = new System.Drawing.Point(12, 42);
            this.mainTabControl.Name = "mainTabControl";
            this.mainTabControl.SelectedIndex = 0;
            this.mainTabControl.Size = new System.Drawing.Size(560, 390); // Adjusted Height
            this.mainTabControl.TabIndex = 1;
            // 
            // automatedReportsTabPage
            // 
            this.automatedReportsTabPage.Controls.Add(this.automatedReportsTableLayoutPanel);
            this.automatedReportsTabPage.Location = new System.Drawing.Point(4, 22);
            this.automatedReportsTabPage.Name = "automatedReportsTabPage";
            this.automatedReportsTabPage.Padding = new System.Windows.Forms.Padding(3);
            this.automatedReportsTabPage.Size = new System.Drawing.Size(552, 364); // Adjusted Height
            this.automatedReportsTabPage.TabIndex = 0;
            this.automatedReportsTabPage.Text = "Automated Reports";
            this.automatedReportsTabPage.UseVisualStyleBackColor = true;
            // 
            // automatedReportsTableLayoutPanel
            // 
            this.automatedReportsTableLayoutPanel.ColumnCount = 2;
            this.automatedReportsTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 220F));
            this.automatedReportsTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.automatedReportsTableLayoutPanel.Controls.Add(this.lblProdAutoRunDailyTo, 0, 0);
            this.automatedReportsTableLayoutPanel.Controls.Add(this.txtProdAutoRunDailyTo, 1, 0);
            this.automatedReportsTableLayoutPanel.Controls.Add(this.lblProdAutoRunDailyCC, 0, 1);
            this.automatedReportsTableLayoutPanel.Controls.Add(this.txtProdAutoRunDailyCC, 1, 1);
            this.automatedReportsTableLayoutPanel.Controls.Add(this.lblProdAutoRunDaily5Day1kTo, 0, 2);
            this.automatedReportsTableLayoutPanel.Controls.Add(this.txtProdAutoRunDaily5Day1kTo, 1, 2);
            this.automatedReportsTableLayoutPanel.Controls.Add(this.lblProdAutoRunDaily5Day1kCC, 0, 3);
            this.automatedReportsTableLayoutPanel.Controls.Add(this.txtProdAutoRunDaily5Day1kCC, 1, 3);
            this.automatedReportsTableLayoutPanel.Controls.Add(this.lblProdAutoRunWeeklyTo, 0, 4);
            this.automatedReportsTableLayoutPanel.Controls.Add(this.txtProdAutoRunWeeklyTo, 1, 4);
            this.automatedReportsTableLayoutPanel.Controls.Add(this.lblProdAutoRunWeeklyCC, 0, 5);
            this.automatedReportsTableLayoutPanel.Controls.Add(this.txtProdAutoRunWeeklyCC, 1, 5);
            this.automatedReportsTableLayoutPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.automatedReportsTableLayoutPanel.Location = new System.Drawing.Point(3, 3);
            this.automatedReportsTableLayoutPanel.Name = "automatedReportsTableLayoutPanel";
            this.automatedReportsTableLayoutPanel.RowCount = 7; // 6 for data, 1 for spacing
            for (int i = 0; i < 6; i++) { this.automatedReportsTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 35F)); }
            this.automatedReportsTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.automatedReportsTableLayoutPanel.Size = new System.Drawing.Size(546, 358);
            this.automatedReportsTableLayoutPanel.TabIndex = 0;
            // 
            // lblProdAutoRunDailyTo
            // 
            this.lblProdAutoRunDailyTo.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.lblProdAutoRunDailyTo.AutoSize = true;
            this.lblProdAutoRunDailyTo.Location = new System.Drawing.Point(70, 11);
            this.lblProdAutoRunDailyTo.Name = "lblProdAutoRunDailyTo";
            this.lblProdAutoRunDailyTo.Size = new System.Drawing.Size(147, 13);
            this.lblProdAutoRunDailyTo.Text = "Automated Std. Daily TO:";
            // 
            // txtProdAutoRunDailyTo
            // 
            this.txtProdAutoRunDailyTo.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.txtProdAutoRunDailyTo.Location = new System.Drawing.Point(223, 7);
            this.txtProdAutoRunDailyTo.Name = "txtProdAutoRunDailyTo";
            this.txtProdAutoRunDailyTo.Size = new System.Drawing.Size(320, 20);
            this.txtProdAutoRunDailyTo.TabIndex = 0;
            // 
            // lblProdAutoRunDailyCC
            // 
            this.lblProdAutoRunDailyCC.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.lblProdAutoRunDailyCC.AutoSize = true;
            this.lblProdAutoRunDailyCC.Location = new System.Drawing.Point(70, 46);
            this.lblProdAutoRunDailyCC.Name = "lblProdAutoRunDailyCC";
            this.lblProdAutoRunDailyCC.Size = new System.Drawing.Size(147, 13);
            this.lblProdAutoRunDailyCC.Text = "Automated Std. Daily CC:";
            // 
            // txtProdAutoRunDailyCC
            // 
            this.txtProdAutoRunDailyCC.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.txtProdAutoRunDailyCC.Location = new System.Drawing.Point(223, 42);
            this.txtProdAutoRunDailyCC.Name = "txtProdAutoRunDailyCC";
            this.txtProdAutoRunDailyCC.Size = new System.Drawing.Size(320, 20);
            this.txtProdAutoRunDailyCC.TabIndex = 1;
            // 
            // lblProdAutoRunDaily5Day1kTo
            // 
            this.lblProdAutoRunDaily5Day1kTo.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.lblProdAutoRunDaily5Day1kTo.AutoSize = true;
            this.lblProdAutoRunDaily5Day1kTo.Location = new System.Drawing.Point(23, 81);
            this.lblProdAutoRunDaily5Day1kTo.Name = "lblProdAutoRunDaily5Day1kTo";
            this.lblProdAutoRunDaily5Day1kTo.Size = new System.Drawing.Size(194, 13);
            this.lblProdAutoRunDaily5Day1kTo.Text = "Automated Daily (5d >= £1k) TO:";
            // 
            // txtProdAutoRunDaily5Day1kTo
            // 
            this.txtProdAutoRunDaily5Day1kTo.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.txtProdAutoRunDaily5Day1kTo.Location = new System.Drawing.Point(223, 77);
            this.txtProdAutoRunDaily5Day1kTo.Name = "txtProdAutoRunDaily5Day1kTo";
            this.txtProdAutoRunDaily5Day1kTo.Size = new System.Drawing.Size(320, 20);
            this.txtProdAutoRunDaily5Day1kTo.TabIndex = 2;
            // 
            // lblProdAutoRunDaily5Day1kCC
            // 
            this.lblProdAutoRunDaily5Day1kCC.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.lblProdAutoRunDaily5Day1kCC.AutoSize = true;
            this.lblProdAutoRunDaily5Day1kCC.Location = new System.Drawing.Point(23, 116);
            this.lblProdAutoRunDaily5Day1kCC.Name = "lblProdAutoRunDaily5Day1kCC";
            this.lblProdAutoRunDaily5Day1kCC.Size = new System.Drawing.Size(194, 13);
            this.lblProdAutoRunDaily5Day1kCC.Text = "Automated Daily (5d >= £1k) CC:";
            // 
            // txtProdAutoRunDaily5Day1kCC
            // 
            this.txtProdAutoRunDaily5Day1kCC.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.txtProdAutoRunDaily5Day1kCC.Location = new System.Drawing.Point(223, 112);
            this.txtProdAutoRunDaily5Day1kCC.Name = "txtProdAutoRunDaily5Day1kCC";
            this.txtProdAutoRunDaily5Day1kCC.Size = new System.Drawing.Size(320, 20);
            this.txtProdAutoRunDaily5Day1kCC.TabIndex = 3;
            // 
            // lblProdAutoRunWeeklyTo
            // 
            this.lblProdAutoRunWeeklyTo.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.lblProdAutoRunWeeklyTo.AutoSize = true;
            this.lblProdAutoRunWeeklyTo.Location = new System.Drawing.Point(64, 151);
            this.lblProdAutoRunWeeklyTo.Name = "lblProdAutoRunWeeklyTo";
            this.lblProdAutoRunWeeklyTo.Size = new System.Drawing.Size(153, 13);
            this.lblProdAutoRunWeeklyTo.Text = "Automated Weekly TO:";
            // 
            // txtProdAutoRunWeeklyTo
            // 
            this.txtProdAutoRunWeeklyTo.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.txtProdAutoRunWeeklyTo.Location = new System.Drawing.Point(223, 147);
            this.txtProdAutoRunWeeklyTo.Name = "txtProdAutoRunWeeklyTo";
            this.txtProdAutoRunWeeklyTo.Size = new System.Drawing.Size(320, 20);
            this.txtProdAutoRunWeeklyTo.TabIndex = 4;
            // 
            // lblProdAutoRunWeeklyCC
            // 
            this.lblProdAutoRunWeeklyCC.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.lblProdAutoRunWeeklyCC.AutoSize = true;
            this.lblProdAutoRunWeeklyCC.Location = new System.Drawing.Point(64, 186);
            this.lblProdAutoRunWeeklyCC.Name = "lblProdAutoRunWeeklyCC";
            this.lblProdAutoRunWeeklyCC.Size = new System.Drawing.Size(153, 13);
            this.lblProdAutoRunWeeklyCC.Text = "Automated Weekly CC:";
            // 
            // txtProdAutoRunWeeklyCC
            // 
            this.txtProdAutoRunWeeklyCC.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.txtProdAutoRunWeeklyCC.Location = new System.Drawing.Point(223, 182);
            this.txtProdAutoRunWeeklyCC.Name = "txtProdAutoRunWeeklyCC";
            this.txtProdAutoRunWeeklyCC.Size = new System.Drawing.Size(320, 20);
            this.txtProdAutoRunWeeklyCC.TabIndex = 5;
            // 
            // manualReportsTabPage
            // 
            this.manualReportsTabPage.Controls.Add(this.manualReportsTableLayoutPanel);
            this.manualReportsTabPage.Location = new System.Drawing.Point(4, 22);
            this.manualReportsTabPage.Name = "manualReportsTabPage";
            this.manualReportsTabPage.Padding = new System.Windows.Forms.Padding(3);
            this.manualReportsTabPage.Size = new System.Drawing.Size(552, 364);
            this.manualReportsTabPage.TabIndex = 1;
            this.manualReportsTabPage.Text = "Manual Reports";
            this.manualReportsTabPage.UseVisualStyleBackColor = true;
            // 
            // manualReportsTableLayoutPanel
            // 
            this.manualReportsTableLayoutPanel.ColumnCount = 2;
            this.manualReportsTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 220F));
            this.manualReportsTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.manualReportsTableLayoutPanel.Controls.Add(this.lblProdManualRunDailyTo, 0, 0);
            this.manualReportsTableLayoutPanel.Controls.Add(this.txtProdManualRunDailyTo, 1, 0);
            this.manualReportsTableLayoutPanel.Controls.Add(this.lblProdManualRunDailyCC, 0, 1);
            this.manualReportsTableLayoutPanel.Controls.Add(this.txtProdManualRunDailyCC, 1, 1);
            this.manualReportsTableLayoutPanel.Controls.Add(this.lblProdFemiTo, 0, 2);
            this.manualReportsTableLayoutPanel.Controls.Add(this.txtProdFemiTo, 1, 2);
            this.manualReportsTableLayoutPanel.Controls.Add(this.lblProdFemiCC, 0, 3);
            this.manualReportsTableLayoutPanel.Controls.Add(this.txtProdFemiCC, 1, 3);
            this.manualReportsTableLayoutPanel.Controls.Add(this.lblProdTeamTo, 0, 4);
            this.manualReportsTableLayoutPanel.Controls.Add(this.txtProdTeamTo, 1, 4);
            this.manualReportsTableLayoutPanel.Controls.Add(this.lblProdTeamCC, 0, 5);
            this.manualReportsTableLayoutPanel.Controls.Add(this.txtProdTeamCC, 1, 5);
            this.manualReportsTableLayoutPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.manualReportsTableLayoutPanel.Location = new System.Drawing.Point(3, 3);
            this.manualReportsTableLayoutPanel.Name = "manualReportsTableLayoutPanel";
            this.manualReportsTableLayoutPanel.RowCount = 7; // 6 for data, 1 for spacing
            for (int i = 0; i < 6; i++) { this.manualReportsTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 35F)); }
            this.manualReportsTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.manualReportsTableLayoutPanel.Size = new System.Drawing.Size(546, 358);
            this.manualReportsTableLayoutPanel.TabIndex = 0;
            // 
            // lblProdManualRunDailyTo 
            // 
            this.lblProdManualRunDailyTo.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.lblProdManualRunDailyTo.AutoSize = true;
            this.lblProdManualRunDailyTo.Location = new System.Drawing.Point(85, 11);
            this.lblProdManualRunDailyTo.Name = "lblProdManualRunDailyTo";
            this.lblProdManualRunDailyTo.Size = new System.Drawing.Size(132, 13);
            this.lblProdManualRunDailyTo.Text = "Manual Std. Daily TO:";
            // 
            // txtProdManualRunDailyTo 
            // 
            this.txtProdManualRunDailyTo.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.txtProdManualRunDailyTo.Location = new System.Drawing.Point(223, 7);
            this.txtProdManualRunDailyTo.Name = "txtProdManualRunDailyTo";
            this.txtProdManualRunDailyTo.Size = new System.Drawing.Size(320, 20);
            this.txtProdManualRunDailyTo.TabIndex = 0;
            // 
            // lblProdManualRunDailyCC 
            // 
            this.lblProdManualRunDailyCC.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.lblProdManualRunDailyCC.AutoSize = true;
            this.lblProdManualRunDailyCC.Location = new System.Drawing.Point(85, 46);
            this.lblProdManualRunDailyCC.Name = "lblProdManualRunDailyCC";
            this.lblProdManualRunDailyCC.Size = new System.Drawing.Size(132, 13);
            this.lblProdManualRunDailyCC.Text = "Manual Std. Daily CC:";
            // 
            // txtProdManualRunDailyCC 
            // 
            this.txtProdManualRunDailyCC.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.txtProdManualRunDailyCC.Location = new System.Drawing.Point(223, 42);
            this.txtProdManualRunDailyCC.Name = "txtProdManualRunDailyCC";
            this.txtProdManualRunDailyCC.Size = new System.Drawing.Size(320, 20);
            this.txtProdManualRunDailyCC.TabIndex = 1;
            // 
            // lblProdFemiTo
            // 
            this.lblProdFemiTo.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.lblProdFemiTo.AutoSize = true;
            this.lblProdFemiTo.Location = new System.Drawing.Point(31, 81);
            this.lblProdFemiTo.Name = "lblProdFemiTo";
            this.lblProdFemiTo.Size = new System.Drawing.Size(186, 13);
            this.lblProdFemiTo.Text = "Manual Non-Daily \'Femi Only\' TO:";
            // 
            // txtProdFemiTo
            // 
            this.txtProdFemiTo.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.txtProdFemiTo.Location = new System.Drawing.Point(223, 77);
            this.txtProdFemiTo.Name = "txtProdFemiTo";
            this.txtProdFemiTo.Size = new System.Drawing.Size(320, 20);
            this.txtProdFemiTo.TabIndex = 2;
            // 
            // lblProdFemiCC
            // 
            this.lblProdFemiCC.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.lblProdFemiCC.AutoSize = true;
            this.lblProdFemiCC.Location = new System.Drawing.Point(31, 116);
            this.lblProdFemiCC.Name = "lblProdFemiCC";
            this.lblProdFemiCC.Size = new System.Drawing.Size(186, 13);
            this.lblProdFemiCC.Text = "Manual Non-Daily \'Femi Only\' CC:";
            // 
            // txtProdFemiCC
            // 
            this.txtProdFemiCC.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.txtProdFemiCC.Location = new System.Drawing.Point(223, 112);
            this.txtProdFemiCC.Name = "txtProdFemiCC";
            this.txtProdFemiCC.Size = new System.Drawing.Size(320, 20);
            this.txtProdFemiCC.TabIndex = 3;
            // 
            // lblProdTeamTo
            // 
            this.lblProdTeamTo.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.lblProdTeamTo.AutoSize = true;
            this.lblProdTeamTo.Location = new System.Drawing.Point(38, 151);
            this.lblProdTeamTo.Name = "lblProdTeamTo";
            this.lblProdTeamTo.Size = new System.Drawing.Size(179, 13);
            this.lblProdTeamTo.Text = "Manual Non-Daily Team TO:";
            // 
            // txtProdTeamTo
            // 
            this.txtProdTeamTo.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.txtProdTeamTo.Location = new System.Drawing.Point(223, 147);
            this.txtProdTeamTo.Name = "txtProdTeamTo";
            this.txtProdTeamTo.Size = new System.Drawing.Size(320, 20);
            this.txtProdTeamTo.TabIndex = 4;
            // 
            // lblProdTeamCC
            // 
            this.lblProdTeamCC.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.lblProdTeamCC.AutoSize = true;
            this.lblProdTeamCC.Location = new System.Drawing.Point(38, 186);
            this.lblProdTeamCC.Name = "lblProdTeamCC";
            this.lblProdTeamCC.Size = new System.Drawing.Size(179, 13);
            this.lblProdTeamCC.Text = "Manual Non-Daily Team CC:";
            // 
            // txtProdTeamCC
            // 
            this.txtProdTeamCC.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.txtProdTeamCC.Location = new System.Drawing.Point(223, 182);
            this.txtProdTeamCC.Name = "txtProdTeamCC";
            this.txtProdTeamCC.Size = new System.Drawing.Size(320, 20);
            this.txtProdTeamCC.TabIndex = 5;
            // 
            // debugTabPage
            // 
            this.debugTabPage.Controls.Add(this.debugTableLayoutPanel);
            this.debugTabPage.Location = new System.Drawing.Point(4, 22);
            this.debugTabPage.Name = "debugTabPage";
            this.debugTabPage.Padding = new System.Windows.Forms.Padding(3);
            this.debugTabPage.Size = new System.Drawing.Size(552, 364);
            this.debugTabPage.TabIndex = 2; // Changed index
            this.debugTabPage.Text = "Debug Recipients";
            this.debugTabPage.UseVisualStyleBackColor = true;
            // 
            // debugTableLayoutPanel
            // 
            this.debugTableLayoutPanel.ColumnCount = 2;
            this.debugTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 220F));
            this.debugTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.debugTableLayoutPanel.Controls.Add(this.lblDebugTo, 0, 0);
            this.debugTableLayoutPanel.Controls.Add(this.txtDebugTo, 1, 0);
            this.debugTableLayoutPanel.Controls.Add(this.lblDebugCC1, 0, 1);
            this.debugTableLayoutPanel.Controls.Add(this.txtDebugCC1, 1, 1);
            this.debugTableLayoutPanel.Controls.Add(this.lblDebugCC2, 0, 2);
            this.debugTableLayoutPanel.Controls.Add(this.txtDebugCC2, 1, 2);
            this.debugTableLayoutPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.debugTableLayoutPanel.Location = new System.Drawing.Point(3, 3);
            this.debugTableLayoutPanel.Name = "debugTableLayoutPanel";
            this.debugTableLayoutPanel.RowCount = 4;
            this.debugTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 35F));
            this.debugTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 35F));
            this.debugTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 35F));
            this.debugTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.debugTableLayoutPanel.Size = new System.Drawing.Size(546, 358);
            this.debugTableLayoutPanel.TabIndex = 0;
            // 
            // lblDebugTo
            // 
            this.lblDebugTo.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.lblDebugTo.AutoSize = true;
            this.lblDebugTo.Location = new System.Drawing.Point(159, 11);
            this.lblDebugTo.Name = "lblDebugTo";
            this.lblDebugTo.Size = new System.Drawing.Size(58, 13);
            this.lblDebugTo.Text = "Debug TO:";
            // 
            // txtDebugTo
            // 
            this.txtDebugTo.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.txtDebugTo.Location = new System.Drawing.Point(223, 7);
            this.txtDebugTo.Name = "txtDebugTo";
            this.txtDebugTo.Size = new System.Drawing.Size(320, 20);
            this.txtDebugTo.TabIndex = 0;
            // 
            // lblDebugCC1
            // 
            this.lblDebugCC1.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.lblDebugCC1.AutoSize = true;
            this.lblDebugCC1.Location = new System.Drawing.Point(153, 46);
            this.lblDebugCC1.Name = "lblDebugCC1";
            this.lblDebugCC1.Size = new System.Drawing.Size(64, 13);
            this.lblDebugCC1.Text = "Debug CC1:";
            // 
            // txtDebugCC1
            // 
            this.txtDebugCC1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.txtDebugCC1.Location = new System.Drawing.Point(223, 42);
            this.txtDebugCC1.Name = "txtDebugCC1";
            this.txtDebugCC1.Size = new System.Drawing.Size(320, 20);
            this.txtDebugCC1.TabIndex = 1;
            // 
            // lblDebugCC2
            // 
            this.lblDebugCC2.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.lblDebugCC2.AutoSize = true;
            this.lblDebugCC2.Location = new System.Drawing.Point(153, 81);
            this.lblDebugCC2.Name = "lblDebugCC2";
            this.lblDebugCC2.Size = new System.Drawing.Size(64, 13);
            this.lblDebugCC2.Text = "Debug CC2:";
            // 
            // txtDebugCC2
            // 
            this.txtDebugCC2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.txtDebugCC2.Location = new System.Drawing.Point(223, 77);
            this.txtDebugCC2.Name = "txtDebugCC2";
            this.txtDebugCC2.Size = new System.Drawing.Size(320, 20);
            this.txtDebugCC2.TabIndex = 2;
            // 
            // lblInstructions
            // 
            this.lblInstructions.AutoSize = true;
            this.lblInstructions.Location = new System.Drawing.Point(12, 9);
            this.lblInstructions.Name = "lblInstructions";
            this.lblInstructions.Size = new System.Drawing.Size(554, 26); // Span width
            this.lblInstructions.TabIndex = 0;
            this.lblInstructions.Text = "Enter email addresses separated by commas (,) or semicolons (;).\r\nLeave a field " +
    "blank to use the application default for that specific recipient list.";
            // 
            // ManageEmailRecipientsForm
            // 
            this.AcceptButton = this.btnSave;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.btnClose;
            this.ClientSize = new System.Drawing.Size(584, 481); // Adjusted height
            this.Controls.Add(this.lblInstructions);
            this.Controls.Add(this.mainTabControl);
            this.Controls.Add(this.buttonsFlowLayoutPanel);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.MinimumSize = new System.Drawing.Size(600, 520); // Adjusted min height
            this.Name = "ManageEmailRecipientsForm";
            this.Text = "Manage Email Recipients";
            this.Load += new System.EventHandler(this.ManageEmailRecipientsForm_Load);
            this.buttonsFlowLayoutPanel.ResumeLayout(false);
            this.mainTabControl.ResumeLayout(false);
            this.automatedReportsTabPage.ResumeLayout(false);
            this.automatedReportsTableLayoutPanel.ResumeLayout(false);
            this.automatedReportsTableLayoutPanel.PerformLayout();
            this.manualReportsTabPage.ResumeLayout(false);
            this.manualReportsTableLayoutPanel.ResumeLayout(false);
            this.manualReportsTableLayoutPanel.PerformLayout();
            this.debugTabPage.ResumeLayout(false);
            this.debugTableLayoutPanel.ResumeLayout(false);
            this.debugTableLayoutPanel.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private System.Windows.Forms.FlowLayoutPanel buttonsFlowLayoutPanel;
        private System.Windows.Forms.Button btnSave;
        private System.Windows.Forms.Button btnRestoreDefaults;
        private System.Windows.Forms.Button btnClose;
        private System.Windows.Forms.ToolTip toolTipProvider;
        private System.Windows.Forms.TabControl mainTabControl;
        private System.Windows.Forms.TabPage productionTabPage; // Renamed to automatedReportsTabPage
        private System.Windows.Forms.TabPage automatedReportsTabPage; // Explicit declaration
        private System.Windows.Forms.TableLayoutPanel automatedReportsTableLayoutPanel;
        private System.Windows.Forms.Label lblProdAutoRunDailyTo;
        private System.Windows.Forms.TextBox txtProdAutoRunDailyTo;
        private System.Windows.Forms.Label lblProdAutoRunDailyCC;
        private System.Windows.Forms.TextBox txtProdAutoRunDailyCC;
        private System.Windows.Forms.Label lblProdAutoRunDaily5Day1kTo;
        private System.Windows.Forms.TextBox txtProdAutoRunDaily5Day1kTo;
        private System.Windows.Forms.Label lblProdAutoRunDaily5Day1kCC;
        private System.Windows.Forms.TextBox txtProdAutoRunDaily5Day1kCC;
        private System.Windows.Forms.Label lblProdAutoRunWeeklyTo;
        private System.Windows.Forms.TextBox txtProdAutoRunWeeklyTo;
        private System.Windows.Forms.Label lblProdAutoRunWeeklyCC;
        private System.Windows.Forms.TextBox txtProdAutoRunWeeklyCC;
        private System.Windows.Forms.TabPage manualReportsTabPage;
        private System.Windows.Forms.TableLayoutPanel manualReportsTableLayoutPanel;
        private System.Windows.Forms.Label lblProdManualRunDailyTo;
        private System.Windows.Forms.TextBox txtProdManualRunDailyTo;
        private System.Windows.Forms.Label lblProdManualRunDailyCC;
        private System.Windows.Forms.TextBox txtProdManualRunDailyCC;
        private System.Windows.Forms.Label lblProdFemiTo;
        private System.Windows.Forms.TextBox txtProdFemiTo;
        private System.Windows.Forms.Label lblProdFemiCC;
        private System.Windows.Forms.TextBox txtProdFemiCC;
        private System.Windows.Forms.Label lblProdTeamTo;
        private System.Windows.Forms.TextBox txtProdTeamTo;
        private System.Windows.Forms.Label lblProdTeamCC;
        private System.Windows.Forms.TextBox txtProdTeamCC;
        private System.Windows.Forms.TabPage debugTabPage;
        private System.Windows.Forms.TableLayoutPanel debugTableLayoutPanel;
        private System.Windows.Forms.Label lblDebugTo;
        private System.Windows.Forms.TextBox txtDebugTo;
        private System.Windows.Forms.Label lblDebugCC1;
        private System.Windows.Forms.TextBox txtDebugCC1;
        private System.Windows.Forms.Label lblDebugCC2;
        private System.Windows.Forms.TextBox txtDebugCC2;
        private System.Windows.Forms.Label lblInstructions;
    }
}
