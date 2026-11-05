using System.Collections.Generic;
using UnityEngine;

namespace ElevatorRush
{
    /// <summary>
    /// Code-made textures for flooring and wall finishes (home plan 5.1): checkers, planks, tiles,
    /// bricks and so on in two colors. One tile of the pattern per texture, wrapped with Repeat
    /// and tiled by the material. Cached per pattern and color pair.
    /// </summary>
    public sealed class PatternFactory
    {
        const int Size = 64;

        readonly Dictionary<(SurfacePattern, Color32, Color32), Texture2D> cache = new Dictionary<(SurfacePattern, Color32, Color32), Texture2D>();

        public Texture2D Get(SurfacePattern pattern, Color a, Color b)
        {
            var key = (pattern, (Color32)a, (Color32)b);
            if (cache.TryGetValue(key, out var texture))
                return texture;

            texture = new Texture2D(Size, Size, TextureFormat.RGBA32, true)
            {
                name = "Pattern_" + pattern,
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Bilinear,
                anisoLevel = 2,
            };
            var pixels = new Color32[Size * Size];
            for (int y = 0; y < Size; y++)
                for (int x = 0; x < Size; x++)
                {
                    float u = (x + 0.5f) / Size;
                    float v = (y + 0.5f) / Size;
                    pixels[y * Size + x] = Color.Lerp(a, b, Mix(pattern, u, v));
                }
            texture.SetPixels32(pixels);
            texture.Apply(true, true);
            cache.Add(key, texture);
            return texture;
        }

        /// <summary>How many meters one texture tile covers for a pattern.</summary>
        public static float TileMeters(SurfacePattern pattern, GameConfig config)
        {
            return pattern == SurfacePattern.Marble || pattern == SurfacePattern.Rug ? config.decorMarbleTileSize : config.decorTileSize;
        }

        /// <summary>0 for the base color, 1 for the second color, at (u, v) in one tile.</summary>
        static float Mix(SurfacePattern pattern, float u, float v)
        {
            switch (pattern)
            {
                case SurfacePattern.Checker:
                    return (u < 0.5f) ^ (v < 0.5f) ? 1f : 0f;
                case SurfacePattern.Planks:
                {
                    // Four planks per tile, end joints staggered, a darker seam and grain.
                    int row = Mathf.FloorToInt(v * 4f);
                    float along = Mathf.Repeat(u + row * 0.37f, 1f);
                    bool seam = Frac(v * 4f) < 0.06f || along < 0.03f;
                    float grain = 0.18f * Mathf.Abs(Mathf.Sin((v * 40f + row * 3f) + Mathf.Sin(u * 9f + row) * 1.5f));
                    return seam ? 1f : grain + (row % 2) * 0.12f;
                }
                case SurfacePattern.Tiles:
                    return Frac(u * 2f) < 0.05f || Frac(v * 2f) < 0.05f ? 1f : 0f;
                case SurfacePattern.Herringbone:
                {
                    float a = Frac((u + v) * 2f);
                    float b = Frac((u - v) * 4f);
                    bool zig = Frac(u * 4f) < 0.5f;
                    return (zig ? a : b) < 0.08f ? 1f : (zig ? 0.15f : 0f);
                }
                case SurfacePattern.Stripes:
                    return Frac(u * 2f) < 0.35f ? 1f : 0f;
                case SurfacePattern.Marble:
                {
                    float vein = Mathf.Abs(Mathf.Sin((u * 3f + v * 2f + Mathf.PerlinNoise(u * 4f, v * 4f) * 2.5f) * Mathf.PI));
                    return Mathf.Pow(1f - vein, 12f) + Mathf.PerlinNoise(u * 12f + 5f, v * 12f) * 0.08f;
                }
                case SurfacePattern.Bricks:
                {
                    int row = Mathf.FloorToInt(v * 4f);
                    float along = Frac(u * 2f + (row % 2) * 0.5f);
                    return Frac(v * 4f) < 0.1f || along < 0.05f ? 1f : 0f;
                }
                case SurfacePattern.Panels:
                    return Frac(u * 2f) < 0.04f || Frac(u * 2f) > 0.96f ? 1f : 0.1f * Mathf.Abs(Mathf.Sin(u * 30f));
                case SurfacePattern.Dots:
                {
                    float du = Frac(u * 3f) - 0.5f;
                    float dv = Frac(v * 3f) - 0.5f;
                    return du * du + dv * dv < 0.08f ? 1f : 0f;
                }
                case SurfacePattern.Grid:
                    return Frac(u * 2f) < 0.06f || Frac(v * 2f) < 0.06f ? 1f : 0f;
                case SurfacePattern.Rug:
                {
                    // A border and a diamond medallion.
                    float edge = Mathf.Min(Mathf.Min(u, 1f - u), Mathf.Min(v, 1f - v));
                    float diamond = Mathf.Abs(u - 0.5f) + Mathf.Abs(v - 0.5f);
                    return edge < 0.06f || (edge > 0.09f && edge < 0.11f) || Mathf.Abs(diamond - 0.25f) < 0.03f ? 1f : 0f;
                }
                default:
                    return 0f;
            }
        }

        static float Frac(float x) => x - Mathf.Floor(x);
    }
}
