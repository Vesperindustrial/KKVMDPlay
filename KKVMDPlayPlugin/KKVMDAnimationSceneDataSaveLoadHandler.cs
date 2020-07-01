using System;
using System.Xml;
using UnityEngine;

namespace KKVMDPlayPlugin
{
	
	public class KKVMDAnimationSceneDataSaveLoadHandler : SaveLoadBase
	{
		
		public void OnLoad(XmlElement rootElement)
		{
			Console.WriteLine("Load VMDSceneData from XML.");
			XmlNodeList elementsByTagName = rootElement.GetElementsByTagName("VMDSceneData");
			if (elementsByTagName.Count == 1)
			{
				XmlElement vmdSceneDataInfo = elementsByTagName.Item(0) as XmlElement;
				this.LoadVMDSceneDataInfo(vmdSceneDataInfo);
			}
		}

		
		public void LoadVMDSceneDataInfo(XmlElement vmdSceneDataInfo)
		{
			VMDAnimationMgr instance = VMDAnimationMgr.Instance;
			instance.SoundMgr.Loop = base.GetAttr(vmdSceneDataInfo, "Sound.Loop", false);
			string attr = base.GetAttr(vmdSceneDataInfo, "Sound.Filename", "");
			if (!string.IsNullOrEmpty(attr))
			{
				instance.SoundMgr.SetSoundClip(attr);
			}
			else
			{
				instance.SoundMgr.ClearClip();
			}
			instance.gui.lastSoundFilename = instance.SoundMgr.audioFilePath;
			instance.CameraMgr.Loop = base.GetAttr(vmdSceneDataInfo, "Camera.Loop", false);
			instance.CameraMgr.Speed = base.GetAttr(vmdSceneDataInfo, "Camera.Speed", 1f);
			instance.CameraMgr.modelScale = base.GetAttr(vmdSceneDataInfo, "Camera.ModelScale", 1f);
			instance.CameraMgr.cameraPosAdjust = base.GetAttrVec3(vmdSceneDataInfo, "Camera.PosAdjust", Vector3.zero);
			instance.CameraMgr.cameraDistanceAdjust = base.GetAttr(vmdSceneDataInfo, "Camera.DistanceAdjust", 0f);
			string attr2 = base.GetAttr(vmdSceneDataInfo, "Camera.VMDFilename", "");
			if (!string.IsNullOrEmpty(attr2))
			{
				instance.CameraMgr.SetCameraAnimation(attr2);
			}
			else
			{
				instance.CameraMgr.ClearClip();
			}
			instance.gui.lastCameraVMDFilename = instance.CameraMgr.cameraVMDFilePath;
		}

		
		public void OnSave(XmlElement rootElement)
		{
			XmlElement xmlElement = rootElement.OwnerDocument.CreateElement("VMDSceneData");
			xmlElement.SetAttribute("Version", "1");
			VMDAnimationMgr instance = VMDAnimationMgr.Instance;
			base.SetAttr(xmlElement, "Sound.Loop", instance.SoundMgr.Loop);
			base.SetAttr(xmlElement, "Sound.Filename", instance.SoundMgr.audioFilePath);
			base.SetAttr(xmlElement, "Camera.Loop", instance.CameraMgr.Loop);
			base.SetAttr(xmlElement, "Camera.Speed", instance.CameraMgr.Speed);
			base.SetAttr(xmlElement, "Camera.ModelScale", instance.CameraMgr.modelScale);
			base.SetAttrVec3(xmlElement, "Camera.PosAdjust", instance.CameraMgr.cameraPosAdjust);
			base.SetAttr(xmlElement, "Camera.DistanceAdjust", instance.CameraMgr.cameraDistanceAdjust);
			base.SetAttr(xmlElement, "Camera.VMDFilename", instance.CameraMgr.cameraVMDFilePath);
			rootElement.AppendChild(xmlElement);
		}
	}
}
