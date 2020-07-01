using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx;
using Manager;
using Studio;

using UnityEngine;
using VRUtil;

namespace KKVMDPlayPlugin
{
    
    public class KKVMDGUI : MonoBehaviour
	{
        

		
		private void Start()
		{
            //KKVMDPlugin.settingControllerVisible.SettingChanged += this.SettingControllerVisible_SettingChanged;
            //KKVMDPlugin.settingControllerAutoHide.SettingChanged += 
            try
			{
				if (Application.dataPath.EndsWith("KoikatuVR_Data") || Environment.CommandLine.Contains("--vr") || Environment.CommandLine.Contains("--studiovr"))
				{
					this.isVR = true;
				}
				this.windowBG.SetPixel(0, 0, Color.black);
				this.windowBG.Apply();
				if (this.cameraCtrl == null)
				{
					this.cameraCtrl = base.gameObject.AddComponent<CameraCtrlOff>();
					this.cameraCtrl.ikInfoGui = this;
					this.cameraCtrl.enabled = true;
				}
				this.autoShowHideController = KKVMDPlugin.settingControllerAutoHide.Value;
                    //Settings.Instance.GetBoolValue("ControllerAutoHide", false, true);
			}
			catch (Exception)
			{
                KKVMDPlugin.Logger.Log(BepInEx.Logging.LogLevel.Error, "Failed to initialize KKVMDGUI");
			}
		}

		
		private void OnEnable()
		{
			if (this.cameraCtrl)
			{
				this.cameraCtrl.enabled = true;
			}
		}

		
		private void OnDisable()
		{
			if (this.cameraCtrl)
			{
				this.cameraCtrl.enabled = false;
			}
		}

		
		public void Clear()
		{
			this.focusChara = null;
			this.lastController = null;
			this.lastFilename = null;
			this.lastCameraVMDFilename = null;
			this.lastSoundFilename = null;
		}

		
		private GUISkin CreateGUISkin()
		{
			GUISkin guiskin = UnityEngine.Object.Instantiate<GUISkin>(GUI.skin);
			if (guiskin.FindStyle("List Item") == null)
			{
				GUIStyle[] array = new GUIStyle[guiskin.customStyles.Length + 1];
				for (int i = 0; i < guiskin.customStyles.Length; i++)
				{
					array[i] = guiskin.customStyles[i];
				}
				GUIStyle guistyle = new GUIStyle(guiskin.button);
				guistyle.name = "List Item";
				array[guiskin.customStyles.Length] = guistyle;
				guiskin.customStyles = array;
				KKVMDGUI.m_Apply.Invoke(guiskin, new object[0]);
			}
            KKVMDPlugin.Logger.Log(BepInEx.Logging.LogLevel.Debug, string.Format("GUISkin {0} Created.", guiskin));
			return guiskin;
		}

		
		private GUISkin CreateVRGUISkin()
		{
			GUISkin guiskin = VRIMGUIUtil.CreateVRGUISkin(this.guiSkin);
			GUIStyle style = guiskin.GetStyle("List Item");
			style.normal = guiskin.button.normal;
			style.onNormal = guiskin.button.onNormal;
            KKVMDPlugin.Logger.Log(BepInEx.Logging.LogLevel.Debug, string.Format("VRGUISkin {0} Created.", guiskin));
			return guiskin;
		}

		
		private void OnGUI()
		{
			GUISkin skin = GUI.skin;
			try
			{
				if (this.guiSkin == null)
				{
					this.guiSkin = this.CreateGUISkin();
					this.vrGUISkin = this.CreateVRGUISkin();
				}
				if (this.isVR)
				{
					GUI.skin = this.vrGUISkin;
				}
				else
				{
					GUI.skin = this.guiSkin;
				}
				if (this.visibleGUI)
				{
					try
					{
						if (this.m_fileBrowser != null)
						{
							this.m_fileBrowser.OnGUIAsWindow(this.dialogWindowID);
						}
						else
						{
							this.windowRect = GUI.Window(this.windowID, this.windowRect, new GUI.WindowFunction(this.FuncWindowGUI), this.windowTitle);
						}
					}
					catch (Exception value)
					{
						Console.WriteLine(value);
					}
				}
				this.ProcessControllerGUI();
			}
			finally
			{
				GUI.skin = skin;
			}
		}

		
		private float CalcAdjustedSliderMax(float value)
		{
			if (value <= 1f)
			{
				return 1f;
			}
			if (value <= 10f)
			{
				return 10f;
			}
			return 100f;
		}

		
		private void FuncWindowGUI(int winID)
		{
			this.styleBackup = new Dictionary<string, GUIStyle>();
			this.BackupGUIStyle("Button");
			this.BackupGUIStyle("Label");
			this.BackupGUIStyle("Toggle");
			try
			{
				if (GUIUtility.hotControl == 0)
				{
					this.cameraCtrl.enabled = false;
				}
				if (Event.current.type == 0) //Was originally null
                {
					GUI.FocusControl("");
					GUI.FocusWindow(winID);
					this.cameraCtrl.enabled = true;
					this.cameraCtrl.cameraCtrlOff = true;
				}
				this.hideController = false;
				GUI.enabled = true;
				GUI.skin.GetStyle("Button").alignment = (TextAnchor)4;
				GUIStyle style = GUI.skin.GetStyle("Label");
				style.alignment = (TextAnchor)3;
				style.wordWrap = false;
				GUI.skin.GetStyle("Toggle");
				GUILayout.BeginVertical(new GUILayoutOption[0]);
				if (this.focusChara != null && this.focusChara.objBody == null)
				{
					this.focusChara = null;
				}
				if (this.focusChara == null)
				{
					this.focusChara = this.FindFirstChaControl();
				}
				this.DrawVMDAnimationArea();
				GUILayout.EndVertical();
				GUI.DragWindow();
			}
			catch (Exception value)
			{
				Console.WriteLine(value);
			}
			finally
			{
				this.RestoreGUIStyle("Button");
				this.RestoreGUIStyle("Label");
				this.RestoreGUIStyle("Toggle");
			}
		}

		
		protected ChaControl FindFirstChaControl()
		{
			foreach (ChaControl chaControl in Singleton<Character>.Instance.dictEntryChara.Values.ToArray<ChaControl>())
			{
				if (chaControl != null && chaControl.loadEnd && chaControl.objBody)
				{
					return chaControl;
				}
			}
			return null;
		}

		
		protected ChaControl FindPrevNextChaControl(bool next)
		{
			List<ChaControl> list = new List<ChaControl>();
			foreach (ChaControl chaControl in Singleton<Character>.Instance.dictEntryChara.Values.ToArray<ChaControl>())
			{
				if (chaControl != null && chaControl.loadEnd && chaControl.objBody)
				{
					list.Add(chaControl);
				}
			}
			if (list.Count == 0)
			{
				return null;
			}
			if (this.focusChara != null)
			{
				int num = list.IndexOf(this.focusChara);
				if (num >= 0)
				{
					num += (next ? 1 : -1);
					num = (num + list.Count) % list.Count;
					return list[num];
				}
			}
			return list[0];
		}

		
		private void BackupGUIStyle(string name)
		{
			GUIStyle value = new GUIStyle(GUI.skin.GetStyle(name));
			this.styleBackup.Add(name, value);
		}

		
		private void RestoreGUIStyle(string name)
		{
			if (this.styleBackup.ContainsKey(name))
			{
				GUIStyle guistyle = this.styleBackup[name];
				GUIStyle style = GUI.skin.GetStyle(name);
				style.normal.textColor = guistyle.normal.textColor;
				style.alignment = guistyle.alignment;
				style.wordWrap = guistyle.wordWrap;
			}
		}

		
		private void DrawVMDAnimationArea()
		{
			this.EnsureResourceLoaded();
			GUI.skin.GetStyle("Button");
			List<string> list = new List<string>
			{
				"Character",
				"Camera",
				"Sound"
			};
			this.currentTab = GUILayout.Toolbar(this.currentTab, list.ToArray(), new GUILayoutOption[0]);
			if (this.currentTab == 0)
			{
				if (this.focusChara == null)
				{
					GUILayout.Label("Character not selected.", new GUILayoutOption[]
					{
						GUILayout.Width(300f)
					});
					return;
				}
				VMDAnimationController vmdanimationController = VMDAnimationController.Install(this.focusChara);
				if (vmdanimationController == null)
				{
					return;
				}
				this.DrawCharacterArea(vmdanimationController);
				return;
			}
			else
			{
				if (this.currentTab == 1)
				{
					this.DrawCameraArea();
					return;
				}
				if (this.currentTab == 2)
				{
					this.DrawSoundArea();
				}
				return;
			}
		}

		
		private void DrawCharacterArea(VMDAnimationController vmdAnimController)
		{
			if (this.focusChara != null)
			{
				GUILayout.BeginVertical(new GUILayoutOption[0]);
				if (vmdAnimController != this.lastController)
				{
					this.lastFilename = vmdAnimController.lastLoadedVMD;
					this.lastController = vmdAnimController;
				}
				if (this.lastFilename == null)
				{
					this.lastFilename = "";
				}
				if (this.lastSoundFilename == null)
				{
					if (VMDAnimationMgr.Instance.SoundMgr.audioFilePath != null)
					{
						this.lastSoundFilename = VMDAnimationMgr.Instance.SoundMgr.audioFilePath;
					}
					else
					{
						this.lastSoundFilename = "";
					}
				}
				GUILayout.BeginHorizontal(new GUILayoutOption[0]);
				if (GUILayout.Button("<", new GUILayoutOption[]
				{
					GUILayout.Width(20f),
					GUILayout.Height(25f)
				}))
				{
					this.focusChara = this.FindPrevNextChaControl(false);
				}
				if (GUILayout.Button(">", new GUILayoutOption[]
				{
					GUILayout.Width(20f),
					GUILayout.Height(25f)
				}))
				{
					this.focusChara = this.FindPrevNextChaControl(true);
				}
				GUILayout.Label((this.focusChara.chaFile.parameter.fullname ?? "") ?? "", new GUILayoutOption[]
				{
					GUILayout.Width(150f)
				});
				if (GUILayout.Button(vmdAnimController.VMDAnimEnabled ? "On" : "Off", new GUILayoutOption[]
				{
					GUILayout.Width(50f),
					GUILayout.Height(25f)
				}))
				{
					vmdAnimController.VMDAnimEnabled = !vmdAnimController.VMDAnimEnabled;
				}
				if (vmdAnimController.VMDAnimEnabled)
				{
					GUILayout.Space(30f);
					if (vmdAnimController.lastLoadedVMD != null && File.Exists(vmdAnimController.lastLoadedVMD))
					{
						GUILayout.Label(Path.GetFileNameWithoutExtension(vmdAnimController.lastLoadedVMD), new GUILayoutOption[0]);
					}
				}
				GUILayout.EndHorizontal();
				if (vmdAnimController.VMDAnimEnabled)
				{
					GUILayout.BeginHorizontal(new GUILayoutOption[0]);
					GUILayout.Label("VMD", new GUILayoutOption[]
					{
						GUILayout.Width(80f)
					});
					if (GUILayout.Button("Load", new GUILayoutOption[]
					{
						GUILayout.Width(50f),
						GUILayout.Height(25f)
					}))
					{
						vmdAnimController.LoadVMDAnimation(this.lastFilename, false);
					}
					if (GUILayout.Button("Reload", new GUILayoutOption[]
					{
						GUILayout.Width(50f),
						GUILayout.Height(25f)
					}))
					{
						vmdAnimController.ReloadVMDAnimation(true);
						this.lastFilename = vmdAnimController.lastLoadedVMD;
						VMDAnimationMgr.Instance.SoundMgr.PlaySound();
					}
					this.lastFilename = GUILayout.TextField(this.lastFilename, new GUILayoutOption[]
					{
						GUILayout.Width(350f),
						GUILayout.Height(25f)
					});
					if (GUILayout.Button("...", new GUILayoutOption[]
					{
						GUILayout.Width(30f),
						GUILayout.Height(25f)
					}))
					{
						this.m_fileBrowser = new FileBrowser(new Rect((float)(Screen.width / 2 - 300), 200f, 600f, 500f), "Choose .vmd File", new FileBrowser.FinishedCallback(this.FileSelectedCallback));
						this.m_fileBrowser.SelectionPattern = "*.vmd";
						this.m_fileBrowser.DirectoryImage = this.m_directoryImage;
						this.m_fileBrowser.FileImage = this.m_fileImage;
						if (!string.IsNullOrEmpty(this.lastLoadedFilePath) && File.Exists(this.lastLoadedFilePath))
						{
							this.m_fileBrowser.CurrentDirectory = Path.GetDirectoryName(this.lastLoadedFilePath);
						}
					}
					GUILayout.EndHorizontal();
					GUILayout.BeginHorizontal(new GUILayoutOption[0]);
					GUILayout.Label("Sound", new GUILayoutOption[]
					{
						GUILayout.Width(80f)
					});
					if (GUILayout.Button("Load", new GUILayoutOption[]
					{
						GUILayout.Width(50f),
						GUILayout.Height(25f)
					}))
					{
						VMDAnimationMgr.Instance.SoundMgr.SetSoundClip(this.lastSoundFilename);
					}
					if (GUILayout.Button("Clear", new GUILayoutOption[]
					{
						GUILayout.Width(50f),
						GUILayout.Height(25f)
					}))
					{
						VMDAnimationMgr.Instance.SoundMgr.ClearClip();
						this.lastSoundFilename = "";
					}
					this.lastSoundFilename = GUILayout.TextField(this.lastSoundFilename, new GUILayoutOption[]
					{
						GUILayout.Width(350f),
						GUILayout.Height(25f)
					});
					if (GUILayout.Button("...", new GUILayoutOption[]
					{
						GUILayout.Width(30f),
						GUILayout.Height(25f)
					}))
					{
						this.m_fileBrowser = new FileBrowser(new Rect((float)(Screen.width / 2 - 300), 200f, 600f, 500f), "Choose .wav or .ogg File", new FileBrowser.FinishedCallback(this.SoundFileSelectedCallback));
						this.m_fileBrowser.SelectionPattern = "*.wav;*.ogg";
						this.m_fileBrowser.DirectoryImage = this.m_directoryImage;
						this.m_fileBrowser.FileImage = this.m_fileImage;
						if (!string.IsNullOrEmpty(this.lastLoadedFilePath) && File.Exists(this.lastLoadedFilePath))
						{
							this.m_fileBrowser.CurrentDirectory = Path.GetDirectoryName(this.lastLoadedFilePath);
						}
					}
					GUILayout.EndHorizontal();
					GUILayout.BeginHorizontal(new GUILayoutOption[0]);
					GUILayout.Label("(Player)", new GUILayoutOption[]
					{
						GUILayout.Width(50f)
					});
					if (GUILayout.Button("Play", new GUILayoutOption[]
					{
						GUILayout.Width(50f),
						GUILayout.Height(25f)
					}))
					{
						vmdAnimController.Play();
						VMDAnimationMgr.Instance.SoundMgr.PlaySound();
					}
					if (GUILayout.Button("Pause", new GUILayoutOption[]
					{
						GUILayout.Width(50f),
						GUILayout.Height(25f)
					}))
					{
						vmdAnimController.Pause();
						VMDAnimationMgr.Instance.SoundMgr.PauseSound();
					}
					if (GUILayout.Button("Stop", new GUILayoutOption[]
					{
						GUILayout.Width(50f),
						GUILayout.Height(25f)
					}))
					{
						vmdAnimController.Stop();
						VMDAnimationMgr.Instance.SoundMgr.StopSound();
					}
					GUILayout.Space(30f);
					GUILayout.Label("(All)", new GUILayoutOption[]
					{
						GUILayout.Width(50f)
					});
					if (GUILayout.Button("Play", new GUILayoutOption[]
					{
						GUILayout.Width(50f),
						GUILayout.Height(25f)
					}))
					{
						VMDAnimationMgr.Instance.PlayAll();
					}
					if (GUILayout.Button("Pause", new GUILayoutOption[]
					{
						GUILayout.Width(50f),
						GUILayout.Height(25f)
					}))
					{
						VMDAnimationMgr.Instance.PauseAll();
					}
					if (GUILayout.Button("Stop", new GUILayoutOption[]
					{
						GUILayout.Width(50f),
						GUILayout.Height(25f)
					}))
					{
						VMDAnimationMgr.Instance.StopAll();
					}
					GUILayout.EndHorizontal();
					GUILayout.BeginHorizontal(new GUILayoutOption[0]);
					vmdAnimController.speed = this.AddSliderWithText("vmdAnimSpeed", "Speed", vmdAnimController.speed, 5f);
					GUILayout.EndHorizontal();
					GUILayout.BeginHorizontal(new GUILayoutOption[0]);
					GUILayout.Label("Loop", new GUILayoutOption[]
					{
						GUILayout.Width(30f)
					});
					if (GUILayout.Button(vmdAnimController.Loop ? "On" : "Off", new GUILayoutOption[]
					{
						GUILayout.Width(40f)
					}))
					{
						vmdAnimController.Loop = !vmdAnimController.Loop;
					}
					GUILayout.Space(5f);
					GUILayout.Label("Skirt", new GUILayoutOption[]
					{
						GUILayout.Width(30f)
					});
					if (GUILayout.Button(vmdAnimController.AdjustSkirtBones ? "On" : "Off", new GUILayoutOption[]
					{
						GUILayout.Width(40f)
					}))
					{
						vmdAnimController.AdjustSkirtBones = !vmdAnimController.AdjustSkirtBones;
					}
					GUILayout.Space(5f);
					GUILayout.Label("Face", new GUILayoutOption[]
					{
						GUILayout.Width(30f)
					});
					if (GUILayout.Button(vmdAnimController.faceAnimeEnabled ? "On" : "Off", new GUILayoutOption[]
					{
						GUILayout.Width(40f)
					}))
					{
						vmdAnimController.faceAnimeEnabled = !vmdAnimController.faceAnimeEnabled;
					}
					GUILayout.Space(5f);
					GUILayout.Label("IK(foot)", new GUILayoutOption[]
					{
						GUILayout.Width(45f)
					});
					if (GUILayout.Button(vmdAnimController.enableIK ? "On" : "Off", new GUILayoutOption[]
					{
						GUILayout.Width(40f)
					}))
					{
						vmdAnimController.enableIK = !vmdAnimController.enableIK;
					}
					GUILayout.Space(5f);
					if (vmdAnimController.enableIK)
					{
						GUILayout.Label("IK(toe)", new GUILayoutOption[]
						{
							GUILayout.Width(45f)
						});
						if (GUILayout.Button(vmdAnimController.IKWeight.disableToeIK ? "Off" : "On", new GUILayoutOption[]
						{
							GUILayout.Width(40f)
						}))
						{
							vmdAnimController.IKWeight.disableToeIK = !vmdAnimController.IKWeight.disableToeIK;
						}
					}
					GUILayout.Space(5f);
					if (vmdAnimController.enableIK)
					{
						GUILayout.Label("IK(on floor)", new GUILayoutOption[]
						{
							GUILayout.Width(60f)
						});
						if (GUILayout.Button(vmdAnimController.IKWeight.limitIKPositionFloor ? "On" : "Off", new GUILayoutOption[]
						{
							GUILayout.Width(40f)
						}))
						{
							vmdAnimController.IKWeight.limitIKPositionFloor = !vmdAnimController.IKWeight.limitIKPositionFloor;
						}
					}
					GUILayout.EndHorizontal();
					if (vmdAnimController.enableIK)
					{
						GUILayout.BeginHorizontal(new GUILayoutOption[0]);
						vmdAnimController.IKWeight.footIKPosWeight = this.AddSliderWithText("vmdIKFootPosWeight", "IK Weight(pos)", vmdAnimController.IKWeight.footIKPosWeight, 1f);
						GUILayout.EndHorizontal();
						GUILayout.BeginHorizontal(new GUILayoutOption[0]);
						vmdAnimController.IKWeight.lastFrameRefRate = this.AddSliderWithText("vmdIKFootRefWeight", "IK Weight(ref)", vmdAnimController.IKWeight.lastFrameRefRate, 1f);
						GUILayout.EndHorizontal();
					}
					GUILayout.BeginHorizontal(new GUILayoutOption[0]);
					float num = this.AddSliderWithTextFixedScale("Center(y)adjust", vmdAnimController.centerPosAdjust.y, -2f, 2f);
					if (num != vmdAnimController.centerPosAdjust.y)
					{
						vmdAnimController.centerPosAdjust = new Vector3(0f, num, 0f);
					}
					GUILayout.EndHorizontal();
					GUILayout.BeginHorizontal(new GUILayoutOption[0]);
					float num2 = this.AddSliderWithText("vmdModelScale", "Model Scale", vmdAnimController.quickAdjust.ScaleModel, 2f);
					if (num2 != vmdAnimController.quickAdjust.ScaleModel)
					{
						vmdAnimController.quickAdjust.ScaleModel = num2;
					}
					GUILayout.EndHorizontal();
					GUILayout.BeginHorizontal(new GUILayoutOption[0]);
					float num3 = this.AddSliderWithTextFixedScale("Move(y)scale", vmdAnimController.moveYScaleAdjust, 0f, 2f);
					if (num3 != vmdAnimController.moveYScaleAdjust)
					{
						vmdAnimController.moveYScaleAdjust = num3;
					}
					GUILayout.EndHorizontal();
					GUILayout.BeginHorizontal(new GUILayoutOption[0]);
					GUILayout.Label("VMD Load Config: (needs Reload): ", new GUILayoutOption[]
					{
						GUILayout.Width(200f)
					});
					GUILayout.EndHorizontal();
					GUILayout.BeginHorizontal(new GUILayoutOption[0]);
					float num4 = this.AddSliderWithText("vmdCenterYPos", "Center(y)base", vmdAnimController.centerBasePos.y, 15f);
					if (num4 != vmdAnimController.centerBasePos.y)
					{
						vmdAnimController.centerBasePos = new Vector3(0f, num4, 0f);
					}
					GUILayout.EndHorizontal();
					GUILayout.BeginHorizontal(new GUILayoutOption[0]);
					vmdAnimController.quickAdjust.Shoulder = this.AddSliderWithTextFixedScale("Shoulder Tilt", vmdAnimController.quickAdjust.Shoulder, -10f, 40f);
					GUILayout.EndHorizontal();
					GUILayout.BeginHorizontal(new GUILayoutOption[0]);
					vmdAnimController.quickAdjust.ArmUp = this.AddSliderWithTextFixedScale("Upper Arm Tilt", vmdAnimController.quickAdjust.ArmUp, -10f, 40f);
					GUILayout.EndHorizontal();
					GUILayout.BeginHorizontal(new GUILayoutOption[0]);
					vmdAnimController.quickAdjust.ArmLow = this.AddSliderWithTextFixedScale("Lower Arm Tilt", vmdAnimController.quickAdjust.ArmLow, -10f, 40f);
					GUILayout.EndHorizontal();
				}
				GUILayout.EndVertical();
			}
		}

		
		private void EnsureResourceLoaded()
		{
			if (this.m_fileImage == null)
			{
				this.m_fileImage = new Texture2D(32, 32, (TextureFormat)5, false);
				this.m_fileImage.LoadImage(VMDResources.file_icon);
				this.m_fileImage.Apply();
				this.m_directoryImage = new Texture2D(32, 32, (TextureFormat)5, false);
				this.m_directoryImage.LoadImage(VMDResources.folder_icon);
				this.m_directoryImage.Apply();
			}
		}

		
		protected void FileSelectedCallback(string path)
		{
			this.m_fileBrowser = null;
			this.lastFilename = path;
			if (!string.IsNullOrEmpty(path))
			{
				this.lastLoadedFilePath = path;
			}
		}

		
		protected void SoundFileSelectedCallback(string path)
		{
			this.m_fileBrowser = null;
			this.lastSoundFilename = path;
			if (!string.IsNullOrEmpty(path))
			{
				this.lastLoadedFilePath = path;
			}
		}

		
		private void DrawCameraArea()
		{
			GUILayout.BeginVertical(new GUILayoutOption[0]);
			if (this.lastCameraVMDFilename == null)
			{
				if (VMDCameraMgr.Instance.cameraVMDFilePath != null)
				{
					this.lastCameraVMDFilename = VMDCameraMgr.Instance.cameraVMDFilePath;
				}
				else
				{
					this.lastCameraVMDFilename = "";
				}
			}
			GUILayout.BeginHorizontal(new GUILayoutOption[0]);
			GUILayout.Label("Enabled", new GUILayoutOption[]
			{
				GUILayout.Width(80f)
			});
			if (GUILayout.Button(VMDCameraMgr.Instance.CameraEnabled ? "On" : "Off", new GUILayoutOption[]
			{
				GUILayout.Width(40f)
			}))
			{
				VMDCameraMgr.Instance.CameraEnabled = !VMDCameraMgr.Instance.CameraEnabled;
			}
			GUILayout.EndHorizontal();
			GUILayout.BeginHorizontal(new GUILayoutOption[0]);
			GUILayout.Label("VMD(Camera)", new GUILayoutOption[]
			{
				GUILayout.Width(80f)
			});
			if (GUILayout.Button("Load", new GUILayoutOption[]
			{
				GUILayout.Width(50f),
				GUILayout.Height(25f)
			}) && this.lastCameraVMDFilename != "")
			{
				VMDCameraMgr.Instance.SetCameraAnimation(this.lastCameraVMDFilename);
			}
			if (GUILayout.Button("Clear", new GUILayoutOption[]
			{
				GUILayout.Width(50f),
				GUILayout.Height(25f)
			}))
			{
				VMDCameraMgr.Instance.ClearClip();
				this.lastCameraVMDFilename = "";
			}
			this.lastCameraVMDFilename = GUILayout.TextField(this.lastCameraVMDFilename, new GUILayoutOption[]
			{
				GUILayout.Width(350f),
				GUILayout.Height(25f)
			});
			if (GUILayout.Button("...", new GUILayoutOption[]
			{
				GUILayout.Width(30f),
				GUILayout.Height(25f)
			}))
			{
				this.m_fileBrowser = new FileBrowser(new Rect((float)(Screen.width / 2 - 300), 200f, 600f, 500f), "Choose .vmd File", new FileBrowser.FinishedCallback(this.CameraVMDFileSelectedCallback));
				this.m_fileBrowser.SelectionPattern = "*.vmd";
				this.m_fileBrowser.DirectoryImage = this.m_directoryImage;
				this.m_fileBrowser.FileImage = this.m_fileImage;
				if (!string.IsNullOrEmpty(this.lastLoadedFilePath) && File.Exists(this.lastLoadedFilePath))
				{
					this.m_fileBrowser.CurrentDirectory = Path.GetDirectoryName(this.lastLoadedFilePath);
				}
			}
			GUILayout.EndHorizontal();
			GUILayout.BeginHorizontal(new GUILayoutOption[0]);
			GUILayout.Label("(Player)", new GUILayoutOption[]
			{
				GUILayout.Width(50f)
			});
			if (GUILayout.Button("Play", new GUILayoutOption[]
			{
				GUILayout.Width(50f),
				GUILayout.Height(25f)
			}))
			{
				VMDCameraMgr.Instance.Play();
			}
			if (GUILayout.Button("Pause", new GUILayoutOption[]
			{
				GUILayout.Width(50f),
				GUILayout.Height(25f)
			}))
			{
				VMDCameraMgr.Instance.Pause();
			}
			if (GUILayout.Button("Stop", new GUILayoutOption[]
			{
				GUILayout.Width(50f),
				GUILayout.Height(25f)
			}))
			{
				VMDCameraMgr.Instance.Stop();
			}
			GUILayout.Space(30f);
			GUILayout.Label("(All)", new GUILayoutOption[]
			{
				GUILayout.Width(50f)
			});
			if (GUILayout.Button("Play", new GUILayoutOption[]
			{
				GUILayout.Width(50f),
				GUILayout.Height(25f)
			}))
			{
				VMDAnimationMgr.Instance.PlayAll();
			}
			if (GUILayout.Button("Pause", new GUILayoutOption[]
			{
				GUILayout.Width(50f),
				GUILayout.Height(25f)
			}))
			{
				VMDAnimationMgr.Instance.PauseAll();
			}
			if (GUILayout.Button("Stop", new GUILayoutOption[]
			{
				GUILayout.Width(50f),
				GUILayout.Height(25f)
			}))
			{
				VMDAnimationMgr.Instance.StopAll();
			}
			GUILayout.EndHorizontal();
			GUILayout.BeginHorizontal(new GUILayoutOption[0]);
			VMDCameraMgr.Instance.Speed = this.AddSliderWithText("cameraVMDAnimSpeed", "Speed", VMDCameraMgr.Instance.Speed, 5f);
			GUILayout.EndHorizontal();
			GUILayout.BeginHorizontal(new GUILayoutOption[0]);
			GUILayout.Label("Loop", new GUILayoutOption[]
			{
				GUILayout.Width(40f)
			});
			if (GUILayout.Button(VMDCameraMgr.Instance.Loop ? "On" : "Off", new GUILayoutOption[]
			{
				GUILayout.Width(40f)
			}))
			{
				VMDCameraMgr.Instance.Loop = !VMDCameraMgr.Instance.Loop;
			}
			GUILayout.Space(20f);
			GUILayout.EndHorizontal();
			GUILayout.BeginHorizontal(new GUILayoutOption[0]);
			float num = this.AddSliderWithText("CameraModelScale", "Model Scale", VMDCameraMgr.Instance.modelScale, 2f);
			if (num != VMDCameraMgr.Instance.modelScale)
			{
				VMDCameraMgr.Instance.modelScale = num;
			}
			GUILayout.EndHorizontal();
			GUILayout.BeginHorizontal(new GUILayoutOption[0]);
			float num2 = this.AddSliderWithTextFixedScale("Camera(X)adjust", VMDCameraMgr.Instance.cameraPosAdjust.x, -2f, 2f);
			GUILayout.EndHorizontal();
			GUILayout.BeginHorizontal(new GUILayoutOption[0]);
			float num3 = this.AddSliderWithTextFixedScale("Camera(Y)adjust", VMDCameraMgr.Instance.cameraPosAdjust.y, -2f, 2f);
			GUILayout.EndHorizontal();
			GUILayout.BeginHorizontal(new GUILayoutOption[0]);
			float num4 = this.AddSliderWithTextFixedScale("Camera(Z)adjust", VMDCameraMgr.Instance.cameraPosAdjust.z, -2f, 2f);
			GUILayout.EndHorizontal();
			Vector3 vector;
            vector = new Vector3(num2, num3, num4);
			if (vector != VMDCameraMgr.Instance.cameraPosAdjust)
			{
				VMDCameraMgr.Instance.cameraPosAdjust = vector;
			}
			GUILayout.BeginHorizontal(new GUILayoutOption[0]);
			VMDCameraMgr.Instance.cameraDistanceAdjust = this.AddSliderWithTextFixedScale("Distance adjust", VMDCameraMgr.Instance.cameraDistanceAdjust, -2f, 5f);
			GUILayout.EndHorizontal();
			GUILayout.EndVertical();
		}

		
		protected void CameraVMDFileSelectedCallback(string path)
		{
			this.m_fileBrowser = null;
			this.lastCameraVMDFilename = path;
			if (!string.IsNullOrEmpty(path))
			{
				this.lastLoadedFilePath = path;
			}
		}

		
		private void DrawSoundArea()
		{
			GUILayout.BeginVertical(new GUILayoutOption[0]);
			if (this.lastCameraVMDFilename == null)
			{
				this.lastCameraVMDFilename = "";
			}
			GUILayout.BeginHorizontal(new GUILayoutOption[0]);
			GUILayout.Label("Sound", new GUILayoutOption[]
			{
				GUILayout.Width(40f)
			});
			if (GUILayout.Button("Load", new GUILayoutOption[]
			{
				GUILayout.Width(50f),
				GUILayout.Height(25f)
			}))
			{
				VMDAnimationMgr.Instance.SoundMgr.SetSoundClip(this.lastSoundFilename);
			}
			if (GUILayout.Button("Clear", new GUILayoutOption[]
			{
				GUILayout.Width(50f),
				GUILayout.Height(25f)
			}))
			{
				VMDAnimationMgr.Instance.SoundMgr.ClearClip();
				this.lastSoundFilename = "";
			}
			this.lastSoundFilename = GUILayout.TextField(this.lastSoundFilename, new GUILayoutOption[]
			{
				GUILayout.Width(350f),
				GUILayout.Height(25f)
			});
			if (GUILayout.Button("...", new GUILayoutOption[]
			{
				GUILayout.Width(30f),
				GUILayout.Height(25f)
			}))
			{
				this.m_fileBrowser = new FileBrowser(new Rect((float)(Screen.width / 2 - 300), 200f, 600f, 500f), "Choose .wav or .ogg File", new FileBrowser.FinishedCallback(this.SoundFileSelectedCallback));
				this.m_fileBrowser.SelectionPattern = "*.wav;*.ogg";
				this.m_fileBrowser.DirectoryImage = this.m_directoryImage;
				this.m_fileBrowser.FileImage = this.m_fileImage;
				if (!string.IsNullOrEmpty(this.lastLoadedFilePath) && File.Exists(this.lastLoadedFilePath))
				{
					this.m_fileBrowser.CurrentDirectory = Path.GetDirectoryName(this.lastLoadedFilePath);
				}
			}
			GUILayout.EndHorizontal();
			GUILayout.BeginHorizontal(new GUILayoutOption[0]);
			GUILayout.Label("(Player)", new GUILayoutOption[]
			{
				GUILayout.Width(50f)
			});
			if (GUILayout.Button("Play", new GUILayoutOption[]
			{
				GUILayout.Width(50f),
				GUILayout.Height(25f)
			}))
			{
				VMDAnimationMgr.Instance.SoundMgr.PlaySound();
			}
			if (GUILayout.Button("Pause", new GUILayoutOption[]
			{
				GUILayout.Width(50f),
				GUILayout.Height(25f)
			}))
			{
				VMDAnimationMgr.Instance.SoundMgr.PauseSound();
			}
			if (GUILayout.Button("Stop", new GUILayoutOption[]
			{
				GUILayout.Width(50f),
				GUILayout.Height(25f)
			}))
			{
				VMDAnimationMgr.Instance.SoundMgr.StopSound();
			}
			GUILayout.Space(30f);
			GUILayout.Label("(All)", new GUILayoutOption[]
			{
				GUILayout.Width(50f)
			});
			if (GUILayout.Button("Play", new GUILayoutOption[]
			{
				GUILayout.Width(50f),
				GUILayout.Height(25f)
			}))
			{
				VMDAnimationMgr.Instance.PlayAll();
			}
			if (GUILayout.Button("Pause", new GUILayoutOption[]
			{
				GUILayout.Width(50f),
				GUILayout.Height(25f)
			}))
			{
				VMDAnimationMgr.Instance.PauseAll();
			}
			if (GUILayout.Button("Stop", new GUILayoutOption[]
			{
				GUILayout.Width(50f),
				GUILayout.Height(25f)
			}))
			{
				VMDAnimationMgr.Instance.StopAll();
			}
			GUILayout.EndHorizontal();
			GUILayout.BeginHorizontal(new GUILayoutOption[0]);
			VMDAnimationMgr.Instance.SoundMgr.Speed = this.AddSliderWithText("vmdSoundSpeed", "Speed", VMDAnimationMgr.Instance.SoundMgr.Speed, 5f);
			GUILayout.EndHorizontal();
			GUILayout.BeginHorizontal(new GUILayoutOption[0]);
			GUILayout.Label("Loop", new GUILayoutOption[]
			{
				GUILayout.Width(40f)
			});
			if (GUILayout.Button(VMDAnimationMgr.Instance.SoundMgr.Loop ? "On" : "Off", new GUILayoutOption[]
			{
				GUILayout.Width(40f)
			}))
			{
				VMDAnimationMgr.Instance.SoundMgr.Loop = !VMDAnimationMgr.Instance.SoundMgr.Loop;
			}
			GUILayout.EndHorizontal();
			GUILayout.EndVertical();
		}

		
		public float AddSliderGeneral(string prop, string label, float value, float defaultMin, float defaultMax, bool fixedScale, bool useText)
		{
			GUILayout.Label(label, new GUILayoutOption[]
			{
				GUILayout.Width(this.sliderLabelWidth)
			});
			GUILayout.Space(5f);
			float num;
			float num2;
			if (fixedScale)
			{
				num = defaultMin;
				num2 = defaultMax;
			}
			else
			{
				num = defaultMin;
				num2 = this.GetSliderMax(prop, defaultMax);
			}
			float result = GUILayout.HorizontalSlider(value, num, num2, new GUILayoutOption[]
			{
				GUILayout.Width(this.sliderWidth)
			});
			GUILayout.Space(5f);
			if (useText)
			{
				string text = value.ToString("F4");
				string text2 = GUILayout.TextField(text, new GUILayoutOption[]
				{
					GUILayout.Width(this.valueLabelWidth)
				});
				if (!(text2 != text))
				{
					goto IL_CD;
				}
				try
				{
					result = float.Parse(text2);
					goto IL_CD;
				}
				catch (Exception)
				{
					goto IL_CD;
				}
			}
			GUILayout.Label(value.ToString("F4"), new GUILayoutOption[]
			{
				GUILayout.Width(this.valueLabelWidth)
			});
			IL_CD:
			GUILayout.Space(5f);
			if (!fixedScale)
			{
				if (GUILayout.Button("0-1", new GUILayoutOption[]
				{
					GUILayout.Width(50f),
					GUILayout.Height(25f)
				}))
				{
					this.SetSliderMax(prop, 1f);
				}
				if (GUILayout.Button("0-10", new GUILayoutOption[]
				{
					GUILayout.Width(50f),
					GUILayout.Height(25f)
				}))
				{
					this.SetSliderMax(prop, 10f);
				}
				if (GUILayout.Button("x2", new GUILayoutOption[]
				{
					GUILayout.Width(50f),
					GUILayout.Height(25f)
				}))
				{
					this.SetSliderMax(prop, this.GetSliderMax(prop, 1f) * 2f);
				}
			}
			return result;
		}

		
		public float AddSliderWithLabel(string prop, string label, float value, float defaultMax)
		{
			return this.AddSliderGeneral(prop, label, value, 0f, defaultMax, false, false);
		}

		
		public float AddSliderWithText(string prop, string label, float value, float defaultMax)
		{
			return this.AddSliderGeneral(prop, label, value, 0f, defaultMax, false, true);
		}

		
		public float AddSliderWithLabelFixedScale(string label, float value, float min, float max)
		{
			return this.AddSliderGeneral("", label, value, min, max, true, false);
		}

		
		public float AddSliderWithTextFixedScale(string label, float value, float min, float max)
		{
			return this.AddSliderGeneral("", label, value, min, max, true, true);
		}

		
		public float GetSliderMax(string key, float defaultMax)
		{
			if (this.sliderMax.ContainsKey(key))
			{
				return this.sliderMax[key];
			}
			return defaultMax;
		}

		
		public void SetSliderMax(string key, float value)
		{
			this.sliderMax[key] = value;
		}

		
		private void ProcessControllerGUI()
		{
			if (this.controlAreaStartX == -1f)
			{
				this.controlAreaStartX = this.CalcControlAreaStartX();
			}
			this.controllerRect = new Rect(this.controlAreaStartX, (float)Screen.height - this.controlAreaHeight, (float)Screen.width - this.controlAreaStartX, this.controlAreaHeight);
			this.controllerRectInScreenspace = new Rect(this.controlAreaStartX, 0f, (float)Screen.width - this.controlAreaStartX, this.controlAreaHeight);
			if (!this.visibleGUI && this.autoShowHideController)
			{
				Vector2 vector = Input.mousePosition;
				if (!this.controllerRectInScreenspace.Contains(vector))
				{
					this.hideController = true;
				}
				else
				{
					this.hideController = false;
				}
			}
			if (this.visibleControllerGUI && !this.hideController)
			{
				GUI.Box(this.controllerRect, "");
				GUILayout.BeginArea(this.controllerRect);
				this.FuncControllerWindowGUI();
				GUILayout.EndArea();
			}
		}

		
		private float CalcControlAreaStartX()
		{
			Transform transform = Singleton<Studio.Studio>.Instance.gameObject.transform.Find("Canvas Guide Input/Guide Input/Button Scale");
			if (transform != null)
			{
				return transform.position.x + transform.position.y + 10f;
			}
			return 0f;
		}

		
		private void FuncControllerWindowGUI()
		{
			this.styleBackup = new Dictionary<string, GUIStyle>();
			this.BackupGUIStyle("Button");
			this.BackupGUIStyle("Label");
			this.BackupGUIStyle("Toggle");
			try
			{
				if (GUIUtility.hotControl == 0)
				{
					this.cameraCtrl.enabled = false;
				}
				Vector2 vector = Input.mousePosition;
				if (this.controllerRectInScreenspace.Contains(vector))
				{
					this.cameraCtrl.enabled = true;
					this.cameraCtrl.cameraCtrlOff = true;
				}
				GUI.enabled = true;
				GUI.skin.GetStyle("Button").alignment = (TextAnchor)4;
				GUIStyle style = GUI.skin.GetStyle("Label");
				style.alignment = (TextAnchor)3;
				style.wordWrap = false;
				GUI.skin.GetStyle("Toggle").onNormal.textColor = Color.white;
				GUILayout.BeginVertical(new GUILayoutOption[0]);
				float animationLength = VMDAnimationMgr.Instance.AnimationLength;
				float animationPosition = VMDAnimationMgr.Instance.AnimationPosition;
				int num = (int)(animationPosition * 30f);
				GUILayout.BeginHorizontal(new GUILayoutOption[0]);
				GUILayout.FlexibleSpace();
				GUILayout.Label("VMD Play ", new GUILayoutOption[0]);
				GUILayout.Space(10f);
				if (GUILayout.Button((this.visibleGUI ? "Close" : "Show") + " Config", new GUILayoutOption[0]))
				{
					this.ToggleConfigGUI();
				}
				GUILayout.Space(10f);
				TimeSpan timeSpan = new TimeSpan((long)animationPosition * 10000000L);
				TimeSpan timeSpan2 = new TimeSpan((long)animationLength * 10000000L);
				GUIStyle guistyle = new GUIStyle(style);
				guistyle.alignment = (TextAnchor)4;
				GUILayout.Label(string.Format("{0:hh:mm:ss} of {1:hh:mm:ss}  (frame:{2}) ", timeSpan, timeSpan2, num), guistyle, new GUILayoutOption[]
				{
					GUILayout.Width(220f)
				});
				if (GUILayout.Button("Play", new GUILayoutOption[]
				{
					GUILayout.Width(50f)
				}))
				{
					VMDAnimationMgr.Instance.PlayAll();
				}
				GUILayout.Space(5f);
				if (GUILayout.Button("Pause", new GUILayoutOption[]
				{
					GUILayout.Width(50f)
				}))
				{
					VMDAnimationMgr.Instance.PauseAll();
				}
				GUILayout.Space(5f);
				if (GUILayout.Button("Stop", new GUILayoutOption[]
				{
					GUILayout.Width(50f)
				}))
				{
					VMDAnimationMgr.Instance.StopAll();
				}
				GUILayout.FlexibleSpace();
				bool flag = GUILayout.Toggle(this.autoShowHideController, "Auto Hide", new GUILayoutOption[0]);
				if (this.autoShowHideController != flag)
				{
					this.autoShowHideController = flag;
                    KKVMDPlugin.settingControllerAutoHide.Value = this.autoShowHideController;
                    //Settings.Instance.SetBoolValue("ControllerAutoHide", this.autoShowHideController);
				}
				if (GUILayout.Button("Close", new GUILayoutOption[0]))
				{
					this.ShowControllerGUI(false);
				}
				GUILayout.EndHorizontal();
				GUILayout.BeginHorizontal(new GUILayoutOption[0]);
				GUILayout.FlexibleSpace();
				float num2 = GUILayout.HorizontalSlider(animationPosition, 0f, animationLength, new GUILayoutOption[]
				{
					GUILayout.Width(this.controllerRect.width - 50f)
				});
				if (Mathf.Abs(animationPosition - num2) > 0.01f)
				{
					VMDAnimationMgr.Instance.AnimationPosition = num2;
				}
				GUILayout.FlexibleSpace();
				GUILayout.EndHorizontal();
				GUILayout.EndVertical();
			}
			catch (Exception value)
			{
				Console.WriteLine(value);
			}
			finally
			{
				this.RestoreGUIStyle("Button");
				this.RestoreGUIStyle("Label");
				this.RestoreGUIStyle("Toggle");
			}
		}

        public void SettingControllerVisible_SettingChanged(object sender, EventArgs e)
        {
            this.ToggleConrolerGUI();
        }
		
		public void ToggleConfigGUI()
		{
            this.visibleGUI = !this.visibleGUI;
		}

		
		public void ToggleConrolerGUI()
		{
			this.ShowControllerGUI(!this.visibleControllerGUI);

        }

		
		public void RestoreControllerGUIShow()
		{
			this.visibleControllerGUI = KKVMDPlugin.settingControllerVisible.Value;
                //Settings.Instance.GetBoolValue("ControllerVisble", false, true);
			if (this.visibleControllerGUI)
			{
				this.visibleGUI = KKVMDPlugin.settingConfigVisible.Value;
			}
		}

		
		public void ShowControllerGUI(bool show)
		{
			//Settings.Instance.SetBoolValue("ControllerVisble", show);
            KKVMDPlugin.settingControllerVisible.Value = show;
			this.visibleControllerGUI = show;
			if (show)
			{
				this.hideController = false;
			}
			this.visibleGUI = false;
		}

		
		public ChaControl focusChara;

		
		private CameraCtrlOff cameraCtrl;

		
		private int windowID = 8723;

		
		private int dialogWindowID = 8724;

		
		private Rect windowRect = new Rect(0f, 300f, 630f, 500f);

		
		private string windowTitle = "Koikatu VMDPlay Plugin";

		
		private Texture2D windowBG = new Texture2D(1, 1, (TextureFormat)5, false);

		
		public bool visibleGUI = KKVMDPlugin.settingConfigVisible.Value;

		
		private bool isVR;

		
		private int currentTab;

		
		private float sliderLabelWidth = 100f;

		
		private float sliderWidth = 240f;

		
		private float valueLabelWidth = 70f;

		
		public bool pinObject;

		
		public Dictionary<string, Action> AdditionalMenus = new Dictionary<string, Action>();

		
		private GUISkin guiSkin;

		
		private GUISkin vrGUISkin;

		
		private static MethodInfo m_Apply = typeof(GUISkin).GetMethod("Apply", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.InvokeMethod);

		
		private Dictionary<string, GUIStyle> styleBackup = new Dictionary<string, GUIStyle>();

		
		protected FileBrowser m_fileBrowser;

		
		protected Texture2D m_directoryImage;

		
		protected Texture2D m_fileImage;

		
		public string lastLoadedFilePath;

		
		private VMDAnimationController lastController;

		
		public string lastFilename;

		
		public string lastSoundFilename;

		
		private Vector2 vmdAreaScrollPos;

		
		public string lastCameraVMDFilename;

		
		private Dictionary<string, float> sliderMax = new Dictionary<string, float>();

		
		public bool visibleControllerGUI = true;

		
		public bool hideController;

		
		public float controlAreaStartX = -1f;

		
		public float controlAreaHeight = 40f;

		
		private Rect controllerRect;

		
		private Rect controllerRectInScreenspace;

		
		public bool autoShowHideController;

		
		private const string SETTINGS_KEY_AUTO_HIDE = "ControllerAutoHide";

		
		private const string SETTINGS_KEY_CONTROLLER_OPEN = "ControllerVisble";
	}
}
