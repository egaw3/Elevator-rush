using System;

namespace ElevatorRush
{
    /// <summary>
    /// Pure: turns saved elevator tiers into the effective values for a run (home plan 6).
    /// Falling speed, snapRange, snapSpeed and perfectRange are never touched.
    /// </summary>
    public static class UpgradeMath
    {
        // Run coins are summed as floats; this absorbs float error so 10 x 1.1 rounds to 11, not 12.
        const double RoundingTolerance = 1e-4;

        public static RunUpgrades Apply(GameConfig baseConfig, SaveData save)
        {
            return Apply(baseConfig,
                save.UpgradeTier(UpgradeKind.Motor),
                save.UpgradeTier(UpgradeKind.Doors),
                save.UpgradeTier(UpgradeKind.Comfort),
                save.UpgradeTier(UpgradeKind.Tips),
                save.UpgradeTier(UpgradeKind.Capacity));
        }

        public static RunUpgrades Apply(GameConfig baseConfig, int motor, int doors, int comfort, int tips, int capacity)
        {
            var config = baseConfig.Clone();
            float speed = Factor(UpgradeKind.Motor, motor);
            config.riseAccel *= speed;
            config.maxRiseSpeed *= speed;

            float doorTime = Factor(UpgradeKind.Doors, doors);
            config.dwellTime *= doorTime;
            config.exchangeTime *= doorTime;

            int extraSlots = (int)Math.Round(Catalog.Upgrade(UpgradeKind.Capacity).perTier * Clamp(UpgradeKind.Capacity, capacity));
            config.cabinCapacity += extraSlots;
            // A wider cabin and shaft, so 5 or 6 riders fit visibly.
            config.cabinWidth += extraSlots * config.cabinWidthPerExtraSlot;
            config.shaftWidth += extraSlots * config.cabinWidthPerExtraSlot;

            return new RunUpgrades(config, Factor(UpgradeKind.Comfort, comfort), Factor(UpgradeKind.Tips, tips));
        }

        /// <summary>
        /// Multiplier for a tier: 1 + perTier x tier, or 1 - perTier x tier for Doors (times shrink).
        /// Tier 0 gives exactly 1.
        /// </summary>
        public static float Factor(UpgradeKind kind, int tier)
        {
            float change = Catalog.Upgrade(kind).perTier * Clamp(kind, tier);
            return kind == UpgradeKind.Doors ? 1f - change : 1f + change;
        }

        /// <summary>Whole percent shown for one tier, for example 8 for "Doors open 8% faster".</summary>
        public static int PercentPerTier(UpgradeKind kind) => (int)Math.Round(Catalog.Upgrade(kind).perTier * 100f);

        /// <summary>Cosmetic cabin look from the total tiers owned: 0 plain, 1 trim and lights, 2 glass and gold.</summary>
        public static int CabinLook(int totalTiers)
        {
            return totalTiers >= Catalog.CabinGlassTiers ? 2 : totalTiers >= Catalog.CabinTrimTiers ? 1 : 0;
        }

        /// <summary>Rounds a run's float coin total up once, at the end of the run.</summary>
        public static int RoundCoins(double coins)
        {
            if (coins <= 0)
                return 0;
            return (int)Math.Ceiling(coins - RoundingTolerance);
        }

        /// <summary>Whole coins already earned in a run total, safe from float error (the part to bank now).</summary>
        public static int WholeCoins(double coins)
        {
            if (coins <= 0)
                return 0;
            return (int)Math.Floor(coins + RoundingTolerance);
        }

        static int Clamp(UpgradeKind kind, int tier) => Math.Max(0, Math.Min(Catalog.Upgrade(kind).MaxTier, tier));
    }
}
