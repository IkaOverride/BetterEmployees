using System.Collections.Generic;
using System.Linq;

namespace BetterEmployees.Features.Tasks
{
    // employeeIndex, (shelfId, shelfProductIndex)
    // todo: employeeIndex, (productId, quantity)
    public class RestockingTask
    {
        private static Dictionary<int, RestockingTask> List = [];

        public int ShelfId;

        public int ShelfProductIndex;

        private RestockingTask(int shelfId, int shelfProductIndex)
        {
            ShelfId = shelfId;
            ShelfProductIndex = shelfProductIndex;
        }

        public static void Set(int employeeId, int[] task) =>
            List[employeeId] = new(task[0], task[1]);

        public static void Remove(int employeeIndex) => List.Remove(employeeIndex);

        public static void Cleanup(int employeeIndex, int task)
        {
            if (List.ContainsKey(employeeIndex) && task != 2)
                List.Remove(employeeIndex);
        }

        public static bool Exists(int shelfId,int shelfProductIndex)
            => List.Where(task => task.Value.ShelfId == shelfId && task.Value.ShelfProductIndex == shelfProductIndex).Count() > 0;
    }
}
