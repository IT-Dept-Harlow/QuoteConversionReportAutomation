// Shared classes for request/response structure (Use this file in BOTH projects)
// Place this in a shared location or copy it to both the .NET 8 and .NET 4.8 projects.
// Ensure namespaces match or are accessible in both.
namespace ReportWrapperCommon
{
    public class ReportRequest
    {
        public string CrystalReportLocation { get; set; }
        public string ReportOutputLocation { get; set; }
        public DateTime ReportDateFrom { get; set; }
        public DateTime ReportDateTo { get; set; }
        // Add other parameters if needed in the future
    }

    public class ReportResponse
    {
        public bool Success { get; set; }
        public string OutputPath { get; set; } // Return the actual output path
        public string ErrorMessage { get; set; }
    }
}
