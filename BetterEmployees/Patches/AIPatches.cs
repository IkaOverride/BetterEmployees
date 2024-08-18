using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;

namespace BetterEmployees.Patches
{
    [HarmonyPatch(typeof(NPC_Manager), nameof(NPC_Manager.EmployeeNPCControl))]
    internal class AIPatches
    {
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            List<CodeInstruction> newInstructions = instructions.ToList();

            // Storage employee check for empty containers
            int index = newInstructions.FindIndex(instruction => instruction.Calls(AccessTools.Method(typeof(NPC_Manager), nameof(NPC_Manager.GetRandomGroundBox)))) + 2;

            newInstructions.RemoveRange(index, 2);
            newInstructions.InsertRange(index, [
                new(OpCodes.Ldloc_S, 21),
                new(OpCodes.Call, AccessTools.Method(typeof(BetterEmployees), nameof(BetterEmployees.GetProductEmptyContainer)))
            ]);

            // Restockers check for empty containers
            newInstructions
                .Select((instruction, index) => new { instruction, index })
                .Where(x => x.instruction.Calls(AccessTools.Method(typeof(NPC_Manager), nameof(NPC_Manager.GetFreeStorageContainer))))
                .ToList()
                .ForEach(x =>
                {
                    int index = x.index - 1;
                    List<Label> labels = newInstructions[index].labels;

                    newInstructions.RemoveRange(index, 2);
                    newInstructions.InsertRange(index, [
                        new CodeInstruction(OpCodes.Ldarg_1).WithLabels(labels),
                        new(OpCodes.Call, AccessTools.Method(typeof(BetterEmployees), nameof(BetterEmployees.GetEmployeeEmptyContainer)))
                    ]);
                });

            newInstructions
                .Select((instruction, index) => new { instruction, index })
                .Where(x => x.instruction.Calls(AccessTools.Method(typeof(NPC_Manager), nameof(NPC_Manager.GetFreeStorageRow))))
                .ToList()
                .ForEach(x =>
                {
                    int index = x.index - 2;
                    object variableIndex = newInstructions[index + 1].operand;

                    newInstructions.RemoveRange(index, 3);
                    newInstructions.InsertRange(index, [
                        new CodeInstruction(OpCodes.Ldarg_1),
                        new(OpCodes.Ldloc_S, variableIndex),
                        new(OpCodes.Call, AccessTools.Method(typeof(BetterEmployees), nameof(BetterEmployees.GetEmptyRow)))
                    ]);
                });

            // Replace boxProductID = 0 with -1
            newInstructions
                .Select((instruction, index) => new { instruction, index })
                .Where(x =>
                {
                    if (x.index + 2 >= newInstructions.Count)
                        return false;

                    CodeInstruction thirdInstruction = newInstructions[x.index + 2];

                    return x.instruction.opcode == OpCodes.Ldloc_0 && newInstructions[x.index + 1].opcode == OpCodes.Ldc_I4_0 && thirdInstruction.opcode == OpCodes.Stfld && thirdInstruction.operand == (object)AccessTools.Field(typeof(NPC_Info), nameof(NPC_Info.boxProductID));
                })
                .ToList()
                .ForEach(x => newInstructions[x.index + 1] = new(OpCodes.Ldc_I4, -1));

            // Fix cashiers randomly stopping scanning
            index = newInstructions.FindIndex(instruction => instruction.opcode == OpCodes.Ldfld && instruction.operand == (object)AccessTools.Field(typeof(Data_Container), nameof(Data_Container.currentNPC))) + 1;

            Label skip = generator.DefineLabel();

            newInstructions.InsertRange(index, [

                // if (!ShouldScan(employeeIndex))
                //     goto skip;
                new CodeInstruction(OpCodes.Ldarg_1),
                new(OpCodes.Call, AccessTools.Method(typeof(BetterEmployees), nameof(BetterEmployees.ShouldScan))),
                new(OpCodes.Brfalse_S, skip),

                // component.state = 6;
                // return;
                new(OpCodes.Ldloc_0),
                new(OpCodes.Ldc_I4_6),
                new(OpCodes.Stfld, AccessTools.Field(typeof(NPC_Info), nameof(NPC_Info.state))),
                new(OpCodes.Pop),
                new(OpCodes.Ret),

                // skip
                new CodeInstruction(OpCodes.Nop).WithLabels(skip)
            ]);

            // Restocker jobs
            index = newInstructions
            .Select((instruction, index) => new { instruction, index })
            .ToList()
            .FindIndex(x =>
            {
                if (x.index + 5 >= newInstructions.Count)
                    return false;

                CodeInstruction thirdInstruction = newInstructions[x.index + 2];
                CodeInstruction fifthInstruction = newInstructions[x.index + 5];

                return x.instruction.opcode == OpCodes.Ldloc_2 &&
                    thirdInstruction.opcode == OpCodes.Ldfld && thirdInstruction.operand == (object)AccessTools.Field(typeof(NPC_Manager), nameof(NPC_Manager.storageOBJ)) &&
                    fifthInstruction.opcode == OpCodes.Ldfld && fifthInstruction.operand == (object)AccessTools.Field(typeof(NPC_Info), nameof(NPC_Info.productAvailableArray));
            });

            newInstructions.InsertRange(index, [
                new CodeInstruction(OpCodes.Ldarg_1).MoveLabelsFrom(newInstructions[index]),
                new CodeInstruction(OpCodes.Ldloc_0),
                new(OpCodes.Ldfld, AccessTools.Field(typeof(NPC_Info), nameof(NPC_Info.productAvailableArray))),
                new(OpCodes.Call, AccessTools.Method(typeof(BetterEmployees), nameof(BetterEmployees.AddRestockerJob)))
            ]);

            index = newInstructions.FindIndex(instruction => instruction.opcode == OpCodes.Stloc_S && instruction.operand is LocalBuilder { LocalIndex: 15 }) - 2;
            newInstructions.InsertRange(index, [
                new CodeInstruction(OpCodes.Ldarg_1).MoveLabelsFrom(newInstructions[index]),
                new(OpCodes.Call, AccessTools.Method(typeof(BetterEmployees), nameof(BetterEmployees.RemoveRestockerJob)))
            ]);

            index = newInstructions.FindIndex(instruction => instruction.opcode == OpCodes.Ldfld && instruction.operand == (object)AccessTools.Field(typeof(NPC_Info), nameof(NPC_Info.taskPriority))) + 2;

            newInstructions.InsertRange(index, [
                new(OpCodes.Ldarg_1),
                new(OpCodes.Ldloc_3),
                new(OpCodes.Call, AccessTools.Method(typeof(BetterEmployees), nameof(BetterEmployees.CleanupRestockerJob)))
            ]);

            for (int z = 0; z < newInstructions.Count; z++)
                yield return newInstructions[z];
        }
    }
}
