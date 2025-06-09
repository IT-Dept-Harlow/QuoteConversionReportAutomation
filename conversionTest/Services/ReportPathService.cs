// ReportPathService.cs
// Implements IReportPathService to provide centralized access to application paths
// and report-specific path generation logic for the QCRA application.
// Updated to use AppConfigKeys and ReportTypeHelper, with revised path resolution
// to better support user-profile relative paths for base directories if configured as such or by default.

#region Using Directives
using Microsoft.Extensions.Configuration;
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
    /// <summary>
    /// Provides centralized access to application paths and report-specific path generation logic.
    /// Reads path configurations from IConfiguration and handles resolution of user-profile relative paths
    /// and environment variables using constants from <see cref="AppConfigKeys"/>.
    /// Utilizes <see cref="ReportTypeHelper"/> for report type related logic.
    /// </summary>
    public sealed class ReportPathService : IReportPathService
    {
        #region Fields
        private readonly IConfiguration _configuration;
        private readonly string _userProfilePath;
        #endregion

        #region Constructor
        /// <summary>
        /// Initializes a new instance of the <see cref="ReportPathService"/> class.
        /// </summary>
        /// <param name="configuration">The application's configuration settings.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="configuration"/> is null.</exception>
        public ReportPathService(IConfiguration configuration)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _userProfilePath = GetUserProfilePathInternal();
            AppSettingsDirectory = DetermineAppSettingsDirectory();
            Logger.LogInfo($"ReportPathService initialized. UserProfilePath: '{_userProfilePath}', AppSettingsDirectory: '{AppSettingsDirectory}'");
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
            treatAsUserProfileRelativeIfRelativeAndConfigured: true, // << CHANGED to true
            defaultRelativePathToUserProfile: @"Harlow Printing\IT Projects - Documents\Dashboard Datasets\Raw_data\Quotes conversion\Estimates", // Default if key is missing
            defaultAbsolutePathIfConfigMissing: null
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
            treatAsUserProfileRelativeIfRelativeAndConfigured: true, // << CHANGED to true
            defaultRelativePathToUserProfile: @"Harlow Printing\IT Projects - Documents\Dashboard Datasets\Raw_data\Quotes conversion\Estimate Reports Exports", // Default if key is missing
            defaultAbsolutePathIfConfigMissing: null
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
            Logger.LogTrace($"GetRawReportOutputPath called. ReportType: {reportType}, DateContext: {dateContext:d}, ReportNameForFileName: {reportNameForFileName}");
            string baseDir = RawReportExportBaseDirectory; // This property now reflects the user-profile relative intent
            if (string.IsNullOrEmpty(baseDir))
            {
                Logger.LogError("RawReportExportBaseDirectory is not configured or resolved correctly. Cannot generate raw report output path.");
                return null;
            }

            int reportTypeIndex = ReportTypeHelper.ToInt(reportType);
            string? specificFolder = FolderCreation.GetReportSpecificFolderPath(reportType, baseDir, dateContext, _configuration);

            if (string.IsNullOrEmpty(specificFolder))
            {
                Logger.LogError($"Could not determine specific folder path for raw report output. ReportType: {reportType}, Base: {baseDir}. Using fallback.");
                string reportTypeFolderNameKey = ReportTypeHelper.GetConfigKeyForFolderName(reportType);
                string fullConfigKeyForFolderName = $"{AppConfigKeys.OperationalParameters.ReportTypeFolderNames.Base}:{reportTypeFolderNameKey}";
                string reportTypeFolderName = _configuration.GetValue<string>(fullConfigKeyForFolderName, reportTypeFolderNameKey + " Reports")!;
                specificFolder = Path.Combine(baseDir, reportTypeFolderName);
                try { Directory.CreateDirectory(specificFolder); }
                catch (Exception ex) { Logger.LogError($"Failed to create fallback directory for raw report output '{specificFolder}': {ex.Message}"); return null; }
            }

            string sanitizedReportName = string.Join("_", (reportNameForFileName ?? "Report").Split(Path.GetInvalidFileNameChars()));
            string fileName = $"{dateContext:yyyyMMdd}_{sanitizedReportName}_Raw.xlsx";
            if (reportType == ReportType.Daily5Day1k)
            {
                fileName = $"{dateContext:yyyyMMdd}_{sanitizedReportName}_5Day1k_Raw.xlsx";
            }
            else if (reportType == ReportType.Custom)
            {
                fileName = $"{dateContext:yyyyMMdd}_{DateTime.Now:HHmmss}_{sanitizedReportName}_Custom_Raw.xlsx";
            }

            try
            {
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
            string baseDir = TemplateBaseDirectory; // This property also respects user-profile relative logic
            if (string.IsNullOrEmpty(baseDir))
            {
                Logger.LogError("TemplateBaseDirectory is not configured or resolved correctly. Cannot determine Excel template path.");
                return null;
            }

            string templateName = reportType switch
            {
                ReportType.Monthly or ReportType.Quarterly or ReportType.Annual or ReportType.Custom
                    => "TEMPLATE_Estimate Success Rate_Monthly.xlsx",
                _ => "TEMPLATE_Estimate Success Rate.xlsx"
            };

            try
            {
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
            if (string.IsNullOrEmpty(AppSettingsDirectory) || string.IsNullOrEmpty(ReportDefinitionsFileName))
            {
                Logger.LogError("Cannot determine report definitions file path: AppSettingsDirectory or ReportDefinitionsFileName is missing.");
                return null;
            }
            try
            {
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
            bool crystalReportFileExists = !string.IsNullOrEmpty(CrystalReportRptFilePath) && File.Exists(CrystalReportRptFilePath);
            bool wrapperExeFileExists = !string.IsNullOrEmpty(WrapperExecutablePath) && File.Exists(WrapperExecutablePath);

            if (!crystalReportFileExists) Logger.LogWarning($"Essential Config Check: Crystal Report file not found or path invalid: '{CrystalReportRptFilePath}' (from {AppConfigKeys.Paths.CrystalReportRptFile})");
            if (!wrapperExeFileExists) Logger.LogWarning($"Essential Config Check: Wrapper EXE not found or path invalid: '{WrapperExecutablePath}' (from {AppConfigKeys.Paths.WrapperExecutable})");

            return crystalReportFileExists && wrapperExeFileExists;
        }

        /// <inheritdoc/>
        public string GetUserSpecificLogDirectory()
        {
            string effectiveBaseLogDir = LogDirectoryBase;
            if (string.IsNullOrEmpty(effectiveBaseLogDir))
            {
                effectiveBaseLogDir = FallbackLogDirectory;
                Logger.LogWarning($"GetUserSpecificLogDirectory: Primary log directory base is empty or invalid. Using fallback: '{effectiveBaseLogDir}'.");
            }
            if (string.IsNullOrEmpty(effectiveBaseLogDir))
            {
                Logger.LogWarning("GetUserSpecificLogDirectory: Effective base log directory (including fallback) is empty. Using emergency fallback in Temp.");
                effectiveBaseLogDir = Path.Combine(Path.GetTempPath(), "QCRA_EmergencyLogs");
            }

            string sanitizedUserName = string.Join("_", Environment.UserName.Split(Path.GetInvalidFileNameChars()));
            return Path.Combine(effectiveBaseLogDir, sanitizedUserName);
        }
        #endregion

        #region Private Helper Methods
        /// <summary>
        /// Resolves a path string from configuration.
        /// Handles defaults, environment variable expansion, and normalization.
        /// </summary>
        private string? ResolvePath(
            string? configuredPath,
            string pathKeyNameForLogging,
            bool isDirectory,
            bool allowEnvironmentVariables = false,
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
                    Logger.LogDebug($"{logPrefix}: Configured path was empty, using default absolute path: '{pathValueToProcess}'");
                }
                else if (!string.IsNullOrWhiteSpace(defaultRelativePathToUserProfile))
                {
                    if (string.IsNullOrEmpty(_userProfilePath))
                    {
                        Logger.LogError($"{logPrefix}: User profile path is not available. Cannot resolve default relative path.");
                        return null;
                    }
                    try
                    {
                        pathValueToProcess = Path.Combine(_userProfilePath, defaultRelativePathToUserProfile.TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
                        Logger.LogDebug($"{logPrefix}: Configured path was empty, using default user-profile relative path '{defaultRelativePathToUserProfile}' -> '{pathValueToProcess}'");
                    }
                    catch (ArgumentException ex)
                    {
                        Logger.LogWarning($"{logPrefix}: Invalid characters in defaultRelativePathToUserProfile '{defaultRelativePathToUserProfile}'. Error: {ex.Message}");
                        return null;
                    }
                }
                else
                {
                    Logger.LogTrace($"{logPrefix}: Configured path is null or empty, and no defaults provided. Returning null for original input: '{configuredPath}'.");
                    return null;
                }
            }

            if (string.IsNullOrWhiteSpace(pathValueToProcess))
            {
                Logger.LogTrace($"{logPrefix}: Path value is still empty after default handling for original input: '{configuredPath}'. Returning null.");
                return null;
            }
            string resolvedPath = pathValueToProcess;

            if (allowEnvironmentVariables && resolvedPath.Contains('%'))
            {
                try
                {
                    resolvedPath = Environment.ExpandEnvironmentVariables(resolvedPath);
                    Logger.LogTrace($"{logPrefix}: Expanded env vars in '{pathValueToProcess}' to '{resolvedPath}'.");
                }
                catch (ArgumentException ex)
                {
                    Logger.LogWarning($"{logPrefix}: Error expanding env vars for '{pathValueToProcess}': {ex.Message}.");
                    return null;
                }
            }

            if (!wasConfiguredPathInitiallyEmpty &&
                treatAsUserProfileRelativeIfRelativeAndConfigured &&
                !Path.IsPathRooted(resolvedPath) &&
                !resolvedPath.StartsWith(@"\\", StringComparison.Ordinal))
            {
                if (string.IsNullOrEmpty(_userProfilePath))
                {
                    Logger.LogError($"{logPrefix}: User profile path not available. Cannot resolve '{configuredPath}' as user-profile relative.");
                    return null;
                }
                try
                {
                    resolvedPath = Path.Combine(_userProfilePath, resolvedPath.TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
                    Logger.LogDebug($"{logPrefix}: Configured path '{configuredPath}' was relative and marked for user-profile; resolved to '{resolvedPath}'.");
                }
                catch (ArgumentException ex)
                {
                    Logger.LogWarning($"{logPrefix}: Invalid chars combining user profile with '{configuredPath}'. Error: {ex.Message}");
                    return null;
                }
            }

            if (resolvedPath.StartsWith(@"\") && !resolvedPath.StartsWith(@"\\"))
            {
                Logger.LogWarning($"{logPrefix}: Path '{resolvedPath}' (from configured value '{configuredPath}') starts with a single backslash. If this is intended as a UNC path, it MUST start with '\\\\'. Path.GetFullPath will likely resolve it against the current drive's root (e.g., C:\\{resolvedPath.Substring(1)}).");
            }

            try
            {
                string finalPath = Path.GetFullPath(resolvedPath);
                Logger.LogInfo($"{logPrefix}: Input='{configuredPath}', ProcessedInput='{resolvedPath}', FinalResolvedPath='{finalPath}'.");
                return finalPath;
            }
            catch (ArgumentException ex)
            {
                Logger.LogWarning($"{logPrefix}: Invalid path characters/format for '{resolvedPath}' (Original: '{configuredPath}'). Error: {ex.Message}");
                return null;
            }
            catch (PathTooLongException ex)
            {
                Logger.LogWarning($"{logPrefix}: Path too long for '{resolvedPath}' (Original: '{configuredPath}'). Error: {ex.Message}");
                return null;
            }
            catch (NotSupportedException ex)
            {
                Logger.LogWarning($"{logPrefix}: Path format not supported for '{resolvedPath}' (Original: '{configuredPath}'). Error: {ex.Message}");
                return null;
            }
            catch (Exception ex)
            {
                Logger.LogError($"{logPrefix}: Unexpected error fully resolving path for '{resolvedPath}' (Original: '{configuredPath}'): {ex.Message}", ex);
                return null;
            }
        }

        /// <summary>
        /// Gets the user's profile directory path.
        /// </summary>
        /// <returns>The full path to the user's profile directory.</returns>
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
        /// <returns>The directory path of `appsettings.json`, or the application's base directory as a fallback.</returns>
        private string DetermineAppSettingsDirectory()
        {
            string basePath = "\\\\harlow.local\\DFS\\IT Department\\Applications\\Development 2025\\QuoteConversionReportAutomation\\conversionTest";
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
            Logger.LogWarning($"DetermineAppSettingsDirectory: Could not find appsettings.json in '{basePath}' or its parent. Defaulting to BaseDirectory. This might affect locating co-located config files.");
            return basePath;
        }
        #endregion
    }
}
