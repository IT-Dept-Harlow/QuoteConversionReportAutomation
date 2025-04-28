// C# 10 File-Scoped Namespace
using conversionTest;

namespace QuoteConversionReportAutomation; // Or a more general namespace like YourProject.Security

using System;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms; // Required for MessageBox

/// <summary>
/// Provides static methods for encrypting and decrypting data using the
/// Windows Data Protection API (DPAPI), scoped to the current user.
/// </summary>
public static class ProtectedDataHelper
{
    // Optional: Add entropy for additional security. If used, the SAME entropy
    // MUST be used for both encryption and decryption. Keep it secret.
    // private static readonly byte[] s_entropy = Encoding.UTF8.GetBytes("YourSecretEntropyPhrase"); // Example

    /// <summary>
    /// Encrypts a plain text string using DPAPI scoped to the current user.
    /// </summary>
    /// <param name="plainText">The string to encrypt.</param>
    /// <returns>A Base64 encoded string representing the encrypted data.</returns>
    /// <exception cref="ArgumentNullException">Thrown if plainText is null.</exception>
    /// <exception cref="CryptographicException">Thrown if encryption fails.</exception>
    public static string EncryptString(string plainText)
    {
        ArgumentNullException.ThrowIfNull(plainText);
        byte[] plainTextBytes = Encoding.UTF8.GetBytes(plainText);
        byte[] encryptedBytes = ProtectedData.Protect(
            plainTextBytes,
            null, // Optional entropy (s_entropy)
            DataProtectionScope.CurrentUser);
        return Convert.ToBase64String(encryptedBytes);
    }

    /// <summary>
    /// Decrypts a Base64 encoded string that was encrypted using DPAPI (CurrentUser scope).
    /// </summary>
    /// <param name="encryptedText">The Base64 encoded encrypted string.</param>
    /// <returns>The original plain text string.</returns>
    /// <exception cref="ArgumentNullException">Thrown if encryptedText is null.</exception>
    /// <exception cref="FormatException">Thrown if encryptedText is not a valid Base64 string.</exception>
    /// <exception cref="CryptographicException">Thrown if decryption fails (wrong user, corrupted data, etc.).</exception>
    public static string DecryptString(string encryptedText)
    {
        ArgumentNullException.ThrowIfNull(encryptedText);
        byte[] encryptedBytes = Convert.FromBase64String(encryptedText);
        byte[] plainTextBytes = ProtectedData.Unprotect(
            encryptedBytes,
            null, // Optional entropy (s_entropy) - MUST match encryption entropy
            DataProtectionScope.CurrentUser);
        return Encoding.UTF8.GetString(plainTextBytes);
    }

    /// <summary>
    /// Encrypts the content of a file and overwrites the original file with encrypted data.
    /// Performs NO checks if the file is already encrypted. Use with caution or use EncryptFileContentIfNotEncrypted.
    /// </summary>
    /// <param name="filePath">Path to the file to encrypt.</param>
    /// <exception cref="FileNotFoundException">Thrown if the file doesn't exist.</exception>
    public static void EncryptFileContent(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("File not found for encryption.", filePath);
        }
        string plainText = File.ReadAllText(filePath, Encoding.UTF8);
        string encryptedText = EncryptString(plainText);
        File.WriteAllText(filePath, encryptedText, Encoding.UTF8); // Overwrite
        Console.WriteLine($"Force encrypted content of: {filePath}");
        Logger.LogInfo($"Force encrypted content of: {filePath}"); // Assuming Logger is accessible
    }

    /// <summary>
    /// Checks if a file appears to be plain text JSON, prompts the user if it seems already encrypted,
    /// and encrypts the content if confirmed or if it's plain text.
    /// </summary>
    /// <param name="filePath">Path to the file to potentially encrypt.</param>
    /// <returns>True if encryption was performed or skipped due to user cancellation, False if an error occurred or file not found.</returns>
    public static bool EncryptFileContentIfNotEncrypted(string filePath)
    {
        try
        {
            Console.WriteLine($"Checking encryption status for: {filePath}");
            if (!File.Exists(filePath))
            {
                MessageBox.Show($"The file to encrypt was not found:\n{filePath}", "Encryption Error - File Not Found", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false; // Indicate failure
            }

            // Read content to check format
            string currentContentCheck = File.ReadAllText(filePath, Encoding.UTF8).Trim();

            // Basic heuristic: if not starting with { or [ (common JSON starts) AND reasonably long, assume encrypted
            bool looksEncrypted = !currentContentCheck.StartsWith('{') && !currentContentCheck.StartsWith('[')
                                   && currentContentCheck.Length > 50; // Adjust length threshold if needed

            if (looksEncrypted)
            {
                Console.WriteLine("File appears to be already encrypted (heuristic check).");
                DialogResult overwriteResult = MessageBox.Show(
                   $"The file appears to might already be encrypted:\n{filePath}\n\nRe-encrypting will CORRUPT it.\n\nDo you want to proceed with encryption anyway (ONLY if you are SURE it's plain text)?",
                   "Encryption Warning - Already Encrypted?",
                   MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);

                if (overwriteResult == DialogResult.No)
                {
                    MessageBox.Show("Encryption cancelled by user.", "Encryption Cancelled", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return true; // Indicate skipped/cancelled, not an error state
                }
                Console.WriteLine("User chose to proceed with encryption despite warning.");
            }
            else
            {
                Console.WriteLine("File appears to be plain text or empty. Proceeding with encryption.");
            }

            // Proceed with encryption (either because it looked plain or user confirmed overwrite)
            Console.WriteLine("Encrypting file content...");
            string plainText = currentContentCheck; // Use already read content
            string encryptedText = EncryptString(plainText);
            File.WriteAllText(filePath, encryptedText, Encoding.UTF8); // Overwrite
            Console.WriteLine($"Successfully encrypted content of: {filePath}");
            Logger.LogInfo($"Successfully encrypted content of: {filePath}");
            return true; // Indicate success
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"ERROR during EncryptFileContentIfNotEncrypted: {ex}");
            Console.Error.WriteLine($"ERROR during EncryptFileContentIfNotEncrypted: {ex}");
            MessageBox.Show($"Failed to check or encrypt '{filePath}':\n{ex.Message}", "Encryption Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return false; // Indicate failure
        }
    }


    /// <summary>
    /// Decrypts the content of a file.
    /// </summary>
    /// <param name="filePath">Path to the file to decrypt.</param>
    /// <returns>The decrypted file content as a string.</returns>
    /// <exception cref="FileNotFoundException">Thrown if the file doesn't exist.</exception>
    /// <exception cref="FormatException">Thrown if content is not valid Base64.</exception>
    /// <exception cref="CryptographicException">Thrown if decryption fails.</exception>
    public static string DecryptFileContent(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("File not found for decryption.", filePath);
        }
        string encryptedText = File.ReadAllText(filePath, Encoding.UTF8);
        // Add check for empty file before attempting decryption
        if (string.IsNullOrWhiteSpace(encryptedText))
        {
            throw new CryptographicException($"The file '{filePath}' is empty or contains only whitespace and cannot be decrypted.");
        }
        return DecryptString(encryptedText);
    }
}
