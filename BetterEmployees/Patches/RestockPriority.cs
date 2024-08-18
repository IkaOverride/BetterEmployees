using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BetterEmployees.Patches
{
    [HarmonyPatch(typeof(NPC_Manager), nameof(NPC_Manager.CheckProductAvailability))]
    internal class RestockPriority
    {
        private static bool Prefix(NPC_Manager __instance, ref int[] __result)
        {
            if (__instance.storageOBJ.transform.childCount == 0)
            {
                __result = [-1, -1, -1, -1, -1, -1];
                return false;
            }

            // Percentage, value
            List<Tuple<float, int[]>> results = [];

            for (int shelfId = 0; shelfId < __instance.shelvesOBJ.transform.childCount; shelfId++)
            {
                Data_Container shelfData = __instance.shelvesOBJ.transform.GetChild(shelfId).GetComponent<Data_Container>();
                int[] shelfProducts = shelfData.productInfoArray;
                int shelfProductsCount = shelfProducts.Length / 2;

                for (int shelfProductIndex = 0; shelfProductIndex < shelfProductsCount; shelfProductIndex++)
                {
                    if (BetterEmployees.RestockerJobs.ContainsValue(new Tuple<int, int>(shelfId, shelfProductIndex * 2)))
                        continue;

                    int productID = shelfProducts[shelfProductIndex * 2];

                    if (productID < 0)
                        continue;

                    int currentQuantity = shelfProducts[shelfProductIndex * 2 + 1];
                    int maxQuantity = __instance.GetMaxProductsPerRow(shelfId, productID);

                    if (currentQuantity >= maxQuantity)
                        continue;

                    for (int storageId = 0; storageId < __instance.storageOBJ.transform.childCount; storageId++)
                    {
                        Data_Container storageData = __instance.storageOBJ.transform.GetChild(storageId).GetComponent<Data_Container>();
                        int[] storageProductInfo = storageData.productInfoArray;
                        int storageProductsCount = storageProductInfo.Length / 2;

                        for (int storageProductIndex = 0; storageProductIndex < storageProductsCount; storageProductIndex++)
                        {
                            int storageProductID = storageProductInfo[storageProductIndex * 2];

                            if (storageProductID >= 0 && storageProductID == productID && storageProductInfo[storageProductIndex * 2 + 1] > 0)
                            {
                                results.Add(new Tuple<float, int[]>((float)currentQuantity / maxQuantity, [shelfId, shelfProductIndex * 2, storageId, storageProductIndex * 2, productID, storageProductID]));
                                break;
                            }
                        }
                    }
                }
            }

            if (results.Count == 0)
            {
                __result = [-1, -1, -1, -1, -1, -1];
                return false;
            }

            for (int resultIndex = 0; resultIndex < results.Count; resultIndex++)
            {
                var result = results[resultIndex];
                var sameResults = results.Where(result2 => result.Item2[4] == result2.Item2[4]);

                if (sameResults.All(result2 => result.Item2[4] == result2.Item2[4]))
                    continue;

                float average = sameResults.Select(result2 => result2.Item1).Average();

                sameResults.ToList().ForEach(result2 => results[resultIndex] = new Tuple<float, int[]>(average, result2.Item2));
            }

            __result = results.OrderBy(result => result.Item1).First().Item2;
            return false;
        }
    }
}
