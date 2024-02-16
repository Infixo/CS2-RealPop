using Game;
using Game.Prefabs;
using HarmonyLib;
using RealPop.Systems;

namespace RealPop.Patches;

[HarmonyPatch]
class RealPopPatches
{
    [HarmonyPatch(typeof(Game.Common.SystemOrder), "Initialize")]
    [HarmonyPostfix]
    public static void Initialize_Postfix(UpdateSystem updateSystem)
    {
        updateSystem.UpdateAt<AgingSystem_RealPop>(SystemUpdatePhase.GameSimulation);
        updateSystem.UpdateAt<ApplyToSchoolSystem_RealPop>(SystemUpdatePhase.GameSimulation);
        updateSystem.UpdateAt<BirthSystem_RealPop>(SystemUpdatePhase.GameSimulation);
        updateSystem.UpdateAt<CountHomesSystem>(SystemUpdatePhase.GameSimulation);
        updateSystem.UpdateAt<GraduationSystem_RealPop>(SystemUpdatePhase.GameSimulation);
        updateSystem.UpdateAt<SchoolAISystem_RealPop>(SystemUpdatePhase.GameSimulation);
        updateSystem.UpdateAt<CitizenInitializeSystem_RealPop>(SystemUpdatePhase.Modification5);
        if (Plugin.DeathChanceIncrease.Value > 0)
            updateSystem.UpdateAt<DeathCheckSystem_RealPop>(SystemUpdatePhase.GameSimulation);
        else
            Plugin.Log("Using original DeathCheckSystem.");
    }

    [HarmonyPatch(typeof(Game.Simulation.AgingSystem), "OnUpdate")]
    [HarmonyPrefix]
    static bool AgingSystem_OnUpdate(Game.Simulation.AgingSystem __instance)
    {
        //RealPop.Debug.Log("Original AgingSystem disabled.");
        //__instance.World.GetOrCreateSystemManaged<RealPop.Systems.AgingSystem_RealPop>().Update();
        return false; // don't execute the original system
    }

    [HarmonyPatch(typeof(Game.Simulation.ApplyToSchoolSystem), "OnUpdate")]
    [HarmonyPrefix]
    static bool ApplyToSchoolSystem_OnUpdate(Game.Simulation.ApplyToSchoolSystem __instance)
    {
        return false; // don't execute the original system
    }

    [HarmonyPatch(typeof(Game.Simulation.BirthSystem), "OnUpdate")]
    [HarmonyPrefix]
    static bool BirthSystem_OnUpdate(Game.Simulation.BirthSystem __instance)
    {
        return false; // don't execute the original system
    }

    [HarmonyPatch(typeof(Game.Simulation.DeathCheckSystem), "OnUpdate")]
    [HarmonyPrefix]
    static bool DeathCheckSystem_OnUpdate()
    {
        return Plugin.DeathChanceIncrease.Value <= 0; // don't execute the original if set to >0
    }

    [HarmonyPatch(typeof(Game.Simulation.GraduationSystem), "OnUpdate")]
    [HarmonyPrefix]
    static bool GraduationSystem_OnUpdate(Game.Simulation.GraduationSystem __instance)
    {
        return false; // don't execute the original system
    }

    [HarmonyPatch(typeof(Game.Simulation.SchoolAISystem), "OnUpdate")]
    [HarmonyPrefix]
    static bool SchoolAISystem_OnUpdate(Game.Simulation.SchoolAISystem __instance)
    {
        //RealPop.Debug.Log("Original SchoolAISystem disabled.");
        //__instance.World.GetOrCreateSystemManaged<RealPop.Systems.AgingSystem_RealPop>().Update();
        return false; // don't execute the original system
    }

    [HarmonyPatch(typeof(Game.Citizens.CitizenInitializeSystem), "OnUpdate")]
    [HarmonyPrefix]
    static bool CitizenInitializeSystem_OnUpdate()
    {
        //RealPop.Debug.Log("Original AgingSystem disabled.");
        //__instance.World.GetOrCreateSystemManaged<RealPop.Systems.AgingSystem_RealPop>().Update();
        return false; // don't execute the original system
    }

    // TURN OFF STACK TRACE IN LOGS
#if DEBUG
    [HarmonyPatch(typeof(Game.SceneFlow.GameManager), "SetNativeStackTrace")]
    [HarmonyPrefix]
    static bool SetNativeStackTrace()
    {
        Application.SetStackTraceLogType(LogType.Assert, StackTraceLogType.Full);
        Application.SetStackTraceLogType(LogType.Error, StackTraceLogType.Full);
        Application.SetStackTraceLogType(LogType.Exception, StackTraceLogType.Full);
        Application.SetStackTraceLogType(LogType.Log, StackTraceLogType.None); // turn off for info messages
        Application.SetStackTraceLogType(LogType.Warning, StackTraceLogType.Full);
        UnityEngine.Debug.Log("Game version: " + Version.current.fullVersion);
        UnityEngine.Debug.Log(Game.SceneFlow.GameManager.GetSystemInfoString());
        return false; // don't execute the original
    }
#endif

    [HarmonyPatch(typeof(Game.Prefabs.PrefabSystem), "AddPrefab")]
    [HarmonyPrefix]
    public static bool PrefabSystem_AddPrefab_Prefix(PrefabBase prefab)
    {
        if (prefab.name == "CoupleHousehold")
        {
            HouseholdPrefab comp = prefab.GetComponent<HouseholdPrefab>();
            //comp.m_Weight = 0;
            comp.m_AdultCount = 2; // vanilla prefab has 1 Adult for CoupleHousehold
            Plugin.Log($"Patched {prefab.name} for AdultCount={comp.m_AdultCount}");
        }
        else if (prefab.name == "CoupleHousehold2") // family 2+2
        {
            HouseholdPrefab comp = prefab.GetComponent<HouseholdPrefab>();
            comp.m_Weight = 5; // vanilla = 1
            Plugin.Log($"Patched {prefab.name} for Weight={comp.m_Weight}");
        }
        else if (prefab.name == "CoupleHousehold5") // 5 children!!! - will be family 2+1
        {
            HouseholdPrefab comp = prefab.GetComponent<HouseholdPrefab>();
            comp.m_Weight = 7;
            comp.m_ChildCount = 1; // vanilla is missing classic 2+1 family
            Plugin.Log($"Patched {prefab.name} for ChildCount={comp.m_ChildCount} and Weight={comp.m_Weight}");
        }
        else if (prefab.name == "DemandParameters") // 240208 fix for homeless demand factor
        {
            DemandPrefab comp = prefab.GetComponent<DemandPrefab>();
            comp.m_HomelessEffect = 20f; // vanilla is 0.2f which makes it virtually meaningless
            Plugin.Log($"Patched {prefab.name} for HomelessEffect={comp.m_HomelessEffect}");
        }
        return true;
    }

}
