// C# 10+ Features
// Ensure this namespace matches your project structure, e.g., QuoteConversionReportAutomation
namespace QuoteConversionReportAutomation
{
    using conversionTest; // Added to access the static Logger class
    // Required using directives
    using Microsoft.Extensions.Configuration;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq; // Added for Enumerable.Empty and Any()
    using System.Net;
    using System.Net.Mail;
    using System.Net.Mime; // Required for ContentType, MediaTypeNames
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Provides utility methods for sending emails asynchronously using configuration settings.
    /// Includes logging integration and reads attachments into memory to avoid file locks.
    /// </summary>
    public class EmailUtility // Class name is EmailUtility in the provided file
    {
        // Store configuration settings read from IConfiguration
        private readonly string _smtpServer;
        private readonly int _smtpPort;
        private readonly string _smtpUsername; // Used for authentication
        private readonly string _smtpPassword;
        private readonly string _fromAddress; // Actual From address
        private readonly string _fromDisplayName; // Display name for From address
        private readonly bool _enableSsl;

        /// <summary>
        /// Initializes a new instance of the <see cref="EmailUtility"/> class.
        /// Reads SMTP settings from the provided configuration.
        /// </summary>
        /// <param name="configuration">The application configuration instance.</param>
        /// <exception cref="ArgumentNullException">Thrown if configuration is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown if required configuration keys are missing or invalid.</exception>
        public EmailUtility(IConfiguration configuration)
        {
            ArgumentNullException.ThrowIfNull(configuration);

            _smtpServer = configuration["settings:SmtpServer"]
                ?? throw new InvalidOperationException("Configuration key 'settings:SmtpServer' is missing or empty.");

            string? smtpPortStr = configuration["settings:SmtpPort"];
            if (string.IsNullOrEmpty(smtpPortStr) || !int.TryParse(smtpPortStr, out _smtpPort))
            {
                Logger.LogError($"Invalid or missing SMTP Port configured: {smtpPortStr}. Must be an integer.");
                throw new InvalidOperationException($"Invalid or missing configuration key 'settings:SmtpPort': '{smtpPortStr}'. Must be an integer.");
            }

            _fromAddress = configuration["settings:FromAddress"]
                 ?? throw new InvalidOperationException("Configuration key 'settings:FromAddress' is missing or empty.");
            _smtpUsername = configuration["settings:SmtpUsername"]
                ?? throw new InvalidOperationException("Configuration key 'settings:SmtpUsername' is missing or empty.");

            _fromDisplayName = configuration["settings:FromDisplayName"] ?? "Automation Service";

            _smtpPassword = configuration["settings:SmtpPassword"] ?? string.Empty;
            if (string.IsNullOrEmpty(_smtpPassword))
            {
                Logger.LogWarning("Configuration key 'settings:SmtpPassword' is empty. Authentication might fail if required.");
            }

            if (!bool.TryParse(configuration["settings:EnableSsl"], out _enableSsl))
            {
                _enableSsl = true;
                Logger.LogWarning($"Configuration key 'settings:EnableSsl' is missing or invalid. Defaulting to true.");
            }

            Logger.LogInfo($"EmailUtility initialized: Server={_smtpServer}, Port={_smtpPort}, AuthUser={_smtpUsername}, From='{_fromDisplayName} <{_fromAddress}>', SSL={_enableSsl}");
        }

        /// <summary>
        /// Sends an email asynchronously with optional attachments.
        /// Uses SMTP settings read during initialization.
        /// Reads attachments into memory to avoid file locks.
        /// </summary>
        /// <param name="toAddresses">A list of email addresses to send the email to.</param>
        /// <param name="ccAddresses">A list of email addresses to CC on the email.</param>
        /// <param name="subject">The subject of the email.</param>
        /// <param name="body">The body of the email.</param>
        /// <param name="attachmentPath">The path to an optional attachment file.</param>
        /// <param name="progress">Optional progress reporter for status updates.</param>
        /// <param name="cancellationToken">Optional token to cancel the operation.</param>
        /// <returns>True if the email was sent successfully, false otherwise.</returns>
        public async Task<bool> SendEmailAsync(
            List<string> toAddresses,
            List<string> ccAddresses,
            string subject,
            string body,
            string? attachmentPath,
            IProgress<string>? progress = null,
            CancellationToken cancellationToken = default)
        {
            if (toAddresses == null || !toAddresses.Any(a => !string.IsNullOrWhiteSpace(a))) // Check if any valid 'To' address exists
            {
                Logger.LogError("Email sending failed: No valid 'To' recipients provided.");
                progress?.Report("Error: No recipients specified.");
                return false;
            }

            try
            {
                progress?.Report("Preparing email...");
                Logger.LogInfo("Preparing email...");
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
                Logger.LogDebug($"Recipients added. To: {string.Join(";", toAddresses)}, CC: {string.Join(";", ccAddresses ?? Enumerable.Empty<string>())}");

                if (!string.IsNullOrWhiteSpace(attachmentPath))
                {
                    Attachment? attachment = await AddAttachmentFromStreamAsync(attachmentPath, cancellationToken);
                    if (attachment != null)
                    {
                        mail.Attachments.Add(attachment);
                        Logger.LogDebug($"Attachment added: {attachmentPath}");
                    }
                    else
                    {
                        progress?.Report("Error: Failed to prepare attachment.");
                        return false;
                    }
                }
                else
                {
                    Logger.LogDebug("No attachment path provided.");
                }

                cancellationToken.ThrowIfCancellationRequested();
                progress?.Report("Connecting to SMTP server...");
                Logger.LogInfo($"Connecting to SMTP server: {_smtpServer}:{_smtpPort}");

                using var smtpClient = CreateSmtpClient();

                progress?.Report("Sending email...");
                Logger.LogInfo($"Attempting to send email. Subject: '{subject}'");

                await smtpClient.SendMailAsync(mail, cancellationToken);

                progress?.Report("Email sent successfully!");
                string ccString = (ccAddresses != null && ccAddresses.Any(a => !string.IsNullOrWhiteSpace(a)))
                                ? $", CC: {string.Join(";", ccAddresses.Where(a => !string.IsNullOrWhiteSpace(a)))}"
                                : string.Empty;
                Logger.LogInfo($"Email sent successfully to {string.Join(";", toAddresses.Where(a => !string.IsNullOrWhiteSpace(a)))}{ccString}. Subject: '{subject}'");
                return true;
            }
            catch (OperationCanceledException)
            {
                Logger.LogWarning("Email sending operation was cancelled.");
                progress?.Report("Email sending cancelled.");
                return false;
            }
            catch (FormatException fx)
            {
                Logger.LogError($"Email format error: {fx.Message}", fx);
                progress?.Report($"Error: Invalid email address format ({fx.Message}).");
                return false;
            }
            catch (FileNotFoundException fnfEx)
            {
                Logger.LogError($"Attachment error: {fnfEx.Message}", fnfEx);
                progress?.Report($"Error: Attachment file not found or accessible ({fnfEx.FileName}).");
                return false;
            }
            catch (SmtpException sx)
            {
                Logger.LogError($"SMTP error: {sx.Message} (StatusCode: {sx.StatusCode})", sx);
                progress?.Report($"Error: SMTP issue ({sx.StatusCode} - {sx.Message}).");
                return false;
            }
            catch (Exception ex)
            {
                Logger.LogCritical($"Unexpected error sending email: {ex.Message}", ex);
                progress?.Report($"Error: An unexpected issue occurred ({ex.Message}).");
                return false;
            }
        }

        private SmtpClient CreateSmtpClient()
        {
            var client = new SmtpClient(_smtpServer, _smtpPort)
            {
                EnableSsl = _enableSsl,
                Timeout = 30000,
            };

            if (!string.IsNullOrEmpty(_smtpUsername) && !string.IsNullOrEmpty(_smtpPassword))
            {
                client.Credentials = new NetworkCredential(_smtpUsername, _smtpPassword);
                Logger.LogDebug("Using provided SMTP credentials.");
            }
            else
            {
                Logger.LogDebug("No SMTP credentials provided, attempting anonymous/integrated auth.");
            }
            return client;
        }

        private static void AddRecipients(MailMessage mail, List<string>? addresses, MailMessageRecipientType recipientType)
        {
            if (addresses == null || !addresses.Any()) return;

            foreach (string address in addresses)
            {
                string trimmedAddress = address.Trim();
                if (string.IsNullOrWhiteSpace(trimmedAddress)) continue; // Skip empty or whitespace-only entries

                if (!IsValidEmail(trimmedAddress))
                {
                    Logger.LogWarning($"Invalid email address format skipped: {address}");
                    throw new FormatException($"Invalid email address format: {trimmedAddress}");
                }

                switch (recipientType)
                {
                    case MailMessageRecipientType.To:
                        mail.To.Add(trimmedAddress);
                        break;
                    case MailMessageRecipientType.CC:
                        mail.CC.Add(trimmedAddress);
                        break;
                }
            }
        }

        private async Task<Attachment?> AddAttachmentFromStreamAsync(string filePath, CancellationToken cancellationToken, int maxRetries = 3, int delayMs = 500)
        {
            Logger.LogDebug($"Attempting to read file into memory stream for attachment: {filePath}");
            byte[] fileBytes = [];
            bool fileReadSuccess = false;

            for (int i = 0; i < maxRetries; i++)
            {
                try
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    fileBytes = await File.ReadAllBytesAsync(filePath, cancellationToken);
                    fileReadSuccess = true;
                    Logger.LogDebug($"Successfully read {fileBytes.Length} bytes from {filePath}");
                    break;
                }
                catch (IOException ioEx) when (i < maxRetries - 1)
                {
                    Logger.LogWarning($"Attempt {i + 1} failed to read attachment file '{filePath}' due to IO error: {ioEx.Message}. Retrying in {delayMs}ms...");
                    await Task.Delay(delayMs, cancellationToken);
                }
                catch (IOException ioEx)
                {
                    Logger.LogError($"Failed to read attachment file '{filePath}' after {maxRetries} attempts: {ioEx.Message}", ioEx);
                    return null;
                }
                catch (OperationCanceledException)
                {
                    Logger.LogWarning("File read for attachment cancelled.");
                    throw;
                }
                catch (Exception ex)
                {
                    Logger.LogError($"Unexpected error reading attachment file '{filePath}': {ex.Message}", ex);
                    return null;
                }
            }

            if (!fileReadSuccess)
            {
                Logger.LogError($"Failed to read attachment file '{filePath}' after retries (fileReadSuccess is false).");
                return null;
            }

            try
            {
                var memoryStream = new MemoryStream(fileBytes);
                var contentType = new ContentType(MediaTypeNames.Application.Octet);
                string fileExtension = Path.GetExtension(filePath).ToLowerInvariant();
                if (fileExtension == ".xlsx") contentType = new ContentType("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
                else if (fileExtension == ".xls") contentType = new ContentType("application/vnd.ms-excel");

                var attachment = new Attachment(memoryStream, contentType)
                {
                    Name = Path.GetFileName(filePath)
                };
                Logger.LogDebug($"Created attachment '{attachment.Name}' from MemoryStream.");
                return attachment;
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error creating attachment from memory stream for file '{filePath}': {ex.Message}", ex);
                return null;
            }
        }

        public static bool IsValidEmail(string email) // Made public static for access from EmailRecipientManager
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;
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

        private enum MailMessageRecipientType
        {
            To,
            CC
        }
    }
}
