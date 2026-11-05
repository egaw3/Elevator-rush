using System;
using System.Collections.Generic;
using UnityEngine;

namespace ElevatorRush
{
    /// <summary>
    /// Drives the cabin from input through <see cref="ElevatorMotion"/>, shows the dwell ring,
    /// and runs the passenger exchange when it docks.
    /// </summary>
    public sealed class ElevatorController : MonoBehaviour, IExchangeQuery
    {
        readonly List<Passenger> riders = new List<Passenger>();

        GameConfig config;
        InputReader input;
        Building building;
        Transform passengerRoot;
        ElevatorMotion motion;
        Transform cabin;
        Transform body;
        Transform cable;
        MaterialLibrary materials;
        AssetLibrary assets;
        float builtWidth;
        int builtLook = -1;
        DwellRing ring;

        // Idle mode (home tower): the cabin glides between floors on its own.
        int idleFloors;
        int idleTarget;
        float idleY;
        float idleVelocity;
        float idleWait;

        public ElevatorMotion Motion => motion;
        public Transform Cabin => cabin;
        public IReadOnlyList<Passenger> Riders => riders;
        /// <summary>When false the cabin behaves as if nobody is holding (it settles under gravity).</summary>
        public bool InputEnabled { get; set; }
        public bool IsIdling => idleFloors > 0;
        /// <summary>Cosmetic cabin look (home plan 6): 0 plain, 1 trim and lights, 2 glass and gold. Applied on AttachTo.</summary>
        public int CabinLook { get; set; }

        /// <summary>Raised when a rider steps out at its destination floor.</summary>
        public event Action<Passenger> Delivered;
        /// <summary>Raised on every dock with the floor and whether it was perfect.</summary>
        public event Action<int, bool> Docked;

        public void Init(GameConfig config, InputReader input, MaterialLibrary materials, AssetLibrary assets,
            Transform passengerRoot)
        {
            this.config = config;
            this.input = input;
            this.passengerRoot = passengerRoot;
            this.materials = materials;
            this.assets = assets;
            motion = new ElevatorMotion(config);
            BuildVisuals();
        }

        /// <summary>Moves the cabin into a (new) building at the ground floor, empty.</summary>
        public void AttachTo(Building target)
        {
            building = target;
            riders.Clear();
            idleFloors = 0;
            if (!Mathf.Approximately(builtWidth, config.cabinWidth) || builtLook != CabinLook)
                BuildBody();
            motion.SetFloors(building.FloorCount, building.FloorY(0));
            motion.Reset(building.FloorY(0));
            ring.SetProgress(0f);
            ApplyTransforms(motion.Y);
        }

        /// <summary>
        /// Lets the cabin move up and down on its own between the lowest <paramref name="floorCount"/>
        /// floors, pausing at each, ignoring input (home plan 3.2). Ends on the next AttachTo.
        /// </summary>
        public void StartIdle(int floorCount)
        {
            idleFloors = Mathf.Clamp(floorCount, 1, building.FloorCount);
            idleY = motion.Y;
            idleVelocity = 0f;
            idleTarget = 0;
            idleWait = config.idleCabinPause;
            InputEnabled = false;
            ring.SetProgress(0f);
        }

        /// <summary>Picks up a change in the building's floor count without moving the cabin.</summary>
        public void RefreshFloors()
        {
            motion.SetFloors(building.FloorCount, building.FloorY(0));
            ApplyTransforms(motion.Y);
        }

        public bool CanExchange(int floor)
        {
            return ExchangeRules.CanExchange(floor, riders, building.Floors[floor].Queue, config.cabinCapacity);
        }

        void Update()
        {
            if (building == null)
                return;
            if (idleFloors > 0)
            {
                UpdateIdle(Time.deltaTime);
                return;
            }

            var dockEvent = motion.Step(InputEnabled && input.IsHolding, Time.deltaTime, this);
            if (dockEvent == DockEvent.Docked)
                OnDocked(motion.DockedFloor, motion.LastDockWasPerfect);

            ring.SetProgress(motion.DwellProgress);
            ApplyTransforms(motion.Y);
        }

        void UpdateIdle(float dt)
        {
            float target = building.FloorY(idleTarget);
            idleY = Mathf.SmoothDamp(idleY, target, ref idleVelocity, config.idleCabinSmoothing, config.idleCabinSpeed, dt);
            if (Mathf.Abs(idleY - target) < 0.01f)
            {
                idleWait -= dt;
                if (idleWait <= 0f && idleFloors > 1)
                {
                    // Visit floors in order up, then back down to the ground.
                    idleTarget = idleTarget == 0 ? 1 : (idleTarget + 1) % idleFloors;
                    idleWait = config.idleCabinPause;
                }
            }
            ApplyTransforms(idleY);
        }

        void OnDocked(int floorIndex, bool perfect)
        {
            var floor = building.Floors[floorIndex];
            floor.Flash(perfect);

            // Riders for this floor exit first.
            int exited = 0;
            for (int i = 0; i < riders.Count; )
            {
                var rider = riders[i];
                if (rider.Destination != floorIndex)
                {
                    i++;
                    continue;
                }
                riders.RemoveAt(i);
                rider.Exit(passengerRoot, floor.DeliverPoint(exited++));
                Delivered?.Invoke(rider);
            }

            // Then the queue boards in order while it fits.
            int free = config.cabinCapacity - ExchangeRules.UsedSlots(riders);
            int boarding = ExchangeRules.CountBoarders(floor.Queue, free);
            for (int i = 0; i < boarding; i++)
            {
                var passenger = floor.Dequeue();
                passenger.Board(cabin);
                riders.Add(passenger);
            }

            LayoutRiders();
            UpdateGlow();
            Docked?.Invoke(floorIndex, perfect);
        }

        /// <summary>Riders stand side by side by slot, centered in the cabin, alternating back and front.</summary>
        void LayoutRiders()
        {
            int slot = 0;
            float center = ExchangeRules.UsedSlots(riders) * 0.5f;
            for (int i = 0; i < riders.Count; i++)
            {
                var rider = riders[i];
                float x = (slot + rider.Slots * 0.5f - center) * config.riderSpacing;
                float z = slot % 2 == 0 ? config.riderDepthStagger : -config.riderDepthStagger;
                rider.SetCabinSlot(new Vector3(x, config.cabinPanelThickness, z));
                slot += rider.Slots;
            }
        }

        void UpdateGlow()
        {
            for (int f = 0; f < building.FloorCount; f++)
            {
                bool wanted = false;
                for (int i = 0; i < riders.Count && !wanted; i++)
                    wanted = riders[i].Destination == f;
                building.Floors[f].SetGlow(wanted);
            }
        }

        void ApplyTransforms(float y)
        {
            cabin.position = new Vector3(building.ShaftX, y, 0f);

            float cabinTop = y + config.cabinHeight;
            float length = Mathf.Max(0f, building.CableAnchorY - cabinTop);
            cable.position = new Vector3(building.ShaftX, cabinTop + length * 0.5f, 0f);
            cable.localScale = new Vector3(config.cableWidth, length, config.cableWidth);
        }

        void BuildVisuals()
        {
            cabin = new GameObject("Cabin").transform;
            cabin.SetParent(transform, false);
            BuildBody();

            float h = config.cabinHeight;
            float d = config.roomDepth * config.cabinDepthFraction * 0.5f;
            var ringGo = new GameObject("DwellRing");
            ringGo.transform.SetParent(cabin, false);
            ringGo.transform.localPosition = new Vector3(0f, h * config.ringHeightFraction, -d - config.ringFrontOffset);
            ring = ringGo.AddComponent<DwellRing>();
            ring.Init(config, materials);

            cable = assets.CreatePrimitive(PrimitiveType.Cube, "Cable", transform,
                Vector3.zero, Vector3.one, materials.Get(config.cableColor)).transform;
        }

        /// <summary>
        /// The cabin's walls, rebuilt when Capacity widens it or the look changes. Open front
        /// (toward the camera) and open right side (toward the rooms) so riders stay visible.
        /// Look 1 adds gold trim and a strip light; look 2 makes the walls glass with gold frames.
        /// </summary>
        void BuildBody()
        {
            if (body != null)
            {
                body.gameObject.SetActive(false);
                Destroy(body.gameObject);
            }
            body = new GameObject("Body").transform;
            body.SetParent(cabin, false);
            builtWidth = config.cabinWidth;
            builtLook = CabinLook;

            float w = config.cabinWidth * 0.5f;
            float h = config.cabinHeight;
            float d = config.roomDepth * config.cabinDepthFraction * 0.5f;
            float p = config.cabinPanelThickness;
            bool glass = CabinLook >= 2;
            var wall = glass ? materials.GetShiny(config.cabinGlassColor) : materials.Get(config.cabinColor);
            var trim = glass ? materials.GetShiny(config.cabinTrimGold) : materials.Get(config.cabinTrimColor);

            assets.CreateBox("Floor", body, new Vector3(-w, 0f, -d), new Vector3(w, p, d), trim);
            assets.CreateBox("Roof", body, new Vector3(-w, h - p, -d), new Vector3(w, h, d), trim);
            assets.CreateBox("Back", body, new Vector3(-w, p, d - p), new Vector3(w, h - p, d), wall);
            assets.CreateBox("Left", body, new Vector3(-w, p, -d), new Vector3(-w + p, h - p, d), wall);
            assets.CreateBox("RightPostFront", body, new Vector3(w - p, p, -d), new Vector3(w, h - p, -d + p), trim);
            assets.CreateBox("RightPostBack", body, new Vector3(w - p, p, d - p), new Vector3(w, h - p, d), trim);
            assets.CreateBox("LeftPostFront", body, new Vector3(-w, p, -d), new Vector3(-w + p, h - p, -d + p), trim);

            if (CabinLook >= 1)
            {
                var gold = materials.GetShiny(config.cabinTrimGold);
                float t = config.cabinTrimThickness;
                // Gold rails on the back wall and around the open front, and a warm strip light under the roof.
                assets.CreateBox("TrimLow", body, new Vector3(-w, h * 0.35f, d - p - t * 0.5f), new Vector3(w, h * 0.35f + t, d - p), gold);
                assets.CreateBox("TrimHigh", body, new Vector3(-w, h * 0.75f, d - p - t * 0.5f), new Vector3(w, h * 0.75f + t, d - p), gold);
                assets.CreateBox("TrimFront", body, new Vector3(-w, h - p - t, -d - t * 0.5f), new Vector3(w, h - p, -d), gold);
                assets.CreateBox("Light", body, new Vector3(-w * 0.7f, h - p - config.cabinLightHeight, -d * 0.4f),
                    new Vector3(w * 0.7f, h - p, d * 0.4f), materials.GetUnlit(config.cabinLightColor));
            }
        }
    }
}
