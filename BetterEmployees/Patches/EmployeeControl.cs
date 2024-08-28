using BetterEmployees.Features;
using HarmonyLib;
using System.Collections.Generic;
using System.Reflection.Emit;
using static HarmonyLib.AccessTools;

namespace BetterEmployees.Patches
{
    [HarmonyPatch(typeof(NPC_Manager), nameof(NPC_Manager.EmployeeNPCControl))]
    internal class EmployeeControl
    {
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            CodeMatcher matcher = new(instructions, generator);

            // Replace boxProductID = 0 with -1
            matcher
                .Start()
                .MatchStartForward
                (
                    new(OpCodes.Ldc_I4_0),
                    new(OpCodes.Stfld, Field(typeof(NPC_Info), nameof(NPC_Info.boxProductID)))
                )
                .Repeat(matcher =>
                {
                    matcher.SetOpcodeAndAdvance(OpCodes.Ldc_I4_M1);
                })
                .Instructions();

            Label skip = generator.DefineLabel();

            // Fix cashiers randomly stopping scanning
            matcher
                .Start()
                .MatchStartForward(new CodeMatch(OpCodes.Ldfld, Field(typeof(Data_Container), nameof(Data_Container.currentNPC))))
                .Advance(1)
                .Insert(
                    // if (!ShouldScan(employeeIndex))
                    //     goto skip;
                    new CodeInstruction(OpCodes.Ldarg_1),
                    new(OpCodes.Call, Method(typeof(EmployeesUtil), nameof(EmployeesUtil.ShouldScan))),
                    new(OpCodes.Brfalse_S, skip),

                    // component.state = 6;
                    // return;
                    new(OpCodes.Ldloc_0),
                    new(OpCodes.Ldc_I4_6),
                    new(OpCodes.Stfld, Field(typeof(NPC_Info), nameof(NPC_Info.state))),
                    new(OpCodes.Pop),
                    new(OpCodes.Ret),

                    // skip
                    new CodeInstruction(OpCodes.Nop).WithLabels(skip)
                );

            if (ModEntry.StorageSaveOrder)
            {
                // Storage employee check for empty containers
                matcher
                    .MatchStartForward(new CodeMatch(OpCodes.Call, Method(typeof(NPC_Manager), nameof(NPC_Manager.GetRandomGroundBox))))
                    .Advance(1)
                    .InsertAndAdvance(new CodeInstruction(OpCodes.Dup))
                    .Advance(1)
                    .RemoveInstructions(2)
                    .Insert(new CodeInstruction(OpCodes.Call, Method(typeof(EmployeesUtil), nameof(EmployeesUtil.GetProductEmptyContainer))));

                // Restockers check for empty containers
                matcher
                    .Start()
                    .MatchStartForward(new CodeMatch(OpCodes.Call, Method(typeof(NPC_Manager), nameof(NPC_Manager.GetFreeStorageContainer))))
                    .Repeat(matcher =>
                    {
                        matcher
                            .Advance(-1)
                            .SetOpcodeAndAdvance(OpCodes.Ldarg_1)
                            .SetAndAdvance(OpCodes.Call, Method(typeof(EmployeesUtil), nameof(EmployeesUtil.GetEmployeeEmptyContainer)));
                    });

                matcher
                    .Start()
                    .MatchStartForward(new CodeMatch(OpCodes.Call, Method(typeof(NPC_Manager), nameof(NPC_Manager.GetFreeStorageRow))))
                    .Repeat(matcher =>
                    {
                        matcher
                            .Advance(-2)
                            .SetOpcodeAndAdvance(OpCodes.Ldarg_1)
                            .SetOpcodeAndAdvance(OpCodes.Ldloc_S)
                            .SetAndAdvance(OpCodes.Call, Method(typeof(EmployeesUtil), nameof(EmployeesUtil.GetEmptyRow)));
                    });
            }

            // Restocker jobs
            if (ModEntry.RestockerJobs)
            {
                matcher
                .Start()
                .MatchStartForward
                (
                    new(OpCodes.Ldloc_2),
                    new(),
                    new(OpCodes.Ldfld, Field(typeof(NPC_Manager), nameof(NPC_Manager.storageOBJ))),
                    new(),
                    new(),
                    new(OpCodes.Ldfld, Field(typeof(NPC_Info), nameof(NPC_Info.productAvailableArray)))
                )
                .Insert
                (
                    new CodeInstruction(OpCodes.Ldarg_1).MoveLabelsFrom(matcher.Instruction),
                    new(OpCodes.Ldloc_0),
                    new(OpCodes.Ldfld, Field(typeof(NPC_Info), nameof(NPC_Info.productAvailableArray))),
                    new(OpCodes.Call, Method(typeof(EmployeesUtil), nameof(EmployeesUtil.AddRestockerJob)))
                );

                matcher
                    .Start()
                    .MatchEndForward
                    (
                        new CodeMatch(i => i.opcode == OpCodes.Stloc_S && i.operand is LocalBuilder { LocalIndex: 15 })
                    )
                    .Advance(-2)
                    .Insert(
                        new CodeInstruction(OpCodes.Ldarg_1).MoveLabelsFrom(matcher.Instruction),
                        new(OpCodes.Call, Method(typeof(EmployeesUtil), nameof(EmployeesUtil.RemoveRestockerJob)))
                    );

                matcher
                    .Start()
                    .MatchStartForward
                    (
                        new CodeMatch(OpCodes.Ldfld, Field(typeof(NPC_Info), nameof(NPC_Info.taskPriority)))
                    )
                    .Advance(2)
                    .Insert(
                        new(OpCodes.Ldarg_1),
                        new(OpCodes.Ldloc_3),
                        new(OpCodes.Call, Method(typeof(EmployeesUtil), nameof(EmployeesUtil.CleanupRestockerJob)))
                    );
            }

            return matcher.InstructionEnumeration();
        }
    }
}
