using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace QuoteConversionReportAutomation
{
    /// <summary>
    /// Provides methods for copying data between Excel sheets and performing related operations asynchronously with status reporting.
    /// </summary>
    public class ExcelCopyData
    {
        #region Constants

        // Constants for column indices. Use 0-based indexing.
        private const int CustomerColumn = 0;       // Column A
        private const int DateColumn = 12;          // Column M
        private const int FinancialYearColumn = 13; // Column N
        private const int SourceFileNameColumn = 11; // Column L

        private const string DataSheetName = "DATA";
        private const string AnalysisSheetName = "Analysis";
        private const string MonthlyOrderPivotSheetName = "OrderPivot";
        private const string MonthlyEstimatePivotSheetName = "Estimate Success PivotTable";
        private const string MonthlyOrderPivotName = "PivotTable1";       // OrderPivot
        private const string MonthlyEstimatePivotName = "PivotTable3"; // Estimate Success Pivot

        #endregion Constants

        #region Public Methods

        /// <summary>
        /// Copies data from a source Excel sheet to a destination Excel sheet, performing related operations asynchronously with status reporting.
        /// </summary>
        /// <param name="selectedFinYear">User selected financial year.</param>
        /// <param name="reportType">Indicates the report type: 0 = Weekly, 1 = Monthly, 2 = Quarterly, 3 = Annual.</param>
        /// <param name="sendEmailonlytoFemi">Indicates whether to send the email only to Femi.</param>
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
        /// <param name="enableDatePickFromAction">Action to enable the DatePickerFrom.</param>
        /// <param name="enableDatePickToAction">Action to enable the DatePickerTo.</param>
        /// <param name="enableFinYearDropBoxAction">Action to enable the FinYearDropBox.</param>
        /// <param name="enableCheckBox1Action">Action to enable the CheckBox1.</param>
        /// <param name="enableTypeDropBoxAction">Action to enable the TypeDropBoxAction.</param>
        /// <returns>The path to the newly created Excel file, or null if an error occurs.</returns>
        public static string CopyDataBetweenExcelSheetsAsync(
            string selectedFinYear,
            int reportType,
            bool sendEmailonlytoFemi,
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
            Action<bool> enableDatePickFromAction = null,
            Action<bool> enableDatePickToAction = null,
            Action<bool> enableFinYearDropBoxAction = null,
            Action<bool> enableCheckBox1Action = null,
            Action<bool> enableTypeDropBoxAction = null,
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
                ReportType = reportType,
                SendEmailOnlyToFemi = sendEmailonlytoFemi,
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
                EnableDatePickFromAction = enableDatePickFromAction,
                EnableDatePickToAction = enableDatePickToAction,
                EnableFinYearDropBoxAction = enableFinYearDropBoxAction,
                EnableCheckBox1Action = enableCheckBox1Action,
                ShowViewAnalysisButtonAction = showViewAnalysisButtonAction,
                EnableTypeDropBoxAction = enableTypeDropBoxAction,
                SelectedFinYear = selectedFinYear, // Include selectedFinYear in parameters
                SetFilePathAction = setFilePathAction
            };

            // Define the DoWork event handler.
            worker.DoWork += (sender, eventArgs) =>
            {
                DoWorkHandler(sender, eventArgs, parameters);
            };

            // Sets up the status bar, progress updates, using the background worker.
            worker.ProgressChanged += (sender, eventArgs) =>
            {
                ProgressChangedHandler(eventArgs, parameters);
            };

            // Background worker completed, run this when operation has been successful, pass back actions.
            worker.RunWorkerCompleted += (sender, eventArgs) =>
            {
                RunWorkerCompletedHandler(eventArgs, parameters);
            };

            // Run the worker async, pass in the params
            worker.RunWorkerAsync(parameters);
            return null;
        }

        #endregion Public Methods

        #region Private Methods

        /// <summary>
        /// Handles the DoWork event of the BackgroundWorker.
        /// </summary>
        private static void DoWorkHandler(object sender, DoWorkEventArgs eventArgs, dynamic parameters)
        {
            string result = null;
            BackgroundWorker bw = (BackgroundWorker)sender;
            string fileSaveLocation2 = null; // Declare this outside the try block
            bool fileExists = false;
            string createdFolderPath = CreateFolders(parameters.ReportType, parameters.FileSaveLocation);


            // First check if the file exists
            fileSaveLocation2 = GenerateFileName(parameters.ReportType, createdFolderPath); // Get the expected file name
            if (File.Exists(fileSaveLocation2))
            {
                fileExists = true;
                Logger.LogInfo($"File Exists = {fileExists} File Path = {fileSaveLocation2}");// Log if file exists and it's path

                // Ask the user if they want to use the existing file.
                DialogResult dialogResult = MessageBox.Show(
                    $"File '{Path.GetFileName(fileSaveLocation2)}' already exists. Do you want to load this file and send it as an attachment?",
                    "File Exists",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (dialogResult == DialogResult.Yes)
                {
                    result = fileSaveLocation2; // Set the result to the existing file path
                    eventArgs.Result = result;
                    return; // Exit the worker method, skipping file creation
                }
                // If the user chooses No, continue with the report generation
                else
                {
                    //File.Delete(fileSaveLocation2);
                }
            }

            try
            {

                result = CopyDataBetweenExcelSheetsInternal(
                    parameters.ReportType,
                    parameters.SourceFilePath,
                    parameters.SourceSheetName,
                    parameters.FileSaveLocation,
                    parameters.DestinationFilePath,
                    parameters.DestinationSheetName,
                    parameters.StartRow,
                    parameters.StartCol,
                    bw,
                    parameters.StatusBar,
                    parameters.SelectedFinYear
                    );
                eventArgs.Result = result;
                if (string.IsNullOrEmpty(result))
                {
                    eventArgs.Cancel = true;
                    return;
                }

                //generate file name
                using (ExcelPackage package = new ExcelPackage(new FileInfo(result)))
                {
                    string temp = result;

                    fileSaveLocation2 = GenerateFileNameAndSave(package, parameters.ReportType, createdFolderPath);

                    File.Delete(temp);
                }
                eventArgs.Result = fileSaveLocation2;



            }
            catch (IOException ex)
            {
                // Check for file access exception; handle it and potentially retry.
                if (ex.HResult == -2147024864) // 0x80070020: The process cannot access the file
                {
                    if (!TryAgainHandler(ex, parameters, bw))
                    {
                        eventArgs.Cancel = true;
                        return;
                    }
                }
                else
                {
                    Logger.LogError($"Error in DoWork: {ex.Message}");
                    eventArgs.Result = null;
                    eventArgs.Cancel = true; // Ensure cancellation is flagged
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error in DoWork: {ex.Message}");
                eventArgs.Result = null;
                eventArgs.Cancel = true; // Ensure cancellation is flagged
            }
        }

        private static bool TryAgainHandler(IOException ex, dynamic parameters, BackgroundWorker bw)
        {
            int tryCount = 0;
            const int maxTries = 3;
            do
            {
                tryCount++;
                Logger.LogWarning($"File access error (attempt {tryCount}/{maxTries}): {ex.Message}. Retrying...");
                parameters.StatusBar?.Invoke((MethodInvoker)delegate
                {
                    parameters.StatusBar.Items[0].Text = $"File access error, retrying... ({tryCount}/{maxTries})";
                });
                DialogResult retryResult = MessageBox.Show("Please close the Excel file and click OK to try again.", "File In Use", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning);
                if (retryResult == DialogResult.OK)
                {
                    System.Threading.Thread.Sleep(1000); // Wait before retrying
                    return true;
                }
                else
                {
                    bw.CancelAsync();
                    return false; // User cancelled, exit the method.
                }
            } while (tryCount < maxTries);
        }

        /// <summary>
        /// Handles the ProgressChanged event of the BackgroundWorker.
        /// </summary>
        private static void ProgressChangedHandler(ProgressChangedEventArgs eventArgs, dynamic parameters)
        {
            parameters.StatusBar?.Invoke((MethodInvoker)delegate
            {
                if (eventArgs.UserState != null)
                {
                    parameters.StatusBar.Items[0].Text = eventArgs.UserState.ToString();
                }
            });
        }

        /// <summary>
        /// Handles the RunWorkerCompleted event of the BackgroundWorker.
        /// </summary>
        private static void RunWorkerCompletedHandler(RunWorkerCompletedEventArgs eventArgs, dynamic parameters)
        {
            if (eventArgs.Cancelled)
            {
                MessageBox.Show("Operation was cancelled - Check logs", "Cancelled", MessageBoxButtons.OK, MessageBoxIcon.Information);
                parameters.SetButtonTextAction?.Invoke("Try Again");
                parameters.EnableButtonAction?.Invoke(true);
                parameters.EnableDatePickFromAction?.Invoke(true);
                parameters.EnableDatePickToAction?.Invoke(true);
                parameters.EnableFinYearDropBoxAction?.Invoke(true);
                parameters.EnableCheckBox1Action?.Invoke(true);
            }
            else if (eventArgs.Error != null)
            {
                MessageBox.Show("An error occurred: " + eventArgs.Error.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                parameters.SetButtonTextAction?.Invoke("Try Again");
                parameters.EnableButtonAction?.Invoke(true);
                parameters.EnableDatePickFromAction?.Invoke(true);
                parameters.EnableDatePickToAction?.Invoke(true);
                parameters.EnableFinYearDropBoxAction?.Invoke(true);
                parameters.EnableCheckBox1Action?.Invoke(true);
                parameters.EnableTypeDropBoxAction?.Invoke(true);
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

                    //  Moved email sending into SendEmail, runs after the Excel package is disposed.
                    string emailResult = CopyDataBetweenExcelSheetsInternal_PostProcess(result, parameters.ReportType, parameters.SendEmailAction, result); // Pass the file path
                    if (emailResult != null)
                    {
                        Logger.LogInfo(emailResult);
                    }

                    // Update the UI elements.
                    parameters.SetButtonTextAction?.Invoke("Create Analysis &\r\nSend Email"); // Change button text
                    parameters.SetButtonTextAction2?.Invoke("Create Report");                                         // Change button text
                    parameters.SetFilePathAction?.Invoke(result);
                    parameters.EnableButtonAction?.Invoke(false); // create report button
                    parameters.EnableButtonAction2?.Invoke(true);//report button 
                    parameters.ShowButtonAction?.Invoke(true);
                    parameters.ShowViewAnalysisButtonAction?.Invoke(true); // Make sure this is called.
                    parameters.EnableDatePickFromAction?.Invoke(true);
                    parameters.EnableDatePickToAction?.Invoke(true);
                    parameters.EnableFinYearDropBoxAction?.Invoke(true);
                    parameters.EnableCheckBox1Action?.Invoke(true);
                    parameters.EnableTypeDropBoxAction?.Invoke(true);
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
                    parameters.EnableDatePickFromAction?.Invoke(true);
                    parameters.EnableDatePickToAction?.Invoke(true);
                    parameters.EnableFinYearDropBoxAction?.Invoke(true);
                    parameters.EnableCheckBox1Action?.Invoke(true);
                    parameters.EnableTypeDropBoxAction?.Invoke(true);
                }
            }
            if (parameters.StatusBar != null && parameters.StatusBar.Parent != null)
            {
                parameters.StatusBar.Invoke((MethodInvoker)delegate
                {
                    parameters.StatusBar.Parent.Enabled = true;
                });
            }
        }

        /// <summary>
        /// Internal method to copy data between Excel sheets.  Handles file operations and calls the actual copy logic.
        /// </summary>
        private static string CopyDataBetweenExcelSheetsInternal(
            int reportType,
            string sourceFilePath,
            string sourceSheetName,
            string fileSaveLocation,
            string destinationFilePath,
            string destinationSheetName,
            int startRow,
            int startCol,
            BackgroundWorker worker,
            StatusStrip statusBar,
            string selectedFinYear)
        {
            if (string.IsNullOrEmpty(sourceFilePath))
            {
                throw new ArgumentException($"'{nameof(sourceFilePath)}' cannot be null or empty.", nameof(sourceFilePath));
            }

            if (string.IsNullOrEmpty(sourceSheetName))
            {
                throw new ArgumentException($"'{nameof(sourceSheetName)}' cannot be null or empty.", nameof(sourceSheetName));
            }

            if (string.IsNullOrEmpty(fileSaveLocation))
            {
                throw new ArgumentException($"'{nameof(fileSaveLocation)}' cannot be null or empty.", nameof(fileSaveLocation));
            }

            if (string.IsNullOrEmpty(destinationFilePath))
            {
                throw new ArgumentException($"'{nameof(destinationFilePath)}' cannot be null or empty.", nameof(destinationFilePath));
            }

            if (string.IsNullOrEmpty(destinationSheetName))
            {
                throw new ArgumentException($"'{nameof(destinationSheetName)}' cannot be null or empty.", nameof(destinationSheetName));
            }

            if (worker is null)
            {
                throw new ArgumentNullException(nameof(worker));
            }

            if (statusBar is null)
            {
                throw new ArgumentNullException(nameof(statusBar));
            }

            if (string.IsNullOrEmpty(selectedFinYear))
            {
                throw new ArgumentException($"'{nameof(selectedFinYear)}' cannot be null or empty.", nameof(selectedFinYear));
            }

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

                // Check if the destination sheet exists, create it if it doesn't.
                var destinationWorksheet = GetOrCreateDestinationWorksheet(destinationPackage, destinationSheetName, sourcePackage, sourceSheetName);

                int sourceRowCount = sourceWorksheet.Dimension?.Rows ?? 0;
                int sourceColCount = sourceWorksheet.Dimension?.Columns ?? 0;
                int destRow = 2;
                int destColCount = destinationWorksheet.Dimension?.Columns ?? 0;

                Logger.LogInfo($"Source Sheet: {sourceSheetName}, Rows: {sourceRowCount}, Cols: {sourceColCount}");
                Logger.LogInfo($"Destination Sheet: {destinationSheetName}, Rows: {destRow}, Cols: {destColCount}");

                if (sourceRowCount > 0 && sourceColCount > 0)
                {
                    EnsureDestinationSheetHasEnoughColumns(destinationWorksheet, sourceColCount, ref destColCount);
                    statusBar?.Invoke((MethodInvoker)delegate { statusBar.Items[0].Text = "Copying Data..."; });
                    CopyDataBetweenExcelSheetsData(sourceWorksheet, destinationWorksheet, startRow, sourceRowCount, sourceColCount, ref destRow, destColCount, worker, statusBar); // Pass statusBar
                }
                else
                {
                    Logger.LogInfo("Source sheet is empty.");
                }
                string createdFolderPath = CreateFolders(reportType, fileSaveLocation);
                string fileSaveLocation2 = Path.Combine(createdFolderPath, "temp.xlsx");
                destinationPackage.SaveAs(new FileInfo(fileSaveLocation2));
                result = fileSaveLocation2;

                ProcessPostCopyOperations(destinationPackage, fileSaveLocation2, reportType, statusBar, worker, selectedFinYear); // Pass selectedFinYear

                return result;
            }
        }

        /// <summary>
        /// Gets or creates the destination worksheet.
        /// </summary>
        private static ExcelWorksheet GetOrCreateDestinationWorksheet(ExcelPackage package, string sheetName, ExcelPackage sourcePackage, string sourceSheetName)
        {
            ExcelWorksheet worksheet = package.Workbook.Worksheets[sheetName];
            if (worksheet == null)
            {
                worksheet = package.Workbook.Worksheets.Add(sheetName);
                // Copy the first row (headers) from the source sheet
                ExcelWorksheet sourceWorksheet = sourcePackage.Workbook.Worksheets[sourceSheetName];
                if (sourceWorksheet != null && sourceWorksheet.Dimension != null && sourceWorksheet.Dimension.Rows > 0)
                {
                    ExcelRange headerRow = sourceWorksheet.Cells[1, 1, 1, sourceWorksheet.Dimension.Columns];
                    worksheet.Cells[1, 1].LoadFromArrays(new object[][] { (object[])headerRow.Value });
                }
                else
                {
                    // If the source worksheet is null or empty, add default headers.
                    worksheet.Cells[1, 1].Value = "Column1";
                }
                Logger.LogInfo($"Created new sheet: {sheetName} and copied headers.");
            }
            return worksheet;
        }

        /// <summary>
        /// Ensures the destination sheet has enough columns.
        /// </summary>
        private static void EnsureDestinationSheetHasEnoughColumns(ExcelWorksheet destinationWorksheet, int sourceColCount, ref int destColCount)
        {
            if (destColCount < sourceColCount)
            {
                for (int i = destColCount + 1; i <= sourceColCount; i++)
                {
                    destinationWorksheet.InsertColumn(i, 1);
                }
                destColCount = sourceColCount;
            }
        }

        /// <summary>
        /// Copies data from one Excel sheet to another using Range.Copy.
        /// </summary>
        private static void CopyDataBetweenExcelSheetsData(ExcelWorksheet sourceWorksheet, ExcelWorksheet destinationWorksheet, int startRow, int sourceRowCount, int sourceColCount, ref int destRow, int destColCount, BackgroundWorker worker, StatusStrip statusBar)
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

                EnsureDestinationSheetHasEnoughRows(destinationWorksheet, destRow, rowsToCopy);

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
        /// Ensures that the destination worksheet has enough rows.
        /// </summary>
        private static void EnsureDestinationSheetHasEnoughRows(ExcelWorksheet destinationWorksheet, int destRow, int rowsToCopy)
        {
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

            int rowCount = dataSheet.Dimension?.Rows ?? 0;
            HashSet<string> uniqueCustomers = GetUniqueCustomers(dataSheet, rowCount, worker, statusBar);

            PopulateAnalysisSheetWithCustomers(package, analysisSheet, uniqueCustomers, worker);

            Logger.LogInfo($"Unique customers extracted and copied to '{analysisSheetName}'.");
            statusBar?.Invoke((MethodInvoker)delegate { statusBar.Items[0].Text = "Unique customers extracted."; });
        }

        /// <summary>
        /// Extracts unique customer names from the data sheet.
        /// </summary>
        private static HashSet<string> GetUniqueCustomers(ExcelWorksheet dataSheet, int rowCount, BackgroundWorker worker = null, StatusStrip statusBar = null)
        {
            HashSet<string> uniqueCustomers = new HashSet<string>();
            int processedRows = 0;

            for (int row = 3; row <= rowCount; row++)
            {
                if (worker != null && worker.CancellationPending)
                {
                    return uniqueCustomers;
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
            return uniqueCustomers;
        }

        /// <summary>
        /// Populates the analysis sheet with the unique customers.
        /// </summary>
        private static void PopulateAnalysisSheetWithCustomers(ExcelPackage package, ExcelWorksheet analysisSheet, HashSet<string> uniqueCustomers, BackgroundWorker worker = null)
        {
            int analysisRow = 6; //starts from row from row 6 as the 1st 5 rows are the headers
            string calTime = DateTime.Today.ToString("dd/MM/yyyy"); //for filling out the date column

            foreach (string customer in uniqueCustomers)
            {
                if (worker != null && worker.CancellationPending)
                {
                    return;
                }
#if DEBUG
                Logger.LogDebug($"{customer}"); //outputs list of the customers in debug env to make sure it's working
#endif
                analysisSheet.Cells[analysisRow, CustomerColumn + 1].Value = customer; // +1 for 1-based indexing
                analysisSheet.Cells[analysisRow, DateColumn + 1].Value = calTime;              // +1 for 1-based indexing
                analysisSheet.Cells[analysisRow, FinancialYearColumn + 1].Value = GetCurrentFinancialYear(); // +1
                analysisRow++;
            }
            package.SaveAsync();
        }

        /// <summary>
        /// Calculates and returns the current financial year.
        /// </summary>
        /// <param name="returnFullYear">If true, returns "YYYY-YY" format; otherwise, returns "FY YY/YY" format.</param>
        public static string GetCurrentFinancialYear(bool returnFullYear = false)
        {
            DateTime today = DateTime.Today;
            int year = today.Year;
            int startYear;
            int endYear;

            // Determine the financial year.  Using May as the start month (Harlow's FinYear).
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

            string fyFormat = $"FY {startYear.ToString().Substring(2)}/{endYear.ToString().Substring(2)}";
            string fyFullYearFormat = $"{startYear}_{endYear.ToString().Substring(2)}";

            return returnFullYear ? fyFullYearFormat : fyFormat;
        }

        /// <summary>
        /// Calculates the Analysis sheet.  Added to ensure calculations are performed.
        /// </summary>
        private static void CalculateAnalysisSheet(ExcelPackage package, string sheetName, StatusStrip statusBar = null)
        {
            statusBar?.Invoke((MethodInvoker)delegate { statusBar.Items[0].Text = "Calculating... this will take a few minutes.";});

            ExcelWorksheet analysisSheet = package.Workbook.Worksheets[sheetName];
            if (analysisSheet != null)
            {
                analysisSheet.Calculate();
                Logger.LogInfo($"Analysis sheet '{sheetName}' calculations performed.");
                statusBar?.Invoke((MethodInvoker)delegate { statusBar.Items[0].Text = "Calculations complete."; });
            }
            else
            {
                Logger.LogError($"Analysis sheet '{sheetName}' not found.");
            }
        }

        /// <summary>
        /// Copies data from the Analysis sheet to the weekly report, appending it to the next free row.
        /// </summary>
        private static void CopyAnalysisDataToWeeklyReport(string sourceFilePath, StatusStrip statusBar = null, BackgroundWorker worker = null, string selectedFinYear = null)
        {
            ExcelPackage.License.SetNonCommercialPersonal("Harlow");
            string username = Environment.UserName;
#if DEBUG
            //File for debugging
            string destinationFilePath = $@"C:\Users\{username}\Harlow Printing\IT - Documents\PowerBI\Quote Conversion Report\Quotes conversion data_wrangled\weekly report quotes conversion merged - copy.xlsx";
#else
            //Release File
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

                //force calculation to ensure correct data is copied
                sourceWorksheet.Calculate();
                sourcePackage.Workbook.Calculate();

                // Get the financial year string
                string financialYear = selectedFinYear ?? GetCurrentFinancialYear(true); // Use selectedFinYear if provided

                // Check if the destination sheet exists, create it if it doesn't.
                ExcelWorksheet destinationWorksheet = destinationPackage.Workbook.Worksheets[financialYear];
                if (destinationWorksheet == null)
                {
                    // The sheet does not exist, so create it and copy headers.
                    destinationWorksheet = destinationPackage.Workbook.Worksheets.Add(financialYear);
                    ExcelWorksheet anySheet = destinationPackage.Workbook.Worksheets.FirstOrDefault();
                    if (anySheet != null)
                    {
                        CopyHeaders(anySheet, destinationWorksheet);
                        Logger.LogInfo($"Created sheet '{financialYear}' and copied headers from '{anySheet.Name}'");
                    }
                    else
                    {
                        destinationWorksheet.Cells[1, 1].Value = "Column1"; //add a default column
                        Logger.LogWarning("No existing sheet to copy headers from. Created default headers.");
                    }
                }

                int nextFreeRow = GetNextFreeRow(destinationWorksheet); //finds the next free row, to append data to file.

                string sourceFileName = Path.GetFileName(sourceFilePath);
                int sourceRowCount = sourceWorksheet.Dimension?.Rows ?? 0;
                int sourceColCount = sourceWorksheet.Dimension?.Columns ?? 0;

                if (sourceRowCount > 0 && sourceColCount > 0)
                {
                    CopyAnalysisData(sourceWorksheet, destinationWorksheet, nextFreeRow, sourceColCount, sourceFileName, worker, statusBar);
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

        private static string GetPreviousFinancialYear(string currentFinancialYear)
        {
            //split the string
            string[] years = currentFinancialYear.Split('_');
            if (years.Length == 2 && int.TryParse(years[0], out int startYear))
            {
                return $"{startYear - 1}_{years[0].Substring(2)}";
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Copy's headers from source sheet to destination sheet
        /// </summary>
        /// <param name="sourceSheet">Sheet to copy headers from</param>
        /// <param name="destinationSheet">Sheet to copy headers to</param>
        private static void CopyHeaders(ExcelWorksheet sourceSheet, ExcelWorksheet destinationSheet)
        {
            if (sourceSheet != null && sourceSheet.Dimension != null && sourceSheet.Dimension.Rows > 0)
            {
                ExcelRange headerRow = sourceSheet.Cells[1, 1, 1, sourceSheet.Dimension.Columns];
                headerRow.Copy(destinationSheet.Cells[1, 1]);
            }
            else
            {
                destinationSheet.Cells[1, 1].Value = "Column1";
            }
        }

        /// <summary>
        /// Gets the next free row in the worksheet.
        /// </summary>
        private static int GetNextFreeRow(ExcelWorksheet worksheet)
        {
            int nextFreeRow = worksheet.Dimension?.Rows ?? 0;
            while (worksheet.Cells[nextFreeRow + 1, 1].Value != null)
            {
                nextFreeRow++;
            }
            return nextFreeRow + 1;
        }

        /// <summary>
        /// Copies the analysis data to the destination worksheet.
        /// </summary>
        private static void CopyAnalysisData(ExcelWorksheet sourceWorksheet, ExcelWorksheet destinationWorksheet, int nextFreeRow, int sourceColCount, string sourceFileName, BackgroundWorker worker = null, StatusStrip statusBar = null)
        {
            int sourceRowCount = sourceWorksheet.Dimension?.Rows ?? 0;
            int processedRows = 0;
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
                        Logger.LogDebug($"Copying from [{sourceRow},{col}]: Value = '{cellValue}'");
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

        /// <summary>
        /// Deletes rows from a worksheet where the specified column is empty.
        /// </summary>
        private static void DeleteEmptyRows(string filePath, string sheetName, StatusStrip statusBar = null, BackgroundWorker worker = null, int reportType = 0)
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

                List<object> columnAValues = GetColumnAValues(worksheet, rowCount, worker);

                int deletedRows = DeleteRowsWithEmptyColumnA(worksheet, rowCount, columnAValues, worker, statusBar);

                package.Workbook.Calculate();
                package.SaveAsync();
                package.Dispose();
                Logger.LogInfo($"Empty rows deleted from '{filePath}', sheet '{sheetName}'.");
                statusBar?.Invoke((MethodInvoker)delegate { statusBar.Items[0].Text = "Empty rows deleted."; });
                RefreshPivotTablesIfNeeded(reportType, filePath, statusBar, worker);
            }
        }

        /// <summary>
        /// Gets the values from column A of the worksheet.
        /// </summary>
        private static List<object> GetColumnAValues(ExcelWorksheet worksheet, int rowCount, BackgroundWorker worker = null)
        {
            List<object> columnAValues = new List<object>();
            for (int row = 7; row <= rowCount; row++)
            {
                if (worker != null && worker.CancellationPending)
                {
                    return columnAValues;
                }
                columnAValues.Add(worksheet.Cells[row, CustomerColumn + 1].Value); // +1 indexing
            }
            return columnAValues;
        }

        /// <summary>
        /// Deletes rows from the worksheet where column A is empty.
        /// </summary>
        private static int DeleteRowsWithEmptyColumnA(ExcelWorksheet worksheet, int rowCount, List<object> columnAValues, BackgroundWorker worker = null, StatusStrip statusBar = null)
        {
            int deletedRows = 0;
            for (int row = rowCount; row >= 7; row--)
            {
                if (worker != null && worker.CancellationPending)
                {
                    return deletedRows;
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
            return deletedRows;
        }

        /// <summary>
        /// Refreshes pivot tables if the report is a monthly report as only monthly has pivots for now.
        /// </summary>
        private static void RefreshPivotTablesIfNeeded(int reportType, string filePath, StatusStrip statusBar = null, BackgroundWorker worker = null)
        {
            if (reportType == 1 || reportType == 2 || reportType == 3) // Only non weekly reports
            {
                RefreshPivotTables(filePath, MonthlyOrderPivotSheetName, MonthlyOrderPivotName, statusBar, worker);
                RefreshPivotTables(filePath, MonthlyEstimatePivotSheetName, MonthlyEstimatePivotName, statusBar, worker);
                Logger.LogInfo("Pivot tables refreshed after deleting empty rows (reportType = 1).");
                statusBar?.Invoke((MethodInvoker)delegate { statusBar.Items[0].Text = "Pivot tables refreshed."; });
            }
            else
            {
                Logger.LogInfo($"Pivot tables not refreshed (reportType = {reportType}).");
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

                        // only if the pivot table was refreshed
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
        /// Handles post-processing after the Excel file is saved, including sending the email.
        /// </summary>
        /// <param name="filePath">The path to the saved Excel file.</param>
        /// <param name="reportType">Indicates the report type: 0 = Weekly, 1 = Monthly, 2 = Quarterly, 3 = Annual.</param>
        /// <param name="sendEmailAction">The action to send the email.</param>
        ///  <param name="financialYearText">The financial year text to add to the email.</param>
        /// <param name="tempFilePath">The path to the saved Excel temp file.</param>
        private static string CopyDataBetweenExcelSheetsInternal_PostProcess(string filePath, int reportType, Action<string> sendEmailAction, string tempFilePath, string financialYearText = "")
        {
            if (string.IsNullOrEmpty(filePath))
            {
                throw new ArgumentException($"'{nameof(filePath)}' cannot be null or empty.", nameof(filePath));
            }

            if (sendEmailAction is null)
            {
                throw new ArgumentNullException(nameof(sendEmailAction));
            }

            if (string.IsNullOrEmpty(tempFilePath))
            {
                throw new ArgumentException($"'{nameof(tempFilePath)}' cannot be null or empty.", nameof(tempFilePath));
            }

            string emailResult = null;
            Process process = null;
            bool excelWasOpen = false;
            bool sendEmailConfirmed = false;
            string finalFilePath = filePath;

            try
            {
                if (reportType == 1 || reportType == 2 || reportType == 3) // Only for non weekly reports
                {
                    excelWasOpen = CheckForExcelProcesses();
                    if (excelWasOpen)
                    {
                        DialogResult result = MessageBox.Show("Other Excel processes are running.  All Excel files must be closed before continuing.  Do you want to close them?", "Close Excel Files?", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);
                        if (result == DialogResult.Yes)
                        {
                            CloseExcelProcesses();
                        }
                        else
                        {
                            emailResult = "Email not sent.  Please close all Excel files and try again.";
                            return emailResult; // Exit, skipping email send
                        }
                    }
                    DisplayExcelOpenMessage();
                    process = OpenExcelFile(filePath);
                    WaitForExcelToClose(process);
                }
            }
            catch (Exception ex)
            {
                emailResult = HandleExcelOpeningError(ex);
            }
            finally
            {
                DisposeExcelProcess(process);
                if (!excelWasOpen)
                {
                    sendEmailConfirmed = true;
                }
                else //weekly, quarterly, annual, monthly
                {
                    sendEmailConfirmed = MessageBox.Show("Do you want to send the email with the report?", "Send Email", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes;
                }

                if (sendEmailConfirmed)
                {
                    SendEmail(sendEmailAction, finalFilePath ); // Pass the file path and financialYearText
                    emailResult = "Email sent.";
                }
                else
                {
                    emailResult = "Email not sent.";
                }

                // Delete the temporary file
                try
                {

                }
                catch (Exception ex)
                {
                    Logger.LogError($"Error deleting temporary file: {ex.Message}");
                }
            }
            return emailResult;
        }

        /// <summary>
        /// Checks if any Excel processes are currently running.
        /// </summary>
        /// <returns>True if any Excel processes are running, false otherwise.</returns>
        private static bool CheckForExcelProcesses()
        {
            Process[] processes = Process.GetProcessesByName("Excel");
            return processes.Length > 0;
        }

        /// <summary>
        /// Closes all running Excel processes.
        /// </summary>
        private static void CloseExcelProcesses()
        {
            Process[] processes = Process.GetProcessesByName("Excel");
            foreach (Process process in processes)
            {
                try
                {
                    process.Kill();
                    process.WaitForExit(); // Ensure the process is fully closed.
                }
                catch (Exception ex)
                {
                    Logger.LogError($"Error closing Excel process (ID {process.Id}): {ex.Message}");
                    // Consider showing a message to the user, or adding to a list of failed processes.
                }
            }
        }

        /// <summary>
        /// Displays a message box to the user, prompting them to refresh the pivot tables.
        /// </summary>
        private static void DisplayExcelOpenMessage()
        {
            MessageBox.Show("Opening Excel File. Please manually refresh PivotTables and Slicers and close excel to allow program to continue.", "Opening Excel File...", MessageBoxButtons.OK, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1, MessageBoxOptions.DefaultDesktopOnly);
        }

        /// <summary>
        /// Opens the Excel file using Process.Start.
        /// </summary>
        private static Process OpenExcelFile(string filePath)
        {
            return Process.Start(filePath);
        }

        /// <summary>
        /// Waits for the Excel process to exit.
        /// </summary>
        private static void WaitForExcelToClose(Process process)
        {
            process?.WaitForExit(); // if not null wait for exit, else return.
        }

        /// <summary>
        /// Handles errors that occur while opening the Excel file.
        /// </summary>
        private static string HandleExcelOpeningError(Exception ex)
        {
            Logger.LogError($"Error opening file: {ex.Message}");
            MessageBox.Show($"Error opening file: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.DefaultDesktopOnly);
            return $"Error opening file: {ex.Message}";
        }

        /// <summary>
        /// Ensures that the Excel process is disposed.
        /// </summary>
        private static void DisposeExcelProcess(Process process)
        {
            process?.Dispose(); // if not null then dispose, else return
        }

        /// <summary>
        /// Sends the email.
        /// </summary>
        private static void SendEmail(Action<string> sendEmailAction, string filePath)
        {
            sendEmailAction?.Invoke(filePath); //if not null invoke method with the file path
        }

        /// <summary>
        /// Creates the folder structure if necessary.
        /// </summary>
        private static string CreateFolders(int reportType, string fileSaveLocation)
        {
            FolderCreation createFolderStruc = new FolderCreation();
            return createFolderStruc.CreateFolder(reportType, fileSaveLocation);
        }

        /// <summary>
        /// Generates the file name and saves the Excel file.
        /// </summary>
        private static string GenerateFileNameAndSave(ExcelPackage package, int reportType, string createdFolderPath)
        {
            string currentDate;
            string fileName;

            switch (reportType)
            {
                case 0:  // Weekly
                    currentDate = DateTime.Now.ToString("yyyyMMdd");
                    fileName = $"{currentDate} Estimate Success Rate.xlsx";
                    break;
                case 1: // Monthly
                    DateTime now = DateTime.Now;
                    DateTime targetMonth = now.Day <= 15 ? now.AddMonths(-1) : now;
                    currentDate = targetMonth.ToString("MMM_yyyy");
                    fileName = $"Estimate Success Rate {currentDate}.xlsx";
                    break;
                case 2: // Quarterly
                    DateTime today = DateTime.Today;
                    int currentQuarter = (today.Month - 1) / 3 + 1;
                    int previousQuarter = currentQuarter - 1;
                    int previousQuarterYear = today.Year;

                    if (previousQuarter < 1)
                    {
                        previousQuarter = 4; // Wrap around to the 4th quarter of the previous year
                        previousQuarterYear--;
                    }

                    DateTime quarterStart = new DateTime(previousQuarterYear, (previousQuarter - 1) * 3 + 1, 1);
                    DateTime quarterEnd = quarterStart.AddMonths(3).AddDays(-1);
                    currentDate = $"{quarterStart:MMM} to {quarterEnd:MMM} {today.Year}";
                    fileName = $"Estimate Success Rate {currentDate}.xlsx";
                    break;
                case 3: // Annual
                    currentDate = (DateTime.Now.Year - 1).ToString();  // Changed to last year
                    fileName = $"Estimate Success Rate {currentDate}.xlsx";
                    break;
                default:
                    currentDate = DateTime.Now.ToString("yyyyMMdd");
                    fileName = $"{currentDate}_Estimate_Success_Rate.xlsx";
                    break;
            }

            string fileSaveLocation2 = Path.Combine(createdFolderPath, fileName);
            FileInfo newFile = new FileInfo(fileSaveLocation2);
            package.SaveAs(newFile);
            return fileSaveLocation2;
        }

        /// <summary>
        /// Generates the file name for finding if the file exists.
        /// </summary>
        private static string GenerateFileName(int reportType, string createdFolderPath)
        {
            string currentDate;
            string fileName;

            switch (reportType)
            {
                case 0:  // Weekly
                    currentDate = DateTime.Now.ToString("yyyyMMdd");
                    fileName = $"{currentDate} Estimate Success Rate.xlsx";
                    break;
                case 1: // Monthly
                    DateTime now = DateTime.Now;
                    DateTime targetMonth = now.Day <= 15 ? now.AddMonths(-1) : now;
                    currentDate = targetMonth.ToString("MMM_yyyy");
                    fileName = $"Estimate Success Rate {currentDate}.xlsx";
                    break;
                case 2: // Quarterly
                    DateTime today = DateTime.Today;
                    int currentQuarter = (today.Month - 1) / 3 + 1;
                    int previousQuarter = currentQuarter - 1;
                    int previousQuarterYear = today.Year;

                    if (previousQuarter < 1)
                    {
                        previousQuarter = 4; // Wrap around to the 4th quarter of the previous year
                        previousQuarterYear--;
                    }

                    DateTime quarterStart = new DateTime(previousQuarterYear, (previousQuarter - 1) * 3 + 1, 1);
                    DateTime quarterEnd = quarterStart.AddMonths(3).AddDays(-1);

                    string startMonth = quarterStart.ToString("MMM");
                    string endMonth = quarterEnd.ToString("MMM");
                    currentDate = $"{startMonth} to {endMonth} {previousQuarterYear}";
                    fileName = $"Estimate Success Rate {currentDate}.xlsx";
                    break;
                case 3: // Annual
                    currentDate = (DateTime.Now.Year - 1).ToString();  // Changed to last year
                    fileName = $"Estimate Success Rate {currentDate}.xlsx";
                    break;
                default:
                    currentDate = DateTime.Now.ToString("yyyyMMdd");
                    fileName = $"{currentDate}_Estimate_Success_Rate.xlsx";
                    break;
            }

            string fileSaveLocation2 = Path.Combine(createdFolderPath, fileName);
            FileInfo newFile = new FileInfo(fileSaveLocation2);
            return fileSaveLocation2;
        }

        /// <summary>
        /// Performs post-copy operations such as extracting customers, calculating analysis, deleting rows, and copying to weekly report.
        /// </summary>
        private static void ProcessPostCopyOperations(ExcelPackage package, string fileSaveLocation, int reportType, StatusStrip statusBar, BackgroundWorker worker, string selectedFinYear) // Added selectedFinYear
        {
            ExtractUniqueCustomers(package, DataSheetName, AnalysisSheetName, statusBar, worker);
            CalculateAnalysisSheet(package, AnalysisSheetName, statusBar);
            DeleteEmptyRows(fileSaveLocation, AnalysisSheetName, statusBar, worker, reportType);
            if (reportType == 0) // not weekly
            {
                CopyAnalysisDataToWeeklyReport(fileSaveLocation, statusBar, worker, selectedFinYear); // Pass selectedFinYear
            }
            package.Dispose();
        }

        #endregion Private Methods
    }
}
