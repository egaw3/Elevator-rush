using System.Collections.Generic;

namespace ElevatorRush
{
    /// <summary>What the exchange rules need to know about a passenger.</summary>
    public interface IPassengerInfo
    {
        int Destination { get; }
        int Slots { get; }
    }

    /// <summary>
    /// Pure exchange rules at a dock (plan 5.3 and 5.4): riders for the floor exit first, then
    /// the queue boards in order while it fits. The first passenger that does not fit blocks
    /// everyone behind it.
    /// </summary>
    public static class ExchangeRules
    {
        public static int UsedSlots(IReadOnlyList<IPassengerInfo> riders)
        {
            int used = 0;
            for (int i = 0; i < riders.Count; i++)
                used += riders[i].Slots;
            return used;
        }

        public static int ExitingSlots(int floor, IReadOnlyList<IPassengerInfo> riders)
        {
            int slots = 0;
            for (int i = 0; i < riders.Count; i++)
                if (riders[i].Destination == floor)
                    slots += riders[i].Slots;
            return slots;
        }

        public static bool CanExchange(int floor, IReadOnlyList<IPassengerInfo> riders,
            IReadOnlyList<IPassengerInfo> queue, int capacity)
        {
            int exiting = ExitingSlots(floor, riders);
            if (exiting > 0)
                return true;
            int free = capacity - UsedSlots(riders);
            return queue.Count > 0 && queue[0].Slots <= free;
        }

        /// <summary>How many passengers from the front of the queue board, given the free slots after exits.</summary>
        public static int CountBoarders(IReadOnlyList<IPassengerInfo> queue, int freeSlots)
        {
            int count = 0;
            for (int i = 0; i < queue.Count; i++)
            {
                if (queue[i].Slots > freeSlots)
                    break;
                freeSlots -= queue[i].Slots;
                count++;
            }
            return count;
        }
    }
}
