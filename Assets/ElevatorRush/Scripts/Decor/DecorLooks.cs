using UnityEngine;

namespace ElevatorRush
{
    /// <summary>
    /// The look of every decor option (home plan 5.1), indexed like <see cref="Catalog"/>:
    /// floor, slot (flooring, wall, left, center, right, wall art), option (basic, nice, premium).
    /// Names live in the catalog; this is only how each one is drawn.
    /// </summary>
    public static class DecorLooks
    {
        // Variant numbers per shape family.
        public const int PlantSmall = 0, PlantFern = 1, PlantPalm = 2, PlantBamboo = 3, PlantBonsai = 4, PlantBush = 5;
        public const int CounterPlain = 0, CounterTrim = 1, CounterDisplay = 2, CounterCart = 3, CounterBar = 4;
        public const int DeskComputer = 0, DeskStanding = 1, DeskExecutive = 2, DeskReading = 3;
        public const int SeatBench = 0, SeatSofa = 1, SeatArmchair = 2, SeatArmchairLamp = 3, SeatChesterfield = 4,
            SeatBooth = 5, SeatLounger = 6, SeatMassage = 7, SeatCorner = 8;
        public const int TableSquare = 0, TableBistro = 1;
        public const int ShelfBooks = 0, ShelfTall = 1, ShelfLadder = 2, ShelfFiling = 3, ShelfMats = 4, ShelfTowels = 5;
        public const int ApplianceCoffee = 0, ApplianceEspresso = 1, ApplianceRoaster = 2, ApplianceCooler = 3,
            ApplianceStation = 4, ApplianceArcade = 5, ApplianceRacing = 6, ApplianceTwin = 7, ApplianceClaw = 8, ApplianceRetro = 9;
        public const int MachineTreadmill = 0, MachineBike = 1, MachineRower = 2, MachinePinball = 3, MachineHockey = 4, MachineDance = 5;
        public const int RoundBall = 0, RoundBag = 1, RoundGlobe = 2, RoundAntiqueGlobe = 3;
        public const int RackDumbbells = 0, RackWeights = 1, RackPower = 2;
        public const int TubHot = 0, TubWood = 1, TubInfinity = 2, TubFountain = 3;
        public const int PianoUpright = 0, PianoGrand = 1;
        public const int ArtClock = 0, ArtFrame = 1, ArtBoard = 2, ArtMirror = 3, ArtNeon = 4, ArtJoystick = 5, ArtScore = 6,
            ArtTrophies = 7, ArtChart = 8, ArtMap = 9, ArtSconces = 10, ArtChandelier = 11, ArtPainting = 12;

        const int Gold = 0xE6B422;
        // Model folders under Resources/Models (see CREDITS.md). Any missing model falls back to the procedural shape.
        const string Furniture = "decor/furniture/";
        const string Arcade = "decor/arcade/";
        /// <summary>Key suffix for models that face away from the camera as imported (Mini Arcade, Mini Market).</summary>
        const string Turned = "@180";

        static readonly DecorLook[][][] Looks =
        {
            new[] // 0 Lobby
            {
                new[] { S(SurfacePattern.Checker, 0xE9E5DC, 0x3D4350), S(SurfacePattern.Planks, 0xC99B6B, 0xA57A4F), S(SurfacePattern.Marble, 0xF3F1EC, 0xB9B3AA) },
                new[] { S(SurfacePattern.Plain, 0xF1E7D6, 0xF1E7D6), S(SurfacePattern.Panels, 0xB88A5C, 0x93693F), S(SurfacePattern.Marble, 0xEAE6DF, 0xC4BDB2) },
                new[] { F(DecorShape.Plant, PlantSmall, 0xC8693A, 0x4CAF50, 0x6D4C41, "props_room/pottedPlant"), F(DecorShape.Plant, PlantFern, 0xEDEDED, 0x3E8E41, 0x2E7D32), F(DecorShape.Plant, PlantPalm, 0x8D6E63, 0x43A047, 0x795548) },
                new[] { F(DecorShape.Counter, CounterPlain, 0xD8CFC0, 0x8D6E63, 0x8D6E63, Furniture + "kitchenBar"), F(DecorShape.Counter, CounterPlain, 0xB88A5C, 0xEADCC6, 0x8D6E63), F(DecorShape.Counter, CounterTrim, 0x2F3440, 0xF3F1EC, Gold) },
                new[] { F(DecorShape.Seat, SeatBench, 0xA1887F, 0x5D4037, 0x5D4037, "props_street/bench"), F(DecorShape.Seat, SeatSofa, 0x5C8DD6, 0x41689E, 0x5D4037, "props_room/loungeSofa"), F(DecorShape.Seat, SeatSofa, 0x6D4C41, 0x4E342E, Gold, Furniture + "loungeSofaLong") },
                new[] { F(DecorShape.WallArt, ArtClock, 0xFFFFFF, 0x333333, 0x333333), F(DecorShape.WallArt, ArtFrame, 0x37474F, 0xFFB74D, 0x4FC3F7), F(DecorShape.WallArt, ArtClock, 0xFFF8E1, Gold, 0x5D4037) },
            },
            new[] // 1 Cafe
            {
                new[] { S(SurfacePattern.Tiles, 0xC96F4A, 0xA9553A), S(SurfacePattern.Checker, 0xF5F1E8, 0x2B2B2B), S(SurfacePattern.Herringbone, 0xB7834F, 0x8E6238) },
                new[] { S(SurfacePattern.Plain, 0xF6E9CF, 0xF6E9CF), S(SurfacePattern.Bricks, 0xB5553A, 0xE8DCCB), S(SurfacePattern.Tiles, 0xFFFFFF, 0xBFD8D2) },
                new[] { F(DecorShape.Appliance, ApplianceCoffee, 0xB0BEC5, 0x263238, 0x8D6E63), F(DecorShape.Appliance, ApplianceEspresso, 0x6D4C41, 0xC0C0C0, 0x3E2723), F(DecorShape.Appliance, ApplianceRoaster, 0xB87333, 0x3E2723, Gold) },
                new[] { F(DecorShape.Counter, CounterPlain, 0xE0C9A6, 0x8D6E63, 0x8D6E63), F(DecorShape.Counter, CounterDisplay, 0xF4E1C1, 0x9AD1E8, 0xF8BBD0, "decor/market/display-bread" + Turned), F(DecorShape.Counter, CounterTrim, 0x3E2723, 0xF3F1EC, Gold) },
                new[] { F(DecorShape.TableChairs, TableSquare, 0xA1887F, 0x6D4C41, 0x6D4C41), F(DecorShape.Seat, SeatBooth, 0xC62828, 0x8E1B1B, 0x8D6E63), F(DecorShape.TableChairs, TableBistro, 0x37474F, Gold, 0xF3F1EC) },
                new[] { F(DecorShape.WallArt, ArtBoard, 0x2E3B32, 0xFFFFFF, 0x8D6E63), F(DecorShape.WallArt, ArtBoard, 0x3E2723, 0xFFE0B2, 0x6D4C41), F(DecorShape.WallArt, ArtNeon, 0xFF6F00, 0x212121, 0xFFD54F) },
            },
            new[] // 2 Office
            {
                new[] { S(SurfacePattern.Tiles, 0x8E9AAF, 0x7A859B), S(SurfacePattern.Planks, 0xD7B98E, 0xBF9F72), S(SurfacePattern.Planks, 0xE6D3A3, 0xCDB984) },
                new[] { S(SurfacePattern.Plain, 0xDADDE2, 0xDADDE2), S(SurfacePattern.Stripes, 0xDADDE2, 0x7E8CA8), S(SurfacePattern.Panels, 0xBFD9E8, 0x90A4AE) },
                new[] { F(DecorShape.Shelf, ShelfBooks, 0x8D6E63, 0x5C8DD6, 0xE57373), F(DecorShape.Shelf, ShelfFiling, 0x90A4AE, 0x607D8B, 0xECEFF1), F(DecorShape.Shelf, ShelfTall, 0x5D4037, 0x81C784, 0xFFB74D) },
                new[] { F(DecorShape.Desk, DeskComputer, 0xD7CCC8, 0x37474F, 0x8D6E63), F(DecorShape.Desk, DeskStanding, 0xFFFFFF, 0x263238, 0x90A4AE), F(DecorShape.Desk, DeskExecutive, 0x4E342E, 0x212121, Gold) },
                new[] { F(DecorShape.Appliance, ApplianceCooler, 0xECEFF1, 0x4FC3F7, 0x90A4AE), F(DecorShape.Plant, PlantBush, 0x795548, 0x388E3C, 0x2E7D32), F(DecorShape.Appliance, ApplianceStation, 0x455A64, 0xB0BEC5, 0x8D6E63) },
                new[] { F(DecorShape.WallArt, ArtBoard, 0xFFFFFF, 0x5C8DD6, 0x90A4AE), F(DecorShape.WallArt, ArtChart, 0xFFFFFF, 0x4CAF50, 0x37474F), F(DecorShape.WallArt, ArtBoard, 0xCFE8F3, 0x37474F, 0xB0BEC5) },
            },
            new[] // 3 Gym
            {
                new[] { S(SurfacePattern.Tiles, 0x3C4048, 0x2E3138), S(SurfacePattern.Planks, 0xE0A96D, 0xC98F52), S(SurfacePattern.Stripes, 0x4CAF50, 0x43A047) },
                new[] { S(SurfacePattern.Plain, 0xE3F2FD, 0xE3F2FD), S(SurfacePattern.Stripes, 0xFFFFFF, 0xFF7043), S(SurfacePattern.Panels, 0xB0BEC5, 0x90A4AE) },
                new[] { F(DecorShape.Round, RoundBall, 0x29B6F6, 0x0288D1, 0x0288D1), F(DecorShape.Shelf, ShelfMats, 0x8D6E63, 0xBA68C8, 0x4DB6AC), F(DecorShape.Round, RoundBag, 0xC62828, 0x37474F, 0x212121) },
                new[] { F(DecorShape.Machine, MachineTreadmill, 0x37474F, 0x90A4AE, 0x4FC3F7), F(DecorShape.Machine, MachineBike, 0x455A64, 0xFF7043, 0x212121), F(DecorShape.Machine, MachineRower, 0x263238, 0xB0BEC5, Gold) },
                new[] { F(DecorShape.Rack, RackDumbbells, 0x37474F, 0x212121, 0x90A4AE), F(DecorShape.Rack, RackWeights, 0x455A64, 0x212121, 0xFF7043), F(DecorShape.Rack, RackPower, 0x263238, 0x212121, Gold) },
                new[] { F(DecorShape.WallArt, ArtMirror, 0xCFD8DC, 0xB3E5FC, 0xCFD8DC), F(DecorShape.WallArt, ArtMirror, 0x37474F, 0xB3E5FC, 0x37474F), F(DecorShape.WallArt, ArtTrophies, 0x8D6E63, Gold, 0xC0C0C0) },
            },
            new[] // 4 Library
            {
                new[] { S(SurfacePattern.Planks, 0x9C6B43, 0x7D532F), S(SurfacePattern.Plain, 0x9E2A2B, 0x9E2A2B), S(SurfacePattern.Rug, 0x7B1E28, 0xE0B04A) },
                new[] { S(SurfacePattern.Plain, 0xF0D9B5, 0xF0D9B5), S(SurfacePattern.Stripes, 0x5B7553, 0x4C6446), S(SurfacePattern.Panels, 0xA0703F, 0x80572E) },
                new[] { F(DecorShape.Shelf, ShelfBooks, 0x8D6E63, 0xC62828, 0x1565C0, "props_room/bookcaseOpen"), F(DecorShape.Shelf, ShelfTall, 0x6D4C41, 0x2E7D32, 0xF9A825), F(DecorShape.Shelf, ShelfLadder, 0x4E342E, 0x6A1B9A, 0xC62828) },
                new[] { F(DecorShape.Seat, SeatArmchair, 0x8D6E63, 0x6D4C41, 0x5D4037, Furniture + "loungeChair"), F(DecorShape.Seat, SeatArmchairLamp, 0x2E7D32, 0x1B5E20, 0xFFE082), F(DecorShape.Seat, SeatChesterfield, 0x5D2E1F, 0x3E1F14, Gold) },
                new[] { F(DecorShape.Round, RoundGlobe, 0x1E88E5, 0x43A047, 0x8D6E63), F(DecorShape.Desk, DeskReading, 0x6D4C41, 0x4E342E, 0xFFE082), F(DecorShape.Round, RoundAntiqueGlobe, 0xD7B98E, 0x8D6E63, Gold) },
                new[] { F(DecorShape.WallArt, ArtPainting, 0x8D6E63, 0x81C784, 0x64B5F6), F(DecorShape.WallArt, ArtMap, 0x6D4C41, 0xE8D9B5, 0x8D6E63), F(DecorShape.WallArt, ArtPainting, Gold, 0x5D4037, 0xFFB74D) },
            },
            new[] // 5 Arcade
            {
                new[] { S(SurfacePattern.Tiles, 0x1E1F26, 0x2A2C36), S(SurfacePattern.Dots, 0x2B1B4A, 0xFFD54F), S(SurfacePattern.Grid, 0x1A1A2E, 0x00E5FF) },
                new[] { S(SurfacePattern.Plain, 0x2D2F3A, 0x2D2F3A), S(SurfacePattern.Stripes, 0x2D2F3A, 0xFF4081), S(SurfacePattern.Checker, 0x3F51B5, 0xFFEB3B) },
                new[] { F(DecorShape.Appliance, ApplianceArcade, 0x3949AB, 0x00E5FF, 0x212121, Arcade + "arcade-machine" + Turned), F(DecorShape.Appliance, ApplianceRacing, 0xE53935, 0xFFEB3B, 0x212121), F(DecorShape.Appliance, ApplianceTwin, 0x8E24AA, 0x76FF03, 0x212121) },
                new[] { F(DecorShape.Machine, MachinePinball, 0x8E24AA, 0xFFEB3B, 0x212121, Arcade + "pinball" + Turned), F(DecorShape.Machine, MachineHockey, 0x1E88E5, 0xFFFFFF, 0xE53935, Arcade + "air-hockey" + Turned), F(DecorShape.Machine, MachineDance, 0x212121, 0xFF4081, 0x00E5FF, Arcade + "dance-machine" + Turned) },
                new[] { F(DecorShape.Appliance, ApplianceClaw, 0xFF4081, 0xB3E5FC, 0xFFEB3B, Arcade + "claw-machine" + Turned), F(DecorShape.Counter, CounterDisplay, 0xFFC107, 0xB3E5FC, 0x6A1B9A, Arcade + "prizes" + Turned), F(DecorShape.Appliance, ApplianceRetro, 0x6D4C41, 0x76FF03, 0x212121) },
                new[] { F(DecorShape.WallArt, ArtNeon, 0xFF4081, 0x212121, 0xFF80AB), F(DecorShape.WallArt, ArtJoystick, 0x00E5FF, 0x212121, 0xFF4081), F(DecorShape.WallArt, ArtScore, 0x212121, 0x76FF03, 0xFFEB3B) },
            },
            new[] // 6 Spa
            {
                new[] { S(SurfacePattern.Dots, 0xCFC8BC, 0xA89F91), S(SurfacePattern.Planks, 0xA0703F, 0x8A5E33), S(SurfacePattern.Tiles, 0xBDB6AA, 0x9E978B) },
                new[] { S(SurfacePattern.Plain, 0xD8EAD3, 0xD8EAD3), S(SurfacePattern.Panels, 0xC9B57A, 0xA8955A), S(SurfacePattern.Bricks, 0x9E9A93, 0x7E7A73) },
                new[] { F(DecorShape.Plant, PlantBamboo, 0xE0E0E0, 0x7CB342, 0x9CCC65), F(DecorShape.Plant, PlantBonsai, 0x5D4037, 0x388E3C, 0x6D4C41), F(DecorShape.Tub, TubFountain, 0x9E9E9E, 0x4FC3F7, 0xBDBDBD) },
                new[] { F(DecorShape.Tub, TubHot, 0xECEFF1, 0x4FC3F7, 0xB0BEC5, Furniture + "bathtub"), F(DecorShape.Tub, TubWood, 0x8D6E63, 0x4FC3F7, 0x5D4037), F(DecorShape.Tub, TubInfinity, 0x263238, 0x29B6F6, Gold) },
                new[] { F(DecorShape.Seat, SeatLounger, 0xF5F5F5, 0x8D6E63, 0x8D6E63, Furniture + "loungeChairRelax"), F(DecorShape.Shelf, ShelfTowels, 0x8D6E63, 0xFFFFFF, 0x80CBC4), F(DecorShape.Seat, SeatMassage, 0xECEFF1, 0x8D6E63, 0x8D6E63) },
                new[] { F(DecorShape.WallArt, ArtPainting, 0x212121, 0xF5F5F5, 0x212121), F(DecorShape.WallArt, ArtPainting, 0xFFFFFF, 0xF8BBD0, 0x81C784), F(DecorShape.WallArt, ArtPainting, 0x8D6E63, 0x4FC3F7, 0x81C784) },
            },
            new[] // 7 Penthouse
            {
                new[] { S(SurfacePattern.Planks, 0xEDE3D1, 0xD9CCB4), S(SurfacePattern.Marble, 0x2A2A2A, 0x777777), S(SurfacePattern.Marble, 0xF3F1EC, Gold) },
                new[] { S(SurfacePattern.Plain, 0xF7F1E8, 0xF7F1E8), S(SurfacePattern.Panels, 0x4A148C, 0x38006B), S(SurfacePattern.Panels, 0xE8C872, 0xC9A548) },
                new[] { F(DecorShape.Counter, CounterCart, 0xC0C0C0, 0x8D6E63, 0xE57373), F(DecorShape.Counter, CounterBar, 0x3E2723, 0x212121, 0x80CBC4), F(DecorShape.Counter, CounterBar, 0x212121, 0xF3F1EC, Gold) },
                new[] { F(DecorShape.Seat, SeatSofa, 0xECEFF1, 0x9E9E9E, 0x5D4037, Furniture + "loungeDesignSofa"), F(DecorShape.Seat, SeatCorner, 0x37474F, 0x263238, 0x212121, Furniture + "loungeDesignSofaCorner"), F(DecorShape.Seat, SeatChesterfield, 0x6A1B9A, 0x4A148C, Gold) },
                new[] { F(DecorShape.Piano, PianoUpright, 0x3E2723, 0xFFFFFF, 0x212121), F(DecorShape.Piano, PianoGrand, 0x151515, 0xFFFFFF, 0x212121), F(DecorShape.Piano, PianoGrand, 0xFAFAFA, 0xFFFFFF, Gold) },
                new[] { F(DecorShape.WallArt, ArtPainting, 0xFAFAFA, 0x90A4AE, 0xE57373), F(DecorShape.WallArt, ArtSconces, Gold, 0xFFF59D, Gold), F(DecorShape.WallArt, ArtChandelier, Gold, 0xFFF9C4, 0xE1F5FE) },
            },
        };

        public static DecorLook Get(int floor, int slot, int option) => Looks[floor][slot][option];

        static DecorLook S(SurfacePattern pattern, int a, int b)
        {
            return new DecorLook(DecorShape.Surface, 0, pattern, Hex(a), Hex(b), Hex(b), null);
        }

        static DecorLook F(DecorShape shape, int variant, int a, int b, int c, string model = null)
        {
            return new DecorLook(shape, variant, SurfacePattern.Plain, Hex(a), Hex(b), Hex(c), model);
        }

        static Color Hex(int rgb)
        {
            return new Color(((rgb >> 16) & 0xFF) / 255f, ((rgb >> 8) & 0xFF) / 255f, (rgb & 0xFF) / 255f);
        }
    }
}
