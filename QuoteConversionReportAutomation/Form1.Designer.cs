namespace QuoteConversionReportAutomation
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            this.datepickFrom = new System.Windows.Forms.DateTimePicker();
            this.datepickTo = new System.Windows.Forms.DateTimePicker();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.button1 = new System.Windows.Forms.Button();
            this.button2 = new System.Windows.Forms.Button();
            this.btnViewReport = new System.Windows.Forms.Button();
            this.btnViewAnalysis = new System.Windows.Forms.Button();
            this.toolStripProgressBar1 = new System.Windows.Forms.ToolStripStatusLabel();
            this.statusStrip1 = new System.Windows.Forms.StatusStrip();
            this.checkBox1 = new System.Windows.Forms.CheckBox();
            this.typeDropBox = new System.Windows.Forms.ComboBox();
            this.label4 = new System.Windows.Forms.Label();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.label5 = new System.Windows.Forms.Label();
            this.finYearDropBox = new System.Windows.Forms.ComboBox();
            this.statusStrip1.SuspendLayout();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // datepickFrom
            // 
            this.datepickFrom.Location = new System.Drawing.Point(241, 191);
            this.datepickFrom.Name = "datepickFrom";
            this.datepickFrom.Size = new System.Drawing.Size(200, 20);
            this.datepickFrom.TabIndex = 0;
            // 
            // datepickTo
            // 
            this.datepickTo.Location = new System.Drawing.Point(241, 225);
            this.datepickTo.Name = "datepickTo";
            this.datepickTo.Size = new System.Drawing.Size(200, 20);
            this.datepickTo.TabIndex = 1;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(137, 197);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(87, 14);
            this.label1.TabIndex = 2;
            this.label1.Text = "Enter From Date:";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(137, 232);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(74, 14);
            this.label2.TabIndex = 3;
            this.label2.Text = "Enter To Date:";
            // 
            // label3
            // 
            this.label3.BackColor = System.Drawing.SystemColors.Control;
            this.label3.Font = new System.Drawing.Font("Arial", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label3.Location = new System.Drawing.Point(12, 10);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(611, 110);
            this.label3.TabIndex = 4;
            this.label3.Text = resources.GetString("label3.Text");
            this.label3.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // button1
            // 
            this.button1.Font = new System.Drawing.Font("Arial", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.button1.Location = new System.Drawing.Point(45, 330);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(134, 76);
            this.button1.TabIndex = 5;
            this.button1.Text = "Create Report";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.Button1_Click);
            // 
            // button2
            // 
            this.button2.Font = new System.Drawing.Font("Arial", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.button2.Location = new System.Drawing.Point(448, 330);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(134, 76);
            this.button2.TabIndex = 6;
            this.button2.Text = "Create Analysis &\r\nSend Email";
            this.button2.UseMnemonic = false;
            this.button2.UseVisualStyleBackColor = true;
            this.button2.Click += new System.EventHandler(this.Button2_Click);
            // 
            // btnViewReport
            // 
            this.btnViewReport.Font = new System.Drawing.Font("Arial", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnViewReport.Location = new System.Drawing.Point(76, 412);
            this.btnViewReport.Name = "btnViewReport";
            this.btnViewReport.Size = new System.Drawing.Size(75, 25);
            this.btnViewReport.TabIndex = 8;
            this.btnViewReport.Text = "View File";
            this.btnViewReport.UseVisualStyleBackColor = true;
            this.btnViewReport.Click += new System.EventHandler(this.btnViewReport_Click);
            // 
            // btnViewAnalysis
            // 
            this.btnViewAnalysis.Font = new System.Drawing.Font("Arial", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnViewAnalysis.Location = new System.Drawing.Point(478, 415);
            this.btnViewAnalysis.Name = "btnViewAnalysis";
            this.btnViewAnalysis.Size = new System.Drawing.Size(75, 25);
            this.btnViewAnalysis.TabIndex = 9;
            this.btnViewAnalysis.Text = "View File";
            this.btnViewAnalysis.UseVisualStyleBackColor = true;
            this.btnViewAnalysis.Click += new System.EventHandler(this.btnViewAnalysis_Click);
            // 
            // toolStripProgressBar1
            // 
            this.toolStripProgressBar1.Name = "toolStripProgressBar1";
            this.toolStripProgressBar1.Size = new System.Drawing.Size(0, 17);
            // 
            // statusStrip1
            // 
            this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripProgressBar1});
            this.statusStrip1.Location = new System.Drawing.Point(0, 472);
            this.statusStrip1.Name = "statusStrip1";
            this.statusStrip1.Size = new System.Drawing.Size(635, 22);
            this.statusStrip1.TabIndex = 10;
            this.statusStrip1.Text = "statusStrip1";
            // 
            // checkBox1
            // 
            this.checkBox1.AutoSize = true;
            this.checkBox1.Font = new System.Drawing.Font("Arial", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.checkBox1.Location = new System.Drawing.Point(241, 295);
            this.checkBox1.Name = "checkBox1";
            this.checkBox1.Size = new System.Drawing.Size(150, 20);
            this.checkBox1.TabIndex = 11;
            this.checkBox1.Text = "Send to only Femi?";
            this.checkBox1.UseVisualStyleBackColor = true;
            // 
            // typeDropBox
            // 
            this.typeDropBox.AutoCompleteCustomSource.AddRange(new string[] {
            "Weekly",
            "Monthly",
            "Quarterly (3 Months)",
            "Annual"});
            this.typeDropBox.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.SuggestAppend;
            this.typeDropBox.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.CustomSource;
            this.typeDropBox.FormattingEnabled = true;
            this.typeDropBox.Items.AddRange(new object[] {
            "Weekly",
            "Monthly",
            "Quarterly (3 Months)",
            "Annual"});
            this.typeDropBox.Location = new System.Drawing.Point(241, 157);
            this.typeDropBox.Name = "typeDropBox";
            this.typeDropBox.Size = new System.Drawing.Size(200, 22);
            this.typeDropBox.TabIndex = 12;
            this.typeDropBox.SelectedIndexChanged += new System.EventHandler(this.typeDropBox_SelectedIndexChanged);
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(137, 160);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(68, 14);
            this.label4.TabIndex = 13;
            this.label4.Text = "Report Type:";
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.label5);
            this.groupBox1.Controls.Add(this.finYearDropBox);
            this.groupBox1.Font = new System.Drawing.Font("Arial", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.groupBox1.Location = new System.Drawing.Point(122, 135);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(346, 189);
            this.groupBox1.TabIndex = 14;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Report Settings";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(15, 126);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(78, 14);
            this.label5.TabIndex = 16;
            this.label5.Text = "Financial Year:";
            // 
            // finYearDropBox
            // 
            this.finYearDropBox.AutoCompleteCustomSource.AddRange(new string[] {
            "Weekly",
            "Monthly",
            "Quarterly (3 Months)",
            "Annual"});
            this.finYearDropBox.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.SuggestAppend;
            this.finYearDropBox.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.CustomSource;
            this.finYearDropBox.FormattingEnabled = true;
            this.finYearDropBox.Location = new System.Drawing.Point(119, 123);
            this.finYearDropBox.Name = "finYearDropBox";
            this.finYearDropBox.Size = new System.Drawing.Size(200, 22);
            this.finYearDropBox.TabIndex = 15;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 14F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(635, 494);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.typeDropBox);
            this.Controls.Add(this.checkBox1);
            this.Controls.Add(this.statusStrip1);
            this.Controls.Add(this.btnViewAnalysis);
            this.Controls.Add(this.btnViewReport);
            this.Controls.Add(this.button2);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.datepickTo);
            this.Controls.Add(this.datepickFrom);
            this.Controls.Add(this.groupBox1);
            this.Font = new System.Drawing.Font("Arial", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Name = "Form1";
            this.Text = "Quote Conversion Automation";
            this.statusStrip1.ResumeLayout(false);
            this.statusStrip1.PerformLayout();
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.DateTimePicker datepickFrom;
        private System.Windows.Forms.DateTimePicker datepickTo;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.Button btnViewReport;
        private System.Windows.Forms.Button btnViewAnalysis;
        private System.Windows.Forms.ToolStripStatusLabel toolStripProgressBar1;
        private System.Windows.Forms.StatusStrip statusStrip1;
        private System.Windows.Forms.CheckBox checkBox1;
        private System.Windows.Forms.ComboBox typeDropBox;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.ComboBox finYearDropBox;
    }
}

