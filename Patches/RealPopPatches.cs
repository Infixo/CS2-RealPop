//using System;
//using System.Reflection.Emit;
//using System.Collections.Generic;
//using System.Linq;
//using Unity.Mathematics;
//using Colossal.UI;
using Game;
//using Game.Simulation;
//using Game.SceneFlow;
//using Game.Audio;
//using Game.UI.Menu;
using HarmonyLib;
using RealPop.Systems;
using UnityEngine;
//using BepInEx;

namespace RealPop.Patches;

[HarmonyPatch]
class RealPopPatches
{
    [HarmonyPatch(typeof(Game.Common.SystemOrder), "Initialize")]
    [HarmonyPostfix]
    public static void Initialize_Postfix(UpdateSystem updateSystem)
    {
        updateSystem.UpdateAt<AgingSystem_RealPop>(SystemUpdatePhase.GameSimulation);
        //updateSystem.UpdateAt<ApplyToSchoolSystem_RealPop>(SystemUpdatePhase.GameSimulation);
        updateSystem.UpdateAt<GraduationSystem_RealPop>(SystemUpdatePhase.GameSimulation);
        updateSystem.UpdateAt<SchoolAISystem_RealPop>(SystemUpdatePhase.GameSimulation);
    }

    // AGING SYSTEM REPLACEMENT
    /*
    [HarmonyPatch(typeof(Game.Simulation.AgingSystem), "OnCreate")]
    [HarmonyPrefix]
    static bool OnCreate(Game.Simulation.AgingSystem __instance)
    {
        UnityEngine.Debug.Log("AgingSystem.OnCreate_Prefix");
        __instance.World.GetOrCreateSystemManaged<RealPop.Systems.AgingSystem_RealPop>();
        __instance.World.GetOrCreateSystemManaged<Game.UpdateSystem>().UpdateAt<RealPop.Systems.AgingSystem_RealPop>(Game.SystemUpdatePhase.GameSimulation);
        return true; // Allow the original method to run so that we only receive update requests when necessary
    }
    */
    [HarmonyPatch(typeof(Game.Simulation.AgingSystem), "OnUpdate")]
    [HarmonyPrefix]
    static bool AgingSystem_OnUpdate(Game.Simulation.AgingSystem __instance)
    {
        //RealPop.Debug.Log("Original AgingSystem disabled.");
        //__instance.World.GetOrCreateSystemManaged<RealPop.Systems.AgingSystem_RealPop>().Update();
        return false; // don't execute the original system
    }

    // APPLY TO SCHOOL SYSTEM REPLACEMENT
    /*

    [HarmonyPatch(typeof(Game.Simulation.ApplyToSchoolSystem), "OnCreate")]
    [HarmonyPrefix]
    static bool OnCreate(Game.Simulation.ApplyToSchoolSystem __instance)
    {
        UnityEngine.Debug.Log("ApplyToSchoolSystem.OnCreate_Prefix");
        __instance.World.GetOrCreateSystemManaged<RealPop.Systems.ApplyToSchoolSystem_RealPop>();
        __instance.World.GetOrCreateSystemManaged<Game.UpdateSystem>().UpdateAt<RealPop.Systems.ApplyToSchoolSystem_RealPop>(Game.SystemUpdatePhase.GameSimulation);
        return true; // allow the original method to run so we can later receive update requests
    }
    */
    /*
    [HarmonyPatch(typeof(Game.Simulation.ApplyToSchoolSystem), "OnUpdate")]
    [HarmonyPrefix]
    static bool ApplyToSchoolSystem_OnUpdate(Game.Simulation.ApplyToSchoolSystem __instance)
    {
        //RealPop.Debug.Log("original system disabled");
        //__instance.World.GetOrCreateSystemManaged<RealPop.Systems.ApplyToSchoolSystem_RealPop>().Update();
        return false; // don't execute the original system
    }
    */

    // GRADUATION SYSTEM REPLACEMENT
    /*
    [HarmonyPatch(typeof(Game.Simulation.GraduationSystem), "OnCreate")]
    [HarmonyPrefix]
    static bool OnCreate(Game.Simulation.GraduationSystem __instance)
    {
        UnityEngine.Debug.Log("GraduationSystem.OnCreate_Prefix");
        __instance.World.GetOrCreateSystemManaged<RealPop.Systems.GraduationSystem_RealPop>();
        __instance.World.GetOrCreateSystemManaged<Game.UpdateSystem>().UpdateAt<RealPop.Systems.GraduationSystem_RealPop>(Game.SystemUpdatePhase.GameSimulation);
        return true; // Allow the original method to run so that we only receive update requests when necessary
    }
    */
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

    // TURN OFF STACK TRACE IN LOGS
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
}

/*

[HarmonyPrefix, HarmonyPatch(typeof(Game.Simulation.AgingSystem), "GetTeenAgeLimitInDays")]
static bool GetTeenAgeLimitInDays_Prefix(ref int __result)
{
    UnityEngine.Debug.Log("GetTeenAgeLimitInDays_Prefix");
    __result = 10; // 21
    return false; // don't execute the original method after this
}

[HarmonyPrefix, HarmonyPatch(typeof(Game.Simulation.AgingSystem), "GetAdultAgeLimitInDays")]
static bool GetAdultAgeLimitInDays_Prefix(ref int __result)
{
    UnityEngine.Debug.Log("GetAdultAgeLimitInDays_Prefix");
    __result = 36; // 36
    return false; // don't execute the original method after this
}

[HarmonyPrefix, HarmonyPatch(typeof(Game.Simulation.AgingSystem), "GetElderAgeLimitInDays")]
static bool GetElderAgeLimitInDays_Prefix(ref int __result)
{
    UnityEngine.Debug.Log("GetElderAgeLimitInDays_Prefix");
    __result = 84; // 84
    return false; // don't execute the original method after this
}

*/
/*
[HarmonyPrefix, HarmonyPatch(typeof(Game.Simulation.GraduationSystem), "GetGraduationProbability",
    new Type[] { typeof(int), typeof(int), typeof(float), typeof(float2), typeof(float2), typeof(float), typeof(float) },
    new ArgumentType[] { ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Normal })] // use ArgumentType.Ref for ref argument

static bool GetGraduationProbability_Prefix(ref float __result, int level, int wellbeing, float graduationModifier, float2 collegeModifier, float2 uniModifier, float studyWillingness, float efficiency)
{
    if (efficiency <= 0.001f)
    {
        __result = 0f;
        return false; // don't execute the original method after this
    }
    float num = math.saturate((0.5f + studyWillingness) * (float)wellbeing / 75f);
    float num2 = 0f;
    switch (level)
    {
        case 1:
            num2 = math.smoothstep(0f, 1f, 0.6f * num + 0.41f);
            break;
        case 2:
            num2 = 0.6f * math.log(2.6f * num + 1.1f);
            break;
        case 3:
            num2 = 90f * math.log(1.6f * num + 1f);
            num2 += collegeModifier.x;
            num2 += num2 * collegeModifier.y;
            num2 /= 100f;
            break;
        case 4:
            num2 = 70f * num;
            num2 += uniModifier.x;
            num2 += num2 * uniModifier.y;
            num2 /= 100f;
            break;
        default:
            num2 = 0f;
            break;
    }
    num2 = 1f - (1f - num2) / efficiency;
    __result = num2 + graduationModifier;
    UnityEngine.Debug.Log($"{MyPluginInfo.PLUGIN_GUID} School {level} ({efficiency}) GradProb={__result} input: {wellbeing},{graduationModifier},{collegeModifier},{uniModifier},{studyWillingness}");
    return false; // don't execute the original method after this
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