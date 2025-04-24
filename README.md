# Quote Conversion Application

Automates the running of the Daily, Weekly, Monthly, Quarterly, or Annual reports, processing the data, and sending the result via email.

## ChangeLog

### Version 1.3.5
* **New Features**
    * Added "Daily" report type option (uses Weekly template).
    * Implemented specific folder structure for Daily reports (`<Base Path>\Daily Reports\<Month Name>\<Month Name> Week <Num>\`).
    * Added special email rule: Daily reports in Release mode are sent only to Paul S.
    * Added Dark Mode toggle (`checkBox2DarkMode`) with theme application logic. Dark mode is now the default on startup.
* **Bug Fixes**
    * Resolved Excel file corruption potentially caused by EPPlus interaction with Excel Tables (solution: converted table to range in template).
    * Fixed ambiguous reference errors caused by duplicated helper methods in `Form1.cs`.
    * Corrected date format string in email body (`yyyy` instead of `pyrolysis`).
    * Corrected weekly date range calculation (now correctly covers 14 days).
    * Removed DPAPI encryption from `appsettings.json` to allow multi-user execution from a shared location. Configuration file must now be plain text JSON.
* **UI Improvements**
    * Added `label6` to indicate when Daily report email goes specifically to Paul.
    * Dynamically show/hide `checkBox1` (Femi Only) and `label6` based on whether the Daily report type is selected.

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
    * Fixed `datepickFrom` control not re-enabling correctly.
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
