#region Using Directives

// System-related namespaces for core functionalities.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

// Third-party namespaces for external libraries.
using OfficeOpenXml;

// Project-specific namespaces for application components.
using QuoteConversionReportAutomation.Services.Interfaces;

#endregion

namespace QuoteConversionReportAutomation.Services.Excel
{
    #region Class Definition
    /// <summary>
    /// Implements the <see cref="IExcelFilteringService"/> to provide concrete methods
    /// for filtering data within an Excel worksheet using the EPPlus library.
    /// This service encapsulates the logic for removing or retaining rows based on specific criteria.
    /// </summary>
    public class ExcelFilteringService : IExcelFilteringService
    {
        #region IExcelFilteringService Implementation

        /// <inheritdoc/>
        public async Task<ExcelWorksheet> FilterDataSheetByValueAsync(ExcelWorksheet worksheet, int priceColumnIndex, decimal threshold, CancellationToken cancellationToken)
        {
            // Exit early if the worksheet has no data to filter.
            if (worksheet.Dimension == null || worksheet.Dimension.Rows < 2)
            {
                return worksheet;
            }

            // Perform the filtering on a background thread to keep the UI responsive.
            await Task.Run(() =>
            {
                // Get the total number of rows to iterate through.
                int initialRowCount = worksheet.Dimension.Rows;

                // Iterate backwards from the last row to the first data row (row 2).
                // Iterating backwards is essential when deleting rows to avoid skipping items
                // as the row indices shift after a deletion.
                for (int r = initialRowCount; r >= 2; r--)
                {
                    // Check for a cancellation request from the user on each iteration.
                    cancellationToken.ThrowIfCancellationRequested();

                    // Get the raw value from the cell in the specified price column.
                    var cellValue = worksheet.Cells[r, priceColumnIndex].Value;
                    bool shouldDeleteRow = true; // Assume the row should be deleted unless proven otherwise.

                    // Check if the cell has a value.
                    if (cellValue != null)
                    {
                        // Clean the string value by removing currency symbols and commas.
                        string valStr = cellValue.ToString()!.Replace("£", "").Replace(",", "").Trim();

                        // Attempt to parse the cleaned string as a decimal.
                        if (decimal.TryParse(valStr, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal amount))
                        {
                            // If the parsed amount meets or exceeds the threshold, we keep the row.
                            if (amount >= threshold)
                            {
                                shouldDeleteRow = false;
                            }
                        }
                    }

                    // If the row is still marked for deletion, remove it from the worksheet.
                    if (shouldDeleteRow)
                    {
                        worksheet.DeleteRow(r, 1);
                    }
                }
            }, cancellationToken);
            return worksheet;
        }

        /// <inheritdoc/>
        public async Task<ExcelWorksheet> FilterDataSheetByPostingCodeAsync(ExcelWorksheet worksheet, int postingCodeColumnIndex, HashSet<string> validPostingCodes, CancellationToken cancellationToken)
        {
            // Exit early if there is no data to filter.
            if (worksheet.Dimension == null || worksheet.Dimension.Rows < 2)
            {
                return worksheet;
            }

            // Perform the operation on a background thread.
            var newWorksheet = await Task.Run(() =>
            {
                // copy the rows we want to keep to a new temporary sheet, then replace the original.
                var workbook = worksheet.Workbook;
                string originalSheetName = worksheet.Name;
                string tempSheetName = $"_temp_filter_{Guid.NewGuid()}";
                var tempWorksheet = workbook.Worksheets.Add(tempSheetName);

                // Copy the header row (row 1) from the original sheet to the temporary sheet.
                worksheet.Cells[1, 1, 1, worksheet.Dimension.End.Column].Copy(tempWorksheet.Cells[1, 1]);

                // This counter will track the next available row in our new temporary sheet.
                int destinationRowIndex = 2;

                // Iterate through all data rows in the original sheet.
                for (int r = 2; r <= worksheet.Dimension.End.Row; r++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    // Get the posting code from the current row.
                    var cellValue = worksheet.Cells[r, postingCodeColumnIndex].Value?.ToString()?.Trim();

                    // Check if the posting code exists in our set of valid codes (case-insensitive).
                    if (!string.IsNullOrEmpty(cellValue) && validPostingCodes.Contains(cellValue))
                    {
                        // If it's a valid code, copy the entire row to the temporary sheet.
                        worksheet.Cells[r, 1, r, worksheet.Dimension.End.Column].Copy(tempWorksheet.Cells[destinationRowIndex, 1]);
                        // Increment the destination row counter for the next valid row.
                        destinationRowIndex++;
                    }
                }

                // After iterating, replace the original, unfiltered sheet with our new, filtered one.
                int originalIndex = worksheet.Index;
                workbook.Worksheets.Delete(worksheet);
                tempWorksheet.Name = originalSheetName;

                // Move the new sheet back to the original's position in the workbook.
                if (originalIndex <= workbook.Worksheets.Count)
                {
                    var sheetToMoveBefore = workbook.Worksheets[originalIndex];
                    workbook.Worksheets.MoveBefore(tempWorksheet.Name, sheetToMoveBefore.Name);
                }

                // Return the reference to the new, valid worksheet.
                return tempWorksheet;

            }, cancellationToken);

            // Return the new worksheet reference to the calling code.
            return newWorksheet;
        }

        #endregion
    }
    #endregion
}