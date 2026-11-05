# Elevator Rush: build plan

A 3D hypercasual mobile game in Unity. This file is the full spec. Build it milestone by milestone, in order, and stop after each milestone so it can be play-tested.

## 1. The game in one paragraph

The player controls an elevator in a cutaway 3D building with one input: hold anywhere on the screen to make the cabin rise, release to let it fall. Passengers wait on floors, each wanting a specific destination floor shown by color and icon. Hovering the cabin at a floor docks it, riders for that floor get off, and waiting passengers get on. Waiting passengers have a patience bar; when it empties they leave and the player loses a life. The game is a sequence of short levels, each with a delivery target. Reaching the target wins the level; losing three lives fails it.

## 2. Look and feel target

The game should look like the 3D hypercasual games seen in mobile ads:

- A "dollhouse" view: the building's front wall is missing, so every room and the elevator shaft are visible from the front.
- Chunky, low-poly, rounded shapes. Flat saturated colors, no realistic textures.
- Simple stylized characters (stickman or capsule-style people).
- Soft lighting with one sun, soft shadows, a gradient sky, and a small street scene at the base of the building.
- A perspective camera with a narrow field of view, turned slightly to the side so depth is visible.
- Big readable UI: level bar at the top, large rounded font, confetti on a win.

Gameplay is still two-dimensional. Everything that matters happens on the X-Y plane at Z = 0; depth is visual only.

## 3. Technical constraints

Follow these strictly. They exist so the whole game can be built from files without manual work in the Unity Editor.

- **Unity version:** Unity 6 LTS (6000.x), **Universal 3D** (URP) template. Check `ProjectSettings/ProjectVersion.txt` and adapt to the installed version. If the project was created from a 2D template, stop and say so, because the 2D renderer will not light 3D meshes correctly.
- **The scene is built from code at runtime.** Do not hand-edit `.unity`, `.prefab`, `.mat` or `.asset` YAML files. A bootstrap class with `[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]` creates every GameObject, light and camera, so the game runs from the template's default scene. Disable or reuse the template scene's own camera and light so nothing is duplicated.
- **Assets that must exist on disk** (materials, import settings) are created by editor scripts through the `AssetDatabase` API, never by writing YAML.
- **Loaded by key.** Models, audio and fonts live under `Assets/ElevatorRush/Resources/` and are loaded through one `AssetLibrary` class. Every key has a primitive fallback (cube, capsule, generated tone, built-in font), so the game always runs even if an asset is missing.
- **Primitives first.** Milestones 1 to 6 use only Unity primitives and procedural shapes. Downloaded art replaces them in milestone 7. Do not download anything before then.
- **Materials:** URP Lit. One shared material per palette color, created once and cached. Do not create a material per object.
- **UI:** uGUI built in code with `UnityEngine.UI.Text`. Use the built-in font (`Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")`) until milestone 7 replaces it. Do not use TextMeshPro, because it needs a manual resource import.
- **Input:** one boolean, `IsHolding`, exposed by an `InputReader`. With the Input System package use `Pointer.current.press.isPressed` (covers touch and mouse) plus the space key for editor testing. Wrap it in `#if ENABLE_INPUT_SYSTEM` with a legacy `Input.GetMouseButton(0)` fallback. Touches that start on a UI button do not count.
- **No physics engine.** The cabin and passengers are moved by setting transforms. No Rigidbody or colliders.
- **Packages:** Unity registry packages only. No ads or analytics SDKs and no in-app purchases in this plan.
- **Config:** tunable values live in one plain C# class, `GameConfig`, with the defaults from section 7.
- **Target:** Android and iOS, portrait only, 60 fps on a mid-range phone.

## 4. File layout

```
Assets/ElevatorRush/
  ElevatorRush.asmdef            references Unity.InputSystem, UnityEngine.UI, URP runtime
  CREDITS.md                     every downloaded asset with source and license
  Scripts/
    Bootstrap.cs                 entry point, builds the scene
    GameConfig.cs                all tunable numbers
    GameManager.cs               state machine (section 5.8)
    InputReader.cs               IsHolding
    AssetLibrary.cs              load by key with primitive fallback
    MaterialLibrary.cs           shared materials per color
    Building.cs                  builds floors and shaft, floor index to world Y
    Floor.cs                     color, icon, waiting queue, glow
    FloorPalette.cs              destination color and icon per floor index
    Theme.cs                     sky, wall, prop and light colors per theme
    Environment.cs               street, props, sky, lighting
    ElevatorController.cs        movement, hover detection, docking
    ElevatorMotion.cs            pure C# movement and docking math
    Passenger.cs                 state machine and visuals
    PassengerKind.cs             Normal, Vip, Big
    PassengerSpawner.cs          spawn timing and destination choice
    LevelDefinition.cs           data for one level
    LevelCatalog.cs              hand-made levels 1 to 20
    LevelGenerator.cs            pure C# generator for level 21 and up
    LevelSession.cs              pure C# progress of the level being played
    EndlessRamp.cs               pure C# difficulty curves for Endless mode
    ScoreSystem.cs               pure C# coins, combo, lives, stars
    SaveData.cs                  pure C# progress, stored as JSON in PlayerPrefs
    Hud.cs                       level bar, lives, coins, combo
    Screens.cs                   Ready, LevelComplete, LevelFailed, Paused
    Tutorial.cs                  hint overlays for levels 1 and 4
    CameraRig.cs                 fits the building to the screen, shake
    Billboard.cs                 makes icons and bars face the camera
    SpriteFactory.cs             procedural icon and UI sprites, cached
    Sfx.cs                       audio by key, procedural fallback tones
    Fx.cs                        confetti, coin pops, floating text
    Pool.cs                      generic object pool
    SafeArea.cs                  fits UI inside Screen.safeArea
  Editor/
    ElevatorRush.Editor.asmdef
    AssetImportRules.cs          AssetPostprocessor for models and audio
    MobileSetup.cs               menu item that applies player settings
  Resources/
    Models/  Audio/  Fonts/      filled in milestone 7
  Tests/EditMode/
    ElevatorRush.Tests.asmdef
    ElevatorMotionTests.cs  ScoreSystemTests.cs  LevelGeneratorTests.cs
    LevelSessionTests.cs    EndlessRampTests.cs  SaveDataTests.cs
```

Keep the classes marked "pure C#" free of scene dependencies so they can be unit tested.

## 5. Game rules

### 5.1 World layout

- Gameplay plane is X-Y at Z = 0. Floor 0 is the ground floor.
- The elevator shaft runs up the left side of the building. Each floor is a room to the right of the shaft: a floor slab, a back wall and a right side wall, with the front open.
- Rooms are about 2 units deep. Passengers queue along X on the floor slab, lined up toward the shaft.
- The cabin is an open-fronted box with a cable going up to the roof. Riders stand inside it and are visible.
- Each floor's back wall and door frame are tinted with that floor's destination color at reduced saturation, and the floor's icon is drawn large on the back wall.
- The building stands on a street with a few props. Props never overlap the gameplay area.
- **Camera:** perspective, field of view about 25 degrees, turned about 12 degrees to the side and tilted about 6 degrees down. `CameraRig` computes its distance by projecting the building's bounding box corners so the whole building plus a margin always fits in portrait, for any aspect ratio. When floors are added, the camera moves back smoothly.

### 5.2 Elevator movement

- While `IsHolding`: velocity increases by `riseAccel` per second, up to `maxRiseSpeed`.
- Otherwise: velocity decreases by `gravity` per second, down to `-maxFallSpeed`.
- The cabin rests on the ground floor (velocity set to 0) and is clamped at the top floor (velocity set to 0).
- Falling is faster than rising on purpose, so high floors cost more time.

### 5.3 Docking

- Each frame, find the nearest floor. The cabin is "hovering" at it when the distance is below `snapRange` and the absolute velocity is below `snapSpeed`.
- Docking only happens if an exchange is possible at that floor: at least one rider wants this floor, or at least one waiting passenger fits in the cabin's free capacity.
- When hovering with an exchange possible, a dwell timer fills. At `dwellTime` the cabin docks. Show the dwell as a ring filling on the cabin.
- **Perfect dock:** if the distance is below `perfectRange` when hovering begins, dock immediately with no dwell and count it as perfect.
- On docking, the cabin snaps to the floor's Y, velocity is set to 0, and input is ignored for `exchangeTime` while the exchange plays out. Then normal movement resumes.
- Because docking needs a possible exchange, the cabin never re-docks at a floor with nothing to do, and no cooldown is needed.

### 5.4 Passengers and destinations

- A passenger's body is tinted with the color of its destination floor. A billboarded bubble above its head shows the destination floor's icon.
- A passenger never spawns with its own floor as the destination.
- States: `Waiting`, `Boarding`, `Riding`, `Exiting`, `Leaving` (angry, patience ran out).
- Exchange order at a dock: riders for this floor exit first, then waiting passengers board in queue order while they fit.
- Riders have no patience timer.
- Any floor that is the destination of a current rider glows softly.
- Patience is a billboarded bar above a waiting passenger, going from green to red. Below 25 percent the passenger shakes.
- When patience reaches 0, the passenger walks off to the right and the player loses one life.
- Each floor holds at most `maxQueuePerFloor` waiting passengers. The spawner skips full floors.
- Delivered passengers walk into the room and fade out, or walk out of the front door on the ground floor.

### 5.5 Passenger kinds

| Kind | Look | Rule | First appears |
|---|---|---|---|
| Normal | plain body | as above | level 1 |
| VIP | gold crown, sparkle | patience is 60 percent of normal, pays 3 times the coins | level 8 |
| Big | 1.5 times wider | takes 2 cabin slots, pays 2 times the coins | level 12 |

A Big passenger at the front of a queue blocks the passengers behind it until it fits.

### 5.6 Coins, combo, lives, stars

- Each delivery pays `coinsPerDelivery` multiplied by the combo multiplier and the passenger kind's multiplier.
- Each perfect dock raises the combo by 1, up to `maxCombo`. A normal dock resets the combo to 1.
- Lives start at `startLives` every level.
- Stars at the end of a level: 3 if no life was lost, 2 if one was lost, 1 if two were lost.
- Level bonus: `starBonus` coins per star, doubled on rush levels.
- Coins are saved between sessions. Spending them (skins) is outside this plan.

### 5.7 Levels

A level is a `LevelDefinition`:

| Field | Meaning |
|---|---|
| number | 1-based level number |
| floors | number of floors, 3 to 8 |
| groundOnly | true if every passenger wants floor 0 |
| target | deliveries needed to win |
| spawnInterval | seconds between spawns |
| patience | seconds of patience for a Normal passenger |
| vipChance, bigChance | 0 to 1, chance per spawn |
| rush | true for a rush level |
| theme | index of the visual theme |
| seed | random seed, equal to the level number |

Rules:

- The spawner keeps spawning until the target is reached. The level is won the moment the last required delivery completes.
- Three lost lives fails the level. The player retries the same level.
- Spawning uses `System.Random(seed)`, so a level plays the same way each attempt.
- **Rush level:** every 5th level. Faster spawns, higher target, a "Rush hour" banner at the start, double level bonus.
- **Themes** change every 10 levels and cycle: City Office (day), Seaside Hotel (sunset), Night Tower (night). A theme changes sky, wall tint, props and light color. It never changes the destination colors in section 8.
- **Tutorial hints:** level 1 shows an animated hand and "Hold to rise, release to fall" until the first dock. Level 4 shows "Match the color" until the first delivery to a floor other than the ground floor.

Hand-made levels in `LevelCatalog`:

| Level | Floors | Destinations | Target | Spawn s | Patience s | VIP | Big | Notes |
|---|---|---|---|---|---|---|---|---|
| 1 | 3 | ground only | 4 | 4.0 | 30 | 0 | 0 | hint: hold to rise |
| 2 | 3 | ground only | 6 | 3.5 | 26 | 0 | 0 | |
| 3 | 4 | ground only | 8 | 3.2 | 24 | 0 | 0 | |
| 4 | 4 | mixed | 8 | 3.5 | 26 | 0 | 0 | hint: match the color |
| 5 | 4 | mixed | 12 | 2.8 | 22 | 0 | 0 | rush |
| 6 | 5 | mixed | 10 | 3.0 | 22 | 0 | 0 | |
| 7 | 5 | mixed | 12 | 2.8 | 21 | 0 | 0 | |
| 8 | 5 | mixed | 12 | 2.8 | 21 | 0.15 | 0 | VIP introduced |
| 9 | 5 | mixed | 14 | 2.6 | 20 | 0.10 | 0 | |
| 10 | 5 | mixed | 18 | 2.2 | 19 | 0.10 | 0 | rush, unlocks Endless |
| 11 | 6 | mixed | 14 | 2.6 | 20 | 0.10 | 0 | new theme |
| 12 | 6 | mixed | 14 | 2.6 | 19 | 0.05 | 0.15 | Big introduced |
| 13 | 6 | mixed | 16 | 2.4 | 18 | 0.10 | 0.10 | |
| 14 | 6 | mixed | 16 | 2.4 | 18 | 0.10 | 0.10 | |
| 15 | 6 | mixed | 22 | 2.0 | 17 | 0.10 | 0.10 | rush |
| 16 | 7 | mixed | 16 | 2.4 | 18 | 0.10 | 0.10 | |
| 17 | 7 | mixed | 18 | 2.2 | 17 | 0.10 | 0.10 | |
| 18 | 7 | mixed | 18 | 2.2 | 16 | 0.12 | 0.12 | |
| 19 | 7 | mixed | 20 | 2.0 | 16 | 0.12 | 0.12 | |
| 20 | 7 | mixed | 26 | 1.8 | 15 | 0.12 | 0.12 | rush |

From level 21 on, `LevelGenerator` builds the definition from the number `n`, with `k = n - 21`:

- floors = min(8, 7 + k / 15), integer division
- target = min(30, 18 + k / 3), multiplied by 1.3 and rounded on rush levels
- spawnInterval = max(1.3, 2.0 - 0.01 * k), multiplied by 0.85 on rush levels
- patience = max(11, 16 - 0.1 * k)
- vipChance = 0.12, bigChance = 0.12
- rush = n is divisible by 5
- theme = ((n - 1) / 10) mod 3

These numbers are a first guess and will need tuning after play-testing.

### 5.8 Game flow

- **Ready:** the building for the current level is visible. Shows "Level N", the target ("Deliver 8"), total coins, and "Hold to start". An "Endless" button appears once level 10 is complete.
- **Playing:** HUD shows the level bar (current level number, a fill bar of deliveries against the target, next level number), lives, coins and combo.
- **LevelComplete:** confetti, stars appearing one by one, coins earned counting up, "Next" button. Progress is saved before the screen appears.
- **LevelFailed:** "Retry" button. No coins are lost.
- **Paused:** entered when the app loses focus during Playing. Resumes on the next touch.
- After any end screen, ignore input for 0.5 seconds so a held finger does not skip it.
- Moving to another level rebuilds the building and environment without reloading the scene.

### 5.9 Endless mode

Unlocked after level 10. No target; the run ends at three lost lives and the score is the number of deliveries, with a saved high score. `EndlessRamp` drives difficulty from the delivery count:

- Floors: 4 at the start, then 5, 6, 7 and 8 at 20, 40, 70 and 110 deliveries.
- Ground-only destinations for the first 10 deliveries, mixed after that.
- Spawn interval from 3.5 s to 1.3 s and patience from 22 s to 11 s, linear over the first 120 deliveries.
- VIP and Big chance 0 until 30 deliveries, then 0.1 each.

### 5.10 Saving

`SaveData` holds: current level, coins, best stars per level, endless high score, and whether Endless is unlocked. It is serialized with `JsonUtility` into a single `PlayerPrefs` key. A missing or corrupt save loads as a fresh one.

## 6. Assets

Used from milestone 7 on. Assets may be found and downloaded from the internet under these rules.

- **Licenses allowed:** CC0 / public domain for models, textures and audio. SIL Open Font License for fonts. Nothing else: no attribution-required, non-commercial or "free for personal use" assets, no Unity Asset Store content, nothing taken from other games.
- **Check the license on the asset's own download page** before downloading. If the license is unclear, skip the asset.
- **Places to look first:** Kenney (kenney.nl), Quaternius, Poly Pizza (CC0 items only), Poly Haven, ambientCG, Google Fonts.
- **Record every asset** in `Assets/ElevatorRush/CREDITS.md`: name, author, page URL, license, date downloaded, and where it is used.
- **One style.** Prefer taking characters, building pieces and props from the same author or pack family so the game looks consistent. Low-poly with flat colors.
- **Formats:** FBX preferred. If a needed model exists only as GLB, add the Unity registry package `com.unity.cloud.gltfast`. Audio as OGG or WAV. Fonts as TTF or OTF.
- **Keep only what is used.** Copy the individual files the game needs into `Resources/`, not whole packs. Target under 40 MB of assets.
- **Import rules** are enforced by `AssetImportRules` (an `AssetPostprocessor`): consistent scale, no imported cameras or lights, mesh compression, read/write off, audio compressed and forced to mono for effects.
- **Tinting:** downloaded character meshes are re-materialed with the shared palette materials so destination colors stay exact.
- **If a download fails or nothing suitable is found,** keep the primitive fallback for that key and list it as outstanding in the milestone summary.

Asset list:

| Key | What | Fallback |
|---|---|---|
| character | one simple low-poly person, rigged if available | capsule with a sphere head |
| crown | small crown for the VIP | yellow cone |
| cabin | elevator cabin, or built from cubes | cubes |
| props_street | 4 to 6 props: tree, lamp post, bench, car, bush | colored cubes and spheres |
| props_room | 4 to 6 props: desk, plant, sofa, bed, lamp | none |
| font_ui | one bold rounded display font | built-in font |
| sfx_dock, sfx_perfect, sfx_deliver, sfx_coin, sfx_angry, sfx_win, sfx_fail, sfx_button | short effects | generated tones |
| music_loop | one light looping track, optional | none |

## 7. Default tuning values

World units. One floor is `floorHeight` tall.

| Name | Value |
|---|---|
| floorHeight | 2.2 |
| roomWidth | 4.5 |
| roomDepth | 2.0 |
| riseAccel | 18 |
| gravity | 16 |
| maxRiseSpeed | 5.5 |
| maxFallSpeed | 8 |
| snapRange | 0.30 |
| snapSpeed | 1.5 |
| perfectRange | 0.08 |
| dwellTime | 0.30 s |
| exchangeTime | 0.40 s |
| cabinCapacity | 4 |
| maxQueuePerFloor | 4 |
| startLives | 3 |
| coinsPerDelivery | 1 |
| maxCombo | 5 |
| starBonus | 10 |

Expect `snapRange`, `snapSpeed` and `dwellTime` to need the most tuning. Too strict makes docking frustrating; too loose makes the cabin dock at floors the player meant to pass.

## 8. Destination palette

Eight entries, indexed by floor. Each floor also has a unique icon so the game does not rely on color alone. These colors are the same in every theme.

| Floor | Color | Icon |
|---|---|---|
| 0 | #F2C14E yellow | circle |
| 1 | #E4572E red-orange | triangle |
| 2 | #4D9DE0 blue | square |
| 3 | #3BB273 green | diamond |
| 4 | #9B5DE5 purple | plus |
| 5 | #F15BB5 pink | ring |
| 6 | #00BBF9 cyan | bar (wide rectangle) |
| 7 | #8D99AE grey | two dots |

Icons are generated by `SpriteFactory` and shown on billboarded quads.

## 9. Milestones

Do them in order. After each one, run the verification in section 10, then stop and summarize what was built, what could not be verified, and what to try in Play mode.

### M1: Bootstrap, 3D building, camera, elevator movement

- `Bootstrap`, `GameConfig`, `InputReader`, `MaterialLibrary`, `AssetLibrary` (fallbacks only), `Building`, `Floor`, `FloorPalette`, `ElevatorController`, `ElevatorMotion`, `CameraRig`, one directional light with soft shadows, a plain ground plane and a solid sky color.
- **Done when:** pressing Play in the default scene shows a 4-floor cutaway building made of cubes, fully framed in portrait, and holding the mouse button or space raises the cabin while releasing drops it, with the ground and top limits working.

### M2: Docking

- Hover detection, dwell ring, perfect dock, exchange lock. For testing, treat every floor as having an exchange available and flash the floor on dock.
- **Done when:** hovering docks after the dwell time, a centered slow arrival docks instantly, and flying past a floor never docks.

### M3: Passengers and destinations

- `Passenger` (capsule body, sphere head), `PassengerSpawner`, `Pool`, `Billboard`, `SpriteFactory`, queues, boarding and exiting by position lerps, riders standing in the cabin, destination glow, capacity limit. Docking now requires a possible exchange.
- **Done when:** passengers spawn, board, ride and exit only at their destination floor, and a full cabin leaves the rest of the queue waiting.

### M4: Patience, coins, lives, one playable level

- Patience bars, angry leaving, `ScoreSystem`, `GameManager`, `Hud`, `Screens`, using level 1's definition hard-wired.
- **Done when:** one level can be played from Ready to LevelComplete and to LevelFailed, with retry working.

### M5: Levels and progression

- `LevelDefinition`, `LevelCatalog`, `LevelGenerator`, `LevelSession`, `SaveData`, level bar, stars, level bonus, rush banner, `Tutorial`, rebuilding the building between levels, camera refit.
- Editor-only debug keys: skip to next level, jump to a typed level number, reset the save.
- **Done when:** levels 1 to 25 can be played in sequence, progress survives restarting Play mode, and the level table in 5.7 is reproduced.

### M6: Passenger kinds and Endless mode

- `PassengerKind`, VIP and Big rules, `EndlessRamp`, the Endless button and its high score.
- **Done when:** VIPs and Big passengers behave as in 5.5, and Endless unlocks after level 10 and ramps as in 5.9.

### M7: Art pass

- Download assets under section 6, write `CREDITS.md`, add `AssetImportRules`, switch `AssetLibrary` keys to the downloaded assets, add `Theme` and `Environment` (gradient sky, street, props, room props), apply the UI font.
- If the character is rigged with animations, play idle, walk and a cheer through the legacy `Animation` component or the Playables API, set up from code. Otherwise animate procedurally: bobbing while waiting, hopping while walking, squash on landing.
- **Done when:** no placeholder primitive is visible in a normal level unless it is listed as outstanding, and all three themes appear at levels 1, 11 and 21.

### M8: Feel

- `Fx`: confetti on a win, coin pops flying to the coin counter, floating "+1" and "Perfect" text, scale punch on dock and delivery, small camera shake on a perfect dock.
- `Sfx` hooked to every action, dock pitch rising with the combo, optional music loop with a mute toggle.
- Haptic pulse on a lost life and on a win (mobile only).
- A URP post-processing volume created from code with mild color grading and vignette. No bloom unless performance allows.
- **Done when:** every player action has a visible and audible response, and nothing blocks input for longer than `exchangeTime`.

### M9: Mobile readiness

- `Application.targetFrameRate = 60`, `Screen.sleepTimeout = SleepTimeout.NeverSleep` while playing.
- `SafeArea` applied to the HUD root.
- Camera fit checked at 9:16, 9:19.5 and 9:21, plus a 3:4 tablet.
- Performance budget: under 150,000 triangles on screen, one shadow-casting light, short shadow distance, no real-time additional lights, pooled passengers and effects.
- `MobileSetup` menu item "Elevator Rush/Apply Mobile Settings" that sets through `PlayerSettings`: portrait-only orientation, IL2CPP and ARM64 for Android, a placeholder bundle identifier, and the product name.
- **Done when:** the menu item runs without errors and the game is playable in the Device Simulator on a phone with a notch.

### Later, not in this plan

Courier passengers with a delivery timer, groups, out-of-order floors, a smooth-ride bonus, a coin shop with cabin and character skins, more themes, ads and analytics.

## 10. Verification

After each milestone:

1. Write or extend EditMode tests for the pure classes. Minimum cases:
   - `ElevatorMotion`: rises while holding, falls when released, speed caps hold, clamps at ground and top, docks after the dwell time, docks instantly inside the perfect range, never docks above `snapSpeed`, never docks when no exchange is possible.
   - `ScoreSystem`: coins per kind and combo, combo increase, reset and cap, life loss, stars for 0, 1 and 2 lost lives.
   - `LevelGenerator`: values at levels 21, 25, 60 and 500 stay inside their limits, rush every 5th level, theme cycling, same output for the same number.
   - `LevelSession`: win exactly at the target, fail at 0 lives, no win after a fail.
   - `EndlessRamp`: every threshold in 5.9.
   - `SaveData`: round trip through JSON, corrupt input loads as a fresh save.
2. If a Unity editor executable can be found on this machine and the project is not currently open in the editor, run the tests in batch mode:
   `Unity -batchmode -nographics -projectPath . -runTests -testPlatform EditMode -logFile -`
   Fix every compile error and test failure before continuing.
3. If batch mode is not possible, say so clearly, and ask the user to open the project, run the tests in the Test Runner window, press Play, and paste any Console errors.
4. Visual results cannot be checked from the command line. Ask the user for a screenshot of Play mode after M1, M3, M7 and M8, and compare it against section 2.
5. Never report a milestone as verified unless the tests actually ran. If they did not, say which checks are still outstanding.

## 11. Code conventions

- Namespace `ElevatorRush` for everything.
- One class per file, file name matches the class.
- No `FindObjectOfType` or `GameObject.Find` in gameplay code. `Bootstrap` wires references explicitly.
- No per-frame allocations in `Update` during play: pool passengers, effects and floating text, cache sprites, materials and audio clips.
- No magic numbers in gameplay code. Every number comes from `GameConfig` or a `LevelDefinition`.
- Use `Time.deltaTime` for all movement and timers so pausing with `Time.timeScale = 0` works. UI animations on end screens use unscaled time.
