// C# 10+ Features
namespace QuoteConversionReportAutomation;

using conversionTest; // Added to access the static Logger class
// Required using directives
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Provides utility methods for sending emails asynchronously using configuration settings.
/// Includes logging integration.
/// </summary>
public class EmailUtility
{
    // Store configuration settings read from IConfiguration
    private readonly string _smtpServer;
    private readonly int _smtpPort;
    private readonly string _smtpUsername; // Used as 'From' address and for authentication
    private readonly string _smtpPassword;
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

        _smtpUsername = configuration["settings:SmtpUsername"]
            ?? throw new InvalidOperationException("Configuration key 'settings:SmtpUsername' (used as From address) is missing or empty.");

        // Password might be optional depending on auth mechanism. Consider if validation is needed.
        _smtpPassword = configuration["settings:SmtpPassword"] ?? string.Empty; // Allow empty password if server permits
        if (string.IsNullOrEmpty(_smtpPassword))
        {
            Logger.LogWarning("Configuration key 'settings:SmtpPassword' is empty. Authentication might fail if required."); // Use Logger
        }

        // Parse EnableSsl, defaulting to false if missing or invalid
        if (!bool.TryParse(configuration["settings:EnableSsl"], out _enableSsl))
        {
            _enableSsl = false; // Default value
            Logger.LogWarning($"Configuration key 'settings:EnableSsl' is missing or invalid. Defaulting to false."); // Use Logger
        }

        // Log configuration details (excluding password)
        Logger.LogInfo($"EmailUtility initialized: Server={_smtpServer}, Port={_smtpPort}, User={_smtpUsername}, SSL={_enableSsl}"); // Use Logger
    }

    /// <summary>
    /// Sends an email asynchronously with optional attachments.
    /// Uses SMTP settings read during initialization.
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
                From = new MailAddress(_smtpUsername), // Use configured username as From address
                Subject = subject,
                Body = body,
                IsBodyHtml = false // Set to true if body contains HTML
            };

            // Add recipients (validation happens inside AddRecipients)
            AddRecipients(mail, toAddresses, MailMessageRecipientType.To);
            AddRecipients(mail, ccAddresses, MailMessageRecipientType.CC); // Handles null/empty list
            Logger.LogDebug($"Recipients added. To: {string.Join(";", toAddresses)}, CC: {string.Join(";", ccAddresses ?? [])}"); // Use Logger

            // Add attachment if provided and valid (validation happens inside AddAttachment)
            using var attachment = AddAttachment(attachmentPath); // Returns IDisposable Attachment or null
            if (attachment != null)
            {
                mail.Attachments.Add(attachment);
                Logger.LogDebug($"Attachment added: {attachmentPath}"); // Use Logger
            }

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
        catch (FileNotFoundException fnfEx) // Catch attachment errors
        {
            Logger.LogError($"Attachment error: {fnfEx.Message}", fnfEx); // Use Logger, include exception
            progress?.Report($"Error: Attachment file not found ({fnfEx.FileName}).");
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
    /// Creates an Attachment object if the path is valid and the file exists.
    /// Returns null if no path is provided.
    /// </summary>
    /// <param name="attachmentPath">Path to the attachment file (can be null).</param>
    /// <returns>A disposable Attachment object or null.</returns>
    /// <exception cref="FileNotFoundException">Thrown if the attachment file does not exist.</exception>
    private static Attachment? AddAttachment(string? attachmentPath)
    {
        if (string.IsNullOrWhiteSpace(attachmentPath))
        {
            return null; // No attachment requested
        }

        if (!File.Exists(attachmentPath))
        {
            Logger.LogError($"Attachment file not found: {attachmentPath}"); // Use Logger
            throw new FileNotFoundException($"Attachment file not found: {attachmentPath}", attachmentPath);
        }

        // Create and return the attachment (caller is responsible for disposal via using statement on mail message)
        try
        {
            return new Attachment(attachmentPath);
        }
        catch (Exception ex)
        {
            Logger.LogError($"Error creating attachment from path '{attachmentPath}': {ex.Message}", ex); // Use Logger
                                                                                                          // Wrap in a more informative exception if needed, or rethrow
            throw new InvalidOperationException($"Failed to create attachment from '{attachmentPath}'.", ex);
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
