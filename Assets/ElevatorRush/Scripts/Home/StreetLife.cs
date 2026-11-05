using System.Collections.Generic;
using UnityEngine;

namespace ElevatorRush
{
    /// <summary>
    /// A busy street in front of the home tower: cars driving past in both lanes and people
    /// strolling along both sidewalks. Home only; levels have no traffic at all. Cars are a small
    /// reused set of models; pedestrians are pooled passengers without bubbles, wearing the
    /// skins and colors of the player's characters.
    /// </summary>
    public sealed class StreetLife : MonoBehaviour
    {
        const string CarFolder = "cars/";
        static readonly string[] CarKeys = { "taxi", "sedan", "police", "van", "suv", "hatchback-sports", "delivery" };

        struct Car
        {
            public Transform transform;
            public int lane;
            public float speed;
            public float waitUntil;
            public bool driving;
        }

        struct Walker
        {
            public Passenger passenger;
            public int side;
            public int direction;
        }

        readonly List<Car> cars = new List<Car>();
        readonly List<Walker> walkers = new List<Walker>();
        readonly System.Random random = new System.Random(11);

        GameConfig config;
        AssetLibrary assets;
        MaterialLibrary materials;
        PassengerSpawner spawner;
        Building building;
        Transform carRoot;
        bool running;
        bool pedestriansAllowed = true;
        float center;
        float[] laneZ;
        float[] sidewalkZ;

        /// <summary>The save to read the player's characters from; set by GameManager.</summary>
        public System.Func<SaveData> Save { get; set; }

        public void Init(GameConfig config, AssetLibrary assets, MaterialLibrary materials, PassengerSpawner spawner, Building building)
        {
            this.config = config;
            this.assets = assets;
            this.materials = materials;
            this.spawner = spawner;
            this.building = building;
            carRoot = new GameObject("Traffic").transform;
            carRoot.SetParent(transform, false);
            for (int i = 0; i < config.streetCars; i++)
                cars.Add(new Car { transform = CreateCar(CarKeys[i % CarKeys.Length]) });
        }

        Transform CreateCar(string key)
        {
            var prefab = assets.Model(CarFolder + key);
            GameObject car;
            if (prefab != null)
            {
                car = assets.SpawnModel(prefab, key, carRoot, config.carHeight);
            }
            else
            {
                // Fallback: a body and a cabin.
                car = new GameObject(key + " (fallback)");
                car.transform.SetParent(carRoot, false);
                var body = materials.Get(Color.HSVToRGB((float)random.NextDouble(), 0.4f, 0.85f));
                assets.CreateBox("Body", car.transform, new Vector3(-0.45f, 0.15f, -0.9f), new Vector3(0.45f, 0.5f, 0.9f), body);
                assets.CreateBox("Cabin", car.transform, new Vector3(-0.4f, 0.5f, -0.5f), new Vector3(0.4f, 0.8f, 0.4f), materials.Get(new Color(0.75f, 0.85f, 0.95f)));
            }
            foreach (var r in car.GetComponentsInChildren<Renderer>())
                r.receiveShadows = false;
            car.SetActive(false);
            return car.transform;
        }

        /// <summary>Starts the street around the current tower (Home opened or rebuilt).</summary>
        public void Begin()
        {
            running = true;
            float front = building.FrontZ;
            float sidewalkFront = front - config.sidewalkDepth;
            float roadFront = sidewalkFront - config.roadDepth;
            // Near lane drives right, far lane drives left; walkers keep to the middle of each sidewalk.
            laneZ = new[] { sidewalkFront - config.roadDepth * 0.27f, roadFront + config.roadDepth * 0.27f };
            sidewalkZ = new[] { (front + sidewalkFront) * 0.5f, roadFront - config.sidewalkDepth * 0.5f };
            center = (building.LeftX + building.RightX) * 0.5f;
            for (int i = 0; i < cars.Count; i++)
            {
                var c = cars[i];
                c.lane = i % 2;
                c.driving = false;
                c.waitUntil = Time.time + i * 1.3f;
                c.transform.gameObject.SetActive(false);
                cars[i] = c;
            }
        }

        /// <summary>Stops the street (a level starts): cars park out of sight, pedestrians leave.</summary>
        public void End()
        {
            running = false;
            foreach (var c in cars)
                c.transform.gameObject.SetActive(false);
            walkers.Clear();
        }

        /// <summary>Keeps pedestrians away for a while (the Characters preview stands alone).</summary>
        public void SetPedestriansAllowed(bool allowed)
        {
            pedestriansAllowed = allowed;
            if (!allowed)
                walkers.Clear();
        }

        void Update()
        {
            if (!running)
                return;
            UpdateCars(Time.deltaTime);
            UpdateWalkers();
        }

        void UpdateCars(float dt)
        {
            float half = config.trafficHalfLength;
            for (int i = 0; i < cars.Count; i++)
            {
                var c = cars[i];
                int dir = c.lane == 0 ? 1 : -1;
                if (!c.driving)
                {
                    if (Time.time < c.waitUntil)
                        continue;
                    c.driving = true;
                    c.speed = Mathf.Lerp(config.carSpeed.x, config.carSpeed.y, (float)random.NextDouble());
                    c.transform.localPosition = new Vector3(center - dir * half, config.groundTopY, laneZ[c.lane]);
                    // Kenney cars face +Z; turn them to face along the lane.
                    c.transform.localRotation = Quaternion.Euler(0f, dir * 90f, 0f);
                    c.transform.gameObject.SetActive(true);
                }
                var p = c.transform.localPosition;
                p.x += dir * c.speed * dt;
                c.transform.localPosition = p;
                if (dir * (p.x - center) > half)
                {
                    c.driving = false;
                    c.waitUntil = Time.time + Mathf.Lerp(config.carGap.x, config.carGap.y, (float)random.NextDouble());
                    c.transform.gameObject.SetActive(false);
                    // Alternate lanes so both directions stay busy.
                    c.lane = 1 - c.lane;
                }
                cars[i] = c;
            }
        }

        void UpdateWalkers()
        {
            // Pedestrians the spawner took back (tower rebuilt) are gone; others reaching an end turn around.
            for (int i = walkers.Count - 1; i >= 0; i--)
            {
                var w = walkers[i];
                if (w.passenger == null || !w.passenger.gameObject.activeInHierarchy)
                {
                    walkers.RemoveAt(i);
                    continue;
                }
                if (w.passenger.IsMoving)
                    continue;
                w.direction = -w.direction;
                w.passenger.WalkTo(new Vector3(center + w.direction * config.pedestrianHalfLength, 0f, sidewalkZ[w.side]));
                walkers[i] = w;
            }
            if (!pedestriansAllowed || Save == null)
                return;
            var data = Save();
            int colors = UnlockSchedule.UnlockedFloorCount(data);
            while (walkers.Count < config.streetPedestrians)
            {
                int side = walkers.Count % 2;
                int direction = random.Next(2) == 0 ? 1 : -1;
                float start = center + Mathf.Lerp(-config.pedestrianHalfLength, config.pedestrianHalfLength, (float)random.NextDouble());
                int color = random.Next(colors);
                var p = spawner.SpawnIdle(color, data.EquippedSkin(color), new Vector3(start, 0f, sidewalkZ[side]));
                p.WalkTo(new Vector3(center + direction * config.pedestrianHalfLength, 0f, sidewalkZ[side]));
                walkers.Add(new Walker { passenger = p, side = side, direction = direction });
            }
        }
    }
}
