namespace QuoteConversionReportAutomation.Forms
{
    #region Class Definition
    /// <summary>
    /// Contains the Windows Forms Designer generated code for the ManageTenderExclusionsForm.
    /// This form provides a user interface for managing the list of tender account posting codes to be excluded from analysis.
    /// </summary>
    partial class ManageTenderExclusionsForm
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
            this.grpExclusionList = new System.Windows.Forms.GroupBox();
            this.lblInstructions = new System.Windows.Forms.Label();
            this.btnRemove = new System.Windows.Forms.Button();
            this.btnAdd = new System.Windows.Forms.Button();
            this.txtPostingCode = new System.Windows.Forms.TextBox();
            this.lblPostingCode = new System.Windows.Forms.Label();
            this.lstExclusionCodes = new System.Windows.Forms.ListView();
            this.colPostingCode = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.btnClose = new System.Windows.Forms.Button();
            this.toolTipProvider = new System.Windows.Forms.ToolTip(this.components);
            this.grpExclusionList.SuspendLayout();
            this.SuspendLayout();
            // 
            // grpExclusionList
            // 
            this.grpExclusionList.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.grpExclusionList.Controls.Add(this.lblInstructions);
            this.grpExclusionList.Controls.Add(this.btnRemove);
            this.grpExclusionList.Controls.Add(this.btnAdd);
            this.grpExclusionList.Controls.Add(this.txtPostingCode);
            this.grpExclusionList.Controls.Add(this.lblPostingCode);
            this.grpExclusionList.Controls.Add(this.lstExclusionCodes);
            this.grpExclusionList.Location = new System.Drawing.Point(12, 12);
            this.grpExclusionList.Name = "grpExclusionList";
            this.grpExclusionList.Size = new System.Drawing.Size(360, 337);
            this.grpExclusionList.TabIndex = 0;
            this.grpExclusionList.TabStop = false;
            this.grpExclusionList.Text = "Tender Account Posting Code Exclusions";
            // 
            // lblInstructions
            // 
            this.lblInstructions.AutoSize = true;
            this.lblInstructions.Location = new System.Drawing.Point(7, 25);
            this.lblInstructions.Name = "lblInstructions";
            this.lblInstructions.Size = new System.Drawing.Size(325, 13);
            this.lblInstructions.TabIndex = 5;
            this.lblInstructions.Text = "Add or remove posting codes for tender accounts to exclude them.";
            // 
            // btnRemove
            // 
            this.btnRemove.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnRemove.Location = new System.Drawing.Point(244, 308);
            this.btnRemove.Name = "btnRemove";
            this.btnRemove.Size = new System.Drawing.Size(110, 23);
            this.btnRemove.TabIndex = 4;
            this.btnRemove.Text = "Remove Selected";
            this.toolTipProvider.SetToolTip(this.btnRemove, "Remove the selected posting code from the exclusion list.");
            this.btnRemove.UseVisualStyleBackColor = true;
            this.btnRemove.Click += new System.EventHandler(this.btnRemove_Click);
            // 
            // btnAdd
            // 
            this.btnAdd.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnAdd.Location = new System.Drawing.Point(279, 50);
            this.btnAdd.Name = "btnAdd";
            this.btnAdd.Size = new System.Drawing.Size(75, 23);
            this.btnAdd.TabIndex = 3;
            this.btnAdd.Text = "Add";
            this.toolTipProvider.SetToolTip(this.btnAdd, "Add the specified posting code to the exclusion list.");
            this.btnAdd.UseVisualStyleBackColor = true;
            this.btnAdd.Click += new System.EventHandler(this.btnAdd_Click);
            // 
            // txtPostingCode
            // 
            this.txtPostingCode.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtPostingCode.Location = new System.Drawing.Point(85, 52);
            this.txtPostingCode.Name = "txtPostingCode";
            this.txtPostingCode.Size = new System.Drawing.Size(188, 20);
            this.txtPostingCode.TabIndex = 2;
            this.toolTipProvider.SetToolTip(this.txtPostingCode, "Enter the posting code to add to the exclusion list.");
            // 
            // lblPostingCode
            // 
            this.lblPostingCode.AutoSize = true;
            this.lblPostingCode.Location = new System.Drawing.Point(7, 55);
            this.lblPostingCode.Name = "lblPostingCode";
            this.lblPostingCode.Size = new System.Drawing.Size(72, 13);
            this.lblPostingCode.TabIndex = 1;
            this.lblPostingCode.Text = "Posting Code:";
            // 
            // lstExclusionCodes
            // 
            this.lstExclusionCodes.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lstExclusionCodes.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.colPostingCode});
            this.lstExclusionCodes.FullRowSelect = true;
            this.lstExclusionCodes.HideSelection = false;
            this.lstExclusionCodes.Location = new System.Drawing.Point(10, 81);
            this.lstExclusionCodes.MultiSelect = false;
            this.lstExclusionCodes.Name = "lstExclusionCodes";
            this.lstExclusionCodes.Size = new System.Drawing.Size(344, 221);
            this.lstExclusionCodes.TabIndex = 0;
            this.lstExclusionCodes.UseCompatibleStateImageBehavior = false;
            this.lstExclusionCodes.View = System.Windows.Forms.View.Details;
            // 
            // colPostingCode
            // 
            this.colPostingCode.Text = "Excluded Posting Code";
            this.colPostingCode.Width = 320;
            // 
            // btnClose
            // 
            this.btnClose.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnClose.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnClose.Location = new System.Drawing.Point(297, 355);
            this.btnClose.Name = "btnClose";
            this.btnClose.Size = new System.Drawing.Size(75, 23);
            this.btnClose.TabIndex = 1;
            this.btnClose.Text = "Close";
            this.toolTipProvider.SetToolTip(this.btnClose, "Close this window. Changes are saved automatically when adding or removing codes" +
        ".");
            this.btnClose.UseVisualStyleBackColor = true;
            this.btnClose.Click += new System.EventHandler(this.btnClose_Click);
            // 
            // ManageTenderExclusionsForm
            // 
            this.AcceptButton = this.btnAdd;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.btnClose;
            this.ClientSize = new System.Drawing.Size(384, 386);
            this.Controls.Add(this.btnClose);
            this.Controls.Add(this.grpExclusionList);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.MinimumSize = new System.Drawing.Size(350, 400);
            this.Name = "ManageTenderExclusionsForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Manage Tender Exclusions";
            this.Load += new System.EventHandler(this.ManageTenderExclusionsForm_Load);
            this.grpExclusionList.ResumeLayout(false);
            this.grpExclusionList.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox grpExclusionList;
        private System.Windows.Forms.Button btnClose;
        private System.Windows.Forms.ListView lstExclusionCodes;
        private System.Windows.Forms.Button btnRemove;
        private System.Windows.Forms.Button btnAdd;
        private System.Windows.Forms.TextBox txtPostingCode;
        private System.Windows.Forms.Label lblPostingCode;
        private System.Windows.Forms.Label lblInstructions;
        private System.Windows.Forms.ToolTip toolTipProvider;
        private System.Windows.Forms.ColumnHeader colPostingCode;
    }
    #endregion
}
