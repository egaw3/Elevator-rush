using System;

namespace ElevatorRush
{
    /// <summary>A saved per-day use count (see <see cref="DailyCounter"/>). <see cref="day"/> is yyyymmdd.</summary>
    [Serializable]
    public sealed class DailyCount
    {
        public int day;
        public int count;
    }
}
