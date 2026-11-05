using System;

namespace ElevatorRush
{
    /// <summary>Data for one level (plan 5.7).</summary>
    [Serializable]
    public sealed class LevelDefinition
    {
        public int number;
        public int floors;
        public bool groundOnly;
        public int target;
        public float spawnInterval;
        public float patience;
        public float vipChance;
        public float bigChance;
        public bool rush;
        public int theme;
        public int seed;

        public LevelDefinition(int number, int floors, bool groundOnly, int target, float spawnInterval,
            float patience, float vipChance, float bigChance, bool rush, int theme)
        {
            this.number = number;
            this.floors = floors;
            this.groundOnly = groundOnly;
            this.target = target;
            this.spawnInterval = spawnInterval;
            this.patience = patience;
            this.vipChance = vipChance;
            this.bigChance = bigChance;
            this.rush = rush;
            this.theme = theme;
            seed = number;
        }
    }
}
