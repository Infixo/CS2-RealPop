using Colossal.IO.AssetDatabase;
using Colossal.Logging;
using Game;
using Game.Modding;
using Game.SceneFlow;

namespace RealPop
{
    public class Mod : IMod
    {
        // mod's instance
        public static Mod instance { get; private set; }

        // logging
        public static ILog log = LogManager.GetLogger($"{nameof(RealPop)}").SetShowsErrorsInUI(false);

        // settings
        //private Setting m_Setting;

        public static Setting setting { get;  private set; }

        public void OnLoad(UpdateSystem updateSystem)
        {
            instance = this;

            log.Info(nameof(OnLoad));

            if (GameManager.instance.modManager.TryGetExecutableAsset(this, out var asset))
                log.Info($"Current mod asset at {asset.path}");

            setting = new Setting(this);
            setting.RegisterInOptionsUI();
            GameManager.instance.localizationManager.AddSource("en-US", new LocaleEN(setting));

            AssetDatabase.global.LoadSettings(nameof(RealPop), setting, new Setting(this));
        }

        public void OnDispose()
        {
            log.Info(nameof(OnDispose));
            if (setting != null)
            {
                setting.UnregisterInOptionsUI();
                setting = null;
            }
        }
    }
}
