# Elevator Rush

A one-button 3D mobile game made in Unity. Hold anywhere to make the elevator rise, let go to let it fall, and stop at the right floors to get everyone where they're going before they lose patience.

Between levels, spend your coins on your own tower: furnish its rooms, upgrade the elevator, and collect character and tower skins.

The game targets Android and iOS in portrait. It's still in development (version 0.1.0).

## Gameplay

- **One input:** hold to rise, release to fall. Hover at a floor to dock, and passengers get on and off.
- **Passengers:** each one shows their destination by color and icon, and has a patience bar. If it runs out they storm off and you lose one of your 3 lives.
- **Passenger types:** VIPs pay more but have less patience. Big passengers pay more but take two cabin slots.
- **Scoring:** perfect stops build a coin combo, and you earn up to 3 stars per level depending on lives lost.
- **Levels:** 20 hand-made levels, then generated ones without end. Every 5th level is a rush hour, and the setting changes every 10 levels.
- **Endless mode:** unlocks after level 10.

## Home screen and progression

| Tab | What you get |
|---|---|
| **Build** | 8 themed floors with 6 slots each, from Lobby to Penthouse. Finishing a floor pays a bonus. |
| **Elevator** | Motor, Doors, Comfort, Tips and Capacity upgrades that affect gameplay. |
| **Characters** | 12 skins, equipped per destination color. |
| **Towers** | 8 skins for the building, such as Brick Loft, Candy, Castle and Gold. |

New content unlocks as you complete levels, with a new floor every 25 levels. Gems, earned through play, can cover a coin shortfall or unlock things early. There's no real-money store or ads yet.

## Getting started

You need **Unity 6000.6.4f1** with the Universal Render Pipeline packages, which Unity installs from `Packages/manifest.json`.

1. Clone the repo:

   ```bash
   git clone https://github.com/egaw3/Elevator-rush.git
   ```

2. Open the folder in Unity Hub. The first import takes a few minutes while Unity rebuilds `Library`.
3. Open `Assets/Scenes/SampleScene.unity` and press Play.

For the right screen shape, switch the Game view to **Simulator** and pick a phone.

### Debug keys

These work in the Unity editor only.

| Key | Action |
|---|---|
| F1 | Complete the current level |
| F2 | Type a level number, then press Enter to jump to it (Esc cancels) |
| F3 | Reset the save |
| F4 | Add 500 coins |
| F5 | Add 5 completed levels |
| F6 | Add 100 gems |

### Editor menu

Under **Elevator Rush** in Unity's menu bar:

- **Economy Report** writes expected earnings against prices for levels 1 to 200 to `Logs/EconomyReport.txt`.
- **Apply Mobile Settings** applies the project's mobile player settings.
- **Create Required Assets** recreates the base materials if they're missing.

## Running the tests

In Unity, open **Window → General → Test Runner** and run the **EditMode** tests.

Or run them from the command line with the project closed in the editor, using your Unity executable:

```bash
Unity -batchmode -nographics -projectPath . -runTests -testPlatform EditMode -testResults results.xml -logFile -
```

## Project layout

Almost everything is built from code at runtime. The scene is nearly empty, and `Bootstrap` creates the game when it loads.

```
Assets/ElevatorRush/
  Scripts/               Core game: elevator, passengers, levels, UI, save data
    Decor/               Room decor shapes, layout and patterns
    Home/                Home screen, panels and street life
    Meta/                Catalog, shop, unlocks, upgrades, skins
    Monetization/        Gem rules and daily counters
  Editor/                Import rules, mobile setup, Economy Report
  Tests/EditMode/        Unit tests
  Resources/             Models, audio, fonts and materials, loaded by key
  CREDITS.md             Every downloaded asset, with source and license
ELEVATOR_RUSH_PLAN_1.md     Original game design and build plan
ELEVATOR_RUSH_HOME_PLAN.md  Home screen and progression plan
```

Tuning values live in `Scripts/GameConfig.cs`. Prices and unlock levels live in `Scripts/Meta/Catalog.cs`.

## Credits

Models and sounds are by [Kenney](https://kenney.nl) under CC0. The UI font is Lilita One by Juan Montoreano under the SIL Open Font License. See [CREDITS.md](Assets/ElevatorRush/CREDITS.md) for the full list.

The project's own code doesn't have a license yet, so all rights are reserved by default.
