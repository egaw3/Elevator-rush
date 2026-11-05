namespace ElevatorRush
{
    /// <summary>Passenger kinds (plan 5.5).</summary>
    public enum PassengerKind
    {
        Normal,
        Vip,
        Big
    }

    /// <summary>Rules per kind: cabin slots, coin multiplier, patience factor.</summary>
    public static class PassengerKindRules
    {
        public static int Slots(this PassengerKind kind, GameConfig config)
        {
            return kind == PassengerKind.Big ? config.bigSlots : 1;
        }

        public static int CoinMultiplier(this PassengerKind kind, GameConfig config)
        {
            switch (kind)
            {
                case PassengerKind.Vip: return config.vipCoinMultiplier;
                case PassengerKind.Big: return config.bigCoinMultiplier;
                default: return 1;
            }
        }

        public static float PatienceFactor(this PassengerKind kind, GameConfig config)
        {
            return kind == PassengerKind.Vip ? config.vipPatienceFactor : 1f;
        }

        /// <summary>Picks a kind from one roll in [0, 1): VIP first, then Big, otherwise Normal.</summary>
        public static PassengerKind Pick(double roll, float vipChance, float bigChance)
        {
            if (roll < vipChance)
                return PassengerKind.Vip;
            if (roll < vipChance + bigChance)
                return PassengerKind.Big;
            return PassengerKind.Normal;
        }
    }
}
