namespace ElevatorRush
{
    public enum SessionState
    {
        Playing,
        Won,
        Failed
    }

    /// <summary>
    /// Pure progress of the level being played: deliveries against the target, lives through
    /// <see cref="ScoreSystem"/>, and the result. Once won or failed, nothing changes any more.
    /// A target of 0 or less means no target (Endless).
    /// </summary>
    public sealed class LevelSession
    {
        public LevelDefinition Level { get; }
        public ScoreSystem Score { get; }
        public int Delivered { get; private set; }
        public SessionState State { get; private set; } = SessionState.Playing;

        public bool HasTarget => Level.target > 0;
        public bool Ended => State != SessionState.Playing;
        public int Stars => State == SessionState.Won ? Score.Stars : 0;
        public int Bonus => State == SessionState.Won ? Score.LevelBonus(Level.rush) : 0;

        public LevelSession(GameConfig config, LevelDefinition level)
        {
            Level = level;
            Score = new ScoreSystem(config);
        }

        public void RegisterDock(bool perfect)
        {
            if (!Ended)
                Score.RegisterDock(perfect);
        }

        /// <summary>Counts a delivery and returns the coins it paid (0 once the session has ended).</summary>
        public int RegisterDelivery(int kindMultiplier)
        {
            if (Ended)
                return 0;
            int coins = Score.RegisterDelivery(kindMultiplier);
            Delivered++;
            if (HasTarget && Delivered >= Level.target)
                State = SessionState.Won;
            return coins;
        }

        /// <summary>Loses a life; the session fails when none are left.</summary>
        public void LoseLife()
        {
            if (Ended)
                return;
            if (Score.LoseLife())
                State = SessionState.Failed;
        }
    }
}
