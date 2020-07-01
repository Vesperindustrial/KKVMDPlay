using System;
using System.Collections.Generic;
using UnityEngine;
using BepInEx;

namespace KKVMDPlayPlugin
{
    
    public class VMDAnimationMgr : MonoBehaviour
	{
		
		
		
		public bool SyncAllAnimeAndSound
		{
			get
			{
				return this._syncAllAnimeAndSound;
			}
			set
			{
				if (this._syncAllAnimeAndSound != value)
				{
					this._syncAllAnimeAndSound = value;
				}
			}
		}

		
		private void Start()
		{
			//string stringValue = Settings.Instance.GetStringValue("UIKey", "Ctrl+Shift+V", true);
            string stringValue = KKVMDPlugin.settingUIKey.Value;
			this.UIKey = KeyUtil.Parse(stringValue);
		}

		
		public static VMDAnimationMgr Install(GameObject container)
		{
			if (VMDAnimationMgr._instance == null)
			{
				VMDAnimationMgr._instance = container.AddComponent<VMDAnimationMgr>();
                GameObject gameObject = new GameObject("CameraMgr");
                gameObject.transform.parent = container.transform;
                gameObject.transform.localPosition = Vector3.zero;
                gameObject.transform.localRotation = Quaternion.identity;
                VMDAnimationMgr._instance.CameraMgr = VMDCameraMgr.Install(gameObject);
            }
			return VMDAnimationMgr._instance;
		}

		
		
		public static VMDAnimationMgr Instance
		{
			get
			{
				return VMDAnimationMgr._instance;
			}
		}

		
		public void PlayAll()
		{
			foreach (VMDAnimationController vmdanimationController in this.controllers)
			{
				vmdanimationController.Play();
			}
			this.SoundMgr.PlaySound();
			this.CameraMgr.Play();
		}

		
		public void StopAll()
		{
			foreach (VMDAnimationController vmdanimationController in this.controllers)
			{
				vmdanimationController.Stop();
			}
			this.SoundMgr.StopSound();
			this.CameraMgr.Stop();
		}

		
		public void PauseAll()
		{
			foreach (VMDAnimationController vmdanimationController in this.controllers)
			{
				vmdanimationController.Pause();
			}
			this.SoundMgr.PauseSound();
			this.CameraMgr.Pause();
		}

		
		private void OnLevelWasLoaded(int level)
		{
			if (this.gui == null)
			{
				this.gui = new GameObject("GUI").AddComponent<KKVMDGUI>();
				this.gui.transform.parent = base.transform;
				this.gui.visibleGUI = false;
				this.gui.RestoreControllerGUIShow();
				return;
			}
			this.gui.visibleGUI = false;
			this.gui.visibleControllerGUI = false;
			this.gui.focusChara = null;
		}

		
		
		
		public float AnimationPosition
		{
			get
			{
				AnimationState referenceAnimationState = this.GetReferenceAnimationState();
				if (referenceAnimationState != null)
				{
					return referenceAnimationState.time;
				}
				return 0f;
			}
			set
			{
				AnimationState referenceAnimationState = this.GetReferenceAnimationState();
				if (referenceAnimationState != null && referenceAnimationState.time != value)
				{
					referenceAnimationState.time = value;
					this.DoSyncAllAnimeAndSound();
				}
			}
		}

		
		
		public float AnimationLength
		{
			get
			{
				AnimationState referenceAnimationState = this.GetReferenceAnimationState();
				if (referenceAnimationState != null)
				{
					return referenceAnimationState.length;
				}
				return 0f;
			}
		}

		
		private AnimationState GetReferenceAnimationState()
		{
			foreach (VMDAnimationController vmdanimationController in this.controllers)
			{
				if (vmdanimationController.gameObject != null && vmdanimationController.VMDAnimEnabled && vmdanimationController.animationForVMD != null && vmdanimationController.animationForVMD["VMDAnim"] != null)
				{
					return vmdanimationController.animationForVMD["VMDAnim"];
				}
			}
			if (this.CameraMgr.CameraEnabled && this.CameraMgr.cameraAnimation != null && this.CameraMgr.cameraAnimation["VMDCameraAnim"] != null)
			{
				return this.CameraMgr.cameraAnimation["VMDCameraAnim"];
			}
			return null;
		}

		
		private void DoSyncAllAnimeAndSound()
		{
			AnimationState referenceAnimationState = this.GetReferenceAnimationState();
			if (referenceAnimationState != null)
			{
				float time = referenceAnimationState.time;
				foreach (VMDAnimationController vmdanimationController in this.controllers)
				{
					if (vmdanimationController.gameObject != null && vmdanimationController.VMDAnimEnabled && vmdanimationController.IsVMDAnimeActive())
					{
						vmdanimationController.SetAnimPosition(time);
					}
				}
				if (this.CameraMgr.CameraEnabled && this.CameraMgr.cameraAnimation != null && this.CameraMgr.cameraAnimation["VMDCameraAnim"] != null)
				{
					this.CameraMgr.AnimePosition = time;
				}
				if (this.SoundMgr.currentAudioClip != null)
				{
					this.SoundMgr.AnimePosition = time;
				}
			}
		}

		
		private void Update()
		{
			try
			{
				if (this.UIKey.TestKeyDown())
				{
					this.ToggleGUI();
				}
			}
			catch (Exception value)
			{
				Console.WriteLine(value);
			}
		}

		
		public void ToggleGUI()
		{
			this.gui.ToggleConrolerGUI();
			if (this.gui.visibleControllerGUI)
			{
				this.gui.visibleGUI = this.gui.visibleControllerGUI;
			}
		}

		
		private static VMDAnimationMgr _instance;

		
		public KKVMDGUI gui;

		
		private KeyUtil UIKey;

		
		public CustomSoundMgr SoundMgr = new CustomSoundMgr();

		
		public VMDCameraMgr CameraMgr;

		
		private bool _syncAllAnimeAndSound;

		
		public List<VMDAnimationController> controllers = new List<VMDAnimationController>();
	}
}
