namespace ElevatorRush
{
    public enum UpgradeKind
    {
        Motor,
        Doors,
        Comfort,
        Tips,
        Capacity
    }

    /// <summary>One elevator upgrade (home plan 6): price and tier cap needed for each tier.</summary>
    public sealed class UpgradeDef
    {
        public readonly string id;
        public readonly UpgradeKind kind;
        public readonly string name;
        /// <summary>Price of tier t at index t - 1.</summary>
        public readonly int[] prices;
        /// <summary>Elevator tier cap (home plan 4.1) needed to buy tier t, at index t - 1.</summary>
        public readonly int[] capNeeded;
        /// <summary>Change per tier: a fraction for Motor, Doors, Comfort and Tips, slots for Capacity.</summary>
        public readonly float perTier;
        /// <summary>Next-tier effect in plain words, with {0} for the percent or slot count.</summary>
        public readonly string effectText;

        public int MaxTier => prices.Length;

        public UpgradeDef(string id, UpgradeKind kind, string name, int[] prices, int[] capNeeded,
            float perTier, string effectText)
        {
            this.id = id;
            this.kind = kind;
            this.name = name;
            this.prices = prices;
            this.capNeeded = capNeeded;
            this.perTier = perTier;
            this.effectText = effectText;
        }

        /// <summary>Price of a 1-based tier.</summary>
        public int PriceOf(int tier) => prices[tier - 1];

        /// <summary>Tier cap needed for a 1-based tier.</summary>
        public int CapNeeded(int tier) => capNeeded[tier - 1];
    }
}
