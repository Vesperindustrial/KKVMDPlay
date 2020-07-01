using System;
using System.Xml;
using Studio;

namespace KKVMDPlayPlugin
{
	
	public class KKVMDAnimationDataSaveLoad : SaveLoadBase
	{
		
		public void LoadVMDAnimInfo(XmlElement vmdElem, ObjectCtrlInfo studioChara)
		{
			if (!(studioChara is OCIChar))
			{
				return;
			}
			VMDAnimationController vmdanimationController = VMDAnimationController.Install(((OCIChar)studioChara).charInfo);
			if (vmdanimationController != null)
			{
				string attribute = vmdElem.GetAttribute("lastLoadedVMD");
				if (!string.IsNullOrEmpty(attribute))
				{
					vmdanimationController.LoadVMDAnimation(attribute, false);
				}
				else
				{
					vmdanimationController.DeleteAnim();
					vmdanimationController.lastLoadedVMD = null;
				}
				VMDAnimationController.QuickAdjust quickAdjust = vmdanimationController.quickAdjust;
				quickAdjust.Shoulder = base.GetAttr(vmdElem, "adjustShoulder", quickAdjust.Shoulder);
				quickAdjust.ArmUp = base.GetAttr(vmdElem, "adjustArmUp", quickAdjust.ArmUp);
				quickAdjust.ArmLow = base.GetAttr(vmdElem, "adjustArmLow", quickAdjust.ArmLow);
				quickAdjust.ScaleModel = base.GetAttr(vmdElem, "adjustScaleModel", quickAdjust.ScaleModel);
				vmdanimationController.centerBasePos = base.GetAttrVec3(vmdElem, "centerPos", vmdanimationController.centerBasePos);
				vmdanimationController.centerPosAdjust = base.GetAttrVec3(vmdElem, "centerPosAdjust", vmdanimationController.centerPosAdjust);
				vmdanimationController.moveYScaleAdjust = base.GetAttr(vmdElem, "moveYScaleAdjust", vmdanimationController.moveYScaleAdjust);
				vmdanimationController.enableIK = base.GetAttr(vmdElem, "IK.enable", vmdanimationController.enableIK);
				VMDAnimationController.IKWeightData ikweight = vmdanimationController.IKWeight;
				ikweight.footIKPosWeight = base.GetAttr(vmdElem, "IK.footIKPosWeight", ikweight.footIKPosWeight);
				ikweight.footIKRotWeight = base.GetAttr(vmdElem, "IK.footIKRotWeight", ikweight.footIKRotWeight);
				ikweight.disableToeIK = base.GetAttr(vmdElem, "IK.disableToeIK", ikweight.disableToeIK);
				ikweight.lastFrameRefRate = base.GetAttr(vmdElem, "IK.lastFrameRefRate", ikweight.lastFrameRefRate);
				ikweight.limitIKPositionFloor = base.GetAttr(vmdElem, "IK.onFloor", ikweight.limitIKPositionFloor);
				vmdanimationController.speed = base.GetAttr(vmdElem, "Anim.speed", vmdanimationController.speed);
				vmdanimationController.Loop = base.GetAttr(vmdElem, "Anim.Loop", vmdanimationController.Loop);
				if (vmdanimationController.lastLoadedVMD != null)
				{
					vmdanimationController.LoadVMDAnimation(vmdanimationController.lastLoadedVMD, false);
				}
				if (base.GetAttr(vmdElem, "VMDAnimEnabled", false))
				{
					vmdanimationController.VMDAnimEnabled = true;
				}
				else
				{
					vmdanimationController.VMDAnimEnabled = false;
				}
				vmdanimationController.faceController.AnimeEnabled = base.GetAttr(vmdElem, "Face.enable", vmdanimationController.faceController.AnimeEnabled);
				vmdanimationController.AdjustSkirtBones = base.GetAttr(vmdElem, "AdjustSkirtBones", false);
			}
		}

		
		public void SaveVMDAnimInfo(XmlElement vmdElem, VMDAnimationController vmdAnimController)
		{
			if (vmdAnimController != null)
			{
				base.SetAttr(vmdElem, "VMDAnimEnabled", vmdAnimController.VMDAnimEnabled);
				if (vmdAnimController.lastLoadedVMD != null)
				{
					vmdElem.SetAttribute("lastLoadedVMD", vmdAnimController.lastLoadedVMD);
				}
				VMDAnimationController.QuickAdjust quickAdjust = vmdAnimController.quickAdjust;
				base.SetAttr(vmdElem, "adjustShoulder", quickAdjust.Shoulder);
				base.SetAttr(vmdElem, "adjustArmUp", quickAdjust.ArmUp);
				base.SetAttr(vmdElem, "adjustArmLow", quickAdjust.ArmLow);
				base.SetAttr(vmdElem, "adjustScaleModel", quickAdjust.ScaleModel);
				base.SetAttrVec3(vmdElem, "centerPos", vmdAnimController.centerBasePos);
				base.SetAttrVec3(vmdElem, "centerPosAdjust", vmdAnimController.centerPosAdjust);
				base.SetAttr(vmdElem, "moveYScaleAdjust", vmdAnimController.moveYScaleAdjust);
				base.SetAttr(vmdElem, "IK.enable", vmdAnimController.enableIK);
				VMDAnimationController.IKWeightData ikweight = vmdAnimController.IKWeight;
				base.SetAttr(vmdElem, "IK.footIKPosWeight", ikweight.footIKPosWeight);
				base.SetAttr(vmdElem, "IK.footIKRotWeight", ikweight.footIKRotWeight);
				base.SetAttr(vmdElem, "IK.disableToeIK", ikweight.disableToeIK);
				base.SetAttr(vmdElem, "IK.lastFrameRefRate", ikweight.lastFrameRefRate);
				base.SetAttr(vmdElem, "IK.onFloor", ikweight.limitIKPositionFloor);
				base.SetAttr(vmdElem, "Anim.speed", vmdAnimController.speed);
				base.SetAttr(vmdElem, "Anim.Loop", vmdAnimController.Loop);
				base.SetAttr(vmdElem, "Face.enable", vmdAnimController.faceController.AnimeEnabled);
				base.SetAttr(vmdElem, "AdjustSkirtBones", vmdAnimController.AdjustSkirtBones);
			}
		}
	}
}
