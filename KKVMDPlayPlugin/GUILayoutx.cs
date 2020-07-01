using System;
using UnityEngine;

namespace KKVMDPlayPlugin
{
	
	public class GUILayoutx
	{
		
		public static int SelectionList(int selected, GUIContent[] list)
		{
			return GUILayoutx.SelectionList(selected, list, "List Item", null);
		}

		
		public static int SelectionList(int selected, GUIContent[] list, GUIStyle elementStyle)
		{
			return GUILayoutx.SelectionList(selected, list, elementStyle, null);
		}

		
		public static int SelectionList(int selected, GUIContent[] list, GUILayoutx.DoubleClickCallback callback)
		{
			return GUILayoutx.SelectionList(selected, list, "List Item", callback);
		}

		
		public static int SelectionList(int selected, GUIContent[] list, GUIStyle elementStyle, GUILayoutx.DoubleClickCallback callback)
		{
			for (int i = 0; i < list.Length; i++)
			{
				Rect rect = GUILayoutUtility.GetRect(list[i], elementStyle);
				bool flag = rect.Contains(Event.current.mousePosition);
				if (flag && Event.current.type == (EventType)0) //Was originally null
				{
					selected = i;
					Event.current.Use();
				}
				else if (flag && callback != null && Event.current.type == (EventType)1)
				{
					callback(i);
					Event.current.Use();
				}
				else if (Event.current.type == (EventType)7)
				{
					elementStyle.Draw(rect, list[i], flag, false, i == selected, false);
				}
			}
			return selected;
		}

		
		public static int SelectionList(int selected, string[] list)
		{
			return GUILayoutx.SelectionList(selected, list, "List Item", null);
		}

		
		public static int SelectionList(int selected, string[] list, GUIStyle elementStyle)
		{
			return GUILayoutx.SelectionList(selected, list, elementStyle, null);
		}

		
		public static int SelectionList(int selected, string[] list, GUILayoutx.DoubleClickCallback callback)
		{
			return GUILayoutx.SelectionList(selected, list, "List Item", callback);
		}

		
		public static int SelectionList(int selected, string[] list, GUIStyle elementStyle, GUILayoutx.DoubleClickCallback callback)
		{
			for (int i = 0; i < list.Length; i++)
			{
				Rect rect = GUILayoutUtility.GetRect(new GUIContent(list[i]), elementStyle);
				bool flag = rect.Contains(Event.current.mousePosition);
				if (flag && Event.current.type == 0) //Was originally null
				{
					selected = i;
					Event.current.Use();
				}
				else if (flag && callback != null && Event.current.type == (EventType)1)
				{
					callback(i);
					Event.current.Use();
				}
				else if (Event.current.type == (EventType)7)
				{
					elementStyle.Draw(rect, list[i], flag, false, i == selected, false);
				}
			}
			return selected;
		}

		
		
		public delegate void DoubleClickCallback(int index);
	}
}
