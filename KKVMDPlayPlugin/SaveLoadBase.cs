using System;
using System.Xml;
using Studio;
using UnityEngine;

namespace KKVMDPlayPlugin
{
	
	public class SaveLoadBase
	{
		
		
		
		public int priority { get; set; }

		
		public int GetCharaNo(OCIChar ociChar)
		{
			return ociChar.objectInfo.dicKey;
		}

		
		public OCIChar GetChara(int sexType, int charNo)
		{
			ObjectCtrlInfo objectCtrlInfo = null;
			Singleton<Studio.Studio>.Instance.dicObjectCtrl.TryGetValue(charNo, out objectCtrlInfo);
			if (objectCtrlInfo is OCIChar)
			{
				return objectCtrlInfo as OCIChar;
			}
			return null;
		}

		
		public void SetAttr(XmlElement elem, string name, object value)
		{
			if (value != null)
			{
				elem.SetAttribute(name, value.ToString());
			}
		}

		
		public void SetAttrVec3(XmlElement elem, string name, Vector3 vec)
		{
			string value = string.Format("({0},{1},{2})", vec.x, vec.y, vec.z);
			elem.SetAttribute(name, value);
		}

		
		public float GetAttr(XmlElement elem, string name, float defaultValue)
		{
			string attribute = elem.GetAttribute(name);
			if (string.IsNullOrEmpty(attribute))
			{
				return defaultValue;
			}
			return float.Parse(attribute);
		}

		
		public bool GetAttr(XmlElement elem, string name, bool defaultValue)
		{
			string attribute = elem.GetAttribute(name);
			if (string.IsNullOrEmpty(attribute))
			{
				return defaultValue;
			}
			return bool.Parse(attribute);
		}

		
		public int GetAttr(XmlElement elem, string name, int defaultValue)
		{
			string attribute = elem.GetAttribute(name);
			if (string.IsNullOrEmpty(attribute))
			{
				return defaultValue;
			}
			return int.Parse(attribute);
		}

		
		public string GetAttr(XmlElement elem, string name, string defaultValue)
		{
			if (elem.HasAttribute(name))
			{
				return elem.GetAttribute(name);
			}
			return defaultValue;
		}

		
		public Vector3 GetAttrVec3(XmlElement elem, string name, Vector3 defaultValue)
		{
			string text = elem.GetAttribute(name);
			if (text != null && text.Length > 2)
			{
				text = text.Substring(1, text.Length - 2);
				string[] array = text.Split(new char[]
				{
					','
				});
				float num = float.Parse(array[0]);
				float num2 = float.Parse(array[1]);
				float num3 = float.Parse(array[2]);
				return new Vector3(num, num2, num3);
			}
			return defaultValue;
		}
	}
}
