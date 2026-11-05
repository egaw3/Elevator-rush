using System;

namespace ElevatorRush
{
    /// <summary>
    /// Pure generator for level 21 and up (plan 5.7). The same number always gives the same level.
    /// </summary>
    public static class LevelGenerator
    {
        public const int FirstGeneratedLevel = 21;
        public const int RushEvery = 5;
        public const int LevelsPerTheme = 10;
        public const int ThemeCount = 3;

        const int MaxFloors = 8;
        const int BaseFloors = 7;
        const int FloorStep = 15;
        const int MaxTarget = 30;
        const int BaseTarget = 18;
        const int TargetStep = 3;
        const float RushTargetFactor = 1.3f;
        const float MinSpawnInterval = 1.3f;
        const float BaseSpawnInterval = 2.0f;
        const float SpawnIntervalStep = 0.01f;
        const float RushSpawnFactor = 0.85f;
        const float MinPatience = 11f;
        const float BasePatience = 16f;
        const float PatienceStep = 0.1f;
        const float VipChance = 0.12f;
        const float BigChance = 0.12f;

        public static bool IsRush(int number) => number % RushEvery == 0;

        public static int ThemeOf(int number) => ((number - 1) / LevelsPerTheme) % ThemeCount;

        public static LevelDefinition Generate(int number)
        {
            int k = Math.Max(0, number - FirstGeneratedLevel);
            bool rush = IsRush(number);

            int floors = Math.Min(MaxFloors, BaseFloors + k / FloorStep);
            int target = Math.Min(MaxTarget, BaseTarget + k / TargetStep);
            if (rush)
                target = (int)Math.Round(target * RushTargetFactor, MidpointRounding.AwayFromZero);
            float spawn = Math.Max(MinSpawnInterval, BaseSpawnInterval - SpawnIntervalStep * k);
            if (rush)
                spawn *= RushSpawnFactor;
            float patience = Math.Max(MinPatience, BasePatience - PatienceStep * k);

            return new LevelDefinition(number, floors, false, target, spawn, patience,
                VipChance, BigChance, rush, ThemeOf(number));
        }
    }
}
