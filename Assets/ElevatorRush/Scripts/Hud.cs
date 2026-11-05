using UnityEngine;
using UnityEngine.UI;

namespace ElevatorRush
{
    /// <summary>
    /// In-game HUD: level bar (level number, delivery progress, next level), lives, coins and combo.
    /// In Endless mode the bar shows the score and best score instead. Texts change only when
    /// their value does.
    /// </summary>
    public sealed class Hud : MonoBehaviour
    {
        // Layout in reference pixels (1080 x 1920 portrait).
        const float TopRowY = -110f;
        const float SecondRowY = -225f;
        const float SideMargin = 60f;
        const float BarWidth = 560f;
        const float BarHeight = 56f;
        const float BarPadding = 8f;
        const float CircleSize = 124f;
        const float CircleGap = 26f;
        const float HeartSize = 72f;
        const float HeartSpacing = 82f;
        const float CoinSize = 72f;
        const int NumberFont = 62;
        const int ProgressFont = 38;
        const int CoinFont = 60;
        const int ComboFont = 56;

        GameConfig config;
        Text levelText;
        Text nextText;
        Text progressText;
        RectTransform barFill;
        Image[] hearts;
        Text coinText;
        Text comboText;
        bool endless;
        RectTransform items;

        int shownCoins = -1;
        int shownCombo = -1;
        int shownDelivered = -1;
        int shownTarget = -1;

        public RectTransform CoinIcon { get; private set; }
        public RectTransform LevelBar { get; private set; }

        public void Init(GameConfig config, Font font, SpriteFactory sprites, RectTransform root)
        {
            this.config = config;
            var top = new Vector2(0.5f, 1f);
            var shadow = config.uiShadow;
            items = UiBuilder.Stretch(UiBuilder.Rect("HudItems", root));
            root = items;

            // Level bar.
            var back = UiBuilder.Image(root, "LevelBar", sprites.Panel, config.uiBarBack, true);
            LevelBar = UiBuilder.Place(back.rectTransform, top, new Vector2(0.5f, 0.5f), new Vector2(0f, TopRowY), new Vector2(BarWidth, BarHeight));
            var fillArea = UiBuilder.Stretch(UiBuilder.Rect("FillArea", back.transform));
            fillArea.offsetMin = Vector2.one * BarPadding;
            fillArea.offsetMax = -Vector2.one * BarPadding;
            var fill = UiBuilder.Image(fillArea, "Fill", sprites.Panel, config.uiBarFill, true);
            barFill = UiBuilder.Stretch(fill.rectTransform);
            progressText = UiBuilder.Label(back.transform, "Progress", font, "", ProgressFont, config.uiText, TextAnchor.MiddleCenter, shadow);
            UiBuilder.Stretch(progressText.rectTransform);

            float circleX = BarWidth * 0.5f + CircleGap + CircleSize * 0.5f;
            levelText = NumberCircle(root, font, sprites, "Level", config.uiLevelCircle, new Vector2(-circleX, TopRowY));
            nextText = NumberCircle(root, font, sprites, "Next", config.uiNextCircle, new Vector2(circleX, TopRowY));

            // Lives, top left.
            hearts = new Image[config.startLives];
            for (int i = 0; i < hearts.Length; i++)
            {
                hearts[i] = UiBuilder.Image(root, "Heart" + i, sprites.Heart, config.uiHeart, false);
                UiBuilder.Place(hearts[i].rectTransform, new Vector2(0f, 1f), new Vector2(0f, 0.5f),
                    new Vector2(SideMargin + i * HeartSpacing, SecondRowY), Vector2.one * HeartSize);
            }

            // Coins, top right.
            var coin = UiBuilder.Image(root, "Coin", sprites.Coin, config.uiCoin, false);
            CoinIcon = UiBuilder.Place(coin.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 0.5f),
                new Vector2(-SideMargin, SecondRowY), Vector2.one * CoinSize);
            coinText = UiBuilder.Label(root, "Coins", font, "0", CoinFont, config.uiText, TextAnchor.MiddleRight, shadow);
            UiBuilder.Place(coinText.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 0.5f),
                new Vector2(-SideMargin - CoinSize - BarPadding * 2f, SecondRowY), new Vector2(BarWidth * 0.5f, CoinSize));

            // Combo, under the level bar.
            comboText = UiBuilder.Label(root, "Combo", font, "", ComboFont, config.uiCombo, TextAnchor.MiddleCenter, shadow);
            UiBuilder.Place(comboText.rectTransform, top, new Vector2(0.5f, 0.5f), new Vector2(0f, SecondRowY), new Vector2(BarWidth, CoinSize));
        }

        Text NumberCircle(RectTransform root, Font font, SpriteFactory sprites, string name, Color color, Vector2 position)
        {
            var circle = UiBuilder.Image(root, name + "Circle", sprites.Circle, color, false);
            UiBuilder.Place(circle.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), position, Vector2.one * CircleSize);
            var text = UiBuilder.Label(circle.transform, name + "Number", font, "", NumberFont, config.uiText, TextAnchor.MiddleCenter, config.uiShadow);
            UiBuilder.Stretch(text.rectTransform);
            return text;
        }

        /// <summary>Shown in levels and Endless, hidden on Home.</summary>
        public void SetVisible(bool visible)
        {
            items.gameObject.SetActive(visible);
        }

        public void SetLevel(int number)
        {
            levelText.text = number.ToString();
            nextText.text = (number + 1).ToString();
        }

        /// <summary>Switches between the level bar and the Endless score bar.</summary>
        public void SetEndless(bool on)
        {
            endless = on;
            levelText.transform.parent.gameObject.SetActive(!on);
            nextText.transform.parent.gameObject.SetActive(!on);
            shownDelivered = -1;
            shownTarget = -1;
            if (on)
                barFill.gameObject.SetActive(false);
        }

        public void SetScore(int score, int best)
        {
            if (score == shownDelivered && best == shownTarget)
                return;
            shownDelivered = score;
            shownTarget = best;
            progressText.text = "Score " + score + "    Best " + Mathf.Max(score, best);
        }

        public void SetProgress(int delivered, int target)
        {
            if (endless || (delivered == shownDelivered && target == shownTarget))
                return;
            shownDelivered = delivered;
            shownTarget = target;
            float f = target > 0 ? Mathf.Clamp01((float)delivered / target) : 0f;
            barFill.anchorMax = new Vector2(f, 1f);
            barFill.gameObject.SetActive(f > 0f);
            progressText.text = delivered + " / " + target;
        }

        public void SetLives(int lives)
        {
            for (int i = 0; i < hearts.Length; i++)
                hearts[i].color = i < lives ? config.uiHeart : config.uiHeartLost;
        }

        public void SetCoins(int coins)
        {
            if (coins == shownCoins)
                return;
            shownCoins = coins;
            coinText.text = coins.ToString();
        }

        public void SetCombo(int combo)
        {
            if (combo == shownCombo)
                return;
            shownCombo = combo;
            comboText.gameObject.SetActive(combo > 1);
            if (combo > 1)
                comboText.text = "Combo x" + combo;
        }
    }
}
