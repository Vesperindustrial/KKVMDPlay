using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;
using UnityEngine;

namespace KKVMDPlayPlugin
{
    //You need to subscribe to SettingChanged events to make the values update immediately
    [BepInProcess("CharaStudio")]
	[BepInPlugin("KKVMDPlayPlugin.KKVMDPlayPlugin", "KKVMDPlayPlugin", "0.0.15")]
	public class KKVMDPlugin : BaseUnityPlugin
	{
        internal static new ManualLogSource Logger;

        public static ConfigEntry<string> settingUIKey { get; set; }
        public static ConfigEntry<bool> settingControllerAutoHide { get; set; }
        public static ConfigEntry<bool> settingControllerVisible { get; set; }
        public static ConfigEntry<bool> settingCacheGagEyesTexture { get; set; }
        public static ConfigEntry<bool> settingConfigVisible { get; set; }

        public KKVMDPlugin()
		{
            //Initial settings
            settingControllerAutoHide = Config.AddSetting("Main Game", "ControllerAutoHide", true, new ConfigDescription("Should the plugin bar be hidden automatically if the config window is closed?", null, new ConfigurationManagerAttributes { Browsable = false }));
            settingUIKey = Config.AddSetting("Main Game", "UIKey", "Ctrl+Shift+V", new ConfigDescription("Keyboard shortcut to show/hide the main plugin UI", null, new ConfigurationManagerAttributes { Browsable = false }));
            settingControllerVisible = Config.AddSetting("Main Game", "ControllerVisible", true, new ConfigDescription("Is the plugin UI bar visible?", null, new ConfigurationManagerAttributes { Browsable = false }));
            settingCacheGagEyesTexture = Config.AddSetting("Main Game", "CacheGagEyesTexture", false, new ConfigDescription("Some backend performance cache that doesn't work with post-Afterschool or Darkness DLC, leave as false. Requires studio restart if changed (at your own risk)!", null, new ConfigurationManagerAttributes { Browsable = false }));
            settingConfigVisible = Config.AddSetting("Main Game", "ConfigVisible", false, new ConfigDescription("Show or hide the config window on startup based on the previous game status", null, new ConfigurationManagerAttributes { Browsable = false }));

            if (settingCacheGagEyesTexture.Value == true)
            {
                EyeTextureCacheHook.InstallHook();
            }
        }


		
		private void Awake()
		{
		}

		
		private void Start()
		{
            Logger = base.Logger;
            KKVMDPlugin.Logger.Log(BepInEx.Logging.LogLevel.Info, "Loading KKVMDPlayPlugin");
			GameObject gameObject = new GameObject("KKVMDPlayPlugin");
			UnityEngine.Object.DontDestroyOnLoad(gameObject);
			VMDAnimationMgr.Install(gameObject);
			DebugHelper.Install(gameObject);

        }

		
		private void OnLevelWasLoaded(int level)
		{
		}
	}
}
