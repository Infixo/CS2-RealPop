using System;
using HarmonyLib;
using RealPop.Systems;

namespace RealPop.Patches;

[HarmonyPatch]
class HouseholdSpawnSystemPatches
{
    private static CountHomesSystem m_CountHomesSystem;
    private static int s_SpawnCounter = 0;

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
            //Plugin.Log($"New households limiter: FreeRatioTreshold={Plugin.FreeRatioTreshold.Value}, FreeRatioFullSpeed={Plugin.FreeRatioFullSpeed.Value}.");
        }

        // wait for the CountHomesSystem to activate
        if (m_CountHomesSystem.totalProperties < 0)
        {
            //Plugin.Log($"NewCims: households not counted"); // debug
            return false;
        }

        // enable the feature only after 100 properties are in the city
        if (m_CountHomesSystem.totalProperties < 100)
        {
            //Plugin.Log($"NewCims: households <100"); // debug
            return true;
        }

        int cappedRatio = Math.Clamp(1000 * m_CountHomesSystem.freeProperties / m_CountHomesSystem.totalProperties, Plugin.FreeRatioTreshold.Value, Plugin.FreeRatioFullSpeed.Value);

        //int tempCounter = s_SpawnCounter;

        s_SpawnCounter += Plugin.FreeRatioTreshold.Value;
        s_SpawnCounter -= cappedRatio;

        bool activate = s_SpawnCounter <= 0;

        if (activate) s_SpawnCounter += Plugin.FreeRatioFullSpeed.Value;

        //Plugin.Log($"NewCims: households {m_CountHomesSystem.freeProperties}/{m_CountHomesSystem.totalProperties} ratio {cappedRatio} counter {tempCounter} -> {s_SpawnCounter} run {activate}");
        return activate; // true - the job will run, false - no new households
    }
    /*
    [HarmonyPatch(typeof(Game.Simulation.HouseholdSpawnSystem), "GetUpdateInterval")]
    [HarmonyPrefix]
    static bool HouseholdSpawnSystem_GetUpdateInterval(ref int __result)
    {
        __result = 64;
        return false;
    }
    */
}
