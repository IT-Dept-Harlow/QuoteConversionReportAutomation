// EmailSender.cs
// Contains the EmailUtility class for sending emails and the EmailSendResult record.
// Configuration for SMTP, sender details, and attachment handling is read from
// appsettings.json using the new structured format.
// Utilises C# 10+ features.

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
using Microsoft.Extensions.Configuration; // For IConfiguration.

// Project specific namespaces
using QuoteConversionReportAutomation.Services.Logging; // For Logger.
// Note: The old namespace QuoteConversionReportAutomation.Helpers might have been where EmailUtility was.
// Assuming it's now intended to be in Services.Communication or a similar appropriate namespace.
// For this refactoring, I'll keep it in the namespace it was provided in: QuoteConversionReportAutomation.Helpers
#endregion

namespace QuoteConversionReportAutomation.Helpers // Or QuoteConversionReportAutomation.Services.Communication
{
    /// <summary>
    /// Represents the result of an email sending operation, providing success status and any error details.
    /// </summary>
    /// <param name="Success">True if the email was sent successfully; otherwise, false.</param>
    /// <param name="ErrorMessage">An optional error message if sending failed. This may include underlying exception messages.</param>
    /// <param name="SmtpErrorCode">An optional <see cref="System.Net.Mail.SmtpStatusCode"/> if an SMTP-specific error occurred.</param>
    public record EmailSendResult(bool Success, string? ErrorMessage = null, SmtpStatusCode? SmtpErrorCode = null);

    /// <summary>
    /// Provides utility methods for sending emails asynchronously for the QCRA application.
    /// This class reads SMTP server settings, sender details, and operational parameters (like timeouts, retries, attachment limits)
    /// from the application's configuration (`appsettings.json`).
    /// It supports sending emails with optional attachments and includes logging for all email operations.
    /// </summary>
    public class EmailUtility
    {
        #region Fields
        // SMTP Configuration
        private readonly string _smtpServer;
        private readonly int _smtpPort;
        private readonly string _smtpUsername;
        private readonly string _smtpPassword; // Consider encrypting this in configuration.
        private readonly bool _enableSsl;
        private readonly int _smtpTimeoutMs;
        private readonly int _smtpMaxSendRetries;
        private readonly int _smtpSendRetryDelayMs;

        // Sender & Email Configuration
        private readonly string _fromAddress;
        private readonly string _fromDisplayName;
        private readonly int _maxAttachmentSizeBytes;
        private readonly int _attachmentReadMaxRetries;
        private readonly int _attachmentReadDelayMs;
        #endregion

        #region Constructor
        /// <summary>
        /// Initializes a new instance of the <see cref="EmailUtility"/> class.
        /// Reads SMTP settings, sender details, and other email-related configurations
        /// from the provided <see cref="IConfiguration"/> instance, aligning with the new `appsettings.json` structure.
        /// </summary>
        /// <param name="configuration">The application configuration instance.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="configuration"/> is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown if essential configuration keys are missing, empty, or invalid (e.g., non-integer port).</exception>
        public EmailUtility(IConfiguration configuration)
        {
            ArgumentNullException.ThrowIfNull(configuration, nameof(configuration));
            Logger.LogTrace("Initializing EmailUtility...");

            // --- SMTP Server Configuration ---
            _smtpServer = configuration["SmtpConfiguration:Server"]
                ?? throw new InvalidOperationException("Configuration key 'SmtpConfiguration:Server' is missing or empty.");
            if (!int.TryParse(configuration["SmtpConfiguration:Port"], out _smtpPort))
            {
                throw new InvalidOperationException($"Invalid or missing configuration key 'SmtpConfiguration:Port'. Must be a valid integer. Value: '{configuration["SmtpConfiguration:Port"]}'");
            }
            _smtpUsername = configuration["SmtpConfiguration:Username"]
                ?? throw new InvalidOperationException("Configuration key 'SmtpConfiguration:Username' is missing or empty.");
            _smtpPassword = configuration["SmtpConfiguration:Password"] ?? string.Empty; // Password can be empty if auth method doesn't require it.
            if (string.IsNullOrEmpty(_smtpPassword))
            {
                Logger.LogWarning("Configuration key 'SmtpConfiguration:Password' is empty. SMTP authentication might fail if a password is required by the server.");
            }
            _enableSsl = configuration.GetValue<bool>("SmtpConfiguration:EnableSsl", false); // Default to false if not specified, as per new appsettings.json
            _smtpTimeoutMs = configuration.GetValue<int>("SmtpConfiguration:TimeoutMs", 30000); // Default 30 seconds
            _smtpMaxSendRetries = configuration.GetValue<int>("SmtpConfiguration:MaxSendRetries", 3); // Default 3 retries
            _smtpSendRetryDelayMs = configuration.GetValue<int>("SmtpConfiguration:SendRetryDelayMs", 2000); // Default 2 seconds

            // --- Sender and General Email Settings ---
            _fromAddress = configuration["EmailSettings:SenderAddress"]
                 ?? throw new InvalidOperationException("Configuration key 'EmailSettings:SenderAddress' is missing or empty.");
            if (!IsValidEmail(_fromAddress))
            {
                throw new InvalidOperationException($"Invalid email format for configuration key 'EmailSettings:SenderAddress': '{_fromAddress}'.");
            }
            _fromDisplayName = configuration["EmailSettings:SenderDisplayName"] ?? "QCRA Automation Service"; // Default display name
            _maxAttachmentSizeBytes = configuration.GetValue<int>("EmailSettings:MaxAttachmentSizeBytes", 10 * 1024 * 1024); // Default 10MB

            // --- Attachment Reading Settings ---
            _attachmentReadMaxRetries = configuration.GetValue<int>("EmailSettings:AttachmentReadMaxRetries", 3); // Default 3 retries
            _attachmentReadDelayMs = configuration.GetValue<int>("EmailSettings:AttachmentReadDelayMs", 500);    // Default 500ms delay

            Logger.LogInfo($"EmailUtility initialized: Server='{_smtpServer}', Port={_smtpPort}, User='{_smtpUsername}', From='{_fromDisplayName} <{_fromAddress}>', SSL={_enableSsl}, Timeout={_smtpTimeoutMs}ms, MaxAttach={_maxAttachmentSizeBytes}B, SendRetries={_smtpMaxSendRetries}, AttachReadRetries={_attachmentReadMaxRetries}");
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Sends an email asynchronously with enhanced error handling, retry logic for transient SMTP errors,
        /// configurable timeouts, attachment size limits, and detailed result reporting.
        /// </summary>
        /// <param name="toAddresses">A list of primary recipient email addresses ("To" field). Must not be null or empty.</param>
        /// <param name="ccAddresses">An optional list of carbon copy recipient email addresses ("CC" field).</param>
        /// <param name="subject">The subject line of the email.</param>
        /// <param name="body">The main content (body) of the email.</param>
        /// <param name="attachmentPath">The full file path to an optional attachment. If provided, the file must exist and be accessible.</param>
        /// <param name="progress">An optional <see cref="IProgress{T}"/> reporter for status updates during the send operation.</param>
        /// <param name="cancellationToken">An optional <see cref="CancellationToken"/> to observe for cancellation requests.</param>
        /// <returns>A <see cref="Task{TResult}"/> that represents the asynchronous operation. The task result is an <see cref="EmailSendResult"/> object
        /// indicating success or failure along with error details.</returns>
        public async Task<EmailSendResult> SendEmailAsync(
            List<string> toAddresses,
            List<string>? ccAddresses, // Made nullable as it's optional
            string subject,
            string body,
            string? attachmentPath,
            IProgress<string>? progress = null,
            CancellationToken cancellationToken = default)
        {
            // Validate essential 'To' recipients.
            if (toAddresses == null || !toAddresses.Any(a => !string.IsNullOrWhiteSpace(a)))
            {
                const string errorMsg = "No valid 'To' recipients provided for the email.";
                Logger.LogError($"Email sending failed: {errorMsg}");
                progress?.Report($"Error: {errorMsg}");
                return new EmailSendResult(false, errorMsg);
            }

            try
            {
                progress?.Report("Preparing email message...");
                Logger.LogInfo($"Preparing email message. Subject: '{subject}', To: {string.Join(";", toAddresses)}");
                cancellationToken.ThrowIfCancellationRequested();

                using var mail = new MailMessage
                {
                    From = new MailAddress(_fromAddress, _fromDisplayName),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = false // Assuming plain text body. Set to true if HTML.
                };

                // Add 'To' and 'CC' recipients, validating each address format.
                AddRecipients(mail, toAddresses, MailMessageRecipientType.To);
                AddRecipients(mail, ccAddresses, MailMessageRecipientType.CC); // Handles null ccAddresses gracefully.
                Logger.LogDebug($"Recipients added. To: {string.Join("; ", mail.To.Select(m => m.Address))}, CC: {string.Join("; ", mail.CC.Select(m => m.Address))}");

                // Handle attachment if path is provided.
                if (!string.IsNullOrWhiteSpace(attachmentPath))
                {
                    progress?.Report($"Preparing attachment: {Path.GetFileName(attachmentPath)}...");
                    var fileInfo = new FileInfo(attachmentPath);
                    if (!fileInfo.Exists) // Check if attachment file exists first.
                    {
                        string errorMsg = $"Attachment file not found: '{attachmentPath}'.";
                        Logger.LogError(errorMsg);
                        progress?.Report($"Error: {errorMsg}");
                        return new EmailSendResult(false, errorMsg);
                    }
                    if (fileInfo.Length > _maxAttachmentSizeBytes) // Check attachment size.
                    {
                        string errorMsg = $"Attachment '{Path.GetFileName(attachmentPath)}' ({fileInfo.Length} bytes) exceeds maximum allowed size of {_maxAttachmentSizeBytes} bytes.";
                        Logger.LogError(errorMsg);
                        progress?.Report($"Error: {errorMsg}");
                        return new EmailSendResult(false, errorMsg);
                    }

                    // Add attachment from stream with configured retries for file reading.
                    Attachment? attachment = await AddAttachmentFromStreamAsync(attachmentPath, cancellationToken, _attachmentReadMaxRetries, _attachmentReadDelayMs);
                    if (attachment != null)
                    {
                        mail.Attachments.Add(attachment); // Attachment will be disposed when MailMessage is disposed.
                        Logger.LogDebug($"Attachment added to email: {attachmentPath}");
                    }
                    else // Failed to prepare or read the attachment.
                    {
                        string errorMsg = $"Failed to prepare or read the attachment file: {Path.GetFileName(attachmentPath)}. Check logs for details.";
                        Logger.LogError(errorMsg); // AddAttachmentFromStreamAsync should log specifics.
                        progress?.Report($"Error: {errorMsg}");
                        return new EmailSendResult(false, errorMsg);
                    }
                }
                else
                {
                    Logger.LogDebug("No attachment path provided for the email.");
                }

                cancellationToken.ThrowIfCancellationRequested(); // Check for cancellation before attempting SMTP send.

                // Create SMTP client and send email with retry logic.
                using var smtpClient = CreateSmtpClient();
                bool emailSentSuccessfully = false;
                int currentRetryDelayMs = _smtpSendRetryDelayMs; // Initial delay for SMTP send retries.

                for (int attempt = 1; attempt <= _smtpMaxSendRetries; attempt++)
                {
                    try
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        progress?.Report($"Connecting to SMTP and sending email (Attempt {attempt}/{_smtpMaxSendRetries})...");
                        Logger.LogInfo($"Attempting to send email (Attempt {attempt}/{_smtpMaxSendRetries}). Subject: '{subject}'");

                        await smtpClient.SendMailAsync(mail, cancellationToken); // Send the email.
                        emailSentSuccessfully = true;
                        Logger.LogInfo($"Email send attempt {attempt} successful.");
                        break; // Success, exit retry loop.
                    }
                    catch (SmtpException sx) // Catch SMTP specific exceptions.
                    {
                        Logger.LogWarning($"SMTP send attempt {attempt} failed. StatusCode: {sx.StatusCode}, Message: {sx.Message}");
                        if (attempt == _smtpMaxSendRetries || !IsTransientSmtpError(sx.StatusCode))
                        {
                            // Non-transient error or last attempt, re-throw to be caught by the outer SmtpException catch block.
                            throw;
                        }
                        progress?.Report($"Email send attempt {attempt} failed (SMTP Error: {sx.StatusCode}). Retrying in {currentRetryDelayMs / 1000}s...");
                        await Task.Delay(currentRetryDelayMs, cancellationToken);
                        currentRetryDelayMs = Math.Min(currentRetryDelayMs * 2, 30000); // Exponential backoff, max 30s.
                    }
                } // End of retry loop.

                if (!emailSentSuccessfully)
                {
                    // This state implies all retries failed with transient errors, or the loop was exited for other reasons not leading to success.
                    // The actual error would have been re-thrown and caught by outer handlers.
                    // However, as a safeguard if logic changes, this ensures failure is reported.
                    string finalErrorMsg = $"Failed to send email after {_smtpMaxSendRetries} attempts due to persistent SMTP issues.";
                    Logger.LogError(finalErrorMsg);
                    return new EmailSendResult(false, finalErrorMsg, SmtpStatusCode.GeneralFailure);
                }

                progress?.Report("Email sent successfully!");
                Logger.LogInfo($"Email sent successfully. Subject: '{subject}'");
                return new EmailSendResult(true);
            }
            catch (OperationCanceledException)
            {
                Logger.LogWarning("Email sending operation was cancelled by request.");
                progress?.Report("Email sending cancelled.");
                return new EmailSendResult(false, "Email sending operation cancelled by request.");
            }
            catch (FormatException fx) // Invalid email address format.
            {
                Logger.LogError($"Email format error encountered: {fx.Message}", fx);
                progress?.Report($"Error: Invalid email address format ({fx.Message}).");
                return new EmailSendResult(false, $"Invalid email address format: {fx.Message}");
            }
            catch (SmtpException sx) // Non-transient SMTP errors or last retry failure.
            {
                Logger.LogError($"SMTP error occurred: {sx.Message} (StatusCode: {sx.StatusCode})", sx);
                progress?.Report($"Error: SMTP issue ({sx.StatusCode} - {sx.Message}).");
                return new EmailSendResult(false, $"SMTP error: {sx.Message}", sx.StatusCode);
            }
            catch (IOException ioEx) // Catch IO errors, likely related to attachment.
            {
                Logger.LogError($"IO error during email preparation (e.g., attachment): {ioEx.Message}", ioEx);
                progress?.Report($"Error: File operation failed ({ioEx.Message}).");
                return new EmailSendResult(false, $"File operation error: {ioEx.Message}");
            }
            catch (Exception ex) // Catch any other unexpected errors.
            {
                Logger.LogCritical($"Unexpected error occurred while sending email: {ex.Message}", ex);
                progress?.Report($"Error: An unexpected issue occurred ({ex.Message}).");
                return new EmailSendResult(false, $"Unexpected error: {ex.Message}");
            }
        }
        #endregion

        #region Private Helper Methods
        /// <summary>
        /// Creates and configures a new <see cref="SmtpClient"/> instance using settings
        /// loaded from the application configuration.
        /// </summary>
        /// <returns>A configured <see cref="SmtpClient"/> instance.</returns>
        private SmtpClient CreateSmtpClient()
        {
            var client = new SmtpClient(_smtpServer, _smtpPort)
            {
                EnableSsl = _enableSsl,
                Timeout = _smtpTimeoutMs, // Use configurable timeout.
            };

            // Set credentials if username and password are provided.
            if (!string.IsNullOrEmpty(_smtpUsername) && !string.IsNullOrEmpty(_smtpPassword))
            {
                client.Credentials = new NetworkCredential(_smtpUsername, _smtpPassword);
                Logger.LogDebug("SMTP client configured with provided credentials.");
            }
            else
            {
                Logger.LogDebug("No SMTP username/password provided; client will attempt default/anonymous authentication if supported by server.");
            }
            return client;
        }

        /// <summary>
        /// Adds a list of email addresses to the specified recipient collection of a <see cref="MailMessage"/>.
        /// Validates each email address format before adding.
        /// </summary>
        /// <param name="mail">The <see cref="MailMessage"/> to add recipients to.</param>
        /// <param name="addresses">A list of email address strings. Can be null or empty.</param>
        /// <param name="recipientType">The type of recipient (To, CC).</param>
        /// <exception cref="FormatException">Thrown if any email address in the list is invalid.</exception>
        private static void AddRecipients(MailMessage mail, List<string>? addresses, MailMessageRecipientType recipientType)
        {
            if (addresses == null || !addresses.Any()) return; // No addresses to add.

            foreach (string address in addresses)
            {
                string trimmedAddress = address.Trim();
                if (string.IsNullOrWhiteSpace(trimmedAddress)) continue; // Skip empty entries.

                if (!IsValidEmail(trimmedAddress)) // Validate format.
                {
                    string errorMsg = $"Invalid email address format: '{trimmedAddress}'. Cannot add to {recipientType} list.";
                    Logger.LogWarning(errorMsg);
                    throw new FormatException(errorMsg); // Throw to halt processing if an address is invalid.
                }

                // Add to the appropriate collection based on recipientType.
                switch (recipientType)
                {
                    case MailMessageRecipientType.To:
                        mail.To.Add(trimmedAddress);
                        break;
                    case MailMessageRecipientType.CC:
                        mail.CC.Add(trimmedAddress);
                        break;
                        // Bcc is not explicitly handled here but could be added.
                }
            }
        }

        /// <summary>
        /// Asynchronously reads an attachment file into a <see cref="MemoryStream"/> and creates an <see cref="Attachment"/> object.
        /// Includes retry logic for file reading, using configured retry attempts and delay.
        /// </summary>
        /// <param name="filePath">The full path to the attachment file.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <param name="maxRetries">The maximum number of retry attempts for reading the file.</param>
        /// <param name="initialDelayMs">The initial delay in milliseconds before the first retry.</param>
        /// <returns>An <see cref="Attachment"/> object if successful; otherwise, null.</returns>
        /// <exception cref="OperationCanceledException">Thrown if the operation is cancelled via the <paramref name="cancellationToken"/>.</exception>
        private async Task<Attachment?> AddAttachmentFromStreamAsync(string filePath, CancellationToken cancellationToken, int maxRetries, int initialDelayMs)
        {
            Logger.LogDebug($"Attempting to read attachment file into memory stream: '{filePath}' (MaxRetries: {maxRetries}, InitialDelay: {initialDelayMs}ms)");
            byte[] fileBytes = Array.Empty<byte>();
            bool fileReadSuccess = false;
            int currentDelayMs = initialDelayMs;

            for (int i = 0; i < maxRetries; i++)
            {
                try
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    fileBytes = await File.ReadAllBytesAsync(filePath, cancellationToken);
                    fileReadSuccess = true;
                    Logger.LogDebug($"Successfully read {fileBytes.Length} bytes from attachment file '{filePath}' on attempt {i + 1}.");
                    break; // Success, exit retry loop.
                }
                catch (IOException ioEx) when (i < maxRetries - 1) // Retry on IO errors if not the last attempt.
                {
                    Logger.LogWarning($"Attempt {i + 1}/{maxRetries} failed to read attachment file '{filePath}' (IO error): {ioEx.Message}. Retrying in {currentDelayMs}ms...");
                    await Task.Delay(currentDelayMs, cancellationToken);
                    currentDelayMs = Math.Min(currentDelayMs * 2, 10000); // Exponential backoff, capped at 10s.
                }
                catch (IOException ioEx) // Last attempt failed with IO error.
                {
                    Logger.LogError($"Failed to read attachment file '{filePath}' after {maxRetries} attempts (IO error): {ioEx.Message}", ioEx);
                    return null; // Indicate failure.
                }
                catch (OperationCanceledException) // Propagate cancellation.
                {
                    Logger.LogWarning($"File read operation for attachment '{filePath}' was cancelled.");
                    throw;
                }
                catch (Exception ex) // Catch other unexpected errors during file read.
                {
                    Logger.LogError($"Unexpected error reading attachment file '{filePath}' on attempt {i + 1}: {ex.Message}", ex);
                    if (i == maxRetries - 1) return null; // Fail on last attempt.
                    // For other attempts, could retry or fail immediately depending on severity.
                    // For now, let's assume only IO errors are retried for file reading.
                    return null; // Or throw to indicate a more severe, non-retriable read issue.
                }
            }

            if (!fileReadSuccess)
            {
                Logger.LogError($"Failed to read attachment file '{filePath}' after all retries (fileReadSuccess flag is false).");
                return null;
            }

            MemoryStream? memoryStream = null;
            try
            {
                memoryStream = new MemoryStream(fileBytes); // Create MemoryStream from the read bytes.
                string fileName = Path.GetFileName(filePath);
                string fileExtension = Path.GetExtension(filePath).ToLowerInvariant();

                // Determine ContentType based on file extension.
                ContentType contentType = fileExtension switch
                {
                    ".xlsx" => new ContentType("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"),
                    ".xls" => new ContentType("application/vnd.ms-excel"),
                    ".pdf" => new ContentType(MediaTypeNames.Application.Pdf),
                    ".txt" => new ContentType(MediaTypeNames.Text.Plain),
                    ".csv" => new ContentType("text/csv"), // Common CSV MIME type
                    _ => new ContentType(MediaTypeNames.Application.Octet) // Default binary stream.
                };
                contentType.Name = fileName; // Set the name in ContentType for better email client display.

                var attachment = new Attachment(memoryStream, contentType)
                {
                    Name = fileName // Also set Name property of Attachment.
                };

                // The Attachment object now owns the memoryStream. Set local ref to null
                // to prevent disposal in the finally block if attachment creation was successful.
                memoryStream = null;
                Logger.LogDebug($"Created attachment '{attachment.Name}' from MemoryStream with ContentType '{contentType.MediaType}'.");
                return attachment; // The MailMessage will dispose this attachment, which in turn disposes the stream.
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error creating Attachment object from memory stream for file '{filePath}': {ex.Message}", ex);
                return null; // memoryStream will be disposed by the finally block if it was created.
            }
            finally
            {
                // Dispose the memoryStream only if it wasn't successfully passed to an Attachment
                // (i.e., if memoryStream is not null here, an error occurred after its creation
                // but before the Attachment took ownership, or Attachment creation itself failed).
                memoryStream?.Dispose();
            }
        }

        /// <summary>
        /// Determines if an <see cref="SmtpStatusCode"/> represents a potentially transient error
        /// for which a retry might be successful.
        /// </summary>
        /// <param name="statusCode">The <see cref="SmtpStatusCode"/> to check.</param>
        /// <returns>True if the status code suggests a transient error; otherwise, false.</returns>
        private bool IsTransientSmtpError(SmtpStatusCode statusCode)
        {
            // List of SMTP status codes often considered transient.
            // This list might need adjustment based on specific SMTP server behavior.
            switch (statusCode)
            {
                case SmtpStatusCode.ServiceNotAvailable:        // 421 (Service not available, closing transmission channel)
                                                                // 451 (Requested action aborted: local error in processing)
                                                                // 452 (Requested action not taken: insufficient system storage)
                case SmtpStatusCode.MailboxBusy:                // 450 (Requested mail action not taken: mailbox unavailable (e.g., mailbox busy))
                case SmtpStatusCode.MailboxUnavailable:         // 550 (Can be permanent if user doesn't exist, but sometimes temporary e.g., mailbox full or temp policy block)
                                                                // Retrying 550 can be risky if it's for a non-existent user, but sometimes it's due to temporary greylisting.
                                                                // For this app, we'll consider it potentially transient for retry.
                case SmtpStatusCode.TransactionFailed:          // 554 (Often indicates temporary issues like spam filter or relay problems)
                case SmtpStatusCode.ExceededStorageAllocation:  // 452, 552 (Requested mail action aborted: exceeded storage allocation)
                case SmtpStatusCode.GeneralFailure:             // 451 (Often used for unspecified temporary server issues by some servers)
                                                                // Add other codes that might be considered transient based on experience with the specific SMTP server.
                                                                // Examples:
                                                                // SmtpStatusCode.InsufficientStorage (452) - already covered by ExceededStorageAllocation or ServiceNotAvailable
                                                                // SmtpStatusCode.ClientNotPermitted (454) - usually temporary if due to rate limiting
                                                                // SmtpStatusCode.LocalErrorInProcessing (451) - already covered by ServiceNotAvailable or GeneralFailure
                    Logger.LogDebug($"SMTP StatusCode {statusCode} considered transient for retry.");
                    return true;
                default:
                    Logger.LogDebug($"SMTP StatusCode {statusCode} considered non-transient.");
                    return false;
            }
        }

        /// <summary>
        /// Validates if the given string is a syntactically valid email address.
        /// </summary>
        /// <param name="email">The email string to validate.</param>
        /// <returns>True if the email address is valid; otherwise, false.</returns>
        public static bool IsValidEmail(string? email) // Made email nullable
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;
            try
            {
                // Use System.Net.Mail.MailAddress for validation.
                // This constructor throws FormatException if the address is invalid.
                _ = new MailAddress(email);
                return true;
            }
            catch (FormatException)
            {
                return false; // Invalid format.
            }
            catch (ArgumentException) // Catch ArgumentException for empty string after potential trim, though IsNullOrWhiteSpace handles it.
            {
                return false;
            }
        }

        /// <summary>
        /// Private enum to specify the type of recipient when adding to a MailMessage.
        /// This is used internally by the AddRecipients method.
        /// </summary>
        private enum MailMessageRecipientType
        {
            To,
            CC
            // Bcc could be added if needed.
        }
        #endregion
    }
}