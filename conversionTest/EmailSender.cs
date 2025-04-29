// C# 10+ Features
namespace QuoteConversionReportAutomation
{
    using conversionTest; // Added to access the static Logger class
    // Required using directives
    using Microsoft.Extensions.Configuration;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net;
    using System.Net.Mail;
    using System.Net.Mime; // Required for ContentType, MediaTypeNames
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Provides utility methods for sending emails asynchronously using configuration settings.
    /// Includes logging integration and reads attachments into memory to avoid file locks.
    /// </summary>
    public class EmailUtility
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

            // Read settings using the "settings:" prefix convention
            _smtpServer = configuration["settings:SmtpServer"]
                ?? throw new InvalidOperationException("Configuration key 'settings:SmtpServer' is missing or empty.");

            string? smtpPortStr = configuration["settings:SmtpPort"];
            if (string.IsNullOrEmpty(smtpPortStr) || !int.TryParse(smtpPortStr, out _smtpPort))
            {
                Logger.LogError($"Invalid or missing SMTP Port configured: {smtpPortStr}. Must be an integer."); // Use Logger
                throw new InvalidOperationException($"Invalid or missing configuration key 'settings:SmtpPort': '{smtpPortStr}'. Must be an integer.");
            }

            // Separate From Address and Auth Username
            _fromAddress = configuration["settings:FromAddress"]
                 ?? throw new InvalidOperationException("Configuration key 'settings:FromAddress' is missing or empty.");
            _smtpUsername = configuration["settings:SmtpUsername"] // Username for auth
                ?? throw new InvalidOperationException("Configuration key 'settings:SmtpUsername' is missing or empty.");

            // *** ADDED: Read From Display Name ***
            _fromDisplayName = configuration["settings:FromDisplayName"] ?? "Automation Service"; // Default if missing

            _smtpPassword = configuration["settings:SmtpPassword"] ?? string.Empty; // Allow empty password if server permits
            if (string.IsNullOrEmpty(_smtpPassword))
            {
                Logger.LogWarning("Configuration key 'settings:SmtpPassword' is empty. Authentication might fail if required."); // Use Logger
            }

            // Parse EnableSsl, defaulting to true if missing or invalid (common practice)
            if (!bool.TryParse(configuration["settings:EnableSsl"], out _enableSsl))
            {
                _enableSsl = true; // Default value changed to true
                Logger.LogWarning($"Configuration key 'settings:EnableSsl' is missing or invalid. Defaulting to true."); // Use Logger
            }

            // Log configuration details (excluding password)
            Logger.LogInfo($"EmailUtility initialized: Server={_smtpServer}, Port={_smtpPort}, AuthUser={_smtpUsername}, From='{_fromDisplayName} <{_fromAddress}>', SSL={_enableSsl}"); // Use Logger
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
            string? attachmentPath, // Make attachment path nullable
            IProgress<string>? progress = null, // Keep progress for UI updates
            CancellationToken cancellationToken = default)
        {
            // Basic validation
            if (toAddresses == null || toAddresses.Count == 0)
            {
                Logger.LogError("Email sending failed: No 'To' recipients provided."); // Use Logger
                progress?.Report("Error: No recipients specified.");
                return false;
            }

            try
            {
                progress?.Report("Preparing email...");
                Logger.LogInfo("Preparing email..."); // Use Logger
                cancellationToken.ThrowIfCancellationRequested();

                // Create MailMessage (implements IDisposable)
                using var mail = new MailMessage
                {
                    // *** UPDATED: Use From Address and Display Name ***
                    From = new MailAddress(_fromAddress, _fromDisplayName),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = false // Set to true if body contains HTML
                };

                // Add recipients (validation happens inside AddRecipients)
                AddRecipients(mail, toAddresses, MailMessageRecipientType.To);
                AddRecipients(mail, ccAddresses, MailMessageRecipientType.CC); // Handles null/empty list
                Logger.LogDebug($"Recipients added. To: {string.Join(";", toAddresses)}, CC: {string.Join(";", ccAddresses ?? [])}"); // Use Logger

                // *** FIX: Add attachment from memory stream ***
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
                        // Error logged in AddAttachmentFromStreamAsync
                        progress?.Report("Error: Failed to prepare attachment.");
                        return false; // Fail if attachment requested but couldn't be prepared
                    }
                    // Attachment will be disposed when 'mail' (MailMessage) is disposed
                }
                else
                {
                    Logger.LogDebug("No attachment path provided.");
                }
                // *** End Fix ***

                cancellationToken.ThrowIfCancellationRequested();
                progress?.Report("Connecting to SMTP server...");
                Logger.LogInfo($"Connecting to SMTP server: {_smtpServer}:{_smtpPort}"); // Use Logger

                // Create SmtpClient (implements IDisposable)
                using var smtpClient = CreateSmtpClient(); // Create client using configured settings

                progress?.Report("Sending email...");
                Logger.LogInfo($"Attempting to send email. Subject: '{subject}'"); // Use Logger

                // Send the email asynchronously
                await smtpClient.SendMailAsync(mail, cancellationToken);

                progress?.Report("Email sent successfully!");
                Logger.LogInfo($"Email sent successfully to {string.Join(";", toAddresses)}. Subject: '{subject}'"); // Use Logger
                return true; // Indicate success
            }
            catch (OperationCanceledException)
            {
                Logger.LogWarning("Email sending operation was cancelled."); // Use Logger
                progress?.Report("Email sending cancelled.");
                return false;
            }
            catch (FormatException fx) // Catch specific format errors (e.g., invalid email)
            {
                Logger.LogError($"Email format error: {fx.Message}", fx); // Use Logger, include exception
                progress?.Report($"Error: Invalid email address format ({fx.Message}).");
                return false; // Return failure
            }
            catch (FileNotFoundException fnfEx) // Catch attachment errors (less likely with stream method, but possible during read)
            {
                Logger.LogError($"Attachment error: {fnfEx.Message}", fnfEx); // Use Logger, include exception
                progress?.Report($"Error: Attachment file not found or accessible ({fnfEx.FileName}).");
                return false;
            }
            catch (SmtpException sx) // Catch SMTP specific errors
            {
                Logger.LogError($"SMTP error: {sx.Message} (StatusCode: {sx.StatusCode})", sx); // Use Logger, include exception
                progress?.Report($"Error: SMTP issue ({sx.StatusCode} - {sx.Message}).");
                return false; // Return failure
            }
            catch (Exception ex) // Catch general exceptions
            {
                Logger.LogCritical($"Unexpected error sending email: {ex.Message}", ex); // Use Logger, include exception
                progress?.Report($"Error: An unexpected issue occurred ({ex.Message}).");
                return false; // Return failure
            }
        }

        /// <summary>
        /// Creates and configures an SmtpClient instance using settings read during initialization.
        /// </summary>
        /// <returns>Configured SmtpClient instance.</returns>
        private SmtpClient CreateSmtpClient()
        {
            // Use the fields populated in the constructor
            var client = new SmtpClient(_smtpServer, _smtpPort)
            {
                EnableSsl = _enableSsl,
                Timeout = 30000, // 30 second timeout for network operations
                // DeliveryMethod = SmtpDeliveryMethod.Network // Default
            };

            // Add credentials only if username/password are provided
            if (!string.IsNullOrEmpty(_smtpUsername) && !string.IsNullOrEmpty(_smtpPassword))
            {
                client.Credentials = new NetworkCredential(_smtpUsername, _smtpPassword);
                Logger.LogDebug("Using provided SMTP credentials."); // Use Logger
            }
            else
            {
                Logger.LogDebug("No SMTP credentials provided, attempting anonymous/integrated auth."); // Use Logger
                // client.UseDefaultCredentials = true; // Consider if integrated auth is needed/supported
            }

            return client;
        }

        /// <summary>
        /// Adds recipients to the MailMessage object, validating each address.
        /// </summary>
        /// <param name="mail">MailMessage instance.</param>
        /// <param name="addresses">List of email addresses.</param>
        /// <param name="recipientType">Type of recipient (To or CC).</param>
        /// <exception cref="FormatException">Thrown if an email address is invalid.</exception>
        private static void AddRecipients(MailMessage mail, List<string>? addresses, MailMessageRecipientType recipientType)
        {
            if (addresses == null || addresses.Count == 0) return; // Nothing to add

            foreach (string address in addresses)
            {
                string trimmedAddress = address.Trim();
                if (!IsValidEmail(trimmedAddress)) // Use helper for validation
                {
                    Logger.LogWarning($"Invalid email address format skipped: {address}"); // Use Logger
                    throw new FormatException($"Invalid email address format: {trimmedAddress}");
                }

                // Add validated address
                switch (recipientType)
                {
                    case MailMessageRecipientType.To:
                        mail.To.Add(trimmedAddress);
                        break;
                    case MailMessageRecipientType.CC:
                        mail.CC.Add(trimmedAddress);
                        break;
                        // Bcc could be added here if needed:
                        // case MailMessageRecipientType.Bcc:
                        //    mail.Bcc.Add(trimmedAddress);
                        //    break;
                }
            }
        }

        /// <summary>
        /// Creates a MailAttachment by reading the specified file into a MemoryStream.
        /// This avoids holding a lock on the original file path during email sending.
        /// Includes retry logic for reading the file.
        /// </summary>
        /// <param name="filePath">The full path to the file.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <param name="maxRetries">Maximum number of read attempts.</param>
        /// <param name="delayMs">Delay between retries in milliseconds.</param>
        /// <returns>An Attachment object, or null if the file cannot be read or other error occurs.</returns>
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
                    // Read all bytes asynchronously
                    fileBytes = await File.ReadAllBytesAsync(filePath, cancellationToken);
                    fileReadSuccess = true;
                    Logger.LogDebug($"Successfully read {fileBytes.Length} bytes from {filePath}");
                    break; // Exit loop on success
                }
                catch (IOException ioEx) when (i < maxRetries - 1)
                {
                    Logger.LogWarning($"Attempt {i + 1} failed to read attachment file '{filePath}' due to IO error: {ioEx.Message}. Retrying in {delayMs}ms...");
                    await Task.Delay(delayMs, cancellationToken);
                }
                catch (IOException ioEx) // Final attempt failed
                {
                    Logger.LogError($"Failed to read attachment file '{filePath}' after {maxRetries} attempts: {ioEx.Message}", ioEx);
                    return null; // Return null if file cannot be read
                }
                catch (OperationCanceledException)
                {
                    Logger.LogWarning("File read for attachment cancelled.");
                    throw; // Re-throw cancellation
                }
                catch (Exception ex)
                {
                    Logger.LogError($"Unexpected error reading attachment file '{filePath}': {ex.Message}", ex);
                    return null; // Return null on other errors
                }
            }

            if (!fileReadSuccess)
            {
                Logger.LogError($"Failed to read attachment file '{filePath}' after retries (fileReadSuccess is false).");
                return null;
            }

            try
            {
                // Create a MemoryStream from the byte array
                var memoryStream = new MemoryStream(fileBytes);

                // Determine content type (optional but good practice)
                var contentType = new ContentType(MediaTypeNames.Application.Octet); // Default binary type
                // Example for Excel:
                string fileExtension = Path.GetExtension(filePath).ToLowerInvariant();
                if (fileExtension == ".xlsx") contentType = new ContentType("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
                else if (fileExtension == ".xls") contentType = new ContentType("application/vnd.ms-excel");

                // Create the attachment from the stream
                var attachment = new Attachment(memoryStream, contentType)
                {
                    // Set the filename for the attachment as it appears in the email
                    Name = Path.GetFileName(filePath)
                };

                // IMPORTANT: Do NOT dispose the memoryStream here. The Attachment object takes ownership.
                Logger.LogDebug($"Created attachment '{attachment.Name}' from MemoryStream.");
                return attachment;
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error creating attachment from memory stream for file '{filePath}': {ex.Message}", ex);
                return null;
            }
        }


        /// <summary>
        /// Validates an email address format using the MailAddress class.
        /// </summary>
        /// <param name="email">The email address to validate.</param>
        /// <returns>True if the email address format is valid, false otherwise.</returns>
        private static bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            try
            {
                // Use .NET's built-in parser - throws FormatException for invalid formats
                _ = new MailAddress(email);
                return true;
            }
            catch (FormatException)
            {
                return false;
            }
            // Note: This only checks format, not deliverability or domain existence.
        }

        /// <summary>
        /// Represents the type of recipient for an email message. (Private as it's only used internally)
        /// </summary>
        private enum MailMessageRecipientType
        {
            To,
            CC
            // Bcc // Add if needed
        }
    }
}
