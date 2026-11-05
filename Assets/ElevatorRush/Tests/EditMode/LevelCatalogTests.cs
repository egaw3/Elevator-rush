using NUnit.Framework;

namespace ElevatorRush.Tests
{
    public class LevelCatalogTests
    {
        const float Eps = 1e-4f;

        // The table in plan section 5.7: floors, groundOnly, target, spawn, patience, vip, big, rush.
        static readonly object[][] Table =
        {
            new object[] { 3, true, 4, 4.0f, 30f, 0f, 0f, false },
            new object[] { 3, true, 6, 3.5f, 26f, 0f, 0f, false },
            new object[] { 4, true, 8, 3.2f, 24f, 0f, 0f, false },
            new object[] { 4, false, 8, 3.5f, 26f, 0f, 0f, false },
            new object[] { 4, false, 12, 2.8f, 22f, 0f, 0f, true },
            new object[] { 5, false, 10, 3.0f, 22f, 0f, 0f, false },
            new object[] { 5, false, 12, 2.8f, 21f, 0f, 0f, false },
            new object[] { 5, false, 12, 2.8f, 21f, 0.15f, 0f, false },
            new object[] { 5, false, 14, 2.6f, 20f, 0.10f, 0f, false },
            new object[] { 5, false, 18, 2.2f, 19f, 0.10f, 0f, true },
            new object[] { 6, false, 14, 2.6f, 20f, 0.10f, 0f, false },
            new object[] { 6, false, 14, 2.6f, 19f, 0.05f, 0.15f, false },
            new object[] { 6, false, 16, 2.4f, 18f, 0.10f, 0.10f, false },
            new object[] { 6, false, 16, 2.4f, 18f, 0.10f, 0.10f, false },
            new object[] { 6, false, 22, 2.0f, 17f, 0.10f, 0.10f, true },
            new object[] { 7, false, 16, 2.4f, 18f, 0.10f, 0.10f, false },
            new object[] { 7, false, 18, 2.2f, 17f, 0.10f, 0.10f, false },
            new object[] { 7, false, 18, 2.2f, 16f, 0.12f, 0.12f, false },
            new object[] { 7, false, 20, 2.0f, 16f, 0.12f, 0.12f, false },
            new object[] { 7, false, 26, 1.8f, 15f, 0.12f, 0.12f, true },
        };

        [Test]
        public void HandMadeLevelsMatchThePlan()
        {
            for (int n = 1; n <= Table.Length; n++)
            {
                var row = Table[n - 1];
                var l = LevelCatalog.Get(n);
                string at = " at level " + n;
                Assert.AreEqual(n, l.number, "number" + at);
                Assert.AreEqual(n, l.seed, "seed" + at);
                Assert.AreEqual((int)row[0], l.floors, "floors" + at);
                Assert.AreEqual((bool)row[1], l.groundOnly, "groundOnly" + at);
                Assert.AreEqual((int)row[2], l.target, "target" + at);
                Assert.AreEqual((float)row[3], l.spawnInterval, Eps, "spawn" + at);
                Assert.AreEqual((float)row[4], l.patience, Eps, "patience" + at);
                Assert.AreEqual((float)row[5], l.vipChance, Eps, "vip" + at);
                Assert.AreEqual((float)row[6], l.bigChance, Eps, "big" + at);
                Assert.AreEqual((bool)row[7], l.rush, "rush" + at);
                Assert.AreEqual(n <= 10 ? 0 : 1, l.theme, "theme" + at);
            }
        }

        [Test]
        public void LaterLevelsComeFromTheGenerator()
        {
            Assert.AreEqual(UnityEngine.JsonUtility.ToJson(LevelGenerator.Generate(21)),
                UnityEngine.JsonUtility.ToJson(LevelCatalog.Get(21)));
        }

        [Test]
        public void LevelNumbersBelowOneClampToOne()
        {
            Assert.AreEqual(1, LevelCatalog.Get(0).number);
        }
    }
}
