using Game;
using Game.UI.InGame;
using HarmonyLib;
using RealPop.Systems;
using UnityEngine;

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
        updateSystem.UpdateAt<GraduationSystem_RealPop>(SystemUpdatePhase.GameSimulation);
        updateSystem.UpdateAt<SchoolAISystem_RealPop>(SystemUpdatePhase.GameSimulation);
        updateSystem.UpdateAt<CitizenInitializeSystem_RealPop>(SystemUpdatePhase.Modification5);
        updateSystem.UpdateAt<RealPop.UI.PopulationStructureUISystem>(SystemUpdatePhase.UIUpdate);
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
        //RealPop.Debug.Log("original system disabled");
        //__instance.World.GetOrCreateSystemManaged<RealPop.Systems.ApplyToSchoolSystem_RealPop>().Update();
        return false; // don't execute the original system
    }    

    [HarmonyPatch(typeof(Game.Simulation.GraduationSystem), "OnUpdate")]
    [HarmonyPrefix]
    static bool GraduationSystem_OnUpdate(Game.Simulation.GraduationSystem __instance)
    {
        //RealPop.Debug.Log("Original GraduationSystem disabled.");
        //__instance.World.GetOrCreateSystemManaged<RealPop.Systems.GraduationSystem_RealPop>().Update();
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
}
/*
    [HarmonyPrefix, HarmonyPatch(typeof(Game.Simulation.AgingSystem), "GetTeenAgeLimitInDays")]
    static bool GetTeenAgeLimitInDays_Prefix(ref int __result)
    {
        RealPop.Debug.Log("GetTeenAgeLimitInDays_Prefix");
        __result = 12; // 21
        return false; // don't execute the original method after this
    }

    [HarmonyPrefix, HarmonyPatch(typeof(Game.Simulation.AgingSystem), "GetAdultAgeLimitInDays")]
    static bool GetAdultAgeLimitInDays_Prefix(ref int __result)
    {
        RealPop.Debug.Log("GetAdultAgeLimitInDays_Prefix");
        __result = 24; // 36
        return false; // don't execute the original method after this
    }

    [HarmonyPrefix, HarmonyPatch(typeof(Game.Simulation.AgingSystem), "GetElderAgeLimitInDays")]
    static bool GetElderAgeLimitInDays_Prefix(ref int __result)
    {
        RealPop.Debug.Log("GetElderAgeLimitInDays_Prefix");
        __result = 77; // 84
        return false; // don't execute the original method after this
    }

}
*/


/*
 * 
// This example patch adds the loading of a custom ECS System after the AudioManager has
// its "OnGameLoadingComplete" method called. We're just using it as a entrypoint, and
// it won't affect anything related to audio.
[HarmonyPatch(typeof(AudioManager), "OnGameLoadingComplete")]
class AudioManager_OnGameLoadingComplete
{
    static void Postfix(AudioManager __instance, Colossal.Serialization.Entities.Purpose purpose, GameMode mode)
    {
        if (!mode.IsGameOrEditor())
            return;

        // Here we add our custom ECS System to the game's ECS World, so it's "online" at runtime
        __instance.World.GetOrCreateSystem<RealPopSystem>();
    }
}

// This example patch enables the editor in the main menu
[HarmonyPatch(typeof(MenuUISystem), "IsEditorEnabled")]
class MenuUISystem_IsEditorEnabledPatch
{
    static bool Prefix(ref bool __result)
    {
        __result = true;

        return false; // Ignore original function
    }
}
// Thanks to @89pleasure for the MenuUISystem_IsEditorEnabledPatch snippet above
// https://github.com/89pleasure/cities2-mod-collection/blob/71385c000779c23b85e5cc023fd36022a06e9916/EditorEnabled/Patches/MenuUISystemPatches.cs

*/