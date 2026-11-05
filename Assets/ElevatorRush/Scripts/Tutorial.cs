using UnityEngine;
using UnityEngine.UI;

namespace ElevatorRush
{
    /// <summary>
    /// Hint overlays (plan 5.7): level 1 shows an animated hand and "Hold to rise, release to fall"
    /// until the first dock; level 4 shows "Match the color" until the first delivery to a floor
    /// other than the ground floor. Shown again on every attempt of those levels.
    /// </summary>
    public sealed class Tutorial : MonoBehaviour
    {
        enum Hint
        {
            None,
            Hold,
            MatchColor
        }

        // Layout in reference pixels, inside the band below the building.
        const float HandY = 300f;
        const float HandSize = 200f;
        const float RingSize = 150f;
        const float TextY = 120f;
        const int TextFont = 60;
        const float RingStartScale = 0.3f;
        const float RingEndScale = 1.3f;

        GameConfig config;
        RectTransform root;
        RectTransform handGroup;
        Image ring;
        Text text;
        Hint hint;

        public void Init(GameConfig config, Font font, SpriteFactory sprites, RectTransform parent)
        {
            this.config = config;
            var bottom = new Vector2(0.5f, 0f);
            var center = new Vector2(0.5f, 0.5f);

            root = UiBuilder.Stretch(UiBuilder.Rect("Tutorial", parent));

            handGroup = UiBuilder.Place(UiBuilder.Rect("Hand", root), bottom, center, new Vector2(0f, HandY), Vector2.one * HandSize);
            var tip = SpriteFactory.HandFingertip * (HandSize * 0.5f);
            ring = UiBuilder.Image(handGroup, "Ring", sprites.IconSprite(FloorIcon.Ring), config.uiText, false);
            UiBuilder.Place(ring.rectTransform, center, center, tip, Vector2.one * RingSize);
            var outline = UiBuilder.Image(handGroup, "Outline", sprites.HandOutline, config.uiTextDark, false);
            UiBuilder.Stretch(outline.rectTransform);
            var hand = UiBuilder.Image(handGroup, "Fill", sprites.Hand, config.uiText, false);
            UiBuilder.Stretch(hand.rectTransform);

            text = UiBuilder.Label(root, "Text", font, "", TextFont, config.uiText, TextAnchor.MiddleCenter, config.uiShadow);
            UiBuilder.Place(text.rectTransform, bottom, center, new Vector2(0f, TextY), Vector2.zero);

            Hide();
        }

        /// <summary>Picks the hint for a level; it appears when play starts.</summary>
        public void Prepare(int level)
        {
            hint = level == LevelCatalog.HoldHintLevel ? Hint.Hold
                : level == LevelCatalog.ColorHintLevel ? Hint.MatchColor
                : Hint.None;
            Hide();
        }

        public void OnPlayStarted()
        {
            if (hint == Hint.None)
                return;
            handGroup.gameObject.SetActive(hint == Hint.Hold);
            text.text = hint == Hint.Hold ? "Hold to rise, release to fall" : "Match the color";
            root.gameObject.SetActive(true);
        }

        public void OnDocked()
        {
            if (hint == Hint.Hold)
                Finish();
        }

        public void OnDelivered(int destination)
        {
            if (hint == Hint.MatchColor && destination != 0)
                Finish();
        }

        void Finish()
        {
            hint = Hint.None;
            Hide();
        }

        public void Hide()
        {
            root.gameObject.SetActive(false);
        }

        void Update()
        {
            if (!root.gameObject.activeSelf)
                return;

            float phase = Mathf.Repeat(Time.unscaledTime / config.tutorialPressPeriod, 1f);
            bool pressed = phase < 0.5f;
            handGroup.localScale = Vector3.one * (pressed ? config.tutorialPressScale : 1f);

            // While pressed, a ring grows from the fingertip and fades.
            float t = pressed ? phase * 2f : 0f;
            ring.enabled = pressed;
            ring.rectTransform.localScale = Vector3.one * Mathf.Lerp(RingStartScale, RingEndScale, t);
            var c = config.uiText;
            c.a = 1f - t;
            ring.color = c;

            var tc = config.uiText;
            tc.a = 0.7f + 0.3f * Mathf.Sin(Time.unscaledTime * config.holdHintPulseSpeed);
            text.color = tc;
        }
    }
}
