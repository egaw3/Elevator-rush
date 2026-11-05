using NUnit.Framework;

namespace ElevatorRush.Tests
{
    public class ElevatorMotionTests
    {
        const float Dt = 1f / 60f;
        const float Eps = 1e-4f;
        const int Floors = 4;

        sealed class Exchange : IExchangeQuery
        {
            public bool[] Allowed = new bool[64];
            public bool CanExchange(int floor) => floor < Allowed.Length && Allowed[floor];
            public void AllowAll() { for (int i = 0; i < Allowed.Length; i++) Allowed[i] = true; }
        }

        GameConfig config;
        ElevatorMotion motion;
        Exchange exchange;

        [SetUp]
        public void SetUp()
        {
            config = new GameConfig();
            motion = new ElevatorMotion(config);
            motion.SetFloors(Floors);
            motion.Reset(0f);
            exchange = new Exchange();
        }

        /// <summary>Runs whole frames and returns the first dock event's frame index, or -1.</summary>
        int Run(bool holding, float seconds)
        {
            int steps = (int)(seconds / Dt);
            for (int i = 0; i < steps; i++)
                if (motion.Step(holding, Dt, exchange) == DockEvent.Docked)
                    return i;
            return -1;
        }

        // ---- Movement ----

        [Test]
        public void RisesWhileHolding()
        {
            motion.Step(true, Dt, exchange);
            Assert.Greater(motion.Y, 0f);
            Assert.AreEqual(config.riseAccel * Dt, motion.Velocity, Eps);
        }

        [Test]
        public void FallsWhenReleased()
        {
            Run(true, 0.4f);
            float peakY = motion.Y;
            Run(false, 1.5f);
            Assert.Less(motion.Y, peakY);
        }

        [Test]
        public void RiseSpeedIsCapped()
        {
            motion.SetFloors(500);
            Run(true, 3f);
            Assert.AreEqual(config.maxRiseSpeed, motion.Velocity, Eps);
        }

        [Test]
        public void FallSpeedIsCapped()
        {
            motion.SetFloors(500);
            motion.Reset(motion.MaxY);
            Run(false, 3f);
            Assert.AreEqual(-config.maxFallSpeed, motion.Velocity, Eps);
        }

        [Test]
        public void RestsOnGround()
        {
            Run(false, 1f);
            Assert.AreEqual(0f, motion.Y, Eps);
            Assert.AreEqual(0f, motion.Velocity, Eps);
            Assert.IsTrue(motion.AtGround);
        }

        [Test]
        public void ClampsAtTop()
        {
            Run(true, 5f);
            Assert.AreEqual(motion.FloorY(Floors - 1), motion.Y, Eps);
            Assert.AreEqual(0f, motion.Velocity, Eps);
            Assert.IsTrue(motion.AtTop);
        }

        [Test]
        public void FallingIsFasterThanRising()
        {
            Assert.Greater(config.maxFallSpeed, config.maxRiseSpeed);
        }

        [Test]
        public void ZeroDeltaTimeDoesNothing()
        {
            motion.Step(true, 0f, exchange);
            Assert.AreEqual(0f, motion.Y);
            Assert.AreEqual(0f, motion.Velocity);
        }

        // ---- Docking ----

        [Test]
        public void DocksAfterDwellTime()
        {
            exchange.Allowed[1] = true;
            // Outside the perfect range, inside the snap range, kept near rest by tapping.
            motion.SetState(motion.FloorY(1) - 0.2f, 0f);
            int frame = -1;
            for (int i = 0; i < 120 && frame < 0; i++)
                if (motion.Step(i % 2 == 0, Dt, exchange) == DockEvent.Docked)
                    frame = i;

            Assert.GreaterOrEqual(frame, 0, "never docked");
            Assert.GreaterOrEqual((frame + 1) * Dt, config.dwellTime - Eps);
            Assert.IsFalse(motion.LastDockWasPerfect);
            Assert.AreEqual(1, motion.DockedFloor);
            Assert.AreEqual(motion.FloorY(1), motion.Y, Eps);
            Assert.AreEqual(0f, motion.Velocity);
        }

        [Test]
        public void DocksInstantlyInsidePerfectRange()
        {
            exchange.Allowed[2] = true;
            motion.SetState(motion.FloorY(2) + 0.02f, 0f);
            Assert.AreEqual(DockEvent.Docked, motion.Step(false, Dt, exchange));
            Assert.IsTrue(motion.LastDockWasPerfect);
            Assert.AreEqual(motion.FloorY(2), motion.Y, Eps);
        }

        [Test]
        public void NeverDocksAboveSnapSpeed()
        {
            exchange.AllowAll();
            exchange.Allowed[0] = false;
            exchange.Allowed[Floors - 1] = false;
            Assert.AreEqual(-1, Run(true, 3f), "docked while rising past floors");
            Assert.IsTrue(motion.AtTop);
            Assert.AreEqual(-1, Run(false, 3f), "docked while falling past floors");
        }

        [Test]
        public void NeverDocksWithoutExchange()
        {
            motion.SetState(motion.FloorY(0), 0f);
            Assert.AreEqual(-1, Run(false, 2f));
            Assert.AreEqual(ElevatorMotion.NoFloor, motion.HoverFloor);
        }

        [Test]
        public void InputIgnoredDuringExchange()
        {
            exchange.Allowed[1] = true;
            motion.SetState(motion.FloorY(1), 0f);
            Assert.AreEqual(DockEvent.Docked, motion.Step(false, Dt, exchange));
            exchange.Allowed[1] = false;

            int frames = (int)(config.exchangeTime / Dt) - 1;
            for (int i = 0; i < frames; i++)
                motion.Step(true, Dt, exchange);
            Assert.IsTrue(motion.IsExchanging);
            Assert.AreEqual(motion.FloorY(1), motion.Y, Eps);

            DockEvent last = DockEvent.None;
            for (int i = 0; i < 5 && last != DockEvent.ExchangeFinished; i++)
                last = motion.Step(true, Dt, exchange);
            Assert.AreEqual(DockEvent.ExchangeFinished, last);
            motion.Step(true, Dt, exchange);
            Assert.Greater(motion.Y, motion.FloorY(1));
        }

        [Test]
        public void DoesNotRedockWhenNothingLeftToDo()
        {
            exchange.Allowed[0] = true;
            Assert.AreEqual(0, Run(false, 0.1f));
            exchange.Allowed[0] = false;
            Run(false, config.exchangeTime + 0.1f);
            Assert.AreEqual(-1, Run(false, 2f));
        }

        [Test]
        public void RedockAtSameFloorIsNotPerfect()
        {
            exchange.Allowed[0] = true;
            Assert.AreEqual(0, Run(false, 0.1f));
            Assert.IsTrue(motion.LastDockWasPerfect);
            DockEvent e = DockEvent.None;
            for (int i = 0; i < 120 && e != DockEvent.ExchangeFinished; i++)
                e = motion.Step(false, Dt, exchange);
            Assert.AreEqual(DockEvent.ExchangeFinished, e);
            // Something new to do while the cabin is still at the floor: dwell again, no perfect.
            Assert.GreaterOrEqual(Run(false, 1f), 0);
            Assert.IsFalse(motion.LastDockWasPerfect);
        }
    }
}
