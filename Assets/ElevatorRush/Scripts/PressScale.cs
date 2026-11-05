using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ElevatorRush
{
    /// <summary>
    /// Button press animation (H7): squeezes a little while held and springs back with a small
    /// bounce on release. Unscaled time, so it works on paused and end screens. Added to every
    /// button made by <see cref="UiBuilder"/>.
    /// </summary>
    public sealed class PressScale : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        const float PressedScale = 0.92f;
        const float Stiffness = 380f;
        const float Damping = 18f;

        Selectable selectable;
        float target = 1f;
        float scale = 1f;
        float velocity;

        void Awake()
        {
            selectable = GetComponent<Selectable>();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (selectable == null || selectable.IsInteractable())
                target = PressedScale;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            target = 1f;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            target = 1f;
        }

        void OnDisable()
        {
            target = 1f;
            scale = 1f;
            velocity = 0f;
            transform.localScale = Vector3.one;
        }

        void Update()
        {
            if (Mathf.Approximately(scale, target) && Mathf.Abs(velocity) < 1e-3f)
                return;
            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
            // A damped spring: quick squeeze, a little overshoot on release.
            velocity += ((target - scale) * Stiffness - velocity * Damping) * dt;
            scale += velocity * dt;
            if (Mathf.Abs(target - scale) < 1e-4f && Mathf.Abs(velocity) < 1e-3f)
            {
                scale = target;
                velocity = 0f;
            }
            transform.localScale = Vector3.one * scale;
        }
    }
}
