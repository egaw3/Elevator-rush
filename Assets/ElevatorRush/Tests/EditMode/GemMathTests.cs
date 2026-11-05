using NUnit.Framework;

namespace ElevatorRush.Tests
{
    public class GemMathTests
    {
        [TestCase(1, 1)]
        [TestCase(9, 1)]
        [TestCase(10, 1)]
        [TestCase(11, 2)]
        [TestCase(95, 10)]
        public void ShortfallRoundsUpToWholeGems(int missingCoins, int gems)
        {
            Assert.AreEqual(gems, GemMath.ShortfallGems(100 + missingCoins, 100));
        }

        [Test]
        public void NoGemsNeededWhenCoinsSuffice()
        {
            Assert.AreEqual(0, GemMath.ShortfallGems(100, 100));
            Assert.AreEqual(0, GemMath.ShortfallGems(100, 500));
            Assert.AreEqual(0, GemMath.ShortfallGems(0, 0));
        }

        [Test]
        public void EarlyUnlockOfCoinPricedItemsIsTwiceTheCoinValueRoundedUp()
        {
            Assert.AreEqual(30, GemMath.EarlyUnlockGems(150));
            Assert.AreEqual(31, GemMath.EarlyUnlockGems(151));
            Assert.AreEqual(31, GemMath.EarlyUnlockGems(155));
            Assert.AreEqual(0, GemMath.EarlyUnlockGems(0));
        }

        [Test]
        public void EarlyUnlockPricesForEachItemType()
        {
            Assert.AreEqual(200, GemMath.EarlyUnlockGems(Catalog.Find(Catalog.FloorId(1))));
            Assert.AreEqual(1400, GemMath.EarlyUnlockGems(Catalog.Find(Catalog.FloorId(7))));
            // Skin: DJ costs 300 coins.
            Assert.AreEqual(60, GemMath.EarlyUnlockGems(Catalog.Find("skin.dj")));
            // Tower skin: Brick Loft costs 400 coins.
            Assert.AreEqual(80, GemMath.EarlyUnlockGems(Catalog.Find("tower.brick")));
            // Elevator tier: Motor tier 2 costs 250 coins.
            Assert.AreEqual(50, GemMath.EarlyUnlockGems(Catalog.Upgrade(UpgradeKind.Motor), 2));
            Assert.AreEqual(240, GemMath.EarlyUnlockGems(Catalog.Upgrade(UpgradeKind.Capacity), 2));
        }

        [Test]
        public void DecorAndTierCapItemsHaveNoEarlyUnlock()
        {
            Assert.AreEqual(-1, GemMath.EarlyUnlockGems(Catalog.Decor(1, 0, 0)));
            Assert.AreEqual(-1, GemMath.EarlyUnlockGems(Catalog.Find(Catalog.TierId(2))));
            Assert.AreEqual(-1, GemMath.EarlyUnlockGems((CatalogItem)null));
        }

        [Test]
        public void OnlyTheNextFloorCanBeUnlockedEarly()
        {
            Assert.IsTrue(GemMath.IsNextFloor(1, 1));
            Assert.IsFalse(GemMath.IsNextFloor(2, 1));
            Assert.IsFalse(GemMath.IsNextFloor(0, 1));
            Assert.IsFalse(GemMath.IsNextFloor(Catalog.FloorCount, Catalog.FloorCount));
        }
    }
}
