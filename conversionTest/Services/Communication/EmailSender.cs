// EmailSender.cs
// Contains the EmailUtility class for sending emails.
// This version is fully refactored to use the IStatusManagerService for all progress reporting,
// removing the need for IProgress<T> parameters.

#region Using Directives
// System related namespaces
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Net.Mime; // Required for MediaTypeNames
using System.Threading;
using System.Threading.Tasks;

// Third-party namespaces
using Microsoft.Extensions.Configuration;

// Project specific namespaces
using QuoteConversionReportAutomation.Interfaces; // For IStatusManagerService
using QuoteConversionReportAutomation.Models.Status; // For MessageType
using QuoteConversionReportAutomation.Services.Logging;
#endregion

namespace QuoteConversionReportAutomation.Services.Communication
{
    /// <summary>
    /// Represents the result of an email sending operation, providing success status and any error details.
    /// </summary>
    /// <param name="Success">True if the email was sent successfully; otherwise, false.</param>
    /// <param name="ErrorMessage">An optional error message if sending failed. This may include underlying exception messages.</param>
    /// <param name="SmtpErrorCode">An optional <see cref="SmtpStatusCode"/> if an SMTP-specific error occurred.</param>
    public record EmailSendResult(bool Success, string? ErrorMessage = null, SmtpStatusCode? SmtpErrorCode = null);

    /// <summary>
    /// Provides utility methods for sending emails asynchronously for the QCRA application.
    /// This class reads SMTP server settings and sender details from the application's configuration.
    /// It reports its progress and status via the injected <see cref="IStatusManagerService"/>.
    /// </summary>
    public class EmailUtility
    {
        #region Fields
        private readonly IStatusManagerService _statusManager;
        private readonly string _smtpServer;
        private readonly int _smtpPort;
        private readonly string _smtpUsername;
        private readonly string _smtpPassword;
        private readonly bool _enableSsl;
        private readonly int _smtpTimeoutMs;
        private readonly int _smtpMaxSendRetries;
        private readonly int _smtpSendRetryDelayMs;
        private readonly string _fromAddress;
        private readonly string _fromDisplayName;
        private readonly int _maxAttachmentSizeBytes;
        private readonly int _attachmentReadMaxRetries;
        private readonly int _attachmentReadDelayMs;
        #endregion

        #region Constructor
        /// <summary>
        /// Initializes a new instance of the <see cref="EmailUtility"/> class.
        /// Reads SMTP settings and other email-related configurations from the IConfiguration instance.
        /// </summary>
        /// <param name="configuration">The application configuration instance.</param>
        /// <param name="statusManager">The centralised service for status reporting.</param>
        /// <exception cref="ArgumentNullException">Thrown if configuration or statusManager is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown if essential configuration keys are missing or invalid.</exception>
        public EmailUtility(IConfiguration configuration, IStatusManagerService statusManager)
        {
            ArgumentNullException.ThrowIfNull(configuration, nameof(configuration));
            _statusManager = statusManager ?? throw new ArgumentNullException(nameof(statusManager)); // Inject and store the status manager.
            Logger.LogTrace("Initializing EmailUtility...");

            // --- SMTP Server Configuration ---
            _smtpServer = configuration["SmtpConfiguration:Server"] ?? throw new InvalidOperationException("Config key 'SmtpConfiguration:Server' is missing.");
            if (!int.TryParse(configuration["SmtpConfiguration:Port"], out _smtpPort)) throw new InvalidOperationException($"Invalid or missing config key 'SmtpConfiguration:Port'.");
            _smtpUsername = configuration["SmtpConfiguration:Username"] ?? throw new InvalidOperationException("Config key 'SmtpConfiguration:Username' is missing.");
            _smtpPassword = configuration["SmtpConfiguration:Password"] ?? string.Empty;
            _enableSsl = configuration.GetValue("SmtpConfiguration:EnableSsl", false);
            _smtpTimeoutMs = configuration.GetValue("SmtpConfiguration:TimeoutMs", 30000);
            _smtpMaxSendRetries = configuration.GetValue("SmtpConfiguration:MaxSendRetries", 3);
            _smtpSendRetryDelayMs = configuration.GetValue("SmtpConfiguration:SendRetryDelayMs", 2000);

            // --- Sender and General Email Settings ---
            _fromAddress = configuration["EmailSettings:SenderAddress"] ?? throw new InvalidOperationException("Config key 'EmailSettings:SenderAddress' is missing.");
            if (!IsValidEmail(_fromAddress)) throw new InvalidOperationException($"Invalid email format for 'EmailSettings:SenderAddress': '{_fromAddress}'.");
            _fromDisplayName = configuration["EmailSettings:SenderDisplayName"] ?? "QCRA Automation Service";
            _maxAttachmentSizeBytes = configuration.GetValue("EmailSettings:MaxAttachmentSizeBytes", 10 * 1024 * 1024);

            // --- Attachment Reading Settings ---
            _attachmentReadMaxRetries = configuration.GetValue("EmailSettings:AttachmentReadMaxRetries", 3);
            _attachmentReadDelayMs = configuration.GetValue("EmailSettings:AttachmentReadDelayMs", 500);

            Logger.LogInfo("EmailUtility initialized successfully.");
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Sends an email asynchronously with enhanced error handling and retry logic.
        /// Progress and status are reported via the injected <see cref="IStatusManagerService"/>.
        /// </summary>
        /// <param name="toAddresses">A list of primary recipient email addresses ("To" field).</param>
        /// <param name="ccAddresses">An optional list of carbon copy recipient email addresses ("CC" field).</param>
        /// <param name="subject">The subject line of the email.</param>
        /// <param name="body">The main content (body) of the email.</param>
        /// <param name="attachmentPath">The full file path to an optional attachment.</param>
        /// <param name="cancellationToken">An optional <see cref="CancellationToken"/> to observe for cancellation requests.</param>
        /// <returns>A <see cref="Task{TResult}"/> whose result is an <see cref="EmailSendResult"/> object.</returns>
        public async Task<EmailSendResult> SendEmailAsync(
            List<string> toAddresses,
            List<string>? ccAddresses,
            string subject,
            string body,
            string? attachmentPath,
            CancellationToken cancellationToken = default)
        {
            if (toAddresses == null || !toAddresses.Any(a => !string.IsNullOrWhiteSpace(a)))
            {
                const string errorMsg = "No valid 'To' recipients provided for the email.";
                _statusManager.Post(errorMsg, MessageType.Error);
                return new EmailSendResult(false, errorMsg);
            }

            try
            {
                _statusManager.Post("Preparing email message...", MessageType.InProgress);
                cancellationToken.ThrowIfCancellationRequested();

                using var mail = new MailMessage
                {
                    From = new MailAddress(_fromAddress, _fromDisplayName),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = false
                };

                AddRecipients(mail, toAddresses, MailMessageRecipientType.To);
                AddRecipients(mail, ccAddresses, MailMessageRecipientType.CC);

                if (!string.IsNullOrWhiteSpace(attachmentPath))
                {
                    _statusManager.Post($"Preparing attachment: {Path.GetFileName(attachmentPath)}...", MessageType.InProgress);
                    var fileInfo = new FileInfo(attachmentPath);
                    if (!fileInfo.Exists)
                    {
                        return new EmailSendResult(false, $"Attachment file not found: '{attachmentPath}'.");
                    }
                    if (fileInfo.Length > _maxAttachmentSizeBytes)
                    {
                        return new EmailSendResult(false, $"Attachment '{Path.GetFileName(attachmentPath)}' exceeds maximum size of {_maxAttachmentSizeBytes} bytes.");
                    }
                    Attachment? attachment = await AddAttachmentFromStreamAsync(attachmentPath, cancellationToken);
                    if (attachment != null)
                    {
                        mail.Attachments.Add(attachment);
                    }
                    else
                    {
                        return new EmailSendResult(false, $"Failed to prepare or read the attachment file: {Path.GetFileName(attachmentPath)}.");
                    }
                }

                cancellationToken.ThrowIfCancellationRequested();
                using var smtpClient = CreateSmtpClient();
                int currentRetryDelayMs = _smtpSendRetryDelayMs;

                for (int attempt = 1; attempt <= _smtpMaxSendRetries; attempt++)
                {
                    try
                    {
                        _statusManager.Post($"Sending email (Attempt {attempt}/{_smtpMaxSendRetries})...", MessageType.InProgress);
                        await smtpClient.SendMailAsync(mail, cancellationToken);
                        _statusManager.Post("Email sent successfully!", MessageType.Success, TimeSpan.FromSeconds(5));
                        return new EmailSendResult(true);
                    }
                    catch (SmtpException sx) when (attempt < _smtpMaxSendRetries && IsTransientSmtpError(sx.StatusCode))
                    {
                        _statusManager.Post($"Email failed (SMTP Error: {sx.StatusCode}). Retrying...", MessageType.Warning);
                        await Task.Delay(currentRetryDelayMs, cancellationToken);
                        currentRetryDelayMs *= 2;
                    }
                }

                // If the loop finishes without returning, it means all retries have failed.
                throw new SmtpException($"Failed to send email after {_smtpMaxSendRetries} attempts.");
            }
            catch (OperationCanceledException)
            {
                _statusManager.Post("Email sending cancelled.", MessageType.Warning, TimeSpan.FromSeconds(5));
                return new EmailSendResult(false, "Email sending operation cancelled by request.");
            }
            catch (FormatException fx)
            {
                _statusManager.Post($"Error: Invalid email address format ({fx.Message}).", MessageType.Error);
                return new EmailSendResult(false, $"Invalid email address format: {fx.Message}");
            }
            catch (SmtpException sx)
            {
                _statusManager.Post($"Error: SMTP issue ({sx.StatusCode} - {sx.Message}).", MessageType.Error);
                return new EmailSendResult(false, $"SMTP error: {sx.Message}", sx.StatusCode);
            }
            catch (Exception ex)
            {
                _statusManager.Post($"Error: An unexpected issue occurred ({ex.Message}).", MessageType.Error);
                return new EmailSendResult(false, $"Unexpected error: {ex.Message}");
            }
        }
        #endregion

        #region Private Helper Methods
        /// <summary>
        /// Creates and configures a new SmtpClient instance.
        /// </summary>
        private SmtpClient CreateSmtpClient()
        {
            var client = new SmtpClient(_smtpServer, _smtpPort)
            {
                EnableSsl = _enableSsl,
                Timeout = _smtpTimeoutMs,
            };
            if (!string.IsNullOrEmpty(_smtpUsername) && !string.IsNullOrEmpty(_smtpPassword))
            {
                client.Credentials = new NetworkCredential(_smtpUsername, _smtpPassword);
            }
            return client;
        }

        /// <summary>
        /// Adds a list of email addresses to the MailMessage.
        /// </summary>
        private static void AddRecipients(MailMessage mail, List<string>? addresses, MailMessageRecipientType recipientType)
        {
            if (addresses == null) return;
            foreach (string address in addresses)
            {
                if (string.IsNullOrWhiteSpace(address)) continue;
                if (!IsValidEmail(address)) throw new FormatException($"Invalid email address format: '{address}'.");
                switch (recipientType)
                {
                    case MailMessageRecipientType.To: mail.To.Add(address); break;
                    case MailMessageRecipientType.CC: mail.CC.Add(address); break;
                }
            }
        }

        /// <summary>
        /// Asynchronously reads an attachment file into an Attachment object with retry logic.
        /// </summary>
        private async Task<Attachment?> AddAttachmentFromStreamAsync(string filePath, CancellationToken cancellationToken)
        {
            int currentDelayMs = _attachmentReadDelayMs;
            for (int i = 0; i < _attachmentReadMaxRetries; i++)
            {
                try
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    byte[] fileBytes = await File.ReadAllBytesAsync(filePath, cancellationToken);
                    var memoryStream = new MemoryStream(fileBytes);
                    string fileName = Path.GetFileName(filePath);
                    var contentType = new ContentType(MediaTypeNames.Application.Octet) { Name = fileName };
                    return new Attachment(memoryStream, contentType);
                }
                catch (IOException) when (i < _attachmentReadMaxRetries - 1)
                {
                    await Task.Delay(currentDelayMs, cancellationToken);
                    currentDelayMs *= 2;
                }
            }
            return null;
        }

        /// <summary>
        /// Determines if an SmtpStatusCode represents a transient error suitable for retry.
        /// </summary>
        private bool IsTransientSmtpError(SmtpStatusCode statusCode)
        {
            return statusCode switch
            {
                SmtpStatusCode.ServiceNotAvailable or
                SmtpStatusCode.MailboxBusy or
                SmtpStatusCode.MailboxUnavailable or
                SmtpStatusCode.TransactionFailed or
                SmtpStatusCode.ExceededStorageAllocation or
                SmtpStatusCode.GeneralFailure => true,
                _ => false,
            };
        }

        /// <summary>
        /// Validates if the given string is a syntactically valid email address.
        /// </summary>
        public static bool IsValidEmail(string? email)
        {
            if (string.IsNullOrWhiteSpace(email)) return false;
            try
            {
                _ = new MailAddress(email);
                return true;
            }
            catch (FormatException)
            {
                return false;
            }
        }

        private enum MailMessageRecipientType { To, CC }
        #endregion
    }
}