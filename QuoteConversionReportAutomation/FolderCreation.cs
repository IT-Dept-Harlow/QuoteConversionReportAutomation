using System;
using System.Globalization;
using System.IO;

namespace QuoteConversionReportAutomation
{
    /// <summary>
    /// Class for folder Creation
    /// </summary>
    public class FolderCreation
    {
        /// <summary>
        /// Creates a folder with a name based on the current month and week, or just the month.
        /// </summary>
        /// <param name="useMonthly">
        ///   <para>If <c>true</c>, creates a folder named "MMM YY" (e.g., "Mar 24").</para>
        ///   <para>If <c>false</c>, creates a folder named "MMM Week W" (e.g., "Mar Week 2").</para>
        /// </param>
        /// <param name="basePath">
        ///   The base path where the folder should be created.
        ///   If <c>null</c>, the current directory is used.
        /// </param>
        /// <returns>
        ///   The full path of the created folder, or an error message if creation fails.
        ///   Returns <c>null</c> if folder creation is successful, otherwise returns the error message.
        /// </returns>
        public string CreateFolder(bool useMonthly, string basePath = null)
        {
            DateTime now = DateTime.Now; // Get the current date and time.
            string folderName; // Declare a variable to store the folder name.

            if (useMonthly)
            {
                // Determine the month for the folder name.
                DateTime folderDate;
                if (now.Day <= 15) // Changed from now.Day < 15 to now.Day <=15
                {
                    folderDate = now.AddMonths(-1); // Go to the previous month.
                }
                else
                {
                    folderDate = now; // Use the current month.
                }
                folderName = folderDate.ToString("MMM yy", CultureInfo.InvariantCulture);
            }
            else
            {
                string monthAbbreviation = now.ToString("MMM", CultureInfo.InvariantCulture);
                int weekInMonth = GetWeekOfMonth(now);
                folderName = $"{monthAbbreviation} Week {weekInMonth}";
            }

            string currentDirectory = basePath ?? Directory.GetCurrentDirectory(); // Get the base path or current directory.
            string folderPath = Path.Combine(currentDirectory, folderName); // Combine the base path and folder name.

            try
            {
                // Attempt to create the directory.
                if (!Directory.Exists(folderPath)) // Check if the directory already exists.
                {
                    Directory.CreateDirectory(folderPath); // Create the directory.
                    Logger.LogInfo($"Folder created: {folderPath}"); // Log the creation.
                    return folderPath; // Return the full path of the created folder.
                }
                else
                {
                    Logger.LogInfo($"Folder already exists: {folderPath}"); // Log that the folder exists.
                    return folderPath; // Return the existing path.
                }
            }
            catch (Exception ex)
            {
                // Handle any exceptions that occur during directory creation.
                Logger.LogError($"Error: {ex.Message}"); // Log the error message.
                return $"Error: {ex.Message}"; // Return the error message.  Consider returning null and throwing an exception.
            }
        }

        /// <summary>
        /// Calculates the week of the month for a given date, using Monday as the first day of the week.
        /// </summary>
        /// <param name="date">The date for which to calculate the week of the month.</param>
        /// <returns>The week number of the month (1-5).</returns>
        private int GetWeekOfMonth(DateTime date)
        {
            CultureInfo culture = CultureInfo.CurrentCulture; // Get the current culture.
            DayOfWeek firstDayOfWeek = DayOfWeek.Monday; // Explicitly set the first day of the week to Monday.

            DateTime firstDayOfMonth = new DateTime(date.Year, date.Month, 1); // Get the first day of the month.
            int firstDayOfMonthDayOfWeek = (int)firstDayOfMonth.DayOfWeek; // Get the day of the week for the first day of the month.

            // Adjust to Monday-based week.  This ensures that the week calculation is correct
            // even if the system's default first day of the week is not Monday.
            int dayOfWeekAdjustment = (int)firstDayOfWeek - (int)culture.DateTimeFormat.FirstDayOfWeek;
            if (dayOfWeekAdjustment < 0)
            {
                dayOfWeekAdjustment += 7; // Ensure the adjustment is positive.
            }
            firstDayOfMonthDayOfWeek = (firstDayOfMonthDayOfWeek + dayOfWeekAdjustment) % 7; // Calculate the adjusted day of week.

            int dayOfMonth = date.Day; // Get the day of the month.
            int weekOfMonth = (dayOfMonth + firstDayOfMonthDayOfWeek - 1) / 7 + 1; // Calculate the week of the month.

            // Ensure weekInMonth does not exceed 5.
            int daysInMonth = DateTime.DaysInMonth(date.Year, date.Month);
            DateTime lastDayOfMonth = new DateTime(date.Year, date.Month, daysInMonth);
            int lastDayOfMonthDayOfWeek = (int)lastDayOfMonth.DayOfWeek;

            //Adjust last day of week to monday start.
            lastDayOfMonthDayOfWeek = (lastDayOfMonthDayOfWeek + dayOfWeekAdjustment) % 7;

            if ((daysInMonth + firstDayOfMonthDayOfWeek - 1) / 7 + 1 > 5)
            {
                if (weekOfMonth > 5)
                {
                    weekOfMonth = 5;
                }
            }
            return weekOfMonth; // Return the week of the month.
        }
    }
}
