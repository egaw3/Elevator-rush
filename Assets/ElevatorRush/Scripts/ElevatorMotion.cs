using UnityEngine;

namespace ElevatorRush
{
    /// <summary>Answers whether docking at a floor would let anyone on or off.</summary>
    public interface IExchangeQuery
    {
        bool CanExchange(int floor);
    }

    public enum DockEvent
    {
        None,
        Docked,
        ExchangeFinished
    }

    /// <summary>
    /// Pure movement and docking math for the cabin (plan 5.2 and 5.3). No scene dependencies.
    /// Floors sit at MinY + index * floorHeight.
    /// </summary>
    public sealed class ElevatorMotion
    {
        public const int NoFloor = -1;

        readonly GameConfig config;
        int hoverFloor = NoFloor;
        float exchangeTimer;

        public float Y { get; private set; }
        public float Velocity { get; private set; }
        public float MinY { get; private set; }
        public float MaxY { get; private set; }
        public int FloorCount { get; private set; }

        /// <summary>Floor the cabin is hovering at with an exchange possible, or NoFloor.</summary>
        public int HoverFloor => hoverFloor;
        /// <summary>Dwell progress from 0 to 1 while hovering.</summary>
        public float DwellProgress { get; private set; }
        public bool IsExchanging { get; private set; }
        /// <summary>Floor of the current or most recent dock.</summary>
        public int DockedFloor { get; private set; } = NoFloor;
        public bool LastDockWasPerfect { get; private set; }

        public bool AtGround => Y <= MinY;
        public bool AtTop => Y >= MaxY;

        public ElevatorMotion(GameConfig config)
        {
            this.config = config;
        }

        public void SetFloors(int floorCount, float groundY = 0f)
        {
            FloorCount = Mathf.Max(1, floorCount);
            MinY = groundY;
            MaxY = FloorY(FloorCount - 1);
            Clamp();
        }

        public float FloorY(int floor) => MinY + floor * config.floorHeight;

        public int NearestFloor(float y)
        {
            return Mathf.Clamp(Mathf.RoundToInt((y - MinY) / config.floorHeight), 0, FloorCount - 1);
        }

        public void Reset(float y)
        {
            SetState(y, 0f);
            IsExchanging = false;
            DockedFloor = NoFloor;
            LastDockWasPerfect = false;
        }

        /// <summary>Places the cabin directly. For setup, tests and debugging.</summary>
        public void SetState(float y, float velocity)
        {
            Y = y;
            Velocity = velocity;
            hoverFloor = NoFloor;
            DwellProgress = 0f;
            Clamp();
        }

        public DockEvent Step(bool holding, float deltaTime, IExchangeQuery exchange)
        {
            if (deltaTime <= 0f)
                return DockEvent.None;

            if (IsExchanging)
                return StepExchange(deltaTime);

            if (holding)
                Velocity = Mathf.Min(Velocity + config.riseAccel * deltaTime, config.maxRiseSpeed);
            else
                Velocity = Mathf.Max(Velocity - config.gravity * deltaTime, -config.maxFallSpeed);

            Y += Velocity * deltaTime;
            Clamp();

            return UpdateHover(deltaTime, exchange);
        }

        DockEvent StepExchange(float deltaTime)
        {
            exchangeTimer -= deltaTime;
            if (exchangeTimer > 0f)
                return DockEvent.None;

            IsExchanging = false;
            // Still at this floor, so a later dwell here is not a fresh (perfect) arrival.
            hoverFloor = DockedFloor;
            DwellProgress = 0f;
            return DockEvent.ExchangeFinished;
        }

        DockEvent UpdateHover(float deltaTime, IExchangeQuery exchange)
        {
            int nearest = NearestFloor(Y);
            float distance = Mathf.Abs(Y - FloorY(nearest));
            bool hovering = distance < config.snapRange
                && Mathf.Abs(Velocity) < config.snapSpeed
                && exchange != null
                && exchange.CanExchange(nearest);

            if (!hovering)
            {
                hoverFloor = NoFloor;
                DwellProgress = 0f;
                return DockEvent.None;
            }

            if (hoverFloor != nearest)
            {
                hoverFloor = nearest;
                DwellProgress = 0f;
                if (distance < config.perfectRange)
                    return Dock(nearest, true);
                return DockEvent.None;
            }

            DwellProgress = config.dwellTime > 0f ? DwellProgress + deltaTime / config.dwellTime : 1f;
            if (DwellProgress >= 1f)
                return Dock(nearest, false);
            return DockEvent.None;
        }

        DockEvent Dock(int floor, bool perfect)
        {
            Y = FloorY(floor);
            Velocity = 0f;
            IsExchanging = true;
            exchangeTimer = config.exchangeTime;
            DockedFloor = floor;
            LastDockWasPerfect = perfect;
            DwellProgress = 0f;
            return DockEvent.Docked;
        }

        void Clamp()
        {
            if (Y <= MinY)
            {
                Y = MinY;
                if (Velocity < 0f)
                    Velocity = 0f;
            }
            if (Y >= MaxY)
            {
                Y = MaxY;
                if (Velocity > 0f)
                    Velocity = 0f;
            }
        }
    }
}
