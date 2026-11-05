using UnityEngine;

namespace ElevatorRush
{
    /// <summary>
    /// Pure room geometry for decor (home plan 5.1 and 5.2), in room space: x from 0 at the
    /// room's left edge to the room width, y from 0 at the floor surface to the ceiling, z from
    /// the open front (negative) to the back wall (positive). Furniture boxes stay in the back
    /// half and around the color band (the tinted wall around the floor icon), so decor never
    /// covers the icon, the queue or the patience bars.
    /// </summary>
    public sealed class DecorLayout
    {
        readonly GameConfig config;

        public float Width => config.roomWidth;
        public float Height => config.floorHeight - config.slabThickness;
        public float Front => -config.roomDepth * 0.5f;
        public float Back => config.roomDepth * 0.5f;

        public Vector2 IconCenter => new Vector2(Width * config.wallIconXFraction, Height * config.wallIconYFraction);

        /// <summary>The color band on the back wall (x, y): the icon plus a margin, up to the ceiling.</summary>
        public Rect ColorBand
        {
            get
            {
                float half = config.wallIconSize * 0.5f + config.colorBandMargin;
                var c = IconCenter;
                return Rect.MinMaxRect(c.x - half, c.y - half, c.x + half, Height);
            }
        }

        public DecorLayout(GameConfig config)
        {
            this.config = config;
        }

        /// <summary>The box a slot's decor must fit inside. Flooring and wall return their surface slab.</summary>
        public Bounds SlotBox(int slot)
        {
            var band = ColorBand;
            float gap = config.decorGap;
            float inset = config.decorSideInset;
            float zMin = config.decorFrontZ;
            float zMax = Back - config.decorWallGap;
            switch (slot)
            {
                case Catalog.SlotFlooring:
                    return Box(0f, 0f, Front, Width, config.decorFloorThickness, Back);
                case Catalog.SlotWall:
                    return Box(Width - config.decorWallThickness, 0f, Front, Width, Height, Back);
                case Catalog.SlotLeft:
                    return Box(inset, 0f, zMin, band.xMin - gap, config.decorLeftMaxHeight, zMax);
                case Catalog.SlotCenter:
                    return Box(band.xMin, 0f, zMin, band.xMax, band.yMin - gap, zMax);
                case Catalog.SlotRight:
                    return Box(band.xMax + gap, 0f, zMin, Width - inset, config.decorRightMaxHeight, zMax);
                default: // wall art, above the right furniture
                    return Box(band.xMax + gap, config.decorRightMaxHeight + gap, Back - config.decorArtDepth,
                        Width - inset, Height - gap, Back);
            }
        }

        /// <summary>Where a slot's marker floats in Build mode.</summary>
        public Vector3 MarkerPoint(int slot)
        {
            var box = SlotBox(slot);
            switch (slot)
            {
                case Catalog.SlotFlooring:
                    return new Vector3(Width * 0.5f, 0.15f, Front + 0.35f);
                case Catalog.SlotWall:
                    return new Vector3(Width - 0.25f, Height * 0.8f, 0f);
                case Catalog.SlotWallArt:
                    return box.center;
                default:
                    return new Vector3(box.center.x, box.size.y * 0.55f, box.min.z);
            }
        }

        /// <summary>Back-wall pieces a wall finish covers: everything except the color band.</summary>
        public Rect[] WallPanels()
        {
            var band = ColorBand;
            return new[]
            {
                Rect.MinMaxRect(0f, 0f, band.xMin, Height),
                Rect.MinMaxRect(band.xMax, 0f, Width, Height),
                Rect.MinMaxRect(band.xMin, 0f, band.xMax, band.yMin),
            };
        }

        /// <summary>The gold star of a completed floor: high on the back wall at the left, in front of any wall finish.</summary>
        public Vector3 StarPoint => new Vector3(config.decorStarX, Height - config.decorStarInset,
            Back - config.decorWallThickness - config.decorWallGap);

        static Bounds Box(float x0, float y0, float z0, float x1, float y1, float z1)
        {
            var min = new Vector3(x0, y0, z0);
            var max = new Vector3(x1, y1, z1);
            return new Bounds((min + max) * 0.5f, max - min);
        }
    }
}
