using System.Collections.Generic;

namespace ElevatorRush
{
    /// <summary>One row of the unlock table: everything that unlocks at the same level count.</summary>
    public sealed class UnlockRow
    {
        public readonly int threshold;
        /// <summary>Item ids in display order: floor, tower skin, elevator tier, then character skins.</summary>
        public readonly IReadOnlyList<string> ids;

        public UnlockRow(int threshold, IReadOnlyList<string> ids)
        {
            this.threshold = threshold;
            this.ids = ids;
        }
    }
}
