using BetterEmployees.Extensions;
using HarmonyLib;

namespace BetterEmployees.Patches
{
    [HarmonyPatch(typeof(NPC_Manager), nameof(NPC_Manager.CheckProductAvailability))]
    internal class ProductToRestock
    {
        private static bool Prefix(NPC_Manager __instance, ref int[] __result)
        {
            __result = EmployeeExtensions.GetProductToRestock(__instance);
            return false;
        }
    }
}
