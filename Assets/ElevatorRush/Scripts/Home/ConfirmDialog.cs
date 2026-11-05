using System;
using UnityEngine;
using UnityEngine.UI;

namespace ElevatorRush
{
    /// <summary>
    /// A modal card over everything (monetization plan 2.4). <see cref="Ask"/> confirms a gem
    /// spend, showing the item, its price and the gem balance afterwards, with Cancel and a
    /// confirm button; <see cref="Tell"/> shows a message with OK. Tapping outside cancels.
    /// Animates on unscaled time.
    /// </summary>
    public sealed class ConfirmDialog : MonoBehaviour
    {
        const float CardWidth = 820f;
        const float CardHeight = 760f;
        const float TitleY = -100f;
        const float NameY = -220f;
        const float PriceY = -330f;
        const float PriceHeight = 90f;
        const float AfterY = -440f;
        const float MessageY = -330f;
        const float MessageWidth = 700f;
        const float ButtonBottom = 60f;
        const float ButtonGap = 30f;
        static readonly Vector2 ButtonSize = new Vector2(340f, 140f);
        const int TitleFont = 76;
        const int NameFont = 58;
        const int PriceFont = 64;
        const int AfterFont = 42;
        const int MessageFont = 46;
        const int ButtonFont = 60;
        const float PopTime = 0.22f;
        const float PopScale = 0.7f;

        GameConfig config;
        RectTransform root;
        RectTransform card;
        Text title;
        Text itemName;
        PriceLabel price;
        PriceLabel after;
        Text message;
        Button cancel;
        Button confirm;
        Text confirmLabel;
        Button ok;
        Action onConfirm;
        float shownAt;

        public bool IsOpen => root.gameObject.activeSelf;

        /// <summary>Raised whenever a dialog button is pressed (for the click sound).</summary>
        public event Action ButtonPressed;

        public void Init(GameConfig config, Font font, SpriteFactory sprites, RectTransform uiRoot)
        {
            this.config = config;
            var center = new Vector2(0.5f, 0.5f);
            var top = new Vector2(0.5f, 1f);
            var bottom = new Vector2(0.5f, 0f);

            var dim = UiBuilder.Button(uiRoot, "ConfirmDialog", null, null, "", 1, config.uiDim, Color.clear, Color.clear, () => Press(Close));
            Destroy(dim.GetComponentInChildren<Text>().gameObject);
            Destroy(dim.GetComponent<PressScale>());
            dim.transition = Selectable.Transition.None;
            dim.GetComponent<Image>().type = Image.Type.Simple;
            root = UiBuilder.Stretch((RectTransform)dim.transform);

            var cardImage = UiBuilder.Image(root, "Card", sprites.Panel, config.uiCard, true);
            cardImage.raycastTarget = true;
            card = UiBuilder.Place(cardImage.rectTransform, center, center, Vector2.zero, new Vector2(CardWidth, CardHeight));
            title = UiBuilder.Label(card, "Title", font, "", TitleFont, config.uiTextDark, TextAnchor.MiddleCenter, Color.clear);
            UiBuilder.Place(title.rectTransform, top, center, new Vector2(0f, TitleY), Vector2.zero);
            itemName = UiBuilder.Label(card, "Item", font, "", NameFont, config.uiTextDark, TextAnchor.MiddleCenter, Color.clear);
            UiBuilder.Place(itemName.rectTransform, top, center, new Vector2(0f, NameY), Vector2.zero);

            var priceRow = UiBuilder.Place(UiBuilder.Rect("PriceRow", card), top, center, new Vector2(0f, PriceY), new Vector2(CardWidth, PriceHeight));
            price = new PriceLabel(priceRow, config, font, sprites, PriceFont, config.uiTextDark, Color.clear);
            var afterRow = UiBuilder.Place(UiBuilder.Rect("AfterRow", card), top, center, new Vector2(0f, AfterY), new Vector2(CardWidth, PriceHeight));
            after = new PriceLabel(afterRow, config, font, sprites, AfterFont, config.uiNextCircle, Color.clear);

            message = UiBuilder.Label(card, "Message", font, "", MessageFont, config.uiTextDark, TextAnchor.MiddleCenter, Color.clear);
            message.horizontalOverflow = HorizontalWrapMode.Wrap;
            UiBuilder.Place(message.rectTransform, top, center, new Vector2(0f, MessageY), new Vector2(MessageWidth, MessageFont * 4f));

            float half = (ButtonSize.x + ButtonGap) * 0.5f;
            cancel = UiBuilder.Button(card, "Cancel", font, sprites.Panel, "Cancel", ButtonFont, config.uiNextCircle, config.uiText, config.uiShadow, () => Press(Close));
            UiBuilder.Place((RectTransform)cancel.transform, bottom, bottom, new Vector2(-half, ButtonBottom), ButtonSize);
            confirm = UiBuilder.Button(card, "Confirm", font, sprites.Panel, "", ButtonFont, config.uiGemButton, config.uiText, config.uiShadow, () => Press(Confirm));
            UiBuilder.Place((RectTransform)confirm.transform, bottom, bottom, new Vector2(half, ButtonBottom), ButtonSize);
            confirmLabel = confirm.GetComponentInChildren<Text>();
            ok = UiBuilder.Button(card, "Ok", font, sprites.Panel, "OK", ButtonFont, config.uiPlayButton, config.uiText, config.uiShadow, () => Press(Close));
            UiBuilder.Place((RectTransform)ok.transform, bottom, bottom, new Vector2(0f, ButtonBottom), ButtonSize);

            root.gameObject.SetActive(false);
        }

        /// <summary>
        /// Confirms a gem spend: the item, its price (coins, if any are spent too, plus gems) and the
        /// gem balance afterwards. <paramref name="onConfirm"/> runs only if the player confirms.
        /// </summary>
        public void Ask(string heading, string item, int coins, int gems, int gemsAfter, string confirmText, Action onConfirm)
        {
            this.onConfirm = onConfirm;
            title.text = heading;
            itemName.text = item;
            if (coins > 0)
                price.ShowCoinsAndGems(coins, gems);
            else
                price.ShowGems(gems);
            after.ShowText("Gems after: " + gemsAfter);
            confirmLabel.text = confirmText;
            SetMode(true);
        }

        /// <summary>A message with an OK button.</summary>
        public void Tell(string heading, string text)
        {
            onConfirm = null;
            title.text = heading;
            message.text = text;
            SetMode(false);
        }

        void SetMode(bool asking)
        {
            itemName.gameObject.SetActive(asking);
            price.SetVisible(asking);
            after.SetVisible(asking);
            cancel.gameObject.SetActive(asking);
            confirm.gameObject.SetActive(asking);
            message.gameObject.SetActive(!asking);
            ok.gameObject.SetActive(!asking);
            root.gameObject.SetActive(true);
            root.SetAsLastSibling();
            shownAt = Time.unscaledTime;
            card.localScale = Vector3.one * PopScale;
        }

        public void Close()
        {
            onConfirm = null;
            root.gameObject.SetActive(false);
        }

        void Confirm()
        {
            var action = onConfirm;
            Close();
            action?.Invoke();
        }

        void Press(Action action)
        {
            // A tap that opened the dialog cannot also answer it.
            if (Time.unscaledTime - shownAt < config.confirmDialogInputLock)
                return;
            ButtonPressed?.Invoke();
            action();
        }

        void Update()
        {
            if (!IsOpen)
                return;
            float t = Mathf.Clamp01((Time.unscaledTime - shownAt) / PopTime);
            float ease = 1f - (1f - t) * (1f - t);
            card.localScale = Vector3.one * Mathf.Lerp(PopScale, 1f, ease);
        }
    }
}
