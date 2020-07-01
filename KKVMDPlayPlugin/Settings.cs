using System;
using BepInEx;
using BepInEx.Configuration;
using UnityEngine;

namespace KKVMDPlayPlugin
{
	
	internal class Settings
	{
		
		
		public static Settings Instance
		{
			get
			{
				if (Settings.instance == null)
				{
					Settings.instance = new Settings();
				}
				return Settings.instance;
			}
		}

		
		public Settings()
		{
			this.Load();
		}

		
		private void Load()
		{
		}

		
		//public bool GetBoolValue(string keyString, bool defaultValue, bool saveDefault = false)
		//{
		//	string stringValue = this.GetStringValue(keyString, null, false);
		//	if (stringValue != null)
		//	{
		//		try
		//		{
		//			return bool.Parse(stringValue);
		//		}
		//		catch (Exception)
		//		{
		//		}
		//	}
		//	if (saveDefault)
		//	{
		//		this.SetBoolValue(keyString, defaultValue);
		//	}
		//	return defaultValue;
		//}

		
		//public float GetFloatValue(string keyString, float defaultValue, bool saveDefault = false)
		//{
		//	string stringValue = this.GetStringValue(keyString, null, false);
		//	if (stringValue != null)
		//	{
		//		try
		//		{
		//			return float.Parse(stringValue);
		//		}
		//		catch (Exception)
		//		{
		//		}
		//	}
		//	if (saveDefault)
		//	{
		//		this.SetFloatValue(keyString, defaultValue);
		//	}
		//	return defaultValue;
		//}

		
		public string GetStringValue(string keyString, string defaultValue, bool saveDefault = false)
		{
			return "Ctrl+Shift+V";//Config.GetStringValue(keyString, defaultValue, "VMDPlay");
		}

		
		//public void SetBoolValue(string keyString, bool value)
		//{
		//	this.SetStringValue(keyString, value.ToString());
		//}

		
		//public void SetFloatValue(string keyString, float value)
		//{
		//	this.SetStringValue(keyString, value.ToString());
		//}

		
		//public void SetStringValue(string keyString, string value)
		//{
  //          //Config.SetEntry(keyString, value, "VMDPlay");
            
  //          this.Save();
		//}

		
		//public void Save()
		//{
		//	//Config.SaveConfig();
		//}

		
		private static Settings instance;

		
		public const string DEFAULT_SECTION_NAME = "VMDPlay";
	}
}
