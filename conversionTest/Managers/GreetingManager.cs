#region Using Directives
// System related namespaces
using System;
using System.IO;
using System.Text.Json; // For System.Text.Json serialisation/deserialisation.

// Third-party namespaces
using Microsoft.Extensions.Configuration; // For IConfiguration.

// Project specific namespaces
using QuoteConversionReportAutomation.Models;   // For UserGreetingSettings model.
using QuoteConversionReportAutomation.Services.Logging; // For Logger.
#endregion

namespace QuoteConversionReportAutomation.Managers
{
    /// <summary>
    /// Manages loading, saving, and providing email greeting messages.
    /// It prioritises user-defined overrides over application defaults specified in configuration.
    /// </summary>
    public class GreetingManager
    {
        #region Fields and Constants

        private readonly IConfiguration _appConfiguration;      // Application configuration instance.
        private UserGreetingSettings _userGreetingOverrides; // In-memory cache of user-defined greeting settings.
        private readonly string _userGreetingsSettingsFilePath; // Full path to the user's greeting settings JSON file.
        private static readonly object _fileLock = new object(); // Lock object for thread-safe file access.

        // Default fallback text if no greeting is found in overrides or configuration.
        private const string DefaultGreetingFallbackText = "Hi Team,";

        // Configuration key base paths for accessing greeting settings in appsettings.json.
        private const string ProdEmailGreetingsSectionKey = "settings:ProductionEmails:EmailGreetings";
        private const string DebugEmailGreetingsSectionKey = "settings:DebugEmails:EmailGreetings";

        #endregion

        #region Constructor
        /// <summary>
        /// Initialises a new instance of the <see cref="GreetingManager"/> class.
        /// Loads user greeting overrides from their specific settings file.
        /// </summary>
        /// <param name="appConfiguration">The application's configuration interface.</param>
        public GreetingManager(IConfiguration appConfiguration)
        {
            _appConfiguration = appConfiguration ?? throw new ArgumentNullException(nameof(appConfiguration));

            // Construct the path to the user-specific greeting settings file within AppData.
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string companyFolder = "HarlowSolutions"; // Company name for folder structure.
            string appFolder = "QuoteConversionReportAutomation"; // Application name for folder structure.
            _userGreetingsSettingsFilePath = Path.Combine(appDataPath, companyFolder, appFolder, "user_greeting_settings.json");

            _userGreetingOverrides = LoadUserGreetingOverrides(); // Load any existing user overrides.
            Logger.LogInfo($"GreetingManager initialised. User greeting overrides loaded from: {_userGreetingsSettingsFilePath}");
        }
        #endregion

        #region User Overrides Management
        /// <summary>
        /// Loads user-defined email greeting overrides from their local JSON settings file.
        /// If the file doesn't exist or is invalid, returns a new <see cref="UserGreetingSettings"/> instance.
        /// </summary>
        /// <returns>A <see cref="UserGreetingSettings"/> object containing the user's overrides or default (null) settings.</returns>
        private UserGreetingSettings LoadUserGreetingOverrides()
        {
            try
            {
                if (File.Exists(_userGreetingsSettingsFilePath))
                {
                    string json;
                    // Lock file access for thread safety during read.
                    lock (_fileLock)
                    {
                        json = File.ReadAllText(_userGreetingsSettingsFilePath);
                    }
                    if (!string.IsNullOrWhiteSpace(json))
                    {
                        // Deserialise the JSON content into a UserGreetingSettings object.
                        var settings = JsonSerializer.Deserialize<UserGreetingSettings>(json);
                        if (settings != null)
                        {
                            Logger.LogInfo("Successfully loaded user greeting overrides.");
                            return settings; // Return loaded settings.
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error loading user greeting overrides from '{_userGreetingsSettingsFilePath}': {ex.Message}", ex);
            }
            Logger.LogInfo("No user greeting overrides found or file was empty/invalid. Using new UserGreetingSettings instance.");
            return new UserGreetingSettings(); // Return a new instance if loading fails or file doesn't exist.
        }

        /// <summary>
        /// Saves the provided <see cref="UserGreetingSettings"/> to the user's local JSON settings file.
        /// Overwrites any existing file.
        /// </summary>
        /// <param name="settingsToSave">The <see cref="UserGreetingSettings"/> object to save. Null properties will be serialised as null.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="settingsToSave"/> is null.</exception>
        /// <exception cref="Exception">Can throw various file I/O or serialisation exceptions.</exception>
        public void SaveUserGreetingOverrides(UserGreetingSettings settingsToSave)
        {
            if (settingsToSave == null) throw new ArgumentNullException(nameof(settingsToSave));
            try
            {
                // Ensure the directory for the settings file exists.
                string directoryPath = Path.GetDirectoryName(_userGreetingsSettingsFilePath)!; // '!' assumes path will be valid.
                if (!Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                    Logger.LogInfo($"Created directory for user greeting settings: {directoryPath}");
                }

                // Configure JsonSerializer options: write indented JSON and ignore null values when writing (though UserGreetingSettings properties are nullable strings).
                var options = new JsonSerializerOptions { WriteIndented = true, DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull };
                string json = JsonSerializer.Serialize(settingsToSave, options);

                // Lock file access for thread safety during write.
                lock (_fileLock)
                {
                    File.WriteAllText(_userGreetingsSettingsFilePath, json);
                }
                _userGreetingOverrides = settingsToSave; // Update the in-memory cache with the saved settings.
                Logger.LogInfo($"User greeting overrides saved to '{_userGreetingsSettingsFilePath}'.");
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error saving user greeting overrides to '{_userGreetingsSettingsFilePath}': {ex.Message}", ex);
                throw; // Re-throw the exception to be handled by the caller.
            }
        }

        /// <summary>
        /// Clears all user-defined email greeting overrides by deleting the local settings file
        /// and resetting the in-memory cache to a new <see cref="UserGreetingSettings"/> instance.
        /// </summary>
        /// <exception cref="Exception">Can throw file I/O exceptions if deletion fails.</exception>
        public void ClearUserGreetingOverrides()
        {
            try
            {
                // Lock file access for thread safety.
                lock (_fileLock)
                {
                    if (File.Exists(_userGreetingsSettingsFilePath))
                    {
                        File.Delete(_userGreetingsSettingsFilePath); // Delete the user settings file.
                        Logger.LogInfo($"User greeting overrides file '{_userGreetingsSettingsFilePath}' deleted.");
                    }
                }
                _userGreetingOverrides = new UserGreetingSettings(); // Reset the in-memory cache.
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error clearing user greeting overrides file '{_userGreetingsSettingsFilePath}': {ex.Message}", ex);
                throw; // Re-throw to be handled by the caller.
            }
        }
        #endregion

        #region Greeting Retrieval Logic
        /// <summary>
        /// Gets the effective greeting string for a given key name.
        /// It prioritises user overrides, then checks `appsettings.json` for a default,
        /// and finally falls back to a hardcoded default greeting if none are found.
        /// </summary>
        /// <param name="greetingKeyName">The specific name of the greeting key (e.g., "AutoRunDaily", "ManualCustom").
        /// This should match a property name in <see cref="UserGreetingSettings"/> and a key in the `EmailGreetings` section of `appsettings.json`.</param>
        /// <param name="isForDebugSection">True if the <paramref name="greetingKeyName"/> refers to a key within the "DebugEmails:EmailGreetings"
        /// section of `appsettings.json`; false if it refers to "ProductionEmails:EmailGreetings".</param>
        /// <returns>The effective greeting string. Returns <see cref="DefaultGreetingFallbackText"/> if no specific greeting is found.</returns>
        public string GetGreeting(string greetingKeyName, bool isForDebugSection = false)
        {
            string? userOverride = null;
            string? appSettingDefault = null;
            // Determine the base path in appsettings.json based on whether it's for debug or production.
            string configSectionPath = isForDebugSection ? DebugEmailGreetingsSectionKey : ProdEmailGreetingsSectionKey;

            // 1. Attempt to retrieve the greeting from user overrides.
            if (_userGreetingOverrides != null)
            {
                try
                {
                    // Use reflection to get the property value from _userGreetingOverrides based on greetingKeyName.
                    var propInfo = typeof(UserGreetingSettings).GetProperty(greetingKeyName);
                    if (propInfo != null)
                    {
                        userOverride = propInfo.GetValue(_userGreetingOverrides) as string;
                    }
                }
                catch (Exception ex)
                {
                    // Log if reflection fails, but don't let it crash the greeting retrieval.
                    Logger.LogWarning($"Error accessing user override for greeting '{greetingKeyName}' via reflection: {ex.Message}");
                }
            }

            // If a non-empty user override is found, use it.
            if (!string.IsNullOrWhiteSpace(userOverride))
            {
                Logger.LogDebug($"Using user override for greeting '{greetingKeyName}': '{userOverride}'");
                return userOverride;
            }

            // 2. If no user override, attempt to retrieve from appsettings.json.
            appSettingDefault = _appConfiguration[$"{configSectionPath}:{greetingKeyName}"];
            if (!string.IsNullOrWhiteSpace(appSettingDefault))
            {
                Logger.LogDebug($"Using appsettings default for greeting '{greetingKeyName}': '{appSettingDefault}'");
                return appSettingDefault;
            }

            // 3. If not found in overrides or appsettings, use the hardcoded fallback.
            Logger.LogWarning($"Greeting key '{greetingKeyName}' not found in user overrides or appsettings (section: '{configSectionPath}'). Using hardcoded fallback: '{DefaultGreetingFallbackText}'");
            return DefaultGreetingFallbackText;
        }

        /// <summary>
        /// Retrieves all current effective greetings by merging user overrides with application defaults.
        /// This method is primarily used to populate the <see cref="ManageGreetingsForm"/> with current values.
        /// </summary>
        /// <returns>A <see cref="UserGreetingSettings"/> object populated with the effective greeting for each defined key.</returns>
        public UserGreetingSettings GetCurrentEffectiveGreetings()
        {
            // Create a new UserGreetingSettings object and populate each property
            // by calling GetGreeting, which handles the override/default logic.
            var effective = new UserGreetingSettings
            {
                AutoRunDaily = GetGreeting("AutoRunDaily"),
                ManualStdDaily = GetGreeting("ManualStdDaily"),
                AutoRunDaily5Day1k = GetGreeting("AutoRunDaily5Day1k"),
                ManualFemi = GetGreeting("ManualFemi"),
                ManualTeam = GetGreeting("ManualTeam"),
                ManualCustom = GetGreeting("ManualCustom"), // Added for manual custom reports
                DebugDefault = GetGreeting("DebugDefault", isForDebugSection: true) // Specify it's a debug greeting
            };
            return effective;
        }
        #endregion
    }
}
