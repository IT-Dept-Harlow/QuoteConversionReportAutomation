namespace QuoteConversionReportAutomation.Forms
{
    partial class ManageGreetingsForm
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
            this.lblAutoRunDaily = new System.Windows.Forms.Label();
            this.txtAutoRunDaily = new System.Windows.Forms.TextBox();
            this.lblManualStdDaily = new System.Windows.Forms.Label();
            this.txtManualStdDaily = new System.Windows.Forms.TextBox();
            this.lblAutoRunDaily5Day1k = new System.Windows.Forms.Label();
            this.txtAutoRunDaily5Day1k = new System.Windows.Forms.TextBox();
            this.lblManualFemi = new System.Windows.Forms.Label();
            this.txtManualFemi = new System.Windows.Forms.TextBox();
            this.lblManualTeam = new System.Windows.Forms.Label();
            this.txtManualTeam = new System.Windows.Forms.TextBox();
            this.lblDebugDefault = new System.Windows.Forms.Label();
            this.txtDebugDefault = new System.Windows.Forms.TextBox();
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
            this.mainTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 220F));
            this.mainTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.mainTableLayoutPanel.Controls.Add(this.lblInstructions, 0, 0);
            this.mainTableLayoutPanel.Controls.Add(this.lblAutoRunDaily, 0, 1);
            this.mainTableLayoutPanel.Controls.Add(this.txtAutoRunDaily, 1, 1);
            this.mainTableLayoutPanel.Controls.Add(this.lblManualStdDaily, 0, 2);
            this.mainTableLayoutPanel.Controls.Add(this.txtManualStdDaily, 1, 2);
            this.mainTableLayoutPanel.Controls.Add(this.lblAutoRunDaily5Day1k, 0, 3);
            this.mainTableLayoutPanel.Controls.Add(this.txtAutoRunDaily5Day1k, 1, 3);
            this.mainTableLayoutPanel.Controls.Add(this.lblManualFemi, 0, 4);
            this.mainTableLayoutPanel.Controls.Add(this.txtManualFemi, 1, 4);
            this.mainTableLayoutPanel.Controls.Add(this.lblManualTeam, 0, 5);
            this.mainTableLayoutPanel.Controls.Add(this.txtManualTeam, 1, 5);
            this.mainTableLayoutPanel.Controls.Add(this.lblDebugDefault, 0, 6);
            this.mainTableLayoutPanel.Controls.Add(this.txtDebugDefault, 1, 6);
            this.mainTableLayoutPanel.Location = new System.Drawing.Point(12, 12);
            this.mainTableLayoutPanel.Name = "mainTableLayoutPanel";
            this.mainTableLayoutPanel.RowCount = 7; // Instructions + 6 greeting types
            this.mainTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 40F)); // Instructions
            this.mainTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 16.66667F));
            this.mainTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 16.66667F));
            this.mainTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 16.66667F));
            this.mainTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 16.66667F));
            this.mainTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 16.66667F));
            this.mainTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 16.66667F));
            this.mainTableLayoutPanel.Size = new System.Drawing.Size(560, 280); // Adjusted height
            this.mainTableLayoutPanel.TabIndex = 0;
            // 
            // lblInstructions
            // 
            this.lblInstructions.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.lblInstructions.AutoSize = true;
            this.mainTableLayoutPanel.SetColumnSpan(this.lblInstructions, 2);
            this.lblInstructions.Location = new System.Drawing.Point(3, 13);
            this.lblInstructions.Name = "lblInstructions";
            this.lblInstructions.Size = new System.Drawing.Size(554, 13);
            this.lblInstructions.TabIndex = 0;
            this.lblInstructions.Text = "Enter the desired greetings. Leave blank to use the application default from appsettings.json.";
            // 
            // lblAutoRunDaily
            // 
            this.lblAutoRunDaily.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.lblAutoRunDaily.AutoSize = true;
            this.lblAutoRunDaily.Location = new System.Drawing.Point(50, 53); // Adjusted for clarity
            this.lblAutoRunDaily.Name = "lblAutoRunDaily";
            this.lblAutoRunDaily.Size = new System.Drawing.Size(167, 13);
            this.lblAutoRunDaily.TabIndex = 1;
            this.lblAutoRunDaily.Text = "Automated Standard Daily Greeting:";
            // 
            // txtAutoRunDaily
            // 
            this.txtAutoRunDaily.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.txtAutoRunDaily.Location = new System.Drawing.Point(223, 50);
            this.txtAutoRunDaily.Name = "txtAutoRunDaily";
            this.txtAutoRunDaily.Size = new System.Drawing.Size(334, 20);
            this.txtAutoRunDaily.TabIndex = 1;
            // 
            // lblManualStdDaily
            // 
            this.lblManualStdDaily.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.lblManualStdDaily.AutoSize = true;
            this.lblManualStdDaily.Location = new System.Drawing.Point(62, 93);
            this.lblManualStdDaily.Name = "lblManualStdDaily";
            this.lblManualStdDaily.Size = new System.Drawing.Size(155, 13);
            this.lblManualStdDaily.TabIndex = 3;
            this.lblManualStdDaily.Text = "Manual Standard Daily Greeting:";
            // 
            // txtManualStdDaily
            // 
            this.txtManualStdDaily.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.txtManualStdDaily.Location = new System.Drawing.Point(223, 90);
            this.txtManualStdDaily.Name = "txtManualStdDaily";
            this.txtManualStdDaily.Size = new System.Drawing.Size(334, 20);
            this.txtManualStdDaily.TabIndex = 2;
            // 
            // lblAutoRunDaily5Day1k
            // 
            this.lblAutoRunDaily5Day1k.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.lblAutoRunDaily5Day1k.AutoSize = true;
            this.lblAutoRunDaily5Day1k.Location = new System.Drawing.Point(8, 133);
            this.lblAutoRunDaily5Day1k.Name = "lblAutoRunDaily5Day1k";
            this.lblAutoRunDaily5Day1k.Size = new System.Drawing.Size(209, 13);
            this.lblAutoRunDaily5Day1k.TabIndex = 5;
            this.lblAutoRunDaily5Day1k.Text = "Automated Daily (5d >= £1k) Greeting:";
            // 
            // txtAutoRunDaily5Day1k
            // 
            this.txtAutoRunDaily5Day1k.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.txtAutoRunDaily5Day1k.Location = new System.Drawing.Point(223, 130);
            this.txtAutoRunDaily5Day1k.Name = "txtAutoRunDaily5Day1k";
            this.txtAutoRunDaily5Day1k.Size = new System.Drawing.Size(334, 20);
            this.txtAutoRunDaily5Day1k.TabIndex = 3;
            // 
            // lblManualFemi
            // 
            this.lblManualFemi.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.lblManualFemi.AutoSize = true;
            this.lblManualFemi.Location = new System.Drawing.Point(32, 173);
            this.lblManualFemi.Name = "lblManualFemi";
            this.lblManualFemi.Size = new System.Drawing.Size(185, 13);
            this.lblManualFemi.TabIndex = 7;
            this.lblManualFemi.Text = "Manual Non-Daily \'Femi Only\' Greeting:";
            // 
            // txtManualFemi
            // 
            this.txtManualFemi.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.txtManualFemi.Location = new System.Drawing.Point(223, 170);
            this.txtManualFemi.Name = "txtManualFemi";
            this.txtManualFemi.Size = new System.Drawing.Size(334, 20);
            this.txtManualFemi.TabIndex = 4;
            // 
            // lblManualTeam
            // 
            this.lblManualTeam.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.lblManualTeam.AutoSize = true;
            this.lblManualTeam.Location = new System.Drawing.Point(40, 213);
            this.lblManualTeam.Name = "lblManualTeam";
            this.lblManualTeam.Size = new System.Drawing.Size(177, 13);
            this.lblManualTeam.TabIndex = 9;
            this.lblManualTeam.Text = "Manual Non-Daily Team Greeting:";
            // 
            // txtManualTeam
            // 
            this.txtManualTeam.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.txtManualTeam.Location = new System.Drawing.Point(223, 210);
            this.txtManualTeam.Name = "txtManualTeam";
            this.txtManualTeam.Size = new System.Drawing.Size(334, 20);
            this.txtManualTeam.TabIndex = 5;
            // 
            // lblDebugDefault
            // 
            this.lblDebugDefault.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.lblDebugDefault.AutoSize = true;
            this.lblDebugDefault.Location = new System.Drawing.Point(108, 253);
            this.lblDebugDefault.Name = "lblDebugDefault";
            this.lblDebugDefault.Size = new System.Drawing.Size(109, 13);
            this.lblDebugDefault.TabIndex = 11;
            this.lblDebugDefault.Text = "Debug Default Greeting:";
            // 
            // txtDebugDefault
            // 
            this.txtDebugDefault.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.txtDebugDefault.Location = new System.Drawing.Point(223, 250);
            this.txtDebugDefault.Name = "txtDebugDefault";
            this.txtDebugDefault.Size = new System.Drawing.Size(334, 20);
            this.txtDebugDefault.TabIndex = 6;
            // 
            // buttonsFlowLayoutPanel
            // 
            this.buttonsFlowLayoutPanel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonsFlowLayoutPanel.Controls.Add(this.btnSave);
            this.buttonsFlowLayoutPanel.Controls.Add(this.btnRestoreDefaults);
            this.buttonsFlowLayoutPanel.Controls.Add(this.btnClose);
            this.buttonsFlowLayoutPanel.FlowDirection = System.Windows.Forms.FlowDirection.RightToLeft;
            this.buttonsFlowLayoutPanel.Location = new System.Drawing.Point(12, 298); // Adjusted Y
            this.buttonsFlowLayoutPanel.Name = "buttonsFlowLayoutPanel";
            this.buttonsFlowLayoutPanel.Size = new System.Drawing.Size(560, 35);
            this.buttonsFlowLayoutPanel.TabIndex = 1;
            // 
            // btnSave
            // 
            this.btnSave.Location = new System.Drawing.Point(482, 3); // Adjusted for FlowDirection
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(75, 28);
            this.btnSave.TabIndex = 7;
            this.btnSave.Text = "&Save";
            this.btnSave.UseVisualStyleBackColor = true;
            this.btnSave.Click += new System.EventHandler(this.BtnSave_Click);
            // 
            // btnRestoreDefaults
            // 
            this.btnRestoreDefaults.Location = new System.Drawing.Point(351, 3); // Adjusted for FlowDirection
            this.btnRestoreDefaults.Name = "btnRestoreDefaults";
            this.btnRestoreDefaults.Size = new System.Drawing.Size(125, 28);
            this.btnRestoreDefaults.TabIndex = 8;
            this.btnRestoreDefaults.Text = "&Restore App Defaults";
            this.btnRestoreDefaults.UseVisualStyleBackColor = true;
            this.btnRestoreDefaults.Click += new System.EventHandler(this.BtnRestoreDefaults_Click);
            // 
            // btnClose
            // 
            this.btnClose.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnClose.Location = new System.Drawing.Point(270, 3); // Adjusted for FlowDirection
            this.btnClose.Name = "btnClose";
            this.btnClose.Size = new System.Drawing.Size(75, 28);
            this.btnClose.TabIndex = 9;
            this.btnClose.Text = "&Close";
            this.btnClose.UseVisualStyleBackColor = true;
            this.btnClose.Click += new System.EventHandler(this.BtnClose_Click);
            // 
            // ManageGreetingsForm
            // 
            this.AcceptButton = this.btnSave;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.btnClose;
            this.ClientSize = new System.Drawing.Size(584, 341); // Adjusted height
            this.Controls.Add(this.buttonsFlowLayoutPanel);
            this.Controls.Add(this.mainTableLayoutPanel);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.MinimumSize = new System.Drawing.Size(600, 380); // Adjusted min height
            this.Name = "ManageGreetingsForm";
            this.Text = "Manage Email Greetings";
            this.Load += new System.EventHandler(this.ManageGreetingsForm_Load);
            this.mainTableLayoutPanel.ResumeLayout(false);
            this.mainTableLayoutPanel.PerformLayout();
            this.buttonsFlowLayoutPanel.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel mainTableLayoutPanel;
        private System.Windows.Forms.Label lblInstructions;
        private System.Windows.Forms.Label lblAutoRunDaily;
        private System.Windows.Forms.TextBox txtAutoRunDaily;
        private System.Windows.Forms.Label lblManualStdDaily;
        private System.Windows.Forms.TextBox txtManualStdDaily;
        private System.Windows.Forms.Label lblAutoRunDaily5Day1k;
        private System.Windows.Forms.TextBox txtAutoRunDaily5Day1k;
        private System.Windows.Forms.Label lblManualFemi;
        private System.Windows.Forms.TextBox txtManualFemi;
        private System.Windows.Forms.Label lblManualTeam;
        private System.Windows.Forms.TextBox txtManualTeam;
        private System.Windows.Forms.Label lblDebugDefault;
        private System.Windows.Forms.TextBox txtDebugDefault;
        private System.Windows.Forms.FlowLayoutPanel buttonsFlowLayoutPanel;
        private System.Windows.Forms.Button btnSave;
        private System.Windows.Forms.Button btnRestoreDefaults;
        private System.Windows.Forms.Button btnClose;
        private System.Windows.Forms.ToolTip toolTipProvider;
    }
}
