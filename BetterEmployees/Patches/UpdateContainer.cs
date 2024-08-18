using HarmonyLib;

namespace BetterEmployees.Patches
{
    [HarmonyPatch(typeof(Data_Container), nameof(Data_Container.RpcUpdateArrayValuesStorage))]
    internal class UpdateContainer
    {
        private static void Postfix(Data_Container __instance, int index, int PID, int PNUMBER)
        {
            if (!BetterEmployees.Containers.ContainsKey(__instance))
                BetterEmployees.Containers.Add(__instance, [.. __instance.productInfoArray]);

            if (PID != -1)
            {
                BetterEmployees.Containers[__instance][index] = PID;
                BetterEmployees.Containers[__instance][index + 1] = PNUMBER;
            }
        }
    }
}
