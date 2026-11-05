using System.Collections.Generic;
using UnityEngine;

namespace ElevatorRush
{
    /// <summary>
    /// Loads models, audio and fonts from Resources by key. Every key has a primitive fallback,
    /// so a missing asset never stops the game.
    /// </summary>
    public sealed class AssetLibrary
    {
        const string ModelFolder = "Models/";
        const string AudioFolder = "Audio/";
        const string FontKey = "Fonts/font_ui";
        const string BuiltinFont = "LegacyRuntime.ttf";

        readonly Dictionary<PrimitiveType, Mesh> primitiveMeshes = new Dictionary<PrimitiveType, Mesh>();
        readonly Dictionary<string, GameObject> models = new Dictionary<string, GameObject>();
        readonly Dictionary<int, Mesh> cones = new Dictionary<int, Mesh>();
        Mesh lowSphere;
        readonly Dictionary<string, AudioClip> sounds = new Dictionary<string, AudioClip>();
        readonly Dictionary<string, Color> modelColors;
        readonly MaterialLibrary materials;
        Font font;

        /// <summary>True when the downloaded UI font is in use (the built-in one needs synthetic bold).</summary>
        public bool HasCustomFontLoaded()
        {
            return UiFont != null && hasCustomFont;
        }

        bool hasCustomFont;

        public AssetLibrary(MaterialLibrary materials, GameConfig config)
        {
            this.materials = materials;
            modelColors = new Dictionary<string, Color>();
            for (int i = 0; i < config.modelMaterialNames.Length; i++)
                modelColors[config.modelMaterialNames[i]] = config.modelMaterialColors[i];
        }

        /// <summary>Model prefab for a key, or null when it is not on disk (caller uses its fallback).</summary>
        public GameObject Model(string key)
        {
            if (!models.TryGetValue(key, out var prefab))
            {
                prefab = Resources.Load<GameObject>(ModelFolder + key);
                models.Add(key, prefab);
            }
            return prefab;
        }

        public Font UiFont
        {
            get
            {
                if (font == null)
                {
                    font = Resources.Load<Font>(FontKey);
                    hasCustomFont = font != null;
                    if (font == null)
                        font = Resources.GetBuiltinResource<Font>(BuiltinFont);
                }
                return font;
            }
        }

        /// <summary>Audio clip for a key, or null when it is not on disk (Sfx generates a tone instead).</summary>
        public AudioClip Sound(string key)
        {
            if (!sounds.TryGetValue(key, out var clip))
            {
                clip = Resources.Load<AudioClip>(AudioFolder + key);
                sounds.Add(key, clip);
            }
            return clip;
        }

        /// <summary>
        /// Instantiates a model under a new holder whose origin is the model's bottom center,
        /// scaled so the model is <paramref name="height"/> tall. Untextured imported materials are
        /// swapped for shared palette materials; textured ones (color atlases) are kept.
        /// </summary>
        public GameObject SpawnModel(GameObject prefab, string name, Transform parent, float height)
        {
            return SpawnModel(prefab, name, parent, height, float.PositiveInfinity, out _);
        }

        /// <summary>As above, but also no wider (along X) than <paramref name="maxWidth"/>; reports the final size.</summary>
        public GameObject SpawnModel(GameObject prefab, string name, Transform parent, float height, float maxWidth, out Vector3 size)
        {
            return SpawnModel(prefab, name, parent, height, maxWidth, null, out size);
        }

        /// <summary>
        /// As above; untextured materials whose names are in <paramref name="recolor"/> take that
        /// color instead of the shared palette (e.g. a sofa's fabric in the decor option's color).
        /// </summary>
        public GameObject SpawnModel(GameObject prefab, string name, Transform parent, float height, float maxWidth,
            IReadOnlyDictionary<string, Color> recolor, out Vector3 size)
        {
            size = Vector3.zero;
            var holder = new GameObject(name).transform;
            var instance = Object.Instantiate(prefab, holder, false);
            instance.name = prefab.name;

            var renderers = instance.GetComponentsInChildren<Renderer>(true);
            bool any = false;
            var bounds = new Bounds();
            foreach (var r in renderers)
            {
                if (!any) { bounds = r.bounds; any = true; }
                else bounds.Encapsulate(r.bounds);
                RemapMaterials(r, recolor);
            }

            if (any && bounds.size.y > 0f)
            {
                float scale = height / bounds.size.y;
                if (bounds.size.x * scale > maxWidth)
                    scale = maxWidth / bounds.size.x;
                size = bounds.size * scale;
                var bottomCenter = new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
                var t = instance.transform;
                t.localScale *= scale;
                t.localPosition = (t.localPosition - bottomCenter) * scale;
            }

            holder.SetParent(parent, false);
            return holder.gameObject;
        }

        void RemapMaterials(Renderer renderer, IReadOnlyDictionary<string, Color> recolor)
        {
            var shared = renderer.sharedMaterials;
            for (int i = 0; i < shared.Length; i++)
            {
                var m = shared[i];
                if (m == null || (m.HasProperty(MaterialLibrary.BaseMapProperty) && m.GetTexture(MaterialLibrary.BaseMapProperty) != null))
                    continue;
                if (recolor != null && recolor.TryGetValue(m.name, out var custom))
                    shared[i] = materials.Get(custom);
                else
                    shared[i] = materials.Get(modelColors.TryGetValue(m.name, out var c) ? c : m.color);
            }
            renderer.sharedMaterials = shared;
        }

        public Mesh PrimitiveMesh(PrimitiveType type)
        {
            if (primitiveMeshes.TryGetValue(type, out var mesh))
                return mesh;

            // CreatePrimitive adds a collider; take only the shared mesh and discard the object.
            var temp = GameObject.CreatePrimitive(type);
            temp.SetActive(false);
            mesh = temp.GetComponent<MeshFilter>().sharedMesh;
            Object.Destroy(temp);
            primitiveMeshes.Add(type, mesh);
            return mesh;
        }

        /// <summary>A mesh-only primitive (no collider) under a parent.</summary>
        public GameObject CreatePrimitive(PrimitiveType type, string name, Transform parent,
            Vector3 localPosition, Vector3 localScale, Material material)
        {
            return CreateMesh(PrimitiveMesh(type), name, parent, localPosition, localScale, material);
        }

        public GameObject CreateMesh(Mesh mesh, string name, Transform parent,
            Vector3 localPosition, Vector3 localScale, Material material)
        {
            var go = new GameObject(name);
            var t = go.transform;
            t.SetParent(parent, false);
            t.localPosition = localPosition;
            t.localScale = localScale;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = material;
            return go;
        }

        /// <summary>
        /// Flat-shaded low-poly cone, base radius 0.5 at y = 0 and apex at y = 1. Unity has no cone
        /// primitive; this is the fallback for keys like "crown".
        /// </summary>
        public Mesh ConeMesh(int sides)
        {
            if (cones.TryGetValue(sides, out var mesh))
                return mesh;

            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            var apex = Vector3.up;
            for (int i = 0; i < sides; i++)
            {
                float a0 = i * Mathf.PI * 2f / sides;
                float a1 = (i + 1) * Mathf.PI * 2f / sides;
                var b0 = new Vector3(Mathf.Cos(a0), 0f, Mathf.Sin(a0)) * 0.5f;
                var b1 = new Vector3(Mathf.Cos(a1), 0f, Mathf.Sin(a1)) * 0.5f;
                var outward = (b0 + b1) * 0.5f;
                AddTriangle(vertices, triangles, apex, b0, b1, outward);
                AddTriangle(vertices, triangles, Vector3.zero, b1, b0, Vector3.down);
            }

            mesh = new Mesh { name = "Cone" + sides };
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            cones.Add(sides, mesh);
            return mesh;
        }

        /// <summary>Adds a triangle with its own vertices (flat shading), wound to face <paramref name="outward"/>.</summary>
        static void AddTriangle(List<Vector3> vertices, List<int> triangles, Vector3 a, Vector3 b, Vector3 c, Vector3 outward)
        {
            if (Vector3.Dot(Vector3.Cross(b - a, c - a), outward) < 0f)
            {
                var swap = b;
                b = c;
                c = swap;
            }
            int start = vertices.Count;
            vertices.Add(a);
            vertices.Add(b);
            vertices.Add(c);
            triangles.Add(start);
            triangles.Add(start + 1);
            triangles.Add(start + 2);
        }

        // Low-poly sphere resolution: about 160 triangles instead of the built-in sphere's 768.
        const int SphereRings = 7;
        const int SphereSegments = 12;

        /// <summary>
        /// A flat-shaded low-poly sphere of diameter 1, centered on the origin. Used for decor,
        /// skins and ornaments, where many small spheres would otherwise blow the triangle budget.
        /// </summary>
        public Mesh LowSphereMesh()
        {
            if (lowSphere != null)
                return lowSphere;
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            for (int r = 0; r < SphereRings; r++)
            {
                float a0 = Mathf.PI * r / SphereRings;
                float a1 = Mathf.PI * (r + 1) / SphereRings;
                for (int s = 0; s < SphereSegments; s++)
                {
                    float b0 = Mathf.PI * 2f * s / SphereSegments;
                    float b1 = Mathf.PI * 2f * (s + 1) / SphereSegments;
                    var p00 = Point(a0, b0);
                    var p01 = Point(a0, b1);
                    var p10 = Point(a1, b0);
                    var p11 = Point(a1, b1);
                    var outward = (p00 + p01 + p10 + p11) * 0.25f;
                    if (r > 0)
                        AddTriangle(vertices, triangles, p00, p10, p01, outward);
                    if (r < SphereRings - 1)
                        AddTriangle(vertices, triangles, p01, p10, p11, outward);
                }
            }
            lowSphere = new Mesh { name = "LowSphere" };
            lowSphere.SetVertices(vertices);
            lowSphere.SetTriangles(triangles, 0);
            lowSphere.RecalculateNormals();
            lowSphere.RecalculateBounds();
            return lowSphere;
        }

        static Vector3 Point(float polar, float azimuth)
        {
            return new Vector3(Mathf.Sin(polar) * Mathf.Cos(azimuth), Mathf.Cos(polar), Mathf.Sin(polar) * Mathf.Sin(azimuth)) * 0.5f;
        }

        /// <summary>A low-poly sphere (see <see cref="LowSphereMesh"/>) under a parent.</summary>
        public GameObject CreateLowSphere(string name, Transform parent, Vector3 localPosition, Vector3 localScale, Material material)
        {
            return CreateMesh(LowSphereMesh(), name, parent, localPosition, localScale, material);
        }

        /// <summary>An axis-aligned cube spanning two local corners.</summary>
        public GameObject CreateBox(string name, Transform parent, Vector3 min, Vector3 max, Material material)
        {
            return CreatePrimitive(PrimitiveType.Cube, name, parent, (min + max) * 0.5f, max - min, material);
        }
    }
}
