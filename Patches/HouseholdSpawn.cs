using HarmonyLib;
using RealPop.Systems;

namespace RealPop.Patches;

[HarmonyPatch]
class HouseholdSpawnSystemPatches
{
    private static CountHomesSystem m_CountHomesSystem;

    [HarmonyPatch(typeof(Game.Simulation.HouseholdSpawnSystem), "OnUpdate")]
    [HarmonyPrefix]
    static bool HouseholdSpawnSystem_OnUpdate(Game.Simulation.HouseholdSpawnSystem __instance)
    {
        // continue as in vanilla, feature is turned off
        if (Plugin.FreeRatioTreshold.Value < 0)
            return true;

        //Plugin.Log($"Patched OnUpdate: {m_CountHomesSystem == null}");
        if (m_CountHomesSystem == null)
        {
            m_CountHomesSystem = __instance.World.GetOrCreateSystemManaged<CountHomesSystem>();
            //Plugin.Log("GET SYSTEM");
        }

        bool isAbove = m_CountHomesSystem.totalProperties > 0 ? ( 1000 * m_CountHomesSystem.freeProperties / m_CountHomesSystem.totalProperties > Plugin.FreeRatioTreshold.Value ) : false;
        
        //Plugin.Log($"IsAbove {isAbove}: total {m_CountHomesSystem.totalProperties} free {m_CountHomesSystem.freeProperties} ratio {1000 * m_CountHomesSystem.freeProperties / m_CountHomesSystem.totalProperties} tres {Plugin.FreeRatioTreshold.Value} ");
        return isAbove; // true - the job will run, false - no new households
    }

    [HarmonyPatch(typeof(Game.Simulation.HouseholdSpawnSystem), "GetUpdateInterval")]
    [HarmonyPrefix]
    static bool HouseholdSpawnSystem_GetUpdateInterval(ref int __result)
    {
        __result = 64;
        return false;
    }
}
