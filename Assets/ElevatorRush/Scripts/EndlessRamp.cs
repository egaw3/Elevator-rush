using UnityEngine;

namespace ElevatorRush
{
    /// <summary>
    /// Pure difficulty curves for Endless mode (plan 5.9), driven by the delivery count.
    /// </summary>
    public static class EndlessRamp
    {
        public const int StartFloors = 4;
        /// <summary>Delivery counts at which the building gains its 5th, 6th, 7th and 8th floor.</summary>
        public static readonly int[] FloorThresholds = { 20, 40, 70, 110 };
        public const int GroundOnlyUntil = 10;
        public const int RampLength = 120;
        public const float StartSpawnInterval = 3.5f;
        public const float EndSpawnInterval = 1.3f;
        public const float StartPatience = 22f;
        public const float EndPatience = 11f;
        public const int KindsFrom = 30;
        public const float KindChance = 0.1f;

        public static int Floors(int delivered)
        {
            int floors = StartFloors;
            for (int i = 0; i < FloorThresholds.Length; i++)
                if (delivered >= FloorThresholds[i])
                    floors++;
            return floors;
        }

        public static bool GroundOnly(int delivered) => delivered < GroundOnlyUntil;

        public static float SpawnInterval(int delivered) =>
            Mathf.Lerp(StartSpawnInterval, EndSpawnInterval, Progress(delivered));

        public static float Patience(int delivered) =>
            Mathf.Lerp(StartPatience, EndPatience, Progress(delivered));

        public static float KindChanceAt(int delivered) => delivered >= KindsFrom ? KindChance : 0f;

        static float Progress(int delivered) => Mathf.Clamp01((float)delivered / RampLength);

        /// <summary>A no-target definition for the start of a run.</summary>
        public static LevelDefinition Create(int seed)
        {
            var level = new LevelDefinition(0, StartFloors, true, 0, StartSpawnInterval, StartPatience, 0f, 0f, false, 0);
            level.seed = seed;
            Apply(level, 0);
            return level;
        }

        /// <summary>Updates a run's definition in place for the current delivery count.</summary>
        public static void Apply(LevelDefinition level, int delivered)
        {
            level.floors = Floors(delivered);
            level.groundOnly = GroundOnly(delivered);
            level.spawnInterval = SpawnInterval(delivered);
            level.patience = Patience(delivered);
            level.vipChance = KindChanceAt(delivered);
            level.bigChance = KindChanceAt(delivered);
        }
    }
}
