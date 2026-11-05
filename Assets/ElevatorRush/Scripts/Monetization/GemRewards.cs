namespace ElevatorRush
{
    /// <summary>
    /// Pure free gem sources from play (monetization plan 2.1): the first three-star finish of a
    /// level, and each row of the unlock schedule reached. Floor completion gems are paid with the
    /// floor bonus in <see cref="TowerProgress"/>.
    /// </summary>
    public static class GemRewards
    {
        /// <summary>
        /// Call after <see cref="SaveData.RecordLevelComplete"/> with the levels completed before it.
        /// Adds and returns the gems earned.
        /// </summary>
        public static int ForLevelComplete(SaveData save, int level, int levelsCompletedBefore)
        {
            int gems = 0;
            if (save.BestStars(level) >= ScoreSystem.MaxStars && !save.threeStarGemPaid.Contains(level))
            {
                save.threeStarGemPaid.Add(level);
                gems += Catalog.GemsPerThreeStars;
            }
            gems += Catalog.GemsPerUnlockRow * UnlockSchedule.RowsCrossed(levelsCompletedBefore, save.LevelsCompleted);
            new Wallet(save).AddGems(gems);
            return gems;
        }
    }
}
