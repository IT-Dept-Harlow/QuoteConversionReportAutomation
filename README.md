# Quote Conversion Application

Automates the running of the Daily, Weekly, Monthly, Quarterly, or Annual reports, processing the data, and sending the result via email.

---

## ChangeLog

### Version 1.6.5

* **Bug Fixes & Improvements**
    * Fixed issue where the application would use the date it was initially opened for UI calculations (like default date ranges and financial year) instead of the current date if left running over midnight.
    * Modified `Form1` to remove the stored `_today` field and related `_financialYear` field.
    * Updated `reportTypeComboBox_SelectedIndexChanged` and `PopulateFinancialYearDropdown` to always use `DateTime.Today` for calculating default date ranges and the current financial year, ensuring the UI reflects the actual current date.
    * Ensured `processEmailButton_Click` gets the correct financial year dynamically before processing.
    * Updated Help text slightly to reflect that default dates/FY are based on the current date.
	* Removed Financial Year dropdown from daily reports as it's not used.
	* Updated and reformatted help text.

---

### Version 1.6.4

* **New Features**
    * Replaced help message box with a dedicated, resizable `HelpForm` with RTF support and basic theme awareness (dark/light mode).
    * Added automatic archiving for old report files on application startup:
        * **Final Reports:** Archives entire previous year folders (e.g., `...\Estimates\Weekly Reports\2024`) into a central `Archive` folder (e.g., `...\Estimates\Archive\Weekly Reports\2024`), merging contents if the destination year folder already exists.
        * **Raw Reports:** Archives files older than a configurable number of days (default 30, set via `settings:ArchiveRawOlderThanDays`) into an `Archive\YYYY-MM` subfolder within their respective report type folder (e.g., `...\Exports\Daily Reports\Archive\2025-03`).
* **Refactoring**
    * Removed redundant file cleanup/archiving logic from the `CrystalReportWrapper` project (`RunCrystalReportClass.cs`).
* **Bug Fixes & Improvements**
    * Passed dark mode theme setting to the new `HelpForm`.
    * Updated help text to include information about automated features (folder creation, archiving, sheet creation).

---

### Version 1.6.3

* **New Features**
    * Added automatic archiving for old log files on application startup (moves files older than 30 days to `Logs\[User]\Archive\YYYY\MM\WeekN`). *(Note: This was previously documented under v1.1.1 but implemented more robustly here)*.
* **Bug Fixes & Improvements**
    * Fixed folder creation logic for Quarterly reports in `FolderCreation.cs` to include the quarter subfolder (e.g., "Jan to Mar").
    * Ensured `FolderCreation.cs` and `ExcelCopyData.cs` use the `reportDate` for consistent folder path generation.

---

### Version 1.6.2

* **Logging Improvements**
    * Added configurable minimum logging level via `appsettings.json` (`settings:LogLevel` for Release, `settings:LogLevelDebug` for Debug).
    * Updated `Logger.cs` to read and apply the configured minimum log level.
    * Refined logging levels used in `ExcelCopyData.cs` and `Logger.cs` for better granularity (using `LogTrace` for finer details, adjusting `LogDebug`, `LogError`, `LogCritical` usage).
    * Replaced most `Debug.WriteLine` calls in `Logger.cs` with appropriate level-based logging.

---

### Version 1.6.1

* **New Features**
    * Added "Custom" report type, automatically selected when date pickers are manually changed.
    * Implemented specific folder structure (`Custom Reports\YYYY\YYYY-MM-DD_HHMMSS`) and filename format (`{EndDate}_{Timestamp}_Estimate_Success_Rate_Custom.xlsx`) for Custom reports.
    * Added distinct email subject/body content for Custom reports.
    * Added `Trace` logging level to `Logger` class (active only in DEBUG builds).
* **Refactoring**
    * Consolidated folder creation logic into the static `FolderCreation` class, removing duplication from `ExcelCopyData`.
* **Bug Fixes & Improvements**
    * Fixed issue where AutoRun could fail due to file lock when attaching the report to email (implemented reading attachment to memory stream).
    * Corrected DEBUG mode email recipient logic for the "Send to Femi Only" checkbox based on user clarification.
    * Fixed issue where manual refresh prompt (`FlexibleMessageBox`) could appear behind the main window and freeze the application (specified owner window).
    * Corrected status messages after report creation and during manual Excel refresh wait to be more informative.
    * Fixed folder creation logic for Monthly and Quarterly reports to include year/month or year/quarter subfolders.
    * Restored missing help text content.

---

### Version 1.6.0

* **Refactoring**
    * Refactored large `Form1.cs` into smaller, focused classes: `UIManager`, `ReportProcessManager`, `NamedPipeCommunicator`, `AutoRunManager`.
    * Created static `ReportHelper` class for utility functions (date calculations, file operations).
    * Changed `ExcelCopyData` to be a non-static class requiring instantiation.
* **Bug Fixes & Improvements**
    * Resolved various initial bugs related to the refactoring, including non-static method calls, protection levels, and `IProgress<T>` type mismatches during status reporting.

---

### Version 1.5.0

* **Bug Fixes & Improvements**
    * Refactored `FlexibleMessageBox.cs` to use the latest C# features.
    * Fixed bug where Rich Text was not showing in the message box.
    * *(Note: Empty bullet point removed)*

---

### Version 1.4.10

* **Bug Fixes & Improvements**
    * Fixed UI state management after manual report runs:
        * Auto-Run toggle button now remains enabled during manual report creation/processing.
        * Create/Process buttons correctly reset their enabled state after successful completion (Create enabled, Process disabled).
        * View Report/Analysis buttons now correctly remain visible and enabled if their corresponding files exist, only resetting when the report type is changed.
        * Main status label reliably resets to "Ready" after a 5-second delay following completion or error messages.
* **UI Improvements**
    * Integrated `FlexibleMessageBox` for displaying user messages (e.g., Help, errors, confirmations). *(Requires adding `FlexibleMessageBox.cs` to the project).*
* **Code Cleanup**
    * Removed unused `using` statements (`System.Collections.Generic`, `System.Data`, `System.Linq`, `System.Text`). Added `Microsoft.Win32`.

---

### Version 1.4.9

* **Bug Fixes & Improvements**
    * Fixed UI state issues after manual report runs:
        * Auto-Run toggle button remains enabled during manual report creation/processing.
        * Create/Process buttons now correctly reset their enabled state after completion or error.
        * View Report/Analysis buttons now correctly remain visible and enabled if their corresponding files exist, only resetting when the report type is changed.
        * Main status label now resets to "Ready" more reliably after displaying completion or error messages.
* **UI Improvements**
    * Integrated `FlexibleMessageBox` for displaying user messages (e.g., Help, errors, confirmations). *(Requires adding `FlexibleMessageBox.cs` to the project).*
* **Code Cleanup**
    * Removed unused `using` statements (`System.Collections.Generic`, `System.Data`, `System.Linq`, `System.Text`). Added `Microsoft.Win32`.

---

### Version 1.4.8

* **Bug Fixes & Improvements**
    * Improved AutoRun status display logic: The right status label now correctly preserves the final status ("Completed", "FAILED", "Done for...") for the current day, even when the timer state changes or the UI is reset.
    * Fixed main status label reset: The main (left) status label now reliably resets to "Ready" after AutoRun completion or failure, avoiding it getting stuck on the last AutoRun message.
* **Other**
    * Reverted the automated run check hour back to 8 AM in `dailyCheckTimer_Tick`.

---

### Version 1.4.7

* **Bug Fixes & Improvements**
    * Reverted AutoRun check hour back to 8 AM in `dailyCheckTimer_Tick`.
    * Fixed AutoRun status display logic: The right status label now correctly preserves the final status ("Completed", "FAILED", "Done for...") for the current day after the auto-run attempt finishes and controls are re-enabled.
    * Ensured main status label resets correctly to "Ready" after AutoRun completion or failure in the `dailyCheckTimer_Tick`'s `finally` block.

---

### Version 1.4.6

* **UI Improvements & Configuration**
    * Changed dark mode `CheckBox` background color back to match the main dark mode background (`_darkModeCheckBoxBackColor = Color.FromArgb(45, 45, 48)`).
    * Adjusted the auto-run check hour in `dailyCheckTimer_Tick` from 8 AM to 9 AM. *(Reverted in 1.4.7)*
    * Added a troubleshooting tip about Slicer refresh to the Help text (`helpToolStripMenuItem_Click`).
    * Added "AUTOMATED: " prefix to the email subject line generated by `GetEmailSubjectAndBody`.
    * Changed the text of the "Process & Email" button in `ResetButtonStatesAfterTypeChange` to "Process and Email".

---

### Version 1.4.5

* **Bug Fixes & Improvements**
    * Improved `CheckBox` visibility in dark mode by applying a specific background color (`_darkModeCheckBoxBackColor`). *(Reverted in 1.4.6)*
    * Updated `UpdateControlColors` method to accept and apply the theme-specific checkbox background color.

---

### Version 1.4.4

* **Bug Fixes & Improvements**
    * Modified `dailyCheckTimer_Tick` logic: The timer now continues running after the daily check completes (even if the report already ran). It uses a flag (`_autoRunStatusSetForToday`) to prevent repeated logging/status updates within the target hour once the check for the day is done.
    * Added `_autoRunStatusSetForToday` and `_autoRunStatusDate` fields to `Form1` to manage the daily check status.
    * Updated `toggleAutoRunButton_Click` to reset the `_autoRunStatusSetForToday` flag.

---

### Version 1.4.3

* **Bug Fixes & Improvements**
    * Modified `dailyCheckTimer_Tick` logic: When the timer determines the auto-run has already completed for the day, it now stops the timer to prevent repeated checks within the 8 AM hour. The timer needs to be manually re-enabled or the app restarted for the next day's check. *(Superseded by 1.4.4)*

---

### Version 1.4.2

* **Bug Fixes**
    * Reverted calls to `ExcelCopyData.GetExpectedFinalFilePath` and `ExcelCopyData.ProcessExcelReportAsync` in `Form1.cs` to remove the extra `DateTime` argument, resolving "No overload for method..." errors caused by the mismatch with the existing `ExcelCopyData.cs` method signatures. Filename generation logic remains solely within `ExcelCopyData.cs` for this version.

---

### Version 1.4.1

* **Bug Fixes**
    * Corrected the logic in `dailyCheckTimer_Tick`'s `finally` block to ensure the timer restarts reliably after an automated run, allowing the "Enable Auto Run" setting to persist across runs. The user no longer needs to re-enable auto-run after it completes successfully.
    * Ensured the `toggleAutoRunButton` is correctly re-enabled after an automated run completes or fails via the `ResetUIOnError` method.
* **Refactoring (Intended - Caused Overload Errors, Fixed in 1.4.2)**
    * Added `DateTime` argument to calls to `ExcelCopyData.GetExpectedFinalFilePath` and `ExcelCopyData.ProcessExcelReportAsync` in `Form1.cs` to control filename generation based on report date. *(Requires corresponding changes in `ExcelCopyData.cs`)*.

---

### Version 1.4.0

* **Refactoring**
    * Modified `Program.cs` to pass the full path of `appsettings.json` to the `Form1` constructor.
    * Updated `Form1` constructor to accept and store the `appsettings.json` path.
    * Changed `Form1` to read the `AutoReport:LastRunDate` value using the injected `IConfiguration` instance (`ReadLastRunDateFromConfig`).
    * Modified `Form1` to save the `LastRunDate` back to the original `appsettings.json` file using the stored file path (`SaveLastRunDateToFile`), instead of relying solely on `IConfiguration` for writing.
    * Updated dynamic path properties (`ReportOutputLocation`, `ExcelTemplateLocation`, `ExcelFinalSaveLocation`) and other configuration reads in `Form1` to consistently use the injected `IConfiguration`.

---

### Version 1.3.9

* **New Features**
    * Implemented reading of `AutoReport:LastRunDate` from `appsettings.json` on startup to prevent the daily auto-run from executing if it already ran successfully on the current date.
    * Implemented saving the current date to `AutoReport:LastRunDate` in `appsettings.json` (formatted as `yyyy-MM-dd`) after a successful automated daily run.
    * Added logic to disable main UI controls (`createReportButton`, `processEmailButton`, input fields, view buttons, etc.) while the automated daily report is running and re-enable them afterward (`DisableControlsForAutoRun`, `EnableControlsAfterAutoRun`).
* **Bug Fixes**
    * Corrected the auto-run time check in `dailyCheckTimer_Tick` to only trigger between 8:00 AM and 8:05 AM.
    * Improved error handling and logging for reading/writing `appsettings.json` during the save/load of `LastRunDate`.

---

### Version 1.3.8

* **New Features**
    * Added `MenuStrip` with "Options" and "Help" menus.
    * Moved Dark Mode toggle from a `CheckBox` to the Options -> Dark Mode `ToolStripMenuItem`.
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

---

### Version 1.3.6

* **New Features**
    * Added Auto-Run feature with UI toggle button (`btnToggleAutoRun`) and status label (`lblAutoRunStatus` - later changed to ToolStripLabel), triggered by a Timer (`timerDailyCheck`) to run the Daily report automatically at 8 AM.
    * Added logic to auto run the program, sending email to only Paul.
* **Bug Fixes**
    * Resolved issue with auto-run status label updates. *(Superseded by 1.3.8 fixes)*

---

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

---

### Version 1.2.5

* **Refactoring & Modernization** *(Combined related items)*
    * Refactored entirely to use .NET 8 and latest C# features.
    * Moved email client variables from `App.config` to `appsettings.json`.
    * Vastly improved performance by rewriting methods, especially row deletion in Excel processing.

---

### Version 1.1.1

* **New Features**
    * Added archiving for old log files. *(Clarified from original)*
* **Bug Fixes**
    * Fixed bug with creating new sheets in the weekly Power BI source document.
    * Fixed `startDatePicker` control not re-enabling correctly.
    * Fixed and re-enabled the "View Analysis" button.
* **Refactoring** *(Combined related items)*
    * Refactored entire code base for more modularity and maintainability.
    * Moved Email client variables to `App.config`.

---

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

---

### Version 1.0.5

* **New Features**
    * Added checks for running Excel processes and prompts/attempts to close them before manual refresh steps.
    * Added prompt asking if the email needs to be sent after processing.
* **Performance Fixes & Refactoring** *(Combined related items)*
    * Refactored code to increase performance and modularity.

---

### Version 1.0.4

* **Performance Fixes**
    * Changed Excel data copying function to use `Range.Copy` for increased performance.
* **Bug Fixes**
    * Fixed bugs with the email sending logic.

---

### Version 1.0.3

* **New Features**
    * Added options to run the report monthly. *(Superseded by 1.1.0)*

---

### Version 1.0.2

* **Bug Fixes**
    * Fixed problems caused by making program async, specifically issues with Excel data copying.

---

### Version 1.0.1

* **New Features**
    * Added status tracking via status bar.
* **Performance Fixes**
    * Made operations asynchronous for performance.

---

### Version 1.0.0

* **Initial Release**
    * Automates the creation of the weekly estimates report using templates and sends email to directors.
