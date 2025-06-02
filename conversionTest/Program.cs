// Program.cs
// Main entry point for the QCRA (Quote Conversion Report Automation) application.
// This class is responsible for setting up the application environment,
// loading configuration, initializing logging, and launching the main form.

#region Using Directives
// System related namespaces
using System;
using System.Diagnostics; // For Debug.WriteLine
using System.IO;          // For Path, File, Directory operations
using System.Windows.Forms; // For Application, MessageBox
using System.Text.Json; // For JSON handling (if needed, though not used in this snippet)   

// Third-party namespaces
using Microsoft.Extensions.Configuration; // For IConfiguration and ConfigurationBuilder

// Project specific namespaces
using conversionTest; // The namespace of your Form1 class
using QuoteConversionReportAutomation.Services.Logging; // For the Logger class
#endregion

namespace QuoteConversionReportAutomation
{
    /// <summary>
    /// Contains the main entry point and initial setup logic for the QCRA application.
    /// </summary>
    internal static class Program // Changed to internal as it's typically not accessed from outside the assembly
    {
        #region Properties
        /// <summary>
        /// Gets the loaded application configuration.
        /// This provides access to settings defined in `appsettings.json` and other configuration sources.
        /// It is set once during application startup.
        /// </summary>
        public static IConfiguration? Configuration { get; private set; }

        // It's generally recommended to locate configuration files relative to the application's
        // executable or allow the path to be configurable (e.g., via environment variable or command-line argument)
        // for better deployment flexibility. Hardcoding network paths can be problematic.
        // However, sticking to your existing path for now.
        /// <summary>
        /// Defines the directory path where the main `appsettings.json` configuration file is located.
        /// </summary>
        private const string SettingsDirectoryPath = @"\\harlow.local\DFS\IT Department\Applications\Development 2025\QuoteConversionReportAutomation\conversionTest";
        
        /// <summary>
        /// Defines the filename for the main JSON application configuration.
        /// </summary>
        private const string SettingsFileName = "appsettings.json";
        #endregion

        #region Main Entry Point
        /// <summary>
        /// The main entry point for the application.
        /// This method initializes the application, loads configuration, sets up logging,
        /// and runs the main form.
        /// </summary>
        [STAThread] // Specifies that the COM threading model for the application is single-threaded apartment. Required for WinForms.
        static void Main()
        {
            // Construct the full path to the appsettings.json file.
            string settingsFilePath = string.Empty; // Initialize to prevent use before assignment if Path.Combine fails (unlikely)
            try
            {
                settingsFilePath = Path.Combine(SettingsDirectoryPath, SettingsFileName);
            }
            catch (ArgumentException argEx)
            {
                // This can happen if SettingsDirectoryPath or SettingsFileName contain invalid characters.
                Debug.WriteLine($"CRITICAL: Invalid path components for settings file. Directory: '{SettingsDirectoryPath}', FileName: '{SettingsFileName}'. Error: {argEx.Message}");
                MessageBox.Show($"Error: Invalid path components for the configuration file.\nDirectory: {SettingsDirectoryPath}\nFile: {SettingsFileName}\n\nDetails: {argEx.Message}",
                                "Configuration Path Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return; // Exit application if path is invalid.
            }

            Debug.WriteLine($"Attempting to load configuration from: {settingsFilePath}");

            // Create a ConfigurationBuilder to construct the IConfiguration object.
            var builder = new ConfigurationBuilder();

            try
            {
                // --- Validate Directory and File Existence ---
                // Check if the specified settings directory exists.
                if (!Directory.Exists(SettingsDirectoryPath))
                {
                    throw new DirectoryNotFoundException($"The specified configuration directory does not exist or is inaccessible: {SettingsDirectoryPath}");
                }
                // Check if the appsettings.json file exists in that directory.
                if (!File.Exists(settingsFilePath))
                {
                    throw new FileNotFoundException($"The configuration file ('{SettingsFileName}') was not found at the specified path: {settingsFilePath}");
                }
                // --- End Validation ---

                // --- Load Configuration ---
                // Set the base path for the configuration builder to the directory containing appsettings.json.
                // This allows AddJsonFile to find the file using just its name.
                builder.SetBasePath(SettingsDirectoryPath)
                       .AddJsonFile(SettingsFileName, optional: false, reloadOnChange: true); // Load appsettings.json.
                                                                                               // optional: false - The file is required.
                                                                                               // reloadOnChange: true - The configuration will be reloaded if the file changes.

                // Example: Add environment variables as a configuration source (optional).
                // builder.AddEnvironmentVariables();

                // Build the IConfiguration object.
                Configuration = builder.Build();

                // --- Initialize Logger ---
                // The Logger must be initialized *after* the Configuration is built,
                // as the Logger itself reads settings (like log directory and level) from the configuration.
                // The internal logic of Logger.Initialize will need to be updated to read from the new config paths.
                Logger.Initialize(Configuration);
                // --- End Logger Initialization ---

                Logger.LogInfo($"Configuration loaded successfully from: {settingsFilePath}");
            }
            // Specific exception handling for configuration loading issues.
            catch (DirectoryNotFoundException dirEx)
            {
                // Log to Debug output as Logger might not be initialized or working.
                Debug.WriteLine($"CRITICAL: Configuration directory not found: {dirEx.Message}");
                // Show a user-friendly message box.
                MessageBox.Show($"Error: Configuration directory not found or inaccessible.\nPlease check the path:\n{SettingsDirectoryPath}\n\nDetails: {dirEx.Message}", 
                                "Configuration Path Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return; // Exit application as configuration is essential.
            }
            catch (FileNotFoundException fileEx)
            {
                Debug.WriteLine($"CRITICAL: Configuration file not found: {fileEx.Message}");
                MessageBox.Show($"Error: Configuration file ('{SettingsFileName}') not found in the specified directory.\nPlease check the path:\n{settingsFilePath}\n\nDetails: {fileEx.Message}", 
                                "Configuration File Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return; 
            }
            catch (JsonException jsonEx) // Handles errors if appsettings.json is not valid JSON.
            {
                Debug.WriteLine($"CRITICAL: Failed to parse configuration file '{settingsFilePath}' due to JSON format error: {jsonEx.Message} (Line: {jsonEx.LineNumber}, Path: {jsonEx.Path})");
                MessageBox.Show($"Error: The configuration file ('{SettingsFileName}') is not valid JSON.\nPlease check the format, particularly around line {jsonEx.LineNumber} (Path: {jsonEx.Path}).\nFile: {settingsFilePath}\n\nDetails: {jsonEx.Message}", 
                                "Configuration Format Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return; 
            }
            catch (Exception ex) // Catch any other unexpected errors during configuration loading.
            {
                Debug.WriteLine($"CRITICAL: Failed to load or build configuration from '{settingsFilePath}': {ex}");
                // Attempt to initialize logger with null config for this specific error, then log.
                // This is a best-effort attempt if primary logger init failed.
                try 
                { 
                    if (!Logger.IsInitialized) Logger.Initialize(null); // Pass null if Configuration is the issue
                    Logger.LogCritical($"CRITICAL: Failed to load or build configuration from '{settingsFilePath}': {ex}"); 
                } 
                catch (Exception loggerInitEx)
                {
                    Debug.WriteLine($"CRITICAL: Additionally, Logger failed to initialize for error reporting: {loggerInitEx.Message}");
                }
                MessageBox.Show($"An unexpected error occurred while loading application configuration from '{settingsFilePath}':\n\n{ex.Message}\n\nThe application will now exit.", 
                                "Configuration Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return; 
            }

            // --- Run Application ---
            // Standard WinForms setup to enable visual styles and set compatible text rendering.
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Ensure Configuration is not null before passing it to Form1.
            // The null-forgiving operator (!) is used here because the preceding try-catch blocks
            // should exit the application if Configuration remains null due to loading errors.
            if (Configuration == null)
            {
                // This state should ideally not be reached if error handling above is correct.
                Logger.LogCritical("CRITICAL: Configuration is null at the point of running Form1. Application cannot start.");
                 MessageBox.Show("A critical error occurred: Application configuration could not be loaded. The application will now exit.", 
                                "Fatal Error", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                return;
            }
            
            // Create and run the main application form (Form1), passing the loaded configuration.
            Application.Run(new Form1(Configuration));
        }
        #endregion
    }
}
