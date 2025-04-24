namespace conversionTest;

using Microsoft.Extensions.Configuration;
using System;
using System.Diagnostics; // Added for Debug.WriteLine
using System.IO;
using System.Text; // Required for Encoding
using System.Windows.Forms;
using System.Security.Cryptography; // Required for CryptographicException

static class Program
{
    public static IConfiguration? Configuration { get; private set; }

    // Define the specific path for appsettings.json
    // Use verbatim string literal @"" to avoid escaping backslashes
    private const string SettingsDirectoryPath = @"\\harlow.local\DFS\IT Department\Applications\Development 2025\QuoteConversionReportAutomation\conversionTest";
    private const string SettingsFileName = "appsettings.json";

    /// <summary>
    /// The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main()
    {
        // Construct the full path to the settings file
        string settingsFilePath = Path.Combine(SettingsDirectoryPath, SettingsFileName);

        // --- TEMPORARY CODE: RUN ONCE TO ENCRYPT appsettings.json ---
        // REMOVE OR COMMENT OUT THIS BLOCK AFTER RUNNING ONCE!
        try
        {
            // Uncomment the following block to perform encryption:
            /*
            Console.WriteLine($"Attempting to encrypt if necessary: {settingsFilePath}");

            // Call the helper method which includes checks and prompts
            bool encryptionResult = ProtectedDataHelper.EncryptFileContentIfNotEncrypted(settingsFilePath);

            if (encryptionResult) // Helper returns true if encryption happened OR user cancelled overwrite
            {
                // Check if the file *still* looks like plain text (meaning user cancelled overwrite)
                string finalContentCheck = File.Exists(settingsFilePath) ? File.ReadAllText(settingsFilePath, Encoding.UTF8).Trim() : "";
                if (finalContentCheck.StartsWith('{') || finalContentCheck.StartsWith('['))
                {
                    // User must have cancelled the overwrite of an already encrypted-looking file
                    Console.WriteLine("Encryption was cancelled by the user.");
                    // MessageBox is shown by the helper method in this case.
                }
                else if (!File.Exists(settingsFilePath))
                {
                    // This case shouldn't happen if helper ran correctly, but check anyway
                    MessageBox.Show($"Encryption failed: File not found after attempt.", "Encryption Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    // Encryption was performed or user confirmed overwrite
                    MessageBox.Show($"'{SettingsFileName}' has been encrypted (or overwrite confirmed).\n\n*** IMPORTANT: REMOVE or COMMENT OUT the encryption code block in Program.cs NOW before running the application again! ***",
                                    "Encryption Complete/Confirmed - REMOVE CODE NOW", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            else
            {
                // Helper returned false, indicating an error occurred (e.g., file not found initially)
                // MessageBox is shown by the helper method in this case.
                Console.WriteLine("Encryption check/process failed.");
            }
            return; // Stop the application after attempting encryption check/process
            */

            // Keep the block commented out during normal operation
            Console.WriteLine("Encryption block is commented out (normal operation).");

        }
        catch (Exception encEx) // Catch unexpected errors during the encryption call itself
        {
            Debug.WriteLine($"CRITICAL: Unexpected error during encryption attempt '{settingsFilePath}': {encEx}");
            Console.Error.WriteLine($"CRITICAL: Unexpected error during encryption attempt '{settingsFilePath}': {encEx}");
            MessageBox.Show($"An unexpected error occurred during the encryption process for '{SettingsFileName}':\n{encEx.Message}", "Encryption Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return; // Stop if encryption fails critically
        }
        // --- END TEMPORARY CODE ---


        // --- Load Configuration (Expects Encrypted File) ---
        // Logger might not be initialized yet, use Debug/Console for critical startup errors
        Debug.WriteLine($"Attempting to load configuration from: {settingsFilePath}");

        IConfigurationBuilder builder = new ConfigurationBuilder();

        try
        {
            // --- Check if the directory and file exist before attempting decryption ---
            if (!Directory.Exists(SettingsDirectoryPath))
            {
                throw new DirectoryNotFoundException($"The specified configuration directory does not exist or is inaccessible: {SettingsDirectoryPath}");
            }
            if (!File.Exists(settingsFilePath))
            {
                throw new FileNotFoundException($"The configuration file was not found at the specified path: {settingsFilePath}");
            }
            // --- End Check ---


            // 1. Read and Decrypt the settings file using the full path
            string decryptedJson = ProtectedDataHelper.DecryptFileContent(settingsFilePath);

            // 2. Convert decrypted string content to a stream
            byte[] decryptedBytes = Encoding.UTF8.GetBytes(decryptedJson);
            using var decryptedStream = new MemoryStream(decryptedBytes);

            // 3. Load configuration from the decrypted stream
            builder.AddJsonStream(decryptedStream); // Load from the decrypted stream
                                                    // .AddEnvironmentVariables(); // Add other providers if needed AFTER json

            Configuration = builder.Build();

            // --- Initialize Logger AFTER configuration is built ---
            Logger.Initialize(Configuration);
            // --- End Logger Initialization ---

            Logger.LogInfo($"Configuration loaded successfully from encrypted file: {settingsFilePath}");
        }
        catch (DirectoryNotFoundException dirEx)
        {
            Debug.WriteLine($"CRITICAL: Configuration directory not found: {dirEx.Message}");
            Console.Error.WriteLine($"CRITICAL: Configuration directory not found: {dirEx.Message}");
            MessageBox.Show($"Error: Configuration directory not found or inaccessible.\nPlease check the path:\n{SettingsDirectoryPath}\n\nDetails: {dirEx.Message}", "Configuration Path Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return; // Exit
        }
        catch (FileNotFoundException fileEx)
        {
            Debug.WriteLine($"CRITICAL: Configuration file not found: {fileEx.Message}");
            Console.Error.WriteLine($"CRITICAL: Configuration file not found: {fileEx.Message}");
            MessageBox.Show($"Error: Configuration file '{SettingsFileName}' not found in the specified directory.\nPlease check the path:\n{settingsFilePath}\n\nDetails: {fileEx.Message}", "Configuration File Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return; // Exit
        }
        catch (CryptographicException cryptoEx) when (cryptoEx.Message.Contains("Keyset does not exist")) // More specific check
        {
            Debug.WriteLine($"CRITICAL: Failed to decrypt configuration file (Keyset Error) '{settingsFilePath}': {cryptoEx.Message}");
            Console.Error.WriteLine($"CRITICAL: Failed to decrypt configuration file (Keyset Error) '{settingsFilePath}': {cryptoEx.Message}");
            MessageBox.Show($"Error: Could not decrypt configuration file.\nThis usually means it was encrypted by a different user or the user profile is corrupted.\nPath: {settingsFilePath}\n\nDetails: {cryptoEx.Message}", "Configuration Decryption Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return; // Exit
        }
        catch (FormatException formatEx) when (formatEx.Message.Contains("Base-64")) // Catch the specific Base64 error
        {
            Debug.WriteLine($"CRITICAL: Failed to decode Base64 configuration file '{settingsFilePath}': {formatEx.Message}");
            Console.Error.WriteLine($"CRITICAL: Failed to decode Base64 configuration file '{settingsFilePath}': {formatEx.Message}");
            MessageBox.Show($"Error: The configuration file is not valid encrypted data (not Base64).\nPlease ensure the file contains the encrypted text and was not saved as plain JSON.\nPath: {settingsFilePath}\n\nDetails: {formatEx.Message}", "Configuration Format Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return; // Exit
        }
        catch (CryptographicException cryptoEx) // Catch other decryption errors
        {
            Debug.WriteLine($"CRITICAL: Failed to decrypt configuration file '{settingsFilePath}': {cryptoEx.Message}");
            Console.Error.WriteLine($"CRITICAL: Failed to decrypt configuration file '{settingsFilePath}': {cryptoEx.Message}");
            MessageBox.Show($"Error: Could not decrypt configuration file.\nEnsure it was encrypted by the current user and is not corrupted.\nPath: {settingsFilePath}\n\nDetails: {cryptoEx.Message}", "Configuration Decryption Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return; // Exit
        }
        catch (Exception ex) // Catch other potential errors (parsing, etc.)
        {
            Debug.WriteLine($"CRITICAL: Failed to load or build configuration from '{settingsFilePath}': {ex}");
            Console.Error.WriteLine($"CRITICAL: Failed to load or build configuration from '{settingsFilePath}': {ex}");
            MessageBox.Show($"An error occurred while loading configuration: {ex.Message}", "Configuration Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return; // Exit
        }


        // --- Run Application ---
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        // Configuration should not be null here due to earlier checks/returns
        Application.Run(new Form1(Configuration!)); // Use null-forgiving operator
    }
}
