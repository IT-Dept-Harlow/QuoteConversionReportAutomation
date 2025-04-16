using EmailSender;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Windows.Forms;

namespace QuoteConversionReportAutomation
{
    /// <summary>
    /// Represents the main form of the Quote Conversion Report Automation application.
    /// </summary>
    public partial class Form1 : Form
    {
        #region Fields and Properties

        private readonly string _version = "1.1.0";
        private string _generatedFilePath;
        private readonly DateTime _today = DateTime.Today;
        private string _financialYear = ExcelCopyData.GetCurrentFinancialYear(true);

        /// <summary>
        /// Gets the Crystal Report location from application settings.
        /// </summary>
        public string CrystalReportLocation { get; } = ConfigurationManager.AppSettings["CrystalReportPath"];

        /// <summary>
        /// Gets the location where the report output will be saved, based on the selected report type.
        /// </summary>
        public string ReportOutputLocation
        {
            get
            {
                string baseDir = $@"C:\Users\{Environment.UserName}\Harlow Printing\IT Projects - Documents\Dashboard Datasets\Raw_data\Quotes conversion\Estimate Reports Exports";
                string fileName = $"{_today:yyyyMMdd} Estimate Success Report.xlsx";
                string result;

                switch (typeDropBox.SelectedIndex)
                {
                    case 1:
                        result = Path.Combine(baseDir, "Monthly Reports", fileName);
                        break;
                    case 2:
                        result = Path.Combine(baseDir, "Quarterly reports", fileName);
                        break;
                    case 3:
                        result = Path.Combine(baseDir, "Annual Reports", fileName);
                        break;
                    default:
                        result = Path.Combine(baseDir, "Weekly Reports", fileName); // Default to Weekly
                        break;
                }
                return result;
            }
        }

        /// <summary>
        /// Gets the location of the Excel copy template file, based on the selected report type.
        /// </summary>
        public string ExcelCopyTemplateLocation
        {
            get
            {
                string baseDir = $@"C:\Users\{Environment.UserName}\Harlow Printing\IT Projects - Documents\Dashboard Datasets\Raw_data\Quotes conversion\TEMPLATE\";
                string templateName = (typeDropBox.SelectedIndex == 1 || typeDropBox.SelectedIndex == 2 || typeDropBox.SelectedIndex == 3)
                    ? "TEMPLATE_Estimate Success Rate_Monthly.xlsx"  // Monthly, Quarterly, Annual
                    : "TEMPLATE_Estimate Success Rate.xlsx";        // Weekly or invalid

                return Path.Combine(baseDir, templateName);
            }
        }

        /// <summary>
        /// Gets the location where the Excel copy will be saved, based on the selected report type.
        /// </summary>
        public string ExcelCopySaveLocation
        {
            get
            {
                string year = DateTime.Today.Year.ToString();
                string baseDir = $@"C:\Users\{Environment.UserName}\Harlow Printing\IT Projects - Documents\Dashboard Datasets\Raw_data\Quotes conversion\Estimates\";
                string result;
                switch (typeDropBox.SelectedIndex)
                {
                    case 1:
                        result = Path.Combine(baseDir, "Monthly reports", year);
                        break;
                    case 2:
                        result = Path.Combine(baseDir, "Quarterly reports", year);
                        break;
                    case 3:
                        result = Path.Combine(baseDir, "Annual reports");
                        break;
                    default:
                        result = Path.Combine(baseDir, "Weekly reports", year);
                        break;
                }
                return result;
            }
        }

        #endregion Fields and Properties

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the Form1 class.
        /// </summary>
        public Form1()
        {
            // Logs the start of form initialization.
            Logger.LogInfo("Initialising...");

            // Initializes the form components.
            InitializeComponent();

            // Populate the financial year dropdown with the current and previous financial years.
            PopulateFinancialYearDropdown();

            finYearDropBox.SelectedIndex = 0;

#if DEBUG
            Text = $"Quote Conversion Automation - Debug - {_version}";
#else
            Text = $"Quote Conversion Automation - Release - {_version}";
#endif
            // Centers the form on the screen.
            StartPosition = FormStartPosition.CenterScreen;

            //hide as not used outside weekly reports
            label5.Visible = false;
            finYearDropBox.Visible = false;

            // Disables the second button initially.
            button2.Enabled = false;

            // Hides the report and analysis view buttons initially.
            btnViewReport.Visible = false;
            btnViewAnalysis.Visible = false;

            //init dropdown
            typeDropBox.SelectedIndex = 0;

            // Sets the "from" date picker to 15 days before today.
            DateTime dateFromParam = _today.AddDays(-15);
            Logger.LogInfo("Date From set to " + dateFromParam);
            datepickFrom.Value = dateFromParam;

            // Sets the "to" date picker to today's date.
            datepickTo.Value = _today;

            // Logs the completion of form initialization.
            Logger.LogInfo("Initialisation Complete");
        }

        #endregion Constructors

        #region Event Handlers

        /// <summary>
        /// Handles the click event of the report creation button.
        /// </summary>
        private void Button1_Click(object sender, EventArgs e)
        {
            // Disable controls
            button1.Enabled = false;
            button1.Text = "Creating Report...";
            EnableDatePickFrom(false);
            EnableDatePickTo(false);
            EnableFinYearDropBox(false);
            EnableCheckBox1(false);
            EnabletypeDropBox(false);
            // Logs the button click.
            Logger.LogDebug("Report creation button pressed");

            // Validates that the "from" date is not after the "to" date.
            if (datepickFrom.Value > datepickTo.Value)
            {
                Logger.LogError("From date is after To date. Report creation aborted.");
                MessageBox.Show("The 'From' date cannot be after the 'To' date.", "Date Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return; // Exits the method if dates are invalid.
            }

            //check financial year
            if (!IsFinancialYearValid(finYearDropBox.SelectedItem.ToString(), datepickFrom.Value, datepickTo.Value))
            {
                DialogResult dialogResult = MessageBox.Show("The selected financial year does not match the selected date range. Do you want to continue?", "Financial Year Mismatch", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (dialogResult == DialogResult.No)
                {
                    EnableButton1(true);
                    EnableDatePickFrom(true);
                    EnableDatePickTo(true);
                    EnableFinYearDropBox(true);
                    EnableCheckBox1(true);
                    EnabletypeDropBox(true);
                    button1.Text = "Try Again";
                    return;
                }
            }

            // Validates that the Crystal Report location is set.
            if (string.IsNullOrEmpty(CrystalReportLocation))
            {
                Logger.LogError("Crystal Report Location is null or empty. Report creation aborted.");
                MessageBox.Show("The Crystal Report Location is not set.", "Configuration Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            try
            {
                // Creates an instance of the report runner class.
                RunCrystalReportClass reportRunner = new RunCrystalReportClass(typeDropBox.SelectedIndex);
                // Runs the Crystal Report.
                reportRunner.RunReport(CrystalReportLocation, ReportOutputLocation, datepickFrom.Value, datepickTo.Value, statusStrip1);

                // Updates the button state and visibility.
                button1.Text = "Complete";
                button2.Enabled = true;
                btnViewReport.Visible = true;
            }
            catch (Exception ex)
            {
                // Logs and displays an error message if report creation fails.
                Logger.LogError($"Error creating Crystal report: {ex.Message}");
                MessageBox.Show($"An error occurred while creating the report: {ex.Message}", "Report Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                button1.Text = "Error";
                button1.Enabled = true; // Re-enable on error
            }
        }

        /// <summary>
        /// Handles the Click event of the button2 control.  This method initiates the Excel data copying
        /// process and handles potential errors, ensuring the UI is updated appropriately.
        /// </summary>
        private void Button2_Click(object sender, EventArgs e)
        {
            // Updates the button text to indicate processing.
            button2.Text = "Processing...";
            // Disable the button to prevent multiple clicks
            button2.Enabled = false;
            _generatedFilePath = "";

            try
            {
                // Validates file paths.
                if (string.IsNullOrEmpty(ReportOutputLocation) || string.IsNullOrEmpty(ExcelCopySaveLocation) || string.IsNullOrEmpty(ExcelCopyTemplateLocation))
                {
                    MessageBox.Show("File paths are invalid.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    button2.Text = "Error"; // Updates button text to indicate an error.
                    button2.Enabled = true; // Re-enables the button to allow the user to correct the error.
                    return; // Exits the method if file paths are invalid.
                }

                string sourceSheetName = "Sheet1";
                string destinationSheetName = "DATA";
                string financialYear = finYearDropBox.SelectedItem.ToString();

                // Calls the method to copy data between Excel sheets and gets the generated file path.
                // Pass the SendEmailWithAttachment method as an Action.
                // Store the action, and the result
                Action<string> setEmailAction = SendEmailWithAttachment;
                Action<string> setTextAction = SetButton2Text;
                Action<string> setTextAction2 = SetButton1Text;
                Action<bool> enableAction = EnableButton2;
                Action<bool> enableAction2 = EnableButton1;
                Action<bool> showAnalysisButtonAction = ShowBtnViewAnalysis;
                Action<bool> enableDatePickFromAction = EnableDatePickFrom;
                Action<bool> enableDatePickToAction = EnableDatePickTo;
                Action<bool> enableFinYearDropBoxAction = EnableFinYearDropBox;
                Action<bool> enableCheckBox1Action = EnableCheckBox1;
                Action<bool> enabletypeDropBoxAction = EnabletypeDropBox;

                ExcelCopyData.CopyDataBetweenExcelSheetsAsync(financialYear, typeDropBox.SelectedIndex, checkBox1.Checked, ReportOutputLocation, sourceSheetName, ExcelCopySaveLocation, ExcelCopyTemplateLocation, destinationSheetName, 0, 0, statusStrip1, setEmailAction, setTextAction, setTextAction2, enableAction, enableAction2, showAnalysisButtonAction, enableDatePickFromAction, enableDatePickToAction, enableFinYearDropBoxAction, enableCheckBox1Action, enabletypeDropBoxAction); // Pass value
            }
            catch (FileNotFoundException ex)
            {
                // Handles file not found exceptions.
                Logger.LogError($"File not found: {ex.Message}");
                MessageBox.Show($"File not found: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                button2.Text = "Error";
                button2.Enabled = true; // Re-enable on error
            }
            catch (IOException ex)
            {
                // Handles IO exceptions.
                Logger.LogError($"IO error: {ex.Message}");
                MessageBox.Show($"IO error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                button2.Text = "Error";
                button2.Enabled = true; // Re-enable on error
            }
            catch (Exception ex)
            {
                // Handles general exceptions.
                Logger.LogError($"Failed to copy data or process pivot table: {ex}");
                MessageBox.Show($"Failed to copy data or process pivot table: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                button2.Text = "Error";
                button2.Enabled = true; // Re-enable the button
            }
        }

        /// <summary>
        /// sets the filePath from the excel class
        /// </summary>
        private void SetFilePath(string filePath)
        {
            _generatedFilePath = filePath;
        }

        /// <summary>
        /// Handles the click event of the "View Report" button.
        /// Opens the report file specified by ReportOutputLocation.
        /// </summary>
        private void btnViewReport_Click(object sender, EventArgs e)
        {
            // Null check for sender and event arguments.
            if (sender == null || e == null) return;

            // Check if the report output location is set.
            if (!string.IsNullOrEmpty(ReportOutputLocation))
            {
                try
                {
                    // Create an instance of OpenFileClass.
                    OpenFileClass openFile = new OpenFileClass();

                    // Attempt to open the report file.
                    if (openFile.OpenFile(ReportOutputLocation))
                    {
                        // Optionally display a success message (commented out).
                        Logger.LogInfo("File opened successfully: " + ReportOutputLocation);
                    }
                }
                catch (Exception ex)
                {
                    // Log the exception for debugging purposes.
                    Logger.LogError($"Error opening report: {ex}");

                    // Display an error message to the user.
                    MessageBox.Show($"An unexpected error occurred while opening the report.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                // Display a message if the report output location is not set.
                Logger.LogInfo($"No file path available to view.");
                MessageBox.Show("Report output location is not set.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        /// <summary>
        /// Handles the click event of the "View Analysis" button.
        /// Opens the file specified by _generatedFilePath.
        /// </summary>
        private void btnViewAnalysis_Click(object sender, EventArgs e)
        {
            if (sender == null || e == null) return;

            if (!string.IsNullOrEmpty(_generatedFilePath))
            {
                try
                {
                    OpenFileClass openFile = new OpenFileClass();
                    if (openFile.OpenFile(_generatedFilePath))
                    {
                        // Optionally display success message or perform other actions
                        Logger.LogInfo("File opened successfully: " + _generatedFilePath);
                    }

                }
                catch (Exception ex)
                {
                    // Log the exception for debugging
                    Logger.LogError($"Error opening file: {ex}");
                    MessageBox.Show($"An unexpected error occurred while opening the file.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                Logger.LogInfo($"No file path available to view.");
                MessageBox.Show("No file path available to view.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        /// <summary>
        /// Handles the SelectedIndexChanged event of the typeDropBox ComboBox.
        /// This method calculates and sets the date range for datepickFrom and datepickTo
        /// based on the user's selection in the ComboBox.
        /// </summary>
        private void typeDropBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Check if the sender is a ComboBox and an item is selected.
            if (sender is ComboBox comboBox && comboBox.SelectedItem != null)
            {
                // Get the selected index.
                int selectedIndex = comboBox.SelectedIndex;
                // Get today's date.
                DateTime today = DateTime.Today;

                // Use a switch statement to determine the date range based on the selected index.
                switch (selectedIndex)
                {
                    case 0: // Weekly - Last 15 days
                        // Calculate the date 15 days prior to today.
                        DateTime fifteenDaysAgo = today.AddDays(-15);
                        // Set the value of the "From" date picker.
                        datepickFrom.Value = fifteenDaysAgo;
                        // Set the value of the "To" date picker.
                        datepickTo.Value = today;
                        // Log the date range.
                        Logger.LogInfo($"typeDropBox set to Weekly (Last 15 Days), Date From: {fifteenDaysAgo}, Date To: {today}");

                        //show as used in weekly report
                        label5.Visible = true;
                        finYearDropBox.Visible = true;
                        PopulateFinancialYearDropdown();

                        break;

                    case 1: // Monthly
                        // Calculate the first day of the current month.
                        DateTime firstDayOfMonth = new DateTime(today.Year, today.Month, 1);
                        // Calculate the last day of the current month.
                        DateTime lastDayOfMonth = firstDayOfMonth.AddMonths(1).AddDays(-1);

                        // Check if today is in the first half of the month.
                        if (today.Day <= 15)
                        {
                            // If in the first half, get the first and last days of the previous month.
                            DateTime firstDayOfLastMonth = firstDayOfMonth.AddMonths(-1);
                            DateTime lastDayOfLastMonth = firstDayOfMonth.AddDays(-1);
                            // Set the "From" and "To" date pickers to the previous month.
                            datepickFrom.Value = firstDayOfLastMonth;
                            datepickTo.Value = lastDayOfLastMonth;
                            // Log the date range.
                            Logger.LogInfo($"typeDropBox set to Monthly (first half), Date From: {firstDayOfLastMonth}, Date To: {lastDayOfLastMonth}");
                        }
                        else
                        {
                            // If in the second half, set the "From" and "To" date pickers to the current month.
                            datepickFrom.Value = firstDayOfMonth;
                            datepickTo.Value = lastDayOfMonth;
                            // Log the date range.
                            Logger.LogInfo($"typeDropBox set to Monthly (second half), Date From: {firstDayOfMonth}, Date To: {lastDayOfMonth}");
                        }

                        //hide as not used outside weekly reports
                        label5.Visible = false;
                        finYearDropBox.Visible = false;
                        finYearDropBox.Items.Clear();
                        finYearDropBox.Items.Add(ExcelCopyData.GetCurrentFinancialYear(true));
                        finYearDropBox.SelectedIndex = 0;

                        break;

                    case 2: // Quarterly - Previous Quarter
                        // Calculate the first day of the current quarter.
                        DateTime firstDayOfCurrentQuarter;
                        if (today.Month >= 1 && today.Month <= 3)
                        {
                            firstDayOfCurrentQuarter = new DateTime(today.Year, 1, 1);
                        }
                        else if (today.Month >= 4 && today.Month <= 6)
                        {
                            firstDayOfCurrentQuarter = new DateTime(today.Year, 4, 1);
                        }
                        else if (today.Month >= 7 && today.Month <= 9)
                        {
                            firstDayOfCurrentQuarter = new DateTime(today.Year, 7, 1);
                        }
                        else
                        {
                            firstDayOfCurrentQuarter = new DateTime(today.Year, 10, 1);
                        }

                        // Calculate the first and last days of the *previous* quarter.
                        DateTime firstDayOfPreviousQuarter = firstDayOfCurrentQuarter.AddMonths(-3);
                        DateTime lastDayOfPreviousQuarter = firstDayOfCurrentQuarter.AddDays(-1);

                        // Set the "From" and "To" date pickers to the previous quarter.
                        datepickFrom.Value = firstDayOfPreviousQuarter;
                        datepickTo.Value = lastDayOfPreviousQuarter;
                        // Log the date range.
                        Logger.LogInfo($"typeDropBox set to Quarterly (Previous Quarter), Date From: {firstDayOfPreviousQuarter}, Date To: {lastDayOfPreviousQuarter}");

                        //hide as not used outside weekly reports
                        label5.Visible = false;
                        finYearDropBox.Visible = false;
                        finYearDropBox.Items.Clear();
                        finYearDropBox.Items.Add(ExcelCopyData.GetCurrentFinancialYear(true));
                        finYearDropBox.SelectedIndex = 0;

                        break;

                    case 3: // Annual - Last Year.
                        // Calculate the first day of the last year.
                        DateTime firstDayOfLastYear = new DateTime(today.Year - 1, 1, 1);
                        // Calculate the last day of the last year.
                        DateTime lastDayOfLastYear = new DateTime(today.Year - 1, 12, 31);
                        // Set the "From" and "To" date pickers to the entire last year.
                        datepickFrom.Value = firstDayOfLastYear;
                        datepickTo.Value = lastDayOfLastYear;
                        // Log the date range.
                        Logger.LogInfo($"typeDropBox set to Annual (Last Year), Date From: {firstDayOfLastYear}, Date To: {lastDayOfLastYear}");

                        //hide as not used outside weekly reports
                        label5.Visible = false;
                        finYearDropBox.Visible = false;
                        finYearDropBox.Items.Clear();
                        finYearDropBox.Items.Add(ExcelCopyData.GetCurrentFinancialYear(true));
                        finYearDropBox.SelectedIndex = 0;

                        break;

                    default:
                        // Log a warning for an invalid selection.
                        Logger.LogWarning("Invalid typeDropBox selection.");
                        break;
                }
            }
            else
            {
                // Log a warning if the sender is not a ComboBox or no item is selected.
                Logger.LogWarning("Sender is not a ComboBox or no item selected.");
            }
        }

        #endregion Event Handlers

        #region Helper Methods

        // Action methods to update the UI.  These MUST be in the form.
        /// <summary>
        /// Sets the text of button1.
        /// </summary>
        /// <param name="text">The text to set.</param>
        private void SetButton1Text(string text)
        {
            if (button1.InvokeRequired)
            {
                button1.Invoke(new MethodInvoker(delegate { button1.Text = text; }));
            }
            else
            {
                button1.Text = text;
            }
        }

        /// <summary>
        /// Enables or disables button1.
        /// </summary>
        /// <param name="enable">True to enable, false to disable.</param>
        private void EnableButton1(bool enable)
        {
            if (button1.InvokeRequired)
            {
                button1.Invoke(new MethodInvoker(delegate { button1.Enabled = enable; }));
            }
            else
            {
                button1.Enabled = enable;
            }
        }

        /// <summary>
        /// Sets the text of button2.
        /// </summary>
        /// <param name="text">The text to set.</param>
        private void SetButton2Text(string text)
        {
            if (button2.InvokeRequired)
            {
                button2.Invoke(new MethodInvoker(delegate { button2.Text = text; }));
            }
            else
            {
                button2.Text = text;
            }
        }

        /// <summary>
        /// Enables or disables button2.
        /// </summary>
        /// <param name="enable">True to enable, false to disable.</param>
        private void EnableButton2(bool enable)
        {
            if (button2.InvokeRequired)
            {
                button2.Invoke(new MethodInvoker(delegate { button2.Enabled = enable; }));
            }
            else
            {
                button2.Enabled = enable;
            }
        }

        private void EnabletypeDropBox(bool enable)
        {
            if (typeDropBox.InvokeRequired)
            {
                typeDropBox.Invoke(new MethodInvoker(delegate { typeDropBox.Enabled = enable; }));
            }
            else
            {
                typeDropBox.Enabled = enable;
            }
        }

        /// <summary>
        /// Enables or disables datepickFrom.
        /// </summary>
        /// <param name="enable">True to enable, false to disable.</param>
        private void EnableDatePickFrom(bool enable)
        {
            if (datepickFrom.InvokeRequired)
            {
                datepickFrom.Invoke(new MethodInvoker(delegate { datepickFrom.Enabled = enable; }));
            }
            else
            {
                datepickFrom.Enabled = enable;
            }
        }

        /// <summary>
        /// Enables or disables datepickTo.
        /// </summary>
        /// <param name="enable">True to enable, false to disable.</param>
        private void EnableDatePickTo(bool enable)
        {
            if (datepickTo.InvokeRequired)
            {
                datepickTo.Invoke(new MethodInvoker(delegate { datepickTo.Enabled = enable; }));
            }
            else
            {
                datepickTo.Enabled = enable;
            }
        }

        /// <summary>
        /// Enables or disables finYearDropBox.
        /// </summary>
        /// <param name="enable">True to enable, false to disable.</param>
        private void EnableFinYearDropBox(bool enable)
        {
            if (finYearDropBox.InvokeRequired)
            {
                finYearDropBox.Invoke(new MethodInvoker(delegate { finYearDropBox.Enabled = enable; }));
            }
            else
            {
                finYearDropBox.Enabled = enable;
            }
        }

        /// <summary>
        /// Enables or disables checkBox1.
        /// </summary>
        private void EnableCheckBox1(bool enable)
        {
            if (checkBox1.InvokeRequired)
            {
                checkBox1.Invoke(new MethodInvoker(delegate { checkBox1.Enabled = enable; }));
            }
            else
            {
                checkBox1.Enabled = enable;
            }
        }

        /// <summary>
        /// Shows or hides the analysis button.
        /// </summary>
        /// <param name="show">True to show, false to hide.</param>
        private void ShowBtnViewAnalysis(bool show)
        {
            if (btnViewAnalysis.InvokeRequired)
            {
                btnViewAnalysis.Invoke(new MethodInvoker(delegate { btnViewAnalysis.Visible = show; }));
            }
            else
            {
                btnViewAnalysis.Visible = show;
            }
        }

        /// <summary>
        /// Sends an email with the specified file attached.
        /// </summary>
        /// <param name="attachmentPath">The path to the file to attach to the email.</param>
        /// <remarks>
        /// This method handles both debug and release configurations for recipient lists.
        /// It also includes basic error handling and progress logging.
        /// </remarks>
        private void SendEmailWithAttachment(string attachmentPath)
        {
            try
            {
                // TODO: Change this to read configuration from a configuration object, or environment variables.
                string smtpServer = "harlowsolutions-co-uk.mail.protection.outlook.com";
                int smtpPort = 25;
                string smtpUsername = "chrisp@harlowsolutions.co.uk"; // replace with IT email
                string smtpPassword = "Pringc1!"; // Replace with secure storage.
                bool enableSsl = true;

                EmailUtility emailUtility = new EmailUtility(smtpServer, smtpPort, smtpUsername, smtpPassword, enableSsl);

                List<string> toAddresses = new List<string>();
                List<string> ccAddresses = new List<string>();

#if DEBUG
                if (checkBox1.Checked)
                {
                    // Debug configuration: send emails to the me & Jamie for testing.
                    toAddresses.Add("chrisp@harlowsolutions.co.uk");
                    ccAddresses.Add("jamier@harlowsolutions.co.uk");
                }
                else
                {
                    // Debug configuration: send emails to the me for testing.
                    toAddresses.Add("chrisp@harlowsolutions.co.uk");
                    ccAddresses.Add("chrisp@harlowsolutions.co.uk");
                }

#else
                if (checkBox1.Checked)//Send to femi only
                {
                    ccAddresses.Add("femi@harlowsolutions.co.uk");
                    ccAddresses.Add("ITdept@harlowsolutions.co.uk");
                }
                else
                {
                    // Release configuration: send emails to the team.
                    toAddresses.Add("andrewp@harlowsolutions.co.uk");
                    toAddresses.Add("kirstym@harlowsolutions.co.uk");
                    toAddresses.Add("stuartm@harlowsolutions.co.uk");
                    ccAddresses.Add("emmanuel@harlowsolutions.co.uk");
                    ccAddresses.Add("femi@harlowsolutions.co.uk");
                    ccAddresses.Add("jackh@harlowsolutions.co.uk");
                    ccAddresses.Add("pauls@harlowsolutions.co.uk");
                    ccAddresses.Add("ITdept@harlowsolutions.co.uk");
                    ccAddresses.Add("gordonb@harlowsolutions.co.uk");
                }
#endif

                string subject;
                string body;
                string greeting = "Hi All,\r\n\r\n";
                string greetingFemi = "Hi Femi,\r\n\r\n";
                string actualGreeting = checkBox1.Checked ? greetingFemi : greeting; //changes if checked, greeting Femi, else all.
                string financialYearText = "";
                if (typeDropBox.SelectedIndex == 0 && finYearDropBox.SelectedItem.ToString() != ExcelCopyData.GetCurrentFinancialYear(true))
                {
                    financialYearText = $" for Financial Year {finYearDropBox.SelectedItem}.";
                }

                switch (typeDropBox.SelectedIndex)
                {
                    case 0:
                        subject = "Weekly Estimate Success Rate Report";
                        body = $"{actualGreeting}Please see the \"Estimates Success Rate\" file attached with the list of quotes in the last two weeks for the entire team{financialYearText}; please review them ahead of your respective check-ins for follow-ups required.\r\n\r\nThank you.\r\n";
                        break;
                    case 1:
                        subject = "Monthly Estimate Success Rate Report";
                        body = $"{actualGreeting}Please see the \"Estimates Success Rate\" file attached with the list of quotes in for: {datepickFrom.Value:MMMMM} for the entire team; please review them ahead of your respective check-ins for follow-ups required.\r\n\r\nThank you.\r\n";
                        break;
                    case 2:
                        subject = "Quarterly Estimate Success Rate Report";
                        body = $"{actualGreeting}Please see the \"Estimates Success Rate\" file attached with the list of quotes for the previous quarter: {GetQuarterString(datepickFrom.Value)} {datepickFrom.Value.Year}  for the entire team; please review them ahead of your respective check-ins for follow-ups required.\r\n\r\nThank you.\r\n";
                        break;
                    case 3:
                        subject = "Annual Estimate Success Rate Report";
                        body = $"{actualGreeting}Please see the \"Estimates Success Rate\" file attached with the list of quotes for the year: {datepickFrom.Value.Year} for the entire team; please review them ahead of your respective check-ins for follow-ups required.\r\n\r\nThank you.\r\n";
                        break;
                    default:
                        subject = "Estimate Success Rate Report";  // Default Subject
                        body = $"{actualGreeting}Please see the \"Estimates Success Rate\" file attached.\r\n\r\nThank you.\r\n";
                        break;
                }

                // Validate attachment path.
                if (!File.Exists(attachmentPath))
                {
                    throw new FileNotFoundException($"Attachment file not found: {attachmentPath}");
                }

                // Send the email using the EmailUtility.
                emailUtility.SendEmail(toAddresses, ccAddresses, subject, body, attachmentPath,
                    progress => Logger.LogInfo(progress), // Log progress updates.
                    (success, error) => // Handle email sending completion.
                    {
                        if (success)
                        {
                            statusStrip1.Invoke(new MethodInvoker(delegate { statusStrip1.Items[0].Text = "Email Sent - Report Completed Please Close Program. "; }));
                        }
                        else
                        {
                            MessageBox.Show($"{(typeDropBox.SelectedIndex == 0 ? "Weekly" : typeDropBox.SelectedIndex == 1 ? "Monthly" : typeDropBox.SelectedIndex == 2 ? "Quarterly" : "Annual")} report email failed: {error}");
                            Logger.LogError($"{(typeDropBox.SelectedIndex == 0 ? "Weekly" : typeDropBox.SelectedIndex == 1 ? "Monthly" : typeDropBox.SelectedIndex == 2 ? "Quarterly" : "Annual")} report email failed: {error}"); // Log detailed error.
                        }
                    }, statusStrip1);
            }
            catch (Exception ex)
            {
                // Handle exceptions during email sending.
                MessageBox.Show($"An error occurred while sending the email: {ex.Message}");
                Logger.LogError($"An error occurred while sending the email: {ex.Message}"); // Log detailed error.
            }
        }


        /// <summary>
        /// Gets the quarter string (e.g., "Q1", "Q2", "Q3", "Q4") for a given date.
        /// </summary>
        /// <param name="date">The date for which to determine the quarter.</param>
        /// <returns>The quarter string.</returns>
        private static string GetQuarterString(DateTime date)
        {
            string result;
            if (date.Month >= 1 && date.Month <= 3)
            {
                result = "Q1";
            }
            else if (date.Month >= 4 && date.Month <= 6)
            {
                result = "Q2";
            }
            else if (date.Month >= 7 && date.Month <= 9)
            {
                result = "Q3";
            }
            else
            {
                result = "Q4";
            }
            return result;
        }

        /// <summary>
        /// Populates the financial year dropdown with the current and previous financial years.
        /// </summary>
        private void PopulateFinancialYearDropdown()
        {
            // Get the current financial year
            string currentFinancialYear = ExcelCopyData.GetCurrentFinancialYear(true);
            // Calculate the previous financial year.
            string previousFinancialYear = GetPreviousFinancialYear(currentFinancialYear);

            // Clear existing items in the dropdown.
            finYearDropBox.Items.Clear();

            // Add the current and previous financial years.
            finYearDropBox.Items.Add(currentFinancialYear);
            finYearDropBox.Items.Add(previousFinancialYear);

            //set selected index
            finYearDropBox.SelectedIndex = 0;
        }

        /// <summary>
        /// Calculates the previous financial year from the current financial year.
        /// </summary>
        /// <param name="currentFinancialYear">The current financial year in "YYYY_YY" format (e.g., "2024_25").</param>
        /// <returns>The previous financial year in "YYYY_YY" format, or null if the input is invalid.</returns>
        private static string GetPreviousFinancialYear(string currentFinancialYear)
        {
            string[] years = currentFinancialYear.Split('_');
            if (years.Length == 2 && int.TryParse(years[0], out int startYear))
            {
                return $"{startYear - 1}_{years[0].Substring(2)}";
            }
            else
            {
                return null;
            }
        }

        private bool IsFinancialYearValid(string selectedFinYear, DateTime fromDate, DateTime toDate)
        {
            string fromYearPart = selectedFinYear.Substring(0, 4);
            string toYearPart = selectedFinYear.Substring(5, 2);

            int fromYear = int.Parse(fromYearPart);
            int toYear = int.Parse("20" + toYearPart);

            //check if the from and to dates are within the financial year
            bool fromDateValid = (fromDate.Year == fromYear && fromDate.Month >= 5) || (fromDate.Year == toYear && fromDate.Month < 5);
            bool toDateValid = (toDate.Year == fromYear && toDate.Month >= 5) || (toDate.Year == toYear && toDate.Month < 5);

            return fromDateValid && toDateValid;
        }
        #endregion Helper Methods
    }
}

