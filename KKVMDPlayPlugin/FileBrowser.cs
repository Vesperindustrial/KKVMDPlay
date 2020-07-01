using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace KKVMDPlayPlugin
{
	
	public class FileBrowser
	{
		
		
		
		public string CurrentDirectory
		{
			get
			{
				return this.m_currentDirectory;
			}
			set
			{
				this.SetNewDirectory(value);
				this.SwitchDirectoryNow();
			}
		}

		
		
		
		public string SelectionPattern
		{
			get
			{
				return this.m_filePattern;
			}
			set
			{
				this.m_filePattern = value;
				this.ReadDirectoryContents();
			}
		}

		
		
		
		public Texture2D DirectoryImage
		{
			get
			{
				return this.m_directoryImage;
			}
			set
			{
				this.m_directoryImage = value;
				this.BuildContent();
			}
		}

		
		
		
		public Texture2D FileImage
		{
			get
			{
				return this.m_fileImage;
			}
			set
			{
				this.m_fileImage = value;
				this.BuildContent();
			}
		}

		
		
		
		public FileBrowserType BrowserType
		{
			get
			{
				return this.m_browserType;
			}
			set
			{
				this.m_browserType = value;
				this.ReadDirectoryContents();
			}
		}

		
		
		protected GUIStyle CentredText
		{
			get
			{
				if (this.m_centredText == null)
				{
					this.m_centredText = new GUIStyle(GUI.skin.label);
					this.m_centredText.alignment = (TextAnchor)3;
					this.m_centredText.fixedHeight = GUI.skin.button.fixedHeight;
				}
				return this.m_centredText;
			}
		}

		
		public FileBrowser(Rect screenRect, string name, FileBrowser.FinishedCallback callback)
		{
			this.m_name = name;
			this.m_screenRect = screenRect;
			this.m_browserType = FileBrowserType.File;
			this.m_callback = callback;
			this.SetNewDirectory(Directory.GetCurrentDirectory());
			this.SwitchDirectoryNow();
		}

		
		protected void SetNewDirectory(string directory)
		{
			this.m_newDirectory = directory;
		}

		
		protected void SwitchDirectoryNow()
		{
			if (this.m_newDirectory == null || this.m_currentDirectory == this.m_newDirectory)
			{
				return;
			}
			this.m_currentDirectory = this.m_newDirectory;
			this.m_scrollPosition = Vector2.zero;
			this.m_selectedDirectory = (this.m_selectedNonMatchingDirectory = (this.m_selectedFile = -1));
			this.ReadDirectoryContents();
		}

		
		protected void ReadDirectoryContents()
		{
			if (this.m_currentDirectory == null || this.m_currentDirectory == "")
			{
				try
				{
					string[] logicalDrives = Directory.GetLogicalDrives();
					this.m_directories = new string[logicalDrives.Length];
					for (int i = 0; i < logicalDrives.Length; i++)
					{
						this.m_directories[i] = logicalDrives[i];
					}
					this.m_files = new string[0];
					this.m_currentDirectoryParts = new string[]
					{
						""
					};
					this.BuildContent();
					this.m_newDirectory = null;
				}
				catch (Exception value)
				{
					Console.WriteLine(value);
					DriveInfo[] drives = DriveInfo.GetDrives();
					this.m_directories = new string[drives.Length];
					for (int j = 0; j < drives.Length; j++)
					{
						this.m_directories[j] = drives[j].Name;
					}
					this.m_files = new string[0];
					this.m_currentDirectoryParts = new string[]
					{
						""
					};
					this.BuildContent();
					this.m_newDirectory = null;
				}
				return;
			}
			string[] array = this.m_currentDirectory.Split(new char[]
			{
				Path.DirectorySeparatorChar
			});
			this.m_currentDirectoryParts = new string[array.Length + 1];
			this.m_currentDirectoryParts[0] = "";
			for (int k = 0; k < array.Length; k++)
			{
				this.m_currentDirectoryParts[k + 1] = array[k];
			}
			if (this.SelectionPattern != null)
			{
				string directoryName = Path.GetDirectoryName(this.m_currentDirectory);
				string[] directories;
				if (directoryName == null)
				{
					directories = Directory.GetDirectories(this.m_currentDirectory + "\\");
				}
				else
				{
					directories = Directory.GetDirectories(directoryName, this.SelectionPattern);
				}
				this.m_currentDirectoryMatches = (Array.IndexOf<string>(directories, this.m_currentDirectory) >= 0);
			}
			else
			{
				this.m_currentDirectoryMatches = false;
			}
			if (this.BrowserType == FileBrowserType.File || this.SelectionPattern == null)
			{
				this.m_directories = Directory.GetDirectories(this.m_currentDirectory);
				this.m_nonMatchingDirectories = new string[0];
			}
			else
			{
				this.m_directories = Directory.GetDirectories(this.m_currentDirectory, this.SelectionPattern);
				List<string> list = new List<string>();
				foreach (string text in Directory.GetDirectories(this.m_currentDirectory))
				{
					if (Array.IndexOf<string>(this.m_directories, text) < 0)
					{
						list.Add(text);
					}
				}
				this.m_nonMatchingDirectories = list.ToArray();
				for (int m = 0; m < this.m_nonMatchingDirectories.Length; m++)
				{
					int num = this.m_nonMatchingDirectories[m].LastIndexOf(Path.DirectorySeparatorChar);
					this.m_nonMatchingDirectories[m] = this.m_nonMatchingDirectories[m].Substring(num + 1);
				}
				Array.Sort<string>(this.m_nonMatchingDirectories);
			}
			for (int n = 0; n < this.m_directories.Length; n++)
			{
				this.m_directories[n] = this.m_directories[n].Substring(this.m_directories[n].LastIndexOf(Path.DirectorySeparatorChar) + 1);
			}
			if (this.BrowserType == FileBrowserType.Directory || this.SelectionPattern == null)
			{
				this.m_files = Directory.GetFiles(this.m_currentDirectory);
				this.m_nonMatchingFiles = new string[0];
			}
			else
			{
				string[] array3 = this.SelectionPattern.Split(new char[]
				{
					';'
				});
				List<string> list2 = new List<string>();
				foreach (string searchPattern in array3)
				{
					string[] files = Directory.GetFiles(this.m_currentDirectory, searchPattern);
					list2 = list2.Union(files).ToList<string>();
				}
				this.m_files = list2.ToArray();
				List<string> list3 = new List<string>();
				foreach (string text2 in Directory.GetFiles(this.m_currentDirectory))
				{
					if (Array.IndexOf<string>(this.m_files, text2) < 0)
					{
						list3.Add(text2);
					}
				}
				this.m_nonMatchingFiles = list3.ToArray();
				for (int num2 = 0; num2 < this.m_nonMatchingFiles.Length; num2++)
				{
					this.m_nonMatchingFiles[num2] = Path.GetFileName(this.m_nonMatchingFiles[num2]);
				}
				Array.Sort<string>(this.m_nonMatchingFiles);
			}
			for (int num3 = 0; num3 < this.m_files.Length; num3++)
			{
				this.m_files[num3] = Path.GetFileName(this.m_files[num3]);
			}
			Array.Sort<string>(this.m_files);
			this.BuildContent();
			this.m_newDirectory = null;
		}

		
		protected void BuildContent()
		{
			this.m_directoriesWithImages = new GUIContent[this.m_directories.Length];
			for (int i = 0; i < this.m_directoriesWithImages.Length; i++)
			{
				this.m_directoriesWithImages[i] = new GUIContent(this.m_directories[i], this.DirectoryImage);
			}
			this.m_nonMatchingDirectoriesWithImages = new GUIContent[this.m_nonMatchingDirectories.Length];
			for (int j = 0; j < this.m_nonMatchingDirectoriesWithImages.Length; j++)
			{
				this.m_nonMatchingDirectoriesWithImages[j] = new GUIContent(this.m_nonMatchingDirectories[j], this.DirectoryImage);
			}
			this.m_filesWithImages = new GUIContent[this.m_files.Length];
			for (int k = 0; k < this.m_filesWithImages.Length; k++)
			{
				this.m_filesWithImages[k] = new GUIContent(this.m_files[k], this.FileImage);
			}
			this.m_nonMatchingFilesWithImages = new GUIContent[this.m_nonMatchingFiles.Length];
			for (int l = 0; l < this.m_nonMatchingFilesWithImages.Length; l++)
			{
				this.m_nonMatchingFilesWithImages[l] = new GUIContent(this.m_nonMatchingFiles[l], this.FileImage);
			}
		}

		
		public void OnGUIAsWindow(int winID)
		{
			this.m_screenRect = GUI.Window(winID, this.m_screenRect, new GUI.WindowFunction(this.OnGUIWindow), this.m_name, GUI.skin.window);
		}

		
		public void OnGUI()
		{
			GUILayout.BeginArea(this.m_screenRect, this.m_name, GUI.skin.window);
			this.OnGUI(-1);
			GUILayout.EndArea();
		}

		
		public void OnGUIWindow(int winID)
		{
			this.OnGUI(winID);
			GUI.DragWindow();
		}

		
		public void OnGUI(int winID)
		{
			GUI.skin.GetStyle("List Item").alignment = (TextAnchor)3;
			GUILayout.BeginHorizontal(new GUILayoutOption[0]);
			for (int i = 0; i < this.m_currentDirectoryParts.Length; i++)
			{
				if (i == this.m_currentDirectoryParts.Length - 1)
				{
					GUILayout.Label(this.m_currentDirectoryParts[i], this.CentredText, new GUILayoutOption[0]);
				}
				else if (GUILayout.Button(this.m_currentDirectoryParts[i], new GUILayoutOption[0]))
				{
					if (i == 0)
					{
						this.SetNewDirectory("");
					}
					else
					{
						string text = this.m_currentDirectory;
						for (int j = this.m_currentDirectoryParts.Length - 1; j > i; j--)
						{
							text = Path.GetDirectoryName(text);
						}
						this.SetNewDirectory(text);
					}
				}
			}
			GUILayout.FlexibleSpace();
			GUILayout.EndHorizontal();
			this.m_scrollPosition = GUILayout.BeginScrollView(this.m_scrollPosition, false, true, GUI.skin.horizontalScrollbar, GUI.skin.verticalScrollbar, GUI.skin.box, new GUILayoutOption[0]);
			this.m_selectedDirectory = GUILayoutx.SelectionList(this.m_selectedDirectory, this.m_directoriesWithImages, new GUILayoutx.DoubleClickCallback(this.DirectoryDoubleClickCallback));
			if (this.m_selectedDirectory > -1)
			{
				this.m_selectedFile = (this.m_selectedNonMatchingDirectory = -1);
			}
			this.m_selectedNonMatchingDirectory = GUILayoutx.SelectionList(this.m_selectedNonMatchingDirectory, this.m_nonMatchingDirectoriesWithImages, new GUILayoutx.DoubleClickCallback(this.NonMatchingDirectoryDoubleClickCallback));
			if (this.m_selectedNonMatchingDirectory > -1)
			{
				this.m_selectedDirectory = (this.m_selectedFile = -1);
			}
			GUI.enabled = (this.BrowserType == FileBrowserType.File);
			this.m_selectedFile = GUILayoutx.SelectionList(this.m_selectedFile, this.m_filesWithImages, new GUILayoutx.DoubleClickCallback(this.FileDoubleClickCallback));
			GUI.enabled = true;
			if (this.m_selectedFile > -1)
			{
				this.m_selectedDirectory = (this.m_selectedNonMatchingDirectory = -1);
			}
			GUI.enabled = false;
			GUILayoutx.SelectionList(-1, this.m_nonMatchingFilesWithImages);
			GUI.enabled = true;
			GUILayout.EndScrollView();
			GUILayout.BeginHorizontal(new GUILayoutOption[0]);
			GUILayout.FlexibleSpace();
			if (GUILayout.Button("Cancel", new GUILayoutOption[]
			{
				GUILayout.Width(50f)
			}))
			{
				this.m_callback(null);
			}
			if (this.BrowserType == FileBrowserType.File)
			{
				GUI.enabled = (this.m_selectedFile > -1);
			}
			else if (this.SelectionPattern == null)
			{
				GUI.enabled = (this.m_selectedDirectory > -1);
			}
			else
			{
				GUI.enabled = (this.m_selectedDirectory > -1 || (this.m_currentDirectoryMatches && this.m_selectedNonMatchingDirectory == -1 && this.m_selectedFile == -1));
			}
			if (GUILayout.Button("Select", new GUILayoutOption[]
			{
				GUILayout.Width(50f)
			}))
			{
				if (this.BrowserType == FileBrowserType.File)
				{
					this.m_callback(Path.Combine(this.m_currentDirectory, this.m_files[this.m_selectedFile]));
				}
				else if (this.m_selectedDirectory > -1)
				{
					this.m_callback(Path.Combine(this.m_currentDirectory, this.m_directories[this.m_selectedDirectory]));
				}
				else
				{
					this.m_callback(this.m_currentDirectory);
				}
			}
			GUI.enabled = true;
			GUILayout.EndHorizontal();
			if (Event.current.type == (EventType)7)
			{
				this.SwitchDirectoryNow();
			}
		}

		
		protected void FileDoubleClickCallback(int i)
		{
			if (this.BrowserType == FileBrowserType.File)
			{
				this.m_callback(Path.Combine(this.m_currentDirectory, this.m_files[i]));
			}
		}

		
		protected void DirectoryDoubleClickCallback(int i)
		{
			if (this.m_directories[i].Contains(":"))
			{
				this.SetNewDirectory(this.m_directories[i]);
				return;
			}
			this.SetNewDirectory(Path.Combine(this.m_currentDirectory, this.m_directories[i]));
		}

		
		protected void NonMatchingDirectoryDoubleClickCallback(int i)
		{
			if (this.m_nonMatchingDirectories[i].Contains(":"))
			{
				this.SetNewDirectory(this.m_nonMatchingDirectories[i]);
				return;
			}
			this.SetNewDirectory(Path.Combine(this.m_currentDirectory, this.m_nonMatchingDirectories[i]));
		}

		
		protected string m_currentDirectory;

		
		protected string m_filePattern;

		
		protected Texture2D m_directoryImage;

		
		protected Texture2D m_fileImage;

		
		protected FileBrowserType m_browserType;

		
		protected string m_newDirectory;

		
		protected string[] m_currentDirectoryParts;

		
		protected string[] m_files;

		
		protected GUIContent[] m_filesWithImages;

		
		protected int m_selectedFile;

		
		protected string[] m_nonMatchingFiles;

		
		protected GUIContent[] m_nonMatchingFilesWithImages;

		
		protected int m_selectedNonMatchingDirectory;

		
		protected string[] m_directories;

		
		protected GUIContent[] m_directoriesWithImages;

		
		protected int m_selectedDirectory;

		
		protected string[] m_nonMatchingDirectories;

		
		protected GUIContent[] m_nonMatchingDirectoriesWithImages;

		
		protected bool m_currentDirectoryMatches;

		
		protected GUIStyle m_centredText;

		
		protected string m_name;

		
		protected Rect m_screenRect;

		
		protected Vector2 m_scrollPosition;

		
		protected FileBrowser.FinishedCallback m_callback;

		
		
		public delegate void FinishedCallback(string path);
	}
}
