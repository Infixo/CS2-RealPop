using System;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;
using HarmonyLib;
using System.Collections.Generic;
using System.Text;

#if BEPINEX_V6
    using BepInEx.Unity.Mono;
#endif

namespace RealPop;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
    internal static new ManualLogSource Logger;
	
	// mod settings
	public static ConfigEntry<int> TeenAgeLimitInDays;
	public static ConfigEntry<int> AdultAgeLimitInDays;
	public static ConfigEntry<int> ElderAgeLimitInDays;
	public static ConfigEntry<float> GraduationLevel1; // elementary school
	public static ConfigEntry<float> GraduationLevel2; // high school
	public static ConfigEntry<float> GraduationLevel3; // college
	public static ConfigEntry<float> GraduationLevel4; // university

    private void Awake()
    {
        Logger = base.Logger;

        TeenAgeLimitInDays = base.Config.Bind<int>("Lifecycle", "TeenAgeLimitInDays", 12, "When Children become Teens; Vanilla 21");
        AdultAgeLimitInDays = base.Config.Bind<int>("Lifecycle", "AdultAgeLimitInDays", 24, "When Teens become Adults; Vanilla 36");
        ElderAgeLimitInDays = base.Config.Bind<int>("Lifecycle", "ElderAgeLimitInDays", 77, "When Adults become Seniors; Vanilla 84");
		GraduationLevel1 = base.Config.Bind<float>("Graduation", "Level1", 90f, "Elementary School; Vanilla 100");
        GraduationLevel2 = base.Config.Bind<float>("Graduation", "Level2", 75f, "High School; Vanilla 60");
        GraduationLevel3 = base.Config.Bind<float>("Graduation", "Level3", 90f, "College; Vanilla 90");
        GraduationLevel4 = base.Config.Bind<float>("Graduation", "Level4", 80f, "University; Vanilla 70");

        Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");

        var harmony = Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), MyPluginInfo.PLUGIN_GUID + "_Cities2Harmony");
        var patchedMethods = harmony.GetPatchedMethods().ToArray();

        Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} made patches! Patched methods: " + patchedMethods.Length);

        foreach (var patchedMethod in patchedMethods) {
            Logger.LogInfo($"Patched method: {patchedMethod.Module.Name}:{patchedMethod.Name}");
        }
    }
}
