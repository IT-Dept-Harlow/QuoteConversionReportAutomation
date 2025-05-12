# Quote Conversion Application

Automates the running of the Daily, Weekly, Monthly, Quarterly, or Annual reports, processing the data, and sending the result via email.

---

## ChangeLog

## [1.8.0] - 2025-05-12

### Changed
- **Annual Report Dates**: Modified the "Annual" report type to use a financial year running from May 1st to April 30th, instead of a calendar year. This affects date calculations in `Form1.cs` and `ReportHelper.cs`, and the corresponding descriptions in email subjects/bodies and help text.
- **Help Text**: Updated the help text for the "Annual" report type to reflect the May-April financial year.
- **Email Content**: Adjusted the email subject and body for "Annual" reports to correctly state the financial year range (e.g., "Financial Year 2023-2024").
- **Filename Logic**: Ensured that filename generation and expected path checks for "Annual" reports use a consistent date (the start of the financial year, e.g., May 1st) for clarity.

### Fixed
- Ensured UI controls (especially the Auto-Run button and main action buttons) are consistently re-enabled and their text/status updated correctly after an automated run cycle completes or is bypassed by the timer.
- Removed a redundant UI update call when toggling the 1-Click processing mode to potentially improve responsiveness.

---

## [1.7.9] - 2025-05-09

### Added
- **1-Click Processing Mode**: Introduced a new "Enable 1-Click Processing" option in the "Options" menu. When enabled, the "Create Report" and "Process and Email" buttons are replaced by a single "Generate, Process & Email Report" button to perform all actions sequentially.
- **Skip Sending Email Option**: Added a "Skip Sending Email" checkbox within the "Report Settings" group box. If checked, the email sending step is bypassed during manual or 1-Click processing.
- **Configurable Auto-Run Hour**: Added an option under "Options" menu ("Set Auto-Run Hour...") to allow users to change the hour (0-23) at which the daily automated report task executes. This setting is saved in `appsettings.json`.
- **Auto-Run Time Display on Button**: The "Enable/Disable Daily Auto Run" button now displays the configured auto-run hour (e.g., "@ 8:00") as part of its text on application load and after changes.

### Changed
- **Help Text Enhancements**:
    - Significantly updated and expanded the RTF help text to be more comprehensive, explaining all new features, automated processes, and detailed troubleshooting steps.
    - Corrected RTF formatting for better readability, including ensuring spaces appear correctly after bolded text followed by colons and fixing bullet point alignment.
    - Revised Excel refresh instructions in the help text to be more specific about right-clicking PivotTables and Slicers in the "OrderPivot" and "Estimate Success PivotTable" sheets.
- **UI Manager**: Updated to correctly manage new UI elements related to 1-Click processing and skip email functionality. Also updated to correctly format the auto-run button text with the configured hour.
- **AutoRunManager**: Modified to use and persist the configurable auto-run hour.
- **Form1 Logic**:
    - Implemented event handlers and UI logic for the new 1-Click processing mode, skip email checkbox, and setting the auto-run hour.
    - Ensured the `toggleAutoRunButton` text is updated on application load and when the hour is changed to reflect the currently configured auto-run hour.

### Fixed
- Resolved issue where the auto-run button text might not update correctly to "Disable..." when toggled.
- Corrected RTF formatting for help text to ensure proper spacing and bullet point rendering.

---

## [1.7.4] - 2025-05-09

### Added
- **Email Recipient Management Feature**:
    - Added a new "Manage Email Recipients" option under the "Options" menu in `Form1.cs`.
    - This opens a new `ManageEmailRecipientsForm` which allows users to:
        - View current email recipients for different report types and scenarios (Production: AutoRun Daily, Femi Only, Team; Debug: To, CC1, CC2).
        - Edit these recipient lists (multiple emails can be entered, separated by commas or semicolons).
        - Save their custom recipient settings. These overrides are stored in a `user_email_settings.json` file in the user's `AppData\Roaming\HarlowSolutions\QuoteConversionReportAutomation` directory.
        - Restore all recipients to the application defaults defined in `appsettings.json` (this deletes the user override file).
    - Introduced `UserEmailSettings.cs` as the data model for storing user-defined recipient lists.
    - Created `EmailRecipientManager.cs` to:
        - Load default recipients from `appsettings.json`.
        - Load user-defined overrides from `user_email_settings.json`.
        - Provide the effective email recipients by merging defaults with overrides (overrides take precedence).
        - Save user overrides to `user_email_settings.json`.
        - Clear user overrides to restore defaults.
        - Validate email address formats.
    - The `ManageEmailRecipientsForm` includes theming support (Dark/Light mode) consistent with the main application.

### Changed
- **Form1.cs**:
    - Updated `GetEmailRecipients()` to use `EmailRecipientManager` to determine To/CC lists, thus incorporating user overrides.
    - Corrected calls for `GetPreviousWorkday` to use `ReportHelper.GetPreviousWorkday` instead of `BankHolidayHelper.GetPreviousWorkday`.
    - Refactored `createReportButton_Click` and `processEmailButton_Click` event handlers from `async Task` back to `async void`. Core async logic moved to new `PerformCreateReportAsync()` and `PerformProcessAndEmailAsync()` methods.
    - Corrected references to `autoRunStatusLabel.Text`.
    - Corrected configuration key for `ExcelTemplateBaseDir` to `settings:ExcelTemplateFolder`.
    - Updated help text to include the new "Manage Email Recipients" feature.
- **AutoRunManager.cs**:
    - Updated constructor to accept `EmailRecipientManager`.
    - Modified `RunAutomatedDailyReportAsync` to use injected `EmailRecipientManager` for email recipients.
- **Application Version**: Updated to `1.7.4`.

---

## [1.7.2] - 2025-05-09

### Changed
- **Power BI Data Export Refinements (ExcelCopyData.cs)**:
    - Renamed `CopyAnalysisDataToWeeklyReportAsync` to `CopyAnalysisDataToPowerBIReportAsync`.
    - Modified `CopyAnalysisDataToPowerBIReportAsync` to always use a hardcoded sheet name: `"powerBI"`.
    - Logic for creating the target sheet now specifically checks for and creates `"powerBI"` sheet if non-existent.
    - Removed `selectedFinYear` parameter from `CopyAnalysisDataToPowerBIReportAsync` for sheet naming.
    - Updated call to `CopyAnalysisDataToPowerBIReportAsync` in `ProcessPostCopyOperationsAsync`.
    - Aligned XML comments and logging.

---

## [1.7.1] - 2025-05-07

### Added
- **Custom Bank Holiday Management UI**:
    - "Manage Custom Bank Holidays" option in "Options" menu.
    - New `ManageBankHolidaysForm` for viewing, adding (one-off/recurring), and removing custom bank holidays.
    - Persistence of custom bank holidays in `custom_bank_holidays.json`.
- **BankHolidayHelper.cs**: Updated to load, save, add, remove, and clear custom holidays from JSON.
- **Help Text**: Updated to include custom bank holiday management.

### Changed
- **UI Theming & Rendering**:
    - Refined `DarkModeMenuRenderer`, `DarkModeColorTable`, `ApplyTheme`, and `UpdateMenuItemsTheme` in `UIManager.cs` for consistent `MenuStrip` theming.
- **Form1.cs**: Updated `GetEmailRecipients()` to user-provided version.
- **Application Version**: Incremented to v1.7.1.

### Fixed
- **UI Theming**: Resolved `MenuStrip` rendering issues in dark/light modes.
- **CS0120 Errors**: Corrected `static readonly` for color fields in `DarkModeMenuRenderer`.
- **RTF Formatting**: Ensured correct help text display in `HelpForm`.
- **CS7036 Error**: Aligned `UIManager` constructor call in `Form1.cs` regarding removed `ToolStripProgressBar`.

---

## [1.7.0] - 2025-05-07

### Added
- **Bank Holiday Integration**: Integrated comprehensive bank holiday calculations for England and Wales into `GetPreviousWorkday` logic, accounting for weekends, standard holidays, moving holidays, and observed days.
- **UI Enhancements & Menu Options**:
    - `ToolTip` support throughout `Form1.cs` and `Form1.Designer.cs`.
    - Revamped "Options" menu:
        - "View Configuration": Shows detailed configuration paths and status.
        - "Validate Configuration": Quick check of essential configs, updates status bar.
        - "Open Logs Folder": Opens user-specific log directory.
        - "Edit appsettings.json": Opens main config file.
        - "Exit": Closes the application.
- **Help Text**: Updated to include bank holiday details and new menu options.

### Changed
- **Form1.cs**: Utilizes enhanced `ReportHelper.GetPreviousWorkday`.
- **UI Theming**: Implemented custom `DarkModeMenuRenderer` and `DarkModeColorTable` in `UIManager.cs`.
- **Log Path Logic**: Aligned log path determination in `Form1.cs` with `Logger.cs`.
- **UIManager.cs**: Ensured thread-safe updates for `ToolStripItems`.
- **Application Version**: Incremented to v1.7.0.

### Fixed
- **UI Theming**: Addressed initial `MenuStrip` theming issues.
- **Event Handlers**: Corrected subscriptions for new menu items in `Form1.cs`.
- **StatusStrip Layout**: Corrected item layout in `Form1.Designer.cs`.

---

## [1.6.5] - 2025-05-01

### Changed
- **Date Calculation**: Modified `Form1` to use `DateTime.Today` for UI calculations (default date ranges, financial year) instead of application start date.
- **Financial Year**: Removed Financial Year dropdown from daily reports.
- **Help Text**: Updated to reflect dynamic date calculations.
- **AutoRunManager.cs**: Corrected path construction for `RawReportExportBaseDir`, `ExcelFinalSaveLocation`, `ExcelTemplateBaseDir` by combining with user profile path.

### Fixed
- **UI Date Issue**: Application now uses current date for UI calculations if left running over midnight.
- **AutoRun Access Denied**: Resolved issue by correctly constructing full paths for AutoRun file operations.

---

## [1.6.4] - 2025-04-29

### Added
- **HelpForm**: Replaced help message box with a dedicated, resizable `HelpForm` with RTF support and theme awareness.
- **Report Archiving**: Automatic archiving for old report files on startup:
    - Final Reports: Archives previous year folders into a central `Archive` folder.
    - Raw Reports: Archives files older than configurable days into `Archive\YYYY-MM` subfolders.
- **Help Text**: Updated to include automated features.

### Changed
- **Refactoring**: Removed redundant file cleanup/archiving from `CrystalReportWrapper`.

### Fixed
- **HelpForm Theming**: Passed dark mode setting to `HelpForm`.

---

## [1.6.3] - 2025-04-29

### Added
- **Log Archiving**: Automatic archiving for old log files on startup (older than 30 days to `Logs\[User]\Archive\YYYY\MM\WeekN`).

### Fixed
- **Folder Creation**: Corrected logic for Quarterly reports in `FolderCreation.cs` to include quarter subfolder.
- **Path Generation**: Ensured `FolderCreation.cs` and `ExcelCopyData.cs` use `reportDate` for consistent folder paths.

---

## [1.6.2] - 2025-04-29

### Changed
- **Logging**:
    - Added configurable minimum logging level via `appsettings.json`.
    - Updated `Logger.cs` to read and apply configured log level.
    - Refined logging levels in `ExcelCopyData.cs` and `Logger.cs`.
    - Replaced most `Debug.WriteLine` in `Logger.cs` with level-based logging.

---

## [1.6.1] - 2025-04-29

### Added
- **"Custom" Report Type**: Automatically selected on manual date changes.
- **Custom Report Structure**: Specific folder (`Custom Reports\YYYY\YYYY-MM-DD_HHMMSS`) and filename (`{EndDate}_{Timestamp}_Estimate_Success_Rate_Custom.xlsx`) format.
- **Custom Report Email**: Distinct email subject/body.
- **Trace Logging**: Added `Trace` level to `Logger` (DEBUG builds only).

### Changed
- **Refactoring**: Consolidated folder creation into static `FolderCreation` class.

### Fixed
- **AutoRun File Lock**: Resolved issue by reading email attachment to memory stream.
- **Debug Email Recipients**: Corrected "Send to Femi Only" logic for DEBUG mode.
- **MessageBox Focus**: Ensured `FlexibleMessageBox` (manual refresh prompt) appears in front.
- **Status Messages**: Improved clarity for report creation and manual refresh.
- **Folder Creation**: Corrected Monthly/Quarterly report folder logic.
- **Help Text**: Restored missing content.

---

## [1.6.0] - 2025-04-29

### Changed
- **Refactoring**:
    - Decomposed `Form1.cs` into `UIManager`, `ReportProcessManager`, `NamedPipeCommunicator`, `AutoRunManager`.
    - Created static `ReportHelper` for utilities.
    - Changed `ExcelCopyData` to a non-static class.

### Fixed
- **Refactoring Bugs**: Resolved initial issues from refactoring (method calls, protection levels, `IProgress<T>` mismatches).

---

## [1.5.0] - 2025-04-28

### Changed
- **FlexibleMessageBox.cs**: Refactored to use latest C# features.

### Fixed
- **Rich Text Display**: Corrected issue where Rich Text was not showing in `FlexibleMessageBox`.

---

## [1.4.10] - 2025-04-27

### Fixed
- **UI State Management**:
    - Auto-Run toggle button remains enabled during manual report operations.
    - Create/Process buttons correctly reset enabled state after completion.
    - View Report/Analysis buttons visibility/enabled state correctly maintained.
    - Main status label reliably resets to "Ready".
- **Code Cleanup**: Removed unused `using` statements. Added `Microsoft.Win32`.

### Changed
- **UI**: Integrated `FlexibleMessageBox` for user messages.

---

## [1.4.9] - 2025-04-27

### Fixed
- **UI State**: Corrected UI state issues after manual report runs.
- **Status Label**: Main status label now resets to "Ready" more reliably.

### Changed
- **UI**: Integrated `FlexibleMessageBox`.
- **Code Cleanup**: Removed unused `using` statements. Added `Microsoft.Win32`.

---

## [1.4.8] - 2025-04-27

### Changed
- **AutoRun**: Reverted automated run check hour to 8 AM.

### Fixed
- **AutoRun Status**: Improved display logic.
- **Status Label**: Fixed main status label reset.

---

## [1.4.7] - 2025-04-27

### Changed
- **AutoRun**: Reverted AutoRun check hour to 8 AM.

### Fixed
- **AutoRun Status**: Corrected display logic.
- **Status Label**: Ensured main status label resets correctly.

---

## [1.4.6] - 2025-04-27

### Changed
- **UI**:
    - Adjusted dark mode `CheckBox` background.
    - Added Slicer refresh tip to Help.
    - Added "AUTOMATED:" prefix to email subjects.
    - Updated button text.

---

## [1.4.5] - 2025-04-26

### Fixed
- **UI**: Improved `CheckBox` visibility in dark mode.
- **Theming**: Updated `UpdateControlColors`.

---

## [1.4.4] - 2025-04-26

### Changed
- **AutoRun**: Modified `dailyCheckTimer_Tick` logic for continuous running.
- **State Management**: Added flags for daily check status.

---

## [1.4.3] - 2025-04-26

### Fixed
- **AutoRun**: Modified `dailyCheckTimer_Tick` logic to stop timer after daily completion. *(Superseded)*

---

## [1.4.2] - 2025-04-26

### Fixed
- **ExcelCopyData**: Corrected method calls in `Form1.cs`.

---

## [1.4.1] - 2025-04-26

### Fixed
- **AutoRun**:
    - Corrected `dailyCheckTimer_Tick` restart logic.
    - Ensured `toggleAutoRunButton` re-enables correctly.

---

## [1.4.0] - 2025-04-25

### Changed
- **Configuration**:
    - Modified `appsettings.json` handling for `LastRunDate`.
    - Updated configuration reads to use `IConfiguration`.

---

## [1.3.9] - 2025-04-25

### Added
- **AutoRun**:
    - Implemented `LastRunDate` read/save.
    - UI control disabling during AutoRun.

### Fixed
- **AutoRun**: Corrected time check.
- **Configuration**: Improved `appsettings.json` error handling.

---

## [1.3.8] - 2025-04-25

### Added
- **UI**:
    - `MenuStrip` with "Options" and "Help".
    - Moved Dark Mode to menu.

### Fixed
- **AutoRun**: Corrected status label updates.
- **UI**:
    - Fixed `SafeControlUpdate` for `ToolStripStatusLabel`.
    - Improved status reporting clarity.
    - Corrected Auto-Run button color logic.
- **Status Label**: Ensured reset.


---

## [1.3.6] - 2025-04-24

### Added
- **AutoRun**: Implemented feature.
- **Email**: Auto-run emails only Paul for Daily reports.

---

## [1.3.5] - 2025-04-24

### Added
- **"Daily" Report Type**: Implemented.
- **Folder Structure**: Specific structure for Daily reports.
- **Email Rule**: Special rule for Daily reports (Release mode).
- **Dark Mode**: Added toggle.

### Fixed
- **Excel**: Resolved file corruption issue.
- **Compilation**: Fixed ambiguous reference errors.
- **Email**: Corrected date format string.
- **Date Calculation**: Corrected weekly date range.
- **Security**: Removed DPAPI encryption.
- **ExcelCopyData**: Fixed `CopyAnalysisDataToWeeklyReportAsync`.
- **Analysis Sheet**: Corrected filename population.

### Changed
- **UI**:
    - Added label for Daily report email recipient.
    - Dynamic visibility for "Femi Only" checkbox.

---

## [1.2.5] - 2025-04-24

### Changed
- **Refactoring & Modernization**:
    - Refactored to .NET 8 and latest C# features.
    - Moved email client variables to `appsettings.json`.
    - Improved performance, especially Excel row deletion.

---

## [1.1.1] - 2025-04-17

### Added
- **Log Archiving**: Implemented.

### Fixed
- **Excel**: Corrected bug with creating new sheets in weekly Power BI source.
- **UI**:
    - Fixed `startDatePicker` re-enabling.
    - Fixed "View Analysis" button.

### Changed
- **Refactoring**:
    - Modularized code base.
    - Moved Email client variables to `App.config`.

---

## [1.1.0] - 2025-04-16

### Added
- **Report Types**: Selection (Weekly, Monthly, etc.).
- **Financial Year**: Picking implemented.
- **File Structure**: Automatic file/folder creation.
- **"Send to Femi Only"**: Option added.
- **File Check**: Check for existing final report file.
- **File Lock**: Retry logic for locked files.
- **Power BI**: Automatic FY sheet creation in source.
- **Email**: Dynamic text.
- **Skip Email**: Option added.

### Fixed
- **Data Copy**: Corrected bug skipping row 2 in source data.
- **Email**: Fixed date formatting.

---

## [1.0.5] - 2025-04-11

### Added
- **Excel**: Checks for running Excel processes.
- **Email**: Prompt to send email after processing.

### Changed
- **Performance & Refactoring**: Improvements.

---

## [1.0.4] - 2025-04-11

### Changed
- **Performance**: Excel data copying now uses `Range.Copy`.

### Fixed
- **Email**: Corrected sending logic bugs.

---

## [1.0.3] - 2025-04-02

### Added
- **Report Type**: Options to run report monthly. *(Superseded)*

---

## [1.0.2] - 2025-04-02

### Fixed
- **Excel**: Corrected issues with async data copying.

---

## [1.0.1] - 2025-04-01

### Added
- **UI**: Status tracking via status bar.

### Changed
- **Performance**: Made operations asynchronous.

---

## [1.0.0] - 2025-04-01

### Added
- **Initial Release**: Automates weekly estimates report creation and emailing.
