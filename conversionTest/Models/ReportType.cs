// ReportType.cs
// Defines the enumeration for different types of reports in the QCRA application.

namespace QuoteConversionReportAutomation.Models
{
    /// <summary>
    /// Specifies the different types of reports that can be generated.
    /// </summary>
    public enum ReportType
    {
        /// <summary>
        /// Standard daily report for the previous working day.
        /// </summary>
        Daily = 0,

        /// <summary>
        /// Daily report covering the previous five working days, filtered for estimates >= £1000.
        /// </summary>
        Daily5Day1k = 1,

        /// <summary>
        /// Weekly report, typically covering a 15-day rolling period.
        /// </summary>
        Weekly = 2,

        /// <summary>
        /// Monthly report for the previous full calendar month.
        /// </summary>
        Monthly = 3,

        /// <summary>
        /// Quarterly report for the previous full calendar quarter.
        /// </summary>
        Quarterly = 4,

        /// <summary>
        /// Annual report for the previous full financial year.
        /// </summary>
        Annual = 5,

        /// <summary>
        /// Report for a custom-defined date range.
        /// </summary>
        Custom = 6,

        /// <summary>
        /// Represents an unknown or unspecified report type.
        /// </summary>
        Unknown = -1 // Or another appropriate default/error value
    }
}
