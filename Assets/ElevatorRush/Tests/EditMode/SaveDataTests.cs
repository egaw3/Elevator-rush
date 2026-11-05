using NUnit.Framework;

namespace ElevatorRush.Tests
{
    public class SaveDataTests
    {
        [Test]
        public void RoundTripsThroughJson()
        {
            var a = new SaveData { currentLevel = 7, coins = 123, endlessHighScore = 45, endlessUnlocked = true };
            a.bestStars.AddRange(new[] { 3, 2, 1 });
            var b = SaveData.FromJson(a.ToJson());
            Assert.AreEqual(7, b.currentLevel);
            Assert.AreEqual(123, b.coins);
            Assert.AreEqual(45, b.endlessHighScore);
            Assert.IsTrue(b.endlessUnlocked);
            CollectionAssert.AreEqual(new[] { 3, 2, 1 }, b.bestStars);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("not json at all {{{")]
        [TestCase("{\"currentLevel\": \"seven\"")]
        public void CorruptInputLoadsFresh(string json)
        {
            var s = SaveData.FromJson(json);
            Assert.AreEqual(1, s.currentLevel);
            Assert.AreEqual(0, s.coins);
            Assert.IsNotNull(s.bestStars);
            Assert.IsFalse(s.endlessUnlocked);
        }

        [Test]
        public void OutOfRangeValuesAreRepaired()
        {
            var s = SaveData.FromJson("{\"currentLevel\":-4,\"coins\":-10,\"bestStars\":[9,-1]}");
            Assert.AreEqual(1, s.currentLevel);
            Assert.AreEqual(0, s.coins);
            CollectionAssert.AreEqual(new[] { 3, 0 }, s.bestStars);
        }

        [Test]
        public void RecordsCompletionAndKeepsBestStars()
        {
            var s = new SaveData();
            s.RecordLevelComplete(3, 2);
            Assert.AreEqual(4, s.currentLevel);
            Assert.AreEqual(2, s.BestStars(3));
            s.RecordLevelComplete(3, 1);
            Assert.AreEqual(2, s.BestStars(3));
            Assert.AreEqual(0, s.BestStars(2));
            Assert.IsFalse(s.endlessUnlocked);
            s.RecordLevelComplete(10, 3);
            Assert.IsTrue(s.endlessUnlocked);
            Assert.AreEqual(11, s.currentLevel);
        }

        [Test]
        public void ReplayingAnOldLevelDoesNotMoveProgressBack()
        {
            var s = new SaveData { currentLevel = 9 };
            s.RecordLevelComplete(2, 3);
            Assert.AreEqual(9, s.currentLevel);
        }

        [Test]
        public void EndlessHighScoreOnlyRises()
        {
            var s = new SaveData();
            Assert.IsTrue(s.RecordEndlessScore(10));
            Assert.IsFalse(s.RecordEndlessScore(5));
            Assert.AreEqual(10, s.endlessHighScore);
        }
    }
}
