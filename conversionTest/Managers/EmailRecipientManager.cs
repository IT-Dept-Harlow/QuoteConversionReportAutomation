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
    /// considering both application defaults and user-defined overrides.
    /// </summary>
    public class EmailRecipientManager
    {
        private readonly IConfiguration _appConfiguration;
        private UserEmailSettings _userOverrides;
        private readonly string _userSettingsFilePath;
        private static readonly object _fileLock = new object();

        // Constants for configuration keys from appsettings.json
        private const string ProdAutoRunDailyToKey = "settings:ProductionEmails:AutoRunDailyTo";
        private const string ProdAutoRunDailyCCKey = "settings:ProductionEmails:AutoRunDailyCC";
        private const string ProdFemiToKey = "settings:ProductionEmails:FemiTo";
        private const string ProdFemiCCKey = "settings:ProductionEmails:FemiCC";
        private const string ProdTeamToKey = "settings:ProductionEmails:TeamTo"; // Array
        private const string ProdTeamCCKey = "settings:ProductionEmails:TeamCC"; // Array
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

            // Define path for user-specific settings
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string companyFolder = "HarlowSolutions";
            string appFolder = "QuoteConversionReportAutomation";
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
                            return settings;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error loading user email overrides from '{_userSettingsFilePath}': {ex.Message}", ex);
            }
            Logger.LogInfo("No user email overrides found or file was empty/invalid. Using application defaults.");
            return new UserEmailSettings(); // Return empty settings if file not found or error
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
                string directoryPath = Path.GetDirectoryName(_userSettingsFilePath);
                if (!string.IsNullOrEmpty(directoryPath) && !Directory.Exists(directoryPath))
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
                _userOverrides = settingsToSave; // Update in-memory cache
                Logger.LogInfo($"User email overrides saved to '{_userSettingsFilePath}'.");
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error saving user email overrides to '{_userSettingsFilePath}': {ex.Message}", ex);
                // Optionally, re-throw or handle more gracefully (e.g., inform the user)
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
                _userOverrides = new UserEmailSettings(); // Reset in-memory cache to empty
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
            var effective = new UserEmailSettings();

            // Helper to get a list from config or override
            List<string> GetList(List<string>? userOverride, string appConfigKey, bool isArrayInConfig = false)
            {
                if (userOverride != null && userOverride.Any()) return new List<string>(userOverride);
                if (isArrayInConfig) return GetStringListFromAppConfig(appConfigKey) ?? new List<string>();
                string? singleValue = _appConfiguration[appConfigKey];
                return !string.IsNullOrWhiteSpace(singleValue) ? [singleValue] : new List<string>();
            }

            // Helper to get a single string from config or override
            string GetSingle(string? userOverride, string appConfigKey)
            {
                return !string.IsNullOrWhiteSpace(userOverride) ? userOverride : (_appConfiguration[appConfigKey] ?? string.Empty);
            }

            effective.ProdAutoRunDailyTo = GetList(_userOverrides.ProdAutoRunDailyTo, ProdAutoRunDailyToKey);
            effective.ProdAutoRunDailyCC = GetList(_userOverrides.ProdAutoRunDailyCC, ProdAutoRunDailyCCKey);
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
        /// Determines the final To and CC email recipient lists based on report context,
        /// applying user overrides if they exist.
        /// </summary>
        /// <param name="reportTypeIndex">The index of the report type (e.g., Form1.DailyReportIndex).</param>
        /// <param name="isFemiOnlyChecked">Whether the "Send to Femi Only" checkbox is checked.</param>
        /// <param name="isDebugBuild">True if the application is running in a DEBUG build.</param>
        /// <returns>A tuple containing (List<string> To, List<string> Cc).</returns>
        public (List<string> To, List<string> Cc) GetRecipients(int reportTypeIndex, bool isFemiOnlyChecked, bool isDebugBuild)
        {
            Logger.LogTrace("EmailRecipientManager: Entering GetRecipients...");
            UserEmailSettings currentSettings = GetCurrentEffectiveSettings(); // Gets merged settings

            List<string> toAddresses = new List<string>();
            List<string> ccAddresses = new List<string>();

            // Corresponds to Form1.DailyReportIndex
            const int DailyReportIndex = 0;

            if (reportTypeIndex == DailyReportIndex && !isDebugBuild)
            {
                // Special rule for Daily Release
                toAddresses.AddRange(currentSettings.ProdAutoRunDailyTo ?? Enumerable.Empty<string>());
                ccAddresses.AddRange(currentSettings.ProdAutoRunDailyCC ?? Enumerable.Empty<string>());
                Logger.LogInfo("EmailRecipientManager: RELEASE Build & Daily Report. Using ProdAutoRunDaily recipients.");
            }
            else
            {
                if (isDebugBuild)
                {
                    Logger.LogInfo("EmailRecipientManager: DEBUG Build. Using debug email recipients.");
                    if (!string.IsNullOrWhiteSpace(currentSettings.DebugTo)) toAddresses.Add(currentSettings.DebugTo);

                    if (isFemiOnlyChecked)
                    {
                        Logger.LogDebug("EmailRecipientManager: DEBUG Build: Femi checkbox CHECKED. Adding DebugCC1 and DebugCC2.");
                        if (!string.IsNullOrWhiteSpace(currentSettings.DebugCC1)) ccAddresses.Add(currentSettings.DebugCC1);
                        if (!string.IsNullOrWhiteSpace(currentSettings.DebugCC2)) ccAddresses.Add(currentSettings.DebugCC2);
                    }
                    else
                    {
                        Logger.LogDebug("EmailRecipientManager: DEBUG Build: Femi checkbox NOT CHECKED. Adding DebugCC1 only.");
                        if (!string.IsNullOrWhiteSpace(currentSettings.DebugCC1)) ccAddresses.Add(currentSettings.DebugCC1);
                    }
                }
                else // RELEASE Build Recipients (for non-Daily reports)
                {
                    Logger.LogInfo($"EmailRecipientManager: RELEASE Build (Non-Daily/Custom): SendToFemiOnly = {isFemiOnlyChecked}");
                    if (isFemiOnlyChecked)
                    {
                        toAddresses.AddRange(currentSettings.ProdFemiTo ?? Enumerable.Empty<string>());
                        ccAddresses.AddRange(currentSettings.ProdFemiCC ?? Enumerable.Empty<string>());
                        Logger.LogInfo("EmailRecipientManager: Sending to Femi list (ProdFemiTo/CC).");
                    }
                    else
                    {
                        toAddresses.AddRange(currentSettings.ProdTeamTo ?? Enumerable.Empty<string>());
                        ccAddresses.AddRange(currentSettings.ProdTeamCC ?? Enumerable.Empty<string>());
                        Logger.LogInfo("EmailRecipientManager: Sending to Team list (ProdTeamTo/CC).");
                    }
                }
            }

            // Clean up lists: remove empty entries and duplicates
            toAddresses = toAddresses.Where(e => !string.IsNullOrWhiteSpace(e)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            ccAddresses = ccAddresses.Where(e => !string.IsNullOrWhiteSpace(e)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

            // Ensure CCs are not also in To
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
            return _appConfiguration.GetSection(key).Get<List<string>>()?
                .Select(e => e.Trim())
                .Where(e => !string.IsNullOrWhiteSpace(e))
                .ToList();
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
            if (emails == null) return true;

            bool allValid = true;
            foreach (var emailStr in emails)
            {
                if (string.IsNullOrWhiteSpace(emailStr)) continue; // Skip empty entries

                // Split if multiple emails are in one string (comma/semicolon separated)
                var individualEmails = emailStr.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var singleEmail in individualEmails)
                {
                    string trimmedEmail = singleEmail.Trim();
                    if (!string.IsNullOrWhiteSpace(trimmedEmail) && !EmailUtility.IsValidEmail(trimmedEmail))
                    {
                        allValid = false;
                        invalidEmails.Add(trimmedEmail);
                    }
                }
            }
            return allValid;
        }
    }
}
