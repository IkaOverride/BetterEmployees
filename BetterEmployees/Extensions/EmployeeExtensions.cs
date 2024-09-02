#nullable enable
using BetterEmployees.Enums;
using BetterEmployees.Features;
using BetterEmployees.Features.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BetterEmployees.Extensions
{
    public static class EmployeeExtensions
    {
        public static bool ShouldScan(int employeeIndex) => NPC_Manager.Instance.checkoutOBJ.transform.GetChild(employeeIndex).GetComponent<Data_Container>().productsLeft > 0;

        public static int GetEmptyStorageContainer(this NPC_Info employee)
        {
            int productId = employee.boxProductID;

            if (productId == -1)
            {
                ModEntry.Logger.LogError("Product ID was not found.");
                return -1;
            }

            return GetEmptyStorageContainer(employee, productId);
        }

        public static int GetEmptyStorageContainer(this NPC_Info employee, int productId)
        {
            if (productId == -1)
                return -1;

            Transform storageManager = NPC_Manager.Instance.storageOBJ.transform;

            if (storageManager.transform.childCount == 0)
                return -1;

            Dictionary<int, StorageInfo> emptyContainers = [];

            for (int childId = 0; childId < storageManager.childCount; childId++)
            {
                Data_Container storage = storageManager.GetChild(childId).GetComponent<Data_Container>();
                Tuple<int, StorageMode>? storageSlot = storage.GetEmptyStorageSlot(productId);

                if (storageSlot is not null && storageSlot.Item2 <= ModEntry.EmployeeStorageMode.Value)
                    emptyContainers.Add(childId, new(storageSlot.Item2, Vector3.Distance(employee.transform.position, storage.transform.position)));
            }

            emptyContainers = emptyContainers
                .OrderBy(container => container.Value.Distance)
                .OrderBy(container => container.Value.EmptySlotMode)
                .ToDictionary(x => x.Key, x => x.Value);

            if (emptyContainers.Count == 0)
                return -1;

            return emptyContainers.First().Key;
        }

        public static int GetEmptyStorageSlot(this NPC_Info employee, int storageId)
        {
            int currentProduct = employee.boxProductID;

            if (currentProduct == -1)
            {
                ModEntry.Logger.LogError("Box product ID was not found for the NPC.");
                return -1;
            }

            Transform storageManager = NPC_Manager.Instance.storageOBJ.transform;
            Data_Container storage = storageManager.GetChild(storageId).GetComponent<Data_Container>();

            Tuple<int, StorageMode>? storageSlot = storage.GetEmptyStorageSlot(currentProduct);

            if (storageSlot is not null)
                return storageSlot.Item1;

            return -1;
        }

        public static int[] GetProductToRestock()
        {
            NPC_Manager npcManager = NPC_Manager.Instance;
            Transform storageParent = npcManager.storageOBJ.transform;
            Transform shelvesParent = npcManager.shelvesOBJ.transform;

            if (storageParent.childCount == 0)
                return [-1, -1, -1, -1, -1, -1];

            // Percentage, value
            List<Tuple<float, int[]>> results = [];

            for (int shelfId = 0; shelfId < shelvesParent.childCount; shelfId++)
            {
                Data_Container shelfData = shelvesParent.GetChild(shelfId).GetComponent<Data_Container>();
                int[] shelfProducts = shelfData.productInfoArray;
                int shelfProductsCount = shelfProducts.Length / 2;

                for (int shelfProductIndex = 0; shelfProductIndex < shelfProductsCount; shelfProductIndex++)
                {
                    if (ModEntry.RestockerTasks.Value && RestockingTask.Exists(shelfId, shelfProductIndex * 2))
                        continue;

                    int productID = shelfProducts[shelfProductIndex * 2];

                    if (productID < 0)
                        continue;

                    int currentQuantity = shelfProducts[shelfProductIndex * 2 + 1];
                    int maxQuantity = npcManager.GetMaxProductsPerRow(shelfId, productID);

                    if (currentQuantity >= maxQuantity)
                        continue;

                    for (int storageId = 0; storageId < storageParent.childCount; storageId++)
                    {
                        Data_Container storageData = storageParent.GetChild(storageId).GetComponent<Data_Container>();
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
                return [-1, -1, -1, -1, -1, -1];
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

            if (ModEntry.RestockerProductPriority.Value)
                results = [.. results.OrderBy(result2 => result2.Item1)];

            return results.FirstOrDefault()?.Item2 ?? [-1, -1, -1, -1, -1, -1];
        }
    }
}
