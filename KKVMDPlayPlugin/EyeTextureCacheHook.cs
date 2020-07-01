using System;
using BepInEx;
using HarmonyLib;
using BepInEx.Harmony;

namespace KKVMDPlayPlugin
{
	
	public static class EyeTextureCacheHook
	{
		
		[HarmonyPrefix]
		[HarmonyPatch(typeof(ChaControl), "ChangeEyesPtn", new Type[]
		{
			typeof(int),
			typeof(bool)
		}, null)]
		public static bool ChangeEyesPtnPrefix(ChaControl __instance, int ptn, bool blend)
		{
			EyeTextureCache.Instance.ChangeEyesPtn(__instance, ptn, blend);
			return false;
		}

		
		public static void InstallHook()
		{
			try
			{
				KKVMDPlugin.Logger.Log(BepInEx.Logging.LogLevel.Debug, "Patch ChaControl#ChangeEyePtn().");
				HarmonyWrapper.PatchAll(typeof(EyeTextureCacheHook));
                KKVMDPlugin.Logger.Log(BepInEx.Logging.LogLevel.Debug, "Patch completed.");
			}
			catch (Exception ex)
			{
                KKVMDPlugin.Logger.Log(BepInEx.Logging.LogLevel.Error, ex);
			}
		}
	}
}
