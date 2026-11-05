using NUnit.Framework;

namespace ElevatorRush.Tests
{
    public class ScoreSystemTests
    {
        GameConfig config;
        ScoreSystem score;

        [SetUp]
        public void SetUp()
        {
            config = new GameConfig();
            score = new ScoreSystem(config);
        }

        [Test]
        public void StartsWithFullLivesAndComboOne()
        {
            Assert.AreEqual(config.startLives, score.Lives);
            Assert.AreEqual(1, score.Combo);
            Assert.AreEqual(0, score.LevelCoins);
        }

        [Test]
        public void DeliveryPaysBaseTimesComboTimesKind()
        {
            Assert.AreEqual(config.coinsPerDelivery, score.RegisterDelivery(1));
            Assert.AreEqual(config.coinsPerDelivery * 3, score.RegisterDelivery(3));
            score.RegisterDock(true);
            score.RegisterDock(true);
            Assert.AreEqual(3, score.Combo);
            Assert.AreEqual(config.coinsPerDelivery * 3 * 2, score.RegisterDelivery(2));
            Assert.AreEqual(config.coinsPerDelivery * (1 + 3 + 6), score.LevelCoins);
        }

        [Test]
        public void PerfectDockRaisesCombo()
        {
            score.RegisterDock(true);
            Assert.AreEqual(2, score.Combo);
        }

        [Test]
        public void NormalDockResetsCombo()
        {
            score.RegisterDock(true);
            score.RegisterDock(true);
            score.RegisterDock(false);
            Assert.AreEqual(1, score.Combo);
        }

        [Test]
        public void ComboIsCapped()
        {
            for (int i = 0; i < config.maxCombo + 5; i++)
                score.RegisterDock(true);
            Assert.AreEqual(config.maxCombo, score.Combo);
        }

        [Test]
        public void LosingLives()
        {
            Assert.IsFalse(score.LoseLife());
            Assert.AreEqual(config.startLives - 1, score.Lives);
            Assert.IsFalse(score.LoseLife());
            Assert.IsTrue(score.LoseLife());
            Assert.IsTrue(score.OutOfLives);
            score.LoseLife();
            Assert.AreEqual(0, score.Lives);
        }

        [Test]
        public void StarsByLivesLost()
        {
            Assert.AreEqual(3, score.Stars);
            score.LoseLife();
            Assert.AreEqual(2, score.Stars);
            score.LoseLife();
            Assert.AreEqual(1, score.Stars);
        }

        [Test]
        public void LevelBonusDoublesOnRush()
        {
            Assert.AreEqual(config.starBonus * 3, score.LevelBonus(false));
            Assert.AreEqual(config.starBonus * 3 * 2, score.LevelBonus(true));
        }

        [Test]
        public void ResetRestoresLivesAndCombo()
        {
            score.LoseLife();
            score.RegisterDock(true);
            score.RegisterDelivery(1);
            score.ResetForLevel();
            Assert.AreEqual(config.startLives, score.Lives);
            Assert.AreEqual(1, score.Combo);
            Assert.AreEqual(0, score.LevelCoins);
        }
    }
}
