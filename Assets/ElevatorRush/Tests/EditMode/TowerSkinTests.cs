using NUnit.Framework;

namespace ElevatorRush.Tests
{
    public class TowerSkinTests
    {
        [Test]
        public void EveryCatalogTowerHasASkinInTheSameOrder()
        {
            Assert.AreEqual(Catalog.Towers.Length, TowerSkins.Count);
            for (int i = 0; i < Catalog.Towers.Length; i++)
            {
                Assert.AreEqual(Catalog.Towers[i].id, TowerSkins.At(i).id);
                Assert.AreSame(TowerSkins.At(i), TowerSkins.Get(Catalog.Towers[i].id));
            }
        }

        [Test]
        public void UnknownIdsFallBackToClassic()
        {
            Assert.AreSame(TowerSkins.Classic, TowerSkins.Get("tower.moon"));
            Assert.AreSame(TowerSkins.Classic, TowerSkins.Get(null));
            Assert.AreEqual(Catalog.DefaultTower, TowerSkins.Classic.id);
        }

        [Test]
        public void SkinsLookDifferentFromEachOther()
        {
            for (int a = 0; a < TowerSkins.Count; a++)
                for (int b = a + 1; b < TowerSkins.Count; b++)
                {
                    var x = TowerSkins.At(a);
                    var y = TowerSkins.At(b);
                    bool same = x.exterior == y.exterior && x.exteriorPattern == y.exteriorPattern && x.ornament == y.ornament;
                    Assert.IsFalse(same, x.id + " vs " + y.id);
                }
        }

        [Test]
        public void TowerSkinPricesAndUnlocksFollowThePlan()
        {
            for (int i = 0; i < Catalog.Towers.Length; i++)
            {
                Assert.AreEqual(400 * i, Catalog.Towers[i].price);
                Assert.AreEqual(25 * i, Catalog.Towers[i].unlockAt);
            }
        }
    }
}
