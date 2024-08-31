using System.Collections.Generic;

namespace BetterEmployees.Features
{
    public static class ProductArray
    {
        private static Dictionary<Data_Container, int[]> List = [];

        public static void Add(Data_Container container)
        {
            if (!List.ContainsKey(container))
                List.Add(container, [.. container.productInfoArray]);
        }

        public static int[] Get(Data_Container container)
        {
            if (!List.ContainsKey(container))
                List.Add(container, [.. container.productInfoArray]);

            return List[container];
        }
    }
}
