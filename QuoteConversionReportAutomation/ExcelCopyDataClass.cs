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
    // Constants for column indices.
    private const int CustomerColumn = 1;
    private const int DateColumn = 13;
    private const int FinancialYearColumn = 14;
    private const int SourceFileNameColumn = 12;
    private const string DataSheetName = "DATA";
    private const string AnalysisSheetName = "Analysis";
    private const string WeeklyReportSheetName = "2024_25"; // Or your target sheet name i.e 2025_26

    /// <summary>
    /// Copies data from a source Excel sheet to a destination Excel sheet, performing processing steps asynchronously with status reporting.
    /// </summary>
    /// <param name="sourceFilePath">The path to the source Excel file.</param>
    /// <param name="sourceSheetName">The name of the source sheet.</param>
    /// <param name="fileSaveLocation">The location where the new Excel file will be saved.</param>
    /// <param name="destinationFilePath">The path to the destination Excel file.</param>
    /// <param name="destinationSheetName">The name of the destination sheet.</param>
    /// <param name="startRow">The starting row for copying data (default is 1).</param>
    /// <param name="startCol">The starting column for copying data (default is 1).</param>
    /// <param name="statusBar">The StatusStrip control to display status messages.</param>
    /// <param name="sendEmailAction">Action to send email with attachment.</param>
    /// <param name="setButtonTextAction">Action to set button text.</param>
    /// <param name="enableButtonAction">Action to enable button.</param>
    /// <param name="showButtonAction">Action to show button.</param>
    /// <param name="showViewAnalysisButtonAction">Action to show the View Analysis Button</param>
    /// <param name="setFilePathAction">Action to return the fileLocation2 in the main form</param>
    /// <returns>The path to the newly created Excel file, or null if an error occurs.</returns>
    public static string CopyDataBetweenExcelSheetsAsync(string sourceFilePath, string sourceSheetName, string fileSaveLocation, string destinationFilePath, string destinationSheetName, int startRow = 1, int startCol = 1, StatusStrip statusBar = null, Action<string> sendEmailAction = null, Action<string> setButtonTextAction = null, Action<bool> enableButtonAction = null, Action<bool> showButtonAction = null, Action<bool> showViewAnalysisButtonAction = null, Action<string> setFilePathAction = null)
    {
        // Create a new BackgroundWorker instance.
        BackgroundWorker worker = new BackgroundWorker
        {
            WorkerReportsProgress = true,
            WorkerSupportsCancellation = true
        };

        // Define the parameters for the DoWork event.
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
            EnableButtonAction = enableButtonAction,
            ShowButtonAction = showButtonAction,
            ShowViewAnalysisButtonAction = showViewAnalysisButtonAction,
            SetFilePathAction = setFilePathAction // TODO: make this work, currently not passing path to main form
        };

        // Define the DoWork event handler.
        worker.DoWork += (sender, e) =>
        {
            var paramsObj = (dynamic)e.Argument;
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
            Action<bool> showViewAnalysisButtonActionBW = paramsObj.ShowViewAnalysisButtonAction; // Get the View analysis button action
            Action<string> setFilePathActionBW = paramsObj.SetFilePathAction; // Get the file path action

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
                        e.Result = null;
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

                        // Get the dimensions of the destingation worksheet.
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
                                    //add columns until it matches, should never need this loop if using the template
                                    destinationWorksheet.InsertColumn(i, 1);
                                }
                                destColCount = sourceColCount;
                            }
                            statusBarBW?.Invoke((MethodInvoker)delegate { statusBarBW.Items[0].Text = "Copying Data..."; });

                            // Copy the cell value.
                            CopyDataBetweenExcelSheets(sourcePackage, destinationPackage, sourceWorksheet, destinationWorksheet, startRowBW, sourceRowCount, sourceColCount, ref destRow, destColCount, bw);
                        }
                        else
                        {
                            Logger.LogInfo("Source sheet is empty.");
                        }

                        // Create folder structure if necessary.
                        FolderCreation createFolderStruc = new FolderCreation();
                        destinationPackage.Workbook.FullCalcOnLoad = true;
                        string createdFolderPath = createFolderStruc.CreateFolder(fileSaveLocationBW);

                        if (createdFolderPath != null)
                        {
                            // Generate the file name with the date.
                            string currentDate = DateTime.Now.ToString("yyyyMMdd");
                            string fileName = $"{currentDate} Estimate Success Rate.xlsx";
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
                            DeleteEmptyRows(fileSaveLocation2, AnalysisSheetName, statusBarBW, bw);

                            // Copy data from the "Analysis" sheet to the weekly report.
                            CopyAnalysisDataToWeeklyReport(fileSaveLocation2, statusBarBW, bw);

                            e.Result = fileSaveLocation2; // Pass the file path back via param
                            setFilePathActionBW?.Invoke(fileSaveLocation2); // Call the action to pass back file path to main form
                        }
                        else
                        {
                            Logger.LogError("Failed to create folder structure.");
                            e.Result = null;
                            return; // Return null if folder creation fails.
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error occurred: {ex.Message}");
                e.Result = null;
                e.Cancel = true; //cancel background thread
            }
        };

        //sets up the status bar, progress updates, using the background worker
        worker.ProgressChanged += (sender, e) =>
        {
            parameters.StatusBar?.Invoke((MethodInvoker)delegate
            {
                if (e.UserState != null)
                {
                    parameters.StatusBar.Items[0].Text = e.UserState.ToString();
                }
            });
        };

        //background worked, run this when operation has been successful, pass back actions
        worker.RunWorkerCompleted += (sender, e) =>
        {
            if (e.Cancelled)
            {
                MessageBox.Show("Operation was cancelled", "Cancelled", MessageBoxButtons.OK, MessageBoxIcon.Information);
                parameters.SetButtonTextAction?.Invoke("Cancelled"); //set UI to cancelled
                parameters.EnableButtonAction?.Invoke(true); //re enable button 2, to allow the analysis to re run
            }
            else if (e.Error != null)
            {
                MessageBox.Show("An error occurred: " + e.Error.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                parameters.SetButtonTextAction?.Invoke("Error"); //set UI to "Error"
                parameters.EnableButtonAction?.Invoke(true); // re enables button to allow try again
            }
            else
            {
                // Get the result
                if (e.Result is string result)
                {
                    parameters.StatusBar?.Invoke((MethodInvoker)delegate
                    {
                        parameters.StatusBar.Items[0].Text = "Complete";
                    });
                    Logger.LogInfo($"Excel processing completed. File saved to: {result}");
                    // Send the email here, after successful processing
                    parameters.SendEmailAction?.Invoke(result);
                    // Update the UI elements.
                    parameters.SetButtonTextAction?.Invoke("Complete"); // Change button text
                    parameters.SetFilePathAction?.Invoke(result);
                    parameters.EnableButtonAction?.Invoke(false);      // disable the button as all processing is complete now.
                    parameters.ShowButtonAction?.Invoke(false);          // disable the button as all processing is complete now.
                    parameters.ShowViewAnalysisButtonAction?.Invoke(false); //TODO: Fix this as fileLocaton2 is not returning to the main form, disabled for now
                }
                else
                {
                    parameters.StatusBar?.Invoke((MethodInvoker)delegate
                    {
                        parameters.StatusBar.Items[0].Text = "Error";
                    });
                    MessageBox.Show("Excel processing failed.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    parameters.SetButtonTextAction?.Invoke("Error");
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

        //run the worker async, pass in the params
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
            destRow = 1; // Set destRow to 1 if it's less than 1
        }

        //insert romake sure destination has at least 1 row
        if (destinationWorksheet.Dimension == null || destRow > destinationWorksheet.Dimension.Rows + 1)
        {
            destinationWorksheet.InsertRow(destinationWorksheet.Dimension?.Rows ?? 1, destRow - (destinationWorksheet.Dimension?.Rows ?? 1));
        }

        //loop to copy the values between the sheets
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
                        //copy the value
                        var sourceCellValue = sourceWorksheet.Cells[sourceRowIndex, col].Value;
                        destinationWorksheet.Cells[destRow, col].Value = sourceCellValue;
#if DEBUG               //Debug logging, shows all values copied
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
            //creates a percentage to be used in status bar
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

        //Variable for progress tracking
        int processedRows = 0;

        // Extract unique customer names, start from row 3, because of the copying data bug that leaves row 2 blank and row 1 is headers which needs to be skipped.
        for (int row = 3; row <= rowCount; row++)
        {
            if (worker != null && worker.CancellationPending)
            {
                return;
            }
            object cellValue = dataSheet.Cells[row, CustomerColumn].Value;
            if (cellValue != null && !string.IsNullOrWhiteSpace(cellValue.ToString()))
            {
                uniqueCustomers.Add(cellValue.ToString());
            }
            processedRows++;
            if (processedRows % 10 == 0 && statusBar != null && worker != null) // Update every 10 rows(arbitrary)
            {
                int progress = (int)((double)processedRows / rowCount * 100);
                worker.ReportProgress(progress, $"Extracting customers... {progress}%");
            }
        }
        //log the count of unique customers to make sure the correct amount of lines are copied to into the sheets and and most importantly the PowerBI sheet, copied lines should match this
        Logger.LogInfo($"Unique customers count: {uniqueCustomers.Count}");

        //variables for the foreach loop, outside to ensure they don't change
        int analysisRow = 6; // Start from row 6. To skip the Headers in the template.
        string calTime = DateTime.Today.ToString("dd/MM/yyyy");

#if DEBUG //Debug log for checking the calculated time is correct in the worksheet
        Logger.LogDebug(calTime);
#endif

        foreach (string customer in uniqueCustomers)
        {
            if (worker != null && worker.CancellationPending)
            {
                return;
            }
#if DEBUG
            Logger.LogDebug($"{customer}");
#endif    
            //copy the unique customers, and fill out other columns
            analysisSheet.Cells[analysisRow, CustomerColumn].Value = customer;
            analysisSheet.Cells[analysisRow, DateColumn].Value = null;
            analysisSheet.Cells[analysisRow, DateColumn].Value = calTime;
            analysisSheet.Cells[analysisRow, FinancialYearColumn].Value = GetCurrentFinancialYear();
            analysisRow++;
        }

        //save package to ensure the delete step works correctly
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

        // Determine the financial year (assuming it starts in April).
        if (today.Month >= 4)
        {
            startYear = year;
            endYear = year + 1;
        }
        else
        {
            startYear = year - 1;
            endYear = year;
        }

        //returns the last 2 numbers of the year - e.g FY24/25
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

        // Get the current user's username.
        string username = Environment.UserName;
#if DEBUG
// Use a copy of the weekly report file for debugging.
        string destinationFilePath = $@"C:\Users\{username}\Harlow Printing\IT - Documents\PowerBI\Quote Conversion Report\Quotes conversion data_wrangled\weekly report quotes conversion merged - copy.xlsx";
#else
        // Use the actual weekly report file.
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

            // Refresh the worksheet and calculate formulas.
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

                // Find the next free row in the destination worksheet (e.g., in column A).
                int nextFreeRow = destinationWorksheet.Dimension?.Rows ?? 0;
                while (destinationWorksheet.Cells[nextFreeRow + 1, 1].Value != null)
                {
                    nextFreeRow++;
                }
                nextFreeRow++;// Move to the next free row

                // Get the source file name (without the full path).
                string sourceFileName = Path.GetFileName(sourceFilePath);

                // Get the dimensions of the source worksheet.
                int sourceRowCount = sourceWorksheet.Dimension?.Rows ?? 0;
                int sourceColCount = sourceWorksheet.Dimension?.Columns ?? 0;
                int processedRows = 0;

                // Copy data from the source worksheet to the destination worksheet
                if (sourceRowCount > 0 && sourceColCount > 0)
                {
                    for (int sourceRow = 6; sourceRow <= sourceRowCount; sourceRow++) // Start from row 6, skip headers
                    {
                        //makes sure worker is not null or pending Cancellation, returns if true
                        if (worker != null && worker.CancellationPending)
                        {
                            return;
                        }
                        // Check if there is data in column A of the source row before copying.
                        object cellValueA = sourceWorksheet.Cells[sourceRow, 1].Value; // Get value from column A

                        if (cellValueA != null && !string.IsNullOrWhiteSpace(cellValueA.ToString()))
                        {
                            for (int col = 1; col <= sourceColCount; col++)
                            {
                                // *** Explicitly get and set the value ***
                                object cellValue = sourceWorksheet.Cells[sourceRow, col].Value;
#if DEBUG
                                // *** Debugging: Inspect the value ***
                                Debug.WriteLine($"Copying from [{sourceRow},{col}]: Value = '{cellValue}'");
#endif
                                destinationWorksheet.Cells[nextFreeRow, col].Value = cellValue;
                            }
                            // Set column L to the source file name.
                            destinationWorksheet.Cells[nextFreeRow, SourceFileNameColumn].Value = sourceFileName;
                            nextFreeRow++;
                        }
                        // If column A is empty, the row is skipped.

                        //status strip tracking
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
    private static void DeleteEmptyRows(string filePath, string sheetName, StatusStrip statusBar = null, BackgroundWorker worker = null)
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

            // Get the number of rows in the worksheet
            int rowCount = worksheet.Dimension?.Rows ?? 0;
            if (rowCount == null || rowCount <= 0) return; // Add check for empty sheet - might now work, always false

            // Get the data from Column A into a list (starting from row 7, skipping headers).
            List<object> columnAValues = new List<object>();
            for (int row = 7; row <= rowCount; row++)
            {
                if (worker != null && worker.CancellationPending)
                {
                    return;
                }
                columnAValues.Add(worksheet.Cells[row, CustomerColumn].Value);
            }
            int deletedRows = 0;

            // Iterate backwards through the collected data, avoids indexing errors.
            // Adjust loop to start from the *original* rowCount.
            for (int row = rowCount; row >= 7; row--)
            {
                if (worker != null && worker.CancellationPending)
                {
                    return;
                }
                object cellValue = columnAValues[row - 7]; // Adjust index for list

                // Check if the cell value is empty.
                if (cellValue == null || (cellValue is string strValue && string.IsNullOrWhiteSpace(strValue)))
                {
                    worksheet.DeleteRow(row); // Delete the row.
                    deletedRows++;
                    if (deletedRows % 5 == 0 && statusBar != null && worker != null) // Update progress every 5 deleted rows.
                    {
                        int progress = (int)((double)deletedRows / (rowCount - 6) * 100); //subtract 6 because we start at row 7
                        worker.ReportProgress(progress, $"Deleting empty rows... {progress}%");
                    }
                }
            }
            
            package.Workbook.Calculate(); // Calculate the workbook.
            package.Save(); // Save the changes.
            package.Dispose(); // Dispose of the package to release resources.
            Logger.LogInfo($"Empty rows deleted from '{filePath}', sheet '{sheetName}'.");
            statusBar?.Invoke((MethodInvoker)delegate { statusBar.Items[0].Text = "Empty rows deleted."; });
        }
    }
}

