// ManageAutoReportDefinitionsForm.Designer.cs
namespace QuoteConversionReportAutomation.Forms
{
    partial class ManageAutoReportDefinitionsForm
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
            this.dgvReportDefinitions = new System.Windows.Forms.DataGridView();
            this.grpDefinitionDetails = new System.Windows.Forms.GroupBox();
            this.chkIncludeLeadTimeAnalysis = new System.Windows.Forms.CheckBox();
            this.lblReportId = new System.Windows.Forms.Label();
            this.chkAppendToPowerBi = new System.Windows.Forms.CheckBox();
            this.chkRequiresNetValueFiltering = new System.Windows.Forms.CheckBox();
            this.lblReportDurationDays = new System.Windows.Forms.Label();
            this.numReportDurationDays = new System.Windows.Forms.NumericUpDown();
            this.lblReportEndDateOffsetDays = new System.Windows.Forms.Label();
            this.numReportEndDateOffsetDays = new System.Windows.Forms.NumericUpDown();
            this.txtTemplateName = new System.Windows.Forms.TextBox();
            this.lblTemplateName = new System.Windows.Forms.Label();
            this.txtSubjectPrefix = new System.Windows.Forms.TextBox();
            this.lblSubjectPrefix = new System.Windows.Forms.Label();
            this.cmbRecipientCategoryKey = new System.Windows.Forms.ComboBox();
            this.lblRecipientCategoryKey = new System.Windows.Forms.Label();
            this.cmbGreetingKey = new System.Windows.Forms.ComboBox();
            this.lblGreetingKey = new System.Windows.Forms.Label();
            this.txtSuccessFlagJsonName = new System.Windows.Forms.TextBox();
            this.lblSuccessFlagJsonName = new System.Windows.Forms.Label();
            this.cmbRunOnDayOfWeek = new System.Windows.Forms.ComboBox();
            this.lblRunOnDayOfWeek = new System.Windows.Forms.Label();
            this.cmbReportTypeIndex = new System.Windows.Forms.ComboBox();
            this.lblReportTypeIndex = new System.Windows.Forms.Label();
            this.chkIsEnabled = new System.Windows.Forms.CheckBox();
            this.txtReportName = new System.Windows.Forms.TextBox();
            this.lblReportName = new System.Windows.Forms.Label();
            this.btnAdd = new System.Windows.Forms.Button();
            this.btnUpdate = new System.Windows.Forms.Button();
            this.btnDelete = new System.Windows.Forms.Button();
            this.btnDuplicate = new System.Windows.Forms.Button();
            this.btnSaveChanges = new System.Windows.Forms.Button();
            this.btnClose = new System.Windows.Forms.Button();
            this.flowLayoutPanelButtons = new System.Windows.Forms.FlowLayoutPanel();
            this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
            ((System.ComponentModel.ISupportInitialize)(this.dgvReportDefinitions)).BeginInit();
            this.grpDefinitionDetails.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numReportDurationDays)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numReportEndDateOffsetDays)).BeginInit();
            this.flowLayoutPanelButtons.SuspendLayout();
            this.SuspendLayout();
            // 
            // dgvReportDefinitions
            // 
            this.dgvReportDefinitions.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.dgvReportDefinitions.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvReportDefinitions.Location = new System.Drawing.Point(12, 12);
            this.dgvReportDefinitions.Name = "dgvReportDefinitions";
            this.dgvReportDefinitions.Size = new System.Drawing.Size(776, 200);
            this.dgvReportDefinitions.TabIndex = 0;
            this.toolTip1.SetToolTip(this.dgvReportDefinitions, "List of all configured automated reports. Click a row to edit. Check/uncheck \'En" +
        "abled\' to activate/deactivate a report.");
            // 
            // grpDefinitionDetails
            // 
            this.grpDefinitionDetails.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.grpDefinitionDetails.Controls.Add(this.chkIncludeLeadTimeAnalysis);
            this.grpDefinitionDetails.Controls.Add(this.lblReportId);
            this.grpDefinitionDetails.Controls.Add(this.chkAppendToPowerBi);
            this.grpDefinitionDetails.Controls.Add(this.chkRequiresNetValueFiltering);
            this.grpDefinitionDetails.Controls.Add(this.lblReportDurationDays);
            this.grpDefinitionDetails.Controls.Add(this.numReportDurationDays);
            this.grpDefinitionDetails.Controls.Add(this.lblReportEndDateOffsetDays);
            this.grpDefinitionDetails.Controls.Add(this.numReportEndDateOffsetDays);
            this.grpDefinitionDetails.Controls.Add(this.txtTemplateName);
            this.grpDefinitionDetails.Controls.Add(this.lblTemplateName);
            this.grpDefinitionDetails.Controls.Add(this.txtSubjectPrefix);
            this.grpDefinitionDetails.Controls.Add(this.lblSubjectPrefix);
            this.grpDefinitionDetails.Controls.Add(this.cmbRecipientCategoryKey);
            this.grpDefinitionDetails.Controls.Add(this.lblRecipientCategoryKey);
            this.grpDefinitionDetails.Controls.Add(this.cmbGreetingKey);
            this.grpDefinitionDetails.Controls.Add(this.lblGreetingKey);
            this.grpDefinitionDetails.Controls.Add(this.txtSuccessFlagJsonName);
            this.grpDefinitionDetails.Controls.Add(this.lblSuccessFlagJsonName);
            this.grpDefinitionDetails.Controls.Add(this.cmbRunOnDayOfWeek);
            this.grpDefinitionDetails.Controls.Add(this.lblRunOnDayOfWeek);
            this.grpDefinitionDetails.Controls.Add(this.cmbReportTypeIndex);
            this.grpDefinitionDetails.Controls.Add(this.lblReportTypeIndex);
            this.grpDefinitionDetails.Controls.Add(this.chkIsEnabled);
            this.grpDefinitionDetails.Controls.Add(this.txtReportName);
            this.grpDefinitionDetails.Controls.Add(this.lblReportName);
            this.grpDefinitionDetails.Location = new System.Drawing.Point(12, 218);
            this.grpDefinitionDetails.Name = "grpDefinitionDetails";
            this.grpDefinitionDetails.Size = new System.Drawing.Size(776, 250);
            this.grpDefinitionDetails.TabIndex = 1;
            this.grpDefinitionDetails.TabStop = false;
            this.grpDefinitionDetails.Text = "Definition Details";
            // 
            // chkIncludeLeadTimeAnalysis
            // 
            this.chkIncludeLeadTimeAnalysis.AutoSize = true;
            this.chkIncludeLeadTimeAnalysis.Location = new System.Drawing.Point(400, 182);
            this.chkIncludeLeadTimeAnalysis.Name = "chkIncludeLeadTimeAnalysis";
            this.chkIncludeLeadTimeAnalysis.Size = new System.Drawing.Size(168, 17);
            this.chkIncludeLeadTimeAnalysis.TabIndex = 12;
            this.chkIncludeLeadTimeAnalysis.Text = "Include Lead Time Analysis";
            this.toolTip1.SetToolTip(this.chkIncludeLeadTimeAnalysis, "If checked, the \'Lead Time Analysis\' sheet will be added to this automated repor" +
        "t.");
            this.chkIncludeLeadTimeAnalysis.UseVisualStyleBackColor = true;
            // 
            // lblReportId
            // 
            this.lblReportId.AutoSize = true;
            this.lblReportId.Location = new System.Drawing.Point(500, 15);
            this.lblReportId.Name = "lblReportId";
            this.lblReportId.Size = new System.Drawing.Size(0, 13);
            this.lblReportId.TabIndex = 26;
            this.lblReportId.Visible = false;
            // 
            // chkAppendToPowerBi
            // 
            this.chkAppendToPowerBi.AutoSize = true;
            this.chkAppendToPowerBi.Location = new System.Drawing.Point(400, 155);
            this.chkAppendToPowerBi.Name = "chkAppendToPowerBi";
            this.chkAppendToPowerBi.Size = new System.Drawing.Size(120, 17);
            this.chkAppendToPowerBi.TabIndex = 11;
            this.chkAppendToPowerBi.Text = "Append to Power BI";
            this.toolTip1.SetToolTip(this.chkAppendToPowerBi, "Check if this report\'s data should be appended to a central Power BI source file." +
        "");
            this.chkAppendToPowerBi.UseVisualStyleBackColor = true;
            // 
            // chkRequiresNetValueFiltering
            // 
            this.chkRequiresNetValueFiltering.AutoSize = true;
            this.chkRequiresNetValueFiltering.Location = new System.Drawing.Point(400, 128);
            this.chkRequiresNetValueFiltering.Name = "chkRequiresNetValueFiltering";
            this.chkRequiresNetValueFiltering.Size = new System.Drawing.Size(160, 17);
            this.chkRequiresNetValueFiltering.TabIndex = 10;
            this.chkRequiresNetValueFiltering.Text = "Requires Net Value Filtering";
            this.toolTip1.SetToolTip(this.chkRequiresNetValueFiltering, "Check if this report requires filtering for Net Value >= £1000 (e.g., Daily 5d1k" +
        " report).");
            this.chkRequiresNetValueFiltering.UseVisualStyleBackColor = true;
            // 
            // lblReportDurationDays
            // 
            this.lblReportDurationDays.AutoSize = true;
            this.lblReportDurationDays.Location = new System.Drawing.Point(397, 101);
            this.lblReportDurationDays.Name = "lblReportDurationDays";
            this.lblReportDurationDays.Size = new System.Drawing.Size(108, 13);
            this.lblReportDurationDays.TabIndex = 23;
            this.lblReportDurationDays.Text = "Duration (Work Days):";
            // 
            // numReportDurationDays
            // 
            this.numReportDurationDays.Location = new System.Drawing.Point(520, 99);
            this.numReportDurationDays.Maximum = new decimal(new int[] {
            365,
            0,
            0,
            0});
            this.numReportDurationDays.Minimum = new decimal(new int[] {
            0,
            0,
            0,
            0});
            this.numReportDurationDays.Name = "numReportDurationDays";
            this.numReportDurationDays.Size = new System.Drawing.Size(120, 20);
            this.numReportDurationDays.TabIndex = 9;
            this.toolTip1.SetToolTip(this.numReportDurationDays, "Duration of the report in working days, ending on the calculated End Date. (e.g.," +
        " 1 for single day, 5 for a week). Set to 0 to use default based on report type." +
        "");
            this.numReportDurationDays.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            // 
            // lblReportEndDateOffsetDays
            // 
            this.lblReportEndDateOffsetDays.AutoSize = true;
            this.lblReportEndDateOffsetDays.Location = new System.Drawing.Point(397, 75);
            this.lblReportEndDateOffsetDays.Name = "lblReportEndDateOffsetDays";
            this.lblReportEndDateOffsetDays.Size = new System.Drawing.Size(117, 13);
            this.lblReportEndDateOffsetDays.TabIndex = 21;
            this.lblReportEndDateOffsetDays.Text = "End Date Offset (Days):";
            // 
            // numReportEndDateOffsetDays
            // 
            this.numReportEndDateOffsetDays.Location = new System.Drawing.Point(520, 73);
            this.numReportEndDateOffsetDays.Maximum = new decimal(new int[] {
            30,
            0,
            0,
            0});
            this.numReportEndDateOffsetDays.Minimum = new decimal(new int[] {
            0,
            0,
            0,
            0});
            this.numReportEndDateOffsetDays.Name = "numReportEndDateOffsetDays";
            this.numReportEndDateOffsetDays.Size = new System.Drawing.Size(120, 20);
            this.numReportEndDateOffsetDays.TabIndex = 8;
            this.toolTip1.SetToolTip(this.numReportEndDateOffsetDays, "Offset in working days from current day to determine report end date (e.g., 1 for" +
        " previous workday). Set to 0 to use default based on report type.");
            // 
            // txtTemplateName
            // 
            this.txtTemplateName.Location = new System.Drawing.Point(520, 47);
            this.txtTemplateName.Name = "txtTemplateName";
            this.txtTemplateName.Size = new System.Drawing.Size(240, 20);
            this.txtTemplateName.TabIndex = 7;
            this.toolTip1.SetToolTip(this.txtTemplateName, "Filename of the Excel template (e.g., TEMPLATE_Estimate Success Rate.xlsx).");
            // 
            // lblTemplateName
            // 
            this.lblTemplateName.AutoSize = true;
            this.lblTemplateName.Location = new System.Drawing.Point(397, 50);
            this.lblTemplateName.Name = "lblTemplateName";
            this.lblTemplateName.Size = new System.Drawing.Size(84, 13);
            this.lblTemplateName.TabIndex = 18;
            this.lblTemplateName.Text = "Template Name:";
            // 
            // txtSubjectPrefix
            // 
            this.txtSubjectPrefix.Location = new System.Drawing.Point(520, 21);
            this.txtSubjectPrefix.Name = "txtSubjectPrefix";
            this.txtSubjectPrefix.Size = new System.Drawing.Size(240, 20);
            this.txtSubjectPrefix.TabIndex = 6;
            this.toolTip1.SetToolTip(this.txtSubjectPrefix, "Prefix for the automated email subject line (e.g., Daily Estimate Success Rate).");
            // 
            // lblSubjectPrefix
            // 
            this.lblSubjectPrefix.AutoSize = true;
            this.lblSubjectPrefix.Location = new System.Drawing.Point(397, 24);
            this.lblSubjectPrefix.Name = "lblSubjectPrefix";
            this.lblSubjectPrefix.Size = new System.Drawing.Size(74, 13);
            this.lblSubjectPrefix.TabIndex = 16;
            this.lblSubjectPrefix.Text = "Subject Prefix:";
            // 
            // cmbRecipientCategoryKey
            // 
            this.cmbRecipientCategoryKey.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbRecipientCategoryKey.FormattingEnabled = true;
            this.cmbRecipientCategoryKey.Location = new System.Drawing.Point(130, 153);
            this.cmbRecipientCategoryKey.Name = "cmbRecipientCategoryKey";
            this.cmbRecipientCategoryKey.Size = new System.Drawing.Size(240, 21);
            this.cmbRecipientCategoryKey.TabIndex = 5;
            this.toolTip1.SetToolTip(this.cmbRecipientCategoryKey, "Select the key to look up email recipients.");
            // 
            // lblRecipientCategoryKey
            // 
            this.lblRecipientCategoryKey.AutoSize = true;
            this.lblRecipientCategoryKey.Location = new System.Drawing.Point(7, 156);
            this.lblRecipientCategoryKey.Name = "lblRecipientCategoryKey";
            this.lblRecipientCategoryKey.Size = new System.Drawing.Size(122, 13);
            this.lblRecipientCategoryKey.TabIndex = 14;
            this.lblRecipientCategoryKey.Text = "Recipient Category Key:";
            // 
            // cmbGreetingKey
            // 
            this.cmbGreetingKey.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbGreetingKey.FormattingEnabled = true;
            this.cmbGreetingKey.Location = new System.Drawing.Point(130, 127);
            this.cmbGreetingKey.Name = "cmbGreetingKey";
            this.cmbGreetingKey.Size = new System.Drawing.Size(240, 21);
            this.cmbGreetingKey.TabIndex = 4;
            this.toolTip1.SetToolTip(this.cmbGreetingKey, "Select the key to look up the email greeting.");
            // 
            // lblGreetingKey
            // 
            this.lblGreetingKey.AutoSize = true;
            this.lblGreetingKey.Location = new System.Drawing.Point(7, 130);
            this.lblGreetingKey.Name = "lblGreetingKey";
            this.lblGreetingKey.Size = new System.Drawing.Size(71, 13);
            this.lblGreetingKey.TabIndex = 12;
            this.lblGreetingKey.Text = "Greeting Key:";
            // 
            // txtSuccessFlagJsonName
            // 
            this.txtSuccessFlagJsonName.Location = new System.Drawing.Point(130, 101);
            this.txtSuccessFlagJsonName.Name = "txtSuccessFlagJsonName";
            this.txtSuccessFlagJsonName.Size = new System.Drawing.Size(240, 20);
            this.txtSuccessFlagJsonName.TabIndex = 3;
            this.toolTip1.SetToolTip(this.txtSuccessFlagJsonName, "Unique JSON key used to track daily success status in appsettings.json (e.g., St" +
        "andardDailyReportSucceeded).");
            // 
            // lblSuccessFlagJsonName
            // 
            this.lblSuccessFlagJsonName.AutoSize = true;
            this.lblSuccessFlagJsonName.Location = new System.Drawing.Point(7, 104);
            this.lblSuccessFlagJsonName.Name = "lblSuccessFlagJsonName";
            this.lblSuccessFlagJsonName.Size = new System.Drawing.Size(106, 13);
            this.lblSuccessFlagJsonName.TabIndex = 10;
            this.lblSuccessFlagJsonName.Text = "Success Flag Name:";
            // 
            // cmbRunOnDayOfWeek
            // 
            this.cmbRunOnDayOfWeek.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbRunOnDayOfWeek.FormattingEnabled = true;
            this.cmbRunOnDayOfWeek.Location = new System.Drawing.Point(130, 72);
            this.cmbRunOnDayOfWeek.Name = "cmbRunOnDayOfWeek";
            this.cmbRunOnDayOfWeek.Size = new System.Drawing.Size(240, 21);
            this.cmbRunOnDayOfWeek.TabIndex = 2;
            this.toolTip1.SetToolTip(this.cmbRunOnDayOfWeek, "Select specific day for report run, or \'Not Specific\' for daily if enabled.");
            // 
            // lblRunOnDayOfWeek
            // 
            this.lblRunOnDayOfWeek.AutoSize = true;
            this.lblRunOnDayOfWeek.Location = new System.Drawing.Point(7, 75);
            this.lblRunOnDayOfWeek.Name = "lblRunOnDayOfWeek";
            this.lblRunOnDayOfWeek.Size = new System.Drawing.Size(105, 13);
            this.lblRunOnDayOfWeek.TabIndex = 6;
            this.lblRunOnDayOfWeek.Text = "Run on Day of Week:";
            // 
            // cmbReportTypeIndex
            // 
            this.cmbReportTypeIndex.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbReportTypeIndex.FormattingEnabled = true;
            this.cmbReportTypeIndex.Location = new System.Drawing.Point(130, 45);
            this.cmbReportTypeIndex.Name = "cmbReportTypeIndex";
            this.cmbReportTypeIndex.Size = new System.Drawing.Size(240, 21);
            this.cmbReportTypeIndex.TabIndex = 1;
            this.toolTip1.SetToolTip(this.cmbReportTypeIndex, "Internal index for the report type, influences processing logic.");
            // 
            // lblReportTypeIndex
            // 
            this.lblReportTypeIndex.AutoSize = true;
            this.lblReportTypeIndex.Location = new System.Drawing.Point(7, 48);
            this.lblReportTypeIndex.Name = "lblReportTypeIndex";
            this.lblReportTypeIndex.Size = new System.Drawing.Size(96, 13);
            this.lblReportTypeIndex.TabIndex = 4;
            this.lblReportTypeIndex.Text = "Report Type Index:";
            // 
            // chkIsEnabled
            // 
            this.chkIsEnabled.AutoSize = true;
            this.chkIsEnabled.Location = new System.Drawing.Point(390, 212);
            this.chkIsEnabled.Name = "chkIsEnabled";
            this.chkIsEnabled.Size = new System.Drawing.Size(65, 17);
            this.chkIsEnabled.TabIndex = 13;
            this.chkIsEnabled.Text = "Enabled";
            this.toolTip1.SetToolTip(this.chkIsEnabled, "Check to enable this automated report. Uncheck to disable.");
            this.chkIsEnabled.UseVisualStyleBackColor = true;
            // 
            // txtReportName
            // 
            this.txtReportName.Location = new System.Drawing.Point(130, 21);
            this.txtReportName.Name = "txtReportName";
            this.txtReportName.Size = new System.Drawing.Size(240, 20);
            this.txtReportName.TabIndex = 0;
            this.toolTip1.SetToolTip(this.txtReportName, "Descriptive name for this automated report (should be unique).");
            // 
            // lblReportName
            // 
            this.lblReportName.AutoSize = true;
            this.lblReportName.Location = new System.Drawing.Point(7, 24);
            this.lblReportName.Name = "lblReportName";
            this.lblReportName.Size = new System.Drawing.Size(73, 13);
            this.lblReportName.TabIndex = 0;
            this.lblReportName.Text = "Report Name:";
            // 
            // btnAdd
            // 
            this.btnAdd.Location = new System.Drawing.Point(3, 3);
            this.btnAdd.Name = "btnAdd";
            this.btnAdd.Size = new System.Drawing.Size(80, 25);
            this.btnAdd.TabIndex = 0;
            this.btnAdd.Text = "Add";
            this.toolTip1.SetToolTip(this.btnAdd, "Add a new report definition with the details above, or clear fields if a report " +
        "is selected (text changes to \'New\').");
            this.btnAdd.UseVisualStyleBackColor = true;
            this.btnAdd.Click += new System.EventHandler(this.btnAdd_Click);
            // 
            // btnUpdate
            // 
            this.btnUpdate.Location = new System.Drawing.Point(89, 3);
            this.btnUpdate.Name = "btnUpdate";
            this.btnUpdate.Size = new System.Drawing.Size(80, 25);
            this.btnUpdate.TabIndex = 1;
            this.btnUpdate.Text = "Update";
            this.toolTip1.SetToolTip(this.btnUpdate, "Update the selected report definition in the list with the details from the form" +
        " fields.");
            this.btnUpdate.UseVisualStyleBackColor = true;
            this.btnUpdate.Click += new System.EventHandler(this.btnUpdate_Click);
            // 
            // btnDelete
            // 
            this.btnDelete.Location = new System.Drawing.Point(175, 3);
            this.btnDelete.Name = "btnDelete";
            this.btnDelete.Size = new System.Drawing.Size(80, 25);
            this.btnDelete.TabIndex = 2;
            this.btnDelete.Text = "Delete";
            this.toolTip1.SetToolTip(this.btnDelete, "Delete the selected report definition from the list (requires confirmation).");
            this.btnDelete.UseVisualStyleBackColor = true;
            this.btnDelete.Click += new System.EventHandler(this.btnDelete_Click);
            // 
            // btnDuplicate
            // 
            this.btnDuplicate.Enabled = false; // Initially disabled
            this.btnDuplicate.Location = new System.Drawing.Point(261, 3);
            this.btnDuplicate.Name = "btnDuplicate";
            this.btnDuplicate.Size = new System.Drawing.Size(80, 25);
            this.btnDuplicate.TabIndex = 2; // Adjust TabIndex as needed
            this.btnDuplicate.Text = "Duplicate";
            this.toolTip1.SetToolTip(this.btnDuplicate, "Create a copy of the selected report definition.");
            this.btnDuplicate.UseVisualStyleBackColor = true;
            this.btnDuplicate.Click += new System.EventHandler(this.btnDuplicate_Click);
            // 
            // btnSaveChanges
            // 
            this.btnSaveChanges.Location = new System.Drawing.Point(261, 3);
            this.btnSaveChanges.Name = "btnSaveChanges";
            this.btnSaveChanges.Size = new System.Drawing.Size(120, 25);
            this.btnSaveChanges.TabIndex = 3;
            this.btnSaveChanges.Text = "Save All Changes";
            this.toolTip1.SetToolTip(this.btnSaveChanges, "Save all additions, updates, and deletions to the autoReportDefinitions.json fil" +
        "e.");
            this.btnSaveChanges.UseVisualStyleBackColor = true;
            this.btnSaveChanges.Click += new System.EventHandler(this.btnSaveChanges_Click);
            // 
            // btnClose
            // 
            this.btnClose.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnClose.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnClose.Location = new System.Drawing.Point(387, 3);
            this.btnClose.Name = "btnClose";
            this.btnClose.Size = new System.Drawing.Size(80, 25);
            this.btnClose.TabIndex = 4;
            this.btnClose.Text = "Close";
            this.toolTip1.SetToolTip(this.btnClose, "Close this window. Prompts to save if there are unsaved changes.");
            this.btnClose.UseVisualStyleBackColor = true;
            this.btnClose.Click += new System.EventHandler(this.btnClose_Click);
            // 
            // flowLayoutPanelButtons
            // 
            this.flowLayoutPanelButtons.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.flowLayoutPanelButtons.Controls.Add(this.btnAdd);
            this.flowLayoutPanelButtons.Controls.Add(this.btnUpdate);
            this.flowLayoutPanelButtons.Controls.Add(this.btnDelete);
            this.flowLayoutPanelButtons.Controls.Add(this.btnDuplicate);
            this.flowLayoutPanelButtons.Controls.Add(this.btnSaveChanges);
            this.flowLayoutPanelButtons.Controls.Add(this.btnClose);
            this.flowLayoutPanelButtons.Location = new System.Drawing.Point(12, 474);
            this.flowLayoutPanelButtons.Name = "flowLayoutPanelButtons";
            this.flowLayoutPanelButtons.Size = new System.Drawing.Size(776, 35);
            this.flowLayoutPanelButtons.TabIndex = 2;
            // 
            // ManageAutoReportDefinitionsForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.btnClose;
            this.ClientSize = new System.Drawing.Size(800, 521);
            this.Controls.Add(this.flowLayoutPanelButtons);
            this.Controls.Add(this.grpDefinitionDetails);
            this.Controls.Add(this.dgvReportDefinitions);
            this.MinimumSize = new System.Drawing.Size(816, 560);
            this.Name = "ManageAutoReportDefinitionsForm";
            this.Text = "Manage Automated Report Definitions";
            ((System.ComponentModel.ISupportInitialize)(this.dgvReportDefinitions)).EndInit();
            this.grpDefinitionDetails.ResumeLayout(false);
            this.grpDefinitionDetails.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numReportDurationDays)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numReportEndDateOffsetDays)).EndInit();
            this.flowLayoutPanelButtons.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.DataGridView dgvReportDefinitions;
        private System.Windows.Forms.GroupBox grpDefinitionDetails;
        private System.Windows.Forms.Button btnAdd;
        private System.Windows.Forms.Button btnUpdate;
        private System.Windows.Forms.Button btnDelete;
        private System.Windows.Forms.Button btnDuplicate;
        private System.Windows.Forms.Button btnSaveChanges;
        private System.Windows.Forms.Button btnClose;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanelButtons;
        private System.Windows.Forms.Label lblReportName;
        private System.Windows.Forms.TextBox txtReportName;
        private System.Windows.Forms.CheckBox chkIsEnabled;
        private System.Windows.Forms.Label lblReportTypeIndex;
        private System.Windows.Forms.ComboBox cmbReportTypeIndex;
        private System.Windows.Forms.Label lblRunOnDayOfWeek;
        private System.Windows.Forms.ComboBox cmbRunOnDayOfWeek;
        private System.Windows.Forms.Label lblSuccessFlagJsonName;
        private System.Windows.Forms.TextBox txtSuccessFlagJsonName;
        private System.Windows.Forms.Label lblGreetingKey;
        private System.Windows.Forms.ComboBox cmbGreetingKey;
        private System.Windows.Forms.Label lblRecipientCategoryKey;
        private System.Windows.Forms.ComboBox cmbRecipientCategoryKey;
        private System.Windows.Forms.Label lblSubjectPrefix;
        private System.Windows.Forms.TextBox txtSubjectPrefix;
        private System.Windows.Forms.Label lblTemplateName;
        private System.Windows.Forms.TextBox txtTemplateName;
        private System.Windows.Forms.NumericUpDown numReportEndDateOffsetDays;
        private System.Windows.Forms.Label lblReportEndDateOffsetDays;
        private System.Windows.Forms.NumericUpDown numReportDurationDays;
        private System.Windows.Forms.Label lblReportDurationDays;
        private System.Windows.Forms.CheckBox chkRequiresNetValueFiltering;
        private System.Windows.Forms.CheckBox chkAppendToPowerBi;
        private System.Windows.Forms.Label lblReportId;
        private System.Windows.Forms.ToolTip toolTip1;
        private System.Windows.Forms.CheckBox chkIncludeLeadTimeAnalysis;
    }
}