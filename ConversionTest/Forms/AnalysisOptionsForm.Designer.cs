namespace QuoteConversionReportAutomation.Forms
{
    partial class AnalysisOptionsForm
    {
        private System.ComponentModel.IContainer components = null;
        protected override void Dispose(bool disposing) { if (disposing && (components != null)) { components.Dispose(); } base.Dispose(disposing); }
        private void InitializeComponent()
        {
            this.btnSelectFolder = new System.Windows.Forms.Button();
            this.txtFolderPath = new System.Windows.Forms.TextBox();
            this.lblFolder = new System.Windows.Forms.Label();
            this.btnSelectFile = new System.Windows.Forms.Button();
            this.txtFilePath = new System.Windows.Forms.TextBox();
            this.lblFile = new System.Windows.Forms.Label();
            this.btnOK = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // btnSelectFolder
            // 
            this.btnSelectFolder.Location = new System.Drawing.Point(417, 23);
            this.btnSelectFolder.Name = "btnSelectFolder";
            this.btnSelectFolder.Size = new System.Drawing.Size(75, 23);
            this.btnSelectFolder.TabIndex = 1;
            this.btnSelectFolder.Text = "Browse...";
            this.btnSelectFolder.UseVisualStyleBackColor = true;
            this.btnSelectFolder.Click += new System.EventHandler(this.btnSelectFolder_Click);
            // 
            // txtFolderPath
            // 
            this.txtFolderPath.Location = new System.Drawing.Point(145, 25);
            this.txtFolderPath.Name = "txtFolderPath";
            this.txtFolderPath.ReadOnly = true;
            this.txtFolderPath.Size = new System.Drawing.Size(266, 20);
            this.txtFolderPath.TabIndex = 0;
            // 
            // lblFolder
            // 
            this.lblFolder.AutoSize = true;
            this.lblFolder.Location = new System.Drawing.Point(12, 28);
            this.lblFolder.Name = "lblFolder";
            this.lblFolder.Size = new System.Drawing.Size(127, 13);
            this.lblFolder.TabIndex = 2;
            this.lblFolder.Text = "Folder to Analyse:";
            // 
            // btnSelectFile
            // 
            this.btnSelectFile.Location = new System.Drawing.Point(417, 52);
            this.btnSelectFile.Name = "btnSelectFile";
            this.btnSelectFile.Size = new System.Drawing.Size(75, 23);
            this.btnSelectFile.TabIndex = 3;
            this.btnSelectFile.Text = "Browse...";
            this.btnSelectFile.UseVisualStyleBackColor = true;
            this.btnSelectFile.Click += new System.EventHandler(this.btnSelectFile_Click);
            // 
            // txtFilePath
            // 
            this.txtFilePath.Location = new System.Drawing.Point(145, 54);
            this.txtFilePath.Name = "txtFilePath";
            this.txtFilePath.ReadOnly = true;
            this.txtFilePath.Size = new System.Drawing.Size(266, 20);
            this.txtFilePath.TabIndex = 2;
            // 
            // lblFile
            // 
            this.lblFile.AutoSize = true;
            this.lblFile.Location = new System.Drawing.Point(12, 57);
            this.lblFile.Name = "lblFile";
            this.lblFile.Size = new System.Drawing.Size(95, 13);
            this.lblFile.TabIndex = 5;
            this.lblFile.Text = "Example File Type:";
            // 
            // btnOK
            // 
            this.btnOK.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnOK.Location = new System.Drawing.Point(336, 95);
            this.btnOK.Name = "btnOK";
            this.btnOK.Size = new System.Drawing.Size(75, 23);
            this.btnOK.TabIndex = 4;
            this.btnOK.Text = "Run Analysis";
            this.btnOK.UseVisualStyleBackColor = true;
            this.btnOK.Click += new System.EventHandler(this.btnOK_Click);
            // 
            // btnCancel
            // 
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Location = new System.Drawing.Point(417, 95);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(75, 23);
            this.btnCancel.TabIndex = 5;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            // 
            // AnalysisOptionsForm
            // 
            this.AcceptButton = this.btnOK;
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(504, 131);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnOK);
            this.Controls.Add(this.lblFile);
            this.Controls.Add(this.txtFilePath);
            this.Controls.Add(this.btnSelectFile);
            this.Controls.Add(this.lblFolder);
            this.Controls.Add(this.txtFolderPath);
            this.Controls.Add(this.btnSelectFolder);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "AnalysisOptionsForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Retrospective Analysis Options";
            this.Load += new System.EventHandler(this.AnalysisOptionsForm_Load);
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private System.Windows.Forms.Button btnSelectFolder;
        private System.Windows.Forms.TextBox txtFolderPath;
        private System.Windows.Forms.Label lblFolder;
        private System.Windows.Forms.Button btnSelectFile;
        private System.Windows.Forms.TextBox txtFilePath;
        private System.Windows.Forms.Label lblFile;
        private System.Windows.Forms.Button btnOK;
        private System.Windows.Forms.Button btnCancel;
    }
}