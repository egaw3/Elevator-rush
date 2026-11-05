namespace ElevatorRush
{
    /// <summary>
    /// Hand-made levels 1 to 20 (plan 5.7); later levels come from <see cref="LevelGenerator"/>.
    /// </summary>
    public static class LevelCatalog
    {
        public const int HandMadeCount = 20;
        /// <summary>Completing this level unlocks Endless mode.</summary>
        public const int EndlessUnlockLevel = 10;

        // number, floors, groundOnly, target, spawn s, patience s, vip, big
        static readonly LevelDefinition[] Levels =
        {
            L(1, 3, true, 4, 4.0f, 30f, 0f, 0f),
            L(2, 3, true, 6, 3.5f, 26f, 0f, 0f),
            L(3, 4, true, 8, 3.2f, 24f, 0f, 0f),
            L(4, 4, false, 8, 3.5f, 26f, 0f, 0f),
            L(5, 4, false, 12, 2.8f, 22f, 0f, 0f),
            L(6, 5, false, 10, 3.0f, 22f, 0f, 0f),
            L(7, 5, false, 12, 2.8f, 21f, 0f, 0f),
            L(8, 5, false, 12, 2.8f, 21f, 0.15f, 0f),
            L(9, 5, false, 14, 2.6f, 20f, 0.10f, 0f),
            L(10, 5, false, 18, 2.2f, 19f, 0.10f, 0f),
            L(11, 6, false, 14, 2.6f, 20f, 0.10f, 0f),
            L(12, 6, false, 14, 2.6f, 19f, 0.05f, 0.15f),
            L(13, 6, false, 16, 2.4f, 18f, 0.10f, 0.10f),
            L(14, 6, false, 16, 2.4f, 18f, 0.10f, 0.10f),
            L(15, 6, false, 22, 2.0f, 17f, 0.10f, 0.10f),
            L(16, 7, false, 16, 2.4f, 18f, 0.10f, 0.10f),
            L(17, 7, false, 18, 2.2f, 17f, 0.10f, 0.10f),
            L(18, 7, false, 18, 2.2f, 16f, 0.12f, 0.12f),
            L(19, 7, false, 20, 2.0f, 16f, 0.12f, 0.12f),
            L(20, 7, false, 26, 1.8f, 15f, 0.12f, 0.12f),
        };

        static LevelDefinition L(int n, int floors, bool groundOnly, int target, float spawn, float patience,
            float vip, float big)
        {
            return new LevelDefinition(n, floors, groundOnly, target, spawn, patience, vip, big,
                LevelGenerator.IsRush(n), LevelGenerator.ThemeOf(n));
        }

        /// <summary>The definition for a 1-based level number.</summary>
        public static LevelDefinition Get(int number)
        {
            if (number < 1)
                number = 1;
            return number <= HandMadeCount ? Levels[number - 1] : LevelGenerator.Generate(number);
        }

        /// <summary>Level 1 hints "hold to rise"; level 4 hints "match the color".</summary>
        public const int HoldHintLevel = 1;
        public const int ColorHintLevel = 4;
    }
}
