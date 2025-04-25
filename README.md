# Quote Conversion Application

Automates the running of the Daily, Weekly, Monthly, Quarterly, or Annual reports, processing the data, and sending the result via email.

## ChangeLog

### Version 1.4.1
* **Bug Fixes**
    * Corrected the logic in `dailyCheckTimer_Tick`'s `finally` block to ensure the timer restarts reliably after an automated run, allowing the "Enable Auto Run" setting to persist across runs. The user no longer needs to re-enable auto-run after it completes successfully.
    * Ensured the `toggleAutoRunButton` is correctly re-enabled after an automated run completes or fails via the `ResetUIOnError` method.

### Version 1.4.0
* **Refactoring**
    * Modified `Program.cs` to pass the full path of `appsettings.json` to the `Form1` constructor.
    * Updated `Form1` constructor to accept and store the `appsettings.json` path.
    * Changed `Form1` to read the `AutoReport:LastRunDate` value using the injected `IConfiguration` instance (`ReadLastRunDateFromConfig`).
    * Modified `Form1` to save the `LastRunDate` back to the original `appsettings.json` file using the stored file path (`SaveLastRunDateToFile`), instead of relying solely on `IConfiguration` for writing.
    * Updated dynamic path properties (`ReportOutputLocation`, `ExcelTemplateLocation`, `ExcelFinalSaveLocation`) and other configuration reads in `Form1` to consistently use the injected `IConfiguration`.

### Version 1.3.9
* **New Features**
    * Implemented reading of `AutoReport:LastRunDate` from `appsettings.json` on startup to prevent the daily auto-run from executing if it already ran successfully on the current date.
    * Implemented saving the current date to `AutoReport:LastRunDate` in `appsettings.json` (formatted as `yyyy-MM-dd`) after a successful automated daily run.
    * Added logic to disable main UI controls (`createReportButton`, `processEmailButton`, input fields, view buttons, etc.) while the automated daily report is running and re-enable them afterward (`DisableControlsForAutoRun`, `EnableControlsAfterAutoRun`).
* **Bug Fixes**
    * Corrected the auto-run time check in `dailyCheckTimer_Tick` to only trigger between 8:00 AM and 8:05 AM.
    * Improved error handling and logging for reading/writing `appsettings.json` during the save/load of `LastRunDate`.

### Version 1.3.8
* **New Features**
    * Added `MenuStrip` with "Options" and "Help" menus.
    * Moved Dark Mode toggle from a CheckBox to the Options -> Dark Mode `ToolStripMenuItem`.
    * Added Help `ToolStripMenuItem` which displays a `MessageBox` with usage instructions.
* **Bug Fixes**
    * Corrected `UpdateAutoRunUI` and `RunAutomatedDailyReportAsync` to update the correct `ToolStripStatusLabel` (`autoRunStatusLabel`) for auto-run status updates, resolving issues with updating the label text and color.
    * Corrected `SafeControlUpdate` calls targeting `ToolStripStatusLabel` to invoke on the parent `StatusStrip` control for thread safety.
	* Corrected `RunAutomatedDailyReportAsync` progress reporting: Operational messages now correctly update the main status label (`statusLabel` on the left), while the final outcome (Completed/FAILED) is shown temporarily on the `autoRunStatusLabel` (right).
    * Updated various control names in `Form1.cs` code to match the provided `Form1.Designer.cs` names (e.g., `reportTypeComboBox`, `createReportButton`, `emailRecipientLabel`, etc.).
* **UI Improvements**
	* Main status label (`statusLabel`) now resets to "Ready" after an automated run attempt finishes.
    * Status updates are now split: General operations on the left (`statusLabel`), Auto-Run status on the right (`autoRunStatusLabel`).
    * Auto-Run button (`toggleAutoRunButton`) now correctly changes background color (LightGreen/LightCoral) when toggled, independent of the main theme.

### Version 1.3.6
* **New Features**
    * Added Auto-Run feature with UI toggle button (`btnToggleAutoRun`) and status label (`lblAutoRunStatus` - later changed to ToolStripLabel), triggered by a Timer (`timerDailyCheck`) to run the Daily report automatically at 8 AM.
	* Added logic to auto run the program, sending email to only Paul
* **Bug Fixes**
    * Resolved issue with auto-run status label updates. *(Superseded by 1.3.8 fixes)*

### Version 1.3.5
* **New Features**
    * Added "Daily" report type option (uses Weekly template).
    * Implemented specific folder structure for Daily reports (`<Base Path>\Daily Reports\<Month Name>\<Month Name> Week <Num>\`). *(Folder structure may be superseded by configuration settings)*
    * Added special email rule: Daily reports in Release mode are sent only to Paul S.
    * Added Dark Mode toggle (`checkBox2DarkMode` - later moved to MenuStrip) with theme application logic. Dark mode is now the default on startup.
* **Bug Fixes**
    * Resolved Excel file corruption potentially caused by EPPlus interaction with Excel Tables (solution: converted table to range in template).
    * Fixed ambiguous reference errors caused by duplicated helper methods in `Form1.cs`.
    * Corrected date format string in email body (`yyyy` instead of `pyrolysis`).
    * Corrected weekly date range calculation (now correctly covers 14 days).
    * Removed DPAPI encryption from `appsettings.json` to allow multi-user execution from a shared location. Configuration file must now be plain text JSON.
    * Fixed `CopyAnalysisDataToWeeklyReportAsync` to copy values instead of formulas.
    * Corrected logic to ensure the *original raw report filename* is populated in the `Analysis` sheet, and the *final processed weekly filename* is populated in the central weekly report during the append step.
* **UI Improvements**
    * Added `label6` (later renamed `emailRecipientLabel`) to indicate when Daily report email goes specifically to Paul.
    * Dynamically show/hide `checkBox1` (Femi Only) and `emailRecipientLabel` based on whether the Daily report type is selected.
    * Reformatted some single-line methods/lambdas in `Form1.cs` for better readability.

### Version 1.2.5
* **Other**
    * Refactored entirely to use .NET 8 and latest C# features.
    * Moved email client variables from `App.config` to `appsettings.json`.
    * Vastly improved performance by rewriting methods, especially row deletion in Excel processing.

### Version 1.1.1
* **New Features**
    * Added archiving for old log files. *(Clarified from original)*
* **Bug Fixes**
    * Fixed bug with creating new sheets in the weekly Power BI source document.
    * Fixed `startDatePicker` control not re-enabling correctly.
    * Fixed and re-enabled the "View Analysis" button.
* **Other**
    * Refactored entire code base for more modularity and maintainability.
    * Moved Email client variables to App.config

### Version 1.1.0
* **New Features**
    * Added options allowing user to select Weekly, Monthly, Quarterly, or Annual reports.
    * Added code to allow picking financial year (current or previous) and select the corresponding sheet in the weekly Power BI source document.
    * Added logic to automatically create the files and folder structure for each report type.
    * Added logic to automatically create folders for each year.
    * Added option to send the email only to Femi (for approval & custom date ranges).
    * Added check to see if the final report file already exists, allowing sending of the existing file.
    * Added retry logic for accessing files that might be temporarily locked.
    * Added checks to add the financial year sheet into the Power BI source document if it doesn't exist (copying headers).
    * Added logic to change email text based on report type and "Send to Femi Only" option.
    * Added option to skip sending the email after processing.
* **Bug Fixes**
    * Fixed bug where processing might skip row 2 of source data.
    * Fixed logic in `SendEmail` where sometimes incorrect dates could be set in the body/subject.

### Version 1.0.5
* **New Features**
    * Added checks for running Excel processes and prompts/attempts to close them before manual refresh steps.
    * Added prompt asking if the email needs to be sent after processing.
* **Performance Fixes**
    * Refactored code to increase performance and modularity.

### Version 1.0.4
* **Performance Fixes**
    * Changed Excel data copying function to use `Range.Copy` for increased performance.
* **Bug Fixes**
    * Fixed bugs with the email sending logic.

### Version 1.0.3
* **New Features**
    * Added options to run the report monthly. *(Superseded by 1.1.0)*

### Version 1.0.2
* **Bug Fixes**
    * Fixed problems caused by making program async, specifically issues with Excel data copying.

### Version 1.0.1
* **New Features**
    * Added status tracking via status bar.
* **Performance Fixes**
    * Made operations asynchronous for performance.

### Version 1.0.0
* Initial release version. Automates the creation of the weekly estimates report using templates and sends email to directors.
