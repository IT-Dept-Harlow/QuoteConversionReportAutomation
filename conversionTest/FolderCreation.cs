// C# 10+ Features
namespace conversionTest;

using System;
using System.Globalization;
using System.IO;
using conversionTest; // For Logger

/// <summary>
/// Provides static utility methods for creating report-specific folders with a structured hierarchy.
/// </summary>
public static class FolderCreation
{
    /// <summary>
    /// Creates a nested folder structure based on the report type and date.
    /// Example Paths:
    /// - Weekly:    {basePath}\Weekly reports\{ReportYear}\{Month} Week {WeekNum}  (e.g., ...\Weekly reports\2025\Apr Week 4)
    /// - Monthly:   {basePath}\Monthly reports\{ReportYear}\{Month} {YY}           (e.g., ...\Monthly reports\2025\Apr 25)
    /// - Quarterly: {basePath}\Quarterly reports\{ReportYear}\{StartMonth} to {EndMonth} {Year} (e.g., ...\Quarterly reports\2025\Jan to Mar 2025)
    /// - Annual:    {basePath}\Annual reports\{ReportYear}                      (e.g., ...\Annual reports\2024)
    /// Handles directory creation if it doesn't exist.
    /// </summary>
    /// <param name="reportType">Indicates the report type (0=Weekly, 1=Monthly, 2=Quarterly, 3=Annual).</param>
    /// <param name="basePath">The base path where the folder structure should be created (e.g., ...\Estimates\).</param>
    /// <returns>The full path of the created or existing final folder, or null if an error occurs.</returns>
    public static string? CreateReportSpecificFolder(int reportType, string basePath)
    {
        ArgumentException.ThrowIfNullOrEmpty(basePath);

        try
        {
            // Determine the parts of the path
            string reportTypeSubDir = GetReportTypeBaseSubDir(reportType); // e.g., "Weekly reports"
            string reportYearString = GetReportYearString(reportType);     // e.g., "2025" or "2024" for annual
            string? finalFolderName = GetFinalFolderName(reportType);       // e.g., "Apr Week 4", "Apr 25", "Jan to Mar 2025", or null for annual

            if (reportYearString == null) // Should not happen with current logic, but safety check
            {
                Logger.LogError($"Could not determine report year for report type {reportType}.");
                return null;
            }

            // Construct the full path
            string fullFolderPath;
            if (reportType == 3) // Annual: basePath \ reportTypeSubDir \ reportYearString
            {
                fullFolderPath = Path.Combine(basePath, reportTypeSubDir, reportYearString);
                // finalFolderName is effectively the year itself for annual reports
            }
            else if (finalFolderName != null) // Other types: basePath \ reportTypeSubDir \ reportYearString \ finalFolderName
            {
                fullFolderPath = Path.Combine(basePath, reportTypeSubDir, reportYearString, finalFolderName);
            }
            else // Error case for non-annual types if finalFolderName is null
            {
                Logger.LogError($"Could not determine final folder name for report type {reportType}.");
                return null;
            }

            // Ensure the full directory structure exists
            Directory.CreateDirectory(fullFolderPath); // Creates all directories in the path if they don't exist
            Logger.LogInfo($"Ensured report-specific folder exists: {fullFolderPath}");
            return fullFolderPath;
        }
        catch (Exception ex)
        {
            Logger.LogError($"Error creating report-specific folder structure based on type {reportType} and base path '{basePath}': {ex.Message}", ex);
            return null; // Return null to indicate failure
        }
    }

    /// <summary>
    /// Gets the base subdirectory name for the report type (e.g., "Weekly reports").
    /// </summary>
    private static string GetReportTypeBaseSubDir(int reportType)
    {
        return reportType switch
        {
            1 => "Monthly reports",
            2 => "Quarterly reports",
            3 => "Annual reports",
            _ => "Weekly reports" // Default for 0 or invalid
        };
    }

    /// <summary>
    /// Gets the relevant year string for the report period.
    /// </summary>
    private static string GetReportYearString(int reportType)
    {
        DateTime now = DateTime.Now;
        return reportType switch
        {
            0 => // Weekly: Current year
                 now.Year.ToString(),
            1 => // Monthly: Year of the target month
                 (now.Day <= 15 ? now.AddMonths(-1) : now).Year.ToString(),
            2 => // Quarterly: Year the previous quarter *ends* in
                 GetPreviousQuarterEndDate(now).Year.ToString(),
            3 => // Annual: Previous year
                 (now.Year - 1).ToString(),
            _ => // Default/Invalid: Current year
                 now.Year.ToString()
        };
    }


    /// <summary>
    /// Gets the final, specific part of the folder name based on the report type and date logic.
    /// Returns null for Annual reports as the year itself is the final part handled elsewhere.
    /// </summary>
    /// <param name="reportType">The type of report (0: Weekly, 1: Monthly, 2: Quarterly, 3: Annual).</param>
    /// <returns>The final folder name string (e.g., "Apr Week 4", "Apr 25", "Jan to Mar 2025"), or null for Annual or invalid types.</returns>
    private static string? GetFinalFolderName(int reportType)
    {
        DateTime now = DateTime.Now;
        try
        {
            return reportType switch
            {
                0 => // Weekly: "Apr Week 4"
                    $"{now.ToString("MMM", CultureInfo.InvariantCulture)} Week {GetWeekOfMonth(now)}",
                1 => // Monthly: "Apr 25" (Uses previous month if run <= 15th)
                    (now.Day <= 15 ? now.AddMonths(-1) : now).ToString("MMM yy", CultureInfo.InvariantCulture),
                2 => // Quarterly: "Jan to Mar 2025" (Uses previous quarter)
                    GetPreviousQuarterFolderName(now),
                3 => // Annual: Year is handled by GetReportYearString
                    null, // Return null, year is the folder itself
                _ => // Invalid type
                    null,
            };
        }
        catch (Exception ex)
        {
            Logger.LogError($"Error determining final folder name for report type {reportType}: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Helper to get the end date of the previous quarter.
    /// </summary>
    private static DateTime GetPreviousQuarterEndDate(DateTime currentDate)
    {
        int currentQuarter = (currentDate.Month - 1) / 3 + 1;
        int firstMonthOfCurrentQuarter = (currentQuarter - 1) * 3 + 1;
        return new DateTime(currentDate.Year, firstMonthOfCurrentQuarter, 1).AddDays(-1);
    }

    /// <summary>
    /// Generates the folder name for the previous quarter (e.g., "Jan to Mar 2025").
    /// </summary>
    private static string GetPreviousQuarterFolderName(DateTime currentDate)
    {
        DateTime endOfPreviousQuarter = GetPreviousQuarterEndDate(currentDate);
        DateTime startOfPreviousQuarter = endOfPreviousQuarter.AddMonths(-3).AddDays(1);

        string startMonth = startOfPreviousQuarter.ToString("MMM", CultureInfo.InvariantCulture);
        string endMonth = endOfPreviousQuarter.ToString("MMM", CultureInfo.InvariantCulture);
        string yearString = endOfPreviousQuarter.Year.ToString(); // Use the year the quarter ends in

        return $"{startMonth} to {endMonth} {yearString}";
    }


    /// <summary>
    /// Calculates the week of the month for a given date, using Monday as the first day of the week.
    /// </summary>
    /// <param name="date">The date for which to calculate the week of the month.</param>
    /// <returns>The week number of the month (1-5).</returns>
    private static int GetWeekOfMonth(DateTime date)
    {
        DateTime firstDayOfMonth = new DateTime(date.Year, date.Month, 1);
        // DayOfWeek returns 0 for Sunday, 1 for Monday, ..., 6 for Saturday.
        // We want Monday to be 0, Sunday to be 6.
        int firstDayOfMonthDayOfWeek = ((int)firstDayOfMonth.DayOfWeek + 6) % 7; // Adjust Sunday to 6, Monday to 0 etc.
        // Calculate week number: (day of month + offset of first day - 1) / 7 + 1
        int weekOfMonth = (date.Day + firstDayOfMonthDayOfWeek - 1) / 7 + 1;
        return Math.Min(weekOfMonth, 5); // Cap at 5 weeks
    }
}
