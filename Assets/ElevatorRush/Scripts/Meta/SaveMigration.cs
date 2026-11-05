using System;
using System.Collections.Generic;

namespace ElevatorRush
{
    /// <summary>
    /// Pure: brings a loaded save up to the current format and repairs it (home plan 10).
    /// Coins, level, stars and Endless progress are always kept. Missing lists come back empty,
    /// unknown item ids are dropped without errors, and equipped items always refer to owned ones.
    /// </summary>
    public static class SaveMigration
    {
        /// <summary>0: the original game. 1: home tower, upgrades, skins, settings. 2: gems, purchases and ads.</summary>
        public const int CurrentVersion = 2;

        public static SaveData Migrate(SaveData save)
        {
            if (save == null)
                save = new SaveData();

            RepairCore(save);
            RepairLists(save);

            if (save.version < 1)
                FromVersion0(save);

            RepairHome(save);
            RepairMonetization(save);

            if (save.version < 2)
                FromVersion1(save);

            save.version = Math.Max(save.version, CurrentVersion);
            return save;
        }

        /// <summary>
        /// Things unlocked before the home screen existed are not announced one by one with
        /// popups; they show red dots instead (nothing is in <c>viewed</c> yet).
        /// </summary>
        static void FromVersion0(SaveData save)
        {
            int done = save.LevelsCompleted;
            foreach (var id in UnlockSchedule.UnlockedIds(done))
                AddOnce(save.unlocksShown, id);
        }

        /// <summary>
        /// Gems did not exist yet: pay the free gems the player already earned (monetization plan
        /// 2.1), so existing players start with some and learn what gems do.
        /// </summary>
        static void FromVersion1(SaveData save)
        {
            int gems = 0;
            for (int level = 1; level <= save.bestStars.Count; level++)
                if (save.BestStars(level) >= ScoreSystem.MaxStars && !save.threeStarGemPaid.Contains(level))
                {
                    save.threeStarGemPaid.Add(level);
                    gems += Catalog.GemsPerThreeStars;
                }
            gems += Catalog.GemsPerUnlockRow * UnlockSchedule.RowsCrossed(0, save.LevelsCompleted);
            gems += Catalog.GemsPerFloorComplete * save.floorBonusPaid.Count;
            save.gems += gems;
        }

        static void RepairCore(SaveData save)
        {
            if (save.currentLevel < 1)
                save.currentLevel = 1;
            if (save.coins < 0)
                save.coins = 0;
            if (save.endlessHighScore < 0)
                save.endlessHighScore = 0;
            if (save.bestStars == null)
                save.bestStars = new List<int>();
            for (int i = 0; i < save.bestStars.Count; i++)
                save.bestStars[i] = Math.Max(0, Math.Min(ScoreSystem.MaxStars, save.bestStars[i]));
        }

        static void RepairLists(SaveData save)
        {
            save.owned = save.owned ?? new List<string>();
            save.equippedDecor = save.equippedDecor ?? new List<string>();
            save.floorBonusPaid = save.floorBonusPaid ?? new List<int>();
            save.upgrades = save.upgrades ?? new List<UpgradeLevel>();
            save.equippedSkins = save.equippedSkins ?? new List<string>();
            save.unlocksShown = save.unlocksShown ?? new List<string>();
            save.viewed = save.viewed ?? new List<string>();
            save.earlyUnlocked = save.earlyUnlocked ?? new List<string>();
            save.transactions = save.transactions ?? new List<string>();
            save.threeStarGemPaid = save.threeStarGemPaid ?? new List<int>();
            save.freeGemAds = save.freeGemAds ?? new DailyCount();
        }

        static void RepairHome(SaveData save)
        {
            // Owned: known buyable items only, no duplicates.
            save.owned = Distinct(save.owned, id =>
            {
                var item = Catalog.Find(id);
                return item != null && (item.kind == ItemKind.Decor || item.kind == ItemKind.Skin || item.kind == ItemKind.Tower);
            });

            // Equipped decor: owned decor, one per slot (the first one listed wins).
            var decor = new List<string>();
            var filled = new HashSet<string>();
            foreach (var id in save.equippedDecor)
            {
                var item = Catalog.Find(id);
                if (item == null || item.kind != ItemKind.Decor || !save.Owns(id))
                    continue;
                if (filled.Add(Catalog.DecorSlotPrefix(item.floor, item.slot)))
                    decor.Add(id);
            }
            save.equippedDecor = decor;

            // Bonus floors: valid floor numbers, once each.
            var floors = new List<int>();
            foreach (int f in save.floorBonusPaid)
                if (f >= 0 && f < Catalog.FloorCount && !floors.Contains(f))
                    floors.Add(f);
            save.floorBonusPaid = floors;

            // Upgrades: known ids, tiers within range, one entry per upgrade.
            var upgrades = new List<UpgradeLevel>();
            foreach (var level in save.upgrades)
            {
                var def = level != null ? Catalog.FindUpgrade(level.id) : null;
                if (def == null || upgrades.Exists(u => u.id == level.id))
                    continue;
                int tier = Math.Max(0, Math.Min(def.MaxTier, level.tier));
                if (tier > 0)
                    upgrades.Add(new UpgradeLevel(level.id, tier));
            }
            save.upgrades = upgrades;

            // Skins: exactly one owned skin per color slot.
            var skins = new List<string>(Catalog.ColorSlotCount);
            for (int i = 0; i < Catalog.ColorSlotCount; i++)
            {
                string id = i < save.equippedSkins.Count ? save.equippedSkins[i] : null;
                var item = Catalog.Find(id);
                skins.Add(item != null && item.kind == ItemKind.Skin && save.Owns(id) ? id : Catalog.DefaultSkin);
            }
            save.equippedSkins = skins;

            var tower = Catalog.Find(save.equippedTower);
            if (tower == null || tower.kind != ItemKind.Tower || !save.Owns(save.equippedTower))
                save.equippedTower = Catalog.DefaultTower;

            save.unlocksShown = Distinct(save.unlocksShown, id => Catalog.IsKnown(id));
            save.viewed = Distinct(save.viewed, id => Catalog.IsKnown(id));
        }

        static void RepairMonetization(SaveData save)
        {
            if (save.gems < 0)
                save.gems = 0;
            if (save.interstitialsShown < 0)
                save.interstitialsShown = 0;
            if (save.freeGemAds.count < 0)
                save.freeGemAds.count = 0;

            // Early unlocks: floors, skins and tower skins only. Floors count only while unlocked in order.
            save.earlyUnlocked = Distinct(save.earlyUnlocked, id =>
            {
                var item = Catalog.Find(id);
                return item != null && (item.kind == ItemKind.Floor || item.kind == ItemKind.Skin || item.kind == ItemKind.Tower);
            });

            // Transactions: no blanks or duplicates, the newest ones kept.
            var transactions = Distinct(save.transactions, id => id.Length > 0);
            if (transactions.Count > SaveData.MaxTransactions)
                transactions.RemoveRange(0, transactions.Count - SaveData.MaxTransactions);
            save.transactions = transactions;

            var paid = new List<int>();
            foreach (int level in save.threeStarGemPaid)
                if (level >= 1 && !paid.Contains(level))
                    paid.Add(level);
            save.threeStarGemPaid = paid;
        }

        static List<string> Distinct(List<string> ids, Predicate<string> keep)
        {
            var result = new List<string>(ids.Count);
            foreach (var id in ids)
                if (id != null && keep(id) && !result.Contains(id))
                    result.Add(id);
            return result;
        }

        static void AddOnce(List<string> list, string id)
        {
            if (!list.Contains(id))
                list.Add(id);
        }
    }
}
