// C# 10+ Features (using file-scoped namespace, global using directives if applicable elsewhere)
using conversionTest;
using OfficeOpenXml; // EPPlus library for Excel manipulation
using OfficeOpenXml.Table.PivotTable;

namespace QuoteConversionReportAutomation; // File-scoped namespace

/// <summary>
/// Represents progress information for Excel operations.
/// </summary>
/// <param name="Message">The status message to display.</param>
/// <param name="Percentage">Optional progress percentage (0-100), -1 if not applicable.</param>
public record ProgressReport(string Message, int Percentage = -1);

/// <summary>
/// Provides methods for copying data between Excel sheets and performing related operations asynchronously using Tasks.
/// Uses OfficeOpenXml (EPPlus). Ensure EPPlus license context is set in your application startup.
/// Includes specific folder structure logic for different report types.
/// Now uses a specific report date for filename generation.
/// </summary>
public static class ExcelCopyData // Made static as methods were static
{
    #region Constants

    // --- Report Type Indices (Must match Form1.cs) ---
    private const int DailyReportIndex = 0;
    private const int WeeklyReportIndex = 1;
    private const int MonthlyReportIndex = 2;
    private const int QuarterlyReportIndex = 3;
    private const int AnnualReportIndex = 4;

    // Constants for column indices (1-based for EPPlus access).
    private const int CustomerColumnIndex = 1;     // Column A
    private const int DateColumnIndex = 13;        // Column M
    private const int FinancialYearColumnIndex = 14; // Column N
    private const int SourceFileNameColumnIndex = 12; // Column L

    // Define the range of columns containing formulas to be cleared below the last customer
    // Adjust these indices as needed for your template
    private const int FirstFormulaColumnIndex = 2;  // Column B
    private const int LastFormulaColumnIndex = 14;  // Column N (or adjust as needed)


    // --- Sheet Names ---
    private const string AnalysisSheetName = "Analysis"; // Sheet containing formulas/unique customers
    private const string MonthlyOrderPivotSheetName = "OrderPivot";
    private const string MonthlyEstimatePivotSheetName = "Estimate Success PivotTable";

    // --- Pivot Table Names ---
    private const string MonthlyOrderPivotName = "PivotTable1";
    private const string MonthlyEstimatePivotName = "PivotTable3";

    #endregion Constants

    #region Public Async Methods

    /// <summary>
    /// Asynchronously processes an Excel report: copies data, extracts unique customers, calculates, cleans,
    /// updates weekly summary (if applicable), and handles pivot tables. Saves the final file into a report-type specific folder.
    /// Uses the provided report date for filename generation.
    /// </summary>
    /// <param name="selectedFinYear">User selected financial year (e.g., "2023_24"). Required for Weekly/Daily.</param>
    /// <param name="reportType">Indicates the report type: 0=Daily, 1=Weekly, 2=Monthly, 3=Quarterly, 4=Annual.</param>
    /// <param name="sourceFilePath">Path to the source Excel file (Crystal Report output).</param>
    /// <param name="sourceSheetName">Name of the sheet in the source file (e.g., "Sheet1").</param>
    /// <param name="baseFileSaveLocation">Base directory where the report-specific folder structure will be created (e.g., ...\Estimates\).</param>
    /// <param name="templateFilePath">Path to the Excel template file.</param>
    /// <param name="destinationDataSheetName">Name of the sheet where data will be copied TO in the template (e.g., "DATA").</param>
    /// <param name="startRow">The starting row for copying data from source (1-based).</param>
    /// <param name="startCol">The starting column for copying data from source (1-based).</param>
    /// <param name="progress">Optional progress reporter.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <param name="reportDate">The specific date the report pertains to (used for filename generation).</param> // <<< ADDED Parameter
    /// <returns>The path to the final processed Excel file, or null if cancelled or an error occurs.</returns>
    public static async Task<string?> ProcessExcelReportAsync(
        string selectedFinYear, // Still needed for weekly append and potentially daily context
        int reportType,
        string sourceFilePath, // <<< Path to the ORIGINAL raw file
        string sourceSheetName,
        string baseFileSaveLocation, // e.g., ...\Estimates\
        string templateFilePath,
        string destinationDataSheetName, // e.g., "DATA"
        int startRow = 1,
        int startCol = 1,
        IProgress<ProgressReport>? progress = null,    
        DateTime reportDate = default,
        CancellationToken cancellationToken = default) // <<< ADDED Parameter
    {
        ArgumentException.ThrowIfNullOrEmpty(sourceFilePath);
        ArgumentException.ThrowIfNullOrEmpty(sourceSheetName);
        ArgumentException.ThrowIfNullOrEmpty(baseFileSaveLocation);
        ArgumentException.ThrowIfNullOrEmpty(templateFilePath);
        ArgumentException.ThrowIfNullOrEmpty(destinationDataSheetName);
        // selectedFinYear is needed for weekly append, so keep check if weekly is processed
        if (reportType == WeeklyReportIndex || reportType == DailyReportIndex) // Daily also needs FY context
        {
            ArgumentException.ThrowIfNullOrEmpty(selectedFinYear);
        }

        // Use Today if default date was passed (shouldn't happen from updated Form1)
        if (reportDate == default)
        {
            reportDate = DateTime.Today;
            Logger.LogWarning($"ProcessExcelReportAsync called without a specific reportDate. Defaulting to Today for filename generation: {reportDate:yyyy-MM-dd}");
        }


        ExcelPackage.License.SetNonCommercialPersonal("Harlow"); // Set license context

        string? finalFilePath = null;
        string? tempFilePath = null;
        string? fullOutputFolderPath = null; // Use nullable string

        try
        {
            progress?.Report(new ProgressReport("Starting Excel processing...", 0));
            cancellationToken.ThrowIfCancellationRequested();

            // 1. Determine and Create Report-Specific Folder using local logic
            // Folder structure still based on *current* date/time when the process runs
            fullOutputFolderPath = CreateReportSpecificFolder(reportType, baseFileSaveLocation, DateTime.Now);
            if (fullOutputFolderPath == null)
            {
                throw new InvalidOperationException("Failed to create or determine the report output folder.");
            }
            progress?.Report(new ProgressReport("Output folder prepared."));
            cancellationToken.ThrowIfCancellationRequested();

            // 2. Define temporary file path within the specific output folder
            tempFilePath = Path.Combine(fullOutputFolderPath, $"temp_{Guid.NewGuid()}.xlsx");
            Logger.LogDebug($"Using temporary file: {tempFilePath}");

            // 3. Copy Template to Temp Location
            await Task.Run(() => File.Copy(templateFilePath, tempFilePath, true), cancellationToken);
            progress?.Report(new ProgressReport("Template copied."));
            cancellationToken.ThrowIfCancellationRequested();

            // 4. Open Packages and Copy Data
            progress?.Report(new ProgressReport("Opening Excel files..."));
            using (var sourcePackage = new ExcelPackage(new FileInfo(sourceFilePath)))
            using (var destinationPackage = new ExcelPackage(new FileInfo(tempFilePath)))
            {
                ExcelWorksheet? sourceWorksheet = sourcePackage.Workbook.Worksheets[sourceSheetName] ?? throw new FileNotFoundException($"Source sheet '{sourceSheetName}' not found in '{sourceFilePath}'.");
                ExcelWorksheet destinationWorksheet = GetOrCreateDestinationWorksheet(destinationPackage, destinationDataSheetName, sourceWorksheet);

                int sourceRowCount = sourceWorksheet.Dimension?.Rows ?? 0;
                int sourceColCount = sourceWorksheet.Dimension?.Columns ?? 0;
                Logger.LogDebug($"Source dimensions: {sourceRowCount} rows, {sourceColCount} cols. Start copy from R{startRow}C{startCol}.");

                progress?.Report(new ProgressReport("Copying data from source to template...", 10));
                if (sourceRowCount >= startRow && sourceColCount >= startCol)
                {
                    await Task.Run(() =>
                    {
                        ExcelRange sourceRange = sourceWorksheet.Cells[startRow, startCol, sourceRowCount, sourceColCount];
                        ExcelRange destStartCell = destinationWorksheet.Cells[2, 1]; // Data starts at row 2
                        sourceRange.Copy(destStartCell);
                        Logger.LogInfo($"Copied range {sourceRange.Address} from {sourceSheetName} to {destinationDataSheetName}!{destStartCell.Address}.");
                    }, cancellationToken);
                }
                else
                {
                    Logger.LogWarning($"Source sheet '{sourceSheetName}' has no data to copy (Rows: {sourceRowCount}, StartRow: {startRow}) or start column is out of bounds.");
                }
                progress?.Report(new ProgressReport("Data copy complete.", 30));
                cancellationToken.ThrowIfCancellationRequested();

                // 5. Post-Copy Processing (Unique Customers, Calculations, Cleaning)
                // Pass the reportDate for potential use in post-copy steps if needed later
                await ProcessPostCopyOperationsAsync(destinationPackage, destinationDataSheetName, AnalysisSheetName, reportType, progress, selectedFinYear, sourceFilePath, reportDate, cancellationToken);

                // 6. Save the destination package (temp file) before renaming
                progress?.Report(new ProgressReport("Saving processed file...", 85));
                try
                {
                    await destinationPackage.SaveAsync(cancellationToken);
                    Logger.LogDebug($"Saved changes to temporary file: {tempFilePath}");
                }
                catch (Exception saveEx)
                {
                    Logger.LogError($"Error saving temporary Excel package '{tempFilePath}': {saveEx}");
                    throw; // Re-throw save exception
                }

            } // DestinationPackage is disposed here. File lock *should* be released.

            await Task.Delay(500, cancellationToken); // Wait 500 milliseconds
            Logger.LogDebug("Brief delay completed after disposing destination package.");


            // 7. Generate Final File Name using the provided reportDate
            progress?.Report(new ProgressReport("Generating final filename...", 90));
            string generatedFileName = await Task.Run(() => GenerateFinalFileName(reportType, reportDate), cancellationToken); // <<< Pass reportDate
            finalFilePath = Path.Combine(fullOutputFolderPath, generatedFileName);
            Logger.LogDebug($"Generated final filename based on report date {reportDate:yyyy-MM-dd}: {generatedFileName}");
            Logger.LogDebug($"Full final file path: {finalFilePath}");

            Logger.LogInfo($"Attempting to rename file.");
            Logger.LogInfo($"Source (Temp): '{tempFilePath}'");
            Logger.LogInfo($"Destination (Final): '{finalFilePath}'");

            // 8. Rename Temp File to Final File (with retry for locks)
            await RenameFileWithRetryAsync(tempFilePath, finalFilePath, progress, cancellationToken);
            tempFilePath = null; // Prevent deletion in finally block as rename was successful

            progress?.Report(new ProgressReport("Excel processing complete.", 100));
            Logger.LogInfo($"Excel processing finished. Final file: {finalFilePath}");

            return finalFilePath;
        }
        catch (OperationCanceledException)
        {
            Logger.LogWarning("Excel processing was cancelled.");
            progress?.Report(new ProgressReport("Operation cancelled."));
            return null; // Indicate cancellation
        }
        catch (Exception ex)
        {
            Logger.LogError($"Error during Excel processing: {ex}"); // Log the full exception
            progress?.Report(new ProgressReport($"Error: {ex.Message}"));
            return null; // Indicate error
        }
        finally
        {
            if (tempFilePath != null && File.Exists(tempFilePath))
            {
                try
                {
                    File.Delete(tempFilePath);
                    Logger.LogInfo($"Deleted temporary file due to incomplete process: {tempFilePath}");
                }
                catch (Exception cleanupEx)
                {
                    Logger.LogWarning($"Failed to delete temporary file '{tempFilePath}': {cleanupEx.Message}");
                }
            }
        }
    }

    /// <summary>
    /// Calculates and returns the current financial year string based on Harlow's fiscal calendar (starting May).
    /// </summary>
    /// <param name="useUnderscoreFormat">If true, returns "YYYY_YY" format; otherwise, returns "FY YY/YY" format.</param>
    /// <returns>The financial year string.</returns>
    public static string GetCurrentFinancialYear(bool useUnderscoreFormat = false)
    {
        DateTime today = DateTime.Today;
        int year = today.Year;
        int startYear = today.Month >= 5 ? year : year - 1; // Financial year starts in May
        int endYear = startYear + 1;

        return useUnderscoreFormat
            ? $"{startYear}_{endYear.ToString()[2..]}" // E.g., 2023_24
            : $"FY {startYear.ToString()[2..]}/{endYear.ToString()[2..]}"; // E.g., FY 23/24
    }

    /// <summary>
    /// Calculates the previous financial year string based on the current one.
    /// </summary>
    /// <param name="currentFinancialYearUnderscore">The current financial year in "YYYY_YY" format.</param>
    /// <returns>The previous financial year in "YYYY_YY" format, or null if input format is invalid.</returns>
    public static string? GetPreviousFinancialYear(string currentFinancialYearUnderscore)
    {
        if (string.IsNullOrEmpty(currentFinancialYearUnderscore)) return null;

        string[] parts = currentFinancialYearUnderscore.Split('_');
        if (parts.Length == 2 && int.TryParse(parts[0], out int startYear))
        {
            int prevStartYear = startYear - 1;
            return $"{prevStartYear}_{startYear.ToString()[2..]}"; // e.g., 2022_23
        }
        Logger.LogWarning($"Invalid financial year format for calculating previous: {currentFinancialYearUnderscore}");
        return null; // Invalid format
    }

    /// <summary>
    /// Validates if the selected date range falls within the specified financial year.
    /// Assumes financial year starts in May.
    /// </summary>
    /// <param name="selectedFinYearUnderscore">Financial year in "YYYY_YY" format.</param>
    /// <param name="fromDate">Start date of the range.</param>
    /// <param name="toDate">End date of the range.</param>
    /// <returns>True if the date range is valid for the financial year, false otherwise.</returns>
    public static bool IsFinancialYearValid(string selectedFinYearUnderscore, DateTime fromDate, DateTime toDate)
    {
        if (string.IsNullOrEmpty(selectedFinYearUnderscore)) return false;

        string[] parts = selectedFinYearUnderscore.Split('_');
        if (parts.Length == 2 && int.TryParse(parts[0], out int startYear))
        {
            int endYear = startYear + 1;
            // Financial year starts May 1st of startYear and ends April 30th of endYear
            DateTime fyStartDate = new(startYear, 5, 1);
            DateTime fyEndDate = new(endYear, 4, 30);

            // Check if the entire range [fromDate, toDate] is within [fyStartDate, fyEndDate]
            bool isValid = fromDate >= fyStartDate && toDate <= fyEndDate;
            if (!isValid)
            {
                Logger.LogWarning($"Date range {fromDate:yyyy-MM-dd} to {toDate:yyyy-MM-dd} is outside selected FY {selectedFinYearUnderscore} ({fyStartDate:yyyy-MM-dd} to {fyEndDate:yyyy-MM-dd}).");
            }
            return isValid;
        }
        Logger.LogWarning($"Invalid financial year format for validation: {selectedFinYearUnderscore}");
        return false; // Invalid format
    }

    /// <summary>
    /// Gets the expected final file path without creating directories or files.
    /// Used for checking if the file already exists before processing.
    /// Uses the provided report date for filename generation.
    /// </summary>
    /// <param name="reportType">Indicates the report type (0=Daily, 1=Weekly, 2=Monthly, 3=Quarterly, 4=Annual).</param>
    /// <param name="baseFileSaveLocation">Base directory where the report structure will be created.</param>
    /// <param name="reportDate">The specific date the report pertains to (used for filename generation).</param> // <<< ADDED Parameter
    /// <returns>The calculated full path for the final file, or null if an error occurs.</returns>
    public static string? GetExpectedFinalFilePath(int reportType, string baseFileSaveLocation, DateTime reportDate) // <<< ADDED Parameter
    {
        try
        {
            // Use Today for folder structure, reportDate for filename
            DateTime today = DateTime.Today;

            // Use default date if not provided (shouldn't happen from updated Form1)
            if (reportDate == default)
            {
                reportDate = today;
                Logger.LogWarning($"GetExpectedFinalFilePath called without a specific reportDate. Defaulting to Today for filename generation: {reportDate:yyyy-MM-dd}");
            }

            // Get the full folder path using the corrected local logic (based on today's date)
            string? folderPath = GetReportSpecificFolderPath(reportType, baseFileSaveLocation, today);
            if (folderPath == null) return null;

            // Generate filename based on the specific reportDate
            string fileName = GenerateFinalFileName(reportType, reportDate); // <<< Pass reportDate
            return Path.Combine(folderPath, fileName);
        }
        catch (Exception ex)
        {
            Logger.LogError($"Error getting expected final file path: {ex.Message}");
            return null;
        }
    }


    #endregion Public Async Methods

    #region Internal Processing Steps (Async)

    /// <summary>
    /// Performs post-copy operations: extracts unique customers, calculates analysis, cleans rows/content,
    /// refreshes pivots, and potentially copies to a weekly summary report.
    /// </summary>
    /// <param name="package">The Excel package being processed.</param>
    /// <param name="sourceDataSheetName">The name of the sheet where raw data was copied (e.g., "DATA").</param>
    /// <param name="targetAnalysisSheetName">The name of the sheet to populate and clean (e.g., "Analysis").</param>
    /// <param name="reportType">The type of report being generated.</param>
    /// <param name="progress">Progress reporter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <param name="selectedFinYear">Selected financial year (YYYY_YY format). Required for Weekly append.</param>
    /// <param name="originalSourceFilePath">The file path of the original raw data file (e.g., Crystal report output).</param>
    /// <param name="reportDate">The specific date the report pertains to.</param> // <<< ADDED Parameter
    private static async Task ProcessPostCopyOperationsAsync(
        ExcelPackage package,
        string sourceDataSheetName,
        string targetAnalysisSheetName,
        int reportType,
        IProgress<ProgressReport>? progress,
        string selectedFinYear,
        string originalSourceFilePath,
        DateTime reportDate,
        CancellationToken cancellationToken) // <<< ADDED Parameter
    {
        progress?.Report(new ProgressReport("Extracting unique customers...", 40));
        // Extract customers from the sourceDataSheetName and populate the targetAnalysisSheetName
        // Pass reportDate for potential use in populating analysis sheet date column
        await ExtractUniqueCustomersAsync(package, sourceDataSheetName, targetAnalysisSheetName, progress, originalSourceFilePath, reportDate, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();

        progress?.Report(new ProgressReport("Calculating analysis sheet...", 50));
        // Calculate the targetAnalysisSheetName
        await Task.Run(() => CalculateSheet(package, targetAnalysisSheetName), cancellationToken);
        Logger.LogInfo($"Sheet '{targetAnalysisSheetName}' calculations performed.");
        cancellationToken.ThrowIfCancellationRequested();

        progress?.Report(new ProgressReport("Cleaning analysis sheet...", 60));
        // Clean content below the last customer in the targetAnalysisSheetName
        await Task.Run(() => ClearContentBelowLastCustomer(package, targetAnalysisSheetName, CustomerColumnIndex, FirstFormulaColumnIndex, LastFormulaColumnIndex), cancellationToken);
        Logger.LogInfo($"Cleaned content below last customer in sheet '{targetAnalysisSheetName}'.");
        cancellationToken.ThrowIfCancellationRequested();

        // Refresh Pivot Tables (if applicable, e.g., Monthly/Quarterly/Annual templates)
        if (reportType is MonthlyReportIndex or QuarterlyReportIndex or AnnualReportIndex)
        {
            progress?.Report(new ProgressReport("Setting pivot tables to refresh on load...", 70));
            // Run sequentially to avoid potential concurrency issues with EPPlus
            await Task.Run(() => RefreshPivotTable(package, MonthlyOrderPivotSheetName, MonthlyOrderPivotName), cancellationToken);
            await Task.Run(() => RefreshPivotTable(package, MonthlyEstimatePivotSheetName, MonthlyEstimatePivotName), cancellationToken);
            Logger.LogInfo("Pivot tables set to refresh on load.");
            cancellationToken.ThrowIfCancellationRequested();
        }

        // Append to Central Weekly Report (only if processing a Weekly report type)
        if (reportType == WeeklyReportIndex)
        {
            progress?.Report(new ProgressReport("Appending data to central weekly report...", 75));
            // Pass reportDate to determine the filename for the weekly append
            await CopyAnalysisDataToWeeklyReportAsync(package, targetAnalysisSheetName, progress, selectedFinYear, reportType, originalSourceFilePath, reportDate, cancellationToken);
            Logger.LogInfo("Data appended to central weekly report.");
            cancellationToken.ThrowIfCancellationRequested();
        }
    }

    /// <summary>
    /// Gets or creates the destination worksheet, copying headers from the source if creating.
    /// Also clears existing data below row 1 if the sheet already exists.
    /// </summary>
    private static ExcelWorksheet GetOrCreateDestinationWorksheet(ExcelPackage package, string sheetName, ExcelWorksheet sourceWorksheet)
    {
        ExcelWorksheet? worksheet = package.Workbook.Worksheets[sheetName];
        if (worksheet == null)
        {
            worksheet = package.Workbook.Worksheets.Add(sheetName);
            // Copy the first row (headers) from the source sheet if it exists and has headers
            if (sourceWorksheet.Dimension != null && sourceWorksheet.Dimension.Rows > 0)
            {
                int headerColCount = sourceWorksheet.Dimension.Columns;
                ExcelRange sourceHeaderRow = sourceWorksheet.Cells[1, 1, 1, headerColCount];
                ExcelRange destHeader = worksheet.Cells[1, 1, 1, headerColCount];
                sourceHeaderRow.Copy(destHeader); // Copy headers including formatting
                Logger.LogInfo($"Created sheet '{sheetName}' and copied headers from '{sourceWorksheet.Name}'.");
            }
            else
            {
                worksheet.Cells[1, 1].Value = "DefaultHeader"; // Add a default if source is empty
                Logger.LogWarning($"Created sheet '{sheetName}', source sheet '{sourceWorksheet.Name}' was empty, added default header.");
            }
        }
        else
        {
            // Clear existing data below header row if sheet already exists
            if (worksheet.Dimension != null && worksheet.Dimension.Rows > 1)
            {
                // Delete rows from 2 to the end
                worksheet.DeleteRow(2, worksheet.Dimension.Rows - 1);
                Logger.LogInfo($"Cleared existing data (rows 2 onwards) from sheet '{sheetName}'.");
            }
            else
            {
                Logger.LogDebug($"Sheet '{sheetName}' already existed but had no data below header row.");
            }
        }
        return worksheet;
    }

    /// <summary>
    /// Extracts unique customers from the source data sheet and populates the target analysis sheet asynchronously.
    /// Also populates Date (using reportDate), Financial Year, and Source Filename.
    /// </summary>
    /// <param name="package">The Excel package containing source and target sheets.</param>
    /// <param name="sourceDataSheetName">Name of the sheet with raw data (e.g., "DATA").</param>
    /// <param name="targetAnalysisSheetName">Name of the sheet to populate (e.g., "Analysis").</param>
    /// <param name="progress">Progress reporter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <param name="originalSourceFilePath">Full path of the original source file (Crystal report output).</param>
    /// <param name="reportDate">The specific date the report pertains to.</param> // <<< ADDED Parameter
    private static async Task ExtractUniqueCustomersAsync(
        ExcelPackage package,
        string sourceDataSheetName,
        string targetAnalysisSheetName,
        IProgress<ProgressReport>? progress,   
        string originalSourceFilePath,
        DateTime reportDate,
        CancellationToken cancellationToken) // <<< ADDED Parameter
    {
        ExcelWorksheet? dataSheet = package.Workbook.Worksheets[sourceDataSheetName];
        if (dataSheet == null)
        {
            Logger.LogError($"Source data sheet '{sourceDataSheetName}' not found for customer extraction.");
            return;
        }

        ExcelWorksheet analysisSheet = package.Workbook.Worksheets[targetAnalysisSheetName]
                                         ?? package.Workbook.Worksheets.Add(targetAnalysisSheetName); // Create if not exists

        // Ensure Analysis sheet has necessary headers if newly created (assuming row 1-5 are headers/fixed)
        if (analysisSheet.Dimension == null || analysisSheet.Dimension.Rows < 5)
        {
            Logger.LogWarning($"Analysis sheet '{targetAnalysisSheetName}' might be missing expected headers/structure. Ensure template is correct.");
        }

        int dataRowCount = dataSheet.Dimension?.Rows ?? 0;
        int startDataRowInDataSheet = 2; // Data usually starts from row 2 in the DATA sheet
        if (dataRowCount < startDataRowInDataSheet)
        {
            Logger.LogWarning($"Source data sheet '{sourceDataSheetName}' has insufficient rows ({dataRowCount}) for customer extraction starting at row {startDataRowInDataSheet}.");
            return;
        }

        // --- Get the filename to populate ---
        // Use the original raw filename for reference in the Analysis sheet.
        string sourceFileName = Path.GetFileName(originalSourceFilePath);
        Logger.LogDebug($"Filename determined for Analysis sheet population: {sourceFileName}");

        // Extract unique customers first
        var uniqueCustomers = await Task.Run(() =>
        {
            var customers = new HashSet<string>();
            for (int row = startDataRowInDataSheet; row <= dataRowCount; row++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                string? customerName = dataSheet.Cells[row, CustomerColumnIndex].Value?.ToString()?.Trim();
                if (!string.IsNullOrWhiteSpace(customerName))
                {
                    customers.Add(customerName);
                }
                if (row % 100 == 0)
                {
                    int percent = (int)((double)(row - startDataRowInDataSheet + 1) / (dataRowCount - startDataRowInDataSheet + 1) * 100);
                    progress?.Report(new ProgressReport($"Extracting customers... {percent}%", percent));
                }
            }
            progress?.Report(new ProgressReport($"Extracting customers... 100%", 100));
            return customers;
        }, cancellationToken);

        // Now, populate the Analysis sheet
        await Task.Run(() =>
        {
            int analysisPopulateStartRow = 6; // Start populating customer names from row 6
            // *** Use the passed reportDate for the Date column ***
            string reportDateString = reportDate.ToString("dd/MM/yyyy");
            string currentFY = GetCurrentFinancialYear(false); // E.g., FY 2024/25

            foreach (string customer in uniqueCustomers.OrderBy(c => c)) // Optionally order customers
            {
                cancellationToken.ThrowIfCancellationRequested();
                analysisSheet.Cells[analysisPopulateStartRow, CustomerColumnIndex].Value = customer;
                // Populate Date, FY, and Source Filename columns
                // *** Use reportDateString ***
                analysisSheet.Cells[analysisPopulateStartRow, DateColumnIndex].Value = reportDateString;
                analysisSheet.Cells[analysisPopulateStartRow, FinancialYearColumnIndex].Value = currentFY;
                analysisSheet.Cells[analysisPopulateStartRow, SourceFileNameColumnIndex].Value = sourceFileName;
                analysisPopulateStartRow++;
            }
            Logger.LogInfo($"Populated '{targetAnalysisSheetName}' with {uniqueCustomers.Count} unique customers starting at row 6, using report date {reportDateString}.");
        }, cancellationToken);
    }

    /// <summary>
    /// Triggers calculation for a specific worksheet.
    /// Note: EPPlus calculation engine might have limitations compared to Excel.
    /// </summary>
    private static void CalculateSheet(ExcelPackage package, string sheetName)
    {
        ExcelWorksheet? sheet = package.Workbook.Worksheets[sheetName];
        if (sheet != null)
        {
            try
            {
                sheet.Calculate();
                Logger.LogInfo($"Triggered calculation for sheet '{sheetName}'.");
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error during calculation of sheet '{sheetName}': {ex.Message}");
            }
        }
        else
        {
            Logger.LogWarning($"Sheet '{sheetName}' not found for calculation.");
        }
    }

    /// <summary>
    /// Clears content (values and formulas) in specified columns for rows below the last row containing data
    /// in the primary check column (e.g., Customer column). Leaves formatting intact.
    /// </summary>
    /// <param name="package">The Excel package.</param>
    /// <param name="sheetName">The name of the sheet to clean.</param>
    /// <param name="checkColumnIndex">The 1-based index of the column to check for the last data entry (e.g., Customer column A = 1).</param>
    /// <param name="firstClearColumnIndex">The 1-based index of the first column to clear content from.</param>
    /// <param name="lastClearColumnIndex">The 1-based index of the last column to clear content from.</param>
    private static void ClearContentBelowLastCustomer(ExcelPackage package, string sheetName, int checkColumnIndex, int firstClearColumnIndex, int lastClearColumnIndex)
    {
        ExcelWorksheet? worksheet = package.Workbook.Worksheets[sheetName];
        if (worksheet == null || worksheet.Dimension == null)
        {
            Logger.LogWarning($"Sheet '{sheetName}' not found or is empty, cannot clear content.");
            return;
        }

        int totalRows = worksheet.Dimension.Rows;
        int lastCustomerRow = 0;
        int customerDataStartRow = 6; // Assuming customer data starts at row 6 in Analysis sheet

        // Find the last row with a customer name in the check column (Column A)
        for (int row = totalRows; row >= customerDataStartRow; row--)
        {
            var cellValue = worksheet.Cells[row, checkColumnIndex].Value;
            if (cellValue != null && !string.IsNullOrWhiteSpace(cellValue.ToString()))
            {
                lastCustomerRow = row;
                break; // Found the last customer row
            }
        }

        Logger.LogDebug($"Sheet '{sheetName}': Total rows: {totalRows}, Last customer found at row: {lastCustomerRow} (Data starts row {customerDataStartRow})");

        if (lastCustomerRow == 0 && totalRows >= customerDataStartRow)
        {
            Logger.LogWarning($"No customer data found in column {checkColumnIndex} starting from row {customerDataStartRow} in sheet '{sheetName}'. Clearing content from row {customerDataStartRow} onwards.");
            lastCustomerRow = customerDataStartRow - 1;
        }
        else if (lastCustomerRow == 0)
        {
            Logger.LogInfo($"No customer data found and few rows exist in sheet '{sheetName}'. No content to clear.");
            return;
        }
        else if (lastCustomerRow >= totalRows)
        {
            Logger.LogInfo($"Last customer is on the last row ({lastCustomerRow}). No content below to clear in sheet '{sheetName}'.");
            return;
        }

        int startClearRow = lastCustomerRow + 1;
        if (startClearRow > totalRows)
        {
            Logger.LogInfo($"Start clear row ({startClearRow}) is beyond total rows ({totalRows}). No content to clear.");
            return;
        }

        ExcelRange rangeToClear = worksheet.Cells[startClearRow, firstClearColumnIndex, totalRows, lastClearColumnIndex];
        Logger.LogInfo($"Clearing cell values (setting to null) in range {rangeToClear.Address} (below last customer row {lastCustomerRow}) in sheet '{sheetName}'.");

        // Use .Value = null to clear content but keep formatting
        rangeToClear.Value = null;

        Logger.LogInfo($"Cleared cell values in {totalRows - startClearRow + 1} rows below the last customer.");
    }


    /// <summary>
    /// Sets a specific pivot table to refresh when the workbook is opened.
    /// Avoids forcing refresh in code due to potential corruption issues.
    /// </summary>
    private static void RefreshPivotTable(ExcelPackage package, string sheetName, string pivotTableName)
    {
        ExcelWorksheet? worksheet = package.Workbook.Worksheets[sheetName];
        if (worksheet == null)
        {
            Logger.LogWarning($"Sheet '{sheetName}' not found for pivot table refresh setting.");
            return;
        }

        ExcelPivotTable? pivotTable = worksheet.PivotTables.FirstOrDefault(pt => pt.Name == pivotTableName);

        if (pivotTable != null)
        {
            try
            {
                Logger.LogDebug($"Attempting to set Refresh for pivot table '{pivotTableName}' in sheet '{sheetName}'.");
                pivotTable.CacheDefinition.Refresh(); // <<< call refresh
                Logger.LogInfo($"Set pivot table '{pivotTableName}' in sheet '{sheetName}' to refresh.");
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error setting RefreshOnLoad for pivot table '{pivotTableName}' in '{sheetName}': {ex.Message}");
            }
        }
        else
        {
            Logger.LogWarning($"Pivot table '{pivotTableName}' not found in sheet '{sheetName}'. Available tables: {string.Join(", ", worksheet.PivotTables.Select(pt => pt.Name))}");
        }
    }

    /// <summary>
    /// Copies data VALUES from the processed Analysis sheet to the central weekly report file asynchronously.
    /// Appends data to the sheet corresponding to the selected financial year.
    /// Sets the SourceFileName column based on the report type and report date.
    /// </summary>
    /// <param name="sourcePackage">The package containing the 'Analysis' sheet.</param>
    /// <param name="sourceSheetName">The name of the source sheet (e.g., "Analysis").</param>
    /// <param name="progress">Progress reporter.</param>
    /// <param name="selectedFinYear">Financial year in format YYYY_YY. Used as the target sheet name.</param>
    /// <param name="reportType">The type of report being processed (used to determine filename).</param>
    /// <param name="originalSourceFilePath">The full path of the original raw data file.</param>
    /// <param name="reportDate">The specific date the report pertains to.</param> 
    ///     /// <param name="cancellationToken">Cancellation token.</param>
    private static async Task CopyAnalysisDataToWeeklyReportAsync(
        ExcelPackage sourcePackage,
        string sourceSheetName,
        IProgress<ProgressReport>? progress,
        string selectedFinYear,
        int reportType,
        string originalSourceFilePath,
        DateTime reportDate,
        CancellationToken cancellationToken)
    {
        string username = Environment.UserName;
        string destinationFilePath = GetWeeklyReportPath(username); // Get path based on Debug/Release

        if (string.IsNullOrEmpty(destinationFilePath))
        {
            Logger.LogError($"Central weekly report path is invalid or could not be determined. Cannot append data.");
            progress?.Report(new ProgressReport("Error: Central weekly report path invalid."));
            return; // Cannot proceed
        }
        if (!File.Exists(destinationFilePath))
        {
            Logger.LogError($"Central weekly report file not found: '{destinationFilePath}'. Cannot append data.");
            progress?.Report(new ProgressReport("Error: Central weekly report file not found."));
            return; // Cannot proceed
        }


        ExcelWorksheet? sourceWorksheet = sourcePackage.Workbook.Worksheets[sourceSheetName];
        if (sourceWorksheet == null || sourceWorksheet.Dimension == null)
        {
            Logger.LogWarning($"Source sheet '{sourceSheetName}' not found or is empty. Cannot copy to weekly report.");
            progress?.Report(new ProgressReport("Warning: No analysis data to copy to weekly report."));
            return;
        }

        try
        {
            Logger.LogInfo($"Opening weekly report file for appending: {destinationFilePath}");
            using var destinationPackage = await Task.Run(() => new ExcelPackage(new FileInfo(destinationFilePath)), cancellationToken);

            string targetSheetName = selectedFinYear; // Use the FY (e.g., "2023_24") as the sheet name
            ExcelWorksheet? destinationWorksheet = destinationPackage.Workbook.Worksheets[targetSheetName];

            if (destinationWorksheet == null)
            {
                destinationWorksheet = destinationPackage.Workbook.Worksheets.Add(targetSheetName);
                CopyHeaders(sourceWorksheet, destinationWorksheet); // Copy headers only if creating sheet
                Logger.LogInfo($"Created sheet '{targetSheetName}' in weekly report and copied headers.");
            }

            int nextFreeRow = await Task.Run(() => GetNextFreeRow(destinationWorksheet), cancellationToken);
            Logger.LogDebug($"Next free row in weekly report sheet '{targetSheetName}' is {nextFreeRow}.");

            // --- Determine the filename to write based on report type and date ---
            string filenameToWrite = GenerateFinalFileName(reportType, reportDate); // <<< Use reportDate
            Logger.LogDebug($"Using filename for weekly report append: {filenameToWrite}");
            // --- End filename determination ---


            await Task.Run(() =>
            {
                int sourceRowCount = sourceWorksheet.Dimension.Rows;
                // Determine the actual last column with data/formulas in the source sheet reliably
                int sourceColCount = sourceWorksheet.Dimension.End.Column; // Use Dimension.End.Column
                int startDataRowInAnalysis = 6; // Assuming analysis data starts at row 6

                if (sourceRowCount < startDataRowInAnalysis)
                {
                    Logger.LogWarning($"Source analysis sheet '{sourceSheetName}' has no data rows starting from row {startDataRowInAnalysis}.");
                    return; // Nothing to copy
                }

                int copiedRowCount = 0;
                // Iterate through relevant rows in the source sheet
                for (int sourceRow = startDataRowInAnalysis; sourceRow <= sourceRowCount; sourceRow++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    // Check if the customer cell in the source row is non-empty
                    var firstCellVal = sourceWorksheet.Cells[sourceRow, CustomerColumnIndex].Value;
                    if (firstCellVal != null && !string.IsNullOrWhiteSpace(firstCellVal.ToString()))
                    {
                        // Iterate through columns and copy VALUES
                        for (int col = 1; col <= sourceColCount; col++)
                        {
                            // Copy the value from source cell to destination cell
                            destinationWorksheet.Cells[nextFreeRow, col].Value = sourceWorksheet.Cells[sourceRow, col].Value;
                        }
                        // <<< Set the source file name using the determined filename >>>
                        destinationWorksheet.Cells[nextFreeRow, SourceFileNameColumnIndex].Value = filenameToWrite;

                        nextFreeRow++; // Move to the next row in the destination
                        copiedRowCount++;
                    }

                    // Report progress periodically
                    if (sourceRow % 50 == 0)
                    {
                        int percent = (int)((double)(sourceRow - startDataRowInAnalysis + 1) / (sourceRowCount - startDataRowInAnalysis + 1) * 100);
                        progress?.Report(new ProgressReport($"Copying to weekly report... {percent}%", percent));
                    }
                }
                Logger.LogInfo($"Copied values for {copiedRowCount} rows from '{sourceSheetName}' to weekly report sheet '{targetSheetName}'.");
                progress?.Report(new ProgressReport($"Copying to weekly report... 100%", 100));
            }, cancellationToken);

            await destinationPackage.SaveAsync(cancellationToken);
            Logger.LogInfo($"Successfully appended data to sheet '{targetSheetName}' in '{destinationFilePath}'.");
            progress?.Report(new ProgressReport("Data appended to central weekly report."));

        }
        catch (OperationCanceledException)
        {
            Logger.LogWarning("Operation cancelled during copy to weekly report.");
            progress?.Report(new ProgressReport("Cancelled copy to weekly report."));
            throw; // Re-throw cancellation
        }
        catch (Exception ex)
        {
            Logger.LogError($"Error copying data to weekly report '{destinationFilePath}': {ex}");
            progress?.Report(new ProgressReport($"Error copying to weekly report: {ex.Message}"));
        }
    }


    /// <summary>
    /// Copies headers (Row 1) from a source worksheet to a destination worksheet.
    /// </summary>
    private static void CopyHeaders(ExcelWorksheet sourceSheet, ExcelWorksheet destinationSheet)
    {
        if (sourceSheet.Dimension != null && sourceSheet.Dimension.Rows >= 1)
        {
            int headerColCount = sourceSheet.Dimension.Columns;
            ExcelRange sourceHeaderRow = sourceSheet.Cells[1, 1, 1, headerColCount];
            ExcelRange destHeader = destinationSheet.Cells[1, 1, 1, headerColCount];
            sourceHeaderRow.Copy(destHeader);
            Logger.LogDebug($"Copied header row (1 to {headerColCount}) from {sourceSheet.Name} to {destinationSheet.Name}");
        }
        else
        {
            destinationSheet.Cells[1, 1].Value = "DefaultHeader";
            Logger.LogWarning($"Source sheet '{sourceSheet.Name}' for header copy was empty or had no rows. Added default header to {destinationSheet.Name}.");
        }
    }

    /// <summary>
    /// Finds the next empty row in a worksheet by checking Column 1 from the bottom up.
    /// </summary>
    private static int GetNextFreeRow(ExcelWorksheet worksheet)
    {
        if (worksheet.Dimension == null) return 1; // Sheet is empty, start at row 1
        int lastUsedRow = worksheet.Dimension.End.Row;
        while (lastUsedRow >= 1)
        {
            var cell = worksheet.Cells[lastUsedRow, 1].Value;
            if (cell != null && !string.IsNullOrWhiteSpace(cell.ToString()))
            {
                return lastUsedRow + 1;
            }
            lastUsedRow--;
        }
        return 1; // If column 1 is entirely empty, start at row 1
    }

    /// <summary>
    /// Gets the path to the central weekly report file based on DEBUG or RELEASE build configuration.
    /// </summary>
    private static string GetWeeklyReportPath(string username)
    {
#if DEBUG
        return $@"C:\Users\{username}\Harlow Printing\IT - Documents\PowerBI\Quote Conversion Report\Quotes conversion data_wrangled\weekly report quotes conversion merged - copy.xlsx";
#else
        return $@"C:\Users\{username}\Harlow Printing\IT - Documents\PowerBI\Quote Conversion Report\Quotes conversion data_wrangled\weekly report quotes conversion merged.xlsx";
#endif
    }

    #endregion Internal Processing Steps (Async)

    #region File and Folder Helpers

    /// <summary>
    /// Creates the specific folder structure for the report type and returns the full path.
    /// Folder structure is based on the date the process is run.
    /// </summary>
    /// <param name="reportType">The report type index (0=Daily, 1=Weekly, etc.).</param>
    /// <param name="baseSaveLocation">The root directory (e.g., ...\Estimates\).</param>
    /// <param name="runDate">The date the process is run (used for folder naming).</param> // <<< ADDED Parameter
    /// <returns>The full path to the target folder, or null on error.</returns>
    private static string? CreateReportSpecificFolder(int reportType, string baseSaveLocation, DateTime runDate)
    {
        try
        {
            string targetFolderPath = GetReportSpecificFolderPath(reportType, baseSaveLocation, runDate); // <<< Pass runDate

            if (string.IsNullOrEmpty(targetFolderPath))
            {
                Logger.LogError($"Could not determine target folder path for report type {reportType}.");
                return null;
            }

            // Ensure the directory exists
            Directory.CreateDirectory(targetFolderPath);
            Logger.LogInfo($"Ensured report output folder exists: {targetFolderPath}");
            return targetFolderPath;
        }
        catch (Exception ex)
        {
            Logger.LogError($"Error creating report folder for type {reportType}: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Determines the specific folder path based on the report type and run date, without creating it.
    /// </summary>
    /// <param name="reportType">The report type index (0=Daily, 1=Weekly, etc.).</param>
    /// <param name="baseSaveLocation">The root directory (e.g., ...\Estimates\).</param>
    /// <param name="runDate">The date the process is run (used for folder naming).</param> // <<< ADDED Parameter
    /// <returns>The full path to the target folder, or null if type is invalid.</returns>
    private static string? GetReportSpecificFolderPath(int reportType, string baseSaveLocation, DateTime runDate) // <<< ADDED Parameter
    {
        // Validate base location first
        if (string.IsNullOrWhiteSpace(baseSaveLocation))
        {
            Logger.LogError("Base save location provided to GetReportSpecificFolderPath is null or empty.");
            return null; // Cannot proceed without a base path
        }

        // Use the provided runDate for folder structure calculations
        string reportTypeFolder;
        string yearFolder = string.Empty;     // e.g., "2025"
        string monthFolder = string.Empty;    // e.g., "April"
        string weekFolder = string.Empty;     // e.g., "Week 4"

        switch (reportType)
        {
            case DailyReportIndex: // 0 = Daily
            case WeeklyReportIndex: // 1 = Weekly
                reportTypeFolder = reportType == DailyReportIndex ? "Daily Reports" : "Weekly Reports";
                yearFolder = runDate.ToString("yyyy");       // Full year "2025"
                monthFolder = runDate.ToString("MMMM");      // Full month name "April"
                int weekNum = GetWeekOfMonth(runDate);       // Use helper
                weekFolder = $"Week {weekNum}";              // "Week 4"
                break;
            case MonthlyReportIndex: // 2 = Monthly
                reportTypeFolder = "Monthly Reports";
                break;
            case QuarterlyReportIndex: // 3 = Quarterly
                reportTypeFolder = "Quarterly reports";
                break;
            case AnnualReportIndex: // 4 = Annual
                reportTypeFolder = "Annual Reports";
                break;
            default:
                Logger.LogWarning($"Invalid report type '{reportType}' for folder creation. Using 'Other Reports'.");
                reportTypeFolder = "Other Reports";
                break;
        }

        // Construct the full path safely
        try
        {
            // Start with Base -> ReportType
            string fullPath = Path.Combine(baseSaveLocation, reportTypeFolder);

            // Add Year for Daily/Weekly
            if (!string.IsNullOrEmpty(yearFolder))
            {
                fullPath = Path.Combine(fullPath, yearFolder);
            }
            // Add Month for Daily/Weekly
            if (!string.IsNullOrEmpty(monthFolder))
            {
                fullPath = Path.Combine(fullPath, monthFolder);
            }
            // *** FIXED: Add Week for Daily/Weekly ***
            if (!string.IsNullOrEmpty(weekFolder))
            {
                fullPath = Path.Combine(fullPath, weekFolder);
            }
            return fullPath;
        }
        catch (ArgumentException ex) // Catch errors during Path.Combine
        {
            Logger.LogError($"Error combining path segments: {ex.Message}. Base='{baseSaveLocation}', Type='{reportTypeFolder}', Year='{yearFolder}', Month='{monthFolder}', Week='{weekFolder}'");
            return null;
        }
    }


    /// <summary>
    /// Generates the final file name based on the report type and the specific report date.
    /// </summary>
    /// <param name="reportType">The report type index.</param>
    /// <param name="reportDate">The date the report pertains to.</param> // <<< ADDED Parameter
    /// <returns>The file name string (not the full path).</returns>
    private static string GenerateFinalFileName(int reportType, DateTime reportDate) // <<< ADDED Parameter
    {
        string fileName;
        // Use the provided reportDate for filename generation

        switch (reportType)
        {
            case DailyReportIndex: // 0 = Daily
                // Use the specific reportDate (e.g., previous workday)
                fileName = $"{reportDate:yyyyMMdd}_Estimate_Success_Rate_Daily.xlsx";
                break;
            case WeeklyReportIndex: // 1 = Weekly
                // Use the specific reportDate (end date of the weekly period)
                fileName = $"{reportDate:yyyyMMdd} Estimate Success Rate.xlsx";
                break;
            case MonthlyReportIndex: // 2 = Monthly
                // Monthly filename based on the month/year of the reportDate
                fileName = $"Estimate Success Rate {reportDate:MMM yy}.xlsx";
                break;
            case QuarterlyReportIndex: // 3 = Quarterly
                // Quarterly filename based on the quarter of the reportDate
                int quarter = (reportDate.Month - 1) / 3 + 1;
                // Use the start/end dates of the quarter the reportDate falls into
                DateTime quarterStartDate = new(reportDate.Year, (quarter - 1) * 3 + 1, 1);
                DateTime quarterEndDate = quarterStartDate.AddMonths(3).AddDays(-1);
                string qtrFolderName = $"{quarterStartDate:MMM} to {quarterEndDate:MMM}{(quarterStartDate.Year != quarterEndDate.Year ? $" {quarterStartDate.Year}-{quarterEndDate.Year}" : $" {quarterStartDate.Year}")}";
                fileName = $"Estimate Success Rate {qtrFolderName}.xlsx";
                break;
            case AnnualReportIndex: // 4 = Annual
                // Annual filename based on the year of the reportDate
                fileName = $"Estimate Success Rate {reportDate.Year}.xlsx";
                break;
            default:
                Logger.LogWarning($"Invalid report type '{reportType}' for filename generation, defaulting to generic format using report date.");
                fileName = $"{reportDate:yyyyMMdd}_Estimate_Success_Rate_UnknownType.xlsx";
                break;
        }
        return fileName;
    }

    /// <summary>
    /// Attempts to rename (move) a file with retries on IOException.
    /// </summary>
    private static async Task RenameFileWithRetryAsync(string sourcePath, string destinationPath, IProgress<ProgressReport>? progress, CancellationToken cancellationToken, int maxRetries = 5, int delayMs = 500)
    {
        for (int i = 0; i < maxRetries; i++)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                await Task.Run(() => File.Move(sourcePath, destinationPath, true), cancellationToken); // Allow overwrite
                Logger.LogInfo($"Successfully renamed/moved '{sourcePath}' to '{destinationPath}'.");
                return; // Success
            }
            catch (IOException ex) when (i < maxRetries - 1)
            {
                Logger.LogWarning($"Attempt {i + 1} failed to rename '{sourcePath}' due to lock/IO error: {ex.Message}. Retrying in {delayMs}ms...");
                progress?.Report(new ProgressReport($"Waiting for file lock release (Attempt {i + 1})..."));
                await Task.Delay(delayMs, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                Logger.LogWarning($"Rename operation cancelled while trying to move '{sourcePath}'.");
                throw; // Re-throw cancellation
            }
            // Catching the final IOException after retries below
        }
        // If loop finishes, all retries failed
        throw new IOException($"Failed to rename file '{sourcePath}' to '{destinationPath}' after {maxRetries} attempts. The file might still be locked or another IO error occurred.");
    }


    /// <summary>
    /// Calculates the week number of a given date within its month.
    /// Assumes weeks start on Monday.
    /// </summary>
    /// <param name="date">The date to check.</param>
    /// <returns>The week number (1-5/6).</returns>
    public static int GetWeekOfMonth(DateTime date)
    {
        // Get the first day of the month
        DateTime firstOfMonth = new(date.Year, date.Month, 1);

        // Get the day of the week for the first day (Monday = 1, Sunday = 7, using ISO 8601 standard)
        // DayOfWeek enum: Sunday = 0, Monday = 1, ..., Saturday = 6
        int firstDayOfWeekIso = firstOfMonth.DayOfWeek == 0 ? 7 : (int)firstOfMonth.DayOfWeek; // Convert Sunday to 7

        // Calculate the week number using integer division.
        // Add the offset of the first day (1-based, where Monday is 1) minus 1.
        // Add the day of the month (1-based).
        // Subtract 1 because we want the result to be 0-based for the division.
        // Divide by 7.
        // Add 1 to make the final result 1-based (Week 1, Week 2, etc.).
        int weekOfMonth = (date.Day + firstDayOfWeekIso - 1 - 1) / 7 + 1;

        return weekOfMonth;
    }


    #endregion File and Folder Helpers
}
