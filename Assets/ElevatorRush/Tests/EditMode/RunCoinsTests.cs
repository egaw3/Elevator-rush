using NUnit.Framework;

namespace ElevatorRush.Tests
{
    public class RunCoinsTests
    {
        [Test]
        public void WithoutTipsEveryDeliveryBanksExactly()
        {
            var run = new RunCoins(1f);
            Assert.AreEqual(3, run.Add(3));
            Assert.AreEqual(2, run.Add(2));
            Assert.AreEqual(0, run.Finish());
            Assert.AreEqual(5, run.Banked);
        }

        [Test]
        public void TipsBankWholeCoinsAndRoundUpOnceAtTheEnd()
        {
            float tips = UpgradeMath.Factor(UpgradeKind.Tips, 1);
            var run = new RunCoins(tips);
            Assert.AreEqual(1, run.Add(1)); // 1.1
            Assert.AreEqual(1, run.Add(1)); // 2.2
            Assert.AreEqual(1, run.Add(1)); // 3.3
            Assert.AreEqual(1, run.Finish()); // rounds 3.3 up to 4
            Assert.AreEqual(4, run.Banked);
        }

        [Test]
        public void ExactTotalsDoNotRoundUpFromFloatError()
        {
            var run = new RunCoins(UpgradeMath.Factor(UpgradeKind.Tips, 1));
            int banked = 0;
            for (int i = 0; i < 10; i++)
                banked += run.Add(1);
            banked += run.Finish();
            Assert.AreEqual(11, banked);
        }

        [Test]
        public void FinishingTwiceAddsNothing()
        {
            var run = new RunCoins(1.5f);
            run.Add(1);
            Assert.AreEqual(1, run.Finish());
            Assert.AreEqual(0, run.Finish());
            Assert.AreEqual(0, run.Add(5));
            Assert.AreEqual(2, run.Banked);
        }

        [Test]
        public void FloatingTextValueIncludesTips()
        {
            Assert.AreEqual(3.3, new RunCoins(UpgradeMath.Factor(UpgradeKind.Tips, 1)).Value(3), 1e-4);
        }
    }
}
