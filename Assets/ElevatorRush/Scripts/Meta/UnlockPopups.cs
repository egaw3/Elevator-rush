using System.Collections.Generic;

namespace ElevatorRush
{
    /// <summary>
    /// Pure "Unlocked!" popup bookkeeping (home plan 2 and H7): every unlock id that has been
    /// reached but not yet shown, in table order. Each is shown exactly once, because showing it
    /// records it in the save. Items already unlocked early with gems are never announced.
    /// </summary>
    public static class UnlockPopups
    {
        public static List<string> Pending(SaveData save)
        {
            var pending = new List<string>();
            foreach (var id in UnlockSchedule.UnlockedIds(save.LevelsCompleted))
                if (!save.unlocksShown.Contains(id) && !save.IsEarlyUnlocked(id))
                    pending.Add(id);
            return pending;
        }

        public static void MarkShown(SaveData save, string id)
        {
            if (!save.unlocksShown.Contains(id))
                save.unlocksShown.Add(id);
        }

        /// <summary>Where the player finds a newly unlocked item, for the popup's hint line.</summary>
        public static string HintOf(string id)
        {
            var item = Catalog.Find(id);
            if (item == null)
                return "";
            switch (item.kind)
            {
                case ItemKind.Floor: return "A new floor to decorate, and a new character color";
                case ItemKind.Skin: return "Now in Characters";
                case ItemKind.Tower: return "Now in Towers";
                case ItemKind.ElevatorTier: return "New Elevator upgrades";
                default: return "";
            }
        }
    }
}
