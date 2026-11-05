using System;

namespace ElevatorRush
{
    /// <summary>
    /// Pure per-day limits by the device's local date (monetization plan 4.1). The count starts
    /// again on a later date; a date earlier than the saved one (the clock was turned back) keeps
    /// counting against the saved day, so it cannot be used to reset the limit.
    /// </summary>
    public static class DailyCounter
    {
        /// <summary>A date as yyyymmdd, which orders the same way as the dates.</summary>
        public static int DayOf(DateTime date) => date.Year * 10000 + date.Month * 100 + date.Day;

        public static int Today => DayOf(DateTime.Now);

        public static int Used(DailyCount counter, int today) => today > counter.day ? 0 : counter.count;

        public static int Remaining(DailyCount counter, int today, int limit) => Math.Max(0, limit - Used(counter, today));

        /// <summary>Counts one use and returns true, or returns false when today's limit is reached.</summary>
        public static bool TryUse(DailyCount counter, int today, int limit)
        {
            if (today > counter.day)
            {
                counter.day = today;
                counter.count = 0;
            }
            if (counter.count >= limit)
                return false;
            counter.count++;
            return true;
        }
    }
}
