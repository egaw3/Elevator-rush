using System;
using System.Collections.Generic;
using UnityEngine;

namespace ElevatorRush
{
    /// <summary>
    /// Builds the cutaway building: shaft on the left, one open-fronted room per floor to its right.
    /// Gameplay happens on the X-Y plane at Z = 0; the front of every room faces -Z (the camera).
    /// </summary>
    public sealed class Building : MonoBehaviour
    {
        readonly List<Floor> floors = new List<Floor>();

        GameConfig config;
        MaterialLibrary materials;
        AssetLibrary assets;
        SpriteFactory sprites;
        RoomDecorator decorator;
        Transform structure;
        TowerSkin skin;
        PatternFactory patterns;
        TowerOrnamentBuilder ornaments;
        bool constructionSite;

        public int FloorCount => floors.Count;
        public IReadOnlyList<Floor> Floors => floors;
        public float ShaftX => 0f;
        public float RoomLeftX => config.shaftWidth * 0.5f;
        public float RoomRightX => RoomLeftX + config.roomWidth;
        public float TopY => FloorCount * config.floorHeight;
        /// <summary>World Y where the cable attaches (underside of the roof).</summary>
        public float CableAnchorY => TopY - config.slabThickness;
        public float LeftX => -config.shaftWidth * 0.5f - config.wallThickness - config.towerPilasterWidth;
        public float RightX => RoomRightX + config.wallThickness + config.towerPilasterWidth;
        public float FrontZ => -config.roomDepth * 0.5f;
        public float BackZ => config.roomDepth * 0.5f + config.wallThickness;
        public int ThemeIndex { get; private set; }
        /// <summary>World-space box that the camera must keep in frame.</summary>
        public Bounds Bounds { get; private set; }
        /// <summary>True when a construction site stands on the roof (home tower, home plan 3.2).</summary>
        public bool HasConstructionSite => constructionSite;
        /// <summary>Center of the construction site's sign face, for its UI label.</summary>
        public Vector3 ConstructionSignPosition { get; private set; }

        /// <summary>
        /// The player's decor: (floor, slot) to the equipped option, or -1 for an empty slot
        /// (home plan 5.2). Null builds plain rooms.
        /// </summary>
        public Func<int, int, int> DecorSource { get; set; }

        /// <summary>The tower skin used by the next Build (home plan 8). Classic by default.</summary>
        public TowerSkin Skin { get; set; } = TowerSkins.Classic;

        public void Init(GameConfig config, MaterialLibrary materials, AssetLibrary assets, SpriteFactory sprites, RoomDecorator decorator)
        {
            this.config = config;
            this.materials = materials;
            this.assets = assets;
            this.sprites = sprites;
            this.decorator = decorator;
            patterns = decorator.Patterns;
            ornaments = new TowerOrnamentBuilder(materials, assets);
        }

        public float FloorY(int index) => index * config.floorHeight;

        /// <summary>
        /// Builds a fresh building with this many floors. With <paramref name="withConstructionSite"/>
        /// a bare frame with scaffolding and a sign stands on the roof, as the next floor to unlock.
        /// </summary>
        public void Build(int floorCount, int themeIndex, bool withConstructionSite = false)
        {
            Clear();
            constructionSite = withConstructionSite;
            ThemeIndex = themeIndex;
            skin = Skin ?? TowerSkins.Classic;
            Grow(floorCount);
        }

        /// <summary>
        /// Adds floors on top until there are this many, keeping existing floors (and their queues)
        /// and rebuilding the full-height walls and the roof. Used by Endless mode.
        /// </summary>
        public void Grow(int floorCount)
        {
            float fh = config.floorHeight;
            float st = config.slabThickness;
            float wt = config.wallThickness;
            float halfDepth = config.roomDepth * 0.5f;
            float shaftLeft = -config.shaftWidth * 0.5f;
            float roomLeft = RoomLeftX;
            float roomRight = RoomRightX;
            float back = halfDepth;
            float front = -halfDepth;
            float height = floorCount * fh;

            var slabMat = materials.Get(skin.slab);
            var shaftMat = materials.Get(skin.shaft);
            var interiorMat = materials.Get(skin.interior);
            var root = transform;

            for (int i = floors.Count; i < floorCount; i++)
            {
                float y = FloorY(i);
                float ceiling = y + fh - st;

                var floorGo = new GameObject("Floor " + i);
                floorGo.transform.SetParent(root, false);
                var floor = floorGo.AddComponent<Floor>();
                floor.Init(i, y, roomLeft, roomRight, config, materials);
                var ft = floorGo.transform;

                var tintColor = FloorPalette.WallTint(i, config.wallTintSaturation, config.wallTintValue);
                var tint = materials.Get(tintColor);
                var full = materials.Get(floor.DestinationColor);

                assets.CreateBox("Slab", ft,
                    new Vector3(roomLeft, y - st, front), new Vector3(roomRight + wt, y, back + wt), slabMat);
                floor.AddTinted(assets.CreateBox("BackWall", ft,
                    new Vector3(roomLeft, y, back), new Vector3(roomRight, ceiling, back + wt), tint), tintColor);
                floor.AddTinted(assets.CreateBox("DoorLintel", ft,
                    new Vector3(roomLeft - wt * 0.5f, y + config.doorHeight, front), new Vector3(roomLeft + wt * 0.5f, ceiling, back), tint), tintColor);
                floor.AddTinted(assets.CreateBox("SlabTrim", ft,
                    new Vector3(roomLeft, y - st, front - config.trimDepth), new Vector3(roomRight + wt, y, front), full), floor.DestinationColor);

                // The floor's icon, large on the back wall.
                var icon = assets.CreatePrimitive(PrimitiveType.Quad, "WallIcon", ft,
                    new Vector3(roomLeft + config.roomWidth * config.wallIconXFraction, y + (fh - st) * config.wallIconYFraction, back - config.wallIconOffset),
                    Vector3.one * config.wallIconSize,
                    materials.GetCutout(sprites.Icon(floor.Icon), floor.DestinationColor));
                icon.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

                // The room's side wall in the skin's neutral interior color, and a window on the outside.
                assets.CreateBox("InteriorWall", ft,
                    new Vector3(roomRight - config.towerInteriorThickness, y, front), new Vector3(roomRight, ceiling, back), interiorMat);
                BuildWindow(ft, y, shaftLeft - wt - config.towerPilasterWidth);

                Decorate(ft, i);
                floors.Add(floor);
            }

            // Full-height structure, rebuilt whenever the height changes.
            if (structure != null)
                Destroy(structure.gameObject);
            structure = new GameObject("Structure").transform;
            structure.SetParent(transform, false);
            root = structure;

            float depth = back + wt - front;
            assets.CreateBox("RightWall", root,
                new Vector3(roomRight, -st, front), new Vector3(roomRight + wt, height, back + wt), Exterior(depth, height + st));
            assets.CreateBox("ShaftBack", root,
                new Vector3(shaftLeft, -st, back), new Vector3(roomLeft, height, back + wt), shaftMat);
            assets.CreateBox("ShaftLeft", root,
                new Vector3(shaftLeft - wt, -st, front), new Vector3(shaftLeft, height, back + wt), Exterior(depth, height + st));
            // Pilasters: the skin's exterior, wide enough to read from the front camera.
            float p = config.towerPilasterWidth;
            assets.CreateBox("PilasterLeft", root,
                new Vector3(shaftLeft - wt - p, -st, front), new Vector3(shaftLeft - wt, height, back + wt), Exterior(p, height + st));
            assets.CreateBox("PilasterRight", root,
                new Vector3(roomRight + wt, -st, front), new Vector3(roomRight + wt + p, height, back + wt), Exterior(p, height + st));
            assets.CreateBox("ShaftPit", root,
                new Vector3(shaftLeft, -st, front), new Vector3(roomLeft, -st * 0.5f, back), shaftMat);

            // Roof slab over shaft and rooms, a low parapet, and the machine room above the shaft.
            float roofTop = height;
            assets.CreateBox("Roof", root,
                new Vector3(shaftLeft - wt - p, height - st, front), new Vector3(roomRight + wt + p, roofTop, back + wt), slabMat);
            float width = roomRight + wt - (shaftLeft - wt) + 2f * p;
            assets.CreateBox("Parapet", root,
                new Vector3(shaftLeft - wt - p, roofTop, front), new Vector3(roomRight + wt + p, roofTop + config.roofParapetHeight, front + wt),
                Exterior(width, config.roofParapetHeight));
            var machine = config.roofMachineSize;
            assets.CreateBox("Machine", root,
                new Vector3(-machine.x * 0.5f, roofTop, -machine.z * 0.5f), new Vector3(machine.x * 0.5f, roofTop + machine.y, machine.z * 0.5f),
                Exterior(machine.x, machine.y));
            BuildTrim(root, shaftLeft, roomLeft, roomRight, front, roofTop);

            float ornamentHeight = ornaments.Build(root, skin, new Vector3(0f, roofTop + machine.y, 0f), machine.x);
            float top = roofTop + Mathf.Max(config.roofParapetHeight, machine.y + ornamentHeight);
            if (constructionSite)
            {
                BuildConstructionSite(root, roofTop, roomLeft, roomRight, front, back);
                top = Mathf.Max(top, roofTop + fh);
            }
            var min = new Vector3(LeftX, -config.cameraGroundPadding, front);
            var max = new Vector3(RightX, top, back + wt);
            Bounds = new Bounds((min + max) * 0.5f, max - min);
        }

        /// <summary>The skin's exterior wall material for a face of this size (patterns tile at the decor tile size).</summary>
        Material Exterior(float faceWidth, float faceHeight)
        {
            if (skin.exteriorPattern != SurfacePattern.Plain)
            {
                float tile = PatternFactory.TileMeters(skin.exteriorPattern, config);
                return materials.GetPattern(patterns.Get(skin.exteriorPattern, skin.exterior, skin.exteriorDetail),
                    new Vector2(faceWidth / tile, faceHeight / tile));
            }
            switch (skin.finish)
            {
                case TowerFinish.Metallic: return materials.GetShiny(skin.exterior);
                case TowerFinish.Glossy: return materials.GetGlossy(skin.exterior);
                default: return materials.Get(skin.exterior);
            }
        }

        /// <summary>
        /// Skin trim: front corner strips, the shaft frame, a cornice on the parapet, and
        /// battlements for the Castle skin. Destination bands are never touched.
        /// </summary>
        void BuildTrim(Transform root, float shaftLeft, float roomLeft, float roomRight, float front, float roofTop)
        {
            float wt = config.wallThickness;
            float st = config.slabThickness;
            float t = config.towerTrimSize;
            float p = config.towerPilasterWidth;
            var trim = skin.trimShiny ? materials.GetShiny(skin.trim) : materials.Get(skin.trim);
            var frame = materials.Get(skin.frame);
            float parapetTop = roofTop + config.roofParapetHeight;

            // Trim at the outer and inner edges of both pilasters, and a cornice on the parapet.
            float outerLeft = shaftLeft - wt - p;
            float outerRight = roomRight + wt + p;
            assets.CreateBox("CornerLeft", root, new Vector3(outerLeft - t, -st, front - t), new Vector3(outerLeft + t, roofTop, front + t), trim);
            assets.CreateBox("EdgeLeft", root, new Vector3(shaftLeft - t, -st, front - t), new Vector3(shaftLeft, roofTop, front + t), trim);
            assets.CreateBox("CornerRight", root, new Vector3(outerRight - t, -st, front - t), new Vector3(outerRight + t, roofTop, front + t), trim);
            assets.CreateBox("EdgeRight", root, new Vector3(roomRight, -st, front - t), new Vector3(roomRight + t, roofTop, front + t), trim);
            assets.CreateBox("Cornice", root, new Vector3(outerLeft - t, parapetTop, front - t),
                new Vector3(outerRight + t, parapetTop + t, front + wt + t), trim);
            // The shaft frame: a post on each side of the shaft opening.
            float f = config.towerFrameWidth;
            assets.CreateBox("ShaftFrameLeft", root, new Vector3(shaftLeft, -st, front - t * 0.5f), new Vector3(shaftLeft + f, roofTop, front + t), frame);
            assets.CreateBox("ShaftFrameRight", root, new Vector3(roomLeft - wt * 0.5f - f, -st, front - t * 0.5f), new Vector3(roomLeft - wt * 0.5f, roofTop, front + t), frame);

            if (!skin.battlements)
                return;
            float cw = config.towerCrenelWidth;
            for (float x = shaftLeft - wt - p; x + cw <= roomRight + wt + p + 1e-3f; x += cw * 2f)
                assets.CreateBox("Crenel", root, new Vector3(x, parapetTop, front - t), new Vector3(x + cw, parapetTop + config.towerCrenelHeight, front + wt + t),
                    Exterior(cw, config.towerCrenelHeight));
        }

        /// <summary>A framed window on the building's left outer wall, one per floor.</summary>
        void BuildWindow(Transform floor, float y, float wallOutside)
        {
            var size = config.towerWindowSize;
            float fr = config.towerWindowFrame;
            float d = config.towerWindowDepth;
            float h = config.floorHeight - config.slabThickness;
            float y0 = y + (h - size.y) * 0.5f;
            var frame = skin.trimShiny ? materials.GetShiny(skin.windowFrame) : materials.Get(skin.windowFrame);
            assets.CreateBox("WindowFrame", floor, new Vector3(wallOutside - d, y0, -size.x * 0.5f), new Vector3(wallOutside, y0 + size.y, size.x * 0.5f), frame);
            assets.CreateBox("WindowGlass", floor, new Vector3(wallOutside - d - 0.005f, y0 + fr, -size.x * 0.5f + fr),
                new Vector3(wallOutside - d * 0.5f, y0 + size.y - fr, size.x * 0.5f - fr), materials.GetGlossy(skin.glass));
        }

        /// <summary>
        /// The next floor as a construction site on the roof: corner posts and beams, scaffolding
        /// on the right, and a sign whose text is a UI label placed at <see cref="ConstructionSignPosition"/>.
        /// </summary>
        void BuildConstructionSite(Transform root, float y, float roomLeft, float roomRight, float front, float back)
        {
            float h = config.floorHeight - config.slabThickness;
            float post = config.constructionPostSize;
            var steel = materials.Get(config.constructionSteelColor);
            var site = new GameObject("ConstructionSite").transform;
            site.SetParent(root, false);

            float[] xs = { roomLeft, roomRight - post };
            float[] zs = { front, back - post };
            foreach (float x in xs)
                foreach (float z in zs)
                    assets.CreateBox("Post", site, new Vector3(x, y, z), new Vector3(x + post, y + h, z + post), steel);
            foreach (float z in zs)
                assets.CreateBox("Beam", site, new Vector3(roomLeft, y + h - post, z), new Vector3(roomRight, y + h, z + post), steel);
            assets.CreateBox("SideBeam", site, new Vector3(roomRight - post, y + h - post, front), new Vector3(roomRight, y + h, back), steel);

            // Scaffolding: a grid of poles in front of the right end, with a plank on each level.
            var pole = materials.Get(config.scaffoldColor);
            var plank = materials.Get(config.plankColor);
            float p = config.scaffoldPoleSize;
            float spacing = config.roomWidth / (config.scaffoldColumns + 2f);
            float left = roomRight - spacing * (config.scaffoldColumns - 1) - p;
            float zFront = front - config.scaffoldDepth;
            for (int c = 0; c < config.scaffoldColumns; c++)
            {
                float x = left + c * spacing;
                assets.CreateBox("Pole", site, new Vector3(x, y, zFront), new Vector3(x + p, y + h, zFront + p), pole);
                assets.CreateBox("Pole", site, new Vector3(x, y, front - p), new Vector3(x + p, y + h, front), pole);
            }
            for (int l = 1; l <= config.scaffoldLevels; l++)
            {
                float ly = y + h * l / config.scaffoldLevels - config.scaffoldPlankThickness;
                assets.CreateBox("Plank", site, new Vector3(left, ly, zFront), new Vector3(roomRight, ly + config.scaffoldPlankThickness, front), plank);
                assets.CreateBox("Rail", site, new Vector3(left, ly + h * 0.25f, zFront), new Vector3(roomRight, ly + h * 0.25f + p, zFront + p), pole);
            }

            // Sign on two legs, left of the scaffolding, facing the camera.
            var size = config.constructionSignSize;
            float cx = roomLeft + (left - roomLeft) * 0.5f;
            float signBottom = y + config.constructionSignHeight - size.y * 0.5f;
            float t = config.constructionSignThickness;
            assets.CreateBox("Sign", site, new Vector3(cx - size.x * 0.5f, signBottom, -t), new Vector3(cx + size.x * 0.5f, signBottom + size.y, 0f),
                materials.Get(config.constructionSignColor));
            foreach (float lx in new[] { cx - size.x * 0.35f, cx + size.x * 0.35f })
                assets.CreateBox("SignLeg", site, new Vector3(lx - p, y, 0f), new Vector3(lx + p, signBottom, p * 2f), pole);
            ConstructionSignPosition = new Vector3(cx, signBottom + size.y * 0.5f, -t);
        }

        /// <summary>The player's decor on one floor; floors without any stay plain rooms.</summary>
        void Decorate(Transform floor, int index)
        {
            var source = DecorSource;
            if (source == null)
                return;
            decorator.DecorateFloor(floor, index, RoomOrigin(index), slot => source(index, slot));
        }

        Vector3 RoomOrigin(int index) => new Vector3(RoomLeftX, FloorY(index), 0f);

        /// <summary>
        /// Rebuilds one decor slot after a purchase or swap and returns the new object (null for
        /// an empty slot), so the caller can animate it.
        /// </summary>
        public GameObject RedecorateSlot(int floor, int slot)
        {
            if (DecorSource == null || floor < 0 || floor >= floors.Count)
                return null;
            bool complete = true;
            for (int s = 0; s < Catalog.SlotsPerFloor; s++)
                complete &= DecorSource(floor, s) >= 0;
            var root = floors[floor].transform;
            if (root.Find("Decor") == null)
                decorator.DecorateFloor(root, floor, RoomOrigin(floor), s => -1);
            return decorator.RebuildSlot(root, floor, slot, DecorSource(floor, slot), complete);
        }

        /// <summary>World position of a point given in a floor's room space (see DecorLayout).</summary>
        public Vector3 RoomToWorld(int floor, Vector3 roomPoint) => transform.TransformPoint(RoomOrigin(floor) + roomPoint);

        /// <summary>World box around one floor: the room, the shaft beside it, slab to ceiling.</summary>
        public Bounds FloorBounds(int floor)
        {
            var min = new Vector3(LeftX, FloorY(floor) - config.slabThickness, FrontZ);
            var max = new Vector3(RightX, FloorY(floor) + config.floorHeight, BackZ);
            return new Bounds((min + max) * 0.5f, max - min);
        }

        public void Clear()
        {
            floors.Clear();
            structure = null;
            for (int i = transform.childCount - 1; i >= 0; i--)
                Destroy(transform.GetChild(i).gameObject);
        }
    }
}
