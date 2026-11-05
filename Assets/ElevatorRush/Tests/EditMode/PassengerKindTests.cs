using NUnit.Framework;

namespace ElevatorRush.Tests
{
    public class PassengerKindTests
    {
        readonly GameConfig config = new GameConfig();

        [Test]
        public void VipHasLessPatienceAndPaysTriple()
        {
            Assert.AreEqual(0.6f, PassengerKind.Vip.PatienceFactor(config), 1e-4f);
            Assert.AreEqual(3, PassengerKind.Vip.CoinMultiplier(config));
            Assert.AreEqual(1, PassengerKind.Vip.Slots(config));
        }

        [Test]
        public void BigTakesTwoSlotsAndPaysDouble()
        {
            Assert.AreEqual(2, PassengerKind.Big.Slots(config));
            Assert.AreEqual(2, PassengerKind.Big.CoinMultiplier(config));
            Assert.AreEqual(1f, PassengerKind.Big.PatienceFactor(config));
        }

        [Test]
        public void NormalIsPlain()
        {
            Assert.AreEqual(1, PassengerKind.Normal.Slots(config));
            Assert.AreEqual(1, PassengerKind.Normal.CoinMultiplier(config));
            Assert.AreEqual(1f, PassengerKind.Normal.PatienceFactor(config));
        }

        [Test]
        public void PickUsesChancesInOrder()
        {
            Assert.AreEqual(PassengerKind.Vip, PassengerKindRules.Pick(0.05, 0.1f, 0.2f));
            Assert.AreEqual(PassengerKind.Big, PassengerKindRules.Pick(0.15, 0.1f, 0.2f));
            Assert.AreEqual(PassengerKind.Normal, PassengerKindRules.Pick(0.31, 0.1f, 0.2f));
            Assert.AreEqual(PassengerKind.Normal, PassengerKindRules.Pick(0.0, 0f, 0f));
        }
    }
}
