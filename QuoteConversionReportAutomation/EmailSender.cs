using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
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
        private readonly string _smtpServer;
        private readonly int _smtpPort;
        private readonly string _smtpUsername;
        private readonly string _smtpPassword;
        private readonly bool _enableSsl;

        /// <summary>
        /// Initializes a new instance of the <see cref="EmailUtility"/> class.
        /// </summary>
        public EmailUtility()
        {
            //see App.Config for the email settings.
            _smtpServer = ConfigurationManager.AppSettings["SmtpServer"];
            _smtpPort = int.Parse(ConfigurationManager.AppSettings["SmtpPort"] ?? "25"); // Provide a default value if missing
            _smtpUsername = ConfigurationManager.AppSettings["SmtpUsername"];
            _smtpPassword = ConfigurationManager.AppSettings["SmtpPassword"];
            _enableSsl = bool.Parse(ConfigurationManager.AppSettings["EnableSsl"] ?? "false"); // Default to false if not present.
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
                e.Result = SendEmailInternal(toAddresses, ccAddresses, subject, body, attachmentPath, worker);
            };

            worker.ProgressChanged += (sender, e) =>
            {
                progressCallback?.Invoke(e.UserState.ToString());
                UpdateStatusStrip(statusStrip, e.UserState.ToString());
            };

            worker.RunWorkerCompleted += (sender, e) =>
            {
                HandleCompletion(e.Result, completionCallback, statusStrip);
            };

            worker.RunWorkerAsync();
        }

        /// <summary>
        /// Sends the email message.
        /// </summary>
        /// <param name="toAddresses">List of recipient addresses.</param>
        /// <param name="ccAddresses">List of carbon copy recipient addresses.</param>
        /// <param name="subject">Email subject.</param>
        /// <param name="body">Email body.</param>
        /// <param name="attachmentPath">Path to the attachment file.</param>
        /// <param name="worker">BackgroundWorker instance for reporting progress.</param>
        /// <returns>Returns true on success, or an error message string on failure.</returns>
        private object SendEmailInternal(List<string> toAddresses, List<string> ccAddresses, string subject, string body, string attachmentPath, BackgroundWorker worker)
        {
            using (MailMessage mail = new MailMessage())
            {
                using (SmtpClient smtpClient = CreateSmtpClient())
                {
                    try
                    {
                        mail.From = new MailAddress(_smtpUsername);
                        AddRecipients(mail, toAddresses, MailMessageRecipientType.To);
                        AddRecipients(mail, ccAddresses, MailMessageRecipientType.CC);
                        mail.Subject = subject;
                        mail.Body = body;
                        AddAttachment(mail, attachmentPath);

                        worker.ReportProgress(50, "Sending email...");
                        smtpClient.Send(mail);
                        worker.ReportProgress(100, "Email sent successfully!");
                        return true;
                    }
                    catch (Exception ex)
                    {
                        return $"Error sending email: {ex.Message}";
                    }
                }
            }
        }

        /// <summary>
        /// Creates and configures an SmtpClient instance.
        /// </summary>
        /// <returns>Configured SmtpClient instance.</returns>
        private SmtpClient CreateSmtpClient()
        {
            return new SmtpClient(_smtpServer)
            {
                Port = _smtpPort,
                Credentials = new NetworkCredential(_smtpUsername, _smtpPassword),
                EnableSsl = _enableSsl,
                Timeout = 10000
            };
        }

        /// <summary>
        /// Adds recipients to the MailMessage object.
        /// </summary>
        /// <param name="mail">MailMessage instance.</param>
        /// <param name="addresses">List of email addresses.</param>
        /// <param name="recipientType">Type of recipient (To or CC).</param>
        private void AddRecipients(MailMessage mail, List<string> addresses, MailMessageRecipientType recipientType)
        {
            if (addresses == null || addresses.Count == 0) return;

            foreach (string address in addresses)
            {
                if (!IsValidEmail(address))
                    throw new FormatException($"Invalid email address: {address}");

                switch (recipientType)
                {
                    case MailMessageRecipientType.To:
                        mail.To.Add(address);
                        break;
                    case MailMessageRecipientType.CC:
                        mail.CC.Add(address);
                        break;
                }
            }
        }

        /// <summary>
        /// Adds an attachment to the MailMessage if the path is valid.
        /// </summary>
        /// <param name="mail">MailMessage instance.</param>
        /// <param name="attachmentPath">Path to the attachment file.</param>
        private void AddAttachment(MailMessage mail, string attachmentPath)
        {
            if (!string.IsNullOrEmpty(attachmentPath) && File.Exists(attachmentPath))
            {
                mail.Attachments.Add(new Attachment(attachmentPath));
            }
        }

        /// <summary>
        /// Handles the completion of the email sending operation.
        /// </summary>
        /// <param name="result">The result of the operation.</param>
        /// <param name="completionCallback">Callback to execute on completion.</param>
        /// <param name="statusStrip">StatusStrip to update.</param>
        private void HandleCompletion(object result, Action<bool, string> completionCallback, StatusStrip statusStrip)
        {
            bool success = result is bool boolResult && boolResult;
            string errorMessage = result is string stringResult ? stringResult : null;

            completionCallback?.Invoke(success, errorMessage);

            //since this is the very last thing include overall success message, fail message for failure
            UpdateStatusStrip(statusStrip, success ? "Email sent - Report Complete. " : "Email operation failed.");
        }

        /// <summary>
        /// Updates the StatusStrip with the provided message.
        /// </summary>
        /// <param name="statusStrip">The StatusStrip control to update.</param>
        /// <param name="message">The message to display.</param>
        private void UpdateStatusStrip(StatusStrip statusStrip, string message)
        {
            if (statusStrip != null)
                statusStrip.Items[0].Text = message;
        }

        /// <summary>
        /// Validates an email address using a regular expression.
        /// </summary>
        /// <param name="email">The email address to validate.</param>
        /// <returns>True if the email address is valid, false otherwise.</returns>
        private bool IsValidEmail(string email)
        {
            if (string.IsNullOrEmpty(email))
                return false;

            string pattern = @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$";
            return Regex.IsMatch(email, pattern);
            //TODO:  expand on this, check domain?
        }

        /// <summary>
        /// Represents the type of recipient for an email message.
        /// </summary>
        private enum MailMessageRecipientType
        {
            /// <summary>
            /// The recipient is on the "To" line.
            ///</summary>

      To,

            /// <summary>
            /// The recipient is on the "CC" line.
            ///</summary>

      CC
        }
    }
}
