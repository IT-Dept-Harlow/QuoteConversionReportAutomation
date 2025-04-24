// C# 10+ Features
namespace conversionTest;

// --- Global Usings (Consider moving to a Usings.cs file in C# 10+) ---
using Microsoft.Extensions.Configuration; // For IConfiguration
using EmailSender;                      // For EmailUtility
using System;
using System.Collections.Generic;
using System.Diagnostics;                 // Required for Process AND Debug
using System.IO;
using System.IO.Pipes;                  // Required for Named Pipes
using System.Text;                      // Required for Encoding
using System.Windows.Forms;
using System.Threading;                   // Required for CancellationTokenSource
using System.Threading.Tasks;             // Required for Task, Task.Delay etc.
using ReportWrapperCommon;              // Namespace for ReportRequest/Response
using Newtonsoft.Json;                  // For JSON serialization
using QuoteConversionReportAutomation;

/// <summary>
/// Represents the main form of the Quote Conversion Report Automation application.
/// Refactored to use async/await and Task-based patterns.
/// </summary>
public partial class Form1 : Form
{
    #region Fields and Properties

    private readonly IConfiguration _configuration;
    private readonly EmailUtility _emailUtility; // Inject or create EmailUtility instance

    private const string AppVersion = "1.2.5"; // Updated version for fix
    private string _generatedReportPath = string.Empty; // Path to the raw report file from Crystal Wrapper
    private string _generatedAnalysisFilePath = string.Empty; // Path to the final processed Excel file
    private DateTime _today; // Initialized in Form1_Load
    private string _financialYear = string.Empty; // Initialized in Form1_Load

    // Properties read from configuration
    private string CrystalReportLocation => _configuration["settings:CrystalReportPath"] ?? string.Empty;
    private string WrapperExePath => Path.GetFullPath(_configuration["settings:WrapperExePath"] ?? "CrystalReportWrapper.exe");
    private string WrapperProcessName => Path.GetFileNameWithoutExtension(WrapperExePath);

    // --- Dynamic Path Properties ---
    // Calculate paths based on configuration and current state (UI selections)
    public string ReportOutputLocation // Path for the Crystal Report export (input for Button 2)
    {
        get
        {
            string baseDir = $@"C:\Users\{Environment.UserName}\Harlow Printing\IT Projects - Documents\Dashboard Datasets\Raw_data\Quotes conversion\Estimate Reports Exports";
            // Use a consistent file name format, potentially including report type if needed
            string fileName = $"{_today:yyyyMMdd}_EstimateSuccessReport_Raw.xlsx"; // Added _Raw suffix
            string subFolder = typeDropBox.SelectedIndex switch
            {
                1 => "Monthly Reports",
                2 => "Quarterly reports",
                3 => "Annual Reports",
                _ => "Weekly Reports", // Default to Weekly (index 0 or invalid)
            };
            string fullPath = Path.Combine(baseDir, subFolder, fileName);
            // Ensure the directory exists before returning the path
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
            }
            catch (Exception ex)
            {
                Logger.LogError($"Failed to create directory '{Path.GetDirectoryName(fullPath)}': {ex.Message}");
                // Optionally return a default/fallback path or re-throw
            }
            return fullPath;
        }
    }

    public string ExcelTemplateLocation // Path to the Excel template used by Button 2
    {
        get
        {
            string baseDir = $@"C:\Users\{Environment.UserName}\Harlow Printing\IT Projects - Documents\Dashboard Datasets\Raw_data\Quotes conversion\TEMPLATE\";
            // Template name depends on whether it's Weekly (0) or Monthly/Quarterly/Annual (1, 2, 3)
            string templateName = typeDropBox.SelectedIndex switch
            {
                1 or 2 or 3 => "TEMPLATE_Estimate Success Rate_Monthly.xlsx", // Non-Weekly template
                _ => "TEMPLATE_Estimate Success Rate.xlsx" // Weekly template (index 0 or invalid)
            };
            return Path.Combine(baseDir, templateName);
        }
    }

    public string ExcelFinalSaveLocation // Base directory where the final analysis file (from Button 2) is saved
    {
        get
        {
            // This path is now primarily handled within ExcelCopyData.CreateFolders
            // We just need the base location here if still needed elsewhere.
            return $@"C:\Users\{Environment.UserName}\Harlow Printing\IT Projects - Documents\Dashboard Datasets\Raw_data\Quotes conversion\Estimates\";
        }
    }

    // Helper for debug/release builds
    private static bool IsDebug =>
#if DEBUG
        true;
#else
        false;
#endif

    #endregion

    #region Constructor

    public Form1(IConfiguration configuration)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _emailUtility = new EmailUtility(_configuration); // Initialize EmailUtility

        Debug.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Form1 Constructor: Initializing components...");
        try
        {
            InitializeComponent(); // Standard WinForms initialization
            Debug.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Form1 Constructor: InitializeComponent() completed.");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Form1 Constructor: CRITICAL ERROR during InitializeComponent! Exception: {ex}");
            Logger.LogCritical($"CRITICAL ERROR during InitializeComponent: {ex.Message}", ex); // Assuming Logger setup
            MessageBox.Show($"A critical error occurred initializing the form components:\n\n{ex.Message}\n\nThe application cannot continue.",
                            "Form Initialization Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            // Consider Environment.Exit(-1) or Application.Exit() if throwing doesn't stop execution flow properly
            throw; // Rethrow to signal critical failure
        }
        Debug.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Form1 Constructor: Exiting.");
    }

    #endregion

    #region Form Load / Closing

    private async void Form1_Load(object sender, EventArgs e)
    {
        Debug.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Form1_Load: Entered.");
        UpdateStatus("Loading application...");
        try
        {
            // Initialize date/year fields
            _today = DateTime.Today;
            _financialYear = ExcelCopyData.GetCurrentFinancialYear(true); // Use helper, format like 2023_24
            Debug.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Form1_Load: Date='{_today:yyyy-MM-dd}', FY='{_financialYear}'");

            Logger.LogInfo("Form Loading..."); // Assuming Logger is initialized

            // Validate essential configuration
            if (string.IsNullOrEmpty(CrystalReportLocation) || !File.Exists(CrystalReportLocation))
            {
                Logger.LogError($"Config 'settings:CrystalReportPath' missing or file not found: '{CrystalReportLocation}'. Report generation disabled.");
                MessageBox.Show($"Warning: Crystal Report file path is missing or invalid ('{CrystalReportLocation}'). Report generation (Button 1) will be disabled.", "Configuration Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                button1.Enabled = false; // Disable Crystal Report button
            }
            if (string.IsNullOrEmpty(WrapperExePath) || !File.Exists(WrapperExePath))
            {
                Logger.LogError($"Config 'settings:WrapperExePath' missing or file not found: '{WrapperExePath}'. Report generation disabled.");
                MessageBox.Show($"Warning: Crystal Report Wrapper executable path is missing or invalid ('{WrapperExePath}'). Report generation (Button 1) will be disabled.", "Configuration Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                button1.Enabled = false; // Also disable if wrapper is missing
            }
            // Check template path based on initial selection (Weekly)
            string initialTemplatePath = ExcelTemplateLocation; // Gets path based on typeDropBox.SelectedIndex = 0
            if (string.IsNullOrEmpty(initialTemplatePath) || !File.Exists(initialTemplatePath))
            {
                Logger.LogWarning($"Initial Excel template file path is missing or invalid ('{initialTemplatePath}'). Excel processing (Button 2) might fail if Weekly type is used.");
                // Consider showing a warning here as well, or disable Button 2 initially if template is critical from the start.
            }


            // Initialize UI elements
            this.Text = $"Quote Conversion Automation - {(IsDebug ? "DEBUG" : "RELEASE")} - v{AppVersion}";
            this.StartPosition = FormStartPosition.CenterScreen;

            PopulateFinancialYearDropdown(); // Populate FY dropdown
            if (finYearDropBox.Items.Count > 0) finYearDropBox.SelectedIndex = 0;
            finYearDropBox.DropDownStyle = ComboBoxStyle.DropDownList;
            finYearDropBox.BackColor = System.Drawing.Color.White;

            typeDropBox.SelectedIndex = 0; // Default to Weekly
            typeDropBox.DropDownStyle = ComboBoxStyle.DropDownList;
            typeDropBox.BackColor = System.Drawing.Color.White;
            typeDropBox_SelectedIndexChanged(typeDropBox, EventArgs.Empty); // Trigger initial date/UI setup

            // Initial control states
            button2.Enabled = false;
            btnViewReport.Visible = false;
            btnViewAnalysis.Visible = false;

            // Asynchronously ensure the Crystal Report Wrapper process is running
            UpdateStatus("Checking report service...");
            await EnsureWrapperIsRunningAsync(); // Check/Launch wrapper on load

            Logger.LogInfo("Form Load Initialisation Complete.");
            UpdateStatus("Ready");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Form1_Load: CRITICAL ERROR! Exception: {ex}");
            Logger.LogCritical($"CRITICAL ERROR during Form_Load: {ex.Message}", ex);
            MessageBox.Show($"A critical error occurred loading the application:\n\n{ex.Message}\n\nThe application may not function correctly.",
                            "Application Load Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            UpdateStatus("Error during load.");
            // Consider closing if load fails critically: this.Close();
        }
    }

    // Handles form closing: Cancel operations and terminate wrapper
    private void Form1_FormClosing(object sender, FormClosingEventArgs e)
    {
        // No CancellationTokenSource to cancel here as they are method-scoped now.
        // Terminate the wrapper process on exit
        TerminateWrapperProcess();
    }

    #endregion

    #region Event Handlers (Async)

    /// <summary>
    /// Button 1: Requests Crystal Report generation via Named Pipes.
    /// </summary>
    private async void Button1_Click(object sender, EventArgs e)
    {
        SetAllControlsEnabled(false); // Disable controls during operation
        button1.Text = "Requesting...";
        UpdateStatus("Validating request...");
        Logger.LogDebug("Button 1 Clicked: Requesting Crystal Report generation.");

        // --- CancellationToken ---
        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(6)); // 6-minute total timeout for this operation

        try
        {
            // --- Input Validations ---
            if (!ValidateInputDates()) { ResetUIOnError("Date Error"); return; }
            if (!ValidateFinancialYearSelection()) { ResetUIOnError("FY Mismatch"); return; } // Includes confirmation prompt
            if (string.IsNullOrEmpty(CrystalReportLocation) || !File.Exists(CrystalReportLocation))
            { throw new InvalidOperationException("Crystal Report location is invalid or file not found."); }
            if (string.IsNullOrEmpty(WrapperExePath) || !File.Exists(WrapperExePath))
            { throw new InvalidOperationException("Crystal Report Wrapper executable location is invalid or file not found."); }


            // --- Ensure Wrapper is Running ---
            UpdateStatus("Checking report service...");
            if (!await EnsureWrapperIsRunningAsync(cts.Token))
            { throw new InvalidOperationException($"Failed to start or connect to the report service ({WrapperProcessName})."); }


            // --- Prepare Request ---
            string reportOutputPath = ReportOutputLocation; // Get dynamic path
            var request = new ReportRequest
            {
                CrystalReportLocation = this.CrystalReportLocation, // Use property
                ReportOutputLocation = reportOutputPath,
                ReportDateFrom = datepickFrom.Value,
                ReportDateTo = datepickTo.Value
            };

            // --- Named Pipe Communication ---
            UpdateStatus("Connecting to report service...");
            Logger.LogInfo("Attempting Named Pipe communication...");
            ReportResponse? response = await SendRequestReceiveResponseAsync(request, cts.Token);

            // --- Process Response ---
            if (response?.Success == true && !string.IsNullOrEmpty(response.OutputPath) && File.Exists(response.OutputPath)) // Verify file exists
            {
                _generatedReportPath = response.OutputPath; // Store the path of the generated raw report
                Logger.LogInfo($"Report generated successfully by wrapper: {_generatedReportPath}");
                UpdateStatus("Report created successfully.");
                button1.Text = "Report Created"; // Indicate success but keep disabled
                button1.Enabled = false; // Keep button 1 disabled
                button2.Enabled = true;  // Enable button 2 for processing
                btnViewReport.Visible = true; // Show button to view raw report
                btnViewAnalysis.Visible = false; // Hide analysis button until processed
                _generatedAnalysisFilePath = string.Empty; // Clear previous analysis path

                // Re-enable other controls after Button 1 success
                SetOtherControlsEnabled(true);
            }
            else
            {
                string errorMessage = response?.ErrorMessage ?? "Unknown error from report service.";
                if (response?.Success == true && (string.IsNullOrEmpty(response.OutputPath) || !File.Exists(response.OutputPath)))
                {
                    errorMessage = $"Report service indicated success, but the output file path ('{response?.OutputPath ?? "NULL"}') is invalid or the file does not exist.";
                    Logger.LogError(errorMessage);
                }
                throw new Exception($"Report generation failed: {errorMessage}");
            }
        }
        catch (OperationCanceledException)
        {
            Logger.LogWarning("Report generation request cancelled or timed out.");
            MessageBox.Show("The report generation request timed out or was cancelled.", "Timeout / Cancelled", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            ResetUIOnError("Cancelled");
            UpdateStatus("Report request timed out/cancelled.");
        }
        catch (Exception ex)
        {
            Logger.LogError($"Error during Button 1 operation: {ex}");
            MessageBox.Show($"An error occurred while requesting the report:\n\n{ex.Message}", "Report Request Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            ResetUIOnError("Error");
            UpdateStatus($"Error: {ex.Message}");
        }
        // No finally block needed to re-enable controls here, ResetUIOnError handles it on failure.
        // On success, controls remain selectively enabled/disabled.
    }


    /// <summary>
    /// Button 2: Processes the generated Excel report and handles emailing.
    /// Includes check for existing final file.
    /// </summary>
    private async void Button2_Click(object sender, EventArgs e)
    {
        SetAllControlsEnabled(false); // Disable controls
        button2.Text = "Processing...";
        UpdateStatus("Starting Excel processing...");
        Logger.LogDebug("Button 2 Clicked: Processing Excel report.");

        // --- CancellationToken ---
        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(15));
        var token = cts.Token;

        // --- Progress Reporter ---
        var excelProgress = new Progress<ProgressReport>(report => UpdateStatus(report.Message));
        var emailProgress = new Progress<string>(message => UpdateStatus(message));

        string? finalFilePath = null;
        bool requiresManualRefresh = typeDropBox.SelectedIndex is 1 or 2 or 3; // Monthly, Quarterly, Annual
        string baseSaveLocation = ExcelFinalSaveLocation; // Base directory for Estimates
        int reportType = typeDropBox.SelectedIndex;

        try
        {
            // --- Check if Final File Already Exists ---
            string? expectedFinalPath = ExcelCopyData.GetExpectedFinalFilePath(reportType, baseSaveLocation);
            if (expectedFinalPath != null && File.Exists(expectedFinalPath))
            {
                Logger.LogWarning($"Expected final file already exists: {expectedFinalPath}");
                DialogResult dr = MessageBox.Show(
                    $"The report file '{Path.GetFileName(expectedFinalPath)}' already exists for this period.\n\n" +
                    "Do you want to skip processing and send this existing file?",
                    "File Already Exists",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (dr == DialogResult.Yes)
                {
                    Logger.LogInfo("User chose to send existing file.");
                    finalFilePath = expectedFinalPath; // Use the existing file path
                    _generatedAnalysisFilePath = finalFilePath; // Store path for View button
                    ShowViewAnalysisButton(true); // Show button to view the processed file

                    // Proceed directly to manual refresh check (if needed) and email
                    bool proceedToEmail = true;
                    if (requiresManualRefresh)
                    {
                        UpdateStatus("Waiting for manual Excel refresh...");
                        proceedToEmail = await HandleManualExcelRefreshAsync(finalFilePath, token);
                        if (!proceedToEmail)
                        {
                            UpdateStatus("Manual refresh/confirmation cancelled.");
                            ResetUIOnError("Cancelled"); // Reset UI as operation didn't fully complete
                            return; // Stop execution
                        }
                        UpdateStatus("Manual refresh confirmed. Preparing email...");
                    }
                    if (proceedToEmail)
                    {
                        await SendCompletionEmailAsync(finalFilePath, emailProgress, token);
                    }
                    return; // Exit Button2_Click as processing was skipped
                }
                else
                {
                    Logger.LogInfo("User chose to overwrite/regenerate the existing file.");
                    // Attempt to delete the existing file before processing
                    try
                    {
                        File.Delete(expectedFinalPath);
                        Logger.LogInfo($"Deleted existing file: {expectedFinalPath}");
                    }
                    catch (Exception delEx)
                    {
                        Logger.LogError($"Failed to delete existing file '{expectedFinalPath}': {delEx.Message}");
                        MessageBox.Show($"Could not delete the existing report file:\n{expectedFinalPath}\n\nPlease ensure the file is not open and try again.\n\nError: {delEx.Message}",
                                        "File Deletion Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        ResetUIOnError("File Error");
                        return; // Stop if deletion fails
                    }
                }
            }
            // --- End File Exists Check ---


            // --- Get Paths and Validate (if generating new) ---
            string reportToProcessPath = _generatedReportPath; // Use the path from Button 1's output
            string templatePath = ExcelTemplateLocation; // Get path based on current selection
            if (string.IsNullOrEmpty(templatePath) || !File.Exists(templatePath))
            {
                throw new FileNotFoundException($"The required Excel template file for the selected report type was not found.", templatePath);
            }

            if (string.IsNullOrEmpty(reportToProcessPath) || !File.Exists(reportToProcessPath))
            { throw new FileNotFoundException("The raw report file to process was not found. Please generate the report first (Button 1).", reportToProcessPath); }

            if (string.IsNullOrEmpty(baseSaveLocation))
            { throw new InvalidOperationException("The base save location for the final report is not configured correctly."); }

            // --- Get Parameters ---
            string financialYear = finYearDropBox.SelectedItem?.ToString() ?? _financialYear; // Use selected or default FY
            string sourceSheet = "Sheet1"; // Assuming Crystal exports to Sheet1
            string destSheet = "DATA";     // Target sheet in the template

            // --- Call Async Excel Processing ---
            finalFilePath = await ExcelCopyData.ProcessExcelReportAsync(
                financialYear, reportType,
                reportToProcessPath, sourceSheet, baseSaveLocation, templatePath, destSheet,
                1, 1, // startRow, startCol (adjust if Crystal export format changes)
                excelProgress, token // Pass the correct progress reporter
            );

            if (string.IsNullOrEmpty(finalFilePath) || !File.Exists(finalFilePath))
            {
                // Check if cancellation was the reason
                if (token.IsCancellationRequested)
                {
                    throw new OperationCanceledException("Excel processing was cancelled.");
                }
                else
                {
                    throw new Exception("Excel processing failed to produce a final file. Check logs for details.");
                }
            }

            _generatedAnalysisFilePath = finalFilePath; // Store path of the processed file
            Logger.LogInfo($"Excel processing completed. Final file: {finalFilePath}");
            UpdateStatus("Excel processing complete.");
            ShowViewAnalysisButton(true); // Show button to view the processed file

            // --- Manual Refresh Step (if required) ---
            bool proceedToEmailAfterGenerate = true;
            if (requiresManualRefresh)
            {
                UpdateStatus("Waiting for manual Excel refresh...");
                proceedToEmailAfterGenerate = await HandleManualExcelRefreshAsync(finalFilePath, token);
                if (!proceedToEmailAfterGenerate)
                {
                    UpdateStatus("Manual refresh/confirmation cancelled.");
                    ResetUIOnError("Cancelled"); // Reset UI as operation didn't fully complete
                    return; // Stop execution
                }
                UpdateStatus("Manual refresh confirmed. Preparing email...");
            }

            // --- Send Email ---
            if (proceedToEmailAfterGenerate)
            {
                // Pass the emailProgress reporter here
                await SendCompletionEmailAsync(finalFilePath, emailProgress, token);
                // UI state (Completed or Error) is handled within SendCompletionEmailAsync
            }
        }
        catch (OperationCanceledException)
        {
            Logger.LogWarning("Excel processing or subsequent step cancelled.");
            UpdateStatus("Operation Cancelled.");
            ResetUIOnError("Cancelled");
        }
        catch (FileNotFoundException fnfEx)
        {
            Logger.LogError($"File not found during Button 2 operation: {fnfEx}");
            MessageBox.Show(fnfEx.Message, "File Not Found Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            UpdateStatus("Error: Required file not found.");
            ResetUIOnError("File Error");
        }
        catch (Exception ex)
        {
            Logger.LogError($"Error during Button 2 operation: {ex}");
            MessageBox.Show($"An unexpected error occurred during processing:\n\n{ex.Message}", "Processing Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            UpdateStatus("Error during processing.");
            ResetUIOnError("Error");
        }
        // No finally block needed for UI reset, handled by ResetUIOnError or SetUICompleted within called methods.
    }


    /// <summary>
    /// Handles the click event of the "View Report" button (views raw Crystal output).
    /// </summary>
    private void btnViewReport_Click(object sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(_generatedReportPath))
        {
            Logger.LogWarning("View Report clicked, but no raw report path is available.");
            MessageBox.Show("The raw report file path is not available. Please generate the report first.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        OpenFileHelper(_generatedReportPath, "raw report output");
    }

    /// <summary>
    /// Handles the click event of the "View Analysis" button (views final processed file).
    /// </summary>
    private void btnViewAnalysis_Click(object sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(_generatedAnalysisFilePath))
        {
            Logger.LogWarning("View Analysis clicked, but no processed file path is available.");
            MessageBox.Show("The processed analysis file path is not available. Please process the report first (Button 2).", "Information", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        OpenFileHelper(_generatedAnalysisFilePath, "processed analysis file");
    }

    /// <summary>
    /// Handles changes in the Report Type dropdown, updating date ranges and UI visibility.
    /// </summary>
    private void typeDropBox_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (sender is not ComboBox comboBox || comboBox.SelectedItem == null) return;

        int selectedIndex = comboBox.SelectedIndex;
        DateTime todayValue = _today; // Use the value set during Load

        // Determine date range and FY visibility based on selection
        var (dateFrom, dateTo, showFinYear) = selectedIndex switch
        {
            // 0: Weekly (Last 2 weeks including today, show FY for context)
            0 => (todayValue.AddDays(-15), todayValue, true),
            // 1: Monthly (Previous full month if run <= 15th, else current month's period. Hide FY.)
            1 => CalculateMonthlyRange(todayValue),
            // 2: Quarterly (Previous full quarter. Hide FY.)
            2 => CalculateQuarterlyRange(todayValue),
            // 3: Annual (Previous full calendar year. Hide FY.)
            3 => (new DateTime(todayValue.Year - 1, 1, 1), new DateTime(todayValue.Year - 1, 12, 31), false),
            // Default: Same as Weekly
            _ => (todayValue.AddDays(-13), todayValue, true)
        };

        Logger.LogInfo($"Report Type changed (Index {selectedIndex}). Range: {dateFrom:d} to {dateTo:d}. ShowFinYear: {showFinYear}");

        // Update UI elements safely
        SafeControlUpdate(datepickFrom, () => datepickFrom.Value = dateFrom);
        SafeControlUpdate(datepickTo, () => datepickTo.Value = dateTo);
        SafeControlUpdate(label5, () => label5.Visible = showFinYear); // FY Label
        SafeControlUpdate(finYearDropBox, () =>
        {
            finYearDropBox.Visible = showFinYear;
            finYearDropBox.Enabled = showFinYear; // Enable/disable along with visibility
            if (showFinYear && finYearDropBox.Items.Count == 0)
            {
                PopulateFinancialYearDropdown(); // Repopulate if needed and visible
            }
            // Ensure correct template path is considered if template depends on type
            // (ExcelTemplateLocation property already handles this based on typeDropBox.SelectedIndex)
            Logger.LogDebug($"Template path for type {selectedIndex}: {ExcelTemplateLocation}");
            // Validate template existence for the *newly selected* type
            string currentTemplatePath = ExcelTemplateLocation;
            if (string.IsNullOrEmpty(currentTemplatePath) || !File.Exists(currentTemplatePath))
            {
                Logger.LogWarning($"Excel template file for the selected report type ({comboBox.SelectedItem}) is missing or invalid ('{currentTemplatePath}'). Processing might fail.");
                // Optionally show a non-blocking warning or visual indicator
            }
        });

        // Reset dependent buttons when type changes
        ResetButtonStatesAfterTypeChange();
    }

    #endregion

    #region Named Pipe Communication (Async)

    /// <summary>
    /// Sends a request object via named pipe and awaits a response object.
    /// Handles serialization, length-prefixing, and timeouts.
    /// </summary>
    private async Task<ReportResponse?> SendRequestReceiveResponseAsync(ReportRequest request, CancellationToken cancellationToken)
    {
        const string pipeName = "CrystalReportPipe";
        const int connectTimeoutMs = 5000; // 5 seconds to connect
        // Timeout for the entire operation (including waiting for response) is handled by the CancellationToken passed in

        Logger.LogDebug($"Connecting to named pipe '{pipeName}'...");

        // Use await using for automatic disposal
        await using var pipeClient = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);

        try
        {
            // Connect with timeout and cancellation
            await pipeClient.ConnectAsync(connectTimeoutMs, cancellationToken);
            Logger.LogInfo("Connected to pipe server.");
            UpdateStatus("Connected. Sending request...");

            // --- Send Request (Length-Prefixed) ---
            string requestJson = JsonConvert.SerializeObject(request);
            byte[] requestBytes = Encoding.UTF8.GetBytes(requestJson);
            byte[] lengthBytes = BitConverter.GetBytes(requestBytes.Length); // 4-byte length prefix

            // Write length, then message
            await pipeClient.WriteAsync(lengthBytes, cancellationToken);
            await pipeClient.WriteAsync(requestBytes, cancellationToken);
            await pipeClient.FlushAsync(cancellationToken); // Ensure data is sent
            Logger.LogDebug($"Sent request ({requestBytes.Length} bytes): {requestJson}");
            UpdateStatus("Request sent. Waiting for response...");

            // --- Read Response (Length-Prefixed) ---
            // 1. Read the 4-byte length prefix
            byte[] responseLengthBuffer = new byte[4];
            int bytesRead = await ReadPipeAsync(pipeClient, responseLengthBuffer, 0, 4, cancellationToken);
            if (bytesRead < 4) throw new IOException("Failed to read full response length prefix from service.");

            int responseLength = BitConverter.ToInt32(responseLengthBuffer, 0);
            if (responseLength <= 0 || responseLength > 10 * 1024 * 1024) // Basic validation (e.g., max 10MB response)
            { throw new IOException($"Invalid response length received: {responseLength}"); }
            Logger.LogDebug($"Expecting response length: {responseLength}");

            // 2. Read the actual response message
            byte[] responseBuffer = new byte[responseLength];
            bytesRead = await ReadPipeAsync(pipeClient, responseBuffer, 0, responseLength, cancellationToken);
            if (bytesRead < responseLength) throw new IOException("Failed to read complete response message from service.");

            // 3. Decode and Deserialize
            string responseJson = Encoding.UTF8.GetString(responseBuffer);
            Logger.LogDebug($"Received response ({responseLength} bytes): {responseJson}");

            var response = JsonConvert.DeserializeObject<ReportResponse>(responseJson);
            if (response == null) throw new InvalidDataException("Failed to deserialize response JSON.");

            UpdateStatus("Response received.");
            return response;
        }
        catch (TimeoutException ex) // Catch specific timeout from ConnectAsync
        {
            Logger.LogError($"Timeout connecting to named pipe server '{pipeName}'.");
            throw new TimeoutException($"Connection to the report service timed out. Ensure '{WrapperProcessName}' is running.", ex);
        }
        catch (IOException ex) // Catch pipe-related errors
        {
            Logger.LogError($"IO Error communicating with named pipe server: {ex.Message}");
            throw new IOException($"Communication error with the report service: {ex.Message}", ex);
        }
        catch (OperationCanceledException) // Catch cancellation
        {
            Logger.LogWarning("Named pipe communication cancelled.");
            throw; // Re-throw cancellation
        }
        catch (Exception ex) // Catch other potential errors (serialization, etc.)
        {
            Logger.LogError($"Unexpected error during named pipe communication: {ex}");
            throw new Exception($"An unexpected error occurred communicating with the report service: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Helper to read exact number of bytes from pipe with cancellation.
    /// </summary>
    private async Task<int> ReadPipeAsync(PipeStream pipe, byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        int totalBytesRead = 0;
        while (totalBytesRead < count)
        {
            // ReadAsync can return 0 if the pipe is closed gracefully.
            int bytesRead = await pipe.ReadAsync(buffer, offset + totalBytesRead, count - totalBytesRead, cancellationToken);
            if (bytesRead == 0)
            {
                // Pipe closed before reading expected amount
                throw new EndOfStreamException("The pipe connection was closed prematurely while reading data.");
            }
            totalBytesRead += bytesRead;
        }
        return totalBytesRead;
    }


    #endregion

    #region Manual Excel Refresh Handling (Async)

    /// <summary>
    /// Handles the process of opening Excel for manual pivot refresh, waiting, and confirming continuation.
    /// </summary>
    /// <returns>True if the user confirms to proceed after closing Excel, false otherwise (or on error/cancellation).</returns>
    private async Task<bool> HandleManualExcelRefreshAsync(string filePath, CancellationToken token)
    {
        Process? excelProcess = null;
        bool userConfirmedProceed = false;

        try
        {
            // Check if other Excel instances are running
            if (await Task.Run(() => Process.GetProcessesByName("EXCEL").Length > 0, token))
            {
                DialogResult closeResult = MessageBox.Show(
                    "Other Excel instances are currently running. It's recommended to close all Excel windows before proceeding to avoid issues.\n\nDo you want the application to attempt closing them?",
                    "Close Other Excel Instances?",
                    MessageBoxButtons.YesNoCancel,
                    MessageBoxIcon.Warning);

                if (closeResult == DialogResult.Cancel) return false; // User cancelled the operation
                if (closeResult == DialogResult.Yes)
                {
                    UpdateStatus("Attempting to close other Excel instances...");
                    await Task.Run(() => CloseExcelProcesses(), token); // Run synchronous kill loop in background
                    await Task.Delay(1500, token); // Brief pause after attempting closure
                    UpdateStatus("Ready to open report for manual refresh.");
                }
                // If 'No', proceed with caution
            }

            // Prompt user before opening
            MessageBox.Show(
                "The processed report will now open in Excel.\n\n" +
                "*** IMPORTANT ***\n" +
                "1. Go to the Pivot sheets in Excel.\n" +
                "2. Right click 'Refresh' for all pivot tables and slicers.\n" +
                "3. Wait for the refresh to complete.\n" +
                "4. SAVE the file!\n" +
                "5. CLOSE Excel completely.\n\n" +
                "The application will wait until Excel is closed and continue.",
                "Manual Refresh Required",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            token.ThrowIfCancellationRequested(); // Check before starting process

            // Start Excel process
            UpdateStatus("Opening Excel...");
            excelProcess = await Task.Run(() =>
            {
                try
                {
                    // UseShellExecute = true allows opening with default application
                    return Process.Start(new ProcessStartInfo(filePath) { UseShellExecute = true });
                }
                catch (Exception ex)
                {
                    Logger.LogError($"Failed to start Excel process for '{filePath}': {ex.Message}");
                    return null; // Return null if starting fails
                }
            }, token);

            if (excelProcess == null)
            {
                throw new Exception($"Failed to open the Excel file: {filePath}. Ensure Excel is installed and the file is accessible.");
            }

            UpdateStatus("Excel opened. Waiting for you to Refresh All, Save, and Close...");
            Logger.LogInfo($"Waiting for Excel process (ID: {excelProcess.Id}) to exit...");

            // Asynchronously wait for the process to exit
            await excelProcess.WaitForExitAsync(token); // Requires .NET 5+

            // Alternative for older .NET:
            // await Task.Run(() => excelProcess.WaitForExit(), token);

            token.ThrowIfCancellationRequested(); // Check immediately after exit/cancellation

            Logger.LogInfo("Excel process has exited.");
            UpdateStatus("Excel closed.");

            // Confirm if user wants to proceed (e.g., send email)
            DialogResult sendResult = MessageBox.Show(
                "Excel has been closed.\n\nDo you want to proceed with sending the email now?",
                "Confirm Email Send",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            userConfirmedProceed = (sendResult == DialogResult.Yes);
            return userConfirmedProceed; // Return user's choice
        }
        catch (OperationCanceledException)
        {
            Logger.LogWarning("Manual Excel refresh step cancelled.");
            UpdateStatus("Manual refresh cancelled.");
            return false; // Indicate cancellation
        }
        catch (Exception ex)
        {
            Logger.LogError($"Error during manual Excel handling: {ex}");
            MessageBox.Show($"An error occurred while managing the Excel refresh process:\n\n{ex.Message}", "Excel Interaction Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            UpdateStatus("Error during manual refresh.");
            return false; // Indicate error
        }
        finally
        {
            // Ensure the process handle is disposed
            excelProcess?.Dispose();
        }
    }

    /// <summary>
    /// Attempts to gracefully close then kill running Excel processes. Synchronous method.
    /// </summary>
    private static void CloseExcelProcesses()
    {
        Process[] processes;
        try
        {
            processes = Process.GetProcessesByName("EXCEL");
        }
        catch (Exception ex)
        {
            Logger.LogError($"Error getting Excel processes: {ex.Message}");
            return; // Cannot proceed
        }

        if (processes.Length == 0)
        {
            Logger.LogInfo("No running Excel processes found to close.");
            return;
        }

        Logger.LogInfo($"Found {processes.Length} Excel processes. Attempting to close...");
        foreach (var process in processes)
        {
            using (process) // Ensure disposal
            {
                try
                {
                    if (!process.HasExited)
                    {
                        Logger.LogInfo($"Attempting to close Excel process ID: {process.Id} (MainWindowTitle: '{process.MainWindowTitle}')");

                        // Try closing gracefully first (might not work for all scenarios)
                        // bool closed = process.CloseMainWindow();
                        // if (closed)
                        // {
                        //     if (process.WaitForExit(3000)) // Wait 3 seconds
                        //     {
                        //         Logger.LogInfo($"Gracefully closed Excel process ID: {process.Id}");
                        //         continue; // Move to next process
                        //     }
                        //     Logger.LogWarning($"Process {process.Id} did not exit after CloseMainWindow.");
                        // }
                        // else if (!process.HasExited) // Check if it exited anyway
                        // {
                        //     Logger.LogWarning($"CloseMainWindow failed or window not found for process {process.Id}.");
                        // }

                        // If graceful close failed or wasn't attempted, force kill
                        Logger.LogWarning($"Forcing termination (Kill) for Excel process ID: {process.Id}");
                        process.Kill(true); // Kill entire process tree
                        process.WaitForExit(5000); // Wait up to 5 seconds for termination
                        if (process.HasExited)
                            Logger.LogInfo($"Successfully terminated Excel process ID: {process.Id}");
                        else
                            Logger.LogWarning($"Excel process ID: {process.Id} did not terminate after Kill.");

                    }
                }
                catch (InvalidOperationException ex) when (ex.Message.Contains("Process has exited"))
                {
                    Logger.LogInfo($"Excel process ID: {process.Id} already exited.");
                }
                catch (Exception ex)
                {
                    // Log errors during closure/kill (e.g., access denied)
                    Logger.LogError($"Error closing/killing Excel process ID {process.Id}: {ex.Message}");
                }
            }
        }
        Logger.LogInfo("Finished attempting to close Excel processes.");
    }


    #endregion

    #region Wrapper Process Handling (Async Check/Launch)

    /// <summary>
    /// Asynchronously ensures the wrapper process is running, launching it if necessary.
    /// </summary>
    /// <returns>True if the wrapper is running or was successfully launched, false otherwise.</returns>
    private async Task<bool> EnsureWrapperIsRunningAsync(CancellationToken cancellationToken = default)
    {
        // Check if running (synchronous check is usually okay here)
        if (IsWrapperRunning())
        {
            Logger.LogInfo($"Wrapper process '{WrapperProcessName}' is already running.");
            return true;
        }

        Logger.LogWarning($"Wrapper process '{WrapperProcessName}' not found. Attempting to launch...");
        UpdateStatus("Starting report service...");

        try
        {
            // Launch the process (synchronous start)
            await Task.Run(() => LaunchWrapper(), cancellationToken); // Use Task.Run if LaunchWrapper has potential to block

            // Wait briefly for the process to initialize
            await Task.Delay(3000, cancellationToken); // 3 seconds grace period

            // Check again after attempting launch
            if (IsWrapperRunning())
            {
                Logger.LogInfo($"Wrapper process '{WrapperProcessName}' appears to be running after launch.");
                UpdateStatus("Report service started.");
                return true;
            }
            else
            {
                Logger.LogError($"Wrapper process '{WrapperProcessName}' did not start successfully after launch attempt.");
                UpdateStatus("Error: Failed to start report service.");
                return false;
            }
        }
        catch (OperationCanceledException)
        {
            Logger.LogWarning("Operation cancelled during wrapper launch check.");
            UpdateStatus("Operation cancelled.");
            return false;
        }
        catch (Exception launchEx)
        {
            Logger.LogError($"Failed to launch the Crystal Report Wrapper ('{WrapperExePath}'): {launchEx.Message}", launchEx);
            MessageBox.Show($"Could not start the required report service ({WrapperProcessName}).\n" +
                            $"Please check the path in configuration ('{WrapperExePath}') and ensure the application exists and has permissions to run.\n\n" +
                            $"Error: {launchEx.Message}",
                            "Wrapper Launch Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            UpdateStatus("Error: Failed to start report service.");
            SafeControlUpdate(button1, () => button1.Enabled = false); // Disable report creation if wrapper fails
            return false;
        }
    }

    /// <summary>
    /// Checks if the wrapper process is currently running by name. Synchronous.
    /// </summary>
    private bool IsWrapperRunning()
    {
        string processName = this.WrapperProcessName;
        if (string.IsNullOrEmpty(processName))
        {
            Logger.LogError("Wrapper process name is not configured. Cannot check if running.");
            return false;
        }
        try
        {
            // GetProcessesByName can be slow if many processes exist, but generally acceptable for UI thread here.
            // Consider Task.Run if it causes noticeable UI lag.
            Process[] processes = Process.GetProcessesByName(processName);
            bool isRunning = processes.Length > 0;
            // Dispose process handles returned by GetProcessesByName
            foreach (var p in processes) p.Dispose();
            return isRunning;
        }
        catch (Exception ex)
        {
            Logger.LogError($"Error checking for wrapper process '{processName}': {ex.Message}");
            return false; // Assume not running if check fails
        }
    }

    /// <summary>
    /// Launches the wrapper executable. Synchronous.
    /// </summary>
    /// <exception cref="FileNotFoundException">Thrown if the executable is not found.</exception>
    /// <exception cref="Exception">Thrown if process start fails.</exception>
    private void LaunchWrapper()
    {
        string exePath = this.WrapperExePath;
        if (!File.Exists(exePath))
        {
            throw new FileNotFoundException($"Wrapper executable not found at the configured path: {exePath}");
        }

        try
        {
            Logger.LogInfo($"Launching wrapper: {exePath}");
            // UseShellExecute = true is often better for launching external EXEs
            // WorkingDirectory helps if the EXE depends on files in its own directory
            var startInfo = new ProcessStartInfo(exePath)
            {
                WorkingDirectory = Path.GetDirectoryName(exePath) ?? string.Empty,
                UseShellExecute = true // Recommended for launching external apps
            };
            Process.Start(startInfo); // Returns Process object, but we don't wait for it here
            Logger.LogInfo($"Wrapper launch command initiated for '{exePath}'.");
        }
        catch (Exception ex)
        {
            Logger.LogError($"Failed to start wrapper process '{exePath}': {ex.Message}", ex);
            // Wrap in a more specific exception for the caller
            throw new Exception($"Failed to start the wrapper process '{exePath}'. Check permissions and path.", ex);
        }
    }

    /// <summary>
    /// Attempts to find and terminate the wrapper process on application exit. Synchronous.
    /// </summary>
    private void TerminateWrapperProcess()
    {
        string processName = this.WrapperProcessName;
        if (string.IsNullOrEmpty(processName)) return; // Nothing to terminate

        Logger.LogInfo($"Attempting to terminate wrapper process '{processName}' on application exit...");
        Process[] processes;
        try
        {
            processes = Process.GetProcessesByName(processName);
        }
        catch (Exception ex)
        {
            Logger.LogError($"Error finding wrapper processes '{processName}' to terminate: {ex.Message}");
            return;
        }


        if (processes.Length == 0)
        {
            Logger.LogInfo("Wrapper process not found, likely already closed.");
            return;
        }

        foreach (var process in processes)
        {
            using (process) // Ensure disposal
            {
                try
                {
                    if (!process.HasExited)
                    {
                        Logger.LogInfo($"Terminating wrapper process ID: {process.Id}");
                        process.Kill(true); // Kill process and its descendants
                        process.WaitForExit(2000); // Wait briefly for termination
                        if (process.HasExited)
                            Logger.LogInfo($"Wrapper process {process.Id} terminated.");
                        else
                            Logger.LogWarning($"Wrapper process {process.Id} did not terminate after Kill.");
                    }
                }
                catch (Exception ex)
                {
                    // Log errors during termination (e.g., access denied)
                    Logger.LogWarning($"Error terminating wrapper process ID {process.Id}: {ex.Message}");
                }
            }
        }
        Logger.LogInfo("Finished attempting to terminate wrapper processes.");
    }

    #endregion

    #region Email Handling (Async)

    /// <summary>
    /// Prepares and sends the completion email with the final report attached.
    /// Handles UI updates for completion or error.
    /// </summary>
    private async Task SendCompletionEmailAsync(string attachmentPath, IProgress<string> progress, CancellationToken cancellationToken)
    {
        Logger.LogInfo($"Preparing completion email with attachment: {attachmentPath}");
        UpdateStatus("Preparing email...");

        if (!File.Exists(attachmentPath))
        {
            // Log the error before throwing
            Logger.LogError($"Attachment file not found: {attachmentPath}");
            throw new FileNotFoundException("The attachment file for the email was not found.", attachmentPath);
        }

        try
        {
            // Determine Recipients based on Checkbox and Build Config
            var (toAddresses, ccAddresses) = GetEmailRecipients();
            if (!toAddresses.Any() && !ccAddresses.Any())
            {
                // Log the error before throwing
                Logger.LogError("No valid email recipients configured or found. Cannot send email.");
                throw new InvalidOperationException("No valid email recipients configured or found. Cannot send email.");
            }

            // Determine Subject and Body based on Report Type
            var (subject, body) = GetEmailSubjectAndBody();

            // Use the injected/created EmailUtility instance
            UpdateStatus("Sending email...");
            bool success = await _emailUtility.SendEmailAsync(
                toAddresses,        // Arg 1
                ccAddresses,        // Arg 2
                subject,            // Arg 3
                body,               // Arg 4
                attachmentPath,     // Arg 5
                progress,           // Arg 6
                cancellationToken); // Arg 7

            // Handle completion (Update UI on UI thread)
            if (success)
            {
                Logger.LogInfo("Email sent successfully.");
                // Keep the success message for a moment before resetting
                UpdateStatus("Email Sent - Report Completed.");
                await Task.Delay(1500, cancellationToken); // Optional delay to show "Completed" status
                SetUICompleted(); // Reset UI to initial state
            }
            else
            {
                // If SendEmailAsync returns false (e.g., due to cancellation handled internally in EmailUtility)
                // We should reset the UI unless the cancellation token passed *into this method* was triggered.
                if (!cancellationToken.IsCancellationRequested)
                {
                    Logger.LogError("Email sending failed (SendEmailAsync returned false). Check previous logs.");
                    throw new Exception("Email sending failed. Check logs for details.");
                }
                // If the token passed to this method was cancelled, the OperationCanceledException handler below will catch it.
            }
        }
        catch (OperationCanceledException)
        {
            Logger.LogWarning("Email sending was cancelled.");
            UpdateStatus("Email sending cancelled.");
            ResetUIOnError("Cancelled"); // Reset UI on cancellation
                                         // Do not re-throw here, let the caller (Button2_Click) handle its own cancellation flow.
        }
        catch (Exception ex) // Catches exceptions from GetRecipients, GetSubjectBody, SendEmailAsync, or the logic above
        {
            Logger.LogError($"Error sending completion email: {ex}");
            MessageBox.Show($"Failed to send the completion email:\n\n{ex.Message}", "Email Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            UpdateStatus($"Email Error: {ex.Message}");
            ResetUIOnError("Email Failed"); // Reset UI on email failure
            // Do not re-throw here, as we've handled the UI reset
        }
    }

    /// <summary>
    /// Determines email recipients based on the checkbox state and build configuration.
    /// </summary>
    private (List<string> To, List<string> Cc) GetEmailRecipients()
    {
        List<string> toAddresses = [];
        List<string> ccAddresses = [];
        bool sendToFemiOnly = checkBox1.Checked; // Get current state

#if DEBUG
        // --- DEBUG Build Recipients (Updated per user request) ---
        Logger.LogInfo("DEBUG Build: Using updated debug email recipients.");
        toAddresses.Add(_configuration["settings:DebugEmails:To"] ?? "chrisp@harlowsolutions.co.uk"); // Default TO
        string debugCC = sendToFemiOnly
            ? (_configuration["settings:DebugEmails:CC2"] ?? "chrisp@harlowsolutions.co.uk") // CC if FemiOnly checked
            : (_configuration["settings:DebugEmails:CC1"] ?? "jamier@harlowsolutions.co.uk"); // CC if FemiOnly unchecked
        if (!string.IsNullOrWhiteSpace(debugCC)) ccAddresses.Add(debugCC);

#else
            // --- RELEASE Build Recipients ---
             Logger.LogInfo($"RELEASE Build: SendToFemiOnly = {sendToFemiOnly}");
            if (sendToFemiOnly)
            {
                // Send only to Femi (and specific CCs)
                toAddresses.Add(_configuration["settings:ProductionEmails:FemiTo"] ?? "femi@harlowsolutions.co.uk");
                ccAddresses = GetStringListFromConfig("settings:ProductionEmails:FemiCC")
                                ?? ["ITdept@harlowsolutions.co.uk"]; // Default CC for Femi
                 Logger.LogInfo("Sending to Femi (and FemiCC list).");
            }
            else
            {
                // Send to the main team list
                toAddresses = GetStringListFromConfig("settings:ProductionEmails:TeamTo")
                                ?? ["andrewp@harlowsolutions.co.uk", "kirstym@harlowsolutions.co.uk", "stuartm@harlowsolutions.co.uk"]; // Default To
                ccAddresses = GetStringListFromConfig("settings:ProductionEmails:TeamCC")
                                ?? ["emmanuel@harlowsolutions.co.uk", "femi@harlowsolutions.co.uk", "jackh@harlowsolutions.co.uk", "pauls@harlowsolutions.co.uk", "ITdept@harlowsolutions.co.uk", "gordonb@harlowsolutions.co.uk"]; // Default CC
                 Logger.LogInfo("Sending to Team list.");
            }
#endif

        // Log the final recipient lists
        Logger.LogDebug($"To Addresses: {string.Join("; ", toAddresses)}");
        Logger.LogDebug($"CC Addresses: {string.Join("; ", ccAddresses)}");

        return (toAddresses, ccAddresses);
    }

    /// <summary>
    /// Reads a list of strings from configuration, splitting by common delimiters.
    /// </summary>
    private List<string>? GetStringListFromConfig(string key)
    {
        string? configValue = _configuration[key];
        if (string.IsNullOrWhiteSpace(configValue))
        {
            return null;
        }
        // Split by comma, semicolon, or space, trim entries, remove empty ones
        return configValue.Split([',', ';', ' '], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                          .ToList();
    }

    /// <summary>
    /// Generates the email subject and body based on the selected report type and date range.
    /// </summary>
    private (string Subject, string Body) GetEmailSubjectAndBody()
    {
        string reportTypeName = "Estimate Success Rate";
        string greeting = IsDebug ? "Hi Debug," : (checkBox1.Checked ? "Hi Femi," : "Hi All,");
        string dateRangeInfo = string.Empty;
        string subjectPrefix = string.Empty;

        // Use the date pickers' current values for email text
        DateTime fromDate = datepickFrom.Value;
        DateTime toDate = datepickTo.Value;

        switch (typeDropBox.SelectedIndex)
        {
            case 0: // Weekly
                subjectPrefix = $"Weekly {reportTypeName}";
                // Use a relative description for weekly
                dateRangeInfo = $"for the period ending {toDate:dd MMM yyyy}"; // Added year for clarity
                break;
            case 1: // Monthly
                subjectPrefix = $"Monthly {reportTypeName}";
                // Use the month/year of the 'from' date for clarity
                dateRangeInfo = $"for {fromDate:MMMM yyyy}";
                break;
            case 2: // Quarterly
                subjectPrefix = $"Quarterly {reportTypeName}";
                dateRangeInfo = $"for {GetQuarterString(fromDate)} {fromDate.Year}";
                break;
            case 3: // Annual
                subjectPrefix = $"Annual {reportTypeName}";
                dateRangeInfo = $"for {fromDate.Year}";
                break;
            default:
                subjectPrefix = reportTypeName; // Generic fallback
                dateRangeInfo = $"from {fromDate:d} to {toDate:d}";
                break;
        }

        string subject = $"{subjectPrefix} Report ({DateTime.Today:yyyy-MM-dd})"; // Add run date to subject
        string body = $"{greeting}\n\nPlease find attached the {subjectPrefix} report {dateRangeInfo}.\n\nThis report includes quotes data for review.\n\nThank you,\nAutomation Service";

        return (subject, body);
    }


    #endregion

    #region Helper Methods (UI Updates, Validation, File Handling, etc.)

    // --- UI Update Helpers ---

    /// <summary>
    /// Safely updates a control's property or state, marshalling to the UI thread if required.
    /// </summary>
    private void SafeControlUpdate(Control ctrl, Action action)
    {
        ArgumentNullException.ThrowIfNull(action);
        if (ctrl == null || ctrl.IsDisposed || !ctrl.IsHandleCreated)
        {
            // Logger.LogWarning($"Control '{ctrl?.Name ?? "Unknown"}' not available for update.");
            return; // Ignore if control is not valid
        }

        if (ctrl.InvokeRequired)
        {
            try
            {
                // Use BeginInvoke for potentially faster UI response, Invoke waits for completion
                ctrl.BeginInvoke(action);
                // ctrl.Invoke(action); // Use Invoke if subsequent code depends on the update being complete
            }
            catch (ObjectDisposedException) { /* Ignore if disposed during invoke */ }
            catch (InvalidOperationException ex) when (ex.Message.Contains("Invoke"))
            {
                Logger.LogWarning($"SafeControlUpdate ignored invoke error: {ex.Message}");
            }
            catch (Exception ex) // Catch other potential exceptions during invoke
            {
                Logger.LogError($"Unexpected error during SafeControlUpdate Invoke/BeginInvoke: {ex}");
            }
        }
        else
        {
            try
            {
                action(); // Execute directly if already on UI thread
            }
            catch (Exception ex)
            {
                Logger.LogError($"Unexpected error during SafeControlUpdate direct action: {ex}");
            }
        }
    }

    /// <summary>
    /// Updates the text of the first ToolStripStatusLabel in statusStrip1 safely.
    /// </summary>
    private void UpdateStatus(string message)
    {
        SafeControlUpdate(statusStrip1, () =>
        {
            if (statusStrip1.Items.Count > 0 && statusStrip1.Items[0] is ToolStripStatusLabel label)
            {
                label.Text = message;
            }
            else
            {
                // Handle case where status strip isn't set up correctly (optional)
                // Logger.LogWarning("Could not update status: StatusStrip or first item not found/valid.");
            }
        });
    }

    /// <summary>
    /// Enables or disables all primary user input controls safely.
    /// </summary>
    private void SetAllControlsEnabled(bool enable)
    {
        SafeControlUpdate(button1, () => button1.Enabled = enable);
        SafeControlUpdate(button2, () => button2.Enabled = enable); // Also manage button2 state here
        SetOtherControlsEnabled(enable); // Use helper for the rest
    }

    /// <summary>
    /// Enables or disables controls other than the main action buttons.
    /// </summary>
    private void SetOtherControlsEnabled(bool enable)
    {
        SafeControlUpdate(typeDropBox, () => typeDropBox.Enabled = enable);
        SafeControlUpdate(datepickFrom, () => datepickFrom.Enabled = enable);
        SafeControlUpdate(datepickTo, () => datepickTo.Enabled = enable);
        // Enable FinYear only if it should be visible AND controls are being enabled
        SafeControlUpdate(finYearDropBox, () => finYearDropBox.Enabled = enable && finYearDropBox.Visible);
        SafeControlUpdate(checkBox1, () => checkBox1.Enabled = enable);
    }


    /// <summary>
    /// Resets the UI to an initial or error state. Called on errors or cancellations.
    /// </summary>
    /// <param name="button1Text">Text to set for Button 1 (e.g., "Create Report", "Error", "Cancelled").</param>
    private void ResetUIOnError(string button1Text = "Create Report")
    {
        SafeControlUpdate(this, () => // Update multiple controls within one Invoke/direct call
        {
            Logger.LogDebug($"Resetting UI state. Button 1 text: '{button1Text}'");
            SetAllControlsEnabled(true); // Re-enable most controls
            button1.Text = button1Text;
            // Re-enable button 1 only if config is valid
            button1.Enabled = !(string.IsNullOrEmpty(CrystalReportLocation) || !File.Exists(CrystalReportLocation) || string.IsNullOrEmpty(WrapperExePath) || !File.Exists(WrapperExePath));


            button2.Text = "Process & Email";
            // Only enable Button 2 if Button 1 previously succeeded AND the raw report file still exists
            bool rawReportExists = !string.IsNullOrEmpty(_generatedReportPath) && File.Exists(_generatedReportPath);
            button2.Enabled = rawReportExists;
            if (!rawReportExists) Logger.LogDebug("Button 2 remains disabled as raw report path is missing or file doesn't exist.");


            // Hide view buttons if the corresponding files aren't available
            btnViewReport.Visible = rawReportExists;
            btnViewAnalysis.Visible = !string.IsNullOrEmpty(_generatedAnalysisFilePath) && File.Exists(_generatedAnalysisFilePath);

            UpdateStatus("Ready"); // Reset status message
        });
    }

    /// <summary>
    /// Sets the UI state after the entire process completes successfully.
    /// Resets buttons and enables controls for a new run.
    /// </summary>
    private void SetUICompleted()
    {
        SafeControlUpdate(this, () =>
        {
            Logger.LogDebug("Setting UI to completed state (re-enabling controls).");

            // Re-enable all standard input controls
            SetOtherControlsEnabled(true);

            // Reset Button 1 text and enable if config is valid
            button1.Text = "Create Report";
            button1.Enabled = !(string.IsNullOrEmpty(CrystalReportLocation) || !File.Exists(CrystalReportLocation) || string.IsNullOrEmpty(WrapperExePath) || !File.Exists(WrapperExePath));

            // Reset Button 2 text and disable it (needs Button 1 to run first)
            button2.Text = "Process & Email"; // Reset text
            button2.Enabled = false; // Disable until Button 1 completes again

            // Keep view buttons visible if files exist (optional, could hide them too)
            btnViewReport.Visible = !string.IsNullOrEmpty(_generatedReportPath) && File.Exists(_generatedReportPath);
            btnViewAnalysis.Visible = !string.IsNullOrEmpty(_generatedAnalysisFilePath) && File.Exists(_generatedAnalysisFilePath);

            // Update status to Ready after completion message was shown briefly
            UpdateStatus("Ready");
        });
    }


    /// <summary>
    /// Resets button states when the report type changes, forcing re-generation.
    /// </summary>
    private void ResetButtonStatesAfterTypeChange()
    {
        SafeControlUpdate(this, () =>
        {
            Logger.LogDebug("Resetting button states due to report type change.");
            button1.Text = "Create Report";
            // Re-enable button 1 only if config is valid
            button1.Enabled = !(string.IsNullOrEmpty(CrystalReportLocation) || !File.Exists(CrystalReportLocation) || string.IsNullOrEmpty(WrapperExePath) || !File.Exists(WrapperExePath));

            button2.Text = "Process & Email";
            button2.Enabled = false; // Disable processing until new report created
            btnViewReport.Visible = false;
            btnViewAnalysis.Visible = false;
            _generatedReportPath = string.Empty; // Clear paths
            _generatedAnalysisFilePath = string.Empty;
            UpdateStatus("Ready");
        });
    }


    /// <summary>
    /// Safely shows or hides the "View Analysis" button.
    /// </summary>
    private void ShowViewAnalysisButton(bool show)
    {
        SafeControlUpdate(btnViewAnalysis, () => btnViewAnalysis.Visible = show);
    }

    // --- Validation Helpers ---

    private bool ValidateInputDates()
    {
        if (datepickFrom.Value > datepickTo.Value)
        {
            Logger.LogError("Validation Failed: 'From' date cannot be after 'To' date.");
            MessageBox.Show("The 'From' date cannot be after the 'To' date.", "Date Range Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return false;
        }
        // Add other date validations if needed (e.g., max range)
        return true;
    }

    private bool ValidateFinancialYearSelection()
    {
        // Only validate if the FY dropdown is visible (i.e., for Weekly reports)
        if (finYearDropBox.Visible && finYearDropBox.SelectedItem != null)
        {
            string selectedFinYear = finYearDropBox.SelectedItem.ToString()!;
            // Use the static validation method from ExcelCopyData
            if (!ExcelCopyData.IsFinancialYearValid(selectedFinYear, datepickFrom.Value, datepickTo.Value))
            {
                Logger.LogWarning($"Potential FY mismatch: Selected FY '{selectedFinYear}', Date Range '{datepickFrom.Value:d}' to '{datepickTo.Value:d}'. Prompting user.");
                DialogResult dr = MessageBox.Show(
                    $"The selected date range ({datepickFrom.Value:d} - {datepickTo.Value:d}) does not fall entirely within the selected Financial Year ({selectedFinYear}).\n\n" +
                    "Do you want to continue anyway?",
                    "Financial Year Mismatch Warning",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);

                if (dr == DialogResult.No)
                {
                    Logger.LogInfo("User chose not to proceed due to FY mismatch.");
                    return false; // User cancelled
                }
                Logger.LogWarning("User chose to proceed despite FY mismatch warning.");
            }
        }
        return true; // Validation passed or not applicable
    }

    // --- File Handling Helper ---

    /// <summary>
    /// Opens the specified file using the default system application.
    /// </summary>
    private void OpenFileHelper(string filePath, string fileTypeDescription)
    {
        Logger.LogInfo($"Attempting to open {fileTypeDescription}: {filePath}");
        try
        {
            if (!File.Exists(filePath))
            {
                Logger.LogWarning($"{Capitalize(fileTypeDescription)} file not found at path: {filePath}");
                MessageBox.Show($"{Capitalize(fileTypeDescription)} file was not found:\n{filePath}", "File Not Found", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            // Use Process.Start with UseShellExecute=true to open with default app
            Process.Start(new ProcessStartInfo(filePath) { UseShellExecute = true });
            Logger.LogInfo($"Successfully initiated opening of {fileTypeDescription} file.");
        }
        catch (Exception ex)
        {
            Logger.LogError($"Error opening {fileTypeDescription} file '{filePath}': {ex}");
            MessageBox.Show($"Could not open the {fileTypeDescription} file.\nError: {ex.Message}", "File Open Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    // --- Date Calculation Helpers ---

    // Calculate date range for Monthly report type
    private static (DateTime DateFrom, DateTime DateTo, bool ShowFinYear) CalculateMonthlyRange(DateTime today)
    {
        // Report for previous full month if run <= 15th, else current month's period (start to today)
        DateTime dateFrom, dateTo;
        if (today.Day <= 15)
        {
            // Previous full month
            DateTime firstDayOfCurrentMonth = new DateTime(today.Year, today.Month, 1);
            dateTo = firstDayOfCurrentMonth.AddDays(-1); // Last day of previous month
            dateFrom = dateTo.AddDays(1).AddMonths(-1); // First day of previous month
        }
        else
        {
            // Current month period (1st to today)
            dateFrom = new DateTime(today.Year, today.Month, 1);
            dateTo = today; // Up to today
        }
        return (dateFrom, dateTo, false); // Hide FY for Monthly
    }

    // Calculate date range for Quarterly report type
    private static (DateTime DateFrom, DateTime DateTo, bool ShowFinYear) CalculateQuarterlyRange(DateTime today)
    {
        // Report for the previous full quarter
        int currentQuarter = (today.Month - 1) / 3 + 1;
        // First day of the current quarter
        DateTime firstDayOfCurrentQuarter = new DateTime(today.Year, (currentQuarter - 1) * 3 + 1, 1);
        // Last day of the previous quarter is the day before the first day of the current quarter
        DateTime dateTo = firstDayOfCurrentQuarter.AddDays(-1);
        // First day of the previous quarter is 3 months before the first day of the current quarter
        DateTime dateFrom = firstDayOfCurrentQuarter.AddMonths(-3);
        return (dateFrom, dateTo, false); // Hide FY for Quarterly
    }

    // --- String Helpers ---
    private static string Capitalize(string text) => string.IsNullOrEmpty(text) ? text : char.ToUpperInvariant(text[0]) + text[1..];
    private static string GetQuarterString(DateTime date) => $"Q{(date.Month - 1) / 3 + 1}";


    // --- Dropdown Population ---
    private void PopulateFinancialYearDropdown()
    {
        SafeControlUpdate(finYearDropBox, () =>
        {
            finYearDropBox.Items.Clear();
            // Use the _financialYear field (YYYY_YY format) initialized in Load
            string currentFY = _financialYear;
            if (!string.IsNullOrEmpty(currentFY))
            {
                finYearDropBox.Items.Add(currentFY);
                // Get previous FY based on the current one
                string? previousFY = ExcelCopyData.GetPreviousFinancialYear(currentFY);
                if (!string.IsNullOrEmpty(previousFY))
                {
                    finYearDropBox.Items.Add(previousFY);
                }
            }
            else
            {
                Logger.LogWarning("Could not determine current financial year for dropdown population.");
                // Optionally add a default or placeholder
                finYearDropBox.Items.Add("FY Unknown");
            }

            // Select the first item (usually current FY) if available
            if (finYearDropBox.Items.Count > 0)
            {
                finYearDropBox.SelectedIndex = 0;
            }
        });
    }

    #endregion
}
