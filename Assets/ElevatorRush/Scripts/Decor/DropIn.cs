using UnityEngine;

namespace ElevatorRush
{
    /// <summary>
    /// A just-bought decor item falls into place with a bounce (home plan 5.3), then removes
    /// itself. Unscaled time, so it plays the same however the game is paced.
    /// </summary>
    public sealed class DropIn : MonoBehaviour
    {
        Vector3 rest;
        float height;
        float duration;
        float t;

        public static void Play(GameObject target, float height, float duration)
        {
            var drop = target.AddComponent<DropIn>();
            drop.rest = target.transform.localPosition;
            drop.height = height;
            drop.duration = Mathf.Max(0.01f, duration);
            drop.Apply(0f);
        }

        void Update()
        {
            t += Time.unscaledDeltaTime / duration;
            Apply(Mathf.Clamp01(t));
            if (t >= 1f)
                Destroy(this);
        }

        void Apply(float u)
        {
            transform.localPosition = rest + Vector3.up * height * (1f - Bounce(u));
        }

        /// <summary>Standard ease-out bounce, 0 to 1.</summary>
        static float Bounce(float u)
        {
            const float n = 7.5625f;
            const float d = 2.75f;
            if (u < 1f / d)
                return n * u * u;
            if (u < 2f / d)
                return n * (u -= 1.5f / d) * u + 0.75f;
            if (u < 2.5f / d)
                return n * (u -= 2.25f / d) * u + 0.9375f;
            return n * (u -= 2.625f / d) * u + 0.984375f;
        }
    }
}
