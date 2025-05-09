// ManageEmailRecipientsForm.Designer.cs
// Make sure the namespace matches your project structure, e.g., QuoteConversionReportAutomation or conversionTest
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
            components = new System.ComponentModel.Container();
            mainTableLayoutPanel = new TableLayoutPanel();
            lblProdAutoRunDailyTo = new Label();
            txtProdAutoRunDailyTo = new TextBox();
            lblProdAutoRunDailyCC = new Label();
            txtProdAutoRunDailyCC = new TextBox();
            lblProdFemiTo = new Label();
            txtProdFemiTo = new TextBox();
            lblProdFemiCC = new Label();
            txtProdFemiCC = new TextBox();
            lblProdTeamTo = new Label();
            txtProdTeamTo = new TextBox();
            lblProdTeamCC = new Label();
            txtProdTeamCC = new TextBox();
            lblDebugTo = new Label();
            txtDebugTo = new TextBox();
            lblDebugCC1 = new Label();
            txtDebugCC1 = new TextBox();
            lblDebugCC2 = new Label();
            txtDebugCC2 = new TextBox();
            lblInstructions = new Label();
            buttonsFlowLayoutPanel = new FlowLayoutPanel();
            btnSave = new Button();
            btnRestoreDefaults = new Button();
            btnClose = new Button();
            toolTipInfo = new ToolTip(components);
            mainTableLayoutPanel.SuspendLayout();
            buttonsFlowLayoutPanel.SuspendLayout();
            SuspendLayout();
            // 
            // mainTableLayoutPanel
            // 
            mainTableLayoutPanel.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            mainTableLayoutPanel.ColumnCount = 2;
            mainTableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 233F));
            mainTableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            mainTableLayoutPanel.Controls.Add(lblProdAutoRunDailyTo, 0, 1);
            mainTableLayoutPanel.Controls.Add(txtProdAutoRunDailyTo, 1, 1);
            mainTableLayoutPanel.Controls.Add(lblProdAutoRunDailyCC, 0, 2);
            mainTableLayoutPanel.Controls.Add(txtProdAutoRunDailyCC, 1, 2);
            mainTableLayoutPanel.Controls.Add(lblProdFemiTo, 0, 3);
            mainTableLayoutPanel.Controls.Add(txtProdFemiTo, 1, 3);
            mainTableLayoutPanel.Controls.Add(lblProdFemiCC, 0, 4);
            mainTableLayoutPanel.Controls.Add(txtProdFemiCC, 1, 4);
            mainTableLayoutPanel.Controls.Add(lblProdTeamTo, 0, 5);
            mainTableLayoutPanel.Controls.Add(txtProdTeamTo, 1, 5);
            mainTableLayoutPanel.Controls.Add(lblProdTeamCC, 0, 6);
            mainTableLayoutPanel.Controls.Add(txtProdTeamCC, 1, 6);
            mainTableLayoutPanel.Controls.Add(lblDebugTo, 0, 7);
            mainTableLayoutPanel.Controls.Add(txtDebugTo, 1, 7);
            mainTableLayoutPanel.Controls.Add(lblDebugCC1, 0, 8);
            mainTableLayoutPanel.Controls.Add(txtDebugCC1, 1, 8);
            mainTableLayoutPanel.Controls.Add(lblDebugCC2, 0, 9);
            mainTableLayoutPanel.Controls.Add(txtDebugCC2, 1, 9);
            mainTableLayoutPanel.Controls.Add(lblInstructions, 0, 0);
            mainTableLayoutPanel.Location = new Point(14, 14);
            mainTableLayoutPanel.Margin = new Padding(4, 3, 4, 3);
            mainTableLayoutPanel.Name = "mainTableLayoutPanel";
            mainTableLayoutPanel.RowCount = 10;
            mainTableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 58F));
            mainTableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 11.11111F));
            mainTableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 11.11111F));
            mainTableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 11.11111F));
            mainTableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 11.11111F));
            mainTableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 11.11111F));
            mainTableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 11.11111F));
            mainTableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 11.11111F));
            mainTableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 11.11111F));
            mainTableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 11.11111F));
            mainTableLayoutPanel.Size = new Size(887, 519);
            mainTableLayoutPanel.TabIndex = 0;
            // 
            // lblProdAutoRunDailyTo
            // 
            lblProdAutoRunDailyTo.Anchor = AnchorStyles.Right;
            lblProdAutoRunDailyTo.AutoSize = true;
            lblProdAutoRunDailyTo.Location = new Point(64, 76);
            lblProdAutoRunDailyTo.Margin = new Padding(4, 0, 4, 0);
            lblProdAutoRunDailyTo.Name = "lblProdAutoRunDailyTo";
            lblProdAutoRunDailyTo.Size = new Size(165, 15);
            lblProdAutoRunDailyTo.TabIndex = 0;
            lblProdAutoRunDailyTo.Text = "Production AutoRun Daily TO:";
            // 
            // txtProdAutoRunDailyTo
            // 
            txtProdAutoRunDailyTo.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            txtProdAutoRunDailyTo.Location = new Point(237, 72);
            txtProdAutoRunDailyTo.Margin = new Padding(4, 3, 4, 3);
            txtProdAutoRunDailyTo.Name = "txtProdAutoRunDailyTo";
            txtProdAutoRunDailyTo.Size = new Size(646, 23);
            txtProdAutoRunDailyTo.TabIndex = 1;
            toolTipInfo.SetToolTip(txtProdAutoRunDailyTo, "Recipients for automated daily reports in Production mode.");
            // 
            // lblProdAutoRunDailyCC
            // 
            lblProdAutoRunDailyCC.Anchor = AnchorStyles.Right;
            lblProdAutoRunDailyCC.AutoSize = true;
            lblProdAutoRunDailyCC.Location = new Point(62, 127);
            lblProdAutoRunDailyCC.Margin = new Padding(4, 0, 4, 0);
            lblProdAutoRunDailyCC.Name = "lblProdAutoRunDailyCC";
            lblProdAutoRunDailyCC.Size = new Size(167, 15);
            lblProdAutoRunDailyCC.TabIndex = 2;
            lblProdAutoRunDailyCC.Text = "Production AutoRun Daily CC:";
            // 
            // txtProdAutoRunDailyCC
            // 
            txtProdAutoRunDailyCC.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            txtProdAutoRunDailyCC.Location = new Point(237, 123);
            txtProdAutoRunDailyCC.Margin = new Padding(4, 3, 4, 3);
            txtProdAutoRunDailyCC.Name = "txtProdAutoRunDailyCC";
            txtProdAutoRunDailyCC.Size = new Size(646, 23);
            txtProdAutoRunDailyCC.TabIndex = 2;
            toolTipInfo.SetToolTip(txtProdAutoRunDailyCC, "CC recipients for automated daily reports in Production mode.");
            // 
            // lblProdFemiTo
            // 
            lblProdFemiTo.Anchor = AnchorStyles.Right;
            lblProdFemiTo.AutoSize = true;
            lblProdFemiTo.Location = new Point(114, 178);
            lblProdFemiTo.Margin = new Padding(4, 0, 4, 0);
            lblProdFemiTo.Name = "lblProdFemiTo";
            lblProdFemiTo.Size = new Size(115, 15);
            lblProdFemiTo.TabIndex = 4;
            lblProdFemiTo.Text = "Production Femi TO:";
            // 
            // txtProdFemiTo
            // 
            txtProdFemiTo.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            txtProdFemiTo.Location = new Point(237, 174);
            txtProdFemiTo.Margin = new Padding(4, 3, 4, 3);
            txtProdFemiTo.Name = "txtProdFemiTo";
            txtProdFemiTo.Size = new Size(646, 23);
            txtProdFemiTo.TabIndex = 3;
            toolTipInfo.SetToolTip(txtProdFemiTo, "Recipients when 'Send to Femi Only' is checked in Production mode.");
            // 
            // lblProdFemiCC
            // 
            lblProdFemiCC.Anchor = AnchorStyles.Right;
            lblProdFemiCC.AutoSize = true;
            lblProdFemiCC.Location = new Point(112, 229);
            lblProdFemiCC.Margin = new Padding(4, 0, 4, 0);
            lblProdFemiCC.Name = "lblProdFemiCC";
            lblProdFemiCC.Size = new Size(117, 15);
            lblProdFemiCC.TabIndex = 6;
            lblProdFemiCC.Text = "Production Femi CC:";
            // 
            // txtProdFemiCC
            // 
            txtProdFemiCC.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            txtProdFemiCC.Location = new Point(237, 225);
            txtProdFemiCC.Margin = new Padding(4, 3, 4, 3);
            txtProdFemiCC.Name = "txtProdFemiCC";
            txtProdFemiCC.Size = new Size(646, 23);
            txtProdFemiCC.TabIndex = 4;
            toolTipInfo.SetToolTip(txtProdFemiCC, "CC recipients when 'Send to Femi Only' is checked in Production mode.");
            // 
            // lblProdTeamTo
            // 
            lblProdTeamTo.Anchor = AnchorStyles.Right;
            lblProdTeamTo.AutoSize = true;
            lblProdTeamTo.Location = new Point(112, 280);
            lblProdTeamTo.Margin = new Padding(4, 0, 4, 0);
            lblProdTeamTo.Name = "lblProdTeamTo";
            lblProdTeamTo.Size = new Size(117, 15);
            lblProdTeamTo.TabIndex = 8;
            lblProdTeamTo.Text = "Production Team TO:";
            // 
            // txtProdTeamTo
            // 
            txtProdTeamTo.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            txtProdTeamTo.Location = new Point(237, 276);
            txtProdTeamTo.Margin = new Padding(4, 3, 4, 3);
            txtProdTeamTo.Name = "txtProdTeamTo";
            txtProdTeamTo.Size = new Size(646, 23);
            txtProdTeamTo.TabIndex = 5;
            toolTipInfo.SetToolTip(txtProdTeamTo, "Recipients for general reports in Production mode (when not 'Femi Only').");
            // 
            // lblProdTeamCC
            // 
            lblProdTeamCC.Anchor = AnchorStyles.Right;
            lblProdTeamCC.AutoSize = true;
            lblProdTeamCC.Location = new Point(110, 331);
            lblProdTeamCC.Margin = new Padding(4, 0, 4, 0);
            lblProdTeamCC.Name = "lblProdTeamCC";
            lblProdTeamCC.Size = new Size(119, 15);
            lblProdTeamCC.TabIndex = 10;
            lblProdTeamCC.Text = "Production Team CC:";
            // 
            // txtProdTeamCC
            // 
            txtProdTeamCC.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            txtProdTeamCC.Location = new Point(237, 327);
            txtProdTeamCC.Margin = new Padding(4, 3, 4, 3);
            txtProdTeamCC.Name = "txtProdTeamCC";
            txtProdTeamCC.Size = new Size(646, 23);
            txtProdTeamCC.TabIndex = 6;
            toolTipInfo.SetToolTip(txtProdTeamCC, "CC recipients for general reports in Production mode (when not 'Femi Only').");
            // 
            // lblDebugTo
            // 
            lblDebugTo.Anchor = AnchorStyles.Right;
            lblDebugTo.AutoSize = true;
            lblDebugTo.Location = new Point(167, 382);
            lblDebugTo.Margin = new Padding(4, 0, 4, 0);
            lblDebugTo.Name = "lblDebugTo";
            lblDebugTo.Size = new Size(62, 15);
            lblDebugTo.TabIndex = 12;
            lblDebugTo.Text = "Debug TO:";
            // 
            // txtDebugTo
            // 
            txtDebugTo.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            txtDebugTo.Location = new Point(237, 378);
            txtDebugTo.Margin = new Padding(4, 3, 4, 3);
            txtDebugTo.Name = "txtDebugTo";
            txtDebugTo.Size = new Size(646, 23);
            txtDebugTo.TabIndex = 7;
            toolTipInfo.SetToolTip(txtDebugTo, "Primary recipient in Debug mode.");
            // 
            // lblDebugCC1
            // 
            lblDebugCC1.Anchor = AnchorStyles.Right;
            lblDebugCC1.AutoSize = true;
            lblDebugCC1.Location = new Point(156, 433);
            lblDebugCC1.Margin = new Padding(4, 0, 4, 0);
            lblDebugCC1.Name = "lblDebugCC1";
            lblDebugCC1.Size = new Size(73, 15);
            lblDebugCC1.TabIndex = 14;
            lblDebugCC1.Text = "Debug CC 1:";
            // 
            // txtDebugCC1
            // 
            txtDebugCC1.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            txtDebugCC1.Location = new Point(237, 429);
            txtDebugCC1.Margin = new Padding(4, 3, 4, 3);
            txtDebugCC1.Name = "txtDebugCC1";
            txtDebugCC1.Size = new Size(646, 23);
            txtDebugCC1.TabIndex = 8;
            toolTipInfo.SetToolTip(txtDebugCC1, "First CC recipient in Debug mode.");
            // 
            // lblDebugCC2
            // 
            lblDebugCC2.Anchor = AnchorStyles.Right;
            lblDebugCC2.AutoSize = true;
            lblDebugCC2.Location = new Point(156, 485);
            lblDebugCC2.Margin = new Padding(4, 0, 4, 0);
            lblDebugCC2.Name = "lblDebugCC2";
            lblDebugCC2.Size = new Size(73, 15);
            lblDebugCC2.TabIndex = 16;
            lblDebugCC2.Text = "Debug CC 2:";
            // 
            // txtDebugCC2
            // 
            txtDebugCC2.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            txtDebugCC2.Location = new Point(237, 481);
            txtDebugCC2.Margin = new Padding(4, 3, 4, 3);
            txtDebugCC2.Name = "txtDebugCC2";
            txtDebugCC2.Size = new Size(646, 23);
            txtDebugCC2.TabIndex = 9;
            toolTipInfo.SetToolTip(txtDebugCC2, "Second CC recipient in Debug mode (used when 'Femi Only' is checked).");
            // 
            // lblInstructions
            // 
            lblInstructions.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            lblInstructions.AutoSize = true;
            mainTableLayoutPanel.SetColumnSpan(lblInstructions, 2);
            lblInstructions.Location = new Point(4, 14);
            lblInstructions.Margin = new Padding(4, 0, 4, 0);
            lblInstructions.Name = "lblInstructions";
            lblInstructions.Size = new Size(879, 30);
            lblInstructions.TabIndex = 17;
            lblInstructions.Text = "Enter email addresses separated by commas (,) or semicolons (;).\r\nLeave a field blank to use the application default for that specific recipient list.";
            // 
            // buttonsFlowLayoutPanel
            // 
            buttonsFlowLayoutPanel.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            buttonsFlowLayoutPanel.Controls.Add(btnSave);
            buttonsFlowLayoutPanel.Controls.Add(btnRestoreDefaults);
            buttonsFlowLayoutPanel.Controls.Add(btnClose);
            buttonsFlowLayoutPanel.FlowDirection = FlowDirection.RightToLeft;
            buttonsFlowLayoutPanel.Location = new Point(14, 540);
            buttonsFlowLayoutPanel.Margin = new Padding(4, 3, 4, 3);
            buttonsFlowLayoutPanel.Name = "buttonsFlowLayoutPanel";
            buttonsFlowLayoutPanel.Size = new Size(887, 46);
            buttonsFlowLayoutPanel.TabIndex = 1;
            // 
            // btnSave
            // 
            btnSave.Location = new Point(743, 3);
            btnSave.Margin = new Padding(4, 3, 4, 3);
            btnSave.Name = "btnSave";
            btnSave.Size = new Size(140, 35);
            btnSave.TabIndex = 10;
            btnSave.Text = "&Save and Use";
            toolTipInfo.SetToolTip(btnSave, "Save the current email settings for future reports.");
            btnSave.UseVisualStyleBackColor = true;
            btnSave.Click += BtnSave_Click;
            // 
            // btnRestoreDefaults
            // 
            btnRestoreDefaults.Location = new Point(548, 3);
            btnRestoreDefaults.Margin = new Padding(4, 3, 4, 3);
            btnRestoreDefaults.Name = "btnRestoreDefaults";
            btnRestoreDefaults.Size = new Size(187, 35);
            btnRestoreDefaults.TabIndex = 11;
            btnRestoreDefaults.Text = "&Restore Application Defaults";
            toolTipInfo.SetToolTip(btnRestoreDefaults, "Revert all email settings to the application defaults.");
            btnRestoreDefaults.UseVisualStyleBackColor = true;
            btnRestoreDefaults.Click += BtnRestoreDefaults_Click;
            // 
            // btnClose
            // 
            btnClose.DialogResult = DialogResult.Cancel;
            btnClose.Location = new Point(452, 3);
            btnClose.Margin = new Padding(4, 3, 4, 3);
            btnClose.Name = "btnClose";
            btnClose.Size = new Size(88, 35);
            btnClose.TabIndex = 12;
            btnClose.Text = "&Close";
            toolTipInfo.SetToolTip(btnClose, "Close this window without saving changes.");
            btnClose.UseVisualStyleBackColor = true;
            btnClose.Click += BtnClose_Click;
            // 
            // ManageEmailRecipientsForm
            // 
            AcceptButton = btnSave;
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            CancelButton = btnClose;
            ClientSize = new Size(915, 601);
            Controls.Add(buttonsFlowLayoutPanel);
            Controls.Add(mainTableLayoutPanel);
            Margin = new Padding(4, 3, 4, 3);
            MaximizeBox = false;
            MinimizeBox = false;
            MinimumSize = new Size(697, 640);
            Name = "ManageEmailRecipientsForm";
            ShowIcon = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            Text = "Email Recipients Manager";
            Load += ManageEmailRecipientsForm_Load;
            mainTableLayoutPanel.ResumeLayout(false);
            mainTableLayoutPanel.PerformLayout();
            buttonsFlowLayoutPanel.ResumeLayout(false);
            ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel mainTableLayoutPanel;
        private System.Windows.Forms.Label lblProdAutoRunDailyTo;
        private System.Windows.Forms.TextBox txtProdAutoRunDailyTo;
        private System.Windows.Forms.Label lblProdAutoRunDailyCC;
        private System.Windows.Forms.TextBox txtProdAutoRunDailyCC;
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
        private System.Windows.Forms.ToolTip toolTipInfo;
        private System.Windows.Forms.Label lblInstructions;
    }
}
