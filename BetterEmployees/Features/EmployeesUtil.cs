using BetterEmployees.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BetterEmployees.Features
{
    public static class EmployeesUtil
    {
        // Container, productInfoArray
        public static Dictionary<Data_Container, int[]> SavedStorageShelves = [];

        // employeeIndex, (shelfId, shelfProductIndex)
        // todo: employeeIndex, (productId, quantity)
        public static Dictionary<int, Tuple<int, int>> RestockerJobs = [];

        public static bool ShouldScan(int employeeIndex) => NPC_Manager.Instance.checkoutOBJ.transform.GetChild(employeeIndex).GetComponent<Data_Container>().productsLeft > 0;

        public static int GetEmployeeEmptyContainer(int employeeIndex)
        {
            int currentProduct = NPC_Manager.Instance.employeeParentOBJ.transform.GetChild(employeeIndex).gameObject.GetComponent<NPC_Info>().boxProductID;

            if (currentProduct == -1)
            {
                ModEntry.Logger.LogError("Product ID was not found.");
                return -1;
            }

            return GetProductEmptyContainer(currentProduct);
        }

        public static int GetProductEmptyContainer(int currentProduct)
        {
            if (currentProduct == -1)
                return -1;

            Transform storageManager = NPC_Manager.Instance.storageOBJ.transform;

            if (storageManager.transform.childCount == 0)
                return -1;

            Dictionary<int, bool> emptyContainers = [];

            for (int childId = 0; childId < storageManager.childCount; childId++)
            {
                Data_Container storage = storageManager.GetChild(childId).GetComponent<Data_Container>();

                int[] realProductArray = storage.productInfoArray;

                if (!SavedStorageShelves.ContainsKey(storage))
                    SavedStorageShelves.Add(storage, [.. storage.productInfoArray]);

                int[] savedProductArray = SavedStorageShelves[storage];

                if (realProductArray.Length != savedProductArray.Length)
                {
                    ModEntry.Logger.LogError("Product arrays are different in length. Real: " + string.Join(", ", realProductArray) + ". Saved: " + string.Join(", ", savedProductArray));
                    continue;
                }

                int slotCount = realProductArray.Length / 2;

                for (int slot = 0; slot < slotCount; slot++)
                {
                    if (savedProductArray[slot * 2] == currentProduct && realProductArray[slot * 2] == -1)
                        return childId;

                    if (realProductArray[slot * 2] == -1 && !emptyContainers.ContainsKey(childId))
                        emptyContainers.Add(childId, savedProductArray[slot * 2] == -1);
                }
            }

            if (ModEntry.StorageOrderEmployeeMode != EmployeeStorageMode.ForceOrder)
            {
                var fullyEmptyContainers = emptyContainers.Where(container => container.Value);
                
                if (fullyEmptyContainers.Count() != 0)
                    return fullyEmptyContainers.First().Key;

                if (ModEntry.StorageOrderEmployeeMode == EmployeeStorageMode.AllowEmpty && emptyContainers.Count != 0)
                    return emptyContainers.First().Key;
            }

            return -1;
        }

        public static int GetEmptyRow(int employeeIndex, int storageContainerIndex)
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

            int[] realProductArray = storage.productInfoArray;

            if (!SavedStorageShelves.ContainsKey(storage))
                SavedStorageShelves.Add(storage, [.. storage.productInfoArray]);

            int[] savedProductArray = SavedStorageShelves[storage];

            if (realProductArray.Length != savedProductArray.Length)
            {
                ModEntry.Logger.LogError("Product arrays are different in length. Real: " + string.Join(", ", realProductArray) + ". Saved: " + string.Join(", ", savedProductArray));
                return -1;
            }

            int slotCount = realProductArray.Length / 2;

            Dictionary<int, bool> emptyContainers = [];

            for (int slot = 0; slot < slotCount; slot++)
            {
                if (savedProductArray[slot * 2] == currentProduct && realProductArray[slot * 2] == -1)
                    return slot;

                if (realProductArray[slot * 2] == -1 && !emptyContainers.ContainsKey(slot))
                    emptyContainers.Add(slot, savedProductArray[slot * 2] == -1);
            }

            if (ModEntry.StorageOrderEmployeeMode != EmployeeStorageMode.ForceOrder)
            {
                var fullyEmptyContainers = emptyContainers.Where(container => container.Value);

                if (fullyEmptyContainers.Count() != 0)
                    return fullyEmptyContainers.First().Key;

                if (ModEntry.StorageOrderEmployeeMode == EmployeeStorageMode.AllowEmpty && emptyContainers.Count != 0)
                    return emptyContainers.First().Key;
            }

            return -1;
        }

        public static void AddRestockerJob(int employeeIndex, int[] job) => RestockerJobs[employeeIndex] = new Tuple<int, int>(job[0], job[1]);

        public static void RemoveRestockerJob(int employeeIndex) => RestockerJobs.Remove(employeeIndex);

        public static void CleanupRestockerJob(int employeeIndex, int task)
        {
            if (RestockerJobs.ContainsKey(employeeIndex) && task != 2)
                RestockerJobs.Remove(employeeIndex);
        }
    }
}
