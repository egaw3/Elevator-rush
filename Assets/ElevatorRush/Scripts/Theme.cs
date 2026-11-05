using UnityEngine;

namespace ElevatorRush
{
    /// <summary>
    /// A visual theme (plan 5.7): sky, ground and street colors, light, and street props. The
    /// building's own look comes from the equipped <see cref="TowerSkin"/> (home plan 8).
    /// Themes never change the destination palette.
    /// </summary>
    public sealed class Theme
    {
        public string name;
        public Color skyTop;
        public Color skyHorizon;
        public Color ground;
        public Color sidewalk;
        public Color road;
        public Color roadLine;
        public Color sunColor;
        public float sunIntensity;
        public Vector3 sunEuler;
        public Color ambient;
        /// <summary>Street tree model key under props_street.</summary>
        public string tree;
        /// <summary>Street lamps show a glowing bulb.</summary>
        public bool lampsLit;

        static readonly Theme[] All =
        {
            new Theme
            {
                name = "City Office",
                skyTop = new Color(0.36f, 0.62f, 0.94f),
                skyHorizon = new Color(0.78f, 0.9f, 1f),
                ground = new Color(0.55f, 0.78f, 0.45f),
                sidewalk = new Color(0.82f, 0.82f, 0.84f),
                road = new Color(0.32f, 0.34f, 0.38f),
                roadLine = new Color(0.95f, 0.95f, 0.9f),
                sunColor = new Color(1f, 0.96f, 0.88f),
                sunIntensity = 1.25f,
                sunEuler = new Vector3(26f, 22f, 0f),
                ambient = new Color(0.72f, 0.74f, 0.82f),
                tree = "tree_city",
            },
            new Theme
            {
                name = "Seaside Hotel",
                skyTop = new Color(0.98f, 0.5f, 0.45f),
                skyHorizon = new Color(1f, 0.84f, 0.6f),
                ground = new Color(0.93f, 0.84f, 0.62f),
                sidewalk = new Color(0.95f, 0.9f, 0.82f),
                road = new Color(0.42f, 0.38f, 0.4f),
                roadLine = new Color(1f, 0.92f, 0.8f),
                sunColor = new Color(1f, 0.78f, 0.6f),
                sunIntensity = 1.2f,
                sunEuler = new Vector3(18f, 38f, 0f),
                ambient = new Color(0.8f, 0.68f, 0.68f),
                tree = "tree_seaside",
            },
            new Theme
            {
                name = "Night Tower",
                skyTop = new Color(0.05f, 0.07f, 0.2f),
                skyHorizon = new Color(0.24f, 0.24f, 0.48f),
                ground = new Color(0.22f, 0.34f, 0.3f),
                sidewalk = new Color(0.46f, 0.48f, 0.56f),
                road = new Color(0.16f, 0.17f, 0.22f),
                roadLine = new Color(0.85f, 0.82f, 0.6f),
                sunColor = new Color(0.7f, 0.78f, 1f),
                sunIntensity = 0.8f,
                sunEuler = new Vector3(32f, -24f, 0f),
                ambient = new Color(0.56f, 0.58f, 0.76f),
                tree = "tree_night",
                lampsLit = true,
            },
        };

        public static int Count => All.Length;

        public static Theme Get(int index) => All[((index % All.Length) + All.Length) % All.Length];
    }
}
