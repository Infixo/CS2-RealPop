﻿using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;
using HarmonyLib;
using System.Collections.Generic;
using System.Text;
using Colossal.Logging;

#if BEPINEX_V6
    using BepInEx.Unity.Mono;
#endif

namespace RealPop;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
    internal static new ManualLogSource Logger; // BepInEx logging
    private static ILog s_Log; // CO logging

    public static void Log(string text, bool bMethod = false)
    {
        if (bMethod) text = GetCallingMethod(2) + ": " + text;
        Logger.LogInfo(text);
        s_Log.Info(text);
    }

    public static void LogStack(string text)
    {
        //string msg = GetCallingMethod(2) + ": " + text + " STACKTRACE";
        Logger.LogInfo(text + " STACKTRACE");
        s_Log.logStackTrace = true;
        s_Log.Info(text + "STACKTRACE");
        s_Log.logStackTrace = false;
    }

    /// <summary>
    /// Gets the method from the specified <paramref name="frame"/>.
    /// </summary>
    public static string GetCallingMethod(int frame)
    {
        StackTrace st = new StackTrace();
        MethodBase mb = st.GetFrame(frame).GetMethod(); // 0 - GetCallingMethod, 1 - Log, 2 - actual function calling a Log method
        return mb.DeclaringType + "." + mb.Name;
    }

    // mod settings
    public static ConfigEntry<int> TeenAgeLimitInDays;
	public static ConfigEntry<int> AdultAgeLimitInDays;
	public static ConfigEntry<int> ElderAgeLimitInDays;
    public static ConfigEntry<int> Education2InDays; // high school
    public static ConfigEntry<int> Education3InDays; // college
    public static ConfigEntry<int> Education4InDays; // university
    public static ConfigEntry<float> GraduationLevel1; // elementary school
	public static ConfigEntry<float> GraduationLevel2; // high school
	public static ConfigEntry<float> GraduationLevel3; // college
	public static ConfigEntry<float> GraduationLevel4; // university

    private void Awake()
    {
        Logger = base.Logger;

        // CO logging standard as described here https://cs2.paradoxwikis.com/Logging
        s_Log = LogManager.GetLogger(MyPluginInfo.PLUGIN_NAME);

        TeenAgeLimitInDays = base.Config.Bind<int>("Lifecycle", "TeenAgeLimitInDays", 12, "When Children become Teens; Vanilla 21");
        AdultAgeLimitInDays = base.Config.Bind<int>("Lifecycle", "AdultAgeLimitInDays", 21, "When Teens become Adults; Vanilla 36");
        ElderAgeLimitInDays = base.Config.Bind<int>("Lifecycle", "ElderAgeLimitInDays", 75, "When Adults become Seniors; Vanilla 84");
        Education2InDays = base.Config.Bind<int>("Schools", "Education2InDays", 3, "How long High School should typically last (only for Teens)");
        Education3InDays = base.Config.Bind<int>("Schools", "Education3InDays", 4, "How long College should typically last (for both Teens and Adults)");
        Education4InDays = base.Config.Bind<int>("Schools", "Education4InDays", 5, "How long University should typically last (only for Adults)");
        GraduationLevel1 = base.Config.Bind<float>("Graduation", "Level1", 90f, "Elementary School; Vanilla 100");
        GraduationLevel2 = base.Config.Bind<float>("Graduation", "Level2", 80f, "High School; Vanilla 60");
        GraduationLevel3 = base.Config.Bind<float>("Graduation", "Level3", 80f, "College; Vanilla 90");
        GraduationLevel4 = base.Config.Bind<float>("Graduation", "Level4", 70f, "University; Vanilla 70");

        Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");

        var harmony = Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), MyPluginInfo.PLUGIN_GUID + "_Cities2Harmony");
        var patchedMethods = harmony.GetPatchedMethods().ToArray();

        Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} made patches! Patched methods: " + patchedMethods.Length);

        foreach (var patchedMethod in patchedMethods) {
            Logger.LogInfo($"Patched method: {patchedMethod.Module.Name}:{patchedMethod.Name}");
        }
    }
}
