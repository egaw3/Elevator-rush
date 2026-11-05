using System;
using UnityEngine;
using UnityEngine.UI;

namespace ElevatorRush
{
    /// <summary>
    /// A panel that slides up from the bottom of the screen over a dimmer (home plan 5.3).
    /// Tapping the dimmer or the close button closes it. Content goes under <see cref="Content"/>.
    /// Animates on unscaled time.
    /// </summary>
    public sealed class BottomSheet : MonoBehaviour
    {
        const float SlideTime = 0.22f;
        const float TitleY = -70f;
        const int TitleFont = 66;
        const float CloseSize = 100f;
        const float CloseMargin = 30f;

        RectTransform panel;
        Text title;
        float height;
        float shownAt;
        bool closing;

        public RectTransform Content { get; private set; }
        public bool IsOpen => gameObject.activeSelf && !closing;
        public float Height => height;

        /// <summary>Raised when the sheet starts closing.</summary>
        public event Action Closed;

        public static BottomSheet Create(RectTransform uiRoot, string name, GameConfig config, Font font, SpriteFactory sprites,
            float height, Action onClick)
        {
            var root = UiBuilder.Stretch(UiBuilder.Rect(name, uiRoot));
            var sheet = root.gameObject.AddComponent<BottomSheet>();
            sheet.height = height;

            var dim = UiBuilder.Image(root, "Dimmer", null, config.uiDim, false);
            dim.raycastTarget = true;
            UiBuilder.Stretch(dim.rectTransform);
            var dimButton = dim.gameObject.AddComponent<Button>();
            dimButton.transition = Selectable.Transition.None;
            dimButton.onClick.AddListener(() => { onClick(); sheet.Close(); });

            var card = UiBuilder.Image(root, "Panel", sprites.Panel, config.uiCard, true);
            card.raycastTarget = true;
            sheet.panel = card.rectTransform;
            sheet.panel.anchorMin = new Vector2(0f, 0f);
            sheet.panel.anchorMax = new Vector2(1f, 0f);
            sheet.panel.pivot = new Vector2(0.5f, 0f);
            sheet.panel.sizeDelta = new Vector2(0f, height);

            sheet.title = UiBuilder.Label(sheet.panel, "Title", font, "", TitleFont, config.uiTextDark, TextAnchor.MiddleCenter, Color.clear);
            UiBuilder.Place(sheet.title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, TitleY), Vector2.zero);

            var close = UiBuilder.Button(sheet.panel, "Close", font, sprites.Circle, "X", TitleFont / 2 + 10, config.uiNextCircle, config.uiText, Color.clear,
                () => { onClick(); sheet.Close(); });
            close.GetComponent<Image>().type = Image.Type.Simple;
            UiBuilder.Place((RectTransform)close.transform, Vector2.one, Vector2.one, new Vector2(-CloseMargin, -CloseMargin), Vector2.one * CloseSize);

            sheet.Content = UiBuilder.Stretch(UiBuilder.Rect("Content", sheet.panel));
            sheet.Content.offsetMax = new Vector2(0f, TitleY * 2f);
            root.gameObject.SetActive(false);
            return sheet;
        }

        public void Open(string text)
        {
            title.text = text;
            closing = false;
            shownAt = Time.unscaledTime;
            gameObject.SetActive(true);
            Apply(1f);
        }

        public void SetTitle(string text)
        {
            title.text = text;
        }

        public void Close()
        {
            if (!gameObject.activeSelf || closing)
                return;
            closing = true;
            shownAt = Time.unscaledTime;
            Closed?.Invoke();
        }

        /// <summary>Hides at once, without the slide.</summary>
        public void Hide()
        {
            closing = false;
            gameObject.SetActive(false);
        }

        void Update()
        {
            float t = Mathf.Clamp01((Time.unscaledTime - shownAt) / SlideTime);
            if (closing)
            {
                Apply(t);
                if (t >= 1f)
                    Hide();
            }
            else
            {
                Apply(1f - t);
            }
        }

        /// <summary>0 is fully shown, 1 fully below the screen.</summary>
        void Apply(float hidden)
        {
            float e = hidden * hidden;
            panel.anchoredPosition = new Vector2(0f, -height * e);
        }
    }
}
