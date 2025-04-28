// C# 10 File-Scoped Namespace
using conversionTest;

namespace QuoteConversionReportAutomation;

using Microsoft.Extensions.Configuration;
using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
// using System.Security.Cryptography; // No longer needed for config loading
// using System.Text; // No longer needed for config loading

static class Program
{
    public static IConfiguration? Configuration { get; private set; }

    // Define the specific path for appsettings.json
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

        // --- Load Configuration (Plain Text JSON) ---
        Debug.WriteLine($"Attempting to load configuration from: {settingsFilePath}");

        ConfigurationBuilder builder = new();

        try
        {
            // --- Check if the directory and file exist ---
            if (!Directory.Exists(SettingsDirectoryPath))
            {
                throw new DirectoryNotFoundException($"The specified configuration directory does not exist or is inaccessible: {SettingsDirectoryPath}");
            }
            if (!File.Exists(settingsFilePath))
            {
                throw new FileNotFoundException($"The configuration file was not found at the specified path: {settingsFilePath}");
            }
            // --- End Check ---

            // --- Load configuration directly from the JSON file ---
            // SetBasePath is important so the builder knows where to look for the file
            builder.SetBasePath(SettingsDirectoryPath)
                   .AddJsonFile(SettingsFileName, optional: false, reloadOnChange: true); // Load directly

            // .AddEnvironmentVariables(); // Consider adding this if using environment variables for secrets

            Configuration = builder.Build();

            // --- Initialize Logger AFTER configuration is built ---
            Logger.Initialize(Configuration);
            // --- End Logger Initialization ---

            Logger.LogInfo($"Configuration loaded successfully from: {settingsFilePath}");
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
        catch (FormatException formatEx) // Catch JSON formatting errors
        {
            Debug.WriteLine($"CRITICAL: Failed to parse configuration file '{settingsFilePath}': {formatEx.Message}");
            Console.Error.WriteLine($"CRITICAL: Failed to parse configuration file '{settingsFilePath}': {formatEx.Message}");
            MessageBox.Show($"Error: The configuration file is not valid JSON.\nPlease check the format.\nPath: {settingsFilePath}\n\nDetails: {formatEx.Message}", "Configuration Format Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return; // Exit
        }
        catch (Exception ex) // Catch other potential errors (parsing, access rights, etc.)
        {
            Debug.WriteLine($"CRITICAL: Failed to load or build configuration from '{settingsFilePath}': {ex}");
            Console.Error.WriteLine($"CRITICAL: Failed to load or build configuration from '{settingsFilePath}': {ex}");
            // Attempt to initialize logger for this error if possible, otherwise use MessageBox
            try { Logger.Initialize(null); Logger.LogCritical($"CRITICAL: Failed to load or build configuration from '{settingsFilePath}': {ex}"); } catch { /* Ignore logger init error */ }
            MessageBox.Show($"An error occurred while loading configuration: {ex.Message}", "Configuration Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return; // Exit
        }


        // --- Run Application ---
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        // Configuration should not be null here due to earlier checks/returns
        // Pass the configuration AND the settings file path to Form1
        //Application.Run(new Form1(Configuration!, settingsFilePath)); // Use null-forgiving operator for Configuration
        Application.Run(new Form1(Configuration!)); // Use null-forgiving operator for Configuration
    }
}
