using NUnit.Framework;

namespace ElevatorRush.Tests
{
    public class ShopTests
    {
        [Test]
        public void BuySucceedsDeductsCoinsAndEquipsDecor()
        {
            var save = TestSaves.With(0, 100);
            var shop = new Shop(save);
            int changes = 0;
            shop.Changed += () => changes++;

            var receipt = shop.TryBuy("decor.f0.left.0");
            Assert.IsTrue(receipt.Ok);
            Assert.AreEqual(40, receipt.spent);
            Assert.AreEqual(60, save.coins);
            Assert.IsTrue(save.Owns("decor.f0.left.0"));
            Assert.AreEqual(0, save.EquippedDecor(0, Catalog.SlotLeft));
            Assert.AreEqual(1, changes);
        }

        [Test]
        public void DecorPriceScalesWithFloor()
        {
            Assert.AreEqual(40, Catalog.Decor(0, 0, 0).price);
            Assert.AreEqual(160, Catalog.Decor(0, 0, 2).price);
            Assert.AreEqual(90 * 4, Catalog.Decor(3, 1, 1).price);
        }

        [Test]
        public void FailsWhenLocked()
        {
            var save = TestSaves.With(24, 10000);
            var shop = new Shop(save);
            Assert.AreEqual(ShopResult.Locked, shop.TryBuy("decor.f1.left.0").result);
            Assert.AreEqual(ShopResult.Locked, shop.TryBuy("skin.dj").result);
            Assert.AreEqual(ShopResult.Locked, shop.TryBuy("tower.brick").result);
            Assert.AreEqual(10000, save.coins);
        }

        [Test]
        public void FailsWhenTooExpensive()
        {
            var save = TestSaves.With(0, 39);
            var shop = new Shop(save);
            Assert.AreEqual(ShopResult.NotEnoughCoins, shop.TryBuy("decor.f0.left.0").result);
            Assert.AreEqual(39, save.coins);
            Assert.IsFalse(save.Owns("decor.f0.left.0"));
        }

        [Test]
        public void FailsWhenAlreadyOwned()
        {
            var save = TestSaves.With(0, 1000);
            var shop = new Shop(save);
            Assert.IsTrue(shop.TryBuy("skin.office").Ok);
            Assert.AreEqual(ShopResult.AlreadyOwned, shop.TryBuy("skin.office").result);
            Assert.AreEqual(ShopResult.AlreadyOwned, shop.TryBuy(Catalog.DefaultSkin).result);
            Assert.AreEqual(ShopResult.AlreadyOwned, shop.TryBuy(Catalog.DefaultTower).result);
            Assert.AreEqual(850, save.coins);
        }

        [Test]
        public void UnknownIdsFail()
        {
            var shop = new Shop(TestSaves.With(0, 1000));
            Assert.AreEqual(ShopResult.UnknownItem, shop.TryBuy("decor.f9.left.0").result);
            Assert.AreEqual(ShopResult.UnknownItem, shop.TryBuy("floor.0").result);
            Assert.AreEqual(ShopResult.UnknownItem, shop.TryBuy(null).result);
        }

        [Test]
        public void BuyingATowerSkinEquipsIt()
        {
            var save = TestSaves.With(25, 400);
            var shop = new Shop(save);
            Assert.IsTrue(shop.TryBuy("tower.brick").Ok);
            Assert.AreEqual("tower.brick", save.equippedTower);
            Assert.AreEqual(0, save.coins);
            Assert.AreEqual(ShopResult.Ok, shop.TryEquip(Catalog.DefaultTower));
            Assert.AreEqual(Catalog.DefaultTower, save.equippedTower);
        }

        [Test]
        public void OwnedDecorSwitchesForFree()
        {
            var save = TestSaves.With(0, 1000);
            var shop = new Shop(save);
            shop.TryBuy("decor.f0.right.0");
            shop.TryBuy("decor.f0.right.2");
            Assert.AreEqual(2, save.EquippedDecor(0, Catalog.SlotRight));
            int coins = save.coins;
            Assert.AreEqual(ShopResult.Ok, shop.TryEquip("decor.f0.right.0"));
            Assert.AreEqual(0, save.EquippedDecor(0, Catalog.SlotRight));
            Assert.AreEqual(coins, save.coins);
            Assert.AreEqual(1, save.equippedDecor.Count);
        }

        [Test]
        public void ElevatorTiersGoInOrder()
        {
            var save = TestSaves.With(100, 100000);
            var shop = new Shop(save);
            var motor = Catalog.Upgrade(UpgradeKind.Motor);
            Assert.AreEqual(ShopResult.OutOfOrder, shop.TryBuyUpgrade(motor, 2).result);
            Assert.AreEqual(0, save.UpgradeTier(motor.id));

            Assert.IsTrue(shop.TryBuyUpgrade(motor, 1).Ok);
            Assert.AreEqual(ShopResult.AlreadyOwned, shop.TryBuyUpgrade(motor, 1).result);
            Assert.AreEqual(ShopResult.OutOfOrder, shop.TryBuyUpgrade(motor, 3).result);
            Assert.IsTrue(shop.TryBuyUpgrade(motor, 2).Ok);
            Assert.AreEqual(100000 - 100 - 250, save.coins);
        }

        [Test]
        public void UpgradeIdBuysTheNextTierUntilMaxed()
        {
            var save = TestSaves.With(100, 100000);
            var shop = new Shop(save);
            for (int t = 1; t <= 5; t++)
            {
                Assert.AreEqual(Catalog.Upgrade(UpgradeKind.Tips).PriceOf(t), shop.PriceOf("elev.tips"));
                Assert.IsTrue(shop.TryBuy("elev.tips").Ok, "tier " + t);
            }
            Assert.AreEqual(5, save.UpgradeTier(UpgradeKind.Tips));
            Assert.AreEqual(ShopResult.MaxedOut, shop.TryBuy("elev.tips").result);
            Assert.AreEqual(100000 - (100 + 250 + 450 + 700 + 1000), save.coins);
        }

        [Test]
        public void ElevatorTiersStayWithinTheCap()
        {
            var save = TestSaves.With(24, 100000);
            var shop = new Shop(save);
            Assert.IsTrue(shop.TryBuy("elev.doors").Ok);
            Assert.AreEqual(ShopResult.Locked, shop.TryBuy("elev.doors").result);
            Assert.AreEqual(ShopResult.Locked, shop.TryBuy("elev.capacity").result);

            var later = TestSaves.With(25, 100000);
            var laterShop = new Shop(later);
            Assert.IsTrue(laterShop.TryBuy("elev.capacity").Ok);
            Assert.AreEqual(ShopResult.Locked, laterShop.TryBuy("elev.capacity").result);
        }

        [Test]
        public void EquippingAnUnownedItemFails()
        {
            var save = TestSaves.With(100, 100000);
            var shop = new Shop(save);
            Assert.AreEqual(ShopResult.NotOwned, shop.TryEquip("decor.f0.left.1"));
            Assert.AreEqual(ShopResult.NotOwned, shop.TryEquip("tower.candy"));
            Assert.AreEqual(ShopResult.NotOwned, shop.TryEquipSkin("skin.chef", 0));
            Assert.AreEqual(-1, save.EquippedDecor(0, Catalog.SlotLeft));
            Assert.AreEqual(Catalog.DefaultTower, save.equippedTower);
            Assert.AreEqual(Catalog.DefaultSkin, save.EquippedSkin(0));
        }

        [Test]
        public void EquippingASkinOnALockedColorSlotFails()
        {
            var save = TestSaves.With(10, 1000);
            var shop = new Shop(save);
            Assert.IsTrue(shop.TryBuy("skin.chef").Ok);
            Assert.AreEqual(ShopResult.SlotLocked, shop.TryEquipSkin("skin.chef", 1));
            Assert.AreEqual(ShopResult.Ok, shop.TryEquipSkin("skin.chef", 0));
            Assert.AreEqual("skin.chef", save.EquippedSkin(0));
            Assert.AreEqual(Catalog.DefaultSkin, save.EquippedSkin(1));
        }

        [Test]
        public void OneSkinCanBeEquippedOnManySlots()
        {
            var save = TestSaves.With(50, 1000);
            var shop = new Shop(save);
            shop.TryBuy("skin.builder");
            Assert.AreEqual(ShopResult.Ok, shop.TryEquipSkin("skin.builder", 0));
            Assert.AreEqual(ShopResult.Ok, shop.TryEquipSkin("skin.builder", 2));
            Assert.AreEqual("skin.builder", save.EquippedSkin(0));
            Assert.AreEqual("skin.builder", save.EquippedSkin(2));
            Assert.AreEqual(ShopResult.UnknownItem, shop.TryEquipSkin("skin.builder", Catalog.ColorSlotCount));
        }

        [Test]
        public void CompletingAFloorPaysTheBonusInTheReceipt()
        {
            var save = TestSaves.With(0, 10000);
            var shop = new Shop(save);
            for (int s = 0; s < Catalog.SlotsPerFloor - 1; s++)
                Assert.AreEqual(0, shop.TryBuy(Catalog.DecorId(0, s, 0)).bonus);
            var last = shop.TryBuy(Catalog.DecorId(0, Catalog.SlotsPerFloor - 1, 0));
            Assert.AreEqual(100, last.bonus);
            Assert.AreEqual(10000 - 6 * 40 + 100, save.coins);
            // Swapping for a pricier option on a complete floor pays nothing more.
            Assert.AreEqual(0, shop.TryBuy(Catalog.DecorId(0, 0, 2)).bonus);
        }
    }
}
