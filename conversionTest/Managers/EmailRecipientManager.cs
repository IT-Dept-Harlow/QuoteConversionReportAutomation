// EmailRecipientManager.cs
// Manages email recipient lists for various report scenarios,
// merging application defaults (from the new appsettings.json structure)
// with user-specific overrides.
// Utilises C# 10+ features.

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
using QuoteConversionReportAutomation.Helpers; // For EmailUtility (specifically IsValidEmail).
using QuoteConversionReportAutomation.Models;   // For UserEmailSettings, AutoReportDefinition.
using QuoteConversionReportAutomation.Services.Logging; // For Logger.
#endregion

namespace QuoteConversionReportAutomation.Managers
{
    /// <summary>
    /// Manages the loading, saving, and provision of email recipient lists for the QCRA application.
    /// This class centralizes the logic for determining "To" and "CC" recipients for various report
    /// scenarios, including manual runs and automated reports. It prioritizes user-defined overrides
    /// (stored in `user_email_settings.json`) over application defaults (defined in `appsettings.json`).
    /// For automated reports, recipient lists are primarily determined by a `RecipientCategoryKey`
    /// specified in the <see cref="AutoReportDefinition"/>.
    /// The configuration keys used to fetch defaults now align with the revised `appsettings.json` structure.
    /// </summary>
    public class EmailRecipientManager
    {
        #region Fields and Constants

        /// <summary>
        /// Provides access to the application's main configuration settings (from `appsettings.json`).
        /// Used to retrieve default email recipient lists.
        /// </summary>
        private readonly IConfiguration _appConfiguration;

        /// <summary>
        /// In-memory cache of user-defined email recipient overrides, loaded from `user_email_settings.json`.
        /// This object is updated when user overrides are saved or cleared.
        /// </summary>
        private UserEmailSettings _userOverrides;

        /// <summary>
        /// The full file path to the user-specific JSON file where email recipient overrides are stored.
        /// Typically located in the user's AppData\Roaming directory.
        /// </summary>
        private readonly string _userSettingsFilePath;

        /// <summary>
        /// A static lock object to ensure thread-safe access when reading or writing the user settings file.
        /// </summary>
        private static readonly object s_fileLock = new object();

        // Report Type Indices (primarily for context in manual runs if specific logic is needed beyond category keys)
        private const int DailyReportIndex = 0;
        private const int CustomReportIndex = 6;

        // --- Configuration Keys for default recipient lists in the NEW appsettings.json structure ---
        // Paths are now relative to the root of appsettings.json.

        // Manual Run Defaults:
        private const string ProdManualRunDailyToKey = "EmailSettings:ProductionRecipients:ManualRunDailyTo";
        private const string ProdManualRunDailyCCKey = "EmailSettings:ProductionRecipients:ManualRunDailyCC";
        private const string ProdFemiToKey = "EmailSettings:ProductionRecipients:FemiTo";
        private const string ProdFemiCCKey = "EmailSettings:ProductionRecipients:FemiCC";
        private const string ProdTeamToKey = "EmailSettings:ProductionRecipients:TeamTo";
        private const string ProdTeamCCKey = "EmailSettings:ProductionRecipients:TeamCC";
        private const string ProdManualCustomToKey = "EmailSettings:ProductionRecipients:ManualCustomTo";
        private const string ProdManualCustomCCKey = "EmailSettings:ProductionRecipients:ManualCustomCC";

        // Category-Based Automated Report Defaults:
        private const string AutoRunDailyStandardRecipientsToKey = "EmailSettings:ProductionRecipients:AutoRunDailyStandardRecipientsTo";
        private const string AutoRunDailyStandardRecipientsCCKey = "EmailSettings:ProductionRecipients:AutoRunDailyStandardRecipientsCC";
        private const string AutoRunDaily5Day1kRecipientsToKey = "EmailSettings:ProductionRecipients:AutoRunDaily5Day1kRecipientsTo";
        private const string AutoRunDaily5Day1kRecipientsCCKey = "EmailSettings:ProductionRecipients:AutoRunDaily5Day1kRecipientsCC";
        private const string AutoRunWeeklyRecipientsToKey = "EmailSettings:ProductionRecipients:AutoRunWeeklyRecipientsTo";
        private const string AutoRunWeeklyRecipientsCCKey = "EmailSettings:ProductionRecipients:AutoRunWeeklyRecipientsCC";
        // Add more constants here for other RecipientCategoryKeys if introduced, following the pattern:
        // e.g., "EmailSettings:ProductionRecipients:YourNewCategoryRecipientsTo"

        // Debug Email Defaults:
        private const string DebugToKey = "EmailSettings:DebugRecipients:To";
        private const string DebugCC1Key = "EmailSettings:DebugRecipients:CC1";
        private const string DebugCC2Key = "EmailSettings:DebugRecipients:CC2";

        #endregion

        #region Constructor
        /// <summary>
        /// Initialises a new instance of the <see cref="EmailRecipientManager"/> class.
        /// It loads user-defined email recipient overrides from a local JSON file, which take
        /// precedence over application defaults specified in `appsettings.json`.
        /// </summary>
        /// <param name="appConfiguration">The application's main configuration (typically `IConfiguration` instance).</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="appConfiguration"/> is null.</exception>
        public EmailRecipientManager(IConfiguration appConfiguration)
        {
            _appConfiguration = appConfiguration ?? throw new ArgumentNullException(nameof(appConfiguration));

            // Construct the path to the user-specific settings file.
            // Example: C:\Users\<User>\AppData\Roaming\HarlowSolutions\QuoteConversionReportAutomation\user_email_settings.json
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string companyFolder = "HarlowSolutions"; // Standardised company folder.
            string appFolder = "QuoteConversionReportAutomation"; // Standardised application folder.
            _userSettingsFilePath = Path.Combine(appDataPath, companyFolder, appFolder, "user_email_settings.json");

            // Load user overrides from the file.
            _userOverrides = LoadUserOverrides();
            Logger.LogInfo($"EmailRecipientManager initialised. User overrides loaded from: '{_userSettingsFilePath}'");
        }
        #endregion

        #region User Overrides Management
        /// <summary>
        /// Loads user-defined email recipient overrides from their local JSON settings file (`user_email_settings.json`).
        /// This method is called during the initialization of the <see cref="EmailRecipientManager"/>.
        /// </summary>
        /// <returns>A <see cref="UserEmailSettings"/> object containing the loaded overrides. If the file does not exist,
        /// is empty, or contains invalid JSON, a new <see cref="UserEmailSettings"/> instance with default (empty)
        /// lists and strings is returned.</returns>
        private UserEmailSettings LoadUserOverrides()
        {
            try
            {
                if (File.Exists(_userSettingsFilePath))
                {
                    string json;
                    lock (s_fileLock) // Ensure thread-safe read.
                    {
                        json = File.ReadAllText(_userSettingsFilePath);
                    }

                    if (!string.IsNullOrWhiteSpace(json))
                    {
                        // Deserialize using System.Text.Json
                        var settings = JsonSerializer.Deserialize<UserEmailSettings>(json);
                        if (settings != null)
                        {
                            Logger.LogInfo($"Successfully loaded user email overrides from '{_userSettingsFilePath}'.");
                            return settings;
                        }
                        else
                        {
                            Logger.LogWarning($"Deserialization of '{_userSettingsFilePath}' resulted in a null UserEmailSettings object. Using defaults.");
                        }
                    }
                    else
                    {
                        Logger.LogInfo($"User email overrides file '{_userSettingsFilePath}' is empty. Using defaults.");
                    }
                }
                else
                {
                    Logger.LogInfo($"User email overrides file not found at '{_userSettingsFilePath}'. Using defaults. File will be created if overrides are saved.");
                }
            }
            catch (JsonException jsonEx)
            {
                Logger.LogError($"Error deserializing user email overrides from '{_userSettingsFilePath}': {jsonEx.Message}. File might be corrupt. Ensure it's valid JSON.", jsonEx);
            }
            catch (IOException ioEx)
            {
                Logger.LogError($"IO Error loading user email overrides from '{_userSettingsFilePath}': {ioEx.Message}", ioEx);
            }
            catch (Exception ex)
            {
                Logger.LogError($"Unexpected error loading user email overrides from '{_userSettingsFilePath}': {ex.Message}", ex);
            }
            Logger.LogDebug("Returning new UserEmailSettings instance (no overrides loaded or error occurred).");
            return new UserEmailSettings(); // Constructor initializes lists.
        }

        /// <summary>
        /// Saves the provided <see cref="UserEmailSettings"/> object to the user's local JSON settings file,
        /// overwriting any existing content. Also updates the in-memory cache of user overrides.
        /// </summary>
        /// <param name="settingsToSave">The <see cref="UserEmailSettings"/> object containing the recipient lists to save.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="settingsToSave"/> is null.</exception>
        /// <exception cref="IOException">Thrown if an I/O error occurs during directory creation or file writing.</exception>
        /// <exception cref="JsonException">Thrown if an error occurs during JSON serialization.</exception>
        /// <exception cref="Exception">Can throw other exceptions if saving fails (e.g., permission issues, disk full).</exception>
        public void SaveUserOverrides(UserEmailSettings settingsToSave)
        {
            ArgumentNullException.ThrowIfNull(settingsToSave, nameof(settingsToSave));
            try
            {
                string? directoryPath = Path.GetDirectoryName(_userSettingsFilePath);
                if (string.IsNullOrEmpty(directoryPath))
                {
                    throw new InvalidOperationException($"Could not determine directory path from '{_userSettingsFilePath}'.");
                }
                if (!Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                    Logger.LogInfo($"Created directory for user email settings: {directoryPath}");
                }

                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(settingsToSave, options);

                lock (s_fileLock) // Ensure thread-safe write.
                {
                    File.WriteAllText(_userSettingsFilePath, json);
                }
                _userOverrides = settingsToSave; // Update in-memory cache.
                Logger.LogInfo($"User email overrides saved to '{_userSettingsFilePath}'.");
            }
            catch (JsonException jsonEx)
            {
                Logger.LogError($"Error serializing user email overrides for '{_userSettingsFilePath}': {jsonEx.Message}", jsonEx);
                throw;
            }
            catch (IOException ioEx)
            {
                Logger.LogError($"IO Error saving user email overrides to '{_userSettingsFilePath}': {ioEx.Message}", ioEx);
                throw;
            }
            catch (Exception ex)
            {
                Logger.LogError($"Unexpected error saving user email overrides to '{_userSettingsFilePath}': {ex.Message}", ex);
                throw;
            }
        }

        /// <summary>
        /// Clears all user-defined email recipient overrides by deleting the local settings file
        /// and resetting the in-memory cache to a new, default <see cref="UserEmailSettings"/> instance.
        /// </summary>
        /// <exception cref="IOException">Can throw file I/O exceptions if deletion of the settings file fails.</exception>
        /// <exception cref="Exception">Can throw other exceptions if deletion fails.</exception>
        public void ClearUserOverrides()
        {
            try
            {
                lock (s_fileLock) // Ensure thread-safe file operation.
                {
                    if (File.Exists(_userSettingsFilePath))
                    {
                        File.Delete(_userSettingsFilePath);
                        Logger.LogInfo($"User email overrides file '{_userSettingsFilePath}' deleted.");
                    }
                }
                _userOverrides = new UserEmailSettings(); // Reset in-memory cache.
                Logger.LogInfo("In-memory user email overrides reset to defaults.");
            }
            catch (IOException ioEx)
            {
                Logger.LogError($"IO Error clearing user email overrides file '{_userSettingsFilePath}': {ioEx.Message}", ioEx);
                throw;
            }
            catch (Exception ex)
            {
                Logger.LogError($"Unexpected error clearing user email overrides file '{_userSettingsFilePath}': {ex.Message}", ex);
                throw;
            }
        }
        #endregion

        #region Recipient Retrieval Logic
        /// <summary>
        /// Gets the current effective email recipient settings by merging user overrides
        /// (from `_userOverrides` cache) with application defaults (from `appsettings.json`).
        /// User overrides take precedence for each specific list.
        /// </summary>
        /// <returns>A new <see cref="UserEmailSettings"/> object representing the combined effective settings.
        /// All list properties in the returned object are guaranteed to be non-null (empty if no recipients).</returns>
        public UserEmailSettings GetCurrentEffectiveSettings()
        {
            var effective = new UserEmailSettings(); // Constructor initializes all lists to empty.

            List<string> GetList(List<string>? userOverrideList, string appConfigKey)
            {
                if (userOverrideList != null && userOverrideList.Any(e => !string.IsNullOrWhiteSpace(e)))
                {
                    return new List<string>(userOverrideList.Where(e => !string.IsNullOrWhiteSpace(e)));
                }
                var appConfigValues = GetStringListFromAppConfig(appConfigKey);
                return appConfigValues?.Any() == true ? appConfigValues : new List<string>();
            }

            string GetSingleString(string? userOverride, string appConfigKey)
            {
                if (!string.IsNullOrWhiteSpace(userOverride)) return userOverride;
                var listValue = GetStringListFromAppConfig(appConfigKey);
                return listValue?.FirstOrDefault(e => !string.IsNullOrWhiteSpace(e)) ?? string.Empty;
            }

            // Populate category-based automated report recipient lists.
            effective.AutoRunDailyStandardRecipientsTo = GetList(_userOverrides.AutoRunDailyStandardRecipientsTo, AutoRunDailyStandardRecipientsToKey);
            effective.AutoRunDailyStandardRecipientsCC = GetList(_userOverrides.AutoRunDailyStandardRecipientsCC, AutoRunDailyStandardRecipientsCCKey);
            effective.AutoRunDaily5Day1kRecipientsTo = GetList(_userOverrides.AutoRunDaily5Day1kRecipientsTo, AutoRunDaily5Day1kRecipientsToKey);
            effective.AutoRunDaily5Day1kRecipientsCC = GetList(_userOverrides.AutoRunDaily5Day1kRecipientsCC, AutoRunDaily5Day1kRecipientsCCKey);
            effective.AutoRunWeeklyRecipientsTo = GetList(_userOverrides.AutoRunWeeklyRecipientsTo, AutoRunWeeklyRecipientsToKey);
            effective.AutoRunWeeklyRecipientsCC = GetList(_userOverrides.AutoRunWeeklyRecipientsCC, AutoRunWeeklyRecipientsCCKey);
            // Add more categories here following the pattern.

            // Populate manual report recipient lists.
            effective.ProdManualRunDailyTo = GetList(_userOverrides.ProdManualRunDailyTo, ProdManualRunDailyToKey);
            effective.ProdManualRunDailyCC = GetList(_userOverrides.ProdManualRunDailyCC, ProdManualRunDailyCCKey);
            effective.ProdManualCustomTo = GetList(_userOverrides.ProdManualCustomTo, ProdManualCustomToKey);
            effective.ProdManualCustomCC = GetList(_userOverrides.ProdManualCustomCC, ProdManualCustomCCKey);
            effective.ProdFemiTo = GetList(_userOverrides.ProdFemiTo, ProdFemiToKey);
            effective.ProdFemiCC = GetList(_userOverrides.ProdFemiCC, ProdFemiCCKey);
            effective.ProdTeamTo = GetList(_userOverrides.ProdTeamTo, ProdTeamToKey);
            effective.ProdTeamCC = GetList(_userOverrides.ProdTeamCC, ProdTeamCCKey);

            // Populate Debug email recipients.
            effective.DebugTo = GetSingleString(_userOverrides.DebugTo, DebugToKey);
            effective.DebugCC1 = GetSingleString(_userOverrides.DebugCC1, DebugCC1Key);
            effective.DebugCC2 = GetSingleString(_userOverrides.DebugCC2, DebugCC2Key);

            Logger.LogDebug("GetCurrentEffectiveSettings completed.");
            return effective;
        }

        /// <summary>
        /// Gets the final "To" and "CC" email recipient lists for a specific report context.
        /// This is the primary method used by other parts of the application to determine who should receive an email.
        /// </summary>
        /// <param name="reportTypeIndex">The index identifying the type of report. Primarily used for manual runs or as a fallback if category-based determination fails for automated runs.</param>
        /// <param name="isFemiOnlyChecked">True if the "Send to Femi Only" option is active (relevant for certain manual non-daily/non-custom reports).</param>
        /// <param name="isDebugBuild">True if the application is compiled in Debug mode. If true, debug recipients are used exclusively.</param>
        /// <param name="isAutoRunContext">True if this call is for an automated report run. This influences which recipient lists are consulted.</param>
        /// <param name="definition">The <see cref="AutoReportDefinition"/> for the report if <paramref name="isAutoRunContext"/> is true.
        /// This definition contains the `RecipientCategoryKey` used to look up specific automated report recipients. Can be null for manual runs.</param>
        /// <returns>A tuple containing two <see cref="List{T}"/> of strings: the first for "To" recipients and the second for "CC" recipients.
        /// Lists are cleaned of duplicates (case-insensitive) and whitespace, and an address will not appear in both "To" and "CC".</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="definition"/> is null when <paramref name="isAutoRunContext"/> is true and `RecipientCategoryKey` is needed.</exception>
        public (List<string> To, List<string> Cc) GetRecipients(
            int reportTypeIndex,
            bool isFemiOnlyChecked,
            bool isDebugBuild,
            bool isAutoRunContext = false,
            AutoReportDefinition? definition = null)
        {
            Logger.LogTrace($"GetRecipients called. ReportTypeIndex: {reportTypeIndex}, FemiOnly: {isFemiOnlyChecked}, DebugBuild: {isDebugBuild}, IsAutoRun: {isAutoRunContext}, DefName: {definition?.ReportName ?? "N/A"}");
            UserEmailSettings settings = GetCurrentEffectiveSettings(); // Gets merged settings.
            List<string> toAddresses = new List<string>();
            List<string> ccAddresses = new List<string>();

            if (isDebugBuild)
            {
                Logger.LogInfo("DEBUG Build: Using debug email recipients.");
                if (!string.IsNullOrWhiteSpace(settings.DebugTo)) toAddresses.Add(settings.DebugTo);
                if (!string.IsNullOrWhiteSpace(settings.DebugCC1)) ccAddresses.Add(settings.DebugCC1);
                if (!string.IsNullOrWhiteSpace(settings.DebugCC2)) ccAddresses.Add(settings.DebugCC2);
            }
            else // Release mode logic.
            {
                if (isAutoRunContext)
                {
                    if (definition == null)
                    {
                        Logger.LogError("AutoReportDefinition is null in auto-run context. Cannot determine recipients.");
                        throw new ArgumentNullException(nameof(definition), "AutoReportDefinition cannot be null for recipient determination in auto-run context.");
                    }
                    if (string.IsNullOrWhiteSpace(definition.RecipientCategoryKey))
                    {
                        Logger.LogWarning($"Automated report '{definition.ReportName}' (ID: {definition.ReportId}) has no RecipientCategoryKey. No category-based recipients will be added.");
                    }
                    else
                    {
                        Logger.LogInfo($"RELEASE Build & AutoRun Context. Using RecipientCategoryKey: '{definition.RecipientCategoryKey}' for report '{definition.ReportName}'.");
                        // Logic to map RecipientCategoryKey to UserEmailSettings properties.
                        // This assumes UserEmailSettings has properties named like the keys (e.g., AutoRunDailyStandardRecipientsTo).
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
                            // Add more cases here for other RecipientCategoryKey values as needed.
                            default:
                                Logger.LogWarning($"Unknown RecipientCategoryKey '{definition.RecipientCategoryKey}' for auto-run report '{definition.ReportName}'. No category-specific recipients added.");
                                break;
                        }
                    }
                }
                else // Manual run context.
                {
                    Logger.LogInfo($"RELEASE Build & Manual Run Context. ReportTypeIndex: {reportTypeIndex}, FemiOnlyChecked: {isFemiOnlyChecked}");
                    if (reportTypeIndex == DailyReportIndex)
                    {
                        toAddresses.AddRange(settings.ProdManualRunDailyTo ?? Enumerable.Empty<string>());
                        ccAddresses.AddRange(settings.ProdManualRunDailyCC ?? Enumerable.Empty<string>());
                        Logger.LogDebug("Using ProdManualRunDaily recipients for manual standard daily report.");
                    }
                    else if (reportTypeIndex == CustomReportIndex)
                    {
                        toAddresses.AddRange(settings.ProdManualCustomTo ?? Enumerable.Empty<string>());
                        ccAddresses.AddRange(settings.ProdManualCustomCC ?? Enumerable.Empty<string>());
                        Logger.LogDebug("Using ProdManualCustom recipients for manual custom report.");
                    }
                    else // Other manual reports (Weekly, Monthly, etc., including Daily 5d>=1k)
                    {
                        if (isFemiOnlyChecked)
                        {
                            toAddresses.AddRange(settings.ProdFemiTo ?? Enumerable.Empty<string>());
                            ccAddresses.AddRange(settings.ProdFemiCC ?? Enumerable.Empty<string>());
                            Logger.LogDebug("Using ProdFemiTo/CC for manual non-daily/non-custom (Femi Only checked).");
                        }
                        else
                        {
                            toAddresses.AddRange(settings.ProdTeamTo ?? Enumerable.Empty<string>());
                            ccAddresses.AddRange(settings.ProdTeamCC ?? Enumerable.Empty<string>());
                            Logger.LogDebug("Using ProdTeamTo/CC for manual non-daily/non-custom (Team list).");
                        }
                    }
                }
            }

            // Clean up recipient lists: remove duplicates, ensure no overlap between To and CC.
            var finalTo = toAddresses.Where(e => !string.IsNullOrWhiteSpace(e)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            var finalCc = ccAddresses.Where(e => !string.IsNullOrWhiteSpace(e))
                                     .Distinct(StringComparer.OrdinalIgnoreCase)
                                     .Except(finalTo, StringComparer.OrdinalIgnoreCase) // Remove any CC if already in To.
                                     .ToList();

            Logger.LogDebug($"Final 'To' Addresses: {string.Join("; ", finalTo)}");
            Logger.LogDebug($"Final 'CC' Addresses: {string.Join("; ", finalCc)}");
            Logger.LogTrace("Exiting GetRecipients.");
            return (finalTo, finalCc);
        }

        /// <summary>
        /// Helper method to read a configuration value as a list of strings from `appsettings.json`.
        /// Handles cases where the value is a single string or a JSON array.
        /// </summary>
        /// <param name="key">The configuration key (e.g., "EmailSettings:ProductionRecipients:ManualRunDailyTo").</param>
        /// <returns>A list of trimmed, non-empty email addresses, or null if key not found or parsing fails.</returns>
        private List<string>? GetStringListFromAppConfig(string key)
        {
            try
            {
                var section = _appConfiguration.GetSection(key);
                if (!section.Exists())
                {
                    Logger.LogDebug($"Configuration key '{key}' not found in appsettings.");
                    return null;
                }

                // Try to bind as List<string> first (for JSON arrays).
                var list = section.Get<List<string>>();
                if (list != null && list.Any(s => !string.IsNullOrWhiteSpace(s)))
                {
                    return list.Select(e => e?.Trim())
                               .Where(e => !string.IsNullOrWhiteSpace(e))
                               .Select(e => e!) // Non-null assertion.
                               .ToList();
                }

                // If not a list, try as a single string (and split if it contains separators).
                string? singleValue = section.Get<string>();
                if (!string.IsNullOrWhiteSpace(singleValue))
                {
                    return singleValue.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                                      .Select(e => e.Trim())
                                      .Where(e => !string.IsNullOrWhiteSpace(e))
                                      .ToList();
                }

                Logger.LogDebug($"Configuration key '{key}' exists but is empty or not a string/array.");
                return null;
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"Could not parse appsetting key '{key}' as a list/string. Error: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Validates a collection of email address strings. Each string in the collection
        /// can itself be a comma or semicolon-separated list of emails.
        /// </summary>
        /// <param name="emails">An enumerable collection of strings, where each string might contain multiple email addresses.</param>
        /// <param name="invalidEmails">An output list that will be populated with any email addresses found to be invalid.</param>
        /// <returns>True if all parsed email addresses are valid; otherwise, false.</returns>
        public static bool ValidateEmailAddresses(IEnumerable<string>? emails, out List<string> invalidEmails)
        {
            invalidEmails = new List<string>();
            if (emails == null) return true; // Consider null input as valid (no emails to check).

            bool allValid = true;
            foreach (var emailStr in emails.Where(e => !string.IsNullOrWhiteSpace(e))) // Process only non-empty strings.
            {
                var individualEmails = emailStr.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var singleEmail in individualEmails)
                {
                    string trimmedEmail = singleEmail.Trim();
                    if (!string.IsNullOrWhiteSpace(trimmedEmail)) // Validate after trimming.
                    {
                        if (!EmailUtility.IsValidEmail(trimmedEmail)) // Assumes EmailUtility.IsValidEmail is available.
                        {
                            allValid = false;
                            if (!invalidEmails.Contains(trimmedEmail, StringComparer.OrdinalIgnoreCase))
                            {
                                invalidEmails.Add(trimmedEmail); // Add invalid email to the output list.
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