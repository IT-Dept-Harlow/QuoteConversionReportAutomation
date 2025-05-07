# Quote Conversion Application

Automates the running of the Daily, Weekly, Monthly, Quarterly, or Annual reports, processing the data, and sending the result via email.

---

## ChangeLog

### Version 1.7.1

* **Custom Bank Holiday Management UI**
    * Added a new "Manage Custom Bank Holidays" option in the "Options" menu.
    * This opens a new form (`ManageBankHolidaysForm`) allowing users to:
        * View existing custom one-off and recurring bank holidays.
        * Add new one-off bank holidays (specifying date and description).
        * Add new recurring bank holidays (specifying day, month, and description).
        * Remove selected custom one-off or recurring bank holidays.
    * Custom bank holidays added by the user are now persisted in a `custom_bank_holidays.json` file located in the application's base directory. This file is loaded at startup and updated whenever changes are made via the new management form.
    * The `BankHolidayHelper.cs` was updated with methods to load, save, add, and remove these custom holidays from the JSON file, and to clear its internal cache when modifications occur.
    * The main application help text was updated to include information about this new feature.

* **UI Theming & Rendering Fixes**
    * Resolved issues with `MenuStrip` theming when switching between dark and light modes, ensuring sub-menus and item backgrounds/foregrounds render correctly and consistently. This involved refining the `DarkModeMenuRenderer`, `DarkModeColorTable`, and the `ApplyTheme` and `UpdateMenuItemsTheme` methods in `UIManager.cs`.
    * Fixed CS0120 errors in `DarkModeMenuRenderer` by correctly making color fields used in the base constructor call `static readonly`.
    * Corrected RTF formatting for the help text in `Form1.cs` to ensure it displays correctly in the `HelpForm`, primarily by using `StringBuilder` and ensuring proper C# escaping for RTF control words.

* **Code & Functionality Refinements**
    * Updated the `GetEmailRecipients()` method in `Form1.cs` to the user-provided version for accurate email distribution logic based on report type, "Send to Femi Only" checkbox, and build mode (Debug/Release).
    * Corrected a CS7036 error in `Form1.cs` related to a missing `ToolStripProgressBar` argument in the `UIManager` constructor call, after the progress bar functionality was intentionally removed. The `UIManager` constructor and `Form1.cs` instantiation were aligned.
    * Incremented application version to v1.7.1.

---

### Version 1.7.0

* **Bank Holiday Integration & Previous Workday Logic**
    * Integrated comprehensive bank holiday calculations (`BankHolidayHelper.cs`) into the `GetPreviousWorkday` logic used by `ReportHelper.cs` and `AutoRunManager.cs`.
    * This enhancement ensures that Daily reports and the auto-run Daily report feature now accurately determine the previous working day by correctly skipping:
        * Weekends (Saturdays and Sundays).
        * Standard bank holidays for **England and Wales**.
        * Moving bank holidays (e.g., Good Friday, Easter Monday, Spring, and Summer bank holidays which depend on Easter's date or are proclaimed).
        * Bank holidays that fall on a weekend, which are then observed on the following Monday (or Tuesday if Monday itself is a bank holiday, e.g., Christmas/Boxing Day scenarios).
    * Updated `Form1.cs` to utilize the enhanced `ReportHelper.GetPreviousWorkday` for UI date calculations for the "Daily" report type.
    * Help text in `Form1.cs` updated to reflect the new bank holiday considerations for daily reports, including details on how they are calculated.

* **UI Enhancements & Menu Options**
    * Added `ToolTip` support throughout `Form1.cs` and `Form1.Designer.cs`, providing helpful hints and descriptions for various UI controls to improve user experience.
    * Revamped "Options" menu in `Form1.cs` and `Form1.Designer.cs` with new functionalities:
        * "View Configuration": Replaced "Check Configuration". Shows a detailed breakdown of current configuration paths (Crystal Report, Wrapper EXE, Template, Export, Save, and Log directories) and their existence status in a message box.
        * "Validate Configuration": Performs a quick validation of essential configuration paths (Crystal Report and Wrapper EXE) and updates the main status bar with "Configuration OK." or an error message.
        * "Open Logs Folder": Opens the user-specific application log directory in File Explorer.
        * "Edit appsettings.json": Opens the main `appsettings.json` configuration file for manual editing using the default system application.
        * "Exit": Added an option to close the application.
    * Updated application help text to include information about these new menu options.

* **UI Theming & Rendering (Initial Fixes)**
    * Addressed initial issues with `MenuStrip` theming when switching between dark and light modes.
    * Implemented a custom `DarkModeMenuRenderer` and `DarkModeColorTable` in `UIManager.cs`.
    * Corrected `StatusStrip` item layout in `Form1.Designer.cs`.

* **Code & Functionality Refinements**
    * Corrected event handler subscriptions in `Form1.cs` for new menu items to prevent them from firing twice.
    * Aligned log path determination logic in `Form1.cs` (for "View Configuration" and "Open Logs Folder" features) to correctly use the `settings:LogDirectory` configuration key, consistent with `Logger.cs`.
    * Ensured that `UIManager.cs` correctly handles updates to `ToolStripItems` (like `ToolStripStatusLabel`) by invoking updates on their parent `StatusStrip` or `MenuStrip` when necessary for thread safety.
    * Incremented application version to v1.7.0.

---

### Version 1.6.5

* **Bug Fixes & Improvements**
    * Fixed issue where the application would use the date it was initially opened for UI calculations (like default date ranges and financial year) instead of the current date if left running over midnight.
    * Modified `Form1` to remove the stored `_today` field and related `_financialYear` field.
    * Updated `reportTypeComboBox_SelectedIndexChanged` and `PopulateFinancialYearDropdown` to always use `DateTime.Today` for calculating default date ranges and the current financial year, ensuring the UI reflects the actual current date.
    * Ensured `processEmailButton_Click` gets the correct financial year dynamically before processing.
    * Removed Financial Year dropdown from daily reports as it's not used.
    * Updated Help text (`HelpForm`) to accurately state that default date ranges and financial year selections are calculated based on the *current date* when the report type is changed, not the application start date. Also improved RTF formatting for folder path examples.
    * Fixed issue where AutoRun failed with "Access Denied" because `AutoRunManager.cs` was not correctly combining relative paths from configuration (`RawReportExportBaseDir`, `ExcelFinalSaveLocation`, `ExcelTemplateBaseDir`) with the user's profile path. Updated `AutoRunManager.cs` to construct full paths correctly, matching `Form1.cs`.

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

---

### Version 1.4.10

* **Bug Fixes & Improvements**
    * Fixed UI state management after manual report runs:
        * Auto-Run toggle button now remains enabled during manual report creation/processing.
        * Create/Process buttons correctly reset their enabled state after successful completion (Create enabled, Process disabled).
        * View Report/Analysis buttons now correctly remain visible and enabled if their corresponding files exist, only resetting when the report type is changed.
        * Main status label reliably resets to "Ready" after a 5-second delay following completion or error messages.
* **UI Improvements**
    * Integrated `FlexibleMessageBox` for displaying user messages (e.g., Help, errors, confirmations).
* **Code Cleanup**
    * Removed unused `using` statements. Added `Microsoft.Win32`.

---

### Version 1.4.9

* **Bug Fixes & Improvements**
    * Fixed UI state issues after manual report runs.
    * Main status label now resets to "Ready" more reliably.
* **UI Improvements**
    * Integrated `FlexibleMessageBox`.
* **Code Cleanup**
    * Removed unused `using` statements. Added `Microsoft.Win32`.

---

### Version 1.4.8

* **Bug Fixes & Improvements**
    * Improved AutoRun status display logic.
    * Fixed main status label reset.
* **Other**
    * Reverted automated run check hour to 8 AM.

---

### Version 1.4.7

* **Bug Fixes & Improvements**
    * Reverted AutoRun check hour to 8 AM.
    * Fixed AutoRun status display logic.
    * Ensured main status label resets correctly.

---

### Version 1.4.6

* **UI Improvements & Configuration**
    * Adjusted dark mode `CheckBox` background.
    * Added Slicer refresh tip to Help.
    * Added "AUTOMATED:" prefix to email subjects.
    * Updated button text.

---

### Version 1.4.5

* **Bug Fixes & Improvements**
    * Improved `CheckBox` visibility in dark mode.
    * Updated `UpdateControlColors`.

---

### Version 1.4.4

* **Bug Fixes & Improvements**
    * Modified `dailyCheckTimer_Tick` logic for continuous running.
    * Added flags for daily check status management.

---

### Version 1.4.3

* **Bug Fixes & Improvements**
    * Modified `dailyCheckTimer_Tick` logic to stop timer after daily completion. *(Superseded)*

---

### Version 1.4.2

* **Bug Fixes**
    * Corrected `ExcelCopyData` method calls in `Form1.cs`.

---

### Version 1.4.1

* **Bug Fixes**
    * Fixed `dailyCheckTimer_Tick` restart logic.
    * Ensured `toggleAutoRunButton` re-enables correctly.

---

### Version 1.4.0

* **Refactoring**
    * Changed `appsettings.json` handling for `LastRunDate`.
    * Updated configuration reads to use `IConfiguration`.

---

### Version 1.3.9

* **New Features**
    * Implemented `LastRunDate` read/save for AutoRun.
    * Added UI control disabling during AutoRun.
* **Bug Fixes**
    * Corrected AutoRun time check.
    * Improved `appsettings.json` error handling.

---

### Version 1.3.8

* **New Features**
    * Added `MenuStrip` with "Options" and "Help".
    * Moved Dark Mode to menu.
* **Bug Fixes**
    * Corrected AutoRun status label updates.
    * Fixed `SafeControlUpdate` for `ToolStripStatusLabel`.
    * Improved status reporting clarity.
* **UI Improvements**
    * Status label reset.
    * Auto-Run button color logic.

---

### Version 1.3.6

* **New Features**
    * Added Auto-Run feature.
    * Auto-run emails only Paul for Daily reports.

---

### Version 1.3.5

* **New Features**
    * Added "Daily" report type.
    * Specific folder structure for Daily reports.
    * Special email rule for Daily reports (Release mode).
    * Added Dark Mode toggle.
* **Bug Fixes**
    * Resolved Excel file corruption issue.
    * Fixed ambiguous reference errors.
    * Corrected date format string in email.
    * Corrected weekly date range calculation.
    * Removed DPAPI encryption.
    * Fixed `CopyAnalysisDataToWeeklyReportAsync`.
    * Corrected filename population in `Analysis` sheet.
* **UI Improvements**
    * Added label for Daily report email recipient.
    * Dynamic visibility for "Femi Only" checkbox.

---

### Version 1.2.5

* **Refactoring & Modernization**
    * Refactored to .NET 8 and latest C# features.
    * Moved email client variables to `appsettings.json`.
    * Improved performance, especially Excel row deletion.

---

### Version 1.1.1

* **New Features**
    * Added archiving for old log files.
* **Bug Fixes**
    * Fixed bug with creating new sheets in weekly Power BI source.
    * Fixed `startDatePicker` re-enabling.
    * Fixed "View Analysis" button.
* **Refactoring**
    * Modularized code base.
    * Moved Email client variables to `App.config`.

---

### Version 1.1.0

* **New Features**
    * Added report type selection (Weekly, Monthly, etc.).
    * Added financial year picking.
    * Automatic file/folder structure creation.
    * "Send to Femi Only" option.
    * Check for existing final report file.
    * Retry logic for locked files.
    * Automatic FY sheet creation in Power BI source.
    * Dynamic email text.
    * Option to skip email sending.
* **Bug Fixes**
    * Fixed bug skipping row 2 in source data.
    * Fixed email date formatting.

---

### Version 1.0.5

* **New Features**
    * Checks for running Excel processes.
    * Prompt to send email after processing.
* **Performance Fixes & Refactoring**
    * Performance and modularity improvements.

---

### Version 1.0.4

* **Performance Fixes**
    * Changed Excel data copying to use `Range.Copy`.
* **Bug Fixes**
    * Fixed email sending logic bugs.

---

### Version 1.0.3

* **New Features**
    * Added options to run report monthly. *(Superseded)*

---

### Version 1.0.2

* **Bug Fixes**
    * Fixed issues with async Excel data copying.

---

### Version 1.0.1

* **New Features**
    * Added status tracking via status bar.
* **Performance Fixes**
    * Made operations asynchronous.

---

### Version 1.0.0

* **Initial Release**
    * Automates weekly estimates report creation and emailing.
