using System;

namespace ElevatorRush
{
    /// <summary>Saved tier of one elevator upgrade. A list of these stands in for a dictionary,
    /// which JsonUtility cannot serialize.</summary>
    [Serializable]
    public sealed class UpgradeLevel
    {
        public string id;
        public int tier;

        public UpgradeLevel()
        {
        }

        public UpgradeLevel(string id, int tier)
        {
            this.id = id;
            this.tier = tier;
        }
    }
}
