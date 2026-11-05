using UnityEngine;

namespace ElevatorRush
{
    /// <summary>Procedural shape families; each has a few variants picked by <see cref="DecorLook.variant"/>.</summary>
    public enum DecorShape
    {
        /// <summary>A flooring or wall finish: a code-made pattern, no model.</summary>
        Surface,
        Plant,
        Counter,
        Desk,
        Seat,
        TableChairs,
        Shelf,
        Appliance,
        Machine,
        Round,
        Rack,
        Tub,
        Piano,
        WallArt
    }

    public enum SurfacePattern
    {
        Plain,
        Checker,
        Planks,
        Tiles,
        Herringbone,
        Stripes,
        Marble,
        Bricks,
        Panels,
        Dots,
        Grid,
        Rug
    }

    /// <summary>
    /// How one decor option looks: a procedural shape with up to three colors, or a surface
    /// pattern. <see cref="model"/> is an optional model key under Resources/Models, optionally
    /// followed by "@degrees" to turn it about Y; when the model is missing the procedural shape
    /// is the fallback.
    /// </summary>
    public readonly struct DecorLook
    {
        public readonly DecorShape shape;
        public readonly int variant;
        public readonly SurfacePattern pattern;
        public readonly Color a;
        public readonly Color b;
        public readonly Color c;
        public readonly string model;

        /// <summary>The model key without any "@degrees" suffix.</summary>
        public string ModelKey
        {
            get
            {
                if (model == null)
                    return null;
                int at = model.IndexOf('@');
                return at < 0 ? model : model.Substring(0, at);
            }
        }

        /// <summary>Degrees to turn the model about Y, from the "@degrees" suffix (0 without one).</summary>
        public float ModelYaw
        {
            get
            {
                int at = model != null ? model.IndexOf('@') : -1;
                return at >= 0 && float.TryParse(model.Substring(at + 1), System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out float yaw) ? yaw : 0f;
            }
        }

        public DecorLook(DecorShape shape, int variant, SurfacePattern pattern, Color a, Color b, Color c, string model)
        {
            this.shape = shape;
            this.variant = variant;
            this.pattern = pattern;
            this.a = a;
            this.b = b;
            this.c = c;
            this.model = model;
        }
    }
}
