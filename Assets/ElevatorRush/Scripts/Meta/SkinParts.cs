using UnityEngine;

namespace ElevatorRush
{
    /// <summary>One built character skin on one passenger: the head and body accessory groups.</summary>
    public sealed class SkinParts
    {
        public GameObject head;
        public GameObject body;
        /// <summary>How far the skin's hat rises above the head; the VIP crown sits on top of it.</summary>
        public float hatHeight;

        public void SetActive(bool active)
        {
            if (head != null)
                head.SetActive(active);
            if (body != null)
                body.SetActive(active);
        }
    }
}
