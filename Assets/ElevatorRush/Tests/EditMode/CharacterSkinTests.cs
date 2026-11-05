using NUnit.Framework;
using UnityEngine;

namespace ElevatorRush.Tests
{
    public class CharacterSkinTests
    {
        // An accessory may never be mistaken for a destination color (home plan 7).
        const float MinDistance = 0.25f;

        static float Distance(Color a, Color b)
        {
            return Mathf.Sqrt((a.r - b.r) * (a.r - b.r) + (a.g - b.g) * (a.g - b.g) + (a.b - b.b) * (a.b - b.b));
        }

        [Test]
        public void AccessoryColorsStayClearOfEveryDestinationColor()
        {
            foreach (var accessory in CharacterSkinner.AccessoryColors)
                for (int d = 0; d < FloorPalette.Count; d++)
                    Assert.GreaterOrEqual(Distance(accessory, FloorPalette.ColorOf(d)), MinDistance,
                        "accessory " + accessory + " vs destination " + d);
        }

        [Test]
        public void EveryColorSlotStartsOnDefault()
        {
            var save = TestSaves.With(0);
            for (int c = 0; c < Catalog.ColorSlotCount; c++)
                Assert.AreEqual(Catalog.DefaultSkin, save.EquippedSkin(c));
        }

        [Test]
        public void DifferentSkinsOnDifferentColorsSurviveASave()
        {
            var save = TestSaves.With(60, 5000);
            var shop = new Shop(save);
            Assert.IsTrue(shop.TryBuy("skin.chef").Ok);
            Assert.IsTrue(shop.TryBuy("skin.cowboy").Ok);
            Assert.AreEqual(ShopResult.Ok, shop.TryEquipSkin("skin.chef", 0));
            Assert.AreEqual(ShopResult.Ok, shop.TryEquipSkin("skin.cowboy", 2));
            Assert.AreEqual(ShopResult.SlotLocked, shop.TryEquipSkin("skin.cowboy", 3));

            var loaded = SaveData.FromJson(save.ToJson());
            Assert.AreEqual("skin.chef", loaded.EquippedSkin(0));
            Assert.AreEqual(Catalog.DefaultSkin, loaded.EquippedSkin(1));
            Assert.AreEqual("skin.cowboy", loaded.EquippedSkin(2));
            Assert.AreEqual(Catalog.DefaultSkin, loaded.EquippedSkin(3));
        }

        [Test]
        public void TwelveSkinsInUnlockOrderWithPlanPrices()
        {
            int[] prices = { 0, 150, 150, 200, 200, 300, 400, 500, 600, 700, 800, 1000 };
            Assert.AreEqual(prices.Length, Catalog.Skins.Length);
            for (int i = 0; i < prices.Length; i++)
            {
                Assert.AreEqual(prices[i], Catalog.Skins[i].price, Catalog.Skins[i].id);
                if (i > 0)
                    Assert.GreaterOrEqual(Catalog.Skins[i].unlockAt, Catalog.Skins[i - 1].unlockAt);
            }
        }
    }
}
