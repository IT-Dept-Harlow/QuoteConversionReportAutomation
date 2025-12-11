#region Using Directives

// System-related namespaces for core functionalities.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

// Third-party namespaces for external libraries.
using Microsoft.Extensions.Configuration;
using OfficeOpenXml;

// Project-specific namespaces for application components.
using QuoteConversionReportAutomation.Configuration;
using QuoteConversionReportAutomation.Helpers;
using QuoteConversionReportAutomation.Interfaces;
using QuoteConversionReportAutomation.Models.Status;
using QuoteConversionReportAutomation.Services.Interfaces;
using QuoteConversionReportAutomation.Services.Logging;

#endregion

namespace QuoteConversionReportAutomation.Services.Excel
{
    #region Class Definition
    /// <summary>
    /// Implements the <see cref="IExcelDataExclusionService"/> to provide concrete methods
    /// for filtering and removing specific data, such as tender accounts, from an Excel worksheet.
    /// </summary>
    public class ExcelDataExclusionService : IExcelDataExclusionService
    {
        #region Fields

        /// <summary>
        /// Provides read-only access to the application's configuration settings.
        /// </summary>
        private readonly IConfiguration _configuration;

        /// <summary>
        /// The centralised service for broadcasting application-wide status messages.
        /// </summary>
        private readonly IStatusManagerService _statusManager;

        #endregion

        #region Constructor

        /// <summary>
        /// Initialises a new instance of the <see cref="ExcelDataExclusionService"/> class.
        /// </summary>
        /// <param name="configuration">The application's configuration, used to retrieve the list of exclusion codes.</param>
        /// <param name="statusManager">The service for reporting progress to the UI.</param>
        public ExcelDataExclusionService(IConfiguration configuration, IStatusManagerService statusManager)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _statusManager = statusManager ?? throw new ArgumentNullException(nameof(statusManager));
            Logger.LogTrace("ExcelDataExclusionService instance created.");
        }

        #endregion

        #region IExcelDataExclusionService Implementation

        /// <inheritdoc/>
        public async Task<ExcelWorksheet> ExcludeTenderAccountsAsync(ExcelWorksheet worksheet, CancellationToken cancellationToken)
        {
            // Report the start of the exclusion process.
            _statusManager.Post("Excluding tender accounts from analysis...", MessageType.InProgress);

            // Retrieve the list of tender account posting codes from appsettings.json.
            var tenderCodesToExclude = _configuration.GetSection(AppConfigKeys.OperationalParameters.TenderAccountPostingCodesToExclude)
                                                     .Get<List<string>>();

            // If the exclusion list is not configured or is empty, there is nothing to do. Return the original sheet.
            if (tenderCodesToExclude == null || !tenderCodesToExclude.Any())
            {
                Logger.LogInfo("Tender account exclusion list is empty or not configured. Skipping exclusion step.");
                _statusManager.Post("No tender exclusions configured.", MessageType.Info);
                return worksheet;
            }

            // For efficient lookups, convert the list of codes into a HashSet with case-insensitive comparison.
            var exclusionSet = new HashSet<string>(tenderCodesToExclude, StringComparer.OrdinalIgnoreCase);

            // Exit early if the worksheet has no data to filter. Return the original sheet.
            if (worksheet.Dimension == null || worksheet.Dimension.Rows < 2)
            {
                Logger.LogWarning($"Worksheet '{worksheet.Name}' has no data rows to filter for tender exclusions.");
                return worksheet;
            }

            // Perform the filtering on a background thread and get the new worksheet as the return value.
            var newWorksheet = await Task.Run(() =>
            {
                // Find the column index for "Posting Code" using our robust helper method.
                var columnMap = ExcelHelper.MapColumnIndices(worksheet, 1, new[] { "Posting Code" });
                int postingCodeColumnIndex = columnMap["Posting Code"];

                // Create a new temporary worksheet to hold the filtered data.
                var workbook = worksheet.Workbook;
                string originalSheetName = worksheet.Name;
                string tempSheetName = $"_temp_exclude_{Guid.NewGuid()}";
                var tempWorksheet = workbook.Worksheets.Add(tempSheetName);

                // Copy the header row from the original sheet to the new one.
                worksheet.Cells[1, 1, 1, worksheet.Dimension.End.Column].Copy(tempWorksheet.Cells[1, 1]);

                // This counter will track the next available row in our new temporary sheet.
                int destinationRowIndex = 2;
                int excludedRowCount = 0;

                // Iterate through all data rows in the original sheet.
                for (int r = 2; r <= worksheet.Dimension.End.Row; r++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    // Get the posting code from the current row.
                    var cellValue = worksheet.Cells[r, postingCodeColumnIndex].Value?.ToString()?.Trim();

                    // Check if the posting code is in our exclusion set.
                    if (string.IsNullOrEmpty(cellValue) || !exclusionSet.Contains(cellValue))
                    {
                        // If the code is NOT in the exclusion set, copy the entire row to the temporary sheet.
                        worksheet.Cells[r, 1, r, worksheet.Dimension.End.Column].Copy(tempWorksheet.Cells[destinationRowIndex, 1]);
                        destinationRowIndex++;
                    }
                    else
                    {
                        // If the code is in the exclusion set, increment our counter.
                        excludedRowCount++;
                    }
                }

                // After iterating, replace the original, unfiltered sheet with our new, filtered one.
                int originalIndex = worksheet.Index;
                workbook.Worksheets.Delete(worksheet); // This disposes the old worksheet object.
                tempWorksheet.Name = originalSheetName;

                // Move the new sheet back to the original's position in the workbook's sheet collection.
                if (originalIndex <= workbook.Worksheets.Count)
                {
                    var sheetToMoveBefore = workbook.Worksheets[originalIndex];
                    workbook.Worksheets.MoveBefore(tempWorksheet.Name, sheetToMoveBefore.Name);
                }

                Logger.LogInfo($"Tender account exclusion complete. Removed {excludedRowCount} row(s).");

                // Return the new, valid worksheet.
                return tempWorksheet;

            }, cancellationToken);

            // Return the new worksheet reference to the caller.
            return newWorksheet;
        }
        #endregion
    }
    #endregion
}

