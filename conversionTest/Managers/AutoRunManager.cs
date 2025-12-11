// AutoRunManager.cs
// This version has been fully corrected for the date calculation bug in automated reports.
// It uses the IReportPathService for all path generation to ensure consistency.

#region Using Directives
// System related namespaces
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

// Project specific namespaces
using QuoteConversionReportAutomation.Configuration;
using QuoteConversionReportAutomation.Helpers;
using QuoteConversionReportAutomation.Interfaces; // For IAutoRunUIContext and IStatusManagerService
using QuoteConversionReportAutomation.Models;
using QuoteConversionReportAutomation.Models.Status; // For MessageType
using QuoteConversionReportAutomation.Services.Communication;
using QuoteConversionReportAutomation.Services.Excel;
using QuoteConversionReportAutomation.Services.Interfaces;
using QuoteConversionReportAutomation.Services.Logging;
#endregion

namespace QuoteConversionReportAutomation.Managers
{
    /// <summary>
    /// Manages the automated (scheduled) generation and processing of reports for the QCRA application.
    /// This class checks daily at a configured hour if any predefined automated reports are due for execution.
    /// It uses an <see cref="IAutoRunUIContext"/> for specific UI updates and the <see cref="IStatusManagerService"/>
    /// for centralised progress reporting.
    /// </summary>
    public class AutoRunManager
    {
        #region Fields and Properties

        #region Dependencies
        /// <summary>
        /// Provides access to the application's configuration settings from appsettings.json.
        /// </summary>
        private readonly IConfiguration _configuration;

        /// <summary>
        /// The service responsible for resolving all application and report-related file paths.
        /// </summary>
        private readonly IReportPathService _reportPathService;

        /// <summary>
        /// The utility responsible for constructing and sending emails via SMTP.
        /// </summary>
        private readonly EmailUtility _emailUtility;

        /// <summary>
        /// The manager for the external Crystal Reports wrapper process.
        /// </summary>
        private readonly ReportProcessManager _processManager;

        /// <summary>
        /// The service that handles inter-process communication with the wrapper via named pipes.
        /// </summary>
        private readonly NamedPipeCommunicator _pipeCommunicator;

        /// <summary>
        /// A lazily-initialised reference to the UI context, used to update UI elements like the AutoRun button and status.
        /// This prevents circular dependency issues during application startup.
        /// </summary>
        private readonly Lazy<IAutoRunUIContext> _lazyAutoRunUIContext;

        /// <summary>
        /// Gets the concrete instance of the UI context.
        /// </summary>
        private IAutoRunUIContext AutoRunUIContext => _lazyAutoRunUIContext.Value;

        /// <summary>
        /// The service responsible for all Excel file processing tasks.
        /// </summary>
        private readonly IExcelProcessingOrchestrator _excelProcessingOrchestrator;

        /// <summary>
        /// The manager responsible for determining email recipients for different scenarios.
        /// </summary>
        private readonly EmailRecipientManager _emailRecipientManager;

        /// <summary>
        /// The manager responsible for determining email greetings for different scenarios.
        /// </summary>
        private readonly GreetingManager _greetingManager;

        /// <summary>
        /// The centralised service for broadcasting application-wide status messages.
        /// </summary>
        private readonly IStatusManagerService _statusManager;
        #endregion

        #region File Paths
        /// <summary>
        /// The full path to the main appsettings.json file.
        /// </summary>
        private readonly string _appSettingsFilePath;

        /// <summary>
        /// The full path to the autoReportDefinitions.json file.
        /// </summary>
        private readonly string _reportDefinitionsFilePath;
        #endregion

        #region State Variables
        /// <summary>
        /// A lock object to ensure thread-safe read/write operations on configuration files.
        /// </summary>
        private static readonly object s_jsonFileLock = new object();

        /// <summary>
        /// A flag to prevent multiple auto-run tasks from executing concurrently.
        /// </summary>
        private bool _isAutoRunTaskExecuting = false;

        /// <summary>
        /// The last date on which all scheduled reports ran successfully. Loaded from appsettings.json.
        /// </summary>
        private DateTime _lastGlobalSuccessDate = DateTime.MinValue;

        /// <summary>
        /// The hour of the day (0-23) when the auto-run check should be performed.
        /// </summary>
        private int _autoRunCheckHour;

        /// <summary>
        /// The in-memory list of all configured automated report definitions.
        /// </summary>
        private List<AutoReportDefinition> _reportDefinitions;
        #endregion

        #region Build Configuration
        /// <summary>
        /// Gets a value indicating whether the application is running in a DEBUG build configuration.
        /// </summary>
        private static bool IsDebug =>
#if DEBUG
            true;
#else
            false;
#endif
        #endregion

        #endregion

        #region Constructor
        /// <summary>
        /// Initialises a new instance of the <see cref="AutoRunManager"/> class.
        /// </summary>
        /// <param name="configuration">The application configuration.</param>
        /// <param name="reportPathService">Service for resolving paths.</param>
        /// <param name="emailUtility">Utility for sending emails.</param>
        /// <param name="processManager">Manager for the external wrapper process.</param>
        /// <param name="pipeCommunicator">Communicator for IPC with the wrapper.</param>
        /// <param name="lazyAutoRunUIContext">A lazy-loaded reference to the UI context for specific UI updates.</param>
        /// <param name="excelProcessor">Service for processing Excel files.</param>
        /// <param name="emailRecipientManager">Manager for determining email recipients.</param>
        /// <param name="greetingManager">Manager for determining email greetings.</param>
        /// <param name="statusManager">The centralised service for status reporting.</param>
        public AutoRunManager(
            IConfiguration configuration,
            IReportPathService reportPathService,
            EmailUtility emailUtility,
            ReportProcessManager processManager,
            NamedPipeCommunicator pipeCommunicator,
            Lazy<IAutoRunUIContext> lazyAutoRunUIContext,
            IExcelProcessingOrchestrator excelProcessingOrchestrator,
            EmailRecipientManager emailRecipientManager,
            GreetingManager greetingManager,
            IStatusManagerService statusManager)
        {
            // Assign all injected dependencies.
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _reportPathService = reportPathService ?? throw new ArgumentNullException(nameof(reportPathService));
            _emailUtility = emailUtility ?? throw new ArgumentNullException(nameof(emailUtility));
            _processManager = processManager ?? throw new ArgumentNullException(nameof(processManager));
            _pipeCommunicator = pipeCommunicator ?? throw new ArgumentNullException(nameof(pipeCommunicator));
            _lazyAutoRunUIContext = lazyAutoRunUIContext ?? throw new ArgumentNullException(nameof(lazyAutoRunUIContext));
            _excelProcessingOrchestrator = excelProcessingOrchestrator ?? throw new ArgumentNullException(nameof(excelProcessingOrchestrator));
            _emailRecipientManager = emailRecipientManager ?? throw new ArgumentNullException(nameof(emailRecipientManager));
            _greetingManager = greetingManager ?? throw new ArgumentNullException(nameof(greetingManager));
            _statusManager = statusManager ?? throw new ArgumentNullException(nameof(statusManager));

            // Set up file paths from the injected path service and configuration.
            string appSettingsDirectory = _reportPathService.AppSettingsDirectory;
            _autoRunCheckHour = _configuration.GetValue<int>(AppConfigKeys.AutoRunProcess.CheckHour, 8);
            _appSettingsFilePath = Path.Combine(appSettingsDirectory, "appsettings.json");
            _reportDefinitionsFilePath = _reportPathService.GetReportDefinitionsFilePath() ?? Path.Combine(appSettingsDirectory, "autoReportDefinitions.json");

            // Load initial state from configuration files.
            _reportDefinitions = LoadReportDefinitions(_reportDefinitionsFilePath);
            _lastGlobalSuccessDate = ReadLastGlobalSuccessDate();
            Logger.LogInfo($"AutoRunManager initialised. Check Hour: {_autoRunCheckHour}. Last Global Success: {_lastGlobalSuccessDate:yyyy-MM-dd}");
        }
        #endregion

        #region Report Definition Management
        /// <summary>
        /// Loads automated report definitions from the specified JSON file. This method is static
        /// so it can be used by UI forms (e.g., ManageAutoReportDefinitionsForm) without needing an instance of the manager.
        /// </summary>
        /// <param name="definitionsFilePath">The full path to the report definitions JSON file.</param>
        /// <returns>A list of <see cref="AutoReportDefinition"/> objects.</returns>
        public static List<AutoReportDefinition> LoadReportDefinitions(string definitionsFilePath)
        {
            ArgumentException.ThrowIfNullOrEmpty(definitionsFilePath, nameof(definitionsFilePath));
            List<AutoReportDefinition>? definitions = null;
            if (File.Exists(definitionsFilePath))
            {
                try
                {
                    string jsonContent;
                    lock (s_jsonFileLock) { jsonContent = File.ReadAllText(definitionsFilePath); }
                    if (string.IsNullOrWhiteSpace(jsonContent)) return new List<AutoReportDefinition>();
                    definitions = JsonConvert.DeserializeObject<List<AutoReportDefinition>>(jsonContent, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore, ContractResolver = new DefaultContractResolver() });
                }
                catch (Exception ex) { Logger.LogError($"Error loading/parsing report definitions from '{definitionsFilePath}': {ex.Message}", ex); }
            }
            else { Logger.LogWarning($"Report definitions file not found: '{definitionsFilePath}'."); }

            definitions ??= new List<AutoReportDefinition>();

            // Ensure every definition has a unique ID, generating one for any legacy definitions that might be missing it.
            bool idsGenerated = false;
            foreach (var def in definitions)
            {
                if (string.IsNullOrWhiteSpace(def.ReportId))
                {
                    def.ReportId = Guid.NewGuid().ToString();
                    idsGenerated = true;
                }
            }
            if (idsGenerated) { Logger.LogInfo("New ReportIds were generated for some definitions. Save via UI to persist these new IDs if necessary."); }

            return definitions;
        }

        /// <summary>
        /// Saves a list of automated report definitions to the specified JSON file. This method is static
        /// for use by UI management forms.
        /// </summary>
        /// <param name="definitionsFilePath">The full path to the report definitions JSON file.</param>
        /// <param name="definitionsToSave">The list of definitions to save.</param>
        public static void SaveReportDefinitions(string definitionsFilePath, List<AutoReportDefinition> definitionsToSave)
        {
            ArgumentNullException.ThrowIfNull(definitionsToSave, nameof(definitionsToSave));
            ArgumentException.ThrowIfNullOrEmpty(definitionsFilePath, nameof(definitionsFilePath));
            lock (s_jsonFileLock)
            {
                try
                {
                    string jsonString = JsonConvert.SerializeObject(definitionsToSave, Formatting.Indented, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore, ContractResolver = new DefaultContractResolver() });
                    string? directoryPath = Path.GetDirectoryName(definitionsFilePath);
                    if (!string.IsNullOrEmpty(directoryPath) && !Directory.Exists(directoryPath)) Directory.CreateDirectory(directoryPath);
                    File.WriteAllText(definitionsFilePath, jsonString);
                }
                catch (Exception ex) { Logger.LogError($"Error saving report definitions to '{definitionsFilePath}': {ex.Message}", ex); throw; }
            }
        }

        /// <summary>
        /// Reloads the automated report definitions from the file system into the manager's active list.
        /// </summary>
        public void ReloadReportDefinitions()
        {
            _reportDefinitions = LoadReportDefinitions(_reportDefinitionsFilePath);
            Logger.LogInfo($"AutoRunManager: Report definitions reloaded. Count: {_reportDefinitions.Count}");
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Performs the main daily check to see if any automated reports are due to be run.
        /// </summary>
        /// <param name="isTimerCurrentlyEnabled">A flag indicating if the auto-run feature is enabled in the UI.</param>
        /// <param name="configuredHour">The hour (0-23) at which the check should run.</param>
        /// <returns>An <see cref="AutoRunActionResult"/> indicating the outcome of the check.</returns>
        public async Task<AutoRunActionResult> PerformDailyCheckAsync(bool isTimerCurrentlyEnabled, int configuredHour)
        {
            ReloadReportDefinitions();

            // Pre-flight checks: exit early if auto-run is disabled, no reports are defined, or a task is already running.
            if (!isTimerCurrentlyEnabled || !_reportDefinitions.Any(d => d.IsEnabled) || _isAutoRunTaskExecuting)
            {
                return AutoRunActionResult.NoActionNeeded;
            }

            DateTime now = DateTime.Now;
            _autoRunCheckHour = configuredHour;
            DailyReportRunStatus currentDayStatuses = ReadDailyReportStatuses();

            // If the date has changed since the last status was saved, reset the success flags for the new day.
            if (currentDayStatuses.StatusDate != now.ToString("yyyy-MM-dd"))
            {
                ResetDailyReportStatuses(now.Date);
                currentDayStatuses = ReadDailyReportStatuses();
                AutoRunUIContext.UpdateAutoRunButtonAndStatus(true, false, $"Auto Run: Enabled (Next check ~{_autoRunCheckHour}:00)");
            }

            // Only proceed if the current hour matches the configured check hour.
            if (now.Hour != _autoRunCheckHour)
            {
                return AutoRunActionResult.NoActionNeeded;
            }

            // Check if all reports that are scheduled for today have already been successfully run.
            bool allDueReportsAlreadySucceeded = currentDayStatuses.AllCurrentlyEnabledAndDueReportsSucceeded(_reportDefinitions, now.DayOfWeek);
            int totalEnabledAndDueToday = _reportDefinitions.Count(def => def.IsEnabled && (!def.RunOnDayOfWeek.HasValue || def.RunOnDayOfWeek.Value == now.DayOfWeek));

            if (allDueReportsAlreadySucceeded && totalEnabledAndDueToday > 0)
            {
                string doneMsg = $"Auto Run: Done for {now:dd/MM}";
                AutoRunUIContext.ReportAutoRunStatusRight(doneMsg);
                AutoRunUIContext.UpdateAutoRunButtonAndStatus(true, true, doneMsg);
                return AutoRunActionResult.NoActionNeeded;
            }

            // Set guard flag and update UI to indicate processing has started.
            _isAutoRunTaskExecuting = true;
            AutoRunUIContext.SetControlsForAutoRunInProgress(true);
            _statusManager.Post($"Auto Run: Starting checks for {now:dd-MM-yyyy}...", MessageType.InProgress);

            try
            {
                // Iterate through each report definition to see if it needs to be run.
                foreach (var definition in _reportDefinitions)
                {
                    // Skip if disabled, not due today, or already successfully run today.
                    if (!definition.IsEnabled || (definition.RunOnDayOfWeek.HasValue && now.DayOfWeek != definition.RunOnDayOfWeek.Value)) continue;

                    currentDayStatuses = ReadDailyReportStatuses(); // Refresh status before checking this specific report.
                    if (currentDayStatuses.GetReportSuccessStatus(definition.SuccessFlagJsonName)) continue;

                    _statusManager.Post($"Auto Run: Processing {definition.ReportName}...", MessageType.InProgress);

                    // The original code called a generic helper that did not use the definition's offset/duration.
                    // This new call uses a dedicated method that correctly calculates the date range based on
                    // the specific properties in the AutoReportDefinition.
                    var (reportStartDate, reportEndDate) = CalculateAutoRunDateRange(definition, now);

                    // Execute the report workflow.
                    await RunConfiguredAutomatedReportAsync(definition, reportEndDate, reportStartDate, now.Date);
                }

                // After attempting all due reports, update the final status.
                currentDayStatuses = ReadDailyReportStatuses();
                bool allNowSucceeded = currentDayStatuses.AllCurrentlyEnabledAndDueReportsSucceeded(_reportDefinitions, now.DayOfWeek);
                int succeededCount = _reportDefinitions.Count(def => def.IsEnabled && (!def.RunOnDayOfWeek.HasValue || def.RunOnDayOfWeek.Value == now.DayOfWeek) && currentDayStatuses.GetReportSuccessStatus(def.SuccessFlagJsonName));

                string finalMsg = allNowSucceeded ? $"Auto Run: All due DONE ({succeededCount}/{totalEnabledAndDueToday})" : $"Auto Run: Partial ({succeededCount}/{totalEnabledAndDueToday} succeeded)";
                AutoRunUIContext.ReportAutoRunStatusRight(finalMsg);
                AutoRunUIContext.UpdateAutoRunButtonAndStatus(isTimerCurrentlyEnabled, allNowSucceeded, finalMsg);

                if (allNowSucceeded && totalEnabledAndDueToday > 0)
                {
                    SaveLastGlobalSuccessDate(now.Date);
                }

                return AutoRunActionResult.ActionAttempted;
            }
            catch (Exception ex)
            {
                Logger.LogCritical($"Auto Run: CRITICAL Unhandled exception in PerformDailyCheckAsync: {ex.Message}", ex);
                _statusManager.Post("Auto Run: CRITICAL ERROR! Check Logs.", MessageType.Error);
                AutoRunUIContext.ReportAutoRunStatusRight("Auto Run: CRITICAL ERROR");
                return AutoRunActionResult.CriticalError;
            }
            finally
            {
                // Reset guard flag and re-enable UI controls.
                _isAutoRunTaskExecuting = false;
                AutoRunUIContext.SetControlsForAutoRunInProgress(false);
            }
        }

        /// <summary>
        /// Saves the user-configured auto-run hour to the appsettings.json file.
        /// </summary>
        /// <param name="newHour">The new hour for the check (0-23).</param>
        /// <returns>True if the setting was saved successfully; otherwise, false.</returns>
        public async Task<bool> SetAutoRunHourAsync(int newHour)
        {
            if (newHour < 0 || newHour > 23) return false;

            return await Task.Run(() =>
            {
                lock (s_jsonFileLock)
                {
                    try
                    {
                        if (!File.Exists(_appSettingsFilePath)) return false;
                        string jsonContent = File.ReadAllText(_appSettingsFilePath);
                        var jsonRoot = JObject.Parse(string.IsNullOrWhiteSpace(jsonContent) ? "{}" : jsonContent);
                        JObject autoRunSection = GetOrAddSection(jsonRoot, AppConfigKeys.AutoRunProcess.Base);
                        autoRunSection[AppConfigKeys.AutoRunProcess.CheckHour.Split(':').Last()] = newHour;
                        File.WriteAllText(_appSettingsFilePath, jsonRoot.ToString(Formatting.Indented));
                        _autoRunCheckHour = newHour;
                        return true;
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError($"Error saving auto-run hour to appsettings.json: {ex.Message}", ex);
                        return false;
                    }
                }
            });
        }
        #endregion

        #region Private Helper Methods
        /// <summary>
        /// Calculates the correct start and end date for an automated report based on its specific definition.
        /// This method correctly uses the offset and duration properties from the AutoReportDefinition.
        /// </summary>
        /// <param name="definition">The definition of the automated report.</param>
        /// <param name="referenceDate">The current date and time to calculate from (usually DateTime.Now).</param>
        /// <returns>A tuple containing the calculated start and end dates for the report period.</returns>
        private (DateTime StartDate, DateTime EndDate) CalculateAutoRunDateRange(AutoReportDefinition definition, DateTime referenceDate)
        {
            // 1. Determine the End Date by applying the offset.
            //    The offset is in *workdays*. E.g., an offset of 1 means the previous workday.
            int endDateOffset = definition.ReportEndDateOffsetDays ?? 1; // Default to 1 (previous workday) if not specified.
            DateTime endDate = ReportHelper.GetNthPreviousWorkday(referenceDate, endDateOffset);

            // 2. Determine the Start Date from the End Date by applying the duration.
            //    The duration is also in *workdays*. A duration of 1 means start and end dates are the same.
            int durationDays = definition.ReportDurationDays ?? 1; // Default to a 1-day duration if not specified.
            DateTime startDate = ReportHelper.GetNthPreviousWorkday(endDate, durationDays - 1);

            Logger.LogDebug($"Calculated auto-run date range for '{definition.ReportName}'. EndDateOffset: {endDateOffset}, Duration: {durationDays}. Result: {startDate:d} to {endDate:d}");
            return (startDate, endDate);
        }

        /// <summary>
        /// The main execution logic for a single automated report.
        /// This method orchestrates the creation, processing, and emailing of a report defined by an <see cref="AutoReportDefinition"/>.
        /// </summary>
        /// <param name="definition">The definition of the automated report to run.</param>
        /// <param name="reportEndDate">The calculated end date for the report's data range.</param>
        /// <param name="reportStartDate">The calculated start date for the report's data range.</param>
        /// <param name="processingDate">The current date, used for recording the status of the run.</param>
        /// <returns>A boolean indicating whether the entire process succeeded.</returns>
        private async Task<bool> RunConfiguredAutomatedReportAsync(AutoReportDefinition definition, DateTime reportEndDate, DateTime? reportStartDate, DateTime processingDate)
        {
            DateTime effectiveReportStartDate = reportStartDate ?? reportEndDate;
            ReportType currentReportType = ReportTypeHelper.FromInt(definition.ReportTypeIndex);
            Logger.LogInfo($"Auto Run: Executing report: '{definition.ReportName}' (Type: {currentReportType}) for period {effectiveReportStartDate:yyyy-MM-dd} to {reportEndDate:yyyy-MM-dd}.");

            bool overallSuccess = false;
            using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(_configuration.GetValue<int>(AppConfigKeys.OperationalParameters.ProcessTimeoutMinutes, 15)));
            var token = cts.Token;

            // This adapter is used to report progress to the central status manager.
            var progressAdapter = new Progress<string>(status => _statusManager.Post(status, MessageType.InProgress));

            try
            {
                // Step 1: Ensure the external report generation service is running.
                if (!await _processManager.EnsureWrapperIsRunningAsync(progressAdapter, token))
                {
                    throw new InvalidOperationException("Failed to start or connect to report service.");
                }

                // Step 2: Get all required paths from the centralised path service.
                string crystalRptPath = _reportPathService.CrystalReportRptFilePath ?? throw new InvalidOperationException("Crystal Report path not configured.");

                // *** Use the injected IReportPathService to get the output path ***
                // This ensures the path resolution logic is consistent with the rest of the application.
                string? outputPath = _reportPathService.GetRawReportOutputPath(currentReportType, reportEndDate, definition.ReportName);

                if (string.IsNullOrEmpty(outputPath))
                {
                    throw new InvalidOperationException($"Failed to generate a valid output path for the raw report '{definition.ReportName}'.");
                }

                // Step 3: Send the request to the wrapper to generate the raw report.
                var request = new ReportRequest { CrystalReportLocation = crystalRptPath, ReportOutputLocation = outputPath, ReportDateFrom = effectiveReportStartDate, ReportDateTo = reportEndDate };
                ReportResponse? response = await _pipeCommunicator.SendRequestReceiveResponseAsync(request, progressAdapter, token);

                if (!(response?.Success == true && !string.IsNullOrEmpty(response.OutputPath) && File.Exists(response.OutputPath)))
                {
                    throw new Exception($"Raw report generation failed: {response?.ErrorMessage ?? "Unknown wrapper error"}");
                }
                string generatedRawPath = response.OutputPath;

                // Step 4: Process the raw report using the Excel service.
                string templatePath = _reportPathService.GetExcelTemplatePath(currentReportType) ?? throw new InvalidOperationException("Excel template path not configured.");
                string baseSaveLocation = _reportPathService.FinalReportOutputBaseDirectory ?? throw new InvalidOperationException("Final report output directory not configured.");

                string? finalAnalysisPath = await _excelProcessingOrchestrator.ProcessExcelReportAsync(
                    selectedFinYear: null, currentReportType, generatedRawPath, "RawDataSourceSheet",
                    baseSaveLocation, templatePath, "TemplateDataCopySheet", 1, 1, reportEndDate, manualParams: null,
    autoRunDef: definition, token);

                if (string.IsNullOrEmpty(finalAnalysisPath))
                {
                    throw new Exception("Excel processing failed to produce a final file.");
                }

                // Step 5: Send the final report via email.
                var (mailTo, mailCc) = _emailRecipientManager.GetRecipients(definition.ReportTypeIndex, false, IsDebug, true, definition);
                var (subject, body) = GetEmailSubjectAndBodyForAutoRun(definition, effectiveReportStartDate, reportEndDate);
                EmailSendResult emailResult = await _emailUtility.SendEmailAsync(mailTo, mailCc, subject, body, finalAnalysisPath, token);

                if (!emailResult.Success)
                {
                    throw new Exception($"Email sending failed: {emailResult.ErrorMessage}");
                }

                Logger.LogInfo($"Auto Run ({definition.ReportName}): Email sent successfully.");
                overallSuccess = true;
            }
            catch (OperationCanceledException)
            {
                Logger.LogWarning($"Auto Run ({definition.ReportName}): Operation cancelled.");
            }
            catch (Exception ex)
            {
                Logger.LogError($"Auto Run ({definition.ReportName}): Error: {ex.Message}", ex);
                _statusManager.Post($"ERROR ({definition.ReportName}): {ex.Message.Substring(0, Math.Min(ex.Message.Length, 100))}", MessageType.Error);
            }
            finally
            {
                // Always save the success/failure status of this specific report for today.
                SaveDailyReportStatus(definition.SuccessFlagJsonName, overallSuccess, processingDate);
            }

            return overallSuccess;
        }

        /// <summary>
        /// Gets or adds a JObject section from a parent JObject, creating it if it does not exist.
        /// </summary>
        /// <param name="parent">The parent JObject to search within.</param>
        /// <param name="fullSectionKeyPath">The colon-separated path to the section (e.g., "AutoRunProcess:DailyRunStatus").</param>
        /// <param name="logCreation">Whether to log a debug message if a new section is created.</param>
        /// <returns>The found or newly created JObject section.</returns>
        private JObject GetOrAddSection(JObject parent, string fullSectionKeyPath, bool logCreation = true)
        {
            string[] segments = fullSectionKeyPath.Split(':');
            JToken? currentToken = parent;
            JObject? section = null;

            foreach (string segment in segments)
            {
                if (currentToken is JObject currentObject)
                {
                    if (!currentObject.TryGetValue(segment, out JToken? nextToken) || !(nextToken is JObject))
                    {
                        var newObjSection = new JObject();
                        currentObject[segment] = newObjSection;
                        currentToken = newObjSection;
                        if (logCreation)
                        {
                            Logger.LogDebug($"JSON segment '{segment}' created under '{currentObject.Path}'.");
                        }
                    }
                    else
                    {
                        currentToken = nextToken;
                    }
                    section = currentToken as JObject;
                }
                else
                {
                    throw new InvalidOperationException($"Cannot create/access section '{segment}' as parent is not a JObject at path '{currentToken?.Path}'.");
                }
            }
            return section ?? throw new InvalidOperationException($"Section '{fullSectionKeyPath}' could not be resolved to a JObject.");
        }

        /// <summary>
        /// Reads the daily run status object from the appsettings.json file.
        /// </summary>
        /// <returns>A <see cref="DailyReportRunStatus"/> object.</returns>
        private DailyReportRunStatus ReadDailyReportStatuses()
        {
            lock (s_jsonFileLock)
            {
                try
                {
                    if (!File.Exists(_appSettingsFilePath))
                    {
                        return new DailyReportRunStatus { StatusDate = DateTime.MinValue.ToString("yyyy-MM-dd") };
                    }
                    string jsonContent = File.ReadAllText(_appSettingsFilePath);
                    if (string.IsNullOrWhiteSpace(jsonContent))
                    {
                        return new DailyReportRunStatus { StatusDate = DateTime.MinValue.ToString("yyyy-MM-dd") };
                    }

                    var jsonRoot = JObject.Parse(jsonContent);
                    string jsonPath = AppConfigKeys.AutoRunProcess.DailyRunStatus.Replace(":", ".");
                    JToken? statusToken = jsonRoot.SelectToken(jsonPath);

                    if (statusToken != null)
                    {
                        var status = statusToken.ToObject<DailyReportRunStatus>(JsonSerializer.CreateDefault(new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }));
                        if (status != null)
                        {
                            status.StatusDate ??= DateTime.MinValue.ToString("yyyy-MM-dd");
                            return status;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError($"Error reading DailyReportStatus from appsettings.json: {ex.Message}", ex);
                }
                return new DailyReportRunStatus { StatusDate = DateTime.MinValue.ToString("yyyy-MM-dd") };
            }
        }

        /// <summary>
        /// Saves the success status for a specific report to the appsettings.json file.
        /// </summary>
        /// <param name="successFlagJsonName">The JSON key for the report's success flag.</param>
        /// <param name="success">The success status to save.</param>
        /// <param name="statusDate">The date for which this status applies.</param>
        private void SaveDailyReportStatus(string successFlagJsonName, bool success, DateTime statusDate)
        {
            lock (s_jsonFileLock)
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(successFlagJsonName)) { return; }

                    string todayDateString = statusDate.ToString("yyyy-MM-dd");
                    string jsonContent = File.Exists(_appSettingsFilePath) ? File.ReadAllText(_appSettingsFilePath) : "{}";
                    var jsonRoot = JObject.Parse(string.IsNullOrWhiteSpace(jsonContent) ? "{}" : jsonContent);

                    JObject autoRunProcessSection = GetOrAddSection(jsonRoot, AppConfigKeys.AutoRunProcess.Base);
                    string dailyRunStatusSimpleKey = AppConfigKeys.AutoRunProcess.DailyRunStatus.Split(':').Last();
                    JObject dailyStatusJson = GetOrAddSection(autoRunProcessSection, dailyRunStatusSimpleKey, logCreation: false);
                    string statusDateSimpleKey = AppConfigKeys.AutoRunProcess.DailyRunStatus_StatusDate.Split(':').Last();

                    if (dailyStatusJson[statusDateSimpleKey]?.ToString() != todayDateString || !dailyStatusJson.Properties().Any(p => p.Name != statusDateSimpleKey))
                    {
                        dailyStatusJson.RemoveAll();
                        dailyStatusJson[statusDateSimpleKey] = todayDateString;
                        foreach (var def in _reportDefinitions.Where(d => !string.IsNullOrEmpty(d.SuccessFlagJsonName)))
                        {
                            dailyStatusJson[def.SuccessFlagJsonName] = false;
                        }
                    }
                    dailyStatusJson[successFlagJsonName] = success;

                    File.WriteAllText(_appSettingsFilePath, jsonRoot.ToString(Formatting.Indented));
                }
                catch (Exception ex)
                {
                    Logger.LogError($"Error saving DailyRunStatus for '{successFlagJsonName}': {ex.Message}", ex);
                }
            }
        }

        /// <summary>
        /// Resets all daily report success flags for a new day in the appsettings.json file.
        /// </summary>
        /// <param name="forDate">The new date to set.</param>
        private void ResetDailyReportStatuses(DateTime forDate)
        {
            lock (s_jsonFileLock)
            {
                try
                {
                    string jsonContent = File.Exists(_appSettingsFilePath) ? File.ReadAllText(_appSettingsFilePath) : "{}";
                    var jsonRoot = JObject.Parse(string.IsNullOrWhiteSpace(jsonContent) ? "{}" : jsonContent);

                    JObject autoRunProcessSection = GetOrAddSection(jsonRoot, AppConfigKeys.AutoRunProcess.Base);
                    string dailyRunStatusSimpleKey = AppConfigKeys.AutoRunProcess.DailyRunStatus.Split(':').Last();
                    string statusDateSimpleKey = AppConfigKeys.AutoRunProcess.DailyRunStatus_StatusDate.Split(':').Last();

                    JObject newStatusJson = new JObject { [statusDateSimpleKey] = forDate.ToString("yyyy-MM-dd") };
                    foreach (var definition in _reportDefinitions.Where(d => !string.IsNullOrEmpty(d.SuccessFlagJsonName)))
                    {
                        newStatusJson[definition.SuccessFlagJsonName] = false;
                    }
                    autoRunProcessSection[dailyRunStatusSimpleKey] = newStatusJson;

                    File.WriteAllText(_appSettingsFilePath, jsonRoot.ToString(Formatting.Indented));
                }
                catch (Exception ex)
                {
                    Logger.LogError($"Error resetting DailyReportStatuses: {ex.Message}", ex);
                }
            }
        }

        /// <summary>
        /// Reads the last date on which all reports successfully ran from appsettings.json.
        /// </summary>
        /// <returns>The last success date, or DateTime.MinValue if not found.</returns>
        private DateTime ReadLastGlobalSuccessDate()
        {
            lock (s_jsonFileLock)
            {
                try
                {
                    if (!File.Exists(_appSettingsFilePath)) return DateTime.MinValue;
                    string jsonContent = File.ReadAllText(_appSettingsFilePath);
                    if (string.IsNullOrWhiteSpace(jsonContent)) return DateTime.MinValue;

                    var json = JObject.Parse(jsonContent);
                    string jsonPath = AppConfigKeys.AutoRunProcess.LastRunDate.Replace(":", ".");
                    string? dateString = json.SelectToken(jsonPath)?.ToString();

                    if (DateTime.TryParseExact(dateString, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsedDate))
                    {
                        return parsedDate.Date;
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError($"Error reading LastGlobalSuccessDate from '{AppConfigKeys.AutoRunProcess.LastRunDate}': {ex.Message}", ex);
                }
                return DateTime.MinValue;
            }
        }

        /// <summary>
        /// Saves the given date as the last global success date in appsettings.json.
        /// </summary>
        /// <param name="dateToSave">The date to save.</param>
        private void SaveLastGlobalSuccessDate(DateTime dateToSave)
        {
            lock (s_jsonFileLock)
            {
                try
                {
                    string jsonContent = File.Exists(_appSettingsFilePath) ? File.ReadAllText(_appSettingsFilePath) : "{}";
                    var json = JObject.Parse(string.IsNullOrWhiteSpace(jsonContent) ? "{}" : jsonContent);

                    JObject autoRunProcessSection = GetOrAddSection(json, AppConfigKeys.AutoRunProcess.Base);
                    string lastRunDateSimpleKey = AppConfigKeys.AutoRunProcess.LastRunDate.Split(':').Last();
                    autoRunProcessSection[lastRunDateSimpleKey] = dateToSave.ToString("yyyy-MM-dd");

                    File.WriteAllText(_appSettingsFilePath, json.ToString(Formatting.Indented));
                }
                catch (Exception ex)
                {
                    Logger.LogError($"Error saving LastGlobalSuccessDate as '{AppConfigKeys.AutoRunProcess.LastRunDate}': {ex.Message}", ex);
                }
            }
        }

        /// <summary>
        /// Constructs the email subject and body for an automated report run.
        /// </summary>
        /// <param name="definition">The definition of the report being run.</param>
        /// <param name="reportStartDate">The start date of the report period.</param>
        /// <param name="reportEndDate">The end date of the report period.</param>
        /// <returns>A tuple containing the generated subject and body strings.</returns>
        private (string Subject, string Body) GetEmailSubjectAndBodyForAutoRun(AutoReportDefinition definition, DateTime reportStartDate, DateTime reportEndDate)
        {
            string greeting;
            if (IsDebug)
            {
                greeting = _greetingManager.GetGreeting(nameof(UserGreetingSettings.DebugDefault), isForDebugSection: true);
            }
            else
            {
                greeting = _greetingManager.GetGreeting(definition.GreetingKey);
            }

            if (!string.IsNullOrWhiteSpace(greeting) && !greeting.TrimEnd().EndsWith(","))
            {
                greeting = greeting.TrimEnd() + ",";
            }

            string rangeInfo = (reportStartDate.Date == reportEndDate.Date) ? $"for {reportEndDate:dd MMM yy}" : $"for period {reportStartDate:dd MMM yy} to {reportEndDate:dd MMM yy}";
            string subjectDateSuffix = (reportStartDate.Date == reportEndDate.Date) ? $"({reportEndDate:yyyy-MM-dd})" : $"({reportStartDate:yyyy-MM-dd} to {reportEndDate:yyyy-MM-dd})";

            string subject = $"AUTOMATED: {definition.SubjectPrefix} Report {subjectDateSuffix}";
            if (IsDebug)
            {
                subject = $"DEBUG - {subject}";
            }

            string emailSignature = _configuration.GetValue<string>(AppConfigKeys.EmailSettings.DefaultEmailSignature, "Thank you,\nAutomation Service")!;
            string body = $"{greeting}\n\nPlease find attached the automated {definition.SubjectPrefix.ToLowerInvariant()} report {rangeInfo}.\n\n{emailSignature}";

            return (subject, body);
        }

        /// <summary>
        /// Reads the current report definitions and ensures a success flag key exists
        /// for each one in the appsettings.json DailyRunStatus section.
        /// </summary>
        public void SynchronizeSuccessFlags()
        {
            lock (s_jsonFileLock)
            {
                try
                {
                    Logger.LogInfo("Synchronizing success flags in appsettings.json with current definitions...");
                    string jsonContent = File.Exists(_appSettingsFilePath) ? File.ReadAllText(_appSettingsFilePath) : "{}";
                    var jsonRoot = JObject.Parse(string.IsNullOrWhiteSpace(jsonContent) ? "{}" : jsonContent);

                    // Navigate to or create the 'DailyRunStatus' section
                    JObject autoRunProcessSection = GetOrAddSection(jsonRoot, AppConfigKeys.AutoRunProcess.Base);
                    string dailyRunStatusSimpleKey = AppConfigKeys.AutoRunProcess.DailyRunStatus.Split(':').Last();
                    JObject dailyStatusJson = GetOrAddSection(autoRunProcessSection, dailyRunStatusSimpleKey, logCreation: false);

                    int flagsAdded = 0;
                    // Loop through all current definitions
                    foreach (var definition in _reportDefinitions)
                    {
                        if (!string.IsNullOrEmpty(definition.SuccessFlagJsonName))
                        {
                            // Check if the key already exists
                            if (!dailyStatusJson.ContainsKey(definition.SuccessFlagJsonName))
                            {
                                // If not, add it with a default value of false
                                dailyStatusJson[definition.SuccessFlagJsonName] = false;
                                flagsAdded++;
                                Logger.LogDebug($"Added missing success flag to appsettings: '{definition.SuccessFlagJsonName}'");
                            }
                        }
                    }

                    if (flagsAdded > 0)
                    {
                        File.WriteAllText(_appSettingsFilePath, jsonRoot.ToString(Formatting.Indented));
                        Logger.LogInfo($"Synchronization complete. Added {flagsAdded} new success flag(s) to appsettings.json.");
                    }
                    else
                    {
                        Logger.LogInfo("Synchronization complete. No new success flags needed.");
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError($"Error during success flag synchronization: {ex.Message}", ex);
                }
            }
        }
        #endregion
    }
}