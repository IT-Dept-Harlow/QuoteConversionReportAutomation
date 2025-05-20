// ManageEmailRecipientsForm.Designer.cs
// Make sure the namespace matches your project structure
namespace QuoteConversionReportAutomation
{
    partial class ManageEmailRecipientsForm
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
            this.mainTableLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
            this.lblInstructions = new System.Windows.Forms.Label();

            this.lblProdAutoRunDailyTo = new System.Windows.Forms.Label();
            this.txtProdAutoRunDailyTo = new System.Windows.Forms.TextBox();
            this.lblProdAutoRunDailyCC = new System.Windows.Forms.Label();
            this.txtProdAutoRunDailyCC = new System.Windows.Forms.TextBox();

            // New Labels and TextBoxes for Manual Standard Daily
            this.lblProdManualRunDailyTo = new System.Windows.Forms.Label();
            this.txtProdManualRunDailyTo = new System.Windows.Forms.TextBox();
            this.lblProdManualRunDailyCC = new System.Windows.Forms.Label();
            this.txtProdManualRunDailyCC = new System.Windows.Forms.TextBox();

            this.lblProdAutoRunDaily5Day1kTo = new System.Windows.Forms.Label();
            this.txtProdAutoRunDaily5Day1kTo = new System.Windows.Forms.TextBox();
            this.lblProdAutoRunDaily5Day1kCC = new System.Windows.Forms.Label();
            this.txtProdAutoRunDaily5Day1kCC = new System.Windows.Forms.TextBox();

            this.lblProdFemiTo = new System.Windows.Forms.Label();
            this.txtProdFemiTo = new System.Windows.Forms.TextBox();
            this.lblProdFemiCC = new System.Windows.Forms.Label();
            this.txtProdFemiCC = new System.Windows.Forms.TextBox();
            this.lblProdTeamTo = new System.Windows.Forms.Label();
            this.txtProdTeamTo = new System.Windows.Forms.TextBox();
            this.lblProdTeamCC = new System.Windows.Forms.Label();
            this.txtProdTeamCC = new System.Windows.Forms.TextBox();

            this.lblDebugTo = new System.Windows.Forms.Label();
            this.txtDebugTo = new System.Windows.Forms.TextBox();
            this.lblDebugCC1 = new System.Windows.Forms.Label();
            this.txtDebugCC1 = new System.Windows.Forms.TextBox();
            this.lblDebugCC2 = new System.Windows.Forms.Label();
            this.txtDebugCC2 = new System.Windows.Forms.TextBox();

            this.buttonsFlowLayoutPanel = new System.Windows.Forms.FlowLayoutPanel();
            this.btnSave = new System.Windows.Forms.Button();
            this.btnRestoreDefaults = new System.Windows.Forms.Button();
            this.btnClose = new System.Windows.Forms.Button();
            this.toolTipProvider = new System.Windows.Forms.ToolTip(this.components);
            this.mainTableLayoutPanel.SuspendLayout();
            this.buttonsFlowLayoutPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // mainTableLayoutPanel
            // 
            this.mainTableLayoutPanel.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.mainTableLayoutPanel.ColumnCount = 2;
            this.mainTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 210F)); // Increased width for longer labels
            this.mainTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.mainTableLayoutPanel.Controls.Add(this.lblInstructions, 0, 0);
            this.mainTableLayoutPanel.Controls.Add(this.lblProdAutoRunDailyTo, 0, 1);
            this.mainTableLayoutPanel.Controls.Add(this.txtProdAutoRunDailyTo, 1, 1);
            this.mainTableLayoutPanel.Controls.Add(this.lblProdAutoRunDailyCC, 0, 2);
            this.mainTableLayoutPanel.Controls.Add(this.txtProdAutoRunDailyCC, 1, 2);
            this.mainTableLayoutPanel.Controls.Add(this.lblProdManualRunDailyTo, 0, 3); // New
            this.mainTableLayoutPanel.Controls.Add(this.txtProdManualRunDailyTo, 1, 3); // New
            this.mainTableLayoutPanel.Controls.Add(this.lblProdManualRunDailyCC, 0, 4); // New
            this.mainTableLayoutPanel.Controls.Add(this.txtProdManualRunDailyCC, 1, 4); // New
            this.mainTableLayoutPanel.Controls.Add(this.lblProdAutoRunDaily5Day1kTo, 0, 5);
            this.mainTableLayoutPanel.Controls.Add(this.txtProdAutoRunDaily5Day1kTo, 1, 5);
            this.mainTableLayoutPanel.Controls.Add(this.lblProdAutoRunDaily5Day1kCC, 0, 6);
            this.mainTableLayoutPanel.Controls.Add(this.txtProdAutoRunDaily5Day1kCC, 1, 6);
            this.mainTableLayoutPanel.Controls.Add(this.lblProdFemiTo, 0, 7);
            this.mainTableLayoutPanel.Controls.Add(this.txtProdFemiTo, 1, 7);
            this.mainTableLayoutPanel.Controls.Add(this.lblProdFemiCC, 0, 8);
            this.mainTableLayoutPanel.Controls.Add(this.txtProdFemiCC, 1, 8);
            this.mainTableLayoutPanel.Controls.Add(this.lblProdTeamTo, 0, 9);
            this.mainTableLayoutPanel.Controls.Add(this.txtProdTeamTo, 1, 9);
            this.mainTableLayoutPanel.Controls.Add(this.lblProdTeamCC, 0, 10);
            this.mainTableLayoutPanel.Controls.Add(this.txtProdTeamCC, 1, 10);
            this.mainTableLayoutPanel.Controls.Add(this.lblDebugTo, 0, 11);
            this.mainTableLayoutPanel.Controls.Add(this.txtDebugTo, 1, 11);
            this.mainTableLayoutPanel.Controls.Add(this.lblDebugCC1, 0, 12);
            this.mainTableLayoutPanel.Controls.Add(this.txtDebugCC1, 1, 12);
            this.mainTableLayoutPanel.Controls.Add(this.lblDebugCC2, 0, 13);
            this.mainTableLayoutPanel.Controls.Add(this.txtDebugCC2, 1, 13);
            this.mainTableLayoutPanel.Location = new System.Drawing.Point(12, 12);
            this.mainTableLayoutPanel.Name = "mainTableLayoutPanel";
            this.mainTableLayoutPanel.RowCount = 14; // Increased row count for new fields
            this.mainTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 40F)); // Instructions row
            // Adjust RowStyle percentages if necessary, for 13 data rows now: 100/13 = ~7.69%
            float percentHeight = 100F / 13F;
            for (int i = 0; i < 13; i++)
            {
                this.mainTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, percentHeight));
            }
            this.mainTableLayoutPanel.Size = new System.Drawing.Size(560, 480); // Adjusted height if necessary
            this.mainTableLayoutPanel.TabIndex = 0;
            // 
            // lblInstructions
            // 
            this.lblInstructions.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.lblInstructions.AutoSize = true;
            this.mainTableLayoutPanel.SetColumnSpan(this.lblInstructions, 2);
            this.lblInstructions.Location = new System.Drawing.Point(3, 7);
            this.lblInstructions.Name = "lblInstructions";
            this.lblInstructions.Size = new System.Drawing.Size(554, 26);
            this.lblInstructions.TabIndex = 21; // Adjusted TabIndex
            this.lblInstructions.Text = "Enter email addresses separated by commas (,) or semicolons (;).\r\nLeave a field " +
    "blank to use the application default for that specific recipient list.";
            // 
            // lblProdAutoRunDailyTo
            // 
            this.lblProdAutoRunDailyTo.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.lblProdAutoRunDailyTo.AutoSize = true;
            this.lblProdAutoRunDailyTo.Location = new System.Drawing.Point(39, 49); // Adjusted X for wider ColumnStyle[0]
            this.lblProdAutoRunDailyTo.Name = "lblProdAutoRunDailyTo";
            this.lblProdAutoRunDailyTo.Size = new System.Drawing.Size(168, 13);
            this.lblProdAutoRunDailyTo.TabIndex = 0;
            this.lblProdAutoRunDailyTo.Text = "Automated Std. Daily TO:";
            // 
            // txtProdAutoRunDailyTo
            // 
            this.txtProdAutoRunDailyTo.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.txtProdAutoRunDailyTo.Location = new System.Drawing.Point(213, 46); // Adjusted X
            this.txtProdAutoRunDailyTo.Name = "txtProdAutoRunDailyTo";
            this.txtProdAutoRunDailyTo.Size = new System.Drawing.Size(344, 20);
            this.txtProdAutoRunDailyTo.TabIndex = 0;
            // 
            // lblProdAutoRunDailyCC
            // 
            this.lblProdAutoRunDailyCC.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.lblProdAutoRunDailyCC.AutoSize = true;
            this.lblProdAutoRunDailyCC.Location = new System.Drawing.Point(39, 82); // Adjusted Y and X
            this.lblProdAutoRunDailyCC.Name = "lblProdAutoRunDailyCC";
            this.lblProdAutoRunDailyCC.Size = new System.Drawing.Size(168, 13);
            this.lblProdAutoRunDailyCC.TabIndex = 2;
            this.lblProdAutoRunDailyCC.Text = "Automated Std. Daily CC:";
            // 
            // txtProdAutoRunDailyCC
            // 
            this.txtProdAutoRunDailyCC.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.txtProdAutoRunDailyCC.Location = new System.Drawing.Point(213, 79); // Adjusted Y and X
            this.txtProdAutoRunDailyCC.Name = "txtProdAutoRunDailyCC";
            this.txtProdAutoRunDailyCC.Size = new System.Drawing.Size(344, 20);
            this.txtProdAutoRunDailyCC.TabIndex = 1;
            // 
            // lblProdManualRunDailyTo // New
            // 
            this.lblProdManualRunDailyTo.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.lblProdManualRunDailyTo.AutoSize = true;
            this.lblProdManualRunDailyTo.Location = new System.Drawing.Point(54, 115); // Adjusted Y and X
            this.lblProdManualRunDailyTo.Name = "lblProdManualRunDailyTo";
            this.lblProdManualRunDailyTo.Size = new System.Drawing.Size(153, 13);
            this.lblProdManualRunDailyTo.TabIndex = 4;
            this.lblProdManualRunDailyTo.Text = "Manual Std. Daily TO:";
            // 
            // txtProdManualRunDailyTo // New
            // 
            this.txtProdManualRunDailyTo.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.txtProdManualRunDailyTo.Location = new System.Drawing.Point(213, 112); // Adjusted Y and X
            this.txtProdManualRunDailyTo.Name = "txtProdManualRunDailyTo";
            this.txtProdManualRunDailyTo.Size = new System.Drawing.Size(344, 20);
            this.txtProdManualRunDailyTo.TabIndex = 2;
            // 
            // lblProdManualRunDailyCC // New
            // 
            this.lblProdManualRunDailyCC.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.lblProdManualRunDailyCC.AutoSize = true;
            this.lblProdManualRunDailyCC.Location = new System.Drawing.Point(54, 148); // Adjusted Y and X
            this.lblProdManualRunDailyCC.Name = "lblProdManualRunDailyCC";
            this.lblProdManualRunDailyCC.Size = new System.Drawing.Size(153, 13);
            this.lblProdManualRunDailyCC.TabIndex = 6;
            this.lblProdManualRunDailyCC.Text = "Manual Std. Daily CC:";
            // 
            // txtProdManualRunDailyCC // New
            // 
            this.txtProdManualRunDailyCC.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.txtProdManualRunDailyCC.Location = new System.Drawing.Point(213, 145); // Adjusted Y and X
            this.txtProdManualRunDailyCC.Name = "txtProdManualRunDailyCC";
            this.txtProdManualRunDailyCC.Size = new System.Drawing.Size(344, 20);
            this.txtProdManualRunDailyCC.TabIndex = 3;
            // 
            // lblProdAutoRunDaily5Day1kTo
            // 
            this.lblProdAutoRunDaily5Day1kTo.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.lblProdAutoRunDaily5Day1kTo.AutoSize = true;
            this.lblProdAutoRunDaily5Day1kTo.Location = new System.Drawing.Point(12, 181); // Adjusted Y
            this.lblProdAutoRunDaily5Day1kTo.Name = "lblProdAutoRunDaily5Day1kTo";
            this.lblProdAutoRunDaily5Day1kTo.Size = new System.Drawing.Size(195, 13);
            this.lblProdAutoRunDaily5Day1kTo.TabIndex = 8; // Adjusted TabIndex
            this.lblProdAutoRunDaily5Day1kTo.Text = "Automated Daily (5d >= £1k) TO:";
            // 
            // txtProdAutoRunDaily5Day1kTo
            // 
            this.txtProdAutoRunDaily5Day1kTo.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.txtProdAutoRunDaily5Day1kTo.Location = new System.Drawing.Point(213, 178); // Adjusted Y
            this.txtProdAutoRunDaily5Day1kTo.Name = "txtProdAutoRunDaily5Day1kTo";
            this.txtProdAutoRunDaily5Day1kTo.Size = new System.Drawing.Size(344, 20);
            this.txtProdAutoRunDaily5Day1kTo.TabIndex = 4; // Adjusted TabIndex
            // 
            // lblProdAutoRunDaily5Day1kCC
            // 
            this.lblProdAutoRunDaily5Day1kCC.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.lblProdAutoRunDaily5Day1kCC.AutoSize = true;
            this.lblProdAutoRunDaily5Day1kCC.Location = new System.Drawing.Point(12, 214); // Adjusted Y
            this.lblProdAutoRunDaily5Day1kCC.Name = "lblProdAutoRunDaily5Day1kCC";
            this.lblProdAutoRunDaily5Day1kCC.Size = new System.Drawing.Size(195, 13);
            this.lblProdAutoRunDaily5Day1kCC.TabIndex = 10; // Adjusted TabIndex
            this.lblProdAutoRunDaily5Day1kCC.Text = "Automated Daily (5d >= £1k) CC:";
            // 
            // txtProdAutoRunDaily5Day1kCC
            // 
            this.txtProdAutoRunDaily5Day1kCC.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.txtProdAutoRunDaily5Day1kCC.Location = new System.Drawing.Point(213, 211); // Adjusted Y
            this.txtProdAutoRunDaily5Day1kCC.Name = "txtProdAutoRunDaily5Day1kCC";
            this.txtProdAutoRunDaily5Day1kCC.Size = new System.Drawing.Size(344, 20);
            this.txtProdAutoRunDaily5Day1kCC.TabIndex = 5; // Adjusted TabIndex
            // 
            // lblProdFemiTo
            // 
            this.lblProdFemiTo.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.lblProdFemiTo.AutoSize = true;
            this.lblProdFemiTo.Location = new System.Drawing.Point(59, 247); // Adjusted Y
            this.lblProdFemiTo.Name = "lblProdFemiTo";
            this.lblProdFemiTo.Size = new System.Drawing.Size(148, 13);
            this.lblProdFemiTo.TabIndex = 12; // Adjusted TabIndex
            this.lblProdFemiTo.Text = "Manual Non-Daily \'Femi Only\' TO:";
            // 
            // txtProdFemiTo
            // 
            this.txtProdFemiTo.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.txtProdFemiTo.Location = new System.Drawing.Point(213, 244); // Adjusted Y
            this.txtProdFemiTo.Name = "txtProdFemiTo";
            this.txtProdFemiTo.Size = new System.Drawing.Size(344, 20);
            this.txtProdFemiTo.TabIndex = 6; // Adjusted TabIndex
            // 
            // lblProdFemiCC
            // 
            this.lblProdFemiCC.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.lblProdFemiCC.AutoSize = true;
            this.lblProdFemiCC.Location = new System.Drawing.Point(59, 280); // Adjusted Y
            this.lblProdFemiCC.Name = "lblProdFemiCC";
            this.lblProdFemiCC.Size = new System.Drawing.Size(148, 13);
            this.lblProdFemiCC.TabIndex = 14; // Adjusted TabIndex
            this.lblProdFemiCC.Text = "Manual Non-Daily \'Femi Only\' CC:";
            // 
            // txtProdFemiCC
            // 
            this.txtProdFemiCC.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.txtProdFemiCC.Location = new System.Drawing.Point(213, 277); // Adjusted Y
            this.txtProdFemiCC.Name = "txtProdFemiCC";
            this.txtProdFemiCC.Size = new System.Drawing.Size(344, 20);
            this.txtProdFemiCC.TabIndex = 7; // Adjusted TabIndex
            // 
            // lblProdTeamTo
            // 
            this.lblProdTeamTo.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.lblProdTeamTo.AutoSize = true;
            this.lblProdTeamTo.Location = new System.Drawing.Point(66, 313); // Adjusted Y
            this.lblProdTeamTo.Name = "lblProdTeamTo";
            this.lblProdTeamTo.Size = new System.Drawing.Size(141, 13);
            this.lblProdTeamTo.TabIndex = 16; // Adjusted TabIndex
            this.lblProdTeamTo.Text = "Manual Non-Daily Team TO:";
            // 
            // txtProdTeamTo
            // 
            this.txtProdTeamTo.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.txtProdTeamTo.Location = new System.Drawing.Point(213, 310); // Adjusted Y
            this.txtProdTeamTo.Name = "txtProdTeamTo";
            this.txtProdTeamTo.Size = new System.Drawing.Size(344, 20);
            this.txtProdTeamTo.TabIndex = 8; // Adjusted TabIndex
            // 
            // lblProdTeamCC
            // 
            this.lblProdTeamCC.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.lblProdTeamCC.AutoSize = true;
            this.lblProdTeamCC.Location = new System.Drawing.Point(66, 346); // Adjusted Y
            this.lblProdTeamCC.Name = "lblProdTeamCC";
            this.lblProdTeamCC.Size = new System.Drawing.Size(141, 13);
            this.lblProdTeamCC.TabIndex = 18; // Adjusted TabIndex
            this.lblProdTeamCC.Text = "Manual Non-Daily Team CC:";
            // 
            // txtProdTeamCC
            // 
            this.txtProdTeamCC.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.txtProdTeamCC.Location = new System.Drawing.Point(213, 343); // Adjusted Y
            this.txtProdTeamCC.Name = "txtProdTeamCC";
            this.txtProdTeamCC.Size = new System.Drawing.Size(344, 20);
            this.txtProdTeamCC.TabIndex = 9; // Adjusted TabIndex
            // 
            // lblDebugTo
            // 
            this.lblDebugTo.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.lblDebugTo.AutoSize = true;
            this.lblDebugTo.Location = new System.Drawing.Point(149, 379); // Adjusted Y
            this.lblDebugTo.Name = "lblDebugTo";
            this.lblDebugTo.Size = new System.Drawing.Size(58, 13);
            this.lblDebugTo.TabIndex = 20; // Adjusted TabIndex
            this.lblDebugTo.Text = "Debug TO:";
            // 
            // txtDebugTo
            // 
            this.txtDebugTo.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.txtDebugTo.Location = new System.Drawing.Point(213, 376); // Adjusted Y
            this.txtDebugTo.Name = "txtDebugTo";
            this.txtDebugTo.Size = new System.Drawing.Size(344, 20);
            this.txtDebugTo.TabIndex = 10; // Adjusted TabIndex
            // 
            // lblDebugCC1
            // 
            this.lblDebugCC1.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.lblDebugCC1.AutoSize = true;
            this.lblDebugCC1.Location = new System.Drawing.Point(143, 412); // Adjusted Y
            this.lblDebugCC1.Name = "lblDebugCC1";
            this.lblDebugCC1.Size = new System.Drawing.Size(64, 13);
            this.lblDebugCC1.TabIndex = 22; // Adjusted TabIndex
            this.lblDebugCC1.Text = "Debug CC1:";
            // 
            // txtDebugCC1
            // 
            this.txtDebugCC1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.txtDebugCC1.Location = new System.Drawing.Point(213, 409); // Adjusted Y
            this.txtDebugCC1.Name = "txtDebugCC1";
            this.txtDebugCC1.Size = new System.Drawing.Size(344, 20);
            this.txtDebugCC1.TabIndex = 11; // Adjusted TabIndex
            // 
            // lblDebugCC2
            // 
            this.lblDebugCC2.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.lblDebugCC2.AutoSize = true;
            this.lblDebugCC2.Location = new System.Drawing.Point(143, 446); // Adjusted Y
            this.lblDebugCC2.Name = "lblDebugCC2";
            this.lblDebugCC2.Size = new System.Drawing.Size(64, 13);
            this.lblDebugCC2.TabIndex = 24; // Adjusted TabIndex
            this.lblDebugCC2.Text = "Debug CC2:";
            // 
            // txtDebugCC2
            // 
            this.txtDebugCC2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.txtDebugCC2.Location = new System.Drawing.Point(213, 443); // Adjusted Y
            this.txtDebugCC2.Name = "txtDebugCC2";
            this.txtDebugCC2.Size = new System.Drawing.Size(344, 20);
            this.txtDebugCC2.TabIndex = 12; // Adjusted TabIndex
            // 
            // buttonsFlowLayoutPanel
            // 
            this.buttonsFlowLayoutPanel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonsFlowLayoutPanel.Controls.Add(this.btnSave);
            this.buttonsFlowLayoutPanel.Controls.Add(this.btnRestoreDefaults);
            this.buttonsFlowLayoutPanel.Controls.Add(this.btnClose);
            this.buttonsFlowLayoutPanel.FlowDirection = System.Windows.Forms.FlowDirection.RightToLeft;
            this.buttonsFlowLayoutPanel.Location = new System.Drawing.Point(12, 498); // Adjusted Y
            this.buttonsFlowLayoutPanel.Name = "buttonsFlowLayoutPanel";
            this.buttonsFlowLayoutPanel.Size = new System.Drawing.Size(560, 35);
            this.buttonsFlowLayoutPanel.TabIndex = 1;
            // 
            // btnSave
            // 
            this.btnSave.Location = new System.Drawing.Point(452, 3);
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(105, 28);
            this.btnSave.TabIndex = 13; // Adjusted TabIndex
            this.btnSave.Text = "&Save and Use";
            this.btnSave.UseVisualStyleBackColor = true;
            this.btnSave.Click += new System.EventHandler(this.BtnSave_Click);
            // 
            // btnRestoreDefaults
            // 
            this.btnRestoreDefaults.Location = new System.Drawing.Point(306, 3);
            this.btnRestoreDefaults.Name = "btnRestoreDefaults";
            this.btnRestoreDefaults.Size = new System.Drawing.Size(140, 28);
            this.btnRestoreDefaults.TabIndex = 14; // Adjusted TabIndex
            this.btnRestoreDefaults.Text = "&Restore Application Defaults";
            this.btnRestoreDefaults.UseVisualStyleBackColor = true;
            this.btnRestoreDefaults.Click += new System.EventHandler(this.BtnRestoreDefaults_Click);
            // 
            // btnClose
            // 
            this.btnClose.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnClose.Location = new System.Drawing.Point(225, 3);
            this.btnClose.Name = "btnClose";
            this.btnClose.Size = new System.Drawing.Size(75, 28);
            this.btnClose.TabIndex = 15; // Adjusted TabIndex
            this.btnClose.Text = "&Close";
            this.btnClose.UseVisualStyleBackColor = true;
            this.btnClose.Click += new System.EventHandler(this.BtnClose_Click);
            // 
            // ManageEmailRecipientsForm
            // 
            this.AcceptButton = this.btnSave;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.btnClose;
            this.ClientSize = new System.Drawing.Size(584, 541); // Adjusted height
            this.Controls.Add(this.buttonsFlowLayoutPanel);
            this.Controls.Add(this.mainTableLayoutPanel);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.MinimumSize = new System.Drawing.Size(600, 580); // Adjusted min height
            this.Name = "ManageEmailRecipientsForm";
            this.Text = "Email Recipients Manager";
            this.Load += new System.EventHandler(this.ManageEmailRecipientsForm_Load);
            this.mainTableLayoutPanel.ResumeLayout(false);
            this.mainTableLayoutPanel.PerformLayout();
            this.buttonsFlowLayoutPanel.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel mainTableLayoutPanel;
        private System.Windows.Forms.Label lblInstructions;
        private System.Windows.Forms.Label lblProdAutoRunDailyTo;
        private System.Windows.Forms.TextBox txtProdAutoRunDailyTo;
        private System.Windows.Forms.Label lblProdAutoRunDailyCC;
        private System.Windows.Forms.TextBox txtProdAutoRunDailyCC;
        // New Controls
        private System.Windows.Forms.Label lblProdManualRunDailyTo;
        private System.Windows.Forms.TextBox txtProdManualRunDailyTo;
        private System.Windows.Forms.Label lblProdManualRunDailyCC;
        private System.Windows.Forms.TextBox txtProdManualRunDailyCC;

        private System.Windows.Forms.Label lblProdAutoRunDaily5Day1kTo;
        private System.Windows.Forms.TextBox txtProdAutoRunDaily5Day1kTo;
        private System.Windows.Forms.Label lblProdAutoRunDaily5Day1kCC;
        private System.Windows.Forms.TextBox txtProdAutoRunDaily5Day1kCC;
        private System.Windows.Forms.Label lblProdFemiTo;
        private System.Windows.Forms.TextBox txtProdFemiTo;
        private System.Windows.Forms.Label lblProdFemiCC;
        private System.Windows.Forms.TextBox txtProdFemiCC;
        private System.Windows.Forms.Label lblProdTeamTo;
        private System.Windows.Forms.TextBox txtProdTeamTo;
        private System.Windows.Forms.Label lblProdTeamCC;
        private System.Windows.Forms.TextBox txtProdTeamCC;
        private System.Windows.Forms.Label lblDebugTo;
        private System.Windows.Forms.TextBox txtDebugTo;
        private System.Windows.Forms.Label lblDebugCC1;
        private System.Windows.Forms.TextBox txtDebugCC1;
        private System.Windows.Forms.Label lblDebugCC2;
        private System.Windows.Forms.TextBox txtDebugCC2;
        private System.Windows.Forms.FlowLayoutPanel buttonsFlowLayoutPanel;
        private System.Windows.Forms.Button btnSave;
        private System.Windows.Forms.Button btnRestoreDefaults;
        private System.Windows.Forms.Button btnClose;
        private System.Windows.Forms.ToolTip toolTipProvider;
    }
}
