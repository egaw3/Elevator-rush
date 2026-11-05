using System;
using System.Collections.Generic;

namespace ElevatorRush
{
    /// <summary>
    /// Every item, price and unlock threshold of the home-tower meta game, as data (home plan 4.1,
    /// 5, 6, 7, 8 and 9). Prices are first guesses; tune them with Elevator Rush/Economy Report.
    /// </summary>
    public static class Catalog
    {
        // ---- Floors and rooms (home plan 5) ----
        public const int FloorCount = 8;
        public const int SlotsPerFloor = 6;
        public const int OptionsPerSlot = 3;
        public const int TotalSlots = FloorCount * SlotsPerFloor;
        /// <summary>Floor k unlocks at this many levels completed times k.</summary>
        public const int LevelsPerFloor = 25;
        /// <summary>Completing floor k pays this times (k + 1), once.</summary>
        public const int FloorBonusStep = 100;

        // ---- Elevator (home plan 4.1 and 6) ----
        public const int MaxTierCap = 5;
        /// <summary>Total elevator tiers owned at which the cabin gains trim and lights, then turns glass and gold.</summary>
        public const int CabinTrimTiers = 6;
        public const int CabinGlassTiers = 14;

        public const string DefaultSkin = "skin.default";
        public const string DefaultTower = "tower.classic";
        /// <summary>Character color slots, one per destination color.</summary>
        public const int ColorSlotCount = FloorPalette.Count;

        /// <summary>Slot keys used in decor ids, in slot order.</summary>
        public static readonly string[] SlotKeys = { "flooring", "wall", "left", "center", "right", "wallArt" };
        public static readonly string[] SlotNames = { "Flooring", "Walls", "Left", "Center", "Right", "Wall art" };
        public static readonly string[] OptionGrades = { "Basic", "Nice", "Premium" };

        public const int SlotFlooring = 0;
        public const int SlotWall = 1;
        public const int SlotLeft = 2;
        public const int SlotCenter = 3;
        public const int SlotRight = 4;
        public const int SlotWallArt = 5;

        public static readonly string[] RoomNames = { "Lobby", "Cafe", "Office", "Gym", "Library", "Arcade", "Spa", "Penthouse" };

        /// <summary>Decor price for option 0, 1, 2 on floor 0; floor k multiplies by (1 + k).</summary>
        static readonly int[] DecorBasePrices = { 40, 90, 160 };

        // Option names per floor, per slot (flooring, wall, left, center, right, wallArt), basic to premium.
        static readonly string[][][] DecorNames =
        {
            new[] // 0 Lobby
            {
                new[] { "Checker Tiles", "Oak Parquet", "Marble Floor" },
                new[] { "Fresh Paint", "Wood Panels", "Marble Panels" },
                new[] { "Potted Plant", "Big Fern", "Palm Tree" },
                new[] { "Reception Desk", "Oak Reception", "Grand Reception" },
                new[] { "Bench", "Sofa", "Leather Sofa" },
                new[] { "Wall Clock", "Framed Poster", "Gold Clock" },
            },
            new[] // 1 Cafe
            {
                new[] { "Terracotta", "Checkerboard", "Herringbone" },
                new[] { "Cream Paint", "Brick Wall", "Tiled Wall" },
                new[] { "Coffee Machine", "Espresso Bar", "Copper Roaster" },
                new[] { "Counter", "Pastry Counter", "Marble Counter" },
                new[] { "Table and Chairs", "Booth", "Bistro Set" },
                new[] { "Chalk Menu", "Menu Board", "Neon Coffee Sign" },
            },
            new[] // 2 Office
            {
                new[] { "Carpet Tiles", "Wood Laminate", "Bamboo Floor" },
                new[] { "Office Grey", "Accent Stripe", "Glass Panels" },
                new[] { "Bookshelf", "Filing Wall", "Walnut Shelves" },
                new[] { "Desk with Computer", "Standing Desk", "Executive Desk" },
                new[] { "Water Cooler", "Plant Corner", "Coffee Station" },
                new[] { "Whiteboard", "Team Chart", "Glass Board" },
            },
            new[] // 3 Gym
            {
                new[] { "Rubber Mat", "Sport Floor", "Pro Turf" },
                new[] { "Sport Paint", "Racing Stripes", "Brushed Steel" },
                new[] { "Exercise Ball", "Yoga Mats", "Punching Bag" },
                new[] { "Treadmill", "Exercise Bike", "Rowing Machine" },
                new[] { "Dumbbells", "Weights Rack", "Power Rack" },
                new[] { "Mirror", "Big Mirror", "Trophy Shelf" },
            },
            new[] // 4 Library
            {
                new[] { "Wood Boards", "Red Carpet", "Persian Rug" },
                new[] { "Warm Paint", "Wallpaper", "Oak Panels" },
                new[] { "Bookcase", "Tall Bookcase", "Ladder Bookcase" },
                new[] { "Reading Chair", "Armchair and Lamp", "Chesterfield" },
                new[] { "Globe", "Reading Table", "Antique Globe" },
                new[] { "Painting", "Old Map", "Old Master" },
            },
            new[] // 5 Arcade
            {
                new[] { "Black Tiles", "Star Carpet", "Light-up Floor" },
                new[] { "Dark Paint", "Neon Stripes", "Pixel Mural" },
                new[] { "Arcade Cabinet", "Racing Cabinet", "Twin Cabinets" },
                new[] { "Pinball Table", "Air Hockey", "Dance Machine" },
                new[] { "Claw Machine", "Prize Counter", "Retro Cabinet" },
                new[] { "Neon Sign", "Neon Joystick", "High Score Board" },
            },
            new[] // 6 Spa
            {
                new[] { "Pebble Tiles", "Teak Deck", "Stone Slabs" },
                new[] { "Soft Green", "Bamboo Panels", "Stone Wall" },
                new[] { "Bamboo Plant", "Bonsai", "Water Fountain" },
                new[] { "Hot Tub", "Wooden Tub", "Infinity Tub" },
                new[] { "Lounger", "Towel Shelf", "Massage Bed" },
                new[] { "Zen Print", "Lotus Panel", "Waterfall Panel" },
            },
            new[] // 7 Penthouse
            {
                new[] { "White Oak", "Black Marble", "Gold Inlay" },
                new[] { "Silk Paint", "Velvet Panels", "Gold Leaf" },
                new[] { "Bar Cart", "Cocktail Bar", "Grand Bar" },
                new[] { "Luxury Sofa", "Corner Sofa", "Velvet Sofa" },
                new[] { "Upright Piano", "Grand Piano", "White Grand Piano" },
                new[] { "Art Print", "Crystal Sconces", "Chandelier" },
            },
        };

        /// <summary>Character skins in unlock order (home plan 7, prices in 9).</summary>
        public static readonly CatalogItem[] Skins =
        {
            Skin(DefaultSkin, "Default", 0, 0),
            Skin("skin.office", "Office Worker", 150, 0),
            Skin("skin.builder", "Builder", 150, 5),
            Skin("skin.chef", "Chef", 200, 10),
            Skin("skin.tourist", "Tourist", 200, 15),
            Skin("skin.dj", "DJ", 300, 35),
            Skin("skin.cowboy", "Cowboy", 400, 60),
            Skin("skin.wizard", "Wizard", 500, 85),
            Skin("skin.astronaut", "Astronaut", 600, 110),
            Skin("skin.robot", "Robot", 700, 135),
            Skin("skin.pirate", "Pirate", 800, 160),
            Skin("skin.royal", "Royal", 1000, 185),
        };

        /// <summary>Tower skin i costs this times i (Classic is 0 and free).</summary>
        public const int TowerPriceStep = 400;

        /// <summary>Tower skins (home plan 8); skin i unlocks with floor i.</summary>
        public static readonly CatalogItem[] Towers =
        {
            Tower(0, DefaultTower, "Classic"),
            Tower(1, "tower.brick", "Brick Loft"),
            Tower(2, "tower.glass", "Glass Modern"),
            Tower(3, "tower.candy", "Candy"),
            Tower(4, "tower.pagoda", "Pagoda"),
            Tower(5, "tower.castle", "Castle"),
            Tower(6, "tower.space", "Space Station"),
            Tower(7, "tower.gold", "Gold"),
        };

        static readonly int[] TierPrices = { 100, 250, 450, 700, 1000 };
        static readonly int[] TierCaps = { 1, 2, 3, 4, 5 };

        /// <summary>Elevator upgrades (home plan 6, prices in 9).</summary>
        public static readonly UpgradeDef[] Upgrades =
        {
            new UpgradeDef("elev.motor", UpgradeKind.Motor, "Motor", TierPrices, TierCaps, 0.05f, "Rises {0}% faster"),
            new UpgradeDef("elev.doors", UpgradeKind.Doors, "Doors", TierPrices, TierCaps, 0.08f, "Doors open {0}% faster"),
            new UpgradeDef("elev.comfort", UpgradeKind.Comfort, "Comfort", TierPrices, TierCaps, 0.06f, "Passengers wait {0}% longer"),
            new UpgradeDef("elev.tips", UpgradeKind.Tips, "Tips", TierPrices, TierCaps, 0.10f, "{0}% more coins per delivery"),
            new UpgradeDef("elev.capacity", UpgradeKind.Capacity, "Capacity", new[] { 400, 1200 }, new[] { 2, 4 }, 1f, "Cabin holds {0} more rider"),
        };

        // ---- Gems (monetization plan 2) ----
        /// <summary>Coins one gem covers when paying a coin shortfall.</summary>
        public const int CoinsPerGem = 10;
        /// <summary>Unlocking a skin, tower skin or elevator tier early costs its coin price in gems times this.</summary>
        public const int EarlyUnlockGemFactor = 2;
        /// <summary>Unlocking floor k early costs this times k gems.</summary>
        public const int FloorGemStep = 200;
        public const int GemsPerThreeStars = 1;
        public const int GemsPerFloorComplete = 20;
        public const int GemsPerUnlockRow = 5;

        static readonly Dictionary<string, CatalogItem> Items = BuildIndex();

        static CatalogItem Skin(string id, string name, int price, int unlockAt)
        {
            return new CatalogItem(id, ItemKind.Skin, name, price, unlockAt);
        }

        static CatalogItem Tower(int index, string id, string name)
        {
            return new CatalogItem(id, ItemKind.Tower, name, TowerPriceStep * index, FloorUnlockAt(index));
        }

        static Dictionary<string, CatalogItem> BuildIndex()
        {
            var index = new Dictionary<string, CatalogItem>();
            for (int f = 0; f < FloorCount; f++)
            {
                index.Add(FloorId(f), new CatalogItem(FloorId(f), ItemKind.Floor, RoomNames[f], 0, FloorUnlockAt(f), f));
                for (int s = 0; s < SlotsPerFloor; s++)
                    for (int o = 0; o < OptionsPerSlot; o++)
                    {
                        string id = DecorId(f, s, o);
                        index.Add(id, new CatalogItem(id, ItemKind.Decor, DecorNames[f][s][o], DecorPrice(f, o), FloorUnlockAt(f), f, s, o));
                    }
            }
            for (int t = 1; t <= MaxTierCap; t++)
                index.Add(TierId(t), new CatalogItem(TierId(t), ItemKind.ElevatorTier, "Elevator tier " + t, 0, TierUnlockAt(t)));
            foreach (var skin in Skins)
                index.Add(skin.id, skin);
            foreach (var tower in Towers)
                index.Add(tower.id, tower);
            return index;
        }

        // ---- Lookups ----

        /// <summary>The item with this id, or null when the id is unknown (old or corrupt save).</summary>
        public static CatalogItem Find(string id)
        {
            return id != null && Items.TryGetValue(id, out var item) ? item : null;
        }

        /// <summary>The upgrade with this id, or null.</summary>
        public static UpgradeDef FindUpgrade(string id)
        {
            foreach (var upgrade in Upgrades)
                if (upgrade.id == id)
                    return upgrade;
            return null;
        }

        public static UpgradeDef Upgrade(UpgradeKind kind) => Upgrades[(int)kind];

        /// <summary>True for any id the catalog knows: items and upgrades.</summary>
        public static bool IsKnown(string id) => Find(id) != null || FindUpgrade(id) != null;

        public static IEnumerable<CatalogItem> AllItems => Items.Values;

        /// <summary>Display name for any id, or the id itself when unknown.</summary>
        public static string NameOf(string id)
        {
            var item = Find(id);
            if (item != null)
                return item.name;
            var upgrade = FindUpgrade(id);
            return upgrade != null ? upgrade.name : id;
        }

        // ---- Ids (stable: never rename or renumber) ----

        public static string DecorId(int floor, int slot, int option) => "decor.f" + floor + "." + SlotKeys[slot] + "." + option;

        /// <summary>The prefix shared by every option of one slot, for example "decor.f0.left.".</summary>
        public static string DecorSlotPrefix(int floor, int slot) => "decor.f" + floor + "." + SlotKeys[slot] + ".";

        public static string FloorId(int floor) => "floor." + floor;

        public static string TierId(int tier) => "elev.tier" + tier;

        public static CatalogItem Decor(int floor, int slot, int option) => Find(DecorId(floor, slot, option));

        // ---- Rules as numbers ----

        public static int FloorUnlockAt(int floor) => LevelsPerFloor * floor;

        public static int TierUnlockAt(int tier) => LevelsPerFloor * (tier - 1);

        public static int DecorPrice(int floor, int option) => DecorBasePrices[option] * (1 + floor);

        public static int FloorCompletionBonus(int floor) => FloorBonusStep * (floor + 1);

        /// <summary>Elevator tier cap at this many levels completed: min(5, 1 + n / 25).</summary>
        public static int TierCap(int levelsCompleted) => Math.Min(MaxTierCap, 1 + Math.Max(0, levelsCompleted) / LevelsPerFloor);
    }
}
