using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace ElevatorRush.Editor
{
    /// <summary>
    /// Elevator Rush/Economy Report (home plan 9): for levels 1 to 200, the expected coins per level
    /// at two and three stars with an average combo of 1.5, the running total, and the running cost
    /// of everything unlocked so far with the cheapest and the most expensive decor options.
    /// Written to Logs/EconomyReport.txt, with a summary in the Console. The tool for tuning prices.
    /// </summary>
    public static class EconomyReport
    {
        const int LastLevel = 200;
        const float AverageCombo = 1.5f;
        const string OutputPath = "Logs/EconomyReport.txt";
        // Levels where the summary in the Console checks affordability.
        static readonly int[] SummaryLevels = { 10, 25, 50, 100, 150, 200 };

        [MenuItem("Elevator Rush/Economy Report")]
        public static void Run()
        {
            var config = new GameConfig();
            var table = new StringBuilder();
            var summary = new StringBuilder("[ElevatorRush] Economy report (full table in " + OutputPath + ")\n");
            table.AppendLine("level\tcoins2\tcoins3\tcum2\tcum3\tunlockedMin\tunlockedMax");

            double total2 = 0, total3 = 0;
            for (int n = 1; n <= LastLevel; n++)
            {
                var level = LevelCatalog.Get(n);
                double coins2 = ExpectedCoins(config, level, 2);
                double coins3 = ExpectedCoins(config, level, 3);
                total2 += coins2;
                total3 += coins3;
                // After winning level n, n levels are completed.
                int costMin = UnlockedCost(n, 0);
                int costMax = UnlockedCost(n, Catalog.OptionsPerSlot - 1);
                table.AppendLine(n + "\t" + Math.Round(coins2) + "\t" + Math.Round(coins3) + "\t" +
                    Math.Round(total2) + "\t" + Math.Round(total3) + "\t" + costMin + "\t" + costMax);

                if (Array.IndexOf(SummaryLevels, n) >= 0)
                    summary.AppendLine("Level " + n + ": earned " + Math.Round(total2) + " (2 stars) to " + Math.Round(total3) +
                        " (3 stars); everything unlocked costs " + costMin + " (basic decor) to " + costMax + " (premium decor)");
            }

            summary.AppendLine("Level 25 check (3 stars): " + Level25Check(TotalCoins(config, 25, 3)));

            Directory.CreateDirectory(Path.GetDirectoryName(OutputPath));
            File.WriteAllText(OutputPath, table.ToString());
            Debug.Log(summary.ToString());
        }

        /// <summary>Coins from one win: deliveries at the average combo and kind mix, plus the star bonus.</summary>
        static double ExpectedCoins(GameConfig config, LevelDefinition level, int stars)
        {
            double kind = 1.0 + level.vipChance * (config.vipCoinMultiplier - 1) + level.bigChance * (config.bigCoinMultiplier - 1);
            double deliveries = level.target * config.coinsPerDelivery * AverageCombo * kind;
            double bonus = config.starBonus * stars * (level.rush ? 2 : 1);
            return deliveries + bonus;
        }

        static double TotalCoins(GameConfig config, int lastLevel, int stars)
        {
            double total = 0;
            for (int n = 1; n <= lastLevel; n++)
                total += ExpectedCoins(config, LevelCatalog.Get(n), stars);
            return total;
        }

        /// <summary>
        /// The home plan's intent at level 25: basic decor for all of floor 0, most tier 1 elevator
        /// upgrades and one or two skins should be affordable, but not everything.
        /// </summary>
        static string Level25Check(double earned)
        {
            int floor0Basic = Catalog.SlotsPerFloor * Catalog.DecorPrice(0, 0);
            int tier1 = 0;
            foreach (var upgrade in Catalog.Upgrades)
                if (upgrade.CapNeeded(1) <= 1)
                    tier1 += upgrade.PriceOf(1);
            int twoSkins = Catalog.Skins[1].price + Catalog.Skins[2].price;
            int target = floor0Basic + tier1 + twoSkins;
            return "earned " + Math.Round(earned) + ", target bundle " + target + " (floor 0 basic " + floor0Basic +
                ", tier 1 upgrades " + tier1 + ", two skins " + twoSkins + "), everything " + UnlockedCost(25, Catalog.OptionsPerSlot - 1);
        }

        /// <summary>
        /// Cost of everything purchasable at this many levels completed: decor on unlocked floors
        /// (one option per slot, the given grade), elevator tiers within the cap, skins and tower skins.
        /// </summary>
        static int UnlockedCost(int levelsCompleted, int decorOption)
        {
            int cost = 0;
            for (int f = 0; f < Catalog.FloorCount; f++)
                if (UnlockSchedule.IsFloorUnlocked(f, levelsCompleted))
                    cost += Catalog.SlotsPerFloor * Catalog.DecorPrice(f, decorOption);
            foreach (var upgrade in Catalog.Upgrades)
                for (int t = 1; t <= upgrade.MaxTier; t++)
                    if (UnlockSchedule.IsTierUnlocked(upgrade, t, levelsCompleted))
                        cost += upgrade.PriceOf(t);
            foreach (var skin in Catalog.Skins)
                if (skin.unlockAt <= levelsCompleted)
                    cost += skin.price;
            foreach (var tower in Catalog.Towers)
                if (tower.unlockAt <= levelsCompleted)
                    cost += tower.price;
            return cost;
        }
    }
}
