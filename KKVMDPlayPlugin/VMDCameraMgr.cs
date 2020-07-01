using BepInEx.Logging;
using MMD.VMD;
using Studio;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace KKVMDPlayPlugin
{
    
    public class VMDCameraMgr : MonoBehaviour
	{
		
		public bool CameraEnabled
		{
			get
			{
				return this.cameraEnabled;
			}
			set
			{
				this.cameraEnabled = value;
			}
		}

		public float Speed
		{
			get
			{
				return this._speed;
			}
			set
			{
				if (this._speed != value)
				{
					this._speed = value;
					this.ChangeSpeed(value);
				}
			}
		}


		public static VMDCameraMgr Instance
		{
			get
			{
				return VMDCameraMgr._instance;
			}
		}


		public bool Loop
		{
			get
			{
				return this._loop;
			}
			set
			{
				if (value)
				{
					this._loop = true;
					this.cameraAnimation.wrapMode = (WrapMode)2;
					return;
				}
				this._loop = false;
				this.cameraAnimation.wrapMode = (WrapMode)1;
			}
		}

		
		public static VMDCameraMgr Install(GameObject root)
		{
			VMDCameraMgr vmdcameraMgr = root.GetComponent<VMDCameraMgr>();
			if (vmdcameraMgr == null)
			{
				vmdcameraMgr = root.AddComponent<VMDCameraMgr>();
				vmdcameraMgr.Init();
				VMDCameraMgr._instance = vmdcameraMgr;
			}
			return vmdcameraMgr;
		}

		
		private void Init()
		{
			this.cameraAnimationRoot = base.gameObject;
			this.cameraAnimation = base.gameObject.AddComponent<Animation>();
			this.t_camera = this.addDummy("camera");
			this.t_length = this.addDummy("length");
			this.t_view_angle = this.addDummy("view_angle");
			this.t_perspective = this.addDummy("perspective");
			this.cameraControl = UnityEngine.Object.FindObjectOfType<Studio.CameraControl>();
		}

		
		private Transform addDummy(string name)
		{
			Transform transform = new GameObject(name).transform;
			transform.parent = base.transform;
			transform.localPosition = Vector3.zero;
			transform.localRotation = Quaternion.identity;
			return transform;
		}

		
		private void ResetDummy()
		{
			this.t_camera.localPosition = Vector3.zero;
			this.t_camera.localRotation = Quaternion.identity;
			this.t_length.localPosition = Vector3.zero;
			this.t_view_angle.localPosition = Vector3.zero;
			this.t_perspective.localPosition = Vector3.zero;
		}

		public string cameraVMDFilePath
		{
			get
			{
				return this._cameraVMDFilePath;
			}
		}

		
		public void ClearClip()
		{
			this.cameraAnimation.Stop();
			this.cameraAnimation.RemoveClip("VMDCameraAnim");
			this._cameraVMDFilePath = "";
		}

		
		public bool SetCameraAnimation(string path)
		{
			Dictionary<string, BoneAdjustment> dictionary = new Dictionary<string, BoneAdjustment>();
			dictionary.Add("camera", BoneAdjustment.Init("VMDCameraAnim", this.boneAdjustSpec, Vector3.zero, false));
            KKVMDPlugin.Logger.Log(BepInEx.Logging.LogLevel.Debug, string.Format("bone adjustment init", path));
			if (!File.Exists(path))
			{
                KKVMDPlugin.Logger.Log(BepInEx.Logging.LogLevel.Debug, string.Format("File not found. {0} ", path));
				return false;
			}
			this.Stop();
			bool result;
			using (BinaryReader binaryReader = new BinaryReader(File.OpenRead(path)))
			{
				VMDFormat format = VMDLoader.Load(binaryReader, path, "VMDCameraAnim");
                KKVMDPlugin.Logger.Log(BepInEx.Logging.LogLevel.Debug, string.Format("VMD file loaded {0}:", path));
				this._cameraVMDFilePath = path;
				AnimationClip animationClip = new VMDHSConverter(dictionary, this._scaleBase * this.modelScale).CreateCameraAnimationClip(format, base.gameObject, 4);
                KKVMDPlugin.Logger.Log(BepInEx.Logging.LogLevel.Debug, string.Format("Converted as an animation clip", animationClip));
                KKVMDPlugin.Logger.Log(BepInEx.Logging.LogLevel.Debug, string.Format("Set to animation ", this.cameraAnimation));
				animationClip.name = "VMDCameraAnim";
				this.cameraAnimation.AddClip(animationClip, "VMDCameraAnim");
				this.cameraAnimation.clip = animationClip;
				AnimationState animationState = this.cameraAnimation["VMDCameraAnim"];
                KKVMDPlugin.Logger.Log(BepInEx.Logging.LogLevel.Debug, string.Format("Found animation info {0}", animationState));
				animationState.speed = this._speed;
				animationState.wrapMode = (this._loop ? (WrapMode)2 : (WrapMode)1);
				result = true;
			}
			return result;
		}

		
		private void ChangeSpeed(float value)
		{
			if (this.cameraAnimation.GetClipCount() > 0)
			{
				this.cameraAnimation["VMDCameraAnim"].speed = value;
			}
		}

		
		public void SetAnimePosition(float time)
		{
			if (this.cameraAnimation.GetClipCount() > 0)
			{
				this.cameraAnimation["VMDCameraAnim"].time = time;
			}
		}

		
		public void Play()
		{
			if (this.cameraAnimation.GetClipCount() > 0)
			{
				if (this.cameraAnimation.IsPlaying("VMDCameraAnim"))
				{
					this.cameraAnimation["VMDCameraAnim"].speed = this._speed;
					return;
				}
				this.cameraAnimation["VMDCameraAnim"].speed = this._speed;
				this.cameraAnimation.Play("VMDCameraAnim");
			}
		}

		
		public void Pause()
		{
			if (this.cameraAnimation.IsPlaying("VMDCameraAnim"))
			{
				this.cameraAnimation["VMDCameraAnim"].speed = 0f;
			}
		}

		
		public void Stop()
		{
			this.cameraAnimation.Stop();
		}


		public float AnimeLength
		{
			get
			{
				if (this.cameraAnimation.GetClipCount() > 0 && this.cameraAnimation["VMDCameraAnim"] != null)
				{
					return this.cameraAnimation["VMDCameraAnim"].length;
				}
				return 0f;
			}
		}


		public float AnimePosition
		{
			get
			{
				if (this.cameraAnimation.GetClipCount() > 0 && this.cameraAnimation["VMDCameraAnim"] != null)
				{
					return this.cameraAnimation["VMDCameraAnim"].time;
				}
				return 0f;
			}
			set
			{
				if (this.cameraAnimation.GetClipCount() > 0 && this.cameraAnimation["VMDCameraAnim"] != null)
				{
					if (value < 0f)
					{
						value = 0f;
					}
					if (this.cameraAnimation["VMDCameraAnim"].length >= value)
					{
						this.cameraAnimation["VMDCameraAnim"].time = value;
					}
					else
					{
						this.cameraAnimation["VMDCameraAnim"].time = this.cameraAnimation["VMDCameraAnim"].length;
					}
					this.UpdateCameraPos();
				}
			}
		}

		
		private void LateUpdate()
		{
			if (this.cameraEnabled && this.cameraAnimation.GetClipCount() > 0 && this.cameraAnimation.IsPlaying("VMDCameraAnim") && this.cameraAnimation["VMDCameraAnim"].speed > 0f)
			{
				try
				{
					this.UpdateCameraPos();
				}
				catch (Exception ex)
				{
                    KKVMDPlugin.Logger.Log(BepInEx.Logging.LogLevel.Debug, ex);
				}
			}
		}

		
		private void UpdateCameraPos()
		{
            if (this.cameraControl == null)
            {
                this.cameraControl = UnityEngine.Object.FindObjectOfType<Studio.CameraControl>();
            }
            Quaternion quaternion = Quaternion.Euler(0f, 180f, 0f) * this.t_camera.localRotation;
            Vector3 localPosition = this.t_length.localPosition;
            localPosition.z -= this.cameraDistanceAdjust;
            Studio.CameraControl.CameraData cameraData = new Studio.CameraControl.CameraData();
            Vector3 vector = this.t_camera.localPosition * this.modelScale + this.cameraPosAdjust;
            cameraData.pos = vector;
            cameraData.distance = localPosition;
            cameraData.rotate = quaternion.eulerAngles;
            cameraData.parse = this.t_view_angle.localPosition.z * -1f;
            this.cameraControl.Import(cameraData);
            this.cameraControl.transform.rotation = quaternion;
            this.cameraControl.transform.position = quaternion * localPosition + vector;
            this.cameraControl.fieldOfView = this.t_view_angle.localPosition.z;
        }

		
		private bool cameraEnabled = true;

		
		private string _cameraVMDFilePath;

		
		private GameObject cameraAnimationRoot;

		
		public Animation cameraAnimation;

		
		private float _scaleBase = 0.095f;

		
		public float modelScale = 1f;

		
		private float _speed = 1f;

		
		private bool _loop;

		
		public Vector3 cameraPosAdjust = new Vector3(0f, 0f, 0f);

		
		public float cameraDistanceAdjust;

		
		public const string CLIP_NAME = "VMDCameraAnim";

		
		public string boneAdjustSpec = "-x,y,-z";

		
		public bool useCameraBasePos;

		
		private static VMDCameraMgr _instance;

		
		private Transform t_camera;

		
		private Transform t_length;

		
		private Transform t_view_angle;

		
		private Transform t_perspective;

		
		private Studio.CameraControl cameraControl;
	}
}
