using NUnit.Framework;
using UnityEngine;

namespace ElevatorRush.Tests
{
    public class DecorLayoutTests
    {
        static readonly int[] Furniture = { Catalog.SlotLeft, Catalog.SlotCenter, Catalog.SlotRight };

        GameConfig config;
        DecorLayout layout;

        [SetUp]
        public void SetUp()
        {
            config = new GameConfig();
            layout = new DecorLayout(config);
        }

        static bool Overlaps(Bounds box, Rect wall)
        {
            return box.min.x < wall.xMax && box.max.x > wall.xMin && box.min.y < wall.yMax && box.max.y > wall.yMin;
        }

        [Test]
        public void ColorBandHoldsTheWholeIcon()
        {
            var band = layout.ColorBand;
            var icon = layout.IconCenter;
            float half = config.wallIconSize * 0.5f;
            Assert.LessOrEqual(band.xMin, icon.x - half);
            Assert.GreaterOrEqual(band.xMax, icon.x + half);
            Assert.LessOrEqual(band.yMin, icon.y - half);
            Assert.GreaterOrEqual(band.yMax, icon.y + half);
            Assert.LessOrEqual(band.yMax, layout.Height + 1e-4f);
        }

        [Test]
        public void FurnitureAndArtNeverCoverTheColorBand()
        {
            var band = layout.ColorBand;
            foreach (int slot in new[] { Catalog.SlotLeft, Catalog.SlotCenter, Catalog.SlotRight, Catalog.SlotWallArt })
                Assert.IsFalse(Overlaps(layout.SlotBox(slot), band), Catalog.SlotKeys[slot]);
        }

        [Test]
        public void WallFinishLeavesTheColorBandUncovered()
        {
            var band = layout.ColorBand;
            foreach (var panel in layout.WallPanels())
                Assert.IsFalse(panel.Overlaps(band), panel.ToString());
        }

        [Test]
        public void FurnitureStaysInTheBackHalfBehindTheQueue()
        {
            float queueBack = config.passengerBodyWidth * config.bigWidthScale * 0.5f;
            foreach (int slot in Furniture)
            {
                var box = layout.SlotBox(slot);
                Assert.Greater(box.min.z, queueBack, Catalog.SlotKeys[slot]);
                Assert.GreaterOrEqual(box.min.z, 0f);
                Assert.LessOrEqual(box.max.z, layout.Back);
                Assert.GreaterOrEqual(box.min.x, 0f);
                Assert.LessOrEqual(box.max.x, layout.Width);
                Assert.LessOrEqual(box.max.y, layout.Height);
                Assert.Greater(box.size.x, 0.5f, Catalog.SlotKeys[slot]);
                Assert.Greater(box.size.y, 0.5f, Catalog.SlotKeys[slot]);
            }
        }

        [Test]
        public void SideBySideSlotsDoNotOverlap()
        {
            var left = layout.SlotBox(Catalog.SlotLeft);
            var center = layout.SlotBox(Catalog.SlotCenter);
            var right = layout.SlotBox(Catalog.SlotRight);
            var art = layout.SlotBox(Catalog.SlotWallArt);
            Assert.LessOrEqual(left.max.x, center.min.x);
            Assert.LessOrEqual(center.max.x, right.min.x);
            Assert.GreaterOrEqual(art.min.y, right.max.y);
        }

        [Test]
        public void GoldStarIsClearOfTheBandAndFurniture()
        {
            var p = layout.StarPoint;
            float half = config.decorStarSize * 0.5f;
            var star = new Bounds(p, new Vector3(config.decorStarSize, config.decorStarSize, 0.01f));
            Assert.IsFalse(Overlaps(star, layout.ColorBand));
            foreach (int slot in Furniture)
            {
                var box = layout.SlotBox(slot);
                Assert.IsFalse(Overlaps(star, Rect.MinMaxRect(box.min.x, box.min.y, box.max.x, box.max.y)), Catalog.SlotKeys[slot]);
            }
            Assert.LessOrEqual(p.y + half, layout.Height);
            // Clear of the door lintel and the part of the wall it hides from the camera.
            Assert.Greater(p.x - half, config.wallThickness + config.roomDepth * Mathf.Tan(config.cameraYaw * Mathf.Deg2Rad));
        }

        [Test]
        public void FlooringIsThinAndCoversTheRoom()
        {
            var box = layout.SlotBox(Catalog.SlotFlooring);
            Assert.Less(box.size.y, 0.05f);
            Assert.AreEqual(layout.Width, box.size.x, 1e-4f);
        }

        [Test]
        public void EveryDecorModelKeyIsOnDisk()
        {
            for (int f = 0; f < Catalog.FloorCount; f++)
                for (int s = 0; s < Catalog.SlotsPerFloor; s++)
                    for (int o = 0; o < Catalog.OptionsPerSlot; o++)
                    {
                        var look = DecorLooks.Get(f, s, o);
                        if (look.model != null)
                            Assert.IsNotNull(Resources.Load<GameObject>("Models/" + look.ModelKey), Catalog.DecorId(f, s, o) + " " + look.model);
                    }
        }

        [Test]
        public void EveryCatalogDecorOptionHasALook()
        {
            for (int f = 0; f < Catalog.FloorCount; f++)
                for (int s = 0; s < Catalog.SlotsPerFloor; s++)
                    for (int o = 0; o < Catalog.OptionsPerSlot; o++)
                    {
                        var look = DecorLooks.Get(f, s, o);
                        bool surface = s == Catalog.SlotFlooring || s == Catalog.SlotWall;
                        Assert.AreEqual(surface, look.shape == DecorShape.Surface, Catalog.DecorId(f, s, o));
                        if (s == Catalog.SlotWallArt)
                            Assert.AreEqual(DecorShape.WallArt, look.shape, Catalog.DecorId(f, s, o));
                        if (!surface)
                        {
                            var size = DecorBuilder.NaturalSize(look);
                            Assert.Greater(size.x, 0f);
                            Assert.Greater(size.y, 0f);
                            Assert.Greater(size.z, 0f);
                        }
                    }
        }
    }
}
