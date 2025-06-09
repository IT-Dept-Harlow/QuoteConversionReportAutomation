using QuoteConversionReportAutomation.Services.Logging;
using System.Text.Json;

namespace QuoteConversionReportAutomation.Helpers
{
    /// <summary>
    /// Represents a one-off custom bank holiday.
    /// </summary>
    public class CustomHolidayEntry
    {
        public DateTime Date { get; set; }
        public string Description { get; set; } = string.Empty;
    }

    /// <summary>
    /// Represents a recurring custom bank holiday (e.g., always on a specific day/month).
    /// </summary>
    public class RecurringHolidayEntry
    {
        public int Day { get; set; }
        public int Month { get; set; }
        public string Description { get; set; } = string.Empty;
    }

    /// <summary>
    /// Container for custom holiday data to be serialized/deserialized.
    /// </summary>
    public class CustomHolidaysData
    {
        public List<CustomHolidayEntry> OneOffHolidays { get; set; } = new List<CustomHolidayEntry>();
        public List<RecurringHolidayEntry> RecurringHolidays { get; set; } = new List<RecurringHolidayEntry>();
    }

    /// <summary>
    /// Provides functionality to calculate and check for England and Wales bank holidays,
    /// including support for custom and recurring custom bank holidays loaded from/saved to a file.
    /// </summary>
    public static class BankHolidayHelper
    {
        // Cache calculated holidays per year to avoid recalculation
        private static readonly Dictionary<int, HashSet<DateTime>> s_bankHolidayCache = new Dictionary<int, HashSet<DateTime>>();
        private static readonly object s_cacheLock = new object();

        // In-memory lists for custom holidays, loaded from/saved to file
        private static List<CustomHolidayEntry> s_customOneOffHolidays = new List<CustomHolidayEntry>();
        private static List<RecurringHolidayEntry> s_customRecurringHolidays = new List<RecurringHolidayEntry>();
        private static bool s_customHolidaysLoaded = false;
        //private static readonly string s_customHolidaysFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "custom_bank_holidays.json");
        // Define path for user-specific settings
        private static string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        private static string companyFolder = "HarlowSolutions";
        private static string appFolder = "QuoteConversionReportAutomation";
        private static readonly string s_customHolidaysFilePath = Path.Combine(appDataPath, companyFolder, appFolder, "custom_bank_holidays.json");

        /// <summary>
        /// Initializes the BankHolidayHelper by loading custom bank holidays from the persistent store.
        /// This should be called once at application startup.
        /// </summary>
        public static void Initialize()
        {
            LoadCustomBankHolidays();
        }

        /// <summary>
        /// Clears the internal bank holiday cache. 
        /// This method should be called after any modifications to custom bank holidays (add/remove)
        /// to ensure that subsequent calls to IsBankHoliday reflect the changes.
        /// </summary>
        public static void ClearCache()
        {
            lock (s_cacheLock)
            {
                s_bankHolidayCache.Clear();
                Logger.LogInfo("Bank holiday cache cleared due to custom holiday modification.");
            }
        }

        /// <summary>
        /// Adds a one-off custom bank holiday to the persistent store and in-memory list.
        /// </summary>
        /// <param name="date">The date of the custom bank holiday. The time component is ignored.</param>
        /// <param name="description">A description for the holiday.</param>
        /// <returns>True if the holiday was added successfully, false if a holiday for that date already exists.</returns>
        public static bool AddCustomBankHoliday(DateTime date, string description)
        {
            ArgumentException.ThrowIfNullOrEmpty(description, nameof(description));
            EnsureCustomHolidaysLoaded();

            DateTime dateOnly = date.Date;
            if (!s_customOneOffHolidays.Any(h => h.Date == dateOnly))
            {
                s_customOneOffHolidays.Add(new CustomHolidayEntry { Date = dateOnly, Description = description });
                SaveCustomBankHolidays();
                ClearCache();
                Logger.LogInfo($"Added custom one-off bank holiday: {dateOnly:yyyy-MM-dd} - {description}");
                return true;
            }
            else
            {
                Logger.LogWarning($"Custom one-off bank holiday for {dateOnly:yyyy-MM-dd} already exists. Not added.");
                return false;
            }
        }

        /// <summary>
        /// Removes a one-off custom bank holiday based on its date.
        /// </summary>
        /// <param name="date">The date of the one-off holiday to remove.</param>
        /// <returns>True if a holiday was found and removed, false otherwise.</returns>
        public static bool RemoveCustomOneOffHoliday(DateTime date)
        {
            EnsureCustomHolidaysLoaded();
            DateTime dateOnly = date.Date;
            int removedCount = s_customOneOffHolidays.RemoveAll(h => h.Date == dateOnly);
            if (removedCount > 0)
            {
                SaveCustomBankHolidays();
                ClearCache();
                Logger.LogInfo($"Removed {removedCount} custom one-off bank holiday(s) for date: {dateOnly:yyyy-MM-dd}");
                return true;
            }
            Logger.LogWarning($"No custom one-off bank holiday found for date: {dateOnly:yyyy-MM-dd} to remove.");
            return false;
        }


        /// <summary>
        /// Adds a recurring custom bank holiday (same day/month each year) to the persistent store and in-memory list.
        /// </summary>
        /// <param name="day">The day of the month (1-31).</param>
        /// <param name="month">The month (1-12).</param>
        /// <param name="description">A description for the holiday.</param>
        /// <returns>True if the holiday was added successfully, false if a recurring holiday for that day/month already exists.</returns>
        public static bool AddRecurringCustomBankHoliday(int day, int month, string description)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(day, 1, nameof(day));
            ArgumentOutOfRangeException.ThrowIfGreaterThan(day, 31, nameof(day)); // Basic validation, specific month day counts checked during calculation
            ArgumentOutOfRangeException.ThrowIfLessThan(month, 1, nameof(month));
            ArgumentOutOfRangeException.ThrowIfGreaterThan(month, 12, nameof(month));
            ArgumentException.ThrowIfNullOrEmpty(description, nameof(description));
            EnsureCustomHolidaysLoaded();

            if (!s_customRecurringHolidays.Any(h => h.Day == day && h.Month == month))
            {
                s_customRecurringHolidays.Add(new RecurringHolidayEntry { Day = day, Month = month, Description = description });
                SaveCustomBankHolidays();
                ClearCache();
                Logger.LogInfo($"Added custom recurring bank holiday: {day:D2}/{month:D2} - {description}");
                return true;
            }
            else
            {
                Logger.LogWarning($"Custom recurring bank holiday for {day:D2}/{month:D2} already exists. Not added.");
                return false;
            }
        }

        /// <summary>
        /// Removes a recurring custom bank holiday based on its day and month.
        /// </summary>
        /// <param name="day">The day of the recurring holiday.</param>
        /// <param name="month">The month of the recurring holiday.</param>
        /// <returns>True if a holiday was found and removed, false otherwise.</returns>
        public static bool RemoveCustomRecurringHoliday(int day, int month)
        {
            EnsureCustomHolidaysLoaded();
            int removedCount = s_customRecurringHolidays.RemoveAll(h => h.Day == day && h.Month == month);
            if (removedCount > 0)
            {
                SaveCustomBankHolidays();
                ClearCache();
                Logger.LogInfo($"Removed {removedCount} custom recurring bank holiday(s) for day/month: {day:D2}/{month:D2}");
                return true;
            }
            Logger.LogWarning($"No custom recurring bank holiday found for day/month: {day:D2}/{month:D2} to remove.");
            return false;
        }

        /// <summary>
        /// Gets a copy of the current list of one-off custom bank holidays.
        /// </summary>
        /// <returns>A new list containing <see cref="CustomHolidayEntry"/> objects.</returns>
        public static List<CustomHolidayEntry> GetCustomOneOffHolidays()
        {
            EnsureCustomHolidaysLoaded();
            return new List<CustomHolidayEntry>(s_customOneOffHolidays);
        }

        /// <summary>
        /// Gets a copy of the current list of recurring custom bank holidays.
        /// </summary>
        /// <returns>A new list containing <see cref="RecurringHolidayEntry"/> objects.</returns>
        public static List<RecurringHolidayEntry> GetCustomRecurringHolidays()
        {
            EnsureCustomHolidaysLoaded();
            return new List<RecurringHolidayEntry>(s_customRecurringHolidays);
        }

        /// <summary>
        /// Loads custom bank holidays from the JSON file. This is called internally when needed.
        /// </summary>
        private static void LoadCustomBankHolidays()
        {
            lock (s_cacheLock)
            {
                if (s_customHolidaysLoaded) return; // Already loaded

                try
                {
                    if (File.Exists(s_customHolidaysFilePath))
                    {
                        string json = File.ReadAllText(s_customHolidaysFilePath);
                        var data = JsonSerializer.Deserialize<CustomHolidaysData>(json);
                        if (data != null)
                        {
                            s_customOneOffHolidays = data.OneOffHolidays ?? new List<CustomHolidayEntry>();
                            s_customRecurringHolidays = data.RecurringHolidays ?? new List<RecurringHolidayEntry>();
                            Logger.LogInfo($"Loaded {s_customOneOffHolidays.Count} one-off and {s_customRecurringHolidays.Count} recurring custom bank holidays from '{s_customHolidaysFilePath}'.");
                        }
                        else
                        {
                            Logger.LogWarning($"Deserialized custom holiday data was null from '{s_customHolidaysFilePath}'. Initializing empty lists.");
                            s_customOneOffHolidays = new List<CustomHolidayEntry>();
                            s_customRecurringHolidays = new List<RecurringHolidayEntry>();
                        }
                    }
                    else
                    {
                        Logger.LogInfo($"Custom bank holiday file not found ('{s_customHolidaysFilePath}'). No custom holidays loaded. An empty file will be created if custom holidays are added.");
                        s_customOneOffHolidays = new List<CustomHolidayEntry>();
                        s_customRecurringHolidays = new List<RecurringHolidayEntry>();
                    }
                }
                catch (JsonException jsonEx)
                {
                    Logger.LogError($"Error deserializing custom bank holidays JSON from '{s_customHolidaysFilePath}': {jsonEx.Message}. Check file format.");
                    s_customOneOffHolidays = new List<CustomHolidayEntry>();
                    s_customRecurringHolidays = new List<RecurringHolidayEntry>();
                }
                catch (Exception ex)
                {
                    Logger.LogError($"Error loading custom bank holidays from '{s_customHolidaysFilePath}': {ex.Message}");
                    s_customOneOffHolidays = new List<CustomHolidayEntry>();
                    s_customRecurringHolidays = new List<RecurringHolidayEntry>();
                }
                s_customHolidaysLoaded = true;
            }
        }

        /// <summary>
        /// Saves the current custom bank holidays (both one-off and recurring) to the JSON file.
        /// </summary>
        private static void SaveCustomBankHolidays()
        {
            lock (s_cacheLock)
            {
                try
                {
                    var data = new CustomHolidaysData
                    {
                        OneOffHolidays = s_customOneOffHolidays,
                        RecurringHolidays = s_customRecurringHolidays
                    };
                    string json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
                    File.WriteAllText(s_customHolidaysFilePath, json);
                    Logger.LogInfo($"Saved custom bank holidays to '{s_customHolidaysFilePath}'.");
                }
                catch (Exception ex)
                {
                    Logger.LogError($"Error saving custom bank holidays to '{s_customHolidaysFilePath}': {ex.Message}");
                    // Consider how to handle save failures (e.g., notify user if critical)
                }
            }
        }

        /// <summary>
        /// Ensures custom holidays are loaded if they haven't been already.
        /// </summary>
        private static void EnsureCustomHolidaysLoaded()
        {
            // Double-check locking pattern for thread-safe lazy initialization
            if (!s_customHolidaysLoaded)
            {
                lock (s_cacheLock)
                {
                    if (!s_customHolidaysLoaded)
                    {
                        LoadCustomBankHolidays();
                    }
                }
            }
        }

        /// <summary>
        /// Checks if a given date is an England or Wales bank holiday for that year, including custom holidays.
        /// </summary>
        /// <param name="date">The date to check.</param>
        /// <returns>True if the date is a bank holiday, false otherwise.</returns>
        public static bool IsBankHoliday(DateTime date)
        {
            EnsureCustomHolidaysLoaded();
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
            return holidays.Contains(date.Date);
        }

        /// <summary>
        /// Calculates all England and Wales bank holidays for a given year, including custom ones.
        /// </summary>
        /// <param name="year">The year for which to calculate bank holidays.</param>
        /// <returns>A HashSet of DateTime objects representing the bank holidays.</returns>
        private static HashSet<DateTime> CalculateEnglandBankHolidays(int year)
        {
            // EnsureCustomHolidaysLoaded() is called by IsBankHoliday before this,
            // or should be called here if this method can be accessed independently.
            // For safety, let's ensure it here too.
            EnsureCustomHolidaysLoaded();
            var holidays = new HashSet<DateTime>();

            // --- Standard Bank Holidays ---
            holidays.Add(SubstituteWeekendHoliday(new DateTime(year, 1, 1))); // New Year's Day
            DateTime easterSunday = CalculateEasterSunday(year);
            holidays.Add(easterSunday.AddDays(-2)); // Good Friday
            holidays.Add(easterSunday.AddDays(1));  // Easter Monday
            holidays.Add(GetNthMondayInMonth(year, 5, 1)); // Early May Bank Holiday
            holidays.Add(GetLastMondayInMonth(year, 5));   // Spring Bank Holiday
            holidays.Add(GetLastMondayInMonth(year, 8));   // Summer Bank Holiday
            holidays.Add(SubstituteWeekendHoliday(new DateTime(year, 12, 25))); // Christmas Day
            holidays.Add(SubstituteWeekendHoliday(new DateTime(year, 12, 26), new DateTime(year, 12, 25))); // Boxing Day

            // --- Add Custom One-Off Holidays for the given year ---
            foreach (var customHoliday in s_customOneOffHolidays)
            {
                if (customHoliday.Date.Year == year)
                {
                    holidays.Add(customHoliday.Date.Date);
                }
            }

            // --- Add Custom Recurring Holidays for the given year ---
            foreach (var recurringHoliday in s_customRecurringHolidays)
            {
                try
                {
                    if (recurringHoliday.Day <= DateTime.DaysInMonth(year, recurringHoliday.Month))
                    {
                        holidays.Add(SubstituteWeekendHoliday(new DateTime(year, recurringHoliday.Month, recurringHoliday.Day)));
                    }
                }
                catch (ArgumentOutOfRangeException)
                {
                    Logger.LogWarning($"Invalid day/month for recurring holiday: {recurringHoliday.Day}/{recurringHoliday.Month} in year {year}. Description: {recurringHoliday.Description}");
                }
            }

            Logger.LogDebug($"Calculated {holidays.Count} bank holidays for year {year}.");
            return holidays;
        }

        /// <summary>
        /// Gets the Nth Monday in a specific month and year.
        /// </summary>
        private static DateTime GetNthMondayInMonth(int year, int month, int n)
        {
            DateTime firstDayOfMonth = new DateTime(year, month, 1);
            int daysToAdd = (DayOfWeek.Monday - firstDayOfMonth.DayOfWeek + 7) % 7;
            DateTime firstMonday = firstDayOfMonth.AddDays(daysToAdd);
            return firstMonday.AddDays((n - 1) * 7);
        }

        /// <summary>
        /// Gets the last Monday in a specific month and year.
        /// </summary>
        private static DateTime GetLastMondayInMonth(int year, int month)
        {
            DateTime lastDayOfMonth = new DateTime(year, month, DateTime.DaysInMonth(year, month));
            int daysToSubtract = (lastDayOfMonth.DayOfWeek - DayOfWeek.Monday + 7) % 7;
            return lastDayOfMonth.AddDays(-daysToSubtract);
        }

        /// <summary>
        /// Adjusts a holiday if it falls on a weekend.
        /// </summary>
        private static DateTime SubstituteWeekendHoliday(DateTime holiday, DateTime? christmasDay = null)
        {
            DayOfWeek dayOfWeek = holiday.DayOfWeek;
            DayOfWeek christmasDayOfWeek = christmasDay?.DayOfWeek ?? DayOfWeek.Wednesday;

            switch (dayOfWeek)
            {
                case DayOfWeek.Saturday:
                    return holiday.AddDays(2);
                case DayOfWeek.Sunday:
                    if (holiday.Month == 12 && holiday.Day == 26 && christmasDayOfWeek == DayOfWeek.Sunday)
                    {
                        return holiday.AddDays(2);
                    }
                    return holiday.AddDays(1);
                default:
                    return holiday;
            }
        }

        /// <summary>
        /// Calculates Easter Sunday for a given year using the Anonymous Gregorian algorithm.
        /// </summary>
        private static DateTime CalculateEasterSunday(int year)
        {
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
            int day = (h + l - 7 * m + 114) % 31 + 1;
            return new DateTime(year, month, day);
        }
    }
}
