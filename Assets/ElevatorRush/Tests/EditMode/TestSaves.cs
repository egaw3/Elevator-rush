namespace ElevatorRush.Tests
{
    /// <summary>Builds saves for the meta-game tests.</summary>
    static class TestSaves
    {
        /// <summary>A fresh, migrated save with this many levels won and this many coins.</summary>
        public static SaveData With(int levelsCompleted, int coins = 0)
        {
            var save = SaveMigration.Migrate(new SaveData());
            for (int i = 1; i <= levelsCompleted; i++)
                save.RecordLevelComplete(i, ScoreSystem.MaxStars);
            save.coins = coins;
            return save;
        }
    }
}
