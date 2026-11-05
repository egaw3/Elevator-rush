using UnityEngine;

namespace ElevatorRush
{
    public enum TowerFinish
    {
        /// <summary>Matte paint.</summary>
        Matte,
        /// <summary>Glossy panels (Glass Modern): smooth and reflective.</summary>
        Glossy,
        /// <summary>Polished metal (Gold).</summary>
        Metallic
    }

    public enum RoofOrnament
    {
        WaterTank,
        Chimneys,
        Spire,
        Lollipops,
        Pagoda,
        Turrets,
        Dish,
        Dome
    }

    /// <summary>
    /// How the building itself looks (home plan 8): exterior walls, slabs, shaft, trim, the shaft
    /// frame, window frames on the side, the neutral base of the room's side wall, and a roof
    /// ornament. Destination color bands, room tints and icons never come from a skin.
    /// The level theme only sets the sky, street, props and light.
    /// </summary>
    public sealed class TowerSkin
    {
        public string id;
        public Color exterior;
        /// <summary>Exterior pattern (e.g. Bricks) drawn with <see cref="exteriorDetail"/>; Plain for paint.</summary>
        public SurfacePattern exteriorPattern = SurfacePattern.Plain;
        public Color exteriorDetail;
        public TowerFinish finish = TowerFinish.Matte;
        public Color slab;
        public Color shaft;
        public Color trim;
        public bool trimShiny;
        public Color frame;
        public Color windowFrame;
        public Color glass;
        /// <summary>Neutral base color of the room's side wall (behind any wall finish decor).</summary>
        public Color interior;
        public RoofOrnament ornament;
        public Color ornamentMain;
        public Color ornamentAccent;
        /// <summary>Optional roof ornament model key under Resources/Models; the procedural ornament is the fallback.</summary>
        public string ornamentModel;
        /// <summary>Crenellated parapet (Castle).</summary>
        public bool battlements;
    }
}
