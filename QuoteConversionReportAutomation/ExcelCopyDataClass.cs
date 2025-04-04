using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using QuoteConversionReportAutomation;
using System.Diagnostics;
using OfficeOpenXml.Table;
using System.Linq;

/// <summary>
/// Provides methods for copying data between Excel sheets and performing related operations asynchronously with status reporting.
/// </summary>
public class ExcelCopyData
{
    // Constants for column indices.  Using 0-based indexing.
    private const int CustomerColumn = 0; // Column A
    private const int DateColumn = 12;     // Column M
    private const int FinancialYearColumn = 13; // Column N
    private const int SourceFileNameColumn = 11; // Column L
    private const string DataSheetName = "DATA";
    private const string AnalysisSheetName = "Analysis";
    private const string MonthlyOrderPivotSheetName = "OrderPivot";
    private const string MonthlyEstimatePivotSheetName = "Estimate Success PivotTable";
    private const string MonthlyOrderPivotName = "PivotTable1";       // OrderPivot - Changed from PivotTable6 to PivotTable1
    private const string MonthlyEstimatePivotName = "PivotTable3"; // Estimate Success Pivot
    private const string WeeklyReportSheetName = "2024_25"; // Or your target sheet name i.e 2025_26

    /// <summary>
    /// Copies data from a source Excel sheet to a destination Excel sheet, performing processing steps asynchronously with status reporting.
    /// </summary>
    /// <param name="useMonthly">Indicates whether to use monthly or weekly report processing.</param>
    /// <param name="sourceFilePath">The path to the source Excel file.</param>
    /// <param name="sourceSheetName">The name of the source sheet.</param>
    /// <param name="fileSaveLocation">The location where the new Excel file will be saved.</param>
    /// <param name="destinationFilePath">The path to the destination Excel file.</param>
    /// <param name="destinationSheetName">The name of the destination sheet.</param>
    /// <param name="startRow">The starting row for copying data (default is 1).</param>
    /// <param name="startCol">The starting column for copying data (default is 1).</param>
    /// <param name="statusBar">The StatusStrip control to display status messages.</param>
    /// <param name="sendEmailAction">Action to send email with attachment.</param>
    /// <param name="setButtonTextAction">Action to set button text for the first button.</param>
    /// <param name="setButtonTextAction2">Action to set button text for the second button.</param>
    /// <param name="enableButtonAction">Action to enable the first button.</param>
    /// <param name="enableButtonAction2">Action to enable the second button.</param>
    /// <param name="showButtonAction">Action to show the first button.</param>
    /// <param name="showViewAnalysisButtonAction">Action to show the View Analysis Button.</param>
    /// <param name="setFilePathAction">Action to return the fileLocation2 in the main form.</param>
    /// <returns>The path to the newly created Excel file, or null if an error occurs.</returns>
    public static string CopyDataBetweenExcelSheetsAsync(
        bool useMonthly,
        string sourceFilePath,
        string sourceSheetName,
        string fileSaveLocation,
        string destinationFilePath,
        string destinationSheetName,
        int startRow = 1,
        int startCol = 1,
        StatusStrip statusBar = null,
        Action<string> sendEmailAction = null,
        Action<string> setButtonTextAction = null,
        Action<string> setButtonTextAction2 = null,
        Action<bool> enableButtonAction = null,
        Action<bool> enableButtonAction2 = null,
        Action<bool> showButtonAction = null,
        Action<bool> showViewAnalysisButtonAction = null,
        Action<string> setFilePathAction = null)
    {
        // Create a new BackgroundWorker instance.
        BackgroundWorker worker = new BackgroundWorker
        {
            WorkerReportsProgress = true,
            WorkerSupportsCancellation = true
        };

        // Define the parameters for the DoWork event.  Use descriptive names.
        var parameters = new
        {
            UseMonthly = useMonthly,
            SourceFilePath = sourceFilePath,
            SourceSheetName = sourceSheetName,
            FileSaveLocation = fileSaveLocation,
            DestinationFilePath = destinationFilePath,
            DestinationSheetName = destinationSheetName,
            StartRow = startRow,
            StartCol = startCol,
            StatusBar = statusBar,
            SendEmailAction = sendEmailAction,
            SetButtonTextAction = setButtonTextAction,
            SetButtonTextAction2 = setButtonTextAction2,
            EnableButtonAction = enableButtonAction,
            EnableButtonAction2 = enableButtonAction2,
            ShowButtonAction = showButtonAction,
            ShowViewAnalysisButtonAction = showViewAnalysisButtonAction,
            SetFilePathAction = setFilePathAction,
        };

        // Define the DoWork event handler.
        worker.DoWork += (sender, eventArgs) =>
        {
            var paramsObj = (dynamic)eventArgs.Argument;
            string result = null;
            BackgroundWorker bw = (BackgroundWorker)sender;

            try
            {
                result = CopyDataBetweenExcelSheetsInternal(
                    paramsObj.UseMonthly,
                    paramsObj.SourceFilePath,
                    paramsObj.SourceSheetName,
                    paramsObj.FileSaveLocation,
                    paramsObj.DestinationFilePath,
                    paramsObj.DestinationSheetName,
                    paramsObj.StartRow,
                    paramsObj.StartCol,
                    bw,
                    paramsObj.StatusBar
                    );
                eventArgs.Result = result;
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error in DoWork: {ex.Message}");
                eventArgs.Result = null;
                eventArgs.Cancel = true; // Ensure cancellation is flagged
            }
        };

        // Sets up the status bar, progress updates, using the background worker.
        worker.ProgressChanged += (sender, eventArgs) =>
        {
            parameters.StatusBar?.Invoke((MethodInvoker)delegate
            {
                if (eventArgs.UserState != null)
                {
                    parameters.StatusBar.Items[0].Text = eventArgs.UserState.ToString();
                }
            });
        };

        // Background worker completed, run this when operation has been successful, pass back actions.
        worker.RunWorkerCompleted += (sender, eventArgs) =>
        {
            if (eventArgs.Cancelled)
            {
                MessageBox.Show("Operation was cancelled - Check logs", "Cancelled", MessageBoxButtons.OK, MessageBoxIcon.Information);
                parameters.SetButtonTextAction?.Invoke("Try Again");
                parameters.EnableButtonAction?.Invoke(true);
            }
            else if (eventArgs.Error != null)
            {
                MessageBox.Show("An error occurred: " + eventArgs.Error.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                parameters.SetButtonTextAction?.Invoke("Try Again");
                parameters.EnableButtonAction?.Invoke(true);
            }
            else
            {
                // Get the result
                if (eventArgs.Result is string result)
                {
                    parameters.StatusBar?.Invoke((MethodInvoker)delegate
                    {
                        parameters.StatusBar.Items[0].Text = "Complete";
                    });
                    Logger.LogInfo($"Excel processing completed. File saved to: {result}");
                    // Send the email here, after successful processing only if it's not the monthly report, will change after approval of report
                    if (!useMonthly)
                    {
                        parameters.SendEmailAction?.Invoke(result);
                    }
                    else
                    {
                        MessageBox.Show("Opening Excel File. Please manually refresh PivotTables and Slicers and close excel to allow program to continue.", "Opening Excel File...", MessageBoxButtons.OK, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1, MessageBoxOptions.DefaultDesktopOnly);

                        try
                        {
                            // Open the file using the default associated program
                            Process process = Process.Start(result);
                            if (process != null)
                            {
                                process.WaitForExit(); // Wait for the process to exit.
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.LogError($"Error opening file: {ex.Message}");
                            MessageBox.Show($"Error opening file: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.DefaultDesktopOnly);
                        }
                        parameters.SendEmailAction?.Invoke(result); //send email
                    }

                    // Update the UI elements.
                    parameters.SetButtonTextAction?.Invoke("Create Analysis &\r\nSend Email"); // Change button text
                    parameters.SetButtonTextAction2?.Invoke("Create Report");                                 // Change button text
                    parameters.SetFilePathAction?.Invoke(result);
                    parameters.EnableButtonAction?.Invoke(false);
                    parameters.EnableButtonAction2?.Invoke(true);
                    parameters.ShowButtonAction?.Invoke(false);
                    parameters.ShowViewAnalysisButtonAction?.Invoke(false); // Make sure this is called.
                }
                else
                {
                    parameters.StatusBar?.Invoke((MethodInvoker)delegate
                    {
                        parameters.StatusBar.Items[0].Text = "Error";
                    });
                    MessageBox.Show("Excel processing failed.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.DefaultDesktopOnly);
                    parameters.SetButtonTextAction?.Invoke("Try again");
                    parameters.EnableButtonAction?.Invoke(true);
                }
                if (parameters.StatusBar != null && parameters.StatusBar.Parent != null)
                {
                    parameters.StatusBar.Invoke((MethodInvoker)delegate
                    {
                        parameters.StatusBar.Parent.Enabled = true;
                    });
                }
            }
        };

        // Run the worker async, pass in the params
        worker.RunWorkerAsync(parameters);
        return null;
    }

    /// <summary>
    /// Internal method to copy data between Excel sheets.  Handles file operations and calls the actual copy logic.
    /// </summary>
    private static string CopyDataBetweenExcelSheetsInternal(
        bool useMonthly,
        string sourceFilePath,
        string sourceSheetName,
        string fileSaveLocation,
        string destinationFilePath,
        string destinationSheetName,
        int startRow,
        int startCol,
        BackgroundWorker worker,
        StatusStrip statusBar)
    {
        ExcelPackage.License.SetNonCommercialPersonal("Harlow");
        string result = null;

        using (var sourcePackage = new ExcelPackage(new FileInfo(sourceFilePath)))
        using (var destinationPackage = new ExcelPackage(new FileInfo(destinationFilePath)))
        {
            var sourceWorksheet = sourcePackage.Workbook.Worksheets[sourceSheetName];
            if (sourceWorksheet == null)
            {
                Logger.LogInfo($"Source sheet '{sourceSheetName}' not found.");
                return null;
            }

            var destinationWorksheet = destinationPackage.Workbook.Worksheets[destinationSheetName] ??
                                         destinationPackage.Workbook.Worksheets.Add(destinationSheetName);

            int sourceRowCount = sourceWorksheet.Dimension?.Rows ?? 0;
            int sourceColCount = sourceWorksheet.Dimension?.Columns ?? 0;
            int destRow = 2;
            int destColCount = destinationWorksheet.Dimension?.Columns ?? 0;

            Logger.LogInfo($"Source Sheet: {sourceSheetName}, Rows: {sourceRowCount}, Cols: {sourceColCount}");
            Logger.LogInfo($"Destination Sheet: {destinationSheetName}, Rows: {destRow}, Cols: {destColCount}");

            if (sourceRowCount > 0 && sourceColCount > 0)
            {
                if (destColCount < sourceColCount)
                {
                    for (int i = destColCount + 1; i <= sourceColCount; i++)
                    {
                        destinationWorksheet.InsertColumn(i, 1);
                    }
                    destColCount = sourceColCount;
                }
                statusBar?.Invoke((MethodInvoker)delegate { statusBar.Items[0].Text = "Copying Data..."; });
                CopyDataBetweenExcelSheets(sourceWorksheet, destinationWorksheet, startRow, sourceRowCount, sourceColCount, ref destRow, destColCount, worker, statusBar); // Pass statusBar
            }
            else
            {
                Logger.LogInfo("Source sheet is empty.");
            }
            // Create folder structure if necessary.
            FolderCreation createFolderStruc = new FolderCreation();
            destinationPackage.Workbook.FullCalcOnLoad = true;
            string createdFolderPath = createFolderStruc.CreateFolder(useMonthly, fileSaveLocation);

            string currentDate;
            string fileName;
            if (useMonthly)
            {
                DateTime now = DateTime.Now;
                DateTime targetMonth = now.Day <= 15 ? now.AddMonths(-1) : now;
                currentDate = targetMonth.ToString("MMM_yyyy");
                fileName = $"Estimate_Success_Rate_{currentDate}.xlsx";
            }
            else
            {
                currentDate = DateTime.Now.ToString("yyyyMMdd");
                fileName = $"{currentDate}_Estimate_Success_Rate.xlsx";
            }

            if (createdFolderPath != null)
            {
                string fileSaveLocation2 = Path.Combine(createdFolderPath, fileName);
                FileInfo newFile = new FileInfo(fileSaveLocation2);
                destinationPackage.SaveAs(newFile);
                Logger.LogInfo($"Data copied from '{sourceSheetName}' in '{sourceFilePath}' to '{destinationSheetName}' in '{destinationFilePath}'. Saved to: {fileSaveLocation2}");
                result = fileSaveLocation2;

                ExtractUniqueCustomers(destinationPackage, DataSheetName, AnalysisSheetName, statusBar, worker);
                CalculateAnalysisSheet(destinationPackage, AnalysisSheetName);
                DeleteEmptyRows(fileSaveLocation2, AnalysisSheetName, statusBar, worker, useMonthly);

                if (!useMonthly)
                {
                    CopyAnalysisDataToWeeklyReport(fileSaveLocation2, statusBar, worker);
                }
            }
            else
            {
                Logger.LogError("Failed to create folder structure.");
                return null;
            }
        }
        return result;
    }

    /// <summary>
    /// Copies data from one Excel sheet to another using Range.Copy.
    /// </summary>
    private static void CopyDataBetweenExcelSheets(ExcelWorksheet sourceWorksheet, ExcelWorksheet destinationWorksheet, int startRow, int sourceRowCount, int sourceColCount, ref int destRow, int destColCount, BackgroundWorker worker, StatusStrip statusBar)
    {
        // Ensure destRow is within the valid range of the destination worksheet.
        if (destRow < 1)
        {
            destRow = 1;
        }

        try
        {
            // Log the parameters for debugging
            Logger.LogInfo($"CopyDataBetweenExcelSheets: startRow = {startRow}, sourceRowCount = {sourceRowCount}, sourceColCount = {sourceColCount}, destRow = {destRow}, destColCount = {destColCount}");
            Logger.LogInfo($"Source worksheet dimension rows: {sourceWorksheet.Dimension?.Rows}");

            // Get the source range to copy.
            ExcelRange sourceRange = sourceWorksheet.Cells[1, 1, sourceRowCount, sourceColCount];
            int rowsToCopy = sourceRowCount - 1 + 1;

            // Ensure that the destination worksheet has enough rows.
            if (destinationWorksheet.Dimension == null)
            {
                destinationWorksheet.InsertRow(1, rowsToCopy);
                Logger.LogInfo($"Destination sheet was empty. Inserted {rowsToCopy} rows.");
            }
            else if (destinationWorksheet.Dimension.Rows < destRow + rowsToCopy - 1)
            {
                int existingRows = destinationWorksheet.Dimension.Rows;
                int missingRows = destRow + rowsToCopy - 1 - existingRows;
                destinationWorksheet.InsertRow(existingRows + 1, missingRows);
                Logger.LogInfo($"Destination sheet had {existingRows} rows. Inserted {missingRows} rows starting from {existingRows + 1}.");
            }
            else
            {
                Logger.LogInfo($"Destination sheet has enough rows ({destinationWorksheet.Dimension.Rows}).");
            }

            // Get the starting cell for the destination range.
            ExcelRange destinationStartCell = destinationWorksheet.Cells[destRow, 1];

            // Copy the source range to the destination.
            sourceRange.Copy(destinationStartCell);

            // Calculate the new destRow.
            destRow = destRow + rowsToCopy;
            worker.ReportProgress(100, $"Data copied using Range.Copy");
            statusBar?.Invoke((MethodInvoker)delegate { statusBar.Items[0].Text = "Data Copied"; });

        }
        catch (Exception ex)
        {
            Logger.LogError($"Error copying data using Range.Copy: {ex.Message}");
            throw; // Re-throw the exception to be handled by the caller.
        }
    }

    /// <summary>
    /// Extracts unique customers from the data sheet and populates the analysis sheet.
    /// </summary>
    private static void ExtractUniqueCustomers(ExcelPackage package, string dataSheetName, string analysisSheetName, StatusStrip statusBar = null, BackgroundWorker worker = null)
    {
        // Get the data and analysis worksheets.
        ExcelWorksheet dataSheet = package.Workbook.Worksheets[dataSheetName];
        ExcelWorksheet analysisSheet = package.Workbook.Worksheets[analysisSheetName] ?? package.Workbook.Worksheets.Add(analysisSheetName);

        // Check if the data sheet exists.
        if (dataSheet == null)
        {
            Logger.LogError($"Data sheet '{dataSheetName}' not found.");
            return;
        }

        // Get the number of rows in the data sheet.
        int rowCount = dataSheet.Dimension?.Rows ?? 0;
        HashSet<string> uniqueCustomers = new HashSet<string>();

        // Variable for progress tracking
        int processedRows = 0;

        // Extract unique customer names, start from row 3 (index 2).
        for (int row = 3; row <= rowCount; row++)
        {
            if (worker != null && worker.CancellationPending)
            {
                return;
            }
            object cellValue = dataSheet.Cells[row, CustomerColumn + 1].Value; // +1 for 1-based indexing
            if (cellValue != null && !string.IsNullOrWhiteSpace(cellValue.ToString()))
            {
                uniqueCustomers.Add(cellValue.ToString());
            }
            processedRows++;
            if (processedRows % 10 == 0 && statusBar != null && worker != null)
            {
                int progress = (int)((double)processedRows / rowCount * 100);
                worker.ReportProgress(progress, $"Extracting customers... {progress}%");
            }
        }
        Logger.LogInfo($"Unique customers count: {uniqueCustomers.Count}");

        // Variables for the foreach loop.
        int analysisRow = 6;
        string calTime = DateTime.Today.ToString("dd/MM/yyyy");

        foreach (string customer in uniqueCustomers)
        {
            if (worker != null && worker.CancellationPending)
            {
                return;
            }
#if DEBUG
            Logger.LogDebug($"{customer}");
#endif
            analysisSheet.Cells[analysisRow, CustomerColumn + 1].Value = customer; // +1 for 1-based indexing
            analysisSheet.Cells[analysisRow, DateColumn + 1].Value = calTime;     // +1 for 1-based indexing
            analysisSheet.Cells[analysisRow, FinancialYearColumn + 1].Value = GetCurrentFinancialYear(); // +1
            analysisRow++;
        }

        // Save package to ensure the delete step works correctly.
        package.SaveAsync();

        Logger.LogInfo($"Unique customers extracted and copied to '{analysisSheetName}'.");
        statusBar?.Invoke((MethodInvoker)delegate { statusBar.Items[0].Text = "Unique customers extracted."; });
    }

    /// <summary>
    /// Calculates and returns the current financial year.
    /// </summary>
    private static string GetCurrentFinancialYear()
    {
        DateTime today = DateTime.Today;
        int year = today.Year;
        int startYear;
        int endYear;

        // Determine the financial year.  Using May as the start month.
        if (today.Month >= 5)
        {
            startYear = year;
            endYear = year + 1;
        }
        else
        {
            startYear = year - 1;
            endYear = year;
        }

        return $"FY {startYear.ToString().Substring(2)}/{endYear.ToString().Substring(2)}";
    }

    /// <summary>
    /// Calculates the Analysis sheet.  Added to ensure calculations are performed.
    /// </summary>
    private static void CalculateAnalysisSheet(ExcelPackage package, string sheetName)
    {
        ExcelWorksheet analysisSheet = package.Workbook.Worksheets[sheetName];
        if (analysisSheet != null)
        {
            analysisSheet.Calculate();
            Logger.LogInfo($"Analysis sheet '{sheetName}' calculations performed.");
        }
        else
        {
            Logger.LogError($"Analysis sheet '{sheetName}' not found.");
        }
    }

    /// <summary>
    /// Copies data from the Analysis sheet to the weekly report, appending it to the next free row.
    /// </summary>
    private static void CopyAnalysisDataToWeeklyReport(string sourceFilePath, StatusStrip statusBar = null, BackgroundWorker worker = null)
    {
        ExcelPackage.License.SetNonCommercialPersonal("Harlow");
        string username = Environment.UserName;
#if DEBUG
        string destinationFilePath = $@"C:\Users\{username}\Harlow Printing\IT - Documents\PowerBI\Quote Conversion Report\Quotes conversion data_wrangled\weekly report quotes conversion merged - copy.xlsx";
#else
        string destinationFilePath = $@"C:\Users\{username}\Harlow Printing\IT - Documents\PowerBI\Quote Conversion Report\Quotes conversion data_wrangled\weekly report quotes conversion merged.xlsx";
#endif

        using (ExcelPackage sourcePackage = new ExcelPackage(new FileInfo(sourceFilePath)))
        using (ExcelPackage destinationPackage = new ExcelPackage(new FileInfo(destinationFilePath)))
        {
            ExcelWorksheet sourceWorksheet = sourcePackage.Workbook.Worksheets["Analysis"];
            if (sourceWorksheet == null)
            {
                Logger.LogError("Source worksheet 'Analysis' not found.");
                return;
            }

            sourceWorksheet.Calculate();
            sourcePackage.Workbook.Calculate();

            ExcelWorksheet destinationWorksheet = destinationPackage.Workbook.Worksheets[WeeklyReportSheetName];
            if (destinationWorksheet == null)
            {
                Logger.LogError($"Destination worksheet '{WeeklyReportSheetName}' not found.");
                return;
            }

            int nextFreeRow = destinationWorksheet.Dimension?.Rows ?? 0;
            while (destinationWorksheet.Cells[nextFreeRow + 1, 1].Value != null)
            {
                nextFreeRow++;
            }
            nextFreeRow++;

            string sourceFileName = Path.GetFileName(sourceFilePath);
            int sourceRowCount = sourceWorksheet.Dimension?.Rows ?? 0;
            int sourceColCount = destinationWorksheet.Dimension?.Columns ?? 0;
            int processedRows = 0;

            if (sourceRowCount > 0 && sourceColCount > 0)
            {
                for (int sourceRow = 6; sourceRow <= sourceRowCount; sourceRow++)
                {
                    if (worker != null && worker.CancellationPending)
                    {
                        return;
                    }
                    object cellValueA = sourceWorksheet.Cells[sourceRow, 1].Value;
                    if (cellValueA != null && !string.IsNullOrWhiteSpace(cellValueA.ToString()))
                    {
                        for (int col = 1; col <= sourceColCount; col++)
                        {
                            object cellValue = sourceWorksheet.Cells[sourceRow, col].Value;
#if DEBUG
                            Debug.WriteLine($"Copying from [{sourceRow},{col}]: Value = '{cellValue}'");
#endif
                            destinationWorksheet.Cells[nextFreeRow, col].Value = cellValue;
                        }
                        destinationWorksheet.Cells[nextFreeRow, SourceFileNameColumn + 1].Value = sourceFileName; // +1
                        nextFreeRow++;
                    }
                    processedRows++;
                    if (processedRows % 10 == 0 && statusBar != null && worker != null)
                    {
                        int progress = (int)((double)processedRows / sourceRowCount * 100);
                        worker.ReportProgress(progress, $"Copying analysis data... {progress}%");
                    }
                }
            }
            else
            {
                Logger.LogInfo("Source worksheet is empty.");
            }

            destinationPackage.SaveAsync();
            Logger.LogInfo($"Data appended to '{destinationFilePath}'.");
        }
        statusBar?.Invoke((MethodInvoker)delegate { statusBar.Items[0].Text = "Data copied to weekly report."; });
    }

    /// <summary>
    /// Deletes rows from a worksheet where the specified column is empty.
    /// </summary>
    private static void DeleteEmptyRows(string filePath, string sheetName, StatusStrip statusBar = null, BackgroundWorker worker = null, bool useMonthly = false)
    {
        ExcelPackage.License.SetNonCommercialPersonal("Harlow");
        using (ExcelPackage package = new ExcelPackage(new FileInfo(filePath)))
        {
            ExcelWorksheet worksheet = package.Workbook.Worksheets[sheetName];
            if (worksheet == null)
            {
                Logger.LogInfo($"Sheet '{sheetName}' not found in '{filePath}'.");
                return;
            }

            int rowCount = worksheet.Dimension?.Rows ?? 0;
            if (rowCount <= 0) return;

            List<object> columnAValues = new List<object>();
            for (int row = 7; row <= rowCount; row++)
            {
                if (worker != null && worker.CancellationPending)
                {
                    return;
                }
                columnAValues.Add(worksheet.Cells[row, CustomerColumn + 1].Value); // +1 indexing 
            }

            int deletedRows = 0;
            for (int row = rowCount; row >= 7; row--)
            {
                if (worker != null && worker.CancellationPending)
                {
                    return;
                }
                object cellValue = columnAValues[row - 7];
                if (cellValue == null || (cellValue is string strValue && string.IsNullOrWhiteSpace(strValue)))
                {
                    worksheet.DeleteRow(row);
                    deletedRows++;
                    if (deletedRows % 5 == 0 && statusBar != null && worker != null)
                    {
                        int progress = (int)((double)deletedRows / (rowCount - 6) * 100);
                        worker.ReportProgress(progress, $"Deleting empty rows... {progress}%");
                    }
                }
            }

            package.Workbook.Calculate();
            package.SaveAsync();
            package.Dispose();
            Logger.LogInfo($"Empty rows deleted from '{filePath}', sheet '{sheetName}'.");
            statusBar?.Invoke((MethodInvoker)delegate { statusBar.Items[0].Text = "Empty rows deleted."; });
            RefreshPivotTablesIfNeeded(useMonthly, filePath, statusBar, worker);
        }
    }

    /// <summary>
    /// Refreshes the specified pivot table.
    /// </summary>
    private static void RefreshPivotTables(string filePath, string sheetName, string pivotTableName, StatusStrip statusBar = null, BackgroundWorker worker = null)
    {
        ExcelPackage.License.SetNonCommercialPersonal("Harlow");
        try
        {
            using (ExcelPackage package = new ExcelPackage(new FileInfo(filePath)))
            {
                ExcelWorksheet worksheet = package.Workbook.Worksheets[sheetName];
                if (worksheet != null)
                {
                    Logger.LogInfo($"Worksheet '{sheetName}' found.");
                    bool pivotTableRefreshed = false; // Track if the pivot table was actually refreshed

                    //gets all pivot tables in the sheet.
                    foreach (var pivotTable in worksheet.PivotTables)
                    {
                        Logger.LogInfo($"Found pivot table: {pivotTable.Name}");
                        if (pivotTable.Name == pivotTableName)
                        {
                            Logger.LogInfo($"Refreshing pivot table: {pivotTableName}");
                            package.Workbook.FullCalcOnLoad = true;
                            pivotTable.CacheDefinition.PivotTable.Calculate();
                            pivotTable.Calculate();
                            package.Workbook.Calculate();
                            pivotTableRefreshed = true; // Set to true after refresh
                            break; // Exit the loop after refreshing the target pivot table
                        }
                    }

                    // Refresh slicers only if the pivot table was refreshed
                    if (pivotTableRefreshed)
                    {
                        package.SaveAsync();
                        package.Dispose();
                        Logger.LogInfo($"Pivot table '{pivotTableName}' in sheet '{sheetName}' refreshed.");
                        if (statusBar != null && worker != null)
                        {
                            worker.ReportProgress(100, $"Pivot table '{pivotTableName}' refreshed.");
                            statusBar.Invoke((MethodInvoker)delegate { statusBar.Items[0].Text = $"Pivot table '{pivotTableName}' refreshed."; });
                        }
                    }
                    else
                    {
                        Logger.LogWarning($"Pivot table '{pivotTableName}' not found in sheet '{sheetName}'.");
                    }
                }
                else
                {
                    Logger.LogError($"Worksheet '{sheetName}' not found.");
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogError($"Error refreshing pivot table: {ex.Message}");
        }
    }

    /// <summary>
    /// Refreshes pivot tables if the report is a monthly report.
    /// </summary>
    private static void RefreshPivotTablesIfNeeded(bool useMonthly, string filePath, StatusStrip statusBar = null, BackgroundWorker worker = null)
    {
        if (useMonthly)
        {
            RefreshPivotTables(filePath, MonthlyOrderPivotSheetName, MonthlyOrderPivotName, statusBar, worker);
            RefreshPivotTables(filePath, MonthlyEstimatePivotSheetName, MonthlyEstimatePivotName, statusBar, worker);
            Logger.LogInfo("Pivot tables refreshed after deleting empty rows (useMonthly = true).");
            statusBar?.Invoke((MethodInvoker)delegate { statusBar.Items[0].Text = "Pivot tables refreshed."; });
        }
        else
        {
            Logger.LogInfo("Pivot tables not refreshed (useMonthly = false).");
        }
    }
}

