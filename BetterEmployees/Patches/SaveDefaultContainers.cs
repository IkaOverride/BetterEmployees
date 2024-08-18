using HarmonyLib;
using UnityEngine;

namespace BetterEmployees.Patches
{
    [HarmonyPatch(typeof(NetworkSpawner), nameof(NetworkSpawner.LoadDecorationCoroutine))]
    internal class SaveDefaultContainers
    {
        private static void Prefix()
        {
            Transform storageManager = NPC_Manager.Instance.storageOBJ.transform;

            if (storageManager.childCount == 0)
                return;

            for (int childId = 0; childId < storageManager.childCount; childId++)
            {
                Data_Container storage = storageManager.GetChild(childId).GetComponent<Data_Container>();
                BetterEmployees.Containers[storage] = [.. storage.productInfoArray];
            }
        }
    }
}
