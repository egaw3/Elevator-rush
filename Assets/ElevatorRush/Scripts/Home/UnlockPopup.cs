using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ElevatorRush
{
    /// <summary>
    /// The "Unlocked!" popup (home plan 2 and H7): when Home opens after crossing unlock
    /// thresholds, each new item pops up in turn over a dimmer that blocks everything else,
    /// with a sound, a haptic pulse and a confetti burst. Each id is reported once as shown.
    /// </summary>
    public sealed class UnlockPopup : MonoBehaviour
    {
        const float CardWidth = 820f;
        const float CardHeight = 860f;
        const float TitleY = -110f;
        const float IconY = -330f;
        const float IconSize = 260f;
        const float NameY = -540f;
        const float HintY = -620f;
        const float ButtonBottom = 60f;
        static readonly Vector2 ButtonSize = new Vector2(420f, 140f);
        const int TitleFont = 96;
        const int NameFont = 64;
        const int HintFont = 40;
        const int ButtonFont = 64;
        const float PopTime = 0.3f;
        const float PopScale = 0.6f;
        const float IconSpin = 8f;
        const float InputLock = 0.35f;

        GameConfig config;
        SpriteFactory sprites;
        Fx fx;
        Sfx sfx;
        RectTransform root;
        RectTransform card;
        Image icon;
        Image iconBack;
        Text nameText;
        Text hintText;
        Text counter;
        Button button;
        readonly Queue<string> queue = new Queue<string>();
        float shownAt;
        string current;

        public bool IsShowing => root.gameObject.activeSelf;

        /// <summary>Raised when an item's popup is dismissed; the caller records it as shown and saves.</summary>
        public event Action<string> Shown;

        public void Init(GameConfig config, Font font, SpriteFactory sprites, RectTransform uiRoot, Fx fx, Sfx sfx)
        {
            this.config = config;
            this.sprites = sprites;
            this.fx = fx;
            this.sfx = sfx;
            var center = new Vector2(0.5f, 0.5f);
            var top = new Vector2(0.5f, 1f);

            var dim = UiBuilder.Image(uiRoot, "UnlockPopup", null, config.uiDim, false);
            dim.raycastTarget = true;
            root = UiBuilder.Stretch(dim.rectTransform);

            var cardImage = UiBuilder.Image(root, "Card", sprites.Panel, config.uiCard, true);
            card = UiBuilder.Place(cardImage.rectTransform, center, center, Vector2.zero, new Vector2(CardWidth, CardHeight));
            var title = UiBuilder.Label(card, "Title", font, "Unlocked!", TitleFont, config.uiLevelCircle, TextAnchor.MiddleCenter, Color.clear);
            UiBuilder.Place(title.rectTransform, top, center, new Vector2(0f, TitleY), Vector2.zero);

            iconBack = UiBuilder.Image(card, "IconBack", sprites.Star, config.uiStarOn, false);
            UiBuilder.Place(iconBack.rectTransform, top, center, new Vector2(0f, IconY), Vector2.one * IconSize * 1.35f);
            icon = UiBuilder.Image(card, "Icon", sprites.Circle, Color.white, false);
            UiBuilder.Place(icon.rectTransform, top, center, new Vector2(0f, IconY), Vector2.one * IconSize * 0.62f);

            nameText = UiBuilder.Label(card, "Name", font, "", NameFont, config.uiTextDark, TextAnchor.MiddleCenter, Color.clear);
            UiBuilder.Place(nameText.rectTransform, top, center, new Vector2(0f, NameY), Vector2.zero);
            hintText = UiBuilder.Label(card, "Hint", font, "", HintFont, config.uiNextCircle, TextAnchor.MiddleCenter, Color.clear);
            UiBuilder.Place(hintText.rectTransform, top, center, new Vector2(0f, HintY), Vector2.zero);
            counter = UiBuilder.Label(card, "Counter", font, "", HintFont, config.uiNextCircle, TextAnchor.MiddleRight, Color.clear);
            UiBuilder.Place(counter.rectTransform, Vector2.one, Vector2.one, new Vector2(-40f, -40f), new Vector2(200f, HintFont));

            button = UiBuilder.Button(card, "Ok", font, sprites.Panel, "Nice!", ButtonFont, config.uiPlayButton, config.uiText, config.uiShadow, Next);
            UiBuilder.Place((RectTransform)button.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, ButtonBottom), ButtonSize);

            root.gameObject.SetActive(false);
        }

        /// <summary>Queues these unlock ids and shows the first one at once.</summary>
        public void Show(IList<string> ids)
        {
            foreach (var id in ids)
                queue.Enqueue(id);
            if (!IsShowing)
                ShowNext();
        }

        public void Hide()
        {
            queue.Clear();
            current = null;
            root.gameObject.SetActive(false);
        }

        void ShowNext()
        {
            if (queue.Count == 0)
            {
                root.gameObject.SetActive(false);
                return;
            }
            current = queue.Dequeue();
            icon.sprite = UnlockTeaser.IconOf(sprites, current);
            icon.color = UnlockTeaser.ColorOf(config, current);
            nameText.text = UnlockTeaser.LabelOf(current);
            hintText.text = UnlockPopups.HintOf(current);
            counter.text = queue.Count > 0 ? "+" + queue.Count + " more" : "";
            root.gameObject.SetActive(true);
            root.SetAsLastSibling();
            shownAt = Time.unscaledTime;
            card.localScale = Vector3.one * PopScale;

            sfx.Play(SfxKey.Unlock);
            Fx.Haptic();
            fx.ConfettiBurst(icon.rectTransform.position, config.purchaseConfetti, config.purchaseConfettiSpeed);
        }

        void Next()
        {
            // A held finger from the level cannot skip a popup the moment it appears.
            if (Time.unscaledTime - shownAt < InputLock || current == null)
                return;
            sfx.Play(SfxKey.Button);
            var id = current;
            current = null;
            Shown?.Invoke(id);
            ShowNext();
        }

        void Update()
        {
            if (!IsShowing)
                return;
            float t = Mathf.Clamp01((Time.unscaledTime - shownAt) / PopTime);
            // Ease out to full size with a small overshoot.
            float ease = 1f - (1f - t) * (1f - t);
            card.localScale = Vector3.one * (Mathf.Lerp(PopScale, 1f, ease) + 0.08f * Mathf.Sin(t * Mathf.PI));
            iconBack.rectTransform.localRotation = Quaternion.Euler(0f, 0f, Time.unscaledTime * IconSpin);
        }
    }
}
