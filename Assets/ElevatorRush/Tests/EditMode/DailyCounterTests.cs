using System;
using NUnit.Framework;

namespace ElevatorRush.Tests
{
    public class DailyCounterTests
    {
        const int Limit = 5;
        const int Day = 20261006;

        [Test]
        public void CountsUpToTheLimit()
        {
            var counter = new DailyCount();
            for (int i = 0; i < Limit; i++)
            {
                Assert.AreEqual(Limit - i, DailyCounter.Remaining(counter, Day, Limit));
                Assert.IsTrue(DailyCounter.TryUse(counter, Day, Limit));
            }
            Assert.IsFalse(DailyCounter.TryUse(counter, Day, Limit));
            Assert.AreEqual(0, DailyCounter.Remaining(counter, Day, Limit));
            Assert.AreEqual(Limit, counter.count);
        }

        [Test]
        public void ResetsOnANewDate()
        {
            var counter = new DailyCount();
            for (int i = 0; i < Limit; i++)
                DailyCounter.TryUse(counter, Day, Limit);

            Assert.AreEqual(Limit, DailyCounter.Remaining(counter, Day + 1, Limit));
            Assert.IsTrue(DailyCounter.TryUse(counter, Day + 1, Limit));
            Assert.AreEqual(1, DailyCounter.Used(counter, Day + 1));
            Assert.AreEqual(Day + 1, counter.day);
        }

        [Test]
        public void DoesNotResetWhenTheDateGoesBackwards()
        {
            var counter = new DailyCount();
            for (int i = 0; i < Limit; i++)
                DailyCounter.TryUse(counter, Day, Limit);

            Assert.AreEqual(0, DailyCounter.Remaining(counter, Day - 1, Limit));
            Assert.IsFalse(DailyCounter.TryUse(counter, Day - 1, Limit));
            Assert.AreEqual(Day, counter.day);
            Assert.AreEqual(Limit, counter.count);
        }

        [Test]
        public void DaysOrderLikeDates()
        {
            Assert.AreEqual(20261006, DailyCounter.DayOf(new DateTime(2026, 10, 6, 23, 59, 0)));
            Assert.Less(DailyCounter.DayOf(new DateTime(2026, 12, 31)), DailyCounter.DayOf(new DateTime(2027, 1, 1)));
            Assert.Less(DailyCounter.DayOf(new DateTime(2026, 1, 31)), DailyCounter.DayOf(new DateTime(2026, 2, 1)));
        }
    }
}
