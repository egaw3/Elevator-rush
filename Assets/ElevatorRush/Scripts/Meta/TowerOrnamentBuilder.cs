using UnityEngine;

namespace ElevatorRush
{
    /// <summary>
    /// The roof ornament of a tower skin (home plan 8), standing on the machine room above the
    /// shaft: the skin's model when it exists, otherwise a procedural one. Returns its height.
    /// </summary>
    public sealed class TowerOrnamentBuilder
    {
        const int Sides = 12;

        readonly MaterialLibrary materials;
        readonly AssetLibrary assets;
        Transform root;
        float w;

        public TowerOrnamentBuilder(MaterialLibrary materials, AssetLibrary assets)
        {
            this.materials = materials;
            this.assets = assets;
        }

        /// <summary>Builds the ornament centered at <paramref name="basePosition"/>, about <paramref name="width"/> wide.</summary>
        public float Build(Transform parent, TowerSkin skin, Vector3 basePosition, float width)
        {
            root = new GameObject("RoofOrnament").transform;
            root.SetParent(parent, false);
            root.localPosition = basePosition;
            w = width;

            var prefab = skin.ornamentModel != null ? assets.Model(skin.ornamentModel) : null;
            if (prefab != null)
            {
                assets.SpawnModel(prefab, skin.ornamentModel, root, width, width, out var size);
                return size.y;
            }

            var main = Mat(skin.ornamentMain, skin.finish == TowerFinish.Metallic);
            var accent = Mat(skin.ornamentAccent, skin.finish == TowerFinish.Metallic);
            switch (skin.ornament)
            {
                case RoofOrnament.WaterTank:
                    foreach (float x in new[] { -0.3f, 0.3f })
                        foreach (float z in new[] { -0.2f, 0.2f })
                            Box(x - 0.03f, 0f, z - 0.03f, x + 0.03f, 0.35f, z + 0.03f, accent);
                    Cyl(0f, 0.35f, 0f, 0.75f, 0.45f, main);
                    Cone(0f, 0.8f, 0f, 0.8f, 0.25f, accent);
                    return 1.05f * w;
                case RoofOrnament.Chimneys:
                    Box(-0.42f, 0f, -0.15f, -0.18f, 0.6f, 0.15f, main);
                    Box(-0.46f, 0.6f, -0.19f, -0.14f, 0.66f, 0.19f, accent);
                    Box(0.18f, 0f, -0.12f, 0.38f, 0.45f, 0.12f, main);
                    Box(0.14f, 0.45f, -0.16f, 0.42f, 0.51f, 0.16f, accent);
                    return 0.66f * w;
                case RoofOrnament.Spire:
                    Cyl(0f, 0f, 0f, 0.3f, 0.15f, main);
                    Cyl(0f, 0.15f, 0f, 0.06f, 1.2f, main);
                    Ball(0f, 1.38f, 0f, 0.1f, materials.GetUnlit(skin.ornamentAccent));
                    return 1.43f * w;
                case RoofOrnament.Lollipops:
                    for (int i = 0; i < 3; i++)
                    {
                        float x = -0.35f + i * 0.35f;
                        float h = 0.55f + 0.2f * (i % 2);
                        Cyl(x, 0f, 0f, 0.04f, h, accent);
                        Disc(x, h + 0.12f, 0f, 0.32f, main);
                        Disc(x, h + 0.12f, -0.02f, 0.16f, accent);
                    }
                    return 1.0f * w;
                case RoofOrnament.Pagoda:
                    for (int i = 0; i < 3; i++)
                    {
                        float y = i * 0.28f;
                        float s = 1.1f - i * 0.28f;
                        Box(-s * 0.32f, y, -s * 0.25f, s * 0.32f, y + 0.18f, s * 0.25f, Mat(Color.Lerp(skin.ornamentMain, Color.white, 0.75f), false));
                        Cone(0f, y + 0.16f, 0f, s, 0.14f, main);
                    }
                    Cyl(0f, 0.95f, 0f, 0.04f, 0.3f, accent);
                    Ball(0f, 1.28f, 0f, 0.09f, accent);
                    return 1.33f * w;
                case RoofOrnament.Turrets:
                    foreach (float x in new[] { -0.32f, 0.32f })
                    {
                        Cyl(x, 0f, 0f, 0.32f, 0.6f, main);
                        Cone(x, 0.6f, 0f, 0.4f, 0.45f, accent);
                    }
                    return 1.05f * w;
                case RoofOrnament.Dish:
                    Cyl(0f, 0f, 0f, 0.08f, 0.45f, accent);
                    var dish = Ball(0f, 0.6f, 0f, 0.8f, main);
                    dish.transform.localScale = new Vector3(0.8f * w, 0.22f * w, 0.8f * w);
                    dish.transform.localRotation = Quaternion.Euler(-30f, 0f, 20f);
                    Cyl(0f, 0.6f, 0f, 0.03f, 0.5f, accent);
                    Ball(0f, 1.1f, 0f, 0.07f, materials.GetUnlit(new Color(1f, 0.35f, 0.35f)));
                    return 1.15f * w;
                default: // golden dome with a spire
                    Cyl(0f, 0f, 0f, 0.8f, 0.2f, accent);
                    Ball(0f, 0.2f, 0f, 0.75f, main);
                    Cyl(0f, 0.5f, 0f, 0.05f, 0.45f, main);
                    Ball(0f, 0.97f, 0f, 0.1f, main);
                    return 1.02f * w;
            }
        }

        Material Mat(Color color, bool shiny) => shiny ? materials.GetShiny(color) : materials.Get(color);

        Vector3 P(float x, float y, float z) => new Vector3(x, y, z) * w;

        void Box(float x0, float y0, float z0, float x1, float y1, float z1, Material m)
        {
            assets.CreateBox("Part", root, P(x0, y0, z0), P(x1, y1, z1), m);
        }

        void Cyl(float x, float y0, float z, float diameter, float height, Material m)
        {
            assets.CreatePrimitive(PrimitiveType.Cylinder, "Part", root, P(x, y0 + height * 0.5f, z),
                new Vector3(diameter, height * 0.5f, diameter) * w, m);
        }

        GameObject Ball(float x, float y, float z, float diameter, Material m)
        {
            return assets.CreateLowSphere("Part", root, P(x, y, z), Vector3.one * diameter * w, m);
        }

        void Cone(float x, float y0, float z, float diameter, float height, Material m)
        {
            assets.CreateMesh(assets.ConeMesh(Sides), "Part", root, P(x, y0, z), new Vector3(diameter, height, diameter) * w, m);
        }

        /// <summary>A disc facing the camera.</summary>
        void Disc(float x, float y, float z, float diameter, Material m)
        {
            var go = assets.CreatePrimitive(PrimitiveType.Cylinder, "Part", root, P(x, y, z), new Vector3(diameter, 0.02f, diameter) * w, m);
            go.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        }
    }
}
