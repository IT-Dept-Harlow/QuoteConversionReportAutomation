#region Using Directives
// System related namespaces
using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;

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
using QuoteConversionReportAutomation.Interfaces;
#endregion

// Disable specific warnings about async void usage in event handlers
// This is a known pattern in WinForms applications.
#pragma warning disable WFO5001

namespace QuoteConversionReportAutomation
{
    /// <summary>
    /// Contains the main entry point and initial setup logic for the QCRA application.
    /// </summary>
    internal static class Program
    {
        #region Properties
        /// <summary>
        /// Gets the application configuration loaded from appsettings.json.
        /// </summary>
        public static IConfiguration? Configuration { get; private set; }
        /// <summary>
        /// Gets the root service provider for dependency injection.
        /// </summary>
        public static IServiceProvider? ServiceProvider { get; private set; }
        /// <summary>
        /// The UNC path to the directory containing the application settings file.
        /// </summary>
        private const string SettingsDirectoryPath = @"\\harlow.local\DFS\IT Department\Applications\Development 2025\QuoteConversionReportAutomation\conversionTest";
        /// <summary>
        /// The name of the application settings file.
        /// </summary>
        private const string SettingsFileName = "appsettings.json";
        #endregion

        #region Main Entry Point
        /// <summary>
        /// The main entry point for the application. Handles configuration loading, DI setup, and application startup.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            try
            {
                // Load configuration from appsettings.json
                Configuration = LoadConfiguration();
                if (Configuration == null) return;

                // Initialize the logger
                Logger.Initialize(Configuration);
                Logger.LogInfo("Logger initialised successfully.");

                // Set up dependency injection
                var services = new ServiceCollection();
                ConfigureServices(services, Configuration);
                ServiceProvider = services.BuildServiceProvider();

                Logger.LogInfo("Resolving and running the main application form (Form1).");
                // Resolve and run the main form
                var mainForm = ServiceProvider.GetRequiredService<Form1>();
                Application.Run(mainForm);
            }
            catch (Exception ex)
            {
                // Handle critical startup errors
                string errorMessage = $"A critical error occurred during application startup: {ex.Message}";
                if (Logger.IsInitialized) Logger.LogCritical(errorMessage, ex);
                else Debug.WriteLine($"CRITICAL STARTUP ERROR (Logger not ready): {ex}");

                MessageBox.Show(errorMessage + "\n\nPlease check the logs. The application will now exit.",
                                        "Application Startup Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                // Log shutdown and dispose DI provider if needed
                if (Logger.IsInitialized) Logger.LogInfo("Application shutting down.");
                if (ServiceProvider is IDisposable disposableProvider)
                {
                    disposableProvider.Dispose();
                }
            }
        }
        #endregion

        #region Configuration Loading
        /// <summary>
        /// Loads the application configuration from the specified settings file.
        /// </summary>
        /// <returns>The loaded configuration, or null if loading fails.</returns>
        private static IConfiguration? LoadConfiguration()
        {
            string settingsFilePath = string.Empty;
            try
            {
                settingsFilePath = Path.Combine(SettingsDirectoryPath, SettingsFileName);

                // Ensure the settings directory exists
                if (!Directory.Exists(SettingsDirectoryPath))
                {
                    throw new DirectoryNotFoundException($"Configuration directory does not exist or is inaccessible: {SettingsDirectoryPath}.");
                }
                // Ensure the settings file exists
                if (!File.Exists(settingsFilePath))
                {
                    throw new FileNotFoundException($"Configuration file ('{SettingsFileName}') was not found at: {settingsFilePath}.", settingsFilePath);
                }

                // Build the configuration
                var builder = new ConfigurationBuilder()
                    .SetBasePath(SettingsDirectoryPath)
                    .AddJsonFile(SettingsFileName, optional: false, reloadOnChange: false);

                var configuration = builder.Build();
                Debug.WriteLine($"Configuration loaded successfully from: {settingsFilePath}");
                return configuration;
            }
            catch (Exception ex)
            {
                // Handle configuration loading errors
                string loadErrorMsg = $"Failed to load configuration from '{settingsFilePath}': {ex.Message}";
                Debug.WriteLine($"CRITICAL CONFIGURATION ERROR: {loadErrorMsg}");
                Debug.WriteLine(ex.ToString());
                MessageBox.Show($"A critical error occurred while loading application configuration from '{settingsFilePath}':\n\n{ex.Message}\n\nThe application cannot start and will now exit.",
                                        "Configuration Load Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }
        }
        #endregion

        #region Dependency Injection Configuration
        /// <summary>
        /// Configures the dependency injection container with all the application's services, managers, and forms.
        /// </summary>
        /// <param name="services">The service collection to add registrations to.</param>
        /// <param name="configuration">The application configuration.</param>
        private static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            // Add the main configuration object itself as a singleton.
            services.AddSingleton(configuration);

            // --- Services ---
            services.AddSingleton<IReportPathService, ReportPathService>();
            services.AddSingleton<IStatusManagerService, StatusManagerService>();
            services.AddSingleton<EmailUtility>();
            services.AddSingleton<NamedPipeCommunicator>();
            //services.AddSingleton<ExcelCopyData>();
            // --- Orchestrator registration ---
            services.AddSingleton<IExcelProcessingOrchestrator, ExcelProcessingOrchestrator>();


            // --- NEW: Register the newly created, specialised services ---
            services.AddSingleton<IReportPeriodService, ReportPeriodService>();
            services.AddSingleton<IFormValidationService, FormValidationService>();
            services.AddSingleton<IFinancialYearService, FinancialYearService>();
            services.AddSingleton<IExcelFilteringService, ExcelFilteringService>();
            services.AddSingleton<IExcelAnalysisService, ExcelAnalysisService>();
            services.AddSingleton<ILeadTimeAnalysisService, LeadTimeAnalysisService>();
            services.AddSingleton<IPowerBiDataService, PowerBiDataService>();
            services.AddSingleton<IExcelDataExclusionService, ExcelDataExclusionService>();


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
                var excelProcessor = sp.GetRequiredService<IExcelProcessingOrchestrator>();
                var emailRecipientManager = sp.GetRequiredService<EmailRecipientManager>();
                var greetingManager = sp.GetRequiredService<GreetingManager>();
                var statusManager = sp.GetRequiredService<IStatusManagerService>();

                return new AutoRunManager(config, reportPathService, emailUtility, processManager, pipeCommunicator, lazyAutoRunUIContext,
                                          excelProcessor, emailRecipientManager, greetingManager, statusManager);
            });

            // --- Orchestrators ---
            services.AddSingleton<IManualReportOrchestrator, ManualReportOrchestrator>();
            services.AddSingleton<IBatchRegenerationOrchestrator>(sp =>
                new BatchRegenerationOrchestrator(
                    sp.GetRequiredService<IStatusManagerService>(),
                    sp.GetRequiredService<IManualReportOrchestrator>(),
                    sp.GetRequiredService<IConfiguration>()
                ));
            services.AddSingleton<IRetrospectiveAnalysisOrchestrator>(sp =>
                new RetrospectiveAnalysisOrchestrator(
                    sp.GetRequiredService<IStatusManagerService>(),
                    sp.GetRequiredService<ILeadTimeAnalysisService>()
                ));

            // --- Forms (Dialogs) ---
            services.AddTransient<SettingsForm>(sp =>
                new SettingsForm(
                    sp.GetRequiredService<IConfiguration>(),
                    Path.Combine(sp.GetRequiredService<IReportPathService>().AppSettingsDirectory, SettingsFileName)
                )
            );
            services.AddTransient<DateRangeSelectionForm>(sp => new DateRangeSelectionForm(sp.GetRequiredService<IConfiguration>()));
            services.AddTransient<ManageAutoReportDefinitionsForm>(sp =>
                new ManageAutoReportDefinitionsForm(
                    sp.GetRequiredService<IConfiguration>(),
                    Path.Combine(sp.GetRequiredService<IReportPathService>().AppSettingsDirectory, "autoReportDefinitions.json")
                )
            );
            services.AddTransient<ManageBankHolidaysForm>();
            services.AddTransient<ManageEmailRecipientsForm>();
            services.AddTransient<ManageGreetingsForm>();
            services.AddTransient<AnalysisOptionsForm>();
            services.AddTransient<ManageTenderExclusionsForm>();

            Logger.LogInfo("Dependency Injection services configured.");
        }
        #endregion
    }
}