# Elevator Rush: home screen and progression plan

A follow-up to `ELEVATOR_RUSH_PLAN.md`. The core game already exists. This plan adds a home screen, a tower the player builds up with coins, elevator upgrades, character skins, tower skins, and an unlock schedule tied to levels completed.

## 0. Before writing any code

1. Read the existing code under `Assets/ElevatorRush/` and `CREDITS.md`. The code may differ from the original plan. Where it does, follow the code and adapt the class names used below.
2. Write a short summary of what exists: game states, how `SaveData` is stored, how the building and rooms are built, how themes work, which assets are in `Resources/`.
3. List anything in this plan that conflicts with the existing code and say how it will be resolved. Then start milestone H1.

Everything in the original plan still applies unless this file changes it: the technical constraints (section 3), asset rules (section 6), verification (section 10) and code conventions (section 11). In particular: build the UI and scene from code, no hand-edited YAML, pure C# for logic so it can be unit tested, CC0 assets only, every asset key has a primitive fallback.

## 1. What changes for the player

- The game opens on a **home screen** showing the player's own tower in 3D, with a big Play button.
- After every level the player returns to the home screen with the coins they earned.
- Coins are spent in four places: **Build** (furnish each floor of the tower), **Elevator** (upgrades that affect gameplay), **Characters** (a skin for each passenger color), **Towers** (a skin for the whole building).
- Content unlocks as levels are completed. A new floor opens every 25 levels, and something smaller unlocks in between, so there is always a visible next goal.
- What the player buys shows up inside the levels: their decor in the rooms, their skins on the passengers, their tower skin on the building.

## 2. Flow

| From | Action | To |
|---|---|---|
| App start | | Home |
| Home | Play | Level (the existing Ready state is skipped; the level starts on first touch, with its target shown) |
| LevelComplete | Continue | Home |
| LevelFailed | Retry | Same level |
| LevelFailed | Home | Home |
| Home | Endless (after level 10) | Endless run, then back to Home |

Changes to existing screens:

- LevelComplete replaces "Next" with "Continue" and adds a "Next unlock" bar (section 4.2).
- On arriving at Home after a win, the earned coins fly into the coin counter. If the win crossed an unlock threshold, an "Unlocked!" popup shows each new item before the player can do anything else.

## 3. Home screen

### 3.1 Layout

- Background: the home tower in 3D, using the same camera rig as the levels.
- Top: coin counter, settings gear, tower progress ("Tower 34%").
- Center-bottom: the Play button, labelled "Level N".
- Above the Play button: the "Next unlock" teaser (section 4.2).
- Bottom navigation: Build, Elevator, Characters, Towers. A red dot appears on a tab when something in it is affordable and not yet owned, or newly unlocked and not yet viewed.
- An Endless button once it is unlocked.
- Settings: sound, music and vibration toggles, saved.

Everything sits inside the safe area. Touches on UI never start a level.

### 3.2 The home tower

- Built with the same building code as the levels, so rooms and decor look identical in both places.
- It shows every unlocked floor, plus the next locked floor on top as a construction site (bare frame, scaffolding, a sign saying "Level 25"). The tower grows taller as the player progresses. The camera refits when it grows.
- The elevator cabin idles up and down on its own.
- Tapping an unlocked floor opens Build mode on that floor.
- World-space taps use invisible UI buttons positioned with `WorldToScreenPoint`. Do not add physics colliders.

## 4. Unlock schedule

### 4.1 Rules

- `levelsCompleted` is the number of distinct levels won.
- **Floor k** unlocks at `25 * k` levels completed (floor 0 from the start, floor 7 at 175). Unlocking a floor opens its room for decorating and opens the character-skin slot for that floor's color.
- **Elevator tier cap** is `min(5, 1 + levelsCompleted / 25)` with integer division.
- **Tower skins** and **character skins** each have their own unlock threshold, listed below. Unlocking makes an item purchasable; it still costs coins.
- Locked items stay visible, greyed out, with the level that unlocks them.

| Levels completed | Unlocks |
|---|---|
| 0 | Floor 0 Lobby and the yellow skin slot, tower skin Classic, skins Default and Office Worker, elevator tier 1 |
| 5 | skin Builder |
| 10 | skin Chef (Endless mode also unlocks here, as before) |
| 15 | skin Tourist |
| 25 | Floor 1 Cafe and the red-orange slot, tower skin Brick Loft, elevator tier 2 |
| 35 | skin DJ |
| 50 | Floor 2 Office and the blue slot, tower skin Glass Modern, elevator tier 3 |
| 60 | skin Cowboy |
| 75 | Floor 3 Gym and the green slot, tower skin Candy, elevator tier 4 |
| 85 | skin Wizard |
| 100 | Floor 4 Library and the purple slot, tower skin Pagoda, elevator tier 5 |
| 110 | skin Astronaut |
| 125 | Floor 5 Arcade and the pink slot, tower skin Castle |
| 135 | skin Robot |
| 150 | Floor 6 Spa and the cyan slot, tower skin Space Station |
| 160 | skin Pirate |
| 175 | Floor 7 Penthouse and the grey slot, tower skin Gold |
| 185 | skin Royal |

### 4.2 Next unlock teaser

Shown on Home and on LevelComplete: an icon of the next item in the table, its name, and a bar filling from the previous threshold to the next ("3 levels to go"). After the last row it shows tower progress instead.

## 5. Build: decorating floors

### 5.1 Rooms and slots

Each floor is a themed room with 6 slots. Each slot has 3 options (basic, nice, premium).

| Slot | What it is | Where |
|---|---|---|
| flooring | floor finish | floor slab |
| wall | wall finish | back and side wall, outside the color band |
| left | furniture | back-left of the room |
| center | furniture | back-center |
| right | furniture | back-right |
| wallArt | hung item | back wall, beside the floor icon |

| Floor | Room | Example furniture |
|---|---|---|
| 0 | Lobby | reception desk, sofa, plant, clock |
| 1 | Cafe | counter, table with chairs, coffee machine, menu board |
| 2 | Office | desk with computer, bookshelf, water cooler, whiteboard |
| 3 | Gym | treadmill, weights rack, exercise ball, mirror |
| 4 | Library | bookcases, reading chair, globe, painting |
| 5 | Arcade | arcade cabinets, pinball table, neon sign |
| 6 | Spa | hot tub, lounger, bamboo plant, towel shelf |
| 7 | Penthouse | grand piano, luxury sofa, bar, chandelier |

Choose the three concrete options per slot from the furniture models available under the asset rules, matching each room's theme. Flooring and wall finishes are materials made in code (colors and simple generated patterns), not downloaded textures.

### 5.2 Rules

- Buying any option fills the slot and equips it. Owned options can be switched for free.
- Empty slots show the room unfinished: bare concrete floor, plain wall, no furniture.
- Decor must never cover the floor's destination color band or icon, and furniture stays in the back half of the room so it never overlaps queuing passengers or their patience bars.
- A floor is **complete** when all 6 slots are filled. Completing a floor pays a one-time bonus of `100 * (k + 1)` coins and puts a gold star on that floor.
- **Tower progress** is filled slots divided by 48.
- **In levels:** a level with `n` floors shows the player's equipped decor on floors 0 to `n - 1`. Floors the player has not unlocked or furnished appear as plain rooms. This replaces the random room props from the original art pass.

### 5.3 Build mode

- Tapping a floor moves the camera in to frame that room. A back button returns to the full tower.
- Each slot shows a marker: a "+" bubble if empty, a small swap icon if filled.
- Tapping a marker opens a bottom sheet with the 3 options: thumbnail, name, and price, "Owned" or "Equipped".
- On purchase: coins count down, the item drops in with a bounce, and a small confetti burst plays. When the purchase completes the floor, play the larger win confetti and show the bonus.
- Left and right arrows move to the neighbouring unlocked floor without leaving Build mode.

## 6. Elevator upgrades

These affect gameplay in every level and in Endless.

| Upgrade | Effect per tier | Tiers |
|---|---|---|
| Motor | +5% `riseAccel` and `maxRiseSpeed` | 5 |
| Doors | -8% `dwellTime` and `exchangeTime` | 5 |
| Comfort | +6% patience for every waiting passenger | 5 |
| Tips | +10% coins from deliveries | 5 |
| Capacity | +1 cabin slot | 2 |

- Tiers are bought in order. A tier can be bought only if it is at or below the current tier cap (section 4.1). Capacity tier 1 needs cap 2 and Capacity tier 2 needs cap 4.
- Falling speed, `snapRange`, `snapSpeed` and `perfectRange` are never changed by upgrades, so the handling stays familiar.
- A pure class, `UpgradeMath`, turns the saved tiers into the effective values for a run. Gameplay code reads those values and never reads the save directly.
- Tips can produce fractions. Accumulate coins as a float during the run and round up once at the end.
- With Capacity upgrades the cabin gets wider so 5 or 6 riders fit visibly.
- The cabin's look improves with the total number of tiers owned: at 6 tiers it gains trim and lights, at 14 it becomes a glass-and-gold cabin. These are cosmetic.
- The Elevator panel shows each upgrade as a row: icon, name, tier pips, the next effect in plain words ("Doors open 8% faster"), and a price button or a lock with the level needed.

## 7. Character skins

- There are 8 **color slots**, one per destination color. A slot becomes available when its floor unlocks.
- A skin is bought once and can then be equipped on any number of available color slots. Each slot has one equipped skin; the default is Default.
- A skin changes the character's outfit and accessories (hat, hair, props, optionally a different body mesh). **The body stays tinted with the destination color**, and accessories use neutral colors, so skins never make destinations harder to read. The destination icon bubble is unchanged.
- VIP crowns and Big scaling apply on top of any skin.
- Skins: Default, Office Worker, Builder, Chef, Tourist, DJ, Cowboy, Wizard, Astronaut, Robot, Pirate, Royal. Unlock thresholds are in section 4.1.
- If suitable CC0 character variants exist in the same style as the current character, use them with the main material re-tinted. Otherwise build each skin from accessories attached to the head and body of the existing character.
- The Characters panel: a row of 8 color chips at the top (locked ones show a lock and the level), a grid of skins below, and a live preview of the selected color wearing the selected skin, standing on the ground floor with the camera moved in. Buttons: Buy, Equip, Equipped.

## 8. Tower skins

- A tower skin changes the building itself: exterior walls, trim, window frames, shaft frame, roof ornament and the neutral base color of interior walls.
- The existing level themes keep cycling every 10 levels, but from now on a theme only controls the sky, street, street props and lighting. Move wall and building colors out of `Theme` into a new `TowerSkin`.
- The equipped tower skin is used on the home tower and in every level and Endless run.
- Skins: Classic (free), Brick Loft, Glass Modern, Candy, Pagoda, Castle, Space Station, Gold. Each is defined by a small set of colors, material settings (for example metallic for Gold, transparent panels for Glass Modern) and one or two roof and trim model keys with primitive fallbacks.
- Destination color bands and icons look the same on every skin.
- The Towers panel is a horizontal carousel. Selecting a skin previews it live on the home tower. Leaving the panel without buying restores the equipped skin.

## 9. Economy

Coins are earned as before: per delivery with combo and kind multipliers, plus the star bonus. All prices below are first guesses and must be tuned after play-testing.

| Item | Price |
|---|---|
| Decor option on floor k | basic 40, nice 90, premium 160, each multiplied by `(1 + k)` |
| Elevator tier 1 to 5 (Motor, Doors, Comfort, Tips) | 100, 250, 450, 700, 1000 |
| Capacity tier 1, tier 2 | 400, 1200 |
| Character skins, in unlock order after Default | 150, 150, 200, 200, 300, 400, 500, 600, 700, 800, 1000 |
| Tower skin number i (Classic is 0) | `400 * i` |

Rough intent: by level 25 a player earning mostly three stars should afford the basic options for all of floor 0, most tier 1 elevator upgrades and one or two skins, but not everything. There should always be something affordable soon and something expensive to save for.

Add an editor menu item, "Elevator Rush/Economy Report", that prints for levels 1 to 200: expected coins per level at two and three stars with an average combo of 1.5, cumulative coins, and the cumulative cost of everything unlocked so far (cheapest and most expensive decor options). This is the tool for tuning prices.

All prices and thresholds live in one `Catalog` class as data.

## 10. Save data

- Add a `version` field. Existing saves must load with coins, current level, stars and Endless progress intact, and new fields at their defaults. A corrupt save still loads as a fresh one.
- New fields, using arrays and lists only (`JsonUtility` cannot serialize dictionaries):
  - per floor, per slot: owned options and the equipped option
  - floors whose completion bonus has been paid
  - elevator tiers per upgrade
  - owned character skins, and the equipped skin per color slot
  - owned tower skins and the equipped one
  - item ids already shown in an "Unlocked!" popup, and ids already viewed (for the red dots)
  - settings: sound, music, vibration
- Item ids are stable strings (for example `decor.f0.left.2`, `elev.motor`, `skin.chef`, `tower.candy`). Never renumber them.
- Save after every purchase, equip, settings change and level result.

## 11. New files

```
Scripts/Meta/
  Catalog.cs            all items, prices and unlock thresholds as data
  UnlockSchedule.cs     pure C#: what is unlocked at N levels, next unlock
  Wallet.cs             pure C#: balance, add, spend
  Shop.cs               pure C#: TryBuy and TryEquip with result codes
  UpgradeMath.cs        pure C#: tiers to effective run values
  TowerProgress.cs      pure C#: slots filled, floor complete, percent
  SaveMigration.cs      pure C#: old save to current version
  TowerSkin.cs          building look data
  RoomDecorator.cs      builds a room's decor from the save, used by home and levels
  CharacterSkinner.cs   applies a skin and tint to a passenger
Scripts/Home/
  HomeController.cs     home tower, camera focus, idle elevator
  HomeHud.cs            Play, coins, tabs, teaser, settings
  BuildPanel.cs  ElevatorPanel.cs  CharactersPanel.cs  TowersPanel.cs
  UnlockPopup.cs  BottomSheet.cs  WorldButton.cs
Editor/
  EconomyReport.cs
Tests/EditMode/
  UnlockScheduleTests.cs  ShopTests.cs  UpgradeMathTests.cs
  TowerProgressTests.cs   SaveMigrationTests.cs
```

## 12. Milestones

Do them in order. After each one, run the verification from the original plan, then stop and summarize what was built, what could not be verified, and what to try in Play mode. In each milestone get the logic working with primitive fallbacks first, then fetch the assets it needs and update `CREDITS.md`.

### H1: Data and logic

- `Catalog`, `UnlockSchedule`, `Wallet`, `Shop`, `UpgradeMath`, `TowerProgress`, `SaveMigration`, the extended `SaveData`, `EconomyReport`.
- No UI. Editor-only debug keys: add 500 coins, add 5 completed levels, reset the save.
- **Done when:** all new tests pass and a save from before this plan loads with nothing lost.

### H2: Home screen and flow

- `HomeController`, `HomeHud`, the flow in section 2, the home tower with its construction floor, the next-unlock teaser, settings, coin fly-in. Tabs open empty panels.
- **Done when:** the app opens on Home, Play starts the current level, a win returns to Home with coins added, and a fail offers Retry and Home.

### H3: Build mode

- `RoomDecorator`, `BuildPanel`, `BottomSheet`, `WorldButton`, floor focus, purchase and swap, floor completion bonus, tower progress, decor shown in levels.
- **Done when:** floor 0 can be fully furnished from the home screen, the same decor appears in the next level, passengers are never hidden by it, and floor 1 becomes available at 25 levels completed.

### H4: Elevator upgrades

- `ElevatorPanel`, upgrades applied through `UpgradeMath`, wider cabin for Capacity, cabin look tiers.
- **Done when:** each upgrade has a measurable effect in a level, tier caps follow section 4.1, and an unupgraded save plays exactly as before.

### H5: Character skins

- `CharacterSkinner`, `CharactersPanel`, per-color equip, preview.
- **Done when:** different skins can be equipped on different colors, they appear correctly in levels, and body colors still match their destinations.

### H6: Tower skins

- `TowerSkin`, the `Theme` split, `TowersPanel` with live preview.
- **Done when:** every skin changes the home tower and the level building, themes still change the sky and street every 10 levels, and color bands and icons are unchanged on every skin.

### H7: Unlock moments and polish

- `UnlockPopup`, red dots, sounds and haptics for buying and unlocking, a few passengers wandering in furnished home floors, button press animations.
- Check performance with all 8 floors fully furnished on the home screen against the original budget, and check the layout at 9:16, 9:19.5, 9:21 and 3:4.
- Run the Economy Report and include its summary in the milestone notes, with any price changes it suggests.
- **Done when:** crossing each threshold in section 4.1 shows the right popup exactly once, and the home screen holds 60 fps in the Device Simulator profile used before.

## 13. Tests to add

- `UnlockSchedule`: every row of the table in 4.1, the values just below and at each threshold, the elevator tier cap at 0, 24, 25, 99, 100 and 500, the next unlock at 0, 7 and 185 and beyond.
- `Shop`: buy succeeds and deducts coins; fails when locked, when too expensive, when already owned; elevator tiers must be bought in order and within the cap; equipping an unowned item fails; equipping a skin on a locked color slot fails.
- `UpgradeMath`: zero tiers returns the base config unchanged; each upgrade at tier 1 and at its maximum; Tips rounding.
- `TowerProgress`: empty, partial and complete floors; the completion bonus is paid once only; the percent out of 48.
- `SaveMigration`: an old-format save keeps coins, level and stars; unknown item ids in a save are ignored without errors.

## 14. Later, not in this plan

Rewarded ads for double coins, in-app purchases, daily rewards, cabin skins sold separately, more rooms beyond 8 floors, seasonal tower skins.
