using System;
using System.Collections.Generic;
using UnityEngine;

namespace ElevatorRush
{
    /// <summary>
    /// Spawns passengers on floors at a fixed interval with a seeded random, so a level plays the
    /// same way each attempt. Skips full floors. Owns the passenger pool.
    /// </summary>
    public sealed class PassengerSpawner : MonoBehaviour
    {
        readonly List<Passenger> active = new List<Passenger>();

        GameConfig config;
        Pool<Passenger> pool;
        Building building;
        LevelDefinition level;
        System.Random random;
        float timer;
        int created;

        /// <summary>Spawning and patience both run only while this is true.</summary>
        public bool Running { get; set; }
        Passenger preview;
        readonly List<Passenger> idle = new List<Passenger>();
        /// <summary>Multiplies every new passenger's patience (the Comfort upgrade). 1 by default.</summary>
        public float PatienceFactor { get; set; } = 1f;
        /// <summary>The equipped character skin for a destination color (home plan 7). Null wears Default.</summary>
        public Func<int, string> SkinOf { get; set; }
        public bool PatienceActive => Running;

        /// <summary>Raised when a waiting passenger runs out of patience and leaves.</summary>
        public event Action<Passenger> PassengerLeft;

        public void Init(GameConfig config, MaterialLibrary materials, AssetLibrary assets, SpriteFactory sprites, CharacterSkinner skinner)
        {
            this.config = config;
            // Alternate the two character models as the pool grows.
            pool = new Pool<Passenger>(() => Passenger.Create(config, materials, assets, sprites, skinner, this, created++), transform);
            pool.Prewarm(config.passengerPrewarm);
        }

        /// <summary>Clears the last run and gets ready to spawn for a level. Call Running = true to start.</summary>
        public void Prepare(Building target, LevelDefinition definition)
        {
            Clear();
            building = target;
            level = definition;
            random = new System.Random(definition.seed);
            timer = config.firstSpawnDelay;
            Running = false;
        }

        /// <summary>
        /// A single passenger standing still at a point, wearing a skin, for the Characters
        /// panel preview (home plan 7). Patience does not run while the spawner is stopped.
        /// </summary>
        public Passenger ShowPreview(int destination, string skin, Vector3 position)
        {
            if (preview == null)
            {
                preview = pool.Get();
                preview.transform.SetParent(transform, false);
            }
            preview.Spawn(0, destination, PassengerKind.Normal, position, float.MaxValue);
            preview.SetSkin(skin);
            return preview;
        }

        /// <summary>
        /// A passenger that only stands and walks where it is told (Home wanderers, H7): no
        /// bubble, no patience, not part of any queue. Returned to the pool by <see cref="Clear"/>.
        /// </summary>
        public Passenger SpawnIdle(int destination, string skin, Vector3 position)
        {
            var p = pool.Get();
            p.transform.SetParent(transform, false);
            p.Spawn(0, destination, PassengerKind.Normal, position, float.MaxValue);
            p.SetSkin(skin);
            p.SetPatienceVisible(false);
            p.SetBubbleVisible(false);
            idle.Add(p);
            return p;
        }

        /// <summary>Returns the idle (wandering) passengers to the pool.</summary>
        public void ClearIdle()
        {
            for (int i = idle.Count - 1; i >= 0; i--)
            {
                idle[i].SetBubbleVisible(true);
                pool.Release(idle[i]);
            }
            idle.Clear();
        }

        public void HidePreview()
        {
            if (preview == null)
                return;
            pool.Release(preview);
            preview = null;
        }

        /// <summary>Returns every passenger to the pool and empties the queues.</summary>
        public void Clear()
        {
            HidePreview();
            ClearIdle();
            for (int i = active.Count - 1; i >= 0; i--)
                pool.Release(active[i]);
            active.Clear();
            if (building != null)
                for (int i = 0; i < building.FloorCount; i++)
                    building.Floors[i].ClearQueue();
        }

        void Update()
        {
            if (!Running || building == null)
                return;

            timer -= Time.deltaTime;
            if (timer > 0f)
                return;
            // When every floor is full, wait and spawn as soon as a spot frees up.
            if (TrySpawn())
                timer += level.spawnInterval;
            else
                timer = 0f;
        }

        bool TrySpawn()
        {
            int floors = building.FloorCount;
            bool groundOnly = level.groundOnly;
            int firstOrigin = groundOnly ? 1 : 0;

            int candidates = 0;
            for (int i = firstOrigin; i < floors; i++)
                if (!building.Floors[i].IsFull)
                    candidates++;
            if (candidates == 0 || floors < 2)
                return false;

            int pick = random.Next(candidates);
            int origin = firstOrigin;
            for (int i = firstOrigin; i < floors; i++)
            {
                if (building.Floors[i].IsFull)
                    continue;
                if (pick-- == 0)
                {
                    origin = i;
                    break;
                }
            }

            int destination = 0;
            if (!groundOnly)
            {
                // Any floor except the origin.
                destination = random.Next(floors - 1);
                if (destination >= origin)
                    destination++;
            }

            var kind = PassengerKindRules.Pick(random.NextDouble(), level.vipChance, level.bigChance);

            var floor = building.Floors[origin];
            var passenger = pool.Get();
            passenger.transform.SetParent(transform, false);
            passenger.Spawn(origin, destination, kind, floor.SpawnPoint, level.patience * PatienceFactor);
            passenger.SetSkin(SkinOf != null ? SkinOf(destination) : Catalog.DefaultSkin);
            active.Add(passenger);
            floor.Enqueue(passenger);
            return true;
        }

        public void OnPatienceOut(Passenger passenger)
        {
            var floor = building.Floors[passenger.Origin];
            floor.Remove(passenger);
            passenger.WalkTo(floor.LeavePoint);
            PassengerLeft?.Invoke(passenger);
        }

        public void Despawn(Passenger passenger)
        {
            active.Remove(passenger);
            pool.Release(passenger);
        }
    }
}
