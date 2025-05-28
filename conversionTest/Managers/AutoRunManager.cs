// AutoRunManager.cs
// Manages the automated execution of predefined reports based on a schedule
// and configuration settings. It co-ordinates report generation, processing,
// and emailing for these automated tasks.
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

// Project specific namespaces
using QuoteConversionReportAutomation.Helpers; // For ReportHelper, FolderCreation.
using QuoteConversionReportAutomation.Models;   // For DailyReportRunStatus, AutoReportDefinition.
using QuoteConversionReportAutomation.Services.Communication; // For EmailUtility, NamedPipeCommunicator.
using QuoteConversionReportAutomation.Services.Excel; // For ExcelCopyData.
using QuoteConversionReportAutomation.Services.Logging; // For Logger.
#endregion

namespace QuoteConversionReportAutomation.Managers
{
    /// <summary>
    /// Enum to represent the outcome of an automated run check.
    /// </summary>
    public enum AutoRunActionResult
    {
        /// <summary>Indicates no action was needed or taken during the check.</summary>
        NoActionNeeded,
        /// <summary>Indicates that at least one automated report processing was attempted.</summary>
        ActionAttempted,
        /// <summary>Indicates a critical error occurred during the auto-run process.</summary>
        CriticalError
    }

    /// <summary>
    /// Manages the automated (scheduled) generation and processing of reports.
    /// It checks daily at a configured hour if any predefined reports are due,
    /// then orchestrates their creation, processing, and emailing.
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
        private readonly string _appSettingsPath; // Full path to appsettings.json for updating run statuses.

        // --- State Variables ---
        private static readonly object _jsonFileLock = new object(); // Lock for thread-safe access to appsettings.json.
        private bool _isAutoRunTaskExecuting = false; // Flag to prevent concurrent auto-run executions.
        private DateTime _lastGlobalSuccessDate = DateTime.MinValue; // Tracks the last date all due reports succeeded.
        private int _autoRunCheckHour; // The configured hour (0-23) for daily auto-run checks.

        // --- Report Definitions ---
        private readonly List<AutoReportDefinition> _reportDefinitions; // Loaded from configuration.

        // --- JSON Keys ---
        // Constants for keys used in appsettings.json.
        private const string JsonSectionSettings = "settings";
        private const string JsonSectionAutoReport = "AutoReport";
        private const string JsonKeyDailyRunStatus = "DailyRunStatus";
        private const string JsonKeyStatusDate = "StatusDate";
        private const string JsonKeyLastRunDate = "LastRunDate"; // Tracks overall success for a day.
        private const string JsonKeyAutoRunCheckHour = "AutoRunCheckHour";

        // --- Build Configuration ---
        /// <summary>Gets a value indicating whether the application is running in DEBUG mode.</summary>
        private static bool IsDebug =>
#if DEBUG
            true;
#else
            false;
#endif

        // --- Convenience Path Properties (derived from configuration) ---
        private string UserProfilePath => Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        private string ExcelFinalSaveLocation => Path.Combine(UserProfilePath, _configuration[$"{JsonSectionSettings}:ExcelFinalSaveLocation"]?.TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) ?? @"Harlow Printing\IT Projects - Documents\Dashboard Datasets\Raw_data\Quotes conversion\Estimates");
        private string CrystalReportLocation => _configuration[$"{JsonSectionSettings}:CrystalReportPath"] ?? string.Empty;
        private string RawReportExportBaseDir => Path.Combine(UserProfilePath, _configuration[$"{JsonSectionSettings}:RawReportExportBaseDir"]?.TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) ?? @"Harlow Printing\IT Projects - Documents\Dashboard Datasets\Raw_data\Quotes conversion\Estimate Reports Exports");
        public string ExcelTemplateBaseDir => Path.Combine(UserProfilePath, _configuration[$"{JsonSectionSettings}:ExcelTemplateFolder"]?.TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) ?? @"Harlow Printing\IT Projects - Documents\Dashboard Datasets\Raw_data\Quotes conversion\TEMPLATE");

        #endregion

        #region Constructor
        /// <summary>
        /// Initialises a new instance of the <see cref="AutoRunManager"/> class.
        /// </summary>
        /// <param name="configuration">The application configuration.</param>
        /// <param name="emailUtility">Utility for sending emails.</param>
        /// <param name="processManager">Manager for the Crystal Report wrapper process.</param>
        /// <param name="pipeCommunicator">Communicator for IPC with the wrapper.</param>
        /// <param name="uiManager">Manager for UI updates.</param>
        /// <param name="excelProcessor">Service for Excel processing.</param>
        /// <param name="appSettingsPath">Full path to the appsettings.json file.</param>
        /// <param name="emailRecipientManager">Manager for email recipients.</param>
        /// <param name="greetingManager">Manager for email greetings.</param>
        /// <param name="initialAutoRunHour">The initial configured hour for auto-run checks.</param>
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

            // Load report definitions from the "AutoReport:ReportDefinitions" section of appsettings.json.
            _reportDefinitions = _configuration.GetSection($"{JsonSectionAutoReport}:ReportDefinitions").Get<List<AutoReportDefinition>>() ?? new List<AutoReportDefinition>();
            if (!_reportDefinitions.Any(d => d != null)) // Check if there are any non-null definitions.
            {
                Logger.LogWarning("AutoRunManager: No valid report definitions found in configuration. Auto-run will not process any reports.");
            }
            else
            {
                Logger.LogInfo($"AutoRunManager: Loaded {_reportDefinitions.Count(d => d != null)} valid report definitions.");
                foreach (var def in _reportDefinitions.Where(d => d != null))
                {
                    Logger.LogDebug($"Loaded Definition: Name='{def.ReportName}', TypeIndex={def.ReportTypeIndex}, EnableKey='{def.EnableConfigKey}', SuccessFlag='{def.SuccessFlagJsonName}', GreetingKey='{def.GreetingKey}', RecipientCategoryKey='{def.RecipientCategoryKey ?? "N/A"}'");
                }
            }

            _lastGlobalSuccessDate = ReadLastGlobalSuccessDate(); // Read the last date all reports were successfully run.
            Logger.LogInfo($"AutoRunManager initialised. Auto-run check hour: {_autoRunCheckHour}. Last Global Success Date: {_lastGlobalSuccessDate:yyyy-MM-dd}");
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Performs the daily check to see if any automated reports are due and processes them.
        /// This method is typically called by a timer.
        /// </summary>
        /// <param name="isTimerCurrentlyEnabled">Indicates if the master auto-run timer is currently enabled by the user.</param>
        /// <param name="configuredHour">The hour (0-23) configured for the auto-run check.</param>
        /// <returns>An <see cref="AutoRunActionResult"/> indicating the outcome of the check.</returns>
        public async Task<AutoRunActionResult> PerformDailyCheckAsync(bool isTimerCurrentlyEnabled, int configuredHour)
        {
            // Do not proceed if the main auto-run timer is disabled by the user, or if no valid report definitions are loaded.
            if (!isTimerCurrentlyEnabled || !_reportDefinitions.Any(d => d != null))
            {
                if (!_reportDefinitions.Any(d => d != null)) Logger.LogInfo("Auto Run: No valid report definitions loaded. Skipping check.");
                else Logger.LogInfo("Auto Run: Timer is disabled by user. Skipping check.");
                return AutoRunActionResult.NoActionNeeded;
            }

            // Prevent concurrent execution of the auto-run task.
            if (_isAutoRunTaskExecuting)
            {
                Logger.LogInfo("Auto Run: Task is already executing. Skipping this check cycle.");
                return AutoRunActionResult.NoActionNeeded;
            }

            DateTime now = DateTime.Now;
            _autoRunCheckHour = configuredHour; // Update with the potentially changed configured hour.
            AutoRunActionResult overallResult = AutoRunActionResult.NoActionNeeded; // Default outcome.

            // Read the persisted status of daily report runs.
            DailyReportRunStatus currentDayStatuses = ReadDailyReportStatuses();

            // If the persisted status date is not today, reset all statuses for the new day.
            if (currentDayStatuses.StatusDate != now.ToString("yyyy-MM-dd"))
            {
                Logger.LogInfo($"Persisted StatusDate ({currentDayStatuses.StatusDate}) is not for today ({now:yyyy-MM-dd}). Resetting daily report statuses for today.");
                ResetDailyReportStatuses(now.Date);
                currentDayStatuses = ReadDailyReportStatuses(); // Re-read after reset.
                // Update UI to reflect that checks are pending for the new day.
                _uiManager.UpdateAutoRunUI(true, false, UIManager.IsWindowsDarkModeEnabled(), $"Auto Run: Enabled (Next check ~{_autoRunCheckHour}:00)");
            }

            // Only proceed if the current hour matches the configured auto-run check hour.
            if (now.Hour != _autoRunCheckHour)
            {
                Logger.LogDebug($"Auto Run: Not the configured hour ({_autoRunCheckHour}). Current hour: {now.Hour}. Skipping report execution.");
                return AutoRunActionResult.NoActionNeeded;
            }

            // Check if all reports that are enabled AND due to run today have already succeeded.
            bool allDueReportsAlreadySucceeded = currentDayStatuses.AllCurrentlyEnabledAndDueReportsSucceeded(_configuration, _reportDefinitions, now.DayOfWeek);
            int totalEnabledAndDueTodayForInitialCheck = _reportDefinitions.Count(def =>
                def != null &&
                _configuration.GetValue<bool>($"{JsonSectionAutoReport}:{def.EnableConfigKey}", false) && // Is enabled in config?
                (!def.RunOnDayOfWeek.HasValue || def.RunOnDayOfWeek.Value == now.DayOfWeek) // Is it due today?
            );

            // If there were reports due today and all of them have already succeeded, no further action is needed.
            if (allDueReportsAlreadySucceeded && totalEnabledAndDueTodayForInitialCheck > 0)
            {
                Logger.LogInfo($"Auto Run: All enabled AND DUE reports already succeeded for today ({now:yyyy-MM-dd}). No further action needed at this hour.");
                _uiManager.UpdateStatusRight($"Auto Run: Done for {now:dd/MM}");
                _uiManager.UpdateAutoRunUI(true, true, UIManager.IsWindowsDarkModeEnabled(), $"Auto Run: Done for {now:dd/MM}");
                return AutoRunActionResult.NoActionNeeded;
            }

            // If we reach here, it's either the correct hour and some reports might be pending,
            // or no reports were due today (in which case we still log this).
            _isAutoRunTaskExecuting = true; // Set flag to indicate task is starting.
            overallResult = AutoRunActionResult.ActionAttempted; // Assume an action will be attempted.
            _uiManager.DisableControlsForAutoRun(); // Disable relevant UI controls on the main form.
            _uiManager.UpdateStatusMain($"Auto Run: Starting checks for {now:dd-MM-yyyy} (scheduled ~{_autoRunCheckHour}:00)...");
            Logger.LogInfo($"Auto Run: Triggered for {now:yyyy-MM-dd} at {now:HH:mm:ss}. Persisted StatusDate: {currentDayStatuses.StatusDate}");

            bool anyReportActuallyAttemptedThisCycle = false; // Track if any report processing is initiated in this cycle.

            try
            {
                // Iterate through each defined automated report.
                foreach (var definition in _reportDefinitions.Where(d => d != null)) // Ensure definition object itself is not null.
                {
                    // Check if this specific report definition is enabled in the configuration.
                    bool isEnabled = _configuration.GetValue<bool>($"{JsonSectionAutoReport}:{definition.EnableConfigKey}", false);
                    if (!isEnabled)
                    {
                        Logger.LogInfo($"Auto Run: Report '{definition.ReportName}' (Key: {definition.EnableConfigKey}) is DISABLED. Skipping.");
                        continue; // Skip this report if it's not enabled.
                    }

                    // If the report is configured to run only on a specific day of the week, check if today is that day.
                    if (definition.RunOnDayOfWeek.HasValue && now.DayOfWeek != definition.RunOnDayOfWeek.Value)
                    {
                        Logger.LogInfo($"Auto Run: Report '{definition.ReportName}' is configured to run on {definition.RunOnDayOfWeek.Value}, but today is {now.DayOfWeek}. Skipping.");
                        continue; // Skip if not the correct day of the week.
                    }

                    currentDayStatuses = ReadDailyReportStatuses(); // Refresh status before checking this specific report.
                    // Check if this report has already succeeded today.
                    if (currentDayStatuses.GetReportSuccessStatus(definition.SuccessFlagJsonName))
                    {
                        Logger.LogInfo($"Auto Run: Report '{definition.ReportName}' (Flag: {definition.SuccessFlagJsonName}) already succeeded today. Skipping.");
                        continue; // Skip if already successfully run today.
                    }

                    anyReportActuallyAttemptedThisCycle = true; // A report will be attempted.
                    _uiManager.UpdateStatusMain($"Auto Run: Processing {definition.ReportName}...");
                    Logger.LogInfo($"Auto Run: Report '{definition.ReportName}' is ENABLED and PENDING. Attempting to run.");

                    // Determine the report's date range.
                    DateTime reportEndDate;
                    DateTime? reportStartDate = null;

                    if (definition.ReportName == "Weekly Estimate Success Rate") // Specific logic for weekly report.
                    {
                        reportEndDate = now.Date; // Ends today.
                        reportStartDate = reportEndDate.AddDays(-14); // Covers the last 15 days.
                        Logger.LogDebug($"Weekly Report Dates: Start={reportStartDate:yyyy-MM-dd}, End={reportEndDate:yyyy-MM-dd}");
                    }
                    else if (definition.ReportEndDateOffsetDays.HasValue) // For reports defined by date offsets.
                    {
                        reportEndDate = ReportHelper.GetNthPreviousWorkday(now.Date, definition.ReportEndDateOffsetDays.Value);
                        if (definition.ReportDurationDays.HasValue && definition.ReportDurationDays.Value > 1)
                        {
                            reportStartDate = ReportHelper.GetNthPreviousWorkday(reportEndDate, definition.ReportDurationDays.Value - 1);
                        }
                        else
                        {
                            reportStartDate = reportEndDate; // Single day report.
                        }
                        Logger.LogDebug($"Offset-based Report '{definition.ReportName}' Dates: Start={reportStartDate:yyyy-MM-dd}, End={reportEndDate:yyyy-MM-dd}");
                    }
                    else // Fallback date calculation if no specific offsets are defined.
                    {
                        reportEndDate = ReportHelper.GetPreviousWorkday(now.Date);
                        reportStartDate = reportEndDate;
                        Logger.LogWarning($"Auto Run: Report '{definition.ReportName}' has no specific date offset or duration defined. Defaulting to previous workday ({reportEndDate:yyyy-MM-dd}).");
                    }

                    // Run the configured report.
                    await RunConfiguredAutomatedReportAsync(definition, reportEndDate, reportStartDate, now.Date);
                }

                // After attempting all due reports, update the overall status.
                if (!anyReportActuallyAttemptedThisCycle && totalEnabledAndDueTodayForInitialCheck > 0 && allDueReportsAlreadySucceeded)
                {
                    // This case implies all due reports were already done before this cycle started.
                    overallResult = AutoRunActionResult.NoActionNeeded;
                }
                else if (!anyReportActuallyAttemptedThisCycle && totalEnabledAndDueTodayForInitialCheck == 0)
                {
                    // No reports were due to run today.
                    overallResult = AutoRunActionResult.NoActionNeeded;
                    Logger.LogInfo("Auto Run: No reports were due for execution in this cycle.");
                }

                // Final status update based on the outcomes of this cycle.
                currentDayStatuses = ReadDailyReportStatuses(); // Re-read statuses after processing.
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

                if (totalEnabledAndDueReportsToday == 0) // No reports were scheduled to run today.
                {
                    int totalAnyEnabledReports = _reportDefinitions.Count(def => def != null && _configuration.GetValue<bool>($"{JsonSectionAutoReport}:{def.EnableConfigKey}", false));
                    finalStatusMessage = totalAnyEnabledReports == 0 ? $"Auto Run: No reports enabled {now:dd/MM HH:mm}"
                                                                  : $"Auto Run: No reports due today {now:dd/MM HH:mm}";
                    Logger.LogInfo(finalStatusMessage);
                    if (!anyReportActuallyAttemptedThisCycle) overallResult = AutoRunActionResult.NoActionNeeded;
                }
                else if (allEnabledAndDueReportsSucceededToday) // All due reports for today completed successfully.
                {
                    SaveLastGlobalSuccessDate(now.Date); // Mark the day as globally successful.
                    _lastGlobalSuccessDate = now.Date;
                    finalStatusMessage = $"Auto Run: All due reports DONE ({totalSucceededAmongEnabledAndDueToday}/{totalEnabledAndDueReportsToday}) for {now:dd/MM HH:mm}";
                    Logger.LogInfo(finalStatusMessage);
                }
                else // Some reports may have failed or are still pending.
                {
                    finalStatusMessage = $"Auto Run: Partial success ({totalSucceededAmongEnabledAndDueToday}/{totalEnabledAndDueReportsToday} due reports succeeded) {now:dd/MM HH:mm}";
                    Logger.LogWarning(finalStatusMessage + ". Will retry incomplete reports if app restarts or at next check hour if within the same day.");
                }
                _uiManager.UpdateStatusRight(finalStatusMessage);
                _uiManager.UpdateAutoRunUI(isTimerCurrentlyEnabled, allEnabledAndDueReportsSucceededToday && totalEnabledAndDueReportsToday > 0, UIManager.IsWindowsDarkModeEnabled(), finalStatusMessage);
            }
            catch (Exception ex) // Catch any unhandled exceptions during the main auto-run loop.
            {
                Logger.LogCritical($"Auto Run: Unhandled exception during PerformDailyCheckAsync: {ex.Message}", ex);
                string errorMsg = $"Auto Run: CRITICAL ERROR {now:dd/MM HH:mm}";
                _uiManager.UpdateStatusRight(errorMsg);
                _uiManager.UpdateAutoRunUI(isTimerCurrentlyEnabled, true, UIManager.IsWindowsDarkModeEnabled(), errorMsg); // Mark as final status (error) for today.
                overallResult = AutoRunActionResult.CriticalError;
            }
            finally
            {
                _isAutoRunTaskExecuting = false; // Clear the execution flag.
                // Refine overallResult if no specific action was taken but it was initially set to ActionAttempted.
                if (overallResult == AutoRunActionResult.ActionAttempted && !anyReportActuallyAttemptedThisCycle && totalEnabledAndDueTodayForInitialCheck > 0 && allDueReportsAlreadySucceeded)
                {
                    overallResult = AutoRunActionResult.NoActionNeeded;
                }
            }
            return overallResult;
        }

        /// <summary>
        /// Executes a single configured automated report.
        /// </summary>
        /// <param name="definition">The definition of the report to run.</param>
        /// <param name="reportEndDate">The calculated end date for the report.</param>
        /// <param name="reportStartDate">The calculated start date for the report (can be same as end date).</param>
        /// <param name="processingDate">The current date, used for status tracking.</param>
        /// <returns>True if the report was processed and emailed successfully, false otherwise.</returns>
        private async Task<bool> RunConfiguredAutomatedReportAsync(AutoReportDefinition definition, DateTime reportEndDate, DateTime? reportStartDate, DateTime processingDate)
        {
            DateTime effectiveReportStartDate = reportStartDate ?? reportEndDate; // Use end date if start date is not specified.
            Logger.LogInfo($"Auto Run: Executing: {definition.ReportName} for date {reportEndDate:yyyy-MM-dd} (Start: {effectiveReportStartDate:yyyy-MM-dd}, Processing Date: {processingDate:yyyy-MM-dd})");

            bool success = false; // Assume failure initially.
            using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(15)); // Timeout for this specific report run.
            var token = cts.Token;

            // Local progress reporters for logging within this specific report run.
            IProgress<string> localProgress = new Progress<string>(status => Logger.LogDebug($"AutoRun ({definition.ReportName}): {status}"));
            IProgress<ProgressReport> localExcelProgress = new Progress<ProgressReport>(report => Logger.LogDebug($"AutoRun ({definition.ReportName}) Excel: {report.Message}"));

            string? generatedRawPath = null;
            string? finalAnalysisPath = null;

            try
            {
                // 1. Ensure Crystal Report Wrapper service is running.
                _uiManager.UpdateProgress($"Auto Run ({definition.ReportName}): Ensuring report service...");
                if (!await _processManager.EnsureWrapperIsRunningAsync(localProgress, token))
                { throw new InvalidOperationException($"Auto Run Error ({definition.ReportName}): Failed to start or connect to the report service."); }

                // 2. Prepare and send request to generate raw report.
                _uiManager.UpdateProgress($"Auto Run ({definition.ReportName}): Preparing request...");
                string outputPath = GetAutomatedReportOutputPath(definition.ReportTypeIndex, reportEndDate, definition.ReportName); // Get structured output path.

                string crystalReportPath = CrystalReportLocation; // Get global Crystal Report path.
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
                else // Handle raw report generation failure.
                {
                    string errorMessage = response?.ErrorMessage ?? "Unknown error from report service.";
                    if (response?.Success == true && (string.IsNullOrEmpty(response.OutputPath) || !File.Exists(response.OutputPath)))
                    { errorMessage = $"Auto Run Error ({definition.ReportName}): Report service success, but output file invalid/missing ('{response?.OutputPath ?? "NULL"}')."; }
                    Logger.LogError($"Auto Run Error ({definition.ReportName}): Report generation failed for '{outputPath}'. Message: {errorMessage}");
                    throw new Exception($"Auto Run Error ({definition.ReportName}): Report generation failed: {errorMessage}");
                }

                // 3. Process the raw report into a final analysis file.
                _uiManager.UpdateProgress($"Auto Run ({definition.ReportName}): Processing report...");
                string templatePath = Path.Combine(ExcelTemplateBaseDir, definition.TemplateName); // Get full template path.
                string baseSaveLocation = ExcelFinalSaveLocation; // Base directory for final reports.
                string currentFY = _excelProcessor.GetCurrentFinancialYear(true); // Get current financial year string.

                if (string.IsNullOrEmpty(templatePath) || !File.Exists(templatePath))
                { throw new FileNotFoundException($"Auto Run Error ({definition.ReportName}): Template '{definition.TemplateName}' not found at '{templatePath}'.", templatePath); }

                // Check for and delete existing final file for this period to ensure a fresh run.
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
                    "Sheet1", // Source sheet name in raw report.
                    baseSaveLocation,
                    templatePath,
                    "DATA",   // Destination sheet name in template for raw data.
                    1, 1,     // Start row/col for copying.
                    localExcelProgress,
                    reportEndDate, // Date for filename and internal logic.
                    token);

                if (string.IsNullOrEmpty(finalAnalysisPath) || !File.Exists(finalAnalysisPath))
                {
                    if (token.IsCancellationRequested) throw new OperationCanceledException($"Auto Run ({definition.ReportName}): Excel processing cancelled.");
                    throw new Exception($"Auto Run Error ({definition.ReportName}): Excel processing failed. Check logs.");
                }
                Logger.LogInfo($"Auto Run ({definition.ReportName}): Report processed: {finalAnalysisPath}");
                _uiManager.UpdateProgress($"Auto Run ({definition.ReportName}): Report processed.");

                // 4. Send the completion email.
                _uiManager.UpdateProgress($"Auto Run ({definition.ReportName}): Sending email...");
                // Get recipients using the definition (which includes RecipientCategoryKey).
                var (mailTo, mailCc) = _emailRecipientManager.GetRecipients(
                                            definition.ReportTypeIndex, // reportTypeIndex might still be useful for EmailRecipientManager's internal logic or logging.
                                            isFemiOnlyChecked: false,    // "Femi Only" is not applicable for auto-runs.
                                            IsDebug,
                                            isAutoRunContext: true,
                                            definition: definition);     // Pass the full definition.

                var (subject, body) = GetEmailSubjectAndBodyForAutoRun(definition, effectiveReportStartDate, reportEndDate);

                bool emailSuccess = await _emailUtility.SendEmailAsync(mailTo, mailCc, subject, body, finalAnalysisPath, localProgress, token);
                if (!emailSuccess)
                {
                    if (token.IsCancellationRequested) throw new OperationCanceledException($"Auto Run ({definition.ReportName}): Email sending cancelled.");
                    throw new Exception($"Auto Run Error ({definition.ReportName}): Email sending failed. Check logs.");
                }
                Logger.LogInfo($"Auto Run ({definition.ReportName}): Email sent successfully for {definition.ReportName}.");
                _uiManager.UpdateProgress($"Auto Run ({definition.ReportName}): Email sent.");
                success = true; // Mark as successful if all steps complete.
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
                // Update the persisted status for this specific report for today.
                SaveDailyReportStatus(definition.SuccessFlagJsonName, success, processingDate);
            }
            return success;
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
        /// Reads the daily report run statuses from appsettings.json.
        /// </summary>
        private DailyReportRunStatus ReadDailyReportStatuses()
        {
            try
            {
                if (!File.Exists(_appSettingsPath))
                {
                    Logger.LogWarning("appsettings.json not found for ReadDailyReportStatuses. Returning new status object with MinValue date.");
                    return new DailyReportRunStatus { StatusDate = DateTime.MinValue.ToString("yyyy-MM-dd") };
                }

                string jsonContent;
                lock (_jsonFileLock) { jsonContent = File.ReadAllText(_appSettingsPath); }
                var jsonRoot = JObject.Parse(jsonContent);
                JToken? autoReportToken = jsonRoot[JsonSectionAutoReport];
                JToken? statusToken = autoReportToken?[JsonKeyDailyRunStatus];

                if (statusToken != null)
                {
                    // Use JsonSerializer that can handle JsonExtensionData for dynamic properties.
                    var status = statusToken.ToObject<DailyReportRunStatus>(JsonSerializer.CreateDefault(new JsonSerializerSettings
                    {
                        NullValueHandling = NullValueHandling.Ignore
                    }));

                    if (status == null)
                    {
                        Logger.LogWarning("DailyRunStatus section is null after parsing. Returning default status with MinValue date.");
                        return new DailyReportRunStatus { StatusDate = DateTime.MinValue.ToString("yyyy-MM-dd") };
                    }
                    status.StatusDate ??= DateTime.MinValue.ToString("yyyy-MM-dd"); // Ensure StatusDate is not null.
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
            return new DailyReportRunStatus { StatusDate = DateTime.MinValue.ToString("yyyy-MM-dd") }; // Fallback.
        }

        /// <summary>
        /// Saves the success status of a specific automated report for a given date to appsettings.json.
        /// </summary>
        private void SaveDailyReportStatus(string successFlagJsonName, bool success, DateTime statusDate)
        {
            lock (_jsonFileLock) // Ensure thread-safe file access.
            {
                try
                {
                    string todayDateString = statusDate.ToString("yyyy-MM-dd");
                    string jsonContent = File.Exists(_appSettingsPath) ? File.ReadAllText(_appSettingsPath) : "{}"; // Read or start with empty JSON.
                    var jsonRoot = JObject.Parse(jsonContent);

                    JObject autoReportSection = GetOrAddSection(jsonRoot, JsonSectionAutoReport);
                    JObject dailyStatusJson = GetOrAddSection(autoReportSection, JsonKeyDailyRunStatus, logCreation: false);

                    // If the date has changed or the section is new/empty, initialise all known report flags for the new day.
                    if (dailyStatusJson[JsonKeyStatusDate]?.ToString() != todayDateString || !dailyStatusJson.HasValues || dailyStatusJson[JsonKeyStatusDate] == null)
                    {
                        dailyStatusJson.RemoveAll(); // Clear old statuses if any.
                        dailyStatusJson[JsonKeyStatusDate] = todayDateString;
                        // Initialise all defined report flags to false for the new day.
                        foreach (var def in _reportDefinitions.Where(d => d != null && !string.IsNullOrEmpty(d.SuccessFlagJsonName)))
                        {
                            dailyStatusJson[def.SuccessFlagJsonName] = false;
                        }
                        Logger.LogInfo($"DailyRunStatus in JSON was for a different date or newly created/empty. Initialised for {todayDateString} with all defined report flags set to false.");
                    }

                    // Set the specific report's success status.
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

        /// <summary>
        /// Resets all daily report success statuses in appsettings.json for a given date, setting them to false.
        /// </summary>
        private void ResetDailyReportStatuses(DateTime forDate)
        {
            lock (_jsonFileLock)
            {
                try
                {
                    string jsonContent = File.Exists(_appSettingsPath) ? File.ReadAllText(_appSettingsPath) : "{}";
                    var jsonRoot = JObject.Parse(jsonContent);

                    JObject autoReportSection = GetOrAddSection(jsonRoot, JsonSectionAutoReport);
                    JObject newStatusJson = new JObject { [JsonKeyStatusDate] = forDate.ToString("yyyy-MM-dd") };

                    // Set all defined report success flags to false for the specified date.
                    foreach (var definition in _reportDefinitions.Where(d => d != null && !string.IsNullOrEmpty(d.SuccessFlagJsonName)))
                    {
                        newStatusJson[definition.SuccessFlagJsonName] = false;
                    }

                    autoReportSection[JsonKeyDailyRunStatus] = newStatusJson; // Replace the old status object.

                    File.WriteAllText(_appSettingsPath, jsonRoot.ToString(Formatting.Indented));
                    Logger.LogInfo($"Reset DailyReportStatuses in JSON for date {forDate:yyyy-MM-dd}. All defined report success flags set to false.");
                }
                catch (Exception ex)
                {
                    Logger.LogError($"Error resetting DailyRunStatuses in appsettings.json: {ex.Message}", ex);
                }
            }
        }

        /// <summary>
        /// Reads the last date on which all due automated reports were successfully completed.
        /// </summary>
        private DateTime ReadLastGlobalSuccessDate()
        {
            try
            {
                if (!File.Exists(_appSettingsPath)) return DateTime.MinValue;
                string jsonContent;
                lock (_jsonFileLock) { jsonContent = File.ReadAllText(_appSettingsPath); }
                var json = JObject.Parse(jsonContent);
                string? dateString = json?[JsonSectionAutoReport]?[JsonKeyLastRunDate]?.ToString();
                if (DateTime.TryParseExact(dateString, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsedDate))
                {
                    return parsedDate.Date;
                }
            }
            catch (Exception ex) { Logger.LogError($"Error reading LastGlobalSuccessDate ('{JsonKeyLastRunDate}' from JSON): {ex.Message}", ex); }
            return DateTime.MinValue; // Return MinValue if not found or error.
        }

        /// <summary>
        /// Saves the date on which all due automated reports were successfully completed.
        /// </summary>
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

        /// <summary>
        /// Sets the configured hour for daily auto-run checks and saves it to appsettings.json.
        /// </summary>
        /// <param name="newHour">The new hour (0-23) for auto-run checks.</param>
        /// <returns>True if the setting was successfully saved; otherwise, false.</returns>
        public async Task<bool> SetAutoRunHourAsync(int newHour)
        {
            if (newHour < 0 || newHour > 23) // Validate hour range.
            {
                Logger.LogError($"SetAutoRunHourAsync: Invalid hour provided: {newHour}. Must be between 0 and 23.");
                return false;
            }
            _autoRunCheckHour = newHour; // Update in-memory value.
            Logger.LogInfo($"SetAutoRunHourAsync: Attempting to set auto-run hour to {newHour}. Internal state updated.");

            try
            {
                string jsonContent;
                lock (_jsonFileLock) { jsonContent = File.ReadAllText(_appSettingsPath); }
                var json = JObject.Parse(jsonContent);
                JObject settingsSection = GetOrAddSection(json, JsonSectionSettings); // Ensure "settings" section exists.
                settingsSection[JsonKeyAutoRunCheckHour] = newHour; // Update or add the AutoRunCheckHour.
                lock (_jsonFileLock) { File.WriteAllTextAsync(_appSettingsPath, json.ToString(Formatting.Indented)); }
                Logger.LogInfo($"Successfully saved '{JsonKeyAutoRunCheckHour}' ({newHour}) to appsettings.json.");
                return true;
            }
            catch (Exception ex)
            {
                Logger.LogError($"SetAutoRunHourAsync: Error saving '{JsonKeyAutoRunCheckHour}': {ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// Generates the full output path for a raw automated report file.
        /// </summary>
        private string GetAutomatedReportOutputPath(int reportTypeIndex, DateTime reportDate, string reportName)
        {
            string baseDir = RawReportExportBaseDir;
            // Sanitise report name for use in a filename.
            string sanitizedReportName = string.Join("_", reportName.Split(Path.GetInvalidFileNameChars())).Replace(" ", "_");
            string fileName = $"{reportDate:yyyyMMdd}_{sanitizedReportName}_Raw_AutoType{reportTypeIndex}.xlsx";

            string fullPath;
            try
            {
                // Get the specific folder path (e.g., Year/Month/Week) for this report type and date.
                string? folderPath = FolderCreation.GetReportSpecificFolderPath(reportTypeIndex, baseDir, reportDate);
                if (!string.IsNullOrEmpty(folderPath))
                {
                    fullPath = Path.Combine(folderPath, fileName);
                }
                else // Fallback if specific folder path couldn't be determined.
                {
                    string fallbackFolder = Path.Combine(baseDir, $"AutoRun_Fallback_{sanitizedReportName}_Type{reportTypeIndex}");
                    Directory.CreateDirectory(fallbackFolder); // Ensure fallback directory exists.
                    fullPath = Path.Combine(fallbackFolder, fileName);
                    Logger.LogWarning($"GetAutomatedReportOutputPath: Using fallback folder for Report '{reportName}': {fullPath}");
                }
            }
            catch (Exception ex) // Catch critical errors during path determination.
            {
                Logger.LogError($"Auto Run: Critical error determining raw output directory for Report '{reportName}': {ex.Message}", ex);
                // Use a very basic fallback path in case of severe errors.
                string errorFallbackFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), $"QuoteConversion_ErrorFallback_Raw_{sanitizedReportName}_AutoType{reportTypeIndex}");
                try { Directory.CreateDirectory(errorFallbackFolder); } catch { /* Best effort for error fallback. */ }
                fullPath = Path.Combine(errorFallbackFolder, fileName);
                Logger.LogError($"GetAutomatedReportOutputPath: Using CRITICAL ErrorFallback path for Report '{reportName}': {fullPath}");
            }
            return fullPath;
        }

        /// <summary>
        /// Constructs the email subject and body for an automated report.
        /// </summary>
        private (string Subject, string Body) GetEmailSubjectAndBodyForAutoRun(AutoReportDefinition definition, DateTime reportStartDate, DateTime reportEndDate)
        {
            string greeting;
            // Use debug greeting if in debug mode, otherwise use the greeting key from the definition.
            if (IsDebug)
            {
                greeting = _greetingManager.GetGreeting("DebugDefault", isForDebugSection: true);
            }
            else
            {
                greeting = _greetingManager.GetGreeting(definition.GreetingKey);
            }

            // Ensure greeting ends with a comma if not empty.
            if (!string.IsNullOrWhiteSpace(greeting) && !greeting.TrimEnd().EndsWith(","))
            {
                greeting = greeting.TrimEnd() + ",";
            }

            // Format date range information for the email.
            string dateRangeInfo = (reportStartDate.Date == reportEndDate.Date) ?
                                   $"for {reportEndDate:dd MMM yy}" :
                                   $"for period {reportStartDate:dd MMM yy} to {reportEndDate:dd MMM yy}";
            // Specific override for weekly report's date range display if needed.
            if (definition.ReportName == "Weekly Estimate Success Rate")
            {
                dateRangeInfo = $"for period {reportStartDate:dd MMM yy} to {reportEndDate:dd MMM yy}";
            }

            // Format date suffix for the email subject.
            string subjectDateSuffix = (reportStartDate.Date == reportEndDate.Date) ?
                                       $"({reportEndDate:yyyy-MM-dd})" :
                                       $"({reportStartDate:yyyy-MM-dd} to {reportEndDate:yyyy-MM-dd})";
            if (definition.ReportName == "Weekly Estimate Success Rate") // Specific subject date format for weekly.
            {
                subjectDateSuffix = $"({reportStartDate:yyyy-MM-dd} to {reportEndDate:yyyy-MM-dd})";
            }

            // Construct the full subject and body.
            string subject = $"AUTOMATED: {definition.SubjectPrefix} Report {subjectDateSuffix}";
            string body = $"{greeting}\n\nPlease find attached the automated {definition.SubjectPrefix.ToLower()} report {dateRangeInfo}.\n\nThank you,\nAutomation Service";

            Logger.LogDebug($"Auto Run: Email for {definition.ReportName}: Subject='{subject}', GreetingKey='{definition.GreetingKey}' (Resolved: '{greeting.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? ""}')");
            return (subject, body);
        }
        #endregion
    }
}
