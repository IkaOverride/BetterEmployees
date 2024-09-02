using BetterEmployees.Enums;

namespace BetterEmployees.Features
{
    public class StorageInfo(StorageMode bestSlotMode, float distance)
    {
        public StorageMode BestSlotMode = bestSlotMode;

        public float Distance = distance;

        public override string ToString() =>
            $"({BestSlotMode}.{Distance})";
    }
}
