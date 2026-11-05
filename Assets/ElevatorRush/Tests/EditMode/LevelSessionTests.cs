using NUnit.Framework;

namespace ElevatorRush.Tests
{
    public class LevelSessionTests
    {
        GameConfig config;

        [SetUp]
        public void SetUp() => config = new GameConfig();

        LevelSession Session(int target) =>
            new LevelSession(config, new LevelDefinition(1, 3, true, target, 4f, 30f, 0f, 0f, false, 0));

        [Test]
        public void WinsExactlyAtTarget()
        {
            var s = Session(3);
            s.RegisterDelivery(1);
            s.RegisterDelivery(1);
            Assert.AreEqual(SessionState.Playing, s.State);
            s.RegisterDelivery(1);
            Assert.AreEqual(SessionState.Won, s.State);
            Assert.AreEqual(3, s.Delivered);
        }

        [Test]
        public void FailsAtZeroLives()
        {
            var s = Session(3);
            for (int i = 0; i < config.startLives - 1; i++)
                s.LoseLife();
            Assert.AreEqual(SessionState.Playing, s.State);
            s.LoseLife();
            Assert.AreEqual(SessionState.Failed, s.State);
        }

        [Test]
        public void NoWinAfterFail()
        {
            var s = Session(1);
            for (int i = 0; i < config.startLives; i++)
                s.LoseLife();
            Assert.AreEqual(0, s.RegisterDelivery(1));
            Assert.AreEqual(SessionState.Failed, s.State);
            Assert.AreEqual(0, s.Delivered);
        }

        [Test]
        public void NothingChangesAfterWin()
        {
            var s = Session(1);
            s.RegisterDelivery(1);
            s.LoseLife();
            Assert.AreEqual(SessionState.Won, s.State);
            Assert.AreEqual(config.startLives, s.Score.Lives);
            Assert.AreEqual(3, s.Stars);
            Assert.AreEqual(config.starBonus * 3, s.Bonus);
        }

        [Test]
        public void NoTargetNeverWins()
        {
            var s = Session(0);
            for (int i = 0; i < 100; i++)
                s.RegisterDelivery(1);
            Assert.AreEqual(SessionState.Playing, s.State);
        }

        [Test]
        public void RushDoublesBonus()
        {
            var s = new LevelSession(config, new LevelDefinition(5, 4, false, 1, 2.8f, 22f, 0f, 0f, true, 0));
            s.LoseLife();
            s.RegisterDelivery(1);
            Assert.AreEqual(config.starBonus * 2 * 2, s.Bonus);
        }
    }
}
