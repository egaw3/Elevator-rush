using System;
using UnityEngine;
using UnityEngine.UI;

namespace ElevatorRush
{
    /// <summary>
    /// Full-screen states: Ready, LevelComplete, LevelFailed, Endless over and Paused. Ends lead back
    /// to Home (home plan 2): Continue after a win, Retry or Home after a fail. Animations use unscaled
    /// time so they play while the game is paused. End-screen buttons stay disabled for a moment
    /// so a held finger cannot skip them.
    /// </summary>
    public sealed class Screens : MonoBehaviour
    {
        // Layout in reference pixels (1080 x 1920 portrait).
        // Ready text sits in the band below the building that the camera keeps free.
        const float ReadyTitleY = 330f;
        const float ReadyTargetY = 225f;
        const float HoldHintY = 110f;
        const float CardWidth = 820f;
        const float CardHeight = 1040f;
        const float TeaserY = -630f;
        static readonly Vector2 TeaserSize = new Vector2(700f, 170f);
        const float FailCardHeight = 560f;
        const float CardTitleY = -110f;
        const float StarsY = -300f;
        const float StarSize = 170f;
        const float StarSpacing = 200f;
        const float CoinsRowY = -460f;
        const float CoinSize = 80f;
        const float GemsX = 250f;
        const float GemSize = 70f;
        const float ButtonBottom = 70f;
        const float ButtonWidth = 520f;
        const float ButtonHeight = 150f;
        const float SmallButtonWidth = 340f;
        const float ButtonGap = 30f;
        const float EndlessCardHeight = 700f;
        const float ScoreY = -250f;
        const float BestY = -350f;
        const float HomeButtonWidth = 240f;
        const float HomeButtonHeight = 110f;
        const float HomeButtonMargin = 40f;
        const int HomeButtonFont = 50;
        const int TitleFont = 120;
        const int SubtitleFont = 70;
        const int CardTitleFont = 84;
        const int BodyFont = 64;
        const int ButtonFont = 68;

        GameConfig config;
        RectTransform readyPanel;
        RectTransform completePanel;
        RectTransform failedPanel;
        RectTransform pausedPanel;
        Text readyTitle;
        Text readyTarget;
        Text holdHint;
        Text pausedHint;
        Image[] stars;
        Text earnedText;
        Image earnedGem;
        Text earnedGemsText;
        Button continueButton;
        Button retryButton;
        Button failedHome;
        UnlockTeaser teaser;
        RectTransform endlessPanel;
        Text endlessScore;
        Text endlessBest;
        Button endlessRetry;
        Button endlessHome;
        Text banner;
        float bannerShownAt;

        float shownAt;
        float buttonsUnlockAt;
        int starCount;
        int coinsEarned;
        int shownCoinValue = -1;

        public void Init(GameConfig config, Font font, SpriteFactory sprites, RectTransform root, RectTransform safeRoot,
            Action onContinue, Action onRetry, Action onHome)
        {
            this.config = config;
            var shadow = config.uiShadow;
            var top = new Vector2(0.5f, 1f);
            var center = new Vector2(0.5f, 0.5f);

            // Ready: text only, never blocks touches.
            readyPanel = UiBuilder.Stretch(UiBuilder.Rect("Ready", safeRoot));
            // Total coins show in the HUD's coin counter, which stays visible on this screen.
            var bottom = new Vector2(0.5f, 0f);
            readyTitle = UiBuilder.Label(readyPanel, "Title", font, "", TitleFont, config.uiText, TextAnchor.MiddleCenter, shadow);
            UiBuilder.Place(readyTitle.rectTransform, bottom, center, new Vector2(0f, ReadyTitleY), Vector2.zero);
            readyTarget = UiBuilder.Label(readyPanel, "Target", font, "", SubtitleFont, config.uiText, TextAnchor.MiddleCenter, shadow);
            UiBuilder.Place(readyTarget.rectTransform, bottom, center, new Vector2(0f, ReadyTargetY), Vector2.zero);
            holdHint = UiBuilder.Label(readyPanel, "Hold", font, "Hold to start", SubtitleFont, config.uiText, TextAnchor.MiddleCenter, shadow);
            UiBuilder.Place(holdHint.rectTransform, bottom, center, new Vector2(0f, HoldHintY), Vector2.zero);

            // Back to Home before the level starts, bottom left of the Ready screen.
            var homeButton = UiBuilder.Button(readyPanel, "Home", font, sprites.Panel, "Home", HomeButtonFont,
                config.uiNextCircle, config.uiText, shadow, () => { Clicked(); onHome(); });
            UiBuilder.Place((RectTransform)homeButton.transform, new Vector2(0f, 0f), new Vector2(0f, 0.5f),
                new Vector2(HomeButtonMargin, HoldHintY), new Vector2(HomeButtonWidth, HomeButtonHeight));

            // Level complete: dim, card, stars, coins, Next.
            completePanel = Dimmer(root, sprites, "Complete", true);
            var card = Card(completePanel, sprites, CardHeight);
            Title(card, font, "Level Complete!");
            stars = new Image[ScoreSystem.MaxStars];
            for (int i = 0; i < stars.Length; i++)
            {
                stars[i] = UiBuilder.Image(card, "Star" + i, sprites.Star, config.uiStarOff, false);
                UiBuilder.Place(stars[i].rectTransform, top, center,
                    new Vector2((i - (stars.Length - 1) * 0.5f) * StarSpacing, StarsY), Vector2.one * StarSize);
            }
            var earnedCoin = UiBuilder.Image(card, "Coin", sprites.Coin, config.uiCoin, false);
            UiBuilder.Place(earnedCoin.rectTransform, top, new Vector2(1f, 0.5f), new Vector2(-CoinSize * 0.25f, CoinsRowY), Vector2.one * CoinSize);
            earnedText = UiBuilder.Label(card, "Earned", font, "", BodyFont, config.uiTextDark, TextAnchor.MiddleLeft, Color.clear);
            UiBuilder.Place(earnedText.rectTransform, top, new Vector2(0f, 0.5f), new Vector2(CoinSize * 0.25f, CoinsRowY), new Vector2(CardWidth * 0.5f, CoinSize));
            // Gems earned by this win (monetization plan 2.1), beside the coins when there are any.
            earnedGem = UiBuilder.Image(card, "Gem", sprites.Gem, config.uiGem, false);
            UiBuilder.Place(earnedGem.rectTransform, top, new Vector2(1f, 0.5f), new Vector2(GemsX, CoinsRowY), Vector2.one * GemSize);
            earnedGemsText = UiBuilder.Label(card, "EarnedGems", font, "", BodyFont, config.uiTextDark, TextAnchor.MiddleLeft, Color.clear);
            UiBuilder.Place(earnedGemsText.rectTransform, top, new Vector2(0f, 0.5f), new Vector2(GemsX + CoinSize * 0.2f, CoinsRowY), new Vector2(CardWidth * 0.25f, CoinSize));
            teaser = new UnlockTeaser(card, config, font, sprites, config.uiStarOff, config.uiTextDark, TeaserSize);
            UiBuilder.Place(teaser.Root, top, center, new Vector2(0f, TeaserY), TeaserSize);
            continueButton = UiBuilder.Button(card, "Continue", font, sprites.Panel, "Continue", ButtonFont, config.uiNextButton, config.uiText, shadow, () => Press(onContinue));
            UiBuilder.Place((RectTransform)continueButton.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, ButtonBottom), new Vector2(ButtonWidth, ButtonHeight));

            // Level failed: dim, card, Retry and Home.
            float half = (SmallButtonWidth + ButtonGap) * 0.5f;
            failedPanel = Dimmer(root, sprites, "Failed", true);
            var failCard = Card(failedPanel, sprites, FailCardHeight);
            Title(failCard, font, "Out of lives!");
            retryButton = UiBuilder.Button(failCard, "Retry", font, sprites.Panel, "Retry", ButtonFont, config.uiRetryButton, config.uiText, shadow, () => Press(onRetry));
            UiBuilder.Place((RectTransform)retryButton.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-half, ButtonBottom), new Vector2(SmallButtonWidth, ButtonHeight));
            failedHome = UiBuilder.Button(failCard, "Home", font, sprites.Panel, "Home", ButtonFont, config.uiNextCircle, config.uiText, shadow, () => Press(onHome));
            UiBuilder.Place((RectTransform)failedHome.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(half, ButtonBottom), new Vector2(SmallButtonWidth, ButtonHeight));

            // Endless run over: score, best, Retry and Home.
            endlessPanel = Dimmer(root, sprites, "EndlessOver", true);
            var endlessCard = Card(endlessPanel, sprites, EndlessCardHeight);
            Title(endlessCard, font, "Run over!");
            endlessScore = UiBuilder.Label(endlessCard, "Score", font, "", CardTitleFont, config.uiTextDark, TextAnchor.MiddleCenter, Color.clear);
            UiBuilder.Place(endlessScore.rectTransform, top, center, new Vector2(0f, ScoreY), Vector2.zero);
            endlessBest = UiBuilder.Label(endlessCard, "Best", font, "", BodyFont, config.uiTextDark, TextAnchor.MiddleCenter, Color.clear);
            UiBuilder.Place(endlessBest.rectTransform, top, center, new Vector2(0f, BestY), Vector2.zero);
            endlessRetry = UiBuilder.Button(endlessCard, "Retry", font, sprites.Panel, "Retry", ButtonFont, config.uiRetryButton, config.uiText, shadow, () => Press(onRetry));
            UiBuilder.Place((RectTransform)endlessRetry.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-half, ButtonBottom), new Vector2(SmallButtonWidth, ButtonHeight));
            endlessHome = UiBuilder.Button(endlessCard, "Home", font, sprites.Panel, "Home", ButtonFont, config.uiNextCircle, config.uiText, shadow, () => Press(onHome));
            UiBuilder.Place((RectTransform)endlessHome.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(half, ButtonBottom), new Vector2(SmallButtonWidth, ButtonHeight));

            // Paused: dim but not blocking, so the next touch both resumes and counts as a press.
            pausedPanel = Dimmer(root, sprites, "Paused", false);
            var pausedTitle = UiBuilder.Label(pausedPanel, "Title", font, "Paused", TitleFont, config.uiText, TextAnchor.MiddleCenter, shadow);
            UiBuilder.Place(pausedTitle.rectTransform, center, center, Vector2.zero, Vector2.zero);
            pausedHint = UiBuilder.Label(pausedPanel, "Hint", font, "Tap to resume", SubtitleFont, config.uiText, TextAnchor.MiddleCenter, shadow);
            UiBuilder.Place(pausedHint.rectTransform, new Vector2(0.5f, 0f), center, new Vector2(0f, ReadyTitleY), Vector2.zero);

            // Banner: big text popping in over the building, e.g. "Rush hour!".
            banner = UiBuilder.Label(root, "Banner", font, "", TitleFont, config.uiBanner, TextAnchor.MiddleCenter, shadow);
            UiBuilder.Place(banner.rectTransform, center, center, Vector2.zero, Vector2.zero);

            HideAll();
        }

        RectTransform Dimmer(RectTransform root, SpriteFactory sprites, string name, bool blocksTouches)
        {
            var dim = UiBuilder.Image(root, name, null, config.uiDim, false);
            dim.raycastTarget = blocksTouches;
            return UiBuilder.Stretch(dim.rectTransform);
        }

        RectTransform Card(RectTransform parent, SpriteFactory sprites, float height)
        {
            var card = UiBuilder.Image(parent, "Card", sprites.Panel, config.uiCard, true);
            return UiBuilder.Place(card.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(CardWidth, height));
        }

        void Title(RectTransform card, Font font, string text)
        {
            var title = UiBuilder.Label(card, "Title", font, text, CardTitleFont, config.uiTextDark, TextAnchor.MiddleCenter, Color.clear);
            UiBuilder.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, CardTitleY), Vector2.zero);
        }

        /// <summary>Raised whenever a screen button is pressed (for the click sound).</summary>
        public event Action ButtonPressed;

        void Clicked()
        {
            ButtonPressed?.Invoke();
        }

        void Press(Action action)
        {
            if (Time.unscaledTime < buttonsUnlockAt)
                return;
            Clicked();
            action();
        }

        public void HideAll()
        {
            readyPanel.gameObject.SetActive(false);
            completePanel.gameObject.SetActive(false);
            failedPanel.gameObject.SetActive(false);
            pausedPanel.gameObject.SetActive(false);
            endlessPanel.gameObject.SetActive(false);
            banner.gameObject.SetActive(false);
        }

        public void ShowBanner(string message)
        {
            banner.text = message;
            bannerShownAt = Time.unscaledTime;
            banner.gameObject.SetActive(true);
        }

        public void ShowReady(int level, int target, bool rush)
        {
            HideAll();
            readyTitle.text = "Level " + level;
            readyTarget.text = (rush ? "Rush hour! " : "") + "Deliver " + target;
            readyPanel.gameObject.SetActive(true);
        }

        public void ShowReadyEndless(int best)
        {
            HideAll();
            readyTitle.text = "Endless";
            readyTarget.text = "Best " + best;
            readyPanel.gameObject.SetActive(true);
        }

        public void ShowEndlessOver(int score, int best, bool newBest)
        {
            HideAll();
            endlessScore.text = "Score " + score;
            endlessBest.text = newBest ? "New best!" : "Best " + best;
            endlessHome.interactable = false;
            ShowEnd(endlessPanel, endlessRetry);
        }

        /// <summary>The win card; the teaser shows the next unlock after this win.</summary>
        public void ShowComplete(int earnedStars, int coins, int gems, int levelsCompleted, float towerFraction)
        {
            earnedGem.gameObject.SetActive(gems > 0);
            earnedGemsText.gameObject.SetActive(gems > 0);
            earnedGemsText.text = "+" + gems;
            teaser.Show(levelsCompleted, towerFraction);
            HideAll();
            starCount = earnedStars;
            coinsEarned = coins;
            shownCoinValue = -1;
            for (int i = 0; i < stars.Length; i++)
            {
                stars[i].color = config.uiStarOff;
                stars[i].rectTransform.localScale = Vector3.one;
            }
            ShowEnd(completePanel, continueButton);
        }

        public void ShowFailed()
        {
            HideAll();
            failedHome.interactable = false;
            ShowEnd(failedPanel, retryButton);
        }

        public void ShowPaused()
        {
            HideAll();
            pausedPanel.gameObject.SetActive(true);
        }

        void ShowEnd(RectTransform panel, Button button)
        {
            shownAt = Time.unscaledTime;
            buttonsUnlockAt = shownAt + config.endScreenInputLock;
            button.interactable = false;
            panel.gameObject.SetActive(true);
        }

        void Update()
        {
            float now = Time.unscaledTime;

            if (readyPanel.gameObject.activeSelf)
                holdHint.color = PulseColor(now);
            if (pausedPanel.gameObject.activeSelf)
                pausedHint.color = PulseColor(now);

            if (banner.gameObject.activeSelf)
                AnimateBanner(now - bannerShownAt);

            if (failedPanel.gameObject.activeSelf)
            {
                retryButton.interactable = now >= buttonsUnlockAt;
                failedHome.interactable = now >= buttonsUnlockAt;
            }
            if (endlessPanel.gameObject.activeSelf)
            {
                endlessRetry.interactable = now >= buttonsUnlockAt;
                endlessHome.interactable = now >= buttonsUnlockAt;
            }

            if (completePanel.gameObject.activeSelf)
                AnimateComplete(now - shownAt, now);
        }

        void AnimateBanner(float elapsed)
        {
            if (elapsed >= config.bannerTime)
            {
                banner.gameObject.SetActive(false);
                return;
            }
            float pop = Mathf.Clamp01(elapsed / config.bannerPopTime);
            banner.rectTransform.localScale = Vector3.one * Mathf.Lerp(config.bannerPopScale, 1f, pop);
            float fadeStart = config.bannerTime - config.bannerPopTime;
            var c = config.uiBanner;
            c.a = elapsed > fadeStart ? 1f - (elapsed - fadeStart) / config.bannerPopTime : 1f;
            banner.color = c;
        }

        Color PulseColor(float now)
        {
            var c = config.uiText;
            c.a = 0.55f + 0.45f * (0.5f + 0.5f * Mathf.Sin(now * config.holdHintPulseSpeed));
            return c;
        }

        void AnimateComplete(float elapsed, float now)
        {
            continueButton.interactable = now >= buttonsUnlockAt;

            for (int i = 0; i < starCount; i++)
            {
                float t = (elapsed - (i + 1) * config.starRevealDelay) / config.starPopTime;
                if (t < 0f)
                    continue;
                stars[i].color = config.uiStarOn;
                float scale = Mathf.Lerp(config.starPopScale, 1f, Mathf.Clamp01(t));
                stars[i].rectTransform.localScale = Vector3.one * scale;
            }

            float countStart = (starCount + 1) * config.starRevealDelay;
            float f = Mathf.Clamp01((elapsed - countStart) / config.coinCountTime);
            int value = Mathf.RoundToInt(coinsEarned * f);
            if (value != shownCoinValue)
            {
                shownCoinValue = value;
                earnedText.text = "+" + value;
            }
        }
    }
}
