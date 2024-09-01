using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BetterEmployees.Enums;
using BetterEmployees.Patches;
using HarmonyLib;
using static HarmonyLib.AccessTools;

namespace BetterEmployees
{
    [BepInPlugin("ika.betteremployees", "BetterEmployees", "0.3.0")]
    public class ModEntry : BaseUnityPlugin
    {
        internal static new ManualLogSource Logger;

        internal static Harmony Harmony;

        private static ModEntry Instance { get; set; }

        private ConfigEntry<bool> ConfigStorageOrderSave;

        private ConfigEntry<StorageMode> ConfigStorageOrderEmployeeMode;

        private ConfigEntry<bool> ConfigStorageOrderCanEmployeeUpdate;

        private ConfigEntry<bool> ConfigEmployeeCollisions;

        private ConfigEntry<bool> ConfigRestockerProductPriority;

        private ConfigEntry<bool> ConfigRestockerTasks;

        public static bool StorageOrderSave => Instance.ConfigStorageOrderSave.Value;

        public static StorageMode StorageOrderEmployeeMode => Instance.ConfigStorageOrderEmployeeMode.Value;

        public static bool StorageOrderCanEmployeeUpdate => Instance.ConfigStorageOrderCanEmployeeUpdate.Value;

        public static bool EmployeeCollisions => Instance.ConfigEmployeeCollisions.Value;

        public static bool RestockerProductPriority => Instance.ConfigRestockerProductPriority.Value;

        public static bool RestockerTasks => Instance.ConfigRestockerTasks.Value;

        private void Awake()
        {
            Instance = this;
            Logger = base.Logger;

            ConfigStorageOrderSave = Config.Bind("StorageZone", "Order", true, "Should the storage order be saved.");
            ConfigStorageOrderCanEmployeeUpdate = Config.Bind("StorageOrder", "CanEmployeeUpdate", false, "Can employees update the storage order.");
            ConfigStorageOrderEmployeeMode = Config.Bind("StorageOrder", "EmployeeMode", StorageMode.EmptyButReserved, 
                "InStorageOrder: The employee will always respect the storage order.\n" +
                "FullyEmpty: If the employee can't respect the order, they will put the box in a storage that is not reserved.\n" +
                "EmptyButReserved: If the employee didn't find any better storage to put the box, they will put the box in a random empty storage.");

            ConfigEmployeeCollisions = Config.Bind("Employee", "Collisions", true, "Should employees have collisions with each other.");

            ConfigRestockerProductPriority = Config.Bind("RestockerEmployee", "ProductPriority", true, "Should restockers prioritize more empty shelves to restock.");
            ConfigRestockerTasks = Config.Bind("RestockerEmployee", "Tasks", true, "Should restockers check what others are already restocking to not do the same task.");

            Harmony = new("ika.betteremployees");
            
            Harmony.PatchAll();

            if (!EmployeeCollisions)
                Harmony.Patch
                (
                    Method(typeof(NPC_Manager), nameof(NPC_Manager.SpawnEmployee)),
                    postfix: new(typeof(EmployeeCollisions), nameof(Patches.EmployeeCollisions.Disable))
                );

            if (StorageOrderSave)
            {
                Harmony.Patch
                (
                    Method
                    (
                        typeof(Data_Container),
                        StorageOrderCanEmployeeUpdate ? nameof(Data_Container.RpcUpdateArrayValuesStorage) : nameof(Data_Container.UserCode_CmdUpdateArrayValuesStorage__Int32__Int32__Int32)
                    ),
                    postfix: new(typeof(SavedStorageUpdater), nameof(SavedStorageUpdater.UpdateIndex))
                );

                Harmony.Patch
                (
                    Method(typeof(NetworkSpawner), nameof(NetworkSpawner.LoadDecorationCoroutine)),
                    postfix: new(typeof(SavedStorageUpdater), nameof(SavedStorageUpdater.UpdateAll))
                );
            }
        }
    }
}
