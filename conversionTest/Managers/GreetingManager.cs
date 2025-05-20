// GreetingManager.cs
namespace QuoteConversionReportAutomation.Managers
{
    using Microsoft.Extensions.Configuration;
    using QuoteConversionReportAutomation.Models;
    using QuoteConversionReportAutomation.Services.Logging;
    using System;
    using System.IO;
    using System.Text.Json;

    public class GreetingManager
    {
        private readonly IConfiguration _appConfiguration;
        private UserGreetingSettings _userGreetingOverrides;
        private readonly string _userGreetingsSettingsFilePath;
        private static readonly object _fileLock = new object();

        private const string DefaultGreetingFallbackText = "Hi Team,";

        // Configuration key base paths
        private const string ProdEmailGreetingsSectionKey = "settings:ProductionEmails:EmailGreetings";
        private const string DebugEmailGreetingsSectionKey = "settings:DebugEmails:EmailGreetings";

        public GreetingManager(IConfiguration appConfiguration)
        {
            _appConfiguration = appConfiguration ?? throw new ArgumentNullException(nameof(appConfiguration));

            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string companyFolder = "HarlowSolutions";
            string appFolder = "QuoteConversionReportAutomation";
            _userGreetingsSettingsFilePath = Path.Combine(appDataPath, companyFolder, appFolder, "user_greeting_settings.json");

            _userGreetingOverrides = LoadUserGreetingOverrides();
            Logger.LogInfo($"GreetingManager initialized. User greeting overrides loaded from: {_userGreetingsSettingsFilePath}");
        }

        private UserGreetingSettings LoadUserGreetingOverrides()
        {
            try
            {
                if (File.Exists(_userGreetingsSettingsFilePath))
                {
                    string json;
                    lock (_fileLock)
                    {
                        json = File.ReadAllText(_userGreetingsSettingsFilePath);
                    }
                    if (!string.IsNullOrWhiteSpace(json))
                    {
                        var settings = JsonSerializer.Deserialize<UserGreetingSettings>(json);
                        if (settings != null)
                        {
                            Logger.LogInfo("Successfully loaded user greeting overrides.");
                            return settings;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error loading user greeting overrides from '{_userGreetingsSettingsFilePath}': {ex.Message}", ex);
            }
            Logger.LogInfo("No user greeting overrides found or file was empty/invalid. Using new UserGreetingSettings instance.");
            return new UserGreetingSettings();
        }

        public void SaveUserGreetingOverrides(UserGreetingSettings settingsToSave)
        {
            if (settingsToSave == null) throw new ArgumentNullException(nameof(settingsToSave));
            try
            {
                string directoryPath = Path.GetDirectoryName(_userGreetingsSettingsFilePath)!;
                if (!Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                    Logger.LogInfo($"Created directory for user greeting settings: {directoryPath}");
                }

                var options = new JsonSerializerOptions { WriteIndented = true, DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull };
                string json = JsonSerializer.Serialize(settingsToSave, options);
                lock (_fileLock)
                {
                    File.WriteAllText(_userGreetingsSettingsFilePath, json);
                }
                _userGreetingOverrides = settingsToSave; // Update in-memory cache
                Logger.LogInfo($"User greeting overrides saved to '{_userGreetingsSettingsFilePath}'.");
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error saving user greeting overrides to '{_userGreetingsSettingsFilePath}': {ex.Message}", ex);
                throw;
            }
        }

        public void ClearUserGreetingOverrides()
        {
            try
            {
                lock (_fileLock)
                {
                    if (File.Exists(_userGreetingsSettingsFilePath))
                    {
                        File.Delete(_userGreetingsSettingsFilePath);
                        Logger.LogInfo($"User greeting overrides file '{_userGreetingsSettingsFilePath}' deleted.");
                    }
                }
                _userGreetingOverrides = new UserGreetingSettings(); // Reset in-memory cache
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error clearing user greeting overrides file '{_userGreetingsSettingsFilePath}': {ex.Message}", ex);
                throw;
            }
        }

        /// <summary>
        /// Gets the effective greeting string for a given key, prioritizing user overrides, then appsettings, then a hardcoded fallback.
        /// </summary>
        /// <param name="greetingKeyName">The specific name of the greeting (e.g., "AutoRunDaily", "ManualStdDaily", "DebugDefault").</param>
        /// <param name="isForDebugSection">True if the greetingKeyName refers to a key within the DebugEmails:EmailGreetings section.</param>
        /// <returns>The effective greeting string.</returns>
        public string GetGreeting(string greetingKeyName, bool isForDebugSection = false)
        {
            string? userOverride = null;
            string? appSettingDefault = null;
            string configSectionPath = isForDebugSection ? DebugEmailGreetingsSectionKey : ProdEmailGreetingsSectionKey;

            // Try to get user override
            if (_userGreetingOverrides != null)
            {
                try
                {
                    var propInfo = typeof(UserGreetingSettings).GetProperty(greetingKeyName);
                    if (propInfo != null)
                    {
                        userOverride = propInfo.GetValue(_userGreetingOverrides) as string;
                    }
                }
                catch (Exception ex) { Logger.LogWarning($"Error accessing user override for greeting '{greetingKeyName}': {ex.Message}"); }
            }

            if (!string.IsNullOrWhiteSpace(userOverride))
            {
                Logger.LogDebug($"Using user override for greeting '{greetingKeyName}': '{userOverride}'");
                return userOverride;
            }

            // Try to get appsettings default
            appSettingDefault = _appConfiguration[$"{configSectionPath}:{greetingKeyName}"];
            if (!string.IsNullOrWhiteSpace(appSettingDefault))
            {
                Logger.LogDebug($"Using appsettings default for greeting '{greetingKeyName}': '{appSettingDefault}'");
                return appSettingDefault;
            }

            Logger.LogWarning($"Greeting key '{greetingKeyName}' not found in user overrides or appsettings (section: '{configSectionPath}'). Using hardcoded fallback.");
            return DefaultGreetingFallbackText;
        }

        /// <summary>
        /// Retrieves all current effective greetings, merging user overrides with app defaults.
        /// Primarily for populating the ManageGreetingsForm.
        /// </summary>
        public UserGreetingSettings GetCurrentEffectiveGreetings()
        {
            var effective = new UserGreetingSettings
            {
                AutoRunDaily = GetGreeting("AutoRunDaily"),
                ManualStdDaily = GetGreeting("ManualStdDaily"),
                AutoRunDaily5Day1k = GetGreeting("AutoRunDaily5Day1k"),
                ManualFemi = GetGreeting("ManualFemi"),
                ManualTeam = GetGreeting("ManualTeam"),
                DebugDefault = GetGreeting("DebugDefault", isForDebugSection: true)
            };
            return effective;
        }
    }
}
