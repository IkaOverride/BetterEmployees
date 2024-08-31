using BetterEmployees.Features;
using UnityEngine;

namespace BetterEmployees.Patches
{
    internal class SavedStorageUpdater
    {
        public static void UpdateIndex(Data_Container __instance, int index, int PID, int PNUMBER)
        {
            int[] savedProductArray = ProductArray.Get(__instance);

            if (PID != -1)
            {
                savedProductArray[index] = PID;
                savedProductArray[index + 1] = PNUMBER;
            } 
            else
            {
                savedProductArray[index + 1] = PNUMBER;
            }
        }

        public static void UpdateAll()
        {
            Transform storageManager = NPC_Manager.Instance.storageOBJ.transform;

            if (storageManager.childCount == 0)
                return;

            for (int childId = 0; childId < storageManager.childCount; childId++)
                ProductArray.Get(storageManager.GetChild(childId).GetComponent<Data_Container>());
        }
    }
}
