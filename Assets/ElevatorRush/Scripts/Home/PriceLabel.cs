using UnityEngine;
using UnityEngine.UI;

namespace ElevatorRush
{
    /// <summary>
    /// A centered row that shows a price on a button or card: coins, gems, the player's coins
    /// plus the gems for the rest (monetization plan 2.2), a padlock with text, or plain text.
    /// A layout group keeps each icon right next to its number at any length.
    /// </summary>
    public sealed class PriceLabel
    {
        readonly RectTransform row;
        readonly Image padlock;
        readonly Image coin;
        readonly Text coinText;
        readonly Text plus;
        readonly Image gem;
        readonly Text gemText;
        readonly Text text;

        public RectTransform Root => row;

        public PriceLabel(RectTransform parent, GameConfig config, Font font, SpriteFactory sprites, int fontSize, Color textColor, Color shadow)
        {
            row = UiBuilder.Stretch(UiBuilder.Rect("Price", parent));
            var layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.spacing = fontSize * 0.15f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            float iconSize = fontSize * 1.1f;
            padlock = Icon("Lock", sprites.Lock, textColor, iconSize);
            coin = Icon("Coin", sprites.Coin, config.uiCoin, iconSize);
            coinText = Label(font, fontSize, textColor, shadow);
            plus = Label(font, fontSize, textColor, shadow);
            plus.text = "+";
            gem = Icon("Gem", sprites.Gem, config.uiGem, iconSize);
            gemText = Label(font, fontSize, textColor, shadow);
            text = Label(font, fontSize, textColor, shadow);
        }

        Image Icon(string name, Sprite sprite, Color color, float size)
        {
            var image = UiBuilder.Image(row, name, sprite, color, false);
            var element = image.gameObject.AddComponent<LayoutElement>();
            element.preferredWidth = size;
            element.preferredHeight = size;
            return image;
        }

        Text Label(Font font, int size, Color color, Color shadow)
        {
            return UiBuilder.Label(row, "Text", font, "", size, color, TextAnchor.MiddleCenter, shadow);
        }

        public void ShowCoins(int coins) => Set(false, coins, -1, null);

        public void ShowGems(int gems) => Set(false, -1, gems, null);

        /// <summary>A shortfall: the coins the player has plus the gems for the rest (gems alone with no coins).</summary>
        public void ShowCoinsAndGems(int coins, int gems) => Set(false, coins > 0 ? coins : -1, gems, null);

        public void ShowText(string label) => Set(false, -1, -1, label);

        public void ShowLocked(string label) => Set(true, -1, -1, label);

        public void SetVisible(bool visible) => row.gameObject.SetActive(visible);

        void Set(bool locked, int coins, int gems, string label)
        {
            row.gameObject.SetActive(true);
            padlock.gameObject.SetActive(locked);
            coin.gameObject.SetActive(coins >= 0);
            coinText.gameObject.SetActive(coins >= 0);
            coinText.text = coins.ToString();
            plus.gameObject.SetActive(coins >= 0 && gems >= 0);
            gem.gameObject.SetActive(gems >= 0);
            gemText.gameObject.SetActive(gems >= 0);
            gemText.text = gems.ToString();
            text.gameObject.SetActive(label != null);
            text.text = label ?? "";
            LayoutRebuilder.MarkLayoutForRebuild(row);
        }
    }
}
