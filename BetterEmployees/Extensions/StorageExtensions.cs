#nullable enable
using BetterEmployees.Enums;
using BetterEmployees.Features;
using System;

namespace BetterEmployees.Extensions
{
    public static class StorageExtensions
    {
        public static Tuple<int, StorageMode>? GetEmptyStorageSlot(this Data_Container storage, int productId)
        {
            Tuple<int, StorageMode>? value = null;

            int[] realProductArray = storage.productInfoArray;
            int[] savedProductArray = ProductArray.Get(storage);

            int slotCount = realProductArray.Length / 2;

            if (ModEntry.StorageOrderSave)
            {
                for (int slot = 0; slot < slotCount; slot++)
                {
                    if (realProductArray[slot * 2] == -1 && savedProductArray[slot * 2] == productId)
                        return new Tuple<int, StorageMode>(slot, StorageMode.InStorageOrder);

                    if (realProductArray[slot * 2] == -1 && value?.Item2 != StorageMode.FullyEmpty)
                        value = new Tuple<int, StorageMode>(slot, savedProductArray[slot * 2] == -1 ? StorageMode.FullyEmpty : StorageMode.EmptyButReserved);
                }
            }
            else
            {
                for (int slot = 0; slot < slotCount; slot++)
                {
                    if (realProductArray[slot * 2] == -1)
                        return new Tuple<int, StorageMode>(slot, StorageMode.InStorageOrder);
                }
            }

            return value;
        }
    }
}
