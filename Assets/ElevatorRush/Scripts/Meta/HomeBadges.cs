namespace ElevatorRush
{
    public enum HomeTab
    {
        Build,
        Elevator,
        Characters,
        Towers
    }

    /// <summary>
    /// Pure red-dot rules for the Home tabs (home plan 3.1): a tab shows a dot when something in
    /// it is affordable and not yet owned, or was unlocked and has not been viewed yet. Things
    /// available from the very start, or unlocked early with gems, never count as newly unlocked.
    /// </summary>
    public static class HomeBadges
    {
        public static bool HasDot(SaveData save, HomeTab tab) => HasAffordable(save, tab) || HasUnviewed(save, tab);

        public static bool HasAffordable(SaveData save, HomeTab tab)
        {
            var shop = new Shop(save);
            switch (tab)
            {
                case HomeTab.Build:
                    int floors = UnlockSchedule.UnlockedFloorCount(save);
                    for (int f = 0; f < floors; f++)
                        for (int s = 0; s < Catalog.SlotsPerFloor; s++)
                            for (int o = 0; o < Catalog.OptionsPerSlot; o++)
                                if (shop.CheckBuy(Catalog.DecorId(f, s, o)) == ShopResult.Ok)
                                    return true;
                    return false;
                case HomeTab.Elevator:
                    foreach (var upgrade in Catalog.Upgrades)
                        if (shop.CheckBuy(upgrade.id) == ShopResult.Ok)
                            return true;
                    return false;
                case HomeTab.Characters:
                    foreach (var skin in Catalog.Skins)
                        if (shop.CheckBuy(skin.id) == ShopResult.Ok)
                            return true;
                    return false;
                default:
                    foreach (var tower in Catalog.Towers)
                        if (shop.CheckBuy(tower.id) == ShopResult.Ok)
                            return true;
                    return false;
            }
        }

        public static bool HasUnviewed(SaveData save, HomeTab tab)
        {
            foreach (var id in UnlockSchedule.UnlockedIds(save.LevelsCompleted))
                if (TabOf(id) == tab && UnlockSchedule.UnlockAt(id) > 0 && !save.viewed.Contains(id) && !save.IsEarlyUnlocked(id))
                    return true;
            return false;
        }

        /// <summary>Marks everything unlocked in a tab as viewed (the tab was opened). Returns true if anything changed.</summary>
        public static bool MarkViewed(SaveData save, HomeTab tab)
        {
            bool changed = false;
            foreach (var id in UnlockSchedule.UnlockedIds(save.LevelsCompleted))
                if (TabOf(id) == tab && !save.viewed.Contains(id))
                {
                    save.viewed.Add(id);
                    changed = true;
                }
            return changed;
        }

        /// <summary>The tab an unlock id belongs to. A floor also opens a character color slot, but its dot is on Build.</summary>
        public static HomeTab TabOf(string id)
        {
            var item = Catalog.Find(id);
            if (item == null)
                return HomeTab.Elevator;
            switch (item.kind)
            {
                case ItemKind.Skin: return HomeTab.Characters;
                case ItemKind.Tower: return HomeTab.Towers;
                case ItemKind.ElevatorTier: return HomeTab.Elevator;
                default: return HomeTab.Build;
            }
        }
    }
}
