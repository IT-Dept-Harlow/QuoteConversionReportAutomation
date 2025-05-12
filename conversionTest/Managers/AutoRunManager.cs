// C# 10+ Features
namespace QuoteConversionReportAutomation.Managers
{
    using Microsoft.Extensions.Configuration; // For IConfiguration
    using Newtonsoft.Json; // For reading/writing appsettings
    using Newtonsoft.Json.Linq;
    using QuoteConversionReportAutomation.Helpers;
    using QuoteConversionReportAutomation.Services.Communication;
    using QuoteConversionReportAutomation.Services.Excel;
    using QuoteConversionReportAutomation.Services.Logging;
    // --- Using Statements ---
    using System;
    using System.Globalization;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;


    /// <summary>
    /// Manages the automated daily report generation feature.
    /// Handles timing checks (with configurable hour), execution logic, state persistence, 
    /// and coordination with other services.
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

        // --- Configuration & State ---
        private readonly string _appSettingsPath;
        private bool _isAutoRunTaskExecuting = false;
        private DateTime _lastAutoRunDate = DateTime.MinValue;
        private bool _autoRunStatusSetForToday = false;
        private DateTime _autoRunStatusDate = DateTime.MinValue;
        private int _autoRunCheckHour; // Configurable hour for the daily check

        // --- Constants ---
        private const int DailyReportIndex = 0;
        // private const int AutoRunCheckHour = 8; // Removed, now configurable via _autoRunCheckHour

        // --- Build Configuration Helper ---
        private static bool IsDebug =>
#if DEBUG
            true;
#else
            false;
#endif

        private string UserProfilePath => Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        private string ExcelFinalSaveLocation => Path.Combine(UserProfilePath, _configuration["settings:ExcelFinalSaveLocation"]?.TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) ?? @"Harlow Printing\IT Projects - Documents\Dashboard Datasets\Raw_data\Quotes conversion\Estimates");
        private string CrystalReportLocation => _configuration["settings:CrystalReportPath"] ?? string.Empty;
        private string RawReportExportBaseDir => Path.Combine(UserProfilePath, _configuration["settings:RawReportExportBaseDir"]?.TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) ?? @"Harlow Printing\IT Projects - Documents\Dashboard Datasets\Raw_data\Quotes conversion\Estimate Reports Exports");
        public string ExcelTemplateBaseDir => Path.Combine(UserProfilePath, _configuration["settings:ExcelTemplateFolder"]?.TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) ?? @"Harlow Printing\IT Projects - Documents\Dashboard Datasets\Raw_data\Quotes conversion\TEMPLATE");

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the AutoRunManager class.
        /// Now accepts initialAutoRunHour.
        /// </summary>
        public AutoRunManager(
            IConfiguration configuration,
            EmailUtility emailUtility,
            ReportProcessManager processManager,
            NamedPipeCommunicator pipeCommunicator,
            UIManager uiManager,
            ExcelCopyData excelProcessor,
            string appSettingsPath,
            EmailRecipientManager emailRecipientManager,
            int initialAutoRunHour) // Added initialAutoRunHour parameter
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _emailUtility = emailUtility ?? throw new ArgumentNullException(nameof(emailUtility));
            _processManager = processManager ?? throw new ArgumentNullException(nameof(processManager));
            _pipeCommunicator = pipeCommunicator ?? throw new ArgumentNullException(nameof(pipeCommunicator));
            _uiManager = uiManager ?? throw new ArgumentNullException(nameof(uiManager));
            _excelProcessor = excelProcessor ?? throw new ArgumentNullException(nameof(excelProcessor));
            _appSettingsPath = appSettingsPath ?? throw new ArgumentNullException(nameof(appSettingsPath));
            _emailRecipientManager = emailRecipientManager ?? throw new ArgumentNullException(nameof(emailRecipientManager));
            _autoRunCheckHour = initialAutoRunHour; // Initialize configurable hour

            ReadLastRunDate();
            _autoRunStatusDate = DateTime.Today;
            Logger.LogInfo($"AutoRunManager initialized. Auto-run check hour set to: {_autoRunCheckHour}");
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Performs the daily check to see if the automated report should run, using the configured hour.
        /// </summary>
        /// <param name="isTimerCurrentlyEnabled">Indicates if the timer in Form1 is currently enabled.</param>
        /// <param name="configuredHour">The hour (0-23) at which the check should be performed.</param>
        public async Task PerformDailyCheckAsync(bool isTimerCurrentlyEnabled, int configuredHour)
        {
            if (!isTimerCurrentlyEnabled || _isAutoRunTaskExecuting) return;

            DateTime now = DateTime.Now;
            _autoRunCheckHour = configuredHour; // Ensure internal field is up-to-date if changed externally

            if (now.Date != _autoRunStatusDate)
            {
                _autoRunStatusSetForToday = false;
                _autoRunStatusDate = now.Date;
                _uiManager.UpdateAutoRunUI(true, false, UIManager.IsWindowsDarkModeEnabled(), $"Auto Run: Enabled (Next check ~{_autoRunCheckHour}:00)");
            }

            if (now.Hour != _autoRunCheckHour) // Use the dynamically set _autoRunCheckHour
            {
                // Optional: Log that it's not the check hour yet, but can be verbose.
                // Logger.LogTrace($"Auto Run: Not the configured check hour ({_autoRunCheckHour}). Current hour: {now.Hour}.");
                return;
            }


            ReadLastRunDate(); // Ensure we have the latest from file, though unlikely to change mid-operation
            if (now.Date <= _lastAutoRunDate.Date)
            {
                if (!_autoRunStatusSetForToday)
                {
                    Logger.LogInfo($"Auto Run: Check complete for today ({now:yyyy-MM-dd}). Report already ran on {_lastAutoRunDate:yyyy-MM-dd}.");
                    string doneMessage = $"Auto Run: Done for {now:dd/MM}";
                    _uiManager.UpdateStatusRight(doneMessage);
                    _uiManager.UpdateAutoRunUI(true, true, UIManager.IsWindowsDarkModeEnabled(), doneMessage);
                    _autoRunStatusSetForToday = true;
                }
                return;
            }

            if (_isAutoRunTaskExecuting) return; // Should be redundant due to the first check, but good for safety

            _isAutoRunTaskExecuting = true;
            _uiManager.DisableControlsForAutoRun();
            _uiManager.UpdateStatusMain($"Auto Run: Starting daily report (scheduled for ~{_autoRunCheckHour}:00)...");
            Logger.LogInfo($"Auto Run: Triggered for today ({now:yyyy-MM-dd}) at {now:HH:mm:ss} (Configured Hour: {_autoRunCheckHour}). Last run was {_lastAutoRunDate:yyyy-MM-dd}.");

            bool success = false;
            try
            {
                success = await RunAutomatedDailyReportAsync();

                string finalStatusMessage;
                if (success)
                {
                    _lastAutoRunDate = now.Date; // Update in-memory last run date
                    SaveLastRunDate(_lastAutoRunDate); // Persist to appsettings.json
                    Logger.LogInfo("Auto Run: Daily report completed successfully.");
                    finalStatusMessage = $"Auto Run: Completed {now:dd/MM HH:mm}";
                }
                else
                {
                    Logger.LogError("Auto Run: Daily report failed. See previous logs.");
                    finalStatusMessage = $"Auto Run: FAILED {now:dd/MM HH:mm}";
                }
                _uiManager.UpdateStatusRight(finalStatusMessage);
                _uiManager.UpdateAutoRunUI(isTimerCurrentlyEnabled, true, UIManager.IsWindowsDarkModeEnabled(), finalStatusMessage);
                _autoRunStatusSetForToday = true;
            }
            catch (Exception ex)
            {
                Logger.LogCritical($"Auto Run: Unhandled exception during automated run: {ex.Message}", ex);
                string errorMessage = $"Auto Run: CRITICAL ERROR {now:dd/MM HH:mm}";
                _uiManager.UpdateStatusRight(errorMessage);
                // Consider if timer should be disabled via UI manager if critical error
                _uiManager.UpdateAutoRunUI(false, true, UIManager.IsWindowsDarkModeEnabled(), errorMessage);
                _autoRunStatusSetForToday = true;
                success = false; // Ensure success is false
            }
            finally
            {
                _isAutoRunTaskExecuting = false;
                // UI re-enabling and main status reset should be handled by Form1's timer tick logic
                // after this method returns, or by calling ResetUIStateOnError from Form1.
            }
        }

        /// <summary>
        /// Sets the hour for the automated daily check and saves it to appsettings.json.
        /// </summary>
        /// <param name="newHour">The new hour (0-23) for the auto-run check.</param>
        /// <returns>True if the setting was successfully saved, false otherwise.</returns>
        public async Task<bool> SetAutoRunHourAsync(int newHour)
        {
            if (newHour < 0 || newHour > 23)
            {
                Logger.LogError($"SetAutoRunHourAsync: Invalid hour provided: {newHour}. Must be between 0 and 23.");
                return false;
            }

            try
            {
                Logger.LogInfo($"SetAutoRunHourAsync: Attempting to set auto-run hour to {newHour}.");
                string jsonContent = await File.ReadAllTextAsync(_appSettingsPath);
                var json = JObject.Parse(jsonContent);

                JObject? settingsSection = json["settings"] as JObject;
                if (settingsSection == null)
                {
                    settingsSection = new JObject();
                    json["settings"] = settingsSection;
                    Logger.LogWarning("SetAutoRunHourAsync: 'settings' section not found in appsettings.json. Creating it.");
                }

                settingsSection["AutoRunCheckHour"] = newHour;

                await File.WriteAllTextAsync(_appSettingsPath, json.ToString(Formatting.Indented));
                _autoRunCheckHour = newHour; // Update internal state
                Logger.LogInfo($"Successfully saved AutoRunCheckHour ({newHour}) to appsettings.json and updated internal state.");
                return true;
            }
            catch (Exception ex) when (ex is JsonException || ex is IOException || ex is UnauthorizedAccessException)
            {
                Logger.LogError($"SetAutoRunHourAsync: Error saving AutoRunCheckHour to '{_appSettingsPath}': {ex.Message}. Check permissions or JSON format.", ex);
                return false;
            }
            catch (Exception ex)
            {
                Logger.LogError($"SetAutoRunHourAsync: Unexpected error saving AutoRunCheckHour to '{_appSettingsPath}': {ex.Message}", ex);
                return false;
            }
        }

        #endregion

        #region Core Auto Run Logic

        /// <summary>
        /// Executes the automated daily report generation, processing, and emailing sequence.
        /// Uses the previous workday for the report dates, considering bank holidays.
        /// </summary>
        private async Task<bool> RunAutomatedDailyReportAsync()
        {
            Logger.LogInfo("Auto Run: Starting automated daily report process...");
            string? generatedRawPath = null;
            string? finalAnalysisPath = null;
            DateTime reportDate = ReportHelper.GetPreviousWorkday(DateTime.Today);
            Logger.LogInfo($"Auto Run: Calculated report date (previous workday considering bank holidays): {reportDate:yyyy-MM-dd}");

            using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(20)); // Overall timeout for the auto-run task
            var token = cts.Token;

            IProgress<string> progress = new Progress<string>(status => _uiManager.UpdateStatusMain($"Auto Run: {status}"));
            IProgress<ProgressReport> excelProgress = new Progress<ProgressReport>(report => _uiManager.UpdateStatusMain($"Auto Run: {report.Message}"));

            try
            {
                progress.Report("Ensuring report service...");
                if (!await _processManager.EnsureWrapperIsRunningAsync(progress, token))
                { throw new InvalidOperationException($"Auto Run Error: Failed to start or connect to the report service."); }

                progress.Report("Preparing request...");
                string dailyOutputPath = GetAutomatedReportOutputPath(reportDate);
                string crystalReportPath = CrystalReportLocation;
                if (string.IsNullOrEmpty(crystalReportPath) || !File.Exists(crystalReportPath))
                { throw new FileNotFoundException("Auto Run Error: Crystal Report file path is invalid or missing.", crystalReportPath); }

                var request = new ReportRequest
                {
                    CrystalReportLocation = crystalReportPath,
                    ReportOutputLocation = dailyOutputPath,
                    ReportDateFrom = reportDate,
                    ReportDateTo = reportDate
                };

                progress.Report("Requesting report...");
                ReportResponse? response = await _pipeCommunicator.SendRequestReceiveResponseAsync(request, progress, token);

                if (response?.Success == true && !string.IsNullOrEmpty(response.OutputPath) && File.Exists(response.OutputPath))
                {
                    generatedRawPath = response.OutputPath;
                    Logger.LogInfo($"Auto Run: Raw report generated for {reportDate:yyyy-MM-dd}: {generatedRawPath}");
                    progress.Report("Raw report created.");
                }
                else
                {
                    string errorMessage = response?.ErrorMessage ?? "Unknown error from report service.";
                    if (response?.Success == true && (string.IsNullOrEmpty(response.OutputPath) || !File.Exists(response.OutputPath)))
                    { errorMessage = $"Auto Run Error: Report service success, but output file invalid/missing ('{response?.OutputPath ?? "NULL"}')."; }
                    Logger.LogError($"Auto Run Error: Report generation failed attempting to write to '{dailyOutputPath}'. Message: {errorMessage}");
                    throw new Exception($"Auto Run Error: Report generation failed: {errorMessage}");
                }

                progress.Report("Processing report...");
                string templatePath = GetAutomatedTemplatePath();
                string baseSaveLocation = ExcelFinalSaveLocation;
                string currentFY = _excelProcessor.GetCurrentFinancialYear(true);

                if (string.IsNullOrEmpty(templatePath) || !File.Exists(templatePath))
                { throw new FileNotFoundException($"Auto Run Error: Required template not found.", templatePath); }
                if (string.IsNullOrEmpty(generatedRawPath)) // Should have been caught by previous check, but defensive
                { throw new FileNotFoundException("Auto Run Error: Raw report path is missing for processing."); }
                if (!File.Exists(generatedRawPath)) // Double check file existence
                { throw new FileNotFoundException("Auto Run Error: Raw report to process not found at path.", generatedRawPath); }
                if (string.IsNullOrEmpty(baseSaveLocation)) // Check configuration
                { throw new InvalidOperationException("Auto Run Error: Base save location for final Excel file is not configured."); }

                string? expectedFinalPath = _excelProcessor.GetExpectedFinalFilePath(DailyReportIndex, baseSaveLocation, reportDate);
                if (expectedFinalPath != null && File.Exists(expectedFinalPath))
                {
                    try
                    {
                        File.Delete(expectedFinalPath);
                        Logger.LogInfo($"Auto Run: Deleted existing final file before regeneration: {expectedFinalPath}");
                    }
                    catch (Exception delEx)
                    {
                        // Log as warning, processing will likely overwrite or fail if file is locked.
                        Logger.LogWarning($"Auto Run: Failed to delete existing final file '{expectedFinalPath}': {delEx.Message}. Processing will attempt to continue.");
                    }
                }

                finalAnalysisPath = await _excelProcessor.ProcessExcelReportAsync(
                    currentFY, DailyReportIndex,
                    generatedRawPath, "Sheet1", baseSaveLocation, templatePath, "DATA",
                    1, 1, excelProgress, reportDate, token
                );

                if (string.IsNullOrEmpty(finalAnalysisPath) || !File.Exists(finalAnalysisPath))
                {
                    if (token.IsCancellationRequested) throw new OperationCanceledException("Auto Run: Excel processing was cancelled.");
                    throw new Exception("Auto Run Error: Excel processing failed to produce a final file. Check logs for details.");
                }

                Logger.LogInfo($"Auto Run: Report processed for {reportDate:yyyy-MM-dd}: {finalAnalysisPath}");
                progress.Report("Report processed.");

                progress.Report("Sending email...");
                var (mailTo, mailCc) = _emailRecipientManager.GetRecipients(DailyReportIndex, false, IsDebug);
                var (subject, body) = GetEmailSubjectAndBodyForAutoRun(reportDate);

                bool emailSuccess = await _emailUtility.SendEmailAsync(
                    mailTo, mailCc, subject, body, finalAnalysisPath, progress, token);

                if (!emailSuccess)
                {
                    if (token.IsCancellationRequested) throw new OperationCanceledException("Auto Run: Email sending was cancelled.");
                    throw new Exception("Auto Run Error: Email sending failed. Check EmailUtility logs for details.");
                }

                Logger.LogInfo("Auto Run: Email sent successfully.");
                progress.Report("Email sent.");
                return true; // Success
            }
            catch (OperationCanceledException)
            {
                Logger.LogWarning("Auto Run: Operation cancelled (timeout or explicit cancellation).");
                progress.Report("Operation cancelled.");
                return false;
            }
            catch (Exception ex)
            {
                Logger.LogError($"Auto Run: Error during automated process: {ex.Message}", ex);
                progress.Report($"ERROR: {ex.Message.Split('.')[0]}..."); // Show a concise error part
                return false;
            }
        }

        #endregion

        #region Helper Methods

        private string GetAutomatedReportOutputPath(DateTime reportDate)
        {
            string baseDir = RawReportExportBaseDir;
            string fileName = $"{reportDate:yyyyMMdd}_EstimateSuccessReport_Raw.xlsx";
            string fullPath = string.Empty;

            try
            {
                string? folderPath = FolderCreation.CreateReportSpecificFolder(DailyReportIndex, baseDir, reportDate);
                if (!string.IsNullOrEmpty(folderPath))
                {
                    fullPath = Path.Combine(folderPath, fileName);
                    Logger.LogDebug($"Auto Run: Determined raw output path: {fullPath}");
                }
                else // Fallback if specific folder creation fails (should ideally not happen if permissions are correct)
                {
                    string fallbackFolder = Path.Combine(baseDir, "Daily Reports", "Fallback_AutoRun"); // More specific fallback
                    Directory.CreateDirectory(fallbackFolder); // Ensure it exists
                    fullPath = Path.Combine(fallbackFolder, fileName);
                    Logger.LogError($"GetAutomatedReportOutputPath: Could not get/create specific folder. Using fallback path: {fullPath}");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Auto Run: Error determining or creating raw output directory: {ex.Message}", ex);
                // Critical fallback if even the primary fallback logic fails
                string errorFallbackFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "QuoteConversionAutomation_ErrorFallback_Raw");
                try { Directory.CreateDirectory(errorFallbackFolder); } catch { /* Best effort */ }
                fullPath = Path.Combine(errorFallbackFolder, fileName);
                Logger.LogError($"GetAutomatedReportOutputPath: Using CRITICAL ErrorFallback path: {fullPath}");
            }
            return fullPath;
        }

        private string GetAutomatedTemplatePath()
        {
            string baseDir = ExcelTemplateBaseDir;
            // Assuming Daily reports always use the standard template.
            string templateName = "TEMPLATE_Estimate Success Rate.xlsx";
            return Path.Combine(baseDir, templateName);
        }


        private (string Subject, string Body) GetEmailSubjectAndBodyForAutoRun(DateTime reportDate)
        {
            string reportTypeName = "Estimate Success Rate";
            // Use configuration for greeting, fallback if not present
            string greeting = IsDebug ? "Hi Debug," : (_configuration["settings:ProductionEmails:AutoRunDailyGreeting"] ?? "Hi Paul,");
            string dateRangeInfo = $"for {reportDate:dd MMM yy}";
            string subjectPrefix = $"Daily {reportTypeName}"; // Keep it specific to Daily

            string subject = $"AUTOMATED: {subjectPrefix} Report ({reportDate:yyyy-MM-dd})";
            string body = $"{greeting}\n\nPlease find attached the automated {subjectPrefix.ToLower()} report {dateRangeInfo}.\n\nThank you,\nAutomation Service";

            return (subject, body);
        }

        private void ReadLastRunDate()
        {
            try
            {
                if (!File.Exists(_appSettingsPath))
                {
                    Logger.LogWarning($"appsettings.json not found at '{_appSettingsPath}'. Cannot read LastRunDate. Defaulting to MinValue.");
                    _lastAutoRunDate = DateTime.MinValue;
                    return;
                }

                string jsonContent = File.ReadAllText(_appSettingsPath);
                var json = JObject.Parse(jsonContent);
                string? dateString = json?["AutoReport"]?["LastRunDate"]?.ToString();

                if (!string.IsNullOrEmpty(dateString) &&
                    DateTime.TryParseExact(dateString, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsedDate))
                {
                    _lastAutoRunDate = parsedDate.Date; // Use .Date to ensure only date part is considered
                    Logger.LogDebug($"Read LastRunDate from appsettings.json: {_lastAutoRunDate:yyyy-MM-dd}");
                }
                else
                {
                    Logger.LogInfo($"LastRunDate empty, not found, or invalid format ('{dateString}') in appsettings.json. Using default MinValue.");
                    _lastAutoRunDate = DateTime.MinValue;
                }
            }
            catch (Exception ex) when (ex is JsonException || ex is IOException) // More specific catch for JSON/IO issues
            {
                Logger.LogError($"Error reading/parsing JSON for LastRunDate from '{_appSettingsPath}': {ex.Message}", ex);
                _lastAutoRunDate = DateTime.MinValue; // Default on error
            }
            catch (Exception ex) // General catch for other unexpected errors
            {
                Logger.LogError($"Unexpected error reading LastRunDate from '{_appSettingsPath}': {ex.Message}", ex);
                _lastAutoRunDate = DateTime.MinValue; // Default on error
            }
        }

        private void SaveLastRunDate(DateTime dateToSave)
        {
            try
            {
                if (!File.Exists(_appSettingsPath))
                {
                    Logger.LogError($"appsettings.json not found at '{_appSettingsPath}'. Cannot save LastRunDate.");
                    return;
                }

                string jsonContent = File.ReadAllText(_appSettingsPath);
                var json = JObject.Parse(jsonContent);

                JObject? autoReportSection = json["AutoReport"] as JObject;
                if (autoReportSection == null)
                {
                    autoReportSection = new JObject();
                    json["AutoReport"] = autoReportSection;
                    Logger.LogWarning("SaveLastRunDate: 'AutoReport' section not found in appsettings.json. Creating it.");
                }

                autoReportSection["LastRunDate"] = dateToSave.ToString("yyyy-MM-dd");

                File.WriteAllText(_appSettingsPath, json.ToString(Formatting.Indented));
                Logger.LogInfo($"Successfully saved LastRunDate ({dateToSave:yyyy-MM-dd}) to appsettings.json");
            }
            catch (Exception ex) when (ex is JsonException || ex is IOException || ex is UnauthorizedAccessException)
            {
                Logger.LogError($"Error saving LastRunDate to '{_appSettingsPath}': {ex.Message}. Check permissions or JSON format.", ex);
            }
            catch (Exception ex)
            {
                Logger.LogError($"Unexpected error saving LastRunDate to '{_appSettingsPath}': {ex.Message}", ex);
            }
        }
        #endregion
    }
}
