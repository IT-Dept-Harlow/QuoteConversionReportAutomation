// C# 10+ Features
namespace QuoteConversionReportAutomation.Managers
{
    using Microsoft.Extensions.Configuration; // For IConfiguration
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using QuoteConversionReportAutomation.Helpers;
    using QuoteConversionReportAutomation.Models; // For DailyReportRunStatus, UserGreetingSettings
    using QuoteConversionReportAutomation.Services.Communication;
    using QuoteConversionReportAutomation.Services.Excel;
    using QuoteConversionReportAutomation.Services.Logging;
    using System;
    using System.Globalization;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;

    public class AutoRunManager
    {
        #region Fields and Properties
        private readonly IConfiguration _configuration; // Still used for non-greeting settings
        private readonly EmailUtility _emailUtility;
        private readonly ReportProcessManager _processManager;
        private readonly NamedPipeCommunicator _pipeCommunicator;
        private readonly UIManager _uiManager;
        private readonly ExcelCopyData _excelProcessor;
        private readonly EmailRecipientManager _emailRecipientManager;
        private readonly GreetingManager _greetingManager; // Added GreetingManager instance
        private readonly string _appSettingsPath;
        private bool _isAutoRunTaskExecuting = false;
        private DateTime _lastGlobalSuccessDate = DateTime.MinValue;
        private DateTime _currentProcessingDate = DateTime.MinValue;

        private int _autoRunCheckHour;

        // Constants for Report Types (ensure these align with Form1 and ExcelCopyData)
        private const int DailyReportIndex = 0;
        private const int NewDailyReportOver1kIndex = 1;

        // Greeting key names (used with GreetingManager)
        private const string GreetingKeyAutoRunDaily = "AutoRunDaily";
        private const string GreetingKeyAutoRunDaily5Day1k = "AutoRunDaily5Day1k";
        private const string GreetingKeyDebugDefault = "DebugDefault";


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

        public AutoRunManager(
            IConfiguration configuration,
            EmailUtility emailUtility,
            ReportProcessManager processManager,
            NamedPipeCommunicator pipeCommunicator,
            UIManager uiManager,
            ExcelCopyData excelProcessor,
            string appSettingsPath,
            EmailRecipientManager emailRecipientManager,
            GreetingManager greetingManager, // Added GreetingManager parameter
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
            _greetingManager = greetingManager ?? throw new ArgumentNullException(nameof(greetingManager)); // Store GreetingManager
            _autoRunCheckHour = initialAutoRunHour;

            _lastGlobalSuccessDate = ReadLastGlobalSuccessDate();
            Logger.LogInfo($"AutoRunManager initialized. Auto-run check hour: {_autoRunCheckHour}. Last Global Success Date: {_lastGlobalSuccessDate:yyyy-MM-dd}");
        }

        public async Task PerformDailyCheckAsync(bool isTimerCurrentlyEnabled, int configuredHour)
        {
            if (!isTimerCurrentlyEnabled || _isAutoRunTaskExecuting) return;

            DateTime now = DateTime.Now;
            _autoRunCheckHour = configuredHour;

            DailyReportRunStatus currentDayStatuses = ReadDailyReportStatuses();
            if (now.Date != _currentProcessingDate.Date || currentDayStatuses.StatusDate != now.ToString("yyyy-MM-dd"))
            {
                Logger.LogInfo($"New day ({now:yyyy-MM-dd}) or status date mismatch (Stored: {currentDayStatuses.StatusDate}). Resetting daily report statuses for today.");
                ResetDailyReportStatuses(now.Date);
                _currentProcessingDate = now.Date;
                currentDayStatuses = ReadDailyReportStatuses();
                _uiManager.UpdateAutoRunUI(true, false, UIManager.IsWindowsDarkModeEnabled(), $"Auto Run: Enabled (Next check ~{_autoRunCheckHour}:00)");
            }

            if (now.Hour != _autoRunCheckHour) return;

            if (_lastGlobalSuccessDate.Date == now.Date)
            {
                Logger.LogInfo($"Auto Run: All enabled reports already globally succeeded for today ({now:yyyy-MM-dd}). No further action needed at this hour.");
                _uiManager.UpdateStatusRight($"Auto Run: Done for {now:dd/MM}");
                _uiManager.UpdateAutoRunUI(true, true, UIManager.IsWindowsDarkModeEnabled(), $"Auto Run: Done for {now:dd/MM}");
                return;
            }

            _isAutoRunTaskExecuting = true;
            _uiManager.DisableControlsForAutoRun();
            _uiManager.UpdateStatusMain($"Auto Run: Starting checks for {now:dd-MM-yyyy} (scheduled ~{_autoRunCheckHour}:00)...");
            Logger.LogInfo($"Auto Run: Triggered for {now:yyyy-MM-dd} at {now:HH:mm:ss}. Last Global Success: {_lastGlobalSuccessDate:yyyy-MM-dd}.");

            bool standardDailyEnabled = _configuration.GetValue<bool>("AutoReport:EnableStandardDailyAutoReport", true);
            bool daily5Day1kEnabled = _configuration.GetValue<bool>("AutoReport:EnableDaily5Day1kAutoReport", true);

            try
            {
                currentDayStatuses = ReadDailyReportStatuses();

                if (standardDailyEnabled)
                {
                    if (currentDayStatuses.StandardDailyReportSucceeded)
                    {
                        Logger.LogInfo("Auto Run: Standard Daily Report already succeeded today. Skipping.");
                    }
                    else
                    {
                        _uiManager.UpdateStatusMain($"Auto Run: Processing Standard Daily Report...");
                        Logger.LogInfo("Auto Run: Standard Daily Report is ENABLED and PENDING. Attempting to run.");
                        await RunSpecificAutomatedReportAsync(DailyReportIndex, ReportHelper.GetPreviousWorkday(now.Date), null, now.Date);
                    }
                }
                else Logger.LogInfo("Auto Run: Standard Daily Report is DISABLED.");

                if (daily5Day1kEnabled)
                {
                    currentDayStatuses = ReadDailyReportStatuses();
                    if (currentDayStatuses.Daily5Day1kReportSucceeded)
                    {
                        Logger.LogInfo("Auto Run: Daily (5days >= £1000) Report already succeeded today. Skipping.");
                    }
                    else
                    {
                        _uiManager.UpdateStatusMain($"Auto Run: Processing Daily (5days >= £1000) Report...");
                        Logger.LogInfo("Auto Run: Daily (5days >= £1000) Report is ENABLED and PENDING. Attempting to run.");
                        DateTime dateTo = ReportHelper.GetPreviousWorkday(now.Date);
                        DateTime dateFrom = ReportHelper.GetNthPreviousWorkday(dateTo, 4);
                        await RunSpecificAutomatedReportAsync(NewDailyReportOver1kIndex, dateTo, dateFrom, now.Date);
                    }
                }
                else Logger.LogInfo("Auto Run: Daily (5days >= £1000) Report is DISABLED.");

                currentDayStatuses = ReadDailyReportStatuses();
                bool allEnabledAndTrackedReportsSucceededToday = currentDayStatuses.AllCurrentlyEnabledReportsSucceeded(_configuration);

                string finalStatusMessage;
                if (allEnabledAndTrackedReportsSucceededToday)
                {
                    SaveLastGlobalSuccessDate(now.Date);
                    _lastGlobalSuccessDate = now.Date;
                    finalStatusMessage = $"Auto Run: All enabled reports DONE for {now:dd/MM HH:mm}";
                    Logger.LogInfo(finalStatusMessage);
                }
                else
                {
                    int totalEnabled = (standardDailyEnabled ? 1 : 0) + (daily5Day1kEnabled ? 1 : 0);
                    int totalSucceededToday = (currentDayStatuses.StandardDailyReportSucceeded ? 1 : 0) + (currentDayStatuses.Daily5Day1kReportSucceeded ? 1 : 0);
                    finalStatusMessage = $"Auto Run: Partial success ({totalSucceededToday}/{totalEnabled} enabled reports succeeded) {now:dd/MM HH:mm}";
                    Logger.LogWarning(finalStatusMessage + ". Will retry incomplete reports if app restarts or at next check hour if within the same day.");
                }
                _uiManager.UpdateStatusRight(finalStatusMessage);
                _uiManager.UpdateAutoRunUI(isTimerCurrentlyEnabled, allEnabledAndTrackedReportsSucceededToday, UIManager.IsWindowsDarkModeEnabled(), finalStatusMessage);
            }
            catch (Exception ex)
            {
                Logger.LogCritical($"Auto Run: Unhandled exception during PerformDailyCheckAsync: {ex.Message}", ex);
                string errorMsg = $"Auto Run: CRITICAL ERROR {now:dd/MM HH:mm}";
                _uiManager.UpdateStatusRight(errorMsg);
                _uiManager.UpdateAutoRunUI(false, true, UIManager.IsWindowsDarkModeEnabled(), errorMsg);
            }
            finally
            {
                _isAutoRunTaskExecuting = false;
            }
        }

        private async Task<bool> RunSpecificAutomatedReportAsync(int reportTypeIndex, DateTime reportEndDate, DateTime? reportStartDate, DateTime processingDate)
        {
            DateTime effectiveReportStartDate = reportStartDate ?? reportEndDate;
            string reportTypeNameForLog = reportTypeIndex == DailyReportIndex ? "Standard Daily" : "Daily (5days >= £1000)";
            Logger.LogInfo($"Auto Run: Executing: {reportTypeNameForLog} for date {reportEndDate:yyyy-MM-dd}");

            bool success = false;
            using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(15));
            var token = cts.Token;
            IProgress<string> progress = new Progress<string>(status => _uiManager.UpdateStatusMain($"Auto Run ({reportTypeNameForLog}): {status}"));
            IProgress<ProgressReport> excelProgress = new Progress<ProgressReport>(report => _uiManager.UpdateStatusMain($"Auto Run ({reportTypeNameForLog}): {report.Message}"));

            string? generatedRawPath = null;
            string? finalAnalysisPath = null;

            try
            {
                progress.Report("Ensuring report service...");
                if (!await _processManager.EnsureWrapperIsRunningAsync(progress, token))
                { throw new InvalidOperationException($"Auto Run Error ({reportTypeNameForLog}): Failed to start or connect to the report service."); }

                progress.Report("Preparing request...");
                string outputPath = GetAutomatedReportOutputPath(reportTypeIndex, reportEndDate);
                string crystalReportPath = CrystalReportLocation;
                if (string.IsNullOrEmpty(crystalReportPath) || !File.Exists(crystalReportPath))
                { throw new FileNotFoundException($"Auto Run Error ({reportTypeNameForLog}): Crystal Report file path is invalid or missing.", crystalReportPath); }

                var request = new ReportRequest
                {
                    CrystalReportLocation = crystalReportPath,
                    ReportOutputLocation = outputPath,
                    ReportDateFrom = effectiveReportStartDate,
                    ReportDateTo = reportEndDate
                };

                progress.Report("Requesting raw report...");
                ReportResponse? response = await _pipeCommunicator.SendRequestReceiveResponseAsync(request, progress, token);

                if (response?.Success == true && !string.IsNullOrEmpty(response.OutputPath) && File.Exists(response.OutputPath))
                {
                    generatedRawPath = response.OutputPath;
                    Logger.LogInfo($"Auto Run ({reportTypeNameForLog}): Raw report generated: {generatedRawPath}");
                    progress.Report("Raw report created.");
                }
                else
                {
                    string errorMessage = response?.ErrorMessage ?? "Unknown error from report service.";
                    if (response?.Success == true && (string.IsNullOrEmpty(response.OutputPath) || !File.Exists(response.OutputPath)))
                    { errorMessage = $"Auto Run Error ({reportTypeNameForLog}): Report service success, but output file invalid/missing ('{response?.OutputPath ?? "NULL"}')."; }
                    Logger.LogError($"Auto Run Error ({reportTypeNameForLog}): Report generation failed for '{outputPath}'. Message: {errorMessage}");
                    throw new Exception($"Auto Run Error ({reportTypeNameForLog}): Report generation failed: {errorMessage}");
                }

                progress.Report("Processing report...");
                string templatePath = GetAutomatedTemplatePath(reportTypeIndex);
                string baseSaveLocation = ExcelFinalSaveLocation;
                string currentFY = _excelProcessor.GetCurrentFinancialYear(true);

                if (string.IsNullOrEmpty(templatePath) || !File.Exists(templatePath))
                { throw new FileNotFoundException($"Auto Run Error ({reportTypeNameForLog}): Required template not found.", templatePath); }

                string? expectedFinalPath = _excelProcessor.GetExpectedFinalFilePath(reportTypeIndex, baseSaveLocation, reportEndDate);
                if (expectedFinalPath != null && File.Exists(expectedFinalPath))
                {
                    try { File.Delete(expectedFinalPath); Logger.LogInfo($"Auto Run ({reportTypeNameForLog}): Deleted existing final file: {expectedFinalPath}"); }
                    catch (Exception delEx) { Logger.LogWarning($"Auto Run ({reportTypeNameForLog}): Failed to delete existing file '{expectedFinalPath}': {delEx.Message}."); }
                }

                finalAnalysisPath = await _excelProcessor.ProcessExcelReportAsync(currentFY, reportTypeIndex, generatedRawPath, "Sheet1", baseSaveLocation, templatePath, "DATA", 1, 1, excelProgress, reportEndDate, token);

                if (string.IsNullOrEmpty(finalAnalysisPath) || !File.Exists(finalAnalysisPath))
                {
                    if (token.IsCancellationRequested) throw new OperationCanceledException($"Auto Run ({reportTypeNameForLog}): Excel processing cancelled.");
                    throw new Exception($"Auto Run Error ({reportTypeNameForLog}): Excel processing failed. Check logs.");
                }
                Logger.LogInfo($"Auto Run ({reportTypeNameForLog}): Report processed: {finalAnalysisPath}");
                progress.Report("Report processed.");

                progress.Report("Sending email...");
                var (mailTo, mailCc) = _emailRecipientManager.GetRecipients(reportTypeIndex, isFemiOnlyChecked: false, IsDebug, isAutoRunContext: true);
                var (subject, body) = GetEmailSubjectAndBodyForAutoRun(reportTypeIndex, effectiveReportStartDate, reportEndDate);

                bool emailSuccess = await _emailUtility.SendEmailAsync(mailTo, mailCc, subject, body, finalAnalysisPath, progress, token);
                if (!emailSuccess)
                {
                    if (token.IsCancellationRequested) throw new OperationCanceledException($"Auto Run ({reportTypeNameForLog}): Email sending cancelled.");
                    throw new Exception($"Auto Run Error ({reportTypeNameForLog}): Email sending failed. Check logs.");
                }
                Logger.LogInfo($"Auto Run ({reportTypeNameForLog}): Email sent successfully for {reportTypeNameForLog}.");
                progress.Report("Email sent.");
                success = true;
            }
            catch (OperationCanceledException)
            {
                Logger.LogWarning($"Auto Run ({reportTypeNameForLog}): Operation cancelled.");
                progress.Report("Operation cancelled.");
                success = false;
            }
            catch (Exception ex)
            {
                Logger.LogError($"Auto Run ({reportTypeNameForLog}): Error: {ex.Message}", ex);
                progress.Report($"ERROR: {ex.Message.Split('.')[0]}...");
                success = false;
            }
            finally
            {
                SaveDailyReportStatus(reportTypeIndex, success, processingDate);
            }
            return success;
        }

        private DailyReportRunStatus ReadDailyReportStatuses()
        {
            try
            {
                if (!File.Exists(_appSettingsPath))
                {
                    Logger.LogWarning("appsettings.json not found for ReadDailyReportStatuses. Returning new status object.");
                    return new DailyReportRunStatus { StatusDate = DateTime.MinValue.ToString("yyyy-MM-dd") };
                }

                string jsonContent = File.ReadAllText(_appSettingsPath);
                var json = JObject.Parse(jsonContent);
                JToken? statusToken = json?["AutoReport"]?["DailyRunStatus"];

                if (statusToken != null)
                {
                    var status = statusToken.ToObject<DailyReportRunStatus>(JsonSerializer.CreateDefault(new JsonSerializerSettings
                    {
                        NullValueHandling = NullValueHandling.Ignore
                    }));

                    if (status == null)
                    {
                        Logger.LogWarning("DailyRunStatus section is null after parsing. Returning default status.");
                        return new DailyReportRunStatus { StatusDate = DateTime.MinValue.ToString("yyyy-MM-dd") };
                    }
                    status.StatusDate ??= DateTime.MinValue.ToString("yyyy-MM-dd");
                    return status;
                }
                Logger.LogWarning("AutoReport:DailyRunStatus section not found in appsettings.json. Returning default status object.");
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error reading DailyRunStatus from appsettings.json: {ex.Message}", ex);
            }
            return new DailyReportRunStatus { StatusDate = DateTime.MinValue.ToString("yyyy-MM-dd") };
        }

        private void SaveDailyReportStatus(int reportTypeIndex, bool success, DateTime statusDate)
        {
            lock (_appSettingsPath)
            {
                try
                {
                    if (!File.Exists(_appSettingsPath))
                    {
                        Logger.LogError($"appsettings.json not found at '{_appSettingsPath}'. Cannot save daily report status.");
                        return;
                    }
                    string jsonContent = File.ReadAllText(_appSettingsPath);
                    var json = JObject.Parse(jsonContent);

                    JObject? autoReportSection = json["AutoReport"] as JObject;
                    if (autoReportSection == null) { autoReportSection = new JObject(); json["AutoReport"] = autoReportSection; }

                    JObject? dailyStatusJson = autoReportSection["DailyRunStatus"] as JObject;
                    if (dailyStatusJson == null || dailyStatusJson["StatusDate"]?.ToString() != statusDate.ToString("yyyy-MM-dd"))
                    {
                        dailyStatusJson = JObject.FromObject(new DailyReportRunStatus { StatusDate = statusDate.ToString("yyyy-MM-dd") });
                        autoReportSection["DailyRunStatus"] = dailyStatusJson;
                        Logger.LogInfo($"DailyRunStatus for {statusDate:yyyy-MM-dd} was missing or for a different date. Initialized.");
                    }

                    if (reportTypeIndex == DailyReportIndex)
                        dailyStatusJson["StandardDailyReportSucceeded"] = success;
                    else if (reportTypeIndex == NewDailyReportOver1kIndex)
                        dailyStatusJson["Daily5Day1kReportSucceeded"] = success;

                    File.WriteAllText(_appSettingsPath, json.ToString(Formatting.Indented));
                    Logger.LogInfo($"Saved DailyRunStatus for ReportType {reportTypeIndex} ({(reportTypeIndex == DailyReportIndex ? "StandardDaily" : "Daily5Day1k")}): Success={success}, Date={statusDate:yyyy-MM-dd}");
                }
                catch (Exception ex)
                {
                    Logger.LogError($"Error saving DailyRunStatus to appsettings.json: {ex.Message}", ex);
                }
            }
        }

        private void ResetDailyReportStatuses(DateTime forDate)
        {
            lock (_appSettingsPath)
            {
                try
                {
                    string jsonContent = File.Exists(_appSettingsPath) ? File.ReadAllText(_appSettingsPath) : "{}";
                    var json = JObject.Parse(jsonContent);

                    JObject? autoReportSection = json["AutoReport"] as JObject;
                    if (autoReportSection == null) { autoReportSection = new JObject(); json["AutoReport"] = autoReportSection; }

                    var newStatus = new DailyReportRunStatus
                    {
                        StatusDate = forDate.ToString("yyyy-MM-dd"),
                        StandardDailyReportSucceeded = false,
                        Daily5Day1kReportSucceeded = false
                    };
                    autoReportSection["DailyRunStatus"] = JObject.FromObject(newStatus);

                    File.WriteAllText(_appSettingsPath, json.ToString(Formatting.Indented));
                    Logger.LogInfo($"Reset DailyReportStatuses for date {forDate:yyyy-MM-dd}.");
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
                string? dateString = json?["AutoReport"]?["LastRunDate"]?.ToString();
                if (DateTime.TryParseExact(dateString, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsedDate))
                {
                    return parsedDate.Date;
                }
            }
            catch (Exception ex) { Logger.LogError($"Error reading LastGlobalSuccessDate (LastRunDate from JSON): {ex.Message}", ex); }
            return DateTime.MinValue;
        }

        private void SaveLastGlobalSuccessDate(DateTime dateToSave)
        {
            lock (_appSettingsPath)
            {
                try
                {
                    if (!File.Exists(_appSettingsPath)) { Logger.LogError("appsettings.json not found. Cannot save LastGlobalSuccessDate."); return; }
                    string jsonContent = File.ReadAllText(_appSettingsPath);
                    var json = JObject.Parse(jsonContent);
                    JObject? autoReportSection = json["AutoReport"] as JObject;
                    if (autoReportSection == null) { autoReportSection = new JObject(); json["AutoReport"] = autoReportSection; }
                    autoReportSection["LastRunDate"] = dateToSave.ToString("yyyy-MM-dd");
                    File.WriteAllText(_appSettingsPath, json.ToString(Formatting.Indented));
                    Logger.LogInfo($"Successfully saved LastGlobalSuccessDate (as LastRunDate in JSON): {dateToSave:yyyy-MM-dd}");
                }
                catch (Exception ex) { Logger.LogError($"Error saving LastGlobalSuccessDate (as LastRunDate in JSON): {ex.Message}", ex); }
            }
        }

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
                if (settingsSection == null) { settingsSection = new JObject(); json["settings"] = settingsSection; }
                settingsSection["AutoRunCheckHour"] = newHour;
                await File.WriteAllTextAsync(_appSettingsPath, json.ToString(Formatting.Indented));
                _autoRunCheckHour = newHour;
                Logger.LogInfo($"Successfully saved AutoRunCheckHour ({newHour}) to appsettings.json and updated internal state.");
                return true;
            }
            catch (Exception ex) { Logger.LogError($"SetAutoRunHourAsync: Error saving AutoRunCheckHour: {ex.Message}", ex); return false; }
        }

        private string GetAutomatedReportOutputPath(int reportTypeIndex, DateTime reportDate)
        {
            string baseDir = RawReportExportBaseDir;
            string fileName = $"{reportDate:yyyyMMdd}_EstimateSuccessReport_Raw.xlsx";
            string fullPath = string.Empty;
            try
            {
                string? folderPath = FolderCreation.CreateReportSpecificFolder(reportTypeIndex, baseDir, reportDate);
                if (!string.IsNullOrEmpty(folderPath)) { fullPath = Path.Combine(folderPath, fileName); }
                else
                {
                    string fallbackFolder = Path.Combine(baseDir, $"ReportType_{reportTypeIndex}_Fallback_AutoRun");
                    Directory.CreateDirectory(fallbackFolder);
                    fullPath = Path.Combine(fallbackFolder, fileName);
                    Logger.LogError($"GetAutomatedReportOutputPath: Using fallback for ReportType {reportTypeIndex}: {fullPath}");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Auto Run: Error determining raw output directory for ReportType {reportTypeIndex}: {ex.Message}", ex);
                string errorFallbackFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), $"QuoteConversion_ErrorFallback_Raw_Type{reportTypeIndex}");
                try { Directory.CreateDirectory(errorFallbackFolder); } catch { /* Best effort */ }
                fullPath = Path.Combine(errorFallbackFolder, fileName);
                Logger.LogError($"GetAutomatedReportOutputPath: Using CRITICAL ErrorFallback for ReportType {reportTypeIndex}: {fullPath}");
            }
            return fullPath;
        }

        private string GetAutomatedTemplatePath(int reportTypeIndex)
        {
            string baseDir = ExcelTemplateBaseDir;
            string templateName = reportTypeIndex switch
            {
                _ => "TEMPLATE_Estimate Success Rate.xlsx"
            };
            return Path.Combine(baseDir, templateName);
        }

        private (string Subject, string Body) GetEmailSubjectAndBodyForAutoRun(int reportTypeIndex, DateTime reportStartDate, DateTime reportEndDate)
        {
            string reportTypeNameBase = "Estimate Success Rate";
            string greeting;
            string greetingKeyName; // Specific key name like "AutoRunDaily"

            if (IsDebug)
            {
                greetingKeyName = GreetingKeyDebugDefault; // "DebugDefault"
                greeting = _greetingManager.GetGreeting(greetingKeyName, isForDebugSection: true);
            }
            else // Release mode for AutoRun
            {
                greetingKeyName = reportTypeIndex switch
                {
                    DailyReportIndex => GreetingKeyAutoRunDaily, // "AutoRunDaily"
                    NewDailyReportOver1kIndex => GreetingKeyAutoRunDaily5Day1k, // "AutoRunDaily5Day1k"
                    _ => "ManualTeam" // Fallback greeting key if type is unexpected for auto-run
                };
                greeting = _greetingManager.GetGreeting(greetingKeyName); // isForDebugSection defaults to false

                if (greetingKeyName == "ManualTeam" && (reportTypeIndex == DailyReportIndex || reportTypeIndex == NewDailyReportOver1kIndex))
                {
                    //Logger.LogWarning($"AutoRun: Specific greeting for automated report type {reportTypeIndex} ('{greetingKeyName}') not found or resolved to fallback. Ensure '{greetingKeyName}' exists under '{ProdEmailGreetingsSectionKey}'.");
                }
            }

            if (!string.IsNullOrWhiteSpace(greeting) && !greeting.TrimEnd().EndsWith(","))
            {
                greeting = greeting.TrimEnd() + ",";
            }

            string subjectPrefix = reportTypeIndex switch
            {
                DailyReportIndex => $"Daily {reportTypeNameBase}",
                NewDailyReportOver1kIndex => $"Daily (5days >= £1000) {reportTypeNameBase}",
                _ => $"Automated {reportTypeNameBase}"
            };
            string dateRangeInfo = (reportStartDate.Date == reportEndDate.Date) ? $"for {reportEndDate:dd MMM yy}" : $"for period {reportStartDate:dd MMM yy} to {reportEndDate:dd MMM yy}";
            string subjectDateSuffix = (reportStartDate.Date == reportEndDate.Date) ? $"({reportEndDate:yyyy-MM-dd})" : $"({reportStartDate:yyyy-MM-dd} to {reportEndDate:yyyy-MM-dd})";
            string subject = $"AUTOMATED: {subjectPrefix} Report {subjectDateSuffix}";
            string body = $"{greeting}\n\nPlease find attached the automated {subjectPrefix.ToLower()} report {dateRangeInfo}.\n\nThank you,\nAutomation Service";
            Logger.LogDebug($"Auto Run: Email for ReportType {reportTypeIndex}: Subject='{subject}', Greeting used='{greeting.Split('\n')[0]}'");
            return (subject, body);
        }
    }
}
