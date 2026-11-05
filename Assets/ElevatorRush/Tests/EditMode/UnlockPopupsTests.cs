using System.Collections.Generic;
using NUnit.Framework;

namespace ElevatorRush.Tests
{
    public class UnlockPopupsTests
    {
        static void AddLevels(SaveData save, int count)
        {
            int first = save.currentLevel;
            for (int i = 0; i < count; i++)
                save.RecordLevelComplete(first + i, ScoreSystem.MaxStars);
        }

        static void ShowAll(SaveData save)
        {
            foreach (var id in UnlockPopups.Pending(save))
                UnlockPopups.MarkShown(save, id);
        }

        [Test]
        public void FreshSaveHasNothingToAnnounce()
        {
            CollectionAssert.IsEmpty(UnlockPopups.Pending(TestSaves.With(0)));
        }

        [Test]
        public void CrossingEachThresholdShowsThatRowExactlyOnce()
        {
            var save = TestSaves.With(0);
            ShowAll(save);
            int done = 0;
            foreach (var row in UnlockSchedule.Rows)
            {
                if (row.threshold == 0)
                    continue;
                // Just below: nothing new.
                AddLevels(save, row.threshold - 1 - done);
                done = row.threshold - 1;
                CollectionAssert.IsEmpty(UnlockPopups.Pending(save), "below " + row.threshold);
                // At the threshold: exactly this row, in table order.
                AddLevels(save, 1);
                done++;
                CollectionAssert.AreEqual(row.ids, UnlockPopups.Pending(save), "at " + row.threshold);
                ShowAll(save);
                // Never again, also after a save and load.
                save = SaveData.FromJson(save.ToJson());
                CollectionAssert.IsEmpty(UnlockPopups.Pending(save), "after showing " + row.threshold);
            }
            Assert.AreEqual(185, done);
        }

        [Test]
        public void ABigJumpQueuesEveryCrossedRowInOrder()
        {
            var save = TestSaves.With(0);
            ShowAll(save);
            AddLevels(save, 30);
            var expected = new List<string>();
            foreach (var row in UnlockSchedule.Rows)
                if (row.threshold > 0 && row.threshold <= 30)
                    expected.AddRange(row.ids);
            CollectionAssert.AreEqual(expected, UnlockPopups.Pending(save));
        }

        [Test]
        public void EveryUnlockHasAHint()
        {
            foreach (var row in UnlockSchedule.Rows)
                foreach (var id in row.ids)
                    Assert.IsNotEmpty(UnlockPopups.HintOf(id), id);
        }
    }
}
