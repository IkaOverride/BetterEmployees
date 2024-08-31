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

        public static int GetEmptyStorageContainer(NPC_Info employee)
        {
            int productId = employee.boxProductID;

            if (productId == -1)
            {
                ModEntry.Logger.LogError("Product ID was not found.");
                return -1;
            }

            return GetEmptyStorageContainer(employee, productId);
        }

        public static int GetEmptyStorageContainer(NPC_Info employee, int productId)
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
                Tuple<int, StorageMode>? storageSlot = GetBestStorageSlot((int)productId, storage);

                if (storageSlot is not null && storageSlot.Item2 <= ModEntry.StorageOrderEmployeeMode)
                    emptyContainers.Add(childId, new(Vector3.Distance(employee.transform.position, storage.transform.position), storageSlot.Item2));
            }

            emptyContainers = emptyContainers
                .OrderBy(container => container.Value.Distance)
                .OrderBy(container => container.Value.Mode)
                .ToDictionary(x => x.Key, x => x.Value);

            if (emptyContainers.Count == 0)
                return -1;

            return emptyContainers.First().Key;
        }

        public static int GetEmptyStorageRow(int employeeIndex, int storageContainerIndex)
        {
            GameObject npcObject = NPC_Manager.Instance.employeeParentOBJ.transform.GetChild(employeeIndex).gameObject;
            NPC_Info npcInfo = npcObject.GetComponent<NPC_Info>();
            int currentProduct = npcInfo.boxProductID;

            if (currentProduct == -1)
            {
                ModEntry.Logger.LogError("Box product ID was not found for the NPC.");
                return -1;
            }

            Transform storageManager = NPC_Manager.Instance.storageOBJ.transform;
            Data_Container storage = storageManager.GetChild(storageContainerIndex).GetComponent<Data_Container>();

            Tuple<int, StorageMode>? storageSlot = GetBestStorageSlot(currentProduct, storage);

            if (storageSlot is not null)
                return storageSlot.Item1;

            return -1;
        }

        public static Tuple<int, StorageMode>? GetBestStorageSlot(int productId, Data_Container storage)
        {
            Tuple<int, StorageMode>? value = null;

            int[] realProductArray = storage.productInfoArray;
            int[] savedProductArray = ProductArray.Get(storage);

            int slotCount = realProductArray.Length / 2;

            if (ModEntry.StorageOrderSave)
            {
                for (int slot = 0; slot < slotCount; slot++)
                {
                    if (realProductArray[slot * 2] == -1 && savedProductArray[slot * 2] == productId)
                        return new Tuple<int, StorageMode>(slot, StorageMode.InStorageOrder);

                    if (realProductArray[slot * 2] == -1 && value?.Item2 != StorageMode.FullyEmpty)
                        value = new Tuple<int, StorageMode>(slot, savedProductArray[slot * 2] == -1 ? StorageMode.FullyEmpty : StorageMode.EmptyButReserved);
                }
            }
            else
            {
                for (int slot = 0; slot < slotCount; slot++)
                {
                    if (realProductArray[slot * 2] == -1)
                        return new Tuple<int, StorageMode>(slot, StorageMode.InStorageOrder);
                }
            }

            return value;
        }

        public static int[] GetProductToRestock(NPC_Manager manager)
        {
            if (manager.storageOBJ.transform.childCount == 0)
                return [-1, -1, -1, -1, -1, -1];

            // Percentage, value
            List<Tuple<float, int[]>> results = [];

            for (int shelfId = 0; shelfId < manager.shelvesOBJ.transform.childCount; shelfId++)
            {
                Data_Container shelfData = manager.shelvesOBJ.transform.GetChild(shelfId).GetComponent<Data_Container>();
                int[] shelfProducts = shelfData.productInfoArray;
                int shelfProductsCount = shelfProducts.Length / 2;

                for (int shelfProductIndex = 0; shelfProductIndex < shelfProductsCount; shelfProductIndex++)
                {
                    if (ModEntry.RestockerJobs && RestockingTask.Exists(shelfId, shelfProductIndex * 2))
                        continue;

                    int productID = shelfProducts[shelfProductIndex * 2];

                    if (productID < 0)
                        continue;

                    int currentQuantity = shelfProducts[shelfProductIndex * 2 + 1];
                    int maxQuantity = manager.GetMaxProductsPerRow(shelfId, productID);

                    if (currentQuantity >= maxQuantity)
                        continue;

                    for (int storageId = 0; storageId < manager.storageOBJ.transform.childCount; storageId++)
                    {
                        Data_Container storageData = manager.storageOBJ.transform.GetChild(storageId).GetComponent<Data_Container>();
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

            if (ModEntry.RestockerProductPriority)
                results = [.. results.OrderBy(result2 => result2.Item1)];

            return results.FirstOrDefault()?.Item2 ?? [-1, -1, -1, -1, -1, -1];
        }
    }
}
