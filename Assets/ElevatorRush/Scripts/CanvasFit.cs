using UnityEngine;
using UnityEngine.UI;

namespace ElevatorRush
{
    /// <summary>
    /// Tall phones (9:16 and taller) scale the UI by width so every phone gets the same
    /// 1080-wide layout. Anything wider than the 9:16 reference (tablets at 3:4, a landscape
    /// Editor window) scales by height instead, so the UI never takes over the screen's height.
    /// </summary>
    [RequireComponent(typeof(CanvasScaler))]
    public sealed class CanvasFit : MonoBehaviour
    {
        const float MatchHeight = 1f;

        CanvasScaler scaler;
        GameConfig config;
        bool? byHeight;

        static GameConfig shared;

        public static bool IsLandscape => Screen.width > Screen.height;

        /// <summary>True when the UI scales by screen height (screen wider than the reference aspect).</summary>
        public static bool MatchesHeight
        {
            get
            {
                if (Screen.height <= 0)
                    return false;
                var reference = shared != null ? shared.uiReferenceResolution : new Vector2(1080f, 1920f);
                return (float)Screen.width / Screen.height > reference.x / reference.y + 0.001f;
            }
        }

        /// <summary>Pixels per UI reference pixel for the current screen, matching the canvas scaler.</summary>
        public static float UiScale(GameConfig config)
        {
            return MatchesHeight ? Screen.height / config.uiReferenceResolution.y : Screen.width / config.uiReferenceResolution.x;
        }

        public void Init(GameConfig config)
        {
            this.config = config;
            shared = config;
            scaler = GetComponent<CanvasScaler>();
            Apply();
        }

        void Update()
        {
            if (byHeight != MatchesHeight)
                Apply();
        }

        void Apply()
        {
            if (scaler == null)
                return;
            byHeight = MatchesHeight;
            scaler.matchWidthOrHeight = MatchesHeight ? MatchHeight : config.uiMatchWidthOrHeight;
        }
    }
}
