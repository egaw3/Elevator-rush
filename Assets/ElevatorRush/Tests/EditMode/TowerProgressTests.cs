using NUnit.Framework;

namespace ElevatorRush.Tests
{
    public class TowerProgressTests
    {
        static void Fill(SaveData save, int floor, int slots)
        {
            for (int s = 0; s < slots; s++)
            {
                var item = Catalog.Decor(floor, s, 0);
                save.AddOwned(item.id);
                save.SetEquippedDecor(item);
            }
        }

        [Test]
        public void EmptyTower()
        {
            var save = TestSaves.With(0);
            Assert.AreEqual(0, TowerProgress.FilledSlots(save, 0));
            Assert.IsFalse(TowerProgress.IsFloorComplete(save, 0));
            Assert.AreEqual(0, TowerProgress.TotalFilled(save));
            Assert.AreEqual(0, TowerProgress.Percent(save));
            Assert.AreEqual(0, TowerProgress.TryPayCompletionBonus(save, 0));
        }

        [Test]
        public void PartialFloor()
        {
            var save = TestSaves.With(0);
            Fill(save, 0, 4);
            Assert.AreEqual(4, TowerProgress.FilledSlots(save, 0));
            Assert.IsFalse(TowerProgress.IsFloorComplete(save, 0));
            Assert.AreEqual(0, TowerProgress.TryPayCompletionBonus(save, 0));
            Assert.AreEqual(0, save.coins);
        }

        [Test]
        public void CompleteFloorPaysItsBonusOnce()
        {
            var save = TestSaves.With(75);
            Fill(save, 3, Catalog.SlotsPerFloor);
            Assert.IsTrue(TowerProgress.IsFloorComplete(save, 3));
            Assert.AreEqual(400, TowerProgress.TryPayCompletionBonus(save, 3));
            Assert.AreEqual(400, save.coins);
            Assert.AreEqual(0, TowerProgress.TryPayCompletionBonus(save, 3));
            Assert.AreEqual(400, save.coins);
            Assert.IsTrue(save.IsBonusPaid(3));
        }

        [Test]
        public void BonusIsNotPaidAgainAfterReload()
        {
            var save = TestSaves.With(0);
            Fill(save, 0, Catalog.SlotsPerFloor);
            TowerProgress.TryPayCompletionBonus(save, 0);
            var loaded = SaveData.FromJson(save.ToJson());
            Assert.AreEqual(0, TowerProgress.TryPayCompletionBonus(loaded, 0));
            Assert.AreEqual(100, loaded.coins);
        }

        [Test]
        public void PercentIsOutOfFortyEight()
        {
            var save = TestSaves.With(0);
            Fill(save, 0, 6);
            Fill(save, 1, 6);
            Fill(save, 2, 4);
            Assert.AreEqual(16, TowerProgress.TotalFilled(save));
            Assert.AreEqual(16f / 48f, TowerProgress.Fraction(save), 1e-6f);
            Assert.AreEqual(33, TowerProgress.Percent(save));
            for (int f = 0; f < Catalog.FloorCount; f++)
                Fill(save, f, Catalog.SlotsPerFloor);
            Assert.AreEqual(100, TowerProgress.Percent(save));
        }
    }
}
