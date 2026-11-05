namespace ElevatorRush
{
    /// <summary>
    /// The effective values for one run after elevator upgrades (home plan 6). Gameplay reads
    /// these and never the save. Patience lives in each <see cref="LevelDefinition"/>, so Comfort
    /// is a factor rather than a config value.
    /// </summary>
    public sealed class RunUpgrades
    {
        /// <summary>A copy of the base config with motor, door and capacity changes applied.</summary>
        public readonly GameConfig config;
        /// <summary>Multiplies the patience of every waiting passenger.</summary>
        public readonly float patienceFactor;
        /// <summary>Multiplies the coins from each delivery.</summary>
        public readonly float coinFactor;

        public RunUpgrades(GameConfig config, float patienceFactor, float coinFactor)
        {
            this.config = config;
            this.patienceFactor = patienceFactor;
            this.coinFactor = coinFactor;
        }

        /// <summary>
        /// Copies the upgraded values into the config that gameplay objects share, so they take
        /// effect for the next run without rewiring anything.
        /// </summary>
        public void ApplyTo(GameConfig target)
        {
            target.riseAccel = config.riseAccel;
            target.maxRiseSpeed = config.maxRiseSpeed;
            target.dwellTime = config.dwellTime;
            target.exchangeTime = config.exchangeTime;
            target.cabinCapacity = config.cabinCapacity;
            target.cabinWidth = config.cabinWidth;
            target.shaftWidth = config.shaftWidth;
        }
    }
}
