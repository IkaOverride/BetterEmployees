using BetterEmployees.Enums;

namespace BetterEmployees.Features
{
    public class StorageInfo(float distance, StorageMode emptySlotMode)
    {
        public float Distance = distance;

        public StorageMode EmptySlotMode = emptySlotMode;

        public override string ToString() =>
            $"{Distance}:{EmptySlotMode}";
    }
}
