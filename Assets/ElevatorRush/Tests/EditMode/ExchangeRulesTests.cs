using System.Collections.Generic;
using NUnit.Framework;

namespace ElevatorRush.Tests
{
    public class ExchangeRulesTests
    {
        const int Capacity = 4;

        sealed class P : IPassengerInfo
        {
            public P(int destination, int slots = 1) { Destination = destination; Slots = slots; }
            public int Destination { get; }
            public int Slots { get; }
        }

        static List<IPassengerInfo> L(params P[] items) => new List<IPassengerInfo>(items);

        [Test]
        public void NothingToDoMeansNoExchange()
        {
            Assert.IsFalse(ExchangeRules.CanExchange(1, L(new P(2)), L(), Capacity));
        }

        [Test]
        public void RiderForThisFloorAllowsExchange()
        {
            Assert.IsTrue(ExchangeRules.CanExchange(2, L(new P(2)), L(), Capacity));
        }

        [Test]
        public void WaitingPassengerThatFitsAllowsExchange()
        {
            Assert.IsTrue(ExchangeRules.CanExchange(0, L(new P(2)), L(new P(3)), Capacity));
        }

        [Test]
        public void FullCabinWithNobodyExitingBlocksExchange()
        {
            var riders = L(new P(2), new P(2), new P(3), new P(3));
            Assert.IsFalse(ExchangeRules.CanExchange(1, riders, L(new P(0)), Capacity));
        }

        [Test]
        public void FullCabinWithSomeoneExitingAllowsExchange()
        {
            var riders = L(new P(1), new P(2), new P(3), new P(3));
            Assert.IsTrue(ExchangeRules.CanExchange(1, riders, L(new P(0)), Capacity));
        }

        [Test]
        public void BoardersStopWhenCabinIsFull()
        {
            var queue = L(new P(0), new P(0), new P(0));
            Assert.AreEqual(2, ExchangeRules.CountBoarders(queue, 2));
            Assert.AreEqual(0, ExchangeRules.CountBoarders(queue, 0));
        }

        [Test]
        public void BigPassengerAtFrontBlocksQueue()
        {
            var queue = L(new P(0, 2), new P(0), new P(0));
            Assert.AreEqual(0, ExchangeRules.CountBoarders(queue, 1));
            Assert.AreEqual(1, ExchangeRules.CountBoarders(queue, 2));
            Assert.AreEqual(2, ExchangeRules.CountBoarders(queue, 3));
            Assert.IsFalse(ExchangeRules.CanExchange(1, L(new P(2), new P(2), new P(3)), queue, Capacity));
        }

        [Test]
        public void UsedAndExitingSlotsCountBigPassengers()
        {
            var riders = L(new P(1, 2), new P(2), new P(1));
            Assert.AreEqual(4, ExchangeRules.UsedSlots(riders));
            Assert.AreEqual(3, ExchangeRules.ExitingSlots(1, riders));
        }
    }
}
