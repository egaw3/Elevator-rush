using UnityEngine;

namespace ElevatorRush
{
    /// <summary>
    /// Pure coins, combo, lives and stars for the level being played (plan 5.6).
    /// </summary>
    public sealed class ScoreSystem
    {
        public const int MaxStars = 3;

        readonly GameConfig config;

        public int Combo { get; private set; } = 1;
        public int Lives { get; private set; }
        public int LivesLost => config.startLives - Lives;
        /// <summary>Coins earned from deliveries in this level (bonus not included).</summary>
        public int LevelCoins { get; private set; }
        public bool OutOfLives => Lives <= 0;

        /// <summary>3 stars with no life lost, 2 with one, 1 with two; 0 once out of lives.</summary>
        public int Stars => OutOfLives ? 0 : Mathf.Clamp(MaxStars - LivesLost, 1, MaxStars);

        public ScoreSystem(GameConfig config)
        {
            this.config = config;
            ResetForLevel();
        }

        public void ResetForLevel()
        {
            Combo = 1;
            Lives = config.startLives;
            LevelCoins = 0;
        }

        /// <summary>A perfect dock raises the combo up to the cap; a normal dock resets it to 1.</summary>
        public void RegisterDock(bool perfect)
        {
            Combo = perfect ? Mathf.Min(Combo + 1, config.maxCombo) : 1;
        }

        /// <summary>Pays for one delivery and returns the coins earned.</summary>
        public int RegisterDelivery(int kindMultiplier)
        {
            int coins = config.coinsPerDelivery * Combo * kindMultiplier;
            LevelCoins += coins;
            return coins;
        }

        /// <summary>Loses one life; returns true when that was the last one.</summary>
        public bool LoseLife()
        {
            if (Lives > 0)
                Lives--;
            return OutOfLives;
        }

        public int LevelBonus(bool rush)
        {
            return config.starBonus * Stars * (rush ? 2 : 1);
        }
    }
}
