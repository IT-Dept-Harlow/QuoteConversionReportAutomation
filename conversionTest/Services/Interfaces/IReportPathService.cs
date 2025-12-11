#region Using Directives
using QuoteConversionReportAutomation.Models; // For ReportType enum
using System;
#endregion

namespace QuoteConversionReportAutomation.Services.Interfaces
{
    #region Interface Definition
    /// <summary>
    /// Defines the contract for a service that provides access to application paths
    /// and report-specific path generation logic.
    /// </summary>
    public interface IReportPathService
    {
        #region Properties
        /// <summary>
        /// Gets the full path to the Crystal Report definition file (.rpt).
        /// </summary>
        string CrystalReportRptFilePath { get; }

        /// <summary>
        /// Gets the full path to the Crystal Report Wrapper executable.
        /// </summary>
        string WrapperExecutablePath { get; }

        /// <summary>
        /// Gets the base directory for storing final processed Excel reports.
        /// This path is resolved, handling user-profile relative paths from configuration.
        /// </summary>
        string FinalReportOutputBaseDirectory { get; }

        /// <summary>
        /// Gets the base directory where Excel template files are stored.
        /// This path is resolved, handling user-profile relative paths from configuration.
        /// </summary>
        string TemplateBaseDirectory { get; }

        /// <summary>
        /// Gets the base directory for exporting raw Crystal Reports.
        /// This path is resolved, handling user-profile relative paths from configuration.
        /// </summary>
        string RawReportExportBaseDirectory { get; }

        /// <summary>
        /// Gets the configured base directory for application log files.
        /// This path is resolved, handling environment variables.
        /// </summary>
        string LogDirectoryBase { get; }

        /// <summary>
        /// Gets the configured filename for the automated report definitions JSON file (e.g., "autoReportDefinitions.json").
        /// </summary>
        string ReportDefinitionsFileName { get; }

        /// <summary>
        /// Gets the directory where the main application settings file (appsettings.json) is located.
        /// This is used to determine the location of other configuration files like autoReportDefinitions.json.
        /// </summary>
        string AppSettingsDirectory { get; }

        /// <summary>
        /// Gets the configured fallback directory for logs if the primary <see cref="LogDirectoryBase"/> is inaccessible.
        /// This path is resolved, handling environment variables.
        /// </summary>
        string FallbackLogDirectory { get; }
        #endregion

        #region Methods
        /// <summary>
        /// Gets the full output path for a raw Crystal Report export file based on the report type and date context.
        /// </summary>
        /// <param name="reportType">The <see cref="ReportType"/> enum value representing the report type.</param>
        /// <param name="dateContext">The date used to determine year, month, week, or timestamp subfolders.</param>
        /// <param name="reportNameForFileName">The base name to use for the report file, defaults to "EstimateSuccessReport".</param>
        /// <returns>The full, absolute path to where the raw report should be saved, or null if path construction fails.</returns>
        string? GetRawReportOutputPath(ReportType reportType, DateTime dateContext, string reportNameForFileName = "EstimateSuccessReport");

        /// <summary>
        /// Gets the full path to the Excel template file based on the report type.
        /// </summary>
        /// <param name="reportType">The <see cref="ReportType"/> enum value representing the report type.</param>
        /// <returns>The full, absolute path to the Excel template file, or null if path construction fails.</returns>
        string? GetExcelTemplatePath(ReportType reportType);

        /// <summary>
        /// Gets the full path to the automated report definitions JSON file.
        /// </summary>
        /// <returns>The full, absolute path to the report definitions file, or null if path construction fails.</returns>
        string? GetReportDefinitionsFilePath();

        /// <summary>
        /// Checks if essential path configurations (e.g., Crystal Report .rpt file, Wrapper executable) are valid and exist.
        /// </summary>
        /// <returns>True if essential path configurations are valid; otherwise, false.</returns>
        bool IsEssentialPathConfigurationValid();

        /// <summary>
        /// Gets the fully resolved, user-specific directory path for storing log files.
        /// This combines the <see cref="LogDirectoryBase"/> (or <see cref="FallbackLogDirectory"/>) with a sanitised username subfolder.
        /// </summary>
        /// <returns>The full path to the user-specific log directory.</returns>
        string GetUserSpecificLogDirectory();

        /// <summary>
        /// Generates the final filename for a processed report based on its type and date.
        /// </summary>
        /// <param name="reportType">The type of the report.</param>
        /// <param name="reportDate">The primary date for the report.</param>
        /// <param name="runTimestamp">The timestamp of the run, used for uniqueness in custom reports.</param>
        /// <returns>A string representing the generated filename.</returns>
        string GenerateFinalFileName(ReportType reportType, DateTime reportDate, DateTime runTimestamp);

        /// <summary>
        /// Gets the expected full file path for a final processed report.
        /// Used for checking if a report for the period already exists.
        /// </summary>
        /// <param name="reportType">The type of the report.</param>
        /// <param name="baseFileSaveLocation">The base directory where final reports are saved.</param>
        /// <param name="reportDate">The primary date for the report (e.g., end date).</param>
        /// <returns>The expected full file path, or null if an error occurs.</returns>
        string? GetExpectedFinalFilePath(ReportType reportType, string baseFileSaveLocation, DateTime reportDate);
        #endregion
    }
    #endregion
}