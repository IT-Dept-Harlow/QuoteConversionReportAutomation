// ManualReportOrchestrator.cs
// Orchestrates the manual creation, processing, and emailing of quote conversion reports.
// This version is fully refactored to use the IStatusManagerService for all progress reporting.

#region Using Directives
// System related namespaces
using Microsoft.Extensions.Configuration;
// Project specific namespaces
using QuoteConversionReportAutomation.Configuration;
using QuoteConversionReportAutomation.Helpers;
using QuoteConversionReportAutomation.Interfaces; // For IManualReportOrchestrator and IStatusManagerService
using QuoteConversionReportAutomation.Managers;
using QuoteConversionReportAutomation.Models;
using QuoteConversionReportAutomation.Models.Status; // For MessageType
using QuoteConversionReportAutomation.Orchestrators.Interfaces;
using QuoteConversionReportAutomation.Services;
using QuoteConversionReportAutomation.Services.Communication;
using QuoteConversionReportAutomation.Services.Excel;
using QuoteConversionReportAutomation.Services.Interfaces;
using QuoteConversionReportAutomation.Services.Logging;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
#endregion

namespace QuoteConversionReportAutomation.Orchestrators
{
    /// <summary>
    /// Orchestrates the manual creation, processing, and emailing of quote conversion reports.
    /// This class encapsulates the high-level workflow for user-initiated actions.
    /// It uses the <see cref="IStatusManagerService"/> to provide real-time feedback to the UI.
    /// </summary>
    public class ManualReportOrchestrator : IManualReportOrchestrator
    {
        #region Fields
        private readonly IConfiguration _configuration;
        private readonly IReportPathService _reportPathService;
        private readonly ReportProcessManager _processManager;
        private readonly NamedPipeCommunicator _pipeCommunicator;
        private readonly ExcelCopyData _excelProcessor;
        private readonly EmailUtility _emailUtility;
        private readonly EmailRecipientManager _emailRecipientManager;
        private readonly GreetingManager _greetingManager;
        private readonly IStatusManagerService _statusManager;
        #endregion

        #region Constructor
        /// <summary>
        /// Initializes a new instance of the <see cref="ManualReportOrchestrator"/> class.
        /// </summary>
        /// <param name="configuration">The application configuration.</param>
        /// <param name="reportPathService">Service for resolving application and report paths.</param>
        /// <param name="processManager">Manager for the external report wrapper process.</param>
        /// <param name="pipeCommunicator">Communicator for IPC with the report wrapper.</param>
        /// <param name="excelProcessor">Service for processing Excel files.</param>
        /// <param name="emailUtility">Utility for sending emails.</param>
        /// <param name="emailRecipientManager">Manager for determining email recipients.</param>
        /// <param name="greetingManager">Manager for determining email greetings.</param>
        /// <param name="statusManager">The centralised service for status reporting.</param>
        public ManualReportOrchestrator(
            IConfiguration configuration,
            IReportPathService reportPathService,
            ReportProcessManager processManager,
            NamedPipeCommunicator pipeCommunicator,
            ExcelCopyData excelProcessor,
            EmailUtility emailUtility,
            EmailRecipientManager emailRecipientManager,
            GreetingManager greetingManager,
            IStatusManagerService statusManager) // New dependency
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _reportPathService = reportPathService ?? throw new ArgumentNullException(nameof(reportPathService));
            _processManager = processManager ?? throw new ArgumentNullException(nameof(processManager));
            _pipeCommunicator = pipeCommunicator ?? throw new ArgumentNullException(nameof(pipeCommunicator));
            _excelProcessor = excelProcessor ?? throw new ArgumentNullException(nameof(excelProcessor));
            _emailUtility = emailUtility ?? throw new ArgumentNullException(nameof(emailUtility));
            _emailRecipientManager = emailRecipientManager ?? throw new ArgumentNullException(nameof(emailRecipientManager));
            _greetingManager = greetingManager ?? throw new ArgumentNullException(nameof(greetingManager));
            _statusManager = statusManager ?? throw new ArgumentNullException(nameof(statusManager)); // New dependency

            Logger.LogInfo("ManualReportOrchestrator initialized.");
        }
        #endregion

        #region IManualReportOrchestrator Implementation

        /// <inheritdoc/>
        public async Task<ReportCreationResult> CreateRawReportAsync(
            ManualReportParameters parameters,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(parameters, nameof(parameters));

            Logger.LogInfo($"CreateRawReportAsync called for ReportType: {parameters.ReportType}, DateRange: {parameters.StartDate:d} to {parameters.EndDate:d}");
            _statusManager.Post("Validating request for raw report...", MessageType.InProgress);

            try
            {
                // Create a progress adapter to pass to downstream services that still expect IProgress<T>.
                // This adapter forwards all messages to our new StatusManagerService.
                var progressAdapter = new Progress<string>(status => _statusManager.Post(status, MessageType.InProgress));

                string? crystalReportPath = _reportPathService.CrystalReportRptFilePath;
                if (string.IsNullOrEmpty(crystalReportPath) || !File.Exists(crystalReportPath))
                {
                    return ReportCreationResult.FailureResult($"Crystal Report location ('{crystalReportPath}') is invalid or file not found. Check configuration path '{AppConfigKeys.Paths.CrystalReportRptFile}'.");
                }

                _statusManager.Post("Ensuring report service is active...", MessageType.InProgress);
                if (!await _processManager.EnsureWrapperIsRunningAsync(progressAdapter, cancellationToken))
                {
                    return ReportCreationResult.FailureResult("Failed to start or connect to the report service (CrystalReportWrapper).");
                }

                string? reportOutputPath = _reportPathService.GetRawReportOutputPath(parameters.ReportType, parameters.EndDate, parameters.ReportBaseName);
                if (string.IsNullOrEmpty(reportOutputPath))
                {
                    return ReportCreationResult.FailureResult("Failed to determine the output path for the raw report.");
                }

                var request = new ReportRequest
                {
                    CrystalReportLocation = crystalReportPath,
                    ReportOutputLocation = reportOutputPath,
                    ReportDateFrom = parameters.StartDate,
                    ReportDateTo = parameters.EndDate
                };

                _statusManager.Post("Sending request to report service...", MessageType.InProgress);
                ReportResponse? response = await _pipeCommunicator.SendRequestReceiveResponseAsync(request, progressAdapter, cancellationToken);

                if (response?.Success == true && !string.IsNullOrEmpty(response.OutputPath) && File.Exists(response.OutputPath))
                {
                    Logger.LogInfo($"Raw report generated successfully by wrapper: {response.OutputPath}");
                    // The final success message will be posted by the calling method in Form1.
                    return ReportCreationResult.SuccessResult(response.OutputPath);
                }
                else
                {
                    string errorMessage = response?.ErrorMessage ?? "Unknown error from report service.";
                    if (response?.Success == true && (string.IsNullOrEmpty(response.OutputPath) || !File.Exists(response.OutputPath)))
                    {
                        errorMessage = $"Report service indicated success, but the output file ('{response?.OutputPath ?? "NULL"}') is invalid or missing.";
                    }
                    Logger.LogError($"Raw report generation failed: {errorMessage}");
                    return ReportCreationResult.FailureResult($"Raw report generation failed: {errorMessage}");
                }
            }
            catch (OperationCanceledException)
            {
                Logger.LogWarning("Raw report generation request cancelled or timed out.");
                return ReportCreationResult.FailureResult("The report generation request timed out or was cancelled.");
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error during CreateRawReportAsync: {ex.Message}", ex);
                return ReportCreationResult.FailureResult($"An error occurred while requesting the raw report: {ex.Message}");
            }
        }

        /// <inheritdoc/>
        public async Task<ReportProcessingResult> ProcessAndEmailReportAsync(
            string rawReportPath,
            ManualReportParameters parameters,
            CancellationToken cancellationToken)
        {
            ArgumentException.ThrowIfNullOrEmpty(rawReportPath, nameof(rawReportPath));
            ArgumentNullException.ThrowIfNull(parameters, nameof(parameters));

            Logger.LogInfo($"ProcessAndEmailReportAsync called for RawReport: '{rawReportPath}', ReportType: {parameters.ReportType}, SkipEmail: {parameters.SkipEmail}");
            _statusManager.Post("Starting Excel processing...", MessageType.InProgress);

            string? finalAnalysisPath = null;
            try
            {
                if (!File.Exists(rawReportPath))
                {
                    return ReportProcessingResult.FailureResult($"The raw report file '{rawReportPath}' has not been generated or cannot be found. Please create the report first.");
                }

                string? templatePath = _reportPathService.GetExcelTemplatePath(parameters.ReportType);
                if (string.IsNullOrEmpty(templatePath) || !File.Exists(templatePath))
                {
                    return ReportProcessingResult.FailureResult($"Excel template path is invalid or file not found: '{templatePath}'. Check configuration.");
                }

                string baseSaveLocation = _reportPathService.FinalReportOutputBaseDirectory;
                if (string.IsNullOrEmpty(baseSaveLocation))
                {
                    return ReportProcessingResult.FailureResult("Final report output base directory is not configured.");
                }

                DateTime dateForFilenameAndProcessing = (parameters.ReportType == ReportType.Annual) ? parameters.StartDate : parameters.EndDate;
                string? expectedFinalPath = _excelProcessor.GetExpectedFinalFilePath(parameters.ReportType, baseSaveLocation, dateForFilenameAndProcessing);

                if (expectedFinalPath != null && File.Exists(expectedFinalPath))
                {
                    _statusManager.Post("Found existing file. Overwriting...", MessageType.InProgress);
                    try { File.Delete(expectedFinalPath); }
                    catch (Exception delEx) { return ReportProcessingResult.FailureResult($"Could not delete the existing report file '{expectedFinalPath}': {delEx.Message}"); }
                }

                // Keys for sheet names; ExcelCopyData resolves them via AppConfigKeys.
                string rawDataSourceSheetConfigKey = "RawDataSourceSheet";
                string templateDataCopySheetConfigKey = "TemplateDataCopySheet";

                // Call the refactored Excel processor, which now uses the status manager internally.
                finalAnalysisPath = await _excelProcessor.ProcessExcelReportAsync(
                    parameters.FinancialYear ?? _excelProcessor.GetCurrentFinancialYear(true),
                    parameters.ReportType,
                    rawReportPath,
                    rawDataSourceSheetConfigKey,
                    baseSaveLocation,
                    templatePath,
                    templateDataCopySheetConfigKey,
                    1, 1,
                    dateForFilenameAndProcessing,
                    cancellationToken);

                if (string.IsNullOrEmpty(finalAnalysisPath) || !File.Exists(finalAnalysisPath))
                {
                    if (cancellationToken.IsCancellationRequested) throw new OperationCanceledException("Excel processing was cancelled.");
                    return ReportProcessingResult.FailureResult("Excel processing failed to produce a final file. Check logs for details.");
                }
                Logger.LogInfo($"Excel report processed successfully. Final analysis file: {finalAnalysisPath}");

                // Determine if this report type requires the user to manually refresh pivots in Excel.
                bool requiresManualRefresh = parameters.ReportType is ReportType.Monthly or ReportType.Quarterly or ReportType.Annual or ReportType.Custom;

                if (requiresManualRefresh && !parameters.SkipEmail)
                {
                    _statusManager.Post("Excel processing complete. Manual refresh required.", MessageType.Warning);
                    Logger.LogInfo($"Report type {parameters.ReportType} requires manual Excel refresh. Emailing deferred pending user action.");
                    // Return a success result that indicates manual refresh is needed. The UI will handle the interaction.
                    return ReportProcessingResult.SuccessResult(finalAnalysisPath, emailResult: null, manualRefreshRequired: true);
                }

                EmailSendResult? emailSendOutcome = null;
                if (!parameters.SkipEmail)
                {
                    emailSendOutcome = await SendManualReportEmailAsync(finalAnalysisPath, parameters, cancellationToken);
                    if (!emailSendOutcome.Success)
                    {
                        return ReportProcessingResult.FailureResult($"Email sending failed: {emailSendOutcome.ErrorMessage}", finalAnalysisPath, emailSendOutcome);
                    }
                }
                else
                {
                    Logger.LogInfo("Email sending skipped by user.");
                }

                return ReportProcessingResult.SuccessResult(finalAnalysisPath, emailSendOutcome, manualRefreshRequired: false);
            }
            catch (OperationCanceledException)
            {
                Logger.LogWarning("Excel processing or email sending was cancelled.");
                return ReportProcessingResult.FailureResult("Operation cancelled.");
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error during ProcessAndEmailReportAsync: {ex.Message}", ex);
                return ReportProcessingResult.FailureResult($"An unexpected error occurred during processing: {ex.Message}");
            }
        }

        /// <inheritdoc/>
        public async Task<EmailSendResult> SendEmailAfterManualRefreshAsync(
            string analysisFilePath,
            ManualReportParameters parameters,
            CancellationToken cancellationToken)
        {
            ArgumentException.ThrowIfNullOrEmpty(analysisFilePath, nameof(analysisFilePath));
            ArgumentNullException.ThrowIfNull(parameters, nameof(parameters));

            Logger.LogInfo($"SendEmailAfterManualRefreshAsync called for AnalysisFile: '{analysisFilePath}'");

            if (parameters.SkipEmail)
            {
                Logger.LogInfo("SendEmailAfterManualRefreshAsync: Email was marked as skipped in parameters. No email will be sent.");
                return new EmailSendResult(true, "Email skipped as per original request.");
            }

            return await SendManualReportEmailAsync(analysisFilePath, parameters, cancellationToken);
        }

        #endregion

        #region Private Email Helper Methods
        /// <summary>
        /// Asynchronously sends the completion email for a manually generated report.
        /// This is a private helper used by ProcessAndEmailReportAsync and SendEmailAfterManualRefreshAsync.
        /// </summary>
        private async Task<EmailSendResult> SendManualReportEmailAsync(
            string attachmentPath,
            ManualReportParameters parameters,
            CancellationToken cancellationToken)
        {
            Logger.LogTrace("ManualReportOrchestrator: Entering SendManualReportEmailAsync.");
            _statusManager.Post("Preparing email...", MessageType.InProgress);

            if (!File.Exists(attachmentPath))
            {
                return new EmailSendResult(false, $"Attachment file not found for email: {attachmentPath}");
            }

            try
            {
                // Create a progress adapter for the email utility.
                var progressAdapter = new Progress<string>(status => _statusManager.Post(status, MessageType.InProgress));

                var (to, cc) = _emailRecipientManager.GetRecipients(
                    (int)parameters.ReportType,
                    parameters.IsFemiOnlyChecked,
                    parameters.IsDebugBuild,
                    isAutoRunContext: false,
                    definition: null);

                if (!to.Any() && !cc.Any() && !parameters.IsDebugBuild)
                {
                    return new EmailSendResult(true, "No recipients configured, email not sent.");
                }

                var (subject, body) = GetManualEmailSubjectAndBody(parameters);
                _statusManager.Post("Sending email...", MessageType.InProgress);

                return await _emailUtility.SendEmailAsync(to, cc, subject, body, attachmentPath, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                Logger.LogWarning("SendManualReportEmailAsync: Email sending operation was cancelled.");
                throw; // Re-throw so the caller knows the operation was cancelled.
            }
            catch (Exception ex)
            {
                Logger.LogError($"SendManualReportEmailAsync: Error preparing or dispatching email: {ex.Message}", ex);
                return new EmailSendResult(false, $"Unexpected error during email preparation: {ex.Message}");
            }
        }

        /// <summary>
        /// Constructs the email subject line and body content for a manually generated report.
        /// </summary>
        private (string Subject, string Body) GetManualEmailSubjectAndBody(ManualReportParameters parameters)
        {
            string typeName = "Estimate Success Rate";
            string reportTypeString = ReportTypeHelper.GetDisplayString(parameters.ReportType);

            string greetingKeyName = parameters.IsDebugBuild
                ? "DebugDefault"
                : parameters.ReportType switch
                {
                    ReportType.Daily => "ManualStdDaily",
                    ReportType.Daily5Day1k => parameters.IsFemiOnlyChecked ? "ManualFemi" : "ManualTeam",
                    ReportType.Custom => "ManualCustom",
                    _ => parameters.IsFemiOnlyChecked ? "ManualFemi" : "ManualTeam"
                };

            string greeting = _greetingManager.GetGreeting(greetingKeyName, parameters.IsDebugBuild);
            if (!string.IsNullOrWhiteSpace(greeting) && !greeting.TrimEnd().EndsWith(","))
            {
                greeting = greeting.TrimEnd() + ",";
            }

            string rangeInfo = (parameters.StartDate.Date == parameters.EndDate.Date)
                ? $"for {parameters.EndDate:dd MMM yy}"
                : $"for period {parameters.StartDate:dd MMM yy} to {parameters.EndDate:dd MMM yy}";

            if (parameters.ReportType == ReportType.Monthly) rangeInfo = $"for {parameters.StartDate:MMMM yy}";
            else if (parameters.ReportType == ReportType.Quarterly) rangeInfo = $"for {ReportHelper.GetQuarterString(parameters.StartDate)} {parameters.StartDate.Year}";
            else if (parameters.ReportType == ReportType.Annual) rangeInfo = $"for Financial Year {parameters.StartDate.Year}-{parameters.EndDate.Year}";

            string subjectPrefix = $"{reportTypeString} {typeName}";
            if (parameters.ReportType == ReportType.Annual) subjectPrefix = $"Annual {typeName}";
            else if (parameters.ReportType == ReportType.Custom && string.IsNullOrWhiteSpace(reportTypeString)) subjectPrefix = $"Report {typeName}";

            string subjectDateSuffix = (parameters.StartDate.Date == parameters.EndDate.Date)
                                       ? $"({parameters.EndDate:yyyy-MM-dd})"
                                       : $"({parameters.StartDate:yyyy-MM-dd} to {parameters.EndDate:yyyy-MM-dd})";
            if (parameters.ReportType == ReportType.Annual) subjectDateSuffix = $"({parameters.StartDate.Year}-{parameters.EndDate.Year})";

            string manualPrefix = (parameters.ReportType != ReportType.Custom && parameters.ReportType != ReportType.Unknown) ? "MANUAL: " : "";
            string appNamePrefix = _configuration.GetValue<string>(AppConfigKeys.ApplicationInfo.AppName, "QCRA")!;
            string subject = $"{manualPrefix}: {subjectPrefix} Report {subjectDateSuffix}";
            if (parameters.IsDebugBuild) subject = $"DEBUG - {subject}";

            string emailSignature = _configuration.GetValue<string>(AppConfigKeys.EmailSettings.DefaultEmailSignature, "Thank you,\nAutomation Service")!;
            string body = $"{greeting}\n\nPlease find attached the {subjectPrefix.ToLowerInvariant()} report {rangeInfo}.\n\nThis report includes quotes data for review.\n\n{emailSignature}";

            Logger.LogDebug($"GetManualEmailSubjectAndBody: GreetingKey='{greetingKeyName}', Subject='{subject}'");
            return (subject, body);
        }
        #endregion
    }
}