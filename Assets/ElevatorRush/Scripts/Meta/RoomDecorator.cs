using System;
using System.Collections.Generic;
using UnityEngine;

namespace ElevatorRush
{
    /// <summary>
    /// Builds a room's decor from the equipped options (home plan 5), the same way on the home
    /// tower and in levels. Every slot is a child of the floor's "Decor" object, placed in room
    /// space (see <see cref="DecorLayout"/>). Furniture uses its model key when the model is on
    /// disk and the procedural shape from <see cref="DecorBuilder"/> otherwise. A complete floor
    /// gets a gold star.
    /// </summary>
    public sealed class RoomDecorator
    {
        const string RootName = "Decor";
        const string StarName = "Star";
        const string RemovedName = "Removed";

        readonly GameConfig config;
        readonly MaterialLibrary materials;
        readonly AssetLibrary assets;
        readonly SpriteFactory sprites;
        readonly PatternFactory patterns = new PatternFactory();
        readonly DecorBuilder builder;

        public DecorLayout Layout { get; }
        /// <summary>The shared pattern textures (also used by tower skins).</summary>
        public PatternFactory Patterns => patterns;

        public RoomDecorator(GameConfig config, MaterialLibrary materials, AssetLibrary assets, SpriteFactory sprites)
        {
            this.config = config;
            this.materials = materials;
            this.assets = assets;
            this.sprites = sprites;
            builder = new DecorBuilder(materials, assets);
            Layout = new DecorLayout(config);
        }

        static string SlotName(int slot) => "Slot" + slot;

        /// <summary>
        /// Builds every equipped slot of one floor. <paramref name="origin"/> is the room's origin
        /// (left edge, floor surface, center line) in the floor object's space; optionOf(slot)
        /// returns the equipped option or -1.
        /// </summary>
        public void DecorateFloor(Transform floorRoot, int floor, Vector3 origin, Func<int, int> optionOf)
        {
            var old = floorRoot.Find(RootName);
            if (old != null)
                Remove(old.gameObject);
            if (floor < 0 || floor >= Catalog.FloorCount || optionOf == null)
                return;

            var root = new GameObject(RootName).transform;
            root.SetParent(floorRoot, false);
            root.localPosition = origin;
            int filled = 0;
            for (int slot = 0; slot < Catalog.SlotsPerFloor; slot++)
            {
                int option = optionOf(slot);
                if (option < 0 || option >= Catalog.OptionsPerSlot)
                    continue;
                BuildSlot(root, floor, slot, option);
                filled++;
            }
            SetStar(root, filled == Catalog.SlotsPerFloor);
        }

        /// <summary>
        /// Replaces one slot on an already decorated floor and updates the star. Returns the new
        /// slot object (for the drop-in animation), or null if the floor has no decor root.
        /// </summary>
        public GameObject RebuildSlot(Transform floorRoot, int floor, int slot, int option, bool floorComplete)
        {
            var root = floorRoot.Find(RootName);
            if (root == null)
                return null;
            var old = root.Find(SlotName(slot));
            if (old != null)
                Remove(old.gameObject);
            var built = option >= 0 ? BuildSlot(root, floor, slot, option) : null;
            SetStar(root, floorComplete);
            return built;
        }

        /// <summary>Builds one option at its natural size, centered at the parent's origin, for a thumbnail.</summary>
        public GameObject BuildPreview(Transform parent, int floor, int slot, int option)
        {
            var look = DecorLooks.Get(floor, slot, option);
            var holder = new GameObject("Preview").transform;
            holder.SetParent(parent, false);
            if (look.shape == DecorShape.Surface)
            {
                // A square swatch facing the camera.
                float tile = PatternFactory.TileMeters(look.pattern, config);
                float side = Mathf.Max(1f, tile);
                assets.CreateBox("Swatch", holder, new Vector3(-side * 0.5f, 0f, 0f), new Vector3(side * 0.5f, side, 0.05f),
                    SurfaceMaterial(look, new Vector2(side, side)));
                return holder.gameObject;
            }
            var model = new GameObject("Model").transform;
            model.SetParent(holder, false);
            DrawOrSpawn(model, look, Seed(floor, slot, option), out _);
            return holder.gameObject;
        }

        GameObject BuildSlot(Transform root, int floor, int slot, int option)
        {
            var look = DecorLooks.Get(floor, slot, option);
            var holder = new GameObject(SlotName(slot)).transform;
            holder.SetParent(root, false);

            if (slot == Catalog.SlotFlooring)
                BuildFlooring(holder, look);
            else if (slot == Catalog.SlotWall)
                BuildWallFinish(holder, look);
            else
                PlaceObject(holder, slot, look, Seed(floor, slot, option));
            return holder.gameObject;
        }

        static int Seed(int floor, int slot, int option) => (floor * 31 + slot) * 7 + option;

        // ---- Surfaces ----

        void BuildFlooring(Transform holder, DecorLook look)
        {
            var box = Layout.SlotBox(Catalog.SlotFlooring);
            float tile = PatternFactory.TileMeters(look.pattern, config);
            assets.CreateBox("Flooring", holder, box.min, box.max, SurfaceMaterial(look, new Vector2(box.size.x / tile, box.size.z / tile)));
        }

        void BuildWallFinish(Transform holder, DecorLook look)
        {
            float tile = PatternFactory.TileMeters(look.pattern, config);
            float back = Layout.Back;
            float t = config.decorWallThickness;
            foreach (var r in Layout.WallPanels())
            {
                if (r.width <= 0f || r.height <= 0f)
                    continue;
                assets.CreateBox("WallPanel", holder, new Vector3(r.xMin, r.yMin, back - t), new Vector3(r.xMax, r.yMax, back),
                    SurfaceMaterial(look, new Vector2(r.width / tile, r.height / tile)));
            }
            var side = Layout.SlotBox(Catalog.SlotWall);
            assets.CreateBox("SideWall", holder, side.min, side.max, SurfaceMaterial(look, new Vector2(side.size.z / tile, side.size.y / tile)));
        }

        Material SurfaceMaterial(DecorLook look, Vector2 tiling)
        {
            if (look.pattern == SurfacePattern.Plain)
                return materials.Get(look.a);
            return materials.GetPattern(patterns.Get(look.pattern, look.a, look.b), tiling);
        }

        // ---- Furniture and wall art ----

        /// <summary>
        /// Fits an object into its slot box: uniform scale to fit width and height, then depth
        /// squashed further if needed (barely visible from the front camera). Furniture stands on
        /// the floor against the back of the box; wall art is centered in its box on the wall.
        /// </summary>
        void PlaceObject(Transform holder, int slot, DecorLook look, int seed)
        {
            var box = Layout.SlotBox(slot);
            var model = new GameObject("Model").transform;
            model.SetParent(holder, false);
            DrawOrSpawn(model, look, seed, out var natural);

            float s = Mathf.Min(1f, box.size.x / natural.x, box.size.y / natural.y);
            float sz = Mathf.Min(s, box.size.z / natural.z);
            model.localScale = new Vector3(model.localScale.x * s, model.localScale.y * s, model.localScale.z * sz);

            if (slot == Catalog.SlotWallArt)
                foreach (var r in model.GetComponentsInChildren<Renderer>())
                    r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            float depth = natural.z * sz;
            float y = slot == Catalog.SlotWallArt ? box.center.y - natural.y * s * 0.5f : box.min.y;
            holder.localPosition = new Vector3(box.center.x, y, box.max.z - depth * 0.5f);
        }

        /// <summary>The model for the look's key when it exists, otherwise the procedural shape.</summary>
        void DrawOrSpawn(Transform model, DecorLook look, int seed, out Vector3 natural)
        {
            natural = DecorBuilder.NaturalSize(look);
            var prefab = look.model != null ? assets.Model(look.ModelKey) : null;
            if (prefab == null)
            {
                builder.Draw(model, look, seed);
                return;
            }
            var spawned = assets.SpawnModel(prefab, look.ModelKey, model, natural.y, natural.x, FabricColors(look), out var size);
            // Turn about the base center; the model is centered there, so its footprint stays put.
            spawned.transform.localRotation = Quaternion.Euler(0f, look.ModelYaw, 0f);
            if (size.z > natural.z && size.z > 0f)
            {
                // Squash an over-deep model to the shape's depth and keep it centered.
                var t = spawned.transform;
                t.localScale = new Vector3(1f, 1f, natural.z / size.z);
                size.z = natural.z;
            }
            if (size.x > 0f && size.y > 0f)
                natural = size;
        }

        /// <summary>Seats take the option's own fabric color, so two sofas from one model still differ.</summary>
        static Dictionary<string, Color> FabricColors(DecorLook look)
        {
            if (look.shape != DecorShape.Seat)
                return null;
            return new Dictionary<string, Color> { { "carpet", look.a }, { "carpetBlue", look.a } };
        }

        // ---- Completion star ----

        void SetStar(Transform root, bool complete)
        {
            var star = root.Find(StarName);
            if (complete && star == null)
            {
                var go = assets.CreatePrimitive(PrimitiveType.Quad, StarName, root, Layout.StarPoint,
                    Vector3.one * config.decorStarSize, materials.GetCutout(sprites.Star.texture, config.decorGold));
                go.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            else if (!complete && star != null)
            {
                Remove(star.gameObject);
            }
        }

        /// <summary>Destroys at the end of the frame, renamed first so lookups by name skip it now.</summary>
        static void Remove(GameObject go)
        {
            go.name = RemovedName;
            go.SetActive(false);
            UnityEngine.Object.Destroy(go);
        }
    }
}
