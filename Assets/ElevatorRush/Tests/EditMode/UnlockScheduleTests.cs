using System.Collections.Generic;
using NUnit.Framework;

namespace ElevatorRush.Tests
{
    public class UnlockScheduleTests
    {
        // Home plan 4.1, row by row.
        static readonly (int at, string[] ids)[] Table =
        {
            (0, new[] { "floor.0", "tower.classic", "elev.tier1", "skin.default", "skin.office" }),
            (5, new[] { "skin.builder" }),
            (10, new[] { "skin.chef" }),
            (15, new[] { "skin.tourist" }),
            (25, new[] { "floor.1", "tower.brick", "elev.tier2" }),
            (35, new[] { "skin.dj" }),
            (50, new[] { "floor.2", "tower.glass", "elev.tier3" }),
            (60, new[] { "skin.cowboy" }),
            (75, new[] { "floor.3", "tower.candy", "elev.tier4" }),
            (85, new[] { "skin.wizard" }),
            (100, new[] { "floor.4", "tower.pagoda", "elev.tier5" }),
            (110, new[] { "skin.astronaut" }),
            (125, new[] { "floor.5", "tower.castle" }),
            (135, new[] { "skin.robot" }),
            (150, new[] { "floor.6", "tower.space" }),
            (160, new[] { "skin.pirate" }),
            (175, new[] { "floor.7", "tower.gold" }),
            (185, new[] { "skin.royal" }),
        };

        [Test]
        public void RowsMatchThePlanTable()
        {
            Assert.AreEqual(Table.Length, UnlockSchedule.Rows.Count);
            for (int i = 0; i < Table.Length; i++)
            {
                Assert.AreEqual(Table[i].at, UnlockSchedule.Rows[i].threshold, "row " + i);
                CollectionAssert.AreEqual(Table[i].ids, UnlockSchedule.Rows[i].ids, "row " + i);
            }
        }

        [Test]
        public void EachItemIsLockedJustBelowItsThresholdAndUnlockedAtIt()
        {
            foreach (var row in Table)
                foreach (var id in row.ids)
                {
                    if (row.at > 0)
                        Assert.IsFalse(UnlockSchedule.IsUnlocked(id, row.at - 1), id + " at " + (row.at - 1));
                    Assert.IsTrue(UnlockSchedule.IsUnlocked(id, row.at), id + " at " + row.at);
                }
        }

        [Test]
        public void NewlyUnlockedReturnsExactlyTheCrossedRows()
        {
            CollectionAssert.AreEqual(new[] { "skin.builder" }, UnlockSchedule.NewlyUnlocked(4, 5));
            CollectionAssert.IsEmpty(UnlockSchedule.NewlyUnlocked(5, 9));
            CollectionAssert.AreEqual(new[] { "skin.tourist", "floor.1", "tower.brick", "elev.tier2" },
                UnlockSchedule.NewlyUnlocked(14, 25));
        }

        [Test]
        public void FloorsUnlockEveryTwentyFiveLevels()
        {
            for (int k = 0; k < Catalog.FloorCount; k++)
            {
                if (k > 0)
                    Assert.IsFalse(UnlockSchedule.IsFloorUnlocked(k, 25 * k - 1));
                Assert.IsTrue(UnlockSchedule.IsFloorUnlocked(k, 25 * k));
                Assert.AreEqual(k == 0 ? 1 : k, UnlockSchedule.UnlockedFloorCount(25 * k - (k > 0 ? 1 : 0)));
            }
            Assert.AreEqual(8, UnlockSchedule.UnlockedFloorCount(1000));
            Assert.IsFalse(UnlockSchedule.IsFloorUnlocked(8, 1000));
        }

        [Test]
        public void DecorFollowsItsFloor()
        {
            Assert.IsTrue(UnlockSchedule.IsUnlocked("decor.f0.left.2", 0));
            Assert.IsFalse(UnlockSchedule.IsUnlocked("decor.f1.left.0", 24));
            Assert.IsTrue(UnlockSchedule.IsUnlocked("decor.f1.left.0", 25));
            Assert.IsFalse(UnlockSchedule.IsUnlocked("no.such.item", 1000));
        }

        [TestCase(0, 1)]
        [TestCase(24, 1)]
        [TestCase(25, 2)]
        [TestCase(99, 4)]
        [TestCase(100, 5)]
        [TestCase(500, 5)]
        public void ElevatorTierCap(int levelsCompleted, int cap)
        {
            Assert.AreEqual(cap, UnlockSchedule.TierCap(levelsCompleted));
        }

        [Test]
        public void CapacityTiersNeedCapsTwoAndFour()
        {
            var capacity = Catalog.Upgrade(UpgradeKind.Capacity);
            Assert.IsFalse(UnlockSchedule.IsTierUnlocked(capacity, 1, 24));
            Assert.IsTrue(UnlockSchedule.IsTierUnlocked(capacity, 1, 25));
            Assert.IsFalse(UnlockSchedule.IsTierUnlocked(capacity, 2, 74));
            Assert.IsTrue(UnlockSchedule.IsTierUnlocked(capacity, 2, 75));
            Assert.IsFalse(UnlockSchedule.IsTierUnlocked(capacity, 3, 1000));
        }

        [Test]
        public void NextUnlockAtZero()
        {
            var next = UnlockSchedule.Next(0);
            Assert.AreEqual("skin.builder", next.id);
            Assert.AreEqual(5, next.threshold);
            Assert.AreEqual(5, next.levelsToGo);
            Assert.AreEqual(0f, next.fill, 1e-5f);
        }

        [Test]
        public void NextUnlockAtSeven()
        {
            var next = UnlockSchedule.Next(7);
            Assert.AreEqual("skin.chef", next.id);
            Assert.AreEqual(3, next.levelsToGo);
            Assert.AreEqual(0.4f, next.fill, 1e-5f);
        }

        [Test]
        public void NextUnlockShowsTheFloorFirstOnFloorRows()
        {
            Assert.AreEqual("floor.1", UnlockSchedule.Next(15).id);
        }

        [TestCase(185)]
        [TestCase(186)]
        [TestCase(10000)]
        public void NothingLeftAfterTheLastRow(int levelsCompleted)
        {
            var next = UnlockSchedule.Next(levelsCompleted);
            Assert.IsTrue(next.IsDone);
            Assert.AreEqual(1f, next.fill);
        }

        [Test]
        public void EveryUnlockIdHasAName()
        {
            var seen = new HashSet<string>();
            foreach (var row in UnlockSchedule.Rows)
                foreach (var id in row.ids)
                {
                    Assert.IsTrue(seen.Add(id), "duplicate " + id);
                    Assert.AreNotEqual(id, Catalog.NameOf(id));
                }
        }
    }
}
