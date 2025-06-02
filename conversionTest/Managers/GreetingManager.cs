// GreetingManager.cs
// Manages loading, saving, and providing email greeting messages.
// Prioritises user-defined overrides over application defaults from appsettings.json.
// Aligned with the new appsettings.json structure.

#region Using Directives
// System related namespaces
using System;
using System.IO;
using System.Text.Json; // For System.Text.Json serialization/deserialization.
using System.Text.Json.Serialization; // For JsonIgnoreCondition

// Third-party namespaces
using Microsoft.Extensions.Configuration; // For IConfiguration.

// Project specific namespaces
using QuoteConversionReportAutomation.Models;   // For UserGreetingSettings model.
using QuoteConversionReportAutomation.Services.Logging; // For Logger.
#endregion

namespace QuoteConversionReportAutomation.Managers
{
    /// <summary>
    /// Manages loading, saving, and providing email greeting messages for various report contexts.
    /// It prioritises user-defined overrides (from `user_greeting_settings.json`) over application defaults
    /// specified in `appsettings.json` (under the `EmailSettings` section).
    /// </summary>
    public class GreetingManager
    {
        #region Fields and Constants

        /// <summary>
        /// Provides access to the application's main configuration settings (from `appsettings.json`).
        /// Used to retrieve default email greeting messages.
        /// </summary>
        private readonly IConfiguration _appConfiguration;

        /// <summary>
        /// In-memory cache of user-defined email greeting overrides, loaded from `user_greeting_settings.json`.
        /// This object is updated when user overrides are saved or cleared.
        /// </summary>
        private UserGreetingSettings _userGreetingOverrides;

        /// <summary>
        /// The full file path to the user-specific JSON file where email greeting overrides are stored.
        /// Typically located in the user's AppData\Roaming directory.
        /// </summary>
        private readonly string _userGreetingsSettingsFilePath;

        /// <summary>
        /// A static lock object to ensure thread-safe access when reading or writing the user settings file.
        /// </summary>
        private static readonly object s_fileLock = new object();

        /// <summary>
        /// Default fallback greeting text if no specific greeting is found in user overrides or application configuration.
        /// </summary>
        private const string DefaultGreetingFallbackText = "Hi Team,";

        // Configuration key base paths for accessing greeting settings in the new appsettings.json structure.
        // These point to the "EmailGreetings" objects within "ProductionRecipients" and "DebugRecipients".
        private const string ProdEmailGreetingsSectionKey = "EmailSettings:ProductionRecipients:EmailGreetings";
        private const string DebugEmailGreetingsSectionKey = "EmailSettings:DebugRecipients:EmailGreetings";

        #endregion

        #region Constructor
        /// <summary>
        /// Initialises a new instance of the <see cref="GreetingManager"/> class.
        /// Loads user greeting overrides from their specific settings file located in the user's AppData directory.
        /// </summary>
        /// <param name="appConfiguration">The application's configuration interface, used for retrieving default greetings.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="appConfiguration"/> is null.</exception>
        public GreetingManager(IConfiguration appConfiguration)
        {
            _appConfiguration = appConfiguration ?? throw new ArgumentNullException(nameof(appConfiguration));

            // Construct the path to the user-specific greeting settings file.
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string companyFolder = "HarlowSolutions";
            string appFolder = "QuoteConversionReportAutomation";
            _userGreetingsSettingsFilePath = Path.Combine(appDataPath, companyFolder, appFolder, "user_greeting_settings.json");

            _userGreetingOverrides = LoadUserGreetingOverrides(); // Load any existing user overrides.
            Logger.LogInfo($"GreetingManager initialised. User greeting overrides loaded from: '{_userGreetingsSettingsFilePath}'");
        }
        #endregion

        #region User Overrides Management
        /// <summary>
        /// Loads user-defined email greeting overrides from their local JSON settings file (`user_greeting_settings.json`).
        /// If the file doesn't exist, is empty, or contains invalid JSON, returns a new <see cref="UserGreetingSettings"/> instance
        /// with all properties as null (or default for their type).
        /// </summary>
        /// <returns>A <see cref="UserGreetingSettings"/> object containing the user's overrides.
        /// Properties will be null if not defined in the user's file, allowing fallback to app defaults.</returns>
        private UserGreetingSettings LoadUserGreetingOverrides()
        {
            try
            {
                if (File.Exists(_userGreetingsSettingsFilePath))
                {
                    string json;
                    lock (s_fileLock) // Ensure thread-safe read.
                    {
                        json = File.ReadAllText(_userGreetingsSettingsFilePath);
                    }
                    if (!string.IsNullOrWhiteSpace(json))
                    {
                        // Deserialize using System.Text.Json.
                        var settings = JsonSerializer.Deserialize<UserGreetingSettings>(json);
                        if (settings != null)
                        {
                            Logger.LogInfo($"Successfully loaded user greeting overrides from '{_userGreetingsSettingsFilePath}'.");
                            return settings; // Loaded settings (properties can be null).
                        }
                        else
                        {
                            Logger.LogWarning($"Deserialization of '{_userGreetingsSettingsFilePath}' for greetings resulted in a null object. Using defaults.");
                        }
                    }
                    else
                    {
                        Logger.LogInfo($"User greeting overrides file '{_userGreetingsSettingsFilePath}' is empty. Using defaults.");
                    }
                }
                else
                {
                    Logger.LogInfo($"User greeting overrides file not found at '{_userGreetingsSettingsFilePath}'. Using defaults. File will be created if overrides are saved.");
                }
            }
            catch (JsonException jsonEx)
            {
                Logger.LogError($"Error deserializing user greeting overrides from '{_userGreetingsSettingsFilePath}': {jsonEx.Message}. File might be corrupt.", jsonEx);
            }
            catch (IOException ioEx)
            {
                Logger.LogError($"IO Error loading user greeting overrides from '{_userGreetingsSettingsFilePath}': {ioEx.Message}", ioEx);
            }
            catch (Exception ex)
            {
                Logger.LogError($"Unexpected error loading user greeting overrides from '{_userGreetingsSettingsFilePath}': {ex.Message}", ex);
            }
            Logger.LogDebug("Returning new UserGreetingSettings instance (no overrides loaded or error occurred).");
            return new UserGreetingSettings(); // Constructor initializes properties to null.
        }

        /// <summary>
        /// Saves the provided <see cref="UserGreetingSettings"/> to the user's local JSON settings file,
        /// overwriting any existing file content. Also updates the in-memory cache of user overrides.
        /// </summary>
        /// <param name="settingsToSave">The <see cref="UserGreetingSettings"/> object to save.
        /// Properties with null values will be ignored during serialization if `DefaultIgnoreCondition` is set appropriately.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="settingsToSave"/> is null.</exception>
        /// <exception cref="IOException">Thrown if an I/O error occurs during directory creation or file writing.</exception>
        /// <exception cref="JsonException">Thrown if an error occurs during JSON serialization.</exception>
        /// <exception cref="Exception">Can throw other exceptions if saving fails (e.g., permission issues).</exception>
        public void SaveUserGreetingOverrides(UserGreetingSettings settingsToSave)
        {
            ArgumentNullException.ThrowIfNull(settingsToSave, nameof(settingsToSave));
            try
            {
                string? directoryPath = Path.GetDirectoryName(_userGreetingsSettingsFilePath);
                if (string.IsNullOrEmpty(directoryPath))
                {
                    throw new InvalidOperationException($"Could not determine directory path from '{_userGreetingsSettingsFilePath}'.");
                }
                if (!Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                    Logger.LogInfo($"Created directory for user greeting settings: {directoryPath}");
                }

                // Configure JsonSerializer options: write indented JSON and ignore null values when writing.
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                };
                string json = JsonSerializer.Serialize(settingsToSave, options);

                lock (s_fileLock) // Ensure thread-safe write.
                {
                    File.WriteAllText(_userGreetingsSettingsFilePath, json);
                }
                _userGreetingOverrides = settingsToSave; // Update the in-memory cache.
                Logger.LogInfo($"User greeting overrides saved to '{_userGreetingsSettingsFilePath}'.");
            }
            catch (JsonException jsonEx)
            {
                Logger.LogError($"Error serializing user greeting overrides for '{_userGreetingsSettingsFilePath}': {jsonEx.Message}", jsonEx);
                throw;
            }
            catch (IOException ioEx)
            {
                Logger.LogError($"IO Error saving user greeting overrides to '{_userGreetingsSettingsFilePath}': {ioEx.Message}", ioEx);
                throw;
            }
            catch (Exception ex)
            {
                Logger.LogError($"Unexpected error saving user greeting overrides to '{_userGreetingsSettingsFilePath}': {ex.Message}", ex);
                throw;
            }
        }

        /// <summary>
        /// Clears all user-defined email greeting overrides by deleting the local settings file
        /// and resetting the in-memory cache to a new <see cref="UserGreetingSettings"/> instance (all properties null).
        /// </summary>
        /// <exception cref="IOException">Can throw file I/O exceptions if deletion of the settings file fails.</exception>
        /// <exception cref="Exception">Can throw other exceptions if deletion fails.</exception>
        public void ClearUserGreetingOverrides()
        {
            try
            {
                lock (s_fileLock) // Ensure thread-safe file operation.
                {
                    if (File.Exists(_userGreetingsSettingsFilePath))
                    {
                        File.Delete(_userGreetingsSettingsFilePath);
                        Logger.LogInfo($"User greeting overrides file '{_userGreetingsSettingsFilePath}' deleted.");
                    }
                }
                _userGreetingOverrides = new UserGreetingSettings(); // Reset in-memory cache.
                Logger.LogInfo("In-memory user greeting overrides reset (all properties are now null, will fallback to app defaults).");
            }
            catch (IOException ioEx)
            {
                Logger.LogError($"IO Error clearing user greeting overrides file '{_userGreetingsSettingsFilePath}': {ioEx.Message}", ioEx);
                throw;
            }
            catch (Exception ex)
            {
                Logger.LogError($"Unexpected error clearing user greeting overrides file '{_userGreetingsSettingsFilePath}': {ex.Message}", ex);
                throw;
            }
        }
        #endregion

        #region Greeting Retrieval Logic
        /// <summary>
        /// Gets the effective greeting string for a given key name.
        /// It prioritises user overrides (from `user_greeting_settings.json`), then checks `appsettings.json`
        /// under the appropriate section (Production or Debug greetings) for a default,
        /// and finally falls back to a hardcoded default greeting (<see cref="DefaultGreetingFallbackText"/>) if none are found.
        /// </summary>
        /// <param name="greetingKeyName">The specific name of the greeting key (e.g., "AutoRunDaily", "ManualCustom", "DebugDefault").
        /// This should match a property name in <see cref="UserGreetingSettings"/> and a key within the
        /// relevant `EmailGreetings` object in `appsettings.json`.</param>
        /// <param name="isForDebugSection">True if the <paramref name="greetingKeyName"/> refers to a key within the
        /// `"EmailSettings:DebugRecipients:EmailGreetings"` section of `appsettings.json`;
        /// false if it refers to `"EmailSettings:ProductionRecipients:EmailGreetings"`.</param>
        /// <returns>The effective greeting string. Returns <see cref="DefaultGreetingFallbackText"/> if no specific greeting is found.</returns>
        public string GetGreeting(string greetingKeyName, bool isForDebugSection = false)
        {
            ArgumentException.ThrowIfNullOrEmpty(greetingKeyName, nameof(greetingKeyName));

            string? userOverride = null;
            string? appSettingDefault = null;

            // Determine the base path in appsettings.json based on whether it's for debug or production.
            string configSectionPath = isForDebugSection ? DebugEmailGreetingsSectionKey : ProdEmailGreetingsSectionKey;

            // 1. Attempt to retrieve the greeting from user overrides.
            //    UserGreetingSettings properties are nullable strings.
            if (_userGreetingOverrides != null)
            {
                try
                {
                    // Use reflection to get the property value from _userGreetingOverrides based on greetingKeyName.
                    var propInfo = typeof(UserGreetingSettings).GetProperty(greetingKeyName);
                    if (propInfo != null && propInfo.CanRead)
                    {
                        userOverride = propInfo.GetValue(_userGreetingOverrides) as string;
                    }
                    else
                    {
                        Logger.LogDebug($"Greeting key '{greetingKeyName}' not found as a readable property in UserGreetingSettings model during override check.");
                    }
                }
                catch (Exception ex) // Catch potential reflection errors.
                {
                    Logger.LogWarning($"Error accessing user override for greeting '{greetingKeyName}' via reflection: {ex.Message}");
                }
            }

            // If a non-empty user override is found, use it.
            if (!string.IsNullOrWhiteSpace(userOverride))
            {
                Logger.LogDebug($"Using user override for greeting '{greetingKeyName}': '{userOverride}'");
                return userOverride;
            }

            // 2. If no user override, attempt to retrieve from appsettings.json using the constructed full key.
            string fullConfigKey = $"{configSectionPath}:{greetingKeyName}";
            appSettingDefault = _appConfiguration[fullConfigKey];

            if (!string.IsNullOrWhiteSpace(appSettingDefault))
            {
                Logger.LogDebug($"Using appsettings default for greeting '{greetingKeyName}' (from key '{fullConfigKey}'): '{appSettingDefault}'");
                return appSettingDefault;
            }

            // 3. If not found in overrides or appsettings, use the hardcoded fallback.
            Logger.LogWarning($"Greeting key '{greetingKeyName}' not found in user overrides or appsettings (config key: '{fullConfigKey}'). Using hardcoded fallback: '{DefaultGreetingFallbackText}'");
            return DefaultGreetingFallbackText;
        }

        /// <summary>
        /// Retrieves all current effective greetings by merging user overrides with application defaults.
        /// This method is primarily used to populate the <see cref="ManageGreetingsForm"/> with current values.
        /// Each property in the returned <see cref="UserGreetingSettings"/> object will represent the
        /// effective greeting for that key, determined by the GetGreeting logic (override > app default > fallback).
        /// </summary>
        /// <returns>A <see cref="UserGreetingSettings"/> object populated with the effective greeting for each defined key.</returns>
        public UserGreetingSettings GetCurrentEffectiveGreetings()
        {
            // Create a new UserGreetingSettings object. Its properties will be null initially.
            var effective = new UserGreetingSettings
            {
                // For each property, call GetGreeting to determine its effective value.
                // GetGreeting handles the logic of checking user overrides, then app defaults, then fallback.
                AutoRunDaily = GetGreeting(nameof(UserGreetingSettings.AutoRunDaily)),
                ManualStdDaily = GetGreeting(nameof(UserGreetingSettings.ManualStdDaily)),
                AutoRunDaily5Day1k = GetGreeting(nameof(UserGreetingSettings.AutoRunDaily5Day1k)),
                ManualFemi = GetGreeting(nameof(UserGreetingSettings.ManualFemi)),
                ManualTeam = GetGreeting(nameof(UserGreetingSettings.ManualTeam)),
                ManualCustom = GetGreeting(nameof(UserGreetingSettings.ManualCustom)),
                // For DebugDefault, specify it's from the debug section of appsettings.json for its default lookup.
                DebugDefault = GetGreeting(nameof(UserGreetingSettings.DebugDefault), isForDebugSection: true)
            };
            Logger.LogDebug("GetCurrentEffectiveGreetings retrieved all effective greeting values.");
            return effective;
        }
        #endregion
    }
}