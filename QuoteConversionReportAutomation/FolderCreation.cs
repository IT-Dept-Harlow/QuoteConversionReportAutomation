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
        /// Creates a folder with a name based on the current month and week.
        /// </summary>
        /// <param name="basePath">The base path where the folder should be created. If null, the current directory is used.</param>
        /// <returns>The full path of the created folder, or an error message if creation fails.</returns>
        public string CreateFolder(string basePath = null)
        {
            DateTime now = DateTime.Now;
            string monthAbbreviation = now.ToString("MMM", CultureInfo.InvariantCulture);
            int weekInMonth = GetWeekOfMonth(now);
            string folderName = $"{monthAbbreviation} Week {weekInMonth}";
            string currentDirectory = basePath ?? Directory.GetCurrentDirectory();
            string folderPath = Path.Combine(currentDirectory, folderName);

            try
            {
                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                    Logger.LogInfo($"Folder created: {folderPath}");
                    return folderPath;
                }
                else
                {
                    Logger.LogInfo($"Folder already exists: {folderPath}");
                    return folderPath;
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error: {ex.Message}");
                return $"Error: {ex.Message}";
            }
        }

        /// <summary>
        /// Calculates the week of the month for a given date, using Monday as the first day of the week.
        /// </summary>
        /// <param name="date">The date for which to calculate the week of the month.</param>
        /// <returns>The week number of the month (1-5).</returns>
        private int GetWeekOfMonth(DateTime date)
        {
            CultureInfo culture = CultureInfo.CurrentCulture;
            DayOfWeek firstDayOfWeek = DayOfWeek.Monday; // Explicitly set to Monday

            DateTime firstDayOfMonth = new DateTime(date.Year, date.Month, 1);
            int firstDayOfMonthDayOfWeek = (int)firstDayOfMonth.DayOfWeek;

            // Adjust to Monday-based week
            int dayOfWeekAdjustment = (int)firstDayOfWeek - (int)culture.DateTimeFormat.FirstDayOfWeek;
            if (dayOfWeekAdjustment < 0)
            {
                dayOfWeekAdjustment += 7;
            }
            firstDayOfMonthDayOfWeek = (firstDayOfMonthDayOfWeek + dayOfWeekAdjustment) % 7;

            int dayOfMonth = date.Day;
            int weekOfMonth = (dayOfMonth + firstDayOfMonthDayOfWeek - 1) / 7 + 1;

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

            return weekOfMonth;
        }
    }
}