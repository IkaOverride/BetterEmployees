using BepInEx;
using BepInEx.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BetterEmployees
{
    [BepInPlugin("ika.betteremployees", "BetterEmployees", "0.1.1")]
    public class BetterEmployees : BaseUnityPlugin
    {
        internal static new ManualLogSource Logger;

        internal static HarmonyLib.Harmony Harmony;

        private void Awake()
        {
            Logger = base.Logger;
            Harmony = new("ika.betteremployees");
            Harmony.PatchAll();
        }

        public static bool ShouldScan(int employeeIndex) => NPC_Manager.Instance.checkoutOBJ.transform.GetChild(employeeIndex).GetComponent<Data_Container>().productsLeft > 0;

        /* Storage containers */

        // Container, saved productInfoArray
        public static Dictionary<Data_Container, int[]> Containers = [];

        public static int GetEmployeeEmptyContainer(int employeeIndex)
        {
            int currentProduct = NPC_Manager.Instance.employeeParentOBJ.transform.GetChild(employeeIndex).gameObject.GetComponent<NPC_Info>().boxProductID;

            if (currentProduct == -1)
            {
                Logger.LogError("Product ID was not found.");
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

            List<int> fullyEmptyContainers = [];

            for (int childId = 0; childId < storageManager.childCount; childId++)
            {
                Data_Container storage = storageManager.GetChild(childId).GetComponent<Data_Container>();

                int[] realProductArray = storage.productInfoArray;

                if (!Containers.ContainsKey(storage))
                    Containers.Add(storage, [.. storage.productInfoArray]);

                int[] savedProductArray = Containers[storage];

                if (realProductArray.Length != savedProductArray.Length)
                {
                    Logger.LogError("Product arrays are different in length. Real: " + string.Join(", ", realProductArray) + ". Saved: " + string.Join(", ", savedProductArray));
                    continue;
                }

                int slotCount = realProductArray.Length / 2;

                for (int slot = 0; slot < slotCount; slot++)
                {
                    if (savedProductArray[slot * 2] == -1 && !fullyEmptyContainers.Contains(childId))
                        fullyEmptyContainers.Add(childId);

                    if (savedProductArray[slot * 2] == currentProduct && realProductArray[slot * 2] == -1)
                        return childId;
                }
            }

            if (fullyEmptyContainers.Count != 0)
                return fullyEmptyContainers.First();

            return -1;
        }

        public static int GetEmptyRow(int employeeIndex, int storageContainerIndex)
        {
            GameObject npcObject = NPC_Manager.Instance.employeeParentOBJ.transform.GetChild(employeeIndex).gameObject;
            NPC_Info npcInfo = npcObject.GetComponent<NPC_Info>();
            int currentProduct = npcInfo.boxProductID;

            if (currentProduct == -1)
            {
                Logger.LogError("Box product ID was not found for the NPC.");
                return -1;
            }

            Transform storageManager = NPC_Manager.Instance.storageOBJ.transform;
            Data_Container storage = storageManager.GetChild(storageContainerIndex).GetComponent<Data_Container>();

            int[] realProductArray = storage.productInfoArray;

            if (!Containers.ContainsKey(storage))
                Containers.Add(storage, [.. storage.productInfoArray]);

            int[] savedProductArray = Containers[storage];

            if (realProductArray.Length != savedProductArray.Length)
            {
                Logger.LogError("Product arrays are different in length. Real: " + string.Join(", ", realProductArray) + ". Saved: " + string.Join(", ", savedProductArray));
                return -1;
            }

            int slotCount = realProductArray.Length / 2;

            List<int> fullyEmptyContainers = [];

            for (int slot = 0; slot < slotCount; slot++)
            {
                if (savedProductArray[slot * 2] == -1 && !fullyEmptyContainers.Contains(slot))
                    fullyEmptyContainers.Add(slot);

                if (savedProductArray[slot * 2] == currentProduct && realProductArray[slot * 2] == -1)
                    return slot;
            }

            if (fullyEmptyContainers.Count != 0)
                return fullyEmptyContainers.First();

            return -1;
        }

        /* Restocker Jobs */

        // employeeIndex, (shelfId, shelfProductIndex)
        public static Dictionary<int, Tuple<int, int>> RestockerJobs = [];

        public static void AddRestockerJob(int employeeIndex, int[] job) => RestockerJobs[employeeIndex] = new Tuple<int, int>(job[0], job[1]);

        public static void RemoveRestockerJob(int employeeIndex) => RestockerJobs.Remove(employeeIndex);

        public static void CleanupRestockerJob(int employeeIndex, int task)
        {
            if (RestockerJobs.ContainsKey(employeeIndex) && task != 2)
            {
                Logger.LogInfo("Job cleaned up");
                RestockerJobs.Remove(employeeIndex);
            }
        }
    }
}
