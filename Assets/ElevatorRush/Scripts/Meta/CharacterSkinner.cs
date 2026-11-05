using UnityEngine;

namespace ElevatorRush
{
    /// <summary>
    /// Character skins (home plan 7) built from accessories on the existing character, since
    /// the CC0 character pack has no outfit variants. The body keeps its destination tint;
    /// accessories use neutral colors (white, black, greys, browns, gold, silver) so a skin
    /// never makes a destination harder to read. Head parts follow the head bone and body parts
    /// the torso bone, so they move with the animation.
    /// </summary>
    public sealed class CharacterSkinner
    {
        const int ConeSides = 12;
        // A hat never rises more than this above the head; the icon bubble lifts by the hat's height.
        const float MaxHatHeight = 0.35f;

        // Neutral accessory colors, each kept clearly apart from every destination color (see the tests).
        static readonly Color White = new Color(0.97f, 0.97f, 0.95f);
        static readonly Color OffWhite = new Color(0.9f, 0.88f, 0.84f);
        static readonly Color Black = new Color(0.12f, 0.12f, 0.14f);
        static readonly Color Charcoal = new Color(0.25f, 0.26f, 0.3f);
        static readonly Color Grey = new Color(0.42f, 0.43f, 0.46f);
        static readonly Color Silver = new Color(0.8f, 0.82f, 0.86f);
        static readonly Color Brown = new Color(0.45f, 0.3f, 0.2f);
        static readonly Color Straw = new Color(0.93f, 0.86f, 0.68f);
        // Brass rather than gold: gold reads as the yellow destination.
        static readonly Color Brass = new Color(0.74f, 0.68f, 0.52f);
        static readonly Color Lens = new Color(0.75f, 0.85f, 0.95f);
        static readonly Color Visor = new Color(0.3f, 0.35f, 0.45f);

        /// <summary>Every color a skin accessory uses.</summary>
        public static readonly Color[] AccessoryColors = { White, OffWhite, Black, Charcoal, Grey, Silver, Brown, Straw, Brass, Lens, Visor };

        readonly MaterialLibrary materials;
        readonly AssetLibrary assets;

        // The group being built and its size unit per axis (head or torso width, and depth on Z,
        // so 0.5 on Z is the face or the chest surface).
        Transform group;
        Vector3 unit;

        public CharacterSkinner(MaterialLibrary materials, AssetLibrary assets)
        {
            this.materials = materials;
            this.assets = assets;
        }

        /// <summary>
        /// Builds a skin's accessories. Each group is placed under its bone with the bone-space
        /// pose of the rest head or chest center, given as matrices from figure space.
        /// </summary>
        public SkinParts Build(string skinId, SkinRig rig)
        {
            var parts = new SkinParts();
            if (skinId == Catalog.DefaultSkin || Catalog.Find(skinId) == null)
                return parts;

            parts.head = Group("SkinHead", rig.headBone, rig.headAnchor);
            unit = rig.headUnit;
            group = parts.head.transform;
            parts.hatHeight = Mathf.Min(MaxHatHeight, BuildHead(skinId, rig));

            parts.body = Group("SkinBody", rig.bodyBone, rig.bodyAnchor);
            unit = rig.bodyUnit;
            group = parts.body.transform;
            BuildBody(skinId, rig);
            // Accessories are small; their shadows would only add triangles to the shadow pass.
            foreach (var g in new[] { parts.head, parts.body })
                foreach (var r in g.GetComponentsInChildren<Renderer>())
                    r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return parts;
        }

        static GameObject Group(string name, Transform bone, Matrix4x4 anchor)
        {
            var go = new GameObject(name);
            var t = go.transform;
            t.SetParent(bone, false);
            t.localPosition = anchor.GetColumn(3);
            t.localRotation = anchor.rotation;
            t.localScale = anchor.lossyScale;
            return go;
        }

        // ---- Head accessories (positions in head widths from the head center; +Z is the face) ----

        /// <summary>Returns the hat height above the head top, in meters.</summary>
        float BuildHead(string id, SkinRig rig)
        {
            float top = rig.headTop;
            switch (id)
            {
                case "skin.office":
                    Glasses(Black);
                    return 0f;
                case "skin.builder":
                    Dome(top - 0.02f, 1.06f, 0.28f, White);
                    Cyl(0f, top - 0.1f, 0.12f, 1.2f, 0.04f, White);
                    Box(-0.04f, top + 0.2f, -0.5f, 0.04f, top + 0.32f, 0.5f, OffWhite);
                    return 0.3f * unit.y;
                case "skin.chef":
                    Cyl(0f, top - 0.1f, 0f, 0.95f, 0.3f, White);
                    Ball(0f, top + 0.32f, 0f, 1.05f, 0.4f, 1.05f, White);
                    return 0.5f * unit.y;
                case "skin.tourist":
                    Cyl(0f, top - 0.06f, 0f, 1.4f, 0.04f, Straw);
                    Cyl(0f, top - 0.04f, 0f, 0.9f, 0.22f, Straw);
                    Cyl(0f, top + 0.02f, 0f, 0.92f, 0.06f, Brown);
                    Glasses(Black);
                    return 0.2f * unit.y;
                case "skin.dj":
                    Box(-0.56f, top - 0.6f, -0.12f, -0.46f, top + 0.06f, 0.12f, Black);
                    Box(0.46f, top - 0.6f, -0.12f, 0.56f, top + 0.06f, 0.12f, Black);
                    Box(-0.56f, top - 0.02f, -0.12f, 0.56f, top + 0.08f, 0.12f, Black);
                    CylX(-0.66f, -0.5f, top - 0.6f, 0f, 0.36f, Charcoal);
                    CylX(0.5f, 0.66f, top - 0.6f, 0f, 0.36f, Charcoal);
                    return 0.08f * unit.y;
                case "skin.cowboy":
                    Cyl(0f, top - 0.1f, 0f, 1.45f, 0.04f, Brown);
                    Cyl(0f, top - 0.08f, 0f, 0.82f, 0.32f, Brown);
                    Cyl(0f, top - 0.04f, 0f, 0.84f, 0.06f, Black);
                    return 0.25f * unit.y;
                case "skin.wizard":
                    Cyl(0f, top - 0.1f, 0f, 1.2f, 0.04f, Charcoal);
                    Cone(0f, top - 0.08f, 0f, 0.9f, 0.6f, Charcoal);
                    Ball(0.12f, top + 0.12f, 0.43f, 0.12f, 0.12f, 0.04f, Brass);
                    Ball(0f, top - 0.92f, 0.45f, 0.45f, 0.32f, 0.18f, White);
                    return 0.54f * unit.y;
                case "skin.astronaut":
                    Ball(0f, top - 0.5f, 0f, 1.3f, 1.25f, 1.3f, materials.GetShiny(Silver));
                    Ball(0f, top - 0.5f, 0.2f, 1.05f, 0.85f, 1f, materials.GetShiny(Visor));
                    return 0.12f * unit.y;
                case "skin.robot":
                    Box(-0.58f, top - 1f, -0.55f, 0.58f, top + 0.08f, 0.55f, materials.GetShiny(Silver));
                    Box(-0.4f, top - 0.6f, 0.55f, -0.12f, top - 0.38f, 0.58f, materials.GetUnlit(OffWhite));
                    Box(0.12f, top - 0.6f, 0.55f, 0.4f, top - 0.38f, 0.58f, materials.GetUnlit(OffWhite));
                    Box(-0.3f, top - 0.88f, 0.55f, 0.3f, top - 0.8f, 0.58f, Charcoal);
                    Cyl(0f, top + 0.08f, 0f, 0.06f, 0.25f, Grey);
                    Ball(0f, top + 0.36f, 0f, 0.16f, 0.16f, 0.16f, Brass);
                    return 0.44f * unit.y;
                case "skin.pirate":
                    Box(-0.75f, top - 0.08f, -0.35f, 0.75f, top - 0.02f, 0.35f, Black);
                    Cyl(0f, top - 0.06f, 0f, 0.85f, 0.26f, Black);
                    Box(-0.3f, top + 0.08f, 0.33f, 0.3f, top + 0.14f, 0.38f, Brass);
                    Box(0.1f, top - 0.62f, 0.5f, 0.38f, top - 0.38f, 0.55f, Black);
                    Box(-0.55f, top - 0.38f, 0.48f, 0.55f, top - 0.34f, 0.52f, Black);
                    return 0.22f * unit.y;
                default:
                    return 0f;
            }
        }

        void Glasses(Color color)
        {
            // Eye level sits a little below the head center (the hair takes the top).
            float y = -0.1f;
            Box(-0.42f, y - 0.03f, 0.5f, -0.08f, y + 0.17f, 0.54f, color);
            Box(0.08f, y - 0.03f, 0.5f, 0.42f, y + 0.17f, 0.54f, color);
            Box(-0.38f, y + 0.01f, 0.535f, -0.12f, y + 0.13f, 0.55f, materials.GetShiny(Lens));
            Box(0.12f, y + 0.01f, 0.535f, 0.38f, y + 0.13f, 0.55f, materials.GetShiny(Lens));
            Box(-0.08f, y + 0.08f, 0.5f, 0.08f, y + 0.12f, 0.54f, color);
        }

        // ---- Body accessories (positions in body widths from the chest center; +Z is the front) ----

        void BuildBody(string id, SkinRig rig)
        {
            switch (id)
            {
                case "skin.office":
                    Box(-0.08f, -0.35f, 0.5f, 0.08f, 0.25f, 0.56f, Charcoal);
                    Box(-0.12f, 0.2f, 0.5f, 0.12f, 0.3f, 0.56f, Charcoal);
                    break;
                case "skin.builder":
                    Box(-0.56f, -0.5f, -0.56f, 0.56f, -0.36f, 0.56f, Brown);
                    Box(0.3f, -0.62f, 0.5f, 0.46f, -0.36f, 0.62f, Grey);
                    break;
                case "skin.chef":
                    Box(-0.3f, -0.4f, 0.5f, 0.3f, 0.12f, 0.56f, White);
                    Box(-0.3f, 0.22f, 0.45f, 0.3f, 0.34f, 0.58f, White);
                    break;
                case "skin.tourist":
                    Box(-0.18f, -0.12f, 0.52f, 0.18f, 0.12f, 0.68f, Black);
                    Cyl(0f, -0.02f, 0.68f, 0.14f, 0.04f, Grey, true);
                    Box(-0.38f, 0.1f, 0.48f, -0.32f, 0.36f, 0.52f, Brown);
                    Box(0.32f, 0.1f, 0.48f, 0.38f, 0.36f, 0.52f, Brown);
                    break;
                case "skin.cowboy":
                    Box(-0.56f, -0.5f, -0.56f, 0.56f, -0.4f, 0.56f, Brown);
                    Box(-0.12f, -0.52f, 0.54f, 0.12f, -0.38f, 0.6f, materials.GetShiny(Silver));
                    Box(-0.3f, 0.12f, 0.48f, 0.3f, 0.34f, 0.56f, OffWhite);
                    break;
                case "skin.wizard":
                    Box(-0.6f, -0.62f, -0.62f, 0.6f, 0.35f, -0.45f, Charcoal);
                    Cyl(0.62f, -0.6f, 0.3f, 0.06f, 1.5f, Brown);
                    Ball(0.62f, 0.95f, 0.3f, 0.18f, 0.18f, 0.18f, materials.GetShiny(Silver));
                    break;
                case "skin.astronaut":
                    Box(-0.42f, -0.4f, -0.8f, 0.42f, 0.35f, -0.48f, White);
                    Box(-0.22f, -0.15f, 0.5f, 0.22f, 0.12f, 0.58f, Grey);
                    break;
                case "skin.robot":
                    Box(-0.32f, -0.3f, 0.5f, 0.32f, 0.2f, 0.58f, materials.GetShiny(Silver));
                    Ball(-0.15f, -0.05f, 0.6f, 0.1f, 0.1f, 0.04f, materials.GetUnlit(OffWhite));
                    Ball(0.15f, -0.05f, 0.6f, 0.1f, 0.1f, 0.04f, Brass);
                    break;
                case "skin.pirate":
                    Box(-0.56f, -0.48f, -0.56f, 0.56f, -0.38f, 0.56f, Black);
                    Box(-0.1f, -0.5f, 0.54f, 0.1f, -0.36f, 0.6f, Brass);
                    break;
                case "skin.royal":
                    // An ermine cape behind the back and a gold scepter.
                    Box(-0.62f, -0.68f, -0.66f, 0.62f, 0.38f, -0.5f, White);
                    for (int i = 0; i < 6; i++)
                        Box(-0.45f + (i % 3) * 0.4f, -0.5f + (i / 3) * 0.5f, -0.68f, -0.39f + (i % 3) * 0.4f, -0.38f + (i / 3) * 0.5f, -0.65f, Black);
                    Box(-0.6f, 0.3f, -0.62f, 0.6f, 0.42f, -0.1f, White);
                    Cyl(0.66f, -0.5f, 0.35f, 0.07f, 1.2f, materials.GetShiny(Brass));
                    Ball(0.66f, 0.72f, 0.35f, 0.2f, 0.2f, 0.2f, materials.GetShiny(Brass));
                    break;
            }
        }

        // ---- Part helpers, in units of the group's size ----

        Material M(Color color) => materials.Get(color);

        /// <summary>A point or size in group units to meters.</summary>
        Vector3 U(float x, float y, float z) => new Vector3(x * unit.x, y * unit.y, z * unit.z);

        /// <summary>A size for a part turned to face forward (its Y axis along Z): swaps the Y and Z units.</summary>
        Vector3 U(float x, float y, float z, bool facing) => new Vector3(x * unit.x, y * unit.z, z * unit.y);

        void Box(float x0, float y0, float z0, float x1, float y1, float z1, Color color) => Box(x0, y0, z0, x1, y1, z1, M(color));

        void Box(float x0, float y0, float z0, float x1, float y1, float z1, Material m)
        {
            assets.CreateBox("Part", group, U(x0, y0, z0), U(x1, y1, z1), m);
        }

        void Cyl(float x, float y0, float z, float diameter, float height, Color color, bool facing = false)
        {
            var go = assets.CreatePrimitive(PrimitiveType.Cylinder, "Part", group, U(x, y0 + height * 0.5f, z),
                facing ? U(diameter, height * 0.5f, diameter, true) : U(diameter, height * 0.5f, diameter), M(color));
            if (facing)
                go.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        }

        void Cyl(float x, float y0, float z, float diameter, float height, Material m)
        {
            assets.CreatePrimitive(PrimitiveType.Cylinder, "Part", group, U(x, y0 + height * 0.5f, z),
                U(diameter, height * 0.5f, diameter), m);
        }

        void CylX(float x0, float x1, float y, float z, float diameter, Color color)
        {
            // Lying along X: its length axis is the group's X, its round section uses the Y unit.
            var go = assets.CreatePrimitive(PrimitiveType.Cylinder, "Part", group, U((x0 + x1) * 0.5f, y, z),
                new Vector3(diameter * unit.y, (x1 - x0) * 0.5f * unit.x, diameter * unit.y), M(color));
            go.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        }

        void Ball(float x, float y, float z, float sx, float sy, float sz, Color color) => Ball(x, y, z, sx, sy, sz, M(color));

        void Ball(float x, float y, float z, float sx, float sy, float sz, Material m)
        {
            assets.CreateLowSphere("Part", group, U(x, y, z), U(sx, sy, sz), m);
        }

        /// <summary>A flattened cap over the top of the head (a helmet), <paramref name="height"/> above its center line.</summary>
        void Dome(float y, float diameter, float height, Color color)
        {
            Ball(0f, y, 0f, diameter, height * 2f, diameter * 1.05f, color);
        }

        void Cone(float x, float y0, float z, float diameter, float height, Color color)
        {
            assets.CreateMesh(assets.ConeMesh(ConeSides), "Part", group, U(x, y0, z),
                U(diameter, height, diameter), M(color));
        }
    }
}
