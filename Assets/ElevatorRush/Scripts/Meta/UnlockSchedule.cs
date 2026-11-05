using System;
using System.Collections.Generic;

namespace ElevatorRush
{
    /// <summary>
    /// Pure unlock schedule (home plan 4.1): what is available at a given number of levels
    /// completed, what unlocked between two counts, and the next goal for the teaser (4.2).
    /// </summary>
    public static class UnlockSchedule
    {
        public static readonly IReadOnlyList<UnlockRow> Rows = BuildRows();

        static IReadOnlyList<UnlockRow> BuildRows()
        {
            var byThreshold = new SortedDictionary<int, List<string>>();
            void Add(int at, string id)
            {
                if (!byThreshold.TryGetValue(at, out var list))
                    byThreshold.Add(at, list = new List<string>());
                list.Add(id);
            }

            for (int f = 0; f < Catalog.FloorCount; f++)
                Add(Catalog.FloorUnlockAt(f), Catalog.FloorId(f));
            foreach (var tower in Catalog.Towers)
                Add(tower.unlockAt, tower.id);
            for (int t = 1; t <= Catalog.MaxTierCap; t++)
                Add(Catalog.TierUnlockAt(t), Catalog.TierId(t));
            foreach (var skin in Catalog.Skins)
                Add(skin.unlockAt, skin.id);

            var rows = new List<UnlockRow>(byThreshold.Count);
            foreach (var pair in byThreshold)
                rows.Add(new UnlockRow(pair.Key, pair.Value));
            return rows;
        }

        public static int TierCap(int levelsCompleted) => Catalog.TierCap(levelsCompleted);

        public static bool IsFloorUnlocked(int floor, int levelsCompleted)
        {
            return floor >= 0 && floor < Catalog.FloorCount && levelsCompleted >= Catalog.FloorUnlockAt(floor);
        }

        /// <summary>Character color slot k opens with floor k.</summary>
        public static bool IsColorSlotUnlocked(int colorSlot, int levelsCompleted) => IsFloorUnlocked(colorSlot, levelsCompleted);

        /// <summary>Number of floors open for decorating, 1 to 8.</summary>
        public static int UnlockedFloorCount(int levelsCompleted)
        {
            return Math.Min(Catalog.FloorCount, 1 + Math.Max(0, levelsCompleted) / Catalog.LevelsPerFloor);
        }

        /// <summary>Levels completed needed for any item or upgrade tier id; -1 for unknown ids.</summary>
        public static int UnlockAt(string id)
        {
            var item = Catalog.Find(id);
            if (item != null)
                return item.unlockAt;
            return Catalog.FindUpgrade(id) != null ? 0 : -1;
        }

        /// <summary>Levels completed needed to buy a 1-based tier of an upgrade.</summary>
        public static int UnlockAt(UpgradeDef upgrade, int tier) => Catalog.TierUnlockAt(upgrade.CapNeeded(tier));

        public static bool IsUnlocked(string id, int levelsCompleted)
        {
            int at = UnlockAt(id);
            return at >= 0 && levelsCompleted >= at;
        }

        public static bool IsTierUnlocked(UpgradeDef upgrade, int tier, int levelsCompleted)
        {
            return tier >= 1 && tier <= upgrade.MaxTier && upgrade.CapNeeded(tier) <= TierCap(levelsCompleted);
        }

        /// <summary>Every unlock id available at this count, in table order.</summary>
        public static List<string> UnlockedIds(int levelsCompleted)
        {
            var ids = new List<string>();
            foreach (var row in Rows)
                if (row.threshold <= levelsCompleted)
                    ids.AddRange(row.ids);
            return ids;
        }

        /// <summary>Unlock ids crossed when the count went from <paramref name="before"/> to <paramref name="after"/>.</summary>
        public static List<string> NewlyUnlocked(int before, int after)
        {
            var ids = new List<string>();
            foreach (var row in Rows)
                if (row.threshold > before && row.threshold <= after)
                    ids.AddRange(row.ids);
            return ids;
        }

        /// <summary>Rows (each paying the unlock-row gems) crossed when the count went from <paramref name="before"/> to <paramref name="after"/>.</summary>
        public static int RowsCrossed(int before, int after)
        {
            int n = 0;
            foreach (var row in Rows)
                if (row.threshold > before && row.threshold <= after)
                    n++;
            return n;
        }

        // ---- With early unlocks (monetization plan 2.3): unlocked by level, or bought early with gems ----

        public static bool IsFloorUnlocked(int floor, SaveData save)
        {
            return IsFloorUnlocked(floor, save.LevelsCompleted)
                || (floor >= 0 && floor < Catalog.FloorCount && save.IsEarlyUnlocked(Catalog.FloorId(floor)));
        }

        public static bool IsColorSlotUnlocked(int colorSlot, SaveData save) => IsFloorUnlocked(colorSlot, save);

        /// <summary>Open floors, counted from the bottom while they stay unlocked (floors unlock in order).</summary>
        public static int UnlockedFloorCount(SaveData save)
        {
            int n = 1;
            while (n < Catalog.FloorCount && IsFloorUnlocked(n, save))
                n++;
            return n;
        }

        /// <summary>An item unlocked by level or early; decor follows its floor.</summary>
        public static bool IsUnlocked(string id, SaveData save)
        {
            if (IsUnlocked(id, save.LevelsCompleted) || save.IsEarlyUnlocked(id))
                return true;
            var item = Catalog.Find(id);
            return item != null && item.kind == ItemKind.Decor && IsFloorUnlocked(item.floor, save);
        }

        /// <summary>The first row still ahead, or null after the last row.</summary>
        public static UnlockRow NextRow(int levelsCompleted)
        {
            foreach (var row in Rows)
                if (row.threshold > levelsCompleted)
                    return row;
            return null;
        }

        /// <summary>The last row already reached (the start row at 0).</summary>
        public static UnlockRow PreviousRow(int levelsCompleted)
        {
            UnlockRow previous = Rows[0];
            foreach (var row in Rows)
                if (row.threshold <= levelsCompleted)
                    previous = row;
            return previous;
        }

        /// <summary>The teaser (home plan 4.2): next item, levels to go, and the bar fill 0 to 1.</summary>
        public static NextUnlock Next(int levelsCompleted)
        {
            var next = NextRow(levelsCompleted);
            if (next == null)
                return new NextUnlock(null, 0, 0, 1f);
            int from = PreviousRow(levelsCompleted).threshold;
            float fill = (float)(levelsCompleted - from) / (next.threshold - from);
            return new NextUnlock(next.ids[0], next.threshold, next.threshold - levelsCompleted, fill);
        }
    }
}
