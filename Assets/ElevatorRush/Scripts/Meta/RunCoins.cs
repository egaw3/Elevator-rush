namespace ElevatorRush
{
    /// <summary>
    /// Pure coin counter for one run with the Tips upgrade (home plan 6): deliveries add
    /// fractional coins, whole coins are banked as they are reached (so a failed level keeps
    /// them, as before), and the total is rounded up once when the run ends. Without Tips it
    /// banks exactly the delivery coins, so an unupgraded save earns the same as before.
    /// </summary>
    public sealed class RunCoins
    {
        readonly float factor;
        double total;

        /// <summary>Whole coins handed out so far this run.</summary>
        public int Banked { get; private set; }
        public bool Finished { get; private set; }

        public RunCoins(float tipFactor)
        {
            factor = tipFactor;
        }

        /// <summary>The value of a delivery after Tips, for the floating "+N" text.</summary>
        public double Value(int deliveryCoins) => deliveryCoins * (double)factor;

        /// <summary>Adds a delivery's coins; returns the whole coins to bank now.</summary>
        public int Add(int deliveryCoins)
        {
            if (Finished)
                return 0;
            total += Value(deliveryCoins);
            return BankUpTo(UpgradeMath.WholeCoins(total));
        }

        /// <summary>Ends the run: rounds the total up once and returns the remaining coins to bank.</summary>
        public int Finish()
        {
            if (Finished)
                return 0;
            Finished = true;
            return BankUpTo(UpgradeMath.RoundCoins(total));
        }

        int BankUpTo(int whole)
        {
            int now = whole - Banked;
            if (now <= 0)
                return 0;
            Banked = whole;
            return now;
        }
    }
}
