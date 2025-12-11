#region Using Directives
using System;
#endregion

namespace QuoteConversionReportAutomation.Models
{
    #region Record Definition
    /// <summary>
    /// Represents the extracted data for a single lead time entry.
    /// This is used for generating the lead time analysis sheet and for retrospective analysis.
    /// </summary>
    /// <param name="SourceFile">The name of the source Excel file.</param>
    /// <param name="CustomerName">The name of the customer.</param>
    /// <param name="CustomerType">The type of customer (e.g., contract, non-contract).</param>
    /// <param name="EstimateNumber">The estimate or quote number.</param>
    /// <param name="OrderNumber">The resulting order or job number.</param>
    /// <param name="Value">The value of the estimate.</param>
    /// <param name="EstimateDate">The date the estimate was created.</param>
    /// <param name="OrderDate">The date the order was placed.</param>
    /// <param name="LeadTimeCalendarDays">The lead time calculated in total calendar days.</param>
    /// <param name="LeadTimeBusinessDays">The lead time calculated in business days (excluding weekends and bank holidays).</param>
    public record LeadTimeRecord(
        string SourceFile,
        string CustomerName,
        string CustomerType,
        string EstimateNumber,
        string OrderNumber,
        decimal Value,
        DateTime EstimateDate,
        DateTime OrderDate,
        double LeadTimeCalendarDays,
        int LeadTimeBusinessDays
    );
    #endregion
}