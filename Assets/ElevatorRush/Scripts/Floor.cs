using System.Collections.Generic;
using UnityEngine;

namespace ElevatorRush
{
    /// <summary>
    /// One floor of the building: destination color and icon, the waiting queue, glow and dock flash.
    /// Passengers queue along X on the slab at Z = 0, lined up toward the shaft.
    /// </summary>
    public sealed class Floor : MonoBehaviour
    {
        readonly List<Passenger> queue = new List<Passenger>();
        readonly List<Renderer> tintedRenderers = new List<Renderer>();
        readonly List<Material> normalMaterials = new List<Material>();
        readonly List<Material> glowMaterials = new List<Material>();

        GameConfig config;
        MaterialLibrary materials;
        float roomLeftX;
        float roomRightX;
        float flashTimer;
        bool glowing;

        public int Index { get; private set; }
        public float WorldY { get; private set; }
        public Color DestinationColor { get; private set; }
        public FloorIcon Icon { get; private set; }
        public IReadOnlyList<Passenger> Queue => queue;
        public bool IsFull => queue.Count >= config.maxQueuePerFloor;
        public bool IsGround => Index == 0;

        public void Init(int index, float worldY, float roomLeftX, float roomRightX,
            GameConfig config, MaterialLibrary materials)
        {
            this.config = config;
            this.materials = materials;
            this.roomLeftX = roomLeftX;
            this.roomRightX = roomRightX;
            Index = index;
            WorldY = worldY;
            DestinationColor = FloorPalette.ColorOf(index);
            Icon = FloorPalette.IconOf(index);
        }

        /// <summary>Registers a part that is tinted with the floor color and takes part in glow and flash.</summary>
        public void AddTinted(GameObject part, Color color)
        {
            var r = part.GetComponent<Renderer>();
            tintedRenderers.Add(r);
            normalMaterials.Add(r.sharedMaterial);
            glowMaterials.Add(materials.Get(Color.Lerp(color, DestinationColor, config.glowAmount)));
        }

        // ---- Queue ----

        public Vector3 QueueSpot(int position)
        {
            return new Vector3(roomLeftX + config.queueFirstOffset + position * config.queueSpacing, WorldY, 0f);
        }

        public Vector3 SpawnPoint => new Vector3(roomRightX - config.spawnInset, WorldY, 0f);

        /// <summary>Where an impatient passenger stomps off to, on the far right of the room.</summary>
        public Vector3 LeavePoint => new Vector3(roomRightX - config.leaveInset, WorldY, 0f);

        /// <summary>Where the k-th passenger delivered in one exchange walks to before fading out.</summary>
        public Vector3 DeliverPoint(int k)
        {
            if (IsGround)
            {
                // Out through the open front onto the street.
                float x = roomLeftX + config.queueFirstOffset + k * config.deliverSpread;
                return new Vector3(x, WorldY, -config.roomDepth * 0.5f - config.frontDoorDistance);
            }
            return new Vector3(roomRightX - config.deliverInset - k * config.deliverSpread, WorldY, config.deliverDepth);
        }

        public void Enqueue(Passenger passenger)
        {
            queue.Add(passenger);
            passenger.WalkTo(QueueSpot(queue.Count - 1));
        }

        public Passenger Dequeue()
        {
            var front = queue[0];
            queue.RemoveAt(0);
            for (int i = 0; i < queue.Count; i++)
                queue[i].WalkTo(QueueSpot(i));
            return front;
        }

        /// <summary>Removes a passenger from anywhere in the queue; the rest close the gap.</summary>
        public void Remove(Passenger passenger)
        {
            int index = queue.IndexOf(passenger);
            if (index < 0)
                return;
            queue.RemoveAt(index);
            for (int i = index; i < queue.Count; i++)
                queue[i].WalkTo(QueueSpot(i));
        }

        public void ClearQueue()
        {
            queue.Clear();
        }

        // ---- Glow and flash ----

        public void SetGlow(bool on)
        {
            if (glowing == on)
                return;
            glowing = on;
            if (flashTimer <= 0f)
                ApplyMaterials();
        }

        /// <summary>Briefly paints the tinted parts in the full destination color (white on a perfect dock).</summary>
        public void Flash(bool perfect)
        {
            var flash = materials.Get(perfect ? config.perfectFlashColor : DestinationColor);
            for (int i = 0; i < tintedRenderers.Count; i++)
                tintedRenderers[i].sharedMaterial = flash;
            flashTimer = perfect ? config.perfectFlashTime : config.dockFlashTime;
        }

        void Update()
        {
            if (flashTimer <= 0f)
                return;
            flashTimer -= Time.deltaTime;
            if (flashTimer <= 0f)
                ApplyMaterials();
        }

        void ApplyMaterials()
        {
            var source = glowing ? glowMaterials : normalMaterials;
            for (int i = 0; i < tintedRenderers.Count; i++)
                tintedRenderers[i].sharedMaterial = source[i];
        }
    }
}
