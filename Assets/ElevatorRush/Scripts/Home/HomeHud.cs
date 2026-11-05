using System;
using UnityEngine;
using UnityEngine.UI;

namespace ElevatorRush
{
    /// <summary>
    /// Home screen UI (home plan 3.1): coins and gems, settings and tower progress on top; the next-unlock
    /// teaser, the Play button and Endless above the four tabs at the bottom. Settings and the
    /// tab panels open as modal sheets (the tab panels are empty until their milestones).
    /// Everything except the full-screen dimmers sits inside the safe area. The construction site
    /// carries a gem button that opens the next floor now (monetization plan 2.3).
    /// </summary>
    public sealed class HomeHud : MonoBehaviour
    {
        // Layout in reference pixels (1080 wide portrait).
        const float TopRowY = -110f;
        const float SideMargin = 50f;
        const float GearSize = 120f;
        const float CoinSize = 76f;
        const float CoinTextWidth = 320f;
        const float GemRowStep = 92f;
        const float GemSize = 66f;
        static readonly Vector2 FloorButtonSize = new Vector2(260f, 100f);
        const int FloorButtonFont = 48;
        const float FloorButtonGap = 12f;
        const float ProgressWidth = 340f;
        const float ProgressHeight = 96f;
        const float TabsBottom = 24f;
        const float TabWidth = 250f;
        const float TabHeight = 210f;
        const float TabSpacing = 262f;
        const float TabIconSize = 104f;
        const float TabIconY = -22f;
        const float TabLabelY = 40f;
        const float DotSize = 50f;
        const float PlayBottom = 300f;
        const float PlayWidth = 560f;
        const float PlayHeight = 190f;
        const float EndlessWidth = 200f;
        const float EndlessHeight = 130f;
        const float TeaserBottom = 530f;
        static readonly Vector2 TeaserSize = new Vector2(800f, 170f);
        const float SheetWidth = 1000f;
        const float SheetHeight = 900f;
        const float SettingsWidth = 760f;
        const float SettingsHeight = 900f;
        const float ToggleWidth = 560f;
        const float ToggleHeight = 130f;
        const float ToggleTop = -220f;
        const float ToggleStep = 150f;
        const float CloseBottom = 50f;
        const float CloseWidth = 400f;
        const float CloseHeight = 130f;
        const float CardTitleY = -90f;
        const int TopFont = 60;
        const int ProgressFont = 50;
        const int TabFont = 40;
        const int PlayFont = 88;
        const int EndlessFont = 50;
        const int CardTitleFont = 80;
        const int BodyFont = 60;
        const int ButtonFont = 60;
        const int SignFont = 44;

        static readonly string[] TabNames = { "Build", "Elevator", "Characters", "Towers" };

        GameConfig config;
        Camera cam;
        Canvas canvas;
        RectTransform items;
        RectTransform homeOnly;
        bool buildMode;
        RectTransform modals;
        Text coinText;
        Text gemText;
        Text progressText;
        Text playLabel;
        Button endlessButton;
        Image[] tabDots;
        UnlockTeaser teaser;
        Text signLabel;
        Button floorButton;
        Image floorButtonImage;
        PriceLabel floorPrice;
        HomeController home;

        RectTransform settingsPanel;
        Text[] toggleLabels;
        Image[] toggleImages;
        RectTransform sheetPanel;
        Text sheetTitle;

        Action onPlay;
        Action onEndless;
        Func<HomeTab, bool> onTabOpened;
        Action<int> onToggle;
        Action onUnlockFloor;

        int shownCoins = -1;
        float coinAnimStart;
        float coinAnimEnd;
        int coinAnimFrom;
        int coinAnimTo;
        bool coinAnimating;

        public RectTransform CoinIcon { get; private set; }
        public RectTransform GemIcon { get; private set; }
        /// <summary>True while a tab panel is open; the construction site's gem button hides then.</summary>
        public Func<bool> PanelOpen { get; set; }
        public bool IsModalOpen => settingsPanel.gameObject.activeSelf || sheetPanel.gameObject.activeSelf;

        /// <summary>Raised whenever a Home button is pressed (for the click sound).</summary>
        public event Action ButtonPressed;

        /// <summary>Settings toggles, in the order passed to <see cref="SetSettings"/>.</summary>
        public const int SettingSound = 0;
        public const int SettingMusic = 1;
        public const int SettingVibration = 2;
        static readonly string[] SettingNames = { "Sound", "Music", "Vibration" };

        public void Init(GameConfig config, Font font, SpriteFactory sprites, Canvas canvas, RectTransform uiRoot, RectTransform safeRoot,
            Camera camera, HomeController home, Action onPlay, Action onEndless, Func<HomeTab, bool> onTabOpened, Action<int> onToggle,
            Action onUnlockFloor)
        {
            this.config = config;
            this.canvas = canvas;
            cam = camera;
            this.home = home;
            this.onPlay = onPlay;
            this.onEndless = onEndless;
            this.onTabOpened = onTabOpened;
            this.onToggle = onToggle;
            this.onUnlockFloor = onUnlockFloor;
            var shadow = config.uiShadow;
            var center = new Vector2(0.5f, 0.5f);
            var bottom = new Vector2(0.5f, 0f);

            items = UiBuilder.Stretch(UiBuilder.Rect("HomeItems", safeRoot));

            // The construction sign's text, placed over the 3D sign every frame.
            signLabel = UiBuilder.Label(items, "SignLabel", font, "", SignFont, config.uiSignText, TextAnchor.MiddleCenter, Color.clear);
            UiBuilder.Place(signLabel.rectTransform, center, center, Vector2.zero, Vector2.zero);
            // Below the sign: open that floor now with gems. Placed under the sign every frame.
            floorButton = UiBuilder.Button(items, "UnlockFloor", font, sprites.Panel, "", FloorButtonFont, config.uiGemButton, config.uiText, shadow,
                () => Press(this.onUnlockFloor));
            floorButtonImage = floorButton.GetComponent<Image>();
            UiBuilder.Place((RectTransform)floorButton.transform, center, new Vector2(0.5f, 1f), Vector2.zero, FloorButtonSize);
            floorPrice = new PriceLabel((RectTransform)floorButton.transform, config, font, sprites, FloorButtonFont, config.uiText, shadow);

            // Everything except coins and tower progress hides in Build mode.
            homeOnly = UiBuilder.Stretch(UiBuilder.Rect("HomeOnly", items));

            // Top row: settings, tower progress, coins.
            var gear = UiBuilder.Button(homeOnly, "Settings", font, sprites.Circle, "", TopFont, config.uiTabButton, config.uiText, shadow, () => Press(OpenSettings));
            gear.GetComponent<Image>().type = Image.Type.Simple;
            UiBuilder.Place((RectTransform)gear.transform, new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(SideMargin, TopRowY), Vector2.one * GearSize);
            var gearIcon = UiBuilder.Image(gear.transform, "Gear", sprites.Gear, config.uiText, false);
            UiBuilder.Place(gearIcon.rectTransform, center, center, Vector2.zero, Vector2.one * GearSize * 0.72f);

            var progress = UiBuilder.Image(items, "TowerProgress", sprites.Panel, config.uiBarBack, true);
            UiBuilder.Place(progress.rectTransform, new Vector2(0.5f, 1f), center, new Vector2(0f, TopRowY), new Vector2(ProgressWidth, ProgressHeight));
            progressText = UiBuilder.Label(progress.transform, "Text", font, "", ProgressFont, config.uiText, TextAnchor.MiddleCenter, shadow);
            UiBuilder.Stretch(progressText.rectTransform);

            var coin = UiBuilder.Image(items, "Coin", sprites.Coin, config.uiCoin, false);
            CoinIcon = UiBuilder.Place(coin.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 0.5f), new Vector2(-SideMargin, TopRowY), Vector2.one * CoinSize);
            coinText = UiBuilder.Label(items, "Coins", font, "0", TopFont, config.uiText, TextAnchor.MiddleRight, shadow);
            UiBuilder.Place(coinText.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 0.5f),
                new Vector2(-SideMargin - CoinSize - 16f, TopRowY), new Vector2(CoinTextWidth, CoinSize));

            // Gems, under the coins.
            var gem = UiBuilder.Image(items, "Gem", sprites.Gem, config.uiGem, false);
            GemIcon = UiBuilder.Place(gem.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 0.5f),
                new Vector2(-SideMargin - (CoinSize - GemSize) * 0.5f, TopRowY - GemRowStep), Vector2.one * GemSize);
            gemText = UiBuilder.Label(items, "Gems", font, "0", TopFont, config.uiText, TextAnchor.MiddleRight, shadow);
            UiBuilder.Place(gemText.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 0.5f),
                new Vector2(-SideMargin - CoinSize - 16f, TopRowY - GemRowStep), new Vector2(CoinTextWidth, CoinSize));

            // Teaser, Play and Endless.
            teaser = new UnlockTeaser(homeOnly, config, font, sprites, config.uiBarBack, config.uiText, TeaserSize);
            UiBuilder.Place(teaser.Root, bottom, bottom, new Vector2(0f, TeaserBottom), TeaserSize);

            var play = UiBuilder.Button(homeOnly, "Play", font, sprites.Panel, "Level 1", PlayFont, config.uiPlayButton, config.uiText, shadow, () => Press(onPlay));
            UiBuilder.Place((RectTransform)play.transform, bottom, bottom, new Vector2(0f, PlayBottom), new Vector2(PlayWidth, PlayHeight));
            playLabel = play.GetComponentInChildren<Text>();

            endlessButton = UiBuilder.Button(homeOnly, "Endless", font, sprites.Panel, "Endless", EndlessFont, config.uiLevelCircle, config.uiText, shadow, () => Press(onEndless));
            UiBuilder.Place((RectTransform)endlessButton.transform, new Vector2(1f, 0f), new Vector2(1f, 0f),
                new Vector2(-SideMargin * 0.6f, PlayBottom + (PlayHeight - EndlessHeight) * 0.5f), new Vector2(EndlessWidth, EndlessHeight));

            // Tabs.
            var tabIcons = new[] { sprites.House, sprites.ArrowUp, sprites.Person, sprites.Tower };
            tabDots = new Image[TabNames.Length];
            for (int i = 0; i < TabNames.Length; i++)
            {
                var tab = (HomeTab)i;
                var button = UiBuilder.Button(homeOnly, TabNames[i], font, sprites.Panel, "", TabFont, config.uiTabButton, config.uiText, shadow, () => Press(() => OpenTab(tab)));
                UiBuilder.Place((RectTransform)button.transform, bottom, bottom,
                    new Vector2((i - (TabNames.Length - 1) * 0.5f) * TabSpacing, TabsBottom), new Vector2(TabWidth, TabHeight));
                var icon = UiBuilder.Image(button.transform, "Icon", tabIcons[i], config.uiText, false);
                UiBuilder.Place(icon.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, TabIconY), Vector2.one * TabIconSize);
                var label = button.GetComponentInChildren<Text>();
                label.text = TabNames[i];
                UiBuilder.Place(label.rectTransform, bottom, center, new Vector2(0f, TabLabelY), new Vector2(TabWidth, TabFont));
                tabDots[i] = UiBuilder.Image(button.transform, "Dot", sprites.Circle, config.uiRedDot, false);
                UiBuilder.Place(tabDots[i].rectTransform, Vector2.one, center, new Vector2(-DotSize * 0.3f, -DotSize * 0.3f), Vector2.one * DotSize);
            }

            // Modals cover the whole screen, above the Home items.
            modals = UiBuilder.Stretch(UiBuilder.Rect("HomeModals", uiRoot));
            settingsPanel = Dimmer(modals, sprites);
            var settingsCard = Card(settingsPanel, sprites, new Vector2(SettingsWidth, SettingsHeight), center);
            CardTitle(settingsCard, font, "Settings");
            toggleLabels = new Text[SettingNames.Length];
            toggleImages = new Image[SettingNames.Length];
            for (int i = 0; i < SettingNames.Length; i++)
            {
                int index = i;
                var toggle = UiBuilder.Button(settingsCard, SettingNames[i], font, sprites.Panel, "", ButtonFont, config.uiToggleOn, config.uiText, shadow,
                    () => Press(() => this.onToggle(index)));
                UiBuilder.Place((RectTransform)toggle.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f),
                    new Vector2(0f, ToggleTop - i * ToggleStep), new Vector2(ToggleWidth, ToggleHeight));
                toggleLabels[i] = toggle.GetComponentInChildren<Text>();
                toggleImages[i] = toggle.GetComponent<Image>();
            }
            CloseButton(settingsCard, font, sprites);

            sheetPanel = Dimmer(modals, sprites);
            var sheet = Card(sheetPanel, sprites, new Vector2(SheetWidth, SheetHeight), bottom);
            sheet.anchorMin = sheet.anchorMax = bottom;
            sheet.anchoredPosition = new Vector2(0f, TabsBottom);
            sheetTitle = CardTitle(sheet, font, "");
            var body = UiBuilder.Label(sheet, "Body", font, "Coming soon", BodyFont, config.uiNextCircle, TextAnchor.MiddleCenter, Color.clear);
            UiBuilder.Place(body.rectTransform, center, center, Vector2.zero, Vector2.zero);
            CloseButton(sheet, font, sprites);

            CloseModals();
        }

        RectTransform Dimmer(RectTransform parent, SpriteFactory sprites)
        {
            // A full-screen button: tapping outside the card closes it.
            var dim = UiBuilder.Button(parent, "Dimmer", null, null, "", 1, config.uiDim, Color.clear, Color.clear, () => Press(CloseModals));
            Destroy(dim.GetComponentInChildren<Text>().gameObject);
            dim.transition = Selectable.Transition.None;
            // A full-screen dimmer must not squeeze when tapped.
            Destroy(dim.GetComponent<PressScale>());
            dim.GetComponent<Image>().type = Image.Type.Simple;
            return UiBuilder.Stretch((RectTransform)dim.transform);
        }

        RectTransform Card(RectTransform parent, SpriteFactory sprites, Vector2 size, Vector2 pivot)
        {
            var card = UiBuilder.Image(parent, "Card", sprites.Panel, config.uiCard, true);
            // Cards catch touches so tapping them does not close the panel.
            card.raycastTarget = true;
            return UiBuilder.Place(card.rectTransform, new Vector2(0.5f, 0.5f), pivot, Vector2.zero, size);
        }

        Text CardTitle(RectTransform card, Font font, string text)
        {
            var title = UiBuilder.Label(card, "Title", font, text, CardTitleFont, config.uiTextDark, TextAnchor.MiddleCenter, Color.clear);
            UiBuilder.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, CardTitleY), Vector2.zero);
            return title;
        }

        void CloseButton(RectTransform card, Font font, SpriteFactory sprites)
        {
            var close = UiBuilder.Button(card, "Close", font, sprites.Panel, "Close", ButtonFont, config.uiNextCircle, config.uiText, config.uiShadow, () => Press(CloseModals));
            UiBuilder.Place((RectTransform)close.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, CloseBottom), new Vector2(CloseWidth, CloseHeight));
        }

        void Press(Action action)
        {
            ButtonPressed?.Invoke();
            action();
        }

        // ---- Showing ----

        public void SetVisible(bool visible)
        {
            items.gameObject.SetActive(visible);
            if (!visible)
                CloseModals();
        }

        /// <summary>Build mode keeps only the coin counter and tower progress on screen.</summary>
        public void SetBuildMode(bool building)
        {
            buildMode = building;
            homeOnly.gameObject.SetActive(!building);
            if (building)
                CloseModals();
        }

        /// <summary>Refreshes everything from the save. The coin counter is left alone while it counts up.</summary>
        public void Refresh(SaveData save)
        {
            int done = save.LevelsCompleted;
            playLabel.text = "Level " + save.currentLevel;
            endlessButton.gameObject.SetActive(save.endlessUnlocked);
            float tower = TowerProgress.Fraction(save);
            progressText.text = "Tower " + TowerProgress.Percent(save) + "%";
            teaser.Show(done, tower);
            for (int i = 0; i < tabDots.Length; i++)
                tabDots[i].gameObject.SetActive(HomeBadges.HasDot(save, (HomeTab)i));
            signLabel.text = home.HasConstructionSite ? "Level " + home.NextFloorAt : "";
            if (home.HasConstructionSite)
            {
                int gems = GemMath.FloorUnlockGems(home.FloorCount);
                floorPrice.ShowGems(gems);
                floorButtonImage.color = save.gems >= gems ? config.uiGemButton : config.uiCantAfford;
            }
            gemText.text = save.gems.ToString();
            if (!coinAnimating)
                SetCoins(save.coins);
        }

        public void SetSettings(bool sound, bool music, bool vibration)
        {
            SetToggle(SettingSound, sound);
            SetToggle(SettingMusic, music);
            SetToggle(SettingVibration, vibration);
        }

        void SetToggle(int index, bool on)
        {
            toggleLabels[index].text = SettingNames[index] + (on ? ": On" : ": Off");
            toggleImages[index].color = on ? config.uiToggleOn : config.uiToggleOff;
        }

        void SetCoins(int coins)
        {
            if (coins == shownCoins)
                return;
            shownCoins = coins;
            coinText.text = coins.ToString();
        }

        /// <summary>Counts the coin counter from one value to another, e.g. while earned coins fly in.</summary>
        public void AnimateCoins(int from, int to, float delay, float duration)
        {
            coinAnimFrom = from;
            coinAnimTo = to;
            coinAnimStart = Time.unscaledTime + delay;
            coinAnimEnd = coinAnimStart + Mathf.Max(0.01f, duration);
            coinAnimating = true;
            SetCoins(from);
        }

        // ---- Modals ----

        void OpenSettings()
        {
            CloseModals();
            settingsPanel.gameObject.SetActive(true);
        }

        void OpenTab(HomeTab tab)
        {
            CloseModals();
            // Tabs whose panel exists open it; the rest show a placeholder sheet for now.
            if (onTabOpened(tab))
                return;
            sheetTitle.text = TabNames[(int)tab];
            sheetPanel.gameObject.SetActive(true);
        }

        public void CloseModals()
        {
            settingsPanel.gameObject.SetActive(false);
            sheetPanel.gameObject.SetActive(false);
        }

        void Update()
        {
            if (coinAnimating)
            {
                float now = Time.unscaledTime;
                float t = Mathf.Clamp01((now - coinAnimStart) / (coinAnimEnd - coinAnimStart));
                SetCoins(Mathf.RoundToInt(Mathf.Lerp(coinAnimFrom, coinAnimTo, t)));
                if (t >= 1f)
                    coinAnimating = false;
            }
        }

        void LateUpdate()
        {
            // The sign's label would sit under the top row when the camera is close in Build mode.
            if (!items.gameObject.activeSelf || !home.Visible || !home.HasConstructionSite || buildMode)
            {
                signLabel.enabled = false;
                floorButton.gameObject.SetActive(false);
                return;
            }
            bool panelOpen = PanelOpen != null && PanelOpen();
            var screen = cam.WorldToScreenPoint(home.SignPosition);
            signLabel.enabled = screen.z > 0f;
            signLabel.rectTransform.position = new Vector3(screen.x, screen.y, 0f);
            var signBottom = cam.WorldToScreenPoint(home.SignPosition + Vector3.down * config.constructionSignHeight * 0.5f);
            floorButton.gameObject.SetActive(screen.z > 0f && !panelOpen);
            floorButton.transform.position = new Vector3(screen.x, signBottom.y - FloorButtonGap * canvas.scaleFactor, 0f);
            // Scale the text with the sign's size on screen, so it fits whatever the camera distance.
            var edge = cam.WorldToScreenPoint(home.SignPosition + Vector3.right * config.constructionSignSize.x * 0.5f);
            float signPixels = Mathf.Abs(edge.x - screen.x) * 2f / canvas.scaleFactor;
            float textPixels = Mathf.Max(1f, signLabel.preferredWidth);
            signLabel.rectTransform.localScale = Vector3.one * Mathf.Min(1.5f, signPixels * 0.85f / textPixels);
        }
    }
}
