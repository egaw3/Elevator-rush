using NUnit.Framework;

namespace ElevatorRush.Tests
{
    public class EndlessRampTests
    {
        const float Eps = 1e-4f;

        [TestCase(0, 4)]
        [TestCase(19, 4)]
        [TestCase(20, 5)]
        [TestCase(39, 5)]
        [TestCase(40, 6)]
        [TestCase(69, 6)]
        [TestCase(70, 7)]
        [TestCase(109, 7)]
        [TestCase(110, 8)]
        [TestCase(1000, 8)]
        public void FloorsGrowAtThresholds(int delivered, int floors)
        {
            Assert.AreEqual(floors, EndlessRamp.Floors(delivered));
        }

        [Test]
        public void GroundOnlyForFirstTenDeliveries()
        {
            Assert.IsTrue(EndlessRamp.GroundOnly(0));
            Assert.IsTrue(EndlessRamp.GroundOnly(9));
            Assert.IsFalse(EndlessRamp.GroundOnly(10));
        }

        [Test]
        public void SpawnIntervalRampsLinearlyOver120()
        {
            Assert.AreEqual(3.5f, EndlessRamp.SpawnInterval(0), Eps);
            Assert.AreEqual(2.4f, EndlessRamp.SpawnInterval(60), Eps);
            Assert.AreEqual(1.3f, EndlessRamp.SpawnInterval(120), Eps);
            Assert.AreEqual(1.3f, EndlessRamp.SpawnInterval(500), Eps);
        }

        [Test]
        public void PatienceRampsLinearlyOver120()
        {
            Assert.AreEqual(22f, EndlessRamp.Patience(0), Eps);
            Assert.AreEqual(16.5f, EndlessRamp.Patience(60), Eps);
            Assert.AreEqual(11f, EndlessRamp.Patience(120), Eps);
            Assert.AreEqual(11f, EndlessRamp.Patience(500), Eps);
        }

        [Test]
        public void KindsAppearAtThirty()
        {
            Assert.AreEqual(0f, EndlessRamp.KindChanceAt(29));
            Assert.AreEqual(0.1f, EndlessRamp.KindChanceAt(30), Eps);
        }

        [Test]
        public void ApplyUpdatesDefinitionInPlace()
        {
            var level = EndlessRamp.Create(7);
            Assert.AreEqual(4, level.floors);
            Assert.IsTrue(level.groundOnly);
            Assert.AreEqual(0, level.target);
            Assert.AreEqual(7, level.seed);
            EndlessRamp.Apply(level, 45);
            Assert.AreEqual(6, level.floors);
            Assert.IsFalse(level.groundOnly);
            Assert.AreEqual(0.1f, level.vipChance, Eps);
            Assert.AreEqual(0.1f, level.bigChance, Eps);
        }
    }
}
