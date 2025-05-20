// EmailRecipientManager.cs
// Ensure this namespace matches your project structure
namespace QuoteConversionReportAutomation.Managers
{
    using Microsoft.Extensions.Configuration;
    using QuoteConversionReportAutomation.Helpers; // For EmailUtility if IsValidEmail is there
    using QuoteConversionReportAutomation.Models;
    using QuoteConversionReportAutomation.Services.Logging;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text.Json; 

    /// <summary>
    /// Manages loading, saving, and providing email recipient lists,
    /// considering both application defaults and user-defined overrides.
    /// Handles different recipient lists for various report types and run contexts (manual/auto, debug/release).
    /// </summary>
    public class EmailRecipientManager
    {
        private readonly IConfiguration _appConfiguration;
        private UserEmailSettings _userOverrides;
        private readonly string _userSettingsFilePath;
        private static readonly object _fileLock = new object();

        // --- Report Type Indices (Must match Form1.cs) ---
        private const int DailyReportIndex = 0;
        private const int NewDailyReportOver1kIndex = 1; // "Daily (5days >= £1000)"
        // Other indices (Weekly, Monthly, etc.) are implicitly handled by the logic for non-specific daily types.

        // Constants for configuration keys from appsettings.json
        private const string ProdAutoRunDailyToKey = "settings:ProductionEmails:AutoRunDailyTo";
        private const string ProdAutoRunDailyCCKey = "settings:ProductionEmails:AutoRunDailyCC";
        
        // New keys for the "Daily (5days >= £1000)" automated report
        private const string ProdAutoRunDaily5Day1kToKey = "settings:ProductionEmails:AutoRunDaily5Day1kTo";
        private const string ProdAutoRunDaily5Day1kCCKey = "settings:ProductionEmails:AutoRunDaily5Day1kCC";

        private const string ProdFemiToKey = "settings:ProductionEmails:FemiTo";
        private const string ProdFemiCCKey = "settings:ProductionEmails:FemiCC";
        private const string ProdTeamToKey = "settings:ProductionEmails:TeamTo"; 
        private const string ProdTeamCCKey = "settings:ProductionEmails:TeamCC"; 
        private const string DebugToKey = "settings:DebugEmails:To";
        private const string DebugCC1Key = "settings:DebugEmails:CC1";
        private const string DebugCC2Key = "settings:DebugEmails:CC2";


        /// <summary>
        /// Initializes a new instance of the EmailRecipientManager.
        /// </summary>
        /// <param name="appConfiguration">The application's IConfiguration instance.</param>
        public EmailRecipientManager(IConfiguration appConfiguration)
        {
            _appConfiguration = appConfiguration ?? throw new ArgumentNullException(nameof(appConfiguration));

            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string companyFolder = "HarlowSolutions"; // Consistent with your company/solution name
            string appFolder = "QuoteConversionReportAutomation"; // Specific application folder
            _userSettingsFilePath = Path.Combine(appDataPath, companyFolder, appFolder, "user_email_settings.json");

            _userOverrides = LoadUserOverrides();
            Logger.LogInfo($"EmailRecipientManager initialized. User overrides loaded from: {_userSettingsFilePath}");
        }

        /// <summary>
        /// Loads user-defined email recipient overrides from the JSON file.
        /// If the file doesn't exist or is invalid, returns a new empty UserEmailSettings object.
        /// </summary>
        private UserEmailSettings LoadUserOverrides()
        {
            try
            {
                if (File.Exists(_userSettingsFilePath))
                {
                    string json;
                    lock (_fileLock)
                    {
                        json = File.ReadAllText(_userSettingsFilePath);
                    }
                    if (!string.IsNullOrWhiteSpace(json))
                    {
                        var settings = JsonSerializer.Deserialize<UserEmailSettings>(json);
                        if (settings != null)
                        {
                            Logger.LogInfo("Successfully loaded user email overrides.");
                            // Ensure lists are not null after deserialization
                            settings.ProdAutoRunDailyTo ??= new List<string>();
                            settings.ProdAutoRunDailyCC ??= new List<string>();
                            settings.ProdAutoRunDaily5Day1kTo ??= new List<string>();
                            settings.ProdAutoRunDaily5Day1kCC ??= new List<string>();
                            settings.ProdFemiTo ??= new List<string>();
                            settings.ProdFemiCC ??= new List<string>();
                            settings.ProdTeamTo ??= new List<string>();
                            settings.ProdTeamCC ??= new List<string>();
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
            return new UserEmailSettings(); 
        }

        /// <summary>
        /// Saves the provided user email settings to the JSON file.
        /// </summary>
        /// <param name="settingsToSave">The UserEmailSettings object to save.</param>
        public void SaveUserOverrides(UserEmailSettings settingsToSave)
        {
            if (settingsToSave == null) throw new ArgumentNullException(nameof(settingsToSave));
            try
            {
                string directoryPath = Path.GetDirectoryName(_userSettingsFilePath)!; // Null forgiveness, path should be valid
                if (!Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                    Logger.LogInfo($"Created directory for user email settings: {directoryPath}");
                }

                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(settingsToSave, options);
                lock (_fileLock)
                {
                    File.WriteAllText(_userSettingsFilePath, json);
                }
                _userOverrides = settingsToSave; 
                Logger.LogInfo($"User email overrides saved to '{_userSettingsFilePath}'.");
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error saving user email overrides to '{_userSettingsFilePath}': {ex.Message}", ex);
                throw;
            }
        }

        /// <summary>
        /// Clears all user-defined email overrides, reverting to application defaults.
        /// </summary>
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

        /// <summary>
        /// Gets the current effective email settings, merging user overrides with application defaults.
        /// User overrides take precedence.
        /// </summary>
        /// <returns>A UserEmailSettings object representing the effective settings.</returns>
        public UserEmailSettings GetCurrentEffectiveSettings()
        {
            var effective = new UserEmailSettings(); // Ensures all lists are initialized

            // Helper to get a list from config or override, handling nulls from _userOverrides
            List<string> GetList(List<string>? userOverrideList, string appConfigKey, bool isArrayInConfig = false, List<string>? defaultList = null)
            {
                if (userOverrideList != null && userOverrideList.Any()) return new List<string>(userOverrideList);
                
                var appConfigValues = isArrayInConfig 
                    ? GetStringListFromAppConfig(appConfigKey) 
                    : (!string.IsNullOrWhiteSpace(_appConfiguration[appConfigKey]) ? new List<string> { _appConfiguration[appConfigKey]! } : new List<string>());

                if (appConfigValues != null && appConfigValues.Any()) return appConfigValues;
                return defaultList ?? new List<string>();
            }

            // Helper to get a single string from config or override
            string GetSingle(string? userOverride, string appConfigKey, string defaultString = "")
            {
                return !string.IsNullOrWhiteSpace(userOverride) ? userOverride : (_appConfiguration[appConfigKey] ?? defaultString);
            }

            effective.ProdAutoRunDailyTo = GetList(_userOverrides.ProdAutoRunDailyTo, ProdAutoRunDailyToKey);
            effective.ProdAutoRunDailyCC = GetList(_userOverrides.ProdAutoRunDailyCC, ProdAutoRunDailyCCKey);
            
            // For the new report, provide an initial hardcoded default if not in appsettings or user overrides
            effective.ProdAutoRunDaily5Day1kTo = GetList(_userOverrides.ProdAutoRunDaily5Day1kTo, ProdAutoRunDaily5Day1kToKey, defaultList: new List<string> { "chrisp@harlowsolutions.co.uk" });
            effective.ProdAutoRunDaily5Day1kCC = GetList(_userOverrides.ProdAutoRunDaily5Day1kCC, ProdAutoRunDaily5Day1kCCKey);

            effective.ProdFemiTo = GetList(_userOverrides.ProdFemiTo, ProdFemiToKey);
            effective.ProdFemiCC = GetList(_userOverrides.ProdFemiCC, ProdFemiCCKey);
            effective.ProdTeamTo = GetList(_userOverrides.ProdTeamTo, ProdTeamToKey, true);
            effective.ProdTeamCC = GetList(_userOverrides.ProdTeamCC, ProdTeamCCKey, true);

            effective.DebugTo = GetSingle(_userOverrides.DebugTo, DebugToKey);
            effective.DebugCC1 = GetSingle(_userOverrides.DebugCC1, DebugCC1Key);
            effective.DebugCC2 = GetSingle(_userOverrides.DebugCC2, DebugCC2Key);

            return effective;
        }


        /// <summary>
        /// Determines the final To and CC email recipient lists based on report context.
        /// </summary>
        /// <param name="reportTypeIndex">The index of the report type (e.g., Form1.DailyReportIndex).</param>
        /// <param name="isFemiOnlyChecked">Whether the "Send to Femi Only" checkbox is checked (relevant for manual non-daily reports).</param>
        /// <param name="isDebugBuild">True if the application is running in a DEBUG build.</param>
        /// <param name="isAutoRunContext">True if this is called from an automated run context.</param>
        /// <returns>A tuple containing (List<string> To, List<string> Cc).</returns>
        public (List<string> To, List<string> Cc) GetRecipients(
            int reportTypeIndex, 
            bool isFemiOnlyChecked, 
            bool isDebugBuild,
            bool isAutoRunContext = false) // New parameter with default
        {
            Logger.LogTrace($"EmailRecipientManager: GetRecipients called. ReportType: {reportTypeIndex}, FemiOnly: {isFemiOnlyChecked}, Debug: {isDebugBuild}, AutoRun: {isAutoRunContext}");
            UserEmailSettings settings = GetCurrentEffectiveSettings(); 

            List<string> toAddresses = new List<string>();
            List<string> ccAddresses = new List<string>();

            if (isDebugBuild)
            {
                Logger.LogInfo("EmailRecipientManager: DEBUG Build. Using debug email recipients.");
                if (!string.IsNullOrWhiteSpace(settings.DebugTo)) toAddresses.Add(settings.DebugTo);
                // For debug, CC logic can be simpler or follow a specific debug rule.
                // Current logic: if FemiOnly is checked in UI (even for debug), add both CCs, else add CC1.
                // This might be counter-intuitive for debug. Let's simplify: always add CC1 and CC2 if present.
                if (!string.IsNullOrWhiteSpace(settings.DebugCC1)) ccAddresses.Add(settings.DebugCC1);
                if (!string.IsNullOrWhiteSpace(settings.DebugCC2)) ccAddresses.Add(settings.DebugCC2);
            }
            else // RELEASE Build
            {
                if (isAutoRunContext)
                {
                    Logger.LogInfo($"EmailRecipientManager: RELEASE Build & AutoRun Context. ReportType: {reportTypeIndex}");
                    if (reportTypeIndex == DailyReportIndex) // Standard Daily AutoRun
                    {
                        toAddresses.AddRange(settings.ProdAutoRunDailyTo ?? Enumerable.Empty<string>());
                        ccAddresses.AddRange(settings.ProdAutoRunDailyCC ?? Enumerable.Empty<string>());
                        Logger.LogInfo("Using ProdAutoRunDaily recipients for standard daily auto-run.");
                    }
                    else if (reportTypeIndex == NewDailyReportOver1kIndex) // "Daily (5days >= £1000)" AutoRun
                    {
                        toAddresses.AddRange(settings.ProdAutoRunDaily5Day1kTo ?? Enumerable.Empty<string>());
                        ccAddresses.AddRange(settings.ProdAutoRunDaily5Day1kCC ?? Enumerable.Empty<string>());
                        Logger.LogInfo("Using ProdAutoRunDaily5Day1k recipients for new daily (5day >=1k) auto-run.");
                    }
                    else
                    {
                        Logger.LogWarning($"AutoRun context for unexpected report type: {reportTypeIndex}. No specific auto-run recipients defined. Email might not send if lists are empty.");
                        // Fallback to empty or default team if necessary, but ideally auto-run is only for defined types.
                    }
                }
                else // Manual Run (RELEASE Build)
                {
                    Logger.LogInfo($"EmailRecipientManager: RELEASE Build & Manual Run Context. ReportType: {reportTypeIndex}, FemiOnly: {isFemiOnlyChecked}");
                    if (reportTypeIndex == DailyReportIndex) 
                    {
                        // Manual run of Standard Daily uses the AutoRunDaily list (as per original logic)
                        toAddresses.AddRange(settings.ProdAutoRunDailyTo ?? Enumerable.Empty<string>());
                        ccAddresses.AddRange(settings.ProdAutoRunDailyCC ?? Enumerable.Empty<string>());
                        Logger.LogInfo("Using ProdAutoRunDaily recipients for manual run of standard daily report.");
                    }
                    // For "Daily (5days >= £1000)" (manual) and other non-standard daily reports (Weekly, Monthly etc.)
                    else if (reportTypeIndex == NewDailyReportOver1kIndex || 
                             reportTypeIndex != DailyReportIndex) // Catches Weekly, Monthly, Annual, Custom too
                    {
                        if (isFemiOnlyChecked)
                        {
                            toAddresses.AddRange(settings.ProdFemiTo ?? Enumerable.Empty<string>());
                            ccAddresses.AddRange(settings.ProdFemiCC ?? Enumerable.Empty<string>());
                            Logger.LogInfo("Using ProdFemiTo/CC recipients.");
                        }
                        else
                        {
                            toAddresses.AddRange(settings.ProdTeamTo ?? Enumerable.Empty<string>());
                            ccAddresses.AddRange(settings.ProdTeamCC ?? Enumerable.Empty<string>());
                            Logger.LogInfo("Using ProdTeamTo/CC recipients.");
                        }
                    }
                }
            }

            // Clean up lists: remove empty entries and duplicates, ensure CCs are not in To
            toAddresses = toAddresses.Where(e => !string.IsNullOrWhiteSpace(e)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            ccAddresses = ccAddresses.Where(e => !string.IsNullOrWhiteSpace(e)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            ccAddresses = ccAddresses.Except(toAddresses, StringComparer.OrdinalIgnoreCase).ToList();

            Logger.LogDebug($"EmailRecipientManager: Final To Addresses: {string.Join("; ", toAddresses)}");
            Logger.LogDebug($"EmailRecipientManager: Final CC Addresses: {string.Join("; ", ccAddresses)}");
            Logger.LogTrace("EmailRecipientManager: Exiting GetRecipients.");
            return (toAddresses, ccAddresses);
        }

        /// <summary>
        /// Helper to read a configuration value and split it into a list of strings.
        /// Used for array-like settings in appsettings.json.
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
                return null;
            }
        }

        /// <summary>
        /// Validates a list of email addresses for format.
        /// </summary>
        /// <param name="emails">A list of email strings.</param>
        /// <param name="invalidEmails">Output list of emails that failed validation.</param>
        /// <returns>True if all emails are valid, false otherwise.</returns>
        public static bool ValidateEmailAddresses(IEnumerable<string> emails, out List<string> invalidEmails)
        {
            invalidEmails = new List<string>();
            if (emails == null) return true; // No emails to validate is considered valid.

            bool allValid = true;
            foreach (var emailStr in emails.Where(e => !string.IsNullOrWhiteSpace(e))) // Process only non-empty strings
            {
                // Split if multiple emails are in one string (comma/semicolon separated)
                var individualEmails = emailStr.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var singleEmail in individualEmails)
                {
                    string trimmedEmail = singleEmail.Trim();
                    if (!string.IsNullOrWhiteSpace(trimmedEmail)) // Check again after trim
                    {
                         // Assuming EmailUtility.IsValidEmail is a static method in QuoteConversionReportAutomation.Helpers
                         // If it's an instance method or in a different class, adjust accordingly.
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
