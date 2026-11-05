using UnityEngine;

namespace ElevatorRush
{
    /// <summary>
    /// The eight tower skins (home plan 8), in catalog order. Classic matches the building's
    /// original look. Colors stay soft and away from the destination palette so bands and icons
    /// read the same on every skin.
    /// </summary>
    public static class TowerSkins
    {
        static readonly TowerSkin[] All =
        {
            new TowerSkin
            {
                id = Catalog.DefaultTower,
                exterior = Hex(0xF5EDE0), slab = Hex(0xDBCCB8), shaft = Hex(0x5C6680),
                trim = Hex(0xC9B9A3), frame = Hex(0x4A5266), windowFrame = Hex(0xFFFFFF), glass = Hex(0x9FC6E0),
                interior = Hex(0xEDE6DA), ornament = RoofOrnament.WaterTank, ornamentMain = Hex(0x8C8C94), ornamentAccent = Hex(0x5C5C66),
                ornamentModel = "towers/classic_roof",
            },
            new TowerSkin
            {
                id = "tower.brick",
                exterior = Hex(0xA65A44), exteriorPattern = SurfacePattern.Bricks, exteriorDetail = Hex(0xD9CBB8),
                slab = Hex(0x6E6A66), shaft = Hex(0x3C3A3A), trim = Hex(0x2E2C2C), frame = Hex(0x2E2C2C),
                windowFrame = Hex(0x1F1F1F), glass = Hex(0x8FB0C0), interior = Hex(0xE6DED3),
                ornament = RoofOrnament.Chimneys, ornamentMain = Hex(0x8E4A38), ornamentAccent = Hex(0x3C3A3A),
                ornamentModel = "towers/brick_roof",
            },
            new TowerSkin
            {
                id = "tower.glass",
                exterior = Hex(0xA9C8D8), exteriorPattern = SurfacePattern.Grid, exteriorDetail = Hex(0x6F8A99), finish = TowerFinish.Glossy,
                slab = Hex(0xD8DEE3), shaft = Hex(0x39424A), trim = Hex(0xC7CED4), trimShiny = true, frame = Hex(0x8C969E),
                windowFrame = Hex(0xC7CED4), glass = Hex(0x7FA9C2), interior = Hex(0xF2F4F5),
                ornament = RoofOrnament.Spire, ornamentMain = Hex(0xC7CED4), ornamentAccent = Hex(0xFF5A5A),
                ornamentModel = "towers/glass_roof",
            },
            new TowerSkin
            {
                id = "tower.candy",
                exterior = Hex(0xFBE3EC), exteriorPattern = SurfacePattern.Stripes, exteriorDetail = Hex(0xFFFFFF),
                slab = Hex(0xF6D9A8), shaft = Hex(0x8E7AA8), trim = Hex(0xFFFFFF), frame = Hex(0xB8A6CC),
                windowFrame = Hex(0xFFFFFF), glass = Hex(0xBFE6F2), interior = Hex(0xFFF5F8),
                ornament = RoofOrnament.Lollipops, ornamentMain = Hex(0xF7C6D9), ornamentAccent = Hex(0xFFFFFF),
                ornamentModel = "towers/candy_roof",
            },
            new TowerSkin
            {
                id = "tower.pagoda",
                exterior = Hex(0xEFE4CC), slab = Hex(0x7A3B2E), shaft = Hex(0x3D2B25),
                trim = Hex(0x7A3B2E), frame = Hex(0x3D2B25), windowFrame = Hex(0x3D2B25), glass = Hex(0xF2E3B8),
                interior = Hex(0xF4ECDA), ornament = RoofOrnament.Pagoda, ornamentMain = Hex(0x2F4F4A), ornamentAccent = Hex(0xC9A54A),
                ornamentModel = "towers/pagoda_roof",
            },
            new TowerSkin
            {
                id = "tower.castle",
                exterior = Hex(0xB3AEA4), exteriorPattern = SurfacePattern.Bricks, exteriorDetail = Hex(0x8E897F),
                slab = Hex(0x7E7A72), shaft = Hex(0x4A4740), trim = Hex(0x8E897F), frame = Hex(0x5A3E2B),
                windowFrame = Hex(0x5A3E2B), glass = Hex(0x2F3440), interior = Hex(0xE3DDD0),
                ornament = RoofOrnament.Turrets, ornamentMain = Hex(0xB3AEA4), ornamentAccent = Hex(0x3F4E66), battlements = true,
                ornamentModel = "towers/castle_roof",
            },
            new TowerSkin
            {
                id = "tower.space",
                exterior = Hex(0xE6E9EE), exteriorPattern = SurfacePattern.Panels, exteriorDetail = Hex(0xB9C0CA), finish = TowerFinish.Glossy,
                slab = Hex(0x9AA3AF), shaft = Hex(0x262B33), trim = Hex(0x3A414D), trimShiny = true, frame = Hex(0x3A414D),
                windowFrame = Hex(0x3A414D), glass = Hex(0x1C2433), interior = Hex(0xF0F2F5),
                ornament = RoofOrnament.Dish, ornamentMain = Hex(0xE6E9EE), ornamentAccent = Hex(0x3A414D),
                ornamentModel = "towers/space_roof",
            },
            new TowerSkin
            {
                id = "tower.gold",
                exterior = Hex(0xE3C77A), finish = TowerFinish.Metallic, slab = Hex(0xF4EEE0), shaft = Hex(0x2B2B2B),
                trim = Hex(0xF2DB8C), trimShiny = true, frame = Hex(0x2B2B2B), windowFrame = Hex(0xF2DB8C), glass = Hex(0x2B2B2B),
                interior = Hex(0xFAF6EC), ornament = RoofOrnament.Dome, ornamentMain = Hex(0xE8C860), ornamentAccent = Hex(0xFFF3C4),
                ornamentModel = "towers/gold_roof",
            },
        };

        public static int Count => All.Length;

        public static TowerSkin Classic => All[0];

        /// <summary>The skin for a tower id; Classic for unknown ids.</summary>
        public static TowerSkin Get(string id)
        {
            foreach (var skin in All)
                if (skin.id == id)
                    return skin;
            return All[0];
        }

        public static TowerSkin At(int index) => All[index];

        static Color Hex(int rgb)
        {
            return new Color(((rgb >> 16) & 0xFF) / 255f, ((rgb >> 8) & 0xFF) / 255f, (rgb & 0xFF) / 255f);
        }
    }
}
