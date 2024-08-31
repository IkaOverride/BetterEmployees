using BetterEmployees.Enums;

namespace BetterEmployees.Features
{
    public class StorageInfo(float distance, StorageMode mode)
    {
        public float Distance = distance;

        public StorageMode Mode = mode;

        public override string ToString() =>
            $"{Distance}:{Mode}";
    }
}
