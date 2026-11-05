using NUnit.Framework;

namespace ElevatorRush.Tests
{
    /// <summary>Gems in the shop (monetization plan 2.2 and 2.3).</summary>
    public class ShopGemTests
    {
        const string Decor = "decor.f0.left.0"; // 40 coins

        static SaveData With(int levelsCompleted, int coins, int gems)
        {
            var save = TestSaves.With(levelsCompleted, coins);
            save.gems = gems;
            return save;
        }

        // ---- Covering a coin shortfall ----

        [Test]
        public void ShortfallSpendsAllCoinsAndTheGemsForTheRest()
        {
            var save = With(0, 35, 10);
            var shop = new Shop(save);
            Assert.AreEqual(1, shop.ShortfallGems(Decor));

            var receipt = shop.TryBuy(Decor, true);
            Assert.IsTrue(receipt.Ok);
            Assert.AreEqual(35, receipt.spent);
            Assert.AreEqual(1, receipt.gemsSpent);
            Assert.AreEqual(0, save.coins);
            Assert.AreEqual(9, save.gems);
            Assert.IsTrue(save.Owns(Decor));
        }

        [Test]
        public void RoundingRemainderIsNotRefunded()
        {
            var save = With(0, 21, 10);
            var receipt = new Shop(save).TryBuy(Decor, true);
            // 19 coins missing: 2 gems cover 20, the extra coin is not given back.
            Assert.AreEqual(2, receipt.gemsSpent);
            Assert.AreEqual(0, save.coins);
            Assert.AreEqual(8, save.gems);
        }

        [Test]
        public void ShortfallFailsWithoutEnoughGems()
        {
            var save = With(0, 0, 3);
            var shop = new Shop(save);
            Assert.AreEqual(ShopResult.NotEnoughGems, shop.CheckBuy(Decor, true));
            Assert.AreEqual(ShopResult.NotEnoughGems, shop.TryBuy(Decor, true).result);
            Assert.AreEqual(3, save.gems);
            Assert.IsFalse(save.Owns(Decor));
        }

        [Test]
        public void GemsAreNeverSpentUnlessAsked()
        {
            var save = With(0, 35, 10);
            Assert.AreEqual(ShopResult.NotEnoughCoins, new Shop(save).TryBuy(Decor).result);
            Assert.AreEqual(35, save.coins);
            Assert.AreEqual(10, save.gems);
        }

        [Test]
        public void CoinsThatSufficeAreUsedAlone()
        {
            var save = With(0, 100, 10);
            var receipt = new Shop(save).TryBuy(Decor, true);
            Assert.AreEqual(40, receipt.spent);
            Assert.AreEqual(0, receipt.gemsSpent);
            Assert.AreEqual(60, save.coins);
            Assert.AreEqual(10, save.gems);
        }

        [Test]
        public void ShortfallWorksForUpgradeTiers()
        {
            var save = With(0, 50, 5);
            var receipt = new Shop(save).TryBuy("elev.motor", true);
            Assert.IsTrue(receipt.Ok);
            Assert.AreEqual(5, receipt.gemsSpent);
            Assert.AreEqual(1, save.UpgradeTier(UpgradeKind.Motor));
            Assert.AreEqual(0, save.coins);
            Assert.AreEqual(0, save.gems);
        }

        [Test]
        public void ShortfallStillRespectsLocks()
        {
            var save = With(0, 0, 10000);
            Assert.AreEqual(ShopResult.Locked, new Shop(save).TryBuy("skin.dj", true).result);
            Assert.AreEqual(10000, save.gems);
        }

        // ---- Unlocking early ----

        [Test]
        public void EarlyUnlockedSkinIsUnlockedAndOwned()
        {
            var save = With(0, 0, 100);
            var shop = new Shop(save);
            var receipt = shop.TryUnlockEarly("skin.dj");
            Assert.IsTrue(receipt.Ok);
            Assert.AreEqual(60, receipt.gemsSpent);
            Assert.AreEqual(40, save.gems);
            Assert.IsTrue(save.Owns("skin.dj"));
            Assert.IsTrue(UnlockSchedule.IsUnlocked("skin.dj", save));
            Assert.AreEqual(ShopResult.AlreadyOwned, shop.CheckBuy("skin.dj"));
            Assert.AreEqual(ShopResult.AlreadyOwned, shop.CheckUnlockEarly("skin.dj"));
            Assert.AreEqual(ShopResult.Ok, shop.TryEquipSkin("skin.dj", 0));
        }

        [Test]
        public void EarlyUnlockedTowerSkinIsOwnedAndEquipped()
        {
            var save = With(0, 0, 100);
            Assert.IsTrue(new Shop(save).TryUnlockEarly("tower.brick").Ok);
            Assert.IsTrue(save.Owns("tower.brick"));
            Assert.AreEqual("tower.brick", save.equippedTower);
            Assert.AreEqual(20, save.gems);
        }

        [Test]
        public void EarlyUnlockedFloorOpensItsSlotButDecorStillCostsCoins()
        {
            var save = With(0, 1000, 200);
            var shop = new Shop(save);
            Assert.AreEqual(ShopResult.Locked, shop.CheckBuy("decor.f1.left.0"));
            Assert.AreEqual(ShopResult.SlotLocked, shop.TryEquipSkin(Catalog.DefaultSkin, 1));

            Assert.IsTrue(shop.TryUnlockEarly(Catalog.FloorId(1)).Ok);
            Assert.AreEqual(0, save.gems);
            Assert.AreEqual(1000, save.coins);
            Assert.IsTrue(UnlockSchedule.IsFloorUnlocked(1, save));
            Assert.IsTrue(UnlockSchedule.IsColorSlotUnlocked(1, save));
            Assert.AreEqual(2, UnlockSchedule.UnlockedFloorCount(save));
            Assert.AreEqual(ShopResult.Ok, shop.TryEquipSkin(Catalog.DefaultSkin, 1));

            var receipt = shop.TryBuy("decor.f1.left.0");
            Assert.IsTrue(receipt.Ok);
            Assert.AreEqual(Catalog.Decor(1, Catalog.SlotLeft, 0).price, receipt.spent);
            // The floor's tower skin is not part of the floor.
            Assert.AreEqual(ShopResult.Locked, shop.CheckBuy("tower.brick"));
        }

        [Test]
        public void FloorsUnlockEarlyInOrder()
        {
            var save = With(0, 0, 10000);
            var shop = new Shop(save);
            Assert.AreEqual(ShopResult.OutOfOrder, shop.TryUnlockEarly(Catalog.FloorId(2)).result);
            Assert.AreEqual(10000, save.gems);
            Assert.IsTrue(shop.TryUnlockEarly(Catalog.FloorId(1)).Ok);
            Assert.IsTrue(shop.TryUnlockEarly(Catalog.FloorId(2)).Ok);
            Assert.AreEqual(3, UnlockSchedule.UnlockedFloorCount(save));
            Assert.AreEqual(10000 - 200 - 400, save.gems);
        }

        [Test]
        public void NextFloorAfterLevelUnlocksCanBeBoughtEarly()
        {
            var save = With(25, 0, 400);
            var shop = new Shop(save);
            Assert.AreEqual(ShopResult.NotLocked, shop.CheckUnlockEarly(Catalog.FloorId(1)));
            Assert.AreEqual(ShopResult.Ok, shop.CheckUnlockEarly(Catalog.FloorId(2)));
        }

        [Test]
        public void AlreadyUnlockedItemsCannotBeUnlockedEarly()
        {
            var save = With(0, 0, 10000);
            var shop = new Shop(save);
            Assert.AreEqual(ShopResult.NotLocked, shop.TryUnlockEarly("skin.office").result);
            Assert.AreEqual(ShopResult.NotLocked, shop.TryUnlockEarly(Catalog.FloorId(0)).result);
            Assert.AreEqual(ShopResult.NotLocked, shop.TryUnlockEarly("elev.motor").result);
            Assert.AreEqual(ShopResult.UnknownItem, shop.TryUnlockEarly("decor.f1.left.0").result);
            Assert.AreEqual(ShopResult.UnknownItem, shop.TryUnlockEarly("skin.ninja").result);
            Assert.AreEqual(10000, save.gems);
        }

        [Test]
        public void EarlyUnlockFailsWithoutEnoughGems()
        {
            var save = With(0, 0, 59);
            Assert.AreEqual(ShopResult.NotEnoughGems, new Shop(save).TryUnlockEarly("skin.dj").result);
            Assert.AreEqual(59, save.gems);
            Assert.IsFalse(save.Owns("skin.dj"));
        }

        [Test]
        public void ElevatorTiersAboveTheCapUnlockOneAtATimeInOrder()
        {
            var save = With(0, 100, 1000);
            var shop = new Shop(save);
            var motor = Catalog.Upgrade(UpgradeKind.Motor);

            // Tier 1 is within the cap: coins, not gems.
            Assert.AreEqual(ShopResult.NotLocked, shop.CheckUnlockEarly(motor.id));
            Assert.IsTrue(shop.TryBuy(motor.id).Ok);

            // Tier 2 needs the cap at 2; gems buy it now, then tier 3 is next.
            Assert.AreEqual(ShopResult.Locked, shop.CheckBuy(motor.id));
            Assert.AreEqual(50, shop.EarlyUnlockPrice(motor.id));
            Assert.IsTrue(shop.TryUnlockEarly(motor.id).Ok);
            Assert.AreEqual(2, save.UpgradeTier(motor.id));
            Assert.AreEqual(ShopResult.OutOfOrder, shop.CheckUpgrade(motor, 4));
            Assert.AreEqual(90, shop.EarlyUnlockPrice(motor.id));
            Assert.IsTrue(shop.TryUnlockEarly(motor.id).Ok);
            Assert.IsTrue(shop.TryUnlockEarly(motor.id).Ok);
            Assert.IsTrue(shop.TryUnlockEarly(motor.id).Ok);
            Assert.AreEqual(motor.MaxTier, save.UpgradeTier(motor.id));
            Assert.AreEqual(ShopResult.MaxedOut, shop.TryUnlockEarly(motor.id).result);
            Assert.AreEqual(1000 - 50 - 90 - 140 - 200, save.gems);
        }

        [Test]
        public void EarlyUnlocksAreNotAnnouncedWhenTheirLevelIsReached()
        {
            var save = With(0, 0, 1000);
            var shop = new Shop(save);
            Assert.IsTrue(shop.TryUnlockEarly("skin.builder").Ok);
            Assert.IsTrue(shop.TryUnlockEarly(Catalog.FloorId(1)).Ok);
            for (int level = 1; level <= 25; level++)
                save.RecordLevelComplete(level, ScoreSystem.MaxStars);

            var pending = UnlockPopups.Pending(save);
            CollectionAssert.DoesNotContain(pending, "skin.builder");
            CollectionAssert.DoesNotContain(pending, Catalog.FloorId(1));
            CollectionAssert.Contains(pending, "skin.chef");
            CollectionAssert.Contains(pending, "tower.brick");
            // No red dot either: floor 1 is the only Build unlock up to 25 levels.
            Assert.IsFalse(HomeBadges.HasUnviewed(save, HomeTab.Build));
            Assert.IsTrue(HomeBadges.HasUnviewed(save, HomeTab.Characters));
        }

        [Test]
        public void EveryGemSpendFiresChangedForSaving()
        {
            var save = With(0, 0, 1000);
            var shop = new Shop(save);
            int changes = 0;
            shop.Changed += () => changes++;
            shop.TryUnlockEarly("skin.dj");
            shop.TryBuy(Decor, true);
            shop.TryUnlockEarly("skin.dj");
            Assert.AreEqual(2, changes);
        }
    }
}
