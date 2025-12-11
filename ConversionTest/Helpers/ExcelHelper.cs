#region Using Directives
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Linq;
#endregion

namespace QuoteConversionReportAutomation.Helpers
{
    #region Static Class Definition
    /// <summary>
    /// Provides common, stateless static helper methods for interacting with Excel worksheets using the EPPlus library.
    /// This centralises utility functions to avoid code duplication across different services.
    /// </summary>
    public static class ExcelHelper
    {
        #region Static Methods
        /// <summary>
        /// Scans the header row of a worksheet to build a dictionary that maps column names to their 1-based index.
        /// This makes dependent code resilient to changes in column order within Excel templates.
        /// </summary>
        /// <param name="worksheet">The worksheet to scan for headers.</param>
        /// <param name="headerRow">The row number (1-based) where the column headers are located.</param>
        /// <param name="requiredColumns">An enumerable of column header names that are expected to be present.</param>
        /// <returns>A dictionary mapping each column name (case-insensitively) to its integer index.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown if the specified header row does not exist, or if one of the required columns is not found in the header row.
        /// </exception>
        public static Dictionary<string, int> MapColumnIndices(ExcelWorksheet worksheet, int headerRow, IEnumerable<string> requiredColumns)
        {
            // Initialise a case-insensitive dictionary to store the mapping from column name to column index.
            var columnMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            // Validate that the worksheet has dimensions and that the specified header row exists within those dimensions.
            if (worksheet.Dimension == null || worksheet.Dimension.End.Row < headerRow)
            {
                throw new InvalidOperationException($"Header row {headerRow} does not exist in worksheet '{worksheet.Name}'. The sheet may be empty or malformed.");
            }

            // Iterate through each column in the specified header row to build the map.
            for (int col = 1; col <= worksheet.Dimension.End.Column; col++)
            {
                // Read the value from the cell and trim any whitespace.
                var cellValue = worksheet.Cells[headerRow, col].Value?.ToString()?.Trim();

                // If the header text is not empty and has not already been added to the map, add it.
                // This prevents errors from duplicate column names and only maps the first occurrence.
                if (!string.IsNullOrEmpty(cellValue) && !columnMap.ContainsKey(cellValue))
                {
                    columnMap[cellValue] = col;
                }
            }

            // After mapping all available headers, verify that all required columns were found.
            foreach (var requiredColumn in requiredColumns)
            {
                if (!columnMap.ContainsKey(requiredColumn))
                {
                    // If a required column is missing, throw a descriptive exception to the caller.
                    throw new InvalidOperationException($"Required column '{requiredColumn}' not found in the header of worksheet '{worksheet.Name}'. Please check the template file.");
                }
            }

            // Return the completed map of column names to their indices.
            return columnMap;
        }

        /// <summary>
        /// Finds the next available (empty) row in a worksheet, typically for appending new data.
        /// </summary>
        /// <param name="worksheet">The worksheet to check.</param>
        /// <param name="checkColumn">The 1-based index of the column to check for content to determine if a row is used.</param>
        /// <returns>The 1-based index of the next free row.</returns>
        public static int GetNextFreeRow(ExcelWorksheet worksheet, int checkColumn = 1)
        {
            // If the worksheet has no data, the first row is row 1.
            if (worksheet.Dimension == null)
            {
                return 1;
            }

            // The first row for data entry is typically row 2, assuming row 1 is for headers.
            const int firstDataRowAfterHeaders = 2;
            int lastUsedRow = worksheet.Dimension.End.Row;

            // If the sheet only contains headers (or less), the next free row is the first data row.
            if (lastUsedRow < firstDataRowAfterHeaders)
            {
                return firstDataRowAfterHeaders;
            }

            // Iterate backwards from the last used row to find the first row with content in the check column.
            for (int r = lastUsedRow; r >= 1; r--)
            {
                var cell = worksheet.Cells[r, checkColumn].Value;
                // If the cell is not null and its content is not just whitespace, we've found the last data row.
                if (cell != null && !string.IsNullOrWhiteSpace(cell.ToString()))
                {
                    // The next free row is one row after the last row with data.
                    // Ensure it's at least the designated first data row.
                    return Math.Max(r + 1, firstDataRowAfterHeaders);
                }
            }

            // If the loop completes, it means all rows are empty, so the next free row is the first data row.
            return firstDataRowAfterHeaders;
        }
        #endregion
    }
    #endregion
}