using System;
using System.Collections.Generic;
using UnityEngine;

namespace ElevatorRush
{
    /// <summary>
    /// Saved progress (plan 5.10, home plan 10), stored as JSON under one PlayerPrefs key.
    /// A missing or corrupt save loads as a fresh one; older saves are brought up to date by
    /// <see cref="SaveMigration"/>. Only lists and arrays, because JsonUtility cannot store dictionaries.
    /// </summary>
    [Serializable]
    public sealed class SaveData
    {
        public const string PrefsKey = "ElevatorRush.Save";

        /// <summary>Format version. Saves from before the home screen have none and read as 0;
        /// <see cref="SaveMigration.Migrate"/> brings every loaded save to the current version.</summary>
        public int version;

        public int currentLevel = 1;
        public int coins;
        /// <summary>Best stars per level; index 0 is level 1. A won level always has at least one star.</summary>
        public List<int> bestStars = new List<int>();
        public int endlessHighScore;
        public bool endlessUnlocked;

        // ---- Settings. Stored as "off" flags so saves without them load with everything on. ----
        /// <summary>Sound effects off.</summary>
        public bool muted;
        public bool musicMuted;
        public bool vibrationMuted;

        // ---- Home tower (home plan 5, 6, 7, 8) ----
        /// <summary>Ids of every bought decor option, character skin and tower skin.</summary>
        public List<string> owned = new List<string>();
        /// <summary>Equipped decor ids, at most one per floor and slot. A slot with none is empty.</summary>
        public List<string> equippedDecor = new List<string>();
        /// <summary>Floors whose completion bonus has been paid.</summary>
        public List<int> floorBonusPaid = new List<int>();
        public List<UpgradeLevel> upgrades = new List<UpgradeLevel>();
        /// <summary>Equipped character skin id per color slot; index is the destination floor.</summary>
        public List<string> equippedSkins = new List<string>();
        public string equippedTower = Catalog.DefaultTower;

        // ---- Unlock popups and red dots (home plan 2, 3.1) ----
        /// <summary>Unlock ids already shown in an "Unlocked!" popup.</summary>
        public List<string> unlocksShown = new List<string>();
        /// <summary>Item ids the player has already looked at, so they no longer show a red dot.</summary>
        public List<string> viewed = new List<string>();

        // ---- Gems, purchases and ads (monetization plan 8) ----
        public int gems;
        /// <summary>Floors, character skins and tower skins unlocked early with gems, before their level.</summary>
        public List<string> earlyUnlocked = new List<string>();
        public bool removeAds;
        public bool starterPackOwned;
        /// <summary>The Starter Pack's gems and coins were paid; a restore on a fresh save pays only the skin.</summary>
        public bool starterCurrencyGranted;
        /// <summary>Store transaction ids already granted, newest last (at most <see cref="MaxTransactions"/>).</summary>
        public List<string> transactions = new List<string>();
        /// <summary>Levels that already paid their first three-star gem.</summary>
        public List<int> threeStarGemPaid = new List<int>();
        public DailyCount freeGemAds = new DailyCount();
        /// <summary>Interstitials shown so far, for the "Remove ads?" prompt.</summary>
        public int interstitialsShown;
        public bool consentResolved;

        public const int MaxTransactions = 200;

        /// <summary>Number of distinct levels won.</summary>
        public int LevelsCompleted
        {
            get
            {
                int n = 0;
                for (int i = 0; i < bestStars.Count; i++)
                    if (bestStars[i] > 0)
                        n++;
                return n;
            }
        }

        public int BestStars(int level)
        {
            int i = level - 1;
            return i >= 0 && i < bestStars.Count ? bestStars[i] : 0;
        }

        /// <summary>Records a won level: best stars, the next level to play, and the Endless unlock.</summary>
        public void RecordLevelComplete(int level, int stars)
        {
            while (bestStars.Count < level)
                bestStars.Add(0);
            bestStars[level - 1] = Math.Max(bestStars[level - 1], Math.Max(1, stars));
            currentLevel = Math.Max(currentLevel, level + 1);
            if (level >= LevelCatalog.EndlessUnlockLevel)
                endlessUnlocked = true;
        }

        /// <summary>Returns true when this is a new high score.</summary>
        public bool RecordEndlessScore(int score)
        {
            if (score <= endlessHighScore)
                return false;
            endlessHighScore = score;
            return true;
        }

        // ---- Owned items ----

        /// <summary>True for bought items, and always for the free default skin and tower.</summary>
        public bool Owns(string id) => id == Catalog.DefaultSkin || id == Catalog.DefaultTower || owned.Contains(id);

        public void AddOwned(string id)
        {
            if (!owned.Contains(id))
                owned.Add(id);
        }

        public bool IsEarlyUnlocked(string id) => earlyUnlocked.Contains(id);

        public void AddEarlyUnlock(string id)
        {
            if (!earlyUnlocked.Contains(id))
                earlyUnlocked.Add(id);
        }

        // ---- Decor ----

        /// <summary>The equipped option (0 to 2) in a slot, or -1 when the slot is empty.</summary>
        public int EquippedDecor(int floor, int slot)
        {
            string prefix = Catalog.DecorSlotPrefix(floor, slot);
            for (int i = 0; i < equippedDecor.Count; i++)
            {
                var item = Catalog.Find(equippedDecor[i]);
                if (item != null && item.kind == ItemKind.Decor && equippedDecor[i].StartsWith(prefix, StringComparison.Ordinal))
                    return item.option;
            }
            return -1;
        }

        /// <summary>Puts a decor option in its slot, replacing whatever was there.</summary>
        public void SetEquippedDecor(CatalogItem decor)
        {
            string prefix = Catalog.DecorSlotPrefix(decor.floor, decor.slot);
            equippedDecor.RemoveAll(id => id.StartsWith(prefix, StringComparison.Ordinal));
            equippedDecor.Add(decor.id);
        }

        public bool IsBonusPaid(int floor) => floorBonusPaid.Contains(floor);

        // ---- Elevator ----

        public int UpgradeTier(string id)
        {
            for (int i = 0; i < upgrades.Count; i++)
                if (upgrades[i].id == id)
                    return upgrades[i].tier;
            return 0;
        }

        public int UpgradeTier(UpgradeKind kind) => UpgradeTier(Catalog.Upgrade(kind).id);

        public void SetUpgradeTier(string id, int tier)
        {
            for (int i = 0; i < upgrades.Count; i++)
                if (upgrades[i].id == id)
                {
                    upgrades[i].tier = tier;
                    return;
                }
            upgrades.Add(new UpgradeLevel(id, tier));
        }

        /// <summary>Sum of all bought elevator tiers (drives the cabin look).</summary>
        public int TotalUpgradeTiers
        {
            get
            {
                int n = 0;
                for (int i = 0; i < upgrades.Count; i++)
                    n += upgrades[i].tier;
                return n;
            }
        }

        // ---- Character skins ----

        public string EquippedSkin(int colorSlot)
        {
            return colorSlot >= 0 && colorSlot < equippedSkins.Count ? equippedSkins[colorSlot] : Catalog.DefaultSkin;
        }

        public void SetEquippedSkin(int colorSlot, string skinId)
        {
            while (equippedSkins.Count <= colorSlot)
                equippedSkins.Add(Catalog.DefaultSkin);
            equippedSkins[colorSlot] = skinId;
        }

        // ---- JSON and PlayerPrefs ----

        public string ToJson() => JsonUtility.ToJson(this);

        public static SaveData FromJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return SaveMigration.Migrate(new SaveData());
            try
            {
                var data = JsonUtility.FromJson<SaveData>(json);
                return SaveMigration.Migrate(data != null ? data : new SaveData());
            }
            catch (Exception)
            {
                return SaveMigration.Migrate(new SaveData());
            }
        }

        public static SaveData Load() => FromJson(PlayerPrefs.GetString(PrefsKey, string.Empty));

        public void Save()
        {
            PlayerPrefs.SetString(PrefsKey, ToJson());
            PlayerPrefs.Save();
        }

        public static void Delete()
        {
            PlayerPrefs.DeleteKey(PrefsKey);
            PlayerPrefs.Save();
        }
    }
}
