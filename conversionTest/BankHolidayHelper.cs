using System;
using System.Collections.Generic;
using System.Linq;

namespace QuoteConversionReportAutomation // Or your appropriate namespace
{
    /// <summary>
    /// Provides functionality to calculate and check for England bank holidays.
    /// Note: This provides a basic implementation. For long-term accuracy,
    /// consider using a dedicated library or external data source, as bank holidays
    /// can be changed by proclamation (e.g., for jubilees, state funerals).
    /// </summary>
    public static class BankHolidayHelper
    {
        // Cache calculated holidays per year to avoid recalculation
        private static readonly Dictionary<int, HashSet<DateTime>> s_bankHolidayCache = new Dictionary<int, HashSet<DateTime>>();
        private static readonly object s_cacheLock = new object();

        /// <summary>
        /// Checks if a given date is an England bank holiday for that year.
        /// </summary>
        /// <param name="date">The date to check.</param>
        /// <returns>True if the date is an England bank holiday, false otherwise.</returns>
        public static bool IsBankHoliday(DateTime date)
        {
            int year = date.Year;
            HashSet<DateTime> holidays;

            lock (s_cacheLock)
            {
                if (!s_bankHolidayCache.TryGetValue(year, out holidays))
                {
                    holidays = CalculateEnglandBankHolidays(year);
                    s_bankHolidayCache[year] = holidays;
                }
            }

            return holidays.Contains(date.Date); // Compare only the date part
        }

        /// <summary>
        /// Calculates all England bank holidays for a specific year.
        /// </summary>
        /// <param name="year">The year for which to calculate bank holidays.</param>
        /// <returns>A HashSet containing the dates of all England bank holidays for the year.</returns>
        private static HashSet<DateTime> CalculateEnglandBankHolidays(int year)
        {
            HashSet<DateTime> holidays = new HashSet<DateTime>();

            // --- Fixed Dates (with adjustments for weekends) ---

            // New Year's Day (1st Jan or substitute Monday/Tuesday)
            DateTime newYearsDay = new DateTime(year, 1, 1);
            holidays.Add(GetSubstituteDay(newYearsDay));

            // May Day Bank Holiday (First Monday in May)
            DateTime mayDay = new DateTime(year, 5, 1);
            while (mayDay.DayOfWeek != DayOfWeek.Monday)
            {
                mayDay = mayDay.AddDays(1);
            }
            holidays.Add(mayDay);

            // Spring Bank Holiday (Last Monday in May)
            DateTime springHoliday = new DateTime(year, 5, 31);
            while (springHoliday.DayOfWeek != DayOfWeek.Monday)
            {
                springHoliday = springHoliday.AddDays(-1);
            }
            holidays.Add(springHoliday);

            // Summer Bank Holiday (Last Monday in August)
            DateTime summerHoliday = new DateTime(year, 8, 31);
            while (summerHoliday.DayOfWeek != DayOfWeek.Monday)
            {
                summerHoliday = summerHoliday.AddDays(-1);
            }
            holidays.Add(summerHoliday);

            // Christmas Day (25th Dec or substitute Monday/Tuesday)
            DateTime christmasDay = new DateTime(year, 12, 25);
            holidays.Add(GetSubstituteDay(christmasDay));

            // Boxing Day (26th Dec or substitute Monday/Tuesday/Wednesday)
            DateTime boxingDay = new DateTime(year, 12, 26);
            holidays.Add(GetSubstituteDay(boxingDay, christmasDay.DayOfWeek)); // Pass Christmas DayOfWeek for correct Boxing Day substitute

            // --- Easter Dependent Dates ---
            DateTime easterSunday = CalculateEasterSunday(year);
            DateTime goodFriday = easterSunday.AddDays(-2);
            DateTime easterMonday = easterSunday.AddDays(1);

            holidays.Add(goodFriday);
            holidays.Add(easterMonday);

            // --- Special Holidays (Add manually if needed for specific years) ---
            // Example: Platinum Jubilee 2022
            // if (year == 2022) {
            //     holidays.Add(new DateTime(2022, 6, 2)); // Spring bank holiday moved
            //     holidays.Add(new DateTime(2022, 6, 3)); // Platinum Jubilee bank holiday
            //     holidays.Remove(new DateTime(2022, 5, 30)); // Remove original Spring date if calculated above
            // }
            // Example: Queen's Funeral 2022
            // if (year == 2022) {
            //     holidays.Add(new DateTime(2022, 9, 19));
            // }
            // Example: King's Coronation 2023
            // if (year == 2023) {
            //     holidays.Add(new DateTime(2023, 5, 8));
            // }


            return holidays;
        }

        /// <summary>
        /// Calculates the substitute day for a bank holiday that falls on a weekend.
        /// </summary>
        /// <param name="holiday">The original date of the holiday.</param>
        /// <param name="christmasDayOfWeek">Optional: The DayOfWeek of Christmas Day, needed for correct Boxing Day calculation.</param>
        /// <returns>The actual bank holiday date (original or substitute).</returns>
        private static DateTime GetSubstituteDay(DateTime holiday, DayOfWeek? christmasDayOfWeek = null)
        {
            switch (holiday.DayOfWeek)
            {
                case DayOfWeek.Saturday:
                    return holiday.AddDays(2); // Substitute on Monday
                case DayOfWeek.Sunday:
                    // Special handling for Boxing Day if Christmas was Sunday
                    if (holiday.Month == 12 && holiday.Day == 26 && christmasDayOfWeek == DayOfWeek.Sunday)
                    {
                        return holiday.AddDays(2); // Substitute on Tuesday
                    }
                    return holiday.AddDays(1); // Substitute on Monday
                default:
                    return holiday; // Holiday is on a weekday
            }
        }


        /// <summary>
        /// Calculates Easter Sunday for a given year using the Anonymous Gregorian algorithm.
        /// </summary>
        /// <param name="year">The year.</param>
        /// <returns>The date of Easter Sunday.</returns>
        private static DateTime CalculateEasterSunday(int year)
        {
            // Anonymous Gregorian algorithm
            int a = year % 19;
            int b = year / 100;
            int c = year % 100;
            int d = b / 4;
            int e = b % 4;
            int f = (b + 8) / 25;
            int g = (b - f + 1) / 3;
            int h = (19 * a + b - d - g + 15) % 30;
            int i = c / 4;
            int k = c % 4;
            int l = (32 + 2 * e + 2 * i - h - k) % 7;
            int m = (a + 11 * h + 22 * l) / 451;
            int month = (h + l - 7 * m + 114) / 31;
            int day = ((h + l - 7 * m + 114) % 31) + 1;

            return new DateTime(year, month, day);
        }
    }
}