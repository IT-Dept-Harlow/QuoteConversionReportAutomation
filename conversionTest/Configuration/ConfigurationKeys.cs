// ConfigurationKeys.cs
// Centralizes all configuration key strings used to access settings from appsettings.json
// for the Quote Conversion Report Automation (QCRA) application.

namespace QuoteConversionReportAutomation.Configuration
{
    /// <summary>
    /// Provides static constants for configuration keys, mirroring the structure of appsettings.json.
    /// This helps avoid "magic strings" and improves maintainability when accessing configuration values.
    /// </summary>
    public static class AppConfigKeys
    {
        #region Application Information
        /// <summary>
        /// Configuration keys related to general application information.
        /// Path: "ApplicationInfo"
        /// </summary>
        public static class ApplicationInfo
        {
            /// <summary>Base path for ApplicationInfo settings. Path: "ApplicationInfo"</summary>
            public const string Base = "ApplicationInfo";
            /// <summary>Configuration key for the application's display name. Path: "ApplicationInfo:AppName"</summary>
            public const string AppName = Base + ":AppName";
            /// <summary>Configuration key for the application's current version. Path: "ApplicationInfo:AppVersion"</summary>
            public const string AppVersion = Base + ":AppVersion";
        }
        #endregion

        #region File and Directory Paths
        /// <summary>
        /// Configuration keys related to file and directory paths used by the application.
        /// Path: "Paths"
        /// </summary>
        public static class Paths
        {
            /// <summary>Base path for Paths settings. Path: "Paths"</summary>
            public const string Base = "Paths";
            /// <summary>Configuration key for the Crystal Report definition file (.rpt). Path: "Paths:CrystalReportRptFile"</summary>
            public const string CrystalReportRptFile = Base + ":CrystalReportRptFile";
            /// <summary>Configuration key for the Crystal Report Wrapper executable. Path: "Paths:WrapperExecutable"</summary>
            public const string WrapperExecutable = Base + ":WrapperExecutable";
            /// <summary>Configuration key for the base directory for final processed Excel reports. Path: "Paths:FinalReportOutputBase"</summary>
            public const string FinalReportOutputBase = Base + ":FinalReportOutputBase";
            /// <summary>Configuration key for the base directory for Excel template files. Path: "Paths:TemplateBase"</summary>
            public const string TemplateBase = Base + ":TemplateBase";
            /// <summary>Configuration key for the base directory for raw exported Crystal Reports. Path: "Paths:RawReportOutputBase"</summary>
            public const string RawReportOutputBase = Base + ":RawReportOutputBase";
            /// <summary>Configuration key for the base directory for application log files. Path: "Paths:LogDirectoryBase"</summary>
            public const string LogDirectoryBase = Base + ":LogDirectoryBase";
            /// <summary>Configuration key for the filename of the automated report definitions JSON file. Path: "Paths:ReportDefinitionsFileName"</summary>
            public const string ReportDefinitionsFileName = Base + ":ReportDefinitionsFileName";
        }
        #endregion

        #region SMTP Configuration
        /// <summary>
        /// Configuration keys related to SMTP (email server) settings.
        /// Path: "SmtpConfiguration"
        /// </summary>
        public static class SmtpConfiguration
        {
            /// <summary>Base path for SmtpConfiguration settings. Path: "SmtpConfiguration"</summary>
            public const string Base = "SmtpConfiguration";
            /// <summary>Configuration key for the SMTP server hostname or IP address. Path: "SmtpConfiguration:Server"</summary>
            public const string Server = Base + ":Server";
            /// <summary>Configuration key for the SMTP server port number. Path: "SmtpConfiguration:Port"</summary>
            public const string Port = Base + ":Port";
            /// <summary>Configuration key for the SMTP authentication username. Path: "SmtpConfiguration:Username"</summary>
            public const string Username = Base + ":Username";
            /// <summary>Configuration key for the SMTP authentication password. Path: "SmtpConfiguration:Password"</summary>
            public const string Password = Base + ":Password";
            /// <summary>Configuration key for enabling SSL/TLS for SMTP. Path: "SmtpConfiguration:EnableSsl"</summary>
            public const string EnableSsl = Base + ":EnableSsl";
            /// <summary>Configuration key for the maximum number of retries for sending an email. Path: "SmtpConfiguration:MaxSendRetries"</summary>
            public const string MaxSendRetries = Base + ":MaxSendRetries";
            /// <summary>Configuration key for the initial delay in milliseconds before retrying an email send. Path: "SmtpConfiguration:SendRetryDelayMs"</summary>
            public const string SendRetryDelayMs = Base + ":SendRetryDelayMs";
            /// <summary>Configuration key for the timeout in milliseconds for SMTP operations. Path: "SmtpConfiguration:TimeoutMs"</summary>
            public const string TimeoutMs = Base + ":TimeoutMs";
        }
        #endregion

        #region Email Settings
        /// <summary>
        /// Configuration keys related to general email settings and default recipient/greeting lists.
        /// Path: "EmailSettings"
        /// </summary>
        public static class EmailSettings
        {
            /// <summary>Base path for EmailSettings. Path: "EmailSettings"</summary>
            public const string Base = "EmailSettings";
            /// <summary>Configuration key for the default 'From' email address. Path: "EmailSettings:SenderAddress"</summary>
            public const string SenderAddress = Base + ":SenderAddress";
            /// <summary>Configuration key for the default sender display name. Path: "EmailSettings:SenderDisplayName"</summary>
            public const string SenderDisplayName = Base + ":SenderDisplayName";
            /// <summary>Configuration key for the maximum allowed size for email attachments in bytes. Path: "EmailSettings:MaxAttachmentSizeBytes"</summary>
            public const string MaxAttachmentSizeBytes = Base + ":MaxAttachmentSizeBytes";
            /// <summary>Configuration key for the default email signature text. Path: "EmailSettings:DefaultEmailSignature"</summary>
            public const string DefaultEmailSignature = Base + ":DefaultEmailSignature";
            /// <summary>Configuration key for maximum retries for reading attachment files if locked. Path: "EmailSettings:AttachmentReadMaxRetries"</summary>
            public const string AttachmentReadMaxRetries = Base + ":AttachmentReadMaxRetries";
            /// <summary>Configuration key for delay in milliseconds between attachment read retries. Path: "EmailSettings:AttachmentReadDelayMs"</summary>
            public const string AttachmentReadDelayMs = Base + ":AttachmentReadDelayMs";

            /// <summary>
            /// Configuration keys for default email recipient lists in production scenarios.
            /// Path: "EmailSettings:ProductionRecipients"
            /// </summary>
            public static class ProductionRecipients
            {
                /// <summary>Base path for production recipient settings. Path: "EmailSettings:ProductionRecipients"</summary>
                public const string Base = EmailSettings.Base + ":ProductionRecipients";

                /// <summary>Configuration key for 'To' recipients for manually run standard daily reports. Path: "EmailSettings:ProductionRecipients:ManualRunDailyTo"</summary>
                public const string ManualRunDailyTo = Base + ":ManualRunDailyTo";
                /// <summary>Configuration key for 'CC' recipients for manually run standard daily reports. Path: "EmailSettings:ProductionRecipients:ManualRunDailyCC"</summary>
                public const string ManualRunDailyCC = Base + ":ManualRunDailyCC";
                /// <summary>Configuration key for 'To' recipients for manually run custom reports. Path: "EmailSettings:ProductionRecipients:ManualCustomTo"</summary>
                public const string ManualCustomTo = Base + ":ManualCustomTo";
                /// <summary>Configuration key for 'CC' recipients for manually run custom reports. Path: "EmailSettings:ProductionRecipients:ManualCustomCC"</summary>
                public const string ManualCustomCC = Base + ":ManualCustomCC";
                /// <summary>Configuration key for 'To' recipients when 'Send to Femi Only' is checked. Path: "EmailSettings:ProductionRecipients:FemiTo"</summary>
                public const string FemiTo = Base + ":FemiTo";
                /// <summary>Configuration key for 'CC' recipients when 'Send to Femi Only' is checked. Path: "EmailSettings:ProductionRecipients:FemiCC"</summary>
                public const string FemiCC = Base + ":FemiCC";
                /// <summary>Configuration key for 'To' recipients for the general team list (manual non-daily/non-custom). Path: "EmailSettings:ProductionRecipients:TeamTo"</summary>
                public const string TeamTo = Base + ":TeamTo";
                /// <summary>Configuration key for 'CC' recipients for the general team list (manual non-daily/non-custom). Path: "EmailSettings:ProductionRecipients:TeamCC"</summary>
                public const string TeamCC = Base + ":TeamCC";
                /// <summary>Configuration key for 'To' recipients for the manual new customer report. Path: "EmailSettings:ProductionRecipients:ManualNewCustomerTo"</summary>
                public const string ManualNewCustomerTo = Base + ":ManualNewCustomerTo";
                /// <summary>Configuration key for 'CC' recipients for the manual new customer report. Path: "EmailSettings:ProductionRecipients:ManualNewCustomerCC"</summary>
                public const string ManualNewCustomerCC = Base + ":ManualNewCustomerCC";

                // Category-based automated report recipients
                /// <summary>Configuration key for 'To' recipients for automated standard daily reports. Path: "EmailSettings:ProductionRecipients:AutoRunDailyStandardRecipientsTo"</summary>
                public const string AutoRunDailyStandardRecipientsTo = Base + ":AutoRunDailyStandardRecipientsTo";
                /// <summary>Configuration key for 'CC' recipients for automated standard daily reports. Path: "EmailSettings:ProductionRecipients:AutoRunDailyStandardRecipientsCC"</summary>
                public const string AutoRunDailyStandardRecipientsCC = Base + ":AutoRunDailyStandardRecipientsCC";
                /// <summary>Configuration key for 'To' recipients for automated 'Daily (5days >= £1k)' reports. Path: "EmailSettings:ProductionRecipients:AutoRunDaily5Day1kRecipientsTo"</summary>
                public const string AutoRunDaily5Day1kRecipientsTo = Base + ":AutoRunDaily5Day1kRecipientsTo";
                /// <summary>Configuration key for 'CC' recipients for automated 'Daily (5days >= £1k)' reports. Path: "EmailSettings:ProductionRecipients:AutoRunDaily5Day1kRecipientsCC"</summary>
                public const string AutoRunDaily5Day1kRecipientsCC = Base + ":AutoRunDaily5Day1kRecipientsCC";
                /// <summary>Configuration key for 'To' recipients for automated weekly reports. Path: "EmailSettings:ProductionRecipients:AutoRunWeeklyRecipientsTo"</summary>
                public const string AutoRunWeeklyRecipientsTo = Base + ":AutoRunWeeklyRecipientsTo";
                /// <summary>Configuration key for 'CC' recipients for automated weekly reports. Path: "EmailSettings:ProductionRecipients:AutoRunWeeklyRecipientsCC"</summary>
                public const string AutoRunWeeklyRecipientsCC = Base + ":AutoRunWeeklyRecipientsCC";
                /// <summary>Configuration key for 'To' recipients for the automated new customer report. Path: "EmailSettings:ProductionRecipients:AutoRunNewCustomerRecipientsTo"</summary>
                public const string AutoRunNewCustomerRecipientsTo = Base + ":AutoRunNewCustomerRecipientsTo";
                /// <summary>Configuration key for 'CC' recipients for the automated new customer report. Path: "EmailSettings:ProductionRecipients:AutoRunNewCustomerRecipientsCC"</summary>
                public const string AutoRunNewCustomerRecipientsCC = Base + ":AutoRunNewCustomerRecipientsCC";
                // Add other category keys here as needed, e.g.:
                // public const string AutoRunMonthlyMarketingRecipientsTo = Base + ":AutoRunMonthlyMarketingRecipientsTo";

                /// <summary>
                /// Configuration keys for default email greetings in production scenarios.
                /// Path: "EmailSettings:ProductionRecipients:EmailGreetings"
                /// </summary>
                public static class EmailGreetings
                {
                    /// <summary>Base path for production email greeting settings. Path: "EmailSettings:ProductionRecipients:EmailGreetings"</summary>
                    public const string Base = ProductionRecipients.Base + ":EmailGreetings";
                    /// <summary>Path: "EmailSettings:ProductionRecipients:EmailGreetings:AutoRunDaily"</summary>
                    public const string AutoRunDaily = Base + ":AutoRunDaily";
                    /// <summary>Path: "EmailSettings:ProductionRecipients:EmailGreetings:ManualStdDaily"</summary>
                    public const string ManualStdDaily = Base + ":ManualStdDaily";
                    /// <summary>Path: "EmailSettings:ProductionRecipients:EmailGreetings:AutoRunDaily5Day1k"</summary>
                    public const string AutoRunDaily5Day1k = Base + ":AutoRunDaily5Day1k";
                    /// <summary>Path: "EmailSettings:ProductionRecipients:EmailGreetings:ManualFemi"</summary>
                    public const string ManualFemi = Base + ":ManualFemi";
                    /// <summary>Path: "EmailSettings:ProductionRecipients:EmailGreetings:ManualTeam"</summary>
                    public const string ManualTeam = Base + ":ManualTeam";
                    /// <summary>Path: "EmailSettings:ProductionRecipients:EmailGreetings:ManualCustom"</summary>
                    public const string ManualCustom = Base + ":ManualCustom";
                    // Add other greeting keys here as needed
                }
            }

            /// <summary>
            /// Configuration keys for default email recipient lists in debug mode.
            /// Path: "EmailSettings:DebugRecipients"
            /// </summary>
            public static class DebugRecipients
            {
                /// <summary>Base path for debug recipient settings. Path: "EmailSettings:DebugRecipients"</summary>
                public const string Base = EmailSettings.Base + ":DebugRecipients";
                /// <summary>Path: "EmailSettings:DebugRecipients:To"</summary>
                public const string To = Base + ":To";
                /// <summary>Path: "EmailSettings:DebugRecipients:CC1"</summary>
                public const string CC1 = Base + ":CC1";
                /// <summary>Path: "EmailSettings:DebugRecipients:CC2"</summary>
                public const string CC2 = Base + ":CC2";

                /// <summary>
                /// Configuration keys for default email greetings in debug mode.
                /// Path: "EmailSettings:DebugRecipients:EmailGreetings"
                /// </summary>
                public static class EmailGreetings
                {
                    /// <summary>Base path for debug email greeting settings. Path: "EmailSettings:DebugRecipients:EmailGreetings"</summary>
                    public const string Base = DebugRecipients.Base + ":EmailGreetings";
                    /// <summary>Path: "EmailSettings:DebugRecipients:EmailGreetings:DebugDefault"</summary>
                    public const string DebugDefault = Base + ":DebugDefault";
                }
            }
        }
        #endregion

        #region Logging Configuration
        /// <summary>
        /// Configuration keys related to application logging.
        /// Path: "Logging"
        /// </summary>
        public static class Logging
        {
            /// <summary>Base path for Logging settings. Path: "Logging"</summary>
            public const string Base = "Logging";
            /// <summary>Configuration key for the default minimum log level in Release builds. Path: "Logging:DefaultLogLevel"</summary>
            public const string DefaultLogLevel = Base + ":DefaultLogLevel";
            /// <summary>Configuration key for the minimum log level in Debug builds. Path: "Logging:DebugBuildLogLevel"</summary>
            public const string DebugBuildLogLevel = Base + ":DebugBuildLogLevel";
            /// <summary>Configuration key for the number of days after which log files are archived. Path: "Logging:LogArchiveOlderThanDays"</summary>
            public const string LogArchiveOlderThanDays = Base + ":LogArchiveOlderThanDays";
            /// <summary>Configuration key for the log filename format string. Path: "Logging:LogFileNameFormat"</summary>
            public const string LogFileNameFormat = Base + ":LogFileNameFormat";
            /// <summary>Configuration key for the fallback log directory if the primary is inaccessible. Path: "Logging:DefaultFallbackLogDirectory"</summary>
            public const string DefaultFallbackLogDirectory = Base + ":DefaultFallbackLogDirectory";
        }
        #endregion

        #region Operational Parameters
        /// <summary>
        /// Configuration keys related to various operational parameters of the application.
        /// Path: "OperationalParameters"
        /// </summary>
        public static class OperationalParameters
        {
            public const string Base = "OperationalParameters";
            /// <summary>Configuration key for archiving raw reports older than a specified number of days. Path: "OperationalParameters:ArchiveRawReportsOlderThanDays"</summary>
            public const string ArchiveRawReportsOlderThanDays = Base + ":ArchiveRawReportsOlderThanDays";
            /// <summary>Configuration key for the name of the main report archive folder. Path: "OperationalParameters:ReportArchiveFolderName"</summary>
            public const string ReportArchiveFolderName = Base + ":ReportArchiveFolderName";
            /// <summary>Configuration key for the general timeout in minutes for long-running processes. Path: "OperationalParameters:ProcessTimeoutMinutes"</summary>
            public const string ProcessTimeoutMinutes = Base + ":ProcessTimeoutMinutes";
            /// <summary>Configuration key for the month (1-12) when the financial year starts. Path: "OperationalParameters:FinancialYearStartMonth"</summary>
            public const string FinancialYearStartMonth = Base + ":FinancialYearStartMonth";
            /// <summary>Configuration key for the day (1-31) of the month when the financial year starts. Path: "OperationalParameters:FinancialYearStartDay"</summary>
            public const string FinancialYearStartDay = Base + ":FinancialYearStartDay";
            /// <summary>Configuration key for the monetary threshold for 'Daily (5days >= £X)' report filtering. Path: "OperationalParameters:Daily5Day1kFilteringThreshold"</summary>
            public const string Daily5Day1kFilteringThreshold = Base + ":Daily5Day1kFilteringThreshold";
            /// <summary>Configuration key for the list of posting codes for the New Customer report. Path: "OperationalParameters:NewCustomerPostingCodes"</summary>
            public const string NewCustomerPostingCodes = Base + ":NewCustomerPostingCodes";
            /// <summary>Configuration key for the list of tender posting codes to exclude from analysis. Path: "OperationalParameters:TenderAccountPostingCodesToExclude"</summary>
            public const string TenderAccountPostingCodesToExclude = Base + ":TenderAccountPostingCodesToExclude";
            /// <summary>Configuration key for maximum retries for general file system operations. Path: 
            /// "OperationalParameters:GeneralFileOperationMaxRetries"</summary>
            public const string GeneralFileOperationMaxRetries = Base + ":GeneralFileOperationMaxRetries";
            /// <summary>Configuration key for initial delay in milliseconds between file operation retries. Path: "OperationalParameters:GeneralFileOperationDelayMs"</summary>
            public const string GeneralFileOperationDelayMs = Base + ":GeneralFileOperationDelayMs";

            /// <summary>
            /// Lead Time Analysis settings.
            /// </summary>
            public static class LeadTimeAnalysisSettings
            {
                /// <summary>Base path for Lead Time Analysis settings. Path: "OperationalParameters:LeadTimeAnalysisSettings"</summary>
                public const string Base = OperationalParameters.Base + ":LeadTimeAnalysisSettings";
                public const string EnabledForReportTypes = Base + ":EnabledForReportTypes";
            }

            /// <summary>
            /// Configuration keys for Excel sheet names.
            /// Path: "OperationalParameters:ExcelSheetNames"
            /// </summary>
            public static class ExcelSheetNames
            {
                /// <summary>Base path for Excel sheet name settings. Path: "OperationalParameters:ExcelSheetNames"</summary>
                public const string Base = OperationalParameters.Base + ":ExcelSheetNames";
                /// <summary>Path: "OperationalParameters:ExcelSheetNames:RawDataSourceSheet"</summary>
                public const string RawDataSourceSheet = Base + ":RawDataSourceSheet";
                /// <summary>Path: "OperationalParameters:ExcelSheetNames:TemplateDataCopySheet"</summary>
                public const string TemplateDataCopySheet = Base + ":TemplateDataCopySheet";
                /// <summary>Path: "OperationalParameters:ExcelSheetNames:TemplateAnalysisSheet"</summary>
                public const string TemplateAnalysisSheet = Base + ":TemplateAnalysisSheet";
                /// <summary>Path: "OperationalParameters:ExcelSheetNames:PowerBiDataSheet"</summary>
                public const string PowerBiDataSheet = Base + ":PowerBiDataSheet";
                /// <summary>Path: "OperationalParameters:ExcelSheetNames:MonthlyOrderPivotSheet"</summary>
                public const string MonthlyOrderPivotSheet = Base + ":MonthlyOrderPivotSheet";
                /// <summary>Path: "OperationalParameters:ExcelSheetNames:MonthlyEstimatePivotSheet"</summary>
                public const string MonthlyEstimatePivotSheet = Base + ":MonthlyEstimatePivotSheet";
            }

            /// <summary>
            /// Configuration keys for PivotTable names within Excel templates.
            /// Path: "OperationalParameters:PivotTableNames"
            /// </summary>
            public static class PivotTableNames
            {
                /// <summary>Base path for PivotTable name settings. Path: "OperationalParameters:PivotTableNames"</summary>
                public const string Base = OperationalParameters.Base + ":PivotTableNames";
                /// <summary>Path: "OperationalParameters:PivotTableNames:MonthlyOrderPivot"</summary>
                public const string MonthlyOrderPivot = Base + ":MonthlyOrderPivot";
                /// <summary>Path: "OperationalParameters:PivotTableNames:MonthlyEstimatePivot"</summary>
                public const string MonthlyEstimatePivot = Base + ":MonthlyEstimatePivot";
            }

            /// <summary>
            /// Configuration keys for folder names based on report type.
            /// Path: "OperationalParameters:ReportTypeFolderNames"
            /// </summary>
            public static class ReportTypeFolderNames
            {
                /// <summary>Base path for report type folder name settings. Path: "OperationalParameters:ReportTypeFolderNames"</summary>
                public const string Base = OperationalParameters.Base + ":ReportTypeFolderNames";
                /// <summary>Path: "OperationalParameters:ReportTypeFolderNames:Daily"</summary>
                public const string Daily = Base + ":Daily";
                /// <summary>Path: "OperationalParameters:ReportTypeFolderNames:Daily5Day1k"</summary>
                public const string Daily5Day1k = Base + ":Daily5Day1k";
                /// <summary>Path: "OperationalParameters:ReportTypeFolderNames:Weekly"</summary>
                public const string Weekly = Base + ":Weekly";
                /// <summary>Path: "OperationalParameters:ReportTypeFolderNames:Monthly"</summary>
                public const string Monthly = Base + ":Monthly";
                /// <summary>Path: "OperationalParameters:ReportTypeFolderNames:Quarterly"</summary>
                public const string Quarterly = Base + ":Quarterly";
                /// <summary>Path: "OperationalParameters:ReportTypeFolderNames:Annual"</summary>
                public const string Annual = Base + ":Annual";
                /// <summary>Path: "OperationalParameters:ReportTypeFolderNames:Custom"</summary>
                public const string Custom = Base + ":Custom";
                /// <summary>Path: "OperationalParameters:ReportTypeFolderNames:Other"</summary>
                public const string Other = Base + ":Other";
            }
        }
        #endregion

        #region Inter-Process Communication (IPC)
        /// <summary>
        /// Configuration keys related to inter-process communication (e.g., Named Pipes).
        /// Path: "InterProcessCommunication"
        /// </summary>
        public static class InterProcessCommunication
        {
            /// <summary>Base path for IPC settings. Path: "InterProcessCommunication"</summary>
            public const string Base = "InterProcessCommunication";
            /// <summary>Configuration key for the named pipe name. Path: "InterProcessCommunication:NamedPipeName"</summary>
            public const string NamedPipeName = Base + ":NamedPipeName";
            /// <summary>Configuration key for the named pipe connection timeout in milliseconds. Path: "InterProcessCommunication:PipeConnectTimeoutMs"</summary>
            public const string PipeConnectTimeoutMs = Base + ":PipeConnectTimeoutMs";
            /// <summary>Configuration key for the maximum expected response message size from the pipe server in bytes. Path: "InterProcessCommunication:MaxPipeResponseSizeBytes"</summary>
            public const string MaxPipeResponseSizeBytes = Base + ":MaxPipeResponseSizeBytes";
        }
        #endregion

        #region AutoRun Process
        /// <summary>
        /// Configuration keys related to the automated report processing (AutoRun) feature.
        /// Path: "AutoRunProcess"
        /// </summary>
        public static class AutoRunProcess
        {
            /// <summary>Base path for AutoRunProcess settings. Path: "AutoRunProcess"</summary>
            public const string Base = "AutoRunProcess";
            /// <summary>Configuration key for the hour of the day (0-23) for the daily auto-run check. Path: "AutoRunProcess:CheckHour"</summary>
            public const string CheckHour = Base + ":CheckHour";
            /// <summary>Configuration key for storing the date of the last successful global auto-run. Path: "AutoRunProcess:LastRunDate"</summary>
            public const string LastRunDate = Base + ":LastRunDate";
            /// <summary>
            /// Configuration key for the section storing daily run statuses for individual automated reports.
            /// Path: "AutoRunProcess:DailyRunStatus"
            /// </summary>
            public const string DailyRunStatus = Base + ":DailyRunStatus";
            /// <summary>
            /// Configuration key within DailyRunStatus for the date of those statuses.
            /// Path: "AutoRunProcess:DailyRunStatus:StatusDate"
            /// </summary>
            public const string DailyRunStatus_StatusDate = DailyRunStatus + ":StatusDate";
            // Individual report success flags (e.g., "StandardDailyReportSucceeded") are dynamic keys under DailyRunStatus.
        }
        #endregion
    }
}
