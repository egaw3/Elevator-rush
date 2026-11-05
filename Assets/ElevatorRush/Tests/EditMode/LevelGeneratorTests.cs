using NUnit.Framework;

namespace ElevatorRush.Tests
{
    public class LevelGeneratorTests
    {
        const float Eps = 1e-4f;

        static void AssertLevel(int n, int floors, int target, float spawn, float patience, bool rush, int theme)
        {
            var l = LevelGenerator.Generate(n);
            Assert.AreEqual(n, l.number);
            Assert.AreEqual(n, l.seed);
            Assert.AreEqual(floors, l.floors, "floors");
            Assert.AreEqual(target, l.target, "target");
            Assert.AreEqual(spawn, l.spawnInterval, Eps, "spawn");
            Assert.AreEqual(patience, l.patience, Eps, "patience");
            Assert.AreEqual(rush, l.rush, "rush");
            Assert.AreEqual(theme, l.theme, "theme");
            Assert.IsFalse(l.groundOnly);
            Assert.AreEqual(0.12f, l.vipChance, Eps);
            Assert.AreEqual(0.12f, l.bigChance, Eps);
        }

        [Test] public void Level21() => AssertLevel(21, 7, 18, 2.0f, 16f, false, 2);
        [Test] public void Level25() => AssertLevel(25, 7, 25, 1.96f * 0.85f, 15.6f, true, 2);
        [Test] public void Level60() => AssertLevel(60, 8, 39, 1.61f * 0.85f, 12.1f, true, 2);
        [Test] public void Level500() => AssertLevel(500, 8, 39, 1.3f * 0.85f, 11f, true, 1);

        [Test]
        public void ValuesStayInsideLimits()
        {
            for (int n = 21; n <= 2000; n++)
            {
                var l = LevelGenerator.Generate(n);
                Assert.That(l.floors, Is.InRange(3, 8), "floors at " + n);
                Assert.That(l.target, Is.InRange(18, 39), "target at " + n);
                Assert.GreaterOrEqual(l.spawnInterval, 1.3f * 0.85f - Eps, "spawn at " + n);
                Assert.GreaterOrEqual(l.patience, 11f - Eps, "patience at " + n);
            }
        }

        [Test]
        public void RushEveryFifthLevel()
        {
            for (int n = 21; n <= 60; n++)
                Assert.AreEqual(n % 5 == 0, LevelGenerator.Generate(n).rush, "level " + n);
        }

        [Test]
        public void ThemesChangeEveryTenLevelsAndCycle()
        {
            Assert.AreEqual(0, LevelGenerator.ThemeOf(1));
            Assert.AreEqual(0, LevelGenerator.ThemeOf(10));
            Assert.AreEqual(1, LevelGenerator.ThemeOf(11));
            Assert.AreEqual(2, LevelGenerator.ThemeOf(21));
            Assert.AreEqual(2, LevelGenerator.ThemeOf(30));
            Assert.AreEqual(0, LevelGenerator.ThemeOf(31));
            Assert.AreEqual(1, LevelGenerator.ThemeOf(41));
        }

        [Test]
        public void SameNumberGivesSameLevel()
        {
            var a = LevelGenerator.Generate(137);
            var b = LevelGenerator.Generate(137);
            Assert.AreEqual(UnityEngine.JsonUtility.ToJson(a), UnityEngine.JsonUtility.ToJson(b));
        }
    }
}
