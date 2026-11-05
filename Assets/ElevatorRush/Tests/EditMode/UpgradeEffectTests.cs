using NUnit.Framework;
using UnityEngine;

namespace ElevatorRush.Tests
{
    /// <summary>Each upgrade has a measurable effect on play, and no upgrades change nothing (home plan H4).</summary>
    public class UpgradeEffectTests
    {
        const float Dt = 1f / 60f;
        const int MaxFrames = 6000;

        sealed class AllowAll : IExchangeQuery
        {
            public bool CanExchange(int floor) => true;
        }

        sealed class AllowNone : IExchangeQuery
        {
            public bool CanExchange(int floor) => false;
        }

        /// <summary>A shared config with the given tiers applied, the way GameManager does it.</summary>
        static GameConfig Upgraded(int motor, int doors, int capacity)
        {
            var shared = new GameConfig();
            UpgradeMath.Apply(new GameConfig(), motor, doors, 0, 0, capacity).ApplyTo(shared);
            return shared;
        }

        static int FramesToReach(GameConfig config, float height)
        {
            var motion = new ElevatorMotion(config);
            motion.SetFloors(4);
            motion.Reset(0f);
            var none = new AllowNone();
            for (int i = 0; i < MaxFrames; i++)
            {
                motion.Step(true, Dt, none);
                if (motion.Y >= height)
                    return i;
            }
            return MaxFrames;
        }

        /// <summary>Frames from resting at the ground floor until the exchange there is over.</summary>
        static int FramesToFinishExchange(GameConfig config)
        {
            var motion = new ElevatorMotion(config);
            motion.SetFloors(4);
            motion.Reset(0f);
            var all = new AllowAll();
            bool docked = false;
            for (int i = 0; i < MaxFrames; i++)
            {
                docked |= motion.Step(false, Dt, all) == DockEvent.Docked;
                if (docked && !motion.IsExchanging)
                    return i;
            }
            return MaxFrames;
        }

        [Test]
        public void NoUpgradesLeaveEveryTuningValueUnchanged()
        {
            var shared = new GameConfig();
            UpgradeMath.Apply(new GameConfig(), TestSaves.With(0)).ApplyTo(shared);
            Assert.AreEqual(JsonUtility.ToJson(new GameConfig()), JsonUtility.ToJson(shared));
        }

        [Test]
        public void MotorReachesFloorsSooner()
        {
            var config = new GameConfig();
            float floor2 = config.floorHeight * 2f;
            int baseFrames = FramesToReach(config, floor2);
            int upgraded = FramesToReach(Upgraded(5, 0, 0), floor2);
            Assert.Less(baseFrames, MaxFrames);
            Assert.Less(upgraded, baseFrames);
        }

        [Test]
        public void DoorsDockAndExchangeFaster()
        {
            int baseFrames = FramesToFinishExchange(new GameConfig());
            int upgraded = FramesToFinishExchange(Upgraded(0, 5, 0));
            Assert.Less(baseFrames, MaxFrames);
            Assert.Less(upgraded, baseFrames);
        }

        [Test]
        public void CapacityLetsMoreRidersBoard()
        {
            var config = Upgraded(0, 0, 2);
            Assert.AreEqual(6, config.cabinCapacity);
            Assert.Greater(config.cabinWidth, new GameConfig().cabinWidth);
            Assert.Greater(config.shaftWidth, new GameConfig().shaftWidth);
            // Six riders side by side fit inside the wider cabin.
            Assert.LessOrEqual(6 * config.riderSpacing, config.cabinWidth - 2f * config.cabinPanelThickness);
        }

        [Test]
        public void ComfortAndTipsComeThroughTheRunValues()
        {
            var run = UpgradeMath.Apply(new GameConfig(), 0, 0, 3, 2, 0);
            Assert.AreEqual(1.18f, run.patienceFactor, 1e-5f);
            Assert.AreEqual(1.2f, run.coinFactor, 1e-5f);
        }

        [TestCase(0, 0)]
        [TestCase(5, 0)]
        [TestCase(6, 1)]
        [TestCase(13, 1)]
        [TestCase(14, 2)]
        [TestCase(22, 2)]
        public void CabinLookFollowsTotalTiers(int tiers, int look)
        {
            Assert.AreEqual(look, UpgradeMath.CabinLook(tiers));
        }
    }
}
