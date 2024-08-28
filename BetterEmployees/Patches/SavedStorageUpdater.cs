using BetterEmployees.Features;
using UnityEngine;

namespace BetterEmployees.Patches
{
    internal class SavedStorageUpdater
    {
        public static void UpdateIndex(Data_Container __instance, int index, int PID, int PNUMBER)
        {
            if (!EmployeesUtil.SavedStorageShelves.ContainsKey(__instance))
                EmployeesUtil.SavedStorageShelves.Add(__instance, [.. __instance.productInfoArray]);

            if (PID != -1)
            {
                EmployeesUtil.SavedStorageShelves[__instance][index] = PID;
                EmployeesUtil.SavedStorageShelves[__instance][index + 1] = PNUMBER;
            } 
            else
            {
                EmployeesUtil.SavedStorageShelves[__instance][index + 1] = PNUMBER;
            }
        }

        public static void UpdateAll()
        {
            Transform storageManager = NPC_Manager.Instance.storageOBJ.transform;

            if (storageManager.childCount == 0)
                return;

            for (int childId = 0; childId < storageManager.childCount; childId++)
            {
                Data_Container storage = storageManager.GetChild(childId).GetComponent<Data_Container>();
                EmployeesUtil.SavedStorageShelves[storage] = [.. storage.productInfoArray];
            }
        }
    }
}
