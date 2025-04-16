# Quote conversion application, automates the running of the Weekly, Monthly, quaterly or annual reports, sending the result to the directors distribution list.

## ChangeLog

# **Version 1.1.1**

## New Features
*Added archiving for folders and old logs.

## Bug Fixes
* Fixed bug with creating new sheets in the weekly powerBI document
* Fixed date from control not re enabling
* Fixed and re-enabled the view file button for the analysis

## Other
* Refactored entire code base for more modularity and maintainability.
* Moved the email client variables to App.config

# **Version 1.1.0**

## New Features
* Added options allowing user to select either weekly, monthly, quaterly or annual reports.
* Added code to allow picking financial year, either current or previous, then select sheet in weekly BI document based on choice.
* Added logic to automatically create the files and folder structure for each report type.
* Added logic to automatically create folders for each year, and archive old year folders and files.
* Added option to send the email only to Femi, In case it needs approval before it's sent to the distribution list, or Femi asks for a custom date range.
* Added check to see if file already exists, this is in case femi wants to check it before it's sent, allows sending the same file that Femi got rather than recreating it, as that would be different data. 
* Added retry logic for files that are open.
* Added checks to add financial year sheet into the powerBI excel document, if it doesn't exist, copys row 1 from old sheet.
* Added logic to change email text based on report type and if sending to femi.
* Added Option to not send the email, in case the report needs changing

## Bug Fixes
* Fixed bug where it starts on skips row 2.
* Fixed logic in sendEmail where sometimes incorrect dates could be set.

# **Version 1.0.5**

## New Features
* Added checks for excel being open and ways to kill the process, since excel must be closed.
* Added prompt asking if email needs to be sent.

## Performance Fixes
* Refactored the code, to increase performance and modularity.

# **Version 1.0.4**

## Performance Fixes
* Changed copying function to use range.copy to increase performance.

## Bug Fixes
* Fixed bugs with the email logic.

# **Version 1.0.3**
## New Features
* Added options to run the report monthly.

# **Version 1.0.2**
## Bug Fixes
* Fixed problems caused by making program async, issues with data copying.

# **Version 1.0.1**
## New Features
* Added status tracking via status bar

## Performance Fixes
* made async for performance

# **Version 1.0.0**

* 1st release version of the program, automates the creation of the weekly estimates report, using templates, then sends email to directors.
