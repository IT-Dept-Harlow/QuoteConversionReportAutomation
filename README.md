# Quote conversion application, automates the running of the Weekly and Monthly reports, 

## ChangeLog

### **Version 1.0.5**

* Added checks for excel being open and ways to kill the process, since excel must be closed.
* Added prompt asking if email needs to be sent.
* Refactored the code, to increase performance and modularity.

### **Version 1.0.4**

* Changed copying function to use range.copy to increase performance.
* Fixed bugs with the email logic.

### **Version 1.0.3**
* Added options to run the report monthly.

## **Version 1.0.2**
*Fixed problems caused by making program async, issues with data copying.

### **Version 1.0.1**
* made async and added status tracking

### **Version 1.0.0**

* 1st production version of the program, automates the creation of the weekly estimates report, using templates, then sends email to directors.
