using NUnit.Framework;

namespace ElevatorRush.Tests
{
    public class UpgradeMathTests
    {
        const float Eps = 1e-5f;
        GameConfig config;

        [SetUp]
        public void SetUp()
        {
            config = new GameConfig();
        }

        [Test]
        public void ZeroTiersReturnTheBaseConfigUnchanged()
        {
            var run = UpgradeMath.Apply(config, TestSaves.With(0));
            Assert.AreNotSame(config, run.config);
            Assert.AreEqual(config.riseAccel, run.config.riseAccel);
            Assert.AreEqual(config.maxRiseSpeed, run.config.maxRiseSpeed);
            Assert.AreEqual(config.dwellTime, run.config.dwellTime);
            Assert.AreEqual(config.exchangeTime, run.config.exchangeTime);
            Assert.AreEqual(config.cabinCapacity, run.config.cabinCapacity);
            Assert.AreEqual(1f, run.patienceFactor);
            Assert.AreEqual(1f, run.coinFactor);
            AssertHandlingUnchanged(run.config);
        }

        [TestCase(1, 1.05f)]
        [TestCase(5, 1.25f)]
        public void MotorRaisesRiseAccelAndMaxSpeed(int tier, float factor)
        {
            var run = UpgradeMath.Apply(config, tier, 0, 0, 0, 0);
            Assert.AreEqual(config.riseAccel * factor, run.config.riseAccel, Eps);
            Assert.AreEqual(config.maxRiseSpeed * factor, run.config.maxRiseSpeed, Eps);
            AssertHandlingUnchanged(run.config);
        }

        [TestCase(1, 0.92f)]
        [TestCase(5, 0.60f)]
        public void DoorsShortenDwellAndExchange(int tier, float factor)
        {
            var run = UpgradeMath.Apply(config, 0, tier, 0, 0, 0);
            Assert.AreEqual(config.dwellTime * factor, run.config.dwellTime, Eps);
            Assert.AreEqual(config.exchangeTime * factor, run.config.exchangeTime, Eps);
            AssertHandlingUnchanged(run.config);
        }

        [TestCase(1, 1.06f)]
        [TestCase(5, 1.30f)]
        public void ComfortRaisesPatience(int tier, float factor)
        {
            Assert.AreEqual(factor, UpgradeMath.Apply(config, 0, 0, tier, 0, 0).patienceFactor, Eps);
        }

        [TestCase(1, 1.1f)]
        [TestCase(5, 1.5f)]
        public void TipsRaiseCoins(int tier, float factor)
        {
            Assert.AreEqual(factor, UpgradeMath.Apply(config, 0, 0, 0, tier, 0).coinFactor, Eps);
        }

        [TestCase(1, 5)]
        [TestCase(2, 6)]
        [TestCase(9, 6)]
        public void CapacityAddsCabinSlots(int tier, int slots)
        {
            Assert.AreEqual(slots, UpgradeMath.Apply(config, 0, 0, 0, 0, tier).config.cabinCapacity);
        }

        [Test]
        public void ReadsTiersFromTheSave()
        {
            var save = TestSaves.With(0);
            save.SetUpgradeTier("elev.motor", 2);
            save.SetUpgradeTier("elev.capacity", 1);
            var run = UpgradeMath.Apply(config, save);
            Assert.AreEqual(config.riseAccel * 1.1f, run.config.riseAccel, Eps);
            Assert.AreEqual(config.cabinCapacity + 1, run.config.cabinCapacity);
        }

        [Test]
        public void BaseConfigIsNeverModified()
        {
            float accel = config.riseAccel;
            UpgradeMath.Apply(config, 5, 5, 5, 5, 2);
            Assert.AreEqual(accel, config.riseAccel);
            Assert.AreEqual(4, config.cabinCapacity);
        }

        [Test]
        public void TipsRoundUpOnceAtTheEnd()
        {
            float factor = UpgradeMath.Apply(config, 0, 0, 0, 1, 0).coinFactor;
            double total = 0;
            for (int i = 0; i < 10; i++)
                total += 1 * factor;
            // 10 x 1.1 is exactly 11 coins, despite float error.
            Assert.AreEqual(11, UpgradeMath.RoundCoins(total));
            Assert.AreEqual(4, UpgradeMath.RoundCoins(3 * 1.1));
            Assert.AreEqual(1, UpgradeMath.RoundCoins(0.1));
            Assert.AreEqual(0, UpgradeMath.RoundCoins(0));
            Assert.AreEqual(7, UpgradeMath.RoundCoins(7));
        }

        [Test]
        public void EffectTextPercents()
        {
            Assert.AreEqual(5, UpgradeMath.PercentPerTier(UpgradeKind.Motor));
            Assert.AreEqual(8, UpgradeMath.PercentPerTier(UpgradeKind.Doors));
            Assert.AreEqual(6, UpgradeMath.PercentPerTier(UpgradeKind.Comfort));
            Assert.AreEqual(10, UpgradeMath.PercentPerTier(UpgradeKind.Tips));
        }

        void AssertHandlingUnchanged(GameConfig run)
        {
            Assert.AreEqual(config.gravity, run.gravity);
            Assert.AreEqual(config.maxFallSpeed, run.maxFallSpeed);
            Assert.AreEqual(config.snapRange, run.snapRange);
            Assert.AreEqual(config.snapSpeed, run.snapSpeed);
            Assert.AreEqual(config.perfectRange, run.perfectRange);
        }
    }
}
