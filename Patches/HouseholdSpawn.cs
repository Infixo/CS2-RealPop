using System;
using Unity.Mathematics;
using HarmonyLib;
using RealPop.Systems;

namespace RealPop.Patches
{

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
            if (Mod.setting.FreeRatioTreshold < 0)
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

            int cappedRatio = math.clamp(1000 * m_CountHomesSystem.freeProperties / m_CountHomesSystem.totalProperties, Mod.setting.FreeRatioTreshold, Mod.setting.FreeRatioFullSpeed);

            //int tempCounter = s_SpawnCounter;

            s_SpawnCounter += Mod.setting.FreeRatioTreshold;
            s_SpawnCounter -= cappedRatio;

            bool activate = s_SpawnCounter <= 0;

            if (activate) s_SpawnCounter += Mod.setting.FreeRatioFullSpeed;

            //Plugin.Log($"NewCims: households {m_CountHomesSystem.freeProperties}/{m_CountHomesSystem.totalProperties} ratio {cappedRatio} counter {tempCounter} -> {s_SpawnCounter} run {activate}");
            return activate; // true - the job will run, false - no new households
        }
    }

} // namespace