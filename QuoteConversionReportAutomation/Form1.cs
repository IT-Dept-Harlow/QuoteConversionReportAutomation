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
        string Version = "1.0.3";

        // Private field to store the generated file path.
        private string _generatedFilePath;

        // Field to store today's date.
        readonly DateTime today = DateTime.Today;

        /// <summary>
        /// Property to get the Crystal Report location from application settings.
        /// </summary>
        public string crystalReportLocation = ConfigurationManager.AppSettings["CrystalReportPath"];

        /// <summary>
        /// Gets the location where the report output will be saved.
        /// </summary>
        /// <summary>
        /// Gets the location where the report output will be saved.
        /// </summary>
        public string ReportOutputLocation
        {
            get
            {
                // Gets today's date.
                DateTime today = DateTime.Today;
                string baseDir = @"C:\Users\" + Environment.UserName + @"\Harlow Printing\IT Projects - Documents\Dashboard Datasets\Raw_data\Quotes conversion\Estimate Reports Exports";
                // Constructs the file path using the current user's profile and date.
                if (checkBox1.Checked)
                {
                    return Path.Combine(baseDir, "Monthly Reports", $"{today.ToString("yyyyMMdd ")}Estimate Success Report.xlsx");
                }
                else
                {
                    return Path.Combine(baseDir, "Weekly Reports", $"{today.ToString("yyyyMMdd ")}Estimate Success Report.xlsx");
                }
            }
        }

        /// <summary>
        /// Gets the location of the Excel copy template file.
        /// </summary>
        public string excelCopyTemplateLocation
        {
            get
            {
                string baseDir = @"C:\Users\" + Environment.UserName + @"\Harlow Printing\IT Projects - Documents\Dashboard Datasets\Raw_data\Quotes conversion\Estimates\";
                if (checkBox1.Checked)
                {
                    // Constructs the template file path.
                    return Path.Combine(baseDir, $"Monthly reports", "TEMPLATE_Estimate Success Rate MMM 2025.xlsx");
                }
                else
                {
                    // Constructs the template file path.
                    return Path.Combine(baseDir, $"Weekly reports", "TEMPLATE_Estimate Success Rate.xlsx");
                }
            }
        }

        /// <summary>
        /// Gets the location where the Excel copy will be saved.
        /// </summary>
        public string excelCopytSaveLocation
        {
            get
            {
                // Gets today's date.
                DateTime today = DateTime.Today;
                string baseDir = @"C:\Users\" + Environment.UserName + @"\Harlow Printing\IT Projects - Documents\Dashboard Datasets\Raw_data\Quotes conversion\Estimates\";
                // Constructs the save location path.
                if (checkBox1.Checked)
                {
                    return Path.Combine(baseDir, "Monthly reports", "2025");
                }
                else
                {
                    return Path.Combine(baseDir, "Weekly reports", "2025");
                }
            }
        }


        /// <summary>
        /// Initializes a new instance of the Form1 class.
        /// </summary>
        public Form1()
        {
            // Logs the start of form initialization.
            Logger.LogInfo("Initialising...");

            // Initializes the form components.
            InitializeComponent();


#if DEBUG
            Text = $"Quote Conversion Automation - Debug - {Version}";
#else
            Text = $"Quote Conversion Automation - Release - {Version}";
#endif

            // Centers the form on the screen.
            StartPosition = FormStartPosition.CenterScreen;

            // Disables the second button initially.
            button2.Enabled = false;

            // Hides the report and analysis view buttons initially.
            btnViewReport.Visible = false;
            btnViewAnalysis.Visible = false;

            // Sets the "from" date picker to 15 days before today.
            DateTime dateFromParam = today.AddDays(-15);
            Logger.LogInfo("Date From set to " + dateFromParam);
            datepickFrom.Value = dateFromParam;

            // Sets the "to" date picker to today's date.
            datepickTo.Value = today;

            // Logs the completion of form initialization.
            Logger.LogInfo("Initialisation Complete");
        }

        /// <summary>
        /// Handles the click event of the report creation button.
        /// </summary>
        private void Button1_Click(object sender, EventArgs e)
        {
            // Logs the button click.
            Logger.LogDebug("Report creation button pressed");

            // Validates that the "from" date is not after the "to" date.
            if (datepickFrom.Value > datepickTo.Value)
            {
                Logger.LogError("From date is after To date. Report creation aborted.");
                MessageBox.Show("The 'From' date cannot be after the 'To' date.", "Date Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return; // Exits the method if dates are invalid.
            }

            // Validates that the Crystal Report location is set.
            if (string.IsNullOrEmpty(crystalReportLocation))
            {
                Logger.LogError("Crystal Report Location is null or empty. Report creation aborted.");
                MessageBox.Show("The Crystal Report Location is not set.", "Configuration Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            try
            {
                // Creates an instance of the report runner class.
                RunCrystalReportClass reportRunner = new RunCrystalReportClass(checkBox1.Checked); // Pass the value
                // Runs the Crystal Report.
                reportRunner.RunReport(crystalReportLocation, ReportOutputLocation, datepickFrom.Value, datepickTo.Value, statusStrip1);

                // Updates the button state and visibility.
                button1.Text = "Complete";
                button1.Enabled = false;
                button2.Enabled = true;
                btnViewReport.Visible = true;
            }
            catch (Exception ex)
            {
                // Logs and displays an error message if report creation fails.
                Logger.LogError($"Error creating Crystal report: {ex.Message}");
                MessageBox.Show($"An error occurred while creating the report: {ex.Message}", "Report Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Handles the Click event of the button2 control.  This method initiates the Excel data copying
        /// process and handles potential errors, ensuring the UI is updated appropriately.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The EventArgs instance containing the event data.</param>
        private async void button2_Click(object sender, EventArgs e)
        {
            // Updates the button text to indicate processing.
            button2.Text = "Processing...";
            // Disable the button to prevent multiple clicks
            button2.Enabled = false;
            _generatedFilePath = "";

            try
            {
                // Validates file paths.
                if (string.IsNullOrEmpty(ReportOutputLocation) || string.IsNullOrEmpty(excelCopytSaveLocation) || string.IsNullOrEmpty(excelCopyTemplateLocation))
                {
                    MessageBox.Show("File paths are invalid.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    button2.Text = "Error"; // Updates button text to indicate an error.
                    button2.Enabled = true; // Re-enables the button to allow the user to correct the error.
                    return; // Exits the method if file paths are invalid.
                }

                string sourceSheetName = "Sheet1";
                string destinationSheetName = "DATA";

                // Calls the method to copy data between Excel sheets and gets the generated file path.
                // Pass the SendEmailWithAttachment method as an Action.
                // Store the action, and the result
                Action<string> setEmailAction = SendEmailWithAttachment;
                Action<string> setTextAction = SetButton2Text;
                Action<string> setTextAction2 =  SetButton1Text;
                Action<bool> enableAction = EnableButton2;
                Action<bool> enableAction2 = EnableButton1;
                Action<bool> showAnalysisButtonAction = ShowBtnViewAnalysis;

                ExcelCopyData.CopyDataBetweenExcelSheetsAsync(checkBox1.Checked, ReportOutputLocation, sourceSheetName, excelCopytSaveLocation, excelCopyTemplateLocation, destinationSheetName, 0, 0, statusStrip1, setEmailAction, setTextAction, setTextAction2, enableAction, enableAction2, ShowBtnViewAnalysis); // Pass value
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

        // Action methods to update the UI.  These MUST be in the form.
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

#if DEBUG
                // Debug configuration: send emails to the me for testing.
                List<string> toAddresses = new List<string>
    {
      "chrisp@harlowsolutions.co.uk"
    };
                List<string> ccAddresses = new List<string>
    {
      "chrisp@harlowsolutions.co.uk"
    };
#else
        // Release configuration: send emails to the team.
        List<string> toAddresses = new List<string>
        {
            "andrewp@harlowsolutions.co.uk",
            "kirstym@harlowsolutions.co.uk",
            "stuartm@harlowsolutions.co.uk"
        };
        List<string> ccAddresses = new List<string>
        {
            "emmanuel@harlowsolutions.co.uk",
            "femi@harlowsolutions.co.uk",
            "jackh@harlowsolutions.co.uk",
            "pauls@harlowsolutions.co.uk",
            "ITdept@harlowsolutions.co.uk",
            "gordonb@harlowsolutions.co.uk"
        };
#endif

                string subject;
                string body;

                if (!checkBox1.Checked)
                {
                    subject = "Weekly Estimate Success Rate Report";
                    body = "Hi All,\r\n\r\nPlease see the \"Estimates Success Rate\" file attached with the list of quotes in the last two weeks for the entire team; please review them ahead of your respective check-ins for follow-ups required.\r\n\r\nThank you.\r\n";
                }
                else
                {
                    subject = "Monthly Estimate Success Rate Report";
                    body = $"Hi All,\r\n\r\nPlease see the \"Estimates Success Rate\" file attached with the list of quotes in for: {datepickFrom.Value.ToString("MMMMM yyyy")} for the entire team; please review them ahead of your respective check-ins for follow-ups required.\r\n\r\nThank you.\r\n";
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
                            statusStrip1.Invoke((MethodInvoker)delegate { statusStrip1.Items[0].Text = "Email Sent - Report Completed Please Close Program. "; });
                        }
                        else
                        {
                            MessageBox.Show($"Weekly report email failed: {error}");
                            Logger.LogError($"Weekly report email failed: {error}"); // Log detailed error.
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

        private void button3_Click(object sender, EventArgs e)
        {
            // This button's click event handler is empty.
        }

        /// <summary>
        /// Handles the click event of the "View Report" button.
        /// Opens the report file specified by ReportOutputLocation.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
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
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
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

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            //  The code block will be executed when the CheckBox's state changes (checked or unchecked).
            if (sender is CheckBox checkBox)
            {
                if (checkBox.Checked)
                {
                    // Calculate the first day of the current month.
                    DateTime firstDayOfMonth = new DateTime(today.Year, today.Month, 1);

                    // Determine if today is in the first half of the month.
                    if (today.Day <= 15)
                    {
                        // If in the first half, set the date to the 1st of the *previous* month.
                        DateTime firstDayOfLastMonth = firstDayOfMonth.AddMonths(-1);
                        datepickFrom.Value = firstDayOfLastMonth;
                        Logger.LogInfo("CheckBox1 is checked, Date From set to 1st of last month: " + firstDayOfLastMonth);
                    }
                    else
                    {
                        // Otherwise, set it to the 1st of the *current* month.
                        datepickFrom.Value = firstDayOfMonth;
                        Logger.LogInfo("CheckBox1 is checked, Date From set to 1st of current month: " + firstDayOfMonth);
                    }
                }
                else
                {
                    // If the checkbox is unchecked, set the date to the last 15 days.
                    DateTime fifteenDaysAgo = today.AddDays(-15);
                    datepickFrom.Value = fifteenDaysAgo;
                    Logger.LogInfo("CheckBox1 is unchecked, Date From set to last 15 days: " + fifteenDaysAgo);
                }
            }
            else
            {
                Logger.LogWarning("Sender is not a CheckBox");
            }
        }
    }
}
