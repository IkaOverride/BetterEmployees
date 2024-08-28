using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BetterEmployees.Enums;
using BetterEmployees.Patches;
using HarmonyLib;
using static HarmonyLib.AccessTools;

namespace BetterEmployees
{
    [BepInPlugin("ika.betteremployees", "BetterEmployees", "0.2.0")]
    public class ModEntry : BaseUnityPlugin
    {
        internal static new ManualLogSource Logger;

        internal static Harmony Harmony;

        private static ModEntry Instance { get; set; }

        private ConfigEntry<bool> ConfigStorageSaveOrder;

        private ConfigEntry<EmployeeStorageMode> ConfigStorageEmployeeMode;

        private ConfigEntry<bool> ConfigStorageCanEmployeeUpdateOrder;

        private ConfigEntry<bool> ConfigEmployeeCollisions;

        private ConfigEntry<bool> ConfigRestockerProductPriority;

        private ConfigEntry<bool> ConfigRestockerJobs;

        public static bool StorageSaveOrder => Instance.ConfigStorageSaveOrder.Value;

        public static EmployeeStorageMode StorageEmployeeMode => Instance.ConfigStorageEmployeeMode.Value;

        public static bool StorageCanEmployeeUpdateOrder => Instance.ConfigStorageCanEmployeeUpdateOrder.Value;

        public static bool EmployeeCollisions => Instance.ConfigEmployeeCollisions.Value;

        public static bool RestockerProductPriority => Instance.ConfigRestockerProductPriority.Value;

        public static bool RestockerJobs => Instance.ConfigRestockerJobs.Value;

        private void Awake()
        {
            Instance = this;
            Logger = base.Logger;

            ConfigStorageSaveOrder = Config.Bind("Storage", "SaveOrder", true, "Should the storage order be saved.");
            ConfigStorageCanEmployeeUpdateOrder = Config.Bind("Storage", "CanEmployeeUpdateOrder", false, "Can employees update the storage order.");
            ConfigStorageEmployeeMode = Config.Bind("Storage", "EmployeeMode", EmployeeStorageMode.AllowFullyEmpty, 
                "ForceOrder: If the employee can't respect the order, they will drop the box.\n" +
                "AllowFullyEmpty: If the employee can't respect the order, they will put the box in a storage that is not reserved.\n" +
                "AllowEmpty: If the employee can't respect the order and all storages are reserved for other products, they will put the box in a random empty storage.");

            ConfigEmployeeCollisions = Config.Bind("Employee", "Collisions", true, "Should employees have collisions.");

            ConfigRestockerProductPriority = Config.Bind("RestockerEmployee", "ProductPriority", true, "Should restockers prioritize more empty shelves to restock.");
            ConfigRestockerJobs = Config.Bind("RestockerEmployee", "Jobs", true, "Should restockers check what others are already restocking to not do the same task.");

            Harmony = new("ika.betteremployees");
            
            Harmony.PatchAll();

            if (EmployeeCollisions)
                Harmony.Patch
                (
                    Method(typeof(NPC_Manager), nameof(NPC_Manager.SpawnEmployee)),
                    postfix: new(typeof(EmployeeCollisions), nameof(Patches.EmployeeCollisions.Disable))
                );

            if (StorageSaveOrder)
            {
                Harmony.Patch
                (
                    Method
                    (
                        typeof(Data_Container),
                        StorageCanEmployeeUpdateOrder ? nameof(Data_Container.RpcUpdateArrayValuesStorage) : nameof(Data_Container.UserCode_CmdUpdateArrayValuesStorage__Int32__Int32__Int32)
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
