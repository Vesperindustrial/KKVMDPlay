using System;
using System.Collections.Generic;
using UnityEngine;

namespace KKVMDPlayPlugin
{
	
	public class KeyUtil
	{
		
		public static KeyUtil Parse(string keyPattern)
		{
			string[] array = keyPattern.Split(new char[]
			{
				'+'
			});
			List<KeyCode> list = new List<KeyCode>();
			string text;
			if (array.Length == 1)
			{
				text = array[0];
			}
			else
			{
				for (int i = 0; i < array.Length - 1; i++)
				{
					string a = array[i].ToLower();
					if (a == "ctrl")
					{
						list.Add((KeyCode)306);
						list.Add((KeyCode)305);
					}
					else if (a == "shift")
					{
						list.Add((KeyCode)304);
						list.Add((KeyCode)303);
					}
					else if (a == "alt")
					{
						list.Add((KeyCode)308);
						list.Add((KeyCode)307);
					}
				}
				text = array[array.Length - 1].ToLower();
			}
			return new KeyUtil
			{
				supportKeys = list,
				key = text
			};
		}

		
		public bool TestKeyUp()
		{
			return Input.GetKeyUp(this.key) && this.TestSupports();
		}

		
		public bool TestKeyDown()
		{
			return Input.GetKeyUp(this.key) && this.TestSupports();
		}

		
		public bool TestSupports()
		{
			for (int i = 0; i < this.supportKeys.Count; i += 2)
			{
				if (!Input.GetKey(this.supportKeys[i]) && !Input.GetKey(this.supportKeys[i + 1]))
				{
					return false;
				}
			}
			return true;
		}

		
		private List<KeyCode> supportKeys = new List<KeyCode>();

		
		private string key;
	}
}
