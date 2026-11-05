using System;
using UnityEngine;

namespace ElevatorRush
{
    /// <summary>
    /// Every tunable number in the game. Plain C# so pure classes and tests can use it.
    /// </summary>
    [Serializable]
    public sealed class GameConfig
    {
        // ---- Tuning (plan section 7) ----
        public float floorHeight = 2.2f;
        public float roomWidth = 4.5f;
        public float roomDepth = 2.0f;

        public float riseAccel = 18f;
        public float gravity = 16f;
        public float maxRiseSpeed = 5.5f;
        public float maxFallSpeed = 8f;

        public float snapRange = 0.40f;
        public float snapSpeed = 2.5f;
        public float perfectRange = 0.12f;
        public float dwellTime = 0.18f;
        public float exchangeTime = 0.40f;

        public int cabinCapacity = 4;
        public int maxQueuePerFloor = 4;
        public int startLives = 3;
        public int coinsPerDelivery = 1;
        public int maxCombo = 5;
        public int starBonus = 10;

        // ---- Building layout ----
        public float shaftWidth = 1.7f;
        public float wallThickness = 0.16f;
        public float slabThickness = 0.22f;
        public float doorHeight = 1.75f;
        public float trimDepth = 0.06f;
        public float roofParapetHeight = 0.35f;
        public Vector3 roofMachineSize = new Vector3(1.2f, 0.6f, 1.2f);
        // Wall tint: destination color with reduced saturation, raised brightness.
        public float wallTintSaturation = 0.35f;
        public float wallTintValue = 0.96f;

        // ---- Cabin ----
        public float cabinWidth = 1.45f;
        public float cabinHeight = 1.85f;
        public float cabinDepthFraction = 0.8f;
        public float cabinPanelThickness = 0.08f;
        public float cableWidth = 0.05f;

        // ---- Dock feedback ----
        public float dockFlashTime = 0.3f;
        public float perfectFlashTime = 0.45f;
        public Color perfectFlashColor = Color.white;
        public float ringRadius = 0.42f;
        public float ringThickness = 0.14f;
        public int ringSegments = 48;
        // Ring height as a fraction of the cabin height, and distance in front of the cabin.
        public float ringHeightFraction = 0.55f;
        public float ringFrontOffset = 0.12f;
        public float ringFillOffset = 0.01f;
        public Color ringFillColor = new Color(1f, 1f, 1f);
        public Color ringTrackColor = new Color(0.2f, 0.22f, 0.3f);

        // ---- Floors: queues, delivery spots, wall icon, glow ----
        public float queueFirstOffset = 0.55f;
        public float queueSpacing = 0.55f;
        public float spawnInset = 0.4f;
        public float deliverInset = 0.9f;
        public float deliverSpread = 0.3f;
        public float deliverDepth = 0.45f;
        public float frontDoorDistance = 1.3f;
        public float wallIconSize = 1.0f;
        // The icon sits mid-room and high on the back wall, so decor can stand on both sides and
        // low furniture under it without covering it (home plan 5.2).
        public float wallIconXFraction = 0.5f;
        public float wallIconYFraction = 0.72f;
        public float wallIconOffset = 0.01f;
        // How far a glowing floor's tint moves toward its full destination color.
        public float glowAmount = 0.5f;

        // ---- Passengers ----
        public float passengerBodyHeight = 0.62f;
        public float passengerBodyWidth = 0.34f;
        public float passengerHeadSize = 0.3f;
        public float bubbleHeight = 1.2f;
        public float bubbleSize = 0.42f;
        public float bubbleIconSize = 0.3f;
        public float bubbleIconOffset = 0.01f;
        public Color skinColor = new Color(1f, 0.84f, 0.7f);
        public Color bubbleColor = Color.white;
        public float walkSpeed = 2.2f;
        public float hopHeight = 0.07f;
        public float hopLength = 0.35f;
        public float boardTime = 0.35f;
        public float fadeTime = 0.3f;
        // Riders stand side by side across the cabin, alternating back and front, with smaller,
        // lower bubbles so they stay visible under the cabin roof.
        public float riderSpacing = 0.32f;
        public float riderDepthStagger = 0.22f;
        public float riderBubbleHeight = 1.02f;
        public float riderBubbleScale = 0.75f;
        public int passengerPrewarm = 16;

        // ---- Passenger kinds (plan 5.5) ----
        public float vipPatienceFactor = 0.6f;
        public int vipCoinMultiplier = 3;
        public int bigCoinMultiplier = 2;
        public int bigSlots = 2;
        public float bigWidthScale = 1.5f;
        public Color crownColor = new Color(1f, 0.78f, 0.15f);
        public Vector2 crownSize = new Vector2(0.26f, 0.18f);
        public int crownSides = 6;
        public int crownPoints = 5;
        public float crownBandFraction = 0.45f;
        public float crownPointSize = 0.3f;
        // How far the crown sits down into the head, as a fraction of its height.
        public float crownSink = 0.35f;
        public int sparkleCount = 2;
        public float sparkleSize = 0.2f;
        public float sparkleRadius = 0.34f;
        public float sparkleSpeed = 2.5f;
        public float sparkleTwinkleSpeed = 7f;

        // ---- Patience (plan 5.4) ----
        public float patienceBarGap = 0.07f;
        public float patienceBarWidth = 0.42f;
        public float patienceBarHeight = 0.07f;
        public float patienceBarBorder = 0.02f;
        public int patienceColorSteps = 8;
        public Color patienceTrackColor = new Color(0.18f, 0.2f, 0.26f);
        public Color patienceFullColor = new Color(0.3f, 0.82f, 0.36f);
        public Color patienceEmptyColor = new Color(0.92f, 0.22f, 0.2f);
        public float patienceShakeBelow = 0.25f;
        public float shakeFrequency = 45f;
        public float shakeAmplitude = 0.03f;
        public Color angryColor = new Color(0.9f, 0.25f, 0.2f);
        public float leaveInset = 0.15f;

        // ---- Game flow (plan 5.8) ----
        public float endScreenInputLock = 0.5f;
        public float starRevealDelay = 0.35f;
        public float starPopTime = 0.25f;
        public float starPopScale = 1.6f;
        public float coinCountTime = 0.8f;
        public float holdHintPulseSpeed = 4f;
        public float bannerTime = 1.8f;
        public float bannerPopTime = 0.25f;
        public float bannerPopScale = 1.5f;
        public float tutorialPressPeriod = 1.4f;
        public float tutorialPressScale = 0.85f;
        public Color uiBanner = new Color(1f, 0.45f, 0.25f);

        // ---- Feel (plan M8) ----
        public int sfxVoices = 8;
        public float sfxVolume = 0.9f;
        // Dock and perfect sounds rise in pitch with the combo.
        public float dockPitchStep = 0.08f;
        public int fxFloaterCount = 12;
        public float floatTextTime = 0.9f;
        public float floatTextRise = 150f;
        public float floatTextPopScale = 1.4f;
        public int floatTextFont = 64;
        public int perfectTextFont = 80;
        public float floatTextHeight = 1.4f;
        public float floatTextStack = 0.45f;
        public float floatTextStackPixels = 70f;
        public float floatTextEdgeMargin = 30f;
        public int maxCoinPops = 5;
        public int fxCoinBursts = 4;
        public float coinPopSize = 64f;
        public float coinPopTime = 0.55f;
        public float coinPopStagger = 0.07f;
        public float coinPopArc = 160f;
        public float coinPopStartScale = 1.4f;
        public int confettiCount = 80;
        public Vector2 confettiSize = new Vector2(22f, 34f);
        public float confettiTime = 3.2f;
        public float confettiSpread = 500f;
        public float confettiStartInside = 350f;
        public float confettiSpeed = 500f;
        public float confettiDrift = 220f;
        public float confettiGravity = 700f;
        public float confettiSpin = 540f;
        public int fxPunchSlots = 16;
        public float punchTime = 0.25f;
        public float cabinPunch = 0.12f;
        public float uiPunch = 0.3f;
        public float cameraShakeTime = 0.25f;
        public float cameraShakeAmount = 0.08f;
        public float cameraShakeFrequency = 35f;
        // Post-processing: mild color grading and a vignette, no bloom.
        public float postSaturation = 12f;
        public float postContrast = 8f;
        public float vignetteIntensity = 0.22f;
        public float vignetteSmoothness = 0.45f;
        public Color uiPerfect = new Color(1f, 0.85f, 0.25f);

        // ---- UI ----
        public Vector2 uiReferenceResolution = new Vector2(1080f, 1920f);
        // Portrait: match width so every phone gets the same 1080-wide layout; taller screens get more height.
        public float uiMatchWidthOrHeight = 0f;
        public Color uiText = Color.white;
        public Color uiTextDark = new Color(0.2f, 0.22f, 0.3f);
        public Color uiShadow = new Color(0f, 0f, 0f, 0.35f);
        public Color uiDim = new Color(0.05f, 0.06f, 0.1f, 0.55f);
        public Color uiCard = new Color(1f, 0.98f, 0.95f);
        public Color uiBarBack = new Color(0.15f, 0.17f, 0.24f, 0.75f);
        public Color uiBarFill = new Color(0.36f, 0.85f, 0.42f);
        public Color uiLevelCircle = new Color(1f, 0.62f, 0.2f);
        public Color uiNextCircle = new Color(0.55f, 0.58f, 0.66f);
        public Color uiCoin = new Color(1f, 0.8f, 0.2f);
        public Color uiHeart = new Color(0.95f, 0.27f, 0.33f);
        public Color uiHeartLost = new Color(0.4f, 0.42f, 0.48f, 0.6f);
        public Color uiStarOn = new Color(1f, 0.8f, 0.2f);
        public Color uiStarOff = new Color(0.8f, 0.8f, 0.84f);
        public Color uiCombo = new Color(1f, 0.85f, 0.25f);
        public Color uiNextButton = new Color(0.3f, 0.78f, 0.4f);
        public Color uiRetryButton = new Color(1f, 0.55f, 0.2f);

        public float firstSpawnDelay = 0.8f;

        // ---- Mobile (plan M9) ----
        public int targetFrameRate = 60;
        // Shadows reach this far past the back of the building.
        public float shadowDistanceMargin = 4f;

        // ---- Camera (plan 5.1) ----
        public float cameraFov = 25f;
        public float cameraYaw = 12f;
        public float cameraPitch = 6f;
        // Fractions of the viewport kept free: sides, a band at the top for the HUD, and a band
        // at the bottom for the Ready text.
        public float cameraMargin = 0.06f;
        // Heights of the HUD band (top) and Ready-text band (bottom) in UI reference pixels. The
        // UI matches width, so these become a larger share of the screen on wide tablets.
        public float cameraTopBand = 290f;
        public float cameraBottomBand = 390f;
        public float cameraMaxReserve = 0.6f;
        // Extra world space around the building that should stay in frame.
        public float cameraGroundPadding = 0.6f;
        public float cameraSmoothing = 4f;
        public float cameraNear = 0.3f;
        public float cameraFar = 300f;

        // ---- World (per-theme colors and light live in Theme) ----
        public float shadowStrength = 0.75f;
        public Vector2 groundSize = new Vector2(600f, 600f);
        public float groundThickness = 0.5f;
        public float groundTopY = -0.022f;
        public float streetLayerHeight = 0.012f;
        public float sidewalkDepth = 1.6f;
        public float roadDepth = 4.4f;
        public float roadLineLength = 1.1f;
        public float roadLineGap = 0.9f;
        public float roadLineWidth = 0.12f;
        public float roadLength = 80f;
        // The sky is a gradient quad parented to the camera, near the far plane.
        public float skyDistanceFraction = 0.9f;
        public float skyOverscan = 1.1f;
        public int skyTextureHeight = 64;
        public Color lampBulbColor = new Color(1f, 0.9f, 0.55f);
        public float lampBulbSize = 0.32f;

        // Imported model materials without textures are swapped for shared palette colors by name.
        public string[] modelMaterialNames =
        {
            "wood", "woodDark", "woodBark", "woodBarkDark", "leafsGreen", "leafsDark", "leafsFall",
            "grass", "plant", "carpet", "carpetWhite", "carpetBlue", "metal", "metalDark", "lamp",
            "metalLight", "metalMedium", "carpetDarker",
        };
        public Color[] modelMaterialColors =
        {
            new Color(0.86f, 0.66f, 0.46f), new Color(0.62f, 0.44f, 0.3f), new Color(0.58f, 0.4f, 0.27f),
            new Color(0.42f, 0.3f, 0.22f), new Color(0.36f, 0.72f, 0.32f), new Color(0.2f, 0.48f, 0.34f),
            new Color(0.9f, 0.55f, 0.25f), new Color(0.42f, 0.74f, 0.32f), new Color(0.36f, 0.7f, 0.36f),
            new Color(0.92f, 0.5f, 0.45f), new Color(0.96f, 0.95f, 0.92f), new Color(0.4f, 0.6f, 0.9f),
            new Color(0.74f, 0.77f, 0.82f), new Color(0.36f, 0.38f, 0.44f), new Color(1f, 0.94f, 0.72f),
            new Color(0.86f, 0.87f, 0.9f), new Color(0.55f, 0.57f, 0.62f), new Color(0.36f, 0.31f, 0.33f),
        };

        // ---- Character model and animation ----
        public float characterHeight = 0.95f;
        public float characterTurnSpeed = 10f;
        // Yaw that faces the camera, and how far a walker turns toward its direction (3/4 view).
        public float faceCameraYaw = 180f;
        public float walkYawOffset = 55f;
        public float walkAnimSpeed = 1.6f;
        public float animFadeTime = 0.12f;
        public float cheerTime = 0.6f;
        // Procedural animation when there is no rigged character.
        public float idleBobHeight = 0.03f;
        public float idleBobSpeed = 3f;
        public float squashTime = 0.15f;
        public float squashAmount = 0.15f;

        public Color cabinColor = new Color(0.92f, 0.94f, 0.97f);
        public Color cabinTrimColor = new Color(0.25f, 0.28f, 0.36f);
        public Color cableColor = new Color(0.15f, 0.15f, 0.18f);

        // ---- Home screen (home plan 3) ----
        // UI bands the camera keeps free on Home: the top row, and the teaser, Play button and tabs.
        public float homeCameraTopBand = 230f;
        public float homeCameraBottomBand = 760f;
        // The idle cabin glides between unlocked floors and waits a moment at each.
        public float idleCabinSpeed = 1.6f;
        public float idleCabinPause = 1.2f;
        public float idleCabinSmoothing = 0.35f;
        // Construction site above the top unlocked floor: posts, scaffolding and a sign.
        public float constructionPostSize = 0.12f;
        public float scaffoldPoleSize = 0.05f;
        public float scaffoldPlankThickness = 0.05f;
        public float scaffoldDepth = 0.45f;
        public int scaffoldColumns = 4;
        public int scaffoldLevels = 2;
        public Vector2 constructionSignSize = new Vector2(1.5f, 0.6f);
        public float constructionSignHeight = 0.75f;
        public float constructionSignThickness = 0.06f;
        public Color constructionSteelColor = new Color(0.92f, 0.55f, 0.18f);
        public Color scaffoldColor = new Color(0.62f, 0.66f, 0.72f);
        public Color plankColor = new Color(0.78f, 0.6f, 0.4f);
        public Color constructionSignColor = new Color(1f, 0.82f, 0.2f);
        public Color concreteColor = new Color(0.7f, 0.7f, 0.7f);
        // Coins earned in a level fly from the screen center into the Home counter.
        public int homeCoinFlyCount = 12;
        public Color uiPlayButton = new Color(0.3f, 0.78f, 0.4f);
        public Color uiTabButton = new Color(0.22f, 0.26f, 0.36f, 0.92f);
        public Color uiRedDot = new Color(0.95f, 0.22f, 0.25f);
        public Color uiToggleOn = new Color(0.3f, 0.78f, 0.4f);
        public Color uiToggleOff = new Color(0.55f, 0.58f, 0.66f);
        public Color uiSignText = new Color(0.2f, 0.18f, 0.12f);

        // ---- Room decor (home plan 5) ----
        // The color band: the part of the tinted back wall around the icon that decor never covers.
        public float colorBandMargin = 0.12f;
        // Furniture stands in the back half, behind the queue and the delivery spots.
        public float decorFrontZ = 0.52f;
        public float decorWallGap = 0.02f;
        public float decorSideInset = 0.1f;
        public float decorGap = 0.06f;
        public float decorLeftMaxHeight = 1.6f;
        public float decorRightMaxHeight = 1.12f;
        public float decorArtDepth = 0.06f;
        public float decorFloorThickness = 0.012f;
        public float decorWallThickness = 0.02f;
        // Pattern tile sizes in meters for code-made flooring and wall finishes.
        public float decorTileSize = 0.5f;
        public float decorMarbleTileSize = 1.6f;
        public float decorStarSize = 0.32f;
        // The star sits clear of the door lintel, which hides the left edge of the back wall from the camera.
        public float decorStarX = 0.85f;
        public float decorStarInset = 0.2f;
        public Color decorGold = new Color(0.9f, 0.72f, 0.2f);
        // Purchase feedback.
        public float dropInHeight = 0.9f;
        public float dropInTime = 0.5f;
        public int purchaseConfetti = 26;
        public float purchaseConfettiSpeed = 650f;

        // ---- Build mode (home plan 5.3) ----
        public float buildCameraBottomBand = 300f;
        public float buildSheetHeight = 760f;
        public float markerSize = 104f;
        public int thumbnailSize = 256;
        public int thumbnailCacheSize = 24;
        public Color thumbnailBackground = new Color(0.93f, 0.91f, 0.87f);
        public Color uiMarker = new Color(1f, 1f, 1f, 0.95f);
        public Color uiMarkerIcon = new Color(0.25f, 0.6f, 0.95f);
        public Color uiEquipped = new Color(0.3f, 0.78f, 0.4f);
        public Color uiOwned = new Color(0.36f, 0.62f, 0.95f);
        public Color uiCantAfford = new Color(0.62f, 0.62f, 0.66f);

        // ---- Gems (monetization plan 2) ----
        public Color uiGem = new Color(0.4f, 0.88f, 1f);
        // Buttons that spend gems.
        public Color uiGemButton = new Color(0.6f, 0.38f, 0.95f);
        public float confirmDialogInputLock = 0.25f;

        // ---- Elevator upgrades (home plan 6) ----
        // Each extra Capacity slot widens the cabin and the shaft so every rider stays visible.
        public float cabinWidthPerExtraSlot = 0.32f;
        // Cabin looks by total tiers owned: trim and lights, then glass and gold.
        public Color cabinTrimGold = new Color(0.9f, 0.72f, 0.2f);
        public Color cabinLightColor = new Color(1f, 0.93f, 0.7f);
        public Color cabinGlassColor = new Color(0.72f, 0.88f, 0.96f);
        public float cabinTrimThickness = 0.05f;
        public float cabinLightHeight = 0.05f;
        public float upgradeSheetHeight = 1300f;

        // ---- Character skins (home plan 7) ----
        public float skinSheetHeight = 1080f;
        // The camera frames this box around the preview character, feet at the bottom.
        public Vector3 skinPreviewBox = new Vector3(1.3f, 1.35f, 0.8f);
        // The preview sits above a tall sheet, so the UI bands may take more of the screen here.
        public float skinPreviewMaxReserve = 0.82f;

        // ---- Tower skins (home plan 8) ----
        public float towerTrimSize = 0.05f;
        // Wide pilasters on both sides of the façade show the skin's exterior from the front.
        public float towerPilasterWidth = 0.32f;
        public float towerFrameWidth = 0.08f;
        public Vector2 towerWindowSize = new Vector2(1.1f, 0.9f);
        public float towerWindowFrame = 0.06f;
        public float towerWindowDepth = 0.03f;
        public float towerInteriorThickness = 0.01f;
        public float towerCrenelWidth = 0.28f;
        public float towerCrenelHeight = 0.22f;
        public float towerSheetHeight = 720f;

        // ---- City backdrop and street life ----
        // Buildings behind the tower, in rows that get taller and denser with distance.
        public int cityRows = 3;
        public float cityFirstRowZ = 16f;
        public float cityRowStep = 11f;
        public float cityHalfWidth = 46f;
        public float cityGap = 1.2f;
        public Vector2 cityHeight = new Vector2(4f, 8f);
        public float cityRowHeightGain = 3.5f;
        // Aerial haze: each row is tinted toward the theme's horizon color, more with distance.
        public float cityHazeNear = 0.35f;
        public float cityHazeStep = 0.15f;
        // The block straight behind the tower stays lower so the tower reads against the sky.
        public float cityClearHalfWidth = 6f;
        public float cityClearHeightFactor = 0.55f;
        public Color cityFallbackColor = new Color(0.62f, 0.66f, 0.74f);
        public int cityFallbackSeed = 17;
        // Home street life: cars in both lanes, pedestrians on both sidewalks.
        public int streetCars = 4;
        public Vector2 carSpeed = new Vector2(3.2f, 5.2f);
        public Vector2 carGap = new Vector2(0.8f, 3.5f);
        public float carHeight = 0.85f;
        public float trafficHalfLength = 22f;
        public int streetPedestrians = 6;
        public float pedestrianHalfLength = 14f;

        /// <summary>A shallow copy; arrays are shared and must be treated as read-only.</summary>
        public GameConfig Clone() => (GameConfig)MemberwiseClone();
    }
}
