using UnityEngine.AI;

namespace BetterEmployees.Patches
{
    internal class EmployeeCollisions
    {
        // Credit: @aeroluna
        internal static void Disable(NPC_Manager __instance)
        {
            foreach (NavMeshAgent agent in __instance.employeeParentOBJ.GetComponentsInChildren<NavMeshAgent>())
            {
                agent.obstacleAvoidanceType = ObstacleAvoidanceType.NoObstacleAvoidance;
            }
        }
    }
}
