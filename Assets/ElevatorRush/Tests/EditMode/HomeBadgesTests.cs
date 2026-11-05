using NUnit.Framework;

namespace ElevatorRush.Tests
{
    public class HomeBadgesTests
    {
        [Test]
        public void FreshSaveWithNoCoinsHasNoDots()
        {
            var save = TestSaves.With(0, 0);
            for (int t = 0; t < 4; t++)
                Assert.IsFalse(HomeBadges.HasDot(save, (HomeTab)t), ((HomeTab)t).ToString());
        }

        [Test]
        public void AffordableItemsShowDots()
        {
            var save = TestSaves.With(0, 100);
            Assert.IsTrue(HomeBadges.HasDot(save, HomeTab.Build));
            Assert.IsTrue(HomeBadges.HasDot(save, HomeTab.Elevator));
            Assert.IsFalse(HomeBadges.HasDot(save, HomeTab.Characters));
            Assert.IsFalse(HomeBadges.HasDot(save, HomeTab.Towers));
        }

        [Test]
        public void NewUnlocksShowDotsUntilTheTabIsViewed()
        {
            var save = TestSaves.With(25, 0);
            Assert.IsTrue(HomeBadges.HasUnviewed(save, HomeTab.Build));
            Assert.IsTrue(HomeBadges.HasUnviewed(save, HomeTab.Characters));
            Assert.IsTrue(HomeBadges.HasUnviewed(save, HomeTab.Towers));
            Assert.IsTrue(HomeBadges.HasUnviewed(save, HomeTab.Elevator));

            Assert.IsTrue(HomeBadges.MarkViewed(save, HomeTab.Towers));
            Assert.IsFalse(HomeBadges.HasUnviewed(save, HomeTab.Towers));
            Assert.IsFalse(HomeBadges.MarkViewed(save, HomeTab.Towers));
            Assert.IsTrue(HomeBadges.HasUnviewed(save, HomeTab.Build));
        }

        [Test]
        public void ViewedSurvivesASaveRoundTrip()
        {
            var save = TestSaves.With(10, 0);
            HomeBadges.MarkViewed(save, HomeTab.Characters);
            var loaded = SaveData.FromJson(save.ToJson());
            Assert.IsFalse(HomeBadges.HasUnviewed(loaded, HomeTab.Characters));
        }

        [Test]
        public void OwnedItemsDoNotShowAffordableDots()
        {
            var save = TestSaves.With(0, 150);
            var shop = new Shop(save);
            Assert.IsTrue(shop.TryBuy("skin.office").Ok);
            Assert.IsFalse(HomeBadges.HasAffordable(save, HomeTab.Characters));
        }
    }
}
