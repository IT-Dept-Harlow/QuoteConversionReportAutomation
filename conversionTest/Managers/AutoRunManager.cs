// C# 10+ Features
namespace QuoteConversionReportAutomation.Managers
{
    // --- Standard and Third-Party Using Statements ---
    using Microsoft.Extensions.Configuration; // For IConfiguration
    using Newtonsoft.Json; // For JSON serialization/deserialization
    using Newtonsoft.Json.Linq; // For JObject manipulation
    using QuoteConversionReportAutomation.Helpers; // For helper classes like ReportHelper, FolderCreation
    using QuoteConversionReportAutomation.Models; // For data models like DailyReportRunStatus, AutoReportDefinition
    using QuoteConversionReportAutomation.Services.Communication; // For EmailUtility, NamedPipeCommunicator
    using QuoteConversionReportAutomation.Services.Excel; // For ExcelCopyData
    using QuoteConversionReportAutomation.Services.Logging; // For Logger
    using System;
    using System.Collections.Generic; // For List
    using System.Globalization; // For CultureInfo, DateTimeStyles
    using System.IO; // For Path, File, Directory operations
    using System.Linq; // For LINQ operations like FirstOrDefault
    using System.Threading; // For CancellationToken, CancellationTokenSource
    using System.Threading.Tasks; // For Task, Task.Run

    public enum AutoRunActionResult
    {
        NoActionNeeded,
        ActionAttempted,
        CriticalError
    }

    public class AutoRunManager
    {
        #region Fields and Properties
        private readonly IConfiguration _configuration;
        private readonly EmailUtility _emailUtility;
        private readonly ReportProcessManager _processManager;
        private readonly NamedPipeCommunicator _pipeCommunicator;
        private readonly UIManager _uiManager;
        private readonly ExcelCopyData _excelProcessor;
        private readonly EmailRecipientManager _emailRecipientManager;
        private readonly GreetingManager _greetingManager;
        private readonly string _appSettingsPath;
        private static readonly object _jsonFileLock = new object();

        private bool _isAutoRunTaskExecuting = false;
        private DateTime _lastGlobalSuccessDate = DateTime.MinValue;
        private int _autoRunCheckHour;

        private readonly List<AutoReportDefinition> _reportDefinitions;

        private const string JsonSectionSettings = "settings";
        private const string JsonSectionAutoReport = "AutoReport";
        private const string JsonKeyDailyRunStatus = "DailyRunStatus";
        private const string JsonKeyStatusDate = "StatusDate";
        private const string JsonKeyLastRunDate = "LastRunDate";
        private const string JsonKeyAutoRunCheckHour = "AutoRunCheckHour";

        private static bool IsDebug =>
#if DEBUG
            true;
#else
            false;
#endif

        private string UserProfilePath => Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        private string ExcelFinalSaveLocation => Path.Combine(UserProfilePath, _configuration[$"{JsonSectionSettings}:ExcelFinalSaveLocation"]?.TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) ?? @"Harlow Printing\IT Projects - Documents\Dashboard Datasets\Raw_data\Quotes conversion\Estimates");
        private string CrystalReportLocation => _configuration[$"{JsonSectionSettings}:CrystalReportPath"] ?? string.Empty;
        private string RawReportExportBaseDir => Path.Combine(UserProfilePath, _configuration[$"{JsonSectionSettings}:RawReportExportBaseDir"]?.TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) ?? @"Harlow Printing\IT Projects - Documents\Dashboard Datasets\Raw_data\Quotes conversion\Estimate Reports Exports");
        public string ExcelTemplateBaseDir => Path.Combine(UserProfilePath, _configuration[$"{JsonSectionSettings}:ExcelTemplateFolder"]?.TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) ?? @"Harlow Printing\IT Projects - Documents\Dashboard Datasets\Raw_data\Quotes conversion\TEMPLATE");

        #endregion

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

            _reportDefinitions = _configuration.GetSection($"{JsonSectionAutoReport}:ReportDefinitions").Get<List<AutoReportDefinition>>() ?? new List<AutoReportDefinition>();
            if (!_reportDefinitions.Any())
            {
                Logger.LogWarning("AutoRunManager: No report definitions found in configuration. Auto-run will not process any reports.");
            }
            else
            {
                Logger.LogInfo($"AutoRunManager: Loaded {_reportDefinitions.Count} report definitions.");
                foreach (var def in _reportDefinitions.Where(d => d != null)) // Ensure def is not null
                {
                    Logger.LogDebug($"Loaded Definition: {def.ReportName}, Index: {def.ReportTypeIndex}, EnableKey: {def.EnableConfigKey}, SuccessFlag: {def.SuccessFlagJsonName}");
                }
            }

            _lastGlobalSuccessDate = ReadLastGlobalSuccessDate();
            Logger.LogInfo($"AutoRunManager initialized. Auto-run check hour: {_autoRunCheckHour}. Last Global Success Date: {_lastGlobalSuccessDate:yyyy-MM-dd}");
        }

        public async Task<AutoRunActionResult> PerformDailyCheckAsync(bool isTimerCurrentlyEnabled, int configuredHour)
        {
            if (!isTimerCurrentlyEnabled || !_reportDefinitions.Any(d => d != null))
            {
                if (!_reportDefinitions.Any(d => d != null)) Logger.LogInfo("Auto Run: No valid report definitions loaded. Skipping check.");
                return AutoRunActionResult.NoActionNeeded;
            }

            if (_isAutoRunTaskExecuting)
            {
                Logger.LogInfo("Auto Run: Task is already executing. Skipping this check cycle.");
                return AutoRunActionResult.NoActionNeeded;
            }

            DateTime now = DateTime.Now;
            _autoRunCheckHour = configuredHour;
            AutoRunActionResult overallResult = AutoRunActionResult.NoActionNeeded;

            DailyReportRunStatus currentDayStatuses = ReadDailyReportStatuses();

            if (currentDayStatuses.StatusDate != now.ToString("yyyy-MM-dd"))
            {
                Logger.LogInfo($"Persisted StatusDate ({currentDayStatuses.StatusDate}) is not for today ({now:yyyy-MM-dd}). Resetting daily report statuses for today.");
                ResetDailyReportStatuses(now.Date);
                currentDayStatuses = ReadDailyReportStatuses();
                _uiManager.UpdateAutoRunUI(true, false, UIManager.IsWindowsDarkModeEnabled(), $"Auto Run: Enabled (Next check ~{_autoRunCheckHour}:00)");
            }

            if (now.Hour != _autoRunCheckHour)
            {
                Logger.LogDebug($"Auto Run: Not the configured hour ({_autoRunCheckHour}). Current hour: {now.Hour}. Skipping report execution.");
                return AutoRunActionResult.NoActionNeeded;
            }

            // Check if all *due* reports have already succeeded for today.
            // This uses the new method in DailyReportRunStatus.
            bool allDueReportsAlreadySucceeded = currentDayStatuses.AllCurrentlyEnabledAndDueReportsSucceeded(_configuration, _reportDefinitions, now.DayOfWeek);
            int totalEnabledAndDueTodayForInitialCheck = _reportDefinitions.Count(def =>
                def != null &&
                _configuration.GetValue<bool>($"{JsonSectionAutoReport}:{def.EnableConfigKey}", false) &&
                (!def.RunOnDayOfWeek.HasValue || def.RunOnDayOfWeek.Value == now.DayOfWeek)
            );


            if (allDueReportsAlreadySucceeded && totalEnabledAndDueTodayForInitialCheck > 0) // Only consider "done for today" if there were reports due and they all succeeded.
            {
                Logger.LogInfo($"Auto Run: All enabled AND DUE reports already succeeded for today ({now:yyyy-MM-dd}). No further action needed at this hour.");
                _uiManager.UpdateStatusRight($"Auto Run: Done for {now:dd/MM}");
                _uiManager.UpdateAutoRunUI(true, true, UIManager.IsWindowsDarkModeEnabled(), $"Auto Run: Done for {now:dd/MM}");
                return AutoRunActionResult.NoActionNeeded;
            }
            // If totalEnabledAndDueTodayForInitialCheck is 0, it means no reports were due, so we proceed to check if any action is needed (e.g. logging "no reports due")
            // or if it's the first check of the hour. The _lastGlobalSuccessDate check is more about avoiding re-processing if the app restarts within the same success day.
            // The more granular check above handles the "all due reports for today are done" scenario.

            _isAutoRunTaskExecuting = true;
            overallResult = AutoRunActionResult.ActionAttempted;
            _uiManager.DisableControlsForAutoRun();
            _uiManager.UpdateStatusMain($"Auto Run: Starting checks for {now:dd-MM-yyyy} (scheduled ~{_autoRunCheckHour}:00)...");
            Logger.LogInfo($"Auto Run: Triggered for {now:yyyy-MM-dd} at {now:HH:mm:ss}. Persisted StatusDate: {currentDayStatuses.StatusDate}");

            bool anyReportActuallyAttemptedThisCycle = false;

            try
            {
                foreach (var definition in _reportDefinitions.Where(d => d != null)) // Ensure definition is not null
                {
                    bool isEnabled = _configuration.GetValue<bool>($"{JsonSectionAutoReport}:{definition.EnableConfigKey}", false);
                    if (!isEnabled)
                    {
                        Logger.LogInfo($"Auto Run: Report '{definition.ReportName}' (Key: {definition.EnableConfigKey}) is DISABLED. Skipping.");
                        continue;
                    }

                    if (definition.RunOnDayOfWeek.HasValue && now.DayOfWeek != definition.RunOnDayOfWeek.Value)
                    {
                        Logger.LogInfo($"Auto Run: Report '{definition.ReportName}' is configured to run on {definition.RunOnDayOfWeek.Value}, but today is {now.DayOfWeek}. Skipping.");
                        continue;
                    }

                    currentDayStatuses = ReadDailyReportStatuses(); // Refresh status before checking this specific report
                    if (currentDayStatuses.GetReportSuccessStatus(definition.SuccessFlagJsonName))
                    {
                        Logger.LogInfo($"Auto Run: Report '{definition.ReportName}' (Flag: {definition.SuccessFlagJsonName}) already succeeded today. Skipping.");
                        continue;
                    }

                    anyReportActuallyAttemptedThisCycle = true;
                    _uiManager.UpdateStatusMain($"Auto Run: Processing {definition.ReportName}...");
                    Logger.LogInfo($"Auto Run: Report '{definition.ReportName}' is ENABLED and PENDING. Attempting to run.");

                    DateTime reportEndDate;
                    DateTime? reportStartDate = null;

                    if (definition.ReportName == "Weekly Estimate Success Rate")
                    {
                        reportEndDate = now.Date;
                        reportStartDate = reportEndDate.AddDays(-14);
                        Logger.LogDebug($"Weekly Report Dates: Start={reportStartDate:yyyy-MM-dd}, End={reportEndDate:yyyy-MM-dd}");
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
                        Logger.LogDebug($"Offset-based Report '{definition.ReportName}' Dates: Start={reportStartDate:yyyy-MM-dd}, End={reportEndDate:yyyy-MM-dd}");
                    }
                    else
                    {
                        reportEndDate = ReportHelper.GetPreviousWorkday(now.Date);
                        reportStartDate = reportEndDate;
                        Logger.LogWarning($"Auto Run: Report '{definition.ReportName}' has no specific date offset or duration defined. Defaulting to previous workday ({reportEndDate:yyyy-MM-dd}).");
                    }

                    await RunConfiguredAutomatedReportAsync(definition, reportEndDate, reportStartDate, now.Date);
                }

                if (!anyReportActuallyAttemptedThisCycle && totalEnabledAndDueTodayForInitialCheck > 0) // If reports were due but none were attempted (e.g. all succeeded already)
                {
                    // This case should be caught by the "allDueReportsAlreadySucceeded" check earlier.
                    // If we reach here and nothing was attempted, it might mean all due reports were already marked as success.
                    // The overallResult might need to be NoActionNeeded if no new actions were taken.
                    Logger.LogInfo("Auto Run: No new report processing was attempted in this cycle (likely all due reports already marked success).");
                    if (allDueReportsAlreadySucceeded)
                    { // Re-affirm based on current state
                        overallResult = AutoRunActionResult.NoActionNeeded;
                    }

                }
                else if (!anyReportActuallyAttemptedThisCycle && totalEnabledAndDueTodayForInitialCheck == 0)
                {
                    // No reports were due, and none were attempted.
                    overallResult = AutoRunActionResult.NoActionNeeded;
                    Logger.LogInfo("Auto Run: No reports were due for execution in this cycle.");
                }


                currentDayStatuses = ReadDailyReportStatuses();
                DayOfWeek todayDayOfWeek = now.DayOfWeek;
                bool allEnabledAndDueReportsSucceededToday = currentDayStatuses.AllCurrentlyEnabledAndDueReportsSucceeded(_configuration, _reportDefinitions, todayDayOfWeek);

                string finalStatusMessage;

                int totalEnabledAndDueReportsToday = _reportDefinitions.Count(def =>
                    def != null &&
                    _configuration.GetValue<bool>($"{JsonSectionAutoReport}:{def.EnableConfigKey}", false) &&
                    (!def.RunOnDayOfWeek.HasValue || def.RunOnDayOfWeek.Value == todayDayOfWeek)
                );

                int totalSucceededAmongEnabledAndDueToday = _reportDefinitions.Count(def =>
                    def != null &&
                    _configuration.GetValue<bool>($"{JsonSectionAutoReport}:{def.EnableConfigKey}", false) &&
                    (!def.RunOnDayOfWeek.HasValue || def.RunOnDayOfWeek.Value == todayDayOfWeek) &&
                    currentDayStatuses.GetReportSuccessStatus(def.SuccessFlagJsonName)
                );

                if (totalEnabledAndDueReportsToday == 0)
                {
                    int totalAnyEnabledReports = _reportDefinitions.Count(def => def != null && _configuration.GetValue<bool>($"{JsonSectionAutoReport}:{def.EnableConfigKey}", false));
                    if (totalAnyEnabledReports == 0)
                    {
                        finalStatusMessage = $"Auto Run: No reports enabled {now:dd/MM HH:mm}";
                    }
                    else
                    {
                        finalStatusMessage = $"Auto Run: No reports due today {now:dd/MM HH:mm}";
                    }
                    Logger.LogInfo(finalStatusMessage);
                    // If nothing was due and nothing was attempted, result should be NoActionNeeded.
                    if (!anyReportActuallyAttemptedThisCycle) overallResult = AutoRunActionResult.NoActionNeeded;
                }
                else if (allEnabledAndDueReportsSucceededToday)
                {
                    SaveLastGlobalSuccessDate(now.Date); // Save if all *due* reports succeeded
                    _lastGlobalSuccessDate = now.Date;
                    finalStatusMessage = $"Auto Run: All due reports DONE ({totalSucceededAmongEnabledAndDueToday}/{totalEnabledAndDueReportsToday}) for {now:dd/MM HH:mm}";
                    Logger.LogInfo(finalStatusMessage);
                }
                else
                {
                    finalStatusMessage = $"Auto Run: Partial success ({totalSucceededAmongEnabledAndDueToday}/{totalEnabledAndDueReportsToday} due reports succeeded) {now:dd/MM HH:mm}";
                    Logger.LogWarning(finalStatusMessage + ". Will retry incomplete reports if app restarts or at next check hour if within the same day.");
                }
                _uiManager.UpdateStatusRight(finalStatusMessage);
                _uiManager.UpdateAutoRunUI(isTimerCurrentlyEnabled, allEnabledAndDueReportsSucceededToday && totalEnabledAndDueReportsToday > 0, UIManager.IsWindowsDarkModeEnabled(), finalStatusMessage);
            }
            catch (Exception ex)
            {
                Logger.LogCritical($"Auto Run: Unhandled exception during PerformDailyCheckAsync: {ex.Message}", ex);
                string errorMsg = $"Auto Run: CRITICAL ERROR {now:dd/MM HH:mm}";
                _uiManager.UpdateStatusRight(errorMsg);
                _uiManager.UpdateAutoRunUI(isTimerCurrentlyEnabled, true, UIManager.IsWindowsDarkModeEnabled(), errorMsg);
                overallResult = AutoRunActionResult.CriticalError;
            }
            finally
            {
                _isAutoRunTaskExecuting = false;
                // If overallResult is still ActionAttempted but no specific report was actually run this cycle (e.g., all were already done but it wasn't caught by the top check)
                // then it should be NoActionNeeded.
                if (overallResult == AutoRunActionResult.ActionAttempted && !anyReportActuallyAttemptedThisCycle && totalEnabledAndDueTodayForInitialCheck > 0 && allDueReportsAlreadySucceeded)
                {
                    overallResult = AutoRunActionResult.NoActionNeeded;
                }
            }
            return overallResult;
        }

        private async Task<bool> RunConfiguredAutomatedReportAsync(AutoReportDefinition definition, DateTime reportEndDate, DateTime? reportStartDate, DateTime processingDate)
        {
            DateTime effectiveReportStartDate = reportStartDate ?? reportEndDate;
            Logger.LogInfo($"Auto Run: Executing: {definition.ReportName} for date {reportEndDate:yyyy-MM-dd} (Start: {effectiveReportStartDate:yyyy-MM-dd}, Processing Date: {processingDate:yyyy-MM-dd})");

            bool success = false;
            using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(15));
            var token = cts.Token;

            IProgress<string> localProgress = new Progress<string>(status => Logger.LogDebug($"AutoRun ({definition.ReportName}): {status}"));
            IProgress<ProgressReport> localExcelProgress = new Progress<ProgressReport>(report => Logger.LogDebug($"AutoRun ({definition.ReportName}) Excel: {report.Message}"));

            string? generatedRawPath = null;
            string? finalAnalysisPath = null;

            try
            {
                _uiManager.UpdateProgress($"Auto Run ({definition.ReportName}): Ensuring report service...");
                if (!await _processManager.EnsureWrapperIsRunningAsync(localProgress, token))
                { throw new InvalidOperationException($"Auto Run Error ({definition.ReportName}): Failed to start or connect to the report service."); }

                _uiManager.UpdateProgress($"Auto Run ({definition.ReportName}): Preparing request...");
                string outputPath = GetAutomatedReportOutputPath(definition.ReportTypeIndex, reportEndDate, definition.ReportName);

                string crystalReportPath = CrystalReportLocation;
                if (string.IsNullOrEmpty(crystalReportPath) || !File.Exists(crystalReportPath))
                { throw new FileNotFoundException($"Auto Run Error ({definition.ReportName}): Crystal Report file path is invalid or missing.", crystalReportPath); }

                var request = new ReportRequest
                {
                    CrystalReportLocation = crystalReportPath,
                    ReportOutputLocation = outputPath,
                    ReportDateFrom = effectiveReportStartDate,
                    ReportDateTo = reportEndDate
                };

                _uiManager.UpdateProgress($"Auto Run ({definition.ReportName}): Requesting raw report...");
                ReportResponse? response = await _pipeCommunicator.SendRequestReceiveResponseAsync(request, localProgress, token);

                if (response?.Success == true && !string.IsNullOrEmpty(response.OutputPath) && File.Exists(response.OutputPath))
                {
                    generatedRawPath = response.OutputPath;
                    Logger.LogInfo($"Auto Run ({definition.ReportName}): Raw report generated: {generatedRawPath}");
                    _uiManager.UpdateProgress($"Auto Run ({definition.ReportName}): Raw report created.");
                }
                else
                {
                    string errorMessage = response?.ErrorMessage ?? "Unknown error from report service.";
                    if (response?.Success == true && (string.IsNullOrEmpty(response.OutputPath) || !File.Exists(response.OutputPath)))
                    { errorMessage = $"Auto Run Error ({definition.ReportName}): Report service success, but output file invalid/missing ('{response?.OutputPath ?? "NULL"}')."; }
                    Logger.LogError($"Auto Run Error ({definition.ReportName}): Report generation failed for '{outputPath}'. Message: {errorMessage}");
                    throw new Exception($"Auto Run Error ({definition.ReportName}): Report generation failed: {errorMessage}");
                }

                _uiManager.UpdateProgress($"Auto Run ({definition.ReportName}): Processing report...");
                string templatePath = Path.Combine(ExcelTemplateBaseDir, definition.TemplateName);
                string baseSaveLocation = ExcelFinalSaveLocation;
                string currentFY = _excelProcessor.GetCurrentFinancialYear(true);

                if (string.IsNullOrEmpty(templatePath) || !File.Exists(templatePath))
                { throw new FileNotFoundException($"Auto Run Error ({definition.ReportName}): Template '{definition.TemplateName}' not found at '{templatePath}'.", templatePath); }

                string? expectedFinalPath = _excelProcessor.GetExpectedFinalFilePath(definition.ReportTypeIndex, baseSaveLocation, reportEndDate);
                if (expectedFinalPath != null && File.Exists(expectedFinalPath))
                {
                    try
                    {
                        File.Delete(expectedFinalPath);
                        Logger.LogInfo($"Auto Run ({definition.ReportName}): Deleted existing final file: {expectedFinalPath}");
                    }
                    catch (Exception delEx)
                    {
                        Logger.LogWarning($"Auto Run ({definition.ReportName}): Failed to delete existing file '{expectedFinalPath}': {delEx.Message}. Processing will attempt to overwrite.");
                    }
                }

                finalAnalysisPath = await _excelProcessor.ProcessExcelReportAsync(
                    currentFY,
                    definition.ReportTypeIndex,
                    generatedRawPath,
                    "Sheet1",
                    baseSaveLocation,
                    templatePath,
                    "DATA",
                    1, 1,
                    localExcelProgress,
                    reportEndDate,
                    token);

                if (string.IsNullOrEmpty(finalAnalysisPath) || !File.Exists(finalAnalysisPath))
                {
                    if (token.IsCancellationRequested) throw new OperationCanceledException($"Auto Run ({definition.ReportName}): Excel processing cancelled.");
                    throw new Exception($"Auto Run Error ({definition.ReportName}): Excel processing failed. Check logs.");
                }
                Logger.LogInfo($"Auto Run ({definition.ReportName}): Report processed: {finalAnalysisPath}");
                _uiManager.UpdateProgress($"Auto Run ({definition.ReportName}): Report processed.");

                _uiManager.UpdateProgress($"Auto Run ({definition.ReportName}): Sending email...");
                var (mailTo, mailCc) = _emailRecipientManager.GetRecipients(definition.ReportTypeIndex, isFemiOnlyChecked: false, IsDebug, isAutoRunContext: true);
                var (subject, body) = GetEmailSubjectAndBodyForAutoRun(definition, effectiveReportStartDate, reportEndDate);

                bool emailSuccess = await _emailUtility.SendEmailAsync(mailTo, mailCc, subject, body, finalAnalysisPath, localProgress, token);
                if (!emailSuccess)
                {
                    if (token.IsCancellationRequested) throw new OperationCanceledException($"Auto Run ({definition.ReportName}): Email sending cancelled.");
                    throw new Exception($"Auto Run Error ({definition.ReportName}): Email sending failed. Check logs.");
                }
                Logger.LogInfo($"Auto Run ({definition.ReportName}): Email sent successfully for {definition.ReportName}.");
                _uiManager.UpdateProgress($"Auto Run ({definition.ReportName}): Email sent.");
                success = true;
            }
            catch (OperationCanceledException)
            {
                Logger.LogWarning($"Auto Run ({definition.ReportName}): Operation cancelled.");
                _uiManager.UpdateProgress($"Auto Run ({definition.ReportName}): Operation cancelled.");
                success = false;
            }
            catch (Exception ex)
            {
                Logger.LogError($"Auto Run ({definition.ReportName}): Error: {ex.Message}", ex);
                string shortError = ex.Message.Length > 100 ? ex.Message.Substring(0, 100) + "..." : ex.Message;
                _uiManager.UpdateProgress($"Auto Run ({definition.ReportName}): ERROR - {shortError}");
                success = false;
            }
            finally
            {
                SaveDailyReportStatus(definition.SuccessFlagJsonName, success, processingDate);
            }
            return success;
        }

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

        private DailyReportRunStatus ReadDailyReportStatuses()
        {
            try
            {
                if (!File.Exists(_appSettingsPath))
                {
                    Logger.LogWarning("appsettings.json not found for ReadDailyReportStatuses. Returning new status object with MinValue date.");
                    return new DailyReportRunStatus { StatusDate = DateTime.MinValue.ToString("yyyy-MM-dd") };
                }

                string jsonContent = File.ReadAllText(_appSettingsPath);
                var jsonRoot = JObject.Parse(jsonContent);
                JToken? autoReportToken = jsonRoot[JsonSectionAutoReport];
                JToken? statusToken = autoReportToken?[JsonKeyDailyRunStatus];

                if (statusToken != null)
                {
                    var status = statusToken.ToObject<DailyReportRunStatus>(JsonSerializer.CreateDefault(new JsonSerializerSettings
                    {
                        NullValueHandling = NullValueHandling.Ignore // Important for JsonExtensionData
                    }));

                    if (status == null)
                    {
                        Logger.LogWarning("DailyRunStatus section is null after parsing. Returning default status with MinValue date.");
                        return new DailyReportRunStatus { StatusDate = DateTime.MinValue.ToString("yyyy-MM-dd") };
                    }
                    status.StatusDate ??= DateTime.MinValue.ToString("yyyy-MM-dd"); // Ensure StatusDate is not null
                    return status;
                }
                Logger.LogWarning($"'{JsonSectionAutoReport}:{JsonKeyDailyRunStatus}' section not found in appsettings.json. Returning default status object with MinValue date.");
            }
            catch (JsonException jsonEx)
            {
                Logger.LogError($"Error parsing DailyRunStatus from appsettings.json (JSON format issue): {jsonEx.Message}", jsonEx);
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error reading DailyRunStatus from appsettings.json: {ex.Message}", ex);
            }
            return new DailyReportRunStatus { StatusDate = DateTime.MinValue.ToString("yyyy-MM-dd") };
        }

        private void SaveDailyReportStatus(string successFlagJsonName, bool success, DateTime statusDate)
        {
            lock (_jsonFileLock)
            {
                try
                {
                    string todayDateString = statusDate.ToString("yyyy-MM-dd");
                    string jsonContent = File.Exists(_appSettingsPath) ? File.ReadAllText(_appSettingsPath) : "{}";
                    var jsonRoot = JObject.Parse(jsonContent);

                    JObject autoReportSection = GetOrAddSection(jsonRoot, JsonSectionAutoReport);
                    JObject dailyStatusJson = GetOrAddSection(autoReportSection, JsonKeyDailyRunStatus, logCreation: false); // Get or create the DailyRunStatus object

                    // If the date has changed or the section is new, initialize all known report flags
                    if (dailyStatusJson[JsonKeyStatusDate]?.ToString() != todayDateString || !dailyStatusJson.HasValues || dailyStatusJson[JsonKeyStatusDate] == null)
                    {
                        dailyStatusJson.RemoveAll(); // Clear old statuses if any
                        dailyStatusJson[JsonKeyStatusDate] = todayDateString;
                        foreach (var def in _reportDefinitions.Where(d => d != null && !string.IsNullOrEmpty(d.SuccessFlagJsonName)))
                        {
                            dailyStatusJson[def.SuccessFlagJsonName] = false; // Initialize all to false for the new day
                        }
                        Logger.LogInfo($"DailyRunStatus in JSON was for a different date or newly created/empty. Initialized for {todayDateString} with all defined report flags set to false.");
                    }

                    // Now set the specific report's status
                    dailyStatusJson[successFlagJsonName] = success;

                    File.WriteAllText(_appSettingsPath, jsonRoot.ToString(Formatting.Indented));
                    Logger.LogInfo($"Saved DailyRunStatus for '{successFlagJsonName}': Success={success}, Date={todayDateString}");
                }
                catch (Exception ex)
                {
                    Logger.LogError($"Error saving DailyRunStatus to appsettings.json for '{successFlagJsonName}': {ex.Message}", ex);
                }
            }
        }

        private void ResetDailyReportStatuses(DateTime forDate)
        {
            lock (_jsonFileLock)
            {
                try
                {
                    string jsonContent = File.Exists(_appSettingsPath) ? File.ReadAllText(_appSettingsPath) : "{}";
                    var jsonRoot = JObject.Parse(jsonContent);

                    JObject autoReportSection = GetOrAddSection(jsonRoot, JsonSectionAutoReport);

                    JObject newStatusJson = new JObject
                    {
                        [JsonKeyStatusDate] = forDate.ToString("yyyy-MM-dd")
                    };

                    foreach (var definition in _reportDefinitions.Where(d => d != null && !string.IsNullOrEmpty(d.SuccessFlagJsonName)))
                    {
                        newStatusJson[definition.SuccessFlagJsonName] = false;
                    }

                    autoReportSection[JsonKeyDailyRunStatus] = newStatusJson;

                    File.WriteAllText(_appSettingsPath, jsonRoot.ToString(Formatting.Indented));
                    Logger.LogInfo($"Reset DailyReportStatuses in JSON for date {forDate:yyyy-MM-dd}. All defined report success flags set to false.");
                }
                catch (Exception ex)
                {
                    Logger.LogError($"Error resetting DailyRunStatuses in appsettings.json: {ex.Message}", ex);
                }
            }
        }

        private DateTime ReadLastGlobalSuccessDate()
        {
            try
            {
                if (!File.Exists(_appSettingsPath)) return DateTime.MinValue;
                string jsonContent = File.ReadAllText(_appSettingsPath);
                var json = JObject.Parse(jsonContent);
                string? dateString = json?[JsonSectionAutoReport]?[JsonKeyLastRunDate]?.ToString();
                if (DateTime.TryParseExact(dateString, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsedDate))
                {
                    return parsedDate.Date;
                }
            }
            catch (Exception ex) { Logger.LogError($"Error reading LastGlobalSuccessDate ('{JsonKeyLastRunDate}' from JSON): {ex.Message}", ex); }
            return DateTime.MinValue;
        }

        private void SaveLastGlobalSuccessDate(DateTime dateToSave)
        {
            lock (_jsonFileLock)
            {
                try
                {
                    string jsonContent = File.Exists(_appSettingsPath) ? File.ReadAllText(_appSettingsPath) : "{}";
                    var json = JObject.Parse(jsonContent);
                    JObject autoReportSection = GetOrAddSection(json, JsonSectionAutoReport);
                    autoReportSection[JsonKeyLastRunDate] = dateToSave.ToString("yyyy-MM-dd");
                    File.WriteAllText(_appSettingsPath, json.ToString(Formatting.Indented));
                    Logger.LogInfo($"Successfully saved LastGlobalSuccessDate (as '{JsonKeyLastRunDate}' in JSON): {dateToSave:yyyy-MM-dd}");
                }
                catch (Exception ex) { Logger.LogError($"Error saving LastGlobalSuccessDate (as '{JsonKeyLastRunDate}' in JSON): {ex.Message}", ex); }
            }
        }

        public async Task<bool> SetAutoRunHourAsync(int newHour)
        {
            if (newHour < 0 || newHour > 23)
            {
                Logger.LogError($"SetAutoRunHourAsync: Invalid hour provided: {newHour}. Must be between 0 and 23.");
                return false;
            }
            _autoRunCheckHour = newHour;
            Logger.LogInfo($"SetAutoRunHourAsync: Attempting to set auto-run hour to {newHour}. Internal state updated.");

            try
            {
                string jsonContent = await File.ReadAllTextAsync(_appSettingsPath);
                var json = JObject.Parse(jsonContent);
                JObject settingsSection = GetOrAddSection(json, JsonSectionSettings);
                settingsSection[JsonKeyAutoRunCheckHour] = newHour;
                await File.WriteAllTextAsync(_appSettingsPath, json.ToString(Formatting.Indented));
                Logger.LogInfo($"Successfully saved '{JsonKeyAutoRunCheckHour}' ({newHour}) to appsettings.json.");
                return true;
            }
            catch (Exception ex)
            {
                Logger.LogError($"SetAutoRunHourAsync: Error saving '{JsonKeyAutoRunCheckHour}': {ex.Message}", ex);
                return false;
            }
        }

        private string GetAutomatedReportOutputPath(int reportTypeIndex, DateTime reportDate, string reportName)
        {
            string baseDir = RawReportExportBaseDir;
            string sanitizedReportName = string.Join("_", reportName.Split(Path.GetInvalidFileNameChars())).Replace(" ", "_");
            string fileName = $"{reportDate:yyyyMMdd}_{sanitizedReportName}_Raw_AutoType{reportTypeIndex}.xlsx";

            string fullPath;
            try
            {
                string? folderPath = FolderCreation.CreateReportSpecificFolder(reportTypeIndex, baseDir, reportDate);
                if (!string.IsNullOrEmpty(folderPath))
                {
                    fullPath = Path.Combine(folderPath, fileName);
                }
                else
                {
                    string fallbackFolder = Path.Combine(baseDir, $"AutoRun_Fallback_{sanitizedReportName}_Type{reportTypeIndex}");
                    Directory.CreateDirectory(fallbackFolder); // Ensure fallback directory exists
                    fullPath = Path.Combine(fallbackFolder, fileName);
                    Logger.LogWarning($"GetAutomatedReportOutputPath: Using fallback folder for Report '{reportName}': {fullPath}");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Auto Run: Critical error determining raw output directory for Report '{reportName}': {ex.Message}", ex);
                string errorFallbackFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), $"QuoteConversion_ErrorFallback_Raw_{sanitizedReportName}_AutoType{reportTypeIndex}");
                try { Directory.CreateDirectory(errorFallbackFolder); } catch { /* Best effort */ }
                fullPath = Path.Combine(errorFallbackFolder, fileName);
                Logger.LogError($"GetAutomatedReportOutputPath: Using CRITICAL ErrorFallback path for Report '{reportName}': {fullPath}");
            }
            return fullPath;
        }

        private (string Subject, string Body) GetEmailSubjectAndBodyForAutoRun(AutoReportDefinition definition, DateTime reportStartDate, DateTime reportEndDate)
        {
            string greeting;
            if (IsDebug)
            {
                greeting = _greetingManager.GetGreeting("DebugDefault", isForDebugSection: true);
            }
            else
            {
                greeting = _greetingManager.GetGreeting(definition.GreetingKey);
            }

            if (!string.IsNullOrWhiteSpace(greeting) && !greeting.TrimEnd().EndsWith(","))
            {
                greeting = greeting.TrimEnd() + ",";
            }

            string dateRangeInfo = (reportStartDate.Date == reportEndDate.Date) ?
                                   $"for {reportEndDate:dd MMM yy}" :
                                   $"for period {reportStartDate:dd MMM yy} to {reportEndDate:dd MMM yy}";
            if (definition.ReportName == "Weekly Estimate Success Rate") // Specific handling for weekly report name if needed
            {
                dateRangeInfo = $"for period {reportStartDate:dd MMM yy} to {reportEndDate:dd MMM yy}";
            }

            string subjectDateSuffix = (reportStartDate.Date == reportEndDate.Date) ?
                                       $"({reportEndDate:yyyy-MM-dd})" :
                                       $"({reportStartDate:yyyy-MM-dd} to {reportEndDate:yyyy-MM-dd})";
            if (definition.ReportName == "Weekly Estimate Success Rate") // Specific handling for weekly report name if needed
            {
                subjectDateSuffix = $"({reportStartDate:yyyy-MM-dd} to {reportEndDate:yyyy-MM-dd})";
            }

            string subject = $"AUTOMATED: {definition.SubjectPrefix} Report {subjectDateSuffix}";
            string body = $"{greeting}\n\nPlease find attached the automated {definition.SubjectPrefix.ToLower()} report {dateRangeInfo}.\n\nThank you,\nAutomation Service";

            Logger.LogDebug($"Auto Run: Email for {definition.ReportName}: Subject='{subject}', GreetingKey='{definition.GreetingKey}' (Resolved: '{greeting.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? ""}')");
            return (subject, body);
        }
    }
}
