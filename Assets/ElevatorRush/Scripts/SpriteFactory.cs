using System.Collections.Generic;
using UnityEngine;

namespace ElevatorRush
{
    /// <summary>
    /// Procedural icon textures and UI sprites, drawn from signed distance functions and cached.
    /// Shapes are white with an alpha mask; materials and UI colors tint them.
    /// </summary>
    public sealed class SpriteFactory
    {
        const int Size = 128;
        const int PanelSize = 64;
        const float PanelCornerPixels = 24f;
        const float PixelsPerUnit = 100f;
        const float Sqrt3 = 1.7320508f;

        readonly Dictionary<FloorIcon, Texture2D> icons = new Dictionary<FloorIcon, Texture2D>();
        readonly Dictionary<FloorIcon, Sprite> iconSprites = new Dictionary<FloorIcon, Sprite>();
        Texture2D disc;
        Sprite panel, circle, heart, star, coin, hand, handOutline;
        Sprite gear, person, arrowUp, house, tower, swap, chevron, padlock, gem;

        public Texture2D Icon(FloorIcon icon)
        {
            if (!icons.TryGetValue(icon, out var texture))
            {
                texture = Draw("Icon_" + icon, p => IconDistance(icon, p));
                icons.Add(icon, texture);
            }
            return texture;
        }

        /// <summary>A filled circle, used for speech bubbles.</summary>
        public Texture2D Disc
        {
            get
            {
                if (disc == null)
                    disc = Draw("Disc", p => p.magnitude - 0.94f);
                return disc;
            }
        }

        public Sprite IconSprite(FloorIcon icon)
        {
            if (!iconSprites.TryGetValue(icon, out var sprite))
            {
                sprite = ToSprite(Icon(icon));
                iconSprites.Add(icon, sprite);
            }
            return sprite;
        }

        /// <summary>White rounded rectangle, nine-sliced so it stretches to any size.</summary>
        public Sprite Panel
        {
            get
            {
                if (panel == null)
                {
                    float half = PanelSize * 0.5f;
                    var texture = Draw("Panel", PanelSize, p => Box(p * half, new Vector2(half - PanelCornerPixels, half - PanelCornerPixels)) - PanelCornerPixels + 1f, 1f / half);
                    var border = Vector4.one * (PanelCornerPixels + 2f);
                    panel = Sprite.Create(texture, new Rect(0, 0, PanelSize, PanelSize), new Vector2(0.5f, 0.5f),
                        PixelsPerUnit, 0, SpriteMeshType.FullRect, border);
                }
                return panel;
            }
        }

        public Sprite Circle => circle != null ? circle : (circle = ToSprite(Disc));

        public Sprite Heart => heart != null ? heart : (heart = ToSprite(Draw("Heart", p => HeartDistance(p))));

        public Sprite Star => star != null ? star : (star = ToSprite(Draw("Star", p => StarDistance(p, 0.92f, 0.45f))));

        /// <summary>A coin: a disc with a thin engraved ring.</summary>
        public Sprite Coin => coin != null ? coin : (coin = ToSprite(Draw("Coin",
            p => Mathf.Max(p.magnitude - 0.94f, -(Mathf.Abs(p.magnitude - 0.66f) - 0.05f)))));

        /// <summary>A cut gem: flat top, bevelled shoulders and a point, split by a thin girdle line.</summary>
        public Sprite Gem => gem != null ? gem : (gem = ToSprite(Draw("Gem", GemDistance)));

        /// <summary>A pointing hand for tutorial hints, with a slightly larger outline to put behind it.</summary>
        public Sprite Hand => hand != null ? hand : (hand = ToSprite(Draw("Hand", HandDistance)));

        public Sprite HandOutline => handOutline != null ? handOutline
            : (handOutline = ToSprite(Draw("HandOutline", p => HandDistance(p) - HandOutlineWidth)));

        // ---- Home screen icons (home plan 3.1) ----

        /// <summary>Settings gear.</summary>
        public Sprite Gear => gear != null ? gear : (gear = ToSprite(Draw("Gear", GearDistance)));

        /// <summary>A character: head and shoulders (Characters tab, skin unlocks).</summary>
        public Sprite Person => person != null ? person : (person = ToSprite(Draw("Person",
            p => Mathf.Min((p - new Vector2(0f, 0.4f)).magnitude - 0.3f, Box(p - new Vector2(0f, -0.5f), new Vector2(0.42f, 0.24f)) - 0.2f))));

        /// <summary>An up arrow (Elevator tab, elevator tiers).</summary>
        public Sprite ArrowUp => arrowUp != null ? arrowUp : (arrowUp = ToSprite(Draw("ArrowUp",
            p => Mathf.Min(Triangle(p - new Vector2(0f, 0.22f), 0.66f), Box(p - new Vector2(0f, -0.48f), new Vector2(0.2f, 0.38f))))));

        /// <summary>A house with a door (Build tab, floor unlocks).</summary>
        public Sprite House => house != null ? house : (house = ToSprite(Draw("House",
            p => Mathf.Max(Mathf.Min(Triangle(p - new Vector2(0f, 0.3f), 0.86f), Box(p - new Vector2(0f, -0.42f), new Vector2(0.6f, 0.42f))),
                -Box(p - new Vector2(0f, -0.58f), new Vector2(0.15f, 0.27f))))));

        /// <summary>A tall tower with a spire (Towers tab, tower skins).</summary>
        public Sprite Tower => tower != null ? tower : (tower = ToSprite(Draw("Tower",
            p => Mathf.Min(Box(p - new Vector2(0f, -0.26f), new Vector2(0.38f, 0.66f)), Triangle(p - new Vector2(0f, 0.62f), 0.38f)))));

        /// <summary>Two opposed arrows: swap a filled decor slot.</summary>
        public Sprite Swap => swap != null ? swap : (swap = ToSprite(Draw("Swap", p => Mathf.Min(
            Mathf.Min(Box(p - new Vector2(-0.12f, 0.3f), new Vector2(0.42f, 0.09f)), Triangle(Rotate(p - new Vector2(0.4f, 0.3f), 90f), 0.24f)),
            Mathf.Min(Box(p - new Vector2(0.12f, -0.3f), new Vector2(0.42f, 0.09f)), Triangle(Rotate(p - new Vector2(-0.4f, -0.3f), -90f), 0.24f))))));

        /// <summary>A chevron pointing right; flip the image for left.</summary>
        public Sprite Chevron => chevron != null ? chevron : (chevron = ToSprite(Draw("Chevron", p =>
            Mathf.Min(Box(Rotate(p - new Vector2(0f, 0.25f), 45f), new Vector2(0.42f, 0.12f)),
                Box(Rotate(p - new Vector2(0f, -0.25f), -45f), new Vector2(0.42f, 0.12f))))));

        /// <summary>A padlock: locked upgrades and items.</summary>
        public Sprite Lock => padlock != null ? padlock : (padlock = ToSprite(Draw("Lock", p => Mathf.Min(
            Box(p - new Vector2(0f, -0.3f), new Vector2(0.5f, 0.38f)) - 0.06f,
            Mathf.Max(Mathf.Abs((p - new Vector2(0f, 0.18f)).magnitude - 0.32f) - 0.1f, -p.y + 0.1f)))));

        /// <summary>Rotates a point by degrees (counterclockwise), which turns a shape drawn from it clockwise.</summary>
        static Vector2 Rotate(Vector2 p, float degrees)
        {
            float r = degrees * Mathf.Deg2Rad;
            float c = Mathf.Cos(r);
            float s = Mathf.Sin(r);
            return new Vector2(c * p.x - s * p.y, s * p.x + c * p.y);
        }

        static float GearDistance(Vector2 p)
        {
            const int teeth = 8;
            const float sector = Mathf.PI * 2f / teeth;
            float r = p.magnitude;
            float a = Mathf.Repeat(Mathf.Atan2(p.y, p.x) + sector * 0.5f, sector) - sector * 0.5f;
            var q = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r;
            float tooth = Box(q - new Vector2(0.76f, 0f), new Vector2(0.16f, 0.15f));
            float body = Mathf.Min(r - 0.64f, tooth);
            return Mathf.Max(body, -(r - 0.26f));
        }

        /// <summary>Where the pointing fingertip sits, in sprite space from -1 to 1.</summary>
        public static readonly Vector2 HandFingertip = new Vector2(-0.08f, 0.82f);
        const float HandOutlineWidth = 0.07f;

        static float HandDistance(Vector2 p)
        {
            float palm = Box(p - new Vector2(0.06f, -0.42f), new Vector2(0.24f, 0.22f)) - 0.14f;
            float index = Capsule(p, new Vector2(-0.08f, -0.2f), new Vector2(-0.08f, 0.7f), 0.13f);
            float middle = Capsule(p, new Vector2(0.14f, -0.22f), new Vector2(0.14f, 0.1f), 0.12f);
            float ring = Capsule(p, new Vector2(0.34f, -0.26f), new Vector2(0.34f, 0.02f), 0.11f);
            float thumb = Capsule(p, new Vector2(-0.26f, -0.46f), new Vector2(-0.5f, -0.14f), 0.12f);
            return Mathf.Min(Mathf.Min(palm, index), Mathf.Min(Mathf.Min(middle, ring), thumb));
        }

        static float GemDistance(Vector2 p)
        {
            var q = new Vector2(Mathf.Abs(p.x), p.y);
            float top = q.y - 0.5f;
            float shoulder = Vector2.Dot(q - new Vector2(0.5f, 0.5f), new Vector2(0.659f, 0.753f));
            float lower = Vector2.Dot(q - new Vector2(0.9f, 0.15f), new Vector2(0.743f, -0.669f));
            float body = Mathf.Max(top, Mathf.Max(shoulder, lower));
            float girdle = Mathf.Abs(q.y - 0.15f) - 0.035f;
            return Mathf.Max(body, -girdle);
        }

        static float Capsule(Vector2 p, Vector2 a, Vector2 b, float r)
        {
            var pa = p - a;
            var ba = b - a;
            float h = Mathf.Clamp01(Vector2.Dot(pa, ba) / Vector2.Dot(ba, ba));
            return (pa - ba * h).magnitude - r;
        }

        static Sprite ToSprite(Texture2D texture)
        {
            return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), PixelsPerUnit);
        }

        static Texture2D Draw(string name, System.Func<Vector2, float> distance)
        {
            return Draw(name, Size, distance, 1f);
        }

        /// <summary>
        /// Rasterizes a signed distance function over p in [-1, 1]. distanceToUnits converts the
        /// function's result into those units when it measures in something else (like pixels).
        /// </summary>
        static Texture2D Draw(string name, int size, System.Func<Vector2, float> distance, float distanceToUnits)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, true)
            {
                name = name,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Trilinear,
            };
            var pixels = new Color32[size * size];
            float pixel = 2f / size;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                var p = new Vector2((x + 0.5f) * pixel - 1f, (y + 0.5f) * pixel - 1f);
                // One-pixel soft edge; alpha 0.5 sits exactly on the shape outline.
                float a = Mathf.Clamp01(0.5f - distance(p) * distanceToUnits / pixel);
                pixels[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255f));
            }
            texture.SetPixels32(pixels);
            texture.Apply(true, true);
            return texture;
        }

        static float IconDistance(FloorIcon icon, Vector2 p)
        {
            switch (icon)
            {
                case FloorIcon.Circle: return p.magnitude - 0.72f;
                case FloorIcon.Triangle: return Triangle(p - new Vector2(0f, -0.12f), 0.8f);
                case FloorIcon.Square: return Box(p, new Vector2(0.62f, 0.62f));
                case FloorIcon.Diamond: return Box(Rotate45(p), new Vector2(0.58f, 0.58f));
                case FloorIcon.Plus:
                    return Mathf.Min(Box(p, new Vector2(0.78f, 0.24f)), Box(p, new Vector2(0.24f, 0.78f)));
                case FloorIcon.Ring: return Mathf.Abs(p.magnitude - 0.58f) - 0.18f;
                case FloorIcon.Bar: return Box(p, new Vector2(0.86f, 0.3f));
                case FloorIcon.TwoDots:
                    return Mathf.Min((p - new Vector2(-0.44f, 0f)).magnitude, (p - new Vector2(0.44f, 0f)).magnitude) - 0.32f;
                default: return p.magnitude - 0.72f;
            }
        }

        /// <summary>Heart with its tip at the bottom, filling most of the square.</summary>
        static float HeartDistance(Vector2 p)
        {
            const float scale = 0.62f;
            var q = new Vector2(Mathf.Abs(p.x) * scale, p.y * scale + 0.55f);
            float d;
            if (q.y + q.x > 1f)
                d = (q - new Vector2(0.25f, 0.75f)).magnitude - 0.35355339f;
            else
            {
                float m = 0.5f * Mathf.Max(q.x + q.y, 0f);
                float a = (q - new Vector2(0f, 1f)).sqrMagnitude;
                float b = (q - new Vector2(m, m)).sqrMagnitude;
                d = Mathf.Sqrt(Mathf.Min(a, b)) * Mathf.Sign(q.x - q.y);
            }
            return d / scale;
        }

        /// <summary>Five-point star, outer radius r, inner radius factor rf.</summary>
        static float StarDistance(Vector2 p, float r, float rf)
        {
            var k1 = new Vector2(0.809016994f, -0.587785252f);
            var k2 = new Vector2(-k1.x, k1.y);
            p.x = Mathf.Abs(p.x);
            p -= 2f * Mathf.Max(Vector2.Dot(k1, p), 0f) * k1;
            p -= 2f * Mathf.Max(Vector2.Dot(k2, p), 0f) * k2;
            p.x = Mathf.Abs(p.x);
            p.y -= r;
            var ba = rf * new Vector2(-k1.y, k1.x) - new Vector2(0f, 1f);
            float h = Mathf.Clamp(Vector2.Dot(p, ba) / Vector2.Dot(ba, ba), 0f, r);
            return (p - ba * h).magnitude * Mathf.Sign(p.y * ba.x - p.x * ba.y);
        }

        static float Box(Vector2 p, Vector2 half)
        {
            var d = new Vector2(Mathf.Abs(p.x) - half.x, Mathf.Abs(p.y) - half.y);
            var outside = new Vector2(Mathf.Max(d.x, 0f), Mathf.Max(d.y, 0f));
            return outside.magnitude + Mathf.Min(Mathf.Max(d.x, d.y), 0f);
        }

        static Vector2 Rotate45(Vector2 p)
        {
            const float c = 0.70710678f;
            return new Vector2(c * (p.x - p.y), c * (p.x + p.y));
        }

        /// <summary>Equilateral triangle pointing up, circumradius-ish size r.</summary>
        static float Triangle(Vector2 p, float r)
        {
            p.x = Mathf.Abs(p.x) - r;
            p.y = p.y + r / Sqrt3;
            if (p.x + Sqrt3 * p.y > 0f)
                p = new Vector2(p.x - Sqrt3 * p.y, -Sqrt3 * p.x - p.y) * 0.5f;
            p.x -= Mathf.Clamp(p.x, -2f * r, 0f);
            return -p.magnitude * Mathf.Sign(p.y);
        }
    }
}
