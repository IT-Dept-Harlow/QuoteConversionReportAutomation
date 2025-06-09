#region Using Directives
// System related namespaces
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json; // For System.Text.Json serialization/deserialization.

// Third-party namespaces
using Microsoft.Extensions.Configuration; // For IConfiguration.

// Project specific namespaces
using QuoteConversionReportAutomation.Models;   // For UserEmailSettings, AutoReportDefinition.
using QuoteConversionReportAutomation.Services.Communication;
using QuoteConversionReportAutomation.Services.Logging; // For Logger.
#endregion

namespace QuoteConversionReportAutomation.Managers
{
    /// <summary>
    /// Manages loading, saving, and providing email recipient lists.
    /// It considers both application defaults (from appsettings.json, expected as arrays) 
    /// and user-defined overrides from a local JSON file.
    /// For automated reports, recipient lists are determined by a category key.
    /// </summary>
    public class EmailRecipientManager
    {
        #region Fields and Constants

        private readonly IConfiguration _appConfiguration;
        private UserEmailSettings _userOverrides;
        private readonly string _userSettingsFilePath;
        private static readonly object _fileLock = new object();

        // Report Type Indices (used for manual runs or as fallback if category key is missing)
        private const int DailyReportIndex = 0;
        private const int NewDailyReportOver1kIndex = 1; // Retained for manual context
        private const int WeeklyReportIndex = 2;         // Retained for manual context
        private const int CustomReportIndex = 6;

        // --- Configuration Keys for appsettings.json ---
        // Keys for recipient lists (app defaults)
        private const string ProdManualRunDailyToKey = "settings:ProductionEmails:ManualRunDailyTo";
        private const string ProdManualRunDailyCCKey = "settings:ProductionEmails:ManualRunDailyCC";
        private const string ProdFemiToKey = "settings:ProductionEmails:FemiTo";
        private const string ProdFemiCCKey = "settings:ProductionEmails:FemiCC";
        private const string ProdTeamToKey = "settings:ProductionEmails:TeamTo";
        private const string ProdTeamCCKey = "settings:ProductionEmails:TeamCC";
        private const string ProdManualCustomToKey = "settings:ProductionEmails:ManualCustomTo";
        private const string ProdManualCustomCCKey = "settings:ProductionEmails:ManualCustomCC";

        // Keys for NEW category-based automated report recipients
        private const string AutoRunDailyStandardRecipientsToKey = "settings:ProductionEmails:AutoRunDailyStandardRecipientsTo";
        private const string AutoRunDailyStandardRecipientsCCKey = "settings:ProductionEmails:AutoRunDailyStandardRecipientsCC";
        private const string AutoRunDaily5Day1kRecipientsToKey = "settings:ProductionEmails:AutoRunDaily5Day1kRecipientsTo";
        private const string AutoRunDaily5Day1kRecipientsCCKey = "settings:ProductionEmails:AutoRunDaily5Day1kRecipientsCC";
        private const string AutoRunWeeklyRecipientsToKey = "settings:ProductionEmails:AutoRunWeeklyRecipientsTo";
        private const string AutoRunWeeklyRecipientsCCKey = "settings:ProductionEmails:AutoRunWeeklyRecipientsCC";
        // Add constants for other categories like "AutoRunMonthlyMarketingRecipients" if defined in appsettings.json

        // Debug email keys
        private const string DebugToKey = "settings:DebugEmails:To";
        private const string DebugCC1Key = "settings:DebugEmails:CC1";
        private const string DebugCC2Key = "settings:DebugEmails:CC2";

        #endregion

        #region Constructor
        /// <summary>
        /// Initialises a new instance of the <see cref="EmailRecipientManager"/> class.
        /// Loads user overrides from their specific settings file.
        /// </summary>
        /// <param name="appConfiguration">The application's configuration interface.</param>
        public EmailRecipientManager(IConfiguration appConfiguration)
        {
            _appConfiguration = appConfiguration ?? throw new ArgumentNullException(nameof(appConfiguration));
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string companyFolder = "HarlowSolutions";
            string appFolder = "QuoteConversionReportAutomation";
            _userSettingsFilePath = Path.Combine(appDataPath, companyFolder, appFolder, "user_email_settings.json");

            _userOverrides = LoadUserOverrides();
            Logger.LogInfo($"EmailRecipientManager initialised. User overrides loaded from: '{_userSettingsFilePath}'");
        }
        #endregion

        #region User Overrides Management
        /// <summary>
        /// Loads user-defined email recipient overrides from their local JSON settings file.
        /// </summary>
        /// <returns>A <see cref="UserEmailSettings"/> object with loaded or default empty settings.</returns>
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
                            // Ensure all list properties are initialised to prevent null issues.
                            settings.AutoRunDailyStandardRecipientsTo ??= new List<string>();
                            settings.AutoRunDailyStandardRecipientsCC ??= new List<string>();
                            settings.AutoRunDaily5Day1kRecipientsTo ??= new List<string>();
                            settings.AutoRunDaily5Day1kRecipientsCC ??= new List<string>();
                            settings.AutoRunWeeklyRecipientsTo ??= new List<string>();
                            settings.AutoRunWeeklyRecipientsCC ??= new List<string>();
                            // Initialise other category-based lists if added to UserEmailSettings

                            settings.ProdManualRunDailyTo ??= new List<string>();
                            settings.ProdManualRunDailyCC ??= new List<string>();
                            settings.ProdManualCustomTo ??= new List<string>();
                            settings.ProdManualCustomCC ??= new List<string>();
                            settings.ProdFemiTo ??= new List<string>();
                            settings.ProdFemiCC ??= new List<string>();
                            settings.ProdTeamTo ??= new List<string>();
                            settings.ProdTeamCC ??= new List<string>();

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
            return new UserEmailSettings(); // Constructor initialises all lists.
        }

        /// <summary>
        /// Saves the provided <see cref="UserEmailSettings"/> to the user's local JSON settings file.
        /// </summary>
        /// <param name="settingsToSave">The settings object to save.</param>
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

        /// <summary>
        /// Clears all user-defined email recipient overrides.
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
        #endregion

        #region Recipient Retrieval Logic
        /// <summary>
        /// Gets the current effective email recipient settings by merging user overrides
        /// with application defaults from `appsettings.json`. User overrides take precedence.
        /// </summary>
        /// <returns>A <see cref="UserEmailSettings"/> object representing the effective settings.</returns>
        public UserEmailSettings GetCurrentEffectiveSettings()
        {
            var effective = new UserEmailSettings();

            List<string> GetList(List<string>? userOverrideList, string appConfigKey, List<string>? defaultList = null)
            {
                if (userOverrideList != null && userOverrideList.Any()) return new List<string>(userOverrideList);
                var appConfigValues = GetStringListFromAppConfig(appConfigKey);
                if (appConfigValues != null && appConfigValues.Any()) return appConfigValues;
                return defaultList ?? new List<string>();
            }

            string GetSingleString(string? userOverride, string appConfigKey, string defaultString = "")
            {
                if (!string.IsNullOrWhiteSpace(userOverride)) return userOverride;
                var listValue = GetStringListFromAppConfig(appConfigKey);
                return listValue?.FirstOrDefault() ?? defaultString;
            }

            // Populate new category-based automated report recipient lists
            effective.AutoRunDailyStandardRecipientsTo = GetList(_userOverrides.AutoRunDailyStandardRecipientsTo, AutoRunDailyStandardRecipientsToKey);
            effective.AutoRunDailyStandardRecipientsCC = GetList(_userOverrides.AutoRunDailyStandardRecipientsCC, AutoRunDailyStandardRecipientsCCKey);
            effective.AutoRunDaily5Day1kRecipientsTo = GetList(_userOverrides.AutoRunDaily5Day1kRecipientsTo, AutoRunDaily5Day1kRecipientsToKey);
            effective.AutoRunDaily5Day1kRecipientsCC = GetList(_userOverrides.AutoRunDaily5Day1kRecipientsCC, AutoRunDaily5Day1kRecipientsCCKey);
            effective.AutoRunWeeklyRecipientsTo = GetList(_userOverrides.AutoRunWeeklyRecipientsTo, AutoRunWeeklyRecipientsToKey);
            effective.AutoRunWeeklyRecipientsCC = GetList(_userOverrides.AutoRunWeeklyRecipientsCC, AutoRunWeeklyRecipientsCCKey);
            // Populate others if added, e.g.:
            // effective.AutoRunMonthlyMarketingRecipientsTo = GetList(_userOverrides.AutoRunMonthlyMarketingRecipientsTo, "settings:ProductionEmails:AutoRunMonthlyMarketingRecipientsTo");

            // Populate manual report recipient lists
            effective.ProdManualRunDailyTo = GetList(_userOverrides.ProdManualRunDailyTo, ProdManualRunDailyToKey);
            effective.ProdManualRunDailyCC = GetList(_userOverrides.ProdManualRunDailyCC, ProdManualRunDailyCCKey);
            effective.ProdManualCustomTo = GetList(_userOverrides.ProdManualCustomTo, ProdManualCustomToKey);
            effective.ProdManualCustomCC = GetList(_userOverrides.ProdManualCustomCC, ProdManualCustomCCKey);
            effective.ProdFemiTo = GetList(_userOverrides.ProdFemiTo, ProdFemiToKey);
            effective.ProdFemiCC = GetList(_userOverrides.ProdFemiCC, ProdFemiCCKey);
            effective.ProdTeamTo = GetList(_userOverrides.ProdTeamTo, ProdTeamToKey);
            effective.ProdTeamCC = GetList(_userOverrides.ProdTeamCC, ProdTeamCCKey);

            // Debug emails
            effective.DebugTo = GetSingleString(_userOverrides.DebugTo, DebugToKey);
            effective.DebugCC1 = GetSingleString(_userOverrides.DebugCC1, DebugCC1Key);
            effective.DebugCC2 = GetSingleString(_userOverrides.DebugCC2, DebugCC2Key);

            Logger.LogDebug("GetCurrentEffectiveSettings completed.");
            return effective;
        }

        /// <summary>
        /// Gets the final "To" and "CC" email recipient lists for a specific report context.
        /// For automated reports, an <see cref="AutoReportDefinition"/> must be provided.
        /// </summary>
        /// <param name="reportTypeIndex">The index identifying the type of report (used for manual runs or as fallback).</param>
        /// <param name="isFemiOnlyChecked">True if "Send to Femi Only" is active (for manual non-daily/non-custom reports).</param>
        /// <param name="isDebugBuild">True if the application is in Debug build.</param>
        /// <param name="isAutoRunContext">True if the call is for an automated report.</param>
        /// <param name="definition">The <see cref="AutoReportDefinition"/> for the report if <paramref name="isAutoRunContext"/> is true. Otherwise, can be null.</param>
        /// <returns>A tuple containing lists of "To" and "CC" email addresses.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="definition"/> is null when <paramref name="isAutoRunContext"/> is true.</exception>
        public (List<string> To, List<string> Cc) GetRecipients(
            int reportTypeIndex,
            bool isFemiOnlyChecked,
            bool isDebugBuild,
            bool isAutoRunContext = false,
            AutoReportDefinition? definition = null) // Definition is now nullable, but required for auto-run
        {
            Logger.LogTrace($"EmailRecipientManager: GetRecipients. ReportTypeIndex: {reportTypeIndex}, FemiOnly: {isFemiOnlyChecked}, Debug: {isDebugBuild}, AutoRun: {isAutoRunContext}, DefName: {definition?.ReportName ?? "N/A"}");
            UserEmailSettings settings = GetCurrentEffectiveSettings();
            List<string> toAddresses = new List<string>();
            List<string> ccAddresses = new List<string>();

            if (isDebugBuild)
            {
                Logger.LogInfo("EmailRecipientManager: DEBUG Build. Using debug email recipients.");
                if (!string.IsNullOrWhiteSpace(settings.DebugTo)) toAddresses.Add(settings.DebugTo);
                if (!string.IsNullOrWhiteSpace(settings.DebugCC1)) ccAddresses.Add(settings.DebugCC1);
                if (!string.IsNullOrWhiteSpace(settings.DebugCC2)) ccAddresses.Add(settings.DebugCC2);
            }
            else // Release mode
            {
                if (isAutoRunContext)
                {
                    if (definition == null)
                    {
                        Logger.LogError("EmailRecipientManager: AutoReportDefinition is null in auto-run context. Cannot determine recipients.");
                        throw new ArgumentNullException(nameof(definition), "AutoReportDefinition cannot be null in auto-run context.");
                    }
                    if (string.IsNullOrWhiteSpace(definition.RecipientCategoryKey))
                    {
                        Logger.LogWarning($"EmailRecipientManager: Auto-run report '{definition.ReportName}' has no RecipientCategoryKey defined. Falling back to legacy ReportTypeIndex logic if applicable, or no recipients.");
                        // Fallback to old logic if category key is missing (optional, for smoother transition)
                        // For now, we'll assume if category key is missing, it's an error or means no specific list.
                        // If you want a fallback:
                        // switch (definition.ReportTypeIndex) { /* old auto-run switch cases */ }
                    }
                    else
                    {
                        Logger.LogInfo($"EmailRecipientManager: RELEASE Build & AutoRun Context. RecipientCategoryKey: '{definition.RecipientCategoryKey}' for report '{definition.ReportName}'.");
                        // Use a switch or if-else chain for known RecipientCategoryKey values
                        switch (definition.RecipientCategoryKey)
                        {
                            case "AutoRunDailyStandardRecipients":
                                toAddresses.AddRange(settings.AutoRunDailyStandardRecipientsTo ?? Enumerable.Empty<string>());
                                ccAddresses.AddRange(settings.AutoRunDailyStandardRecipientsCC ?? Enumerable.Empty<string>());
                                break;
                            case "AutoRunDaily5Day1kRecipients":
                                toAddresses.AddRange(settings.AutoRunDaily5Day1kRecipientsTo ?? Enumerable.Empty<string>());
                                ccAddresses.AddRange(settings.AutoRunDaily5Day1kRecipientsCC ?? Enumerable.Empty<string>());
                                break;
                            case "AutoRunWeeklyRecipients":
                                toAddresses.AddRange(settings.AutoRunWeeklyRecipientsTo ?? Enumerable.Empty<string>());
                                ccAddresses.AddRange(settings.AutoRunWeeklyRecipientsCC ?? Enumerable.Empty<string>());
                                break;
                            // Add cases for other RecipientCategoryKeys as defined in appsettings.json and UserEmailSettings.cs
                            // case "AutoRunMonthlyMarketingRecipients":
                            //     toAddresses.AddRange(settings.AutoRunMonthlyMarketingRecipientsTo ?? Enumerable.Empty<string>());
                            //     ccAddresses.AddRange(settings.AutoRunMonthlyMarketingRecipientsCC ?? Enumerable.Empty<string>());
                            //     break;
                            default:
                                Logger.LogWarning($"EmailRecipientManager: Unknown RecipientCategoryKey '{definition.RecipientCategoryKey}' for auto-run report '{definition.ReportName}'. No recipients will be added for this category.");
                                break;
                        }
                    }
                }
                else // Manual run context
                {
                    Logger.LogInfo($"EmailRecipientManager: RELEASE Build & Manual Run Context. ReportTypeIndex: {reportTypeIndex}, FemiOnly: {isFemiOnlyChecked}");
                    if (reportTypeIndex == DailyReportIndex)
                    {
                        toAddresses.AddRange(settings.ProdManualRunDailyTo ?? Enumerable.Empty<string>());
                        ccAddresses.AddRange(settings.ProdManualRunDailyCC ?? Enumerable.Empty<string>());
                        Logger.LogInfo("Using ProdManualRunDaily recipients for manual run of standard daily report.");
                    }
                    else if (reportTypeIndex == CustomReportIndex)
                    {
                        toAddresses.AddRange(settings.ProdManualCustomTo ?? Enumerable.Empty<string>());
                        ccAddresses.AddRange(settings.ProdManualCustomCC ?? Enumerable.Empty<string>());
                        Logger.LogInfo("Using ProdManualCustom recipients for manual run of Custom report.");
                    }
                    else // Other manual reports (Weekly, Monthly, Quarterly, Annual, Daily 5d>=1k)
                    {
                        if (isFemiOnlyChecked)
                        {
                            toAddresses.AddRange(settings.ProdFemiTo ?? Enumerable.Empty<string>());
                            ccAddresses.AddRange(settings.ProdFemiCC ?? Enumerable.Empty<string>());
                            Logger.LogInfo("Using ProdFemiTo/CC recipients for this manual report type (Femi Only checked).");
                        }
                        else
                        {
                            toAddresses.AddRange(settings.ProdTeamTo ?? Enumerable.Empty<string>());
                            ccAddresses.AddRange(settings.ProdTeamCC ?? Enumerable.Empty<string>());
                            Logger.LogInfo("Using ProdTeamTo/CC recipients for this manual report type (Team list).");
                        }
                    }
                }
            }

            // Clean up and de-duplicate recipient lists.
            toAddresses = toAddresses.Where(e => !string.IsNullOrWhiteSpace(e)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            ccAddresses = ccAddresses.Where(e => !string.IsNullOrWhiteSpace(e)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            ccAddresses = ccAddresses.Except(toAddresses, StringComparer.OrdinalIgnoreCase).ToList();

            Logger.LogDebug($"EmailRecipientManager: Final To Addresses: {string.Join("; ", toAddresses)}");
            Logger.LogDebug($"EmailRecipientManager: Final CC Addresses: {string.Join("; ", ccAddresses)}");
            Logger.LogTrace("EmailRecipientManager: Exiting GetRecipients.");
            return (toAddresses, ccAddresses);
        }

        /// <summary>
        /// Helper method to read a configuration value as a list of strings from `appsettings.json`.
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
        /// Validates a collection of email address strings.
        /// </summary>
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
        #endregion
    }
}
