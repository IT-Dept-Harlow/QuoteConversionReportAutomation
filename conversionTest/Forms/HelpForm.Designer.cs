// HelpForm.Designer.cs
// Contains the Windows Forms Designer generated code for the HelpForm.

namespace QuoteConversionReportAutomation.Forms
{
    partial class HelpForm
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
            this.rtbHelpContent = new System.Windows.Forms.RichTextBox();
            this.btnClose = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // rtbHelpContent
            // 
            this.rtbHelpContent.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.rtbHelpContent.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.rtbHelpContent.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.rtbHelpContent.Location = new System.Drawing.Point(12, 12);
            this.rtbHelpContent.Name = "rtbHelpContent";
            this.rtbHelpContent.ReadOnly = true;
            this.rtbHelpContent.ScrollBars = System.Windows.Forms.RichTextBoxScrollBars.Vertical;
            this.rtbHelpContent.Size = new System.Drawing.Size(610, 400);
            this.rtbHelpContent.TabIndex = 0;
            this.rtbHelpContent.Text = "";
            this.rtbHelpContent.DetectUrls = true;
            this.rtbHelpContent.LinkClicked += new System.Windows.Forms.LinkClickedEventHandler(this.RtbHelpContent_LinkClicked);
            // 
            // btnClose
            // 
            this.btnClose.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnClose.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnClose.Location = new System.Drawing.Point(547, 426);
            this.btnClose.Name = "btnClose";
            this.btnClose.Size = new System.Drawing.Size(75, 23);
            this.btnClose.TabIndex = 1;
            this.btnClose.Text = "Close";
            this.btnClose.UseVisualStyleBackColor = true;
            this.btnClose.Click += new System.EventHandler(this.BtnClose_Click);
            // 
            // HelpForm
            // 
            this.AcceptButton = this.btnClose;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(634, 461);
            this.Controls.Add(this.btnClose);
            this.Controls.Add(this.rtbHelpContent);
            this.Name = "HelpForm";
            // this.Text is set in the constructor of HelpForm.cs
            this.Load += new System.EventHandler(this.HelpForm_Load);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.RichTextBox rtbHelpContent;
        private System.Windows.Forms.Button btnClose;
    }
}