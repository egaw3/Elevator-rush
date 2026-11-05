using NUnit.Framework;

namespace ElevatorRush.Tests
{
    /// <summary>Free gems from play (monetization plan 2.1).</summary>
    public class GemRewardsTests
    {
        static int Win(SaveData save, int level, int stars)
        {
            int before = save.LevelsCompleted;
            save.RecordLevelComplete(level, stars);
            return GemRewards.ForLevelComplete(save, level, before);
        }

        [Test]
        public void FirstThreeStarFinishPaysOneGemOnce()
        {
            var save = TestSaves.With(0);
            Assert.AreEqual(1, Win(save, 1, 3));
            Assert.AreEqual(0, Win(save, 1, 3));
            Assert.AreEqual(1, save.gems);
        }

        [Test]
        public void FewerStarsPayNothingUntilThreeStars()
        {
            var save = TestSaves.With(0);
            Assert.AreEqual(0, Win(save, 1, 2));
            Assert.AreEqual(1, Win(save, 1, 3));
            Assert.AreEqual(1, save.gems);
        }

        [Test]
        public void ReachingAnUnlockRowPaysFiveGems()
        {
            var save = TestSaves.With(0);
            for (int level = 1; level <= 4; level++)
                Win(save, level, 1);
            Assert.AreEqual(0, save.gems);
            // Level 5 reaches the row at 5 levels completed.
            Assert.AreEqual(Catalog.GemsPerUnlockRow, Win(save, 5, 1));
            Assert.AreEqual(Catalog.GemsPerUnlockRow + Catalog.GemsPerThreeStars, Win(save, 6, 3) + Win(save, 7, 1) + Win(save, 8, 1)
                + Win(save, 9, 1) + Win(save, 10, 1));
        }

        [Test]
        public void ReplayingAWonLevelReachesNoNewRow()
        {
            var save = TestSaves.With(0);
            for (int level = 1; level <= 5; level++)
                Win(save, level, 1);
            int gems = save.gems;
            Assert.AreEqual(0, Win(save, 5, 2));
            Assert.AreEqual(gems, save.gems);
        }

        [Test]
        public void RowsCrossedCountsRowsNotItems()
        {
            Assert.AreEqual(0, UnlockSchedule.RowsCrossed(0, 0));
            Assert.AreEqual(1, UnlockSchedule.RowsCrossed(0, 5));
            // Row 25 holds a floor, a tower skin and a tier cap: still one row.
            Assert.AreEqual(1, UnlockSchedule.RowsCrossed(24, 25));
            Assert.AreEqual(UnlockSchedule.Rows.Count - 1, UnlockSchedule.RowsCrossed(0, 100000));
        }

        [Test]
        public void CompletingAFloorPaysGemsWithTheBonus()
        {
            var save = TestSaves.With(0, 100000);
            var shop = new Shop(save);
            ShopReceipt last = default;
            for (int s = 0; s < Catalog.SlotsPerFloor; s++)
                last = shop.TryBuy(Catalog.DecorId(0, s, 0));
            Assert.AreEqual(Catalog.GemsPerFloorComplete, last.bonusGems);
            Assert.AreEqual(Catalog.GemsPerFloorComplete, save.gems);
            // Swapping options on a finished floor pays nothing more.
            shop.TryBuy(Catalog.DecorId(0, 0, 1));
            Assert.AreEqual(Catalog.GemsPerFloorComplete, save.gems);
        }
    }
}
