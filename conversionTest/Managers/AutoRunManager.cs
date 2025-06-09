// AutoRunManager.cs
// Manages the automated execution of predefined reports.
// This version is corrected to use the IReportPathService for all path generation,
// resolving the issue of blank reports being generated in automated runs.

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
    /// It uses an <see cref="IAutoRunUIContext"/> for specific UI updates and the <see cref="IStatusManagerService"/>
    /// for centralised progress reporting.
    /// </summary>
    public class AutoRunManager
    {
        #region Fields and Properties

        #region Dependencies
        private readonly IConfiguration _configuration;
        private readonly IReportPathService _reportPathService;
        private readonly EmailUtility _emailUtility;
        private readonly ReportProcessManager _processManager;
        private readonly NamedPipeCommunicator _pipeCommunicator;
        private readonly Lazy<IAutoRunUIContext> _lazyAutoRunUIContext;
        private IAutoRunUIContext AutoRunUIContext => _lazyAutoRunUIContext.Value;
        private readonly ExcelCopyData _excelProcessor;
        private readonly EmailRecipientManager _emailRecipientManager;
        private readonly GreetingManager _greetingManager;
        private readonly IStatusManagerService _statusManager;
        #endregion

        #region File Paths
        private readonly string _appSettingsFilePath;
        private readonly string _reportDefinitionsFilePath;
        #endregion

        #region State Variables
        private static readonly object s_jsonFileLock = new object();
        private bool _isAutoRunTaskExecuting = false;
        private DateTime _lastGlobalSuccessDate = DateTime.MinValue;
        private int _autoRunCheckHour;
        private List<AutoReportDefinition> _reportDefinitions;
        #endregion

        #region Build Configuration
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
            ExcelCopyData excelProcessor,
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
            _excelProcessor = excelProcessor ?? throw new ArgumentNullException(nameof(excelProcessor));
            _emailRecipientManager = emailRecipientManager ?? throw new ArgumentNullException(nameof(emailRecipientManager));
            _greetingManager = greetingManager ?? throw new ArgumentNullException(nameof(greetingManager));
            _statusManager = statusManager ?? throw new ArgumentNullException(nameof(statusManager));

            // Set up file paths from configuration.
            string appSettingsDirectory = _reportPathService.AppSettingsDirectory;
            _autoRunCheckHour = _configuration.GetValue<int>(AppConfigKeys.AutoRunProcess.CheckHour, 8);
            _appSettingsFilePath = Path.Combine(appSettingsDirectory, "appsettings.json");
            _reportDefinitionsFilePath = _reportPathService.GetReportDefinitionsFilePath() ?? Path.Combine(appSettingsDirectory, "autoReportDefinitions.json");

            // Load initial state.
            _reportDefinitions = LoadReportDefinitions(_reportDefinitionsFilePath);
            _lastGlobalSuccessDate = ReadLastGlobalSuccessDate();
            Logger.LogInfo($"AutoRunManager initialised. Check Hour: {_autoRunCheckHour}. Last Global Success: {_lastGlobalSuccessDate:yyyy-MM-dd}");
        }
        #endregion

        #region Report Definition Management
        /// <summary>
        /// Loads automated report definitions from the specified JSON file.
        /// </summary>
        /// <param name="definitionsFilePath">The full path to the report definitions JSON file.</param>
        /// <returns>A list of AutoReportDefinition objects.</returns>
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
        /// Saves a list of automated report definitions to the specified JSON file.
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
        /// Reloads the automated report definitions from the file system.
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

            if (!isTimerCurrentlyEnabled || !_reportDefinitions.Any(d => d.IsEnabled) || _isAutoRunTaskExecuting)
            {
                return AutoRunActionResult.NoActionNeeded;
            }

            DateTime now = DateTime.Now;
            _autoRunCheckHour = configuredHour;
            DailyReportRunStatus currentDayStatuses = ReadDailyReportStatuses();

            // Reset statuses if it's a new day.
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

            bool allDueReportsAlreadySucceeded = currentDayStatuses.AllCurrentlyEnabledAndDueReportsSucceeded(_reportDefinitions, now.DayOfWeek);
            int totalEnabledAndDueToday = _reportDefinitions.Count(def => def.IsEnabled && (!def.RunOnDayOfWeek.HasValue || def.RunOnDayOfWeek.Value == now.DayOfWeek));

            if (allDueReportsAlreadySucceeded && totalEnabledAndDueToday > 0)
            {
                string doneMsg = $"Auto Run: Done for {now:dd/MM}";
                AutoRunUIContext.ReportAutoRunStatusRight(doneMsg);
                AutoRunUIContext.UpdateAutoRunButtonAndStatus(true, true, doneMsg);
                return AutoRunActionResult.NoActionNeeded;
            }

            _isAutoRunTaskExecuting = true;
            AutoRunUIContext.SetControlsForAutoRunInProgress(true);
            _statusManager.Post($"Auto Run: Starting checks for {now:dd-MM-yyyy}...", MessageType.InProgress);

            try
            {
                foreach (var definition in _reportDefinitions)
                {
                    if (!definition.IsEnabled || (definition.RunOnDayOfWeek.HasValue && now.DayOfWeek != definition.RunOnDayOfWeek.Value)) continue;
                    currentDayStatuses = ReadDailyReportStatuses();
                    if (currentDayStatuses.GetReportSuccessStatus(definition.SuccessFlagJsonName)) continue;

                    _statusManager.Post($"Auto Run: Processing {definition.ReportName}...", MessageType.InProgress);
                    DateTime reportEndDate = ReportHelper.GetNthPreviousWorkday(now.Date, definition.ReportEndDateOffsetDays ?? 1);
                    DateTime reportStartDate = (definition.ReportDurationDays.HasValue && definition.ReportDurationDays.Value > 1) ? ReportHelper.GetNthPreviousWorkday(reportEndDate, definition.ReportDurationDays.Value - 1) : reportEndDate;

                    if (ReportTypeHelper.FromInt(definition.ReportTypeIndex) == ReportType.Weekly)
                    {
                        reportEndDate = now.Date;
                        reportStartDate = reportEndDate.AddDays(-14);
                    }

                    await RunConfiguredAutomatedReportAsync(definition, reportEndDate, reportStartDate, now.Date);
                }

                currentDayStatuses = ReadDailyReportStatuses();
                bool allNowSucceeded = currentDayStatuses.AllCurrentlyEnabledAndDueReportsSucceeded(_reportDefinitions, now.DayOfWeek);
                int succeededCount = _reportDefinitions.Count(def => def.IsEnabled && (!def.RunOnDayOfWeek.HasValue || def.RunOnDayOfWeek.Value == now.DayOfWeek) && currentDayStatuses.GetReportSuccessStatus(def.SuccessFlagJsonName));

                string finalMsg = allNowSucceeded ? $"Auto Run: All due DONE ({succeededCount}/{totalEnabledAndDueToday})" : $"Auto Run: Partial ({succeededCount}/{totalEnabledAndDueToday} succeeded)";
                AutoRunUIContext.ReportAutoRunStatusRight(finalMsg);
                AutoRunUIContext.UpdateAutoRunButtonAndStatus(isTimerCurrentlyEnabled, allNowSucceeded, finalMsg);

                if (allNowSucceeded && totalEnabledAndDueToday > 0) SaveLastGlobalSuccessDate(now.Date);

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
                _isAutoRunTaskExecuting = false;
                AutoRunUIContext.SetControlsForAutoRunInProgress(false);
            }
        }

        /// <summary>
        /// Saves the user-configured auto-run hour to the appsettings.json file.
        /// </summary>
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
                    catch (Exception ex) { Logger.LogError($"Error saving auto-run hour to appsettings.json: {ex.Message}", ex); return false; }
                }
            });
        }
        #endregion

        #region Private Helper Methods
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

            var progressAdapter = new Progress<string>(status => _statusManager.Post(status, MessageType.InProgress));

            string? finalAnalysisPath;

            try
            {
                if (!await _processManager.EnsureWrapperIsRunningAsync(progressAdapter, token)) throw new InvalidOperationException("Failed to start or connect to report service.");

                string crystalRptPath = _reportPathService.CrystalReportRptFilePath ?? throw new InvalidOperationException("Crystal Report path not configured.");

                string? outputPath = _reportPathService.GetRawReportOutputPath(currentReportType, reportEndDate, definition.ReportName);

                if (string.IsNullOrEmpty(outputPath))
                {
                    throw new InvalidOperationException($"Failed to generate a valid output path for the raw report '{definition.ReportName}'.");
                }

                var request = new ReportRequest { CrystalReportLocation = crystalRptPath, ReportOutputLocation = outputPath, ReportDateFrom = effectiveReportStartDate, ReportDateTo = reportEndDate };
                ReportResponse? response = await _pipeCommunicator.SendRequestReceiveResponseAsync(request, progressAdapter, token);
                if (!(response?.Success == true && !string.IsNullOrEmpty(response.OutputPath) && File.Exists(response.OutputPath)))
                {
                    throw new Exception($"Raw report generation failed: {response?.ErrorMessage ?? "Unknown wrapper error"}");
                }
                string generatedRawPath = response.OutputPath;

                string templatePath = _reportPathService.GetExcelTemplatePath(currentReportType) ?? throw new InvalidOperationException("Excel template path not configured.");
                string baseSaveLocation = _reportPathService.FinalReportOutputBaseDirectory ?? throw new InvalidOperationException("Final report output directory not configured.");

                finalAnalysisPath = await _excelProcessor.ProcessExcelReportAsync(
                    _excelProcessor.GetCurrentFinancialYear(true), currentReportType, generatedRawPath, "RawDataSourceSheet",
                    baseSaveLocation, templatePath, "TemplateDataCopySheet", 1, 1, reportEndDate, token);

                if (string.IsNullOrEmpty(finalAnalysisPath)) throw new Exception("Excel processing failed to produce a final file.");

                var (mailTo, mailCc) = _emailRecipientManager.GetRecipients(definition.ReportTypeIndex, false, IsDebug, true, definition);
                var (subject, body) = GetEmailSubjectAndBodyForAutoRun(definition, effectiveReportStartDate, reportEndDate);
                EmailSendResult emailResult = await _emailUtility.SendEmailAsync(mailTo, mailCc, subject, body, finalAnalysisPath, token);

                if (!emailResult.Success) throw new Exception($"Email sending failed: {emailResult.ErrorMessage}");

                Logger.LogInfo($"Auto Run ({definition.ReportName}): Email sent successfully.");
                overallSuccess = true;
            }
            catch (OperationCanceledException) { Logger.LogWarning($"Auto Run ({definition.ReportName}): Operation cancelled."); }
            catch (Exception ex)
            {
                Logger.LogError($"Auto Run ({definition.ReportName}): Error: {ex.Message}", ex);
                _statusManager.Post($"ERROR ({definition.ReportName}): {ex.Message.Substring(0, Math.Min(ex.Message.Length, 100))}", MessageType.Error);
            }
            finally { SaveDailyReportStatus(definition.SuccessFlagJsonName, overallSuccess, processingDate); }
            return overallSuccess;
        }

        private JObject GetOrAddSection(JObject parent, string fullSectionKeyPath, bool logCreation = true)
        {
            string[] segments = fullSectionKeyPath.Split(':'); JToken? currentToken = parent; JObject? section = null;
            foreach (string segment in segments)
            {
                if (currentToken is JObject currentObject) { if (!currentObject.TryGetValue(segment, out JToken? nextToken) || !(nextToken is JObject)) { var newObjSection = new JObject(); currentObject[segment] = newObjSection; currentToken = newObjSection; if (logCreation) Logger.LogDebug($"JSON segment '{segment}' created under '{currentObject.Path}'."); } else { currentToken = nextToken; } section = currentToken as JObject; }
                else { throw new InvalidOperationException($"Cannot create/access section '{segment}' as parent is not a JObject at path '{currentToken?.Path}'."); }
            }
            return section ?? throw new InvalidOperationException($"Section '{fullSectionKeyPath}' could not be resolved to a JObject.");
        }
        private DailyReportRunStatus ReadDailyReportStatuses() { lock (s_jsonFileLock) { try { if (!File.Exists(_appSettingsFilePath)) return new DailyReportRunStatus { StatusDate = DateTime.MinValue.ToString("yyyy-MM-dd") }; string jsonContent = File.ReadAllText(_appSettingsFilePath); if (string.IsNullOrWhiteSpace(jsonContent)) return new DailyReportRunStatus { StatusDate = DateTime.MinValue.ToString("yyyy-MM-dd") }; var jsonRoot = JObject.Parse(jsonContent); string jsonPath = AppConfigKeys.AutoRunProcess.DailyRunStatus.Replace(":", "."); JToken? statusToken = jsonRoot.SelectToken(jsonPath); if (statusToken != null) { var status = statusToken.ToObject<DailyReportRunStatus>(JsonSerializer.CreateDefault(new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore })); if (status != null) { status.StatusDate ??= DateTime.MinValue.ToString("yyyy-MM-dd"); return status; } } } catch (Exception ex) { Logger.LogError($"Error reading DailyReportStatus from appsettings.json: {ex.Message}", ex); } return new DailyReportRunStatus { StatusDate = DateTime.MinValue.ToString("yyyy-MM-dd") }; } }
        private void SaveDailyReportStatus(string successFlagJsonName, bool success, DateTime statusDate) { lock (s_jsonFileLock) { try { if (string.IsNullOrWhiteSpace(successFlagJsonName)) { return; } string todayDateString = statusDate.ToString("yyyy-MM-dd"); string jsonContent = File.Exists(_appSettingsFilePath) ? File.ReadAllText(_appSettingsFilePath) : "{}"; var jsonRoot = JObject.Parse(string.IsNullOrWhiteSpace(jsonContent) ? "{}" : jsonContent); JObject autoRunProcessSection = GetOrAddSection(jsonRoot, AppConfigKeys.AutoRunProcess.Base); string dailyRunStatusSimpleKey = AppConfigKeys.AutoRunProcess.DailyRunStatus.Split(':').Last(); JObject dailyStatusJson = GetOrAddSection(autoRunProcessSection, dailyRunStatusSimpleKey, logCreation: false); string statusDateSimpleKey = AppConfigKeys.AutoRunProcess.DailyRunStatus_StatusDate.Split(':').Last(); if (dailyStatusJson[statusDateSimpleKey]?.ToString() != todayDateString || !dailyStatusJson.Properties().Any(p => p.Name != statusDateSimpleKey)) { dailyStatusJson.RemoveAll(); dailyStatusJson[statusDateSimpleKey] = todayDateString; foreach (var def in _reportDefinitions.Where(d => !string.IsNullOrEmpty(d.SuccessFlagJsonName))) { dailyStatusJson[def.SuccessFlagJsonName] = false; } } dailyStatusJson[successFlagJsonName] = success; File.WriteAllText(_appSettingsFilePath, jsonRoot.ToString(Formatting.Indented)); } catch (Exception ex) { Logger.LogError($"Error saving DailyRunStatus for '{successFlagJsonName}': {ex.Message}", ex); } } }
        private void ResetDailyReportStatuses(DateTime forDate) { lock (s_jsonFileLock) { try { string jsonContent = File.Exists(_appSettingsFilePath) ? File.ReadAllText(_appSettingsFilePath) : "{}"; var jsonRoot = JObject.Parse(string.IsNullOrWhiteSpace(jsonContent) ? "{}" : jsonContent); JObject autoRunProcessSection = GetOrAddSection(jsonRoot, AppConfigKeys.AutoRunProcess.Base); string dailyRunStatusSimpleKey = AppConfigKeys.AutoRunProcess.DailyRunStatus.Split(':').Last(); string statusDateSimpleKey = AppConfigKeys.AutoRunProcess.DailyRunStatus_StatusDate.Split(':').Last(); JObject newStatusJson = new JObject { [statusDateSimpleKey] = forDate.ToString("yyyy-MM-dd") }; foreach (var definition in _reportDefinitions.Where(d => !string.IsNullOrEmpty(d.SuccessFlagJsonName))) { newStatusJson[definition.SuccessFlagJsonName] = false; } autoRunProcessSection[dailyRunStatusSimpleKey] = newStatusJson; File.WriteAllText(_appSettingsFilePath, jsonRoot.ToString(Formatting.Indented)); } catch (Exception ex) { Logger.LogError($"Error resetting DailyReportStatuses: {ex.Message}", ex); } } }
        private DateTime ReadLastGlobalSuccessDate() { lock (s_jsonFileLock) { try { if (!File.Exists(_appSettingsFilePath)) return DateTime.MinValue; string jsonContent = File.ReadAllText(_appSettingsFilePath); if (string.IsNullOrWhiteSpace(jsonContent)) return DateTime.MinValue; var json = JObject.Parse(jsonContent); string jsonPath = AppConfigKeys.AutoRunProcess.LastRunDate.Replace(":", "."); string? dateString = json.SelectToken(jsonPath)?.ToString(); if (DateTime.TryParseExact(dateString, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsedDate)) { return parsedDate.Date; } } catch (Exception ex) { Logger.LogError($"Error reading LastGlobalSuccessDate from '{AppConfigKeys.AutoRunProcess.LastRunDate}': {ex.Message}", ex); } return DateTime.MinValue; } }
        private void SaveLastGlobalSuccessDate(DateTime dateToSave) { lock (s_jsonFileLock) { try { string jsonContent = File.Exists(_appSettingsFilePath) ? File.ReadAllText(_appSettingsFilePath) : "{}"; var json = JObject.Parse(string.IsNullOrWhiteSpace(jsonContent) ? "{}" : jsonContent); JObject autoRunProcessSection = GetOrAddSection(json, AppConfigKeys.AutoRunProcess.Base); string lastRunDateSimpleKey = AppConfigKeys.AutoRunProcess.LastRunDate.Split(':').Last(); autoRunProcessSection[lastRunDateSimpleKey] = dateToSave.ToString("yyyy-MM-dd"); File.WriteAllText(_appSettingsFilePath, json.ToString(Formatting.Indented)); } catch (Exception ex) { Logger.LogError($"Error saving LastGlobalSuccessDate as '{AppConfigKeys.AutoRunProcess.LastRunDate}': {ex.Message}", ex); } } }

        private (string Subject, string Body) GetEmailSubjectAndBodyForAutoRun(AutoReportDefinition definition, DateTime reportStartDate, DateTime reportEndDate)
        {
            string greeting;
            if (IsDebug)
            {
                // The key for DebugDefault is just "DebugDefault"
                greeting = _greetingManager.GetGreeting(nameof(UserGreetingSettings.DebugDefault), isForDebugSection: true);
            }
            else
            {
                greeting = _greetingManager.GetGreeting(definition.GreetingKey);
            }

            if (!string.IsNullOrWhiteSpace(greeting) && !greeting.TrimEnd().EndsWith(",")) greeting = greeting.TrimEnd() + ",";

            string rangeInfo = (reportStartDate.Date == reportEndDate.Date) ? $"for {reportEndDate:dd MMM yy}" : $"for period {reportStartDate:dd MMM yy} to {reportEndDate:dd MMM yy}";

            string subjectDateSuffix = (reportStartDate.Date == reportEndDate.Date) ? $"({reportEndDate:yyyy-MM-dd})" : $"({reportStartDate:yyyy-MM-dd} to {reportEndDate:yyyy-MM-dd})";

            string subject = $"AUTOMATED: {definition.SubjectPrefix} Report {subjectDateSuffix}";
            if (IsDebug) subject = $"DEBUG - {subject}";

            string emailSignature = _configuration.GetValue<string>(AppConfigKeys.EmailSettings.DefaultEmailSignature, "Thank you,\nAutomation Service")!;
            string body = $"{greeting}\n\nPlease find attached the automated {definition.SubjectPrefix.ToLowerInvariant()} report {rangeInfo}.\n\n{emailSignature}";

            return (subject, body);
        }
        #endregion
    }
}