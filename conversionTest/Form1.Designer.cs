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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            datepickFrom = new DateTimePicker();
            datepickTo = new DateTimePicker();
            label1 = new Label();
            label2 = new Label();
            label3 = new Label();
            button1 = new Button();
            button2 = new Button();
            btnViewReport = new Button();
            btnViewAnalysis = new Button();
            toolStripProgressBar1 = new ToolStripStatusLabel();
            statusStrip1 = new StatusStrip();
            checkBox1 = new CheckBox();
            typeDropBox = new ComboBox();
            label4 = new Label();
            groupBox1 = new GroupBox();
            label6 = new Label();
            label5 = new Label();
            finYearDropBox = new ComboBox();
            checkBox2DarkMode = new CheckBox();
            statusStrip1.SuspendLayout();
            groupBox1.SuspendLayout();
            SuspendLayout();
            // 
            // datepickFrom
            // 
            datepickFrom.Location = new Point(241, 191);
            datepickFrom.Name = "datepickFrom";
            datepickFrom.Size = new Size(200, 20);
            datepickFrom.TabIndex = 0;
            // 
            // datepickTo
            // 
            datepickTo.Location = new Point(241, 225);
            datepickTo.Name = "datepickTo";
            datepickTo.Size = new Size(200, 20);
            datepickTo.TabIndex = 1;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(137, 197);
            label1.Name = "label1";
            label1.Size = new Size(87, 14);
            label1.TabIndex = 2;
            label1.Text = "Enter From Date:";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(137, 232);
            label2.Name = "label2";
            label2.Size = new Size(74, 14);
            label2.TabIndex = 3;
            label2.Text = "Enter To Date:";
            // 
            // label3
            // 
            label3.BackColor = SystemColors.ControlLightLight;
            label3.Font = new Font("Arial", 12F, FontStyle.Bold, GraphicsUnit.Point, 0);
            label3.Location = new Point(12, 10);
            label3.Name = "label3";
            label3.Size = new Size(611, 110);
            label3.TabIndex = 4;
            label3.Text = resources.GetString("label3.Text");
            label3.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // button1
            // 
            button1.FlatAppearance.MouseDownBackColor = Color.FromArgb(128, 255, 128);
            button1.FlatAppearance.MouseOverBackColor = Color.Gray;
            button1.FlatStyle = FlatStyle.System;
            button1.Font = new Font("Arial", 8.25F, FontStyle.Bold, GraphicsUnit.Point, 0);
            button1.Location = new Point(45, 330);
            button1.Name = "button1";
            button1.Size = new Size(134, 76);
            button1.TabIndex = 5;
            button1.Text = "Create Report";
            button1.UseVisualStyleBackColor = true;
            button1.Click += Button1_Click;
            // 
            // button2
            // 
            button2.FlatAppearance.MouseDownBackColor = Color.FromArgb(128, 255, 128);
            button2.FlatAppearance.MouseOverBackColor = Color.Gray;
            button2.FlatStyle = FlatStyle.System;
            button2.Font = new Font("Arial", 8.25F, FontStyle.Bold, GraphicsUnit.Point, 0);
            button2.Location = new Point(448, 330);
            button2.Name = "button2";
            button2.Size = new Size(134, 76);
            button2.TabIndex = 6;
            button2.Text = "Create Analysis &\r\nSend Email";
            button2.UseMnemonic = false;
            button2.UseVisualStyleBackColor = true;
            button2.Click += Button2_Click;
            // 
            // btnViewReport
            // 
            btnViewReport.FlatAppearance.MouseDownBackColor = Color.FromArgb(128, 255, 128);
            btnViewReport.FlatAppearance.MouseOverBackColor = Color.Gray;
            btnViewReport.FlatStyle = FlatStyle.System;
            btnViewReport.Font = new Font("Arial", 8.25F, FontStyle.Bold, GraphicsUnit.Point, 0);
            btnViewReport.Location = new Point(76, 412);
            btnViewReport.Name = "btnViewReport";
            btnViewReport.Size = new Size(75, 25);
            btnViewReport.TabIndex = 8;
            btnViewReport.Text = "View File";
            btnViewReport.UseVisualStyleBackColor = true;
            btnViewReport.Click += btnViewReport_Click;
            // 
            // btnViewAnalysis
            // 
            btnViewAnalysis.FlatAppearance.MouseDownBackColor = Color.FromArgb(128, 255, 128);
            btnViewAnalysis.FlatAppearance.MouseOverBackColor = Color.Gray;
            btnViewAnalysis.FlatStyle = FlatStyle.System;
            btnViewAnalysis.Font = new Font("Arial", 8.25F, FontStyle.Bold, GraphicsUnit.Point, 0);
            btnViewAnalysis.Location = new Point(478, 415);
            btnViewAnalysis.Name = "btnViewAnalysis";
            btnViewAnalysis.Size = new Size(75, 25);
            btnViewAnalysis.TabIndex = 9;
            btnViewAnalysis.Text = "View File";
            btnViewAnalysis.UseVisualStyleBackColor = true;
            btnViewAnalysis.Click += btnViewAnalysis_Click;
            // 
            // toolStripProgressBar1
            // 
            toolStripProgressBar1.Name = "toolStripProgressBar1";
            toolStripProgressBar1.Size = new Size(0, 17);
            // 
            // statusStrip1
            // 
            statusStrip1.Items.AddRange(new ToolStripItem[] { toolStripProgressBar1 });
            statusStrip1.Location = new Point(0, 472);
            statusStrip1.Name = "statusStrip1";
            statusStrip1.Size = new Size(635, 22);
            statusStrip1.TabIndex = 10;
            statusStrip1.Text = "statusStrip1";
            // 
            // checkBox1
            // 
            checkBox1.AutoSize = true;
            checkBox1.FlatStyle = FlatStyle.Flat;
            checkBox1.Font = new Font("Arial", 9.75F, FontStyle.Bold, GraphicsUnit.Point, 0);
            checkBox1.Location = new Point(119, 158);
            checkBox1.Name = "checkBox1";
            checkBox1.Size = new Size(147, 20);
            checkBox1.TabIndex = 11;
            checkBox1.Text = "Send to only Femi?";
            checkBox1.UseVisualStyleBackColor = true;
            // 
            // typeDropBox
            // 
            typeDropBox.AutoCompleteCustomSource.AddRange(new string[] { "Weekly", "Monthly", "Quarterly (3 Months)", "Annual" });
            typeDropBox.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
            typeDropBox.AutoCompleteSource = AutoCompleteSource.CustomSource;
            typeDropBox.FormattingEnabled = true;
            typeDropBox.Items.AddRange(new object[] { "Daily", "Weekly", "Monthly", "Quarterly (3 Months)", "Annual" });
            typeDropBox.Location = new Point(241, 157);
            typeDropBox.Name = "typeDropBox";
            typeDropBox.Size = new Size(200, 22);
            typeDropBox.TabIndex = 12;
            typeDropBox.SelectedIndexChanged += typeDropBox_SelectedIndexChanged;
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new Point(137, 160);
            label4.Name = "label4";
            label4.Size = new Size(68, 14);
            label4.TabIndex = 13;
            label4.Text = "Report Type:";
            // 
            // groupBox1
            // 
            groupBox1.Controls.Add(label6);
            groupBox1.Controls.Add(label5);
            groupBox1.Controls.Add(checkBox1);
            groupBox1.Controls.Add(finYearDropBox);
            groupBox1.Font = new Font("Arial", 8.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
            groupBox1.Location = new Point(122, 135);
            groupBox1.Name = "groupBox1";
            groupBox1.Size = new Size(346, 189);
            groupBox1.TabIndex = 14;
            groupBox1.TabStop = false;
            groupBox1.Text = "Report Settings";
            // 
            // label6
            // 
            label6.AutoSize = true;
            label6.Font = new Font("Arial", 9.75F, FontStyle.Bold);
            label6.Location = new Point(119, 158);
            label6.Name = "label6";
            label6.Size = new Size(0, 16);
            label6.TabIndex = 17;
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Location = new Point(15, 126);
            label5.Name = "label5";
            label5.Size = new Size(78, 14);
            label5.TabIndex = 16;
            label5.Text = "Financial Year:";
            // 
            // finYearDropBox
            // 
            finYearDropBox.AutoCompleteCustomSource.AddRange(new string[] { "Weekly", "Monthly", "Quarterly (3 Months)", "Annual" });
            finYearDropBox.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
            finYearDropBox.AutoCompleteSource = AutoCompleteSource.CustomSource;
            finYearDropBox.FormattingEnabled = true;
            finYearDropBox.Location = new Point(119, 123);
            finYearDropBox.Name = "finYearDropBox";
            finYearDropBox.Size = new Size(200, 22);
            finYearDropBox.TabIndex = 15;
            // 
            // checkBox2DarkMode
            // 
            checkBox2DarkMode.AutoSize = true;
            checkBox2DarkMode.Location = new Point(0, 0);
            checkBox2DarkMode.Name = "checkBox2DarkMode";
            checkBox2DarkMode.Size = new Size(77, 18);
            checkBox2DarkMode.TabIndex = 15;
            checkBox2DarkMode.Text = "Dark Mode";
            checkBox2DarkMode.UseVisualStyleBackColor = true;
            checkBox2DarkMode.CheckedChanged += checkBox2DarkMode_CheckedChanged;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(6F, 14F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = SystemColors.ControlLightLight;
            ClientSize = new Size(635, 494);
            Controls.Add(checkBox2DarkMode);
            Controls.Add(label4);
            Controls.Add(typeDropBox);
            Controls.Add(statusStrip1);
            Controls.Add(btnViewAnalysis);
            Controls.Add(btnViewReport);
            Controls.Add(button2);
            Controls.Add(button1);
            Controls.Add(label3);
            Controls.Add(label2);
            Controls.Add(label1);
            Controls.Add(datepickTo);
            Controls.Add(datepickFrom);
            Controls.Add(groupBox1);
            Font = new Font("Arial", 8.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            Icon = (Icon)resources.GetObject("$this.Icon");
            Name = "Form1";
            Text = "Quote Conversion Automation";
            Load += Form1_Load;
            statusStrip1.ResumeLayout(false);
            statusStrip1.PerformLayout();
            groupBox1.ResumeLayout(false);
            groupBox1.PerformLayout();
            ResumeLayout(false);
            PerformLayout();

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
        private Label label6;
        private CheckBox checkBox2DarkMode;
    }
}

