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
        /// Creates a folder with a name based on the report type.
        /// </summary>
        /// <param name="reportType">
        ///     <para>0 = Weekly: Creates a folder named "MMM Week W" (e.g., "Mar Week 2").</para>
        ///     <para>1 = Monthly: Creates a folder named "MMM YY" (e.g., "Mar 24").</para>
        ///     <para>2 = Quarterly: Creates a folder named "MMM to MMM" (e.g., "Jan to Mar").</para>
        ///     <para>3 = Annual: Creates a folder named "YYYY" (e.g., "2025").</para>
        /// </param>
        /// <param name="basePath">
        ///     The base path where the folder should be created.
        ///     If <c>null</c>, the current directory is used.
        /// </param>
        /// <returns>
        ///     The full path of the created folder, or an error message if creation fails.
        ///     Returns the full path of the created folder if successful.
        /// </returns>
        public string CreateFolder(int reportType, string basePath = null)
        {
            DateTime now = DateTime.Now;
            string folderName = "";
            string currentDirectory = basePath ?? Directory.GetCurrentDirectory();
            string folderPath = "";

            try
            {
                switch (reportType)
                {
                    case 0: // Weekly
                        string monthAbbreviation = now.ToString("MMM", CultureInfo.InvariantCulture);
                        int weekInMonth = GetWeekOfMonth(now);
                        folderName = $"{monthAbbreviation} Week {weekInMonth}";
                        break;
                    case 1: // Monthly
                        DateTime folderDate = now.Day <= 15 ? now.AddMonths(-1) : now;
                        folderName = folderDate.ToString("MMM yy", CultureInfo.InvariantCulture);
                        break;
                    case 2: // Quarterly
                        DateTime today = DateTime.Today;
                        int currentQuarter = (today.Month - 1) / 3 + 1;
                        int previousQuarter = currentQuarter - 1;
                        int previousQuarterYear = today.Year;

                        if (previousQuarter < 1)
                        {
                            previousQuarter = 4; // Wrap around to the 4th quarter of the previous year
                            previousQuarterYear--;
                        }

                        DateTime quarterStart = new DateTime(previousQuarterYear, (previousQuarter - 1) * 3 + 1, 1);
                        DateTime quarterEnd = quarterStart.AddMonths(3).AddDays(-1);

                        string startMonth = quarterStart.ToString("MMM");
                        string endMonth = quarterEnd.ToString("MMM");
                        folderName = $"{startMonth} to {endMonth}";
                        break;
                    case 3: // Annual
                        folderName = now.Year.ToString();
                        break;
                    default:
                        folderName = "Default";
                        break;
                }

                folderPath = Path.Combine(currentDirectory, folderName);

                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                    Logger.LogInfo($"Folder created: {folderPath}");
                }
                else
                {
                    Logger.LogInfo($"Folder already exists: {folderPath}");
                }
                return folderPath;
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error creating folder: {ex.Message}");
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
            DayOfWeek firstDayOfWeek = DayOfWeek.Monday;

            DateTime firstDayOfMonth = new DateTime(date.Year, date.Month, 1);
            int firstDayOfMonthDayOfWeek = (int)firstDayOfMonth.DayOfWeek;

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

        /// <summary>
        /// Gets the first day of the quarter for a given date.
        /// </summary>
        /// <param name="date">The date for which to find the first day of the quarter.</param>
        /// <returns>The first day of the quarter.</returns>
        private DateTime GetFirstDayOfQuarter(DateTime date)
        {
            int month = date.Month;
            int year = date.Year;

            if (month >= 1 && month <= 3)
            {
                return new DateTime(year, 1, 1);
            }
            else if (month >= 4 && month <= 6)
            {
                return new DateTime(year, 4, 1);
            }
            else if (month >= 7 && month <= 9)
            {
                return new DateTime(year, 7, 1);
            }
            else
            {
                return new DateTime(year, 10, 1);
            }
        }
    }
}