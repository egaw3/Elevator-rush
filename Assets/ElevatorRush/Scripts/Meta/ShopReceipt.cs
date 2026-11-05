namespace ElevatorRush
{
    public enum ShopResult
    {
        Ok,
        UnknownItem,
        /// <summary>Not unlocked yet at the current levels completed (or above the elevator tier cap).</summary>
        Locked,
        NotEnoughCoins,
        AlreadyOwned,
        /// <summary>The upgrade is already at its last tier.</summary>
        MaxedOut,
        /// <summary>Elevator tiers must be bought one after another; floors unlock early in order.</summary>
        OutOfOrder,
        /// <summary>Equipping something that has not been bought.</summary>
        NotOwned,
        /// <summary>Equipping a character skin on a color slot whose floor is still locked.</summary>
        SlotLocked,
        /// <summary>Not enough gems for an early unlock or to cover a coin shortfall.</summary>
        NotEnoughGems,
        /// <summary>An early unlock of something that is already unlocked (buy it with coins instead).</summary>
        NotLocked
    }

    /// <summary>
    /// What a purchase did: the result, coins and gems spent, and any floor completion bonus paid
    /// in coins and gems.
    /// </summary>
    public readonly struct ShopReceipt
    {
        public readonly ShopResult result;
        public readonly int spent;
        public readonly int bonus;
        public readonly int gemsSpent;
        public readonly int bonusGems;

        public bool Ok => result == ShopResult.Ok;

        public ShopReceipt(ShopResult result, int spent = 0, int bonus = 0, int gemsSpent = 0, int bonusGems = 0)
        {
            this.result = result;
            this.spent = spent;
            this.bonus = bonus;
            this.gemsSpent = gemsSpent;
            this.bonusGems = bonusGems;
        }

        public static implicit operator ShopReceipt(ShopResult result) => new ShopReceipt(result);
    }
}
