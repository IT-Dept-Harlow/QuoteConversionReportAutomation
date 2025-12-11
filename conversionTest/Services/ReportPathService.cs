#region Using Directives

// Third-party namespaces for external libraries.
using Microsoft.Extensions.Configuration;

// Project-specific namespaces for application components.
using QuoteConversionReportAutomation.Configuration; // For AppConfigKeys
using QuoteConversionReportAutomation.Helpers;    // For FolderCreation and ReportTypeHelper
using QuoteConversionReportAutomation.Models;     // For ReportType enum
using QuoteConversionReportAutomation.Services.Logging; // For Logger (static calls)
using QuoteConversionReportAutomation.Services.Interfaces; // For IReportPathService
using System;
using System.IO;

#endregion

namespace QuoteConversionReportAutomation.Services
{
    #region Class Definition
    /// <summary>
    /// Implements the <see cref="IReportPathService"/> to provide centralised access to application paths
    /// and report-specific path generation logic. It reads path configurations from IConfiguration
    /// and handles the resolution of user-profile relative paths and environment variables.
    /// </summary>
    public sealed class ReportPathService : IReportPathService
    {
        #region Fields

        /// <summary>
        /// Provides read-only access to the application's configuration settings.
        /// </summary>
        private readonly IConfiguration _configuration;

        /// <summary>
        /// Stores the cached path to the current user's profile directory (e.g., C:\Users\Username).
        /// </summary>
        private readonly string _userProfilePath;

        #endregion

        #region Constructor

        /// <summary>
        /// Initialises a new instance of the <see cref="ReportPathService"/> class.
        /// </summary>
        /// <param name="configuration">The application's configuration settings.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="configuration"/> is null.</exception>
        public ReportPathService(IConfiguration configuration)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            // Determine and cache the user's profile path and the location of appsettings.json upon initialisation.
            _userProfilePath = GetUserProfilePathInternal();
            AppSettingsDirectory = DetermineAppSettingsDirectory();
            Logger.LogInfo($"ReportPathService initialised. UserProfilePath: '{_userProfilePath}', AppSettingsDirectory: '{AppSettingsDirectory}'");
        }

        #endregion

        #region Properties

        /// <inheritdoc/>
        public string CrystalReportRptFilePath => ResolvePath(_configuration[AppConfigKeys.Paths.CrystalReportRptFile], "CrystalReportRptFile", isDirectory: false, allowEnvironmentVariables: true) ?? string.Empty;

        /// <inheritdoc/>
        public string WrapperExecutablePath => ResolvePath(_configuration[AppConfigKeys.Paths.WrapperExecutable], "WrapperExecutablePath", isDirectory: false, allowEnvironmentVariables: true) ?? string.Empty;

        /// <inheritdoc/>
        public string FinalReportOutputBaseDirectory => ResolvePath(
            _configuration[AppConfigKeys.Paths.FinalReportOutputBase],
            "FinalReportOutputBaseDirectory",
            isDirectory: true,
            allowEnvironmentVariables: true,
            treatAsUserProfileRelativeIfRelativeAndConfigured: true,
            defaultRelativePathToUserProfile: @"Harlow Printing\IT Projects - Documents\Dashboard Datasets\Raw_data\Quotes conversion\Estimates"
        ) ?? string.Empty;

        /// <inheritdoc/>
        public string TemplateBaseDirectory => ResolvePath(
            _configuration[AppConfigKeys.Paths.TemplateBase],
            "TemplateBaseDirectory",
            isDirectory: true,
            allowEnvironmentVariables: true,
            treatAsUserProfileRelativeIfRelativeAndConfigured: true,
            defaultRelativePathToUserProfile: @"Harlow Printing\IT Projects - Documents\Dashboard Datasets\Raw_data\Quotes conversion\TEMPLATE"
        ) ?? string.Empty;

        /// <inheritdoc/>
        public string RawReportExportBaseDirectory => ResolvePath(
            _configuration[AppConfigKeys.Paths.RawReportOutputBase],
            "RawReportExportBaseDirectory",
            isDirectory: true,
            allowEnvironmentVariables: true,
            treatAsUserProfileRelativeIfRelativeAndConfigured: true,
            defaultRelativePathToUserProfile: @"Harlow Printing\IT Projects - Documents\Dashboard Datasets\Raw_data\Quotes conversion\Estimate Reports Exports"
        ) ?? string.Empty;

        /// <inheritdoc/>
        public string LogDirectoryBase => ResolvePath(_configuration[AppConfigKeys.Paths.LogDirectoryBase], "LogDirectoryBase", isDirectory: true, allowEnvironmentVariables: true) ?? string.Empty;

        /// <inheritdoc/>
        public string ReportDefinitionsFileName => _configuration.GetValue<string>(AppConfigKeys.Paths.ReportDefinitionsFileName, "autoReportDefinitions.json")!;

        /// <inheritdoc/>
        public string AppSettingsDirectory { get; }

        /// <inheritdoc/>
        public string FallbackLogDirectory
        {
            get
            {
                // Get the configured path, or use a default path in the user's Local AppData folder if not configured.
                string? configuredPath = _configuration[AppConfigKeys.Logging.DefaultFallbackLogDirectory];
                string defaultPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "QCRA_Logs_Fallback", "Logs");
                return ResolvePath(configuredPath, "FallbackLogDirectory", isDirectory: true, allowEnvironmentVariables: true, defaultAbsolutePathIfConfigMissing: defaultPath) ?? defaultPath;
            }
        }
        #endregion

        #region Methods

        /// <inheritdoc/>
        public string? GetRawReportOutputPath(ReportType reportType, DateTime dateContext, string reportNameForFileName = "EstimateSuccessReport")
        {
            // Log entry for tracing.
            Logger.LogTrace($"GetRawReportOutputPath called. ReportType: {reportType}, DateContext: {dateContext:d}, ReportNameForFileName: {reportNameForFileName}");

            // Retrieve the base directory for raw report exports.
            string baseDir = RawReportExportBaseDirectory;
            if (string.IsNullOrEmpty(baseDir))
            {
                Logger.LogError("RawReportExportBaseDirectory is not configured or resolved correctly. Cannot generate raw report output path.");
                return null;
            }

            // Determine the full path to the specific subfolder for this report type and date.
            string? specificFolder = FolderCreation.GetReportSpecificFolderPath(reportType, baseDir, dateContext, _configuration);

            // Handle cases where the specific folder path could not be determined.
            if (string.IsNullOrEmpty(specificFolder))
            {
                Logger.LogError($"Could not determine specific folder path for raw report output. ReportType: {reportType}, Base: {baseDir}.");
                return null;
            }

            // Sanitise the report name to ensure it's a valid filename component.
            string sanitisedReportName = string.Join("_", (reportNameForFileName ?? "Report").Split(Path.GetInvalidFileNameChars()));

            // Generate the filename based on report type and date.
            string fileName = $"{dateContext:yyyyMMdd}_{sanitisedReportName}_Raw.xlsx";
            if (reportType == ReportType.Daily5Day1k)
            {
                fileName = $"{dateContext:yyyyMMdd}_{sanitisedReportName}_5Day1k_Raw.xlsx";
            }
            else if (reportType == ReportType.Custom)
            {
                fileName = $"{dateContext:yyyyMMdd}_{DateTime.Now:HHmmss}_{sanitisedReportName}_Custom_Raw.xlsx";
            }

            try
            {
                // Combine the folder and filename to create the full path.
                return Path.Combine(specificFolder, fileName);
            }
            catch (ArgumentException ex)
            {
                Logger.LogError($"Error combining path for raw report output: Invalid characters in path segments. SpecificFolder='{specificFolder}', FileName='{fileName}'. Error: {ex.Message}", ex);
                return null;
            }
        }

        /// <inheritdoc/>
        public string? GetExcelTemplatePath(ReportType reportType)
        {
            Logger.LogTrace($"GetExcelTemplatePath called. ReportType: {reportType}");

            // Retrieve the base directory where templates are stored.
            string baseDir = TemplateBaseDirectory;
            if (string.IsNullOrEmpty(baseDir))
            {
                Logger.LogError("TemplateBaseDirectory is not configured or resolved correctly. Cannot determine Excel template path.");
                return null;
            }

            // Retrieve the specific template filename from configuration, with a fallback default.
            string templateName = _configuration.GetValue<string>("Paths:ExcelTemplateFileName", "TEMPLATE_Estimate Success Rate_FINAL.xlsx")!;
            Logger.LogDebug($"Using template '{templateName}' from configuration for report type {reportType}.");

            try
            {
                // Combine the base directory and filename to get the full path.
                return Path.Combine(baseDir, templateName);
            }
            catch (ArgumentException ex)
            {
                Logger.LogError($"Error combining path for Excel template: Invalid characters in path segments. BaseDir='{baseDir}', TemplateName='{templateName}'. Error: {ex.Message}", ex);
                return null;
            }
        }

        /// <inheritdoc/>
        public string? GetReportDefinitionsFilePath()
        {
            // Validate that the necessary path components are available.
            if (string.IsNullOrEmpty(AppSettingsDirectory) || string.IsNullOrEmpty(ReportDefinitionsFileName))
            {
                Logger.LogError("Cannot determine report definitions file path: AppSettingsDirectory or ReportDefinitionsFileName is missing.");
                return null;
            }
            try
            {
                // Combine the app settings directory with the definitions filename.
                return Path.Combine(AppSettingsDirectory, ReportDefinitionsFileName);
            }
            catch (ArgumentException ex)
            {
                Logger.LogError($"Error combining path for report definitions file: Invalid characters. AppSettingsDir='{AppSettingsDirectory}', FileName='{ReportDefinitionsFileName}'. Error: {ex.Message}", ex);
                return null;
            }
        }

        /// <inheritdoc/>
        public bool IsEssentialPathConfigurationValid()
        {
            // Check for the existence of the two most critical files required for report generation.
            bool crystalReportFileExists = !string.IsNullOrEmpty(CrystalReportRptFilePath) && File.Exists(CrystalReportRptFilePath);
            bool wrapperExeFileExists = !string.IsNullOrEmpty(WrapperExecutablePath) && File.Exists(WrapperExecutablePath);

            // Log warnings if either of the essential files is missing.
            if (!crystalReportFileExists) Logger.LogWarning($"Essential Config Check: Crystal Report file not found or path invalid: '{CrystalReportRptFilePath}' (from {AppConfigKeys.Paths.CrystalReportRptFile})");
            if (!wrapperExeFileExists) Logger.LogWarning($"Essential Config Check: Wrapper EXE not found or path invalid: '{WrapperExecutablePath}' (from {AppConfigKeys.Paths.WrapperExecutable})");

            return crystalReportFileExists && wrapperExeFileExists;
        }

        /// <inheritdoc/>
        public string GetUserSpecificLogDirectory()
        {
            // Determine the effective base directory for logs, using the fallback if the primary is not configured.
            string effectiveBaseLogDir = LogDirectoryBase;
            if (string.IsNullOrEmpty(effectiveBaseLogDir))
            {
                effectiveBaseLogDir = FallbackLogDirectory;
                Logger.LogWarning($"GetUserSpecificLogDirectory: Primary log directory base is empty or invalid. Using fallback: '{effectiveBaseLogDir}'.");
            }

            // Sanitise the current username to ensure it is a valid directory name.
            string sanitisedUserName = string.Join("_", Environment.UserName.Split(Path.GetInvalidFileNameChars()));

            // Return the combined path.
            return Path.Combine(effectiveBaseLogDir, sanitisedUserName);
        }

        /// <inheritdoc/>
        public string GenerateFinalFileName(ReportType reportType, DateTime reportDate, DateTime runTimestamp)
        {
            // Use a switch expression to determine the filename based on the report type and date context.
            return reportType switch
            {
                ReportType.Daily => $"{reportDate:yyyyMMdd}_Estimate_Success_Rate_Daily.xlsx",
                ReportType.Daily5Day1k => $"{reportDate:yyyyMMdd}_Estimate_Success_Rate_Daily_5day_1k.xlsx",
                ReportType.Weekly => $"{reportDate:yyyyMMdd} Estimate Success Rate.xlsx",
                ReportType.Monthly => $"Estimate Success Rate {reportDate:MMM yy}.xlsx",
                ReportType.Quarterly => $"Estimate Success Rate {ReportHelper.GetQuarterString(reportDate)}.xlsx",
                ReportType.Annual => $"Estimate Success Rate FY {ReportHelper.GetFinancialYearStartCalendarYear(reportDate, _configuration)}-{ReportHelper.GetFinancialYearStartCalendarYear(reportDate, _configuration) + 1}.xlsx",
                ReportType.Custom => $"{reportDate:yyyyMMdd}_{runTimestamp:HHmmss}_Estimate_Success_Rate_Custom.xlsx",
                ReportType.NewCustomer => $"{reportDate:yyyyMMdd}_Estimate_Success_Rate_New_Customer.xlsx",
                // Fallback for any unknown types to prevent errors, includes a timestamp for uniqueness.
                _ => $"{reportDate:yyyyMMdd}_Estimate_Success_Rate_UnknownType_{runTimestamp:HHmmss}.xlsx",
            };
        }

        /// <inheritdoc/>
        public string? GetExpectedFinalFilePath(ReportType reportType, string baseFileSaveLocation, DateTime reportDate)
        {
            try
            {
                // Default the report date to today if not specified for a non-custom report.
                if (reportDate == default && reportType != ReportType.Custom)
                {
                    reportDate = DateTime.Today;
                }

                // For custom reports, use the current time for folder naming to ensure uniqueness.
                DateTime folderTimestampDate = reportType == ReportType.Custom ? DateTime.Now : reportDate;

                // Get the specific folder path for this report.
                string? folderPath = FolderCreation.GetReportSpecificFolderPath(reportType, baseFileSaveLocation, folderTimestampDate, _configuration);

                // If a valid folder path was determined, generate the filename and combine them.
                if (!string.IsNullOrEmpty(folderPath))
                {
                    string fileName = GenerateFinalFileName(reportType, reportDate, DateTime.Now);
                    return Path.Combine(folderPath, fileName);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error in GetExpectedFinalFilePath: {ex.Message}", ex);
            }

            // Return null if any part of the path generation fails.
            return null;
        }
        #endregion

        #region Private Helper Methods

        /// <summary>
        /// Resolves a path string from configuration, handling defaults, environment variables, and user-profile relativity.
        /// </summary>
        private string? ResolvePath(
            string? configuredPath,
            string pathKeyNameForLogging,
            bool isDirectory,
            bool allowEnvironmentVariables,
            bool treatAsUserProfileRelativeIfRelativeAndConfigured = false,
            string? defaultRelativePathToUserProfile = null,
            string? defaultAbsolutePathIfConfigMissing = null)
        {
            string? pathValueToProcess = configuredPath;
            bool wasConfiguredPathInitiallyEmpty = string.IsNullOrWhiteSpace(pathValueToProcess);
            string logPrefix = $"ResolvePath (Key: '{pathKeyNameForLogging}')";

            if (wasConfiguredPathInitiallyEmpty)
            {
                if (!string.IsNullOrWhiteSpace(defaultAbsolutePathIfConfigMissing))
                {
                    pathValueToProcess = defaultAbsolutePathIfConfigMissing;
                }
                else if (!string.IsNullOrWhiteSpace(defaultRelativePathToUserProfile))
                {
                    if (string.IsNullOrEmpty(_userProfilePath)) { return null; }
                    try
                    {
                        pathValueToProcess = Path.Combine(_userProfilePath, defaultRelativePathToUserProfile.TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
                    }
                    catch (ArgumentException) { return null; }
                }
                else { return null; }
            }

            if (string.IsNullOrWhiteSpace(pathValueToProcess)) return null;

            string resolvedPath = pathValueToProcess;
            if (allowEnvironmentVariables && resolvedPath.Contains('%'))
            {
                try { resolvedPath = Environment.ExpandEnvironmentVariables(resolvedPath); }
                catch (ArgumentException) { return null; }
            }

            if (!wasConfiguredPathInitiallyEmpty && treatAsUserProfileRelativeIfRelativeAndConfigured && !Path.IsPathRooted(resolvedPath) && !resolvedPath.StartsWith(@"\\", StringComparison.Ordinal))
            {
                if (string.IsNullOrEmpty(_userProfilePath)) { return null; }
                try { resolvedPath = Path.Combine(_userProfilePath, resolvedPath.TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)); }
                catch (ArgumentException) { return null; }
            }

            try
            {
                return Path.GetFullPath(resolvedPath);
            }
            catch (Exception) { return null; }
        }

        /// <summary>
        /// Gets the user's profile directory path.
        /// </summary>
        private string GetUserProfilePathInternal()
        {
            try
            {
                return Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            }
            catch (Exception ex)
            {
                Logger.LogCritical($"Failed to get UserProfilePath: {ex.Message}. Defaulting to current directory.", ex);
                return Environment.CurrentDirectory; // Fallback
            }
        }

        /// <summary>
        /// Determines the directory where the main `appsettings.json` file is located.
        /// </summary>
        private string DetermineAppSettingsDirectory()
        {
            string basePath = @"\\harlow.local\DFS\IT Department\Applications\Development 2025\QuoteConversionReportAutomation\conversionTest";
            string appSettingsPathInBase = Path.Combine(basePath, "appsettings.json");

            if (File.Exists(appSettingsPathInBase))
            {
                return basePath;
            }

            string? parentPath = Path.GetDirectoryName(basePath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
            if (!string.IsNullOrEmpty(parentPath))
            {
                string appSettingsPathInParent = Path.Combine(parentPath, "appsettings.json");
                if (File.Exists(appSettingsPathInParent))
                {
                    return parentPath;
                }
            }

            Logger.LogWarning($"DetermineAppSettingsDirectory: Could not find appsettings.json in '{basePath}' or its parent. Defaulting to BaseDirectory.");
            return AppDomain.CurrentDomain.BaseDirectory;
        }
        #endregion
    }
    #endregion
}