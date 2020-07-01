using System;
using Studio;
using UnityEngine;

namespace KKVMDPlayPlugin
{
	
	public class CameraCtrlOff : MonoBehaviour
	{
		
		public bool cameraCtrlOff
		{
			get
			{
				return this._cameraCtrlOff;
			}
			set
			{
				try
				{
					if (this.cameraControllerFunc != null)
					{
						this.cameraControllerFunc(!value);
					}
					this._cameraCtrlOff = value;
				}
				catch
				{
				}
			}
		}

		
		private void Start()
		{
		}

		
		private void OnLevelWasInitialized(int level)
		{
			this.cameraControllerFunc = null;
			if (!this.CameraControllerInit())
			{
				this.disableControl = true;
				return;
			}
			this.disableControl = false;
		}

		
		private void Update()
		{
			if (this.disableControl)
			{
				return;
			}
			if (this.cameraControllerFunc == null)
			{
				this.CameraControllerInit();
			}
			if (this.ikInfoGui != null)
			{
				int hotControl = GUIUtility.hotControl;
				if (this.ikInfoGui.visibleGUI || this.tempCamCtrlOn || (this.ikInfoGui.visibleControllerGUI && !this.ikInfoGui.hideController))
				{
					if (Input.GetKey((KeyCode)308) || Input.GetKey((KeyCode)307))
					{
						if (this.cameraCtrlOff)
						{
							this.cameraCtrlOff = false;
							this.tempCamCtrlOn = true;
							this.ikInfoGui.visibleGUI = false;
							this.ikInfoGui.visibleControllerGUI = false;
							return;
						}
					}
					else if (!this.cameraCtrlOff)
					{
						this.cameraCtrlOff = true;
						this.tempCamCtrlOn = false;
						this.ikInfoGui.visibleGUI = true;
						this.ikInfoGui.visibleControllerGUI = true;
						return;
					}
				}
				else if (this.cameraCtrlOff)
				{
					this.cameraCtrlOff = false;
					this.tempCamCtrlOn = false;
				}
			}
		}

		
		private void OnEnable()
		{
			this.cameraCtrlOff = true;
		}

		
		private void OnDisable()
		{
			this.cameraCtrlOff = false;
		}

		
		private bool CameraControllerInit()
		{
			bool result = false;
			try
			{
				Console.WriteLine("Install Camera Control");
				Studio.CameraControl cameraControl = UnityEngine.Object.FindObjectOfType<Studio.CameraControl>();
				if (cameraControl == null)
				{
					Console.WriteLine("camera contoller not found");
					return false;
				}
				this.cameraControllerFunc = delegate(bool enable)
				{
					try
					{
						cameraControl.enabled = enable;
					}
					catch
					{
					}
				};
				result = true;
			}
			catch
			{
				Console.WriteLine("exception : camera contoller setting failed");
			}
			return result;
		}

		
		private CameraCtrlOff.SetCameraDelegate cameraControllerFunc;

		
		public KKVMDGUI ikInfoGui;

		
		private bool tempCamCtrlOn;

		
		private bool _cameraCtrlOff = true;

		
		private bool disableControl;

		
		
		public delegate void SetCameraDelegate(bool enable);
	}
}
