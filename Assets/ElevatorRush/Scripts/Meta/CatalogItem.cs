namespace ElevatorRush
{
    public enum ItemKind
    {
        Decor,
        Skin,
        Tower,
        /// <summary>A floor of the home tower and its character color slot.</summary>
        Floor,
        /// <summary>A raise of the elevator tier cap.</summary>
        ElevatorTier
    }

    /// <summary>
    /// One thing the player can unlock or buy (home plan 4 and 9). Ids are stable strings that
    /// are stored in saves, so they must never be renamed or renumbered.
    /// </summary>
    public sealed class CatalogItem
    {
        public readonly string id;
        public readonly ItemKind kind;
        public readonly string name;
        public readonly int price;
        /// <summary>Levels completed needed before the item can be bought.</summary>
        public readonly int unlockAt;
        /// <summary>Decor only: floor, slot and option (0 basic, 1 nice, 2 premium). -1 otherwise.</summary>
        public readonly int floor;
        public readonly int slot;
        public readonly int option;

        public CatalogItem(string id, ItemKind kind, string name, int price, int unlockAt,
            int floor = -1, int slot = -1, int option = -1)
        {
            this.id = id;
            this.kind = kind;
            this.name = name;
            this.price = price;
            this.unlockAt = unlockAt;
            this.floor = floor;
            this.slot = slot;
            this.option = option;
        }
    }
}
