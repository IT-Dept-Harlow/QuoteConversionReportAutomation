// C# 10+ Features
namespace conversionTest
{
    // --- Using Statements ---
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Configuration; // For IConfiguration
    using Newtonsoft.Json; // For reading/writing appsettings
    using Newtonsoft.Json.Linq;
    using ReportWrapperCommon; // For ReportRequest/Response
    using QuoteConversionReportAutomation; // For ExcelCopyData, EmailUtility etc.

    /// <summary>
    /// Manages the automated daily report generation feature.
    /// Handles timing checks, execution logic, state persistence, and coordination
    /// with other services like process management, communication, and UI updates.
    /// Takes into account bank holidays for daily report date calculation.
    /// </summary>
    public class AutoRunManager
    {
        #region Fields and Properties

        // --- Dependencies ---
        private readonly IConfiguration _configuration;
        private readonly EmailUtility _emailUtility;
        private readonly ReportProcessManager _processManager;
        private readonly NamedPipeCommunicator _pipeCommunicator;
        private readonly UIManager _uiManager; // To disable/enable controls and update status
        private readonly ExcelCopyData _excelProcessor; // Instance of the non-static class

        // --- Configuration & State ---
        private readonly string _appSettingsPath;
        private bool _isAutoRunTaskExecuting = false;
        private DateTime _lastAutoRunDate = DateTime.MinValue;
        private bool _autoRunStatusSetForToday = false;
        private DateTime _autoRunStatusDate = DateTime.MinValue;

        // --- Constants ---
        private const int DailyReportIndex = 0; // Assuming Daily is always index 0
        private const int AutoRunCheckHour = 8; // The hour (0-23) to check for auto-run

        // --- Build Configuration Helper ---
        /// <summary>Gets a value indicating whether the application is running in DEBUG configuration.</summary>
        private static bool IsDebug =>
#if DEBUG
            true;
#else
            false;
#endif

        // --- User Profile Path ---
        /// <summary>Gets the user's profile directory path.</summary>
        private string UserProfilePath => Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);


        /// <summary>Gets the base directory where the final processed analysis Excel file will be saved, combined with user profile.</summary>
        private string ExcelFinalSaveLocation
        {
            get
            {
                // Read relative path, trim leading slashes, provide fallback
                string relativePath = _configuration["settings:ExcelFinalSaveLocation"]
                                          ?.TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                                      // Fallback relative path if config is missing
                                      ?? @"Harlow Printing\IT Projects - Documents\Dashboard Datasets\Raw_data\Quotes conversion\Estimates";
                // *** FIX: Combine with user profile path ***
                return Path.Combine(UserProfilePath, relativePath);
            }
        }

        /// <summary>Gets the Crystal Report file path from configuration.</summary>
        private string CrystalReportLocation => _configuration["settings:CrystalReportPath"] ?? string.Empty;

        /// <summary>Gets the base directory for raw report exports from configuration, combined with user profile.</summary>
        private string RawReportExportBaseDir
        {
            get
            {
                // Read relative path, trim leading slashes, provide fallback
                string relativePath = _configuration["settings:RawReportExportBaseDir"]
                                          ?.TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                                      // Fallback relative path if config is missing
                                      ?? @"Harlow Printing\IT Projects - Documents\Dashboard Datasets\Raw_data\Quotes conversion\Estimate Reports Exports";
                // *** FIX: Combine with user profile path ***
                return Path.Combine(UserProfilePath, relativePath);
            }
        }

        /// <summary>Gets the base directory for templates from configuration, combined with user profile.</summary>
        public string ExcelTemplateBaseDir
        {
            get
            {
                // Read relative path, trim leading slashes, provide fallback
                string relativePath = _configuration["settings:TemplateBaseDir"]
                                          ?.TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                                      // Fallback relative path if config is missing
                                      ?? @"Harlow Printing\IT Projects - Documents\Dashboard Datasets\Raw_data\Quotes conversion\TEMPLATE";
                // *** FIX: Combine with user profile path ***
                return Path.Combine(UserProfilePath, relativePath);
            }
        }


        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the AutoRunManager class.
        /// </summary>
        public AutoRunManager(
            IConfiguration configuration,
            EmailUtility emailUtility,
            ReportProcessManager processManager,
            NamedPipeCommunicator pipeCommunicator,
            UIManager uiManager,
            ExcelCopyData excelProcessor, // Accept the instance
            string appSettingsPath)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _emailUtility = emailUtility ?? throw new ArgumentNullException(nameof(emailUtility));
            _processManager = processManager ?? throw new ArgumentNullException(nameof(processManager));
            _pipeCommunicator = pipeCommunicator ?? throw new ArgumentNullException(nameof(pipeCommunicator));
            _uiManager = uiManager ?? throw new ArgumentNullException(nameof(uiManager));
            _excelProcessor = excelProcessor ?? throw new ArgumentNullException(nameof(excelProcessor)); // Store the instance
            _appSettingsPath = appSettingsPath ?? throw new ArgumentNullException(nameof(appSettingsPath));

            // Load the initial last run date
            ReadLastRunDate();
            _autoRunStatusDate = DateTime.Today; // Initialize status date check
        }

        #endregion

        #region Public Methods (Timer Tick Handler)

        /// <summary>
        /// Performs the daily check to see if the automated report should run.
        /// This method should be called by the Timer's Tick event handler in Form1.
        /// </summary>
        public async Task PerformDailyCheckAsync(bool isTimerCurrentlyEnabled)
        {
            // Exit immediately if timer disabled by user or a task is already running
            if (!isTimerCurrentlyEnabled || _isAutoRunTaskExecuting) return;

            DateTime now = DateTime.Now;

            // Reset the "done for today" flag if the date has changed
            if (now.Date != _autoRunStatusDate)
            {
                _autoRunStatusSetForToday = false;
                _autoRunStatusDate = now.Date;
                // Reset the status text if the date changed and timer is enabled
                _uiManager.UpdateAutoRunUI(true, false, UIManager.IsWindowsDarkModeEnabled(), "Auto Run: Enabled");
            }

            // --- Check 1: Time of Day ---
            if (now.Hour != AutoRunCheckHour) return; // Check only during the designated hour

            // --- Check 2: Already Run Today? ---
            ReadLastRunDate(); // Re-read in case settings file was modified
            if (now.Date <= _lastAutoRunDate.Date)
            {
                // Report has already run. Update status only if not already done.
                if (!_autoRunStatusSetForToday)
                {
                    Logger.LogInfo($"Auto Run: Check complete for today ({now:yyyy-MM-dd}). Report already ran on {_lastAutoRunDate:yyyy-MM-dd}.");
                    string doneMessage = $"Auto Run: Done for {now:dd/MM}";
                    _uiManager.UpdateStatusRight(doneMessage);
                    _uiManager.UpdateAutoRunUI(true, true, UIManager.IsWindowsDarkModeEnabled(), doneMessage); // Mark as final status
                    _autoRunStatusSetForToday = true;
                }
                return; // Exit event handler
            }

            // --- Prevent multiple triggers within the hour ---
            if (_isAutoRunTaskExecuting) return;

            // --- Passed Checks: Time to Run ---
            _isAutoRunTaskExecuting = true; // Set flag
            // Note: Timer stopping/starting should be handled by the caller (Form1)
            _uiManager.DisableControlsForAutoRun(); // Disable UI via UIManager
            _uiManager.UpdateStatusMain("Auto Run: Starting daily report...");
            Logger.LogInfo($"Auto Run: Triggered for today ({now:yyyy-MM-dd}) at {now:HH:mm:ss}. Last run was {_lastAutoRunDate:yyyy-MM-dd}.");

            bool success = false;
            try
            {
                // Execute the core auto-run sequence
                success = await RunAutomatedDailyReportAsync();

                string finalStatusMessage;
                if (success)
                {
                    _lastAutoRunDate = now.Date; // Update in-memory date
                    SaveLastRunDate(_lastAutoRunDate); // Save to appsettings.json
                    Logger.LogInfo("Auto Run: Daily report completed successfully.");
                    finalStatusMessage = $"Auto Run: Completed {now:dd/MM HH:mm}";
                }
                else
                {
                    Logger.LogError("Auto Run: Daily report failed. See previous logs.");
                    finalStatusMessage = $"Auto Run: FAILED {now:dd/MM HH:mm}";
                }
                _uiManager.UpdateStatusRight(finalStatusMessage);
                _uiManager.UpdateAutoRunUI(isTimerCurrentlyEnabled, true, UIManager.IsWindowsDarkModeEnabled(), finalStatusMessage); // Update with final status
                _autoRunStatusSetForToday = true; // Mark as done for today (success or fail)
            }
            catch (Exception ex)
            {
                Logger.LogCritical($"Auto Run: Unhandled exception during automated run: {ex}");
                string errorMessage = $"Auto Run: CRITICAL ERROR {now:dd/MM HH:mm}";
                _uiManager.UpdateStatusRight(errorMessage);
                _uiManager.UpdateAutoRunUI(false, true, UIManager.IsWindowsDarkModeEnabled(), errorMessage); // Show error, mark as final, reflect potentially stopped timer
                _autoRunStatusSetForToday = true; // Mark as done even if failed critically
                success = false; // Ensure success is false on critical error
                // Consider stopping the timer in Form1 if a critical error occurs here
            }
            finally
            {
                _isAutoRunTaskExecuting = false; // Clear flag

                // Re-enable controls via UIManager - Form1 needs to call this
                // Example in Form1's timer tick after awaiting PerformDailyCheckAsync:
                // _uiManager.ResetUIOnError("Create Report", configValid, ..., isTimerEnabled, ...);
                // _uiManager.UpdateStatusMain("Ready"); // Reset main status after operation

                // The decision to restart the timer is left to the caller (Form1)
                // based on the original state and the success/failure outcome.
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
            // *** MODIFIED: Use GetPreviousWorkday which now considers bank holidays ***
            DateTime reportDate = GetPreviousWorkday(DateTime.Today);
            Logger.LogInfo($"Auto Run: Calculated report date (previous workday considering bank holidays): {reportDate:yyyy-MM-dd}");


            using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(20)); // Timeout for the whole process
            var token = cts.Token;

            // Progress reporter that updates the main status label via UIManager
            IProgress<string> progress = new Progress<string>(status => _uiManager.UpdateStatusMain($"Auto Run: {status}"));
            // Progress reporter for detailed Excel steps
            IProgress<ProgressReport> excelProgress = new Progress<ProgressReport>(report => _uiManager.UpdateStatusMain($"Auto Run: {report.Message}"));

            try
            {
                // --- Step 1: Generate Raw Report ---
                progress.Report("Ensuring report service...");
                if (!await _processManager.EnsureWrapperIsRunningAsync(progress, token)) // Use injected process manager
                { throw new InvalidOperationException($"Auto Run Error: Failed to start or connect to the report service."); }

                progress.Report("Preparing request...");
                // *** FIX: GetAutomatedReportOutputPath now uses the RawReportExportBaseDir property which includes UserProfilePath ***
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
                ReportResponse? response = await _pipeCommunicator.SendRequestReceiveResponseAsync(request, progress, token); // Use injected communicator

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
                    // Log the full path attempted
                    Logger.LogError($"Auto Run Error: Report generation failed attempting to write to '{dailyOutputPath}'. Message: {errorMessage}");
                    throw new Exception($"Auto Run Error: Report generation failed: {errorMessage}");
                }

                // --- Step 2: Process Report ---
                progress.Report("Processing report...");
                // *** FIX: GetAutomatedTemplatePath now uses the ExcelTemplateBaseDir property which includes UserProfilePath ***
                string templatePath = GetAutomatedTemplatePath();
                // *** FIX: ExcelFinalSaveLocation property now includes UserProfilePath ***
                string baseSaveLocation = ExcelFinalSaveLocation;
                string currentFY = _excelProcessor.GetCurrentFinancialYear(true); // Use instance

                // Validate paths
                if (string.IsNullOrEmpty(templatePath) || !File.Exists(templatePath))
                { throw new FileNotFoundException($"Auto Run Error: Required template not found.", templatePath); }
                if (string.IsNullOrEmpty(generatedRawPath)) // Check null/empty before File.Exists
                { throw new FileNotFoundException("Auto Run Error: Raw report path is missing."); }
                if (!File.Exists(generatedRawPath))
                { throw new FileNotFoundException("Auto Run Error: Raw report to process not found.", generatedRawPath); }
                if (string.IsNullOrEmpty(baseSaveLocation))
                { throw new InvalidOperationException("Auto Run Error: Base save location not configured."); }


                // Delete existing final file for the report date if it exists
                string? expectedFinalPath = _excelProcessor.GetExpectedFinalFilePath(DailyReportIndex, baseSaveLocation, reportDate); // Use instance
                if (expectedFinalPath != null && File.Exists(expectedFinalPath))
                {
                    try { File.Delete(expectedFinalPath); Logger.LogInfo($"Auto Run: Deleted existing file: {expectedFinalPath}"); }
                    catch (Exception delEx) { Logger.LogWarning($"Auto Run: Failed to delete existing file '{expectedFinalPath}': {delEx.Message}"); /* Continue anyway */ }
                }

                // Call processing function using injected processor
                finalAnalysisPath = await _excelProcessor.ProcessExcelReportAsync( // Use instance
                    currentFY, DailyReportIndex,
                    generatedRawPath, "Sheet1", baseSaveLocation, templatePath, "DATA",
                    1, 1, excelProgress, reportDate, token
                );

                if (string.IsNullOrEmpty(finalAnalysisPath) || !File.Exists(finalAnalysisPath))
                { throw new Exception("Auto Run Error: Excel processing failed. Check logs."); }

                Logger.LogInfo($"Auto Run: Report processed for {reportDate:yyyy-MM-dd}: {finalAnalysisPath}");
                progress.Report("Report processed.");

                // --- Step 3: Email Report ---
                progress.Report("Sending email...");
                var (mailTo, mailCc) = GetAutoRunEmailRecipients(); // Use helper method
                var (subject, body) = GetEmailSubjectAndBodyForAutoRun(reportDate); // Use helper method

                // Send email using injected utility
                bool emailSuccess = await _emailUtility.SendEmailAsync(
                    mailTo, mailCc, subject, body, finalAnalysisPath, progress, token);

                if (!emailSuccess)
                { throw new Exception("Auto Run Error: Email sending failed. Check EmailUtility logs."); }

                Logger.LogInfo("Auto Run: Email sent successfully.");
                progress.Report("Email sent.");
                return true; // Overall success
            }
            catch (OperationCanceledException)
            {
                Logger.LogWarning("Auto Run: Operation cancelled.");
                progress.Report("Operation cancelled.");
                return false;
            }
            catch (Exception ex)
            {
                // Log the exception already includes the path issue from step 1 or other errors
                Logger.LogError($"Auto Run: Error during automated process: {ex}");
                progress.Report($"ERROR: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Gets the output path for the automated raw report generation, using a specific date.
        /// Ensures the directory exists using the FolderCreation utility.
        /// Uses the RawReportExportBaseDir property which combines config with UserProfilePath.
        /// </summary>
        private string GetAutomatedReportOutputPath(DateTime reportDate)
        {
            // *** FIX: Use the property that combines with user profile ***
            string baseDir = RawReportExportBaseDir;
            string fileName = $"{reportDate:yyyyMMdd}_EstimateSuccessReport_Raw.xlsx";
            string fullPath = string.Empty; // Initialize

            try
            {
                // Use FolderCreation static method
                // Use reportDate for folder structure consistency
                string? folderPath = FolderCreation.CreateReportSpecificFolder(DailyReportIndex, baseDir, reportDate);

                if (!string.IsNullOrEmpty(folderPath))
                {
                    // Construct the path using the determined folder
                    fullPath = Path.Combine(folderPath, fileName);
                    Logger.LogDebug($"Auto Run: Determined raw output path: {fullPath}"); // Log the correct path
                }
                else
                {
                    // Fallback if folder creation failed (error already logged by FolderCreation)
                    string fallbackFolder = Path.Combine(baseDir, "Daily Reports", "Fallback");
                    Directory.CreateDirectory(fallbackFolder);
                    fullPath = Path.Combine(fallbackFolder, fileName);
                    Logger.LogError($"GetAutomatedReportOutputPath: Using fallback path: {fullPath}");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Auto Run: Error determining or creating raw output directory: {ex.Message}");
                // Construct a fallback path even on error
                string fallbackFolder = Path.Combine(baseDir, "Daily Reports", "ErrorFallback");
                try { Directory.CreateDirectory(fallbackFolder); } catch { /* Ignore secondary error */ }
                fullPath = Path.Combine(fallbackFolder, fileName);
                Logger.LogError($"GetAutomatedReportOutputPath: Using ErrorFallback path: {fullPath}"); // Log error fallback path
            }
            return fullPath;
        }

        /// <summary>
        /// Gets the template path for the automated daily run (uses the weekly template).
        /// Uses the ExcelTemplateBaseDir property which combines config with UserProfilePath.
        /// </summary>
        private string GetAutomatedTemplatePath()
        {
            // *** FIX: Use the property that combines with user profile ***
            string baseDir = ExcelTemplateBaseDir;
            string templateName = "TEMPLATE_Estimate Success Rate.xlsx"; // Daily uses the weekly template
            return Path.Combine(baseDir, templateName);
        }

        /// <summary>
        /// Determines the To and CC email recipients for the automated daily run.
        /// Uses Debug recipients if in DEBUG mode, otherwise uses specific production recipients.
        /// </summary>
        private (List<string> To, List<string> Cc) GetAutoRunEmailRecipients()
        {
            List<string> mailTo;
            List<string> mailCc;

            if (IsDebug)
            {
                mailTo = GetStringListFromConfig("settings:DebugEmails:To") ?? ["chrisp@harlowsolutions.co.uk"];
                mailCc = GetStringListFromConfig("settings:DebugEmails:CC1") ?? ["itdept@harlowsolutions.co.uk"];
                Logger.LogInfo("Auto Run (DEBUG): Sending email to Debug recipients.");
            }
            else
            {
                // Specific recipients for Production Auto Run Daily report
                mailTo = GetStringListFromConfig("settings:ProductionEmails:AutoRunDailyTo") ?? ["pauls@harlowsolutions.co.uk"];
                mailCc = GetStringListFromConfig("settings:ProductionEmails:AutoRunDailyCC") ?? ["itdept@harlowsolutions.co.uk"];
                Logger.LogInfo("Auto Run (RELEASE): Sending email to configured AutoRun Daily recipients.");
            }
            return (mailTo, mailCc);
        }


        /// <summary>
        /// Generates the email subject and body specifically for the automated daily run.
        /// </summary>
        private (string Subject, string Body) GetEmailSubjectAndBodyForAutoRun(DateTime reportDate)
        {
            string reportTypeName = "Estimate Success Rate";
            string greeting = IsDebug ? "Hi Debug," : (_configuration["settings:ProductionEmails:AutoRunDailyGreeting"] ?? "Hi Paul,");
            string dateRangeInfo = $"for {reportDate:dd MMM yy}"; // Corrected format
            string subjectPrefix = $"Daily {reportTypeName}"; // Always Daily for auto-run

            string subject = $"AUTOMATED: {subjectPrefix} Report ({reportDate:yyyy-MM-dd})"; // Indicate automated run and report date
            string body = $"{greeting}\n\nPlease find attached the automated {subjectPrefix.ToLower()} report {dateRangeInfo}.\n\nThank you,\nAutomation Service";

            return (subject, body);
        }

        /// <summary>
        /// Reads the last run date from the appsettings.json file.
        /// </summary>
        private void ReadLastRunDate()
        {
            try
            {
                if (!File.Exists(_appSettingsPath))
                {
                    Logger.LogWarning($"appsettings.json not found at '{_appSettingsPath}'. Cannot read LastRunDate.");
                    _lastAutoRunDate = DateTime.MinValue;
                    return;
                }

                string jsonContent = File.ReadAllText(_appSettingsPath);
                var json = JObject.Parse(jsonContent);
                string? dateString = json?["AutoReport"]?["LastRunDate"]?.ToString();

                if (!string.IsNullOrEmpty(dateString) &&
                    DateTime.TryParseExact(dateString, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsedDate))
                {
                    _lastAutoRunDate = parsedDate.Date;
                    Logger.LogDebug($"Read LastRunDate from appsettings.json: {_lastAutoRunDate:yyyy-MM-dd}");
                }
                else
                {
                    Logger.LogInfo($"LastRunDate empty, not found, or invalid format ('{dateString}') in appsettings.json. Using default MinValue.");
                    _lastAutoRunDate = DateTime.MinValue;
                }
            }
            catch (Exception ex) when (ex is JsonReaderException || ex is IOException)
            {
                Logger.LogError($"Error reading/parsing JSON from '{_appSettingsPath}': {ex.Message}");
                _lastAutoRunDate = DateTime.MinValue;
            }
            catch (Exception ex)
            {
                Logger.LogError($"Unexpected error reading LastRunDate from '{_appSettingsPath}': {ex.Message}");
                _lastAutoRunDate = DateTime.MinValue;
            }
        }

        /// <summary>
        /// Saves the last run date to the appsettings.json file.
        /// </summary>
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

                JObject autoReportSection = json["AutoReport"] as JObject ?? new JObject();
                if (json["AutoReport"] == null) json["AutoReport"] = autoReportSection; // Add section if missing

                autoReportSection["LastRunDate"] = dateToSave.ToString("yyyy-MM-dd");

                File.WriteAllText(_appSettingsPath, json.ToString(Formatting.Indented));
                Logger.LogInfo($"Successfully saved LastRunDate ({dateToSave:yyyy-MM-dd}) to appsettings.json");
            }
            catch (Exception ex) when (ex is JsonReaderException || ex is IOException || ex is UnauthorizedAccessException)
            {
                Logger.LogError($"Error saving LastRunDate to '{_appSettingsPath}': {ex.Message}. Check permissions.");
                // Optionally notify UI via UIManager or FlexibleMessageBox if critical
            }
            catch (Exception ex)
            {
                Logger.LogError($"Unexpected error saving LastRunDate to '{_appSettingsPath}': {ex.Message}");
            }
        }

        /// <summary>
        /// Calculates the previous working day, skipping weekends and bank holidays.
        /// </summary>
        /// <param name="currentDate">The date to calculate from (usually Today).</param>
        /// <returns>The DateTime representing the previous workday.</returns>
        private static DateTime GetPreviousWorkday(DateTime currentDate)
        {
            DateTime previousDay = currentDate.AddDays(-1);

            // Loop backwards until a non-weekend, non-bank holiday is found
            while (true)
            {
                // Check for weekends
                if (previousDay.DayOfWeek == DayOfWeek.Saturday)
                {
                    previousDay = previousDay.AddDays(-1); // Move to Friday
                }
                else if (previousDay.DayOfWeek == DayOfWeek.Sunday)
                {
                    previousDay = previousDay.AddDays(-2); // Move to Friday
                }

                // Check for bank holidays (using the BankHolidayHelper)
                if (!BankHolidayHelper.IsBankHoliday(previousDay))
                {
                    // Not a weekend and not a bank holiday, so this is our workday
                    return previousDay;
                }

                // If it was a bank holiday, subtract another day and check again
                previousDay = previousDay.AddDays(-1);
            }
        }

        /// <summary>
        /// Reads a configuration value and splits it into a list of strings.
        /// </summary>
        private List<string>? GetStringListFromConfig(string key)
        {
            string? configValue = _configuration[key];
            if (string.IsNullOrWhiteSpace(configValue)) return null;
            return [.. configValue.Split([',', ';', ' '], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)];
        }

        #endregion
    }
}
