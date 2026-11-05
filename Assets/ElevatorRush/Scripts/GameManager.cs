using UnityEngine;

namespace ElevatorRush
{
    public enum GameState
    {
        Home,
        Ready,
        Playing,
        LevelComplete,
        LevelFailed,
        Paused
    }

    /// <summary>
    /// Game flow (plan 5.8 and 5.9, home plan 2): the app opens on Home; Play goes to the current
    /// level (Ready waits for the first touch with the target shown); a win continues to Home with
    /// the earned coins flying in; a fail offers Retry or Home; Endless runs end back on Home.
    /// Keeps the save up to date and rebuilds the scene without reloading it.
    /// </summary>
    public sealed class GameManager : MonoBehaviour
    {
        /// <summary>Shared by every gameplay object; upgrades are copied into it for each run.</summary>
        GameConfig config;
        /// <summary>The untouched tuning values that upgrades are applied to.</summary>
        GameConfig baseConfig;
        InputReader input;
        Building building;
        ElevatorController elevator;
        PassengerSpawner spawner;
        CameraRig cameraRig;
        Hud hud;
        Screens screens;
        Tutorial tutorial;
        Environment environment;
        Sfx sfx;
        Fx fx;
        HomeController home;
        HomeHud homeHud;
        BuildPanel buildPanel;
        ElevatorPanel elevatorPanel;
        CharactersPanel charactersPanel;
        TowersPanel towersPanel;
        UnlockPopup unlockPopup;
        HomeWanderers wanderers;
        StreetLife streetLife;
        ConfirmDialog dialog;
        GemFlow gemFlow;

        SaveData save;
        Shop shop;
        RunCoins runCoins = new RunCoins(1f);
        float coinFactor = 1f;
        LevelSession session;
        bool endless;
        float inputLockedUntil;
        bool wasHolding;
        bool cameraPlaced;

        public GameState State { get; private set; }
        public bool IsEndless => endless;
        public int LevelNumber => session != null ? session.Level.number : save.currentLevel;
        public int Delivered => session != null ? session.Delivered : 0;

        public void Init(GameConfig config, InputReader input, Building building, ElevatorController elevator,
            PassengerSpawner spawner, CameraRig cameraRig, Hud hud, Screens screens, Tutorial tutorial,
            Environment environment, Sfx sfx, Fx fx, HomeController home, HomeHud homeHud, BuildPanel buildPanel,
            ElevatorPanel elevatorPanel, CharactersPanel charactersPanel, TowersPanel towersPanel,
            UnlockPopup unlockPopup, HomeWanderers wanderers, StreetLife streetLife, ConfirmDialog dialog)
        {
            this.environment = environment;
            this.sfx = sfx;
            this.fx = fx;
            this.config = config;
            baseConfig = config.Clone();
            this.input = input;
            this.building = building;
            this.elevator = elevator;
            this.spawner = spawner;
            this.cameraRig = cameraRig;
            this.hud = hud;
            this.screens = screens;
            this.tutorial = tutorial;
            this.home = home;
            this.homeHud = homeHud;
            this.buildPanel = buildPanel;
            this.elevatorPanel = elevatorPanel;
            this.charactersPanel = charactersPanel;
            this.towersPanel = towersPanel;
            this.unlockPopup = unlockPopup;
            this.wanderers = wanderers;
            this.streetLife = streetLife;
            this.dialog = dialog;
            streetLife.Save = () => save;
            save = SaveData.Load();
            CreateShop();
            ApplySettings();

            // Every gem spend is confirmed first (monetization plan 2.4).
            gemFlow = new GemFlow(dialog, () => save, () => shop);
            gemFlow.NotEnoughGems = missing => dialog.Tell("Not enough gems",
                "You need " + missing + " more " + (missing == 1 ? "gem" : "gems") + ".\nEarn gems with three-star wins, new unlocks and finished floors.");
            dialog.ButtonPressed += () => sfx.Play(SfxKey.Button);
            buildPanel.Gems = gemFlow;
            elevatorPanel.Gems = gemFlow;
            charactersPanel.Gems = gemFlow;
            towersPanel.Gems = gemFlow;
            homeHud.PanelOpen = () => elevatorPanel.IsOpen || charactersPanel.IsOpen || towersPanel.IsOpen;

            // Rooms in levels and on Home show the player's decor (home plan 5.2).
            building.DecorSource = (floor, slot) => save.EquippedDecor(floor, slot);
            buildPanel.Save = () => save;
            buildPanel.Buy = id => shop.TryBuy(id);
            buildPanel.Equip = id => shop.TryEquip(id);
            buildPanel.ModeChanged += OnBuildModeChanged;
            buildPanel.Purchased += OnPurchased;
            elevatorPanel.Save = () => save;
            elevatorPanel.Buy = id => shop.TryBuy(id);
            elevatorPanel.Purchased += OnUpgradePurchased;
            // Passengers wear the skin equipped on their destination color (home plan 7).
            spawner.SkinOf = destination => save.EquippedSkin(destination);
            charactersPanel.Save = () => save;
            charactersPanel.Buy = id => shop.TryBuy(id);
            charactersPanel.Equip = (id, slot) => shop.TryEquipSkin(id, slot);
            charactersPanel.Purchased += OnPurchased;
            // Each unlock popup is recorded as shown once dismissed, so it never shows again.
            unlockPopup.Shown += id =>
            {
                UnlockPopups.MarkShown(save, id);
                save.Save();
                homeHud.Refresh(save);
            };
            towersPanel.Save = () => save;
            towersPanel.Buy = id => shop.TryBuy(id);
            towersPanel.Equip = id => shop.TryEquip(id);
            towersPanel.Purchased += OnPurchased;
            towersPanel.Preview = PreviewTower;
            towersPanel.Closed += () =>
            {
                // Leaving without buying shows the equipped skin again.
                if (State == GameState.Home)
                    RebuildHome();
            };
            charactersPanel.Closed += () =>
            {
                if (State == GameState.Home)
                {
                    // A floor opened early from a color chip appears once the panel closes.
                    if (home.FloorCount != UnlockSchedule.UnlockedFloorCount(save))
                        RebuildHome();
                    else
                        wanderers.Populate(save, home.FloorCount);
                    streetLife.SetPedestriansAllowed(true);
                }
                if (State == GameState.Home && !buildPanel.IsOpen)
                    home.ShowTower();
            };
            screens.ButtonPressed += () => sfx.Play(SfxKey.Button);
            homeHud.ButtonPressed += () => sfx.Play(SfxKey.Button);

            elevator.Delivered += OnDelivered;
            elevator.Docked += OnDocked;
            spawner.PassengerLeft += OnPassengerLeft;
        }

        void Start()
        {
            GoHome(0);
        }

        /// <summary>
        /// Turns the saved elevator tiers into this run's values (home plan 6) and hands them to
        /// gameplay: motor, doors, capacity and cabin size through the shared config, Comfort to
        /// the spawner, Tips to the run's coin counter, and the cabin look. With no tiers every
        /// value equals the base tuning, so an unupgraded save plays exactly as before.
        /// </summary>
        void ApplyUpgrades()
        {
            var run = UpgradeMath.Apply(baseConfig, save);
            run.ApplyTo(config);
            spawner.PatienceFactor = run.patienceFactor;
            coinFactor = run.coinFactor;
            elevator.CabinLook = UpgradeMath.CabinLook(save.TotalUpgradeTiers);
            // The equipped tower skin, on Home and in every level and Endless run (home plan 8).
            building.Skin = TowerSkins.Get(save.equippedTower);
        }

        /// <summary>Rebuilds the home tower with the current upgrades and the equipped skin.</summary>
        void RebuildHome()
        {
            ApplyUpgrades();
            home.Show(save, LevelGenerator.ThemeOf(save.currentLevel), false);
            wanderers.Populate(save, home.FloorCount);
            buildPanel.SetHomeVisible(true);
        }

        /// <summary>Towers panel: shows a skin on the home tower, framed above the sheet.</summary>
        void PreviewTower(string towerId, float bottomBand)
        {
            ApplyUpgrades();
            building.Skin = TowerSkins.Get(towerId);
            home.Show(save, LevelGenerator.ThemeOf(save.currentLevel), false);
            wanderers.Populate(save, home.FloorCount);
            home.FocusBox(building.Bounds, bottomBand, config.skinPreviewMaxReserve);
        }

        /// <summary>A shop over the current save that saves after every purchase and equip.</summary>
        void CreateShop()
        {
            shop = new Shop(save);
            shop.Changed += () => save.Save();
        }

        // ---- Home ----

        /// <summary>Shows the home tower. Coins earned in the run just finished fly into the counter.</summary>
        void GoHome(int earnedCoins)
        {
            Time.timeScale = 1f;
            State = GameState.Home;
            session = null;
            endless = false;
            fx.ClearAll();
            tutorial.Hide();
            screens.HideAll();
            hud.SetVisible(false);
            spawner.Running = false;
            Screen.sleepTimeout = SleepTimeout.SystemSetting;

            buildPanel.SetHomeVisible(false);
            elevatorPanel.Close();
            charactersPanel.Close();
            towersPanel.Close();
            ApplyUpgrades();
            home.Show(save, LevelGenerator.ThemeOf(save.currentLevel), !cameraPlaced);
            wanderers.Populate(save, home.FloorCount);
            // Traffic and pedestrians on Home only.
            streetLife.Begin();
            cameraPlaced = true;
            homeHud.SetVisible(true);
            homeHud.SetBuildMode(false);
            homeHud.Refresh(save);
            buildPanel.SetHomeVisible(true);
            fx.CoinTarget = homeHud.CoinIcon;

            if (earnedCoins > 0)
            {
                var center = new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f);
                int flying = fx.CoinBurstFromScreen(center, config.homeCoinFlyCount);
                // The counter starts counting when the first coin lands and ends with the last one.
                homeHud.AnimateCoins(save.coins - earnedCoins, save.coins, config.coinPopTime,
                    fx.CoinBurstDuration(flying) - config.coinPopTime);
            }
            // Anything newly unlocked pops up before the player can do anything else.
            var pending = UnlockPopups.Pending(save);
            if (pending.Count > 0)
                unlockPopup.Show(pending);
            LockInput();
        }

        /// <summary>Home: Play starts the current level.</summary>
        public void Play()
        {
            if (State == GameState.Home)
                LoadLevel(save.currentLevel);
        }

        /// <summary>Home: Endless, once unlocked.</summary>
        public void PlayEndless()
        {
            if (State == GameState.Home && save.endlessUnlocked)
                LoadEndless();
        }

        /// <summary>
        /// Home: a tab was opened, so what it unlocked is no longer new. Returns true when the tab
        /// has its own panel (Build), false for tabs that still show the placeholder sheet.
        /// </summary>
        public bool OnTabOpened(HomeTab tab)
        {
            if (HomeBadges.MarkViewed(save, tab))
                save.Save();
            homeHud.Refresh(save);
            switch (tab)
            {
                case HomeTab.Build:
                    buildPanel.EnterDefault();
                    return true;
                case HomeTab.Elevator:
                    elevatorPanel.Open();
                    return true;
                case HomeTab.Characters:
                    // The preview character stands alone on the ground floor.
                    streetLife.SetPedestriansAllowed(false);
                    wanderers.Remove();
                    charactersPanel.Open();
                    return true;
                case HomeTab.Towers:
                    towersPanel.Open();
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>Home: the construction site's gem button opens the next floor now (monetization plan 2.3).</summary>
        public void UnlockNextFloor()
        {
            if (State != GameState.Home || home.FloorCount >= Catalog.FloorCount || homeHud.PanelOpen())
                return;
            gemFlow.UnlockEarly(Catalog.FloorId(home.FloorCount), receipt =>
            {
                sfx.Play(SfxKey.Unlock);
                Fx.Haptic();
                fx.Confetti();
                RebuildHome();
                homeHud.Refresh(save);
            });
        }

        void OnUpgradePurchased(ShopReceipt receipt, int coinsBefore, UpgradeDef upgrade)
        {
            OnPurchased(receipt, coinsBefore);
            // Rebuild the tower so a wider cabin or a new cabin look shows at once.
            RebuildHome();
        }

        void OnBuildModeChanged(bool building)
        {
            homeHud.SetBuildMode(building);
            homeHud.Refresh(save);
        }

        void OnPurchased(ShopReceipt receipt, int coinsBefore)
        {
            // The counter counts down the price, then up again for a floor completion bonus.
            homeHud.AnimateCoins(coinsBefore, save.coins, 0f, config.coinCountTime * 0.5f);
            homeHud.Refresh(save);
        }

        /// <summary>Home settings: sound, music or vibration on or off, saved.</summary>
        public void ToggleSetting(int setting)
        {
            switch (setting)
            {
                case HomeHud.SettingSound: save.muted = !save.muted; break;
                case HomeHud.SettingMusic: save.musicMuted = !save.musicMuted; break;
                case HomeHud.SettingVibration: save.vibrationMuted = !save.vibrationMuted; break;
            }
            ApplySettings();
            save.Save();
            if (!save.vibrationMuted && setting == HomeHud.SettingVibration)
                Fx.Haptic();
        }

        void ApplySettings()
        {
            sfx.Muted = save.muted;
            Fx.HapticsEnabled = !save.vibrationMuted;
            // There is no music yet; the setting is saved for when there is.
            homeHud.SetSettings(!save.muted, !save.musicMuted, !save.vibrationMuted);
        }

        // ---- Levels ----

        void LoadLevel(int number)
        {
            endless = false;
            Setup(LevelCatalog.Get(number));
            hud.SetEndless(false);
            hud.SetLevel(session.Level.number);
            hud.SetProgress(0, session.Level.target);
            tutorial.Prepare(session.Level.number);
            screens.ShowReady(session.Level.number, session.Level.target, session.Level.rush);
        }

        void LoadEndless()
        {
            endless = true;
            Setup(EndlessRamp.Create(System.Environment.TickCount));
            hud.SetEndless(true);
            hud.SetScore(0, save.endlessHighScore);
            tutorial.Prepare(0);
            screens.ShowReadyEndless(save.endlessHighScore);
        }

        /// <summary>Leaves Home if needed, rebuilds the scene for a level definition and enters Ready.</summary>
        void Setup(LevelDefinition level)
        {
            Time.timeScale = 1f;
            dialog.Close();
            buildPanel.SetHomeVisible(false);
            elevatorPanel.Close();
            charactersPanel.Close();
            towersPanel.Close();
            unlockPopup.Hide();
            wanderers.Clear();
            streetLife.End();
            home.Hide();
            ApplyUpgrades();
            runCoins = new RunCoins(coinFactor);
            homeHud.SetVisible(false);
            hud.SetVisible(true);
            fx.CoinTarget = hud.CoinIcon;

            session = new LevelSession(config, level);
            fx.ClearAll();

            spawner.Clear();
            building.Build(level.floors, level.theme);
            environment.Apply(Theme.Get(level.theme), building.LeftX, building.RightX, building.FrontZ, building.BackZ);
            elevator.AttachTo(building);
            elevator.InputEnabled = false;
            spawner.Prepare(building, level);
            cameraRig.Fit(building.Bounds, !cameraPlaced);
            cameraPlaced = true;

            hud.SetLives(session.Score.Lives);
            hud.SetCombo(session.Score.Combo);
            hud.SetCoins(save.coins);

            State = GameState.Ready;
            LockInput();
        }

        void Update()
        {
            bool holding = input.IsHolding;
            bool pressed = holding && !wasHolding && Time.unscaledTime >= inputLockedUntil;
            wasHolding = holding;

            // Home ignores touches outside its buttons; a level starts only from Ready.
            if (State == GameState.Ready && pressed)
                StartPlaying();
            else if (State == GameState.Paused && pressed)
                Resume();
        }

        void StartPlaying()
        {
            State = GameState.Playing;
            screens.HideAll();
            if (session.Level.rush)
                screens.ShowBanner("Rush hour!");
            tutorial.OnPlayStarted();
            elevator.InputEnabled = true;
            spawner.Running = true;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
        }

        void OnDocked(int floor, bool perfect)
        {
            if (State != GameState.Playing)
                return;
            session.RegisterDock(perfect);
            int combo = session.Score.Combo;
            hud.SetCombo(combo);
            tutorial.OnDocked();

            float pitch = 1f + (combo - 1) * config.dockPitchStep;
            fx.Punch(elevator.Cabin, config.cabinPunch);
            if (perfect)
            {
                sfx.Play(SfxKey.Perfect, pitch);
                cameraRig.Shake();
                fx.FloatText(elevator.Cabin.position + Vector3.up * config.floatTextHeight,
                    combo > 1 ? "Perfect! x" + combo : "Perfect!", config.uiPerfect, config.perfectTextFont);
            }
            else
            {
                sfx.Play(SfxKey.Dock, pitch);
            }
        }

        void OnDelivered(Passenger passenger)
        {
            if (State != GameState.Playing)
                return;
            // Coins are banked as they are earned, so a failed level loses none.
            // Whole coins are banked as they are reached; Tips fractions round up at the end of the run.
            int earned = session.RegisterDelivery(passenger.CoinMultiplier);
            save.coins += runCoins.Add(earned);
            hud.SetCoins(save.coins);

            var above = passenger.transform.position + Vector3.up * config.floatTextHeight;
            sfx.Play(SfxKey.Deliver);
            fx.FloatText(above, "+" + runCoins.Value(earned).ToString("0.#", System.Globalization.CultureInfo.InvariantCulture), config.uiCoin, config.floatTextFont);
            fx.CoinBurst(above, earned);
            fx.Punch(hud.LevelBar, config.uiPunch);
            tutorial.OnDelivered(passenger.Destination);

            if (endless)
            {
                hud.SetScore(session.Delivered, save.endlessHighScore);
                RampEndless();
                return;
            }

            hud.SetProgress(session.Delivered, session.Level.target);
            if (session.State == SessionState.Won)
                Complete();
        }

        /// <summary>Applies the Endless difficulty for the new score, growing the building when due.</summary>
        void RampEndless()
        {
            EndlessRamp.Apply(session.Level, session.Delivered);
            if (session.Level.floors <= building.FloorCount)
                return;
            building.Grow(session.Level.floors);
            elevator.RefreshFloors();
            cameraRig.Fit(building.Bounds, false);
        }

        void OnPassengerLeft(Passenger passenger)
        {
            if (State != GameState.Playing)
                return;
            session.LoseLife();
            hud.SetLives(session.Score.Lives);
            sfx.Play(SfxKey.Angry);
            Fx.Haptic();
            if (session.State != SessionState.Failed)
                return;
            if (endless)
                EndRun();
            else
                Fail();
        }

        void Complete()
        {
            StopPlay(GameState.LevelComplete);
            save.coins += runCoins.Finish();
            int bonus = session.Bonus;
            save.coins += bonus;
            int doneBefore = save.LevelsCompleted;
            save.RecordLevelComplete(session.Level.number, session.Stars);
            int gems = GemRewards.ForLevelComplete(save, session.Level.number, doneBefore);
            LogUnlocks(doneBefore);
            save.Save();
            hud.SetCoins(save.coins);
            screens.ShowComplete(session.Stars, RunTotal, gems, save.LevelsCompleted, TowerProgress.Fraction(save));
            sfx.Play(SfxKey.Win);
            fx.Confetti();
            Fx.Haptic();
        }

        /// <summary>Coins earned in the current level or run (Tips included), plus the star bonus after a win.</summary>
        int RunTotal => runCoins.Banked + session.Bonus;

        void Fail()
        {
            StopPlay(GameState.LevelFailed);
            save.coins += runCoins.Finish();
            save.Save();
            screens.ShowFailed();
            sfx.Play(SfxKey.Fail);
        }

        void EndRun()
        {
            StopPlay(GameState.LevelFailed);
            save.coins += runCoins.Finish();
            bool newBest = save.RecordEndlessScore(session.Delivered);
            save.Save();
            screens.ShowEndlessOver(session.Delivered, save.endlessHighScore, newBest);
            sfx.Play(newBest ? SfxKey.Win : SfxKey.Fail);
            if (newBest)
                fx.Confetti();
        }

        void StopPlay(GameState endState)
        {
            State = endState;
            fx.ClearFloaters();
            elevator.InputEnabled = false;
            spawner.Running = false;
            tutorial.Hide();
            LockInput();
            Screen.sleepTimeout = SleepTimeout.SystemSetting;
        }

        /// <summary>LevelComplete: back to Home, where the earned coins fly in.</summary>
        public void Continue()
        {
            if (State == GameState.LevelComplete)
                GoHome(RunTotal);
        }

        public void Retry()
        {
            if (State != GameState.LevelFailed)
                return;
            if (endless)
                LoadEndless();
            else
                LoadLevel(session.Level.number);
        }

        /// <summary>Ready, LevelFailed or the end of an Endless run: back to Home.</summary>
        public void BackHome()
        {
            if (State == GameState.Ready)
                GoHome(0);
            else if (State == GameState.LevelFailed)
                // An Endless run's coins fly in like a win's; a failed level's were already counted in play.
                GoHome(endless ? RunTotal : 0);
        }

        void LockInput()
        {
            inputLockedUntil = Time.unscaledTime + config.endScreenInputLock;
        }

        void Pause()
        {
            if (State != GameState.Playing)
                return;
            State = GameState.Paused;
            Time.timeScale = 0f;
            elevator.InputEnabled = false;
            save.Save();
            Screen.sleepTimeout = SleepTimeout.SystemSetting;
            screens.ShowPaused();
        }

        void Resume()
        {
            State = GameState.Playing;
            Time.timeScale = 1f;
            elevator.InputEnabled = true;
            screens.HideAll();
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
        }

        void OnApplicationPause(bool paused)
        {
            if (paused)
                Pause();
        }

        void OnApplicationFocus(bool focused)
        {
            if (!focused)
                Pause();
        }

        void OnApplicationQuit()
        {
            save?.Save();
        }

        /// <summary>Logs anything that crossed its unlock threshold since this many levels completed.
        /// Stands in for the "Unlocked!" popup until milestone H7.</summary>
        void LogUnlocks(int doneBefore)
        {
            foreach (var id in UnlockSchedule.NewlyUnlocked(doneBefore, save.LevelsCompleted))
                Debug.Log("[ElevatorRush] Unlocked: " + Catalog.NameOf(id) + " (" + id + ")");
        }

        // ---- Editor debug hooks (see DebugKeys) ----

        /// <summary>Wins the current level on the spot, as if the last delivery just landed.</summary>
        public void DebugCompleteLevel()
        {
            if (endless || (State != GameState.Ready && State != GameState.Playing))
                return;
            while (session.State == SessionState.Playing)
                save.coins += runCoins.Add(session.RegisterDelivery(1));
            hud.SetProgress(session.Delivered, session.Level.target);
            Complete();
        }

        public void DebugAddCoins(int amount)
        {
            new Wallet(save).Add(amount);
            save.Save();
            hud.SetCoins(save.coins);
            RefreshHome();
        }

        public void DebugAddGems(int amount)
        {
            new Wallet(save).AddGems(amount);
            save.Save();
            RefreshHome();
        }

        /// <summary>Refreshes the Home HUD and any open panel after the balances changed outside a purchase.</summary>
        void RefreshHome()
        {
            homeHud.Refresh(save);
            if (buildPanel.IsOpen)
                buildPanel.Refresh();
            if (elevatorPanel.IsOpen)
                elevatorPanel.Refresh();
            if (charactersPanel.IsOpen)
                charactersPanel.Refresh();
            if (towersPanel.IsOpen)
                towersPanel.Refresh();
        }

        /// <summary>Wins the next levels with three stars without playing them, then reloads Home or the current level.</summary>
        public void DebugAddCompletedLevels(int count)
        {
            int doneBefore = save.LevelsCompleted;
            int first = save.currentLevel;
            for (int i = 0; i < count; i++)
            {
                int before = save.LevelsCompleted;
                save.RecordLevelComplete(first + i, ScoreSystem.MaxStars);
                GemRewards.ForLevelComplete(save, first + i, before);
            }
            LogUnlocks(doneBefore);
            save.Save();
            if (State == GameState.Home)
                GoHome(0);
            else if (!endless)
                LoadLevel(save.currentLevel);
        }

        public void DebugJumpToLevel(int number)
        {
            LoadLevel(Mathf.Max(1, number));
        }

        public void DebugResetSave()
        {
            SaveData.Delete();
            save = SaveData.Load();
            unlockPopup.Hide();
            dialog.Close();
            CreateShop();
            ApplySettings();
            GoHome(0);
        }
    }
}
