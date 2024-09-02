using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BetterEmployees.Enums;
using BetterEmployees.Patches;
using HarmonyLib;
using System.Collections.Generic;
using static HarmonyLib.AccessTools;

namespace BetterEmployees
{
    [BepInPlugin("ika.betteremployees", "BetterEmployees", "0.2.1")]
    public class ModEntry : BaseUnityPlugin
    {
        internal static new ManualLogSource Logger;

        internal static Harmony Harmony;

        internal static ConfigEntry<bool> SaveStorageOrder;

        internal static ConfigEntry<bool> CanEmployeeUpdateStorageOrder;

        internal static ConfigEntry<StorageMode> EmployeeStorageMode;

        internal static ConfigEntry<bool> EmployeeCollisions;

        internal static ConfigEntry<bool> RestockerProductPriority;

        internal static ConfigEntry<bool> RestockerTasks;

        private void Awake()
        {
            Logger = base.Logger;
            Harmony = new("ika.betteremployees");

            BindConfig();
            UpdateConfig();
            Patch();
        }

        private void BindConfig()
        {
            SaveStorageOrder = Config.Bind("StorageOrder", "Enable", true, "Should the storage order be saved.");
            CanEmployeeUpdateStorageOrder = Config.Bind("StorageOrder", "CanEmployeeUpdate", false, "Can employees update the storage order.");
            EmployeeStorageMode = Config.Bind("StorageOrder", "EmployeeStorageMode", StorageMode.EmptyButReserved,
                "InStorageOrder: The employee will always respect the storage order.\n" +
                "FullyEmpty: If the employee can't respect the storage order, they will try to find a storage that is not reserved.\n" +
                "EmptyButReserved: The employee will ignore the storage order if it's the only choice they have.");

            EmployeeCollisions = Config.Bind("Employee", "Collisions", true, "Should employees have collisions with each other.");

            RestockerProductPriority = Config.Bind("RestockerEmployee", "ProductPriority", true, "Should restockers prioritize more empty shelves to restock.");
            RestockerTasks = Config.Bind("RestockerEmployee", "Tasks", true, "Should restockers check what others are already restocking to not do the same task.");
        }

        private void Patch()
        {
            Harmony.PatchAll();

            if (!EmployeeCollisions.Value)
                Harmony.Patch
                (
                    Method(typeof(NPC_Manager), nameof(NPC_Manager.SpawnEmployee)),
                    postfix: new(typeof(EmployeeCollisions), nameof(Patches.EmployeeCollisions.Disable))
                );

            if (SaveStorageOrder.Value)
            {
                Harmony.Patch
                (
                    Method
                    (
                        typeof(Data_Container),
                        CanEmployeeUpdateStorageOrder.Value ? nameof(Data_Container.RpcUpdateArrayValuesStorage) : nameof(Data_Container.UserCode_CmdUpdateArrayValuesStorage__Int32__Int32__Int32)
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

        private void UpdateConfig()
        {
            Dictionary<ConfigDefinition, string> orphans = Traverse.Create(Config).Property("OrphanedEntries").GetValue<Dictionary<ConfigDefinition, string>>();

            // Update from v0.2
            ConfigDefinition v020OrderConfig = new("StorageZone", "Order");
            ConfigDefinition v020EmployeeModeConfig = new("StorageOrder", "EmployeeMode");
            ConfigDefinition v020JobsConfig = new("RestockerEmployee", "Jobs");

            if (orphans.TryGetValue(v020OrderConfig, out string value))
            {
                if (bool.TryParse(value, out bool saveOrder))
                    SaveStorageOrder.Value = saveOrder;

                orphans.Remove(v020OrderConfig);

                StorageMode storageMode = orphans[v020EmployeeModeConfig] switch
                {
                    "ForceOrder" => StorageMode.InStorageOrder,
                    "AllowFullyEmpty" => StorageMode.FullyEmpty,
                    "AllowEmpty" => StorageMode.EmptyButReserved,
                    _ => StorageMode.EmptyButReserved
                };
                EmployeeStorageMode.Value = storageMode;
                orphans.Remove(v020EmployeeModeConfig);

                if (bool.TryParse(orphans[v020JobsConfig], out bool tasks))
                {
                    RestockerTasks.Value = tasks;
                }
                orphans.Remove(v020JobsConfig);

                Config.Save();
                Logger.LogInfo(storageMode.ToString());
            }
        }
    }
}
