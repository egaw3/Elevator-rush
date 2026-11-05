namespace ElevatorRush
{
    /// <summary>
    /// Pure home-tower progress (home plan 5.2): filled slots, complete floors, the one-time
    /// completion bonus, and the tower percent out of all 48 slots.
    /// </summary>
    public static class TowerProgress
    {
        public static int FilledSlots(SaveData save, int floor)
        {
            int n = 0;
            for (int s = 0; s < Catalog.SlotsPerFloor; s++)
                if (save.EquippedDecor(floor, s) >= 0)
                    n++;
            return n;
        }

        public static bool IsFloorComplete(SaveData save, int floor) => FilledSlots(save, floor) == Catalog.SlotsPerFloor;

        public static int TotalFilled(SaveData save)
        {
            int n = 0;
            for (int f = 0; f < Catalog.FloorCount; f++)
                n += FilledSlots(save, f);
            return n;
        }

        /// <summary>Filled slots over all slots, 0 to 1.</summary>
        public static float Fraction(SaveData save) => (float)TotalFilled(save) / Catalog.TotalSlots;

        /// <summary>Whole percent for "Tower 34%", rounded down so 100 means every slot is filled.</summary>
        public static int Percent(SaveData save) => TotalFilled(save) * 100 / Catalog.TotalSlots;

        /// <summary>
        /// Pays the completion bonus if the floor is complete and has not been paid before.
        /// Returns the coins paid, 0 otherwise. The bonus also pays <see cref="Catalog.GemsPerFloorComplete"/>
        /// gems (monetization plan 2.1).
        /// </summary>
        public static int TryPayCompletionBonus(SaveData save, int floor)
        {
            if (!IsFloorComplete(save, floor) || save.IsBonusPaid(floor))
                return 0;
            int bonus = Catalog.FloorCompletionBonus(floor);
            var wallet = new Wallet(save);
            wallet.Add(bonus);
            wallet.AddGems(Catalog.GemsPerFloorComplete);
            save.floorBonusPaid.Add(floor);
            return bonus;
        }
    }
}
