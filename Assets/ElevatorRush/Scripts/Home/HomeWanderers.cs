using System.Collections.Generic;
using UnityEngine;

namespace ElevatorRush
{
    /// <summary>
    /// A few passengers strolling around the furnished floors of the home tower (H7): one on a
    /// floor with at least half its slots filled, two on a complete floor. They walk between
    /// spots in the front half of the room (in front of the furniture), pause, and walk again.
    /// They wear the equipped skins and have no bubble or patience.
    /// </summary>
    public sealed class HomeWanderers : MonoBehaviour
    {
        const int MaxWanderers = 8;
        const float EdgeInset = 0.7f;
        const float FrontZ = -0.55f;
        const float BackZ = 0.15f;
        const float MinPause = 1.2f;
        const float MaxPause = 4f;

        struct Walker
        {
            public Passenger passenger;
            public int floor;
            public float waitUntil;
        }

        readonly List<Walker> walkers = new List<Walker>();
        PassengerSpawner spawner;
        Building building;
        GameConfig config;
        System.Random random = new System.Random(7);

        public int Count => walkers.Count;

        public void Init(GameConfig config, PassengerSpawner spawner, Building building)
        {
            this.config = config;
            this.spawner = spawner;
            this.building = building;
        }

        /// <summary>Spawns wanderers for this save on the tower just built. Call after the tower is (re)built.</summary>
        public void Populate(SaveData save, int unlockedFloors)
        {
            walkers.Clear();
            int colors = UnlockSchedule.UnlockedFloorCount(save);
            for (int f = 0; f < unlockedFloors && walkers.Count < MaxWanderers; f++)
            {
                int filled = TowerProgress.FilledSlots(save, f);
                int count = filled == Catalog.SlotsPerFloor ? 2 : filled * 2 >= Catalog.SlotsPerFloor ? 1 : 0;
                for (int i = 0; i < count && walkers.Count < MaxWanderers; i++)
                {
                    int color = random.Next(colors);
                    var p = spawner.SpawnIdle(color, save.EquippedSkin(color), Spot(f));
                    walkers.Add(new Walker { passenger = p, floor = f, waitUntil = Time.time + Pause() });
                }
            }
        }

        /// <summary>Forgets the wanderers; the spawner's Clear returns them to the pool.</summary>
        public void Clear()
        {
            walkers.Clear();
        }

        /// <summary>Sends the wanderers away at once (e.g. so the Characters preview stands alone).</summary>
        public void Remove()
        {
            walkers.Clear();
            spawner.ClearIdle();
        }

        Vector3 Spot(int floor)
        {
            float x = Mathf.Lerp(building.RoomLeftX + EdgeInset, building.RoomRightX - EdgeInset, (float)random.NextDouble());
            float z = Mathf.Lerp(FrontZ, BackZ, (float)random.NextDouble());
            return new Vector3(x, building.FloorY(floor), z);
        }

        float Pause() => Mathf.Lerp(MinPause, MaxPause, (float)random.NextDouble());

        void Update()
        {
            float now = Time.time;
            for (int i = 0; i < walkers.Count; i++)
            {
                var w = walkers[i];
                // A pooled passenger taken back by the spawner (tower rebuilt) is no longer ours.
                if (w.passenger == null || !w.passenger.gameObject.activeInHierarchy)
                {
                    walkers.RemoveAt(i--);
                    continue;
                }
                if (w.passenger.IsMoving || now < w.waitUntil)
                    continue;
                var target = Spot(w.floor);
                float walk = Vector3.Distance(w.passenger.transform.localPosition, target) / config.walkSpeed;
                w.passenger.WalkTo(target);
                w.waitUntil = now + walk + Pause();
                walkers[i] = w;
            }
        }
    }
}
