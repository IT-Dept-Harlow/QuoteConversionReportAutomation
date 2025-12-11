// QuoteConversionReportAutomation/Orchestrators/ManualReportOrchestrator.cs

#region Using Directives

// System-related namespaces for core functionalities.
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

// Third-party namespaces for external libraries.
using Microsoft.Extensions.Configuration;

// Project-specific namespaces for application components.
using QuoteConversionReportAutomation.Configuration;
using QuoteConversionReportAutomation.Helpers;
using QuoteConversionReportAutomation.Interfaces;
using QuoteConversionReportAutomation.Managers;
using QuoteConversionReportAutomation.Models;
using QuoteConversionReportAutomation.Models.Status;
using QuoteConversionReportAutomation.Orchestrators.Interfaces;
using QuoteConversionReportAutomation.Services.Communication;
using QuoteConversionReportAutomation.Services.Interfaces;
using QuoteConversionReportAutomation.Services.Logging;

#endregion

namespace QuoteConversionReportAutomation.Orchestrators
{
    #region Class Definition
    /// <summary>
    /// Implements the <see cref="IManualReportOrchestrator"/> to manage the high-level workflow
    /// for user-initiated report creation and processing.
    /// </summary>
    public class ManualReportOrchestrator : IManualReportOrchestrator
    {
        #region Fields

        // --- Injected Dependencies ---
        private readonly IConfiguration _configuration; // Application configuration
        private readonly IReportPathService _reportPathService; // Service for resolving report and app paths
        private readonly ReportProcessManager _processManager; // Manages the external report wrapper process
        private readonly NamedPipeCommunicator _pipeCommunicator; // Handles IPC with the report wrapper
        private readonly IExcelProcessingOrchestrator _excelProcessingOrchestrator; // Orchestrates Excel processing
        private readonly EmailUtility _emailUtility; // Utility for sending emails
        private readonly EmailRecipientManager _emailRecipientManager; // Manages email recipients
        private readonly GreetingManager _greetingManager; // Manages email greetings
        private readonly IStatusManagerService _statusManager; // Centralised status reporting

        #endregion

        #region Constructor

        /// <summary>
        /// Initialises a new instance of the <see cref="ManualReportOrchestrator"/> class.
        /// </summary>
        /// <param name="configuration">The application configuration.</param>
        /// <param name="reportPathService">Service for resolving application and report paths.</param>
        /// <param name="processManager">Manager for the external report wrapper process.</param>
        /// <param name="pipeCommunicator">Communicator for IPC with the report wrapper.</param>
        /// <param name="excelProcessingOrchestrator">The orchestrator for the Excel processing workflow.</param>
        /// <param name="emailUtility">Utility for sending emails.</param>
        /// <param name="emailRecipientManager">Manager for determining email recipients.</param>
        /// <param name="greetingManager">Manager for determining email greetings.</param>
        /// <param name="statusManager">The centralised service for status reporting.</param>
        public ManualReportOrchestrator(
            IConfiguration configuration,
            IReportPathService reportPathService,
            ReportProcessManager processManager,
            NamedPipeCommunicator pipeCommunicator,
            IExcelProcessingOrchestrator excelProcessingOrchestrator,
            EmailUtility emailUtility,
            EmailRecipientManager emailRecipientManager,
            GreetingManager greetingManager,
            IStatusManagerService statusManager)
        {
            // Validate and assign dependencies
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _reportPathService = reportPathService ?? throw new ArgumentNullException(nameof(reportPathService));
            _processManager = processManager ?? throw new ArgumentNullException(nameof(processManager));
            _pipeCommunicator = pipeCommunicator ?? throw new ArgumentNullException(nameof(pipeCommunicator));
            _excelProcessingOrchestrator = excelProcessingOrchestrator ?? throw new ArgumentNullException(nameof(excelProcessingOrchestrator));
            _emailUtility = emailUtility ?? throw new ArgumentNullException(nameof(emailUtility));
            _emailRecipientManager = emailRecipientManager ?? throw new ArgumentNullException(nameof(emailRecipientManager));
            _greetingManager = greetingManager ?? throw new ArgumentNullException(nameof(greetingManager));
            _statusManager = statusManager ?? throw new ArgumentNullException(nameof(statusManager));

            Logger.LogInfo("ManualReportOrchestrator initialised.");
        }

        #endregion

        #region IManualReportOrchestrator Implementation

        /// <inheritdoc/>
        /// <summary>
        /// Asynchronously creates a raw data report based on the provided parameters.
        /// </summary>
        /// <param name="parameters">The parameters defining the report to be created.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A <see cref="Task{TResult}"/> that represents the asynchronous operation.
        /// The task result contains a <see cref="ReportCreationResult"/> object with the outcome.</returns>
        public async Task<ReportCreationResult> CreateRawReportAsync(
            ManualReportParameters parameters,
            CancellationToken cancellationToken)
        {
            // Validate input
            ArgumentNullException.ThrowIfNull(parameters, nameof(parameters));
            Logger.LogInfo($"CreateRawReportAsync called for ReportType: {parameters.ReportType}, DateRange: {parameters.StartDate:d} to {parameters.EndDate:d}");
            _statusManager.Post("Validating request for raw report...", MessageType.InProgress);

            try
            {
                // Progress adapter to relay status updates
                var progressAdapter = new Progress<string>(status => _statusManager.Post(status, MessageType.InProgress));
                // Get the path to the Crystal Report definition file
                string? crystalReportPath = _reportPathService.CrystalReportRptFilePath;
                if (string.IsNullOrEmpty(crystalReportPath) || !File.Exists(crystalReportPath))
                {
                    // Fail if the report definition file is missing
                    return ReportCreationResult.FailureResult($"Crystal Report location ('{crystalReportPath}') is invalid or file not found. Check configuration path '{AppConfigKeys.Paths.CrystalReportRptFile}'.");
                }

                _statusManager.Post("Ensuring report service is active...", MessageType.InProgress);
                // Ensure the external report wrapper process is running
                if (!await _processManager.EnsureWrapperIsRunningAsync(progressAdapter, cancellationToken))
                {
                    return ReportCreationResult.FailureResult("Failed to start or connect to the report service (CrystalReportWrapper).");
                }

                // Determine the output path for the raw report
                string? reportOutputPath = _reportPathService.GetRawReportOutputPath(parameters.ReportType, parameters.EndDate, parameters.ReportBaseName);
                if (string.IsNullOrEmpty(reportOutputPath))
                {
                    return ReportCreationResult.FailureResult("Failed to determine the output path for the raw report.");
                }

                // Build the request object for the report wrapper
                var request = new ReportRequest
                {
                    CrystalReportLocation = crystalReportPath,
                    ReportOutputLocation = reportOutputPath,
                    ReportDateFrom = parameters.StartDate,
                    ReportDateTo = parameters.EndDate
                };

                _statusManager.Post("Sending request to report service...", MessageType.InProgress);
                // Send the request and await the response from the wrapper
                ReportResponse? response = await _pipeCommunicator.SendRequestReceiveResponseAsync(request, progressAdapter, cancellationToken);

                // Check if the response indicates success and the output file exists
                if (response?.Success == true && !string.IsNullOrEmpty(response.OutputPath) && File.Exists(response.OutputPath))
                {
                    Logger.LogInfo($"Raw report generated successfully by wrapper: {response.OutputPath}");
                    return ReportCreationResult.SuccessResult(response.OutputPath);
                }
                else
                {
                    // If failed, return the error message from the response or a generic error
                    string errorMessage = response?.ErrorMessage ?? "Unknown error from report service.";
                    return ReportCreationResult.FailureResult($"Raw report generation failed: {errorMessage}");
                }
            }
            catch (OperationCanceledException)
            {
                // Handle cancellation
                return ReportCreationResult.FailureResult("The report generation request timed out or was cancelled.");
            }
            catch (Exception ex)
            {
                // Handle unexpected errors
                return ReportCreationResult.FailureResult($"An error occurred while requesting the raw report: {ex.Message}");
            }
        }

        /// <inheritdoc/>
        /// <summary>
        /// Asynchronously processes a raw report file and, if configured, sends it via email.
        /// </summary>
        /// <param name="rawReportPath">The full path to the raw report file to be processed.</param>
        /// <param name="parameters">The parameters that guided the report's creation.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A <see cref="Task{TResult}"/> that represents the asynchronous operation.
        /// The task result contains a <see cref="ReportProcessingResult"/> object with the outcome.</returns>
        public async Task<ReportProcessingResult> ProcessAndEmailReportAsync(
            string rawReportPath,
            ManualReportParameters parameters,
            CancellationToken cancellationToken)
        {
            // Validate input
            ArgumentException.ThrowIfNullOrEmpty(rawReportPath, nameof(rawReportPath));
            ArgumentNullException.ThrowIfNull(parameters, nameof(parameters));
            Logger.LogInfo($"ProcessAndEmailReportAsync called for RawReport: '{rawReportPath}', ReportType: {parameters.ReportType}, SkipEmail: {parameters.SkipEmail}");
            _statusManager.Post("Starting Excel processing...", MessageType.InProgress);

            string? finalAnalysisPath = null;
            try
            {
                // Ensure the raw report file exists
                if (!File.Exists(rawReportPath))
                {
                    return ReportProcessingResult.FailureResult($"The raw report file '{rawReportPath}' has not been generated or cannot be found.");
                }

                // Get the Excel template path for the report type
                string? templatePath = _reportPathService.GetExcelTemplatePath(parameters.ReportType);
                if (string.IsNullOrEmpty(templatePath) || !File.Exists(templatePath))
                {
                    return ReportProcessingResult.FailureResult($"Excel template path is invalid or file not found: '{templatePath}'.");
                }

                // Get the base directory for saving the final report
                string baseSaveLocation = _reportPathService.FinalReportOutputBaseDirectory;
                if (string.IsNullOrEmpty(baseSaveLocation))
                {
                    return ReportProcessingResult.FailureResult("Final report output base directory is not configured.");
                }

                // Determine the primary date for processing logic and file naming
                DateTime dateForFilenameAndProcessing = (parameters.ReportType == ReportType.Annual) ? parameters.StartDate : parameters.EndDate;

                // Call the Excel processing orchestrator to process the report
                finalAnalysisPath = await _excelProcessingOrchestrator.ProcessExcelReportAsync(
                    parameters.FinancialYear,
                    parameters.ReportType,
                    rawReportPath,
                    "RawDataSourceSheet", // Config key for source sheet name
                    baseSaveLocation,
                    templatePath,
                    "TemplateDataCopySheet", // Config key for destination sheet name
                    1, 1,
                    dateForFilenameAndProcessing,
                    parameters,
                    autoRunDef: null,
                    cancellationToken);

                // Check if the orchestration was successful and the output file exists
                if (string.IsNullOrEmpty(finalAnalysisPath) || !File.Exists(finalAnalysisPath))
                {
                    if (cancellationToken.IsCancellationRequested) throw new OperationCanceledException("Excel processing was cancelled.");
                    return ReportProcessingResult.FailureResult("Excel processing failed to produce a final file. Check logs for details.");
                }
                Logger.LogInfo($"Excel report processed successfully. Final analysis file: {finalAnalysisPath}");

                // After successful processing, handle the email step if not skipped
                EmailSendResult? emailSendOutcome = null;
                if (!parameters.SkipEmail)
                {
                    emailSendOutcome = await SendManualReportEmailAsync(finalAnalysisPath, parameters, cancellationToken);
                    if (!emailSendOutcome.Success)
                    {
                        // If email fails, the overall result is a failure, but we include the generated path
                        return ReportProcessingResult.FailureResult($"Email sending failed: {emailSendOutcome.ErrorMessage}", finalAnalysisPath, emailSendOutcome);
                    }
                }
                else
                {
                    Logger.LogInfo("Email sending skipped by user.");
                }

                // Return a successful result, including the email outcome
                return ReportProcessingResult.SuccessResult(finalAnalysisPath, emailSendOutcome);
            }
            catch (OperationCanceledException)
            {
                // Handle cancellation
                return ReportProcessingResult.FailureResult("Operation cancelled.");
            }
            catch (Exception ex)
            {
                // Handle unexpected errors
                return ReportProcessingResult.FailureResult($"An unexpected error occurred during processing: {ex.Message}");
            }
        }

        #region Private Email Helper Methods
        /// <summary>
        /// Asynchronously sends the completion email for a manually generated report.
        /// </summary>
        /// <param name="attachmentPath">The path to the file to attach to the email.</param>
        /// <param name="parameters">The parameters used for the report, which influence recipients and content.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>An <see cref="EmailSendResult"/> indicating the outcome of the email operation.</returns>
        private async Task<EmailSendResult> SendManualReportEmailAsync(
            string attachmentPath,
            ManualReportParameters parameters,
            CancellationToken cancellationToken)
        {
            _statusManager.Post("Preparing email...", MessageType.InProgress);

            // Ensure the attachment exists
            if (!File.Exists(attachmentPath))
            {
                return new EmailSendResult(false, $"Attachment file not found for email: {attachmentPath}");
            }

            try
            {
                // Get the recipient lists for this report context
                var (to, cc) = _emailRecipientManager.GetRecipients(
                    (int)parameters.ReportType,
                    parameters.IsFemiOnlyChecked,
                    parameters.IsDebugBuild,
                    isAutoRunContext: false,
                    definition: null);

                // If no recipients and not a debug build, treat as a success (no email sent)
                if (!to.Any() && !cc.Any() && !parameters.IsDebugBuild)
                {
                    return new EmailSendResult(true, "No recipients configured, email not sent.");
                }

                // Build the subject and body for the email
                var (subject, body) = GetManualEmailSubjectAndBody(parameters);
                _statusManager.Post("Sending email...", MessageType.InProgress);

                // Send the email using the utility
                return await _emailUtility.SendEmailAsync(to, cc, subject, body, attachmentPath, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                // Propagate cancellation
                throw;
            }
            catch (Exception ex)
            {
                // Handle unexpected errors
                return new EmailSendResult(false, $"Unexpected error during email preparation: {ex.Message}");
            }
        }

        /// <summary>
        /// Constructs the email subject line and body content for a manually generated report.
        /// </summary>
        /// <param name="parameters">The parameters used for the report, which influence the subject and body.</param>
        /// <returns>A tuple containing the subject and body strings for the email.</returns>
        private (string Subject, string Body) GetManualEmailSubjectAndBody(ManualReportParameters parameters)
        {
            // The type name for the report (used in subject/body)
            string typeName = "Estimate Success Rate";
            // Get the display string for the report type
            string reportTypeString = ReportTypeHelper.GetDisplayString(parameters.ReportType, _configuration);

            // Determine the greeting key based on report type and debug status
            string greetingKeyName = parameters.IsDebugBuild
                ? "DebugDefault"
                : parameters.ReportType switch
                {
                    ReportType.Daily => "ManualStdDaily",
                    ReportType.Daily5Day1k => parameters.IsFemiOnlyChecked ? "ManualFemi" : "ManualTeam",
                    ReportType.Custom => "ManualCustom",
                    _ => parameters.IsFemiOnlyChecked ? "ManualFemi" : "ManualTeam"
                };

            // Get the greeting string
            string greeting = _greetingManager.GetGreeting(greetingKeyName, parameters.IsDebugBuild);
            // Ensure the greeting ends with a comma
            if (!string.IsNullOrWhiteSpace(greeting) && !greeting.TrimEnd().EndsWith(","))
            {
                greeting = greeting.TrimEnd() + ",";
            }

            // Build the date range info for the body
            string rangeInfo = (parameters.StartDate.Date == parameters.EndDate.Date)
                ? $"for {parameters.EndDate:dd MMM yy}"
                : $"for period {parameters.StartDate:dd MMM yy} to {parameters.EndDate:dd MMM yy}";

            // Build the subject date suffix
            string subjectDateSuffix = (parameters.StartDate.Date == parameters.EndDate.Date)
                                       ? $"({parameters.EndDate:yyyy-MM-dd})"
                                       : $"({parameters.StartDate:yyyy-MM-dd} to {parameters.EndDate:yyyy-MM-dd})";

            // Build the subject line
            string subject = $"MANUAL: {reportTypeString} {typeName} Report {subjectDateSuffix}";
            if (parameters.IsDebugBuild) subject = $"DEBUG - {subject}";

            // Get the email signature from configuration (with fallback)
            string emailSignature = _configuration.GetValue<string>(AppConfigKeys.EmailSettings.DefaultEmailSignature, "Thank you,\nAutomation Service")!;
            // Build the body
            string body = $"{greeting}\n\nPlease find attached the {reportTypeString.ToLowerInvariant()} {typeName.ToLowerInvariant()} report {rangeInfo}.\n\n{emailSignature}";

            return (subject, body);
        }
        #endregion

        #endregion
    }
    #endregion
}