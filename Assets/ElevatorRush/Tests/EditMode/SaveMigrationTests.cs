using System.Linq;
using NUnit.Framework;

namespace ElevatorRush.Tests
{
    public class SaveMigrationTests
    {
        // Exactly what the game wrote before the home-screen plan (no version field).
        const string OldSave =
            "{\"currentLevel\":31,\"coins\":1234,\"bestStars\":[3,3,2,3,1,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,2,3,3,3]," +
            "\"endlessHighScore\":57,\"endlessUnlocked\":true,\"muted\":true}";

        [Test]
        public void OldSaveKeepsCoinsLevelStarsAndEndless()
        {
            var save = SaveData.FromJson(OldSave);
            Assert.AreEqual(SaveMigration.CurrentVersion, save.version);
            Assert.AreEqual(31, save.currentLevel);
            Assert.AreEqual(1234, save.coins);
            Assert.AreEqual(30, save.bestStars.Count);
            Assert.AreEqual(2, save.BestStars(3));
            Assert.AreEqual(1, save.BestStars(5));
            Assert.AreEqual(30, save.LevelsCompleted);
            Assert.AreEqual(57, save.endlessHighScore);
            Assert.IsTrue(save.endlessUnlocked);
            Assert.IsTrue(save.muted);
        }

        [Test]
        public void OldSaveGetsNewFieldsAtTheirDefaults()
        {
            var save = SaveData.FromJson(OldSave);
            Assert.IsFalse(save.musicMuted);
            Assert.IsFalse(save.vibrationMuted);
            CollectionAssert.IsEmpty(save.owned);
            CollectionAssert.IsEmpty(save.equippedDecor);
            CollectionAssert.IsEmpty(save.floorBonusPaid);
            CollectionAssert.IsEmpty(save.upgrades);
            CollectionAssert.IsEmpty(save.viewed);
            Assert.AreEqual(Catalog.ColorSlotCount, save.equippedSkins.Count);
            CollectionAssert.AreEqual(Enumerable.Repeat(Catalog.DefaultSkin, Catalog.ColorSlotCount), save.equippedSkins);
            Assert.AreEqual(Catalog.DefaultTower, save.equippedTower);
            Assert.IsTrue(save.Owns(Catalog.DefaultSkin));
            Assert.IsTrue(save.Owns(Catalog.DefaultTower));
        }

        [Test]
        public void ThingsUnlockedBeforeTheUpdateAreNotAnnouncedAgain()
        {
            var save = SaveData.FromJson(OldSave);
            CollectionAssert.Contains(save.unlocksShown, "floor.1");
            CollectionAssert.Contains(save.unlocksShown, "skin.tourist");
            CollectionAssert.DoesNotContain(save.unlocksShown, "skin.dj");
        }

        [Test]
        public void MigratedSaveRoundTrips()
        {
            var save = SaveData.FromJson(OldSave);
            var shop = new Shop(save);
            Assert.IsTrue(shop.TryBuy("decor.f1.center.1").Ok);
            Assert.IsTrue(shop.TryBuy("elev.motor").Ok);
            Assert.AreEqual(ShopResult.Ok, shop.TryEquipSkin(Catalog.DefaultSkin, 1));
            save.musicMuted = true;

            var loaded = SaveData.FromJson(save.ToJson());
            Assert.AreEqual(save.coins, loaded.coins);
            Assert.AreEqual(1, loaded.EquippedDecor(1, Catalog.SlotCenter));
            Assert.AreEqual(1, loaded.UpgradeTier(UpgradeKind.Motor));
            Assert.IsTrue(loaded.musicMuted);
            CollectionAssert.AreEqual(save.unlocksShown, loaded.unlocksShown);
        }

        [Test]
        public void UnknownIdsAreIgnoredWithoutErrors()
        {
            const string json =
                "{\"version\":1,\"currentLevel\":3,\"coins\":50,\"bestStars\":[3,3]," +
                "\"owned\":[\"decor.f0.left.1\",\"decor.f0.sofa.9\",\"skin.ninja\",null,\"decor.f0.left.1\",\"floor.0\"]," +
                "\"equippedDecor\":[\"decor.f0.sofa.9\",\"decor.f0.left.1\",\"decor.f0.right.0\"]," +
                "\"floorBonusPaid\":[0,0,42,-1]," +
                "\"upgrades\":[{\"id\":\"elev.turbo\",\"tier\":3},{\"id\":\"elev.motor\",\"tier\":99},{\"id\":\"elev.motor\",\"tier\":1}]," +
                "\"equippedSkins\":[\"skin.ninja\",\"skin.chef\"]," +
                "\"equippedTower\":\"tower.moon\"," +
                "\"unlocksShown\":[\"floor.0\",\"floor.99\"],\"viewed\":[\"what\"]}";

            SaveData save = null;
            Assert.DoesNotThrow(() => save = SaveData.FromJson(json));
            Assert.AreEqual(50, save.coins);
            CollectionAssert.AreEqual(new[] { "decor.f0.left.1" }, save.owned);
            // The right slot was never bought, so it is not equipped.
            CollectionAssert.AreEqual(new[] { "decor.f0.left.1" }, save.equippedDecor);
            CollectionAssert.AreEqual(new[] { 0 }, save.floorBonusPaid);
            Assert.AreEqual(1, save.upgrades.Count);
            Assert.AreEqual(5, save.UpgradeTier(UpgradeKind.Motor));
            // Unknown and unowned skins fall back to Default.
            Assert.AreEqual(Catalog.DefaultSkin, save.EquippedSkin(0));
            Assert.AreEqual(Catalog.DefaultSkin, save.EquippedSkin(1));
            Assert.AreEqual(Catalog.DefaultTower, save.equippedTower);
            CollectionAssert.AreEqual(new[] { "floor.0" }, save.unlocksShown);
            CollectionAssert.IsEmpty(save.viewed);
        }

        // A save from before gems (monetization plan 8): 30 levels, floor 0 complete and paid.
        const string Version1Save =
            "{\"version\":1,\"currentLevel\":31,\"coins\":777," +
            "\"bestStars\":[3,3,2,3,1,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,2,3,3,3]," +
            "\"endlessHighScore\":57,\"endlessUnlocked\":true,\"musicMuted\":true," +
            "\"owned\":[\"decor.f0.flooring.0\",\"decor.f0.wall.0\",\"decor.f0.left.0\",\"decor.f0.center.0\",\"decor.f0.right.0\",\"decor.f0.wallArt.0\",\"skin.chef\",\"tower.brick\"]," +
            "\"equippedDecor\":[\"decor.f0.flooring.0\",\"decor.f0.wall.0\",\"decor.f0.left.0\",\"decor.f0.center.0\",\"decor.f0.right.0\",\"decor.f0.wallArt.0\"]," +
            "\"floorBonusPaid\":[0],\"upgrades\":[{\"id\":\"elev.motor\",\"tier\":2}]," +
            "\"equippedSkins\":[\"skin.chef\"],\"equippedTower\":\"tower.brick\"," +
            "\"unlocksShown\":[\"floor.1\"],\"viewed\":[\"skin.chef\"]}";

        [Test]
        public void Version1SaveKeepsEverything()
        {
            var save = SaveData.FromJson(Version1Save);
            Assert.AreEqual(SaveMigration.CurrentVersion, save.version);
            Assert.AreEqual(31, save.currentLevel);
            Assert.AreEqual(777, save.coins);
            Assert.AreEqual(30, save.LevelsCompleted);
            Assert.AreEqual(57, save.endlessHighScore);
            Assert.IsTrue(save.musicMuted);
            Assert.AreEqual(8, save.owned.Count);
            Assert.IsTrue(TowerProgress.IsFloorComplete(save, 0));
            CollectionAssert.AreEqual(new[] { 0 }, save.floorBonusPaid);
            Assert.AreEqual(2, save.UpgradeTier(UpgradeKind.Motor));
            Assert.AreEqual("skin.chef", save.EquippedSkin(0));
            Assert.AreEqual("tower.brick", save.equippedTower);
            CollectionAssert.AreEqual(new[] { "floor.1" }, save.unlocksShown);
            CollectionAssert.AreEqual(new[] { "skin.chef" }, save.viewed);
        }

        [Test]
        public void Version1SaveGetsTheGemsItAlreadyEarned()
        {
            var save = SaveData.FromJson(Version1Save);
            // 27 three-star levels, 4 unlock rows reached (5, 10, 15, 25) and 1 finished floor.
            int expected = 27 * Catalog.GemsPerThreeStars + 4 * Catalog.GemsPerUnlockRow + Catalog.GemsPerFloorComplete;
            Assert.AreEqual(expected, save.gems);
            Assert.AreEqual(27, save.threeStarGemPaid.Count);
            CollectionAssert.DoesNotContain(save.threeStarGemPaid, 3);
        }

        [Test]
        public void GemsArePaidOnlyOnceAcrossLoads()
        {
            var save = SaveData.FromJson(Version1Save);
            int gems = save.gems;
            var loaded = SaveData.FromJson(save.ToJson());
            Assert.AreEqual(gems, loaded.gems);
            // A three-star level already paid by the migration pays nothing when replayed.
            Assert.AreEqual(0, GemRewards.ForLevelComplete(loaded, 1, loaded.LevelsCompleted));
        }

        [Test]
        public void MonetizationFieldsStartEmpty()
        {
            var save = SaveData.FromJson(OldSave);
            CollectionAssert.IsEmpty(save.earlyUnlocked);
            CollectionAssert.IsEmpty(save.transactions);
            Assert.IsFalse(save.removeAds);
            Assert.IsFalse(save.starterPackOwned);
            Assert.IsFalse(save.starterCurrencyGranted);
            Assert.IsFalse(save.consentResolved);
            Assert.AreEqual(0, save.interstitialsShown);
            Assert.IsNotNull(save.freeGemAds);
            Assert.AreEqual(0, save.freeGemAds.count);
        }

        [Test]
        public void MonetizationFieldsRoundTrip()
        {
            var save = TestSaves.With(3, 10);
            save.gems = 242;
            Assert.IsTrue(new Shop(save).TryUnlockEarly(Catalog.FloorId(1)).Ok);
            save.removeAds = true;
            save.starterPackOwned = true;
            save.starterCurrencyGranted = true;
            save.transactions.Add("GPA.1234");
            save.interstitialsShown = 7;
            save.consentResolved = true;
            DailyCounter.TryUse(save.freeGemAds, 20261006, 5);

            var loaded = SaveData.FromJson(save.ToJson());
            Assert.AreEqual(42, loaded.gems);
            CollectionAssert.AreEqual(new[] { "floor.1" }, loaded.earlyUnlocked);
            Assert.AreEqual(2, UnlockSchedule.UnlockedFloorCount(loaded));
            Assert.IsTrue(loaded.removeAds);
            Assert.IsTrue(loaded.starterPackOwned);
            Assert.IsTrue(loaded.starterCurrencyGranted);
            CollectionAssert.AreEqual(new[] { "GPA.1234" }, loaded.transactions);
            Assert.AreEqual(7, loaded.interstitialsShown);
            Assert.IsTrue(loaded.consentResolved);
            Assert.AreEqual(20261006, loaded.freeGemAds.day);
            Assert.AreEqual(1, loaded.freeGemAds.count);
        }

        [Test]
        public void BrokenMonetizationFieldsAreRepaired()
        {
            var ids = new System.Text.StringBuilder();
            for (int i = 0; i < SaveData.MaxTransactions + 20; i++)
                ids.Append(i == 0 ? "" : ",").Append("\"t").Append(i).Append('"');
            string json =
                "{\"version\":2,\"gems\":-5,\"interstitialsShown\":-1," +
                "\"earlyUnlocked\":[\"floor.1\",\"floor.1\",\"skin.ninja\",\"decor.f1.left.0\",\"skin.dj\"]," +
                "\"threeStarGemPaid\":[1,1,0,-4,2]," +
                "\"transactions\":[" + ids + ",\"\",\"t5\"]}";

            var save = SaveData.FromJson(json);
            Assert.AreEqual(0, save.gems);
            Assert.AreEqual(0, save.interstitialsShown);
            CollectionAssert.AreEqual(new[] { "floor.1", "skin.dj" }, save.earlyUnlocked);
            CollectionAssert.AreEqual(new[] { 1, 2 }, save.threeStarGemPaid);
            Assert.AreEqual(SaveData.MaxTransactions, save.transactions.Count);
            Assert.AreEqual("t" + (SaveData.MaxTransactions + 19), save.transactions[save.transactions.Count - 1]);
            CollectionAssert.DoesNotContain(save.transactions, "t0");
        }

        [Test]
        public void CorruptSaveStillLoadsFresh()
        {
            var save = SaveData.FromJson("{\"coins\": 12, \"owned\": [");
            Assert.AreEqual(0, save.coins);
            Assert.AreEqual(1, save.currentLevel);
            Assert.AreEqual(SaveMigration.CurrentVersion, save.version);
            Assert.AreEqual(Catalog.ColorSlotCount, save.equippedSkins.Count);
        }
    }
}
