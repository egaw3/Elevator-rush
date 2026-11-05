using UnityEngine;

namespace ElevatorRush
{
    /// <summary>
    /// Builds decor objects from primitives (the fallback for every decor model key). Each shape
    /// is drawn at its natural size in meters, centered on X and Z with its base at Y = 0; parts
    /// are given as fractions of that size (x and z from 0 at the left/front to 1 at the
    /// right/back, y from 0 to 1). Placement and fitting into a room slot happen in
    /// <see cref="RoomDecorator"/>.
    /// </summary>
    public sealed class DecorBuilder
    {
        const int ConeSides = 10;
        // Prop models (Kenney Furniture Kit and Food Kit); each has a primitive fallback.
        const string Furniture = "decor/furniture/";
        const string Food = "decor/food/";
        static readonly string[] Cakes = { "cake", "cupcake", "donut-sprinkles", "cupcake" };
        const float WallArtDepth = 0.05f;

        readonly MaterialLibrary materials;
        readonly AssetLibrary assets;

        // The object being built.
        Transform target;
        Vector3 size;
        DecorLook look;
        System.Random random;

        public DecorBuilder(MaterialLibrary materials, AssetLibrary assets)
        {
            this.materials = materials;
            this.assets = assets;
        }

        /// <summary>Natural size of a shape in meters (x wide, y tall, z deep).</summary>
        public static Vector3 NaturalSize(DecorLook look)
        {
            int v = look.variant;
            switch (look.shape)
            {
                case DecorShape.Plant:
                    return v == DecorLooks.PlantSmall ? new Vector3(0.5f, 0.95f, 0.5f)
                        : v == DecorLooks.PlantPalm ? new Vector3(1.1f, 1.55f, 0.7f)
                        : v == DecorLooks.PlantBamboo ? new Vector3(0.7f, 1.55f, 0.45f)
                        : v == DecorLooks.PlantBonsai ? new Vector3(0.8f, 0.9f, 0.5f)
                        : new Vector3(0.9f, 1f, 0.6f);
                case DecorShape.Counter:
                    return v == DecorLooks.CounterCart ? new Vector3(0.8f, 0.85f, 0.45f)
                        : v == DecorLooks.CounterBar ? new Vector3(1.4f, 1.5f, 0.55f)
                        : new Vector3(1.25f, 0.72f, 0.55f);
                case DecorShape.Desk:
                    return v == DecorLooks.DeskStanding ? new Vector3(1f, 1f, 0.55f)
                        : v == DecorLooks.DeskExecutive ? new Vector3(1.4f, 0.95f, 0.7f)
                        : new Vector3(1.2f, 0.95f, 0.6f);
                case DecorShape.Seat:
                    switch (v)
                    {
                        case DecorLooks.SeatBench: return new Vector3(1.2f, 0.5f, 0.45f);
                        case DecorLooks.SeatArmchair: return new Vector3(0.8f, 0.8f, 0.7f);
                        case DecorLooks.SeatArmchairLamp: return new Vector3(1.25f, 1.3f, 0.7f);
                        case DecorLooks.SeatBooth: return new Vector3(1.4f, 1f, 0.6f);
                        case DecorLooks.SeatLounger: return new Vector3(1.4f, 0.6f, 0.6f);
                        case DecorLooks.SeatMassage: return new Vector3(1.4f, 0.7f, 0.55f);
                        case DecorLooks.SeatCorner: return new Vector3(1.5f, 0.75f, 0.8f);
                        default: return new Vector3(1.45f, 0.75f, 0.7f);
                    }
                case DecorShape.TableChairs:
                    return new Vector3(1.4f, 0.85f, 0.6f);
                case DecorShape.Shelf:
                    return v == DecorLooks.ShelfTall ? new Vector3(1.2f, 1.6f, 0.4f)
                        : v == DecorLooks.ShelfLadder ? new Vector3(1.35f, 1.6f, 0.5f)
                        : v == DecorLooks.ShelfFiling ? new Vector3(1.2f, 1.15f, 0.5f)
                        : v == DecorLooks.ShelfMats || v == DecorLooks.ShelfTowels ? new Vector3(0.9f, 1.1f, 0.4f)
                        : new Vector3(1f, 1.5f, 0.4f);
                case DecorShape.Appliance:
                    switch (v)
                    {
                        case DecorLooks.ApplianceCoffee: return new Vector3(0.6f, 1.2f, 0.5f);
                        case DecorLooks.ApplianceEspresso: return new Vector3(1.2f, 1.1f, 0.55f);
                        case DecorLooks.ApplianceRoaster: return new Vector3(1f, 1.3f, 0.6f);
                        case DecorLooks.ApplianceCooler: return new Vector3(0.42f, 1.1f, 0.4f);
                        case DecorLooks.ApplianceStation: return new Vector3(1f, 1.05f, 0.5f);
                        case DecorLooks.ApplianceRacing: return new Vector3(1.2f, 1.45f, 0.7f);
                        case DecorLooks.ApplianceTwin: return new Vector3(1.4f, 1.55f, 0.6f);
                        case DecorLooks.ApplianceClaw: return new Vector3(0.8f, 1.5f, 0.7f);
                        default: return new Vector3(0.7f, 1.55f, 0.6f);
                    }
                case DecorShape.Machine:
                    switch (v)
                    {
                        case DecorLooks.MachineBike: return new Vector3(0.95f, 1f, 0.5f);
                        case DecorLooks.MachineRower: return new Vector3(1.5f, 0.6f, 0.45f);
                        case DecorLooks.MachinePinball: return new Vector3(1.2f, 1.05f, 0.6f);
                        case DecorLooks.MachineHockey: return new Vector3(1.3f, 0.8f, 0.7f);
                        case DecorLooks.MachineDance: return new Vector3(1.2f, 1.2f, 0.7f);
                        default: return new Vector3(1.3f, 1.15f, 0.6f);
                    }
                case DecorShape.Round:
                    return v == DecorLooks.RoundBall ? new Vector3(0.62f, 0.62f, 0.62f)
                        : v == DecorLooks.RoundBag ? new Vector3(0.7f, 1.55f, 0.5f)
                        : new Vector3(0.65f, 1.1f, 0.55f);
                case DecorShape.Rack:
                    return v == DecorLooks.RackDumbbells ? new Vector3(1.25f, 0.75f, 0.45f)
                        : v == DecorLooks.RackWeights ? new Vector3(1f, 1.1f, 0.5f)
                        : new Vector3(1.25f, 1.6f, 0.6f);
                case DecorShape.Tub:
                    return v == DecorLooks.TubFountain ? new Vector3(0.85f, 1.25f, 0.6f) : new Vector3(1.35f, 0.6f, 0.75f);
                case DecorShape.Piano:
                    return v == DecorLooks.PianoGrand ? new Vector3(1.45f, 1.05f, 0.9f) : new Vector3(1.3f, 1.1f, 0.6f);
                case DecorShape.WallArt:
                    switch (v)
                    {
                        case DecorLooks.ArtClock: return new Vector3(0.5f, 0.5f, WallArtDepth);
                        case DecorLooks.ArtMirror: return new Vector3(0.55f, 0.75f, WallArtDepth);
                        case DecorLooks.ArtSconces: return new Vector3(1f, 0.5f, 0.18f);
                        case DecorLooks.ArtChandelier: return new Vector3(0.75f, 0.65f, 0.4f);
                        case DecorLooks.ArtNeon:
                        case DecorLooks.ArtJoystick: return new Vector3(0.8f, 0.55f, WallArtDepth);
                        case DecorLooks.ArtTrophies: return new Vector3(1f, 0.55f, 0.22f);
                        default: return new Vector3(0.9f, 0.62f, WallArtDepth);
                    }
                default:
                    return Vector3.one * 0.5f;
            }
        }

        /// <summary>Draws a shape under <paramref name="parent"/> at its natural size.</summary>
        public void Draw(Transform parent, DecorLook look, int seed)
        {
            target = parent;
            this.look = look;
            size = NaturalSize(look);
            random = new System.Random(seed);
            switch (look.shape)
            {
                case DecorShape.Plant: Plant(); break;
                case DecorShape.Counter: Counter(); break;
                case DecorShape.Desk: Desk(); break;
                case DecorShape.Seat: Seat(); break;
                case DecorShape.TableChairs: TableChairs(); break;
                case DecorShape.Shelf: Shelf(); break;
                case DecorShape.Appliance: Appliance(); break;
                case DecorShape.Machine: Machine(); break;
                case DecorShape.Round: Round(); break;
                case DecorShape.Rack: Rack(); break;
                case DecorShape.Tub: Tub(); break;
                case DecorShape.Piano: Piano(); break;
                case DecorShape.WallArt: WallArt(); break;
            }
        }

        // ---- Materials ----

        Material A => materials.Get(look.a);
        Material B => materials.Get(look.b);
        Material C => materials.Get(look.c);
        Material Shiny => materials.GetShiny(look.c);
        Material Glow(Color color) => materials.GetUnlit(color);
        Material Dark => materials.Get(new Color(0.13f, 0.13f, 0.15f));
        Material White => materials.Get(new Color(0.96f, 0.96f, 0.95f));
        Material Metal => materials.GetShiny(new Color(0.75f, 0.77f, 0.8f));

        Material RandomColor()
        {
            var c = Color.HSVToRGB((float)random.NextDouble(), 0.45f + 0.3f * (float)random.NextDouble(), 0.55f + 0.35f * (float)random.NextDouble());
            return materials.Get(c);
        }

        // ---- Part helpers, in fractions of the natural size ----

        Vector3 N(float x, float y, float z) => new Vector3((x - 0.5f) * size.x, y * size.y, (z - 0.5f) * size.z);

        /// <summary>
        /// A small model prop (a monitor, a cake, a bottle) standing at (x, y, z) in fractions,
        /// <paramref name="height"/> as a fraction of the object's height. Returns false when the
        /// model is not on disk, so the caller draws its primitive fallback instead.
        /// </summary>
        bool Prop(string key, float x, float y, float z, float height)
        {
            var prefab = assets.Model(key);
            if (prefab == null)
                return false;
            var prop = assets.SpawnModel(prefab, key, target, height * size.y);
            prop.transform.localPosition = N(x, y, z);
            return true;
        }

        void Box(float x0, float y0, float z0, float x1, float y1, float z1, Material m)
        {
            assets.CreateBox("Part", target, N(x0, y0, z0), N(x1, y1, z1), m);
        }

        /// <summary>Upright cylinder; diameter as a fraction of the width.</summary>
        void Cyl(float cx, float y0, float cz, float diameter, float y1, Material m)
        {
            float d = diameter * size.x;
            assets.CreatePrimitive(PrimitiveType.Cylinder, "Part", target, N(cx, (y0 + y1) * 0.5f, cz),
                new Vector3(d, (y1 - y0) * size.y * 0.5f, d), m);
        }

        /// <summary>Cylinder lying along X from x0 to x1; diameter as a fraction of the height.</summary>
        void CylX(float x0, float x1, float cy, float cz, float diameter, Material m)
        {
            float d = diameter * size.y;
            var go = assets.CreatePrimitive(PrimitiveType.Cylinder, "Part", target, N((x0 + x1) * 0.5f, cy, cz),
                new Vector3(d, (x1 - x0) * size.x * 0.5f, d), m);
            go.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        }

        /// <summary>A disc facing the camera (axis along Z); diameter as a fraction of the height.</summary>
        void Disc(float cx, float cy, float z, float diameter, float thickness, Material m)
        {
            float d = diameter * size.y;
            var go = assets.CreatePrimitive(PrimitiveType.Cylinder, "Part", target, N(cx, cy, z),
                new Vector3(d, thickness * 0.5f, d), m);
            go.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        }

        /// <summary>Ellipsoid with per-axis diameters as fractions of the natural size, optionally rotated.</summary>
        void Blob(float cx, float cy, float cz, float sx, float sy, float sz, Material m, float tiltZ = 0f, float yaw = 0f)
        {
            var go = assets.CreateLowSphere("Part", target, N(cx, cy, cz),
                new Vector3(sx * size.x, sy * size.y, sz * size.z), m);
            go.transform.localRotation = Quaternion.Euler(0f, yaw, tiltZ);
        }

        void Cone(float cx, float y0, float cz, float diameter, float y1, Material m)
        {
            float d = diameter * size.x;
            assets.CreateMesh(assets.ConeMesh(ConeSides), "Part", target, N(cx, y0, cz),
                new Vector3(d, (y1 - y0) * size.y, d), m);
        }

        /// <summary>A tilted box (for lounger backs, lids, ladders), rotated about Z around its center.</summary>
        void Tilted(float cx, float cy, float cz, float w, float h, float d, float angle, Material m)
        {
            var go = assets.CreatePrimitive(PrimitiveType.Cube, "Part", target, N(cx, cy, cz),
                new Vector3(w * size.x, h * size.y, d * size.z), m);
            go.transform.localRotation = Quaternion.Euler(0f, 0f, angle);
        }

        void Legs(float inset, float height, float thickness, Material m)
        {
            foreach (float x in new[] { inset, 1f - inset - thickness })
                foreach (float z in new[] { inset, 1f - inset - thickness })
                    Box(x, 0f, z, x + thickness, height, z + thickness, m);
        }

        // ---- Shapes ----

        void Plant()
        {
            switch (look.variant)
            {
                case DecorLooks.PlantSmall:
                    Cyl(0.5f, 0f, 0.5f, 0.62f, 0.36f, A);
                    Blob(0.5f, 0.62f, 0.5f, 0.95f, 0.5f, 0.95f, B);
                    Blob(0.38f, 0.82f, 0.45f, 0.5f, 0.3f, 0.6f, materials.Get(look.c * 1.4f));
                    break;
                case DecorLooks.PlantPalm:
                    Cyl(0.5f, 0f, 0.5f, 0.38f, 0.22f, A);
                    Cyl(0.5f, 0.2f, 0.5f, 0.07f, 0.85f, C);
                    for (int i = 0; i < 6; i++)
                        Blob(0.5f, 0.86f, 0.5f, 0.55f, 0.07f, 0.35f, B, i % 2 == 0 ? 25f : -25f, i * 60f);
                    break;
                case DecorLooks.PlantBamboo:
                    Box(0.15f, 0f, 0.2f, 0.85f, 0.2f, 0.8f, A);
                    for (int i = 0; i < 5; i++)
                    {
                        float x = 0.24f + i * 0.13f;
                        float top = 0.7f + 0.3f * (float)random.NextDouble();
                        Cyl(x, 0.2f, 0.5f, 0.06f, top, B);
                        Blob(x + 0.05f, top - 0.05f, 0.5f, 0.22f, 0.04f, 0.3f, C, 20f);
                    }
                    break;
                case DecorLooks.PlantBonsai:
                    Box(0.05f, 0f, 0.1f, 0.95f, 0.45f, 0.9f, A);
                    Box(0.25f, 0.45f, 0.25f, 0.75f, 0.56f, 0.75f, materials.Get(new Color(0.3f, 0.32f, 0.36f)));
                    Tilted(0.47f, 0.68f, 0.5f, 0.06f, 0.26f, 0.2f, 18f, C);
                    Blob(0.38f, 0.86f, 0.5f, 0.45f, 0.14f, 0.7f, B);
                    Blob(0.66f, 0.8f, 0.5f, 0.3f, 0.12f, 0.6f, B);
                    break;
                default: // fern and bush
                    Cyl(0.5f, 0f, 0.5f, 0.46f, 0.32f, A);
                    Blob(0.5f, 0.6f, 0.5f, 0.85f, 0.55f, 0.95f, B);
                    Blob(0.3f, 0.72f, 0.45f, 0.45f, 0.4f, 0.6f, C);
                    Blob(0.7f, 0.74f, 0.55f, 0.45f, 0.4f, 0.6f, C);
                    break;
            }
        }

        void Counter()
        {
            switch (look.variant)
            {
                case DecorLooks.CounterCart:
                    foreach (float x in new[] { 0.08f, 0.88f })
                        Box(x, 0.1f, 0.15f, x + 0.04f, 0.95f, 0.85f, Metal);
                    Box(0.05f, 0.4f, 0.1f, 0.95f, 0.44f, 0.9f, B);
                    Box(0.05f, 0.85f, 0.1f, 0.95f, 0.89f, 0.9f, B);
                    for (int i = 0; i < 4; i++)
                        if (!Prop(Food + (i % 2 == 0 ? "wine-red" : "glass-wine"), 0.2f + i * 0.2f, 0.89f, 0.5f, 0.2f))
                            Cyl(0.2f + i * 0.2f, 0.89f, 0.5f, 0.08f, 1f, i % 2 == 0 ? C : RandomColor());
                    foreach (float x in new[] { 0.12f, 0.88f })
                        Disc(x, 0.06f, 0.2f, 0.12f, 0.03f, Dark);
                    break;
                case DecorLooks.CounterBar:
                    Box(0f, 0f, 0.4f, 1f, 0.5f, 1f, A);
                    Box(-0.02f, 0.5f, 0.35f, 1.02f, 0.53f, 1f, B);
                    Box(0.05f, 0.75f, 0.85f, 0.95f, 0.77f, 1f, B);
                    Box(0.05f, 0.95f, 0.85f, 0.95f, 0.97f, 1f, B);
                    for (int i = 0; i < 8; i++)
                    {
                        float x = 0.1f + i * 0.11f;
                        if (!Prop(Food + (i % 2 == 0 ? "wine-red" : "wine-white"), x, 0.77f, 0.92f, 0.13f))
                            Cyl(x, 0.77f, 0.92f, 0.04f, 0.88f, RandomColor());
                        if (!Prop(Food + (i % 3 == 0 ? "glass-wine" : "wine-white"), x + 0.04f, 0.97f, 0.92f, 0.03f + 0.07f * (i % 2)))
                            Cyl(x + 0.04f, 0.97f, 0.92f, 0.035f, 1f, RandomColor());
                    }
                    Box(0f, 0.47f, 0.38f, 1f, 0.5f, 0.4f, Shiny);
                    Prop(Food + "cocktail", 0.3f, 0.53f, 0.6f, 0.08f);
                    Prop(Food + "cocktail", 0.7f, 0.53f, 0.55f, 0.08f);
                    foreach (float x in new[] { 0.2f, 0.5f, 0.8f })
                    {
                        Cyl(x, 0f, 0.15f, 0.03f, 0.3f, Metal);
                        Cyl(x, 0.3f, 0.15f, 0.12f, 0.33f, C);
                    }
                    break;
                case DecorLooks.CounterDisplay:
                    Box(0f, 0f, 0.05f, 1f, 0.6f, 1f, A);
                    Box(0.03f, 0.6f, 0.1f, 0.97f, 0.62f, 0.95f, White);
                    Box(0.03f, 0.62f, 0.1f, 0.97f, 0.95f, 0.14f, materials.Get(look.b));
                    for (int i = 0; i < 4; i++)
                        if (!Prop(Food + Cakes[i], 0.15f + i * 0.23f, 0.62f, 0.5f, 0.14f))
                            Cyl(0.15f + i * 0.23f, 0.62f, 0.5f, 0.12f, 0.75f, i % 2 == 0 ? C : RandomColor());
                    break;
                default: // plain and trimmed
                    Box(0f, 0f, 0.1f, 1f, 0.92f, 1f, A);
                    Box(-0.02f, 0.92f, 0.05f, 1.02f, 1f, 1f, B);
                    Box(0.06f, 0.18f, 0.08f, 0.94f, 0.82f, 0.1f, materials.Get(look.a * 0.88f));
                    if (look.variant == DecorLooks.CounterTrim)
                    {
                        Box(-0.02f, 0.88f, 0.04f, 1.02f, 0.92f, 0.08f, Shiny);
                        Box(0f, 0f, 0.07f, 1f, 0.05f, 0.1f, Shiny);
                    }
                    break;
            }
        }

        void Desk()
        {
            float top = look.variant == DecorLooks.DeskStanding ? 0.82f : 0.75f / size.y;
            Box(0f, top - 0.04f, 0f, 1f, top, 1f, A);
            if (look.variant == DecorLooks.DeskExecutive)
            {
                Box(0.02f, 0f, 0.1f, 0.3f, top - 0.04f, 0.95f, A);
                Box(0.7f, 0f, 0.1f, 0.98f, top - 0.04f, 0.95f, A);
                Box(0.14f, 0.45f, 0.08f, 0.18f, 0.48f, 0.1f, Shiny);
                Box(0.82f, 0.45f, 0.08f, 0.86f, 0.48f, 0.1f, Shiny);
                if (!Prop(Furniture + "lampSquareTable", 0.85f, top, 0.7f, 0.26f))
                {
                    Cyl(0.85f, top, 0.7f, 0.04f, top + 0.18f, Shiny);
                    Cone(0.85f, top + 0.16f, 0.7f, 0.14f, top + 0.25f, Glow(new Color(1f, 0.92f, 0.7f)));
                }
            }
            else
            {
                Legs(0.03f, top - 0.04f, 0.04f, look.variant == DecorLooks.DeskStanding ? Metal : materials.Get(look.a * 0.8f));
            }

            if (look.variant == DecorLooks.DeskReading)
            {
                if (!Prop(Furniture + "lampRoundTable", 0.8f, top, 0.6f, 0.28f))
                {
                    Cyl(0.8f, top, 0.6f, 0.04f, top + 0.2f, B);
                    Cone(0.8f, top + 0.18f, 0.6f, 0.16f, top + 0.27f, Glow(look.c));
                }
                if (!Prop(Furniture + "books", 0.3f, top, 0.5f, 0.12f))
                    for (int i = 0; i < 3; i++)
                        Box(0.2f, top + i * 0.03f, 0.35f, 0.42f, top + (i + 1) * 0.03f, 0.65f, RandomColor());
            }
            else
            {
                // A monitor with a lit screen, keyboard in front.
                float mx = look.variant == DecorLooks.DeskExecutive ? 0.4f : 0.5f;
                bool laptop = look.variant == DecorLooks.DeskStanding;
                if (!Prop(Furniture + (laptop ? "laptop" : "computerScreen"), mx, top, 0.6f, laptop ? 0.1f : 0.28f))
                {
                    Box(mx - 0.02f, top, 0.65f, mx + 0.02f, top + 0.08f, 0.7f, B);
                    Box(mx - 0.17f, top + 0.08f, 0.66f, mx + 0.17f, top + 0.26f, 0.7f, B);
                    Box(mx - 0.15f, top + 0.1f, 0.655f, mx + 0.15f, top + 0.24f, 0.66f, Glow(new Color(0.55f, 0.8f, 1f)));
                }
                if (!laptop && !Prop(Furniture + "computerKeyboard", mx, top, 0.3f, 0.03f))
                    Box(mx - 0.15f, top, 0.3f, mx + 0.15f, top + 0.015f, 0.42f, White);
            }
        }

        void Seat()
        {
            var body = A;
            var cushion = B;
            switch (look.variant)
            {
                case DecorLooks.SeatBench:
                    Box(0f, 0.75f, 0.1f, 1f, 0.88f, 0.9f, body);
                    Legs(0.06f, 0.75f, 0.06f, cushion);
                    break;
                case DecorLooks.SeatLounger:
                    Box(0f, 0.25f, 0.05f, 0.72f, 0.42f, 0.95f, body);
                    Tilted(0.82f, 0.6f, 0.5f, 0.3f, 0.12f, 0.9f, 55f, body);
                    Legs(0.05f, 0.25f, 0.05f, cushion);
                    break;
                case DecorLooks.SeatMassage:
                    Box(0f, 0.6f, 0.05f, 1f, 0.78f, 0.95f, body);
                    Box(0.02f, 0.78f, 0.1f, 0.98f, 0.82f, 0.9f, materials.Get(look.a * 0.95f));
                    Legs(0.06f, 0.6f, 0.05f, cushion);
                    Disc(0.06f, 0.8f, 0.5f, 0.15f, 0.02f, cushion);
                    break;
                case DecorLooks.SeatBooth:
                    Box(0f, 0f, 0.5f, 1f, 0.45f, 1f, body);
                    Box(0f, 0.45f, 0.85f, 1f, 1f, 1f, body);
                    Box(0.03f, 0.45f, 0.52f, 0.97f, 0.52f, 0.85f, cushion);
                    Box(0.15f, 0.7f, 0f, 0.85f, 0.75f, 0.45f, C);
                    Box(0.47f, 0f, 0.15f, 0.53f, 0.7f, 0.25f, Metal);
                    break;
                case DecorLooks.SeatArmchair:
                case DecorLooks.SeatArmchairLamp:
                {
                    float w = look.variant == DecorLooks.SeatArmchair ? 1f : 0.64f;
                    Box(0f, 0f, 0.05f, w, 0.5f, 1f, body);
                    Box(0f, 0.5f, 0.75f, w, 1f, 1f, body);
                    Box(0f, 0.5f, 0.05f, w * 0.15f, 0.72f, 1f, body);
                    Box(w * 0.85f, 0.5f, 0.05f, w, 0.72f, 1f, body);
                    Box(w * 0.15f, 0.5f, 0.08f, w * 0.85f, 0.58f, 0.75f, cushion);
                    if (look.variant == DecorLooks.SeatArmchairLamp)
                    {
                        if (!Prop("props_room/lampRoundFloor", 0.86f, 0f, 0.6f, 1f))
                        {
                            Cyl(0.86f, 0f, 0.6f, 0.2f, 0.02f, Dark);
                            Cyl(0.86f, 0f, 0.6f, 0.03f, 0.8f, Dark);
                            Cone(0.86f, 0.75f, 0.6f, 0.26f, 1f, Glow(look.c));
                        }
                    }
                    break;
                }
                default: // sofa, chesterfield, corner sofa
                {
                    bool rolled = look.variant == DecorLooks.SeatChesterfield;
                    Box(0.05f, 0.1f, 0.05f, 0.95f, 0.5f, 1f, body);
                    Box(0.05f, 0.5f, 0.72f, 0.95f, 1f, 1f, body);
                    Box(0.08f, 0.5f, 0.08f, 0.5f, 0.6f, 0.72f, cushion);
                    Box(0.5f, 0.5f, 0.08f, 0.92f, 0.6f, 0.72f, cushion);
                    if (rolled)
                    {
                        Box(0f, 0.1f, 0.05f, 0.07f, 0.65f, 1f, body);
                        Box(0.93f, 0.1f, 0.05f, 1f, 0.65f, 1f, body);
                        CylX(-0.02f, 0.09f, 0.7f, 0.5f, 0.18f, body);
                        CylX(0.91f, 1.02f, 0.7f, 0.5f, 0.18f, body);
                        Legs(0.03f, 0.1f, 0.05f, Shiny);
                    }
                    else
                    {
                        Box(0f, 0.1f, 0.05f, 0.07f, 0.72f, 1f, body);
                        Box(0.93f, 0.1f, 0.05f, 1f, 0.72f, 1f, body);
                        Legs(0.05f, 0.1f, 0.04f, C);
                    }
                    if (look.variant == DecorLooks.SeatCorner)
                        Box(0.62f, 0.1f, -0.02f, 0.95f, 0.6f, 0.3f, cushion);
                    break;
                }
            }
        }

        void TableChairs()
        {
            bool bistro = look.variant == DecorLooks.TableBistro;
            if (bistro)
            {
                Cyl(0.5f, 0f, 0.5f, 0.3f, 0.02f, A);
                Cyl(0.5f, 0f, 0.5f, 0.04f, 0.85f, A);
                Cyl(0.5f, 0.85f, 0.5f, 0.45f, 0.88f, materials.Get(look.c));
                Cyl(0.5f, 0.84f, 0.5f, 0.46f, 0.86f, Shiny);
            }
            else
            {
                Box(0.28f, 0.82f, 0.1f, 0.72f, 0.88f, 0.9f, A);
                Box(0.47f, 0f, 0.45f, 0.53f, 0.82f, 0.55f, A);
            }
            foreach (float side in new[] { 0f, 1f })
            {
                float x0 = side == 0f ? 0.02f : 0.78f;
                float x1 = x0 + 0.2f;
                var chair = bistro ? Shiny : B;
                Box(x0, 0.5f, 0.2f, x1, 0.55f, 0.8f, chair);
                foreach (float x in new[] { x0, x1 - 0.03f })
                    foreach (float z in new[] { 0.2f, 0.77f })
                        Box(x, 0f, z, x + 0.03f, 0.5f, z + 0.03f, chair);
                float back = side == 0f ? x0 : x1 - 0.03f;
                Box(back, 0.55f, 0.2f, back + 0.03f, 1f, 0.8f, chair);
            }
        }

        void Shelf()
        {
            var frame = A;
            switch (look.variant)
            {
                case DecorLooks.ShelfFiling:
                    foreach (float x0 in new[] { 0f, 0.51f })
                    {
                        Box(x0, 0f, 0f, x0 + 0.49f, 1f, 1f, frame);
                        for (int i = 0; i < 4; i++)
                        {
                            float y = 0.05f + i * 0.24f;
                            Box(x0 + 0.03f, y, -0.02f, x0 + 0.46f, y + 0.2f, 0.02f, B);
                            Box(x0 + 0.18f, y + 0.08f, -0.04f, x0 + 0.31f, y + 0.11f, 0f, Metal);
                        }
                    }
                    break;
                case DecorLooks.ShelfMats:
                    Box(0f, 0f, 0f, 0.05f, 1f, 1f, frame);
                    Box(0.95f, 0f, 0f, 1f, 1f, 1f, frame);
                    for (int row = 0; row < 3; row++)
                    {
                        float y = 0.05f + row * 0.33f;
                        Box(0.05f, y, 0f, 0.95f, y + 0.03f, 1f, frame);
                        for (int i = 0; i < 2; i++)
                            CylX(0.08f, 0.92f, y + 0.12f + i * 0.0f, 0.3f + i * 0.4f, 0.17f, i == 0 ? B : C);
                    }
                    break;
                case DecorLooks.ShelfTowels:
                    Box(0f, 0f, 0f, 0.05f, 1f, 1f, frame);
                    Box(0.95f, 0f, 0f, 1f, 1f, 1f, frame);
                    for (int row = 0; row < 4; row++)
                    {
                        float y = 0.02f + row * 0.25f;
                        Box(0.05f, y, 0f, 0.95f, y + 0.03f, 1f, frame);
                        for (int i = 0; i < 3; i++)
                            Box(0.1f + i * 0.28f, y + 0.03f, 0.1f, 0.33f + i * 0.28f, y + 0.15f, 0.9f, (i + row) % 2 == 0 ? B : C);
                    }
                    break;
                default: // bookcases
                {
                    int rows = look.variant == DecorLooks.ShelfBooks ? 4 : 5;
                    float right = look.variant == DecorLooks.ShelfLadder ? 0.82f : 1f;
                    Box(0f, 0f, 0.85f, right, 1f, 1f, materials.Get(look.a * 0.85f));
                    Box(0f, 0f, 0f, 0.05f, 1f, 1f, frame);
                    Box(right - 0.05f, 0f, 0f, right, 1f, 1f, frame);
                    Box(0f, 0.97f, 0f, right, 1f, 1f, frame);
                    for (int row = 0; row < rows; row++)
                    {
                        float y = row / (float)rows;
                        Box(0.05f, y, 0f, right - 0.05f, y + 0.025f, 1f, frame);
                        float x = 0.07f;
                        float shelfTop = y + 1f / rows - 0.03f;
                        while (x < right - 0.12f)
                        {
                            float w = 0.04f + 0.04f * (float)random.NextDouble();
                            float h = (shelfTop - y) * (0.6f + 0.35f * (float)random.NextDouble());
                            Box(x, y + 0.025f, 0.15f, x + w, y + 0.025f + h, 0.85f, random.Next(3) == 0 ? (random.Next(2) == 0 ? B : C) : RandomColor());
                            x += w + 0.005f;
                            if (random.Next(7) == 0)
                                x += 0.08f;
                        }
                    }
                    if (look.variant == DecorLooks.ShelfLadder)
                    {
                        Tilted(0.88f, 0.48f, 0.05f, 0.03f, 1f, 0.06f, -12f, Metal);
                        Tilted(0.97f, 0.48f, 0.05f, 0.03f, 1f, 0.06f, -12f, Metal);
                        for (int i = 0; i < 5; i++)
                            Box(0.86f + i * 0.02f, 0.15f + i * 0.18f, 0f, 0.98f + i * 0.02f, 0.17f + i * 0.18f, 0.1f, Metal);
                    }
                    break;
                }
            }
        }

        void Appliance()
        {
            switch (look.variant)
            {
                case DecorLooks.ApplianceCoffee:
                    Box(0f, 0f, 0f, 1f, 0.55f, 1f, materials.Get(look.c));
                    Box(0.1f, 0.55f, 0.15f, 0.9f, 0.95f, 0.95f, A);
                    Box(0.18f, 0.7f, 0.12f, 0.82f, 0.88f, 0.15f, B);
                    Box(0.42f, 0.6f, 0.1f, 0.58f, 0.64f, 0.3f, Metal);
                    if (!Prop(Food + "cup-coffee", 0.5f, 0.55f, 0.25f, 0.06f))
                        Cyl(0.5f, 0.55f, 0.25f, 0.18f, 0.6f, White);
                    break;
                case DecorLooks.ApplianceEspresso:
                case DecorLooks.ApplianceStation:
                    Box(0f, 0f, 0.05f, 1f, 0.68f, 1f, A);
                    Box(-0.02f, 0.68f, 0f, 1.02f, 0.72f, 1f, look.variant == DecorLooks.ApplianceStation ? C : B);
                    Box(0.25f, 0.72f, 0.4f, 0.65f, 1f, 0.95f, look.variant == DecorLooks.ApplianceEspresso ? Metal : B);
                    Box(0.3f, 0.8f, 0.37f, 0.6f, 0.83f, 0.4f, Dark);
                    for (int i = 0; i < 3; i++)
                        if (!Prop(Food + "cup-coffee", 0.75f + i * 0.08f, 0.72f, 0.3f, 0.07f))
                            Cyl(0.75f + i * 0.08f, 0.72f, 0.3f, 0.06f, 0.78f, White);
                    break;
                case DecorLooks.ApplianceRoaster:
                    Box(0.05f, 0f, 0.1f, 0.95f, 0.45f, 0.95f, B);
                    CylX(0.15f, 0.85f, 0.62f, 0.55f, 0.32f, Shiny);
                    Disc(0.2f, 0.62f, 0.15f, 0.25f, 0.05f, B);
                    Cone(0.7f, 0.78f, 0.55f, 0.25f, 1f, Shiny);
                    Cyl(0.3f, 0.45f, 0.2f, 0.25f, 0.5f, A);
                    break;
                case DecorLooks.ApplianceCooler:
                    Box(0.05f, 0f, 0.05f, 0.95f, 0.72f, 0.95f, A);
                    Box(0.2f, 0.45f, 0.02f, 0.8f, 0.5f, 0.05f, Metal);
                    Cyl(0.5f, 0.72f, 0.5f, 0.8f, 1f, materials.Get(look.b));
                    break;
                case DecorLooks.ApplianceClaw:
                    Box(0f, 0f, 0f, 1f, 0.45f, 1f, A);
                    Box(0.02f, 0.45f, 0.02f, 0.98f, 0.9f, 0.98f, materials.Get(look.b));
                    Box(0f, 0.9f, 0f, 1f, 1f, 1f, A);
                    Box(0.15f, 0.92f, -0.01f, 0.85f, 0.98f, 0f, Glow(look.c));
                    for (int i = 0; i < 5; i++)
                        Blob(0.2f + i * 0.15f, 0.5f, 0.3f + (i % 2) * 0.3f, 0.18f, 0.1f, 0.25f, RandomColor());
                    Cyl(0.5f, 0.72f, 0.5f, 0.03f, 0.9f, Metal);
                    Cone(0.5f, 0.66f, 0.5f, 0.15f, 0.72f, Metal);
                    break;
                case DecorLooks.ApplianceTwin:
                    Cabinet(0f, 0.48f);
                    Cabinet(0.52f, 1f);
                    break;
                case DecorLooks.ApplianceRacing:
                    Cabinet(0f, 0.55f);
                    Box(0.62f, 0f, 0.15f, 1f, 0.3f, 0.95f, Dark);
                    Box(0.62f, 0.3f, 0.2f, 0.95f, 0.38f, 0.9f, A);
                    Tilted(0.95f, 0.5f, 0.55f, 0.06f, 0.3f, 0.7f, 10f, A);
                    Disc(0.28f, 0.42f, -0.02f, 0.12f, 0.03f, Dark);
                    break;
                default: // arcade and retro cabinet
                    Cabinet(0f, 1f);
                    break;
            }
        }

        /// <summary>An arcade cabinet across x0..x1: body, glowing screen and marquee, control panel.</summary>
        void Cabinet(float x0, float x1)
        {
            float w = x1 - x0;
            Box(x0, 0f, 0.1f, x1, 1f, 1f, A);
            Box(x0 + w * 0.12f, 0.52f, 0.08f, x1 - w * 0.12f, 0.82f, 0.12f, Glow(look.b));
            Box(x0 + w * 0.05f, 0.88f, 0.08f, x1 - w * 0.05f, 0.96f, 0.12f, Glow(look.b * 0.8f + Color.white * 0.2f));
            Box(x0, 0.42f, -0.05f, x1, 0.48f, 0.3f, C);
            Cyl(x0 + w * 0.3f, 0.48f, 0.05f, 0.04f, 0.53f, Glow(Color.red));
            Cyl(x0 + w * 0.65f, 0.48f, 0.05f, 0.04f, 0.51f, Glow(Color.yellow));
        }

        void Machine()
        {
            switch (look.variant)
            {
                case DecorLooks.MachineBike:
                    Box(0.1f, 0f, 0.3f, 0.9f, 0.05f, 0.7f, A);
                    Disc(0.75f, 0.3f, 0.5f, 0.4f, 0.12f, B);
                    Tilted(0.45f, 0.35f, 0.5f, 0.06f, 0.6f, 0.15f, -30f, A);
                    Box(0.3f, 0.6f, 0.35f, 0.5f, 0.65f, 0.65f, Dark);
                    Tilted(0.72f, 0.65f, 0.5f, 0.05f, 0.6f, 0.12f, 20f, A);
                    Box(0.62f, 0.92f, 0.2f, 0.82f, 0.96f, 0.8f, Dark);
                    break;
                case DecorLooks.MachineRower:
                    Box(0f, 0.15f, 0.4f, 1f, 0.22f, 0.6f, Metal);
                    Box(0.02f, 0f, 0.3f, 0.06f, 0.15f, 0.7f, A);
                    Box(0.94f, 0f, 0.3f, 0.98f, 0.15f, 0.7f, A);
                    Disc(0.12f, 0.45f, 0.5f, 0.55f, 0.25f, A);
                    Box(0.45f, 0.22f, 0.3f, 0.65f, 0.32f, 0.7f, Dark);
                    Box(0.2f, 0.55f, 0.1f, 0.24f, 0.6f, 0.9f, C);
                    break;
                case DecorLooks.MachinePinball:
                    Legs(0.02f, 0.62f, 0.05f, Dark);
                    Tilted(0.42f, 0.68f, 0.5f, 0.84f, 0.06f, 1f, 6f, A);
                    Tilted(0.42f, 0.715f, 0.5f, 0.78f, 0.01f, 0.9f, 6f, Glow(look.b * 0.6f));
                    Box(0.86f, 0.62f, 0.05f, 1f, 1f, 0.95f, A);
                    Box(0.855f, 0.72f, 0.1f, 0.86f, 0.95f, 0.9f, Glow(look.b));
                    break;
                case DecorLooks.MachineHockey:
                    Legs(0.05f, 0.75f, 0.06f, Dark);
                    Box(0f, 0.75f, 0f, 1f, 0.88f, 1f, A);
                    Box(0.03f, 0.88f, 0.05f, 0.97f, 0.9f, 0.95f, B);
                    Cyl(0.25f, 0.9f, 0.5f, 0.08f, 1f, C);
                    Cyl(0.75f, 0.9f, 0.5f, 0.08f, 1f, C);
                    Cyl(0.5f, 0.9f, 0.4f, 0.05f, 0.92f, Dark);
                    break;
                case DecorLooks.MachineDance:
                    Box(0f, 0f, 0f, 1f, 0.08f, 1f, A);
                    for (int i = 0; i < 4; i++)
                        Box(0.1f + i * 0.22f, 0.08f, 0.3f, 0.28f + i * 0.22f, 0.09f, 0.7f, Glow(i % 2 == 0 ? look.b : look.c));
                    Box(0.2f, 0.08f, 0.85f, 0.8f, 1f, 1f, A);
                    Box(0.25f, 0.55f, 0.84f, 0.75f, 0.9f, 0.85f, Glow(look.c));
                    break;
                default: // treadmill
                    Box(0f, 0f, 0.1f, 1f, 0.12f, 0.9f, A);
                    Box(0.05f, 0.12f, 0.15f, 0.95f, 0.13f, 0.85f, Dark);
                    Box(0.82f, 0.12f, 0.12f, 0.86f, 0.9f, 0.18f, B);
                    Box(0.82f, 0.12f, 0.82f, 0.86f, 0.9f, 0.88f, B);
                    Tilted(0.86f, 0.92f, 0.5f, 0.12f, 0.1f, 0.8f, -20f, A);
                    Box(0.85f, 0.95f, 0.35f, 0.9f, 1f, 0.65f, Glow(look.c));
                    break;
            }
        }

        void Round()
        {
            switch (look.variant)
            {
                case DecorLooks.RoundBall:
                    Blob(0.5f, 0.5f, 0.5f, 1f, 1f, 1f, A);
                    Blob(0.5f, 0.5f, 0.5f, 1.01f, 0.12f, 1.01f, B);
                    break;
                case DecorLooks.RoundBag:
                    Box(0.05f, 0f, 0.2f, 0.95f, 0.04f, 0.8f, C);
                    Box(0.05f, 0.04f, 0.45f, 0.11f, 1f, 0.55f, B);
                    Box(0.05f, 0.95f, 0.45f, 0.6f, 1f, 0.55f, B);
                    Cyl(0.55f, 0.85f, 0.5f, 0.02f, 0.95f, Metal);
                    Cyl(0.55f, 0.3f, 0.5f, 0.42f, 0.85f, A);
                    break;
                default: // globes
                {
                    bool antique = look.variant == DecorLooks.RoundAntiqueGlobe;
                    Cyl(0.5f, 0f, 0.5f, 0.6f, 0.04f, C);
                    Cyl(0.5f, 0.04f, 0.5f, 0.06f, 0.5f, antique ? B : C);
                    Blob(0.5f, 0.72f, 0.5f, 0.75f, 0.45f, 0.9f, A);
                    for (int i = 0; i < 4; i++)
                        Blob(0.35f + (float)random.NextDouble() * 0.3f, 0.6f + (float)random.NextDouble() * 0.2f, 0.12f,
                            0.2f, 0.1f, 0.12f, antique ? materials.Get(look.b) : B);
                    if (antique)
                        Disc(0.5f, 0.72f, 0.5f, 0.5f, 0.02f, Shiny);
                    break;
                }
            }
        }

        void Rack()
        {
            switch (look.variant)
            {
                case DecorLooks.RackDumbbells:
                    foreach (float x in new[] { 0f, 0.96f })
                        Box(x, 0f, 0.2f, x + 0.04f, 1f, 0.8f, A);
                    for (int row = 0; row < 2; row++)
                    {
                        float y = 0.45f + row * 0.45f;
                        Tilted(0.5f, y, 0.5f, 0.96f, 0.05f, 0.6f, 0f, A);
                        for (int i = 0; i < 5; i++)
                        {
                            float x = 0.12f + i * 0.19f;
                            Disc(x, y + 0.1f, 0.3f, 0.18f, 0.05f, B);
                            Disc(x, y + 0.1f, 0.7f, 0.18f, 0.05f, B);
                        }
                    }
                    break;
                case DecorLooks.RackWeights:
                    Box(0.1f, 0f, 0.1f, 0.9f, 0.05f, 0.9f, A);
                    Box(0.47f, 0.05f, 0.45f, 0.53f, 1f, 0.55f, A);
                    for (int i = 0; i < 3; i++)
                    {
                        float y = 0.25f + i * 0.25f;
                        Disc(0.3f, y, 0.5f, 0.3f - i * 0.05f, 0.08f, i == 0 ? C : B);
                        Disc(0.7f, y, 0.5f, 0.3f - i * 0.05f, 0.08f, i == 0 ? C : B);
                    }
                    break;
                default: // power rack
                    foreach (float x in new[] { 0.05f, 0.9f })
                        foreach (float z in new[] { 0.05f, 0.85f })
                            Box(x, 0f, z, x + 0.05f, 1f, z + 0.1f, A);
                    Box(0.05f, 0.95f, 0.05f, 0.95f, 1f, 0.95f, A);
                    CylX(-0.05f, 1.05f, 0.55f, 0.1f, 0.025f, Metal);
                    Disc(0f, 0.55f, 0.1f, 0.22f, 0.06f, B);
                    Disc(1f, 0.55f, 0.1f, 0.22f, 0.06f, B);
                    Box(0.3f, 0f, 0.2f, 0.7f, 0.25f, 0.8f, Shiny);
                    break;
            }
        }

        void Tub()
        {
            switch (look.variant)
            {
                case DecorLooks.TubFountain:
                    Cyl(0.5f, 0f, 0.5f, 0.95f, 0.22f, A);
                    Cyl(0.5f, 0.2f, 0.5f, 0.85f, 0.21f, Glow(look.b * 0.85f));
                    Cyl(0.5f, 0.2f, 0.5f, 0.14f, 0.75f, C);
                    Cyl(0.5f, 0.72f, 0.5f, 0.55f, 0.78f, A);
                    Cyl(0.5f, 0.78f, 0.5f, 0.45f, 0.785f, Glow(look.b));
                    Cyl(0.5f, 0.78f, 0.5f, 0.06f, 1f, C);
                    Blob(0.5f, 0.95f, 0.5f, 0.15f, 0.08f, 0.2f, Glow(look.b));
                    break;
                case DecorLooks.TubWood:
                    Blob(0.5f, 0.45f, 0.5f, 1f, 0.9f, 1f, A);
                    Box(0f, 0f, 0f, 1f, 0.45f, 1f, A);
                    Box(-0.01f, 0.15f, -0.01f, 1.01f, 0.2f, 1.01f, C);
                    Box(-0.01f, 0.7f, -0.01f, 1.01f, 0.75f, 0.0f, C);
                    Box(0.05f, 0.82f, 0.05f, 0.95f, 0.84f, 0.95f, Glow(look.b));
                    Box(0f, 0.84f, 0f, 1f, 0.9f, 1f, A);
                    break;
                default: // hot tub and infinity tub
                    Box(0f, 0f, 0f, 1f, 0.9f, 1f, A);
                    Box(0.06f, 0.88f, 0.08f, 0.94f, 0.9f, 0.92f, Glow(look.b));
                    Box(0f, 0.9f, 0f, 1f, 1f, 0.08f, C);
                    Box(0f, 0.9f, 0.92f, 1f, 1f, 1f, C);
                    Box(0f, 0.9f, 0f, 0.06f, 1f, 1f, C);
                    Box(0.94f, 0.9f, 0f, 1f, 1f, 1f, C);
                    if (look.variant == DecorLooks.TubInfinity)
                    {
                        Box(-0.02f, 0.97f, -0.02f, 1.02f, 1f, 0.02f, Shiny);
                        Box(0.1f, 0.3f, -0.01f, 0.9f, 0.35f, 0f, Glow(look.b));
                    }
                    break;
            }
        }

        void Piano()
        {
            var body = A;
            if (look.variant == DecorLooks.PianoGrand)
            {
                Legs(0.08f, 0.6f, 0.06f, body);
                Box(0f, 0.6f, 0.05f, 1f, 0.75f, 1f, body);
                Tilted(0.55f, 0.95f, 0.65f, 0.9f, 0.02f, 0.6f, 25f, body);
                Box(0.1f, 0.63f, -0.08f, 0.9f, 0.68f, 0.05f, materials.Get(look.b));
                for (int i = 0; i < 14; i++)
                    Box(0.13f + i * 0.055f, 0.68f, -0.06f, 0.15f + i * 0.055f, 0.7f, 0f, Dark);
                Box(0.3f, 0f, -0.35f, 0.7f, 0.45f, -0.2f, body);
            }
            else
            {
                Box(0f, 0f, 0.4f, 1f, 1f, 1f, body);
                Box(0f, 0.55f, 0.1f, 1f, 0.62f, 0.4f, body);
                Box(0.05f, 0.62f, 0.12f, 0.95f, 0.66f, 0.38f, materials.Get(look.b));
                for (int i = 0; i < 16; i++)
                    Box(0.08f + i * 0.055f, 0.66f, 0.25f, 0.1f + i * 0.055f, 0.68f, 0.38f, Dark);
                Box(0.06f, 0f, 0.15f, 0.1f, 0.55f, 0.4f, body);
                Box(0.9f, 0f, 0.15f, 0.94f, 0.55f, 0.4f, body);
                Box(0.25f, 0f, -0.3f, 0.75f, 0.4f, -0.05f, C);
            }
        }

        void WallArt()
        {
            switch (look.variant)
            {
                case DecorLooks.ArtClock:
                    Disc(0.5f, 0.5f, 0.6f, 1f, 0.04f, B);
                    Disc(0.5f, 0.5f, 0.4f, 0.86f, 0.04f, A);
                    Box(0.48f, 0.5f, -0.1f, 0.52f, 0.85f, 0f, C);
                    Box(0.5f, 0.48f, -0.1f, 0.75f, 0.52f, 0f, C);
                    break;
                case DecorLooks.ArtMirror:
                    Box(0f, 0f, 0.5f, 1f, 1f, 1f, A);
                    Box(0.08f, 0.06f, 0.2f, 0.92f, 0.94f, 0.5f, materials.GetShiny(look.b));
                    break;
                case DecorLooks.ArtNeon:
                case DecorLooks.ArtJoystick:
                    Box(0f, 0f, 0.7f, 1f, 1f, 1f, materials.Get(look.b));
                    if (look.variant == DecorLooks.ArtNeon)
                    {
                        // A steaming cup.
                        Box(0.3f, 0.2f, 0.2f, 0.34f, 0.65f, 0.7f, Glow(look.a));
                        Box(0.62f, 0.2f, 0.2f, 0.66f, 0.65f, 0.7f, Glow(look.a));
                        Box(0.3f, 0.18f, 0.2f, 0.66f, 0.22f, 0.7f, Glow(look.a));
                        Box(0.66f, 0.4f, 0.2f, 0.78f, 0.44f, 0.7f, Glow(look.a));
                        Box(0.76f, 0.3f, 0.2f, 0.78f, 0.44f, 0.7f, Glow(look.a));
                        for (int i = 0; i < 3; i++)
                            Box(0.38f + i * 0.09f, 0.72f, 0.2f, 0.4f + i * 0.09f, 0.88f, 0.7f, Glow(look.c));
                    }
                    else
                    {
                        Box(0.25f, 0.15f, 0.2f, 0.75f, 0.19f, 0.7f, Glow(look.a));
                        Box(0.25f, 0.15f, 0.2f, 0.28f, 0.4f, 0.7f, Glow(look.a));
                        Box(0.72f, 0.15f, 0.2f, 0.75f, 0.4f, 0.7f, Glow(look.a));
                        Box(0.25f, 0.37f, 0.2f, 0.75f, 0.4f, 0.7f, Glow(look.a));
                        Box(0.48f, 0.4f, 0.2f, 0.52f, 0.72f, 0.7f, Glow(look.a));
                        Disc(0.5f, 0.78f, 0.3f, 0.2f, 0.02f, Glow(look.c));
                    }
                    break;
                case DecorLooks.ArtScore:
                    Box(0f, 0f, 0.5f, 1f, 1f, 1f, A);
                    for (int i = 0; i < 4; i++)
                        Box(0.1f, 0.15f + i * 0.2f, 0.3f, 0.3f + 0.5f * (float)random.NextDouble() + 0.1f, 0.25f + i * 0.2f, 0.5f, Glow(i == 3 ? look.c : look.b));
                    break;
                case DecorLooks.ArtTrophies:
                    Box(0f, 0f, 0f, 1f, 0.08f, 1f, A);
                    for (int i = 0; i < 4; i++)
                    {
                        float x = 0.15f + i * 0.23f;
                        float h = 0.45f + 0.15f * (i % 2);
                        Box(x - 0.05f, 0.08f, 0.35f, x + 0.05f, 0.18f, 0.65f, Dark);
                        Cyl(x, 0.18f, 0.5f, 0.02f, h - 0.1f, i == 2 ? C : Shiny);
                        Cone(x, h + 0.25f, 0.5f, 0.12f, h - 0.12f, i == 2 ? C : Shiny);
                    }
                    break;
                case DecorLooks.ArtChart:
                    Box(0f, 0f, 0.5f, 1f, 1f, 1f, materials.Get(look.c));
                    Box(0.05f, 0.07f, 0.3f, 0.95f, 0.93f, 0.5f, A);
                    for (int i = 0; i < 5; i++)
                        Box(0.12f + i * 0.16f, 0.12f, 0.2f, 0.22f + i * 0.16f, 0.2f + 0.12f * i + 0.1f * (float)random.NextDouble(), 0.3f, i == 4 ? B : RandomColor());
                    break;
                case DecorLooks.ArtSconces:
                    foreach (float x in new[] { 0.15f, 0.85f })
                    {
                        Box(x - 0.06f, 0.2f, 0.85f, x + 0.06f, 0.5f, 1f, A);
                        Box(x - 0.015f, 0.35f, 0.4f, x + 0.015f, 0.4f, 0.85f, A);
                        Cyl(x, 0.4f, 0.4f, 0.1f, 0.48f, A);
                        Blob(x, 0.6f, 0.4f, 0.12f, 0.3f, 0.6f, Glow(look.b));
                    }
                    break;
                case DecorLooks.ArtChandelier:
                    Cyl(0.5f, 0.7f, 0.5f, 0.02f, 1f, A);
                    Disc(0.5f, 0.55f, 0.5f, 0.35f, 0.03f, Shiny);
                    for (int i = 0; i < 6; i++)
                    {
                        float a = i * Mathf.PI / 3f;
                        float x = 0.5f + Mathf.Cos(a) * 0.38f;
                        float z = 0.5f + Mathf.Sin(a) * 0.38f;
                        Cyl(x, 0.45f, z, 0.05f, 0.6f, White);
                        Blob(x, 0.66f, z, 0.06f, 0.1f, 0.1f, Glow(look.b));
                    }
                    for (int i = 0; i < 8; i++)
                        Blob(0.3f + 0.4f * i / 7f, 0.38f - 0.1f * Mathf.Sin(i * 0.45f), 0.5f, 0.05f, 0.08f, 0.1f, Glow(look.c));
                    break;
                case DecorLooks.ArtMap:
                    Box(0f, 0f, 0.5f, 1f, 1f, 1f, A);
                    Box(0.05f, 0.07f, 0.3f, 0.95f, 0.93f, 0.5f, materials.Get(look.b));
                    for (int i = 0; i < 4; i++)
                        Blob(0.2f + 0.2f * i, 0.3f + 0.4f * (float)random.NextDouble(), 0.3f, 0.18f, 0.22f, 0.2f, C);
                    break;
                case DecorLooks.ArtBoard:
                    Box(0f, 0f, 0.5f, 1f, 1f, 1f, C);
                    Box(0.04f, 0.06f, 0.3f, 0.96f, 0.94f, 0.5f, A);
                    for (int i = 0; i < 4; i++)
                        Box(0.12f, 0.72f - i * 0.17f, 0.25f, 0.4f + 0.45f * (float)random.NextDouble(), 0.76f - i * 0.17f, 0.3f, B);
                    break;
                default: // frame and painting
                    Box(0f, 0f, 0.5f, 1f, 1f, 1f, A);
                    Box(0.07f, 0.45f, 0.3f, 0.93f, 0.9f, 0.5f, materials.Get(look.c));
                    Box(0.07f, 0.1f, 0.3f, 0.93f, 0.45f, 0.5f, B);
                    Disc(0.72f, 0.72f, 0.25f, 0.18f, 0.02f, materials.Get(new Color(1f, 0.92f, 0.6f)));
                    if (look.variant == DecorLooks.ArtPainting)
                        Blob(0.35f, 0.42f, 0.25f, 0.5f, 0.35f, 0.2f, materials.Get(look.b * 0.8f));
                    break;
            }
        }
    }
}
