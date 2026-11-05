using UnityEngine;

namespace ElevatorRush
{
    /// <summary>
    /// Fits a RectTransform inside Screen.safeArea (notches, rounded corners, home indicator) and
    /// re-fits when the safe area or screen size changes. The camera also reads the insets so the
    /// building never sits under a notch.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class SafeArea : MonoBehaviour
    {
        /// <summary>Test hook: a fake safe area (in pixels) for trying notches on desktop.</summary>
        public static Rect? Override;

        RectTransform rect;
        Rect applied;
        int width;
        int height;

        public static Rect Current => Override ?? Screen.safeArea;

        /// <summary>Fraction of the screen height hidden at the top (notch, status bar).</summary>
        public static float TopInset => Screen.height > 0 ? Mathf.Clamp01(1f - Current.yMax / Screen.height) : 0f;

        /// <summary>Fraction of the screen height hidden at the bottom (home indicator).</summary>
        public static float BottomInset => Screen.height > 0 ? Mathf.Clamp01(Current.yMin / Screen.height) : 0f;

        void Awake()
        {
            rect = (RectTransform)transform;
            Apply();
        }

        void Update()
        {
            if (Current != applied || Screen.width != width || Screen.height != height)
                Apply();
        }

        void Apply()
        {
            applied = Current;
            width = Screen.width;
            height = Screen.height;
            if (width <= 0 || height <= 0)
                return;
            rect.anchorMin = new Vector2(applied.xMin / width, applied.yMin / height);
            rect.anchorMax = new Vector2(applied.xMax / width, applied.yMax / height);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
