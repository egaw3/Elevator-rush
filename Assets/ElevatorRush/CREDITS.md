# Credits

Every downloaded asset in the game, with its source and license. All were checked on the
asset's own page before download, on 2026-10-05 (and 2026-10-06 for the decor kits). Only the individual files listed are in the
project, renamed and placed under `Assets/ElevatorRush/Resources/`.

## Models

| Asset | Author | Page | License | Downloaded | Used as |
|---|---|---|---|---|---|
| Mini Characters 1.0: `character-male-a.fbx`, `character-female-a.fbx`, `Textures/colormap.png` | Kenney | https://kenney.nl/assets/mini-characters | CC0 1.0 | 2026-10-05 | `Models/character/character_a.fbx`, `character_b.fbx` (passengers; body re-materialed with the destination palette; idle, walk and emote-yes clips) |
| Furniture Kit: `desk`, `pottedPlant`, `loungeSofa`, `bookcaseOpen`, `bedSingle`, `lampRoundFloor` (FBX) | Kenney | https://kenney.nl/assets/furniture-kit | CC0 1.0 | 2026-10-05 | `Models/props_room/*`: decor options Potted Plant (Lobby), Sofa (Lobby), Bookcase (Library); `lampRoundFloor` is the floor lamp of Armchair and Lamp (Library). `desk` and `bedSingle` are no longer used since the home-tower decor replaced the random room props |
| Furniture Kit 2.0: `kitchenBar`, `loungeSofaLong`, `loungeChair`, `loungeChairRelax`, `loungeDesignSofa`, `loungeDesignSofaCorner`, `bathtub`, `computerScreen`, `computerKeyboard`, `laptop`, `lampSquareTable`, `lampRoundTable`, `books` (FBX) | Kenney | https://kenney.nl/assets/furniture-kit | CC0 1.0 (License.txt in the pack) | 2026-10-06 | `Models/decor/furniture/*`: decor options Reception Desk and Leather Sofa (Lobby), Reading Chair (Library), Hot Tub and Lounger (Spa), Luxury Sofa and Corner Sofa (Penthouse); the rest are props on procedural pieces (monitors, keyboards and a laptop on desks, table lamps, books) |
| Mini Arcade 1.2: `arcade-machine`, `pinball`, `air-hockey`, `dance-machine`, `claw-machine`, `prizes` (FBX), `Textures/colormap.png` | Kenney (with Fleur Keijsers, Guus Vermeulen) | https://kenney.nl/assets/mini-arcade | CC0 1.0 (License.txt in the pack) | 2026-10-06 | `Models/decor/arcade/*`: Arcade decor options Arcade Cabinet, Pinball Table, Air Hockey, Dance Machine, Claw Machine, Prize Counter (turned 180 degrees to face the camera) |
| Mini Market 1.0: `display-bread` (FBX), `Textures/colormap.png` | Kenney (with Fleur Keijsers, Guus Vermeulen) | https://kenney.nl/assets/mini-market | CC0 1.0 (License.txt in the pack) | 2026-10-06 | `Models/decor/market/display-bread.fbx`: Pastry Counter (Cafe) |
| Food Kit 2.0: `cake`, `cupcake`, `donut-sprinkles`, `cup-coffee`, `wine-red`, `wine-white`, `cocktail`, `glass-wine` (FBX), `Textures/colormap.png` | Kenney | https://kenney.nl/assets/food-kit | CC0 1.0 (License.txt in the pack) | 2026-10-06 | `Models/decor/food/*`: props on procedural pieces (cakes in display counters, coffee cups, bottles and glasses on bars and the bar cart) |
| Furniture Kit: `bench.fbx` | Kenney | https://kenney.nl/assets/furniture-kit | CC0 1.0 | 2026-10-05 | `Models/props_street/bench.fbx` (street bench, and the Lobby's Bench decor option) |
| Nature Kit: `tree_default`, `tree_palmTall`, `tree_pineTallA`, `plant_bush` (FBX) | Kenney | https://kenney.nl/assets/nature-kit | CC0 1.0 | 2026-10-05 | `Models/props_street/tree_city`, `tree_seaside`, `tree_night`, `bush` |
| Car Kit: `sedan.fbx`, `Textures/colormap.png` | Kenney | https://kenney.nl/assets/car-kit | CC0 1.0 | 2026-10-05 | `Models/props_street/car/car.fbx`: no longer used, since levels have no cars |
| Car Kit 3.1: `taxi`, `sedan`, `police`, `van`, `suv`, `hatchback-sports`, `delivery` (FBX), `Textures/colormap.png` | Kenney | https://kenney.nl/assets/car-kit | CC0 1.0 (License.txt in the pack) | 2026-10-06 | `Models/cars/*`: traffic driving past on the Home screen only |
| City Kit (Commercial) 2.1: `low-detail-building-a` to `-n`, `low-detail-building-wide-a`, `-wide-b`, `building-skyscraper-a` to `-e` (FBX), `Textures/colormap.png` | Kenney | https://kenney.nl/assets/city-kit-commercial | CC0 1.0 (License.txt in the pack) | 2026-10-06 | `Models/city/*`: the city skyline behind the tower, on Home and in levels |
| City Kit (Roads): `light-square.fbx`, `Textures/colormap.png` | Kenney | https://kenney.nl/assets/city-kit-roads | CC0 1.0 | 2026-10-05 | `Models/props_street/lamp/lamp.fbx` (street lamp) |

## Audio

| Asset | Author | Page | License | Downloaded | Used as |
|---|---|---|---|---|---|
| Interface Sounds: `drop_002`, `confirmation_002`, `pluck_001`, `glass_002`, `error_004`, `click_002` (OGG) | Kenney | https://kenney.nl/assets/interface-sounds | CC0 1.0 | 2026-10-05 | `Audio/sfx_dock`, `sfx_perfect`, `sfx_deliver`, `sfx_coin`, `sfx_angry`, `sfx_button` |
| Music Jingles: `Steel jingles/jingles_STEEL00`, `Pizzicato jingles/jingles_PIZZI03` (OGG) | Kenney | https://kenney.nl/assets/music-jingles | CC0 1.0 | 2026-10-05 | `Audio/sfx_win`, `sfx_fail` |

The Impact Sounds pack (https://kenney.nl/assets/impact-sounds, CC0) was downloaded but no
file from it is used, so none is in the project.

## Fonts

| Asset | Author | Page | License | Downloaded | Used as |
|---|---|---|---|---|---|
| Lilita One (`LilitaOne-Regular.ttf`) | Juan Montoreano | https://fonts.google.com/specimen/Lilita+One (file from https://github.com/google/fonts/tree/main/ofl/lilitaone) | SIL Open Font License 1.1 | 2026-10-05 | `Fonts/font_ui.ttf` (all UI text); license text shipped alongside as `Fonts/font_ui_OFL.txt` |

## Outstanding (primitive or procedural fallback kept)

| Key | Reason | Fallback |
|---|---|---|
| `crown` | No CC0 crown model found from the allowed sources (search turned up only paid models) | Procedural gold crown: cylinder band with five cone points |
| `cabin` | Plan allows building it from cubes | Cubes |
| `music_loop` | Optional; none chosen | None |
| Decor furniture and wall art (home plan 5) | Options without a matching CC0 model (gym equipment, pianos, tubs other than the bathtub, plants other than the potted plant, all wall art) | Procedural primitives in `DecorBuilder`, one recipe per option in `DecorLooks`; every model key above also falls back to these |
| Flooring and wall finishes | The plan asks for code-made materials | Patterns generated by `PatternFactory` |
| Tower skin roof ornaments (`towers/*_roof`) | No CC0 models in the Kenney style for water tanks, pagodas, turrets, dishes or domes | Procedural ornaments in `TowerOrnamentBuilder`; exterior patterns (bricks, glass grid, stripes, panels) from `PatternFactory` |
