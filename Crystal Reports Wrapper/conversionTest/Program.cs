<<<<<<< HEAD
// Program.cs
// Main entry point for the QCRA (Quote Conversion Report Automation) application.
// Sets up Dependency Injection, loads configuration, initialises logging,
// and launches the main form.
=======
// C# 10 File-Scoped Namespace
using conversionTest;
>>>>>>> parent of 171b8e4 (v1.9.2)

namespace QuoteConversionReportAutomation;

using Microsoft.Extensions.Configuration;
using QuoteConversionReportAutomation.Services.Logging;
using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
<<<<<<< HEAD

// Third-party namespaces
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

// Project specific namespaces
using conversionTest;
using QuoteConversionReportAutomation.Services.Logging;
using QuoteConversionReportAutomation.Services;
using QuoteConversionReportAutomation.Services.Interfaces;
using QuoteConversionReportAutomation.Services.Communication;
using QuoteConversionReportAutomation.Services.Excel;
using QuoteConversionReportAutomation.Managers;
using QuoteConversionReportAutomation.Orchestrators;
using QuoteConversionReportAutomation.Orchestrators.Interfaces;
using QuoteConversionReportAutomation.Configuration;
using QuoteConversionReportAutomation.Helpers;
using QuoteConversionReportAutomation.Forms;
using QuoteConversionReportAutomation.Interfaces; // For IAutoRunUIContext
#endregion

#pragma warning disable WFO5001

namespace QuoteConversionReportAutomation
=======
// using System.Security.Cryptography; // No longer needed for config loading
// using System.Text; // No longer needed for config loading

static class Program
>>>>>>> parent of 171b8e4 (v1.9.2)
{
    public static IConfiguration? Configuration { get; private set; }

    // Define the specific path for appsettings.json
    private const string SettingsDirectoryPath = @"\\harlow.local\DFS\IT Department\Applications\Development 2025\QuoteConversionReportAutomation\conversionTest";
    private const string SettingsFileName = "appsettings.json";

    /// <summary>
    /// The main entry point for the application.
    /// </summary>
<<<<<<< HEAD
    internal static class Program
    {
        #region Properties
        public static IConfiguration? Configuration { get; private set; }
        public static IServiceProvider? ServiceProvider { get; private set; }
        private const string SettingsDirectoryPath = @"\\harlow.local\DFS\IT Department\Applications\Development 2025\QuoteConversionReportAutomation\conversionTest";
        private const string SettingsFileName = "appsettings.json";
        #endregion

        #region Main Entry Point
        [STAThread]
        static void Main()
        {
            // Disabled until it works properly
            //Application.SetColorMode(SystemColorMode.System);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            try
            {
                Configuration = LoadConfiguration();
                if (Configuration == null) return;

                Logger.Initialize(Configuration);
                Logger.LogInfo("Logger initialised successfully.");

                var services = new ServiceCollection();
                ConfigureServices(services, Configuration);
                ServiceProvider = services.BuildServiceProvider();

                Logger.LogInfo("Resolving and running the main application form (Form1).");
                // Form1 itself is resolved, and its dependencies (including AutoRunManager) are created by DI.
                var mainForm = ServiceProvider.GetRequiredService<Form1>();
                Application.Run(mainForm);
            }
            catch (Exception ex)
            {
                string errorMessage = $"A critical error occurred during application startup: {ex.Message}";
                if (Logger.IsInitialized) Logger.LogCritical(errorMessage, ex);
                else Debug.WriteLine($"CRITICAL STARTUP ERROR (Logger not ready): {ex}");

                MessageBox.Show(errorMessage + "\n\nPlease check the logs. The application will now exit.",
                                        "Application Startup Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                if (Logger.IsInitialized) Logger.LogInfo("Application shutting down.");
                if (ServiceProvider is IDisposable disposableProvider)
                {
                    disposableProvider.Dispose();
                }
            }
        }
        #endregion

        #region Configuration Loading
        private static IConfiguration? LoadConfiguration()
        {
            // This method's logic is unchanged.
            #region Original Method Content
            string settingsFilePath = string.Empty;
            try
            {
                settingsFilePath = Path.Combine(SettingsDirectoryPath, SettingsFileName);

                if (!Directory.Exists(SettingsDirectoryPath))
                {
                    throw new DirectoryNotFoundException($"Configuration directory does not exist or is inaccessible: {SettingsDirectoryPath}. Please ensure the application can access this path or update it in Program.cs.");
                }
                if (!File.Exists(settingsFilePath))
                {
                    throw new FileNotFoundException($"Configuration file ('{SettingsFileName}') was not found at: {settingsFilePath}. Please ensure it exists or update the path in Program.cs.", settingsFilePath);
                }

                var builder = new ConfigurationBuilder()
                    .SetBasePath(SettingsDirectoryPath)
                    .AddJsonFile(SettingsFileName, optional: false, reloadOnChange: false);

                var configuration = builder.Build();
                Debug.WriteLine($"Configuration loaded successfully from: {settingsFilePath}");
                return configuration;
            }
            catch (Exception ex)
            {
                string loadErrorMsg = $"Failed to load configuration from '{settingsFilePath}': {ex.Message}";
                Debug.WriteLine($"CRITICAL CONFIGURATION ERROR: {loadErrorMsg}");
                Debug.WriteLine(ex.ToString());

                MessageBox.Show($"A critical error occurred while loading application configuration from '{settingsFilePath}':\n\n{ex.Message}\n\nThe application cannot start and will now exit. Please check the file path and JSON format.",
                                        "Configuration Load Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }
            #endregion
        }
        #endregion

        #region Dependency Injection Configuration
        private static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            services.AddSingleton(configuration);

            // --- Services ---
            services.AddSingleton<IReportPathService, ReportPathService>();
            services.AddSingleton<IStatusManagerService, StatusManagerService>();
            services.AddSingleton<EmailUtility>();
            services.AddSingleton<NamedPipeCommunicator>();
            services.AddSingleton<ExcelCopyData>();


            // --- Managers ---
            services.AddSingleton<ReportProcessManager>(sp =>
                new ReportProcessManager(sp.GetRequiredService<IReportPathService>().WrapperExecutablePath)
            );
            services.AddSingleton<EmailRecipientManager>();
            services.AddSingleton<GreetingManager>();

            // Register Form1 as a singleton first, as it implements IAutoRunUIContext
            services.AddSingleton<Form1>();
            // Register IAutoRunUIContext to be resolved from the Form1 singleton instance
            services.AddSingleton<IAutoRunUIContext>(sp => sp.GetRequiredService<Form1>());

            // Register AutoRunManager, constructing Lazy<IAutoRunUIContext> within its factory
            services.AddSingleton<AutoRunManager>(sp =>
            {
                var config = sp.GetRequiredService<IConfiguration>();
                var reportPathService = sp.GetRequiredService<IReportPathService>();
                var emailUtility = sp.GetRequiredService<EmailUtility>();
                var processManager = sp.GetRequiredService<ReportProcessManager>();
                var pipeCommunicator = sp.GetRequiredService<NamedPipeCommunicator>();

                var lazyAutoRunUIContext = new Lazy<IAutoRunUIContext>(() => sp.GetRequiredService<IAutoRunUIContext>());

                var excelProcessor = sp.GetRequiredService<ExcelCopyData>();
                var emailRecipientManager = sp.GetRequiredService<EmailRecipientManager>();
                var greetingManager = sp.GetRequiredService<GreetingManager>();
                var statusManager = sp.GetRequiredService<IStatusManagerService>();

                return new AutoRunManager(
                    config,
                    reportPathService,
                    emailUtility,
                    processManager,
                    pipeCommunicator,
                    lazyAutoRunUIContext,
                    excelProcessor,
                    emailRecipientManager,
                    greetingManager,
                    statusManager
                );
            });

            // --- Orchestrators ---
            services.AddSingleton<IManualReportOrchestrator, ManualReportOrchestrator>();

            // --- Forms (Dialogs) ---
            // Register forms that can be resolved directly or need specific parameters built.
            // The 'isDarkMode' parameter is no longer needed as forms now read from the static ThemeSettings class.
            services.AddTransient<SettingsForm>(sp =>
                new SettingsForm(
                    sp.GetRequiredService<IConfiguration>(),
                    Path.Combine(sp.GetRequiredService<IReportPathService>().AppSettingsDirectory, SettingsFileName)
                )
            );

            // Assuming ManageAutoReportDefinitionsForm constructor was also simplified to remove the boolean theme flag.
            // If its constructor is now just (IConfiguration, string), this registration is correct.
            services.AddTransient<ManageAutoReportDefinitionsForm>(sp =>
                new ManageAutoReportDefinitionsForm(
                    sp.GetRequiredService<IConfiguration>(),
                    Path.Combine(sp.GetRequiredService<IReportPathService>().AppSettingsDirectory, "autoReportDefinitions.json")
                )
            );

            // These forms now have simpler constructors that can be resolved by the container.
            services.AddTransient<ManageBankHolidaysForm>();
            services.AddTransient<ManageEmailRecipientsForm>();
            services.AddTransient<ManageGreetingsForm>();

            Logger.LogInfo("Dependency Injection services configured.");
=======
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
>>>>>>> parent of 171b8e4 (v1.9.2)
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