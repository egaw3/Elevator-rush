namespace ElevatorRush
{
    /// <summary>The "Next unlock" teaser (home plan 4.2). <see cref="id"/> is null after the last row.</summary>
    public readonly struct NextUnlock
    {
        public readonly string id;
        public readonly int threshold;
        public readonly int levelsToGo;
        /// <summary>Bar fill from the previous threshold to this one, 0 to 1.</summary>
        public readonly float fill;

        public bool IsDone => id == null;

        public NextUnlock(string id, int threshold, int levelsToGo, float fill)
        {
            this.id = id;
            this.threshold = threshold;
            this.levelsToGo = levelsToGo;
            this.fill = fill;
        }
    }
}
