using BetterEmployees.Enums;

namespace BetterEmployees.Features
{
    public class StorageInfo(StorageMode emptySlotMode, float distance)
    {
        public StorageMode EmptySlotMode = emptySlotMode;

        public float Distance = distance;

        public override string ToString() =>
            $"({EmptySlotMode}.{Distance})";
    }
}
