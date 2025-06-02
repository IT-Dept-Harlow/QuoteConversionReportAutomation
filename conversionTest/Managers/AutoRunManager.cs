// AutoRunManager.cs
// Manages the automated execution of predefined reports based on a schedule
// and configuration settings. It co-ordinates report generation, processing,
// and emailing for these automated tasks for the QCRA application.
// Configuration for paths and operational parameters is read from appsettings.json using the new structure.
// Report definitions are managed in a separate autoReportDefinitions.json file.
// Utilises C# 10+ features.

#region Using Directives
// System related namespaces
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

// Third-party namespaces
using Microsoft.Extensions.Configuration; // For IConfiguration.
using Newtonsoft.Json.Linq; // For JObject manipulation when updating appsettings.json.
using Newtonsoft.Json; // For JSON serialisation/deserialisation.
using Newtonsoft.Json.Serialization; // For DefaultContractResolver

// Project specific namespaces
using QuoteConversionReportAutomation.Helpers;
using QuoteConversionReportAutomation.Models;
using QuoteConversionReportAutomation.Services.Communication;
using QuoteConversionReportAutomation.Services.Excel;
using QuoteConversionReportAutomation.Services.Logging;
#endregion

namespace QuoteConversionReportAutomation.Managers
{
    /// <summary>
    /// Manages the automated (scheduled) generation and processing of reports for the QCRA application.
    /// This class checks daily at a configured hour if any predefined automated reports are due for execution.
    /// If so, it orchestrates their creation, processing, and email distribution.
    /// It loads and saves <see cref="AutoReportDefinition"/> objects to `autoReportDefinitions.json`
    /// and manages its own operational state (like run times and daily success flags) in `appsettings.json`
    /// under the "AutoRunProcess" section.
    /// </summary>
    public class AutoRunManager
    {
        #region Fields and Properties

        // --- Dependencies ---
        private readonly IConfiguration _configuration;
        private readonly EmailUtility _emailUtility;
        private readonly ReportProcessManager _processManager;
        private readonly NamedPipeCommunicator _pipeCommunicator;
        private readonly UIManager _uiManager;
        private readonly ExcelCopyData _excelProcessor;
        private readonly EmailRecipientManager _emailRecipientManager;
        private readonly GreetingManager _greetingManager;
        private readonly string _appSettingsPath;
        private readonly string _reportDefinitionsFilePath;

        // --- State Variables ---
        private static readonly object s_jsonFileLock = new object();
        private bool _isAutoRunTaskExecuting = false;
        private DateTime _lastGlobalSuccessDate = DateTime.MinValue;
        private int _autoRunCheckHour;

        // --- Report Definitions ---
        private List<AutoReportDefinition> _reportDefinitions;

        // --- JSON Keys and Filenames (Updated for new appsettings.json structure) ---
        private const string JsonSectionAutoRunProcess = "AutoRunProcess";
        private const string JsonKeyDailyRunStatus = "DailyRunStatus";
        private const string JsonKeyStatusDate = "StatusDate";
        private const string JsonKeyLastRunDate = "LastRunDate";
        private const string JsonKeyAutoRunCheckHour = "CheckHour";
        private readonly string _reportDefinitionsFileName;

        // --- Build Configuration ---
        private static bool IsDebug =>
#if DEBUG
            true;
#else
            false;
#endif
        #endregion

        #region Configuration-derived Path Properties
        /// <summary>
        /// Gets the current user's profile directory path.
        /// </summary>
        private string UserProfilePath
        {
            get
            {
                try { return Environment.GetFolderPath(Environment.SpecialFolder.UserProfile); }
                catch (Exception ex)
                {
                    Logger.LogError($"AutoRunManager: Error getting UserProfilePath: {ex.Message}. Defaulting to current directory.", ex);
                    return Environment.CurrentDirectory;
                }
            }
        }

        /// <summary>
        /// Gets the base directory for saving final processed Excel analysis files for automated reports.
        /// Path is UserProfile + configured "Paths:FinalReportOutputBase".
        /// </summary>
        private string ExcelFinalSaveLocation
        {
            get
            {
                string? relativePath = _configuration["Paths:FinalReportOutputBase"];
                string defaultRelativePath = @"Harlow Printing\IT Projects - Documents\Dashboard Datasets\Raw_data\Quotes conversion\Estimates";
                if (string.IsNullOrWhiteSpace(relativePath))
                {
                    Logger.LogWarning($"AutoRunManager: Config key 'Paths:FinalReportOutputBase' missing. Using default: '{defaultRelativePath}'");
                    relativePath = defaultRelativePath;
                }
                else { relativePath = relativePath.TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar); }
                try { return Path.Combine(UserProfilePath, relativePath); }
                catch (ArgumentException argEx)
                {
                    Logger.LogError($"AutoRunManager: Error constructing ExcelFinalSaveLocation. UserProfile='{UserProfilePath}', Relative='{relativePath}'. Error: {argEx.Message}", argEx);
                    return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "QCRA_AutoRun_Fallback", "FinalReports");
                }
            }
        }

        /// <summary>
        /// Gets the full path to the Crystal Report definition file (.rpt) from "Paths:CrystalReportRptFile".
        /// </summary>
        private string CrystalReportLocation
        {
            get
            {
                string? configuredPath = _configuration["Paths:CrystalReportRptFile"];
                if (string.IsNullOrWhiteSpace(configuredPath))
                {
                    Logger.LogWarning("AutoRunManager: Config key 'Paths:CrystalReportRptFile' missing. Crystal Report location unknown.");
                    return string.Empty;
                }
                try { return Path.GetFullPath(configuredPath); }
                catch (Exception ex)
                {
                    Logger.LogError($"AutoRunManager: Error resolving CrystalReportLocation from '{configuredPath}': {ex.Message}", ex);
                    return string.Empty;
                }
            }
        }

        /// <summary>
        /// Gets the base directory for exporting raw Crystal Reports for automated runs.
        /// Path is UserProfile + configured "Paths:RawReportOutputBase".
        /// </summary>
        private string RawReportExportBaseDir
        {
            get
            {
                string? relativePath = _configuration["Paths:RawReportOutputBase"];
                string defaultRelativePath = @"Harlow Printing\IT Projects - Documents\Dashboard Datasets\Raw_data\Quotes conversion\Estimate Reports Exports";
                if (string.IsNullOrWhiteSpace(relativePath))
                {
                    Logger.LogWarning($"AutoRunManager: Config key 'Paths:RawReportOutputBase' missing. Using default: '{defaultRelativePath}'");
                    relativePath = defaultRelativePath;
                }
                else { relativePath = relativePath.TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar); }
                try { return Path.Combine(UserProfilePath, relativePath); }
                catch (ArgumentException argEx)
                {
                    Logger.LogError($"AutoRunManager: Error constructing RawReportExportBaseDir. UserProfile='{UserProfilePath}', Relative='{relativePath}'. Error: {argEx.Message}", argEx);
                    return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "QCRA_AutoRun_Fallback", "RawExports");
                }
            }
        }

        /// <summary>
        /// Gets the base directory where Excel template files are stored for automated reports.
        /// Path is UserProfile + configured "Paths:TemplateBase".
        /// </summary>
        public string ExcelTemplateBaseDir
        {
            get
            {
                string? relativePath = _configuration["Paths:TemplateBase"];
                string defaultRelativePath = @"Harlow Printing\IT Projects - Documents\Dashboard Datasets\Raw_data\Quotes conversion\TEMPLATE";
                if (string.IsNullOrWhiteSpace(relativePath))
                {
                    Logger.LogWarning($"AutoRunManager: Config key 'Paths:TemplateBase' missing. Using default: '{defaultRelativePath}'");
                    relativePath = defaultRelativePath;
                }
                else { relativePath = relativePath.TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar); }
                try { return Path.Combine(UserProfilePath, relativePath); }
                catch (ArgumentException argEx)
                {
                    Logger.LogError($"AutoRunManager: Error constructing ExcelTemplateBaseDir. UserProfile='{UserProfilePath}', Relative='{relativePath}'. Error: {argEx.Message}", argEx);
                    return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "QCRA_AutoRun_Fallback", "Templates");
                }
            }
        }
        #endregion

        #region Constructor
        /// <summary>
        /// Initialises a new instance of the <see cref="AutoRunManager"/> class.
        /// </summary>
        /// <param name="configuration">The application's main configuration settings.</param>
        /// <param name="emailUtility">Utility for sending emails.</param>
        /// <param name="processManager">Manager for the Crystal Report wrapper process.</param>
        /// <param name="pipeCommunicator">Service for IPC with the report wrapper.</param>
        /// <param name="uiManager">Manager for UI updates.</param>
        /// <param name="excelProcessor">Service for Excel file processing.</param>
        /// <param name="appSettingsPath">Full path to `appsettings.json`, used to locate `autoReportDefinitions.json` and store operational state.</param>
        /// <param name="emailRecipientManager">Manager for email recipient lists.</param>
        /// <param name="greetingManager">Manager for email greeting messages.</param>
        /// <param name="initialAutoRunHour">Initial configured hour (0-23) for daily auto-run checks.</param>
        public AutoRunManager(
            IConfiguration configuration,
            EmailUtility emailUtility,
            ReportProcessManager processManager,
            NamedPipeCommunicator pipeCommunicator,
            UIManager uiManager,
            ExcelCopyData excelProcessor,
            string appSettingsPath,
            EmailRecipientManager emailRecipientManager,
            GreetingManager greetingManager,
            int initialAutoRunHour)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _emailUtility = emailUtility ?? throw new ArgumentNullException(nameof(emailUtility));
            _processManager = processManager ?? throw new ArgumentNullException(nameof(processManager));
            _pipeCommunicator = pipeCommunicator ?? throw new ArgumentNullException(nameof(pipeCommunicator));
            _uiManager = uiManager ?? throw new ArgumentNullException(nameof(uiManager));
            _excelProcessor = excelProcessor ?? throw new ArgumentNullException(nameof(excelProcessor));
            _appSettingsPath = appSettingsPath ?? throw new ArgumentNullException(nameof(appSettingsPath));
            _emailRecipientManager = emailRecipientManager ?? throw new ArgumentNullException(nameof(emailRecipientManager));
            _greetingManager = greetingManager ?? throw new ArgumentNullException(nameof(greetingManager));
            _autoRunCheckHour = initialAutoRunHour;

            _reportDefinitionsFileName = _configuration.GetValue<string>("Paths:ReportDefinitionsFileName", "autoReportDefinitions.json")!;
            if (string.IsNullOrWhiteSpace(_reportDefinitionsFileName))
            {
                _reportDefinitionsFileName = "autoReportDefinitions.json";
                Logger.LogWarning($"Config 'Paths:ReportDefinitionsFileName' missing. Defaulting: '{_reportDefinitionsFileName}'");
            }

            string? appSettingsDir = Path.GetDirectoryName(_appSettingsPath);
            if (string.IsNullOrEmpty(appSettingsDir))
            {
                string errorMsg = $"Could not determine directory from appSettingsPath: '{_appSettingsPath}'. Critical for '{_reportDefinitionsFileName}'.";
                Logger.LogCritical(errorMsg);
                throw new DirectoryNotFoundException(errorMsg);
            }
            _reportDefinitionsFilePath = Path.Combine(appSettingsDir, _reportDefinitionsFileName);
            Logger.LogInfo($"AutoRunManager: Report definitions path: '{_reportDefinitionsFilePath}'");

            _reportDefinitions = LoadReportDefinitions(_reportDefinitionsFilePath);
            Logger.LogInfo($"AutoRunManager: Loaded {_reportDefinitions.Count} report definitions.");

            _lastGlobalSuccessDate = ReadLastGlobalSuccessDate();
            Logger.LogInfo($"AutoRunManager initialised. Check Hour: {_autoRunCheckHour}. Last Global Success: {_lastGlobalSuccessDate:yyyy-MM-dd}");
        }
        #endregion

        #region Report Definition Management (Static Methods)
        /// <summary>
        /// Loads <see cref="AutoReportDefinition"/> objects from the specified dedicated JSON file.
        /// Static to allow UI forms to manage definitions.
        /// </summary>
        /// <param name="definitionsFilePath">Full path to the JSON file (e.g., `autoReportDefinitions.json`).</param>
        /// <returns>A list of <see cref="AutoReportDefinition"/>. Empty list on error or if file not found.</returns>
        public static List<AutoReportDefinition> LoadReportDefinitions(string definitionsFilePath)
        {
            ArgumentException.ThrowIfNullOrEmpty(definitionsFilePath, nameof(definitionsFilePath));
            List<AutoReportDefinition>? definitions = null;

            if (File.Exists(definitionsFilePath))
            {
                try
                {
                    string jsonContent;
                    lock (s_jsonFileLock)
                    {
                        jsonContent = File.ReadAllText(definitionsFilePath);
                    }

                    if (string.IsNullOrWhiteSpace(jsonContent))
                    {
                        Logger.LogInfo($"Report definitions file '{definitionsFilePath}' is empty. Returning empty list.");
                        return new List<AutoReportDefinition>();
                    }

                    definitions = JsonConvert.DeserializeObject<List<AutoReportDefinition>>(jsonContent,
                        new JsonSerializerSettings
                        {
                            NullValueHandling = NullValueHandling.Ignore,
                            ContractResolver = new DefaultContractResolver() // Respects JsonProperty attributes
                        });
                    Logger.LogDebug($"Loaded {definitions?.Count ?? 0} definitions from file: '{definitionsFilePath}'.");
                }
                catch (JsonException jsonEx)
                {
                    Logger.LogError($"Error parsing report definitions from '{definitionsFilePath}': {jsonEx.Message}. File might be corrupt.", jsonEx);
                    definitions = null;
                }
                catch (IOException ioEx)
                {
                    Logger.LogError($"IO Error reading report definitions file '{definitionsFilePath}': {ioEx.Message}", ioEx);
                    definitions = null;
                }
                catch (Exception ex)
                {
                    Logger.LogError($"Unexpected error reading report definitions from '{definitionsFilePath}': {ex.Message}", ex);
                    definitions = null;
                }
            }
            else
            {
                Logger.LogWarning($"Report definitions file not found: '{definitionsFilePath}'. Returning empty list.");
            }

            if (definitions == null)
            {
                return new List<AutoReportDefinition>();
            }

            bool idsGenerated = false;
            foreach (var def in definitions)
            {
                if (string.IsNullOrWhiteSpace(def.ReportId))
                {
                    def.ReportId = Guid.NewGuid().ToString();
                    idsGenerated = true;
                    Logger.LogWarning($"Generated new ReportId '{def.ReportId}' for definition named '{def.ReportName}' as its ID was missing.");
                }
            }
            if (idsGenerated)
            {
                Logger.LogInfo("New ReportIds were generated for some definitions. These will be saved when 'Save All Changes' is used in the management UI.");
            }
            return definitions;
        }

        /// <summary>
        /// Saves the provided list of <see cref="AutoReportDefinition"/> objects to the dedicated JSON file.
        /// Static to allow UI forms to call it directly.
        /// </summary>
        /// <param name="definitionsFilePath">Full path to the JSON file (e.g., `autoReportDefinitions.json`).</param>
        /// <param name="definitionsToSave">The list of definitions to save.</param>
        public static void SaveReportDefinitions(string definitionsFilePath, List<AutoReportDefinition> definitionsToSave)
        {
            ArgumentNullException.ThrowIfNull(definitionsToSave, nameof(definitionsToSave));
            ArgumentException.ThrowIfNullOrEmpty(definitionsFilePath, nameof(definitionsFilePath));

            lock (s_jsonFileLock)
            {
                try
                {
                    string definitionsJsonString = JsonConvert.SerializeObject(definitionsToSave, Formatting.Indented,
                                                       new JsonSerializerSettings
                                                       {
                                                           NullValueHandling = NullValueHandling.Ignore,
                                                           ContractResolver = new DefaultContractResolver()
                                                       });
                    string? directoryPath = Path.GetDirectoryName(definitionsFilePath);
                    if (!string.IsNullOrEmpty(directoryPath) && !Directory.Exists(directoryPath))
                    {
                        Directory.CreateDirectory(directoryPath);
                        Logger.LogInfo($"Created directory for report definitions: {directoryPath}");
                    }
                    File.WriteAllText(definitionsFilePath, definitionsJsonString);
                    Logger.LogInfo($"Successfully saved {definitionsToSave.Count} report definitions to '{definitionsFilePath}'.");
                }
                catch (JsonException jsonEx)
                {
                    Logger.LogError($"Error serializing report definitions for '{definitionsFilePath}': {jsonEx.Message}", jsonEx);
                    throw;
                }
                catch (IOException ioEx)
                {
                    Logger.LogError($"IO error saving report definitions to '{definitionsFilePath}': {ioEx.Message}", ioEx);
                    throw;
                }
                catch (Exception ex)
                {
                    Logger.LogError($"Unexpected error saving report definitions to '{definitionsFilePath}': {ex.Message}", ex);
                    throw;
                }
            }
        }

        /// <summary>
        /// Reloads report definitions from the configured file path into the manager's internal list.
        /// </summary>
        public void ReloadReportDefinitions()
        {
            _reportDefinitions = LoadReportDefinitions(_reportDefinitionsFilePath);
            Logger.LogInfo($"AutoRunManager: Report definitions reloaded from '{_reportDefinitionsFilePath}'. Count: {_reportDefinitions.Count}");
        }
        #endregion

        #region Public Methods (PerformDailyCheckAsync, SetAutoRunHourAsync)
        /// <summary>
        /// Performs the daily check for automated reports.
        /// Reads operational state from "AutoRunProcess" section in `appsettings.json`.
        /// </summary>
        /// <param name="isTimerCurrentlyEnabled">If the main UI timer is enabled.</param>
        /// <param name="configuredHour">The current auto-run hour from `Form1` (read from "AutoRunProcess:CheckHour").</param>
        /// <returns>An <see cref="AutoRunActionResult"/> indicating the outcome.</returns>
        public async Task<AutoRunActionResult> PerformDailyCheckAsync(bool isTimerCurrentlyEnabled, int configuredHour)
        {
            ReloadReportDefinitions();

            if (!isTimerCurrentlyEnabled)
            {
                Logger.LogInfo("Auto Run: Timer is disabled by user. Skipping daily check.");
                return AutoRunActionResult.NoActionNeeded;
            }
            if (!_reportDefinitions.Any())
            {
                Logger.LogInfo("Auto Run: No report definitions loaded. Skipping daily check.");
                return AutoRunActionResult.NoActionNeeded;
            }
            if (_isAutoRunTaskExecuting)
            {
                Logger.LogInfo("Auto Run: A daily check task is already executing. Skipping this cycle.");
                return AutoRunActionResult.NoActionNeeded;
            }

            DateTime now = DateTime.Now;
            _autoRunCheckHour = configuredHour; // This comes from Form1, which reads "AutoRunProcess:CheckHour"
            AutoRunActionResult overallResult = AutoRunActionResult.NoActionNeeded;
            DailyReportRunStatus currentDayStatuses = ReadDailyReportStatuses(); // Reads from "AutoRunProcess:DailyRunStatus"

            if (currentDayStatuses.StatusDate != now.ToString("yyyy-MM-dd"))
            {
                Logger.LogInfo($"New day detected ({now:yyyy-MM-dd}). Resetting daily report success flags.");
                ResetDailyReportStatuses(now.Date); // Writes to "AutoRunProcess:DailyRunStatus"
                currentDayStatuses = ReadDailyReportStatuses();
                _uiManager.UpdateAutoRunUI(true, false, UIManager.IsWindowsDarkModeEnabled(), $"Auto Run: Enabled (Next check ~{_autoRunCheckHour}:00)");
            }

            if (now.Hour != _autoRunCheckHour)
            {
                Logger.LogDebug($"Auto Run: Not the configured execution hour ({_autoRunCheckHour}). Current: {now.Hour}. Skipping for this tick.");
                return AutoRunActionResult.NoActionNeeded;
            }

            bool allDueReportsAlreadySucceeded = currentDayStatuses.AllCurrentlyEnabledAndDueReportsSucceeded(_reportDefinitions, now.DayOfWeek);
            int totalEnabledAndDueTodayForInitialCheck = _reportDefinitions.Count(def =>
                def.IsEnabled && (!def.RunOnDayOfWeek.HasValue || def.RunOnDayOfWeek.Value == now.DayOfWeek));

            if (allDueReportsAlreadySucceeded && totalEnabledAndDueTodayForInitialCheck > 0)
            {
                Logger.LogInfo($"Auto Run: All enabled AND DUE reports have already succeeded for {now:yyyy-MM-dd}. No further action needed at this hour.");
                _uiManager.UpdateStatusRight($"Auto Run: Done for {now:dd/MM}");
                _uiManager.UpdateAutoRunUI(true, true, UIManager.IsWindowsDarkModeEnabled(), $"Auto Run: Done for {now:dd/MM}");
                return AutoRunActionResult.NoActionNeeded;
            }

            _isAutoRunTaskExecuting = true;
            overallResult = AutoRunActionResult.ActionAttempted; // Assume action will be attempted
            _uiManager.DisableControlsForAutoRun();
            _uiManager.UpdateStatusMain($"Auto Run: Starting checks for {now:dd-MM-yyyy} (scheduled ~{_autoRunCheckHour}:00)...");
            Logger.LogInfo($"Auto Run: Daily check triggered for {now:yyyy-MM-dd} at {now:HH:mm:ss}.");

            bool anyReportActuallyAttemptedThisCycle = false;
            try
            {
                foreach (var definition in _reportDefinitions)
                {
                    if (!definition.IsEnabled)
                    {
                        Logger.LogInfo($"Auto Run: Report '{definition.ReportName}' (ID: {definition.ReportId}) is DISABLED. Skipping.");
                        continue;
                    }
                    if (definition.RunOnDayOfWeek.HasValue && now.DayOfWeek != definition.RunOnDayOfWeek.Value)
                    {
                        Logger.LogInfo($"Auto Run: Report '{definition.ReportName}' runs on {definition.RunOnDayOfWeek.Value}, today is {now.DayOfWeek}. Skipping.");
                        continue;
                    }

                    currentDayStatuses = ReadDailyReportStatuses(); // Refresh status before checking this specific report.
                    if (currentDayStatuses.GetReportSuccessStatus(definition.SuccessFlagJsonName))
                    {
                        Logger.LogInfo($"Auto Run: Report '{definition.ReportName}' (Flag: {definition.SuccessFlagJsonName}) already succeeded today. Skipping.");
                        continue;
                    }

                    anyReportActuallyAttemptedThisCycle = true;
                    _uiManager.UpdateStatusMain($"Auto Run: Processing {definition.ReportName}...");
                    Logger.LogInfo($"Auto Run: Report '{definition.ReportName}' (ID: {definition.ReportId}) is ENABLED and PENDING. Attempting to run.");

                    DateTime reportEndDate;
                    DateTime? reportStartDate = null;

                    if (definition.ReportName.Equals("Weekly Estimate Success Rate", StringComparison.OrdinalIgnoreCase))
                    {
                        reportEndDate = now.Date; // Report ends today.
                        reportStartDate = reportEndDate.AddDays(-14); // Covers the last 15 days (inclusive).
                    }
                    else if (definition.ReportEndDateOffsetDays.HasValue)
                    {
                        reportEndDate = ReportHelper.GetNthPreviousWorkday(now.Date, definition.ReportEndDateOffsetDays.Value);
                        if (definition.ReportDurationDays.HasValue && definition.ReportDurationDays.Value > 1)
                        {
                            reportStartDate = ReportHelper.GetNthPreviousWorkday(reportEndDate, definition.ReportDurationDays.Value - 1);
                        }
                        else
                        {
                            reportStartDate = reportEndDate;
                        }
                    }
                    else
                    {
                        reportEndDate = ReportHelper.GetPreviousWorkday(now.Date); // Fallback
                        reportStartDate = reportEndDate;
                        Logger.LogWarning($"Auto Run: Report '{definition.ReportName}' has no specific date offset or duration defined. Defaulting to previous workday ({reportEndDate:yyyy-MM-dd}).");
                    }

                    await RunConfiguredAutomatedReportAsync(definition, reportEndDate, reportStartDate, now.Date);
                } // End foreach report definition loop.

                if (!anyReportActuallyAttemptedThisCycle && totalEnabledAndDueTodayForInitialCheck > 0 && allDueReportsAlreadySucceeded)
                {
                    overallResult = AutoRunActionResult.NoActionNeeded;
                }
                else if (!anyReportActuallyAttemptedThisCycle && totalEnabledAndDueTodayForInitialCheck == 0)
                {
                    overallResult = AutoRunActionResult.NoActionNeeded;
                    Logger.LogInfo("Auto Run: No reports were enabled and due for execution in this cycle.");
                }

                currentDayStatuses = ReadDailyReportStatuses(); // Re-read statuses from appsettings.json after processing.
                bool allEnabledAndDueReportsSucceededToday = currentDayStatuses.AllCurrentlyEnabledAndDueReportsSucceeded(_reportDefinitions, now.DayOfWeek);
                int totalSucceededAmongEnabledAndDueToday = _reportDefinitions.Count(def =>
                    def.IsEnabled &&
                    (!def.RunOnDayOfWeek.HasValue || def.RunOnDayOfWeek.Value == now.DayOfWeek) &&
                    currentDayStatuses.GetReportSuccessStatus(def.SuccessFlagJsonName)
                );

                string finalStatusMessage;
                if (totalEnabledAndDueTodayForInitialCheck == 0)
                {
                    finalStatusMessage = _reportDefinitions.Any(d => d.IsEnabled) ? $"Auto Run: No reports due today {now:dd/MM HH:mm}"
                                                                  : $"Auto Run: No reports currently enabled {now:dd/MM HH:mm}";
                }
                else if (allEnabledAndDueReportsSucceededToday)
                {
                    SaveLastGlobalSuccessDate(now.Date); // Mark the day as globally successful in "AutoRunProcess:LastRunDate"
                    _lastGlobalSuccessDate = now.Date;
                    finalStatusMessage = $"Auto Run: All due DONE ({totalSucceededAmongEnabledAndDueToday}/{totalEnabledAndDueTodayForInitialCheck}) {now:dd/MM HH:mm}";
                }
                else
                {
                    finalStatusMessage = $"Auto Run: Partial success ({totalSucceededAmongEnabledAndDueToday}/{totalEnabledAndDueTodayForInitialCheck} due reports succeeded) {now:dd/MM HH:mm}";
                    Logger.LogWarning(finalStatusMessage + ". Check logs for errors. Will retry incomplete reports if app restarts or at next check hour if within the same day.");
                }
                _uiManager.UpdateStatusRight(finalStatusMessage);
                _uiManager.UpdateAutoRunUI(isTimerCurrentlyEnabled, allEnabledAndDueReportsSucceededToday && totalEnabledAndDueTodayForInitialCheck > 0, UIManager.IsWindowsDarkModeEnabled(), finalStatusMessage);
            }
            catch (Exception ex)
            {
                Logger.LogCritical($"Auto Run: CRITICAL Unhandled exception during PerformDailyCheckAsync: {ex.Message}", ex);
                string errorMsg = $"Auto Run: CRITICAL ERROR {now:dd/MM HH:mm}";
                _uiManager.UpdateStatusRight(errorMsg);
                _uiManager.UpdateAutoRunUI(isTimerCurrentlyEnabled, true, UIManager.IsWindowsDarkModeEnabled(), errorMsg); // Mark as final status (error)
                overallResult = AutoRunActionResult.CriticalError;
            }
            finally
            {
                _isAutoRunTaskExecuting = false;
                if (overallResult == AutoRunActionResult.ActionAttempted && !anyReportActuallyAttemptedThisCycle && totalEnabledAndDueTodayForInitialCheck > 0 && allDueReportsAlreadySucceeded)
                {
                    overallResult = AutoRunActionResult.NoActionNeeded;
                }
            }
            return overallResult;
        }

        /// <summary>
        /// Sets the configured hour for daily auto-run checks and saves this setting to `appsettings.json`
        /// under the "AutoRunProcess:CheckHour" key.
        /// </summary>
        /// <param name="newHour">The new hour (0-23) for the daily auto-run check.</param>
        /// <returns>A task that represents the asynchronous operation. The task result is true if the setting was successfully saved; otherwise, false.</returns>
        public async Task<bool> SetAutoRunHourAsync(int newHour)
        {
            if (newHour < 0 || newHour > 23)
            {
                Logger.LogError($"SetAutoRunHourAsync: Invalid hour provided: {newHour}. Hour must be between 0 and 23 inclusive.");
                return false;
            }
            // Note: _autoRunCheckHour field in this class instance is updated upon successful save.
            // The value displayed in Form1 is managed by Form1's _currentAutoRunHour and UIManager.
            Logger.LogInfo($"SetAutoRunHourAsync: Requested to set auto-run check hour to {newHour}. Attempting to save to appsettings.json.");

            return await Task.Run(() => // Offload file I/O to a background thread.
            {
                lock (s_jsonFileLock) // Ensure thread-safe write to appsettings.json.
                {
                    try
                    {
                        if (!File.Exists(_appSettingsPath))
                        {
                            Logger.LogError($"SetAutoRunHourAsync: appsettings.json not found at '{_appSettingsPath}'. Cannot save '{JsonSectionAutoRunProcess}:{JsonKeyAutoRunCheckHour}'.");
                            return false;
                        }
                        string jsonContent = File.ReadAllText(_appSettingsPath);
                        var jsonRoot = JObject.Parse(string.IsNullOrWhiteSpace(jsonContent) ? "{}" : jsonContent);

                        // Navigate to or create the "AutoRunProcess" section.
                        JObject autoRunProcessSection = GetOrAddSection(jsonRoot, JsonSectionAutoRunProcess);
                        // Set the "CheckHour" key within the "AutoRunProcess" section.
                        autoRunProcessSection[JsonKeyAutoRunCheckHour] = newHour;

                        File.WriteAllText(_appSettingsPath, jsonRoot.ToString(Formatting.Indented)); // Save changes.
                        Logger.LogInfo($"Successfully saved '{JsonSectionAutoRunProcess}:{JsonKeyAutoRunCheckHour}' (value: {newHour}) to appsettings.json.");
                        _autoRunCheckHour = newHour; // Update the internal field after successful save.
                        return true;
                    }
                    catch (JsonException jsonEx)
                    {
                        Logger.LogError($"SetAutoRunHourAsync: Error parsing '{_appSettingsPath}' to update '{JsonKeyAutoRunCheckHour}': {jsonEx.Message}", jsonEx);
                        return false;
                    }
                    catch (IOException ioEx)
                    {
                        Logger.LogError($"SetAutoRunHourAsync: IO error saving '{JsonKeyAutoRunCheckHour}' to appsettings.json: {ioEx.Message}", ioEx);
                        return false;
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError($"SetAutoRunHourAsync: Unexpected error saving '{JsonKeyAutoRunCheckHour}' to appsettings.json: {ex.Message}", ex);
                        return false;
                    }
                }
            });
        }
        #endregion

        #region Private Helper Methods for AutoRun
        /// <summary>
        /// Orchestrates the execution of a single configured automated report.
        /// This includes generating the raw report, processing it via Excel, and emailing the result.
        /// Uses the class's refactored path properties and configuration settings.
        /// </summary>
        /// <param name="definition">The <see cref="AutoReportDefinition"/> for the report to run.</param>
        /// <param name="reportEndDate">The calculated end date for the report's data period.</param>
        /// <param name="reportStartDate">The calculated start date for the report's data period.</param>
        /// <param name="processingDate">The current date, used for recording run status.</param>
        /// <returns>True if the report was fully processed and emailed successfully; otherwise, false.</returns>
        private async Task<bool> RunConfiguredAutomatedReportAsync(AutoReportDefinition definition, DateTime reportEndDate, DateTime? reportStartDate, DateTime processingDate)
        {
            DateTime effectiveReportStartDate = reportStartDate ?? reportEndDate;
            Logger.LogInfo($"Auto Run: Executing report: '{definition.ReportName}' for period {effectiveReportStartDate:yyyy-MM-dd} to {reportEndDate:yyyy-MM-dd}. (Processing on: {processingDate:yyyy-MM-dd})");

            bool overallSuccess = false;
            int processTimeoutMinutes = _configuration.GetValue<int>("OperationalParameters:ProcessTimeoutMinutes", 15); // Default 15 mins
            using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(processTimeoutMinutes));
            var token = cts.Token;

            IProgress<string> localProgress = new Progress<string>(status => Logger.LogDebug($"AutoRun ({definition.ReportName}): {status}"));
            IProgress<ProgressReport> localExcelProgress = new Progress<ProgressReport>(report => Logger.LogDebug($"AutoRun ({definition.ReportName}) Excel: {report.Message} ({report.Percentage}%)"));

            string? generatedRawPath = null;
            string? finalAnalysisPath = null;

            try
            {
                // Step 1: Ensure Wrapper Service is Running
                _uiManager.UpdateProgress($"Auto Run ({definition.ReportName}): Ensuring report service is active...");
                if (!await _processManager.EnsureWrapperIsRunningAsync(localProgress, token))
                {
                    throw new InvalidOperationException($"Auto Run Error ({definition.ReportName}): Failed to start or connect to the report service (CrystalReportWrapper).");
                }

                // Step 2: Generate Raw Report
                _uiManager.UpdateProgress($"Auto Run ({definition.ReportName}): Preparing request for raw report data...");
                string outputPath = GetAutomatedReportOutputPath(definition.ReportTypeIndex, reportEndDate, definition.ReportName); // Uses refactored RawReportExportBaseDir
                string crystalReportPath = CrystalReportLocation; // Uses refactored CrystalReportLocation
                if (string.IsNullOrEmpty(crystalReportPath) || !File.Exists(crystalReportPath))
                {
                    throw new FileNotFoundException($"Auto Run Error ({definition.ReportName}): Crystal Report file path ('{crystalReportPath}') is invalid or missing. Check 'Paths:CrystalReportRptFile'.", crystalReportPath);
                }

                var request = new ReportRequest
                {
                    CrystalReportLocation = crystalReportPath,
                    ReportOutputLocation = outputPath,
                    ReportDateFrom = effectiveReportStartDate,
                    ReportDateTo = reportEndDate
                };
                ReportResponse? response = await _pipeCommunicator.SendRequestReceiveResponseAsync(request, localProgress, token);

                if (response?.Success == true && !string.IsNullOrEmpty(response.OutputPath) && File.Exists(response.OutputPath))
                {
                    generatedRawPath = response.OutputPath;
                    Logger.LogInfo($"Auto Run ({definition.ReportName}): Raw report generated successfully: {generatedRawPath}");
                }
                else
                {
                    string errorMsg = response?.ErrorMessage ?? "Unknown error from report service during raw report generation.";
                    if (response?.Success == true) errorMsg = $"Report service indicated success, but output file ('{response?.OutputPath ?? "NULL"}') is invalid/missing.";
                    throw new Exception($"Auto Run Error ({definition.ReportName}): Raw report generation failed: {errorMsg}");
                }

                // Step 3: Process Excel Report
                _uiManager.UpdateProgress($"Auto Run ({definition.ReportName}): Processing final analysis file...");
                string templatePath = Path.Combine(ExcelTemplateBaseDir, definition.TemplateName); // Uses refactored ExcelTemplateBaseDir
                string baseSaveLocation = ExcelFinalSaveLocation; // Uses refactored ExcelFinalSaveLocation
                string currentFY = _excelProcessor.GetCurrentFinancialYear(true); // Uses configured FY start month/day via ExcelCopyData's IConfig

                if (!File.Exists(templatePath))
                {
                    throw new FileNotFoundException($"Auto Run Error ({definition.ReportName}): Excel template '{definition.TemplateName}' not found at '{templatePath}'. Check 'Paths:TemplateBase'.", templatePath);
                }

                string? expectedFinalPath = _excelProcessor.GetExpectedFinalFilePath(definition.ReportTypeIndex, baseSaveLocation, reportEndDate);
                if (expectedFinalPath != null && File.Exists(expectedFinalPath))
                {
                    try { File.Delete(expectedFinalPath); Logger.LogInfo($"Auto Run ({definition.ReportName}): Deleted existing final analysis file to ensure fresh processing: {expectedFinalPath}"); }
                    catch (Exception delEx) { Logger.LogWarning($"Auto Run ({definition.ReportName}): Failed to delete existing final file '{expectedFinalPath}': {delEx.Message}. Processing will attempt to overwrite."); }
                }

                // Get sheet names from configuration
                string rawDataSourceSheet = _configuration.GetValue<string>("OperationalParameters:ExcelSheetNames:RawDataSourceSheet", "Sheet1")!;
                string templateDataCopySheet = _configuration.GetValue<string>("OperationalParameters:ExcelSheetNames:TemplateDataCopySheet", "DATA")!;

                finalAnalysisPath = await _excelProcessor.ProcessExcelReportAsync(
                    currentFY, definition.ReportTypeIndex, generatedRawPath, rawDataSourceSheet, // Pass configured sheet name
                    baseSaveLocation, templatePath, templateDataCopySheet, // Pass configured sheet name
                    1, 1, localExcelProgress, reportEndDate, token);

                if (string.IsNullOrEmpty(finalAnalysisPath) || !File.Exists(finalAnalysisPath))
                {
                    if (token.IsCancellationRequested) throw new OperationCanceledException($"Auto Run ({definition.ReportName}): Excel processing was cancelled.");
                    throw new Exception($"Auto Run Error ({definition.ReportName}): Excel processing failed to produce a final analysis file. Check logs from ExcelCopyData.");
                }
                Logger.LogInfo($"Auto Run ({definition.ReportName}): Report processed successfully. Final analysis file: {finalAnalysisPath}");

                // Step 4: Send Email
                _uiManager.UpdateProgress($"Auto Run ({definition.ReportName}): Preparing and sending completion email...");
                var (mailTo, mailCc) = _emailRecipientManager.GetRecipients(
                    definition.ReportTypeIndex, false, IsDebug, true, definition); // EmailRecipientManager uses new config
                var (subject, body) = GetEmailSubjectAndBodyForAutoRun(definition, effectiveReportStartDate, reportEndDate); // Uses GreetingManager (needs new config)

                EmailSendResult emailResult = await _emailUtility.SendEmailAsync(mailTo, mailCc, subject, body, finalAnalysisPath, localProgress, token); // EmailUtility uses new config
                if (!emailResult.Success)
                {
                    if (token.IsCancellationRequested && emailResult.ErrorMessage?.Contains("cancel", StringComparison.OrdinalIgnoreCase) == true)
                    {
                        throw new OperationCanceledException($"Auto Run ({definition.ReportName}): Email sending was cancelled.");
                    }
                    throw new Exception($"Auto Run Error ({definition.ReportName}): Email sending failed. Details: {emailResult.ErrorMessage}");
                }
                Logger.LogInfo($"Auto Run ({definition.ReportName}): Email sent successfully for report '{definition.ReportName}'.");
                overallSuccess = true; // All steps completed successfully for this report.
            }
            catch (OperationCanceledException)
            {
                Logger.LogWarning($"Auto Run ({definition.ReportName}): Operation was cancelled.");
                _uiManager.UpdateProgress($"Auto Run ({definition.ReportName}): Operation cancelled.");
                overallSuccess = false;
            }
            catch (Exception ex)
            {
                Logger.LogError($"Auto Run ({definition.ReportName}): An error occurred during processing: {ex.Message}", ex);
                string shortError = ex.Message.Length > 100 ? ex.Message[..100] + "..." : ex.Message;
                _uiManager.UpdateProgress($"Auto Run ({definition.ReportName}): ERROR - {shortError}");
                overallSuccess = false;
            }
            finally
            {
                // Update the persisted status for this specific report for today in "AutoRunProcess:DailyRunStatus".
                SaveDailyReportStatus(definition.SuccessFlagJsonName, overallSuccess, processingDate);
            }
            return overallSuccess;
        }

        /// <summary>
        /// Gets or adds a JObject section to a parent JObject.
        /// </summary>
        private JObject GetOrAddSection(JObject parent, string sectionName, bool logCreation = true)
        {
            JObject? section = parent[sectionName] as JObject;
            if (section == null)
            {
                section = new JObject();
                parent[sectionName] = section;
                if (logCreation)
                {
                    Logger.LogDebug($"JSON section '{sectionName}' was missing under '{parent.Path}' and has been created.");
                }
            }
            return section;
        }

        /// <summary>
        /// Reads daily report run statuses from "AutoRunProcess:DailyRunStatus" in `appsettings.json`.
        /// </summary>
        private DailyReportRunStatus ReadDailyReportStatuses()
        {
            lock (s_jsonFileLock)
            {
                try
                {
                    if (!File.Exists(_appSettingsPath)) { Logger.LogWarning($"appsettings.json not found at '{_appSettingsPath}' for ReadDailyReportStatuses. Returning new status object."); return new DailyReportRunStatus { StatusDate = DateTime.MinValue.ToString("yyyy-MM-dd") }; }
                    string jsonContent = File.ReadAllText(_appSettingsPath);
                    if (string.IsNullOrWhiteSpace(jsonContent)) { Logger.LogWarning($"appsettings.json at '{_appSettingsPath}' is empty. Returning new status."); return new DailyReportRunStatus { StatusDate = DateTime.MinValue.ToString("yyyy-MM-dd") }; }

                    var jsonRoot = JObject.Parse(jsonContent);
                    // Correctly target "AutoRunProcess:DailyRunStatus"
                    JToken? statusToken = jsonRoot[JsonSectionAutoRunProcess]?[JsonKeyDailyRunStatus];

                    if (statusToken != null)
                    {
                        var status = statusToken.ToObject<DailyReportRunStatus>(JsonSerializer.CreateDefault(new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }));
                        if (status == null) { Logger.LogWarning("DailyRunStatus section parsed as null from appsettings.json. Returning default status."); return new DailyReportRunStatus { StatusDate = DateTime.MinValue.ToString("yyyy-MM-dd") }; }
                        status.StatusDate ??= DateTime.MinValue.ToString("yyyy-MM-dd");
                        return status;
                    }
                    Logger.LogWarning($"'{JsonSectionAutoRunProcess}:{JsonKeyDailyRunStatus}' section not found in appsettings.json. Returning default status object.");
                }
                catch (JsonException jsonEx) { Logger.LogError($"Error parsing DailyRunStatus from appsettings.json (JSON format issue): {jsonEx.Message}", jsonEx); }
                catch (IOException ioEx) { Logger.LogError($"IO Error reading DailyRunStatus from appsettings.json: {ioEx.Message}", ioEx); }
                catch (Exception ex) { Logger.LogError($"Error reading DailyReportStatus from appsettings.json: {ex.Message}", ex); }
                return new DailyReportRunStatus { StatusDate = DateTime.MinValue.ToString("yyyy-MM-dd") }; // Fallback
            }
        }

        /// <summary>
        /// Saves success status of a report for a date to "AutoRunProcess:DailyRunStatus" in `appsettings.json`.
        /// </summary>
        private void SaveDailyReportStatus(string successFlagJsonName, bool success, DateTime statusDate)
        {
            lock (s_jsonFileLock)
            {
                try
                {
                    string todayDateString = statusDate.ToString("yyyy-MM-dd");
                    string jsonContent = File.Exists(_appSettingsPath) ? File.ReadAllText(_appSettingsPath) : "{}";
                    var jsonRoot = JObject.Parse(string.IsNullOrWhiteSpace(jsonContent) ? "{}" : jsonContent);

                    JObject autoRunProcessSection = GetOrAddSection(jsonRoot, JsonSectionAutoRunProcess);
                    JObject dailyStatusJson = GetOrAddSection(autoRunProcessSection, JsonKeyDailyRunStatus, logCreation: false);

                    if (dailyStatusJson[JsonKeyStatusDate]?.ToString() != todayDateString || !dailyStatusJson.HasValues || dailyStatusJson[JsonKeyStatusDate] == null)
                    {
                        dailyStatusJson.RemoveAll();
                        dailyStatusJson[JsonKeyStatusDate] = todayDateString;
                        foreach (var def in _reportDefinitions.Where(d => d != null && !string.IsNullOrEmpty(d.SuccessFlagJsonName)))
                        {
                            dailyStatusJson[def.SuccessFlagJsonName] = false; // Initialize all to false for new date
                        }
                        Logger.LogInfo($"DailyRunStatus in '{JsonSectionAutoRunProcess}' section was for a different date or newly created/empty. Initialised for {todayDateString}.");
                    }
                    dailyStatusJson[successFlagJsonName] = success; // Set the specific report's status

                    File.WriteAllText(_appSettingsPath, jsonRoot.ToString(Formatting.Indented));
                    Logger.LogInfo($"Saved DailyRunStatus for '{successFlagJsonName}': Success={success}, Date={todayDateString} to '{JsonSectionAutoRunProcess}' section in appsettings.json.");
                }
                catch (JsonException jsonEx) { Logger.LogError($"Error parsing appsettings.json for SaveDailyReportStatus ('{successFlagJsonName}'): {jsonEx.Message}", jsonEx); }
                catch (IOException ioEx) { Logger.LogError($"IO Error saving DailyRunStatus to appsettings.json for '{successFlagJsonName}': {ioEx.Message}", ioEx); }
                catch (Exception ex) { Logger.LogError($"Error saving DailyRunStatus to appsettings.json for '{successFlagJsonName}': {ex.Message}", ex); }
            }
        }

        /// <summary>
        /// Resets all daily report statuses in "AutoRunProcess:DailyRunStatus" for a date.
        /// </summary>
        private void ResetDailyReportStatuses(DateTime forDate)
        {
            lock (s_jsonFileLock)
            {
                try
                {
                    string jsonContent = File.Exists(_appSettingsPath) ? File.ReadAllText(_appSettingsPath) : "{}";
                    var jsonRoot = JObject.Parse(string.IsNullOrWhiteSpace(jsonContent) ? "{}" : jsonContent);

                    JObject autoRunProcessSection = GetOrAddSection(jsonRoot, JsonSectionAutoRunProcess);
                    JObject newStatusJson = new JObject { [JsonKeyStatusDate] = forDate.ToString("yyyy-MM-dd") };
                    foreach (var definition in _reportDefinitions.Where(d => d != null && !string.IsNullOrEmpty(d.SuccessFlagJsonName)))
                    {
                        newStatusJson[definition.SuccessFlagJsonName] = false;
                    }
                    autoRunProcessSection[JsonKeyDailyRunStatus] = newStatusJson; // Replace existing DailyRunStatus

                    File.WriteAllText(_appSettingsPath, jsonRoot.ToString(Formatting.Indented));
                    Logger.LogInfo($"Reset DailyReportStatuses in '{JsonSectionAutoRunProcess}' section of appsettings.json for date {forDate:yyyy-MM-dd}.");
                }
                catch (JsonException jsonEx) { Logger.LogError($"Error parsing appsettings.json for ResetDailyReportStatuses: {jsonEx.Message}", jsonEx); }
                catch (IOException ioEx) { Logger.LogError($"IO Error resetting DailyRunStatuses in appsettings.json: {ioEx.Message}", ioEx); }
                catch (Exception ex) { Logger.LogError($"Error resetting DailyReportStatuses in appsettings.json: {ex.Message}", ex); }
            }
        }

        /// <summary>
        /// Reads the last global success date from "AutoRunProcess:LastRunDate" in `appsettings.json`.
        /// </summary>
        private DateTime ReadLastGlobalSuccessDate()
        {
            lock (s_jsonFileLock)
            {
                try
                {
                    if (!File.Exists(_appSettingsPath)) { Logger.LogWarning($"appsettings.json not found for ReadLastGlobalSuccessDate. Returning MinValue."); return DateTime.MinValue; }
                    string jsonContent = File.ReadAllText(_appSettingsPath);
                    if (string.IsNullOrWhiteSpace(jsonContent)) { Logger.LogWarning($"appsettings.json is empty. Returning MinValue for LastGlobalSuccessDate."); return DateTime.MinValue; }

                    var json = JObject.Parse(jsonContent);
                    string? dateString = json?[JsonSectionAutoRunProcess]?[JsonKeyLastRunDate]?.ToString();
                    if (DateTime.TryParseExact(dateString, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsedDate))
                    {
                        return parsedDate.Date;
                    }
                }
                catch (JsonException jsonEx) { Logger.LogError($"Error parsing LastGlobalSuccessDate ('{JsonSectionAutoRunProcess}:{JsonKeyLastRunDate}'): {jsonEx.Message}", jsonEx); }
                catch (IOException ioEx) { Logger.LogError($"IO Error reading LastGlobalSuccessDate from appsettings.json: {ioEx.Message}", ioEx); }
                catch (Exception ex) { Logger.LogError($"Error reading LastGlobalSuccessDate ('{JsonSectionAutoRunProcess}:{JsonKeyLastRunDate}'): {ex.Message}", ex); }
                return DateTime.MinValue; // Fallback
            }
        }

        /// <summary>
        /// Saves the last global success date to "AutoRunProcess:LastRunDate" in `appsettings.json`.
        /// </summary>
        private void SaveLastGlobalSuccessDate(DateTime dateToSave)
        {
            lock (s_jsonFileLock)
            {
                try
                {
                    string jsonContent = File.Exists(_appSettingsPath) ? File.ReadAllText(_appSettingsPath) : "{}";
                    var json = JObject.Parse(string.IsNullOrWhiteSpace(jsonContent) ? "{}" : jsonContent);
                    JObject autoRunProcessSection = GetOrAddSection(json, JsonSectionAutoRunProcess);
                    autoRunProcessSection[JsonKeyLastRunDate] = dateToSave.ToString("yyyy-MM-dd");
                    File.WriteAllText(_appSettingsPath, json.ToString(Formatting.Indented));
                    Logger.LogInfo($"Successfully saved LastGlobalSuccessDate (as '{JsonSectionAutoRunProcess}:{JsonKeyLastRunDate}'): {dateToSave:yyyy-MM-dd}.");
                }
                catch (JsonException jsonEx) { Logger.LogError($"Error parsing appsettings.json for SaveLastGlobalSuccessDate: {jsonEx.Message}", jsonEx); }
                catch (IOException ioEx) { Logger.LogError($"IO Error saving LastGlobalSuccessDate to appsettings.json: {ioEx.Message}", ioEx); }
                catch (Exception ex) { Logger.LogError($"Error saving LastGlobalSuccessDate (as '{JsonSectionAutoRunProcess}:{JsonKeyLastRunDate}'): {ex.Message}", ex); }
            }
        }

        /// <summary>
        /// Generates the full output path for a raw automated report file.
        /// Uses the refactored <see cref="RawReportExportBaseDir"/> property.
        /// </summary>
        private string GetAutomatedReportOutputPath(int reportTypeIndex, DateTime reportDate, string reportName)
        {
            string baseDir = RawReportExportBaseDir; // This now uses the updated property
            if (string.IsNullOrEmpty(baseDir) || baseDir.Contains("QCRA_AutoRun_Fallback"))
            {
                string errorMsg = $"GetAutomatedReportOutputPath: RawReportExportBaseDir for AutoRun is invalid or a fallback ('{baseDir}'). Cannot determine reliable report output location for '{reportName}'.";
                Logger.LogError(errorMsg);
                throw new InvalidOperationException(errorMsg);
            }

            string sanitizedReportName = string.Join("_", reportName.Split(Path.GetInvalidFileNameChars())).Replace(" ", "_");
            string fileName = $"{reportDate:yyyyMMdd}_{sanitizedReportName}_Raw_AutoType{reportTypeIndex}.xlsx";
            string fullPath;

            try
            {
                // FolderCreation uses its own logic for subfolder names.
                // If these also need to be configurable, FolderCreation would need IConfiguration or parameters.
                string? folderPath = FolderCreation.GetReportSpecificFolderPath(reportTypeIndex, baseDir, reportDate, _configuration);
                if (!string.IsNullOrEmpty(folderPath))
                {
                    Directory.CreateDirectory(folderPath); // Ensure folder exists
                    fullPath = Path.Combine(folderPath, fileName);
                }
                else // Fallback if FolderCreation fails
                {
                    Logger.LogWarning($"GetAutomatedReportOutputPath: Could not determine specific folder for Report '{reportName}'. Using fallback structure under base directory.");
                    // Use configured folder name for report type if available, else a generic one.
                    string reportTypeFolderName = _configuration.GetValue<string>($"OperationalParameters:ReportTypeFolderNames:{GetReportTypeKeyByIndex(reportTypeIndex)}", $"AutoRun_ReportType{reportTypeIndex}_Fallback")!;
                    string fallbackFolder = Path.Combine(baseDir, reportTypeFolderName);
                    Directory.CreateDirectory(fallbackFolder);
                    fullPath = Path.Combine(fallbackFolder, fileName);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Auto Run: Critical error constructing or ensuring output directory for Report '{reportName}': {ex.Message}", ex);
                string errorFallbackFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "QCRA_ErrorFallback_AutoRaw", sanitizedReportName);
                try { Directory.CreateDirectory(errorFallbackFolder); } catch { /* Best effort */ }
                fullPath = Path.Combine(errorFallbackFolder, fileName);
                Logger.LogError($"GetAutomatedReportOutputPath: Using CRITICAL ErrorFallback path for Report '{reportName}': {fullPath}");
            }
            Logger.LogDebug($"Automated report output path for '{reportName}': {fullPath}");
            return fullPath;
        }

        /// <summary>
        /// Gets a string key for the report type based on its index, used for looking up configured folder names.
        /// </summary>
        private string GetReportTypeKeyByIndex(int reportTypeIndex)
        {
            // This should match keys in "OperationalParameters:ReportTypeFolderNames"
            return reportTypeIndex switch
            {
                0 => "Daily",
                1 => "Daily5Day1k",
                2 => "Weekly",
                3 => "Monthly",
                4 => "Quarterly",
                5 => "Annual",
                6 => "Custom",
                _ => "Other"
            };
        }


        /// <summary>
        /// Constructs email subject and body for an automated report.
        /// </summary>
        private (string Subject, string Body) GetEmailSubjectAndBodyForAutoRun(AutoReportDefinition definition, DateTime reportStartDate, DateTime reportEndDate)
        {
            string greeting;
            if (IsDebug)
            {
                // Key for DebugEmails:EmailGreetings:DebugDefault
                greeting = _greetingManager.GetGreeting("DebugDefault", isForDebugSection: true);
            }
            else
            {
                // Key for EmailSettings:ProductionRecipients:EmailGreetings:<GreetingKeyFromDefinition>
                greeting = _greetingManager.GetGreeting(definition.GreetingKey);
            }

            if (!string.IsNullOrWhiteSpace(greeting) && !greeting.TrimEnd().EndsWith(","))
            {
                greeting = greeting.TrimEnd() + ",";
            }

            string dateRangeInfo = (reportStartDate.Date == reportEndDate.Date) ?
                                   $"for {reportEndDate:dd MMM yy}" :
                                   $"for period {reportStartDate:dd MMM yy} to {reportEndDate:dd MMM yy}";
            // Specific formatting for certain reports can be added here if needed.

            string subjectDateSuffix = (reportStartDate.Date == reportEndDate.Date) ?
                                       $"({reportEndDate:yyyy-MM-dd})" :
                                       $"({reportStartDate:yyyy-MM-dd} to {reportEndDate:yyyy-MM-dd})";

            string subject = $"AUTOMATED: {definition.SubjectPrefix} Report {subjectDateSuffix}";
            if (IsDebug) subject = $"DEBUG - {subject}";

            // Get default email signature from configuration.
            string emailSignature = _configuration.GetValue<string>("EmailSettings:DefaultEmailSignature", "Thank you,\nQCRA Automation Service")!;

            string body = $"{greeting}\n\nPlease find attached the automated {definition.SubjectPrefix.ToLowerInvariant()} report {dateRangeInfo}.\n\n{emailSignature}";

            Logger.LogDebug($"AutoRun Email for '{definition.ReportName}': Subject='{subject}' (GreetingKey: '{definition.GreetingKey}')");
            return (subject, body);
        }
        #endregion
    }
}