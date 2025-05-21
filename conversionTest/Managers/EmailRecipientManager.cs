// EmailRecipientManager.cs
namespace QuoteConversionReportAutomation.Managers
{
    using Microsoft.Extensions.Configuration;
    using QuoteConversionReportAutomation.Helpers;
    using QuoteConversionReportAutomation.Models;
    using QuoteConversionReportAutomation.Services.Logging;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text.Json;

    /// <summary>
    /// Manages loading, saving, and providing email recipient lists,
    /// considering both application defaults (from appsettings.json, expected as arrays) 
    /// and user-defined overrides.
    /// Handles different recipient lists for various report types and run contexts (manual/auto, debug/release).
    /// </summary>
    public class EmailRecipientManager
    {
        private readonly IConfiguration _appConfiguration;
        private UserEmailSettings _userOverrides;
        private readonly string _userSettingsFilePath;
        private static readonly object _fileLock = new object();

        // Report Type Indices (Must match Form1.cs and AutoReportDefinition.ReportTypeIndex)
        private const int DailyReportIndex = 0;
        private const int NewDailyReportOver1kIndex = 1;
        private const int WeeklyReportIndex = 2;

        // Constants for configuration keys from appsettings.json
        // These keys now point to JSON arrays in appsettings.json
        private const string ProdAutoRunDailyToKey = "settings:ProductionEmails:AutoRunDailyTo";
        private const string ProdAutoRunDailyCCKey = "settings:ProductionEmails:AutoRunDailyCC";
        private const string ProdManualRunDailyToKey = "settings:ProductionEmails:ManualRunDailyTo";
        private const string ProdManualRunDailyCCKey = "settings:ProductionEmails:ManualRunDailyCC";
        private const string ProdAutoRunDaily5Day1kToKey = "settings:ProductionEmails:AutoRunDaily5Day1kTo";
        private const string ProdAutoRunDaily5Day1kCCKey = "settings:ProductionEmails:AutoRunDaily5Day1kCC";
        private const string ProdAutoRunWeeklyToKey = "settings:ProductionEmails:AutoRunWeeklyTo";
        private const string ProdAutoRunWeeklyCCKey = "settings:ProductionEmails:AutoRunWeeklyCC";
        private const string ProdFemiToKey = "settings:ProductionEmails:FemiTo";
        private const string ProdFemiCCKey = "settings:ProductionEmails:FemiCC";
        private const string ProdTeamToKey = "settings:ProductionEmails:TeamTo";
        private const string ProdTeamCCKey = "settings:ProductionEmails:TeamCC";
        private const string DebugToKey = "settings:DebugEmails:To";
        private const string DebugCC1Key = "settings:DebugEmails:CC1";
        private const string DebugCC2Key = "settings:DebugEmails:CC2";

        public EmailRecipientManager(IConfiguration appConfiguration)
        {
            _appConfiguration = appConfiguration ?? throw new ArgumentNullException(nameof(appConfiguration));
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string companyFolder = "HarlowSolutions";
            string appFolder = "QuoteConversionReportAutomation";
            _userSettingsFilePath = Path.Combine(appDataPath, companyFolder, appFolder, "user_email_settings.json");
            _userOverrides = LoadUserOverrides();
            Logger.LogInfo($"EmailRecipientManager initialized. User overrides loaded from: {_userSettingsFilePath}");
        }

        private UserEmailSettings LoadUserOverrides()
        {
            try
            {
                if (File.Exists(_userSettingsFilePath))
                {
                    string json;
                    lock (_fileLock) { json = File.ReadAllText(_userSettingsFilePath); }
                    if (!string.IsNullOrWhiteSpace(json))
                    {
                        var settings = JsonSerializer.Deserialize<UserEmailSettings>(json);
                        if (settings != null)
                        {
                            Logger.LogInfo("Successfully loaded user email overrides.");
                            // Ensure all list properties are initialized to prevent null reference issues.
                            settings.ProdAutoRunDailyTo ??= new List<string>();
                            settings.ProdAutoRunDailyCC ??= new List<string>();
                            settings.ProdManualRunDailyTo ??= new List<string>();
                            settings.ProdManualRunDailyCC ??= new List<string>();
                            settings.ProdAutoRunDaily5Day1kTo ??= new List<string>();
                            settings.ProdAutoRunDaily5Day1kCC ??= new List<string>();
                            settings.ProdAutoRunWeeklyTo ??= new List<string>();
                            settings.ProdAutoRunWeeklyCC ??= new List<string>();
                            settings.ProdFemiTo ??= new List<string>();
                            settings.ProdFemiCC ??= new List<string>();
                            settings.ProdTeamTo ??= new List<string>();
                            settings.ProdTeamCC ??= new List<string>();
                            // Debug fields are single strings, ensure they are not null
                            settings.DebugTo ??= string.Empty;
                            settings.DebugCC1 ??= string.Empty;
                            settings.DebugCC2 ??= string.Empty;
                            return settings;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error loading user email overrides from '{_userSettingsFilePath}': {ex.Message}", ex);
            }
            Logger.LogInfo("No user email overrides found or file was empty/invalid. Using a new UserEmailSettings instance.");
            return new UserEmailSettings(); // Constructor initializes lists and strings
        }

        public void SaveUserOverrides(UserEmailSettings settingsToSave)
        {
            if (settingsToSave == null) throw new ArgumentNullException(nameof(settingsToSave));
            try
            {
                string directoryPath = Path.GetDirectoryName(_userSettingsFilePath)!;
                if (!Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                    Logger.LogInfo($"Created directory for user email settings: {directoryPath}");
                }
                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(settingsToSave, options);
                lock (_fileLock) { File.WriteAllText(_userSettingsFilePath, json); }
                _userOverrides = settingsToSave;
                Logger.LogInfo($"User email overrides saved to '{_userSettingsFilePath}'.");
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error saving user email overrides to '{_userSettingsFilePath}': {ex.Message}", ex);
                throw;
            }
        }

        public void ClearUserOverrides()
        {
            try
            {
                lock (_fileLock)
                {
                    if (File.Exists(_userSettingsFilePath))
                    {
                        File.Delete(_userSettingsFilePath);
                        Logger.LogInfo($"User email overrides file '{_userSettingsFilePath}' deleted.");
                    }
                }
                _userOverrides = new UserEmailSettings();
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error clearing user email overrides file '{_userSettingsFilePath}': {ex.Message}", ex);
                throw;
            }
        }

        public UserEmailSettings GetCurrentEffectiveSettings()
        {
            var effective = new UserEmailSettings();

            // Helper to get a list from config (now always expecting array) or override
            List<string> GetList(List<string>? userOverrideList, string appConfigKey, List<string>? defaultList = null)
            {
                if (userOverrideList != null && userOverrideList.Any())
                {
                    return new List<string>(userOverrideList);
                }
                // Always attempt to read as an array from appsettings.json
                var appConfigValues = GetStringListFromAppConfig(appConfigKey);
                if (appConfigValues != null && appConfigValues.Any())
                {
                    return appConfigValues;
                }
                return defaultList ?? new List<string>();
            }

            // Helper to get a single string (for Debug emails which are still single strings in the model)
            string GetSingleString(string? userOverride, string appConfigKey, string defaultString = "")
            {
                if (!string.IsNullOrWhiteSpace(userOverride)) return userOverride;
                // For Debug emails, appsettings.json now also stores them as arrays of one element.
                // So, we read as a list and take the first element if it exists.
                var listValue = GetStringListFromAppConfig(appConfigKey);
                return listValue?.FirstOrDefault() ?? defaultString;
            }


            effective.ProdAutoRunDailyTo = GetList(_userOverrides.ProdAutoRunDailyTo, ProdAutoRunDailyToKey);
            effective.ProdAutoRunDailyCC = GetList(_userOverrides.ProdAutoRunDailyCC, ProdAutoRunDailyCCKey);
            effective.ProdManualRunDailyTo = GetList(_userOverrides.ProdManualRunDailyTo, ProdManualRunDailyToKey);
            effective.ProdManualRunDailyCC = GetList(_userOverrides.ProdManualRunDailyCC, ProdManualRunDailyCCKey);
            effective.ProdAutoRunDaily5Day1kTo = GetList(_userOverrides.ProdAutoRunDaily5Day1kTo, ProdAutoRunDaily5Day1kToKey, defaultList: new List<string> { "chrisp@harlowsolutions.co.uk" });
            effective.ProdAutoRunDaily5Day1kCC = GetList(_userOverrides.ProdAutoRunDaily5Day1kCC, ProdAutoRunDaily5Day1kCCKey);
            effective.ProdAutoRunWeeklyTo = GetList(_userOverrides.ProdAutoRunWeeklyTo, ProdAutoRunWeeklyToKey);
            effective.ProdAutoRunWeeklyCC = GetList(_userOverrides.ProdAutoRunWeeklyCC, ProdAutoRunWeeklyCCKey);
            effective.ProdFemiTo = GetList(_userOverrides.ProdFemiTo, ProdFemiToKey);
            effective.ProdFemiCC = GetList(_userOverrides.ProdFemiCC, ProdFemiCCKey);
            effective.ProdTeamTo = GetList(_userOverrides.ProdTeamTo, ProdTeamToKey);
            effective.ProdTeamCC = GetList(_userOverrides.ProdTeamCC, ProdTeamCCKey);

            // Debug emails are single strings in the model, but arrays in appsettings.json
            effective.DebugTo = GetSingleString(_userOverrides.DebugTo, DebugToKey);
            effective.DebugCC1 = GetSingleString(_userOverrides.DebugCC1, DebugCC1Key);
            effective.DebugCC2 = GetSingleString(_userOverrides.DebugCC2, DebugCC2Key);

            Logger.LogDebug("GetCurrentEffectiveSettings completed.");
            return effective;
        }

        public (List<string> To, List<string> Cc) GetRecipients(
            int reportTypeIndex,
            bool isFemiOnlyChecked,
            bool isDebugBuild,
            bool isAutoRunContext = false)
        {
            Logger.LogTrace($"EmailRecipientManager: GetRecipients called. ReportType: {reportTypeIndex}, FemiOnly: {isFemiOnlyChecked}, Debug: {isDebugBuild}, AutoRun: {isAutoRunContext}");
            UserEmailSettings settings = GetCurrentEffectiveSettings();
            List<string> toAddresses = new List<string>();
            List<string> ccAddresses = new List<string>();

            if (isDebugBuild)
            {
                Logger.LogInfo("EmailRecipientManager: DEBUG Build. Using debug email recipients.");
                // Debug settings are single strings in the model, but GetCurrentEffectiveSettings reads them from arrays in JSON
                if (!string.IsNullOrWhiteSpace(settings.DebugTo)) toAddresses.Add(settings.DebugTo);
                if (!string.IsNullOrWhiteSpace(settings.DebugCC1)) ccAddresses.Add(settings.DebugCC1);
                if (!string.IsNullOrWhiteSpace(settings.DebugCC2)) ccAddresses.Add(settings.DebugCC2);
            }
            else
            {
                if (isAutoRunContext)
                {
                    Logger.LogInfo($"EmailRecipientManager: RELEASE Build & AutoRun Context. ReportType: {reportTypeIndex}");
                    switch (reportTypeIndex)
                    {
                        case DailyReportIndex:
                            toAddresses.AddRange(settings.ProdAutoRunDailyTo ?? Enumerable.Empty<string>());
                            ccAddresses.AddRange(settings.ProdAutoRunDailyCC ?? Enumerable.Empty<string>());
                            Logger.LogInfo("Using ProdAutoRunDaily recipients for standard daily auto-run.");
                            break;
                        case NewDailyReportOver1kIndex:
                            toAddresses.AddRange(settings.ProdAutoRunDaily5Day1kTo ?? Enumerable.Empty<string>());
                            ccAddresses.AddRange(settings.ProdAutoRunDaily5Day1kCC ?? Enumerable.Empty<string>());
                            Logger.LogInfo("Using ProdAutoRunDaily5Day1k recipients for new daily (5day >=1k) auto-run.");
                            break;
                        case WeeklyReportIndex:
                            toAddresses.AddRange(settings.ProdAutoRunWeeklyTo ?? Enumerable.Empty<string>());
                            ccAddresses.AddRange(settings.ProdAutoRunWeeklyCC ?? Enumerable.Empty<string>());
                            Logger.LogInfo("Using ProdAutoRunWeekly recipients for weekly auto-run.");
                            break;
                        default:
                            Logger.LogWarning($"AutoRun context for unexpected report type: {reportTypeIndex}. No specific auto-run recipients defined. Email might not send if lists are empty.");
                            break;
                    }
                }
                else
                {
                    Logger.LogInfo($"EmailRecipientManager: RELEASE Build & Manual Run Context. ReportType: {reportTypeIndex}, FemiOnly: {isFemiOnlyChecked}");
                    if (reportTypeIndex == DailyReportIndex)
                    {
                        toAddresses.AddRange(settings.ProdManualRunDailyTo ?? Enumerable.Empty<string>());
                        ccAddresses.AddRange(settings.ProdManualRunDailyCC ?? Enumerable.Empty<string>());
                        Logger.LogInfo("Using ProdManualRunDaily recipients for manual run of standard daily report.");
                    }
                    else
                    {
                        if (isFemiOnlyChecked && reportTypeIndex != NewDailyReportOver1kIndex && reportTypeIndex != WeeklyReportIndex)
                        {
                            toAddresses.AddRange(settings.ProdFemiTo ?? Enumerable.Empty<string>());
                            ccAddresses.AddRange(settings.ProdFemiCC ?? Enumerable.Empty<string>());
                            Logger.LogInfo("Using ProdFemiTo/CC recipients.");
                        }
                        else
                        {
                            toAddresses.AddRange(settings.ProdTeamTo ?? Enumerable.Empty<string>());
                            ccAddresses.AddRange(settings.ProdTeamCC ?? Enumerable.Empty<string>());
                            Logger.LogInfo("Using ProdTeamTo/CC recipients for this manual report type.");
                        }
                    }
                }
            }

            toAddresses = toAddresses.Where(e => !string.IsNullOrWhiteSpace(e)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            ccAddresses = ccAddresses.Where(e => !string.IsNullOrWhiteSpace(e)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            ccAddresses = ccAddresses.Except(toAddresses, StringComparer.OrdinalIgnoreCase).ToList();

            Logger.LogDebug($"EmailRecipientManager: Final To Addresses: {string.Join("; ", toAddresses)}");
            Logger.LogDebug($"EmailRecipientManager: Final CC Addresses: {string.Join("; ", ccAddresses)}");
            Logger.LogTrace("EmailRecipientManager: Exiting GetRecipients.");
            return (toAddresses, ccAddresses);
        }

        /// <summary>
        /// Helper to read a configuration value as a list of strings from appsettings.json.
        /// Assumes the configuration key points to a JSON array.
        /// </summary>
        private List<string>? GetStringListFromAppConfig(string key)
        {
            try
            {
                return _appConfiguration.GetSection(key)?.Get<List<string>>()?
                    .Select(e => e.Trim())
                    .Where(e => !string.IsNullOrWhiteSpace(e))
                    .ToList();
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"Could not parse appsetting key '{key}' as a list of strings. Error: {ex.Message}");
                return null; // Return null to indicate parsing failure or missing key
            }
        }

        public static bool ValidateEmailAddresses(IEnumerable<string> emails, out List<string> invalidEmails)
        {
            invalidEmails = new List<string>();
            if (emails == null) return true;

            bool allValid = true;
            foreach (var emailStr in emails.Where(e => !string.IsNullOrWhiteSpace(e)))
            {
                var individualEmails = emailStr.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var singleEmail in individualEmails)
                {
                    string trimmedEmail = singleEmail.Trim();
                    if (!string.IsNullOrWhiteSpace(trimmedEmail))
                    {
                        if (!EmailUtility.IsValidEmail(trimmedEmail))
                        {
                            allValid = false;
                            if (!invalidEmails.Contains(trimmedEmail, StringComparer.OrdinalIgnoreCase))
                            {
                                invalidEmails.Add(trimmedEmail);
                            }
                        }
                    }
                }
            }
            return allValid;
        }
    }
}
