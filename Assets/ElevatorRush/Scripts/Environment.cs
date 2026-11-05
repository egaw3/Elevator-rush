using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace ElevatorRush
{
    /// <summary>
    /// The world around the building for a theme: gradient sky, ground, sidewalk and road, street
    /// props, and the sun and ambient light. Street props in front of the building stay low enough
    /// that they never cover the rooms from the camera's point of view.
    /// </summary>
    public sealed class Environment : MonoBehaviour
    {
        enum Anchor
        {
            Left,
            Center,
            Right
        }

        struct StreetProp
        {
            public string key;
            public Anchor anchor;
            public Vector3 offset;
            public float height;
            public float yaw;

            public StreetProp(string key, Anchor anchor, Vector3 offset, float height, float yaw)
            {
                this.key = key;
                this.anchor = anchor;
                this.offset = offset;
                this.height = height;
                this.yaw = yaw;
            }
        }

        const string TreeKey = "tree";
        const string LampKey = "lamp/lamp";
        const string StreetFolder = "props_street/";
        const string CityFolder = "city/";

        // Kenney City Kit (Commercial) buildings: low-detail ones near, skyscrapers only in the far row.
        static readonly string[] CityBuildings =
        {
            "low-detail-building-a", "low-detail-building-b", "low-detail-building-c", "low-detail-building-d",
            "low-detail-building-e", "low-detail-building-f", "low-detail-building-g", "low-detail-building-h",
            "low-detail-building-i", "low-detail-building-j", "low-detail-building-k", "low-detail-building-l",
            "low-detail-building-m", "low-detail-building-n", "low-detail-building-wide-a", "low-detail-building-wide-b",
        };
        static readonly string[] CityTowers =
        {
            "building-skyscraper-a", "building-skyscraper-b", "building-skyscraper-c", "building-skyscraper-d", "building-skyscraper-e",
        };

        // Offsets are from the anchor point at the building's front edge (z = 0 is the front).
        // In front of the building a prop must be shorter than about 0.18 x its distance, or it
        // would cover the ground floor. The middle of the far side of the street stays clear
        // because the Ready and tutorial text sits over it.
        static readonly StreetProp[] StreetProps =
        {
            new StreetProp(TreeKey, Anchor.Left, new Vector3(-1.3f, 0f, 3.2f), 3.2f, 0f),
            new StreetProp(TreeKey, Anchor.Right, new Vector3(1.1f, 0f, 4.0f), 3.6f, 40f),
            new StreetProp(TreeKey, Anchor.Left, new Vector3(-0.2f, 0f, -8.4f), 1.5f, 10f),
            new StreetProp(TreeKey, Anchor.Right, new Vector3(0.4f, 0f, -9.0f), 1.6f, 70f),
            new StreetProp("bush", Anchor.Left, new Vector3(-0.5f, 0f, -0.6f), 0.4f, 0f),
            new StreetProp("bush", Anchor.Right, new Vector3(0.5f, 0f, -0.8f), 0.45f, 30f),
            new StreetProp(LampKey, Anchor.Left, new Vector3(0.6f, 0f, -6.8f), 1.2f, 180f),
            new StreetProp(LampKey, Anchor.Right, new Vector3(-0.6f, 0f, -6.8f), 1.2f, 180f),
            new StreetProp("bench", Anchor.Right, new Vector3(-1.3f, 0f, -7.4f), 0.5f, 0f),
        };

        readonly Dictionary<string, Material> skyMaterials = new Dictionary<string, Material>();
        readonly Dictionary<(Material, string, int), Material> hazed = new Dictionary<(Material, string, int), Material>();

        GameConfig config;
        MaterialLibrary materials;
        AssetLibrary assets;
        Light sun;
        Camera cam;
        Transform world;
        Transform sky;
        Renderer skyRenderer;
        Theme current;

        public void Init(GameConfig config, MaterialLibrary materials, AssetLibrary assets, Light sun, Camera camera)
        {
            this.config = config;
            this.materials = materials;
            this.assets = assets;
            this.sun = sun;
            cam = camera;

            var skyGo = assets.CreatePrimitive(PrimitiveType.Quad, "Sky", cam.transform, Vector3.zero, Vector3.one, null);
            sky = skyGo.transform;
            skyRenderer = skyGo.GetComponent<Renderer>();
            skyRenderer.shadowCastingMode = ShadowCastingMode.Off;
            skyRenderer.receiveShadows = false;

            CreatePostProcessing();
        }

        /// <summary>A global URP volume made in code: mild color grading and a vignette, bloom off.</summary>
        void CreatePostProcessing()
        {
            var cameraData = cam.GetUniversalAdditionalCameraData();
            cameraData.renderPostProcessing = true;

            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            var color = profile.Add<ColorAdjustments>(true);
            color.saturation.Override(config.postSaturation);
            color.contrast.Override(config.postContrast);
            var vignette = profile.Add<Vignette>(true);
            vignette.intensity.Override(config.vignetteIntensity);
            vignette.smoothness.Override(config.vignetteSmoothness);
            var bloom = profile.Add<Bloom>(true);
            bloom.intensity.Override(0f);

            var go = new GameObject("PostProcessing");
            go.transform.SetParent(transform, false);
            var volume = go.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.sharedProfile = profile;
        }

        /// <summary>Builds the street and lighting for a theme around a building of the given width.</summary>
        public void Apply(Theme theme, float buildingLeft, float buildingRight, float buildingFront, float buildingBack)
        {
            if (theme == current)
                return;
            current = theme;

            if (world != null)
                Destroy(world.gameObject);
            world = new GameObject("World").transform;
            world.SetParent(transform, false);

            ApplyLighting(theme);
            skyRenderer.sharedMaterial = SkyMaterial(theme);
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = theme.skyHorizon;

            BuildGround(theme, buildingFront);
            BuildProps(theme, buildingLeft, buildingRight, buildingFront);
            BuildSkyline(theme, (buildingLeft + buildingRight) * 0.5f, buildingBack);
        }

        /// <summary>
        /// A busy city behind the tower: rows of buildings that grow taller with distance, the
        /// block straight behind the tower kept lower so the tower reads against the sky. The
        /// same layout every time. No shadows, so the backdrop costs no shadow-map work.
        /// </summary>
        void BuildSkyline(Theme theme, float center, float back)
        {
            var random = new System.Random(config.cityFallbackSeed);
            for (int row = 0; row < config.cityRows; row++)
            {
                float z = back + config.cityFirstRowZ + row * config.cityRowStep;
                bool farRow = row == config.cityRows - 1;
                float x = center - config.cityHalfWidth + (float)random.NextDouble() * 3f;
                while (x < center + config.cityHalfWidth)
                {
                    bool tower = farRow && random.Next(3) == 0;
                    string key = tower ? CityTowers[random.Next(CityTowers.Length)] : CityBuildings[random.Next(CityBuildings.Length)];
                    float height = Mathf.Lerp(config.cityHeight.x, config.cityHeight.y, (float)random.NextDouble()) + row * config.cityRowHeightGain;
                    if (tower)
                        height *= 1.6f;
                    float width = CityBlock(theme, key, height, x, z, center, row, out float placedWidth);
                    x += placedWidth + config.cityGap * (0.5f + (float)random.NextDouble());
                    if (width <= 0f)
                        x += 4f;
                }
            }
        }

        /// <summary>Places one city building with its left edge at x; returns its width.</summary>
        float CityBlock(Theme theme, string key, float height, float x, float z, float center, int row, out float placedWidth)
        {
            var prefab = assets.Model(CityFolder + key);
            GameObject block;
            Vector3 size;
            if (prefab != null)
            {
                block = assets.SpawnModel(prefab, key, world, height, float.PositiveInfinity, out size);
                // Lower the blocks straight behind the tower, so the tower reads against the sky.
                if (Mathf.Abs(x + size.x * 0.5f - center) < config.cityClearHalfWidth + row * 2f)
                {
                    block.transform.localScale *= config.cityClearHeightFactor;
                    size *= config.cityClearHeightFactor;
                }
                block.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            }
            else
            {
                // Fallback: a plain block with a lighter roof.
                size = new Vector3(4f, height, 4f);
                block = new GameObject(key + " (fallback)");
                block.transform.SetParent(world, false);
                assets.CreateBox("Block", block.transform, new Vector3(-size.x * 0.5f, 0f, -size.z * 0.5f), new Vector3(size.x * 0.5f, size.y, size.z * 0.5f),
                    materials.Get(config.cityFallbackColor));
            }
            block.transform.localPosition = new Vector3(x + size.x * 0.5f, config.groundTopY, z + size.z * 0.5f);
            foreach (var r in block.GetComponentsInChildren<Renderer>())
            {
                r.shadowCastingMode = ShadowCastingMode.Off;
                r.receiveShadows = false;
                var shared = r.sharedMaterials;
                for (int m = 0; m < shared.Length; m++)
                    shared[m] = Hazed(shared[m], theme, row);
                r.sharedMaterials = shared;
            }
            placedWidth = size.x;
            return size.x;
        }

        /// <summary>A copy of a city material tinted toward the theme's horizon color for a row (cached).</summary>
        Material Hazed(Material source, Theme theme, int row)
        {
            if (source == null)
                return null;
            var key = (source, theme.name, row);
            if (hazed.TryGetValue(key, out var material))
                return material;
            material = new Material(source) { name = source.name + "_Haze" + row };
            float haze = Mathf.Clamp01(config.cityHazeNear + row * config.cityHazeStep);
            var tint = Color.Lerp(Color.white, theme.skyHorizon, haze);
            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", material.GetColor("_BaseColor") * tint);
            hazed.Add(key, material);
            return material;
        }

        void ApplyLighting(Theme theme)
        {
            sun.transform.rotation = Quaternion.Euler(theme.sunEuler);
            sun.color = theme.sunColor;
            sun.intensity = theme.sunIntensity;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = theme.ambient;
        }

        void BuildGround(Theme theme, float front)
        {
            var size = config.groundSize;
            float top = config.groundTopY;
            assets.CreateBox("Ground", world,
                new Vector3(-size.x * 0.5f, top - config.groundThickness, -size.y * 0.5f),
                new Vector3(size.x * 0.5f, top, size.y * 0.5f),
                materials.Get(theme.ground));

            float half = config.roadLength * 0.5f;
            float layer = top + config.streetLayerHeight;
            float sidewalkFront = front - config.sidewalkDepth;
            float roadFront = sidewalkFront - config.roadDepth;
            float farSidewalk = roadFront - config.sidewalkDepth;

            assets.CreateBox("Sidewalk", world, new Vector3(-half, top, sidewalkFront), new Vector3(half, layer, front), materials.Get(theme.sidewalk));
            assets.CreateBox("Road", world, new Vector3(-half, top, roadFront), new Vector3(half, layer, sidewalkFront), materials.Get(theme.road));
            assets.CreateBox("FarSidewalk", world, new Vector3(-half, top, farSidewalk), new Vector3(half, layer, roadFront), materials.Get(theme.sidewalk));

            // Dashed center line.
            var lineMat = materials.Get(theme.roadLine);
            float lineZ = (roadFront + sidewalkFront) * 0.5f;
            float step = config.roadLineLength + config.roadLineGap;
            float w = config.roadLineWidth * 0.5f;
            for (float x = -half; x < half; x += step)
                assets.CreateBox("Line", world, new Vector3(x, layer, lineZ - w), new Vector3(x + config.roadLineLength, layer + config.streetLayerHeight, lineZ + w), lineMat);
        }

        void BuildProps(Theme theme, float left, float right, float front)
        {
            float center = (left + right) * 0.5f;
            for (int i = 0; i < StreetProps.Length; i++)
            {
                var p = StreetProps[i];
                string key = p.key == TreeKey ? theme.tree : p.key;
                float x = p.anchor == Anchor.Left ? left : p.anchor == Anchor.Right ? right : center;
                var position = new Vector3(x, 0f, front) + p.offset;
                position.y = config.groundTopY;

                var prefab = assets.Model(StreetFolder + key);
                GameObject prop = prefab != null
                    ? assets.SpawnModel(prefab, key, world, p.height)
                    : Fallback(key, p.height, theme);
                prop.transform.localPosition = position;
                prop.transform.localRotation = Quaternion.Euler(0f, p.yaw, 0f);

                if (theme.lampsLit && p.key == LampKey)
                    AddBulb(prop.transform, p.height);
            }
        }

        /// <summary>Primitive stand-ins when a street model is missing: cubes and spheres.</summary>
        GameObject Fallback(string key, float height, Theme theme)
        {
            var root = new GameObject(key + " (fallback)");
            root.transform.SetParent(world, false);
            var t = root.transform;
            if (key.StartsWith("tree"))
            {
                assets.CreatePrimitive(PrimitiveType.Cube, "Trunk", t, new Vector3(0f, height * 0.25f, 0f), new Vector3(height * 0.1f, height * 0.5f, height * 0.1f), materials.Get(config.modelMaterialColors[2]));
                assets.CreateLowSphere("Crown", t, new Vector3(0f, height * 0.68f, 0f), Vector3.one * height * 0.6f, materials.Get(config.modelMaterialColors[4]));
            }
            else if (key == "bush")
                assets.CreateLowSphere("Bush", t, new Vector3(0f, height * 0.5f, 0f), new Vector3(height * 1.6f, height, height * 1.6f), materials.Get(config.modelMaterialColors[7]));
            else
                assets.CreatePrimitive(PrimitiveType.Cube, key, t, new Vector3(0f, height * 0.5f, 0f), new Vector3(height, height, height * 2f), materials.Get(theme.road));
            return root;
        }

        void AddBulb(Transform lamp, float height)
        {
            var bulb = assets.CreateLowSphere("Bulb", lamp, new Vector3(0f, height, 0f),
                Vector3.one * config.lampBulbSize, materials.GetUnlit(config.lampBulbColor));
            bulb.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
        }

        Material SkyMaterial(Theme theme)
        {
            if (skyMaterials.TryGetValue(theme.name, out var material))
                return material;

            int h = Mathf.Max(2, config.skyTextureHeight);
            var texture = new Texture2D(1, h, TextureFormat.RGBA32, false)
            {
                name = "Sky_" + theme.name,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
            };
            for (int y = 0; y < h; y++)
                texture.SetPixel(0, y, Color.Lerp(theme.skyHorizon, theme.skyTop, (float)y / (h - 1)));
            texture.Apply(false, true);
            material = materials.GetCutout(texture, Color.white);
            skyMaterials.Add(theme.name, material);
            return material;
        }

        /// <summary>Keeps the sky quad filling the view near the far plane for any aspect ratio.</summary>
        void LateUpdate()
        {
            float d = cam.farClipPlane * config.skyDistanceFraction;
            float height = 2f * d * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad) * config.skyOverscan;
            sky.localPosition = new Vector3(0f, 0f, d);
            sky.localRotation = Quaternion.identity;
            sky.localScale = new Vector3(height * cam.aspect, height, 1f);
        }
    }
}
