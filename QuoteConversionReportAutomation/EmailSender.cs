using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace EmailSender
{
    /// <summary>
    /// Provides utility methods for sending emails with attachments.
    /// </summary>
    public class EmailUtility
    {
        /// <summary>
        /// Gets or sets the SMTP server address.
        /// </summary>
        public string SmtpServer { get; set; }

        /// <summary>
        /// Gets or sets the SMTP port number.
        /// </summary>
        public int SmtpPort { get; set; }

        /// <summary>
        /// Gets or sets the username for SMTP authentication.
        /// </summary>
        public string SmtpUsername { get; set; }

        /// <summary>
        /// Gets or sets the password for SMTP authentication.
        /// </summary>
        public string SmtpPassword { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether SSL is enabled for the SMTP connection.
        /// </summary>
        public bool EnableSsl { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="EmailUtility"/> class.
        /// </summary>
        /// <param name="smtpServer">The SMTP server address.</param>
        /// <param name="smtpPort">The SMTP port number.</param>
        /// <param name="smtpUsername">The username for SMTP authentication.</param>
        /// <param name="smtpPassword">The password for SMTP authentication.</param>
        /// <param name="enableSsl">A value indicating whether SSL is enabled for the SMTP connection.</param>
        public EmailUtility(string smtpServer, int smtpPort, string smtpUsername, string smtpPassword, bool enableSsl)
        {
            SmtpServer = smtpServer;
            SmtpPort = smtpPort;
            SmtpUsername = smtpUsername;
            SmtpPassword = smtpPassword;
            EnableSsl = enableSsl;
        }

        /// <summary>
        /// Sends an email with optional attachments, providing progress and completion callbacks, and updating a StatusStrip.
        /// </summary>
        /// <param name="toAddresses">A list of email addresses to send the email to.</param>
        /// <param name="ccAddresses">A list of email addresses to CC on the email.</param>
        /// <param name="subject">The subject of the email.</param>
        /// <param name="body">The body of the email.</param>
        /// <param name="attachmentPath">The path to an optional attachment file.</param>
        /// <param name="progressCallback">An optional callback function to report progress updates.</param>
        /// <param name="completionCallback">An optional callback function to report completion status and any errors.</param>
        /// <param name="statusStrip">The StatusStrip to update with progress information.</param>
        public void SendEmail(List<string> toAddresses, List<string> ccAddresses, string subject, string body, string attachmentPath, Action<string> progressCallback, Action<bool, string> completionCallback, StatusStrip statusStrip)
        {
            BackgroundWorker worker = new BackgroundWorker { WorkerReportsProgress = true };

            worker.DoWork += (sender, e) =>
            {
                MailMessage mail = null;
                SmtpClient smtpClient = null;

                try
                {
                    //create new mail client
                    mail = new MailMessage();
                    smtpClient = new SmtpClient(SmtpServer)
                    {
                        Port = SmtpPort,
                        Credentials = new NetworkCredential(SmtpUsername, SmtpPassword),
                        EnableSsl = EnableSsl,
                        Timeout = 10000
                    };

                    mail.From = new MailAddress(SmtpUsername);

                    //checks each address to make sure it's valid
                    foreach (string address in toAddresses)
                    {
                        if (IsValidEmail(address))
                        {
                            mail.To.Add(address);
                        }
                        else
                        {
                            throw new Exception($"Invalid To email address: {address}");
                        }
                    }

                    if (ccAddresses != null)
                    {
                        foreach (string address in ccAddresses)
                        {
                            if (IsValidEmail(address))
                            {
                                mail.CC.Add(address);
                            }
                            else
                            {
                                throw new Exception($"Invalid CC email address: {address}");
                            }
                        }
                    }

                    //set up email, passed from main form
                    mail.Subject = subject;
                    mail.Body = body;

                    //if there is an attachment add it to the email
                    if (!string.IsNullOrEmpty(attachmentPath) && File.Exists(attachmentPath))
                    {
                        mail.Attachments.Add(new Attachment(attachmentPath));
                    }

                    worker.ReportProgress(50, "Sending email...");
                    smtpClient.Send(mail);
                    worker.ReportProgress(100, "Email sent successfully!");
                    e.Result = true;
                }
                catch (Exception ex)
                {
                    e.Result = ex.Message;
                    worker.ReportProgress(100, $"Error sending email: {ex.Message}");
                }
                finally
                {
                    mail?.Dispose();
                    smtpClient?.Dispose();
                }
            };

            worker.ProgressChanged += (sender, e) =>
            {
                progressCallback?.Invoke(e.UserState.ToString());
                if (statusStrip != null) // Avoid null exception
                {
                    statusStrip.Items[0].Text = e.UserState.ToString(); // Update the first item in the StatusStrip
                }

            };

            worker.RunWorkerCompleted += (sender, e) =>
            {
                bool success = e.Result is bool eResult && eResult;
                string errorMessage = e.Result is string eResult2 ? eResult2 : null;
                completionCallback?.Invoke(success, errorMessage);
                if (statusStrip != null)
                {
                    //since this is the very last thing include overall success message, fail message for failure
                    statusStrip.Items[0].Text = success ? "Email sent - Report Complete" : "Email operation failed.";
                }

            };

            worker.RunWorkerAsync();
        }

        //checks to make sure the emails are in a valid format, TODO: expand on this?
        private bool IsValidEmail(string email)
        {
            string pattern = @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$";
            return Regex.IsMatch(email, pattern);
        }
    }
}
