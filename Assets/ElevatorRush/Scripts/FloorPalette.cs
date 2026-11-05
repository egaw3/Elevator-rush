using UnityEngine;

namespace ElevatorRush
{
    public enum FloorIcon
    {
        Circle,
        Triangle,
        Square,
        Diamond,
        Plus,
        Ring,
        Bar,
        TwoDots
    }

    /// <summary>
    /// Destination color and icon per floor index (plan section 8). Same in every theme.
    /// </summary>
    public static class FloorPalette
    {
        public const int Count = 8;

        static readonly Color[] Colors =
        {
            Hex(0xF2C14E), // 0 yellow
            Hex(0xE4572E), // 1 red-orange
            Hex(0x4D9DE0), // 2 blue
            Hex(0x3BB273), // 3 green
            Hex(0x9B5DE5), // 4 purple
            Hex(0xF15BB5), // 5 pink
            Hex(0x00BBF9), // 6 cyan
            Hex(0x8D99AE), // 7 grey
        };

        public static Color ColorOf(int floor) => Colors[Wrap(floor)];

        public static FloorIcon IconOf(int floor) => (FloorIcon)Wrap(floor);

        /// <summary>The floor color with its saturation scaled down, for walls and frames.</summary>
        public static Color WallTint(int floor, float saturation, float value)
        {
            Color.RGBToHSV(ColorOf(floor), out float h, out float s, out float v);
            return Color.HSVToRGB(h, s * saturation, Mathf.Max(v, value));
        }

        static int Wrap(int floor) => ((floor % Count) + Count) % Count;

        static Color Hex(int rgb)
        {
            return new Color(((rgb >> 16) & 0xFF) / 255f, ((rgb >> 8) & 0xFF) / 255f, (rgb & 0xFF) / 255f);
        }
    }
}
