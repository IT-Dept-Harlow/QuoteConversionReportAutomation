using OfficeOpenXml;
using QuoteConversionReportAutomation;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;

/// <summary>
/// Provides methods for copying data between Excel sheets and performing related operations asynchronously using BackgroundWorkers with status reporting.
/// </summary>
public class ExcelCopyData
{
    // Constants for column indices.  Using 0-based indexing.
    private const int CustomerColumn = 0; // Column A
    private const int DateColumn = 12;    // Column M
    private const int FinancialYearColumn = 13; // Column N
    private const int SourceFileNameColumn = 11; // Column L
    private const string DataSheetName = "DATA";
    private const string AnalysisSheetName = "Analysis";
    private const string MonthlyOrderPivotSheetName = "OrderPivot";
    private const string MonthlyEstimatePivotSheetName = "Estimate Success PivotTable";
    private const string MonthlyOrderPivotName = "PivotTable6";       // OrderPivot
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
            UseMonthly = useMonthly
        };

        // Define the DoWork event handler.  Use more descriptive variable names.
        worker.DoWork += (sender, eventArgs) =>
        {
            var paramsObj = (dynamic)eventArgs.Argument;
            string sourceFilePathBW = paramsObj.SourceFilePath;
            string sourceSheetNameBW = paramsObj.SourceSheetName;
            string fileSaveLocationBW = paramsObj.FileSaveLocation;
            string destinationFilePathBW = paramsObj.DestinationFilePath;
            string destinationSheetNameBW = paramsObj.DestinationSheetName;
            int startRowBW = paramsObj.StartRow;
            int startColBW = paramsObj.StartCol;
            StatusStrip statusBarBW = paramsObj.StatusBar;
            BackgroundWorker bw = (BackgroundWorker)sender;
            string result = null;
            Action<bool> showViewAnalysisButtonActionBW = paramsObj.ShowViewAnalysisButtonAction;
            Action<string> setFilePathActionBW = paramsObj.SetFilePathAction;
            bool useMonthlyBW = paramsObj.UseMonthly;

            try
            {
                // Set the license context for EPPlus to non-commercial.
                ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;

                // Open the source Excel file.
                using (var sourcePackage = new ExcelPackage(new FileInfo(sourceFilePathBW)))
                {
                    // Get the source worksheet.
                    var sourceWorksheet = sourcePackage.Workbook.Worksheets[sourceSheetNameBW];

                    // Check if the source worksheet exists.
                    if (sourceWorksheet == null)
                    {
                        Logger.LogInfo($"Source sheet '{sourceSheetNameBW}' not found.");
                        eventArgs.Result = null;
                        return; // Return null if the source sheet is not found.
                    }

                    // Open the destination Excel file.
                    using (var destinationPackage = new ExcelPackage(new FileInfo(destinationFilePathBW)))
                    {
                        // Get or create the destination worksheet.
                        var destinationWorksheet = destinationPackage.Workbook.Worksheets[destinationSheetNameBW] ??
                            destinationPackage.Workbook.Worksheets.Add(destinationSheetNameBW);

                        // Get the dimensions of the source worksheet.
                        int sourceRowCount = sourceWorksheet.Dimension?.Rows ?? 0;
                        int sourceColCount = sourceWorksheet.Dimension?.Columns ?? 0;

                        // Get the dimensions of the destination worksheet.
                        int destRow = 2;
                        int destColCount = destinationWorksheet.Dimension?.Columns ?? 0;

                        Logger.LogInfo($"Source Sheet: {sourceSheetNameBW}, Rows: {sourceRowCount}, Cols: {sourceColCount}");
                        Logger.LogInfo($"Destination Sheet: {destinationSheetNameBW}, Rows: {destRow}, Cols: {destColCount}");

                        if (sourceRowCount > 0 && sourceColCount > 0)
                        {
                            if (destColCount < sourceColCount)
                            {
                                for (int i = destColCount + 1; i <= sourceColCount; i++)
                                {
                                    // Add columns until it matches, should never need this loop if using the template.
                                    destinationWorksheet.InsertColumn(i, 1);
                                }
                                destColCount = sourceColCount;
                            }
                            statusBarBW?.Invoke((MethodInvoker)delegate { statusBarBW.Items[0].Text = "Copying Data..."; });

                            // Copy the cell values.
                            CopyDataBetweenExcelSheets(sourcePackage, destinationPackage, sourceWorksheet, destinationWorksheet, startRowBW, sourceRowCount, sourceColCount, ref destRow, destColCount, bw);
                        }
                        else
                        {
                            Logger.LogInfo("Source sheet is empty.");
                        }

                        // Create folder structure if necessary.
                        FolderCreation createFolderStruc = new FolderCreation();
                        destinationPackage.Workbook.FullCalcOnLoad = true;
                        string createdFolderPath = createFolderStruc.CreateFolder(useMonthlyBW, fileSaveLocationBW);

                        string currentDate;
                        string fileName;
                        if (useMonthlyBW)
                        {
                            // Generate the file name with the date (Monthly).
                            currentDate = DateTime.Now.ToString("MMM_yyyy"); // Consistent format
                            fileName = $"Estimate_Success_Rate_{currentDate}.xlsx";
                        }
                        else
                        {
                            // Generate the file name with the date (Weekly).
                            currentDate = DateTime.Now.ToString("yyyyMMdd");
                            fileName = $"{currentDate}_Estimate_Success_Rate.xlsx";
                        }

                        if (createdFolderPath != null)
                        {
                            string fileSaveLocation2 = Path.Combine(createdFolderPath, fileName);
                            FileInfo newFile = new FileInfo(fileSaveLocation2);
                            destinationPackage.Workbook.CalcMode = ExcelCalcMode.Automatic;
                            destinationPackage.SaveAs(newFile);

                            Logger.LogInfo($"Data copied from '{sourceSheetNameBW}' in '{sourceFilePathBW}' to '{destinationSheetNameBW}' in '{destinationFilePathBW}'. Saved to: {fileSaveLocation2}");
                            result = fileSaveLocation2; // Store the file path

                            // Extract unique customers from the "DATA" sheet and populate the "Analysis" sheet
                            ExtractUniqueCustomers(destinationPackage, DataSheetName, AnalysisSheetName, statusBarBW, bw);

                            // Calculate the "Analysis" sheet.
                            CalculateAnalysisSheet(destinationPackage, AnalysisSheetName);

                            // Delete empty rows from the "Analysis" sheet.
                            DeleteEmptyRows(fileSaveLocation2, AnalysisSheetName, statusBarBW, bw, useMonthlyBW);

                            if (!useMonthlyBW)
                            {
                                // Copy data from the "Analysis" sheet to the weekly report.
                                CopyAnalysisDataToWeeklyReport(fileSaveLocation2, statusBarBW, bw);
                            }

                            eventArgs.Result = fileSaveLocation2; // Pass the file path back via eventArgs
                            setFilePathActionBW?.Invoke(fileSaveLocation2); // Call the action to pass back file path to main form
                        }
                        else
                        {
                            Logger.LogError("Failed to create folder structure.");
                            eventArgs.Result = null;
                            return; // Return null if folder creation fails.
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error occurred: {ex.Message}");
                eventArgs.Result = null;
                eventArgs.Cancel = true; // Cancel background thread
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
                    // Send the email here, after successful processing
                    parameters.SendEmailAction?.Invoke(result);
                    // Update the UI elements.
                    parameters.SetButtonTextAction?.Invoke("Create Analysis &\r\nSend Email"); // Change button text
                    parameters.SetButtonTextAction2?.Invoke("Create Report");             // Change button text
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
                    MessageBox.Show("Excel processing failed.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
    /// Copies data from one Excel sheet to another.
    /// </summary>
    private static void CopyDataBetweenExcelSheets(ExcelPackage sourcePackage, ExcelPackage destinationPackage, ExcelWorksheet sourceWorksheet, ExcelWorksheet destinationWorksheet, int startRow, int sourceRowCount, int sourceColCount, ref int destRow, int destColCount, BackgroundWorker worker)
    {
        // Ensure destRow is within the valid range of the destination worksheet.
        if (destRow < 1)
        {
            destRow = 1;
        }

        // Insert row to make sure destination has at least 1 row
        if (destinationWorksheet.Dimension == null || destRow > destinationWorksheet.Dimension.Rows + 1)
        {
            destinationWorksheet.InsertRow(destinationWorksheet.Dimension?.Rows ?? 1, destRow - (destinationWorksheet.Dimension?.Rows ?? 1));
        }

        // Loop to copy the values between the sheets
        for (int sourceRowIndex = startRow; sourceRowIndex <= sourceRowCount; sourceRowIndex++)
        {
            if (worker.CancellationPending)
            {
                return;
            }
            for (int col = 1; col <= sourceColCount; col++)
            {
                if (col <= destColCount && col > 0)
                {
                    try
                    {
                        // Copy the value
                        var sourceCellValue = sourceWorksheet.Cells[sourceRowIndex, col].Value;
                        destinationWorksheet.Cells[destRow, col].Value = sourceCellValue;
#if DEBUG
                        Logger.LogDebug($"Copied from Source[{sourceRowIndex},{col}] to Dest[{destRow},{col}], Value: '{sourceCellValue}'");
#endif
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError($"Error setting cell value at Dest[{destRow},{col}]: {ex.Message}");
                    }
                }
                else
                {
                    Logger.LogError($"Column {col} is out of range in destination sheet. Source Row: {sourceRowIndex}, Source ColCount: {sourceColCount}, Dest ColCount: {destColCount}");
                    break;
                }
            }
            destRow++;
            // Creates a percentage to be used in status bar
            int progress = (int)((double)sourceRowIndex / sourceRowCount * 100);
            worker.ReportProgress(progress, $"Copying data... {progress}%");
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
            analysisSheet.Cells[analysisRow, DateColumn + 1].Value = calTime;    // +1 for 1-based indexing
            analysisSheet.Cells[analysisRow, FinancialYearColumn + 1].Value = GetCurrentFinancialYear(); // +1
            analysisRow++;
        }

        // Save package to ensure the delete step works correctly.
        package.Save();

        Logger.LogInfo($"Unique customers extracted and copied to '{analysisSheetName}'.");
        statusBar?.Invoke((MethodInvoker)delegate { statusBar.Items[0].Text = "Unique customers extracted."; });
    }

    /// <summary>
    /// Calculates and returns the current financial year.
    /// </summary>
    /// <returns>The current financial year in the format "FY YY/YY".</returns>
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
        ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;
        string username = Environment.UserName;
#if DEBUG
        string destinationFilePath = $@"C:\Users\{username}\Harlow Printing\IT - Documents\PowerBI\Quote Conversion Report\Quotes conversion data_wrangled\weekly report quotes conversion merged - copy.xlsx";
#else
        string destinationFilePath = $@"C:\Users\{username}\Harlow Printing\IT - Documents\PowerBI\Quote Conversion Report\Quotes conversion data_wrangled\weekly report quotes conversion merged.xlsx";
#endif
        // Open the source Excel file.
        using (ExcelPackage sourcePackage = new ExcelPackage(new FileInfo(sourceFilePath)))
        {
            // Get the Analysis worksheet.
            ExcelWorksheet sourceWorksheet = sourcePackage.Workbook.Worksheets["Analysis"];

            // Check if the Analysis worksheet exists.
            if (sourceWorksheet == null)
            {
                Logger.LogError("Source worksheet 'Analysis' not found.");
                return;
            }

            sourceWorksheet.Calculate();
            sourcePackage.Workbook.Calculate();

            // Open the destination Excel file (weekly report).
            using (ExcelPackage destinationPackage = new ExcelPackage(new FileInfo(destinationFilePath)))
            {
                // Get the destination worksheet.
                ExcelWorksheet destinationWorksheet = destinationPackage.Workbook.Worksheets[WeeklyReportSheetName];

                // Check if the destination worksheet exists.
                if (destinationWorksheet == null)
                {
                    Logger.LogError($"Destination worksheet '{WeeklyReportSheetName}' not found.");
                    return;
                }

                // Find the next free row in the destination worksheet.
                int nextFreeRow = destinationWorksheet.Dimension?.Rows ?? 0;
                while (destinationWorksheet.Cells[nextFreeRow + 1, 1].Value != null)
                {
                    nextFreeRow++;
                }
                nextFreeRow++;

                // Get the source file name.
                string sourceFileName = Path.GetFileName(sourceFilePath);

                // Get the dimensions of the source worksheet.
                int sourceRowCount = sourceWorksheet.Dimension?.Rows ?? 0;
                int sourceColCount = sourceWorksheet.Dimension?.Columns ?? 0;
                int processedRows = 0;

                // Copy data from the source worksheet to the destination worksheet
                if (sourceRowCount > 0 && sourceColCount > 0)
                {
                    // Start from row 6 (index 5) in the source sheet.
                    for (int sourceRow = 6; sourceRow <= sourceRowCount; sourceRow++)
                    {
                        if (worker != null && worker.CancellationPending)
                        {
                            return;
                        }
                        // Check if there is data in column A of the source row before copying.
                        object cellValueA = sourceWorksheet.Cells[sourceRow, 1].Value;

                        if (cellValueA != null && !string.IsNullOrWhiteSpace(cellValueA.ToString()))
                        {
                            for (int col = 1; col <= sourceColCount; col++)
                            {
                                //  Explicitly get and set the value
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

                destinationPackage.Save();
                Logger.LogInfo($"Data appended to '{destinationFilePath}'.");
            }
        }
        statusBar?.Invoke((MethodInvoker)delegate { statusBar.Items[0].Text = "Data copied to weekly report."; });
    }

    /// <summary>
    /// Deletes rows from a worksheet where the specified column is empty.
    /// </summary>
    private static void DeleteEmptyRows(string filePath, string sheetName, StatusStrip statusBar = null, BackgroundWorker worker = null, bool useMonthly = false)
    {
        // Set the license context for EPPlus to non-commercial.
        ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;

        // Open the Excel file.
        using (ExcelPackage package = new ExcelPackage(new FileInfo(filePath)))
        {
            // Get the worksheet.
            ExcelWorksheet worksheet = package.Workbook.Worksheets[sheetName];

            // Check if the worksheet exists.
            if (worksheet == null)
            {
                Logger.LogInfo($"Sheet '{sheetName}' not found in '{filePath}'.");
                return;
            }

            // Get the number of rows in the worksheet.
            int rowCount = worksheet.Dimension?.Rows ?? 0;
            if (rowCount == null || rowCount <= 0) return;

            // Get the data from Column A into a list (starting from row 7).
            List<object> columnAValues = new List<object>();
            for (int row = 7; row <= rowCount; row++)
            {
                if (worker != null && worker.CancellationPending)
                {
                    return;
                }
                columnAValues.Add(worksheet.Cells[row, CustomerColumn + 1].Value); // +1
            }

            int deletedRows = 0;
            // Iterate in reverse to avoid index shifting issues when deleting rows.
            for (int row = rowCount; row >= 7; row--)
            {
                if (worker != null && worker.CancellationPending)
                {
                    return;
                }
                object cellValue = columnAValues[row - 7];

                // Check if the cell value is empty.
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
            package.Save();
            package.Dispose();
            Logger.LogInfo($"Empty rows deleted from '{filePath}', sheet '{sheetName}'.");
            statusBar?.Invoke((MethodInvoker)delegate { statusBar.Items[0].Text = "Empty rows deleted."; });

            RefreshPivotTablesIfNeeded(useMonthly, filePath, statusBar, worker);
        }
    }

    /// <summary>
    /// Refreshes the specified pivot table in the Excel workbook.
    /// </summary>
    /// <param name="filePath">The path to the Excel file.</param>
    /// <param name="sheetName">The name of the worksheet containing the pivot table.</param>
    /// <param name="pivotTableName">The name of the pivot table to refresh.</param>
    /// <param name="statusBar">Status bar for status reporting.</param>
    /// <param name="worker">Background worker for async operations.</param>
    private static void RefreshPivotTables(string filePath, string sheetName, string pivotTableName, StatusStrip statusBar = null, BackgroundWorker worker = null)
    {
        ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;
        try
        {
            using (ExcelPackage package = new ExcelPackage(new FileInfo(filePath)))
            {
                ExcelWorksheet worksheet = package.Workbook.Worksheets[sheetName];
                if (worksheet != null)
                {
                    Logger.LogInfo($"Worksheet '{sheetName}' found.");
                    foreach (var pivotTable in worksheet.PivotTables)
                    {
                        Logger.LogInfo($"Found pivot table: {pivotTable.Name}");
                        if (pivotTable.Name == pivotTableName)
                        {
                            Logger.LogInfo($"Refreshing pivot table: {pivotTableName}");
                            pivotTable.CacheDefinition.PivotTable.Calculate();
                            pivotTable.Calculate();
                            pivotTable.CacheDefinition.Refresh();
                            package.Workbook.Calculate();
                            package.Save();
                            package.Dispose();
                            Logger.LogInfo($"Pivot table '{pivotTableName}' in sheet '{sheetName}' refreshed.");
                            if (statusBar != null && worker != null)
                            {
                                worker.ReportProgress(100, $"Pivot table '{pivotTableName}' refreshed.");
                                statusBar.Invoke((MethodInvoker)delegate { statusBar.Items[0].Text = $"Pivot table '{pivotTableName}' refreshed."; });
                            }
                            return; // Refresh only the specified pivot table
                        }
                    }
                    Logger.LogWarning($"Pivot table '{pivotTableName}' not found in sheet '{sheetName}'.");
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
