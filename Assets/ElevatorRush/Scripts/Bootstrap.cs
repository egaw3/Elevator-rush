using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

namespace ElevatorRush
{
    /// <summary>
    /// Entry point. Builds the whole game from code after the first scene loads, so the
    /// template's default scene needs no editing.
    /// </summary>
    public sealed class Bootstrap : MonoBehaviour
    {
        static Bootstrap instance;

        GameConfig config;
        MaterialLibrary materials;
        AssetLibrary assets;
        SpriteFactory sprites;
        InputReader input;
        Building building;
        ElevatorController elevator;
        PassengerSpawner spawner;
        CameraRig cameraRig;
        GameManager gameManager;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            instance = null;
            Billboard.Target = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (instance != null)
                return;
            var go = new GameObject("ElevatorRush");
            DontDestroyOnLoad(go);
            instance = go.AddComponent<Bootstrap>();
        }

        void Awake()
        {
            config = new GameConfig();
            Application.targetFrameRate = config.targetFrameRate;
            materials = new MaterialLibrary();
            assets = new AssetLibrary(materials, config);
            sprites = new SpriteFactory();
            // The downloaded display font is already heavy; only the built-in one needs bold.
            UiBuilder.FontStyle = assets.HasCustomFontLoaded() ? FontStyle.Normal : FontStyle.Bold;

            var camera = TakeOverTemplateScene();
            var sun = CreateSun();

            var environment = new GameObject("Environment").AddComponent<Environment>();
            environment.transform.SetParent(transform, false);
            environment.Init(config, materials, assets, sun, camera);

            input = gameObject.AddComponent<InputReader>();

            building = new GameObject("Building").AddComponent<Building>();
            building.transform.SetParent(transform, false);
            var decorator = new RoomDecorator(config, materials, assets, sprites);
            building.Init(config, materials, assets, sprites, decorator);

            // Passengers live in world space at the root, which stays at the origin.
            spawner = new GameObject("Passengers").AddComponent<PassengerSpawner>();
            spawner.transform.SetParent(transform, false);
            spawner.Init(config, materials, assets, sprites, new CharacterSkinner(materials, assets));

            elevator = new GameObject("Elevator").AddComponent<ElevatorController>();
            elevator.transform.SetParent(transform, false);
            elevator.Init(config, input, materials, assets, spawner.transform);

            Billboard.Target = camera.transform;
            cameraRig = camera.gameObject.AddComponent<CameraRig>();
            cameraRig.Init(config, camera);

            CreateEventSystem();
            var canvas = UiBuilder.CreateCanvas("UI", transform, config, 0);
            var uiRoot = (RectTransform)canvas.transform;
            canvas.gameObject.AddComponent<CanvasFit>().Init(config);
            // HUD, hints and the Ready screen stay inside the safe area; dimmers and effects cover the whole screen.
            var safeRoot = UiBuilder.Stretch(UiBuilder.Rect("SafeArea", uiRoot));
            safeRoot.gameObject.AddComponent<SafeArea>();

            gameManager = gameObject.AddComponent<GameManager>();

            var hud = new GameObject("Hud").AddComponent<Hud>();
            hud.transform.SetParent(uiRoot, false);
            hud.Init(config, assets.UiFont, sprites, safeRoot);

            var tutorial = new GameObject("Tutorial").AddComponent<Tutorial>();
            tutorial.transform.SetParent(uiRoot, false);
            tutorial.Init(config, assets.UiFont, sprites, safeRoot);

            var home = new GameObject("Home").AddComponent<HomeController>();
            home.transform.SetParent(transform, false);
            home.Init(config, building, elevator, spawner, environment, cameraRig);
            var wanderers = home.gameObject.AddComponent<HomeWanderers>();
            wanderers.Init(config, spawner, building);
            var streetLife = home.gameObject.AddComponent<StreetLife>();
            streetLife.Init(config, assets, materials, spawner, building);

            var sfx = new GameObject("Sfx").AddComponent<Sfx>();
            sfx.transform.SetParent(transform, false);
            sfx.Init(config, assets);
            // Fx is initialized last (its layer must draw over everything) but referenced earlier.
            var fx = new GameObject("Fx").AddComponent<Fx>();
            fx.transform.SetParent(transform, false);

            // Build mode sits under the Home HUD, so the tabs and Play take touches before the floors.
            var thumbnails = new ThumbnailRenderer(config, decorator, transform);
            var buildPanel = new GameObject("BuildPanel").AddComponent<BuildPanel>();
            buildPanel.transform.SetParent(uiRoot, false);
            buildPanel.Init(config, assets.UiFont, sprites, canvas, uiRoot, safeRoot, camera, building, home, thumbnails,
                decorator.Layout, fx, sfx);

            var elevatorPanel = new GameObject("ElevatorPanel").AddComponent<ElevatorPanel>();
            elevatorPanel.transform.SetParent(uiRoot, false);
            elevatorPanel.Init(config, assets.UiFont, sprites, uiRoot, fx, sfx);

            var charactersPanel = new GameObject("CharactersPanel").AddComponent<CharactersPanel>();
            charactersPanel.transform.SetParent(uiRoot, false);
            charactersPanel.Init(config, assets.UiFont, sprites, uiRoot, building, home, spawner, fx, sfx);

            var towersPanel = new GameObject("TowersPanel").AddComponent<TowersPanel>();
            towersPanel.transform.SetParent(uiRoot, false);
            towersPanel.Init(config, assets.UiFont, sprites, uiRoot, fx, sfx);

            var homeHud = new GameObject("HomeHud").AddComponent<HomeHud>();
            homeHud.transform.SetParent(uiRoot, false);
            homeHud.Init(config, assets.UiFont, sprites, canvas, uiRoot, safeRoot, camera, home,
                gameManager.Play, gameManager.PlayEndless, gameManager.OnTabOpened, gameManager.ToggleSetting, gameManager.UnlockNextFloor);

            var screens = new GameObject("Screens").AddComponent<Screens>();
            screens.transform.SetParent(uiRoot, false);
            screens.Init(config, assets.UiFont, sprites, uiRoot, safeRoot, gameManager.Continue, gameManager.Retry, gameManager.BackHome);

            // Unlock popups cover everything except the effects.
            var unlockPopup = new GameObject("UnlockPopup").AddComponent<UnlockPopup>();
            unlockPopup.transform.SetParent(uiRoot, false);
            unlockPopup.Init(config, assets.UiFont, sprites, uiRoot, fx, sfx);

            // Gem spend confirmations and messages cover the panels and popups.
            var dialog = new GameObject("ConfirmDialog").AddComponent<ConfirmDialog>();
            dialog.transform.SetParent(uiRoot, false);
            dialog.Init(config, assets.UiFont, sprites, uiRoot);

            // Effects go last so they draw over the HUD and screens.
            fx.Init(config, assets.UiFont, sprites, canvas, camera, hud, sfx);

            // GameManager opens Home in its Start.
            gameManager.Init(config, input, building, elevator, spawner, cameraRig, hud, screens, tutorial, environment, sfx, fx,
                home, homeHud, buildPanel, elevatorPanel, charactersPanel, towersPanel, unlockPopup, wanderers, streetLife, dialog);
            gameObject.AddComponent<DebugKeys>().Init(gameManager);
        }

        void CreateEventSystem()
        {
            if (EventSystem.current != null)
                return;
            var go = new GameObject("EventSystem");
            go.transform.SetParent(transform, false);
            go.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
            go.AddComponent<InputSystemUIInputModule>();
#else
            go.AddComponent<StandaloneInputModule>();
#endif
        }

        /// <summary>
        /// Reuses the template scene's camera and turns off its lights and volumes so nothing is
        /// duplicated. Walks the scene roots once at startup; gameplay code never searches.
        /// </summary>
        Camera TakeOverTemplateScene()
        {
            Camera reused = null;
            var roots = SceneManager.GetActiveScene().GetRootGameObjects();
            foreach (var root in roots)
            {
                foreach (var cam in root.GetComponentsInChildren<Camera>(true))
                {
                    if (reused == null)
                        reused = cam;
                    else
                        cam.gameObject.SetActive(false);
                }
                foreach (var light in root.GetComponentsInChildren<Light>(true))
                    light.gameObject.SetActive(false);
                foreach (var volume in root.GetComponentsInChildren<Volume>(true))
                    volume.gameObject.SetActive(false);
            }

            if (reused == null)
            {
                var go = new GameObject("Main Camera") { tag = "MainCamera" };
                go.transform.SetParent(transform, false);
                reused = go.AddComponent<Camera>();
            }

            return reused;
        }

        /// <summary>The one shadow-casting light. Its color and angle come from the theme.</summary>
        Light CreateSun()
        {
            var sunGo = new GameObject("Sun");
            sunGo.transform.SetParent(transform, false);
            var sun = sunGo.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = config.shadowStrength;
            RenderSettings.sun = sun;
            RenderSettings.skybox = null;
            return sun;
        }
    }
}
