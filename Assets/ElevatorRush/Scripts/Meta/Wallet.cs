using System;

namespace ElevatorRush
{
    /// <summary>Pure coin and gem balances on top of the save: add and spend, never below zero.</summary>
    public sealed class Wallet
    {
        readonly SaveData save;

        public Wallet(SaveData save)
        {
            this.save = save;
        }

        public int Balance => save.coins;
        public int Gems => save.gems;

        public bool CanAfford(int amount) => amount <= save.coins;

        public bool CanAffordGems(int amount) => amount <= save.gems;

        public void Add(int amount)
        {
            if (amount < 0)
                throw new ArgumentOutOfRangeException(nameof(amount), "Use TrySpend to take coins away.");
            save.coins += amount;
        }

        public void AddGems(int amount)
        {
            if (amount < 0)
                throw new ArgumentOutOfRangeException(nameof(amount), "Use TrySpendGems to take gems away.");
            save.gems += amount;
        }

        /// <summary>Takes the amount and returns true, or changes nothing and returns false.</summary>
        public bool TrySpend(int amount)
        {
            if (amount < 0 || !CanAfford(amount))
                return false;
            save.coins -= amount;
            return true;
        }

        /// <summary>Takes the gems and returns true, or changes nothing and returns false.</summary>
        public bool TrySpendGems(int amount)
        {
            if (amount < 0 || !CanAffordGems(amount))
                return false;
            save.gems -= amount;
            return true;
        }
    }
}
