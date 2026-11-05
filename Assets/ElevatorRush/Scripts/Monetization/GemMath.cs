namespace ElevatorRush
{
    /// <summary>
    /// Pure gem prices (monetization plan 2.2 and 2.3): gems that cover a coin shortfall, and the
    /// gem price of unlocking a floor, skin, tower skin or elevator tier before its level.
    /// </summary>
    public static class GemMath
    {
        /// <summary>Gems that cover this many coins, rounded up (0 for none).</summary>
        public static int GemsForCoins(int coins)
        {
            return coins <= 0 ? 0 : (coins + Catalog.CoinsPerGem - 1) / Catalog.CoinsPerGem;
        }

        /// <summary>Gems needed on top of all the player's coins to pay a coin price; 0 when the coins suffice.</summary>
        public static int ShortfallGems(int price, int coins) => GemsForCoins(price - coins);

        /// <summary>Early unlock of something with a coin price: ceil(coinPrice / 10 * 2).</summary>
        public static int EarlyUnlockGems(int coinPrice)
        {
            int scaled = coinPrice * Catalog.EarlyUnlockGemFactor;
            return scaled <= 0 ? 0 : (scaled + Catalog.CoinsPerGem - 1) / Catalog.CoinsPerGem;
        }

        /// <summary>Early unlock of floor k: 200 x k. Floor 0 is always open.</summary>
        public static int FloorUnlockGems(int floor) => Catalog.FloorGemStep * floor;

        public static int EarlyUnlockGems(UpgradeDef upgrade, int tier) => EarlyUnlockGems(upgrade.PriceOf(tier));

        /// <summary>Early unlock price of a floor, character skin or tower skin; -1 for anything else.</summary>
        public static int EarlyUnlockGems(CatalogItem item)
        {
            if (item == null)
                return -1;
            switch (item.kind)
            {
                case ItemKind.Floor: return FloorUnlockGems(item.floor);
                case ItemKind.Skin:
                case ItemKind.Tower: return EarlyUnlockGems(item.price);
                default: return -1;
            }
        }

        /// <summary>Floors unlock in order: only the first locked floor can be unlocked early.</summary>
        public static bool IsNextFloor(int floor, int unlockedFloorCount) => floor == unlockedFloorCount && floor < Catalog.FloorCount;
    }
}
